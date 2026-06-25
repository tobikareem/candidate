using Recon.Domain;

namespace Recon.App.Reconciliation;

public sealed record StudyData(
    Study Study,
    IReadOnlyList<Invoice> Invoices,
    IReadOnlyList<Payment> Payments,
    IReadOnlyList<Remittance> Remittances,
    IReadOnlyList<Activity> Activities,
    IReadOnlyList<Autopay> Autopays,
    IReadOnlyList<CtaBudgetLine> CtaLines);
