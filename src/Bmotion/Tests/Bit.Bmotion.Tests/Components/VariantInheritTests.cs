using Bit.Bmotion.Tests.TestInfra;
using Bunit;
using Microsoft.AspNetCore.Components;

namespace Bit.Bmotion.Tests.Components;

/// <summary>
/// Tests for <c>Inherit</c> - motion.dev's <c>inherit</c>. It cuts an element out of an ancestor's
/// variant cascade, one-directionally: the element stops reacting to the label coming down, but
/// anything below it still inherits from the variants it defines itself.
/// </summary>
[TestClass]
public class VariantInheritTests
{
    private static readonly BmVariants _states = new()
    {
        ["hidden"] = Bm.To(opacity: 0),
        ["visible"] = Bm.To(opacity: 1),
    };

    // A parent that publishes an active variant, wrapping one child Bmotion configured by the test.
    private static RenderFragment Tree(bool inherit, string childId) => b =>
    {
        b.OpenComponent<Bmotion>(0);
        b.AddComponentParameter(1, nameof(Bmotion.Id), "parent");
        b.AddComponentParameter(2, nameof(Bmotion.Variants), _states);
        b.AddComponentParameter(3, nameof(Bmotion.State), "visible");
        b.AddComponentParameter(4, nameof(Bmotion.ChildContent), (RenderFragment)(inner =>
        {
            inner.OpenElement(0, "div");
            inner.OpenComponent<Bmotion>(1);
            inner.AddComponentParameter(2, nameof(Bmotion.Id), childId);
            inner.AddComponentParameter(3, nameof(Bmotion.Inherit), inherit);
            inner.AddComponentParameter(4, nameof(Bmotion.ChildContent), (RenderFragment)(leaf =>
            {
                leaf.OpenElement(0, "div");
                leaf.CloseElement();
            }));
            inner.CloseComponent();
            inner.CloseElement();
        }));
        b.CloseComponent();
    };

    // An opacity variant is compositor-eligible, so "did the cascade reach this element" shows up
    // as the engine handing the browser a Web Animation for that element id - observable without
    // a browser through the recorded interop calls.
    private static int AnimationsFor(BmotionTestContext ctx, string id)
        => ctx.Interop.Calls.Count(c => c.Method == "playWaapiAnimation" && (string?)c.Args[0] == id);

    [TestMethod]
    public void Inheriting_Child_PicksUpTheAncestorsActiveVariant()
    {
        using var ctx = new BmotionTestContext();
        ctx.Render(Tree(inherit: true, childId: "child"));

        Assert.AreEqual(1, AnimationsFor(ctx, "child"),
            "the default cascade must reach the child - this is the baseline the opt-out is measured against");
    }

    [TestMethod]
    public void NonInheriting_Child_IgnoresTheAncestorsActiveVariant()
    {
        using var ctx = new BmotionTestContext();
        ctx.Render(Tree(inherit: false, childId: "child"));

        Assert.AreEqual(0, AnimationsFor(ctx, "child"),
            "Inherit=false must cut the element out of the ancestor's variant cascade");
    }

    [TestMethod]
    public void NonInheriting_Child_DoesNotStopTheParentAnimating()
    {
        using var ctx = new BmotionTestContext();
        ctx.Render(Tree(inherit: false, childId: "child"));

        Assert.AreEqual(1, AnimationsFor(ctx, "parent"),
            "the opt-out is the child's alone; the orchestrating parent still plays its own variant");
    }

    [TestMethod]
    public void NonInheriting_Child_StillAnimatesItsOwnAnimateTarget()
    {
        using var ctx = new BmotionTestContext();
        // The same orchestrating parent as the other tests, but the opted-out child carries its own
        // Animate target - one the parent's variants could never produce (opacity 0.5).
        ctx.Render(b =>
        {
            b.OpenComponent<Bmotion>(0);
            b.AddComponentParameter(1, nameof(Bmotion.Id), "parent");
            b.AddComponentParameter(2, nameof(Bmotion.Variants), _states);
            b.AddComponentParameter(3, nameof(Bmotion.State), "visible");
            b.AddComponentParameter(4, nameof(Bmotion.ChildContent), (RenderFragment)(inner =>
            {
                inner.OpenElement(0, "div");
                inner.OpenComponent<Bmotion>(1);
                inner.AddComponentParameter(2, nameof(Bmotion.Id), "solo");
                inner.AddComponentParameter(3, nameof(Bmotion.Inherit), false);
                inner.AddComponentParameter(4, nameof(Bmotion.Animate), (BmTarget?)Bm.To(opacity: 0.5));
                inner.AddComponentParameter(5, nameof(Bmotion.ChildContent), (RenderFragment)(leaf =>
                {
                    leaf.OpenElement(0, "div");
                    leaf.CloseElement();
                }));
                inner.CloseComponent();
                inner.CloseElement();
            }));
            b.CloseComponent();
        });

        // Cutting the inherited link must not disable the element's own animation: exactly one
        // compositor animation for it, heading for its own target rather than the parent's variant.
        var calls = ctx.Interop.Calls.Where(c => c.Method == "playWaapiAnimation" && (string?)c.Args[0] == "solo").ToArray();
        Assert.AreEqual(1, calls.Length);
        var keyframes = (Dictionary<string, object>[])calls[0].Args[2]!;
        Assert.AreEqual("0.5", keyframes[^1]["opacity"]);
    }

    [TestMethod]
    public void Inherit_DefaultsToTrue()
    {
        Assert.IsTrue(new Bmotion().Inherit);
    }
}
