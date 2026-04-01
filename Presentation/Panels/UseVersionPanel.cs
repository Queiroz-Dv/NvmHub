using NvmManager.Application.Services;
using NvmManager.Presentation.Controls;
using NvmManager.Presentation.Theme;

namespace NvmManager.Presentation.Panels;

public sealed class UseVersionPanel : BaseContentPanel
{
    protected override string Title    => "Usar Versão";
    protected override string Subtitle => "Define a versão ativa do Node.js para o terminal atual.";

    public UseVersionPanel(NvmApplicationService appService) : base(appService) { }

    protected override void BuildContent(FlowLayoutPanel container)
    {
        var card = new CardPanel { Width = 800, AutoSize = true, Margin = new Padding(0, 12, 0, 0) };

        // ─ Aviso sobre permissões ─
        var warningBox = new Panel
        {
            Width     = 750,
            Height    = 64,
            BackColor = Color.FromArgb(40, 32, 18),   // âmbar escuro sólido
            Margin    = new Padding(0, 0, 0, 20),
        };
        var warningIcon = new Label { Text = "⚠", Font = new Font("Segoe UI", 16f), ForeColor = AppTheme.AccentWarning, Location = new Point(12, 16), AutoSize = true };
        var warningText = new Label
        {
            Text      = "O comando 'nvm use' requer que o NVM Manager seja executado como Administrador para alterar symlinks do Node.js.",
            Font      = AppTheme.FontSmall,
            ForeColor = AppTheme.AccentWarning,
            Location  = new Point(42, 10),
            Size      = new Size(700, 46),
        };
        warningBox.Controls.AddRange([warningIcon, warningText]);
        card.Controls.Add(warningBox);

        // ─ Input ─
        var inputWrapper = new LabeledInput("Versão a usar", "Ex: 18.17.0 ou 20.11.1")
        {
            Width  = 400,
            Margin = new Padding(0, 0, 0, 20),
        };
        card.Controls.Add(inputWrapper);

        // ─ Lista rápida instaladas ─
        var listTitle = new Label
        {
            Text      = "Selecione de uma versão instalada:",
            Font      = AppTheme.FontSmall,
            ForeColor = AppTheme.TextSecondary,
            AutoSize  = true,
            Margin    = new Padding(0, 0, 0, 8),
        };
        card.Controls.Add(listTitle);

        var listBox = new ListBox
        {
            Width       = 400,
            Height      = 120,
            BackColor   = Color.FromArgb(15, 20, 36),
            ForeColor   = AppTheme.TextPrimary,
            Font        = AppTheme.FontMono,
            BorderStyle = BorderStyle.FixedSingle,
            Margin      = new Padding(0, 0, 0, 20),
        };
        card.Controls.Add(listBox);

        listBox.SelectedIndexChanged += (_, _) =>
        {
            if (listBox.SelectedItem is string sel)
                inputWrapper.Input.Text = sel.TrimStart('*', ' ', 'v').Trim().Split(' ')[0];
        };

        // ─ Botão ─
        var useBtn = new StyledButton
        {
            Text        = "▶  Usar Esta Versão",
            Width       = 220,
            NormalColor = AppTheme.AccentSuccess,
            HoverColor  = Color.FromArgb(14, 177, 74),
            PressedColor = Color.FromArgb(10, 157, 60),
        };
        card.Controls.Add(useBtn);
        container.Controls.Add(card);

        // ── Eventos ──────────────────────────────────────────────────────────
        useBtn.Click += async (_, _) =>
        {
            var version = inputWrapper.Input.Text.Trim();
            if (string.IsNullOrEmpty(version)) { LogError("Informe uma versão."); return; }

            await RunWithFeedbackAsync(useBtn, "▶  Usar Esta Versão", async ct =>
            {
                LogInfo($"Alternando para Node.js v{version}...");
                var result = await AppService.UseVersionAsync(version, ct);

                if (result.IsSuccess)
                    LogLine($"✓ {result.Message}");
                else
                {
                    LogError($"✗ {result.Message}");
                    if (result.Output is not null) LogInfo(result.Output);
                }
            });
        };

        // Popula lista ao tornar visível
        VisibleChanged += async (_, _) =>
        {
            if (!Visible) return;
            var result = await AppService.ListVersionsAsync();
            listBox.Items.Clear();
            if (result.IsSuccess && result.Data is not null)
                foreach (var v in result.Data)
                    listBox.Items.Add($"{(v.IsActive ? "* " : "  ")}{v.DisplayVersion}{(v.IsLts ? " (LTS)" : "")}");
        };
    }
}
