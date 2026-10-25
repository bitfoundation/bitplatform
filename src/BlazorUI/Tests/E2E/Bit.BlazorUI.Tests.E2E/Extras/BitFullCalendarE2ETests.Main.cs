using System.Globalization;
using System.Text.RegularExpressions;

namespace Bit.BlazorUI.Tests.E2E.Extras;

/// <summary>
/// The rest of the main calendar: the year, agenda and day views in detail, the mini calendar and the "Happening now"
/// panel, the current-time line, the "+N more" list, the filters, the settings panel, the read-only, disabled and
/// permission paths, the public methods, and the Events and Settings parameters changing at runtime.
/// </summary>
public partial class BitFullCalendarE2ETests
{
    private ILocator SettingsButton(ILocator calendar) => calendar.Locator("button[aria-label='Settings']");

    private ILocator SettingsPanel(ILocator calendar) => calendar.Locator(".bit-bfc-dropdown-menu[role='dialog']");

    private async Task<ILocator> OpenSettingsAsync(ILocator calendar)
    {
        await SettingsButton(calendar).ClickAsync();
        var panel = SettingsPanel(calendar);
        await Expect(panel).ToBeVisibleAsync();
        return panel;
    }

    // ---------------------------------------------------------------- year view

    [TestMethod]
    public async Task Year_AMonthTitle_OpensThatMonth_AndABusyDay_ListsItsEvents()
    {
        await OpenMainAsync("Year");
        await Expect(Cal.Locator(".bit-bfc-year-month")).ToHaveCountAsync(12);
        await Expect(Cal.Locator(".bit-bfc-year-day.has-events")).ToHaveCountAsync(7);
        await Expect(Cal.Locator(".bit-bfc-year-day.has-events[aria-current='date']")).ToHaveAttributeAsync("aria-label", "Wednesday, July 16, 2025, 3 event(s)");
        await Expect(Cal.Locator(".bit-bfc-year-day.has-events[aria-current='date'] .bit-bfc-bullet")).ToHaveCountAsync(3);

        await Cal.Locator(".bit-bfc-year-day.has-events[aria-current='date']").ClickAsync();
        var list = Dialog(Cal);
        await Expect(list.Locator("h3")).ToHaveTextAsync("Events on Wednesday, July 16, 2025");
        await Expect(list.Locator(".bit-bfc-agenda-title")).ToHaveTextAsync(["Team Standup", "Focus Time", "Design Review"]);
        await list.Locator(".bit-bfc-agenda-item", new() { HasText = "Focus Time" }).ClickAsync();
        await Expect(Cal.Locator(".bit-bfc-dialog[role='dialog'] h3", new() { HasText = "Focus Time" })).ToBeVisibleAsync();
        await PressEscapeAsync();
        await Expect(Cal.Locator(".bit-bfc-dialog[role='dialog'] h3", new() { HasText = "Focus Time" })).ToHaveCountAsync(0);
        await list.GetByRole(AriaRole.Button, new() { Name = "Close" }).ClickAsync();
        await Expect(Dialog(Cal)).ToHaveCountAsync(0);

        await Cal.Locator(".bit-bfc-year-month-title", new() { HasText = "August" }).ClickAsync();
        await Expect(Cal.Locator(".bit-bfc-month")).ToBeVisibleAsync();
        await Expect(Title(Cal)).ToHaveTextAsync("August 2025");
        await Expect(Page.Locator("#view")).ToHaveTextAsync("Month");
        await Expect(Page.Locator("#last-view-change")).ToHaveTextAsync("Month");
    }

    // ---------------------------------------------------------------- agenda view

    [TestMethod]
    public async Task Agenda_GroupsTheMonthByDate_AndTheSearchNarrowsIt()
    {
        await OpenMainAsync("Agenda");
        var groups = Cal.Locator(".bit-bfc-agenda-group-title");
        await Expect(groups).ToHaveTextAsync(["Monday, Jul 14, 2025", "Wednesday, Jul 16, 2025", "Thursday, Jul 17, 2025", "Monday, Jul 21, 2025", "Wednesday, Jul 23, 2025"]);
        await Expect(Cal.Locator(".bit-bfc-agenda-item[data-bit-bfc-event='e1'] .bit-bfc-agenda-avatar")).ToHaveTextAsync("AS");
        await Expect(Cal.Locator(".bit-bfc-agenda-item[data-bit-bfc-event='e1'] .bit-bfc-agenda-time")).ToHaveTextAsync("09:00 - 09:45");
        await Expect(Cal.Locator(".bit-bfc-agenda-item[data-bit-bfc-event='e5'] .bit-bfc-agenda-time")).ToHaveTextAsync("All day");

        var search = Cal.GetByPlaceholder("Search events...");
        await search.FillAsync("review");
        await Expect(Cal.Locator(".bit-bfc-agenda-title")).ToHaveTextAsync(["Quarterly Review", "Design Review"]);
        await Expect(Cal.Locator(".bit-bfc-agenda [role='status']")).ToHaveTextAsync("2 event(s)");

        // attendees are searched too
        await search.FillAsync("carol");
        await Expect(Cal.Locator(".bit-bfc-agenda-title")).ToHaveTextAsync(["Client Call"]);

        await search.FillAsync("nothing like it");
        await Expect(Cal.Locator(".bit-bfc-agenda-empty")).ToHaveTextAsync("No events found.");
        await Expect(Cal.Locator(".bit-bfc-agenda [role='status']")).ToHaveTextAsync("0 event(s)");

        await search.FillAsync("");
        await Cal.Locator(".bit-bfc-agenda-item[data-bit-bfc-event='e6']").ClickAsync();
        await Expect(Dialog(Cal).Locator("h3")).ToHaveTextAsync("Quarterly Review");
    }

    // ---------------------------------------------------------------- day view

    [TestMethod]
    public async Task Day_ShowsTheDayWithItsSidebar_HappeningNowAndTheMiniCalendar()
    {
        await OpenMainAsync("Day");

        await Expect(Cal.Locator(".bit-bfc-day-header-weekday")).ToHaveTextAsync("Wed");
        await Expect(Cal.Locator(".bit-bfc-day-header-date")).ToHaveTextAsync("16");
        await Expect(Cal.Locator(".bit-bfc-day-header-date")).ToHaveAttributeAsync("aria-current", "date");
        await Expect(Cal.Locator(".bit-bfc-event-block")).ToHaveCountAsync(3);

        // "now" is 10:30, inside Focus Time only
        await Expect(Cal.Locator(".bit-bfc-happening-now-title")).ToHaveTextAsync("Happening now");
        await Expect(Cal.Locator(".bit-bfc-happening-event-title")).ToHaveTextAsync(["Focus Time"]);
        await Cal.Locator(".bit-bfc-happening-event").ClickAsync();
        await Expect(Dialog(Cal).Locator("h3")).ToHaveTextAsync("Focus Time");
        await PressEscapeAsync();

        var mini = Cal.Locator(".bit-bfc-mini-calendar");
        await Expect(mini.Locator(".bit-bfc-mini-calendar-title")).ToHaveTextAsync("July 2025");
        await Expect(mini.Locator(".bit-bfc-mini-calendar-day.today")).ToHaveAttributeAsync("aria-current", "date");
        await mini.GetByRole(AriaRole.Button, new() { Name = "Next month" }).ClickAsync();
        await Expect(mini.Locator(".bit-bfc-mini-calendar-title")).ToHaveTextAsync("August 2025");
        await mini.GetByRole(AriaRole.Button, new() { Name = "Previous month" }).ClickAsync();
        await mini.GetByRole(AriaRole.Button, new() { Name = "Previous month" }).ClickAsync();
        await Expect(mini.Locator(".bit-bfc-mini-calendar-title")).ToHaveTextAsync("June 2025");
        await mini.GetByRole(AriaRole.Button, new() { Name = "Next month" }).ClickAsync();

        await mini.GetByRole(AriaRole.Button, new() { Name = "Monday, July 21, 2025" }).ClickAsync();
        await Expect(Title(Cal)).ToHaveTextAsync("Jul 21, 2025");
        await Expect(Page.Locator("#date")).ToHaveTextAsync("2025-07-21");
        await Expect(mini.Locator(".bit-bfc-mini-calendar-day.selected")).ToHaveAttributeAsync("aria-label", "Monday, July 21, 2025, selected");
        // the all-day event of the day sits above the grid, and nothing is happening at 10:30 of a day that is not today
        await Expect(Cal.Locator(".bit-bfc-multiday-row [data-bit-bfc-event='e5']")).ToBeVisibleAsync();
        await Expect(Cal.Locator(".bit-bfc-happening-event")).ToHaveCountAsync(1);
        await Expect(Cal.Locator(".bit-bfc-timeline")).ToHaveCountAsync(0);
    }

    // ---------------------------------------------------------------- the current-time line

    [TestMethod]
    [DataRow("Week")]
    [DataRow("Day")]
    public async Task NowLine_SitsAtThePinnedTimeOfTodayOnly(string view)
    {
        await OpenMainAsync(view);

        var line = Cal.Locator(".bit-bfc-timeline");
        await Expect(line).ToHaveCountAsync(1);
        await Expect(line.Locator(".bit-bfc-timeline-label")).ToHaveTextAsync("10:30");
        var top = await line.EvaluateAsync<double>("e => parseFloat(getComputedStyle(e).top)");
        var hour = await Cal.Locator(".bit-bfc-hour-row").First.EvaluateAsync<double>("e => e.getBoundingClientRect().height");
        Assert.AreEqual(10.5 * hour, top, 1, "the line sits 10.5 hours down the grid");
        if (view == "Week")
        {
            await Expect(Cal.Locator(".bit-bfc-today-col .bit-bfc-timeline")).ToHaveCountAsync(1);
            await Expect(Cal.Locator(".bit-bfc-week-day-col.bit-bfc-today-col")).ToHaveCountAsync(1);
        }

        var color = await line.Locator(".bit-bfc-timeline-line").EvaluateAsync<string>("e => getComputedStyle(e).backgroundColor");
        Assert.AreNotEqual("rgba(0, 0, 0, 0)", color);

        await Cal.Locator(".bit-bfc-btn-nav[aria-label='Next']").ClickAsync();
        await Expect(line).ToHaveCountAsync(0);
    }

    [TestMethod]
    public async Task NowLine_TheTimelineDayDrawsItOnEveryRow()
    {
        await OpenMainAsync();
        await Page.Locator("#btn-mode-timeline").ClickAsync();
        await SwitchViewAsync(Cal, "Day");

        var lines = Cal.Locator(".bit-bfc-tl-now");
        await Expect(lines).ToHaveCountAsync(3);
        await Expect(lines.First).ToHaveAttributeAsync("title", "10:30");
        Assert.AreEqual("1008px", await lines.First.EvaluateAsync<string>("e => getComputedStyle(e).insetInlineStart"));
    }

    // ---------------------------------------------------------------- the overflow list and the Events parameter

    [TestMethod]
    public async Task More_AnEventAddedByThePage_IsDrawn_AndAFullDayCollapsesBehindPlusMore()
    {
        await OpenMainAsync();
        var cell = MonthCell(Cal, "Wednesday, July 16, 2025");
        await Expect(cell.Locator(".bit-bfc-month-more")).ToHaveCountAsync(0);

        await Page.Locator("#btn-add-event").ClickAsync();

        var more = cell.Locator(".bit-bfc-month-more");
        await Expect(more).ToHaveTextAsync("+1 more");
        await Expect(more).ToHaveAttributeAsync("aria-label", "+1 more, Wednesday, July 16, 2025");
        await Expect(cell.Locator(".bit-bfc-event-badge-title")).ToHaveCountAsync(3);

        await more.ClickAsync();
        var list = Dialog(Cal);
        await Expect(list.Locator("h3")).ToHaveTextAsync("Events on Wednesday, July 16, 2025");
        await Expect(list.Locator("p")).ToHaveTextAsync("4 event(s)");
        await Expect(list.Locator(".bit-bfc-agenda-title")).ToHaveTextAsync(["Team Standup", "Focus Time", "Design Review", "Added From Page"]);
        // opening the list is not a click on the day: no add dialog
        await Expect(Cal.Locator(".bit-bfc-dialog[role='dialog'] h3", new() { HasText = "Add New Event" })).ToHaveCountAsync(0);
        await PressEscapeAsync();
        await Expect(list).ToHaveCountAsync(0);
    }

    // ---------------------------------------------------------------- filters

    [TestMethod]
    public async Task Filters_TheColorFilter_KeepsOnlyThatColorsEvents()
    {
        await OpenMainAsync();
        var colorFilter = Cal.Locator("select[aria-label='Filter events by color']");
        await Expect(colorFilter.Locator("option")).ToHaveTextAsync(["All colors", "Blue", "Green", "Red", "Yellow", "Purple", "Orange"]);

        await colorFilter.SelectOptionAsync("red");

        await Expect(Badge(Cal, "e6")).ToBeVisibleAsync();
        await Expect(Badge(Cal, "e5")).ToBeVisibleAsync();
        await Expect(Cal.Locator("[data-bit-bfc-event='e1']")).ToHaveCountAsync(0);
        await Expect(Cal.Locator("[data-bit-bfc-event='e2']")).ToHaveCountAsync(0);

        await colorFilter.SelectOptionAsync("");
        await Expect(Badge(Cal, "e1")).ToBeVisibleAsync();
    }

    [TestMethod]
    public async Task Filters_ThePersonFilter_ListsTheAttendeesInView_AndKeepsTheirEvents()
    {
        await OpenMainAsync();
        var personFilter = Cal.Locator("select[aria-label='Filter events by person in current view']");
        await Expect(personFilter.Locator("option")).ToHaveTextAsync(["All people", "Alice Smith", "Bob Jones", "Carol White"]);

        await personFilter.SelectOptionAsync(new SelectOptionValue { Label = "Carol White" });

        await Expect(Badge(Cal, "e7")).ToBeVisibleAsync();
        await Expect(Cal.Locator("[data-bit-bfc-event='e1']")).ToHaveCountAsync(0);
        await Expect(Cal.Locator("[data-bit-bfc-event='e3']")).ToHaveCountAsync(0);
    }

    // ---------------------------------------------------------------- settings panel

    [TestMethod]
    public async Task Settings_TheGearOpensANonModalDialog_EscapeClosesItAndGivesTheFocusBack()
    {
        await OpenMainAsync();
        var gear = SettingsButton(Cal);
        await Expect(gear).ToHaveAttributeAsync("aria-haspopup", "dialog");
        await Expect(gear).ToHaveAttributeAsync("aria-expanded", "false");

        var panel = await OpenSettingsAsync(Cal);
        await Expect(gear).ToHaveAttributeAsync("aria-expanded", "true");
        await Expect(gear).ToHaveAttributeAsync("aria-controls", (await panel.GetAttributeAsync("id"))!);
        await Expect(panel).ToHaveAttributeAsync("data-bit-esc", "claim");
        await Expect(panel.Locator(".bit-bfc-dropdown-label").First).ToHaveTextAsync("Calendar settings");
        await Expect(panel.GetByRole(AriaRole.Switch)).ToHaveCountAsync(7);
        await Expect(panel.GetByRole(AriaRole.Switch, new() { Name = "24-hour format" })).ToHaveAttributeAsync("aria-checked", "true");
        await Expect(panel.GetByRole(AriaRole.Switch, new() { Name = "Dot badge" })).ToHaveAttributeAsync("aria-checked", "false");
        await Expect(panel).ToBeFocusedAsync();

        await PressEscapeAsync();
        await Expect(panel).ToHaveCountAsync(0);
        await Expect(gear).ToBeFocusedAsync();
        await Expect(gear).ToHaveAttributeAsync("aria-expanded", "false");

        // a click elsewhere closes it too
        await OpenSettingsAsync(Cal);
        await Page.Locator("#view").ClickAsync();
        await Expect(SettingsPanel(Cal)).ToHaveCountAsync(0);
    }

    [TestMethod]
    public async Task Settings_The24HourSwitch_RendersTwelveHourTimes_AndWritesTheChoiceBack()
    {
        await OpenMainAsync("Week");
        var panel = await OpenSettingsAsync(Cal);

        await panel.GetByRole(AriaRole.Switch, new() { Name = "24-hour format" }).ClickAsync();

        await Expect(panel.GetByRole(AriaRole.Switch, new() { Name = "24-hour format" })).ToHaveAttributeAsync("aria-checked", "false");
        await Expect(Block(Cal, "e2").Locator(".bit-bfc-event-block-time")).ToHaveTextAsync("10:00 AM - 11:00 AM");
        await Expect(Cal.Locator(".bit-bfc-time-slot-label").Nth(13)).ToHaveTextAsync("1 PM");
        await Expect(Page.Locator("#settings")).ToContainTextAsync("24h=False");
        await Expect(Page.Locator("#settings-change-count")).ToHaveTextAsync("1");
    }

    [TestMethod]
    public async Task Settings_TheDotBadgeSwitch_DrawsMonthBadgesAsDots()
    {
        await OpenMainAsync();
        await Expect(Badge(Cal, "e1")).ToHaveClassAsync(HasClass("bit-bfc-color"));
        var colored = await Badge(Cal, "e1").EvaluateAsync<string>("e => getComputedStyle(e).backgroundColor");
        var panel = await OpenSettingsAsync(Cal);

        await panel.GetByRole(AriaRole.Switch, new() { Name = "Dot badge" }).ClickAsync();

        await Expect(Badge(Cal, "e1")).ToHaveClassAsync(HasClass("dot-variant"));
        await Expect(Badge(Cal, "e1").Locator(".bit-bfc-bullet")).ToBeVisibleAsync();
        Assert.AreNotEqual(colored, await Badge(Cal, "e1").EvaluateAsync<string>("e => getComputedStyle(e).backgroundColor"));
        await Expect(Page.Locator("#settings")).ToContainTextAsync("badge=Dot");
    }

    [TestMethod]
    public async Task Settings_TheWeekNumbersSwitch_AddsTheIsoWeekRail()
    {
        await OpenMainAsync();
        await Expect(Cal.Locator(".bit-bfc-weeknum-cell")).ToHaveCountAsync(0);
        var panel = await OpenSettingsAsync(Cal);

        await panel.GetByRole(AriaRole.Switch, new() { Name = "Show week numbers" }).ClickAsync();

        var weeks = Cal.Locator(".bit-bfc-month-grid .bit-bfc-weeknum-cell");
        await Expect(weeks).ToHaveCountAsync(5);
        await Expect(weeks.First.Locator("[aria-hidden='true']")).ToHaveTextAsync(new Regex(@"^W\d+$"));
        await Expect(weeks.First.Locator(".bit-bfc-sr-only")).ToHaveTextAsync(new Regex(@"^Week \d+$"));
        await Expect(Page.Locator("#settings")).ToContainTextAsync("weeknum=True");
    }

    [TestMethod]
    public async Task Settings_TheCurrentTimeAndDayCalendarSwitches_HideTheLineAndTheSidebar()
    {
        await OpenMainAsync("Day");
        await Expect(Cal.Locator(".bit-bfc-timeline")).ToHaveCountAsync(1);
        await Expect(Cal.Locator(".bit-bfc-day-sidebar")).ToBeVisibleAsync();
        var panel = await OpenSettingsAsync(Cal);

        await panel.GetByRole(AriaRole.Switch, new() { Name = "Show current time" }).ClickAsync();
        await Expect(Cal.Locator(".bit-bfc-timeline")).ToHaveCountAsync(0);

        await panel.GetByRole(AriaRole.Switch, new() { Name = "Show calendar in day view" }).ClickAsync();
        await Expect(Cal.Locator(".bit-bfc-day-sidebar")).ToHaveCountAsync(0);

        await Expect(Page.Locator("#settings")).ToContainTextAsync("mini=False;weeknum=False;now=False");
        await Expect(Page.Locator("#settings-change-count")).ToHaveTextAsync("2");
    }

    [TestMethod]
    public async Task Settings_TheBusinessHoursSwitch_ShadesTheOffHours()
    {
        await OpenMainAsync("Week");
        await Expect(Cal.Locator(".bit-bfc-slot-off")).ToHaveCountAsync(0);
        var panel = await OpenSettingsAsync(Cal);

        await panel.GetByRole(AriaRole.Switch, new() { Name = "Highlight business hours" }).ClickAsync();

        // Mon-Fri 09:00-17:00 is business time; everything else, the whole weekend included, is shaded
        await Expect(Slot(Cal, "Wed 08:30")).ToHaveClassAsync(HasClass("bit-bfc-slot-off"));
        await Expect(Slot(Cal, "Wed 09:00")).Not.ToHaveClassAsync(HasClass("bit-bfc-slot-off"));
        await Expect(Slot(Cal, "Wed 16:30")).Not.ToHaveClassAsync(HasClass("bit-bfc-slot-off"));
        await Expect(Slot(Cal, "Wed 17:00")).ToHaveClassAsync(HasClass("bit-bfc-slot-off"));
        await Expect(Slot(Cal, "Sat 12:00")).ToHaveClassAsync(HasClass("bit-bfc-slot-off"));
        await Expect(Cal.Locator(".bit-bfc-slot-off")).ToHaveCountAsync(7 * 48 - 5 * 16);
        var shade = await Slot(Cal, "Wed 08:30").EvaluateAsync<string>("e => getComputedStyle(e).backgroundColor");
        Assert.AreNotEqual("rgba(0, 0, 0, 0)", shade);
        await Expect(Page.Locator("#settings")).ToContainTextAsync("business=True");
    }

    [TestMethod]
    public async Task Settings_TheDayStartAndSlotDuration_ScrollTheGridAndSplitTheHours()
    {
        await OpenMainAsync("Week");
        var scroller = Cal.Locator(".bit-bfc-week-scroll");
        var hour = await Cal.Locator(".bit-bfc-hour-row").First.EvaluateAsync<double>("e => e.getBoundingClientRect().height");
        // the grid opens scrolled to StartOfDayHour (8)
        Assert.AreEqual(8 * hour, await scroller.EvaluateAsync<double>("e => e.scrollTop"), hour / 2);
        var panel = await OpenSettingsAsync(Cal);

        var start = panel.GetByLabel("Day starts at");
        await start.FillAsync("13");
        await start.PressAsync("Tab");
        await Expect(Page.Locator("#settings")).ToContainTextAsync("start=13");
        await Page.WaitForFunctionAsync("h => document.querySelector('#cal .bit-bfc-week-scroll').scrollTop > h", 12 * hour);

        await panel.GetByLabel("Slot duration").SelectOptionAsync("15");
        await Expect(Page.Locator("#settings")).ToContainTextAsync("slot=15");
        await Expect(Slot(Cal, "Wed 13:15")).ToBeAttachedAsync();
        await Expect(Slot(Cal, "Wed 13:45")).ToBeAttachedAsync();
        await Expect(Cal.Locator(".bit-bfc-week-day-col").First.Locator("[role='button'][aria-label^='Add event']")).ToHaveCountAsync(96);
    }

    [TestMethod]
    public async Task Settings_TheAgendaGroupingRadios_RegroupTheAgendaByColor()
    {
        await OpenMainAsync("Agenda");
        var panel = await OpenSettingsAsync(Cal);
        var date = panel.GetByRole(AriaRole.Radio, new() { Name = "Date" });
        var color = panel.GetByRole(AriaRole.Radio, new() { Name = "Color" });
        await Expect(date).ToHaveAttributeAsync("aria-checked", "true");
        await Expect(date).ToHaveAttributeAsync("tabindex", "0");
        await Expect(color).ToHaveAttributeAsync("tabindex", "-1");

        await date.FocusAsync();
        await Page.Keyboard.PressAsync("ArrowDown");

        await Expect(color).ToHaveAttributeAsync("aria-checked", "true");
        await Expect(color).ToBeFocusedAsync();
        await Expect(Cal.Locator(".bit-bfc-agenda-group-title")).ToHaveTextAsync(["Blue", "Green", "Red", "Yellow", "Purple", "Orange"]);
        await Expect(Page.Locator("#settings")).ToContainTextAsync("group=Color");

        await date.ClickAsync();
        await Expect(Cal.Locator(".bit-bfc-agenda-group-title").First).ToHaveTextAsync("Monday, Jul 14, 2025");
    }

    [TestMethod]
    public async Task Settings_AChangeTheAppMakesOnTheSettingsInstance_IsAppliedOnTheNextRender()
    {
        await OpenMainAsync("Week");
        await Expect(Block(Cal, "e2").Locator(".bit-bfc-event-block-time")).ToHaveTextAsync("10:00 - 11:00");

        await Page.Locator("#btn-24h").ClickAsync();

        await Expect(Block(Cal, "e2").Locator(".bit-bfc-event-block-time")).ToHaveTextAsync("10:00 AM - 11:00 AM");
        // an app's own change is not reported back to it as the user's
        await Expect(Page.Locator("#settings-change-count")).ToHaveTextAsync("0");
    }

    // ---------------------------------------------------------------- read-only, disabled and the permissions

    [TestMethod]
    [DataRow("#btn-readonly", "bit-bfc-readonly")]
    [DataRow("#btn-disabled", "bit-dis")]
    public async Task ReadOnly_AndDisabled_TakeEveryEditAwayButKeepNavigationAndDetails(string toggle, string rootClass)
    {
        await OpenMainAsync();

        await Page.Locator(toggle).ClickAsync();

        await Expect(Cal).ToHaveClassAsync(HasClass(rootClass));
        await Expect(Cal).ToHaveClassAsync(HasClass("bit-bfc-no-add"));
        await Expect(Cal.Locator(".bit-bfc-header-right .bit-bfc-btn-primary")).ToHaveCountAsync(0);
        await Expect(Cal.Locator(".bit-bfc-cell-add-hint-month")).ToHaveCountAsync(0);
        await Expect(Badge(Cal, "e1")).ToHaveAttributeAsync("draggable", "false");
        await Expect(Badge(Cal, "e1")).Not.ToHaveAttributeAsync("data-bit-bfc-move", new Regex(".*"));

        // a click on a day only selects it
        await Cal.Locator(".bit-bfc-month-cell:has(.bit-bfc-month-cell-day:text-is('22'))").ClickAsync(new() { Position = new() { X = 10, Y = 60 } });
        await Expect(Page.Locator("#date")).ToHaveTextAsync("2025-07-22");
        await Expect(Dialog(Cal)).ToHaveCountAsync(0);

        // the details stay readable, with Close alone
        await Badge(Cal, "e1").ClickAsync();
        var dialog = Dialog(Cal);
        await Expect(dialog.Locator("h3")).ToHaveTextAsync("Team Standup");
        await Expect(dialog.Locator(".bit-bfc-dialog-footer button")).ToHaveTextAsync(["Close"]);
        await PressEscapeAsync();

        // the time grids expose no add slots and no resize handles, and the keys edit nothing
        await SwitchViewAsync(Cal, "Week");
        await Expect(Cal.Locator(".bit-bfc-week-scroll [role='button'][aria-label^='Add event']")).ToHaveCountAsync(0);
        await Expect(Cal.Locator(".bit-bfc-resize-handle")).ToHaveCountAsync(0);
        // the week of the selected July 22, with the Client Call of the 23rd
        await Block(Cal, "e7").FocusAsync();
        await Page.Keyboard.PressAsync("Alt+ArrowDown");
        await Page.Keyboard.PressAsync("Shift+ArrowDown");
        await Cal.Locator(".bit-bfc-btn-nav[aria-label='Next']").ClickAsync();
        await Expect(Title(Cal)).ToHaveTextAsync("Jul 27, 2025 - Aug 2, 2025");
        await Expect(Page.Locator("#change-count")).ToHaveTextAsync("0");

        // and back on, everything is editable again
        await Page.Locator(toggle).ClickAsync();
        await Expect(Cal).Not.ToHaveClassAsync(HasClass(rootClass));
        await Expect(Cal.Locator(".bit-bfc-header-right .bit-bfc-btn-primary")).ToBeVisibleAsync();
    }

    [TestMethod]
    public async Task Permissions_AllowAddOff_HidesEveryAddAffordance_ButEditingStays()
    {
        await OpenMainAsync();

        await Page.Locator("#btn-allow-add").ClickAsync();

        await Expect(Cal).ToHaveClassAsync(HasClass("bit-bfc-no-add"));
        await Expect(Cal).Not.ToHaveClassAsync(HasClass("bit-bfc-readonly"));
        await Expect(Cal.Locator(".bit-bfc-header-right .bit-bfc-btn-primary")).ToHaveCountAsync(0);
        await Expect(Cal.Locator(".bit-bfc-cell-add-hint-month")).ToHaveCountAsync(0);
        await Expect(Badge(Cal, "e1")).ToHaveAttributeAsync("draggable", "true");
        await Badge(Cal, "e1").ClickAsync();
        await Expect(Dialog(Cal).GetByRole(AriaRole.Button, new() { Name = "Edit" })).ToBeVisibleAsync();
        await Expect(Dialog(Cal).GetByRole(AriaRole.Button, new() { Name = "Delete" })).ToBeVisibleAsync();
    }

    [TestMethod]
    public async Task Permissions_AllowEditOff_DropsEditDragAndResize_ButNotDelete()
    {
        await OpenMainAsync("Week");

        await Page.Locator("#btn-allow-edit").ClickAsync();

        await Expect(Block(Cal, "e2")).ToHaveAttributeAsync("draggable", "false");
        await Expect(Cal.Locator(".bit-bfc-resize-handle")).ToHaveCountAsync(0);
        await Block(Cal, "e2").ClickAsync();
        var dialog = Dialog(Cal);
        await Expect(dialog.GetByRole(AriaRole.Button, new() { Name = "Edit" })).ToHaveCountAsync(0);
        await Expect(dialog.GetByRole(AriaRole.Button, new() { Name = "Delete" })).ToBeVisibleAsync();
    }

    [TestMethod]
    public async Task Permissions_AllowDeleteOff_DropsOnlyTheDeleteAction()
    {
        await OpenMainAsync();

        await Page.Locator("#btn-allow-delete").ClickAsync();
        await Badge(Cal, "e1").ClickAsync();

        var dialog = Dialog(Cal);
        await Expect(dialog.GetByRole(AriaRole.Button, new() { Name = "Edit" })).ToBeVisibleAsync();
        await Expect(dialog.GetByRole(AriaRole.Button, new() { Name = "Delete" })).ToHaveCountAsync(0);
    }

    [TestMethod]
    public async Task Permissions_AllowDragOff_StopsMovingButKeepsResizing()
    {
        await OpenMainAsync("Week");

        await Page.Locator("#btn-allow-drag").ClickAsync();

        var block = Block(Cal, "e2");
        await Expect(block).ToHaveAttributeAsync("draggable", "false");
        await Expect(block).Not.ToHaveAttributeAsync("data-bit-bfc-move", new Regex(".*"));
        await Expect(block).ToHaveAttributeAsync("data-bit-bfc-resize", "y");
        await block.FocusAsync();
        await Page.Keyboard.PressAsync("Alt+ArrowDown");
        await Page.Keyboard.PressAsync("Shift+ArrowDown");
        await Expect(Page.Locator("#last-change")).ToHaveTextAsync(new Regex(@"^Edit\|Resize\|Focus Time\|2025-07-16 10:00\|2025-07-16 11:30\|"));
        await Expect(Page.Locator("#change-count")).ToHaveTextAsync("1");
    }

    [TestMethod]
    public async Task Permissions_AllowResizeOff_DropsTheHandlesButKeepsMoving()
    {
        await OpenMainAsync("Week");

        await Page.Locator("#btn-allow-resize").ClickAsync();

        var block = Block(Cal, "e2");
        await Expect(Cal.Locator(".bit-bfc-resize-handle")).ToHaveCountAsync(0);
        await Expect(block).Not.ToHaveAttributeAsync("data-bit-bfc-resize", new Regex(".*"));
        await Expect(block).ToHaveAttributeAsync("draggable", "true");
        await block.FocusAsync();
        await Page.Keyboard.PressAsync("Shift+ArrowDown");
        await Page.Keyboard.PressAsync("Alt+ArrowDown");
        await Expect(Page.Locator("#last-change")).ToHaveTextAsync(new Regex(@"^Edit\|Drag\|Focus Time\|2025-07-16 10:30\|2025-07-16 11:30\|"));
        await Expect(Page.Locator("#change-count")).ToHaveTextAsync("1");
    }

    // ---------------------------------------------------------------- the public methods

    [TestMethod]
    public async Task Methods_GoToDateTodayNextAndPrevious_MoveTheCalendar()
    {
        await OpenMainAsync();

        await Page.Locator("#btn-goto-date").ClickAsync();
        await Expect(Title(Cal)).ToHaveTextAsync("December 2025");
        await Expect(Page.Locator("#date")).ToHaveTextAsync("2025-12-25");

        await Page.Locator("#btn-nav-next").ClickAsync();
        await Expect(Title(Cal)).ToHaveTextAsync("January 2026");
        await Page.Locator("#btn-nav-prev").ClickAsync();
        await Page.Locator("#btn-nav-prev").ClickAsync();
        await Expect(Title(Cal)).ToHaveTextAsync("November 2025");

        await Page.Locator("#btn-goto-today").ClickAsync();
        await Expect(Title(Cal)).ToHaveTextAsync("July 2025");
        await Expect(Page.Locator("#date")).ToHaveTextAsync("2025-07-16");
    }

    [TestMethod]
    public async Task Methods_ChangeViewAndChangeMode_SwitchTheCalendarAndReportIt()
    {
        await OpenMainAsync();

        await Page.Locator("#btn-change-view-day").ClickAsync();
        await Expect(Cal.Locator(".bit-bfc-day-layout")).ToBeVisibleAsync();
        await Expect(Page.Locator("#view")).ToHaveTextAsync("Day");
        await Expect(Page.Locator("#last-view-change")).ToHaveTextAsync("Day");

        await Page.Locator("#btn-change-mode-timeline").ClickAsync();
        await Expect(Cal.Locator(".bit-bfc-tl-layout")).ToBeVisibleAsync();
        await Expect(Page.Locator("#mode")).ToHaveTextAsync("Timeline");
        await Expect(Page.Locator("#last-mode-change")).ToHaveTextAsync("Timeline");
    }

    [TestMethod]
    public async Task Methods_ScrollToTimeAsync_ScrollsATimeGridAndRefusesAViewWithoutOne()
    {
        await OpenMainAsync();
        await Page.Locator("#btn-scroll-14").ClickAsync();
        await Expect(Page.Locator("#scroll-result")).ToHaveTextAsync("False");

        await SwitchViewAsync(Cal, "Week");
        await Page.Locator("#btn-scroll-14").ClickAsync();
        await Expect(Page.Locator("#scroll-result")).ToHaveTextAsync("True");

        // 14:00 now sits right under the sticky day header
        var slotTop = (await Slot(Cal, "Wed 14:00").BoundingBoxAsync())!.Y;
        var header = (await Cal.Locator(".bit-bfc-week-header").BoundingBoxAsync())!;
        Assert.AreEqual(header.Y + header.Height, slotTop, 2);
    }

    [TestMethod]
    public async Task Methods_GetVisibleRangeAndState_ReadWhatIsOnScreen()
    {
        await OpenMainAsync("Week");

        await Page.Locator("#btn-visible-range").ClickAsync();
        await Expect(Page.Locator("#visible-range")).ToHaveTextAsync("2025-07-13|2025-07-19");

        await Page.Locator("#btn-read-state").ClickAsync();
        await Expect(Page.Locator("#state")).ToHaveTextAsync(new Regex(@"^Week\|Event\|2025-07-16\|2025-07-16\|\d+$"));

        await SwitchViewAsync(Cal, "Day");
        await Page.Locator("#btn-visible-range").ClickAsync();
        await Expect(Page.Locator("#visible-range")).ToHaveTextAsync("2025-07-16|2025-07-16");
    }

    [TestMethod]
    public async Task Methods_TheSelectedDateOfTheReadOutState_FollowsTheUser()
    {
        await OpenMainAsync();
        await Cal.Locator(".bit-bfc-btn-nav[aria-label='Next']").ClickAsync();

        await Page.Locator("#btn-read-state").ClickAsync();

        await Expect(Page.Locator("#state")).ToHaveTextAsync(new Regex(@"^Month\|Event\|2025-08-16\|2025-07-16\|"));
        var count = int.Parse((await Page.Locator("#state").InnerTextAsync()).Split('|')[4], CultureInfo.InvariantCulture);
        Assert.IsTrue(count >= 0);
    }
}
