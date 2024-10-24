using Content.Shared._NC.DiscordAuth;
using Robust.Client.State;
using Robust.Shared.Network;

namespace Content.Client._NC.DiscordAuth;

public sealed class DiscordAuthManager
{
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly IStateManager _state = default!;

    public string AuthLink = default!;

    public void Initialize()
    {
        _net.RegisterNetMessage<MsgDiscordAuthRequired>(OnDiscordAuthRequired);
    }

    public void OnDiscordAuthRequired(MsgDiscordAuthRequired args)
    {
        AuthLink = args.Link;
        _state.RequestStateChange<DiscordAuthState>();
    }
}
