using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Umbraco.Cms.Core.Security;

namespace UmbracoBase.Core.Controllers
{
    [AllowAnonymous]
    public class AdminController : Controller
    {
        private readonly IBackOfficeSecurity _backOfficeSecurity;
        private readonly ILogger<AdminController> _logger;

        public AdminController(
            IBackOfficeSecurity backOfficeSecurity,
            ILogger<AdminController> logger)
        {
            _backOfficeSecurity = backOfficeSecurity;
            _logger = logger;
        }

        [HttpGet]
        [Route("admin")]
        public IActionResult Index()
        {
            try
            {
                // Check if user is authenticated and approved
                var currentUser = _backOfficeSecurity.CurrentUser;
                if (currentUser != null && currentUser.IsApproved)
                {
                    _logger.LogInformation("User is already authenticated, redirecting to backoffice");
                    // User is already logged in, redirect to Umbraco backoffice
                    return Redirect("/umbraco");
                }
                else
                {
                    _logger.LogInformation("User not authenticated, redirecting to custom login");
                    // User is not logged in, redirect to custom login page
                    return RedirectToAction("Login", "CustomBackOfficeLogin");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking authentication status");
                // If there's any error checking authentication, redirect to login to be safe
                return RedirectToAction("Login", "CustomBackOfficeLogin");
            }
        }
    }
}
