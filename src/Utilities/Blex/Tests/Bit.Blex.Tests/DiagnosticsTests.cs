using System.Diagnostics;
using System.Diagnostics.Metrics;
using Bit.Blex.Query;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Bit.Blex.Tests;

/// <summary>
/// <see cref="BlexDiagnostics"/>: actions, effects and query fetches as spans and metrics on the standard
/// .NET diagnostics APIs, recording names only.
/// </summary>
[TestClass]
[DoNotParallelize]
public class DiagnosticsTests
{
    private sealed class Recorder : IDisposable
    {
        private readonly ActivityListener _activities;
        private readonly MeterListener _meters = new();

        public List<Activity> Spans { get; } = [];

        public List<(string Instrument, double Value, Dictionary<string, object?> Tags)> Measurements { get; } = [];

        public Recorder()
        {
            _activities = new ActivityListener
            {
                ShouldListenTo = source => source.Name == BlexDiagnostics.SourceName,
                Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllDataAndRecorded,
                ActivityStopped = Spans.Add,
            };
            ActivitySource.AddActivityListener(_activities);

            _meters.InstrumentPublished = (instrument, listener) =>
            {
                if (instrument.Meter.Name == BlexDiagnostics.SourceName)
                    listener.EnableMeasurementEvents(instrument);
            };
            _meters.SetMeasurementEventCallback<long>((instrument, value, tags, _) => Record(instrument, value, tags));
            _meters.SetMeasurementEventCallback<double>((instrument, value, tags, _) => Record(instrument, value, tags));
            _meters.Start();
        }

        private void Record(Instrument instrument, double value, ReadOnlySpan<KeyValuePair<string, object?>> tags)
        {
            var copy = new Dictionary<string, object?>();
            foreach (var (key, tagValue) in tags)
                copy[key] = tagValue;

            lock (Measurements)
                Measurements.Add((instrument.Name, value, copy));
        }

        public IEnumerable<(string Instrument, double Value, Dictionary<string, object?> Tags)> Of(string instrument, string store)
        {
            lock (Measurements)
                return [.. Measurements.Where(m => m.Instrument == instrument && Equals(m.Tags.GetValueOrDefault("blex.store"), store))];
        }

        public void Dispose()
        {
            _activities.Dispose();
            _meters.Dispose();
        }
    }

    [TestMethod]
    public void An_action_is_a_span_and_is_counted_and_timed()
    {
        using var recorder = new Recorder();
        var manager = new BlexManager();
        var store = new CounterStore();
        manager.Register(store);

        store.Add(5);

        var span = recorder.Spans.Single(s => s.DisplayName == "counter/Add");
        Assert.AreEqual("counter", span.GetTagItem("blex.store"));
        Assert.AreEqual("Add", span.GetTagItem("blex.action"));
        Assert.AreEqual(ActivityStatusCode.Unset, span.Status);
        Assert.IsNull(span.GetTagItem("amount"), "arguments are never recorded");

        var count = Assert.ContainsSingle(recorder.Of("blex.actions", "counter"));
        Assert.AreEqual(1, count.Value);
        Assert.AreEqual("Add", count.Tags["blex.action"]);
        Assert.ContainsSingle(recorder.Of("blex.action.duration", "counter"));
    }

    [TestMethod]
    public async Task A_failed_effect_is_an_error_span_and_counted()
    {
        using var recorder = new Recorder();
        var manager = new BlexManager();
        var store = new FailingStore();
        manager.Register(store);

        await store.Load("x");

        var span = recorder.Spans.Single(s => s.DisplayName == "failing/Load");
        Assert.AreEqual(ActivityStatusCode.Error, span.Status);
        Assert.AreEqual(typeof(InvalidOperationException).FullName, span.GetTagItem("error.type"));
        Assert.ContainsSingle(recorder.Of("blex.action.errors", "failing"));
    }

    [TestMethod]
    public void An_action_inside_another_is_its_child_span()
    {
        using var recorder = new Recorder();
        var manager = new BlexManager();
        var store = new FailingStore();
        manager.Register(store);

        Assert.ThrowsExactly<InvalidOperationException>(() => store.Outer());

        var outer = recorder.Spans.Single(s => s.DisplayName == "failing/Outer");
        var inner = recorder.Spans.Single(s => s.DisplayName == "failing/Boom");
        Assert.AreEqual(outer.SpanId, inner.ParentSpanId);
    }

    [TestMethod]
    public async Task A_cancelled_effect_is_tagged_not_failed()
    {
        using var recorder = new Recorder();
        var manager = new BlexManager();
        var store = new FailingStore();
        manager.Register(store);

        var run = store.Watch();
        store.CancelWatch();
        await run;

        var span = recorder.Spans.Single(s => s.DisplayName == "failing/Watch");
        Assert.AreEqual(true, span.GetTagItem("blex.cancelled"));
        Assert.AreNotEqual(ActivityStatusCode.Error, span.Status);
        Assert.IsEmpty(recorder.Of("blex.action.errors", "failing"));
    }

    [TestMethod]
    public async Task An_effects_retries_are_events_on_its_span()
    {
        using var recorder = new Recorder();
        var manager = new BlexManager();
        var store = new RetryStore();
        manager.Register(store);

        await store.Exhaust();

        var span = recorder.Spans.Single(s => s.DisplayName == "retry/Exhaust");
        var retries = span.Events.Where(e => e.Name == "blex.effect.retry").ToList();
        Assert.HasCount(2, retries);
        Assert.AreEqual(1, retries[0].Tags.Single(t => t.Key == "blex.attempt").Value);
    }

    [TestMethod]
    public void A_scoped_store_is_one_metric_series_and_its_key_is_on_the_span()
    {
        using var recorder = new Recorder();
        var manager = new BlexManager();
        using var a = manager.CreateScope(new LooseLedgerStore(), "a");
        using var b = manager.CreateScope(new LooseLedgerStore(), "b");

        a.Store.Withdraw(1);
        b.Store.Withdraw(1);

        Assert.HasCount(2, recorder.Of("blex.actions", "loose"), "the declared name, not \"loose:a\" and \"loose:b\"");
        Assert.AreEqual("a", recorder.Spans.First(s => s.DisplayName == "loose/Withdraw").GetTagItem("blex.store.instance"));
    }

    [TestMethod]
    public async Task A_query_fetch_is_a_span_named_by_its_resource()
    {
        using var recorder = new Recorder();
        using var client = new BlexQueryClient(new BlexManager(), new BlexQueryClientOptions { RetryCount = 0 });

        await client.FetchAsync<int>(["diag-orders", 42], _ => Task.FromResult(1));
        await Assert.ThrowsExactlyAsync<InvalidOperationException>(() => client.FetchAsync<int>(["diag-orders", 43], _ => throw new InvalidOperationException()));

        var spans = recorder.Spans.Where(s => s.DisplayName == "query diag-orders").ToList();
        Assert.HasCount(2, spans);
        Assert.IsTrue(spans.All(s => !s.TagObjects.Any(t => Equals(t.Value, 42))), "ids in the key are never recorded");

        List<(string Instrument, double Value, Dictionary<string, object?> Tags)> fetches;
        lock (recorder.Measurements)
            fetches = [.. recorder.Measurements.Where(m => m.Instrument == "blex.query.fetches" && Equals(m.Tags["blex.query.resource"], "diag-orders"))];

        CollectionAssert.AreEquivalent(new[] { "success", "error" }, fetches.Select(f => (string)f.Tags["blex.query.outcome"]!).ToArray());
    }
}

/// <summary><see cref="BlexOptions.UseLogging"/>: actions and failures through the host's ILogger.</summary>
[TestClass]
public class LoggingTests
{
    private sealed class CapturingProvider : ILoggerProvider
    {
        public List<(string Category, LogLevel Level, EventId Id, string Message, Exception? Exception)> Entries { get; } = [];

        public ILogger CreateLogger(string categoryName) => new Logger(this, categoryName);

        public void Dispose()
        {
        }

        private sealed class Logger(CapturingProvider provider, string category) : ILogger
        {
            public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

            public bool IsEnabled(LogLevel logLevel) => true;

            public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
                => provider.Entries.Add((category, logLevel, eventId, formatter(state, exception), exception));
        }
    }

    private static (CounterStore Counter, FailingStore Failing, CapturingProvider Logs, List<BlexError> Errors) Setup(LogLevel actionLevel = LogLevel.Information)
    {
        var provider = new CapturingProvider();
        var errors = new List<BlexError>();
        var services = new ServiceCollection();
        services.AddLogging(logging => logging.AddProvider(provider).SetMinimumLevel(LogLevel.Trace));
        services.AddBitBlexServices(options =>
        {
            options.UseLogging(actionLevel);
            options.OnError = errors.Add;
        }, ServiceLifetime.Singleton);
        services.AddBitBlexStore<CounterStore>(ServiceLifetime.Singleton);
        services.AddBitBlexStore<FailingStore>(ServiceLifetime.Singleton);
        var sp = services.BuildServiceProvider();
        return (sp.GetRequiredService<CounterStore>(), sp.GetRequiredService<FailingStore>(), provider, errors);
    }

    [TestMethod]
    public void Actions_are_logged_by_name_at_the_chosen_level()
    {
        var (counter, _, logs, _) = Setup();

        counter.Add(41);

        var entry = logs.Entries.Single(e => e.Id.Name == "BlexAction");
        Assert.AreEqual("Bit.Blex", entry.Category);
        Assert.AreEqual(LogLevel.Information, entry.Level);
        Assert.AreEqual("Blex action counter/Add (#1)", entry.Message);
        Assert.DoesNotContain("41", entry.Message, "arguments are never logged");
    }

    [TestMethod]
    public void A_failing_action_is_logged_as_an_error_with_its_exception()
    {
        var (_, failing, logs, _) = Setup();

        Assert.ThrowsExactly<InvalidOperationException>(() => failing.Boom());

        var entry = logs.Entries.Single(e => e.Id.Name == "BlexActionFailed");
        Assert.AreEqual(LogLevel.Error, entry.Level);
        Assert.AreEqual("sync-boom", entry.Exception?.Message);
    }

    [TestMethod]
    public void An_isolated_failure_is_logged_and_still_reaches_OnError()
    {
        var (counter, _, logs, errors) = Setup();
        counter.StateChanged += () => throw new InvalidOperationException("bad subscriber");

        counter.Increment();

        var entry = logs.Entries.Single(e => e.Id.Name == "BlexIsolatedFailure");
        Assert.AreEqual(LogLevel.Warning, entry.Level);
        Assert.AreEqual("bad subscriber", entry.Exception?.Message);
        Assert.ContainsSingle(errors);
    }

    [TestMethod]
    public void Level_None_logs_failures_only()
    {
        var (counter, _, logs, _) = Setup(LogLevel.None);

        counter.Increment();

        Assert.IsFalse(logs.Entries.Any(e => e.Id.Name == "BlexAction"));
    }

    [TestMethod]
    public void Without_a_logger_factory_nothing_breaks()
    {
        var services = new ServiceCollection();
        services.AddBitBlexServices(options => options.UseLogging(), ServiceLifetime.Singleton);
        services.AddBitBlexStore<CounterStore>(ServiceLifetime.Singleton);
        using var sp = services.BuildServiceProvider();

        sp.GetRequiredService<CounterStore>().Increment();

        Assert.AreEqual(1, sp.GetRequiredService<CounterStore>().Count);
    }
}
