using System.Text.RegularExpressions;

namespace Bit.BlazorUI.Tests.E2E.Extras;

/// <summary>
/// BitFullCalendar on a phone (a 390x844 touch screen): the toolbar stacks and wraps instead of overflowing, the week
/// view warns that it is cramped unless it is a three-day week, the day view drops its sidebar, the year view stacks its
/// months, and a tap on a day or an event opens dialogs that fit the screen. "Today" is pinned through the TimeProvider
/// to Wednesday 2025-07-16 10:30, like every other page of the calendar.
/// </summary>
[TestClass]
public class BitFullCalendarMobileE2ETests : E2ETestBase
{
    protected override BrowserNewContextOptions? ContextOptions => new()
    {
        ViewportSize = new() { Width = 390, Height = 844 },
        HasTouch = true
    };

    private ILocator Cal => Page.Locator("#cal");

    private async Task SwitchViewAsync(string view)
    {
        await Cal.Locator(".bit-bfc-view-tab", new() { HasTextRegex = new Regex($"^\\s*{view}\\s*$") }).TapAsync();
        await Expect(Page.Locator("#view")).ToHaveTextAsync(view);
    }

    [TestMethod]
    public async Task Mobile_TheToolbarStacksAndWraps_InsteadOfOverflowing()
    {
        await OpenAsync("/e2e/full-calendar");

        var header = Cal.Locator(".bit-bfc-header");
        await Expect(header).ToHaveCSSAsync("flex-direction", "column");
        await Expect(Cal.Locator(".bit-bfc-view-tabs")).ToHaveCSSAsync("flex-wrap", "wrap");
        Assert.IsTrue(await header.EvaluateAsync<bool>("e => e.scrollWidth <= e.clientWidth + 1"), "the toolbar fits the phone's width");
        var calendarWidth = (await Cal.BoundingBoxAsync())!.Width;
        Assert.IsTrue(calendarWidth <= 390, $"the calendar fits the screen ({calendarWidth}px)");
        // the filters share a row of their own
        await Expect(Cal.Locator(".bit-bfc-filter-row")).ToHaveCSSAsync("flex-basis", "100%");
    }

    [TestMethod]
    public async Task Mobile_TheWeekViewWarnsThatItIsCramped_AndTheDayViewDropsItsSidebar()
    {
        await OpenAsync("/e2e/full-calendar");

        await SwitchViewAsync("Week");
        var warning = Cal.Locator(".bit-bfc-week-mobile-warning");
        await Expect(warning).ToBeVisibleAsync();
        await Expect(warning).ToContainTextAsync("Weekly view is not recommended on smaller devices.");

        await SwitchViewAsync("Day");
        await Expect(Cal.Locator(".bit-bfc-day-sidebar")).ToBeHiddenAsync();
        await Expect(Cal.Locator(".bit-bfc-day-layout")).ToBeVisibleAsync();
    }

    [TestMethod]
    public async Task Mobile_AThreeDayWeek_IsThePhoneLayout_WithoutAWarning()
    {
        await OpenAsync("/e2e/full-calendar/grid");
        var cal = Page.Locator("#workweek-cal");
        await Expect(cal.Locator(".bit-bfc-week-mobile-warning")).ToBeVisibleAsync();

        await Page.Locator("#btn-workweek-three").TapAsync();

        await Expect(cal.Locator(".bit-bfc-week-header-day-name")).ToHaveCountAsync(3);
        await Expect(cal.Locator(".bit-bfc-week-mobile-warning")).ToHaveCountAsync(0);
    }

    [TestMethod]
    public async Task Mobile_TheYearViewStacksItsMonthsInOneColumn()
    {
        await OpenAsync("/e2e/full-calendar");

        await SwitchViewAsync("Year");

        var columns = await Cal.Locator(".bit-bfc-year").EvaluateAsync<string>("e => getComputedStyle(e).gridTemplateColumns");
        Assert.AreEqual(1, columns.Split(' ', StringSplitOptions.RemoveEmptyEntries).Length, $"one column ({columns})");
        var january = (await Cal.Locator(".bit-bfc-year-month").Nth(0).BoundingBoxAsync())!;
        var february = (await Cal.Locator(".bit-bfc-year-month").Nth(1).BoundingBoxAsync())!;
        Assert.AreEqual(january.X, february.X, 1);
        Assert.IsTrue(february.Y > january.Y + january.Height - 1, "February sits under January");
    }

    [TestMethod]
    public async Task Mobile_ATapOnADayOrAnEvent_OpensADialogThatFitsTheScreen()
    {
        await OpenAsync("/e2e/full-calendar");

        await Cal.Locator(".bit-bfc-cell-add-hint-month[aria-label^='Add event, Tuesday, July 22, 2025']").TapAsync();
        var dialog = Cal.Locator(".bit-bfc-dialog[role='dialog']");
        await Expect(dialog.Locator("h3")).ToHaveTextAsync("Add New Event");
        var box = (await dialog.BoundingBoxAsync())!;
        Assert.IsTrue(box.X >= 0 && box.X + box.Width <= 390, $"the dialog fits the width ({box.X}, {box.Width})");
        Assert.IsTrue(box.Height <= 844 * 0.9 + 1, "and 90% of the height, scrolling inside");
        await dialog.GetByRole(AriaRole.Button, new() { Name = "Cancel" }).TapAsync();
        await Expect(dialog).ToHaveCountAsync(0);

        await Cal.Locator("[data-bit-bfc-event='e1'][role='button']").TapAsync();
        await Expect(dialog.Locator("h3")).ToHaveTextAsync("Team Standup");
    }
}
