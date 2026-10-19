namespace UmbracoBase.Core.Models;

/// <summary>
/// One alumni signup waiting for staff review, for the staff-only pending
/// approvals page. Includes Email since the audience is authenticated
/// backoffice staff, not the public.
/// </summary>
public sealed record AlumniPendingApproval(
    Guid Id,
    string FirstName,
    string LastName,
    string Email,
    DateTime SubmittedUtc);
