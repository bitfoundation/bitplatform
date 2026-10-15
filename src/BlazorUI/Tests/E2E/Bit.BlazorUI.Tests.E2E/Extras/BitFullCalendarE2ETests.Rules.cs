using System.Text.RegularExpressions;

namespace Bit.BlazorUI.Tests.E2E.Extras;

/// <summary>
/// What BitFullCalendar refuses and what it hands over (/e2e/full-calendar/rules): the booking rules (no overlaps, the
/// business hours shaded and enforced, a locked event, a background band and a blocking one) with their notices and
/// OnRefused, the MinDate/MaxDate window, and OnAddClick / OnEventClick replacing the built-in dialogs.
/// </summary>
public partial class BitFullCalendarE2ETests
{
    private const string RulesPage = "/e2e/full-calendar/rules";

    private ILocator ErrorToast(ILocator calendar) => calendar.Locator(".bit-bfc-toast-region[role='alert'] .bit-bfc-toast.error");

    // ---------------------------------------------------------------- background events

    [TestMethod]
    public async Task Rules_BackgroundEvents_AreBandsBehindTheGrid_SpokenWithTheSlotsTheyCover()
    {
        await OpenAsync(RulesPage);
        var cal = Page.Locator("#rules-cal");

        var lunch = cal.Locator(".bit-bfc-week-day-col .bit-bfc-bg-event.bit-bfc-bg-event-blocking");
        await Expect(lunch).ToHaveCountAsync(4);
        await Expect(lunch.First).ToHaveAttributeAsync("aria-hidden", "true");
        await Expect(lunch.First.Locator(".bit-bfc-bg-event-title")).ToHaveTextAsync("Lunch break");
        await Expect(cal.Locator("[data-bit-bfc-event='r4']")).ToHaveCountAsync(0);
        await Expect(Slot(cal, "Wed 12:00, Lunch break")).ToBeAttachedAsync();
        await Expect(Slot(cal, "Wed 13:00")).ToBeAttachedAsync();

        // the whole-day offsite is labelled in its day's header and fills its column
        await Expect(cal.Locator(".bit-bfc-week-header-day").Nth(5).Locator(".bit-bfc-bg-event-label")).ToHaveTextAsync("Company offsite");
        await Expect(cal.Locator(".bit-bfc-week-day-col").Nth(5).Locator(".bit-bfc-bg-event:not(.bit-bfc-bg-event-blocking)")).ToHaveCountAsync(1);

        // the month grid tints the whole-day band only
        await SwitchViewAsync(cal, "Month");
        await Expect(MonthCell(cal, "Friday, July 18, 2025")).ToHaveClassAsync(HasClass("bit-bfc-month-cell-has-bg"));
        await Expect(MonthCell(cal, "Wednesday, July 16, 2025")).Not.ToHaveClassAsync(HasClass("bit-bfc-month-cell-has-bg"));
        await Expect(MonthAdd(cal, "Friday, July 18, 2025")).ToHaveAttributeAsync("aria-label", "Add event, Friday, July 18, 2025, Company offsite");
    }

    [TestMethod]
    public async Task Rules_ALockedEvent_CannotBeMovedResizedEditedOrDeleted()
    {
        await OpenAsync(RulesPage);
        var cal = Page.Locator("#rules-cal");
        var payroll = Block(cal, "r1");

        await Expect(payroll).ToHaveAttributeAsync("draggable", "false");
        await Expect(payroll.Locator(".bit-bfc-resize-handle")).ToHaveCountAsync(0);
        await Expect(payroll).Not.ToHaveAttributeAsync("data-bit-bfc-move", new Regex(".*"));
        await Expect(Block(cal, "r2").Locator(".bit-bfc-resize-handle")).ToHaveCountAsync(2);

        await payroll.FocusAsync();
        await Page.Keyboard.PressAsync("Alt+ArrowDown");
        await Page.Keyboard.PressAsync("Shift+ArrowDown");
        await Page.Keyboard.PressAsync("Enter");
        var dialog = Dialog(cal);
        await Expect(dialog.Locator("h3")).ToHaveTextAsync("Payroll run");
        await Expect(dialog.Locator(".bit-bfc-dialog-footer button")).ToHaveTextAsync(["Close"]);
        await Expect(Page.Locator("#rules-change-count")).ToHaveTextAsync("0");
    }

    // ---------------------------------------------------------------- refusals

    [TestMethod]
    public async Task Rules_AnOverlap_IsRefusedWithANoticeAndOnRefused_UntilOverlapsAreAllowed()
    {
        await OpenAsync(RulesPage);
        var cal = Page.Locator("#rules-cal");
        var review = Block(cal, "r2");
        await review.FocusAsync();

        await Page.Keyboard.PressAsync("Alt+ArrowUp");
        await Expect(Page.Locator("#rules-change")).ToHaveTextAsync("Edit|Drag|Design Review|2025-07-16 10:30|2025-07-16 11:30");
        // the page re-supplies the event it stored, which redraws the block: the focus is given back to it
        await Expect(review).ToBeFocusedAsync();
        await Page.Keyboard.PressAsync("Alt+ArrowUp");
        await Expect(Page.Locator("#rules-change")).ToHaveTextAsync("Edit|Drag|Design Review|2025-07-16 10:00|2025-07-16 11:00");
        await Expect(review).ToBeFocusedAsync();
        // touching the payroll run is fine; running into it is not
        await Page.Keyboard.PressAsync("Alt+ArrowUp");

        await Expect(Page.Locator("#last-refusal")).ToHaveTextAsync("Overlap");
        await Expect(Page.Locator("#refused-count")).ToHaveTextAsync("1");
        await Expect(ErrorToast(cal)).ToHaveTextAsync("This time range is already taken on that resource.");
        await Expect(Page.Locator("#rules-change-count")).ToHaveTextAsync("2");
        await Expect(review.Locator(".bit-bfc-event-block-time")).ToHaveTextAsync("10:00 - 11:00");

        await Page.Locator("#btn-overlap").ClickAsync();
        await review.FocusAsync();
        await Page.Keyboard.PressAsync("Alt+ArrowUp");
        await Expect(Page.Locator("#rules-change")).ToHaveTextAsync("Edit|Drag|Design Review|2025-07-16 09:30|2025-07-16 10:30");
        await Expect(Page.Locator("#refused-count")).ToHaveTextAsync("1");
    }

    [TestMethod]
    public async Task Rules_ADropOntoATakenSlot_IsRefusedAndTheEventStays()
    {
        await OpenAsync(RulesPage);
        var cal = Page.Locator("#rules-cal");

        // the payroll run covers the slot: the drag lets it pass the pointer through once it has started
        await DragWithMouseAsync(cal, Block(cal, "r2"), Slot(cal, "Wed 09:30"));

        await Expect(Page.Locator("#last-refusal")).ToHaveTextAsync("Overlap");
        await Expect(Page.Locator("#rules-change-count")).ToHaveTextAsync("0");
        await Expect(Block(cal, "r2").Locator(".bit-bfc-event-block-time")).ToHaveTextAsync("11:00 - 12:00");
    }

    [TestMethod]
    public async Task Rules_ABlockingBand_RefusesWhateverRunsIntoIt_EvenWithOverlapsAllowed()
    {
        await OpenAsync(RulesPage);
        var cal = Page.Locator("#rules-cal");
        await Page.Locator("#btn-overlap").ClickAsync();
        await Block(cal, "r2").FocusAsync();

        await Page.Keyboard.PressAsync("Shift+ArrowDown");

        await Expect(Page.Locator("#last-refusal")).ToHaveTextAsync("Blocked");
        await Expect(ErrorToast(cal)).ToHaveTextAsync("That time is unavailable.");
        await Expect(Page.Locator("#rules-change-count")).ToHaveTextAsync("0");
    }

    [TestMethod]
    public async Task Rules_RestrictToBusinessHours_RefusesARangeReachingPastThem()
    {
        await OpenAsync(RulesPage);
        var cal = Page.Locator("#rules-cal");
        await Page.Locator("#btn-restrict").ClickAsync();
        var retro = Block(cal, "r3");
        await retro.FocusAsync();

        await Page.Keyboard.PressAsync("Shift+ArrowDown");
        await Expect(Page.Locator("#rules-change")).ToHaveTextAsync("Edit|Resize|Retro|2025-07-16 15:00|2025-07-16 16:30");
        await Expect(retro).ToBeFocusedAsync();
        await Page.Keyboard.PressAsync("Shift+ArrowDown");
        await Expect(Page.Locator("#rules-change")).ToHaveTextAsync("Edit|Resize|Retro|2025-07-16 15:00|2025-07-16 17:00");
        await Expect(retro).ToBeFocusedAsync();
        await Page.Keyboard.PressAsync("Shift+ArrowDown");

        await Expect(Page.Locator("#last-refusal")).ToHaveTextAsync("OutsideBusinessHours");
        await Expect(ErrorToast(cal)).ToHaveTextAsync("That time is outside business hours.");
        await Expect(Page.Locator("#rules-change-count")).ToHaveTextAsync("2");
    }

    [TestMethod]
    public async Task Rules_ADialogSaveTheRulesRefuse_ShowsTheReasonInTheDialog_AndRaisesOnRefused()
    {
        await OpenAsync(RulesPage);
        var cal = Page.Locator("#rules-cal");

        await Slot(cal, "Thu 12:30, Lunch break").ClickAsync();
        var dialog = Dialog(cal);
        await dialog.GetByPlaceholder("Event title").FillAsync("Working lunch");
        await dialog.GetByRole(AriaRole.Button, new() { Name = "Create Event" }).ClickAsync();

        await Expect(dialog.Locator(".bit-bfc-dialog-footer [role='alert']")).ToHaveTextAsync("That time is unavailable.");
        await Expect(dialog).ToBeVisibleAsync();
        await Expect(Page.Locator("#rules-change-count")).ToHaveTextAsync("0");
        // reported like a refused drag or resize, but said once, in the dialog - not again as the calendar's notice
        await Expect(Page.Locator("#refused-count")).ToHaveTextAsync("1");
        await Expect(Page.Locator("#last-refusal")).ToHaveTextAsync("Blocked");
        await Expect(ErrorToast(cal)).ToHaveCountAsync(0);

        await dialog.GetByRole(AriaRole.Button, new() { Name = "Cancel" }).ClickAsync();
        await Expect(dialog).ToHaveCountAsync(0);
        await Page.Locator("#btn-restrict").ClickAsync();
        await Slot(cal, "Thu 18:00").ClickAsync();
        await dialog.GetByPlaceholder("Event title").FillAsync("After hours");
        await dialog.GetByRole(AriaRole.Button, new() { Name = "Create Event" }).ClickAsync();
        await Expect(dialog.Locator(".bit-bfc-dialog-footer [role='alert']")).ToHaveTextAsync("That time is outside business hours.");
        await Expect(Page.Locator("#refused-count")).ToHaveTextAsync("2");
        await Expect(Page.Locator("#last-refusal")).ToHaveTextAsync("OutsideBusinessHours");
        await Expect(ErrorToast(cal)).ToHaveCountAsync(0);
    }

    [TestMethod]
    public async Task Rules_TheBusinessHoursAndDays_DecideWhatIsShaded()
    {
        await OpenAsync(RulesPage);
        var cal = Page.Locator("#rules-cal");

        await Expect(Slot(cal, "Wed 08:30")).ToHaveClassAsync(HasClass("bit-bfc-slot-off"));
        await Expect(Slot(cal, "Wed 10:00")).Not.ToHaveClassAsync(HasClass("bit-bfc-slot-off"));
        await Expect(Slot(cal, "Sat 10:00")).ToHaveClassAsync(HasClass("bit-bfc-slot-off"));

        await Page.Locator("#btn-business-days").ClickAsync();
        await Expect(Slot(cal, "Sat 10:00")).Not.ToHaveClassAsync(HasClass("bit-bfc-slot-off"));
        await Expect(Slot(cal, "Wed 10:00")).ToHaveClassAsync(HasClass("bit-bfc-slot-off"));

        await SwitchViewAsync(cal, "Month");
        await Expect(MonthCell(cal, "Wednesday, July 16, 2025")).ToHaveClassAsync(HasClass("bit-bfc-day-off"));
        await Expect(MonthCell(cal, "Saturday, July 19, 2025")).Not.ToHaveClassAsync(HasClass("bit-bfc-day-off"));
    }

    // ---------------------------------------------------------------- MinDate and MaxDate

    [TestMethod]
    public async Task Bounds_TheNavigationStopsAtTheWindow_AndTheDaysOutsideAreInert()
    {
        await OpenAsync(RulesPage);
        var cal = Page.Locator("#bounds-cal");
        var previous = cal.Locator(".bit-bfc-btn-nav[aria-label='Previous']");
        var next = cal.Locator(".bit-bfc-btn-nav[aria-label='Next']");

        await Expect(previous).ToHaveAttributeAsync("aria-disabled", "true");
        await Expect(next).Not.ToHaveAttributeAsync("aria-disabled", "true");
        await previous.ClickAsync(new() { Force = true });
        await Expect(Title(cal)).ToHaveTextAsync("July 2025");

        // July 1-5 (and the borrowed June days) lie before MinDate (July 6)
        var outside = cal.Locator(".bit-bfc-month-cell.bit-bfc-out-of-range");
        await Expect(outside).ToHaveCountAsync(7);
        await Expect(outside.Locator(".bit-bfc-cell-add-hint-month")).ToHaveCountAsync(0);
        await outside.Nth(3).ClickAsync(new() { Position = new() { X = 10, Y = 60 } });
        await Expect(Page.Locator("#bounds-date")).ToHaveTextAsync("2025-07-16");
        await Expect(Dialog(cal)).ToHaveCountAsync(0);
        var dimmed = await outside.First.EvaluateAsync<string>("e => getComputedStyle(e).opacity");
        Assert.AreNotEqual("1", dimmed, "the days outside are dimmed");

        await next.ClickAsync();
        await Expect(Title(cal)).ToHaveTextAsync("August 2025");
        await Expect(next).ToHaveAttributeAsync("aria-disabled", "true");
        await next.ClickAsync(new() { Force = true });
        await Expect(Title(cal)).ToHaveTextAsync("August 2025");
        await Expect(MonthAdd(cal, "Tuesday, August 5, 2025")).ToBeAttachedAsync();
        await Expect(MonthAdd(cal, "Wednesday, August 6, 2025")).ToHaveCountAsync(0);
        await Expect(cal.Locator(".bit-bfc-header-left > .bit-bfc-btn", new() { HasText = "Today" })).ToBeEnabledAsync();
    }

    [TestMethod]
    public async Task Bounds_ADropOutsideTheWindow_IsRefused()
    {
        await OpenAsync(RulesPage);
        var cal = Page.Locator("#bounds-cal");

        await Badge(cal, "b1").DragToAsync(cal.Locator(".bit-bfc-month-cell:has(.bit-bfc-month-cell-day:text-is('3'))").First, new() { TargetPosition = new() { X = 20, Y = 30 } });

        await Expect(Page.Locator("#bounds-refusal")).ToHaveTextAsync("OutOfRange");
        await Expect(ErrorToast(cal)).ToHaveTextAsync("That date is outside the allowed range.");
        await Expect(Page.Locator("#bounds-change")).ToHaveTextAsync("");
    }

    [TestMethod]
    public async Task Bounds_ABoundDateOutsideTheWindow_IsPulledBackIntoIt()
    {
        await OpenAsync(RulesPage);
        var cal = Page.Locator("#bounds-cal");

        await Page.Locator("#btn-bounds-far").ClickAsync();
        await Expect(Page.Locator("#bounds-date")).ToHaveTextAsync("2025-08-05");
        await Expect(Title(cal)).ToHaveTextAsync("August 2025");

        await Page.Locator("#btn-bounds-early").ClickAsync();
        await Expect(Page.Locator("#bounds-date")).ToHaveTextAsync("2025-07-06");
        await Expect(Title(cal)).ToHaveTextAsync("July 2025");
    }

    [TestMethod]
    public async Task Bounds_TheDialogsDatePicker_DisablesTheDaysOutside_AndTheRovingFocusSkipsThem()
    {
        await OpenAsync(RulesPage);
        var cal = Page.Locator("#bounds-cal");

        var stop = cal.Locator(".bit-bfc-month-grid [data-bit-bfc-roving][tabindex='0']");
        await Expect(stop).ToHaveAttributeAsync("aria-label", "Add event, Sunday, July 6, 2025");
        await stop.FocusAsync();
        await Page.Keyboard.PressAsync("ArrowLeft");
        await Page.Keyboard.PressAsync("ArrowUp");
        await NextFramesAsync();
        await ExpectFocusedLabelAsync("Add event, Sunday, July 6, 2025");

        await cal.Locator(".bit-bfc-header-right .bit-bfc-btn-primary").ClickAsync();
        var dialog = Dialog(cal);
        await StartTrigger(dialog).ClickAsync();
        var day = dialog.GetByRole(AriaRole.Button, new() { Name = "Saturday, July 5, 2025" });
        await Expect(day).ToBeDisabledAsync();
        await Expect(day).ToHaveAttributeAsync("aria-disabled", "true");
        await Expect(dialog.GetByRole(AriaRole.Button, new() { Name = "Sunday, July 6, 2025" })).ToBeEnabledAsync();
    }

    [TestMethod]
    public async Task Bounds_AWindowLeavingTodayOut_DisablesToday_AndOpensOnItsFirstDay()
    {
        await OpenAsync(RulesPage);
        var cal = Page.Locator("#future-cal");

        var today = cal.Locator(".bit-bfc-header-left > .bit-bfc-btn", new() { HasText = "Today" });
        await Expect(today).ToBeDisabledAsync();
        await Expect(today).ToHaveAttributeAsync("aria-disabled", "true");
        await Expect(Title(cal)).ToHaveTextAsync("August 2025");
        await Expect(cal.Locator(".bit-bfc-btn-nav[aria-label='Previous']")).ToHaveAttributeAsync("aria-disabled", "true");
    }

    // ---------------------------------------------------------------- the app's own handlers

    [TestMethod]
    public async Task Handlers_OnAddClick_ReplacesTheAddDialog_WithADraftOfWhereTheUserClicked()
    {
        await OpenAsync(RulesPage);
        var cal = Page.Locator("#handlers-cal");

        await MonthCell(cal, "Tuesday, July 22, 2025").ClickAsync(new() { Position = new() { X = 10, Y = 60 } });
        await Expect(Page.Locator("#add-click")).ToHaveTextAsync("2025-07-22 08:00|2025-07-22 08:30||-");
        await Expect(Dialog(cal)).ToHaveCountAsync(0);

        await cal.Locator(".bit-bfc-header-right .bit-bfc-btn-primary").ClickAsync();
        await Expect(Page.Locator("#add-click")).ToHaveTextAsync("2025-07-22 08:00|2025-07-22 08:30||-");
        await Expect(Page.Locator("#add-click-count")).ToHaveTextAsync("2");

        await SwitchViewAsync(cal, "Week");
        await Slot(cal, "Thu 13:00").ClickAsync();
        await Expect(Page.Locator("#add-click")).ToHaveTextAsync("2025-07-24 13:00|2025-07-24 13:30||-");

        var from = Slot(cal, "Fri 14:00");
        var to = Slot(cal, "Fri 15:30");
        await from.ScrollIntoViewIfNeededAsync();
        var a = (await from.BoundingBoxAsync())!;
        var b = (await to.BoundingBoxAsync())!;
        await Page.Mouse.MoveAsync(a.X + a.Width / 2, a.Y + a.Height / 2);
        await Page.Mouse.DownAsync();
        await Page.Mouse.MoveAsync(b.X + b.Width / 2, b.Y + b.Height / 2, new() { Steps = 6 });
        await Page.Mouse.UpAsync();
        await Expect(Page.Locator("#add-click")).ToHaveTextAsync("2025-07-25 14:00|2025-07-25 16:00||-");
        await Expect(Page.Locator("#add-click-count")).ToHaveTextAsync("4");
        await Expect(Dialog(cal)).ToHaveCountAsync(0);
    }

    [TestMethod]
    public async Task Handlers_OnEventClick_ReplacesTheDetailsDialog_InEveryView()
    {
        await OpenAsync(RulesPage);
        var cal = Page.Locator("#handlers-cal");

        await Badge(cal, "x1").ClickAsync();
        await Expect(Page.Locator("#event-click")).ToHaveTextAsync("x1|Clickable");
        await Expect(Dialog(cal)).ToHaveCountAsync(0);

        await SwitchViewAsync(cal, "Week");
        await Block(cal, "x1").ClickAsync();
        await SwitchViewAsync(cal, "Agenda");
        await cal.Locator(".bit-bfc-agenda-item[data-bit-bfc-event='x1']").ClickAsync();
        await Expect(Page.Locator("#event-click-count")).ToHaveTextAsync("3");
        await Expect(Dialog(cal)).ToHaveCountAsync(0);
    }
}
