using Content.Shared.DoAfter;
using Robust.Shared.Serialization;

namespace Content.Shared.Fishing;

[Serializable, NetSerializable]
public sealed partial class FishingDoAfterEvent : SimpleDoAfterEvent
{
}
