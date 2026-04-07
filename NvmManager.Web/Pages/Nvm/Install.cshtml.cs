using Core.Application.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace NvmManager.Web.Pages.Nvm
{
    public class InstallModel : PageModel
    {
        private readonly NvmApplicationService _app;

        public bool Success { get; private set; }
        public string? Message { get; private set; }
        public string? Output { get; private set; }

        public InstallModel(NvmApplicationService app) => _app = app;

        public void OnGet() { }

        public async Task<IActionResult> OnPostAsync()
        {
            var result = await _app.InstallNvmAsync();
            Success = result.IsSuccess;
            Message = result.Message;
            Output = result.Output;
            return Page();
        }
    }
}
