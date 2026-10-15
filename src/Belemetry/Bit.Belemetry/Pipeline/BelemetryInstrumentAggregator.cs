using System.Diagnostics.Metrics;
using System.Text;

namespace Bit.Belemetry;

/// <summary>
/// Aggregates the measurements of one instrument per attribute set, keeping cumulative and delta state side by
/// side (see <see cref="BelemetryMetricPoint"/>).
/// </summary>
internal sealed class BelemetryInstrumentAggregator
{
    private const string OverflowKey = "\u0000otel.metric.overflow";

    private readonly object _lock = new();
    private readonly Dictionary<string, PointState> _points = new(StringComparer.Ordinal);
    private readonly int _cardinalityLimit;
    private readonly long _startTimeUnixNano;
    private long _lastCollectUnixNano;

    public BelemetryInstrumentAggregator(Instrument instrument, BelemetryMetricsOptions options, long startTimeUnixNano)
    {
        Instrument = instrument;
        _cardinalityLimit = Math.Max(1, options.CardinalityLimit);
        _startTimeUnixNano = startTimeUnixNano;
        _lastCollectUnixNano = startTimeUnixNano;

        // The generic type's name tells the instrument kind without reflection over generic arguments, which
        // keeps this trimming-safe for every T.
        var kind = instrument.GetType().Name;
        var tick = kind.IndexOf('`', StringComparison.Ordinal);
        if (tick > 0) kind = kind[..tick];

        switch (kind)
        {
            case "Counter":
                Type = BelemetryMetricType.Sum;
                IsMonotonic = true;
                break;
            case "UpDownCounter":
                Type = BelemetryMetricType.Sum;
                break;
            case "ObservableCounter":
                Type = BelemetryMetricType.Sum;
                IsMonotonic = true;
                ObservesCumulative = true;
                break;
            case "ObservableUpDownCounter":
                Type = BelemetryMetricType.Sum;
                ObservesCumulative = true;
                break;
            case "Histogram":
                Type = BelemetryMetricType.Histogram;
                break;
            default: // Gauge, ObservableGauge and anything newer: last value wins.
                Type = BelemetryMetricType.Gauge;
                break;
        }

        Bounds = Type == BelemetryMetricType.Histogram ? (AdvisedBounds(instrument) ?? options.HistogramBoundaries) : [];
    }

    public Instrument Instrument { get; }

    public BelemetryMetricType Type { get; }

    public bool IsMonotonic { get; }

    /// <summary>Observable counters report running totals, not increments.</summary>
    public bool ObservesCumulative { get; }

    public double[] Bounds { get; }

    public bool IsInteger { get; set; }

    public void Record(double value, long longValue, ReadOnlySpan<KeyValuePair<string, object?>> tags)
    {
        if (double.IsNaN(value) || double.IsInfinity(value)) return;
        if (Type == BelemetryMetricType.Sum && IsMonotonic && ObservesCumulative is false && value < 0) return; // a counter must not decrease

        var key = BuildKey(tags);

        lock (_lock)
        {
            if (_points.TryGetValue(key, out var point) is false)
            {
                if (_points.Count >= _cardinalityLimit)
                {
                    key = OverflowKey;
                    if (_points.TryGetValue(key, out point) is false)
                    {
                        point = new PointState(new BelemetryAttributes().Set("otel.metric.overflow", true), Bounds.Length);
                        _points[key] = point;
                    }
                }
                else
                {
                    var attributes = new BelemetryAttributes();
                    foreach (var (k, v) in tags) attributes.Set(k, v);
                    point = new PointState(attributes, Bounds.Length);
                    _points[key] = point;
                }
            }

            point.Updated = true;

            switch (Type)
            {
                case BelemetryMetricType.Gauge:
                    point.Value = value;
                    point.LongValue = longValue;
                    break;

                case BelemetryMetricType.Sum when ObservesCumulative:
                    point.Value = value;
                    point.LongValue = longValue;
                    break;

                case BelemetryMetricType.Sum:
                    point.Value += value;
                    point.LongValue += longValue;
                    point.DeltaValue += value;
                    point.DeltaLongValue += longValue;
                    break;

                case BelemetryMetricType.Histogram:
                    var bucket = FindBucket(value);
                    point.Count++;
                    point.Sum += value;
                    point.Min = point.Count == 1 ? value : Math.Min(point.Min, value);
                    point.Max = point.Count == 1 ? value : Math.Max(point.Max, value);
                    point.Buckets[bucket]++;
                    point.DeltaCount++;
                    point.DeltaSum += value;
                    point.DeltaMin = point.DeltaCount == 1 ? value : Math.Min(point.DeltaMin, value);
                    point.DeltaMax = point.DeltaCount == 1 ? value : Math.Max(point.DeltaMax, value);
                    point.DeltaBuckets[bucket]++;
                    break;
            }
        }
    }

    /// <summary>Snapshots every point and starts a new delta interval. Returns null when nothing was ever recorded.</summary>
    public BelemetryMetric? Collect(long nowUnixNano)
    {
        lock (_lock)
        {
            if (_points.Count == 0) return null;

            var metric = new BelemetryMetric
            {
                Name = Instrument.Name,
                Description = Instrument.Description,
                Unit = Instrument.Unit,
                Type = Type,
                IsMonotonic = IsMonotonic,
                Scope = new BelemetryScope(Instrument.Meter.Name, Instrument.Meter.Version),
            };

            foreach (var point in _points.Values)
            {
                var snapshot = new BelemetryMetricPoint
                {
                    Attributes = point.Attributes.Clone(),
                    StartTimeUnixNano = _startTimeUnixNano,
                    DeltaStartTimeUnixNano = _lastCollectUnixNano,
                    TimeUnixNano = nowUnixNano,
                    IsInteger = IsInteger,
                    HasDeltaUpdate = point.Updated,
                    Value = point.Value,
                    LongValue = point.LongValue,
                    ExplicitBounds = Bounds,
                };

                if (Type == BelemetryMetricType.Sum)
                {
                    if (ObservesCumulative)
                    {
                        snapshot.DeltaValue = point.Value - point.LastReportedValue;
                        snapshot.DeltaLongValue = point.LongValue - point.LastReportedLongValue;
                        point.LastReportedValue = point.Value;
                        point.LastReportedLongValue = point.LongValue;
                    }
                    else
                    {
                        snapshot.DeltaValue = point.DeltaValue;
                        snapshot.DeltaLongValue = point.DeltaLongValue;
                    }
                }
                else if (Type == BelemetryMetricType.Histogram)
                {
                    snapshot.Count = point.Count;
                    snapshot.Sum = point.Sum;
                    snapshot.Min = point.Min;
                    snapshot.Max = point.Max;
                    snapshot.BucketCounts = (long[])point.Buckets.Clone();
                    snapshot.DeltaCount = point.DeltaCount;
                    snapshot.DeltaSum = point.DeltaSum;
                    snapshot.DeltaMin = point.DeltaMin;
                    snapshot.DeltaMax = point.DeltaMax;
                    snapshot.DeltaBucketCounts = (long[])point.DeltaBuckets.Clone();
                }

                metric.Points.Add(snapshot);
                point.ResetDelta();
            }

            _lastCollectUnixNano = nowUnixNano;
            return metric;
        }
    }

    private int FindBucket(double value)
    {
        // OTLP explicit buckets are (previous, bound]: a value equal to a bound belongs to that bound's bucket.
        for (var i = 0; i < Bounds.Length; i++)
        {
            if (value <= Bounds[i]) return i;
        }
        return Bounds.Length;
    }

    private static string BuildKey(ReadOnlySpan<KeyValuePair<string, object?>> tags)
    {
        if (tags.Length == 0) return string.Empty;

        if (tags.Length == 1) return tags[0].Key + "\u0001" + BelemetryAttributes.ToInvariantString(BelemetryAttributes.Normalize(tags[0].Value));

        // The same attribute set can arrive in any order; sort a copy so it maps to one point.
        var copy = tags.ToArray();
        Array.Sort(copy, static (a, b) => string.CompareOrdinal(a.Key, b.Key));

        var builder = new StringBuilder();
        foreach (var (key, value) in copy)
        {
            builder.Append(key).Append('\u0001').Append(BelemetryAttributes.ToInvariantString(BelemetryAttributes.Normalize(value))).Append('\u0002');
        }
        return builder.ToString();
    }

    private static double[]? AdvisedBounds(Instrument instrument)
    {
#if NET9_0_OR_GREATER
        return instrument switch
        {
            Histogram<double> h => h.Advice?.HistogramBucketBoundaries?.ToArray(),
            Histogram<float> h => h.Advice?.HistogramBucketBoundaries?.Select(x => (double)x).ToArray(),
            Histogram<long> h => h.Advice?.HistogramBucketBoundaries?.Select(x => (double)x).ToArray(),
            Histogram<int> h => h.Advice?.HistogramBucketBoundaries?.Select(x => (double)x).ToArray(),
            _ => null,
        };
#else
        return null;
#endif
    }

    private sealed class PointState(BelemetryAttributes attributes, int boundsLength)
    {
        public BelemetryAttributes Attributes { get; } = attributes;
        public bool Updated;
        public double Value;
        public long LongValue;
        public double DeltaValue;
        public long DeltaLongValue;
        public double LastReportedValue;
        public long LastReportedLongValue;
        public long Count;
        public double Sum;
        public double Min;
        public double Max;
        public long[] Buckets = new long[boundsLength + 1];
        public long DeltaCount;
        public double DeltaSum;
        public double DeltaMin;
        public double DeltaMax;
        public long[] DeltaBuckets = new long[boundsLength + 1];

        public void ResetDelta()
        {
            Updated = false;
            DeltaValue = 0;
            DeltaLongValue = 0;
            DeltaCount = 0;
            DeltaSum = 0;
            DeltaMin = 0;
            DeltaMax = 0;
            Array.Clear(DeltaBuckets);
        }
    }
}
