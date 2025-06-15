using System;
using Content.KayMisaZlevels.Shared.Miscellaneous;
using Robust.Shared.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Maths;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.ViewVariables;

namespace Content.KayMisaZlevels.Shared.Components;

/// <summary>
///     Marker that transitions between Z-Levels
/// </summary>
[RegisterComponent]
public sealed partial class ZTransitionMarkerComponent : Component
{
    /// <summary>
    /// Direction this marker transitions an entity (Up or Down)
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    [DataField("direction")]
    public ZDirection Dir;

    /// <summary>
    /// AHTUNG!: Used only for prototypes like ladders and stairs for define ZDirection Dir;
    /// </summary>
    [DataField("directionStr")]
    public string DirStr = "down";

    /// <summary>
    /// Map position of this marker
    /// </summary>
    [ViewVariables(VVAccess.ReadOnly)]
    [NonSerialized]
    public MapCoordinates Position;
}
