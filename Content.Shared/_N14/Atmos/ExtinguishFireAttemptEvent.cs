namespace Content.Shared._N14.Atmos;

[ByRefEvent]
public record struct ExtinguishFireAttemptEvent(EntityUid Extinguisher, EntityUid Target, bool Cancelled = false);
