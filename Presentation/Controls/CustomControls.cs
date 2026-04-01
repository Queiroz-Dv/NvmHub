using NvmManager.Presentation.Theme;

namespace NvmManager.Presentation.Controls;

// ─────────────────────────────────────────────────────────────────────────────
// StyledButton - botão com cantos arredondados e estado hover/press
// ─────────────────────────────────────────────────────────────────────────────
public sealed class StyledButton : Button
{
    private bool _isHovered;
    private bool _isPressed;

    public Color NormalColor  { get; set; } = AppTheme.AccentPrimary;
    public Color HoverColor   { get; set; } = Color.FromArgb(79, 82, 221);
    public Color PressedColor { get; set; } = Color.FromArgb(67, 56, 202);
    public Color TextColor    { get; set; } = AppTheme.TextPrimary;
    public int   Radius       { get; set; } = 8;

    public StyledButton()
    {
        FlatStyle = FlatStyle.Flat;
        FlatAppearance.BorderSize = 0;
        Cursor    = Cursors.Hand;
        Font      = AppTheme.FontButton;
        ForeColor = TextColor;
        Height    = AppTheme.ButtonHeight;
    }

    protected override void OnMouseEnter(EventArgs e) { _isHovered = true;  Invalidate(); base.OnMouseEnter(e); }
    protected override void OnMouseLeave(EventArgs e) { _isHovered = false; Invalidate(); base.OnMouseLeave(e); }
    protected override void OnMouseDown(MouseEventArgs e) { _isPressed = true;  Invalidate(); base.OnMouseDown(e); }
    protected override void OnMouseUp(MouseEventArgs e)   { _isPressed = false; Invalidate(); base.OnMouseUp(e); }

    protected override void OnPaint(PaintEventArgs e)
    {
        e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
        var color  = _isPressed ? PressedColor : _isHovered ? HoverColor : NormalColor;
        var rect   = new Rectangle(0, 0, Width - 1, Height - 1);

        using var brush = new SolidBrush(color);
        AppTheme.FillRoundRect(e.Graphics, brush, rect, Radius);

        var textSize = e.Graphics.MeasureString(Text, Font);
        using var textBrush = new SolidBrush(TextColor);
        e.Graphics.DrawString(
            Text, Font, textBrush,
            (Width  - textSize.Width)  / 2,
            (Height - textSize.Height) / 2);
    }
}

// ─────────────────────────────────────────────────────────────────────────────
// SidebarButton - botão da barra lateral com ícone e indicador ativo
// ─────────────────────────────────────────────────────────────────────────────
public sealed class SidebarButton : Control
{
    private bool _isActive;
    private bool _isHovered;

    public string Icon    { get; set; } = "●";
    public bool IsActive
    {
        get => _isActive;
        set { _isActive = value; Invalidate(); }
    }

    public event EventHandler? Activated;

    public SidebarButton()
    {
        Height = 50;
        Cursor = Cursors.Hand;
        Font   = AppTheme.FontSidebar;
        SetStyle(ControlStyles.OptimizedDoubleBuffer | ControlStyles.AllPaintingInWmPaint, true);
    }

    protected override void OnMouseEnter(EventArgs e) { _isHovered = true;  Invalidate(); base.OnMouseEnter(e); }
    protected override void OnMouseLeave(EventArgs e) { _isHovered = false; Invalidate(); base.OnMouseLeave(e); }
    protected override void OnClick(EventArgs e) { Activated?.Invoke(this, e); base.OnClick(e); }

    protected override void OnPaint(PaintEventArgs e)
    {
        e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
        var g = e.Graphics;

        // Background
        if (_isActive)
        {
            using var bg = new SolidBrush(AppTheme.SidebarHover);
            g.FillRectangle(bg, 0, 0, Width, Height);

            // Indicador lateral esquerdo
            using var indicator = new SolidBrush(AppTheme.AccentPrimary);
            g.FillRectangle(indicator, 0, 8, 4, Height - 16);
        }
        else if (_isHovered)
        {
            using var bg = new SolidBrush(AppTheme.SidebarHover);
            g.FillRectangle(bg, 0, 0, Width, Height);
        }

        // Ícone
        var iconColor = _isActive ? AppTheme.AccentPrimary : AppTheme.TextSecondary;
        using var iconBrush = new SolidBrush(iconColor);
        using var iconFont  = new Font("Segoe UI", 14f);
        g.DrawString(Icon, iconFont, iconBrush, 20, (Height - 22) / 2);

        // Texto
        var textColor = _isActive ? AppTheme.TextPrimary : AppTheme.TextSecondary;
        using var textBrush = new SolidBrush(textColor);
        g.DrawString(Text, Font, textBrush, 52, (Height - 16) / 2f);
    }
}

// ─────────────────────────────────────────────────────────────────────────────
// StatusBadge - tag colorida para status de versões
// ─────────────────────────────────────────────────────────────────────────────
public sealed class StatusBadge : Label
{
    public StatusBadge(string text, Color bgColor, Color fgColor)
    {
        Text        = text;
        BackColor   = bgColor;
        ForeColor   = fgColor;
        Font        = AppTheme.FontSmall;
        AutoSize    = true;
        Padding     = new Padding(6, 2, 6, 2);
        TextAlign   = ContentAlignment.MiddleCenter;
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
        using var bg = new SolidBrush(BackColor);
        AppTheme.FillRoundRect(e.Graphics, bg, new Rectangle(0, 0, Width - 1, Height - 1), 4);
        using var tb = new SolidBrush(ForeColor);
        e.Graphics.DrawString(Text, Font, tb, 6f, 2f);
    }

    protected override void OnPaintBackground(PaintEventArgs e) { /* suppress */ }
}

// ─────────────────────────────────────────────────────────────────────────────
// CardPanel - panel com visual de card escuro
// ─────────────────────────────────────────────────────────────────────────────
public sealed class CardPanel : Panel
{
    public CardPanel()
    {
        BackColor = AppTheme.CardBg;
        Padding   = new Padding(AppTheme.CardPadding);
        SetStyle(ControlStyles.OptimizedDoubleBuffer | ControlStyles.AllPaintingInWmPaint, true);
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
        using var pen = new Pen(AppTheme.CardBorder, 1);
        AppTheme.DrawRoundRect(e.Graphics, pen, new Rectangle(0, 0, Width - 1, Height - 1), AppTheme.CornerRadius);
    }
}

// ─────────────────────────────────────────────────────────────────────────────
// StyledTextBox - wrapper do TextBox com label e placeholder
// ─────────────────────────────────────────────────────────────────────────────
public sealed class LabeledInput : Panel
{
    public TextBox Input { get; }

    public LabeledInput(string labelText, string placeholder = "")
    {
        BackColor   = Color.Transparent;
        AutoSize    = true;
        MinimumSize = new Size(0, 70);

        var label = new Label
        {
            Text      = labelText,
            Font      = AppTheme.FontSmall,
            ForeColor = AppTheme.TextSecondary,
            Dock      = DockStyle.Top,
            Height    = 22,
        };

        Input = new TextBox
        {
            PlaceholderText = placeholder,
            Dock            = DockStyle.Top,
            Height          = AppTheme.InputHeight,
        };
        AppTheme.StyleInput(Input);

        Controls.Add(Input);
        Controls.Add(label);
    }
}
