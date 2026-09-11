using UmbracoBase.Core.Calendar;
using Xunit;

namespace UmbracoBase.Tests.Calendar;

public class IcsCalendarParserTests
{
    private static string Feed(string name)
        => File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Calendar", "Feeds", name));

    // A fixed "now" so the fixtures stay valid forever.
    private static readonly DateTime From = new(2026, 10, 1);

    [Fact]
    public void Parse_reads_a_timed_event_with_its_wall_clock_start()
    {
        var events = IcsCalendarParser.Parse(Feed("basic.ics"), From, daysAhead: 60, maxResults: 12);

        var movie = Assert.Single(events, e => e.Title == "Movie Night");
        Assert.Equal(new DateTime(2026, 10, 14, 18, 0, 0), movie.Start);
        Assert.False(movie.IsAllDay);
        Assert.Equal("MPR", movie.Location);
    }

    [Fact]
    public void Parse_excludes_events_that_start_before_from()
    {
        var events = IcsCalendarParser.Parse(Feed("basic.ics"), From, daysAhead: 60, maxResults: 12);

        Assert.DoesNotContain(events, e => e.Title == "First Day Assembly");
    }

    [Fact]
    public void Parse_excludes_events_beyond_the_days_ahead_window()
    {
        var events = IcsCalendarParser.Parse(Feed("basic.ics"), From, daysAhead: 60, maxResults: 12);

        // Prom is March 2027, well past 60 days from 1 Oct 2026.
        Assert.DoesNotContain(events, e => e.Title == "Prom 2027");
    }

    [Fact]
    public void Parse_marks_a_date_only_event_as_all_day()
    {
        var events = IcsCalendarParser.Parse(Feed("basic.ics"), From, daysAhead: 60, maxResults: 12);

        var breakEvent = Assert.Single(events, e => e.Title == "Thanksgiving Break");
        Assert.True(breakEvent.IsAllDay);
        Assert.Equal(new DateTime(2026, 11, 23), breakEvent.Start.Date);
    }

    [Fact]
    public void Parse_returns_events_sorted_by_start()
    {
        var events = IcsCalendarParser.Parse(Feed("basic.ics"), From, daysAhead: 120, maxResults: 12);

        var starts = events.Select(e => e.Start).ToList();
        Assert.Equal(starts.OrderBy(s => s), starts);
    }

    [Fact]
    public void Parse_caps_the_result_at_max_results()
    {
        var events = IcsCalendarParser.Parse(Feed("basic.ics"), From, daysAhead: 400, maxResults: 2);

        Assert.Equal(2, events.Count);
        // The earliest two, in order.
        Assert.Equal("Movie Night", events[0].Title);
        Assert.Equal("Thanksgiving Break", events[1].Title);
    }

    [Fact]
    public void Parse_html_encodes_the_description()
    {
        var events = IcsCalendarParser.Parse(Feed("basic.ics"), From, daysAhead: 60, maxResults: 12);

        var movie = Assert.Single(events, e => e.Title == "Movie Night");
        Assert.Equal("Bring a friend &amp; a blanket", movie.DescriptionHtml);
    }

    [Fact]
    public void Parse_expands_a_weekly_recurring_event()
    {
        var events = IcsCalendarParser.Parse(Feed("recurring.ics"), From, daysAhead: 21, maxResults: 12);

        // Mondays within 1-22 Oct 2026: the 5th, 12th, 19th.
        Assert.Equal(3, events.Count);
        Assert.All(events, e => Assert.Equal("Chess Club", e.Title));
        Assert.Equal(
            new[] { new DateTime(2026, 10, 5, 15, 0, 0), new(2026, 10, 12, 15, 0, 0), new(2026, 10, 19, 15, 0, 0) },
            events.Select(e => e.Start));
    }

    [Fact]
    public void Parse_accepts_a_local_kind_from_date()
    {
        // CalendarFeedService passes DateTime.Now, which is DateTimeKind.Local.
        var localNow = DateTime.SpecifyKind(new DateTime(2026, 10, 1, 8, 0, 0), DateTimeKind.Local);

        var events = IcsCalendarParser.Parse(Feed("basic.ics"), localNow, daysAhead: 60, maxResults: 12);

        Assert.Contains(events, e => e.Title == "Movie Night");
    }

    [Fact]
    public void Parse_returns_empty_for_malformed_input()
    {
        var events = IcsCalendarParser.Parse("this is not a calendar", From, daysAhead: 60, maxResults: 12);

        Assert.Empty(events);
    }

    [Fact]
    public void Parse_returns_empty_for_an_empty_string()
    {
        var events = IcsCalendarParser.Parse("", From, daysAhead: 60, maxResults: 12);

        Assert.Empty(events);
    }
}
