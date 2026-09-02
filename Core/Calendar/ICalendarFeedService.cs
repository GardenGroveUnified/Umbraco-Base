using UmbracoBase.Core.Models;

namespace UmbracoBase.Core.Calendar;

/// <summary>
/// Fetches a public Google Calendar (or any iCalendar) feed and returns the
/// next run of events. Results are cached. A failure to fetch or parse the feed
/// returns an empty list rather than throwing, so a bad URL never breaks a page.
/// </summary>
public interface ICalendarFeedService
{
    Task<IReadOnlyList<EventCardViewModel>> GetUpcomingAsync(
        string? icsUrl, int daysAhead, int maxResults, CancellationToken cancellationToken = default);
}
