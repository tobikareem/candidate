using System.Text.Json;
using Recon.App.Api;
using Recon.App.Output;
using Recon.Domain;

namespace Recon.Tests;

/// Unit tests for the API's data layer: the ReconResultsProvider (which builds the reconciliation
/// from documents/+fixtures/ on disk) and the shared projections in CanonicalFileWriter. These prove
/// the API serves the same numbers and shapes the file writer emits. No HTTP involved.
public class ReconApiTests
{
    // The provider loads from disk, so build it once and share it across every test.
    private static readonly ReconResultsProvider Provider = new();

    [Fact]
    public void Provider_builds_all_three_studies()
    {
        Assert.Equal(3, Provider.All.Count);
    }

    [Fact]
    public void Provider_finds_study_by_slug()
    {
        var horizon = Provider.BySlug("study-01-horizon");

        Assert.NotNull(horizon);
        Assert.Equal("MRD-204-017", horizon!.Study.Protocol);
        Assert.Null(Provider.BySlug("nope"));
    }

    [Fact]
    public void Horizon_dashboard_outstanding_ar_via_provider()
    {
        var horizon = Provider.BySlug("study-01-horizon");

        Assert.NotNull(horizon);
        Assert.Equal(13061.60m, horizon!.Dashboard.OutstandingAr);
    }

    [Fact]
    public void Dashboard_payload_serializes_with_snake_case_keys()
    {
        var horizon = Provider.BySlug("study-01-horizon")!;

        var json = JsonSerializer.Serialize(CanonicalFileWriter.DashboardPayload(horizon));

        Assert.Contains("\"outstanding_ar\"", json);
        Assert.Contains("13061.6", json);
    }

    [Fact]
    public void Entity_lookup_returns_null_for_unknown_id()
    {
        Assert.Null(Provider.Store.InvoiceById("does-not-exist"));
    }
}
