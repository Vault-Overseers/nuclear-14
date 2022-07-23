using Content.Server.Audio;
using Content.Server.GameTicking;
using Content.Server.Players;
using Content.Server.Popups;
using Content.Server.Power.Components;
using Content.Server.Power.EntitySystems;
using Content.Server.Recycling.Components;
using Content.Shared.Audio;
using Content.Shared.Body.Components;
using Content.Shared.Emag.Systems;
using Content.Shared.IdentityManagement;
using Content.Shared.Interaction.Events;
using Content.Shared.Recycling;
using Content.Shared.Tag;
using Robust.Server.GameObjects;
using Robust.Shared.Audio;
using Robust.Shared.Physics.Dynamics;
using Robust.Shared.Player;
using Robust.Shared.Timing;

namespace Content.Server.Recycling
{
    public sealed class RecyclerSystem : EntitySystem
    {
        [Dependency] private readonly IGameTiming _timing = default!;
        [Dependency] private readonly AmbientSoundSystem _ambience = default!;
        [Dependency] private readonly GameTicker _ticker = default!;
        [Dependency] private readonly PopupSystem _popup = default!;
        [Dependency] private readonly TagSystem _tags = default!;

        private const float RecyclerSoundCooldown = 0.8f;

        public override void Initialize()
        {
            SubscribeLocalEvent<RecyclerComponent, StartCollideEvent>(OnCollide);
            SubscribeLocalEvent<RecyclerComponent, GotEmaggedEvent>(OnEmagged);
            SubscribeLocalEvent<RecyclerComponent, SuicideEvent>(OnSuicide);
        }

        private void OnSuicide(EntityUid uid, RecyclerComponent component, SuicideEvent args)
        {
            if (args.Handled) return;
            args.SetHandled(SuicideKind.Bloodloss);
            var victim = args.Victim;
            if (TryComp(victim, out ActorComponent? actor) &&
                actor.PlayerSession.ContentData()?.Mind is { } mind)
            {
                _ticker.OnGhostAttempt(mind, false);
                if (mind.OwnedEntity is { Valid: true } entity)
                {
                    _popup.PopupEntity(Loc.GetString("recycler-component-suicide-message"), entity, Filter.Pvs(entity, entityManager: EntityManager));
                }
            }

            _popup.PopupEntity(Loc.GetString("recycler-component-suicide-message-others", ("victim", Identity.Entity(victim, EntityManager))),
                victim,
                Filter.Pvs(victim, entityManager: EntityManager).RemoveWhereAttachedEntity(e => e == victim));

            if (TryComp<SharedBodyComponent?>(victim, out var body))
            {
                body.Gib(true);
            }

            Bloodstain(component);
        }

        public void EnableRecycler(RecyclerComponent component)
        {
            if (component.Enabled) return;

            component.Enabled = true;
            _ambience.SetAmbience(component.Owner, true);
        }

        public void DisableRecycler(RecyclerComponent component)
        {
            if (!component.Enabled) return;

            component.Enabled = false;
            _ambience.SetAmbience(component.Owner, false);
        }

        private void OnCollide(EntityUid uid, RecyclerComponent component, StartCollideEvent args)
        {
            if (component.Enabled && args.OurFixture.ID != "brrt") return;

            Recycle(component, args.OtherFixture.Body.Owner);
        }

        private void Recycle(RecyclerComponent component, EntityUid entity)
        {
            RecyclableComponent? recyclable = null;

            // Can only recycle things that are recyclable... And also check the safety of the thing to recycle.
            if (!_tags.HasTag(entity, "Recyclable") &&
                (!TryComp(entity, out recyclable) || !recyclable.Safe && component.Safe))
            {
                return;
            }

            // TODO: Prevent collision with recycled items

            // Mobs are a special case!
            if (CanGib(component, entity))
            {
                Comp<SharedBodyComponent>(entity).Gib(true);
                Bloodstain(component);
                return;
            }

            if (recyclable == null)
                QueueDel(entity);
            else
                Recycle(recyclable, component.Efficiency);

            if (component.Sound != null && (_timing.CurTime - component.LastSound).TotalSeconds > RecyclerSoundCooldown)
            {
                SoundSystem.Play(component.Sound.GetSound(), Filter.Pvs(component.Owner, entityManager: EntityManager), component.Owner, AudioHelpers.WithVariation(0.01f).WithVolume(-3));
                component.LastSound = _timing.CurTime;
            }
        }

        private bool CanGib(RecyclerComponent component, EntityUid entity)
        {
            return HasComp<SharedBodyComponent>(entity) && !component.Safe &&
                   this.IsPowered(component.Owner, EntityManager);
        }

        public void Bloodstain(RecyclerComponent component)
        {
            if (EntityManager.TryGetComponent(component.Owner, out AppearanceComponent? appearance))
            {
                appearance.SetData(RecyclerVisuals.Bloody, true);
            }
        }

        private void Recycle(RecyclableComponent component, float efficiency = 1f)
        {
            if (!string.IsNullOrEmpty(component.Prototype))
            {
                var xform = Transform(component.Owner);

                for (var i = 0; i < Math.Max(component.Amount * efficiency, 1); i++)
                {
                    Spawn(component.Prototype, xform.Coordinates);
                }
            }

            QueueDel(component.Owner);
        }

        private void OnEmagged(EntityUid uid, RecyclerComponent component, GotEmaggedEvent args)
        {
            if (!component.Safe) return;
            component.Safe = false;
            args.Handled = true;
        }
    }
}
