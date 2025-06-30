using Robust.Shared.GameStates;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;
using Robust.Shared.Map;
using Robust.Shared.Audio;

namespace Content.Shared._N14.Support;

/// <summary>
/// Schedules a series of explosions representing vertibird fire support.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState,
    Access(typeof(SharedVertibirdSupportSystem))]
[AutoGenerateComponentPause]
public sealed partial class VertibirdSupportComponent : Component
{
    [DataField, AutoNetworkedField]
    public MapCoordinates Target;

    [DataField, AutoNetworkedField]
    public TimeSpan Delay = TimeSpan.FromSeconds(5);

    [DataField, AutoNetworkedField]
    public int Shots = 3;

    [DataField, AutoNetworkedField]
    public TimeSpan ShotInterval = TimeSpan.FromSeconds(1);

    [DataField, AutoNetworkedField]
    public float Spread = 2f;

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

    [DataField, AutoNetworkedField]
    public int ShotsFired;
}
