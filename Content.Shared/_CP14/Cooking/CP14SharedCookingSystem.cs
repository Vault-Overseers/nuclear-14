/*
 * This file is sublicensed under MIT License
 * https://github.com/space-wizards/space-station-14/blob/master/LICENSE.TXT
 */

using System.Linq;
using Content.Shared._CP14.Cooking.Components;
using Content.Shared.Audio;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.DoAfter;
using Content.Shared.Examine;
using Content.Shared.Fluids;
using Content.Shared.Nutrition.Components;
using Content.Shared.Popups;
using Content.Shared.Tag;
using Content.Shared.Throwing;
using Robust.Shared.Containers;
using Robust.Shared.Network;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Shared._CP14.Cooking;

public abstract partial class CP14SharedCookingSystem : EntitySystem
{
    [Dependency] protected readonly IPrototypeManager _proto = default!;
    [Dependency] protected readonly SharedContainerSystem _container = default!;
    [Dependency] protected readonly IRobustRandom _random = default!;
    [Dependency] protected readonly MetaDataSystem _metaData = default!;
    [Dependency] private readonly SharedSolutionContainerSystem _solution = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedPuddleSystem _puddle = default!;
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;
    [Dependency] private readonly SharedAmbientSoundSystem _ambientSound = default!;
    [Dependency] protected readonly SharedAppearanceSystem _appearance = default!;

    /// <summary>
    /// When overcooking food, we will replace the reagents inside with this reagent.
    /// </summary>
    private readonly ProtoId<ReagentPrototype> _burntFoodReagent = "CP14BurntFood";

    /// <summary>
    /// Stores a list of all recipes sorted by complexity: the most complex ones at the beginning.
    /// When attempting to cook, the most complex recipes will be checked first,
    /// gradually moving down to the easiest ones.
    /// The easiest recipes are usually the most “abstract,”
    /// so they will be suitable for the largest number of recipes.
    /// </summary>
    protected List<CP14CookingRecipePrototype> OrderedRecipes = [];

    public override void Initialize()
    {
        base.Initialize();
        InitTransfer();
        InitDoAfter();

        CacheAndOrderRecipes();

        SubscribeLocalEvent<PrototypesReloadedEventArgs>(OnPrototypesReloaded);
        SubscribeLocalEvent<CP14FoodCookerComponent, ExaminedEvent>(OnExaminedEvent);
        SubscribeLocalEvent<CP14FoodCookerComponent, LandEvent>(OnLand);
    }

    public override void Update(float frameTime)
    {
        UpdateDoAfter(frameTime);
    }

    private void CacheAndOrderRecipes()
    {
        OrderedRecipes = _proto.EnumeratePrototypes<CP14CookingRecipePrototype>()
            .Where(recipe => recipe.Requirements.Count > 0) // Only include recipes with requirements
            .OrderByDescending(recipe => recipe.Requirements.Sum(condition => condition.GetComplexity()))
            .ToList();
    }

    private void OnPrototypesReloaded(PrototypesReloadedEventArgs ev)
    {
        if (!ev.WasModified<EntityPrototype>())
            return;

        CacheAndOrderRecipes();
    }

    private void OnExaminedEvent(Entity<CP14FoodCookerComponent> ent, ref ExaminedEvent args)
    {
        if (ent.Comp.FoodData?.Name is null)
            return;

        if (!_solution.TryGetSolution(ent.Owner, ent.Comp.SolutionId, out _, out var solution))
            return;

        if (solution.Volume == 0)
            return;

        var remaining = solution.Volume;

        args.PushMarkup(Loc.GetString("cp14-cooking-examine",
            ("name", Loc.GetString(ent.Comp.FoodData.Name)),
            ("count", remaining)));
    }

    private void OnLand(Entity<CP14FoodCookerComponent> ent, ref LandEvent args)
    {
        ent.Comp.FoodData = null;
        Dirty(ent);
    }



    /// <summary>
    /// Transfer food data from cooker to holder
    /// </summary>
    private void MoveFoodToHolder(Entity<CP14FoodHolderComponent> ent, Entity<CP14FoodCookerComponent> cooker)
    {
        if (!TryComp<FoodComponent>(ent, out var foodComp))
            return;

        if (cooker.Comp.FoodData is null)
            return;

        if (!_solution.TryGetSolution(cooker.Owner, cooker.Comp.SolutionId, out _, out var cookerSolution))
            return;

        //Solutions
        if (_solution.TryGetSolution(ent.Owner, foodComp.Solution, out var soln, out var solution))
        {
            if (solution.Volume > 0)
            {
                _popup.PopupEntity(Loc.GetString("cp14-cooking-popup-not-empty", ("name", MetaData(ent).EntityName)),
                    ent);
                return;
            }

            _solution.TryTransferSolution(soln.Value, cookerSolution, solution.MaxVolume);
        }

        //Trash
        //If we have a lot of trash, we put 1 random trash in each plate. If it's a last plate (out of solution in cooker), we put all the remaining trash in it.
        if (cooker.Comp.FoodData.Trash.Count > 0)
        {
            if (cookerSolution.Volume <= 0)
            {
                foodComp.Trash.AddRange(cooker.Comp.FoodData.Trash);
            }
            else
            {
                if (_net.IsServer)
                {
                    var newTrash = _random.Pick(cooker.Comp.FoodData.Trash);
                    cooker.Comp.FoodData.Trash.Remove(newTrash);
                    foodComp.Trash.Add(newTrash);
                }
            }
        }

        //Name and Description
        if (cooker.Comp.FoodData.Name is not null)
            _metaData.SetEntityName(ent, Loc.GetString(cooker.Comp.FoodData.Name));
        if (cooker.Comp.FoodData.Desc is not null)
            _metaData.SetEntityDescription(ent, Loc.GetString(cooker.Comp.FoodData.Desc));

        //Flavors
        EnsureComp<FlavorProfileComponent>(ent, out var flavorComp);
        foreach (var flavor in cooker.Comp.FoodData.Flavors)
        {
            flavorComp.Flavors.Add(flavor);
        }

        //Visuals
        ent.Comp.Visuals = cooker.Comp.FoodData.Visuals;

        //Clear cooker data
        if (cookerSolution.Volume <= 0)
            cooker.Comp.FoodData = null;

        Dirty(ent);
        Dirty(cooker);
    }

    private CP14CookingRecipePrototype? GetRecipe(Entity<CP14FoodCookerComponent> ent)
    {
        if (!_container.TryGetContainer(ent, ent.Comp.ContainerId, out var container))
            return null;

        _solution.TryGetSolution(ent.Owner, ent.Comp.SolutionId, out _, out var solution);

        //Get all tags
        var allTags = new List<ProtoId<TagPrototype>>();
        foreach (var contained in container.ContainedEntities)
        {
            if (!TryComp<TagComponent>(contained, out var tags))
                continue;

            allTags.AddRange(tags.Tags);
        }

        if (OrderedRecipes.Count == 0)
        {
            throw new InvalidOperationException(
                "No cooking recipes found. Please ensure that the CP14CookingRecipePrototype is defined and loaded.");
        }

        CP14CookingRecipePrototype? selectedRecipe = null;
        foreach (var recipe in OrderedRecipes)
        {
            if (recipe.FoodType != ent.Comp.FoodType)
                continue;

            var conditionsMet = true;
            foreach (var condition in recipe.Requirements)
            {
                if (!condition.CheckRequirement(EntityManager, _proto, container.ContainedEntities, allTags, solution))
                {
                    conditionsMet = false;
                    break;
                }
            }

            if (!conditionsMet)
                continue;

            selectedRecipe = recipe;
            break;
        }

        return selectedRecipe;
    }

    protected void CookFood(Entity<CP14FoodCookerComponent> ent, CP14CookingRecipePrototype recipe)
    {
        if (!_solution.TryGetSolution(ent.Owner, ent.Comp.SolutionId, out var soln, out var solution))
            return;

        if (!_container.TryGetContainer(ent, ent.Comp.ContainerId, out var container))
            return;

        var newData = new CP14FoodData
        {
            Visuals = new List<PrototypeLayerData>(recipe.FoodData.Visuals),
            Trash = new List<EntProtoId>(recipe.FoodData.Trash),
            Flavors = new HashSet<LocId>(recipe.FoodData.Flavors),
            Name = recipe.FoodData.Name,
            Desc = recipe.FoodData.Desc,
            CurrentRecipe = recipe
        };

        newData.Name = recipe.FoodData.Name;
        newData.Desc = recipe.FoodData.Desc;

        //Process entities
        foreach (var contained in container.ContainedEntities)
        {
            if (TryComp<FoodComponent>(contained, out var food))
            {
                //Merge trash
                newData.Trash.AddRange(food.Trash);

                //Merge solutions
                if (_solution.TryGetSolution(contained, food.Solution, out _, out var foodSolution))
                {
                    _solution.TryMixAndOverflow(soln.Value, foodSolution, solution.MaxVolume, out var overflowed);
                    if (overflowed is not null)
                    {
                        _puddle.TrySplashSpillAt(ent, Transform(ent).Coordinates, overflowed, out _);
                    }
                }
            }

            if (TryComp<FlavorProfileComponent>(contained, out var flavorComp))
            {
                //Merge flavors
                foreach (var flavor in flavorComp.Flavors)
                {
                    newData.Flavors.Add(flavor);
                }
            }

            QueueDel(contained);
        }

        if (solution.Volume <= 0)
            return;

        ent.Comp.FoodData = newData;
        Dirty(ent);
    }

    protected void BurntFood(Entity<CP14FoodCookerComponent> ent)
    {
        if (ent.Comp.FoodData is null)
            return;

        if (!_solution.TryGetSolution(ent.Owner, ent.Comp.SolutionId, out var soln, out var solution))
            return;

        //Brown visual
        foreach (var visuals in ent.Comp.FoodData.Visuals)
        {
            visuals.Color = Color.FromHex("#212121");
        }

        ent.Comp.FoodData.Name = Loc.GetString("cp14-meal-recipe-burned-trash-name");
        ent.Comp.FoodData.Desc = Loc.GetString("cp14-meal-recipe-burned-trash-desc");

        var replacedVolume = solution.Volume / 2;
        solution.SplitSolution(replacedVolume);
        solution.AddReagent(_burntFoodReagent, replacedVolume);

        DirtyField(ent.Owner, ent.Comp, nameof(CP14FoodCookerComponent.FoodData));
    }
}
