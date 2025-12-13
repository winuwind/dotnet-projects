using System;
using System.Collections.Concurrent;
using System.Drawing;
using System.Net;
using System.Windows.Forms;
using Snake.Config;
using Snake.Control;
using Snake.Game.Field;
using SnakeGame.GUI.MyComponents;
using Snakes;

namespace SnakeGame.GUI;


public partial class MainForm : Form
{
    private readonly List<Color> _colors = [Color.BlueViolet, Color.Aqua, Color.DarkBlue, Color.LawnGreen, Color.Yellow, Color.Blue, Color.Brown, Color.DarkGreen, Color.DarkRed];
    
    private Controller _controller;
    
    private Panel _mainPanel = new Panel();
    private Panel _panelGameList = new Panel();
    private FieldPanel _panelField;
    private Panel _panelInfoGame = new Panel();
    private Panel _panelLeaderBoard = new Panel();
    private Panel _panelGame = new Panel();
    private Panel _panelScrollArea = new Panel();
    private Panel _panelBottom = new Panel();
    private Panel _panelButtonExitGame = new Panel();
    private TableLayoutPanel _tableLeaderBoard = new TableLayoutPanel();
    
    private ErrorForm _errorForm;

    private GameState _state = new GameState();
    private GameState.Types.Snake _snake =  new GameState.Types.Snake();
    
    public MainForm(Controller controller)
    {
        _controller = controller;
        _errorForm = new ErrorForm();
        InitializeComponent();
        SetupUI();
        WireControllerEvents();
        FormClosing += (sender, e) =>
        {
            _controller.Close();
            _errorForm.Close();
        };
        
        _errorForm.Show();
    }

    public void SetSnake(GameState.Types.Snake snake)
    {
        _snake = snake;
    }

    private void AddMenuButton(string text, int x, int y, EventHandler onClick)
    {
        var btn = new Button();
        btn.Text = text;
        btn.Font = new Font("Arial", 20, FontStyle.Bold);
        btn.ForeColor = Color.White;
        btn.BackColor = Color.FromArgb(50, 50, 50);
        btn.FlatStyle = FlatStyle.Flat;
        btn.FlatAppearance.BorderColor = Color.Black;
        btn.Size = new Size(400, 60);
        btn.Location = new Point(x, y);
        btn.Click += onClick;
        btn.Cursor = Cursors.Hand;

        _mainPanel.Controls.Add(btn);
    }

    private void SetupUI()
    {
        // Размер и стиль формы
        Text = "Game Menu";
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        StartPosition = FormStartPosition.Manual;
        Location = new Point(30, 30);

        _mainPanel.Dock = DockStyle.Fill;
        _panelGameList.Dock = DockStyle.Fill;
        _panelGame.Dock = DockStyle.Fill;
        // Фон
        // BackgroundImage = Image.FromFile("background.png"); // положи фон рядом с exe
        // BackgroundImageLayout = ImageLayout.Stretch;

        // Заголовок
        var title = new Label();
        title.Text = "Slither.io for poors";
        title.ForeColor = Color.BlueViolet;
        title.BackColor = Color.Transparent;
        title.Font = new Font("Arial", 36, FontStyle.Bold);
        title.AutoSize = true;
        title.Location = new Point(420, 150);

        _mainPanel.Controls.Add(title);

        AddMenuButton("Ввести имя", 550, 250, OnEnterName);
        AddMenuButton("Создать игру", 550, 320, OnCreateGame);
        AddMenuButton("Присоединиться к игре", 550, 390, OnJoinGame);
        AddMenuButton("Выход", 550, 460, OnExit);
        
        SetupGamePanel();
        SetupGameListPanel();
        
        Controls.Add(_panelGameList);
        Controls.Add(_mainPanel);


        _mainPanel.Show();
        _panelGameList.Hide();
        _panelGame.Hide();
    }

    private void SetupGamePanel()
    {
        _panelGame = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = Color.Black
        };
        
        Controls.Add(_panelGame);

        _panelField = new FieldPanel(this, _controller, _panelGame.Width);

        _panelInfoGame = new Panel
        {
            Dock = DockStyle.Right,
            Width = 400,
            BackColor = Color.FromArgb(230, 230, 230)
        };
        
        _panelLeaderBoard = new Panel
        {
            Dock = DockStyle.Fill,
            // Width = 400,
            BackColor = Color.FromArgb(230, 230, 230)
        };

        _panelButtonExitGame = new Panel
        {
            Dock = DockStyle.Bottom,
            // Width = 400,
            BackColor = Color.FromArgb(230, 230, 230)
        };

        // Таблица игроков
        _tableLeaderBoard = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            ColumnCount = 4,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            BackColor = Color.LightBlue
        };

        _tableLeaderBoard.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 110)); 
        _tableLeaderBoard.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 90));
        _tableLeaderBoard.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 110));
        _tableLeaderBoard.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 90));

        _tableLeaderBoard.Controls.Add(CreateHeader("Имя"), 0, 0);
        _tableLeaderBoard.Controls.Add(CreateHeader("Id"), 1, 0);
        _tableLeaderBoard.Controls.Add(CreateHeader("Роль"), 2, 0);
        _tableLeaderBoard.Controls.Add(CreateHeader("Счёт"), 3, 0);

        var btn = new Button();
        btn.Text = "Выход";
        btn.Font = new Font("Arial", 20, FontStyle.Bold);
        btn.ForeColor = Color.White;
        btn.BackColor = Color.FromArgb(50, 50, 50);
        btn.FlatStyle = FlatStyle.Flat;
        btn.FlatAppearance.BorderColor = Color.Black;
        btn.Size = new Size(200, 60);
        // btn.Location = new Point(x, y);
        btn.Dock = DockStyle.Fill;
        btn.Click += ExitGame;
        btn.Cursor = Cursors.Hand;

        _panelButtonExitGame.Controls.Add(btn);
        
        _panelLeaderBoard.Controls.Add(_tableLeaderBoard);
        _panelInfoGame.Controls.Add(_panelLeaderBoard);
        _panelInfoGame.Controls.Add(_panelButtonExitGame);

        _panelGame.Controls.Add(_panelInfoGame);
        _panelGame.Controls.Add(_panelField);

        _panelGame.TabStop = true;
        _panelGame.KeyDown += GameKeyDown;
    }

    private void ExitGame(object? sender, EventArgs e)
    {
        _controller.ExitGame();
        _panelGame.Hide();
        _panelField.Reset();
        _mainPanel.Show();
    }

    private void GameKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.KeyCode is Keys.Left or Keys.A)
        {
            if (_snake.Points[1].X < 0)
            {
                return;
            }
            _snake.HeadDirection = Direction.Left;
            _controller.ChangeDirection(Direction.Left);
        }
        if (e.KeyCode is Keys.Right or Keys.D)
        {
            if (_snake.Points[1].X > 0)
            {
                return;
            }
            _snake.HeadDirection = Direction.Right;
            _controller.ChangeDirection(Direction.Right);
        }
        if (e.KeyCode is Keys.Up or Keys.W)
        {
            if (_snake.Points[1].Y < 0)
            {
                return;
            }
            _snake.HeadDirection = Direction.Up;
            _controller.ChangeDirection(Direction.Up);
        }
        if (e.KeyCode is Keys.Down or Keys.S)
        {
            if (_snake.Points[1].Y > 0)
            {
                return;
            }
            _snake.HeadDirection = Direction.Down;
            _controller.ChangeDirection(Direction.Down);
        }
        // _panelField.Invalidate();
        _panelField.PaintField();
    }
    
    private Label CreateHeader(string text)
    {
        return new Label
        {
            Text = text,
            ForeColor = Color.White,
            Font = new Font("Arial", 12, FontStyle.Bold),
            AutoSize = true,
            Padding = new Padding(0, 0, 0, 5)
        };
    }

    private void UpdateGameField(GameState state)
    {
        _state = new GameState(state);
        _panelField.SetGameState(_state);
        _panelField.PaintField();
        // _panelField.Invalidate();
       UpdatePlayerTable(state.Players);
    }
    
    private void UpdatePlayerTable(GamePlayers players)
    {
        if (InvokeRequired)
        {
            Invoke(new Action(() => UpdatePlayerTable(players)));
            return;
        }
        
        var playersScore = new List<(string name, int id, NodeRole role, int score)>();
        foreach (var player in players.Players)
        {
            if (player.Role != NodeRole.Viewer)
            {
                playersScore.Add((player.Name, player.Id, player.Role, player.Score));
            }
        }
        
        var sorted = playersScore.OrderByDescending(p => p.score).ToList();

        _tableLeaderBoard.RowCount = 1;
        
        _tableLeaderBoard.Controls.Clear();
        _tableLeaderBoard.RowStyles.Clear();
        
        _tableLeaderBoard.Controls.Add(CreateHeader("Имя"), 0, 0);
        _tableLeaderBoard.Controls.Add(CreateHeader("Id"), 1, 0);
        _tableLeaderBoard.Controls.Add(CreateHeader("Роль"), 2, 0);
        _tableLeaderBoard.Controls.Add(CreateHeader("Счёт"), 3, 0);

        var row = 1;
        foreach (var p in sorted)
        {
            _tableLeaderBoard.RowCount++;

            _tableLeaderBoard.Controls.Add(CreatePlayerLabel(p.name, _colors[p.id]), 0, row);
            _tableLeaderBoard.Controls.Add(CreatePlayerLabel(p.id.ToString(), _colors[p.id]), 1, row);
            _tableLeaderBoard.Controls.Add(CreatePlayerLabel(p.role.ToString(), _colors[p.id]), 2, row);
            _tableLeaderBoard.Controls.Add(CreatePlayerLabel(p.score.ToString(), _colors[p.id]), 3, row);

            row++;
        }
        
        _panelLeaderBoard.Invalidate();
    }

    private Label CreatePlayerLabel(string text, Color? color = null)
    {
        return new Label
        {
            Text = text,
            ForeColor = color ?? Color.White,
            Font = new Font("Arial", 11, FontStyle.Regular),
            AutoSize = true
        };
    }

    private void SetupGameListPanel()
    {
        _panelGameList.Dock = DockStyle.Fill;
        _panelGameList.BackColor = Color.FromArgb(230, 230, 230);

        _panelScrollArea = new Panel
        {
            Dock = DockStyle.Fill,
            AutoScroll = true,
            BackColor = Color.Wheat
        };

        _panelBottom = new Panel
        {
            Dock = DockStyle.Bottom,
            Height = 70,
            BackColor = Color.FromArgb(230, 230, 230)
        };

        var backBtn = new Button
        {
            Text = "Назад",
            Width = 200,
            Height = 40,
            Font = new Font("Arial", 12, FontStyle.Bold),
            BackColor = Color.FromArgb(50, 50, 50),
            ForeColor = Color.Wheat,
            FlatStyle = FlatStyle.Flat,
            Location = new Point(10, 15)
        };

        backBtn.Click += (s, e) =>
        {
            _panelGameList.Hide();
            _mainPanel.Show();
        };

        _panelBottom.Controls.Add(backBtn);

        _panelGameList.Controls.Add(_panelScrollArea);
        _panelGameList.Controls.Add(_panelBottom);
    }

    private void WireControllerEvents()
    {
        _controller.OnError += msg => _errorForm.AppendText(msg);
        _controller.OnGameOver += ShowGameOver;
        _controller.OnGamesList += FillGameList;
        _controller.Game += UpdateGameField;
    }

    private void FillGameList(ConcurrentDictionary<(IPAddress ip, int port), (long seq, List<GameAnnouncement> games, DateTime lastTime)> games)
    {
        _panelScrollArea.Controls.Clear();

        var y = 10;

        foreach (var pair in games)
        {
            var key = pair.Key;
            var game = pair.Value;

            var btn = new BorderButton
            {
                Text = game.games.First().GameName,
                Tag = key,
                Width = 260,
                Height = 40,
                Location = new Point(10, y),
                BorderThickness = 2,
                Font = new Font("Arial", 14, FontStyle.Bold),
            };

            btn.Click += (s, e) =>
            {
                var newKey = ((IPAddress ip, int port)) btn.Tag;
                JoinGameButtonClick(newKey.ip, newKey.port);
            };

            _panelScrollArea.Controls.Add(btn);
            y += 50;
        }
        _panelGameList.Invalidate();
    }
    
    private void OnEnterName(object? sender, EventArgs e)
    {
        var dialog = new ChooseValueForm("Введите имя игрока");

        if (dialog.ShowDialog() == DialogResult.OK)
        {
            var name = dialog.EnteredText;
            _controller.SetPlayerName(name);
        }
    }

    private void OnCreateGame(object? sender, EventArgs e)
    {
        var name = "New Game";
        var dialog = new ChooseValueForm("Введите название игры");

        if (dialog.ShowDialog() == DialogResult.OK)
        {
            name = dialog.EnteredText;
        }
        else
        {
            return;
        }
        
        while (true)
        {
            dialog = new ChooseValueForm("Введите ширину поля игры\n(целое число)");

            if (dialog.ShowDialog() == DialogResult.OK)
            {
                var text = dialog.EnteredText;

                if (int.TryParse(text, out var number))
                {
                    AppConstant.Size.X = number;
                    break;
                }
            }
            else
            {
                return;
            }
        }
        
        while (true)
        {
            dialog = new ChooseValueForm("Введите высоту поля игры\n(целое число)");
            
            if (dialog.ShowDialog() == DialogResult.OK)
            {
                var text = dialog.EnteredText;

                if (int.TryParse(text, out var number))
                {
                    AppConstant.Size.Y = number;
                    break;
                }
            }
            else
            {
                return;
            }
        }
        
        while (true)
        {
            dialog = new ChooseValueForm("Введите FoodStatic\n(целое число)");

            if (dialog.ShowDialog() == DialogResult.OK)
            {
                var text = dialog.EnteredText;
                if (int.TryParse(text, out var number))
                {
                    AppConstant.StaticFood = number;
                    break;
                }
            }
            else
            {
                return;
            }
        }

        while (true)
        {
            dialog = new ChooseValueForm("Введите задержку\n(целое число в мс)");

            if (dialog.ShowDialog() == DialogResult.OK)
            {
                var text = dialog.EnteredText;

                if (int.TryParse(text, out var number))
                {
                    AppConstant.StateDelayMs = number;
                    break;
                }
            }
            else
            {
                return;
            }
        }
        
        _panelField.SetGridSize(AppConstant.Size.X, AppConstant.Size.Y);
        
        new Thread(() => _controller.StartNewGame(name)).Start();
        
        _mainPanel.Hide();
        _panelGame.Show();
        _panelGame.Focus();
    }

    private void OnJoinGame(object? sender, EventArgs e)
    {
        _controller.RequestGamesList();
        _mainPanel.Hide();
        _panelGameList.Show();
    }

    private void OnExit(object? sender, EventArgs e)
    {
        _controller.Close();
        _errorForm.Close();
        Close();
    }

    private void JoinGameButtonClick(IPAddress ip, int port)
    {
        var choose = new ChooseRoleForm();

        if (choose.ShowDialog() == DialogResult.OK)
        {
            var thread = new Thread(() => _controller.JoinToGame(ip, port, choose.SelectedType));
            thread.Start();
            thread.Join();
            _panelField.SetGridSize(AppConstant.Size.X, AppConstant.Size.Y);
            _panelGameList.Hide();
            _panelGame.Show();
            _panelGame.Focus();
        }
    }

    private void ShowGameOver()
    {
        var gameOver = new GameOverForm();
        
        if (gameOver.ShowDialog() == DialogResult.OK)
        {
            if (gameOver.Exit)
            {
                ExitGame(null, EventArgs.Empty);
            }
        }
    }
}
