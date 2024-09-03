using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading;
using System.Threading.Tasks;
using Content.Server._NC.CCCvars;
using Content.Shared._NC.DiscordAuth;
using Lidgren.Network;
using Robust.Server.Player;
using Robust.Shared.Configuration;
using Robust.Shared.Enums;
using Robust.Shared.Network;
using Robust.Shared.Player;
using Robust.Shared.Serialization;
using Timer = Robust.Shared.Timing.Timer;

namespace Content.Server._NC.Discord;

public sealed class DiscordAuthManager : IPostInjectInit
{
    [Dependency] private INetManager _net = default!;
    [Dependency] private readonly IPlayerManager _playerManager = default!;
    [Dependency] private readonly IConfigurationManager _configuration = default!;

    private ISawmill _sawmill = default!;

    private string _apiUrl = default!;
    private string _apiKey = default!;

    private readonly HttpClient _httpClient = new();
    private readonly Dictionary<NetUserId, DiscordUserData> _cachedDiscordUsers = new();
    public event EventHandler<ICommonSession>? PlayerVerified;

    public void PostInject()
    {
        IoCManager.InjectDependencies(this);
    }

    public void Initialize()
    {
        _configuration.OnValueChanged(CCCVars.DiscordApiUrl, (value) => _apiUrl = value, true);
        _configuration.OnValueChanged(CCCVars.ApiKey, (value) => _apiKey = value, true);
        _sawmill = Logger.GetSawmill("discord_auth");
        _net.RegisterNetMessage<MsgDiscordAuthRequired>();
        _net.RegisterNetMessage<MsgDiscordAuthCheck>(OnAuthCheck);
        _net.Disconnect += OnDisconnect;
        _playerManager.PlayerStatusChanged += OnPlayerStatusChanged;
        PlayerVerified += OnPlayerVerified;
    }

    private void OnPlayerVerified(object? sender, ICommonSession e)
    {
        Timer.Spawn(0, () => _playerManager.JoinGame(e));
    }

    private void OnDisconnect(object? sender, NetDisconnectedArgs e)
    {
        _cachedDiscordUsers.Remove(e.Channel.UserId);
    }

    private async void OnAuthCheck(MsgDiscordAuthCheck msg)
    {
        var data = await IsVerified(msg.MsgChannel.UserId);
        if (data is null)
            return;

        var session = _playerManager.GetSessionById(msg.MsgChannel.UserId);
        _cachedDiscordUsers.TryAdd(session.UserId, data);
        PlayerVerified?.Invoke(this, session);
    }

    private async void OnPlayerStatusChanged(object? sender, SessionStatusEventArgs args)
    {
        if (args.NewStatus != SessionStatus.Connected)
            return;

        var data = await IsVerified(args.Session.UserId);
        if (data is not null)
        {
            _cachedDiscordUsers.TryAdd(args.Session.UserId, data);
            PlayerVerified?.Invoke(this, args.Session);
            return;
        }

        var link = await GenerateLink(args.Session.UserId);
        var message = new MsgDiscordAuthRequired() {Link = link};
        args.Session.Channel.SendMessage(message);
    }

    public async Task<DiscordUserData?> IsVerified(NetUserId userId, CancellationToken cancel = default)
    {
        _sawmill.Debug($"Player {userId} check Discord verification");

        var requestUrl = $"{_apiUrl}/check?userid={userId}&api_token={_apiKey}";
        var response = await _httpClient.GetAsync(requestUrl, cancel);
        if (!response.IsSuccessStatusCode)
            return null;
        var discordData = await response.Content.ReadFromJsonAsync<DiscordUserData>(cancel);

        return discordData;
    }

    public async Task<string> GenerateLink(NetUserId userId, CancellationToken cancel = default)
    {
        _sawmill.Debug($"Generating link for {userId}");
        var requestUrl = $"{_apiUrl}/link?userid={userId}&api_token={_apiKey}";
        var response = await _httpClient.GetAsync(requestUrl, cancel);
        var link = await response.Content.ReadFromJsonAsync<DiscordLinkResponse>(cancel);
        return link!.Link;
    }
}

public sealed class DiscordUserData()
{
    public NetUserId UserId { get; set; }
    public string DiscordId { get; set; } = default!;
}

public sealed class DiscordLinkResponse()
{
    public string Link { get; set; } = default!;
}
