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
}
