using Robust.Shared.GameStates;

namespace Content.Shared._RMC.Atmos;

[RegisterComponent, NetworkedComponent]
[Access(typeof(SharedRMCFlammableSystem))]
public sealed partial class BlockTileFireComponent : Component;
