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
            Description = "The unit a recurrence rule repeats in.",
            Items =
            [
                new() { Name = "Daily", Description = "Repeats every Interval days.", Value = "0" },
                new() { Name = "Weekly", Description = "Repeats on the DaysOfWeek of every Interval weeks.", Value = "1" },
                new() { Name = "Monthly", Description = "Repeats every Interval months, on the start's day of the month or on the weekday picked with WeekOfMonth.", Value = "2" },
                new() { Name = "Yearly", Description = "Repeats every Interval years in the start's month, on its day of the month or on the weekday picked with WeekOfMonth.", Value = "3" },
            ]
        },
        new()
        {
            Id = "recurrence-week-of-month-enum",
            Name = "BitFullCalendarRecurrenceWeekOfMonth",
            Description = "The week of the month a monthly or yearly rule picks its weekday in.",
            Items =
            [
                new() { Name = "First", Description = "The first such weekday of the month.", Value = "0" },
                new() { Name = "Second", Description = "The second such weekday of the month.", Value = "1" },
                new() { Name = "Third", Description = "The third such weekday of the month.", Value = "2" },
                new() { Name = "Fourth", Description = "The fourth such weekday of the month.", Value = "3" },
                new() { Name = "Last", Description = "The last such weekday of the month, whether it is the fourth or the fifth.", Value = "4" },
            ]
        },
        new()
        {
            Id = "recurrence-edit-scope-enum",
            Name = "BitFullCalendarRecurrenceEditScope",
            Description = "Which part of a recurring series an edit or a delete made on one of its occurrences applies to.",
            Items =
            [
                new() { Name = "ThisEvent", Description = "Only that occurrence: the series skips its date, and an edited occurrence becomes an event of its own.", Value = "0" },
                new() { Name = "ThisAndFollowing", Description = "That occurrence and every later one: the series ends the day before it, and an edited occurrence starts a new series.", Value = "1" },
                new() { Name = "AllEvents", Description = "Every occurrence: the series itself is edited or deleted.", Value = "2" },
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
                new() { Name = "IsSingleDay", Type = "bool", DefaultValue = "", Description = "Read-only. True when the event starts and ends on the same date." },
                new() { Name = "IsMultiDay", Type = "bool", DefaultValue = "", Description = "Read-only. True when the event spans more than one date." },
                new() { Name = "Duration", Type = "TimeSpan", DefaultValue = "", Description = "Read-only. The difference between EndDate and StartDate." },
                new() { Name = "Recurrence", Type = "BitFullCalendarRecurrence?", DefaultValue = "null", Description = "Makes the event a recurring series: the calendar renders the occurrences the rule produces instead of the event itself, the first one at StartDate, each lasting Duration. On an occurrence, the rule of its series.", LinkType = LinkType.Link, Href = "#recurrence-class" },
                new() { Name = "RecurringEventId", Type = "string?", DefaultValue = "null", Description = "Set on the occurrences the calendar generates - the events its views, templates, and OnEventClick receive - to the Id of their series." },
                new() { Name = "OccurrenceDate", Type = "DateTime?", DefaultValue = "null", Description = "Set on an occurrence to the start it has in its series, which is what identifies it there (see ExceptionDates)." },
                new() { Name = "IsRecurring", Type = "bool", DefaultValue = "", Description = "Read-only. True for the event defining a recurring series: it has a Recurrence and is not an occurrence." },
                new() { Name = "IsOccurrence", Type = "bool", DefaultValue = "", Description = "Read-only. True for an occurrence generated from a recurring series." },
                new() { Name = "Data", Type = "object?", DefaultValue = "null", Description = "Optional consumer-defined payload available to templates and click handlers." },
            ]
        },
        new()
        {
            Id = "recurrence-class",
            Title = "BitFullCalendarRecurrence",
            Description = "Describes how an event repeats. The event's StartDate is always the first occurrence, and every occurrence keeps the event's time of day and duration. Monthly and yearly rules follow the calendar's culture, so with fa-IR they repeat on the same day of the Persian month.",
            Parameters =
            [
                new() { Name = "Frequency", Type = "BitFullCalendarRecurrenceFrequency", DefaultValue = "BitFullCalendarRecurrenceFrequency.Daily", Description = "The unit the rule repeats in.", LinkType = LinkType.Link, Href = "#recurrence-frequency-enum" },
                new() { Name = "Interval", Type = "int", DefaultValue = "1", Description = "How many units lie between two repetitions - 2 with a weekly frequency repeats every other week, 15 with a daily one every 15 days. Values below 1 are stored as 1." },
                new() { Name = "DaysOfWeek", Type = "List<DayOfWeek>", DefaultValue = "[]", Description = "Weekly: every listed day of each repeated week. Monthly and yearly with a WeekOfMonth: the weekdays picked inside the month. Empty means the weekday the series starts on." },
                new() { Name = "WeekOfMonth", Type = "BitFullCalendarRecurrenceWeekOfMonth?", DefaultValue = "null", Description = "Monthly and yearly only: lands on that week's DaysOfWeek of the month, like the third Tuesday. null lands on the start's day of the month and skips months too short to have it.", LinkType = LinkType.Link, Href = "#recurrence-week-of-month-enum" },
                new() { Name = "Count", Type = "int?", DefaultValue = "null", Description = "The number of occurrences, the first one included. Skipped dates still count and extra dates never do. null for no limit." },
                new() { Name = "Until", Type = "DateTime?", DefaultValue = "null", Description = "The last date an occurrence may start on, inclusive; its time of day is ignored. null for no end date." },
                new() { Name = "ExceptionDates", Type = "List<DateTime>", DefaultValue = "[]", Description = "Dates skipped without disturbing the rest of the pattern, compared by date only." },
                new() { Name = "AdditionalDates", Type = "List<DateTime>", DefaultValue = "[]", Description = "Extra dates that get an occurrence at the series' time of day, compared by date only. Not limited by Count or Until; a date that is also an exception date is skipped." },
                new() { Name = "Clone()", Type = "BitFullCalendarRecurrence", DefaultValue = "", Description = "Creates a copy of the rule whose lists are independent of this one." },
                new() { Name = "GetOccurrences(DateTime seriesStart, DateTime rangeStart, DateTime rangeEnd, CultureInfo? culture)", Type = "List<DateTime>", DefaultValue = "", Description = "The starts, in chronological order, of the occurrences of a series starting at seriesStart that begin inside the inclusive range." },
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
                new() { Name = "StartOfDayHour", Type = "int", DefaultValue = "8", Description = "Hour (0–16) at which the day/week time grid begins." },
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
                new() { Name = "BitFcTodayButton", Type = "string", DefaultValue = "\"Today\"", Description = "Label for the today navigation button." },
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
                new() { Name = "RepeatLabel", Type = "string", DefaultValue = "\"Repeat\"", Description = "Label for the repeat field in the add/edit dialog and the recurrence row in the event details." },
                new() { Name = "RecurrenceNone", Type = "string", DefaultValue = "\"Does not repeat\"", Description = "Option text for an event that does not repeat." },
                new() { Name = "RecurrenceDaily", Type = "string", DefaultValue = "\"Daily\"", Description = "Option text and summary for a daily rule." },
                new() { Name = "RecurrenceWeekly", Type = "string", DefaultValue = "\"Weekly\"", Description = "Option text and summary for a weekly rule." },
                new() { Name = "RecurrenceMonthly", Type = "string", DefaultValue = "\"Monthly\"", Description = "Option text and summary for a monthly rule." },
                new() { Name = "RecurrenceYearly", Type = "string", DefaultValue = "\"Yearly\"", Description = "Option text and summary for a yearly rule." },
                new() { Name = "RecurrenceIntervalLabel", Type = "string", DefaultValue = "\"Repeat every\"", Description = "Label for the repeat interval field." },
                new() { Name = "RecurrenceDayUnit", Type = "string", DefaultValue = "\"day(s)\"", Description = "Unit shown after the interval of a daily rule." },
                new() { Name = "RecurrenceWeekUnit", Type = "string", DefaultValue = "\"week(s)\"", Description = "Unit shown after the interval of a weekly rule." },
                new() { Name = "RecurrenceMonthUnit", Type = "string", DefaultValue = "\"month(s)\"", Description = "Unit shown after the interval of a monthly rule." },
                new() { Name = "RecurrenceYearUnit", Type = "string", DefaultValue = "\"year(s)\"", Description = "Unit shown after the interval of a yearly rule." },
                new() { Name = "RecurrenceDaysOfWeekLabel", Type = "string", DefaultValue = "\"Repeat on\"", Description = "Label for the weekday toggles of a weekly rule." },
                new() { Name = "RecurrencePatternLabel", Type = "string", DefaultValue = "\"Repeat on\"", Description = "Label for the day-of-month pattern of a monthly or yearly rule." },
                new() { Name = "RecurrenceFirst", Type = "string", DefaultValue = "\"first\"", Description = "Ordinal for the first week of the month." },
                new() { Name = "RecurrenceSecond", Type = "string", DefaultValue = "\"second\"", Description = "Ordinal for the second week of the month." },
                new() { Name = "RecurrenceThird", Type = "string", DefaultValue = "\"third\"", Description = "Ordinal for the third week of the month." },
                new() { Name = "RecurrenceFourth", Type = "string", DefaultValue = "\"fourth\"", Description = "Ordinal for the fourth week of the month." },
                new() { Name = "RecurrenceLast", Type = "string", DefaultValue = "\"last\"", Description = "Ordinal for the last week of the month." },
                new() { Name = "RecurrenceEndsLabel", Type = "string", DefaultValue = "\"Ends\"", Description = "Label for the field choosing how the rule ends." },
                new() { Name = "RecurrenceEndsNever", Type = "string", DefaultValue = "\"Never\"", Description = "Option text for a rule without an end." },
                new() { Name = "RecurrenceEndsOnDate", Type = "string", DefaultValue = "\"On a date\"", Description = "Option text for a rule ending on a date." },
                new() { Name = "RecurrenceEndsAfterCount", Type = "string", DefaultValue = "\"After a number of occurrences\"", Description = "Option text for a rule ending after a number of occurrences." },
                new() { Name = "RecurrenceUntilAriaLabel", Type = "string", DefaultValue = "\"Last date\"", Description = "Aria label for the picker of the rule's last date." },
                new() { Name = "RecurrenceCountAriaLabel", Type = "string", DefaultValue = "\"Number of occurrences\"", Description = "Aria label for the occurrence count field." },
                new() { Name = "RecurrenceSkippedDatesLabel", Type = "string", DefaultValue = "\"Skipped dates\"", Description = "Label for the rule's exception dates." },
                new() { Name = "RecurrenceExtraDatesLabel", Type = "string", DefaultValue = "\"Extra dates\"", Description = "Label for the rule's additional dates." },
                new() { Name = "RecurrenceSkipDateButton", Type = "string", DefaultValue = "\"Skip\"", Description = "Label for the button adding an exception date." },
                new() { Name = "RecurrenceAddDateButton", Type = "string", DefaultValue = "\"Add\"", Description = "Label for the button adding an additional date." },
                new() { Name = "RecurrenceSkipDateAriaLabel", Type = "string", DefaultValue = "\"Date to skip\"", Description = "Aria label for the picker of a new exception date." },
                new() { Name = "RecurrenceExtraDateAriaLabel", Type = "string", DefaultValue = "\"Date to add\"", Description = "Aria label for the picker of a new additional date." },
                new() { Name = "RemoveDateAriaLabel", Type = "string", DefaultValue = "\"Remove date\"", Description = "Aria label for the remove button on a skipped or extra date chip." },
                new() { Name = "RecurringEventAriaLabel", Type = "string", DefaultValue = "\"Recurring event\"", Description = "Accessible name of the glyph marking an occurrence on event cards, badges, and agenda rows." },
                new() { Name = "RecurrenceScopeEditTitle", Type = "string", DefaultValue = "\"Edit recurring event\"", Description = "Prompt asking which occurrences an edit applies to." },
                new() { Name = "RecurrenceScopeDeleteTitle", Type = "string", DefaultValue = "\"Delete recurring event\"", Description = "Prompt asking which occurrences a delete applies to." },
                new() { Name = "RecurrenceScopeThisEvent", Type = "string", DefaultValue = "\"This event\"", Description = "Choice applying an edit or delete to the occurrence alone." },
                new() { Name = "RecurrenceScopeThisAndFollowing", Type = "string", DefaultValue = "\"This and following events\"", Description = "Choice applying an edit or delete to the occurrence and every later one." },
                new() { Name = "RecurrenceScopeAllEvents", Type = "string", DefaultValue = "\"All events\"", Description = "Choice applying an edit or delete to the whole series." },
                new() { Name = "RecurrenceSummarySeparator", Type = "string", DefaultValue = "\", \"", Description = "Separator joining the parts of a recurrence summary and its list of weekdays." },
                new() { Name = "RecurrenceEveryFormat", Type = "string", DefaultValue = "\"every {0} {1}\"", Description = "Summary part for an interval above 1; {0} is the interval, {1} the unit." },
                new() { Name = "RecurrenceOnDaysFormat", Type = "string", DefaultValue = "\"on {0}\"", Description = "Summary part for the weekdays of a weekly rule; {0} is the list of days." },
                new() { Name = "RecurrenceOnDayOfMonthFormat", Type = "string", DefaultValue = "\"on day {0}\"", Description = "Pattern of a monthly rule on a day of the month; {0} is the day." },
                new() { Name = "RecurrenceOnWeekdayOfMonthFormat", Type = "string", DefaultValue = "\"on the {0} {1}\"", Description = "Pattern of a monthly rule on a weekday; {0} is the ordinal, {1} the weekday." },
                new() { Name = "RecurrenceOnDateOfYearFormat", Type = "string", DefaultValue = "\"on {0}\"", Description = "Pattern of a yearly rule on a date; {0} is the month and day." },
                new() { Name = "RecurrenceOnWeekdayOfYearFormat", Type = "string", DefaultValue = "\"on the {0} {1} of {2}\"", Description = "Pattern of a yearly rule on a weekday; {0} is the ordinal, {1} the weekday, {2} the month." },
                new() { Name = "RecurrenceUntilFormat", Type = "string", DefaultValue = "\"until {0}\"", Description = "Summary part for a rule's last date; {0} is the date." },
                new() { Name = "RecurrenceCountFormat", Type = "string", DefaultValue = "\"{0} times\"", Description = "Summary part for a rule's occurrence count; {0} is the count." },
                new() { Name = "ValidationRecurrenceDaysRequired", Type = "string", DefaultValue = "\"Select at least one day\"", Description = "Validation message when a weekly rule has no weekday selected." },
                new() { Name = "ValidationRecurrenceUntilAfterStart", Type = "string", DefaultValue = "\"The last date cannot be before the start date\"", Description = "Validation message when the rule's last date is before the event's start." },
                new() { Name = "GetViewLabel(BitFullCalendarView)", Type = "string", DefaultValue = "", Description = "Method that returns the localized label for the given view." },
                new() { Name = "GetModeLabel(BitFullCalendarMode)", Type = "string", DefaultValue = "", Description = "Method that returns the localized label for the given mode." },
                new() { Name = "GetRecurrenceFrequencyLabel(BitFullCalendarRecurrenceFrequency)", Type = "string", DefaultValue = "", Description = "Method that returns the localized label for the given frequency." },
                new() { Name = "GetRecurrenceUnitLabel(BitFullCalendarRecurrenceFrequency)", Type = "string", DefaultValue = "", Description = "Method that returns the localized interval unit for the given frequency." },
                new() { Name = "GetWeekOfMonthLabel(BitFullCalendarRecurrenceWeekOfMonth)", Type = "string", DefaultValue = "", Description = "Method that returns the localized ordinal for the given week of the month." },
                new() { Name = "GetRecurrenceScopeLabel(BitFullCalendarRecurrenceEditScope)", Type = "string", DefaultValue = "", Description = "Method that returns the localized label for the given edit scope." },
            ]
        },
    ];



    private readonly List<BitFullCalendarEvent> basicEvents = CreateEvents();
    private readonly List<BitFullCalendarEvent> settingsEvents = CreateEvents();
    private readonly List<BitFullCalendarEvent> templateEvents = CreateEvents();
    private readonly List<BitFullCalendarEvent> changeEvents = CreateEvents();
    private readonly List<BitFullCalendarEvent> localizationEvents = CreateEvents();
    private readonly List<BitFullCalendarEvent> layoutEvents = CreateEvents();
    private readonly List<BitFullCalendarEvent> viewsEvents = CreateEvents();
    private readonly List<BitFullCalendarEvent> readOnlyEvents = CreateEvents();

    private bool isReadOnly = true;

    private string viewsPreset = "week-day";

    private BitFullCalendarView[] SelectedViews => viewsPreset switch
    {
        "month-agenda" => [BitFullCalendarView.Month, BitFullCalendarView.Agenda],
        "month" => [BitFullCalendarView.Month],
        _ => [BitFullCalendarView.Week, BitFullCalendarView.Day]
    };

    private BitFullCalendarEventLayout layoutMode = BitFullCalendarEventLayout.Stack;
    private BitFullCalendarSettings layoutSettings = new()
    {
        EventLayout = BitFullCalendarEventLayout.Stack
    };

    private void HandleLayoutChange(BitFullCalendarEventLayout layout)
    {
        layoutMode = layout;
        // Assign a new settings instance so the calendar re-applies the layout (the
        // Settings parameter is re-applied only when a new reference is supplied).
        layoutSettings = new() { EventLayout = layout };
    }

    private readonly BitFullCalendarSettings settings = new()
    {
        Use24HourFormat = false,
        StartOfDayHour = 7,
        BadgeVariant = BitFullCalendarBadgeVariant.Dot
    };

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
        BitFcTodayButton = "امروز",
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
        AgendaGroupByLabel = "گروه‌بندی برنامه بر اساس",
        AgendaGroupByDate = "تاریخ",
        AgendaGroupByColor = "رنگ",
        StackedEventsLabel = "چیدمان رویدادهای هم‌پوشان",
        ShowDayViewCalendarLabel = "نمایش تقویم در نمای روزانه",

        // Messages
        WeekMobileWarning = "نمای هفتگی برای دستگاه‌های کوچک توصیه نمی‌شود. لطفاً از رایانه استفاده کنید یا نمای روزانه را انتخاب کنید.",
        HappeningNowTitle = "در حال انجام",
        NoAppointmentsNow = "در حال حاضر قراری وجود ندارد",

        // Search & agenda
        SearchEventsPlaceholder = "جستجوی رویدادها...",
        NoEventsFound = "رویدادی یافت نشد.",
        EventListTitleFormat = "رویدادهای {0}",
        EventListCountFormat = "{0} رویداد",
        MoreEventsFormat = "+{0} بیشتر",

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
        NoResourcesMessage = "منبعی برای نمایش وجود ندارد.",

        // Recurrence
        RepeatLabel = "تکرار",
        RecurrenceNone = "بدون تکرار",
        RecurrenceDaily = "روزانه",
        RecurrenceWeekly = "هفتگی",
        RecurrenceMonthly = "ماهانه",
        RecurrenceYearly = "سالانه",
        RecurrenceIntervalLabel = "تکرار هر",
        RecurrenceDayUnit = "روز",
        RecurrenceWeekUnit = "هفته",
        RecurrenceMonthUnit = "ماه",
        RecurrenceYearUnit = "سال",
        RecurrenceDaysOfWeekLabel = "تکرار در روزهای",
        RecurrencePatternLabel = "تکرار در",
        RecurrenceFirst = "اولین",
        RecurrenceSecond = "دومین",
        RecurrenceThird = "سومین",
        RecurrenceFourth = "چهارمین",
        RecurrenceLast = "آخرین",
        RecurrenceEndsLabel = "پایان",
        RecurrenceEndsNever = "هرگز",
        RecurrenceEndsOnDate = "در یک تاریخ",
        RecurrenceEndsAfterCount = "پس از چند بار تکرار",
        RecurrenceUntilAriaLabel = "آخرین تاریخ",
        RecurrenceCountAriaLabel = "تعداد تکرارها",
        RecurrenceSkippedDatesLabel = "تاریخ‌های نادیده‌گرفته",
        RecurrenceExtraDatesLabel = "تاریخ‌های اضافه",
        RecurrenceSkipDateButton = "نادیده‌گرفتن",
        RecurrenceAddDateButton = "افزودن",
        RecurrenceSkipDateAriaLabel = "تاریخ نادیده‌گرفته",
        RecurrenceExtraDateAriaLabel = "تاریخ اضافه",
        RemoveDateAriaLabel = "حذف تاریخ",
        RecurringEventAriaLabel = "رویداد تکرارشونده",
        RecurrenceScopeEditTitle = "ویرایش رویداد تکرارشونده",
        RecurrenceScopeDeleteTitle = "حذف رویداد تکرارشونده",
        RecurrenceScopeThisEvent = "همین رویداد",
        RecurrenceScopeThisAndFollowing = "این رویداد و رویدادهای بعدی",
        RecurrenceScopeAllEvents = "همه رویدادها",
        RecurrenceSummarySeparator = "، ",
        RecurrenceEveryFormat = "هر {0} {1}",
        RecurrenceOnDaysFormat = "در {0}",
        RecurrenceOnDayOfMonthFormat = "در روز {0}",
        RecurrenceOnWeekdayOfMonthFormat = "در {0} {1}",
        RecurrenceOnDateOfYearFormat = "در {0}",
        RecurrenceOnWeekdayOfYearFormat = "در {0} {1} {2}",
        RecurrenceUntilFormat = "تا {0}",
        RecurrenceCountFormat = "{0} بار",
        ValidationRecurrenceDaysRequired = "حداقل یک روز را انتخاب کنید",
        ValidationRecurrenceUntilAfterStart = "آخرین تاریخ نمی‌تواند قبل از تاریخ شروع باشد"
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



    private readonly List<BitFullCalendarEvent> recurringEvents = CreateRecurringEvents();
    private readonly List<string> recurringChanges = [];

    private Task HandleRecurringChange(BitFullCalendarChangeEventArgs args)
    {
        // Editing, deleting, dragging, or resizing an occurrence arrives as ordinary Add/Edit/Delete changes -
        // for example an Edit of the series that skips a date, then an Add of the occurrence split from it.
        recurringChanges.Insert(0, $"{args.Kind} ({args.Source}): {args.Event.Title}");
        if (recurringChanges.Count > 3)
            recurringChanges.RemoveAt(3);

        switch (args.Kind)
        {
            case BitFullCalendarChangeKind.Add:
                recurringEvents.Add(args.Event);
                break;
            case BitFullCalendarChangeKind.Edit:
                var index = recurringEvents.FindIndex(e => e.Id == args.Event.Id);
                if (index >= 0)
                    recurringEvents[index] = args.Event;
                else
                    recurringEvents.Add(args.Event);
                break;
            case BitFullCalendarChangeKind.Delete:
                recurringEvents.RemoveAll(e => e.Id == args.Event.Id);
                break;
        }

        return InvokeAsync(StateHasChanged);
    }

    private static List<BitFullCalendarEvent> CreateRecurringEvents()
    {
        var today = DateTime.Today;
        var monday = today.AddDays(-(((int)today.DayOfWeek + 6) % 7));
        var firstOfMonth = today.AddDays(1 - today.Day);
        var thirdTuesday = firstOfMonth.AddDays(((int)DayOfWeek.Tuesday - (int)firstOfMonth.DayOfWeek + 7) % 7 + 14);

        return
        [
            new()
            {
                Id = "standup",
                Title = "Team Standup",
                Description = "Every weekday, except tomorrow.",
                StartDate = monday.AddHours(9),
                EndDate = monday.AddHours(9).AddMinutes(30),
                Color = "blue",
                Recurrence = new()
                {
                    Frequency = BitFullCalendarRecurrenceFrequency.Weekly,
                    DaysOfWeek = [DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday, DayOfWeek.Thursday, DayOfWeek.Friday],
                    ExceptionDates = [today.AddDays(1)]
                }
            },
            new()
            {
                Id = "gym",
                Title = "Gym",
                Description = "Monday, Wednesday, and Friday - plus this Saturday.",
                StartDate = monday.AddHours(18),
                EndDate = monday.AddHours(19),
                Color = "green",
                Recurrence = new()
                {
                    Frequency = BitFullCalendarRecurrenceFrequency.Weekly,
                    DaysOfWeek = [DayOfWeek.Monday, DayOfWeek.Wednesday, DayOfWeek.Friday],
                    AdditionalDates = [monday.AddDays(5)]
                }
            },
            new()
            {
                Id = "sprint-review",
                Title = "Sprint Review",
                Description = "Every 2 weeks on Friday, 6 times.",
                StartDate = monday.AddDays(4).AddHours(15),
                EndDate = monday.AddDays(4).AddHours(16),
                Color = "purple",
                Recurrence = new()
                {
                    Frequency = BitFullCalendarRecurrenceFrequency.Weekly,
                    Interval = 2,
                    Count = 6
                }
            },
            new()
            {
                Id = "all-hands",
                Title = "All-Hands",
                Description = "The third Tuesday of every month.",
                StartDate = thirdTuesday.AddHours(11),
                EndDate = thirdTuesday.AddHours(12),
                Color = "orange",
                Recurrence = new()
                {
                    Frequency = BitFullCalendarRecurrenceFrequency.Monthly,
                    WeekOfMonth = BitFullCalendarRecurrenceWeekOfMonth.Third,
                    DaysOfWeek = [DayOfWeek.Tuesday]
                }
            },
            new()
            {
                Id = "backup",
                Title = "Server Backup",
                Description = "Every 15 days.",
                StartDate = today.AddDays(-20).AddHours(21),
                EndDate = today.AddDays(-20).AddHours(22),
                Color = "red",
                Recurrence = new()
                {
                    Frequency = BitFullCalendarRecurrenceFrequency.Daily,
                    Interval = 15
                }
            },
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

    private readonly string example4RazorCode = @"<BitFullCalendar Events=""events""
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

    private readonly string example5RazorCode = @"<BitFullCalendar Events=""events""
                 Resources=""resources""
                 DefaultMode=""BitFullCalendarMode.Timeline"" />

@code {
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
    }
}";

    private readonly string example6RazorCode = @"<BitFullCalendar Events=""events"" OnChange=""HandleChange"" />
<br />
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

    private readonly string example8RazorCode = @"<BitFullCalendar Events=""events"" CultureName=""fa-IR"" Texts=""persianTexts"" />

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
        BitFcTodayButton = ""امروز"",
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
        AgendaGroupByLabel = ""گروه‌بندی برنامه بر اساس"",
        AgendaGroupByDate = ""تاریخ"",
        AgendaGroupByColor = ""رنگ"",
        StackedEventsLabel = ""چیدمان رویدادهای هم‌پوشان"",
        ShowDayViewCalendarLabel = ""نمایش تقویم در نمای روزانه"",

        // Messages
        WeekMobileWarning = ""نمای هفتگی برای دستگاه‌های کوچک توصیه نمی‌شود. لطفاً از رایانه استفاده کنید یا نمای روزانه را انتخاب کنید."",
        HappeningNowTitle = ""در حال انجام"",
        NoAppointmentsNow = ""در حال حاضر قراری وجود ندارد"",

        // Search & agenda
        SearchEventsPlaceholder = ""جستجوی رویدادها..."",
        NoEventsFound = ""رویدادی یافت نشد."",
        EventListTitleFormat = ""رویدادهای {0}"",
        EventListCountFormat = ""{0} رویداد"",
        MoreEventsFormat = ""+{0} بیشتر"",

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
        NoResourcesMessage = ""منبعی برای نمایش وجود ندارد."",

        // Recurrence
        RepeatLabel = ""تکرار"",
        RecurrenceNone = ""بدون تکرار"",
        RecurrenceDaily = ""روزانه"",
        RecurrenceWeekly = ""هفتگی"",
        RecurrenceMonthly = ""ماهانه"",
        RecurrenceYearly = ""سالانه"",
        RecurrenceIntervalLabel = ""تکرار هر"",
        RecurrenceDayUnit = ""روز"",
        RecurrenceWeekUnit = ""هفته"",
        RecurrenceMonthUnit = ""ماه"",
        RecurrenceYearUnit = ""سال"",
        RecurrenceDaysOfWeekLabel = ""تکرار در روزهای"",
        RecurrencePatternLabel = ""تکرار در"",
        RecurrenceFirst = ""اولین"",
        RecurrenceSecond = ""دومین"",
        RecurrenceThird = ""سومین"",
        RecurrenceFourth = ""چهارمین"",
        RecurrenceLast = ""آخرین"",
        RecurrenceEndsLabel = ""پایان"",
        RecurrenceEndsNever = ""هرگز"",
        RecurrenceEndsOnDate = ""در یک تاریخ"",
        RecurrenceEndsAfterCount = ""پس از چند بار تکرار"",
        RecurrenceUntilAriaLabel = ""آخرین تاریخ"",
        RecurrenceCountAriaLabel = ""تعداد تکرارها"",
        RecurrenceSkippedDatesLabel = ""تاریخ‌های نادیده‌گرفته"",
        RecurrenceExtraDatesLabel = ""تاریخ‌های اضافه"",
        RecurrenceSkipDateButton = ""نادیده‌گرفتن"",
        RecurrenceAddDateButton = ""افزودن"",
        RecurrenceSkipDateAriaLabel = ""تاریخ نادیده‌گرفته"",
        RecurrenceExtraDateAriaLabel = ""تاریخ اضافه"",
        RemoveDateAriaLabel = ""حذف تاریخ"",
        RecurringEventAriaLabel = ""رویداد تکرارشونده"",
        RecurrenceScopeEditTitle = ""ویرایش رویداد تکرارشونده"",
        RecurrenceScopeDeleteTitle = ""حذف رویداد تکرارشونده"",
        RecurrenceScopeThisEvent = ""همین رویداد"",
        RecurrenceScopeThisAndFollowing = ""این رویداد و رویدادهای بعدی"",
        RecurrenceScopeAllEvents = ""همه رویدادها"",
        RecurrenceSummarySeparator = ""، "",
        RecurrenceEveryFormat = ""هر {0} {1}"",
        RecurrenceOnDaysFormat = ""در {0}"",
        RecurrenceOnDayOfMonthFormat = ""در روز {0}"",
        RecurrenceOnWeekdayOfMonthFormat = ""در {0} {1}"",
        RecurrenceOnDateOfYearFormat = ""در {0}"",
        RecurrenceOnWeekdayOfYearFormat = ""در {0} {1} {2}"",
        RecurrenceUntilFormat = ""تا {0}"",
        RecurrenceCountFormat = ""{0} بار"",
        ValidationRecurrenceDaysRequired = ""حداقل یک روز را انتخاب کنید"",
        ValidationRecurrenceUntilAfterStart = ""آخرین تاریخ نمی‌تواند قبل از تاریخ شروع باشد""
    };
" + eventsCode + @"
}";

    private readonly string example9RazorCode = @"<BitFullCalendar Events=""events"" HideFilters HideSettings />

@code {" + eventsCode + @"
}";

    private readonly string example10RazorCode = @"<BitChoiceGroup Horizontal
                Label=""Available views""
                TItem=""BitChoiceGroupOption<string>""
                TValue=""string""
                @bind-Value=""viewsPreset"">
    <BitChoiceGroupOption Text=""Week and Day"" Value=""@(""week-day"")"" />
    <BitChoiceGroupOption Text=""Month and Agenda"" Value=""@(""month-agenda"")"" />
    <BitChoiceGroupOption Text=""Month only"" Value=""@(""month"")"" />
</BitChoiceGroup>
<br />
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

    private readonly string example11RazorCode = @"<BitToggle @bind-Value=""isReadOnly"" Text=""Read-only"" />
<br />
<BitFullCalendar Events=""events"" ReadOnly=""isReadOnly"" />

@code {
    private bool isReadOnly = true;
" + eventsCode + @"
}";

    private readonly string example7RazorCode = @"<BitChoiceGroup Horizontal Label=""View""
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
    }
}";

    private readonly string example3RazorCode = @"<BitChoiceGroup Horizontal
                Label=""Event layout""
                TItem=""BitChoiceGroupOption<BitFullCalendarEventLayout>""
                TValue=""BitFullCalendarEventLayout""
                Value=""layoutMode""
                OnChange=""HandleLayoutChange"">
    <BitChoiceGroupOption Text=""Overlap"" Value=""BitFullCalendarEventLayout.Overlap"" />
    <BitChoiceGroupOption Text=""Stack"" Value=""BitFullCalendarEventLayout.Stack"" />
</BitChoiceGroup>
<br />
<BitFullCalendar Events=""events"" Settings=""layoutSettings"" />

@code {
    private BitFullCalendarEventLayout layoutMode = BitFullCalendarEventLayout.Stack;
    private BitFullCalendarSettings layoutSettings = new()
    {
        EventLayout = BitFullCalendarEventLayout.Stack
    };

    private void HandleLayoutChange(BitFullCalendarEventLayout layout)
    {
        layoutMode = layout;
        // Assign a new settings instance so the calendar re-applies the layout (the
        // Settings parameter is re-applied only when a new reference is supplied).
        layoutSettings = new() { EventLayout = layout };
    }
" + eventsCode + @"
}";

    private readonly string example12RazorCode = @"<BitFullCalendar Events=""events"" OnChange=""HandleChange"" />
<br />
<BitText>Last changes: <b>@(changes.Count > 0 ? string.Join("" | "", changes) : ""-"")</b></BitText>

@code {
    private readonly List<string> changes = [];

    private Task HandleChange(BitFullCalendarChangeEventArgs args)
    {
        // Editing, deleting, dragging, or resizing an occurrence arrives as ordinary Add/Edit/Delete changes -
        // for example an Edit of the series that skips a date, then an Add of the occurrence split from it.
        changes.Insert(0, $""{args.Kind} ({args.Source}): {args.Event.Title}"");
        if (changes.Count > 3)
            changes.RemoveAt(3);

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

    private readonly List<BitFullCalendarEvent> events = CreateEvents();

    private static List<BitFullCalendarEvent> CreateEvents()
    {
        var today = DateTime.Today;
        var monday = today.AddDays(-(((int)today.DayOfWeek + 6) % 7));
        var firstOfMonth = today.AddDays(1 - today.Day);
        var thirdTuesday = firstOfMonth.AddDays(((int)DayOfWeek.Tuesday - (int)firstOfMonth.DayOfWeek + 7) % 7 + 14);

        return
        [
            new()
            {
                Id = ""standup"",
                Title = ""Team Standup"",
                Description = ""Every weekday, except tomorrow."",
                StartDate = monday.AddHours(9),
                EndDate = monday.AddHours(9).AddMinutes(30),
                Color = ""blue"",
                Recurrence = new()
                {
                    Frequency = BitFullCalendarRecurrenceFrequency.Weekly,
                    DaysOfWeek = [DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday, DayOfWeek.Thursday, DayOfWeek.Friday],
                    ExceptionDates = [today.AddDays(1)]
                }
            },
            new()
            {
                Id = ""gym"",
                Title = ""Gym"",
                Description = ""Monday, Wednesday, and Friday - plus this Saturday."",
                StartDate = monday.AddHours(18),
                EndDate = monday.AddHours(19),
                Color = ""green"",
                Recurrence = new()
                {
                    Frequency = BitFullCalendarRecurrenceFrequency.Weekly,
                    DaysOfWeek = [DayOfWeek.Monday, DayOfWeek.Wednesday, DayOfWeek.Friday],
                    AdditionalDates = [monday.AddDays(5)]
                }
            },
            new()
            {
                Id = ""sprint-review"",
                Title = ""Sprint Review"",
                Description = ""Every 2 weeks on Friday, 6 times."",
                StartDate = monday.AddDays(4).AddHours(15),
                EndDate = monday.AddDays(4).AddHours(16),
                Color = ""purple"",
                Recurrence = new()
                {
                    Frequency = BitFullCalendarRecurrenceFrequency.Weekly,
                    Interval = 2,
                    Count = 6
                }
            },
            new()
            {
                Id = ""all-hands"",
                Title = ""All-Hands"",
                Description = ""The third Tuesday of every month."",
                StartDate = thirdTuesday.AddHours(11),
                EndDate = thirdTuesday.AddHours(12),
                Color = ""orange"",
                Recurrence = new()
                {
                    Frequency = BitFullCalendarRecurrenceFrequency.Monthly,
                    WeekOfMonth = BitFullCalendarRecurrenceWeekOfMonth.Third,
                    DaysOfWeek = [DayOfWeek.Tuesday]
                }
            },
            new()
            {
                Id = ""backup"",
                Title = ""Server Backup"",
                Description = ""Every 15 days."",
                StartDate = today.AddDays(-20).AddHours(21),
                EndDate = today.AddDays(-20).AddHours(22),
                Color = ""red"",
                Recurrence = new()
                {
                    Frequency = BitFullCalendarRecurrenceFrequency.Daily,
                    Interval = 15
                }
            },
        ];
    }
}";
}
