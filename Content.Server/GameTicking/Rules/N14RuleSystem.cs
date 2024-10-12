using Content.Server.GameTicking.Rules.Components;
using Content.Server.Mind;
using Content.Server.Objectives;

namespace Content.Server.GameTicking.Rules;

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
        var query = EntityQueryEnumerator<N14RuleComponent>();
        while (query.MoveNext(out var uid, out var rule))
        {
            if (_mindSystem.TryGetMind(args.Player, out var mindId, out var mind))
            {
                var objective = _objectives.GetRandomObjective(mindId, mind, "N14Objectives");
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

            // break out of loop: we only need to do this once
            break;
        }
    }
}
