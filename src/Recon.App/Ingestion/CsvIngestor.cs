namespace Recon.App.Ingestion;

public class CsvIngestor<T>
{
    private readonly IReadOnlyList<IDocumentAdapter<T>> _adapters;

    public CsvIngestor(IReadOnlyList<IDocumentAdapter<T>> adapters) => _adapters = adapters;

    // picks the first adapter whose header signature matches. This is important since we do not to trust filenames.
    public bool TryRead(CsvTable table, out IReadOnlyList<T> items)
    {
        var adapter = _adapters.FirstOrDefault(a => a.Handles(table.Header));
        if (adapter is null)
        {
            items = Array.Empty<T>();
            return false;
        }

        items = adapter.Read(table).ToList();
        return true;
    }

    public IReadOnlyList<T> Read(string csvText)
    {
        var table = Csv.Parse(csvText);
        if (TryRead(table, out var items)) return items;

        throw new NotSupportedException(
            $"No adapter recognizes header: {string.Join(",", table.Header)}");
    }
}
