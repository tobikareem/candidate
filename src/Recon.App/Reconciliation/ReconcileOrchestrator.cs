using Recon.App.Ingestion;
using Recon.App.Output;

namespace Recon.App.Reconciliation;

public static class ReconcileOrchestrator
{
    public static void Run(string outRoot = "out")
    {
        var root = RepoRoot();
        var store = new DocumentLoader().Load(
            Path.Combine(root, "documents"),
            Path.Combine(root, "fixtures"));

        var results = new ReconciliationEngine().Build(store);

        foreach (var result in results)
        {
            CanonicalFileWriter.Write(result, outRoot);
            Console.WriteLine($"  wrote {result.Study.OutSlug}/ (chains, dashboard, unbilled, unpaid)");
        }
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
