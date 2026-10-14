using System.Text.RegularExpressions;

namespace Bit.BlazorUI.Tests.E2E.Extras;

/// <summary>
/// The time grid settings of BitFullCalendar (/e2e/full-calendar/grid): the visible hours and the slot duration, the work
/// week (HiddenDays, FirstDayOfWeek, ShowWeekNumbers, WeekDayCount), the Overlap and Stack layouts of overlapping
/// events, the all-day row, and range selection and the current-time line turned off.
/// </summary>
public partial class BitFullCalendarE2ETests
{
    private const string GridPage = "/e2e/full-calendar/grid";

    // ---------------------------------------------------------------- visible hours and slots

    [TestMethod]
    public async Task Grid_TheVisibleHours_NarrowTheGrid_AndClipTheEventsAtItsEdges()
    {
        await OpenAsync(GridPage);
        var cal = Page.Locator("#hours-cal");

        await Expect(cal.Locator(".bit-bfc-time-slot-label")).ToHaveCountAsync(10);
        await Expect(cal.Locator(".bit-bfc-time-slot-label").Nth(1)).ToHaveTextAsync("09:00");
        await Expect(cal.Locator(".bit-bfc-time-slot-label").Last).ToHaveTextAsync("17:00");
        // four 15-minute slots an hour
        await Expect(cal.Locator(".bit-bfc-week-day-col").First.Locator("[role='button'][aria-label^='Add event']")).ToHaveCountAsync(40);
        await Expect(Slot(cal, "Wed 09:45")).ToBeAttachedAsync();

        var hour = await cal.Locator(".bit-bfc-hour-row").First.EvaluateAsync<double>("e => e.getBoundingClientRect().height");
        var early = (await Block(cal, "h2").BoundingBoxAsync())!;
        var late = (await Block(cal, "h3").BoundingBoxAsync())!;
        Assert.AreEqual(hour, early.Height, 2, "06:00-09:00 is drawn from 08:00 only");
        Assert.AreEqual(hour, late.Height, 2, "17:00-20:00 is drawn up to 18:00 only");
        await Expect(Block(cal, "h4")).ToHaveCountAsync(0);

        // the grid opened scrolled to StartOfDayHour (9), one hour past its first row
        Assert.AreEqual(hour, await cal.Locator(".bit-bfc-week-scroll").EvaluateAsync<double>("e => e.scrollTop"), hour / 2);

        await Page.Locator("#btn-hours-full").ClickAsync();
        await Expect(cal.Locator(".bit-bfc-time-slot-label")).ToHaveCountAsync(24);
        await Expect(Block(cal, "h4")).ToBeAttachedAsync();
        Assert.AreEqual(3 * hour, (await Block(cal, "h2").BoundingBoxAsync())!.Height, 2);
    }

    [TestMethod]
    public async Task Grid_TheSlotDuration_SplitsEveryHour_AndAnUnacceptedValueRounds()
    {
        await OpenAsync(GridPage);
        var cal = Page.Locator("#hours-cal");

        await Page.Locator("#btn-slot-60").ClickAsync();
        await Expect(cal.Locator(".bit-bfc-week-day-col").First.Locator("[role='button'][aria-label^='Add event']")).ToHaveCountAsync(10);
        await Expect(Slot(cal, "Wed 09:30")).ToHaveCountAsync(0);

        // 7 is no divisor of 60: it rounds to the nearest one, 6
        await Page.Locator("#btn-slot-7").ClickAsync();
        await Expect(cal.Locator(".bit-bfc-week-day-col").First.Locator("[role='button'][aria-label^='Add event']")).ToHaveCountAsync(100);
        await Expect(Slot(cal, "Wed 09:06")).ToBeAttachedAsync();
        await Slot(cal, "Thu 10:12").ClickAsync();
        await Expect(EndTrigger(Dialog(cal))).ToHaveAttributeAsync("aria-label", "End Date & Time: 7/17/2025 10:18");
    }

    [TestMethod]
    public async Task Grid_KeyboardMovesAndMouseResizes_SnapToTheSlotDuration()
    {
        await OpenAsync(GridPage);
        var cal = Page.Locator("#hours-cal");
        var block = Block(cal, "h1");

        await block.FocusAsync();
        await Page.Keyboard.PressAsync("Alt+ArrowDown");
        await Expect(Page.Locator("#hours-change")).ToHaveTextAsync("Edit|Drag|Inside|2025-07-16 09:15|2025-07-16 10:15|timed");
        await Expect(block).ToBeFocusedAsync();
        await Page.Keyboard.PressAsync("Shift+ArrowUp");
        await Expect(Page.Locator("#hours-change")).ToHaveTextAsync("Edit|Resize|Inside|2025-07-16 09:15|2025-07-16 10:00|timed");

        var hour = await cal.Locator(".bit-bfc-hour-row").First.EvaluateAsync<double>("e => e.getBoundingClientRect().height");
        await DragByMouseAsync(block.Locator(".bit-bfc-resize-handle-bottom"), 0, (float)(hour / 2));
        await Expect(Page.Locator("#hours-change")).ToHaveTextAsync("Edit|Resize|Inside|2025-07-16 09:15|2025-07-16 10:30|timed");
    }

    // ---------------------------------------------------------------- work week

    [TestMethod]
    public async Task WorkWeek_HiddenDaysAndTheFirstDay_ShapeTheWeek_AndTheWeekNumberSitsInTheGutter()
    {
        await OpenAsync(GridPage);
        var cal = Page.Locator("#workweek-cal");

        await Expect(cal.Locator(".bit-bfc-week-header-day-name")).ToHaveTextAsync(["Mon", "Tue", "Wed", "Thu", "Fri"]);
        await Expect(cal.Locator(".bit-bfc-week-day-col")).ToHaveCountAsync(5);
        await Expect(Title(cal)).ToHaveTextAsync("Jul 14, 2025 - Jul 20, 2025");
        await Expect(Page.Locator("#workweek-range")).ToHaveTextAsync("2025-07-14|2025-07-20|Week");
        await Expect(cal.Locator(".bit-bfc-week-header-time .bit-bfc-weeknum-cell [aria-hidden='true']")).ToHaveTextAsync("W29");
        await Expect(cal.Locator(".bit-bfc-week-header-time .bit-bfc-weeknum-cell .bit-bfc-sr-only")).ToHaveTextAsync("Week 29");
        // the event of a hidden day is not drawn
        await Expect(cal.Locator("[data-bit-bfc-event='w2']")).ToHaveCountAsync(0);
        await Expect(Block(cal, "w1")).ToBeVisibleAsync();

        await cal.Locator(".bit-bfc-btn-nav[aria-label='Next']").ClickAsync();
        await Expect(Title(cal)).ToHaveTextAsync("Jul 21, 2025 - Jul 27, 2025");
        await Expect(cal.Locator(".bit-bfc-week-header-time .bit-bfc-weeknum-cell [aria-hidden='true']")).ToHaveTextAsync("W30");

        await SwitchViewAsync(cal, "Month");
        await Expect(cal.Locator(".bit-bfc-month-header-cell:not(.bit-bfc-weeknum-header)")).ToHaveTextAsync(["M", "T", "W", "T", "F"]);
        await Expect(cal.Locator(".bit-bfc-month-grid .bit-bfc-weeknum-cell [aria-hidden='true']").First).ToHaveTextAsync("W27");
    }

    [TestMethod]
    public async Task WorkWeek_TheDayViewSteps_OverTheHiddenDays()
    {
        await OpenAsync(GridPage);
        var cal = Page.Locator("#workweek-cal");
        await SwitchViewAsync(cal, "Day");
        var next = cal.Locator(".bit-bfc-btn-nav[aria-label='Next']");

        await next.ClickAsync();
        await next.ClickAsync();
        await Expect(Title(cal)).ToHaveTextAsync("Jul 18, 2025");
        await next.ClickAsync();
        await Expect(Title(cal)).ToHaveTextAsync("Jul 21, 2025");
        await cal.Locator(".bit-bfc-btn-nav[aria-label='Previous']").ClickAsync();
        await Expect(Title(cal)).ToHaveTextAsync("Jul 18, 2025");
    }

    [TestMethod]
    public async Task WorkWeek_AThreeDayWeek_StartsAtTheSelectedDay_AndTurnsByThreeShownDays()
    {
        await OpenAsync(GridPage);
        var cal = Page.Locator("#workweek-cal");

        await Page.Locator("#btn-workweek-three").ClickAsync();

        await Expect(cal.Locator(".bit-bfc-week-header-day-name")).ToHaveTextAsync(["Wed", "Thu", "Fri"]);
        await Expect(Title(cal)).ToHaveTextAsync("Jul 16, 2025 - Jul 18, 2025");
        // three days are the phone layout: no warning
        await Expect(cal.Locator(".bit-bfc-week-mobile-warning")).ToHaveCountAsync(0);

        await cal.Locator(".bit-bfc-btn-nav[aria-label='Next']").ClickAsync();
        await Expect(Title(cal)).ToHaveTextAsync("Jul 21, 2025 - Jul 23, 2025");
        await Expect(Page.Locator("#workweek-range")).ToHaveTextAsync("2025-07-21|2025-07-23|Week");
        await cal.Locator(".bit-bfc-btn-nav[aria-label='Previous']").ClickAsync();
        await Expect(Title(cal)).ToHaveTextAsync("Jul 16, 2025 - Jul 18, 2025");

        await Page.Locator("#btn-workweek-three").ClickAsync();
        await Expect(cal.Locator(".bit-bfc-week-header-day-name")).ToHaveCountAsync(5);
        await Expect(cal.Locator(".bit-bfc-week-mobile-warning")).ToBeAttachedAsync();
    }

    [TestMethod]
    public async Task WorkWeek_AnotherWorkWeek_OrAListHidingEveryDay_IsAppliedOrIgnored()
    {
        await OpenAsync(GridPage);
        var cal = Page.Locator("#workweek-cal");

        await Page.Locator("#btn-workweek-sun-thu").ClickAsync();
        await Expect(cal.Locator(".bit-bfc-week-header-day-name")).ToHaveTextAsync(["Sun", "Mon", "Tue", "Wed", "Thu"]);
        await Expect(Title(cal)).ToHaveTextAsync("Jul 13, 2025 - Jul 19, 2025");

        await Page.Locator("#btn-workweek-all-hidden").ClickAsync();
        await Expect(cal.Locator(".bit-bfc-week-header-day-name")).ToHaveCountAsync(7);
        await Expect(cal.Locator(".bit-bfc-week-header-day-name").First).ToHaveTextAsync("Sun");

        await Page.Locator("#btn-workweek-full").ClickAsync();
        await Expect(cal.Locator(".bit-bfc-week-header-day-name")).ToHaveTextAsync(["Sun", "Mon", "Tue", "Wed", "Thu", "Fri", "Sat"]);
        await Expect(cal.Locator("[data-bit-bfc-event='w2']")).ToBeVisibleAsync();
    }

    // ---------------------------------------------------------------- overlapping events

    [TestMethod]
    public async Task Layout_OverlapCascadesTheCards_AndStackPutsThemSideBySide()
    {
        await OpenAsync(GridPage);
        var cal = Page.Locator("#layout-cal");
        var first = Block(cal, "o1");
        var second = Block(cal, "o2");
        await first.ScrollIntoViewIfNeededAsync();

        var a = (await first.BoundingBoxAsync())!;
        var b = (await second.BoundingBoxAsync())!;
        Assert.IsTrue(b.X > a.X + 1, "the later card is offset");
        Assert.AreEqual(a.X + a.Width, b.X + b.Width, 2, "both cards reach the column edge");
        Assert.IsTrue(b.X < a.X + a.Width, "the cards overlap");

        await Page.Locator("#btn-layout-stack").ClickAsync();
        await Expect(cal.Locator(".bit-bfc-event-stack-item-side-by-side")).ToHaveCountAsync(2);
        a = (await first.BoundingBoxAsync())!;
        b = (await second.BoundingBoxAsync())!;
        Assert.AreEqual(a.Width, b.Width, 2, "equal lanes");
        Assert.IsTrue(b.X >= a.X + a.Width - 2, "side by side, no overlap");

        await Page.Locator("#btn-layout-overlap").ClickAsync();
        await Expect(cal.Locator(".bit-bfc-event-stack-item-side-by-side")).ToHaveCountAsync(0);
    }

    // ---------------------------------------------------------------- the all-day row

    [TestMethod]
    public async Task AllDay_EventsSitInTheRowAboveTheGrid_OneStopPerRun()
    {
        await OpenAsync(GridPage);
        var cal = Page.Locator("#allday-cal");
        var row = cal.Locator(".bit-bfc-multiday-row");

        await Expect(row.Locator("[data-bit-bfc-event='a1']")).ToHaveCountAsync(1);
        await Expect(row.Locator("[data-bit-bfc-event='a2']")).ToHaveCountAsync(3);
        await Expect(row.Locator("[data-bit-bfc-event='a2'][role='button']")).ToHaveCountAsync(1);
        await Expect(row.Locator("[data-bit-bfc-event='a2'][aria-hidden='true']")).ToHaveCountAsync(2);
        await Expect(Badge(cal, "a2")).ToHaveAttributeAsync("aria-label", "Release Freeze, All day, 7/17/2025 - 7/19/2025");
        await Expect(Badge(cal, "a2")).ToHaveClassAsync(HasClass("position-first"));
        await Expect(row.Locator("[data-bit-bfc-event='a2'].position-last .bit-bfc-event-badge-time")).ToHaveTextAsync("All day");
        // the timed event stays on the hour axis
        await Expect(Block(cal, "a3")).ToBeVisibleAsync();
        await Expect(row.Locator("[data-bit-bfc-event='a3']")).ToHaveCountAsync(0);

        await Badge(cal, "a2").ClickAsync();
        var dialog = Dialog(cal);
        await Expect(dialog.Locator(".bit-bfc-event-detail-row", new() { HasText = "Start Date" })).ToContainTextAsync("Thursday, July 17, 2025 · All day");
        await Expect(dialog.Locator(".bit-bfc-event-detail-row", new() { HasText = "End Date" })).ToContainTextAsync("Saturday, July 19, 2025 · All day");
    }

    [TestMethod]
    public async Task AllDay_UncheckingAllDay_GivesTheEventATimeOnTheGrid()
    {
        await OpenAsync(GridPage);
        var cal = Page.Locator("#allday-cal");
        await Badge(cal, "a1").ClickAsync();
        await Dialog(cal).GetByRole(AriaRole.Button, new() { Name = "Edit" }).ClickAsync();
        var edit = EditDialog(cal);
        var allDay = edit.GetByLabel("All day");
        await Expect(allDay).ToBeCheckedAsync();

        await allDay.UncheckAsync();
        await Expect(StartTrigger(edit)).ToHaveAttributeAsync("aria-label", "Start Date & Time: 7/16/2025 08:00");
        await Expect(EndTrigger(edit)).ToHaveAttributeAsync("aria-label", "End Date & Time: 7/16/2025 08:30");
        await edit.GetByRole(AriaRole.Button, new() { Name = "Save Changes" }).ClickAsync();

        await Expect(Page.Locator("#allday-change")).ToHaveTextAsync("Edit|Dialog|Company Holiday|2025-07-16 08:00|2025-07-16 08:30|timed");
        await Expect(Block(cal, "a1")).ToBeAttachedAsync();
        await Expect(cal.Locator(".bit-bfc-multiday-row [data-bit-bfc-event='a1']")).ToHaveCountAsync(0);
    }

    [TestMethod]
    public async Task AllDay_AltArrowMovesAnAllDayEventADay()
    {
        await OpenAsync(GridPage);
        var cal = Page.Locator("#allday-cal");
        await Badge(cal, "a1").FocusAsync();

        await Page.Keyboard.PressAsync("Alt+ArrowRight");

        await Expect(Page.Locator("#allday-change")).ToHaveTextAsync("Edit|Drag|Company Holiday|2025-07-17 00:00|2025-07-18 00:00|allday");
    }

    // ---------------------------------------------------------------- range selection and the current-time line turned off

    [TestMethod]
    public async Task NoRange_DraggingOverSlotsSelectsNothing_ButAClickStillAdds_AndNoTimeLineIsDrawn()
    {
        await OpenAsync(GridPage);
        var cal = Page.Locator("#norange-cal");
        await Expect(cal.Locator(".bit-bfc-timeline")).ToHaveCountAsync(0);
        var from = Slot(cal, "10:00");
        var to = Slot(cal, "11:00");
        await from.ScrollIntoViewIfNeededAsync();
        var a = (await from.BoundingBoxAsync())!;
        var b = (await to.BoundingBoxAsync())!;

        await Page.Mouse.MoveAsync(a.X + a.Width / 2, a.Y + a.Height / 2);
        await Page.Mouse.DownAsync();
        await Page.Mouse.MoveAsync(b.X + b.Width / 2, b.Y + b.Height / 2, new() { Steps = 6 });
        await Expect(cal.Locator(".bit-bfc-slot-selected")).ToHaveCountAsync(0);
        await Page.Mouse.UpAsync();
        await NextFramesAsync();
        await Expect(Dialog(cal)).ToHaveCountAsync(0);

        await from.ClickAsync();
        await Expect(StartTrigger(Dialog(cal))).ToHaveAttributeAsync("aria-label", "Start Date & Time: 7/16/2025 10:00");
        await Expect(EndTrigger(Dialog(cal))).ToHaveAttributeAsync("aria-label", "End Date & Time: 7/16/2025 10:30");
    }
}
