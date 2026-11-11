using System.Diagnostics;
using System.Diagnostics.Metrics;
using Microsoft.Extensions.DependencyInjection;

namespace Bit.Balette.Tests;

/// <summary>
/// What the palette reports through <c>System.Diagnostics</c> (<see cref="BaletteDiagnostics"/>): counts and durations of
/// commands by outcome, source and search durations, and activities - with no query, prompt value or title in any of it.
/// </summary>
[TestClass]
[DoNotParallelize]
public sealed class BaletteDiagnosticsTests : BunitContext
{
    private readonly IBaletteService _service;
    private readonly BaletteOptions _options;
    private readonly MeterListener _meters = new();
    private readonly ActivityListener _activities;
    private readonly List<(string Instrument, double Value, Dictionary<string, object?> Tags)> _measurements = [];
    private readonly List<Activity> _stopped = [];

    public BaletteDiagnosticsTests()
    {
        Services.AddBitBaletteServices(o => o.DebounceMs = 0);
        Services.AddBitBaletteInMemoryHistoryStore();
        JSInterop.Mode = JSRuntimeMode.Loose;
        var module = JSInterop.SetupModule("./_content/Bit.Balette/bit-balette.js");
        module.SetupModule("init", _ => true).Setup<bool>("isMac").SetResult(false);
        _service = Services.GetRequiredService<IBaletteService>();
        _options = Services.GetRequiredService<BaletteOptions>();

        _meters.InstrumentPublished = (instrument, listener) =>
        {
            if (instrument.Meter.Name == BaletteDiagnostics.MeterName)
            {
                listener.EnableMeasurementEvents(instrument);
            }
        };
        _meters.SetMeasurementEventCallback<long>((i, v, tags, _) => Record(i, v, tags));
        _meters.SetMeasurementEventCallback<double>((i, v, tags, _) => Record(i, v, tags));
        _meters.Start();

        _activities = new ActivityListener
        {
            ShouldListenTo = s => s.Name == BaletteDiagnostics.ActivitySourceName,
            Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllDataAndRecorded,
            ActivityStopped = a => { lock (_stopped) { _stopped.Add(a); } },
        };
        ActivitySource.AddActivityListener(_activities);
    }

    private void Record(Instrument instrument, double value, ReadOnlySpan<KeyValuePair<string, object?>> tags)
    {
        var copy = new Dictionary<string, object?>();
        foreach (var tag in tags)
        {
            copy[tag.Key] = tag.Value;
        }

        lock (_measurements)
        {
            _measurements.Add((instrument.Name, value, copy));
        }
    }

    private List<Dictionary<string, object?>> Tags(string instrument)
    {
        lock (_measurements)
        {
            return _measurements.Where(m => m.Instrument == instrument).Select(m => m.Tags).ToList();
        }
    }

    protected override void Dispose(bool disposing)
    {
        _meters.Dispose();
        _activities.Dispose();
        base.Dispose(disposing);
    }

    private async Task<IRenderedComponent<Balette>> OpenAsync(string? query = null)
    {
        var cut = Render<Balette>();
        await cut.InvokeAsync(() => _service.OpenAsync(query));
        return cut;
    }

    private static Task Press(IRenderedComponent<Balette> cut, string key)
        => cut.InvokeAsync(() => cut.Instance.OnKeyDown(key, false, false, false, false));

    [TestMethod]
    public async Task Each_outcome_is_counted_with_its_kind_and_trigger()
    {
        _service.Register(
            BaletteCmd.New("Probe ok").Id("probe-ok").Shortcut("Alt+O").Handler(() => { }),
            BaletteCmd.New("Probe boom").Id("probe-boom").Handler(() => throw new InvalidOperationException("boom")),
            BaletteCmd.New("Probe stop").Id("probe-stop").Handler(_ => Task.FromCanceled<BaletteCommandResult>(new CancellationToken(true))),
            BaletteCmd.New("Probe no").Id("probe-no").Kind(BaletteCommandKind.Setting).Guard(_ => false).Handler(() => { }));
        var cut = Render<Balette>();

        await cut.InvokeAsync(() => cut.Instance.OnGlobalShortcut("alt+o"));
        await cut.InvokeAsync(() => _service.OpenAsync("probe boom"));
        await Press(cut, "Enter");
        await cut.InvokeAsync(() => _service.ExecuteAsync("probe-stop"));
        await cut.InvokeAsync(() => _service.ExecuteAsync("probe-no"));

        var counted = Tags("balette.command.executions")
            .Select(t => $"{t["balette.command.kind"]}/{t["balette.trigger"]}/{t["balette.outcome"]}")
            .ToList();
        Assert.AreSequenceEqual(new[] { "action/shortcut/succeeded", "action/palette/failed", "action/programmatic/cancelled", "setting/programmatic/refused" }, counted);

        // Every run has a duration, a refusal - which ran nothing - has none.
        Assert.HasCount(3, Tags("balette.command.duration"));
        Assert.IsTrue(Tags("balette.command.executions").All(t => !t.ContainsKey("balette.command.id")));
    }

    [TestMethod]
    public async Task A_handler_runs_inside_its_activity_and_a_failure_marks_it()
    {
        Activity? seenInside = null;
        _service.Register(
            BaletteCmd.New("Probe trace").Id("probe-trace").Handler(_ => { seenInside = Activity.Current; return Task.FromResult(BaletteCommandResult.Close); }),
            BaletteCmd.New("Probe fail").Id("probe-fail").Handler(() => throw new InvalidOperationException("secret detail")));
        var cut = await OpenAsync("probe trace");

        await Press(cut, "Enter");
        await cut.InvokeAsync(() => _service.OpenAsync("probe fail"));
        await Press(cut, "Enter");

        var traced = _stopped.Single(a => (string?)a.GetTagItem("balette.command.id") == "probe-trace");
        Assert.AreEqual("balette.command", traced.OperationName);
        Assert.AreSame(traced, seenInside);
        Assert.AreEqual("succeeded", traced.GetTagItem("balette.outcome"));
        Assert.AreEqual("palette", traced.GetTagItem("balette.trigger"));

        var failed = _stopped.Single(a => (string?)a.GetTagItem("balette.command.id") == "probe-fail");
        Assert.AreEqual(ActivityStatusCode.Error, failed.Status);
        Assert.AreEqual("failed", failed.GetTagItem("balette.outcome"));
        // The type, never the message ("secret detail").
        Assert.AreEqual(typeof(InvalidOperationException).FullName, failed.GetTagItem("exception.type"));
        Assert.IsEmpty(failed.Events);

        // Neither the query nor a title is recorded.
        foreach (var activity in _stopped)
        {
            Assert.DoesNotContain(t => t.Value is string s && s.Contains("probe ", StringComparison.OrdinalIgnoreCase), activity.TagObjects);
            Assert.DoesNotContain(t => t.Value is string s && s.Contains("Probe", StringComparison.Ordinal), activity.TagObjects);
        }
    }

    [TestMethod]
    public async Task Command_ids_stay_out_of_traces_when_asked()
    {
        _options.RecordCommandIdsInTraces = false;
        _service.Register(BaletteCmd.New("Probe private").Id("person:ada@example.com").Handler(() => { }));
        var cut = Render<Balette>();

        await cut.InvokeAsync(() => _service.ExecuteAsync("person:ada@example.com"));

        var activity = Assert.ContainsSingle(_stopped);
        Assert.IsNull(activity.GetTagItem("balette.command.id"));
        Assert.AreEqual("programmatic", activity.GetTagItem("balette.trigger"));
    }

    [TestMethod]
    public async Task Sources_and_searches_report_their_durations()
    {
        _options.SourceTimeoutMs = 200;
        _service.RegisterSource(new BaletteDelegateSource("Probe people", (q, ct) => Task.FromResult<IReadOnlyList<BaletteCommand>>([BaletteCmd.New("Ada").Build()])));
        _service.RegisterSource(new BaletteDelegateSource("Probe broken", (q, ct) => throw new HttpRequestException("503")));
        _service.RegisterSource(new BaletteDelegateSource("Probe slow", async (q, ct) => { await Task.Delay(Timeout.Infinite, CancellationToken.None).WaitAsync(TimeSpan.FromSeconds(30), TestContext.CancellationToken); return []; }));
        var cut = await OpenAsync();

        await cut.InvokeAsync(() => cut.Instance.SetQueryAsync("ada"));

        cut.WaitForAssertion(() =>
        {
            var outcomes = Tags("balette.source.duration")
                .Select(t => $"{t["balette.source.name"]}/{t["balette.outcome"]}")
                .Order()
                .ToList();
            Assert.AreSequenceEqual(new[] { "Probe broken/failed", "Probe people/succeeded", "Probe slow/timed_out" }, outcomes);
        }, TimeSpan.FromSeconds(5));
        Assert.Contains(a => a.OperationName == "balette.source" && (string?)a.GetTagItem("balette.source.name") == "Probe people", _stopped);
        Assert.IsNotEmpty(Tags("balette.search.duration"));
    }

    public global::Microsoft.VisualStudio.TestTools.UnitTesting.TestContext TestContext { get; set; }
}
