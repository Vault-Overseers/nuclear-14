using Content.Shared.Arcade.SnakeGame;
using Robust.Client.GameObjects;
using Robust.Client.UserInterface;

namespace Content.Client.Arcade.UI;

public sealed class SnakeGameBoundUserInterface : BoundUserInterface
{
    private SnakeGameMenu? _menu;

    public SnakeGameBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey)
    {
    }

    protected override void Open()
    {
        base.Open();
        _menu = this.CreateWindow<SnakeGameMenu>();
        _menu.OnAction += action => SendMessage(new SnakeGameMessages.SnakeGamePlayerActionMessage(action));
    }

    protected override void ReceiveMessage(BoundUserInterfaceMessage message)
    {
        if (message is SnakeGameMessages.SnakeGameStateMessage state)
        {
            _menu?.UpdateState(state.Board, state.Score, state.GameOver);
        }
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (!disposing)
            return;
        _menu?.Dispose();
    }
}
