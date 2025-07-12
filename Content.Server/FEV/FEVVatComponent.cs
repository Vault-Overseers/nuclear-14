using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Strap.Components;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Server.FEV;

[RegisterComponent]
public sealed partial class FEVVatComponent : Component
{
    [DataField("solution", required: true)]
    public string Solution = "vat";

    [DataField("transferRate", customTypeSerializer: typeof(FixedPoint2Serializer))]
    public FixedPoint2 TransferRate = FixedPoint2.New(10);
}
