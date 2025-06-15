using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Content.KayMisaZlevels.Server.Components;
using Content.KayMisaZlevels.Shared.Components;
using Content.KayMisaZlevels.Shared.Systems;
using Robust.Server.GameObjects;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Map;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.KayMisaZlevels.Server.Systems;

public sealed partial class ZDefinedStackSystem : EntitySystem
{
    [Dependency] private readonly TransformSystem _xform = default!;
    [Dependency] private readonly MapSystem _map = default!;
    [Dependency] private readonly MapLoaderSystem _mapLoader = default!;
    [Dependency] private readonly ZStackSystem _zStack = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ZDefinedStackComponent, ComponentStartup>(OnStartup);
        SubscribeLocalEvent<ZDefinedStackComponent, MapInitEvent>(OnMapInit);
    }

    private void OnStartup(Entity<ZDefinedStackComponent> ent, ref ComponentStartup args)
    {
        ClearViewSubscribers(ent);
    }

    private void OnMapInit(Entity<ZDefinedStackComponent> initialMapUid, ref MapInitEvent args)
    {
        // If there is nothing tracker comp (like - round start, instead of mapping)
        LoadMap(initialMapUid, initialMapUid.Comp, initializeMaps: true);
    }

    private void ClearViewSubscribers(EntityUid mapUid)
    {
        var query = EntityQueryEnumerator<ZLoaderComponent>();
        while (query.MoveNext(out var mapEnt, out var loaderComp))
        {
            if (Transform(mapEnt).ParentUid == mapUid)
                QueueDel(mapEnt);
        }
    }

    public bool LoadMap(EntityUid initialMapUid, ZDefinedStackComponent? defStackComp = null, bool initializeMaps = false)
    {
        if (!Resolve(initialMapUid, ref defStackComp))
            return false;

        // We should use our initial map as stack for Z levels
        // Also, check if tracker comp already exist.
        // It need for mapping tools, instead of round gaming.
        var stackLoc = (EntityUid?) initialMapUid;
        if (TryComp<ZStackTrackerComponent>(initialMapUid, out var _))
            return false;
        else
            AddComp<ZStackTrackerComponent>(initialMapUid);

        List<MapId> mapsToInitialize = new();

        // Load levels downer
        foreach (var path in defStackComp.DownLevels)
        {
            var mapId = LoadLevel(stackLoc, path);
            if (mapId is not null)
                mapsToInitialize.Add((MapId) mapId);
        }

        // Add initial map as middle level in the world
        _zStack.AddToStack(initialMapUid, ref stackLoc);

        // Load level upper
        foreach (var path in defStackComp.UpLevels)
        {
            var mapId = LoadLevel(stackLoc, path);
            if (mapId is not null)
                mapsToInitialize.Add((MapId) mapId);
        }

        // Try to intialize maps
        if (initializeMaps)
        {
            foreach (var mapId in mapsToInitialize)
            {
                _map.InitializeMap(mapId);
            }
        }

        return true;
    }

    /// <summary>
    /// Load children map of the map.
    /// </summary>
    /// <param name="stackLoc">What the fuck is this?</param>
    /// <param name="path">YAML Map path</param>
    /// <param name="initializeMaps">Should we initialize maps whe it was loaded.</param>
    public MapId? LoadLevel(EntityUid? stackLoc, ResPath path, bool initializeMaps = false)
    {
        var options = new DeserializationOptions()
        {
            InitializeMaps = initializeMaps
        };

        if (_mapLoader.TryLoadMap(path, out var map, out _, options: options))
        {
            // Clear garbage PVS subscribes, because WizDen's is trans gays.
            // I think they should die, by shatter they body by themselfs.
            // Just kill yourself, WizDen, pls!
            ClearViewSubscribers(map.Value);

            // Add to stack
            _zStack.AddToStack(map.Value, ref stackLoc);

            // Mark as a member of defined maps. It needs for multi saving
            AddComp(map.Value,
                new ZDefinedStackMemberComponent()
                {
                    SavePath = path
                });

            Log.Info($"Created map {map.Value} for ZDefinedStackSystem system");

            // Don't return MapId, because map was already initialized
            if (initializeMaps)
                return null;

            return map.Value.Comp.MapId;
        }
        else
        {
            Log.Error($"Failed to load map from {path}!");
            return null;
        }
    }
}
