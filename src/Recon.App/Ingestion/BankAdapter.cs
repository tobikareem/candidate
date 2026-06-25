using System.Globalization;
using Recon.Domain;

namespace Recon.App.Ingestion;

/// plaid_transactions.csv — date,name,amount,iso_currency_code,category,account_id,transaction_id
/// The bank feed is the ground truth of what cash actually moved. 
public sealed class PlaidAdapter : IBankAdapter
{
    public bool Handles(IReadOnlyList<string> header) =>
        HeaderSignature.HasAll(header, "date", "name", "amount", "category", "transaction_id");

    public IEnumerable<BankTransaction> Read(CsvTable table)
    {
        foreach (var row in table.Rows)
        {
            var amount = BankMap.Money(row["amount"]);
            yield return new BankTransaction(
                Id: SurrogateKey.For("bank", "plaid", row["transaction_id"]),
                ExternalId: row["transaction_id"],
                Date: BankMap.Date(row["date"]),
                RawName: row["name"],
                Category: row["category"],
                Amount: amount,
                IsDeposit: amount > 0m,
                MatchedStudyId: null,
                MatchedTo: null);
        }
    }
}

/// Bank ingestor: currently the single Plaid adapter, behind the same generic router as the rest.
public sealed class BankIngestor : CsvIngestor<BankTransaction>
{
    public BankIngestor(IReadOnlyList<IDocumentAdapter<BankTransaction>>? adapters = null)
        : base(adapters ?? [new PlaidAdapter()])
    { }
}

internal static class BankMap
{
    public static DateOnly Date(string iso) =>
        DateOnly.ParseExact(iso.Trim(), "yyyy-MM-dd", CultureInfo.InvariantCulture);

    public static decimal Money(string raw) =>
        decimal.Parse(raw.Trim(), NumberStyles.Number, CultureInfo.InvariantCulture);
}
