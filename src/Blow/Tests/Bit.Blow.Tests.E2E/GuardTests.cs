using Bit.Blow.Tests.E2E.Infrastructure;
using Microsoft.Playwright;

namespace Bit.Blow.Tests.E2E;

/// <summary>
/// The decisions that can only be made while a DOM event is still being dispatched, which is why they live in
/// the browser layer: the <c>nodrag</c>, <c>nopan</c> and <c>nowheel</c> escape hatches, and whether a wheel
/// over the flow may scroll the page.
/// </summary>
[TestClass]
public sealed class GuardTests : BlowTest
{
    [TestMethod]
    public async Task A_drag_inside_a_nodrag_area_works_the_control_and_leaves_the_node_in_place()
    {
        var page = await OpenAsync("/examples/interaction-guards");
        var before = await NodeBoxAsync(page, "controls");

        var slider = await BoxAsync(page, ".guard-body input[type=range]");
        await DragAsync(page, new Point(slider.X + 10, slider.Centre.Y), new Point(slider.X + slider.Width * 0.7, slider.Centre.Y + 25));

        Assert.AreEqual(before, await NodeBoxAsync(page, "controls"), "dragging the slider moved its node");
        Assert.AreNotEqual("40", await page.Locator(".guard-value").TextContentAsync(), "the slider itself did not move");
        Assert.IsEmpty(await SelectedNodesAsync(page), "a press in a nodrag area reached the node");

        // The title is outside the nodrag area and drags the node as usual.
        var title = await BoxAsync(page, ".guard-title");
        await DragAsync(page, title.Centre, title.Centre.Offset(40, 25));
        var after = await NodeBoxAsync(page, "controls");
        AssertNear(new Point(before.X + 40, before.Y + 25), new Point(after.X, after.Y), 0.5, "node after a drag by its title");
    }

    [TestMethod]
    public async Task The_wheel_over_a_nowheel_element_scrolls_it_instead_of_zooming_the_flow()
    {
        var page = await OpenAsync("/examples/interaction-guards");
        var before = await ViewportAsync(page);

        var log = await BoxAsync(page, ".guard-log");
        await WheelAsync(page, log.Centre, 120);

        Assert.AreEqual(before, await ViewportAsync(page), "a wheel over the nowheel list zoomed the flow");
        Assert.IsTrue(await page.EvaluateAsync<double>("() => document.querySelector('.guard-log').scrollTop") > 0,
            "the nowheel list did not scroll");

        // The same wheel over the node's title, outside the nowheel element, does zoom.
        var title = await BoxAsync(page, ".bit-blow-node[data-id=\"log\"] .guard-title");
        await WheelAsync(page, title.Centre, -120);
        Assert.IsTrue((await ViewportAsync(page)).Zoom > before.Zoom, "a wheel over the rest of the node did not zoom");
    }

    [TestMethod]
    public async Task Dragging_a_nopan_overlay_does_not_pan_the_flow()
    {
        var page = await OpenAsync("/examples/interaction-guards");
        var before = await ViewportAsync(page);

        var panel = await BoxAsync(page, ".guard-panel");
        await DragAsync(page, panel.Centre, panel.Centre.Offset(-120, 80));
        Assert.AreEqual(before, await ViewportAsync(page), "dragging the nopan panel panned the flow");

        // While a drag on the background beside it does.
        var pane = await PaneBoxAsync(page);
        await DragAsync(page, new Point(pane.X + 40, pane.Y + 40), new Point(pane.X + 140, pane.Y + 100));
        var after = await ViewportAsync(page);
        AssertNear(before.X + 100, after.X, 0.5, "viewport x after a background drag");
    }

    [TestMethod]
    public async Task A_wheel_over_the_flow_never_scrolls_the_page_but_one_over_its_controls_does()
    {
        var page = await OpenAsync("/");
        Assert.IsTrue(await page.EvaluateAsync<bool>("() => document.documentElement.scrollHeight > window.innerHeight + 40"),
            "the page is not tall enough to scroll");
        await page.EvaluateAsync("() => window.scrollTo(0, 0)");
        await page.WaitForTimeoutAsync(250);

        // Over the flow the wheel zooms, and the browser layer cancels the page scroll it would also cause.
        var pane = await PaneBoxAsync(page);
        var before = await ViewportAsync(page);
        await WheelAsync(page, pane.Centre, 100);
        Assert.AreNotEqual(before.Zoom, (await ViewportAsync(page)).Zoom, "the wheel did not zoom the flow");
        Assert.AreEqual(0, await page.EvaluateAsync<double>("() => window.scrollY"), "a wheel the flow used also scrolled the page");

        // The controls are page chrome: a wheel over them scrolls the page and leaves the flow alone.
        var zoomed = await ViewportAsync(page);
        await WheelAsync(page, (await BoxAsync(page, ".bit-blow-controls-button")).Centre, 100);
        await EventuallyAsync(async () => await page.EvaluateAsync<double>("() => window.scrollY") > 0, "the wheel over the controls scrolled the page");
        Assert.AreEqual(zoomed, await ViewportAsync(page), "a wheel over the controls zoomed the flow");
    }
}
