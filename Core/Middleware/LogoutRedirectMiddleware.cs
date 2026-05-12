using Microsoft.AspNetCore.Http;

namespace UmbracoBase.Core.Middleware
{
    public class LogoutRedirectMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<LogoutRedirectMiddleware> _logger;

        public LogoutRedirectMiddleware(RequestDelegate next, ILogger<LogoutRedirectMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            // Check if this is a logout request to the default Umbraco logout endpoint
            if (context.Request.Path.StartsWithSegments("/umbraco/logout", StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogInformation("Intercepting Umbraco logout request, redirecting to custom logout");
                
                // Redirect to our custom logout endpoint
                context.Response.Redirect("/logout");
                return;
            }

            // Continue with the next middleware
            await _next(context);
        }
    }
}
