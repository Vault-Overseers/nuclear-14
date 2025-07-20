using Content.Client.Arcade.UI;
using Content.Shared.Arcade.SnakeGame;
using Content.Shared.Input;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface.CustomControls;
using Robust.Shared.Localization;
using System.Numerics;

namespace Content.Client.Arcade;

public sealed class SnakeGameMenu : DefaultWindow
{
    private readonly Label _boardLabel;
    private readonly Label _scoreLabel;
    private readonly Button _newGameButton;

    public event Action<SnakeGamePlayerAction>? OnAction;

    public SnakeGameMenu()
    {
        Title = Loc.GetString("snake-menu-title");
        MinSize = SetSize = new Vector2(240, 260);

        var vBox = new BoxContainer { Orientation = LayoutOrientation.Vertical };

        _scoreLabel = new Label { Text = Loc.GetString("snake-menu-label-score", ("score", 0)) };
        vBox.AddChild(_scoreLabel);

        _boardLabel = new Label { VerticalExpand = true };
        vBox.AddChild(_boardLabel);

        _newGameButton = new Button { Text = Loc.GetString("snake-menu-button-new-game") };
        _newGameButton.OnPressed += _ => OnAction?.Invoke(SnakeGamePlayerAction.NewGame);
        vBox.AddChild(_newGameButton);

        Contents.AddChild(vBox);
    }

    public void UpdateState(string board, int score, bool gameOver)
    {
        _boardLabel.Text = board;
        _scoreLabel.Text = Loc.GetString("snake-menu-label-score", ("score", score));
        _newGameButton.Disabled = !gameOver;
    }

    protected override void KeyBindDown(GUIBoundKeyEventArgs args)
    {
        base.KeyBindDown(args);

        if (args.Function == ContentKeyFunctions.ArcadeUp)
            OnAction?.Invoke(SnakeGamePlayerAction.Up);
        else if (args.Function == ContentKeyFunctions.ArcadeDown)
            OnAction?.Invoke(SnakeGamePlayerAction.Down);
        else if (args.Function == ContentKeyFunctions.ArcadeLeft)
            OnAction?.Invoke(SnakeGamePlayerAction.Left);
        else if (args.Function == ContentKeyFunctions.ArcadeRight)
            OnAction?.Invoke(SnakeGamePlayerAction.Right);
    }
}
