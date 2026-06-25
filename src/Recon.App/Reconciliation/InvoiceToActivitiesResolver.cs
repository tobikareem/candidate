using Recon.Domain;

namespace Recon.App.Reconciliation;


public static class InvoiceToActivitiesResolver
{
    private const int ServiceDateWindowDays = 10;

    public static IReadOnlyList<InvoiceToActivities> Resolve(StudyData data)
    {
        var results = new List<InvoiceToActivities>(data.Invoices.Count);

        foreach (var invoice in data.Invoices)
        {
            if (invoice.Kind == InvoiceKind.SiteFee || invoice.SubjectId is null)
            {
                results.Add(new InvoiceToActivities(invoice.Id, []));
                continue;
            }

            var candidates = data.Activities
                .Where(a => a.SubjectId == invoice.SubjectId)
                .ToList();

            var lineLabels = invoice.Lines
                .Select(l => Normalize(l.VisitLabel))
                .Where(l => l.Length > 0)
                .ToHashSet();

            var labelMatches = candidates
                .Where(a => lineLabels.Contains(Normalize(a.VisitLabelNorm)))
                .Select(a => a.Id)
                .ToList();

            if (labelMatches.Count > 0)
            {
                results.Add(new InvoiceToActivities(invoice.Id, labelMatches));
                continue;
            }

            var dateMatch = candidates
                .Select(a => new { a.Id, Delta = Math.Abs(a.ServiceDate.DayNumber - invoice.ServiceDate.DayNumber) })
                .Where(x => x.Delta <= ServiceDateWindowDays)
                .OrderBy(x => x.Delta)
                .Select(x => x.Id)
                .FirstOrDefault();

            results.Add(new InvoiceToActivities(
                invoice.Id,
                dateMatch is null ? [] : [dateMatch]));
        }

        return results;
    }

    private static string Normalize(string? label)
    {
        if (string.IsNullOrWhiteSpace(label))
            return string.Empty;

        return new string(label.Where(char.IsLetterOrDigit).ToArray()).ToLowerInvariant();
    }
}
