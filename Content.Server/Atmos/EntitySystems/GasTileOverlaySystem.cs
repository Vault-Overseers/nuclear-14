using System.Runtime.CompilerServices;
using Content.Server.Atmos.Components;
using Content.Shared.Atmos;
using Content.Shared.Atmos.EntitySystems;
using Content.Shared.CCVar;
using Content.Shared.Chunking;
using Content.Shared.GameTicking;
using Content.Shared.Rounding;
using JetBrains.Annotations;
using Microsoft.Extensions.ObjectPool;
using Robust.Server.Player;
using Robust.Shared.Configuration;
using Robust.Shared.Enums;
using Robust.Shared.Map;
using Robust.Shared.Player;
using Robust.Shared.Timing;
using Robust.Shared.Utility;
using DependencyAttribute = Robust.Shared.IoC.DependencyAttribute;

// ReSharper disable once RedundantUsingDirective

namespace Content.Server.Atmos.EntitySystems
{
    [UsedImplicitly]
    internal sealed class GasTileOverlaySystem : SharedGasTileOverlaySystem
    {
        [Dependency] private readonly IGameTiming _gameTiming = default!;
        [Dependency] private readonly IPlayerManager _playerManager = default!;
        [Dependency] private readonly IMapManager _mapManager = default!;
        [Dependency] private readonly IConfigurationManager _confMan = default!;
        [Dependency] private readonly AtmosphereSystem _atmosphereSystem = default!;
        [Dependency] private readonly ChunkingSystem _chunkingSys = default!;

        private readonly Dictionary<IPlayerSession, Dictionary<EntityUid, HashSet<Vector2i>>> _lastSentChunks = new();

        /// <summary>
        ///     The tiles that have had their atmos data updated since last tick
        /// </summary>
        private readonly Dictionary<EntityUid, HashSet<Vector2i>> _invalidTiles = new();

        /// <summary>
        ///     Gas data stored in chunks to make PVS / bubbling easier.
        /// </summary>
        private readonly Dictionary<EntityUid, Dictionary<Vector2i, GasOverlayChunk>> _overlay =
            new();

        // Oh look its more duplicated decal system code!
        private ObjectPool<HashSet<Vector2i>> _chunkIndexPool =
            new DefaultObjectPool<HashSet<Vector2i>>(
                new DefaultPooledObjectPolicy<HashSet<Vector2i>>(), 64);
        private ObjectPool<Dictionary<EntityUid, HashSet<Vector2i>>> _chunkViewerPool =
            new DefaultObjectPool<Dictionary<EntityUid, HashSet<Vector2i>>>(
                new DefaultPooledObjectPolicy<Dictionary<EntityUid, HashSet<Vector2i>>>(), 64);

        /// <summary>
        ///     Overlay update interval, in seconds.
        /// </summary>
        private float _updateInterval;

        private int _thresholds;

        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<GridRemovalEvent>(OnGridRemoved);

            _playerManager.PlayerStatusChanged += OnPlayerStatusChanged;
            _confMan.OnValueChanged(CCVars.NetGasOverlayTickRate, UpdateTickRate, true);
            _confMan.OnValueChanged(CCVars.GasOverlayThresholds, UpdateThresholds, true);
        }

        public override void Shutdown()
        {
            base.Shutdown();
            _playerManager.PlayerStatusChanged -= OnPlayerStatusChanged;
            _confMan.UnsubValueChanged(CCVars.NetGasOverlayTickRate, UpdateTickRate);
            _confMan.UnsubValueChanged(CCVars.GasOverlayThresholds, UpdateThresholds);
        }

        private void UpdateTickRate(float value) => _updateInterval = value > 0.0f ? 1 / value : float.MaxValue;
        private void UpdateThresholds(int value) => _thresholds = value;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Invalidate(EntityUid grid, Vector2i index)
        {
            _invalidTiles.GetOrNew(grid).Add(index);
        }

        private void OnGridRemoved(GridRemovalEvent ev)
        {
            _overlay.Remove(ev.EntityUid);
        }

        private void OnPlayerStatusChanged(object? sender, SessionStatusEventArgs e)
        {
            if (e.NewStatus != SessionStatus.InGame)
            {
                if (_lastSentChunks.Remove(e.Session, out var set))
                    ReturnToPool(set);
                return;
            }

            if (!_lastSentChunks.ContainsKey(e.Session))
            {
                _lastSentChunks[e.Session] = _chunkViewerPool.Get();
                DebugTools.Assert(_lastSentChunks[e.Session].Count == 0);
            }
        }

        private void ReturnToPool(Dictionary<EntityUid, HashSet<Vector2i>> chunks)
        {
            foreach (var (_, previous) in chunks)
            {
                previous.Clear();
                _chunkIndexPool.Return(previous);
            }

            chunks.Clear();
            _chunkViewerPool.Return(chunks);
        }

        /// <summary>
        ///     Updates the visuals for a tile on some grid chunk.
        /// </summary>
        private void UpdateChunkTile(GridAtmosphereComponent gridAtmosphere, GasOverlayChunk chunk, Vector2i index, GameTick curTick)
        {
            var oldData = chunk.GetData(index);
            if (!gridAtmosphere.Tiles.TryGetValue(index, out var tile))
            {
                if (oldData == null)
                    return;

                chunk.LastUpdate = curTick;
                chunk.SetData(index, null);
                return;
            }

            var opacity = new byte[VisibleGasId.Length];
            GasOverlayData newData = new(tile!.Hotspot.State, opacity);
            if (tile.Air != null)
            {
                var i = 0;
                foreach (var id in VisibleGasId)
                {
                    var gas = _atmosphereSystem.GetGas(id);
                    var moles = tile.Air.Moles[id];

                    if (moles >= gas.GasMolesVisible)
                    {
                        opacity[i] = (byte) (ContentHelpers.RoundToLevels(
                            MathHelper.Clamp01((moles - gas.GasMolesVisible) / (gas.GasMolesVisibleMax - gas.GasMolesVisible)) * 255, byte.MaxValue, _thresholds) * 255 / (_thresholds - 1));
                    }
                    i++;
                }
            }

            if (oldData != null && oldData.Value.Equals(newData))
                return;

            chunk.SetData(index, newData);
            chunk.LastUpdate = curTick;
        }

        private void UpdateOverlayData(GameTick curTick)
        {
            foreach (var (gridId, invalidIndices) in _invalidTiles)
            {
                if (!TryComp(gridId, out GridAtmosphereComponent? gam))
                {
                    _overlay.Remove(gridId);
                    continue;
                }

                var chunks = _overlay.GetOrNew(gridId);

                foreach (var index in invalidIndices)
                {
                    var chunkIndex = GetGasChunkIndices(index);

                    if (!chunks.TryGetValue(chunkIndex, out var chunk))
                        chunks[chunkIndex] = chunk = new GasOverlayChunk(chunkIndex);

                    UpdateChunkTile(gam, chunk, index, curTick);
                }
            }
            _invalidTiles.Clear();
        }

        public override void Update(float frameTime)
        {
            base.Update(frameTime);
            AccumulatedFrameTime += frameTime;

            if (AccumulatedFrameTime < _updateInterval) return;
            AccumulatedFrameTime -= _updateInterval;

            var curTick = _gameTiming.CurTick;

            // First, update per-chunk visual data for any invalidated tiles.
            UpdateOverlayData(curTick);

            // Now we'll go through each player, then through each chunk in range of that player checking if the player is still in range
            // If they are, check if they need the new data to send (i.e. if there's an overlay for the gas).
            // Afterwards we reset all the chunk data for the next time we tick.
            var xformQuery = GetEntityQuery<TransformComponent>();

            foreach (var session in Filter.GetAllPlayers(_playerManager))
            {
                if (session is IPlayerSession { Status: SessionStatus.InGame } playerSession)
                    UpdatePlayer(playerSession, xformQuery, curTick);
            }
        }

        private void UpdatePlayer(IPlayerSession playerSession, EntityQuery<TransformComponent> xformQuery, GameTick curTick)
        {
            var chunksInRange = _chunkingSys.GetChunksForSession(playerSession, ChunkSize, xformQuery, _chunkIndexPool, _chunkViewerPool);

            if (!_lastSentChunks.TryGetValue(playerSession, out var previouslySent))
            {
                _lastSentChunks[playerSession] = previouslySent = _chunkViewerPool.Get();
                DebugTools.Assert(previouslySent.Count == 0);
            }

            var ev = new GasOverlayUpdateEvent();

            foreach (var (grid, oldIndices) in previouslySent)
            {
                // Mark the whole grid as stale and flag for removal.
                if (!chunksInRange.TryGetValue(grid, out var chunks))
                {
                    previouslySent.Remove(grid);

                    // If grid was deleted then don't worry about sending it to the client.
                    if (_mapManager.IsGrid(grid))
                        ev.RemovedChunks[grid] = oldIndices;
                    else
                    {
                        oldIndices.Clear();
                        _chunkIndexPool.Return(oldIndices);
                    }

                    continue;
                }

                var old = _chunkIndexPool.Get();
                DebugTools.Assert(old.Count == 0);
                foreach (var chunk in oldIndices)
                {
                    if (!chunks.Contains(chunk))
                        old.Add(chunk);
                }

                if (old.Count == 0)
                    _chunkIndexPool.Return(old);
                else
                    ev.RemovedChunks.Add(grid, old);
            }

            foreach (var (grid, gridChunks) in chunksInRange)
            {
                // Not all grids have atmospheres.
                if (!_overlay.TryGetValue(grid, out var gridData))
                    continue;

                List<GasOverlayChunk> dataToSend = new();
                ev.UpdatedChunks[grid] = dataToSend;

                previouslySent.TryGetValue(grid, out var previousChunks);

                foreach (var index in gridChunks)
                {
                    if (!gridData.TryGetValue(index, out var value))
                        continue;

                    if (previousChunks != null &&
                        previousChunks.Contains(index) &&
                        value.LastUpdate != curTick)
                        continue;

                    dataToSend.Add(value);
                }

                previouslySent[grid] = gridChunks;
                if (previousChunks != null)
                {
                    previousChunks.Clear();
                    _chunkIndexPool.Return(previousChunks);
                }
            }

            RaiseNetworkEvent(ev, playerSession.ConnectedClient);
            ReturnToPool(ev.RemovedChunks);
        }

        public override void Reset(RoundRestartCleanupEvent ev)
        {
            _invalidTiles.Clear();
            _overlay.Clear();

            foreach (var data in _lastSentChunks.Values)
            {
                ReturnToPool(data);
            }

            _lastSentChunks.Clear();
        }
    }
}
