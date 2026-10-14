namespace Bit.Belemetry;

/// <summary>The OTLP metric data type a <see cref="BelemetryMetric"/> carries.</summary>
public enum BelemetryMetricType
{
    /// <summary>The last observed value (<c>Gauge&lt;T&gt;</c>, <c>ObservableGauge&lt;T&gt;</c>).</summary>
    Gauge,
    /// <summary>A sum of measurements (<c>Counter&lt;T&gt;</c>, <c>UpDownCounter&lt;T&gt;</c> and their observable forms).</summary>
    Sum,
    /// <summary>An explicit-bucket histogram (<c>Histogram&lt;T&gt;</c>).</summary>
    Histogram,
}
