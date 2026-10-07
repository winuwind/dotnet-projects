namespace Snake.GUI;

public partial class ErrorForm : Form
{
    private Panel _outerPanel;
    private FlowLayoutPanel _flowPanel;
    
    public ErrorForm()
    {
        InitializeComponent();
        SetUp();
    }
    
    private void SetUp()
    {
        Text = "Ошибки";
        StartPosition = FormStartPosition.CenterScreen;

        _outerPanel = new Panel
        {
            Dock = DockStyle.Fill,
            AutoScroll = true,
            BackColor = Color.Bisque,
            Padding = new Padding(6)
        };

        _flowPanel = new FlowLayoutPanel
        {
            FlowDirection = FlowDirection.TopDown,
            WrapContents = false,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowOnly,
            BackColor = Color.Transparent
        };

        _outerPanel.Controls.Add(_flowPanel);
        Controls.Add(_outerPanel);

        Font = new Font("Consolas", 10);
    }

    public void AppendText(string text, Color? foreColor = null)
    {
        if (IsDisposed) return;

        if (InvokeRequired)
        {
            Invoke(new Action(() => AppendText(text, foreColor)));
            return;
        }

        var lbl = new Label
        {
            AutoSize = true,
            MaximumSize = new Size(_outerPanel.ClientSize.Width - _outerPanel.Padding.Horizontal - 20, 0),
            Text = text,
            ForeColor = foreColor ?? Color.DarkRed,
            Font = new Font(Font.FontFamily, 10, FontStyle.Regular),
            BackColor = Color.Transparent,
            Margin = new Padding(0, 0, 0, 4)
        };

        _flowPanel.Controls.Add(lbl);

        _outerPanel.ScrollControlIntoView(lbl);
    }

    public void ClearLog()
    {
        if (InvokeRequired)
        {
            Invoke(new Action(ClearLog));
            return;
        }

        _flowPanel.Controls.Clear();
    }
}