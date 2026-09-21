using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Services;
using UmbracoBase.Core.Models;

namespace UmbracoBase.Core.Alumni;

public sealed class UmbracoMemoirStore : IMemoirStore
{
    public const string MemberTypeAlias = "memoir";
    private const int DefaultPageSize = 10;

    private readonly IMemberService _memberService;

    public UmbracoMemoirStore(IMemberService memberService)
    {
        _memberService = memberService;
    }

    public MemoirPendingApproval CreateSubmission(MemoirSubmissionInput input)
    {
        // Synthetic, unique Username *and* Email, so a memoir submission
        // never collides with an existing Member of any type that already
        // uses this person's real email (e.g. an approved alumniMember, or
        // an earlier memoir from the same person) - Umbraco enforces both
        // unique across every Member Type. Memoir submitters never log in,
        // so neither value is ever seen or used; the real address lives in
        // the contactEmail property instead.
        var synthetic = $"memoir{Guid.NewGuid():N}";
        var member = _memberService.CreateMember(synthetic, $"{synthetic}@memoir.internal", input.AuthorName, MemberTypeAlias);

        SetString(member, "authorName", input.AuthorName);
        SetInt(member, "gradYear", input.GradYear);
        SetString(member, "memoirText", input.MemoirText);
        SetString(member, "contactEmail", input.Email);
        member.IsApproved = false; // staff moderates in the backoffice Members section

        _memberService.Save(member, Umbraco.Cms.Core.Constants.Security.SuperUserId);

        return ToPendingApproval(member);
    }

    public MemoirBrowseResult GetApproved(int page, int pageSize)
    {
        var effectivePageSize = pageSize <= 0 ? DefaultPageSize : pageSize;

        var approved = _memberService
            .GetMembersByMemberType(MemberTypeAlias)
            .Where(m => m.IsApproved)
            .OrderByDescending(m => m.CreateDate)
            .ToList();

        var pageItems = approved
            .Skip(Math.Max(0, page - 1) * effectivePageSize)
            .Take(effectivePageSize)
            .Select(ToSummary)
            .ToList();

        return new MemoirBrowseResult(pageItems, approved.Count);
    }

    public IReadOnlyList<MemoirPendingApproval> GetPendingApprovals()
        => _memberService
            .GetMembersByMemberType(MemberTypeAlias)
            .Where(m => !m.IsApproved)
            .OrderBy(m => m.CreateDate)
            .Select(ToPendingApproval)
            .ToList();

    private static MemoirEntrySummary ToSummary(IMember member) => new(
        Id: member.Key,
        AuthorName: GetString(member, "authorName") ?? member.Name ?? string.Empty,
        GradYear: GetInt(member, "gradYear"),
        MemoirText: GetString(member, "memoirText") ?? string.Empty,
        SubmittedUtc: member.CreateDate);

    private static MemoirPendingApproval ToPendingApproval(IMember member) => new(
        Id: member.Key,
        AuthorName: GetString(member, "authorName") ?? member.Name ?? string.Empty,
        Email: GetString(member, "contactEmail") ?? string.Empty,
        MemoirText: GetString(member, "memoirText") ?? string.Empty,
        SubmittedUtc: member.CreateDate);

    private static void SetString(IMember member, string alias, string? value)
        => member.Properties[alias]?.SetValue(value, null, null);

    private static void SetInt(IMember member, string alias, int? value)
        => member.Properties[alias]?.SetValue(value, null, null);

    private static string? GetString(IMember member, string alias)
        => member.Properties[alias]?.GetValue(null, null, false) as string;

    private static int? GetInt(IMember member, string alias)
        => member.Properties[alias]?.GetValue(null, null, false) switch
        {
            int i => i,
            long l => (int)l,
            string s when int.TryParse(s, out var parsed) => parsed,
            _ => null
        };
}
