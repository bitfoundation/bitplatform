using System.Globalization;
using System.Text;
using System.Text.Json;

namespace Bit.Belemetry;

/// <summary>
/// A reproduction-oriented summary of a capture: what failed, in which release and environment, on which route, in
/// which component, after which action, with which failed requests, interop failures and circuit transitions, which
/// traces to look up, and what evidence is missing. Every statement says whether it was observed, inferred or
/// user-provided (<see cref="Findings"/>). It is not a replay: it is enough safe, structured evidence to investigate.
/// </summary>
/// <remarks>Machine-readable as <c>report.json</c> (<see cref="SchemaVersion"/>), human-readable as <c>report.md</c> (<see cref="ToMarkdown"/>).</remarks>
public sealed class BelemetryIssueReport
{
    /// <summary>The version of <c>report.json</c>'s shape.</summary>
    public const int SchemaVersion = 1;

    /// <summary>A one-line title: the failure and where it happened.</summary>
    public string Title { get; init; } = string.Empty;

    /// <summary>A concise summary of the failure.</summary>
    public string Summary { get; init; } = string.Empty;

    /// <summary>The capture's id.</summary>
    public string? CaptureId { get; init; }

    /// <summary>When the capture was made.</summary>
    public DateTimeOffset CreatedAt { get; init; }

    /// <summary>Why the capture was made (manual reason or automatic trigger).</summary>
    public string? Trigger { get; init; }

    /// <summary>The subject exception's type.</summary>
    public string? ExceptionType { get; init; }

    /// <summary>The subject exception's sanitized message.</summary>
    public string? ExceptionMessage { get; init; }

    /// <summary>The subject exception's fingerprint.</summary>
    public string? Fingerprint { get; init; }

    /// <summary>Where the subject exception was observed (<see cref="BelemetryDiagnosticException.Origin"/>).</summary>
    public string? ExceptionOrigin { get; init; }

    /// <summary>How many times the subject exception's group happened.</summary>
    public int? Occurrences { get; init; }

    /// <summary>The service name.</summary>
    public string? Service { get; init; }

    /// <summary>The release (service version).</summary>
    public string? Release { get; init; }

    /// <summary>The environment.</summary>
    public string? Environment { get; init; }

    /// <summary>The source commit, when configured.</summary>
    public string? Commit { get; init; }

    /// <summary>The host kind and runtime (<c>WebAssembly, .NET 10.0.1</c>).</summary>
    public string? Runtime { get; init; }

    /// <summary>The route template the failure happened on, when known.</summary>
    public string? Route { get; init; }

    /// <summary>The action that led to the failure, when known (<c>onclick on MyApp.Pages.Orders.Save</c>).</summary>
    public string? TriggeringAction { get; init; }

    /// <summary>The route templates visited, in order.</summary>
    public IReadOnlyList<BelemetryIssueStep> Navigation { get; init; } = [];

    /// <summary>The failed requests, without payloads.</summary>
    public IReadOnlyList<BelemetryIssueStep> FailedRequests { get; init; } = [];

    /// <summary>The JS interop failures.</summary>
    public IReadOnlyList<BelemetryIssueStep> InteropFailures { get; init; } = [];

    /// <summary>The circuit and reconnection transitions.</summary>
    public IReadOnlyList<BelemetryIssueStep> CircuitTransitions { get; init; } = [];

    /// <summary>The components involved, most relevant first (<c>MyApp.Pages.Orders (framework)</c>).</summary>
    public IReadOnlyList<string> Components { get; init; } = [];

    /// <summary>The trace ids to look up in the tracing backend.</summary>
    public IReadOnlyList<string> TraceIds { get; init; } = [];

    /// <summary>Prerequisites: the application state the capture's snapshots describe.</summary>
    public IReadOnlyList<string> Prerequisites { get; init; } = [];

    /// <summary>What evidence is missing, and why.</summary>
    public IReadOnlyList<string> MissingEvidence { get; init; } = [];

    /// <summary>Every statement, with its kind and evidence.</summary>
    public IReadOnlyList<BelemetryIssueFinding> Findings { get; init; } = [];

    /// <summary>What the user said happened (user-provided, unverified).</summary>
    public string? UserDescription { get; init; }

    /// <summary>The report as Markdown: plain text with code spans, no HTML, no links, so it is safe to open anywhere.</summary>
    public string ToMarkdown()
    {
        var md = new StringBuilder(2048);
        md.Append("# ").Append(Escape(Title)).Append("\n\n");
        md.Append(Escape(Summary)).Append("\n\n");

        md.Append("| | |\n| --- | --- |\n");
        Row(md, "Capture", CaptureId);
        Row(md, "Created", CreatedAt.ToUniversalTime().ToString("u", CultureInfo.InvariantCulture));
        Row(md, "Trigger", Trigger);
        Row(md, "Service", Service);
        Row(md, "Release", Release);
        Row(md, "Environment", Environment);
        Row(md, "Commit", Commit);
        Row(md, "Runtime", Runtime);
        Row(md, "Route", Route);
        Row(md, "Exception", ExceptionType);
        Row(md, "Fingerprint", Fingerprint);
        Row(md, "Occurrences", Occurrences?.ToString(CultureInfo.InvariantCulture));
        md.Append('\n');

        if (ExceptionMessage is not null) md.Append("**Message:** ").Append(Code(ExceptionMessage)).Append("\n\n");
        if (TriggeringAction is not null) md.Append("**Triggering action:** ").Append(Code(TriggeringAction)).Append("\n\n");

        if (UserDescription is not null)
        {
            md.Append("## What the user said (user-provided, unverified)\n\n> ").Append(Escape(UserDescription)).Append("\n\n");
        }

        Section(md, "Findings", Findings.Select(f => $"**{f.Kind switch { BelemetryIssueFindingKind.Observed => "Observed", BelemetryIssueFindingKind.Inferred => "Inferred", _ => "User-provided" }}:** {Escape(f.Text)}" + (f.Evidence.Count > 0 ? $" (events {Code(string.Join(", ", f.Evidence))})" : string.Empty)));
        Section(md, "Navigation", Navigation.Select(Step));
        Section(md, "Components", Components.Select(Code));
        Section(md, "Failed requests", FailedRequests.Select(Step));
        Section(md, "JS interop failures", InteropFailures.Select(Step));
        Section(md, "Circuit transitions", CircuitTransitions.Select(Step));
        Section(md, "Traces", TraceIds.Select(Code));
        Section(md, "Prerequisites", Prerequisites.Select(Escape));
        Section(md, "Missing evidence", MissingEvidence.Select(Escape));

        md.Append("_Generated by Bit.Belemetry. Observed facts come from recorded evidence; inferred ones are explanations that may be wrong._\n");
        return md.ToString();

        static string Step(BelemetryIssueStep step) =>
            $"{step.Time.ToUniversalTime().ToString("HH:mm:ss.fff", CultureInfo.InvariantCulture)} {Code(step.Text)}" + (step.TraceId is null ? string.Empty : $" trace {Code(step.TraceId)}");
    }

    private static void Row(StringBuilder md, string name, string? value)
    {
        if (string.IsNullOrEmpty(value)) return;
        md.Append("| ").Append(name).Append(" | ").Append(Code(value)).Append(" |\n");
    }

    private static void Section(StringBuilder md, string title, IEnumerable<string> lines)
    {
        var any = false;
        foreach (var line in lines)
        {
            if (any is false)
            {
                md.Append("## ").Append(title).Append("\n\n");
                any = true;
            }
            md.Append("- ").Append(line).Append('\n');
        }
        if (any) md.Append('\n');
    }

    /// <summary>A value as a code span: rendered literally by every Markdown viewer (no HTML, no links).</summary>
    internal static string Code(string value)
    {
        var clean = value.Replace('\r', ' ').Replace('\n', ' ').Replace('`', '\'').Replace('|', '/');
        return "`" + clean + "`";
    }

    /// <summary>Free text with every Markdown and HTML control character escaped, on one line.</summary>
    internal static string Escape(string value)
    {
        var md = new StringBuilder(value.Length + 16);
        foreach (var c in value)
        {
            switch (c)
            {
                case '\r' or '\n':
                    md.Append(' ');
                    break;
                case '<':
                    md.Append("&lt;");
                    break;
                case '>':
                    md.Append("&gt;");
                    break;
                case '&':
                    md.Append("&amp;");
                    break;
                case '\\' or '`' or '*' or '_' or '[' or ']' or '(' or ')' or '#' or '!' or '|' or '{' or '}':
                    md.Append('\\').Append(c);
                    break;
                default:
                    md.Append(c);
                    break;
            }
        }
        return md.ToString();
    }

    internal void WriteJson(Utf8JsonWriter json)
    {
        json.WriteStartObject();
        json.WriteNumber("schemaVersion", SchemaVersion);
        json.WriteString("title", Title);
        json.WriteString("summary", Summary);
        BelemetryDiagnosticJson.WriteOptional(json, "captureId", CaptureId);
        json.WriteString("createdAt", CreatedAt.ToUniversalTime());
        BelemetryDiagnosticJson.WriteOptional(json, "trigger", Trigger);
        json.WriteStartObject("exception");
        BelemetryDiagnosticJson.WriteOptional(json, "type", ExceptionType);
        BelemetryDiagnosticJson.WriteOptional(json, "message", ExceptionMessage);
        BelemetryDiagnosticJson.WriteOptional(json, "fingerprint", Fingerprint);
        BelemetryDiagnosticJson.WriteOptional(json, "origin", ExceptionOrigin);
        if (Occurrences is { } occurrences) json.WriteNumber("occurrences", occurrences);
        json.WriteEndObject();
        json.WriteStartObject("release");
        BelemetryDiagnosticJson.WriteOptional(json, "service", Service);
        BelemetryDiagnosticJson.WriteOptional(json, "version", Release);
        BelemetryDiagnosticJson.WriteOptional(json, "environment", Environment);
        BelemetryDiagnosticJson.WriteOptional(json, "commit", Commit);
        BelemetryDiagnosticJson.WriteOptional(json, "runtime", Runtime);
        json.WriteEndObject();
        BelemetryDiagnosticJson.WriteOptional(json, "route", Route);
        BelemetryDiagnosticJson.WriteOptional(json, "triggeringAction", TriggeringAction);
        WriteSteps(json, "navigation", Navigation);
        WriteSteps(json, "failedRequests", FailedRequests);
        WriteSteps(json, "interopFailures", InteropFailures);
        WriteSteps(json, "circuitTransitions", CircuitTransitions);
        BelemetryDiagnosticJson.WriteStrings(json, "components", Components);
        BelemetryDiagnosticJson.WriteStrings(json, "traceIds", TraceIds);
        BelemetryDiagnosticJson.WriteStrings(json, "prerequisites", Prerequisites);
        BelemetryDiagnosticJson.WriteStrings(json, "missingEvidence", MissingEvidence);
        json.WriteStartArray("findings");
        foreach (var finding in Findings)
        {
            json.WriteStartObject();
            json.WriteString("kind", finding.Kind switch { BelemetryIssueFindingKind.Observed => "observed", BelemetryIssueFindingKind.Inferred => "inferred", _ => "user-provided" });
            json.WriteString("text", finding.Text);
            BelemetryDiagnosticJson.WriteStrings(json, "evidence", finding.Evidence);
            json.WriteEndObject();
        }
        json.WriteEndArray();
        BelemetryDiagnosticJson.WriteOptional(json, "userDescription", UserDescription);
        json.WriteEndObject();
    }

    private static void WriteSteps(Utf8JsonWriter json, string name, IReadOnlyList<BelemetryIssueStep> steps)
    {
        json.WriteStartArray(name);
        foreach (var step in steps)
        {
            json.WriteStartObject();
            json.WriteString("time", step.Time.ToUniversalTime());
            json.WriteString("text", step.Text);
            BelemetryDiagnosticJson.WriteOptional(json, "traceId", step.TraceId);
            BelemetryDiagnosticJson.WriteOptional(json, "evidence", step.Evidence);
            json.WriteEndObject();
        }
        json.WriteEndArray();
    }

    /// <summary>Reads a <c>report.json</c> (schema version 1); null when it is not one.</summary>
    public static BelemetryIssueReport? FromJson(JsonElement json)
    {
        if (json.ValueKind != JsonValueKind.Object || BelemetryDiagnosticJson.Int(json, "schemaVersion") is not 1) return null;

        var exception = json.TryGetProperty("exception", out var e) && e.ValueKind == JsonValueKind.Object ? e : default;
        var release = json.TryGetProperty("release", out var r) && r.ValueKind == JsonValueKind.Object ? r : default;
        return new BelemetryIssueReport
        {
            Title = BelemetryDiagnosticJson.String(json, "title") ?? string.Empty,
            Summary = BelemetryDiagnosticJson.String(json, "summary") ?? string.Empty,
            CaptureId = BelemetryDiagnosticJson.String(json, "captureId"),
            CreatedAt = BelemetryDiagnosticJson.Time(json, "createdAt") ?? default,
            Trigger = BelemetryDiagnosticJson.String(json, "trigger"),
            ExceptionType = BelemetryDiagnosticJson.String(exception, "type"),
            ExceptionMessage = BelemetryDiagnosticJson.String(exception, "message"),
            Fingerprint = BelemetryDiagnosticJson.String(exception, "fingerprint"),
            ExceptionOrigin = BelemetryDiagnosticJson.String(exception, "origin"),
            Occurrences = BelemetryDiagnosticJson.Int(exception, "occurrences"),
            Service = BelemetryDiagnosticJson.String(release, "service"),
            Release = BelemetryDiagnosticJson.String(release, "version"),
            Environment = BelemetryDiagnosticJson.String(release, "environment"),
            Commit = BelemetryDiagnosticJson.String(release, "commit"),
            Runtime = BelemetryDiagnosticJson.String(release, "runtime"),
            Route = BelemetryDiagnosticJson.String(json, "route"),
            TriggeringAction = BelemetryDiagnosticJson.String(json, "triggeringAction"),
            Navigation = ReadSteps(json, "navigation"),
            FailedRequests = ReadSteps(json, "failedRequests"),
            InteropFailures = ReadSteps(json, "interopFailures"),
            CircuitTransitions = ReadSteps(json, "circuitTransitions"),
            Components = BelemetryDiagnosticJson.Strings(json, "components"),
            TraceIds = BelemetryDiagnosticJson.Strings(json, "traceIds"),
            Prerequisites = BelemetryDiagnosticJson.Strings(json, "prerequisites"),
            MissingEvidence = BelemetryDiagnosticJson.Strings(json, "missingEvidence"),
            Findings = json.TryGetProperty("findings", out var findings) && findings.ValueKind == JsonValueKind.Array
                ? findings.EnumerateArray().Take(BelemetryDiagnosticJson.MaxArrayItems).Where(f => f.ValueKind == JsonValueKind.Object).Select(f => new BelemetryIssueFinding(
                    BelemetryDiagnosticJson.String(f, "kind") switch { "observed" => BelemetryIssueFindingKind.Observed, "inferred" => BelemetryIssueFindingKind.Inferred, _ => BelemetryIssueFindingKind.UserProvided },
                    BelemetryDiagnosticJson.String(f, "text") ?? string.Empty,
                    BelemetryDiagnosticJson.Strings(f, "evidence"))).ToList()
                : [],
            UserDescription = BelemetryDiagnosticJson.String(json, "userDescription"),
        };
    }

    private static List<BelemetryIssueStep> ReadSteps(JsonElement json, string name)
    {
        if (json.TryGetProperty(name, out var steps) is false || steps.ValueKind != JsonValueKind.Array) return [];
        return steps.EnumerateArray().Take(BelemetryDiagnosticJson.MaxArrayItems).Where(s => s.ValueKind == JsonValueKind.Object).Select(s => new BelemetryIssueStep(
            BelemetryDiagnosticJson.Time(s, "time") ?? default, BelemetryDiagnosticJson.String(s, "text") ?? string.Empty,
            BelemetryDiagnosticJson.String(s, "traceId"), BelemetryDiagnosticJson.String(s, "evidence"))).ToList();
    }
}
