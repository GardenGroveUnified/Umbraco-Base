namespace UmbracoBase.Core.Security;

/// <summary>
/// Lets staff sign in with their bare AD username ("jsmith") instead of their
/// full email. Umbraco usernames are emails (UsernameIsEmail defaults to true),
/// so a name with no @ gets the district email domain added before Umbraco
/// looks the account up. A full email is left alone, so that still works too.
/// </summary>
public static class LoginNameNormalizer
{
    /// <summary>Used when ActiveDirectory:UsernameSuffix isn't set in appsettings.json.</summary>
    public const string DefaultSuffix = "@ggusd.us";

    public static string Normalize(string? input, string? configuredSuffix)
    {
        var name = (input ?? string.Empty).Trim();
        if (name.Length == 0 || name.Contains('@')) { return name; }

        // People used to Windows logins may type DOMAIN\jsmith.
        var slash = name.LastIndexOf('\\');
        if (slash >= 0) { name = name[(slash + 1)..]; }

        var suffix = string.IsNullOrWhiteSpace(configuredSuffix) ? DefaultSuffix : configuredSuffix.Trim();
        if (!suffix.StartsWith('@')) { suffix = "@" + suffix; }

        return name + suffix;
    }
}
