namespace Content.Server.Fishing.Components;

[RegisterComponent]
public sealed partial class FishingRodComponent : Component
{
    [DataField("castTime")]
    public float CastTime = 3f;
}
