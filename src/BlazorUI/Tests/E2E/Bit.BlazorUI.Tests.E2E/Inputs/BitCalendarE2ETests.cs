using System.Globalization;
using System.Text.RegularExpressions;

namespace Bit.BlazorUI.Tests.E2E.Inputs;

/// <summary>
/// End-to-end tests of BitCalendar on <c>/e2e/calendar</c>, where every calendar is pinned to en-US, UTC and a fixed today
/// (Friday, March 15, 2024): the prerendered day grid, selecting by pointer and keyboard, the month navigation and
/// go-to-today, disabled / read-only, two-way, one-way and uncontrolled binding with AllowDeselect, StartingValue,
/// Today and OnMonthChange, MinDate / MaxDate, DisablePast / DisableFuture, disabled and highlighted days with
/// GetDayClass, week numbers and the week rules, ShowOutsideDays and FixedWeeks, the month and year pickers (beside and as
/// an overlay, highlighted months, every title), the time picker (24h and 12h, overlay, steps, spin buttons, the now
/// button, the page keys), events with their dialog (dots, colors, labels, Escape, focus return, templates, texts), the
/// cell / header / footer templates, an fa-IR (Persian, right-to-left) and an en-US culture, a custom TimeZone, EditForm
/// validation, the WAI-ARIA grid keyboard of the day, month and year grids, FocusAsync, multiple months and paged
/// navigation, the BitParams cascade, every color and size against the computed styles, external icons, Style / Class /
/// Styles / Classes, the public CSS variables, RTL, the Escape contract inside a BitDialog, the disabled-markup and
/// activation contracts, runtime re-renders, unmounting and reduced motion.
/// </summary>
[TestClass]
public class BitCalendarE2ETests : E2ETestBase
{
    private const string Url = "/e2e/calendar";

    private static readonly (string Suffix, string Main, string On)[] Colors =
    [
        ("pri", "pri", "pri-text"), ("sec", "sec", "sec-text"), ("ter", "ter", "ter-text"), ("inf", "inf", "inf-text"),
        ("suc", "suc", "suc-text"), ("wrn", "wrn", "wrn-text"), ("swr", "swr", "swr-text"), ("err", "err", "err-text"),
        ("pbg", "bg-pri", "fg-pri"), ("sbg", "bg-sec", "fg-pri"), ("tbg", "bg-ter", "fg-pri"),
        ("pfg", "fg-pri", "bg-pri"), ("sfg", "fg-sec", "bg-pri"), ("tfg", "fg-ter", "bg-pri"),
        ("pbr", "brd-pri", "fg-pri"), ("sbr", "brd-sec", "fg-pri"), ("tbr", "brd-ter", "fg-pri")
    ];

    private static readonly Regex AnyValue = new(".*");

    private ILocator Root(string id) => Page.Locator($"#{id}");
    private ILocator Day(string id, string date) => Page.Locator($"#{id}-day-{date}");
    private ILocator Month(string id, int month) => Page.Locator($"#{id}-month-{month:D2}");
    private ILocator Year(string id, int year) => Page.Locator($"#{id}-year-{year:D4}");
    private ILocator Title(string id) => Page.Locator($"#{id} .bit-cal-dwp .bit-cal-pkh").First.Locator(".bit-cal-pkt, .bit-cal-ptb").First;
    private ILocator PrevMonth(string id) => Page.Locator($"#{id} .bit-cal-dwp button.bit-cal-nbt").First;
    private ILocator NextMonth(string id) => Page.Locator($"#{id} .bit-cal-dwp button.bit-cal-nbt").Last;
    private ILocator GoToToday(string id) => Page.Locator($"#{id} .bit-cal-gtb").First;
    private ILocator PickerToggle(string id) => Page.Locator($"#{id} .bit-cal-mwp .bit-cal-ptb");
    private ILocator Span(string id) => Page.Locator($"#{id}");
    private ILocator HourInput(string id) => Page.Locator($"#{id} .bit-cal-tin").Nth(0);
    private ILocator MinuteInput(string id) => Page.Locator($"#{id} .bit-cal-tin").Nth(1);
    private ILocator TimeButton(string id, int index) => Page.Locator($"#{id} .bit-cal-tpr .bit-cal-tbt").Nth(index);
    private ILocator EventDialog(string id) => Page.Locator($"#{id} [role=dialog]");

    private Task<string> ResolveColorAsync(string cssColor)
    {
        return Page.EvaluateAsync<string>(@"v => {
            const d = document.createElement('div');
            d.style.color = v;
            document.body.appendChild(d);
            const c = getComputedStyle(d).color;
            d.remove();
            return c;
        }", cssColor);
    }

    private static Task<string> StyleOfAsync(ILocator locator, string property, string? pseudo = null)
    {
        var pseudoArg = pseudo is null ? "null" : $"'{pseudo}'";
        return locator.EvaluateAsync<string>($"el => getComputedStyle(el, {pseudoArg}).getPropertyValue('{property}')");
    }

    private static async Task<LocatorBoundingBoxResult> BoxOfAsync(ILocator locator)
    {
        var box = await locator.BoundingBoxAsync();
        Assert.IsNotNull(box, "The element has no layout box.");
        return box;
    }

    private static double Px(string value) => double.Parse(value.Replace("px", string.Empty), CultureInfo.InvariantCulture);

    /// <summary>
    /// Presses a spin button of the time picker the way a pointer does: down, then up.
    /// </summary>
    private static async Task SpinAsync(ILocator button)
    {
        await button.DispatchEventAsync("pointerdown");
        await button.DispatchEventAsync("pointerup");
    }

    [TestMethod]
    public async Task Prerender_TheServerHtml_ContainsTheDayGridBeforeTheCircuitStarts()
    {
        var response = await Page.APIRequest.GetAsync($"{BaseUrl}{Url}");
        var html = await response.TextAsync();

        Assert.IsTrue(response.Ok);
        StringAssert.Contains(html, "id=\"cal-basic\"");
        StringAssert.Contains(html, "March 2024");
        StringAssert.Contains(html, "<span id=\"status\">Loading</span>");
        Assert.IsTrue(Regex.IsMatch(html, "id=\"cal-basic-day-2024-03-15\"[^>]*aria-current=\"date\""), "Today is prerendered as the current date.");

        await OpenAsync(Url);
        await Expect(Title("cal-basic")).ToHaveTextAsync("March 2024");
    }

    [TestMethod]
    public async Task Basic_ClickingADay_SelectsItMarksItAndAnnouncesIt()
    {
        await OpenAsync(Url);

        await Expect(Page.Locator("#cal-basic .bit-cal-grd").First).ToHaveAttributeAsync("aria-label", "March 2024");
        var headers = Page.Locator("#cal-basic .bit-cal-dgh [role=columnheader]");
        await Expect(headers).ToHaveCountAsync(7);
        await Expect(headers.Nth(0)).ToHaveAttributeAsync("aria-label", "Sunday");
        await Expect(headers.Nth(0)).ToHaveTextAsync("S");
        await Expect(Day("cal-basic", "2024-02-25")).ToHaveClassAsync(HasClass("bit-cal-dbo"));
        await Expect(Day("cal-basic", "2024-03-15")).ToHaveAttributeAsync("aria-current", "date");
        await Expect(Day("cal-basic", "2024-03-15")).ToHaveClassAsync(HasClass("bit-cal-dtd"));
        await Expect(Day("cal-basic", "2024-03-15")).ToHaveAttributeAsync("aria-label", "Friday, March 15, 2024");

        await Day("cal-basic", "2024-03-20").ClickAsync();

        await Expect(Span("basic-value")).ToHaveTextAsync("2024-03-20 00:00 +00:00");
        await Expect(Span("basic-selects")).ToHaveTextAsync("1");
        await Expect(Day("cal-basic", "2024-03-20")).ToHaveAttributeAsync("aria-selected", "true");
        await Expect(Day("cal-basic", "2024-03-20")).ToHaveClassAsync(HasClass("bit-cal-dbs"));
        await Expect(Day("cal-basic", "2024-03-19")).Not.ToHaveAttributeAsync("aria-selected", AnyValue);
        await Expect(Page.Locator("#cal-basic .bit-cal-sdt")).ToHaveTextAsync("Selected date 3/20/2024");
        await Expect(Day("cal-basic", "2024-03-15")).ToHaveClassAsync(HasClass("bit-cal-dtd"));
    }

    [TestMethod]
    public async Task Basic_TheNavigationButtons_MoveTheMonthAndGoToTodayComesBack()
    {
        await OpenAsync(Url);

        await Expect(GoToToday("cal-basic")).ToBeDisabledAsync();

        await NextMonth("cal-basic").ClickAsync();
        await Expect(Title("cal-basic")).ToHaveTextAsync("April 2024");
        await Expect(Day("cal-basic", "2024-04-30")).ToBeVisibleAsync();

        await PrevMonth("cal-basic").ClickAsync();
        await PrevMonth("cal-basic").ClickAsync();
        await Expect(Title("cal-basic")).ToHaveTextAsync("February 2024");

        await Expect(GoToToday("cal-basic")).ToBeEnabledAsync();
        await GoToToday("cal-basic").ClickAsync();
        await Expect(Title("cal-basic")).ToHaveTextAsync("March 2024");
        await Expect(GoToToday("cal-basic")).ToBeDisabledAsync();
    }

    [TestMethod]
    public async Task Basic_ClickingAnOutsideDay_SelectsItAndMovesToItsMonth()
    {
        await OpenAsync(Url);

        await Day("cal-basic", "2024-04-01").ClickAsync();

        await Expect(Span("basic-value")).ToHaveTextAsync("2024-04-01 00:00 +00:00");
        await Expect(Title("cal-basic")).ToHaveTextAsync("April 2024");
        await Expect(Day("cal-basic", "2024-04-01")).Not.ToHaveClassAsync(HasClass("bit-cal-dbo"));
    }

    [TestMethod]
    public async Task Disabled_RendersTheDisabledMarkupAndIgnoresEveryInteraction()
    {
        await OpenAsync(Url);

        await Expect(Root("cal-disabled")).ToHaveClassAsync(HasClass("bit-dis"));
        await Expect(Page.Locator("#cal-disabled .bit-cal-grd").First).ToHaveAttributeAsync("aria-disabled", "true");
        await Expect(Day("cal-disabled", "2024-03-20")).ToHaveAttributeAsync("disabled", "");
        await Expect(Day("cal-disabled", "2024-03-20")).ToHaveAttributeAsync("aria-disabled", "true");
        await Expect(NextMonth("cal-disabled")).ToHaveAttributeAsync("disabled", "");
        await Expect(NextMonth("cal-disabled")).ToHaveAttributeAsync("aria-disabled", "true");
        await Expect(Month("cal-disabled", 6)).ToHaveAttributeAsync("disabled", "");

        await Day("cal-disabled", "2024-03-20").ClickAsync(new() { Force = true });
        await NextMonth("cal-disabled").ClickAsync(new() { Force = true });
        await Day("cal-basic", "2024-03-21").ClickAsync();
        await Expect(Span("basic-value")).ToHaveTextAsync("2024-03-21 00:00 +00:00");

        await Expect(Day("cal-disabled", "2024-03-20")).Not.ToHaveAttributeAsync("aria-selected", AnyValue);
        await Expect(Title("cal-disabled")).ToHaveTextAsync("March 2024");
    }

    [TestMethod]
    public async Task ReadOnly_BrowsesButNeverSelects()
    {
        await OpenAsync(Url);

        await Expect(Page.Locator("#cal-readonly .bit-cal-grd").First).ToHaveAttributeAsync("aria-readonly", "true");
        await Expect(Day("cal-readonly", "2024-03-20")).ToHaveAttributeAsync("aria-readonly", "true");
        await Expect(Day("cal-readonly", "2024-03-20")).ToHaveAttributeAsync("aria-disabled", "true");
        await Expect(Day("cal-readonly", "2024-03-20")).Not.ToHaveAttributeAsync("disabled", AnyValue);

        await Day("cal-readonly", "2024-03-20").ClickAsync(new() { Force = true });
        await Day("cal-basic", "2024-03-21").ClickAsync();
        await Expect(Span("basic-value")).ToHaveTextAsync("2024-03-21 00:00 +00:00");
        await Expect(Span("readonly-value")).ToHaveTextAsync("null");

        await NextMonth("cal-readonly").ClickAsync();
        await Expect(Title("cal-readonly")).ToHaveTextAsync("April 2024");
    }

    [TestMethod]
    public async Task ShowGoToToday_False_HidesTheButton()
    {
        await OpenAsync(Url);

        await Expect(Page.Locator("#cal-no-today .bit-cal-gtb")).ToHaveCountAsync(0);
        await Expect(Page.Locator("#cal-basic .bit-cal-gtb")).ToHaveCountAsync(1);
    }

    [TestMethod]
    public async Task DateFormatSelectedDateAriaAtomicAndHighlightToday_ShapeWhatIsAnnouncedAndPainted()
    {
        await OpenAsync(Url);

        const string id = "cal-format";
        await Expect(Day(id, "2024-03-15")).Not.ToHaveClassAsync(HasClass("bit-cal-dtd"));
        await Expect(Day(id, "2024-03-15")).ToHaveAttributeAsync("aria-current", "date");
        Assert.AreNotEqual(await StyleOfAsync(Day("cal-basic", "2024-03-15"), "background-color"), await StyleOfAsync(Day(id, "2024-03-15"), "background-color"));

        await Day(id, "2024-03-20").ClickAsync();
        await Expect(Page.Locator($"#{id} .bit-cal-sdt")).ToHaveTextAsync("Picked 2024/03/20");

        // The event dialog is headed with the date in the same format.
        await Day(id, "2024-03-12").ClickAsync();
        await Expect(EventDialog(id)).ToHaveAccessibleNameAsync("2024/03/12");
    }

    [TestMethod]
    public async Task Binding_TwoWay_FlowsBothWaysAndMovesTheView()
    {
        await OpenAsync(Url);

        await Day("cal-twoway", "2024-03-22").ClickAsync();
        await Expect(Span("twoway-value")).ToHaveTextAsync("2024-03-22 00:00 +00:00");

        await Page.Locator("#btn-twoway-set").ClickAsync();
        await Expect(Title("cal-twoway")).ToHaveTextAsync("May 2024");
        await Expect(Day("cal-twoway", "2024-05-10")).ToHaveAttributeAsync("aria-selected", "true");

        await Page.Locator("#btn-twoway-clear").ClickAsync();
        await Expect(Page.Locator("#cal-twoway .bit-cal-dbt[aria-selected=true]")).ToHaveCountAsync(0);
        await Expect(Span("twoway-value")).ToHaveTextAsync("null");
    }

    [TestMethod]
    public async Task Binding_OnSelectDateOneWayAndAllowDeselect_BehaveAsTheirShapesSay()
    {
        await OpenAsync(Url);

        await Day("cal-onselect", "2024-03-09").ClickAsync();
        await Expect(Span("onselect-value")).ToHaveTextAsync("2024-03-09 00:00 +00:00");
        await Expect(Day("cal-onselect", "2024-03-09")).ToHaveAttributeAsync("aria-selected", "true");

        // A one-way value is not moved by a click.
        await Day("cal-oneway", "2024-03-13").ClickAsync();
        await Day("cal-basic", "2024-03-21").ClickAsync();
        await Expect(Span("basic-value")).ToHaveTextAsync("2024-03-21 00:00 +00:00");
        await Expect(Day("cal-oneway", "2024-03-12")).ToHaveAttributeAsync("aria-selected", "true");
        await Expect(Day("cal-oneway", "2024-03-13")).Not.ToHaveAttributeAsync("aria-selected", AnyValue);

        await Expect(Day("cal-deselect", "2024-03-12")).ToHaveAttributeAsync("aria-selected", "true");
        await Day("cal-deselect", "2024-03-12").ClickAsync();
        await Expect(Span("deselect-value")).ToHaveTextAsync("null");
        await Expect(Title("cal-deselect")).ToHaveTextAsync("March 2024");
        await Day("cal-deselect", "2024-03-12").ClickAsync();
        await Expect(Span("deselect-value")).ToHaveTextAsync("2024-03-12 00:00 +00:00");
    }

    [TestMethod]
    public async Task StartingView_StartingValueTodayAndOnMonthChange()
    {
        await OpenAsync(Url);

        await Expect(Title("cal-starting")).ToHaveTextAsync("December 2020");
        await Expect(HourInput("cal-starting")).ToHaveValueAsync("8");
        await Expect(MinuteInput("cal-starting")).ToHaveValueAsync("30");

        await Expect(Title("cal-today")).ToHaveTextAsync("March 2021");
        await Expect(Day("cal-today", "2021-03-15")).ToHaveAttributeAsync("aria-current", "date");

        await NextMonth("cal-monthchange").ClickAsync();
        await Expect(Span("monthchange-value")).ToHaveTextAsync("1:2024-04-01");
        await PrevMonth("cal-monthchange").ClickAsync();
        await Expect(Span("monthchange-value")).ToHaveTextAsync("2:2024-03-01");
    }

    [TestMethod]
    public async Task MinAndMax_DisableTheDaysOutsideAndStopTheNavigationAtTheEdges()
    {
        await OpenAsync(Url);

        await Expect(Day("cal-minmax", "2024-03-09")).ToBeDisabledAsync();
        await Expect(Day("cal-minmax", "2024-03-10")).ToBeEnabledAsync();
        await Expect(Day("cal-minmax", "2024-03-20")).ToBeEnabledAsync();
        await Expect(Day("cal-minmax", "2024-03-21")).ToBeDisabledAsync();
        await Expect(PrevMonth("cal-minmax")).ToBeDisabledAsync();
        await Expect(NextMonth("cal-minmax")).ToBeDisabledAsync();
        await Expect(Month("cal-minmax", 2)).ToBeDisabledAsync();
        await Expect(Month("cal-minmax", 3)).ToBeEnabledAsync();

        await Day("cal-minmax", "2024-03-10").FocusAsync();
        await Page.Keyboard.PressAsync("ArrowLeft");
        await Expect(Day("cal-minmax", "2024-03-10")).ToBeFocusedAsync();

        // The year navigation of the month picker stops at the year of the bound.
        for (var i = 0; i < 5; i++)
        {
            await Page.Locator("#cal-minmax-years .bit-cal-mwp .bit-cal-nbt").First.ClickAsync();
        }
        await Expect(PickerToggle("cal-minmax-years")).ToHaveTextAsync("2019");
        await Expect(Page.Locator("#cal-minmax-years .bit-cal-mwp .bit-cal-nbt").First).ToBeDisabledAsync();
        await Expect(Month("cal-minmax-years", 2)).ToBeDisabledAsync();
        await Expect(Month("cal-minmax-years", 3)).ToBeEnabledAsync();
    }

    [TestMethod]
    public async Task DisablePastAndDisableFuture_BoundTheDaysByTodayAndKeepTodaySelectable()
    {
        await OpenAsync(Url);

        await Expect(Day("cal-past", "2024-03-14")).ToBeDisabledAsync();
        await Expect(Day("cal-past", "2024-03-15")).ToBeEnabledAsync();
        await Expect(Day("cal-past", "2024-03-16")).ToBeEnabledAsync();
        await Expect(PrevMonth("cal-past")).ToBeDisabledAsync();

        await Expect(Day("cal-future", "2024-03-16")).ToBeDisabledAsync();
        await Expect(Day("cal-future", "2024-03-15")).ToBeEnabledAsync();
        await Expect(NextMonth("cal-future")).ToBeDisabledAsync();
    }

    [TestMethod]
    public async Task DisabledDays_RuleOutWeekdaysDatesAndAPredicateAndTheArrowKeysSkipThem()
    {
        await OpenAsync(Url);

        await Expect(Day("cal-weekends", "2024-03-16")).ToBeDisabledAsync();
        await Expect(Day("cal-weekends", "2024-03-17")).ToBeDisabledAsync();
        await Expect(Day("cal-weekends", "2024-03-18")).ToBeEnabledAsync();
        await Expect(Day("cal-weekends", "2024-03-16")).ToHaveAttributeAsync("aria-disabled", "true");

        await Expect(Day("cal-disabled-dates", "2024-03-06")).ToBeDisabledAsync();
        await Expect(Day("cal-disabled-dates", "2024-03-08")).ToBeDisabledAsync();
        await Expect(Day("cal-disabled-dates", "2024-03-07")).ToBeDisabledAsync();
        await Expect(Day("cal-disabled-dates", "2024-03-04")).ToBeEnabledAsync();

        await Day("cal-disabled-dates", "2024-03-04").FocusAsync();
        await Page.Keyboard.PressAsync("ArrowRight");
        await Expect(Day("cal-disabled-dates", "2024-03-10")).ToBeFocusedAsync();
    }

    [TestMethod]
    public async Task HighlightedDatesAndGetDayClass_MarkDaysWithoutDisablingThem()
    {
        await OpenAsync(Url);

        await Expect(Day("cal-highlight", "2024-03-08")).ToHaveClassAsync(HasClass("bit-cal-dhl"));
        await Expect(Day("cal-highlight", "2024-03-08")).ToBeEnabledAsync();
        await Expect(Day("cal-highlight", "2024-03-08")).ToHaveCSSAsync("background-color", await ResolveColorAsync("var(--bit-clr-bg-ter)"));
        await Expect(Day("cal-highlight", "2024-03-09")).Not.ToHaveClassAsync(HasClass("bit-cal-dhl"));

        await Expect(Day("cal-highlight", "2024-03-10")).ToHaveClassAsync(HasClass("sunday-cell"));
        await Expect(Day("cal-highlight", "2024-03-11")).Not.ToHaveClassAsync(HasClass("sunday-cell"));
    }

    [TestMethod]
    public async Task WeekLayout_IsoWeekNumbersAndAMondayStart()
    {
        await OpenAsync(Url);

        var headers = Page.Locator("#cal-iso .bit-cal-dgh [role=columnheader]");
        await Expect(headers).ToHaveCountAsync(8);
        await Expect(headers.Nth(0)).ToHaveAttributeAsync("aria-label", "Hafte");
        await Expect(headers.Nth(1)).ToHaveAttributeAsync("aria-label", "Monday");

        var weekNumbers = Page.Locator("#cal-iso [role=rowheader]");
        await Expect(weekNumbers.Nth(0)).ToHaveTextAsync("9");
        await Expect(weekNumbers.Nth(0)).ToHaveAttributeAsync("title", "Hafte 9");
        await Expect(weekNumbers.Nth(2)).ToHaveTextAsync("11");

        var firstRowFirstDay = Page.Locator("#cal-iso .bit-cal-dgr").Nth(0).Locator(".bit-cal-dbt").First;
        await Expect(firstRowFirstDay).ToHaveAttributeAsync("aria-label", "Monday, February 26, 2024");
    }

    [TestMethod]
    public async Task ShowOutsideDaysAndFixedWeeks_ShapeTheGrid()
    {
        await OpenAsync(Url);

        await Expect(Page.Locator("#cal-no-outside .bit-cal-dbo")).ToHaveCountAsync(0);
        await Expect(Page.Locator("#cal-no-outside .bit-cal-dgr").Nth(0).Locator(".bit-cal-dbe")).ToHaveCountAsync(5);
        await Expect(Page.Locator("#cal-basic .bit-cal-dbo").First).ToBeVisibleAsync();

        await Expect(Title("cal-feb")).ToHaveTextAsync("February 2026");
        await Expect(Page.Locator("#cal-feb .bit-cal-dgr")).ToHaveCountAsync(4);
        await Expect(Page.Locator("#cal-fixed .bit-cal-dgr")).ToHaveCountAsync(6);
    }

    [TestMethod]
    public async Task MonthPicker_BesideTheDayGrid_PicksAMonthAndAYear()
    {
        await OpenAsync(Url);

        await Expect(PickerToggle("cal-pickers")).ToHaveTextAsync("2024");
        await Expect(PickerToggle("cal-pickers")).ToHaveAttributeAsync("aria-label", "2024, change year");
        await Expect(Month("cal-pickers", 6)).ToHaveTextAsync("Jun");
        await Expect(Month("cal-pickers", 6)).ToHaveAttributeAsync("aria-label", "June");
        await Expect(Month("cal-pickers", 3)).ToHaveAttributeAsync("aria-current", "date");

        await Month("cal-pickers", 6).ClickAsync();
        await Expect(Title("cal-pickers")).ToHaveTextAsync("June 2024");
        await Expect(Month("cal-pickers", 6)).ToHaveAttributeAsync("aria-selected", "true");

        var yearNav = Page.Locator("#cal-pickers .bit-cal-mwp .bit-cal-nbt");
        await yearNav.Last.ClickAsync();
        await Expect(PickerToggle("cal-pickers")).ToHaveTextAsync("2025");
        await Expect(Title("cal-pickers")).ToHaveTextAsync("June 2025");
        await yearNav.First.ClickAsync();
        await Expect(PickerToggle("cal-pickers")).ToHaveTextAsync("2024");

        await PickerToggle("cal-pickers").ClickAsync();
        await Expect(Page.Locator("#cal-pickers .bit-cal-mwp .bit-cal-pkb")).ToHaveCountAsync(12);
        await Expect(PickerToggle("cal-pickers")).ToHaveTextAsync(new Regex(@"2023\s*-\s*2034"));
        await Expect(Year("cal-pickers", 2024)).ToHaveAttributeAsync("aria-current", "date");

        await Year("cal-pickers", 2026).ClickAsync();
        await Expect(PickerToggle("cal-pickers")).ToHaveTextAsync("2026");
        await Expect(Title("cal-pickers")).ToHaveTextAsync("June 2026");

        // The year range navigation, from the range around the year that was picked.
        await PickerToggle("cal-pickers").ClickAsync();
        await Expect(PickerToggle("cal-pickers")).ToHaveTextAsync(new Regex(@"2025\s*-\s*2036"));
        await Page.Locator("#cal-pickers .bit-cal-mwp .bit-cal-nbt").Last.ClickAsync();
        await Expect(Year("cal-pickers", 2040)).ToBeVisibleAsync();
        await Page.Locator("#cal-pickers .bit-cal-mwp .bit-cal-nbt").First.ClickAsync();
        await Expect(Year("cal-pickers", 2026)).ToBeVisibleAsync();
    }

    [TestMethod]
    public async Task ShowMonthPicker_TogglesThePickerAtRuntime()
    {
        await OpenAsync(Url);

        await Expect(Page.Locator("#cal-no-monthpicker .bit-cal-mwp")).ToHaveCountAsync(1);

        await Page.Locator("#btn-toggle-monthpicker").ClickAsync();
        await Expect(Page.Locator("#cal-no-monthpicker .bit-cal-mwp")).ToHaveCountAsync(0);
        await Expect(Page.Locator("#cal-no-monthpicker .bit-cal-dwp")).ToBeVisibleAsync();

        await Page.Locator("#btn-toggle-monthpicker").ClickAsync();
        await Expect(Page.Locator("#cal-no-monthpicker .bit-cal-mwp")).ToHaveCountAsync(1);
    }

    [TestMethod]
    public async Task MonthPickerAsOverlay_OpensFromTheTitleAndEscapeLeavesIt()
    {
        await OpenAsync(Url);

        await Expect(Page.Locator("#cal-overlay .bit-cal-mwp")).ToHaveCountAsync(0);
        var toggle = Page.Locator("#cal-overlay .bit-cal-dwp .bit-cal-ptb");
        await Expect(toggle).ToHaveAttributeAsync("aria-label", "March 2024, change month");

        await toggle.ClickAsync();
        await Expect(Page.Locator("#cal-overlay .bit-cal-mwp")).ToBeVisibleAsync();
        await Expect(Page.Locator("#cal-overlay .bit-cal-dwp")).ToHaveCountAsync(0);
        await Expect(Page.Locator("#cal-overlay .bit-cal-mwp")).ToHaveAttributeAsync("data-bit-esc", "claim");

        await Month("cal-overlay", 8).ClickAsync();
        await Expect(Page.Locator("#cal-overlay .bit-cal-mwp")).ToHaveCountAsync(0);
        await Expect(Title("cal-overlay")).ToHaveTextAsync("August 2024");

        // Escape walks back out: the year grid to the month grid, the month grid out of the overlay.
        await Page.Locator("#cal-overlay .bit-cal-dwp .bit-cal-ptb").ClickAsync();
        await PickerToggle("cal-overlay").ClickAsync();
        await Expect(Year("cal-overlay", 2024)).ToBeVisibleAsync();
        await Year("cal-overlay", 2024).FocusAsync();
        await Page.Keyboard.PressAsync("Escape");
        await Expect(Month("cal-overlay", 8)).ToBeVisibleAsync();
        await Expect(Page.Locator("#cal-overlay [role=gridcell]:focus")).ToHaveCountAsync(1);
        await Page.Keyboard.PressAsync("Escape");
        await Expect(Page.Locator("#cal-overlay .bit-cal-mwp")).ToHaveCountAsync(0);
        await Expect(Page.Locator("#cal-overlay .bit-cal-dwp")).ToBeVisibleAsync();

        // Beside the day grid the month picker leaves Escape alone.
        await Expect(Page.Locator("#cal-pickers .bit-cal-mwp")).Not.ToHaveAttributeAsync("data-bit-esc", AnyValue);
    }

    [TestMethod]
    public async Task HighlightCurrentAndSelectedMonth_PaintTheMonthGrid()
    {
        await OpenAsync(Url);

        await Expect(Month("cal-highlight-month", 3)).ToHaveClassAsync(HasClass("bit-cal-pcm"));
        await Expect(Month("cal-highlight-month", 3)).ToHaveCSSAsync("background-color", await ResolveColorAsync("var(--bit-clr-pri)"));
        await Expect(Month("cal-highlight-month", 3)).ToHaveCSSAsync("color", await ResolveColorAsync("var(--bit-clr-pri-text)"));
        await Expect(Month("cal-highlight-month", 6)).ToHaveClassAsync(HasClass("bit-cal-psm"));
        await Expect(Month("cal-highlight-month", 6)).ToHaveCSSAsync("background-color", await ResolveColorAsync("var(--bit-clr-bg-sec)"));

        await Expect(Month("cal-pickers", 3)).Not.ToHaveClassAsync(HasClass("bit-cal-pcm"));
        await Expect(Month("cal-pickers", 3)).Not.ToHaveClassAsync(HasClass("bit-cal-psm"));
    }

    [TestMethod]
    public async Task Titles_EveryNavigationTitleIsOverridable()
    {
        await OpenAsync(Url);

        const string id = "cal-picker-titles";
        var toggle = Page.Locator($"#{id} .bit-cal-dwp .bit-cal-ptb");
        await Expect(toggle).ToHaveAttributeAsync("aria-label", "March 2024, pick a month");
        await Expect(PrevMonth(id)).ToHaveAttributeAsync("title", "Back a month");
        await Expect(NextMonth(id)).ToHaveAttributeAsync("title", "On a month");

        await toggle.ClickAsync();
        await Expect(PickerToggle(id)).ToHaveAttributeAsync("aria-label", "2024, pick a year");
        var nav = Page.Locator($"#{id} .bit-cal-mwp .bit-cal-nbt");
        await Expect(nav.First).ToHaveAttributeAsync("aria-label", "Back to 2023");
        await Expect(nav.Last).ToHaveAttributeAsync("aria-label", "On to 2025");
        await Expect(GoToToday(id)).ToHaveAttributeAsync("title", "Back to today");

        await PickerToggle(id).ClickAsync();
        await Expect(PickerToggle(id)).ToHaveAttributeAsync("aria-label", "2023 to 2034");
        await Expect(nav.First).ToHaveAttributeAsync("aria-label", "Years 2011 to 2022");
        await Expect(nav.Last).ToHaveAttributeAsync("aria-label", "Years 2035 to 2046 next");
    }

    [TestMethod]
    public async Task TimePicker_TheSpinButtonsTheInputsTheNowButtonAndThePageKeysSetTheTime()
    {
        await OpenAsync(Url);

        const string id = "cal-time";
        await Expect(HourInput(id)).ToHaveAttributeAsync("aria-label", "Hours");
        await Expect(MinuteInput(id)).ToHaveAttributeAsync("aria-label", "Minutes");
        await Expect(HourInput(id)).ToHaveAttributeAsync("max", "23");
        await Expect(TimeButton(id, 0)).ToHaveAttributeAsync("aria-label", "More hours");
        await Expect(TimeButton(id, 1)).ToHaveAttributeAsync("aria-label", "Fewer hours");
        await Expect(TimeButton(id, 2)).ToHaveAttributeAsync("aria-label", "More minutes");
        await Expect(TimeButton(id, 3)).ToHaveAttributeAsync("aria-label", "Fewer minutes");
        await Expect(Page.Locator($"#{id} .bit-cal-gtn")).ToHaveAttributeAsync("aria-label", "Right now");

        await Day(id, "2024-03-20").ClickAsync();
        await Expect(Span("time-value")).ToHaveTextAsync("2024-03-20 00:00 +00:00");

        await SpinAsync(TimeButton(id, 0));
        await Expect(Span("time-value")).ToHaveTextAsync("2024-03-20 01:00 +00:00");
        await SpinAsync(TimeButton(id, 2));
        await Expect(Span("time-value")).ToHaveTextAsync("2024-03-20 01:01 +00:00");
        await SpinAsync(TimeButton(id, 3));
        await SpinAsync(TimeButton(id, 1));
        await Expect(Span("time-value")).ToHaveTextAsync("2024-03-20 00:00 +00:00");

        await MinuteInput(id).FillAsync("30");
        await MinuteInput(id).PressAsync("Tab");
        await Expect(Span("time-value")).ToHaveTextAsync("2024-03-20 00:30 +00:00");

        await HourInput(id).FocusAsync();
        await Page.Keyboard.PressAsync("PageUp");
        await Expect(Span("time-value")).ToHaveTextAsync("2024-03-20 01:30 +00:00");
        await Page.Keyboard.PressAsync("PageDown");
        await Expect(Span("time-value")).ToHaveTextAsync("2024-03-20 00:30 +00:00");

        await Page.Locator($"#{id} .bit-cal-gtn").ClickAsync();
        await Expect(Span("time-value")).ToHaveTextAsync("2024-03-20 10:00 +00:00");
    }

    [TestMethod]
    public async Task TimePicker_ClickingATimeInput_SelectsItsTextSoADigitReplacesIt()
    {
        await OpenAsync(Url);

        const string id = "cal-time";
        await Day(id, "2024-03-20").ClickAsync();
        await Expect(Span("time-value")).ToHaveTextAsync("2024-03-20 00:00 +00:00");

        // WebKit collapses the selection the focus made to a caret at the press point of the click, which the calendar
        // answers by selecting the text again once the press is released: the digit typed next replaces the hour
        // rather than being added to it.
        await HourInput(id).ClickAsync();
        await Page.Keyboard.TypeAsync("5");
        await Expect(HourInput(id)).ToHaveValueAsync("5");
    }

    [TestMethod]
    public async Task TimePicker_TwelveHours_HasAnAmPmPairAndNoNowButton()
    {
        await OpenAsync(Url);

        const string id = "cal-time12";
        await Expect(Page.Locator($"#{id} .bit-cal-gtn")).ToHaveCountAsync(0);
        await Expect(HourInput(id)).ToHaveAttributeAsync("max", "12");
        await Expect(HourInput(id)).ToHaveAttributeAsync("min", "1");

        var am = Page.Locator($"#{id} .bit-cal-bam");
        var pm = Page.Locator($"#{id} .bit-cal-bpm");
        await Expect(am).ToHaveTextAsync("AM");
        await Expect(pm).ToHaveTextAsync("PM");

        await Day(id, "2024-03-20").ClickAsync();
        await Expect(am).ToHaveAttributeAsync("aria-pressed", "true");
        await Expect(am).ToHaveClassAsync(HasClass("bit-cal-bns"));
        await Expect(pm).ToHaveAttributeAsync("aria-pressed", "false");

        await pm.ClickAsync();
        await Expect(Span("time12-value")).ToHaveTextAsync("2024-03-20 12:00 +00:00");
        await Expect(pm).ToHaveAttributeAsync("aria-pressed", "true");
        await Expect(HourInput(id)).ToHaveValueAsync("12");

        await am.ClickAsync();
        await Expect(Span("time12-value")).ToHaveTextAsync("2024-03-20 00:00 +00:00");
    }

    [TestMethod]
    public async Task TimePickerAsOverlay_OpensFromTheClockAndClosesWithItsButtonOrEscape()
    {
        await OpenAsync(Url);

        const string id = "cal-time-overlay";
        await Expect(Page.Locator($"#{id} .bit-cal-twp")).ToHaveCountAsync(0);

        var open = Page.Locator($"#{id} button[aria-label='Open the clock']").First;
        await open.ClickAsync();
        await Expect(Page.Locator($"#{id} .bit-cal-twp")).ToBeVisibleAsync();
        await Expect(Page.Locator($"#{id} .bit-cal-twp")).ToHaveAttributeAsync("data-bit-esc", "claim");

        await Page.Locator($"#{id} button[aria-label='Close the clock']").ClickAsync();
        await Expect(Page.Locator($"#{id} .bit-cal-twp")).ToHaveCountAsync(0);

        await open.ClickAsync();
        await HourInput(id).FocusAsync();
        await Page.Keyboard.PressAsync("Escape");
        await Expect(Page.Locator($"#{id} .bit-cal-twp")).ToHaveCountAsync(0);

        await Expect(Page.Locator("#cal-time .bit-cal-twp")).Not.ToHaveAttributeAsync("data-bit-esc", AnyValue);
    }

    [TestMethod]
    public async Task TimePicker_HourStepAndMinuteStep_KeepEveryTimeOnTheGrid()
    {
        await OpenAsync(Url);

        const string id = "cal-time-step";
        await Day(id, "2024-03-20").ClickAsync();
        await Expect(Span("time-step-value")).ToHaveTextAsync("2024-03-20 00:00 +00:00");

        await SpinAsync(TimeButton(id, 0));
        await Expect(Span("time-step-value")).ToHaveTextAsync("2024-03-20 03:00 +00:00");
        await SpinAsync(TimeButton(id, 2));
        await Expect(Span("time-step-value")).ToHaveTextAsync("2024-03-20 03:15 +00:00");
        await SpinAsync(TimeButton(id, 2));
        await Expect(Span("time-step-value")).ToHaveTextAsync("2024-03-20 03:30 +00:00");

        await HourInput(id).FillAsync("7");
        await HourInput(id).PressAsync("Tab");
        await Expect(Span("time-step-value")).ToHaveTextAsync(new Regex(@"^2024-03-20 (06|09):30 \+00:00$"));
    }

    [TestMethod]
    public async Task TimePicker_HoldingASpinButton_SpinsContinuously()
    {
        await OpenAsync(Url);

        const string id = "cal-time-spin";
        await Day(id, "2024-03-20").ClickAsync();
        await Expect(Span("time-spin-value")).ToHaveTextAsync("2024-03-20 00:00 +00:00");

        await TimeButton(id, 0).DispatchEventAsync("pointerdown");
        await Expect(Span("time-spin-value")).ToHaveTextAsync(new Regex(@"^2024-03-20 (0[3-9]|1\d|2[0-3]):00"));
        await TimeButton(id, 0).DispatchEventAsync("pointerup");
    }

    [TestMethod]
    public async Task Events_MarkTheDayAndOpenADialogListingThem()
    {
        await OpenAsync(Url);

        const string id = "cal-events";
        var day = Day(id, "2024-03-12");
        await Expect(day.Locator(".bit-cal-evi")).ToHaveCountAsync(2);
        await Expect(day).ToHaveAttributeAsync("aria-haspopup", "dialog");
        await Expect(day).ToHaveAttributeAsync("aria-label", new Regex(@"^Tuesday, March 12, 2024, All hands, Standup \(09:00\)$"));
        await Expect(day).ToHaveAttributeAsync("title", new Regex("All hands\nStandup"));
        await Expect(Day(id, "2024-03-13")).Not.ToHaveAttributeAsync("aria-haspopup", AnyValue);

        await day.ClickAsync();

        var dialog = EventDialog(id);
        await Expect(dialog).ToBeVisibleAsync();
        await Expect(dialog).ToHaveAttributeAsync("aria-modal", "true");
        await Expect(dialog).ToHaveAccessibleNameAsync("3/12/2024");
        await Expect(Span("events-value")).ToHaveTextAsync("2024-03-12 00:00 +00:00");

        var items = dialog.Locator(".bit-cal-emi");
        await Expect(items).ToHaveCountAsync(2);
        await Expect(items.Nth(0).Locator(".bit-cal-eit")).ToHaveTextAsync("All hands");
        await Expect(items.Nth(0).Locator(".bit-cal-eib")).ToHaveTextAsync("Whole company");
        await Expect(items.Nth(0).Locator(".bit-cal-eis")).ToHaveCountAsync(0);
        await Expect(items.Nth(1).Locator(".bit-cal-eit")).ToHaveTextAsync("Standup");
        await Expect(items.Nth(1).Locator(".bit-cal-eis")).ToHaveTextAsync("09:00 – 09:15");
        await Expect(dialog.Locator(":focus")).ToHaveCountAsync(1);

        // Escape closes it and hands the keyboard back to the day.
        await Page.Keyboard.PressAsync("Escape");
        await Expect(dialog).ToHaveCountAsync(0);
        await Expect(day).ToBeFocusedAsync();

        await day.ClickAsync();
        await dialog.Locator(".bit-cal-emx").ClickAsync();
        await Expect(dialog).ToHaveCountAsync(0);

        await day.ClickAsync();
        await Page.Locator($"#{id} .bit-cal-eov").ClickAsync(new() { Position = new() { X = 4, Y = 4 } });
        await Expect(dialog).ToHaveCountAsync(0);

        // One-sided times.
        await Day(id, "2024-03-18").ClickAsync();
        await Expect(dialog.Locator(".bit-cal-eis").Nth(0)).ToHaveTextAsync("Until 17:00");
        await Expect(dialog.Locator(".bit-cal-eis").Nth(1)).ToHaveTextAsync("From 12:00");
    }

    [TestMethod]
    public async Task Events_TheDialogKeepsTabInsideIt()
    {
        await OpenAsync(Url);

        await Day("cal-events-tpl", "2024-03-12").ClickAsync();
        var dialog = EventDialog("cal-events-tpl");
        await Expect(dialog).ToBeVisibleAsync();

        for (var i = 0; i < 5; i++)
        {
            await Page.Keyboard.PressAsync("Tab");
            await Expect(dialog.Locator(":focus")).ToHaveCountAsync(1);
        }
    }

    [TestMethod]
    public async Task Events_ColorsCapTheDotsAndShowEventDetailsFalseLeavesTheDayToThePage()
    {
        await OpenAsync(Url);

        var dots = Day("cal-events-colored", "2024-03-12").Locator(".bit-cal-evi");
        await Expect(dots).ToHaveCountAsync(3);
        await Expect(dots.Nth(0)).ToHaveClassAsync(HasClass("bit-cal-evi-err"));
        await Expect(dots.Nth(0)).ToHaveCSSAsync("background-color", await ResolveColorAsync("var(--bit-clr-err)"));
        await Expect(dots.Nth(1)).ToHaveCSSAsync("background-color", await ResolveColorAsync("var(--bit-clr-suc)"));
        await Expect(dots.Nth(2)).ToHaveCSSAsync("background-color", await ResolveColorAsync("var(--bit-clr-pri)"));

        var plain = Day("cal-events-nodetails", "2024-03-12");
        await Expect(plain.Locator(".bit-cal-evi")).ToHaveCountAsync(2);
        await Expect(plain).Not.ToHaveAttributeAsync("aria-haspopup", AnyValue);
        await plain.ClickAsync();
        await Expect(Span("events-of-day")).ToHaveTextAsync("Standup, All hands");
        await Expect(EventDialog("cal-events-nodetails")).ToHaveCountAsync(0);
    }

    [TestMethod]
    public async Task EventTemplateAndTexts_RenderTheRowsOfTheDialog()
    {
        await OpenAsync(Url);

        await Day("cal-events-tpl", "2024-03-12").ClickAsync();
        var dialog = EventDialog("cal-events-tpl");
        await Expect(dialog.Locator(".event-tpl")).ToHaveTextAsync(new[] { "All hands", "Standup" });
        await Expect(dialog.Locator(".bit-cal-emi-dft")).ToHaveCountAsync(0);
        await dialog.Locator(".event-open").Nth(1).ClickAsync();
        await Expect(Span("opened-event")).ToHaveTextAsync("Standup");

        await Day("cal-events-texts", "2024-03-18").ClickAsync();
        var texts = EventDialog("cal-events-texts");
        await Expect(texts.Locator(".bit-cal-eis").Nth(0)).ToHaveTextAsync("Ends 17:00");
        await Expect(texts.Locator(".bit-cal-eis").Nth(1)).ToHaveTextAsync("Starts 12:00");
        await Expect(texts.Locator(".bit-cal-emx")).ToHaveAttributeAsync("aria-label", "Dismiss");
    }

    [TestMethod]
    public async Task Templates_DayMonthYearHeaderAndFooterRenderTheirMarkup()
    {
        await OpenAsync(Url);

        await Expect(Day("cal-day-tpl", "2024-03-20").Locator(".day-cell")).ToHaveTextAsync("20");
        await Expect(Day("cal-day-tpl", "2024-03-17").Locator(".day-cell")).ToHaveClassAsync(HasClass("weekend-cell"));
        await Expect(Day("cal-day-tpl", "2024-03-18").Locator(".day-cell")).Not.ToHaveClassAsync(HasClass("weekend-cell"));

        await Expect(Month("cal-month-year-tpl", 3).Locator(".month-cell")).ToHaveTextAsync("M3");
        await PickerToggle("cal-month-year-tpl").ClickAsync();
        await Expect(Year("cal-month-year-tpl", 2024).Locator(".year-cell")).ToHaveTextAsync("2024AC");

        await Expect(Page.Locator("#cal-header-footer .bit-cal-hdr #calendar-header")).ToHaveTextAsync("Pick a delivery date");
        await Page.Locator("#cal-header-footer .bit-cal-ftr #btn-footer-set").ClickAsync();
        await Expect(Span("footer-value")).ToHaveTextAsync("2024-03-01 00:00 +00:00");
        await Expect(Day("cal-header-footer", "2024-03-01")).ToHaveAttributeAsync("aria-selected", "true");
        await Page.Locator("#btn-footer-clear").ClickAsync();
        await Expect(Span("footer-value")).ToHaveTextAsync("null");
    }

    [TestMethod]
    public async Task Culture_FaIr_ShowsThePersianCalendarRightToLeftWithASaturdayStart()
    {
        // WebAssembly's ICU has the Persian calendar but not its Persian month names, so there the months are
        // spelled with their root names (Esfand, Farvardin); the year, the Saturday start and the layout are the same.
        // The ICU data of .NET's WebAssembly runtime carries no fa locale - neither the sharded set it loads by default nor
        // the full one - so under WebAssembly fa-IR has no Farsi names and reads left to right whatever the component does.

        await OpenAsync(Url);

        const string id = "cal-fa";
        await Expect(Root(id)).ToHaveClassAsync(HasClass("bit-rtl"));
        await Expect(Title(id)).ToHaveTextAsync(IsWasm ? "Esfand 1402" : "اسفند 1402");
        var headers = Page.Locator($"#{id} .bit-cal-dgh [role=columnheader]");
        await Expect(headers.Nth(0)).ToHaveTextAsync("ش");
        await Expect(headers.Nth(0)).ToHaveAttributeAsync("aria-label", "شنبه");
        await Expect(Day(id, "2024-03-15")).ToHaveTextAsync("25");
        await Expect(Day(id, "2024-03-15")).ToHaveAttributeAsync("aria-current", "date");
        await Expect(PickerToggle(id)).ToHaveTextAsync("1402");
        await Expect(Month(id, 1)).ToHaveAttributeAsync("aria-label", IsWasm ? "Farvardin" : "فروردین");
        await Expect(GoToToday(id)).ToHaveAttributeAsync("title", "برو به امروز");

        // Saturday, the first day of the Persian week, is laid out on the right.
        var first = await BoxOfAsync(headers.Nth(0));
        var last = await BoxOfAsync(headers.Nth(6));
        Assert.IsTrue(first.X > last.X, "The week reads from the right.");

        await Day(id, "2024-03-15").FocusAsync();
        await Page.Keyboard.PressAsync("ArrowLeft");
        await Expect(Day(id, "2024-03-16")).ToBeFocusedAsync();

        await Day(id, "2024-03-14").ClickAsync();
        await Expect(Span("fa-value")).ToHaveTextAsync("2024-03-14 00:00 +00:00");
        await Expect(Title(id)).ToHaveTextAsync(IsWasm ? "Esfand 1402" : "اسفند 1402");

        await NextMonth(id).ClickAsync();
        await Expect(Title(id)).ToHaveTextAsync(IsWasm ? "Farvardin 1403" : "فروردین 1403");

        await Expect(Title("cal-en")).ToHaveTextAsync("March 2024");
        await Expect(Page.Locator("#cal-en .bit-cal-dgh [role=columnheader]").Nth(0)).ToHaveAttributeAsync("aria-label", "Sunday");
        await Expect(Root("cal-en")).Not.ToHaveClassAsync(HasClass("bit-rtl"));
    }

    [TestMethod]
    public async Task TimeZone_TheValueIsWrittenInTheZoneOfTheCalendar()
    {
        await OpenAsync(Url);

        await Day("cal-tz", "2024-03-20").ClickAsync();

        await Expect(Span("tz-value")).ToHaveTextAsync("2024-03-20 00:00 +05:00");
        await Expect(Day("cal-tz", "2024-03-20")).ToHaveAttributeAsync("aria-selected", "true");
    }

    [TestMethod]
    public async Task Validation_ARequiredCalendarWithoutAValue_RefusesTheSubmitAndMarksTheGrid()
    {
        await OpenAsync(Url);

        var grid = Page.Locator("#cal-validation .bit-cal-grd").First;
        await Expect(grid).ToHaveAttributeAsync("aria-required", "true");

        await Page.Locator("#btn-submit").ClickAsync();

        await Expect(Span("submit-result")).ToHaveTextAsync("-1");
        await Expect(Page.Locator("#cal-error")).ToHaveTextAsync("Pick a date");
        await Expect(grid).ToHaveAttributeAsync("aria-invalid", "true");
        await Expect(Root("cal-validation")).ToHaveClassAsync(HasClass("bit-inv"));
        await Expect(Page.Locator("#cal-validation .bit-cal-cnt")).ToHaveCSSAsync("outline-color", await ResolveColorAsync("var(--bit-clr-err)"));

        await Expect(Page.Locator("#cal-novalidate .bit-cal-grd").First).Not.ToHaveAttributeAsync("aria-invalid", AnyValue);
        await Expect(Root("cal-novalidate")).Not.ToHaveClassAsync(HasClass("bit-inv"));

        await Day("cal-validation", "2024-03-20").ClickAsync();
        await Expect(grid).Not.ToHaveAttributeAsync("aria-invalid", AnyValue);
        await Expect(Page.Locator("#cal-error")).ToHaveTextAsync(string.Empty);

        await Day("cal-novalidate", "2024-03-20").ClickAsync();
        await Page.Locator("#btn-submit").ClickAsync();
        await Expect(Span("submit-result")).ToHaveTextAsync("1");
    }

    [TestMethod]
    public async Task Keyboard_TheDayGridFollowsTheGridPattern()
    {
        await OpenAsync(Url);

        const string id = "cal-a11y";
        await Expect(Root(id)).ToHaveAttributeAsync("role", "group");
        await Expect(Root(id)).ToHaveAttributeAsync("aria-label", "Departure date");

        // The navigation buttons come first in the tab order, then the one cell of the grid that is in it.
        await NextMonth(id).FocusAsync();
        await Page.Keyboard.PressAsync("Tab");
        await Expect(Day(id, "2024-03-15")).ToBeFocusedAsync();
        await Expect(Day(id, "2024-03-15")).ToHaveCSSAsync("box-shadow", new Regex("^(?!none$).+"));

        await Page.Keyboard.PressAsync("ArrowRight");
        await Expect(Day(id, "2024-03-16")).ToBeFocusedAsync();
        await Page.Keyboard.PressAsync("ArrowDown");
        await Expect(Day(id, "2024-03-23")).ToBeFocusedAsync();
        await Page.Keyboard.PressAsync("ArrowLeft");
        await Expect(Day(id, "2024-03-22")).ToBeFocusedAsync();
        await Page.Keyboard.PressAsync("ArrowUp");
        await Expect(Day(id, "2024-03-15")).ToBeFocusedAsync();
        await Page.Keyboard.PressAsync("Home");
        await Expect(Day(id, "2024-03-10")).ToBeFocusedAsync();
        await Page.Keyboard.PressAsync("End");
        await Expect(Day(id, "2024-03-16")).ToBeFocusedAsync();

        await Page.Keyboard.PressAsync("PageDown");
        await Expect(Day(id, "2024-04-16")).ToBeFocusedAsync();
        await Expect(Title(id)).ToHaveTextAsync("April 2024");
        await Page.Keyboard.PressAsync("PageUp");
        await Expect(Day(id, "2024-03-16")).ToBeFocusedAsync();
        await Page.Keyboard.PressAsync("Shift+PageDown");
        await Expect(Day(id, "2025-03-16")).ToBeFocusedAsync();
        await Expect(Title(id)).ToHaveTextAsync("March 2025");
        await Page.Keyboard.PressAsync("Shift+PageUp");
        await Expect(Day(id, "2024-03-16")).ToBeFocusedAsync();

        await Page.Keyboard.PressAsync("Enter");
        await Expect(Span("a11y-value")).ToHaveTextAsync("2024-03-16 00:00 +00:00");
        await Page.Keyboard.PressAsync("ArrowRight");
        await Expect(Day(id, "2024-03-17")).ToBeFocusedAsync();
        await Page.Keyboard.PressAsync("Space");
        await Expect(Span("a11y-value")).ToHaveTextAsync("2024-03-17 00:00 +00:00");

        // Crossing the end of the month navigates and keeps the focus on the cell.
        await Day(id, "2024-03-31").FocusAsync();
        await Page.Keyboard.PressAsync("ArrowDown");
        await Expect(Day(id, "2024-04-07")).ToBeFocusedAsync();
        await Expect(Title(id)).ToHaveTextAsync("April 2024");

        // The grid is a single tab stop: Tab leaves it for the month picker beside it.
        await Expect(Page.Locator($"#{id} .bit-cal-dbt[tabindex='0']")).ToHaveCountAsync(1);
        await Page.Keyboard.PressAsync("Tab");
        await Expect(Page.Locator($"#{id} .bit-cal-mwp :focus")).ToHaveCountAsync(1);
    }

    [TestMethod]
    public async Task Keyboard_TheMonthAndYearGridsFollowTheGridPattern()
    {
        await OpenAsync(Url);

        const string id = "cal-pickers";
        await Expect(Month(id, 3)).ToHaveAttributeAsync("tabindex", "0");
        await Month(id, 3).FocusAsync();

        await Page.Keyboard.PressAsync("ArrowRight");
        await Expect(Month(id, 4)).ToBeFocusedAsync();
        await Page.Keyboard.PressAsync("ArrowDown");
        await Expect(Month(id, 8)).ToBeFocusedAsync();
        await Page.Keyboard.PressAsync("ArrowUp");
        await Expect(Month(id, 4)).ToBeFocusedAsync();
        await Page.Keyboard.PressAsync("ArrowLeft");
        await Expect(Month(id, 3)).ToBeFocusedAsync();
        await Page.Keyboard.PressAsync("End");
        await Expect(Month(id, 12)).ToBeFocusedAsync();
        await Page.Keyboard.PressAsync("Home");
        await Expect(Month(id, 1)).ToBeFocusedAsync();
        await Page.Keyboard.PressAsync("PageDown");
        await Expect(PickerToggle(id)).ToHaveTextAsync("2025");
        await Page.Keyboard.PressAsync("PageUp");
        await Expect(PickerToggle(id)).ToHaveTextAsync("2024");

        await Month(id, 5).FocusAsync();
        await Page.Keyboard.PressAsync("Enter");
        await Expect(Title(id)).ToHaveTextAsync("May 2024");

        await PickerToggle(id).ClickAsync();
        await Year(id, 2024).FocusAsync();
        await Page.Keyboard.PressAsync("ArrowRight");
        await Expect(Year(id, 2025)).ToBeFocusedAsync();
        await Page.Keyboard.PressAsync("ArrowDown");
        await Expect(Year(id, 2029)).ToBeFocusedAsync();
        await Page.Keyboard.PressAsync("Home");
        await Expect(Year(id, 2023)).ToBeFocusedAsync();
        await Page.Keyboard.PressAsync("End");
        await Expect(Year(id, 2034)).ToBeFocusedAsync();
        await Page.Keyboard.PressAsync("PageDown");
        await Expect(Year(id, 2035)).ToBeVisibleAsync();
        await Page.Keyboard.PressAsync("PageUp");
        await Expect(Year(id, 2024)).ToBeVisibleAsync();

        // Escape in the year grid goes back to the months, even beside the day grid.
        await Year(id, 2024).FocusAsync();
        await Page.Keyboard.PressAsync("Escape");
        await Expect(Month(id, 5)).ToBeVisibleAsync();
    }

    [TestMethod]
    public async Task Accessibility_WeekNumbersTheNavigationAndFocusAsyncAreNamed()
    {
        await OpenAsync(Url);

        await Expect(Page.Locator("#cal-a11y [role=rowheader]").Nth(0)).ToHaveAttributeAsync("aria-label", new Regex(@"^Week number \d+$"));
        await Expect(Page.Locator("#cal-a11y .bit-cal-dgh [role=columnheader]").Nth(0)).ToHaveAttributeAsync("aria-label", "Week");
        await Expect(PrevMonth("cal-a11y")).ToHaveAttributeAsync("aria-label", "Go to previous month");
        await Expect(NextMonth("cal-a11y")).ToHaveAttributeAsync("aria-label", "Go to next month");
        await Expect(GoToToday("cal-a11y")).ToHaveAttributeAsync("aria-label", "Go to today");
        await Expect(Root("cal-basic")).Not.ToHaveAttributeAsync("role", AnyValue);

        await Page.Locator("#btn-focus").ClickAsync();
        await Expect(Day("cal-focus-target", "2024-03-15")).ToBeFocusedAsync();
    }

    [TestMethod]
    public async Task MultipleMonths_ShowConsecutiveMonthsSideBySideAndPickAcrossThem()
    {
        await OpenAsync(Url);

        const string id = "cal-multi";
        await Expect(Root(id)).ToHaveClassAsync(HasClass("bit-cal-mcv"));
        var panes = Page.Locator($"#{id} .bit-cal-dwp");
        await Expect(panes).ToHaveCountAsync(2);
        await Expect(panes.Nth(0).Locator(".bit-cal-pkt")).ToHaveTextAsync("March 2024");
        await Expect(panes.Nth(1).Locator(".bit-cal-pkt")).ToHaveTextAsync("April 2024");
        await Expect(panes.Nth(0).Locator(".bit-cal-nbt")).ToHaveCountAsync(1);
        await Expect(panes.Nth(1).Locator(".bit-cal-nbt")).ToHaveCountAsync(1);
        await Expect(Page.Locator($"#{id} .bit-cal-dbo")).ToHaveCountAsync(0);
        await Expect(panes.Nth(0).Locator(".bit-cal-dgr")).ToHaveCountAsync(6);

        var first = await BoxOfAsync(panes.Nth(0));
        var second = await BoxOfAsync(panes.Nth(1));
        Assert.IsTrue(second.X > first.X, "The months sit side by side.");

        await Day(id, "2024-04-10").ClickAsync();
        await Expect(Span("multi-value")).ToHaveTextAsync("2024-04-10 00:00 +00:00");
        await Expect(panes.Nth(0).Locator(".bit-cal-pkt")).ToHaveTextAsync("March 2024");

        await Day(id, "2024-03-31").FocusAsync();
        await Page.Keyboard.PressAsync("ArrowRight");
        await Expect(Day(id, "2024-04-01")).ToBeFocusedAsync();
        await Expect(panes.Nth(0).Locator(".bit-cal-pkt")).ToHaveTextAsync("March 2024");

        await panes.Nth(1).Locator(".bit-cal-nbt").ClickAsync();
        await Expect(panes.Nth(0).Locator(".bit-cal-pkt")).ToHaveTextAsync("April 2024");

        var narrow = Page.Locator("#cal-multi-narrow .bit-cal-dwp");
        var n1 = await BoxOfAsync(narrow.Nth(0));
        var n2 = await BoxOfAsync(narrow.Nth(1));
        Assert.IsTrue(n2.Y >= n1.Y + n1.Height - 1, "Given too little width the second month wraps under the first.");
    }

    [TestMethod]
    public async Task MultipleMonths_PagedNavigationMovesAWholePage()
    {
        await OpenAsync(Url);

        var panes = Page.Locator("#cal-multi3 .bit-cal-dwp");
        await Expect(panes).ToHaveCountAsync(3);
        await Expect(panes.Nth(2).Locator(".bit-cal-pkt")).ToHaveTextAsync("May 2024");

        await panes.Nth(2).Locator(".bit-cal-nbt").Last.ClickAsync();

        await Expect(Span("multi3-month")).ToHaveTextAsync("2024-06");
        await Expect(panes.Nth(0).Locator(".bit-cal-pkt, .bit-cal-ptb").First).ToHaveTextAsync("June 2024");
        await Expect(panes.Nth(2).Locator(".bit-cal-pkt")).ToHaveTextAsync("August 2024");
    }

    [TestMethod]
    public async Task Cascading_BitParams_FillsWhatTheCalendarLeavesUnset()
    {
        await OpenAsync(Url);

        await Expect(Page.Locator("#cal-cascade-1 [role=rowheader]").First).ToBeVisibleAsync();
        await Expect(Page.Locator("#cal-cascade-1 .bit-cal-dgh [role=columnheader]").Nth(1)).ToHaveAttributeAsync("aria-label", "Monday");
        await Expect(Month("cal-cascade-1", 3)).ToHaveClassAsync(HasClass("bit-cal-pcm"));

        await Expect(Page.Locator("#cal-cascade-own [role=rowheader]")).ToHaveCountAsync(0);
        await Expect(Page.Locator("#cal-cascade-own .bit-cal-mwp")).ToHaveCountAsync(0);
        await Expect(Page.Locator("#cal-cascade-own .bit-cal-dgh [role=columnheader]").Nth(0)).ToHaveAttributeAsync("aria-label", "Monday");

        await Expect(Page.Locator("#cal-cascade-outside [role=rowheader]")).ToHaveCountAsync(0);
        await Expect(Page.Locator("#cal-cascade-outside .bit-cal-dgh [role=columnheader]").Nth(0)).ToHaveAttributeAsync("aria-label", "Sunday");
        await Expect(Month("cal-cascade-outside", 3)).Not.ToHaveClassAsync(HasClass("bit-cal-pcm"));
    }

    [TestMethod]
    public async Task Cascading_Today_OpensTheCalendarOnTheMonthOfTheCascadedToday()
    {
        await OpenAsync(Url);

        await Expect(Title("cal-cascade-today")).ToHaveTextAsync("March 2024");
        await Expect(Page.Locator("#cal-cascade-today-day-2024-03-15")).ToHaveAttributeAsync("aria-current", "date");
    }

    [TestMethod]
    public async Task Color_EachValue_PaintsTodayTheCurrentMonthAndTheAmPmButton()
    {
        await OpenAsync(Url);

        foreach (var (suffix, main, on) in Colors)
        {
            var id = $"cal-color-{suffix}";
            await Expect(Root(id)).ToHaveClassAsync(HasClass($"bit-cal-{suffix}"));

            var mainColor = await ResolveColorAsync($"var(--bit-clr-{main})");
            await Expect(Day(id, "2024-03-15")).ToHaveCSSAsync("background-color", mainColor);
            await Expect(Day(id, "2024-03-15")).ToHaveCSSAsync("color", await ResolveColorAsync($"var(--bit-clr-{on})"));
            await Expect(Page.Locator($"#{id} .bit-cal-bam")).ToHaveCSSAsync("background-color", mainColor);

            // Beside a time picker the month grid takes the place of the day grid, opened from the title.
            await Page.Locator($"#{id} .bit-cal-dwp .bit-cal-ptb").ClickAsync();
            await Expect(Month(id, 3)).ToHaveCSSAsync("background-color", mainColor);
        }

        await Expect(Root("cal-color-default")).Not.ToHaveClassAsync(HasClass("bit-cal-pri"));
        await Expect(Day("cal-color-default", "2024-03-15")).ToHaveCSSAsync("background-color", await ResolveColorAsync("var(--bit-clr-pri)"));
        await Expect(Day("cal-color-wins", "2024-03-15")).ToHaveCSSAsync("background-color", await ResolveColorAsync("var(--bit-clr-err)"));
    }

    [TestMethod]
    public async Task ExternalIcons_EveryIconParameterRendersItsClasses()
    {
        await OpenAsync(Url);

        await Expect(PrevMonth("cal-ext-fa").Locator("i")).ToHaveClassAsync(HasClass("fa-chevron-left"));
        await Expect(NextMonth("cal-ext-fa").Locator("i")).ToHaveClassAsync(HasClass("fa-chevron-right"));
        await Expect(GoToToday("cal-ext-fa").Locator("i")).ToHaveClassAsync(HasClass("fa-calendar-day"));
        await Expect(GoToToday("cal-ext-fa").Locator("i")).Not.ToHaveClassAsync(HasClass("bit-icon--Home"));
        var yearNav = Page.Locator("#cal-ext-fa .bit-cal-mwp .bit-cal-nbt");
        await Expect(yearNav.First.Locator("i")).ToHaveClassAsync(HasClass("bi-arrow-left"));
        await Expect(yearNav.Last.Locator("i")).ToHaveClassAsync(HasClass("bi-arrow-right"));
        await PickerToggle("cal-ext-fa").ClickAsync();
        await Expect(yearNav.First.Locator("i")).ToHaveClassAsync(HasClass("my-prev-range"));
        await Expect(yearNav.Last.Locator("i")).ToHaveClassAsync(HasClass("my-next-range"));

        await Expect(Page.Locator("#cal-ext-bi .bit-cal-gtn i")).ToHaveClassAsync(HasClass("bi-clock"));
        await Expect(TimeButton("cal-ext-bi", 0).Locator("i")).ToHaveClassAsync(HasClass("bi-chevron-up"));
        await Expect(TimeButton("cal-ext-bi", 1).Locator("i")).ToHaveClassAsync(HasClass("bi-chevron-down"));
        await Expect(TimeButton("cal-ext-bi", 2).Locator("i")).ToHaveClassAsync(HasClass("bi-plus"));
        await Expect(TimeButton("cal-ext-bi", 3).Locator("i")).ToHaveClassAsync(HasClass("bi-dash"));

        const string id = "cal-ext-names";
        await Expect(PrevMonth(id).Locator("i")).ToHaveClassAsync(HasClass("bit-icon--ChevronLeft"));
        await Expect(Page.Locator($"#{id} .bit-cal-dwp .bit-cal-nbt").Nth(1).Locator("i")).ToHaveClassAsync(HasClass("bit-icon--ChevronRight"));
        await Expect(GoToToday(id).Locator("i")).ToHaveClassAsync(HasClass("bit-icon--Home"));
        var showTime = Page.Locator($"#{id} button.bit-cal-nbt[aria-label='Show time picker']");
        await Expect(showTime.Locator("i")).ToHaveClassAsync(HasClass("bi-alarm"));
        await showTime.ClickAsync();
        await Expect(Page.Locator($"#{id} .bit-cal-twp .bit-cal-nbt i")).ToHaveClassAsync(HasClass("bit-icon--Calendar"));
        await Expect(Page.Locator($"#{id} .bit-cal-gtn i")).ToHaveClassAsync(HasClass("bit-icon--Timer"));
        await Expect(TimeButton(id, 0).Locator("i")).ToHaveClassAsync(HasClass("bit-icon--Add"));
        await Expect(TimeButton(id, 1).Locator("i")).ToHaveClassAsync(HasClass("bit-icon--Remove"));
        await Expect(TimeButton(id, 2).Locator("i")).ToHaveClassAsync(HasClass("bit-icon--CaretUp"));
        await Expect(TimeButton(id, 3).Locator("i")).ToHaveClassAsync(HasClass("bit-icon--CaretDown"));
    }

    [TestMethod]
    public async Task Size_EachValue_ScalesTheCellsAndTheirText()
    {
        await OpenAsync(Url);

        foreach (var suffix in new[] { "sm", "md", "lg" })
        {
            await Expect(Root($"cal-size-{suffix}")).ToHaveClassAsync(HasClass($"bit-cal-{suffix}"));
        }

        var sm = await BoxOfAsync(Day("cal-size-sm", "2024-03-20"));
        var md = await BoxOfAsync(Day("cal-size-md", "2024-03-20"));
        var lg = await BoxOfAsync(Day("cal-size-lg", "2024-03-20"));
        var dflt = await BoxOfAsync(Day("cal-size-default", "2024-03-20"));
        Assert.IsTrue(sm.Width < md.Width && md.Width < lg.Width, $"Cells {sm.Width} < {md.Width} < {lg.Width}.");
        Assert.AreEqual(md.Width, dflt.Width, 0.5, "Medium is the default.");
        Assert.IsTrue(sm.Width >= 24 && sm.Height >= 24, "The smallest cell still clears the 24px target.");

        var smFont = Px(await StyleOfAsync(Day("cal-size-sm", "2024-03-20"), "font-size"));
        var lgFont = Px(await StyleOfAsync(Day("cal-size-lg", "2024-03-20"), "font-size"));
        Assert.IsTrue(smFont < lgFont);

        var wins = await BoxOfAsync(Day("cal-size-wins", "2024-03-20"));
        Assert.AreEqual(lg.Width, wins.Width, 0.5, "An explicit Size wins over --bit-Calendar-day-size.");
    }

    [TestMethod]
    public async Task StyleAndClass_LandOnTheRoot()
    {
        await OpenAsync(Url);

        await Expect(Root("cal-style")).ToHaveCSSAsync("background-color", "rgb(165, 16, 68)");
        await Expect(Root("cal-style")).ToHaveCSSAsync("border-top-left-radius", "16px");
        await Expect(Root("cal-class")).ToHaveClassAsync(HasClass("custom-class"));
        await Expect(Root("cal-class")).ToHaveClassAsync(HasClass("bit-cal"));
    }

    [TestMethod]
    public async Task Styles_ReachThePartsOfTheCalendar()
    {
        await OpenAsync(Url);
        await Page.Mouse.MoveAsync(0, 0);

        const string id = "cal-styles";
        await Expect(Root(id)).ToHaveCSSAsync("margin-top", "4px");
        await Expect(Page.Locator($"#{id} .bit-cal-cnt")).ToHaveCSSAsync("padding-top", "3px");
        await Expect(Page.Locator($"#{id} .bit-cal-dvd").First).ToHaveCSSAsync("border-top-color", "rgb(60, 179, 113)");
        await Expect(Title(id)).ToHaveCSSAsync("color", "rgb(0, 100, 0)");
        await Expect(Day(id, "2024-03-15")).ToHaveCSSAsync("background-color", "rgb(0, 128, 0)");
        await Expect(Day(id, "2024-03-20")).ToHaveCSSAsync("background-color", "rgb(50, 205, 50)");
        await Expect(Day(id, "2024-03-08")).ToHaveCSSAsync("background-color", "rgb(255, 255, 0)");
        await Expect(Day(id, "2024-03-11")).ToHaveCSSAsync("letter-spacing", "1px");
        await Expect(Page.Locator($"#{id} [role=rowheader]").First).ToHaveCSSAsync("color", "rgb(255, 0, 0)");
        await Expect(TimeButton(id, 0)).ToHaveCSSAsync("background-color", "rgb(50, 205, 50)");
        await Expect(HourInput(id)).ToHaveCSSAsync("color", "rgb(0, 0, 255)");
        await Expect(Page.Locator($"#{id} .bit-cal-hdr")).ToHaveTextAsync("Header");
        await Expect(Page.Locator($"#{id} .bit-cal-ftr")).ToHaveTextAsync("Footer");
    }

    [TestMethod]
    public async Task Classes_ReachThePartsOfTheCalendar()
    {
        await OpenAsync(Url);

        const string id = "cal-classes";
        var expectations = new (string Selector, string Class)[]
        {
            ($"#{id}", "custom-root"),
            ($"#{id} .bit-cal-cnt", "c-cnt"),
            ($"#{id} .bit-cal-dwp", "custom-day-picker"),
            ($"#{id}-day-2024-03-11", "custom-day"),
            ($"#{id}-day-2024-03-15", "custom-today-day"),
            ($"#{id}-day-2024-03-20", "c-selected"),
            ($"#{id}-day-2024-03-08", "c-highlighted"),
            ($"#{id} .bit-cal-dwp .bit-cal-nbt >> nth=0", "custom-prev-month"),
            ($"#{id} .bit-cal-dwp .bit-cal-nbt >> nth=-1", "custom-next-month"),
            ($"#{id} .bit-cal-gtb", "c-gtt"),
            ($"#{id} .bit-cal-dwp .bit-cal-pkt", "custom-day-month"),
            ($"#{id} .bit-cal-dwp .bit-cal-pkh", "custom-day-header"),
            ($"#{id} .bit-cal-dwp .bit-cal-grd", "c-grid"),
            ($"#{id} .bit-cal-dgh", "c-dhr"),
            ($"#{id} .bit-cal-dgh [aria-label=Sunday]", "c-dnh"),
            ($"#{id} .bit-cal-dgr >> nth=0", "c-row"),
            ($"#{id} .bit-cal-dgh [aria-label=Week]", "custom-week-header"),
            ($"#{id} [role=rowheader] >> nth=0", "c-wn"),
            ($"#{id} .bit-cal-mwp", "custom-year-picker"),
            ($"#{id} .bit-cal-mwp .bit-cal-pkh", "c-mph"),
            ($"#{id} .bit-cal-mwp .bit-cal-ptb", "c-ypt"),
            ($"#{id}-month-03", "c-month"),
            ($"#{id} .bit-cal-pkr >> nth=0", "c-months-row"),
            ($"#{id} .bit-cal-mwp .bit-cal-nbt >> nth=0", "c-pyn"),
            ($"#{id} .bit-cal-mwp .bit-cal-nbt >> nth=-1", "c-nyn"),
            ($"#{id} .bit-cal-dvd >> nth=0", "c-divider"),
            ($"#{id} .bit-cal-dwp .bit-cal-nbt >> nth=0 >> i", "c-pmi"),
            ($"#{id} .bit-cal-dwp .bit-cal-nbt >> nth=-1 >> i", "c-nmi"),
            ($"#{id} .bit-cal-gtb i", "c-gti"),
            ("#cal-classes-time .bit-cal-twp", "c-tpw"),
            ("#cal-classes-time .bit-cal-tph", "c-tph"),
            ("#cal-classes-time .bit-cal-tic", "c-tic"),
            ("#cal-classes-time .bit-cal-tpr >> nth=0", "c-hic"),
            ("#cal-classes-time .bit-cal-tpr >> nth=2", "c-mic"),
            ("#cal-classes-time .bit-cal-tdv", "c-sep"),
            ("#cal-classes-time .bit-cal-tin >> nth=0", "c-hour"),
            ("#cal-classes-time .bit-cal-tin >> nth=1", "c-minute"),
            ("#cal-classes-time .bit-cal-bam", "c-am"),
            ("#cal-classes-time .bit-cal-bpm", "c-pm"),
            ("#cal-classes-time .bit-cal-am-pm", "c-ampm"),
            ("#cal-classes-time .bit-cal-gtn", "c-now"),
            ("#cal-classes-time .bit-cal-gtn i", "c-nowi"),
            ("#cal-classes-time .bit-cal-tbt >> nth=0", "c-ihb"),
            ("#cal-classes-time .bit-cal-tbt >> nth=0 >> i", "c-ihi"),
            ("#cal-classes-time .bit-cal-tbt >> nth=1", "c-dhb"),
            ("#cal-classes-time .bit-cal-tbt >> nth=2", "c-imb"),
            ("#cal-classes-time .bit-cal-tbt >> nth=3", "c-dmb"),
            ($"#{id} .bit-cal-hdr", "c-header"),
            ($"#{id} .bit-cal-ftr", "c-footer"),
        };

        foreach (var (selector, cls) in expectations)
        {
            await Expect(Page.Locator(selector).First).ToHaveClassAsync(HasClass(cls), new() { Timeout = 5000 });
        }
    }

    [TestMethod]
    public async Task CssVariables_OnTheInstance_RestyleTheCellsTheSelectionAndToday()
    {
        await OpenAsync(Url);
        await Page.Mouse.MoveAsync(0, 0);

        const string id = "cal-var-selected";
        await Expect(Day(id, "2024-03-20")).ToHaveCSSAsync("background-color", "rgb(0, 120, 212)");
        await Expect(Day(id, "2024-03-20")).ToHaveCSSAsync("color", "rgb(255, 255, 255)");
        Assert.AreEqual("rgb(1, 1, 1)", await StyleOfAsync(Day(id, "2024-03-20"), "border-top-color", "::after"));
        await Expect(Day(id, "2024-03-15")).ToHaveCSSAsync("border-top-left-radius", "2px");
        await Expect(Day(id, "2024-03-15")).ToHaveCSSAsync("background-color", "rgb(10, 20, 30)");
        await Expect(Day(id, "2024-03-15")).ToHaveCSSAsync("color", "rgb(250, 250, 250)");

        const string cells = "cal-var-cells";
        await Expect(Day(cells, "2024-03-20")).ToHaveCSSAsync("width", "44px");
        await Expect(Day(cells, "2024-03-20")).ToHaveCSSAsync("font-size", "14px");
        await Expect(Day(cells, "2024-03-20")).ToHaveCSSAsync("border-top-left-radius", "999px");
        await Expect(Day(cells, "2024-03-20")).ToHaveCSSAsync("color", "rgb(20, 20, 120)");
        await Expect(Day(cells, "2024-02-25")).ToHaveCSSAsync("color", "rgb(0, 150, 0)");
        await Expect(Page.Locator($"#{cells} [role=rowheader]").First).ToHaveCSSAsync("background-color", "rgb(240, 240, 240)");
        await Expect(Page.Locator($"#{cells} [role=rowheader]").First).ToHaveCSSAsync("color", "rgb(200, 0, 0)");
        await Expect(Page.Locator($"#{cells} .bit-cal-dwp")).ToHaveCSSAsync("padding-top", "20px");
    }

    [TestMethod]
    public async Task CssVariables_OnAnAncestor_AreInheritedByTheCalendar()
    {
        await OpenAsync(Url);
        await Page.Mouse.MoveAsync(0, 0);

        const string id = "cal-var-anc";
        await Expect(Root(id)).ToHaveCSSAsync("background-color", "rgb(245, 245, 220)");
        await Expect(Root(id)).ToHaveCSSAsync("border-top-left-radius", "16px");
        var dots = Day(id, "2024-03-12").Locator(".bit-cal-evi");
        await Expect(dots.First).ToHaveCSSAsync("background-color", "rgb(0, 128, 0)");
        await Expect(dots.First).ToHaveCSSAsync("width", "6px");
        await Expect(Day(id, "2024-03-12").Locator(".bit-cal-evc")).ToHaveCSSAsync("column-gap", "3px");
        await Expect(Day(id, "2024-03-08")).ToHaveCSSAsync("background-color", "rgb(255, 228, 196)");
        await Expect(Page.Locator($"#{id} .bit-cal-dvd").First).ToHaveCSSAsync("border-right-color", "rgb(128, 0, 128)");

        await Day(id, "2024-03-20").HoverAsync();
        await Expect(Day(id, "2024-03-20")).ToHaveCSSAsync("background-color", "rgb(200, 200, 255)");

        await Expect(Day("cal-var-disabled", "2024-03-16")).ToHaveCSSAsync("color", "rgb(200, 0, 0)");
    }

    [TestMethod]
    public async Task Rtl_ExplicitOrInherited_MirrorsTheGridAndTheArrowKeys()
    {
        await OpenAsync(Url);

        await Expect(Root("cal-rtl")).ToHaveAttributeAsync("dir", "rtl");
        await Expect(Root("cal-rtl")).ToHaveClassAsync(HasClass("bit-rtl"));

        foreach (var id in new[] { "cal-rtl", "cal-rtl-inherited" })
        {
            var sunday = await BoxOfAsync(Day(id, "2024-03-10"));
            var saturday = await BoxOfAsync(Day(id, "2024-03-16"));
            Assert.IsTrue(sunday.X > saturday.X, $"{id}: the week reads from the right.");

            await Day(id, "2024-03-15").FocusAsync();
            await Page.Keyboard.PressAsync("ArrowLeft");
            await Expect(Day(id, "2024-03-16")).ToBeFocusedAsync();
            await Page.Keyboard.PressAsync("ArrowRight");
            await Expect(Day(id, "2024-03-15")).ToBeFocusedAsync();
        }

        await Expect(Root("cal-rtl-inherited")).Not.ToHaveAttributeAsync("dir", AnyValue);
    }

    [TestMethod]
    public async Task EscapeInADialog_IsTakenByTheCalendarWhileItsOverlayIsOpenAndLeftToTheDialogAfter()
    {
        await OpenAsync(Url);

        await Page.Locator("#btn-open-dialog").ClickAsync();
        await Expect(Root("cal-in-dialog")).ToBeVisibleAsync();
        // The dialog moves the focus into itself once it is open; nothing is pressed before it has.
        await Expect(Page.Locator("[role=dialog]:has(#cal-in-dialog) :focus")).ToHaveCountAsync(1);

        await Page.Locator("#cal-in-dialog .bit-cal-dwp .bit-cal-ptb").ClickAsync();
        await Expect(Page.Locator("#cal-in-dialog .bit-cal-mwp")).ToBeVisibleAsync();
        await Month("cal-in-dialog", 3).FocusAsync();
        await Expect(Month("cal-in-dialog", 3)).ToBeFocusedAsync();

        await Page.Keyboard.PressAsync("Escape");
        await Expect(Page.Locator("#cal-in-dialog .bit-cal-mwp")).ToHaveCountAsync(0);
        await Expect(Span("dialog-open")).ToHaveTextAsync("True");
        await Expect(Root("cal-in-dialog")).ToBeVisibleAsync();

        // The event dialog of the calendar takes its own Escape too.
        await Day("cal-in-dialog", "2024-03-12").ClickAsync();
        await Expect(EventDialog("cal-in-dialog")).ToBeVisibleAsync();
        await Expect(EventDialog("cal-in-dialog").Locator(":focus")).ToHaveCountAsync(1);
        await Page.Keyboard.PressAsync("Escape");
        await Expect(EventDialog("cal-in-dialog")).ToHaveCountAsync(0);
        await Expect(Span("dialog-open")).ToHaveTextAsync("True");

        await Expect(Day("cal-in-dialog", "2024-03-12")).ToBeFocusedAsync();
        await Page.Keyboard.PressAsync("Escape");
        await Expect(Span("dialog-open")).ToHaveTextAsync("False");
    }

    [TestMethod]
    public async Task Base_VisibilityAndHtmlAttributes_AreAppliedToTheRoot()
    {
        await OpenAsync(Url);

        await Expect(Root("cal-hidden")).ToHaveCSSAsync("visibility", "hidden");
        await Expect(Root("cal-collapsed")).ToHaveCSSAsync("display", "none");
        await Expect(Root("cal-html-attrs")).ToHaveAttributeAsync("data-custom", "abc");
    }

    [TestMethod]
    public async Task Disabled_DropsThePageClickHandlersUntilEnabledAgain()
    {
        await OpenAsync(Url);

        await Day("cal-handler-disabled", "2024-03-20").ClickAsync(new() { Force = true });
        await Root("cal-handler-disabled").DispatchEventAsync("click");

        await Day("cal-handler-enabled", "2024-03-20").ClickAsync();
        await Expect(Span("enabled-root-clicks")).ToHaveTextAsync("1");
        await Expect(Span("disabled-root-clicks")).ToHaveTextAsync("0");

        await Page.Locator("#btn-toggle-handler-disabled").ClickAsync();
        await Expect(Root("cal-handler-disabled")).Not.ToHaveClassAsync(HasClass("bit-dis"));
        await Day("cal-handler-disabled", "2024-03-20").ClickAsync();
        await Expect(Span("disabled-root-clicks")).ToHaveTextAsync("1");
    }

    [TestMethod]
    public async Task Runtime_ParameterChanges_ReRenderTheCalendar()
    {
        await OpenAsync(Url);

        await Page.Locator("#btn-runtime-color").ClickAsync();
        await Expect(Root("cal-runtime")).ToHaveClassAsync(HasClass("bit-cal-suc"));
        await Expect(Day("cal-runtime", "2024-03-15")).ToHaveCSSAsync("background-color", await ResolveColorAsync("var(--bit-clr-suc)"));

        await Page.Locator("#btn-runtime-months").ClickAsync();
        await Expect(Page.Locator("#cal-runtime .bit-cal-dwp")).ToHaveCountAsync(2);

        await Page.Locator("#btn-runtime-culture").ClickAsync();
        await Expect(Title("cal-runtime")).ToHaveTextAsync(new Regex(" 1402$"));

        // The ICU data of .NET's WebAssembly runtime carries no fa locale, so there the Persian calendar comes with the
        // names of the root locale and a left-to-right layout.
        if (IsWasm is false)
        {
            await Expect(Title("cal-runtime")).ToHaveTextAsync(IsWasm ? "Esfand 1402" : "اسفند 1402");
            await Expect(Root("cal-runtime")).ToHaveClassAsync(HasClass("bit-rtl"));
        }

        await Page.Locator("#btn-runtime-disable").ClickAsync();
        await Expect(Root("cal-runtime")).ToHaveClassAsync(HasClass("bit-dis"));
        await Expect(Day("cal-runtime", "2024-03-20")).ToBeDisabledAsync();

        await Page.Locator("#btn-runtime-disable").ClickAsync();
        await Day("cal-runtime", "2024-03-20").ClickAsync();
        await Expect(Span("runtime-value")).ToHaveTextAsync("2024-03-20 00:00 +00:00");
    }

    [TestMethod]
    public async Task Unmount_RemovesTheCalendarWithoutAnError()
    {
        await OpenAsync(Url);

        await Day("cal-runtime", "2024-03-21").ClickAsync();
        await Expect(Span("runtime-value")).ToHaveTextAsync("2024-03-21 00:00 +00:00");

        await Page.Locator("#btn-runtime-unmount").ClickAsync();
        await Expect(Root("cal-runtime")).ToHaveCountAsync(0);

        await Page.Locator("#btn-runtime-unmount").ClickAsync();
        await Expect(Day("cal-runtime", "2024-03-21")).ToHaveAttributeAsync("aria-selected", "true");

        Assert.IsFalse(await IsCircuitDeadAsync());
        await AssertNoServerErrorsAsync();
    }

    [TestMethod]
    public async Task ReducedMotion_CollapsesTheEventOverlayAnimationUnlessForceAnimation()
    {
        await OpenAsync(Url);

        await Day("cal-motion", "2024-03-12").ClickAsync();
        StringAssert.StartsWith(await StyleOfAsync(Page.Locator("#cal-motion .bit-cal-eov"), "animation-duration"), "0.1s");
        await Page.Keyboard.PressAsync("Escape");
        await Expect(Page.Locator("#cal-motion .bit-cal-eov")).ToHaveCountAsync(0);

        await Page.EmulateMediaAsync(new() { ReducedMotion = ReducedMotion.Reduce });

        await Day("cal-motion", "2024-03-12").ClickAsync();
        var reduced = await StyleOfAsync(Page.Locator("#cal-motion .bit-cal-eov"), "animation-duration");
        Assert.IsFalse(reduced.StartsWith("0.1s"), $"Reduced motion collapses the overlay animation ({reduced}).");
        await Page.Keyboard.PressAsync("Escape");

        await Expect(Root("cal-motion-forced")).ToHaveClassAsync(HasClass("bit-fam"));
        await Day("cal-motion-forced", "2024-03-12").ClickAsync();
        StringAssert.StartsWith(await StyleOfAsync(Page.Locator("#cal-motion-forced .bit-cal-eov"), "animation-duration"), "0.1s");
    }
}
