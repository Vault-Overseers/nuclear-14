using Content.Server._N14.Objectives.Components;
using Content.Server.Mind;
using Content.Shared.Objectives.Components;


namespace Content.Server._N14.Objectives.Systems;

public sealed class FactionRequirementSystem : EntitySystem
{
    [Dependency] private readonly MindSystem _mind = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<FactionRequirementComponent, RequirementCheckEvent>(OnCheck);
    }

    private void OnCheck(EntityUid uid, FactionRequirementComponent comp, ref RequirementCheckEvent args)
    {
        if (args.Cancelled)
            return;

        if (!_mind.InFaction(args.MindId, args.Mind, comp.Factions))
            args.Cancelled = true;
    }
}
