using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared._RMC.Atmos;

[DataDefinition]
[Serializable, NetSerializable]
public sealed partial class RMCFire
{
    [DataField]
    public EntProtoId Type = "RMCTileFire";

    [DataField]
    public int Range;

    [DataField]
    public int? Intensity;

    [DataField]
    public int? Duration;

    [DataField]
    public int? Total;
}
