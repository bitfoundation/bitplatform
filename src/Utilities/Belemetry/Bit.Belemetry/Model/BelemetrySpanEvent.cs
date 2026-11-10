namespace Bit.Belemetry;

/// <summary>A time-stamped annotation on a span, shaped like the OTLP <c>Span.Event</c> message.</summary>
public sealed class BelemetrySpanEvent
{
    /// <summary>The event name.</summary>
    public required string Name { get; set; }

    /// <summary>The event time, in nanoseconds since the Unix epoch.</summary>
    public long TimeUnixNano { get; set; }

    /// <summary>The event attributes.</summary>
    public BelemetryAttributes Attributes { get; init; } = new();

    /// <summary>
    /// Whether it records an exception: an <c>exception</c> event (<c>Activity.AddException</c>) with <c>exception.type</c>,
    /// <c>exception.message</c> or <c>exception.stacktrace</c>. Such an event is <see cref="BelemetryFeatures.Errors"/> data
    /// carried by its span. Only one with a type or a message (<see cref="BelemetryAttributes.HasException"/>) is an error
    /// record of its own - limited, matched to a log record, an Azure Monitor <c>Exception</c>; a stack trace alone is not.
    /// </summary>
    internal bool IsException => Name == BelemetrySemanticConventions.ExceptionEvent
                                 && (Attributes.HasException || Attributes.Contains(BelemetrySemanticConventions.ExceptionStacktrace));

    /// <summary>
    /// For an exception event: the error limits (<see cref="BelemetryOptions.Errors"/>) dropped it as a record of its own,
    /// so an exporter that sends it as an item of its own (an Azure Monitor <c>Exception</c>) does not. It stays on the
    /// span. Never exported.
    /// </summary>
    internal bool Limited { get; set; }

    /// <summary>
    /// For an exception event: the log record of the same exception, logged while its span was current. An exporter that
    /// sends that record as an item of its own does not send another one for this event - unless the record was dropped
    /// before any exporter had it (<see cref="BelemetryLoggedException.IsDropped"/>). Never exported.
    /// </summary>
    internal BelemetryLoggedException? LoggedAs { get; set; }
}
