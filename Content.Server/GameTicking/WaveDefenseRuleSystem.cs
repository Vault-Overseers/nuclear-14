using Content.Server.Cargo.Systems;
using Content.Server.Chat.Managers;
using Content.Server.GameTicking.Rules.Components;
using Content.Server.RoundEnd;
using Content.Server.Spawners.Components;
using Content.Shared.Mobs;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using System.Threading;
using Timer = Robust.Shared.Timing.Timer;
using Content.Server.Administration;
using Robust.Shared.Console;
using Content.Shared.Administration;
using Content.Shared.Mobs.Components;
using System.Linq;
using Content.Server.Cargo.Components;
using Content.Server.GameTicking.Components;
using Content.Server.Station.Systems;
using Content.Shared.GameTicking.Components;


namespace Content.Server.GameTicking.Rules;

/// <summary>
/// Assign vault dwellers objectives at round-start and late-join.
/// </summary>
public sealed class WaveDefenseRuleSystem : GameRuleSystem<WaveDefenseRuleComponent>
{
    [Dependency] private readonly IChatManager _chatManager = default!;
    [Dependency] private readonly IEntityManager _entMan = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly RoundEndSystem _roundEndSystem = default!;
    [Dependency] private readonly IPrototypeManager _protoManager = default!;
    [Dependency] private readonly CargoSystem _cargo = default!;
    [Dependency] private readonly StationSystem _station = default!;

    public int WaveNumber = 0;
    public double HighScore = 0;
    public double KillCount = 0;

    private CancellationTokenSource _timerCancel = new();

    public List<string> Defenders = new();

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<RulePlayerJobsAssignedEvent>(OnPlayersSpawned);
        SubscribeLocalEvent<PlayerSpawnCompleteEvent>(HandleLatejoin);
        SubscribeLocalEvent<RoundEndTextAppendEvent>(OnRoundEndText);
        SubscribeLocalEvent<WaveDefenderComponent, MobStateChangedEvent>(OnPlayerDied);
        SubscribeLocalEvent<WaveMobComponent, MobStateChangedEvent>(OnMobDied);
    }

    protected override void Started(EntityUid uid, WaveDefenseRuleComponent rules, GameRuleComponent gameRule, GameRuleStartedEvent args)
    {
        Logger.InfoS("waves", $"Starting wave defense game mode");
        WaveNumber = 0;
        HighScore = 0;
        KillCount = 0;
        Defenders.Clear();
        RestartTimer();
    }

    protected override void Ended(EntityUid uid, WaveDefenseRuleComponent rules, GameRuleComponent gameRule, GameRuleEndedEvent args)
    {
        StopTimer();
    }

    private void HandleLatejoin(PlayerSpawnCompleteEvent ev)
    {
        var query = EntityQueryEnumerator<WaveDefenseRuleComponent, GameRuleComponent>();
        while (query.MoveNext(out var uid, out var waves, out var gameRule))
        {
            if (!GameTicker.IsGameRuleAdded(uid, gameRule))
                return;
            EnsureComp<WaveDefenderComponent>(ev.Mob);
            Defenders.Add(ev.Profile.Name);
        }
    }

    private void OnPlayersSpawned(RulePlayerJobsAssignedEvent ev)
    {
        var query = EntityQueryEnumerator<WaveDefenseRuleComponent, GameRuleComponent>();
        while (query.MoveNext(out var uid, out var nukeops, out var gameRule))
        {
            if (!GameTicker.IsGameRuleAdded(uid, gameRule))
                return;

            foreach (var player in ev.Players)
            {
                if (player.AttachedEntity is { Valid: true } mob)
                {
                    EnsureComp<WaveDefenderComponent>(mob);
                    Defenders.Add(player.Name);
                }
            }
        }
    }

    private void OnRoundEndText(RoundEndTextAppendEvent ev)
    {
        var query = EntityQueryEnumerator<WaveDefenseRuleComponent, GameRuleComponent>();
        while (query.MoveNext(out var uid, out var rules, out var gameRule))
        {
            if (!GameTicker.IsGameRuleAdded(uid, gameRule))
                return;
            ev.AddLine(Loc.GetString("wave-defence-end-text", ("number", WaveNumber), ("kills", KillCount),
                ("score", HighScore)));
            ev.AddLine(Loc.GetString("wave-defence-participants"));
            foreach (var player in Defenders)
            {
                ev.AddLine(player);
            }
        }

        StopTimer();
    }

    public void RestartTimer()
    {
        var query = EntityQueryEnumerator<WaveDefenseRuleComponent, GameRuleComponent>();
        while (query.MoveNext(out var uid, out var rules, out var gameRule))
        {
            if (!GameTicker.IsGameRuleAdded(uid, gameRule))
                return;
            _timerCancel.Cancel();
            _timerCancel = new CancellationTokenSource();
            Timer.Spawn(rules.WaveTime * 1000, TimerFired, _timerCancel.Token);
            Logger.InfoS("waves", $"Next wave in {rules.WaveTime} seconds");
        }
    }

    public void StopTimer()
    {
        _timerCancel.Cancel();
    }

    public void TimerFired()
    {
        WaveNumber++;
        SpawnWave(WaveNumber);
    }

    private void SpawnWave(int wave)
    {
        _chatManager.DispatchServerAnnouncement(Loc.GetString("wave-defence-new-wave", ("number", wave)));

        var spawns = new List<EntityCoordinates>();
        foreach (var (_, meta, xform) in EntityQuery<SpawnPointComponent, MetaDataComponent, TransformComponent>(true))
        {
            if (meta.EntityPrototype?.ID == "SpawnPointWave")
                spawns.Add(xform.Coordinates);
        }

        if (spawns.Count == 0)
        {
            Logger.WarningS("waves", $"No spawn points found on map. Not spawning any monsters.");
            return;
        }

        foreach (var ent in PickMonsters(wave))
        {
            var coord = RandomExtensions.Pick(_random, spawns);
            var mob = _entMan.SpawnEntity(ent, coord);
            var info = EnsureComp<WaveMobComponent>(mob);
            // TODO: set info here
        }
        RestartTimer();
    }

    private List<string> PickMonsters(int wave)
    {
        var entityPool = _protoManager.EnumeratePrototypes<EntityPrototype>();
        var mobList = new Dictionary<string, Tuple<string, float, bool>>();

        //heres the expensive bit but on the upside it should handle prototype reloading fine since
        //we will invoke this once per wave spawn.
        //if we want this done only once per round for instance, we would need to hook in to proto reload.

        foreach (var proto in entityPool)
        {
            if (!proto.TryGetComponent<WaveMobComponent>(out var waveMobComp))
                continue;

            if (wave < waveMobComp.Difficulty)
                continue;

            var diffGroup = new Tuple<string, float, bool>(waveMobComp.Group, waveMobComp.Difficulty, waveMobComp.Unique);
            mobList.Add(proto.ID, diffGroup);
        }
        //Todo: fine tune this, i just slapped an integer here wcgw.
        //Ideally, we would have no numbers hard coded at all and should rely solely on the RuleConfig, so go there to change the actual difficulty
        //or make a new rule config. Same goes with timer
        var difficulty = 0f;
        var query = EntityQueryEnumerator<WaveDefenseRuleComponent, GameRuleComponent>();
        while (query.MoveNext(out var uid, out var rules, out var gameRule))
        {
            if (!GameTicker.IsGameRuleAdded(uid, gameRule))
                continue;
            difficulty = rules.DifficultyMod;
        }

        var wavePool = wave * difficulty * (Defenders.Count * 2);
        var waveTemplate = _random.Pick(mobList).Value.Item1;
        var spawnList = new List<string>();

        while (wavePool > 0 && mobList.Count > 0)
        {
            var mobCandidate = _random.Pick(mobList);

            if (mobCandidate.Value.Item1 != waveTemplate)
                continue;

            if (mobCandidate.Value.Item3)
            {
                mobList.Remove(mobCandidate.Key);
            }

            spawnList.Add(mobCandidate.Key);
            wavePool -= mobCandidate.Value.Item2;
        }
        return spawnList;
    }

    private void OnMobDied(EntityUid mobUid, WaveMobComponent component, MobStateChangedEvent args)
    {
        var query = EntityQueryEnumerator<WaveDefenseRuleComponent, GameRuleComponent>();
        while (query.MoveNext(out var uid, out var rules, out var gameRule))
        {
            if (!GameTicker.IsGameRuleAdded(uid, gameRule))
                return;
            if (args.NewMobState == MobState.Dead)
            {
                KillCount++;
                HighScore += component.Difficulty * 10;
                RemCompDeferred<WaveMobComponent>(mobUid);
                var station = _station.GetOwningStation(mobUid);
                if (TryComp<StationBankAccountComponent>(station, out var stationBank))
                {
                    _cargo.UpdateBankAccount(station.Value, stationBank, (int) (component.Difficulty * 100));
                }
            }
        }
    }

    private void OnPlayerDied(EntityUid mobUid, WaveDefenderComponent component, MobStateChangedEvent args)
    {
        var query = EntityQueryEnumerator<WaveDefenseRuleComponent, GameRuleComponent>();
        while (query.MoveNext(out var uid, out var nukeops, out var gameRule))
        {
            if (!GameTicker.IsGameRuleAdded(uid, gameRule))
                return;
            var defenders = EntityQuery<WaveDefenderComponent, MobStateComponent>(true);
            var defendersAlive = defenders.Any(ent => ent.Item2.CurrentState == MobState.Alive && ent.Item1.Running);
            if (!defendersAlive)
            {
                _roundEndSystem.EndRound();
            }
        }
    }
}

[AdminCommand(AdminFlags.Round)]
sealed class NextWaveCommand : IConsoleCommand
{
    public string Command => "nextwave";
    public string Description => "Send the next wave now if in wave defense mode";
    public string Help => Loc.GetString("cmd-nextwave-help");

    public void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        var sysMan = IoCManager.Resolve<IEntitySystemManager>();
        var _waves = sysMan.GetEntitySystem<WaveDefenseRuleSystem>();
        _waves.TimerFired();
    }
}
