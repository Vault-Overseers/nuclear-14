using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared._N14.Casino;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class RouletteTableComponent : Component
{
    /// <summary>
    ///     True while the roulette wheel is spinning.
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool Active;

    /// <summary>
    ///     How long the wheel spins for when activated.
    /// </summary>
    [DataField, AutoNetworkedField]
    public float SpinTime = 10f;

    /// <summary>
    ///     Sprite state when spinning.
    /// </summary>
    [DataField]
    public string OnState = "roulette_act";

    /// <summary>
    ///     Sprite state when idle.
    /// </summary>
    [DataField]
    public string OffState = "roulette";
}

[Serializable, NetSerializable]
public enum RouletteTableVisuals : byte
{
    State,
}

[Serializable, NetSerializable]
public enum RouletteTableState : byte
{
    Off,
    On,
}
