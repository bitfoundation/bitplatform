using System.Text.RegularExpressions;

namespace Bit.BlazorUI.Tests.E2E.Extras;

/// <summary>
/// The month grid settings of BitFullCalendar (/e2e/full-calendar/month): FixedWeekCount, ShowNonCurrentDates,
/// MaxEventsPerDayCell, NavLinks and ShowWeekNumbers; the dot badge in a 12-hour calendar; and the event colors - an
/// EventColorOptions palette, a CSS color of an event's own and an unknown one drawn in --bit-FullCalendar-event-color.
/// </summary>
public partial class BitFullCalendarE2ETests
{
    private const string MonthPage = "/e2e/full-calendar/month";

    [TestMethod]
    public async Task MonthGrid_FixedWeekCountDrawsSixRows_AndTheBorrowedDaysAreBlankedOrShown()
    {
        await OpenAsync(MonthPage);
        var cal = Page.Locator("#grid-cal");

        await Expect(cal.Locator(".bit-bfc-month-grid .bit-bfc-weeknum-cell")).ToHaveCountAsync(6);
        var blanks = cal.Locator(".bit-bfc-month-cell-blank");
        await Expect(blanks).ToHaveCountAsync(11);
        await Expect(blanks.First).ToHaveAttributeAsync("aria-hidden", "true");
        await Expect(blanks.Locator(".bit-bfc-cell-add-hint-month")).ToHaveCountAsync(0);
        await Expect(cal.Locator("[data-bit-bfc-event='m4']")).ToHaveCountAsync(0);

        await Page.Locator("#btn-noncurrent").ClickAsync();
        await Expect(blanks).ToHaveCountAsync(0);
        await Expect(cal.Locator(".bit-bfc-month-cell.other-month")).ToHaveCountAsync(11);
        await Expect(Badge(cal, "m4")).ToBeVisibleAsync();
        await Expect(cal.Locator(".bit-bfc-month-cell.other-month .bit-bfc-month-cell-day").First).ToHaveTextAsync("29");
        var muted = await cal.Locator(".bit-bfc-month-cell.other-month .bit-bfc-month-cell-day").First.EvaluateAsync<string>("e => getComputedStyle(e).color");
        var current = await cal.Locator(".bit-bfc-month-cell:not(.other-month) .bit-bfc-month-cell-day").First.EvaluateAsync<string>("e => getComputedStyle(e).color");
        Assert.AreNotEqual(current, muted, "the borrowed days are muted");

        await Page.Locator("#btn-fixed").ClickAsync();
        await Expect(cal.Locator(".bit-bfc-month-grid .bit-bfc-weeknum-cell")).ToHaveCountAsync(5);
        await Expect(cal.Locator(".bit-bfc-month-cell.other-month")).ToHaveCountAsync(4);
    }

    [TestMethod]
    public async Task MonthGrid_MaxEventsPerDayCell_DecidesWhenPlusMoreAppears()
    {
        await OpenAsync(MonthPage);
        var cal = Page.Locator("#grid-cal");
        var cell = MonthCell(cal, "Wednesday, July 16, 2025");

        await Expect(cell.Locator(".bit-bfc-event-badge")).ToHaveCountAsync(2);
        await Expect(cell.Locator(".bit-bfc-month-more")).ToHaveTextAsync("+1 more");

        await Page.Locator("#btn-max-1").ClickAsync();
        await Expect(cell.Locator(".bit-bfc-event-badge")).ToHaveCountAsync(1);
        await Expect(cell.Locator(".bit-bfc-month-more")).ToHaveTextAsync("+2 more");

        // clamped to 10: every event fits
        await Page.Locator("#btn-max-20").ClickAsync();
        await Expect(cell.Locator(".bit-bfc-event-badge")).ToHaveCountAsync(3);
        await Expect(cell.Locator(".bit-bfc-month-more")).ToHaveCountAsync(0);
    }

    [TestMethod]
    public async Task MonthGrid_NavLinks_TheDayNumberOpensTheDay_AndTheWeekNumberTheWeek()
    {
        await OpenAsync(MonthPage);
        var cal = Page.Locator("#grid-cal");
        var dayLink = cal.Locator("button.bit-bfc-navlink[aria-label='Go to Wednesday, July 16, 2025']");
        await Expect(dayLink).ToHaveTextAsync("16");
        await Expect(dayLink).ToHaveAttributeAsync("aria-current", "date");

        await dayLink.ClickAsync();
        await Expect(cal.Locator(".bit-bfc-day-layout")).ToBeVisibleAsync();
        await Expect(Title(cal)).ToHaveTextAsync("Jul 16, 2025");
        await Expect(Page.Locator("#grid-view")).ToHaveTextAsync("Day");
        // following a nav link opens the day, not the add dialog of the cell it sits in
        await Expect(Dialog(cal)).ToHaveCountAsync(0);

        await SwitchViewAsync(cal, "Month");
        await cal.Locator("button.bit-bfc-weeknum-cell[aria-label='Go to week 30']").ClickAsync();
        await Expect(cal.Locator(".bit-bfc-week-layout")).ToBeVisibleAsync();
        await Expect(Title(cal)).ToHaveTextAsync("Jul 20, 2025 - Jul 26, 2025");
        await Expect(Page.Locator("#grid-view")).ToHaveTextAsync("Week");

        // the week view's column headers are nav links too
        await cal.Locator("button.bit-bfc-week-header-day-link[aria-label='Go to Tuesday, July 22, 2025']").ClickAsync();
        await Expect(Title(cal)).ToHaveTextAsync("Jul 22, 2025");

        await SwitchViewAsync(cal, "Month");
        await Page.Locator("#btn-navlinks").ClickAsync();
        await Expect(cal.Locator("button.bit-bfc-navlink")).ToHaveCountAsync(0);
        await Expect(cal.Locator("div.bit-bfc-weeknum-cell .bit-bfc-sr-only").First).ToHaveTextAsync("Week 27");
    }

    [TestMethod]
    public async Task Dots_TheDotBadgeOfATwelveHourCalendar_ShowsTheColorAsABulletAndTheTimeWithAmPm()
    {
        await OpenAsync(MonthPage);
        var cal = Page.Locator("#dots-cal");
        var badge = Badge(cal, "d1");

        await Expect(badge).ToHaveClassAsync(HasClass("dot-variant"));
        await Expect(badge.Locator(".bit-bfc-event-badge-time")).ToHaveTextAsync("9:00 AM");
        await Expect(badge).ToHaveAttributeAsync("aria-label", "Morning, 7/16/2025 9:00 AM - 7/16/2025 10:00 AM");
        await Expect(badge.Locator(".bit-bfc-bullet")).ToHaveCSSAsync("background-color", "rgb(168, 85, 247)");
        await Expect(Badge(cal, "d2").Locator(".bit-bfc-bullet")).ToHaveCSSAsync("background-color", "rgb(249, 115, 22)");
        await Expect(Badge(cal, "d2").Locator(".bit-bfc-event-badge-time")).ToHaveTextAsync("7:00 PM");
    }

    [TestMethod]
    public async Task Colors_ThePaletteACssColorAndAnUnknownColor_AreEachDrawnInTheirColor()
    {
        await OpenAsync(MonthPage);
        var cal = Page.Locator("#colors-cal");
        await Expect(cal.Locator("select[aria-label='Filter events by color'] option")).ToHaveTextAsync(["All colors", "Work", "Personal", "Urgent"]);

        async Task ExpectSwatchAsync(string id, string label, string rgb)
        {
            await Badge(cal, id).ClickAsync();
            var dialog = Dialog(cal);
            await Expect(dialog.Locator(".bit-bfc-event-detail-swatch")).ToHaveCSSAsync("background-color", rgb);
            await Expect(dialog.Locator(".bit-bfc-event-detail-color")).ToHaveTextAsync(label);
            await PressEscapeAsync();
            await Expect(dialog).ToHaveCountAsync(0);
        }

        await ExpectSwatchAsync("c1", "Work", "rgb(37, 99, 235)");
        // an option is matched whatever its case
        await ExpectSwatchAsync("c2", "Personal", "rgb(22, 163, 74)");
        await ExpectSwatchAsync("c3", "#db2777", "rgb(219, 39, 119)");
        // a color nothing knows takes --bit-FullCalendar-event-color, set on an ancestor
        await Badge(cal, "c4").ClickAsync();
        await Expect(Dialog(cal).Locator(".bit-bfc-event-detail-swatch")).ToHaveCSSAsync("background-color", "rgb(100, 116, 139)");
        await PressEscapeAsync();

        // the editor offers the palette
        await Badge(cal, "c1").ClickAsync();
        await Dialog(cal).GetByRole(AriaRole.Button, new() { Name = "Edit" }).ClickAsync();
        await Expect(EditDialog(cal).Locator("select[aria-label='Event color'] option")).ToHaveTextAsync(["Work", "Personal", "Urgent"]);
        await EditDialog(cal).GetByRole(AriaRole.Button, new() { Name = "Cancel" }).ClickAsync();
        await PressEscapeAsync();

        await cal.Locator("select[aria-label='Filter events by color']").SelectOptionAsync("personal");
        await Expect(Badge(cal, "c2")).ToBeVisibleAsync();
        await Expect(cal.Locator("[data-bit-bfc-event='c1']")).ToHaveCountAsync(0);
    }

    [TestMethod]
    public async Task Colors_TheAgendaGroupsByThePaletteTitles()
    {
        await OpenAsync(MonthPage);
        var cal = Page.Locator("#colors-cal");
        await SwitchViewAsync(cal, "Agenda");

        var panel = await OpenSettingsAsync(cal);
        await panel.GetByRole(AriaRole.Radio, new() { Name = "Color" }).ClickAsync();

        await Expect(cal.Locator(".bit-bfc-agenda-group-title").First).ToHaveTextAsync("Work");
        await Expect(cal.Locator(".bit-bfc-agenda-group-title")).ToContainTextAsync([new Regex("Work"), new Regex("Personal")]);
    }
}
