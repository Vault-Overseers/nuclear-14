namespace Content.Server.Warps;

[RegisterComponent]
public sealed partial class WarperComponent : Component
{
    /// Warp destination unique identifier.
    [ViewVariables(VVAccess.ReadWrite)] [DataField("id")] public string? ID { get; set; }
}
