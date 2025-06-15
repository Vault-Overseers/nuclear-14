using Content.KayMisaZlevels.Shared.Systems;
using Robust.Shared.GameObjects;

namespace Content.KayMisaZlevels.Shared;

/// <summary>
///     Indicated an entity ascended a Z level.
/// </summary>
/// <param name="Source">Level ascended from</param>
/// <param name="Destination">Level ascended to</param>
/// <remarks>
///     Contrary to <see cref="ZLevelDroppedEvent"/> this is voluntary without involving breaking legs
/// </remarks>
[ByRefEvent]
public record struct ZLevelAscendedEvent(EntityUid Target, EntityUid Source, EntityUid Destination, bool Handled);

/// <summary>
///     Indicated an entity descended a Z level.
/// </summary>
/// <param name="Source">Level ascended from</param>
/// <param name="Destination">Level ascended to</param>
/// <remarks>
///     Contrary to <see cref="ZLevelDroppedEvent"/> this is voluntary without involving breaking legs
/// </remarks>
[ByRefEvent]
public record struct ZLevelDescendedEvent(EntityUid Target, EntityUid Source, EntityUid Destination, bool Handled);
