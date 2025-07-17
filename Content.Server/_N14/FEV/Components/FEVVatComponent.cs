using Content.Shared.FixedPoint;
using Robust.Shared.GameStates;

namespace Content.Server._N14.FEV.Components;

/// <summary>
/// Marks an entity as an FEV vat that injects FEV when someone is buckled into it.
/// </summary>
[RegisterComponent]
[Access(typeof(Systems.FEVVatSystem))]
public sealed partial class FEVVatComponent : Component
{
    /// <summary>Amount of FEV to inject when buckled.</summary>
    [DataField("transferAmount")]
    public FixedPoint2 TransferAmount = FixedPoint2.New(20);
}
