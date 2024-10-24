using System.Threading;
using Content.Client._NC.DiscordAuth.DiscordGui;
using Content.Shared._NC.DiscordAuth;
using Robust.Client.State;
using Robust.Client.UserInterface;
using Robust.Shared.Network;
using Timer = Robust.Shared.Timing.Timer;

namespace Content.Client._NC.DiscordAuth;

public sealed class DiscordAuthState : State
{
    [Dependency] private readonly IUserInterfaceManager _userInterfaceManager = default!;
    [Dependency] private readonly IClientNetManager _netManager = default!;

    private DiscordAuthGui _gui = default!;

    private readonly CancellationTokenSource _checkTimerCancel = new();

    protected override void Startup()
    {
        _gui = new DiscordAuthGui();
        _userInterfaceManager.StateRoot.AddChild(_gui);

        Timer.SpawnRepeating(TimeSpan.FromSeconds(5),
            () =>
            {
                _netManager.ClientSendMessage(new MsgDiscordAuthCheck());
            },
            _checkTimerCancel.Token);
    }

    protected override void Shutdown()
    {
        _userInterfaceManager.StateRoot.RemoveChild(_gui);
        _checkTimerCancel.Cancel();
    }
}
