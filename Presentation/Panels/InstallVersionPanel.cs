using NvmManager.Application.Services;
using NvmManager.Presentation.Controls;
using NvmManager.Presentation.Theme;

namespace NvmManager.Presentation.Panels;

public sealed class InstallVersionPanel : BaseContentPanel
{
    protected override string Title    => "Instalar Versão do Node.js";
    protected override string Subtitle => "Informe o número da versão para instalação via NVM.";

    public InstallVersionPanel(NvmApplicationService appService) : base(appService) { }

    protected override void BuildContent(FlowLayoutPanel container)
    {
        var card = new CardPanel { Width = 800, AutoSize = true, Margin = new Padding(0, 12, 0, 0) };

        // ─ Input ─
        var inputWrapper = new LabeledInput("Número da versão", "Ex: 18.17.0, 20.11.1, 21.0.0")
        {
            Width  = 400,
            Margin = new Padding(0, 0, 0, 20),
        };
        card.Controls.Add(inputWrapper);

        // ─ Atalhos para versões LTS populares ─
        var quickTitle = new Label
        {
            Text      = "Versões LTS Populares:",
            Font      = AppTheme.FontSmall,
            ForeColor = AppTheme.TextSecondary,
            AutoSize  = true,
            Margin    = new Padding(0, 0, 0, 8),
        };
        card.Controls.Add(quickTitle);

        var quickPanel = new FlowLayoutPanel
        {
            FlowDirection = FlowDirection.LeftToRight,
            AutoSize      = true,
            BackColor     = Color.Transparent,
            Margin        = new Padding(0, 0, 0, 24),
        };

        var ltsVersions = new[] { ("20.11.1", "Iron LTS"), ("18.20.2", "Hydrogen LTS"), ("16.20.2", "Gallium LTS") };
        foreach (var (ver, label) in ltsVersions)
        {
            var btn = new StyledButton
            {
                Text        = $"v{ver}  ({label})",
                Width       = 190,
                Height      = 36,
                NormalColor = Color.FromArgb(30, 38, 65),
                HoverColor  = Color.FromArgb(40, 50, 85),
                Margin      = new Padding(0, 0, 10, 0),
            };
            var capturedVer = ver;
            btn.Click += (_, _) => inputWrapper.Input.Text = capturedVer;
            quickPanel.Controls.Add(btn);
        }
        card.Controls.Add(quickPanel);

        // ─ Botão instalar ─
        var installBtn = new StyledButton
        {
            Text  = "⬇  Instalar Versão",
            Width = 220,
        };
        card.Controls.Add(installBtn);
        container.Controls.Add(card);

        // ── Evento ───────────────────────────────────────────────────────────
        installBtn.Click += async (_, _) =>
        {
            var version = inputWrapper.Input.Text.Trim();
            if (string.IsNullOrEmpty(version))
            {
                LogError("Informe um número de versão antes de instalar.");
                return;
            }

            await RunWithFeedbackAsync(installBtn, "⬇  Instalar Versão", async ct =>
            {
                LogInfo($"Iniciando instalação do Node.js v{version}...");
                var progress = new Progress<string>(msg => LogLine(msg, AppTheme.AccentPrimary));
                var result   = await AppService.InstallVersionAsync(version, progress, ct);

                if (result.IsSuccess)
                    LogLine($"✓ {result.Message}");
                else
                {
                    LogError($"✗ {result.Message}");
                    if (result.Output is not null) LogInfo(result.Output);
                }
            });
        };
    }
}
