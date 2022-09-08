using Content.Server.Administration;
using Content.Server.Administration.Logs;
using Content.Server.Administration.Managers;
using Content.Server.Administration.Notes;
using Content.Server.Afk;
using Content.Server.AI.Utility;
using Content.Server.AI.Utility.Considerations;
using Content.Server.AI.WorldState;
using Content.Server.Chat.Managers;
using Content.Server.Connection;
using Content.Server.Database;
using Content.Server.EUI;
using Content.Server.GhostKick;
using Content.Server.Info;
using Content.Server.Maps;
using Content.Server.Module;
using Content.Server.MoMMI;
using Content.Server.NodeContainer.NodeGroups;
using Content.Server.Objectives;
using Content.Server.Objectives.Interfaces;
using Content.Server.Players.PlayTimeTracking;
using Content.Server.Preferences.Managers;
using Content.Server.ServerUpdates;
using Content.Server.Voting.Managers;
using Content.Shared.Administration;
using Content.Shared.Administration.Logs;
using Content.Shared.Kitchen;
using Content.Shared.Module;

namespace Content.Server.IoC
{
    internal static class ServerContentIoC
    {
        public static void Register()
        {
            IoCManager.Register<IChatManager, ChatManager>();
            IoCManager.Register<IChatSanitizationManager, ChatSanitizationManager>();
            IoCManager.Register<IMoMMILink, MoMMILink>();
            IoCManager.Register<IModuleManager, ServerModuleManager>();
            IoCManager.Register<IServerPreferencesManager, ServerPreferencesManager>();
            IoCManager.Register<IServerDbManager, ServerDbManager>();
            IoCManager.Register<RecipeManager, RecipeManager>();
            IoCManager.Register<INodeGroupFactory, NodeGroupFactory>();
            IoCManager.Register<BlackboardManager, BlackboardManager>();
            IoCManager.Register<ConsiderationsManager, ConsiderationsManager>();
            IoCManager.Register<IConnectionManager, ConnectionManager>();
            IoCManager.Register<ServerUpdateManager>();
            IoCManager.Register<IObjectivesManager, ObjectivesManager>();
            IoCManager.Register<IAdminManager, AdminManager>();
            IoCManager.Register<EuiManager, EuiManager>();
            IoCManager.Register<IVoteManager, VoteManager>();
            IoCManager.Register<IPlayerLocator, PlayerLocator>();
            IoCManager.Register<IAfkManager, AfkManager>();
            IoCManager.Register<IGameMapManager, GameMapManager>();
            IoCManager.Register<IGamePrototypeLoadManager, GamePrototypeLoadManager>();
            IoCManager.Register<RulesManager, RulesManager>();
            IoCManager.Register<RoleBanManager, RoleBanManager>();
            IoCManager.Register<NetworkResourceManager>();
            IoCManager.Register<IAdminNotesManager, AdminNotesManager>();
            IoCManager.Register<GhostKickManager>();
            IoCManager.Register<ISharedAdminLogManager, AdminLogManager>();
            IoCManager.Register<IAdminLogManager, AdminLogManager>();
            IoCManager.Register<PlayTimeTrackingManager>();
            IoCManager.Register<UserDbDataManager>();
        }
    }
}
