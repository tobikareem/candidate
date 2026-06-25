using Recon.Domain;

namespace Recon.App.Reconciliation;

public sealed record PaymentReconciliation(
    IReadOnlyList<PaymentToRemittance> PaymentToRemittance,
    IReadOnlyList<RemittanceToActivities> RemittanceToActivities,
    IReadOnlyList<InvoiceToPayment> InvoiceToPayment);

public static class PaymentReconciler
{
    public static PaymentReconciliation Resolve(StudyData data)
    {
        var paymentToRemittance = new List<PaymentToRemittance>(data.Payments.Count);
        foreach (var payment in data.Payments)
        {
            var remittanceIds = MatchRemittances(payment, data.Remittances)
                .Select(r => r.Id)
                .ToList();
            paymentToRemittance.Add(new PaymentToRemittance(payment.Id, remittanceIds));
        }

        var remittanceToActivities = new List<RemittanceToActivities>(data.Remittances.Count);
        foreach (var remittance in data.Remittances)
        {
            var allocations = new List<RemittanceAllocation>(remittance.Lines.Count);
            foreach (var line in remittance.Lines)
            {
                var invoice = data.Invoices.FirstOrDefault(
                    i => i.PrintedNumber == line.InvoiceRef && i.FaceAmount == line.Gross);
                allocations.Add(new RemittanceAllocation(
                    ActivityId: null,
                    InvoiceId: invoice?.Id,
                    AmountAllocated: line.Paid));
            }
            remittanceToActivities.Add(new RemittanceToActivities(remittance.Id, allocations));
        }

        var invoiceToPayment = new List<InvoiceToPayment>(data.Invoices.Count);
        foreach (var invoice in data.Invoices)
        {
            decimal amountSettled = 0m;
            var settlingRemittanceIds = new List<string>();

            foreach (var remittance in data.Remittances)
            {
                bool settlesThisInvoice = false;
                foreach (var line in remittance.Lines)
                {
                    if (line.InvoiceRef == invoice.PrintedNumber && line.Gross == invoice.FaceAmount)
                    {
                        amountSettled += line.Paid;
                        settlesThisInvoice = true;
                    }
                }
                if (settlesThisInvoice)
                    settlingRemittanceIds.Add(remittance.Id);
            }

            var paymentIds = data.Payments
                .Where(p => MatchRemittances(p, data.Remittances)
                    .Any(r => settlingRemittanceIds.Contains(r.Id)))
                .Select(p => p.Id)
                .Distinct()
                .ToList();

            var status = settlingRemittanceIds.Count > 0 ? InvoiceStatus.Paid : InvoiceStatus.Unpaid;

            invoiceToPayment.Add(new InvoiceToPayment(
                invoice.Id,
                paymentIds,
                invoice.FaceAmount,
                amountSettled,
                status));
        }

        return new PaymentReconciliation(paymentToRemittance, remittanceToActivities, invoiceToPayment);
    }

    private static IEnumerable<Remittance> MatchRemittances(
        Payment payment,
        IReadOnlyList<Remittance> remittances)
    {
        var byRef = remittances.Where(r => r.PrintedRef == payment.PrintedRef).ToList();
        if (byRef.Count > 0)
            return byRef;

        const int nearbyDays = 7;
        return remittances.Where(r =>
            r.NetPaid == payment.Amount &&
            Math.Abs(r.Date.DayNumber - payment.Date.DayNumber) <= nearbyDays);
    }
}
