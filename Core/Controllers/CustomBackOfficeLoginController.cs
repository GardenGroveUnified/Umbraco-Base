using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using UmbracoBase.Core.ViewModels;
using Umbraco.Cms.Web.Common.Security;
using Umbraco.Cms.Core.Security;
namespace UmbracoBase.Core.Controllers
{
    [AllowAnonymous]
    public class CustomBackOfficeLoginController : Controller
    {
        private readonly IBackOfficeSignInManager _signInManager;
        private readonly ILogger<CustomBackOfficeLoginController> _logger;
        private readonly IBackOfficeSecurity _backOfficeSecurity;

        public CustomBackOfficeLoginController(
            IBackOfficeSignInManager signInManager,
            ILogger<CustomBackOfficeLoginController> logger,
            IBackOfficeSecurity backOfficeSecurity)
        {
            _signInManager = signInManager;
            _logger = logger;
            _backOfficeSecurity = backOfficeSecurity;
        }

        [HttpGet]
        public IActionResult Login(string? returnUrl = null)
        {
            try
            {
                // Check if user is authenticated and approved
                var currentUser = _backOfficeSecurity.CurrentUser;
                if (currentUser != null && currentUser.IsApproved)
                {
                    _logger.LogInformation("User is already authenticated, redirecting to backoffice");
                    return Redirect("/umbraco"); // Already logged in
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error checking authentication status in login page");
                // Continue to show login page if there's an error
            }
            ViewData["ReturnUrl"] = returnUrl;
            ViewData["Title"] = "GGUSD - Admin Login"; // Custom title
            
            var model = new BackOfficeLoginViewModel
            {
                ReturnUrl = returnUrl
            };
            
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(BackOfficeLoginViewModel model)
        {
            ViewData["Title"] = "GGUSD - Admin Login"; // Custom title
            
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            try
            {
                var result = await _signInManager.PasswordSignInAsync(
                    model.Username, 
                    model.Password, 
                    model.RememberMe, 
                    lockoutOnFailure: true);

                if (result.Succeeded)
                {
                    _logger.LogInformation("User {Username} logged in.", model.Username);
                    
                    if (!string.IsNullOrEmpty(model.ReturnUrl) && Url.IsLocalUrl(model.ReturnUrl))
                    {
                        return Redirect(model.ReturnUrl);
                    }
                    
                    // Redirect to Umbraco backoffice
                    return Redirect("/umbraco");
                }
                
                if (result.IsLockedOut)
                {
                    _logger.LogWarning("User {Username} account locked out.", model.Username);
                    ModelState.AddModelError(string.Empty, "Account locked out.");
                    return View(model);
                }
                
                ModelState.AddModelError(string.Empty, "Invalid username or password.");
                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during login attempt for user {Username}", model.Username);
                ModelState.AddModelError(string.Empty, "An error occurred during login. Please try again.");
                return View(model);
            }
        }

    }
}
