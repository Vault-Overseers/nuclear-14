/*
 * This file is sublicensed under MIT License
 * https://github.com/space-wizards/space-station-14/blob/master/LICENSE.TXT
 */

using Content.Shared._CP14.Cooking.Components;
using Content.Shared.Interaction;
using Robust.Shared.Containers;

namespace Content.Shared._CP14.Cooking;

public abstract partial class CP14SharedCookingSystem
{
    private void InitTransfer()
    {
        SubscribeLocalEvent<CP14FoodHolderComponent, AfterInteractEvent>(OnAfterInteract);
        SubscribeLocalEvent<CP14FoodCookerComponent, AfterInteractEvent>(OnInteractUsing);

        SubscribeLocalEvent<CP14FoodCookerComponent, ContainerIsInsertingAttemptEvent>(OnInsertAttempt);
    }

    private void OnAfterInteract(Entity<CP14FoodHolderComponent> ent, ref AfterInteractEvent args)
    {
        if (!TryComp<CP14FoodCookerComponent>(args.Target, out var cooker))
            return;

        if (cooker.FoodData is null)
            return;

        if (ent.Comp.Visuals is not null)
            return;

        MoveFoodToHolder(ent, (args.Target.Value, cooker));
    }

    private void OnInteractUsing(Entity<CP14FoodCookerComponent> ent, ref AfterInteractEvent args)
    {
        if (!TryComp<CP14FoodHolderComponent>(args.Target, out var holder))
            return;

        if (holder.Visuals is not null)
            return;

        if (ent.Comp.FoodData is null)
            return;

        MoveFoodToHolder((args.Target.Value, holder), ent);
    }

    private void OnInsertAttempt(Entity<CP14FoodCookerComponent> ent, ref ContainerIsInsertingAttemptEvent args)
    {
        if (args.Cancelled)
            return;

        if (ent.Comp.FoodData is not null)
        {
            _popup.PopupEntity(Loc.GetString("cp14-cooking-popup-not-empty", ("name", MetaData(ent).EntityName)), ent);
            args.Cancel();
        }
    }
}
