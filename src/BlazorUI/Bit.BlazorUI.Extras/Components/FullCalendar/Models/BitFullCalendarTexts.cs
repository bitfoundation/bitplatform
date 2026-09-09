namespace Bit.BlazorUI;

public class BitFullCalendarTexts
{
    public string ViewDay { get; set; } = "Day";
    public string ViewWeek { get; set; } = "Week";
    public string ViewMonth { get; set; } = "Month";
    public string ViewYear { get; set; } = "Year";
    public string ViewAgenda { get; set; } = "Agenda";

    public string ModeEvent { get; set; } = "Events";
    public string ModeTimeline { get; set; } = "Timeline";

    /// <summary>Label of the "today" navigation button.</summary>
    public string TodayButton { get; set; } = "Today";

    /// <summary>
    /// Obsolete alias of <see cref="TodayButton"/>. The old name leaked the internal
    /// <c>BitFcTodayButton</c> component name into the public text bag; it forwards to
    /// <see cref="TodayButton"/> so existing assignments keep working.
    /// </summary>
    [Obsolete($"Use {nameof(TodayButton)} instead. This alias forwards to it and will be removed in a future release.")]
    public string BitFcTodayButton
    {
        get => TodayButton;
        set => TodayButton = value;
    }

    public string AddEventButton { get; set; } = "Add Event";
    public string AddEventHoverHint { get; set; } = "Add event";
    public string PreviousButtonTitle { get; set; } = "Previous";
    public string NextButtonTitle { get; set; } = "Next";
    public string PreviousMonthAriaLabel { get; set; } = "Previous month";
    public string NextMonthAriaLabel { get; set; } = "Next month";
    public string PickerHourAriaLabel { get; set; } = "Hour";
    public string PickerMinuteAriaLabel { get; set; } = "Minute";
    public string PickerMeridiemAriaLabel { get; set; } = "AM/PM";
    public string PickerSelectedDayAriaLabel { get; set; } = "selected";
    public string SettingsButtonTitle { get; set; } = "Settings";

    public string FilterByColorAriaLabel { get; set; } = "Filter events by color";
    public string FilterByPersonAriaLabel { get; set; } = "Filter events by person in current view";
    public string AllColorsOption { get; set; } = "All colors";
    public string AllPeopleOption { get; set; } = "All people";
    public string UnnamedAttendee { get; set; } = "(Unnamed)";

    public string CalendarSettingsLabel { get; set; } = "Calendar settings";
    public string DotBadgeLabel { get; set; } = "Dot badge";
    public string TwentyFourHourFormatLabel { get; set; } = "24-hour format";
    public string DayStartsAtLabel { get; set; } = "Day starts at";
    public string HourSuffix { get; set; } = "h";
    public string AgendaGroupByLabel { get; set; } = "Agenda group by";
    public string AgendaGroupByDate { get; set; } = "Date";
    public string AgendaGroupByColor { get; set; } = "Color";
    public string StackedEventsLabel { get; set; } = "Stack overlapping events";
    public string ShowDayViewCalendarLabel { get; set; } = "Show calendar in day view";
    public string ShowWeekNumbersLabel { get; set; } = "Show week numbers";
    public string ShowCurrentTimeIndicatorLabel { get; set; } = "Show current time";
    public string SlotDurationLabel { get; set; } = "Slot duration";
    public string MinuteSuffix { get; set; } = "min";

    public string WeekMobileWarning { get; set; } = "Weekly view is not recommended on smaller devices. Please switch to a desktop device or use the daily view instead.";
    public string HappeningNowTitle { get; set; } = "Happening now";
    public string NoAppointmentsNow { get; set; } = "No appointments at the moment";

    public string SearchEventsPlaceholder { get; set; } = "Search events...";
    public string NoEventsFound { get; set; } = "No events found.";

    // Full format templates ({0} = the relevant value) so localized strings control word order and
    // placement rather than concatenating fixed English fragments at the call site.
    public string EventListTitleFormat { get; set; } = "Events on {0}";
    public string EventListCountFormat { get; set; } = "{0} event(s)";
    public string MoreEventsFormat { get; set; } = "+{0} more";

    public string AddEventDialogTitle { get; set; } = "Add New Event";
    public string EditEventDialogTitle { get; set; } = "Edit Event";
    public string AddEventDialogSubtitle { get; set; } = "Create a new event for your calendar.";
    public string EditEventDialogSubtitle { get; set; } = "Modify your existing event.";

    public string CloseAriaLabel { get; set; } = "Close";
    public string CloseButton { get; set; } = "Close";
    public string CancelButton { get; set; } = "Cancel";
    public string EditButton { get; set; } = "Edit";
    public string DeleteButton { get; set; } = "Delete";
    public string CreateEventButton { get; set; } = "Create Event";
    public string SaveChangesButton { get; set; } = "Save Changes";

    public string TitleLabel { get; set; } = "Title";
    public string EventTitlePlaceholder { get; set; } = "Event title";
    public string StartDateTimeLabel { get; set; } = "Start Date & Time";
    public string EndDateTimeLabel { get; set; } = "End Date & Time";
    public string ColorLabel { get; set; } = "Color";
    public string EventColorAriaLabel { get; set; } = "Event color";
    public string DescriptionLabel { get; set; } = "Description";
    public string EventDescriptionPlaceholder { get; set; } = "Event description";
    public string AttendeesLabel { get; set; } = "Attendees";
    public string NoAttendeesText { get; set; } = "No attendees";
    public string FirstNamePlaceholder { get; set; } = "First name";
    public string LastNamePlaceholder { get; set; } = "Last name";
    public string IdOptionalPlaceholder { get; set; } = "ID (optional)";
    public string AddButton { get; set; } = "Add";
    public string RemoveAttendeeAriaLabel { get; set; } = "Remove attendee";

    public string StartDateLabel { get; set; } = "Start Date";
    public string EndDateLabel { get; set; } = "End Date";
    public string AtWord { get; set; } = "at";

    /// <summary>Label of the all-day switch in the add/edit dialog, and of the all-day row.</summary>
    public string AllDayLabel { get; set; } = "All day";

    /// <summary>
    /// Format template of the week-number cell ({0} = the ISO-8601 week number).
    /// </summary>
    public string WeekNumberFormat { get; set; } = "W{0}";

    /// <summary>Accessible name of a week-number cell ({0} = the ISO-8601 week number).</summary>
    public string WeekNumberAriaLabelFormat { get; set; } = "Week {0}";

    /// <summary>Notice shown when a move, resize, or save is refused because it would overlap another event.</summary>
    public string EventOverlapMessage { get; set; } = "This time range is already taken on that resource.";

    /// <summary>Notice shown when a move or resize is refused because it falls outside the allowed date range.</summary>
    public string OutOfRangeMessage { get; set; } = "That date is outside the allowed range.";

    public string ValidationTitleRequired { get; set; } = "Title is required";
    public string ValidationDescriptionRequired { get; set; } = "Description is required";
    public string ValidationEndAfterStart { get; set; } = "End date must be after start date";
    public string ValidationAttendeeNameRequired { get; set; } = "First name or last name is required";

    public string ResizePreviewAriaLabel { get; set; } = "New time range";

    public string ResourceLabel { get; set; } = "Resource";
    public string ResourceColumnHeader { get; set; } = "Resource";
    public string NoResourceLabel { get; set; } = "Unassigned";
    public string NoResourceOption { get; set; } = "(none)";
    public string NoResourcesMessage { get; set; } = "No resources to display.";

    public string GetViewLabel(BitFullCalendarView view) => view switch
    {
        BitFullCalendarView.Day => ViewDay,
        BitFullCalendarView.Week => ViewWeek,
        BitFullCalendarView.Month => ViewMonth,
        BitFullCalendarView.Year => ViewYear,
        BitFullCalendarView.Agenda => ViewAgenda,
        _ => view.ToString()
    };

    public string GetModeLabel(BitFullCalendarMode mode) => mode switch
    {
        BitFullCalendarMode.Event => ModeEvent,
        BitFullCalendarMode.Timeline => ModeTimeline,
        _ => mode.ToString()
    };
}

