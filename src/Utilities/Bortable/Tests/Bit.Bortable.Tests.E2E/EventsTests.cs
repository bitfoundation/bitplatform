using Bit.Bortable.Tests.E2E.Infrastructure;
using Microsoft.Playwright;
using static Microsoft.Playwright.Assertions;

namespace Bit.Bortable.Tests.E2E;

/// <summary>
/// The SortableJS lifecycle events a real drag raises, read off the demo's event log: which events, on which
/// list, in which order, with which indices, and how the drag was made. The log puts the newest line first; a line
/// reads <c>[Event @list] item='X' FromId#OldIndex -> ToId#NewIndex (input, flags)</c>.
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
        await Expect(Lines(page)).ToHaveTextAsync(["[OnStart @events-left] item='Item 1' events-left#0 -> events-left#0 (mouse)"]);

        await ReleaseAsync(page);

        await Expect(Lines(page)).ToHaveTextAsync([
            "[OnEnd @events-left] item='Item 1' events-left#0 -> events-left#2 (mouse)",
            "[OnSort @events-left] item='Item 1' events-left#0 -> events-left#2 (mouse)",
            "[OnUpdate @events-left] item='Item 1' events-left#0 -> events-left#2 (mouse)",
            "[OnStart @events-left] item='Item 1' events-left#0 -> events-left#0 (mouse)",
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

        // Remove on the source, add on the destination, sort on both, then end on the source, all with the same indices.
        await Expect(Lines(page)).ToHaveTextAsync([
            "[OnEnd @events-left] item='Item 4' events-left#3 -> events-right#0 (mouse)",
            "[OnSort @events-right] item='Item 4' events-left#3 -> events-right#0 (mouse)",
            "[OnSort @events-left] item='Item 4' events-left#3 -> events-right#0 (mouse)",
            "[OnAdd @events-right] item='Item 4' events-left#3 -> events-right#0 (mouse)",
            "[OnRemove @events-left] item='Item 4' events-left#3 -> events-right#0 (mouse)",
            "[OnStart @events-left] item='Item 4' events-left#3 -> events-left#3 (mouse)",
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
            "[OnEnd @events-right] item='Item B' events-right#1 -> events-right#1 (mouse)",
            "[OnStart @events-right] item='Item B' events-right#1 -> events-right#1 (mouse)",
        ]);
        await AssertOrderAsync(page, "events-right", "events-right-order", "Item A", "Item B");
    }

    [TestMethod]
    public async Task OnChange_is_logged_on_request_for_every_new_position()
    {
        var page = await OpenAsync("/events");
        await page.Locator("#events-log-change").CheckAsync();
        await WaitForQuietAsync(page);

        await DragAsync(page,
            (await BoxAsync(Item(page, "events-left", "Item 1"))).Centre,
            (await BoxAsync(Item(page, "events-left", "Item 3"))).Centre);

        await AssertOrderAsync(page, "events-left", "events-left-order", "Item 2", "Item 3", "Item 1", "Item 4");
        // The last position the item took is the one it was dropped in; how many it passed through on the way depends on
        // the pointer's steps.
        await Expect(Lines(page).Filter(new LocatorFilterOptions { HasText = "[OnChange @events-left]" }).First)
            .ToHaveTextAsync("[OnChange @events-left] item='Item 1' events-left#0 -> events-left#2 (mouse)");

        await page.Locator("#events-clear").ClickAsync();
        await Expect(Lines(page)).ToHaveCountAsync(0);
        await Expect(page.Locator("#events-log .event-log__empty")).ToBeVisibleAsync();
    }

    [TestMethod]
    public async Task A_keyboard_drag_cancelled_with_Escape_raises_start_and_a_canceled_end()
    {
        var page = await OpenAsync("/events");

        await Item(page, "events-left", "Item 2").FocusAsync();
        await PressKeyAsync(page, " ");
        await PressKeyAsync(page, "ArrowDown");
        await PressKeyAsync(page, "Escape");

        await Expect(Lines(page)).ToHaveTextAsync([
            "[OnEnd @events-left] item='Item 2' events-left#1 -> events-left#1 (keyboard, canceled)",
            "[OnStart @events-left] item='Item 2' events-left#1 -> events-left#1 (keyboard)",
        ]);
        await AssertOrderAsync(page, "events-left", "events-left-order", "Item 1", "Item 2", "Item 3", "Item 4");
    }

    private static ILocator Lines(IPage page) => page.Locator("#events-log .event-log__line");

    /// <summary>Presses a key and waits until the page has acted on it.</summary>
    private static async Task PressKeyAsync(IPage page, string key)
    {
        await page.Keyboard.PressAsync(key);
        await WaitForQuietAsync(page);
    }
}
