using Core.Application.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using NvmManager.Core.Application.Services;
using NvmManager.Web.Extensions;

namespace NvmManager.Web.Pages
{
    public class IndexModel : PageModel
    {
        private readonly NvmApplicationService _app;
        private readonly AngularApplicationService _angularApp;

        public string? AngularVersion { get; private set; }
        public string? AngularNodeVersion { get; private set; }


        public bool Success { get; private set; } = true;
        public string? Message { get; private set; }

        public bool IsNvmInstalled { get; private set; }
        public string? CurrentNodeVersion { get; private set; }
        public string? NvmVersion { get; private set; }       

        public IndexModel(NvmApplicationService app, AngularApplicationService angularApp)
        {
            _app = app;
            _angularApp = angularApp;
        }

        public async Task OnGetAsync()
        {
            IsNvmInstalled = await _app.IsNvmInstalledAsync();

            if (!IsNvmInstalled)
            {
                NvmVersion = null;
                CurrentNodeVersion = null;
                AngularVersion = null;
                return;
            }

            NvmVersion = await _app.GetNvmVersionAsync();

            // Node atual
            var nodeVersion = await _app.GetCurrentVersionAsync();
            CurrentNodeVersion = nodeVersion?.ConcatenateVersion();

            // Angular associado ao Node atual
            var angularVersion = await _angularApp.GetInstalledAngularVersionSafeAsync();
            AngularVersion = angularVersion?.ConcatenateVersion();
        }

        public async Task<IActionResult> OnPostInstallStableNodeAsync()
        {
            var result = await _app.InstallStableNodeAsync();

            Success = result.IsSuccess;
            Message = result.Message;

            await OnGetAsync();
            return Page();
        }

        public async Task<IActionResult> OnPostInstallStableAngularAsync()
        {
            var result = await _angularApp.InstallStableAngularAsync();

            Success = result.IsSuccess;
            Message = result.Message;

            await OnGetAsync();
            return Page();
        }
    }
}
