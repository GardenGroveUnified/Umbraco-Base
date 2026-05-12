using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Authentication;

namespace UmbracoBase.Core.Middleware
{
    public class SessionTimeoutRedirectMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<SessionTimeoutRedirectMiddleware> _logger;

        public SessionTimeoutRedirectMiddleware(RequestDelegate next, ILogger<SessionTimeoutRedirectMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            // Let the request proceed first
            await _next(context);

            // After the request has been processed, check if we got a 401 Unauthorized response
            // This happens when session times out and user tries to access backoffice
            if (context.Response.StatusCode == 401 && 
                context.Request.Path.StartsWithSegments("/umbraco", StringComparison.OrdinalIgnoreCase))
            {
                // Check if this is not already a login/logout request
                if (!context.Request.Path.StartsWithSegments("/umbraco/login", StringComparison.OrdinalIgnoreCase) &&
                    !context.Request.Path.StartsWithSegments("/umbraco/logout", StringComparison.OrdinalIgnoreCase))
                {
                    _logger.LogInformation("Session timeout detected (401 response) for backoffice access, redirecting to custom login page");
                    
                    // Build return URL with the original request path
                    var returnUrl = context.Request.Path + context.Request.QueryString;
                    var loginUrl = $"/login?returnUrl={Uri.EscapeDataString(returnUrl)}";
                    
                    // Clear the 401 status and redirect
                    context.Response.Clear();
                    context.Response.StatusCode = 302;
                    context.Response.Headers.Location = loginUrl;
                }
            }
        }
    }
}
