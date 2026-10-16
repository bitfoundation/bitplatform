using System.Text.RegularExpressions;

namespace Bit.BlazorUI.Tests.E2E.Extras;

/// <summary>
/// The pointer and keyboard gestures of BitFullCalendar: dragging an event to another day or slot, resizing it by
/// either edge with the real mouse, the keyboard equivalents (Alt+Arrow moves, Shift+Arrow resizes), and the roving
/// focus of every grid and tab strip.
/// </summary>
public partial class BitFullCalendarE2ETests
{
    /// <summary>
    /// Presses the mouse on <paramref name="handle"/>, moves it by (<paramref name="dx"/>, <paramref name="dy"/>) in
    /// steps, waits for the live resize preview and releases it.
    /// </summary>
    private async Task DragByMouseAsync(ILocator handle, float dx, float dy)
    {
        await handle.ScrollIntoViewIfNeededAsync();
        var box = (await handle.BoundingBoxAsync())!;
        var x = box.X + box.Width / 2;
        var y = box.Y + box.Height / 2;

        await Page.Mouse.MoveAsync(x, y);
        await Page.Mouse.DownAsync();
        await Page.Mouse.MoveAsync(x + dx / 2, y + dy / 2, new() { Steps = 6 });
        await Page.Mouse.MoveAsync(x + dx, y + dy, new() { Steps = 6 });
        // the preview follows the pointer before anything is committed
        await Expect(Page.Locator(".bit-bfc-resize-preview").First).ToBeVisibleAsync();
        await Page.Mouse.UpAsync();
    }

    private Task ExpectFocusedLabelAsync(string label) => Expect(Page.Locator(":focus")).ToHaveAttributeAsync("aria-label", label);

    /// <summary>
    /// Drags <paramref name="source"/> onto <paramref name="target"/> the way a hand does: press, start moving, and only
    /// once the calendar has taken the drag (the other events let the pointer through to the slots under them) carry
    /// it to the target and drop it there.
    /// </summary>
    private async Task DragWithMouseAsync(ILocator calendar, ILocator source, ILocator target)
    {
        await source.ScrollIntoViewIfNeededAsync();
        var from = (await source.BoundingBoxAsync())!;
        var x = from.X + from.Width / 2;
        var y = from.Y + from.Height / 2;

        await Page.Mouse.MoveAsync(x, y);
        await Page.Mouse.DownAsync();
        await Page.Mouse.MoveAsync(x + 6, y + 6, new() { Steps = 3 });
        await Expect(calendar.Locator(".bit-bfc-event-block-pass-through, .bit-bfc-timeline-event-pass-through").First).ToBeAttachedAsync();

        var to = (await target.BoundingBoxAsync())!;
        await Page.Mouse.MoveAsync(to.X + to.Width / 2, to.Y + to.Height / 2, new() { Steps = 8 });
        await Page.Mouse.UpAsync();
    }

    // ---------------------------------------------------------------- drag and drop

    [TestMethod]
    public async Task Drag_AMonthBadgeDroppedOnAnotherDay_MovesTheEventThere()
    {
        await OpenMainAsync();

        await Badge(Cal, "e1").DragToAsync(MonthCell(Cal, "Tuesday, July 22, 2025"), new() { TargetPosition = new() { X = 20, Y = 8 } });

        await Expect(Page.Locator("#last-change")).ToHaveTextAsync(new Regex(@"^Edit\|Drag\|Team Standup\|2025-07-22 09:00\|2025-07-22 09:45\|"));
        await Expect(Page.Locator("#last-change-old")).ToHaveTextAsync(new Regex(@"^Team Standup\|2025-07-16 09:00\|2025-07-16 09:45\|"));
        await Expect(MonthCell(Cal, "Tuesday, July 22, 2025").Locator("[data-bit-bfc-event='e1']")).ToBeVisibleAsync();
        await Expect(MonthCell(Cal, "Wednesday, July 16, 2025").Locator("[data-bit-bfc-event='e1']")).ToHaveCountAsync(0);
    }

    [TestMethod]
    public async Task Drag_AWeekBlockDroppedOnAnotherDaysSlot_MovesItKeepingItsLength()
    {
        await OpenMainAsync("Week");
        var target = Slot(Cal, "Thu 11:30");

        await Block(Cal, "e2").DragToAsync(target, new() { TargetPosition = new() { X = 10, Y = 4 } });

        await Expect(Page.Locator("#last-change")).ToHaveTextAsync(new Regex(@"^Edit\|Drag\|Focus Time\|2025-07-17 11:30\|2025-07-17 12:30\|"));
        await Expect(Page.Locator("#change-count")).ToHaveTextAsync("1");
        await Expect(Block(Cal, "e2").Locator(".bit-bfc-event-block-time")).ToHaveTextAsync("11:30 - 12:30");
    }

    [TestMethod]
    public async Task Drag_AnAllDayBadge_MovesToTheDayItIsDroppedOn()
    {
        await OpenMainAsync();

        await Badge(Cal, "e5").DragToAsync(MonthCell(Cal, "Thursday, July 24, 2025"), new() { TargetPosition = new() { X = 20, Y = 8 } });

        await Expect(Page.Locator("#last-change")).ToHaveTextAsync(new Regex(@"^Edit\|Drag\|Company Holiday\|2025-07-24 00:00\|2025-07-25 00:00\|-\|red\|allday\|"));
    }

    // ---------------------------------------------------------------- resizing

    [TestMethod]
    public async Task Resize_DraggingTheBottomEdgeDown_StretchesTheEndBySnappedSlots()
    {
        await OpenMainAsync("Week");

        await DragByMouseAsync(Block(Cal, "e3").Locator(".bit-bfc-resize-handle-bottom"), 0, 96);

        await Expect(Page.Locator("#last-change")).ToHaveTextAsync(new Regex(@"^Edit\|Resize\|Design Review\|2025-07-16 14:00\|2025-07-16 16:00\|"));
        await Expect(Block(Cal, "e3").Locator(".bit-bfc-event-block-time")).ToHaveTextAsync("14:00 - 16:00");
        await Expect(Cal.Locator(".bit-bfc-resize-preview")).ToHaveCountAsync(0);
        // a resize is not mistaken for a click: no details dialog opened
        await Expect(Dialog(Cal)).ToHaveCountAsync(0);
    }

    [TestMethod]
    public async Task Resize_DraggingTheTopEdgeUp_MovesTheStartEarlier()
    {
        await OpenMainAsync("Week");

        await DragByMouseAsync(Block(Cal, "e3").Locator(".bit-bfc-resize-handle-top"), 0, -96);

        await Expect(Page.Locator("#last-change")).ToHaveTextAsync(new Regex(@"^Edit\|Resize\|Design Review\|2025-07-16 13:00\|2025-07-16 15:00\|"));
    }

    [TestMethod]
    public async Task Resize_TheDayView_ResizesTheSameWay_AndShowsTheLivePreview()
    {
        await OpenMainAsync("Day");
        var handle = Block(Cal, "e2").Locator(".bit-bfc-resize-handle-bottom");
        await handle.ScrollIntoViewIfNeededAsync();
        var box = (await handle.BoundingBoxAsync())!;

        await Page.Mouse.MoveAsync(box.X + box.Width / 2, box.Y + box.Height / 2);
        await Page.Mouse.DownAsync();
        await Page.Mouse.MoveAsync(box.X + box.Width / 2, box.Y + box.Height / 2 + 48, new() { Steps = 8 });
        var preview = Block(Cal, "e2").Locator(".bit-bfc-resize-preview");
        await Expect(preview).ToHaveAttributeAsync("role", "status");
        await Expect(preview).ToHaveAttributeAsync("aria-label", "New time range");
        await Expect(preview).ToHaveTextAsync("10:00 - 11:30");
        await Expect(Block(Cal, "e2")).ToHaveClassAsync(HasClass("bit-bfc-event-block-resizing"));
        await Page.Mouse.UpAsync();

        await Expect(Page.Locator("#last-change")).ToHaveTextAsync(new Regex(@"^Edit\|Resize\|Focus Time\|2025-07-16 10:00\|2025-07-16 11:30\|"));
    }

    // ---------------------------------------------------------------- keyboard edits

    [TestMethod]
    public async Task Keyboard_AltArrowsMoveAWeekBlock_ShiftArrowsResizeIt_AndTheFocusStaysOnIt()
    {
        await OpenMainAsync("Week");
        var block = Block(Cal, "e2");
        await Expect(block).ToHaveAttributeAsync("data-bit-bfc-move", "xy");
        await Expect(block).ToHaveAttributeAsync("data-bit-bfc-resize", "y");
        await block.FocusAsync();

        await Page.Keyboard.PressAsync("Alt+ArrowDown");
        await Expect(Page.Locator("#last-change")).ToHaveTextAsync(new Regex(@"^Edit\|Drag\|Focus Time\|2025-07-16 10:30\|2025-07-16 11:30\|"));
        await Expect(block).ToBeFocusedAsync();

        await Page.Keyboard.PressAsync("Shift+ArrowDown");
        await Expect(Page.Locator("#last-change")).ToHaveTextAsync(new Regex(@"^Edit\|Resize\|Focus Time\|2025-07-16 10:30\|2025-07-16 12:00\|"));
        await Expect(block).ToBeFocusedAsync();

        await Page.Keyboard.PressAsync("Shift+ArrowUp");
        await Expect(Page.Locator("#last-change")).ToHaveTextAsync(new Regex(@"^Edit\|Resize\|Focus Time\|2025-07-16 10:30\|2025-07-16 11:30\|"));

        await Page.Keyboard.PressAsync("Alt+ArrowRight");
        await Expect(Page.Locator("#last-change")).ToHaveTextAsync(new Regex(@"^Edit\|Drag\|Focus Time\|2025-07-17 10:30\|2025-07-17 11:30\|"));
        await Expect(block).ToBeFocusedAsync();

        await Page.Keyboard.PressAsync("Alt+ArrowLeft");
        await Expect(Page.Locator("#last-change")).ToHaveTextAsync(new Regex(@"^Edit\|Drag\|Focus Time\|2025-07-16 10:30\|2025-07-16 11:30\|"));
        await Expect(Page.Locator("#change-count")).ToHaveTextAsync("5");
        await Expect(block).ToBeFocusedAsync();

        // Enter opens the event
        await Page.Keyboard.PressAsync("Enter");
        await Expect(Dialog(Cal).Locator("h3")).ToHaveTextAsync("Focus Time");
    }

    [TestMethod]
    public async Task Keyboard_AltArrowMovesAMonthBadgeADay_AndTheFocusFollowsIt()
    {
        await OpenMainAsync();
        var badge = Badge(Cal, "e1");
        await Expect(badge).ToHaveAttributeAsync("data-bit-bfc-move", "x");
        await badge.FocusAsync();

        await Page.Keyboard.PressAsync("Alt+ArrowRight");

        await Expect(Page.Locator("#last-change")).ToHaveTextAsync(new Regex(@"^Edit\|Drag\|Team Standup\|2025-07-17 09:00\|2025-07-17 09:45\|"));
        await Expect(MonthCell(Cal, "Thursday, July 17, 2025").Locator("[data-bit-bfc-event='e1']")).ToBeVisibleAsync();
        // the focus stays on the event the key moved, not on whatever took its place in the old day
        await Expect(badge).ToBeFocusedAsync();
    }

    [TestMethod]
    public async Task Keyboard_SpaceOnAMonthBadge_OpensIt()
    {
        await OpenMainAsync();
        await Badge(Cal, "e6").FocusAsync();

        await Page.Keyboard.PressAsync(" ");

        await Expect(Dialog(Cal).Locator("h3")).ToHaveTextAsync("Quarterly Review");
        await Expect(Page.Locator("#change-count")).ToHaveTextAsync("0");
    }

    // ---------------------------------------------------------------- roving focus

    [TestMethod]
    public async Task Keyboard_TheMonthGridIsOneTabStop_TheArrowsHomeEndAndPageKeysWalkIt()
    {
        await OpenMainAsync();
        var stops = Cal.Locator(".bit-bfc-month-grid [data-bit-bfc-roving][tabindex='0']");
        await Expect(stops).ToHaveCountAsync(1);
        await Expect(stops).ToHaveAttributeAsync("aria-label", "Add event, Tuesday, July 1, 2025");
        await stops.FocusAsync();

        await Page.Keyboard.PressAsync("ArrowRight");
        await ExpectFocusedLabelAsync("Add event, Wednesday, July 2, 2025");
        await Page.Keyboard.PressAsync("ArrowDown");
        await ExpectFocusedLabelAsync("Add event, Wednesday, July 9, 2025");
        await Page.Keyboard.PressAsync("ArrowLeft");
        await ExpectFocusedLabelAsync("Add event, Tuesday, July 8, 2025");
        await Page.Keyboard.PressAsync("ArrowUp");
        await ExpectFocusedLabelAsync("Add event, Tuesday, July 1, 2025");
        await Page.Keyboard.PressAsync("End");
        await ExpectFocusedLabelAsync("Add event, Saturday, July 5, 2025");
        await Page.Keyboard.PressAsync("Home");
        await ExpectFocusedLabelAsync("Add event, Sunday, June 29, 2025");
        await Expect(stops).ToHaveCountAsync(1);

        await Page.Keyboard.PressAsync("PageDown");
        await ExpectFocusedLabelAsync("Add event, Tuesday, July 29, 2025");
        await Page.Keyboard.PressAsync("PageDown");
        await ExpectFocusedLabelAsync("Add event, Friday, August 29, 2025");
        await Expect(Title(Cal)).ToHaveTextAsync("August 2025");
        await Page.Keyboard.PressAsync("Shift+PageUp");
        await ExpectFocusedLabelAsync("Add event, Thursday, August 29, 2024");
        await Expect(Title(Cal)).ToHaveTextAsync("August 2024");
        await Page.Keyboard.PressAsync("Shift+PageDown");
        await ExpectFocusedLabelAsync("Add event, Friday, August 29, 2025");

        // a day of the next month the grid already shows is walked onto in place; one past the grid turns the month
        await Page.Keyboard.PressAsync("ArrowDown");
        await ExpectFocusedLabelAsync("Add event, Friday, September 5, 2025");
        await Expect(Title(Cal)).ToHaveTextAsync("August 2025");
        await Page.Keyboard.PressAsync("ArrowDown");
        await ExpectFocusedLabelAsync("Add event, Friday, September 12, 2025");
        await Expect(Title(Cal)).ToHaveTextAsync("September 2025");

        await Page.Keyboard.PressAsync("Enter");
        await Expect(StartTrigger(Dialog(Cal))).ToHaveAttributeAsync("aria-label", "Start Date & Time: 9/12/2025 08:00");
    }

    [TestMethod]
    public async Task Keyboard_TheWeekGridIsOneTabStop_TheKeysWalkTheSlots_AndEnterAddsThere()
    {
        await OpenMainAsync("Week");
        var stops = Cal.Locator(".bit-bfc-week-scroll [data-bit-bfc-roving][tabindex='0']");
        await Expect(stops).ToHaveCountAsync(1);
        await Expect(stops).ToHaveAttributeAsync("aria-label", "Add event, Sun 00:00");
        await stops.FocusAsync();

        await Page.Keyboard.PressAsync("ArrowDown");
        await ExpectFocusedLabelAsync("Add event, Sun 00:30");
        await Page.Keyboard.PressAsync("ArrowRight");
        await ExpectFocusedLabelAsync("Add event, Mon 00:30");
        await Page.Keyboard.PressAsync("PageDown");
        await ExpectFocusedLabelAsync("Add event, Mon 01:30");
        await Page.Keyboard.PressAsync("PageUp");
        await ExpectFocusedLabelAsync("Add event, Mon 00:30");
        await Page.Keyboard.PressAsync("End");
        await ExpectFocusedLabelAsync("Add event, Mon 23:30");
        await Page.Keyboard.PressAsync("Home");
        await ExpectFocusedLabelAsync("Add event, Mon 00:00");
        await Page.Keyboard.PressAsync("ArrowLeft");
        await ExpectFocusedLabelAsync("Add event, Sun 00:00");
        await Page.Keyboard.PressAsync("ArrowUp");
        await ExpectFocusedLabelAsync("Add event, Sun 00:00");
        await Expect(stops).ToHaveCountAsync(1);

        await Page.Keyboard.PressAsync("ArrowRight");
        await ExpectFocusedLabelAsync("Add event, Mon 00:00");
        await Page.Keyboard.PressAsync("Enter");
        await Expect(StartTrigger(Dialog(Cal))).ToHaveAttributeAsync("aria-label", "Start Date & Time: 7/14/2025 00:00");
    }

    [TestMethod]
    public async Task Keyboard_TheViewTabs_AreOneTabStopTheArrowsHomeAndEndWalk()
    {
        await OpenMainAsync();
        await Cal.Locator(".bit-bfc-view-tab.active").FocusAsync();

        await Page.Keyboard.PressAsync("ArrowRight");
        await Expect(Page.Locator("#view")).ToHaveTextAsync("Year");
        await Expect(Page.Locator("#cal-view-3")).ToBeFocusedAsync();
        await Expect(Page.Locator("#cal-view-3")).ToHaveAttributeAsync("tabindex", "0");

        await Page.Keyboard.PressAsync("Home");
        await Expect(Page.Locator("#view")).ToHaveTextAsync("Day");
        await Expect(Page.Locator("#cal-view-0")).ToBeFocusedAsync();

        await Page.Keyboard.PressAsync("End");
        await Expect(Page.Locator("#view")).ToHaveTextAsync("Agenda");
        await Expect(Page.Locator("#cal-view-4")).ToBeFocusedAsync();

        await Page.Keyboard.PressAsync("ArrowLeft");
        await Expect(Page.Locator("#view")).ToHaveTextAsync("Year");
        await Expect(Cal.Locator(".bit-bfc-view-tab[tabindex='0']")).ToHaveCountAsync(1);
        await Expect(Page.Locator("#view-change-count")).ToHaveTextAsync("4");
    }

    [TestMethod]
    public async Task Keyboard_TheModeTabs_AreWalkedByTheArrows()
    {
        await OpenMainAsync();
        await Page.Locator("#cal-mode-0").FocusAsync();

        await Page.Keyboard.PressAsync("ArrowRight");
        await Expect(Page.Locator("#mode")).ToHaveTextAsync("Timeline");
        await Expect(Page.Locator("#cal-mode-1")).ToBeFocusedAsync();
        await Expect(Page.Locator("#cal-mode-1")).ToHaveAttributeAsync("aria-selected", "true");

        await Page.Keyboard.PressAsync("Home");
        await Expect(Page.Locator("#mode")).ToHaveTextAsync("Event");
        await Expect(Page.Locator("#cal-mode-0")).ToBeFocusedAsync();

        await Page.Keyboard.PressAsync("End");
        await Expect(Page.Locator("#mode")).ToHaveTextAsync("Timeline");
        await Expect(Page.Locator("#mode-change-count")).ToHaveTextAsync("3");
    }

    [TestMethod]
    public async Task Keyboard_TheYearView_WalksTheBusyDays_AndEnterListsTheirEvents()
    {
        await OpenMainAsync("Year");
        var stop = Cal.Locator(".bit-bfc-year [data-bit-bfc-roving][tabindex='0']");
        await Expect(stop).ToHaveCountAsync(1);
        await Expect(stop).ToHaveAttributeAsync("aria-label", "Wednesday, July 16, 2025, 3 event(s)");
        await stop.FocusAsync();

        await Page.Keyboard.PressAsync("ArrowDown");
        await ExpectFocusedLabelAsync("Wednesday, July 23, 2025, 1 event(s)");
        await Page.Keyboard.PressAsync("ArrowUp");
        await ExpectFocusedLabelAsync("Wednesday, July 16, 2025, 3 event(s)");
        await Page.Keyboard.PressAsync("ArrowRight");
        await ExpectFocusedLabelAsync("Thursday, July 17, 2025, 1 event(s)");
        await Page.Keyboard.PressAsync("Home");
        await ExpectFocusedLabelAsync("Monday, July 14, 2025, 1 event(s)");
        await Page.Keyboard.PressAsync("End");
        await ExpectFocusedLabelAsync("Wednesday, July 23, 2025, 1 event(s)");
        await Page.Keyboard.PressAsync("ArrowLeft");
        await ExpectFocusedLabelAsync("Monday, July 21, 2025, 1 event(s)");

        await Page.Keyboard.PressAsync("Enter");
        var list = Dialog(Cal);
        await Expect(list.Locator("h3")).ToHaveTextAsync("Events on Monday, July 21, 2025");
        await Expect(list.Locator("p")).ToHaveTextAsync("1 event(s)");
    }

    [TestMethod]
    public async Task Keyboard_TheMiniCalendar_IsWalkedByTheArrowsAndPagedByPageKeys()
    {
        await OpenMainAsync("Day");
        var mini = Cal.Locator(".bit-bfc-mini-calendar");
        var stop = mini.Locator("[data-bit-bfc-roving][tabindex='0']");
        await Expect(stop).ToHaveAttributeAsync("aria-label", "Wednesday, July 16, 2025, selected");
        await stop.FocusAsync();

        await Page.Keyboard.PressAsync("ArrowRight");
        await ExpectFocusedLabelAsync("Thursday, July 17, 2025");
        await Page.Keyboard.PressAsync("ArrowDown");
        await ExpectFocusedLabelAsync("Thursday, July 24, 2025");
        await Page.Keyboard.PressAsync("PageDown");
        await ExpectFocusedLabelAsync("Sunday, August 24, 2025");
        await Expect(mini.Locator(".bit-bfc-mini-calendar-title")).ToHaveTextAsync("August 2025");

        await Page.Keyboard.PressAsync("Enter");
        await Expect(Title(Cal)).ToHaveTextAsync("Aug 24, 2025");
        await Expect(Page.Locator("#date")).ToHaveTextAsync("2025-08-24");
    }
}
