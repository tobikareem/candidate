using Recon.App.Ingestion;
using Recon.App.Store;

namespace Recon.Tests;

public class RemittanceFixtureLoaderTests
{
    private const string Json = """
    [
      {
        "source_document": "R-002_Cordis_Payment_Advice.pdf",
        "printed_ref": "R-002",
        "payor": "Cordis Clinical",
        "date": "2022-07-05",
        "method": "ACH",
        "net_paid": 1937.82,
        "lines": [
          { "invoice_ref": "INV-002", "gross": 2153.13, "adjustment": -215.31, "paid": 1937.82 }
        ]
      }
    ]
    """;

    [Fact]
    public void Loads_remittance_with_line_allocation()
    {
        var store = new ReconStore();
        new RemittanceFixtureLoader().Load(Json, store);

        var remittance = Assert.Single(store.Remittances);
        Assert.Equal("R-002", remittance.PrintedRef);
        Assert.Equal("Cordis Clinical", remittance.Payor);
        Assert.Equal(new DateOnly(2022, 7, 5), remittance.Date);
        Assert.Equal(1937.82m, remittance.NetPaid);
        Assert.Equal("R-002_Cordis_Payment_Advice.pdf", remittance.SourceDocument);
        Assert.Equal("", remittance.StudyId);

        var line = Assert.Single(remittance.Lines);
        Assert.Equal("INV-002", line.InvoiceRef);
        Assert.Equal(2153.13m, line.Gross);
        Assert.Equal(-215.31m, line.Adjustment);
        Assert.Equal(1937.82m, line.Paid);
    }
}
