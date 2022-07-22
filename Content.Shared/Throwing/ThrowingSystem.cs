using Content.Shared.Interaction;
using Content.Shared.Movement.Components;
using Content.Shared.Tag;
using Robust.Shared.Physics;
using Robust.Shared.Timing;

namespace Content.Shared.Throwing;

public sealed class ThrowingSystem : EntitySystem
{
    public const float ThrowAngularImpulse = 1.5f;

    /// <summary>
    /// The minimum amount of time an entity needs to be thrown before the timer can be run.
    /// Anything below this threshold never enters the air.
    /// </summary>
    public const float FlyTime = 0.15f;

    [Dependency] private readonly SharedInteractionSystem _interactionSystem = default!;
    [Dependency] private readonly ThrownItemSystem _thrownSystem = default!;
    [Dependency] private readonly TagSystem _tagSystem = default!;

    /// <summary>
    ///     Tries to throw the entity if it has a physics component, otherwise does nothing.
    /// </summary>
    /// <param name="entity">The entity being thrown.</param>
    /// <param name="direction">A vector pointing from the entity to its destination.</param>
    /// <param name="strength">How much the direction vector should be multiplied for velocity.</param>
    /// <param name="user"></param>
    /// <param name="pushbackRatio">The ratio of impulse applied to the thrower - defaults to 10 because otherwise it's not enough to properly recover from getting spaced</param>
    public void TryThrow(
        EntityUid uid,
        Vector2 direction,
        float strength = 1.0f,
        EntityUid? user = null,
        float pushbackRatio = 5.0f,
        PhysicsComponent? physics = null,
        TransformComponent? transform = null,
        EntityQuery<PhysicsComponent>? physicsQuery = null,
        EntityQuery<TransformComponent>? xformQuery = null)
    {
        if (strength <= 0 || direction == Vector2.Infinity || direction == Vector2.NaN || direction == Vector2.Zero)
            return;

        physicsQuery ??= GetEntityQuery<PhysicsComponent>();
        if (physics == null && !physicsQuery.Value.TryGetComponent(uid, out physics))
            return;

        if ((physics.BodyType & (BodyType.Dynamic | BodyType.KinematicController)) == 0x0)
        {
            Logger.Warning($"Tried to throw entity {ToPrettyString(uid)} but can't throw {physics.BodyType} bodies!");
            return;
        }

        var comp = EnsureComp<ThrownItemComponent>(uid);
        comp.Thrower = user;
        // Give it a l'il spin.
        if (!_tagSystem.HasTag(uid, "NoSpinOnThrow"))
            physics.ApplyAngularImpulse(ThrowAngularImpulse);
        else
        {
            if (transform == null)
            {
                xformQuery ??= GetEntityQuery<TransformComponent>();
                transform = xformQuery.Value.GetComponent(uid);
            }
            transform.LocalRotation = direction.ToWorldAngle() - Math.PI;
        }

        if (user != null)
            _interactionSystem.ThrownInteraction(user.Value, uid);

        var impulseVector = direction.Normalized * strength * physics.Mass;
        physics.ApplyLinearImpulse(impulseVector);

        // Estimate time to arrival so we can apply OnGround status and slow it much faster.
        var time = (direction / strength).Length;

        if (time < FlyTime)
        {
            physics.BodyStatus = BodyStatus.OnGround;
            _thrownSystem.LandComponent(comp);
        }
        else
        {
            physics.BodyStatus = BodyStatus.InAir;

            Timer.Spawn(TimeSpan.FromSeconds(time - FlyTime), () =>
            {
                if (physics.Deleted) return;
                physics.BodyStatus = BodyStatus.OnGround;
                _thrownSystem.LandComponent(comp);
            });
        }

        // Give thrower an impulse in the other direction
        if (user != null &&
            pushbackRatio > 0.0f &&
            physicsQuery.Value.TryGetComponent(user.Value, out var userPhysics) &&
            user.Value.IsWeightless(userPhysics, entityManager: EntityManager))
        {
            var msg = new ThrowPushbackAttemptEvent();
            RaiseLocalEvent(physics.Owner, msg, false);

            if (!msg.Cancelled)
                userPhysics.ApplyLinearImpulse(-impulseVector * pushbackRatio);
        }
    }
}
