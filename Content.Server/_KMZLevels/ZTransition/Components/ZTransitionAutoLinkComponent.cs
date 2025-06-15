using Content.KayMisaZlevels.Shared.Miscellaneous;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;

namespace Content.Server._KMZLevels.ZTransition;

/// <summary>
/// Enables the automatic linking of entities by matching keys during searches.
/// </summary>
[RegisterComponent]
public sealed partial class ZTransitionAutoLinkComponent : Component
{
    /// <summary>
    /// A key used to locate another entity with a matching link in the world.
    /// </summary>
    [DataField]
    public string LinkKey { get; set; } = "IgnoreMe";

    /// <summary>
    /// prototype of the ladder being created on the other side
    /// </summary>
    [DataField(required: true)]
    public EntProtoId OtherSideProto = default!;

    [DataField("direction")]
    public ZDirection Direction = ZDirection.Down;
}
