using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Server._N14.FEV.Components;

/// <summary>
/// Handles the staged transformation process once FEV infection reaches the threshold.
/// </summary>
[RegisterComponent]
public sealed partial class PendingFEVTransformComponent : Component
{
    [DataField("nextTime", customTypeSerializer: typeof(TimeOffsetSerializer))]
    public TimeSpan NextTime;

    [DataField("stage"), ViewVariables]
    public int Stage;

    [DataField("species"), ViewVariables]
    public string Species = string.Empty;
}
