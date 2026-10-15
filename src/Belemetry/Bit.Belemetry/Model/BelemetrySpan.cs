namespace Bit.Belemetry;

/// <summary>
/// A finished span, shaped like the OTLP <c>Span</c> message. Produced from a stopped
/// <see cref="System.Diagnostics.Activity"/> on the .NET side, or relayed from the browser.
/// </summary>
/// <remarks>Processors may mutate any member before the span is queued for export.</remarks>
public sealed class BelemetrySpan
{
    /// <summary>The trace id: 32 lowercase hex characters.</summary>
    public required string TraceId { get; set; }

    /// <summary>The span id: 16 lowercase hex characters.</summary>
    public required string SpanId { get; set; }

    /// <summary>The parent span id (16 lowercase hex characters), or <see langword="null"/> for a root span.</summary>
    public string? ParentSpanId { get; set; }

    /// <summary>The W3C <c>tracestate</c>.</summary>
    public string? TraceState { get; set; }

    /// <summary>The W3C trace flags (bit 0 = sampled).</summary>
    public int Flags { get; set; } = 1;

    /// <summary>The span name.</summary>
    public required string Name { get; set; }

    /// <summary>The span kind.</summary>
    public BelemetrySpanKind Kind { get; set; } = BelemetrySpanKind.Internal;

    /// <summary>The start time, in nanoseconds since the Unix epoch.</summary>
    public long StartTimeUnixNano { get; set; }

    /// <summary>The end time, in nanoseconds since the Unix epoch.</summary>
    public long EndTimeUnixNano { get; set; }

    /// <summary>The span duration.</summary>
    public TimeSpan Duration => TimeSpan.FromTicks(Math.Max(0, EndTimeUnixNano - StartTimeUnixNano) / 100);

    /// <summary>The span attributes.</summary>
    public BelemetryAttributes Attributes { get; init; } = new();

    /// <summary>The span events.</summary>
    public List<BelemetrySpanEvent> Events { get; init; } = [];

    /// <summary>The span links.</summary>
    public List<BelemetrySpanLink> Links { get; init; } = [];

    /// <summary>The status code.</summary>
    public BelemetryStatusCode Status { get; set; }

    /// <summary>The status description (only meaningful with <see cref="BelemetryStatusCode.Error"/>).</summary>
    public string? StatusMessage { get; set; }

    /// <summary>The instrumentation scope.</summary>
    public BelemetryScope Scope { get; set; } = BelemetryScope.Default;

    /// <summary>The consent feature this span belongs to.</summary>
    public BelemetryFeatures Feature { get; set; } = BelemetryFeatures.Traces;
}
