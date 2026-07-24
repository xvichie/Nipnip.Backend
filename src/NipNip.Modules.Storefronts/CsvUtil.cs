using System.Text;

namespace NipNip.Modules.Storefronts;

/// <summary>
/// Minimal RFC4180-ish CSV reader/writer for the product import/export feature — hand-rolled
/// rather than pulling in a CSV library, since the schema here is flat (no nested structures)
/// and only needs to handle quoted fields containing commas/quotes/newlines.
/// </summary>
public static class CsvUtil
{
    public static List<string[]> ParseRows(string content)
    {
        var rows = new List<string[]>();
        var row = new List<string>();
        var field = new StringBuilder();
        var inQuotes = false;
        var i = 0;

        while (i < content.Length)
        {
            var c = content[i];

            if (inQuotes)
            {
                if (c == '"')
                {
                    if (i + 1 < content.Length && content[i + 1] == '"')
                    {
                        field.Append('"');
                        i += 2;
                        continue;
                    }
                    inQuotes = false;
                    i++;
                    continue;
                }
                field.Append(c);
                i++;
                continue;
            }

            switch (c)
            {
                case '"':
                    inQuotes = true;
                    i++;
                    break;
                case ',':
                    row.Add(field.ToString());
                    field.Clear();
                    i++;
                    break;
                case '\r':
                    i++;
                    break;
                case '\n':
                    row.Add(field.ToString());
                    field.Clear();
                    rows.Add(row.ToArray());
                    row = [];
                    i++;
                    break;
                default:
                    field.Append(c);
                    i++;
                    break;
            }
        }

        if (field.Length > 0 || row.Count > 0)
        {
            row.Add(field.ToString());
            rows.Add(row.ToArray());
        }

        return rows.Where(r => r.Length > 1 || !string.IsNullOrWhiteSpace(r[0])).ToList();
    }

    public static string WriteRow(IEnumerable<string> fields) => string.Join(",", fields.Select(WriteField));

    private static string WriteField(string value) =>
        value.IndexOfAny(['"', ',', '\n', '\r']) >= 0
            ? $"\"{value.Replace("\"", "\"\"")}\""
            : value;
}
