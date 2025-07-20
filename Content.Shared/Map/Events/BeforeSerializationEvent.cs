using Robust.Shared.GameStates;
using Robust.Shared.Map;

namespace Robust.Shared.Map.Events;

/// <summary>
/// Raised before a map is serialized to disk.
/// Contains the set of map IDs and entities that will be saved.
/// This is a minimal stub for compilation in testing environments.
/// </summary>
public sealed class BeforeSerializationEvent : EntityEventArgs
{
    public HashSet<MapId> MapIds { get; }
    public HashSet<EntityUid> Entities { get; }

    public BeforeSerializationEvent(HashSet<MapId> mapIds, HashSet<EntityUid> entities)
    {
        MapIds = mapIds;
        Entities = entities;
    }
}
