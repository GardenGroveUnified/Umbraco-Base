namespace UmbracoBase.Core.Models;

public sealed record MemoirSubmissionInput(
    string AuthorName,
    int? GradYear,
    string Email,
    string MemoirText);
