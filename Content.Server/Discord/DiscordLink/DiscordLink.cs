using NetCord;
using NetCord.Gateway;
using NetCord.Rest;
using Content.Shared.CCVar;
using Robust.Shared.Configuration;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace Content.Server.Discord.DiscordLink;

public sealed class DiscordLink : IPostInjectInit
{
    [Dependency] private readonly IConfigurationManager _cfg = default!;
    [Dependency] private readonly ILogManager _log = default!;

    private GatewayClient? _client;
    private ISawmill _sawmill = default!;
    private ISawmill _sawmillLog = default!;

    public event Action<Message>? OnMessageReceived;

    public string BotPrefix { get; private set; } = "!";

    public bool IsConnected => _client != null;

    void IPostInjectInit.PostInject()
    {
        _sawmill = _log.GetSawmill("discord.link");
        _sawmillLog = _log.GetSawmill("discord.link.log");

        try
        {
            _cfg.OnValueChanged(CCVars.DiscordPrefix, p => BotPrefix = p, true);
        }
        catch (KeyNotFoundException)
        {
            BotPrefix = "!";
        }
    }

    public void Initialize()
    {
        var token = _cfg.GetCVar(CCVars.DiscordToken);
        if (string.IsNullOrEmpty(token))
            return;

        _client = new GatewayClient(new BotToken(token), new GatewayClientConfiguration
        {
            Intents = GatewayIntents.Guilds | GatewayIntents.GuildUsers |
                      GatewayIntents.GuildMessages | GatewayIntents.MessageContent |
                      GatewayIntents.DirectMessages,
            Logger = new DiscordSawmillLogger(_sawmillLog),
        });

        _client.MessageCreate += OnMessageInternal;
        Task.Run(async () =>
        {
            try
            {
                await _client.StartAsync();
                _sawmill.Info("Connected to Discord.");
            }
            catch (Exception e)
            {
                _sawmill.Error($"Failed to connect to Discord: {e}");
            }
        });
    }

    public async Task Shutdown()
    {
        if (_client == null)
            return;

        _client.MessageCreate -= OnMessageInternal;
        await _client.CloseAsync();
        _client.Dispose();
        _client = null;
    }

    private ValueTask OnMessageInternal(Message message)
    {
        OnMessageReceived?.Invoke(message);
        return ValueTask.CompletedTask;
    }

    public async Task SendMessageAsync(ulong channelId, string message)
    {
        if (_client == null)
            return;

        var channel = await _client.Rest.GetChannelAsync(channelId) as TextChannel;
        if (channel == null)
        {
            _sawmill.Error($"Discord channel {channelId} not found.");
            return;
        }

        await channel.SendMessageAsync(new MessageProperties
        {
            AllowedMentions = AllowedMentionsProperties.None,
            Content = message,
        });
    }
}

