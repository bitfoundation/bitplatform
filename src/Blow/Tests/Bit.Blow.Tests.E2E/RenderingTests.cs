using System.Text.RegularExpressions;
using Bit.Blow.Tests.E2E.Infrastructure;
using Microsoft.Playwright;
using static Microsoft.Playwright.Assertions;

namespace Bit.Blow.Tests.E2E;

/// <summary>
/// The flow as the browser lays it out: nodes are measured by the browser layer, edges are drawn to the
/// handles it measured, size changes nothing in C# can see are observed, and the colour modes repaint it.
/// </summary>
[TestClass]
public sealed class RenderingTests : BlowTest
{
    [TestMethod]
    public async Task The_basic_flow_draws_its_edges_between_the_measured_handles()
    {
        var page = await OpenAsync("/");
        await WaitForEdgesAnchoredAsync(page);

        CollectionAssert.AreEquivalent(new[] { "1", "2", "3", "4" }, await IdsAsync(page));
        CollectionAssert.AreEquivalent(new[] { "e1-2", "e1-3", "e2-4" }, await IdsAsync(page, "edge"));

        // Each edge runs from the centre of its source node's source handle to the centre of its target node's
        // target handle. Before the browser layer measures the handles the canvas can only guess (the middle of a
        // node side of a default-sized node), so this only holds when measurement made it through to C#.
        foreach (var (edge, source, target) in new[] { ("e1-2", "1", "2"), ("e1-3", "1", "3"), ("e2-4", "2", "4") })
        {
            var (start, end) = await EdgeEndsAsync(page, edge);
            AssertNear(await HandleCentreAsync(page, source, "source"), start, 1, $"start of {edge}");
            AssertNear(await HandleCentreAsync(page, target, "target"), end, 1, $"end of {edge}");
        }

        // FitViewOnInit fitted the measured graph: every node is inside the pane, and the graph was scaled up to
        // use it rather than left at zoom 1 in a corner.
        var pane = await PaneBoxAsync(page);
        foreach (var id in await IdsAsync(page))
        {
            var node = await NodeBoxAsync(page, id);
            Assert.IsTrue(pane.Contains(node), $"node {id} {node} is outside the pane {pane}");
        }
        Assert.IsTrue((await ViewportAsync(page)).Zoom > 1.2, "the initial fit did not zoom the small graph in");
    }

    [TestMethod]
    public async Task The_browser_layer_measures_nodes_and_handles_in_flow_units_at_any_zoom()
    {
        var page = await OpenAsync("/");

        // Zoom in further, so screen pixels and flow units differ by a factor that is not the initial one.
        await WheelAsync(page, (await PaneBoxAsync(page)).Centre, -200);
        var viewport = await ViewportAsync(page);
        Assert.IsTrue(viewport.Zoom > 1.5, $"the flow did not zoom in: {viewport}");

        // The zoom handed in is deliberately wrong: the browser layer must measure the scale the viewport is
        // really painted at (a render can lag the zoom C# knows) rather than trust the number it is given.
        var measured = await WithModuleAsync<MeasuredNode[]>(page,
            "return m.measureNodes(document.querySelector('.bit-blow-viewport'), 1);");
        Assert.HasCount(4, measured);

        foreach (var node in measured)
        {
            var box = await NodeBoxAsync(page, node.Id);
            AssertNear(box.Width / viewport.Zoom, node.Width, 0.05, $"width of node {node.Id} in flow units");
            AssertNear(box.Height / viewport.Zoom, node.Height, 0.05, $"height of node {node.Id} in flow units");

            foreach (var handle in node.Handles)
            {
                var centre = await HandleCentreAsync(page, node.Id, handle.Type);
                AssertNear(centre, new Point(box.X + handle.OffsetX * viewport.Zoom, box.Y + handle.OffsetY * viewport.Zoom), 0.1,
                    $"{handle.Type} handle of node {node.Id}");
            }
        }
    }

    [TestMethod]
    public async Task The_browser_layer_finds_the_handle_under_a_point_and_nothing_elsewhere()
    {
        var page = await OpenAsync("/");
        var target = await HandleCentreAsync(page, "3", "target");
        var node = await NodeBoxAsync(page, "3");

        var hit = await WithModuleAsync<HandleHit?>(page,
            "return m.elementDataAtPoint(arg[0], arg[1], '.bit-blow-handle', document.querySelector('.bit-blow'));",
            new[] { target.X, target.Y });
        Assert.IsNotNull(hit, "no handle was found at the centre of a handle");
        Assert.AreEqual("3", hit.NodeId);
        Assert.AreEqual("target", hit.HandleType);
        Assert.AreEqual("top", hit.Position);
        Assert.IsTrue(hit.ConnectableEnd);

        // The middle of the node's body is not a handle, and a point outside the pane is not this flow's at all.
        foreach (var (x, y, where) in new[] { (node.Centre.X, node.Centre.Y, "the middle of a node"), (5d, 5d, "outside the pane") })
        {
            var miss = await WithModuleAsync<HandleHit?>(page,
                "return m.elementDataAtPoint(arg[0], arg[1], '.bit-blow-handle', document.querySelector('.bit-blow'));",
                new[] { x, y });
            Assert.IsNull(miss, $"a handle was reported at {where}");
        }
    }

    [TestMethod]
    public async Task A_resize_of_the_container_is_observed()
    {
        var page = await OpenAsync("/examples/responsive-canvas");

        // The minimap outlines the visible area, which the canvas sizes from the pane size it last measured.
        var before = await MiniMapWindowAsync(page);
        var zoom = (await ViewportAsync(page)).Zoom;
        AssertNear((await PaneBoxAsync(page)).Width / zoom, before.Width, 1, "visible width before the resize");

        // Resized behind Blazor's back: no render, no parameter change. Only the browser layer's ResizeObserver
        // can tell the canvas the pane is narrower now.
        await page.EvaluateAsync("() => { document.querySelector('.bit-blow').parentElement.style.width = '420px'; }");

        await EventuallyAsync(async () => Math.Abs((await MiniMapWindowAsync(page)).Width - 420 / zoom) < 1,
            "the canvas noticed its container shrank to 420px");
        AssertNear(420, (await PaneBoxAsync(page)).Width, 1, "pane width");
    }

    [TestMethod]
    public async Task A_node_that_grows_by_itself_is_remeasured_and_its_edges_follow()
    {
        var page = await OpenAsync("/examples/responsive-canvas");
        await WaitForEdgesAnchoredAsync(page);

        var (start, _) = await EdgeEndsAsync(page, "e2");
        var height = (await NodeBoxAsync(page, "grow")).Height;

        // The node's content grows without Blazor re-rendering anything, as an image loading or a font arriving
        // would make it: only the ResizeObserver on the node can report it.
        await page.EvaluateAsync("() => { document.querySelector('.bit-blow-node[data-id=\"grow\"] .resp-node').style.paddingBottom = '60px'; }");

        await EventuallyAsync(async () => (await EdgeEndsAsync(page, "e2")).Start.Y > start.Y + 40,
            "the edge leaving the bottom of the grown node moved down with it");

        var grown = await NodeBoxAsync(page, "grow");
        Assert.IsTrue(grown.Height > height + 40, $"the node did not grow: {height} -> {grown.Height}");
        AssertNear(await HandleCentreAsync(page, "grow", "source"), (await EdgeEndsAsync(page, "e2")).Start, 1, "start of e2");
    }

    [TestMethod]
    public async Task Dark_color_mode_applies_the_dark_palette()
    {
        var page = await OpenAsync("/examples/color-mode");
        Assert.AreEqual("rgb(250, 250, 250)", await BackgroundAsync(page, ".bit-blow"), "the light pane background");

        await page.Locator(".example-toolbar select").SelectOptionAsync("Dark");

        await Expect(page.Locator(".bit-blow").First).ToHaveClassAsync(new Regex(@"\bbit-blow-dark\b"));
        Assert.AreEqual("rgb(26, 26, 26)", await BackgroundAsync(page, ".bit-blow"), "the dark pane background");
        Assert.AreEqual("rgb(43, 43, 43)", await BackgroundAsync(page, ".bit-blow-node"), "the dark node background");
        Assert.AreEqual("rgb(43, 43, 43)", await BackgroundAsync(page, ".bit-blow-controls-button"), "the dark controls");
    }

    [TestMethod]
    public async Task System_color_mode_follows_the_operating_system_preference()
    {
        var page = await OpenAsync("/examples/color-mode");
        await page.EmulateMediaAsync(new PageEmulateMediaOptions { ColorScheme = ColorScheme.Dark });

        await page.Locator(".example-toolbar select").SelectOptionAsync("System");
        await Expect(page.Locator(".bit-blow").First).ToHaveClassAsync(new Regex(@"\bbit-blow-system\b"));
        Assert.AreEqual("rgb(26, 26, 26)", await BackgroundAsync(page, ".bit-blow"), "the pane under a dark system preference");

        await page.EmulateMediaAsync(new PageEmulateMediaOptions { ColorScheme = ColorScheme.Light });
        Assert.AreEqual("rgb(250, 250, 250)", await BackgroundAsync(page, ".bit-blow"), "the pane under a light system preference");
    }

    [TestMethod]
    public async Task Two_flows_on_one_page_are_measured_and_steered_independently()
    {
        var page = await OpenAsync("/examples/multi-flows");
        await WaitForEdgesAnchoredAsync(page);

        CollectionAssert.AreEquivalent(new[] { "a1", "a2" }, await IdsAsync(page, flow: 0));
        CollectionAssert.AreEquivalent(new[] { "b1", "b2", "b3" }, await IdsAsync(page, flow: 1));

        var a = await ViewportAsync(page, 0);
        var b = await ViewportAsync(page, 1);

        await page.Locator(".bit-blow").Nth(1).GetByRole(AriaRole.Button, new() { Name = "Zoom out" }).ClickAsync();
        await EventuallyAsync(async () => (await ViewportAsync(page, 1)).Zoom < b.Zoom - 0.01, "flow B zoomed out");

        Assert.AreEqual(a, await ViewportAsync(page, 0), "zooming flow B moved flow A");
    }

    private static Task<string> BackgroundAsync(IPage page, string selector) =>
        page.EvaluateAsync<string>("(s) => getComputedStyle(document.querySelector(s)).backgroundColor", selector);

    // What the browser layer hands back to .NET, as Playwright deserializes it.

    private sealed class MeasuredNode
    {
        public string Id { get; set; } = "";
        public double Width { get; set; }
        public double Height { get; set; }
        public MeasuredHandle[] Handles { get; set; } = [];
    }

    private sealed class MeasuredHandle
    {
        public string HandleId { get; set; } = "";
        public string Type { get; set; } = "";
        public string Position { get; set; } = "";
        public double OffsetX { get; set; }
        public double OffsetY { get; set; }
        public bool ConnectableEnd { get; set; }
    }

    private sealed class HandleHit
    {
        public string? NodeId { get; set; }
        public string HandleId { get; set; } = "";
        public string HandleType { get; set; } = "";
        public string Position { get; set; } = "";
        public bool ConnectableEnd { get; set; }
    }
}
