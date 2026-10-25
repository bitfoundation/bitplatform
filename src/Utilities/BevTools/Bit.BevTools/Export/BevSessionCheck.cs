using System.Globalization;
using System.Text;
using System.Text.Json;

namespace Bit.BevTools;

/// <summary>What fails a <see cref="BevSessionCheck"/>.</summary>
public sealed class BevSessionCheckOptions
{
    /// <summary>
    /// The least severe finding that fails the check; <see langword="null"/> lists findings without failing on any.
    /// <see cref="BevDiagnosticSeverity.Warning"/> by default: an <see cref="BevDiagnosticSeverity.Info"/> finding is a
    /// hint, not a problem.
    /// </summary>
    public BevDiagnosticSeverity? FailOn { get; set; } = BevDiagnosticSeverity.Warning;

    /// <summary>Rules whose findings are listed but never fail the check, by id (<c>render.storm</c>).</summary>
    public ISet<string> IgnoredRules { get; } = new HashSet<string>(StringComparer.Ordinal);

    /// <summary>
    /// Whether any recorded error fails the check, whether or not a rule reported it. Off by default: a run that
    /// exercises error handling records errors on purpose.
    /// </summary>
    public bool FailOnErrors { get; set; }
}

/// <summary>A finding as a session export recorded it.</summary>
public sealed record BevCheckedFinding
{
    /// <summary>The rule that reported it (<c>render.storm</c>).</summary>
    public required string RuleId { get; init; }

    /// <summary>As the rule rated it.</summary>
    public required BevDiagnosticSeverity Severity { get; init; }

    /// <summary>What the rule found.</summary>
    public required string Title { get; init; }

    /// <summary>What it was found from.</summary>
    public required string Evidence { get; init; }

    /// <summary>What the rule suggests trying, when it does.</summary>
    public string? Suggestion { get; init; }

    /// <summary>Whether it fails the check under the options it was checked with.</summary>
    public bool Fails { get; init; }
}

/// <summary>The outcome of <see cref="BevSessionCheck.Check"/>.</summary>
public sealed class BevSessionCheckResult
{
    /// <summary>The findings that were not dismissed, most severe first.</summary>
    public IReadOnlyList<BevCheckedFinding> Findings { get; internal init; } = [];

    /// <summary>Findings someone dismissed in the session: counted, never failing.</summary>
    public int DismissedFindings { get; internal init; }

    /// <summary>Error occurrences the session recorded; null when the export has no errors section.</summary>
    public long? Errors { get; internal init; }

    /// <summary>Distinct problems among them, as the export grouped them (its <c>errorGroups</c>); null without that section.</summary>
    public int? ErrorGroups { get; internal init; }

    /// <summary>HTTP requests in the export; null when it has no http section.</summary>
    public int? Requests { get; internal init; }

    /// <summary>Of those, the ones that failed (an error status or an exception; cancelled ones are not failures).</summary>
    public int? FailedRequests { get; internal init; }

    /// <summary>Whether recorded errors failed the check (<see cref="BevSessionCheckOptions.FailOnErrors"/>).</summary>
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

/// <summary>
/// Reads a session export (<see cref="BevSessionExporter.ToJson"/>, or <i>About → Export session</i>) for a CI gate:
/// the findings the session's rules reported, which of them fail under <see cref="BevSessionCheckOptions"/>, and the
/// errors and failed requests it recorded. It reads what the export holds and nothing else, so an export without the
/// section a check needs is refused rather than passed. Most rules look back over a window of 10 to 60 seconds when
/// they run; for a run that is checked, set <see cref="BevDiagnosticsOptions.EvaluateWhileClosed"/> so they run as the
/// app is used, not only when the session is exported (<see cref="BevSessionCheckResult.Caveats"/> says when it was not).
/// </summary>
/// <example>
/// After an end-to-end run that exported its session:
/// <code>
/// var result = BevSessionCheck.Check(File.ReadAllText("checkout.session.json"));
/// Assert.IsFalse(result.Failed, result.ToMarkdown());
/// </code>
/// </example>
public static class BevSessionCheck
{
    private static readonly JsonDocumentOptions DocumentOptions = new() { MaxDepth = 256 };

    /// <exception cref="FormatException">
    /// <paramref name="exportJson"/> is not a BevTools session export, or lacks its diagnostics section (or its errors
    /// section, when <see cref="BevSessionCheckOptions.FailOnErrors"/> is set).
    /// </exception>
    public static BevSessionCheckResult Check(string exportJson, BevSessionCheckOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(exportJson);
        options ??= new BevSessionCheckOptions();
        try
        {
            using var document = JsonDocument.Parse(exportJson, DocumentOptions);
            return Read(document.RootElement, options);
        }
        catch (JsonException ex)
        {
            throw new FormatException("Not JSON: " + ex.Message, ex);
        }
    }

    private static BevSessionCheckResult Read(JsonElement root, BevSessionCheckOptions options)
    {
        if (root.ValueKind != JsonValueKind.Object || !root.TryGetProperty("format", out var format) || format.ValueKind != JsonValueKind.String
            || format.GetString()?.StartsWith("bit-bevtools/", StringComparison.Ordinal) != true)
        {
            throw new FormatException("Not a BevTools session export: it has no \"format\": \"bit-bevtools/…\".");
        }

        if (!root.TryGetProperty("diagnostics", out var diagnostics) || diagnostics.ValueKind != JsonValueKind.Array)
        {
            throw new FormatException("The export has no diagnostics section, so there is nothing to check: export it with that section.");
        }

        var errorsSection = Section(root, "errors");
        if (options.FailOnErrors && errorsSection is null)
        {
            throw new FormatException("The export has no errors section, so recorded errors cannot fail the check: export it with that section.");
        }

        var findings = new List<BevCheckedFinding>();
        var dismissed = 0;
        foreach (var d in diagnostics.EnumerateArray())
        {
            if (d.ValueKind != JsonValueKind.Object)
            {
                continue;
            }

            if (d.TryGetProperty("dismissed", out var isDismissed) && isDismissed.ValueKind == JsonValueKind.True)
            {
                dismissed++;
                continue;
            }

            var ruleId = String(d, "ruleId") ?? "?";
            var severity = SeverityOf(String(d, "severity"));
            findings.Add(new BevCheckedFinding
            {
                RuleId = ruleId,
                Severity = severity,
                Title = String(d, "title") ?? ruleId,
                Evidence = String(d, "evidence") ?? "",
                Suggestion = String(d, "suggestion"),
                Fails = options.FailOn is { } threshold && severity >= threshold && !options.IgnoredRules.Contains(ruleId),
            });
        }

        long? errors = errorsSection is { } errorList ? errorList.Sum(e => e.TryGetProperty("count", out var c) && c.TryGetInt64(out var n) && n > 0 ? n : 1L) : null;
        var http = Section(root, "http");
        var session = root.TryGetProperty("session", out var s) && s.ValueKind == JsonValueKind.Object ? s : default;
        return new BevSessionCheckResult
        {
            Findings = findings.OrderByDescending(f => f.Severity).ThenByDescending(f => f.Fails).ToList(),
            DismissedFindings = dismissed,
            Errors = errors,
            ErrorGroups = Section(root, "errorGroups")?.Count,
            Requests = http?.Count,
            FailedRequests = http?.Count(r => r.TryGetProperty("failed", out var failed) && failed.ValueKind == JsonValueKind.True),
            FailedOnErrors = options.FailOnErrors && errors > 0,
            Caveats = Caveats(root, session),
            Platform = String(session, "platform"),
            Runtime = String(session, "runtime"),
            ExportedAt = root.TryGetProperty("exportedAt", out var at) && at.ValueKind == JsonValueKind.String && at.TryGetDateTimeOffset(out var exportedAt) ? exportedAt : null,
        };
    }

    // By name only: Enum.TryParse would also take "-1" or "Warning, Error". A severity this version does not know (a newer
    // export) counts as the most severe, so it is looked at rather than passed.
    private static BevDiagnosticSeverity SeverityOf(string? text) =>
        text is not null && Enum.GetNames<BevDiagnosticSeverity>().Contains(text, StringComparer.Ordinal) ? Enum.Parse<BevDiagnosticSeverity>(text) : BevDiagnosticSeverity.Error;

    private static List<string> Caveats(JsonElement root, JsonElement session)
    {
        var caveats = new List<string>();
        if (session.ValueKind == JsonValueKind.Object && session.TryGetProperty("capturing", out var capturing) && capturing.ValueKind == JsonValueKind.False)
        {
            caveats.Add("Capture was paused when the session was exported: what happened while it was paused is not in it.");
        }

        var diagnostics = root.TryGetProperty("options", out var o) && o.ValueKind == JsonValueKind.Object && o.TryGetProperty("diagnostics", out var d) && d.ValueKind == JsonValueKind.Object ? d : default;
        if (diagnostics.ValueKind == JsonValueKind.Object && diagnostics.TryGetProperty("disabledRules", out var disabled) && disabled.ValueKind == JsonValueKind.Array
            && disabled.EnumerateArray().Where(r => r.ValueKind == JsonValueKind.String).Select(r => r.GetString()!).ToList() is { Count: > 0 } rules)
        {
            caveats.Add("Rules disabled in this session, so they reported nothing: " + string.Join(", ", rules) + ".");
        }

        if (diagnostics.ValueKind != JsonValueKind.Object || !diagnostics.TryGetProperty("evaluateWhileClosed", out var whileClosed) || whileClosed.ValueKind != JsonValueKind.True)
        {
            caveats.Add("The rules ran only while BevTools was shown and when the session was exported (Diagnostics.EvaluateWhileClosed was not on); most of them look back 10 to 60 seconds, so activity longer ago than that may have gone unchecked.");
        }

        return caveats;
    }

    private static List<JsonElement>? Section(JsonElement root, string name) =>
        root.TryGetProperty(name, out var array) && array.ValueKind == JsonValueKind.Array ? array.EnumerateArray().Where(e => e.ValueKind == JsonValueKind.Object).ToList() : null;

    private static string? String(JsonElement element, string name) =>
        element.ValueKind == JsonValueKind.Object && element.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.String ? value.GetString() : null;
}
