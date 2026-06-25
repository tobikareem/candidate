using System.Globalization;
using System.Text.Json;
using Recon.App.Store;
using Recon.Domain;

namespace Recon.App.Ingestion;

/// Loads committed invoice fixture rows extracted once from invoice PDFs.
public sealed class InvoiceFixtureLoader
{
    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
    };

    public void Load(string json, ReconStore store)
    {
        var fixtures = JsonSerializer.Deserialize<List<InvoiceFixture>>(json, JsonOpts)
            ?? throw new InvalidDataException("invoices.json did not deserialize to a list.");

        var invoices = fixtures.Select(ToInvoice).ToList();
        store.AddInvoices(invoices);
    }

    private static Invoice ToInvoice(InvoiceFixture f)
    {
        var invoiceId = SurrogateKey.For("invoice", f.PrintedStudyCode, f.PrintedNumber);
        var line = new InvoiceLine(
            Id: SurrogateKey.For("invoiceline", invoiceId, f.ServiceDate, f.Description),
            InvoiceId: invoiceId,
            VisitLabel: Text.NormalizeLabel(f.Description),
            ActivityId: null,
            Amount: f.FaceAmount);

        return new Invoice(
            Id: invoiceId,
            PrintedNumber: f.PrintedNumber,
            PrintedStudyCode: f.PrintedStudyCode,
            Payor: f.Payor,
            StudyId: "",
            SubjectId: f.SubjectId,
            IssueDate: ParseDate(f.IssueDate),
            ServiceDate: ParseDate(f.ServiceDate),
            FaceAmount: f.FaceAmount,
            Kind: ParseKind(f.Kind),
            Lines: new[] { line })
        {
            SourceDocument = f.SourceDocument,
        };
    }

    private static DateOnly ParseDate(string iso) =>
        DateOnly.ParseExact(iso.Trim(), "yyyy-MM-dd", CultureInfo.InvariantCulture);

    private static InvoiceKind ParseKind(string kind) =>
        Enum.TryParse<InvoiceKind>(kind, ignoreCase: true, out var parsed) ? parsed : InvoiceKind.Unknown;
}

internal sealed record InvoiceFixture(
    string SourceDocument,
    string PrintedNumber,
    string PrintedStudyCode,
    string Payor,
    string? SubjectId,
    string IssueDate,
    string ServiceDate,
    decimal FaceAmount,
    string Kind,
    string Description);
