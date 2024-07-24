<<<<<<<< HEAD:Content.Server/Traits/Assorted/LightweightDrunkComponent.cs
namespace Content.Shared.Traits.Assorted.Components;

/// <summary>
/// Used for the lightweight trait. LightweightDrunkSystem will multiply the effects of ethanol being metabolized
/// </summary>
[RegisterComponent]
public sealed partial class LightweightDrunkComponent : Component
{
    [DataField("boozeStrengthMultiplier"), ViewVariables(VVAccess.ReadWrite)]
    public float BoozeStrengthMultiplier = 4f;
}
========
>>>>>>>> fd2451f911 (Upstream merge July 2024 (#396)):Content.Shared/Traits/Assorted/Components/LightweightDrunkComponent.cs
