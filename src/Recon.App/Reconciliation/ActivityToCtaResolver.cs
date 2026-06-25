using Recon.Domain;

namespace Recon.App.Reconciliation;

public static class ActivityToCtaResolver
{
    public static IReadOnlyList<ActivityToCta> Resolve(StudyData data)
    {
        var results = new List<ActivityToCta>(data.Activities.Count);

        foreach (var activity in data.Activities)
        {
            results.Add(Map(activity, data.CtaLines));
        }

        return results;
    }

    private static ActivityToCta Map(Activity activity, IReadOnlyList<CtaBudgetLine> ctaLines)
    {
        var label = (activity.VisitLabelNorm ?? string.Empty).Trim();

        if (IsNonBillable(activity, label))
        {
            return new ActivityToCta(activity.Id, null, null, null);
        }

        foreach (var line in ctaLines)
        {
            if (EqualsIgnoreCase(label, line.VisitLabel))
            {
                return new ActivityToCta(activity.Id, line.VisitLabel, line.BaseAmount, MatchConfidence.High);
            }
        }

        if (label.Contains("screening", StringComparison.OrdinalIgnoreCase) &&
            label.Contains("ultrasound", StringComparison.OrdinalIgnoreCase))
        {
            var tvu = ctaLines.FirstOrDefault(l =>
                l.VisitLabel.Contains("TVU", StringComparison.OrdinalIgnoreCase) &&
                l.VisitLabel.Contains("Screening", StringComparison.OrdinalIgnoreCase));
            if (tvu is not null)
                return new ActivityToCta(activity.Id, tvu.VisitLabel, tvu.BaseAmount, MatchConfidence.Medium);
        }

        var stripped = StripParenNotes(label);
        var normalized = NormalizeBillableLabel(stripped);
        foreach (var line in ctaLines)
        {
            if (IsFuzzyMatch(label, stripped, normalized, line.VisitLabel))
            {
                return new ActivityToCta(activity.Id, line.VisitLabel, line.BaseAmount, MatchConfidence.Medium);
            }
        }

        return new ActivityToCta(activity.Id, null, null, null);
    }

    private static bool IsNonBillable(Activity activity, string label)
    {
        if (activity.Status == ActivityStatus.AdminLogOnly)
        {
            return true;
        }

        return label.Contains("[Ad Hoc]", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsFuzzyMatch(string label, string strippedLabel, string normalizedLabel, string ctaVisitLabel)
    {
        var cta = (ctaVisitLabel ?? string.Empty).Trim();
        if (label.Length == 0 || cta.Length == 0)
        {
            return false;
        }

        if (ContainsEitherWay(label, cta) || ContainsEitherWay(strippedLabel, cta) || ContainsEitherWay(normalizedLabel, cta))
        {
            return true;
        }

        return EqualsIgnoreCase(strippedLabel, cta) || EqualsIgnoreCase(normalizedLabel, cta);
    }

    private static bool ContainsEitherWay(string left, string right) =>
        left.Contains(right, StringComparison.OrdinalIgnoreCase) ||
        right.Contains(left, StringComparison.OrdinalIgnoreCase);

    private static string NormalizeBillableLabel(string value)
    {
        var label = value.Trim();
        if (label.StartsWith("UNSCH ", StringComparison.OrdinalIgnoreCase))
            label = label[6..].Trim();
        if (label.StartsWith("Unsch ", StringComparison.OrdinalIgnoreCase))
            label = label[6..].Trim();
        if (label.StartsWith("Unscheduled ", StringComparison.OrdinalIgnoreCase))
            label = label[12..].Trim();
        return label;
    }

    private static string StripParenNotes(string value)
    {
        var open = value.IndexOf('(');
        if (open < 0)
        {
            return value;
        }

        var close = value.IndexOf(')', open + 1);
        var withoutNote = close < 0
            ? value[..open]
            : value[..open] + value[(close + 1)..];

        return withoutNote.Trim();
    }

    private static bool EqualsIgnoreCase(string a, string b)
        => string.Equals(a.Trim(), (b ?? string.Empty).Trim(), StringComparison.OrdinalIgnoreCase);
}
