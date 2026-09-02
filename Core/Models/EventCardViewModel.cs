namespace UmbracoBase.Core.Models;

/// <summary>
/// A single event as shown by the Upcoming Events widget. Both the manually
/// entered block list and the Google Calendar feed map into this shape so the
/// view has one rendering path.
/// </summary>
public sealed class EventCardViewModel
{
    public string? Title { get; init; }

    /// <summary>Start of the event, as a wall-clock time (the time the calendar shows).</summary>
    public DateTime Start { get; init; }

    /// <summary>True when the event has no specific time. The card then hides the time.</summary>
    public bool IsAllDay { get; init; }

    public string? Location { get; init; }

    /// <summary>Ready-to-render HTML. Feed descriptions are plain text, encoded and line-broken.</summary>
    public string? DescriptionHtml { get; init; }

    public string? PhotoUrl { get; init; }

    public string? LinkUrl { get; init; }

    public string? LinkName { get; init; }

    public string? LinkTarget { get; init; }

    /// <summary>Set after the list is built. Applies the highlighted card style.</summary>
    public bool Featured { get; set; }
}
