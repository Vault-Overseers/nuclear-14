using Content.Server.Chat.Managers;
using Content.Server.GameTicking.Rules.Components;
using Content.Server.GameTicking.Rules.Configurations;
using Content.Server.Objectives.Interfaces;
using Content.Server.Players;
using Content.Server.Spawners.Components;
using Content.Server.Traitor;
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
    [Dependency] private readonly IConfigurationManager _cfg = default!;
    [Dependency] private readonly IObjectivesManager _objectivesManager = default!;
    [Dependency] private readonly IChatManager _chatManager = default!;

    public override string Prototype => "WaveDefense";

    private WaveDefenseRuleConfiguration _waveDefenseRuleConfig = new();

    public int WaveNumber = 0;

    private CancellationTokenSource _timerCancel = new();

    public List<EntityUid> Defenders = new();

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<RoundStartAttemptEvent>(OnStartAttempt);
        SubscribeLocalEvent<RulePlayerJobsAssignedEvent>(OnPlayersSpawned);
        SubscribeLocalEvent<PlayerSpawnCompleteEvent>(HandleLatejoin);
        SubscribeLocalEvent<RoundEndTextAppendEvent>(OnRoundEndText);
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

        // TODO: Round-end text
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

    private float WavePool()
    {
        return Defenders.Count * WaveNumber * _waveDefenseRuleConfig.DifficultyMod;
    }
}
