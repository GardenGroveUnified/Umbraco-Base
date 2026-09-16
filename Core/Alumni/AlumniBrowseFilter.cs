using UmbracoBase.Core.Models;

namespace UmbracoBase.Core.Alumni;

/// <summary>
/// Pure name/grad-year filtering and pagination over an in-memory list of
/// already-approved alumni. Kept free of MemberService so it can be unit
/// tested directly - the Umbraco-facing store (UmbracoAlumniMemberStore)
/// loads the full approved list once, then delegates here.
/// </summary>
public static class AlumniBrowseFilter
{
    public static AlumniBrowseResult Apply(
        IEnumerable<AlumniMemberSummary> all, string? query, int? gradYear, int page, int pageSize)
    {
        IEnumerable<AlumniMemberSummary> filtered = all;

        if (!string.IsNullOrWhiteSpace(query))
        {
            filtered = filtered.Where(a =>
                a.FirstName.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                a.LastName.Contains(query, StringComparison.OrdinalIgnoreCase));
        }

        if (gradYear.HasValue)
        {
            filtered = filtered.Where(a => a.GradYear == gradYear.Value);
        }

        var matched = filtered.ToList();
        var pageItems = matched
            .Skip(Math.Max(0, page - 1) * pageSize)
            .Take(pageSize)
            .ToList();

        return new AlumniBrowseResult(pageItems, matched.Count);
    }
}
