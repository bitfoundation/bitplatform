using Bit.Bmotion.Tests.TestInfra;

namespace Bit.Bmotion.Tests.Services;

/// <summary>
/// <see cref="BmAnimationControls"/> reporting and moving time (motion.dev's <c>time</c>/<c>duration</c>/<c>state</c>):
/// how long an animation lasts, where its playhead is under pause and speed, a seek that moves browser animations in
/// place, a seek that plays the animation again from part-way in (the frame loop, after it ended), and sequences.
/// </summary>
[TestClass]
public class ControlsTimeTests
{
    private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(5);

    private static (BmotionAnimateService Service, BmotionAnimationEngine Engine, FakeBmotionInterop Interop) Create(bool inProcess = true)
    {
        var interop = new FakeBmotionInterop { IsInProcess = inProcess, SupportsLinearEasing = true, SelectorResolver = s => [s.TrimStart('#')] };
        var engine = new BmotionAnimationEngine(interop);
        return (new BmotionAnimateService(engine, interop), engine, interop);
    }

    private static Dictionary<string, object?> LastTiming(FakeBmotionInterop interop)
        => (Dictionary<string, object?>)interop.ArgsOf("playWaapiAnimation").Last()[3]!;

    private static double X(BmotionAnimationEngine engine, string id = "box")
        => engine.GetDiagnostics().Single(d => d.Id == id).Transforms.GetValueOrDefault("x");

    // ── Duration ──────────────────────────────────────────────────────────────

    [TestMethod]
    public async Task Duration_IsTheWholeAnimation_StaggerAndRepeatsIncluded()
    {
        var (service, engine, interop) = Create();
        await using var _ = engine;
        interop.SelectorResolver = _ => ["a", "b", "c"];

        var staggered = await service.AnimateAsync(".item", Bm.To(x: 100), Bm.Tween(0.5), Bm.Stagger(0.1));
        Assert.AreEqual(0.7, staggered.Duration, 1e-9, "the last element starts 0.2 s in and plays 0.5 s");

        var repeated = await service.AnimateAsync("#r", Bm.To(x: 100), Bm.Tween(0.5, delay: 0.1, repeat: 2));
        Assert.AreEqual(1.6, repeated.Duration, 1e-9, "a delay and three plays");

        var forever = await service.AnimateAsync("#f", Bm.To(x: 100), Bm.Tween(0.5, repeat: BmRepeat.Forever));
        Assert.IsTrue(double.IsPositiveInfinity(forever.Duration));
        Assert.AreEqual(0, forever.Progress, "no finite length, no progress");
    }

    // ── Time and state ────────────────────────────────────────────────────────

    [TestMethod]
    public async Task Time_StandsStillWhilePaused_AndTheStateSaysSo()
    {
        var (service, engine, _) = Create();
        await using var __ = engine;

        var controls = await service.AnimateAsync("#box", Bm.To(x: 100), Bm.Tween(10));
        Assert.AreEqual(BmPlaybackState.Running, controls.State);

        controls.Pause();
        Assert.AreEqual(BmPlaybackState.Paused, controls.State);
        double held = controls.Time;
        await Task.Delay(30);
        Assert.AreEqual(held, controls.Time, "a paused playhead doesn't move");

        controls.Play();
        Assert.AreEqual(BmPlaybackState.Running, controls.State);
        await Task.Delay(30);
        Assert.IsTrue(controls.Time > held, "and moves again once played");
    }

    [TestMethod]
    public async Task Finishing_LandsTheTimeOnTheDuration()
    {
        var (service, engine, interop) = Create();
        await using var _ = engine;

        var controls = await service.AnimateAsync("#box", Bm.To(x: 100), Bm.Tween(0.4));
        interop.FinishWaapi();
        await controls.WhenCompleteAsync().WaitAsync(Timeout);

        Assert.AreEqual(BmPlaybackState.Finished, controls.State);
        Assert.AreEqual(0.4, controls.Time, 1e-9);
        Assert.AreEqual(1, controls.Progress, 1e-9);
    }

    [TestMethod]
    public async Task Stopping_FreezesTheTime()
    {
        var (service, engine, _) = Create();
        await using var __ = engine;

        var controls = await service.AnimateAsync("#box", Bm.To(x: 100), Bm.Tween(10));
        controls.Stop();
        double stoppedAt = controls.Time;
        await Task.Delay(20);

        Assert.AreEqual(BmPlaybackState.Stopped, controls.State);
        Assert.AreEqual(stoppedAt, controls.Time);
    }

    // ── Seeking ───────────────────────────────────────────────────────────────

    [TestMethod]
    public async Task SeekingABrowserAnimation_MovesItInPlace()
    {
        var (service, engine, interop) = Create();
        await using var _ = engine;
        engine.RegisterElement("box", null); // kept registered past Stop, to read where it was left

        var controls = await service.AnimateAsync("#box", Bm.To(x: 100), Bm.Tween(1, BmEase.Linear));
        controls.Pause();
        int plays = interop.CountOf("playWaapiAnimation");

        controls.Seek(0.5);

        Assert.AreEqual(plays, interop.CountOf("playWaapiAnimation"), "moved in place, not played again");
        var seek = interop.ArgsOf("seekWaapiAnimations").Single();
        Assert.AreEqual("box", seek[0]);
        Assert.AreEqual(500.0, (double)seek[1]!, 1e-9);
        Assert.AreEqual(0.5, controls.Time, 1e-9);
        Assert.AreEqual(BmPlaybackState.Paused, controls.State, "a paused animation holds at the new point");

        // The engine's mirror moved with it: stopping now freezes the element halfway.
        controls.Stop();
        Assert.AreEqual(50, X(engine), 1);
    }

    [TestMethod]
    [DataRow(true)]
    [DataRow(false)]
    public async Task SeekingAFinishedAnimation_TakesHoldOfItAgain_AndHoldsThere(bool inProcess)
    {
        var (service, engine, interop) = Create(inProcess);
        await using var _ = engine;

        var controls = await service.AnimateAsync("#box", Bm.To(x: 100), Bm.Tween(1, BmEase.Linear));
        interop.FinishWaapi();
        await controls.WhenCompleteAsync().WaitAsync(Timeout);

        controls.Seek(0.25);

        Assert.AreEqual(BmPlaybackState.Paused, controls.State);
        Assert.AreEqual(0.25, controls.Time, 1e-9);
        if (inProcess)
        {
            // A paused element plays new animations on the frame loop: held there, a quarter of the way.
            engine.ComputeFrame(0);
            Assert.AreEqual(25, X(engine), 0.5);
        }
        else
        {
            Assert.AreEqual(-250.0, (double)LastTiming(interop)["delay"]!, 1e-9, "played again from 0.25 s in");
        }
        Assert.IsFalse(controls.WhenCompleteAsync().IsCompleted, "there is something to wait for again");

        controls.Play();
        Assert.AreEqual(BmPlaybackState.Running, controls.State);
        interop.FinishWaapi();
        // The frame loop finishes what it holds. Frames stop once the animation has ended: the engine is
        // single-threaded, and here (no synchronization context) its completion lands on another thread.
        for (int i = 0; inProcess && i <= 80 && !controls.WhenCompleteAsync().IsCompleted; i++)
        {
            engine.ComputeFrame(i * 16);
            await Task.Delay(1);
        }
        await controls.WhenCompleteAsync().WaitAsync(Timeout);
        Assert.AreEqual(BmPlaybackState.Finished, controls.State);
    }

    [TestMethod]
    public async Task PlayingAFinishedAnimation_PlaysItAgainFromTheStart()
    {
        var (service, engine, interop) = Create();
        await using var _ = engine;

        var controls = await service.AnimateAsync("#box", Bm.To(x: 100), Bm.Tween(1));
        interop.FinishWaapi();
        await controls.WhenCompleteAsync().WaitAsync(Timeout);
        int plays = interop.CountOf("playWaapiAnimation");

        controls.Play();

        Assert.AreEqual(plays + 1, interop.CountOf("playWaapiAnimation"));
        Assert.AreEqual(BmPlaybackState.Running, controls.State);
        Assert.AreEqual(0.0, (double)LastTiming(interop)["delay"]!, 1e-9);
    }

    [TestMethod]
    public async Task SeekingOnTheFrameLoop_PlaysItAgainFromThePoint_FromWhereItStarted()
    {
        var (service, engine, interop) = Create();
        await using var _ = engine;
        interop.WebAnimationsSupported = false;
        engine.RegisterElement("box", new Dictionary<string, object?> { ["x"] = 20.0 });

        var controls = await service.AnimateAsync("#box", Bm.To(x: 120), Bm.Tween(1, BmEase.Linear));
        for (int i = 0; i <= 50; i++) engine.ComputeFrame(i * 16);
        Assert.IsTrue(X(engine) > 60, "it has moved on");

        controls.Seek(0.25);
        engine.ComputeFrame(2000);

        // From where it started (20), a quarter of the way to 120.
        Assert.AreEqual(45, X(engine), 0.5);
    }

    [TestMethod]
    public async Task SeekingASpringPlayedAgain_StartsItPartWayThroughItsMotion()
    {
        var (service, engine, interop) = Create();
        await using var _ = engine;
        interop.WebAnimationsSupported = false;
        engine.RegisterElement("box", new Dictionary<string, object?> { ["x"] = 0.0 });
        var spring = Bm.Spring(stiffness: 100, damping: 10);

        var controls = await service.AnimateAsync("#box", Bm.To(x: 100), spring);
        controls.Seek(0.3);
        engine.ComputeFrame(0);
        double sought = X(engine);

        // The same spring, ticked frame by frame to 0.3 s.
        var reference = new List<double>();
        var driver = new BmotionSpringDriver(0, 100, spring.ToConfig(), reference.Add);
        for (double t = 0; t <= 300 + 1e-9; t += 1000.0 / 60) driver.Tick(t);
        Assert.AreEqual(reference[^1], sought, 2, "where the spring would be 0.3 s in");
        Assert.IsTrue(sought > 30, "it doesn't start from rest");
    }

    [TestMethod]
    public void SeekRejectsNonFiniteTimes()
    {
        var (service, engine, _) = Create();
        var controls = service.AnimateAsync("#box", Bm.To(x: 100), Bm.Tween(1)).AsTask().GetAwaiter().GetResult();
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => controls.Seek(double.NaN));
        _ = engine;
    }

    // ── Sequences ─────────────────────────────────────────────────────────────

    [TestMethod]
    public async Task ASequence_HasTheLengthOfItsTimeline_AndSeeksIntoIt()
    {
        var (service, engine, interop) = Create();
        await using var _ = engine;
        interop.SelectorResolver = s => [s.TrimStart('#')];
        var sequence = new BmSequence()
            .Add("#box", Bm.To(x: 100), Bm.Tween(1, BmEase.Linear))
            .Add("#box", Bm.To(x: 200), Bm.Tween(1, BmEase.Linear));

        var controls = await service.RunAsync(sequence);
        Assert.AreEqual(2, controls.Duration, 1e-9);

        controls.Seek(1.5);
        engine.ComputeFrame(0); // the timeline fires the step under way

        // The first step is over by then (x lands on 100), the second is half done: it plays from 0.5 s in.
        var play = interop.ArgsOf("playWaapiAnimation").Last();
        Assert.AreEqual(-500.0, (double)((Dictionary<string, object?>)play[3]!)["delay"]!, 1e-9);
        var keyframes = (Dictionary<string, object>[])play[2]!;
        StringAssert.Contains((string)keyframes[0]["transform"], "100px", "from where the first step ended");
        StringAssert.Contains((string)keyframes[^1]["transform"], "200px");
        Assert.AreEqual(1.5, controls.Time, 0.1, "and plays on from there");
    }
}
