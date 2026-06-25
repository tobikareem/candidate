using Recon.App.Classification;
using Recon.App.Ingestion;
using Recon.App.Store;
using Recon.Domain;

namespace Recon.Tests;

public class ClassificationTests
{
    private const string HorizonProtocol = "MRD-204-017";
    private const string AscendProtocol = "VTX-330-201";
    private const string NorthstarProtocol = "CLX-115-300";

    private readonly ReconStore _store;
    private readonly StudyResolver _resolver;

    public ClassificationTests()
    {
        var root = RepoRoot();
        _store = new DocumentLoader().Load(
            Path.Combine(root, "documents"),
            Path.Combine(root, "fixtures"));
        _resolver = new StudyResolver(_store);
    }

    [Fact]
    public void Subject_prefix_resolves_horizon()
    {
        Assert.Equal(StudyId(HorizonProtocol), _resolver.Resolve("S-12-001", null, null).StudyId);
    }

    [Fact]
    public void Subject_prefix_resolves_northstar()
    {
        Assert.Equal(StudyId(NorthstarProtocol), _resolver.Resolve("S-03-001", null, null).StudyId);
    }

    [Fact]
    public void Autopay_subject_prefix_resolves_ascend()
    {
        Assert.Equal(StudyId(AscendProtocol), _resolver.Resolve("S-33-001", null, null).StudyId);
    }

    [Fact]
    public void Misfiled_invoice_resolves_by_subject_not_printed_code()
    {
        var scope = _resolver.Resolve("S-12-021", "Meridian Therapeutics, Inc.", "ASCEND");

        Assert.Equal(StudyId(HorizonProtocol), scope.StudyId);
        Assert.Contains("subject", scope.Evidence, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Site_fee_invoice_resolves_by_payor()
    {
        Assert.Equal(StudyId(NorthstarProtocol), _resolver.Resolve(null, "Calyx Pharma", "NORTHSTAR").StudyId);
    }

    [Fact]
    public void Builds_one_entity_scope_per_entity()
    {
        var scopes = new Classifier(_store, _resolver).BuildEntityScopes();

        // 31 invoices + 13 payments + 14 remittances + 42 activities
        Assert.Equal(100, scopes.Count);
    }

    [Fact]
    public void Cordis_remittance_resolves_to_horizon_via_its_invoice()
    {
        var remittanceId = _store.Remittances.Single(r => r.PrintedRef == "R-002").Id;
        var scopes = new Classifier(_store, _resolver).BuildEntityScopes();

        var scope = scopes.Single(s => s.EntityType == EntityKind.Remittance && s.EntityId == remittanceId);

        Assert.Equal(StudyId(HorizonProtocol), scope.StudyId);
    }

    private string StudyId(string protocol) => _store.Studies.Single(s => s.Protocol == protocol).Id;

    private static string RepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null && !Directory.Exists(Path.Combine(dir.FullName, "documents")))
            dir = dir.Parent;

        return dir?.FullName
            ?? throw new DirectoryNotFoundException("Could not locate the repo root (documents/).");
    }
}
