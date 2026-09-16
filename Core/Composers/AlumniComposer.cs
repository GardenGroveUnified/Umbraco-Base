using System.Text;
using Microsoft.Extensions.DependencyInjection;
using Umbraco.Cms.Core.Composing;
using Umbraco.Cms.Core.DependencyInjection;
using UmbracoBase.Core.Alumni;

namespace UmbracoBase.Core.Composers
{
    /// <summary>
    /// Registers the Alumni Contact Portal's data-access layer. Everything
    /// else in the feature (controllers, widgets) depends only on
    /// IAlumniMemberStore, never on IMemberService directly.
    /// </summary>
    public class AlumniComposer : IComposer
    {
        public void Compose(IUmbracoBuilder builder)
        {
            builder.Services.AddScoped<IAlumniMemberStore, UmbracoAlumniMemberStore>();

            // ExcelDataReader needs this for legacy .xls code-page text encodings.
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        }
    }
}
