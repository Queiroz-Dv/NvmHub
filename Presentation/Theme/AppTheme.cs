namespace NvmManager.Presentation.Theme;

/// <summary>
/// Design system centralizado para garantir consistência visual em toda a aplicação.
/// </summary>
public static class AppTheme
{
    // ── Palette ───────────────────────────────────────────────────────────────
    public static readonly Color Background      = Color.FromArgb(10, 14, 26);
    public static readonly Color SidebarBg       = Color.FromArgb(15, 20, 36);
    public static readonly Color CardBg          = Color.FromArgb(22, 30, 50);
    public static readonly Color CardBorder      = Color.FromArgb(35, 46, 75);

    public static readonly Color AccentPrimary   = Color.FromArgb(99, 102, 241);   // indigo-500
    public static readonly Color AccentSecondary = Color.FromArgb(139, 92, 246);   // violet-500
    public static readonly Color AccentSuccess   = Color.FromArgb(34, 197, 94);    // green-500
    public static readonly Color AccentWarning   = Color.FromArgb(234, 179, 8);    // yellow-500
    public static readonly Color AccentDanger    = Color.FromArgb(239, 68, 68);    // red-500

    public static readonly Color TextPrimary     = Color.FromArgb(248, 250, 252);
    public static readonly Color TextSecondary   = Color.FromArgb(148, 163, 184);
    public static readonly Color TextMuted       = Color.FromArgb(71, 85, 105);

    public static readonly Color SidebarActive   = Color.FromArgb(99, 102, 241);
    public static readonly Color SidebarHover    = Color.FromArgb(30, 38, 65);

    // ── Typography ────────────────────────────────────────────────────────────
    public static readonly Font FontTitle     = new("Segoe UI", 20f, FontStyle.Bold);
    public static readonly Font FontSubtitle  = new("Segoe UI", 13f, FontStyle.Bold);
    public static readonly Font FontBody      = new("Segoe UI", 10f);
    public static readonly Font FontSmall     = new("Segoe UI", 9f);
    public static readonly Font FontButton    = new("Segoe UI", 10f, FontStyle.Bold);
    public static readonly Font FontSidebar   = new("Segoe UI", 10f, FontStyle.Bold);
    public static readonly Font FontMono      = new("Consolas", 10f);
    public static readonly Font FontMonoSmall = new("Consolas", 9f);

    // ── Sizes ─────────────────────────────────────────────────────────────────
    public const int SidebarWidth     = 220;
    public const int HeaderHeight     = 60;
    public const int ButtonHeight     = 44;
    public const int InputHeight      = 40;
    public const int CardPadding      = 24;
    public const int SectionGap       = 20;
    public const int CornerRadius     = 10;

    // ── Helpers ───────────────────────────────────────────────────────────────

    /// <summary>Pinta um retângulo com cantos arredondados.</summary>
    public static void FillRoundRect(Graphics g, Brush brush, Rectangle rect, int radius)
    {
        using var path = GetRoundedPath(rect, radius);
        g.FillPath(brush, path);
    }

    public static void DrawRoundRect(Graphics g, Pen pen, Rectangle rect, int radius)
    {
        using var path = GetRoundedPath(rect, radius);
        g.DrawPath(pen, path);
    }

    private static System.Drawing.Drawing2D.GraphicsPath GetRoundedPath(Rectangle rect, int radius)
    {
        var path = new System.Drawing.Drawing2D.GraphicsPath();
        var d    = radius * 2;
        path.AddArc(rect.X, rect.Y, d, d, 180, 90);
        path.AddArc(rect.Right - d, rect.Y, d, d, 270, 90);
        path.AddArc(rect.Right - d, rect.Bottom - d, d, d, 0, 90);
        path.AddArc(rect.X, rect.Bottom - d, d, d, 90, 90);
        path.CloseFigure();
        return path;
    }

    // ── Custom Controls ───────────────────────────────────────────────────────

    /// <summary>Aplica estilo escuro padrão a um TextBox.</summary>
    public static void StyleInput(TextBox textBox)
    {
        textBox.BackColor   = Color.FromArgb(15, 20, 36);
        textBox.ForeColor   = TextPrimary;
        textBox.BorderStyle = BorderStyle.FixedSingle;
        textBox.Font        = FontBody;
        textBox.Height      = InputHeight;
    }

    /// <summary>Aplica estilo escuro padrão a um RichTextBox (console).</summary>
    public static void StyleConsole(RichTextBox rtb)
    {
        rtb.BackColor   = Color.FromArgb(8, 12, 22);
        rtb.ForeColor   = AccentSuccess;
        rtb.BorderStyle = BorderStyle.None;
        rtb.Font        = FontMonoSmall;
        rtb.ReadOnly    = true;
    }
}
