using System.Collections;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;

namespace Bit.BevTools.Tests;

/// <summary>
/// The calls page script makes to .NET, as the page hub reports them: named by what they call, one timeline event per
/// name per second, and a per-frame loop told apart from a burst.
/// </summary>
[TestClass]
public sealed class JsToDotNetTests : IDisposable
{
    private readonly BevToolsSession _session;
    private readonly IServiceScope _scope;
    private readonly BevJsBridge _bridge;

    public JsToDotNetTests()
    {
        (_session, _scope) = TestHelpers.CreateSession(services: s => s.AddSingleton<IJSRuntime>(new TrackingTestJSRuntime()));
        _bridge = new BevJsBridge(_session, () => { });
    }

    public void Dispose() => _scope.Dispose();

    /// <summary>Hands JavaScript a reference to <paramref name="target"/>, as a library does, and returns the id JS calls it by.</summary>
    private long HandToJavaScript(object target)
    {
        try
        {
            // Tracked when the arguments are serialized; the test runtime never answers, so nothing is awaited.
            _ = _session.Services.GetRequiredService<IJSRuntime>().InvokeVoidAsync("observe", DotNetObjectReference.Create(target));
        }
        catch (JSException)
        {
        }
        foreach (DictionaryEntry entry in BevJSObjectReferences.Table(_session)!)
        {
            if (ReferenceEquals(BevJSObjectReferences.Target(entry.Value), target))
            {
                return (long)entry.Key;
            }
        }

        throw new InvalidOperationException("the JS runtime did not track the reference");
    }

    /// <summary><paramref name="count"/> calls of <paramref name="identifier"/>, <paramref name="intervalMs"/> apart, within one second two seconds ago.</summary>
    private static BevJsCallEntry[] Calls(string identifier, int count, int intervalMs, double durationMs = 0.5)
    {
        var now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        var second = (now / 1000 - 2) * 1000;
        return Enumerable.Range(0, count).Select(i => new BevJsCallEntry
        {
            Identifier = identifier,
            At = second + i * intervalMs,
            AgeMs = now - (second + i * intervalMs),
            DurationMs = durationMs,
        }).ToArray();
    }

    private List<BevToolsEvent> Events(string title) => _session.Timeline.Snapshot().Where(e => e.Category == "js-interop" && e.Title == title).ToList();

    [TestMethod]
    public void A_per_frame_loop_on_a_library_object_is_named_by_its_type_and_reported_as_an_animation_loop()
    {
        var id = HandToJavaScript(new FrameLoop());

        _bridge.ReportJsToDotNet(Calls("object#" + id + ".ComputeFrame", 60, 16));

        var records = _session.Interop.Snapshot().Where(r => r.Direction == BevJsInteropDirection.JsToDotNet).ToList();
        Assert.HasCount(60, records);
        Assert.IsTrue(records.All(r => r.Identifier == "FrameLoop.ComputeFrame"), records[0].Identifier);

        var evt = Assert.ContainsSingle(Events("← .NET FrameLoop.ComputeFrame"), "one event for the second, not sixty");
        Assert.IsTrue(evt.TryGetData("calls", out var calls) && calls is 60);
        StringAssert.StartsWith(evt.Detail, "60 calls in this second, on a FrameLoop (object #" + id + ")");
        Assert.IsTrue(records.All(r => r.TimelineEventId == evt.Id));

        _session.Diagnostics.Evaluate(force: true);
        var finding = Assert.ContainsSingle(f => f.Diagnostic.RuleId == "jsinterop.excessive", _session.Diagnostics.Findings).Diagnostic;
        Assert.AreEqual("← .NET FrameLoop.ComputeFrame runs every animation frame (60 calls in 1 s)", finding.Title);
        Assert.AreEqual(BevDiagnosticSeverity.Info, finding.Severity, "30 ms of calls in a second is what a running animation costs");
        StringAssert.Contains(finding.Evidence, "Called on a FrameLoop, which is not a component.");
        StringAssert.Contains(finding.Evidence, "every 16 ms");
        StringAssert.Contains(finding.Evidence, "The calls took 30 ms of that second");
    }

    [TestMethod]
    public void A_per_frame_loop_whose_calls_take_a_quarter_of_each_second_is_a_warning()
    {
        var id = HandToJavaScript(new FrameLoop());

        _bridge.ReportJsToDotNet(Calls("object#" + id + ".ComputeFrame", 60, 16, durationMs: 6));

        _session.Diagnostics.Evaluate(force: true);
        var finding = Assert.ContainsSingle(f => f.Diagnostic.RuleId == "jsinterop.excessive", _session.Diagnostics.Findings).Diagnostic;
        Assert.AreEqual(BevDiagnosticSeverity.Warning, finding.Severity);
    }

    [TestMethod]
    public void A_burst_that_is_not_frame_paced_is_excessive_interop()
    {
        _bridge.ReportJsToDotNet(Calls("App.OnScroll", 60, 0));

        _session.Diagnostics.Evaluate(force: true);
        var finding = Assert.ContainsSingle(f => f.Diagnostic.RuleId == "jsinterop.excessive", _session.Diagnostics.Findings).Diagnostic;
        Assert.AreEqual("← .NET App.OnScroll called 60 times in 1 s", finding.Title);
        Assert.AreEqual(BevDiagnosticSeverity.Warning, finding.Severity);
        StringAssert.StartsWith(finding.Evidence, "Origin not attributable to a component. The calls took 30 ms");
    }

    [TestMethod]
    public void A_failed_or_slow_call_keeps_an_event_of_its_own()
    {
        var entries = Calls("App.Save", 5, 10);
        entries[3].Error = "boom";
        entries[4].DurationMs = 400;

        _bridge.ReportJsToDotNet(entries);

        var events = Events("← .NET App.Save");
        Assert.HasCount(3, events);
        Assert.ContainsSingle(e => e.TryGetData("calls", out var calls) && calls is 3, events);
        StringAssert.EndsWith(Assert.ContainsSingle(e => e.Severity == BevToolsSeverity.Error, events).Detail, "failed: boom");
        Assert.ContainsSingle(e => e.DurationMs == 400, events);

        _session.Diagnostics.Evaluate(force: true);
        Assert.ContainsSingle(f => f.Diagnostic.RuleId == "jsinterop.slow", _session.Diagnostics.Findings);
    }

    [TestMethod]
    public void An_id_the_JS_runtime_no_longer_has_is_kept()
    {
        _bridge.ReportJsToDotNet(Calls("object#424242.OnTick", 1, 0));

        Assert.AreEqual("object#424242.OnTick", Assert.ContainsSingle(_session.Interop.Snapshot()).Identifier);
    }

    [TestMethod]
    public void Calls_are_dated_by_their_age_not_by_the_report()
    {
        var before = DateTimeOffset.UtcNow;
        _bridge.ReportJsToDotNet(Calls("App.Tick", 2, 500));

        var records = _session.Interop.Snapshot();
        Assert.IsTrue(records[0].At <= before.AddSeconds(-1), $"{records[0].At:O} is at least a second before {before:O}");
        Assert.IsTrue(records[0].StartTicks < records[1].StartTicks, "in the order they were made");
    }
}

public sealed class FrameLoop
{
    [JSInvokable]
    public void ComputeFrame()
    {
    }
}
