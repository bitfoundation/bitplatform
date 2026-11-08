using System.Buffers;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Text.Json;

namespace Bit.Belemetry;

/// <summary>
/// A record as Live Metrics sees it: the fields the portal's filters and custom charts name, and the sample document
/// (<c>DocumentIngress</c>) it streams when a document stream takes it. The values are the Azure Monitor envelope's, so
/// a sample reads like the item it stands for in Application Insights.
/// </summary>
internal sealed class AzureMonitorLiveItem
{
    private const int MaxKeyLength = 150;
    private const int MaxValueLength = 8192;
    private const int MaxMessageLength = 32768;

    private readonly BelemetryAttributes _dimensions;

    private AzureMonitorLiveItem(AzureMonitorLiveTelemetryType type, BelemetryAttributes dimensions)
    {
        Type = type;
        _dimensions = dimensions;
    }

    public AzureMonitorLiveTelemetryType Type { get; }

    /// <summary>A request's, dependency's or event's name.</summary>
    public string Name { get; private init; } = string.Empty;

    /// <summary>A request's URL.</summary>
    public string Url { get; private init; } = string.Empty;

    /// <summary>A request's response code or a dependency's result code.</summary>
    public string Code { get; private init; } = string.Empty;

    /// <summary>Whether a request or dependency succeeded.</summary>
    public bool Success { get; private init; }

    /// <summary>A request's or dependency's duration.</summary>
    public TimeSpan Duration { get; private init; }

    /// <summary>A dependency's target.</summary>
    public string Target { get; private init; } = string.Empty;

    /// <summary>A dependency's type (<c>HTTP</c>, the database system, ...).</summary>
    public string DependencyType { get; private init; } = string.Empty;

    /// <summary>A dependency's data: its URL or command.</summary>
    public string Data { get; private init; } = string.Empty;

    /// <summary>An exception's type.</summary>
    public string ExceptionType { get; private init; } = string.Empty;

    /// <summary>An exception's message.</summary>
    public string ExceptionMessage { get; private init; } = string.Empty;

    /// <summary>An exception's stack trace.</summary>
    public string StackTrace { get; private init; } = string.Empty;

    /// <summary>A trace's message.</summary>
    public string Message { get; private init; } = string.Empty;

    /// <summary>The type of the item <see cref="FromSpan"/> makes of <paramref name="span"/>.</summary>
    public static AzureMonitorLiveTelemetryType TypeOf(BelemetrySpan span) =>
        AzureMonitorEnvelopeSerializer.IsRequest(span) ? AzureMonitorLiveTelemetryType.Request : AzureMonitorLiveTelemetryType.Dependency;

    /// <summary>
    /// The type of the item <see cref="FromLog"/> makes of <paramref name="log"/>; null for a page view or a web vital,
    /// which Live Metrics has no type for.
    /// </summary>
    public static AzureMonitorLiveTelemetryType? TypeOf(BelemetryLogRecord log)
    {
        if (log.HasException) return AzureMonitorLiveTelemetryType.Exception;

        return log.EventName switch
        {
            BelemetrySemanticConventions.PageViewEvent or BelemetrySemanticConventions.WebVitalEvent => null,
            { Length: > 0 } => AzureMonitorLiveTelemetryType.Event,
            _ => AzureMonitorLiveTelemetryType.Trace,
        };
    }

    public static AzureMonitorLiveItem FromSpan(BelemetrySpan span)
    {
        var attributes = span.Attributes;
        var isRequest = AzureMonitorEnvelopeSerializer.IsRequest(span);
        var statusCode = attributes.GetInt64(BelemetrySemanticConventions.HttpResponseStatusCode);
        var url = AzureMonitorEnvelopeSerializer.GetUrl(attributes);
        var success = AzureMonitorEnvelopeSerializer.IsSuccess(span, isRequest, statusCode);

        if (isRequest)
        {
            return new AzureMonitorLiveItem(AzureMonitorLiveTelemetryType.Request, attributes)
            {
                Name = AzureMonitorEnvelopeSerializer.GetRequestName(span),
                Url = url ?? string.Empty,
                Code = (statusCode ?? 0).ToString(CultureInfo.InvariantCulture),
                Success = success,
                Duration = span.Duration,
            };
        }

        return new AzureMonitorLiveItem(AzureMonitorLiveTelemetryType.Dependency, attributes)
        {
            Name = span.Name,
            Target = AzureMonitorEnvelopeSerializer.GetTarget(attributes, url) ?? string.Empty,
            DependencyType = AzureMonitorEnvelopeSerializer.GetDependencyType(span, url),
            Data = AzureMonitorEnvelopeSerializer.GetDependencyData(attributes, url) ?? string.Empty,
            Code = statusCode?.ToString(CultureInfo.InvariantCulture) ?? string.Empty,
            Success = success,
            Duration = span.Duration,
        };
    }

    /// <summary>The exception a log record carries, or one recorded on a span (from the event's attributes).</summary>
    public static AzureMonitorLiveItem FromException(BelemetryAttributes attributes, string? body)
    {
        return new AzureMonitorLiveItem(AzureMonitorLiveTelemetryType.Exception, attributes)
        {
            ExceptionType = attributes.GetString(BelemetrySemanticConventions.ExceptionType) ?? "Error",
            ExceptionMessage = attributes.GetString(BelemetrySemanticConventions.ExceptionMessage) ?? body ?? string.Empty,
            StackTrace = attributes.GetString(BelemetrySemanticConventions.ExceptionStacktrace) ?? string.Empty,
        };
    }

    /// <summary>The item of a log record; null when <see cref="TypeOf(BelemetryLogRecord)"/> is.</summary>
    public static AzureMonitorLiveItem? FromLog(BelemetryLogRecord log)
    {
        return TypeOf(log) switch
        {
            AzureMonitorLiveTelemetryType.Exception => FromException(log.Attributes, log.Body),
            AzureMonitorLiveTelemetryType.Event => new AzureMonitorLiveItem(AzureMonitorLiveTelemetryType.Event, log.Attributes)
            {
                // As the envelope names it.
                Name = log.EventName == BelemetrySemanticConventions.PageVisitTimeEvent ? "PageVisitTime" : log.EventName!,
            },
            AzureMonitorLiveTelemetryType.Trace => new AzureMonitorLiveItem(AzureMonitorLiveTelemetryType.Trace, log.Attributes) { Message = log.Body ?? string.Empty },
            _ => null,
        };
    }

    /// <summary>
    /// A custom dimension: an attribute the envelope carries as a property - not the user and session ids (tags there),
    /// nor an exception's own <c>exception.*</c> attributes.
    /// </summary>
    public bool TryGetDimension(string key, [NotNullWhen(true)] out string? value)
    {
        value = null;
        if (IsDimension(key) is false || _dimensions.TryGetValue(key, out var raw) is false) return false;

        value = BelemetryAttributes.ToInvariantString(raw);
        return true;
    }

    /// <summary>What a filter on any field (<c>*</c>) looks in: the item's text fields and its custom dimensions.</summary>
    public IEnumerable<string> Values()
    {
        switch (Type)
        {
            case AzureMonitorLiveTelemetryType.Request:
                yield return Name;
                yield return Url;
                yield return Code;
                break;
            case AzureMonitorLiveTelemetryType.Dependency:
                yield return Name;
                yield return Target;
                yield return DependencyType;
                yield return Data;
                yield return Code;
                break;
            case AzureMonitorLiveTelemetryType.Exception:
                yield return ExceptionType;
                yield return ExceptionMessage;
                yield return StackTrace;
                break;
            case AzureMonitorLiveTelemetryType.Event:
                yield return Name;
                break;
            default:
                yield return Message;
                break;
        }

        foreach (var (key, value) in _dimensions)
        {
            if (IsDimension(key)) yield return BelemetryAttributes.ToInvariantString(value);
        }
    }

    /// <summary>The sample document (<c>DocumentIngress</c>) for the document streams <paramref name="streamIds"/>.</summary>
    public byte[] ToDocument(IReadOnlyList<string> streamIds)
    {
        var buffer = new ArrayBufferWriter<byte>(512);
        using (var w = new Utf8JsonWriter(buffer))
        {
            w.WriteStartObject();

            // The discriminator first: a polymorphic reader may not look further for it.
            switch (Type)
            {
                case AzureMonitorLiveTelemetryType.Request:
                    w.WriteString("DocumentType", "Request");
                    w.WriteString("Name", Truncate(Name, 1024));
                    if (Url.Length > 0) w.WriteString("Url", Truncate(Url, 2048));
                    w.WriteString("ResponseCode", Truncate(Code, 1024));
                    w.WriteString("Duration", FormatDuration(Duration));
                    break;
                case AzureMonitorLiveTelemetryType.Dependency:
                    w.WriteString("DocumentType", "RemoteDependency");
                    w.WriteString("Name", Truncate(Name, 1024));
                    w.WriteString("CommandName", Truncate(Data, 2048));
                    w.WriteString("ResultCode", Truncate(Code, 1024));
                    w.WriteString("Duration", FormatDuration(Duration));
                    break;
                case AzureMonitorLiveTelemetryType.Exception:
                    w.WriteString("DocumentType", "Exception");
                    w.WriteString("ExceptionType", Truncate(ExceptionType, 1024));
                    w.WriteString("ExceptionMessage", Truncate(ExceptionMessage, MaxMessageLength));
                    break;
                case AzureMonitorLiveTelemetryType.Event:
                    w.WriteString("DocumentType", "Event");
                    w.WriteString("Name", Truncate(Name, 512));
                    break;
                default:
                    w.WriteString("DocumentType", "Trace");
                    w.WriteString("Message", Truncate(Message, MaxMessageLength));
                    break;
            }

            w.WriteStartArray("DocumentStreamIds");
            foreach (var id in streamIds) w.WriteStringValue(id);
            w.WriteEndArray();

            w.WriteStartArray("Properties");
            foreach (var (key, value) in _dimensions)
            {
                if (IsDimension(key) is false) continue;
                w.WriteStartObject();
                w.WriteString("key", Truncate(key, MaxKeyLength));
                w.WriteString("value", Truncate(BelemetryAttributes.ToInvariantString(value), MaxValueLength));
                w.WriteEndObject();
            }
            w.WriteEndArray();

            w.WriteEndObject();
        }

        return buffer.WrittenSpan.ToArray();
    }

    /// <summary>An ISO 8601 duration (<c>PT1.5S</c>), as the Live Metrics documents carry it.</summary>
    internal static string FormatDuration(TimeSpan duration) =>
        "PT" + Math.Max(0, duration.TotalSeconds).ToString("0.#######", CultureInfo.InvariantCulture) + "S";

    private bool IsDimension(string key) => AzureMonitorEnvelopeSerializer.IsContextAttribute(key) is false
                                            && (Type != AzureMonitorLiveTelemetryType.Exception || key.StartsWith("exception.", StringComparison.Ordinal) is false);

    // Never inside a surrogate pair: the writer refuses a lone surrogate, and the sample would be lost.
    private static string Truncate(string value, int max) => value.Length <= max ? value : value[..(char.IsHighSurrogate(value[max - 1]) ? max - 1 : max)];
}
