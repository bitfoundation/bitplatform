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
        Assert.AreEqual(true, options["once"]);
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
        Assert.AreEqual(true, options["once"]);
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
        Assert.AreEqual(1, call.Arguments.Count, "the common FLIP path marshals only the element id");
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
        Assert.AreEqual(true, OptionsOf(calls[0], 1)["trackScroll"]);
        Assert.AreEqual(false, OptionsOf(calls[0], 1)["fixedRoot"]);
        Assert.AreEqual(false, OptionsOf(calls[1], 1)["trackScroll"]);
        Assert.AreEqual(true, OptionsOf(calls[1], 1)["fixedRoot"]);
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
