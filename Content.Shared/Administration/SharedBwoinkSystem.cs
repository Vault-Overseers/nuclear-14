﻿#nullable enable
using Robust.Shared.Network;
using Robust.Shared.Serialization;

namespace Content.Shared.Administration
{
    public abstract class SharedBwoinkSystem : EntitySystem
    {
        // System users
        public static NetUserId SystemUserId { get; } = new NetUserId(Guid.Empty);

        public override void Initialize()
        {
            base.Initialize();

            SubscribeNetworkEvent<BwoinkTextMessage>(OnBwoinkTextMessage);
        }

        protected virtual void OnBwoinkTextMessage(BwoinkTextMessage message, EntitySessionEventArgs eventArgs)
        {
            // Specific side code in target.
        }

        protected void LogBwoink(BwoinkTextMessage message)
        {
            Logger.InfoS("c.s.go.es.bwoink", $"@{message.ChannelId}: {message.Text}");
        }

        [Serializable, NetSerializable]
        public sealed class BwoinkTextMessage : EntityEventArgs
        {
            public DateTime SentAt { get; }
            public NetUserId ChannelId { get; }
            // This is ignored from the client.
            // It's checked by the client when receiving a message from the server for bwoink noises.
            // This could be a boolean "Incoming", but that would require making a second instance.
            public NetUserId TrueSender { get; }
            public string Text { get; }

            public BwoinkTextMessage(NetUserId channelId, NetUserId trueSender, string text, DateTime? sentAt = default)
            {
                SentAt = sentAt ?? DateTime.Now;
                ChannelId = channelId;
                TrueSender = trueSender;
                Text = text;
            }
        }
    }
}
