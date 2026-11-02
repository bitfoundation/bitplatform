using Bit.Bdnd.Tests.E2E.Infrastructure;
using Microsoft.Playwright;
using static Microsoft.Playwright.Assertions;

namespace Bit.Bdnd.Tests.E2E;

/// <summary>
/// The folder tree: a node dropped on a row's top or bottom half goes before or after it, one dropped below a node's
/// children becomes its last child, a node never goes into its own subtree, and the keyboard reaches the same places.
/// </summary>
[TestClass]
public sealed class TreeTests : BdndTest
{
    private static ILocator Label(IPage page, string name) =>
        page.Locator(".tree-label:not([data-bit-bdnd-preview] *):not([data-bit-bdnd-preview])", new() { HasTextRegex = new($"^\\W*{name}$") });

    private static ILocator Row(IPage page, string name) => page.Locator(".tree-row", new() { Has = Label(page, name) });

    private static ILocator Outline(IPage page) => page.Locator(".tree-outline");

    [TestMethod]
    public async Task A_node_dropped_on_the_top_half_of_a_row_goes_before_it()
    {
        var page = await OpenAsync("/nesting/tree");
        var documents = await BoxAsync(Label(page, "Documents"));

        await StartDragAsync(page, Label(page, "Letters"));
        await DragToAsync(page, new Point(documents.Centre.X, documents.Y + documents.Height * 0.2),
            () => Expect(Row(page, "Documents")).ToHaveAttributeAsync("data-bit-bdnd-edge", "top"));

        await Expect(Outline(page)).ToHaveTextAsync("Letters, Documents (Invoices), Photos (2025, 2026), Music");
    }

    [TestMethod]
    public async Task A_node_dropped_below_the_children_of_a_node_becomes_its_last_child()
    {
        var page = await OpenAsync("/nesting/tree");
        var children = page.Locator(".tree-children", new() { Has = Label(page, "Letters") });

        await StartDragAsync(page, Label(page, "Music"));
        // while a node is dragged the children lists grow a strip below their last child
        await Expect(children).ToHaveAttributeAsync("data-bit-bdnd-can-drop", "");
        var box = await BoxAsync(children);
        await DragToAsync(page, new Point(box.Centre.X, box.Bottom - 4),
            () => Expect(children).ToHaveAttributeAsync("data-bit-bdnd-over-shallow", ""));

        await Expect(Outline(page)).ToHaveTextAsync("Documents (Invoices, Letters, Music), Photos (2025, 2026)");
    }

    [TestMethod]
    public async Task A_node_never_goes_into_its_own_subtree()
    {
        var page = await OpenAsync("/nesting/tree");

        await StartDragAsync(page, Label(page, "Photos"));
        // measured once the drag began: the children lists grow a strip then, and the rows move down
        await Expect(page.Locator(".tree-children[data-bit-bdnd-can-drop]").First).ToBeVisibleAsync();
        await WaitForQuietAsync(page, 100);
        var child = await BoxAsync(Label(page, "2025"));
        await MoveAsync(page, child.Centre);
        await WaitForQuietAsync(page, 100);
        // its child's row shows no insertion line and takes no drop
        await Expect(Row(page, "2025")).Not.ToHaveAttributeAsync("data-bit-bdnd-edge", new System.Text.RegularExpressions.Regex(".*"));
        await Expect(Row(page, "2025")).Not.ToHaveAttributeAsync("data-bit-bdnd-can-drop", "");
        await DragToAsync(page, child.Centre);

        await Expect(Outline(page)).ToHaveTextAsync("Documents (Invoices, Letters), Photos (2025, 2026), Music");
    }

    [TestMethod]
    public async Task The_keyboard_moves_a_node_into_another()
    {
        var page = await OpenAsync("/nesting/tree");
        var live = page.Locator("#bit-bdnd-live-region");
        await WaitForBdndAsync(page.MainFrame);
        await Label(page, "Music").FocusAsync();

        await page.Keyboard.PressAsync("Space");
        await page.Keyboard.PressAsync("ArrowUp");
        await Expect(live).ToHaveTextAsync("Music is over the end of Photos.");
        await page.Keyboard.PressAsync("ArrowUp");
        await Expect(live).ToHaveTextAsync("Music is over the end of 2026.");
        await page.Keyboard.PressAsync("ArrowDown");
        await page.Keyboard.PressAsync("Enter");

        await Expect(Outline(page)).ToHaveTextAsync("Documents (Invoices, Letters), Photos (2025, 2026, Music)");
        await EventuallyAsync(() => Label(page, "Music").EvaluateAsync<bool>("el => document.activeElement === el"), "the moved node keeps the focus");
    }
}
