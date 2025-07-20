using Content.Shared.Arcade.SnakeGame;
using Robust.Server.GameObjects;
using Robust.Shared.Random;
using Robust.Shared.Maths;
using Content.Server.UserInterface;
using Content.Server.Arcade;
using System.Text;

namespace Content.Server.Arcade.SnakeGame;

public sealed partial class SnakeGame
{
    private readonly IEntityManager _entityManager = default!;
    private readonly IRobustRandom _random = default!;
    private readonly ArcadeSystem _arcadeSystem = default!;
    private readonly UserInterfaceSystem _ui = default!;

    private readonly EntityUid _owner;

    private Vector2i _direction = new(1, 0);
    private readonly List<Vector2i> _snake = new();
    private Vector2i _food;
    private float _accumulator;
    private bool _running;
    private bool _gameOver;
    public int Score { get; private set; }
    public bool GameOver => _gameOver;
    public string Board => RenderBoard();

    private const int Width = 20;
    private const int Height = 10;

    public SnakeGame(EntityUid owner)
    {
        _owner = owner;
        IoCManager.InjectDependencies(this);
        _arcadeSystem = _entityManager.System<ArcadeSystem>();
        _ui = _entityManager.System<UserInterfaceSystem>();
        Reset();
    }

    private void Reset()
    {
        _running = false;
        _gameOver = false;
        Score = 0;
        _direction = new(1, 0);
        _snake.Clear();
        _snake.Add(new Vector2i(Width/2, Height/2));
        SpawnFood();
    }

    private void SpawnFood()
    {
        do
        {
            _food = new Vector2i(_random.Next(1, Width-1), _random.Next(1, Height-1));
        } while (_snake.Contains(_food));
    }

    public void GameTick(float frameTime)
    {
        if (!_running || _gameOver)
            return;

        _accumulator += frameTime;
        if (_accumulator < 0.25f)
            return;
        _accumulator = 0f;

        var newHead = _snake[0] + _direction;
        if (newHead.X <= 0 || newHead.X >= Width-1 || newHead.Y <=0 || newHead.Y >= Height-1 || _snake.Contains(newHead))
        {
            _gameOver = true;
            if (_entityManager.TryGetComponent<SnakeArcadeComponent>(_owner, out var comp) &&
                comp.Player != null &&
                _entityManager.TryGetComponent<MetaDataComponent>(comp.Player.Value, out var meta))
            {
                _arcadeSystem.RegisterHighScore(meta.EntityName, Score);
            }
            UpdateAll();
            return;
        }

        _snake.Insert(0, newHead);
        if (newHead == _food)
        {
            Score += 1;
            SpawnFood();
        }
        else
        {
            _snake.RemoveAt(_snake.Count-1);
        }

        UpdateAll();
    }

    public void ProcessInput(SnakeGamePlayerAction action)
    {
        switch (action)
        {
            case SnakeGamePlayerAction.NewGame:
                Reset();
                _running = true;
                UpdateAll();
                break;
            case SnakeGamePlayerAction.Up:
                if (_direction != new Vector2i(0, 1))
                    _direction = new Vector2i(0, -1);
                break;
            case SnakeGamePlayerAction.Down:
                if (_direction != new Vector2i(0, -1))
                    _direction = new Vector2i(0, 1);
                break;
            case SnakeGamePlayerAction.Left:
                if (_direction != new Vector2i(1, 0))
                    _direction = new Vector2i(-1, 0);
                break;
            case SnakeGamePlayerAction.Right:
                if (_direction != new Vector2i(-1, 0))
                    _direction = new Vector2i(1, 0);
                break;
        }
    }

    private string RenderBoard()
    {
        var board = new char[Height, Width];
        for (var y=0;y<Height;y++)
        for (var x=0;x<Width;x++)
        {
            if (x==0 || x==Width-1 || y==0 || y==Height-1)
                board[y,x] = '#';
            else
                board[y,x] = ' ';
        }

        foreach (var pos in _snake)
            board[pos.Y,pos.X] = 'o';
        board[_snake[0].Y,_snake[0].X] = '@';
        board[_food.Y,_food.X] = '*';

        var sb = new StringBuilder();
        for (var y=0;y<Height;y++)
        {
            for (var x=0;x<Width;x++)
                sb.Append(board[y,x]);
            sb.Append('\n');
        }
        return sb.ToString();
    }

    public SnakeGameMessages.SnakeGameUiState GetState()
    {
        return new SnakeGameMessages.SnakeGameUiState(RenderBoard(), Score, _gameOver);
    }

    public void UpdateUi(EntityUid? actor = null)
    {
        var msg = new SnakeGameMessages.SnakeGameStateMessage(RenderBoard(), Score, _gameOver);
        if (actor.HasValue)
            _ui.ServerSendUiMessage(_owner, SnakeGameMessages.SnakeGameUiKey.Key, msg, actor.Value);
        else
            _ui.ServerSendUiMessage(_owner, SnakeGameMessages.SnakeGameUiKey.Key, msg);
    }

    private void UpdateAll()
    {
        UpdateUi();
        foreach (var spec in _entityManager.GetComponent<SnakeArcadeComponent>(_owner).Spectators)
        {
            UpdateUi(spec);
        }
    }

    public void RecordHighscore(string name)
    {
        _arcadeSystem.RegisterHighScore(name, Score);
    }
}
