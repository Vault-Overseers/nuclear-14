/*
 * This file is sublicensed under MIT License
 * https://github.com/space-wizards/space-station-14/blob/master/LICENSE.TXT
 */

using System.Numerics;
using Content.Server.DoAfter;
using Content.Server.Popups;
using Content.Shared._CP14.Skill;
using Content.Shared._CP14.Workbench;
using Content.Shared._CP14.Workbench.Prototypes;
using Content.Shared.DoAfter;
using Content.Shared.Placeable;
using Content.Shared.UserInterface;
using Robust.Server.Audio;
using Robust.Server.GameObjects;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Server._CP14.Workbench;

public sealed partial class CP14WorkbenchSystem : CP14SharedWorkbenchSystem
{
    [Dependency] private readonly AudioSystem _audio = default!;
    [Dependency] private readonly EntityLookupSystem _lookup = default!;
    [Dependency] private readonly DoAfterSystem _doAfter = default!;
    [Dependency] private readonly IPrototypeManager _proto = default!;
    [Dependency] private readonly PopupSystem _popup = default!;
    [Dependency] private readonly UserInterfaceSystem _userInterface = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly IRobustRandom _random = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<CP14WorkbenchComponent, MapInitEvent>(OnMapInit);

        SubscribeLocalEvent<CP14WorkbenchComponent, ItemPlacedEvent>(OnItemPlaced);
        SubscribeLocalEvent<CP14WorkbenchComponent, ItemRemovedEvent>(OnItemRemoved);

        SubscribeLocalEvent<CP14WorkbenchComponent, BeforeActivatableUIOpenEvent>(OnBeforeUIOpen);
        SubscribeLocalEvent<CP14WorkbenchComponent, CP14WorkbenchUiCraftMessage>(OnCraft);

        SubscribeLocalEvent<CP14WorkbenchComponent, CP14CraftDoAfterEvent>(OnCraftFinished);
    }

    private void OnMapInit(Entity<CP14WorkbenchComponent> ent, ref MapInitEvent args)
    {
        foreach (var recipe in _proto.EnumeratePrototypes<CP14WorkbenchRecipePrototype>())
        {
            if (ent.Comp.Recipes.Contains(recipe))
                continue;

            if (!ent.Comp.RecipeTags.Contains(recipe.Tag))
                continue;

            ent.Comp.Recipes.Add(recipe);
        }
    }

    private void OnItemRemoved(Entity<CP14WorkbenchComponent> ent, ref ItemRemovedEvent args)
    {
        UpdateUIRecipes(ent);
    }

    private void OnItemPlaced(Entity<CP14WorkbenchComponent> ent, ref ItemPlacedEvent args)
    {
        UpdateUIRecipes(ent);
    }

    private void OnBeforeUIOpen(Entity<CP14WorkbenchComponent> ent, ref BeforeActivatableUIOpenEvent args)
    {
        UpdateUIRecipes(ent);
    }

    private void OnCraftFinished(Entity<CP14WorkbenchComponent> ent, ref CP14CraftDoAfterEvent args)
    {
        if (args.Cancelled || args.Handled)
            return;

        if (!_proto.TryIndex(args.Recipe, out var recipe))
            return;

        var placedEntities = _lookup.GetEntitiesInRange(Transform(ent).Coordinates,
            ent.Comp.WorkbenchRadius,
            LookupFlags.Uncontained);

        if (!CanCraftRecipe(recipe, placedEntities, args.User))
        {
            _popup.PopupEntity(Loc.GetString("cp14-workbench-cant-craft"), ent, args.User);
            return;
        }

        var resultEntities = new HashSet<EntityUid>();
        for (int i = 0; i < recipe.ResultCount; i++)
        {
            var resultEntity = Spawn(recipe.Result);
            resultEntities.Add(resultEntity);
        }

        foreach (var req in recipe.Requirements)
        {
            req.PostCraft(EntityManager, _proto, placedEntities);
        }

        //We teleport result to workbench AFTER craft.
        foreach (var resultEntity in resultEntities)
        {
            _transform.SetCoordinates(resultEntity, Transform(ent).Coordinates.Offset(new Vector2(_random.NextFloat(-0.25f, 0.25f), _random.NextFloat(-0.25f, 0.25f))));
        }

        UpdateUIRecipes(ent);
        args.Handled = true;
    }

    private void StartCraft(Entity<CP14WorkbenchComponent> workbench,
        EntityUid user,
        CP14WorkbenchRecipePrototype recipe)
    {
        var craftDoAfter = new CP14CraftDoAfterEvent
        {
            Recipe = recipe.ID,
        };

        var doAfterArgs = new DoAfterArgs(EntityManager,
            user,
            recipe.CraftTime * workbench.Comp.CraftSpeed,
            craftDoAfter,
            workbench,
            workbench)
        {
            BreakOnMove = true,
            BreakOnDamage = true,
            NeedHand = true,
        };

        _doAfter.TryStartDoAfter(doAfterArgs);
        _audio.PlayPvs(recipe.OverrideCraftSound ?? workbench.Comp.CraftSound, workbench);
    }

    private bool CanCraftRecipe(CP14WorkbenchRecipePrototype recipe, HashSet<EntityUid> entities, EntityUid user)
    {
        foreach (var skill in recipe.RequiredSkills)
        {
            if (!_skill.HaveSkill(user, skill))
                return false;
        }
        foreach (var req in recipe.Requirements)
        {
            if (!req.CheckRequirement(EntityManager, _proto, entities))
                return false;
        }

        return true;
    }
}
