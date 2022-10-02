﻿using Content.Client.Gameplay;
using Content.Client.HUD;
using Content.Client.Markers;
using Content.Client.Sandbox;
using Content.Client.SubFloor;
using Content.Client.UserInterface.Systems.DecalPlacer;
using Content.Client.UserInterface.Systems.Sandbox.Windows;
using Content.Shared.Input;
using JetBrains.Annotations;
using Robust.Client.Debugging;
using Robust.Client.Graphics;
using Robust.Client.Input;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controllers;
using Robust.Client.UserInterface.Controllers.Implementations;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Input.Binding;
using Robust.Shared.Map;
using Robust.Shared.Players;
using Robust.Shared.Utility;

namespace Content.Client.UserInterface.Systems.Sandbox;

// TODO hud refactor should part of this be in engine?
[UsedImplicitly]
public sealed class SandboxUIController : UIController, IOnStateChanged<GameplayState>, IOnSystemChanged<SandboxSystem>
{
    [Dependency] private readonly IEyeManager _eye = default!;
    [Dependency] private readonly IInputManager _input = default!;
    [Dependency] private readonly ILightManager _light = default!;
    [Dependency] private readonly IGameHud _gameHud = default!;

    [UISystemDependency] private readonly DebugPhysicsSystem _debugPhysics = default!;
    [UISystemDependency] private readonly MarkerSystem _marker = default!;
    [UISystemDependency] private readonly SandboxSystem _sandbox = default!;
    [UISystemDependency] private readonly SubFloorHideSystem _subfloorHide = default!;

    private SandboxWindow? _window;

    // TODO hud refactor cache
    private EntitySpawningUIController EntitySpawningController => UIManager.GetUIController<EntitySpawningUIController>();
    private TileSpawningUIController TileSpawningController => UIManager.GetUIController<TileSpawningUIController>();
    private DecalPlacerUIController DecalPlacerController => UIManager.GetUIController<DecalPlacerUIController>();

    public void OnStateEntered(GameplayState state)
    {
        DebugTools.Assert(_window == null);
        EnsureWindow();
        _gameHud.SandboxButtonToggled += GameHudOnSandboxButtonToggled;

        _input.SetInputCommand(ContentKeyFunctions.OpenEntitySpawnWindow,
            InputCmdHandler.FromDelegate(_ => EntitySpawningController.ToggleWindow()));
        _input.SetInputCommand(ContentKeyFunctions.OpenSandboxWindow,
            InputCmdHandler.FromDelegate(_ => ToggleWindow()));
        _input.SetInputCommand(ContentKeyFunctions.OpenTileSpawnWindow,
            InputCmdHandler.FromDelegate(_ => TileSpawningController.ToggleWindow()));
        _input.SetInputCommand(ContentKeyFunctions.OpenDecalSpawnWindow,
            InputCmdHandler.FromDelegate(_ => DecalPlacerController.ToggleWindow()));

        CommandBinds.Builder
            .Bind(ContentKeyFunctions.EditorCopyObject, new PointerInputCmdHandler(Copy))
            .Register<SandboxSystem>();
    }

    private void EnsureWindow()
    {
        if(_window is { Disposed: false })
            return;
        _window = UIManager.CreateWindow<SandboxWindow>();
        _window.OnClose += () => { _gameHud.SandboxButtonDown = false; };
        _window.OnOpen += () => { _gameHud.SandboxButtonDown = true; };
        _window.ToggleLightButton.Pressed = !_light.Enabled;
        _window.ToggleFovButton.Pressed = !_eye.CurrentEye.DrawFov;
        _window.ToggleShadowsButton.Pressed = !_light.DrawShadows;
        _window.ToggleSubfloorButton.Pressed = _subfloorHide.ShowAll;
        _window.ShowMarkersButton.Pressed = _marker.MarkersVisible;
        _window.ShowBbButton.Pressed = (_debugPhysics.Flags & PhysicsDebugFlags.Shapes) != 0x0;

        _window.RespawnButton.OnPressed += _ => _sandbox.Respawn();
        _window.SpawnTilesButton.OnPressed += _ => TileSpawningController.ToggleWindow();
        _window.SpawnEntitiesButton.OnPressed += _ => EntitySpawningController.ToggleWindow();
        _window.SpawnDecalsButton.OnPressed += _ => DecalPlacerController.ToggleWindow();
        _window.GiveFullAccessButton.OnPressed += _ => _sandbox.GiveAdminAccess();
        _window.GiveAghostButton.OnPressed += _ => _sandbox.GiveAGhost();
        _window.ToggleLightButton.OnToggled += _ => _sandbox.ToggleLight();
        _window.ToggleFovButton.OnToggled += _ => _sandbox.ToggleFov();
        _window.ToggleShadowsButton.OnToggled += _ => _sandbox.ToggleShadows();
        _window.SuicideButton.OnPressed += _ => _sandbox.Suicide();
        _window.ToggleSubfloorButton.OnPressed += _ => _sandbox.ToggleSubFloor();
        _window.ShowMarkersButton.OnPressed += _ => _sandbox.ShowMarkers();
        _window.ShowBbButton.OnPressed += _ => _sandbox.ShowBb();
        _window.MachineLinkingButton.OnPressed += _ => _sandbox.MachineLinking();
    }

    private void GameHudOnSandboxButtonToggled(bool pressed)
    {
        ToggleWindow();
    }

    public void OnStateExited(GameplayState state)
    {
        if (_window != null)
        {
            _window.Dispose();
            _window = null;
        }

        _gameHud.SandboxButtonToggled -= GameHudOnSandboxButtonToggled;
        _gameHud.SandboxButtonDown = false;

        CommandBinds.Unregister<SandboxSystem>();
    }

    public void OnSystemLoaded(SandboxSystem system)
    {
        system.SandboxDisabled += CloseAll;
    }

    public void OnSystemUnloaded(SandboxSystem system)
    {
        system.SandboxDisabled -= CloseAll;
    }

    private void CloseAll()
    {
        _window?.Close();
        EntitySpawningController.CloseWindow();
        TileSpawningController.CloseWindow();
    }

    private bool Copy(ICommonSession? session, EntityCoordinates coords, EntityUid uid)
    {
        return _sandbox.Copy(session, coords, uid);
    }

    private void ToggleWindow()
    {
        if (_window == null)
            return;
        if (_sandbox.SandboxAllowed && _window.IsOpen != true)
        {
            _window.OpenCentered();
        }
        else
        {
            _window.Close();
        }
    }
}
