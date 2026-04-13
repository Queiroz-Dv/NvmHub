using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using NvmManager.Web.Core.Application.Services;
using NvmManager.Web.Core.Domain.Entities;

namespace NvmManager.Web.Pages.Node
{
    public class VersionsModel : PageModel
    {
        private readonly NvmApplicationService _app;
        private readonly AngularApplicationService _angularApp;

        [BindProperty]
        public string Version { get; set; } = "12.11.0";
        public bool IsInstalling { get; private set; }
        
        public List<NodeVersion> Versions { get; private set; } = new();
        public string? PendingRemovalVersion { get; private set; }

        public bool Success { get; private set; } = true;
        public string? Message { get; private set; }

        public VersionsModel(NvmApplicationService app, AngularApplicationService angularApp)
        {
            _app = app;
            _angularApp = angularApp;
        }

        public async Task OnGetAsync() => await LoadAsync();

        public async Task<IActionResult> OnPostRefreshAsync()
        {
            await LoadAsync();
            return Page();
        }      

        public async Task<IActionResult> OnPostInstallAsync()
        {
            IsInstalling = true;
          
            var result = await _app.InstallVersionAsync(Version);

            Success = result.IsSuccess;
            Message = result.Message;

            IsInstalling = false;

            await LoadAsync();
            return Page();
        }

        public async Task<IActionResult> OnPostUseAsync(string version)
        {
            var result = await _app.UseVersionAsync(version);
            Success = result.IsSuccess;
            Message = result.Message;

            await LoadAsync();
            return Page();
        }

        public async Task<IActionResult> OnPostRemoveAsync(string version)
        {
            var result = await _app.UninstallVersionAsync(version);
            Success = result.IsSuccess;
            Message = result.Message;

            await LoadAsync();
            return Page();
        }

        public async Task<IActionResult> OnPostAskRemoveAsync(string version)
        {
            PendingRemovalVersion = version;
            await LoadAsync();
            return Page();
        }

        public async Task<IActionResult> OnPostConfirmRemoveAsync(string version)
        {
            var result = await _app.UninstallVersionAsync(version);
            Success = result.IsSuccess;
            Message = result.Message;

            PendingRemovalVersion = null;
            await LoadAsync();
            return Page();
        }

        public async Task<IActionResult> OnPostCancelRemoveAsync()
        {
            PendingRemovalVersion = null;
            await LoadAsync();
            return Page();
        }


        private async Task LoadAsync()
        {
            var result = await _app.ListVersionsAsync();
            if (!result.IsSuccess)
            {
                Success = false;
                Message = result.Message;
                Versions = new();
                return;
            }

            Success = true;
            Message = result.Message;
            Versions = result.Data?.ToList() ?? new();
        }
    }
}
