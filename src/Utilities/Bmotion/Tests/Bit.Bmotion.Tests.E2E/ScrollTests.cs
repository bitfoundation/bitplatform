namespace Bit.Bmotion.Tests.E2E;

/// <summary>Scroll timelines (page, container, view) and BmotionScrollTracker's progress values and bindings.</summary>
[TestClass]
public sealed class ScrollTests : BmotionPageTest
{
    protected override string Route => "scroll";

    private Task ScrollPageToAsync(double fraction)
        => Page.EvaluateAsync("f => window.scrollTo(0, (document.documentElement.scrollHeight - innerHeight) * f)", fraction);

    [TestMethod]
    public async Task PageTimeline_FollowsThePagesScrollProgress()
    {
        await WaitForStateAsync("#page-progress", "s => s.scaleX < 0.02");

        await ScrollPageToAsync(0.5);
        await WaitForStateAsync("#page-progress", "s => Math.abs(s.scaleX - 0.5) < 0.05");

        await ScrollPageToAsync(1);
        await WaitForStateAsync("#page-progress", "s => s.scaleX > 0.98");

        await ScrollPageToAsync(0);
        await WaitForStateAsync("#page-progress", "s => s.scaleX < 0.02");
    }

    [TestMethod]
    public async Task ContainerTimeline_FollowsThatContainersScroll()
    {
        await WaitForStateAsync("#container-driven", "s => Math.abs(s.opacity - 0.2) < 0.03");

        await Page.EvaluateAsync("() => { const s = document.getElementById('tl-scroller'); s.scrollTop = s.scrollHeight; }");
        await WaitForStateAsync("#container-driven", "s => s.opacity > 0.97");
    }

    [TestMethod]
    public async Task ViewTimeline_FollowsTheElementsJourneyThroughTheViewport()
    {
        // Below the fold it hasn't started; scrolled to the middle of the viewport it is mid-journey.
        await WaitForStateAsync("#view-driven", "s => s.x < 2");

        await Page.EvaluateAsync("() => { const el = document.getElementById('view-driven'); window.scrollTo(0, el.offsetTop - innerHeight / 2); }");
        await WaitForStateAsync("#view-driven", "s => s.x > 60 && s.x < 140");
    }

    [TestMethod]
    public async Task ScrollTracker_ReportsProgress_AndDrivesABoundMotionValue()
    {
        await ExpectEventAsync("tracker:observing");

        await ScrollPageToAsync(0.5);
        await WaitForReadoutAsync("tracker-progress", p => Math.Abs(p - 0.5) < 0.05);
        // x = progress * 300, written straight to the element without a re-render.
        await WaitForStateAsync("#bound", "s => Math.abs(s.x - 150) < 15");

        await ScrollPageToAsync(1);
        await WaitForReadoutAsync("tracker-progress", p => p > 0.98);
        await WaitForStateAsync("#bound", "s => Math.abs(s.x - 300) < 3");
    }

    [TestMethod]
    public async Task ScrollTracker_ReportsATargetsJourneyBetweenItsOffsets()
    {
        await ExpectEventAsync("tracker:observing");

        // "start end" -> "end start": 0 as its top meets the viewport's bottom, 1 as its bottom leaves the top -
        // so with its centre at the viewport's centre it is half-way.
        await Page.EvaluateAsync("() => { const el = document.getElementById('tracked-target'); window.scrollTo(0, el.offsetTop + el.offsetHeight / 2 - innerHeight / 2); }");
        await WaitForReadoutAsync("tracker-target", p => p is > 0.3 and < 0.7);
    }
}
