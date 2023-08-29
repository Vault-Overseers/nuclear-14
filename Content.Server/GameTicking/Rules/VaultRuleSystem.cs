using System.Linq;
using Content.Server.Chat.Managers;
using Content.Server.GameTicking.Events;
using Content.Server.GameTicking.Rules.Components;
using Content.Server.NPC.Components;
using Content.Server.NPC.Systems;
using Content.Server.Objectives.Interfaces;
using Content.Server.Players;
using Content.Server.Roles;
using Content.Shared.CCVar;
using Content.Shared.Roles;
using Robust.Server.Player;
using Robust.Shared.Configuration;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Server.GameTicking.Rules;

/// <summary>
/// Assign vault dwellers objectives at round-start and late-join.
/// </summary>
public sealed class FactionRuleSystem : GameRuleSystem<FactionRuleComponent>
{
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly IConfigurationManager _cfg = default!;
    [Dependency] private readonly IObjectivesManager _objectivesManager = default!;
    [Dependency] private readonly IChatManager _chatManager = default!;
    [Dependency] private readonly IGameTiming _gameTiming = default!;
    [Dependency] private readonly NpcFactionSystem _npcFaction = default!;

    private ISawmill _sawmill = default!;

    private Dictionary<NpcFactionPrototype, N14FactionGoalsComponent>

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<RoundStartAttemptEvent>(OnStartAttempt);
        SubscribeLocalEvent<RulePlayerJobsAssignedEvent>(OnPlayersSpawned);
        SubscribeLocalEvent<PlayerSpawnCompleteEvent>(HandleLatejoin);
        SubscribeLocalEvent<RoundEndTextAppendEvent>(OnRoundEndText);
    }

    protected override void Started(EntityUid uid, FactionRuleComponent vaultRule, GameRuleComponent gameRule, GameRuleStartedEvent ev){}

    protected override void Ended(EntityUid uid, FactionRuleComponent vaultRule, GameRuleComponent gameRule, GameRuleEndedEvent ev){}

    protected override void ActiveTick(EntityUid uid, FactionRuleComponent component, GameRuleComponent gameRule, float frameTime)
    {
        base.ActiveTick(uid, component, gameRule, frameTime);

        if (component.SelectionStatus == FactionRuleComponent.SelectionState.ReadyToSelect && _gameTiming.CurTime > component.AnnounceAt)
            GenerateFactionGoals(component);
    }

    private void OnStartAttempt(RoundStartAttemptEvent ev)
    {
        var query = EntityQueryEnumerator<FactionRuleComponent, GameRuleComponent>();
        while (query.MoveNext(out var uid, out var factions, out var gameRule))
        {
            var minPlayers = _cfg.GetCVar(CCVars.FactionMinPlayers);
            if (!ev.Forced && ev.Players.Length < minPlayers)
            {
                _chatManager.SendAdminAnnouncement(Loc.GetString("factions-not-enough-ready-players",
                    ("readyPlayersCount", ev.Players.Length), ("minimumPlayers", minPlayers)));
                ev.Cancel();
                continue;
            }
            if (ev.Players.Length == 0)
            {
                _chatManager.DispatchServerAnnouncement(Loc.GetString("traitor-no-one-ready"));
                ev.Cancel();
            }
            // TODO: Generate shared objectives here
        }
    }

    private void HandleLatejoin(PlayerSpawnCompleteEvent ev)
    {
        var query = EntityQueryEnumerator<FactionRuleComponent, GameRuleComponent>();
        while (query.MoveNext(out var uid, out var vault, out var gameRule))
        {
            if (!GameTicker.IsGameRuleAdded(uid, gameRule))
                continue;

            if (!ev.LateJoin)
                continue;
            if (ev.JobId == null || !_prototypeManager.TryIndex<JobPrototype>(ev.JobId, out var job))
                continue;
            if (!TryComp<NpcFactionMemberComponent>(ev.Player.AttachedEntity, out var faction))
                continue;
            vault.PlayerList[ev.Player] = ev.Profile;
            if (vault.SelectionStatus == FactionRuleComponent.SelectionState.SelectionMade)
                GiveObjectives(ev.Player);
        }
    }

    private void OnPlayersSpawned(RulePlayerJobsAssignedEvent ev)
    {
        var query = EntityQueryEnumerator<FactionRuleComponent, GameRuleComponent>();
        while (query.MoveNext(out var uid, out var vault, out var gameRule))
        {
            if (!GameTicker.IsGameRuleAdded(uid, gameRule))
                continue;

            foreach (var player in ev.Players)
            {
                if (!ev.Profiles.ContainsKey(player.UserId))
                    continue;
                if (!TryComp<NpcFactionMemberComponent>(player.AttachedEntity, out var faction))
                    continue;
                vault.PlayerList[player] = ev.Profiles[player.UserId];
                //GiveGoal(player);  if we have to somehow grab and give the npcfactionmembercomponent another way
            }
        }
    }
    private void GenerateFactionGoals(FactionRuleComponent component)
    {
        if (!component.PlayerList.Any())
        {
            _sawmill.Error("Tried to start Faction mode without any candidates.");
            return;
        }

        var numTraitors = MathHelper.Clamp(component.StartCandidates.Count / PlayersPerTraitor, 1, MaxTraitors);
        var traitorPool = FindPotentialTraitors(component.StartCandidates, component);
        var selectedTraitors = PickTraitors(numTraitors, traitorPool);

        foreach (var traitor in selectedTraitors)
        {
            MakeTraitor(traitor);
        }

        component.SelectionStatus = TraitorRuleComponent.SelectionState.SelectionMade;
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
