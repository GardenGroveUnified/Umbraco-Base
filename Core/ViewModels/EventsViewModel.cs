using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Models.Blocks;
using Umbraco.Cms.Core.Models.PublishedContent;

namespace UmbracoBase.Core.ViewModels
{
    public class EventsViewModel{
        public string EventName { get; set; } = "";
        public string Location { get; set; } = "";
        public Umbraco.Cms.Core.Strings.IHtmlEncodedString? Description { get; set; }
        public DateTime StartDateTime { get; set; } = DateTime.Now;
        public DateTime EndDateTime { get; set; } = DateTime.MaxValue;
        public string ImageSrc { get; set; } = "";
        public string ImageAlt { get; set; } = "";
        public IEnumerable<Link> Links { get; set; } = Enumerable.Empty<Link>();
        public List<string> EventTags { get; set; } = new List<string>();
        public string RecurringEventOption { get; set; } = "";
    }
}