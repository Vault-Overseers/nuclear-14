using Content.Shared.StepTrigger.Components;
using Robust.Shared.Collections;
using Robust.Shared.GameStates;
using Robust.Shared.Physics.Dynamics;

namespace Content.Shared.StepTrigger.Systems;

public sealed class StepTriggerSystem : EntitySystem
{
    [Dependency] private readonly EntityLookupSystem _entityLookup = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<StepTriggerComponent, ComponentGetState>(TriggerGetState);
        SubscribeLocalEvent<StepTriggerComponent, ComponentHandleState>(TriggerHandleState);

        SubscribeLocalEvent<StepTriggerComponent, StartCollideEvent>(HandleCollide);
    }

    public override void Update(float frameTime)
    {
        var query = GetEntityQuery<PhysicsComponent>();
        foreach (var (active, trigger, transform) in EntityQuery<StepTriggerActiveComponent, StepTriggerComponent, TransformComponent>())
        {
            if (!Update(trigger, transform, query))
                continue;

            RemComp(trigger.Owner, active);
        }
    }

    private bool Update(StepTriggerComponent component, TransformComponent transform, EntityQuery<PhysicsComponent> query)
    {
        if (!component.Active ||
            component.Colliding.Count == 0)
            return true;

        var remQueue = new ValueList<EntityUid>();
        foreach (var otherUid in component.Colliding)
        {
            var shouldRemoveFromColliding = UpdateColliding(component, transform, otherUid, query);
            if (!shouldRemoveFromColliding)
                continue;

            remQueue.Add(otherUid);
        }

        if (remQueue.Count > 0)
        {
            foreach (var uid in remQueue)
            {
                component.Colliding.Remove(uid);
                component.CurrentlySteppedOn.Remove(uid);
            }

            Dirty(component);
        }

        return false;
    }

    private bool UpdateColliding(StepTriggerComponent component, TransformComponent ownerTransform, EntityUid otherUid, EntityQuery<PhysicsComponent> query)
    {
        if (!query.TryGetComponent(otherUid, out var otherPhysics))
            return true;

        // TODO: This shouldn't be calculating based on world AABBs.
        var ourAabb = _entityLookup.GetWorldAABB(component.Owner, ownerTransform);
        var otherAabb = _entityLookup.GetWorldAABB(otherUid);

        if (!ourAabb.Intersects(otherAabb))
            return true;

        if (otherPhysics.LinearVelocity.Length < component.RequiredTriggerSpeed
            || component.CurrentlySteppedOn.Contains(otherUid)
            || otherAabb.IntersectPercentage(ourAabb) < component.IntersectRatio
            || !CanTrigger(component.Owner, otherUid, component))
            return false;

        var ev = new StepTriggeredEvent { Source = component.Owner, Tripper = otherUid };
        RaiseLocalEvent(component.Owner, ref ev, true);

        component.CurrentlySteppedOn.Add(otherUid);
        Dirty(component);
        return false;
    }

    private bool CanTrigger(EntityUid uid, EntityUid otherUid, StepTriggerComponent component)
    {
        if (!component.Active || component.CurrentlySteppedOn.Contains(otherUid))
            return false;

        var msg = new StepTriggerAttemptEvent { Source = uid, Tripper = otherUid };

        RaiseLocalEvent(uid, ref msg, true);

        return msg.Continue && !msg.Cancelled;
    }

    private void HandleCollide(EntityUid uid, StepTriggerComponent component, StartCollideEvent args)
    {
        var otherUid = args.OtherFixture.Body.Owner;

        if (!CanTrigger(uid, otherUid, component))
            return;

        EnsureComp<StepTriggerActiveComponent>(uid);

        component.Colliding.Add(otherUid);
    }

    private static void TriggerHandleState(EntityUid uid, StepTriggerComponent component, ref ComponentHandleState args)
    {
        if (args.Current is not StepTriggerComponentState state)
            return;

        component.RequiredTriggerSpeed = state.RequiredTriggerSpeed;
        component.IntersectRatio = state.IntersectRatio;
        component.Active = state.Active;

        component.CurrentlySteppedOn.Clear();
        component.Colliding.Clear();

        component.CurrentlySteppedOn.UnionWith(state.CurrentlySteppedOn);
        component.Colliding.UnionWith(state.Colliding);
    }

    private static void TriggerGetState(EntityUid uid, StepTriggerComponent component, ref ComponentGetState args)
    {
        args.State = new StepTriggerComponentState(
            component.IntersectRatio,
            component.CurrentlySteppedOn,
            component.Colliding,
            component.RequiredTriggerSpeed,
            component.Active);
    }

    public void SetIntersectRatio(EntityUid uid, float ratio, StepTriggerComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return;

        if (MathHelper.CloseToPercent(component.IntersectRatio, ratio))
            return;

        component.IntersectRatio = ratio;
        Dirty(component);
    }

    public void SetRequiredTriggerSpeed(EntityUid uid, float speed, StepTriggerComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return;

        if (MathHelper.CloseToPercent(component.RequiredTriggerSpeed, speed))
            return;

        component.RequiredTriggerSpeed = speed;
        Dirty(component);
    }

    public void SetActive(EntityUid uid, bool active, StepTriggerComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return;

        if (active == component.Active)
            return;

        component.Active = active;
        Dirty(component);
    }
}

[ByRefEvent]
public struct StepTriggerAttemptEvent
{
    public EntityUid Source;
    public EntityUid Tripper;
    public bool Continue;
    /// <summary>
    ///     Set by systems which wish to cancel the step trigger event, regardless of event ordering.
    /// </summary>
    public bool Cancelled;
}

[ByRefEvent]
public struct StepTriggeredEvent
{
    public EntityUid Source;
    public EntityUid Tripper;
}
