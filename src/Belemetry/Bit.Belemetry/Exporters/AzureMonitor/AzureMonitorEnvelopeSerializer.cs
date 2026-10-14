using System.Buffers;
using System.Globalization;
using System.Text.Json;

namespace Bit.Belemetry;

/// <summary>
/// Maps OpenTelemetry-shaped records to Application Insights envelopes (the <c>/v2.1/track</c> schema). The mapping is
/// documented in CLAUDE.md and mirrored by <c>Scripts/exporters/azure.ts</c>.
/// </summary>
internal static class AzureMonitorEnvelopeSerializer
{
    private const int MaxKeyLength = 150;
    private const int MaxValueLength = 8192;
    private const int MaxMessageLength = 32768;

    private static readonly HashSet<string> _contextAttributes =
    [
        BelemetrySemanticConventions.SessionId, BelemetrySemanticConventions.UserId, BelemetrySemanticConventions.EndUserPseudoId,
    ];

    public sealed record Context(string InstrumentationKey, bool MaskIp);

    /// <summary>Serializes every record of <paramref name="batch"/> as a JSON array of envelopes.</summary>
    public static byte[] Serialize(BelemetryBatch batch, Context context, out int count)
    {
        var buffer = new ArrayBufferWriter<byte>(4096);
        count = 0;

        using (var w = new Utf8JsonWriter(buffer))
        {
            var resource = new ResourceTags(batch.Resource, context);

            w.WriteStartArray();
            foreach (var span in batch.Spans)
            {
                WriteSpan(w, span, resource);
                count++;
            }
            foreach (var log in batch.Logs)
            {
                WriteLog(w, log, resource);
                count++;
            }
            foreach (var metric in batch.Metrics)
            {
                count += WriteMetric(w, metric, resource);
            }
            w.WriteEndArray();
        }

        return buffer.WrittenSpan.ToArray();
    }

    private static void WriteSpan(Utf8JsonWriter w, BelemetrySpan span, ResourceTags resource)
    {
        var isRequest = span.Kind is BelemetrySpanKind.Server or BelemetrySpanKind.Consumer;
        var attributes = span.Attributes;
        var statusCode = attributes.GetInt64(BelemetrySemanticConventions.HttpResponseStatusCode);
        var method = attributes.GetString(BelemetrySemanticConventions.HttpRequestMethod);
        var url = attributes.GetString(BelemetrySemanticConventions.UrlFull) ?? BuildServerUrl(attributes);

        var success = span.Status != BelemetryStatusCode.Error
                      && (statusCode is null || statusCode == 0 || statusCode < (isRequest ? 500 : 400));

        var name = span.Name;
        if (isRequest && method is not null && attributes.GetString(BelemetrySemanticConventions.HttpRoute) is { } route) name = $"{method} {route}";

        BeginEnvelope(w, isRequest ? "Request" : "RemoteDependency", span.StartTimeUnixNano);
        WriteTags(w, resource, span.TraceId, span.ParentSpanId, isRequest ? name : null, attributes);

        w.WriteStartObject("data");
        w.WriteString("baseType", isRequest ? "RequestData" : "RemoteDependencyData");
        w.WriteStartObject("baseData");
        w.WriteNumber("ver", 2);
        w.WriteString("id", span.SpanId);
        w.WriteString("name", Truncate(name, 1024));
        w.WriteString("duration", FormatDuration(span.Duration));
        w.WriteBoolean("success", success);

        if (isRequest)
        {
            w.WriteString("responseCode", (statusCode ?? 0).ToString(CultureInfo.InvariantCulture));
            if (url is not null) w.WriteString("url", Truncate(url, 2048));
        }
        else
        {
            var http = method is not null || url is not null;
            var dbSystem = attributes.GetString(BelemetrySemanticConventions.DbSystemName) ?? attributes.GetString("db.system");
            var browser = span.Scope.Name.StartsWith("bit.belemetry", StringComparison.Ordinal);
            var type = http ? (browser ? "Ajax" : "HTTP")
                     : dbSystem ?? (span.Kind == BelemetrySpanKind.Internal ? "InProc" : span.Kind.ToString());

            w.WriteString("type", type);
            if (statusCode is not null) w.WriteString("resultCode", statusCode.Value.ToString(CultureInfo.InvariantCulture));
            var data = url ?? attributes.GetString(BelemetrySemanticConventions.DbQueryText);
            if (data is not null) w.WriteString("data", Truncate(data, 8192));
            if (GetTarget(attributes, url) is { } target) w.WriteString("target", Truncate(target, 1024));
        }

        WriteProperties(w, attributes, null, span.Status == BelemetryStatusCode.Error ? span.StatusMessage : null);
        w.WriteEndObject();
        w.WriteEndObject();
        w.WriteEndObject();
    }

    private static void WriteLog(Utf8JsonWriter w, BelemetryLogRecord log, ResourceTags resource)
    {
        var attributes = log.Attributes;

        if (log.HasException)
        {
            BeginEnvelope(w, "Exception", log.TimeUnixNano);
            WriteTags(w, resource, log.TraceId, log.SpanId, null, attributes);
            w.WriteStartObject("data");
            w.WriteString("baseType", "ExceptionData");
            w.WriteStartObject("baseData");
            w.WriteNumber("ver", 2);
            w.WriteStartArray("exceptions");
            w.WriteStartObject();
            w.WriteNumber("id", 1);
            w.WriteNumber("outerId", 0);
            w.WriteString("typeName", Truncate(attributes.GetString(BelemetrySemanticConventions.ExceptionType) ?? "Error", 1024));
            w.WriteString("message", Truncate(attributes.GetString(BelemetrySemanticConventions.ExceptionMessage) ?? log.Body ?? string.Empty, MaxMessageLength));
            var stack = attributes.GetString(BelemetrySemanticConventions.ExceptionStacktrace);
            w.WriteBoolean("hasFullStack", stack is not null);
            if (stack is not null) w.WriteString("stack", Truncate(stack, MaxMessageLength));
            w.WriteEndObject();
            w.WriteEndArray();
            w.WriteNumber("severityLevel", ToSeverityLevel(log.Severity == BelemetrySeverity.Unspecified ? BelemetrySeverity.Error : log.Severity));
            WriteProperties(w, attributes, static key => key.StartsWith("exception.", StringComparison.Ordinal), null);
            w.WriteEndObject();
            w.WriteEndObject();
            w.WriteEndObject();
            return;
        }

        switch (log.EventName)
        {
            case BelemetrySemanticConventions.PageViewEvent:
                {
                    var url = attributes.GetString(BelemetrySemanticConventions.UrlFull);
                    var name = attributes.GetString(BelemetrySemanticConventions.PageTitle) is { Length: > 0 } title ? title
                             : attributes.GetString(BelemetrySemanticConventions.UrlPath) ?? url ?? "page";

                    BeginEnvelope(w, "PageView", log.TimeUnixNano);
                    WriteTags(w, resource, log.TraceId, log.SpanId, name, attributes);
                    w.WriteStartObject("data");
                    w.WriteString("baseType", "PageViewData");
                    w.WriteStartObject("baseData");
                    w.WriteNumber("ver", 2);
                    w.WriteString("id", log.SpanId ?? BelemetryHex.NewSpanId());
                    w.WriteString("name", Truncate(name, 1024));
                    if (url is not null) w.WriteString("url", Truncate(url, 2048));
                    w.WriteString("duration", FormatDuration(TimeSpan.Zero));
                    WriteProperties(w, attributes, static key => key is BelemetrySemanticConventions.UrlFull or BelemetrySemanticConventions.PageTitle, null);
                    w.WriteEndObject();
                    w.WriteEndObject();
                    w.WriteEndObject();
                    return;
                }

            case BelemetrySemanticConventions.WebVitalEvent:
                {
                    var name = attributes.GetString(BelemetrySemanticConventions.WebVitalName) ?? "web_vital";
                    var value = attributes.GetDouble(BelemetrySemanticConventions.WebVitalValue) ?? 0;

                    BeginEnvelope(w, "Metric", log.TimeUnixNano);
                    WriteTags(w, resource, log.TraceId, log.SpanId, null, attributes);
                    w.WriteStartObject("data");
                    w.WriteString("baseType", "MetricData");
                    w.WriteStartObject("baseData");
                    w.WriteNumber("ver", 2);
                    w.WriteStartArray("metrics");
                    w.WriteStartObject();
                    w.WriteString("name", name);
                    w.WriteNumber("kind", 0);
                    WriteDouble(w, "value", value);
                    w.WriteNumber("count", 1);
                    w.WriteEndObject();
                    w.WriteEndArray();
                    WriteProperties(w, attributes, static key => key is BelemetrySemanticConventions.WebVitalName or BelemetrySemanticConventions.WebVitalValue, null);
                    w.WriteEndObject();
                    w.WriteEndObject();
                    w.WriteEndObject();
                    return;
                }

            case { Length: > 0 } eventName:
                BeginEnvelope(w, "Event", log.TimeUnixNano);
                WriteTags(w, resource, log.TraceId, log.SpanId, null, attributes);
                w.WriteStartObject("data");
                w.WriteString("baseType", "EventData");
                w.WriteStartObject("baseData");
                w.WriteNumber("ver", 2);
                w.WriteString("name", Truncate(eventName, 512));
                WriteProperties(w, attributes, null, log.Body);
                w.WriteEndObject();
                w.WriteEndObject();
                w.WriteEndObject();
                return;

            default:
                BeginEnvelope(w, "Message", log.TimeUnixNano);
                WriteTags(w, resource, log.TraceId, log.SpanId, null, attributes);
                w.WriteStartObject("data");
                w.WriteString("baseType", "MessageData");
                w.WriteStartObject("baseData");
                w.WriteNumber("ver", 2);
                w.WriteString("message", Truncate(log.Body ?? string.Empty, MaxMessageLength));
                w.WriteNumber("severityLevel", ToSeverityLevel(log.Severity));
                WriteProperties(w, attributes, null, null, log.Scope.Name);
                w.WriteEndObject();
                w.WriteEndObject();
                w.WriteEndObject();
                return;
        }
    }

    private static int WriteMetric(Utf8JsonWriter w, BelemetryMetric metric, ResourceTags resource)
    {
        var written = 0;
        foreach (var p in metric.Points)
        {
            double value;
            long count = 1;
            double? min = null, max = null;

            switch (metric.Type)
            {
                case BelemetryMetricType.Gauge:
                    value = p.IsInteger ? p.LongValue : p.Value;
                    break;
                case BelemetryMetricType.Sum:
                    if (p.HasDeltaUpdate is false && p.DeltaValue == 0 && p.DeltaLongValue == 0) continue;
                    value = p.IsInteger ? p.DeltaLongValue : p.DeltaValue;
                    break;
                default:
                    if (p.DeltaCount == 0) continue;
                    value = p.DeltaSum;
                    count = p.DeltaCount;
                    min = p.DeltaMin;
                    max = p.DeltaMax;
                    break;
            }

            BeginEnvelope(w, "Metric", p.TimeUnixNano);
            WriteTags(w, resource, null, null, null, p.Attributes);
            w.WriteStartObject("data");
            w.WriteString("baseType", "MetricData");
            w.WriteStartObject("baseData");
            w.WriteNumber("ver", 2);
            w.WriteStartArray("metrics");
            w.WriteStartObject();
            w.WriteString("name", Truncate(metric.Name, 1024));
            if (string.IsNullOrEmpty(metric.Scope.Name) is false) w.WriteString("ns", Truncate(metric.Scope.Name, 256));
            w.WriteNumber("kind", metric.Type == BelemetryMetricType.Histogram ? 1 : 0);
            WriteDouble(w, "value", value);
            w.WriteNumber("count", count);
            if (min is not null) WriteDouble(w, "min", min.Value);
            if (max is not null) WriteDouble(w, "max", max.Value);
            w.WriteEndObject();
            w.WriteEndArray();
            WriteProperties(w, p.Attributes, null, null);
            w.WriteEndObject();
            w.WriteEndObject();
            w.WriteEndObject();
            written++;
        }

        return written;
    }

    private static void BeginEnvelope(Utf8JsonWriter w, string type, long timeUnixNano)
    {
        w.WriteStartObject();
        w.WriteNumber("ver", 1);
        w.WriteString("name", "Microsoft.ApplicationInsights." + type);
        var time = timeUnixNano > 0 ? BelemetryTime.ToDateTime(timeUnixNano) : DateTime.UtcNow;
        w.WriteString("time", time.ToString("yyyy-MM-ddTHH:mm:ss.fffffffZ", CultureInfo.InvariantCulture));
        w.WriteNumber("sampleRate", 100);
    }

    private static void WriteTags(Utf8JsonWriter w, ResourceTags resource, string? traceId, string? parentId, string? operationName, BelemetryAttributes attributes)
    {
        w.WriteString("iKey", resource.InstrumentationKey);
        w.WriteStartObject("tags");
        w.WriteString("ai.cloud.role", resource.Role);
        w.WriteString("ai.cloud.roleInstance", resource.RoleInstance);
        w.WriteString("ai.device.type", resource.DeviceType);
        w.WriteString("ai.internal.sdkVersion", resource.SdkVersion);
        if (resource.ApplicationVersion is not null) w.WriteString("ai.application.ver", resource.ApplicationVersion);
        if (resource.MaskIp) w.WriteString("ai.location.ip", "0.0.0.0");
        if (resource.UserAgent is not null) w.WriteString("ai.user.agent", Truncate(resource.UserAgent, 1024));
        if (traceId is not null) w.WriteString("ai.operation.id", traceId);
        if (parentId is not null) w.WriteString("ai.operation.parentId", parentId);
        if (operationName is not null) w.WriteString("ai.operation.name", Truncate(operationName, 1024));
        if (attributes.GetString(BelemetrySemanticConventions.SessionId) is { } session) w.WriteString("ai.session.id", session);
        if (attributes.GetString(BelemetrySemanticConventions.EndUserPseudoId) is { } pseudo) w.WriteString("ai.user.id", pseudo);
        if (attributes.GetString(BelemetrySemanticConventions.UserId) is { } user) w.WriteString("ai.user.authUserId", user);
        w.WriteEndObject();
    }

    private static void WriteProperties(Utf8JsonWriter w, BelemetryAttributes attributes, Func<string, bool>? exclude, string? extraMessage, string? category = null)
    {
        w.WriteStartObject("properties");
        foreach (var (key, value) in attributes)
        {
            if (_contextAttributes.Contains(key) || (exclude?.Invoke(key) ?? false)) continue;
            w.WriteString(Truncate(key, MaxKeyLength), Truncate(BelemetryAttributes.ToInvariantString(value), MaxValueLength));
        }
        if (string.IsNullOrEmpty(extraMessage) is false && attributes.Contains("message") is false) w.WriteString("message", Truncate(extraMessage, MaxValueLength));
        if (string.IsNullOrEmpty(category) is false && attributes.Contains("CategoryName") is false) w.WriteString("CategoryName", Truncate(category, MaxValueLength));
        w.WriteEndObject();
    }

    internal static string FormatDuration(TimeSpan duration) => (duration < TimeSpan.Zero ? TimeSpan.Zero : duration).ToString("c", CultureInfo.InvariantCulture);

    internal static int ToSeverityLevel(BelemetrySeverity severity)
    {
        return (int)severity switch
        {
            <= 8 => 0, // Verbose (TRACE, DEBUG)
            <= 12 => 1, // Information
            <= 16 => 2, // Warning
            <= 20 => 3, // Error
            _ => 4, // Critical
        };
    }

    private static string? BuildServerUrl(BelemetryAttributes attributes)
    {
        var scheme = attributes.GetString(BelemetrySemanticConventions.UrlScheme);
        var host = attributes.GetString(BelemetrySemanticConventions.ServerAddress);
        var path = attributes.GetString(BelemetrySemanticConventions.UrlPath);
        if (scheme is null || host is null) return null;

        var port = attributes.GetInt64(BelemetrySemanticConventions.ServerPort);
        var defaultPort = (scheme == "https" && port == 443) || (scheme == "http" && port == 80);
        var query = attributes.GetString(BelemetrySemanticConventions.UrlQuery);
        return $"{scheme}://{host}{(port is null || defaultPort ? string.Empty : ":" + port)}{path}{(string.IsNullOrEmpty(query) ? string.Empty : "?" + query.TrimStart('?'))}";
    }

    private static string? GetTarget(BelemetryAttributes attributes, string? url)
    {
        if (attributes.GetString(BelemetrySemanticConventions.ServerAddress) is { } host)
        {
            var port = attributes.GetInt64(BelemetrySemanticConventions.ServerPort);
            return port is null or 80 or 443 ? host : $"{host}:{port}";
        }

        return url is not null && Uri.TryCreate(url, UriKind.Absolute, out var uri) ? uri.Authority : null;
    }

    private static void WriteDouble(Utf8JsonWriter w, string name, double value) => w.WriteNumber(name, double.IsFinite(value) ? value : 0);

    private static string Truncate(string value, int max) => value.Length <= max ? value : value[..max];

    private sealed class ResourceTags
    {
        public ResourceTags(BelemetryResource resource, Context context)
        {
            var attributes = resource.Attributes;
            var name = attributes.GetString(BelemetrySemanticConventions.ServiceName) ?? "unknown_service";
            var ns = attributes.GetString(BelemetrySemanticConventions.ServiceNamespace);
            var language = attributes.GetString(BelemetrySemanticConventions.TelemetrySdkLanguage);
            var os = attributes.GetString(BelemetrySemanticConventions.OsType);

            InstrumentationKey = context.InstrumentationKey;
            MaskIp = context.MaskIp;
            // Same role naming as the Azure Monitor OpenTelemetry exporter, so both land on one Application Map node.
            Role = string.IsNullOrEmpty(ns) ? name : $"[{ns}]/{name}";
            RoleInstance = attributes.GetString(BelemetrySemanticConventions.ServiceInstanceId) ?? attributes.GetString(BelemetrySemanticConventions.HostName) ?? string.Empty;
            ApplicationVersion = attributes.GetString(BelemetrySemanticConventions.ServiceVersion);
            var browser = language == "webjs";
            DeviceType = browser ? "Browser" : os is "android" or "ios" ? "Phone" : "PC";
            SdkVersion = (browser ? "bitblmjs:" : "bitblm:") + (attributes.GetString(BelemetrySemanticConventions.TelemetrySdkVersion) ?? BelemetrySettings.SdkVersion);
            UserAgent = attributes.GetString("user_agent.original");
        }

        public string InstrumentationKey { get; }
        public bool MaskIp { get; }
        public string Role { get; }
        public string RoleInstance { get; }
        public string? ApplicationVersion { get; }
        public string DeviceType { get; }
        public string SdkVersion { get; }
        public string? UserAgent { get; }
    }
}
