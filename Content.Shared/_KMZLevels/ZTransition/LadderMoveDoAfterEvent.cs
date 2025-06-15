using Content.Shared.DoAfter;
using Robust.Shared.Serialization;

namespace Content.Shared._KMZLevels.ZTransition;

[Serializable, NetSerializable]
public sealed partial class LadderMoveDoAfterEvent : SimpleDoAfterEvent
{
}
