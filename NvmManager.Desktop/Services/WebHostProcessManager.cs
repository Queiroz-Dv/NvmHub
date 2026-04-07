using System;
using System.Diagnostics;
using System.IO;

namespace NvmManager.Desktop.Services;

public sealed class WebHostProcessManager
{
    private readonly string _exeName;
    private Process? _process;

    public WebHostProcessManager(string exeName)
    {
        _exeName = exeName;
    }

    public void StartWebHost()
    {
        if (_process is not null && !_process.HasExited)
            return;

        var exePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, _exeName);

        if (!File.Exists(exePath))
            throw new FileNotFoundException($"Não foi possível localizar {exePath}");

        var psi = new ProcessStartInfo
        {
            FileName = exePath,
            UseShellExecute = false,
            CreateNoWindow = true,
        };

        _process = Process.Start(psi)
            ?? throw new Exception("Falha ao iniciar o processo web.");
    }

    public void StopWebHost()
    {
        try
        {
            if (_process is not null && !_process.HasExited)
            {
                _process.Kill(true);
                _process.Dispose();
            }
        }
        catch { }
    }
}