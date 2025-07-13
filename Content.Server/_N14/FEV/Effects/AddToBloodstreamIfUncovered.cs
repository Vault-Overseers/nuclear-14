using Content.Server.Inventory;
using Content.Server.Nutrition.Components;
using Content.Server.Chemistry.Containers.EntitySystems;
using Content.Shared.EntityEffects;
using Content.Shared.Inventory;
using Content.Shared.IdentityManagement.Components;
using Content.Shared.Tag;
using Content.Shared.FixedPoint;
using JetBrains.Annotations;
using Robust.Shared.Prototypes;

namespace Content.Server._N14.FEV.Effects;

/// <summary>
/// Adds a specified reagent to the target's bloodstream if their head is not fully covered.
/// Coverage is checked via <see cref="IdentityBlockerComponent"/>, <see cref="IngestionBlockerComponent"/>
/// or a <see cref="TagComponent"/> with the "HidesHair" tag on the head slot item.
/// </summary>
[UsedImplicitly]
public sealed partial class AddToBloodstreamIfUncovered : EntityEffect
{
    [DataField(required: true, customTypeSerializer: typeof(PrototypeIdSerializer<ReagentPrototype>))]
    private string _reagent = default!;

    [DataField] private FixedPoint2 _amount = FixedPoint2.New(5);

    [DataField] private string _solution = "bloodstream";

    public override void Effect(EntityEffectBaseArgs args)
    {
        var entMan = args.EntityManager;
        var inv = entMan.System<InventorySystem>();
        var tag = entMan.System<TagSystem>();
        var solutionSys = entMan.System<SolutionContainerSystem>();

        if (!solutionSys.TryGetSolution(args.TargetEntity, _solution, out _, out var solution))
            return;

        bool covered = false;
        if (inv.TryGetSlotEntity(args.TargetEntity, "head", out var headUid))
        {
            if (entMan.HasComponent<IdentityBlockerComponent>(headUid))
                covered = true;
            else if (entMan.TryGetComponent(headUid, out IngestionBlockerComponent? ingest) && ingest.Enabled)
                covered = true;
            else if (tag.HasTag(headUid, "HidesHair"))
                covered = true;
        }

        if (covered)
            return;

        var amount = _amount;
        if (args is EntityEffectReagentArgs rArgs)
            amount = rArgs.Quantity;

        solutionSys.TryAddReagent(solution.Value, _reagent, amount, out var accepted);

        if (args is EntityEffectReagentArgs removeArgs)
            removeArgs.Source?.RemoveReagent(_reagent, accepted);
    }

    protected override string? ReagentEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys) => null;
}
