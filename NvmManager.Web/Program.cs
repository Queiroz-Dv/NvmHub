using Core.Application.Services;
using Core.Domain.Interfaces;
using Core.Infrastructure;
using NvmManager.Core.Application.Services;
using NvmManager.Core.Infrastructure;

namespace NvmManager.Web
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // URL fixa (o WPF vai abrir isso no WebView2)
            var baseUrl = builder.Configuration["Host:BaseUrl"] ?? "http://127.0.0.1:5123";
            builder.WebHost.UseUrls(baseUrl);

            // Add services to the container.
            builder.Services.AddRazorPages();

            // Infra / App
            builder.Services.AddSingleton<IProcessRunner, ProcessRunner>();
            builder.Services.AddSingleton<INvmService, NvmCommandExecutor>();
            builder.Services.AddSingleton<AngularCommandExecutor>();

            builder.Services.AddHttpClient();
            builder.Services.AddSingleton<INvmInstaller>(sp =>
            {
                var factory = sp.GetRequiredService<IHttpClientFactory>();
                return new NvmWindowsInstaller(factory.CreateClient());
            });

            // Cache + Application Service
            builder.Services.AddSingleton<VersionCacheService>();
            builder.Services.AddSingleton<NvmApplicationService>();
            builder.Services.AddSingleton<AngularApplicationService>();

            var app = builder.Build();

            app.MapGet("/health", () => Results.Ok("ok"));

            // Configure the HTTP request pipeline.
            if (!app.Environment.IsDevelopment())
            {
                app.UseExceptionHandler("/Error");
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }

            app.UseHttpsRedirection();

            app.UseRouting();

            app.UseAuthorization();

            app.MapStaticAssets();
            app.MapRazorPages()
               .WithStaticAssets();

            app.Run();
        }
    }
}
