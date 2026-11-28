namespace Bit.BlazorUI.Tests.E2E.Extras;

/// <summary>
/// The lifecycle events a real BitSortable drag raises, read off the log of /e2e/sortable/events: which events, on
/// which list, in which order, with which indexes, and how the drag was made. The log puts the newest line first; a line
/// reads <c>[Event @list] item='X' FromId#OldIndex -> ToId#NewIndex (input, flags)</c>.
/// </summary>
[TestClass]
[E2ERetry]
[DoNotParallelize]
public class BitSortableEventsE2ETests : BitSortableE2ETestBase
{
    private const string Url = "/e2e/sortable/events";

    private ILocator Lines => Page.Locator("#events-log .event-log__line");

    [TestMethod]
    public async Task Events_ADragWithinAList_RaisesStartThenUpdateSortAndEnd()
    {
        await OpenAsync(Url);
        await Expect(Page.Locator("#events-log .event-log__empty")).ToBeVisibleAsync();
        await Expect(Lines).ToHaveCountAsync(0);

        await PressAsync(await CentreOfAsync("events-left", "Item 1"));

        // The drag has started, and nothing else has happened yet: the move under the pointer is not an update.
        await MoveAsync(await CentreOfAsync("events-left", "Item 3"));
        await Expect(Lines).ToHaveTextAsync(["[OnStart @events-left] item='Item 1' events-left#0 -> events-left#0 (mouse)"]);

        await ReleaseAsync();

        await Expect(Lines).ToHaveTextAsync([
            "[OnEnd @events-left] item='Item 1' events-left#0 -> events-left#2 (mouse)",
            "[OnSort @events-left] item='Item 1' events-left#0 -> events-left#2 (mouse)",
            "[OnUpdate @events-left] item='Item 1' events-left#0 -> events-left#2 (mouse)",
            "[OnStart @events-left] item='Item 1' events-left#0 -> events-left#0 (mouse)",
        ]);
        await Expect(Page.Locator("#events-log .event-log__empty")).ToHaveCountAsync(0);
        await AssertOrderAsync("events-left", "events-left-order", "Item 2", "Item 3", "Item 1", "Item 4");
    }

    [TestMethod]
    public async Task Events_ADragAcrossLists_RaisesStartThenRemoveAddSortAndEnd()
    {
        await OpenAsync(Url);

        await DragAsync(await CentreOfAsync("events-left", "Item 4"), (await BoxAsync(Item("events-right", "Item A"))).At(0.5, 0.3));

        // Remove on the source, add on the destination, sort on both, then end on the source, all with the same indexes.
        await Expect(Lines).ToHaveTextAsync([
            "[OnEnd @events-left] item='Item 4' events-left#3 -> events-right#0 (mouse)",
            "[OnSort @events-right] item='Item 4' events-left#3 -> events-right#0 (mouse)",
            "[OnSort @events-left] item='Item 4' events-left#3 -> events-right#0 (mouse)",
            "[OnAdd @events-right] item='Item 4' events-left#3 -> events-right#0 (mouse)",
            "[OnRemove @events-left] item='Item 4' events-left#3 -> events-right#0 (mouse)",
            "[OnStart @events-left] item='Item 4' events-left#3 -> events-left#3 (mouse)",
        ]);
        await AssertOrderAsync("events-left", "events-left-order", "Item 1", "Item 2", "Item 3");
        await AssertOrderAsync("events-right", "events-right-order", "Item 4", "Item A", "Item B");
    }

    [TestMethod]
    public async Task Events_ADragThatEndsWhereItStarted_RaisesOnlyStartAndEnd()
    {
        await OpenAsync(Url);

        // Into the other list and back home before the drop: nothing changed, so there is no update, add or remove.
        var home = await BoxAsync(Item("events-right", "Item B"));
        await PressAsync(home.Centre);
        await MoveAsync(await CentreOfAsync("events-left", "Item 2"));
        await MoveAsync(home.At(0.5, 0.3));
        await ReleaseAsync();

        await Expect(Lines).ToHaveTextAsync([
            "[OnEnd @events-right] item='Item B' events-right#1 -> events-right#1 (mouse)",
            "[OnStart @events-right] item='Item B' events-right#1 -> events-right#1 (mouse)",
        ]);
        await AssertOrderAsync("events-right", "events-right-order", "Item A", "Item B");
    }

    [TestMethod]
    public async Task Events_OnChange_IsRaisedForEveryNewPosition()
    {
        await OpenAsync(Url);
        await Page.Locator("#events-log-change").CheckAsync();
        await WaitForQuietAsync();

        await DragAsync(await CentreOfAsync("events-left", "Item 1"), await CentreOfAsync("events-left", "Item 3"));

        await AssertOrderAsync("events-left", "events-left-order", "Item 2", "Item 3", "Item 1", "Item 4");
        // The last position the item took is the one it was dropped in; how many it passed through on the way depends on
        // the pointer's steps.
        await Expect(Lines.Filter(new() { HasText = "[OnChange @events-left]" }).First)
            .ToHaveTextAsync("[OnChange @events-left] item='Item 1' events-left#0 -> events-left#2 (mouse)");

        await Page.Locator("#events-clear").ClickAsync();
        await Expect(Lines).ToHaveCountAsync(0);
        await Expect(Page.Locator("#events-log .event-log__empty")).ToBeVisibleAsync();
    }

    [TestMethod]
    public async Task Events_AKeyboardDragCancelledWithEscape_RaisesStartAndACanceledEnd()
    {
        await OpenAsync(Url);

        await Item("events-left", "Item 2").FocusAsync();
        await PressKeyAsync(" ");
        await PressKeyAsync("ArrowDown");
        await PressKeyAsync("Escape");

        await Expect(Lines).ToHaveTextAsync([
            "[OnEnd @events-left] item='Item 2' events-left#1 -> events-left#1 (keyboard, canceled)",
            "[OnStart @events-left] item='Item 2' events-left#1 -> events-left#1 (keyboard)",
        ]);
        await AssertOrderAsync("events-left", "events-left-order", "Item 1", "Item 2", "Item 3", "Item 4");
    }
}
