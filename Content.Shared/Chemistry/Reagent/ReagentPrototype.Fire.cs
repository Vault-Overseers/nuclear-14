using Content.Shared.FixedPoint;

namespace Content.Shared.Chemistry.Reagent;

public sealed partial class ReagentPrototype
{
    /// <summary>
    /// The intensity of fires spawned using this reagent.
    /// </summary>
    [DataField]
    public FixedPoint2 Intensity = FixedPoint2.Zero;

    /// <summary>
    /// Duration of fires spawned using this reagent.
    /// </summary>
    [DataField]
    public FixedPoint2 Duration = FixedPoint2.Zero;
}
