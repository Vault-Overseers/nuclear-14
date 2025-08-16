using Content.Client.Arcade;
using Content.Shared.Arcade.SnakeGame;
using Content.Shared.CartridgeLoader;
using Content.Shared.CartridgeLoader.Cartridges;
using Robust.Client.GameObjects;
using Robust.Client.UserInterface;
using Content.Client.UserInterface.Fragments;
using Robust.Shared.Localization;

namespace Content.Client.CartridgeLoader.Cartridges;

public sealed partial class SnakeGameUi : UIFragment
{
    private SnakeGameMenu? _menu;

    public override Control GetUIFragmentRoot()
    {
        return _menu!;
    }

    public override void Setup(BoundUserInterface userInterface, EntityUid? fragmentOwner)
    {
        _menu = new SnakeGameMenu();
        _menu.OnAction += action =>
        {
            var ev = new SnakeGameUiMessageEvent(action);
            userInterface.SendMessage(new CartridgeUiMessage(ev));
        };
    }

    public override void UpdateState(BoundUserInterfaceState state)
    {
        if (state is SnakeGameMessages.SnakeGameUiState cast)
            _menu?.UpdateState(cast.Board, cast.Score, cast.GameOver);
    }
}
