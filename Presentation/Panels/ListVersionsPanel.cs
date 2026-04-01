using NvmManager.Application.Services;
using NvmManager.Domain.Entities;
using NvmManager.Presentation.Controls;
using NvmManager.Presentation.Theme;

namespace NvmManager.Presentation.Panels;

public sealed class ListVersionsPanel : BaseContentPanel
{
    protected override string Title    => "Versões Instaladas";
    protected override string Subtitle => "Lista todas as versões do Node.js gerenciadas pelo NVM.";

    private FlowLayoutPanel _listContainer = null!;
    private Label           _emptyLabel    = null!;
    private StyledButton    _refreshBtn    = null!;

    public ListVersionsPanel(NvmApplicationService appService) : base(appService) { }

    protected override void BuildContent(FlowLayoutPanel container)
    {
        // ─ Toolbar ─
        var toolbar = new FlowLayoutPanel
        {
            FlowDirection = FlowDirection.LeftToRight,
            AutoSize      = true,
            BackColor     = Color.Transparent,
            Margin        = new Padding(0, 0, 0, 16),
        };

        _refreshBtn = new StyledButton
        {
            Text        = "⟳  Atualizar Lista",
            Width       = 170,
            NormalColor = Color.FromArgb(30, 38, 65),
            HoverColor  = Color.FromArgb(40, 50, 85),
        };
        toolbar.Controls.Add(_refreshBtn);
        container.Controls.Add(toolbar);

        // ─ Grid de versões ─
        var card = new CardPanel { Width = 800, AutoSize = true, MinimumSize = new Size(800, 80), Margin = new Padding(0, 0, 0, 0) };

        _emptyLabel = new Label
        {
            Text      = "Nenhuma versão encontrada. Instale uma versão do Node.js.",
            Font      = AppTheme.FontBody,
            ForeColor = AppTheme.TextMuted,
            Dock      = DockStyle.Top,
            Height    = 40,
            Visible   = false,
        };

        _listContainer = new FlowLayoutPanel
        {
            FlowDirection = FlowDirection.TopDown,
            WrapContents  = false,
            AutoSize      = true,
            Dock          = DockStyle.Top,
            BackColor     = Color.Transparent,
        };

        card.Controls.AddRange([_listContainer, _emptyLabel]);
        container.Controls.Add(card);

        // ── Eventos ──────────────────────────────────────────────────────────
        _refreshBtn.Click += async (_, _) => await LoadVersionsAsync();
    }

    private async Task LoadVersionsAsync()
    {
        await RunWithFeedbackAsync(_refreshBtn, "⟳  Atualizar Lista", async ct =>
        {
            LogInfo("Carregando versões instaladas...");
            _listContainer.Controls.Clear();

            var result = await AppService.ListVersionsAsync(ct);

            if (!result.IsSuccess)
            {
                LogError(result.Message);
                _emptyLabel.Visible = true;
                return;
            }

            var versions = result.Data!;

            if (versions.Count == 0)
            {
                _emptyLabel.Visible = true;
                LogInfo("Nenhuma versão instalada.");
                return;
            }

            _emptyLabel.Visible = false;
            LogLine($"✓ {versions.Count} versão(ões) encontrada(s).");

            foreach (var version in versions)
            {
                var row = BuildVersionRow(version);
                if (InvokeRequired) Invoke(() => _listContainer.Controls.Add(row));
                else _listContainer.Controls.Add(row);
            }
        });
    }

    private Panel BuildVersionRow(NodeVersion version)
    {
        var row = new Panel
        {
            Width     = 740,
            Height    = 52,
            BackColor = version.IsActive ? Color.FromArgb(26, 30, 55) : Color.Transparent,
            Margin    = new Padding(0, 0, 0, 4),
        };

        // Indicador ativo
        if (version.IsActive)
        {
            var indicator = new Panel { Width = 4, Height = 52, Location = new Point(0, 0), BackColor = AppTheme.AccentPrimary };
            row.Controls.Add(indicator);
        }

        // Ícone versão
        var icon = new Label
        {
            Text      = "◉",
            Font      = new Font("Segoe UI", 12f),
            ForeColor = version.IsActive ? AppTheme.AccentPrimary : AppTheme.TextMuted,
            Location  = new Point(16, 15),
            Size      = new Size(24, 24),
        };
        row.Controls.Add(icon);

        // Nome da versão
        var verLabel = new Label
        {
            Text      = version.DisplayVersion,
            Font      = new Font("Consolas", 13f, FontStyle.Bold),
            ForeColor = AppTheme.TextPrimary,
            Location  = new Point(46, 14),
            AutoSize  = true,
        };
        row.Controls.Add(verLabel);

        // Badges
        var badgeX = 170;
        if (version.IsActive)
        {
            var activeBadge = new StatusBadge("ATIVA", Color.FromArgb(30, 38, 80), AppTheme.AccentPrimary)
            { Location = new Point(badgeX, 16) };
            row.Controls.Add(activeBadge);
            badgeX += 70;
        }
        if (version.IsLts)
        {
            var ltsBadge = new StatusBadge("LTS", Color.FromArgb(18, 45, 28), AppTheme.AccentSuccess)
            { Location = new Point(badgeX, 16) };
            row.Controls.Add(ltsBadge);
        }

        // Separador
        var sep = new Panel { Width = 740, Height = 1, Location = new Point(0, 51), BackColor = AppTheme.CardBorder };
        row.Controls.Add(sep);

        return row;
    }

    protected override void OnVisibleChanged(EventArgs e)
    {
        base.OnVisibleChanged(e);
        if (Visible) Task.Run(LoadVersionsAsync);
    }
}
