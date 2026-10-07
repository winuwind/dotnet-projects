using System.ComponentModel;

namespace Snake.GUI.MyComponents;

public sealed class BorderButton : Button
{
    [Browsable(true)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
    [Category("Appearance")]
    public Color BorderColor { get; set; } = Color.BlueViolet;

    [Browsable(true)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
    [Category("Appearance")]
    public int BorderThickness { get; set; } = 2;

    [Browsable(true)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
    [Category("Appearance")]
    public Color HoverBorderColor { get; set; } = Color.LawnGreen;

    [Browsable(true)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
    [Category("Appearance")]
    public Color HoverForeColor { get; set; } = Color.Aqua;

    private Color _originalBorderColor;
    private Color _originalForeColor;

    public BorderButton()
    {
        FlatStyle = FlatStyle.Flat;
        FlatAppearance.BorderSize = 0;
        BackColor = Color.Silver;
        ForeColor = Color.DarkViolet;

        SetStyle(ControlStyles.Opaque, true);

        _originalBorderColor = BorderColor;
        _originalForeColor = ForeColor;

        MouseEnter += OnHoverEnter;
        MouseLeave += OnHoverLeave;
    }

    private void OnHoverEnter(object? sender, System.EventArgs e)
    {
        _originalBorderColor = BorderColor;
        _originalForeColor = ForeColor;

        BorderColor = HoverBorderColor;
        ForeColor = HoverForeColor;

        Invalidate(); // перерисовать
    }

    private void OnHoverLeave(object? sender, System.EventArgs e)
    {
        BorderColor = _originalBorderColor;
        ForeColor = _originalForeColor;

        Invalidate();
    }

    protected override void OnPaint(PaintEventArgs pevent)
    {
        base.OnPaint(pevent);

        using var pen = new Pen(BorderColor, BorderThickness);
        var rect = ClientRectangle;
        rect.Width--;
        rect.Height--;
        pevent.Graphics.DrawRectangle(pen, rect);
    }

    protected override CreateParams CreateParams
    {
        get
        {
            var cp = base.CreateParams;
            cp.ExStyle |= 0x20;
            return cp;
        }
    }
}