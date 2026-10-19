using Bit.Bmotion.Tests.TestInfra;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;

namespace Bit.Bmotion.Tests.Services;

/// <summary>
/// What a programmatic animation (<see cref="BmotionAnimateService"/>) leaves on its elements once
/// it lets go of them: their values - an element animated only programmatically still shows them,
/// so the next animation on it starts there instead of from identity - and never its playback
/// rate, which would otherwise freeze or re-time every later animation of the element.
/// </summary>
[TestClass]
public class AnimateServiceStateTests
{
    private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(5);

    private static (BmotionAnimateService Service, BmotionAnimationEngine Engine, FakeBmotionInterop Interop) Create()
    {
        var interop = new FakeBmotionInterop { IsInProcess = true };
        var engine = new BmotionAnimationEngine(interop);
        return (new BmotionAnimateService(engine, interop), engine, interop);
    }

    private static void RunFrames(BmotionAnimationEngine engine, double fromMs, double toMs)
    {
        for (double t = fromMs; t <= toMs; t += 16) engine.ComputeFrame(t);
    }

    // The controls release their elements from the completion's continuation (on the default
    // scheduler here, there being no synchronization context), so give it the time it needs.
    private static async Task WaitUntilAsync(Func<bool> condition, string because)
    {
        var deadline = DateTime.UtcNow + Timeout;
        while (!condition())
        {
            if (DateTime.UtcNow > deadline) Assert.Fail(because);
            await Task.Delay(5);
        }
    }

    private static string FirstFrameTransform(FakeBmotionInterop interop)
        => (string)((Dictionary<string, object>[])interop.ArgsOf("playWaapiAnimation").Last()[2]!)[0]["transform"];

    // ── Values retained between programmatic animations ───────────────────────

    [TestMethod]
    public async Task SecondAnimation_OnAReleasedProgrammaticElement_StartsFromWhereTheFirstLeftIt()
    {
        var (service, engine, interop) = Create();
        await using var _ = engine;

        var first = await service.AnimateAsync("#box", Bm.To(x: 100, scale: 2), Bm.Tween(0.3));
        interop.FinishWaapi();
        await first.WhenCompleteAsync().WaitAsync(Timeout);
        await WaitUntilAsync(() => !engine.IsRegistered("bm-sel-1"), "the finished animation must release its element");

        await service.AnimateAsync("#box", Bm.To(x: 200), Bm.Tween(0.3));

        // From x 100 - and with the scale it doesn't animate kept - rather than from identity.
        var state = engine.GetDiagnostics().Single(d => d.Id == "bm-sel-1");
        Assert.AreEqual(100, state.Transforms["x"], 1e-9);
        Assert.AreEqual(2, state.Transforms["scale"], 1e-9);
        var from = FirstFrameTransform(interop);
        StringAssert.Contains(from, "100px", $"the second animation must leave from x 100: {from}");
        StringAssert.Contains(from, "scale(2)", $"and keep the scale: {from}");
    }

    [TestMethod]
    public async Task ProgrammaticAnimation_OnAnElementAComponentOwned_DoesNotResumeItsState()
    {
        using var ctx = new BmotionTestContext();
        var service = ctx.Services.GetRequiredService<BmotionAnimateService>();
        ctx.Interop.SelectorResolver = _ => ["box"];
        // A <Bmotion> animates the element to x 100, and is then unmounted.
        var host = ctx.Render<ToggleHost>(ps => ps.Add(p => p.ChildContent, (RenderFragment)(b =>
        {
            b.OpenComponent<Bmotion>(0);
            b.AddComponentParameter(1, nameof(Bmotion.Id), "box");
            b.AddComponentParameter(2, nameof(Bmotion.Animate), (BmTarget?)Bm.To(x: 100));
            b.AddComponentParameter(3, nameof(Bmotion.ChildContent), (RenderFragment)(leaf =>
            {
                leaf.OpenElement(0, "div");
                leaf.CloseElement();
            }));
            b.CloseComponent();
        })));
        await ctx.FinishWaapiAsync("box");
        host.WaitForAssertion(() => Assert.AreEqual(100, ctx.Engine.GetDiagnostics().Single(d => d.Id == "box").Transforms["x"], 1e-9));
        host.Render(ps => ps.Add(p => p.Show, false));
        Assert.IsFalse(ctx.Engine.IsRegistered("box"));

        await host.InvokeAsync(async () => await service.AnimateAsync("#box", Bm.To(x: 200), Bm.Tween(0.3)));

        // A component renders its element from scratch: nothing it left in the engine is resumed.
        var state = ctx.Engine.GetDiagnostics().Single(d => d.Id == "box");
        Assert.IsFalse(state.Transforms.ContainsKey("x"), "a component-owned element's state must not be retained");
    }

    // ── Playback rate restored on release ─────────────────────────────────────

    // A colour tween on an element something else also holds (as a <Bmotion> would), so the
    // element - and its playback rate - outlive the controls.
    private static async Task<(BmotionAnimationEngine Engine, BmAnimationControls Controls)> StartOnSharedElement()
    {
        var (service, engine, interop) = Create();
        interop.SelectorResolver = _ => ["el"];
        engine.RegisterElement("el", new Dictionary<string, object?> { ["backgroundColor"] = "#000000" });
        var controls = await service.AnimateAsync("#el", Bm.To(backgroundColor: "#ffffff"), Bm.Tween(0.5, ease: BmEase.Linear));
        engine.ComputeFrame(0);
        return (engine, controls);
    }

    private static double RateOf(BmotionAnimationEngine engine) => engine.GetState("el")!.PlaybackRate;

    [TestMethod]
    public async Task Pause_ThenStop_RestoresRealtime()
    {
        var (engine, controls) = await StartOnSharedElement();
        await using var _ = engine;
        controls.Pause();
        Assert.AreEqual(0, RateOf(engine));

        controls.Stop();

        Assert.AreEqual(1, RateOf(engine), "the element's next animation must not start paused");
    }

    [TestMethod]
    public async Task SetSpeed_ThenComplete_RestoresRealtime()
    {
        var (engine, controls) = await StartOnSharedElement();
        await using var _ = engine;
        controls.SetSpeed(3);

        controls.Complete();

        Assert.AreEqual(1, RateOf(engine));
    }

    [TestMethod]
    public async Task SetSpeed_ThenNaturalCompletion_RestoresRealtime()
    {
        var (engine, controls) = await StartOnSharedElement();
        await using var _ = engine;
        controls.SetSpeed(2);
        Assert.AreEqual(2, RateOf(engine));

        RunFrames(engine, 16, 600);
        await controls.WhenCompleteAsync().WaitAsync(Timeout);

        await WaitUntilAsync(() => RateOf(engine) == 1, "a natural finish must put the rate back too");
    }

    [TestMethod]
    public async Task SpeedChange_PutsBackTheRateTheElementHadBefore_NotRealtime()
    {
        // An element its owner had paused: these controls re-timing and then releasing it must
        // leave it paused, as they found it - not un-pause it behind the owner's back.
        var (engine, controls) = await StartOnSharedElement();
        await using var _ = engine;
        engine.SetPlaybackRate("el", 0);
        controls.SetSpeed(2);
        Assert.AreEqual(2, RateOf(engine));
        controls.Pause();

        controls.Stop();

        Assert.AreEqual(0, RateOf(engine), "the rate goes back to what it was before the first SetSpeed");
    }

    [TestMethod]
    public async Task NoSpeedChange_LeavesARateSetElsewhereAlone()
    {
        // The controls only undo what they did: a rate the element's owner set is not theirs to reset.
        var (engine, controls) = await StartOnSharedElement();
        await using var _ = engine;
        engine.SetPlaybackRate("el", 0.5);

        controls.Stop();

        Assert.AreEqual(0.5, RateOf(engine));
    }
}
