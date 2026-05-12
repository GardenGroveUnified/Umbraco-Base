using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Models.Blocks;
using Umbraco.Cms.Core.Models.PublishedContent;

namespace UmbracoBase.Core.ViewModels
{
    public class SchoolViewModel
    {
        public string ImageSrc { get; set; } = string.Empty;
        public string ImageAlt { get; set; } = string.Empty;
        public string SchoolType { get; set; } = string.Empty;
        public List<string>? SchoolTags { get; set; } 
        public string? SchoolName { get; set; }
        public string? SchoolShort { get; set; }
        public string? Address { get; set; }
        public string? Email { get; set; }
        public string? Phone { get; set; }
        public string? Fax { get; set; }
        public string? Website { get; set; }
        public string? LinkTitle { get; set; }
        public string? LinkUrl { get; set; }
        public string? LinkTarget { get; set; }
        public string? FactSheetLinkUrl { get; set; }
        public string? FactSheetLinkTarget { get; set; }
        public IEnumerable<SocialLink>? SocialMediaLinks { get; set; }
        public IEnumerable<BlockListItem>? Staff { get; set; }
    }
}