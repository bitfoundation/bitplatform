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

    /// <summary>
    /// The fraction of spans like this one that sampling kept, above 0 and up to 1: each one exported stands for
    /// <c>1 / SamplingRatio</c> of them. For a .NET span it is the ratio its trace was kept at, which the trace carries in
    /// its <c>tracestate</c> (the <c>belemetry</c> entry). That is <see cref="BelemetryTracingOptions.SamplingRatio"/> for
    /// a trace this process started, and the caller's for one that came from a Belemetry service, WebAssembly client or
    /// Hangfire job, but never below this process's own ratio (1 when it does not sample): anyone can send a tracestate.
    /// It is 1 for a trace whose ratio is not known. For a span relayed from the browser it is the page's session ratio
    /// (<see cref="BelemetryBrowserOptions.SessionSampleRatio"/>). A processor that samples spans itself lowers it: with
    /// <see cref="BelemetrySampler"/> to the smaller of it and its own ratio (the decisions nest: a trace one ratio keeps,
    /// every larger one keeps too), with an independent (random) decision by multiplying it by its ratio. The Azure Monitor
    /// exporter sends it as the envelope's <c>sampleRate</c> (a percentage of <c>100 / N</c>), so Application Insights
    /// counts each item <c>1 / SamplingRatio</c> times, rounded. Defaults to 1.
    /// </summary>
    public double SamplingRatio { get; set; } = 1;
}
