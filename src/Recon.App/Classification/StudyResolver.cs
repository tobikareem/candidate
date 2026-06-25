using Recon.App.Store;
using Recon.Domain;

namespace Recon.App.Classification;

public sealed record StudyScope(string StudyId, string Evidence);

public sealed class StudyResolver
{
    private readonly Dictionary<string, string> _prefixToStudyId;
    private readonly IReadOnlyList<Study> _studies;

    public StudyResolver(ReconStore store)
    {
        _studies = store.Studies;

        // Tally votes: prefix -> (studyId -> count). Highest count wins per prefix.
        var votes = new Dictionary<string, Dictionary<string, int>>();

        void Vote(string prefix, string studyId)
        {
            if (!votes.TryGetValue(prefix, out var inner))
                votes[prefix] = inner = new Dictionary<string, int>();
            inner[studyId] = inner.GetValueOrDefault(studyId) + 1;
        }

        foreach (var inv in store.Invoices)
        {
            if (inv.SubjectId is null) continue;
            var study = MatchPrintedCode(inv.PrintedStudyCode);
            if (study is not null)
                Vote(Prefix(inv.SubjectId), study.Id);
        }

        foreach (var ap in store.Autopays)
        {
            if (ap.SubjectId is null) continue;
            var line = store.CtaBudgetLines.FirstOrDefault(l => l.BaseAmount == ap.ScheduledAmount);
            if (line is not null)
                Vote(Prefix(ap.SubjectId), line.StudyId);
        }

        _prefixToStudyId = votes.ToDictionary(
            kv => kv.Key,
            kv => kv.Value.MaxBy(v => v.Value).Key);
    }

    public StudyScope Resolve(string? subjectId, string? payor, string? printedStudyCode)
    {
        if (subjectId is not null &&
            _prefixToStudyId.TryGetValue(Prefix(subjectId), out var subjectStudyId))
        {
            var evidence = $"subject-prefix:{Prefix(subjectId)}";

            // If the printed code points to a different study, then the subject overrides it.
            var printed = printedStudyCode is null ? null : MatchPrintedCode(printedStudyCode);
            if (printed is not null && printed.Id != subjectStudyId)
                evidence += $" (override: printed '{printedStudyCode}' but subject wins)";

            return new StudyScope(subjectStudyId, evidence);
        }

        // Priority 2: payor matches a study sponsor.
        if (payor is not null)
        {
            var bySponsor = MatchSponsor(payor);
            if (bySponsor is not null)
                return new StudyScope(bySponsor.Id, $"payor:{payor}");
        }

        // Priority 3: printed study code matches a study code or protocol.
        if (printedStudyCode is not null)
        {
            var byCode = MatchPrintedCode(printedStudyCode);
            if (byCode is not null)
                return new StudyScope(byCode.Id, $"printed-code:{printedStudyCode}");
        }

        // Nothing matched.
        return new StudyScope("", "unresolved");
    }

    private static string Prefix(string subjectId)
    {
        var parts = subjectId.Split('-');
        return parts.Length >= 2 ? $"{parts[0]}-{parts[1]}" : subjectId;
    }

    // Match a printed code against Study.StudyCode or Protocol 
    private Study? MatchPrintedCode(string? code) =>
        string.IsNullOrWhiteSpace(code)
            ? null
            : _studies.FirstOrDefault(s =>
                string.Equals(s.StudyCode, code, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(s.Protocol, code, StringComparison.OrdinalIgnoreCase));

    // Match a payor against a Study.Sponsor 
    private Study? MatchSponsor(string? payor)
    {
        if (string.IsNullOrWhiteSpace(payor)) return null;
        var p = payor.Trim();
        return _studies.FirstOrDefault(s =>
        {
            var sponsor = s.Sponsor.Trim();
            return sponsor.Contains(p, StringComparison.OrdinalIgnoreCase) ||
                   p.Contains(sponsor, StringComparison.OrdinalIgnoreCase);
        });
    }
}
