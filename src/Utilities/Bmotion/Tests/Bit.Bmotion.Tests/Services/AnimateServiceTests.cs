using Bit.Bmotion.Tests.TestInfra;

namespace Bit.Bmotion.Tests.Services;

/// <summary>
/// Tests for <see cref="BmotionAnimateService"/> (motion.dev's <c>animate()</c>) and the
/// <see cref="BmAnimationControls"/> it returns: element targets by selector (with stagger), motion
/// values, raw numbers, spring followers and <see cref="BmSequence"/> timelines - with the engine
/// ticked by hand and compositor animations finished on demand.
/// </summary>
[TestClass]
public class AnimateServiceTests
{
    private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(5);

    private static (BmotionAnimateService Service, BmotionAnimationEngine Engine, FakeBmotionInterop Interop) Create(bool inProcess = true)
    {
        var interop = new FakeBmotionInterop { IsInProcess = inProcess };
        var engine = new BmotionAnimationEngine(interop);
        return (new BmotionAnimateService(engine, interop), engine, interop);
    }

    private static void RunFrames(BmotionAnimationEngine engine, double fromMs, double toMs, double stepMs = 16)
    {
        for (double t = fromMs; t <= toMs; t += stepMs) engine.ComputeFrame(t);
    }

    private static Dictionary<string, object?> Timing(object?[] playArgs) => (Dictionary<string, object?>)playArgs[3]!;

    // ── By selector ───────────────────────────────────────────────────────────

    [TestMethod]
    public async Task BySelector_AnimatesEveryMatch_WithTheStaggerAppliedInDocumentOrder()
    {
        var (service, engine, interop) = Create();
        await using var _ = engine;
        interop.SelectorResolver = _ => ["li-0", "li-1", "li-2"];

        var controls = await service.AnimateAsync(".item", Bm.To(opacity: 0.5), Bm.Tween(0.4), Bm.Stagger(0.1));

        var plays = interop.ArgsOf("playWaapiAnimation");
        CollectionAssert.AreEqual(new[] { "li-0", "li-1", "li-2" }, plays.Select(p => (string)p[0]!).ToArray());
        CollectionAssert.AreEqual(new[] { 0.0, 100.0, 200.0 }, plays.Select(p => Math.Round((double)Timing(p)["delay"]!, 6)).ToArray());
        Assert.IsTrue(plays.All(p => (double)Timing(p)["duration"]! == 400));
        Assert.IsFalse(controls.WhenCompleteAsync().IsCompleted, "nothing has finished yet");

        interop.FinishWaapi();

        await controls.WhenCompleteAsync().WaitAsync(Timeout);
    }

    [TestMethod]
    public async Task BySelector_ATransitionEmbeddedInTheTarget_WinsOverTheArgument()
    {
        var (service, engine, interop) = Create();
        await using var _ = engine;

        await service.AnimateAsync("#box", Bm.To(opacity: 0.5, transition: Bm.Tween(0.2)), Bm.Tween(1));

        Assert.AreEqual(200.0, (double)Timing(interop.ArgsOf("playWaapiAnimation").Single())["duration"]!);
    }

    [TestMethod]
    public async Task BySelector_Stop_FreezesReleasesAndSettlesTheControls()
    {
        var (service, engine, interop) = Create();
        await using var _ = engine;
        interop.SelectorResolver = _ => ["a", "b"];

        var controls = await service.AnimateAsync(".item", Bm.To(opacity: 0.5), Bm.Tween(1));
        Assert.IsTrue(engine.IsRegistered("a") && engine.IsRegistered("b"));

        controls.Stop();

        // Frozen in place: the browser animations are committed and cancelled...
        Assert.IsTrue(interop.ArgsOf("cancelWaapiAnimation").All(a => (bool)a[2]!), "Stop must commit the current values");
        CollectionAssert.AreEquivalent(new[] { "a", "b" }, interop.ArgsOf("cancelWaapiAnimation").Select(a => (string)a[0]!).ToArray());
        // ...the targets are released, and an awaiting caller is let go.
        Assert.IsFalse(engine.IsRegistered("a") || engine.IsRegistered("b"));
        await controls.WhenCompleteAsync().WaitAsync(Timeout);
    }

    [TestMethod]
    public async Task BySelector_APreCancelledToken_NeverTouchesTheDom()
    {
        var (service, engine, interop) = Create();
        await using var _ = engine;

        var controls = await service.AnimateAsync("#box", Bm.To(opacity: 0.5), cancellationToken: new CancellationToken(canceled: true));

        Assert.AreEqual(0, interop.Calls.Count);
        Assert.IsTrue(controls.WhenCompleteAsync().IsCompleted);
    }

    [TestMethod]
    public async Task BySelector_CancellingTheTokenMidFlight_StopsTheAnimation()
    {
        var (service, engine, interop) = Create();
        await using var _ = engine;
        using var cts = new CancellationTokenSource();

        var controls = await service.AnimateAsync("#box", Bm.To(opacity: 0.5), Bm.Tween(1), cancellationToken: cts.Token);
        cts.Cancel();

        Assert.IsTrue(interop.WasCalled("cancelWaapiAnimation"));
        Assert.IsFalse(engine.IsRegistered("bm-sel-1"));
        await controls.WhenCompleteAsync().WaitAsync(Timeout);
    }

    [TestMethod]
    public async Task ByRef_AnimatesTheResolvedElement()
    {
        var (service, engine, interop) = Create();
        await using var _ = engine;
        interop.RefResolver = _ => "hero";

        await service.AnimateAsync(new Microsoft.AspNetCore.Components.ElementReference("ref-1"), Bm.To(scale: 1.5), Bm.Tween(0.3));

        Assert.AreEqual("hero", interop.ArgsOf("playWaapiAnimation").Single()[0]);
    }

    // ── Playback controls ─────────────────────────────────────────────────────

    // A color tween runs on the frame loop (it never offloads), so playback rate is observable.
    private static async Task<(BmotionAnimateService, BmotionAnimationEngine, BmAnimationControls)> StartColorTween(double seconds = 1)
    {
        var (service, engine, interop) = Create();
        // Playback control on the frame loop, ticked by hand (the fake browser has no Web Animations).
        interop.WebAnimationsSupported = false;
        interop.SelectorResolver = _ => ["el"];
        engine.RegisterElement("el", new Dictionary<string, object?> { ["backgroundColor"] = "#000000" });
        var controls = await service.AnimateAsync("#el", Bm.To(backgroundColor: "#ffffff"), Bm.Tween(seconds, ease: BmEase.Linear));
        return (service, engine, controls);
    }

    private static string ColorOf(BmotionAnimationEngine engine)
        => engine.GetDiagnostics().Single(d => d.Id == "el").StringValues["backgroundColor"];

    [TestMethod]
    public async Task Controls_PauseHoldsInPlace_AndPlayResumesFromThere()
    {
        var (_, engine, controls) = await StartColorTween();
        await using var __ = engine;

        RunFrames(engine, 0, 96);
        var atPause = ColorOf(engine);
        controls.Pause();
        RunFrames(engine, 112, 800);
        Assert.AreEqual(atPause, ColorOf(engine), "a paused animation must not advance");

        controls.Play();
        RunFrames(engine, 816, 900);
        Assert.AreNotEqual(atPause, ColorOf(engine), "playing again resumes");
        Assert.IsFalse(controls.WhenCompleteAsync().IsCompleted, "the paused time doesn't count towards the duration");
    }

    [TestMethod]
    public async Task Controls_SetSpeed_ScalesTheRemainingTime()
    {
        var (_, engine, controls) = await StartColorTween();
        await using var __ = engine;

        engine.ComputeFrame(0);
        controls.SetSpeed(2);
        // 512ms of real time at double speed covers the whole one-second tween.
        RunFrames(engine, 16, 528);

        await controls.WhenCompleteAsync().WaitAsync(Timeout);
    }

    [TestMethod]
    public async Task Controls_Complete_SnapsToTheEndValue()
    {
        var (_, engine, controls) = await StartColorTween();
        await using var __ = engine;
        RunFrames(engine, 0, 100);

        controls.Complete();

        await controls.WhenCompleteAsync().WaitAsync(Timeout);
        // Snapped straight to the authored end value, and nothing is left running.
        Assert.AreEqual("#ffffff", ColorOf(engine));
        Assert.IsFalse(engine.GetDiagnostics().Single(d => d.Id == "el").HasActiveAnimations);
    }

    [TestMethod]
    public async Task Controls_Stop_FreezesTheValue_AndLaterSpeedChangesAreIgnored()
    {
        var (_, engine, controls) = await StartColorTween();
        await using var __ = engine;
        RunFrames(engine, 0, 300);

        controls.Stop();
        var frozen = ColorOf(engine);
        controls.SetSpeed(5); // no-op once settled
        RunFrames(engine, 316, 2000);

        Assert.AreEqual(frozen, ColorOf(engine));
        // Frozen mid-way: neither the start nor the end colour.
        Assert.AreNotEqual("rgba(255,255,255,1)", frozen);
        Assert.AreNotEqual("rgba(0,0,0,1)", frozen);
        await controls; // directly awaitable
    }

    // ── Motion values and raw numbers ─────────────────────────────────────────

    [TestMethod]
    public async Task ByValue_DrivesTheMotionValueToItsTarget_OnTheFrameLoop()
    {
        var (service, engine, _) = Create();
        await using var __ = engine;
        var value = Bm.Value(0.0);
        var seen = new List<double>();
        value.Subscribe(v => seen.Add(v));

        var animation = service.AnimateAsync(value, 100, Bm.Tween(0.2, ease: BmEase.Linear));
        RunFrames(engine, 0, 400);
        await animation.WaitAsync(Timeout);

        Assert.AreEqual(100, value.Value, 1e-9);
        Assert.IsTrue(seen.Count >= 5, $"expected per-frame updates, got {seen.Count}");
        for (int i = 1; i < seen.Count; i++)
            Assert.IsTrue(seen[i] >= seen[i - 1], "a linear tween towards a higher target never goes back");
    }

    [TestMethod]
    public async Task ByValue_WithoutAFrameLoop_SettlesInstantly()
    {
        var (service, engine, _) = Create(inProcess: false);
        await using var __ = engine;
        var value = Bm.Value(0.0);

        await service.AnimateAsync(value, 42, Bm.Tween(1)).WaitAsync(Timeout);

        Assert.AreEqual(42, value.Value);
    }

    [TestMethod]
    public async Task FromTo_ReportsTheStartValue_ThenEveryFrame_ThenTheEnd()
    {
        var (service, engine, _) = Create();
        await using var __ = engine;
        var updates = new List<double>();

        var animation = service.AnimateAsync(0, 100, v => updates.Add(v), Bm.Tween(0.1, ease: BmEase.Linear));
        Assert.AreEqual(0, updates[0], "the start value is reported synchronously");

        RunFrames(engine, 0, 200);
        await animation.WaitAsync(Timeout);

        Assert.AreEqual(100, updates[^1], 1e-9);
        Assert.IsTrue(updates.Any(v => v > 0 && v < 100), "intermediate values are reported");
    }

    [TestMethod]
    public async Task Spring_FollowsItsSource_UntilDisposed()
    {
        var (service, engine, _) = Create();
        await using var __ = engine;
        var source = Bm.Value(0.0);
        var follower = service.Spring(source, new BmSpring { Stiffness = 300, Damping = 30 });

        source.SetSync(100);
        Assert.AreEqual(0, follower.Value, "the follower moves on the frame loop, not instantly");
        RunFrames(engine, 0, 3000);
        Assert.AreEqual(100, follower.Value, 0.5);

        follower.Dispose();
        source.SetSync(200);
        RunFrames(engine, 3016, 5000);
        Assert.AreEqual(100, follower.Value, 0.5, "a disposed follower is detached from its source");
    }

    [TestMethod]
    public async Task Spring_KeepsUp_WithASourceThatChangesMoreOftenThanFrames()
    {
        // A source can change several times between two frames - a scroll position or a pointer the browser reports
        // faster than it draws. Each change has to move the follower's target, not restart its spring from a standstill:
        // restarted before it could ever take a step, the follower would sit still for as long as the source moved.
        var (service, engine, _) = Create();
        await using var __ = engine;
        var source = Bm.Value(0.0);
        var follower = service.Spring(source, new BmSpring { Stiffness = 300, Damping = 30 });

        for (int frame = 0; frame < 30; frame++)
        {
            for (int change = 0; change < 3; change++) source.SetSync(source.Value + 10);
            engine.ComputeFrame(frame * 16.0);
        }

        Assert.IsTrue(follower.Value > 300, $"the follower got to {follower.Value} while its source got to {source.Value}");
    }

    [TestMethod]
    public async Task Spring_Retargeted_StillSettlesExactlyOnTheLastTarget()
    {
        var (service, engine, _) = Create();
        await using var __ = engine;
        var source = Bm.Value(0.0);
        var follower = service.Spring(source, new BmSpring { Stiffness = 300, Damping = 30 });

        // A wide first move, then a short one: the second target, not the first one's scale, decides when it rests.
        source.SetSync(3000);
        RunFrames(engine, 0, 100);
        source.SetSync(1);
        RunFrames(engine, 116, 6000);

        Assert.AreEqual(1, follower.Value, 1e-9);
    }

    [TestMethod]
    public async Task Spring_RetargetedFromInsideTheApplyThatLandsIt_GoesOnToTheNewTarget()
    {
        // A subscriber of the follower can change its source the moment the follower lands: the spring that was about
        // to finish is pointed at the new target from inside that apply, and must not finish on the old one.
        var (service, engine, _) = Create();
        await using var __ = engine;
        var source = Bm.Value(0.0);
        var follower = service.Spring(source, new BmSpring { Stiffness = 300, Damping = 30 });
        follower.Subscribe(v => { if (v == 100) source.SetSync(200); });

        source.SetSync(100);
        RunFrames(engine, 0, 6000);

        Assert.AreEqual(200, follower.Value, 1e-6);
    }

    [TestMethod]
    public async Task Spring_StartsWithTheVelocityTheCallerSet()
    {
        var (service, engine, _) = Create();
        await using var __ = engine;
        var source = Bm.Value(0.0);
        var follower = service.Spring(source, new BmSpring { Stiffness = 300, Damping = 30, Velocity = -2000 });

        source.SetSync(100);
        RunFrames(engine, 0, 16);

        Assert.IsTrue(follower.Value < 0, $"the follower went to {follower.Value} instead of first moving away from its target");
    }

    [TestMethod]
    public async Task Spring_SupersedesAnAnimationOfTheFollowerItself_RatherThanRedirectingIt()
    {
        // An animation someone started on the follower has a caller awaiting where it lands: a change of the source
        // replaces it, as any new animation of the value would, instead of pointing it somewhere else unannounced.
        var (service, engine, _) = Create();
        await using var __ = engine;
        var source = Bm.Value(0.0);
        var follower = service.Spring(source, new BmSpring { Stiffness = 300, Damping = 30 });
        source.SetSync(100);
        RunFrames(engine, 0, 48);

        var animation = service.AnimateAsync(follower, 500, Bm.Spring());
        RunFrames(engine, 64, 112);
        source.SetSync(200);

        await animation.WaitAsync(Timeout);   // superseded: settled without another frame
        RunFrames(engine, 128, 6000);
        Assert.AreEqual(200, follower.Value, 1e-6, "and the follower follows its source again");
    }

    [TestMethod]
    public async Task Spring_TriesTheFrameLoopAgain_OnTheNextChange_WhenItFailedToStart()
    {
        // The loop start is fire-and-forget. One that failed is tried again by the next change of the source - a
        // change that only points the spring the first one started at a new target included.
        var (service, engine, interop) = Create();
        await using var __ = engine;
        interop.RafLoopStartFailures = 1;
        var source = Bm.Value(0.0);
        service.Spring(source, new BmSpring { Stiffness = 300, Damping = 30 });

        source.SetSync(100);
        Assert.AreEqual(0, interop.CountOf("startRafLoop"), "the first start failed");
        source.SetSync(200);

        Assert.AreEqual(1, interop.CountOf("startRafLoop"));
    }

    [TestMethod]
    public async Task Spring_WithoutAFrameLoop_TracksTheSourceDirectly()
    {
        var (service, engine, _) = Create(inProcess: false);
        await using var __ = engine;
        var source = Bm.Value(0.0);
        var follower = service.Spring(source);

        source.SetSync(75);

        Assert.AreEqual(75, follower.Value);
    }

    // ── Sequences ─────────────────────────────────────────────────────────────

    private static (BmotionAnimateService, BmotionAnimationEngine, FakeBmotionInterop) CreateForSequence()
    {
        var created = Create();
        created.Interop.SelectorResolver = s => [s.TrimStart('#')];
        return created;
    }

    private static readonly BmSequence TwoSteps = new BmSequence()
        .Add("#a", Bm.To(x: 100), Bm.Tween(0.1))
        .Add("#b", Bm.To(x: 50), Bm.Tween(0.1)); // starts when "#a" ends, at 0.1s

    private static bool Started(FakeBmotionInterop interop, string id)
        => interop.ArgsOf("playWaapiAnimation").Any(a => (string)a[0]! == id);

    [TestMethod]
    public async Task RunAsync_StartsEachSegmentAtItsTimelinePosition()
    {
        var (service, engine, interop) = CreateForSequence();
        await using var _ = engine;

        var controls = await service.RunAsync(TwoSteps);

        engine.ComputeFrame(0);
        Assert.IsTrue(Started(interop, "a"), "the first segment starts at 0");
        RunFrames(engine, 16, 96);
        Assert.IsFalse(Started(interop, "b"), "the second waits for the first to end");
        RunFrames(engine, 112, 128);
        Assert.IsTrue(Started(interop, "b"));

        interop.FinishWaapi();
        await controls.WhenCompleteAsync().WaitAsync(Timeout);
    }

    [TestMethod]
    public async Task RunAsync_Paused_HoldsTheTimelineGapsToo()
    {
        var (service, engine, interop) = CreateForSequence();
        await using var _ = engine;

        var controls = await service.RunAsync(TwoSteps);
        engine.ComputeFrame(0);
        controls.Pause();
        RunFrames(engine, 16, 1000);
        Assert.IsFalse(Started(interop, "b"), "a paused timeline must not start its later segments");

        controls.Play();
        RunFrames(engine, 1016, 1200);
        Assert.IsTrue(Started(interop, "b"));
    }

    [TestMethod]
    public async Task RunAsync_Cancelled_StopsTheRunningSegment_AndNeverStartsTheRest()
    {
        var (service, engine, interop) = CreateForSequence();
        await using var _ = engine;
        using var cts = new CancellationTokenSource();

        var controls = await service.RunAsync(TwoSteps, cts.Token);
        engine.ComputeFrame(0);
        cts.Cancel();

        await controls.WhenCompleteAsync().WaitAsync(Timeout);
        RunFrames(engine, 16, 500);
        Assert.IsFalse(Started(interop, "b"));
        Assert.IsTrue(interop.ArgsOf("cancelWaapiAnimation").Any(a => (string)a[0]! == "a"), "the running segment is stopped");
        Assert.IsFalse(engine.IsRegistered("a") || engine.IsRegistered("b"), "every target is released");
    }

    [TestMethod]
    public async Task RunAsync_APreCancelledToken_ResolvesNothing()
    {
        var (service, engine, interop) = CreateForSequence();
        await using var _ = engine;

        var controls = await service.RunAsync(TwoSteps, new CancellationToken(canceled: true));

        Assert.IsFalse(interop.WasCalled("resolveOrRegisterBySelector"));
        Assert.IsTrue(controls.WhenCompleteAsync().IsCompleted);
    }
}
