using Robust.Shared.GameStates;
using Robust.Shared.Maths;
using Robust.Shared.Prototypes;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.FixedPoint;

namespace Content.Shared._N14.Cooking;

/// <summary>
/// Allows cooked donuts to be topped with various reagents, converting them into new donut variants.
/// </summary>
[RegisterComponent]
public sealed partial class DonutToppingComponent : Component
{
    /// <summary>
    /// Mapping of reagent prototype IDs to the donut prototype produced when applied.
    /// </summary>
    [DataField("toppings")]
    public Dictionary<ReagentId, EntProtoId> Toppings = new();

    /// <summary>
    /// Amount of reagent required to apply a topping.
    /// </summary>
    [DataField("amount")] public FixedPoint2 Amount = FixedPoint2.New(5);
}
