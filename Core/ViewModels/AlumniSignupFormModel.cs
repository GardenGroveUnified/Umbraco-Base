namespace UmbracoBase.Core.ViewModels
{
    public class AlumniSignupFormModel
    {
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string? FormerLastName { get; set; }
        public int? GradYear { get; set; }
        public string? Industry { get; set; }
        public string? Profession { get; set; }
        public string? Update { get; set; }
        public string? HomepageUrl { get; set; }
        public string Email { get; set; } = string.Empty;
        public string? Phone { get; set; }
        public string? Address { get; set; }
        public string? Gender { get; set; }

        /// <summary>Hidden field a real user never fills in. Non-empty = bot.</summary>
        public string? Website { get; set; }
    }
}
