using Content.Client.Administration.Managers;
using Content.Client.Changelog;
using Content.Client.CharacterInterface;
using Content.Client.Chat.Managers;
using Content.Client.Options;
using Content.Client.Eui;
using Content.Client.Flash;
using Content.Client.GhostKick;
using Content.Client.HUD;
using Content.Client.Info;
using Content.Client.Input;
using Content.Client.IoC;
using Content.Client.Launcher;
using Content.Client.MainMenu;
using Content.Client.Parallax.Managers;
using Content.Client.Players.PlayTimeTracking;
using Content.Client.Preferences;
using Content.Client.Radiation;
using Content.Client.Screenshot;
using Content.Client.Singularity;
using Content.Client.Stylesheets;
using Content.Client.Viewport;
using Content.Client.Voting;
using Content.Shared.Administration;
using Content.Shared.AME;
using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.Dispenser;
using Content.Shared.Gravity;
using Content.Shared.Lathe;
using Content.Shared.Markers;
using Robust.Client;
using Robust.Client.Graphics;
using Robust.Client.Input;
using Robust.Client.Player;
using Robust.Client.State;
using Robust.Client.UserInterface;
using Robust.Shared.Configuration;
#if FULL_RELEASE
using Robust.Shared;
using Robust.Shared.Configuration;
#endif
using Robust.Shared.ContentPack;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Client.Entry
{
    public sealed class EntryPoint : GameClient
    {
        [Dependency] private readonly IPlayerManager _playerManager = default!;
        [Dependency] private readonly IEntityManager _entityManager = default!;
        [Dependency] private readonly IBaseClient _baseClient = default!;
        [Dependency] private readonly IGameController _gameController = default!;
        [Dependency] private readonly IStateManager _stateManager = default!;
        [Dependency] private readonly IComponentFactory _componentFactory = default!;
        [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
        [Dependency] private readonly IClientAdminManager _adminManager = default!;
        [Dependency] private readonly IParallaxManager _parallaxManager = default!;
        [Dependency] private readonly IConfigurationManager _configManager = default!;
        [Dependency] private readonly IStylesheetManager _stylesheetManager = default!;
        [Dependency] private readonly IScreenshotHook _screenshotHook = default!;
        [Dependency] private readonly ChangelogManager _changelogManager = default!;
        [Dependency] private readonly RulesManager _rulesManager = default!;
        [Dependency] private readonly ViewportManager _viewportManager = default!;
        [Dependency] private readonly IUserInterfaceManager _userInterfaceManager = default!;
        [Dependency] private readonly IInputManager _inputManager = default!;
        [Dependency] private readonly IMapManager _mapManager = default!;
        [Dependency] private readonly IOverlayManager _overlayManager = default!;
        [Dependency] private readonly IChatManager _chatManager = default!;
        [Dependency] private readonly IClientPreferencesManager _clientPreferencesManager = default!;
        [Dependency] private readonly EuiManager _euiManager = default!;
        [Dependency] private readonly IVoteManager _voteManager = default!;
        [Dependency] private readonly IGamePrototypeLoadManager _gamePrototypeLoadManager = default!;
        [Dependency] private readonly NetworkResourceManager _networkResources = default!;
        [Dependency] private readonly GhostKickManager _ghostKick = default!;
        [Dependency] private readonly ExtendedDisconnectInformationManager _extendedDisconnectInformation = default!;
        [Dependency] private readonly PlayTimeTrackingManager _playTimeTracking = default!;
        [Dependency] private readonly IGameHud _gameHud = default!;

        public const int NetBufferSizeOverride = 2;

        public override void Init()
        {
            ClientContentIoC.Register();

            foreach (var callback in TestingCallbacks)
            {
                var cast = (ClientModuleTestingCallbacks) callback;
                cast.ClientBeforeIoC?.Invoke();
            }

            IoCManager.BuildGraph();
            IoCManager.InjectDependencies(this);

#if FULL_RELEASE
            // if FULL_RELEASE, because otherwise this breaks some integration tests.
            IoCManager.Resolve<IConfigurationManager>().OverrideDefault(CVars.NetBufferSize, NetBufferSizeOverride);
#endif

            _componentFactory.DoAutoRegistrations();
            _componentFactory.IgnoreMissingComponents();

            // Do not add to these, they are legacy.
            _componentFactory.RegisterClass<SharedSpawnPointComponent>();
            _componentFactory.RegisterClass<SharedReagentDispenserComponent>();
            _componentFactory.RegisterClass<SharedGravityGeneratorComponent>();
            _componentFactory.RegisterClass<SharedAMEControllerComponent>();
            // Do not add to the above, they are legacy

            _prototypeManager.RegisterIgnore("accent");
            _prototypeManager.RegisterIgnore("material");
            _prototypeManager.RegisterIgnore("reaction"); //Chemical reactions only needed by server. Reactions checks are server-side.
            _prototypeManager.RegisterIgnore("gasReaction");
            _prototypeManager.RegisterIgnore("seed"); // Seeds prototypes are server-only.
            _prototypeManager.RegisterIgnore("barSign");
            _prototypeManager.RegisterIgnore("objective");
            _prototypeManager.RegisterIgnore("holiday");
            _prototypeManager.RegisterIgnore("aiFaction");
            _prototypeManager.RegisterIgnore("htnCompound");
            _prototypeManager.RegisterIgnore("htnPrimitive");
            _prototypeManager.RegisterIgnore("gameMap");
            _prototypeManager.RegisterIgnore("faction");
            _prototypeManager.RegisterIgnore("lobbyBackground");
            _prototypeManager.RegisterIgnore("advertisementsPack");
            _prototypeManager.RegisterIgnore("metabolizerType");
            _prototypeManager.RegisterIgnore("metabolismGroup");
            _prototypeManager.RegisterIgnore("salvageMap");
            _prototypeManager.RegisterIgnore("gamePreset");
            _prototypeManager.RegisterIgnore("gameRule");
            _prototypeManager.RegisterIgnore("worldSpell");
            _prototypeManager.RegisterIgnore("entitySpell");
            _prototypeManager.RegisterIgnore("instantSpell");
            _prototypeManager.RegisterIgnore("roundAnnouncement");
            _prototypeManager.RegisterIgnore("wireLayout");
            _prototypeManager.RegisterIgnore("alertLevels");
            _prototypeManager.RegisterIgnore("nukeopsRole");
            _prototypeManager.RegisterIgnore("flavor");

            _componentFactory.GenerateNetIds();
            _adminManager.Initialize();
            _stylesheetManager.Initialize();
            _screenshotHook.Initialize();
            _changelogManager.Initialize();
            _rulesManager.Initialize();
            _viewportManager.Initialize();
            _ghostKick.Initialize();
            _extendedDisconnectInformation.Initialize();
            _playTimeTracking.Initialize();
            _baseClient.PlayerJoinedServer += (_, _) => { _mapManager.CreateNewMapEntity(MapId.Nullspace);};

            //AUTOSCALING default Setup!
            _configManager.SetCVar("interface.resolutionAutoScaleUpperCutoffX", 1080);
            _configManager.SetCVar("interface.resolutionAutoScaleUpperCutoffY", 720);
            _configManager.SetCVar("interface.resolutionAutoScaleLowerCutoffX", 520);
            _configManager.SetCVar("interface.resolutionAutoScaleLowerCutoffY", 240);
            _configManager.SetCVar("interface.resolutionAutoScaleMinimum", 0.5f);
        }

        public override void PostInit()
        {
            base.PostInit();
            // Setup key contexts
            ContentContexts.SetupContexts(_inputManager.Contexts);

            _parallaxManager.LoadDefaultParallax();

            _overlayManager.AddOverlay(new SingularityOverlay());
            _overlayManager.AddOverlay(new FlashOverlay());
            _overlayManager.AddOverlay(new RadiationPulseOverlay());

            _baseClient.PlayerJoinedServer += SubscribePlayerAttachmentEvents;
            _baseClient.PlayerLeaveServer += UnsubscribePlayerAttachmentEvents;
            _gameHud.Initialize();
            _chatManager.Initialize();
            _clientPreferencesManager.Initialize();
            _euiManager.Initialize();
            _voteManager.Initialize();
            _gamePrototypeLoadManager.Initialize();
            _networkResources.Initialize();
            _userInterfaceManager.SetDefaultTheme("SS14DefaultTheme");

            _baseClient.RunLevelChanged += (_, args) =>
            {
                if (args.NewLevel == ClientRunLevel.Initialize)
                {
                    SwitchToDefaultState(args.OldLevel == ClientRunLevel.Connected ||
                                         args.OldLevel == ClientRunLevel.InGame);
                }
            };

            // Disable engine-default viewport since we use our own custom viewport control.
            _userInterfaceManager.MainViewport.Visible = false;

            SwitchToDefaultState();
        }

        public override void Update(ModUpdateLevel level, FrameEventArgs frameEventArgs)
        {
            base.Update(level, frameEventArgs);

            switch (level)
            {
                case ModUpdateLevel.FramePreEngine:
                    // TODO: Turn IChatManager into an EntitySystem and remove the line below.
                    IoCManager.Resolve<IChatManager>().FrameUpdate(frameEventArgs);
                    break;
            }
        }

        /// <summary>
        /// Subscribe events to the player manager after the player manager is set up
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        public void SubscribePlayerAttachmentEvents(object? sender, EventArgs args)
        {
            if (_playerManager.LocalPlayer != null)
            {
                _playerManager.LocalPlayer.EntityAttached += AttachPlayerToEntity;
                _playerManager.LocalPlayer.EntityDetached += DetachPlayerFromEntity;
            }
        }
        public void UnsubscribePlayerAttachmentEvents(object? sender, EventArgs args)
        {
            if (_playerManager.LocalPlayer != null)
            {
                _playerManager.LocalPlayer.EntityAttached -= AttachPlayerToEntity;
                _playerManager.LocalPlayer.EntityDetached -= DetachPlayerFromEntity;
            }
        }

        public void AttachPlayerToEntity(EntityAttachedEventArgs eventArgs)
        {
            // TODO This is shitcode. Move this to an entity system, FOR FUCK'S SAKE
            _entityManager.AddComponent<CharacterInterfaceComponent>(eventArgs.NewEntity);
        }

        public void DetachPlayerFromEntity(EntityDetachedEventArgs eventArgs)
        {
            // TODO This is shitcode. Move this to an entity system, FOR FUCK'S SAKE
            if (!_entityManager.Deleted(eventArgs.OldEntity))
            {
                _entityManager.RemoveComponent<CharacterInterfaceComponent>(eventArgs.OldEntity);
            }
        }

        private void SwitchToDefaultState(bool disconnected = false)
        {
            // Fire off into state dependent on launcher or not.

            if (_gameController.LaunchState.FromLauncher)
            {
                _stateManager.RequestStateChange<LauncherConnecting>();
                var state = (LauncherConnecting) _stateManager.CurrentState;

                if (disconnected)
                {
                    state.SetDisconnected();
                }
            }
            else
            {
                _stateManager.RequestStateChange<MainScreen>();
            }
        }
    }
}
