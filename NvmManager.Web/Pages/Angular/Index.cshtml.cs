using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using NvmManager.Web.Core.Application.Services;
using NvmManager.Web.Extensions;

namespace NvmManager.Web.Pages.Angular
{
    public class IndexModel : PageModel
    {
        [BindProperty]
        public string AngularVersionToInstall { get; set; } = "11.1.4";        

        public string? NodeVersion { get; private set; }
        public string? AngularVersion { get; private set; }

        public bool AngularIncompatible { get; private set; }
        public string? RequiredNodeVersion { get; private set; }

        [BindProperty]
        public string? SuggestedNodeVersion { get; private set; }

        public bool Success { get; private set; } = true;
        public string? Message { get; private set; }


        private readonly AngularApplicationService _angularApp;
        private readonly NvmApplicationService _nvmApp;

        public IndexModel(AngularApplicationService angularApp, NvmApplicationService app)
        {
            _angularApp = angularApp;
            _nvmApp = app;
        }

        public async Task OnGetAsync()
        {
            await LoadAsync();
        }

        public async Task<IActionResult> OnPostInstallAsync()
        {
            var result = await _angularApp.InstallAngularAsync(
                AngularVersionToInstall);

            Success = result.IsSuccess;
            Message = result.Message;

            await LoadAsync();
            return Page();
        }

        public async Task<IActionResult> OnPostRemoveAsync()
        {
            var result = await _angularApp.RemoveAngularAsync();

            Success = result.IsSuccess;
            Message = result.Message;

            await LoadAsync();
            return Page();
        }

        public async Task<IActionResult> OnPostUpdateNodeAsync()
        {
            SuggestedNodeVersion = await _angularApp.GetSuggestedNodeVersionAsync();

            if (string.IsNullOrEmpty(SuggestedNodeVersion))
            {
                Success = false;
                Message = "No suggested Node version found for the current Angular version.";
                await LoadAsync();
                return Page();
            }

            var result = await _angularApp.ReinstallAngularWithNodeAsync(SuggestedNodeVersion);

            Success = result.IsSuccess;
            Message = result.Message;

            await LoadAsync();
            return Page();
        }

        private async Task LoadAsync()
        {
            var ctx = await _angularApp.GetCurrentAngularContextAsync();

            string? nodeVersion = ctx.NodeVersion;

            if (string.IsNullOrEmpty(nodeVersion))
            {
                nodeVersion = await _nvmApp.GetCurrentVersionAsync();
            }

            var cpb = await _angularApp.GetAngularCompatibilityAsync();

            if (cpb.Error == "INCOMPATIBLE_NODE")
            {
                AngularIncompatible = true;
                RequiredNodeVersion = cpb.NodeRequired;
            }

            NodeVersion = nodeVersion.ConcatenateVersion();
            AngularVersion = ctx.AngularVersion.ConcatenateVersion();
            SuggestedNodeVersion = await _angularApp.GetSuggestedNodeVersionAsync();
        }
    }
}
