using System.Linq;
using System.Threading.Tasks;
using Content.Server.Announcements;
using Content.Server.GameTicking.Events;
using Content.Server.Ghost;
using Content.Server.Maps;
using Content.Server.Mind;
using Content.Server.Players;
using Content.Shared.CCVar;
using Content.Shared.Coordinates;
using Content.Shared.GameTicking;
using Content.Shared.Preferences;
using Content.Shared.Sound;
using JetBrains.Annotations;
using Prometheus;
using Robust.Server.Maps;
using Robust.Server.Player;
using Robust.Shared.Audio;
using Robust.Shared.Map;
using Robust.Shared.Network;
using Robust.Shared.Player;
using Robust.Shared.Random;
using Robust.Shared.Utility;

namespace Content.Server.GameTicking
{
    public sealed partial class GameTicker
    {
        private static readonly Counter RoundNumberMetric = Metrics.CreateCounter(
            "ss14_round_number",
            "Round number.");

        private static readonly Gauge RoundLengthMetric = Metrics.CreateGauge(
            "ss14_round_length",
            "Round length in seconds.");

#if EXCEPTION_TOLERANCE
        [ViewVariables]
        private int _roundStartFailCount = 0;
#endif

        [ViewVariables]
        private TimeSpan _roundStartTimeSpan;

        [ViewVariables]
        private bool _startingRound;

        [ViewVariables]
        private GameRunLevel _runLevel;

        [ViewVariables]
        public GameRunLevel RunLevel
        {
            get => _runLevel;
            private set
            {
                // Game admins can run `restartroundnow` while still in-lobby, which'd break things with this check.
                // if (_runLevel == value) return;

                var old = _runLevel;
                _runLevel = value;

                RaiseLocalEvent(new GameRunLevelChangedEvent(old, value));
            }
        }

        [ViewVariables]
        public int RoundId { get; private set; }

        /// <summary>
        ///     Loads all the maps for the given round.
        /// </summary>
        /// <remarks>
        ///     Must be called before the runlevel is set to InRound.
        /// </remarks>
        private void LoadMaps()
        {
            AddGamePresetRules();

            DefaultMap = _mapManager.CreateMap();
            _mapManager.AddUninitializedMap(DefaultMap);
            var startTime = _gameTiming.RealTime;
            var maps = new List<GameMapPrototype>() { _gameMapManager.GetSelectedMapChecked(true, true) };

            // Let game rules dictate what maps we should load.
            RaiseLocalEvent(new LoadingMapsEvent(maps));

            foreach (var map in maps)
            {
                var toLoad = DefaultMap;
                if (maps[0] != map)
                {
                    // Create other maps for the others since we need to.
                    toLoad = _mapManager.CreateMap();
                    _mapManager.AddUninitializedMap(toLoad);
                }

                LoadGameMap(map, toLoad, null);
            }

            var timeSpan = _gameTiming.RealTime - startTime;
            _sawmill.Info($"Loaded maps in {timeSpan.TotalMilliseconds:N2}ms.");
        }


        /// <summary>
        ///     Loads a new map, allowing systems interested in it to handle loading events.
        ///     In the base game, this is required to be used if you want to load a station.
        /// </summary>
        /// <param name="map">Game map prototype to load in.</param>
        /// <param name="targetMapId">Map to load into.</param>
        /// <param name="loadOptions">Map loading options, includes offset.</param>
        /// <param name="stationName">Name to assign to the loaded station.</param>
        /// <returns>All loaded entities and grids.</returns>
        public (IReadOnlyList<EntityUid>, IReadOnlyList<EntityUid>) LoadGameMap(GameMapPrototype map, MapId targetMapId, MapLoadOptions? loadOptions, string? stationName = null)
        {
            var loadOpts = loadOptions ?? new MapLoadOptions();

            var ev = new PreGameMapLoad(targetMapId, map, loadOpts);
            RaiseLocalEvent(ev);

            var (entities, gridIds) = _mapLoader.LoadMap(targetMapId, ev.GameMap.MapPath.ToString(), ev.Options);

            var gridUids = gridIds.Select(g => (EntityUid)g).ToList();
            RaiseLocalEvent(new PostGameMapLoad(map, targetMapId, entities, gridUids, stationName));

            _spawnPoint = _mapManager.GetGrid(gridIds[0]).ToCoordinates();
            return (entities, gridUids);
        }

        public void StartRound(bool force = false)
        {
#if EXCEPTION_TOLERANCE
            try
            {
#endif
            // If this game ticker is a dummy or the round is already being started, do nothing!
            if (DummyTicker || _startingRound)
                return;

            _startingRound = true;

            DebugTools.Assert(RunLevel == GameRunLevel.PreRoundLobby);
            _sawmill.Info("Starting round!");

            SendServerMessage(Loc.GetString("game-ticker-start-round"));

            LoadMaps();

            StartGamePresetRules();

            RoundLengthMetric.Set(0);

            var playerIds = _playersInLobby.Keys.Select(player => player.UserId.UserId).ToArray();
            var serverName = _configurationManager.GetCVar(CCVars.AdminLogsServerName);
            // TODO FIXME AAAAAAAAAAAAAAAAAAAH THIS IS BROKEN
            // Task.Run as a terrible dirty workaround to avoid synchronization context deadlock from .Result here.
            // This whole setup logic should be made asynchronous so we can properly wait on the DB AAAAAAAAAAAAAH
            RoundId = Task.Run(async () =>
            {
                var server = await _db.AddOrGetServer(serverName);
                return await _db.AddNewRound(server, playerIds);
            }).Result;

            var startingEvent = new RoundStartingEvent(RoundId);
            RaiseLocalEvent(startingEvent);

            List<IPlayerSession> readyPlayers;
            if (LobbyEnabled)
            {
                readyPlayers = _playersInLobby.Where(p => p.Value == LobbyPlayerStatus.Ready).Select(p => p.Key)
                    .ToList();
            }
            else
            {
                readyPlayers = _playersInLobby.Keys.ToList();
            }

            readyPlayers.RemoveAll(p =>
            {
                if (_roleBanManager.GetRoleBans(p.UserId) != null)
                    return false;
                Logger.ErrorS("RoleBans", $"Role bans for player {p} {p.UserId} have not been loaded yet.");
                return true;
            });

            // Get the profiles for each player for easier lookup.
            var profiles = _prefsManager.GetSelectedProfilesForPlayers(
                    readyPlayers
                        .Select(p => p.UserId).ToList())
                .ToDictionary(p => p.Key, p => (HumanoidCharacterProfile) p.Value);

            foreach (var readyPlayer in readyPlayers)
            {
                if (!profiles.ContainsKey(readyPlayer.UserId))
                {
                    profiles.Add(readyPlayer.UserId, HumanoidCharacterProfile.Random());
                }
            }

            var origReadyPlayers = readyPlayers.ToArray();

            if (!StartPreset(origReadyPlayers, force))
                return;

            // MapInitialize *before* spawning players, our codebase is too shit to do it afterwards...
            _mapManager.DoMapInitialize(DefaultMap);

            SpawnPlayers(readyPlayers, profiles, force);

            _roundStartDateTime = DateTime.UtcNow;
            RunLevel = GameRunLevel.InRound;

            _roundStartTimeSpan = _gameTiming.RealTime;
            SendStatusToAll();
            ReqWindowAttentionAll();
            UpdateLateJoinStatus();
            AnnounceRound();

#if EXCEPTION_TOLERANCE
            }
            catch (Exception e)
            {
                _roundStartFailCount++;

                if (RoundStartFailShutdownCount > 0 && _roundStartFailCount >= RoundStartFailShutdownCount)
                {
                    _sawmill.Fatal($"Failed to start a round {_roundStartFailCount} time(s) in a row... Shutting down!");
                    _runtimeLog.LogException(e, nameof(GameTicker));
                    _baseServer.Shutdown("Restarting server");
                    return;
                }

                _sawmill.Warning($"Exception caught while trying to start the round! Restarting round...");
                _runtimeLog.LogException(e, nameof(GameTicker));
                _startingRound = false;
                RestartRound();
                return;
            }

            // Round started successfully! Reset counter...
            _roundStartFailCount = 0;
#endif
            _startingRound = false;
        }

        private void RefreshLateJoinAllowed()
        {
            var refresh = new RefreshLateJoinAllowedEvent();
            RaiseLocalEvent(refresh);
            DisallowLateJoin = refresh.DisallowLateJoin;
        }

        public void EndRound(string text = "")
        {
            // If this game ticker is a dummy, do nothing!
            if (DummyTicker)
                return;

            DebugTools.Assert(RunLevel == GameRunLevel.InRound);
            _sawmill.Info("Ending round!");

            RunLevel = GameRunLevel.PostRound;

            ShowRoundEndScoreboard(text);
        }

        public void ShowRoundEndScoreboard(string text = "")
        {
            //Tell every client the round has ended.
            var gamemodeTitle = Preset != null ? Loc.GetString(Preset.ModeTitle) : string.Empty;

            // Let things add text here.
            var textEv = new RoundEndTextAppendEvent();
            RaiseLocalEvent(textEv);

            var roundEndText = $"{text}\n{textEv.Text}";

            //Get the timespan of the round.
            var roundDuration = RoundDuration();

            //Generate a list of basic player info to display in the end round summary.
            var listOfPlayerInfo = new List<RoundEndMessageEvent.RoundEndPlayerInfo>();
            // Grab the great big book of all the Minds, we'll need them for this.
            var allMinds = Get<MindTrackerSystem>().AllMinds;
            foreach (var mind in allMinds)
            {
                if (mind != null)
                {
                    // Some basics assuming things fail
                    var userId = mind.OriginalOwnerUserId;
                    var playerOOCName = userId.ToString();
                    var connected = false;
                    var observer = mind.AllRoles.Any(role => role is ObserverRole);
                    // Continuing
                    if (_playerManager.TryGetSessionById(userId, out var ply))
                    {
                        connected = true;
                    }
                    PlayerData? contentPlayerData = null;
                    if (_playerManager.TryGetPlayerData(userId, out var playerData))
                    {
                        contentPlayerData = playerData.ContentData();
                    }
                    // Finish
                    var antag = mind.AllRoles.Any(role => role.Antagonist);

                    var playerIcName = "Unknown";

                    if (mind.CharacterName != null)
                        playerIcName = mind.CharacterName;
                    else if (mind.CurrentEntity != null && TryName(mind.CurrentEntity.Value, out var icName))
                        playerIcName = icName;

                    var playerEndRoundInfo = new RoundEndMessageEvent.RoundEndPlayerInfo()
                    {
                        // Note that contentPlayerData?.Name sticks around after the player is disconnected.
                        // This is as opposed to ply?.Name which doesn't.
                        PlayerOOCName = contentPlayerData?.Name ?? "(IMPOSSIBLE: REGISTERED MIND WITH NO OWNER)",
                        // Character name takes precedence over current entity name
                        PlayerICName = playerIcName,
                        PlayerEntityUid = mind.OwnedEntity,
                        Role = antag
                            ? mind.AllRoles.First(role => role.Antagonist).Name
                            : mind.AllRoles.FirstOrDefault()?.Name ?? Loc.GetString("game-ticker-unknown-role"),
                        Antag = antag,
                        Observer = observer,
                        Connected = connected
                    };
                    listOfPlayerInfo.Add(playerEndRoundInfo);
                }
            }
            // This ordering mechanism isn't great (no ordering of minds) but functions
            var listOfPlayerInfoFinal = listOfPlayerInfo.OrderBy(pi => pi.PlayerOOCName).ToArray();
            _playersInGame.Clear();

            RaiseNetworkEvent(new RoundEndMessageEvent(gamemodeTitle, roundEndText, roundDuration, RoundId,
                listOfPlayerInfoFinal.Length, listOfPlayerInfoFinal, LobbySong,
                new SoundCollectionSpecifier("RoundEnd").GetSound()));
        }

        public void RestartRound()
        {
            // If this game ticker is a dummy, do nothing!
            if (DummyTicker)
                return;

            // Handle restart for server update
            if (_serverUpdates.RoundEnded())
                return;

            _sawmill.Info("Restarting round!");

            SendServerMessage(Loc.GetString("game-ticker-restart-round"));

            RoundNumberMetric.Inc();

            RunLevel = GameRunLevel.PreRoundLobby;
            LobbySong = _robustRandom.Pick(_lobbyMusicCollection.PickFiles).ToString();
            RandomizeLobbyBackground();
            ResettingCleanup();

            if (!LobbyEnabled)
            {
                StartRound();
            }
            else
            {
                if (_playerManager.PlayerCount == 0)
                    _roundStartCountdownHasNotStartedYetDueToNoPlayers = true;
                else
                    _roundStartTime = _gameTiming.CurTime + LobbyDuration;

                SendStatusToAll();

                ReqWindowAttentionAll();
            }
        }

        /// <summary>
        ///     Cleanup that has to run to clear up anything from the previous round.
        ///     Stuff like wiping the previous map clean.
        /// </summary>
        private void ResettingCleanup()
        {
            // Move everybody currently in the server to lobby.
            foreach (var player in _playerManager.ServerSessions)
            {
                PlayerJoinLobby(player);
            }

            // Delete the minds of everybody.
            // TODO: Maybe move this into a separate manager?
            foreach (var unCastData in _playerManager.GetAllPlayerData())
            {
                unCastData.ContentData()?.WipeMind();
            }

            // Delete all entities.
            foreach (var entity in EntityManager.GetEntities().ToArray())
            {
#if EXCEPTION_TOLERANCE
                try
                {
#endif
                // TODO: Maybe something less naive here?
                // FIXME: Actually, definitely.
                EntityManager.DeleteEntity(entity);
#if EXCEPTION_TOLERANCE
                }
                catch (Exception e)
                {
                    _sawmill.Error($"Caught exception while trying to delete entity {ToPrettyString(entity)}, this might corrupt the game state...");
                    _runtimeLog.LogException(e, nameof(GameTicker));
                    continue;
                }
#endif
            }

            _mapManager.Restart();

            _roleBanManager.Restart();

            // Clear up any game rules.
            ClearGameRules();

            _addedGameRules.Clear();
            _allPreviousGameRules.Clear();

            // Round restart cleanup event, so entity systems can reset.
            var ev = new RoundRestartCleanupEvent();
            RaiseLocalEvent(ev);

            // So clients' entity systems can clean up too...
            RaiseNetworkEvent(ev, Filter.Broadcast());

            DisallowLateJoin = false;
        }

        public bool DelayStart(TimeSpan time)
        {
            if (_runLevel != GameRunLevel.PreRoundLobby)
            {
                return false;
            }

            _roundStartTime += time;

            RaiseNetworkEvent(new TickerLobbyCountdownEvent(_roundStartTime, Paused));

            _chatManager.DispatchServerAnnouncement(Loc.GetString("game-ticker-delay-start", ("seconds",time.TotalSeconds)));

            return true;
        }

        private void UpdateRoundFlow(float frameTime)
        {
            if (RunLevel == GameRunLevel.InRound)
            {
                RoundLengthMetric.Inc(frameTime);
            }

            if (RunLevel != GameRunLevel.PreRoundLobby || Paused ||
                _roundStartTime > _gameTiming.CurTime ||
                _roundStartCountdownHasNotStartedYetDueToNoPlayers)
            {
                return;
            }

            StartRound();
        }

        public TimeSpan RoundDuration()
        {
            return _gameTiming.RealTime.Subtract(_roundStartTimeSpan);
        }

        private void AnnounceRound()
        {
            if (Preset == null) return;

            foreach (var proto in _prototypeManager.EnumeratePrototypes<RoundAnnouncementPrototype>())
            {
                if (!proto.GamePresets.Contains(Preset.ID)) continue;

                if (proto.Message != null)
                    _chatSystem.DispatchGlobalAnnouncement(Loc.GetString(proto.Message), playSound: true);

                if (proto.Sound != null)
                    SoundSystem.Play(proto.Sound.GetSound(), Filter.Broadcast());

                // Only play one because A
                break;
            }
        }
    }

    public enum GameRunLevel
    {
        PreRoundLobby = 0,
        InRound = 1,
        PostRound = 2
    }

    public sealed class GameRunLevelChangedEvent
    {
        public GameRunLevel Old { get; }
        public GameRunLevel New { get; }

        public GameRunLevelChangedEvent(GameRunLevel old, GameRunLevel @new)
        {
            Old = old;
            New = @new;
        }
    }

    /// <summary>
    ///     Event raised before maps are loaded in pre-round setup.
    ///     Contains a list of game map prototypes to load; modify it if you want to load different maps,
    ///     for example as part of a game rule.
    /// </summary>
    [PublicAPI]
    public sealed class LoadingMapsEvent : EntityEventArgs
    {
        public List<GameMapPrototype> Maps;

        public LoadingMapsEvent(List<GameMapPrototype> maps)
        {
            Maps = maps;
        }
    }

    /// <summary>
    ///     Event raised before the game loads a given map.
    ///     This event is mutable, and load options should be tweaked if necessary.
    /// </summary>
    /// <remarks>
    ///     You likely want to subscribe to this after StationSystem.
    /// </remarks>
    [PublicAPI]
    public sealed class PreGameMapLoad : EntityEventArgs
    {
        public readonly MapId Map;
        public GameMapPrototype GameMap;
        public MapLoadOptions Options;

        public PreGameMapLoad(MapId map, GameMapPrototype gameMap, MapLoadOptions options)
        {
            Map = map;
            GameMap = gameMap;
            Options = options;
        }
    }


    /// <summary>
    ///     Event raised after the game loads a given map.
    /// </summary>
    /// <remarks>
    ///     You likely want to subscribe to this after StationSystem.
    /// </remarks>
    [PublicAPI]
    public sealed class PostGameMapLoad : EntityEventArgs
    {
        public readonly GameMapPrototype GameMap;
        public readonly MapId Map;
        public readonly IReadOnlyList<EntityUid> Entities;
        public readonly IReadOnlyList<EntityUid> Grids;
        public readonly string? StationName;

        public PostGameMapLoad(GameMapPrototype gameMap, MapId map, IReadOnlyList<EntityUid> entities, IReadOnlyList<EntityUid> grids, string? stationName)
        {
            GameMap = gameMap;
            Map = map;
            Entities = entities;
            Grids = grids;
            StationName = stationName;
        }
    }

    /// <summary>
    ///     Event raised to refresh the late join status.
    ///     If you want to disallow late joins, listen to this and call Disallow.
    /// </summary>
    public sealed class RefreshLateJoinAllowedEvent
    {
        public bool DisallowLateJoin { get; private set; } = false;

        public void Disallow()
        {
            DisallowLateJoin = true;
        }
    }

    /// <summary>
    ///     Attempt event raised on round start.
    ///     This can be listened to by GameRule systems to cancel round start if some condition is not met, like player count.
    /// </summary>
    public sealed class RoundStartAttemptEvent : CancellableEntityEventArgs
    {
        public IPlayerSession[] Players { get; }
        public bool Forced { get; }

        public RoundStartAttemptEvent(IPlayerSession[] players, bool forced)
        {
            Players = players;
            Forced = forced;
        }
    }

    /// <summary>
    ///     Event raised before readied up players are spawned and given jobs by the GameTicker.
    ///     You can use this to spawn people off-station, like in the case of nuke ops or wizard.
    ///     Remove the players you spawned from the PlayerPool and call <see cref="GameTicker.PlayerJoinGame"/> on them.
    /// </summary>
    public sealed class RulePlayerSpawningEvent
    {
        /// <summary>
        ///     Pool of players to be spawned.
        ///     If you want to handle a specific player being spawned, remove it from this list and do what you need.
        /// </summary>
        /// <remarks>If you spawn a player by yourself from this event, don't forget to call <see cref="GameTicker.PlayerJoinGame"/> on them.</remarks>
        public List<IPlayerSession> PlayerPool { get; }
        public IReadOnlyDictionary<NetUserId, HumanoidCharacterProfile> Profiles { get; }
        public bool Forced { get; }

        public RulePlayerSpawningEvent(List<IPlayerSession> playerPool, IReadOnlyDictionary<NetUserId, HumanoidCharacterProfile> profiles, bool forced)
        {
            PlayerPool = playerPool;
            Profiles = profiles;
            Forced = forced;
        }
    }

    /// <summary>
    ///     Event raised after players were assigned jobs by the GameTicker.
    ///     You can give on-station people special roles by listening to this event.
    /// </summary>
    public sealed class RulePlayerJobsAssignedEvent
    {
        public IPlayerSession[] Players { get; }
        public IReadOnlyDictionary<NetUserId, HumanoidCharacterProfile> Profiles { get; }
        public bool Forced { get; }

        public RulePlayerJobsAssignedEvent(IPlayerSession[] players, IReadOnlyDictionary<NetUserId, HumanoidCharacterProfile> profiles, bool forced)
        {
            Players = players;
            Profiles = profiles;
            Forced = forced;
        }
    }

    /// <summary>
    ///     Event raised to allow subscribers to add text to the round end summary screen.
    /// </summary>
    public sealed class RoundEndTextAppendEvent
    {
        private bool _doNewLine;

        /// <summary>
        ///     Text to display in the round end summary screen.
        /// </summary>
        public string Text { get; private set; } = string.Empty;

        /// <summary>
        ///     Invoke this method to add text to the round end summary screen.
        /// </summary>
        /// <param name="text"></param>
        public void AddLine(string text)
        {
            if (_doNewLine)
                Text += "\n";

            Text += text;
            _doNewLine = true;
        }
    }
}
