using Google.Protobuf.Collections;
using Snakes;

namespace Snake.Game;
 
using Field;
using Config;


public class SnakesGame
{
    private readonly FieldGame _field;
    private readonly string _name;
    
    private GameState _game = new();

    private int _lastId = 0;

    public SnakesGame(GameState.Types.Coord size, GamePlayer player, string name)
    {
        _name = name;
        _field = new FieldGame(size);
        _game.Players = new GamePlayers();
        
        var foods = _field.GenerateFood(AppConstant.StaticFood + 1);
        foreach (var food in foods)
        {
            _game.Foods.Add(food);
        }
        AddPlayer(player);
    }
    
    public SnakesGame(GameState.Types.Coord size, string name)
    {
        _name = name;
        _field = new FieldGame(size);
        _game.Players = new GamePlayers();
    }

    public void UpdateLastId()
    {
        foreach (var player in _game.Players.Players)
        {
            _lastId = Math.Max(_lastId, player.Id);
        }

        _lastId++;
    }

    public GameState GetState()
    {
        return _game;
    }

    public String GetName()
    {
        return _name;
    }

    public bool CanJoin()
    {
        if (_field.FindEmptySquad() != null)
        {
            return true;
        }
        return false;
    }

    private GameState.Types.Coord GetNextPointHeadSnake(GameState.Types.Snake snake)
    {
        var pointHead = snake.Points.First();
        GameState.Types.Coord newPointHead = new(pointHead);
        switch (snake.HeadDirection)
        {
            case Direction.Up:
                newPointHead = new GameState.Types.Coord
                {
                    X = pointHead.X,
                    Y = pointHead.Y - 1
                };
                break;
            case Direction.Down:
                newPointHead = new GameState.Types.Coord
                {
                    X = pointHead.X,
                    Y = pointHead.Y + 1
                };
                break;
            case Direction.Right:
                newPointHead = new GameState.Types.Coord
                {
                    X = pointHead.X + 1,
                    Y = pointHead.Y
                };
                break;
            case Direction.Left:
                newPointHead = new GameState.Types.Coord
                {
                    X = pointHead.X - 1,
                    Y = pointHead.Y
                };
                break;
        }
        return newPointHead;
    }

    private void IncrementScorePlayer(int id)
    {
        foreach (var player in _game.Players.Players)
        {
            if (player.Id == id)
            {
                player.Score++;
                return;
            }
        }
    }

    private void ModulateSnake(List<Cell>[] tempField, GameState.Types.Snake snake)
    {
        var sizeStruct = _field.GetSize();
        
        var pointHead = snake.Points.First();
        
        var pointTail = new GameState.Types.Coord(pointHead);
        foreach (var point in snake.Points)
        {
            if (point.Equals(pointHead))
            {
                continue;
            }
            pointTail.X = (pointTail.X + point.X + sizeStruct.X) % sizeStruct.X;
            pointTail.Y = (pointTail.Y + point.Y + sizeStruct.Y) % sizeStruct.Y;
        }
        
        var newPointHead = GetNextPointHeadSnake(snake);
        tempField[(newPointHead.Y + sizeStruct.Y) % sizeStruct.Y * sizeStruct.X + (newPointHead.X + sizeStruct.X) % sizeStruct.X].Add(new Cell(newPointHead, CellType.SnakeHead, snake.PlayerId));
        snake.Points.Insert(0, newPointHead);
        snake.Points[1] = new GameState.Types.Coord
        {
            X = pointHead.X - newPointHead.X,
            Y = pointHead.Y - newPointHead.Y
        };
        newPointHead.X = (newPointHead.X + sizeStruct.X) % sizeStruct.X;
        newPointHead.Y = (newPointHead.Y + sizeStruct.Y) % sizeStruct.Y;
        if (tempField[(newPointHead.Y + sizeStruct.Y) % sizeStruct.Y * sizeStruct.X + (newPointHead.X + sizeStruct.X) % sizeStruct.X].First().Type == CellType.Food)
        {
            IncrementScorePlayer(snake.PlayerId);
            _game.Foods.Remove(newPointHead);
        }
        else
        {
            tempField[pointTail.Y * sizeStruct.X + pointTail.X].Add(new Cell(pointTail, CellType.Empty));
            snake.Points.RemoveAt(snake.Points.Count - 1);
        }
    }

    private RepeatedField<GameState.Types.Coord> KillPlayer(int id, List<GamePlayer> diedPlayers)
    {
        foreach (var player in _game.Players.Players)
        {
            if (player.Id == id)
            {
                // _game.Players.Players.Remove(player);
                player.Role = NodeRole.Viewer;
                diedPlayers.Add(player);
                break;
            }
        }

        var random = new Random();
        // var list = new List<GameState.Types.Coord>();
        var list = new RepeatedField<GameState.Types.Coord>();
        
        foreach (var snake in _game.Snakes)
        {
            if (snake.PlayerId == id)
            {
                var snakePoint = new GameState.Types.Coord(snake.Points.First());
                if (random.Next(0, 2) == 1)
                {
                    list.Add(snakePoint);
                }
                for (var i = 1; i < snake.Points.Count; i++)
                {
                    snakePoint.X = (snakePoint.X + snake.Points[i].X + _field.GetSize().X) % _field.GetSize().X;
                    snakePoint.Y = (snakePoint.Y + snake.Points[i].Y + _field.GetSize().Y) % _field.GetSize().Y;
                    if (random.Next(0, 2) == 1)
                    {
                        list.Add(new GameState.Types.Coord(snakePoint));
                    }
                }
                
                _game.Snakes.Remove(snake);
                break;
            }
        }
        return list;
    }
    
    public List<GamePlayer> Tick()
    {
        var sizeStruct = _field.GetSize();
        var size = sizeStruct.X * sizeStruct.Y;
        
        var tempField = new List<Cell>[size];
        for (var i = 0; i < size; i++)
        {
            var cell = _field.GetCell(i);
            if (cell.Type == CellType.SnakeHead)
            {
                cell.Type = CellType.SnakeBody;
            }
            tempField[i] = [cell];
        }

        foreach (var snake in _game.Snakes)
        {
            ModulateSnake(tempField, snake);
        }

        var diedPlayers = new List<GamePlayer>();
        foreach (var cells in tempField)
        {
            if (cells.Count == 1)
            {
                continue;
            }
            
            var countSnakeInCell = 0;
            var countSnakeBodyInCell = 0;

            foreach (var cell in cells)
            {
                if (cell.Type is CellType.SnakeHead or CellType.SnakeBody)
                {
                    countSnakeInCell++;
                }

                if (cell.Type is CellType.SnakeBody)
                {
                    countSnakeBodyInCell++;
                }

                if (cell.Type is CellType.Empty)
                {
                    countSnakeInCell -= countSnakeBodyInCell;
                    countSnakeBodyInCell = 0;
                }
            }

            if (countSnakeInCell > 1)
            {
                foreach (var cell in cells)
                {
                    if (cell.Type == CellType.SnakeHead)
                    {
                        _game.Foods.AddRange(KillPlayer(cell.IdSnake, diedPlayers));
                    }
                    else if(cell.Type == CellType.SnakeBody)
                    {
                        foreach (var player in _game.Players.Players)
                        {
                            if (player.Id == cell.IdSnake)
                            {
                                player.Score += countSnakeInCell - countSnakeBodyInCell;
                            }
                        }
                    }
                }
            }
        }
        UpdateGameState(_game);
        var listNewFoods = _field.GenerateFood(Math.Max(AppConstant.StaticFood + _game.Snakes.Count - _game.Foods.Count, 0));
        _game.Foods.AddRange(listNewFoods);
        
        _game.StateOrder++;
        return diedPlayers;
    }

    public void UpdateGameState(GameState state)
    {
        _game = state;
        _field.UpdateField(state);
    }

    public void ChangeDirectionSnake(int id, Direction direction)
    {
        foreach (var snake in _game.Snakes)
        {
            if (snake.PlayerId == id)
            {
                var offset = snake.Points[1];
                if (offset.X < 0 && direction == Direction.Left ||
                    offset.X > 0 && direction == Direction.Right ||
                    offset.Y < 0 && direction == Direction.Up ||
                    offset.Y > 0 && direction == Direction.Down)
                {
                    return;
                }
                snake.HeadDirection = direction;
                return;
            }
        }
    }

    public void AddPlayer(GamePlayer player)
    {
        if (player.Role == NodeRole.Viewer)
        {
            player.Id = _lastId++;
            _game.Players.Players.Add(player);
            return;
        }
        var coordinate = _field.FindEmptySquad();
        if (coordinate != null)
        {
            player.Id = _lastId++;
            _game.Players.Players.Add(player);
            var snake = GenerateSnake(coordinate, player.Id);
            snake.PlayerId = player.Id;
            _game.Snakes.Add(snake);
            _field.AddSnake(snake);
        }
        else
        {
            throw new FieldAccessException();
        }
    }

    private GameState.Types.Snake GenerateSnake(GameState.Types.Coord coordinatesSquad, int playerId)
    {
        var coordinatesHead = new GameState.Types.Coord
        {
            X = (coordinatesSquad.X + 2) % _field.GetSize().X,
            Y = (coordinatesSquad.Y + 2) % _field.GetSize().Y
        };
        
        Random random = new();
        var direction = (Direction) random.Next(1, 4);
        var snake = new GameState.Types.Snake
        {
            HeadDirection = direction,
            PlayerId = playerId,
            State = GameState.Types.Snake.Types.SnakeState.Alive
        };
        
        snake.Points.Add(coordinatesHead);
        var coordinatesTail = new GameState.Types.Coord
        {
            X = 0,
            Y = 0
        };
        switch (direction)
        {
            case Direction.Up: coordinatesTail.Y = 1; break;
            case Direction.Down: coordinatesTail.Y = -1; break;
            case Direction.Left: coordinatesTail.X = 1; break;
            case Direction.Right: coordinatesTail.X = -1; break;
        }
        snake.Points.Add(coordinatesTail);
        return snake;
    }
}