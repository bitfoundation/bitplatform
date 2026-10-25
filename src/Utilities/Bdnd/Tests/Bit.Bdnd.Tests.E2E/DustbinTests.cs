using Bit.Bdnd.Tests.E2E.Infrastructure;
using Microsoft.Playwright;
using static Microsoft.Playwright.Assertions;

namespace Bit.Bdnd.Tests.E2E;

/// <summary>
/// The dustbin examples: a target that reacts to the hover and records drops, targets that accept different types
/// and only light up (and only take a drop) for the types they accept, a source removed from the palette once a
/// drop took it, and the same demo running inside an iframe.
/// </summary>
[TestClass]
public sealed class DustbinTests : BdndTest
{
    private const string DarkGreen = "rgb(0, 100, 0)";

    [TestMethod]
    public async Task A_box_dropped_on_the_dustbin_is_recorded_and_the_bin_reacts_while_it_hovers()
    {
        var page = await OpenAsync("/dustbin/single");
        var bin = page.Locator(".dustbin");
        var glass = page.Locator(".box", new() { HasText = "Glass" });

        await StartDragAsync(page, glass);

        // .NET began the drag: the source renders its dragging state, and the bin is not hovered yet.
        await Expect(glass).ToHaveCSSAsync("opacity", "0.4");
        await Expect(bin).ToHaveTextAsync("Drag a box here");

        await DragToAsync(page, (await BoxAsync(bin)).Centre, async () =>
        {
            await Expect(bin).ToHaveTextAsync("Release to drop");
            await Expect(bin).ToHaveCSSAsync("background-color", DarkGreen);
        });

        await Expect(bin).ToHaveTextAsync("Last dropped: Glass (total 1)");
        await Expect(bin).ToHaveCSSAsync("background-color", "rgb(34, 34, 34)");
        await Expect(glass).ToHaveCSSAsync("opacity", "1");

        // The browser ended the drag with a dragend on the source, which is still in the document (so the event
        // bubbled up to the window), after the one drop.
        Assert.AreEqual(1, await page.EvaluateAsync<int>("() => window.__bdndE2E.dragends"), "dragend events");
        Assert.AreEqual(1, (await DropsAsync(page.MainFrame)).Count, "drop events");

        // The next drag is a drag of its own, with its own item.
        await DragAsync(page, page.Locator(".box", new() { HasText = "Banana" }), bin,
            () => Expect(bin).ToHaveTextAsync("Release to drop"));
        await Expect(bin).ToHaveTextAsync("Last dropped: Banana (total 2)");
    }

    [TestMethod]
    public async Task The_hover_survives_the_bin_replacing_the_element_under_the_pointer()
    {
        var page = await OpenAsync("/dustbin/single");
        var bin = page.Locator(".dustbin");
        var centre = (await BoxAsync(bin)).Centre;

        // The pointer enters the bin over its label. Hovered, the bin renders another span in its place: the element
        // the pointer entered is gone, without the browser ever raising a dragleave for it.
        await StartDragAsync(page, page.Locator(".box", new() { HasText = "Glass" }));
        await MoveAsync(page, centre);
        await Expect(bin).ToHaveTextAsync("Release to drop");

        // Every text the bin shows from now on is recorded, so a hover that drops out for a single render shows up.
        await bin.EvaluateAsync(
            """
            (el) => {
              window.__binTexts = [];
              new MutationObserver(() => window.__binTexts.push(el.textContent.trim()))
                .observe(el, { subtree: true, childList: true, characterData: true });
            }
            """);

        // Wandering over the bin (each move a dragover, and a dragenter whenever the element under the pointer
        // changes) keeps it hovered throughout.
        foreach (var (dx, dy) in new[] { (3, 2), (-20, 10), (25, -15), (0, 30), (-30, -30), (0, 0) })
        {
            await MoveAsync(page, centre.Offset(dx, dy), steps: 2);
            await WaitForQuietAsync(page, 100);
        }
        await Expect(bin).ToHaveTextAsync("Release to drop");
        var texts = await page.EvaluateAsync<string[]>("() => window.__binTexts");
        Assert.IsTrue(texts.All(t => t == "Release to drop"), "the hover dropped out on the way: " + string.Join(" | ", texts));

        await DragToAsync(page, centre);
        await Expect(bin).ToHaveTextAsync("Last dropped: Glass (total 1)");
    }

    [TestMethod]
    public async Task Leaving_the_dustbin_clears_its_hover_and_a_drop_elsewhere_records_nothing()
    {
        var page = await OpenAsync("/dustbin/single");
        var bin = page.Locator(".dustbin");
        var paper = page.Locator(".box", new() { HasText = "Paper" });
        var binBox = await BoxAsync(bin);

        await StartDragAsync(page, paper);
        await MoveAsync(page, binBox.Centre);
        await Expect(bin).ToHaveTextAsync("Release to drop");

        // Out to the empty part of the stage, right of the bin, which is no drop target.
        await DragToAsync(page, new Point(binBox.Right + 300, binBox.Centre.Y),
            () => Expect(bin).ToHaveTextAsync("Drag a box here"));

        // The drag ended as a cancel: the source is back to normal and nothing was recorded.
        await Expect(paper).ToHaveCSSAsync("opacity", "1");
        await Expect(bin).ToHaveTextAsync("Drag a box here");
        Assert.AreEqual(0, (await DropsAsync(page.MainFrame)).Count, "a drop was raised where no target took it");
    }

    [TestMethod]
    public async Task A_drag_released_outside_the_window_ends_as_a_cancel_and_the_next_one_works()
    {
        var page = await OpenAsync("/dustbin/single");
        var bin = page.Locator(".dustbin");
        var paper = page.Locator(".box", new() { HasText = "Paper" });
        var binBox = await BoxAsync(bin);

        await StartDragAsync(page, paper);
        await MoveAsync(page, binBox.Centre);
        await Expect(bin).ToHaveTextAsync("Release to drop");
        // held past the first second, in which the module takes no mouse move for the drag's end (Firefox moves the
        // mouse as a drag starts) - as a person's drag out of the window is
        await page.WaitForTimeoutAsync(1100);
        // out of the window past its left edge, and released there
        await MoveAsync(page, new Point(-40, binBox.Centre.Y));
        await page.Mouse.UpAsync();
        // Where the engine reported no end of the drag (Firefox's driver), the module ends it on the button-less move
        // that came with the release, once no drag event has followed for half a second; Chromium and WebKit report
        // the end themselves. Then back in: Firefox's driver, still in a drag of its own, sends a dragover with that
        // move, which a person's Firefox does not - so the move waits until the half second is over.
        await page.WaitForTimeoutAsync(700);
        await page.Mouse.MoveAsync(20, (float)binBox.Centre.Y);
        await EventuallyAsync(async () => await paper.EvaluateAsync<string>("el => getComputedStyle(el).opacity") == "1", "the drag ended");
        await WaitForQuietAsync(page);

        // the drag ended as a cancel: nothing was dropped, the bin and the box are as before
        await Expect(bin).ToHaveTextAsync("Drag a box here");
        await Expect(paper).ToHaveCSSAsync("opacity", "1");
        await Expect(paper).Not.ToHaveAttributeAsync("data-bit-bdnd-dragging", "");
        Assert.AreEqual(0, (await DropsAsync(page.MainFrame)).Count, "a drop was raised outside the window");

        // And the next drag is a drag like any other. Not under Firefox's driver: after a release outside the window
        // it keeps a drag session of its own going, which takes the input - no key or mouse event reaches the page.
        if (Browser.BrowserType.Name == "firefox") return;
        await WaitForBdndAsync(page.MainFrame);
        await page.Locator("[draggable]", new() { Has = page.Locator(".box", new() { HasText = "Paper" }) }).FocusAsync();
        await page.Keyboard.PressAsync("Space");
        await Expect(page.Locator("#bit-bdnd-live-region")).ToContainTextAsync("Picked up");
        await page.Keyboard.PressAsync("ArrowUp"); // the bin is above the boxes
        await Expect(page.Locator("#bit-bdnd-live-region")).ToContainTextAsync("is over");
        await page.Keyboard.PressAsync("Enter");
        await Expect(bin).ToHaveTextAsync("Last dropped: Paper (total 1)");
    }

    [TestMethod]
    public async Task A_box_lights_up_only_the_bins_that_accept_its_type_and_drops_into_one_of_them()
    {
        var page = await OpenAsync("/dustbin/multiple");
        var boxes = Bin(page, "Boxes");
        var pieces = Bin(page, "Pieces");
        var anything = Bin(page, "Anything");

        // While a box (type "box") is in the air, every bin that can take it shows the dashed outline and the
        // one that cannot is dimmed.
        await StartDragAsync(page, Source(page, "Glass"));
        await Expect(boxes).ToHaveCSSAsync("outline-style", "dashed");
        await Expect(anything).ToHaveCSSAsync("outline-style", "dashed");
        await Expect(pieces).ToHaveCSSAsync("outline-style", "none");
        await Expect(pieces).ToHaveCSSAsync("opacity", "0.5");

        await DragToAsync(page, (await BoxAsync(boxes)).Centre, () => Expect(boxes).ToContainTextAsync("release to drop"));
        await Expect(boxes).ToContainTextAsync("last: Glass");
        await Expect(anything).Not.ToContainTextAsync("last:");

        // The drag is over: no bin is highlighted or dimmed any more.
        await Expect(anything).ToHaveCSSAsync("outline-style", "none");
        await Expect(pieces).ToHaveCSSAsync("opacity", "1");

        // A knight is a piece: now Pieces and Anything light up, and Boxes does not.
        await StartDragAsync(page, Source(page, "Knight"));
        await Expect(pieces).ToHaveCSSAsync("outline-style", "dashed");
        await Expect(anything).ToHaveCSSAsync("outline-style", "dashed");
        await Expect(boxes).ToHaveCSSAsync("outline-style", "none");
        await Expect(boxes).ToHaveCSSAsync("opacity", "0.5");

        await DragToAsync(page, (await BoxAsync(anything)).Centre, () => Expect(anything).ToContainTextAsync("release to drop"));
        await Expect(anything).ToContainTextAsync("last: Knight");
        await Expect(pieces).Not.ToContainTextAsync("last:");
        await Expect(boxes).ToContainTextAsync("last: Glass");
    }

    [TestMethod]
    public async Task A_bin_that_does_not_accept_the_type_refuses_the_drop_with_the_no_drop_effect()
    {
        var page = await OpenAsync("/dustbin/multiple");
        var pieces = Bin(page, "Pieces");
        var banana = Source(page, "Banana");

        await StartDragAsync(page, banana);
        await Expect(pieces).ToHaveCSSAsync("opacity", "0.5");

        // Over the empty stage, right of the last bin, the browser's own default applies: nothing accepts the dragover.
        var piecesBox = await BoxAsync(pieces);
        var anythingBox = await BoxAsync(Bin(page, "Anything"));
        var empty = new Point(anythingBox.Right + 150, anythingBox.Centre.Y);
        await MoveAsync(page, empty);
        var overPage = await DragOverAtAsync(page, page.Locator(".bb-demo"), empty);
        Assert.IsFalse(overPage.Prevented, $"a dragover over no drop target was accepted: {overPage}");

        await DragToAsync(page, piecesBox.Centre, async () =>
        {
            // The module answers every dragover over the refusing bin: taken (so the browser does not fall back to
            // its own handling), but with the "none" effect, which shows the no-drop cursor and rules out a drop.
            var over = await DragOverAtAsync(page, pieces, piecesBox.Centre);
            Assert.IsTrue(over.Inside, $"the last dragover was not over the Pieces bin: {over}");
            Assert.IsTrue(over.Prevented, $"the module did not answer the dragover over a refusing bin: {over}");
            Assert.AreEqual("none", over.DropEffect, $"a refusing bin must show the no-drop effect: {over}");

            // Hovered but refusing: the bin is neither "over" nor ready to take the box.
            await Expect(pieces).Not.ToContainTextAsync("release to drop");
            await Expect(pieces).ToHaveCSSAsync("opacity", "0.5");
        });

        // No drop event reached the page, and the drag ended as a cancel.
        Assert.AreEqual(0, (await DropsAsync(page.MainFrame)).Count, "the browser raised a drop on a refusing bin");
        await Expect(banana).ToHaveCSSAsync("opacity", "1");
        await Expect(pieces).ToHaveCSSAsync("opacity", "1");
        await Expect(pieces).Not.ToContainTextAsync("last:");
    }

    [TestMethod]
    public async Task A_moved_box_leaves_the_palette_once_dropped_while_a_copied_one_stays()
    {
        var page = await OpenAsync("/dustbin/copy-or-move");
        var bin = page.Locator(".dustbin");
        var keep = page.Locator(".box", new() { HasText = "Glass (keep)" });
        var take = page.Locator(".box", new() { HasText = "Banana (take)" });

        await DragAsync(page, take, bin, () => Expect(bin).ToHaveCSSAsync("background-color", DarkGreen));
        await Expect(bin).ToContainTextAsync("Dropped 1 item(s)");

        // OnEnd saw DidDrop for the "move" box, and took it out of the palette.
        await Expect(take).ToHaveCountAsync(0);

        // The "copy" box stays, however often it is dropped.
        for (var dropped = 2; dropped <= 3; dropped++)
        {
            await DragAsync(page, keep, bin, () => Expect(bin).ToHaveCSSAsync("background-color", DarkGreen));
            await Expect(bin).ToContainTextAsync($"Dropped {dropped} item(s)");
            await Expect(keep).ToBeVisibleAsync();
        }
        await Expect(bin.Locator(":scope > div > div")).ToHaveTextAsync(["Dropped 3 item(s)", "Banana (take)", "Glass (keep)", "Glass (keep)"]);
    }

    [TestMethod]
    public async Task Boxes_dropped_outside_the_dustbin_both_stay_in_the_palette()
    {
        var page = await OpenAsync("/dustbin/copy-or-move");
        var bin = page.Locator(".dustbin");
        var binBox = await BoxAsync(bin);
        var outside = new Point(binBox.Right + 300, binBox.Centre.Y);

        foreach (var name in new[] { "Banana (take)", "Glass (keep)" })
        {
            var box = page.Locator(".box", new() { HasText = name });
            await StartDragAsync(page, box);
            await Expect(box).ToHaveCSSAsync("opacity", "0.4");
            await DragToAsync(page, outside);

            // OnEnd ran with DidDrop false: the box is back to normal, and still there.
            await Expect(box).ToHaveCSSAsync("opacity", "1");
            await Expect(box).ToBeVisibleAsync();
        }

        await Expect(page.Locator(".box")).ToHaveCountAsync(2);
        await Expect(bin).ToHaveTextAsync("Dropped 0 item(s)");
    }

    [TestMethod]
    public async Task The_dustbin_works_inside_an_iframe()
    {
        var page = await OpenAsync("/dustbin/iframe", bdnd: false);

        // The iframe hosts a page of its own, which boots its own WebAssembly app and loads the module itself.
        var frame = await (await page.Locator("iframe").ElementHandleAsync()).ContentFrameAsync();
        Assert.IsNotNull(frame, "the iframe has no document");
        await WaitForInteractiveAsync(frame);
        await WaitForBdndAsync(frame);

        var bin = frame.Locator(".dustbin");
        var glass = frame.Locator(".box", new() { HasText = "Glass" });

        await StartDragAsync(page, (await BoxAsync(glass)).Centre, frame);
        await Expect(glass).ToHaveCSSAsync("opacity", "0.4");
        await DragToAsync(page, (await BoxAsync(bin)).Centre, () => Expect(bin).ToHaveTextAsync("Release to drop"));

        await Expect(bin).ToHaveTextAsync("Last dropped: Glass (total 1)");
        await Expect(glass).ToHaveCSSAsync("opacity", "1");
    }

    private static ILocator Bin(IPage page, string name) => page.Locator($".dustbin[data-bin=\"{name}\"]");

    private static ILocator Source(IPage page, string name) => page.Locator(".box", new() { HasText = name });
}
