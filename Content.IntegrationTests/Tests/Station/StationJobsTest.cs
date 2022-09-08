﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Content.Server.Maps;
using Content.Server.Station.Systems;
using Content.Shared.Preferences;
using NUnit.Framework;
using Robust.Shared.GameObjects;
using Robust.Shared.Log;
using Robust.Shared.Map;
using Robust.Shared.Network;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.IntegrationTests.Tests.Station;

[TestFixture]
[TestOf(typeof(StationJobsSystem))]
public sealed class StationJobsTest
{
    private const string Prototypes = @"
- type: playTimeTracker
  id: Dummy

- type: playTimeTracker
  id: Overall

- type: gameMap
  id: FooStation
  minPlayers: 0
  mapName: FooStation
  mapPath: Maps/Tests/empty.yml
  stations:
    Station:
      mapNameTemplate: FooStation
      overflowJobs:
      - Assistant
      availableJobs:
        TMime: [0, -1]
        TAssistant: [-1, -1]
        TCaptain: [5, 5]
        TClown: [5, 6]

- type: job
  id: TAssistant
  playTimeTracker: Dummy

- type: job
  id: TMime
  weight: 20
  playTimeTracker: Dummy

- type: job
  id: TClown
  weight: -10
  playTimeTracker: Dummy

- type: job
  id: TCaptain
  weight: 10
  playTimeTracker: Dummy

- type: job
  id: TChaplain
  playTimeTracker: Dummy
";

    private const int StationCount = 100;
    private const int CaptainCount = StationCount;
    private const int PlayerCount = 2000;
    private const int TotalPlayers = PlayerCount + CaptainCount;

    [Test]
    public async Task AssignJobsTest()
    {
        await using var pairTracker = await PoolManager.GetServerClient(new PoolSettings{NoClient = true, ExtraPrototypes = Prototypes});
        var server = pairTracker.Pair.Server;

        var prototypeManager = server.ResolveDependency<IPrototypeManager>();
        var fooStationProto = prototypeManager.Index<GameMapPrototype>("FooStation");
        var entSysMan = server.ResolveDependency<IEntityManager>().EntitySysManager;
        var stationJobs = entSysMan.GetEntitySystem<StationJobsSystem>();
        var stationSystem = entSysMan.GetEntitySystem<StationSystem>();

        List<EntityUid> stations = new();
        await server.WaitPost(() =>
        {
            for (var i = 0; i < StationCount; i++)
            {
                stations.Add(stationSystem.InitializeNewStation(fooStationProto.Stations["Station"], null, $"Foo {StationCount}"));
            }
        });

        await server.WaitAssertion(() =>
        {
            var fakePlayers = new Dictionary<NetUserId, HumanoidCharacterProfile>()
                .AddJob("TAssistant", JobPriority.Medium, PlayerCount)
                .AddPreference("TClown", JobPriority.Low)
                .AddPreference("TMime", JobPriority.High)
                .WithPlayers(
                    new Dictionary<NetUserId, HumanoidCharacterProfile>()
                    .AddJob("TCaptain", JobPriority.High, CaptainCount)
                );
            Assert.That(fakePlayers, Is.Not.Empty);

            var start = new Stopwatch();
            start.Start();
            var assigned = stationJobs.AssignJobs(fakePlayers, stations);
            Assert.That(assigned, Is.Not.Empty);
            var time = start.Elapsed.TotalMilliseconds;
            Logger.Info($"Took {time} ms to distribute {TotalPlayers} players.");

            foreach (var station in stations)
            {
                var assignedHere = assigned
                    .Where(x => x.Value.Item2 == station)
                    .ToDictionary(x => x.Key, x => x.Value);

                // Each station should have SOME players.
                Assert.That(assignedHere, Is.Not.Empty);
                // And it should have at least the minimum players to be considered a "fair" share, as they're all the same.
                Assert.That(assignedHere, Has.Count.GreaterThanOrEqualTo(TotalPlayers/stations.Count), "Station has too few players.");
                // And it shouldn't have ALL the players, either.
                Assert.That(assignedHere, Has.Count.LessThan(TotalPlayers), "Station has too many players.");
                // And there should be *A* captain, as there's one player with captain enabled per station.
                Assert.That(assignedHere.Where(x => x.Value.Item1 == "TCaptain").ToList(), Has.Count.EqualTo(1));
            }

            // All clown players have assistant as a higher priority.
            Assert.That(assigned.Values.Select(x => x.Item1).ToList(), Does.Not.Contain("TClown"));
            // Mime isn't an open job-slot at round-start.
            Assert.That(assigned.Values.Select(x => x.Item1).ToList(), Does.Not.Contain("TMime"));
            // All players have slots they can fill.
            Assert.That(assigned.Values, Has.Count.EqualTo(TotalPlayers), $"Expected {TotalPlayers} players.");
            // There must be assistants present.
            Assert.That(assigned.Values.Select(x => x.Item1).ToList(), Does.Contain("TAssistant"));
            // There must be captains present, too.
            Assert.That(assigned.Values.Select(x => x.Item1).ToList(), Does.Contain("TCaptain"));
        });
        await pairTracker.CleanReturnAsync();
    }

    [Test]
    public async Task AdjustJobsTest()
    {
        await using var pairTracker = await PoolManager.GetServerClient(new PoolSettings{NoClient = true, ExtraPrototypes = Prototypes});
        var server = pairTracker.Pair.Server;

        var prototypeManager = server.ResolveDependency<IPrototypeManager>();
        var mapManager = server.ResolveDependency<IMapManager>();
        var fooStationProto = prototypeManager.Index<GameMapPrototype>("FooStation");
        var entSysMan = server.ResolveDependency<IEntityManager>().EntitySysManager;
        var stationJobs = entSysMan.GetEntitySystem<StationJobsSystem>();
        var stationSystem = entSysMan.GetEntitySystem<StationSystem>();

        var station = EntityUid.Invalid;
        await server.WaitPost(() =>
        {
            mapManager.CreateNewMapEntity(MapId.Nullspace);
            station = stationSystem.InitializeNewStation(fooStationProto.Stations["Station"], null, $"Foo Station");
        });

        await server.WaitRunTicks(1);

        await server.WaitAssertion(() =>
        {
            // Verify jobs are/are not unlimited.
            Assert.Multiple(() =>
            {
                Assert.That(stationJobs.IsJobUnlimited(station, "TAssistant"), "TAssistant is expected to be unlimited.");
                Assert.That(stationJobs.IsJobUnlimited(station, "TMime"), "TMime is expected to be unlimited.");
                Assert.That(!stationJobs.IsJobUnlimited(station, "TCaptain"), "TCaptain is expected to not be unlimited.");
                Assert.That(!stationJobs.IsJobUnlimited(station, "TClown"), "TClown is expected to not be unlimited.");
            });
            Assert.Multiple(() =>
            {
                Assert.That(stationJobs.TrySetJobSlot(station, "TClown", 0), "Could not set TClown to have zero slots.");
                Assert.That(stationJobs.TryGetJobSlot(station, "TClown", out var clownSlots), "Could not get the number of TClown slots.");
                Assert.That(clownSlots, Is.EqualTo(0));
                Assert.That(!stationJobs.TryAdjustJobSlot(station, "TCaptain", -9999), "Was able to adjust TCaptain by -9999 without clamping.");
                Assert.That(stationJobs.TryAdjustJobSlot(station, "TCaptain", -9999, false, true), "Could not adjust TCaptain by -9999.");
                Assert.That(stationJobs.TryGetJobSlot(station, "TCaptain", out var captainSlots), "Could not get the number of TCaptain slots.");
                Assert.That(captainSlots, Is.EqualTo(0));
            });
            Assert.Multiple(() =>
            {
                Assert.That(stationJobs.TrySetJobSlot(station, "TChaplain", 10, true), "Could not create 10 TChaplain slots.");
                stationJobs.MakeJobUnlimited(station, "TChaplain");
                Assert.That(stationJobs.IsJobUnlimited(station, "TChaplain"), "Could not make TChaplain unlimited.");
            });
        });
        await pairTracker.CleanReturnAsync();
    }
}

internal static class JobExtensions
{
    public static Dictionary<NetUserId, HumanoidCharacterProfile> AddJob(
        this Dictionary<NetUserId, HumanoidCharacterProfile> inp, string jobId, JobPriority prio = JobPriority.Medium,
        int amount = 1)
    {
        for (var i = 0; i < amount; i++)
        {
            inp.Add(new NetUserId(Guid.NewGuid()), HumanoidCharacterProfile.Random().WithJobPriority(jobId, prio));
        }

        return inp;
    }

    public static Dictionary<NetUserId, HumanoidCharacterProfile> AddPreference(
        this Dictionary<NetUserId, HumanoidCharacterProfile> inp, string jobId, JobPriority prio = JobPriority.Medium)
    {
        return inp.ToDictionary(x => x.Key, x => x.Value.WithJobPriority(jobId, prio));
    }

    public static Dictionary<NetUserId, HumanoidCharacterProfile> WithPlayers(
        this Dictionary<NetUserId, HumanoidCharacterProfile> inp,
        Dictionary<NetUserId, HumanoidCharacterProfile> second)
    {
        return new[] {inp, second}.SelectMany(x => x).ToDictionary(x => x.Key, x => x.Value);
    }
}
