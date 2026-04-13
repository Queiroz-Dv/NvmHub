namespace NvmManager.Web.Core.Application.Services;

/// <summary>
/// Cache em memória para a versão do Node.js atualmente ativa.
/// Evita chamadas repetidas ao processo nvm para operações de leitura de UI.
/// Invalidado automaticamente quando o usuário troca ou desinstala uma versão.
/// </summary>
public sealed class VersionCacheService
{
    private string? _currentVersion;
    private bool    _hasValue;

    /// <summary>Retorna true e preenche <paramref name="version"/> se o cache tiver um valor.</summary>
    public bool TryGetCurrentVersion(out string? version)
    {
        version = _currentVersion;
        return _hasValue;
    }

    /// <summary>Armazena a versão ativa em memória.</summary>
    public void SetCurrentVersion(string? version)
    {
        _currentVersion = version;
        _hasValue       = true;
    }

    /// <summary>Descarta o valor cacheado — deve ser chamado após UseVersion ou Uninstall.</summary>
    public void Invalidate()
    {
        _currentVersion = null;
        _hasValue       = false;
    }
}
