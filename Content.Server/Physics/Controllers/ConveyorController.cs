using Content.Server.Conveyor;
using Content.Server.Gravity.EntitySystems;
using Content.Server.MachineLinking.Events;
using Content.Server.MachineLinking.System;
using Content.Server.Power.Components;
using Content.Server.Power.EntitySystems;
using Content.Server.Recycling;
using Content.Server.Recycling.Components;
using Content.Shared.Conveyor;
using Content.Shared.Item;
using Content.Shared.Movement.Components;
using Content.Shared.Physics;
using Robust.Shared.Containers;
using Robust.Shared.Map;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Collision.Shapes;
using Robust.Shared.Physics.Controllers;
using Robust.Shared.Physics.Dynamics;

namespace Content.Server.Physics.Controllers
{
    public sealed class ConveyorController : VirtualController
    {
        [Dependency] private readonly IMapManager _mapManager = default!;
        [Dependency] private readonly EntityLookupSystem _lookup = default!;
        [Dependency] private readonly FixtureSystem _fixtures = default!;
        [Dependency] private readonly GravitySystem _gravity = default!;
        [Dependency] private readonly RecyclerSystem _recycler = default!;
        [Dependency] private readonly SignalLinkerSystem _signalSystem = default!;

        public const string ConveyorFixture = "conveyor";

        public override void Initialize()
        {
            UpdatesAfter.Add(typeof(MoverController));
            SubscribeLocalEvent<ConveyorComponent, ComponentInit>(OnInit);
            SubscribeLocalEvent<ConveyorComponent, ComponentShutdown>(OnConveyorShutdown);
            SubscribeLocalEvent<ConveyorComponent, SignalReceivedEvent>(OnSignalReceived);
            SubscribeLocalEvent<ConveyorComponent, PowerChangedEvent>(OnPowerChanged);
            SubscribeLocalEvent<ConveyorComponent, StartCollideEvent>(OnConveyorStartCollide);
            SubscribeLocalEvent<ConveyorComponent, EndCollideEvent>(OnConveyorEndCollide);

            base.Initialize();
        }

        private void OnConveyorEndCollide(EntityUid uid, ConveyorComponent component, EndCollideEvent args)
        {
            component.Intersecting.Remove(args.OtherFixture.Body.Owner);

            if (component.Intersecting.Count == 0)
            {
                RemComp<ActiveConveyorComponent>(uid);
            }
        }

        private void OnConveyorStartCollide(EntityUid uid, ConveyorComponent component, StartCollideEvent args)
        {
            var otherUid = args.OtherFixture.Body.Owner;

            if (args.OtherFixture.Body.BodyType == BodyType.Static) return;

            component.Intersecting.Add(otherUid);
            EnsureComp<ActiveConveyorComponent>(uid);
        }

        private void OnInit(EntityUid uid, ConveyorComponent component, ComponentInit args)
        {
            _signalSystem.EnsureReceiverPorts(uid, component.ReversePort, component.ForwardPort, component.OffPort);

            if (TryComp<PhysicsComponent>(uid, out var body))
            {
                var shape = new PolygonShape();
                shape.SetAsBox(0.55f, 0.55f);

                _fixtures.TryCreateFixture(body, new Fixture(body, shape)
                {
                    ID = ConveyorFixture,
                    CollisionLayer = (int) (CollisionGroup.LowImpassable | CollisionGroup.MidImpassable | CollisionGroup.Impassable),
                    Hard = false,
                });
            }
        }

        private void OnPowerChanged(EntityUid uid, ConveyorComponent component, PowerChangedEvent args)
        {
            UpdateAppearance(component);
        }

        private void UpdateAppearance(ConveyorComponent component)
        {
            if (!EntityManager.TryGetComponent<AppearanceComponent?>(component.Owner, out var appearance)) return;
            var isPowered = this.IsPowered(component.Owner, EntityManager);
            appearance.SetData(ConveyorVisuals.State, isPowered ? component.State : ConveyorState.Off);
        }

        private void OnSignalReceived(EntityUid uid, ConveyorComponent component, SignalReceivedEvent args)
        {
            if (args.Port == component.OffPort)
                SetState(component, ConveyorState.Off);
            else if (args.Port == component.ForwardPort)
                SetState(component, ConveyorState.Forward);
            else if (args.Port == component.ReversePort)
                SetState(component, ConveyorState.Reverse);
        }

        private void SetState(ConveyorComponent component, ConveyorState state)
        {
            component.State = state;

            if (TryComp<RecyclerComponent>(component.Owner, out var recycler))
            {
                if (component.State != ConveyorState.Off)
                    _recycler.EnableRecycler(recycler);
                else
                    _recycler.DisableRecycler(recycler);
            }

            UpdateAppearance(component);
        }

        public bool CanRun(ConveyorComponent component)
        {
            return component.State != ConveyorState.Off && this.IsPowered(component.Owner, EntityManager);
        }

        private void OnConveyorShutdown(EntityUid uid, ConveyorComponent component, ComponentShutdown args)
        {
            RemComp<ActiveConveyorComponent>(uid);

            if (!TryComp<PhysicsComponent>(uid, out var body))
                return;

            _fixtures.DestroyFixture(body, ConveyorFixture);
        }

        public override void UpdateBeforeSolve(bool prediction, float frameTime)
        {
            base.UpdateBeforeSolve(prediction, frameTime);

            var conveyed = new HashSet<EntityUid>();
            // Don't use it directly in EntityQuery because we may be able to save getcomponents.
            var xformQuery = GetEntityQuery<TransformComponent>();
            var bodyQuery = GetEntityQuery<PhysicsComponent>();

            foreach (var (_, comp) in EntityQuery<ActiveConveyorComponent, ConveyorComponent>())
            {
                Convey(comp, xformQuery, bodyQuery, conveyed, frameTime);
            }
        }

        private void Convey(ConveyorComponent comp, EntityQuery<TransformComponent> xformQuery, EntityQuery<PhysicsComponent> bodyQuery, HashSet<EntityUid> conveyed, float frameTime)
        {
            // Use an event for conveyors to know what needs to run
            if (!CanRun(comp))
            {
                return;
            }

            var speed = comp.Speed;

            if (speed <= 0f ||
                !xformQuery.TryGetComponent(comp.Owner, out var xform) ||
                xform.GridUid == null)
                return;

            var conveyorPos = xform.LocalPosition;
            var conveyorRot = xform.LocalRotation;

            conveyorRot += comp.Angle;

            if (comp.State == ConveyorState.Reverse)
            {
                conveyorRot += MathF.PI;
            }

            var direction = conveyorRot.ToWorldVec();

            foreach (var (entity, transform, body) in GetEntitiesToMove(comp, xform, xformQuery, bodyQuery))
            {
                if (!conveyed.Add(entity))
                    continue;

                var localPos = transform.LocalPosition;
                var itemRelative = conveyorPos - localPos;

                localPos += Convey(direction, speed, frameTime, itemRelative);
                transform.LocalPosition = localPos;

                // Force it awake for collisionwake reasons.
                body.Awake = true;
                body.SleepTime = 0f;
            }
        }

        private static Vector2 Convey(Vector2 direction, float speed, float frameTime, Vector2 itemRelative)
        {
            if (speed == 0 || direction.Length == 0) return Vector2.Zero;

            /*
             * Basic idea: if the item is not in the middle of the conveyor in the direction that the conveyor is running,
             * move the item towards the middle. Otherwise, move the item along the direction. This lets conveyors pick up
             * items that are not perfectly aligned in the middle, and also makes corner cuts work.
             *
             * We do this by computing the projection of 'itemRelative' on 'direction', yielding a vector 'p' in the direction
             * of 'direction'. We also compute the rejection 'r'. If the magnitude of 'r' is not (near) zero, then the item
             * is not on the centerline.
             */

            var p = direction * (Vector2.Dot(itemRelative, direction) / Vector2.Dot(direction, direction));
            var r = itemRelative - p;

            if (r.Length < 0.1)
            {
                var velocity = direction * speed;
                return velocity * frameTime;
            }
            else
            {
                var velocity = r.Normalized * speed;
                return velocity * frameTime;
            }
        }

        private IEnumerable<(EntityUid, TransformComponent, PhysicsComponent)> GetEntitiesToMove(
            ConveyorComponent comp,
            TransformComponent xform,
            EntityQuery<TransformComponent> xformQuery,
            EntityQuery<PhysicsComponent> bodyQuery)
        {
            // Check if the thing's centre overlaps the grid tile.
            var grid = _mapManager.GetGrid(xform.GridUid!.Value);
            var tile = grid.GetTileRef(xform.Coordinates);
            var conveyorBounds = _lookup.GetLocalBounds(tile, grid.TileSize);

            foreach (var entity in comp.Intersecting)
            {
                if (!xformQuery.TryGetComponent(entity, out var entityXform) ||
                    entityXform.ParentUid != grid.GridEntityId)
                {
                    continue;
                }

                if (!bodyQuery.TryGetComponent(entity, out var physics) ||
                    physics.BodyType == BodyType.Static ||
                    physics.BodyStatus == BodyStatus.InAir ||
                    _gravity.IsWeightless(entity, physics, entityXform))
                {
                    continue;
                }

                // Yes there's still going to be the occasional rounding issue where it stops getting conveyed
                // When you fix the corner issue that will fix this anyway.
                var gridAABB = new Box2(entityXform.LocalPosition - 0.1f, entityXform.LocalPosition + 0.1f);

                if (!conveyorBounds.Intersects(gridAABB))
                    continue;

                yield return (entity, entityXform, physics);
            }
        }
    }
}
