using Content.Shared.Radio;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._N14.Communications;

/// <summary>
///     Allows an entity to act as a radio tower for cross map communication.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(RadioTowerSystem))]
public sealed partial class RadioTowerComponent : Component
{
    [DataField, AutoNetworkedField]
    public RadioTowerStatus Status = RadioTowerStatus.Off;

    /// <summary>
    ///     List of radio channels this tower can relay. When an <see cref="EncryptionKeyHolderComponent"/>
    ///     is present the channels from the inserted keys will be merged into this list.
    /// </summary>
    [DataField, AutoNetworkedField]
    public HashSet<ProtoId<RadioChannelPrototype>> Channels = new();
}

/// <summary>
///     Current operational state of a radio tower.
/// </summary>
[Serializable, NetSerializable]
public enum RadioTowerStatus
{
    Broken,
    Off,
    On,
}

/// <summary>
///     Visual layers for radio towers.
/// </summary>
[Serializable, NetSerializable]
public enum RadioTowerLayers
{
    Layer,
}
