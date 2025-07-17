/*
 * This file is sublicensed under MIT License
 * https://github.com/space-wizards/space-station-14/blob/master/LICENSE.TXT
 */

using Content.Shared._CP14.Cooking.Components;
using Content.Shared.DoAfter;
using Content.Shared.Temperature;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Serialization;
using Robust.Shared.Timing;

namespace Content.Shared._CP14.Cooking;

public abstract partial class CP14SharedCookingSystem
{
    [Dependency] private readonly IGameTiming _timing = default!;
    private void InitDoAfter()
    {
        SubscribeLocalEvent<CP14FoodCookerComponent, OnTemperatureChangeEvent>(OnTemperatureChange);
        SubscribeLocalEvent<CP14FoodCookerComponent, EntParentChangedMessage>(OnParentChanged);

        SubscribeLocalEvent<CP14FoodCookerComponent, CP14CookingDoAfter>(OnCookFinished);
        SubscribeLocalEvent<CP14FoodCookerComponent, CP14BurningDoAfter>(OnCookBurned);
    }

    private void UpdateDoAfter(float frameTime)
    {
        var query = EntityQueryEnumerator<CP14FoodCookerComponent>();
        while(query.MoveNext(out var uid, out var cooker))
        {
            if (_timing.CurTime > cooker.LastHeatingTime + cooker.HeatingFrequencyRequired && _doAfter.IsRunning(cooker.DoAfterId))
                _doAfter.Cancel(cooker.DoAfterId);
        }
    }


    protected virtual void OnCookBurned(Entity<CP14FoodCookerComponent> ent, ref CP14BurningDoAfter args)
    {
        StopCooking(ent);

        if (args.Cancelled || args.Handled)
            return;

        BurntFood(ent);

        args.Handled = true;
    }

    protected virtual void OnCookFinished(Entity<CP14FoodCookerComponent> ent, ref CP14CookingDoAfter args)
    {
        StopCooking(ent);

        if (args.Cancelled || args.Handled)
            return;

        if (!_proto.TryIndex(args.Recipe, out var indexedRecipe))
            return;

        CookFood(ent, indexedRecipe);

        args.Handled = true;
    }

    private void StartCooking(Entity<CP14FoodCookerComponent> ent, CP14CookingRecipePrototype recipe)
    {
        if (_doAfter.IsRunning(ent.Comp.DoAfterId))
            return;

        _appearance.SetData(ent, CP14CookingVisuals.Cooking, true);

        var doAfterArgs = new DoAfterArgs(EntityManager, ent, recipe.CookingTime, new CP14CookingDoAfter(recipe.ID), ent)
        {
            NeedHand = false,
            BreakOnWeightlessMove = false,
            RequireCanInteract = false,
        };

        _doAfter.TryStartDoAfter(doAfterArgs, out var doAfterId);
        ent.Comp.DoAfterId = doAfterId;
        _ambientSound.SetAmbience(ent, true);
        _ambientSound.SetSound(ent, recipe.CookingAmbient);
    }

    private void StartBurning(Entity<CP14FoodCookerComponent> ent)
    {
        if (_doAfter.IsRunning(ent.Comp.DoAfterId))
            return;

        _appearance.SetData(ent, CP14CookingVisuals.Burning, true);

        var doAfterArgs = new DoAfterArgs(EntityManager, ent, 20, new CP14BurningDoAfter(), ent)
        {
            NeedHand = false,
            BreakOnWeightlessMove = false,
            RequireCanInteract = false,
        };

        _doAfter.TryStartDoAfter(doAfterArgs, out var doAfterId);
        ent.Comp.DoAfterId = doAfterId;
        _ambientSound.SetAmbience(ent, true);
    }

    protected void StopCooking(Entity<CP14FoodCookerComponent> ent)
    {
        if (_doAfter.IsRunning(ent.Comp.DoAfterId))
            _doAfter.Cancel(ent.Comp.DoAfterId);

        _appearance.SetData(ent, CP14CookingVisuals.Cooking, false);
        _appearance.SetData(ent, CP14CookingVisuals.Burning, false);

        _ambientSound.SetAmbience(ent, false);
    }

    private void OnTemperatureChange(Entity<CP14FoodCookerComponent> ent, ref OnTemperatureChangeEvent args)
    {
        if (!_container.TryGetContainer(ent, ent.Comp.ContainerId, out var container))
            return;

        if (container.ContainedEntities.Count <= 0 && ent.Comp.FoodData is null)
        {
            StopCooking(ent);
            return;
        }

        if (args.TemperatureDelta > 0)
        {
            ent.Comp.LastHeatingTime = _timing.CurTime;
            DirtyField(ent.Owner,ent.Comp, nameof(CP14FoodCookerComponent.LastHeatingTime));

            if (!_doAfter.IsRunning(ent.Comp.DoAfterId) && ent.Comp.FoodData is null)
            {
                var recipe = GetRecipe(ent);
                if (recipe is not null)
                    StartCooking(ent, recipe);
            }
            else
            {
                StartBurning(ent);
            }
        }
        else
        {
            StopCooking(ent);
        }
    }

    private void OnParentChanged(Entity<CP14FoodCookerComponent> ent, ref EntParentChangedMessage args)
    {
        StopCooking(ent);
    }
}

[Serializable, NetSerializable]
public sealed partial class CP14CookingDoAfter : DoAfterEvent
{
    [DataField]
    public ProtoId<CP14CookingRecipePrototype> Recipe;

    public CP14CookingDoAfter(ProtoId<CP14CookingRecipePrototype> recipe)
    {
        Recipe = recipe;
    }

    public override DoAfterEvent Clone() => this;
}

[Serializable, NetSerializable]
public sealed partial class CP14BurningDoAfter : SimpleDoAfterEvent;
