using NvmManager.Domain.Entities;
using NvmManager.Domain.Interfaces;
using NvmManager.Domain.Results;

namespace NvmManager.Application.Services;

/// <summary>
/// Camada de aplicação: orquestra os use-cases relacionados ao NVM,
/// adicionando validações e regras de negócio antes de delegar à infra.
/// </summary>
public sealed class NvmApplicationService
{
    private readonly INvmService   _nvmService;
    private readonly INvmInstaller _nvmInstaller;

    public NvmApplicationService(INvmService nvmService, INvmInstaller nvmInstaller)
    {
        _nvmService   = nvmService   ?? throw new ArgumentNullException(nameof(nvmService));
        _nvmInstaller = nvmInstaller ?? throw new ArgumentNullException(nameof(nvmInstaller));
    }

    // ── NVM ───────────────────────────────────────────────────────────────────

    public Task<bool> IsNvmInstalledAsync(CancellationToken ct = default)
        => _nvmService.IsNvmInstalledAsync(ct);

    public Task<string?> GetLatestNvmVersionAsync(CancellationToken ct = default)
        => _nvmInstaller.GetLatestNvmVersionAsync(ct);

    public Task<OperationResult> InstallNvmAsync(
        IProgress<string>? progress = null,
        CancellationToken ct = default)
        => _nvmInstaller.InstallNvmAsync(progress, ct);

    // ── Node versions ─────────────────────────────────────────────────────────

    public async Task<OperationResult<IReadOnlyList<NodeVersion>>> ListVersionsAsync(CancellationToken ct = default)
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

        var validationResult = ValidateVersionString(rawVersion);
        if (!validationResult.IsSuccess) return validationResult;

        return await _nvmService.InstallVersionAsync(rawVersion.Trim(), progress, ct);
    }

    public async Task<OperationResult> UseVersionAsync(string rawVersion, CancellationToken ct = default)
    {
        if (!await _nvmService.IsNvmInstalledAsync(ct))
            return OperationResult.Failure("NVM não está instalado. Por favor, instale-o primeiro.");

        var validationResult = ValidateVersionString(rawVersion);
        if (!validationResult.IsSuccess) return validationResult;

        return await _nvmService.UseVersionAsync(rawVersion.Trim(), ct);
    }

    public async Task<OperationResult> UninstallVersionAsync(string rawVersion, CancellationToken ct = default)
    {
        if (!await _nvmService.IsNvmInstalledAsync(ct))
            return OperationResult.Failure("NVM não está instalado. Por favor, instale-o primeiro.");

        var validationResult = ValidateVersionString(rawVersion);
        if (!validationResult.IsSuccess) return validationResult;

        return await _nvmService.UninstallVersionAsync(rawVersion.Trim(), ct);
    }

    // ── Validation ────────────────────────────────────────────────────────────

    private static OperationResult ValidateVersionString(string? version)
    {
        if (string.IsNullOrWhiteSpace(version))
            return OperationResult.Failure("Informe uma versão válida (ex.: 18.17.0).");

        var clean = version.Trim().TrimStart('v', 'V');
        var parts = clean.Split('.');

        if (parts.Length < 2 || !parts.All(p => int.TryParse(p, out _)))
            return OperationResult.Failure(
                $"Formato de versão inválido: '{version}'. Use o formato X.Y.Z ou X.Y (ex.: 18.17.0).");

        return OperationResult.Success("OK");
    }
}
