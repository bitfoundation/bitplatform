using Bit.Bortable.Tests.E2E.Infrastructure;
using static Microsoft.Playwright.Assertions;

namespace Bit.Bortable.Tests.E2E;

/// <summary>
/// The two SortableJS plugins the library ports, driven with real clicks and drags: MultiDrag (click to select,
/// drag one selected item to move them all as a block, within a list and across lists) and Swap (the item under
/// the pointer is highlighted, and on drop the two trade places, within a list and across lists).
/// </summary>
[TestClass]
public sealed class PluginTests : BortableTest
{
    private const string Selected = "bit-bortable-selected";
    private const string SwapHighlight = "bit-bortable-swap-highlight";

    private static readonly string[] Six = ["Item 1", "Item 2", "Item 3", "Item 4", "Item 5", "Item 6"];

    // ---- MultiDrag ---------------------------------------------------------------------------------

    [TestMethod]
    public async Task Clicking_items_toggles_their_multi_drag_selection()
    {
        var page = await OpenAsync("/multi-drag");

        await Item(page, "multi-list", "Item 2").ClickAsync();
        await Item(page, "multi-list", "Item 4").ClickAsync();
        await Expect(page.Locator($"#multi-list > .{Selected}")).ToHaveTextAsync(["Item 2", "Item 4"]);

        // A second click deselects; the selection belongs to its list, so the lists below are untouched.
        await Item(page, "multi-list", "Item 2").ClickAsync();
        await Expect(page.Locator($"#multi-list > .{Selected}")).ToHaveTextAsync(["Item 4"]);
        await Expect(page.Locator($"#multi-left > .{Selected}, #multi-right > .{Selected}")).ToHaveCountAsync(0);
    }

    [TestMethod]
    public async Task Dragging_a_selected_item_moves_the_whole_selection_as_a_block()
    {
        var page = await OpenAsync("/multi-drag");
        await Item(page, "multi-list", "Item 2").ClickAsync();
        await Item(page, "multi-list", "Item 4").ClickAsync();
        await Expect(page.Locator($"#multi-list > .{Selected}")).ToHaveCountAsync(2);

        await DragAsync(page,
            (await BoxAsync(Item(page, "multi-list", "Item 2"))).Centre,
            (await BoxAsync(Item(page, "multi-list", "Item 6"))).Centre);

        // Item 2 lands where it was dropped, Item 4 joins it, in their original order, and the selection is spent.
        await AssertOrderAsync(page, "multi-list", "multi-order", "Item 1", "Item 3", "Item 5", "Item 6", "Item 2", "Item 4");
        await Expect(page.Locator($"#multi-list > .{Selected}")).ToHaveCountAsync(0);
    }

    [TestMethod]
    public async Task A_selection_moves_as_a_block_into_another_list()
    {
        var page = await OpenAsync("/multi-drag");
        await Item(page, "multi-left", "Item 1").ClickAsync();
        await Item(page, "multi-left", "Item 3").ClickAsync();
        await Expect(page.Locator($"#multi-left > .{Selected}")).ToHaveCountAsync(2);

        await DragAsync(page,
            (await BoxAsync(Item(page, "multi-left", "Item 1"))).Centre,
            (await BoxAsync(Item(page, "multi-right", "Item C"))).At(0.5, 0.3));

        await AssertOrderAsync(page, "multi-left", "multi-left-order", "Item 2", "Item 4");
        await AssertOrderAsync(page, "multi-right", "multi-right-order", "Item A", "Item B", "Item 1", "Item 3", "Item C", "Item D");
        await Expect(page.Locator($".{Selected}")).ToHaveCountAsync(0);
    }

    [TestMethod]
    public async Task Dragging_an_unselected_item_drops_the_selection_and_moves_only_that_item()
    {
        var page = await OpenAsync("/multi-drag");
        await Item(page, "multi-list", "Item 2").ClickAsync();
        await Item(page, "multi-list", "Item 4").ClickAsync();
        await Expect(page.Locator($"#multi-list > .{Selected}")).ToHaveCountAsync(2);

        await PressAsync(page, (await BoxAsync(Item(page, "multi-list", "Item 5"))).Centre);
        await Expect(page.Locator($"#multi-list > .{Selected}")).ToHaveCountAsync(0);
        await MoveAsync(page, (await BoxAsync(Item(page, "multi-list", "Item 1"))).Centre);
        await ReleaseAsync(page);

        await AssertOrderAsync(page, "multi-list", "multi-order", "Item 5", "Item 1", "Item 2", "Item 3", "Item 4", "Item 6");
        await Expect(page.Locator($"#multi-list > .{Selected}")).ToHaveCountAsync(0);
    }

    // ---- Swap --------------------------------------------------------------------------------------

    [TestMethod]
    public async Task Swap_highlights_the_item_under_the_pointer_and_trades_places_with_it_on_drop()
    {
        var page = await OpenAsync("/swap");

        await PressAsync(page, (await BoxAsync(Item(page, "swap-list", "Item 1"))).Centre);
        await MoveAsync(page, (await BoxAsync(Item(page, "swap-list", "Item 4"))).Centre);

        // Mid-drag: only Item 4 is highlighted, and nothing has moved, not even the items the pointer passed.
        await Expect(page.Locator($".{SwapHighlight}")).ToHaveTextAsync(["Item 4"]);
        await Expect(Item(page, "swap-list", "Item 4")).ToHaveClassAsync(HasClass(SwapHighlight));
        await AssertLabelsAsync(page, "swap-list", Six);

        // On to Item 5: the highlight follows the pointer.
        await MoveAsync(page, (await BoxAsync(Item(page, "swap-list", "Item 5"))).Centre, steps: 4);
        await Expect(page.Locator($".{SwapHighlight}")).ToHaveTextAsync(["Item 5"]);

        await ReleaseAsync(page);

        await AssertOrderAsync(page, "swap-list", "swap-order", "Item 5", "Item 2", "Item 3", "Item 4", "Item 1", "Item 6");
        await Expect(page.Locator($".{SwapHighlight}")).ToHaveCountAsync(0);
    }

    [TestMethod]
    public async Task Swap_trades_places_across_lists()
    {
        var page = await OpenAsync("/swap");

        await PressAsync(page, (await BoxAsync(Item(page, "swap-left", "Item 2"))).Centre);
        await MoveAsync(page, (await BoxAsync(Item(page, "swap-right", "Item C"))).Centre);
        await Expect(Item(page, "swap-right", "Item C")).ToHaveClassAsync(HasClass(SwapHighlight));
        await Expect(page.Locator($".{SwapHighlight}")).ToHaveCountAsync(1);
        await Expect(page.Locator("#swap-right > [data-bortable-placeholder]")).ToHaveCountAsync(0);
        await ReleaseAsync(page);

        await AssertOrderAsync(page, "swap-left", "swap-left-order", "Item 1", "Item C", "Item 3", "Item 4");
        await AssertOrderAsync(page, "swap-right", "swap-right-order", "Item A", "Item B", "Item 2", "Item D");

        // The single list above is a group of its own: it is left alone.
        await AssertOrderAsync(page, "swap-list", "swap-order", Six);
    }

    [TestMethod]
    public async Task A_swap_drag_released_over_no_item_changes_nothing()
    {
        var page = await OpenAsync("/swap");

        await PressAsync(page, (await BoxAsync(Item(page, "swap-list", "Item 2"))).Centre);
        await MoveAsync(page, (await BoxAsync(Item(page, "swap-list", "Item 3"))).Centre);
        await Expect(Item(page, "swap-list", "Item 3")).ToHaveClassAsync(HasClass(SwapHighlight));

        // Back over the dragged item itself: no target is left highlighted, so the drop swaps nothing.
        await MoveAsync(page, (await BoxAsync(Item(page, "swap-list", "Item 2"))).Centre);
        await Expect(page.Locator($".{SwapHighlight}")).ToHaveCountAsync(0);
        await ReleaseAsync(page);

        await AssertOrderAsync(page, "swap-list", "swap-order", Six);
    }
}
