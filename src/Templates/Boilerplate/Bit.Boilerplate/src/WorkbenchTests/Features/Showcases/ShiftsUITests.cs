using Boilerplate.Workbench.Components.Pages.Showcases.Shifts.Data;

namespace Boilerplate.WorkbenchTests.Features.Showcases;

/// <summary>The shift planner in a view: the week summary, its modes, views and dates, the staff and role filters, adding, editing, moving and deleting a shift with the editor's checks, the data states, and all of it after a reload.</summary>
[TestClass, TestCategory("UITest")]
public class ShiftsUITests : ShowcaseUITest
{
    private const string PageKey = "shifts";

    private static readonly DateTime today = DateTime.Today;

    /// <summary>Which day of its week today is, the position in each person's pattern.</summary>
    private static int TodayIndex => (today - ShiftsService.WeekStart(today)).Days;

    private static string NameOf(ShiftStaff member) => WorkbenchStrings.ResourceManager.GetString(member.NameKey)!;

    private static ShiftStaff FreeToday => ShiftsService.Staff.First(member => member.Pattern[TodayIndex] == '-');

    private static ShiftStaff WorkingToday => ShiftsService.Staff.First(member => member.Pattern[TodayIndex] != '-');

    private static int ShiftsToday => ShiftsService.Staff.Count(member => member.Pattern[TodayIndex] != '-');

    /// <summary>A person's sample shift today: its title and hours, as the seed gives them.</summary>
    private static (string Title, int Start, int End) TodaysShiftOf(ShiftStaff member) => member.Pattern[TodayIndex] switch
    {
        'O' => (WorkbenchStrings.ShiftsOpening, 7, 15),
        'M' => (WorkbenchStrings.ShiftsMidday, 10, 18),
        'B' => (WorkbenchStrings.ShiftsBakery, 7, 13),
        _ => (WorkbenchStrings.ShiftsClosing, 11, 19)
    };

    private ILocator Calendar => View().Locator(".calendar");

    private ILocator Events => Calendar.Locator("[data-bit-bfc-event]");

    private ILocator Rows => Calendar.Locator(".staff");

    private ILocator Heading => Calendar.Locator(".bit-bfc-header-title");

    private ILocator AddEvent => ViewButton(WorkbenchStrings.CalendarAddEventButton);

    private ILocator Filters => View().Locator(".filters");

    private ILocator StaffFilter => Filters.GetByRole(AriaRole.Combobox, new() { Name = WorkbenchStrings.ShiftsStaffLabel });

    private ILocator RoleFilter => Filters.GetByRole(AriaRole.Combobox, new() { Name = WorkbenchStrings.ShiftsRoleLabel });

    private ILocator Editor => View().Locator(".bit-mdl-ctn.editor-modal");

    private ILocator ShiftName => Editor.GetByRole(AriaRole.Textbox, new() { Name = WorkbenchStrings.ShiftsNameLabel });

    private ILocator StaffField => Editor.GetByRole(AriaRole.Combobox, new() { Name = WorkbenchStrings.ShiftsStaffLabel });

    private ILocator RoleField => Editor.GetByRole(AriaRole.Combobox, new() { Name = WorkbenchStrings.ShiftsRoleLabel });

    private ILocator DayField => Editor.GetByRole(AriaRole.Combobox, new() { Name = WorkbenchStrings.ShiftsDayLabel });

    private ILocator Starts => Editor.GetByRole(AriaRole.Combobox, new() { Name = WorkbenchStrings.ShiftsStartsLabel });

    private ILocator Ends => Editor.GetByRole(AriaRole.Combobox, new() { Name = WorkbenchStrings.ShiftsEndsLabel });

    private ILocator Save => Editor.GetByRole(AriaRole.Button, new() { Name = WorkbenchStrings.Save, Exact = true });

    private ILocator Cancel => Editor.GetByRole(AriaRole.Button, new() { Name = WorkbenchStrings.Cancel, Exact = true });

    private ILocator Delete => Editor.GetByRole(AriaRole.Button, new() { Name = AppStrings.Delete, Exact = true });

    private ILocator NewShiftTitle => Editor.GetByText(WorkbenchStrings.ShiftsAddTitle);

    private ILocator EditShiftTitle => Editor.GetByText(WorkbenchStrings.ShiftsEditTitle);

    private ILocator Tab(string name) => Calendar.GetByRole(AriaRole.Tab, new() { Name = name, Exact = true });

    private ILocator Panel(string mode, string view) => Calendar.GetByRole(AriaRole.Tabpanel, new() { Name = $"{mode} {view}", Exact = true });

    private ILocator Slot(ShiftStaff member, string time) => Calendar.GetByRole(AriaRole.Button, new() { Name = $"{WorkbenchStrings.CalendarAddEventHoverHint}, {NameOf(member)}, {time}", Exact = true });

    /// <summary>The shift with this title and hours on the timeline.</summary>
    private ILocator ShiftAt(string title, int start, int end) => Calendar.GetByRole(AriaRole.Button, new() { Name = $"{title}, {start:00}:00 - {end:00}:00", Exact = true });

    private ILocator ShiftsNamed(string title) => Calendar.GetByRole(AriaRole.Button, new() { NameRegex = new Regex($"^{Regex.Escape(title)},") });

    /// <summary>A person's sample shift today, by the id the seed gives it.</summary>
    private ILocator SampleShiftToday(ShiftStaff member) => Calendar.Locator($"[data-bit-bfc-event='s{21 + TodayIndex}-{ShiftsService.Staff.ToList().IndexOf(member)}']");

    private ILocator Summary(int shifts, string hours) => View().GetByText(string.Format(CultureInfo.InvariantCulture, WorkbenchStrings.ShiftsSummary, shifts, hours));

    private static string Short(DateTime date) => date.ToString("MMM d, yyyy", CultureInfo.CurrentCulture);

    /// <summary>Closes a dropdown's panel by pressing the page beside it, as its close button is under a device's status bar.</summary>
    private async Task PressBesideThePanel()
    {
        var overlay = View().Locator(".bit-drp-ovl").Locator("visible=true");
        var beside = new Position { X = 8, Y = 300 };

        await (IsPhone ? overlay.TapAsync(new() { Position = beside }) : overlay.ClickAsync(new() { Position = beside }));
    }

    /// <summary>Changes a filter's choices in its panel, then closes the panel.</summary>
    private async Task ToggleInFilter(ILocator filter, params string[] options)
    {
        await Press(filter);

        foreach (var option in options)
        {
            var item = VisibleOption(option);
            var wasSelected = await item.GetAttributeAsync("aria-selected");

            await Press(item);
            await Expect(item).Not.ToHaveAttributeAsync("aria-selected", wasSelected!);
        }

        await PressBesideThePanel();
    }

    private async Task PickInEditor(ILocator field, string option)
    {
        await Press(field);
        await Press(VisibleOption(option));
    }

    /// <summary>Sets the hour of a time field in its clock, then closes the clock by pressing beside it.</summary>
    private async Task SetHour(ILocator field, int hour)
    {
        await Press(field);
        await Expect(field).ToHaveAttributeAsync("aria-expanded", "true");
        await OpenPopups.Last.GetByRole(AriaRole.Spinbutton).First.FillAsync(hour.ToString(CultureInfo.InvariantCulture));
        await Press(View().Locator(".bit-tpc-ovl").Locator("visible=true"));
        await Expect(field).ToHaveValueAsync($"{hour:00}:00");
    }

    /// <summary>Picks a day in the day field's calendar, in this month or the next.</summary>
    private async Task PickDay(DateTime day)
    {
        await Press(DayField);

        if (day.Month != today.Month)
        {
            await Press(OpenPopups.Last.GetByRole(AriaRole.Button, new() { Name = "Go to next month", Exact = true }));
        }

        await Press(OpenPopups.Last.GetByRole(AriaRole.Gridcell, new() { Name = day.ToString("dddd, MMMM d, yyyy", CultureInfo.CurrentCulture), Exact = true }));
    }

    /// <summary>Adds a shift by pressing a free person's empty slot at 10:00, which makes it four hours long.</summary>
    private async Task AddAtTen(string name)
    {
        await Press(Slot(FreeToday, "10:00"));
        await Expect(NewShiftTitle).ToBeVisibleAsync();
        await ShiftName.FillAsync(name);
        await Press(Save);
        await Expect(ShiftAt(name, 10, 14)).ToBeVisibleAsync();
    }

    [TestMethod, EverySurface]
    public async Task TheWeekSummary_Should_CountTheShiftsAndHoursOfTheWeekShown(WorkbenchSurface surface)
    {
        await OpenWorkbench(PageKey);

        await Expect(View().GetByRole(AriaRole.Heading, new() { Name = WorkbenchStrings.ShiftsHeading })).ToBeVisibleAsync();
        await Expect(Summary(30, "230")).ToBeVisibleAsync();

        await Press(Tab(WorkbenchStrings.CalendarViewWeek));
        await Press(ViewButton(WorkbenchStrings.CalendarNextButtonTitle));

        // Next week has the stocktake and a day off.
        await Expect(Summary(27, "208")).ToBeVisibleAsync();

        await Press(ViewButton(WorkbenchStrings.CalendarTodayButton));

        await Expect(Summary(30, "230")).ToBeVisibleAsync();

        await ExpectNoSidewaysScroll();
    }

    [TestMethod, EverySurface]
    public async Task ANarrowView_Should_StartOnTheDayWithEveryPersonsRow(WorkbenchSurface surface)
    {
        await OpenWorkbench(PageKey);

        await Expect(Tab(WorkbenchStrings.CalendarViewDay)).ToHaveAttributeAsync("aria-selected", "true");
        await Expect(Panel(WorkbenchStrings.CalendarModeTimeline, WorkbenchStrings.CalendarViewDay)).ToBeVisibleAsync();
        await Expect(Heading).ToHaveTextAsync(Short(today));
        await Expect(Rows).ToHaveCountAsync(ShiftsService.Staff.Count);
        await Expect(Events).ToHaveCountAsync(ShiftsToday);
    }

    [TestMethod, OnDesktops]
    public async Task AWideView_Should_StartOnTheMonth(WorkbenchSurface surface)
    {
        await OpenWorkbench(PageKey, "?device=desktop-1536");

        await Expect(Tab(WorkbenchStrings.CalendarViewMonth)).ToHaveAttributeAsync("aria-selected", "true");
        await Expect(Panel(WorkbenchStrings.CalendarModeTimeline, WorkbenchStrings.CalendarViewMonth)).ToBeVisibleAsync();
        await Expect(Rows).ToHaveCountAsync(ShiftsService.Staff.Count);

        await ExpectNoSidewaysScroll();
    }

    [TestMethod, EverySurface]
    public async Task TheViews_Should_SwitchAndStayAfterAReload(WorkbenchSurface surface)
    {
        await OpenWorkbench(PageKey);

        await Press(Tab(WorkbenchStrings.CalendarViewWeek));

        var weekStart = ShiftsService.WeekStart(today);

        await Expect(Panel(WorkbenchStrings.CalendarModeTimeline, WorkbenchStrings.CalendarViewWeek)).ToBeVisibleAsync();
        await Expect(Heading).ToHaveTextAsync($"{Short(weekStart)} - {Short(weekStart.AddDays(6))}");

        await Press(Tab(WorkbenchStrings.CalendarViewMonth));

        await Expect(Panel(WorkbenchStrings.CalendarModeTimeline, WorkbenchStrings.CalendarViewMonth)).ToBeVisibleAsync();
        await Expect(Heading).ToHaveTextAsync(today.ToString("MMMM yyyy", CultureInfo.CurrentCulture));

        await ReloadTheView();

        await Expect(Tab(WorkbenchStrings.CalendarViewMonth)).ToHaveAttributeAsync("aria-selected", "true");
        await Expect(Panel(WorkbenchStrings.CalendarModeTimeline, WorkbenchStrings.CalendarViewMonth)).ToBeVisibleAsync();
    }

    [TestMethod, EverySurface]
    public async Task TheEventsMode_Should_OfferTheAgendaAndStayAfterAReload(WorkbenchSurface surface)
    {
        await OpenWorkbench(PageKey);

        await Expect(Tab(WorkbenchStrings.CalendarViewAgenda)).ToHaveCountAsync(0);

        await Press(Tab(WorkbenchStrings.CalendarModeEvent));

        await Expect(Panel(WorkbenchStrings.CalendarModeEvent, WorkbenchStrings.CalendarViewDay)).ToBeVisibleAsync();

        await Press(Tab(WorkbenchStrings.CalendarViewAgenda));

        var agenda = Panel(WorkbenchStrings.CalendarModeEvent, WorkbenchStrings.CalendarViewAgenda);

        await Expect(agenda).ToBeVisibleAsync();
        await Expect(agenda.GetByRole(AriaRole.Heading).First).ToBeVisibleAsync();

        await ReloadTheView();

        await Expect(agenda).ToBeVisibleAsync();

        await Press(Tab(WorkbenchStrings.CalendarModeTimeline));

        await Expect(Tab(WorkbenchStrings.CalendarViewAgenda)).ToHaveCountAsync(0);
        await Expect(Rows).ToHaveCountAsync(ShiftsService.Staff.Count);
    }

    [TestMethod, EverySurface]
    public async Task TheAgendaSearch_Should_FindAShift(WorkbenchSurface surface)
    {
        await OpenWorkbench(PageKey);

        await Press(Tab(WorkbenchStrings.CalendarModeEvent));
        await Press(Tab(WorkbenchStrings.CalendarViewAgenda));

        var agenda = Panel(WorkbenchStrings.CalendarModeEvent, WorkbenchStrings.CalendarViewAgenda);

        await agenda.GetByRole(AriaRole.Searchbox, new() { Name = WorkbenchStrings.CalendarSearchEventsPlaceholder }).FillAsync(WorkbenchStrings.ShiftsBakery);

        await Expect(agenda.GetByRole(AriaRole.Button).First).ToContainTextAsync(WorkbenchStrings.ShiftsBakery);
        await Expect(agenda.GetByRole(AriaRole.Button).Filter(new() { HasText = WorkbenchStrings.ShiftsOpening })).ToHaveCountAsync(0);
    }

    [TestMethod, EverySurface]
    public async Task TheDateButtons_Should_MoveAndComeBackAndKeepTheDateAfterAReload(WorkbenchSurface surface)
    {
        await OpenWorkbench(PageKey);

        await Press(ViewButton(WorkbenchStrings.CalendarNextButtonTitle));

        await Expect(Heading).ToHaveTextAsync(Short(today.AddDays(1)));

        await ReloadTheView();

        await Expect(Heading).ToHaveTextAsync(Short(today.AddDays(1)));

        await Press(ViewButton(WorkbenchStrings.CalendarPreviousButtonTitle));
        await Press(ViewButton(WorkbenchStrings.CalendarPreviousButtonTitle));

        await Expect(Heading).ToHaveTextAsync(Short(today.AddDays(-1)));

        await Press(ViewButton(WorkbenchStrings.CalendarTodayButton));

        await Expect(Heading).ToHaveTextAsync(Short(today));
        await Expect(Events).ToHaveCountAsync(ShiftsToday);
    }

    [TestMethod, EverySurface]
    public async Task TheStocktakeAndTheDayOff_Should_ShowOnNextWeek(WorkbenchSurface surface)
    {
        await OpenWorkbench(PageKey);

        await Press(Tab(WorkbenchStrings.CalendarViewWeek));
        await Press(ViewButton(WorkbenchStrings.CalendarNextButtonTitle));

        await Expect(Calendar.GetByRole(AriaRole.Button, new() { NameRegex = new Regex(Regex.Escape(WorkbenchStrings.ShiftsStocktake)) }).First).ToBeAttachedAsync();
        await Expect(Calendar.GetByRole(AriaRole.Button, new() { NameRegex = new Regex(Regex.Escape(WorkbenchStrings.ShiftsTimeOff)) }).First).ToBeAttachedAsync();
    }

    [TestMethod, EverySurface]
    public async Task TheStaffFilter_Should_HideSomeonesRowAndStayAfterAReload(WorkbenchSurface surface)
    {
        await OpenWorkbench(PageKey);

        var hidden = WorkingToday;

        await Expect(StaffFilter).ToContainTextAsync(WorkbenchStrings.ShiftsAllStaff);

        await ToggleInFilter(StaffFilter, NameOf(hidden));

        await Expect(Rows).ToHaveCountAsync(ShiftsService.Staff.Count - 1);
        await Expect(Calendar.GetByText(NameOf(hidden), new() { Exact = true })).ToHaveCountAsync(0);
        await Expect(Events).ToHaveCountAsync(ShiftsToday - 1);
        await Expect(StaffFilter).Not.ToContainTextAsync(WorkbenchStrings.ShiftsAllStaff);
        await Expect(StaffFilter).Not.ToContainTextAsync(NameOf(hidden));

        await ReloadTheView();

        await Expect(Rows).ToHaveCountAsync(ShiftsService.Staff.Count - 1);

        await ToggleInFilter(StaffFilter, NameOf(hidden));

        await Expect(Rows).ToHaveCountAsync(ShiftsService.Staff.Count);
        await Expect(StaffFilter).ToContainTextAsync(WorkbenchStrings.ShiftsAllStaff);
    }

    [TestMethod, EverySurface]
    public async Task TheRoleFilter_Should_HideThatRolesShiftsButNotThePeople(WorkbenchSurface surface)
    {
        await OpenWorkbench(PageKey);

        await Press(Tab(WorkbenchStrings.CalendarViewMonth));

        await Expect(ShiftsNamed(WorkbenchStrings.ShiftsBakery).First).ToBeAttachedAsync();
        await Expect(RoleFilter).ToContainTextAsync(WorkbenchStrings.ShiftsAllRoles);

        await ToggleInFilter(RoleFilter, WorkbenchStrings.ShiftsRoleBaker);

        await Expect(ShiftsNamed(WorkbenchStrings.ShiftsBakery)).ToHaveCountAsync(0);
        await Expect(ShiftsNamed(WorkbenchStrings.ShiftsOpening).First).ToBeAttachedAsync();
        await Expect(Rows).ToHaveCountAsync(ShiftsService.Staff.Count);
        await Expect(RoleFilter).Not.ToContainTextAsync(WorkbenchStrings.ShiftsRoleBaker);

        await ReloadTheView();

        await Expect(ShiftsNamed(WorkbenchStrings.ShiftsBakery)).ToHaveCountAsync(0);
    }

    [TestMethod, EverySurface]
    public async Task EmptyFilters_Should_SayNoOneAndNoRoles(WorkbenchSurface surface)
    {
        await OpenWorkbench(PageKey);

        await ToggleInFilter(StaffFilter, [.. ShiftsService.Staff.Select(NameOf)]);

        await Expect(Rows).ToHaveCountAsync(0);
        await Expect(StaffFilter).ToContainTextAsync(WorkbenchStrings.ShiftsNoStaff);

        await ToggleInFilter(RoleFilter, [.. Enum.GetValues<ShiftRole>().Select(role => WorkbenchStrings.ResourceManager.GetString($"ShiftsRole{role}")!)]);

        await Expect(RoleFilter).ToContainTextAsync(WorkbenchStrings.ShiftsNoRoles);
    }

    [TestMethod, OnPhones]
    public async Task TheFilterPanel_Should_CloseWithItsCloseButton(WorkbenchSurface surface)
    {
        await OpenWorkbench(PageKey);

        await Press(RoleFilter);

        var close = OpenPopups.GetByRole(AriaRole.Button, new() { Name = WorkbenchStrings.Close, Exact = true });

        await Press(close);

        await Expect(VisibleOption(WorkbenchStrings.ShiftsRoleBaker)).ToHaveCountAsync(0);
    }

    [TestMethod, EverySurface]
    public async Task AnEmptySlot_Should_OpenTheEditorForThatPersonAndTime(WorkbenchSurface surface)
    {
        await OpenWorkbench(PageKey);

        await Press(Slot(FreeToday, "10:00"));

        await Expect(NewShiftTitle).ToBeVisibleAsync();
        await Expect(StaffField).ToContainTextAsync(NameOf(FreeToday));
        await Expect(RoleField).ToContainTextAsync(WorkbenchStrings.ResourceManager.GetString($"ShiftsRole{FreeToday.Role}")!);
        await Expect(DayField).ToHaveValueAsync(today.ToString("M/d/yyyy", CultureInfo.CurrentCulture));
        await Expect(Starts).ToHaveValueAsync("10:00");
        await Expect(Ends).ToHaveValueAsync("14:00");
        await Expect(Delete).ToHaveCountAsync(0);
    }

    [TestMethod, EverySurface]
    public async Task AddingAShift_Should_ShowItCountItAndKeepItAfterAReload(WorkbenchSurface surface)
    {
        var name = $"Stocking {Guid.NewGuid():N}";

        await OpenWorkbench(PageKey);

        await AddAtTen(name);

        await Expect(Notice(WorkbenchStrings.ShiftsSaved)).ToBeVisibleAsync();
        await Expect(Summary(31, "234")).ToBeVisibleAsync();
        await Expect(Editor).Not.ToBeVisibleAsync();

        await ReloadTheView();

        await Expect(ShiftAt(name, 10, 14)).ToBeVisibleAsync();
        await Expect(Summary(31, "234")).ToBeVisibleAsync();
    }

    [TestMethod, EverySurface]
    public async Task TheAddButton_Should_OpenTheEditorAndCancelShouldAddNothing(WorkbenchSurface surface)
    {
        await OpenWorkbench(PageKey);

        await Press(AddEvent);

        await Expect(NewShiftTitle).ToBeVisibleAsync();

        await Press(Cancel);

        await Expect(NewShiftTitle).ToHaveCountAsync(0);

        await Press(AddEvent);
        await Press(Editor.Locator(".editor-header").GetByRole(AriaRole.Button));

        await Expect(NewShiftTitle).ToHaveCountAsync(0);
        await Expect(Summary(30, "230")).ToBeVisibleAsync();
        await Expect(Events).ToHaveCountAsync(ShiftsToday);
    }

    [TestMethod, EverySurface]
    public async Task PickingSomeone_Should_BringTheirUsualRole(WorkbenchSurface surface)
    {
        await OpenWorkbench(PageKey);

        await Press(AddEvent);

        await PickInEditor(StaffField, WorkbenchStrings.PosStaffMila);

        await Expect(StaffField).ToContainTextAsync(WorkbenchStrings.PosStaffMila);
        await Expect(RoleField).ToContainTextAsync(WorkbenchStrings.ShiftsRoleBaker);

        await PickInEditor(RoleField, WorkbenchStrings.ShiftsRoleCashier);

        await Expect(RoleField).ToContainTextAsync(WorkbenchStrings.ShiftsRoleCashier);
    }

    [TestMethod, EverySurface]
    public async Task TheEditor_Should_RefuseAShiftThatEndsBeforeItStartsUntilItIsFixed(WorkbenchSurface surface)
    {
        await OpenWorkbench(PageKey);

        await Press(Slot(FreeToday, "10:00"));

        await SetHour(Ends, 9);
        await Press(Save);

        await Expect(Editor).ToContainTextAsync(WorkbenchStrings.ShiftsEndBeforeStart);
        await Expect(NewShiftTitle).ToBeVisibleAsync();
        await Expect(Summary(30, "230")).ToBeVisibleAsync();

        await SetHour(Ends, 12);

        await Expect(Editor).Not.ToContainTextAsync(WorkbenchStrings.ShiftsEndBeforeStart);

        await Press(Save);

        await Expect(NewShiftTitle).ToHaveCountAsync(0);
        await Expect(Summary(31, "232")).ToBeVisibleAsync();
    }

    [TestMethod, EverySurface]
    public async Task TheEditor_Should_RefuseAShiftOutsideOpeningHours(WorkbenchSurface surface)
    {
        await OpenWorkbench(PageKey);

        await Press(Slot(FreeToday, "10:00"));

        await SetHour(Starts, 6);
        await Press(Save);

        await Expect(Editor).ToContainTextAsync(WorkbenchStrings.CalendarOutsideBusinessHoursMessage);
        await Expect(Summary(30, "230")).ToBeVisibleAsync();
    }

    [TestMethod, EverySurface]
    public async Task TheEditor_Should_RefuseAShiftOverSomeonesOtherShift(WorkbenchSurface surface)
    {
        await OpenWorkbench(PageKey);

        await Press(Slot(FreeToday, "11:00"));

        await PickInEditor(StaffField, NameOf(WorkingToday));
        await SetHour(Ends, 13);
        await Press(Save);

        await Expect(Editor).ToContainTextAsync(WorkbenchStrings.CalendarEventOverlapMessage);
        await Expect(Summary(30, "230")).ToBeVisibleAsync();
    }

    [TestMethod, EverySurface]
    public async Task TheEditor_Should_RefuseAShiftOverTheShiftOfSomeoneFilteredOut(WorkbenchSurface surface)
    {
        await OpenWorkbench(PageKey);

        await ToggleInFilter(StaffFilter, NameOf(WorkingToday));
        await Press(Slot(FreeToday, "11:00"));

        await PickInEditor(StaffField, NameOf(WorkingToday));
        await SetHour(Ends, 13);
        await Press(Save);

        await Expect(Editor).ToContainTextAsync(string.Format(CultureInfo.InvariantCulture, WorkbenchStrings.ShiftsOverlap, NameOf(WorkingToday)));
    }

    [TestMethod, EverySurface]
    public async Task TheEditor_Should_RefuseAShiftDuringTheStocktake(WorkbenchSurface surface)
    {
        await OpenWorkbench(PageKey);

        await Press(AddEvent);

        await PickDay(ShiftsService.StocktakeDay);
        await SetHour(Starts, 15);
        await SetHour(Ends, 18);
        await Press(Save);

        await Expect(Editor).ToContainTextAsync(WorkbenchStrings.CalendarBlockedMessage);
    }

    [TestMethod, EverySurface]
    public async Task PressingAShift_Should_OpenItsEditorWithItsValues(WorkbenchSurface surface)
    {
        var (title, start, end) = TodaysShiftOf(WorkingToday);

        await OpenWorkbench(PageKey);

        await Press(SampleShiftToday(WorkingToday));

        await Expect(EditShiftTitle).ToBeVisibleAsync();
        await Expect(ShiftName).ToHaveValueAsync(title);
        await Expect(StaffField).ToContainTextAsync(NameOf(WorkingToday));
        await Expect(RoleField).ToContainTextAsync(WorkbenchStrings.ResourceManager.GetString($"ShiftsRole{WorkingToday.Role}")!);
        await Expect(Starts).ToHaveValueAsync($"{start:00}:00");
        await Expect(Ends).ToHaveValueAsync($"{end:00}:00");
        await Expect(Delete).ToBeVisibleAsync();
    }

    [TestMethod, EverySurface]
    public async Task EditingAShift_Should_ChangeItAndKeepItAfterAReload(WorkbenchSurface surface)
    {
        var (_, start, end) = TodaysShiftOf(WorkingToday);
        var name = $"Renamed {Guid.NewGuid():N}";

        await OpenWorkbench(PageKey);

        await Press(SampleShiftToday(WorkingToday));
        await ShiftName.FillAsync(name);
        await Press(Save);

        await Expect(ShiftAt(name, start, end)).ToBeVisibleAsync();
        await Expect(Notice(WorkbenchStrings.ShiftsSaved)).ToBeVisibleAsync();
        await Expect(Events).ToHaveCountAsync(ShiftsToday);

        await ReloadTheView();

        await Expect(ShiftAt(name, start, end)).ToBeVisibleAsync();
    }

    [TestMethod, EverySurface]
    public async Task CancellingAnEdit_Should_LeaveTheShiftAlone(WorkbenchSurface surface)
    {
        var (title, start, end) = TodaysShiftOf(WorkingToday);

        await OpenWorkbench(PageKey);

        await Press(SampleShiftToday(WorkingToday));
        await ShiftName.FillAsync("Changed my mind");
        await Press(Cancel);

        await Expect(EditShiftTitle).ToHaveCountAsync(0);
        await Expect(Calendar.GetByRole(AriaRole.Button, new() { Name = "Changed my mind" })).ToHaveCountAsync(0);
        await Expect(ShiftAt(title, start, end).First).ToBeAttachedAsync();
    }

    [TestMethod, EverySurface]
    public async Task TheEditor_Should_MoveAShiftToAnotherTime(WorkbenchSurface surface)
    {
        var name = $"Moved {Guid.NewGuid():N}";

        await OpenWorkbench(PageKey);

        await AddAtTen(name);

        await Press(ShiftAt(name, 10, 14));
        await SetHour(Starts, 12);
        await SetHour(Ends, 16);
        await Press(Save);

        await Expect(ShiftAt(name, 12, 16)).ToBeVisibleAsync();
        await Expect(ShiftAt(name, 10, 14)).ToHaveCountAsync(0);

        await ReloadTheView();

        await Expect(ShiftAt(name, 12, 16)).ToBeVisibleAsync();
    }

    [TestMethod, EverySurface]
    public async Task TheEditor_Should_MoveAShiftToAnotherDay(WorkbenchSurface surface)
    {
        var name = $"Moved {Guid.NewGuid():N}";

        await OpenWorkbench(PageKey);

        await AddAtTen(name);

        await Press(ShiftAt(name, 10, 14));
        await PickDay(today.AddDays(7));
        await Press(Save);

        await Expect(ShiftAt(name, 10, 14)).ToHaveCountAsync(0);
        await Expect(Summary(30, "230")).ToBeVisibleAsync();

        await Press(Tab(WorkbenchStrings.CalendarViewWeek));
        await Press(ViewButton(WorkbenchStrings.CalendarNextButtonTitle));

        await Expect(ShiftsNamed(name)).ToHaveCountAsync(1);
    }

    [TestMethod, OnDesktops]
    public async Task DraggingAShift_Should_MoveItAndKeepItAfterAReload(WorkbenchSurface surface)
    {
        var (title, _, _) = TodaysShiftOf(WorkingToday);

        await OpenWorkbench(PageKey, "?device=desktop-1536");

        await Press(Tab(WorkbenchStrings.CalendarViewDay));

        await Expect(Panel(WorkbenchStrings.CalendarModeTimeline, WorkbenchStrings.CalendarViewDay)).ToBeVisibleAsync();
        await Expect(Events).ToHaveCountAsync(ShiftsToday);

        await Drag(SampleShiftToday(WorkingToday), Slot(FreeToday, "09:00"), toX: 0.1);

        await Expect(Events).ToHaveCountAsync(ShiftsToday);

        await Press(SampleShiftToday(WorkingToday));

        await Expect(StaffField).ToContainTextAsync(NameOf(FreeToday));

        await Press(Cancel);
        await ReloadTheView();
        await Press(SampleShiftToday(WorkingToday));

        await Expect(StaffField).ToContainTextAsync(NameOf(FreeToday));
        await Expect(ShiftName).ToHaveValueAsync(title);
    }

    [TestMethod, EverySurface]
    public async Task DraggingAShiftsEnd_Should_ResizeIt(WorkbenchSurface surface)
    {
        var (title, start, end) = TodaysShiftOf(WorkingToday);

        await OpenWorkbench(PageKey);

        await Drag(SampleShiftToday(WorkingToday).Locator(".bit-bfc-resize-handle-end"), Slot(WorkingToday, $"{end - 1:00}:00"), toX: 0);

        await Expect(ShiftAt(title, start, end - 1)).ToBeVisibleAsync();

        await ReloadTheView();

        await Expect(ShiftAt(title, start, end - 1)).ToBeVisibleAsync();
        await Expect(Summary(30, "229")).ToBeVisibleAsync();
    }

    [TestMethod, OnDesktops]
    public async Task TheEnterKeyOnAShift_Should_OpenItsEditor(WorkbenchSurface surface)
    {
        await OpenWorkbench(PageKey);

        await SampleShiftToday(WorkingToday).FocusAsync();
        await Page.Keyboard.PressAsync("Enter");

        await Expect(EditShiftTitle).ToBeVisibleAsync();
    }

    [TestMethod, EverySurface]
    public async Task DeletingAShift_Should_TakeItAwayAndStayDeletedAfterAReload(WorkbenchSurface surface)
    {
        await OpenWorkbench(PageKey);

        await Press(SampleShiftToday(WorkingToday));
        await Press(Delete);

        await Expect(EditShiftTitle).ToHaveCountAsync(0);
        await Expect(Notice(WorkbenchStrings.ShiftsDeleted)).ToBeVisibleAsync();
        await Expect(SampleShiftToday(WorkingToday)).ToHaveCountAsync(0);
        await Expect(Summary(29, WorkingToday.Pattern[TodayIndex] is 'B' ? "224" : "222")).ToBeVisibleAsync();

        await ReloadTheView();

        await Expect(SampleShiftToday(WorkingToday)).ToHaveCountAsync(0);
        await Expect(Events).ToHaveCountAsync(ShiftsToday - 1);
    }

    [TestMethod, EverySurface]
    public async Task TheDraft_Should_ReopenAfterAReload(WorkbenchSurface surface)
    {
        await OpenWorkbench(PageKey);

        await Press(Slot(FreeToday, "10:00"));
        await ShiftName.FillAsync("Half typed");
        await ShiftName.BlurAsync();

        await ReloadTheView();

        await Expect(NewShiftTitle).ToBeVisibleAsync();
        await Expect(ShiftName).ToHaveValueAsync("Half typed");
        await Expect(Starts).ToHaveValueAsync("10:00");
        await Expect(StaffField).ToContainTextAsync(NameOf(FreeToday));
    }

    [TestMethod, EverySurface]
    public async Task Loading_Should_LeaveTheCalendarInPlaceAndOfferNoChanges(WorkbenchSurface surface)
    {
        await OpenWorkbench(PageKey, "?data=loading");

        await Expect(Panel(WorkbenchStrings.CalendarModeTimeline, WorkbenchStrings.CalendarViewDay)).ToBeVisibleAsync();
        await Expect(Events).ToHaveCountAsync(0);
        await Expect(AddEvent).ToHaveCountAsync(0);
        await Expect(View().GetByText(WorkbenchStrings.ShiftsEmpty)).ToHaveCountAsync(0);
    }

    [TestMethod, EverySurface]
    public async Task Slow_Should_ShowTheShiftsAfterAWhile(WorkbenchSurface surface)
    {
        await OpenWithoutWaiting(PageKey, "?data=slow");

        await Expect(Panel(WorkbenchStrings.CalendarModeTimeline, WorkbenchStrings.CalendarViewDay)).ToBeVisibleAsync();
        await Expect(Events).ToHaveCountAsync(ShiftsToday);
        await Expect(AddEvent).ToBeVisibleAsync();
    }

    [TestMethod, EverySurface]
    public async Task Empty_Should_SaySoAndKeepWhatIsAddedOnThePageOnly(WorkbenchSurface surface)
    {
        var name = $"First {Guid.NewGuid():N}";

        await OpenWorkbench(PageKey, "?data=empty");

        await Expect(View().GetByText(WorkbenchStrings.ShiftsEmpty)).ToBeVisibleAsync();
        await Expect(Events).ToHaveCountAsync(0);
        await Expect(Summary(0, "0")).ToBeVisibleAsync();

        await AddAtTen(name);

        await Expect(View().GetByText(WorkbenchStrings.ShiftsEmpty)).ToHaveCountAsync(0);
        await Expect(Summary(1, "4")).ToBeVisibleAsync();

        await ReloadTheView();

        await Expect(View().GetByText(WorkbenchStrings.ShiftsEmpty)).ToBeVisibleAsync();
        await Expect(Events).ToHaveCountAsync(0);
    }

    [TestMethod, EverySurface]
    public async Task Error_Should_SaySoAndARetryShouldLoadTheShifts(WorkbenchSurface surface)
    {
        await OpenWorkbench(PageKey, "?data=error");

        await Expect(View().GetByText(WorkbenchStrings.LoadFailed)).ToBeVisibleAsync();
        await Expect(Events).ToHaveCountAsync(0);
        await Expect(AddEvent).ToHaveCountAsync(0);

        await Press(ViewButton(WorkbenchStrings.PosRetry));

        await Expect(Events).ToHaveCountAsync(ShiftsToday);
        await Expect(View().GetByText(WorkbenchStrings.LoadFailed)).ToHaveCountAsync(0);
        await Expect(AddEvent).ToBeVisibleAsync();
        await Expect(Summary(30, "230")).ToBeVisibleAsync();
    }

    [TestMethod, EverySurface]
    public async Task Mixed_Should_FailItsOnlySourceAndARetryShouldLoadIt(WorkbenchSurface surface)
    {
        await OpenWorkbench(PageKey, "?data=mixed");

        await Expect(View().GetByText(WorkbenchStrings.LoadFailed)).ToBeVisibleAsync();

        await Press(ViewButton(WorkbenchStrings.PosRetry));

        await Expect(View().GetByText(WorkbenchStrings.LoadFailed)).ToHaveCountAsync(0);
        await Expect(Events).ToHaveCountAsync(ShiftsToday);
    }
}
