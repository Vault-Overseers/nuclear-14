using Content.Shared.Radio;
using Robust.Shared.Prototypes;

namespace Content.Shared._N14.Communications;

/// <summary>
///     Allows a backpack to act as a portable relay for cross map radio.
///     Channels are determined from inserted encryption keys.
/// </summary>
[RegisterComponent]
public sealed partial class RadioBackpackComponent : Component
{
    [DataField]
    public HashSet<ProtoId<RadioChannelPrototype>> Channels = new();
}
