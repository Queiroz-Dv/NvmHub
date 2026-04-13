using NvmManager.Web.Core.Domain.Entities;
using NvmManager.Web.Core.Domain.Interfaces;
using NvmManager.Web.Core.Domain.Results;
using System.Text.RegularExpressions;

namespace NvmManager.Web.Core.Infrastructure;

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

    public async Task<string?> GetNvmVersionAsync(CancellationToken ct = default)
    {
        var result = await _runner.RunAsync(NvmExecutable, "version", ct);
        return result.Succeeded
            ? result.StandardOutput.Trim()
            : null;
    }


    // ── List ──────────────────────────────────────────────────────────────────
    public async Task<OperationResult<IReadOnlyList<NodeVersion>>> ListVersionsAsync(CancellationToken ct = default)
    {
        var result = await _runner.RunAsync(NvmExecutable, "list", ct);
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
        return OperationResult<IReadOnlyList<NodeVersion>>.Success(versions);
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

    // ── Current version ───────────────────────────────────────────────────────
    public async Task<string?> GetCurrentVersionAsync(CancellationToken ct = default)
    {
        // Tenta via nvm current
        var result = await _runner.RunAsync(NvmExecutable, "current", ct);
        var raw = (result.StandardOutput + result.StandardError).Trim();

        if (!string.IsNullOrEmpty(raw) &&
            !raw.Contains("No current", StringComparison.OrdinalIgnoreCase) &&
            !raw.Contains("none", StringComparison.OrdinalIgnoreCase))
        {
            return raw.TrimStart('v');
        }

        // Fallback: node --version
        var nodeResult = await _runner.RunAsync("node", "--version", ct);
        var nodeRaw = nodeResult.StandardOutput.Trim();
        return nodeRaw.StartsWith("v", StringComparison.OrdinalIgnoreCase)
            ? nodeRaw.TrimStart('v')
            : null;
    }

    // ── Helpers ───────────────────────────────────────────────────────────────
    private static string NormalizeVersion(string version)
        => version.Trim().TrimStart('v', 'V');

    private static IReadOnlyList<NodeVersion> ParseVersionList(string raw)
    {
        var versions = new List<NodeVersion>();

        var lines = raw
            .Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)
            .Select(l => l.Trim());

        foreach (var line in lines)
        {
            // Exemplo de linhas válidas:
            // "* 12.11.0 (64-bit)"
            // "  18.16.1"
            // "  v20.11.1"

            var isActive = line.StartsWith("*");

            // Remove o marcador de ativo e o 'v'
            var clean = line
                .TrimStart('*')
                .Trim()
                .TrimStart('v', 'V');

            // Extrai apenas X.Y.Z
            var match = Regex.Match(clean, @"\d+\.\d+\.\d+");
            if (!match.Success)
                continue;

            var version = match.Value;

            // LTS no Windows geralmente não vem explícito
            var isLts = false;

            versions.Add(new NodeVersion(version, isActive, isLts));
        }

        return versions;
    }

    public async Task<OperationResult<IEnumerable<string>>> ListRemoteVersionsAsync(CancellationToken ct = default)
    {
        var result = await _runner.RunAsync(NvmExecutable, "list available", ct);
        var raw = (result.StandardOutput + result.StandardError).Trim();

        if (!result.Succeeded || string.IsNullOrWhiteSpace(raw))
            return OperationResult<IEnumerable<string>>.Failure(
                "Não foi possível obter versões disponíveis do Node.js.");

        var versions = new HashSet<string>();

        var lines = raw
            .Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)
            .Select(l => l.Trim());

        foreach (var line in lines)
        {
            // Exemplos válidos no output do nvm for windows:
            // |   18.19.1
            // |   v20.11.1
            // |   16.20.2

            var match = Regex.Match(line, @"\b(v?\d+\.\d+\.\d+)\b");
            if (!match.Success)
                continue;

            var version = match.Value.TrimStart('v', 'V');
            versions.Add(version);
        }

        return OperationResult<IEnumerable<string>>.Success(
            versions.OrderByDescending(v => Version.Parse(v)).ToList());
    }
}
