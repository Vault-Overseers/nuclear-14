using Content.Server.Chemistry.Components;
using Content.Server.Chemistry.Components.SolutionManager;
using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.FixedPoint;
using Content.Shared.Physics;
using Content.Shared.Spawners.Components;
using Content.Shared.Throwing;
using Content.Shared.Vapor;
using JetBrains.Annotations;
using Robust.Shared.Map;
using Robust.Shared.Physics.Dynamics;
using Robust.Shared.Prototypes;

namespace Content.Server.Chemistry.EntitySystems
{
    [UsedImplicitly]
    internal sealed class VaporSystem : EntitySystem
    {
        [Dependency] private readonly IMapManager _mapManager = default!;
        [Dependency] private readonly IPrototypeManager _protoManager = default!;
        [Dependency] private readonly SolutionContainerSystem _solutionContainerSystem = default!;
        [Dependency] private readonly ThrowingSystem _throwing = default!;

        private const float ReactTime = 0.125f;

        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<VaporComponent, StartCollideEvent>(HandleCollide);
        }

        private void HandleCollide(EntityUid uid, VaporComponent component, StartCollideEvent args)
        {
            if (!EntityManager.TryGetComponent(uid, out SolutionContainerManagerComponent? contents)) return;

            foreach (var (_, value) in contents.Solutions)
            {
                value.DoEntityReaction(args.OtherFixture.Body.Owner, ReactionMethod.Touch);
            }

            // Check for collision with a impassable object (e.g. wall) and stop
            if ((args.OtherFixture.CollisionLayer & (int) CollisionGroup.Impassable) != 0 && args.OtherFixture.Hard)
            {
                EntityManager.QueueDeleteEntity(uid);
            }
        }

        public void Start(VaporComponent vapor, TransformComponent vaporXform, Vector2 dir, float speed, MapCoordinates target, float aliveTime, EntityUid? user = null)
        {
            vapor.Active = true;
            var despawn = EnsureComp<TimedDespawnComponent>(vapor.Owner);
            despawn.Lifetime = aliveTime;

            // Set Move
            if (EntityManager.TryGetComponent(vapor.Owner, out PhysicsComponent? physics))
            {
                physics.LinearDamping = 0f;
                physics.AngularDamping = 0f;

                _throwing.TryThrow(vapor.Owner, dir * speed, user: user, pushbackRatio: 50f);

                var distance = (target.Position - vaporXform.WorldPosition).Length;
                var time = (distance / physics.LinearVelocity.Length);
                despawn.Lifetime = MathF.Min(aliveTime, time);
            }
        }

        internal bool TryAddSolution(VaporComponent vapor, Solution solution)
        {
            if (solution.TotalVolume == 0)
            {
                return false;
            }

            if (!_solutionContainerSystem.TryGetSolution(vapor.Owner, VaporComponent.SolutionName,
                out var vaporSolution))
            {
                return false;
            }

            return _solutionContainerSystem.TryAddSolution(vapor.Owner, vaporSolution, solution);
        }

        public override void Update(float frameTime)
        {
            foreach (var (vaporComp, solution, xform) in EntityManager
                .EntityQuery<VaporComponent, SolutionContainerManagerComponent, TransformComponent>())
            {
                foreach (var (_, value) in solution.Solutions)
                {
                    Update(frameTime, vaporComp, value, xform);
                }
            }
        }

        private void Update(float frameTime, VaporComponent vapor, Solution contents, TransformComponent xform)
        {
            if (!vapor.Active)
                return;

            var entity = vapor.Owner;

            vapor.ReactTimer += frameTime;

            if (vapor.ReactTimer >= ReactTime && TryComp(xform.GridUid, out IMapGridComponent? gridComp))
            {
                vapor.ReactTimer = 0;

                var tile = gridComp.Grid.GetTileRef(xform.Coordinates.ToVector2i(EntityManager, _mapManager));
                foreach (var reagentQuantity in contents.Contents.ToArray())
                {
                    if (reagentQuantity.Quantity == FixedPoint2.Zero) continue;
                    var reagent = _protoManager.Index<ReagentPrototype>(reagentQuantity.ReagentId);
                    _solutionContainerSystem.TryRemoveReagent(vapor.Owner, contents, reagentQuantity.ReagentId,
                        reagent.ReactionTile(tile, (reagentQuantity.Quantity / vapor.TransferAmount) * 0.25f));
                }
            }

            if (contents.CurrentVolume == 0)
            {
                // Delete this
                EntityManager.QueueDeleteEntity(entity);
            }
        }
    }
}
