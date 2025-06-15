using Robust.Shared.GameObjects;
using Robust.Shared.Player;

namespace Content.KayMisaZlevels.Server.Components;

/// <summary>
/// This is used for loading the world for the given client.
/// </summary>
[RegisterComponent]
public sealed partial class ZLoaderComponent : Component
{
    public ICommonSession? Target = default!;
}
