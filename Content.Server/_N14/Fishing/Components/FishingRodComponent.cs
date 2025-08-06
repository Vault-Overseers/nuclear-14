namespace Content.Server._N14.Fishing.Components;

[RegisterComponent]
public sealed partial class FishingRodComponent : Component
{
    [DataField("castTime")]
    public float CastTime = 3f;
}
