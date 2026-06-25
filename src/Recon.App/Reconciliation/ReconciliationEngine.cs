using Recon.App.Classification;
using Recon.App.Store;
using Recon.Domain;

namespace Recon.App.Reconciliation;

public sealed class ReconciliationEngine
{
    private const int DepositDateWindowDays = 5;
    private const int DirectPaymentWindowDays = 365;

    public IReadOnlyList<ReconciliationResult> Build(ReconStore store)
    {
        var resolver = new StudyResolver(store);
        var classifier = new Classifier(store, resolver);
        var studyByEntity = classifier.StudyByEntityId();
        var allScopes = classifier.BuildEntityScopes();
        var deposits = store.BankTransactions.Where(b => b.IsDeposit).OrderBy(b => b.Date).ToList();

        var results = new List<ReconciliationResult>();

        foreach (var study in store.Studies)
        {
            var sid = study.Id;
            bool Mine(string id) => studyByEntity.TryGetValue(id, out var s) && s == sid;

            var invoices = store.Invoices.Where(i => Mine(i.Id)).ToList();
            var payments = store.Payments.Where(p => Mine(p.Id)).ToList();
            var remittances = store.Remittances.Where(r => Mine(r.Id)).ToList();
            var activities = store.Activities.Where(a => Mine(a.Id)).ToList();
            var autopays = store.Autopays
                .Where(a => resolver.Resolve(a.SubjectId, null, null).StudyId == sid)
                .OrderBy(a => a.ScheduledDate)
                .ToList();
            var ctaLines = store.CtaBudgetLines.Where(c => c.StudyId == sid).ToList();

            var data = new StudyData(study, invoices, payments, remittances, activities, autopays, ctaLines);

            var activityToCta = ActivityToCtaResolver.Resolve(data);
            var invoiceToActivities = InvoiceToActivitiesResolver.Resolve(data);
            var pay = PaymentReconciler.Resolve(data);
            var entityScope = allScopes.Where(s => s.StudyId == sid).ToList();
            var autopayDepositMatches = MatchAutopayDeposits(study, autopays, deposits);
            var remittanceDepositMatches = MatchRemittanceDeposits(study, remittances, deposits);

            var totalCollected = remittanceDepositMatches.Values.Sum(d => d.Amount)
                + autopayDepositMatches.Values.Sum(d => d.Amount);

            var settled = invoices.Where(inv =>
                    HasDirectPayment(inv, study, payments, deposits) ||
                    pay.InvoiceToPayment.Any(p => p.InvoiceId == inv.Id && p.Status != InvoiceStatus.Unpaid))
                .Select(i => i.Id)
                .ToHashSet();

            var openInvoices = pay.InvoiceToPayment
                .Where(x => x.Status == InvoiceStatus.Unpaid && !settled.Contains(x.InvoiceId))
                .ToList();
            var outstandingAr = openInvoices.Sum(x => x.InvoiceAmount);

            var unpaid = BuildUnpaid(invoices, openInvoices, autopays, autopayDepositMatches);
            var unbilled = BuildUnbilled(data, activityToCta);
            var exceptions = BuildExceptions(study, invoices, autopays, remittances, deposits, autopayDepositMatches);
            var investigator = store.Investigators.FirstOrDefault(i => i.StudyId == sid)?.Name;
            var dashboard = BuildDashboard(study, invoices, autopays, remittances, payments,
                                           totalCollected, outstandingAr, unbilled, exceptions,
                                           study.SiteId, investigator);

            results.Add(new ReconciliationResult(
                Study: study,
                SiteId: study.SiteId,
                Investigator: investigator,
                PaymentToRemittance: pay.PaymentToRemittance,
                InvoiceToPayment: pay.InvoiceToPayment,
                InvoiceToActivities: invoiceToActivities,
                RemittanceToActivities: pay.RemittanceToActivities,
                ActivityToCta: activityToCta,
                EntityScope: entityScope,
                Dashboard: dashboard,
                Unbilled: unbilled,
                Unpaid: unpaid,
                Exceptions: exceptions));
        }

        return results;
    }

    private static Dictionary<string, BankTransaction> MatchRemittanceDeposits(
        Study study,
        IReadOnlyList<Remittance> remittances,
        IReadOnlyList<BankTransaction> deposits)
    {
        var used = new HashSet<string>();
        var matches = new Dictionary<string, BankTransaction>();

        foreach (var r in remittances.OrderBy(r => r.Date))
        {
            var dep = deposits.FirstOrDefault(d =>
                !used.Contains(d.Id) &&
                d.Amount == r.NetPaid &&
                DateWithin(d.Date, r.Date, DepositDateWindowDays) &&
                (PayorMatches(d.RawName, study) || NameContainsToken(d.RawName, r.Payor)));

            if (dep is null) continue;
            used.Add(dep.Id);
            matches[r.Id] = dep;
        }

        return matches;
    }

    private static Dictionary<string, BankTransaction> MatchAutopayDeposits(
        Study study,
        IReadOnlyList<Autopay> autopays,
        IReadOnlyList<BankTransaction> deposits)
    {
        var used = new HashSet<string>();
        var matches = new Dictionary<string, BankTransaction>();

        foreach (var a in autopays)
        {
            var dep = deposits.FirstOrDefault(d =>
                !used.Contains(d.Id) &&
                d.Amount == a.ScheduledAmount &&
                PayorMatches(d.RawName, study) &&
                DateWithin(d.Date, a.ScheduledDate, DepositDateWindowDays));

            if (dep is null) continue;
            used.Add(dep.Id);
            matches[a.Id] = dep;
        }

        // Wrong-amount autopays still landed, so they are exceptions rather than unpaid items.
        foreach (var a in autopays.Where(a => !matches.ContainsKey(a.Id)))
        {
            var dep = deposits.FirstOrDefault(d =>
                !used.Contains(d.Id) &&
                PayorMatches(d.RawName, study) &&
                DateWithin(d.Date, a.ScheduledDate, DepositDateWindowDays));

            if (dep is null) continue;
            used.Add(dep.Id);
            matches[a.Id] = dep;
        }

        return matches;
    }

    private static bool HasDirectPayment(
        Invoice invoice,
        Study study,
        IReadOnlyList<Payment> payments,
        IReadOnlyList<BankTransaction> deposits)
    {
        return payments.Any(p =>
                p.Amount == invoice.FaceAmount &&
                DateWithin(p.Date, invoice.IssueDate, DirectPaymentWindowDays)) ||
            deposits.Any(d =>
                d.Amount == invoice.FaceAmount &&
                PayorMatches(d.RawName, study) &&
                DateWithin(d.Date, invoice.IssueDate, DirectPaymentWindowDays));
    }

    private static List<UnpaidItem> BuildUnpaid(
        IReadOnlyList<Invoice> invoices,
        IReadOnlyList<InvoiceToPayment> openInvoices,
        IReadOnlyList<Autopay> autopays,
        IReadOnlyDictionary<string, BankTransaction> autopayDepositMatches)
    {
        var items = new List<UnpaidItem>();

        foreach (var itp in openInvoices)
        {
            var inv = invoices.First(i => i.Id == itp.InvoiceId);
            items.Add(new UnpaidItem(UnpaidRefType.Invoice, inv.PrintedNumber, inv.FaceAmount,
                AgeDays: null, UnpaidReason.SentNotPaid, "issued; no remittance or deposit", MatchConfidence.High));
        }

        foreach (var a in autopays.Where(a => a.Status == AutopayStatus.Authorized && !autopayDepositMatches.ContainsKey(a.Id)))
        {
            items.Add(new UnpaidItem(UnpaidRefType.Autopay, a.PrintedRef, a.ScheduledAmount,
                AgeDays: null, UnpaidReason.AutopayNoDeposit, "authorized; never deposited", MatchConfidence.High));
        }

        return items;
    }

    private static List<UnbilledItem> BuildUnbilled(StudyData data, IReadOnlyList<ActivityToCta> activityToCta)
    {
        var items = new List<UnbilledItem>();

        var overhead = 1m + data.Study.OverheadPct;

        foreach (var act in data.Activities)
        {
            var invoiced = data.Invoices.Any(i =>
                i.SubjectId == act.SubjectId && Math.Abs(i.ServiceDate.DayNumber - act.ServiceDate.DayNumber) <= 10);
            // Coverage is per VISIT, not just per subject+date: an autopay covers an activity only when
            // it is for the same visit label. Otherwise an unscheduled draw sitting near a scheduled
            // visit's autopay would be wrongly treated as paid (the ASCEND repeat-hematology case).
            var autopaid = data.Autopays.Any(ap =>
                ap.SubjectId == act.SubjectId &&
                string.Equals((ap.VisitLabel ?? string.Empty).Trim(), act.VisitLabelNorm.Trim(),
                    StringComparison.OrdinalIgnoreCase));
            if (invoiced || autopaid) continue;

            var cta = activityToCta.FirstOrDefault(c => c.ActivityId == act.Id);
            var estimate = (cta?.CtaAmount ?? 0m) * overhead;
            items.Add(new UnbilledItem(act.SubjectId, act.VisitLabelNorm, estimate,
                cta?.CtaVisitLabel, "CTMS visit with no invoice or autopay", cta?.MatchConfidence));
        }

        return items;
    }

    private static List<ReconException> BuildExceptions(
        Study study,
        IReadOnlyList<Invoice> invoices,
        IReadOnlyList<Autopay> autopays,
        IReadOnlyList<Remittance> remittances,
        IReadOnlyList<BankTransaction> deposits,
        IReadOnlyDictionary<string, BankTransaction> autopayDepositMatches)
    {
        var ex = new List<ReconException>();
        var remitAmounts = remittances.Select(r => r.NetPaid).ToHashSet();

        foreach (var a in autopays)
        {
            if (autopayDepositMatches.TryGetValue(a.Id, out var dep) && dep.Amount != a.ScheduledAmount)
            {
                ex.Add(new ReconException(ExceptionKind.WrongAmount, dep.ExternalId,
                    $"autopay {a.PrintedRef} deposited {dep.Amount:0.00}; expected {a.ScheduledAmount:0.00}"));
            }
        }

        foreach (var d in deposits.Where(d => PayorMatches(d.RawName, study)))
        {
            var usedForAutopay = autopayDepositMatches.Values.Any(m => m.Id == d.Id);
            if (!usedForAutopay && !remitAmounts.Contains(d.Amount))
            {
                ex.Add(new ReconException(ExceptionKind.WrongAmount, d.ExternalId,
                    $"deposit {d.Amount:0.00} matches no scheduled autopay or remittance"));
            }
        }

        foreach (var inv in invoices)
            foreach (var cap in study.Caps)
                if (inv.FaceAmount > cap.Value &&
                    inv.Lines.Any(l => (l.VisitLabel ?? "").Contains(cap.Key, StringComparison.OrdinalIgnoreCase)))
                    ex.Add(new ReconException(ExceptionKind.CapBreach, inv.PrintedNumber,
                        $"{cap.Key} {inv.FaceAmount:0.00} exceeds cap {cap.Value:0.00}"));

        return ex;
    }

    private static Dashboard BuildDashboard(
        Study study,
        IReadOnlyList<Invoice> invoices,
        IReadOnlyList<Autopay> autopays,
        IReadOnlyList<Remittance> remittances,
        IReadOnlyList<Payment> payments,
        decimal totalCollected,
        decimal outstandingAr,
        IReadOnlyList<UnbilledItem> unbilled,
        IReadOnlyList<ReconException> exceptions,
        string? siteId,
        string? investigator)
    {
        var totalBilled = invoices.Sum(i => i.FaceAmount) + autopays.Sum(a => a.ScheduledAmount);
        var holdback = remittances.SelectMany(r => r.Lines).Sum(l => Math.Abs(l.Adjustment));

        return new Dashboard(
            StudyId: study.Protocol,
            SiteId: siteId,
            Investigator: investigator,
            TotalBilled: totalBilled,
            TotalCollected: totalCollected,
            OutstandingAr: outstandingAr,
            HoldbackWithheld: holdback,
            UnbilledEstimate: unbilled.Sum(u => u.EstimatedAmount),
            ExceptionsCount: exceptions.Count,
            AvgDaysToPayment: AverageDaysToPayment(invoices, remittances, payments));
    }

    private static decimal? AverageDaysToPayment(
        IReadOnlyList<Invoice> invoices,
        IReadOnlyList<Remittance> remittances,
        IReadOnlyList<Payment> payments)
    {
        var days = new List<int>();

        foreach (var inv in invoices)
        {
            var remitDate = remittances
                .Where(r => r.Lines.Any(l => l.InvoiceRef == inv.PrintedNumber && l.Gross == inv.FaceAmount))
                .Select(r => (DateOnly?)r.Date)
                .OrderBy(d => d)
                .FirstOrDefault();

            var paymentDate = remitDate ?? payments
                .Where(p => p.Amount == inv.FaceAmount)
                .Select(p => (DateOnly?)p.Date)
                .OrderBy(d => d)
                .FirstOrDefault();

            if (paymentDate is { } paid)
                days.Add(paid.DayNumber - inv.IssueDate.DayNumber);
        }

        return days.Count == 0 ? null : Math.Round((decimal)days.Average(), 1, MidpointRounding.AwayFromZero);
    }

    private static bool DateWithin(DateOnly actual, DateOnly expected, int days) =>
        actual.DayNumber >= expected.DayNumber && actual.DayNumber - expected.DayNumber <= days;

    private static bool PayorMatches(string rawName, Study study)
    {
        var token = study.Sponsor.Split(',', ' ')[0];
        return rawName.Contains(token, StringComparison.OrdinalIgnoreCase);
    }

    private static bool NameContainsToken(string rawName, string name)
    {
        var token = name.Split(',', ' ')[0];
        return rawName.Contains(token, StringComparison.OrdinalIgnoreCase);
    }
}

