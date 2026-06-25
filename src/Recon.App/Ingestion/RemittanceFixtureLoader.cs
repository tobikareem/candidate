using System.Globalization;
using System.Text.Json;
using Recon.App.Store;
using Recon.Domain;

namespace Recon.App.Ingestion;

public sealed class RemittanceFixtureLoader
{
    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
    };

    public void Load(string json, ReconStore store)
    {
        var fixtures = JsonSerializer.Deserialize<List<RemittanceFixture>>(json, JsonOpts)
            ?? throw new InvalidDataException("remittances.json did not deserialize to a list.");

        var remittances = fixtures.Select(f =>
        {
            var id = SurrogateKey.For("remittance", f.PrintedRef);

            var lines = f.Lines.Select(l => new RemittanceLine(
                Id: SurrogateKey.For("remittanceline", id, l.InvoiceRef),
                RemittanceId: id,
                InvoiceRef: l.InvoiceRef,
                Gross: l.Gross,
                Adjustment: l.Adjustment,
                Paid: l.Paid,
                ActivityId: null)).ToList();

            return new Remittance(
                Id: id,
                PrintedRef: f.PrintedRef,
                Payor: f.Payor,
                StudyId: "",
                Date: DateOnly.ParseExact(f.Date.Trim(), "yyyy-MM-dd", CultureInfo.InvariantCulture),
                NetPaid: f.NetPaid,
                Lines: lines)
            {
                SourceDocument = f.SourceDocument,
            };
        }).ToList();

        store.AddRemittances(remittances);
    }
}

internal sealed record RemittanceFixture(
    string SourceDocument,
    string PrintedRef,
    string Payor,
    string Date,
    string Method,
    decimal NetPaid,
    List<RemittanceLineFixture> Lines);

internal sealed record RemittanceLineFixture(
    string InvoiceRef,
    decimal Gross,
    decimal Adjustment,
    decimal Paid);
