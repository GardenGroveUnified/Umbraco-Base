using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Umbraco.Cms.Core.Models;
public class GoogleCalendar
{
    private int _MaxResults;
    public string CalendarName { get; set; } = string.Empty;
    public string CalendarId { get; set; } = string.Empty;
    public int MaxResults
    {
        get { return _MaxResults; }
        set
        {
            if (value < 1 || value > 100) {
                _MaxResults = 10;
            }
            else {
                _MaxResults = value;
            }
        }
    }

    public List<CalendarEvents>? Items { get; set; }
    public bool HasChildren { get { return Items != null && Items.Any() && Items.Count > 0; } }
    public bool IsGoogleCalendar { get { return !string.IsNullOrWhiteSpace(CalendarId); } }
    public GoogleCalendar(string calendarName, string calendarId, int maxResults)
    {
        CalendarName = calendarName;
        CalendarId = calendarId;
        MaxResults = maxResults;
    }
}
