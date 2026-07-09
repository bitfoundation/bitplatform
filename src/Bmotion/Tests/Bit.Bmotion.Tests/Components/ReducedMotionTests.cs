using Bit.Bmotion.Tests.TestInfra;
using Bunit;
using Microsoft.AspNetCore.Components;

namespace Bit.Bmotion.Tests.Components;

/// <summary>
/// Tests for the reduced-motion policy (plan item 1.3): the global <see cref="BmReducedMotionMode"/>
/// option, OS-preference probing, and the softer selective reduction (transforms snap, opacity/color
/// still animate).
/// </summary>
[TestClass]
public class ReducedMotionTests
{
    private static RenderFragment Div => b =>
    {
        b.OpenElement(0, "div");
        b.AddAttribute(1, "class", "box");
        b.CloseElement();
    };

    // ── Selective reduction shape ─────────────────────────────────────────────

    [TestMethod]
    public void BuildReducedTransition_SnapsBase_ButRetainsOpacityAndColor()
    {
        var normal = new BmotionTransitionConfig { Type = BmotionTransitionType.Tween, Duration = 0.5 };
        var reduced = Bmotion.BuildReducedTransition(normal);

        // Base is instant → transform/layout/dimension props snap.
        Assert.AreEqual(0, reduced.Duration);
        Assert.IsNotNull(reduced.Properties);
        // Retained visual props animate with the normal transition.
        Assert.AreSame(normal, reduced.Properties["opacity"]);
        Assert.AreSame(normal, reduced.Properties["color"]);
        Assert.AreSame(normal, reduced.Properties["backgroundColor"]);
        // Transform props are NOT retained (they fall back to the instant base).
        Assert.IsFalse(reduced.Properties.ContainsKey("x"));
        Assert.IsFalse(reduced.Properties.ContainsKey("scale"));
    }

    [TestMethod]
    public void BuildReducedTransition_NullNormal_UsesDefaultForRetained()
    {
        var reduced = Bmotion.BuildReducedTransition(null);
        Assert.AreEqual(0, reduced.Duration);
        // A default config matches the engine's null-transition fallback, so opacity still animates.
        Assert.IsNotNull(reduced.Properties!["opacity"]);
    }

    // ── OS-preference probing wiring ──────────────────────────────────────────

    [TestMethod]
    public void UserMode_ProbesOsPreference_WithoutConfig()
    {
        using var ctx = new BmotionTestContext();
        ctx.Options.ReducedMotion = BmReducedMotionMode.User;

        ctx.RenderComponent<Bmotion>(ps => ps
            .Add(p => p.Animate, Bm.To(x: 100))
            .Add(p => p.ChildContent, Div));

        Assert.IsTrue(ctx.Interop.WasCalled("prefersReducedMotion"));
    }

    [TestMethod]
    public void IgnoreUnlessConfigured_DoesNotProbe_WithoutConfig()
    {
        using var ctx = new BmotionTestContext(); // default mode = IgnoreUnlessConfigured

        ctx.RenderComponent<Bmotion>(ps => ps
            .Add(p => p.Animate, Bm.To(x: 100))
            .Add(p => p.ChildContent, Div));

        Assert.IsFalse(ctx.Interop.WasCalled("prefersReducedMotion"));
    }

    // ── Programmatic API honours reduced motion ───────────────────────────────

    [TestMethod]
    public async Task AnimateAsync_ExplicitTransition_RespectsReducedMotion()
    {
        using var ctx = new BmotionTestContext();
        ctx.Options.ReducedMotion = BmReducedMotionMode.Always;
        var cut = ctx.RenderComponent<Bmotion>(ps => ps
            .Add(p => p.Id, "box")
            .Add(p => p.ChildContent, Div));

        // An explicit transition passed to the imperative API must still be reduced (the transform
        // snaps) instead of bypassing ShouldReduceMotion() and running the full 1s tween.
        await cut.InvokeAsync(async () => await cut.Instance.AnimateAsync(Bm.To(x: 100), Bm.Tween(1.0)));

        ctx.Engine.ComputeFrame(0);
        ctx.Engine.ComputeFrame(16);

        var el = ctx.Engine.GetDiagnostics().Single(d => d.Id == "box");
        Assert.AreEqual(100.0, el.Transforms["x"], "reduced motion must snap the transform, not animate it");
    }
}
