using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UmbracoBase.Core.ViewModels
{
    public class BrocadeSwitchViewModel
    {
        public string FormTitle { get; set; } = string.Empty;
        public string? SiteNumber { get; set; }
        public string? ZoneName { get; set; }
        public string? SwitchNumber  { get; set; }
        public string? SerialNumber { get; set;}
        public string? DistrictTag { get; set;}
        public string? Location { get; set;}
        public string? ModelNumber { get; set;}
        public bool DataVoice { get; set;}
    }
}
