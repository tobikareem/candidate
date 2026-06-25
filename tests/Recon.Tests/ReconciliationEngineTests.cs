using Recon.App.Ingestion;
using Recon.App.Reconciliation;
using Recon.Domain;

namespace Recon.Tests;

/// Locks the public-sample numbers into the unit suite (no Python needed). Each assertion mirrors a
/// check in conformance/public-sample.
public class ReconciliationEngineTests
{
    private static IReadOnlyList<ReconciliationResult> Run()
    {
        var root = RepoRoot();
        var store = new DocumentLoader().Load(Path.Combine(root, "documents"), Path.Combine(root, "fixtures"));
        return new ReconciliationEngine().Build(store);
    }

    private static ReconciliationResult Study(IReadOnlyList<ReconciliationResult> r, string protocol) =>
        r.Single(x => x.Study.Protocol == protocol);

    private static string RepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null && !Directory.Exists(Path.Combine(dir.FullName, "documents")))
            dir = dir.Parent;
        return dir?.FullName ?? throw new DirectoryNotFoundException("Could not locate the repo root (documents/).");
    }

    [Fact]
    public void Horizon_inv002_is_paid_in_full_after_holdback()
    {
        var horizon = Study(Run(), "MRD-204-017");
        // Both INV-002 and INV-012 are $2,153.13 screening visits settled at the 10% holdback.
        var settled = horizon.InvoiceToPayment.Where(x => x.InvoiceAmount == 2153.13m).ToList();

        Assert.NotEmpty(settled);
        Assert.All(settled, x =>
        {
            Assert.Equal(InvoiceStatus.Paid, x.Status);
            Assert.Equal(1937.82m, x.AmountSettled);    // paid in full per terms, not underpaid
        });
    }

    [Fact]
    public void Horizon_inv105_is_unpaid_and_is_the_only_outstanding_ar()
    {
        var horizon = Study(Run(), "MRD-204-017");

        Assert.Contains(horizon.Unpaid, u =>
            u.RefType == UnpaidRefType.Invoice && u.RefId == "INV-105" && u.AmountExpected == 13061.60m);
        Assert.Equal(13061.60m, horizon.Dashboard.OutstandingAr);
    }

    [Fact]
    public void Horizon_s12_037_screening_is_unbilled()
    {
        var horizon = Study(Run(), "MRD-204-017");
        Assert.Contains(horizon.Unbilled, u => u.SubjectId == "S-12-037");
    }

    [Fact]
    public void Ascend_total_billed_includes_autopays_and_site_fees()
    {
        var ascend = Study(Run(), "VTX-330-201");
        Assert.Equal(21093.40m, ascend.Dashboard.TotalBilled);
    }

    [Fact]
    public void Ascend_has_two_undeposited_autopays_and_a_wrong_amount_exception()
    {
        var ascend = Study(Run(), "VTX-330-201");

        Assert.Contains(ascend.Unpaid, u => u.RefType == UnpaidRefType.Autopay && u.AmountExpected == 523.10m);
        Assert.Contains(ascend.Unpaid, u => u.RefType == UnpaidRefType.Autopay && u.AmountExpected == 418.75m);
        Assert.True(ascend.Dashboard.ExceptionsCount >= 1);
    }

    [Fact]
    public void Northstar_collected_everything_via_ramp_excluding_the_misfiled_invoice()
    {
        var northstar = Study(Run(), "CLX-115-300");
        Assert.Equal(8806.30m, northstar.Dashboard.TotalCollected);
        Assert.Contains(northstar.Unbilled, u => u.SubjectId == "S-03-002");
    }

    [Fact]
    public void Ascend_unscheduled_repeat_hematology_is_unbilled_at_procedure_rate()
    {
        var ascend = Study(Run(), "VTX-330-201");
        var draws = ascend.Unbilled
            .Where(u => u.ProposedVisitLabel!.Contains("Repeat Hematology", StringComparison.OrdinalIgnoreCase))
            .ToList();

        Assert.True(draws.Count >= 2);
        Assert.All(draws, d => Assert.Equal(112.40m, d.EstimatedAmount));   // ASCEND procedure rate, no overhead
    }

    [Fact]
    public void Horizon_unbilled_screening_estimate_uses_tvu_line_plus_overhead()
    {
        var horizon = Study(Run(), "MRD-204-017");
        var s12_037 = horizon.Unbilled.Single(u => u.SubjectId == "S-12-037");

        Assert.Equal("Screening+TVU", s12_037.CtaBasis);
        Assert.Equal(2983.75m, s12_037.EstimatedAmount);   // 2387.00 base x 1.25 overhead
    }
}
