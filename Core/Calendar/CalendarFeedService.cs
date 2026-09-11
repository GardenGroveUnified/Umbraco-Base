using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using UmbracoBase.Core.Models;

namespace UmbracoBase.Core.Calendar;

public sealed class CalendarFeedService : ICalendarFeedService
{
    private readonly HttpClient _httpClient;
    private readonly IMemoryCache _cache;
    private readonly ILogger<CalendarFeedService> _logger;

    private static readonly TimeSpan CacheHitFor = TimeSpan.FromMinutes(30);
    private static readonly TimeSpan CacheMissFor = TimeSpan.FromMinutes(2);

    public CalendarFeedService(HttpClient httpClient, IMemoryCache cache, ILogger<CalendarFeedService> logger)
    {
        _httpClient = httpClient;
        _cache = cache;
        _logger = logger;
    }

    public async Task<IReadOnlyList<EventCardViewModel>> GetUpcomingAsync(
        string? icsUrl, int daysAhead, int maxResults, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(icsUrl))
        {
            return Array.Empty<EventCardViewModel>();
        }

        var cacheKey = $"calendarfeed:{icsUrl}:{daysAhead}:{maxResults}";
        if (_cache.TryGetValue(cacheKey, out IReadOnlyList<EventCardViewModel>? cached) && cached is not null)
        {
            return cached;
        }

        var events = await LoadAsync(icsUrl, daysAhead, maxResults, cancellationToken);

        _cache.Set(cacheKey, events, events.Count > 0 ? CacheHitFor : CacheMissFor);
        return events;
    }

    private async Task<IReadOnlyList<EventCardViewModel>> LoadAsync(
        string icsUrl, int daysAhead, int maxResults, CancellationToken cancellationToken)
    {
        string ics;
        try
        {
            using var response = await _httpClient.GetAsync(icsUrl, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning(
                    "Calendar feed {Url} returned {StatusCode}. Showing no events.", icsUrl, (int)response.StatusCode);
                return Array.Empty<EventCardViewModel>();
            }

            ics = await response.Content.ReadAsStringAsync(cancellationToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogWarning(ex, "Could not fetch calendar feed {Url}. Showing no events.", icsUrl);
            return Array.Empty<EventCardViewModel>();
        }

        var events = IcsCalendarParser.Parse(ics, DateTime.Now, daysAhead, maxResults);
        if (events.Count == 0)
        {
            _logger.LogWarning("Calendar feed {Url} produced no upcoming events.", icsUrl);
        }

        return events;
    }
}
