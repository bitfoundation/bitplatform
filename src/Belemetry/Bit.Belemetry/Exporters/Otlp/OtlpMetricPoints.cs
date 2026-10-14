namespace Bit.Belemetry;

/// <summary>Picks the points of a metric that belong in an export of a given temporality.</summary>
internal static class OtlpMetricPoints
{
    public static List<BelemetryMetricPoint> Select(BelemetryMetric metric, BelemetryMetricTemporality temporality)
    {
        // A delta stream reports only what changed: a point without measurements in the interval would be a
        // misleading zero for a sum and an empty histogram, so it is left out, as the OTel SDKs do. Gauges and
        // cumulative streams always report their current state.
        if (temporality == BelemetryMetricTemporality.Delta && metric.Type != BelemetryMetricType.Gauge)
        {
            return metric.Points.Where(p => p.HasDeltaUpdate || (metric.Type == BelemetryMetricType.Sum && (p.DeltaValue != 0 || p.DeltaLongValue != 0))).ToList();
        }

        return metric.Points;
    }
}
