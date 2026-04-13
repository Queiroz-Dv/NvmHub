using NvmManager.Web.Core.Domain.Entities;

namespace NvmManager.Web.Core.Infrastructure;

/// <summary>
/// Cache em memória para o estado das versões do Node.js.
/// Elimina chamadas repetidas ao nvm.exe para dados que mudam pouco.
///
/// Ciclo de vida:
///   • Leitura inicial: preenchido no Load do MainForm.
///   • Após nvm use:    InvalidateCurrent() → só a versão ativa é limpa.
///   • Após install/uninstall: InvalidateAll() → grid e chip são recarregados.
/// </summary>
public sealed class VersionStateCache
{
    // ── Versão ativa ──────────────────────────────────────────────────────────
    private string? _currentVersion;
    private bool _currentLoaded;

    public bool HasCurrentVersion => _currentLoaded;
    public string? CurrentVersion => _currentVersion;

    public void SetCurrentVersion(string? version)
    {
        _currentVersion = version;
        _currentLoaded = true;
    }

    public void InvalidateCurrent()
    {
        _currentVersion = null;
        _currentLoaded = false;
    }

    // ── Lista de versões instaladas ───────────────────────────────────────────
    private IReadOnlyList<NodeVersion>? _installedVersions;

    public bool HasInstalledVersions => _installedVersions is not null;

    public IReadOnlyList<NodeVersion>? InstalledVersions
    {
        get => _installedVersions;
        set => _installedVersions = value;
    }

    /// <summary>Invalida tudo (após install ou uninstall).</summary>
    public void InvalidateAll()
    {
        InvalidateCurrent();
        _installedVersions = null;
    }
}
