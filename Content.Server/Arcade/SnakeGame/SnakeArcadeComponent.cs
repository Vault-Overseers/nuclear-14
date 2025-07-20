using Robust.Shared.Player;

namespace Content.Server.Arcade.SnakeGame;

[RegisterComponent]
public sealed partial class SnakeArcadeComponent : Component
{
    public SnakeGame? Game;
    public EntityUid? Player;
    public readonly List<EntityUid> Spectators = new();
}
