using System.Text;

namespace Recon.App.Ingestion;

public sealed class CsvTable
{
    public string[] Header { get; }
    public IReadOnlyList<CsvRow> Rows { get; }

    public CsvTable(string[] header, IReadOnlyList<CsvRow> rows)
    {
        Header = header;
        Rows = rows;
    }
}

public sealed class CsvRow
{
    private readonly IReadOnlyDictionary<string, string> _byColumn;

    public CsvRow(IReadOnlyDictionary<string, string> byColumn) => _byColumn = byColumn;

    public string this[string column] =>
        _byColumn.TryGetValue(column, out var value)
            ? value
            : throw new KeyNotFoundException(
                $"CSV column '{column}' not found. Available: {string.Join(", ", _byColumn.Keys)}");

    public bool Has(string column) => _byColumn.ContainsKey(column);
}

public static class Csv
{
    public static CsvTable Parse(string text)
    {
        var records = SplitRecords(text);
        if (records.Count == 0)
            return new CsvTable(Array.Empty<string>(), Array.Empty<CsvRow>());

        var header = records[0].Select(h => h.Trim()).ToArray();
        var rows = new List<CsvRow>(records.Count - 1);

        for (var i = 1; i < records.Count; i++)
        {
            var fields = records[i];
            if (fields.Count == 1 && fields[0].Length == 0) continue;   // skip blank lines

            var map = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            for (var c = 0; c < header.Length; c++)
                map[header[c]] = c < fields.Count ? fields[c] : string.Empty;

            rows.Add(new CsvRow(map));
        }

        return new CsvTable(header, rows);
    }

    private static List<List<string>> SplitRecords(string text)
    {
        var records = new List<List<string>>();
        var record = new List<string>();
        var field = new StringBuilder();
        var inQuotes = false;

        for (var i = 0; i < text.Length; i++)
        {
            var ch = text[i];

            if (inQuotes)
            {
                if (ch == '"')
                {
                    if (i + 1 < text.Length && text[i + 1] == '"') { field.Append('"'); i++; }
                    else inQuotes = false;
                }
                else field.Append(ch);
                continue;
            }

            switch (ch)
            {
                case '"': inQuotes = true; break;
                case ',': record.Add(field.ToString()); field.Clear(); break;
                case '\r': break;
                case '\n':
                    record.Add(field.ToString()); field.Clear();
                    records.Add(record); record = new List<string>();
                    break;
                default: field.Append(ch); break;
            }
        }

        if (field.Length > 0 || record.Count > 0)
        {
            record.Add(field.ToString());
            records.Add(record);
        }

        return records;
    }
}
