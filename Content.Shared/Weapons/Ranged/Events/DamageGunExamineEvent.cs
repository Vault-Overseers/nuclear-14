using Robust.Shared.Utility;

namespace Content.Shared.Damage.Events;

[ByRefEvent]
public readonly record struct DamageGunExamineEvent(FormattedMessage Message, EntityUid User);
