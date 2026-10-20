using Bit.Bmotion.Tests.TestInfra;
using Bunit;
using Microsoft.AspNetCore.Components;

namespace Bit.Bmotion.Tests.Components;

/// <summary>
/// Tests for the <see cref="BmotionConfig"/> cascade: its default <c>Transition</c>, the
/// <c>ReduceMotion</c> override in both directions, <c>TransitionSpeed</c> scaling, and the global
/// <c>ColorSpace</c>. A transform/opacity target offloads to the compositor, so the timing the
/// cascade produced is read straight off the Web Animation handed to the browser.
/// </summary>
[TestClass]
public class ConfigCascadeTests
{
    private static RenderFragment Box(BmProps animate, BmTransition? transition = null, BmProps? initial = null) => b =>
    {
        b.OpenComponent<Bmotion>(0);
        b.AddComponentParameter(1, nameof(Bmotion.Id), "box");
        if (initial is not null) b.AddComponentParameter(2, nameof(Bmotion.Initial), (BmTarget?)initial);
        b.AddComponentParameter(3, nameof(Bmotion.Animate), (BmTarget?)animate);
        if (transition is not null) b.AddComponentParameter(4, nameof(Bmotion.Transition), transition);
        b.AddComponentParameter(5, nameof(Bmotion.ChildContent), (RenderFragment)(leaf =>
        {
            leaf.OpenElement(0, "div");
            leaf.CloseElement();
        }));
        b.CloseComponent();
    };

    private static Dictionary<string, object?>? OffloadTiming(BmotionTestContext ctx)
        => ctx.Interop.ArgsOf("playWaapiAnimation").LastOrDefault() is { } args ? (Dictionary<string, object?>)args[3]! : null;

    private static BmotionElementDiagnostics State(BmotionTestContext ctx) => ctx.Engine.GetDiagnostics().Single(d => d.Id == "box");

    [TestMethod]
    public void Transition_IsTheDefaultForElementsWithoutTheirOwn()
    {
        using var ctx = new BmotionTestContext();
        ctx.Render<BmotionConfig>(ps => ps
            .Add(p => p.Transition, Bm.Tween(0.8))
            .Add(p => p.ChildContent, Box(Bm.To(x: 100))));

        Assert.AreEqual(800.0, (double)OffloadTiming(ctx)!["duration"]!);
    }

    [TestMethod]
    public void Transition_AnElementsOwnTransitionWins()
    {
        using var ctx = new BmotionTestContext();
        ctx.Render<BmotionConfig>(ps => ps
            .Add(p => p.Transition, Bm.Tween(0.8))
            .Add(p => p.ChildContent, Box(Bm.To(x: 100), Bm.Tween(0.2))));

        Assert.AreEqual(200.0, (double)OffloadTiming(ctx)!["duration"]!);
    }

    [TestMethod]
    public async Task ReduceMotionTrue_SnapsTransforms_ButStillAnimatesOpacity()
    {
        using var ctx = new BmotionTestContext();
        ctx.Render<BmotionConfig>(ps => ps
            .Add(p => p.ReduceMotion, true)
            .Add(p => p.ChildContent, Box(Bm.To(x: 100, opacity: 0.2), Bm.Tween(1, ease: BmEase.Linear))));

        Assert.IsNull(OffloadTiming(ctx), "a reduced transition is split per property, which stays on the frame loop");
        await ctx.RunFramesAsync(0, 96);

        var state = State(ctx);
        Assert.AreEqual(100, state.Transforms["x"], 1e-9, "transforms snap under reduced motion");
        Assert.IsTrue(state.NumericValues["opacity"] > 0.2, "opacity keeps its normal (still running) transition");
        Assert.IsFalse(ctx.Interop.WasCalled("prefersReducedMotion"), "an explicit override needs no OS probe");
    }

    [TestMethod]
    public void ReduceMotionFalse_OverridesAnAlwaysReducePolicy()
    {
        using var ctx = new BmotionTestContext();
        ctx.Options.ReducedMotion = BmReducedMotionMode.Always;
        ctx.Render<BmotionConfig>(ps => ps
            .Add(p => p.ReduceMotion, false)
            .Add(p => p.ChildContent, Box(Bm.To(x: 100), Bm.Tween(0.6))));

        Assert.AreEqual(600.0, (double)OffloadTiming(ctx)!["duration"]!, "the local config wins over the library option");
    }

    [TestMethod]
    public void ReduceMotionNull_InsideAConfig_FollowsTheOsPreference()
    {
        using var ctx = new BmotionTestContext();
        ctx.Interop.PrefersReducedMotion = true;
        ctx.Render<BmotionConfig>(ps => ps
            .Add(p => p.ChildContent, Box(Bm.To(x: 100), Bm.Tween(0.6))));

        Assert.IsTrue(ctx.Interop.WasCalled("prefersReducedMotion"));
        Assert.IsNull(OffloadTiming(ctx), "the OS asked for reduced motion, so the transform snaps");
    }

    [TestMethod]
    public async Task TransitionSpeedZero_MakesEveryAnimationInstant()
    {
        using var ctx = new BmotionTestContext();
        ctx.Render<BmotionConfig>(ps => ps
            .Add(p => p.TransitionSpeed, 0.0)
            .Add(p => p.ChildContent, Box(Bm.To(x: 100), Bm.Tween(2, delay: 1))));

        Assert.IsNull(OffloadTiming(ctx), "a zero-length tween has nothing to hand the compositor");
        await ctx.RunFramesAsync(0, 0);
        Assert.AreEqual(100, State(ctx).Transforms["x"], 1e-9, "duration and delay both collapse");
    }

    [TestMethod]
    public void TransitionSpeedTwo_HalvesDurationAndDelay()
    {
        using var ctx = new BmotionTestContext();
        ctx.Render<BmotionConfig>(ps => ps
            .Add(p => p.TransitionSpeed, 2.0)
            .Add(p => p.ChildContent, Box(Bm.To(x: 100), Bm.Tween(1, delay: 0.4))));

        var timing = OffloadTiming(ctx)!;
        Assert.AreEqual(500.0, (double)timing["duration"]!, 1e-9);
        Assert.AreEqual(200.0, (double)timing["delay"]!, 1e-9);
    }

    // Plays a red→blue linear tween to its midpoint under the given cascade and returns the colour.
    private static async Task<string> MidpointColor(BmColorSpace? globalSpace, BmColorSpace? ownSpace = null)
    {
        using var ctx = new BmotionTestContext();
        ctx.Render<BmotionConfig>(ps => ps
            .Add(p => p.ColorSpace, globalSpace)
            .Add(p => p.ChildContent, Box(
                Bm.To(backgroundColor: "#0000ff"),
                Bm.Tween(1, ease: BmEase.Linear, colorSpace: ownSpace),
                initial: Bm.To(backgroundColor: "#ff0000"))));

        await ctx.RunFramesAsync(0, 496);
        return State(ctx).StringValues["backgroundColor"];
    }

    [TestMethod]
    public async Task ColorSpace_AppliesToTransitionsThatDontSetTheirOwn()
    {
        var srgb = await MidpointColor(null);
        var oklab = await MidpointColor(BmColorSpace.Oklab);

        Assert.AreNotEqual(srgb, oklab, "a perceptual mix must differ from the plain sRGB one mid-way");
    }

    [TestMethod]
    public async Task ColorSpace_ATransitionsOwnSpaceWins()
    {
        var srgb = await MidpointColor(null);
        var overridden = await MidpointColor(BmColorSpace.Oklab, ownSpace: BmColorSpace.Srgb);

        Assert.AreEqual(srgb, overridden);
    }
}
