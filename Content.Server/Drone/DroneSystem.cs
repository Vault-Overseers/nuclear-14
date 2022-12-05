using Content.Shared.Drone;
using Content.Server.Drone.Components;
using Content.Shared.MobState;
using Content.Shared.MobState.Components;
using Content.Shared.Interaction.Events;
using Content.Shared.Interaction.Components;
using Content.Shared.Examine;
using Content.Shared.Tag;
using Content.Shared.Throwing;
using Content.Shared.Item;
using Content.Shared.Emoting;
using Content.Shared.Body.Components;
using Content.Shared.IdentityManagement;
using Content.Shared.Popups;
using Content.Server.Popups;
using Content.Server.Mind.Components;
using Content.Server.Ghost.Components;
using Content.Server.Ghost.Roles.Components;
using Content.Server.Tools.Innate;
using Content.Server.UserInterface;
using Robust.Shared.Player;
using Robust.Shared.Timing;

namespace Content.Server.Drone
{
    public sealed class DroneSystem : SharedDroneSystem
    {
        [Dependency] private readonly PopupSystem _popupSystem = default!;
        [Dependency] private readonly TagSystem _tagSystem = default!;
        [Dependency] private readonly EntityLookupSystem _lookup = default!;
        [Dependency] private readonly IGameTiming _gameTiming = default!;
        [Dependency] private readonly InnateToolSystem _innateToolSystem = default!;

        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<DroneComponent, InteractionAttemptEvent>(OnInteractionAttempt);
            SubscribeLocalEvent<DroneComponent, UserOpenActivatableUIAttemptEvent>(OnActivateUIAttempt);
            SubscribeLocalEvent<DroneComponent, MobStateChangedEvent>(OnMobStateChanged);
            SubscribeLocalEvent<DroneComponent, ExaminedEvent>(OnExamined);
            SubscribeLocalEvent<DroneComponent, MindAddedMessage>(OnMindAdded);
            SubscribeLocalEvent<DroneComponent, MindRemovedMessage>(OnMindRemoved);
            SubscribeLocalEvent<DroneComponent, EmoteAttemptEvent>(OnEmoteAttempt);
            SubscribeLocalEvent<DroneComponent, ThrowAttemptEvent>(OnThrowAttempt);
        }

        private void OnInteractionAttempt(EntityUid uid, DroneComponent component, InteractionAttemptEvent args)
        {
            if (args.Target != null && !HasComp<UnremoveableComponent>(args.Target) && NonDronesInRange(uid, component))
                args.Cancel();

            if (HasComp<ItemComponent>(args.Target) && !HasComp<UnremoveableComponent>(args.Target))
            {
                if (!_tagSystem.HasAnyTag(args.Target.Value, "DroneUsable", "Trash"))
                    args.Cancel();
            }
        }

        private void OnActivateUIAttempt(EntityUid uid, DroneComponent component, UserOpenActivatableUIAttemptEvent args)
        {
            if (!_tagSystem.HasTag(args.Target, "DroneUsable"))
            {
                args.Cancel();
            }
        }

        private void OnExamined(EntityUid uid, DroneComponent component, ExaminedEvent args)
        {
            if (TryComp<MindComponent>(uid, out var mind) && mind.HasMind)
            {
                args.PushMarkup(Loc.GetString("drone-active"));
            }
            else
            {
                args.PushMarkup(Loc.GetString("drone-dormant"));
            }
        }

        private void OnMobStateChanged(EntityUid uid, DroneComponent drone, MobStateChangedEvent args)
        {
            if (args.CurrentMobState == DamageState.Dead)
            {
                if (TryComp<InnateToolComponent>(uid, out var innate))
                    _innateToolSystem.Cleanup(uid, innate);

                if (TryComp<SharedBodyComponent>(uid, out var body))
                    body.Gib();
                Del(uid);
            }
        }

        private void OnMindAdded(EntityUid uid, DroneComponent drone, MindAddedMessage args)
        {
            UpdateDroneAppearance(uid, DroneStatus.On);
            _popupSystem.PopupEntity(Loc.GetString("drone-activated"), uid,
                Filter.Pvs(uid), PopupType.Large);
        }

        private void OnMindRemoved(EntityUid uid, DroneComponent drone, MindRemovedMessage args)
        {
            UpdateDroneAppearance(uid, DroneStatus.Off);
            EnsureComp<GhostTakeoverAvailableComponent>(uid);
        }

        private void OnEmoteAttempt(EntityUid uid, DroneComponent component, EmoteAttemptEvent args)
        {
            // No.
            args.Cancel();
        }

        private void OnThrowAttempt(EntityUid uid, DroneComponent drone, ThrowAttemptEvent args)
        {
            args.Cancel();
        }

        private void UpdateDroneAppearance(EntityUid uid, DroneStatus status)
        {
            if (TryComp<AppearanceComponent>(uid, out var appearance))
            {
                appearance.SetData(DroneVisuals.Status, status);
            }
        }

        private bool NonDronesInRange(EntityUid uid, DroneComponent component)
        {
            var xform = Comp<TransformComponent>(uid);
            foreach (var entity in _lookup.GetEntitiesInRange(xform.MapPosition, component.InteractionBlockRange))
            {
                // Return true if the entity is/was controlled by a player and is not a drone or ghost.
                if (HasComp<MindComponent>(entity) && !HasComp<DroneComponent>(entity) && !HasComp<GhostComponent>(entity))
                {
                    // Filter out dead ghost roles. Dead normal players are intended to block.
                    if ((TryComp<MobStateComponent>(entity, out var entityMobState) && HasComp<GhostTakeoverAvailableComponent>(entity) && entityMobState.IsDead()))
                        continue;
                    if (_gameTiming.IsFirstTimePredicted)
                        _popupSystem.PopupEntity(Loc.GetString("drone-too-close", ("being", Identity.Entity(entity, EntityManager))), uid, Filter.Entities(uid));
                    return true;
                }
            }
            return false;
        }
    }
}
