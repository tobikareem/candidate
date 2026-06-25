using Recon.App.Ingestion;
using Recon.Domain;

namespace Recon.Tests;

public class CtmsIngestionTests
{
    private static readonly CtmsIngestor Ingestor = new();

    // --- Header-signature routing (the core of the adapter design) -

    [Fact]
    public void Routes_realtime_export_by_header_not_filename()
    {
        const string csv =
            "SubjectID,VisitName,VisitDate,SiteNumber,VisitStatus\n" +
            "S-12-037,Screening Visit (incl. Ultrasound),2023-01-25,ARP-12,Complete\n";

        var activities = Ingestor.Read(csv);

        Assert.Single(activities);
        Assert.All(activities, a => Assert.Equal(Vendor.RealTime, a.SourceVendor));
        Assert.Equal("S-12-037", activities[0].SubjectId);
    }

    [Fact]
    public void Routes_crio_export_to_crio_adapter()
    {
        const string csv =
            "activity_ref,patient_id,visit_name,activity_name,category,service_date,source,external_id\n" +
            "A-2001,S-33-001,Screening,Screening,VISIT,2026-01-12,CRIO,CRIO-ARP-12-A-2001\n";

        var activities = Ingestor.Read(csv);

        Assert.Single(activities);
        Assert.Equal(Vendor.Crio, activities[0].SourceVendor);
        Assert.Equal("S-33-001", activities[0].SubjectId);
    }

    [Fact]
    public void Routes_clinical_conductor_export_to_its_adapter()
    {
        const string csv =
            "Subject,ProtocolVisit,VisitDate,Status,Investigator\n" +
            "S-03-001,Screening Visit,2026-02-04,Completed,Dana Whitfield\n";

        var activities = Ingestor.Read(csv);

        Assert.Single(activities);
        Assert.Equal(Vendor.ClinicalConductor, activities[0].SourceVendor);
    }

    [Fact]
    public void Unrecognized_header_throws()
    {
        const string csv = "foo,bar,baz\n1,2,3\n";

        Assert.Throws<NotSupportedException>(() => Ingestor.Read(csv));
    }

    [Fact]
    public void Realtime_signature_missing_a_required_column_is_not_routed()
    {
        
        const string csv =
            "SubjectID,VisitName,SiteNumber,VisitStatus\n" +
            "S-12-037,Screening Visit,ARP-12,Complete\n";

        Assert.Throws<NotSupportedException>(() => Ingestor.Read(csv));
    }

    // --- Normalization ----

    [Fact]
    public void Trims_and_collapses_whitespace_in_visit_label()
    {
        // Crio ships "UNSCH Repeat Hematology " with a trailing space.
        const string csv =
            "activity_ref,patient_id,visit_name,activity_name,category,service_date,source,external_id\n" +
            "A-2U2,S-33-001,UNSCH Repeat Hematology ,UNSCH Repeat Hematology ,UNSCHEDULED,2026-03-15,CRIO,CRIO-ARP-12-A-2U2\n";

        var activity = Ingestor.Read(csv).Single();

        Assert.Equal("UNSCH Repeat Hematology ", activity.VisitLabelRaw);  
        Assert.Equal("UNSCH Repeat Hematology", activity.VisitLabelNorm);   
    }

    [Fact]
    public void Activity_id_is_deterministic_for_same_row()
    {
        const string csv =
            "activity_ref,patient_id,visit_name,activity_name,category,service_date,source,external_id\n" +
            "A-2001,S-33-001,Screening,Screening,VISIT,2026-01-12,CRIO,CRIO-ARP-12-A-2001\n";

        var first = Ingestor.Read(csv).Single().Id;
        var second = Ingestor.Read(csv).Single().Id;

        Assert.Equal(first, second);
    }

    // --- Real committed CSVs: verify entity counts ---

    [Theory]
    [InlineData("realtime_visit_log.csv", 22, Vendor.RealTime)]
    [InlineData("crio_activities.csv", 15, Vendor.Crio)]
    [InlineData("clinical_conductor_visits.csv", 5, Vendor.ClinicalConductor)]
    public void Loads_real_documents_with_expected_counts(string file, int expected, Vendor vendor)
    {
        var csv = File.ReadAllText(Path.Combine(DocumentsDir(), file));

        var activities = Ingestor.Read(csv);

        Assert.Equal(expected, activities.Count);
        Assert.All(activities, a => Assert.Equal(vendor, a.SourceVendor));
    }

    // Walk up from the test binary until we find the repo's documents/ folder.
    private static string DocumentsDir()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null && !Directory.Exists(Path.Combine(dir.FullName, "documents")))
            dir = dir.Parent;

        return dir is not null
            ? Path.Combine(dir.FullName, "documents")
            : throw new DirectoryNotFoundException("Could not locate the repo's documents/ folder.");
    }
}
