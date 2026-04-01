using NvmManager.Application.Services;
using NvmManager.Presentation.Controls;
using NvmManager.Presentation.Panels;
using NvmManager.Presentation.Theme;

namespace NvmManager.Presentation.Forms;

/// <summary>
/// Janela principal da aplicação. Usa layout de sidebar + content area.
/// </summary>
public sealed class MainForm : Form
{
    private readonly NvmApplicationService _appService;
    private Panel        _contentArea  = null!;
    private SidebarButton[] _navButtons = [];
    private Panel? _statusChip;
    private bool _isNvmInstalledCached;

    // Páginas
    private InstallNvmPanel?     _installNvmPanel;
    private InstallVersionPanel? _installVersionPanel;
    private ListVersionsPanel?   _listVersionsPanel;
    private UseVersionPanel?     _useVersionPanel;
    private UninstallVersionPanel? _uninstallVersionPanel;

    public MainForm(NvmApplicationService appService)
    {
        _appService = appService;
        InitializeForm();
        BuildLayout();
        // Start background status watcher and perform initial navigation after Load
        StartStatusWatcher();
        Load += async (_, _) => await InitialNavigationAsync();
    }

    private void StartStatusWatcher()
    {
        _ = Task.Run(async () =>
        {
            try
            {
                var installed = await _appService.IsNvmInstalledAsync();
                _isNvmInstalledCached = installed;
                if (_statusChip is not null && !_statusChip.IsDisposed)
                    Invoke(() => _statusChip.Invalidate());
            }
            catch
            {
                // ignore background errors
            }
        });
    }

    /// <summary>
    /// Verifica se o NVM está instalado e ajusta a sidebar e navegação inicial.
    /// Se instalado → oculta o item "Instalar NVM" e abre "Instalar Versão".
    /// Se não instalado → abre "Instalar NVM".
    /// </summary>
    private async Task InitialNavigationAsync()
    {
        var nvmInstalled = await _appService.IsNvmInstalledAsync();

        // Oculta o botão "Instalar NVM" (índice 0) se o NVM já estiver presente
        _navButtons[0].Visible = !nvmInstalled;

        // Navega para o painel correto
        NavigateTo(nvmInstalled ? 1 : 0);
    }

    private void InitializeForm()
    {
        Text            = "NVM Manager";
        ClientSize      = new Size(1060, 660);
        StartPosition   = FormStartPosition.CenterScreen;
        BackColor       = AppTheme.Background;
        ForeColor       = AppTheme.TextPrimary;
        FormBorderStyle = FormBorderStyle.FixedDialog;   // sem redimensionamento nem maximizar
        MaximizeBox     = false;
        MinimizeBox     = true;
        DoubleBuffered  = true;
        Icon            = CreateAppIcon();
    }

    private void BuildLayout()
    {
        // ── Sidebar ─────────────────────────────────────────────────────────
        var sidebar = new Panel
        {
            Dock      = DockStyle.Left,
            Width     = AppTheme.SidebarWidth,
            BackColor = AppTheme.SidebarBg,
        };

        // Logo area — espaçamento generoso entre ícone e texto
        var logoPanel = new Panel { Dock = DockStyle.Top, Height = AppTheme.HeaderHeight + 20, BackColor = AppTheme.SidebarBg };
        var logoIcon  = new Label { Text = "⬡", Font = new Font("Segoe UI", 24f, FontStyle.Bold), ForeColor = AppTheme.AccentPrimary, Location = new Point(14, 12), AutoSize = true };
        var logoName  = new Label { Text = "NVM Manager", Font = new Font("Segoe UI", 12f, FontStyle.Bold), ForeColor = AppTheme.TextPrimary, Location = new Point(72, 18), AutoSize = true };
        var logoVer   = new Label { Text = "v1.0.0", Font = AppTheme.FontSmall, ForeColor = AppTheme.TextMuted, Location = new Point(73, 42), AutoSize = true };
        logoPanel.Controls.AddRange([logoIcon, logoName, logoVer]);

        // Nav separator
        var navSep = new Panel { Dock = DockStyle.Top, Height = 1, BackColor = AppTheme.CardBorder };

        // Nav buttons
        var navPanel = new Panel { Dock = DockStyle.Top, AutoSize = true };

        var navItems = new (string icon, string label)[]
        {
            ("⬇", "Instalar NVM"),    // índice 0 — pode ser ocultado
            ("📦", "Instalar Versão"),
            ("☰", "Listar Versões"),
            ("▶", "Usar Versão"),
            ("🗑", "Desinstalar"),
        };

        _navButtons = new SidebarButton[navItems.Length];
        var yPos = 8;
        for (var i = 0; i < navItems.Length; i++)
        {
            var (icon, label) = navItems[i];
            var idx = i;
            var btn = new SidebarButton
            {
                Icon     = icon,
                Text     = label,
                Location = new Point(0, yPos),
                Width    = AppTheme.SidebarWidth,
            };
            btn.Activated += (_, _) => NavigateTo(idx);
            _navButtons[i] = btn;
            navPanel.Controls.Add(btn);
            yPos += 50;
        }
        navPanel.Height = yPos + 8;

        // Initial navigation is handled in Load via InitialNavigationAsync (subscribed in ctor).

        // Footer na sidebar
        var sidebarFooter = new Panel { Dock = DockStyle.Bottom, Height = 60, BackColor = AppTheme.SidebarBg };
        var footerSep = new Panel { Dock = DockStyle.Top, Height = 1, BackColor = AppTheme.CardBorder };
        var footerText = new Label
        {
            Text      = "NVM Manager  •  github.com/coreybutler",
            Font      = new Font("Segoe UI", 7.5f),
            ForeColor = AppTheme.TextMuted,
            Dock      = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleCenter,
        };
        sidebarFooter.Controls.AddRange([footerText, footerSep]);

        sidebar.Controls.Add(sidebarFooter);
        sidebar.Controls.Add(navPanel);
        sidebar.Controls.Add(navSep);
        sidebar.Controls.Add(logoPanel);

        // ── Header ───────────────────────────────────────────────────────────
        var header = new Panel
        {
            Dock      = DockStyle.Top,
            Height    = AppTheme.HeaderHeight,
            BackColor = AppTheme.SidebarBg,
            Padding   = new Padding(20, 0, 20, 0),
        };
        header.Paint += (_, e) =>
        {
            using var pen = new Pen(AppTheme.CardBorder);
            e.Graphics.DrawLine(pen, 0, header.Height - 1, header.Width, header.Height - 1);
        };

        var headerLabel = new Label
        {
            Text      = "NVM Manager — Gerenciador de Versões Node.js",
            Font      = AppTheme.FontBody,
            ForeColor = AppTheme.TextSecondary,
            Dock      = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleLeft,
        };

        // Status chip (uses cached status; painting must be synchronous)
        _statusChip = new Panel { Width = 160, Height = 28, Dock = DockStyle.Right };
        _statusChip.Paint += (_, e) =>
        {
            e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
            var color = _isNvmInstalledCached ? AppTheme.AccentSuccess : AppTheme.AccentDanger;
            using var bg = new SolidBrush(Color.FromArgb(30, color.R, color.G, color.B));
            AppTheme.FillRoundRect(e.Graphics, bg, new Rectangle(0, 4, 158, 22), 11);
            var dot = _isNvmInstalledCached ? "● NVM instalado" : "● NVM ausente";
            using var tb = new SolidBrush(color);
            e.Graphics.DrawString(dot, AppTheme.FontSmall, tb, 8f, 7f);
        };

        header.Controls.AddRange([_statusChip, headerLabel]);

        // ── Content Area ─────────────────────────────────────────────────────
        _contentArea = new Panel { Dock = DockStyle.Fill, BackColor = AppTheme.Background };

        Controls.Add(_contentArea);
        Controls.Add(header);
        Controls.Add(sidebar);
    }

    private void NavigateTo(int index)
    {
        // Atualiza estado visual apenas dos botões visíveis
        for (var i = 0; i < _navButtons.Length; i++)
            if (_navButtons[i].Visible)
                _navButtons[i].IsActive = i == index;

        // Esconde todos os painéis
        foreach (Control c in _contentArea.Controls)
            c.Visible = false;

        // Cria ou mostra o painel correspondente
        Panel targetPanel = index switch
        {
            0 => _installNvmPanel       ??= new InstallNvmPanel(_appService),
            1 => _installVersionPanel   ??= new InstallVersionPanel(_appService),
            2 => _listVersionsPanel     ??= new ListVersionsPanel(_appService),
            3 => _useVersionPanel       ??= new UseVersionPanel(_appService),
            4 => _uninstallVersionPanel ??= new UninstallVersionPanel(_appService),
            _ => throw new IndexOutOfRangeException($"Unknown nav index: {index}")
        };

        if (!_contentArea.Controls.Contains(targetPanel))
            _contentArea.Controls.Add(targetPanel);

        targetPanel.Visible = true;
        targetPanel.BringToFront();
    }

    private static Icon? CreateAppIcon()
    {
        try
        {
            // Gera um ícone programático simples (16x16)
            using var bmp    = new Bitmap(16, 16);
            using var g      = Graphics.FromImage(bmp);
            g.Clear(Color.Transparent);
            using var brush  = new SolidBrush(Color.FromArgb(99, 102, 241));
            g.FillEllipse(brush, 0, 0, 15, 15);
            using var font   = new Font("Arial", 7f, FontStyle.Bold);
            using var tb     = new SolidBrush(Color.White);
            g.DrawString("N", font, tb, 3f, 2f);
            var handle = bmp.GetHicon();
            return Icon.FromHandle(handle);
        }
        catch { return null; }
    }
}
