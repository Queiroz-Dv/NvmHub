using NvmManager.Domain.Entities;
using NvmManager.Domain.Interfaces;
using NvmManager.Domain.Results;
using System.Text.RegularExpressions;

namespace NvmManager.Infrastructure;

/// <summary>
/// Implementa <see cref="INvmService"/> delegando ao executável nvm.exe do NVM for Windows.
/// </summary>
public sealed class NvmCommandExecutor : INvmService
{
    private readonly IProcessRunner _runner;

    // nvm.exe fica normalmente em %NVM_HOME%\nvm.exe
    private static string NvmExecutable =>
        Path.Combine(
            Environment.GetEnvironmentVariable("NVM_HOME") ?? @"C:\nvm",
            "nvm.exe");

    public NvmCommandExecutor(IProcessRunner runner)
    {
        _runner = runner ?? throw new ArgumentNullException(nameof(runner));
    }

    // ── IsNvmInstalled ────────────────────────────────────────────────────────
    public async Task<bool> IsNvmInstalledAsync(CancellationToken ct = default)
    {
        if (!File.Exists(NvmExecutable)) return false;

        var result = await _runner.RunAsync(NvmExecutable, "version", ct);
        return result.Succeeded;
    }

    // ── List ──────────────────────────────────────────────────────────────────
    public async Task<OperationResult<IReadOnlyList<NodeVersion>>> ListVersionsAsync(CancellationToken ct = default)
    {
        var result = await _runner.RunAsync(NvmExecutable, "ls", ct);
        var rawOutput = result.StandardOutput + result.StandardError;

        if (rawOutput.Contains("No installations", StringComparison.OrdinalIgnoreCase) ||
            rawOutput.Contains("nvm is not installed", StringComparison.OrdinalIgnoreCase) ||
            rawOutput.Trim().Length == 0)
        {
            return OperationResult<IReadOnlyList<NodeVersion>>.Success(
                Array.Empty<NodeVersion>(),
                "Nenhuma versão instalada.");
        }

        var versions = ParseVersionList(rawOutput);
        return OperationResult<IReadOnlyList<NodeVersion>>.Success(
            versions,
            $"{versions.Count} versão(ões) encontrada(s).");
    }

    // ── Install ───────────────────────────────────────────────────────────────
    public async Task<OperationResult> InstallVersionAsync(
        string version,
        IProgress<string>? progress = null,
        CancellationToken ct = default)
    {
        var ver = NormalizeVersion(version);
        progress?.Report($"Iniciando instalação do Node.js v{ver}...");

        var result = await _runner.RunAsync(NvmExecutable, $"install {ver}", ct);
        var output = result.StandardOutput + result.StandardError;

        if (result.Succeeded || output.Contains("installed", StringComparison.OrdinalIgnoreCase))
        {
            progress?.Report($"Node.js v{ver} instalado com sucesso!");
            return OperationResult.Success($"Node.js v{ver} instalado com sucesso!", output);
        }

        return OperationResult.Failure($"Falha ao instalar v{ver}. Verifique o número da versão.", output);
    }

    // ── Use ───────────────────────────────────────────────────────────────────
    public async Task<OperationResult> UseVersionAsync(string version, CancellationToken ct = default)
    {
        var ver = NormalizeVersion(version);
        var result = await _runner.RunAsync(NvmExecutable, $"use {ver}", ct);
        var output = result.StandardOutput + result.StandardError;

        if (result.Succeeded || output.Contains("Now using", StringComparison.OrdinalIgnoreCase))
            return OperationResult.Success($"Agora usando Node.js v{ver}.", output);

        return OperationResult.Failure($"Não foi possível usar a versão v{ver}. Ela está instalada?", output);
    }

    // ── Uninstall ─────────────────────────────────────────────────────────────
    public async Task<OperationResult> UninstallVersionAsync(string version, CancellationToken ct = default)
    {
        var ver = NormalizeVersion(version);
        var result = await _runner.RunAsync(NvmExecutable, $"uninstall {ver}", ct);
        var output = result.StandardOutput + result.StandardError;

        if (result.Succeeded || output.Contains("Uninstalling", StringComparison.OrdinalIgnoreCase))
            return OperationResult.Success($"Node.js v{ver} desinstalado com sucesso!", output);

        return OperationResult.Failure($"Não foi possível desinstalar v{ver}.", output);
    }

    // ── Helpers ───────────────────────────────────────────────────────────────
    private static string NormalizeVersion(string version)
        => version.Trim().TrimStart('v', 'V');

    private static IReadOnlyList<NodeVersion> ParseVersionList(string raw)
    {
        var versionPattern = new Regex(
            @"\*?\s*v?([\d]+\.[\d]+\.[\d]+)(?:\s*([\w-]+))?",
            RegexOptions.Multiline | RegexOptions.IgnoreCase);

        var versions = new List<NodeVersion>();
        foreach (Match match in versionPattern.Matches(raw))
        {
            var versionString = match.Groups[1].Value;
            var isActive      = match.Value.TrimStart().StartsWith('*');
            var label         = match.Groups[2].Value;
            var isLts         = label.Contains("lts", StringComparison.OrdinalIgnoreCase);

            versions.Add(new NodeVersion(versionString, isActive, isLts));
        }

        return versions;
    }
}
