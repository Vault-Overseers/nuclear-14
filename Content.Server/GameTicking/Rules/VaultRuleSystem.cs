using Content.Server.GameTicking.Events;
using Content.Server.GameTicking.Rules.Components;
using Content.Server.Objectives.Interfaces;
using Content.Server.Players;
using Robust.Server.Player;
using Robust.Shared.Configuration;

namespace Content.Server.GameTicking.Rules;

/// <summary>
/// Assign vault dwellers objectives at round-start and late-join.
/// </summary>
public sealed class VaultRuleSystem : GameRuleSystem<VaultRuleComponent>
{
    [Dependency] private readonly IConfigurationManager _cfg = default!;
    [Dependency] private readonly IObjectivesManager _objectivesManager = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<RoundStartAttemptEvent>(OnStartAttempt);
        SubscribeLocalEvent<RulePlayerJobsAssignedEvent>(OnPlayersSpawned);
        SubscribeLocalEvent<PlayerSpawnCompleteEvent>(HandleLatejoin);
        SubscribeLocalEvent<RoundEndTextAppendEvent>(OnRoundEndText);
    }

    protected override void Started(EntityUid uid, VaultRuleComponent vaultRule, GameRuleComponent gameRule, GameRuleStartedEvent ev){}

    protected override void Ended(EntityUid uid, VaultRuleComponent vaultRule, GameRuleComponent gameRule, GameRuleEndedEvent ev){}

    private void OnStartAttempt(RoundStartAttemptEvent ev)
    {
        var query = EntityQueryEnumerator<WaveDefenseRuleComponent, GameRuleComponent>();
        while (query.MoveNext(out var uid, out var waves, out var gameRule))
        {
            if (!GameTicker.IsGameRuleAdded(uid, gameRule))

                if (!ev.Forced)
                {
                    // TODO: If there is no overseer readied up, refuse to start
                    //ev.Cancel();
                }

            // TODO: Generate shared objectives here
        }
    }

    private void HandleLatejoin(PlayerSpawnCompleteEvent ev)
    {
        var query = EntityQueryEnumerator<WaveDefenseRuleComponent, GameRuleComponent>();
        while (query.MoveNext(out var uid, out var vault, out var gameRule))
        {
            if (!GameTicker.IsGameRuleAdded(uid, gameRule))

                GiveObjectives(ev.Player);
        }
    }

    private void OnPlayersSpawned(RulePlayerJobsAssignedEvent ev)
    {
        var query = EntityQueryEnumerator<WaveDefenseRuleComponent, GameRuleComponent>();
        while (query.MoveNext(out var uid, out var vault, out var gameRule))
        {
            if (!GameTicker.IsGameRuleAdded(uid, gameRule))

                foreach (var player in ev.Players)
                {
                    GiveObjectives(player);
                }
        }
    }

    public bool GiveObjectives(IPlayerSession player)
    {
        var mind = player.Data.ContentData()?.Mind;
        if (mind == null)
        {
            return false;
        }

        if (mind.OwnedEntity is not { } entity)
        {
            return false;
        }

        const int maxPicks = 3;
        const float maxDifficulty = 1f;
        var difficulty = 0f;
        for (var pick = 0; pick < maxPicks && maxDifficulty > difficulty; pick++)
        {
            var objective = _objectivesManager.GetRandomObjective(mind, "OverseerObjectiveGroups");
            if (objective == null) continue;
            //ToDo: Kevin Zheng: re-add the mind objective stuff here please, IDK how that all changed I havent touched the objective code.
            // if (mind.TryAddObjective(objective))
            //     difficulty += objective.Difficulty;
        }

        return true;
    }

    private void OnRoundEndText(RoundEndTextAppendEvent ev)
    {
        var query = EntityQueryEnumerator<WaveDefenseRuleComponent, GameRuleComponent>();
        while (query.MoveNext(out var uid, out var vault, out var gameRule))
        {
            if (!GameTicker.IsGameRuleAdded(uid, gameRule))
                return;


            // TODO: Round-end text
        }
    }
}
