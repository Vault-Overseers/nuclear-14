using Robust.Shared.Serialization;
using Content.Shared.Arcade.SnakeGame;

namespace Content.Shared.CartridgeLoader.Cartridges;

[Serializable, NetSerializable]
public sealed class SnakeGameUiMessageEvent : CartridgeMessageEvent
{
    public readonly SnakeGamePlayerAction Action;

    public SnakeGameUiMessageEvent(SnakeGamePlayerAction action)
    {
        Action = action;
    }
}
