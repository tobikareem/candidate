using Recon.Domain;

namespace Recon.App.Store;

public sealed class ReconStore
{
    private readonly List<Activity> _activities = new();
    private readonly Dictionary<string, Activity> _activityById = new();
    private readonly List<Payment> _payments = new();
    private readonly Dictionary<string, Payment> _paymentById = new();
    private readonly List<Autopay> _autopays = new();
    private readonly Dictionary<string, Autopay> _autopayById = new();
    private readonly List<BankTransaction> _bankTransactions = new();
    private readonly Dictionary<string, BankTransaction> _bankTransactionById = new();
    private readonly List<Study> _studies = new();
    private readonly Dictionary<string, Study> _studyById = new();
    private readonly List<CtaBudgetLine> _ctaBudgetLines = new();
    private readonly Dictionary<string, CtaBudgetLine> _ctaBudgetLineById = new();
    private readonly List<Site> _sites = new();
    private readonly Dictionary<string, Site> _siteById = new();
    private readonly List<Investigator> _investigators = new();
    private readonly Dictionary<string, Investigator> _investigatorById = new();
    private readonly List<Invoice> _invoices = new();
    private readonly Dictionary<string, Invoice> _invoiceById = new();
    private readonly List<Remittance> _remittances = new();
    private readonly Dictionary<string, Remittance> _remittanceById = new();
    private readonly List<Comm> _comms = new();
    private readonly Dictionary<string, Comm> _commById = new();

    public IReadOnlyList<Activity> Activities => _activities;
    public IReadOnlyList<Payment> Payments => _payments;
    public IReadOnlyList<Autopay> Autopays => _autopays;
    public IReadOnlyList<BankTransaction> BankTransactions => _bankTransactions;
    public IReadOnlyList<Study> Studies => _studies;
    public IReadOnlyList<CtaBudgetLine> CtaBudgetLines => _ctaBudgetLines;
    public IReadOnlyList<Site> Sites => _sites;
    public IReadOnlyList<Investigator> Investigators => _investigators;
    public IReadOnlyList<Invoice> Invoices => _invoices;
    public IReadOnlyList<Remittance> Remittances => _remittances;
    public IReadOnlyList<Comm> Comms => _comms;

    public void AddActivities(IEnumerable<Activity> items)
    {
        foreach (var item in items) Add(item, item.Id, _activities, _activityById, "activity");
    }

    public void AddPayments(IEnumerable<Payment> items)
    {
        foreach (var item in items) Add(item, item.Id, _payments, _paymentById, "payment");
    }

    public void AddAutopays(IEnumerable<Autopay> items)
    {
        foreach (var item in items) Add(item, item.Id, _autopays, _autopayById, "autopay");
    }

    public void AddBankTransactions(IEnumerable<BankTransaction> items)
    {
        foreach (var item in items) Add(item, item.Id, _bankTransactions, _bankTransactionById, "bank transaction");
    }

    public void AddStudies(IEnumerable<Study> items)
    {
        foreach (var item in items) Add(item, item.Id, _studies, _studyById, "study");
    }

    public void AddCtaBudgetLines(IEnumerable<CtaBudgetLine> items)
    {
        foreach (var item in items) Add(item, item.Id, _ctaBudgetLines, _ctaBudgetLineById, "CTA budget line");
    }

    public void AddSites(IEnumerable<Site> items)
    {
        foreach (var item in items) Add(item, item.Id, _sites, _siteById, "site");
    }

    public void AddInvestigators(IEnumerable<Investigator> items)
    {
        foreach (var item in items) Add(item, item.Id, _investigators, _investigatorById, "investigator");
    }

    public void AddInvoices(IEnumerable<Invoice> items)
    {
        foreach (var item in items) Add(item, item.Id, _invoices, _invoiceById, "invoice");
    }

    public void AddRemittances(IEnumerable<Remittance> items)
    {
        foreach (var item in items) Add(item, item.Id, _remittances, _remittanceById, "remittance");
    }

    public void AddComms(IEnumerable<Comm> items)
    {
        foreach (var item in items) Add(item, item.Id, _comms, _commById, "comm");
    }

    public Activity? ActivityById(string id) => _activityById.GetValueOrDefault(id);
    public Payment? PaymentById(string id) => _paymentById.GetValueOrDefault(id);
    public Autopay? AutopayById(string id) => _autopayById.GetValueOrDefault(id);
    public BankTransaction? BankTransactionById(string id) => _bankTransactionById.GetValueOrDefault(id);
    public Study? StudyById(string id) => _studyById.GetValueOrDefault(id);
    public CtaBudgetLine? CtaBudgetLineById(string id) => _ctaBudgetLineById.GetValueOrDefault(id);
    public Site? SiteById(string id) => _siteById.GetValueOrDefault(id);
    public Investigator? InvestigatorById(string id) => _investigatorById.GetValueOrDefault(id);
    public Invoice? InvoiceById(string id) => _invoiceById.GetValueOrDefault(id);
    public Remittance? RemittanceById(string id) => _remittanceById.GetValueOrDefault(id);
    public Comm? CommById(string id) => _commById.GetValueOrDefault(id);

    private static void Add<T>(T item, string id, List<T> list, Dictionary<string, T> byId, string kind)
    {
        if (!byId.TryAdd(id, item))
            throw new ArgumentException($"Duplicate {kind} id '{id}'.", nameof(item));

        list.Add(item);
    }
}
