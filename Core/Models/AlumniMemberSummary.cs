namespace UmbracoBase.Core.Models;

/// <summary>
/// Public-facing alumni directory card data. Contains only fields the
/// Alumni Contact Portal spec marks public - never email, phone, address
/// or gender. See docs/superpowers/specs/2026-09-15-alumni-contact-portal-design.md.
/// </summary>
public sealed record AlumniMemberSummary(
    Guid Id,
    string FirstName,
    string LastName,
    string? FormerLastName,
    int? GradYear,
    string? Industry,
    string? Profession,
    string? Update,
    string? HomepageUrl,
    bool EmailingOk);
