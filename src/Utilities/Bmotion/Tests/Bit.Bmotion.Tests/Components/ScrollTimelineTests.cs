using Bit.Bmotion.Tests.TestInfra;
using Bunit;
using Microsoft.AspNetCore.Components;

namespace Bit.Bmotion.Tests.Components;

/// <summary>
/// Tests for the scroll-driven timeline path: when <c>Timeline</c> is set the browser owns the
/// animate target (so the time-based path must stand down), the binding follows parameter changes,
/// and a target the browser can't interpolate falls back to the ordinary engine.
/// </summary>
[TestClass]
public class ScrollTimelineTests
{
    private static RenderFragment Div()
        => b =>
        {
            b.OpenElement(0, "div");
            b.AddAttribute(1, "class", "box");
            b.CloseElement();
        };

    // A page-scroll-driven bar inside a host that can unmount it.
    private static IRenderedComponent<ToggleHost> RenderInHost(BmotionTestContext ctx)
        => ctx.Render<ToggleHost>(ps => ps.Add(p => p.ChildContent, (RenderFragment)(b =>
        {
            b.OpenComponent<Bmotion>(0);
            b.AddComponentParameter(1, nameof(Bmotion.Id), "bar");
            b.AddComponentParameter(2, nameof(Bmotion.Timeline), BmotionScrollTimeline.Page());
            b.AddComponentParameter(3, nameof(Bmotion.Animate), (BmotionTarget?)Bm.To(scaleX: new double[] { 0, 1 }));
            b.AddComponentParameter(4, nameof(Bmotion.ChildContent), Div());
            b.CloseComponent();
        })));

    // The renderer disposes the removed component inline while applying the diff.
    private static void Unmount(IRenderedComponent<ToggleHost> host)
        => host.Render(ps => ps.Add(p => p.Show, false));

    // ── The spec ──────────────────────────────────────────────────────────────

    [TestMethod]
    public void Page_LowersToADocumentScrollTimeline()
    {
        var js = BmotionScrollTimeline.Page().ToJsObject();

        Assert.AreEqual(false, js["view"]);
        Assert.IsNull(js["selector"]);
        Assert.AreEqual("block", js["axis"]);
    }

    [TestMethod]
    public void Axis_X_LowersToTheInlineAxis()
    {
        Assert.AreEqual("inline", BmotionScrollTimeline.Page(BmotionScrollAxis.X).ToJsObject()["axis"]);
        Assert.AreEqual("inline", BmotionScrollTimeline.View(BmotionScrollAxis.X).ToJsObject()["axis"]);
    }

    [TestMethod]
    public void View_LowersToAViewTimelineCarryingItsRange()
    {
        var js = BmotionScrollTimeline.View(range: "entry 0% cover 50%").ToJsObject();

        Assert.AreEqual(true, js["view"]);
        Assert.AreEqual("entry 0% cover 50%", js["range"]);
    }

    [TestMethod]
    public void Container_And_ViewOf_RejectAnEmptySelector()
    {
        Assert.ThrowsExactly<ArgumentException>(() => BmotionScrollTimeline.Container(" "));
        Assert.ThrowsExactly<ArgumentException>(() => BmotionScrollTimeline.ViewOf(""));
    }

    [TestMethod]
    public void AreEquivalent_ComparesByValue_SoAnInlineSpecIsNotAChange()
    {
        Assert.IsTrue(BmotionScrollTimeline.AreEquivalent(BmotionScrollTimeline.Page(), BmotionScrollTimeline.Page()));
        Assert.IsTrue(BmotionScrollTimeline.AreEquivalent(
            BmotionScrollTimeline.ViewOf("#hero", range: "cover"), BmotionScrollTimeline.ViewOf("#hero", range: "cover")));
        Assert.IsFalse(BmotionScrollTimeline.AreEquivalent(BmotionScrollTimeline.Page(), BmotionScrollTimeline.View()));
        Assert.IsFalse(BmotionScrollTimeline.AreEquivalent(
            BmotionScrollTimeline.Page(), BmotionScrollTimeline.Page(BmotionScrollAxis.X)));
        Assert.IsFalse(BmotionScrollTimeline.AreEquivalent(BmotionScrollTimeline.Page(), null));
    }

    [TestMethod]
    public void Smoothed_LowersItsSpring_ByDefaultMotionDevsRecipe()
    {
        Assert.IsNull(BmotionScrollTimeline.Page().ToJsObject()["smooth"], "an exact timeline has no spring");

        var smooth = (Dictionary<string, object?>)BmotionScrollTimeline.Page().Smoothed().ToJsObject()["smooth"]!;
        Assert.AreEqual(100.0, smooth["stiffness"]);
        Assert.AreEqual(30.0, smooth["damping"]);
        Assert.AreEqual(1.0, smooth["mass"]);

        var own = (Dictionary<string, object?>)BmotionScrollTimeline.View(BmotionScrollAxis.X).Smoothed(new BmotionSpring { Stiffness = 300, Damping = 12, Mass = 2 }).ToJsObject()["smooth"]!;
        Assert.AreEqual(300.0, own["stiffness"]);
        Assert.AreEqual(12.0, own["damping"]);
        Assert.AreEqual(2.0, own["mass"]);
    }

    [TestMethod]
    public void Smoothed_ResolvesABounceAndDurationSpring_AndKeepsTheRestOfTheTimeline()
    {
        var timeline = BmotionScrollTimeline.ViewOf("#hero", BmotionScrollAxis.X, "cover").Smoothed(new BmotionSpring { Bounce = 0, Duration = 0.4 });
        var js = timeline.ToJsObject();
        var (stiffness, damping) = BmotionTransitionConfig.SpringFromBounce(0.4, 0, 1);

        var smooth = (Dictionary<string, object?>)js["smooth"]!;
        Assert.AreEqual(stiffness, (double)smooth["stiffness"]!, 1e-9);
        Assert.AreEqual(damping, (double)smooth["damping"]!, 1e-9);
        Assert.AreEqual(true, js["view"]);
        Assert.AreEqual("#hero", js["selector"]);
        Assert.AreEqual("inline", js["axis"]);
    }

    [TestMethod]
    public void Smoothed_TakesPartInEquivalence()
    {
        Assert.IsTrue(BmotionScrollTimeline.AreEquivalent(BmotionScrollTimeline.Page().Smoothed(), BmotionScrollTimeline.Page().Smoothed()));
        Assert.IsFalse(BmotionScrollTimeline.AreEquivalent(BmotionScrollTimeline.Page(), BmotionScrollTimeline.Page().Smoothed()));
        Assert.IsFalse(BmotionScrollTimeline.AreEquivalent(
            BmotionScrollTimeline.Page().Smoothed(), BmotionScrollTimeline.Page().Smoothed(new BmotionSpring { Stiffness = 300 })));
    }

    // ── Component wiring ──────────────────────────────────────────────────────

    [TestMethod]
    public void Timeline_BindsTheAnimateTargetToScroll_InsteadOfPlayingItOverTime()
    {
        using var ctx = new BmotionTestContext();
        ctx.Render<Bmotion>(ps => ps
            .Add(p => p.Id, "bar")
            .Add(p => p.Timeline, BmotionScrollTimeline.Page())
            .Add(p => p.Animate, Bm.To(scaleX: new double[] { 0, 1 }))
            .Add(p => p.ChildContent, Div()));

        Assert.AreEqual(1, ctx.Interop.CountOf("playScrollTimelineAnimation"));
        // The compositor/time-based path must not also be driving the same property.
        Assert.AreEqual(0, ctx.Interop.CountOf("playWaapiAnimation"));
    }

    [TestMethod]
    public void Timeline_PassesTheSpecAndTheComposedKeyframesToTheBridge()
    {
        using var ctx = new BmotionTestContext();
        ctx.Render<Bmotion>(ps => ps
            .Add(p => p.Id, "bar")
            .Add(p => p.Timeline, BmotionScrollTimeline.View(range: "cover"))
            .Add(p => p.Animate, Bm.To(opacity: new double[] { 0, 1 }))
            .Add(p => p.ChildContent, Div()));

        var call = ctx.Interop.Calls.Single(c => c.Method == "playScrollTimelineAnimation");
        Assert.AreEqual("bar", call.Args[0]);

        var keyframes = (Dictionary<string, object>[])call.Args[2]!;
        Assert.AreEqual(2, keyframes.Length);
        Assert.AreEqual("0", keyframes[0]["opacity"]);
        Assert.AreEqual("1", keyframes[1]["opacity"]);

        var spec = (Dictionary<string, object?>)call.Args[3]!;
        Assert.AreEqual(true, spec["view"]);
        Assert.AreEqual("cover", spec["range"]);
    }

    [TestMethod]
    public void Timeline_ResamplesMultiStopKeyframesOntoOneAlignedGrid()
    {
        using var ctx = new BmotionTestContext();
        ctx.Render<Bmotion>(ps => ps
            .Add(p => p.Id, "bar")
            .Add(p => p.Timeline, BmotionScrollTimeline.View())
            // Four opacity stops and a scalar y: the y must expand onto the same four frames.
            .Add(p => p.Animate, Bm.To(opacity: new double[] { 0, 1, 1, 0 }, y: 100))
            .Add(p => p.ChildContent, Div()));

        var call = ctx.Interop.Calls.Single(c => c.Method == "playScrollTimelineAnimation");
        var keyframes = (Dictionary<string, object>[])call.Args[2]!;

        Assert.AreEqual(4, keyframes.Length);
        foreach (var frame in keyframes)
        {
            Assert.IsTrue(frame.ContainsKey("opacity"));
            Assert.IsTrue(frame.ContainsKey("transform"));
        }
    }

    [TestMethod]
    public void NonDrivableTarget_FallsBackToTheTimeBasedPath()
    {
        using var ctx = new BmotionTestContext();
        ctx.Render<Bmotion>(ps => ps
            .Add(p => p.Id, "bar")
            .Add(p => p.Timeline, BmotionScrollTimeline.Page())
            // backgroundColor is not something the browser can be handed as numeric keyframes here.
            .Add(p => p.Animate, Bm.To(backgroundColor: "#fff"))
            .Add(p => p.ChildContent, Div()));

        Assert.AreEqual(0, ctx.Interop.CountOf("playScrollTimelineAnimation"));
    }

    [TestMethod]
    public void BridgeReportingNoSupport_LeavesNoTimelineAttached()
    {
        using var ctx = new BmotionTestContext();
        ctx.Interop.ScrollTimelineResult = null; // could not start at all
        var host = RenderInHost(ctx);

        // Unmount it through a render diff, the way an app's @if removes it.
        Unmount(host);

        Assert.IsTrue(ctx.Interop.WasCalled("unregisterElement"), "the component must actually have been disposed");
        // Nothing was attached, so nothing should be cancelled either.
        Assert.AreEqual(0, ctx.Interop.CountOf("cancelScrollTimelineAnimation"));
    }

    [TestMethod]
    public void ScrubFallback_StillCountsAsAttached()
    {
        using var ctx = new BmotionTestContext();
        ctx.Interop.ScrollTimelineResult = false; // no native timeline; the bridge scrubs instead
        ctx.Render<Bmotion>(ps => ps
            .Add(p => p.Id, "bar")
            .Add(p => p.Timeline, BmotionScrollTimeline.Page())
            .Add(p => p.Animate, Bm.To(scaleX: new double[] { 0, 1 }))
            .Add(p => p.ChildContent, Div()));

        Assert.AreEqual(1, ctx.Interop.CountOf("playScrollTimelineAnimation"));
        Assert.AreEqual(0, ctx.Interop.CountOf("playWaapiAnimation"));
    }

    [TestMethod]
    public void ChangingTheTimeline_DetachesTheOldOneAndAttachesTheNew()
    {
        using var ctx = new BmotionTestContext();
        var cut = ctx.Render<Bmotion>(ps => ps
            .Add(p => p.Id, "bar")
            .Add(p => p.Timeline, BmotionScrollTimeline.Page())
            .Add(p => p.Animate, Bm.To(scaleX: new double[] { 0, 1 }))
            .Add(p => p.ChildContent, Div()));

        cut.Render(ps => ps.Add(p => p.Timeline, BmotionScrollTimeline.View()));

        Assert.AreEqual(1, ctx.Interop.CountOf("cancelScrollTimelineAnimation"));
        Assert.AreEqual(2, ctx.Interop.CountOf("playScrollTimelineAnimation"));
    }

    [TestMethod]
    public void RerenderingWithAnEquivalentSpec_DoesNotRebind()
    {
        using var ctx = new BmotionTestContext();
        var cut = ctx.Render<Bmotion>(ps => ps
            .Add(p => p.Id, "bar")
            .Add(p => p.Timeline, BmotionScrollTimeline.Page())
            .Add(p => p.Animate, Bm.To(scaleX: new double[] { 0, 1 }))
            .Add(p => p.ChildContent, Div()));

        // A freshly constructed but identical spec is what a razor attribute produces every render.
        cut.Render(ps => ps.Add(p => p.Timeline, BmotionScrollTimeline.Page()));

        Assert.AreEqual(1, ctx.Interop.CountOf("playScrollTimelineAnimation"));
        Assert.AreEqual(0, ctx.Interop.CountOf("cancelScrollTimelineAnimation"));
    }

    [TestMethod]
    public void DroppingTheTimeline_DetachesItAndReplaysTheTargetOverTime()
    {
        using var ctx = new BmotionTestContext();
        var cut = ctx.Render<Bmotion>(ps => ps
            .Add(p => p.Id, "bar")
            .Add(p => p.Timeline, BmotionScrollTimeline.Page())
            .Add(p => p.Animate, Bm.To(scaleX: new double[] { 0, 1 }))
            .Add(p => p.ChildContent, Div()));

        cut.Render(ps => ps.Add(p => p.Timeline, (BmotionScrollTimeline?)null));

        Assert.AreEqual(1, ctx.Interop.CountOf("cancelScrollTimelineAnimation"));
        // The target now belongs to the ordinary engine again.
        Assert.AreEqual(1, ctx.Interop.CountOf("playWaapiAnimation"));
    }

    // ── Reduced motion ────────────────────────────────────────────────────────

    private static IRenderedComponent<Bmotion> RenderReduced(
        BmotionTestContext ctx, BmotionReducedMotionMode mode, BmotionReducedMotionBehavior behavior)
    {
        ctx.Options.ReducedMotion = mode;
        return ctx.Render<Bmotion>(ps => ps
            .Add(p => p.Id, "bar")
            .Add(p => p.Timeline, BmotionScrollTimeline.Page())
            .Add(p => p.Animate, Bm.To(scaleX: new double[] { 0, 1 }, opacity: new double[] { 0, 1 }))
            .Add(p => p.ReducedMotion, behavior)
            .Add(p => p.ChildContent, Div()));
    }

    [TestMethod]
    public void UnderReducedMotion_AnInstantElement_GivesUpItsScrollAnimation()
    {
        using var ctx = new BmotionTestContext();
        RenderReduced(ctx, BmotionReducedMotionMode.Always, BmotionReducedMotionBehavior.Instant);

        Assert.AreEqual(0, ctx.Interop.CountOf("playScrollTimelineAnimation"), "neither the transform nor the fade follows the scroll");
        Assert.AreEqual(0, ctx.Interop.CountOf("playWaapiAnimation"), "the target lands at once");
    }

    [TestMethod]
    public void UnderReducedMotion_AFullElement_KeepsItsScrollAnimation()
    {
        using var ctx = new BmotionTestContext();
        RenderReduced(ctx, BmotionReducedMotionMode.Always, BmotionReducedMotionBehavior.Full);

        Assert.AreEqual(1, ctx.Interop.CountOf("playScrollTimelineAnimation"));
    }

    [TestMethod]
    public void WithoutReducedMotion_AnInstantElement_KeepsItsScrollAnimation()
    {
        using var ctx = new BmotionTestContext();
        RenderReduced(ctx, BmotionReducedMotionMode.Never, BmotionReducedMotionBehavior.Instant);

        Assert.AreEqual(1, ctx.Interop.CountOf("playScrollTimelineAnimation"));
    }

    [TestMethod]
    public void ChangingReducedMotion_DetachesAndReattachesTheScrollAnimation()
    {
        using var ctx = new BmotionTestContext();
        var cut = RenderReduced(ctx, BmotionReducedMotionMode.Always, BmotionReducedMotionBehavior.Full);

        cut.Render(ps => ps.Add(p => p.ReducedMotion, BmotionReducedMotionBehavior.Instant));
        Assert.AreEqual(1, ctx.Interop.CountOf("cancelScrollTimelineAnimation"));

        cut.Render(ps => ps.Add(p => p.ReducedMotion, BmotionReducedMotionBehavior.Full));
        Assert.AreEqual(2, ctx.Interop.CountOf("playScrollTimelineAnimation"));
    }

    [TestMethod]
    public void Disposing_ReleasesTheTimeline()
    {
        using var ctx = new BmotionTestContext();
        var host = RenderInHost(ctx);

        // Unmount it through a render diff, the way an app's @if removes it.
        Unmount(host);

        var cancel = ctx.Interop.ArgsOf("cancelScrollTimelineAnimation").Single();
        var play = ctx.Interop.ArgsOf("playScrollTimelineAnimation").Single();
        Assert.AreEqual("bar", cancel[0]);
        Assert.AreEqual(play[1], cancel[1], "the cancel must name the token of the timeline that was attached");
    }
}
