namespace Snake.GUI;

public partial class GameOverForm : Form
{
    public bool Exit { get; private set; } = true;
    
    public GameOverForm()
    {
        InitializeComponent();
        SetupUI();
    }
    
    private void SetupUI()
    {
        Text = "Конец игры";
        StartPosition = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        ClientSize = new Size(400, 150);

        var lbl = new Label
        {
            Text = "Продолжить?",
            Font = new Font("Arial", 12, FontStyle.Bold),
            AutoSize = true,
            Location = new Point(100, 20)
        };
        Controls.Add(lbl);

        var btnSpectate = new Button
        {
            Text = "Наблюдать",
            Width = 150,
            Height = 40,
            Location = new Point(50, 70),
            Font = new Font("Arial", 10, FontStyle.Bold)
        };

        var btnExit = new Button
        {
            Text = "Выйти",
            Width = 150,
            Height = 40,
            Location = new Point(200, 70),
            Font = new Font("Arial", 10, FontStyle.Bold)
        };

        btnExit.Click += (s, e) =>
        {
            Exit = true;
            DialogResult = DialogResult.OK;
            Close();
        };

        btnSpectate.Click += (s, e) =>
        {
            Exit = false;
            DialogResult = DialogResult.OK;
            Close();
        };

        Controls.Add(btnExit);
        Controls.Add(btnSpectate);
    }
}