namespace Bit.Bmotion.Tests.E2E;

/// <summary>WhileInView and OnViewportEnter / OnViewportLeave against the page and against a scroll container.</summary>
[TestClass]
public sealed class InViewTests : BmotionPageTest
{
    protected override string Route => "inview";

    [TestMethod]
    public async Task AnElementVisibleAtLoad_EntersAtOnce()
    {
        await ExpectEventAsync("visible:enter");
        await WaitForStateAsync("#visible", "s => Math.abs(s.opacity - 1) < 0.01");
    }

    [TestMethod]
    public async Task AnElementBelowTheFold_NeitherEntersNorLeavesUntilScrolledTo()
    {
        await ExpectEventAsync("visible:enter");
        // Long enough for every observer to have delivered its initial report.
        await Page.WaitForTimeoutAsync(500);

        await ExpectEventAsync("below:enter", 0);
        // It never was in view, so it can't have left it.
        await ExpectEventAsync("below:leave", 0);
        Assert.AreEqual(0.2, (await ProbeAsync("#below")).Opacity, 0.01);
    }

    [TestMethod]
    public async Task ScrollingIn_AppliesTheOverlay_AndScrollingOut_RevertsIt()
    {
        await Page.Locator("#below").ScrollIntoViewIfNeededAsync();
        await ExpectEventAsync("below:enter");
        await WaitForStateAsync("#below", "s => Math.abs(s.opacity - 1) < 0.01 && Math.abs(s.x) < 0.5");

        await Page.EvaluateAsync("() => window.scrollTo(0, 0)");
        await ExpectEventAsync("below:leave");
        await WaitForStateAsync("#below", "s => Math.abs(s.opacity - 0.2) < 0.01 && Math.abs(s.x + 50) < 0.5");
    }

    [TestMethod]
    public async Task Once_KeepsTheOverlayAfterLeaving()
    {
        await Page.Locator("#once").ScrollIntoViewIfNeededAsync();
        await ExpectEventAsync("once:enter");
        await WaitForStateAsync("#once", "s => Math.abs(s.opacity - 1) < 0.01");

        await Page.EvaluateAsync("() => window.scrollTo(0, 0)");
        await Page.WaitForTimeoutAsync(400);

        Assert.AreEqual(1, (await ProbeAsync("#once")).Opacity, 0.01);
        await ExpectEventAsync("once:enter", 1);
    }

    [TestMethod]
    public async Task AmountAll_WaitsForTheWholeElement()
    {
        // Bring only its top edge into view: 100px of 300.
        var top = await Page.Locator("#all").EvaluateAsync<double>("el => el.getBoundingClientRect().top + window.scrollY");
        await Page.EvaluateAsync("y => window.scrollTo(0, y - 620)", top);
        await Page.WaitForTimeoutAsync(400);
        await ExpectEventAsync("all:enter", 0);

        await Page.EvaluateAsync("y => window.scrollTo(0, y - 200)", top);
        await ExpectEventAsync("all:enter");
        await WaitForStateAsync("#all", "s => Math.abs(s.opacity - 1) < 0.01");
    }

    [TestMethod]
    public async Task ViewportRoot_ObservesAgainstTheScrollContainer()
    {
        await ExpectEventAsync("visible:enter");
        await ExpectEventAsync("rooted:enter", 0);

        await Page.EvaluateAsync("() => document.getElementById('scroller').scrollTop = 500");
        await ExpectEventAsync("rooted:enter");
        await WaitForStateAsync("#rooted", "s => Math.abs(s.opacity - 1) < 0.01");

        await Page.EvaluateAsync("() => document.getElementById('scroller').scrollTop = 0");
        await ExpectEventAsync("rooted:leave");
    }
}
