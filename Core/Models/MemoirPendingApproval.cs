namespace UmbracoBase.Core.Models;

/// <summary>
/// One memoir submission waiting for staff review, for the staff-only
/// pending approvals page. Includes Email since the audience is
/// authenticated backoffice staff, not the public.
/// </summary>
public sealed record MemoirPendingApproval(
    Guid Id,
    string AuthorName,
    string Email,
    string MemoirText,
    DateTime SubmittedUtc);
