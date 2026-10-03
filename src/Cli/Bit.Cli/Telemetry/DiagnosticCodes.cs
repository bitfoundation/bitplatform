using System.Text.RegularExpressions;

namespace Bit.Cli.Telemetry;

public static partial class DiagnosticCodes
{
    public static IReadOnlyList<string> Extract(string output, int max = 5)
    {
        return [.. CodeRegex().Matches(output)
            .Select(m => m.Groups["code"].Value)
            .Distinct(StringComparer.Ordinal)
            .Take(max)];
    }

    public static string? FirstErrorLine(string output)
    {
        foreach (var line in output.Split('\n'))
        {
            var trimmed = line.Trim();

            if (ErrorLineRegex().IsMatch(trimmed))
            {
                var message = ProjectSuffixRegex().Replace(LocationPrefixRegex().Replace(trimmed, ""), "").Trim();
                return message.Length > 300 ? message[..300] : message;
            }
        }

        return null;
    }

    [GeneratedRegex(@"\berror\s+(?<code>[A-Z]{2,8}\d{3,6})\b", RegexOptions.CultureInvariant)]
    private static partial Regex CodeRegex();

    [GeneratedRegex(@"(\berror\b|\bERR!|^error:|^fatal:|\bfailed\b)", RegexOptions.CultureInvariant | RegexOptions.IgnoreCase)]
    private static partial Regex ErrorLineRegex();

    [GeneratedRegex(@"^.*?(\(\d+(,\d+)?\))?\s?:\s(?=(error|warning)\b)", RegexOptions.CultureInvariant)]
    private static partial Regex LocationPrefixRegex();

    [GeneratedRegex(@"\s\[[^\]]+proj\]$", RegexOptions.CultureInvariant)]
    private static partial Regex ProjectSuffixRegex();
}
