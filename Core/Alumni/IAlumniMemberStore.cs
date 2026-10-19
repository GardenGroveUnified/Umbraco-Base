using UmbracoBase.Core.Models;

namespace UmbracoBase.Core.Alumni;

/// <summary>
/// The only thing that touches Umbraco's IMemberService for the Alumni
/// Contact Portal. Kept narrow so controllers and widgets can be tested
/// against a hand-written fake instead of the much larger IMemberService
/// interface.
/// </summary>
public interface IAlumniMemberStore
{
    AlumniMemberSummary CreateSignup(AlumniSignupInput input);

    AlumniBrowseResult Browse(string? query, int? gradYear, int page, int pageSize);

    AlumniContactTarget? FindContactTarget(Guid memberId);

    /// <summary>
    /// Creates an approved Member for one legacy row, unless a Member with
    /// the same LegacyRecId already exists (re-running the import is then
    /// a no-op for that row). Returns true if a Member was created.
    /// </summary>
    bool ImportLegacyRow(AlumniImportRow row);

    /// <summary>
    /// Alumni signups with IsApproved still false, oldest first, for the
    /// staff-only pending approvals page.
    /// </summary>
    IReadOnlyList<AlumniPendingApproval> GetPendingApprovals();
}
