namespace Content.Server.Ghoul;

/// <summary>
/// Applied to ghouls. Excessive radiation causes them to go feral.
/// </summary>
[RegisterComponent]
public sealed partial class FeralGhoulifyComponent : Component
{
    [ViewVariables(VVAccess.ReadWrite)]
    public float AccumulatedRads;

    [DataField]
    public float Threshold = 100f;

    [DataField]
    public float ProbabilityPerRad = 0.02f;

    [DataField]
    public float NextNotify = 40f;

    [DataField]
    public float GlowingThreshold = 150f;

    [DataField]
    public float GlowProbabilityPerRad = 0.01f;

    [DataField]
    public float NextGlowNotify = 80f;
}
