/*
 * This file is sublicensed under MIT License
 * https://github.com/space-wizards/space-station-14/blob/master/LICENSE.TXT
 */

using System.Linq;
using Content.Shared._CP14.Cooking;
using Content.Shared._CP14.Cooking.Components;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.FixedPoint;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Shared._CP14.Workbench.Requirements;

public sealed partial class FoodResource : CP14WorkbenchCraftRequirement
{
    [DataField(required: true)]
    public ProtoId<CP14CookingRecipePrototype> Recipe;

    [DataField]
    public FixedPoint2 Count = 1;

    public override bool CheckRequirement(IEntityManager entManager,
        IPrototypeManager protoManager,
        HashSet<EntityUid> placedEntities)
    {
        var solutionSys = entManager.System<SharedSolutionContainerSystem>();
        foreach (var ent in placedEntities)
        {
            if (!entManager.TryGetComponent<CP14FoodVisualsComponent>(ent, out var foodVisuals))
                continue;

            if (entManager.HasComponent<CP14FoodHolderComponent>(ent))
                continue;

            if (foodVisuals.FoodData?.CurrentRecipe != Recipe)
                continue;

            if (!solutionSys.TryGetSolution(ent, foodVisuals.SolutionId, out _, out var solution))
                continue;

            if (solution.Volume < Count)
                continue;

            return true;
        }

        return false;
    }

    public override void PostCraft(IEntityManager entManager,
        IPrototypeManager protoManager,
        HashSet<EntityUid> placedEntities)
    {
        var solutionSys = entManager.System<SharedSolutionContainerSystem>();

        foreach (var ent in placedEntities)
        {
            if (!entManager.TryGetComponent<CP14FoodVisualsComponent>(ent, out var foodVisuals))
                continue;

            if (entManager.HasComponent<CP14FoodHolderComponent>(ent))
                continue;

            if (foodVisuals.FoodData?.CurrentRecipe != Recipe)
                continue;

            if (!solutionSys.TryGetSolution(ent, foodVisuals.SolutionId, out _, out var solution))
                continue;

            if (solution.Volume < Count)
                continue;

            entManager.DeleteEntity(ent);
            return;
        }
    }

    public override double GetPrice(IEntityManager entManager,
        IPrototypeManager protoManager)
    {
        if (!protoManager.TryIndex(Recipe, out var indexedRecipe))
            return 0;

        var complexity = indexedRecipe.Requirements.Sum(req => req.GetComplexity());

        return complexity * 10;
    }

    public override string GetRequirementTitle(IPrototypeManager protoManager)
    {
        if (!protoManager.TryIndex(Recipe, out var indexedRecipe))
            return "Unknown Recipe";

        return $"{Loc.GetString(indexedRecipe.FoodData.Name ?? "Unknown Food")} ({Count}u)";
    }

    public override SpriteSpecifier? GetRequirementTexture(IPrototypeManager protoManager)
    {
        if (!protoManager.TryIndex(Recipe, out var indexedRecipe))
            return null;

        var firstLayer = indexedRecipe.FoodData.Visuals.First();

        return new SpriteSpecifier.Rsi(new(firstLayer.RsiPath ?? ""), firstLayer.State ?? "");
    }

    public override Color GetRequirementColor(IPrototypeManager protoManager)
    {
        if (!protoManager.TryIndex(Recipe, out var indexedRecipe))
            return Color.White;

        var firstLayer = indexedRecipe.FoodData.Visuals.First();
        return firstLayer.Color ?? Color.White;
    }
}
