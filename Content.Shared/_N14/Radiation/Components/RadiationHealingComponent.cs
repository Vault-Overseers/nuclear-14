using Robust.Shared.GameStates;

namespace Content.Shared._N14.Radiation.Components;

/// <summary>
/// Allows entities to heal when exposed to radiation and slows them based on exposure.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class RadiationHealingComponent : Component
{
    /// <summary>
    /// Amount of healing per rad per second.
    /// </summary>
    [DataField("healFactor")] public float HealFactor = 1f;

    /// <summary>
    /// Movement speed reduction per rad of exposure.
    /// </summary>
    [DataField("slowFactor")] public float SlowFactor = 0.5f;

    /// <summary>
    /// Rate that radiation exposure decays each second when not irradiated.
    /// </summary>
    [DataField("decayRate")] public float DecayRate = 1f;

    /// <summary>
    /// Current accumulated radiation intensity in rads per second.
    /// </summary>
    [AutoNetworkedField] public float CurrentExposure;
}
