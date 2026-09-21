namespace UmbracoBase.Core.ViewModels
{
    public class MemoirSubmissionFormModel
    {
        public string AuthorName { get; set; } = string.Empty;
        public int? GradYear { get; set; }
        public string Email { get; set; } = string.Empty;
        public string MemoirText { get; set; } = string.Empty;

        /// <summary>Hidden field a real user never fills in. Non-empty = bot.</summary>
        public string? Website { get; set; }
    }
}
