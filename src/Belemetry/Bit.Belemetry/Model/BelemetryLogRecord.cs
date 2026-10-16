namespace Bit.Belemetry;

/// <summary>
/// A log record or event, shaped like the OTLP <c>LogRecord</c> message. Produced from <c>ILogger</c> entries,
/// <see cref="IBelemetry"/> calls, or relayed from the browser (page views, interactions, web vitals, JS errors).
/// </summary>
/// <remarks>Processors may mutate any member before the record is queued for export.</remarks>
public sealed class BelemetryLogRecord
{
    /// <summary>When the event occurred, in nanoseconds since the Unix epoch.</summary>
    public long TimeUnixNano { get; set; }

    /// <summary>When the event was observed by Belemetry, in nanoseconds since the Unix epoch.</summary>
    public long ObservedTimeUnixNano { get; set; }

    /// <summary>The severity number.</summary>
    public BelemetrySeverity Severity { get; set; } = BelemetrySeverity.Info;

    /// <summary>The severity text, e.g. <c>Information</c> or <c>ERROR</c>.</summary>
    public string? SeverityText { get; set; }

    /// <summary>The body - the formatted log message.</summary>
    public string? Body { get; set; }

    /// <summary>The event name, for records that represent a named event (e.g. <c>browser.page_view</c>).</summary>
    public string? EventName { get; set; }

    /// <summary>The record attributes.</summary>
    public BelemetryAttributes Attributes { get; init; } = new();

    /// <summary>The trace id of the span active when the record was produced (32 lowercase hex characters).</summary>
    public string? TraceId { get; set; }

    /// <summary>The span id of the span active when the record was produced (16 lowercase hex characters).</summary>
    public string? SpanId { get; set; }

    /// <summary>The W3C trace flags of that span.</summary>
    public int Flags { get; set; }

    /// <summary>The instrumentation scope (for <c>ILogger</c> entries, the logger category).</summary>
    public BelemetryScope Scope { get; set; } = BelemetryScope.Default;

    /// <summary>The consent feature this record belongs to.</summary>
    public BelemetryFeatures Feature { get; set; } = BelemetryFeatures.Logs;

    /// <summary>Whether the record carries an exception (<c>exception.type</c> or <c>exception.message</c>).</summary>
    public bool HasException => Attributes.Contains(BelemetrySemanticConventions.ExceptionType) || Attributes.Contains(BelemetrySemanticConventions.ExceptionMessage);
}
