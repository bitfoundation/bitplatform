namespace Bit.BlazorUI.Demo.Client.Core.Pages.Components.Extras.FullCalendar;

public partial class BitFullCalendarDemo
{
    private const string eventsCode = @"
private readonly List<BitFullCalendarEvent> events = CreateEvents();

private static List<BitFullCalendarEvent> CreateEvents()
{
    var today = DateTime.Today;
    var id = 0;
    return
    [
        new() { Id = (++id).ToString(), Title = ""Team Standup"", Description = ""Daily sync with engineering."", StartDate = today.AddHours(9), EndDate = today.AddHours(9).AddMinutes(45), Color = ""blue"" },
        new() { Id = (++id).ToString(), Title = ""Design Review"", Description = ""Dashboard mockups v2."", StartDate = today.AddHours(10), EndDate = today.AddHours(11), Color = ""purple"" },
        new() { Id = (++id).ToString(), Title = ""1:1 with Manager"", Description = ""Career and sprint check-in."", StartDate = today.AddHours(10).AddMinutes(30), EndDate = today.AddHours(11).AddMinutes(15), Color = ""yellow"" },
        new() { Id = (++id).ToString(), Title = ""Lunch with Client"", Description = ""Q3 roadmap discussion."", StartDate = today.AddHours(12), EndDate = today.AddHours(13).AddMinutes(30), Color = ""green"" },
        new() { Id = (++id).ToString(), Title = ""Sprint Planning"", Description = ""Next sprint goals and capacity."", StartDate = today.AddHours(14), EndDate = today.AddHours(15).AddMinutes(30), Color = ""orange"" },
        new() { Id = (++id).ToString(), Title = ""Code Review"", Description = ""Auth module PRs."", StartDate = today.AddHours(16), EndDate = today.AddHours(17), Color = ""red"" },
        new() { Id = (++id).ToString(), Title = ""Tech Conference"", Description = ""Keynotes and workshops."", StartDate = today.AddDays(1).AddHours(9), EndDate = today.AddDays(3).AddHours(17), Color = ""blue"" },
        new() { Id = (++id).ToString(), Title = ""Client Onboarding"", Description = ""Platform walkthrough."", StartDate = today.AddDays(1).AddHours(10), EndDate = today.AddDays(1).AddHours(11).AddMinutes(30), Color = ""yellow"" },
        new() { Id = (++id).ToString(), Title = ""Architecture Review"", Description = ""Migration plan."", StartDate = today.AddDays(2).AddHours(14), EndDate = today.AddDays(2).AddHours(16), Color = ""red"" },
        new() { Id = (++id).ToString(), Title = ""Company Retreat"", Description = ""Strategy and team building."", StartDate = today.AddDays(5), EndDate = today.AddDays(7).AddHours(16), Color = ""purple"" },
        new() { Id = (++id).ToString(), Title = ""Quarterly Review"", Description = ""Company-wide QBR."", StartDate = today.AddDays(-3).AddHours(10), EndDate = today.AddDays(-3).AddHours(12), Color = ""red"" },
        new() { Id = (++id).ToString(), Title = ""Product Demo"", Description = ""Stakeholder walkthrough."", StartDate = today.AddDays(-2).AddHours(14), EndDate = today.AddDays(-2).AddHours(15), Color = ""orange"" },
    ];
}";

    private const string resourcesCode = @"
private readonly List<BitFullCalendarResource> resources =
[
    new() { Id = ""room-bay"", Title = ""HQ - Bay Wing"", Subtitle = ""Headquarters"" },
    new() { Id = ""room-garden"", Title = ""The Garden"", Subtitle = ""Headquarters"" },
    new() { Id = ""room-war"", Title = ""War Room (B1)"", Subtitle = ""Basement"" },
];

private readonly List<BitFullCalendarEvent> events = CreateResourceEvents();

private static List<BitFullCalendarEvent> CreateResourceEvents()
{
    var today = DateTime.Today;
    var id = 100;
    return
    [
        new() { Id = (++id).ToString(), Title = ""Design Review"", StartDate = today.AddHours(10), EndDate = today.AddHours(11), Resource = ""room-bay"", Color = ""purple"" },
        new() { Id = (++id).ToString(), Title = ""Standup"", StartDate = today.AddHours(9), EndDate = today.AddHours(9).AddMinutes(30), Resource = ""room-garden"", Color = ""blue"" },
        new() { Id = (++id).ToString(), Title = ""Incident Bridge"", StartDate = today.AddHours(13), EndDate = today.AddHours(15), Resource = ""room-war"", Color = ""red"" },
        new() { Id = (++id).ToString(), Title = ""Workshop"", StartDate = today.AddHours(14), EndDate = today.AddHours(16), Resource = ""room-bay"", Color = ""orange"" },
    ];
}";



    private readonly string example1RazorCode = @"
<BitFullCalendar Events=""events"" />";
    private readonly string example1CsharpCode = eventsCode;

    private readonly string example2RazorCode = @"
<BitChoiceGroup Horizontal
                Label=""Available views""
                TItem=""BitChoiceGroupOption<string>""
                TValue=""string""
                @bind-Value=""viewsPreset"">
    <BitChoiceGroupOption Text=""Week and Day"" Value=""@(""week-day"")"" />
    <BitChoiceGroupOption Text=""Month and Agenda"" Value=""@(""month-agenda"")"" />
    <BitChoiceGroupOption Text=""Month only"" Value=""@(""month"")"" />
</BitChoiceGroup>

<BitFullCalendar Events=""events"" Views=""SelectedViews"" />";
    private readonly string example2CsharpCode = @"
private string viewsPreset = ""week-day"";

private BitFullCalendarView[] SelectedViews => viewsPreset switch
{
    ""month-agenda"" => [BitFullCalendarView.Month, BitFullCalendarView.Agenda],
    ""month"" => [BitFullCalendarView.Month],
    _ => [BitFullCalendarView.Week, BitFullCalendarView.Day]
};
" + eventsCode;

    private readonly string example3RazorCode = @"
<BitChoiceGroup Horizontal
                Label=""Event layout""
                TItem=""BitChoiceGroupOption<BitFullCalendarEventLayout>""
                TValue=""BitFullCalendarEventLayout""
                @bind-Value=""layoutMode"">
    <BitChoiceGroupOption Text=""Overlap"" Value=""BitFullCalendarEventLayout.Overlap"" />
    <BitChoiceGroupOption Text=""Stack"" Value=""BitFullCalendarEventLayout.Stack"" />
</BitChoiceGroup>

<BitFullCalendar Events=""events""
                 Settings=""settings""
                 DefaultView=""BitFullCalendarView.Week""
                 OnSettingsChange=""HandleSettingsChange"" />";
    private readonly string example3CsharpCode = @"
// Mutated in place: the calendar diffs it against the values it last applied.
private readonly BitFullCalendarSettings settings = new()
{
    Use24HourFormat = false,
    StartOfDayHour = 9,
    BadgeVariant = BitFullCalendarBadgeVariant.Dot,
    EventLayout = BitFullCalendarEventLayout.Stack
};

private BitFullCalendarEventLayout layoutMode
{
    get => settings.EventLayout;
    set => settings.EventLayout = value;
}

// The callback re-renders this component, so the choice group above shows a layout picked from the gear.
private void HandleSettingsChange(BitFullCalendarSettings changed) { }
" + eventsCode;

    private readonly string example4RazorCode = @"
<BitChoiceGroup Horizontal
                Label=""Visible hours""
                TItem=""BitChoiceGroupOption<string>""
                TValue=""string""
                @bind-Value=""gridHoursPreset"">
    <BitChoiceGroupOption Text=""Whole day"" Value=""@(""full"")"" />
    <BitChoiceGroupOption Text=""08 - 18"" Value=""@(""office"")"" />
    <BitChoiceGroupOption Text=""06 - 22"" Value=""@(""extended"")"" />
</BitChoiceGroup>
<BitChoiceGroup Horizontal
                Label=""Slot duration""
                TItem=""BitChoiceGroupOption<int>""
                TValue=""int""
                @bind-Value=""gridSlotMinutes"">
    <BitChoiceGroupOption Text=""15 min"" Value=""15"" />
    <BitChoiceGroupOption Text=""30 min"" Value=""30"" />
    <BitChoiceGroupOption Text=""60 min"" Value=""60"" />
</BitChoiceGroup>

<BitFullCalendar Events=""events"" Settings=""gridSettings"" DefaultView=""BitFullCalendarView.Week"" />";
    private readonly string example4CsharpCode = @"
private readonly BitFullCalendarSettings gridSettings = new()
{
    VisibleStartHour = 8,
    VisibleEndHour = 18,
    SlotDurationMinutes = 30,
    StartOfDayHour = 9
};

private string _gridHoursPreset = ""office"";
private string gridHoursPreset
{
    get => _gridHoursPreset;
    set
    {
        _gridHoursPreset = value;
        (gridSettings.VisibleStartHour, gridSettings.VisibleEndHour) = value switch
        {
            ""office"" => (8, 18),
            ""extended"" => (6, 22),
            _ => (0, 24)
        };
    }
}

private int gridSlotMinutes
{
    get => gridSettings.SlotDurationMinutes;
    set => gridSettings.SlotDurationMinutes = value;
}
" + eventsCode;

    private readonly string example5RazorCode = @"
<BitChoiceGroup Horizontal
                Label=""Days""
                TItem=""BitChoiceGroupOption<string>""
                TValue=""string""
                @bind-Value=""workWeekPreset"">
    <BitChoiceGroupOption Text=""Full week"" Value=""@(""full"")"" />
    <BitChoiceGroupOption Text=""Mon - Fri"" Value=""@(""mon-fri"")"" />
    <BitChoiceGroupOption Text=""Sun - Thu"" Value=""@(""sun-thu"")"" />
</BitChoiceGroup>
<BitToggle @bind-Value=""workWeekNumbers"" Text=""Week numbers"" />
<BitToggle @bind-Value=""workWeekThreeDays"" Text=""Three days"" />

<BitFullCalendar Events=""events"" Settings=""workWeekSettings"" DefaultView=""BitFullCalendarView.Week"" />";
    private readonly string example5CsharpCode = @"
private readonly BitFullCalendarSettings workWeekSettings = new()
{
    HiddenDays = [DayOfWeek.Saturday, DayOfWeek.Sunday],
    FirstDayOfWeek = DayOfWeek.Monday,
    ShowWeekNumbers = true,
    VisibleStartHour = 8,
    VisibleEndHour = 19
};

private string _workWeekPreset = ""mon-fri"";
private string workWeekPreset
{
    get => _workWeekPreset;
    set
    {
        _workWeekPreset = value;
        (workWeekSettings.HiddenDays, workWeekSettings.FirstDayOfWeek) = value switch
        {
            ""mon-fri"" => ((IReadOnlyList<DayOfWeek>?)[DayOfWeek.Saturday, DayOfWeek.Sunday], (DayOfWeek?)DayOfWeek.Monday),
            ""sun-thu"" => ([DayOfWeek.Friday, DayOfWeek.Saturday], DayOfWeek.Sunday),
            _ => (null, null)
        };
    }
}

private bool workWeekNumbers
{
    get => workWeekSettings.ShowWeekNumbers;
    set => workWeekSettings.ShowWeekNumbers = value;
}

private bool workWeekThreeDays
{
    get => workWeekSettings.WeekDayCount is 3;
    set => workWeekSettings.WeekDayCount = value ? 3 : null;
}
" + eventsCode;

    private readonly string example6RazorCode = @"
<BitToggle @bind-Value=""fixedWeeks"" Text=""Fixed six weeks"" />
<BitToggle @bind-Value=""showOtherMonthDays"" Text=""Show neighbouring days"" />
<BitToggle @bind-Value=""navLinks"" Text=""Navigation links"" />

<BitFullCalendar Events=""events"" Settings=""monthGridSettings"" />";
    private readonly string example6CsharpCode = @"
private readonly BitFullCalendarSettings monthGridSettings = new()
{
    FixedWeekCount = true,
    ShowNonCurrentDates = false,
    MaxEventsPerDayCell = 2,
    NavLinks = true,
    ShowWeekNumbers = true
};

private bool fixedWeeks
{
    get => monthGridSettings.FixedWeekCount;
    set => monthGridSettings.FixedWeekCount = value;
}

private bool showOtherMonthDays
{
    get => monthGridSettings.ShowNonCurrentDates;
    set => monthGridSettings.ShowNonCurrentDates = value;
}

private bool navLinks
{
    get => monthGridSettings.NavLinks;
    set => monthGridSettings.NavLinks = value;
}
" + eventsCode;

    private readonly string example7RazorCode = @"
<BitFullCalendar Events=""events"" DefaultView=""BitFullCalendarView.Week"" />";
    private readonly string example7CsharpCode = @"
private readonly List<BitFullCalendarEvent> events = CreateAllDayEvents();

private static List<BitFullCalendarEvent> CreateAllDayEvents()
{
    var today = DateTime.Today;
    var id = 200;
    return
    [
        new() { Id = (++id).ToString(), Title = ""Company Holiday"", Description = ""Offices closed."", StartDate = today, EndDate = today.AddDays(1), Color = ""green"", IsAllDay = true },
        new() { Id = (++id).ToString(), Title = ""Release Freeze"", Description = ""No deploys this week."", StartDate = today.AddDays(1), EndDate = today.AddDays(4), Color = ""red"", IsAllDay = true },
        new() { Id = (++id).ToString(), Title = ""Alice on leave"", StartDate = today.AddDays(-1), EndDate = today.AddDays(2), Color = ""yellow"", IsAllDay = true },
        new() { Id = (++id).ToString(), Title = ""Team Standup"", Description = ""Daily sync with engineering."", StartDate = today.AddHours(9), EndDate = today.AddHours(9).AddMinutes(30), Color = ""blue"" },
        new() { Id = (++id).ToString(), Title = ""Sprint Planning"", Description = ""Next sprint goals."", StartDate = today.AddHours(14), EndDate = today.AddHours(15).AddMinutes(30), Color = ""purple"" },
    ];
}";

    private readonly string example8RazorCode = @"
<BitFullCalendar Events=""events"" />";
    private readonly string example8CsharpCode = @"
private readonly List<BitFullCalendarEvent> events = CreateRecurringEvents();

private static List<BitFullCalendarEvent> CreateRecurringEvents()
{
    var today = DateTime.Today;
    var id = 400;
    return
    [
        new()
        {
            Id = (++id).ToString(), Title = ""Daily Standup"", Description = ""Every weekday at 09:00."",
            StartDate = today.AddHours(9), EndDate = today.AddHours(9).AddMinutes(15), Color = ""blue"",
            Recurrence = new()
            {
                Frequency = BitFullCalendarRecurrenceFrequency.Weekly,
                DaysOfWeek = [DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday, DayOfWeek.Thursday, DayOfWeek.Friday],
                ExceptionDates = [today.AddDays(3)] // a cancelled one
            }
        },
        new()
        {
            Id = (++id).ToString(), Title = ""Sprint Review"", Description = ""Every other Friday, six times."",
            StartDate = today.AddHours(15), EndDate = today.AddHours(16), Color = ""purple"",
            Recurrence = new() { Frequency = BitFullCalendarRecurrenceFrequency.Weekly, Interval = 2, DaysOfWeek = [DayOfWeek.Friday], Count = 6 }
        },
        new()
        {
            Id = (++id).ToString(), Title = ""Board Meeting"", Description = ""The third Tuesday of every month for six months, plus one extra session."",
            StartDate = today.AddHours(11), EndDate = today.AddHours(12), Color = ""red"",
            Recurrence = new()
            {
                Frequency = BitFullCalendarRecurrenceFrequency.Monthly,
                WeekOfMonth = BitFullCalendarWeekOfMonth.Third,
                DaysOfWeek = [DayOfWeek.Tuesday],
                Until = today.AddMonths(6),
                AdditionalDates = [today.AddDays(10)] // a make-up session
            }
        },
        new()
        {
            Id = (++id).ToString(), Title = ""Backup Check"", Description = ""Every 15 days."",
            StartDate = today.AddHours(8), EndDate = today.AddHours(8).AddMinutes(30), Color = ""green"",
            Recurrence = new() { Frequency = BitFullCalendarRecurrenceFrequency.Daily, Interval = 15 }
        },
        new()
        {
            Id = (++id).ToString(), Title = ""Month-end Report"", Description = ""The last Friday of every month."",
            StartDate = today.AddHours(16), EndDate = today.AddHours(17), Color = ""orange"",
            Recurrence = new() { Frequency = BitFullCalendarRecurrenceFrequency.Monthly, WeekOfMonth = BitFullCalendarWeekOfMonth.Last, DaysOfWeek = [DayOfWeek.Friday] }
        },
    ];
}";

    private readonly string example9RazorCode = @"
<div style=""--bit-FullCalendar-event-color:#64748b"">
    <BitFullCalendar Events=""events"" EventColorOptions=""colorOptions"" />
</div>";
    private readonly string example9CsharpCode = @"
private readonly List<BitFullCalendarColorOption> colorOptions =
[
    new() { Id = ""work"", Title = ""Work"", Value = ""#2563eb"" },
    new() { Id = ""personal"", Title = ""Personal"", Value = ""#16a34a"" },
    new() { Id = ""urgent"", Title = ""Urgent"", Value = ""#dc2626"" },
];

private readonly List<BitFullCalendarEvent> events = CreateEvents();

private static List<BitFullCalendarEvent> CreateEvents()
{
    var today = DateTime.Today;
    return
    [
        new() { Id = ""1"", Title = ""Planning"", StartDate = today.AddHours(9), EndDate = today.AddHours(10), Color = ""work"" },
        new() { Id = ""2"", Title = ""Gym"", StartDate = today.AddHours(17), EndDate = today.AddHours(18), Color = ""personal"" },
        new() { Id = ""3"", Title = ""Hotfix"", StartDate = today.AddDays(1).AddHours(11), EndDate = today.AddDays(1).AddHours(12), Color = ""urgent"" },
        // A color of its own, straight from the data.
        new() { Id = ""4"", Title = ""Brand launch"", StartDate = today.AddDays(2).AddHours(13), EndDate = today.AddDays(2).AddHours(15), Color = ""#db2777"" },
        // No option and no CSS color: drawn in --bit-FullCalendar-event-color.
        new() { Id = ""5"", Title = ""Imported"", StartDate = today.AddDays(3).AddHours(10), EndDate = today.AddDays(3).AddHours(11), Color = ""legacy"" },
    ];
}";

    private readonly string example10RazorCode = @"
<style>
    .event-card {
        gap: 2px;
        display: flex;
        flex-direction: column;
    }

    .event-card span {
        opacity: 0.8;
        font-size: 0.75rem;
    }

    .agenda-row {
        gap: 1rem;
        width: 100%;
        display: flex;
        justify-content: space-between;
    }
</style>

<BitFullCalendar Events=""events""
                 DayEventTemplate=""EventCard""
                 WeekEventTemplate=""EventCard""
                 MonthEventTemplate=""MonthBadge""
                 MonthCellTemplate=""HolidayCell""
                 AgendaEventTemplate=""AgendaRow""
                 EventDetailsTemplate=""LocationRow""
                 EventEditorTemplate=""LocationField"" />";
    private readonly string example10CsharpCode = @"
private readonly Dictionary<DateTime, string> holidays = new()
{
    [DateTime.Today.AddDays(4)] = ""Founders' Day"",
    [DateTime.Today.AddDays(11)] = ""Public holiday"",
};

private RenderFragment<BitFullCalendarEvent> EventCard => ev =>
    @<div class=""event-card"">
        <strong>@ev.Title</strong>
        @if (!string.IsNullOrWhiteSpace(ev.Description))
        {
            <span>@ev.Description</span>
        }
    </div>;

private RenderFragment<BitFullCalendarEvent> MonthBadge => ev => @<span>📌 @ev.Title</span>;

private RenderFragment<BitFullCalendarCell> HolidayCell => cell =>
    @<text>
        @if (holidays.TryGetValue(cell.Date, out var holiday))
        {
            <span>🎉 @holiday</span>
        }
    </text>;

private RenderFragment<BitFullCalendarEvent> AgendaRow => ev =>
    @<span class=""agenda-row"">
        <strong>@ev.Title</strong>
        <span>@ev.StartDate.ToString(""ddd d MMM, HH:mm"")</span>
    </span>;

// The location lives in Data; the editor template sets it on the draft, which the save commits.
private RenderFragment<BitFullCalendarEvent> LocationRow => ev =>
    @<text>
        @if (ev.Data is string location)
        {
            <BitText>📍 @location</BitText>
        }
    </text>;

private RenderFragment<BitFullCalendarEvent> LocationField => ev =>
    @<BitTextField Label=""Location"" Value=""@(ev.Data as string)"" ValueChanged=""(string? value) => ev.Data = value"" />;
" + eventsCode;

    private readonly string example11RazorCode = @"
<style>
    .resource-header {
        gap: 0.5rem;
        display: flex;
        min-width: 0;
        align-items: center;
    }

    .resource-header strong, .resource-header small {
        display: block;
        overflow: hidden;
        white-space: nowrap;
        text-overflow: ellipsis;
    }

    .resource-avatar {
        width: 2rem;
        height: 2rem;
        display: grid;
        flex-shrink: 0;
        font-weight: 600;
        place-items: center;
        border-radius: 50%;
        color: var(--bit-clr-pri-text);
        background: var(--bit-clr-pri);
    }
</style>

<BitFullCalendar Events=""events""
                 Resources=""resources""
                 ResourceTemplate=""ResourceHeader""
                 DefaultMode=""BitFullCalendarMode.Timeline"" />";
    private readonly string example11CsharpCode = @"
private RenderFragment<BitFullCalendarResource> ResourceHeader => resource =>
    @<div class=""resource-header"">
        <span class=""resource-avatar"" aria-hidden=""true"">@resource.Title[..1]</span>
        <span>
            <strong>@resource.Title</strong>
            <small>@resource.Subtitle</small>
        </span>
    </div>;
" + resourcesCode;

    private readonly string example12RazorCode = @"
<BitFullCalendar Events=""events"" MinDate=""boundsMin"" MaxDate=""boundsMax"" />

<BitText>Allowed range: <b>@boundsMin.ToString(""yyyy-MM-dd"")</b> to <b>@boundsMax.ToString(""yyyy-MM-dd"")</b></BitText>";
    private readonly string example12CsharpCode = @"
private readonly DateTime boundsMin = DateTime.Today.AddDays(-10);
private readonly DateTime boundsMax = DateTime.Today.AddDays(20);
" + eventsCode;

    private readonly string example13RazorCode = @"
<BitToggle @bind-Value=""allowOverlap"" Text=""Allow overlapping events"" />
<BitToggle @bind-Value=""highlightBusiness"" Text=""Highlight business hours"" />
<BitToggle @bind-Value=""restrictBusiness"" Text=""Restrict to business hours"" />

<BitFullCalendar Events=""events"" Settings=""rulesSettings"" OnRefused=""HandleRefused"" DefaultView=""BitFullCalendarView.Week"" />

<BitText>Last refusal: <b>@(lastRefusal ?? ""-"")</b></BitText>";
    private readonly string example13CsharpCode = @"
private readonly BitFullCalendarSettings rulesSettings = new()
{
    AllowEventOverlap = false,
    HighlightBusinessHours = true,
    BusinessStartHour = 9,
    BusinessEndHour = 17,
    VisibleStartHour = 6,
    VisibleEndHour = 21
};

private bool allowOverlap
{
    get => rulesSettings.AllowEventOverlap;
    set => rulesSettings.AllowEventOverlap = value;
}

private bool highlightBusiness
{
    get => rulesSettings.HighlightBusinessHours;
    set => rulesSettings.HighlightBusinessHours = value;
}

private bool restrictBusiness
{
    get => rulesSettings.RestrictToBusinessHours;
    set => rulesSettings.RestrictToBusinessHours = value;
}

private string? lastRefusal;

private void HandleRefused(BitFullCalendarChangeRefusal refusal) => lastRefusal = refusal.ToString();

private readonly List<BitFullCalendarEvent> events = CreateRuleEvents();

private static List<BitFullCalendarEvent> CreateRuleEvents()
{
    var today = DateTime.Today;
    var id = 300;
    return
    [
        new() { Id = (++id).ToString(), Title = ""Payroll run"", Description = ""Locked - cannot be moved."", StartDate = today.AddHours(9), EndDate = today.AddHours(10), Color = ""red"", IsReadOnly = true },
        new() { Id = (++id).ToString(), Title = ""Design Review"", Description = ""Try dragging this onto the locked slot."", StartDate = today.AddHours(11), EndDate = today.AddHours(12), Color = ""purple"" },
        new() { Id = (++id).ToString(), Title = ""Retro"", Description = ""Sprint retrospective."", StartDate = today.AddHours(15), EndDate = today.AddHours(16), Color = ""blue"" },
        new() { Id = (++id).ToString(), Title = ""Lunch break"", StartDate = today.AddHours(12), EndDate = today.AddHours(13), Color = ""#94a3b8"", IsBackground = true, IsBlocking = true, Recurrence = new() { Frequency = BitFullCalendarRecurrenceFrequency.Daily } },
        new() { Id = (++id).ToString(), Title = ""Company offsite"", StartDate = today.AddDays(2), EndDate = today.AddDays(2), Color = ""green"", IsAllDay = true, IsBackground = true },
    ];
}";

    private readonly string example14RazorCode = @"
<BitToggle @bind-Value=""isReadOnly"" Text=""Read-only"" />
<BitToggle @bind-Value=""permissionSettings.AllowAdd"" Text=""Add"" />
<BitToggle @bind-Value=""permissionSettings.AllowEdit"" Text=""Edit"" />
<BitToggle @bind-Value=""permissionSettings.AllowDelete"" Text=""Delete"" />
<BitToggle @bind-Value=""permissionSettings.AllowDrag"" Text=""Drag"" />
<BitToggle @bind-Value=""permissionSettings.AllowResize"" Text=""Resize"" />

<BitFullCalendar Events=""events"" ReadOnly=""isReadOnly"" Settings=""permissionSettings"" DefaultView=""BitFullCalendarView.Week"" />";
    private readonly string example14CsharpCode = @"
private bool isReadOnly;

private readonly BitFullCalendarSettings permissionSettings = new();
" + eventsCode;

    private readonly string example15RazorCode = @"
<BitToggle @bind-Value=""refusePast"" Text=""Refuse changes that start in the past"" />

<BitFullCalendar Events=""events"" OnChanging=""HandleChanging"" OnChange=""HandleChange"" />

<BitText>Last change: <b>@(lastChange ?? ""-"")</b></BitText>";
    private readonly string example15CsharpCode = @"
private string? lastChange;
private bool refusePast = true;

private void HandleChanging(BitFullCalendarChangingEventArgs args)
{
    if (refusePast && args.Kind is not BitFullCalendarChangeKind.Delete && args.Event.StartDate < DateTime.Now)
    {
        args.Cancel = true;
        lastChange = $""Refused ({args.Source}): {args.Event.Title} would start in the past"";
    }
}

private Task HandleChange(BitFullCalendarChangeEventArgs args)
{
    lastChange = $""{args.Kind} ({args.Source}): {args.Event.Title}"";

    // Keep the bound list in sync with the calendar so a change is not lost on the next render.
    switch (args.Kind)
    {
        case BitFullCalendarChangeKind.Add:
            events.Add(args.Event);
            break;
        case BitFullCalendarChangeKind.Edit:
            var index = events.FindIndex(e => e.Id == args.Event.Id);
            if (index >= 0)
                events[index] = args.Event;
            else
                events.Add(args.Event);
            break;
        case BitFullCalendarChangeKind.Delete:
            events.RemoveAll(e => e.Id == args.Event.Id);
            break;
    }

    return InvokeAsync(StateHasChanged);
}
" + eventsCode;

    private readonly string example16RazorCode = @"
<BitChoiceGroup Horizontal
                Label=""View""
                TItem=""BitChoiceGroupOption<BitFullCalendarView>""
                TValue=""BitFullCalendarView""
                @bind-Value=""bindingView"">
    <BitChoiceGroupOption Text=""Day"" Value=""BitFullCalendarView.Day"" />
    <BitChoiceGroupOption Text=""Week"" Value=""BitFullCalendarView.Week"" />
    <BitChoiceGroupOption Text=""Month"" Value=""BitFullCalendarView.Month"" />
    <BitChoiceGroupOption Text=""Year"" Value=""BitFullCalendarView.Year"" IsEnabled=""@(bindingMode == BitFullCalendarMode.Event)"" />
    <BitChoiceGroupOption Text=""Agenda"" Value=""BitFullCalendarView.Agenda"" IsEnabled=""@(bindingMode == BitFullCalendarMode.Event)"" />
</BitChoiceGroup>
<BitChoiceGroup Horizontal
                Label=""Mode""
                TItem=""BitChoiceGroupOption<BitFullCalendarMode>""
                TValue=""BitFullCalendarMode""
                @bind-Value=""bindingMode"">
    <BitChoiceGroupOption Text=""Event"" Value=""BitFullCalendarMode.Event"" />
    <BitChoiceGroupOption Text=""Timeline"" Value=""BitFullCalendarMode.Timeline"" />
</BitChoiceGroup>
<BitButton Variant=""BitVariant.Outline"" OnClick=""() => bindingDate = bindingDate.AddDays(-1)"">Prev day</BitButton>
<BitButton Variant=""BitVariant.Outline"" OnClick=""() => bindingDate = DateTime.Today"">Today</BitButton>
<BitButton Variant=""BitVariant.Outline"" OnClick=""() => bindingDate = bindingDate.AddDays(1)"">Next day</BitButton>

<BitFullCalendar Events=""events""
                 Resources=""resources""
                 @bind-View=""bindingView""
                 @bind-Mode=""bindingMode""
                 @bind-Date=""bindingDate""
                 OnViewChange=""HandleViewChange""
                 OnModeChange=""HandleModeChange""
                 OnDateChange=""HandleDateChange"" />

<BitText>View: <b>@bindingView</b> | Mode: <b>@bindingMode</b> | Date: <b>@bindingDate.ToString(""yyyy-MM-dd"")</b></BitText>
<BitText>Last calendar event: <b>@(bindingLog ?? ""-"")</b></BitText>";
    private readonly string example16CsharpCode = @"
private BitFullCalendarView bindingView = BitFullCalendarView.Week;
private BitFullCalendarMode _bindingMode = BitFullCalendarMode.Event;
private BitFullCalendarMode bindingMode
{
    get => _bindingMode;
    set
    {
        _bindingMode = value;
        // Timeline mode only lays out Day/Week/Month.
        if (value == BitFullCalendarMode.Timeline && bindingView is BitFullCalendarView.Year or BitFullCalendarView.Agenda)
            bindingView = BitFullCalendarView.Week;
    }
}
private DateTime bindingDate = DateTime.Today;
private string? bindingLog;

private void HandleViewChange(BitFullCalendarView view) => bindingLog = $""View changed to {view}"";

private void HandleModeChange(BitFullCalendarMode mode) => bindingLog = $""Mode changed to {mode}"";

private void HandleDateChange(BitFullCalendarDateChangeEventArgs args)
    => bindingLog = $""Range {args.Start:yyyy-MM-dd} → {args.End:yyyy-MM-dd} ({args.View})"";
" + resourcesCode;

    private readonly string example17RazorCode = @"
<BitFullCalendar Events=""events"" IsLoading=""isLoading"" OnDateChange=""LoadRange"" />

<BitText>Loaded: <b>@(loadedRange ?? ""-"")</b></BitText>";
    private readonly string example17CsharpCode = @"
private List<BitFullCalendarEvent> events = [];
private bool isLoading;
private string? loadedRange;
private int loadRequest;

private async Task LoadRange(BitFullCalendarDateChangeEventArgs args)
{
    // The range can move again before this load returns: only the latest request is applied, and only it ends
    // the loading state.
    var request = ++loadRequest;
    isLoading = true;
    await Task.Delay(800); // stands in for the call to your API
    var rangeEvents = CreateEventsBetween(args.Start, args.End);
    if (request != loadRequest) return;

    events = rangeEvents;
    loadedRange = $""{args.Start:yyyy-MM-dd} → {args.End:yyyy-MM-dd} ({events.Count} events)"";
    isLoading = false;
}

private static List<BitFullCalendarEvent> CreateEventsBetween(DateTime start, DateTime end)
{
    string[] titles = [""Client call"", ""Design sync"", ""Hiring panel"", ""Ops review"", ""Customer demo""];
    string[] colors = [""blue"", ""purple"", ""green"", ""orange"", ""red""];
    var events = new List<BitFullCalendarEvent>();
    var n = 0;
    for (var day = start.Date; day <= end.Date; day = day.AddDays(1))
    {
        if (day.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday) continue;
        var i = n++ % titles.Length;
        events.Add(new() { Id = $""load-{day:yyyyMMdd}"", Title = titles[i], StartDate = day.AddHours(9 + i), EndDate = day.AddHours(10 + i), Color = colors[i] });
    }
    return events;
}";

    private readonly string example18RazorCode = @"
<BitToggle @bind-Value=""hideHeader"" Text=""Hide the whole toolbar"" />
<BitButton Variant=""BitVariant.Outline"" OnClick=""() => calendar?.NavigatePrevious()"">Previous</BitButton>
<BitButton Variant=""BitVariant.Outline"" OnClick=""() => calendar?.GoToToday()"">Today</BitButton>
<BitButton Variant=""BitVariant.Outline"" OnClick=""() => calendar?.NavigateNext()"">Next</BitButton>
<BitButton Variant=""BitVariant.Outline"" OnClick=""() => calendar?.ChangeView(BitFullCalendarView.Day)"">Day view</BitButton>
<BitButton Variant=""BitVariant.Outline"" OnClick=""() => calendar?.ChangeView(BitFullCalendarView.Month)"">Month view</BitButton>
<BitButton Variant=""BitVariant.Outline"" OnClick=""ScrollToAfternoon"">Scroll to 14:00</BitButton>
<BitButton Variant=""BitVariant.Outline"" OnClick=""ShowVisibleRange"">Read visible range</BitButton>

<BitFullCalendar @ref=""calendar"" Events=""events"" HideFilters HideSettings HideHeader=""hideHeader"" />

<BitText>Visible range: <b>@(visibleRange ?? ""-"")</b></BitText>";
    private readonly string example18CsharpCode = @"
private bool hideHeader;
private BitFullCalendar? calendar;
private string? visibleRange;

private async Task ScrollToAfternoon()
{
    if (calendar is null) return;
    await calendar.ScrollToTimeAsync(TimeSpan.FromHours(14));
}

private void ShowVisibleRange()
{
    if (calendar is null) return;
    var (start, end) = calendar.GetVisibleRange();
    visibleRange = $""{start:yyyy-MM-dd} → {end:yyyy-MM-dd}"";
}
" + eventsCode;

    private readonly string example19RazorCode = @"
<BitFullCalendar Events=""events"" CultureName=""fa-IR"" Texts=""persianTexts"" />";
    private readonly string example19CsharpCode = @"
private readonly BitFullCalendarTexts persianTexts = new()
{
    // View & mode tabs
    ViewDay = ""روز"",
    ViewWeek = ""هفته"",
    ViewMonth = ""ماه"",
    ViewYear = ""سال"",
    ViewAgenda = ""برنامه"",
    ModeEvent = ""رویدادها"",
    ModeTimeline = ""خط زمانی"",

    // Toolbar
    TodayButton = ""امروز"",
    AddEventButton = ""افزودن رویداد"",
    AddEventHoverHint = ""افزودن رویداد"",
    PreviousButtonTitle = ""قبلی"",
    NextButtonTitle = ""بعدی"",
    PreviousMonthAriaLabel = ""ماه قبل"",
    NextMonthAriaLabel = ""ماه بعد"",
    SettingsButtonTitle = ""تنظیمات"",

    // Filters
    FilterByColorAriaLabel = ""فیلتر رویدادها بر اساس رنگ"",
    FilterByPersonAriaLabel = ""فیلتر رویدادها بر اساس شخص در نمای فعلی"",
    AllColorsOption = ""همه رنگ‌ها"",
    AllPeopleOption = ""همه افراد"",
    UnnamedAttendee = ""(بدون نام)"",

    // Settings panel
    CalendarSettingsLabel = ""تنظیمات تقویم"",
    DotBadgeLabel = ""نشان نقطه‌ای"",
    TwentyFourHourFormatLabel = ""قالب ۲۴ ساعته"",
    DayStartsAtLabel = ""شروع روز از"",
    HourSuffix = ""ساعت"",
    SlotDurationLabel = ""طول بازه"",
    MinuteSuffix = ""دقیقه"",
    AgendaGroupByLabel = ""گروه‌بندی برنامه بر اساس"",
    AgendaGroupByDate = ""تاریخ"",
    AgendaGroupByColor = ""رنگ"",
    StackedEventsLabel = ""چیدمان رویدادهای هم‌پوشان"",
    ShowDayViewCalendarLabel = ""نمایش تقویم در نمای روزانه"",
    ShowWeekNumbersLabel = ""نمایش شماره هفته"",
    ShowCurrentTimeIndicatorLabel = ""نمایش زمان جاری"",
    HighlightBusinessHoursLabel = ""برجسته‌سازی ساعات کاری"",

    // Messages
    WeekMobileWarning = ""نمای هفتگی برای دستگاه‌های کوچک توصیه نمی‌شود. لطفاً از رایانه استفاده کنید یا نمای روزانه را انتخاب کنید."",
    HappeningNowTitle = ""در حال انجام"",
    NoAppointmentsNow = ""در حال حاضر قراری وجود ندارد"",
    EventOverlapMessage = ""این بازه زمانی روی آن منبع قبلاً رزرو شده است."",
    OutOfRangeMessage = ""این تاریخ خارج از بازه مجاز است."",
    OutsideBusinessHoursMessage = ""این زمان خارج از ساعات کاری است."",
    NavLinkDayAriaLabelFormat = ""رفتن به {0}"",
    NavLinkWeekAriaLabelFormat = ""رفتن به هفته {0}"",

    // Search & agenda
    SearchEventsPlaceholder = ""جستجوی رویدادها..."",
    NoEventsFound = ""رویدادی یافت نشد."",
    EventListTitleFormat = ""رویدادهای {0}"",
    EventListCountFormat = ""{0} رویداد"",
    MoreEventsFormat = ""+{0} بیشتر"",
    WeekNumberFormat = ""ه{0}"",
    WeekNumberAriaLabelFormat = ""هفته {0}"",

    // Dialogs
    AddEventDialogTitle = ""افزودن رویداد جدید"",
    EditEventDialogTitle = ""ویرایش رویداد"",
    AddEventDialogSubtitle = ""یک رویداد جدید برای تقویم خود ایجاد کنید."",
    EditEventDialogSubtitle = ""رویداد موجود خود را تغییر دهید."",

    // Buttons
    CloseAriaLabel = ""بستن"",
    CloseButton = ""بستن"",
    CancelButton = ""انصراف"",
    EditButton = ""ویرایش"",
    DeleteButton = ""حذف"",
    CreateEventButton = ""ایجاد رویداد"",
    SaveChangesButton = ""ذخیره تغییرات"",

    // Event form fields
    TitleLabel = ""عنوان"",
    EventTitlePlaceholder = ""عنوان رویداد"",
    AllDayLabel = ""تمام روز"",
    StartDateTimeLabel = ""تاریخ و زمان شروع"",
    EndDateTimeLabel = ""تاریخ و زمان پایان"",
    ColorLabel = ""رنگ"",
    EventColorAriaLabel = ""رنگ رویداد"",
    DescriptionLabel = ""توضیحات"",
    EventDescriptionPlaceholder = ""توضیحات رویداد"",
    AttendeesLabel = ""شرکت‌کنندگان"",
    NoAttendeesText = ""بدون شرکت‌کننده"",
    FirstNamePlaceholder = ""نام"",
    LastNamePlaceholder = ""نام خانوادگی"",
    IdOptionalPlaceholder = ""شناسه (اختیاری)"",
    AddButton = ""افزودن"",
    RemoveAttendeeAriaLabel = ""حذف شرکت‌کننده"",

    // Event details
    StartDateLabel = ""تاریخ شروع"",
    EndDateLabel = ""تاریخ پایان"",
    AtWord = ""در"",

    // Validation
    ValidationTitleRequired = ""عنوان الزامی است"",
    ValidationDescriptionRequired = ""توضیحات الزامی است"",
    ValidationEndAfterStart = ""تاریخ پایان باید بعد از تاریخ شروع باشد"",
    ValidationAttendeeNameRequired = ""نام یا نام خانوادگی الزامی است"",

    // Resources & timeline
    ResizePreviewAriaLabel = ""بازه زمانی جدید"",
    ResourceLabel = ""منبع"",
    ResourceColumnHeader = ""منبع"",
    NoResourceLabel = ""تخصیص‌نیافته"",
    NoResourceOption = ""(هیچ‌کدام)"",
    NoResourcesMessage = ""منبعی برای نمایش وجود ندارد.""
};
" + eventsCode;

    private readonly string example20RazorCode = @"
<BitFullCalendar Events=""events"" AriaLabel=""Team schedule"" />";
    private readonly string example20CsharpCode = eventsCode;

    private readonly string example21RazorCode = @"
<style>
    .cascade-pair {
        gap: 1rem;
        display: grid;
        grid-template-columns: repeat(auto-fit, minmax(min(100%, 22rem), 1fr));
        --bit-FullCalendar-height: 420px;
    }
</style>

<BitParams Parameters=""calendarParams"">
    <div class=""cascade-pair"">
        <BitFullCalendar Events=""events1"" />
        <BitFullCalendar Events=""events2"" Views=""null"" />
    </div>
</BitParams>";
    private readonly string example21CsharpCode = @"
private readonly BitFullCalendarParams[] calendarParams =
[
    new()
    {
        Views = [BitFullCalendarView.Week, BitFullCalendarView.Month],
        DefaultView = BitFullCalendarView.Month,
        HideFilters = true,
        // One object for both calendars: a preference changed in one's settings gear reaches the other.
        Settings = new() { Use24HourFormat = false, BadgeVariant = BitFullCalendarBadgeVariant.Dot }
    }
];

private readonly List<BitFullCalendarEvent> events1 = CreateEvents();
private readonly List<BitFullCalendarEvent> events2 = CreateEvents();
" + eventsCode.Replace("private readonly List<BitFullCalendarEvent> events = CreateEvents();\n\n", "").Replace("private readonly List<BitFullCalendarEvent> events = CreateEvents();\r\n\r\n", "");

    private readonly string example22RazorCode = @"
<BitFullCalendar Events=""events""
                 Style=""border: 2px dashed var(--bit-clr-pri)""
                 Styles=""calendarStyles"" />";
    private readonly string example22CsharpCode = @"
private readonly BitFullCalendarClassStyles calendarStyles = new()
{
    Header = ""background: var(--bit-clr-bg-sec)"",
    Event = ""font-weight: 600"",
    Dialog = ""border-top: 4px solid var(--bit-clr-pri)""
};
" + eventsCode;

    private readonly string example23RazorCode = @"
<BitFullCalendar Dir=""BitDir.Rtl"" Events=""events"" />";
    private readonly string example23CsharpCode = eventsCode;
}
