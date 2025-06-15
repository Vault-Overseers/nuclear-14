using System.Numerics;

namespace Content.Shared._KMZLevels.ZTransition;

/// <summary>
/// Marks if entity is stairs, for moving between Z levels.
/// </summary>
[RegisterComponent]
public sealed partial class ZStairsComponent : Component
{
    [DataField]
    public float Adjust = 0.25f;
}
