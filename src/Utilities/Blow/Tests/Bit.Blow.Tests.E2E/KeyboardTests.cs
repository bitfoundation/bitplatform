using Bit.Blow.Tests.E2E.Infrastructure;
using Microsoft.Playwright;

namespace Bit.Blow.Tests.E2E;

/// <summary>
/// The flow from the keyboard alone: tabbing to its elements, selecting and moving them, abandoning a gesture,
/// and the keys being left to a text field that has focus.
/// </summary>
[TestClass]
public sealed class KeyboardTests : BlowTest
{
    [TestMethod]
    public async Task Tab_reaches_a_node_Enter_selects_it_and_the_arrow_keys_move_it()
    {
        var page = await OpenAsync("/examples/a11y");
        await FocusPaneAsync(page);

        // Edges come before nodes in the document, and both are in the tab order: tab until a node has focus.
        string? focused = null;
        for (int i = 0; i < 10 && focused is null; i++)
        {
            await page.Keyboard.PressAsync("Tab");
            focused = await page.EvaluateAsync<string?>(
                "() => document.activeElement?.classList.contains('bit-blow-node') ? document.activeElement.dataset.id : null");
        }
        Assert.IsNotNull(focused, "ten presses of Tab never reached a node");
        Assert.IsEmpty(await SelectedNodesAsync(page), "focusing a node selected it");

        await page.Keyboard.PressAsync("Enter");
        await EventuallyAsync(async () => (await SelectedNodesAsync(page)).SequenceEqual([focused]), $"Enter selected node {focused}");

        var zoom = (await ViewportAsync(page)).Zoom;
        var before = await NodeBoxAsync(page, focused);
        var scrolled = await page.EvaluateAsync<double>("() => window.scrollY");

        // Five flow units per press, twenty-five with Shift.
        await page.Keyboard.PressAsync("ArrowRight");
        await page.Keyboard.PressAsync("ArrowRight");
        await page.Keyboard.PressAsync("ArrowDown");
        await page.Keyboard.PressAsync("Shift+ArrowDown");
        await EventuallyAsync(async () => Math.Abs((await NodeBoxAsync(page, focused)).Y - (before.Y + 30 * zoom)) < 0.5,
            "the last arrow key moved the node");

        var after = await NodeBoxAsync(page, focused);
        AssertNear(before.X + 10 * zoom, after.X, 0.5, "node left after two presses of ArrowRight");
        AssertNear(before.Y + 30 * zoom, after.Y, 0.5, "node top after ArrowDown and Shift+ArrowDown");

        // The move is announced, and the page itself did not scroll under the arrow keys.
        StringAssert.Contains(await page.Locator(".bit-blow-a11y-live").TextContentAsync(), "moved");
        Assert.AreEqual(scrolled, await page.EvaluateAsync<double>("() => window.scrollY"), "the arrow keys scrolled the page");
    }

    [TestMethod]
    public async Task Escape_clears_the_selection()
    {
        var page = await OpenAsync("/");
        var two = await NodeBoxAsync(page, "2");
        var three = await NodeBoxAsync(page, "3");

        await page.Mouse.ClickAsync((float)two.Centre.X, (float)two.Centre.Y);
        await page.Keyboard.DownAsync("Shift");
        await page.Mouse.ClickAsync((float)three.Centre.X, (float)three.Centre.Y);
        await page.Keyboard.UpAsync("Shift");
        await EventuallyAsync(async () => (await SelectedNodesAsync(page)).Length == 2, "Shift+click added to the selection");

        await page.Keyboard.PressAsync("Escape");
        await EventuallyAsync(async () => (await SelectedNodesAsync(page)).Length == 0, "Escape cleared the selection");
        CollectionAssert.AreEquivalent(new[] { "1", "2", "3", "4" }, await IdsAsync(page), "Escape removed a node");
    }

    [TestMethod]
    public async Task Escape_abandons_a_drag_and_puts_the_node_back()
    {
        var page = await OpenAsync("/");
        var before = await NodeBoxAsync(page, "3");

        await DragAsync(page, before.Centre, before.Centre.Offset(-90, 60), release: false);
        AssertNear(before.X - 90, (await NodeBoxAsync(page, "3")).X, 0.5, "node during the drag");

        await page.Keyboard.PressAsync("Escape");
        await EventuallyAsync(async () => Math.Abs((await NodeBoxAsync(page, "3")).X - before.X) < 0.5, "Escape put the node back");

        // The button is still down, but the gesture is over: further moves do not pick the node back up.
        await page.Mouse.MoveAsync((float)before.Centre.X + 40, (float)before.Centre.Y + 40, new MouseMoveOptions { Steps = 4 });
        await page.Mouse.UpAsync();
        await WaitForQuietAsync(page);

        var after = await NodeBoxAsync(page, "3");
        AssertNear(new Point(before.X, before.Y), new Point(after.X, after.Y), 0.5, "node after the abandoned drag");
    }

    [TestMethod]
    public async Task Backspace_in_a_field_inside_a_node_edits_the_text_and_does_not_delete_the_node()
    {
        var page = await OpenAsync("/examples/editable-node");
        var field = page.Locator(".bit-blow-node[data-id=\"1\"] .edit-input");

        await field.ClickAsync();
        await page.Keyboard.PressAsync("End");
        await page.Keyboard.TypeAsync("XY");
        await page.Keyboard.PressAsync("Backspace");
        await page.Keyboard.PressAsync("Backspace");
        await page.Keyboard.PressAsync("Backspace");
        await page.Keyboard.PressAsync("Delete");
        await WaitForQuietAsync(page);

        // Pressing in the field selected its node, so without the editable-focus check Backspace would have
        // deleted it.
        Assert.AreEqual("Collect inpu", await field.InputValueAsync());
        CollectionAssert.AreEquivalent(new[] { "1", "2" }, await IdsAsync(page), "a key typed into a field deleted a node");
        Assert.AreEqual(1, await page.Locator(".bit-blow-edge").CountAsync(), "a key typed into a field deleted an edge");

        // The same key does delete the node once focus is on the node rather than in the field.
        await page.Locator(".bit-blow-node[data-id=\"1\"] .edit-grip").ClickAsync();
        await page.Keyboard.PressAsync("Delete");
        await EventuallyAsync(async () => (await IdsAsync(page)).SequenceEqual(["2"]), "Delete removed the selected node");
    }

    private static async Task FocusPaneAsync(IPage page)
    {
        var pane = await PaneBoxAsync(page);
        await page.Mouse.ClickAsync((float)pane.X + 30, (float)pane.Y + 30);
        await EventuallyAsync(() => page.EvaluateAsync<bool>("() => document.activeElement?.classList.contains('bit-blow') === true"),
            "a click on the background focused the pane");
    }
}
