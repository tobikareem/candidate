using System.Text.Json;
using Recon.App.Store;
using Recon.Domain;

namespace Recon.App.Ingestion;
public sealed class CtaFixtureLoader
{
    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,   // maps source_document -> SourceDocument
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
    };

    public void Load(string json, ReconStore store)
    {
        var fixtures = JsonSerializer.Deserialize<List<CtaFixture>>(json, JsonOpts)
            ?? throw new InvalidDataException("ctas.json did not deserialize to a list.");

        // A site can be shared across studies (HORIZON and ASCEND are both ARP-12), so dedupe by site idand add each once
        var sitesById = new Dictionary<string, Site>();

        foreach (var f in fixtures)
        {
            var studyId = SurrogateKey.For("study", f.Protocol);

            var lines = f.BudgetLines.Select(l => new CtaBudgetLine(
                Id: SurrogateKey.For("ctaline", f.Protocol, l.Kind, l.VisitLabel, l.Procedure ?? ""),
                StudyId: studyId,
                VisitLabel: l.VisitLabel,
                Procedure: l.Procedure,
                BaseAmount: l.BaseAmount,
                Kind: ParseKind(l.Kind),
                Cap: l.Cap)
            {
                SourceDocument = f.SourceDocument,
            }).ToList();

            var caps = lines.Where(l => l.Cap is not null)
                            .ToDictionary(l => l.VisitLabel, l => l.Cap!.Value);

            var study = new Study(
                Id: studyId,
                StudyCode: f.StudyCode,
                Protocol: f.Protocol,
                OutSlug: f.OutSlug,
                HoldbackPct: f.HoldbackPct,
                OverheadPct: f.OverheadPct,
                PaymentTermsDays: f.PaymentTermsDays,
                Sponsor: f.Sponsor,
                Cro: f.Cro,
                Caps: caps)
            {
                SourceDocument = f.SourceDocument,
            };

            store.AddStudies(new[] { study });
            store.AddCtaBudgetLines(lines);

            // Site — keyed by number, deduped across studies
            if (!string.IsNullOrWhiteSpace(f.SiteNumber))
            {
                var siteId = SurrogateKey.For("site", f.SiteNumber);
                if (!sitesById.ContainsKey(siteId))
                    sitesById[siteId] = new Site(siteId, f.SiteName ?? f.SiteNumber, null, f.SiteNumber)
                    {
                        SourceDocument = f.SourceDocument,
                    };
            }

            // Investigator 
            if (!string.IsNullOrWhiteSpace(f.Investigator))
            {
                var investigator = new Investigator(
                    SurrogateKey.For("investigator", f.Protocol, f.Investigator),
                    f.Investigator,
                    studyId)
                {
                    SourceDocument = f.SourceDocument,
                };
                store.AddInvestigators(new[] { investigator });
            }
        }

        store.AddSites(sitesById.Values);
    }

    private static CtaLineKind ParseKind(string kind) =>
        Enum.TryParse<CtaLineKind>(kind, ignoreCase: true, out var k) ? k : CtaLineKind.Unknown;
}

// Fixture DTOs — 
internal sealed record CtaFixture(
    string SourceDocument,
    string Protocol,
    string StudyCode,
    string OutSlug,
    string Sponsor,
    string? Cro,
    string? SiteNumber,
    string? SiteName,
    string? Investigator,
    decimal HoldbackPct,
    decimal OverheadPct,
    int PaymentTermsDays,
    List<CtaLineFixture> BudgetLines);

internal sealed record CtaLineFixture(
    string VisitLabel,
    string? Procedure,
    decimal BaseAmount,
    string Kind,
    decimal? Cap);

