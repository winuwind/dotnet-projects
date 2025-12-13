using Snakes;

namespace SnakeGame.GUI;

public partial class ChooseRoleForm : Form
{
    public NodeRole SelectedType { get; private set; } = NodeRole.Normal;
    
    public ChooseRoleForm()
    {
        InitializeComponent();
        SetupUI();
    }

    private void SetupUI()
    {
        Text = "Выберите тип игры";
        StartPosition = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        ClientSize = new Size(400, 150);

        var lbl = new Label
        {
            Text = "Выберите тип игры:",
            Font = new Font("Arial", 12, FontStyle.Bold),
            AutoSize = true,
            Location = new Point(60, 20)
        };
        Controls.Add(lbl);

        var btnPlay = new Button
        {
            Text = "Играть",
            Width = 150,
            Height = 40,
            Location = new Point(50, 70),
            Font = new Font("Arial", 10, FontStyle.Bold)
        };

        var btnSpectate = new Button
        {
            Text = "Наблюдать",
            Width = 150,
            Height = 40,
            Location = new Point(200, 70),
            Font = new Font("Arial", 10, FontStyle.Bold)
        };

        btnPlay.Click += (s, e) =>
        {
            SelectedType = NodeRole.Normal;
            DialogResult = DialogResult.OK;
            Close();
        };

        btnSpectate.Click += (s, e) =>
        {
            SelectedType = NodeRole.Viewer;
            DialogResult = DialogResult.OK;
            Close();
        };

        Controls.Add(btnPlay);
        Controls.Add(btnSpectate);
    }
}