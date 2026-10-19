namespace UmbracoBase.Core.Models;

public sealed record AlumniSignupInput(
    string FirstName,
    string LastName,
    string? FormerLastName,
    int? GradYear,
    string? Industry,
    string? Profession,
    string? Update,
    string? HomepageUrl,
    string Email,
    string? Phone,
    string? Address,
    string? Gender);
