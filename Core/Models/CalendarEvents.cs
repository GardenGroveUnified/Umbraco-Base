using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Umbraco.Cms.Core.Models;

public class CalendarEvents
{
    public DateTime Date { get; set; }
    public List<CalendarEvent> Items { get; set; }
    public CalendarEvents(DateTime date, List<CalendarEvent> items)
    {
        Date = date;
        Items = items;
    }
}
