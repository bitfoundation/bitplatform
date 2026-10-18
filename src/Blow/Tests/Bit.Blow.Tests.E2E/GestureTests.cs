using System.Text.RegularExpressions;
using Bit.Blow.Tests.E2E.Infrastructure;
using Microsoft.Playwright;
using static Microsoft.Playwright.Assertions;

namespace Bit.Blow.Tests.E2E;

/// <summary>
/// Pointer and wheel gestures driven with a real mouse: dragging nodes, connecting handles, panning and zooming
/// the viewport, box selection, the controls, the minimap and the node resizer. Every assertion is on where
/// things ended up on screen.
/// </summary>
[TestClass]
public sealed class GestureTests : BlowTest
{
    [TestMethod]
    public async Task Dragging_a_node_moves_it_with_the_pointer_and_its_edges_follow()
    {
        var page = await OpenAsync("/");
        await WaitForEdgesAnchoredAsync(page);

        var before = await NodeBoxAsync(page, "2");
        var (_, endBefore) = await EdgeEndsAsync(page, "e1-2");

        await DragAsync(page, before.Centre, before.Centre.Offset(-70, 45));

        var after = await NodeBoxAsync(page, "2");
        AssertNear(before.X - 70, after.X, 0.5, "node left after the drag");
        AssertNear(before.Y + 45, after.Y, 0.5, "node top after the drag");

        // The edge into the node and the one out of it are redrawn to the moved handles.
        var (_, endAfter) = await EdgeEndsAsync(page, "e1-2");
        AssertNear(endBefore.Offset(-70, 45), endAfter, 1, "end of e1-2");
        AssertNear(await HandleCentreAsync(page, "2", "source"), (await EdgeEndsAsync(page, "e2-4")).Start, 1, "start of e2-4");

        CollectionAssert.AreEqual(new[] { "2" }, await SelectedNodesAsync(page), "dragging a node selects it");
        Assert.DoesNotContain("dragging", await page.Locator(".bit-blow-node[data-id=\"2\"]").GetAttributeAsync("class") ?? "", "the drag never ended");
    }

    [TestMethod]
    public async Task A_drag_keeps_following_the_pointer_after_it_leaves_the_canvas()
    {
        var page = await OpenAsync("/");
        var pane = await PaneBoxAsync(page);
        var node = await NodeBoxAsync(page, "2");
        var grab = node.Centre;

        // Out over the sidebar and back. The pane captures the pointer, so leaving it neither ends the drag (the
        // pane treats pointerleave as a release) nor loses the moves made outside it.
        await DragAsync(page, grab, new Point(pane.X - 120, grab.Y), release: false);
        StringAssert.Contains(await page.Locator(".bit-blow-node[data-id=\"2\"]").GetAttributeAsync("class"), "dragging",
            "the drag ended when the pointer left the canvas");

        var drop = new Point(pane.X + 320, pane.Y + 220);
        await page.Mouse.MoveAsync((float)drop.X, (float)drop.Y, new MouseMoveOptions { Steps = 10 });
        await page.WaitForTimeoutAsync(60);
        await page.Mouse.UpAsync();
        await WaitForQuietAsync(page);

        // The node ends up held exactly where it was grabbed, however far auto-pan scrolled the view meanwhile.
        var after = await NodeBoxAsync(page, "2");
        AssertNear(new Point(grab.X - node.X, grab.Y - node.Y), new Point(drop.X - after.X, drop.Y - after.Y), 1, "grab offset at the drop");

        // And the release ended it: moving on without a button pressed leaves the node where it is.
        await page.Mouse.MoveAsync((float)drop.X + 100, (float)drop.Y + 80, new MouseMoveOptions { Steps = 5 });
        await WaitForQuietAsync(page);
        Assert.AreEqual(after, await NodeBoxAsync(page, "2"), "the node kept following the pointer after the release");
    }

    [TestMethod]
    public async Task Dragging_from_a_source_handle_to_a_target_handle_connects_them()
    {
        var page = await OpenAsync("/");
        await WaitForEdgesAnchoredAsync(page);

        var source = await HandleCentreAsync(page, "3", "source");
        var target = await HandleCentreAsync(page, "4", "target");

        await DragAsync(page, source, target, release: false);

        // Mid-gesture: the connection line is drawn, and every handle says what a drop on it would do. Strict mode
        // only joins a source to a target, so the target handle is valid, another source handle is not, and the
        // handle the line comes out of is marked as such.
        await Expect(page.Locator(".bit-blow-connectionline")).ToHaveCountAsync(1);
        await Expect(page.Locator(".bit-blow-node[data-id=\"4\"] .bit-blow-handle.target")).ToHaveClassAsync(new Regex(@"\bbit-blow-handle-valid\b"));
        await Expect(page.Locator(".bit-blow-node[data-id=\"2\"] .bit-blow-handle.source")).ToHaveClassAsync(new Regex(@"\bbit-blow-handle-invalid\b"));
        await Expect(page.Locator(".bit-blow-node[data-id=\"3\"] .bit-blow-handle.source")).ToHaveClassAsync(new Regex(@"\bbit-blow-handle-connectingfrom\b"));

        // Released over the handle: the pane holds the pointer capture, so the handle never sees the release, and
        // the drop is resolved by asking the browser layer which handle lies under the point.
        await page.Mouse.UpAsync();
        await WaitForQuietAsync(page);

        await Expect(page.Locator(".bit-blow-connectionline")).ToHaveCountAsync(0);
        var edges = await IdsAsync(page, "edge");
        Assert.HasCount(4, edges, $"expected one new edge, found [{string.Join(", ", edges)}]");
        Assert.Contains("bit-blow-3--4", edges, "the new edge does not carry the id built from its connection");

        // The new edge is drawn between the two handles it joins.
        var (start, end) = await EdgeEndsAsync(page, "bit-blow-3--4");
        AssertNear(source, start, 1, "start of the new edge");
        AssertNear(target, end, 1, "end of the new edge");
    }

    [TestMethod]
    public async Task A_connection_dropped_on_empty_space_is_abandoned()
    {
        var page = await OpenAsync("/");
        var pane = await PaneBoxAsync(page);

        await DragAsync(page, await HandleCentreAsync(page, "3", "source"), new Point(pane.Right - 60, pane.Bottom - 230));

        await Expect(page.Locator(".bit-blow-connectionline")).ToHaveCountAsync(0);
        CollectionAssert.AreEquivalent(new[] { "e1-2", "e1-3", "e2-4" }, await IdsAsync(page, "edge"));
    }

    [TestMethod]
    public async Task The_wheel_zooms_about_the_cursor()
    {
        var page = await OpenAsync("/");
        var pane = await PaneBoxAsync(page);
        var before = await ViewportAsync(page);

        var cursor = new Point(pane.X + pane.Width * 0.3, pane.Y + pane.Height * 0.6);
        await WheelAsync(page, cursor, -150);

        var after = await ViewportAsync(page);
        Assert.IsTrue(after.Zoom > before.Zoom * 1.1, $"a wheel up did not zoom in: {before} -> {after}");

        // The flow point under the cursor is the one that stays put.
        double FlowX(Viewport v) => (cursor.X - pane.X - v.X) / v.Zoom;
        double FlowY(Viewport v) => (cursor.Y - pane.Y - v.Y) / v.Zoom;
        // The computed transform the probe reads is serialized to six significant digits.
        AssertNear(FlowX(before), FlowX(after), 0.05, "flow x under the cursor");
        AssertNear(FlowY(before), FlowY(after), 0.05, "flow y under the cursor");

        await WheelAsync(page, cursor, 300);
        Assert.IsTrue((await ViewportAsync(page)).Zoom < after.Zoom, "a wheel down did not zoom out");
    }

    [TestMethod]
    public async Task Dragging_the_background_pans_the_viewport()
    {
        var page = await OpenAsync("/");
        var pane = await PaneBoxAsync(page);
        var before = await ViewportAsync(page);
        var node = await NodeBoxAsync(page, "3");

        await DragAsync(page, EmptySpot(pane), EmptySpot(pane).Offset(-130, 90));

        var after = await ViewportAsync(page);
        AssertNear(before.X - 130, after.X, 0.5, "viewport x");
        AssertNear(before.Y + 90, after.Y, 0.5, "viewport y");
        Assert.AreEqual(before.Zoom, after.Zoom, "panning changed the zoom");

        var moved = await NodeBoxAsync(page, "3");
        AssertNear(new Point(node.X - 130, node.Y + 90), new Point(moved.X, moved.Y), 0.5, "node 3 on screen");
    }

    [TestMethod]
    public async Task A_double_click_on_the_background_zooms_in_about_that_point()
    {
        var page = await OpenAsync("/");
        var pane = await PaneBoxAsync(page);
        var before = await ViewportAsync(page);

        var at = EmptySpot(pane);
        await page.Mouse.DblClickAsync((float)at.X, (float)at.Y);
        await EventuallyAsync(async () => (await ViewportAsync(page)).Zoom > before.Zoom, "the double click zoomed in");

        var after = await ViewportAsync(page);
        AssertNear(before.Zoom * 1.2, after.Zoom, 0.001, "zoom after a double click");
        AssertNear((at.X - pane.X - before.X) / before.Zoom, (at.X - pane.X - after.X) / after.Zoom, 0.05, "flow x under the double click");
        AssertNear((at.Y - pane.Y - before.Y) / before.Zoom, (at.Y - pane.Y - after.Y) / after.Zoom, 0.05, "flow y under the double click");
    }

    [TestMethod]
    public async Task Shift_dragging_a_box_selects_the_nodes_it_catches_and_Delete_removes_them()
    {
        var page = await OpenAsync("/");
        var two = await NodeBoxAsync(page, "2");
        var three = await NodeBoxAsync(page, "3");

        await page.Keyboard.DownAsync("Shift");
        await DragAsync(page, new Point(two.X - 15, two.Y - 15), new Point(three.Right + 15, three.Bottom + 15), release: false);

        // The box is drawn while the gesture runs, and it selects as it goes.
        await Expect(page.Locator(".bit-blow-selection")).ToBeVisibleAsync();
        await page.Mouse.UpAsync();
        await page.Keyboard.UpAsync("Shift");
        await WaitForQuietAsync(page);

        await Expect(page.Locator(".bit-blow-selection")).ToHaveCountAsync(0);
        CollectionAssert.AreEquivalent(new[] { "2", "3" }, await SelectedNodesAsync(page));

        // The press focused the pane, so its keyboard shortcuts work right away.
        await page.Keyboard.PressAsync("Delete");
        await EventuallyAsync(async () => (await IdsAsync(page)).Length == 2, "the selected nodes were deleted");

        CollectionAssert.AreEquivalent(new[] { "1", "4" }, await IdsAsync(page));
        // Every edge touched one of them, so none is left hanging.
        Assert.IsEmpty(await IdsAsync(page, "edge"));
    }

    [TestMethod]
    public async Task Clicking_an_edge_selects_it_and_Delete_removes_only_that_edge()
    {
        var page = await OpenAsync("/");
        await WaitForEdgesAnchoredAsync(page);

        // The middle of the path, read from the painted geometry.
        var middle = await page.EvaluateAsync<double[]>(
            """
            () => {
              const p = document.querySelector('.bit-blow-edge[data-id="e1-3"] > .bit-blow-edge-path');
              const q = p.getPointAtLength(p.getTotalLength() / 2).matrixTransform(p.getScreenCTM());
              return [q.x, q.y];
            }
            """);
        await page.Mouse.ClickAsync((float)middle[0], (float)middle[1]);

        await Expect(page.Locator(".bit-blow-edge[data-id=\"e1-3\"]")).ToHaveClassAsync(new Regex(@"\bselected\b"));
        Assert.IsEmpty(await SelectedNodesAsync(page), "selecting an edge selected nodes too");

        await page.Keyboard.PressAsync("Delete");
        await EventuallyAsync(async () => (await IdsAsync(page, "edge")).Length == 2, "the edge was deleted");

        CollectionAssert.AreEquivalent(new[] { "e1-2", "e2-4" }, await IdsAsync(page, "edge"));
        CollectionAssert.AreEquivalent(new[] { "1", "2", "3", "4" }, await IdsAsync(page), "deleting an edge deleted a node");
    }

    [TestMethod]
    public async Task The_controls_zoom_in_zoom_out_and_fit_the_view()
    {
        var page = await OpenAsync("/");
        var pane = await PaneBoxAsync(page);
        var fitted = await ViewportAsync(page);

        await page.GetByRole(AriaRole.Button, new() { Name = "Zoom in" }).ClickAsync();
        await EventuallyAsync(async () => (await ViewportAsync(page)).Zoom > fitted.Zoom, "zoom in took effect");
        AssertNear(fitted.Zoom * 1.2, (await ViewportAsync(page)).Zoom, 0.001, "zoom after zoom in");

        await page.GetByRole(AriaRole.Button, new() { Name = "Zoom out" }).ClickAsync();
        await EventuallyAsync(async () => Math.Abs((await ViewportAsync(page)).Zoom - fitted.Zoom) < 0.001, "zoom out undid zoom in");

        // Pan most of the graph out of view, then fit it back.
        await DragAsync(page, EmptySpot(pane), EmptySpot(pane).Offset(-400, 270));
        var node = await NodeBoxAsync(page, "4");
        Assert.IsFalse(pane.Contains(node), "the pan did not move node 4 out of view");

        await page.GetByRole(AriaRole.Button, new() { Name = "Fit view" }).ClickAsync();
        await EventuallyAsync(async () => pane.Contains(await NodeBoxAsync(page, "4")), "fit view brought node 4 back");
        await WaitForQuietAsync(page);

        foreach (var id in await IdsAsync(page))
        {
            var box = await NodeBoxAsync(page, id);
            Assert.IsTrue(pane.Contains(box), $"node {id} {box} is outside the pane {pane} after fit view");
        }
        AssertNear(fitted.Zoom, (await ViewportAsync(page)).Zoom, 0.001, "zoom after fit view");
    }

    [TestMethod]
    public async Task The_minimap_mirrors_the_nodes_and_the_visible_area()
    {
        var page = await OpenAsync("/");
        Assert.AreEqual(4, await page.Locator(".bit-blow-minimap-node").CountAsync(), "one minimap rectangle per node");

        var pane = await PaneBoxAsync(page);
        var zoom = (await ViewportAsync(page)).Zoom;
        var window = await MiniMapWindowAsync(page);
        AssertNear(pane.Width / zoom, window.Width, 0.5, "width of the visible area");
        AssertNear(pane.Height / zoom, window.Height, 0.5, "height of the visible area");

        // Panning the flow moves the outlined area the opposite way, in flow units.
        await DragAsync(page, EmptySpot(pane), EmptySpot(pane).Offset(-200, 100));
        var moved = await MiniMapWindowAsync(page);
        AssertNear(window.X + 200 / zoom, moved.X, 0.5, "x of the visible area after the pan");
        AssertNear(window.Y - 100 / zoom, moved.Y, 0.5, "y of the visible area after the pan");
    }

    [TestMethod]
    public async Task Clicking_a_node_on_the_minimap_brings_it_to_the_middle_of_the_view()
    {
        var page = await OpenAsync("/");
        var pane = await PaneBoxAsync(page);

        // Out of the way first, so the click has somewhere to bring it back from.
        await DragAsync(page, EmptySpot(pane), EmptySpot(pane).Offset(-400, 200));
        Assert.IsFalse((await NodeBoxAsync(page, "4")).Contains(pane.Centre));

        // The minimap draws the nodes in graph order; node 4 is the last.
        var rect = await BoxAsync(page, ".bit-blow-minimap-node:nth-of-type(4)");
        await page.Mouse.ClickAsync((float)rect.Centre.X, (float)rect.Centre.Y);

        await EventuallyAsync(async () => (await NodeBoxAsync(page, "4")).Contains(pane.Centre),
            "the middle of the pane fell on node 4");
    }

    [TestMethod]
    public async Task Clicking_the_minimap_centres_the_view_exactly_on_the_clicked_point()
    {
        var page = await OpenAsync("/");
        var pane = await PaneBoxAsync(page);

        var rect = await BoxAsync(page, ".bit-blow-minimap-node:nth-of-type(4)");
        await page.Mouse.ClickAsync((float)rect.Centre.X, (float)rect.Centre.Y);
        await WaitForQuietAsync(page);

        AssertNear(pane.Centre, (await NodeBoxAsync(page, "4")).Centre, 2, "centre of node 4");
    }

    [TestMethod]
    public async Task The_node_resizer_resizes_a_selected_node_and_its_edges_follow()
    {
        var page = await OpenAsync("/examples/node-resizer");
        await WaitForEdgesAnchoredAsync(page);

        Assert.AreEqual(0, await page.Locator(".bit-blow-resize-handle").CountAsync(), "the resizer shows before anything is selected");

        var before = await NodeBoxAsync(page, "1");
        var handleBefore = await HandleCentreAsync(page, "1", "source");
        await page.Mouse.ClickAsync((float)before.Centre.X, (float)before.Centre.Y);
        var corner = page.Locator(".bit-blow-node[data-id=\"1\"] .bit-blow-resize-handle.bottom-right");
        await Expect(corner).ToBeVisibleAsync();

        var grip = (await corner.BoundingBoxAsync())!;
        await DragAsync(page, new Point(grip.X + grip.Width / 2, grip.Y + grip.Height / 2),
            new Point(grip.X + grip.Width / 2 + 80, grip.Y + grip.Height / 2 + 50));

        var after = await NodeBoxAsync(page, "1");
        AssertNear(before.X, after.X, 0.5, "left edge (anchored while the bottom-right corner moves)");
        AssertNear(before.Y, after.Y, 0.5, "top edge");
        AssertNear(before.Width + 80, after.Width, 1, "width");
        AssertNear(before.Height + 50, after.Height, 1, "height");

        // The edge leaves the handle in the middle of the node's right side, which moved right with the side
        // and down by half the growth.
        var handleAfter = await HandleCentreAsync(page, "1", "source");
        AssertNear(handleBefore.Offset(80, 25), handleAfter, 1, "the right-side handle");
        AssertNear(handleAfter, (await EdgeEndsAsync(page, "e1-2")).Start, 1, "start of e1-2");
    }

    [TestMethod]
    public async Task The_node_resizer_can_keep_the_aspect_ratio()
    {
        var page = await OpenAsync("/examples/node-resizer");

        var before = await NodeBoxAsync(page, "2");
        AssertNear(before.Width, before.Height, 1, "the node starts square");
        await page.Mouse.ClickAsync((float)before.Centre.X, (float)before.Centre.Y);

        var corner = page.Locator(".bit-blow-node[data-id=\"2\"] .bit-blow-resize-handle.bottom-right");
        await Expect(corner).ToBeVisibleAsync();
        var grip = (await corner.BoundingBoxAsync())!;

        // Pulled mostly sideways: the height follows the width.
        await DragAsync(page, new Point(grip.X + grip.Width / 2, grip.Y + grip.Height / 2),
            new Point(grip.X + grip.Width / 2 + 60, grip.Y + grip.Height / 2 + 10));

        var after = await NodeBoxAsync(page, "2");
        Assert.IsTrue(after.Width > before.Width + 40, $"the node did not grow: {before} -> {after}");
        AssertNear(after.Width, after.Height, 1, "height of the resized node");
    }

    /// <summary>A spot on the basic demo's background, clear of its nodes, edges and overlays.</summary>
    private static Point EmptySpot(Box pane) => new(pane.Right - 70, pane.Y + 140);
}
