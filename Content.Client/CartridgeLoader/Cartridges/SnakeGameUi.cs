using Content.Client.Arcade.UI;
using Content.Shared.Arcade.SnakeGame;
using Content.Shared.CartridgeLoader;
using Robust.Client.GameObjects;
using Robust.Client.UserInterface;
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
        _menu.OnAction += action => userInterface.SendMessage(new CartridgeUiMessage(new SnakeGameMessages.SnakeGamePlayerActionMessage(action)));
    }

    public override void UpdateState(BoundUserInterfaceState state)
    {
        if (state is SnakeGameMessages.SnakeGameUiState cast)
            _menu?.UpdateState(cast.Board, cast.Score, cast.GameOver);
    }
}
