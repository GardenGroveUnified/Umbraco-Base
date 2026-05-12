using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Umbraco.Cms.Web.Common.Security;

namespace UmbracoBase.Core.Controllers
{
    [AllowAnonymous]
    public class LogoutController : Controller
    {
        private readonly IBackOfficeSignInManager _signInManager;
        private readonly ILogger<LogoutController> _logger;

        public LogoutController(
            IBackOfficeSignInManager signInManager,
            ILogger<LogoutController> logger)
        {
            _signInManager = signInManager;
            _logger = logger;
        }

        [HttpGet]
        [HttpPost]
        [Route("logout")]
        public async Task<IActionResult> Index()
        {
            try
            {
                await _signInManager.SignOutAsync();
                _logger.LogInformation("User logged out successfully");
                
                // Redirect to /admin which will show the login page
                return Redirect("/admin");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during logout");
                // Even if there's an error, redirect to admin page
                return Redirect("/admin");
            }
        }
    }
}
