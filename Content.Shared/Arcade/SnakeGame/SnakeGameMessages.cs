using Robust.Shared.Serialization;

namespace Content.Shared.Arcade.SnakeGame;

public static class SnakeGameMessages
{
    [Serializable, NetSerializable]
    public sealed class SnakeGamePlayerActionMessage : BoundUserInterfaceMessage
    {
        public readonly SnakeGamePlayerAction Action;
        public SnakeGamePlayerActionMessage(SnakeGamePlayerAction action)
        {
            Action = action;
        }
    }

    [Serializable, NetSerializable]
    public sealed class SnakeGameStateMessage : BoundUserInterfaceMessage
    {
        public readonly string Board;
        public readonly int Score;
        public readonly bool GameOver;
        public SnakeGameStateMessage(string board, int score, bool gameOver)
        {
            Board = board;
            Score = score;
            GameOver = gameOver;
        }
    }

    [Serializable, NetSerializable]
    public sealed class SnakeGameUiState : BoundUserInterfaceState
    {
        public readonly string Board;
        public readonly int Score;
        public readonly bool GameOver;

        public SnakeGameUiState(string board, int score, bool gameOver)
        {
            Board = board;
            Score = score;
            GameOver = gameOver;
        }
    }

    [Serializable, NetSerializable]
    public enum SnakeGameUiKey
    {
        Key
    }
}
