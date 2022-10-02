﻿using System.Linq;
using Content.Server.Administration;
using Content.Server.Body.Components;
using Content.Server.Cargo.Components;
using Content.Server.Materials;
using Content.Server.Stack;
using Content.Shared.Administration;
using Content.Shared.MobState.Components;
using Robust.Shared.Console;
using Robust.Shared.Containers;
using Robust.Shared.Map;
using Robust.Shared.Utility;

namespace Content.Server.Cargo.Systems;

/// <summary>
/// This handles calculating the price of items, and implements two basic methods of pricing materials.
/// </summary>
public sealed class PricingSystem : EntitySystem
{
    [Dependency] private readonly IConsoleHost _consoleHost = default!;
    [Dependency] private readonly IMapManager _mapManager = default!;

    /// <inheritdoc/>
    public override void Initialize()
    {
        SubscribeLocalEvent<StaticPriceComponent, PriceCalculationEvent>(CalculateStaticPrice);
        SubscribeLocalEvent<StackPriceComponent, PriceCalculationEvent>(CalculateStackPrice);
        SubscribeLocalEvent<MobPriceComponent, PriceCalculationEvent>(CalculateMobPrice);

        _consoleHost.RegisterCommand("appraisegrid",
            "Calculates the total value of the given grids.",
            "appraisegrid <grid Ids>", AppraiseGridCommand);
    }

    [AdminCommand(AdminFlags.Debug)]
    private void AppraiseGridCommand(IConsoleShell shell, string argstr, string[] args)
    {
        if (args.Length == 0)
        {
            shell.WriteError("Not enough arguments.");
            return;
        }

        foreach (var gid in args)
        {
            if (!int.TryParse(gid, out var i) || i <= 0)
            {
                shell.WriteError($"Invalid grid ID \"{gid}\".");
                continue;
            }

            var gridId = new GridId(i);

            if (!_mapManager.TryGetGrid(gridId, out var mapGrid))
            {
                shell.WriteError($"Grid \"{i}\" doesn't exist.");
                continue;
            }

            List<(double, EntityUid)> mostValuable = new();

            var value = AppraiseGrid(mapGrid.GridEntityId, null, (uid, price) =>
            {
                mostValuable.Add((price, uid));
                mostValuable.Sort((i1, i2) => i2.Item1.CompareTo(i1.Item1));
                if (mostValuable.Count > 5)
                    mostValuable.Pop();
            });

            shell.WriteLine($"Grid {gid} appraised to {value} spacebucks.");
            shell.WriteLine($"The top most valuable items were:");
            foreach (var (price, ent) in mostValuable)
            {
                shell.WriteLine($"- {ToPrettyString(ent)} @ {price} spacebucks");
            }
        }
    }

    private void CalculateMobPrice(EntityUid uid, MobPriceComponent component, ref PriceCalculationEvent args)
    {
        if (!TryComp<BodyComponent>(uid, out var body) || !TryComp<MobStateComponent>(uid, out var state))
        {
            Logger.ErrorS("pricing", $"Tried to get the mob price of {ToPrettyString(uid)}, which has no {nameof(BodyComponent)} and no {nameof(MobStateComponent)}.");
            return;
        }

        var partList = body.Slots.ToList();
        var totalPartsPresent = partList.Sum(x => x.Part != null ? 1 : 0);
        var totalParts = partList.Count;

        var partRatio = totalPartsPresent / (double) totalParts;
        var partPenalty = component.Price * (1 - partRatio) * component.MissingBodyPartPenalty;

        args.Price += (component.Price - partPenalty) * (state.IsAlive() ? 1.0 : component.DeathPenalty);
    }

    private void CalculateStackPrice(EntityUid uid, StackPriceComponent component, ref PriceCalculationEvent args)
    {
        if (!TryComp<StackComponent>(uid, out var stack))
        {
            Logger.ErrorS("pricing", $"Tried to get the stack price of {ToPrettyString(uid)}, which has no {nameof(StackComponent)}.");
            return;
        }

        args.Price += stack.Count * component.Price;
    }

    private void CalculateStaticPrice(EntityUid uid, StaticPriceComponent component, ref PriceCalculationEvent args)
    {
        args.Price += component.Price;
    }

    /// <summary>
    /// Appraises an entity, returning it's price.
    /// </summary>
    /// <param name="uid">The entity to appraise.</param>
    /// <returns>The price of the entity.</returns>
    /// <remarks>
    /// This fires off an event to calculate the price.
    /// Calculating the price of an entity that somehow contains itself will likely hang.
    /// </remarks>
    public double GetPrice(EntityUid uid)
    {
        var ev = new PriceCalculationEvent();
        RaiseLocalEvent(uid, ref ev, true);

        //TODO: Add an OpaqueToAppraisal component or similar for blocking the recursive descent into containers, or preventing material pricing.

        if (TryComp<MaterialComponent>(uid, out var material) && !HasComp<StackPriceComponent>(uid))
        {
            if (TryComp<StackComponent>(uid, out var stack))
                ev.Price += stack.Count * material.Materials.Sum(x => x.Price * material._materials[x.ID]);
            else
                ev.Price += material.Materials.Sum(x => x.Price);
        }

        if (TryComp<ContainerManagerComponent>(uid, out var containers))
        {
            foreach (var container in containers.Containers)
            {
                foreach (var ent in container.Value.ContainedEntities)
                {
                    ev.Price += GetPrice(ent);
                }
            }
        }

        return ev.Price;
    }

    /// <summary>
    /// Appraises a grid, this is mainly meant to be used by yarrs.
    /// </summary>
    /// <param name="grid">The grid to appraise.</param>
    /// <param name="predicate">An optional predicate that controls whether or not the entity is counted toward the total.</param>
    /// <param name="afterPredicate">An optional predicate to run after the price has been calculated. Useful for high scores or similar.</param>
    /// <returns>The total value of the grid.</returns>
    public double AppraiseGrid(EntityUid grid, Func<EntityUid, bool>? predicate = null, Action<EntityUid, double>? afterPredicate = null)
    {
        var xform = Transform(grid);
        var price = 0.0;

        foreach (var child in xform.ChildEntities)
        {
            if (predicate is null || predicate(child))
            {
                var subPrice = GetPrice(child);
                price += subPrice;
                afterPredicate?.Invoke(child, subPrice);
            }
        }

        return price;
    }
}

/// <summary>
/// A directed by-ref event fired on an entity when something needs to know it's price. This value is not cached.
/// </summary>
[ByRefEvent]
public struct PriceCalculationEvent
{
    /// <summary>
    /// The total price of the entity.
    /// </summary>
    public double Price = 0;

    public PriceCalculationEvent() { }
}
