using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._N14.Cooking;

/// <summary>
/// Marks a donut batter entity that can be shaped and fried into a donut.
/// </summary>
[RegisterComponent]
public sealed partial class DonutBatterComponent : Component
{
    /// <summary>Prototype to swap to when cycling shapes.</summary>
    [DataField("nextShape")]
    public EntProtoId? NextShape;

    /// <summary>Prototype produced when fried.</summary>
    [DataField("cookedPrototype", required: true)]
    public EntProtoId CookedPrototype = default!;
}
