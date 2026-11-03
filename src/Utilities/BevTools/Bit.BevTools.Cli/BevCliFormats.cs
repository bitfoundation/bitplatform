using System.Buffers;
using System.Globalization;
using System.Reflection;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;

namespace Bit.BevTools;

/// <summary>
/// The machine-readable forms of what <c>bevtools check</c> and <c>bevtools compare</c> decided: JSON, SARIF 2.1.0 and
/// GitHub Actions workflow commands. Each is written from the result <see cref="BevSessionCheck"/> or
/// <see cref="BevProfileComparison"/> returned, never re-read from the export, so a format cannot disagree with the
/// Markdown or the exit code.
/// </summary>
internal static class BevCliFormats
{
    internal const string SarifSchema = "https://json.schemastore.org/sarif-2.1.0.json";

    internal const string InformationUri = "https://github.com/bitfoundation/bitplatform";

    // Versioned, as SARIF asks of a fingerprint's name: if BevTools ever fingerprints differently, a consumer must not
    // match the new values against the old ones.
    internal const string FingerprintKey = "bevToolsFingerprint/v1";

    // As the export itself is written: application text stays readable, and the output is UTF-8 anyway.
    private static readonly JsonWriterOptions IndentedJson = new() { Indented = true, Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping };

    private static readonly JsonWriterOptions CompactJson = new() { Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping };

    internal static string ToolVersion { get; } =
        typeof(BevCliFormats).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion
        ?? typeof(BevCliFormats).Assembly.GetName().Version?.ToString()
        ?? "unknown";

    /// <summary>
    /// The check as JSON. <paramref name="maxFindings"/>, <paramref name="maxText"/> and <paramref name="maxBytes"/> bound
    /// it for an agent's context (the MCP server); the CLI writes everything.
    /// </summary>
    internal static string CheckJson(BevSessionCheckResult result, BevSessionCheckOptions options, string? file, bool indented = true, int maxFindings = int.MaxValue, int maxText = int.MaxValue, int maxBytes = int.MaxValue) =>
        Write(indented, w =>
        {
            w.WriteStartObject();
            w.WriteString("format", "bevtools-check/1");
            if (file is not null)
            {
                w.WriteString("file", file);
            }

            w.WriteBoolean("passed", !result.Failed);
            if (options.FailOn is { } failOn)
            {
                w.WriteString("failOn", failOn.ToString());
            }
            else
            {
                w.WriteNull("failOn");
            }

            w.WriteStartArray("ignoredRules");
            foreach (var rule in options.IgnoredRules.Order(StringComparer.Ordinal))
            {
                w.WriteStringValue(rule);
            }

            w.WriteEndArray();
            w.WriteBoolean("failOnErrors", options.FailOnErrors);
            w.WriteBoolean("failedOnErrors", result.FailedOnErrors);
            w.WriteNumber("failingFindings", result.Findings.Count(f => f.Fails));
            WriteNullable(w, "platform", result.Platform);
            WriteNullable(w, "runtime", result.Runtime);
            if (result.ExportedAt is { } exportedAt)
            {
                w.WriteString("exportedAt", exportedAt);
            }
            else
            {
                w.WriteNull("exportedAt");
            }

            // Null, not zero, for a section the export does not have: "none recorded" and "not in the file" differ.
            WriteNullable(w, "errors", result.Errors);
            WriteNullable(w, "errorGroups", result.ErrorGroups);
            WriteNullable(w, "requests", result.Requests);
            WriteNullable(w, "failedRequests", result.FailedRequests);
            w.WriteNumber("dismissedFindings", result.DismissedFindings);
            var all = result.Findings.Concat(result.Dismissed).ToList();
            // What is written after the findings - the caveats and the truncation note - is kept room for.
            var reserve = 512 + result.Caveats.Sum(c => 2L * Encoding.UTF8.GetByteCount(c) + 8);
            var written = 0;
            var full = false;
            w.WriteStartArray("findings");
            foreach (var f in all.Take(maxFindings))
            {
                if (maxBytes == int.MaxValue)
                {
                    WriteFinding(w, f, maxText);
                }
                else if (!TryWriteWithin(w, maxBytes, reserve, always: false, item => WriteFinding(item, f, maxText)))
                {
                    full = true;
                    break;
                }

                written++;
            }

            w.WriteEndArray();
            if (written < all.Count)
            {
                w.WriteNumber("findingsTotal", all.Count);
                w.WriteBoolean("findingsTruncated", true);
                if (full)
                {
                    w.WriteString("truncatedBy", "size: this answer reached its size bound; ignore rules or use get_findings with a filter");
                }
            }

            w.WriteStartArray("caveats");
            foreach (var caveat in result.Caveats)
            {
                w.WriteStringValue(caveat);
            }

            w.WriteEndArray();
            w.WriteEndObject();
        });

    /// <summary>
    /// The check as a SARIF 2.1.0 log. A finding is about a recorded session, not a line of source, so results carry no
    /// <c>locations</c> (SARIF allows that) rather than invented ones.
    /// </summary>
    internal static string Sarif(BevSessionCheckResult result, BevSessionCheckOptions options, string? file) =>
        Write(indented: true, w =>
        {
            var all = result.Findings.Concat(result.Dismissed).ToList();
            var rules = all.Select(f => f.RuleId).Distinct(StringComparer.Ordinal).ToList();
            w.WriteStartObject();
            w.WriteString("$schema", SarifSchema);
            w.WriteString("version", "2.1.0");
            w.WriteStartArray("runs");
            w.WriteStartObject();

            w.WriteStartObject("tool");
            w.WriteStartObject("driver");
            w.WriteString("name", "bevtools");
            w.WriteString("informationUri", InformationUri);
            w.WriteString("version", ToolVersion);
            // Only the ids: the export carries each finding's title, not a description of the rule, and a rule
            // described from one of its findings would describe that finding.
            w.WriteStartArray("rules");
            foreach (var rule in rules)
            {
                w.WriteStartObject();
                w.WriteString("id", rule);
                w.WriteEndObject();
            }

            w.WriteEndArray();
            w.WriteEndObject();
            w.WriteEndObject();

            w.WriteStartArray("invocations");
            w.WriteStartObject();
            // The tool ran; whether the check passed is in the results and the properties below.
            w.WriteBoolean("executionSuccessful", true);
            if (result.Caveats.Count > 0 || result.FailedOnErrors)
            {
                w.WriteStartArray("toolExecutionNotifications");
                if (result.FailedOnErrors)
                {
                    // Recorded errors are no finding, so no result says they failed the check: this does.
                    w.WriteStartObject();
                    w.WriteString("level", "error");
                    w.WriteStartObject("message");
                    w.WriteString("text", $"{result.Errors} recorded error occurrence{(result.Errors == 1 ? "" : "s")} fail the check (--fail-on-errors).");
                    w.WriteEndObject();
                    w.WriteEndObject();
                }

                foreach (var caveat in result.Caveats)
                {
                    w.WriteStartObject();
                    w.WriteString("level", "note");
                    w.WriteStartObject("message");
                    w.WriteString("text", caveat);
                    w.WriteEndObject();
                    w.WriteEndObject();
                }

                w.WriteEndArray();
            }

            w.WriteEndObject();
            w.WriteEndArray();

            w.WriteStartArray("results");
            foreach (var f in all)
            {
                w.WriteStartObject();
                w.WriteString("ruleId", f.RuleId);
                w.WriteNumber("ruleIndex", rules.IndexOf(f.RuleId));
                w.WriteString("level", f.Severity switch
                {
                    BevDiagnosticSeverity.Error => "error",
                    BevDiagnosticSeverity.Warning => "warning",
                    _ => "note",
                });
                w.WriteStartObject("message");
                w.WriteString("text", f.Evidence.Length == 0 ? f.Title : f.Title + " — " + f.Evidence);
                w.WriteEndObject();
                if (f.Fingerprint is { } fingerprint)
                {
                    w.WriteStartObject("partialFingerprints");
                    w.WriteString(FingerprintKey, fingerprint);
                    w.WriteEndObject();
                }

                if (f.Dismissed)
                {
                    // External: it was dismissed in BevTools while the session ran, not in the SARIF consumer.
                    w.WriteStartArray("suppressions");
                    w.WriteStartObject();
                    w.WriteString("kind", "external");
                    w.WriteString("justification", "Dismissed in BevTools during the recorded session.");
                    w.WriteEndObject();
                    w.WriteEndArray();
                }

                w.WriteStartObject("properties");
                w.WriteString("severity", f.Severity.ToString());
                w.WriteBoolean("fails", f.Fails);
                if (f.Suggestion is { } suggestion)
                {
                    w.WriteString("suggestion", suggestion);
                }

                if (f.ComponentInstanceId is { } component)
                {
                    w.WriteNumber("componentInstanceId", component);
                }

                w.WriteEndObject();
                w.WriteEndObject();
            }

            w.WriteEndArray();

            w.WriteStartObject("properties");
            if (file is not null)
            {
                w.WriteString("export", file);
            }

            w.WriteBoolean("passed", !result.Failed);
            WriteNullable(w, "failOn", options.FailOn?.ToString());
            w.WriteBoolean("failedOnErrors", result.FailedOnErrors);
            WriteNullable(w, "errors", result.Errors);
            WriteNullable(w, "errorGroups", result.ErrorGroups);
            WriteNullable(w, "requests", result.Requests);
            WriteNullable(w, "failedRequests", result.FailedRequests);
            WriteNullable(w, "platform", result.Platform);
            WriteNullable(w, "runtime", result.Runtime);
            w.WriteEndObject();

            w.WriteEndObject();
            w.WriteEndArray();
            w.WriteEndObject();
        });

    /// <summary>
    /// The check as GitHub Actions workflow commands: one annotation per finding, by severity, then a plain summary line.
    /// Every value from the export is escaped as the Actions toolkit escapes it, so no text in an export can end an
    /// annotation early or start a command of its own.
    /// </summary>
    internal static string GitHubCommands(BevSessionCheckResult result)
    {
        var sb = new StringBuilder();
        foreach (var f in result.Findings)
        {
            var command = f.Severity switch
            {
                BevDiagnosticSeverity.Error => "error",
                BevDiagnosticSeverity.Warning => "warning",
                _ => "notice",
            };
            var message = new StringBuilder(f.Evidence.Length == 0 ? f.Title : f.Evidence);
            if (f.Suggestion is { Length: > 0 } suggestion)
            {
                message.Append("\nSuggestion: ").Append(suggestion);
            }

            if (!f.Fails)
            {
                message.Append("\nListed only: it does not fail this check.");
            }

            Command(sb, command, "BevTools " + f.RuleId + ": " + f.Title, message.ToString());
        }

        if (result.FailedOnErrors)
        {
            Command(sb, "error", "BevTools check", $"{result.Errors} recorded error occurrence{(result.Errors == 1 ? "" : "s")} fail the check (--fail-on-errors).");
        }

        foreach (var caveat in result.Caveats)
        {
            Command(sb, "notice", "BevTools check caveat", caveat);
        }

        // Counts only, so the one line not written as a command can carry no text from the export.
        var failing = result.Findings.Count(f => f.Fails);
        sb.Append(CultureInfo.InvariantCulture, $"BevTools check {(result.Failed ? "failed" : "passed")}: {result.Findings.Count} finding{(result.Findings.Count == 1 ? "" : "s")}, {failing} failing, {result.DismissedFindings} dismissed.").Append('\n');
        return sb.ToString();
    }

    /// <summary>The comparison as JSON; <paramref name="maxChanges"/> bounds it for an agent (unchanged types go first).</summary>
    internal static string CompareJson(BevProfileComparisonResult result, BevProfileComparisonOptions options, string? baselineFile, string? currentFile, bool indented = true, int maxChanges = int.MaxValue) =>
        Write(indented, w =>
        {
            w.WriteStartObject();
            w.WriteString("format", "bevtools-compare/1");
            if (baselineFile is not null)
            {
                w.WriteString("baseline", baselineFile);
            }

            if (currentFile is not null)
            {
                w.WriteString("current", currentFile);
            }

            w.WriteBoolean("passed", !result.HasRegressions);
            w.WriteNumber("regressions", result.Regressions.Count());
            WriteNullable(w, "baselineRecordedAt", result.BaselineRecordedAt);
            WriteNullable(w, "currentRecordedAt", result.CurrentRecordedAt);
            w.WriteNumber("baselineRenders", result.BaselineRenders);
            w.WriteNumber("currentRenders", result.CurrentRenders);
            // Sums of the export's rounded times; rounded again so float addition does not print 10.500000000000002.
            w.WriteNumber("baselineRenderMs", Math.Round(result.BaselineRenderMs, 3));
            w.WriteNumber("currentRenderMs", Math.Round(result.CurrentRenderMs, 3));
            w.WriteStartObject("options");
            w.WriteNumber("minRenderChange", options.MinRenderChange);
            w.WriteNumber("renderChangeRatio", options.RenderChangeRatio);
            w.WriteNumber("minTimeChangeMs", options.MinTimeChangeMs);
            w.WriteNumber("timeChangeRatio", options.TimeChangeRatio);
            w.WriteNumber("minAllocationChangeBytes", options.MinAllocationChangeBytes);
            w.WriteNumber("allocationChangeRatio", options.AllocationChangeRatio);
            w.WriteEndObject();
            w.WriteStartArray("changes");
            foreach (var c in result.Changes.Take(maxChanges))
            {
                w.WriteStartObject();
                w.WriteString("type", c.Type);
                w.WriteString("displayName", c.DisplayName);
                w.WriteString("kind", c.Kind.ToString());
                WriteNullable(w, "reason", c.Reason);
                w.WriteNumber("baselineInstances", c.BaselineInstances);
                w.WriteNumber("currentInstances", c.CurrentInstances);
                w.WriteNumber("baselineRenders", c.BaselineRenders);
                w.WriteNumber("currentRenders", c.CurrentRenders);
                w.WriteNumber("baselineRenderMs", c.BaselineRenderMs);
                w.WriteNumber("currentRenderMs", c.CurrentRenderMs);
                w.WriteNumber("baselineMaxRenderMs", c.BaselineMaxRenderMs);
                w.WriteNumber("currentMaxRenderMs", c.CurrentMaxRenderMs);
                WriteNullable(w, "baselineP95RenderMs", c.BaselineP95RenderMs);
                WriteNullable(w, "currentP95RenderMs", c.CurrentP95RenderMs);
                WriteNullable(w, "baselineAllocatedBytes", c.BaselineAllocatedBytes);
                WriteNullable(w, "currentAllocatedBytes", c.CurrentAllocatedBytes);
                w.WriteEndObject();
            }

            w.WriteEndArray();
            if (result.Changes.Count > maxChanges)
            {
                w.WriteNumber("changesTotal", result.Changes.Count);
                w.WriteBoolean("changesTruncated", true);
            }

            w.WriteStartArray("warnings");
            foreach (var warning in result.Warnings)
            {
                w.WriteStringValue(warning);
            }

            w.WriteEndArray();
            w.WriteEndObject();
        });

    /// <summary>A workflow command property value, escaped as <c>@actions/core</c>'s <c>escapeProperty</c> does.</summary>
    internal static string EscapeProperty(string value) =>
        EscapeData(value).Replace(":", "%3A", StringComparison.Ordinal).Replace(",", "%2C", StringComparison.Ordinal);

    /// <summary>A workflow command message, escaped as <c>@actions/core</c>'s <c>escapeData</c> does (<c>%</c> first).</summary>
    internal static string EscapeData(string value) =>
        value.Replace("%", "%25", StringComparison.Ordinal).Replace("\r", "%0D", StringComparison.Ordinal).Replace("\n", "%0A", StringComparison.Ordinal);

    /// <summary>
    /// <paramref name="text"/> cut to <paramref name="max"/> characters and marked with an ellipsis, never through a
    /// surrogate pair (half of one is not text the JSON writer accepts). Null when it fits.
    /// </summary>
    internal static string? Clip(string text, int max)
    {
        if (text.Length <= max)
        {
            return null;
        }

        var cut = max > 0 && char.IsHighSurrogate(text[max - 1]) ? max - 1 : max;
        return text[..cut] + "…";
    }

    internal static string Write(bool indented, Action<Utf8JsonWriter> write)
    {
        var buffer = new ArrayBufferWriter<byte>();
        using (var writer = new Utf8JsonWriter(buffer, indented ? IndentedJson : CompactJson))
        {
            write(writer);
        }

        return Encoding.UTF8.GetString(buffer.WrittenSpan);
    }

    /// <summary>
    /// Writes one value with <paramref name="write"/> into the compact <paramref name="w"/>, measured first, unless it
    /// would take the answer past <paramref name="budget"/> bytes with <paramref name="reserve"/> left for what follows;
    /// <paramref name="always"/> writes it whatever its size. False when it was left out.
    /// </summary>
    internal static bool TryWriteWithin(Utf8JsonWriter w, long budget, long reserve, bool always, Action<Utf8JsonWriter> write)
    {
        var buffer = new ArrayBufferWriter<byte>();
        using (var item = new Utf8JsonWriter(buffer, CompactJson))
        {
            write(item);
        }

        // The same accounting as the answer's own: what the writer flushed plus what it still holds, and a separator.
        if (!always && w.BytesCommitted + w.BytesPending + buffer.WrittenCount + 1 + reserve > budget)
        {
            return false;
        }

        w.WriteRawValue(buffer.WrittenSpan, skipInputValidation: true);
        return true;
    }

    private static void WriteFinding(Utf8JsonWriter w, BevCheckedFinding f, int maxText)
    {
        var truncated = new List<string>();
        w.WriteStartObject();
        w.WriteString("ruleId", f.RuleId);
        w.WriteString("severity", f.Severity.ToString());
        w.WriteString("title", Bounded(f.Title, "title", maxText, truncated));
        w.WriteString("evidence", Bounded(f.Evidence, "evidence", maxText, truncated));
        if (f.Suggestion is { } suggestion)
        {
            w.WriteString("suggestion", Bounded(suggestion, "suggestion", maxText, truncated));
        }

        if (f.Fingerprint is { } fingerprint)
        {
            w.WriteString("fingerprint", fingerprint);
        }

        if (f.ComponentInstanceId is { } component)
        {
            w.WriteNumber("componentInstanceId", component);
        }

        w.WriteBoolean("fails", f.Fails);
        w.WriteBoolean("dismissed", f.Dismissed);
        if (truncated.Count > 0)
        {
            w.WriteStartArray("truncatedFields");
            foreach (var field in truncated)
            {
                w.WriteStringValue(field);
            }

            w.WriteEndArray();
        }

        w.WriteEndObject();
    }

    private static string Bounded(string text, string field, int max, List<string> truncated)
    {
        if (Clip(text, max) is not { } clipped)
        {
            return text;
        }

        truncated.Add(field);
        return clipped;
    }

    private static void Command(StringBuilder sb, string command, string title, string message) =>
        sb.Append("::").Append(command).Append(" title=").Append(EscapeProperty(title)).Append("::").Append(EscapeData(message)).Append('\n');

    private static void WriteNullable(Utf8JsonWriter w, string name, string? value)
    {
        if (value is null)
        {
            w.WriteNull(name);
        }
        else
        {
            w.WriteString(name, value);
        }
    }

    private static void WriteNullable(Utf8JsonWriter w, string name, long? value)
    {
        if (value is { } number)
        {
            w.WriteNumber(name, number);
        }
        else
        {
            w.WriteNull(name);
        }
    }

    private static void WriteNullable(Utf8JsonWriter w, string name, double? value)
    {
        if (value is { } number)
        {
            w.WriteNumber(name, number);
        }
        else
        {
            w.WriteNull(name);
        }
    }

    private static void WriteNullable(Utf8JsonWriter w, string name, DateTimeOffset? value)
    {
        if (value is { } at)
        {
            w.WriteString(name, at);
        }
        else
        {
            w.WriteNull(name);
        }
    }
}
