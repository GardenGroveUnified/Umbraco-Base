using UmbracoBase.Core.Alumni;
using UmbracoBase.Core.Models;

namespace UmbracoBase.Tests.Alumni.Fakes;

public sealed class FakeMemoirStore : IMemoirStore
{
    public List<MemoirSubmissionInput> Submissions { get; } = new();
    public List<MemoirEntrySummary> Approved { get; } = new();
    public List<MemoirPendingApproval> PendingApprovals { get; } = new();

    public MemoirPendingApproval CreateSubmission(MemoirSubmissionInput input)
    {
        Submissions.Add(input);
        return new MemoirPendingApproval(Guid.NewGuid(), input.AuthorName, input.Email, input.MemoirText, DateTime.UtcNow);
    }

    public MemoirBrowseResult GetApproved(int page, int pageSize) => new(Approved, Approved.Count);

    public IReadOnlyList<MemoirPendingApproval> GetPendingApprovals() => PendingApprovals;
}
