namespace UmbracoBase.Core.ViewModels
{
public class NavBarViewModel
{
    public List<Umbraco.Cms.Core.Models.Link>? Links { get; set; }
    public string? Heading { get; set; }
    public string? CurrentPageUrl { get; set; }
    public string? VerticalSpacing { get; set; }
    public bool TopBorder { get; set; }
    public bool CenterHeading { get; set; }
    public bool SetPillsStyle { get; set; }
    public string? AnchorID { get; set; }
    public bool HideBlock { get; set; }
    public bool VerticalStyle { get; set; }
}
}