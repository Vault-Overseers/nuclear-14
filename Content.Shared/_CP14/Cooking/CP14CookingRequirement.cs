/*
 * This file is sublicensed under MIT License
 * https://github.com/space-wizards/space-station-14/blob/master/LICENSE.TXT
 */

using Content.Shared.Chemistry.Components;
using Content.Shared.Tag;
using JetBrains.Annotations;
using Robust.Shared.Prototypes;

namespace Content.Shared._CP14.Cooking;

/// <summary>
/// An abstract condition that is a key element of the system. The more complex the conditions for a recipe,
/// the more difficult it is to “get” that recipe by collecting ingredients at random.
/// The system automatically calculates the complexity of a recipe using GetComplexity() for each condition.
/// </summary>
[ImplicitDataDefinitionForInheritors]
[MeansImplicitUse]
public abstract partial class CP14CookingCraftRequirement
{
    public abstract bool CheckRequirement(IEntityManager entManager,
        IPrototypeManager protoManager,
        IReadOnlyList<EntityUid> placedEntities,
        List<ProtoId<TagPrototype>> placedTags,
        Solution? solution = null);

    public abstract float GetComplexity();
}
