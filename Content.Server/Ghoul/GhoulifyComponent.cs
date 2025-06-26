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
    public float Threshold = 500f;

    /// <summary>
    /// Probability per rad that exposure will trigger ghoulification once the threshold has been reached.
    /// </summary>
    [DataField]
    public float ProbabilityPerRad = 0.01f;

    /// <summary>
    /// When to show the next warning popup.
    /// </summary>
    [DataField]
    public float NextNotify = 100f;
}
