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

    // ── The spec ──────────────────────────────────────────────────────────────

    [TestMethod]
    public void Page_LowersToADocumentScrollTimeline()
    {
        var js = BmScrollTimeline.Page().ToJsObject();

        Assert.AreEqual(false, js["view"]);
        Assert.IsNull(js["selector"]);
        Assert.AreEqual("block", js["axis"]);
    }

    [TestMethod]
    public void Axis_X_LowersToTheInlineAxis()
    {
        Assert.AreEqual("inline", BmScrollTimeline.Page(BmScrollAxis.X).ToJsObject()["axis"]);
        Assert.AreEqual("inline", BmScrollTimeline.View(BmScrollAxis.X).ToJsObject()["axis"]);
    }

    [TestMethod]
    public void View_LowersToAViewTimelineCarryingItsRange()
    {
        var js = BmScrollTimeline.View(range: "entry 0% cover 50%").ToJsObject();

        Assert.AreEqual(true, js["view"]);
        Assert.AreEqual("entry 0% cover 50%", js["range"]);
    }

    [TestMethod]
    public void Container_And_ViewOf_RejectAnEmptySelector()
    {
        Assert.ThrowsExactly<ArgumentException>(() => BmScrollTimeline.Container(" "));
        Assert.ThrowsExactly<ArgumentException>(() => BmScrollTimeline.ViewOf(""));
    }

    [TestMethod]
    public void AreEquivalent_ComparesByValue_SoAnInlineSpecIsNotAChange()
    {
        Assert.IsTrue(BmScrollTimeline.AreEquivalent(BmScrollTimeline.Page(), BmScrollTimeline.Page()));
        Assert.IsTrue(BmScrollTimeline.AreEquivalent(
            BmScrollTimeline.ViewOf("#hero", range: "cover"), BmScrollTimeline.ViewOf("#hero", range: "cover")));
        Assert.IsFalse(BmScrollTimeline.AreEquivalent(BmScrollTimeline.Page(), BmScrollTimeline.View()));
        Assert.IsFalse(BmScrollTimeline.AreEquivalent(
            BmScrollTimeline.Page(), BmScrollTimeline.Page(BmScrollAxis.X)));
        Assert.IsFalse(BmScrollTimeline.AreEquivalent(BmScrollTimeline.Page(), null));
    }

    // ── Component wiring ──────────────────────────────────────────────────────

    [TestMethod]
    public void Timeline_BindsTheAnimateTargetToScroll_InsteadOfPlayingItOverTime()
    {
        using var ctx = new BmotionTestContext();
        ctx.Render<Bmotion>(ps => ps
            .Add(p => p.Id, "bar")
            .Add(p => p.Timeline, BmScrollTimeline.Page())
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
            .Add(p => p.Timeline, BmScrollTimeline.View(range: "cover"))
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
            .Add(p => p.Timeline, BmScrollTimeline.View())
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
            .Add(p => p.Timeline, BmScrollTimeline.Page())
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
        var cut = ctx.Render<Bmotion>(ps => ps
            .Add(p => p.Id, "bar")
            .Add(p => p.Timeline, BmScrollTimeline.Page())
            .Add(p => p.Animate, Bm.To(scaleX: new double[] { 0, 1 }))
            .Add(p => p.ChildContent, Div()));

        cut.Instance.DisposeAsync().AsTask().GetAwaiter().GetResult();
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
            .Add(p => p.Timeline, BmScrollTimeline.Page())
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
            .Add(p => p.Timeline, BmScrollTimeline.Page())
            .Add(p => p.Animate, Bm.To(scaleX: new double[] { 0, 1 }))
            .Add(p => p.ChildContent, Div()));

        cut.Render(ps => ps.Add(p => p.Timeline, BmScrollTimeline.View()));

        Assert.AreEqual(1, ctx.Interop.CountOf("cancelScrollTimelineAnimation"));
        Assert.AreEqual(2, ctx.Interop.CountOf("playScrollTimelineAnimation"));
    }

    [TestMethod]
    public void RerenderingWithAnEquivalentSpec_DoesNotRebind()
    {
        using var ctx = new BmotionTestContext();
        var cut = ctx.Render<Bmotion>(ps => ps
            .Add(p => p.Id, "bar")
            .Add(p => p.Timeline, BmScrollTimeline.Page())
            .Add(p => p.Animate, Bm.To(scaleX: new double[] { 0, 1 }))
            .Add(p => p.ChildContent, Div()));

        // A freshly constructed but identical spec is what a razor attribute produces every render.
        cut.Render(ps => ps.Add(p => p.Timeline, BmScrollTimeline.Page()));

        Assert.AreEqual(1, ctx.Interop.CountOf("playScrollTimelineAnimation"));
        Assert.AreEqual(0, ctx.Interop.CountOf("cancelScrollTimelineAnimation"));
    }

    [TestMethod]
    public void DroppingTheTimeline_DetachesItAndReplaysTheTargetOverTime()
    {
        using var ctx = new BmotionTestContext();
        var cut = ctx.Render<Bmotion>(ps => ps
            .Add(p => p.Id, "bar")
            .Add(p => p.Timeline, BmScrollTimeline.Page())
            .Add(p => p.Animate, Bm.To(scaleX: new double[] { 0, 1 }))
            .Add(p => p.ChildContent, Div()));

        cut.Render(ps => ps.Add(p => p.Timeline, (BmScrollTimeline?)null));

        Assert.AreEqual(1, ctx.Interop.CountOf("cancelScrollTimelineAnimation"));
        // The target now belongs to the ordinary engine again.
        Assert.AreEqual(1, ctx.Interop.CountOf("playWaapiAnimation"));
    }

    [TestMethod]
    public void Disposing_ReleasesTheTimeline()
    {
        using var ctx = new BmotionTestContext();
        var cut = ctx.Render<Bmotion>(ps => ps
            .Add(p => p.Id, "bar")
            .Add(p => p.Timeline, BmScrollTimeline.Page())
            .Add(p => p.Animate, Bm.To(scaleX: new double[] { 0, 1 }))
            .Add(p => p.ChildContent, Div()));

        cut.Instance.DisposeAsync().AsTask().GetAwaiter().GetResult();

        Assert.AreEqual(1, ctx.Interop.CountOf("cancelScrollTimelineAnimation"));
    }
}
