using NvmManager.Application.Services;
using NvmManager.Presentation.Controls;
using NvmManager.Presentation.Theme;

namespace NvmManager.Presentation.Panels;

public sealed class UninstallVersionPanel : BaseContentPanel
{
    protected override string Title    => "Desinstalar Versão";
    protected override string Subtitle => "Remove uma versão do Node.js instalada pelo NVM.";

    public UninstallVersionPanel(NvmApplicationService appService) : base(appService) { }

    protected override void BuildContent(FlowLayoutPanel container)
    {
        var card = new CardPanel { Width = 800, AutoSize = true, Margin = new Padding(0, 12, 0, 0) };

        // ─ Aviso ─
        var warningBox = new Panel
        {
            Width     = 750,
            Height    = 56,
            BackColor = Color.FromArgb(45, 18, 18),   // vermelho escuro sólido
            Margin    = new Padding(0, 0, 0, 20),
        };
        var warningIcon = new Label { Text = "✗", Font = new Font("Segoe UI", 15f, FontStyle.Bold), ForeColor = AppTheme.AccentDanger, Location = new Point(12, 14), AutoSize = true };
        var warningText = new Label
        {
            Text      = "Esta ação é irreversível. A versão selecionada será removida do sistema e precisará ser reinstalada caso necessário.",
            Font      = AppTheme.FontSmall,
            ForeColor = AppTheme.AccentDanger,
            Location  = new Point(40, 10),
            Size      = new Size(702, 38),
        };
        warningBox.Controls.AddRange([warningIcon, warningText]);
        card.Controls.Add(warningBox);

        // ─ Lista de versões instaladas ─
        var listTitle = new Label
        {
            Text      = "Selecione a versão para remover:",
            Font      = AppTheme.FontSmall,
            ForeColor = AppTheme.TextSecondary,
            AutoSize  = true,
            Margin    = new Padding(0, 0, 0, 8),
        };
        card.Controls.Add(listTitle);

        var listBox = new ListBox
        {
            Width       = 400,
            Height      = 130,
            BackColor   = Color.FromArgb(15, 20, 36),
            ForeColor   = AppTheme.TextPrimary,
            Font        = AppTheme.FontMono,
            BorderStyle = BorderStyle.FixedSingle,
            Margin      = new Padding(0, 0, 0, 16),
        };
        card.Controls.Add(listBox);

        // ─ Input manual ─
        var inputWrapper = new LabeledInput("Ou informe manualmente a versão", "Ex: 16.20.2")
        {
            Width  = 400,
            Margin = new Padding(0, 0, 0, 20),
        };
        card.Controls.Add(inputWrapper);

        listBox.SelectedIndexChanged += (_, _) =>
        {
            if (listBox.SelectedItem is string sel)
                inputWrapper.Input.Text = sel.Replace("*", "").Trim().TrimStart('v').Split(' ')[0];
        };

        // ─ Botão ─
        var uninstallBtn = new StyledButton
        {
            Text         = "🗑  Desinstalar Versão",
            Width        = 230,
            NormalColor  = AppTheme.AccentDanger,
            HoverColor   = Color.FromArgb(220, 38, 38),
            PressedColor = Color.FromArgb(185, 28, 28),
        };
        card.Controls.Add(uninstallBtn);
        container.Controls.Add(card);

        // ── Eventos ──────────────────────────────────────────────────────────
        uninstallBtn.Click += async (_, _) =>
        {
            var version = inputWrapper.Input.Text.Trim();
            if (string.IsNullOrEmpty(version)) { LogError("Selecione ou informe uma versão para desinstalar."); return; }

            var confirm = MessageBox.Show(
                $"Tem certeza que deseja desinstalar o Node.js v{version}?\n\nEsta ação não pode ser desfeita.",
                "Confirmar Desinstalação",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Warning,
                MessageBoxDefaultButton.Button2);

            if (confirm != DialogResult.Yes) { LogInfo("Operação cancelada pelo usuário."); return; }

            await RunWithFeedbackAsync(uninstallBtn, "🗑  Desinstalar Versão", async ct =>
            {
                LogInfo($"Desinstalando Node.js v{version}...");
                var result = await AppService.UninstallVersionAsync(version, ct);

                if (result.IsSuccess)
                {
                    LogLine($"✓ {result.Message}");
                    // Recarrega lista
                    await RefreshListAsync(listBox);
                    inputWrapper.Input.Clear();
                }
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
            if (Visible) await RefreshListAsync(listBox);
        };
    }

    private async Task RefreshListAsync(ListBox listBox)
    {
        var result = await AppService.ListVersionsAsync();
        Action update = () =>
        {
            listBox.Items.Clear();
            if (result.IsSuccess && result.Data is not null)
                foreach (var v in result.Data)
                    listBox.Items.Add($"{(v.IsActive ? "* " : "  ")}{v.DisplayVersion}{(v.IsLts ? " (LTS)" : "")}");
        };
        if (listBox.InvokeRequired) listBox.Invoke(update);
        else update();
    }
}
