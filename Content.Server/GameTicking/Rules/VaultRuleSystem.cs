using Content.Server.Objectives.Interfaces;
using Content.Server.Players;
using Robust.Server.Player;
using Robust.Shared.Configuration;

namespace Content.Server.GameTicking.Rules;

/// <summary>
/// Assign vault dwellers objectives at round-start and late-join.
/// </summary>
public sealed class VaultRuleSystem : GameRuleSystem
{
    [Dependency] private readonly IConfigurationManager _cfg = default!;
    [Dependency] private readonly IObjectivesManager _objectivesManager = default!;

    public override string Prototype => "Vault";

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<RoundStartAttemptEvent>(OnStartAttempt);
        SubscribeLocalEvent<RulePlayerJobsAssignedEvent>(OnPlayersSpawned);
        SubscribeLocalEvent<PlayerSpawnCompleteEvent>(HandleLatejoin);
        SubscribeLocalEvent<RoundEndTextAppendEvent>(OnRoundEndText);
    }

    public override void Started(){}

    public override void Ended(){}

    private void OnStartAttempt(RoundStartAttemptEvent ev)
    {
        if (!RuleAdded)
            return;

        if (!ev.Forced)
        {
            // TODO: If there is no overseer readied up, refuse to start
            //ev.Cancel();
        }

        // TODO: Generate shared objectives here
    }

    private void HandleLatejoin(PlayerSpawnCompleteEvent ev)
    {
        if (!RuleAdded)
            return;

        GiveObjectives(ev.Player);
    }

    private void OnPlayersSpawned(RulePlayerJobsAssignedEvent ev)
    {
        if (!RuleAdded)
            return;

        foreach (var player in ev.Players)
        {
            GiveObjectives(player);
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
            if (mind.TryAddObjective(objective))
                difficulty += objective.Difficulty;
        }

        return true;
    }

    private void OnRoundEndText(RoundEndTextAppendEvent ev)
    {
        if (!RuleAdded)
            return;

        // TODO: Round-end text
    }
}
