namespace UmbracoBase.Core.Models;

/// <summary>
/// One approved memoir for the public "Read Memoirs" list. Never includes
/// Email - that stays private to staff moderation, same rule as
/// AlumniMemberSummary.
/// </summary>
public sealed record MemoirEntrySummary(
    Guid Id,
    string AuthorName,
    int? GradYear,
    string MemoirText,
    DateTime SubmittedUtc);
