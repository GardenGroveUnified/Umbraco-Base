using Microsoft.Extensions.DependencyInjection;
using Umbraco.Cms.Core.Composing;
using Umbraco.Cms.Core.DependencyInjection;
using UmbracoBase.Core.Calendar;

namespace UmbracoBase.Core.Composers
{
    /// <summary>
    /// Registers <see cref="ICalendarFeedService"/> and the HTTP client it uses
    /// to read Google Calendar feeds for the Upcoming Events widget.
    /// </summary>
    public class CalendarFeedComposer : IComposer
    {
        public void Compose(IUmbracoBuilder builder)
        {
            builder.Services.AddHttpClient<ICalendarFeedService, CalendarFeedService>(client =>
            {
                client.Timeout = TimeSpan.FromSeconds(10);
                client.DefaultRequestHeaders.UserAgent.ParseAdd("SantiagoHS-Umbraco/1.0 (+events widget)");
            });
        }
    }
}
