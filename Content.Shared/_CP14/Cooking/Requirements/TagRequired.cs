/*
 * This file is sublicensed under MIT License
 * https://github.com/space-wizards/space-station-14/blob/master/LICENSE.TXT
 */

using Content.Shared.Chemistry.Components;
using Content.Shared.Tag;
using Robust.Shared.Prototypes;

namespace Content.Shared._CP14.Cooking.Requirements;

public sealed partial class TagRequired : CP14CookingCraftRequirement
{
    /// <summary>
    /// Any of this tags accepted
    /// </summary>
    [DataField(required: true)]
    public List<ProtoId<TagPrototype>> Tags = default!;

    [DataField]
    public bool AllowOtherTags = true;

    public override bool CheckRequirement(IEntityManager entManager,
        IPrototypeManager protoManager,
        List<ProtoId<TagPrototype>> placedTags,
        Solution? solution = null)
    {
        foreach (var placedTag in placedTags)
        {
            if (Tags.Contains(placedTag))
                return true;
        }

        return false;
    }

    public override float GetComplexity()
    {
        return AllowOtherTags ? 5 : 1;
    }
}
