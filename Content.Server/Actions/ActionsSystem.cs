using Content.Server.Chat;
using Content.Server.Chat.Systems;
using Content.Shared.Actions;
using Content.Shared.Actions.ActionTypes;
using JetBrains.Annotations;
using Robust.Server.GameObjects;

namespace Content.Server.Actions
{
    [UsedImplicitly]
    public sealed class ActionsSystem : SharedActionsSystem
    {
        [Dependency] private readonly ChatSystem _chat = default!;
        [Dependency] private readonly MetaDataSystem _metaSystem = default!;

        public override void Initialize()
        {
            base.Initialize();
        }

        protected override bool PerformBasicActions(EntityUid user, ActionType action)
        {
            var result = base.PerformBasicActions(user, action);

            if (!string.IsNullOrWhiteSpace(action.Speech))
            {
                _chat.TrySendInGameICMessage(user, Loc.GetString(action.Speech), InGameICChatType.Speak, false);
                result = true;
            }

            return result;
        }
    }
}
