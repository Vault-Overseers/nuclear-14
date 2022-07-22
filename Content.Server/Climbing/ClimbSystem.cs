using Content.Server.Climbing.Components;
using Content.Server.DoAfter;
using Content.Server.Popups;
using Content.Server.Stunnable;
using Content.Shared.ActionBlocker;
using Content.Shared.Body.Components;
using Content.Shared.Body.Part;
using Content.Shared.Buckle.Components;
using Content.Shared.Climbing;
using Content.Shared.Damage;
using Content.Shared.DragDrop;
using Content.Shared.GameTicking;
using Content.Shared.IdentityManagement;
using Content.Shared.Interaction;
using Content.Shared.Physics;
using Content.Shared.Popups;
using Content.Shared.Verbs;
using JetBrains.Annotations;
using Robust.Shared.GameStates;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Collision.Shapes;
using Robust.Shared.Physics.Dynamics;
using Robust.Shared.Player;

namespace Content.Server.Climbing;

[UsedImplicitly]
public sealed class ClimbSystem : SharedClimbSystem
{
    [Dependency] private readonly ActionBlockerSystem _actionBlockerSystem = default!;
    [Dependency] private readonly DamageableSystem _damageableSystem = default!;
    [Dependency] private readonly DoAfterSystem _doAfterSystem = default!;
    [Dependency] private readonly FixtureSystem _fixtureSystem = default!;
    [Dependency] private readonly PopupSystem _popupSystem = default!;
    [Dependency] private readonly SharedInteractionSystem _interactionSystem = default!;
    [Dependency] private readonly StunSystem _stunSystem = default!;

    private const string ClimbingFixtureName = "climb";
    private const int ClimbingCollisionGroup = (int) (CollisionGroup.TableLayer | CollisionGroup.LowImpassable);

    private readonly Dictionary<EntityUid, List<Fixture>> _fixtureRemoveQueue = new();

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<RoundRestartCleanupEvent>(Reset);
        SubscribeLocalEvent<ClimbableComponent, GetVerbsEvent<AlternativeVerb>>(AddClimbableVerb);
        SubscribeLocalEvent<ClimbableComponent, DragDropEvent>(OnClimbableDragDrop);

        SubscribeLocalEvent<ClimbingComponent, ClimbFinishedEvent>(OnClimbFinished);
        SubscribeLocalEvent<ClimbingComponent, EndCollideEvent>(OnClimbEndCollide);
        SubscribeLocalEvent<ClimbingComponent, BuckleChangeEvent>(OnBuckleChange);
        SubscribeLocalEvent<ClimbingComponent, ComponentGetState>(OnClimbingGetState);

        SubscribeLocalEvent<GlassTableComponent, ClimbedOnEvent>(OnGlassClimbed);
    }

    protected override void OnCanDragDropOn(EntityUid uid, SharedClimbableComponent component, CanDragDropOnEvent args)
    {
        base.OnCanDragDropOn(uid, component, args);

        if (!args.CanDrop)
            return;

        string reason;
        var canVault = args.User == args.Dragged
            ? CanVault(component, args.User, args.Target, out reason)
            : CanVault(component, args.User, args.Dragged, args.Target, out reason);

        if (!canVault)
            _popupSystem.PopupEntity(reason, args.User, Filter.Entities(args.User));

        args.CanDrop = canVault;
        args.Handled = true;
    }

    private void AddClimbableVerb(EntityUid uid, ClimbableComponent component, GetVerbsEvent<AlternativeVerb> args)
    {
        if (!args.CanAccess || !args.CanInteract || !_actionBlockerSystem.CanMove(args.User))
            return;

        if (!TryComp(args.User, out ClimbingComponent? climbingComponent) || climbingComponent.IsClimbing)
            return;

        // TODO VERBS ICON add a climbing icon?
        args.Verbs.Add(new AlternativeVerb
        {
            Act = () => TryMoveEntity(component, args.User, args.User, args.Target),
            Text = Loc.GetString("comp-climbable-verb-climb")
        });
    }

    private void OnClimbableDragDrop(EntityUid uid, ClimbableComponent component, DragDropEvent args)
    {
        TryMoveEntity(component, args.User, args.Dragged, args.Target);
    }

    private void TryMoveEntity(ClimbableComponent component, EntityUid user, EntityUid entityToMove,
        EntityUid climbable)
    {
        if (!TryComp(entityToMove, out ClimbingComponent? climbingComponent) || climbingComponent.IsClimbing)
            return;

        _doAfterSystem.DoAfter(new DoAfterEventArgs(entityToMove, component.ClimbDelay, default, climbable)
        {
            BreakOnTargetMove = true,
            BreakOnUserMove = true,
            BreakOnDamage = true,
            BreakOnStun = true,
            UserFinishedEvent = new ClimbFinishedEvent(user, climbable)
        });
    }

    private void OnClimbFinished(EntityUid uid, ClimbingComponent climbing, ClimbFinishedEvent args)
    {
        Climb(uid, args.User, args.Climbable, climbing: climbing);
    }

    private void Climb(EntityUid uid, EntityUid user, EntityUid climbable, bool silent = false, ClimbingComponent? climbing = null,
        PhysicsComponent? physics = null, FixturesComponent? fixtures = null)
    {
        if (!Resolve(uid, ref climbing, ref physics, ref fixtures, false))
            return;

        if (!ReplaceFixtures(climbing, physics, fixtures))
            return;

        climbing.IsClimbing = true;

        MoveEntityToward(uid, climbable, physics, climbing);
        // we may potentially need additional logic since we're forcing a player onto a climbable
        // there's also the cases where the user might collide with the person they are forcing onto the climbable that i haven't accounted for

        RaiseLocalEvent(uid, new StartClimbEvent(climbable), false);
        RaiseLocalEvent(climbable, new ClimbedOnEvent(uid), false);

        if (silent)
            return;
        if (user == uid)
        {
            var othersMessage = Loc.GetString("comp-climbable-user-climbs-other", ("user", Identity.Entity(uid, EntityManager)),
                ("climbable", climbable));
            uid.PopupMessageOtherClients(othersMessage);

            var selfMessage = Loc.GetString("comp-climbable-user-climbs", ("climbable", climbable));
            uid.PopupMessage(selfMessage);
        }
        else
        {
            var othersMessage = Loc.GetString("comp-climbable-user-climbs-force-other", ("user", Identity.Entity(user, EntityManager)),
                ("moved-user", Identity.Entity(uid, EntityManager)), ("climbable", climbable));
            user.PopupMessageOtherClients(othersMessage);

            var selfMessage = Loc.GetString("comp-climbable-user-climbs-force", ("moved-user", Identity.Entity(uid, EntityManager)),
                ("climbable", climbable));
            user.PopupMessage(selfMessage);
        }
    }

    /// <summary>
    /// Replaces the current fixtures with non-climbing collidable versions so that climb end can be detected
    /// </summary>
    /// <returns>Returns whether adding the new fixtures was successful</returns>
    private bool ReplaceFixtures(ClimbingComponent climbingComp, PhysicsComponent physicsComp, FixturesComponent fixturesComp)
    {
        // Swap fixtures
        foreach (var (name, fixture) in fixturesComp.Fixtures)
        {
            if (climbingComp.DisabledFixtureMasks.ContainsKey(name)
                || fixture.Hard == false
                || (fixture.CollisionMask & ClimbingCollisionGroup) == 0)
                continue;

            climbingComp.DisabledFixtureMasks.Add(fixture.ID, fixture.CollisionMask & ClimbingCollisionGroup);
            fixture.CollisionMask &= ~ClimbingCollisionGroup;
        }

        if (!_fixtureSystem.TryCreateFixture(physicsComp,
            new Fixture(new PhysShapeCircle { Radius = 0.35f }, (int) CollisionGroup.None, ClimbingCollisionGroup, false)
                {ID = ClimbingFixtureName}, manager: fixturesComp))
            return false;
        return true;
    }

    private void OnClimbEndCollide(EntityUid uid, ClimbingComponent component, EndCollideEvent args)
    {
        if (args.OurFixture.ID != ClimbingFixtureName
            || !component.IsClimbing
            || component.OwnerIsTransitioning)
            return;

        foreach (var fixture in args.OurFixture.Contacts.Keys)
        {
            if (fixture == args.OtherFixture)
                continue;
            // If still colliding with a climbable, do not stop climbing
            if (HasComp<ClimbableComponent>(fixture.Body.Owner))
                return;
        }

        StopClimb(uid, component);
    }

    private void StopClimb(EntityUid uid, ClimbingComponent? climbing = null, FixturesComponent? fixtures = null)
    {
        if (!Resolve(uid, ref climbing, ref fixtures, false))
            return;

        foreach (var (name, fixtureMask) in climbing.DisabledFixtureMasks)
        {
            if (!fixtures.Fixtures.TryGetValue(name, out var fixture))
                continue;
            fixture.CollisionMask |= fixtureMask;
        }
        climbing.DisabledFixtureMasks.Clear();

        if (!_fixtureRemoveQueue.TryGetValue(uid, out var removeQueue))
        {
            removeQueue = new List<Fixture>();
            _fixtureRemoveQueue.Add(uid, removeQueue);
        }

        if (fixtures.Fixtures.TryGetValue(ClimbingFixtureName, out var climbingFixture))
            removeQueue.Add(climbingFixture);

        climbing.IsClimbing = false;
        climbing.OwnerIsTransitioning = false;
    }

    /// <summary>
    ///     Checks if the user can vault the target
    /// </summary>
    /// <param name="component">The component of the entity that is being vaulted</param>
    /// <param name="user">The entity that wants to vault</param>
    /// <param name="target">The object that is being vaulted</param>
    /// <param name="reason">The reason why it cant be dropped</param>
    /// <returns></returns>
    private bool CanVault(SharedClimbableComponent component, EntityUid user, EntityUid target, out string reason)
    {
        if (!_actionBlockerSystem.CanInteract(user, target))
        {
            reason = Loc.GetString("comp-climbable-cant-interact");
            return false;
        }

        if (!HasComp<ClimbingComponent>(user)
            || !TryComp(user, out SharedBodyComponent? body)
            || !body.HasPartOfType(BodyPartType.Leg)
            || !body.HasPartOfType(BodyPartType.Foot))
        {
            reason = Loc.GetString("comp-climbable-cant-climb");
            return false;
        }

        if (!_interactionSystem.InRangeUnobstructed(user, target, component.Range))
        {
            reason = Loc.GetString("comp-climbable-cant-reach");
            return false;
        }

        reason = string.Empty;
        return true;
    }

    /// <summary>
    ///     Checks if the user can vault the dragged entity onto the the target
    /// </summary>
    /// <param name="component">The climbable component of the object being vaulted onto</param>
    /// <param name="user">The user that wants to vault the entity</param>
    /// <param name="dragged">The entity that is being vaulted</param>
    /// <param name="target">The object that is being vaulted onto</param>
    /// <param name="reason">The reason why it cant be dropped</param>
    /// <returns></returns>
    private bool CanVault(SharedClimbableComponent component, EntityUid user, EntityUid dragged, EntityUid target,
        out string reason)
    {
        if (!_actionBlockerSystem.CanInteract(user, dragged) || !_actionBlockerSystem.CanInteract(user, target))
        {
            reason = Loc.GetString("comp-climbable-cant-interact");
            return false;
        }

        if (!HasComp<ClimbingComponent>(dragged))
        {
            reason = Loc.GetString("comp-climbable-cant-climb");
            return false;
        }

        bool Ignored(EntityUid entity) => entity == target || entity == user || entity == dragged;

        if (!_interactionSystem.InRangeUnobstructed(user, target, component.Range, predicate: Ignored)
            || !_interactionSystem.InRangeUnobstructed(user, dragged, component.Range, predicate: Ignored))
        {
            reason = Loc.GetString("comp-climbable-cant-reach");
            return false;
        }

        reason = string.Empty;
        return true;
    }

    public void ForciblySetClimbing(EntityUid uid, EntityUid climbable, ClimbingComponent? component = null)
    {
        Climb(uid, uid, climbable, true, component);
    }

    private void OnBuckleChange(EntityUid uid, ClimbingComponent component, BuckleChangeEvent args)
    {
        if (!args.Buckling)
            return;
        StopClimb(uid, component);
    }

    private static void OnClimbingGetState(EntityUid uid, ClimbingComponent component, ref ComponentGetState args)
    {
        args.State = new SharedClimbingComponent.ClimbModeComponentState(component.IsClimbing, component.OwnerIsTransitioning);
    }

    private void OnGlassClimbed(EntityUid uid, GlassTableComponent component, ClimbedOnEvent args)
    {
        if (TryComp<PhysicsComponent>(args.Climber, out var physics) && physics.Mass <= component.MassLimit)
            return;

        _damageableSystem.TryChangeDamage(args.Climber, component.ClimberDamage);
        _damageableSystem.TryChangeDamage(uid, component.TableDamage);
        _stunSystem.TryParalyze(args.Climber, TimeSpan.FromSeconds(component.StunTime), true);

        // Not shown to the user, since they already get a 'you climb on the glass table' popup
        _popupSystem.PopupEntity(
            Loc.GetString("glass-table-shattered-others", ("table", uid), ("climber", Identity.Entity(args.Climber, EntityManager))), args.Climber,
            Filter.Pvs(uid).RemoveWhereAttachedEntity(puid => puid == args.Climber));
    }

    /// <summary>
    /// Moves the entity toward the target climbed entity
    /// </summary>
    public void MoveEntityToward(EntityUid uid, EntityUid target, PhysicsComponent? physics = null, ClimbingComponent? climbing = null)
    {
        if (!Resolve(uid, ref physics, ref climbing, false))
            return;

        var from = Transform(uid).WorldPosition;
        var to = Transform(target).WorldPosition;
        var (x, y) = (to - from).Normalized;

        if (MathF.Abs(x) < 0.6f) // user climbed mostly vertically so lets make it a clean straight line
            to = new Vector2(from.X, to.Y);
        else if (MathF.Abs(y) < 0.6f) // user climbed mostly horizontally so lets make it a clean straight line
            to = new Vector2(to.X, from.Y);

        var velocity = (to - from).Length;

        if (velocity <= 0.0f) return;

        // Since there are bodies with different masses:
        // mass * 10 seems enough to move entity
        // instead of launching cats like rockets against the walls with constant impulse value.
        physics.ApplyLinearImpulse((to - from).Normalized * velocity * physics.Mass * 10);
        physics.BodyType = BodyType.Dynamic;
        climbing.OwnerIsTransitioning = true;
        _actionBlockerSystem.UpdateCanMove(uid);

        // Transition back to KinematicController after BufferTime
        climbing.Owner.SpawnTimer((int) (SharedClimbingComponent.BufferTime * 1000), () =>
        {
            if (climbing.Deleted) return;
            physics.BodyType = BodyType.KinematicController;
            climbing.OwnerIsTransitioning = false;
            _actionBlockerSystem.UpdateCanMove(uid);
        });
    }

    public override void Update(float frameTime)
    {
        foreach (var (uid, fixtures) in _fixtureRemoveQueue)
        {
            if (!TryComp<PhysicsComponent>(uid, out var physicsComp)
                || !TryComp<FixturesComponent>(uid, out var fixturesComp))
                continue;
            foreach (var fixture in fixtures)
            {
                _fixtureSystem.DestroyFixture(physicsComp, fixture, true, fixturesComp);
            }
        }

        _fixtureRemoveQueue.Clear();
    }

    private void Reset(RoundRestartCleanupEvent ev)
    {
        _fixtureRemoveQueue.Clear();
    }
}

internal sealed class ClimbFinishedEvent : EntityEventArgs
{
    public ClimbFinishedEvent(EntityUid user, EntityUid climbable)
    {
        User = user;
        Climbable = climbable;
    }

    public EntityUid User { get; }
    public EntityUid Climbable { get; }
}

/// <summary>
///     Raised on an entity when it is climbed on.
/// </summary>
public sealed class ClimbedOnEvent : EntityEventArgs
{
    public EntityUid Climber;

    public ClimbedOnEvent(EntityUid climber)
    {
        Climber = climber;
    }
}

/// <summary>
///     Raised on an entity when it successfully climbs on something.
/// </summary>
public sealed class StartClimbEvent : EntityEventArgs
{
    public EntityUid Climbable;

    public StartClimbEvent(EntityUid climbable)
    {
        Climbable = climbable;
    }
}
