using Recon.App.Ingestion;
using Recon.App.Store;
using Recon.Domain;

namespace Recon.Tests;

public class CommFixtureLoaderTests
{
    private const string Json = """
    [
      {
        "source_document": "slack_2026-06-02.md",
        "channel": "slack",
        "date": "2026-06-02",
        "body": "INV-105 is unpaid.",
        "facts": [
          { "kind": "ConfirmsUnpaid", "target_ref": "INV-105", "note": "Only open item." }
        ]
      }
    ]
    """;

    [Fact]
    public void Loads_comm_facts_with_source_document()
    {
        var store = new ReconStore();
        new CommFixtureLoader().Load(Json, store);

        var comm = Assert.Single(store.Comms);
        Assert.Equal("slack", comm.Channel);
        Assert.Equal(new DateOnly(2026, 6, 2), comm.Date);
        Assert.Equal("slack_2026-06-02.md", comm.SourceDocument);

        var fact = Assert.Single(comm.Facts);
        Assert.Equal(CommFactKind.ConfirmsUnpaid, fact.Kind);
        Assert.Equal("INV-105", fact.TargetRef);
    }
}
