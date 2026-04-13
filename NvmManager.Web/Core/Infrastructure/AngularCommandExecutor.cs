using NvmManager.Web.Core.Domain.Interfaces;
using NvmManager.Web.Core.Domain.Results;
using System.Text.Json;

namespace NvmManager.Web.Core.Infrastructure
{
    public sealed class AngularCommandExecutor
    {
        private readonly IProcessRunner _runner;

        public AngularCommandExecutor(IProcessRunner runner)
        {
            _runner = runner;
        }

        public async Task<string?> GetLatestAngularVersionAsync(CancellationToken ct = default)
        {
            var result = await _runner.RunAsync("npm", "view @angular/cli version", ct);

            if (!result.Succeeded)
                return null;

            return result.StandardOutput.Trim();
        }

        public string? GetAngularVersionFromPackageJson(string nodeVersion)
        {
            try
            {
                var basePath = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                    "nvm",
                    $"v{nodeVersion}",
                    "node_modules",
                    "@angular",
                    "cli",
                    "package.json");

                if (!File.Exists(basePath))
                    return null;

                using var stream = File.OpenRead(basePath);
                using var doc = JsonDocument.Parse(stream);

                if (doc.RootElement.TryGetProperty("version", out var version))
                    return version.GetString();

                return null;
            }
            catch
            {
                return null;
            }
        }

        public string? GetInstalledAngularVersionFromDisk(string nodeVersion)
        {
            try
            {
                var basePath = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                    "nvm",
                    $"v{nodeVersion}",
                    "node_modules",
                    "@angular",
                    "cli",
                    "package.json");

                if (!File.Exists(basePath))
                    return null;

                var json = File.ReadAllText(basePath);
                using var doc = JsonDocument.Parse(json);

                return doc.RootElement
                          .GetProperty("version")
                          .GetString();
            }
            catch
            {
                return null;
            }
        }

        public async Task<(string? AngularVersion, string? NodeVersion)> GetAngularAndNodeVersionAsync(CancellationToken ct = default)
        {
            // 1. Descobre o Node ativo via NVM
            var nodeResult = await _runner.RunAsync("nvm", "current", ct);
            var nodeRaw = (nodeResult.StandardOutput + nodeResult.StandardError).Trim();

            if (string.IsNullOrEmpty(nodeRaw) ||
                nodeRaw.Contains("No current", StringComparison.OrdinalIgnoreCase) ||
                nodeRaw.Contains("none", StringComparison.OrdinalIgnoreCase))
            {
                return (null, null);
            }

            var nodeVersion = nodeRaw.TrimStart('v');

            // 2. Lê a versão do Angular diretamente do package.json
            var angularVersion = GetAngularVersionFromPackageJson(nodeVersion);

            return (angularVersion, nodeVersion);
        }

        public async Task<OperationResult> InstallAngularAsync(string version, CancellationToken ct = default)
        {
            var args = $"install -g @angular/cli@{version}".Trim();

            var result = await _runner.RunAsync("npm", args, ct);
            var output = (result.StandardOutput ?? "") + (result.StandardError ?? "");

            if (result.Succeeded)
                return OperationResult.Success(
                    $"Angular CLI v{version} instalado com sucesso.",
                    output);

            return OperationResult.Failure(
                $"Falha ao instalar Angular CLI v{version}.",
                output);
        }

        public async Task<OperationResult> RemoveAngularAsync(CancellationToken ct = default)
        {
            var result = await _runner.RunAsync("npm", "uninstall -g @angular/cli", ct);

            var output = (result.StandardOutput ?? "") + (result.StandardError ?? "");

            if (result.Succeeded)
                return OperationResult.Success("Angular CLI removido com sucesso.", output);

            return OperationResult.Failure("Falha ao remover Angular CLI.", output);
        }

        public async Task<(string? AngularVersion, string? NodeRequired, string? Error)> TryGetAngularContextAsync(CancellationToken ct = default)
        {
            var result = await _runner.RunAsync("ng", "version", ct);

            var output = (result.StandardOutput ?? "") + (result.StandardError ?? "");

            if (!result.Succeeded)
            {
                // Detecta erro de versão mínima do Node
                if (output.Contains("requires a minimum Node.js version", StringComparison.OrdinalIgnoreCase))
                {
                    var required = ExtractAfter(output, "minimum Node.js version of");

                    required = required?.Replace("v", string.Empty).Trim();

                    return (null, required, "INCOMPATIBLE_NODE");
                }

                return (null, null, "NOT_INSTALLED");
            }

            string? angular = null;
            string? node = null;

            foreach (var line in output.Split('\n'))
            {
                var t = line.Trim();

                if (t.StartsWith("Angular CLI:", StringComparison.OrdinalIgnoreCase))
                    angular = t.Replace("Angular CLI:", "").Trim();

                if (t.StartsWith("Node:", StringComparison.OrdinalIgnoreCase))
                    node = t.Replace("Node:", "").Trim();
            }

            return (angular, node, null);
        }

        private static string? ExtractAfter(string text, string marker)
        {
            var idx = text.IndexOf(marker, StringComparison.OrdinalIgnoreCase);
            if (idx == -1) return null;

            return text.Substring(idx + marker.Length)
                       .Split('\n')
                       .FirstOrDefault()?
                       .Trim();
        }

    }
}
