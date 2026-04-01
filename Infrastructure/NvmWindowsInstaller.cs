using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using NvmManager.Domain.Interfaces;
using NvmManager.Domain.Results;

namespace NvmManager.Infrastructure;

/// <summary>
/// Faz o download e instalação do NVM for Windows a partir do GitHub Releases.
/// </summary>
public sealed class NvmWindowsInstaller : INvmInstaller
{
    private const string GitHubApiUrl    = "https://api.github.com/repos/coreybutler/nvm-windows/releases/latest";
    private const string SetupAssetName  = "nvm-setup.exe";

    private readonly HttpClient _httpClient;

    public NvmWindowsInstaller(HttpClient httpClient)
    {
        _httpClient = httpClient;
        _httpClient.DefaultRequestHeaders.Add("User-Agent", "NvmManager/1.0");
    }

    public async Task<string?> GetLatestNvmVersionAsync(CancellationToken ct = default)
    {
        try
        {
            using var response = await _httpClient.GetAsync(GitHubApiUrl, ct);
            response.EnsureSuccessStatusCode();

            using var doc = await JsonDocument.ParseAsync(
                await response.Content.ReadAsStreamAsync(ct), cancellationToken: ct);

            return doc.RootElement.GetProperty("tag_name").GetString();
        }
        catch
        {
            return null;
        }
    }

    public async Task<OperationResult> InstallNvmAsync(
        IProgress<string>? progress = null,
        CancellationToken ct = default)
    {
        try
        {
            // 1. Busca a release mais recente
            progress?.Report("Consultando a versão mais recente do NVM for Windows no GitHub...");
            using var latestResponse = await _httpClient.GetAsync(GitHubApiUrl, ct);
            latestResponse.EnsureSuccessStatusCode();

            using var doc = await JsonDocument.ParseAsync(
                await latestResponse.Content.ReadAsStreamAsync(ct), cancellationToken: ct);

            var tagName = doc.RootElement.GetProperty("tag_name").GetString() ?? "latest";
            progress?.Report($"Versão encontrada: {tagName}");

            // 2. Localiza o asset nvm-setup.exe
            string? downloadUrl = null;
            foreach (var asset in doc.RootElement.GetProperty("assets").EnumerateArray())
            {
                var name = asset.GetProperty("name").GetString() ?? string.Empty;
                if (name.Equals(SetupAssetName, StringComparison.OrdinalIgnoreCase))
                {
                    downloadUrl = asset.GetProperty("browser_download_url").GetString();
                    break;
                }
            }

            if (downloadUrl is null)
                return OperationResult.Failure("Não foi possível localizar o instalador nvm-setup.exe na release.");

            // 3. Faz o download do setup
            progress?.Report($"Baixando {SetupAssetName}...");
            var tempPath = Path.Combine(Path.GetTempPath(), SetupAssetName);

            using (var downloadStream = await _httpClient.GetStreamAsync(downloadUrl, ct))
            using (var fileStream    = new FileStream(tempPath, FileMode.Create, FileAccess.Write, FileShare.None))
            {
                await downloadStream.CopyToAsync(fileStream, ct);
            }

            // 4. Executa o instalador
            progress?.Report("Executando o instalador do NVM for Windows...");
            var runner = new ProcessRunner();
            var result = await runner.RunAsync(tempPath, "/silent", ct);

            // Aguarda propagação de variáveis de ambiente
            await Task.Delay(2000, ct);

            if (result.Succeeded || File.Exists(@"C:\nvm\nvm.exe"))
            {
                // Tenta limpar o arquivo temporário
                try { File.Delete(tempPath); } catch { /* best-effort */ }

                progress?.Report("NVM for Windows instalado com sucesso! Reinicie o terminal para aplicar as variáveis de ambiente.");
                return OperationResult.Success(
                    "NVM for Windows instalado com sucesso!",
                    result.StandardOutput);
            }

            return OperationResult.Failure(
                "O instalador foi executado, mas não foi possível verificar a instalação.",
                result.StandardError);
        }
        catch (OperationCanceledException)
        {
            return OperationResult.Failure("Operação cancelada pelo usuário.");
        }
        catch (Exception ex)
        {
            return OperationResult.Failure($"Erro ao instalar o NVM: {ex.Message}");
        }
    }
}
