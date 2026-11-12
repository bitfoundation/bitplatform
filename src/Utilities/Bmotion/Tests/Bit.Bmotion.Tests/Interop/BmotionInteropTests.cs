using Bunit;
using Microsoft.JSInterop;

namespace Bit.Bmotion.Tests.Interop;

/// <summary>
/// Tests for the production <see cref="BmotionInterop"/> against bUnit's JS runtime: what is
/// actually marshalled to <c>bit-bmotion.js</c> for the calls whose argument shapes the module
/// depends on, and that teardown calls never load the module just to release nothing.
/// </summary>
[TestClass]
public class BmotionInteropTests
{
    private const string ModulePath = "./_content/Bit.Bmotion/bit-bmotion.js";

    // A [JSInvokable] target to hand the bridge, standing in for a component.
    private sealed class Target
    {
        [JSInvokable] public void OnIntersect(bool isIntersecting) { }
    }

    private static (BunitContext Ctx, BunitJSModuleInterop Module, BmotionInterop Interop) Create()
    {
        var ctx = new BunitContext();
        ctx.JSInterop.Mode = JSRuntimeMode.Strict;
        var module = ctx.JSInterop.SetupModule(ModulePath);
        return (ctx, module, new BmotionInterop(ctx.JSInterop.JSRuntime));
    }

    private static IDictionary<string, object?> OptionsOf(JSRuntimeInvocation invocation, int index)
        => (IDictionary<string, object?>)invocation.Arguments[index]!;

    [TestMethod]
    public async Task ObserveViewport_WithOptions_MarshalsTheResolvedObserverOptions()
    {
        var (ctx, module, interop) = Create();
        await using var _ = ctx;
        module.SetupVoid("observeViewport", _ => true).SetVoidResult();
        using var target = DotNetObjectReference.Create(new Target());

        await interop.ObserveViewportWithOptionsAsync("box", target, new BmViewport
        {
            Once = true,
            Margin = "0px 0px -20% 0px",
            Amount = "all",
            Root = "#scroller",
        });

        var call = module.VerifyInvoke("observeViewport");
        Assert.AreEqual("box", call.Arguments[0]);
        Assert.AreSame(target, call.Arguments[1], "the reference JS calls back through");
        var options = OptionsOf(call, 2);
        Assert.IsTrue((bool?)options["once"]);
        Assert.AreEqual("0px 0px -20% 0px", options["margin"]);
        Assert.AreEqual(1.0, options["threshold"], "\"all\" lowers to IntersectionObserver's threshold 1");
        Assert.AreEqual("#scroller", options["root"]);
    }

    [TestMethod]
    public async Task ObserveViewport_WithANumericAmount_ClampsTheThreshold()
    {
        var (ctx, module, interop) = Create();
        await using var _ = ctx;
        module.SetupVoid("observeViewport", _ => true).SetVoidResult();
        using var target = DotNetObjectReference.Create(new Target());

        await interop.ObserveViewportWithOptionsAsync("a", target, new BmViewport { Amount = "0.4" });
        await interop.ObserveViewportWithOptionsAsync("b", target, new BmViewport { Amount = "7" });

        var calls = module.Invocations["observeViewport"];
        Assert.AreEqual(0.4, OptionsOf(calls[0], 2)["threshold"]);
        Assert.AreEqual(1.0, OptionsOf(calls[1], 2)["threshold"]);
        Assert.IsNull(OptionsOf(calls[0], 2)["root"], "no root means the browser viewport");
    }

    [TestMethod]
    public async Task ObserveViewport_OnceOverload_MarshalsTheSameOptionShape()
    {
        var (ctx, module, interop) = Create();
        await using var _ = ctx;
        module.SetupVoid("observeViewport", _ => true).SetVoidResult();
        using var target = DotNetObjectReference.Create(new Target());

        await interop.ObserveViewportAsync("box", target, once: true);

        var options = OptionsOf(module.VerifyInvoke("observeViewport"), 2);
        Assert.IsTrue((bool?)options["once"]);
        Assert.AreEqual("0px", options["margin"]);
        Assert.AreEqual(0.0, options["threshold"]);
    }

    [TestMethod]
    public async Task GetBoundingRect_DefaultMeasurement_SendsNoOptionsObject()
    {
        var (ctx, module, interop) = Create();
        await using var _ = ctx;
        module.Setup<BmotionBoundingRect?>("getBoundingRect", _ => true)
            .SetResult(new BmotionBoundingRect { Left = 1, Top = 2, Width = 3, Height = 4 });

        var rect = await interop.GetBoundingRectAsync("box");

        Assert.AreEqual(3, rect!.Width);
        var call = module.VerifyInvoke("getBoundingRect");
        Assert.ContainsSingle(call.Arguments, "the common FLIP path marshals only the element id");
        Assert.AreEqual("box", call.Arguments[0]);
    }

    [TestMethod]
    public async Task GetBoundingRect_WithMeasureOptions_MarshalsBothFlags()
    {
        var (ctx, module, interop) = Create();
        await using var _ = ctx;
        module.Setup<BmotionBoundingRect?>("getBoundingRect", _ => true).SetResult(null);

        await interop.GetBoundingRectAsync("a", new BmotionMeasureOptions(TrackScroll: true, FixedRoot: false));
        await interop.GetBoundingRectAsync("b", new BmotionMeasureOptions(TrackScroll: false, FixedRoot: true));

        var calls = module.Invocations["getBoundingRect"];
        Assert.IsTrue((bool?)OptionsOf(calls[0], 1)["trackScroll"]);
        Assert.IsFalse((bool?)OptionsOf(calls[0], 1)["fixedRoot"]);
        Assert.IsFalse((bool?)OptionsOf(calls[1], 1)["trackScroll"]);
        Assert.IsTrue((bool?)OptionsOf(calls[1], 1)["fixedRoot"]);
    }

    [TestMethod]
    public async Task RafLoop_KeysEachReference_SoAStopFindsTheEngineItsStartRegistered()
    {
        // Blazor revives a new JS wrapper every time it marshals a reference, so the module can't
        // match a stop to its start by the wrapper: it is handed a key that stays the same for
        // the reference across every start and stop, and differs from any other reference's.
        var (ctx, module, interop) = Create();
        await using var _ = ctx;
        module.SetupVoid("startRafLoop", _ => true).SetVoidResult();
        module.SetupVoid("stopRafLoop", _ => true).SetVoidResult();
        using var engine = DotNetObjectReference.Create(new Target());
        using var other = DotNetObjectReference.Create(new Target());

        await interop.StartRafLoopAsync(engine);
        await interop.StopRafLoopAsync(engine);
        await interop.StartRafLoopAsync(engine);
        await interop.StartRafLoopAsync(other);
        await interop.StopRafLoopAsync<Target>();

        var starts = module.Invocations["startRafLoop"];
        var stops = module.Invocations["stopRafLoop"];
        var key = starts[0].Arguments[0] as string;
        Assert.IsFalse(string.IsNullOrEmpty(key));
        Assert.IsFalse(key!.All(char.IsAsciiDigit), "a bare counter would collide with another runtime's on the same page");
        Assert.AreSame(engine, starts[0].Arguments[1], "the reference JS ticks through");
        Assert.AreEqual(key, stops[0].Arguments[0], "the stop names the engine its start registered");
        Assert.ContainsSingle(stops[0].Arguments, "the stop needs only the key");
        Assert.AreEqual(key, starts[1].Arguments[0], "a restart replaces the registration instead of adding one");
        Assert.AreNotEqual(key, starts[2].Arguments[0], "another engine gets a key of its own");
        Assert.IsNull(stops[1].Arguments[0], "no reference still stops every engine");
    }

    [TestMethod]
    public async Task ReducedMotionWatch_KeysTheReference_SoUnwatchFindsTheWatcher()
    {
        var (ctx, module, interop) = Create();
        await using var _ = ctx;
        module.SetupVoid("watchReducedMotion", _ => true).SetVoidResult();
        module.SetupVoid("unwatchReducedMotion", _ => true).SetVoidResult();
        using var engine = DotNetObjectReference.Create(new Target());

        await interop.WatchReducedMotionAsync(engine);
        await interop.UnwatchReducedMotionAsync(engine);

        var watch = module.VerifyInvoke("watchReducedMotion");
        var unwatch = module.VerifyInvoke("unwatchReducedMotion");
        Assert.AreSame(engine, watch.Arguments[1]);
        Assert.AreEqual(watch.Arguments[0], unwatch.Arguments[0]);
        Assert.ContainsSingle(unwatch.Arguments);
    }

    [TestMethod]
    public async Task UnwatchReducedMotion_NullReference_ThrowsLikeWatch()
    {
        var (ctx, _, interop) = Create();
        await using var __ = ctx;

        var ex = await Assert.ThrowsExactlyAsync<ArgumentNullException>(
            async () => await interop.UnwatchReducedMotionAsync<Target>(null!));
        Assert.AreEqual("dotnetRef", ex.ParamName);
    }

    [TestMethod]
    public async Task TeardownCalls_BeforeTheModuleLoaded_NeverImportIt()
    {
        var (ctx, _, interop) = Create();
        await using var __ = ctx;

        // Strict mode would throw on any unplanned call, and the import itself is recorded: releasing
        // things that were never set up must not pay for loading the module.
        await interop.UnobserveViewportAsync("box");
        await interop.UnregisterElementAsync("box");
        await interop.UnobserveScrollAsync("scroll-1");
        await interop.CancelWaapiAnimationAsync("box", 1, commit: true);
        await interop.CancelScrollTimelineAsync("box", 1);
        await interop.StopRafLoopAsync<Target>();
        await interop.DisposeAsync();

        Assert.AreEqual(0, ctx.JSInterop.Invocations.Count);
    }
}
