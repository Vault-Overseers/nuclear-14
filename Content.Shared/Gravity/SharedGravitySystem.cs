using Content.Shared.Alert;
using Content.Shared.Clothing;
using Content.Shared.Inventory;
using Content.Shared.Movement.Components;
using Robust.Shared.GameStates;
using Robust.Shared.Map;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Components;
using Robust.Shared.Serialization;

namespace Content.Shared.Gravity
{
    public abstract class SharedGravitySystem : EntitySystem
    {
        [Dependency] private readonly AlertsSystem _alerts = default!;
        [Dependency] private readonly InventorySystem _inventory = default!;

        public bool IsWeightless(EntityUid uid, PhysicsComponent? body = null, TransformComponent? xform = null)
        {
            Resolve(uid, ref body, false);

            if ((body?.BodyType & (BodyType.Static | BodyType.Kinematic)) != 0)
                return false;

            if (TryComp<MovementIgnoreGravityComponent>(uid, out var ignoreGravityComponent))
                return ignoreGravityComponent.Weightless;

            if (!Resolve(uid, ref xform))
                return true;

            // If grid / map has gravity
            if ((TryComp<GravityComponent>(xform.GridUid, out var gravity) ||
                 TryComp(xform.MapUid, out gravity)) && gravity.Enabled)
            {
                return false;
            }

            // Something holding us down
            // If the planet has gravity component and no gravity it will still give gravity
            // If there's no gravity comp at all (i.e. space) then they don't work.
            if (gravity != null && _inventory.TryGetSlotEntity(uid, "shoes", out var ent))
            {
                if (TryComp<MagbootsComponent>(ent, out var boots) && boots.On)
                    return false;
            }

            return true;
        }

        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<GridInitializeEvent>(HandleGridInitialize);
            SubscribeLocalEvent<AlertsComponent, EntParentChangedMessage>(OnAlertsParentChange);
            SubscribeLocalEvent<GravityChangedEvent>(OnGravityChange);
            SubscribeLocalEvent<GravityComponent, ComponentGetState>(OnGetState);
            SubscribeLocalEvent<GravityComponent, ComponentHandleState>(OnHandleState);
        }

        private void OnHandleState(EntityUid uid, GravityComponent component, ref ComponentHandleState args)
        {
            if (args.Current is not GravityComponentState state) return;

            if (component.EnabledVV == state.Enabled) return;
            component.EnabledVV = state.Enabled;
            RaiseLocalEvent(new GravityChangedEvent(uid, component.EnabledVV));
        }

        private void OnGetState(EntityUid uid, GravityComponent component, ref ComponentGetState args)
        {
            args.State = new GravityComponentState(component.EnabledVV);
        }

        private void OnGravityChange(GravityChangedEvent ev)
        {
            foreach (var (comp, xform) in EntityQuery<AlertsComponent, TransformComponent>(true))
            {
                if (xform.GridUid != ev.ChangedGridIndex) continue;

                if (!ev.HasGravity)
                {
                    _alerts.ShowAlert(comp.Owner, AlertType.Weightless);
                }
                else
                {
                    _alerts.ClearAlert(comp.Owner, AlertType.Weightless);
                }
            }
        }

        private void OnAlertsParentChange(EntityUid uid, AlertsComponent component, ref EntParentChangedMessage args)
        {
            if (IsWeightless(component.Owner))
            {
                _alerts.ShowAlert(uid, AlertType.Weightless);
            }
            else
            {
                _alerts.ClearAlert(uid, AlertType.Weightless);
            }
        }

        private void HandleGridInitialize(GridInitializeEvent ev)
        {
            EntityManager.EnsureComponent<GravityComponent>(ev.EntityUid);
        }

        [Serializable, NetSerializable]
        private sealed class GravityComponentState : ComponentState
        {
            public bool Enabled { get; }

            public GravityComponentState(bool enabled)
            {
                Enabled = enabled;
            }
        }
    }
}
