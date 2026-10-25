using System.Reflection;
using Bit.Bmotion.Tests.TestInfra;
using Bunit;
using Microsoft.AspNetCore.Components;

namespace Bit.Bmotion.Tests.Components;

/// <summary>
/// <see cref="BmPreset"/>s: a preset supplies the targets, viewport and transition an element doesn't set itself,
/// anything set on the element wins, presets compose with <c>+</c> property by property, and every built-in one is
/// something the engine plays.
/// </summary>
[TestClass]
public class PresetTests
{
    private static RenderFragment Div => b =>
    {
        b.OpenElement(0, "div");
        b.CloseElement();
    };

    private static Dictionary<string, object>[] LastKeyframes(BmotionTestContext ctx)
        => (Dictionary<string, object>[])ctx.Interop.ArgsOf("playWaapiAnimation").Last()[2]!;

    private static Dictionary<string, object?> LastTiming(BmotionTestContext ctx)
        => (Dictionary<string, object?>)ctx.Interop.ArgsOf("playWaapiAnimation").Last()[3]!;

    [TestMethod]
    public void APreset_SuppliesTheInitialState_AndTheEnter()
    {
        using var ctx = new BmotionTestContext();
        var cut = ctx.Render<Bmotion>(ps => ps
            .Add(p => p.Id, "box")
            .Add(p => p.Preset, BmPresets.Fade(duration: 0.4))
            .Add(p => p.ChildContent, Div));

        StringAssert.Contains(cut.Find("div").GetAttribute("style"), "opacity:0", "rendered at the preset's Initial");
        Assert.AreEqual("1", LastKeyframes(ctx)[^1]["opacity"], "and enters to its Animate");
        Assert.AreEqual(400.0, (double)LastTiming(ctx)["duration"]!, "with its transition");
    }

    [TestMethod]
    public void TheElementsOwnParameters_WinOverThePresets()
    {
        using var ctx = new BmotionTestContext();
        ctx.Render<Bmotion>(ps => ps
            .Add(p => p.Id, "box")
            .Add(p => p.Preset, BmPresets.Fade(duration: 0.4))
            .Add(p => p.Animate, Bm.To(opacity: 0.5))
            .Add(p => p.Transition, Bm.Tween(0.1))
            .Add(p => p.ChildContent, Div));

        Assert.AreEqual("0.5", LastKeyframes(ctx)[^1]["opacity"]);
        Assert.AreEqual(100.0, (double)LastTiming(ctx)["duration"]!);
    }

    [TestMethod]
    public async Task APresetsGestures_AreWired_AndPlay()
    {
        using var ctx = new BmotionTestContext();
        ctx.Render<Bmotion>(ps => ps
            .Add(p => p.Id, "box")
            .Add(p => p.Preset, BmPresets.HoverLift(lift: 6))
            .Add(p => p.ChildContent, Div));

        var flags = (IReadOnlyDictionary<string, object?>)ctx.Interop.ArgsOf("attachEventListeners").Last()[1]!;
        Assert.AreEqual(true, flags["hover"]);
        Assert.AreEqual(true, flags["tap"]);

        var box = (Bmotion)ctx.Interop.EventListenerRefs["box"];
        await ctx.Renderer.Dispatcher.InvokeAsync(() => box.OnPointerEnter());
        StringAssert.Contains((string)LastKeyframes(ctx)[^1]["transform"], "-6px");
    }

    [TestMethod]
    public void AReveal_ObservesTheViewport_WithItsOwnOptions()
    {
        using var ctx = new BmotionTestContext();
        ctx.Render<Bmotion>(ps => ps
            .Add(p => p.Id, "box")
            .Add(p => p.Preset, BmPresets.Reveal(amount: 0.5))
            .Add(p => p.ChildContent, Div));

        var options = (BmViewport)ctx.Interop.ArgsOf("observeViewport").Last()[1]!;
        Assert.IsTrue(options.Once);
        Assert.AreEqual("0.5", options.Amount);
    }

    [TestMethod]
    public void Presets_ComposeTargetByTarget_AndTheRightHandTransitionWins()
    {
        var combined = BmPresets.Fade(duration: 0.2) + BmPresets.Scale(from: 0.8) with { Transition = Bm.Spring() };

        Assert.AreEqual(0, combined.Initial!.Opacity!.Last);
        Assert.AreEqual(0.8, combined.Initial.Scale!.Last, "the fade fades and the scale scales");
        Assert.IsInstanceOfType<BmSpring>(combined.Transition);
    }

    [TestMethod]
    public void PropsWith_LayersTheRightHandSide_AndLeavesBothInputsAlone()
    {
        var under = Bm.To(opacity: 0, x: 10, css: new() { ["gap"] = "1rem" });
        var over = Bm.To(x: 20, y: 5, css: new() { ["margin"] = "2px" });

        var merged = under + over;

        Assert.AreEqual(0, merged.Opacity!.Last);
        Assert.AreEqual(20, merged.X!.Last, "the right-hand side wins where both set it");
        Assert.AreEqual(5, merged.Y!.Last);
        CollectionAssert.AreEquivalent(new[] { "gap", "margin" }, merged.Css!.Keys.ToArray());
        Assert.AreEqual(10, under.X!.Last, "neither input is changed");
        Assert.IsNull(under.Y);
    }

    [TestMethod]
    public void Merge_CoversEveryProperty()
    {
        // A property added to BmProps but forgotten in Merge would silently vanish from composed presets.
        var full = new BmProps();
        foreach (var property in typeof(BmProps).GetProperties(BindingFlags.Public | BindingFlags.Instance).Where(p => p.CanWrite))
            property.SetValue(full, SampleOf(property.PropertyType));

        var merged = BmProps.Merge(new BmProps(), full)!;

        foreach (var property in typeof(BmProps).GetProperties(BindingFlags.Public | BindingFlags.Instance).Where(p => p.CanWrite))
            Assert.IsNotNull(property.GetValue(merged), $"BmProps.Merge drops {property.Name}");
    }

    private static object SampleOf(Type type)
    {
        if (type == typeof(BmKeyframes)) return (BmKeyframes)1.0;
        if (type == typeof(BmStringKeyframes)) return (BmStringKeyframes)"1px";
        if (type == typeof(double?)) return 0.5;
        if (type == typeof(Dictionary<string, string>)) return new Dictionary<string, string> { ["--a"] = "1" };
        if (type == typeof(Dictionary<string, BmStringKeyframes>)) return new Dictionary<string, BmStringKeyframes> { ["gap"] = "1px" };
        if (type == typeof(BmTransition)) return Bm.Tween(0.1);
        throw new AssertFailedException($"Add a sample for {type.Name} to Merge_CoversEveryProperty.");
    }

    // Every factory with its defaults: each must render and start without the engine rejecting anything.
    public static IEnumerable<object[]> BuiltIns()
        => typeof(BmPresets).GetMethods(BindingFlags.Public | BindingFlags.Static)
            .Where(m => m.ReturnType == typeof(BmPreset))
            .Select(m => new object[] { m.Name, (BmPreset)m.Invoke(null, m.GetParameters().Select(p => p.DefaultValue).ToArray())! });

    [TestMethod]
    [DynamicData(nameof(BuiltIns))]
    public async Task EveryBuiltInPreset_RendersAndPlays(string name, BmPreset preset)
    {
        foreach (var server in new[] { false, true })
        {
            using var ctx = new BmotionTestContext();
            ctx.Interop.IsInProcess = !server;
            var cut = ctx.Render<Bmotion>(ps => ps
                .Add(p => p.Id, "box")
                .Add(p => p.Preset, preset)
                .Add(p => p.ChildContent, Div));
            // Gesture listeners are only attached to an element with gestures; the viewport has its own reference.
            if (ctx.Interop.EventListenerRefs.TryGetValue("box", out var listener))
            {
                var box = (Bmotion)listener;
                if (preset.WhileHover is not null) await cut.InvokeAsync(() => box.OnPointerEnter());
                if (preset.WhileTap is not null) await cut.InvokeAsync(() => box.OnPointerDown());
            }
            if (preset.WhileInView is not null)
                await cut.InvokeAsync(() => ((Bmotion)ctx.Interop.ViewportRefs["box"]).OnIntersect(true));

            var played = ctx.Interop.CountOf("playWaapiAnimation") + ctx.Interop.CountOf("applyStyles");
            Assert.IsTrue(played > 0, $"{name} did nothing on {(server ? "Server" : "WebAssembly")}");
        }
    }
}
