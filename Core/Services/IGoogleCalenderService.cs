using Umbraco.Cms.Core.Models;

namespace Umbraco.Cms.Core.Services
{
    public interface IGoogleCalenderService
    {
        List<CalendarEvents> GetGoogleCalendarEventList(String apiKey, String applicationName, String calendarId, Boolean showOnlySingleOneOffEvents = true, Int32 maxResults = 10);
    }
}