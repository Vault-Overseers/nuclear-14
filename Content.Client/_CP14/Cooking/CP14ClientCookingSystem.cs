/*
 * This file is sublicensed under MIT License
 * https://github.com/space-wizards/space-station-14/blob/master/LICENSE.TXT
 */

using System.Numerics;
using Content.Shared._CP14.Cooking;
using Content.Shared._CP14.Cooking.Components;
using Content.Shared.Rounding;
using Robust.Client.GameObjects;
using Robust.Shared.Random;

namespace Content.Client._CP14.Cooking;

public sealed class CP14ClientCookingSystem : CP14SharedCookingSystem
{
    [Dependency] private readonly SpriteSystem _sprite = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<CP14FoodVisualsComponent, AfterAutoHandleStateEvent>(OnAfterHandleState);
        SubscribeLocalEvent<CP14FoodVisualsComponent, AppearanceChangeEvent>(OnAppearanceChange);
    }

    private void OnAppearanceChange(Entity<CP14FoodVisualsComponent> ent, ref AppearanceChangeEvent args)
    {
        var solutionId = string.Empty;

        if (TryComp<CP14FoodHolderComponent>(ent, out var holder))
            solutionId = holder.SolutionId;
        else if (TryComp<CP14FoodCookerComponent>(ent, out var cooker))
            solutionId = cooker.SolutionId;

        UpdateVisuals(
            ent,
            solutionId,
            ref ent.Comp.RevealedLayers,
            ent.Comp.TargetLayerMap,
            ent.Comp.MaxDisplacementFillLevels,
            ent.Comp.DisplacementRsiPath,
            ent.Comp.FoodData);
    }

    private void OnAfterHandleState(Entity<CP14FoodVisualsComponent> ent, ref AfterAutoHandleStateEvent args)
    {
        var solutionId = string.Empty;

        if (TryComp<CP14FoodHolderComponent>(ent, out var holder))
            solutionId = holder.SolutionId;
        else if (TryComp<CP14FoodCookerComponent>(ent, out var cooker))
            solutionId = cooker.SolutionId;

        UpdateVisuals(
            ent,
            solutionId,
            ref ent.Comp.RevealedLayers,
            ent.Comp.TargetLayerMap,
            ent.Comp.MaxDisplacementFillLevels,
            ent.Comp.DisplacementRsiPath,
            ent.Comp.FoodData);
    }

    private void UpdateVisuals(
        EntityUid ent,
        string? solutionId,
        ref HashSet<string> revealedLayers,
        string targetLayerMap,
        int maxDisplacementFillLevels,
        string? displacementRsiPath,
        CP14FoodData? data)
    {
        if (!TryComp<SpriteComponent>(ent, out var sprite))
            return;

        //Remove old layers
        foreach (var key in revealedLayers)
        {
            _sprite.RemoveLayer((ent, sprite), key);
        }

        revealedLayers.Clear();

        if (data is null)
            return;

        if (!_solution.TryGetSolution(ent, solutionId, out var soln, out var solution))
            return;

        _sprite.LayerMapTryGet((ent, sprite), targetLayerMap, out var index, false);

        var fillLevel = (float)solution.Volume / (float)solution.MaxVolume;
        if (fillLevel > 1)
            fillLevel = 1;

        var closestFillSprite = ContentHelpers.RoundToLevels(fillLevel, 1, maxDisplacementFillLevels + 1);

        //Add new layers
        var counter = 0;
        foreach (var layer in data.Visuals)
        {
            var layerIndex = index + counter;
            var keyCode = $"cp14-food-layer-{counter}";
            revealedLayers.Add(keyCode);

            _sprite.AddBlankLayer((ent, sprite), layerIndex);
            _sprite.LayerMapSet((ent, sprite), keyCode, layerIndex);
            _sprite.LayerSetData((ent, sprite), layerIndex, layer);

            // Displacement map support pending upstream implementation

            counter++;
        }
    }
}
