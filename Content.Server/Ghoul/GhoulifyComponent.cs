using Robust.Shared.GameStates;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Server.Ghoul;

/// <summary>
/// Tracks radiation exposure for humanoids and gradually transforms them into ghouls.
/// </summary>
[RegisterComponent]
public sealed partial class GhoulifyComponent : Component
{
    /// <summary>
    /// Accumulated radiation dose in rads.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    public float AccumulatedRads;

    /// <summary>
    /// Total rads required before ghoulification can start.
    /// </summary>
    [DataField]
    public float Threshold = 50f;

    /// <summary>
    /// Probability per rad that exposure will trigger ghoulification once the threshold has been reached.
    /// </summary>
    [DataField]
    public float ProbabilityPerRad = 0.015f;

    /// <summary>
    /// When to show the next warning popup.
    /// </summary>
    [DataField]
    public float NextNotify = 25f;

    /// <summary>
    /// Rads required before a human can become a glowing ghoul instead of a regular one.
    /// </summary>
    [DataField]
    public float GlowingThreshold = 150f;

    /// <summary>
    /// Chance per rad to become a glowing ghoul once the glowing threshold is reached.
    /// </summary>
    [DataField]
    public float GlowProbabilityPerRad = 0.01f;

    /// <summary>
    /// When to warn the user about potential glowing transformation.
    /// </summary>
    [DataField]
    public float NextGlowNotify = 80f;

    /// <summary>
    /// Amount of radiation lost per second when not being irradiated.
    /// </summary>
    [DataField]
    public float DecayPerSecond = 1f;
}
