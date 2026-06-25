using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Recon.App.Output;

namespace Recon.App.Api;

public static class ReconEndpoints
{
    public static void MapReconEndpoints(this WebApplication app)
    {
        app.MapGet("/reconcile", (ReconResultsProvider provider) => Results.Json(Summary(provider)));

        app.MapPost("/reconcile", (ReconResultsProvider provider) =>
        {
            var outRoot = Path.Combine(RepoRoot(), "out");
            foreach (var result in provider.All)
                CanonicalFileWriter.Write(result, outRoot);
            return Results.Json(new { out_dir = outRoot, studies = Summary(provider) });
        });

        app.MapGet("/studies", (ReconResultsProvider provider) =>
            Results.Json(provider.All.Select(r => new
            {
                study_id = r.Study.Protocol,
                slug = r.Study.OutSlug,
                site_id = r.SiteId,
                investigator = r.Investigator
            })));

        app.MapGet("/studies/{slug}/chains", (string slug, ReconResultsProvider provider) =>
        {
            var result = provider.BySlug(slug);
            return result is null
                ? Results.NotFound()
                : Results.Json(CanonicalFileWriter.ChainsPayload(result));
        });

        app.MapGet("/studies/{slug}/dashboard", (string slug, ReconResultsProvider provider) =>
        {
            var result = provider.BySlug(slug);
            return result is null
                ? Results.NotFound()
                : Results.Json(CanonicalFileWriter.DashboardPayload(result));
        });

        app.MapGet("/studies/{slug}/unbilled", (string slug, ReconResultsProvider provider) =>
        {
            var result = provider.BySlug(slug);
            return result is null
                ? Results.NotFound()
                : Results.Json(CanonicalFileWriter.UnbilledPayload(result));
        });

        app.MapGet("/studies/{slug}/unpaid", (string slug, ReconResultsProvider provider) =>
        {
            var result = provider.BySlug(slug);
            return result is null
                ? Results.NotFound()
                : Results.Json(CanonicalFileWriter.UnpaidPayload(result));
        });

        app.MapGet("/studies/{slug}/exceptions", (string slug, ReconResultsProvider provider) =>
        {
            var result = provider.BySlug(slug);
            return result is null
                ? Results.NotFound()
                : Results.Json(CanonicalFileWriter.ExceptionsPayload(result));
        });

        app.MapGet("/entities/{type}/{id}", (string type, string id, ReconResultsProvider provider) =>
        {
            object? entity = type.ToLowerInvariant() switch
            {
                "invoice" => provider.Store.InvoiceById(id),
                "payment" => provider.Store.PaymentById(id),
                "remittance" => provider.Store.RemittanceById(id),
                "activity" => provider.Store.ActivityById(id),
                "autopay" => provider.Store.AutopayById(id),
                "bank" => provider.Store.BankTransactionById(id),
                "study" => provider.Store.StudyById(id),
                _ => null
            };

            return entity is null
                ? Results.NotFound()
                : Results.Json(entity);
        });
    }

    // Headline numbers per study — the same reconciled result the per-study projections and files expose.
    private static object Summary(ReconResultsProvider provider) =>
        provider.All.Select(r => new
        {
            study_id = r.Study.Protocol,
            slug = r.Study.OutSlug,
            total_billed = r.Dashboard.TotalBilled,
            total_collected = r.Dashboard.TotalCollected,
            outstanding_ar = r.Dashboard.OutstandingAr,
            unbilled = r.Unbilled.Count,
            unpaid = r.Unpaid.Count,
            exceptions = r.Dashboard.ExceptionsCount,
        }).ToList();

    private static string RepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null && !Directory.Exists(Path.Combine(dir.FullName, "documents")))
            dir = dir.Parent;
        return dir?.FullName ?? throw new DirectoryNotFoundException("Could not locate the repo root (documents/).");
    }
}
