using Content.Shared.DoAfter;
using Robust.Shared.Serialization;

namespace Content.Shared.Gatherable;

[Serializable, NetSerializable]
public sealed partial class GatherableDoAfterEvent : SimpleDoAfterEvent
{
}
