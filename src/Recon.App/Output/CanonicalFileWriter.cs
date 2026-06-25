using System.Text.Json;
using Recon.Domain;

namespace Recon.App.Output;

public static class CanonicalFileWriter
{
    private static readonly JsonSerializerOptions Json = new() { WriteIndented = true };

    public static void Write(ReconciliationResult r, string outRoot)
    {
        var dir = Path.Combine(outRoot, r.Study.OutSlug);
        Directory.CreateDirectory(dir);

        var studyId = r.Study.Protocol;

        WriteFile(dir, "chains.json", new
        {
            study_id = studyId,
            site_id = r.SiteId,
            investigator = r.Investigator,
            payment_to_remittance = r.PaymentToRemittance.Select(x => new
            {
                payment_id = x.PaymentId,
                remittance_ids = x.RemittanceIds,
            }),
            invoice_to_payment = r.InvoiceToPayment.Select(x => new
            {
                invoice_id = x.InvoiceId,
                payment_ids = x.PaymentIds,
                invoice_amount = Money(x.InvoiceAmount),
                amount_settled = Money(x.AmountSettled),
                status = x.Status.ToString().ToLowerInvariant(),
            }),
            invoice_to_activities = r.InvoiceToActivities.Select(x => new
            {
                invoice_id = x.InvoiceId,
                activity_ids = x.ActivityIds,
            }),
            remittance_to_activities = r.RemittanceToActivities.Select(x => new
            {
                remittance_id = x.RemittanceId,
                lines = x.Lines.Select(l => new
                {
                    activity_id = l.ActivityId,
                    invoice_id = l.InvoiceId,
                    amount_allocated = Money(l.AmountAllocated),
                }),
            }),
            activity_to_cta = r.ActivityToCta.Select(x => new
            {
                activity_id = x.ActivityId,
                cta_visit_label = x.CtaVisitLabel,
                cta_amount = x.CtaAmount is { } a ? Money(a) : (decimal?)null,
                match_confidence = x.MatchConfidence?.ToString().ToUpperInvariant(),
            }),
            entity_scope = r.EntityScope.Select(x => new
            {
                entity_type = x.EntityType.ToString().ToLowerInvariant(),
                entity_id = x.EntityId,
                study_id = studyId,
                site_id = x.SiteId,
                investigator = x.Investigator,
            }),
        });

        WriteFile(dir, "dashboard.json", new
        {
            study_id = studyId,
            site_id = r.Dashboard.SiteId,
            investigator = r.Dashboard.Investigator,
            total_billed = Money(r.Dashboard.TotalBilled),
            total_collected = Money(r.Dashboard.TotalCollected),
            outstanding_ar = Money(r.Dashboard.OutstandingAr),
            holdback_withheld = Money(r.Dashboard.HoldbackWithheld),
            unbilled_estimate = Money(r.Dashboard.UnbilledEstimate),
            exceptions_count = r.Dashboard.ExceptionsCount,
            avg_days_to_payment = r.Dashboard.AvgDaysToPayment is { } d ? Money(d) : (decimal?)null,
        });

        WriteFile(dir, "unbilled.json", r.Unbilled.Select(u => new
        {
            subject_id = u.SubjectId,
            proposed_visit_label = u.ProposedVisitLabel,
            estimated_amount = Money(u.EstimatedAmount),
            cta_basis = u.CtaBasis,
            evidence = u.Evidence,
            confidence = u.Confidence?.ToString().ToUpperInvariant(),
        }));

        WriteFile(dir, "unpaid.json", r.Unpaid.Select(u => new
        {
            ref_type = u.RefType.ToString().ToLowerInvariant(),
            ref_id = u.RefId,
            amount_expected = Money(u.AmountExpected),
            age_days = u.AgeDays,
            reason = u.Reason == UnpaidReason.SentNotPaid ? "sent_not_paid" : "autopay_no_deposit",
            evidence = u.Evidence,
            confidence = u.Confidence?.ToString().ToUpperInvariant(),
        }));
    }

    private static decimal Money(decimal value) => Math.Round(value, 2, MidpointRounding.AwayFromZero);

    private static void WriteFile(string dir, string name, object payload) =>
        File.WriteAllText(Path.Combine(dir, name), JsonSerializer.Serialize(payload, Json));
}
