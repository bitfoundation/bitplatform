using System.Globalization;
using System.Text.Json;

namespace Bit.Belemetry;

/// <summary>
/// The JSON shapes of the diagnostic contracts, written and read by hand (<see cref="Utf8JsonWriter"/>,
/// <see cref="JsonElement"/>): no reflection, trimming and AOT safe, and a reader that tolerates and bounds whatever it
/// is given - a bundle can come from anywhere.
/// </summary>
internal static class BelemetryDiagnosticJson
{
    public const int MaxArrayItems = 10_000;
    public const int MaxStringLength = 16_384;

    public static void WriteOptional(Utf8JsonWriter json, string name, string? value)
    {
        if (value is not null) json.WriteString(name, value);
    }

    public static void WriteStrings(Utf8JsonWriter json, string name, IEnumerable<string> values)
    {
        json.WriteStartArray(name);
        foreach (var value in values) json.WriteStringValue(value);
        json.WriteEndArray();
    }

    public static string CategoryName(BelemetryDiagnosticCategories category) => category switch
    {
        BelemetryDiagnosticCategories.Navigation => "navigation",
        BelemetryDiagnosticCategories.Component => "component",
        BelemetryDiagnosticCategories.Exception => "exception",
        BelemetryDiagnosticCategories.Http => "http",
        BelemetryDiagnosticCategories.JsInterop => "js-interop",
        BelemetryDiagnosticCategories.Browser => "browser",
        BelemetryDiagnosticCategories.Circuit => "circuit",
        BelemetryDiagnosticCategories.Log => "log",
        BelemetryDiagnosticCategories.Trace => "trace",
        _ => "custom",
    };

    public static BelemetryDiagnosticCategories ParseCategory(string? name) => name switch
    {
        "navigation" => BelemetryDiagnosticCategories.Navigation,
        "component" => BelemetryDiagnosticCategories.Component,
        "exception" => BelemetryDiagnosticCategories.Exception,
        "http" => BelemetryDiagnosticCategories.Http,
        "js-interop" => BelemetryDiagnosticCategories.JsInterop,
        "browser" => BelemetryDiagnosticCategories.Browser,
        "circuit" => BelemetryDiagnosticCategories.Circuit,
        "log" => BelemetryDiagnosticCategories.Log,
        "trace" => BelemetryDiagnosticCategories.Trace,
        _ => BelemetryDiagnosticCategories.Custom,
    };

    public static string SourceName(BelemetryDiagnosticSource source) => source switch
    {
        BelemetryDiagnosticSource.Framework => "framework",
        BelemetryDiagnosticSource.Browser => "browser",
        BelemetryDiagnosticSource.Application => "application",
        BelemetryDiagnosticSource.Belemetry => "belemetry",
        _ => "dotnet",
    };

    public static BelemetryDiagnosticSource ParseSource(string? name) => name switch
    {
        "framework" => BelemetryDiagnosticSource.Framework,
        "browser" => BelemetryDiagnosticSource.Browser,
        "application" => BelemetryDiagnosticSource.Application,
        "belemetry" => BelemetryDiagnosticSource.Belemetry,
        _ => BelemetryDiagnosticSource.Dotnet,
    };

    public static string OutcomeName(BelemetryDiagnosticOutcome outcome) => outcome switch
    {
        BelemetryDiagnosticOutcome.Success => "success",
        BelemetryDiagnosticOutcome.Failure => "failure",
        BelemetryDiagnosticOutcome.Cancelled => "cancelled",
        _ => "unknown",
    };

    public static BelemetryDiagnosticOutcome ParseOutcome(string? name) => name switch
    {
        "success" => BelemetryDiagnosticOutcome.Success,
        "failure" => BelemetryDiagnosticOutcome.Failure,
        "cancelled" => BelemetryDiagnosticOutcome.Cancelled,
        _ => BelemetryDiagnosticOutcome.Unknown,
    };

    public static string SeverityName(BelemetrySeverity severity) =>
        severity >= BelemetrySeverity.Fatal ? "fatal"
        : severity >= BelemetrySeverity.Error ? "error"
        : severity >= BelemetrySeverity.Warn ? "warn"
        : severity >= BelemetrySeverity.Info ? "info"
        : severity >= BelemetrySeverity.Debug ? "debug"
        : "trace";

    public static BelemetrySeverity ParseSeverity(string? name) => name switch
    {
        "fatal" => BelemetrySeverity.Fatal,
        "error" => BelemetrySeverity.Error,
        "warn" => BelemetrySeverity.Warn,
        "debug" => BelemetrySeverity.Debug,
        "trace" => BelemetrySeverity.Trace,
        _ => BelemetrySeverity.Info,
    };

    /// <summary>One event, as <c>timeline.json</c> holds it.</summary>
    public static void WriteEvent(Utf8JsonWriter json, BelemetryDiagnosticEvent e, ISet<string>? redactedKeys = null)
    {
        json.WriteStartObject();
        json.WriteString("id", EventId(e));
        json.WriteNumber("seq", e.Sequence);
        json.WriteString("time", BelemetryTime.ToDateTime(e.TimeUnixNano).ToString("yyyy-MM-ddTHH:mm:ss.fffffffZ", CultureInfo.InvariantCulture));
        json.WriteNumber("timeUnixNano", e.TimeUnixNano);
        if (e.ElapsedMilliseconds is { } elapsed) json.WriteNumber("elapsedMs", Math.Round(elapsed, 3));
        json.WriteString("category", CategoryName(e.Category));
        json.WriteString("name", e.Name);
        json.WriteString("severity", SeverityName(e.Severity));
        json.WriteString("outcome", OutcomeName(e.Outcome));
        json.WriteString("source", SourceName(e.Source));
        json.WriteNumber("feature", (int)e.Feature);
        WriteOptional(json, "traceId", e.TraceId);
        WriteOptional(json, "spanId", e.SpanId);
        WriteOptional(json, "parentSpanId", e.ParentSpanId);
        WriteOptional(json, "route", e.Route);
        WriteOptional(json, "routeSource", e.RouteSource);
        WriteOptional(json, "component", e.ComponentType);
        WriteOptional(json, "componentSource", e.ComponentSource);
        if (e.DurationMilliseconds is { } duration) json.WriteNumber("durationMs", Math.Round(duration, 3));
        WriteOptional(json, "message", e.Message);
        if (e.Exception is { } exception)
        {
            json.WritePropertyName("exception");
            WriteException(json, exception);
        }
        if (e.Attributes.Count > 0)
        {
            json.WriteStartObject("attributes");
            foreach (var (key, value) in e.Attributes.OrderBy(p => p.Key, StringComparer.Ordinal))
            {
                json.WritePropertyName(key);
                if (redactedKeys is not null && redactedKeys.Contains(key)) json.WriteStringValue(BelemetryDiagnosticSanitizer.Redacted);
                else WriteValue(json, value, depth: 0);
            }
            json.WriteEndObject();
        }
        if (e.Redactions > 0 || e.Truncated)
        {
            json.WriteStartObject("privacy");
            json.WriteNumber("redactions", e.Redactions);
            json.WriteBoolean("truncated", e.Truncated);
            json.WriteEndObject();
        }
        if (e.PreviousPage) json.WriteBoolean("previousPage", true);
        json.WriteEndObject();
    }

    /// <summary>A stable reference to an event within a capture: its sequence, prefixed <c>b</c> for the browser's numbering.</summary>
    public static string EventId(BelemetryDiagnosticEvent e) =>
        (e.Source == BelemetryDiagnosticSource.Browser ? "b" : string.Empty) + e.Sequence.ToString(CultureInfo.InvariantCulture);

    public static void WriteException(Utf8JsonWriter json, BelemetryDiagnosticException exception)
    {
        json.WriteStartObject();
        json.WriteString("type", exception.Type);
        WriteOptional(json, "message", exception.Message);
        WriteOptional(json, "messageTemplate", exception.MessageTemplate);
        json.WriteString("fingerprint", exception.Fingerprint);
        json.WriteString("fingerprintVersion", exception.CustomFingerprint ? "custom" : BelemetryExceptionFingerprint.Version);
        WriteOptional(json, "anchor", exception.Anchor);
        json.WriteString("origin", exception.Origin);
        if (exception.Handled is { } handled) json.WriteBoolean("handled", handled);
        WriteOptional(json, "classification", exception.Classification);
        if (exception.Critical) json.WriteBoolean("critical", true);
        WriteOptional(json, "stackTrace", exception.StackTrace);
        if (exception.InnerExceptions.Count > 0)
        {
            json.WriteStartArray("inner");
            foreach (var inner in exception.InnerExceptions) WriteException(json, inner);
            json.WriteEndArray();
        }
        json.WriteEndObject();
    }

    /// <summary>A sanitized value: a scalar, or a tree the sanitizer built.</summary>
    public static void WriteValue(Utf8JsonWriter json, object? value, int depth)
    {
        switch (value)
        {
            case null:
                json.WriteNullValue();
                break;
            case string s:
                json.WriteStringValue(s);
                break;
            case bool b:
                json.WriteBooleanValue(b);
                break;
            case int i:
                json.WriteNumberValue(i);
                break;
            case long l:
                json.WriteNumberValue(l);
                break;
            case double d when double.IsFinite(d):
                json.WriteNumberValue(d);
                break;
            case short or byte or sbyte or ushort or uint:
                json.WriteNumberValue(Convert.ToInt64(value, CultureInfo.InvariantCulture));
                break;
            case IReadOnlyDictionary<string, object?> map when depth < 32:
                json.WriteStartObject();
                foreach (var (key, item) in map)
                {
                    json.WritePropertyName(key);
                    WriteValue(json, item, depth + 1);
                }
                json.WriteEndObject();
                break;
            case IReadOnlyList<object?> list when depth < 32:
                json.WriteStartArray();
                foreach (var item in list) WriteValue(json, item, depth + 1);
                json.WriteEndArray();
                break;
            default:
                // The sanitizer never produces anything else; a value that slipped through is not serialized.
                json.WriteStringValue(BelemetryDiagnosticSanitizer.Unsupported);
                break;
        }
    }

    public static BelemetryDiagnosticEvent? ReadEvent(JsonElement e)
    {
        if (e.ValueKind != JsonValueKind.Object) return null;

        var attributes = new Dictionary<string, object?>(StringComparer.Ordinal);
        if (e.TryGetProperty("attributes", out var attrs) && attrs.ValueKind == JsonValueKind.Object)
        {
            foreach (var property in attrs.EnumerateObject().Take(64))
            {
                attributes[Cut(property.Name, 256)] = property.Value.ValueKind switch
                {
                    JsonValueKind.String => Cut(property.Value.GetString()!, 2048),
                    JsonValueKind.Number => property.Value.TryGetInt64(out var l) ? l : property.Value.GetDouble(),
                    JsonValueKind.True => true,
                    JsonValueKind.False => false,
                    _ => null,
                };
            }
        }

        var privacy = e.TryGetProperty("privacy", out var p) && p.ValueKind == JsonValueKind.Object ? p : default;
        return new BelemetryDiagnosticEvent
        {
            Sequence = Long(e, "seq") ?? 0,
            TimeUnixNano = Long(e, "timeUnixNano") ?? (Time(e, "time") is { } time ? BelemetryTime.ToUnixNano(time) : 0),
            ElapsedMilliseconds = Double(e, "elapsedMs"),
            Category = ParseCategory(String(e, "category")),
            Name = String(e, "name") ?? string.Empty,
            Severity = ParseSeverity(String(e, "severity")),
            Outcome = ParseOutcome(String(e, "outcome")),
            Source = ParseSource(String(e, "source")),
            Feature = (BelemetryFeatures)(Int(e, "feature") ?? 0),
            TraceId = String(e, "traceId"),
            SpanId = String(e, "spanId"),
            ParentSpanId = String(e, "parentSpanId"),
            Route = String(e, "route"),
            RouteSource = String(e, "routeSource"),
            ComponentType = String(e, "component"),
            ComponentSource = String(e, "componentSource"),
            DurationMilliseconds = Double(e, "durationMs"),
            Message = String(e, "message"),
            Exception = e.TryGetProperty("exception", out var exception) ? ReadException(exception, depth: 0) : null,
            Attributes = attributes,
            Redactions = Int(privacy, "redactions") ?? 0,
            Truncated = privacy.ValueKind == JsonValueKind.Object && privacy.TryGetProperty("truncated", out var t) && t.ValueKind == JsonValueKind.True,
            PreviousPage = e.TryGetProperty("previousPage", out var pp) && pp.ValueKind == JsonValueKind.True,
        };
    }

    public static BelemetryDiagnosticException? ReadException(JsonElement e, int depth)
    {
        if (e.ValueKind != JsonValueKind.Object || depth > 4) return null;

        var inner = new List<BelemetryDiagnosticException>();
        if (e.TryGetProperty("inner", out var items) && items.ValueKind == JsonValueKind.Array)
        {
            foreach (var item in items.EnumerateArray().Take(5))
            {
                if (ReadException(item, depth + 1) is { } converted) inner.Add(converted);
            }
        }

        return new BelemetryDiagnosticException
        {
            Type = String(e, "type") ?? "Exception",
            Message = String(e, "message"),
            MessageTemplate = String(e, "messageTemplate"),
            StackTrace = String(e, "stackTrace"),
            Fingerprint = String(e, "fingerprint") ?? string.Empty,
            CustomFingerprint = String(e, "fingerprintVersion") == "custom",
            Anchor = String(e, "anchor"),
            Origin = String(e, "origin") ?? "log",
            Handled = e.TryGetProperty("handled", out var h) && h.ValueKind is JsonValueKind.True or JsonValueKind.False ? h.GetBoolean() : null,
            Classification = String(e, "classification"),
            Critical = e.TryGetProperty("critical", out var c) && c.ValueKind == JsonValueKind.True,
            InnerExceptions = inner,
        };
    }

    public static string? String(JsonElement json, string name) =>
        json.ValueKind == JsonValueKind.Object && json.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.String ? Cut(value.GetString()!, MaxStringLength) : null;

    public static int? Int(JsonElement json, string name) =>
        json.ValueKind == JsonValueKind.Object && json.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.Number && value.TryGetInt32(out var i) ? i : null;

    public static long? Long(JsonElement json, string name) =>
        json.ValueKind == JsonValueKind.Object && json.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.Number && value.TryGetInt64(out var l) ? l : null;

    public static double? Double(JsonElement json, string name) =>
        json.ValueKind == JsonValueKind.Object && json.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.Number && value.TryGetDouble(out var d) && double.IsFinite(d) ? d : null;

    public static DateTimeOffset? Time(JsonElement json, string name) =>
        String(json, name) is { } text && DateTimeOffset.TryParse(text, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out var time) ? time : null;

    public static List<string> Strings(JsonElement json, string name)
    {
        if (json.ValueKind != JsonValueKind.Object || json.TryGetProperty(name, out var array) is false || array.ValueKind != JsonValueKind.Array) return [];
        return array.EnumerateArray().Take(MaxArrayItems).Where(v => v.ValueKind == JsonValueKind.String).Select(v => Cut(v.GetString()!, MaxStringLength)).ToList();
    }

    private static string Cut(string value, int max) => value.Length > max ? BelemetryValueLimit.Truncate(value, max) : value;
}
