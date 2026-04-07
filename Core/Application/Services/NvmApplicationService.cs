using Core.Domain.Entities;
using Core.Domain.Interfaces;
using Core.Domain.Results;

namespace Core.Application.Services;

/// <summary>
/// Camada de aplicação: orquestra os use-cases do NVM com validações e cache.
/// </summary>
public sealed class NvmApplicationService
{
    private readonly INvmService _nvmService;
    private readonly INvmInstaller _nvmInstaller;
    private readonly VersionCacheService _versionCache;

    public NvmApplicationService(
        INvmService nvmService,
        INvmInstaller nvmInstaller,
        VersionCacheService versionCache)
    {
        _nvmService = nvmService ?? throw new ArgumentNullException(nameof(nvmService));
        _nvmInstaller = nvmInstaller ?? throw new ArgumentNullException(nameof(nvmInstaller));
        _versionCache = versionCache ?? throw new ArgumentNullException(nameof(versionCache));
    }

    // ── NVM ───────────────────────────────────────────────────────────────────

    public Task<bool> IsNvmInstalledAsync(CancellationToken ct = default)
        => _nvmService.IsNvmInstalledAsync(ct);

    public Task<string?> GetNvmVersionAsync(CancellationToken ct = default)
    => _nvmService.GetNvmVersionAsync(ct);

    public async Task<IEnumerable<string>> GetAvailableVersionsAsync(CancellationToken ct = default)
    {
        if (!await _nvmService.IsNvmInstalledAsync(ct))
            return Enumerable.Empty<string>();

        var result = await _nvmService.ListVersionsAsync(ct);

        if (!result.IsSuccess || result.Data is null)
            return Enumerable.Empty<string>();

        return result.Data
            .Select(v => v.Version.Trim().TrimStart('v', 'V'))
            .Where(v => Version.TryParse(v, out _))
            .OrderByDescending(v => Version.Parse(v))
            .ToList();
    }


    /// <summary>
    /// Retorna a versão ativa do Node.js.
    /// Primeira chamada consulta o processo; as seguintes usam o cache em memória.
    /// </summary>
    public async Task<string?> GetCurrentVersionAsync(CancellationToken ct = default)
    {
        if (_versionCache.TryGetCurrentVersion(out var cached))
            return cached;

        var version = await _nvmService.GetCurrentVersionAsync(ct);
        _versionCache.SetCurrentVersion(version);
        return version;
    }

    public Task<string?> GetLatestNvmVersionAsync(CancellationToken ct = default)
        => _nvmInstaller.GetLatestNvmVersionAsync(ct);

    public Task<OperationResult> InstallNvmAsync(
        IProgress<string>? progress = null,
        CancellationToken ct = default)
        => _nvmInstaller.InstallNvmAsync(progress, ct);

    // ── Node versions ─────────────────────────────────────────────────────────

    public async Task<OperationResult> InstallStableNodeAsync(CancellationToken ct = default)
    {
        var available = await GetRemoteAvailableVersionsAsync(ct);

        var stable = available
            .OrderByDescending(v => Version.Parse(v))
            .FirstOrDefault();

        if (stable is null)
            return OperationResult.Failure("Não foi possível identificar uma versão estável do Node.");

        await InstallVersionAsync(stable, ct: ct);
        await UseVersionAsync(stable, ct);

        return OperationResult.Success($"Node.js estável v{stable} instalado e ativado.");
    }

    public async Task<OperationResult<IReadOnlyList<NodeVersion>>> ListVersionsAsync(
        CancellationToken ct = default)
    {
        if (!await _nvmService.IsNvmInstalledAsync(ct))
            return OperationResult<IReadOnlyList<NodeVersion>>.Failure(
                "NVM não está instalado. Por favor, instale-o primeiro.");

        return await _nvmService.ListVersionsAsync(ct);
    }

    public async Task<OperationResult> InstallVersionAsync(
        string rawVersion,
        IProgress<string>? progress = null,
        CancellationToken ct = default)
    {
        if (!await _nvmService.IsNvmInstalledAsync(ct))
            return OperationResult.Failure("NVM não está instalado. Por favor, instale-o primeiro.");

        var validation = ValidateVersionString(rawVersion);
        if (!validation.IsSuccess) return validation;

        return await _nvmService.InstallVersionAsync(rawVersion.Trim(), progress, ct);
    }

    /// <summary>
    /// Ativa uma versão. Em caso de sucesso, invalida o cache de versão atual.
    /// </summary>
    public async Task<OperationResult> UseVersionAsync(
        string rawVersion,
        CancellationToken ct = default)
    {
        if (!await _nvmService.IsNvmInstalledAsync(ct))
            return OperationResult.Failure("NVM não está instalado. Por favor, instale-o primeiro.");

        var validation = ValidateVersionString(rawVersion);
        if (!validation.IsSuccess) return validation;

        var result = await _nvmService.UseVersionAsync(rawVersion.Trim(), ct);

        // Invalida o cache sempre que a operação de troca de versão for executada
        // (mesmo em falha parcial, o estado pode ter mudado)
        _versionCache.Invalidate();

        return result;
    }

    /// <summary>
    /// Desinstala uma versão. Invalida o cache pois a versão ativa pode ter mudado.
    /// </summary>
    public async Task<OperationResult> UninstallVersionAsync(
        string rawVersion,
        CancellationToken ct = default)
    {
        if (!await _nvmService.IsNvmInstalledAsync(ct))
            return OperationResult.Failure("NVM não está instalado. Por favor, instale-o primeiro.");

        var validation = ValidateVersionString(rawVersion);
        if (!validation.IsSuccess) return validation;

        var result = await _nvmService.UninstallVersionAsync(rawVersion.Trim(), ct);
        _versionCache.Invalidate();
        return result;
    }

    // ── Validação ─────────────────────────────────────────────────────────────

    private static OperationResult ValidateVersionString(string? version)
    {
        if (string.IsNullOrWhiteSpace(version))
            return OperationResult.Failure("Informe uma versão válida (ex.: 18.17.0).");

        var clean = version.Trim().TrimStart('v', 'V');
        var parts = clean.Split('.');

        if (parts.Length < 2 || !parts.All(p => int.TryParse(p, out _)))
            return OperationResult.Failure(
                $"Formato inválido: '{version}'. Use X.Y.Z ou X.Y (ex.: 18.17.0).");

        return OperationResult.Success("OK");
    }

    public async Task<IEnumerable<string>> GetRemoteAvailableVersionsAsync(
    CancellationToken ct = default)
    {
        if (!await _nvmService.IsNvmInstalledAsync(ct))
            return Enumerable.Empty<string>();

        var result = await _nvmService.ListRemoteVersionsAsync(ct);

        if (!result.IsSuccess || result.Data is null)
            return Enumerable.Empty<string>();

        return result.Data
            .Select(v => v.Trim().TrimStart('v', 'V'))
            .Where(v => Version.TryParse(v, out _))
            .OrderByDescending(v => Version.Parse(v))
            .ToList();
    }

}
