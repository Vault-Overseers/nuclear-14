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

    /// <summary>
    /// Once this amount of radiation is reached, others will see the mob
    /// twitching when examined.
    /// </summary>
    [DataField]
    public float ExamineThreshold = 40f;

    /// <summary>
    /// Amount of radiation lost per second when not irradiated.
    /// </summary>
    [DataField]
    public float DecayPerSecond = 1f;

}
