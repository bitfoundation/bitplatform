using Bit.Bmotion.Tests.TestInfra;
using Bunit;
using Microsoft.AspNetCore.Components;

namespace Bit.Bmotion.Tests.Components;

/// <summary>
/// <see cref="Bmotion.ReducedMotion"/> / <see cref="BmPreset.ReducedMotion"/>: how one element answers reduced motion -
/// the default reduction, everything at once, or the animation in full.
/// </summary>
[TestClass]
public class ReducedMotionBehaviorTests
{
    private static RenderFragment Div => b =>
    {
        b.OpenElement(0, "div");
        b.CloseElement();
    };

    private static async Task<BmotionTestContext> AnimateAsync(
        BmReducedMotionMode mode, BmReducedMotionBehavior? behavior, BmProps target, BmPreset? preset = null)
    {
        var ctx = new BmotionTestContext();
        ctx.Options.ReducedMotion = mode;
        var cut = ctx.Render<Bmotion>(ps =>
        {
            ps.Add(p => p.Id, "box").Add(p => p.ChildContent, Div);
            if (behavior is { } b) ps.Add(p => p.ReducedMotion, b);
            if (preset is not null) ps.Add(p => p.Preset, preset);
        });
        await cut.InvokeAsync(async () => await cut.Instance.AnimateAsync(target, Bm.Tween(1.0)));
        return ctx;
    }

    private static int BrowserAnimations(BmotionTestContext ctx) => ctx.Interop.CountOf("playWaapiAnimation");

    [TestMethod]
    public async Task ByDefault_AReducedMoveLandsAtOnce_AndAFadeStillPlays()
    {
        using var moved = await AnimateAsync(BmReducedMotionMode.Always, null, Bm.To(x: 100));
        Assert.AreEqual(0, BrowserAnimations(moved), "the move landed");
        Assert.AreEqual(100.0, moved.Engine.GetDiagnostics().Single().Transforms["x"]);

        using var faded = await AnimateAsync(BmReducedMotionMode.Always, null, Bm.To(opacity: 0));
        Assert.AreEqual(1, BrowserAnimations(faded), "the fade played");
    }

    [TestMethod]
    public async Task Full_KeepsTheWholeAnimation_UnderReducedMotion()
    {
        using var ctx = await AnimateAsync(BmReducedMotionMode.Always, BmReducedMotionBehavior.Full, Bm.To(x: 100));

        Assert.AreEqual(1, BrowserAnimations(ctx));
        var timing = (Dictionary<string, object?>)ctx.Interop.ArgsOf("playWaapiAnimation")[0][3]!;
        Assert.AreEqual(1000.0, (double)timing["duration"]!, "the full second, not an instant");
    }

    [TestMethod]
    public async Task Instant_LandsFadesToo_UnderReducedMotion()
    {
        using var ctx = await AnimateAsync(BmReducedMotionMode.Always, BmReducedMotionBehavior.Instant, Bm.To(opacity: 0, x: 50));

        Assert.AreEqual(0, BrowserAnimations(ctx), "nothing played");
        var el = ctx.Engine.GetDiagnostics().Single();
        Assert.AreEqual(0.0, el.NumericValues["opacity"]);
        Assert.AreEqual(50.0, el.Transforms["x"]);
    }

    [TestMethod]
    public async Task Instant_ChangesNothing_WhenMotionIsNotReduced()
    {
        using var ctx = await AnimateAsync(BmReducedMotionMode.Never, BmReducedMotionBehavior.Instant, Bm.To(opacity: 0));

        Assert.AreEqual(1, BrowserAnimations(ctx), "it says how to reduce, not whether to");
    }

    [TestMethod]
    public async Task APresetsBehaviour_Applies_AndTheElementsOwnWins()
    {
        var decorative = BmPresets.Fade() with { ReducedMotion = BmReducedMotionBehavior.Instant };

        using var fromPreset = await AnimateAsync(BmReducedMotionMode.Always, null, Bm.To(opacity: 0.5), decorative);
        Assert.AreEqual(0, BrowserAnimations(fromPreset), "the preset's Instant landed the fade");

        using var overridden = await AnimateAsync(BmReducedMotionMode.Always, BmReducedMotionBehavior.Reduce, Bm.To(opacity: 0.5), decorative);
        Assert.IsTrue(BrowserAnimations(overridden) > 0, "the element's own Reduce keeps the fade (the enter and the call)");
    }

    [TestMethod]
    public void ComposedPresets_KeepTheRightHandBehaviour()
    {
        var a = BmPresets.Fade() with { ReducedMotion = BmReducedMotionBehavior.Instant };
        var b = BmPresets.Scale() with { ReducedMotion = BmReducedMotionBehavior.Full };

        Assert.AreEqual(BmReducedMotionBehavior.Full, (a + b).ReducedMotion);
        Assert.AreEqual(BmReducedMotionBehavior.Instant, (a + BmPresets.Scale()).ReducedMotion, "unset on the right keeps the left's");
    }
}

[TestClass]
public class PresetCompositionTests
{
    [TestMethod]
    public void Composition_CoversEveryProperty()
    {
        // A property added to BmPreset but forgotten in operator + would silently vanish from composed presets.
        var properties = typeof(BmPreset).GetProperties(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance)
            .Where(p => p.CanWrite).ToArray();
        var full = new BmPreset();
        foreach (var property in properties)
            property.SetValue(full, SampleOf(property.PropertyType));

        var composed = new BmPreset() + full;

        foreach (var property in properties)
            Assert.IsNotNull(property.GetValue(composed), $"BmPreset + drops {property.Name}");
    }

    private static object SampleOf(Type type)
    {
        if (type == typeof(BmProps)) return Bm.To(opacity: 1);
        if (type == typeof(BmViewport)) return new BmViewport();
        if (type == typeof(BmTransition)) return Bm.Tween(0.1);
        if (type == typeof(BmReducedMotionBehavior?)) return BmReducedMotionBehavior.Full;
        throw new AssertFailedException($"Add a sample for {type.Name} to Composition_CoversEveryProperty.");
    }

    [TestMethod]
    public void Expand_GrowsToItsNaturalHeight_AndShrinksBackOnExit()
    {
        var expand = BmPresets.Expand();

        Assert.AreEqual("0px", expand.Initial!.Height!.Last);
        Assert.AreEqual("auto", expand.Animate!.Height!.Last);
        Assert.AreEqual("0px", expand.Exit!.Height!.Last);
        Assert.AreEqual(0, expand.Exit.Opacity!.Last);
    }
}
