using System.Numerics;
using Content.KayMisaZlevels.Shared.Miscellaneous;
using Content.KayMisaZlevels.Shared.Systems;
using Content.Shared.ActionBlocker;
using Content.Shared.Body.Systems;
using Content.Shared.Buckle.Components;
using Content.Shared.Climbing.Components;
using Content.Shared.Climbing.Events;
using Content.Shared.Damage;
using Content.Shared.DoAfter;
using Content.Shared.DragDrop;
using Content.Shared.Hands.Components;
using Content.Shared.IdentityManagement;
using Content.Shared.Interaction;
using Content.Shared.Maps;
using Content.Shared.Movement.Components;
using Content.Shared.Movement.Events;
using Content.Shared.Physics;
using Content.Shared.Popups;
using Content.Shared.Standing;
using Content.Shared.Stunnable;
using Content.Shared.Tag;
using Content.Shared.Traits.Assorted.Components;
using Content.Shared.Verbs;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Map;
using Robust.Shared.Network;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Collision.Shapes;
using Robust.Shared.Physics.Components;
using Robust.Shared.Physics.Controllers;
using Robust.Shared.Physics.Events;
using Robust.Shared.Physics.Systems;
using Robust.Shared.Player;
using Robust.Shared.Serialization;
using Robust.Shared.Timing;
using Robust.Shared.GameObjects;

namespace Content.Shared.Climbing.Systems;

public sealed partial class ClimbSystem : VirtualController
{
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly IMapManager _mapManager = default!;
    [Dependency] private readonly INetManager _netManager = default!;
    [Dependency] private readonly ActionBlockerSystem _actionBlockerSystem = default!;
    [Dependency] private readonly DamageableSystem _damageableSystem = default!;
    [Dependency] private readonly FixtureSystem _fixtureSystem = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfterSystem = default!;
    [Dependency] private readonly SharedInteractionSystem _interactionSystem = default!;
    [Dependency] private readonly SharedPopupSystem _popupSystem = default!;
    [Dependency] private readonly SharedPhysicsSystem _physics = default!;
    [Dependency] private readonly SharedStunSystem _stunSystem = default!;
    [Dependency] private readonly SharedTransformSystem _xformSystem = default!;
    [Dependency] private readonly IEntitySystemManager _sysMan = default!;
    private SharedZStackSystem? _zStack;
    [Dependency] private readonly SharedMapSystem _mapSys = default!;
    [Dependency] private readonly EntityLookupSystem _lookup = default!;
    [Dependency] private readonly TagSystem _tag = default!;


    private const string ClimbingFixtureName = "climb";
    private const int ClimbingCollisionGroup = (int) (CollisionGroup.TableLayer | CollisionGroup.LowImpassable);

    private const string WallTag = "Wall";

    private EntityQuery<FixturesComponent> _fixturesQuery;
    private EntityQuery<TransformComponent> _xformQuery;
    private EntityQuery<ClimbableComponent> _climbableQuery;

    public override void Initialize()
    {
        base.Initialize();

        _sysMan.TryGetEntitySystem(out _zStack);

        _fixturesQuery = GetEntityQuery<FixturesComponent>();
        _xformQuery = GetEntityQuery<TransformComponent>();

        SubscribeLocalEvent<ClimbingComponent, UpdateCanMoveEvent>(OnMoveAttempt);
        SubscribeLocalEvent<ClimbingComponent, EntParentChangedMessage>(OnParentChange);
        SubscribeLocalEvent<ClimbingComponent, ClimbDoAfterEvent>(OnDoAfter);
        SubscribeLocalEvent<ClimbingComponent, EndCollideEvent>(OnClimbEndCollide);
        SubscribeLocalEvent<ClimbingComponent, BuckledEvent>(OnBuckled);

        SubscribeLocalEvent<ClimbableComponent, CanDropTargetEvent>(OnCanDragDropOn);
        SubscribeLocalEvent<ClimbableComponent, GetVerbsEvent<AlternativeVerb>>(AddClimbableVerb);
        SubscribeLocalEvent<ClimbableComponent, DragDropTargetEvent>(OnClimbableDragDrop);
        SubscribeLocalEvent<ClimbableComponent, AttemptClimbEvent>(OnAttemtClimb);

        SubscribeLocalEvent<GlassTableComponent, ClimbedOnEvent>(OnGlassClimbed);
    }

    public override void UpdateBeforeSolve(bool prediction, float frameTime)
    {
        base.UpdateBeforeSolve(prediction, frameTime);

        var query = EntityQueryEnumerator<ClimbingComponent>();
        var curTime = _timing.CurTime;

        // Move anything still climb in the specified direction.
        while (query.MoveNext(out var uid, out var comp))
        {
            if (comp.NextTransition == null)
                continue;

            if (comp.NextTransition < curTime)
            {
                FinishTransition(uid, comp);
                continue;
            }

            var xform = _xformQuery.GetComponent(uid);
            _xformSystem.SetLocalPosition(uid, xform.LocalPosition + comp.Direction * frameTime, xform);
        }
    }

    private void FinishTransition(EntityUid uid, ClimbingComponent comp)
    {
        // TODO: Validate climb here
        comp.NextTransition = null;
        _actionBlockerSystem.UpdateCanMove(uid);
        Dirty(uid, comp);

        // Stop if necessary.
        if (!_fixturesQuery.TryGetComponent(uid, out var fixtures) ||
            !IsClimbing(uid, fixtures))
        {
            StopClimb(uid, comp);
            return;
        }
    }

    /// <summary>
    /// Returns true if entity currently has a valid vault.
    /// </summary>
    private bool IsClimbing(EntityUid uid, FixturesComponent? fixturesComp = null)
    {
        if (!_fixturesQuery.Resolve(uid, ref fixturesComp) || !fixturesComp.Fixtures.TryGetValue(ClimbingFixtureName, out var climbFixture))
            return false;

        foreach (var contact in climbFixture.Contacts.Values)
        {
            var other = uid == contact.EntityA ? contact.EntityB : contact.EntityA;

            if (HasComp<ClimbableComponent>(other))
            {
                return true;
            }
        }

        return false;
    }

    private void OnMoveAttempt(EntityUid uid, ClimbingComponent component, UpdateCanMoveEvent args)
    {
        // Can't move when transition.
        if (component.NextTransition != null)
            args.Cancel();
    }

    private void OnParentChange(EntityUid uid, ClimbingComponent component, ref EntParentChangedMessage args)
    {
        if (component.NextTransition != null)
        {
            FinishTransition(uid, component);
        }
    }

    private void OnCanDragDropOn(EntityUid uid, ClimbableComponent component, ref CanDropTargetEvent args)
    {
        if (args.Handled)
            return;

        var canVault = args.User == args.Dragged
            ? CanVault(component, args.User, uid, out _)
            : CanVault(component, args.User, args.Dragged, uid, out _);

        args.CanDrop = canVault;

        if (!HasComp<HandsComponent>(args.User))
            args.CanDrop = false;

        args.Handled = true;
    }

    private void AddClimbableVerb(EntityUid uid, ClimbableComponent component, GetVerbsEvent<AlternativeVerb> args)
    {
        if (!args.CanAccess || !args.CanInteract || !_actionBlockerSystem.CanMove(args.User))
            return;

        if (!TryComp(args.User, out ClimbingComponent? climbingComponent) || climbingComponent.IsClimbing || !climbingComponent.CanClimb)
            return;

        // TODO VERBS ICON add a climbing icon?
        args.Verbs.Add(new AlternativeVerb
        {
            Act = () => TryClimb(args.User, args.User, args.Target, out _, component),
            Text = Loc.GetString("comp-climbable-verb-climb")
        });
    }

    private void OnClimbableDragDrop(EntityUid uid, ClimbableComponent component, ref DragDropTargetEvent args)
    {
        if (args.Handled)
            return;

        TryClimb(args.User, args.Dragged, uid, out _, component);
    }

    public bool TryClimb(
        EntityUid user,
        EntityUid entityToMove,
        EntityUid climbable,
        out DoAfterId? id,
        ClimbableComponent? comp = null,
        ClimbingComponent? climbing = null)
    {
        id = null;

        if (!Resolve(climbable, ref comp) || !Resolve(entityToMove, ref climbing, false))
            return false;

        var canVault = user == entityToMove
             ? CanVault(comp, user, climbable, out var reason)
             : CanVault(comp, user, entityToMove, climbable, out reason);
        if (!canVault)
        {
            _popupSystem.PopupClient(reason, user, user);
            return false;
        }

        // Note, IsClimbing does not mean a DoAfter is active, it means the target has already finished a DoAfter and
        // is currently on top of something..
        if (climbing.IsClimbing)
            return true;

        // Should be cleared, before a new define for the descend coordinates
        climbing.IgnoreSkillCheck = false;
        climbing.DescendCoords = null;

        var ev = new AttemptClimbEvent(user, entityToMove, climbable);
        RaiseLocalEvent(climbable, ref ev);
        if (ev.Cancelled)
            return false;

        var climbDelay = comp.ClimbDelay;
        if (user == entityToMove && TryComp<ClimbDelayModifierComponent>(user, out var delayModifier))
            climbDelay *= delayModifier.ClimbDelayMultiplier;

        var args = new DoAfterArgs(EntityManager, user, climbDelay, new ClimbDoAfterEvent(),
            entityToMove,
            target: climbable,
            used: entityToMove)
        {
            BreakOnMove = true,
            BreakOnDamage = true,
            DuplicateCondition = DuplicateConditions.SameTool | DuplicateConditions.SameTarget
        };

        _audio.PlayPredicted(comp.StartClimbSound, climbable, user);
        return _doAfterSystem.TryStartDoAfter(args, out id);
    }

    private void OnDoAfter(EntityUid uid, ClimbingComponent component, ClimbDoAfterEvent args)
    {
        if (args.Handled || args.Cancelled || args.Args.Target == null || args.Args.Used == null)
            return;

        // Try to move player between Z levels, if descend coords is not null
        if (component.DescendCoords is not null && _netManager.IsServer)
        {
            DescendedClimb(uid, args.Args.Used.Value, args.Args.Target.Value, climbing: component);
            args.Handled = true;
            return;
        }

        Climb(uid, args.Args.User, args.Args.Target.Value, climbing: component);
        args.Handled = true;
    }

    private void SendDescendFailureMessage(EntityUid uid, EntityUid user, EntityUid climbable)
    {
        string selfMessage;

        if (user == uid)
        {
            selfMessage = Loc.GetString("comp-climbable-user-climbs-failure", ("climbable", climbable));
        }
        else
        {
            selfMessage = Loc.GetString("comp-climbable-user-climbs-failure-force", ("moved-user", Identity.Entity(uid, EntityManager)),
            ("climbable", climbable));
        }

        _popupSystem.PopupEntity(selfMessage, uid, user, PopupType.SmallCaution);
    }

    private void DescendedClimb(EntityUid uid, EntityUid user, EntityUid climbable, bool silent = false, ClimbingComponent? climbing = null,
        PhysicsComponent? physics = null, FixturesComponent? fixtures = null, ClimbableComponent? comp = null)
    {
        if (!Resolve(uid, ref climbing, ref physics, ref fixtures, false))
            return;

        if (!Resolve(climbable, ref comp, false))
            return;

        if (climbing.DescendCoords is null)
            return;

        var selfEvent = new SelfBeforeClimbEvent(uid, user, (climbable, comp));
        RaiseLocalEvent(uid, selfEvent);

        if (selfEvent.Cancelled)
            return;

        var targetEvent = new TargetBeforeClimbEvent(uid, user, (climbable, comp));
        RaiseLocalEvent(climbable, targetEvent);

        if (targetEvent.Cancelled)
            return;

        // Move the entity on new map
        // TODO: Maybe it should be animated or something like that
        _xformSystem.SetCoordinates(user, climbing.DescendCoords.Value);

        climbing.DescendCoords = null;
        Dirty(uid, climbing);

        _audio.PlayPredicted(comp.FinishClimbSound, climbable, user);

        var startEv = new StartClimbEvent(climbable);
        var climbedEv = new ClimbedOnEvent(uid, user);
        RaiseLocalEvent(uid, ref startEv);
        RaiseLocalEvent(climbable, ref climbedEv);

        if (silent)
            return;

        string selfMessage;
        string othersMessage;

        if (user == uid)
        {
            othersMessage = Loc.GetString("comp-climbable-user-climbs-other",
                ("user", Identity.Entity(uid, EntityManager)),
                ("climbable", climbable));

            selfMessage = Loc.GetString("comp-climbable-user-climbs", ("climbable", climbable));
        }
        else
        {
            othersMessage = Loc.GetString("comp-climbable-user-climbs-force-other",
                ("user", Identity.Entity(user, EntityManager)),
                ("moved-user", Identity.Entity(uid, EntityManager)), ("climbable", climbable));

            selfMessage = Loc.GetString("comp-climbable-user-climbs-force", ("moved-user", Identity.Entity(uid, EntityManager)),
                ("climbable", climbable));
        }

        _popupSystem.PopupPredicted(selfMessage, othersMessage, uid, user);
    }

    private void Climb(EntityUid uid, EntityUid user, EntityUid climbable, bool silent = false, ClimbingComponent? climbing = null,
        PhysicsComponent? physics = null, FixturesComponent? fixtures = null, ClimbableComponent? comp = null)
    {
        if (!Resolve(uid, ref climbing, ref physics, ref fixtures, false))
            return;

        if (!Resolve(climbable, ref comp, false))
            return;

        var selfEvent = new SelfBeforeClimbEvent(uid, user, (climbable, comp));
        RaiseLocalEvent(uid, selfEvent);

        if (selfEvent.Cancelled)
            return;

        var targetEvent = new TargetBeforeClimbEvent(uid, user, (climbable, comp));
        RaiseLocalEvent(climbable, targetEvent);

        if (targetEvent.Cancelled)
            return;

        if (!ReplaceFixtures(uid, climbing, fixtures))
            return;

        var xform = _xformQuery.GetComponent(uid);
        var (worldPos, worldRot) = _xformSystem.GetWorldPositionRotation(xform);
        var worldDirection = _xformSystem.GetWorldPosition(climbable) - worldPos;
        var distance = worldDirection.Length();
        var parentRot = worldRot - xform.LocalRotation;
        // Need direction relative to climber's parent.
        var localDirection = (-parentRot).RotateVec(worldDirection);

        // On top of it already so just do it in place.
        if (localDirection.LengthSquared() < 0.01f)
        {
            climbing.NextTransition = null;
        }
        // VirtualController over to the thing.
        else
        {
            var climbDuration = TimeSpan.FromSeconds(distance / climbing.TransitionRate);
            climbing.NextTransition = _timing.CurTime + climbDuration;

            climbing.Direction = localDirection.Normalized() * climbing.TransitionRate;
            _actionBlockerSystem.UpdateCanMove(uid);
        }

        climbing.IsClimbing = true;
        Dirty(uid, climbing);

        _audio.PlayPredicted(comp.FinishClimbSound, climbable, user);

        var startEv = new StartClimbEvent(climbable);
        var climbedEv = new ClimbedOnEvent(uid, user);
        RaiseLocalEvent(uid, ref startEv);
        RaiseLocalEvent(climbable, ref climbedEv);

        if (silent)
            return;

        string selfMessage;
        string othersMessage;

        if (user == uid)
        {
            othersMessage = Loc.GetString("comp-climbable-user-climbs-other",
                ("user", Identity.Entity(uid, EntityManager)),
                ("climbable", climbable));

            selfMessage = Loc.GetString("comp-climbable-user-climbs", ("climbable", climbable));
        }
        else
        {
            othersMessage = Loc.GetString("comp-climbable-user-climbs-force-other",
                ("user", Identity.Entity(user, EntityManager)),
                ("moved-user", Identity.Entity(uid, EntityManager)), ("climbable", climbable));

            selfMessage = Loc.GetString("comp-climbable-user-climbs-force", ("moved-user", Identity.Entity(uid, EntityManager)),
                ("climbable", climbable));
        }

        _popupSystem.PopupPredicted(selfMessage, othersMessage, uid, user);
    }

    /// <summary>
    /// Replaces the current fixtures with non-climbing collidable versions so that climb end can be detected
    /// </summary>
    /// <returns>Returns whether adding the new fixtures was successful</returns>
    private bool ReplaceFixtures(EntityUid uid, ClimbingComponent climbingComp, FixturesComponent fixturesComp)
    {
        // Swap fixtures
        foreach (var (name, fixture) in fixturesComp.Fixtures)
        {
            if (climbingComp.DisabledFixtureMasks.ContainsKey(name)
                || fixture.Hard == false
                || (fixture.CollisionMask & ClimbingCollisionGroup) == 0)
            {
                continue;
            }

            climbingComp.DisabledFixtureMasks.Add(name, fixture.CollisionMask & ClimbingCollisionGroup);
            _physics.SetCollisionMask(uid, name, fixture, fixture.CollisionMask & ~ClimbingCollisionGroup, fixturesComp);
        }

        if (!_fixtureSystem.TryCreateFixture(
                uid,
                new PhysShapeCircle(0.35f),
                ClimbingFixtureName,
                collisionLayer: (int) CollisionGroup.None,
                collisionMask: ClimbingCollisionGroup,
                hard: false,
                manager: fixturesComp))
        {
            return false;
        }

        return true;
    }

    private void OnAttemtClimb(EntityUid uid, ClimbableComponent climbable, ref AttemptClimbEvent args)
    {
        if (!TryComp<ClimbingComponent>(args.Climber, out var climbing) ||
            climbable.DescendDirection is null)
            return;

        var climbableXform = Transform(args.Climbable);

        // If true - set descend coords for Z levels transition
        if (TryGetDescendableCoords(
                args.Climber,
                climbable.DescendDirection.Value,
                out var targetEntityCoords,
                targetPosition: climbableXform.Coordinates.Position,
                comp: climbing,
                ignoreTiles: climbable.IgnoreTiles))
        {
            climbing.IgnoreSkillCheck = climbable.IgnoreSkillCheck;
            climbing.DescendCoords = targetEntityCoords;
        }
        else
        {
            args.Cancelled = true;
        }
    }

    /// <summary>
    ///     Try to check if climber can descend on Z level.
    ///     If result is true, then we can use the out variable of EntityCoordinates.
    /// </summary>
    /// <param name="climber"></param>
    /// <param name="direction"></param>
    /// <param name="descendEntityCoords"></param>
    /// <param name="targetPosition"></param>
    /// <param name="comp"></param>
    /// <param name="ignoreTiles"></param>
    /// <returns></returns>
    public bool TryGetDescendableCoords(
        EntityUid climber,
        ClimbDirection direction,
        out EntityCoordinates? descendEntityCoords,
        Vector2? targetPosition = null,
        ClimbingComponent? comp = null,
        bool ignoreTiles = false)
    {
        descendEntityCoords = null;

        if (!Resolve(climber, ref comp))
            return false;

        var xform = Transform(climber);
        if (xform.MapUid == null)
            return false;

        if (_zStack == null || !_zStack.TryGetZStack(xform.MapUid.Value, out var zStack))
            return false;

        var maps = zStack.Value.Comp.Maps;
        var mapIdx = maps.IndexOf(xform.MapUid.Value);
        int targetMapIdx = -1;
        if (direction == ClimbDirection.Down)
        {
            targetMapIdx = mapIdx - 1;

            // Because there is no bottom levels
            if (targetMapIdx < 0)
                return false;
        }
        else
        {
            targetMapIdx = mapIdx + 1;

            // Because there is no top levels
            if (targetMapIdx >= maps.Count)
                return false;
        }

        if ((TryComp<InputMoverComponent>(climber, out var inputMoverComp) && !inputMoverComp.CanMove) ||
            (TryComp<StandingStateComponent>(climber, out var standingComp) && standingComp.CurrentState < StandingState.Standing))
            return false;

        if (targetPosition is null)
            targetPosition = xform.Coordinates.Position;

        var userTransf = Transform(climber);
        if ((userTransf.WorldPosition - targetPosition.Value).Length() > comp.DescendRange)
            return false;

        descendEntityCoords = new EntityCoordinates(maps[targetMapIdx], targetPosition.Value);

        // If we try to climb on ladders - then there is all okay, we can do it.
        if (ignoreTiles)
            return true;

        // Check grids and another world objects.
        if (direction == ClimbDirection.Down)
        {
            // No grids founded
            if (!_mapManager.TryFindGridAt(maps[mapIdx], targetPosition.Value, out _, out var grid))
                return false;

            // Check if there is a bottom grid.
            if (!_mapManager.TryFindGridAt(maps[targetMapIdx], targetPosition.Value, out var bottomGridUid, out var bottomGrid))
                return false;

            var currentEntityCoords = new EntityCoordinates(maps[mapIdx], targetPosition.Value);
            var currentCoordsPosInt = currentEntityCoords.ToVector2i(EntityManager, _mapManager, _xformSystem);
            var descendCoordsPosInt = descendEntityCoords.Value.ToVector2i(EntityManager, _mapManager, _xformSystem);

            // Check grid on current map.
            _mapSys.TryGetTile(grid, currentCoordsPosInt, out var tile);
            // Tile should be empty.
            if (!tile.IsEmpty)
                return false;

            // Check for grid on bottom map. Because we can't descend on the empty tile.
            _mapSys.TryGetTile(bottomGrid, descendCoordsPosInt, out var bottomTile);
            if (bottomTile.IsEmpty)
                return false;

            // Check walls on the tile.
            var tileBounds = new Box2(descendCoordsPosInt, descendCoordsPosInt + bottomGrid.TileSize);
            tileBounds = tileBounds.Enlarged(-0.1f);
            foreach (var ent in _lookup.GetEntitiesIntersecting(bottomGridUid, tileBounds))
            {
                if (_tag.HasTag(ent, WallTag))
                    return false;
            }
        }
        else // If we wanna go up
        {
            // Check if there is a bottom grid.
            if (!_mapManager.TryFindGridAt(maps[targetMapIdx], targetPosition.Value, out var topGridUid, out var topGrid))
                return false;

            var currentEntityCoords = xform.Coordinates;
            var currentCoordsPosInt = currentEntityCoords.ToVector2i(EntityManager, _mapManager, _xformSystem);
            var descendCoordsPosInt = descendEntityCoords.Value.ToVector2i(EntityManager, _mapManager, _xformSystem);

            // Check grid on top map from cur position. Because we can't dodge the roof hehe.
            _mapSys.TryGetTile(topGrid, currentCoordsPosInt, out var tile);
            // Tile should be empty.
            if (!tile.IsEmpty)
                return false;

            // Check for grid on bottom map. Because we can't descend on the empty tile.
            _mapSys.TryGetTile(topGrid, descendCoordsPosInt, out var topTile);
            if (topTile.IsEmpty)
                return false;

            // Check walls on the tile.
            var tileBounds = new Box2(descendCoordsPosInt, descendCoordsPosInt + topGrid.TileSize);
            tileBounds = tileBounds.Enlarged(-0.1f);
            foreach (var ent in _lookup.GetEntitiesIntersecting(topGridUid, tileBounds))
            {
                if (_tag.HasTag(ent, WallTag))
                    return false;
            }
        }

        return true;
    }

    private void OnClimbEndCollide(EntityUid uid, ClimbingComponent component, ref EndCollideEvent args)
    {
        if (args.OurFixtureId != ClimbingFixtureName
            || !component.IsClimbing
            || component.NextTransition != null
            || args.OurFixture.Contacts.Count > 1)
        {
            return;
        }

        foreach (var contact in args.OurFixture.Contacts.Values)
        {
            if (!contact.IsTouching)
                continue;

            var otherEnt = contact.OtherEnt(uid);
            var (otherFixtureId, otherFixture) = contact.OtherFixture(uid);

            // TODO: Remove this on engine.
            if (args.OtherEntity == otherEnt && args.OtherFixtureId == otherFixtureId)
                continue;

            if (otherFixture is { Hard: true } &&
                _climbableQuery.HasComp(otherEnt))
            {
                return;
            }
        }

        // TODO: Is this even needed anymore?
        foreach (var otherFixture in args.OurFixture.Contacts.Keys)
        {
            // If it's the other fixture then ignore em
            if (otherFixture == args.OtherFixture)
                continue;

            // If still colliding with a climbable, do not stop climbing
            if (HasComp<ClimbableComponent>(otherFixture.Owner))
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
            {
                continue;
            }

            _physics.SetCollisionMask(uid, name, fixture, fixture.CollisionMask | fixtureMask, fixtures);
        }

        climbing.DisabledFixtureMasks.Clear();
        _fixtureSystem.DestroyFixture(uid, ClimbingFixtureName, manager: fixtures);
        climbing.IsClimbing = false;
        climbing.NextTransition = null;
        var ev = new EndClimbEvent();
        RaiseLocalEvent(uid, ref ev);
        Dirty(uid, climbing);
    }

    /// <summary>
    ///     Checks if the user can vault the target
    /// </summary>
    /// <param name="component">The component of the entity that is being vaulted</param>
    /// <param name="user">The entity that wants to vault</param>
    /// <param name="target">The object that is being vaulted</param>
    /// <param name="reason">The reason why it cant be dropped</param>
    public bool CanVault(ClimbableComponent component, EntityUid user, EntityUid target, out string reason)
    {
        if (!_actionBlockerSystem.CanInteract(user, target))
        {
            reason = Loc.GetString("comp-climbable-cant-interact");
            return false;
        }

        if (!TryComp<ClimbingComponent>(user, out var climbingComp)
            || !climbingComp.CanClimb)
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
    public bool CanVault(ClimbableComponent component, EntityUid user, EntityUid dragged, EntityUid target,
        out string reason)
    {
        if (!_actionBlockerSystem.CanInteract(user, dragged) || !_actionBlockerSystem.CanInteract(user, target))
        {
            reason = Loc.GetString("comp-climbable-cant-interact");
            return false;
        }

        if (!HasComp<ClimbingComponent>(dragged))
        {
            reason = Loc.GetString("comp-climbable-target-cant-climb", ("moved-user", Identity.Entity(dragged, EntityManager)));
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

    public void ForciblyStopClimbing(EntityUid uid, ClimbingComponent? climbing = null, FixturesComponent? fixtures = null)
    {
        StopClimb(uid, climbing, fixtures);
    }

    private void OnBuckled(EntityUid uid, ClimbingComponent component, ref BuckledEvent args)
    {
        StopClimb(uid, component);
    }

    private void OnGlassClimbed(EntityUid uid, GlassTableComponent component, ref ClimbedOnEvent args)
    {
        if (TryComp<PhysicsComponent>(args.Climber, out var physics) && physics.Mass <= component.MassLimit)
            return;

        _damageableSystem.TryChangeDamage(args.Climber, component.ClimberDamage, origin: args.Climber);
        _damageableSystem.TryChangeDamage(uid, component.TableDamage, origin: args.Climber);
        _stunSystem.TryParalyze(args.Climber, TimeSpan.FromSeconds(component.StunTime), true);

        // Not shown to the user, since they already get a 'you climb on the glass table' popup
        _popupSystem.PopupEntity(
            Loc.GetString("glass-table-shattered-others", ("table", uid), ("climber", Identity.Entity(args.Climber, EntityManager))), args.Climber,
            Filter.PvsExcept(args.Climber), true);
    }

    [Serializable, NetSerializable]
    private sealed partial class ClimbDoAfterEvent : SimpleDoAfterEvent
    {
    }
}
