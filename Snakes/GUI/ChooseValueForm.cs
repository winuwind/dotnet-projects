namespace Snake.GUI;

public partial class ChooseValueForm : Form
{
    public string EnteredText { get; private set; }
    
    private TextBox _inputBox;
    private Button _btnOk;
    private Button _btnCancel;
    
    public ChooseValueForm(string title)
    {
        InitializeComponent();
        SetUp(title);
    }

    private void SetUp(string title)
    {
        Text = title;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        StartPosition = FormStartPosition.CenterParent;
        ClientSize = new Size(400, 200);
        MaximizeBox = false;
        MinimizeBox = false;

        var lbl = new Label();
        lbl.Text = title;
        lbl.Font = new Font("Arial", 12, FontStyle.Bold);
        lbl.AutoSize = true;
        lbl.Location = new Point(20, 20);
        Controls.Add(lbl);

        _inputBox = new TextBox();
        _inputBox.Font = new Font("Arial", 12);
        _inputBox.Location = new Point(20, 85);
        _inputBox.Size = new Size(350, 30);
        Controls.Add(_inputBox);

        // Кнопка OK
        _btnOk = new Button();
        _btnOk.Text = "OK";
        _btnOk.Font = new Font("Arial", 10, FontStyle.Bold);
        _btnOk.Location = new Point(80, 135);
        _btnOk.Size = new Size(100, 35);
        _btnOk.Click += BtnOk_Click;
        Controls.Add(_btnOk);

        _btnCancel = new Button();
        _btnCancel.Text = "Отмена";
        _btnCancel.Font = new Font("Arial", 10, FontStyle.Bold);
        _btnCancel.Location = new Point(220, 135);
        _btnCancel.Size = new Size(100, 35);
        _btnCancel.Click += BtnCancel_Click;
        Controls.Add(_btnCancel);
    }
    
    private void BtnOk_Click(object? sender, EventArgs e)
    {
        EnteredText = _inputBox.Text;
        DialogResult = DialogResult.OK;
        Close();
    }

    private void BtnCancel_Click(object? sender, EventArgs e)
    {
        DialogResult = DialogResult.Cancel;
        Close();
    }
}