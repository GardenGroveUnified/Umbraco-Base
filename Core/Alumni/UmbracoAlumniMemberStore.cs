using Umbraco.Cms.Core;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Services;
using UmbracoBase.Core.Models;

namespace UmbracoBase.Core.Alumni;

public sealed class UmbracoAlumniMemberStore : IAlumniMemberStore
{
    public const string MemberTypeAlias = "alumniMember";
    private const int DefaultPageSize = 24;

    private readonly IMemberService _memberService;

    public UmbracoAlumniMemberStore(IMemberService memberService)
    {
        _memberService = memberService;
    }

    public AlumniMemberSummary CreateSignup(AlumniSignupInput input)
    {
        var displayName = $"{input.FirstName} {input.LastName}".Trim();
        var member = _memberService.CreateMember(input.Email, input.Email, displayName, MemberTypeAlias);

        SetProfile(member, input);
        member.IsApproved = false; // staff moderates in the backoffice Members section

        _memberService.Save(member, Umbraco.Cms.Core.Constants.Security.SuperUserId);

        return ToSummary(member);
    }

    public AlumniBrowseResult Browse(string? query, int? gradYear, int page, int pageSize)
    {
        var approved = _memberService
            .GetMembersByMemberType(MemberTypeAlias)
            .Where(m => m.IsApproved)
            .Select(ToSummary);

        return AlumniBrowseFilter.Apply(approved, query, gradYear, page, pageSize <= 0 ? DefaultPageSize : pageSize);
    }

    public AlumniContactTarget? FindContactTarget(Guid memberId)
    {
        var member = _memberService
            .GetMembersByMemberType(MemberTypeAlias)
            .FirstOrDefault(m => m.Key == memberId && m.IsApproved);

        if (member is null) { return null; }

        var displayName = $"{member.Name}".Trim();
        var emailingOk = GetBool(member, "emailingOk");

        return new AlumniContactTarget(member.Key, displayName, member.Email, emailingOk);
    }

    public bool ImportLegacyRow(AlumniImportRow row)
    {
        var alreadyImported = _memberService
            .GetMembersByMemberType(MemberTypeAlias)
            .Any(m => GetString(m, "legacyRecId") == row.LegacyRecId);
        if (alreadyImported) { return false; }

        var displayName = $"{row.FirstName} {row.LastName}".Trim();
        var member = _memberService.CreateMember(row.Email, row.Email, displayName, MemberTypeAlias);

        SetString(member, "firstName", row.FirstName);
        SetString(member, "lastName", row.LastName);
        SetString(member, "formerLastName", row.FormerLastName);
        SetInt(member, "gradYear", row.GradYear);
        SetString(member, "industry", row.Industry);
        SetString(member, "profession", row.Profession);
        SetString(member, "update", row.Update);
        SetString(member, "homepageUrl", row.HomepageUrl);
        SetString(member, "phone", row.Phone);
        SetString(member, "address", row.Address);
        SetString(member, "gender", row.Gender);
        SetBool(member, "emailingOk", row.EmailingOk);
        SetString(member, "legacyRecId", row.LegacyRecId);
        member.IsApproved = true; // already-known real members, not a public submission

        _memberService.Save(member, Umbraco.Cms.Core.Constants.Security.SuperUserId);
        return true;
    }

    private static void SetProfile(IMember member, AlumniSignupInput input)
    {
        SetString(member, "firstName", input.FirstName);
        SetString(member, "lastName", input.LastName);
        SetString(member, "formerLastName", input.FormerLastName);
        SetInt(member, "gradYear", input.GradYear);
        SetString(member, "industry", input.Industry);
        SetString(member, "profession", input.Profession);
        SetString(member, "update", input.Update);
        SetString(member, "homepageUrl", input.HomepageUrl);
        SetString(member, "phone", input.Phone);
        SetString(member, "address", input.Address);
        SetString(member, "gender", input.Gender);
        SetBool(member, "emailingOk", true); // new signups opt in by default
    }

    private static AlumniMemberSummary ToSummary(IMember member) => new(
        Id: member.Key,
        FirstName: GetString(member, "firstName") ?? string.Empty,
        LastName: GetString(member, "lastName") ?? string.Empty,
        FormerLastName: GetString(member, "formerLastName"),
        GradYear: GetInt(member, "gradYear"),
        Industry: GetString(member, "industry"),
        Profession: GetString(member, "profession"),
        Update: GetString(member, "update"),
        HomepageUrl: GetString(member, "homepageUrl"),
        EmailingOk: GetBool(member, "emailingOk"));

    private static void SetString(IMember member, string alias, string? value)
        => member.Properties[alias]?.SetValue(value, null, null);

    private static void SetInt(IMember member, string alias, int? value)
        => member.Properties[alias]?.SetValue(value, null, null);

    private static void SetBool(IMember member, string alias, bool value)
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

    private static bool GetBool(IMember member, string alias)
        => member.Properties[alias]?.GetValue(null, null, false) switch
        {
            bool b => b,
            string s when bool.TryParse(s, out var parsed) => parsed,
            _ => false
        };
}
