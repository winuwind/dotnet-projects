using Snakes;

namespace Snake.Game.Field;

public struct Cell (GameState.Types.Coord point, CellType type, int id = -1)
{
    public CellType Type = type;
    public int IdSnake = id;
}