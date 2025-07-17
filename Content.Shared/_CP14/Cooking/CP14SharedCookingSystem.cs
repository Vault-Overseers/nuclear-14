/*
 * This file is sublicensed under MIT License
 * https://github.com/space-wizards/space-station-14/blob/master/LICENSE.TXT
 */

using System.Linq;
using System.Numerics;
using Content.Shared._CP14.Cooking.Components;
using Content.Shared.Audio;
using Content.Shared.Chemistry.Components;
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
    [Dependency] protected readonly SharedSolutionContainerSystem _solution = default!;
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
        SubscribeLocalEvent<CP14FoodVisualsComponent, ExaminedEvent>(OnExaminedEvent);
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

    private void OnExaminedEvent(Entity<CP14FoodVisualsComponent> ent, ref ExaminedEvent args)
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


    /// <summary>
    /// Transfer food data from cooker to holder
    /// </summary>
    private void MoveFoodToHolder(Entity<CP14FoodHolderComponent> holder, Entity<CP14FoodCookerComponent> cooker)
    {
        if (holder.Comp.HoldFood || !cooker.Comp.HoldFood)
            return;

        if (holder.Comp.FoodType != cooker.Comp.FoodType)
            return;

        if (!TryComp<FoodComponent>(holder, out var holderFoodComp))
            return;

        if (!TryComp<CP14FoodVisualsComponent>(cooker, out var cookerFoodVisuals) || cookerFoodVisuals.FoodData is null)
            return;

        if (!_solution.TryGetSolution(cooker.Owner, cooker.Comp.SolutionId, out var cookerSoln, out var cookerSolution))
            return;

        //Solutions
        if (_solution.TryGetSolution(holder.Owner, holderFoodComp.Solution, out var holderSoln, out var solution))
        {
            if (solution.Volume > 0)
            {
                _popup.PopupEntity(Loc.GetString("cp14-cooking-popup-not-empty", ("name", MetaData(holder).EntityName)),
                    holder);
                return;
            }

            _solution.TryTransferSolution(holderSoln.Value, cookerSolution, solution.MaxVolume);
        }

        //Trash
        //If we have a lot of trash, we put 1 random trash in each plate. If it's a last plate (out of solution in cooker), we put all the remaining trash in it.
        if (cookerFoodVisuals.FoodData.Trash.Count > 0)
        {
            if (cookerSolution.Volume <= 0)
            {
                holderFoodComp.Trash.AddRange(cookerFoodVisuals.FoodData.Trash);
            }
            else
            {
                if (_net.IsServer)
                {
                    var newTrash = _random.Pick(cookerFoodVisuals.FoodData.Trash);
                    cookerFoodVisuals.FoodData.Trash.Remove(newTrash);
                    holderFoodComp.Trash.Add(newTrash);
                }
            }
        }

        //Name and Description
        if (cookerFoodVisuals.FoodData.Name is not null)
            _metaData.SetEntityName(holder, Loc.GetString(cookerFoodVisuals.FoodData.Name));
        if (cookerFoodVisuals.FoodData.Desc is not null)
            _metaData.SetEntityDescription(holder, Loc.GetString(cookerFoodVisuals.FoodData.Desc));

        //Flavors
        EnsureComp<FlavorProfileComponent>(holder, out var flavorComp);
        foreach (var flavor in cookerFoodVisuals.FoodData.Flavors)
        {
            flavorComp.Flavors.Add(flavor);
        }

        //Visuals
        var holderFoodVisuals = EnsureComp<CP14FoodVisualsComponent>(holder);
        holderFoodVisuals.FoodData = new CP14FoodData(cookerFoodVisuals.FoodData);

        //Visual random
        foreach (var layer in holderFoodVisuals.FoodData.Visuals)
        {
            if (_random.Prob(0.5f))
                layer.Scale = new Vector2(-1, 1);
        }

        //Clear cooker data
        if (cookerSolution.Volume <= 0)
        {
            cookerFoodVisuals.FoodData = null;
            cooker.Comp.HoldFood = false;
        }

        holder.Comp.HoldFood = true;

        Dirty(holder, holderFoodVisuals);
        Dirty(cooker, cookerFoodVisuals);

        Dirty(holder);
        Dirty(cooker);

        _solution.UpdateChemicals(cookerSoln.Value);
    }

    public CP14CookingRecipePrototype? GetRecipe(Entity<CP14FoodCookerComponent> ent)
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

        return GetRecipe(ent.Comp.FoodType, solution, allTags);
    }

    public CP14CookingRecipePrototype? GetRecipe(CP14FoodType foodType, Solution? solution, List<ProtoId<TagPrototype>> allTags)
    {
        if (OrderedRecipes.Count == 0)
        {
            throw new InvalidOperationException(
                "No cooking recipes found. Please ensure that the CP14CookingRecipePrototype is defined and loaded.");
        }

        CP14CookingRecipePrototype? selectedRecipe = null;
        foreach (var recipe in OrderedRecipes)
        {
            if (recipe.FoodType != foodType)
                continue;

            var conditionsMet = true;
            foreach (var condition in recipe.Requirements)
            {
                if (!condition.CheckRequirement(EntityManager, _proto, allTags, solution))
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

        var foodVisuals = EnsureComp<CP14FoodVisualsComponent>(ent.Owner);
        foodVisuals.FoodData = newData;

        ent.Comp.HoldFood = true;

        Dirty(ent);
        Dirty(ent, foodVisuals);
    }

    protected void BurntFood(Entity<CP14FoodCookerComponent> ent)
    {
        if (!TryComp<CP14FoodVisualsComponent>(ent, out var foodVisuals) || foodVisuals.FoodData is null)
            return;

        if (!_solution.TryGetSolution(ent.Owner, ent.Comp.SolutionId, out var soln, out var solution))
            return;

        //Brown visual
        foreach (var visuals in foodVisuals.FoodData.Visuals)
        {
            visuals.Color = Color.FromHex("#212121");
        }

        foodVisuals.FoodData.Name = Loc.GetString("cp14-meal-recipe-burned-trash-name");
        foodVisuals.FoodData.Desc = Loc.GetString("cp14-meal-recipe-burned-trash-desc");

        var replacedVolume = solution.Volume / 2;
        solution.SplitSolution(replacedVolume);
        solution.AddReagent(_burntFoodReagent, replacedVolume / 2);

        DirtyField(ent.Owner, foodVisuals, nameof(CP14FoodVisualsComponent.FoodData));
    }
}
