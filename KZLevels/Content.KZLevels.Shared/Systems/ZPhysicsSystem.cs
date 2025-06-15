using System.Collections.Generic;
using System.Numerics;
using Content.KayMisaZlevels.Shared.Components;
using Content.KayMisaZlevels.Shared.Miscellaneous;
using Robust.Shared.Configuration;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Physics.Components;
using Robust.Shared.Physics.Systems;
using Robust.Shared.Toolshed.Commands.Values;

namespace Content.KayMisaZlevels.Shared.Systems;

/// <summary>
///     This handles breaking your legs.
///     The general process for gravity follows:<br/>
///     - Starting from the level below the current, search for a gravity source.<br/>
///     -   - First, check each lower map for gravity. If any of them have gravity, take the one furthest from the player as the source.<br/>
///     -   - Otherwise, check for a grid in the fall location on each map, if one is present, check it for gravity.<br/>
///     - If no gravity source was found, the object does not fall (return).<br/>
///     - Otherwise, construct and fire ZLevelDroppingEvent.<br/>
///     - If the event is marked handled, return.<br/>
///     - Move the object to the fall location.<br/>
///     - Construct and fire ZLevelDroppedEvent.<br/>
/// </summary>
/// <remarks>
///     This does not handle making grids fall.
/// </remarks>

public sealed class ZPhysicsSystem : EntitySystem
{
    [Dependency] private readonly SharedMapSystem _maps = default!;
    [Dependency] private readonly SharedZStackSystem _zStack = default!;
    [Dependency] private readonly SharedTransformSystem _xform = default!;
    [Dependency] private readonly SharedPhysicsSystem _physics = default!;
    [Dependency] private readonly IMapManager _mapManager = default!;
    [Dependency] private readonly IConfigurationManager _cfg = default!;

    /// <inheritdoc/>
    public override void Initialize()
    {
        // TODO: Use KMZPhysicsComponent instead of PhysicsComponent
        SubscribeLocalEvent((Entity<PhysicsComponent> ent, ref MoveEvent args) => OnPossiblyFalling(ent, ref args));
        if (_cfg.GetCVar(KMZLevelsCVars.ProcessAllPhysicsObjects))
            _xform.OnGlobalMoveEvent += XformOnOnGlobalMoveEvent;
    }

    public override void Shutdown()
    {
        if (_cfg.GetCVar(KMZLevelsCVars.ProcessAllPhysicsObjects))
            _xform.OnGlobalMoveEvent -= XformOnOnGlobalMoveEvent;
    }

    private void XformOnOnGlobalMoveEvent(ref MoveEvent ev)
    {
        OnPossiblyFalling(ev.Entity, ref ev);
    }

    private void OnPossiblyFalling(EntityUid ent, ref MoveEvent args)
    {
        if (!_zStack.TryGetZStack(ent, out var zStack))
            return; // Not in a Z level containing space.

        if (args.Entity.Comp1.ParentUid != args.Entity.Comp1.MapUid || args.Entity.Comp1.MapUid is null)
            return; // No falling through grids, no nullspace.

        var coords = _xform.GetWorldPosition(args.Entity.Comp1);

        var maps = zStack.Value.Comp.Maps;
        var mapIdx = maps.IndexOf(args.Entity.Comp1.MapUid.Value);
        if (mapIdx <= 0)
            return; // Bottommost map can't be fallen through.

        var ev = new IsGravityAffectedEvent(ent, false);
        RaiseLocalEvent(ent, ref ev, broadcast: true);
        if (!ev.Affected)
            return; // We're not affected by gravity to begin with, so no falling.

        EntityUid? gravitySource = null;
        var distance = 1;
        var levels = new List<EntityUid>(); // TODO: Remove allocation.

        for (var i = mapIdx - 1; i >= 0; i--)
        {
            levels.Add(maps[i]);
            var gravityEv = new IsGravitySource(ent, maps[i], false);
            RaiseLocalEvent(maps[i], ref gravityEv, true);
            if (gravityEv.Handled)
            {
                gravitySource = maps[i];
                break;
            }

            // Try for grid.
            if (_mapManager.TryFindGridAt(maps[i], coords, out var gridId, out _))
            {
                gravityEv = new IsGravitySource(ent, gridId, false);
                RaiseLocalEvent(gridId, ref gravityEv, broadcast: true);
                if (gravityEv.Handled)
                    gravitySource = gridId;
                // Well.. we can't fall through grids.
                // So break regardless of if we found gravity.
                break;
            }

            distance++;
        }

        if (gravitySource is null)
            return; // Well, nothing to pull us down.

        var fallingEv = new ZLevelDroppingEvent(ent, distance, levels, gravitySource.Value, false);
        RaiseLocalEvent(ent, ref fallingEv, broadcast: true);

        if (fallingEv.Handled) // Someone else did the obliteration, probably.
            return;

        var gSourceXform = Transform(gravitySource.Value);
        var fallTarget = gSourceXform.MapUid ?? gravitySource.Value;

        // splat.
        _xform.SetCoordinates(ent, new EntityCoordinates(fallTarget, coords));

        var fellEv = new ZLevelDroppedEvent(ent, distance, levels, gravitySource.Value, false);
        RaiseLocalEvent(ent, ref fellEv, broadcast: true);
    }

    public bool TryGetTileWithEntity(
        EntityUid ent,
        ZDirection direction,
        out Tile? tile,
        out MapGridComponent? mapGrid,
        out EntityUid? targetMap,
        ZStackTrackerComponent? zStack = null,
        TransformComponent? xform = null,
        bool recursive = true)
    {
        tile = null;
        mapGrid = null;
        targetMap = null;

        if (!Resolve(ent, ref xform) ||
            xform.MapUid is null)
            return false;

        if (zStack is null)
        {
            if (!_zStack.TryGetZStack(xform.MapUid.Value, out var _zStackComp))
                return false;

            zStack = _zStackComp.Value.Comp;
        }

        if (direction == ZDirection.Down)
        {
            var result = GetTileFromBottom(ent, zStack, xform, recursive);
            if (result is not null)
            {
                targetMap = result.Value.Item1;
                mapGrid = result.Value.Item2;
                tile = result.Value.Item3;
            }
        }
        else
        {
            var result = GetTileFromTop(ent, zStack, xform, recursive);
            if (result is not null)
            {
                targetMap = result.Value.Item1;
                mapGrid = result.Value.Item2;
                tile = result.Value.Item3;
            }
        }

        if (tile is null)
            return false;
        else
            return true;
    }

    private (EntityUid, MapGridComponent, Tile)? GetTile(EntityUid targetMap, EntityUid ent, TransformComponent xform)
    {
        if (!_mapManager.TryFindGridAt(targetMap, _xform.GetWorldPosition(ent), out _, out var zGrid))
            return null;

        var intPos = xform.Coordinates.ToVector2i(EntityManager, _mapManager, _xform);
        _maps.TryGetTile(zGrid, intPos, out var resultTile);

        if (resultTile.IsEmpty)
            return null;

        return (targetMap, zGrid, resultTile);
    }

    private (EntityUid, MapGridComponent, Tile)? GetTileFromBottom(
        EntityUid ent,
        ZStackTrackerComponent zStack,
        TransformComponent xform,
        bool recursive = true)
    {
        if (xform.MapUid is null)
            return null;

        var maps = zStack.Maps;
        var mapIdx = maps.IndexOf(xform.MapUid.Value);
        var targetMap = maps[mapIdx];

        if (recursive)
        {
            for (int i = mapIdx; i >= 0; i--)
            {
                targetMap = maps[i];

                var result = GetTile(targetMap, ent, xform);
                if (result is not null)
                    return result;
            }
        }
        else
        {
            var result = GetTile(targetMap, ent, xform);
            if (result is not null)
                return result;
        }

        return null;
    }

    private (EntityUid, MapGridComponent, Tile)? GetTileFromTop(
        EntityUid ent,
        ZStackTrackerComponent zStack,
        TransformComponent xform,
        bool recursive = true)
    {
        if (xform.MapUid is null)
            return null;

        var maps = zStack.Maps;
        var mapIdx = maps.IndexOf(xform.MapUid.Value);

        if (mapIdx + 1 >= maps.Count)
            return null;

        var targetMap = maps[mapIdx + 1];

        if (recursive)
        {
            for (int i = mapIdx + 1; i < maps.Count; i++)
            {
                targetMap = maps[i];

                var result = GetTile(targetMap, ent, xform);
                if (result is not null)
                    return result;
            }
        }
        else
        {
            var result = GetTile(targetMap, ent, xform);
            if (result is not null)
                return result;
        }

        return null;
    }
}

[ByRefEvent]
public record struct IsGravityAffectedEvent(EntityUid Target, bool Affected)
{
    public void Set() => Affected = true;
}

[ByRefEvent]
public record struct IsGravitySource(EntityUid Entity, EntityUid Target, bool Handled)
{
    public void Handle() => Handled = true;
}

/// <summary>
///     Indicates an entity fell between Z levels.
/// </summary>
/// <param name="Distance">An approximation (based on level count) of drop distance.</param>
/// <param name="Levels">The levels dropped through, in order.</param>
/// <param name="GravitySource">The source of the gravitational pull (a grid or map.)</param>
/// <remarks>
///     If your Z levels are not strictly the same height, you'll want to calculate distance yourself.
///     It is strongly encouraged to play a cartoon splat effect if they fall far enough.
/// </remarks>
[ByRefEvent]
public record struct ZLevelDroppingEvent(EntityUid Target, int Distance, IReadOnlyList<EntityUid> Levels, EntityUid GravitySource, bool Handled);


/// <summary>
///     Indicates an entity fell between Z levels.
/// </summary>
/// <param name="Distance">An approximation (based on level count) of drop distance.</param>
/// <param name="Levels">The levels dropped through, in order.</param>
/// <param name="GravitySource">The source of the gravitational pull (a grid or map.)</param>
/// <remarks>
///     If your Z levels are not strictly the same height, you'll want to calculate distance yourself.
///     It is strongly encouraged to play a cartoon splat effect if they fall far enough.
/// </remarks>
[ByRefEvent]
public record struct ZLevelDroppedEvent(EntityUid Target, int Distance, IReadOnlyList<EntityUid> Levels, EntityUid GravitySource, bool Handled);
