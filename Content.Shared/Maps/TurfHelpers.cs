using System.Linq;
using System.Runtime.CompilerServices;
using System.Diagnostics.CodeAnalysis;
using Content.Shared.Physics;
using Robust.Shared.Map;
using Robust.Shared.Physics.Components;
using Robust.Shared.Random;

namespace Content.Shared.Maps
{
    public static class TurfHelpers
    {
        /// <summary>
        ///     Attempts to get the turf at map indices with grid id or null if no such turf is found.
        /// </summary>
        public static TileRef GetTileRef(this Vector2i vector2i, EntityUid gridId, IMapManager? mapManager = null)
        {
            mapManager ??= IoCManager.Resolve<IMapManager>();

            if (!mapManager.TryGetGrid(gridId, out var grid))
                return default;

            if (!grid.TryGetTileRef(vector2i, out var tile))
                return default;

            return tile;
        }

        /// <summary>
        ///     Attempts to get the turf at a certain coordinates or null if no such turf is found.
        /// </summary>
        public static TileRef? GetTileRef(this EntityCoordinates coordinates, IEntityManager? entityManager = null, IMapManager? mapManager = null)
        {
            entityManager ??= IoCManager.Resolve<IEntityManager>();

            if (!coordinates.IsValid(entityManager))
                return null;


            mapManager ??= IoCManager.Resolve<IMapManager>();

            if (!mapManager.TryGetGrid(coordinates.GetGridUid(entityManager), out var grid))
                return null;


            if (!grid.TryGetTileRef(coordinates, out var tile))
                return null;

            return tile;
        }

        public static bool TryGetTileRef(this EntityCoordinates coordinates, [NotNullWhen(true)] out TileRef? turf, IEntityManager? entityManager = null, IMapManager? mapManager = null)
        {
            return (turf = coordinates.GetTileRef(entityManager, mapManager)) != null;
        }

        /// <summary>
        ///     Returns the content tile definition for a tile.
        /// </summary>
        public static ContentTileDefinition GetContentTileDefinition(this Tile tile, ITileDefinitionManager? tileDefinitionManager = null)
        {
            tileDefinitionManager ??= IoCManager.Resolve<ITileDefinitionManager>();
            return (ContentTileDefinition)tileDefinitionManager[tile.TypeId];
        }

        /// <summary>
        ///     Returns whether a tile is considered space.
        /// </summary>
        public static bool IsSpace(this Tile tile, ITileDefinitionManager? tileDefinitionManager = null)
        {
            return tile.GetContentTileDefinition(tileDefinitionManager).IsSpace;
        }

        /// <summary>
        ///     Returns the content tile definition for a tile ref.
        /// </summary>
        public static ContentTileDefinition GetContentTileDefinition(this TileRef tile, ITileDefinitionManager? tileDefinitionManager = null)
        {
            return tile.Tile.GetContentTileDefinition(tileDefinitionManager);
        }

        /// <summary>
        ///     Returns whether a tile ref is considered space.
        /// </summary>
        public static bool IsSpace(this TileRef tile, ITileDefinitionManager? tileDefinitionManager = null)
        {
            return tile.Tile.IsSpace(tileDefinitionManager);
        }

        public static bool PryTile(this Vector2i indices, EntityUid gridId,
            IMapManager? mapManager = null, ITileDefinitionManager? tileDefinitionManager = null, IEntityManager? entityManager = null)
        {
            mapManager ??= IoCManager.Resolve<IMapManager>();
            var grid = mapManager.GetGrid(gridId);
            var tileRef = grid.GetTileRef(indices);
            return tileRef.PryTile(mapManager, tileDefinitionManager, entityManager);
        }

        public static bool PryTile(this TileRef tileRef,
            IMapManager? mapManager = null,
            ITileDefinitionManager? tileDefinitionManager = null,
            IEntityManager? entityManager = null,
            IRobustRandom? robustRandom = null)
        {
            var tile = tileRef.Tile;

            // If the arguments are null, resolve the needed dependencies.
            tileDefinitionManager ??= IoCManager.Resolve<ITileDefinitionManager>();

            if (tile.IsEmpty) return false;

            var tileDef = (ContentTileDefinition) tileDefinitionManager[tile.TypeId];

            if (!tileDef.CanCrowbar) return false;

            return DeconstructTile(tileRef, mapManager, tileDefinitionManager, entityManager, robustRandom);
        }

        public static bool CutTile(this TileRef tileRef,
            IMapManager? mapManager = null,
            ITileDefinitionManager? tileDefinitionManager = null,
            IEntityManager? entityManager = null,
            IRobustRandom? robustRandom = null)
        {
            var tile = tileRef.Tile;

            // If the arguments are null, resolve the needed dependencies.
            tileDefinitionManager ??= IoCManager.Resolve<ITileDefinitionManager>();

            if (tile.IsEmpty) return false;

            var tileDef = (ContentTileDefinition) tileDefinitionManager[tile.TypeId];

            if (!tileDef.CanWirecutter) return false;

            return DeconstructTile(tileRef, mapManager, tileDefinitionManager, entityManager, robustRandom);
        }

        private static bool DeconstructTile(this TileRef tileRef,
            IMapManager? mapManager = null,
            ITileDefinitionManager? tileDefinitionManager = null,
            IEntityManager? entityManager = null,
            IRobustRandom? robustRandom = null)
        {
            var indices = tileRef.GridIndices;

            mapManager ??= IoCManager.Resolve<IMapManager>();
            tileDefinitionManager ??= IoCManager.Resolve<ITileDefinitionManager>();
            entityManager ??= IoCManager.Resolve<IEntityManager>();
            robustRandom ??= IoCManager.Resolve<IRobustRandom>();

            var tileDef = (ContentTileDefinition) tileDefinitionManager[tileRef.Tile.TypeId];
            var mapGrid = mapManager.GetGrid(tileRef.GridUid);

            const float margin = 0.1f;
            var bounds = mapGrid.TileSize - margin * 2;
            var coordinates = mapGrid.GridTileToLocal(indices)
                .Offset(new Vector2(
                    (robustRandom.NextFloat() - 0.5f) * bounds,
                    (robustRandom.NextFloat() - 0.5f) * bounds));

            //Actually spawn the relevant tile item at the right position and give it some random offset.
            var tileItem = entityManager.SpawnEntity(tileDef.ItemDropPrototypeName, coordinates);
            entityManager.GetComponent<TransformComponent>(tileItem).LocalRotation
                = robustRandom.NextDouble() * Math.Tau;

            var plating = tileDefinitionManager[tileDef.BaseTurfs[^1]];

            mapGrid.SetTile(tileRef.GridIndices, new Tile(plating.TileId));

            return true;
        }

        /// <summary>
        ///     Helper that returns all entities in a turf.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static IEnumerable<EntityUid> GetEntitiesInTile(this TileRef turf, LookupFlags flags = LookupFlags.Anchored, EntityLookupSystem? lookupSystem = null)
        {
            lookupSystem ??= EntitySystem.Get<EntityLookupSystem>();

            if (!GetWorldTileBox(turf, out var worldBox))
                return Enumerable.Empty<EntityUid>();

            return lookupSystem.GetEntitiesIntersecting(turf.GridUid, worldBox, flags);
        }

        /// <summary>
        ///     Helper that returns all entities in a turf.
        /// </summary>
        public static IEnumerable<EntityUid> GetEntitiesInTile(this EntityCoordinates coordinates, LookupFlags flags = LookupFlags.Anchored, EntityLookupSystem? lookupSystem = null)
        {
            var turf = coordinates.GetTileRef();

            if (turf == null)
                return Enumerable.Empty<EntityUid>();

            return GetEntitiesInTile(turf.Value, flags, lookupSystem);
        }

        /// <summary>
        ///     Helper that returns all entities in a turf.
        /// </summary>
        public static IEnumerable<EntityUid> GetEntitiesInTile(this Vector2i indices, EntityUid gridId, LookupFlags flags = LookupFlags.Anchored, EntityLookupSystem? lookupSystem = null)
        {
            return GetEntitiesInTile(indices.GetTileRef(gridId), flags, lookupSystem);
        }

        /// <summary>
        /// Checks if a turf has something dense on it.
        /// </summary>
        public static bool IsBlockedTurf(this TileRef turf, bool filterMobs, EntityLookupSystem? physics = null, IEntitySystemManager? entSysMan = null)
        {
            // TODO: Deprecate this with entitylookup.
            if (physics == null)
            {
                IoCManager.Resolve(ref entSysMan);
                physics = entSysMan.GetEntitySystem<EntityLookupSystem>();
            }

            if (!GetWorldTileBox(turf, out var worldBox))
                return false;

            var entManager = IoCManager.Resolve<IEntityManager>();
            var query = physics.GetEntitiesIntersecting(turf.GridUid, worldBox);

            foreach (var ent in query)
            {
                // Yes, this can fail. Welp!
                if (!entManager.TryGetComponent(ent, out PhysicsComponent? body))
                    continue;

                if (body.CanCollide && body.Hard && (body.CollisionLayer & (int) CollisionGroup.Impassable) != 0)
                    return true;

                if (filterMobs && (body.CollisionLayer & (int) CollisionGroup.MobMask) != 0)
                    return true;
            }

            return false;
        }

        public static EntityCoordinates GridPosition(this TileRef turf, IMapManager? mapManager = null)
        {
            mapManager ??= IoCManager.Resolve<IMapManager>();

            return turf.GridIndices.ToEntityCoordinates(turf.GridUid, mapManager);
        }

        /// <summary>
        /// Creates a box the size of a tile, at the same position in the world as the tile.
        /// </summary>
        private static bool GetWorldTileBox(TileRef turf, out Box2Rotated res)
        {
            var map = IoCManager.Resolve<IMapManager>();

            if (map.TryGetGrid(turf.GridUid, out var tileGrid))
            {
                // This is scaled to 90 % so it doesn't encompass walls on other tiles.
                var tileBox = Box2.UnitCentered.Scale(0.9f);
                tileBox = tileBox.Scale(tileGrid.TileSize);
                var worldPos = tileGrid.GridTileToWorldPos(turf.GridIndices);
                tileBox = tileBox.Translated(worldPos);
                // Now tileBox needs to be rotated to match grid rotation
                res = new Box2Rotated(tileBox, tileGrid.WorldRotation, worldPos);
                return true;
            }

            // Have to "return something"
            res = Box2Rotated.UnitCentered;
            return false;
        }
    }
}
