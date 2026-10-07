using System.Collections.Concurrent;
using System.Net;
using Snake.Config;
using Snake.Control;
using Snake.GUI.MyComponents;
using Snakes;

namespace Snake.GUI;


public partial class MainForm : Form
{
    private readonly List<Color> _colors =
    [
        Color.FromArgb(0x1F, 0x3A, 0x5F),
        Color.FromArgb(0x00, 0x6D, 0x6F),
        Color.FromArgb(0x2E, 0x7D, 0x32),
        Color.FromArgb(0xC6, 0x28, 0x28),
        Color.FromArgb(0x6A, 0x1B, 0x9A),
        Color.FromArgb(0xEF, 0x6C, 0x00),
        Color.FromArgb(0x45, 0x27, 0xA0),
        Color.FromArgb(0x00, 0x4D, 0x40),
        Color.FromArgb(0x5D, 0x40, 0x37),
    ];
    private readonly Dictionary<int, PlayerRow> _rowsById = new();
    private readonly System.Windows.Forms.Timer _timer = new System.Windows.Forms.Timer(); 
    
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
    private Panel _panelConfigGame = new Panel();
    private TableLayoutPanel _tableLeaderBoard = new TableLayoutPanel();
    private Label _labelGameInfo = new Label();
    private ErrorForm _errorForm;
    private GameState _state = new GameState();
    private GameState.Types.Snake _snake =  new GameState.Types.Snake();
    
    private sealed class PlayerRow
    {
        public int PlayerId;
        public Label Name;
        public Label Id;
        public Label Role;
        public Label Score;
    }

    private sealed class PlayerInfo
    {
        public string Name;
        public int Id;
        public int Score;
        public NodeRole Role;
    }
    
    public MainForm(Controller controller)
    {
        _controller = controller;
        _errorForm = new ErrorForm();
        InitializeComponent();
        SetupUI();
        WireControllerEvents();
        FormClosing += (sender, e) =>
        {
            _rowsById.Clear();
            ClearTableExceptHeader(_tableLeaderBoard);
            _controller.ExitGame();
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

        _panelConfigGame = new Panel
        {
            Dock = DockStyle.Top,
            AutoSize = true,
            BackColor = Color.FromArgb(230, 230, 230)
        };
        _labelGameInfo = new Label
        {
            Dock = DockStyle.Fill,
            AutoSize = true,
            TextAlign = ContentAlignment.TopLeft,
            Font = new Font("Comic Sans", 11, FontStyle.Bold),
            ForeColor = Color.FromArgb(40, 40, 40),
        };
        _panelConfigGame.Controls.Add(_labelGameInfo);
        
        _panelLeaderBoard = new Panel
        {
            Dock = DockStyle.Fill,
            AutoScroll = true,
            BackColor = Color.BlanchedAlmond
        };

        _panelButtonExitGame = new Panel
        {
            Dock = DockStyle.Bottom,
            BackColor = Color.FromArgb(230, 230, 230)
        };

        _tableLeaderBoard = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            ColumnCount = 4,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            BackColor = Color.BlanchedAlmond
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
        btn.Dock = DockStyle.Fill;
        btn.Click += ExitGame;
        btn.Cursor = Cursors.Hand;

        _panelButtonExitGame.Controls.Add(btn);
        
        _panelLeaderBoard.Controls.Add(_tableLeaderBoard);
        
        _panelInfoGame.Controls.Add(_panelLeaderBoard);
        _panelInfoGame.Controls.Add(_panelConfigGame);
        _panelInfoGame.Controls.Add(_panelButtonExitGame);

        _panelGame.Controls.Add(_panelInfoGame);
        _panelGame.Controls.Add(_panelField);

        _panelGame.TabStop = true;
        _panelGame.KeyDown += GameKeyDown;
    }

    private void ExitGame(object? sender, EventArgs e)
    {
        _rowsById.Clear();
        ClearTableExceptHeader(_tableLeaderBoard);
        
        _controller.ExitGame();
        _panelGame.Hide();
        _panelField.Reset();
        _mainPanel.Show();
    }

    private void GameKeyDown(object? sender, KeyEventArgs e)
    {
        if (_controller.GetRole() == NodeRole.Viewer)
        {
            return;
        }
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
        _panelField.PaintField();
    }
    
    private Label CreateHeader(string text)
    {
        return new Label
        {
            Text = text,
            ForeColor = Color.Black,
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
       UpdatePlayerTable(state.Players);
    }
    
    private PlayerRow CreateRow(PlayerInfo p)
    {
        var color = _colors[(p.Id + _colors.Count) % _colors.Count];

        return new PlayerRow
        {
            PlayerId = p.Id,
            Name  = CreatePlayerLabel(p.Name, color),
            Id    = CreatePlayerLabel(p.Id.ToString(), color),
            Role  = CreatePlayerLabel(p.Role.ToString(), color),
            Score = CreatePlayerLabel(p.Score.ToString(), color),
        };
    }

    private void SetRow(PlayerRow row, int rowIndex)
    {
        _tableLeaderBoard.SetRow(row.Name,  rowIndex);
        _tableLeaderBoard.SetRow(row.Id,    rowIndex);
        _tableLeaderBoard.SetRow(row.Role,  rowIndex);
        _tableLeaderBoard.SetRow(row.Score, rowIndex);

        if (!_tableLeaderBoard.Controls.Contains(row.Name))
        {
            _tableLeaderBoard.Controls.Add(row.Name,  0, rowIndex);
            _tableLeaderBoard.Controls.Add(row.Id,    1, rowIndex);
            _tableLeaderBoard.Controls.Add(row.Role,  2, rowIndex);
            _tableLeaderBoard.Controls.Add(row.Score, 3, rowIndex);
        }
    }
    
    private void UpdatePlayerTable(GamePlayers players)
    {
        if (InvokeRequired)
        {
            Invoke(() => UpdatePlayerTable(players));
            return;
        }

        var list = players.Players
            .Where(p => p.Role != NodeRole.Viewer)
            .Select(p => new
            {
                p.Name,
                p.Id,
                p.Role,
                p.Score
            })
            .OrderByDescending(p => p.Score)
            .ToList();
        
        var actualIds = list.Select(p => p.Id).ToHashSet();

        var removedIds = _rowsById.Keys
            .Where(id => !actualIds.Contains(id))
            .ToList();

        _tableLeaderBoard.SuspendLayout();
        
        foreach (var id in removedIds)
        {
            var row = _rowsById[id];

            _tableLeaderBoard.Controls.Remove(row.Name);
            _tableLeaderBoard.Controls.Remove(row.Id);
            _tableLeaderBoard.Controls.Remove(row.Role);
            _tableLeaderBoard.Controls.Remove(row.Score);

            _rowsById.Remove(id);
        }


        foreach (var p in list)
        {
            if (!_rowsById.TryGetValue(p.Id, out var row))
            {
                row = CreateRow(new PlayerInfo{Name = p.Name, Id = p.Id, Role = p.Role, Score = p.Score});
                _rowsById[p.Id] = row;
            }

            row.Role.Text  = p.Role.ToString();
            row.Score.Text = p.Score.ToString();
        }

        for (int i = 0; i < list.Count; i++)
        {
            var row = _rowsById[list[i].Id];
            int targetRow = i + 1;

            SetRow(row, targetRow);
        }

        _tableLeaderBoard.ResumeLayout();
    }
    
    private void ClearTableExceptHeader(TableLayoutPanel table)
    {
        for (int i = table.Controls.Count - 1; i >= 0; i--)
        {
            var c = table.Controls[i];
            if (table.GetRow(c) > 0)
            {
                table.Controls.RemoveAt(i);
                c.Dispose();
            }
        }

        table.RowCount = 1;

        while (table.RowStyles.Count > 1)
            table.RowStyles.RemoveAt(1);
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

        var flow = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            AutoScroll = true,
            FlowDirection = FlowDirection.TopDown,
            WrapContents = false,
            Padding = new Padding(0, 10, 0, 10),
            BackColor = Color.FloralWhite
        };

        _panelScrollArea = flow;

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

        _panelGameList.Controls.Add(flow);
        _panelGameList.Controls.Add(_panelBottom);
    }

    private void WireControllerEvents()
    {
        _controller.OnError += msg => _errorForm.AppendText(msg);
        _controller.OnGameOver += ShowGameOver;
        _controller.OnGamesList += FillGameList;
        _controller.Game += UpdateGameField;
    }

    private void FillGameList(
        ConcurrentDictionary<(IPAddress ip, int port),
            (long seq, List<GameAnnouncement> games, DateTime lastTime)> games)
    {
        if (InvokeRequired)
        {
            Invoke(new Action(() => FillGameList(games)));
            return;
        }

        _panelScrollArea.Controls.Clear();

        var panelWidth = _panelScrollArea.ClientSize.Width;
        var buttonWidth = (int)(panelWidth * 0.4);

        foreach (var pair in games)
        {
            var key = pair.Key;
            var game = pair.Value.games.First();

            try
            {
                var text =
                    $"""
                     Название: {game.GameName}
                     Ведущий: {game.Players.Players[0].Name} ({key.ip}:{key.port})
                     Играют: {game.Players.Players.Count(p => p.Role != NodeRole.Viewer)}
                     Размер: {game.Config.Width}x{game.Config.Height}
                     Еда: {game.Config.FoodStatic}+x
                     Время перехода: {game.Config.StateDelayMs}
                     """;

                var btn = new BorderButton
                {
                    Text = text,
                    Tag = key,
                    Width = buttonWidth,
                    Height = 170,
                    BorderThickness = 2,
                    Font = new Font("Comic Sans", 10, FontStyle.Regular),
                    TextAlign = ContentAlignment.MiddleCenter,
                    Margin = new Padding(
                        (panelWidth - buttonWidth) / 2,
                        6,
                        0,
                        6)
                };

                btn.Click += (s, e) =>
                {
                    var k = ((IPAddress ip, int port))btn.Tag;
                    _labelGameInfo.Text = $"""
                                           Имя игрока: {_controller.GetName()}
                                           Id: {_controller.GetId()}
                                           Название: {game.GameName}
                                           Размер: {game.Config.Width}x{game.Config.Height}
                                           Еда: {game.Config.FoodStatic}+x
                                           Время перехода: {game.Config.StateDelayMs}

                                           """;
                    JoinGameButtonClick(k.ip, k.port);
                };

                _panelScrollArea.Controls.Add(btn);
            }
            catch
            {
                continue;
            }
        }
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

                if (int.TryParse(text, out var number) && number > 0)
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

                if (int.TryParse(text, out var number) && number > 0)
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
                if (int.TryParse(text, out var number) && number > 0)
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

                if (int.TryParse(text, out var number) && number > 0)
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
        
        _labelGameInfo.Text = $"""
                                Имя игрока: {_controller.GetName()}
                                Id: {_controller.GetId()}
                                Название: {name}
                                Размер: {AppConstant.Size.X}x{AppConstant.Size.Y}
                                Еда: {AppConstant.StaticFood}+x
                                Время перехода: {AppConstant.StateDelayMs}ms
                                
                                """;
        _panelConfigGame.Invalidate();
        
        new Thread(() => _controller.StartNewGame(name)).Start();
        
        _mainPanel.Hide();
        _panelGame.Show();
        _panelGame.Focus();
    }

    private void OnJoinGame(object? sender, EventArgs e)
    {
        _timer.Interval = 1000;
        _timer.Tick += (s, ee) =>
        {
            _controller.RequestGamesList();
        };
        _timer.Start();
        
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
            
            _timer.Stop();
            
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