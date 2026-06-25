using Recon.App.Store;

namespace Recon.App.Ingestion;
public sealed class DocumentLoader
{
    private readonly CtmsIngestor _ctms = new();
    private readonly PaymentIngestor _payments = new();
    private readonly AutopayIngestor _autopays = new();
    private readonly BankIngestor _bank = new();
    private readonly CtaFixtureLoader _ctas = new();
    private readonly InvoiceFixtureLoader _invoices = new();
    private readonly RemittanceFixtureLoader _remittances = new();
    private readonly CommFixtureLoader _comms = new();

    public ReconStore Load(string documentsDir, string fixturesDir)
    {
        var store = new ReconStore();

        _ctas.Load(File.ReadAllText(Path.Combine(fixturesDir, "ctas.json")), store);

        var invoicesPath = Path.Combine(fixturesDir, "invoices.json");
        if (File.Exists(invoicesPath))
            _invoices.Load(File.ReadAllText(invoicesPath), store);

        var remittancesPath = Path.Combine(fixturesDir, "remittances.json");
        if (File.Exists(remittancesPath))
            _remittances.Load(File.ReadAllText(remittancesPath), store);

        var commsPath = Path.Combine(fixturesDir, "comms.json");
        if (File.Exists(commsPath))
            _comms.Load(File.ReadAllText(commsPath), store);

        foreach (var path in Directory.EnumerateFiles(documentsDir, "*.csv"))
        {
            var table = Csv.Parse(File.ReadAllText(path));
            var file = Path.GetFileName(path);   

            if (_ctms.TryRead(table, out var activities))
                store.AddActivities(activities.Select(a => a with { SourceDocument = file }));
            else if (_payments.TryRead(table, out var payments))
                store.AddPayments(payments.Select(p => p with { SourceDocument = file }));
            else if (_autopays.TryRead(table, out var autopays))
                store.AddAutopays(autopays.Select(a => a with { SourceDocument = file }));
            else if (_bank.TryRead(table, out var bank))
                store.AddBankTransactions(bank.Select(b => b with { SourceDocument = file }));
            else throw new NotSupportedException(
                $"No adapter recognizes CSV '{Path.GetFileName(path)}' (header: {string.Join(",", table.Header)}).");
        }

        return store;
    }
}
