/*
 * This file is sublicensed under MIT License
 * https://github.com/space-wizards/space-station-14/blob/master/LICENSE.TXT
 */

using System.Linq;
using Content.Server.Temperature.Systems;
using Content.Shared._CP14.Cooking;
using Content.Shared._CP14.Cooking.Components;
using Content.Shared._CP14.Temperature;
using Content.Shared.Random.Helpers;
using Robust.Shared.Random;

namespace Content.Server._CP14.Cooking;

public sealed class CP14CookingSystem : CP14SharedCookingSystem
{
    [Dependency] private readonly TemperatureSystem _temperature = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<CP14RandomFoodDataComponent, MapInitEvent>(OnRandomFoodMapInit);
    }

    private void OnRandomFoodMapInit(Entity<CP14RandomFoodDataComponent> ent, ref MapInitEvent args)
    {
        if (!TryComp<CP14FoodHolderComponent>(ent, out var holder))
            return;

        if (!_random.Prob(ent.Comp.Prob))
            return;

        var randomFood = _random.Pick(OrderedRecipes.Where(r => r.FoodType == holder.FoodType).ToList());

        //Name and Description
        if (randomFood.FoodData.Name is not null)
            _metaData.SetEntityName(ent, Loc.GetString(randomFood.FoodData.Name));
        if (randomFood.FoodData.Desc is not null)
            _metaData.SetEntityDescription(ent, Loc.GetString(randomFood.FoodData.Desc));

        //Visuals
        holder.Visuals = randomFood.FoodData.Visuals;
        Dirty(ent.Owner, holder);
    }

    protected override void OnCookBurned(Entity<CP14FoodCookerComponent> ent, ref CP14BurningDoAfter args)
    {
        base.OnCookBurned(ent, ref args);

        if (_random.Prob(ent.Comp.BurntAdditionalSpawnProb))
            Spawn(ent.Comp.BurntAdditionalSpawn, Transform(ent).Coordinates);
    }

    protected override void OnCookFinished(Entity<CP14FoodCookerComponent> ent, ref CP14CookingDoAfter args)
    {
        base.OnCookFinished(ent, ref args);

        if (args.Cancelled || args.Handled)
            return;

        TryTransformAll(ent);
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

            _temperature.ForceChangeTemperature(contained, entry.TemperatureRange.X);
        }
    }
}
