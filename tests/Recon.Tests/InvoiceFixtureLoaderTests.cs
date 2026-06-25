using Recon.App.Ingestion;
using Recon.App.Store;
using Recon.Domain;

namespace Recon.Tests;

public class InvoiceFixtureLoaderTests
{
    private const string Json = """
    [
      {
        "source_document": "INV-002_S-12-002_2022-05-11.pdf",
        "printed_number": "INV-002",
        "printed_study_code": "HORIZON",
        "payor": "Meridian Therapeutics, Inc.",
        "subject_id": "S-12-002",
        "issue_date": "2022-05-16",
        "service_date": "2022-05-11",
        "face_amount": 2153.13,
        "kind": "SubjectVisit",
        "description": "Screening Visit + 25% overhead"
      }
    ]
    """;

    [Fact]
    public void Loads_invoice_fixture_into_store()
    {
        var store = new ReconStore();
        new InvoiceFixtureLoader().Load(Json, store);

        var invoice = Assert.Single(store.Invoices);
        Assert.Equal("INV-002", invoice.PrintedNumber);
        Assert.Equal("HORIZON", invoice.PrintedStudyCode);
        Assert.Equal("Meridian Therapeutics, Inc.", invoice.Payor);
        Assert.Equal("S-12-002", invoice.SubjectId);
        Assert.Equal(new DateOnly(2022, 5, 16), invoice.IssueDate);
        Assert.Equal(new DateOnly(2022, 5, 11), invoice.ServiceDate);
        Assert.Equal(2153.13m, invoice.FaceAmount);
        Assert.Equal(InvoiceKind.SubjectVisit, invoice.Kind);
        Assert.Equal("INV-002_S-12-002_2022-05-11.pdf", invoice.SourceDocument);
        Assert.Equal("", invoice.StudyId);
    }

    [Fact]
    public void Creates_summary_line_for_invoice_amount()
    {
        var store = new ReconStore();
        new InvoiceFixtureLoader().Load(Json, store);

        var invoice = store.Invoices.Single();
        var line = Assert.Single(invoice.Lines);
        Assert.Equal(invoice.Id, line.InvoiceId);
        Assert.Equal(2153.13m, line.Amount);
        Assert.Null(line.ActivityId);
    }

    [Fact]
    public void Invoice_id_uses_printed_study_and_number_for_cross_study_reuse()
    {
        const string json = """
        [
          { "source_document": "h.pdf", "printed_number": "INV-100", "printed_study_code": "HORIZON", "payor": "Meridian", "subject_id": null, "issue_date": "2022-01-01", "service_date": "2022-01-01", "face_amount": 1, "kind": "SiteFee", "description": "Fee" },
          { "source_document": "n.pdf", "printed_number": "INV-100", "printed_study_code": "NORTHSTAR", "payor": "Calyx", "subject_id": null, "issue_date": "2026-01-01", "service_date": "2026-01-01", "face_amount": 1, "kind": "SiteFee", "description": "Fee" }
        ]
        """;

        var store = new ReconStore();
        new InvoiceFixtureLoader().Load(json, store);

        Assert.Equal(2, store.Invoices.Count);
        Assert.Equal(2, store.Invoices.Select(i => i.Id).Distinct().Count());
    }
}
