using Robust.Shared.Serialization;

namespace Content.Shared.Arcade.SnakeGame;

[Serializable, NetSerializable]
public enum SnakeGamePlayerAction
{
    NewGame,
    Up,
    Down,
    Left,
    Right
}
