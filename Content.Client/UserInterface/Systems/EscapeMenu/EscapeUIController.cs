﻿using Content.Client.Gameplay;
using Content.Client.HUD;
using Content.Client.Info;
using Content.Client.Links;
using Content.Client.UserInterface.Controls;
using JetBrains.Annotations;
using Robust.Client.Console;
using Robust.Client.Input;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controllers;
using Robust.Shared.Input;
using Robust.Shared.Input.Binding;
using Robust.Shared.Utility;
using static Robust.Client.UserInterface.Controls.BaseButton;

namespace Content.Client.UserInterface.Systems.EscapeMenu;

[UsedImplicitly]
public sealed class EscapeUIController : UIController, IOnStateEntered<GameplayState>, IOnStateExited<GameplayState>
{
    [Dependency] private readonly IClientConsoleHost _console = default!;
    [Dependency] private readonly IUriOpener _uri = default!;
    [Dependency] private readonly IGameHud _gameHud = default!;

    private Options.UI.EscapeMenu? _escapeWindow;

    public void OnStateEntered(GameplayState state)
    {
        DebugTools.Assert(_escapeWindow == null);
        _gameHud.EscapeButtonToggled += GameHudOnEscapeButtonToggled;

        _escapeWindow = UIManager.CreateWindow<Options.UI.EscapeMenu>();
        _escapeWindow.OnClose += () => { _gameHud.EscapeButtonDown = false; };
        _escapeWindow.OnOpen +=  () => { _gameHud.EscapeButtonDown = true; };

        _escapeWindow.ChangelogButton.OnPressed += _ =>
        {
            CloseEscapeWindow();
            // Put this back when changelog button no longer controls the window
            // UIManager.GetUIController<ChangelogUIController>().ToggleWindow();
        };

        _escapeWindow.RulesButton.OnPressed += _ =>
        {
            CloseEscapeWindow();
            new RulesAndInfoWindow().Open();
        };

        _escapeWindow.DisconnectButton.OnPressed += _ =>
        {
            CloseEscapeWindow();
            _console.ExecuteCommand("disconnect");
        };

        _escapeWindow.OptionsButton.OnPressed += _ =>
        {
            CloseEscapeWindow();
            UIManager.GetUIController<OptionsUIController>().OpenWindow();
        };

        _escapeWindow.QuitButton.OnPressed += _ =>
        {
            CloseEscapeWindow();
            _console.ExecuteCommand("quit");
        };

        _escapeWindow.WikiButton.OnPressed += _ =>
        {
            _uri.OpenUri(UILinks.Wiki);
        };

        CommandBinds.Builder
            .Bind(EngineKeyFunctions.EscapeMenu,
                InputCmdHandler.FromDelegate(_ => ToggleWindow()))
            .Register<EscapeUIController>();
    }

    private void GameHudOnEscapeButtonToggled(bool obj)
    {
        ToggleWindow();
    }

    public void OnStateExited(GameplayState state)
    {
        if (_escapeWindow != null)
        {
            _escapeWindow.Dispose();
            _escapeWindow = null;
        }
        _gameHud.EscapeButtonToggled -= GameHudOnEscapeButtonToggled;
        _gameHud.EscapeButtonDown = false;

        CommandBinds.Unregister<EscapeUIController>();
    }

    private void CloseEscapeWindow()
    {
        _escapeWindow?.Close();
    }

    private void ToggleWindow()
    {
        if (_escapeWindow == null)
            return;

        if (_escapeWindow.IsOpen)
        {
            CloseEscapeWindow();
        }
        else
        {
            _escapeWindow.OpenCentered();
        }
    }
}
