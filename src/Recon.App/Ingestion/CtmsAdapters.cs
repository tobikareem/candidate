using System.Globalization;
using Recon.Domain;

namespace Recon.App.Ingestion;

public sealed class RealTimeAdapter : ICtmsAdapter
{
    public bool Handles(IReadOnlyList<string> header) => 
        HeaderSignature.HasAll(header, "SubjectID", "VisitName", "VisitDate", "VisitStatus");

    public IEnumerable<Activity> Read(CsvTable table)
    {
        foreach (var row in table.Rows)
        {
            var subject = row["SubjectID"];
            var raw = row["VisitName"];
            var norm = Text.NormalizeLabel(raw);
            var date = CtmsMap.Date(row["VisitDate"]);

            yield return new Activity(
                Id: SurrogateKey.For("activity", "realtime", subject, norm, row["VisitDate"]),
                StudyId: "",
                SubjectId: subject,
                VisitLabelRaw: raw,
                VisitLabelNorm: norm,
                ServiceDate: date,
                Status: CtmsMap.Status(row["VisitStatus"]),
                SourceVendor: Vendor.RealTime,
                CtaLineId: null);
        }
    }
}

public sealed class CrioAdapter : ICtmsAdapter
{
    public bool Handles(IReadOnlyList<string> header) =>
        HeaderSignature.HasAll(header, "patient_id", "visit_name", "service_date", "external_id");

    public IEnumerable<Activity> Read(CsvTable table)
    {
        foreach (var row in table.Rows)
        {
            var raw = row["visit_name"];

            yield return new Activity(
                // external_id is globally unique, so it alone is a sound natural key.
                Id: SurrogateKey.For("activity", "crio", row["external_id"]),
                StudyId: "",
                SubjectId: row["patient_id"],
                VisitLabelRaw: raw,
                VisitLabelNorm: Text.NormalizeLabel(raw),
                ServiceDate: CtmsMap.Date(row["service_date"]),
                Status: ActivityStatus.Completed,
                SourceVendor: Vendor.Crio,
                CtaLineId: null);
        }
    }
}

/// clinical_conductor_visits.csv — Subject,ProtocolVisit,VisitDate,Status,Investigator
public sealed class ClinicalConductorAdapter : ICtmsAdapter
{
    public bool Handles(IReadOnlyList<string> header) =>
        HeaderSignature.HasAll(header, "Subject", "ProtocolVisit", "VisitDate", "Status");

    public IEnumerable<Activity> Read(CsvTable table)
    {
        foreach (var row in table.Rows)
        {
            var subject = row["Subject"];
            var raw = row["ProtocolVisit"];
            var norm = Text.NormalizeLabel(raw);

            yield return new Activity(
                Id: SurrogateKey.For("activity", "clinicalconductor", subject, norm, row["VisitDate"]),
                StudyId: "",
                SubjectId: subject,
                VisitLabelRaw: raw,
                VisitLabelNorm: norm,
                ServiceDate: CtmsMap.Date(row["VisitDate"]),
                Status: CtmsMap.Status(row["Status"]),
                SourceVendor: Vendor.ClinicalConductor,
                CtaLineId: null);
        }
    }
}

internal static class CtmsMap
{
    public static DateOnly Date(string iso) =>
        DateOnly.ParseExact(iso.Trim(), "yyyy-MM-dd", CultureInfo.InvariantCulture);

    public static ActivityStatus Status(string raw) => raw.Trim().ToLowerInvariant() switch
    {
        "complete" or "completed" => ActivityStatus.Completed,
        "scheduled"               => ActivityStatus.Scheduled,
        "missed" or "no show" or "no-show" => ActivityStatus.Missed,
        _                         => ActivityStatus.Unknown,
    };
}
