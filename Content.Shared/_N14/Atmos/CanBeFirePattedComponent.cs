using Robust.Shared.GameStates;

namespace Content.Shared._N14.Atmos;

[RegisterComponent, NetworkedComponent]
[Access(typeof(SharedRMCFlammableSystem))]
public sealed partial class CanBeFirePattedComponent : Component;
