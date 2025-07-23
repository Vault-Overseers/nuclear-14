using Robust.Shared.GameStates;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;
using Robust.Shared.Map;
using Robust.Shared.Audio;
using System;
using Robust.Shared.Maths;

namespace Content.Shared._N14.Support
{

/// <summary>
/// Schedules a series of explosions representing vertibird fire support.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState,
    Access(typeof(SharedVertibirdSupportSystem))]
[AutoGenerateComponentPause]
public sealed partial class VertibirdSupportComponent : Component
{
    /// <summary>
    /// Target location for the support strike. Set when the flare lands.
    /// Not serialized to avoid client spawn errors.
    /// </summary>
    public MapCoordinates Target = MapCoordinates.Nullspace;

    /// <summary>
    /// Time from activation until the approach sound plays.
    /// </summary>
    [DataField, AutoNetworkedField]
    public TimeSpan ApproachDelay = TimeSpan.FromSeconds(10);

    /// <summary>
    /// Additional delay after the approach before the first shot.
    /// </summary>
    [DataField, AutoNetworkedField]
    public TimeSpan Delay = TimeSpan.FromSeconds(5);

    [DataField, AutoNetworkedField]
    public int Shots = 10;

    [DataField, AutoNetworkedField]
    public TimeSpan ShotInterval = TimeSpan.FromSeconds(0.1);

    [DataField, AutoNetworkedField]
    public float Spread = 4f;

    [DataField, AutoNetworkedField]
    public float LineLength = 10f;

    [DataField, AutoNetworkedField]
    public Angle LineAngle = Angle.Zero;

    [DataField, AutoNetworkedField]
    public string ExplosionType = "Default";

    [DataField, AutoNetworkedField]
    public float Intensity = 30f;

    [DataField, AutoNetworkedField]
    public float Slope = 2f;

    [DataField, AutoNetworkedField]
    public float MaxIntensity = 5f;

    [DataField]
    public SoundSpecifier? ApproachSound;

    [DataField]
    public SoundSpecifier? FireSound;

    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoNetworkedField, AutoPausedField]
    public TimeSpan StartTime;

    /// <summary>
    /// Whether the approach sound has already played.
    /// </summary>
    [DataField]
    public bool ApproachPlayed = false;

    [DataField, AutoNetworkedField]
    public int ShotsFired;
}
}
