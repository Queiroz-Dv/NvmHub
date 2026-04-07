using Core.Application.Services;
using Core.Domain.Results;
using NvmManager.Core.Infrastructure;

namespace NvmManager.Core.Application.Services
{
    public sealed class AngularApplicationService
    {
        private readonly AngularCommandExecutor _executor;
        private readonly NvmApplicationService _nvmApp;

        public AngularApplicationService(AngularCommandExecutor executor, NvmApplicationService nvmApp)
        {
            _executor = executor;
            _nvmApp = nvmApp;
        }

        public async Task<(string? AngularVersion, string? NodeVersion)> GetCurrentAngularContextAsync(CancellationToken ct = default)
            => await _executor.GetAngularAndNodeVersionAsync(ct);

        public Task<OperationResult> InstallAngularAsync(string version, CancellationToken ct = default)
        {
            return _executor.InstallAngularAsync(version, ct);
        }

        public Task<OperationResult> RemoveAngularAsync(CancellationToken ct = default)
        {
            return _executor.RemoveAngularAsync(ct);
        }

        public Task<(string? AngularVersion, string? NodeRequired, string? Error)>
            GetAngularCompatibilityAsync(CancellationToken ct = default)
        {
            return _executor.TryGetAngularContextAsync(ct);
        }

        public async Task<OperationResult> InstallStableAngularAsync(CancellationToken ct = default)
        {
            var node = await _nvmApp.GetCurrentVersionAsync(ct);
            if (string.IsNullOrEmpty(node))
                return OperationResult.Failure("Nenhuma versão do Node ativa.");

            var latest = await _executor.GetLatestAngularVersionAsync(ct);
            if (string.IsNullOrEmpty(latest))
                return OperationResult.Failure("Não foi possível obter a versão estável do Angular.");

            var result = await _executor.InstallAngularAsync(latest, ct);

            if (!result.IsSuccess)
                return OperationResult.Failure(
                    $"Falha ao instalar Angular estável v{latest}.",
                    result.Message);

            return OperationResult.Success(
                $"Angular CLI estável v{latest} instalado com sucesso.");
        }


        public async Task<string?> GetSuggestedNodeVersionAsync(CancellationToken ct = default)
        {
            var ctx = await _executor.TryGetAngularContextAsync(ct);

            if (ctx.Error != "INCOMPATIBLE_NODE" || string.IsNullOrEmpty(ctx.NodeRequired))
                return null;

            // Angular informa apenas X.Y → inferimos X.Y.0
            var required = ctx.NodeRequired.Trim().TrimStart('v');

            if (required != null)
                return required + "0";

            return null;
        }

        public async Task<string?> GetInstalledAngularVersionSafeAsync(CancellationToken ct = default)
        {
            var node = await _nvmApp.GetCurrentVersionAsync(ct);
            if (string.IsNullOrEmpty(node))
                return null;

            return _executor.GetInstalledAngularVersionFromDisk(node);
        }

        public async Task<OperationResult> ReinstallAngularWithNodeAsync(string targetNodeVersion, CancellationToken ct = default)
        {
            // 1. Descobre a versão real do Angular
            var angularVersion = await GetInstalledAngularVersionSafeAsync(ct);

            if (string.IsNullOrEmpty(angularVersion))
                return OperationResult.Failure(
                    "Não foi possível identificar a versão do Angular instalada.");

            // 2. Remove Angular do Node atual
            await _executor.RemoveAngularAsync(ct);

            // 3. Instala e ativa o novo Node
            await _nvmApp.InstallVersionAsync(targetNodeVersion, ct: ct);
            await _nvmApp.UseVersionAsync(targetNodeVersion, ct);

            // 4. Reinstala o Angular correto
            var installResult = await _executor.InstallAngularAsync(angularVersion, ct);

            if (!installResult.IsSuccess)
                return OperationResult.Failure(
                    $"Node atualizado, mas falha ao reinstalar Angular v{angularVersion}.",
                    installResult.Message);

            return OperationResult.Success(
                $"Node atualizado para v{targetNodeVersion} e Angular v{angularVersion} reinstalado com sucesso.");
        }

    }
}
