using System.Diagnostics.Metrics;
using Hangfire;

namespace Bit.Belemetry.Tests.Hangfire;

/// <summary>
/// The execution metrics and the storage gauges, on the server <see cref="BelemetryHangfireFilterTests"/> starts (one
/// process-wide Hangfire configuration, and the tests run one at a time).
/// </summary>
[TestClass]
public class BelemetryHangfireMetricsTests
{
    private sealed record Recorded(string Instrument, double Value, Dictionary<string, object?> Tags);

    private sealed class Recorder : IDisposable
    {
        private readonly MeterListener _listener = new();
        private readonly List<Recorded> _recorded = [];

        public Recorder()
        {
            _listener.InstrumentPublished = (instrument, listener) =>
            {
                if (instrument.Meter.Name == BelemetryHangfireFilter.MeterName) listener.EnableMeasurementEvents(instrument);
            };
            _listener.SetMeasurementEventCallback<long>((instrument, value, tags, _) => Add(instrument, value, tags));
            _listener.SetMeasurementEventCallback<double>((instrument, value, tags, _) => Add(instrument, value, tags));
            _listener.Start();
        }

        private void Add(Instrument instrument, double value, ReadOnlySpan<KeyValuePair<string, object?>> tags)
        {
            var map = new Dictionary<string, object?>();
            foreach (var (key, tagValue) in tags) map[key] = tagValue;
            lock (_recorded) _recorded.Add(new Recorded(instrument.Name, value, map));
        }

        public List<Recorded> Of(string instrument)
        {
            lock (_recorded) return _recorded.Where(r => r.Instrument == instrument).ToList();
        }

        public void Collect() => _listener.RecordObservableInstruments();

        public void Dispose() => _listener.Dispose();
    }

    private static async Task RunAsync(Action enqueue)
    {
        BelemetryHangfireFilterTests.Jobs.Completed = new TaskCompletionSource<string?>(TaskCreationOptions.RunContinuationsAsynchronously);
        enqueue();
        var completed = await Task.WhenAny(BelemetryHangfireFilterTests.Jobs.Completed.Task, Task.Delay(TimeSpan.FromSeconds(15)));
        Assert.AreSame(BelemetryHangfireFilterTests.Jobs.Completed.Task, completed, "The job did not run in time.");
        // OnPerformed runs after the job method returns.
        await Task.Delay(200);
    }

    [TestMethod]
    public async Task AnExecution_IsCountedAndTimed_ByTypeMethodAndOutcome()
    {
        using var recorder = new Recorder();

        await RunAsync(() => BackgroundJob.Enqueue(() => BelemetryHangfireFilterTests.Jobs.Succeed("ok")));
        await RunAsync(() => BackgroundJob.Enqueue(() => BelemetryHangfireFilterTests.Jobs.Fail("boom")));

        var executions = recorder.Of("hangfire.job.executions");
        var durations = recorder.Of("hangfire.job.duration");
        Assert.HasCount(2, executions);
        Assert.HasCount(2, durations);

        var succeeded = executions.Single(r => (string?)r.Tags["job.method"] == "Succeed");
        Assert.AreEqual(1, succeeded.Value);
        Assert.AreEqual(typeof(BelemetryHangfireFilterTests.Jobs).FullName, succeeded.Tags["job.type"]);
        Assert.AreEqual("succeeded", succeeded.Tags["job.outcome"]);
        Assert.IsFalse(succeeded.Tags.ContainsKey("error.type"));
        Assert.IsFalse(succeeded.Tags.ContainsKey("job.id"), "An id per execution would make a series per execution.");

        var failed = executions.Single(r => (string?)r.Tags["job.method"] == "Fail");
        Assert.AreEqual("failed", failed.Tags["job.outcome"]);
        Assert.AreEqual(typeof(InvalidOperationException).FullName, failed.Tags["error.type"], "The job's exception, not Hangfire's wrapper.");

        Assert.IsTrue(durations.All(d => d.Value >= 0 && d.Value < 15), "Seconds.");
        Assert.AreEqual("failed", durations.Single(r => (string?)r.Tags["job.method"] == "Fail").Tags["job.outcome"]);
    }

    [TestMethod]
    public async Task Filter_SkipsTheMetricsToo()
    {
        using var recorder = new Recorder();
        await RunAsync(() => BackgroundJob.Enqueue(() => BelemetryHangfireFilterTests.Jobs.Skipped()));
        Assert.IsEmpty(recorder.Of("hangfire.job.executions"));
        Assert.IsEmpty(recorder.Of("hangfire.job.duration"));
    }

    [TestMethod]
    public void TheStorageGauges_ReportTheStorageStatistics_FromOneQueryPerCollection()
    {
        var id = BackgroundJob.Schedule(() => BelemetryHangfireFilterTests.Jobs.Succeed("later"), TimeSpan.FromDays(1));
        try
        {
            BelemetryHangfireMetrics.ResetStatistics();
            using var recorder = new Recorder();
            recorder.Collect();

            var statistics = BelemetryHangfireMetrics.Statistics();
            Assert.IsNotNull(statistics);
            Assert.AreSame(statistics, BelemetryHangfireMetrics.Statistics(), "Read once, then served from the cache while it is fresh.");

            Assert.AreEqual(statistics.Scheduled, recorder.Of("hangfire.jobs.scheduled").Single().Value);
            Assert.IsGreaterThanOrEqualTo(1, recorder.Of("hangfire.jobs.scheduled").Single().Value, "The job scheduled for tomorrow.");
            Assert.AreEqual(statistics.Enqueued, recorder.Of("hangfire.jobs.enqueued").Single().Value);
            Assert.AreEqual(statistics.Processing, recorder.Of("hangfire.jobs.processing").Single().Value);
            Assert.AreEqual(statistics.Failed, recorder.Of("hangfire.jobs.failed").Single().Value);
            Assert.AreEqual(statistics.Retries ?? 0, recorder.Of("hangfire.jobs.retries").Single().Value);
        }
        finally
        {
            BackgroundJob.Delete(id);
        }
    }

    [TestMethod]
    public void TheStorageGauges_AreCreatedOnce_HoweverOftenTheFilterIsRegistered()
    {
        BelemetryHangfireMetrics.EnableStorageGauges();
        BelemetryHangfireMetrics.EnableStorageGauges();

        using var recorder = new Recorder();
        recorder.Collect();
        Assert.HasCount(1, recorder.Of("hangfire.jobs.enqueued"));
    }
}
