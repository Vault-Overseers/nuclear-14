using Content.Server._N14.GameTicking.Rules.Components;
using Content.Server.GameTicking.Rules;
using Content.Server.GameTicking.Rules.Components;
using Content.Server.Mind;
using Content.Server.Objectives;
using Content.Shared.GameTicking;

namespace Content.Server._N14.GameTicking.Rules;

/// <summary>
/// Assigns a random N14 objective to players when they spawn.
/// </summary>
public sealed class N14RuleSystem : GameRuleSystem<N14RuleComponent>
{
    [Dependency] private readonly MindSystem _mindSystem = default!;
    [Dependency] private readonly ObjectivesSystem _objectives = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<PlayerSpawnCompleteEvent>(OnSpawnComplete);
    }

    private void OnSpawnComplete(PlayerSpawnCompleteEvent args)
    {
        if (_mindSystem.TryGetMind(args.Player, out var mindId, out var mind))
        {
            var objective = _objectives.GetRandomObjective(mindId, mind, "N14Objectives", float.MaxValue);
            if (objective != null)
            {
                Logger.DebugS("n14rule", $"Added objective {objective.Value} for {args.Player}");
                _mindSystem.AddObjective(mindId, mind, objective.Value);
            }
            else
            {
                Logger.DebugS("n14rule", $"No suitable objectives found for {args.Player}");
            }
        }
        else
        {
            Logger.DebugS("n14rule", $"{args.Player} has no mind");
        }
    }
}
