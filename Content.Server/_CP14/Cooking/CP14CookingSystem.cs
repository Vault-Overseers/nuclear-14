/*
 * This file is sublicensed under MIT License
 * https://github.com/space-wizards/space-station-14/blob/master/LICENSE.TXT
 */

using System.Linq;
using System.Numerics;
using Content.Server.Temperature.Systems;
using Content.Shared._CP14.Cooking;
using Content.Shared._CP14.Cooking.Components;
using Content.Shared._CP14.Temperature;
using Content.Shared.Nutrition.Components;
using Robust.Shared.Random;

namespace Content.Server._CP14.Cooking;

public sealed class CP14CookingSystem : CP14SharedCookingSystem
{
    [Dependency] private readonly TemperatureSystem _temperature = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<CP14RandomFoodDataComponent, MapInitEvent>(OnRandomFoodMapInit);

        SubscribeLocalEvent<CP14FoodVisualsComponent, CP14BeforeSpillEvent>(OnSpilled);
        SubscribeLocalEvent<CP14FoodHolderComponent, CP14BeforeSpillEvent>(OnHolderSpilled);
        SubscribeLocalEvent<CP14FoodCookerComponent, CP14BeforeSpillEvent>(OnCookerSpilled);
    }

    private void OnCookerSpilled(Entity<CP14FoodCookerComponent> ent, ref CP14BeforeSpillEvent args)
    {
        ent.Comp.HoldFood = false;
        Dirty(ent);
    }

    private void OnHolderSpilled(Entity<CP14FoodHolderComponent> ent, ref CP14BeforeSpillEvent args)
    {
        ent.Comp.HoldFood = false;
        Dirty(ent);
    }

    private void OnSpilled(Entity<CP14FoodVisualsComponent> ent, ref CP14BeforeSpillEvent args)
    {
        ent.Comp.FoodData = null;
        Dirty(ent);
    }

    private void OnRandomFoodMapInit(Entity<CP14RandomFoodDataComponent> ent, ref MapInitEvent args)
    {
        if (!TryComp<CP14FoodHolderComponent>(ent, out var holder))
            return;

        if (!_random.Prob(ent.Comp.Prob))
            return;

        //TODO: Fuck this dublication logic, and randomization visual
        var randomFood = _random.Pick(OrderedRecipes.Where(r => r.FoodType == holder.FoodType).ToList());

        //Name and Description
        if (randomFood.FoodData.Name is not null)
            _metaData.SetEntityName(ent, Loc.GetString(randomFood.FoodData.Name));
        if (randomFood.FoodData.Desc is not null)
            _metaData.SetEntityDescription(ent, Loc.GetString(randomFood.FoodData.Desc));

        var foodVisuals = EnsureComp<CP14FoodVisualsComponent>(ent);
        //Visuals
        foodVisuals.FoodData = randomFood.FoodData;

        //Some randomize
        foreach (var layer in foodVisuals.FoodData.Visuals)
        {
            if (_random.Prob(0.5f))
                layer.Scale = new Vector2(-1, 1);
        }

        Dirty(ent.Owner, holder);
    }

    protected override void OnCookBurned(Entity<CP14FoodCookerComponent> ent, ref CP14BurningDoAfter args)
    {
        if (args.Cancelled || args.Handled)
            return;

        base.OnCookBurned(ent, ref args);

        if (_random.Prob(ent.Comp.BurntAdditionalSpawnProb))
            Spawn(ent.Comp.BurntAdditionalSpawn, Transform(ent).Coordinates);
    }

    protected override void OnCookFinished(Entity<CP14FoodCookerComponent> ent, ref CP14CookingDoAfter args)
    {
        if (args.Cancelled || args.Handled)
            return;

        //We need transform all BEFORE Shared cooking code
        TryTransformAll(ent);

        base.OnCookFinished(ent, ref args);
    }

    private void TryTransformAll(Entity<CP14FoodCookerComponent> ent)
    {
        if (!_container.TryGetContainer(ent, ent.Comp.ContainerId, out var container))
            return;

        var containedEntities = container.ContainedEntities.ToList();

        foreach (var contained in containedEntities)
        {
            if (!TryComp<CP14TemperatureTransformationComponent>(contained, out var transformable))
                continue;

            if (!transformable.AutoTransformOnCooked)
                continue;

            if (transformable.Entries.Count == 0)
                continue;

            var entry = transformable.Entries[0];

            var newTemp = (entry.TemperatureRange.X + entry.TemperatureRange.Y) / 2;
            _temperature.ForceChangeTemperature(contained, newTemp);
        }
    }
}

/// <summary>
/// It is invoked on the entity from which all reagents are spilled.
/// </summary>
public sealed class CP14BeforeSpillEvent : EntityEventArgs
{
}
