using UmbracoBase.Core.Models;

namespace UmbracoBase.Core.Alumni;

/// <summary>
/// The only thing that touches Umbraco's IMemberService for memoir
/// submissions. Mirrors IAlumniMemberStore: kept narrow so controllers and
/// widgets can be tested against a hand-written fake.
/// </summary>
public interface IMemoirStore
{
    MemoirPendingApproval CreateSubmission(MemoirSubmissionInput input);

    /// <summary>Approved memoirs, newest first, for the public "Read Memoirs" list.</summary>
    MemoirBrowseResult GetApproved(int page, int pageSize);

    /// <summary>
    /// Memoir submissions with IsApproved still false, oldest first, for the
    /// staff-only pending approvals page.
    /// </summary>
    IReadOnlyList<MemoirPendingApproval> GetPendingApprovals();
}
