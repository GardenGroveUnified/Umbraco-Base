using UmbracoBase.Core.Alumni;
using UmbracoBase.Core.Models;

namespace UmbracoBase.Tests.Alumni.Fakes;

public sealed class FakeAlumniMemberStore : IAlumniMemberStore
{
    public List<AlumniSignupInput> Signups { get; } = new();
    public Dictionary<Guid, AlumniContactTarget> Targets { get; } = new();

    public AlumniMemberSummary CreateSignup(AlumniSignupInput input)
    {
        Signups.Add(input);
        return new AlumniMemberSummary(
            Guid.NewGuid(), input.FirstName, input.LastName, input.FormerLastName,
            input.GradYear, input.Industry, input.Profession, input.Update, input.HomepageUrl,
            EmailingOk: true);
    }

    public AlumniBrowseResult Browse(string? query, int? gradYear, int page, int pageSize)
        => new(Array.Empty<AlumniMemberSummary>(), 0);

    public AlumniContactTarget? FindContactTarget(Guid memberId)
        => Targets.TryGetValue(memberId, out var target) ? target : null;
}
