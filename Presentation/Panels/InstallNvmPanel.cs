using NvmManager.Application.Services;
using NvmManager.Presentation.Controls;
using NvmManager.Presentation.Theme;

namespace NvmManager.Presentation.Panels;

public sealed class InstallNvmPanel : BaseContentPanel
{
    protected override string Title    => "Instalar NVM";
    protected override string Subtitle => "Instala a versão mais recente do NVM for Windows automaticamente.";

    public InstallNvmPanel(NvmApplicationService appService) : base(appService) { }

    protected override void BuildContent(FlowLayoutPanel container)
    {
        var card = new CardPanel { Width = 800, AutoSize = true, Margin = new Padding(0, 12, 0, 0) };

        // ─ Status check ─
        var statusRow = new FlowLayoutPanel
        {
            FlowDirection = FlowDirection.LeftToRight,
            AutoSize      = true,
            BackColor     = Color.Transparent,
            Margin        = new Padding(0, 0, 0, 16),
            Padding       = new Padding(0),
        };

        var statusIcon  = new Label { Text = "⟳", Font = new Font("Segoe UI", 20f), ForeColor = AppTheme.TextSecondary, AutoSize = true };
        var statusText  = new Label { Text = "Verificando status do NVM...", Font = AppTheme.FontBody, ForeColor = AppTheme.TextSecondary, AutoSize = true };
        statusRow.Controls.AddRange([statusIcon, statusText]);
        card.Controls.Add(statusRow);

        // ─ Info box ─  (cor sólida sutil — alpha em BackColor não funciona em WinForms)
        var infoBox = new Panel
        {
            Width     = 750,
            Height    = 80,
            BackColor = Color.FromArgb(28, 34, 62),   // azul-escuro discreto, sem saturação
            Margin    = new Padding(0, 0, 0, 20),
        };
        var infoText = new Label
        {
            Text      = "O NVM for Windows permite instalar e alternar entre múltiplas versões do Node.js. " +
                        "Ao clicar em instalar, o setup mais recente será baixado do GitHub e executado automaticamente.",
            Font      = AppTheme.FontBody,
            ForeColor = AppTheme.TextSecondary,
            Location  = new Point(16, 12),
            Size      = new Size(718, 58),
        };
        infoBox.Controls.Add(infoText);
        card.Controls.Add(infoBox);

        // ─ Versão mais recente ─
        var versionLabel = new Label
        {
            Text      = "Buscando versão mais recente...",
            Font      = AppTheme.FontBody,
            ForeColor = AppTheme.TextSecondary,
            AutoSize  = true,
            Margin    = new Padding(0, 0, 0, 16),
        };
        card.Controls.Add(versionLabel);

        // ─ Botão instalar ─
        var installBtn = new StyledButton
        {
            Text   = "⬇  Instalar NVM for Windows",
            Width  = 280,
            NormalColor = AppTheme.AccentPrimary,
        };

        card.Controls.Add(installBtn);
        container.Controls.Add(card);

        // ── Eventos ──────────────────────────────────────────────────────────
        Load += async () =>
        {
            // Verifica status do nvm
            var installed = await AppService.IsNvmInstalledAsync();
            statusIcon.Text      = installed ? "✓" : "✗";
            statusIcon.ForeColor = installed ? AppTheme.AccentSuccess : AppTheme.AccentDanger;
            statusText.Text      = installed ? "NVM está instalado e funcionando." : "NVM não encontrado neste sistema.";
            statusText.ForeColor = installed ? AppTheme.AccentSuccess : AppTheme.AccentDanger;
            installBtn.Text      = installed ? "⟳  Reinstalar / Atualizar NVM" : "⬇  Instalar NVM for Windows";

            // Busca versão mais recente
            var latest = await AppService.GetLatestNvmVersionAsync();
            versionLabel.Text = latest is not null
                ? $"Versão disponível no GitHub: {latest}"
                : "Não foi possível verificar a versão mais recente.";
        };

        installBtn.Click += async (_, _) =>
        {
            await RunWithFeedbackAsync(installBtn, installBtn.Text, async ct =>
            {
                LogInfo("Iniciando instalação do NVM for Windows...");
                var progress = new Progress<string>(msg => LogLine(msg, AppTheme.AccentPrimary));
                var result   = await AppService.InstallNvmAsync(progress, ct);

                if (result.IsSuccess)
                {
                    LogLine($"✓ {result.Message}");
                    statusIcon.Text      = "✓";
                    statusIcon.ForeColor = AppTheme.AccentSuccess;
                    statusText.Text      = "NVM instalado com sucesso!";
                    statusText.ForeColor = AppTheme.AccentSuccess;
                }
                else
                {
                    LogError($"✗ {result.Message}");
                    if (result.Output is not null) LogInfo(result.Output);
                }
            });
        };
    }

    // Shortcut to fire async load on panel visibility
    public event Func<Task>? Load;
    protected override void OnVisibleChanged(EventArgs e)
    {
        base.OnVisibleChanged(e);
        if (Visible && Load is not null)
            Task.Run(async () => { try { await Load(); } catch { /* ignore */ } });
    }
}
