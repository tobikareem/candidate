

using Recon.App.Reconciliation;

// `dotnet run --project .\src\Recon.App\ -- --reconcile` runs the orchestrator and exits BEFORE the web host is even built, so `make reconcile` needs no running server.

/**
 When the program starts, args is the list of command-line arguments after --. If --reconcile is among them:
  1. read an optional --out <dir> (defaulting to "out"),
  2. run the reconciliation once, writing the JSON files,
  3. return — which ends Program.cs immediately, before creating a webserver that ever runs.
**/

if (args.Contains("--reconcile"))
{
    // var outDir = GetOption(args, "--out") ?? Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "out");
    var outDir = GetOption(args, "--out") ?? "out";
    Console.WriteLine($"Reconciling -> {Path.GetFullPath(outDir)}");
    ReconcileOrchestrator.Run(outDir);
    return;
}

var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

app.MapGet("/", () => "Hello World!");
app.MapGet("/healthz", () => "ok");

app.Run();

// Tiny arg reader: returns the value following `name`, or null if absent.
static string? GetOption(string[] args, string name)
{
    var i = Array.IndexOf(args, name);
    return i >= 0 && i + 1 < args.Length ? args[i + 1] : null;
}