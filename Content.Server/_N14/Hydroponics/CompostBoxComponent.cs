using Robust.Shared.GameStates;

namespace Content.Server._N14.Hydroponics;

/// <summary>
/// Added to compost boxes so they automatically start mixing when dung is inserted.
/// </summary>
[RegisterComponent, Access(typeof(CompostBoxSystem))]
public sealed partial class CompostBoxComponent : Component
{
    /// <summary>
    /// Container ID that holds the dung item.
    /// </summary>
    [DataField]
    public string MixerSlotId = "mixer";
}
