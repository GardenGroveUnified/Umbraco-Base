namespace UmbracoBase.Core.ViewModels
{
    public class AlumniContactFormModel
    {
        public Guid MemberId { get; set; }
        public string SenderName { get; set; } = string.Empty;
        public string SenderEmail { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;

        /// <summary>Hidden field a real user never fills in. Non-empty = bot.</summary>
        public string? Website { get; set; }
    }
}
