using System.Diagnostics;
using System.Text;
using NvmManager.Web.Core.Domain.Interfaces;

namespace NvmManager.Web.Core.Infrastructure;

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
        static string? ResolveExecutable(string name)
        {
            var path = Environment.GetEnvironmentVariable("PATH") ?? "";
            var pathext = (Environment.GetEnvironmentVariable("PATHEXT") ?? ".EXE;.CMD;.BAT").Split(';');
            foreach (var dir in path.Split(Path.PathSeparator))
            {
                if (string.IsNullOrWhiteSpace(dir)) continue;
                foreach (var ext in pathext)
                {
                    var candidate = Path.Combine(dir, name + ext);
                    if (File.Exists(candidate)) return candidate;
                }
            }
            return null;
        }

        var exe = ResolveExecutable(fileName) ?? "cmd.exe";

        var startInfo = new ProcessStartInfo
        {
            FileName               = exe,
            Arguments              = exe == "cmd.exe" ? $"/c {fileName} {arguments}" : arguments,
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
