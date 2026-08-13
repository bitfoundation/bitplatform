using Bit.Bmotion.Tests.TestInfra;
using Bunit;
using Microsoft.AspNetCore.Components;

namespace Bit.Bmotion.Tests.Components;

/// <summary>
/// Tests for the variant cascade's timing: the child stagger (including <c>from</c> origins and
/// grids, which need a trustworthy child count) and the <c>when</c> ordering flag.
/// </summary>
[TestClass]
public class VariantOrchestrationTests
{
    private static readonly BmVariants _states = new()
    {
        ["hidden"] = Bm.To(opacity: 0),
        ["visible"] = Bm.To(opacity: 1),
    };

    // A container publishing an active variant over `childCount` cascade participants.
    private static RenderFragment Tree(BmTransition transition, int childCount) => b =>
    {
        b.OpenComponent<Bmotion>(0);
        b.AddComponentParameter(1, nameof(Bmotion.Id), "parent");
        b.AddComponentParameter(2, nameof(Bmotion.Variants), _states);
        b.AddComponentParameter(3, nameof(Bmotion.State), "visible");
        b.AddComponentParameter(4, nameof(Bmotion.Transition), transition);
        b.AddComponentParameter(5, nameof(Bmotion.ChildContent), (RenderFragment)(inner =>
        {
            inner.OpenElement(0, "div");
            for (int i = 0; i < childCount; i++)
            {
                inner.OpenComponent<Bmotion>(1 + i);
                inner.AddComponentParameter(100 + i, nameof(Bmotion.Id), $"child{i}");
                inner.AddComponentParameter(200 + i, nameof(Bmotion.Variants), _states);
                inner.AddComponentParameter(300 + i, nameof(Bmotion.ChildContent), (RenderFragment)(leaf =>
                {
                    leaf.OpenElement(0, "div");
                    leaf.CloseElement();
                }));
                inner.CloseComponent();
            }
            inner.CloseElement();
        }));
        b.CloseComponent();
    };

    // The delay the engine was handed for an element, read off the compositor timing it produced.
    private static double DelayOf(BmotionTestContext ctx, string id)
    {
        var call = ctx.Interop.Calls.Last(c => c.Method == "playWaapiAnimation" && (string?)c.Args[0] == id);
        var timing = (IDictionary<string, object?>)call.Args[3]!;
        return Convert.ToDouble(timing["delay"] ?? 0d);
    }

    // ── Child stagger ─────────────────────────────────────────────────────────

    [TestMethod]
    public void FlatStaggerChildren_DelaysEachChildInRenderOrder()
    {
        using var ctx = new BmotionTestContext();
        ctx.Render(Tree(Bm.Tween(0.3, staggerChildren: 0.1), childCount: 4));

        Assert.AreEqual(0, DelayOf(ctx, "child0"), 1);
        Assert.AreEqual(100, DelayOf(ctx, "child1"), 1);
        Assert.AreEqual(200, DelayOf(ctx, "child2"), 1);
        Assert.AreEqual(300, DelayOf(ctx, "child3"), 1);
    }

    [TestMethod]
    public void ChildStagger_FromLast_ReversesTheCascade()
    {
        // From.Last needs the total child count, which is only trustworthy because children claim
        // their slot during the render pass rather than after it. This is that guarantee under test.
        using var ctx = new BmotionTestContext();
        ctx.Render(Tree(
            Bm.Tween(0.3, childStagger: Bm.Stagger(0.1, from: BmStaggerFrom.Last)), childCount: 4));

        Assert.AreEqual(300, DelayOf(ctx, "child0"), 1);
        Assert.AreEqual(200, DelayOf(ctx, "child1"), 1);
        Assert.AreEqual(100, DelayOf(ctx, "child2"), 1);
        Assert.AreEqual(0, DelayOf(ctx, "child3"), 1);
    }

    [TestMethod]
    public void ChildStagger_FromCenter_RadiatesOutward()
    {
        using var ctx = new BmotionTestContext();
        ctx.Render(Tree(
            Bm.Tween(0.3, childStagger: Bm.Stagger(0.1, from: BmStaggerFrom.Center)), childCount: 5));

        // Origin is index 2 of 5; delay grows with distance from it.
        Assert.AreEqual(200, DelayOf(ctx, "child0"), 1);
        Assert.AreEqual(100, DelayOf(ctx, "child1"), 1);
        Assert.AreEqual(0, DelayOf(ctx, "child2"), 1);
        Assert.AreEqual(100, DelayOf(ctx, "child3"), 1);
        Assert.AreEqual(200, DelayOf(ctx, "child4"), 1);
    }

    [TestMethod]
    public void ChildStagger_SupersedesTheFlatInterval()
    {
        using var ctx = new BmotionTestContext();
        ctx.Render(Tree(
            Bm.Tween(0.3, staggerChildren: 1.0, childStagger: Bm.Stagger(0.05)), childCount: 3));

        Assert.AreEqual(50, DelayOf(ctx, "child1"), 1);
    }

    [TestMethod]
    public void DelayChildren_AddsOnTopOfTheStagger()
    {
        using var ctx = new BmotionTestContext();
        ctx.Render(Tree(
            Bm.Tween(0.3, delayChildren: 0.2, childStagger: Bm.Stagger(0.1)), childCount: 3));

        Assert.AreEqual(200, DelayOf(ctx, "child0"), 1);
        Assert.AreEqual(300, DelayOf(ctx, "child1"), 1);
    }

    // ── when ──────────────────────────────────────────────────────────────────

    [TestMethod]
    public void BeforeChildren_HoldsTheChildrenBackByTheContainersDuration()
    {
        using var ctx = new BmotionTestContext();
        ctx.Render(Tree(Bm.Tween(0.5, staggerChildren: 0.1, when: BmWhen.BeforeChildren), childCount: 2));

        Assert.AreEqual(0, DelayOf(ctx, "parent"), 1, "the container itself goes first, undelayed");
        Assert.AreEqual(500, DelayOf(ctx, "child0"), 1);
        Assert.AreEqual(600, DelayOf(ctx, "child1"), 1);
    }

    [TestMethod]
    public void AfterChildren_DelaysTheContainerUntilTheCascadeHasFinished()
    {
        using var ctx = new BmotionTestContext();
        // A container's transition carries orchestration to its children but not its own timing, so
        // these children animate with the engine's default 0.3s tween. The last one starts at its
        // 0.2s stagger slot and therefore finishes at 0.5s - which is what the container waits for.
        ctx.Render(Tree(Bm.Tween(0.4, staggerChildren: 0.1, when: BmWhen.AfterChildren), childCount: 3));

        Assert.AreEqual(0, DelayOf(ctx, "child0"), 1);
        Assert.AreEqual(500, DelayOf(ctx, "parent"), 5);
    }

    [TestMethod]
    public void AfterChildren_TracksTheChildrensActualDurations()
    {
        using var ctx = new BmotionTestContext();
        // The same cascade with a slower child transition must push the container out further:
        // the wait is derived from what the children really do, not from a fixed assumption.
        var slowChild = Bm.Tween(1.0);
        ctx.Render(b =>
        {
            b.OpenComponent<Bmotion>(0);
            b.AddComponentParameter(1, nameof(Bmotion.Id), "parent");
            b.AddComponentParameter(2, nameof(Bmotion.Variants), _states);
            b.AddComponentParameter(3, nameof(Bmotion.State), "visible");
            b.AddComponentParameter(4, nameof(Bmotion.Transition),
                (BmTransition)Bm.Tween(0.2, staggerChildren: 0.1, when: BmWhen.AfterChildren));
            b.AddComponentParameter(5, nameof(Bmotion.ChildContent), (RenderFragment)(inner =>
            {
                inner.OpenElement(0, "div");
                for (int i = 0; i < 2; i++)
                {
                    inner.OpenComponent<Bmotion>(1 + i);
                    inner.AddComponentParameter(100 + i, nameof(Bmotion.Id), $"child{i}");
                    inner.AddComponentParameter(200 + i, nameof(Bmotion.Variants), _states);
                    inner.AddComponentParameter(400 + i, nameof(Bmotion.Transition), (BmTransition)slowChild);
                    inner.AddComponentParameter(300 + i, nameof(Bmotion.ChildContent), (RenderFragment)(leaf =>
                    {
                        leaf.OpenElement(0, "div");
                        leaf.CloseElement();
                    }));
                    inner.CloseComponent();
                }
                inner.CloseElement();
            }));
            b.CloseComponent();
        });

        // Last child: 0.1s slot + 1.0s of its own = 1.1s.
        Assert.AreEqual(1100, DelayOf(ctx, "parent"), 5);
    }

    [TestMethod]
    public void AfterChildren_WithNoChildren_DoesNotDelayTheContainer()
    {
        using var ctx = new BmotionTestContext();
        ctx.Render(Tree(Bm.Tween(0.4, when: BmWhen.AfterChildren), childCount: 0));

        Assert.AreEqual(0, DelayOf(ctx, "parent"), 1);
    }

    [TestMethod]
    public void Together_IsTheDefault_AndDelaysNobody()
    {
        using var ctx = new BmotionTestContext();
        ctx.Render(Tree(Bm.Tween(0.4, staggerChildren: 0.1), childCount: 2));

        Assert.AreEqual(0, DelayOf(ctx, "parent"), 1);
        Assert.AreEqual(0, DelayOf(ctx, "child0"), 1);
    }
}
