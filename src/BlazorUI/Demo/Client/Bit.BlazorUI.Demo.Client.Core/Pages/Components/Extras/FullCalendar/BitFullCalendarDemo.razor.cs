namespace Bit.BlazorUI.Demo.Client.Core.Pages.Components.Extras.FullCalendar;

public partial class BitFullCalendarDemo
{
    private readonly List<ComponentParameter> componentParameters =
    [
        new()
        {
            Name = "Events",
            Type = "List<BitFullCalendarEvent>?",
            DefaultValue = "null",
            Description = "List of calendar events to display.",
        },
        new()
        {
            Name = "Culture",
            Type = "CultureInfo?",
            DefaultValue = "CultureInfo.CurrentUICulture",
            Description = "Sets calendar/date rendering and formatting. Do not use with @rendermode=\"InteractiveServer\" — use CultureName instead.",
        },
        new()
        {
            Name = "CultureName",
            Type = "string?",
            DefaultValue = "null",
            Description = "Culture name shortcut (e.g. \"fa-IR\", \"ar-SA\", \"fr-FR\"). Takes precedence over Culture when both are supplied.",
        },
        new()
        {
            Name = "Texts",
            Type = "BitFullCalendarTexts",
            DefaultValue = "new()",
            Description = "Custom UI strings for labels, placeholders, action buttons, aria labels, and validation messages.",
        },
        new()
        {
            Name = "EventColorOptions",
            Type = "IReadOnlyList<BitFullCalendarColorOption>?",
            DefaultValue = "null",
            Description = "Ordered list of event colors shown in pickers, filters, agenda headers, badges, and bullets.",
        },
        new()
        {
            Name = "Resources",
            Type = "IReadOnlyList<BitFullCalendarResource>?",
            DefaultValue = "null",
            Description = "Resources displayed as rows in Timeline mode. Each event's Resource property is matched against the resource Id. The Timeline mode tab is hidden when null or empty.",
        },
        new()
        {
            Name = "InitialMode",
            Type = "BitFullCalendarMode?",
            DefaultValue = "null",
            Description = "Initial layout mode. Event shows day/week/month/year/agenda views. Timeline shows resources × time grid and requires Resources to be non-empty.",
            LinkType = LinkType.Link,
            Href = "#mode-enum",
        },
        new()
        {
            Name = "OnChange",
            Type = "EventCallback<BitFullCalendarChangeEventArgs>",
            DefaultValue = "",
            Description = "Raised when a user adds, edits, or deletes an event (Kind: Add, Edit, Delete; Source: Dialog, Drag, Resize, Delete).",
        },
        new()
        {
            Name = "OnAddClick",
            Type = "EventCallback<BitFullCalendarEvent?>",
            DefaultValue = "",
            Description = "When assigned, the built-in add dialog is suppressed. Receives a draft event with pre-filled dates from the clicked slot.",
        },
        new()
        {
            Name = "OnEventClick",
            Type = "EventCallback<BitFullCalendarEvent>",
            DefaultValue = "",
            Description = "When assigned, the built-in event details dialog is suppressed when an event is clicked. Receives the clicked event.",
        },
        new()
        {
            Name = "OnDateChange",
            Type = "EventCallback<BitFullCalendarDateChangeEventArgs>",
            DefaultValue = "",
            Description = "Raised when the visible date range changes after prev/next/today navigation or a view switch. Payload includes inclusive Start/End and the active View.",
        },
        new()
        {
            Name = "HideFilters",
            Type = "bool",
            DefaultValue = "false",
            Description = "When true, hides the built-in color and attendee filter dropdowns. Consumers provide their own filter UI and pass pre-filtered events.",
        },
        new()
        {
            Name = "HideSettings",
            Type = "bool",
            DefaultValue = "false",
            Description = "When true, hides the built-in settings gear button. Settings can still be driven programmatically through Options.",
        },
        new()
        {
            Name = "Options",
            Type = "BitFullCalendarOptions",
            DefaultValue = "new()",
            Description = "Initial preferences — 12/24-hour time format, badge variant, day start hour, and agenda grouping.",
        },
        new()
        {
            Name = "DayEventTemplate",
            Type = "RenderFragment<BitFullCalendarEvent>?",
            DefaultValue = "null",
            Description = "Replaces the default event card content inside day-view time-grid blocks.",
        },
        new()
        {
            Name = "WeekEventTemplate",
            Type = "RenderFragment<BitFullCalendarEvent>?",
            DefaultValue = "null",
            Description = "Replaces the default event card content inside week-view time-grid blocks.",
        },
        new()
        {
            Name = "MonthEventTemplate",
            Type = "RenderFragment<BitFullCalendarEvent>?",
            DefaultValue = "null",
            Description = "Replaces the default event badge content inside month-view cells.",
        },
        new()
        {
            Name = "TimelineEventTemplate",
            Type = "RenderFragment<BitFullCalendarEvent>?",
            DefaultValue = "null",
            Description = "Replaces the default event card content inside Timeline mode blocks.",
        },
    ];

    private readonly List<ComponentSubEnum> componentSubEnums =
    [
        new()
        {
            Id = "mode-enum",
            Name = "BitFullCalendarMode",
            Description = "Top-level layout mode for the calendar surface.",
            Items =
            [
                new() { Name = "Event", Description = "Day, week, month, year, and agenda views on a date grid.", Value = "0" },
                new() { Name = "Timeline", Description = "Resource-centric layout: resources × time grid.", Value = "1" },
            ]
        },
        new()
        {
            Id = "view-enum",
            Name = "BitFullCalendarView",
            Description = "Active view inside the current mode.",
            Items =
            [
                new() { Name = "Day", Description = "Single-day detailed view.", Value = "0" },
                new() { Name = "Week", Description = "7-day view with hourly time slots.", Value = "1" },
                new() { Name = "Month", Description = "Month grid with multi-day events.", Value = "2" },
                new() { Name = "Year", Description = "12-month overview.", Value = "3" },
                new() { Name = "Agenda", Description = "Searchable list grouped by date or color.", Value = "4" },
            ]
        },
        new()
        {
            Id = "badge-variant-enum",
            Name = "BitFullCalendarBadgeVariant",
            Description = "Badge display style in the month view.",
            Items =
            [
                new() { Name = "Colored", Description = "Colored badge.", Value = "0" },
                new() { Name = "Dot", Description = "Colored dot bullet.", Value = "1" },
            ]
        },
        new()
        {
            Id = "agenda-group-by-enum",
            Name = "BitFullCalendarAgendaGroupBy",
            Description = "How events are grouped in the agenda view.",
            Items =
            [
                new() { Name = "Date", Description = "Group agenda items by date.", Value = "0" },
                new() { Name = "Color", Description = "Group agenda items by color.", Value = "1" },
            ]
        },
        new()
        {
            Id = "change-kind-enum",
            Name = "BitFullCalendarChangeKind",
            Description = "Identifies the kind of change applied to a calendar event.",
            Items =
            [
                new() { Name = "Add", Description = "An event was added.", Value = "0" },
                new() { Name = "Edit", Description = "An event was edited.", Value = "1" },
                new() { Name = "Delete", Description = "An event was deleted.", Value = "2" },
            ]
        },
        new()
        {
            Id = "change-source-enum",
            Name = "BitFullCalendarChangeSource",
            Description = "Identifies where a calendar event change originated from in the UI.",
            Items =
            [
                new() { Name = "Dialog", Description = "From the add/edit dialog.", Value = "0" },
                new() { Name = "Drag", Description = "From a drag-and-drop move.", Value = "1" },
                new() { Name = "Resize", Description = "From resizing an event block.", Value = "2" },
                new() { Name = "Delete", Description = "From the delete action.", Value = "3" },
            ]
        },
    ];



    private readonly List<BitFullCalendarEvent> basicEvents = CreateEvents();
    private readonly List<BitFullCalendarEvent> themeEvents = CreateEvents();
    private readonly List<BitFullCalendarEvent> optionsEvents = CreateEvents();
    private readonly List<BitFullCalendarEvent> templateEvents = CreateEvents();
    private readonly List<BitFullCalendarEvent> changeEvents = CreateEvents();
    private readonly List<BitFullCalendarEvent> localizationEvents = CreateEvents();

    private readonly BitFullCalendarOptions options = new()
    {
        Use24HourFormat = false,
        StartOfDayHour = 7,
        BadgeVariant = BitFullCalendarBadgeVariant.Dot
    };

    private readonly BitFullCalendarTexts persianTexts = new()
    {
        AddEventButton = "افزودن رویداد",
        AddEventDialogTitle = "افزودن رویداد جدید",
        StartDateTimeLabel = "تاریخ و زمان شروع",
        EndDateTimeLabel = "تاریخ و زمان پایان",
        CreateEventButton = "ایجاد رویداد"
    };

    private readonly List<BitFullCalendarResource> resources =
    [
        new() { Id = "room-bay", Title = "HQ - Bay Wing", Subtitle = "Headquarters" },
        new() { Id = "room-garden", Title = "The Garden", Subtitle = "Headquarters" },
        new() { Id = "room-war", Title = "War Room (B1)", Subtitle = "Basement" },
    ];

    private readonly List<BitFullCalendarEvent> resourceEvents = CreateResourceEvents();

    private string? lastChange;

    private Task HandleChange(BitFullCalendarChangeEventArgs args)
    {
        lastChange = $"{args.Kind} ({args.Source}): {args.Event.Title}";
        return InvokeAsync(StateHasChanged);
    }

    private static List<BitFullCalendarEvent> CreateEvents()
    {
        var today = DateTime.Today;
        var id = 0;
        return
        [
            new() { Id = (++id).ToString(), Title = "Team Standup", Description = "Daily sync with engineering.", StartDate = today.AddHours(9), EndDate = today.AddHours(9).AddMinutes(45), Color = "blue" },
            new() { Id = (++id).ToString(), Title = "Design Review", Description = "Dashboard mockups v2.", StartDate = today.AddHours(10), EndDate = today.AddHours(11), Color = "purple" },
            new() { Id = (++id).ToString(), Title = "1:1 with Manager", Description = "Career and sprint check-in.", StartDate = today.AddHours(10).AddMinutes(30), EndDate = today.AddHours(11).AddMinutes(15), Color = "yellow" },
            new() { Id = (++id).ToString(), Title = "Lunch with Client", Description = "Q3 roadmap discussion.", StartDate = today.AddHours(12), EndDate = today.AddHours(13).AddMinutes(30), Color = "green" },
            new() { Id = (++id).ToString(), Title = "Sprint Planning", Description = "Next sprint goals and capacity.", StartDate = today.AddHours(14), EndDate = today.AddHours(15).AddMinutes(30), Color = "orange" },
            new() { Id = (++id).ToString(), Title = "Code Review", Description = "Auth module PRs.", StartDate = today.AddHours(16), EndDate = today.AddHours(17), Color = "red" },
            new() { Id = (++id).ToString(), Title = "Tech Conference", Description = "Keynotes and workshops.", StartDate = today.AddDays(1).AddHours(9), EndDate = today.AddDays(3).AddHours(17), Color = "blue" },
            new() { Id = (++id).ToString(), Title = "Client Onboarding", Description = "Platform walkthrough.", StartDate = today.AddDays(1).AddHours(10), EndDate = today.AddDays(1).AddHours(11).AddMinutes(30), Color = "yellow" },
            new() { Id = (++id).ToString(), Title = "Architecture Review", Description = "Migration plan.", StartDate = today.AddDays(2).AddHours(14), EndDate = today.AddDays(2).AddHours(16), Color = "red" },
            new() { Id = (++id).ToString(), Title = "Company Retreat", Description = "Strategy and team building.", StartDate = today.AddDays(5), EndDate = today.AddDays(7).AddHours(16), Color = "purple" },
            new() { Id = (++id).ToString(), Title = "Quarterly Review", Description = "Company-wide QBR.", StartDate = today.AddDays(-3).AddHours(10), EndDate = today.AddDays(-3).AddHours(12), Color = "red" },
            new() { Id = (++id).ToString(), Title = "Product Demo", Description = "Stakeholder walkthrough.", StartDate = today.AddDays(-2).AddHours(14), EndDate = today.AddDays(-2).AddHours(15), Color = "orange" },
        ];
    }

    private static List<BitFullCalendarEvent> CreateResourceEvents()
    {
        var today = DateTime.Today;
        var id = 100;
        return
        [
            new() { Id = (++id).ToString(), Title = "Design Review", StartDate = today.AddHours(10), EndDate = today.AddHours(11), Resource = "room-bay", Color = "purple" },
            new() { Id = (++id).ToString(), Title = "Standup", StartDate = today.AddHours(9), EndDate = today.AddHours(9).AddMinutes(30), Resource = "room-garden", Color = "blue" },
            new() { Id = (++id).ToString(), Title = "Incident Bridge", StartDate = today.AddHours(13), EndDate = today.AddHours(15), Resource = "room-war", Color = "red" },
            new() { Id = (++id).ToString(), Title = "Workshop", StartDate = today.AddHours(14), EndDate = today.AddHours(16), Resource = "room-bay", Color = "orange" },
        ];
    }



    private readonly string example1RazorCode = @"
<BitFullCalendar Events=""events"" />

@code {
    private readonly List<BitFullCalendarEvent> events = [
        new() { Id = ""1"", Title = ""Team Standup"", StartDate = DateTime.Today.AddHours(9), EndDate = DateTime.Today.AddHours(9).AddMinutes(45), Color = ""blue"" },
        new() { Id = ""2"", Title = ""Design Review"", StartDate = DateTime.Today.AddHours(10), EndDate = DateTime.Today.AddHours(11), Color = ""purple"" },
    ];
}";

    private readonly string example2RazorCode = @"
<BitFullCalendar Events=""events"" />";

    private readonly string example3RazorCode = @"
<BitFullCalendar Events=""events"" Options=""options"" />

@code {
    private readonly BitFullCalendarOptions options = new()
    {
        Use24HourFormat = false,
        StartOfDayHour = 7,
        BadgeVariant = BitFullCalendarBadgeVariant.Dot
    };
}";

    private readonly string example4RazorCode = @"
<BitFullCalendar Events=""events""
                 DayEventTemplate=""EventCard""
                 WeekEventTemplate=""EventCard""
                 MonthEventTemplate=""MonthBadge"" />

@code {
    private RenderFragment<BitFullCalendarEvent> EventCard => ev =>
        @<div style=""display:flex;flex-direction:column;gap:2px"">
            <strong>@ev.Title</strong>
            @if (!string.IsNullOrWhiteSpace(ev.Description))
            {
                <span style=""font-size:11px;opacity:.8"">@ev.Description</span>
            }
        </div>;

    private RenderFragment<BitFullCalendarEvent> MonthBadge => ev => @<span>📌 @ev.Title</span>;
}";

    private readonly string example5RazorCode = @"
<BitFullCalendar Events=""events""
                 Resources=""resources""
                 InitialMode=""BitFullCalendarMode.Timeline"" />

@code {
    private readonly List<BitFullCalendarResource> resources = [
        new() { Id = ""room-bay"", Title = ""HQ - Bay Wing"", Subtitle = ""Headquarters"" },
        new() { Id = ""room-garden"", Title = ""The Garden"", Subtitle = ""Headquarters"" },
        new() { Id = ""room-war"", Title = ""War Room (B1)"", Subtitle = ""Basement"" },
    ];

    private readonly List<BitFullCalendarEvent> events = [
        new() { Id = ""1"", Title = ""Design Review"", StartDate = DateTime.Today.AddHours(10), EndDate = DateTime.Today.AddHours(11), Resource = ""room-bay"", Color = ""purple"" },
    ];
}";

    private readonly string example6RazorCode = @"
<BitFullCalendar Events=""events"" OnChange=""HandleChange"" />

<div>Last change: @lastChange</div>

@code {
    private string? lastChange;

    private Task HandleChange(BitFullCalendarChangeEventArgs args)
    {
        lastChange = $""{args.Kind} ({args.Source}): {args.Event.Title}"";
        return Task.CompletedTask;
    }
}";

    private readonly string example7RazorCode = @"
<BitFullCalendar Events=""events"" CultureName=""fa-IR"" Texts=""persianTexts"" />

@code {
    private readonly BitFullCalendarTexts persianTexts = new()
    {
        AddEventButton = ""افزودن رویداد"",
        AddEventDialogTitle = ""افزودن رویداد جدید"",
        StartDateTimeLabel = ""تاریخ و زمان شروع"",
        EndDateTimeLabel = ""تاریخ و زمان پایان"",
        CreateEventButton = ""ایجاد رویداد""
    };
}";

    private readonly string example8RazorCode = @"
<BitFullCalendar Events=""events"" HideFilters HideSettings />";
}
