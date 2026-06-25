using System.Globalization;
using System.Text.Json;
using Recon.App.Store;
using Recon.Domain;

namespace Recon.App.Ingestion;

/// Loads committed email/Slack fixture rows. .
public sealed class CommFixtureLoader
{
    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
    };

    public void Load(string json, ReconStore store)
    {
        var fixtures = JsonSerializer.Deserialize<List<CommFixture>>(json, JsonOpts)
            ?? throw new InvalidDataException("comms.json did not deserialize to a list.");

        store.AddComms(fixtures.Select(ToComm));
    }

    private static Comm ToComm(CommFixture f) => new(
        Id: SurrogateKey.For("comm", f.SourceDocument),
        Channel: f.Channel,
        Date: DateOnly.ParseExact(f.Date, "yyyy-MM-dd", CultureInfo.InvariantCulture),
        Body: f.Body,
        Facts: f.Facts.Select(ToFact).ToList())
    {
        SourceDocument = f.SourceDocument,
    };

    private static CommFact ToFact(CommFactFixture f) => new(
        Kind: ParseKind(f.Kind),
        TargetRef: f.TargetRef,
        Note: f.Note);

    private static CommFactKind ParseKind(string kind) =>
        Enum.TryParse<CommFactKind>(kind, ignoreCase: true, out var parsed) ? parsed : CommFactKind.Clarifies;
}

internal sealed record CommFixture(
    string SourceDocument,
    string Channel,
    string Date,
    string Body,
    List<CommFactFixture> Facts);

internal sealed record CommFactFixture(
    string Kind,
    string TargetRef,
    string Note);
