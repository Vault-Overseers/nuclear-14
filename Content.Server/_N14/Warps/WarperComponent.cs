namespace Content.Server._N14.Warps;

[RegisterComponent]
public sealed partial class WarperComponent : Component
{
    /// Warp destination unique identifier.
    [ViewVariables(VVAccess.ReadWrite)] [DataField("id")] public string? ID { get; set; }
}
