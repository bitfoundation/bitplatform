using Bit.Bmotion.Tests.TestInfra;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Bit.Bmotion.Tests.Components;

/// <summary>
/// Tests for <see cref="BmCssSafeMode"/> beyond the single-value cases in <c>CssValidatorTests</c>:
/// <c>Warn</c> logs but still applies, and <c>Throw</c> inspects every frame of a keyframe array as
/// well as the free-form <c>Css</c> and <c>CssVars</c> dictionaries.
/// </summary>
[TestClass]
public class CssSafeModeTests
{
    private const string Injected = "red; } body { display: none";

    private static RenderFragment Div => b =>
    {
        b.OpenElement(0, "div");
        b.CloseElement();
    };

    private static void RenderWith(BmotionTestContext ctx, BmProps initial)
        => ctx.Render<Bmotion>(ps => ps
            .Add(p => p.Id, "box")
            .Add(p => p.Initial, initial)
            .Add(p => p.ChildContent, Div));

    [TestMethod]
    public void WarnMode_LogsTheRejectedValue_ButStillAppliesIt()
    {
        using var ctx = new BmotionTestContext();
        var logger = new ListLogger<Bmotion>();
        ctx.Services.AddSingleton<ILogger<Bmotion>>(logger);
        ctx.Options.CssSafeMode = BmCssSafeMode.Warn;

        var cut = ctx.Render<Bmotion>(ps => ps
            .Add(p => p.Id, "box")
            .Add(p => p.Initial, Bm.To(boxShadow: $"0 0 4px {Injected}"))
            .Add(p => p.ChildContent, Div));

        var warning = logger.Warnings.FirstOrDefault(w => w.Contains("CSS safe mode"));
        Assert.IsNotNull(warning, "Warn mode must log the rejected value");
        StringAssert.Contains(warning, "boxShadow");
        StringAssert.Contains(warning, "'box'", "the warning names the element");
        // Warn is diagnostics only: the value is still written, exactly as with the mode off.
        StringAssert.Contains(cut.Find("div").GetAttribute("style"), Injected);
    }

    [TestMethod]
    public void WarnMode_CleanValues_LogNothing()
    {
        using var ctx = new BmotionTestContext();
        var logger = new ListLogger<Bmotion>();
        ctx.Services.AddSingleton<ILogger<Bmotion>>(logger);
        ctx.Options.CssSafeMode = BmCssSafeMode.Warn;

        RenderWith(ctx, Bm.To(boxShadow: "0 0 4px rgba(0,0,0,.5)", cssVars: new() { ["--accent"] = "#0af" }));

        Assert.IsFalse(logger.Warnings.Any());
    }

    [TestMethod]
    public void ThrowMode_ChecksEveryFrameOfAKeyframeArray()
    {
        using var ctx = new BmotionTestContext();
        ctx.Options.CssSafeMode = BmCssSafeMode.Throw;

        // Only the last frame is malicious: a check of the first (initial) frame alone would miss it.
        Assert.ThrowsExactly<InvalidOperationException>(() =>
            RenderWith(ctx, Bm.To(backgroundColor: new[] { "#ffffff", "#000000", Injected })));
    }

    [TestMethod]
    public void ThrowMode_ChecksTheFreeFormCssDictionary()
    {
        using var ctx = new BmotionTestContext();
        ctx.Options.CssSafeMode = BmCssSafeMode.Throw;

        var thrown = Assert.ThrowsExactly<InvalidOperationException>(() =>
            RenderWith(ctx, Bm.To(css: new() { ["outline"] = Injected })));
        StringAssert.Contains(thrown.Message, "outline");
    }

    [TestMethod]
    public void ThrowMode_ChecksCssVariables()
    {
        using var ctx = new BmotionTestContext();
        ctx.Options.CssSafeMode = BmCssSafeMode.Throw;

        var thrown = Assert.ThrowsExactly<InvalidOperationException>(() =>
            RenderWith(ctx, Bm.To(cssVars: new() { ["--accent"] = "x</style><script>" })));
        StringAssert.Contains(thrown.Message, "--accent");
    }

    [TestMethod]
    public void ThrowMode_ChecksTheInheritedInitialVariant()
    {
        using var ctx = new BmotionTestContext();
        ctx.Options.CssSafeMode = BmCssSafeMode.Throw;

        var parentStates = new BmVariants { ["hidden"] = Bm.To(opacity: 0), ["visible"] = Bm.To(opacity: 1) };
        var childStates = new BmVariants { ["hidden"] = Bm.To(boxShadow: $"0 0 4px {Injected}"), ["visible"] = Bm.To(boxShadow: "0 0 4px #000") };

        var thrown = Assert.ThrowsExactly<InvalidOperationException>(() => ctx.Render<Bmotion>(ps => ps
            .Add(p => p.Id, "parent")
            .Add(p => p.Variants, parentStates)
            .Add(p => p.InitialState, "hidden")
            .Add(p => p.State, "visible")
            .Add(p => p.ChildContent, ChildWith(childStates))));
        StringAssert.Contains(thrown.Message, "boxShadow");
    }

    [TestMethod]
    public void WarnMode_ChecksAVariantTheChildInheritsLater()
    {
        using var ctx = new BmotionTestContext();
        var logger = new ListLogger<Bmotion>();
        ctx.Services.AddSingleton<ILogger<Bmotion>>(logger);
        ctx.Options.CssSafeMode = BmCssSafeMode.Warn;

        var parentStates = new BmVariants { ["visible"] = Bm.To(opacity: 1), ["pressed"] = Bm.To(opacity: 0.5) };
        var childStates = new BmVariants { ["visible"] = Bm.To(boxShadow: "0 0 4px #000"), ["pressed"] = Bm.To(boxShadow: $"0 0 4px {Injected}") };

        var cut = ctx.Render<Bmotion>(ps => ps
            .Add(p => p.Id, "parent")
            .Add(p => p.Variants, parentStates)
            .Add(p => p.State, "visible")
            .Add(p => p.ChildContent, ChildWith(childStates)));
        Assert.IsFalse(logger.Warnings.Any(), "the variant the child starts with is clean");

        cut.Render(ps => ps.Add(p => p.State, "pressed"));

        cut.WaitForAssertion(() => Assert.IsTrue(logger.Warnings.Any(w => w.Contains("boxShadow") && w.Contains("'child'"))));
    }

    private static RenderFragment ChildWith(BmVariants variants) => b =>
    {
        b.OpenElement(0, "div");
        b.OpenComponent<Bmotion>(1);
        b.AddComponentParameter(2, nameof(Bmotion.Id), "child");
        b.AddComponentParameter(3, nameof(Bmotion.Variants), variants);
        b.AddComponentParameter(4, nameof(Bmotion.ChildContent), Div);
        b.CloseComponent();
        b.CloseElement();
    };

    [TestMethod]
    public async Task WarnMode_LogsEachRejectedValueOncePerElement()
    {
        using var ctx = new BmotionTestContext();
        var logger = new ListLogger<Bmotion>();
        ctx.Services.AddSingleton<ILogger<Bmotion>>(logger);
        ctx.Options.CssSafeMode = BmCssSafeMode.Warn;
        int Logged() => logger.Warnings.Count(w => w.Contains("CSS safe mode"));

        // A prerendered element resolves its targets several times on the way to animating them - five checks of this
        // one value - and one value is one warning.
        ctx.Options.Prerender = BmPrerenderMode.Animate;
        var cut = ctx.Render<Bmotion>(ps => ps
            .Add(p => p.Id, "box")
            .Add(p => p.Initial, Bm.To(boxShadow: $"0 0 4px {Injected}"))
            .Add(p => p.Animate, Bm.To(boxShadow: $"0 0 4px {Injected}"))
            .Add(p => p.ChildContent, Div));
        await ctx.RunFramesAsync(0, 100);
        Assert.AreEqual(1, Logged());

        // Rendered again with the same value: nothing new to report.
        cut.Render(ps => ps.Add(p => p.Animate, Bm.To(boxShadow: $"0 0 4px {Injected}")));
        await ctx.RunFramesAsync(100, 200);
        Assert.AreEqual(1, Logged());

        // A different rejected value is reported in its turn.
        cut.Render(ps => ps.Add(p => p.Animate, Bm.To(boxShadow: $"0 0 8px {Injected}")));
        await ctx.RunFramesAsync(200, 300);
        Assert.AreEqual(2, Logged());
    }

    [TestMethod]
    public async Task ThrowMode_ChecksWhatTheAnimateServiceAnimates()
    {
        using var ctx = new BmotionTestContext();
        ctx.Options.CssSafeMode = BmCssSafeMode.Throw;
        // Resolved from the container, as an app injects it: the options reach the service through it.
        var service = ctx.Services.GetRequiredService<BmotionAnimateService>();
        var unsafeTarget = Bm.To(boxShadow: $"0 0 4px {Injected}");

        var bySelector = await Assert.ThrowsExactlyAsync<InvalidOperationException>(() => service.AnimateAsync(".box", unsafeTarget).AsTask());
        StringAssert.Contains(bySelector.Message, "boxShadow");
        await Assert.ThrowsExactlyAsync<InvalidOperationException>(() => service.AnimateAsync(new ElementReference("box-ref"), unsafeTarget).AsTask());

        // Only the last segment is unsafe: none of the sequence may start, and no element is left registered.
        var sequence = new BmSequence().Add(".a", Bm.To(opacity: 0.5)).Add(".b", unsafeTarget);
        await Assert.ThrowsExactlyAsync<InvalidOperationException>(() => service.RunAsync(sequence).AsTask());

        Assert.IsFalse(ctx.Interop.WasCalled("resolveOrRegisterBySelector"), "a rejected target registers no element");
        Assert.IsFalse(ctx.Interop.WasCalled("resolveOrRegisterByRef"), "a rejected target registers no element");
    }

    [TestMethod]
    public async Task WarnMode_TheAnimateServiceLogsTheSelector_ButStillAnimates()
    {
        using var ctx = new BmotionTestContext();
        var logger = new ListLogger<BmotionAnimateService>();
        ctx.Services.AddSingleton<ILogger<BmotionAnimateService>>(logger);
        ctx.Options.CssSafeMode = BmCssSafeMode.Warn;
        var service = ctx.Services.GetRequiredService<BmotionAnimateService>();

        await service.AnimateAsync(".card", Bm.To(boxShadow: $"0 0 4px {Injected}"));

        var warning = logger.Warnings.SingleOrDefault(w => w.Contains("CSS safe mode"));
        Assert.IsNotNull(warning, "Warn mode must log the rejected value");
        StringAssert.Contains(warning, "'.card'", "the warning names the selector");
        Assert.IsTrue(ctx.Interop.WasCalled("resolveOrRegisterBySelector"), "Warn still animates");
        Assert.IsTrue(ctx.Interop.ArgsOf("playWaapiAnimation")
                          .Any(args => ((Dictionary<string, object>[])args[2]!)[^1].TryGetValue("boxShadow", out var shadow) && Equals(shadow, $"0 0 4px {Injected}")),
                      "Warn animates the rejected value as given");
    }

    [TestMethod]
    public async Task OffMode_TheAnimateServiceChecksNothing()
    {
        using var ctx = new BmotionTestContext();
        var service = ctx.Services.GetRequiredService<BmotionAnimateService>();

        await service.AnimateAsync(".card", Bm.To(boxShadow: $"0 0 4px {Injected}"));

        Assert.IsTrue(ctx.Interop.WasCalled("resolveOrRegisterBySelector"));
    }

    // A free-form entry's name is written verbatim too (`{name}:{value};`): a name can inject a declaration of its own
    // with a value that passes.
    [TestMethod]
    [DataRow(false)]
    [DataRow(true)]
    public void ThrowMode_ChecksTheNamesOfFreeFormEntries(bool customProperty)
    {
        using var ctx = new BmotionTestContext();
        ctx.Options.CssSafeMode = BmCssSafeMode.Throw;
        var target = customProperty
            ? Bm.To(cssVars: new() { ["--a:0;background:url(//evil/x);--b"] = "1" })
            : Bm.To(css: new() { ["x;background-image:url(//evil);y"] = "1px" });

        var thrown = Assert.ThrowsExactly<InvalidOperationException>(() => RenderWith(ctx, target));
        StringAssert.Contains(thrown.Message, "property name");
    }

    // The inline style writes a free-form entry and the typed prop it shadows both; the engine's dictionary keeps one.
    [TestMethod]
    public void ThrowMode_ChecksATypedValueAFreeFormEntryShadows()
    {
        using var ctx = new BmotionTestContext();
        ctx.Options.CssSafeMode = BmCssSafeMode.Throw;

        Assert.ThrowsExactly<InvalidOperationException>(() =>
            RenderWith(ctx, Bm.To(backgroundColor: Injected, css: new() { ["backgroundColor"] = "red" })));
    }

    [TestMethod]
    public void WarnMode_QuotesTheRejectedValueEscapedAndCutShort()
    {
        using var ctx = new BmotionTestContext();
        var logger = new ListLogger<Bmotion>();
        ctx.Services.AddSingleton<ILogger<Bmotion>>(logger);
        ctx.Options.CssSafeMode = BmCssSafeMode.Warn;

        RenderWith(ctx, Bm.To(boxShadow: "red\n[info] Admin login ok user=root" + new string('x', 100_000)));

        var warning = logger.Warnings.Single(w => w.Contains("CSS safe mode"));
        Assert.IsFalse(warning.Contains('\n'), "a rejected value can't start a log line of its own");
        StringAssert.Contains(warning, "red\\u000a[info] Admin login ok");
        Assert.IsTrue(warning.Length < 400, $"the value is cut short, not logged whole ({warning.Length} characters)");
    }

    // An element carrying more rejected values than Warn remembers is reported up to the limit and then told about once:
    // forgetting and starting over would report most of them again on every check.
    [TestMethod]
    public async Task WarnMode_PastTheLimit_SaysOnceThatItStopsReporting()
    {
        using var ctx = new BmotionTestContext();
        var logger = new ListLogger<Bmotion>();
        ctx.Services.AddSingleton<ILogger<Bmotion>>(logger);
        ctx.Options.CssSafeMode = BmCssSafeMode.Warn;
        var many = Enumerable.Range(0, BmotionCssWarnings.Max + 6).ToDictionary(i => $"--v{i}", i => $"{i};");
        int Logged() => logger.Warnings.Count(w => w.Contains("CSS safe mode"));

        var cut = ctx.Render<Bmotion>(ps => ps
            .Add(p => p.Id, "box")
            .Add(p => p.Initial, Bm.To(cssVars: many))
            .Add(p => p.Animate, Bm.To(cssVars: many))
            .Add(p => p.ChildContent, Div));
        await ctx.RunFramesAsync(0, 100);
        Assert.AreEqual(BmotionCssWarnings.Max + 1, Logged());
        Assert.AreEqual(1, logger.Warnings.Count(w => w.Contains("not reported")));

        cut.Render(ps => ps.Add(p => p.Animate, Bm.To(cssVars: many)));
        await ctx.RunFramesAsync(100, 200);
        Assert.AreEqual(BmotionCssWarnings.Max + 1, Logged());
    }

    [TestMethod]
    public async Task WarnMode_TheAnimateServiceLogsATargetAnimatedAgainAndAgainOnce()
    {
        using var ctx = new BmotionTestContext();
        var logger = new ListLogger<BmotionAnimateService>();
        ctx.Services.AddSingleton<ILogger<BmotionAnimateService>>(logger);
        ctx.Options.CssSafeMode = BmCssSafeMode.Warn;
        var service = ctx.Services.GetRequiredService<BmotionAnimateService>();

        for (int i = 0; i < 5; i++)
            await service.AnimateAsync(".swatch", Bm.To(boxShadow: $"0 0 4px {Injected}"));

        Assert.AreEqual(1, logger.Warnings.Count(w => w.Contains("CSS safe mode")));
    }

    // Resolving the elements is a round trip (a network one on Blazor Server): a target changed meanwhile must not play
    // what the check never saw.
    [TestMethod]
    public async Task ThrowMode_TheAnimateServicePlaysTheTargetAsChecked_HoweverItChangesMeanwhile()
    {
        var interop = new FakeBmotionInterop { WebAnimationsSupported = false };
        await using var engine = new BmotionAnimationEngine(interop, null, new BitBmotionOptions { CssSafeMode = BmCssSafeMode.Throw });
        var service = new BmotionAnimateService(engine, interop);
        engine.RegisterElement("el", new Dictionary<string, object?> { ["backgroundColor"] = "#000000" });
        var target = Bm.To(backgroundColor: "#ffffff");
        interop.SelectorResolver = _ =>
        {
            target.BackgroundColor = Injected;
            return ["el"];
        };

        await service.AnimateAsync("#el", target, Bm.Tween(0.1, ease: BmEase.Linear));
        for (double t = 0; t <= 300; t += 16) engine.ComputeFrame(t);

        var played = engine.GetDiagnostics().Single(d => d.Id == "el").StringValues["backgroundColor"];
        Assert.IsFalse(played.Contains("display"), played);
    }

    // The element raises OnAnimationStart between checking its target and playing it, and a handler can await.
    [TestMethod]
    public async Task ThrowMode_AnElementPlaysItsTargetAsChecked_WhateverTheStartHandlerChanges()
    {
        using var ctx = new BmotionTestContext();
        ctx.Options.CssSafeMode = BmCssSafeMode.Throw;
        ctx.Interop.WebAnimationsSupported = false;
        var target = Bm.To(backgroundColor: "#ffffff");

        ctx.Render<Bmotion>(ps => ps
            .Add(p => p.Id, "box")
            .Add(p => p.Initial, Bm.To(backgroundColor: "#000000"))
            .Add(p => p.Animate, target)
            .Add(p => p.Transition, Bm.Tween(0.1, ease: BmEase.Linear))
            .Add(p => p.OnAnimationStart, (BmProps? _) => target.BackgroundColor = Injected)
            .Add(p => p.ChildContent, Div));
        await ctx.RunFramesAsync(0, 300);

        var played = ctx.Engine.GetDiagnostics().Single(d => d.Id == "box").StringValues["backgroundColor"];
        Assert.IsFalse(played.Contains("display"), played);
    }

    // A service created by hand on the application's engine (in a factory, say) follows the application's options.
    [TestMethod]
    public async Task ThrowMode_AServiceCreatedOnTheContainersEngine_FollowsItsOptions()
    {
        using var ctx = new BmotionTestContext();
        ctx.Options.CssSafeMode = BmCssSafeMode.Throw;
        var service = new BmotionAnimateService(ctx.Engine, ctx.Interop);

        await Assert.ThrowsExactlyAsync<InvalidOperationException>(() =>
            service.AnimateAsync(".box", Bm.To(boxShadow: $"0 0 4px {Injected}")).AsTask());
    }

    [TestMethod]
    public void ThrowMode_AcceptsCleanArraysAndDictionaries()
    {
        using var ctx = new BmotionTestContext();
        ctx.Options.CssSafeMode = BmCssSafeMode.Throw;

        RenderWith(ctx, Bm.To(
            backgroundColor: new[] { "#fff", "rgb(0, 0, 0)" },
            css: new() { ["outline"] = new[] { "1px solid red", "2px dashed blue" } },
            cssVars: new() { ["--accent"] = "hsl(200 80% 50%)" }));

        Assert.IsTrue(ctx.Interop.WasCalled("registerElement"), "clean values must render normally");
    }
}
