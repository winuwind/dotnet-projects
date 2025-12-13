using Snakes;

namespace Snake.Game.Field;

public struct Cell (GameState.Types.Coord point, CellType type, int id = -1)
{
    public GameState.Types.Coord Coordinates = point;
    public CellType Type = type;
    public int IdSnake = id;
}