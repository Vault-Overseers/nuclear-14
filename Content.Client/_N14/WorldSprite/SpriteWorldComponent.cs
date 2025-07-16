using Robust.Shared.GameObjects;

namespace Content.Client._N14.WorldSprite;

/// <summary>
/// Changes the entity's sprite state when placed in the game world.
/// </summary>
[RegisterComponent]
public sealed partial class SpriteWorldComponent : Component
{
    /// <summary>
    /// State to use when the entity is placed in the world.
    /// </summary>
    [DataField("worldState")]
    public string? WorldState;

    /// <summary>
    /// The sprite's original state captured on initialization.
    /// </summary>
    [ViewVariables]
    public string? DefaultState;
}
