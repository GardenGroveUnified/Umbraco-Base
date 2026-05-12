// using Umbraco.Cms.Core.Models;
// using Google.Apis.Calendar.v3;
// using Google.Apis.Calendar.v3.Data;
// using Google.Apis.Services;
// using Google.Apis.Auth.OAuth2;
// using Google.Apis.Util.Store;
// using System;
// using System.Collections.Generic;
// using System.Linq;
// using System.Text;
// using System.Threading.Tasks;

// namespace Umbraco.Cms.Core.Services;

// public class GoogleCalenderService : IGoogleCalenderService
// {
//     public List<CalendarEvents> GetGoogleCalendarEventList(string apiKey, string applicationName, string calendarId, bool showOnlySingleOneOffEvents = true, int maxResults = 10)
//     {
//         var googleEvents = GetGoogleEventsList(apiKey, applicationName, calendarId, showOnlySingleOneOffEvents, maxResults);
//         List<CalendarEvent> listOfEvent = CalendarPropertyMapping(googleEvents!, maxResults);

//         //group event by date
//         var events = listOfEvent.GroupBy(u => u.StartDate.DateTime.ToShortDateString())
//                     .Select(grp => new CalendarEvents(DateTime.Parse(grp.Key), grp.ToList()))
//                     .ToList();
//         return events;
//     }

//     private Google.Apis.Calendar.v3.Data.Events ? GetGoogleEventsList(string apiKey, string applicationName, string calendarId, bool showOnlySingleOneOffEvents, int maxResults)
//     {
//         try
//         {
//             var service = new CalendarService(new BaseClientService.Initializer()
//             {
//                 ApiKey = apiKey,
//                 ApplicationName = applicationName
//             });

//             var request = service.Events.List(calendarId);
//             request.ShowDeleted = false;    //Whether to include deleted events (with status equals "cancelled") in the result.
//             request.SingleEvents = showOnlySingleOneOffEvents;  //Whether to expand recurring events into instances and only return single one-off events and instances of recurring events, but not the underlying recurring events themselves.
//             request.MaxResults = maxResults;
//             request.TimeMinDateTimeOffset = DateTime.UtcNow;

//             if (showOnlySingleOneOffEvents == true)
//             {
//                 //Sorting works only when singleEvents: true
//                 //https://github.com/google/google-api-nodejs-client/issues/771
//                 request.OrderBy = EventsResource.ListRequest.OrderByEnum.StartTime;
//             }

//             return request.Execute();
//         }
//         catch (Exception)
//         {
//             return null;
//         }
//     }
//     private List<CalendarEvent> CalendarPropertyMapping(Google.Apis.Calendar.v3.Data.Events events, int maxResults)
//     {
//         var calendarEventList = new List<CalendarEvent>();
//         if (events != null)
//         {
//             if (events.Items != null && events.Items.Count > 0)
//             {
//                 foreach (Event item in events.Items)
//                 {
//                     if (item.Start != null)
//                     {
//                         if ((item.Start.Date != null) && (item.End.Date != null))
//                         {
//                             //All day events could span multiple days - repeat event details for each day.
//                             DateTime eventStart = DateTime.Parse(item.Start.Date);
//                             DateTime eventEnd = DateTime.Parse(item.End.Date);
//                             TimeSpan eventTimeSpan = eventEnd - eventStart;
//                             int numberOfEvent = eventTimeSpan.Days;
//                             if (numberOfEvent > maxResults)
//                             {
//                                 numberOfEvent = maxResults;
//                             }

//                             for (int i = 0; i < numberOfEvent; i++)
//                             {
//                                 item.Start.Date = eventStart.ToShortDateString();
//                                 item.End.Date = eventStart.AddDays(1).ToShortDateString();
//                                 calendarEventList.Add(GetEventDetails(item));

//                                 //increment event detail for the next day accurance.
//                                 eventStart = eventStart.AddDays(1);
//                             }

//                         }
//                         else
//                         {
//                             calendarEventList.Add(GetEventDetails(item));
//                         }
//                     }
//                 }
//             }
//         }

//         return calendarEventList;
//     }
//     private CalendarEvent GetEventDetails(Event item)
//     {
//         string name = item.Summary;
//         DateTimeOffset? startDate;
//         DateTimeOffset? endDate;
//         string duration = "";
//         string location = item.Location;
//         string htmlLink = item.HtmlLink;
//         string startEndDateRange = "";
//         string description = item.Description;
//         string datetimeRaw = item.Start.DateTimeRaw;
//         //DateTimeOffset dateTimeOffset = (DateTimeOffset)item.Start.DateTimeDateTimeOffset;

//         if (item.Start.Date != null)
//         {
//             duration = "All day";
//             startDate = Convert.ToDateTime(item.Start.Date);
//             endDate = Convert.ToDateTime(item.End.Date);
//         }
//         else
//         {
//             startDate = item.Start.DateTimeDateTimeOffset;
//             endDate = item.End.DateTimeDateTimeOffset;
//             //startEndDateRange = string.Format("{0:M/dd}", startDate) + " - " + string.Format("{0:M/dd}", endDate);
//             if (item.Start.DateTimeDateTimeOffset == item.End.DateTimeDateTimeOffset)
//             {
//                 duration = string.Format("{0:t}", item.Start.DateTimeDateTimeOffset);
//             }
//             else
//             {
//                 duration = string.Format("{0:t}", item.Start.DateTimeDateTimeOffset) + " - " + string.Format("{0:t}", item.End.DateTimeDateTimeOffset);
//             }
//         }

//         TimeSpan eventTimeSpan = (TimeSpan)(endDate - startDate)!;
//         if (eventTimeSpan.Days > 1)
//         {
//             startEndDateRange = string.Format("{0:M/dd}", startDate) + " - " + string.Format("{0:M/dd}", endDate);
//         }

//         return new CalendarEvent(name, (DateTimeOffset)startDate!, (DateTimeOffset)endDate!, duration, location, htmlLink, description, startEndDateRange);
//     }
// }
