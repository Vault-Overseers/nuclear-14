using Content.Server.GameTicking.Events;
using Content.Server.GameTicking.Rules.Components;
using Content.Server.Objectives;
using Content.Server.Players;
using Content.Shared.Players;
using Content.Shared.Mind;
using Robust.Server.Player;
using Robust.Shared.Player;
using Robust.Shared.Configuration;
using Content.Shared.Objectives.Components;

namespace Content.Server.GameTicking.Rules;

/// <summary>
/// Assign vault dwellers objectives at round-start and late-join.
/// </summary>
public sealed class VaultRuleSystem : GameRuleSystem<VaultRuleComponent>
{
    [Dependency] private readonly IConfigurationManager _cfg = default!;
    [Dependency] private readonly ObjectivesSystem _objectives = default!;

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

    public bool GiveObjectives(ICommonSession player)
    {
        var mind = player.ContentData()?.Mind;
        if (mind == null)
        {
            return false;
        }
        var minds = IoCManager.Resolve<IEntityManager>().System<SharedMindSystem>();

        if (!minds.TryGetMind(player, out var mindId, out var mindEnt) || mindEnt.OwnedEntity is not { } entity)
        {
            return false;
        }

        const int maxPicks = 3;
        const float maxDifficulty = 1f;
        var difficulty = 0f;
        for (var pick = 0; pick < maxPicks && maxDifficulty > difficulty; pick++)
        {
            var objective = _objectives.GetRandomObjective(mindId, mindEnt, "OverseerObjectiveGroups");
            if (objective == null) continue;
            //ToDo: Kevin Zheng: re-add the mind objective stuff here please, IDK how that all changed I havent touched the objective code.
            // _mindSystem.AddObjective(mindId, mind, objective.Value);
            // difficulty += Comp<ObjectiveComponent>(objective.Value).Difficulty;
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
