namespace Bit.Belemetry;

/// <summary>The OTLP aggregation temporality (numbered as in <c>opentelemetry/proto/metrics/v1/metrics.proto</c>).</summary>
public enum BelemetryMetricTemporality
{
    /// <summary>Each point reports the change since the previous export.</summary>
    Delta = 1,
    /// <summary>Each point reports the total since the stream started (the OpenTelemetry default for OTLP).</summary>
    Cumulative = 2,
}
