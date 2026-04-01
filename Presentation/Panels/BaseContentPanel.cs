using NvmManager.Application.Services;
using NvmManager.Presentation.Controls;
using NvmManager.Presentation.Theme;

namespace NvmManager.Presentation.Panels;

/// <summary>
/// Panel base com layout padrão: título, subtítulo, área de conteúdo e console de saída.
/// </summary>
public abstract class BaseContentPanel : Panel
{
    protected readonly NvmApplicationService AppService;
    protected RichTextBox Console { get; private set; } = null!;
    private CancellationTokenSource? _cts;

    protected BaseContentPanel(NvmApplicationService appService)
    {
        AppService = appService;
        Dock       = DockStyle.Fill;
        BackColor  = AppTheme.Background;
        Padding    = new Padding(32);
        SetStyle(ControlStyles.OptimizedDoubleBuffer | ControlStyles.AllPaintingInWmPaint, true);
        BuildLayout();
    }

    private void BuildLayout()
    {
        var scroll = new Panel { Dock = DockStyle.Fill, AutoScroll = true, BackColor = Color.Transparent };

        var container = new FlowLayoutPanel
        {
            Dock          = DockStyle.Top,
            FlowDirection = FlowDirection.TopDown,
            WrapContents  = false,
            AutoSize      = true,
            BackColor     = Color.Transparent,
        };
        container.ClientSizeChanged += (_, _) => container.Width = scroll.ClientSize.Width;

        // ─ Cabeçalho ─
        var headerPanel = new Panel { BackColor = Color.Transparent, Height = 80, Width = 800 };
        var titleLabel  = new Label
        {
            Text      = Title,
            Font      = AppTheme.FontTitle,
            ForeColor = AppTheme.TextPrimary,
            Location  = new Point(0, 0),
            AutoSize  = true,
        };
        var subtitleLabel = new Label
        {
            Text      = Subtitle,
            Font      = AppTheme.FontBody,
            ForeColor = AppTheme.TextSecondary,
            Location  = new Point(0, 40),
            AutoSize  = true,
        };
        headerPanel.Controls.AddRange([titleLabel, subtitleLabel]);
        container.Controls.Add(headerPanel);

        // Separador
        var sep = new Panel { Height = 1, Width = 800, BackColor = AppTheme.CardBorder, Margin = new Padding(0, 0, 0, 20) };
        container.Controls.Add(sep);

        // ─ Conteúdo específico do painel ─
        BuildContent(container);

        // ─ Console de output ─
        var consoleCard = new CardPanel { Width = 800, Height = 200, Margin = new Padding(0, 24, 0, 0) };
        var consoleTitle = new Label
        {
            Text      = "▶  Output",
            Font      = AppTheme.FontSmall,
            ForeColor = AppTheme.TextSecondary,
            Dock      = DockStyle.Top,
            Height    = 22,
        };
        Console = new RichTextBox { Dock = DockStyle.Fill };
        AppTheme.StyleConsole(Console);
        consoleCard.Controls.AddRange([Console, consoleTitle]);
        container.Controls.Add(consoleCard);

        scroll.Controls.Add(container);
        Controls.Add(scroll);
    }

    // ── Abstrações para subclasses ─────────────────────────────────────────────
    protected abstract string Title    { get; }
    protected abstract string Subtitle { get; }
    protected abstract void BuildContent(FlowLayoutPanel container);

    // ── Helpers para subclasses ───────────────────────────────────────────────

    protected void LogLine(string text, Color? color = null)
    {
        if (InvokeRequired) { Invoke(() => LogLine(text, color)); return; }

        Console.SelectionStart  = Console.TextLength;
        Console.SelectionLength = 0;
        Console.SelectionColor  = color ?? AppTheme.AccentSuccess;
        Console.AppendText($"[{DateTime.Now:HH:mm:ss}] {text}{Environment.NewLine}");
        Console.ScrollToCaret();
    }

    protected void LogError(string text) => LogLine(text, AppTheme.AccentDanger);
    protected void LogInfo(string  text) => LogLine(text, AppTheme.TextSecondary);

    protected CancellationToken StartOperation()
    {
        _cts?.Cancel();
        _cts = new CancellationTokenSource();
        return _cts.Token;
    }

    protected void CancelOperation() => _cts?.Cancel();

    protected async Task RunWithFeedbackAsync(
        StyledButton button,
        string originalText,
        Func<CancellationToken, Task> action)
    {
        var ct = StartOperation();
        button.Enabled = false;
        button.Text    = "Aguarde...";

        try
        {
            await action(ct);
        }
        finally
        {
            if (!IsDisposed)
            {
                button.Enabled = true;
                button.Text    = originalText;
            }
        }
    }
}
