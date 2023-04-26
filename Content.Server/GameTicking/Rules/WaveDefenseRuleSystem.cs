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
using System.Threading;
using Timer = Robust.Shared.Timing.Timer;

namespace Content.Server.GameTicking.Rules;

/// <summary>
/// Assign vault dwellers objectives at round-start and late-join.
/// </summary>
public sealed class WaveDefenseRuleSystem : GameRuleSystem
{
    [Dependency] private readonly IChatManager _chatManager = default!;
    [Dependency] private readonly RoundEndSystem _roundEndSystem = default!;

    public override string Prototype => "WaveDefense";

    private WaveDefenseRuleConfiguration _waveDefenseRuleConfig = new();

    public int WaveNumber = 0;
    public double HighScore = 0;
    public double KillCount = 0;

    private CancellationTokenSource _timerCancel = new();

    public List<EntityUid> Defenders = new();

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

    public override void Started(){}

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
        WaveNumber = 0;
        HighScore = 0;
        KillCount = 0;
        Defenders.Clear();
        RestartTimer();
    }

    private void HandleLatejoin(PlayerSpawnCompleteEvent ev)
    {
        if (!RuleAdded)
            return;

        EnsureComp<WaveDefenderComponent>(ev.Mob);
        Defenders.Add(ev.Mob);
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
                Defenders.Add(mob);
            }
        }
    }

    private void OnRoundEndText(RoundEndTextAppendEvent ev)
    {
        if (!RuleAdded)
            return;
        ev.AddLine(Loc.GetString("wave-defense-end-text", ("number", WaveNumber), ("kills", KillCount), ("score", HighScore)));
    }

    public void RestartTimer()
    {
        _timerCancel.Cancel();
        _timerCancel = new CancellationTokenSource();
        Timer.Spawn(_waveDefenseRuleConfig.WaveTime * 1000, TimerFired, _timerCancel.Token);
    }

    public void StopTimer()
    {
        _timerCancel.Cancel();
    }

    private void TimerFired()
    {
        WaveNumber++;
        _chatManager.DispatchServerAnnouncement(Loc.GetString("wave-defense-new-wave", ("number", WaveNumber)));

        var spawns = new List<EntityCoordinates>();

        // Forgive me for hardcoding prototypes
        foreach (var (_, meta, xform) in EntityQuery<SpawnPointComponent, MetaDataComponent, TransformComponent>(true))
        {
            if (meta.EntityPrototype?.ID != "SpawnPointWave") continue;

            spawns.Add(xform.Coordinates);
        }

        if (spawns.Count == 0)
        {
            Logger.WarningS("waves", $"No spawn points found for the next wave");
        }


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

        if (Defenders.Contains(mobUid) && args.NewMobState == MobState.Dead)
        {
            RemCompDeferred<WaveDefenderComponent>(mobUid);
            Defenders.Remove(mobUid);
        }

        if (Defenders.Count <= 0)
        {
            _roundEndSystem.EndRound();
        }
    }

    private float WavePool()
    {
        return Defenders.Count * WaveNumber * _waveDefenseRuleConfig.DifficultyMod;
    }
}
