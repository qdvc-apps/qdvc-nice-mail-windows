using System.Text;

namespace Qdvc.NiceMail.Model;

/// <summary>
/// Minimal RFC 4180 CSV reader/writer, compatible with Python's csv module
/// defaults (comma delimiter, double-quote quoting, CRLF line terminator).
/// </summary>
public static class Csv
{
    public static List<List<string>> Parse(string text)
    {
        var rows = new List<List<string>>();
        var row = new List<string>();
        var field = new StringBuilder();
        bool inQuotes = false, fieldStarted = false;

        for (int i = 0; i < text.Length; i++)
        {
            char c = text[i];
            if (inQuotes)
            {
                if (c == '"')
                {
                    if (i + 1 < text.Length && text[i + 1] == '"') { field.Append('"'); i++; }
                    else inQuotes = false;
                }
                else field.Append(c);
                continue;
            }

            switch (c)
            {
                case '"':
                    inQuotes = true;
                    fieldStarted = true;
                    break;
                case ',':
                    row.Add(field.ToString());
                    field.Clear();
                    fieldStarted = true;
                    break;
                case '\r':
                    break; // handled with '\n'; a lone CR is dropped
                case '\n':
                    if (fieldStarted || field.Length > 0 || row.Count > 0)
                    {
                        row.Add(field.ToString());
                        rows.Add(row);
                    }
                    row = new List<string>();
                    field.Clear();
                    fieldStarted = false;
                    break;
                default:
                    field.Append(c);
                    fieldStarted = true;
                    break;
            }
        }
        if (fieldStarted || field.Length > 0 || row.Count > 0)
        {
            row.Add(field.ToString());
            rows.Add(row);
        }
        return rows;
    }

    /// <summary>Parses a CSV with a header row into dictionaries keyed by column name.</summary>
    public static List<Dictionary<string, string>> ParseWithHeader(string text)
    {
        var rows = Parse(text.TrimStart('\uFEFF'));
        var result = new List<Dictionary<string, string>>();
        if (rows.Count == 0) return result;
        var header = rows[0].Select(h => h.Trim()).ToList();
        foreach (var r in rows.Skip(1))
        {
            var d = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            for (int i = 0; i < header.Count; i++)
                d[header[i]] = i < r.Count ? r[i] : "";
            result.Add(d);
        }
        return result;
    }

    public static string Format(IEnumerable<IEnumerable<string>> rows)
    {
        var sb = new StringBuilder();
        foreach (var row in rows)
        {
            sb.Append(string.Join(",", row.Select(Quote)));
            sb.Append("\r\n");
        }
        return sb.ToString();
    }

    private static string Quote(string value)
    {
        if (value.IndexOfAny(new[] { ',', '"', '\r', '\n' }) < 0) return value;
        return "\"" + value.Replace("\"", "\"\"") + "\"";
    }
}
