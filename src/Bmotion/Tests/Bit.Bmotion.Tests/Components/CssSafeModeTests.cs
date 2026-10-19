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
