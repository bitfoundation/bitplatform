using Bit.Bortable.Tests.E2E.Infrastructure;
using static Microsoft.Playwright.Assertions;

namespace Bit.Bortable.Tests.E2E;

/// <summary>
/// <c>Sort="false"</c> and <c>Put="false"</c> with real drags: the source list cannot be reordered and refuses
/// items, yet its items can still be dragged out into a list that takes them.
/// </summary>
[TestClass]
public sealed class DisableSortTests : BortableTest
{
    private static readonly string[] Source = ["Item 1", "Item 2", "Item 3", "Item 4", "Item 5"];
    private static readonly string[] Target = ["Item A", "Item B", "Item C"];

    [TestMethod]
    public async Task A_list_without_sorting_keeps_its_order_while_an_item_is_dragged_over_it()
    {
        var page = await OpenAsync("/disable-sort");

        await PressAsync(page, (await BoxAsync(Item(page, "disable-sort-source", "Item 1"))).Centre);
        await MoveAsync(page, (await BoxAsync(Item(page, "disable-sort-source", "Item 4"))).Centre);

        // The drag is running (the item wears the ghost), but the list does not reorder under it.
        await Expect(Item(page, "disable-sort-source", "Item 1")).ToHaveClassAsync(HasClass("demo-ghost"));
        await AssertLabelsAsync(page, "disable-sort-source", Source);

        await ReleaseAsync(page);
        await AssertOrderAsync(page, "disable-sort-source", "disable-sort-source-order", Source);
    }

    [TestMethod]
    public async Task An_item_of_a_list_without_sorting_can_still_be_dragged_out()
    {
        var page = await OpenAsync("/disable-sort");

        await DragAsync(page,
            (await BoxAsync(Item(page, "disable-sort-source", "Item 2"))).Centre,
            (await BoxAsync(Item(page, "disable-sort-target", "Item B"))).At(0.5, 0.3));

        await AssertOrderAsync(page, "disable-sort-source", "disable-sort-source-order", "Item 1", "Item 3", "Item 4", "Item 5");
        await AssertOrderAsync(page, "disable-sort-target", "disable-sort-target-order", "Item A", "Item 2", "Item B", "Item C");
    }

    [TestMethod]
    public async Task A_list_that_does_not_put_refuses_items_from_the_other_list()
    {
        var page = await OpenAsync("/disable-sort");

        // Straight across, along Item A's own row, so that its home list has nothing to reorder on the way.
        await PressAsync(page, (await BoxAsync(Item(page, "disable-sort-target", "Item A"))).Centre);
        await MoveAsync(page, (await BoxAsync(Item(page, "disable-sort-source", "Item 1"))).Centre);
        await Expect(Placeholder(page, "disable-sort-source")).ToHaveCountAsync(0);
        await Expect(Item(page, "disable-sort-target", "Item A")).Not.ToHaveClassAsync(HasClass("bit-bortable-hidden"));

        await ReleaseAsync(page);
        await AssertOrderAsync(page, "disable-sort-source", "disable-sort-source-order", Source);
        await AssertOrderAsync(page, "disable-sort-target", "disable-sort-target-order", Target);
    }
}
