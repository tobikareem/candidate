using Recon.App.Ingestion;
using Recon.App.Store;
using Recon.Domain;

namespace Recon.Tests;

public class CtaFixtureLoaderTests
{
    private const string Json = """
    [
      {
        "source_document": "CTA_MRD-204-017.pdf",
        "protocol": "MRD-204-017",
        "study_code": "HORIZON",
        "out_slug": "study-01-horizon",
        "sponsor": "Meridian Therapeutics, Inc.",
        "cro": null,
        "holdback_pct": 0.10,
        "overhead_pct": 0.25,
        "payment_terms_days": 30,
        "budget_lines": [
          { "visit_label": "Screening", "procedure": null, "base_amount": 1722.50, "kind": "Visit", "cap": null },
          { "visit_label": "Pre-screen Chart Review", "procedure": null, "base_amount": 52.85, "kind": "SiteFee", "cap": 3000.00 }
        ]
      }
    ]
    """;

    [Fact]
    public void Loads_study_terms_from_fixture()
    {
        var store = new ReconStore();
        new CtaFixtureLoader().Load(Json, store);

        var study = Assert.Single(store.Studies);
        Assert.Equal("MRD-204-017", study.Protocol);
        Assert.Equal("HORIZON", study.StudyCode);
        Assert.Equal("study-01-horizon", study.OutSlug);
        Assert.Equal(0.10m, study.HoldbackPct);
        Assert.Equal(0.25m, study.OverheadPct);
        Assert.Equal(30, study.PaymentTermsDays);
    }

    [Fact]
    public void Builds_caps_from_budget_lines_that_carry_one()
    {
        var store = new ReconStore();
        new CtaFixtureLoader().Load(Json, store);

        var study = Assert.Single(store.Studies);
        Assert.Equal(3000.00m, Assert.Contains("Pre-screen Chart Review", study.Caps));
    }

    [Fact]
    public void Budget_lines_link_to_their_study_and_parse_kind()
    {
        var store = new ReconStore();
        new CtaFixtureLoader().Load(Json, store);

        var study = store.Studies.Single();
        Assert.Equal(2, store.CtaBudgetLines.Count);
        Assert.All(store.CtaBudgetLines, l => Assert.Equal(study.Id, l.StudyId));

        var chartReview = store.CtaBudgetLines.Single(l => l.VisitLabel == "Pre-screen Chart Review");
        Assert.Equal(CtaLineKind.SiteFee, chartReview.Kind);
        Assert.Equal(3000.00m, chartReview.Cap);
    }

    [Fact]
    public void Lines_sharing_a_visit_label_but_differing_in_procedure_get_distinct_ids()
    {
        const string json = """
        [
          {
            "source_document": "CTA_X.pdf", "protocol": "X-1", "study_code": "X", "out_slug": "x",
            "sponsor": "S", "cro": null, "holdback_pct": 0, "overhead_pct": 0, "payment_terms_days": 30,
            "budget_lines": [
              { "visit_label": "Screening", "procedure": "Blood Draw", "base_amount": 10.00, "kind": "Procedure", "cap": null },
              { "visit_label": "Screening", "procedure": "ECG", "base_amount": 20.00, "kind": "Procedure", "cap": null }
            ]
          }
        ]
        """;

        var store = new ReconStore();
        new CtaFixtureLoader().Load(json, store);  

        Assert.Equal(2, store.CtaBudgetLines.Count);
        Assert.Equal(2, store.CtaBudgetLines.Select(l => l.Id).Distinct().Count());
    }

    [Fact]
    public void Cta_entities_carry_their_source_document()
    {
        var store = new ReconStore();
        new CtaFixtureLoader().Load(Json, store);

        Assert.Equal("CTA_MRD-204-017.pdf", store.Studies.Single().SourceDocument);
        Assert.All(store.CtaBudgetLines, l => Assert.Equal("CTA_MRD-204-017.pdf", l.SourceDocument));
    }
}
