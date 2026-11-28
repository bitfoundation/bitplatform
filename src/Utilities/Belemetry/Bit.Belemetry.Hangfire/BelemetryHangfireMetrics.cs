using System.Diagnostics;
using System.Diagnostics.Metrics;
using Hangfire;
using Hangfire.Storage.Monitoring;

namespace Bit.Belemetry;

/// <summary>
/// The instruments of the <see cref="BelemetryHangfireFilter.MeterName"/> meter: what the filter measures of every
/// execution, and the state of the job storage. Hangfire.Core has no <see cref="Meter"/> of its own, so this is where
/// an app's jobs are measured.
/// </summary>
internal static class BelemetryHangfireMetrics
{
    /// <summary>How long one read of the storage's statistics serves: every gauge of one collection reads it once.</summary>
    internal static readonly TimeSpan StatisticsLifetime = TimeSpan.FromSeconds(1);

    // OpenTelemetry's duration buckets (seconds), stretched to an hour: a job is not a request, and the default
    // buckets (made for milliseconds) would put nearly every execution in the first one.
    private static readonly double[] _durationBoundaries = [0.005, 0.01, 0.025, 0.05, 0.075, 0.1, 0.25, 0.5, 0.75, 1, 2.5, 5, 7.5, 10, 30, 60, 120, 300, 600, 1800, 3600];

    private static readonly Meter _meter = new(BelemetryHangfireFilter.MeterName, typeof(BelemetryHangfireMetrics).Assembly.GetName().Version?.ToString());

    public static readonly Counter<long> Executions = _meter.CreateCounter<long>("hangfire.job.executions", "{execution}",
        "Job executions, by job type, method and outcome.");

    public static readonly Histogram<double> Duration = _meter.CreateHistogram("hangfire.job.duration", "s",
        "The duration of job executions, by job type, method and outcome.",
        tags: null, advice: new InstrumentAdvice<double> { HistogramBucketBoundaries = _durationBoundaries });

    private static readonly Lock _gate = new();
    private static int _storageGauges;
    private static StatisticsDto? _statistics;
    private static long _statisticsTimestamp;

    /// <summary>Whether a measurement would reach anyone: nothing is timed or tagged otherwise.</summary>
    public static bool Enabled => Executions.Enabled || Duration.Enabled;

    /// <summary>
    /// Creates the storage gauges, once per process: the storage is <see cref="JobStorage.Current"/>, read when a
    /// collection observes them, so registering the filter before the storage is configured is fine.
    /// </summary>
    public static void EnableStorageGauges()
    {
        if (Interlocked.Exchange(ref _storageGauges, 1) == 1) return;

        _meter.CreateObservableGauge("hangfire.jobs.enqueued", () => Observe(static s => s.Enqueued), "{job}", "Jobs waiting in the storage's queues.");
        _meter.CreateObservableGauge("hangfire.jobs.processing", () => Observe(static s => s.Processing), "{job}", "Jobs being performed.");
        _meter.CreateObservableGauge("hangfire.jobs.scheduled", () => Observe(static s => s.Scheduled), "{job}", "Jobs scheduled for later, retries included.");
        _meter.CreateObservableGauge("hangfire.jobs.failed", () => Observe(static s => s.Failed), "{job}", "Jobs in the failed state.");
        _meter.CreateObservableGauge("hangfire.jobs.retries", () => Observe(static s => s.Retries ?? 0), "{job}", "Jobs waiting for an automatic retry.");
    }

    private static IEnumerable<Measurement<long>> Observe(Func<StatisticsDto, long> value) =>
        Statistics() is { } statistics ? [new Measurement<long>(value(statistics))] : [];

    /// <summary>
    /// The storage's statistics, queried at most once per <see cref="StatisticsLifetime"/>: the five gauges of one
    /// collection cost one query. Null when there is no storage or the query failed - a gauge then reports nothing,
    /// rather than a zero that would read as an empty queue.
    /// </summary>
    internal static StatisticsDto? Statistics()
    {
        lock (_gate)
        {
            var now = Stopwatch.GetTimestamp();
            if (_statisticsTimestamp != 0 && Stopwatch.GetElapsedTime(_statisticsTimestamp, now) < StatisticsLifetime) return _statistics;

            try
            {
                _statistics = JobStorage.Current.GetMonitoringApi().GetStatistics();
            }
            catch
            {
                // No storage configured yet, or the database is down: telemetry must never fail the collection.
                _statistics = null;
            }

            _statisticsTimestamp = now;
            return _statistics;
        }
    }

    /// <summary>Forgets the cached statistics, so the tests see a fresh query.</summary>
    internal static void ResetStatistics()
    {
        lock (_gate) _statisticsTimestamp = 0;
    }
}
