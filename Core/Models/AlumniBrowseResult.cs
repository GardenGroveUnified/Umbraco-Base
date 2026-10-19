namespace UmbracoBase.Core.Models;

public sealed record AlumniBrowseResult(IReadOnlyList<AlumniMemberSummary> Items, int TotalCount);
