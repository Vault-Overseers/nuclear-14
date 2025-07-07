namespace Content.Shared._RMC.Atmos;

[ByRefEvent]
public record struct ExtinguishFireAttemptEvent(EntityUid Extinguisher, EntityUid Target, bool Cancelled = false);
