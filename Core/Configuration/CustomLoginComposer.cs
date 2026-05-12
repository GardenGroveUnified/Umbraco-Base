using Umbraco.Cms.Core.Composing;
using Umbraco.Cms.Core.DependencyInjection;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Authentication.Cookies;

namespace UmbracoBase.Core.Configuration
{
    public class CustomLoginComposer : IComposer
    {
        public void Compose(IUmbracoBuilder builder)
        {
            // Register any additional services if needed
            builder.Services.Configure<Microsoft.AspNetCore.Routing.RouteOptions>(options =>
            {
                options.LowercaseUrls = true;
            });
            
            // Configure backoffice authentication cookie to redirect to custom login page
            builder.Services.Configure<CookieAuthenticationOptions>(Umbraco.Cms.Core.Constants.Security.BackOfficeAuthenticationType, options =>
            {
                options.LoginPath = "/login";
                options.AccessDeniedPath = "/login";
                options.ReturnUrlParameter = "returnUrl";
                
                // Ensure redirects go to our custom login page
                options.Events.OnRedirectToLogin = context =>
                {
                    var returnUrl = context.Request.Path + context.Request.QueryString;
                    var loginUrl = $"/login?returnUrl={Uri.EscapeDataString(returnUrl)}";
                    context.Response.Redirect(loginUrl);
                    return Task.CompletedTask;
                };
                
                options.Events.OnRedirectToAccessDenied = context =>
                {
                    var returnUrl = context.Request.Path + context.Request.QueryString;
                    var loginUrl = $"/login?returnUrl={Uri.EscapeDataString(returnUrl)}";
                    context.Response.Redirect(loginUrl);
                    return Task.CompletedTask;
                };
            });
        }
    }
}
