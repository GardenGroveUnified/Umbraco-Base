using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Umbraco.Cms.Core.Models;

public class CalendarEvent
{
        public string Name { get; set; }
        public DateTimeOffset StartDate { get; set; }
        public DateTimeOffset EndDate{ get; set; }
        public string Duration { get; set; }
        public string Location { get; set; }
        public string HtmlLink { get; set; }
        public string StartEndDateRange { get; set; }
        public string Description { get; set; }

        public CalendarEvent(string name, DateTimeOffset startDate, DateTimeOffset endDate, string duration, string location, string htmlLink, string description, string startEndDateRange = "")
        {
            Name = name;
            StartDate = startDate;
            EndDate = endDate;
            Duration = duration.Replace(":00","").Replace(" PM","pm").Replace(" AM", "am");
            Location = location;
            HtmlLink = htmlLink;
            Description = description;
            StartEndDateRange = startEndDateRange;
        }
}
