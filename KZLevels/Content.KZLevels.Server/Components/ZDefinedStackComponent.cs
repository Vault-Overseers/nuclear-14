using System.Collections.Generic;
using Robust.Shared.GameObjects;
using Robust.Shared.Player;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.Utility;

namespace Content.KayMisaZlevels.Server.Components;

/// <summary>
/// This is used for loading prebuilted maps
/// </summary>
[RegisterComponent]
public sealed partial class ZDefinedStackComponent : Component
{
    /// <summary>
    /// A map paths to load on a new map lower.
    /// </summary>
    [DataField("downLevels")]
    public List<ResPath> DownLevels = new();

    /// <summary>
    /// A map paths to load on a new map up.
    /// </summary>
    [DataField("upLevels")]
    public List<ResPath> UpLevels = new();
}
