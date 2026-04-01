using NvmManager.Domain.Entities;
using NvmManager.Domain.Results;

namespace NvmManager.Domain.Interfaces;

/// <summary>
/// Contrato para execução de comandos NVM.
/// </summary>
public interface INvmService
{
    Task<OperationResult<IReadOnlyList<NodeVersion>>> ListVersionsAsync(CancellationToken ct = default);
    Task<OperationResult> InstallVersionAsync(string version, IProgress<string>? progress = null, CancellationToken ct = default);
    Task<OperationResult> UseVersionAsync(string version, CancellationToken ct = default);
    Task<OperationResult> UninstallVersionAsync(string version, CancellationToken ct = default);
    Task<bool> IsNvmInstalledAsync(CancellationToken ct = default);
}

/// <summary>
/// Contrato para instalação do próprio NVM.
/// </summary>
public interface INvmInstaller
{
    Task<OperationResult> InstallNvmAsync(IProgress<string>? progress = null, CancellationToken ct = default);
    Task<string?> GetLatestNvmVersionAsync(CancellationToken ct = default);
}

/// <summary>
/// Abstração para execução de processos do sistema operacional.
/// </summary>
public interface IProcessRunner
{
    Task<ProcessResult> RunAsync(string fileName, string arguments, CancellationToken ct = default);
}

/// <summary>
/// Resultado bruto de execução de processo.
/// </summary>
public sealed record ProcessResult(int ExitCode, string StandardOutput, string StandardError)
{
    public bool Succeeded => ExitCode == 0;
}
