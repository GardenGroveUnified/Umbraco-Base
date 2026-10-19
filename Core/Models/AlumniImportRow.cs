namespace UmbracoBase.Core.Models;

/// <summary>
/// One row of the legacy AlumniDirectory.xls, after column mapping.
/// Title, Middle Name and Record Date are intentionally not carried
/// over - out of scope per the design spec.
/// </summary>
public sealed record AlumniImportRow(
    string LegacyRecId,
    string FirstName,
    string LastName,
    string? FormerLastName,
    int? GradYear,
    string? Gender,
    string Email,
    string? HomepageUrl,
    string? Phone,
    string? Address,
    string? Industry,
    string? Profession,
    string? Update,
    bool EmailingOk);
