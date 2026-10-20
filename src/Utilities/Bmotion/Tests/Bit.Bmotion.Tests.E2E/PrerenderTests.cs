namespace Bit.Bmotion.Tests.E2E;

/// <summary>
/// Static SSR: exactly what the served HTML (plus bit-bmotion.css) shows on its own - what a visitor sees before
/// the runtime arrives, and all a page that never becomes interactive ever shows. Scripts stay on so the tests can
/// sample frames; nothing on a static page runs Bit.Bmotion's runtime either way.
/// </summary>
[TestClass]
public sealed class PrerenderStaticTests : BmotionPageTest
{
    protected override string Route => "prerender";

    protected override string? RenderMode => HarnessHostKinds.Ssr;

    [TestMethod]
    public async Task TheServedPage_ReportsItIsPrerendering_AndNeverBecomesInteractive()
    {
        await Assertions.Expect(Page.Locator("#caps")).ToHaveAttributeAsync("data-prerendering", "true");
        await Assertions.Expect(Page.Locator("#status")).ToHaveTextAsync("loading");
    }

    [TestMethod]
    public async Task Initial_IsServedAtItsInitialState_AndStaysThere()
    {
        var samples = await SampleAsync("#pr-initial", 600);
        Assert.IsTrue(samples.All(s => s.Opacity < 0.01), "BmPrerenderMode.Initial should serve (and keep) the Initial state.");
    }

    [TestMethod]
    public async Task Final_IsServedAtRest_WithNoAnimation()
    {
        var samples = await SampleAsync("#pr-final", 300);
        Assert.IsTrue(samples.All(s => Math.Abs(s.Opacity - 1) < 0.01 && Math.Abs(s.X) < 0.5), $"Not served at rest: {samples[0]}");
        StringAssert.DoesNotMatch(samples[0].InlineStyle, new System.Text.RegularExpressions.Regex("--bm-a"));
    }

    [TestMethod]
    public async Task Animate_PlaysTheEnterInPureCss()
    {
        StringAssert.Contains((await ProbeAsync("#pr-animate")).InlineStyle, "--bm-");

        var samples = await SampleAsync("#pr-animate", 2_500);
        Assert.IsTrue(samples.Any(s => s.Opacity is > 0.05 and < 0.95 && s.X is < -1 and > -49), "The CSS enter never showed an intermediate frame.");
        Assert.IsTrue(Math.Abs(samples[^1].Opacity - 1) < 0.01 && Math.Abs(samples[^1].X) < 0.5, $"The CSS enter didn't land at rest: {samples[^1]}");
    }

    [TestMethod]
    public async Task Animate_ARepeatingAnimationKeepsPlaying()
    {
        var samples = await SampleAsync("#pr-forever", 500);
        Assert.IsTrue(samples.Select(s => Math.Round(s.Rotate)).Distinct().Count() > 3, "The endless CSS animation isn't playing.");
    }

    [TestMethod]
    public async Task InitialFalse_IsServedAtTheAnimateState()
        => await WaitForStateAsync("#pr-no-initial", "s => Math.abs(s.opacity - 0.5) < 0.01 && Math.abs(s.x - 40) < 0.5");
}

/// <summary>
/// The static page with scripting disabled - a crawler, or a visitor with JavaScript off. Only plain probes here:
/// with scripts off the page runs no animation frames, so nothing that waits on one can be used.
/// </summary>
[TestClass]
public sealed class PrerenderNoScriptTests : BmotionPageTest
{
    protected override string Route => "prerender";

    protected override string? RenderMode => HarnessHostKinds.Ssr;

    protected override void ConfigureContext(BrowserNewContextOptions options) => options.JavaScriptEnabled = false;

    [TestMethod]
    public async Task WhileInView_ShowsTheInViewState()
    {
        // Nothing will ever run the observer, so the stylesheet shows it as it would look in view.
        Assert.AreEqual(1, (await ProbeAsync("#pr-inview")).Opacity, 0.01);
    }

    [TestMethod]
    public async Task FinalAndInitial_AreServedAsTheirModesSay()
    {
        var final = await ProbeAsync("#pr-final");
        Assert.AreEqual(1, final.Opacity, 0.01);
        Assert.AreEqual(0, final.X, 0.5);
        Assert.AreEqual(0, (await ProbeAsync("#pr-initial")).Opacity, 0.01);
    }
}

/// <summary>The same static page without bit-bmotion.css: BmPrerenderMode.Animate degrades to serving at rest.</summary>
[TestClass]
public sealed class PrerenderWithoutStylesheetTests : BmotionPageTest
{
    protected override string Route => "prerender";

    protected override string? RenderMode => HarnessHostKinds.Ssr;

    [TestInitialize]
    public Task ReopenWithoutTheStylesheetAsync() => OpenAsync(Route, "nocss=1");

    [TestMethod]
    public async Task Animate_WithoutTheStylesheet_RendersAtRest()
    {
        await Assertions.Expect(Page.Locator("link[href*='bit-bmotion.css']")).ToHaveCountAsync(0);
        var samples = await SampleAsync("#pr-animate", 300);
        Assert.IsTrue(samples.All(s => Math.Abs(s.Opacity - 1) < 0.01 && Math.Abs(s.X) < 0.5), $"Not at rest without the stylesheet: {samples[0]}");
    }
}

/// <summary>The static page for a visitor whose OS asks for reduced motion.</summary>
[TestClass]
public sealed class PrerenderReducedMotionTests : BmotionPageTest
{
    protected override string Route => "prerender";

    protected override string? RenderMode => HarnessHostKinds.Ssr;

    protected override HarnessProfile Profile => HarnessProfile.Strict; // ReducedMotion = User: the OS decides

    protected override void ConfigureContext(BrowserNewContextOptions options) => options.ReducedMotion = ReducedMotion.Reduce;

    [TestMethod]
    public async Task Animate_UnderReducedMotion_SnapsTheTransform_ButStillFades()
    {
        var samples = await SampleAsync("#pr-animate", 2_500);

        Assert.IsTrue(samples.All(s => Math.Abs(s.X) < 0.5), "The transform animated although the visitor asked for reduced motion.");
        Assert.IsTrue(samples.Any(s => s.Opacity is > 0.05 and < 0.95), "The fade should still play under reduced motion.");
    }
}

/// <summary>
/// The runtime taking over a prerendered page (the interactive render modes that prerender): what was served at
/// rest stays at rest, an Animate element is continued rather than replayed, and Initial elements enter.
/// </summary>
[TestClass]
public sealed class PrerenderTakeoverTests : BmotionPageTest
{
    protected override string Route => "prerender";

    [TestInitialize]
    public void RequirePrerendering()
    {
        if (HarnessHostKinds.Prerenders(RunHost) is false)
            Assert.Inconclusive($"{RunHost} doesn't prerender, so there is nothing to take over.");
    }

    [TestMethod]
    public async Task AnElementServedAtRest_IsTakenOverAtRest_WithoutReplayingItsEnter()
    {
        var samples = await SampleAsync("#pr-final", 1_000);

        Assert.IsTrue(samples.All(s => s.Opacity > 0.99 && Math.Abs(s.X) < 0.5), $"The element left its resting state: {samples.First(s => s.Opacity <= 0.99 || Math.Abs(s.X) >= 0.5)}");
        await ExpectEventAsync("final:start", 0);
    }

    [TestMethod]
    public async Task AnAnimateElement_IsContinuedNotReplayed()
    {
        var samples = await SampleAsync("#pr-animate", 2_500);

        // Never back towards its Initial state: opacity only ever rises, x only ever approaches 0.
        for (var i = 1; i < samples.Count; i++)
        {
            Assert.IsTrue(samples[i].Opacity >= samples[i - 1].Opacity - 0.02, $"Opacity went back from {samples[i - 1].Opacity} to {samples[i].Opacity}.");
            Assert.IsTrue(samples[i].X >= samples[i - 1].X - 1, $"x went back from {samples[i - 1].X} to {samples[i].X}.");
        }
        Assert.IsTrue(Math.Abs(samples[^1].Opacity - 1) < 0.01, $"Didn't land at rest: {samples[^1]}");
        await ExpectEventAsync("animate:start", 0);
    }

    [TestMethod]
    public async Task AnInitialElement_EntersOnceInteractive()
    {
        await ExpectEventAsync("initial:start");
        await WaitForStateAsync("#pr-initial", "s => Math.abs(s.opacity - 1) < 0.01", timeout: 5_000);
    }

    [TestMethod]
    public async Task ARepeatingAnimation_IsRestartedByTheRuntime()
    {
        var samples = await SampleAsync("#pr-forever", 500);
        Assert.IsTrue(samples.Select(s => Math.Round(s.Rotate)).Distinct().Count() > 3, "The endless animation stopped at takeover.");
    }

    [TestMethod]
    public async Task OnceInteractive_CapabilitiesNoLongerReportPrerendering()
        => await Assertions.Expect(Page.Locator("#caps")).ToHaveAttributeAsync("data-prerendering", "false");
}

/// <summary>A host whose global options are all off their defaults (<see cref="HarnessProfile.Strict"/>).</summary>
[TestClass]
public sealed class StrictOptionsTests : BmotionPageTest
{
    protected override string Route => "strict";

    protected override HarnessProfile Profile => HarnessProfile.Strict;

    protected override void ConfigureContext(BrowserNewContextOptions options) => options.ReducedMotion = ReducedMotion.Reduce;

    [TestMethod]
    public async Task GlobalPrerenderAnimate_AppliesToElementsWithoutTheirOwnMode()
    {
        using var client = new HttpClient();
        var html = await client.GetStringAsync($"{Host.BaseUrl}/strict?mode=ssr");

        var element = System.Text.RegularExpressions.Regex.Match(html, "<div[^>]*id=\"global\"[^>]*>").Value;
        StringAssert.Contains(element, "--bm-a");
    }

    [TestMethod]
    public async Task GlobalReducedMotionUser_FollowsTheOsOutsideAnyConfig()
    {
        await ClickAsync("go");
        var samples = await SampleAsync("#unconfigured", 400);

        Assert.IsTrue(Snapped(samples, 100), "ReducedMotion=User ignored the OS preference.");
    }

    [TestMethod]
    public async Task CssSafeModeThrow_RejectsAnInjectableValue_AndAppliesASafeOne()
    {
        await ClickAsync("unsafe");
        await ExpectEventAsync("css:rejected");

        await ClickAsync("safe");
        await ExpectEventAsync("css:applied");
        await WaitForStateAsync("#css-target", "s => s.backgroundColor === 'rgb(0, 0, 255)'");
    }
}

/// <summary>CSS safe mode in Warn: the value is reported, and still applied.</summary>
[TestClass]
public sealed class CssSafeModeWarnTests : BmotionPageTest
{
    protected override string Route => "strict";

    protected override HarnessProfile Profile => HarnessProfile.CssWarn;

    [TestMethod]
    public async Task AnInjectableValue_IsStillApplied()
    {
        await ClickAsync("unsafe");
        await ExpectEventAsync("css:applied");
        await WaitForStateAsync("#css-target", "s => s.backgroundColor === 'rgb(0, 128, 0)'");
    }
}
