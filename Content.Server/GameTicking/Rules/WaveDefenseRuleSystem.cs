using Content.Server.Chat.Managers;
using Content.Server.GameTicking.Rules.Components;
using Content.Server.GameTicking.Rules.Configurations;
using Content.Server.Objectives.Interfaces;
using Content.Server.Players;
using Content.Server.RoundEnd;
using Content.Server.Spawners.Components;
using Content.Server.Traitor;
using Content.Shared.Mobs;
using Robust.Server.Player;
using Robust.Shared.Configuration;
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
using Content.Shared.GameTicking;

namespace Content.Server.GameTicking.Rules;

/// <summary>
/// Assign vault dwellers objectives at round-start and late-join.
/// </summary>
public sealed class WaveDefenseRuleSystem : GameRuleSystem
{
    [Dependency] private readonly IChatManager _chatManager = default!;
    [Dependency] private readonly IEntityManager _entMan = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly RoundEndSystem _roundEndSystem = default!;
    [Dependency] private readonly IPrototypeManager _protoManager = default!;

    public override string Prototype => "WaveDefense";

    private WaveDefenseRuleConfiguration _waveDefenseRuleConfig = new();

    public int WaveNumber = 0;
    public double HighScore = 0;
    public double KillCount = 0;

    private CancellationTokenSource _timerCancel = new();

    public List<string> Defenders = new();

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<RoundStartAttemptEvent>(OnStartAttempt);
        SubscribeLocalEvent<RulePlayerJobsAssignedEvent>(OnPlayersSpawned);
        SubscribeLocalEvent<PlayerSpawnCompleteEvent>(HandleLatejoin);
        SubscribeLocalEvent<RoundEndTextAppendEvent>(OnRoundEndText);
        SubscribeLocalEvent<WaveDefenderComponent, MobStateChangedEvent>(OnPlayerDied);
        SubscribeLocalEvent<WaveMobComponent, MobStateChangedEvent>(OnMobDied);
    }

    public override void Started()
    {
        Logger.InfoS("waves", $"Starting wave defense game mode");
        WaveNumber = 0;
        HighScore = 0;
        KillCount = 0;
        Defenders.Clear();
        RestartTimer();
    }

    public override void Ended()
    {
        StopTimer();
    }

    private void OnStartAttempt(RoundStartAttemptEvent ev)
    {
        if (!RuleAdded)
            return;

        if (!ev.Forced)
        {
            // TODO: If nobody readied up, refuse to start
            //ev.Cancel();
        }
    }

    private void HandleLatejoin(PlayerSpawnCompleteEvent ev)
    {
        if (!RuleAdded)
            return;

        EnsureComp<WaveDefenderComponent>(ev.Mob);
        Defenders.Add(ev.Profile.Name);
    }

    private void OnPlayersSpawned(RulePlayerJobsAssignedEvent ev)
    {
        if (!RuleAdded)
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

    private void OnRoundEndText(RoundEndTextAppendEvent ev)
    {
        if (!RuleAdded)
            return;
        ev.AddLine(Loc.GetString("wave-defence-end-text", ("number", WaveNumber), ("kills", KillCount), ("score", HighScore)));
        ev.AddLine(Loc.GetString("wave-defence-participants"));
        foreach (var player in Defenders)
        {
            ev.AddLine(player);
        }
    }

    public void RestartTimer()
    {
        _timerCancel.Cancel();
        _timerCancel = new CancellationTokenSource();
        Timer.Spawn(_waveDefenseRuleConfig.WaveTime * 1000, TimerFired, _timerCancel.Token);
        Logger.InfoS("waves", $"Next wave in {_waveDefenseRuleConfig.WaveTime} seconds");
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
        var wavePool = wave * _waveDefenseRuleConfig.DifficultyMod * (Defenders.Count * 2);
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
        if (!RuleAdded)
            return;

        if (args.NewMobState == MobState.Dead)
        {
            KillCount++;
            HighScore += component.Difficulty * 10;
            RemCompDeferred<WaveMobComponent>(mobUid);
        }    
    }

    private void OnPlayerDied(EntityUid mobUid, WaveDefenderComponent component, MobStateChangedEvent args)
    {
        if (!RuleAdded)
            return;
        var defenders = EntityQuery<WaveDefenderComponent, MobStateComponent>(true);
        var defendersAlive = defenders.Any(ent => ent.Item2.CurrentState == MobState.Alive && ent.Item1.Running);
        if (!defendersAlive)
        {
            _roundEndSystem.EndRound();
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
