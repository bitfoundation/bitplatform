namespace Bit.Bmotion.Tests.E2E;

/// <summary>The <c>&lt;Bmotion&gt;</c> element: enter animations, re-animating on change, value shapes and injection.</summary>
[TestClass]
public sealed class BasicsTests : BmotionPageTest
{
    protected override string Route => "basics";

    [TestMethod]
    public async Task EnterAnimation_PlaysFromInitialToAnimate_AndReportsStartThenComplete()
    {
        await ExpectEventAsync("enter:complete");
        await WaitForStateAsync("#enter", "s => Math.abs(s.opacity - 1) < 0.01 && Math.abs(s.x) < 0.5");

        var log = await LoggedAsync();
        Assert.AreEqual(1, log.Count(e => e == "enter:start"));
        Assert.IsTrue(Array.IndexOf(log, "enter:start") < Array.IndexOf(log, "enter:complete"), string.Join(", ", log));
    }

    [TestMethod]
    public async Task InitialFalse_MountsAlreadyAtTheAnimateState()
    {
        // Its transition is five seconds long: arriving well inside that proves nothing was animated.
        await WaitForStateAsync("#no-initial", "s => Math.abs(s.opacity - 0.5) < 0.01 && Math.abs(s.x - 50) < 0.5", timeout: 2_000);
    }

    [TestMethod]
    public async Task NoInitial_AnimatesFromTheElementsNaturalState()
    {
        await ExpectEventAsync("natural:complete");
        await WaitForStateAsync("#natural", "s => Math.abs(s.scaleX - 0.5) < 0.01");
    }

    [TestMethod]
    public async Task ChangingAnimate_AnimatesThroughIntermediateValuesToTheNewTarget()
    {
        // Its mount animation (to x 0, where it already is) completes first.
        await ExpectEventAsync("toggle:complete");
        await ClickAsync("toggle-btn");
        var samples = await SampleAsync("#toggle", 200);
        await WaitForStateAsync("#toggle", "s => Math.abs(s.x - 120) < 0.5");
        await ExpectEventAsync("toggle:complete", 2);

        Assert.IsTrue(samples.Any(s => s.X is > 1 and < 119), $"No intermediate x while animating: {string.Join(" | ", samples.Select(s => s.X))}");

        await ClickAsync("toggle-btn");
        await WaitForStateAsync("#toggle", "s => Math.abs(s.x) < 0.5");
        await ExpectEventAsync("toggle:complete", 3);
    }

    [TestMethod]
    public async Task ReRenderWithAnEqualTarget_DoesNotRestartTheAnimation()
    {
        await ExpectEventAsync("stable:start");
        for (var i = 0; i < 3; i++) await ClickAsync("rerender-btn");
        await Assertions.Expect(Page.Locator("#rerender-btn")).ToContainTextAsync("(3)");

        await ExpectEventAsync("stable:start", 1);
    }

    [TestMethod]
    public async Task Keyframes_PassThroughEveryFrameAndSettleOnTheLast()
    {
        await ClickAsync("play");
        var samples = await SampleAsync("#keyframes", 700);
        await ExpectEventAsync("keyframes:complete");
        await WaitForStateAsync("#keyframes", "s => Math.abs(s.scaleX - 1) < 0.01");

        Assert.IsTrue(samples.Max(s => s.ScaleX) > 1.4, $"Never reached the middle keyframe: max scale {samples.Max(s => s.ScaleX)}");
    }

    [TestMethod]
    public async Task RepeatForever_KeepsAnimating()
    {
        var samples = await SampleAsync("#forever", 600);

        Assert.IsTrue(samples.Select(s => Math.Round(s.Rotate)).Distinct().Count() > 3, "The endless rotation is not moving.");
    }

    [TestMethod]
    public async Task ColorTween_InterpolatesOnTheFrameLoop_AndLandsOnTheTargetEverywhere()
    {
        await ClickAsync("play");
        var samples = await SampleAsync("#color", 700);
        await ExpectEventAsync("color:complete");
        await WaitForStateAsync("#color", "s => s.backgroundColor === 'rgb(0, 0, 255)'");

        var intermediate = samples.Select(s => s.BackgroundColor).Where(c => c is not "rgb(255, 0, 0)" and not "rgb(0, 0, 255)").ToArray();
        if (FrameLoop)
            Assert.IsTrue(intermediate.Length > 0, "A colour tween should pass through intermediate colours on WebAssembly.");
        else
            // Blazor Server has no per-frame loop: a colour change is documented to land at once.
            Assert.AreEqual(0, intermediate.Length, $"Unexpected intermediate colours on Server: {string.Join(", ", intermediate)}");
    }

    [TestMethod]
    public async Task DimensionTween_LandsOnTheTargetSize()
    {
        await ClickAsync("play");
        var samples = await SampleAsync("#size", 400);
        await ExpectEventAsync("size:complete");
        await WaitForStateAsync("#size", "s => Math.abs(s.width - 200) < 0.5");

        if (FrameLoop)
            Assert.IsTrue(samples.Any(s => s.Width is > 61 and < 199), "A width tween should pass through intermediate widths on WebAssembly.");
    }

    [TestMethod]
    public async Task Spring_OvershootsItsTargetThenSettlesOnIt()
    {
        await ClickAsync("play");
        var samples = await SampleAsync("#spring", 900);
        await ExpectEventAsync("spring:complete", timeout: 10_000);
        await WaitForStateAsync("#spring", "s => Math.abs(s.x - 100) < 0.5");

        Assert.IsTrue(samples.Max(s => s.X) > 102, $"An underdamped spring should overshoot: max x {samples.Max(s => s.X)}");
    }

    [TestMethod]
    public async Task CssAndCssVars_ReachTheInlineStyle()
    {
        await ClickAsync("play");
        await ExpectEventAsync("css:complete");

        var values = await Page.Locator("#css").EvaluateAsync<string[]>(
            "el => [el.style.getPropertyValue('--harness-gap').trim(), getComputedStyle(el).borderTopWidth]");
        Assert.AreEqual("12px", values[0]);
        Assert.AreEqual("4px", values[1]);
    }

    [TestMethod]
    public async Task OnUpdate_ReportsFramesWhileTheFrameLoopRuns()
    {
        await ClickAsync("play");
        await ExpectEventAsync("onupdate:complete");
        await WaitForStateAsync("#onupdate", "s => Math.abs(s.x - 60) < 0.5");

        if (FrameLoop)
            Assert.IsTrue(Number(await ReadoutAsync("onupdate-frames")) > 0, "OnUpdate never fired during an animation on WebAssembly.");
    }

    [TestMethod]
    public async Task AuthoredIdStyleAndAttributes_AreKept_AndTheIdBecomesTheEngineId()
    {
        var el = Page.Locator("#authored");
        await Assertions.Expect(el).ToHaveAttributeAsync("data-bmid", "authored");
        await Assertions.Expect(el).ToHaveAttributeAsync("data-extra", "kept");
        await WaitForStateAsync("#authored", "s => Math.abs(s.opacity - 0.7) < 0.01");

        Assert.AreEqual("rgb(0, 128, 0)", await el.EvaluateAsync<string>("el => getComputedStyle(el).borderTopColor"));
        Assert.AreEqual("3px", await el.EvaluateAsync<string>("el => getComputedStyle(el).borderTopWidth"));
    }

    [TestMethod]
    public async Task IdParameter_WinsOverAnAuthoredId()
    {
        await Assertions.Expect(Page.Locator("#param-id")).ToHaveAttributeAsync("data-bmid", "param-id");
        await Assertions.Expect(Page.Locator("#authored-but-ignored")).ToHaveCountAsync(0);
    }

    [TestMethod]
    public async Task NoId_GetsAGeneratedEngineId_AndExtraRootsRenderUnchanged()
    {
        var el = Page.Locator("#generated-wrap .generated");
        await Assertions.Expect(el).ToHaveAttributeAsync("data-bmid", new System.Text.RegularExpressions.Regex("^bm-"));
        Assert.AreEqual(await el.GetAttributeAsync("id"), await el.GetAttributeAsync("data-bmid"));
        await Assertions.Expect(Page.Locator("#generated-wrap .second-root")).ToHaveTextAsync("second root");
        await Assertions.Expect(Page.Locator("#generated-wrap .second-root")).Not.ToHaveAttributeAsync("data-bmid", new System.Text.RegularExpressions.Regex(".*"));
    }

    [TestMethod]
    public async Task TransformTemplate_KeepsItsAuthoredPartInFrontOfTheAnimatedOne()
    {
        await ExpectEventAsync("template:complete");
        await WaitForStateAsync("#template", "s => Math.abs(s.x - 100) < 0.5 && Math.abs(s.y - 10) < 0.5");
    }

    [TestMethod]
    public async Task SvgPathDrawing_InjectsPathLength_AndDrawsTheStroke()
    {
        var path = Page.Locator("#svg-path");
        await Assertions.Expect(path).ToHaveAttributeAsync("pathLength", "1");
        await ExpectEventAsync("svg:complete");

        await Assertions.Expect(path).ToHaveCSSAsync("stroke-dashoffset", "0px");
    }

    [TestMethod]
    public async Task CallbacksReRenderAnOrdinaryComponent_OnTheRenderersDispatcher()
    {
        // 1 start + 1 complete from the enter animation; each click is one more of each.
        await ExpectReadoutAsync("probe-counts", "1/1/0");
        for (var i = 2; i <= 5; i++)
        {
            await ClickAsync("probe-btn");
            await ExpectReadoutAsync("probe-counts", $"{i}/{i}/0");
        }

        await HoverAsync("#probe");
        await ExpectReadoutAsync("probe-counts", "5/5/1");
    }
}
