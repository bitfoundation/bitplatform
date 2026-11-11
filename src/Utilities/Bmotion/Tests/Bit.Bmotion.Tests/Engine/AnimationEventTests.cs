using System.Collections.Concurrent;
using System.Diagnostics.Metrics;
using Bit.Bmotion.Tests.TestInfra;

namespace Bit.Bmotion.Tests.Engine;

/// <summary>
/// The engine reports how each part of an animation plays (<see cref="BmotionAnimationEngine.AnimationEvent"/> and the
/// <c>Bit.Bmotion</c> meter): the strategy the start path chose, why the frame loop (or its absence) got a part, and how
/// each part ended - with property names only, never values.
/// </summary>
[TestClass]
public class AnimationEventTests
{
    private static (BmotionAnimationEngine Engine, FakeBmotionInterop Interop, ConcurrentQueue<BmotionAnimationEvent> Events) NewEngine(bool inProcess = true)
    {
        var interop = new FakeBmotionInterop { IsInProcess = inProcess, SupportsLinearEasing = true };
        var engine = new BmotionAnimationEngine(interop);
        var events = new ConcurrentQueue<BmotionAnimationEvent>();
        engine.AnimationEvent += events.Enqueue;
        return (engine, interop, events);
    }

    private static Dictionary<string, object?> Values(params (string Key, object Value)[] values)
        => values.ToDictionary(v => v.Key, v => (object?)v.Value);

    // An end is raised from the part's completion, which runs its continuations asynchronously.
    private static async Task<BmotionAnimationEvent> WaitForAsync(
        ConcurrentQueue<BmotionAnimationEvent> events, Func<BmotionAnimationEvent, bool> match)
    {
        for (int i = 0; i < 400; i++)
        {
            var hit = events.FirstOrDefault(match);
            if (hit is not null) return hit;
            await Task.Delay(5);
        }
        Assert.Fail($"No matching event; got: {string.Join("; ", events.Select(e => $"{e.Kind} {e.Strategy} [{string.Join(",", e.Properties)}]"))}");
        return null!;
    }

    private static void RunFrames(BmotionAnimationEngine engine, double untilMs)
    {
        for (double t = 0; t <= untilMs; t += 16) engine.ComputeFrame(t);
    }

    [TestMethod]
    public async Task ATweenTheBrowserCanPlay_StartsAsATimeline_AndFinishesWithIt()
    {
        var (engine, interop, events) = NewEngine();
        await using var _ = engine;
        engine.RegisterElement("el");

        await engine.AnimateToAsync("el", Values(("x", 100.0), ("opacity", 0.5)), Bm.Tween(0.3).ToConfig());

        var started = events.Single();
        Assert.AreEqual(BmAnimationEventKind.Started, started.Kind);
        Assert.AreEqual(BmPlaybackStrategy.BrowserTimeline, started.Strategy);
        Assert.AreEqual("el", started.ElementId);
        CollectionAssert.AreEquivalent(new[] { "x", "opacity" }, started.Properties.ToArray());
        Assert.IsNull(started.Reason, "the browser playing it needs no explanation");

        interop.FinishWaapi("el");
        var finished = await WaitForAsync(events, e => e.Kind == BmAnimationEventKind.Finished);
        Assert.AreEqual(BmPlaybackStrategy.BrowserTimeline, finished.Strategy);
        Assert.IsTrue(finished.ElapsedMs >= 0);
    }

    [TestMethod]
    public async Task KeyframesWithPerSegmentEasing_PlayAsARecording()
    {
        var (engine, _, events) = NewEngine();
        await using var __ = engine;
        engine.RegisterElement("el");

        await engine.AnimateToAsync("el", Values(("x", new[] { 0.0, 50, 100 })),
            Bm.Tween(0.4, eases: [BmEase.In, BmEase.Out]).ToConfig());

        Assert.AreEqual(BmPlaybackStrategy.BrowserRecorded, events.Single().Strategy);
    }

    [TestMethod]
    public async Task APerFrameCallback_RunsOnTheFrameLoop_AndSaysSo()
    {
        var (engine, _, events) = NewEngine();
        await using var __ = engine;
        engine.RegisterElement("el");
        var transition = Bm.Tween(0.1);
        transition.OnUpdate = _ => { };

        await engine.AnimateToAsync("el", Values(("x", 100.0)), transition.ToConfig());

        var started = events.Single();
        Assert.AreEqual(BmPlaybackStrategy.FrameLoop, started.Strategy);
        StringAssert.Contains(started.Reason, "OnUpdate");

        RunFrames(engine, 200);
        var finished = await WaitForAsync(events, e => e.Kind == BmAnimationEventKind.Finished);
        Assert.AreEqual(BmPlaybackStrategy.FrameLoop, finished.Strategy);
        Assert.AreEqual(started.Reason, finished.Reason);
    }

    [TestMethod]
    public async Task APathMorph_NamesTheAttributeAsTheReason()
    {
        var (engine, _, events) = NewEngine();
        await using var __ = engine;
        engine.RegisterElement("el", Values(("d", "M0 0 L10 10")));

        await engine.AnimateToAsync("el", Values(("d", "M0 0 L20 20")), Bm.Tween(0.3).ToConfig());

        var started = events.Single();
        Assert.AreEqual(BmPlaybackStrategy.FrameLoop, started.Strategy);
        StringAssert.Contains(started.Reason, "'d'");
    }

    [TestMethod]
    public async Task WithoutAFrameLoop_WhatNeedsOne_IsReportedAsLandingAtOnce()
    {
        var (engine, _, events) = NewEngine(inProcess: false);
        await using var __ = engine;
        engine.RegisterElement("el");
        var transition = Bm.Tween(0.3);
        transition.OnUpdate = _ => { };

        await engine.AnimateToAsync("el", Values(("x", 100.0)), transition.ToConfig());

        var started = events.First();
        Assert.AreEqual(BmPlaybackStrategy.InstantWithoutFrameLoop, started.Strategy);
        StringAssert.Contains(started.Reason, "OnUpdate");
    }

    [TestMethod]
    public async Task AZeroLengthTransition_IsInstant_AndFinishesAtOnce()
    {
        var (engine, _, events) = NewEngine();
        await using var __ = engine;
        engine.RegisterElement("el");

        await engine.AnimateToAsync("el", Values(("x", 100.0)), Bm.Tween(0).ToConfig());

        Assert.AreEqual(BmPlaybackStrategy.Instant, events.First().Strategy);
        var finished = await WaitForAsync(events, e => e.Kind == BmAnimationEventKind.Finished);
        Assert.AreEqual(BmPlaybackStrategy.Instant, finished.Strategy);
    }

    [TestMethod]
    public async Task APartlyBrowserPartlyLoopAnimation_ReportsEachPartOnItsOwn()
    {
        var (engine, interop, events) = NewEngine();
        await using var __ = engine;
        engine.RegisterElement("el", Values(("d", "M0 0 L10 10")));

        await engine.AnimateToAsync("el", Values(("opacity", 0.5), ("d", "M0 0 L20 20")), Bm.Tween(0.1).ToConfig());

        var starts = events.Where(e => e.Kind == BmAnimationEventKind.Started).ToArray();
        Assert.AreEqual(2, starts.Length);
        CollectionAssert.AreEqual(new[] { "opacity" }, starts.Single(e => e.Strategy == BmPlaybackStrategy.BrowserTimeline).Properties.ToArray());
        CollectionAssert.AreEqual(new[] { "d" }, starts.Single(e => e.Strategy == BmPlaybackStrategy.FrameLoop).Properties.ToArray());

        interop.FinishWaapi("el");
        RunFrames(engine, 200);
        await WaitForAsync(events, e => e.Kind == BmAnimationEventKind.Finished && e.Strategy == BmPlaybackStrategy.BrowserTimeline);
        await WaitForAsync(events, e => e.Kind == BmAnimationEventKind.Finished && e.Strategy == BmPlaybackStrategy.FrameLoop);
    }

    [TestMethod]
    public async Task ANewAnimationOfTheSameProperty_InterruptsTheOldOne()
    {
        var (engine, _, events) = NewEngine();
        await using var __ = engine;
        engine.RegisterElement("el");

        await engine.AnimateToAsync("el", Values(("x", 100.0)), Bm.Tween(1).ToConfig());
        await engine.AnimateToAsync("el", Values(("x", 0.0)), Bm.Tween(1).ToConfig());

        var interrupted = await WaitForAsync(events, e => e.Kind == BmAnimationEventKind.Interrupted);
        CollectionAssert.AreEqual(new[] { "x" }, interrupted.Properties.ToArray());
        Assert.AreEqual(2, events.Count(e => e.Kind == BmAnimationEventKind.Started));
    }

    [TestMethod]
    public async Task TheOldPartEnds_BeforeTheNewOneStarts()
    {
        // A tracker of "what runs on x" applies events in order: the new start must not be followed by the old end.
        var (engine, _, events) = NewEngine();
        await using var __ = engine;
        engine.RegisterElement("el");

        await engine.AnimateToAsync("el", Values(("x", 100.0)), Bm.Tween(1).ToConfig());
        await engine.AnimateToAsync("el", Values(("x", 0.0)), Bm.Tween(1).ToConfig());
        await Task.Delay(50); // the old part's completion settles late, asynchronously: it must not report again

        var kinds = events.Select(e => e.Kind).ToArray();
        CollectionAssert.AreEqual(
            new[] { BmAnimationEventKind.Started, BmAnimationEventKind.Interrupted, BmAnimationEventKind.Started }, kinds);
    }

    [TestMethod]
    public async Task APerPropertyCallbackOfAnotherKey_IsNotTheReason()
    {
        var (engine, _, events) = NewEngine();
        await using var __ = engine;
        engine.RegisterElement("el", Values(("d", "M0 0 L10 10")));
        var config = Bm.Tween(0.3).ToConfig();
        config.Properties = new() { ["opacity"] = new BmotionTransitionConfig { OnUpdate = _ => { } } };

        await engine.AnimateToAsync("el", Values(("d", "M0 0 L20 20")), config);

        var started = events.Single();
        Assert.AreEqual(BmPlaybackStrategy.FrameLoop, started.Strategy);
        StringAssert.Contains(started.Reason, "'d'");
    }

    [TestMethod]
    public async Task ABrowserAnimationThatNeverStarts_IsReportedAsReplayingOnTheFrameLoop()
    {
        var (engine, interop, events) = NewEngine();
        await using var __ = engine;
        interop.PlayWaapiHandler = (_, _, _, _) => Task.FromResult(false); // rejected at once: playback never started
        engine.RegisterElement("el");

        await engine.AnimateToAsync("el", Values(("x", 100.0)), Bm.Tween(0.1).ToConfig());

        var replay = await WaitForAsync(events, e => e.Kind == BmAnimationEventKind.Started && e.Strategy == BmPlaybackStrategy.FrameLoop);
        StringAssert.Contains(replay.Reason, "didn't start");
        var browserEnd = events.Single(e => e.Strategy == BmPlaybackStrategy.BrowserTimeline && e.Kind != BmAnimationEventKind.Started);
        Assert.AreEqual(BmAnimationEventKind.Interrupted, browserEnd.Kind);

        RunFrames(engine, 300);
        var finished = await WaitForAsync(events, e => e.Kind == BmAnimationEventKind.Finished);
        Assert.AreEqual(BmPlaybackStrategy.FrameLoop, finished.Strategy, "the finish is the frame loop's, not the browser's");
    }

    [TestMethod]
    public async Task UnregisteringTheElement_EndsItsRunningParts()
    {
        var (engine, _, events) = NewEngine();
        await using var __ = engine;
        engine.RegisterElement("el");

        await engine.AnimateToAsync("el", Values(("x", 100.0)), Bm.Tween(1).ToConfig());
        engine.UnregisterElement("el");

        var interrupted = events.Single(e => e.Kind == BmAnimationEventKind.Interrupted);
        CollectionAssert.AreEqual(new[] { "x" }, interrupted.Properties.ToArray());
    }

    [TestMethod]
    public async Task InstantParts_AreNotRecordedInTheDurationHistogram()
    {
        var durations = new ConcurrentBag<string>();
        using var listener = new MeterListener();
        listener.InstrumentPublished = (instrument, l) =>
        {
            if (instrument.Meter.Name == BmotionAnimationEngine.MeterName && instrument.Name == "bmotion.animation.duration")
                l.EnableMeasurementEvents(instrument);
        };
        listener.SetMeasurementEventCallback<double>((_, _, tags, _) =>
        {
            foreach (var tag in tags)
                if (tag.Key == "bmotion.strategy") durations.Add((string)tag.Value!);
        });
        listener.Start();

        var interop = new FakeBmotionInterop { SupportsLinearEasing = true };
        await using var engine = new BmotionAnimationEngine(interop);
        engine.RegisterElement("el");
        await engine.AnimateToAsync("el", Values(("x", 100.0)), Bm.Tween(0).ToConfig());
        await engine.AnimateToAsync("el", Values(("opacity", 0.5)), Bm.Tween(0.3).ToConfig());
        interop.FinishWaapi("el");

        for (int i = 0; i < 400 && !durations.Contains("browser_timeline"); i++) await Task.Delay(5);
        Assert.IsTrue(durations.Contains("browser_timeline"));
        Assert.IsFalse(durations.Contains("instant"));
    }

    [TestMethod]
    public async Task AStop_IsAnInterruption()
    {
        var (engine, _, events) = NewEngine();
        await using var __ = engine;
        engine.RegisterElement("el");
        var transition = Bm.Tween(1);
        transition.OnUpdate = _ => { };

        await engine.AnimateToAsync("el", Values(("x", 100.0)), transition.ToConfig());
        engine.ComputeFrame(0);
        engine.Stop("el", null);
        engine.ComputeFrame(16);

        await WaitForAsync(events, e => e.Kind == BmAnimationEventKind.Interrupted && e.Strategy == BmPlaybackStrategy.FrameLoop);
    }

    [TestMethod]
    public async Task AThrowingHandler_DoesNotStopTheAnimationOrTheOtherHandlers()
    {
        var (engine, interop, events) = NewEngine();
        await using var __ = engine;
        engine.AnimationEvent += _ => throw new InvalidOperationException("boom");
        var after = new List<BmotionAnimationEvent>();
        engine.AnimationEvent += after.Add;
        engine.RegisterElement("el");

        await engine.AnimateToAsync("el", Values(("x", 100.0)), Bm.Tween(0.3).ToConfig());

        Assert.AreEqual(1, interop.CountOf("playWaapiAnimation"), "the animation still started");
        Assert.AreEqual(1, events.Count);
        Assert.AreEqual(1, after.Count, "the handler after the throwing one still ran");
    }

    [TestMethod]
    public async Task WithNobodyListening_NothingChanges()
    {
        var interop = new FakeBmotionInterop { SupportsLinearEasing = true };
        await using var engine = new BmotionAnimationEngine(interop);
        engine.RegisterElement("el");

        await engine.AnimateToAsync("el", Values(("x", 100.0)), Bm.Tween(0.3).ToConfig());

        Assert.AreEqual(1, interop.CountOf("playWaapiAnimation"));
    }

    [TestMethod]
    public async Task TheMeter_CountsStartsAndEndsByStrategyAndOutcome()
    {
        var started = new ConcurrentBag<string>();
        var ended = new ConcurrentBag<(string Strategy, string Outcome)>();
        using var listener = new MeterListener();
        listener.InstrumentPublished = (instrument, l) =>
        {
            if (instrument.Meter.Name == BmotionAnimationEngine.MeterName) l.EnableMeasurementEvents(instrument);
        };
        listener.SetMeasurementEventCallback<long>((instrument, _, tags, _) =>
        {
            string? strategy = null, outcome = null;
            foreach (var tag in tags)
            {
                if (tag.Key == "bmotion.strategy") strategy = (string?)tag.Value;
                if (tag.Key == "bmotion.outcome") outcome = (string?)tag.Value;
            }
            if (instrument.Name == "bmotion.animation.started") started.Add(strategy!);
            if (instrument.Name == "bmotion.animation.ended") ended.Add((strategy!, outcome!));
        });
        listener.Start();

        var interop = new FakeBmotionInterop { SupportsLinearEasing = true };
        await using var engine = new BmotionAnimationEngine(interop); // no event handler: the meter alone observes
        engine.RegisterElement("el");
        await engine.AnimateToAsync("el", Values(("x", 100.0)), Bm.Tween(0.3).ToConfig());
        interop.FinishWaapi("el");

        for (int i = 0; i < 400 && !ended.Contains(("browser_timeline", "finished")); i++) await Task.Delay(5);
        Assert.IsTrue(started.Contains("browser_timeline"));
        Assert.IsTrue(ended.Contains(("browser_timeline", "finished")));
    }
}
