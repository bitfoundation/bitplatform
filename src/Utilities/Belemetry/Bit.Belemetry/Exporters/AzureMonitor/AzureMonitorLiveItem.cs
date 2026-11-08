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
/// <remarks>
/// A span's fields are worked out the first time something asks for them: most spans are offered to a stream that only
/// looks at whether they succeeded, or that has no quota left. Read on the thread that recorded the record, and only
/// while it is offered (<see cref="AzureMonitorLiveMetrics"/>), so they need no lock.
/// </remarks>
internal sealed class AzureMonitorLiveItem
{
    private const int MaxKeyLength = 150;
    private const int MaxValueLength = 8192;
    private const int MaxMessageLength = 32768;

    private readonly BelemetrySpan? _span;
    private readonly AzureMonitorProperties _properties;
    private string? _name, _url, _code, _target, _dependencyType, _data;
    private long? _statusCode;
    private bool _statusCodeRead, _urlRead;
    private bool? _success;

    /// <param name="type">Its telemetry type.</param>
    /// <param name="properties">Its custom dimensions.</param>
    /// <param name="span">The request or dependency it is, whose fields it works out.</param>
    /// <param name="name">An event's name.</param>
    private AzureMonitorLiveItem(AzureMonitorLiveTelemetryType type, AzureMonitorProperties properties, BelemetrySpan? span = null, string? name = null)
    {
        Type = type;
        _properties = properties;
        _span = span;
        _name = name;
    }

    /// <summary>Its telemetry type.</summary>
    public AzureMonitorLiveTelemetryType Type { get; }

    /// <summary>A request's, dependency's or event's name.</summary>
    public string Name => _name ??= Type switch
    {
        AzureMonitorLiveTelemetryType.Request => AzureMonitorEnvelopeSerializer.GetRequestName(_span!),
        AzureMonitorLiveTelemetryType.Dependency => _span!.Name,
        _ => string.Empty,
    };

    /// <summary>A request's URL.</summary>
    public string Url => Type == AzureMonitorLiveTelemetryType.Request ? SpanUrl ?? string.Empty : string.Empty;

    /// <summary>A request's response code or a dependency's result code, as the envelope writes it.</summary>
    public string Code => _code ??= Type switch
    {
        AzureMonitorLiveTelemetryType.Request => (StatusCode ?? 0).ToString(CultureInfo.InvariantCulture),
        AzureMonitorLiveTelemetryType.Dependency => StatusCode?.ToString(CultureInfo.InvariantCulture) ?? string.Empty,
        _ => string.Empty,
    };

    /// <summary>
    /// <see cref="Code"/> as a number: a request without a status code has 0, as there; a dependency without one, or an
    /// item of another type, none.
    /// </summary>
    public long? CodeNumber => Type == AzureMonitorLiveTelemetryType.Request ? StatusCode ?? 0 : StatusCode;

    /// <summary>Whether a request or dependency succeeded.</summary>
    public bool Success => _success ??= _span is { } span && AzureMonitorEnvelopeSerializer.IsSuccess(span, Type == AzureMonitorLiveTelemetryType.Request, StatusCode);

    /// <summary>A request's or dependency's duration.</summary>
    public TimeSpan Duration => _span?.Duration ?? TimeSpan.Zero;

    /// <summary>A dependency's target.</summary>
    public string Target => _target ??= Type == AzureMonitorLiveTelemetryType.Dependency
        ? AzureMonitorEnvelopeSerializer.GetTarget(_span!.Attributes, SpanUrl) ?? string.Empty
        : string.Empty;

    /// <summary>A dependency's type (<c>HTTP</c>, the database system, ...).</summary>
    public string DependencyType => _dependencyType ??= Type == AzureMonitorLiveTelemetryType.Dependency
        ? AzureMonitorEnvelopeSerializer.GetDependencyType(_span!, SpanUrl)
        : string.Empty;

    /// <summary>A dependency's data: its URL or command.</summary>
    public string Data => _data ??= Type == AzureMonitorLiveTelemetryType.Dependency
        ? AzureMonitorEnvelopeSerializer.GetDependencyData(_span!.Attributes, SpanUrl) ?? string.Empty
        : string.Empty;

    /// <summary>An exception's type.</summary>
    public string ExceptionType { get; private init; } = string.Empty;

    /// <summary>An exception's message.</summary>
    public string ExceptionMessage { get; private init; } = string.Empty;

    /// <summary>An exception's stack trace.</summary>
    public string StackTrace { get; private init; } = string.Empty;

    /// <summary>A trace's message.</summary>
    public string Message { get; private init; } = string.Empty;

    /// <summary>The span's HTTP status code; null without one.</summary>
    private long? StatusCode
    {
        get
        {
            if (_statusCodeRead is false)
            {
                _statusCode = _span?.Attributes.GetInt64(BelemetrySemanticConventions.HttpResponseStatusCode);
                _statusCodeRead = true;
            }
            return _statusCode;
        }
    }

    /// <summary>The span's URL, which a request is at and a dependency's target and data come from; null without one.</summary>
    private string? SpanUrl
    {
        get
        {
            if (_urlRead is false)
            {
                _url = _span is { } span ? AzureMonitorEnvelopeSerializer.GetUrl(span.Attributes) : null;
                _urlRead = true;
            }
            return _url;
        }
    }

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

    /// <summary>The request or dependency a span is.</summary>
    public static AzureMonitorLiveItem FromSpan(BelemetrySpan span) => new(TypeOf(span), AzureMonitorProperties.OfSpan(span), span);

    /// <summary>The exception a log record carries, or one recorded on a span (from the event's attributes).</summary>
    public static AzureMonitorLiveItem FromException(BelemetryAttributes attributes, string? body)
    {
        return new AzureMonitorLiveItem(AzureMonitorLiveTelemetryType.Exception, AzureMonitorProperties.OfException(attributes))
        {
            ExceptionType = attributes.GetString(BelemetrySemanticConventions.ExceptionType) ?? "Error",
            ExceptionMessage = attributes.GetString(BelemetrySemanticConventions.ExceptionMessage) ?? body ?? string.Empty,
            StackTrace = attributes.GetString(BelemetrySemanticConventions.ExceptionStacktrace) ?? string.Empty,
        };
    }

    /// <summary>The item of a log record, named as the envelope names it; null when <see cref="TypeOf(BelemetryLogRecord)"/> is.</summary>
    public static AzureMonitorLiveItem? FromLog(BelemetryLogRecord log)
    {
        return TypeOf(log) switch
        {
            AzureMonitorLiveTelemetryType.Exception => FromException(log.Attributes, log.Body),
            AzureMonitorLiveTelemetryType.Event => new AzureMonitorLiveItem(AzureMonitorLiveTelemetryType.Event, AzureMonitorProperties.OfLog(log),
                                                                            name: log.EventName == BelemetrySemanticConventions.PageVisitTimeEvent ? "PageVisitTime" : log.EventName),
            AzureMonitorLiveTelemetryType.Trace => new AzureMonitorLiveItem(AzureMonitorLiveTelemetryType.Trace, AzureMonitorProperties.OfLog(log))
            {
                Message = log.Body ?? string.Empty,
            },
            _ => null,
        };
    }

    /// <summary>
    /// The trace a named span event other than an exception is: the envelope's <c>Message</c> of it, with the event's
    /// name as its message and its attributes as its custom dimensions.
    /// </summary>
    public static AzureMonitorLiveItem FromSpanEvent(BelemetrySpanEvent spanEvent) =>
        new(AzureMonitorLiveTelemetryType.Trace, AzureMonitorProperties.Of(spanEvent.Attributes)) { Message = spanEvent.Name };

    /// <summary>A custom dimension: a property of the item the envelope writes (<see cref="AzureMonitorProperties"/>).</summary>
    public bool TryGetDimension(string key, [NotNullWhen(true)] out string? value) => _properties.TryGetValue(key, out value);

    /// <summary>
    /// Whether one of the item's text fields or custom dimensions contains <paramref name="text"/>, ignoring case: what a
    /// filter on any field (<c>*</c>) looks for.
    /// </summary>
    public bool AnyFieldContains(string text)
    {
        var found = Type switch
        {
            AzureMonitorLiveTelemetryType.Request => Has(Name) || Has(Url) || Has(Code),
            AzureMonitorLiveTelemetryType.Dependency => Has(Name) || Has(Target) || Has(DependencyType) || Has(Data) || Has(Code),
            AzureMonitorLiveTelemetryType.Exception => Has(ExceptionType) || Has(ExceptionMessage) || Has(StackTrace),
            AzureMonitorLiveTelemetryType.Event => Has(Name),
            _ => Has(Message),
        };
        if (found) return true;

        foreach (var (_, value) in _properties)
        {
            if (Has(value)) return true;
        }
        return false;

        bool Has(string value) => value.Contains(text, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>The sample document (<c>DocumentIngress</c>) for the document streams <paramref name="streams"/>.</summary>
    public byte[] ToDocument(IReadOnlyList<AzureMonitorLiveConfiguration.DocumentStream> streams)
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
            foreach (var stream in streams) w.WriteStringValue(stream.Id);
            w.WriteEndArray();

            w.WriteStartArray("Properties");
            foreach (var (key, value) in _properties)
            {
                w.WriteStartObject();
                w.WriteString("key", Truncate(key, MaxKeyLength));
                w.WriteString("value", Truncate(value, MaxValueLength));
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

    // Never inside a surrogate pair: the writer refuses a lone surrogate, and the sample would be lost.
    private static string Truncate(string value, int max) => BelemetryValueLimit.Truncate(value, max);
}
