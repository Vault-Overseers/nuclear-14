using Content.Server.Chemistry.EntitySystems;
using Content.Server.Power.Components;
using Content.Server.Power.EntitySystems;
using Content.Server.Power.Events;
using Content.Server.Stunnable.Components;
using Content.Shared.Audio;
using Content.Shared.Damage.Events;
using Content.Shared.Examine;
using Content.Shared.Interaction.Events;
using Content.Shared.Item;
using Content.Shared.Popups;
using Content.Shared.Stunnable;
using Content.Shared.Toggleable;
using Robust.Shared.Audio;
using Robust.Shared.Player;

namespace Content.Server.Stunnable.Systems
{
    public sealed class StunbatonSystem : SharedStunbatonSystem
    {
        [Dependency] private readonly SharedItemSystem _item = default!;
        [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
        [Dependency] private readonly RiggableSystem _riggableSystem = default!;

        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<StunbatonComponent, UseInHandEvent>(OnUseInHand);
            SubscribeLocalEvent<StunbatonComponent, ExaminedEvent>(OnExamined);
            SubscribeLocalEvent<StunbatonComponent, SolutionChangedEvent>(OnSolutionChange);
            SubscribeLocalEvent<StunbatonComponent, StaminaDamageOnHitAttemptEvent>(OnStaminaHitAttempt);
        }

        private void OnStaminaHitAttempt(EntityUid uid, StunbatonComponent component, ref StaminaDamageOnHitAttemptEvent args)
        {
            if (!component.Activated ||
                !TryComp<BatteryComponent>(uid, out var battery) || !battery.TryUseCharge(component.EnergyPerUse))
            {
                args.Cancelled = true;
                return;
            }

            if (battery.CurrentCharge < component.EnergyPerUse)
            {
                SoundSystem.Play(component.SparksSound.GetSound(), Filter.Pvs(component.Owner, entityManager: EntityManager), uid, AudioHelpers.WithVariation(0.25f));
                TurnOff(uid, component);
            }
        }

        private void OnUseInHand(EntityUid uid, StunbatonComponent comp, UseInHandEvent args)
        {
            if (comp.Activated)
            {
                TurnOff(uid, comp);
            }
            else
            {
                TurnOn(uid, comp, args.User);
            }
        }

        private void OnExamined(EntityUid uid, StunbatonComponent comp, ExaminedEvent args)
        {
            var msg = comp.Activated
                ? Loc.GetString("comp-stunbaton-examined-on")
                : Loc.GetString("comp-stunbaton-examined-off");
            args.PushMarkup(msg);
            if(TryComp<BatteryComponent>(uid, out var battery))
                args.PushMarkup(Loc.GetString("stunbaton-component-on-examine-charge",
                    ("charge", (int)((battery.CurrentCharge/battery.MaxCharge) * 100))));
        }

        private void TurnOff(EntityUid uid, StunbatonComponent comp)
        {
            if (!comp.Activated)
                return;

            if (TryComp<AppearanceComponent>(comp.Owner, out var appearance) &&
                TryComp<ItemComponent>(comp.Owner, out var item))
            {
                _item.SetHeldPrefix(comp.Owner, "off", item);
                _appearance.SetData(uid, ToggleVisuals.Toggled, false, appearance);
            }

            SoundSystem.Play(comp.SparksSound.GetSound(), Filter.Pvs(comp.Owner), comp.Owner, AudioHelpers.WithVariation(0.25f));

            comp.Activated = false;
            Dirty(comp);
        }

        private void TurnOn(EntityUid uid, StunbatonComponent comp, EntityUid user)
        {

            if (comp.Activated)
                return;

            var playerFilter = Filter.Pvs(comp.Owner, entityManager: EntityManager);
            if (!TryComp<BatteryComponent>(comp.Owner, out var battery) || battery.CurrentCharge < comp.EnergyPerUse)
            {

                SoundSystem.Play(comp.TurnOnFailSound.GetSound(), playerFilter, comp.Owner, AudioHelpers.WithVariation(0.25f));
                user.PopupMessage(Loc.GetString("stunbaton-component-low-charge"));
                return;
            }

            if (TryComp<RiggableComponent>(uid, out var rig) && rig.IsRigged)
            {
                _riggableSystem.Explode(uid, battery, user);
            }


            if (EntityManager.TryGetComponent<AppearanceComponent>(comp.Owner, out var appearance) &&
                EntityManager.TryGetComponent<ItemComponent>(comp.Owner, out var item))
            {
                _item.SetHeldPrefix(comp.Owner, "on", item);
                _appearance.SetData(uid, ToggleVisuals.Toggled, true, appearance);
            }

            SoundSystem.Play(comp.SparksSound.GetSound(), playerFilter, comp.Owner, AudioHelpers.WithVariation(0.25f));
            comp.Activated = true;
            Dirty(comp);
        }

        // https://github.com/space-wizards/space-station-14/pull/17288#discussion_r1241213341
        private void OnSolutionChange(EntityUid uid, StunbatonComponent component, SolutionChangedEvent args)
        {
            // Explode if baton is activated and rigged.
            if (TryComp<RiggableComponent>(uid, out var riggable))
                if (TryComp<BatteryComponent>(uid, out var battery))
                    if (component.Activated && riggable.IsRigged)
                        _riggableSystem.Explode(uid, battery);
        }

        private void SendPowerPulse(EntityUid target, EntityUid? user, EntityUid used)
        {
            RaiseLocalEvent(target, new PowerPulseEvent()
            {
                Used = used,
                User = user
            }, false);
        }
    }
}
