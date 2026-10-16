using Bit.Bmotion.Tests.TestInfra;
using Bunit;
using Microsoft.AspNetCore.Components;

namespace Bit.Bmotion.Tests.Engine;

/// <summary>
/// Playback rate and per-frame callbacks versus the compositor offload: <c>SetPlaybackRate</c>
/// re-times the animations the browser is already playing (in the bridge and in the engine's
/// mirror of them), and an element that is paused or re-timed - or reports every frame through
/// <c>OnUpdate</c> - plays new animations on the frame loop, which honours both, whenever there is
/// one. Without a frame loop (Blazor Server) the offload is the only way to animate at all, so
/// there it stays.
/// </summary>
[TestClass]
public class PlaybackRateOffloadTests
{
    private static (BmotionAnimationEngine Engine, FakeBmotionInterop Interop) NewEngine(bool inProcess = true)
    {
        var interop = new FakeBmotionInterop { IsInProcess = inProcess, SupportsLinearEasing = true };
        return (new BmotionAnimationEngine(interop), interop);
    }

    private static Dictionary<string, object?> Opacity(double value) => new() { ["opacity"] = value };

    private static BmotionElementDiagnostics State(BmotionAnimationEngine engine, string id = "el")
        => engine.GetDiagnostics().Single(d => d.Id == id);

    // A long linear x plan, one second in: what the engine mirrors of a playing compositor animation.
    private static BmotionElementAnimationState.WaapiPlan PlayingPlan() => new()
    {
        Token = 7,
        StartMs = Environment.TickCount64 - 1000,
        DelayMs = 0,
        DurationMs = 1_000_000,
        Progress = [0, 1],
        Values = new() { ["x"] = (0, 100) },
        Iterations = 0,
        Mirror = false,
    };

    // ── Re-timing playing compositor animations ───────────────────────────────

    [TestMethod]
    public async Task SetPlaybackRate_RetimesTheMirroredPlans_AndTheBrowsersAnimations()
    {
        var (engine, interop) = NewEngine();
        await using var _ = engine;
        engine.RegisterElement("el");
        var plan = PlayingPlan();
        engine.GetState("el")!.AddWaapiPlan(plan);

        engine.SetPlaybackRate("el", 0);

        // Paused: the mirror stops advancing where it was (a second in, give or take the time the
        // test itself took), so an interruption later reads where the browser really holds it.
        Assert.AreEqual(0, plan.Rate);
        long later = Environment.TickCount64 + 60_000;
        double frozen = plan.PlayedMs(later);
        Assert.IsTrue(frozen >= 1000 && frozen < 30_000, $"paused about a second in: {frozen}");
        Assert.AreEqual(frozen, plan.PlayedMs(later + 60_000), 1e-9, "a paused animation does not advance");

        engine.SetPlaybackRate("el", 2);

        Assert.AreEqual(2, plan.Rate);
        long t = Environment.TickCount64 + 1000;
        Assert.AreEqual(200, plan.PlayedMs(t + 100) - plan.PlayedMs(t), 1e-9, "at double speed 100ms play 200ms of it");
        Assert.AreEqual(frozen, plan.PlayedMs(Environment.TickCount64 - 1), 50, "and it resumes from where it was paused");

        // The browser is told each time - the element's compositor animations play on its clock.
        var calls = interop.ArgsOf("setWaapiPlaybackRate");
        CollectionAssert.AreEqual(new object?[] { "el", 0.0 }, calls[0]);
        CollectionAssert.AreEqual(new object?[] { "el", 2.0 }, calls[1]);
    }

    [TestMethod]
    public async Task SetPlaybackRate_WithNoCompositorAnimations_LeavesTheBridgeAlone()
    {
        var (engine, interop) = NewEngine();
        await using var _ = engine;
        engine.RegisterElement("el");

        engine.SetPlaybackRate("el", 0);

        Assert.IsFalse(interop.WasCalled("setWaapiPlaybackRate"));
        Assert.AreEqual(0, engine.GetState("el")!.PlaybackRate);
    }

    // ── A paused or re-timed element and new animations ───────────────────────

    [TestMethod]
    public async Task PausedElement_InProcess_PlaysANewAnimationOnTheFrameLoop_WhereThePauseHolds()
    {
        var (engine, interop) = NewEngine();
        await using var _ = engine;
        engine.RegisterElement("el", Opacity(0));
        engine.SetPlaybackRate("el", 0);

        await engine.AnimateToAsync("el", Opacity(0.5), Bm.Tween(0.2).ToConfig());

        // A compositor animation would run at realtime regardless of the pause.
        Assert.IsFalse(interop.WasCalled("playWaapiAnimation"));
        CollectionAssert.Contains(State(engine).ActiveProperties.ToArray(), "opacity");
        for (double t = 0; t <= 1000; t += 16) engine.ComputeFrame(t);
        Assert.AreEqual(0, State(engine).NumericValues["opacity"], 1e-9, "paused: it holds");

        engine.SetPlaybackRate("el", 1);
        for (double t = 1016; t <= 1500; t += 16) engine.ComputeFrame(t);
        Assert.AreEqual(0.5, State(engine).NumericValues["opacity"], 1e-9);
    }

    [TestMethod]
    public async Task ReTimedElement_WithoutAFrameLoop_StillOffloads_AtItsRate()
    {
        var (engine, interop) = NewEngine(inProcess: false);
        await using var _ = engine;
        engine.RegisterElement("el");
        engine.SetPlaybackRate("el", 0.5);

        await engine.AnimateToAsync("el", Opacity(0.5), Bm.Tween(0.2).ToConfig());

        var timing = (Dictionary<string, object?>)interop.ArgsOf("playWaapiAnimation").Single()[3]!;
        Assert.AreEqual(0.5, (double)timing["playbackRate"]!);
    }

    // ── Per-frame callbacks ───────────────────────────────────────────────────

    [TestMethod]
    public async Task OnFrameCallback_InProcess_KeepsTheAnimationOnTheFrameLoop()
    {
        var (engine, interop) = NewEngine();
        await using var _ = engine;
        engine.RegisterElement("el", Opacity(0));
        var frames = new List<string>();
        engine.SetOnFrame("el", d => { if (d.TryGetValue("opacity", out var o)) frames.Add(o); });

        await engine.AnimateToAsync("el", Opacity(1), Bm.Tween(0.2).ToConfig());
        for (double t = 0; t <= 400; t += 16) engine.ComputeFrame(t);

        // A compositor animation has no frames to report; the frame loop reports every one.
        Assert.IsFalse(interop.WasCalled("playWaapiAnimation"));
        Assert.IsTrue(frames.Distinct().Count() >= 5, $"expected per-frame reports: {string.Join(", ", frames)}");
        Assert.AreEqual("1", frames[^1]);
    }

    [TestMethod]
    public async Task OnFrameCallback_WithoutAFrameLoop_StillOffloads()
    {
        var (engine, interop) = NewEngine(inProcess: false);
        await using var _ = engine;
        engine.RegisterElement("el", Opacity(0));
        engine.SetOnFrame("el", _ => { });

        await engine.AnimateToAsync("el", Opacity(1), Bm.Tween(0.2).ToConfig());

        // Blazor Server: without the offload this would collapse to an instant change.
        Assert.AreEqual(1, interop.CountOf("playWaapiAnimation"));
    }

    [TestMethod]
    public void OnUpdateParameter_InProcess_KeepsTheEnterOffTheCompositor()
    {
        using var ctx = new BmotionTestContext();
        ctx.Render<Bmotion>(ps => ps
            .Add(p => p.Id, "box")
            .Add(p => p.Initial, Bm.To(opacity: 0))
            .Add(p => p.Animate, Bm.To(opacity: 1))
            .Add(p => p.OnUpdate, (Action<IReadOnlyDictionary<string, string>>)(_ => { }))
            .Add(p => p.ChildContent, (RenderFragment)(b => { b.OpenElement(0, "div"); b.CloseElement(); })));

        Assert.IsFalse(ctx.Interop.WasCalled("playWaapiAnimation"));
        CollectionAssert.Contains(ctx.Engine.GetDiagnostics().Single(d => d.Id == "box").ActiveProperties.ToArray(), "opacity");
    }
}
