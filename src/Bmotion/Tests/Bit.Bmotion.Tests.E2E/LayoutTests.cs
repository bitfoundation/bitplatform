namespace Bit.Bmotion.Tests.E2E;

/// <summary>Layout (FLIP) animations and LayoutId shared-element transitions.</summary>
[TestClass]
public sealed class LayoutTests : BmotionPageTest
{
    protected override string Route => "layout";

    /// <summary>
    /// Every element measures its layout only once initialised: a re-render before that has nothing to animate
    /// from. Each announces itself from OnAnimationStart, raised at the end of its initialisation.
    /// </summary>
    [TestInitialize]
    public async Task WaitUntilArmedAsync()
    {
        foreach (var name in new[] { "move", "grow", "both", "dep", "underline0", "ga0" })
            await ExpectEventAsync($"armed:{name}");
    }

    [TestMethod]
    public async Task Layout_AnimatesAMoveCausedByAReRender_FromTheOldSpotToTheNew()
    {
        var before = await ProbeAsync("#move-box");

        await ClickAsync("move");
        var samples = await SampleAsync("#move-box", 300);
        await ExpectEventAsync("move:start");
        await ExpectEventAsync("move:complete");

        var after = await WaitForRestAsync("#move-box");
        Assert.AreEqual(400 - 60, after.Left - before.Left, 1, "The element didn't end up at its new layout position.");
        Assert.AreEqual(0, after.X, 0.5, "The FLIP transform was left on the element.");
        // Visually it travelled: the rendered left edge passed through the space between the two spots.
        Assert.IsTrue(samples.Any(s => s.Left > before.Left + 20 && s.Left < after.Left - 20),
            $"No intermediate position: {string.Join(", ", samples.Select(s => s.Left.ToString("0")))}");
    }

    [TestMethod]
    public async Task LayoutSize_AnimatesTheBoxGrowing()
    {
        await ClickAsync("grow");
        var samples = await SampleAsync("#grow-box", 300);
        await ExpectEventAsync("grow:complete");

        var after = await WaitForRestAsync("#grow-box");
        Assert.AreEqual(200, after.Width, 0.5);
        Assert.AreEqual(1, after.ScaleX, 0.001);
        Assert.IsTrue(samples.Any(s => s.Width is > 70 and < 190), "The size change didn't animate.");
    }

    [TestMethod]
    public async Task AfterALayoutAnimation_TheElementStillShowsItsOwnTransform()
    {
        await ClickAsync("move");
        await ExpectEventAsync("both:layout-complete");
        await WaitForRestAsync("#both-box");

        await ExpectEventAsync("both:complete"); // its mount animation
        await ClickAsync("shift");
        await ExpectEventAsync("both:complete", 2);
        await WaitForStateAsync("#both-box", "s => Math.abs(s.x + 50) < 0.5");
    }

    [TestMethod]
    public async Task ALayoutAnimation_StartsFromTheElementsOwnTransform()
    {
        await ExpectEventAsync("both:complete"); // its mount animation
        await ClickAsync("shift");
        await ExpectEventAsync("both:complete", 2);
        var shifted = await ProbeAsync("#both-box");

        await ClickAsync("move");
        // It travels from where it was - its own x included - rather than jumping 50px when the FLIP starts:
        // no frame moves further than the steady travel does (about 20px a frame here).
        var samples = await SampleAsync("#both-box", 500);
        var jumps = samples.Zip(samples.Skip(1), (a, b) => Math.Abs(b.Left - a.Left)).ToArray();
        Assert.IsTrue(jumps.Length > 0, "No frames sampled.");
        Assert.IsTrue(jumps.All(j => j < 45), $"The layout animation jumped: {string.Join(", ", jumps.Select(j => j.ToString("0")))}");

        await ExpectEventAsync("both:layout-complete");
        var rest = await WaitForRestAsync("#both-box");
        Assert.AreEqual(-50, rest.X, 0.5);
    }

    [TestMethod]
    public async Task ALayoutChangeWhileTheElementIsStillAnimating_EndsWhereThatAnimationEnds()
    {
        await ExpectEventAsync("both:complete"); // its mount animation
        await ClickAsync("shift");
        await Page.WaitForTimeoutAsync(100);
        await ClickAsync("move");

        await ExpectEventAsync("both:layout-complete");
        var rest = await WaitForRestAsync("#both-box");
        Assert.AreEqual(-50, rest.X, 0.5, $"The layout animation lost the x animation it interrupted: {rest}");
    }

    [TestMethod]
    public async Task LayoutDependency_UnrelatedReRendersDontMeasure()
    {
        await ClickAsync("rerender");
        await ClickAsync("rerender");
        await Page.WaitForTimeoutAsync(300);
        await ExpectEventAsync("dep:start", 0);

        await ClickAsync("move");
        await ExpectEventAsync("dep:start");
    }

    [TestMethod]
    public async Task LayoutId_TheSharedElementTravelsFromTheOldTabToTheNew()
    {
        var from = await ProbeAsync("#underline");

        await Page.Locator("#tab2").ClickAsync();
        var samples = await SampleAsync("#underline", 300);
        await ExpectEventAsync("underline:complete");

        var to = await WaitForRestAsync("#underline");
        Assert.IsTrue(to.Left > from.Left + 100, "The underline didn't move to the new tab.");
        Assert.IsTrue(samples.Any(s => s.Left > from.Left + 20 && s.Left < to.Left - 20),
            $"The underline jumped instead of travelling: {string.Join(", ", samples.Select(s => s.Left.ToString("0")))}");
    }

    [TestMethod]
    public async Task LayoutGroups_KeepTheSameLayoutIdApart()
    {
        await Page.Locator("#ga-tab1").ClickAsync();
        await ExpectEventAsync("group-a:start");

        // Group b's marker shares the id "marker" but lives in another group: it never moves.
        await Page.WaitForTimeoutAsync(300);
        await ExpectEventAsync("group-b:start", 0);
    }
}
