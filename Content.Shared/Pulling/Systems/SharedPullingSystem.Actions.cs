using Content.Shared.ActionBlocker;
using Content.Shared.Buckle.Components;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Physics.Pull;
using Content.Shared.Pulling.Components;
using Content.Shared.Pulling.Events;
using Robust.Shared.Containers;
using Robust.Shared.Map;
using Robust.Shared.Physics;

namespace Content.Shared.Pulling
{
    public abstract partial class SharedPullingSystem : EntitySystem
    {
        [Dependency] private readonly ActionBlockerSystem _blocker = default!;
        [Dependency] private readonly SharedContainerSystem _containerSystem = default!;
        [Dependency] private readonly SharedHandsSystem _handsSystem = default!;

        public bool CanPull(EntityUid puller, EntityUid pulled)
        {
            if (!EntityManager.TryGetComponent<SharedPullerComponent>(puller, out var comp))
            {
                return false;
            }

            if (comp.NeedsHands && !_handsSystem.TryGetEmptyHand(puller, out _))
            {
                return false;
            }

            if (!_blocker.CanInteract(puller, pulled))
            {
                return false;
            }

            if (!EntityManager.TryGetComponent<IPhysBody>(pulled, out var physics))
            {
                return false;
            }

            if (physics.BodyType == BodyType.Static)
            {
                return false;
            }

            if (puller == pulled)
            {
                return false;
            }

            if (!_containerSystem.IsInSameOrNoContainer(puller, pulled))
            {
                return false;
            }

            if (EntityManager.TryGetComponent<SharedBuckleComponent?>(puller, out var buckle))
            {
                // Prevent people pulling the chair they're on, etc.
                if (buckle.Buckled && (buckle.LastEntityBuckledTo == pulled))
                {
                    return false;
                }
            }

            var getPulled = new BeingPulledAttemptEvent(puller, pulled);
            RaiseLocalEvent(pulled, getPulled, true);
            var startPull = new StartPullAttemptEvent(puller, pulled);
            RaiseLocalEvent(puller, startPull, true);
            return (!startPull.Cancelled && !getPulled.Cancelled);
        }

        public bool TogglePull(EntityUid puller, SharedPullableComponent pullable)
        {
            if (pullable.Puller == puller)
            {
                return TryStopPull(pullable);
            }
            return TryStartPull(puller, pullable.Owner);
        }

        // -- Core attempted actions --

        public bool TryStopPull(SharedPullableComponent pullable, EntityUid? user = null)
        {
            if (!pullable.BeingPulled)
            {
                return false;
            }

            var msg = new StopPullingEvent(user);
            RaiseLocalEvent(pullable.Owner, msg, true);

            if (msg.Cancelled) return false;

            // Stop pulling confirmed!

            if (TryComp<PhysicsComponent>(pullable.Owner, out var pullablePhysics))
            {
                pullablePhysics.FixedRotation = pullable.PrevFixedRotation;
            }

            _pullSm.ForceRelationship(null, pullable);
            return true;
        }

        public bool TryStartPull(EntityUid puller, EntityUid pullable)
        {
            if (!EntityManager.TryGetComponent<SharedPullerComponent?>(puller, out var pullerComp))
            {
                return false;
            }
            if (!EntityManager.TryGetComponent<SharedPullableComponent?>(pullable, out var pullableComp))
            {
                return false;
            }
            return TryStartPull(pullerComp, pullableComp);
        }

        // The main "start pulling" function.
        public bool TryStartPull(SharedPullerComponent puller, SharedPullableComponent pullable)
        {
            if (puller.Pulling == pullable.Owner)
                return true;

            // Pulling a new object : Perform sanity checks.

            if (!CanPull(puller.Owner, pullable.Owner))
            {
                return false;
            }

            if (!EntityManager.TryGetComponent<PhysicsComponent>(puller.Owner, out var pullerPhysics))
            {
                return false;
            }

            if (!EntityManager.TryGetComponent<PhysicsComponent>(pullable.Owner, out var pullablePhysics))
            {
                return false;
            }

            // Ensure that the puller is not currently pulling anything.
            // If this isn't done, then it happens too late, and the start/stop messages go out of order,
            //  and next thing you know it thinks it's not pulling anything even though it is!

            var oldPullable = puller.Pulling;
            if (oldPullable != null)
            {
                if (EntityManager.TryGetComponent<SharedPullableComponent?>(oldPullable.Value, out var oldPullableComp))
                {
                    if (!TryStopPull(oldPullableComp))
                    {
                        return false;
                    }
                }
                else
                {
                    Logger.WarningS("c.go.c.pulling", "Well now you've done it, haven't you? Someone transferred pulling (onto {0}) while presently pulling something that has no Pullable component (on {1})!", pullable.Owner, oldPullable);
                    return false;
                }
            }

            // Ensure that the pullable is not currently being pulled.
            // Same sort of reasons as before.

            var oldPuller = pullable.Puller;
            if (oldPuller != null)
            {
                if (!TryStopPull(pullable))
                {
                    return false;
                }
            }

            // Continue with pulling process.

            var pullAttempt = new PullAttemptEvent(pullerPhysics, pullablePhysics);

            RaiseLocalEvent(puller.Owner, pullAttempt, broadcast: false);

            if (pullAttempt.Cancelled)
            {
                return false;
            }

            RaiseLocalEvent(pullable.Owner, pullAttempt, true);

            if (pullAttempt.Cancelled)
                return false;

            _pullSm.ForceRelationship(puller, pullable);
            pullable.PrevFixedRotation = pullablePhysics.FixedRotation;
            pullablePhysics.FixedRotation = pullable.FixedRotationOnPull;
            return true;
        }

        public bool TryMoveTo(SharedPullableComponent pullable, EntityCoordinates to)
        {
            if (pullable.Puller == null)
            {
                return false;
            }

            if (!EntityManager.HasComponent<PhysicsComponent>(pullable.Owner))
            {
                return false;
            }

            _pullSm.ForceSetMovingTo(pullable, to);
            return true;
        }

        public void StopMoveTo(SharedPullableComponent pullable)
        {
            _pullSm.ForceSetMovingTo(pullable, null);
        }
    }
}
