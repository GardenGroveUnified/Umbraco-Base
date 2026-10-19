using Microsoft.Extensions.DependencyInjection;
using Umbraco.Cms.Core.Composing;
using Umbraco.Cms.Core.DependencyInjection;
using Umbraco.Cms.Core.Security;
using UmbracoBase.Core.Security;

namespace UmbracoBase.Core.Composers
{
    public class ActiveDirectoryComposer : IComposer
    {
        public void Compose(IUmbracoBuilder builder)
        {
            builder.Services.AddScoped<IBackOfficeUserPasswordChecker, BackOfficeUserPasswordChecker>();
        }
    }
}
