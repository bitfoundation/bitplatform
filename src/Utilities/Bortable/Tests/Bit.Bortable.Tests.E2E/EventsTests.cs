using Bit.Bortable.Tests.E2E.Infrastructure;
using Microsoft.Playwright;
using static Microsoft.Playwright.Assertions;

namespace Bit.Bortable.Tests.E2E;

/// <summary>
/// The SortableJS lifecycle events a real drag raises, read off the demo's event log: which events, on which
/// list, in which order, with which indices. The log puts the newest line first; a line reads
/// <c>[event] item='X' FromId#OldIndex -> ToId#NewIndex</c>.
/// </summary>
[TestClass]
public sealed class EventsTests : BortableTest
{
    [TestMethod]
    public async Task A_drag_within_a_list_raises_start_then_update_and_end()
    {
        var page = await OpenAsync("/events");
        await Expect(page.Locator("#events-log .event-log__empty")).ToBeVisibleAsync();
        await Expect(Lines(page)).ToHaveCountAsync(0);

        await PressAsync(page, (await BoxAsync(Item(page, "events-left", "Item 1"))).Centre);

        // The drag has started, and nothing else has happened yet: the move under the pointer is not an update.
        await MoveAsync(page, (await BoxAsync(Item(page, "events-left", "Item 3"))).Centre);
        await Expect(Lines(page)).ToHaveTextAsync(["[start] item='Item 1' events-left#0 -> events-left#0"]);

        await ReleaseAsync(page);

        await Expect(Lines(page)).ToHaveTextAsync([
            "[end] item='Item 1' events-left#0 -> events-left#2",
            "[update] item='Item 1' events-left#0 -> events-left#2",
            "[start] item='Item 1' events-left#0 -> events-left#0",
        ]);
        await Expect(page.Locator("#events-log .event-log__empty")).ToHaveCountAsync(0);
        await AssertOrderAsync(page, "events-left", "events-left-order", "Item 2", "Item 3", "Item 1", "Item 4");
    }

    [TestMethod]
    public async Task A_drag_across_lists_raises_start_then_remove_add_and_end()
    {
        var page = await OpenAsync("/events");

        await DragAsync(page,
            (await BoxAsync(Item(page, "events-left", "Item 4"))).Centre,
            (await BoxAsync(Item(page, "events-right", "Item A"))).At(0.5, 0.3));

        // Remove on the source, add on the destination, then end on the source, all with the same indices.
        await Expect(Lines(page)).ToHaveTextAsync([
            "[end] item='Item 4' events-left#3 -> events-right#0",
            "[add] item='Item 4' events-left#3 -> events-right#0",
            "[remove] item='Item 4' events-left#3 -> events-right#0",
            "[start] item='Item 4' events-left#3 -> events-left#3",
        ]);
        await AssertOrderAsync(page, "events-left", "events-left-order", "Item 1", "Item 2", "Item 3");
        await AssertOrderAsync(page, "events-right", "events-right-order", "Item 4", "Item A", "Item B");
    }

    [TestMethod]
    public async Task A_drag_that_ends_where_it_started_raises_only_start_and_end()
    {
        var page = await OpenAsync("/events");

        // Into the other list and back home before the drop: nothing changed, so there is no update, add or remove.
        var home = await BoxAsync(Item(page, "events-right", "Item B"));
        await PressAsync(page, home.Centre);
        await MoveAsync(page, (await BoxAsync(Item(page, "events-left", "Item 2"))).Centre);
        await MoveAsync(page, home.At(0.5, 0.3));
        await ReleaseAsync(page);

        await Expect(Lines(page)).ToHaveTextAsync([
            "[end] item='Item B' events-right#1 -> events-right#1",
            "[start] item='Item B' events-right#1 -> events-right#1",
        ]);
        await AssertOrderAsync(page, "events-right", "events-right-order", "Item A", "Item B");
    }

    private static ILocator Lines(IPage page) => page.Locator("#events-log .event-log__line");
}
