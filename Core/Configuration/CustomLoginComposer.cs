using Umbraco.Cms.Core.Composing;
using Umbraco.Cms.Core.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;

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

            // Note: the backoffice cookie's LoginPath/OnRedirectToLogin used to be overridden here
            // to point at a custom "/login" page. Umbraco 14+ authenticates the backoffice SPA via
            // OpenIddict OAuth, not this cookie's redirect events, so overriding it caused an
            // infinite redirect loop between the SPA and "/login". Removed in favor of Umbraco's
            // built-in backoffice login.
        }
    }
}
