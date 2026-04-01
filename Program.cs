using Microsoft.Extensions.DependencyInjection;
using NvmManager.Application.Services;
using NvmManager.Domain.Interfaces;
using NvmManager.Infrastructure;
using NvmManager.Presentation.Forms;

public static class Program
{
    [STAThread]
    private static void Main()
    {
        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);
        Application.SetHighDpiMode(HighDpiMode.PerMonitorV2);

        // ─ Composição de dependências (Poor Man's DI via MS.Extensions.DI) ─
        using var services = BuildServiceProvider();

        var mainForm = services.GetRequiredService<MainForm>();
        Application.Run(mainForm);
    }

    private static ServiceProvider BuildServiceProvider()
    {
        var services = new ServiceCollection();

        // Infrastructure
        services.AddSingleton<IProcessRunner, ProcessRunner>();
        services.AddSingleton<INvmService, NvmCommandExecutor>();

        // Fix: register the concrete typed client, then map the interface to it
        // Ensure AddHttpClient extension methods are available by referencing Microsoft.Extensions.Http
        services.AddHttpClient<NvmWindowsInstaller>();
        services.AddTransient<INvmInstaller, NvmWindowsInstaller>();

        // Application
        services.AddSingleton<NvmApplicationService>();

        // Presentation
        services.AddTransient<MainForm>();

        return services.BuildServiceProvider();
    }
}