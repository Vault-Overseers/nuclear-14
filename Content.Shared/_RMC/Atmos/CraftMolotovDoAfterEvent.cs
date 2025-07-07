using Content.Shared.DoAfter;
using Robust.Shared.Serialization;

namespace Content.Shared._RMC.Atmos;

[Serializable, NetSerializable]
public sealed partial class CraftMolotovDoAfterEvent : SimpleDoAfterEvent;
