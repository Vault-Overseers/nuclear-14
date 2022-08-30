using Content.Shared.GameTicking;
using Robust.Server.Player;
using Robust.Shared.Network;
using Robust.Shared.Player;
using Robust.Shared.Players;

namespace Content.Server.GameTicking
{
    public sealed partial class GameTicker
    {
        [ViewVariables]
        private readonly Dictionary<IPlayerSession, LobbyPlayerStatus> _playersInLobby = new();

        [ViewVariables] private readonly HashSet<NetUserId> _playersInGame = new();

        [ViewVariables]
        private TimeSpan _roundStartTime;

        [ViewVariables]
        private TimeSpan _pauseTime;

        [ViewVariables]
        public new bool Paused { get; set; }

        [ViewVariables]
        private bool _roundStartCountdownHasNotStartedYetDueToNoPlayers;

        public IReadOnlyDictionary<IPlayerSession, LobbyPlayerStatus> PlayersInLobby => _playersInLobby;
        public IReadOnlySet<NetUserId> PlayersInGame => _playersInGame;

        public void UpdateInfoText()
        {
            RaiseNetworkEvent(GetInfoMsg(), Filter.Empty().AddPlayers(_playersInLobby.Keys));
        }

        private string GetInfoText()
        {
            if (Preset == null)
            {
                return string.Empty;
            }

            var playerCount = $"{_playerManager.PlayerCount}";
            var map = _gameMapManager.GetSelectedMap();
            var mapName = map?.MapName ?? Loc.GetString("game-ticker-no-map-selected");
            var gmTitle = Loc.GetString(Preset.ModeTitle);
            var desc = Loc.GetString(Preset.Description);
            return Loc.GetString("game-ticker-get-info-text",("roundId", RoundId), ("playerCount", playerCount),("mapName", mapName),("gmTitle", gmTitle),("desc", desc));
        }

        private TickerLobbyReadyEvent GetStatusSingle(ICommonSession player, LobbyPlayerStatus status)
        {
            return new (new Dictionary<NetUserId, LobbyPlayerStatus> { { player.UserId, status } });
        }

        private TickerLobbyReadyEvent GetPlayerStatus()
        {
            var players = new Dictionary<NetUserId, LobbyPlayerStatus>();
            foreach (var player in _playersInLobby.Keys)
            {
                _playersInLobby.TryGetValue(player, out var status);
                players.Add(player.UserId, status);
            }
            return new TickerLobbyReadyEvent(players);
        }

        private TickerLobbyStatusEvent GetStatusMsg(IPlayerSession session)
        {
            _playersInLobby.TryGetValue(session, out var status);
            return new TickerLobbyStatusEvent(RunLevel != GameRunLevel.PreRoundLobby, LobbySong, LobbyBackground,status == LobbyPlayerStatus.Ready, _roundStartTime, _roundStartTimeSpan, Paused);
        }

        private void SendStatusToAll()
        {
            foreach (var player in _playersInLobby.Keys)
            {
                RaiseNetworkEvent(GetStatusMsg(player), player.ConnectedClient);
            }
        }

        private TickerLobbyInfoEvent GetInfoMsg()
        {
            return new (GetInfoText());
        }

        private void UpdateLateJoinStatus()
        {
            RaiseNetworkEvent(new TickerLateJoinStatusEvent(DisallowLateJoin));
        }

        public bool PauseStart(bool pause = true)
        {
            if (Paused == pause)
            {
                return false;
            }

            Paused = pause;

            if (pause)
            {
                _pauseTime = _gameTiming.CurTime;
            }
            else if (_pauseTime != default)
            {
                _roundStartTime += _gameTiming.CurTime - _pauseTime;
            }

            RaiseNetworkEvent(new TickerLobbyCountdownEvent(_roundStartTime, Paused));

            _chatManager.DispatchServerAnnouncement(Loc.GetString(Paused
                ? "game-ticker-pause-start"
                : "game-ticker-pause-start-resumed"));

            return true;
        }

        public bool TogglePause()
        {
            PauseStart(!Paused);
            return Paused;
        }

        public void ToggleReady(IPlayerSession player, bool ready)
        {
            if (!_playersInLobby.ContainsKey(player)) return;

            if (!_prefsManager.HavePreferencesLoaded(player))
            {
                return;
            }

            var status = ready ? LobbyPlayerStatus.Ready : LobbyPlayerStatus.NotReady;
            _playersInLobby[player] = ready ? LobbyPlayerStatus.Ready : LobbyPlayerStatus.NotReady;
            RaiseNetworkEvent(GetStatusMsg(player), player.ConnectedClient);
            RaiseNetworkEvent(GetStatusSingle(player, status));
        }
    }
}
