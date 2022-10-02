﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Content.Client.Shuttles.Systems;
using Content.Server.GameTicking;
using Content.Server.Maps;
using Content.Server.Shuttles.Components;
using Content.Server.Station.Components;
using NUnit.Framework;
using Robust.Server.Maps;
using Robust.Shared.ContentPack;
using Robust.Shared.GameObjects;
using Robust.Shared.Utility;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;
using YamlDotNet.RepresentationModel;
using ShuttleSystem = Content.Server.Shuttles.Systems.ShuttleSystem;

namespace Content.IntegrationTests.Tests
{
    [TestFixture]
    public sealed class PostMapInitTest
    {
        private const bool SkipTestMaps = true;
        private const string TestMapsPath = "/Maps/Test/";

        [Test]
        public async Task NoSavedPostMapInitTest()
        {
            await using var pairTracker = await PoolManager.GetServerClient(new PoolSettings{NoClient = true});
            var server = pairTracker.Pair.Server;

            var resourceManager = server.ResolveDependency<IResourceManager>();
            var mapFolder = new ResourcePath("/Maps");
            var maps = resourceManager
                .ContentFindFiles(mapFolder)
                .Where(filePath => filePath.Extension == "yml" && !filePath.Filename.StartsWith("."))
                .ToArray();

            foreach (var map in maps)
            {
                var rootedPath = map.ToRootedPath();

                // ReSharper disable once RedundantLogicalConditionalExpressionOperand
                if (SkipTestMaps && rootedPath.ToString().StartsWith(TestMapsPath))
                {
                    continue;
                }

                if (!resourceManager.TryContentFileRead(rootedPath, out var fileStream))
                {
                    Assert.Fail($"Map not found: {rootedPath}");
                }

                using var reader = new StreamReader(fileStream);
                var yamlStream = new YamlStream();

                yamlStream.Load(reader);

                var root = yamlStream.Documents[0].RootNode;
                var meta = root["meta"];
                var postMapInit = meta["postmapinit"].AsBool();

                Assert.False(postMapInit, $"Map {map.Filename} was saved postmapinit");
            }
            await pairTracker.CleanReturnAsync();
        }

        private static string[] GetGameMapNames()
        {
           Task<string[]> task;
            using (ExecutionContext.SuppressFlow())
            {
                task = Task.Run(static async () =>
                {
                    await Task.Yield();
                    await using var pairTracker = await PoolManager.GetServerClient(
                        new PoolSettings
                        {
                            Disconnected = true,
                            TestName = $"{nameof(PostMapInitTest)}.{nameof(GetGameMapNames)}"
                        }
                    );
                    var server = pairTracker.Pair.Server;
                    var protoManager = server.ResolveDependency<IPrototypeManager>();

                    var maps = protoManager.EnumeratePrototypes<GameMapPrototype>().ToList();
                    var mapNames = new List<string>();
                    var naughty = new HashSet<string>()
                    {
                        "empty",
                        "infiltrator",
                        "pirate",
                    };

                    foreach (var map in maps)
                    {
                        // AAAAAAAAAA
                        // Why are they stations!
                        if (naughty.Contains(map.ID))
                            continue;

                        mapNames.Add(map.ID);
                    }

                    await pairTracker.CleanReturnAsync();
                    return mapNames.ToArray();
                });
                Task.WaitAll(task);
            }

            return task.GetAwaiter().GetResult();
        }

        [Test, TestCaseSource(nameof(GetGameMapNames))]
        public async Task GameMapsLoadableTest(string mapProto)
        {
            await using var pairTracker = await PoolManager.GetServerClient(new PoolSettings{NoClient = true});
            var server = pairTracker.Pair.Server;

            var mapLoader = server.ResolveDependency<IMapLoader>();
            var mapManager = server.ResolveDependency<IMapManager>();
            var entManager = server.ResolveDependency<IEntityManager>();
            var protoManager = server.ResolveDependency<IPrototypeManager>();
            var ticker = entManager.EntitySysManager.GetEntitySystem<GameTicker>();
            var shuttleSystem = entManager.EntitySysManager.GetEntitySystem<ShuttleSystem>();

            await server.WaitPost(() =>
            {
                var mapId = mapManager.CreateMap();
                try
                {
                    ticker.LoadGameMap(protoManager.Index<GameMapPrototype>(mapProto), mapId, null);
                }
                catch (Exception ex)
                {
                    throw new Exception($"Failed to load map {mapProto}", ex);
                }

                var shuttleMap = mapManager.CreateMap();
                var largest = 0f;
                EntityUid? targetGrid = null;
                var memberQuery = entManager.GetEntityQuery<StationMemberComponent>();

                var grids = mapManager.GetAllMapGrids(mapId);

                foreach (var grid in grids)
                {
                    if (!memberQuery.HasComponent(grid.GridEntityId))
                        continue;

                    var area = grid.LocalAABB.Width * grid.LocalAABB.Height;

                    if (area > largest)
                    {
                        largest = area;
                        targetGrid = grid.GridEntityId;
                    }
                }

                // Test shuttle can dock.
                // This is done inside gamemap test because loading the map takes ages and we already have it.
                var station = entManager.GetComponent<StationMemberComponent>(targetGrid!.Value).Station;
                var shuttlePath = entManager.GetComponent<StationDataComponent>(station).EmergencyShuttlePath
                    .ToString();
                var shuttle = mapLoader.LoadGrid(shuttleMap, entManager.GetComponent<StationDataComponent>(station).EmergencyShuttlePath.ToString());
                Assert.That(shuttleSystem.TryFTLDock(entManager.GetComponent<ShuttleComponent>(shuttle.gridId!.Value), targetGrid.Value), $"Unable to dock {shuttlePath} to {mapProto}");

                mapManager.DeleteMap(shuttleMap);

                try
                {
                    mapManager.DeleteMap(mapId);
                }
                catch (Exception ex)
                {
                    throw new Exception($"Failed to delete map {mapProto}", ex);
                }
            });
            await server.WaitRunTicks(1);

            await pairTracker.CleanReturnAsync();
        }

        /// <summary>
        /// Get the non-game map maps.
        /// </summary>
        private static string[] GetMaps()
        {
            Task<string[]> task;
            using (ExecutionContext.SuppressFlow())
            {
                task = Task.Run(static async () =>
                {
                    await Task.Yield();
                    await using var pairTracker = await PoolManager.GetServerClient(new PoolSettings{Disconnected = true});
                    var server = pairTracker.Pair.Server;
                    var resourceManager = server.ResolveDependency<IResourceManager>();
                    var protoManager = server.ResolveDependency<IPrototypeManager>();

                    var gameMaps = protoManager.EnumeratePrototypes<GameMapPrototype>().Select(o => o.MapPath).ToHashSet();

                    var mapFolder = new ResourcePath("/Maps");
                    var maps = resourceManager
                        .ContentFindFiles(mapFolder)
                        .Where(filePath => filePath.Extension == "yml" && !filePath.Filename.StartsWith("."))
                        .ToArray();
                    var mapNames = new List<string>();
                    foreach (var map in maps)
                    {
                        var rootedPath = map.ToRootedPath();

                        // ReSharper disable once RedundantLogicalConditionalExpressionOperand
                        if (SkipTestMaps && rootedPath.ToString().StartsWith(TestMapsPath) ||
                            gameMaps.Contains(map))
                        {
                            continue;
                        }
                        mapNames.Add(rootedPath.ToString());
                    }

                    await pairTracker.CleanReturnAsync();
                    return mapNames.ToArray();
                });
                Task.WaitAll(task);
            }

            return task.GetAwaiter().GetResult();
        }

        [Test, TestCaseSource(nameof(GetMaps))]
        public async Task MapsLoadableTest(string mapName)
        {
            await using var pairTracker = await PoolManager.GetServerClient(new PoolSettings{NoClient = true});
            var server = pairTracker.Pair.Server;

            var mapLoader = server.ResolveDependency<IMapLoader>();
            var mapManager = server.ResolveDependency<IMapManager>();

            await server.WaitPost(() =>
            {
                var mapId = mapManager.CreateMap();
                try
                {
                    mapLoader.LoadMap(mapId, mapName);
                }
                catch (Exception ex)
                {
                    throw new Exception($"Failed to load map {mapName}", ex);
                }

                try
                {
                    mapManager.DeleteMap(mapId);
                }
                catch (Exception ex)
                {
                    throw new Exception($"Failed to delete map {mapName}", ex);
                }
            });
            await server.WaitRunTicks(1);

            await pairTracker.CleanReturnAsync();
        }
    }
}
