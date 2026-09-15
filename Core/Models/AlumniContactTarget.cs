namespace UmbracoBase.Core.Models;

/// <summary>
/// Server-side-only view of a Member used to send the contact-relay
/// email. Never returned from a public controller action or rendered to
/// a page - Email is exactly the address the spec says must never reach
/// the browser.
/// </summary>
public sealed record AlumniContactTarget(Guid Id, string DisplayName, string Email, bool EmailingOk);
