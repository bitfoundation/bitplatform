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
    /// Whether it records an exception: an <c>exception</c> event (<c>Activity.AddException</c>) with <c>exception.type</c>
    /// or <c>exception.message</c>. Such an event is an <see cref="BelemetryFeatures.Errors"/> record carried by its span.
    /// </summary>
    internal bool IsException => Name == BelemetrySemanticConventions.ExceptionEvent && Attributes.HasException;

    /// <summary>
    /// For an exception event: the error limits (<see cref="BelemetryOptions.Errors"/>) dropped it as a record of its own,
    /// so an exporter that sends it as an item of its own (an Azure Monitor <c>Exception</c>) does not. It stays on the
    /// span. Never exported.
    /// </summary>
    internal bool Limited { get; set; }

    /// <summary>
    /// For an exception event: the log record of the same exception, logged while its span was current. An exporter that
    /// sends that record as an item of its own does not send another one for this event. Never exported.
    /// </summary>
    internal BelemetryLoggedException? LoggedAs { get; set; }
}
