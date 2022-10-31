using Content.Shared.Chat;
using Robust.Server.Player;
using Robust.Shared.Network;
using Robust.Shared.Player;

namespace Content.Server.Chat.Managers
{
    public interface IChatManager
    {
        void Initialize();

        /// <summary>
        ///     Dispatch a server announcement to every connected player.
        /// </summary>
        /// <param name="message"></param>
        /// <param name="colorOverride">Override the color of the message being sent.</param>
        void DispatchServerAnnouncement(string message, Color? colorOverride = null);

        void DispatchServerMessage(IPlayerSession player, string message);

        void TrySendOOCMessage(IPlayerSession player, string message, OOCChatType type);

        void SendHookOOC(string sender, string message);
        void SendAdminAnnouncement(string message);

        void ChatMessageToOne(ChatChannel channel, string message, string wrappedMessage, EntityUid source, bool hideChat,
            INetChannel client, Color? colorOverride = null);
        void ChatMessageToMany(ChatChannel channel, string message, string wrappedMessage, EntityUid source, bool hideChat,
            List<INetChannel> clients, Color? colorOverride = null);
        void ChatMessageToManyFiltered(Filter filter, ChatChannel channel, string message, string wrappedMessage, EntityUid source, bool hideChat, Color? colorOverride);
        void ChatMessageToAll(ChatChannel channel, string message, string wrappedMessage, Color? colorOverride = null);

        bool MessageCharacterLimit(IPlayerSession player, string message);
    }
}
