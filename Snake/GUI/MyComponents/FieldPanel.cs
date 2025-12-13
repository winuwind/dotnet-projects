using Google.Protobuf.Collections;
using Snake.Config;
using Snake.Control;
using Snakes;

namespace SnakeGame.GUI.MyComponents;

public class FieldPanel : Panel
{
    private readonly List<Color> _colors = [Color.BlueViolet, Color.Aqua, Color.DarkBlue, Color.LawnGreen, Color.Yellow, Color.Blue, Color.Brown, Color.DarkGreen, Color.DarkRed];
    private readonly Controller _controller;
    private readonly MainForm _mainForm;
    
    private GameState _state = new GameState();
    private int _gridWidth = AppConstant.Size.X;
    private int _gridHeight = AppConstant.Size.Y;
    private float _cellSize = AppConstant.Size.X;
    private float _offsetX = AppConstant.Size.X;
    private float _offsetY = AppConstant.Size.Y;

    private GameState.Types.Snake[] _snakes = [];
    private bool _gridIsDrawn = false;

    private int _margin = 5;
    
    private enum SnakeTurn
    {
        UpRight,
        UpLeft,
        DownRight,
        DownLeft,
        LeftUp,
        LeftDown,
        RightUp,
        RightDown
    }
    
    public FieldPanel(MainForm main, Controller controller, int width)
    {
        _mainForm = main;
        _controller = controller;
        Width = width - 400;
        Init();
        Paint += RePaintField;
    }

    private void Init()
    {
        Dock = DockStyle.Left;
        BackColor = Color.White;
    }

    public void SetGameState(GameState state)
    {
        _state = state;
    }
    
    private RectangleF GetCellRect(int x, int y)
    {
        return new RectangleF(
            _offsetX + x * _cellSize,
            _offsetY + y * _cellSize,
            _cellSize,
            _cellSize
        );
    }

    private void DrawHorizontalFill(Graphics g, RectangleF cell, Color color)
    {
        using var br = new SolidBrush(color);

        g.FillRectangle(br,
            cell.X,
            cell.Y + _margin,
            cell.Width,
            cell.Height - 2 * _margin
        );
    }

    private void DrawVerticalFill(Graphics g, RectangleF cell, Color color)
    {
        using var br = new SolidBrush(color);

        g.FillRectangle(br,
            cell.X + _margin,
            cell.Y,
            cell.Width - 2 * _margin,
            cell.Height
        );
    }
    
    private void DrawSnakeCorner(Graphics g, RectangleF cell, SnakeTurn turn, Color color)
    {
        using var brush = new SolidBrush(color);
        
        g.FillRectangle(brush,
            cell.X + _margin,
            cell.Y + _margin,
            cell.Width - 2 * _margin,
            cell.Height - 2 * _margin
        );

        switch (turn)
        {
            case SnakeTurn.UpRight or SnakeTurn.RightUp:
                g.FillRectangle(brush,
                    cell.X + _margin,
                    cell.Y + _margin,
                    cell.Width - _margin,
                    cell.Height - 2 * _margin);
                g.FillRectangle(brush,
                    cell.X + _margin,
                    cell.Y,
                    cell.Width - 2 * _margin,
                    cell.Height - _margin);
                break;

            case SnakeTurn.UpLeft or SnakeTurn.LeftUp:
                g.FillRectangle(brush,
                    cell.X,
                    cell.Y + _margin,
                    cell.Width - _margin,
                    cell.Height - 2 * _margin);
                g.FillRectangle(brush,
                    cell.X + _margin,
                    cell.Y,
                    cell.Width - 2 * _margin,
                    cell.Height - _margin);
                break;

            case SnakeTurn.DownRight or SnakeTurn.RightDown:
                g.FillRectangle(brush,
                    cell.X + _margin,
                    cell.Y + _margin,
                    cell.Width - _margin,
                    cell.Height - 2 * _margin);
                g.FillRectangle(brush,
                    cell.X + _margin,
                    cell.Y + _margin,
                    cell.Width - 2 * _margin,
                    cell.Height - _margin);
                break;

            case SnakeTurn.DownLeft or SnakeTurn.LeftDown:
                g.FillRectangle(brush,
                    cell.X,
                    cell.Y + _margin,
                    cell.Width - _margin,
                    cell.Height - 2 * _margin);
                g.FillRectangle(brush,
                    cell.X + _margin,
                    cell.Y + _margin,
                    cell.Width - 2 * _margin,
                    cell.Height - _margin);
                break;
        }
    }
    
    private void DrawImageInCell(Graphics g, Image img, RectangleF cellRect, float margin)
    {
        var r = new RectangleF(
            cellRect.X + margin,
            cellRect.Y + margin,
            cellRect.Width - margin * 2f,
            cellRect.Height - margin * 2f
        );

        g.DrawImage(img, r);
    }

    private void DrawHeadSnake(GameState.Types.Snake snake, Graphics g)
    {
        var pointHead = snake.Points.First();
        var color = Color.Black;
        var rect = GetCellRect(pointHead.X, pointHead.Y);
        if (_controller.GetId() == snake.PlayerId)
        {
            color = Color.Red;
            _mainForm.SetSnake(snake);
        }
        if (snake.HeadDirection is Direction.Down or Direction.Up && snake.Points[1].Y != 0)
        {
            DrawVerticalFill(g, rect, color);
        }
        else if (snake.HeadDirection is Direction.Down or Direction.Up && snake.Points[1].X != 0)
        {
            if (snake.HeadDirection is Direction.Down && snake.Points[1].X > 0)
            {
                DrawSnakeCorner(g, rect, SnakeTurn.DownRight, color);
            }
            else if (snake.HeadDirection is Direction.Down && snake.Points[1].X < 0)
            {
                DrawSnakeCorner(g, rect, SnakeTurn.DownLeft, color);
            }
            else if (snake.HeadDirection is Direction.Up && snake.Points[1].X > 0)
            {
                DrawSnakeCorner(g, rect, SnakeTurn.UpRight, color);
            }
            else if (snake.HeadDirection is Direction.Up && snake.Points[1].X < 0)
            {
                DrawSnakeCorner(g, rect, SnakeTurn.UpLeft, color);
            }
        }
        else if (snake.HeadDirection is Direction.Left or Direction.Right && snake.Points[1].X != 0)
        {
            DrawHorizontalFill(g, rect, color);
        }
        else if (snake.HeadDirection is Direction.Left or Direction.Right && snake.Points[1].Y != 0)
        {
            if (snake.HeadDirection is Direction.Left && snake.Points[1].Y > 0)
            {
                DrawSnakeCorner(g, rect, SnakeTurn.LeftDown, color);
            }
            else if (snake.HeadDirection is Direction.Left && snake.Points[1].Y < 0)
            {
                DrawSnakeCorner(g, rect, SnakeTurn.LeftUp, color);
            }
            else if (snake.HeadDirection is Direction.Right && snake.Points[1].Y > 0)
            {
                DrawSnakeCorner(g, rect, SnakeTurn.RightDown, color);
            }
            else if (snake.HeadDirection is Direction.Right && snake.Points[1].Y < 0)
            {
                DrawSnakeCorner(g, rect, SnakeTurn.RightUp, color);
            }
        }
        DrawCellLabel(g, rect, snake.PlayerId.ToString());
    }

    private void DrawPartSnake(Direction directionPrev, Direction direction, Color color, Graphics g, RectangleF rect)
    {
        if (directionPrev == direction)
        {
            if (direction is Direction.Down or Direction.Up)
            {
                DrawVerticalFill(g, rect, color);
            }
            else
            {
                DrawHorizontalFill(g, rect, color);
            }
        }
        else
        {
            if (direction is Direction.Down && directionPrev is Direction.Left)
            {
                DrawSnakeCorner(g, rect, SnakeTurn.LeftUp, color);
            }
            else if (direction is Direction.Down && directionPrev is Direction.Right)
            {
                DrawSnakeCorner(g, rect, SnakeTurn.RightUp, color);
            }
            
            else if(direction is Direction.Right && directionPrev is Direction.Up)
            {
                DrawSnakeCorner(g, rect, SnakeTurn.UpLeft, color);
            }
            else if (direction is Direction.Right && directionPrev is Direction.Down)
            {
                DrawSnakeCorner(g, rect, SnakeTurn.DownLeft, color);
            }
            
            else if (direction is Direction.Left && directionPrev is Direction.Up)
            {
                DrawSnakeCorner(g, rect, SnakeTurn.UpRight, color);
            }
            else if (direction is Direction.Left && directionPrev is Direction.Down)
            {
                DrawSnakeCorner(g, rect, SnakeTurn.DownRight, color);
            }
            
            else if (direction is Direction.Up && directionPrev is Direction.Left)
            {
                DrawSnakeCorner(g, rect, SnakeTurn.LeftDown, color);
            }
            else if (direction is Direction.Up && directionPrev is Direction.Right)
            {
                DrawSnakeCorner(g, rect, SnakeTurn.RightDown, color);
            }
        }
    }
    
    private void DrawCellLabel(Graphics g, RectangleF cell, string text)
    {
        using var sf = new StringFormat();
        sf.Alignment = StringAlignment.Center;
        sf.LineAlignment = StringAlignment.Center;

        using var font = new Font("Arial", (cell.Height - 2 * _margin) * 0.4f, FontStyle.Bold);
        using var brush = new SolidBrush(Color.Cyan);

        g.DrawString(text, font, brush, cell, sf);
    }

    public void Reset()
    {
        _gridIsDrawn = false;
    }

    private void RePaintField(object? sender, EventArgs e)
    {
        _gridIsDrawn = false;
        PaintField();
    }
    
    public void PaintField()
    {
        if (InvokeRequired)
        {
            Invoke(new Action(PaintField));
            return;
        }
        // var g = e.Graphics;
        var g = CreateGraphics();

        if (!_gridIsDrawn)
        {
            DrawGrid(this, g);
            _gridIsDrawn = true;
        }
        
        var headImg = Image.FromFile("apple.jpg");

        foreach (var food in _state.Foods)
        {
            DrawImageInCell(g, headImg, GetCellRect(food.X, food.Y), 1f);
        }

        foreach (var snake in _snakes)
        {
            var point = new GameState.Types.Coord(snake.Points.First());
            ClearCell(g, point.X, point.Y);
            for (var i = 1; i < snake.Points.Count; i++)
            {
                point.X = (point.X + snake.Points[i].X + AppConstant.Size.X) % AppConstant.Size.X;
                point.Y = (point.Y + snake.Points[i].Y + AppConstant.Size.Y) % AppConstant.Size.Y;
                ClearCell(g, point.X, point.Y);
            }
        }
        
        foreach (var snake in _state.Snakes)
        {
            DrawHeadSnake(snake, g);
            
            var pointPrev = new GameState.Types.Coord(snake.Points.First());
            var rect = GetCellRect(pointPrev.X, pointPrev.Y);
            Direction directionPrev;
            if (snake.Points[1].X > 0)
            {
                directionPrev = Direction.Left;
            }
            else if (snake.Points[1].X < 0)
            {
                directionPrev = Direction.Right;
            }
            else if (snake.Points[1].Y > 0)
            {
                directionPrev = Direction.Up;
            }
            else
            {
                directionPrev = Direction.Down;
            }
            for (var i = 2; i < snake.Points.Count; i++)
            {
                Direction direction;
                if (snake.Points[i].X > 0)
                {
                    direction = Direction.Left;
                }
                else if (snake.Points[i].X < 0)
                {
                    direction = Direction.Right;
                }
                else if (snake.Points[i].Y > 0)
                {
                    direction = Direction.Up;
                }
                else
                {
                    direction = Direction.Down;
                }

                pointPrev.X = (pointPrev.X + snake.Points[i - 1].X + AppConstant.Size.X) % AppConstant.Size.X;
                pointPrev.Y = (pointPrev.Y + snake.Points[i - 1].Y + AppConstant.Size.Y) % AppConstant.Size.Y;
                rect = GetCellRect(pointPrev.X, pointPrev.Y);
                
                DrawPartSnake(directionPrev, direction, _colors[snake.PlayerId % _colors.Count], g, rect);
                
                directionPrev = direction;
            }
            rect = GetCellRect((pointPrev.X + snake.Points[^1].X + AppConstant.Size.X) % AppConstant.Size.X, (pointPrev.Y + snake.Points[^1].Y + AppConstant.Size.Y) % AppConstant.Size.Y);
            DrawPartSnake(directionPrev, directionPrev, _colors[snake.PlayerId % _colors.Count], g, rect);
        }

        _snakes = _state.Snakes.ToArray();
        
        g.Dispose();
    }
    
    private void DrawGrid(object? sender, Graphics g)
    {
        var panel = (Panel)sender!;
        // var g = e.Graphics;

        g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.None;

        var panelW = panel.ClientSize.Width - 20;
        var panelH = panel.ClientSize.Height - 20;

        // Вычисляем квадратный размер клетки
        var cellW = (float) panelW / _gridWidth;
        var cellH = (float) panelH / _gridHeight;


        _cellSize = Math.Min(cellW, cellH); // делаем квадрат
        _margin = (int) (_cellSize * 0.1f);

        // Итоговое поле (с размерами кратными cellSize)
        var fieldW = _cellSize * _gridWidth;
        var fieldH = _cellSize * _gridHeight;

        // Центрирование поля
        _offsetX = 10 + (panelW - fieldW) / 2f;
        _offsetY = 10 + (panelH - fieldH) / 2f;

        using var pen = new Pen(Color.FromArgb(150, 100, 200, 100), 1);

        // Вертикальные линии
        for (var x = 0; x <= _gridWidth; x++)
        {
            var px = _offsetX + x * _cellSize;
            g.DrawLine(pen, px, _offsetY, px, _offsetY + fieldH);
        }

        // Горизонтальные линии
        for (var y = 0; y <= _gridHeight; y++)
        {
            var py = _offsetY + y * _cellSize;
            g.DrawLine(pen, _offsetX, py, _offsetX + fieldW, py);
        }
    }
    
    private void ClearCell(Graphics g, int x, int y)
    {
        float cx = _offsetX + x * _cellSize;
        float cy = _offsetY + y * _cellSize;

        var rect = new RectangleF(cx, cy, _cellSize, _cellSize);

        using (var brush = new SolidBrush(BackColor))
            g.FillRectangle(brush, rect);

        using (var pen = new Pen(Color.FromArgb(150, 100, 200, 100), 1))
        {
            g.DrawRectangle(pen, cx, cy, _cellSize, _cellSize);
        }
    }


    public void SetGridSize(int w, int h)
    {
        _gridWidth = w;
        _gridHeight = h;
        Invalidate();
    }
}