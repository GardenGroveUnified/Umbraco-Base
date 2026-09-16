namespace UmbracoBase.Core.Alumni;

using UmbracoBase.Core.Models;

/// <summary>
/// Pure mapping from one AlumniDirectory.xls row (as a header-name-keyed
/// dictionary) to an AlumniImportRow. Kept independent of the Excel
/// library so it's testable with plain in-memory dictionaries - see
/// AlumniSpreadsheetReader for the part that actually reads the file.
/// </summary>
public static class AlumniRowMapper
{
    public static AlumniImportRow? Map(IReadOnlyDictionary<string, string> row)
    {
        var email = Get(row, "Email 1");
        if (string.IsNullOrWhiteSpace(email)) { return null; }

        var firstName = Get(row, "First Name");
        var lastName = Get(row, "Last Name");

        return new AlumniImportRow(
            LegacyRecId: Get(row, "REC_ID"),
            FirstName: firstName,
            LastName: lastName,
            FormerLastName: NullIfEmpty(Get(row, "Former Last Name")),
            GradYear: int.TryParse(Get(row, "Graduation Year"), out var year) ? year : null,
            Gender: NullIfEmpty(Get(row, "Gender")),
            Email: email,
            HomepageUrl: NullIfEmpty(Get(row, "Homepage")),
            Phone: NullIfEmpty(Get(row, "Phone 1")),
            Address: CombineAddress(row),
            Industry: NullIfEmpty(Get(row, "Industry")),
            Profession: NullIfEmpty(Get(row, "Profession")),
            Update: NullIfEmpty(Get(row, "Other information")),
            EmailingOk: Get(row, "Emailing is OK") == "1");
    }

    private static string CombineAddress(IReadOnlyDictionary<string, string> row)
    {
        var street = Get(row, "Street");
        var city = Get(row, "City");
        var state = Get(row, "State");
        var zip = Get(row, "Zip");
        var country = Get(row, "Country");

        var cityStateZip = string.Join(" ", new[] { $"{city},", state, zip }
            .Where(s => !string.IsNullOrWhiteSpace(s.TrimEnd(','))))
            .Trim();

        var parts = new[] { street, cityStateZip, country }.Where(s => !string.IsNullOrWhiteSpace(s));
        return string.Join(", ", parts);
    }

    private static string Get(IReadOnlyDictionary<string, string> row, string key)
        => row.TryGetValue(key, out var value) ? value.Trim() : string.Empty;

    private static string? NullIfEmpty(string value) => string.IsNullOrWhiteSpace(value) ? null : value;
}
