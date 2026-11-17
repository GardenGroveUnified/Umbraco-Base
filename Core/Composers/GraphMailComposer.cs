using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Umbraco.Cms.Core.Composing;
using Umbraco.Cms.Core.DependencyInjection;
using Umbraco.Cms.Core.Mail;
using Umbraco.Extensions;
using UmbracoBase.Core.Email;

namespace UmbracoBase.Core.Composers
{
    /// <summary>
    /// Swaps Umbraco's SMTP email sender for GraphEmailSender when the
    /// "GraphMail" appsettings section is filled in (live server). With the
    /// section absent, nothing changes and SMTP keeps working (local dev).
    /// Covers every email the site sends: alumni notices, approvals, contact
    /// relay, and Umbraco's own backoffice emails.
    /// </summary>
    public class GraphMailComposer : IComposer
    {
        public void Compose(IUmbracoBuilder builder)
        {
            var section = builder.Config.GetSection(GraphMailOptions.SectionName);
            builder.Services.Configure<GraphMailOptions>(section);

            var options = section.Get<GraphMailOptions>();
            if (options is null || !options.IsConfigured) { return; }

            builder.Services.AddHttpClient(GraphEmailSender.HttpClientName, client =>
            {
                client.Timeout = TimeSpan.FromSeconds(30);
            });
            builder.Services.AddUnique<IEmailSender, GraphEmailSender>();
        }
    }
}
