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
    public byte Min = 1;

    [DataField]
    public byte Max = 255;

    [DataField]
    public bool AllowOtherTags = true;

    public override bool CheckRequirement(IEntityManager entManager,
        IPrototypeManager protoManager,
        IReadOnlyList<EntityUid> placedEntities,
        List<ProtoId<TagPrototype>> placedTags,
        Solution? solution = null)
    {
        var count = 0;
        foreach (var placedTag in placedTags)
        {
            if (Tags.Contains(placedTag))
            {
                count++;
            }
            else
            {
                if (!AllowOtherTags)
                    return false; // If we don't allow other tags, and we found one that isn't in the required list, fail.
            }
        }

        return count >= Min && count <= Max;
    }

    public override float GetComplexity()
    {
        return Min;
    }
}
