using System.Text.RegularExpressions;

namespace UmbracoBase.Core.Alumni;

/// <summary>
/// Shared spam/validation checks for the alumni signup and contact-relay
/// forms. A tripped honeypot or failed validation must never reveal *why*
/// to the caller - callers should return the same generic rejection either
/// way, per the spec's error-handling section.
/// </summary>
public static partial class AlumniFormGuard
{
    private const int MaxEmailLength = 254;

    public static bool IsHoneypotTripped(string? honeypotValue) => !string.IsNullOrWhiteSpace(honeypotValue);

    public static bool IsValidEmail(string? email)
    {
        if (string.IsNullOrWhiteSpace(email) || email.Length > MaxEmailLength) { return false; }
        return EmailPattern().IsMatch(email);
    }

    public static bool IsValidMessage(string? message, int maxLength)
        => !string.IsNullOrWhiteSpace(message) && message.Length <= maxLength;

    [GeneratedRegex(@"^[^@\s]+@[^@\s]+\.[^@\s]+$")]
    private static partial Regex EmailPattern();
}
