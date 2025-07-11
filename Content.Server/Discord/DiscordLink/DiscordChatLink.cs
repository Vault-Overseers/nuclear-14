using Content.Server.Chat.Managers;
using Content.Shared.CCVar;
using Content.Shared.Chat;
using NetCord.Gateway;
using System.Threading.Tasks;
using Robust.Shared.Asynchronous;
using Robust.Shared.Configuration;

namespace Content.Server.Discord.DiscordLink;

public sealed class DiscordChatLink
{
    [Dependency] private readonly DiscordLink _discord = default!;
    [Dependency] private readonly IConfigurationManager _cfg = default!;
    [Dependency] private readonly IChatManager _chatManager = default!;
    [Dependency] private readonly ITaskManager _task = default!;

    private ulong? _oocChannelId;
    private ulong? _adminChannelId;
    private ulong? _deadChannelId;

    public void Initialize()
    {
        _discord.OnMessageReceived += OnMessage;

        _cfg.OnValueChanged(CCVars.OocDiscordChannelId, v => _oocChannelId = string.IsNullOrEmpty(v) ? null : ulong.Parse(v), true);
        _cfg.OnValueChanged(CCVars.AdminChatDiscordChannelId, v => _adminChannelId = string.IsNullOrEmpty(v) ? null : ulong.Parse(v), true);
        _cfg.OnValueChanged(CCVars.DeadChatDiscordChannelId, v => _deadChannelId = string.IsNullOrEmpty(v) ? null : ulong.Parse(v), true);
    }

    public void Shutdown()
    {
        _discord.OnMessageReceived -= OnMessage;
        _cfg.UnsubValueChanged(CCVars.OocDiscordChannelId, v => _oocChannelId = string.IsNullOrEmpty(v) ? null : ulong.Parse(v));
        _cfg.UnsubValueChanged(CCVars.AdminChatDiscordChannelId, v => _adminChannelId = string.IsNullOrEmpty(v) ? null : ulong.Parse(v));
        _cfg.UnsubValueChanged(CCVars.DeadChatDiscordChannelId, v => _deadChannelId = string.IsNullOrEmpty(v) ? null : ulong.Parse(v));
    }

    private void OnMessage(Message msg)
    {
        if (msg.Author.IsBot)
            return;

        var text = msg.Content.ReplaceLineEndings(" ");

        if (msg.ChannelId == _oocChannelId)
        {
            _task.RunOnMainThread(() => _chatManager.SendHookOOC(msg.Author.Username, text));
        }
        else if (msg.ChannelId == _adminChannelId)
        {
            _task.RunOnMainThread(() => _chatManager.SendHookAdmin(msg.Author.Username, text));
        }
        else if (msg.ChannelId == _deadChannelId)
        {
            _task.RunOnMainThread(() => _chatManager.ChatMessageToAll(ChatChannel.Dead, text, $"DEAD: [bold](D){msg.Author.Username}:[/bold] {text}", EntityUid.Invalid, hideChat: false, recordReplay: false));
        }
    }

    public async Task SendMessage(string message, string author, ChatChannel channel)
    {
        ulong? channelId = channel switch
        {
            ChatChannel.OOC => _oocChannelId,
            ChatChannel.AdminChat => _adminChannelId,
            ChatChannel.Dead => _deadChannelId,
            _ => null
        };

        if (channelId == null)
            return;

        message = message.Replace("@", "\\@").Replace("<", "\\<").Replace("/", "\\/");
        await _discord.SendMessageAsync(channelId.Value, $"**{channel}**: `{author}`: {message}");
    }
}

