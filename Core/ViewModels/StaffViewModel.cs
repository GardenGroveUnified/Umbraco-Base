using Examine.Lucene.Analyzers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UmbracoBase.Core.ViewModels
{
    public class StaffViewModel
    {
        public string ImageSrc { get; set; } = string.Empty;
        public string ImageSrcset { get; set; } = string.Empty;
        public string ImageAlt { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Department { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string LinkTitle { get; set; } = string.Empty;
        public string LinkUrl { get; set; } = string.Empty;
        public string LinkTarget { get; set; } = string.Empty;
    }
}