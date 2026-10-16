namespace Bit.Belemetry.Demo.Client.Services;

/// <summary>
/// One record the demo server's OTLP receiver decoded - a span, a log record or event, or a metric
/// point - flattened for display. The receiver (Server/Services/TelemetryStore.cs) produces it and the
/// live feed renders it, so this one type is the contract between them.
/// </summary>
public sealed record TelemetryRecord
{
    /// <summary>A sequence number, increasing across every bucket, for incremental polling.</summary>
    public long Id { get; init; }

    /// <summary><c>span</c>, <c>log</c> or <c>metric</c> - the OTLP signal it arrived on.</summary>
    public required string Signal { get; init; }

    /// <summary>What it is, for display: Span, Log, Event, Error or Metric.</summary>
    public required string Kind { get; init; }

    /// <summary>
    /// Who sent it: <c>browser</c> (the script), <c>wasm</c> (the .NET pipeline running in the browser)
    /// or <c>server</c> (the demo server's own pipeline) - read off the resource.
    /// </summary>
    public required string Source { get; init; }

    /// <summary>When it happened, in milliseconds since the Unix epoch.</summary>
    public double Time { get; init; }

    /// <summary>The span name, the event name, the log message or the metric name.</summary>
    public required string Name { get; init; }

    public string? Body { get; init; }

    public string? Severity { get; init; }

    public string? Scope { get; init; }

    public string? TraceId { get; init; }

    public string? SpanId { get; init; }

    public string? ParentSpanId { get; init; }

    public string? SpanKind { get; init; }

    public double? DurationMs { get; init; }

    public string? Status { get; init; }

    /// <summary>The metric's points, summarised (e.g. <c>sum=3</c> or <c>count=5 sum=12.5</c>).</summary>
    public string? Value { get; init; }

    public string? SessionId { get; init; }

    /// <summary>Whether it was filed with no session and is shown because it shares a trace with one of the session's records.</summary>
    public bool Correlated { get; init; }

    public Dictionary<string, string> Attributes { get; init; } = [];

    public Dictionary<string, string> Resource { get; init; } = [];

    public string[] Events { get; init; } = [];
}

/// <summary>The answer of <c>GET /api/telemetry</c>.</summary>
public sealed record TelemetryFeed
{
    public TelemetryRecord[] Records { get; init; } = [];

    /// <summary>The highest id handed out so far - pass it back as <c>after</c> to get only what is new.</summary>
    public long LastId { get; init; }

    public string? SessionId { get; init; }
}
