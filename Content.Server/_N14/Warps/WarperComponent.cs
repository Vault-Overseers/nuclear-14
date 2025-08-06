namespace Content.Server._N14.Warps;

/// <summary>
///     Allows an entity to warp users to a predefined warp point when interacted with.
/// </summary>
[RegisterComponent]
public sealed partial class WarperComponent : Component
{
    /// <summary>
    ///     The warp point identifier this warper sends players to.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    [DataField("id")] public string? ID { get; set; }
}
