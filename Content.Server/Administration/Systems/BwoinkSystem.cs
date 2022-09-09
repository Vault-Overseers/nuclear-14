﻿using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using Content.Server.Administration.Managers;
using Content.Server.GameTicking;
using Content.Server.GameTicking.Events;
using Content.Shared.Administration;
using Content.Shared.CCVar;
using JetBrains.Annotations;
using Robust.Server.Player;
using Robust.Shared;
using Robust.Shared.Configuration;
using Robust.Shared.Network;
using Robust.Shared.Utility;

namespace Content.Server.Administration.Systems
{
    [UsedImplicitly]
    public sealed class BwoinkSystem : SharedBwoinkSystem
    {
        [Dependency] private readonly IPlayerManager _playerManager = default!;
        [Dependency] private readonly IAdminManager _adminManager = default!;
        [Dependency] private readonly IConfigurationManager _config = default!;
        [Dependency] private readonly IPlayerLocator _playerLocator = default!;
        [Dependency] private readonly GameTicker _gameTicker = default!;

        private ISawmill _sawmill = default!;
        private readonly HttpClient _httpClient = new();
        private string _webhookUrl = string.Empty;
        private string _serverName = string.Empty;
        private readonly Dictionary<NetUserId, (string id, string username, string content)> _relayMessages = new();
        private readonly Dictionary<NetUserId, Queue<string>> _messageQueues = new();
        private readonly HashSet<NetUserId> _processingChannels = new();
        private const ushort MessageMax = 2000;
        private int _maxAdditionalChars;

        public override void Initialize()
        {
            base.Initialize();
            _config.OnValueChanged(CCVars.DiscordAHelpWebhook, OnWebhookChanged, true);
            _config.OnValueChanged(CVars.GameHostName, OnServerNameChanged, true);
            _sawmill = IoCManager.Resolve<ILogManager>().GetSawmill("AHELP");
            _maxAdditionalChars = GenerateAHelpMessage("", "", true, true).Length + Header("").Length;

            SubscribeLocalEvent<RoundStartingEvent>(RoundStarting);
        }

        private void RoundStarting(RoundStartingEvent ev)
        {
            _relayMessages.Clear();
        }

        private void OnServerNameChanged(string obj)
        {
            _serverName = obj;
        }

        public override void Shutdown()
        {
            base.Shutdown();
            _config.UnsubValueChanged(CCVars.DiscordAHelpWebhook, OnWebhookChanged);
            _config.UnsubValueChanged(CVars.GameHostName, OnServerNameChanged);
        }

        private void OnWebhookChanged(string obj)
        {
            _webhookUrl = obj;
        }

        private string Header(string serverName) => $"Server: {serverName}";

        private async void ProcessQueue(NetUserId channelId, Queue<string> messages)
        {
            if (!_relayMessages.TryGetValue(channelId, out var oldMessage) || messages.Sum(x => x.Length+2) + oldMessage.content.Length > MessageMax)
            {
                var lookup = await _playerLocator.LookupIdAsync(channelId);

                if (lookup == null)
                {
                    _sawmill.Log(LogLevel.Error, $"Unable to find player for netuserid {channelId} when sending discord webhook.");
                    _relayMessages.Remove(channelId);
                    return;
                }

                oldMessage = (string.Empty, lookup.Username, Header(_serverName));
            }

            while (messages.TryDequeue(out var message))
            {
                oldMessage.content += $"\n{message}";
            }

            var payload = new WebhookPayload()
            {
                Username = $"R:{_gameTicker.RoundId}|N:{oldMessage.username}",
                Content = oldMessage.content
            };

            if (oldMessage.id == string.Empty)
            {
                var request = await _httpClient.PostAsync($"{_webhookUrl}?wait=true",
                    new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json"));

                var content = await request.Content.ReadAsStringAsync();
                if (!request.IsSuccessStatusCode)
                {
                    _sawmill.Log(LogLevel.Error, $"Discord returned bad status code when posting message: {request.StatusCode}\nResponse: {content}");
                    _relayMessages.Remove(channelId);
                    return;
                }

                var id = JsonNode.Parse(content)?["id"];
                if (id == null)
                {
                    _sawmill.Log(LogLevel.Error, $"Could not find id in json-content returned from discord webhook: {content}");
                    _relayMessages.Remove(channelId);
                    return;
                }

                oldMessage.id = id.ToString();
            }
            else
            {
                var request = await _httpClient.PatchAsync($"{_webhookUrl}/messages/{oldMessage.id}",
                    new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json"));

                if (!request.IsSuccessStatusCode)
                {
                    var content = await request.Content.ReadAsStringAsync();
                    _sawmill.Log(LogLevel.Error, $"Discord returned bad status code when patching message: {request.StatusCode}\nResponse: {content}");
                    _relayMessages.Remove(channelId);
                    return;
                }
            }

            _relayMessages[channelId] = oldMessage;

            _processingChannels.Remove(channelId);
        }

        public override void Update(float frameTime)
        {
            base.Update(frameTime);

            foreach (var channelId in _messageQueues.Keys.ToArray())
            {
                if(_processingChannels.Contains(channelId)) continue;

                var queue = _messageQueues[channelId];
                _messageQueues.Remove(channelId);
                if (queue.Count == 0) continue;
                _processingChannels.Add(channelId);

                ProcessQueue(channelId, queue);
            }
        }

        protected override void OnBwoinkTextMessage(BwoinkTextMessage message, EntitySessionEventArgs eventArgs)
        {
            base.OnBwoinkTextMessage(message, eventArgs);
            var senderSession = (IPlayerSession) eventArgs.SenderSession;

            // TODO: Sanitize text?
            // Confirm that this person is actually allowed to send a message here.
            var personalChannel = senderSession.UserId == message.ChannelId;
            var senderAdmin = _adminManager.GetAdminData(senderSession);
            var senderAHelpAdmin = senderAdmin?.HasFlag(AdminFlags.Adminhelp) ?? false;
            var authorized = personalChannel || senderAHelpAdmin;
            if (!authorized)
            {
                // Unauthorized bwoink (log?)
                return;
            }

            var escapedText = FormattedMessage.EscapeText(message.Text);

            var bwoinkText = senderAdmin switch
            {
                var x when x is not null && x.Flags == AdminFlags.Adminhelp =>
                    $"[color=purple]{senderSession.Name}[/color]: {escapedText}",
                var x when x is not null && x.HasFlag(AdminFlags.Adminhelp) =>
                    $"[color=red]{senderSession.Name}[/color]: {escapedText}",
                _ => $"{senderSession.Name}: {escapedText}",
            };

            var msg = new BwoinkTextMessage(message.ChannelId, senderSession.UserId, bwoinkText);

            LogBwoink(msg);

            // Admins w/ AHelp access
            var targets = _adminManager.ActiveAdmins
                .Where(p => _adminManager.GetAdminData(p)?.HasFlag(AdminFlags.Adminhelp) ?? false)
                .Select(p => p.ConnectedClient).ToList();

            // And involved player
            if (_playerManager.TryGetSessionById(message.ChannelId, out var session))
                if (!targets.Contains(session.ConnectedClient))
                    targets.Add(session.ConnectedClient);

            foreach (var channel in targets)
                RaiseNetworkEvent(msg, channel);

            var noReceivers = targets.Count == 1;

            var sendsWebhook = _webhookUrl != string.Empty;
            if (sendsWebhook)
            {
                if (!_messageQueues.ContainsKey(msg.ChannelId))
                    _messageQueues[msg.ChannelId] = new Queue<string>();

                var str = message.Text;
                var unameLength = senderSession.Name.Length;

                if (unameLength+str.Length+_maxAdditionalChars > MessageMax)
                {
                    str = str[..(MessageMax - _maxAdditionalChars - unameLength)];
                }
                _messageQueues[msg.ChannelId].Enqueue(GenerateAHelpMessage(senderSession.Name, str, !personalChannel, noReceivers));
            }

            if (noReceivers)
            {
                var systemText = sendsWebhook ?
                    Loc.GetString("bwoink-system-starmute-message-no-other-users-webhook") :
                    Loc.GetString("bwoink-system-starmute-message-no-other-users");
                var starMuteMsg = new BwoinkTextMessage(message.ChannelId, SystemUserId, systemText);
                RaiseNetworkEvent(starMuteMsg, senderSession.ConnectedClient);
            }
        }

        private string GenerateAHelpMessage(string username, string message, bool admin, bool noReceiver)
        {
            var stringbuilder = new StringBuilder();
            if (noReceiver)
                stringbuilder.Append(":sos:");
            stringbuilder.Append(admin ? ":outbox_tray:" : ":inbox_tray:");
            stringbuilder.Append(' ');
            stringbuilder.Append(username);
            stringbuilder.Append(": ");
            stringbuilder.Append(message);
            return stringbuilder.ToString();
        }

        private struct WebhookPayload
        {
            [JsonPropertyName("username")]
            public string Username { get; set; } = "";

            [JsonPropertyName("content")]
            public string Content { get; set; } = "";

            [JsonPropertyName("allowed_mentions")]
            public Dictionary<string, string[]> AllowedMentions { get; set; } =
                new()
                {
                    { "parse", Array.Empty<string>() }
                };

            public WebhookPayload() {}
        }
    }
}

