using System.Net;
using Ical.Net.CalendarComponents;
using Ical.Net.DataTypes;
using UmbracoBase.Core.Models;
using IcalCalendar = Ical.Net.Calendar;

namespace UmbracoBase.Core.Calendar;

/// <summary>
/// Turns raw iCalendar (.ics) text into a sorted list of upcoming event cards.
/// Pure: no HTTP, no cache, no Umbraco. See <see cref="ICalendarFeedService"/>
/// for the piece that fetches and caches the feed.
/// </summary>
public static class IcsCalendarParser
{
    /// <param name="icsContent">The raw feed text.</param>
    /// <param name="from">Events starting before this are dropped.</param>
    /// <param name="daysAhead">Events starting after from plus this many days are dropped.</param>
    /// <param name="maxResults">Cap on the number of events returned.</param>
    public static IReadOnlyList<EventCardViewModel> Parse(
        string icsContent, DateTime from, int daysAhead, int maxResults)
    {
        if (string.IsNullOrWhiteSpace(icsContent))
        {
            return Array.Empty<EventCardViewModel>();
        }

        IcalCalendar? calendar;
        try
        {
            calendar = IcalCalendar.Load(icsContent);
        }
        catch
        {
            return Array.Empty<EventCardViewModel>();
        }

        if (calendar is null || calendar.Events.Count == 0)
        {
            return Array.Empty<EventCardViewModel>();
        }

        // CalDateTime rejects DateTimeKind.Local. Treat the caller's value as a
        // wall-clock time regardless of how they stamped it.
        from = DateTime.SpecifyKind(from, DateTimeKind.Unspecified);
        var windowEnd = from.AddDays(daysAhead);

        IEnumerable<Occurrence> occurrences;
        try
        {
            occurrences = calendar.GetOccurrences(new CalDateTime(from, hasTime: true));
        }
        catch
        {
            return Array.Empty<EventCardViewModel>();
        }

        var cards = new List<EventCardViewModel>();
        try
        {
            foreach (var occ in occurrences)
            {
                var start = ToSiteWallClock(occ.Period.StartTime);

                // GetOccurrences yields in chronological order, so once we pass the
                // window there is nothing left to find.
                if (start >= windowEnd)
                {
                    break;
                }

                if (start < from || occ.Source is not CalendarEvent ev)
                {
                    continue;
                }

                cards.Add(new EventCardViewModel
                {
                    Title = ev.Summary?.Trim(),
                    Start = start,
                    IsAllDay = ev.IsAllDay,
                    Location = string.IsNullOrWhiteSpace(ev.Location) ? null : ev.Location.Trim(),
                    DescriptionHtml = ToHtml(ev.Description),
                });
            }
        }
        catch
        {
            // A bad recurrence rule can throw part way through. Keep what we have.
        }

        return cards
            .OrderBy(c => c.Start)
            .Take(maxResults)
            .ToList();
    }

    // The school's time zone. Every card shows its time as Santiago's clock reads it.
    private static readonly TimeZoneInfo SiteTimeZone =
        TimeZoneInfo.FindSystemTimeZoneById("America/Los_Angeles");

    /// <summary>
    /// CalDateTime.Value is the raw clock value in whatever zone the feed wrote it in.
    /// Google writes most timed events in UTC ("...T200000Z"), so the raw value would
    /// show noon as 8 PM. Convert any zoned or UTC time to Pacific. Leave date-only
    /// and floating times alone, since they have no zone to convert from.
    /// </summary>
    private static DateTime ToSiteWallClock(CalDateTime value)
    {
        if (!value.HasTime || (!value.IsUtc && value.TzId is null))
        {
            return value.Value;
        }

        return TimeZoneInfo.ConvertTimeFromUtc(value.AsUtc, SiteTimeZone);
    }

    private static string? ToHtml(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return null;
        }

        return WebUtility.HtmlEncode(text.Trim())
            .Replace("\r\n", "<br />")
            .Replace("\n", "<br />");
    }
}
