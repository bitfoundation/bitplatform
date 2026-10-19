using System.Collections.Concurrent;
using System.Diagnostics.Metrics;

namespace Bit.Belemetry;

/// <summary>
/// Aggregates measurements of the configured meters with the BCL <see cref="MeterListener"/> and snapshots them on
/// each collection - no OpenTelemetry SDK needed.
/// </summary>
internal sealed class BelemetryMeterCollector : IDisposable
{
    private readonly BelemetrySettings _settings;
    private readonly MeterListener _listener;
    private readonly ConcurrentDictionary<Instrument, BelemetryInstrumentAggregator> _aggregators = new();
    private readonly long _startTimeUnixNano = BelemetryTime.NowUnixNano();

    public BelemetryMeterCollector(BelemetrySettings settings)
    {
        _settings = settings;

        _listener = new MeterListener
        {
            InstrumentPublished = (instrument, listener) =>
            {
                if (BelemetrySettings.Matches(instrument.Meter.Name, _settings.Meters) is false) return;

                var aggregator = _aggregators.GetOrAdd(instrument, i => new BelemetryInstrumentAggregator(i, _settings.Options.Metrics, _startTimeUnixNano));
                listener.EnableMeasurementEvents(instrument, aggregator);
            },
        };

        _listener.SetMeasurementEventCallback<byte>(static (i, v, t, s) => Record(s, v, v, t, true));
        _listener.SetMeasurementEventCallback<short>(static (i, v, t, s) => Record(s, v, v, t, true));
        _listener.SetMeasurementEventCallback<int>(static (i, v, t, s) => Record(s, v, v, t, true));
        _listener.SetMeasurementEventCallback<long>(static (i, v, t, s) => Record(s, v, v, t, true));
        _listener.SetMeasurementEventCallback<float>(static (i, v, t, s) => Record(s, v, (long)v, t, false));
        _listener.SetMeasurementEventCallback<double>(static (i, v, t, s) => Record(s, v, (long)v, t, false));
        _listener.SetMeasurementEventCallback<decimal>(static (i, v, t, s) => Record(s, (double)v, (long)v, t, false));

        _listener.Start();
    }

    private static void Record(object? state, double value, long longValue, ReadOnlySpan<KeyValuePair<string, object?>> tags, bool isInteger)
    {
        if (state is not BelemetryInstrumentAggregator aggregator || BelemetrySuppression.IsActive) return;

        aggregator.IsInteger = isInteger;
        try
        {
            aggregator.Record(value, longValue, tags);
        }
        catch
        {
            // Never let telemetry break the instrumented code path.
        }
    }

    /// <summary>Polls the observable instruments, then snapshots every instrument.</summary>
    public List<BelemetryMetric> Collect()
    {
        try
        {
            _listener.RecordObservableInstruments();
        }
        catch
        {
            // An observable callback that throws must not cost the other instruments their collection.
        }

        var now = BelemetryTime.NowUnixNano();
        var metrics = new List<BelemetryMetric>();
        foreach (var aggregator in _aggregators.Values)
        {
            if (aggregator.Collect(now) is { } metric) metrics.Add(metric);
        }
        return metrics;
    }

    public void Dispose() => _listener.Dispose();
}
