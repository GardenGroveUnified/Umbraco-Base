using ExcelDataReader;

namespace UmbracoBase.Core.Alumni;

/// <summary>
/// Reads an AlumniDirectory.xls/.xlsx export into one header-keyed
/// dictionary per data row. Thin wrapper around ExcelDataReader - the
/// actual column mapping logic lives in AlumniRowMapper, which is unit
/// tested independently of this class.
/// </summary>
public static class AlumniSpreadsheetReader
{
    public static IReadOnlyList<IReadOnlyDictionary<string, string>> ReadRows(Stream xlsStream)
    {
        using var reader = ExcelReaderFactory.CreateReader(xlsStream);

        if (!reader.Read()) { return Array.Empty<IReadOnlyDictionary<string, string>>(); } // no header row

        var headers = new string[reader.FieldCount];
        for (var i = 0; i < reader.FieldCount; i++)
        {
            headers[i] = reader.GetValue(i)?.ToString()?.Trim() ?? string.Empty;
        }

        var rows = new List<IReadOnlyDictionary<string, string>>();
        while (reader.Read())
        {
            var row = new Dictionary<string, string>();
            for (var i = 0; i < reader.FieldCount && i < headers.Length; i++)
            {
                if (string.IsNullOrEmpty(headers[i])) { continue; }
                row[headers[i]] = reader.GetValue(i)?.ToString()?.Trim() ?? string.Empty;
            }
            rows.Add(row);
        }

        return rows;
    }
}
