using Recon.Domain;

namespace Recon.App.Ingestion;


public interface IDocumentAdapter<out T>
{
    bool Handles(IReadOnlyList<string> header);
    IEnumerable<T> Read(CsvTable table);
}
public interface ICtmsAdapter : IDocumentAdapter<Activity> { }
public interface IPaymentAdapter : IDocumentAdapter<Payment> { }
public interface IAutopayAdapter : IDocumentAdapter<Autopay> { }
public interface IBankAdapter : IDocumentAdapter<BankTransaction> { }

internal static class HeaderSignature
{
    public static bool HasAll(IReadOnlyList<string> header, params string[] columns) =>
        columns.All(col => header.Any(h => string.Equals(h, col, StringComparison.OrdinalIgnoreCase)));
}
