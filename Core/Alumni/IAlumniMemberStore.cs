using UmbracoBase.Core.Models;

namespace UmbracoBase.Core.Alumni;

/// <summary>
/// The only thing that touches Umbraco's IMemberService for the Alumni
/// Contact Portal. Kept narrow (three operations) so controllers and
/// widgets can be tested against a hand-written fake instead of the much
/// larger IMemberService interface.
/// </summary>
public interface IAlumniMemberStore
{
    AlumniMemberSummary CreateSignup(AlumniSignupInput input);

    AlumniBrowseResult Browse(string? query, int? gradYear, int page, int pageSize);

    AlumniContactTarget? FindContactTarget(Guid memberId);
}
