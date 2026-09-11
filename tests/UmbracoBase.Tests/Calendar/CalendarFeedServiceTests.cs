using System.Net;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging.Abstractions;
using UmbracoBase.Core.Calendar;
using Xunit;

namespace UmbracoBase.Tests.Calendar;

public class CalendarFeedServiceTests
{
    private const string Url = "https://calendar.example/feed.ics";

    private static string Feed(string name)
        => File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Calendar", "Feeds", name));

    private static CalendarFeedService Build(StubHandler handler)
        => new(new HttpClient(handler), new MemoryCache(new MemoryCacheOptions()), NullLogger<CalendarFeedService>.Instance);

    [Fact]
    public async Task GetUpcomingAsync_returns_events_from_the_fetched_feed()
    {
        var service = Build(StubHandler.Returns(HttpStatusCode.OK, Feed("service-feed.ics")));

        var events = await service.GetUpcomingAsync(Url, daysAhead: 40000, maxResults: 12);

        Assert.Equal(new[] { "Open House", "Spring Rally" }, events.Select(e => e.Title));
    }

    [Fact]
    public async Task GetUpcomingAsync_returns_empty_when_the_response_is_not_success()
    {
        var service = Build(StubHandler.Returns(HttpStatusCode.NotFound, "nope"));

        var events = await service.GetUpcomingAsync(Url, daysAhead: 40000, maxResults: 12);

        Assert.Empty(events);
    }

    [Fact]
    public async Task GetUpcomingAsync_returns_empty_when_the_request_throws()
    {
        var service = Build(StubHandler.Throws(new HttpRequestException("boom")));

        var events = await service.GetUpcomingAsync(Url, daysAhead: 40000, maxResults: 12);

        Assert.Empty(events);
    }

    [Fact]
    public async Task GetUpcomingAsync_returns_empty_for_a_blank_url()
    {
        var handler = StubHandler.Returns(HttpStatusCode.OK, Feed("service-feed.ics"));
        var service = Build(handler);

        var events = await service.GetUpcomingAsync("  ", daysAhead: 40000, maxResults: 12);

        Assert.Empty(events);
        Assert.Equal(0, handler.Calls);
    }

    [Fact]
    public async Task GetUpcomingAsync_fetches_the_feed_only_once_for_repeat_calls()
    {
        var handler = StubHandler.Returns(HttpStatusCode.OK, Feed("service-feed.ics"));
        var service = Build(handler);

        await service.GetUpcomingAsync(Url, daysAhead: 40000, maxResults: 12);
        await service.GetUpcomingAsync(Url, daysAhead: 40000, maxResults: 12);

        Assert.Equal(1, handler.Calls);
    }

    private sealed class StubHandler : HttpMessageHandler
    {
        private readonly Func<HttpResponseMessage> _respond;
        public int Calls { get; private set; }

        private StubHandler(Func<HttpResponseMessage> respond) => _respond = respond;

        public static StubHandler Returns(HttpStatusCode status, string body)
            => new(() => new HttpResponseMessage(status) { Content = new StringContent(body) });

        public static StubHandler Throws(Exception ex) => new(() => throw ex);

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Calls++;
            return Task.FromResult(_respond());
        }
    }
}
