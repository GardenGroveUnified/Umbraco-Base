namespace UmbracoBase.Core.Models;

public sealed record MemoirBrowseResult(IReadOnlyList<MemoirEntrySummary> Items, int TotalCount);
