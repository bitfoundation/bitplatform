namespace Bit.Belemetry;

/// <summary>
/// One collection of a metric stream, shaped like the OTLP <c>Metric</c> message: an instrument with one
/// point per distinct attribute set.
/// </summary>
public sealed class BelemetryMetric
{
    /// <summary>The instrument name.</summary>
    public required string Name { get; set; }

    /// <summary>The instrument description.</summary>
    public string? Description { get; set; }

    /// <summary>The instrument unit (UCUM), e.g. <c>ms</c> or <c>{request}</c>.</summary>
    public string? Unit { get; set; }

    /// <summary>The data type.</summary>
    public BelemetryMetricType Type { get; set; }

    /// <summary>Whether a <see cref="BelemetryMetricType.Sum"/> only ever increases.</summary>
    public bool IsMonotonic { get; set; }

    /// <summary>The instrumentation scope (the <c>Meter</c>).</summary>
    public BelemetryScope Scope { get; set; } = BelemetryScope.Default;

    /// <summary>The data points, one per attribute set.</summary>
    public List<BelemetryMetricPoint> Points { get; init; } = [];
}
