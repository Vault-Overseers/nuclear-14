/*
 * This file is sublicensed under MIT License
 * https://github.com/space-wizards/space-station-14/blob/master/LICENSE.TXT
 */

using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.FixedPoint;
using Content.Shared.Tag;
using Robust.Shared.Prototypes;
using YamlDotNet.Serialization.Schemas;

namespace Content.Shared._CP14.Cooking.Requirements;

public sealed partial class ReagentRequired : CP14CookingCraftRequirement
{
    [DataField(required: true)]
    public HashSet<ProtoId<ReagentPrototype>> Reagents = default!;

    [DataField]
    public FixedPoint2 Amount = 10f;

    public override bool CheckRequirement(IEntityManager entManager,
        IPrototypeManager protoManager,
        List<ProtoId<TagPrototype>> placedTags,
        Solution? solution = null)
    {
        if (solution is null)
            return false;

        var passed = false;
        foreach (var (reagent, quantity) in solution.Contents)
        {
            if (!Reagents.Contains(reagent.Prototype))
                continue;

            if (quantity < Amount)
                continue;

            passed = true;
            break;
        }

        return passed;
    }

    public override float GetComplexity()
    {
        return 1;
    }
}
