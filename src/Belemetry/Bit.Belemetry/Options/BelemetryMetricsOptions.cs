namespace Bit.Belemetry;

/// <summary><see cref="System.Diagnostics.Metrics.Meter"/> collection.</summary>
public sealed class BelemetryMetricsOptions
{
    /// <summary>
    /// The <see cref="System.Diagnostics.Metrics.Meter"/> names collected; a trailing <c>*</c> matches a prefix.
    /// Defaults to <c>System.Net.Http</c>, <c>Microsoft.AspNetCore.Components*</c> and <c>System.Runtime</c>
    /// (plus the ASP.NET Core hosting and Kestrel meters for the server pipeline). Add your own meters here.
    /// </summary>
    public List<string> Meters { get; } = ["System.Net.Http", "Microsoft.AspNetCore.Components*", "System.Runtime"];

    /// <summary>How often metrics are collected and exported. Defaults to 60 seconds (the OpenTelemetry default).</summary>
    public TimeSpan ExportInterval { get; set; } = TimeSpan.FromSeconds(60);

    /// <summary>The aggregation temporality sent over OTLP. Defaults to cumulative (Azure Monitor always gets deltas).</summary>
    public BelemetryMetricTemporality Temporality { get; set; } = BelemetryMetricTemporality.Cumulative;

    /// <summary>The OpenTelemetry default histogram bucket boundaries, used while <see cref="HistogramBoundaries"/> is empty.</summary>
    public static readonly double[] DefaultHistogramBoundaries = [0, 5, 10, 25, 50, 75, 100, 250, 500, 750, 1000, 2500, 5000, 7500, 10000];

    /// <summary>
    /// The histogram bucket boundaries, used unless an instrument advises its own. Empty, the default, means
    /// <see cref="DefaultHistogramBoundaries"/> - empty rather than those values themselves because a configuration
    /// binder appends to an array it binds, and boundaries must replace the defaults, never grow them.
    /// </summary>
    public double[] HistogramBoundaries { get; set; } = [];

    /// <summary>
    /// The maximum number of attribute sets per instrument (the OpenTelemetry default is 2000). Measurements with new
    /// attribute sets past it are folded into one point carrying <c>otel.metric.overflow = true</c>.
    /// </summary>
    public int CardinalityLimit { get; set; } = 2000;
}
