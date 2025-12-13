using System.ComponentModel;
using Snakes;

namespace Snake.Game.Field;

public class FieldGame
{
    private readonly GameState.Types.Coord _sizeStruct;
    private readonly int _size;
    private readonly Cell[] _field;
    private readonly Random _random = new();

    private int _countFreeCells;

    public FieldGame(GameState.Types.Coord coordinates)
    {
        _sizeStruct = coordinates;
        _size = _sizeStruct.X * _sizeStruct.Y;
        _countFreeCells = _size;
       _field = new Cell[_size];
       for (var i = 0; i < _size; i++)
       {
           var coord = new GameState.Types.Coord
           {
               Y = i / _sizeStruct.X,
               X = i % _sizeStruct.X
           };
           _field[i] = new Cell(coord, CellType.Empty);
       }
    }

    public FieldGame(FieldGame other)
    {
        _sizeStruct = other._sizeStruct;
        _size = other._size;
        _field = other._field.ToArray();
        _countFreeCells = other._countFreeCells;
    }

    public GameState.Types.Coord GetSize()
    {
        return _sizeStruct;
    }

    private CellType GetCellType(GameState.Types.Coord coordinates)
    {
        return _field[(coordinates.Y + _sizeStruct.Y) % _sizeStruct.Y * _sizeStruct.X + (coordinates.X + _sizeStruct.X) % _sizeStruct.X].Type;
    }
    
    public Cell GetCell(int index)
    {
        return _field[index];
    }

    private void SetCell(GameState.Types.Coord coordinates, CellType type, int id = -1)
    {
        _field[(coordinates.Y + _sizeStruct.Y) % _sizeStruct.Y * _sizeStruct.X + (coordinates.X + _sizeStruct.X) % _sizeStruct.X].Type = type;
        _field[(coordinates.Y + _sizeStruct.Y) % _sizeStruct.Y * _sizeStruct.X + (coordinates.X + _sizeStruct.X) % _sizeStruct.X].IdSnake = id;
    }

    public List<GameState.Types.Coord> GenerateFood(int count)
    {
        _countFreeCells = 0;
        for (var i = 0; i < _size; i++)
        {
            if (_field[i].Type == CellType.Empty)
            {
                _countFreeCells++;
            }
        }
        
        var foods = new List<GameState.Types.Coord>();
        while (count-- > 0 && _countFreeCells > 0)
        {
            var index = _random.Next(_size);
            var indexCopy = index;
            do
            {
                if (_field[index % _size].Type == CellType.Empty)
                {
                    _field[index % _size].Type = CellType.Food;
                    var coords = new GameState.Types.Coord
                    {
                        Y = index / _sizeStruct.X,
                        X = index % _sizeStruct.X
                    };
                    foods.Add(coords);
                    break;
                }

                index = ++index % _size;
            } while (index != indexCopy);
        }
        return foods;
    }

    public void UpdateField(GameState state)
    {
        ResetField();
        foreach (var food in state.Foods)
        {
            if (food != null)
            {
                SetCell(food, CellType.Food);
                _countFreeCells--;
            }
        }

        foreach (var snake in state.Snakes)
        {
            AddSnake(snake);
        }
    }

    private void ResetField()
    {
        for (var i = 0; i < _size; i++)
        {
            _field[i].Type = CellType.Empty;
        }
        _countFreeCells = _size;
    }

    private bool CheckSquad(GameState.Types.Coord coordinate)
    {
        for (var i = 0; i < 5; i++)
        {
            for (var j = 0; j < 5; j++)
            {
                var coord = new GameState.Types.Coord
                {
                    X = coordinate.X + i,
                    Y = coordinate.Y + j
                };
                var type = GetCellType(coord);
                if ((i == 2 || j == 2) && Math.Abs(i - j) <= 1)
                {
                    if (type != CellType.Empty)
                    {
                        return false;
                    }
                }
                if (type != CellType.Empty && type != CellType.Food)
                {
                    return false;
                }
            }
        }
        return true;
    }

    public GameState.Types.Coord? FindEmptySquad()
    {
        var index = _random.Next(_size);
        var indexCopy = index;
        do
        {
            var coordinate = new GameState.Types.Coord
            {
                Y = index / _sizeStruct.X,
                X = index % _sizeStruct.X
            };
            if (CheckSquad(coordinate))
            {
                return coordinate;
            }

            index = ++index % _size;
        } while (index != indexCopy);

        return null;
    }

    public void AddSnake(GameState.Types.Snake snake)
    {
        var pointHead = snake.Points.First();
        var pointPrev = pointHead;
        for (var i = 1; i < snake.Points.Count; i++)
        {
            var offset = snake.Points[i];
            if (offset.X != 0 && offset.Y != 0)
            {
                throw new InvalidDataException();
            }

            for (var j = Math.Min(0, offset.X); j <= Math.Max(0, offset.X); j++)
            {
                if (j == 0)
                {
                    continue;
                }
                var coords = new GameState.Types.Coord
                {
                    X = pointPrev.X + j,
                    Y = pointPrev.Y
                };
                _countFreeCells--;
                SetCell(coords, CellType.SnakeBody, snake.PlayerId);
            }
            for (var j = Math.Min(0, offset.Y); j <= Math.Max(0, offset.Y); j++)
            {
                if (j == 0)
                {
                    continue;
                }
                var coords = new GameState.Types.Coord
                {
                    X = pointPrev.X,
                    Y = pointPrev.Y + j
                };
                _countFreeCells--;
                SetCell(coords, CellType.SnakeBody, snake.PlayerId);
            }

            pointPrev = new GameState.Types.Coord
            {
                X = (pointPrev.X + offset.X + _sizeStruct.X) % _sizeStruct.X,
                Y = (pointPrev.Y + offset.Y + _sizeStruct.Y) % _sizeStruct.Y
            };
        }

        _countFreeCells--;
        SetCell(pointHead, CellType.SnakeHead, snake.PlayerId);
    }
}