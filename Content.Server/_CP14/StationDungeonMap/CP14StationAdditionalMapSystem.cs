using Content.Server.Parallax;
using Content.Server.Station.Components;
using Content.Server.Station.Events;
using Content.Server.Station.Systems;
using Content.Shared.Teleportation.Systems;
using Robust.Server.GameObjects;
using Robust.Shared.EntitySerialization;
using Robust.Shared.EntitySerialization.Systems;
using Robust.Shared.Prototypes;
using Content.Shared._CP14.StationDungeonMap;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;


namespace Content.Server._CP14.StationDungeonMap;


public sealed partial class CP14StationAdditionalMapSystem : EntitySystem
{
    [Dependency] private readonly MapSystem _map = default!;
    [Dependency] private readonly MapLoaderSystem _mapLoader = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<CP14StationAdditionalMapComponent, StationPostInitEvent>(OnStationPostInit);
    }

    private void OnStationPostInit(
        EntityUid uid,
        CP14StationAdditionalMapComponent addMap,
        ref StationPostInitEvent args
    )
    {
        if (!TryComp(uid, out StationDataComponent? _))
            return;

        foreach (var path in addMap.MapPaths)
        {
            var dOpts = new DeserializationOptions { InitializeMaps = true };

            Entity<MapComponent>? mapEntity;
            HashSet<Entity<MapGridComponent>>? grids;

            if (!_mapLoader.TryLoadMap(path, out mapEntity, out grids, dOpts))
            {
                Log.Error($"Failed to load map from {path}!");
                return;
            }

            // mapEntity is nullable per the signature; guard just in case.
            if (mapEntity is { } me)
            {
                var mapId = me.Comp.MapId;
                var gridCount = grids?.Count ?? 0;
                Log.Info($"Loaded additional map {mapId} from {path} with {gridCount} grids");
            }
            else
            {
                var gridCount = grids?.Count ?? 0;
                Log.Info($"Loaded additional map from {path} (no MapComponent returned) with {gridCount} grids");
            }
        }
    }
}
