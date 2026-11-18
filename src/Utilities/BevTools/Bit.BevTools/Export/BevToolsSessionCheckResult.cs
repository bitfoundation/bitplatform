using System.Globalization;
using System.Text;

namespace Bit.BevTools;

/// <summary>The outcome of <see cref="BevToolsSessionCheck.Check"/>.</summary>
public sealed class BevToolsSessionCheckResult
{
    /// <summary>The findings that were not dismissed, most severe first.</summary>
    public IReadOnlyList<BevToolsCheckedFinding> Findings { get; internal init; } = [];

    /// <summary>Findings someone dismissed in the session: counted, never failing.</summary>
    public int DismissedFindings { get; internal init; }

    /// <summary>
    /// Those dismissed findings themselves (<see cref="BevToolsCheckedFinding.Dismissed"/>), for a report that keeps them
    /// visible as suppressed rather than dropping them.
    /// </summary>
    public IReadOnlyList<BevToolsCheckedFinding> Dismissed { get; internal init; } = [];

    /// <summary>Error occurrences the session recorded; null when the export has no errors section.</summary>
    public long? Errors { get; internal init; }

    /// <summary>Distinct problems among them, as the export grouped them (its <c>errorGroups</c>); null without that section.</summary>
    public int? ErrorGroups { get; internal init; }

    /// <summary>HTTP requests in the export; null when it has no http section.</summary>
    public int? Requests { get; internal init; }

    /// <summary>Of those, the ones that failed (an error status or an exception; cancelled ones are not failures).</summary>
    public int? FailedRequests { get; internal init; }

    /// <summary>Whether recorded errors failed the check (<see cref="BevToolsSessionCheckOptions.FailOnErrors"/>).</summary>
    public bool FailedOnErrors { get; internal init; }

    /// <summary>
    /// What limits what the export can say: capture paused, rules disabled, rules that ran only when someone looked.
    /// They never fail the check; they are why a pass may mean less than it seems.
    /// </summary>
    public IReadOnlyList<string> Caveats { get; internal init; } = [];

    public string? Platform { get; internal init; }

    public string? Runtime { get; internal init; }

    public DateTimeOffset? ExportedAt { get; internal init; }

    /// <summary>Whether the check failed: a failing finding, or errors when they fail it.</summary>
    public bool Failed => FailedOnErrors || Findings.Any(f => f.Fails);

    /// <summary>A Markdown summary for a CI log or a pull request comment.</summary>
    public string ToMarkdown(int maxFindings = 50)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(maxFindings);
        var failing = Findings.Count(f => f.Fails);
        var sb = new StringBuilder();
        sb.AppendLine("### BevTools session check").AppendLine();
        sb.Append(CultureInfo.InvariantCulture, $"{Platform ?? "Unknown platform"}{(Runtime is null ? "" : " · " + Runtime)}{(ExportedAt is { } at ? " · exported " + at.ToString("u", CultureInfo.InvariantCulture) : "")}").AppendLine().AppendLine();
        var errors = Errors is { } count ? $"{count} error occurrence{(count == 1 ? "" : "s")}{(ErrorGroups is { } groups ? $" in {groups} group{(groups == 1 ? "" : "s")}" : "")}" : "errors not in the export";
        var requests = Requests is { } sent ? $"{sent} HTTP request{(sent == 1 ? "" : "s")}{(FailedRequests is { } failed ? $", {failed} failed" : "")}" : "HTTP requests not in the export";
        sb.Append(CultureInfo.InvariantCulture, $"{Findings.Count} finding{(Findings.Count == 1 ? "" : "s")}{(DismissedFindings > 0 ? $" ({DismissedFindings} dismissed, not counted)" : "")} · {errors} · {requests}").AppendLine().AppendLine();
        sb.AppendLine(Failed
            ? "**Failed:** " + string.Join(" and ", new[] { failing > 0 ? $"{failing} failing finding{(failing == 1 ? "" : "s")}" : null, FailedOnErrors ? "recorded errors" : null }.Where(r => r is not null))
            : "Passed.").AppendLine();
        foreach (var caveat in Caveats)
        {
            sb.Append("> ").AppendLine(caveat);
        }

        if (Caveats.Count > 0)
        {
            sb.AppendLine();
        }

        if (Findings.Count == 0)
        {
            return sb.ToString();
        }

        sb.AppendLine("| | Severity | Rule | Finding | Evidence |");
        sb.AppendLine("|---|---|---|---|---|");
        foreach (var f in Findings.Take(maxFindings))
        {
            sb.AppendLine(CultureInfo.InvariantCulture, $"| {(f.Fails ? "✗" : "")} | {f.Severity} | `{Cell(f.RuleId)}` | {Cell(f.Title)} | {Cell(f.Evidence)} |");
        }

        if (Findings.Count > maxFindings)
        {
            sb.AppendLine(CultureInfo.InvariantCulture, $"| | | | … {Findings.Count - maxFindings} more | |");
        }

        return sb.AppendLine().ToString();
    }

    // Text comes from an export file: one line, no pipes or backticks, so a row stays a row.
    private static string Cell(string text) => text.ReplaceLineEndings(" ").Replace("|", "\\|", StringComparison.Ordinal).Replace("`", "'", StringComparison.Ordinal);
}
