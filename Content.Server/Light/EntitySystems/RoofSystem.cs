using Content.KayMisaZlevels.Server.Systems;
using Content.Server.Light.Components;
using Content.Shared.Light.Components;
using Content.Shared.Light.EntitySystems;
using Content.Shared.Maps;
using Robust.Server.GameObjects;
using Robust.Shared.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;

namespace Content.Server.Light.EntitySystems;

/// <inheritdoc/>
public sealed class RoofSystem : SharedRoofSystem
{
    [Dependency] private readonly SharedMapSystem _maps = default!;
    [Dependency] private readonly IEntitySystemManager _sysMan = default!;
    private ZStackSystem? _zstack;
    [Dependency] private readonly TransformSystem _xform = default!;
    [Dependency] private readonly IMapManager _mapManager = default!;
    [Dependency] private readonly ITileDefinitionManager _tileDefinitionManager = default!;

    private EntityQuery<MapGridComponent> _gridQuery;

    public override void Initialize()
    {
        base.Initialize();
        _sysMan.TryGetEntitySystem(out _zstack);
        _gridQuery = GetEntityQuery<MapGridComponent>();
        SubscribeLocalEvent<SetRoofComponent, ComponentStartup>(OnFlagStartup);
        SubscribeLocalEvent<RoofComponent, MapInitEvent>(OnMapInit, after: [typeof(ZDefinedStackSystem)]);
        SubscribeLocalEvent<IsRoofComponent, ComponentStartup>(OnStartup);

        SubscribeLocalEvent<TileChangedEvent>(OnTileChanged);
    }

    private void OnStartup(EntityUid uid, IsRoofComponent roof, ref ComponentStartup args)
    {
        var xform = Transform(uid);
        if (_zstack == null || !_zstack.TryGetZStack(uid, out var zStack) || xform.MapUid == null)
            return;

        var maps = zStack.Value.Comp.Maps;
        var mapIdx = maps.IndexOf(xform.MapUid.Value);

        if (mapIdx + 1 >= maps.Count)
            return;

        var targetMap = maps[mapIdx + 1];

        if (!_mapManager.TryFindGridAt(targetMap, xform.WorldPosition, out _, out var zGrid))
            return;

        var intPos = xform.Coordinates.ToVector2i(EntityManager, _mapManager, _xform);
        _maps.TryGetTile(zGrid, intPos, out var tile);

        if (!tile.IsEmpty)
            return;

        if (xform.GridUid == null || !_gridQuery.TryComp(xform.GridUid.Value, out var grid))
            return;

        _maps.TryGetTile(grid, intPos, out var originTile);

        if (originTile.IsEmpty)
            return;

        zGrid.SetTile(new EntityCoordinates(zGrid.Owner, xform.WorldPosition), originTile);
    }

    private void OnMapInit(Entity<RoofComponent> ent, ref MapInitEvent args)
    {
        if (_zstack == null || !_zstack.TryGetZStack(ent, out var stack))
            return;
        if (!_gridQuery.TryComp(ent, out var grid))
            return;

        var maps = stack.Value.Comp.Maps;
        var mapIdx = maps.IndexOf(ent);
        if (mapIdx <= 0)
            return;

        var targetMapUid = maps[mapIdx - 1];

        if (!_gridQuery.TryComp(targetMapUid, out var targetGrid))
            return;

        var enumerator = _maps.GetAllTilesEnumerator(ent, grid);
        while (enumerator.MoveNext(out var tileRef))
        {
            if (tileRef is null)
                continue;

            ContentTileDefinition tileDef = (ContentTileDefinition) _tileDefinitionManager[tileRef.Value.Tile.TypeId];

            bool isRoofed = tileDef.ID != ContentTileDefinition.SpaceID;

            SetRoof((targetMapUid, targetGrid, null), tileRef.Value.GridIndices, isRoofed);
        }
    }

    private void OnTileChanged(ref TileChangedEvent args)
    {
        if (_zstack == null || !_zstack.TryGetZStack(args.Entity, out var stack))
            return;

        var mapUid = GetMapUidByGrid(args.Entity);
        if (mapUid is null)
            return;

        // var coords = _transformSystem.GetWorldPosition(ent);
        var maps = stack.Value.Comp.Maps;
        var mapIdx = maps.IndexOf(mapUid.Value);
        if (mapIdx <= 0)
            return;

        var targetMapUid = maps[mapIdx - 1];

        // Well, i don't think we should use RoofComponent for space worlds.
        // So, just use single grid.
        if (!_gridQuery.TryComp(targetMapUid, out var targetMapGridComp))
            return;

        ContentTileDefinition? tileDef = (ContentTileDefinition) _tileDefinitionManager[args.NewTile.Tile.TypeId];

        bool isRoofed = tileDef.ID != ContentTileDefinition.SpaceID;

        SetRoof((targetMapUid, targetMapGridComp, null), args.NewTile.GridIndices, isRoofed);
    }

    private EntityUid? GetMapUidByGrid(EntityUid grid)
    {
        if (TryComp<TransformComponent>(grid, out var xformComp))
            return xformComp.MapUid;

        return null;
    }

    private void OnFlagStartup(Entity<SetRoofComponent> ent, ref ComponentStartup args)
    {
        var xform = Transform(ent.Owner);

        if (_gridQuery.TryComp(xform.GridUid, out var grid))
        {
            var index = _maps.LocalToTile(xform.GridUid.Value, grid, xform.Coordinates);
            SetRoof((xform.GridUid.Value, grid, null), index, ent.Comp.Value);
        }

        QueueDel(ent.Owner);
    }
}
