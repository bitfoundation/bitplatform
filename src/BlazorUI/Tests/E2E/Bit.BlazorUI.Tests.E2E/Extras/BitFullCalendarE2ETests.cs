using System.Globalization;
using System.Text.RegularExpressions;

namespace Bit.BlazorUI.Tests.E2E.Extras;

/// <summary>
/// End-to-end tests of BitFullCalendar in a real browser: every view (day, week, month, year, agenda) and both modes
/// (events and the resource timeline), the toolbar navigation and the mini calendar, creating events by a click, a key or
/// a range dragged over empty slots, the built-in dialogs (add, details, edit, delete, recurrence and the scope prompt),
/// moving events by dragging them and resizing them with the real mouse, the permissions and read-only paths, the
/// MinDate/MaxDate window, the filters and the settings panel, keyboard navigation of every grid, the current-time line,
/// the cultures and directions (en-US, fa-IR, ar-SA, an explicit and an inherited right-to-left), the templates, the
/// theme and the public CSS variables, the BitParams cascade, the public methods and the two-way bound Date/View/Mode.
/// Every page pins "today" through the TimeProvider parameter to Wednesday 2025-07-16 10:30, so the tests read the same
/// weeks and months whatever day they run on.
/// </summary>
[TestClass]
public partial class BitFullCalendarE2ETests : E2ETestBase
{
    private const string MainPage = "/e2e/full-calendar";

    private ILocator Cal => Page.Locator("#cal");

    private ILocator Title(ILocator calendar) => calendar.Locator(".bit-bfc-header-title");

    private ILocator Dialog(ILocator calendar) => calendar.Locator(".bit-bfc-dialog[role='dialog']");

    private ILocator Badge(ILocator calendar, string id) => calendar.Locator($"[data-bit-bfc-event='{id}'][role='button']");

    private ILocator Block(ILocator calendar, string id) => calendar.Locator($".bit-bfc-event-block[data-bit-bfc-event='{id}']");

    private ILocator MonthAdd(ILocator calendar, string ariaDate) => calendar.Locator($".bit-bfc-cell-add-hint-month[aria-label^='Add event, {ariaDate}']");

    private ILocator MonthCell(ILocator calendar, string ariaDate) => calendar.Locator($".bit-bfc-month-cell:has(.bit-bfc-cell-add-hint-month[aria-label^='Add event, {ariaDate}'])");

    private ILocator Slot(ILocator calendar, string label) => calendar.Locator($"[role='button'][aria-label='Add event, {label}']");

    private async Task SwitchViewAsync(ILocator calendar, string view)
    {
        await calendar.Locator(".bit-bfc-view-tab", new() { HasTextRegex = new Regex($"^\\s*{view}\\s*$") }).ClickAsync();
        await Expect(calendar.Locator(".bit-bfc-view-tab.active")).ToHaveTextAsync(view);
    }

    /// <summary>
    /// Presses Escape the way a keyboard user does: once the focus has moved into the topmost popup the calendar has open
    /// (a dialog, the settings panel, a date picker) - a key pressed before it got there goes to whatever held it.
    /// </summary>
    private async Task PressEscapeAsync()
    {
        await Page.WaitForFunctionAsync("""
            () => {
                const open = [...document.querySelectorAll('.bit-bfc-dialog[role="dialog"], .bit-bfc-dropdown-menu, .bit-bfc-dtp-panel')];
                return open.length === 0 || open[open.length - 1].contains(document.activeElement);
            }
            """);
        await Page.Keyboard.PressAsync("Escape");
    }

    private async Task OpenMainAsync(string? view = null)
    {
        await OpenAsync(MainPage);
        if (view is not null)
        {
            await SwitchViewAsync(Cal, view);
            await Expect(Page.Locator("#view")).ToHaveTextAsync(view);
        }
    }

    // ---------------------------------------------------------------- lifecycle

    [TestMethod]
    public async Task Prerender_TheServerHtml_AlreadyHoldsTheCalendarOnThePinnedMonth()
    {
        var response = await Page.APIRequest.GetAsync($"{BaseUrl}{MainPage}");
        var html = await response.TextAsync();

        Assert.IsTrue(response.Ok);
        StringAssert.Contains(html, "class=\"bit-bfc ");
        StringAssert.Contains(html, "July 2025");
        StringAssert.Contains(html, "Team Standup");
        StringAssert.Contains(html, "Loading", "The prerendered HTML is the one sent before the circuit connects.");

        await OpenMainAsync();
        await Expect(Title(Cal)).ToHaveTextAsync("July 2025");
    }

    [TestMethod]
    public async Task Basic_OpensOnTheMonthOfThePinnedToday_AsANamedRegion()
    {
        await OpenMainAsync();

        await Expect(Cal).ToHaveAttributeAsync("role", "region");
        await Expect(Cal).ToHaveAttributeAsync("aria-label", "Team schedule");
        await Expect(Cal).ToHaveAttributeAsync("dir", "ltr");
        await Expect(Cal).ToHaveClassAsync(HasClass("bit-bfc"));
        await Expect(Title(Cal)).ToHaveTextAsync("July 2025");
        await Expect(Cal.Locator(".bit-bfc-month-cell-day.today")).ToHaveTextAsync("16");
        await Expect(Cal.Locator(".bit-bfc-month-cell:not(.bit-bfc-month-cell-blank)")).ToHaveCountAsync(35);
        await Expect(Cal.Locator(".bit-bfc-month-header-cell")).ToHaveTextAsync(["S", "M", "T", "W", "T", "F", "S"]);

        var body = Page.Locator("#cal-body");
        await Expect(body).ToHaveAttributeAsync("role", "tabpanel");
        await Expect(body).ToHaveAttributeAsync("aria-labelledby", "cal-mode-0 cal-view-2");

        // the events of the month are drawn on their days, each a named button
        await Expect(Badge(Cal, "e1")).ToHaveAttributeAsync("aria-label", "Team Standup, 7/16/2025 09:00 - 7/16/2025 09:45");
        await Expect(MonthCell(Cal, "Wednesday, July 16, 2025").Locator(".bit-bfc-event-badge-title")).ToHaveTextAsync(["Team Standup", "Focus Time", "Design Review"]);

        // the first range is reported once, on the first render
        await Expect(Page.Locator("#date-change-count")).ToHaveTextAsync("1");
        await Expect(Page.Locator("#last-range")).ToHaveTextAsync(new Regex(@"\|Month$"));
    }

    // ---------------------------------------------------------------- views

    [TestMethod]
    [DataRow("Day", ".bit-bfc-day-layout", "Jul 16, 2025", "2025-07-16|2025-07-16|Day")]
    [DataRow("Week", ".bit-bfc-week-layout", "Jul 13, 2025 - Jul 19, 2025", "2025-07-13|2025-07-19|Week")]
    [DataRow("Year", ".bit-bfc-year", "2025", "2025-01-01|2025-12-31|Year")]
    [DataRow("Agenda", ".bit-bfc-agenda", "July 2025", "2025-07-01|2025-07-31|Agenda")]
    public async Task Views_ClickingAViewTab_RendersThatViewAndReportsIt(string view, string layout, string title, string range)
    {
        await OpenMainAsync();

        await SwitchViewAsync(Cal, view);

        await Expect(Cal.Locator(layout)).ToBeVisibleAsync();
        await Expect(Cal.Locator(".bit-bfc-month")).ToHaveCountAsync(0);
        await Expect(Title(Cal)).ToHaveTextAsync(title);
        await Expect(Page.Locator("#view")).ToHaveTextAsync(view);
        await Expect(Page.Locator("#view-change-count")).ToHaveTextAsync("1");
        await Expect(Page.Locator("#last-view-change")).ToHaveTextAsync(view);
        await Expect(Page.Locator("#last-range")).ToHaveTextAsync(range);

        var tab = Cal.Locator(".bit-bfc-view-tab.active");
        await Expect(tab).ToHaveAttributeAsync("aria-selected", "true");
        await Expect(tab).ToHaveAttributeAsync("tabindex", "0");
        await Expect(Cal.Locator(".bit-bfc-view-tab[aria-selected='false']")).ToHaveCountAsync(4);
    }

    [TestMethod]
    public async Task Views_TheMonthViewRange_IsTheMonthItself()
    {
        await OpenMainAsync("Week");

        await SwitchViewAsync(Cal, "Month");

        await Expect(Cal.Locator(".bit-bfc-month")).ToBeVisibleAsync();
        await Expect(Page.Locator("#last-range")).ToHaveTextAsync(new Regex(@"^2025-0[67]-\d\d\|2025-0[78]-\d\d\|Month$"));
    }

    [TestMethod]
    public async Task Views_ThePageDrivesTheBoundView_WithoutRaisingOnViewChange()
    {
        await OpenMainAsync();

        await Page.Locator("#btn-view-week").ClickAsync();
        await Expect(Cal.Locator(".bit-bfc-week-layout")).ToBeVisibleAsync();
        await Expect(Cal.Locator(".bit-bfc-view-tab.active")).ToHaveTextAsync("Week");

        await Page.Locator("#btn-view-day").ClickAsync();
        await Expect(Cal.Locator(".bit-bfc-day-layout")).ToBeVisibleAsync();

        await Page.Locator("#btn-view-agenda").ClickAsync();
        await Expect(Cal.Locator(".bit-bfc-agenda")).ToBeVisibleAsync();

        await Page.Locator("#btn-view-year").ClickAsync();
        await Expect(Cal.Locator(".bit-bfc-year")).ToBeVisibleAsync();

        await Page.Locator("#btn-view-month").ClickAsync();
        await Expect(Cal.Locator(".bit-bfc-month")).ToBeVisibleAsync();

        // parameter-driven changes are echoes, not user changes
        await Expect(Page.Locator("#view-change-count")).ToHaveTextAsync("0");
        // but every move of the range is still reported
        await Expect(Page.Locator("#date-change-count")).Not.ToHaveTextAsync("1");
    }

    // ---------------------------------------------------------------- navigation

    [TestMethod]
    [DataRow("Month", "August 2025", "June 2025", "2025-08-16", "2025-06-16")]
    [DataRow("Week", "Jul 20, 2025 - Jul 26, 2025", "Jul 6, 2025 - Jul 12, 2025", "2025-07-23", "2025-07-09")]
    [DataRow("Day", "Jul 17, 2025", "Jul 15, 2025", "2025-07-17", "2025-07-15")]
    [DataRow("Year", "2026", "2024", "2026-07-16", "2024-07-16")]
    [DataRow("Agenda", "August 2025", "June 2025", "2025-08-16", "2025-06-16")]
    public async Task Navigation_NextPreviousAndToday_StepTheViewByItsPeriod(string view, string next, string previous, string nextDate, string previousDate)
    {
        await OpenMainAsync(view);
        var nextButton = Cal.Locator(".bit-bfc-btn-nav[aria-label='Next']");
        var previousButton = Cal.Locator(".bit-bfc-btn-nav[aria-label='Previous']");

        await nextButton.ClickAsync();
        await Expect(Title(Cal)).ToHaveTextAsync(next);
        await Expect(Page.Locator("#date")).ToHaveTextAsync(nextDate);

        await previousButton.ClickAsync();
        await previousButton.ClickAsync();
        await Expect(Title(Cal)).ToHaveTextAsync(previous);
        await Expect(Page.Locator("#date")).ToHaveTextAsync(previousDate);

        await Cal.Locator(".bit-bfc-header-left > .bit-bfc-btn", new() { HasText = "Today" }).ClickAsync();
        await Expect(Page.Locator("#date")).ToHaveTextAsync("2025-07-16");
        await Expect(Title(Cal)).Not.ToHaveTextAsync(previous);

        await Expect(nextButton).Not.ToHaveAttributeAsync("aria-disabled", "true");
        await Expect(previousButton).Not.ToHaveAttributeAsync("aria-disabled", "true");
    }

    [TestMethod]
    public async Task Navigation_EveryMoveOfTheRange_IsReportedWithItsBounds()
    {
        await OpenMainAsync("Week");
        await Expect(Page.Locator("#last-range")).ToHaveTextAsync("2025-07-13|2025-07-19|Week");
        var before = int.Parse(await Page.Locator("#date-change-count").InnerTextAsync(), CultureInfo.InvariantCulture);

        await Cal.Locator(".bit-bfc-btn-nav[aria-label='Next']").ClickAsync();

        await Expect(Page.Locator("#last-range")).ToHaveTextAsync("2025-07-20|2025-07-26|Week");
        await Expect(Page.Locator("#date-change-count")).ToHaveTextAsync((before + 1).ToString(CultureInfo.InvariantCulture));

        // pressing Today while today's range is back does not report it twice
        await Cal.Locator(".bit-bfc-header-left > .bit-bfc-btn", new() { HasText = "Today" }).ClickAsync();
        await Expect(Page.Locator("#last-range")).ToHaveTextAsync("2025-07-13|2025-07-19|Week");
        await Expect(Page.Locator("#date-change-count")).ToHaveTextAsync((before + 2).ToString(CultureInfo.InvariantCulture));
    }

    [TestMethod]
    public async Task Binding_ThePageDrivesTheBoundDate_AndTheCalendarFollows()
    {
        await OpenMainAsync("Week");

        await Page.Locator("#btn-date-set").ClickAsync();
        await Expect(Title(Cal)).ToHaveTextAsync("Aug 3, 2025 - Aug 9, 2025");
        await Expect(Page.Locator("#date")).ToHaveTextAsync("2025-08-05");

        await Page.Locator("#btn-date-next").ClickAsync();
        await Expect(Page.Locator("#date")).ToHaveTextAsync("2025-08-06");
        await Expect(Title(Cal)).ToHaveTextAsync("Aug 3, 2025 - Aug 9, 2025");

        // the user's navigation writes the bound value back
        await Cal.Locator(".bit-bfc-btn-nav[aria-label='Next']").ClickAsync();
        await Expect(Page.Locator("#date")).ToHaveTextAsync("2025-08-13");
    }

    // ---------------------------------------------------------------- modes

    [TestMethod]
    public async Task Modes_TheTimelineTab_ShowsTheResourceRowsAndReportsTheMode()
    {
        await OpenMainAsync();

        await Cal.Locator(".bit-bfc-mode-tab", new() { HasText = "Timeline" }).ClickAsync();

        await Expect(Cal.Locator(".bit-bfc-tl-layout")).ToBeVisibleAsync();
        await Expect(Page.Locator("#mode")).ToHaveTextAsync("Timeline");
        await Expect(Page.Locator("#mode-change-count")).ToHaveTextAsync("1");
        await Expect(Page.Locator("#last-mode-change")).ToHaveTextAsync("Timeline");
        await Expect(Cal.Locator(".bit-bfc-tl-resource-title")).ToHaveTextAsync(["Room A", "Room B", "Unassigned"]);
        await Expect(Cal.Locator(".bit-bfc-tl-corner")).ToHaveTextAsync("Resource");
        // the timeline lays out the day, the week and the month only
        await Expect(Cal.Locator(".bit-bfc-view-tab")).ToHaveTextAsync(["Day", "Week", "Month"]);
        await Expect(Page.Locator("#cal-body")).ToHaveAttributeAsync("aria-labelledby", "cal-mode-1 cal-view-2");

        await Cal.Locator(".bit-bfc-mode-tab", new() { HasText = "Events" }).ClickAsync();
        await Expect(Cal.Locator(".bit-bfc-month")).ToBeVisibleAsync();
        await Expect(Page.Locator("#mode")).ToHaveTextAsync("Event");
        await Expect(Page.Locator("#mode-change-count")).ToHaveTextAsync("2");
    }

    [TestMethod]
    public async Task Modes_AYearOrAgendaViewEnteringTheTimeline_FallsBackToTheWeekLayout()
    {
        await OpenMainAsync("Year");

        await Page.Locator("#btn-mode-timeline").ClickAsync();

        await Expect(Cal.Locator(".bit-bfc-tl-layout")).ToBeVisibleAsync();
        await Expect(Cal.Locator(".bit-bfc-view-tab.active")).ToHaveTextAsync("Week");
        await Expect(Page.Locator("#view")).ToHaveTextAsync("Week");
        await Expect(Page.Locator("#mode")).ToHaveTextAsync("Timeline");
        await Expect(Title(Cal)).ToHaveTextAsync("Jul 13, 2025 - Jul 19, 2025");
    }

    [TestMethod]
    [DataRow("Day", "Jul 16, 2025")]
    [DataRow("Week", "Jul 13, 2025 - Jul 19, 2025")]
    [DataRow("Month", "July 2025")]
    public async Task Modes_EveryTimelineLayout_PlacesTheEventsOnTheirResourceRows(string view, string title)
    {
        await OpenMainAsync();
        await Page.Locator("#btn-mode-timeline").ClickAsync();
        await SwitchViewAsync(Cal, view);

        await Expect(Title(Cal)).ToHaveTextAsync(title);
        var rows = Cal.Locator(".bit-bfc-tl-body-row");
        await Expect(rows).ToHaveCountAsync(3);
        await Expect(rows.Nth(0).Locator("[data-bit-bfc-event='e1']").First).ToBeVisibleAsync();
        await Expect(rows.Nth(1).Locator("[data-bit-bfc-event='e3']").First).ToBeVisibleAsync();
        await Expect(rows.Nth(2).Locator("[data-bit-bfc-event='e2']").First).ToBeVisibleAsync();
    }
}
