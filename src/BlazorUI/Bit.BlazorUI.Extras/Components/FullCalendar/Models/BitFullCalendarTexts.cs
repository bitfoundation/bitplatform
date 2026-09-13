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

    public string BitFcTodayButton { get; set; } = "Today";
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

    public string RepeatLabel { get; set; } = "Repeat";
    public string RecurrenceNone { get; set; } = "Does not repeat";
    public string RecurrenceDaily { get; set; } = "Daily";
    public string RecurrenceWeekly { get; set; } = "Weekly";
    public string RecurrenceMonthly { get; set; } = "Monthly";
    public string RecurrenceYearly { get; set; } = "Yearly";
    public string RecurrenceIntervalLabel { get; set; } = "Repeat every";
    public string RecurrenceDayUnit { get; set; } = "day(s)";
    public string RecurrenceWeekUnit { get; set; } = "week(s)";
    public string RecurrenceMonthUnit { get; set; } = "month(s)";
    public string RecurrenceYearUnit { get; set; } = "year(s)";
    public string RecurrenceDaysOfWeekLabel { get; set; } = "Repeat on";
    public string RecurrencePatternLabel { get; set; } = "Repeat on";
    public string RecurrenceFirst { get; set; } = "first";
    public string RecurrenceSecond { get; set; } = "second";
    public string RecurrenceThird { get; set; } = "third";
    public string RecurrenceFourth { get; set; } = "fourth";
    public string RecurrenceLast { get; set; } = "last";
    public string RecurrenceEndsLabel { get; set; } = "Ends";
    public string RecurrenceEndsNever { get; set; } = "Never";
    public string RecurrenceEndsOnDate { get; set; } = "On a date";
    public string RecurrenceEndsAfterCount { get; set; } = "After a number of occurrences";
    public string RecurrenceUntilAriaLabel { get; set; } = "Last date";
    public string RecurrenceCountAriaLabel { get; set; } = "Number of occurrences";
    public string RecurrenceSkippedDatesLabel { get; set; } = "Skipped dates";
    public string RecurrenceExtraDatesLabel { get; set; } = "Extra dates";
    public string RecurrenceSkipDateButton { get; set; } = "Skip";
    public string RecurrenceAddDateButton { get; set; } = "Add";
    public string RecurrenceSkipDateAriaLabel { get; set; } = "Date to skip";
    public string RecurrenceExtraDateAriaLabel { get; set; } = "Date to add";
    public string RemoveDateAriaLabel { get; set; } = "Remove date";
    public string RecurringEventAriaLabel { get; set; } = "Recurring event";
    public string RecurrenceScopeEditTitle { get; set; } = "Edit recurring event";
    public string RecurrenceScopeDeleteTitle { get; set; } = "Delete recurring event";
    public string RecurrenceScopeThisEvent { get; set; } = "This event";
    public string RecurrenceScopeThisAndFollowing { get; set; } = "This and following events";
    public string RecurrenceScopeAllEvents { get; set; } = "All events";

    // Summary templates, joined with RecurrenceSummarySeparator and capitalized - for example
    // "Every 2 weeks, on Mon, Fri, until Oct 30, 2026".
    public string RecurrenceSummarySeparator { get; set; } = ", ";
    public string RecurrenceEveryFormat { get; set; } = "every {0} {1}";
    public string RecurrenceOnDaysFormat { get; set; } = "on {0}";
    public string RecurrenceOnDayOfMonthFormat { get; set; } = "on day {0}";
    public string RecurrenceOnWeekdayOfMonthFormat { get; set; } = "on the {0} {1}";
    public string RecurrenceOnDateOfYearFormat { get; set; } = "on {0}";
    public string RecurrenceOnWeekdayOfYearFormat { get; set; } = "on the {0} {1} of {2}";
    public string RecurrenceUntilFormat { get; set; } = "until {0}";
    public string RecurrenceCountFormat { get; set; } = "{0} times";

    public string ValidationRecurrenceDaysRequired { get; set; } = "Select at least one day";
    public string ValidationRecurrenceUntilAfterStart { get; set; } = "The last date cannot be before the start date";

    public string GetRecurrenceFrequencyLabel(BitFullCalendarRecurrenceFrequency frequency) => frequency switch
    {
        BitFullCalendarRecurrenceFrequency.Daily => RecurrenceDaily,
        BitFullCalendarRecurrenceFrequency.Weekly => RecurrenceWeekly,
        BitFullCalendarRecurrenceFrequency.Monthly => RecurrenceMonthly,
        BitFullCalendarRecurrenceFrequency.Yearly => RecurrenceYearly,
        _ => frequency.ToString()
    };

    public string GetRecurrenceUnitLabel(BitFullCalendarRecurrenceFrequency frequency) => frequency switch
    {
        BitFullCalendarRecurrenceFrequency.Daily => RecurrenceDayUnit,
        BitFullCalendarRecurrenceFrequency.Weekly => RecurrenceWeekUnit,
        BitFullCalendarRecurrenceFrequency.Monthly => RecurrenceMonthUnit,
        BitFullCalendarRecurrenceFrequency.Yearly => RecurrenceYearUnit,
        _ => frequency.ToString()
    };

    public string GetWeekOfMonthLabel(BitFullCalendarRecurrenceWeekOfMonth week) => week switch
    {
        BitFullCalendarRecurrenceWeekOfMonth.First => RecurrenceFirst,
        BitFullCalendarRecurrenceWeekOfMonth.Second => RecurrenceSecond,
        BitFullCalendarRecurrenceWeekOfMonth.Third => RecurrenceThird,
        BitFullCalendarRecurrenceWeekOfMonth.Fourth => RecurrenceFourth,
        BitFullCalendarRecurrenceWeekOfMonth.Last => RecurrenceLast,
        _ => week.ToString()
    };

    public string GetRecurrenceScopeLabel(BitFullCalendarRecurrenceEditScope scope) => scope switch
    {
        BitFullCalendarRecurrenceEditScope.ThisEvent => RecurrenceScopeThisEvent,
        BitFullCalendarRecurrenceEditScope.ThisAndFollowing => RecurrenceScopeThisAndFollowing,
        BitFullCalendarRecurrenceEditScope.AllEvents => RecurrenceScopeAllEvents,
        _ => scope.ToString()
    };

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

