using Bit.Bdnd.Tests.E2E.Infrastructure;
using Microsoft.Playwright;
using static Microsoft.Playwright.Assertions;

namespace Bit.Bdnd.Tests.E2E;

/// <summary>
/// Sortable lists: every card is both a source and a target, and reorders the list from its <c>OnHover</c> as the
/// dragged card passes its middle, measuring itself in the browser to know where that is. The list around the cards
/// is a target too, so a drop in a gap still counts, and a drop outside it is the one that reverts the order.
/// </summary>
[TestClass]
public sealed class SortableTests : BdndTest
{
    [TestMethod]
    public async Task Dragging_a_card_past_the_middle_of_the_next_ones_reorders_the_list_while_it_hovers()
    {
        var page = await OpenAsync("/sortable/simple");
        var order = page.Locator(".sortable-order");
        await Expect(order).ToHaveTextAsync("Order: Write the docs, Review the PR, Ship the release, Celebrate, Sleep");

        var first = Card(page, 1);
        var third = await BoxAsync(Card(page, 3));
        await StartDragAsync(page, first);
        await Expect(first).ToHaveCSSAsync("opacity", "0.2");

        // Into the lower half of the third card: past the middle of the second and of the third. The list reorders
        // while the card is still in the air.
        var target = new Point(third.Centre.X, third.Y + third.Height * 0.75);
        const string Reordered = "Order: Review the PR, Ship the release, Write the docs, Celebrate, Sleep";
        await HoverUntilAsync(page, target, async () => await order.TextContentAsync() == Reordered, "the list reordered");
        CollectionAssert.AreEqual(new[] { "2", "3", "1", "4", "5" }, await CardOrderAsync(page), "the cards on screen");

        // Dropped on the card it now sits under, it stays there.
        await DragToAsync(page, target);
        await Expect(first).ToHaveCSSAsync("opacity", "1");
        await Expect(order).ToHaveTextAsync(Reordered);
        CollectionAssert.AreEqual(new[] { "2", "3", "1", "4", "5" }, await CardOrderAsync(page), "the cards after the drop");
    }

    [TestMethod]
    public async Task A_card_dropped_outside_the_list_goes_back_to_where_it_started()
    {
        var page = await OpenAsync("/sortable/cancel-on-drop-outside");
        var order = page.Locator(".sortable-order");
        // The cards span the list's width.
        var card = await BoxAsync(Card(page, 1));

        await DragAlphaBelowCharlieAsync(page, order);

        // Out of the list, to the empty stage on its right, and released there: no target took the drop, so OnEnd
        // sees DidDrop false and puts the card back.
        await DragToAsync(page, new Point(card.Right + 200, card.Centre.Y));
        await Expect(order).ToHaveTextAsync("Order: Alpha, Bravo, Charlie, Delta, Echo");
        CollectionAssert.AreEqual(new[] { "1", "2", "3", "4", "5" }, await CardOrderAsync(page), "the cards after the cancel");
        await Expect(Card(page, 1)).ToHaveCSSAsync("opacity", "1");
    }

    [TestMethod]
    public async Task A_card_dropped_in_a_gap_of_the_list_keeps_its_new_place()
    {
        var page = await OpenAsync("/sortable/cancel-on-drop-outside");
        var order = page.Locator(".sortable-order");

        await DragAlphaBelowCharlieAsync(page, order);

        // Up into the gap between Charlie and Alpha, where it now sits: no card is hovered there (so nothing
        // reorders), but the list is, and it takes the drop.
        var charlie = await BoxAsync(Card(page, 3));
        var alpha = await BoxAsync(Card(page, 1));
        Assert.IsGreaterThan(charlie.Bottom + 2, alpha.Y, "there is no gap between the cards");
        await DragToAsync(page, new Point(alpha.Centre.X, (charlie.Bottom + alpha.Y) / 2));

        await Expect(Card(page, 1)).ToHaveCSSAsync("opacity", "1");
        await Expect(order).ToHaveTextAsync("Order: Bravo, Charlie, Alpha, Delta, Echo");
        Assert.AreEqual(1, (await DropsAsync(page.MainFrame)).Count, "the drop in the gap");
    }

    private static async Task DragAlphaBelowCharlieAsync(IPage page, ILocator order)
    {
        await Expect(order).ToHaveTextAsync("Order: Alpha, Bravo, Charlie, Delta, Echo");
        var charlie = await BoxAsync(Card(page, 3));
        await StartDragAsync(page, Card(page, 1));
        await HoverUntilAsync(page, new Point(charlie.Centre.X, charlie.Y + charlie.Height * 0.75),
            async () => await order.TextContentAsync() == "Order: Bravo, Charlie, Alpha, Delta, Echo", "Alpha moved below Charlie");
    }

    private static ILocator Card(IPage page, int id) => page.Locator($".sortable-card[data-card=\"{id}\"]");

    private static Task<string[]> CardOrderAsync(IPage page) => page.EvaluateAsync<string[]>(
        "() => [...document.querySelectorAll('.sortable-card')].map(c => c.dataset.card)");
}
