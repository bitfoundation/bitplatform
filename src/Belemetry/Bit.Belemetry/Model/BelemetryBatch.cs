namespace Bit.Belemetry;

/// <summary>A unit of export: records that share one resource.</summary>
public sealed class BelemetryBatch
{
    /// <summary>Creates a batch.</summary>
    public BelemetryBatch(BelemetryResource resource,
                          IReadOnlyList<BelemetrySpan>? spans = null,
                          IReadOnlyList<BelemetryLogRecord>? logs = null,
                          IReadOnlyList<BelemetryMetric>? metrics = null)
    {
        ArgumentNullException.ThrowIfNull(resource);
        Resource = resource;
        Spans = spans ?? [];
        Logs = logs ?? [];
        Metrics = metrics ?? [];
    }

    /// <summary>The resource every record in the batch belongs to.</summary>
    public BelemetryResource Resource { get; }

    /// <summary>The spans.</summary>
    public IReadOnlyList<BelemetrySpan> Spans { get; }

    /// <summary>The log records.</summary>
    public IReadOnlyList<BelemetryLogRecord> Logs { get; }

    /// <summary>The metrics.</summary>
    public IReadOnlyList<BelemetryMetric> Metrics { get; }

    /// <summary>Whether the batch holds no records.</summary>
    public bool IsEmpty => Spans.Count == 0 && Logs.Count == 0 && Metrics.Count == 0;
}
