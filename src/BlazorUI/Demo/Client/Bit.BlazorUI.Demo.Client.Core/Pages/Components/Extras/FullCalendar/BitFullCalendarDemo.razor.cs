namespace Bit.BlazorUI.Demo.Client.Core.Pages.Components.Extras.FullCalendar;

public partial class BitFullCalendarDemo
{
    private readonly List<ComponentParameter> componentParameters =
    [
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
            Name = "HideSettings",
            Type = "bool",
            DefaultValue = "false",
            Description = "When true, hides the built-in settings gear button. Settings can still be driven programmatically through the Settings parameter.",
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
            Name = "OnDateChange",
            Type = "EventCallback<BitFullCalendarDateChangeEventArgs>",
            DefaultValue = "",
            Description = "Raised when the visible date range changes after prev/next/today navigation or a view switch. Payload includes inclusive Start/End and the active View.",
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
            Description = "Raised when the calendar refuses a user-driven change: an overlap while Settings.AllowEventOverlap is false, a target outside the MinDate/MaxDate window, or an event marked IsReadOnly.",
            LinkType = LinkType.Link,
            Href = "#change-refusal-enum",
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
            Description = "Initial preferences - 12/24-hour time format, badge variant, day start hour, agenda grouping, and event card layout.",
            LinkType = LinkType.Link,
            Href = "#settings-class",
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
                new() { Name = "Monthly", Description = "Repeats every Interval months on the start date's day of the month. A month too short for that day is skipped rather than shifted.", Value = "2" },
                new() { Name = "Yearly", Description = "Repeats every Interval years on the start date's month and day. A 29 February series only occurs in leap years.", Value = "3" },
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
                new() { Name = "ReadOnly", Description = "The calendar - or the single event - is read-only, so nothing was changed.", Value = "1" },
                new() { Name = "Overlap", Description = "The resulting range would overlap another event on the same resource while Settings.AllowEventOverlap is false.", Value = "2" },
                new() { Name = "OutOfRange", Description = "The resulting range falls outside the MinDate/MaxDate window the calendar is allowed to show.", Value = "3" },
            ]
        },
    ];



    private readonly List<ComponentSubClass> componentSubClasses =
    [
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
                new() { Name = "Color", Type = "string", DefaultValue = "BitFullCalendarColorScheme.FallbackColorId", Description = "Identifier of the color matching a BitFullCalendarColorOption.Id from the configured palette.", LinkType = LinkType.Link, Href = "#color-option-class" },
                new() { Name = "Attendees", Type = "List<BitFullCalendarAttendee>", DefaultValue = "[]", Description = "People attending the event.", LinkType = LinkType.Link, Href = "#attendee-class" },
                new() { Name = "Resource", Type = "string?", DefaultValue = "null", Description = "Optional resource identifier linking this event to a BitFullCalendarResource. Used by the timeline view to place the event on the matching resource row. null or empty means the event is unassigned.", LinkType = LinkType.Link, Href = "#resource-class" },
                new() { Name = "IsAllDay", Type = "bool", DefaultValue = "false", Description = "Marks the event as lasting the whole day (or span of days) rather than a time range. It renders in the all-day row above the day and week time grids and its badge shows no clock time." },
                new() { Name = "IsReadOnly", Type = "bool", DefaultValue = "false", Description = "Locks this single event: it cannot be dragged, resized, edited, or deleted even while the calendar itself is editable. Reading its details keeps working." },
                new() { Name = "CssClass", Type = "string?", DefaultValue = "null", Description = "Extra CSS class(es) applied to every element that renders this event - the month badge, the day/week block, and the timeline block." },
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
                new() { Name = "DaysOfWeek", Type = "IReadOnlyList<DayOfWeek>?", DefaultValue = "null", Description = "Weekdays a weekly series occurs on. When null or empty the series follows the start date's own weekday. Ignored by the other frequencies." },
                new() { Name = "Count", Type = "int?", DefaultValue = "null", Description = "Total number of occurrences, counting the first. Null leaves the series open-ended unless Until closes it; when both are set, whichever ends the series first wins." },
                new() { Name = "Until", Type = "DateTime?", DefaultValue = "null", Description = "Last date the series may occur on (inclusive). Null leaves the series open-ended unless Count closes it." },
                new() { Name = "ExceptionDates", Type = "IReadOnlyList<DateTime>?", DefaultValue = "null", Description = "Dates the series skips - a cancelled occurrence, a holiday. Only the date part is compared, and a skipped date still counts against Count." },
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
            Description = "Configuration settings applied as initial defaults when the component mounts, or whenever a new instance is assigned to the Settings parameter.",
            Parameters =
            [
                new() { Name = "Use24HourFormat", Type = "bool", DefaultValue = "true", Description = "Uses 24-hour time format instead of 12-hour (AM/PM)." },
                new() { Name = "BadgeVariant", Type = "BitFullCalendarBadgeVariant", DefaultValue = "BitFullCalendarBadgeVariant.Colored", Description = "Badge display style in the month view.", LinkType = LinkType.Link, Href = "#badge-variant-enum" },
                new() { Name = "StartOfDayHour", Type = "int", DefaultValue = "8", Description = "Hour the day/week time grid scrolls to on first render. Clamped into the VisibleStartHour/VisibleEndHour window." },
                new() { Name = "VisibleStartHour", Type = "int", DefaultValue = "0", Description = "First hour rendered by the day, week, and timeline day/week time grids (0–23). Equivalent to slotMinTime in other calendar libraries." },
                new() { Name = "VisibleEndHour", Type = "int", DefaultValue = "24", Description = "Exclusive last hour rendered by the day, week, and timeline day/week time grids (1–24). Equivalent to slotMaxTime. A value at or below VisibleStartHour is corrected to one hour past it." },
                new() { Name = "SlotDurationMinutes", Type = "int", DefaultValue = "30", Description = "Length in minutes of one slot inside an hour, and the granularity drag-and-drop and resizing snap to. Only divisors of 60 are accepted (5, 6, 10, 12, 15, 20, 30, 60); other values round to the nearest accepted one." },
                new() { Name = "HiddenDays", Type = "IReadOnlyList<DayOfWeek>?", DefaultValue = "null", Description = "Days of the week removed from every date grid (week, month, year, and the timeline week layout). Use it to render a work week. A list that would hide every day is ignored." },
                new() { Name = "FirstDayOfWeek", Type = "DayOfWeek?", DefaultValue = "null", Description = "Overrides the day the week starts on. When null the active culture's DateTimeFormat.FirstDayOfWeek is used." },
                new() { Name = "ShowWeekNumbers", Type = "bool", DefaultValue = "false", Description = "Renders the ISO-8601 week number in the month grid and in the week view's time gutter." },
                new() { Name = "ShowCurrentTimeIndicator", Type = "bool", DefaultValue = "true", Description = "Renders the current time indicator line on the day and week time grids." },
                new() { Name = "MaxEventsPerDayCell", Type = "int", DefaultValue = "3", Description = "Number of event badges a month-grid cell renders before the rest collapse behind the \"+N more\" affordance. Clamped to 1–10." },
                new() { Name = "RequireEventDescription", Type = "bool", DefaultValue = "false", Description = "Makes the description field of the built-in add/edit dialog mandatory. Only the title is required by default." },
                new() { Name = "AllowEventOverlap", Type = "bool", DefaultValue = "true", Description = "Allows two events on the same resource to occupy the same time range. When false, a drag, resize, or dialog save that would create an overlap is refused." },
                new() { Name = "AllowRangeSelection", Type = "bool", DefaultValue = "true", Description = "Lets the user press and drag across the day or week time grid to select a range of slots, which opens the new event already spanning it. A plain click still creates a one-slot event." },
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
                new() { Name = "TodayButton", Type = "string", DefaultValue = "\"Today\"", Description = "Label for the today navigation button." },
                new() { Name = "BitFcTodayButton", Type = "string", DefaultValue = "\"Today\"", Description = "Obsolete alias of TodayButton; it forwards to it and will be removed in a future release." },
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
                new() { Name = "ValidationTitleRequired", Type = "string", DefaultValue = "\"Title is required\"", Description = "Validation message when the title is empty." },
                new() { Name = "ValidationDescriptionRequired", Type = "string", DefaultValue = "\"Description is required\"", Description = "Validation message when the description is empty." },
                new() { Name = "ValidationEndAfterStart", Type = "string", DefaultValue = "\"End date must be after start date\"", Description = "Validation message when the end date is not after the start date." },
                new() { Name = "ValidationAttendeeNameRequired", Type = "string", DefaultValue = "\"First name or last name is required\"", Description = "Validation message when an attendee has no name." },
                new() { Name = "ResizePreviewAriaLabel", Type = "string", DefaultValue = "\"New time range\"", Description = "Aria label for the resize preview indicator." },
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



    private readonly List<BitFullCalendarEvent> basicEvents = CreateEvents();
    private readonly List<BitFullCalendarEvent> settingsEvents = CreateEvents();
    private readonly List<BitFullCalendarEvent> gridEvents = CreateEvents();
    private readonly List<BitFullCalendarEvent> workWeekEvents = CreateEvents();
    private readonly List<BitFullCalendarEvent> allDayEvents = CreateAllDayEvents();
    private readonly List<BitFullCalendarEvent> layoutEvents = CreateEvents();
    private readonly List<BitFullCalendarEvent> templateEvents = CreateEvents();
    private readonly List<BitFullCalendarEvent> boundsEvents = CreateEvents();
    private readonly List<BitFullCalendarEvent> rulesEvents = CreateRuleEvents();
    private readonly List<BitFullCalendarEvent> changeEvents = CreateEvents();
    private readonly List<BitFullCalendarEvent> apiEvents = CreateEvents();
    private readonly List<BitFullCalendarEvent> localizationEvents = CreateEvents();
    private readonly List<BitFullCalendarEvent> viewsEvents = CreateEvents();
    private readonly List<BitFullCalendarEvent> readOnlyEvents = CreateEvents();
    private readonly List<BitFullCalendarEvent> recurringEvents = CreateRecurringEvents();
    private readonly List<BitFullCalendarEvent> rtlEvents = CreateEvents();

    private readonly BitFullCalendarSettings settings = new()
    {
        Use24HourFormat = false,
        StartOfDayHour = 7,
        BadgeVariant = BitFullCalendarBadgeVariant.Dot
    };

    private readonly BitFullCalendarSettings gridSettings = new()
    {
        VisibleStartHour = 8,
        VisibleEndHour = 18,
        SlotDurationMinutes = 30,
        StartOfDayHour = 9
    };

    // The settings object is mutated in place: the calendar diffs it against the values it last
    // applied, so only what actually changed is pushed back into the live state.
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

    private BitFullCalendarEventLayout layoutMode
    {
        get => layoutSettings.EventLayout;
        set => layoutSettings.EventLayout = value;
    }

    private readonly BitFullCalendarSettings layoutSettings = new()
    {
        EventLayout = BitFullCalendarEventLayout.Stack
    };

    private readonly DateTime boundsMin = DateTime.Today.AddDays(-10);
    private readonly DateTime boundsMax = DateTime.Today.AddDays(20);

    private readonly BitFullCalendarSettings rulesSettings = new()
    {
        AllowEventOverlap = false,
        VisibleStartHour = 8,
        VisibleEndHour = 18
    };

    private bool allowOverlap
    {
        get => rulesSettings.AllowEventOverlap;
        set => rulesSettings.AllowEventOverlap = value;
    }

    private string? lastRefusal;

    private void HandleRefused(BitFullCalendarChangeRefusal refusal) => lastRefusal = refusal.ToString();

    private bool isReadOnly = true;

    private string viewsPreset = "week-day";

    private BitFullCalendarView[] SelectedViews => viewsPreset switch
    {
        "month-agenda" => [BitFullCalendarView.Month, BitFullCalendarView.Agenda],
        "month" => [BitFullCalendarView.Month],
        _ => [BitFullCalendarView.Week, BitFullCalendarView.Day]
    };

    private BitFullCalendar? apiCalendar;
    private string? apiRange;

    private void ShowVisibleRange()
    {
        if (apiCalendar is null) return;
        var (start, end) = apiCalendar.GetVisibleRange();
        apiRange = $"{start:yyyy-MM-dd} → {end:yyyy-MM-dd}";
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

        // Messages
        WeekMobileWarning = "نمای هفتگی برای دستگاه‌های کوچک توصیه نمی‌شود. لطفاً از رایانه استفاده کنید یا نمای روزانه را انتخاب کنید.",
        HappeningNowTitle = "در حال انجام",
        NoAppointmentsNow = "در حال حاضر قراری وجود ندارد",
        EventOverlapMessage = "این بازه زمانی روی آن منبع قبلاً رزرو شده است.",
        OutOfRangeMessage = "این تاریخ خارج از بازه مجاز است.",

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

    private readonly List<BitFullCalendarResource> resources =
    [
        new() { Id = "room-bay", Title = "HQ - Bay Wing", Subtitle = "Headquarters" },
        new() { Id = "room-garden", Title = "The Garden", Subtitle = "Headquarters" },
        new() { Id = "room-war", Title = "War Room (B1)", Subtitle = "Basement" },
    ];

    private readonly List<BitFullCalendarEvent> resourceEvents = CreateResourceEvents();

    private readonly List<BitFullCalendarEvent> bindingEvents = CreateResourceEvents();
    private BitFullCalendarView bindingView = BitFullCalendarView.Week;
    private BitFullCalendarMode _bindingMode = BitFullCalendarMode.Event;
    private BitFullCalendarMode bindingMode
    {
        get => _bindingMode;
        set
        {
            _bindingMode = value;
            // Timeline mode only supports Day/Week/Month; coerce an unsupported view (Year/Agenda)
            // back to a supported one so the bound View can't drift out of sync with the rendered view.
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

    private string? lastChange;

    private Task HandleChange(BitFullCalendarChangeEventArgs args)
    {
        lastChange = $"{args.Kind} ({args.Source}): {args.Event.Title}";

        // Persist the change into our backing list so it stays in sync with the calendar's internal
        // state. The calendar copies Events into its own store, so without applying the add/edit/delete
        // here a later re-render would re-sync this stale list and discard the user's change.
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
                Title = "Payroll",
                Description = "Monthly, until the end of next quarter.",
                StartDate = today.AddHours(11),
                EndDate = today.AddHours(12),
                Color = "red",
                Recurrence = new()
                {
                    Frequency = BitFullCalendarRecurrenceFrequency.Monthly,
                    Until = today.AddMonths(6)
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

    private readonly string example1RazorCode = @"<BitFullCalendar Events=""events"" />

@code {" + eventsCode + @"
}";

    private readonly string example2RazorCode = @"<BitFullCalendar Events=""events"" Settings=""settings"" />

@code {
    private readonly BitFullCalendarSettings settings = new()
    {
        Use24HourFormat = false,
        StartOfDayHour = 7,
        BadgeVariant = BitFullCalendarBadgeVariant.Dot
    };
" + eventsCode + @"
}";

    private readonly string example3RazorCode = @"<BitChoiceGroup Horizontal
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

<BitFullCalendar Events=""events"" Settings=""gridSettings"" DefaultView=""BitFullCalendarView.Week"" />

@code {
    private readonly BitFullCalendarSettings gridSettings = new()
    {
        VisibleStartHour = 8,
        VisibleEndHour = 18,
        SlotDurationMinutes = 30,
        StartOfDayHour = 9
    };

    // The settings object is mutated in place: the calendar diffs it against the values it last
    // applied, so only what actually changed is pushed back into the live state.
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
" + eventsCode + @"
}";

    private readonly string example4RazorCode = @"<BitChoiceGroup Horizontal
                Label=""Days""
                TItem=""BitChoiceGroupOption<string>""
                TValue=""string""
                @bind-Value=""workWeekPreset"">
    <BitChoiceGroupOption Text=""Full week"" Value=""@(""full"")"" />
    <BitChoiceGroupOption Text=""Mon - Fri"" Value=""@(""mon-fri"")"" />
    <BitChoiceGroupOption Text=""Sun - Thu"" Value=""@(""sun-thu"")"" />
</BitChoiceGroup>
<BitToggle @bind-Value=""workWeekNumbers"" Text=""Week numbers"" />

<BitFullCalendar Events=""events"" Settings=""workWeekSettings"" DefaultView=""BitFullCalendarView.Week"" />

@code {
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
                ""mon-fri"" => (new[] { DayOfWeek.Saturday, DayOfWeek.Sunday }, DayOfWeek.Monday),
                ""sun-thu"" => (new[] { DayOfWeek.Friday, DayOfWeek.Saturday }, DayOfWeek.Sunday),
                _ => (null, (DayOfWeek?)null)
            };
        }
    }

    private bool workWeekNumbers
    {
        get => workWeekSettings.ShowWeekNumbers;
        set => workWeekSettings.ShowWeekNumbers = value;
    }
" + eventsCode + @"
}";

    private readonly string example5RazorCode = @"<BitFullCalendar Events=""events"" DefaultView=""BitFullCalendarView.Week"" />

@code {
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
    }
}";

    private readonly string example6RazorCode = @"<BitChoiceGroup Horizontal
                Label=""Event layout""
                TItem=""BitChoiceGroupOption<BitFullCalendarEventLayout>""
                TValue=""BitFullCalendarEventLayout""
                @bind-Value=""layoutMode"">
    <BitChoiceGroupOption Text=""Overlap"" Value=""BitFullCalendarEventLayout.Overlap"" />
    <BitChoiceGroupOption Text=""Stack"" Value=""BitFullCalendarEventLayout.Stack"" />
</BitChoiceGroup>

<BitFullCalendar Events=""events"" Settings=""layoutSettings"" />

@code {
    private readonly BitFullCalendarSettings layoutSettings = new()
    {
        EventLayout = BitFullCalendarEventLayout.Stack
    };

    private BitFullCalendarEventLayout layoutMode
    {
        get => layoutSettings.EventLayout;
        set => layoutSettings.EventLayout = value;
    }
" + eventsCode + @"
}";

    private readonly string example7RazorCode = @"<BitFullCalendar Events=""events""
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
" + eventsCode + @"
}";

    private readonly string example8RazorCode = @"<BitFullCalendar Events=""events""
                 Resources=""resources""
                 DefaultMode=""BitFullCalendarMode.Timeline"" />

@code {" + resourcesCode + @"
}";

    private readonly string example9RazorCode = @"<BitFullCalendar Events=""events""
                 MinDate=""boundsMin""
                 MaxDate=""boundsMax""
                 DefaultView=""BitFullCalendarView.Month"" />

@code {
    private readonly DateTime boundsMin = DateTime.Today.AddDays(-10);
    private readonly DateTime boundsMax = DateTime.Today.AddDays(20);
" + eventsCode + @"
}";

    private readonly string example10RazorCode = @"<BitToggle @bind-Value=""allowOverlap"" Text=""Allow overlapping events"" />

<BitFullCalendar Events=""events"" Settings=""rulesSettings"" OnRefused=""HandleRefused"" DefaultView=""BitFullCalendarView.Day"" />

<BitText>Last refusal: <b>@(lastRefusal ?? ""-"")</b></BitText>

@code {
    private readonly BitFullCalendarSettings rulesSettings = new()
    {
        AllowEventOverlap = false,
        VisibleStartHour = 8,
        VisibleEndHour = 18
    };

    private bool allowOverlap
    {
        get => rulesSettings.AllowEventOverlap;
        set => rulesSettings.AllowEventOverlap = value;
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
        ];
    }
}";

    private readonly string example11RazorCode = @"<BitFullCalendar Events=""events"" OnChange=""HandleChange"" />

<BitText>Last change: <b>@(lastChange ?? ""-"")</b></BitText>

@code {
    private string? lastChange;

    private Task HandleChange(BitFullCalendarChangeEventArgs args)
    {
        lastChange = $""{args.Kind} ({args.Source}): {args.Event.Title}"";

        // Keep the bound list in sync with the calendar's internal state so changes are not lost on re-render.
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
" + eventsCode + @"
}";

    private readonly string example12RazorCode = @"<BitChoiceGroup Horizontal Label=""View""
                TItem=""BitChoiceGroupOption<BitFullCalendarView>""
                TValue=""BitFullCalendarView""
                @bind-Value=""bindingView"">
    <BitChoiceGroupOption Text=""Day"" Value=""BitFullCalendarView.Day"" />
    <BitChoiceGroupOption Text=""Week"" Value=""BitFullCalendarView.Week"" />
    <BitChoiceGroupOption Text=""Month"" Value=""BitFullCalendarView.Month"" />
    @if (bindingMode == BitFullCalendarMode.Event)
    {
        <BitChoiceGroupOption Text=""Year"" Value=""BitFullCalendarView.Year"" />
        <BitChoiceGroupOption Text=""Agenda"" Value=""BitFullCalendarView.Agenda"" />
    }
</BitChoiceGroup>
<BitChoiceGroup Horizontal Label=""Mode""
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
<BitText>Last calendar event: <b>@(bindingLog ?? ""-"")</b></BitText>

@code {
    private BitFullCalendarView bindingView = BitFullCalendarView.Week;
    private BitFullCalendarMode _bindingMode = BitFullCalendarMode.Event;
    private BitFullCalendarMode bindingMode
    {
        get => _bindingMode;
        set
        {
            _bindingMode = value;
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
" + resourcesCode + @"
}";

    private readonly string example13RazorCode = @"<BitButton Variant=""BitVariant.Outline"" OnClick=""() => calendar?.NavigatePrevious()"">Previous</BitButton>
<BitButton Variant=""BitVariant.Outline"" OnClick=""() => calendar?.GoToToday()"">Today</BitButton>
<BitButton Variant=""BitVariant.Outline"" OnClick=""() => calendar?.NavigateNext()"">Next</BitButton>
<BitButton Variant=""BitVariant.Outline"" OnClick=""() => calendar?.ChangeView(BitFullCalendarView.Day)"">Day view</BitButton>
<BitButton Variant=""BitVariant.Outline"" OnClick=""() => calendar?.ChangeView(BitFullCalendarView.Month)"">Month view</BitButton>
<BitButton Variant=""BitVariant.Outline"" OnClick=""ShowVisibleRange"">Read visible range</BitButton>

<BitFullCalendar @ref=""calendar"" Events=""events"" />

<BitText>Visible range: <b>@(range ?? ""-"")</b></BitText>

@code {
    private BitFullCalendar? calendar;
    private string? range;

    private void ShowVisibleRange()
    {
        if (calendar is null) return;
        var (start, end) = calendar.GetVisibleRange();
        range = $""{start:yyyy-MM-dd} → {end:yyyy-MM-dd}"";
    }
" + eventsCode + @"
}";

    private readonly string example14RazorCode = @"<BitFullCalendar Events=""events"" CultureName=""fa-IR"" Texts=""persianTexts"" />

@code {
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

        // Messages
        WeekMobileWarning = ""نمای هفتگی برای دستگاه‌های کوچک توصیه نمی‌شود. لطفاً از رایانه استفاده کنید یا نمای روزانه را انتخاب کنید."",
        HappeningNowTitle = ""در حال انجام"",
        NoAppointmentsNow = ""در حال حاضر قراری وجود ندارد"",
        EventOverlapMessage = ""این بازه زمانی روی آن منبع قبلاً رزرو شده است."",
        OutOfRangeMessage = ""این تاریخ خارج از بازه مجاز است."",

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
" + eventsCode + @"
}";

    private readonly string example15RazorCode = @"<BitFullCalendar Events=""events"" HideFilters HideSettings />

@code {" + eventsCode + @"
}";

    private readonly string example16RazorCode = @"<BitChoiceGroup Horizontal
                Label=""Available views""
                TItem=""BitChoiceGroupOption<string>""
                TValue=""string""
                @bind-Value=""viewsPreset"">
    <BitChoiceGroupOption Text=""Week and Day"" Value=""@(""week-day"")"" />
    <BitChoiceGroupOption Text=""Month and Agenda"" Value=""@(""month-agenda"")"" />
    <BitChoiceGroupOption Text=""Month only"" Value=""@(""month"")"" />
</BitChoiceGroup>

<BitFullCalendar Events=""events"" Views=""SelectedViews"" />

@code {
    private string viewsPreset = ""week-day"";

    private BitFullCalendarView[] SelectedViews => viewsPreset switch
    {
        ""month-agenda"" => [BitFullCalendarView.Month, BitFullCalendarView.Agenda],
        ""month"" => [BitFullCalendarView.Month],
        _ => [BitFullCalendarView.Week, BitFullCalendarView.Day]
    };
" + eventsCode + @"
}";

    private readonly string example17RazorCode = @"<BitToggle @bind-Value=""isReadOnly"" Text=""Read-only"" />

<BitFullCalendar Events=""events"" ReadOnly=""isReadOnly"" />

@code {
    private bool isReadOnly = true;
" + eventsCode + @"
}";

    private readonly string example18RazorCode = @"<BitFullCalendar Events=""events"" DefaultView=""BitFullCalendarView.Month"" />

@code {
    private readonly List<BitFullCalendarEvent> events = CreateRecurringEvents();

    private static List<BitFullCalendarEvent> CreateRecurringEvents()
    {
        var today = DateTime.Today;
        var id = 400;
        return
        [
            new()
            {
                Id = (++id).ToString(),
                Title = ""Daily Standup"",
                Description = ""Every weekday at 09:00."",
                StartDate = today.AddHours(9),
                EndDate = today.AddHours(9).AddMinutes(15),
                Color = ""blue"",
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
                Title = ""Sprint Review"",
                Description = ""Every other Friday, six times."",
                StartDate = today.AddHours(15),
                EndDate = today.AddHours(16),
                Color = ""purple"",
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
                Title = ""Payroll"",
                Description = ""Monthly, until the end of next quarter."",
                StartDate = today.AddHours(11),
                EndDate = today.AddHours(12),
                Color = ""red"",
                Recurrence = new()
                {
                    Frequency = BitFullCalendarRecurrenceFrequency.Monthly,
                    Until = today.AddMonths(6)
                }
            },
        ];
    }
}";

    private readonly string example19RazorCode = @"<BitFullCalendar Events=""events""
                 Style=""--bit-bfc-height:420px;--bit-bfc-hour-height:64px;border:2px dashed var(--bit-clr-pri);border-radius:8px"" />

@code {" + eventsCode + @"
}";

    private readonly string example20RazorCode = @"<BitFullCalendar Dir=""BitDir.Rtl"" Events=""events"" />

@code {" + eventsCode + @"
}";
}
