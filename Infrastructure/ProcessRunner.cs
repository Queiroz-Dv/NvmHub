using System.Diagnostics;
using System.Text;
using NvmManager.Domain.Interfaces;

namespace NvmManager.Infrastructure;

/// <summary>
/// Executa processos do sistema operacional de forma assíncrona,
/// capturando stdout e stderr sem bloquear a UI.
/// </summary>
public sealed class ProcessRunner : IProcessRunner
{
    public async Task<ProcessResult> RunAsync(
        string fileName,
        string arguments,
        CancellationToken ct = default)
    {
        var startInfo = new ProcessStartInfo
        {
            FileName               = fileName,
            Arguments              = arguments,
            RedirectStandardOutput = true,
            RedirectStandardError  = true,
            UseShellExecute        = false,
            CreateNoWindow         = true,
            StandardOutputEncoding = Encoding.UTF8,
            StandardErrorEncoding  = Encoding.UTF8,
        };

        // Garante que o PATH inclua onde o nvm fica instalado no Windows
        var path = Environment.GetEnvironmentVariable("PATH") ?? string.Empty;
        var nvmHome = Environment.GetEnvironmentVariable("NVM_HOME") ?? string.Empty;
        if (!string.IsNullOrEmpty(nvmHome) && !path.Contains(nvmHome, StringComparison.OrdinalIgnoreCase))
            startInfo.EnvironmentVariables["PATH"] = $"{nvmHome};{path}";

        using var process = new Process { StartInfo = startInfo, EnableRaisingEvents = true };

        var stdoutBuilder = new StringBuilder();
        var stderrBuilder = new StringBuilder();

        var tcs = new TaskCompletionSource<int>(TaskCreationOptions.RunContinuationsAsynchronously);

        process.OutputDataReceived += (_, e) => { if (e.Data is not null) stdoutBuilder.AppendLine(e.Data); };
        process.ErrorDataReceived  += (_, e) => { if (e.Data is not null) stderrBuilder.AppendLine(e.Data); };
        process.Exited             += (_, _) => tcs.TrySetResult(process.ExitCode);

        process.Start();
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();

        await using (ct.Register(() => { try { process.Kill(); } catch { /* ignore */ } }))
        {
            await tcs.Task.ConfigureAwait(false);
        }

        return new ProcessResult(
            process.ExitCode,
            stdoutBuilder.ToString().Trim(),
            stderrBuilder.ToString().Trim());
    }
}
