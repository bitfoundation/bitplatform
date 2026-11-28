namespace Bit.BlazorUI.Tests.E2E.Extras;

/// <summary>
/// What may start a BitSortable drag, decided in the browser before any drag event exists, on /e2e/sortable/handle:
/// with a Handle only a press on the handle makes the item draggable (the script flips <c>draggable</c> on
/// pointerdown), and an item the Filter locks is never draggable while the items around it still sort past it.
/// </summary>
[TestClass]
[E2ERetry]
[DoNotParallelize]
public class BitSortableHandleE2ETests : BitSortableE2ETestBase
{
    private const string Url = "/e2e/sortable/handle";
    private static readonly string[] Rows = ["Item 1", "Item 2", "Item 3", "Item 4", "Item 5", "Item 6"];

    [TestMethod]
    public async Task Handle_DraggingARowByItsBody_StartsNoDrag()
    {
        await OpenAsync(Url);
        await Expect(Page.Locator("#handle-list")).ToHaveClassAsync(HasClass("bit-srt-hnd"));
        await Expect(Item("handle-list", "Item 1")).ToHaveAttributeAsync("draggable", "false");

        // Well to the right of the handle, over the row's text and padding.
        await PressAsync((await BoxAsync(Item("handle-list", "Item 1"))).At(0.6, 0.5));
        await MoveAsync(await CentreOfAsync("handle-list", "Item 4"));

        await Expect(Page.Locator("#handle-list .bit-srt-gst")).ToHaveCountAsync(0);
        await AssertLabelsAsync("handle-list", Rows, because: "a drag by the row's body reordered the list");

        await ReleaseAsync();
        await AssertOrderAsync("handle-list", "handle-order", Rows);
    }

    [TestMethod]
    public async Task Handle_DraggingARowByItsHandle_SortsTheList()
    {
        await OpenAsync(Url);

        var handle = Item("handle-list", "Item 1").Locator(".handle");
        await PressAsync((await BoxAsync(handle)).Centre);
        await MoveAsync(await CentreOfAsync("handle-list", "Item 4"));
        await Expect(Item("handle-list", "Item 1")).ToHaveClassAsync(HasClass("bit-srt-gst"));
        await ReleaseAsync();

        await AssertOrderAsync("handle-list", "handle-order", "Item 2", "Item 3", "Item 4", "Item 1", "Item 5", "Item 6");

        // The release made the row undraggable again: its body still does not drag.
        await Expect(Item("handle-list", "Item 1")).ToHaveAttributeAsync("draggable", "false");
        await DragAsync((await BoxAsync(Item("handle-list", "Item 1"))).At(0.6, 0.5), await CentreOfAsync("handle-list", "Item 6"));
        await AssertOrderAsync("handle-list", "handle-order", "Item 2", "Item 3", "Item 4", "Item 1", "Item 5", "Item 6");
    }

    [TestMethod]
    public async Task Filter_AFilteredItem_CannotBeDragged()
    {
        await OpenAsync(Url);
        var locked = Item("filter-list", "Filtered (locked)");
        await Expect(locked).ToHaveClassAsync(HasClass("bit-srt-flt"));
        await Expect(locked).ToHaveAttributeAsync("draggable", "false");
        await Expect(locked).ToHaveAttributeAsync("data-bit-srt-disabled", "true");

        await PressAsync((await BoxAsync(locked)).Centre);
        await MoveAsync(await CentreOfAsync("filter-list", "Item 1"));
        await Expect(Page.Locator("#filter-list .bit-srt-gst")).ToHaveCountAsync(0);
        await ReleaseAsync();

        await AssertOrderAsync("filter-list", "filter-order", "Item 1", "Item 2", "Item 3", "Filtered (locked)", "Item 4", "Item 5");
    }

    [TestMethod]
    public async Task Filter_TheOtherItems_SortAroundAFilteredItem()
    {
        await OpenAsync(Url);

        // Up past the locked item, to the top.
        await DragAsync(await CentreOfAsync("filter-list", "Item 5"), await CentreOfAsync("filter-list", "Item 1"));
        await AssertOrderAsync("filter-list", "filter-order", "Item 5", "Item 1", "Item 2", "Item 3", "Filtered (locked)", "Item 4");

        // Down past it again, to the slot below it.
        await DragAsync(await CentreOfAsync("filter-list", "Item 2"), await CentreOfAsync("filter-list", "Item 4"));
        await AssertOrderAsync("filter-list", "filter-order", "Item 5", "Item 1", "Item 3", "Filtered (locked)", "Item 4", "Item 2");
    }

    [TestMethod]
    public async Task Axe_EachSection_HasNoWcagAOrAAViolation()
    {
        await OpenAsync(Url);

        await AssertNoAxeViolationsAsync("#handle");
    }

    [TestMethod]
    public async Task Axe_AFilteredItem_HasNoWcagAOrAAViolation()
    {
        await OpenAsync(Url);

        await AssertNoAxeViolationsAsync("#filter");
    }
}
