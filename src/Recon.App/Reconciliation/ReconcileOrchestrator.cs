using System.Text.Json;

namespace Recon.App.Reconciliation;

public static class ReconcileOrchestrator
{
    private static readonly (string Slug, string StudyId)[] Studies =
    {
        ("study-01-horizon",   "MRD-204-017"),
        ("study-02-ascend",    "VTX-330-201"),
        ("study-03-northstar", "CLX-115-300"),
    };

    private static readonly JsonSerializerOptions JsonOpts = new() { WriteIndented = true };

    public static void Run(string outRoot = "out")
    {
        foreach (var (slug, studyId) in Studies)
        {
            var dir = Path.Combine(outRoot, slug);
            Directory.CreateDirectory(dir);

            // chains.json — object with all 7 required keys; the six chains are empty arrays.
            Write(dir, "chains.json", new
            {
                study_id = studyId,
                site_id = (string?)null,
                investigator = (string?)null,
                payment_to_remittance = Array.Empty<object>(),
                invoice_to_payment = Array.Empty<object>(),
                invoice_to_activities = Array.Empty<object>(),
                remittance_to_activities = Array.Empty<object>(),
                activity_to_cta = Array.Empty<object>(),
                entity_scope = Array.Empty<object>(),
            });

            // dashboard.json
            Write(dir, "dashboard.json", new
            {
                study_id = studyId,
                site_id = (string?)null,
                investigator = (string?)null,
                total_billed = 0m,
                total_collected = 0m,
                outstanding_ar = 0m,
                holdback_withheld = 0m,
                unbilled_estimate = 0m,
                exceptions_count = 0,
                avg_days_to_payment = 0m,
            });

            // unbilled.json
            Write(dir, "unbilled.json", Array.Empty<object>());

            // unpaid.json 
            Write(dir, "unpaid.json", Array.Empty<object>());

            Console.WriteLine($"  wrote {slug}/ (chains, dashboard, unbilled, unpaid)");
        }
    }

    private static void Write(string dir, string file, object payload) =>
        File.WriteAllText(Path.Combine(dir, file), JsonSerializer.Serialize(payload, JsonOpts));
}
