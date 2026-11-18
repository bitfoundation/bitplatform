using System.Globalization;
using System.Text;
using System.Text.Json;

namespace Bit.BevTools;

/// <summary>
/// Reads a session export (<see cref="BevToolsSessionExporter.ToJson"/>, or <i>About → Export session</i>) for a CI gate:
/// the findings the session's rules reported, which of them fail under <see cref="BevToolsSessionCheckOptions"/>, and the
/// errors and failed requests it recorded. It reads what the export holds and nothing else, so an export without the
/// section a check needs is refused rather than passed. Most rules look back over a window of 10 to 60 seconds when
/// they run; for a run that is checked, set <see cref="BevToolsDiagnosticsOptions.EvaluateWhileClosed"/> so they run as the
/// app is used, not only when the session is exported (<see cref="BevToolsSessionCheckResult.Caveats"/> says when it was not).
/// </summary>
/// <example>
/// After an end-to-end run that exported its session:
/// <code>
/// var result = BevToolsSessionCheck.Check(File.ReadAllText("checkout.session.json"));
/// Assert.IsFalse(result.Failed, result.ToMarkdown());
/// </code>
/// </example>
public static class BevToolsSessionCheck
{
    private static readonly JsonDocumentOptions DocumentOptions = new() { MaxDepth = 256 };

    /// <exception cref="FormatException">
    /// <paramref name="exportJson"/> is not a BevTools session export, or lacks its diagnostics section (or its errors
    /// section, when <see cref="BevToolsSessionCheckOptions.FailOnErrors"/> is set).
    /// </exception>
    public static BevToolsSessionCheckResult Check(string exportJson, BevToolsSessionCheckOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(exportJson);
        options ??= new BevToolsSessionCheckOptions();
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

    private static BevToolsSessionCheckResult Read(JsonElement root, BevToolsSessionCheckOptions options)
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

        var findings = new List<BevToolsCheckedFinding>();
        var dismissed = new List<BevToolsCheckedFinding>();
        foreach (var d in diagnostics.EnumerateArray())
        {
            if (d.ValueKind != JsonValueKind.Object)
            {
                continue;
            }

            var isDismissed = d.TryGetProperty("dismissed", out var dismissedFlag) && dismissedFlag.ValueKind == JsonValueKind.True;
            var ruleId = String(d, "ruleId") ?? "?";
            var severity = SeverityOf(String(d, "severity"));
            (isDismissed ? dismissed : findings).Add(new BevToolsCheckedFinding
            {
                RuleId = ruleId,
                Severity = severity,
                Title = String(d, "title") ?? ruleId,
                Evidence = String(d, "evidence") ?? "",
                Suggestion = String(d, "suggestion"),
                Fingerprint = String(d, "fingerprint"),
                ComponentInstanceId = d.TryGetProperty("componentInstanceId", out var component) && component.ValueKind == JsonValueKind.Number && component.TryGetInt64(out var instanceId) ? instanceId : null,
                Fails = !isDismissed && options.FailOn is { } threshold && severity >= threshold && !options.IgnoredRules.Contains(ruleId),
                Dismissed = isDismissed,
            });
        }

        long? errors = errorsSection is { } errorList ? errorList.Sum(e => e.TryGetProperty("count", out var c) && c.TryGetInt64(out var n) && n > 0 ? n : 1L) : null;
        var http = Section(root, "http");
        var session = root.TryGetProperty("session", out var s) && s.ValueKind == JsonValueKind.Object ? s : default;
        return new BevToolsSessionCheckResult
        {
            Findings = findings.OrderByDescending(f => f.Severity).ThenByDescending(f => f.Fails).ToList(),
            DismissedFindings = dismissed.Count,
            Dismissed = dismissed.OrderByDescending(f => f.Severity).ToList(),
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
    private static BevToolsDiagnosticSeverity SeverityOf(string? text) =>
        text is not null && Enum.GetNames<BevToolsDiagnosticSeverity>().Contains(text, StringComparer.Ordinal) ? Enum.Parse<BevToolsDiagnosticSeverity>(text) : BevToolsDiagnosticSeverity.Error;

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
