namespace Bit.BlazorUI.Demo.Client.Core.Pages.Components.Extras.FullCalendar;

public partial class BitFullCalendarDemo
{
    private readonly List<ComponentParameter> componentParameters =
    [
        new()
        {
            Name = "AgendaEventTemplate",
            Type = "RenderFragment<BitFullCalendarEvent>?",
            DefaultValue = "null",
            Description = "Replaces the content of an event row in the agenda view and in the event lists (\"+N more\", a year-view day). The row stays the button that opens the event.",
            LinkType = LinkType.Link,
            Href = "#event-class",
        },
        new()
        {
            Name = "Classes",
            Type = "BitFullCalendarClassStyles?",
            DefaultValue = "null",
            Description = "Custom CSS classes for the different parts of the calendar.",
            LinkType = LinkType.Link,
            Href = "#class-styles",
        },
        new()
        {
            Name = "Culture",
            Type = "CultureInfo?",
            DefaultValue = "CultureInfo.CurrentUICulture",
            Description = "Sets calendar/date rendering and formatting. Do not use with @rendermode=\"InteractiveServer\" - use CultureName instead.",
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
            Name = "Date",
            Type = "DateTime",
            DefaultValue = "DateTime.Today",
            Description = "The currently selected (anchor) date of the calendar that determines the visible date range. (two-way bound)",
        },
        new()
        {
            Name = "DayEventTemplate",
            Type = "RenderFragment<BitFullCalendarEvent>?",
            DefaultValue = "null",
            Description = "Replaces the default event card content inside day-view time-grid blocks.",
            LinkType = LinkType.Link,
            Href = "#event-class",
        },
        new()
        {
            Name = "DefaultDate",
            Type = "DateTime?",
            DefaultValue = "null",
            Description = "The default selected date used initially when the Date parameter is not set. Determines the date range shown on first render.",
        },
        new()
        {
            Name = "DefaultMode",
            Type = "BitFullCalendarMode?",
            DefaultValue = "null",
            Description = "The default layout mode used initially when the Mode parameter is not set. Event shows the day/week/month/year/agenda views. Timeline shows a resources × time grid (requires Resources to be non-empty) and supports only the day, week, and month layouts - Year and Agenda fall back to the week layout in Timeline mode.",
            LinkType = LinkType.Link,
            Href = "#mode-enum",
        },
        new()
        {
            Name = "DefaultView",
            Type = "BitFullCalendarView?",
            DefaultValue = "null",
            Description = "The default view used initially when the View parameter is not set. In Event mode any of Day, Week, Month, Year, or Agenda apply; in Timeline mode only Day, Week, and Month are supported (Year and Agenda fall back to the week layout).",
            LinkType = LinkType.Link,
            Href = "#view-enum",
        },
        new()
        {
            Name = "EventColorOptions",
            Type = "IReadOnlyList<BitFullCalendarColorOption>?",
            DefaultValue = "null",
            Description = "Ordered list of event colors shown in pickers, filters, agenda headers, badges, and bullets.",
            LinkType = LinkType.Link,
            Href = "#color-option-class",
        },
        new()
        {
            Name = "EventDetailsTemplate",
            Type = "RenderFragment<BitFullCalendarEvent>?",
            DefaultValue = "null",
            Description = "Extra content for the built-in event details dialog, under the built-in rows.",
            LinkType = LinkType.Link,
            Href = "#event-class",
        },
        new()
        {
            Name = "EventEditorTemplate",
            Type = "RenderFragment<BitFullCalendarEvent>?",
            DefaultValue = "null",
            Description = "Extra fields for the built-in add/edit dialog. It receives the draft, whose Data the save commits; assign a new Data rather than changing the object it starts with, so Cancel leaves the event as it was.",
            LinkType = LinkType.Link,
            Href = "#event-class",
        },
        new()
        {
            Name = "Events",
            Type = "List<BitFullCalendarEvent>?",
            DefaultValue = "null",
            Description = "List of calendar events to display.",
            LinkType = LinkType.Link,
            Href = "#event-class",
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
            Name = "HideHeader",
            Type = "bool",
            DefaultValue = "false",
            Description = "When true, the whole toolbar is removed - navigation, mode and view tabs, filters, the Add Event button, and the settings gear - so the calendar can be driven entirely from the consumer's own chrome through the bound View/Mode/Date parameters or the navigation methods.",
        },
        new()
        {
            Name = "HideSettings",
            Type = "bool",
            DefaultValue = "false",
            Description = "When true, hides the built-in settings gear button. Settings can still be driven programmatically through the Settings parameter.",
        },
        new()
        {
            Name = "IsLoading",
            Type = "bool",
            DefaultValue = "false",
            Description = "Runs an indeterminate progress bar across the top of the body and marks it busy for assistive technology, e.g. while the events of a range reported by OnDateChange are fetched.",
        },
        new()
        {
            Name = "MaxDate",
            Type = "DateTime?",
            DefaultValue = "null",
            Description = "The latest date the calendar can navigate to and display. Navigation past it is refused, the next button is disabled, and a bound Date beyond it is pulled back.",
        },
        new()
        {
            Name = "MinDate",
            Type = "DateTime?",
            DefaultValue = "null",
            Description = "The earliest date the calendar can navigate to and display. Navigation before it is refused, the previous button is disabled, and a bound Date before it is pulled forward. A window whose bounds are inverted is ignored.",
        },
        new()
        {
            Name = "Mode",
            Type = "BitFullCalendarMode",
            DefaultValue = "BitFullCalendarMode.Event",
            Description = "The currently active layout mode of the calendar (Event or Timeline). Timeline requires Resources to be non-empty and only supports the Day, Week, and Month views (Year and Agenda fall back to the week layout). (two-way bound)",
            LinkType = LinkType.Link,
            Href = "#mode-enum",
        },
        new()
        {
            Name = "MonthCellTemplate",
            Type = "RenderFragment<BitFullCalendarCell>?",
            DefaultValue = "null",
            Description = "Extra content for each day of the month grid, under the day number and above the events - a holiday, a price, an availability count.",
            LinkType = LinkType.Link,
            Href = "#cell-class",
        },
        new()
        {
            Name = "MonthEventTemplate",
            Type = "RenderFragment<BitFullCalendarEvent>?",
            DefaultValue = "null",
            Description = "Replaces the default event badge content inside month-view cells.",
            LinkType = LinkType.Link,
            Href = "#event-class",
        },
        new()
        {
            Name = "OnAddClick",
            Type = "EventCallback<BitFullCalendarEvent?>",
            DefaultValue = "",
            Description = "When assigned, the built-in add dialog is suppressed. Receives a draft event with the start/end dates pre-filled from the calendar's selected date and configured start hour (or the clicked slot when adding from a time grid).",
            LinkType = LinkType.Link,
            Href = "#event-class",
        },
        new()
        {
            Name = "OnChange",
            Type = "EventCallback<BitFullCalendarChangeEventArgs>",
            DefaultValue = "",
            Description = "Raised when a user adds, edits, or deletes an event (Kind: Add, Edit, Delete; Source: Dialog, Drag, Resize).",
            LinkType = LinkType.Link,
            Href = "#change-args-class",
        },
        new()
        {
            Name = "OnChanging",
            Type = "EventCallback<BitFullCalendarChangingEventArgs>",
            DefaultValue = "",
            Description = "Raised before a user-driven add, edit or delete is committed (after the built-in rules pass). Set Cancel to refuse it: the event goes back, no OnChange is raised, and the add/edit dialog stays open.",
            LinkType = LinkType.Link,
            Href = "#changing-args-class",
        },
        new()
        {
            Name = "OnDateChange",
            Type = "EventCallback<BitFullCalendarDateChangeEventArgs>",
            DefaultValue = "",
            Description = "Raised on the first render - as the calendar initializes, and awaited, when it is prerendered, so the events fetched for it are in the prerendered page - and whenever the visible range moves (navigation, a view switch). Carries the inclusive Start/End and the active View - fetch that range here.",
            LinkType = LinkType.Link,
            Href = "#date-change-args-class",
        },
        new()
        {
            Name = "OnEventClick",
            Type = "EventCallback<BitFullCalendarEvent>",
            DefaultValue = "",
            Description = "When assigned, the built-in event details dialog is suppressed when an event is clicked. Receives the clicked event.",
            LinkType = LinkType.Link,
            Href = "#event-class",
        },
        new()
        {
            Name = "OnModeChange",
            Type = "EventCallback<BitFullCalendarMode>",
            DefaultValue = "",
            Description = "Raised when the active layout mode changes (switching between the Event and Timeline tabs).",
            LinkType = LinkType.Link,
            Href = "#mode-enum",
        },
        new()
        {
            Name = "OnRefused",
            Type = "EventCallback<BitFullCalendarChangeRefusal>",
            DefaultValue = "",
            Description = "Raised when the calendar refuses a user-driven change: an overlap while Settings.AllowEventOverlap is false, a target outside the MinDate/MaxDate window or the business hours (Settings.RestrictToBusinessHours), or an event marked IsReadOnly.",
            LinkType = LinkType.Link,
            Href = "#change-refusal-enum",
        },
        new()
        {
            Name = "OnSettingsChange",
            Type = "EventCallback<BitFullCalendarSettings>",
            DefaultValue = "",
            Description = "Raised after the user changes a preference from the built-in settings panel, once the new values have been written back onto the Settings instance (which the callback receives).",
            LinkType = LinkType.Link,
            Href = "#settings-class",
        },
        new()
        {
            Name = "OnViewChange",
            Type = "EventCallback<BitFullCalendarView>",
            DefaultValue = "",
            Description = "Raised when the active view changes (selecting a view tab or navigating from the year overview into a month).",
            LinkType = LinkType.Link,
            Href = "#view-enum",
        },
        new()
        {
            Name = "ReadOnly",
            Type = "bool",
            DefaultValue = "false",
            Description = "When true, the calendar becomes presentation-only: the add button and per-cell add affordances are hidden, drag-and-drop and resizing are disabled, and the edit/delete actions are removed from the event details dialog. Navigation, view/mode switching, filtering, and reading event details keep working.",
        },
        new()
        {
            Name = "ResourceTemplate",
            Type = "RenderFragment<BitFullCalendarResource>?",
            DefaultValue = "null",
            Description = "Replaces the title and subtitle in the header cell of a timeline resource row.",
            LinkType = LinkType.Link,
            Href = "#resource-class",
        },
        new()
        {
            Name = "Resources",
            Type = "IReadOnlyList<BitFullCalendarResource>?",
            DefaultValue = "null",
            Description = "Resources displayed as rows in Timeline mode. Each event's Resource property is matched against the resource Id. The Timeline mode tab is hidden when null or empty.",
            LinkType = LinkType.Link,
            Href = "#resource-class",
        },
        new()
        {
            Name = "Settings",
            Type = "BitFullCalendarSettings",
            DefaultValue = "new()",
            Description = "The preferences: time format, hour window, slot duration, hidden days, week numbers, business hours, booking rules, badge style, agenda grouping and event layout. Changes the user makes from the settings gear are written back onto it.",
            LinkType = LinkType.Link,
            Href = "#settings-class",
        },
        new()
        {
            Name = "Styles",
            Type = "BitFullCalendarClassStyles?",
            DefaultValue = "null",
            Description = "Custom CSS styles for the different parts of the calendar.",
            LinkType = LinkType.Link,
            Href = "#class-styles",
        },
        new()
        {
            Name = "Texts",
            Type = "BitFullCalendarTexts",
            DefaultValue = "new()",
            Description = "Custom UI strings for labels, placeholders, action buttons, aria labels, and validation messages.",
            LinkType = LinkType.Link,
            Href = "#texts-class",
        },
        new()
        {
            Name = "TimeProvider",
            Type = "TimeProvider?",
            DefaultValue = "null",
            Description = "The clock \"now\" and \"today\" are read from: the today marker, the current-time line, the Today button and the opening date. Defaults to TimeProvider.System, which on Blazor Server is the server's clock - pass one whose LocalTimeZone is the user's, or a fixed one for tests.",
        },
        new()
        {
            Name = "TimelineEventTemplate",
            Type = "RenderFragment<BitFullCalendarEvent>?",
            DefaultValue = "null",
            Description = "Replaces the default event card content inside Timeline mode blocks.",
            LinkType = LinkType.Link,
            Href = "#event-class",
        },
        new()
        {
            Name = "View",
            Type = "BitFullCalendarView",
            DefaultValue = "BitFullCalendarView.Month",
            Description = "The currently active view of the calendar (Day, Week, Month, Year, Agenda). In Timeline mode only Day, Week, and Month are supported (Year and Agenda fall back to the week layout). (two-way bound)",
            LinkType = LinkType.Link,
            Href = "#view-enum",
        },
        new()
        {
            Name = "Views",
            Type = "IReadOnlyList<BitFullCalendarView>?",
            DefaultValue = "null",
            Description = "The views the calendar offers, in the order the view tabs render them. When null or empty, every view (Day, Week, Month, Year, Agenda) is offered in that order; unknown and repeated entries are ignored. Excluded views are unreachable - the tabs omit them and View, DefaultView, and the indirect navigation paths are clamped into the list. The tab strip is hidden when a single view is left, and Timeline mode is unavailable when none of Day, Week, or Month is listed.",
            LinkType = LinkType.Link,
            Href = "#view-enum",
        },
        new()
        {
            Name = "WeekEventTemplate",
            Type = "RenderFragment<BitFullCalendarEvent>?",
            DefaultValue = "null",
            Description = "Replaces the default event card content inside week-view time-grid blocks.",
            LinkType = LinkType.Link,
            Href = "#event-class",
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
                new() { Name = "Timeline", Description = "Resource-centric layout (resources × time grid); requires Resources. Supports only the day, week, and month layouts.", Value = "1" },
            ]
        },
        new()
        {
            Id = "view-enum",
            Name = "BitFullCalendarView",
            Description = "Active view inside the current mode. In Timeline mode only Day, Week, and Month are supported; Year and Agenda fall back to the week layout.",
            Items =
            [
                new() { Name = "Day", Description = "Single-day detailed view.", Value = "0" },
                new() { Name = "Week", Description = "7-day view with hourly time slots.", Value = "1" },
                new() { Name = "Month", Description = "Month grid with multi-day events.", Value = "2" },
                new() { Name = "Year", Description = "12-month overview. Event mode only - falls back to the week layout in Timeline mode.", Value = "3" },
                new() { Name = "Agenda", Description = "Searchable list grouped by date or color. Event mode only - falls back to the week layout in Timeline mode.", Value = "4" },
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
            Id = "event-layout-enum",
            Name = "BitFullCalendarEventLayout",
            Description = "How overlapping event cards are positioned in the day and week views.",
            Items =
            [
                new() { Name = "Overlap", Description = "Overlapping cards cascade on top of each other, each offset to the right and extending to the column edge.", Value = "0" },
                new() { Name = "Stack", Description = "Overlapping cards are placed side by side in equal-width columns with no overlap.", Value = "1" },
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
            ]
        },
        new()
        {
            Id = "recurrence-frequency-enum",
            Name = "BitFullCalendarRecurrenceFrequency",
            Description = "How often a recurring event repeats. The step between two occurrences is this unit multiplied by BitFullCalendarRecurrence.Interval.",
            Items =
            [
                new() { Name = "Daily", Description = "Repeats every Interval days.", Value = "0" },
                new() { Name = "Weekly", Description = "Repeats every Interval weeks, on the weekdays listed in DaysOfWeek (the start date's own weekday when none are listed).", Value = "1" },
                new() { Name = "Monthly", Description = "Repeats every Interval months on the start date's day of the month. A month too short for that day is skipped rather than shifted. With WeekOfMonth set it repeats on a weekday of the month instead (the third Tuesday).", Value = "2" },
                new() { Name = "Yearly", Description = "Repeats every Interval years on the start date's month and day. A 29 February series only occurs in leap years. With WeekOfMonth set it repeats on a weekday of the start date's month instead (the fourth Thursday of November).", Value = "3" },
            ]
        },
        new()
        {
            Id = "week-of-month-enum",
            Name = "BitFullCalendarWeekOfMonth",
            Description = "Which occurrence of a weekday within its month a monthly or yearly recurrence lands on - the \"third\" in \"the third Tuesday of every month\".",
            Items =
            [
                new() { Name = "First", Description = "The first such weekday of the month (days 1 to 7).", Value = "0" },
                new() { Name = "Second", Description = "The second such weekday of the month (days 8 to 14).", Value = "1" },
                new() { Name = "Third", Description = "The third such weekday of the month (days 15 to 21).", Value = "2" },
                new() { Name = "Fourth", Description = "The fourth such weekday of the month (days 22 to 28).", Value = "3" },
                new() { Name = "Last", Description = "The last such weekday of the month, whether that is its fourth or its fifth.", Value = "4" },
            ]
        },
        new()
        {
            Id = "change-refusal-enum",
            Name = "BitFullCalendarChangeRefusal",
            Description = "Why the calendar refused to commit a user-driven change (a drop, a resize, or a dialog save).",
            Items =
            [
                new() { Name = "None", Description = "The change was applied (or there was nothing to apply).", Value = "0" },
                new() { Name = "ReadOnly", Description = "The calendar, the single event, or the edit permissions (Settings.AllowEdit / AllowDrag / AllowResize) do not allow the change, so nothing was changed.", Value = "1" },
                new() { Name = "Overlap", Description = "The resulting range would overlap another event on the same resource while Settings.AllowEventOverlap is false.", Value = "2" },
                new() { Name = "OutOfRange", Description = "The resulting range falls outside the MinDate/MaxDate window the calendar is allowed to show.", Value = "3" },
                new() { Name = "OutsideBusinessHours", Description = "The resulting range is not fully contained in the business hours while Settings.RestrictToBusinessHours is true.", Value = "4" },
                new() { Name = "Blocked", Description = "The resulting range would overlap an event marked IsBlocking on the same resource (or one blocking every resource).", Value = "5" },
            ]
        },
    ];



    private readonly List<ComponentSubClass> componentSubClasses =
    [
        new()
        {
            Id = "class-styles",
            Title = "BitFullCalendarClassStyles",
            Description = "Custom CSS classes/styles for the different parts of the BitFullCalendar.",
            Parameters =
            [
                new() { Name = "Root", Type = "string?", DefaultValue = "null", Description = "Custom CSS classes/styles for the root element of the BitFullCalendar." },
                new() { Name = "Header", Type = "string?", DefaultValue = "null", Description = "Custom CSS classes/styles for the toolbar." },
                new() { Name = "Body", Type = "string?", DefaultValue = "null", Description = "Custom CSS classes/styles for the body that renders the active view." },
                new() { Name = "Event", Type = "string?", DefaultValue = "null", Description = "Custom CSS classes/styles for every event surface (month badges, day/week and timeline blocks, agenda rows), beside the event's own CssClass." },
                new() { Name = "Dialog", Type = "string?", DefaultValue = "null", Description = "Custom CSS classes/styles for the built-in dialogs." },
            ]
        },
        new()
        {
            Id = "event-class",
            Title = "BitFullCalendarEvent",
            Description = "Represents a single calendar event rendered across the day, week, month, year, agenda, and timeline views.",
            Parameters =
            [
                new() { Name = "Id", Type = "string", DefaultValue = "string.Empty", Description = "Unique identifier of the event." },
                new() { Name = "Title", Type = "string", DefaultValue = "string.Empty", Description = "Event title shown on the event card, badge, and dialogs." },
                new() { Name = "Description", Type = "string", DefaultValue = "string.Empty", Description = "Event description shown in the details and add/edit dialogs." },
                new() { Name = "StartDate", Type = "DateTime", DefaultValue = "", Description = "Start date and time of the event." },
                new() { Name = "EndDate", Type = "DateTime", DefaultValue = "", Description = "End date and time of the event." },
                new() { Name = "Color", Type = "string", DefaultValue = "BitFullCalendarColorScheme.FallbackColorId", Description = "Id of a BitFullCalendarColorOption from the configured palette, or - when no option matches - a CSS color of its own (a hex or a color function such as rgb(), hsl(), oklch(), var()). Anything else is drawn in --bit-FullCalendar-event-color.", LinkType = LinkType.Link, Href = "#color-option-class" },
                new() { Name = "Attendees", Type = "List<BitFullCalendarAttendee>", DefaultValue = "[]", Description = "People attending the event.", LinkType = LinkType.Link, Href = "#attendee-class" },
                new() { Name = "Resource", Type = "string?", DefaultValue = "null", Description = "Optional resource identifier linking this event to a BitFullCalendarResource. Used by the timeline view to place the event on the matching resource row. null or empty means the event is unassigned.", LinkType = LinkType.Link, Href = "#resource-class" },
                new() { Name = "IsAllDay", Type = "bool", DefaultValue = "false", Description = "Marks the event as lasting the whole day (or span of days) rather than a time range. It renders in the all-day row above the day and week time grids and its badge shows no clock time." },
                new() { Name = "IsReadOnly", Type = "bool", DefaultValue = "false", Description = "Locks this single event: it cannot be dragged, resized, edited, or deleted even while the calendar itself is editable. Reading its details keeps working." },
                new() { Name = "IsBackground", Type = "bool", DefaultValue = "false", Description = "Draws the event as a shaded band behind the grid (a lunch break, a holiday) instead of a card: never clicked, focused, dragged or listed, and not an overlap. The time grids and the timeline draw its range (on its Resource's row, or every row); the month grid tints the days it covers whole. Its title is spoken with the slots it covers." },
                new() { Name = "IsBlocking", Type = "bool", DefaultValue = "false", Description = "Keeps every other event out of this event's range - an add, move or resize overlapping it is refused as Blocked, even while overlaps are allowed. With a Resource it blocks that resource only. A blocking background band is hatched." },
                new() { Name = "CssClass", Type = "string?", DefaultValue = "null", Description = "Extra CSS class(es) applied to every element that renders this event - the month badge, the day/week block, the timeline block, and the agenda row." },
                new() { Name = "Recurrence", Type = "BitFullCalendarRecurrence?", DefaultValue = "null", Description = "Repeat rule that turns this event into a series master. The calendar expands it into occurrences across the visible range; the master itself is never rendered once it has one.", LinkType = LinkType.Link, Href = "#recurrence-class" },
                new() { Name = "SeriesId", Type = "string?", DefaultValue = "null", Description = "Set on a generated occurrence to the Id of the series master it came from. Null on every event a consumer supplied." },
                new() { Name = "OccurrenceDate", Type = "DateTime?", DefaultValue = "null", Description = "Set on a generated occurrence to the date it falls on, so a click handler can tell which one of a series the user picked." },
                new() { Name = "IsOccurrence", Type = "bool", DefaultValue = "", Description = "Read-only. True when this event was generated by expanding a recurrence rule." },
                new() { Name = "IsSingleDay", Type = "bool", DefaultValue = "", Description = "Read-only. True when the event starts and ends on the same date." },
                new() { Name = "IsMultiDay", Type = "bool", DefaultValue = "", Description = "Read-only. True when the event spans more than one date." },
                new() { Name = "IsAllDayOrMultiDay", Type = "bool", DefaultValue = "", Description = "Read-only. True when the event belongs in the all-day row: either it is marked IsAllDay or it spans more than one date." },
                new() { Name = "Duration", Type = "TimeSpan", DefaultValue = "", Description = "Read-only. The difference between EndDate and StartDate." },
                new() { Name = "Data", Type = "object?", DefaultValue = "null", Description = "Optional consumer-defined payload available to templates and click handlers." },
            ]
        },
        new()
        {
            Id = "recurrence-class",
            Title = "BitFullCalendarRecurrence",
            Description = "The repeat rule of a recurring event. The event it sits on is the series master: it defines the first occurrence's date, time, and length, and every later occurrence repeats that same length. Recurrence is computed on the Gregorian calendar, independent of the culture the calendar renders in.",
            Parameters =
            [
                new() { Name = "Frequency", Type = "BitFullCalendarRecurrenceFrequency", DefaultValue = "BitFullCalendarRecurrenceFrequency.Daily", Description = "How often the event repeats.", LinkType = LinkType.Link, Href = "#recurrence-frequency-enum" },
                new() { Name = "Interval", Type = "int", DefaultValue = "1", Description = "Number of frequency units between two occurrences - 2 with a weekly frequency means every other week. Values below 1 are treated as 1." },
                new() { Name = "DaysOfWeek", Type = "IReadOnlyList<DayOfWeek>?", DefaultValue = "null", Description = "Weekdays the series occurs on: every week for a weekly series, or on that week of the month for a monthly or yearly series with a WeekOfMonth. When null or empty the series follows the start date's own weekday. Ignored by the other frequencies." },
                new() { Name = "WeekOfMonth", Type = "BitFullCalendarWeekOfMonth?", DefaultValue = "null", Description = "Places a monthly or yearly series on a weekday of the month instead of the start date's day number - Third with DaysOfWeek set to Tuesday is the third Tuesday. A yearly series stays in the start date's month. Ignored by the other frequencies.", LinkType = LinkType.Link, Href = "#week-of-month-enum" },
                new() { Name = "Count", Type = "int?", DefaultValue = "null", Description = "Total number of occurrences the pattern produces, counting the first. Null leaves the series open-ended unless Until closes it; when both are set, whichever ends the series first wins." },
                new() { Name = "Until", Type = "DateTime?", DefaultValue = "null", Description = "Last date the pattern may occur on (inclusive). Null leaves the series open-ended unless Count closes it." },
                new() { Name = "ExceptionDates", Type = "IReadOnlyList<DateTime>?", DefaultValue = "null", Description = "Dates the series skips - a cancelled occurrence, a holiday. Only the date part is compared, a skipped date still counts against Count, and it also removes an AdditionalDates entry on the same date." },
                new() { Name = "AdditionalDates", Type = "IReadOnlyList<DateTime>?", DefaultValue = "null", Description = "Dates the series also occurs on outside its pattern - a make-up session. Each repeats the master's time of day and length, neither counts against Count nor stops at Until, and a date the pattern already produces is not doubled." },
            ]
        },
        new()
        {
            Id = "attendee-class",
            Title = "BitFullCalendarAttendee",
            Description = "Represents a person attending an event, shown in the event details and add/edit dialogs.",
            Parameters =
            [
                new() { Name = "FirstName", Type = "string", DefaultValue = "string.Empty", Description = "First name of the attendee." },
                new() { Name = "LastName", Type = "string", DefaultValue = "string.Empty", Description = "Last name of the attendee." },
                new() { Name = "Id", Type = "string?", DefaultValue = "null", Description = "Optional identifier of the attendee." },
                new() { Name = "FullName", Type = "string", DefaultValue = "", Description = "Read-only. The combined and trimmed first and last name." },
                new() { Name = "Initials", Type = "string", DefaultValue = "", Description = "Read-only. The uppercased initials derived from the first and last name." },
            ]
        },
        new()
        {
            Id = "color-option-class",
            Title = "BitFullCalendarColorOption",
            Description = "Describes one selectable event color shown in the picker, filters, agenda headers, badges, bullets, and swatches. Events reference a color through its Id.",
            Parameters =
            [
                new() { Name = "Id", Type = "string", DefaultValue = "string.Empty", Description = "Stable identifier of the color matched against BitFullCalendarEvent.Color (case-insensitive). Use a short, slug-style value such as \"blue\" or \"skyblue\"." },
                new() { Name = "Title", Type = "string", DefaultValue = "string.Empty", Description = "Display label shown in pickers, filters, agenda headers, and event details. Used as-is with no localization." },
                new() { Name = "Value", Type = "string", DefaultValue = "string.Empty", Description = "CSS color value used for swatches, bullets, badge accents, and chip surfaces. Any valid CSS color such as hex, rgb(), hsl(), or a named color. Badge background, border, and text contrast tints are derived from this value at runtime." },
                new() { Name = "Defaults", Type = "static IReadOnlyList<BitFullCalendarColorOption>", DefaultValue = "", Description = "Built-in palette (blue, green, red, yellow, purple, orange) used when EventColorOptions is null or empty." },
            ]
        },
        new()
        {
            Id = "cell-class",
            Title = "BitFullCalendarCell",
            Description = "One day of the month grid, as MonthCellTemplate receives it.",
            Parameters =
            [
                new() { Name = "Day", Type = "int", DefaultValue = "0", Description = "The day number in the active calendar system (a Persian date shows its own day)." },
                new() { Name = "CurrentMonth", Type = "bool", DefaultValue = "false", Description = "True for a day of the month being shown; false for one borrowed from a neighbouring month." },
                new() { Name = "Date", Type = "DateTime", DefaultValue = "", Description = "The date of the day." },
            ]
        },
        new()
        {
            Id = "resource-class",
            Title = "BitFullCalendarResource",
            Description = "A schedulable resource shown as a row in the resource timeline view (for example a meeting room, a person, or a piece of equipment). Events are linked to a resource through BitFullCalendarEvent.Resource matching Id.",
            Parameters =
            [
                new() { Name = "Id", Type = "string", DefaultValue = "", Description = "Required. Stable, non-blank identifier matched against BitFullCalendarEvent.Resource. Cannot be null, empty, or whitespace - a blank id is rejected at assignment time." },
                new() { Name = "Title", Type = "string", DefaultValue = "string.Empty", Description = "Display name for the resource (for example \"Bay Wing\", \"Alice Johnson\", \"Meeting Room 3B\")." },
                new() { Name = "Subtitle", Type = "string?", DefaultValue = "null", Description = "Optional subtitle shown below the resource title (for example building or department)." },
                new() { Name = "Data", Type = "object?", DefaultValue = "null", Description = "Optional consumer-defined payload available to templates and click handlers." },
            ]
        },
        new()
        {
            Id = "settings-class",
            Title = "BitFullCalendarSettings",
            Description = "The preferences of the calendar. Every value is applied on first render and again whenever it changes; what the user changes from the settings gear is written back onto the same instance.",
            Parameters =
            [
                new() { Name = "Use24HourFormat", Type = "bool", DefaultValue = "true", Description = "Uses 24-hour time format instead of 12-hour (AM/PM)." },
                new() { Name = "BadgeVariant", Type = "BitFullCalendarBadgeVariant", DefaultValue = "BitFullCalendarBadgeVariant.Colored", Description = "Badge display style in the month view.", LinkType = LinkType.Link, Href = "#badge-variant-enum" },
                new() { Name = "StartOfDayHour", Type = "int", DefaultValue = "8", Description = "Hour the day/week time grid scrolls to on first render. Clamped into the VisibleStartHour/VisibleEndHour window." },
                new() { Name = "VisibleStartHour", Type = "int", DefaultValue = "0", Description = "First hour rendered by the day, week, and timeline day/week time grids (0–23). Equivalent to slotMinTime in other calendar libraries." },
                new() { Name = "VisibleEndHour", Type = "int", DefaultValue = "24", Description = "Exclusive last hour rendered by the day, week, and timeline day/week time grids (1–24). Equivalent to slotMaxTime. A value at or below VisibleStartHour is corrected to one hour past it." },
                new() { Name = "SlotDurationMinutes", Type = "int", DefaultValue = "30", Description = "Length in minutes of one slot inside an hour, and the granularity drag-and-drop and resizing snap to. Only divisors of 60 are accepted (5, 6, 10, 12, 15, 20, 30, 60); other values round to the nearest accepted one." },
                new() { Name = "HiddenDays", Type = "IReadOnlyList<DayOfWeek>?", DefaultValue = "null", Description = "Days of the week removed from every date grid (week, month, year, and the timeline week layout). Use it to render a work week. A list that would hide every day is ignored." },
                new() { Name = "BusinessDays", Type = "IReadOnlyList<DayOfWeek>?", DefaultValue = "null", Description = "The weekdays business hours run on. null means Monday to Friday; an empty list means no day is a business day." },
                new() { Name = "BusinessStartHour", Type = "int", DefaultValue = "9", Description = "First hour of the business day (0–23)." },
                new() { Name = "BusinessEndHour", Type = "int", DefaultValue = "17", Description = "Exclusive last hour of the business day (1–24). A value at or below BusinessStartHour is corrected to one hour past it." },
                new() { Name = "HighlightBusinessHours", Type = "bool", DefaultValue = "false", Description = "Shades everything outside the business hours - the off-hours slots of the day, week, and timeline grids and the non-business month cells. Also toggleable from the built-in settings panel." },
                new() { Name = "RestrictToBusinessHours", Type = "bool", DefaultValue = "false", Description = "Refuses any drag, resize, or dialog save whose resulting range is not fully contained in the business hours, reported as BitFullCalendarChangeRefusal.OutsideBusinessHours.", LinkType = LinkType.Link, Href = "#change-refusal-enum" },
                new() { Name = "FixedWeekCount", Type = "bool", DefaultValue = "false", Description = "Always renders six week rows in the month grid, so the calendar keeps the same height across months." },
                new() { Name = "ShowNonCurrentDates", Type = "bool", DefaultValue = "true", Description = "Renders the leading and trailing days the month grid borrows from the neighbouring months. When false those cells stay blank and inert." },
                new() { Name = "NavLinks", Type = "bool", DefaultValue = "false", Description = "Turns the month day numbers, the week-view column headers, and the month week numbers into links that navigate to the matching day or week view. A link whose target view is excluded by Views only selects the date." },
                new() { Name = "FirstDayOfWeek", Type = "DayOfWeek?", DefaultValue = "null", Description = "Overrides the day the week starts on. When null the active culture's DateTimeFormat.FirstDayOfWeek is used." },
                new() { Name = "ShowWeekNumbers", Type = "bool", DefaultValue = "false", Description = "Renders the ISO-8601 week number in the month grid and in the week view's time gutter." },
                new() { Name = "ShowCurrentTimeIndicator", Type = "bool", DefaultValue = "true", Description = "Renders the current time indicator line on the day and week time grids." },
                new() { Name = "MaxEventsPerDayCell", Type = "int", DefaultValue = "3", Description = "Number of event badges a month-grid cell renders before the rest collapse behind the \"+N more\" affordance. Clamped to 1–10." },
                new() { Name = "RequireEventDescription", Type = "bool", DefaultValue = "false", Description = "Makes the description field of the built-in add/edit dialog mandatory. Only the title is required by default." },
                new() { Name = "AllowEventOverlap", Type = "bool", DefaultValue = "true", Description = "Allows two events on the same resource to occupy the same time range. When false, a drag, resize, or dialog save that would create an overlap is refused." },
                new() { Name = "AllowRangeSelection", Type = "bool", DefaultValue = "true", Description = "Lets the user press and drag across the day or week time grid to select a range of slots, which opens the new event already spanning it. A plain click still creates a one-slot event." },
                new() { Name = "WeekDayCount", Type = "int?", DefaultValue = "null", Description = "Days the week view and the week timeline show (3 for a phone), starting at the selected date and skipping hidden days; prev/next turn by that length. null, or a value outside 1-6, is the culture's whole week." },
                new() { Name = "AllowAdd", Type = "bool", DefaultValue = "true", Description = "Lets the user create events: the Add Event button, the add affordance of every day and slot, and range selection." },
                new() { Name = "AllowEdit", Type = "bool", DefaultValue = "true", Description = "Lets the user change existing events: the Edit action of the details dialog and - since they are edits too - dragging and resizing." },
                new() { Name = "AllowDelete", Type = "bool", DefaultValue = "true", Description = "Lets the user delete events from the details dialog." },
                new() { Name = "AllowDrag", Type = "bool", DefaultValue = "true", Description = "Lets the user move events by dragging them or with Alt+Arrow keys, while AllowEdit is on." },
                new() { Name = "AllowResize", Type = "bool", DefaultValue = "true", Description = "Lets the user change how long events last by dragging their edges or with Shift+Arrow keys, while AllowEdit is on." },
                new() { Name = "AgendaModeGroupBy", Type = "BitFullCalendarAgendaGroupBy", DefaultValue = "BitFullCalendarAgendaGroupBy.Date", Description = "How events are grouped in the agenda view.", LinkType = LinkType.Link, Href = "#agenda-group-by-enum" },
                new() { Name = "EventLayout", Type = "BitFullCalendarEventLayout", DefaultValue = "BitFullCalendarEventLayout.Overlap", Description = "How overlapping event cards are positioned in the day and week views.", LinkType = LinkType.Link, Href = "#event-layout-enum" },
                new() { Name = "ShowDayViewCalendar", Type = "bool", DefaultValue = "true", Description = "Renders the mini calendar shown in the day view sidebar." },
            ]
        },
        new()
        {
            Id = "change-args-class",
            Title = "BitFullCalendarChangeEventArgs",
            Description = "Provides details about a user-applied calendar event change, passed to the OnChange callback.",
            Parameters =
            [
                new() { Name = "Event", Type = "BitFullCalendarEvent", DefaultValue = "", Description = "The current event snapshot after the change for Add/Edit, or the removed event snapshot for Delete.", LinkType = LinkType.Link, Href = "#event-class" },
                new() { Name = "Kind", Type = "BitFullCalendarChangeKind", DefaultValue = "", Description = "The change type that occurred (Add, Edit, Delete).", LinkType = LinkType.Link, Href = "#change-kind-enum" },
                new() { Name = "OldEvent", Type = "BitFullCalendarEvent?", DefaultValue = "null", Description = "The event snapshot before the change for Edit/Delete. Null for Add.", LinkType = LinkType.Link, Href = "#event-class" },
                new() { Name = "Source", Type = "BitFullCalendarChangeSource", DefaultValue = "", Description = "The UI source that triggered this change (Dialog, Drag, Resize).", LinkType = LinkType.Link, Href = "#change-source-enum" },
            ]
        },
        new()
        {
            Id = "changing-args-class",
            Title = "BitFullCalendarChangingEventArgs",
            Description = "A user-driven change about to be committed, passed to the OnChanging callback.",
            Parameters =
            [
                new() { Name = "Event", Type = "BitFullCalendarEvent", DefaultValue = "", Description = "The event as the change would leave it for Add/Edit, or the event about to be removed for Delete.", LinkType = LinkType.Link, Href = "#event-class" },
                new() { Name = "Kind", Type = "BitFullCalendarChangeKind", DefaultValue = "", Description = "The kind of change (Add, Edit, Delete).", LinkType = LinkType.Link, Href = "#change-kind-enum" },
                new() { Name = "OldEvent", Type = "BitFullCalendarEvent?", DefaultValue = "null", Description = "The event before the change for Edit/Delete. Null for Add.", LinkType = LinkType.Link, Href = "#event-class" },
                new() { Name = "Source", Type = "BitFullCalendarChangeSource", DefaultValue = "", Description = "The UI source (Dialog, Drag, Resize).", LinkType = LinkType.Link, Href = "#change-source-enum" },
                new() { Name = "Cancel", Type = "bool", DefaultValue = "false", Description = "Set to true to refuse the change." },
            ]
        },
        new()
        {
            Id = "date-change-args-class",
            Title = "BitFullCalendarDateChangeEventArgs",
            Description = "Provides details about a date range change, fired when the user navigates (prev/next/today) or switches views. Passed to the OnDateChange callback.",
            Parameters =
            [
                new() { Name = "Start", Type = "DateTime", DefaultValue = "", Description = "Start of the visible date range (inclusive)." },
                new() { Name = "End", Type = "DateTime", DefaultValue = "", Description = "End of the visible date range (inclusive)." },
                new() { Name = "View", Type = "BitFullCalendarView", DefaultValue = "", Description = "The active calendar view when the change occurred.", LinkType = LinkType.Link, Href = "#view-enum" },
            ]
        },
        new()
        {
            Id = "texts-class",
            Title = "BitFullCalendarTexts",
            Description = "Custom UI strings for labels, placeholders, action buttons, aria labels, and validation messages. Used for localization and customization of all built-in text.",
            Parameters =
            [
                new() { Name = "ViewDay", Type = "string", DefaultValue = "\"Day\"", Description = "Label for the day view tab." },
                new() { Name = "ViewWeek", Type = "string", DefaultValue = "\"Week\"", Description = "Label for the week view tab." },
                new() { Name = "ViewMonth", Type = "string", DefaultValue = "\"Month\"", Description = "Label for the month view tab." },
                new() { Name = "ViewYear", Type = "string", DefaultValue = "\"Year\"", Description = "Label for the year view tab." },
                new() { Name = "ViewAgenda", Type = "string", DefaultValue = "\"Agenda\"", Description = "Label for the agenda view tab." },
                new() { Name = "ModeEvent", Type = "string", DefaultValue = "\"Events\"", Description = "Label for the event mode tab." },
                new() { Name = "ModeTimeline", Type = "string", DefaultValue = "\"Timeline\"", Description = "Label for the timeline mode tab." },
                new() { Name = "ViewTabsAriaLabel", Type = "string", DefaultValue = "\"Views\"", Description = "Accessible name of the view tab strip." },
                new() { Name = "ModeTabsAriaLabel", Type = "string", DefaultValue = "\"Modes\"", Description = "Accessible name of the mode tab strip." },
                new() { Name = "TodayButton", Type = "string", DefaultValue = "\"Today\"", Description = "Label for the today navigation button." },
                new() { Name = "AddEventButton", Type = "string", DefaultValue = "\"Add Event\"", Description = "Label for the add event button." },
                new() { Name = "AddEventHoverHint", Type = "string", DefaultValue = "\"Add event\"", Description = "Tooltip shown when hovering the add event affordance." },
                new() { Name = "PreviousButtonTitle", Type = "string", DefaultValue = "\"Previous\"", Description = "Title for the previous navigation button." },
                new() { Name = "NextButtonTitle", Type = "string", DefaultValue = "\"Next\"", Description = "Title for the next navigation button." },
                new() { Name = "PreviousMonthAriaLabel", Type = "string", DefaultValue = "\"Previous month\"", Description = "Aria label for the mini calendar previous month navigation button." },
                new() { Name = "NextMonthAriaLabel", Type = "string", DefaultValue = "\"Next month\"", Description = "Aria label for the mini calendar next month navigation button." },
                new() { Name = "SettingsButtonTitle", Type = "string", DefaultValue = "\"Settings\"", Description = "Title for the settings gear button." },
                new() { Name = "FilterByColorAriaLabel", Type = "string", DefaultValue = "\"Filter events by color\"", Description = "Aria label for the color filter dropdown." },
                new() { Name = "FilterByPersonAriaLabel", Type = "string", DefaultValue = "\"Filter events by person in current view\"", Description = "Aria label for the attendee filter dropdown." },
                new() { Name = "AllColorsOption", Type = "string", DefaultValue = "\"All colors\"", Description = "Option text for clearing the color filter." },
                new() { Name = "AllPeopleOption", Type = "string", DefaultValue = "\"All people\"", Description = "Option text for clearing the attendee filter." },
                new() { Name = "UnnamedAttendee", Type = "string", DefaultValue = "\"(Unnamed)\"", Description = "Fallback text for an attendee with no name." },
                new() { Name = "CalendarSettingsLabel", Type = "string", DefaultValue = "\"Calendar settings\"", Description = "Heading for the settings panel." },
                new() { Name = "DotBadgeLabel", Type = "string", DefaultValue = "\"Dot badge\"", Description = "Label for the dot badge setting toggle." },
                new() { Name = "TwentyFourHourFormatLabel", Type = "string", DefaultValue = "\"24-hour format\"", Description = "Label for the 24-hour format setting toggle." },
                new() { Name = "DayStartsAtLabel", Type = "string", DefaultValue = "\"Day starts at\"", Description = "Label for the day start hour setting." },
                new() { Name = "HourSuffix", Type = "string", DefaultValue = "\"h\"", Description = "Suffix appended to hour values in the settings." },
                new() { Name = "AgendaGroupByLabel", Type = "string", DefaultValue = "\"Agenda group by\"", Description = "Label for the agenda grouping setting." },
                new() { Name = "AgendaGroupByDate", Type = "string", DefaultValue = "\"Date\"", Description = "Option text for grouping the agenda by date." },
                new() { Name = "AgendaGroupByColor", Type = "string", DefaultValue = "\"Color\"", Description = "Option text for grouping the agenda by color." },
                new() { Name = "StackedEventsLabel", Type = "string", DefaultValue = "\"Stack overlapping events\"", Description = "Label for the overlapping events layout toggle." },
                new() { Name = "ShowDayViewCalendarLabel", Type = "string", DefaultValue = "\"Show calendar in day view\"", Description = "Label for the day view mini calendar toggle." },
                new() { Name = "ShowWeekNumbersLabel", Type = "string", DefaultValue = "\"Show week numbers\"", Description = "Label for the week numbers setting toggle." },
                new() { Name = "ShowCurrentTimeIndicatorLabel", Type = "string", DefaultValue = "\"Show current time\"", Description = "Label for the current time indicator setting toggle." },
                new() { Name = "SlotDurationLabel", Type = "string", DefaultValue = "\"Slot duration\"", Description = "Label for the slot duration setting." },
                new() { Name = "MinuteSuffix", Type = "string", DefaultValue = "\"min\"", Description = "Suffix appended to minute values in the settings." },
                new() { Name = "WeekMobileWarning", Type = "string", DefaultValue = "\"Weekly view is not recommended...\"", Description = "Warning shown when using the week view on small devices." },
                new() { Name = "HappeningNowTitle", Type = "string", DefaultValue = "\"Happening now\"", Description = "Title for the happening-now indicator." },
                new() { Name = "NoAppointmentsNow", Type = "string", DefaultValue = "\"No appointments at the moment\"", Description = "Text shown when there are no current appointments." },
                new() { Name = "SearchEventsPlaceholder", Type = "string", DefaultValue = "\"Search events...\"", Description = "Placeholder for the agenda search box." },
                new() { Name = "NoEventsFound", Type = "string", DefaultValue = "\"No events found.\"", Description = "Text shown when a search returns no events." },
                new() { Name = "EventListTitleFormat", Type = "string", DefaultValue = "\"Events on {0}\"", Description = "Format template for the event list dialog title; {0} is the formatted date." },
                new() { Name = "EventListCountFormat", Type = "string", DefaultValue = "\"{0} event(s)\"", Description = "Format template for the event count in the event list dialog; {0} is the count." },
                new() { Name = "MoreEventsFormat", Type = "string", DefaultValue = "\"+{0} more\"", Description = "Format template for the \"+N more\" affordance in month cells; {0} is the hidden-event count." },
                new() { Name = "AddEventDialogTitle", Type = "string", DefaultValue = "\"Add New Event\"", Description = "Title for the add event dialog." },
                new() { Name = "EditEventDialogTitle", Type = "string", DefaultValue = "\"Edit Event\"", Description = "Title for the edit event dialog." },
                new() { Name = "AddEventDialogSubtitle", Type = "string", DefaultValue = "\"Create a new event for your calendar.\"", Description = "Subtitle for the add event dialog." },
                new() { Name = "EditEventDialogSubtitle", Type = "string", DefaultValue = "\"Modify your existing event.\"", Description = "Subtitle for the edit event dialog." },
                new() { Name = "CloseAriaLabel", Type = "string", DefaultValue = "\"Close\"", Description = "Aria label for the dialog close button." },
                new() { Name = "CloseButton", Type = "string", DefaultValue = "\"Close\"", Description = "Label for the close button." },
                new() { Name = "CancelButton", Type = "string", DefaultValue = "\"Cancel\"", Description = "Label for the cancel button." },
                new() { Name = "EditButton", Type = "string", DefaultValue = "\"Edit\"", Description = "Label for the edit button." },
                new() { Name = "DeleteButton", Type = "string", DefaultValue = "\"Delete\"", Description = "Label for the delete button." },
                new() { Name = "CreateEventButton", Type = "string", DefaultValue = "\"Create Event\"", Description = "Label for the create event button." },
                new() { Name = "SaveChangesButton", Type = "string", DefaultValue = "\"Save Changes\"", Description = "Label for the save changes button." },
                new() { Name = "TitleLabel", Type = "string", DefaultValue = "\"Title\"", Description = "Label for the event title field." },
                new() { Name = "EventTitlePlaceholder", Type = "string", DefaultValue = "\"Event title\"", Description = "Placeholder for the event title field." },
                new() { Name = "StartDateTimeLabel", Type = "string", DefaultValue = "\"Start Date & Time\"", Description = "Label for the start date and time field." },
                new() { Name = "EndDateTimeLabel", Type = "string", DefaultValue = "\"End Date & Time\"", Description = "Label for the end date and time field." },
                new() { Name = "ColorLabel", Type = "string", DefaultValue = "\"Color\"", Description = "Label for the color field." },
                new() { Name = "EventColorAriaLabel", Type = "string", DefaultValue = "\"Event color\"", Description = "Aria label for the color picker." },
                new() { Name = "DescriptionLabel", Type = "string", DefaultValue = "\"Description\"", Description = "Label for the description field." },
                new() { Name = "EventDescriptionPlaceholder", Type = "string", DefaultValue = "\"Event description\"", Description = "Placeholder for the description field." },
                new() { Name = "AttendeesLabel", Type = "string", DefaultValue = "\"Attendees\"", Description = "Label for the attendees field." },
                new() { Name = "NoAttendeesText", Type = "string", DefaultValue = "\"No attendees\"", Description = "Text shown when an event has no attendees." },
                new() { Name = "FirstNamePlaceholder", Type = "string", DefaultValue = "\"First name\"", Description = "Placeholder for the attendee first name field." },
                new() { Name = "LastNamePlaceholder", Type = "string", DefaultValue = "\"Last name\"", Description = "Placeholder for the attendee last name field." },
                new() { Name = "IdOptionalPlaceholder", Type = "string", DefaultValue = "\"ID (optional)\"", Description = "Placeholder for the optional attendee id field." },
                new() { Name = "AddButton", Type = "string", DefaultValue = "\"Add\"", Description = "Label for the add attendee button." },
                new() { Name = "RemoveAttendeeAriaLabel", Type = "string", DefaultValue = "\"Remove attendee\"", Description = "Aria label for the remove attendee button on an attendee chip." },
                new() { Name = "StartDateLabel", Type = "string", DefaultValue = "\"Start Date\"", Description = "Label for the start date in the event details." },
                new() { Name = "EndDateLabel", Type = "string", DefaultValue = "\"End Date\"", Description = "Label for the end date in the event details." },
                new() { Name = "AtWord", Type = "string", DefaultValue = "\"at\"", Description = "Connector word between date and time in the event details." },
                new() { Name = "AllDayLabel", Type = "string", DefaultValue = "\"All day\"", Description = "Label of the all-day switch in the add/edit dialog, and of an all-day event's badge and details." },
                new() { Name = "WeekNumberFormat", Type = "string", DefaultValue = "\"W{0}\"", Description = "Format template of the week-number cell; {0} is the ISO-8601 week number." },
                new() { Name = "WeekNumberAriaLabelFormat", Type = "string", DefaultValue = "\"Week {0}\"", Description = "Accessible name of a week-number cell; {0} is the ISO-8601 week number." },
                new() { Name = "EventOverlapMessage", Type = "string", DefaultValue = "\"This time range is already taken on that resource.\"", Description = "Notice shown when a move, resize, or save is refused because it would overlap another event." },
                new() { Name = "OutOfRangeMessage", Type = "string", DefaultValue = "\"That date is outside the allowed range.\"", Description = "Notice shown when a move or resize is refused because it falls outside the allowed date range." },
                new() { Name = "OutsideBusinessHoursMessage", Type = "string", DefaultValue = "\"That time is outside business hours.\"", Description = "Notice shown when a move, resize, or save is refused because it falls outside the business hours." },
                new() { Name = "BlockedMessage", Type = "string", DefaultValue = "\"That time is unavailable.\"", Description = "Notice shown when a move, resize, or save is refused because it overlaps a blocking event." },
                new() { Name = "HighlightBusinessHoursLabel", Type = "string", DefaultValue = "\"Highlight business hours\"", Description = "Label for the business-hours toggle in the settings panel." },
                new() { Name = "NavLinkDayAriaLabelFormat", Type = "string", DefaultValue = "\"Go to {0}\"", Description = "Accessible name of a day number or column header that navigates to that day; {0} is the formatted date." },
                new() { Name = "NavLinkWeekAriaLabelFormat", Type = "string", DefaultValue = "\"Go to week {0}\"", Description = "Accessible name of a week number that navigates to that week; {0} is the ISO-8601 week number." },
                new() { Name = "ValidationTitleRequired", Type = "string", DefaultValue = "\"Title is required\"", Description = "Validation message when the title is empty." },
                new() { Name = "ValidationDescriptionRequired", Type = "string", DefaultValue = "\"Description is required\"", Description = "Validation message when the description is empty." },
                new() { Name = "ValidationEndAfterStart", Type = "string", DefaultValue = "\"End date must be after start date\"", Description = "Validation message when the end date is not after the start date." },
                new() { Name = "ValidationAttendeeNameRequired", Type = "string", DefaultValue = "\"First name or last name is required\"", Description = "Validation message when an attendee has no name." },
                new() { Name = "ResizePreviewAriaLabel", Type = "string", DefaultValue = "\"New time range\"", Description = "Aria label for the resize preview indicator." },
                new() { Name = "RepeatsLabel", Type = "string", DefaultValue = "\"Repeats\"", Description = "Label of the repeat-rule row in the event details dialog." },
                new() { Name = "RepeatsDaily", Type = "string", DefaultValue = "\"Daily\"", Description = "Name of a daily repeat rule." },
                new() { Name = "RepeatsWeekly", Type = "string", DefaultValue = "\"Weekly\"", Description = "Name of a weekly repeat rule." },
                new() { Name = "RepeatsMonthly", Type = "string", DefaultValue = "\"Monthly\"", Description = "Name of a monthly repeat rule." },
                new() { Name = "RepeatsYearly", Type = "string", DefaultValue = "\"Yearly\"", Description = "Name of a yearly repeat rule." },
                new() { Name = "RepeatsIntervalFormat", Type = "string", DefaultValue = "\"every {0}\"", Description = "Appended to the frequency when the rule repeats every N units; {0} is the interval." },
                new() { Name = "RepeatsCountFormat", Type = "string", DefaultValue = "\"{0} times\"", Description = "Appended when the series is closed by a number of occurrences; {0} is the count." },
                new() { Name = "RepeatsUntilFormat", Type = "string", DefaultValue = "\"until {0}\"", Description = "Appended when the series is closed by a date; {0} is the formatted date." },
                new() { Name = "RepeatsOnDaysFormat", Type = "string", DefaultValue = "\"on {0}\"", Description = "Appended when a weekly series names its weekdays; {0} is the abbreviated weekday names joined with ListSeparator." },
                new() { Name = "RepeatsOnWeekOfMonthFormat", Type = "string", DefaultValue = "\"on the {0} {1}\"", Description = "Appended when a monthly or yearly series lands on a weekday of the month; {0} is the week of the month, {1} the weekday names." },
                new() { Name = "ListSeparator", Type = "string", DefaultValue = "\", \"", Description = "Separator between the items of a list in a summary, such as weekday names." },
                new() { Name = "WeekOfMonthFirst", Type = "string", DefaultValue = "\"first\"", Description = "Name of BitFullCalendarWeekOfMonth.First." },
                new() { Name = "WeekOfMonthSecond", Type = "string", DefaultValue = "\"second\"", Description = "Name of BitFullCalendarWeekOfMonth.Second." },
                new() { Name = "WeekOfMonthThird", Type = "string", DefaultValue = "\"third\"", Description = "Name of BitFullCalendarWeekOfMonth.Third." },
                new() { Name = "WeekOfMonthFourth", Type = "string", DefaultValue = "\"fourth\"", Description = "Name of BitFullCalendarWeekOfMonth.Fourth." },
                new() { Name = "WeekOfMonthLast", Type = "string", DefaultValue = "\"last\"", Description = "Name of BitFullCalendarWeekOfMonth.Last." },
                new() { Name = "RepeatLabel", Type = "string", DefaultValue = "\"Repeat\"", Description = "Label of the repeat picker in the add/edit dialog." },
                new() { Name = "DoesNotRepeatOption", Type = "string", DefaultValue = "\"Does not repeat\"", Description = "The repeat picker's option for a one-off event." },
                new() { Name = "RepeatEveryLabel", Type = "string", DefaultValue = "\"Repeat every\"", Description = "Label of the interval field in the add/edit dialog." },
                new() { Name = "RepeatDaysUnit", Type = "string", DefaultValue = "\"day(s)\"", Description = "Unit shown after the interval of a daily series." },
                new() { Name = "RepeatWeeksUnit", Type = "string", DefaultValue = "\"week(s)\"", Description = "Unit shown after the interval of a weekly series." },
                new() { Name = "RepeatMonthsUnit", Type = "string", DefaultValue = "\"month(s)\"", Description = "Unit shown after the interval of a monthly series." },
                new() { Name = "RepeatYearsUnit", Type = "string", DefaultValue = "\"year(s)\"", Description = "Unit shown after the interval of a yearly series." },
                new() { Name = "RepeatOnLabel", Type = "string", DefaultValue = "\"Repeat on\"", Description = "Label of the weekday and day-of-the-month choices in the add/edit dialog." },
                new() { Name = "RepeatOnDayOfMonthFormat", Type = "string", DefaultValue = "\"Day {0}\"", Description = "The option that keeps a monthly series on the start date's day number; {0} is that day." },
                new() { Name = "RepeatOnWeekOfMonthFormat", Type = "string", DefaultValue = "\"The {0}\"", Description = "The option that moves a monthly or yearly series onto a weekday of the month; {0} is the week of the month. The weekdays are picked beside it." },
                new() { Name = "EndsLabel", Type = "string", DefaultValue = "\"Ends\"", Description = "Label of the field that decides when a series ends." },
                new() { Name = "EndsNeverOption", Type = "string", DefaultValue = "\"Never\"", Description = "The option for an open-ended series." },
                new() { Name = "EndsOnDateOption", Type = "string", DefaultValue = "\"On date\"", Description = "The option for a series that ends on a date." },
                new() { Name = "EndsAfterOption", Type = "string", DefaultValue = "\"After\"", Description = "The option for a series that ends after a number of occurrences." },
                new() { Name = "OccurrencesLabel", Type = "string", DefaultValue = "\"occurrence(s)\"", Description = "Shown after the occurrence count of a series that ends after a number of occurrences." },
                new() { Name = "RepeatExceptionsLabel", Type = "string", DefaultValue = "\"Exceptions\"", Description = "Label of the skipped and added dates of a series in the add/edit dialog." },
                new() { Name = "SkipDateButton", Type = "string", DefaultValue = "\"Skip date\"", Description = "Button that adds the picked date to the dates a series skips." },
                new() { Name = "AddDateButton", Type = "string", DefaultValue = "\"Add date\"", Description = "Button that adds the picked date to the dates a series also occurs on." },
                new() { Name = "SkippedDatesLabel", Type = "string", DefaultValue = "\"Skipped\"", Description = "Caption of the dates a series skips." },
                new() { Name = "AddedDatesLabel", Type = "string", DefaultValue = "\"Added\"", Description = "Caption of the dates a series also occurs on." },
                new() { Name = "RemoveDateAriaLabel", Type = "string", DefaultValue = "\"Remove date\"", Description = "Accessible name of the button that removes a skipped or added date." },
                new() { Name = "ValidationRepeatAtLeastOne", Type = "string", DefaultValue = "\"Must be at least 1\"", Description = "Validation message when an interval or an occurrence count is below 1." },
                new() { Name = "ValidationUntilBeforeStart", Type = "string", DefaultValue = "\"The series cannot end before it starts\"", Description = "Validation message when a series is set to end before it starts." },
                new() { Name = "RecurringEditTitle", Type = "string", DefaultValue = "\"Edit recurring event\"", Description = "Title of the prompt that asks whether an edit applies to one occurrence or the series." },
                new() { Name = "RecurringDeleteTitle", Type = "string", DefaultValue = "\"Delete recurring event\"", Description = "Title of the prompt that asks whether a delete applies to one occurrence or the series." },
                new() { Name = "ThisOccurrenceOption", Type = "string", DefaultValue = "\"This event\"", Description = "The choice that applies an edit or delete to the opened occurrence only." },
                new() { Name = "AllOccurrencesOption", Type = "string", DefaultValue = "\"All events in the series\"", Description = "The choice that applies an edit or delete to the whole series." },
                new() { Name = "OkButton", Type = "string", DefaultValue = "\"OK\"", Description = "Label of the button that confirms a prompt." },
                new() { Name = "GetWeekOfMonthLabel(BitFullCalendarWeekOfMonth)", Type = "string", DefaultValue = "", Description = "Method that returns the localized name of a week of the month." },
                new() { Name = "GetRecurrenceSummary(BitFullCalendarRecurrence, CultureInfo?, DateTime?)", Type = "string", DefaultValue = "", Description = "Method that returns the one-line repeat summary shown in the event details dialog. Pass the master's start so a rule that names no weekday can still be described." },
                new() { Name = "ResourceLabel", Type = "string", DefaultValue = "\"Resource\"", Description = "Label for the resource field in the add/edit dialog." },
                new() { Name = "ResourceColumnHeader", Type = "string", DefaultValue = "\"Resource\"", Description = "Header for the resource column in the timeline view." },
                new() { Name = "NoResourceLabel", Type = "string", DefaultValue = "\"Unassigned\"", Description = "Label for events not assigned to a resource." },
                new() { Name = "NoResourceOption", Type = "string", DefaultValue = "\"(none)\"", Description = "Option text for clearing the resource assignment." },
                new() { Name = "NoResourcesMessage", Type = "string", DefaultValue = "\"No resources to display.\"", Description = "Message shown when there are no resources in the timeline view." },
                new() { Name = "GetViewLabel(BitFullCalendarView)", Type = "string", DefaultValue = "", Description = "Method that returns the localized label for the given view." },
                new() { Name = "GetModeLabel(BitFullCalendarMode)", Type = "string", DefaultValue = "", Description = "Method that returns the localized label for the given mode." },
            ]
        },
    ];



    private readonly List<ComponentParameter> componentPublicMembers =
    [
        new()
        {
            Name = "State",
            Type = "BitFullCalendarState",
            DefaultValue = "",
            Description = "The live state: the active view, mode and date, the resolved settings and the event projections the views render from. Read it; drive the calendar through the parameters and the methods below.",
        },
        new()
        {
            Name = "GoToDate",
            Type = "Action<DateTime>",
            DefaultValue = "",
            Description = "Moves the calendar to the supplied date, clamped into the MinDate/MaxDate window.",
        },
        new()
        {
            Name = "GoToToday",
            Type = "Action",
            DefaultValue = "",
            Description = "Moves the calendar to today, clamped into the MinDate/MaxDate window.",
        },
        new()
        {
            Name = "NavigateNext",
            Type = "Action",
            DefaultValue = "",
            Description = "Steps one period forward (a day, week, month or year, depending on the view). Does nothing past MaxDate.",
        },
        new()
        {
            Name = "NavigatePrevious",
            Type = "Action",
            DefaultValue = "",
            Description = "Steps one period back. Does nothing before MinDate.",
        },
        new()
        {
            Name = "ChangeView",
            Type = "Action<BitFullCalendarView>",
            DefaultValue = "",
            Description = "Switches the active view, clamped into Views and, in Timeline mode, into the layouts the timeline renders.",
            LinkType = LinkType.Link,
            Href = "#view-enum",
        },
        new()
        {
            Name = "ChangeMode",
            Type = "Action<BitFullCalendarMode>",
            DefaultValue = "",
            Description = "Switches the layout mode. Timeline falls back to Event while there is no resource or no timeline-capable view.",
            LinkType = LinkType.Link,
            Href = "#mode-enum",
        },
        new()
        {
            Name = "GetVisibleRange",
            Type = "Func<(DateTime Start, DateTime End)>",
            DefaultValue = "",
            Description = "The inclusive start and end dates currently on screen.",
        },
        new()
        {
            Name = "ScrollToTimeAsync",
            Type = "Func<TimeSpan, Task<bool>>",
            DefaultValue = "",
            Description = "Scrolls the day and week grids, or the day and week timeline, to a time (clamped into the visible hours). Returns false when the active view has no time axis.",
        },
    ];

    private readonly List<ComponentCssVariable> componentCssVariables =
    [
        new() { Name = "--bit-FullCalendar-background", DefaultValue = "var(--bit-clr-bg-pri)", Description = "Surface of the calendar, its dialogs and popups." },
        new() { Name = "--bit-FullCalendar-secondary-background", DefaultValue = "var(--bit-clr-bg-sec)", Description = "Secondary surfaces: the tab strips, the day-view sidebar, sticky group titles, the timeline gutter." },
        new() { Name = "--bit-FullCalendar-hover-background", DefaultValue = "var(--bit-clr-bg-pri-hover)", Description = "Background of a hovered cell, slot, row or control." },
        new() { Name = "--bit-FullCalendar-color", DefaultValue = "var(--bit-clr-fg-pri)", Description = "Text color." },
        new() { Name = "--bit-FullCalendar-secondary-color", DefaultValue = "var(--bit-clr-fg-sec)", Description = "Secondary text: weekday headers, group titles, descriptions." },
        new() { Name = "--bit-FullCalendar-muted-color", DefaultValue = "var(--bit-clr-fg-ter)", Description = "Muted text: hour labels, week numbers, days of the neighbouring months." },
        new() { Name = "--bit-FullCalendar-border-color", DefaultValue = "var(--bit-clr-brd-ter)", Description = "Grid lines and the border of the calendar." },
        new() { Name = "--bit-FullCalendar-control-border-color", DefaultValue = "var(--bit-clr-brd-pri)", Description = "Edges of the fields, selects, search box and switch tracks (kept at 3:1 against the surface)." },
        new() { Name = "--bit-FullCalendar-accent-color", DefaultValue = "var(--bit-clr-pri)", Description = "Accent: today's marker, the primary button, the selected choices, the focus outline of a slot." },
        new() { Name = "--bit-FullCalendar-accent-hover-color", DefaultValue = "var(--bit-clr-pri-hover)", Description = "Accent of a hovered primary button." },
        new() { Name = "--bit-FullCalendar-accent-text-color", DefaultValue = "var(--bit-clr-pri-text)", Description = "Text drawn on the accent color." },
        new() { Name = "--bit-FullCalendar-event-color", DefaultValue = "--bit-FullCalendar-accent-color", Description = "Color of an event whose Color is neither a color option nor a CSS color. Its chip, swatch and bullet are derived from it." },
        new() { Name = "--bit-FullCalendar-event-border-radius", DefaultValue = "var(--bit-shp-radius-control)", Description = "Corner radius of the event badges and blocks." },
        new() { Name = "--bit-FullCalendar-background-event-opacity", DefaultValue = "0.16", Description = "How strongly a background event's color tints its band, from 0 to 1." },
        new() { Name = "--bit-FullCalendar-today-background", DefaultValue = "6% accent", Description = "Tint of today's column in the week grid." },
        new() { Name = "--bit-FullCalendar-selection-background", DefaultValue = "22% accent", Description = "The slots a range selection covers." },
        new() { Name = "--bit-FullCalendar-off-hours-background", DefaultValue = "7% secondary text", Description = "Shading outside the business hours (Settings.HighlightBusinessHours)." },
        new() { Name = "--bit-FullCalendar-now-indicator-color", DefaultValue = "var(--bit-clr-err)", Description = "The current-time line of the time grids and the timeline." },
        new() { Name = "--bit-FullCalendar-border-radius", DefaultValue = "var(--bit-shp-radius-surface)", Description = "Corner radius of the calendar itself and of the year-view months." },
        new() { Name = "--bit-FullCalendar-height", DefaultValue = "600px", Description = "Height of the calendar. Use 100% inside a sized container." },
        new() { Name = "--bit-FullCalendar-hour-height", DefaultValue = "96px", Description = "Height of one hour of the day and week grids; events, the now line, scrolling and resizing follow it." },
        new() { Name = "--bit-FullCalendar-time-gutter-width", DefaultValue = "3.75rem", Description = "Width of the hour-label gutter beside the day and week grids." },
        new() { Name = "--bit-FullCalendar-week-number-width", DefaultValue = "34px", Description = "Width of the week-number rail of the month grid." },
        new() { Name = "--bit-FullCalendar-month-cell-min-height", DefaultValue = "6.25rem", Description = "Minimum height of a month-grid day." },
        new() { Name = "--bit-FullCalendar-dialog-max-width", DefaultValue = "30rem", Description = "Maximum width of the built-in dialogs." },
    ];



    private readonly List<BitFullCalendarEvent> basicEvents = CreateEvents();
    private readonly List<BitFullCalendarEvent> viewsEvents = CreateEvents();
    private readonly List<BitFullCalendarEvent> settingsEvents = CreateEvents();
    private readonly List<BitFullCalendarEvent> gridEvents = CreateEvents();
    private readonly List<BitFullCalendarEvent> workWeekEvents = CreateEvents();
    private readonly List<BitFullCalendarEvent> monthGridEvents = CreateEvents();
    private readonly List<BitFullCalendarEvent> allDayEvents = CreateAllDayEvents();
    private readonly List<BitFullCalendarEvent> recurringEvents = CreateRecurringEvents();
    private readonly List<BitFullCalendarEvent> colorEvents = CreateColorEvents();
    private readonly List<BitFullCalendarEvent> templateEvents = CreateEvents();
    private readonly List<BitFullCalendarEvent> resourceEvents = CreateResourceEvents();
    private readonly List<BitFullCalendarEvent> boundsEvents = CreateEvents();
    private readonly List<BitFullCalendarEvent> rulesEvents = CreateRuleEvents();
    private readonly List<BitFullCalendarEvent> readOnlyEvents = CreateEvents();
    private readonly List<BitFullCalendarEvent> changeEvents = CreateEvents();
    private readonly List<BitFullCalendarEvent> bindingEvents = CreateResourceEvents();
    private readonly List<BitFullCalendarEvent> toolbarEvents = CreateEvents();
    private readonly List<BitFullCalendarEvent> localizationEvents = CreateEvents();
    private readonly List<BitFullCalendarEvent> accessibilityEvents = CreateEvents();
    private readonly List<BitFullCalendarEvent> cascadeEvents1 = CreateEvents();
    private readonly List<BitFullCalendarEvent> cascadeEvents2 = CreateEvents();
    private readonly List<BitFullCalendarEvent> styleEvents = CreateEvents();
    private readonly List<BitFullCalendarEvent> rtlEvents = CreateEvents();

    private readonly List<BitFullCalendarResource> resources =
    [
        new() { Id = "room-bay", Title = "HQ - Bay Wing", Subtitle = "Headquarters" },
        new() { Id = "room-garden", Title = "The Garden", Subtitle = "Headquarters" },
        new() { Id = "room-war", Title = "War Room (B1)", Subtitle = "Basement" },
    ];


    // Views
    private string viewsPreset = "week-day";

    private BitFullCalendarView[] SelectedViews => viewsPreset switch
    {
        "month-agenda" => [BitFullCalendarView.Month, BitFullCalendarView.Agenda],
        "month" => [BitFullCalendarView.Month],
        _ => [BitFullCalendarView.Week, BitFullCalendarView.Day]
    };


    // Settings - the object is mutated in place: the calendar diffs it against the values it last applied, so only
    // what actually changed is pushed into the live state.
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

    // The callback re-renders this page, so the choice group above shows a layout picked from the gear.
    private void HandleSettingsChange(BitFullCalendarSettings changed) { }


    // Time grid
    private readonly BitFullCalendarSettings gridSettings = new()
    {
        VisibleStartHour = 8,
        VisibleEndHour = 18,
        SlotDurationMinutes = 30,
        StartOfDayHour = 9
    };

    private string _gridHoursPreset = "office";
    private string gridHoursPreset
    {
        get => _gridHoursPreset;
        set
        {
            _gridHoursPreset = value;
            (gridSettings.VisibleStartHour, gridSettings.VisibleEndHour) = value switch
            {
                "office" => (8, 18),
                "extended" => (6, 22),
                _ => (0, 24)
            };
        }
    }

    private int gridSlotMinutes
    {
        get => gridSettings.SlotDurationMinutes;
        set => gridSettings.SlotDurationMinutes = value;
    }


    // Work week
    private readonly BitFullCalendarSettings workWeekSettings = new()
    {
        HiddenDays = [DayOfWeek.Saturday, DayOfWeek.Sunday],
        FirstDayOfWeek = DayOfWeek.Monday,
        ShowWeekNumbers = true,
        VisibleStartHour = 8,
        VisibleEndHour = 19
    };

    private string _workWeekPreset = "mon-fri";
    private string workWeekPreset
    {
        get => _workWeekPreset;
        set
        {
            _workWeekPreset = value;
            (workWeekSettings.HiddenDays, workWeekSettings.FirstDayOfWeek) = value switch
            {
                "mon-fri" => ((IReadOnlyList<DayOfWeek>?)[DayOfWeek.Saturday, DayOfWeek.Sunday], (DayOfWeek?)DayOfWeek.Monday),
                "sun-thu" => ([DayOfWeek.Friday, DayOfWeek.Saturday], DayOfWeek.Sunday),
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


    // Month grid
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


    // Date bounds
    private readonly DateTime boundsMin = DateTime.Today.AddDays(-10);
    private readonly DateTime boundsMax = DateTime.Today.AddDays(20);


    // Booking rules
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


    // Read-only & permissions
    private bool isReadOnly;

    private readonly BitFullCalendarSettings permissionSettings = new();


    // OnChange & OnChanging
    private string? lastChange;
    private bool refusePast = true;

    private void HandleChanging(BitFullCalendarChangingEventArgs args)
    {
        if (refusePast && args.Kind is not BitFullCalendarChangeKind.Delete && args.Event.StartDate < DateTime.Now)
        {
            args.Cancel = true;
            lastChange = $"Refused ({args.Source}): {args.Event.Title} would start in the past";
        }
    }

    private Task HandleChange(BitFullCalendarChangeEventArgs args)
    {
        lastChange = $"{args.Kind} ({args.Source}): {args.Event.Title}";

        // Persist the change into the backing list so it stays in sync with the calendar's internal state: the
        // calendar copies Events into its own store, so a later re-render would otherwise re-sync this stale list.
        switch (args.Kind)
        {
            case BitFullCalendarChangeKind.Add:
                changeEvents.Add(args.Event);
                break;
            case BitFullCalendarChangeKind.Edit:
                var index = changeEvents.FindIndex(e => e.Id == args.Event.Id);
                if (index >= 0)
                    changeEvents[index] = args.Event;
                else
                    changeEvents.Add(args.Event);
                break;
            case BitFullCalendarChangeKind.Delete:
                changeEvents.RemoveAll(e => e.Id == args.Event.Id);
                break;
        }

        return InvokeAsync(StateHasChanged);
    }


    // Binding
    private BitFullCalendarView bindingView = BitFullCalendarView.Week;
    private BitFullCalendarMode _bindingMode = BitFullCalendarMode.Event;
    private BitFullCalendarMode bindingMode
    {
        get => _bindingMode;
        set
        {
            _bindingMode = value;
            // Timeline mode only lays out Day/Week/Month, so an unsupported view is brought back to one it supports.
            if (value == BitFullCalendarMode.Timeline && bindingView is BitFullCalendarView.Year or BitFullCalendarView.Agenda)
                bindingView = BitFullCalendarView.Week;
        }
    }
    private DateTime bindingDate = DateTime.Today;
    private string? bindingLog;

    private void HandleViewChange(BitFullCalendarView view) => bindingLog = $"View changed to {view}";

    private void HandleModeChange(BitFullCalendarMode mode) => bindingLog = $"Mode changed to {mode}";

    private void HandleDateChange(BitFullCalendarDateChangeEventArgs args)
        => bindingLog = $"Range {args.Start:yyyy-MM-dd} → {args.End:yyyy-MM-dd} ({args.View})";


    // Loading
    private List<BitFullCalendarEvent> loadedEvents = [];
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

        loadedEvents = rangeEvents;
        loadedRange = $"{args.Start:yyyy-MM-dd} → {args.End:yyyy-MM-dd} ({loadedEvents.Count} events)";
        isLoading = false;
    }


    // Custom toolbar
    private bool hideHeader;
    private BitFullCalendar? toolbarCalendar;
    private string? visibleRange;

    private async Task ScrollToAfternoon()
    {
        if (toolbarCalendar is null) return;
        await toolbarCalendar.ScrollToTimeAsync(TimeSpan.FromHours(14));
    }

    private void ShowVisibleRange()
    {
        if (toolbarCalendar is null) return;
        var (start, end) = toolbarCalendar.GetVisibleRange();
        visibleRange = $"{start:yyyy-MM-dd} → {end:yyyy-MM-dd}";
    }


    // Cascading parameters
    private readonly BitFullCalendarParams[] calendarParams =
    [
        new()
        {
            Views = [BitFullCalendarView.Week, BitFullCalendarView.Month],
            DefaultView = BitFullCalendarView.Month,
            HideFilters = true,
            Settings = new() { Use24HourFormat = false, BadgeVariant = BitFullCalendarBadgeVariant.Dot }
        }
    ];


    // Style & Class
    private readonly BitFullCalendarClassStyles calendarStyles = new()
    {
        Header = "background: var(--bit-clr-bg-sec)",
        Event = "font-weight: 600",
        Dialog = "border-top: 4px solid var(--bit-clr-pri)"
    };


    private static List<BitFullCalendarEvent> CreateEventsBetween(DateTime start, DateTime end)
    {
        string[] titles = ["Client call", "Design sync", "Hiring panel", "Ops review", "Customer demo"];
        string[] colors = ["blue", "purple", "green", "orange", "red"];
        var events = new List<BitFullCalendarEvent>();
        var n = 0;
        for (var day = start.Date; day <= end.Date; day = day.AddDays(1))
        {
            if (day.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday) continue;
            var i = n++ % titles.Length;
            events.Add(new() { Id = $"load-{day:yyyyMMdd}", Title = titles[i], StartDate = day.AddHours(9 + i), EndDate = day.AddHours(10 + i), Color = colors[i] });
        }
        return events;
    }



    private readonly BitFullCalendarTexts persianTexts = new()
    {
        // View & mode tabs
        ViewDay = "روز",
        ViewWeek = "هفته",
        ViewMonth = "ماه",
        ViewYear = "سال",
        ViewAgenda = "برنامه",
        ModeEvent = "رویدادها",
        ModeTimeline = "خط زمانی",

        // Toolbar
        TodayButton = "امروز",
        AddEventButton = "افزودن رویداد",
        AddEventHoverHint = "افزودن رویداد",
        PreviousButtonTitle = "قبلی",
        NextButtonTitle = "بعدی",
        PreviousMonthAriaLabel = "ماه قبل",
        NextMonthAriaLabel = "ماه بعد",
        SettingsButtonTitle = "تنظیمات",

        // Filters
        FilterByColorAriaLabel = "فیلتر رویدادها بر اساس رنگ",
        FilterByPersonAriaLabel = "فیلتر رویدادها بر اساس شخص در نمای فعلی",
        AllColorsOption = "همه رنگ‌ها",
        AllPeopleOption = "همه افراد",
        UnnamedAttendee = "(بدون نام)",

        // Settings panel
        CalendarSettingsLabel = "تنظیمات تقویم",
        DotBadgeLabel = "نشان نقطه‌ای",
        TwentyFourHourFormatLabel = "قالب ۲۴ ساعته",
        DayStartsAtLabel = "شروع روز از",
        HourSuffix = "ساعت",
        SlotDurationLabel = "طول بازه",
        MinuteSuffix = "دقیقه",
        AgendaGroupByLabel = "گروه‌بندی برنامه بر اساس",
        AgendaGroupByDate = "تاریخ",
        AgendaGroupByColor = "رنگ",
        StackedEventsLabel = "چیدمان رویدادهای هم‌پوشان",
        ShowDayViewCalendarLabel = "نمایش تقویم در نمای روزانه",
        ShowWeekNumbersLabel = "نمایش شماره هفته",
        ShowCurrentTimeIndicatorLabel = "نمایش زمان جاری",
        HighlightBusinessHoursLabel = "برجسته‌سازی ساعات کاری",

        // Messages
        WeekMobileWarning = "نمای هفتگی برای دستگاه‌های کوچک توصیه نمی‌شود. لطفاً از رایانه استفاده کنید یا نمای روزانه را انتخاب کنید.",
        HappeningNowTitle = "در حال انجام",
        NoAppointmentsNow = "در حال حاضر قراری وجود ندارد",
        EventOverlapMessage = "این بازه زمانی روی آن منبع قبلاً رزرو شده است.",
        OutOfRangeMessage = "این تاریخ خارج از بازه مجاز است.",
        OutsideBusinessHoursMessage = "این زمان خارج از ساعات کاری است.",
        NavLinkDayAriaLabelFormat = "رفتن به {0}",
        NavLinkWeekAriaLabelFormat = "رفتن به هفته {0}",

        // Search & agenda
        SearchEventsPlaceholder = "جستجوی رویدادها...",
        NoEventsFound = "رویدادی یافت نشد.",
        EventListTitleFormat = "رویدادهای {0}",
        EventListCountFormat = "{0} رویداد",
        MoreEventsFormat = "+{0} بیشتر",
        WeekNumberFormat = "ه{0}",
        WeekNumberAriaLabelFormat = "هفته {0}",

        // Dialogs
        AddEventDialogTitle = "افزودن رویداد جدید",
        EditEventDialogTitle = "ویرایش رویداد",
        AddEventDialogSubtitle = "یک رویداد جدید برای تقویم خود ایجاد کنید.",
        EditEventDialogSubtitle = "رویداد موجود خود را تغییر دهید.",

        // Buttons
        CloseAriaLabel = "بستن",
        CloseButton = "بستن",
        CancelButton = "انصراف",
        EditButton = "ویرایش",
        DeleteButton = "حذف",
        CreateEventButton = "ایجاد رویداد",
        SaveChangesButton = "ذخیره تغییرات",

        // Event form fields
        TitleLabel = "عنوان",
        EventTitlePlaceholder = "عنوان رویداد",
        AllDayLabel = "تمام روز",
        StartDateTimeLabel = "تاریخ و زمان شروع",
        EndDateTimeLabel = "تاریخ و زمان پایان",
        ColorLabel = "رنگ",
        EventColorAriaLabel = "رنگ رویداد",
        DescriptionLabel = "توضیحات",
        EventDescriptionPlaceholder = "توضیحات رویداد",
        AttendeesLabel = "شرکت‌کنندگان",
        NoAttendeesText = "بدون شرکت‌کننده",
        FirstNamePlaceholder = "نام",
        LastNamePlaceholder = "نام خانوادگی",
        IdOptionalPlaceholder = "شناسه (اختیاری)",
        AddButton = "افزودن",
        RemoveAttendeeAriaLabel = "حذف شرکت‌کننده",

        // Event details
        StartDateLabel = "تاریخ شروع",
        EndDateLabel = "تاریخ پایان",
        AtWord = "در",

        // Validation
        ValidationTitleRequired = "عنوان الزامی است",
        ValidationDescriptionRequired = "توضیحات الزامی است",
        ValidationEndAfterStart = "تاریخ پایان باید بعد از تاریخ شروع باشد",
        ValidationAttendeeNameRequired = "نام یا نام خانوادگی الزامی است",

        // Resources & timeline
        ResizePreviewAriaLabel = "بازه زمانی جدید",
        ResourceLabel = "منبع",
        ResourceColumnHeader = "منبع",
        NoResourceLabel = "تخصیص‌نیافته",
        NoResourceOption = "(هیچ‌کدام)",
        NoResourcesMessage = "منبعی برای نمایش وجود ندارد."
    };



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

    private readonly List<BitFullCalendarColorOption> colorOptions =
    [
        new() { Id = "work", Title = "Work", Value = "#2563eb" },
        new() { Id = "personal", Title = "Personal", Value = "#16a34a" },
        new() { Id = "urgent", Title = "Urgent", Value = "#dc2626" },
    ];

    private readonly Dictionary<DateTime, string> holidays = new()
    {
        [DateTime.Today.AddDays(4)] = "Founders' Day",
        [DateTime.Today.AddDays(11)] = "Public holiday",
    };

    private static List<BitFullCalendarEvent> CreateColorEvents()
    {
        var today = DateTime.Today;
        return
        [
            new() { Id = "1", Title = "Planning", StartDate = today.AddHours(9), EndDate = today.AddHours(10), Color = "work" },
            new() { Id = "2", Title = "Gym", StartDate = today.AddHours(17), EndDate = today.AddHours(18), Color = "personal" },
            new() { Id = "3", Title = "Hotfix", StartDate = today.AddDays(1).AddHours(11), EndDate = today.AddDays(1).AddHours(12), Color = "urgent" },
            new() { Id = "4", Title = "Brand launch", StartDate = today.AddDays(2).AddHours(13), EndDate = today.AddDays(2).AddHours(15), Color = "#db2777" },
            new() { Id = "5", Title = "Imported", StartDate = today.AddDays(3).AddHours(10), EndDate = today.AddDays(3).AddHours(11), Color = "legacy" },
        ];
    }

    private static List<BitFullCalendarEvent> CreateAllDayEvents()
    {
        var today = DateTime.Today;
        var id = 200;
        return
        [
            new() { Id = (++id).ToString(), Title = "Company Holiday", Description = "Offices closed.", StartDate = today, EndDate = today.AddDays(1), Color = "green", IsAllDay = true },
            new() { Id = (++id).ToString(), Title = "Release Freeze", Description = "No deploys this week.", StartDate = today.AddDays(1), EndDate = today.AddDays(4), Color = "red", IsAllDay = true },
            new() { Id = (++id).ToString(), Title = "Alice on leave", StartDate = today.AddDays(-1), EndDate = today.AddDays(2), Color = "yellow", IsAllDay = true },
            new() { Id = (++id).ToString(), Title = "Team Standup", Description = "Daily sync with engineering.", StartDate = today.AddHours(9), EndDate = today.AddHours(9).AddMinutes(30), Color = "blue" },
            new() { Id = (++id).ToString(), Title = "Sprint Planning", Description = "Next sprint goals.", StartDate = today.AddHours(14), EndDate = today.AddHours(15).AddMinutes(30), Color = "purple" },
        ];
    }

    private static List<BitFullCalendarEvent> CreateRuleEvents()
    {
        var today = DateTime.Today;
        var id = 300;
        return
        [
            new() { Id = (++id).ToString(), Title = "Payroll run", Description = "Locked - cannot be moved.", StartDate = today.AddHours(9), EndDate = today.AddHours(10), Color = "red", IsReadOnly = true },
            new() { Id = (++id).ToString(), Title = "Design Review", Description = "Try dragging this onto the locked slot.", StartDate = today.AddHours(11), EndDate = today.AddHours(12), Color = "purple" },
            new() { Id = (++id).ToString(), Title = "Retro", Description = "Sprint retrospective.", StartDate = today.AddHours(15), EndDate = today.AddHours(16), Color = "blue" },
            new() { Id = (++id).ToString(), Title = "Lunch break", StartDate = today.AddHours(12), EndDate = today.AddHours(13), Color = "#94a3b8", IsBackground = true, IsBlocking = true, Recurrence = new() { Frequency = BitFullCalendarRecurrenceFrequency.Daily } },
            new() { Id = (++id).ToString(), Title = "Company offsite", StartDate = today.AddDays(2), EndDate = today.AddDays(2), Color = "green", IsAllDay = true, IsBackground = true },
        ];
    }

    private static List<BitFullCalendarEvent> CreateRecurringEvents()
    {
        var today = DateTime.Today;
        var id = 400;
        return
        [
            new()
            {
                Id = (++id).ToString(),
                Title = "Daily Standup",
                Description = "Every weekday at 09:00.",
                StartDate = today.AddHours(9),
                EndDate = today.AddHours(9).AddMinutes(15),
                Color = "blue",
                Recurrence = new()
                {
                    Frequency = BitFullCalendarRecurrenceFrequency.Weekly,
                    DaysOfWeek = [DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday, DayOfWeek.Thursday, DayOfWeek.Friday],
                    // The one on the third day from now was cancelled.
                    ExceptionDates = [today.AddDays(3)]
                }
            },
            new()
            {
                Id = (++id).ToString(),
                Title = "Sprint Review",
                Description = "Every other Friday, six times.",
                StartDate = today.AddHours(15),
                EndDate = today.AddHours(16),
                Color = "purple",
                Recurrence = new()
                {
                    Frequency = BitFullCalendarRecurrenceFrequency.Weekly,
                    Interval = 2,
                    DaysOfWeek = [DayOfWeek.Friday],
                    Count = 6
                }
            },
            new()
            {
                Id = (++id).ToString(),
                Title = "Board Meeting",
                Description = "The third Tuesday of every month for six months, plus one extra session.",
                StartDate = today.AddHours(11),
                EndDate = today.AddHours(12),
                Color = "red",
                Recurrence = new()
                {
                    Frequency = BitFullCalendarRecurrenceFrequency.Monthly,
                    WeekOfMonth = BitFullCalendarWeekOfMonth.Third,
                    DaysOfWeek = [DayOfWeek.Tuesday],
                    Until = today.AddMonths(6),
                    // A make-up session outside the pattern.
                    AdditionalDates = [today.AddDays(10)]
                }
            },
            new()
            {
                Id = (++id).ToString(),
                Title = "Backup Check",
                Description = "Every 15 days.",
                StartDate = today.AddHours(8),
                EndDate = today.AddHours(8).AddMinutes(30),
                Color = "green",
                Recurrence = new()
                {
                    Frequency = BitFullCalendarRecurrenceFrequency.Daily,
                    Interval = 15
                }
            },
            new()
            {
                Id = (++id).ToString(),
                Title = "Month-end Report",
                Description = "The last Friday of every month.",
                StartDate = today.AddHours(16),
                EndDate = today.AddHours(17),
                Color = "orange",
                Recurrence = new()
                {
                    Frequency = BitFullCalendarRecurrenceFrequency.Monthly,
                    WeekOfMonth = BitFullCalendarWeekOfMonth.Last,
                    DaysOfWeek = [DayOfWeek.Friday]
                }
            },
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
}
