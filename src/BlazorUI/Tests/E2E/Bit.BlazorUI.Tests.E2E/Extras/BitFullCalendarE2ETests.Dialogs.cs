using System.Text.RegularExpressions;

namespace Bit.BlazorUI.Tests.E2E.Extras;

/// <summary>
/// The built-in dialogs of BitFullCalendar: adding an event from a month day, a time slot, a dragged range or the
/// toolbar, the validation of the add/edit form, the details dialog, editing, deleting, and OnChanging refusing them.
/// </summary>
public partial class BitFullCalendarE2ETests
{
    private ILocator EditDialog(ILocator calendar) => calendar.Locator(".bit-bfc-dialog[role='dialog']:has(h3:text-is('Edit Event'))");

    private static ILocator StartTrigger(ILocator dialog) => dialog.Locator("button.bit-bfc-dtp-trigger").Nth(0);

    private static ILocator EndTrigger(ILocator dialog) => dialog.Locator("button.bit-bfc-dtp-trigger").Nth(1);

    // ---------------------------------------------------------------- creating events

    [TestMethod]
    public async Task Add_ClickingAMonthDay_OpensTheAddDialogOnThatDay_AndCreateAddsTheEvent()
    {
        await OpenMainAsync();

        await MonthCell(Cal, "Tuesday, July 22, 2025").ClickAsync(new() { Position = new() { X = 10, Y = 60 } });

        var dialog = Dialog(Cal);
        await Expect(dialog).ToBeVisibleAsync();
        await Expect(dialog).ToHaveAttributeAsync("aria-modal", "true");
        await Expect(dialog.Locator("h3")).ToHaveTextAsync("Add New Event");
        await Expect(dialog.Locator("p").First).ToHaveTextAsync("Create a new event for your calendar.");
        // a new event opens at the start-of-day hour and lasts one slot
        await Expect(StartTrigger(dialog)).ToHaveAttributeAsync("aria-label", "Start Date & Time: 7/22/2025 08:00");
        await Expect(EndTrigger(dialog)).ToHaveAttributeAsync("aria-label", "End Date & Time: 7/22/2025 08:30");
        // the focus moved into the dialog
        await Expect(dialog.Locator(":focus")).ToHaveCountAsync(1);
        await Expect(Page.Locator("#date")).ToHaveTextAsync("2025-07-22");

        await dialog.GetByPlaceholder("Event title").FillAsync("Planning Poker");
        await dialog.GetByPlaceholder("Event description").FillAsync("Estimate the backlog.");
        await dialog.Locator("select[aria-label='Event color']").SelectOptionAsync("purple");
        await dialog.Locator("select[aria-label='Resource']").SelectOptionAsync("room-b");
        await dialog.GetByRole(AriaRole.Button, new() { Name = "Create Event" }).ClickAsync();

        await Expect(dialog).ToHaveCountAsync(0);
        await Expect(Page.Locator("#change-count")).ToHaveTextAsync("1");
        await Expect(Page.Locator("#last-change")).ToHaveTextAsync("Add|Dialog|Planning Poker|2025-07-22 08:00|2025-07-22 08:30|room-b|purple|timed|Estimate the backlog.|");
        await Expect(Page.Locator("#last-change-old")).ToHaveTextAsync("null");
        await Expect(Page.Locator("#changing-count")).ToHaveTextAsync("1");
        await Expect(Page.Locator("#store-count")).ToHaveTextAsync("8");
        await Expect(MonthCell(Cal, "Tuesday, July 22, 2025").Locator(".bit-bfc-event-badge-title")).ToHaveTextAsync("Planning Poker");
    }

    [TestMethod]
    public async Task Add_TheToolbarButton_OpensTheDialogOnTheSelectedDate_AndCancelAddsNothing()
    {
        await OpenMainAsync();

        await Cal.Locator(".bit-bfc-header-right .bit-bfc-btn-primary", new() { HasText = "Add Event" }).ClickAsync();

        var dialog = Dialog(Cal);
        await Expect(dialog.Locator("h3")).ToHaveTextAsync("Add New Event");
        await Expect(StartTrigger(dialog)).ToHaveAttributeAsync("aria-label", "Start Date & Time: 7/16/2025 08:00");

        await dialog.GetByPlaceholder("Event title").FillAsync("Never saved");
        await dialog.GetByRole(AriaRole.Button, new() { Name = "Cancel" }).ClickAsync();

        await Expect(dialog).ToHaveCountAsync(0);
        await Expect(Page.Locator("#change-count")).ToHaveTextAsync("0");
        await Expect(Page.Locator("#store-count")).ToHaveTextAsync("7");
    }

    [TestMethod]
    public async Task Add_SavingWithoutATitle_ShowsTheErrorOnTheFieldAndKeepsTheDialogOpen()
    {
        await OpenMainAsync();
        await Cal.Locator(".bit-bfc-header-right .bit-bfc-btn-primary").ClickAsync();
        var dialog = Dialog(Cal);

        await dialog.GetByRole(AriaRole.Button, new() { Name = "Create Event" }).ClickAsync();

        var title = dialog.GetByPlaceholder("Event title");
        var error = dialog.Locator(".bit-bfc-field-error");
        await Expect(error).ToHaveTextAsync("Title is required");
        await Expect(title).ToHaveAttributeAsync("aria-invalid", "true");
        await Expect(title).ToHaveAttributeAsync("aria-describedby", (await error.GetAttributeAsync("id"))!);
        await Expect(title).ToBeFocusedAsync();
        await Expect(dialog).ToBeVisibleAsync();
        await Expect(Page.Locator("#change-count")).ToHaveTextAsync("0");

        // an attendee needs a name too
        await dialog.GetByRole(AriaRole.Button, new() { Name = "Add", Exact = true }).ClickAsync();
        await Expect(dialog.Locator(".bit-bfc-field-error", new() { HasText = "First name or last name is required" })).ToBeVisibleAsync();
        await Expect(dialog.GetByPlaceholder("First name")).ToBeFocusedAsync();
    }

    [TestMethod]
    public async Task Add_TheDialogTakesAttendees_AndAnAllDayEvent_GoesIntoTheAllDayRow()
    {
        await OpenMainAsync("Week");
        await Cal.Locator(".bit-bfc-header-right .bit-bfc-btn-primary").ClickAsync();
        var dialog = Dialog(Cal);

        await dialog.GetByPlaceholder("Event title").FillAsync("Offsite");
        await dialog.GetByPlaceholder("First name").FillAsync("Dana");
        await dialog.GetByPlaceholder("Last name").FillAsync("Scully");
        await dialog.GetByRole(AriaRole.Button, new() { Name = "Add", Exact = true }).ClickAsync();
        await Expect(dialog.Locator(".bit-bfc-attendee-chip")).ToContainTextAsync("Dana Scully");
        await Expect(dialog.Locator(".bit-bfc-attendee-chip-avatar")).ToHaveTextAsync("DS");

        await dialog.GetByLabel("All day").CheckAsync();
        await Expect(StartTrigger(dialog)).ToHaveAttributeAsync("aria-label", "Start Date & Time: 7/16/2025 00:00");
        await Expect(EndTrigger(dialog)).ToHaveAttributeAsync("aria-label", "End Date & Time: 7/17/2025 00:00");

        await dialog.GetByRole(AriaRole.Button, new() { Name = "Create Event" }).ClickAsync();

        await Expect(Page.Locator("#last-change")).ToHaveTextAsync(new Regex(@"^Add\|Dialog\|Offsite\|2025-07-16 00:00\|2025-07-17 00:00\|-\|blue\|allday\|\|Dana Scully$"));
        var allDay = Cal.Locator(".bit-bfc-multiday-row .bit-bfc-event-allday", new() { HasText = "Offsite" });
        await Expect(allDay).ToBeVisibleAsync();
        await Expect(allDay).ToHaveAttributeAsync("aria-label", "Offsite, All day, 7/16/2025 - 7/16/2025");
        await Expect(allDay.Locator(".bit-bfc-event-badge-time")).ToHaveTextAsync("All day");
    }

    [TestMethod]
    public async Task Add_ClickingAWeekSlot_OpensTheDialogAtThatTime()
    {
        await OpenMainAsync("Week");

        await Slot(Cal, "Fri 13:30").ClickAsync();

        var dialog = Dialog(Cal);
        await Expect(StartTrigger(dialog)).ToHaveAttributeAsync("aria-label", "Start Date & Time: 7/18/2025 13:30");
        await Expect(EndTrigger(dialog)).ToHaveAttributeAsync("aria-label", "End Date & Time: 7/18/2025 14:00");
        await Expect(Page.Locator("#date")).ToHaveTextAsync("2025-07-18");
    }

    [TestMethod]
    public async Task Add_DraggingDownAColumnOfEmptySlots_OpensTheDialogSpanningTheRange()
    {
        await OpenMainAsync("Week");
        var from = Slot(Cal, "Fri 13:00");
        var to = Slot(Cal, "Fri 14:30");
        await from.ScrollIntoViewIfNeededAsync();
        var a = (await from.BoundingBoxAsync())!;
        var b = (await to.BoundingBoxAsync())!;

        await Page.Mouse.MoveAsync(a.X + a.Width / 2, a.Y + a.Height / 2);
        await Page.Mouse.DownAsync();
        await Page.Mouse.MoveAsync(b.X + b.Width / 2, b.Y + b.Height / 2, new() { Steps = 8 });
        await Expect(Cal.Locator(".bit-bfc-slot-selected")).ToHaveCountAsync(4);
        await Page.Mouse.UpAsync();

        var dialog = Dialog(Cal);
        await Expect(StartTrigger(dialog)).ToHaveAttributeAsync("aria-label", "Start Date & Time: 7/18/2025 13:00");
        await Expect(EndTrigger(dialog)).ToHaveAttributeAsync("aria-label", "End Date & Time: 7/18/2025 15:00");
        await Expect(Cal.Locator(".bit-bfc-slot-selected")).ToHaveCountAsync(0);
    }

    [TestMethod]
    public async Task Add_DraggingOverTheDayGrid_SelectsARange_AndAClickTakesOneSlot()
    {
        await OpenMainAsync("Day");
        var from = Slot(Cal, "16:00");
        var to = Slot(Cal, "16:30");
        await from.ScrollIntoViewIfNeededAsync();
        var a = (await from.BoundingBoxAsync())!;
        var b = (await to.BoundingBoxAsync())!;

        await Page.Mouse.MoveAsync(a.X + a.Width / 2, a.Y + a.Height / 2);
        await Page.Mouse.DownAsync();
        await Page.Mouse.MoveAsync(b.X + b.Width / 2, b.Y + b.Height / 2, new() { Steps = 5 });
        await Page.Mouse.UpAsync();

        var dialog = Dialog(Cal);
        await Expect(StartTrigger(dialog)).ToHaveAttributeAsync("aria-label", "Start Date & Time: 7/16/2025 16:00");
        await Expect(EndTrigger(dialog)).ToHaveAttributeAsync("aria-label", "End Date & Time: 7/16/2025 17:00");
        await dialog.GetByRole(AriaRole.Button, new() { Name = "Cancel" }).ClickAsync();
        await Expect(dialog).ToHaveCountAsync(0);

        await Slot(Cal, "18:00").ClickAsync();
        await Expect(StartTrigger(dialog)).ToHaveAttributeAsync("aria-label", "Start Date & Time: 7/16/2025 18:00");
        await Expect(EndTrigger(dialog)).ToHaveAttributeAsync("aria-label", "End Date & Time: 7/16/2025 18:30");
    }

    [TestMethod]
    public async Task Add_TheDatePickerOfTheDialog_ChangesTheStart_AndTheEndMovesWithIt()
    {
        await OpenMainAsync();
        await Cal.Locator(".bit-bfc-header-right .bit-bfc-btn-primary").ClickAsync();
        var dialog = Dialog(Cal);
        var start = StartTrigger(dialog);

        await start.ClickAsync();
        await Expect(start).ToHaveAttributeAsync("aria-expanded", "true");
        var panel = dialog.Locator(".bit-bfc-dtp-panel");
        await Expect(panel.Locator(".bit-bfc-dtp-month-label")).ToHaveTextAsync("July 2025");
        await Expect(panel.Locator(".bit-bfc-dtp-day-selected")).ToHaveAttributeAsync("aria-label", "Wednesday, July 16, 2025, selected");
        await panel.Locator("select").First.SelectOptionAsync("10");
        await Expect(start).ToHaveAttributeAsync("aria-label", "Start Date & Time: 7/16/2025 10:00");
        await panel.GetByRole(AriaRole.Button, new() { Name = "Next month" }).ClickAsync();
        await Expect(panel.Locator(".bit-bfc-dtp-month-label")).ToHaveTextAsync("August 2025");
        await panel.GetByRole(AriaRole.Button, new() { Name = "Thursday, August 7, 2025" }).ClickAsync();

        await Expect(panel).ToHaveCountAsync(0);
        await Expect(start).ToHaveAttributeAsync("aria-label", "Start Date & Time: 8/7/2025 10:00");
        await Expect(EndTrigger(dialog)).ToHaveAttributeAsync("aria-label", "End Date & Time: 8/7/2025 10:30");
    }

    [TestMethod]
    public async Task Add_EscapeInsideTheDatePicker_ClosesThePickerFirst_ThenTheDialog()
    {
        await OpenMainAsync();
        await Cal.Locator(".bit-bfc-header-right .bit-bfc-btn-primary").ClickAsync();
        var dialog = Dialog(Cal);
        var start = StartTrigger(dialog);
        await start.ClickAsync();
        var panel = dialog.Locator(".bit-bfc-dtp-panel");
        await Expect(panel).ToBeVisibleAsync();
        await panel.Locator(".bit-bfc-dtp-day[tabindex='0']").FocusAsync();

        await PressEscapeAsync();
        await Expect(panel).ToHaveCountAsync(0);
        await Expect(dialog).ToBeVisibleAsync();

        await PressEscapeAsync();
        await Expect(dialog).ToHaveCountAsync(0);
    }

    [TestMethod]
    public async Task Add_AnEndBeforeTheStart_IsRefusedOnTheEndField()
    {
        await OpenMainAsync();
        await Cal.Locator(".bit-bfc-header-right .bit-bfc-btn-primary").ClickAsync();
        var dialog = Dialog(Cal);
        await dialog.GetByPlaceholder("Event title").FillAsync("Backwards");

        var end = EndTrigger(dialog);
        await end.ClickAsync();
        await dialog.Locator(".bit-bfc-dtp-panel select").First.SelectOptionAsync("7");
        await Expect(end).ToHaveAttributeAsync("aria-label", "End Date & Time: 7/16/2025 07:30");
        await dialog.GetByRole(AriaRole.Button, new() { Name = "Create Event" }).ClickAsync();

        await Expect(dialog.Locator(".bit-bfc-field-error")).ToHaveTextAsync("End date must be after start date");
        await Expect(end).ToHaveAttributeAsync("aria-invalid", "true");
        await Expect(end).ToBeFocusedAsync();
        await Expect(Page.Locator("#change-count")).ToHaveTextAsync("0");
    }

    // ---------------------------------------------------------------- the details dialog

    [TestMethod]
    public async Task Details_ClickingAnEvent_ShowsEverythingItCarries()
    {
        await OpenMainAsync();

        await Badge(Cal, "e1").ClickAsync();

        var dialog = Dialog(Cal);
        await Expect(dialog.Locator("h3")).ToHaveTextAsync("Team Standup");
        await Expect(dialog).ToHaveAttributeAsync("aria-labelledby", (await dialog.Locator("h3").GetAttributeAsync("id"))!);
        await Expect(dialog.Locator(".bit-bfc-event-detail-color")).ToHaveTextAsync("Blue");
        await Expect(dialog.Locator(".bit-bfc-attendee-chip")).ToHaveTextAsync([new Regex("Alice Smith"), new Regex("Bob Jones")]);
        await Expect(dialog.Locator(".bit-bfc-event-detail-row", new() { HasText = "Start Date" })).ToContainTextAsync("Wednesday, July 16, 2025 at 09:00");
        await Expect(dialog.Locator(".bit-bfc-event-detail-row", new() { HasText = "End Date" })).ToContainTextAsync("Wednesday, July 16, 2025 at 09:45");
        await Expect(dialog.Locator(".bit-bfc-event-detail-row", new() { HasText = "Description" })).ToContainTextAsync("Daily sync.");
        await Expect(dialog.Locator(".bit-bfc-event-detail-row", new() { HasText = "Resource" })).ToContainTextAsync("Room A");
        await Expect(dialog.GetByRole(AriaRole.Button, new() { Name = "Edit" })).ToBeVisibleAsync();
        await Expect(dialog.GetByRole(AriaRole.Button, new() { Name = "Delete" })).ToBeVisibleAsync();

        await dialog.Locator(".bit-bfc-dialog-footer").GetByRole(AriaRole.Button, new() { Name = "Close" }).ClickAsync();
        await Expect(dialog).ToHaveCountAsync(0);
        // the focus goes back to the event that opened the dialog
        await Expect(Badge(Cal, "e1")).ToBeFocusedAsync();
    }

    [TestMethod]
    public async Task Details_EscapeTheCloseButtonAndTheOverlay_EachCloseTheDialog()
    {
        await OpenMainAsync();
        var dialog = Dialog(Cal);

        await Badge(Cal, "e2").ClickAsync();
        await Expect(dialog).ToHaveAttributeAsync("data-bit-esc", "claim");
        await PressEscapeAsync();
        await Expect(dialog).ToHaveCountAsync(0);

        await Badge(Cal, "e2").ClickAsync();
        await dialog.Locator(".bit-bfc-dialog-close-btn").ClickAsync();
        await Expect(dialog).ToHaveCountAsync(0);

        await Badge(Cal, "e2").ClickAsync();
        await Cal.Locator(".bit-bfc-overlay").ClickAsync(new() { Position = new() { X = 5, Y = 5 } });
        await Expect(dialog).ToHaveCountAsync(0);
    }

    [TestMethod]
    public async Task Details_TabStaysInsideTheDialog()
    {
        await OpenMainAsync();
        await Badge(Cal, "e3").ClickAsync();
        var dialog = Dialog(Cal);
        await Expect(dialog).ToBeVisibleAsync();

        for (var i = 0; i < 8; i++)
        {
            await Page.Keyboard.PressAsync("Tab");
            Assert.IsTrue(await dialog.EvaluateAsync<bool>("d => d.contains(document.activeElement)"), $"Tab {i + 1} left the dialog.");
        }

        for (var i = 0; i < 8; i++)
        {
            await Page.Keyboard.PressAsync("Shift+Tab");
            Assert.IsTrue(await dialog.EvaluateAsync<bool>("d => d.contains(document.activeElement)"), $"Shift+Tab {i + 1} left the dialog.");
        }
    }

    [TestMethod]
    public async Task Edit_SavingTheEditDialog_ReportsTheEditWithTheOldSnapshot()
    {
        await OpenMainAsync();
        await Badge(Cal, "e3").ClickAsync();
        await Dialog(Cal).GetByRole(AriaRole.Button, new() { Name = "Edit" }).ClickAsync();

        var edit = EditDialog(Cal);
        await Expect(edit).ToBeVisibleAsync();
        await Expect(edit.Locator("p").First).ToHaveTextAsync("Modify your existing event.");
        var title = edit.GetByPlaceholder("Event title");
        await Expect(title).ToHaveValueAsync("Design Review");
        await Expect(edit.GetByPlaceholder("Event description")).ToHaveValueAsync("Mockups v2.");
        await Expect(edit.Locator("select[aria-label='Event color']")).ToHaveValueAsync("purple");
        await Expect(edit.Locator("select[aria-label='Resource']")).ToHaveValueAsync("room-b");

        await title.FillAsync("Design Review v3");
        await edit.GetByRole(AriaRole.Button, new() { Name = "Save Changes" }).ClickAsync();

        await Expect(Dialog(Cal)).ToHaveCountAsync(0);
        await Expect(Page.Locator("#last-change")).ToHaveTextAsync("Edit|Dialog|Design Review v3|2025-07-16 14:00|2025-07-16 15:00|room-b|purple|timed|Mockups v2.|");
        await Expect(Page.Locator("#last-change-old")).ToHaveTextAsync("Design Review|2025-07-16 14:00|2025-07-16 15:00|room-b|purple|timed|Mockups v2.|");
        await Expect(Badge(Cal, "e3").Locator(".bit-bfc-event-badge-title")).ToHaveTextAsync("Design Review v3");
    }

    [TestMethod]
    public async Task Edit_CancellingTheEditDialog_KeepsTheDetailsOpenAndTheEventAsItWas()
    {
        await OpenMainAsync();
        await Badge(Cal, "e3").ClickAsync();
        await Dialog(Cal).GetByRole(AriaRole.Button, new() { Name = "Edit" }).ClickAsync();
        var edit = EditDialog(Cal);
        await edit.GetByPlaceholder("Event title").FillAsync("Thrown away");

        await edit.GetByRole(AriaRole.Button, new() { Name = "Cancel" }).ClickAsync();

        await Expect(edit).ToHaveCountAsync(0);
        await Expect(Dialog(Cal).Locator("h3")).ToHaveTextAsync("Design Review");
        await Expect(Page.Locator("#change-count")).ToHaveTextAsync("0");
    }

    [TestMethod]
    public async Task Delete_TheDetailsDeleteButton_RemovesTheEventAndReportsIt()
    {
        await OpenMainAsync();
        await Badge(Cal, "e7").ClickAsync();

        await Dialog(Cal).GetByRole(AriaRole.Button, new() { Name = "Delete" }).ClickAsync();

        await Expect(Dialog(Cal)).ToHaveCountAsync(0);
        await Expect(Cal.Locator("[data-bit-bfc-event='e7']")).ToHaveCountAsync(0);
        await Expect(Page.Locator("#last-change")).ToHaveTextAsync(new Regex(@"^Delete\|Dialog\|Client Call\|"));
        await Expect(Page.Locator("#store-count")).ToHaveTextAsync("6");
        await Expect(Page.Locator("#store li[data-id='e7']")).ToHaveCountAsync(0);
    }

    [TestMethod]
    public async Task OnChanging_CancellingASave_KeepsTheDialogOpenAndReportsNoChange()
    {
        await OpenMainAsync();
        await Page.Locator("#btn-refuse-changing").ClickAsync();
        await Badge(Cal, "e3").ClickAsync();
        await Dialog(Cal).GetByRole(AriaRole.Button, new() { Name = "Edit" }).ClickAsync();
        var edit = EditDialog(Cal);
        await edit.GetByPlaceholder("Event title").FillAsync("Refused title");

        await edit.GetByRole(AriaRole.Button, new() { Name = "Save Changes" }).ClickAsync();

        await Expect(Page.Locator("#changing-count")).ToHaveTextAsync("1");
        await Expect(Page.Locator("#last-changing")).ToHaveTextAsync("Edit|Dialog|Refused title|2025-07-16 14:00|2025-07-16 14:00");
        await Expect(edit).ToBeVisibleAsync();
        await Expect(edit.GetByPlaceholder("Event title")).ToHaveValueAsync("Refused title");
        await Expect(Page.Locator("#change-count")).ToHaveTextAsync("0");

        await edit.GetByRole(AriaRole.Button, new() { Name = "Cancel" }).ClickAsync();
        await Expect(edit).ToHaveCountAsync(0);
        await PressEscapeAsync();
        await Expect(Dialog(Cal)).ToHaveCountAsync(0);
        await Expect(Badge(Cal, "e3").Locator(".bit-bfc-event-badge-title")).ToHaveTextAsync("Design Review");

        // a refused delete leaves the event where it was, and the dialog open on it
        await Badge(Cal, "e3").ClickAsync();
        await Dialog(Cal).GetByRole(AriaRole.Button, new() { Name = "Delete" }).ClickAsync();
        await Expect(Page.Locator("#changing-count")).ToHaveTextAsync("2");
        await Expect(Dialog(Cal)).ToBeVisibleAsync();
        await Expect(Page.Locator("#change-count")).ToHaveTextAsync("0");
        await Expect(Page.Locator("#store-count")).ToHaveTextAsync("7");
        await Expect(Cal.Locator("[data-bit-bfc-event='e3']").First).ToBeAttachedAsync();
    }
}
