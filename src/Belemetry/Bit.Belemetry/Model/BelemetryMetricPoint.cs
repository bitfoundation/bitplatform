namespace Bit.Belemetry;

/// <summary>
/// A metric data point for one attribute set, carrying both the cumulative value (since the stream started)
/// and the delta value (since the previous collection), so every exporter can pick the temporality its backend
/// wants without the pipeline aggregating twice.
/// </summary>
public sealed class BelemetryMetricPoint
{
    /// <summary>The point attributes.</summary>
    public BelemetryAttributes Attributes { get; init; } = new();

    /// <summary>When the cumulative stream started, in nanoseconds since the Unix epoch.</summary>
    public long StartTimeUnixNano { get; set; }

    /// <summary>When the previous collection happened (the start of the delta interval), in nanoseconds since the Unix epoch.</summary>
    public long DeltaStartTimeUnixNano { get; set; }

    /// <summary>When this collection happened, in nanoseconds since the Unix epoch.</summary>
    public long TimeUnixNano { get; set; }

    /// <summary>Whether the instrument records integers (so <see cref="LongValue"/> is authoritative over <see cref="Value"/>).</summary>
    public bool IsInteger { get; set; }

    /// <summary>Whether the point received a measurement during the delta interval.</summary>
    public bool HasDeltaUpdate { get; set; }

    /// <summary>Gauge: the last value. Sum: the cumulative sum.</summary>
    public double Value { get; set; }

    /// <summary>Gauge: the last value. Sum: the cumulative sum (integer instruments).</summary>
    public long LongValue { get; set; }

    /// <summary>Sum: the change during the delta interval.</summary>
    public double DeltaValue { get; set; }

    /// <summary>Sum: the change during the delta interval (integer instruments).</summary>
    public long DeltaLongValue { get; set; }

    /// <summary>Histogram: the cumulative number of measurements.</summary>
    public long Count { get; set; }

    /// <summary>Histogram: the cumulative sum of measurements.</summary>
    public double Sum { get; set; }

    /// <summary>Histogram: the smallest measurement since the stream started.</summary>
    public double Min { get; set; }

    /// <summary>Histogram: the largest measurement since the stream started.</summary>
    public double Max { get; set; }

    /// <summary>Histogram: the cumulative count per bucket (one more than <see cref="ExplicitBounds"/>).</summary>
    public long[] BucketCounts { get; set; } = [];

    /// <summary>Histogram: the number of measurements during the delta interval.</summary>
    public long DeltaCount { get; set; }

    /// <summary>Histogram: the sum of measurements during the delta interval.</summary>
    public double DeltaSum { get; set; }

    /// <summary>Histogram: the smallest measurement during the delta interval.</summary>
    public double DeltaMin { get; set; }

    /// <summary>Histogram: the largest measurement during the delta interval.</summary>
    public double DeltaMax { get; set; }

    /// <summary>Histogram: the count per bucket during the delta interval.</summary>
    public long[] DeltaBucketCounts { get; set; } = [];

    /// <summary>Histogram: the bucket upper bounds (exclusive lower, inclusive upper, as in OTLP).</summary>
    public double[] ExplicitBounds { get; set; } = [];
}
