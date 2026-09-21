namespace UmbracoBase.Core.Models;

/// <summary>View model for the staff-only /admin/alumni-approvals page.</summary>
public sealed record AlumniApprovalsPageModel(
    IReadOnlyList<AlumniPendingApproval> Signups,
    IReadOnlyList<MemoirPendingApproval> Memoirs);
