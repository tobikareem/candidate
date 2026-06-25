using Recon.App.Ingestion;
using Recon.Domain;

namespace Recon.Tests;

public class DocumentLoaderTests
{
    [Fact]
    public void Loads_all_documents_and_fixtures_with_expected_counts()
    {
        var root = RepoRoot();
        var store = new DocumentLoader().Load(
            Path.Combine(root, "documents"),
            Path.Combine(root, "fixtures"));

        Assert.Equal(3, store.Studies.Count);
        Assert.Equal(26, store.CtaBudgetLines.Count);

        Assert.Equal(42, store.Activities.Count);
        Assert.Equal(13, store.Payments.Count);
        Assert.Equal(12, store.Autopays.Count);
        Assert.Equal(67, store.BankTransactions.Count);
        Assert.Equal(31, store.Invoices.Count);
        Assert.Equal(14, store.Remittances.Count);
        Assert.Equal(4, store.Comms.Count);

        Assert.Equal(2, store.Sites.Count);
        Assert.Equal(3, store.Investigators.Count);
        Assert.Contains(store.Invoices, i => i.PrintedNumber == "INV-105" && i.FaceAmount == 13061.60m);
    }

    [Fact]
    public void Entities_retain_their_source_document()
    {
        var root = RepoRoot();
        var store = new DocumentLoader().Load(
            Path.Combine(root, "documents"),
            Path.Combine(root, "fixtures"));

        Assert.All(store.Activities.Where(a => a.SourceVendor == Vendor.RealTime),
            a => Assert.Equal("realtime_visit_log.csv", a.SourceDocument));

        var horizon = store.Studies.Single(s => s.Protocol == "MRD-204-017");
        Assert.Equal("CTA_MRD-204-017.pdf", horizon.SourceDocument);

        var inv002 = store.Invoices.Single(i => i.PrintedNumber == "INV-002" && i.PrintedStudyCode == "HORIZON");
        Assert.Equal("INV-002_S-12-002_2022-05-11.pdf", inv002.SourceDocument);

        var remittance = store.Remittances.Single(r => r.PrintedRef == "R-002");
        Assert.Equal("R-002_Cordis_Payment_Advice.pdf", remittance.SourceDocument);

        var comm = store.Comms.Single(c => c.SourceDocument == "slack_2026-06-02.md");
        Assert.Contains(comm.Facts, f => f.Kind == CommFactKind.ConfirmsUnpaid && f.TargetRef == "INV-105");
    }

    [Fact]
    public void Shared_site_resolves_to_a_single_entity()
    {
        var root = RepoRoot();
        var store = new DocumentLoader().Load(
            Path.Combine(root, "documents"),
            Path.Combine(root, "fixtures"));

        var arp12 = store.Sites.Where(s => s.Number == "ARP-12").ToList();
        Assert.Single(arp12);
        Assert.Equal("Atlas Research Partners", arp12[0].Name);
    }

    [Fact]
    public void Horizon_study_carries_its_cta_terms()
    {
        var root = RepoRoot();
        var store = new DocumentLoader().Load(
            Path.Combine(root, "documents"),
            Path.Combine(root, "fixtures"));

        var horizon = store.Studies.Single(s => s.Protocol == "MRD-204-017");
        Assert.Equal(0.10m, horizon.HoldbackPct);
        Assert.Equal(0.25m, horizon.OverheadPct);
        Assert.Contains("Pre-screen Chart Review", horizon.Caps);
    }

    private static string RepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null && !Directory.Exists(Path.Combine(dir.FullName, "documents")))
            dir = dir.Parent;

        return dir?.FullName
            ?? throw new DirectoryNotFoundException("Could not locate the repo root (documents/).");
    }
}
