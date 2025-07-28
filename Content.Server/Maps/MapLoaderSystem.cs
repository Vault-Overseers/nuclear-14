using System.Numerics;
using Robust.Shared.Map;
using Robust.Shared.Utility;
using Robust.Shared.Serialization;

namespace Robust.Server.Maps
{
    /// <summary>
    /// Placeholder map loader system used when RobustToolbox is unavailable.
    /// This stub provides method signatures used throughout the code base but
    /// performs no actual loading logic.
    /// </summary>
    public sealed class MapLoaderSystem : EntitySystem
    {
        public bool TryLoadGrid(MapId mapId, ResPath path, out EntityUid? grid, DeserializationOptions? options = null, Vector2? offset = null, Angle? rotation = null)
        {
            grid = null;
            return false;
        }

        public bool TryLoadMap(string path, out EntityUid? map, out List<EntityUid> grids, DeserializationOptions? options = null, Vector2? offset = null, Angle? rotation = null)
        {
            map = null;
            grids = new List<EntityUid>();
            return false;
        }

        public bool TryLoadMapWithId(MapId mapId, string path, out EntityUid? map, out List<EntityUid> grids, DeserializationOptions? options = null, Vector2? offset = null, Angle? rotation = null)
        {
            map = null;
            grids = new List<EntityUid>();
            return false;
        }

        public bool TryMergeMap(MapId targetMap, string path, out List<EntityUid> grids, DeserializationOptions? options = null, Vector2? offset = null, Angle? rotation = null)
        {
            grids = new List<EntityUid>();
            return false;
        }
    }
}
