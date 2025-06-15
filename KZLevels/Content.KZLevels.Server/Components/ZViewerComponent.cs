using System.Collections.Generic;
using Robust.Shared.GameObjects;

namespace Content.KayMisaZlevels.Server.Components;

/// <summary>
/// This is used for tracking Z Level viewers, which handle ensuring a client can view everything above and below it.
/// </summary>
[RegisterComponent, UnsavedComponent]
public sealed partial class ZViewerComponent : Component
{
    public HashSet<EntityUid> Loaders = new();
}
