using Recon.App.Ingestion;
using Recon.App.Reconciliation;
using Recon.App.Store;
using Recon.Domain;

namespace Recon.App.Api;

/// <summary>
/// Builds the reconciliation once at construction and holds the results
/// (and the backing <see cref="ReconStore"/>) in memory for the API to serve.
/// </summary>
public sealed class ReconResultsProvider
{
    public ReconResultsProvider()
    {
        var root = RepoRoot();
        Store = new DocumentLoader().Load(
            Path.Combine(root, "documents"),
            Path.Combine(root, "fixtures"));

        All = new ReconciliationEngine().Build(Store);
    }

    public IReadOnlyList<ReconciliationResult> All { get; }

    public ReconStore Store { get; }

    public ReconciliationResult? BySlug(string slug) =>
        All.FirstOrDefault(r => string.Equals(r.Study.OutSlug, slug, StringComparison.OrdinalIgnoreCase));

    private static string RepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null && !Directory.Exists(Path.Combine(dir.FullName, "documents")))
            dir = dir.Parent;

        return dir?.FullName
            ?? throw new DirectoryNotFoundException("Could not locate the repo root (documents/).");
    }
}
