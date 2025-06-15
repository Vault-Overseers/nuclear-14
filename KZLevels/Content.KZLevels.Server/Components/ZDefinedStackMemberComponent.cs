using System.Collections.Generic;
using Robust.Shared.GameObjects;
using Robust.Shared.Player;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.Utility;

namespace Content.KayMisaZlevels.Server.Components;

/// <summary>
/// Just marks children levels of ZDefinedStackComponent for save mapping commands.
/// </summary>
[RegisterComponent, UnsavedComponent]
public sealed partial class ZDefinedStackMemberComponent : Component
{
    /// <summary>
    /// A map paths to load on a new map lower.
    /// </summary>
    [DataField("savePath")]
    public ResPath? SavePath;
}
