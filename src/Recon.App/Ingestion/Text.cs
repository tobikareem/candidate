using System.Text.RegularExpressions;

namespace Recon.App.Ingestion;

public static partial class Text
{
    public static string NormalizeLabel(string raw) => Whitespace().Replace(raw.Trim(), " ");

    [GeneratedRegex(@"\s+")]
    private static partial Regex Whitespace();
}
