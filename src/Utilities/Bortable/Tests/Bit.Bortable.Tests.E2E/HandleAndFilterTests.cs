using Bit.Bortable.Tests.E2E.Infrastructure;
using static Microsoft.Playwright.Assertions;

namespace Bit.Bortable.Tests.E2E;

/// <summary>
/// What may start a drag, decided in the browser before any drag event exists: with a <c>Handle</c> only a press
/// on the handle makes the item draggable (the browser layer flips <c>draggable</c> on <c>pointerdown</c>), and an
/// item the <c>Filter</c> excludes is never draggable while the items around it still sort past it.
/// </summary>
[TestClass]
public sealed class HandleAndFilterTests : BortableTest
{
    private static readonly string[] Rows = ["Item 1", "Item 2", "Item 3", "Item 4", "Item 5", "Item 6"];
    private static readonly string[] Filtered = ["Item 1", "Item 2", "Item 3", "Filtered (locked)", "Item 4", "Item 5"];

    [TestMethod]
    public async Task Dragging_a_row_by_its_body_does_not_start_a_drag()
    {
        var page = await OpenAsync("/handle");
        await Expect(page.Locator("#handle-list")).ToHaveClassAsync(HasClass("bit-bortable-has-handle"));

        // Well to the right of the handle, over the row's text and padding.
        await PressAsync(page, (await BoxAsync(Item(page, "handle-list", "Item 1"))).At(0.6, 0.5));
        await MoveAsync(page, (await BoxAsync(Item(page, "handle-list", "Item 4"))).Centre);

        await Expect(page.Locator("#handle-list .demo-ghost")).ToHaveCountAsync(0);
        await AssertLabelsAsync(page, "handle-list", Rows, because: "a drag by the row's body reordered the list");

        await ReleaseAsync(page);
        await AssertOrderAsync(page, "handle-list", "handle-order", Rows);
    }

    [TestMethod]
    public async Task Dragging_a_row_by_its_handle_sorts_the_list()
    {
        var page = await OpenAsync("/handle");

        var handle = Item(page, "handle-list", "Item 1").Locator(".handle");
        await PressAsync(page, (await BoxAsync(handle)).Centre);
        await MoveAsync(page, (await BoxAsync(Item(page, "handle-list", "Item 4"))).Centre);
        await Expect(Item(page, "handle-list", "Item 1")).ToHaveClassAsync(HasClass("demo-ghost"));
        await ReleaseAsync(page);

        await AssertOrderAsync(page, "handle-list", "handle-order", "Item 2", "Item 3", "Item 4", "Item 1", "Item 5", "Item 6");

        // The release made the row undraggable again: its body still does not drag.
        await Expect(Item(page, "handle-list", "Item 1")).ToHaveAttributeAsync("draggable", "false");
        await DragAsync(page,
            (await BoxAsync(Item(page, "handle-list", "Item 1"))).At(0.6, 0.5),
            (await BoxAsync(Item(page, "handle-list", "Item 6"))).Centre);
        await AssertOrderAsync(page, "handle-list", "handle-order", "Item 2", "Item 3", "Item 4", "Item 1", "Item 5", "Item 6");
    }

    [TestMethod]
    public async Task A_filtered_item_cannot_be_dragged()
    {
        var page = await OpenAsync("/filter");
        var locked = Item(page, "filter-list", "Filtered (locked)");
        await Expect(locked).ToHaveClassAsync(HasClass("bit-bortable-filtered"));
        await Expect(locked).ToHaveAttributeAsync("draggable", "false");

        await PressAsync(page, (await BoxAsync(locked)).Centre);
        await MoveAsync(page, (await BoxAsync(Item(page, "filter-list", "Item 1"))).Centre);
        await Expect(page.Locator("#filter-list .demo-ghost")).ToHaveCountAsync(0);
        await ReleaseAsync(page);

        await AssertOrderAsync(page, "filter-list", "filter-order", Filtered);
    }

    [TestMethod]
    public async Task The_other_items_sort_around_a_filtered_item()
    {
        var page = await OpenAsync("/filter");

        // Up past the locked item, to the top.
        await DragAsync(page,
            (await BoxAsync(Item(page, "filter-list", "Item 5"))).Centre,
            (await BoxAsync(Item(page, "filter-list", "Item 1"))).Centre);
        await AssertOrderAsync(page, "filter-list", "filter-order", "Item 5", "Item 1", "Item 2", "Item 3", "Filtered (locked)", "Item 4");

        // Down past it again, to the slot below it.
        await DragAsync(page,
            (await BoxAsync(Item(page, "filter-list", "Item 2"))).Centre,
            (await BoxAsync(Item(page, "filter-list", "Item 4"))).Centre);
        await AssertOrderAsync(page, "filter-list", "filter-order", "Item 5", "Item 1", "Item 3", "Filtered (locked)", "Item 4", "Item 2");
    }
}
