namespace Bit.Belemetry;

/// <summary>A link from a span to another span, shaped like the OTLP <c>Span.Link</c> message.</summary>
public sealed class BelemetrySpanLink
{
    /// <summary>The linked trace id: 32 lowercase hex characters.</summary>
    public required string TraceId { get; set; }

    /// <summary>The linked span id: 16 lowercase hex characters.</summary>
    public required string SpanId { get; set; }

    /// <summary>The linked span's W3C <c>tracestate</c>.</summary>
    public string? TraceState { get; set; }

    /// <summary>The link attributes.</summary>
    public BelemetryAttributes Attributes { get; init; } = new();
}
