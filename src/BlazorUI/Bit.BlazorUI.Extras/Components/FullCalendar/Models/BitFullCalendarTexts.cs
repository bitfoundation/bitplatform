namespace Bit.BlazorUI;

/// <summary>
/// Every word <see cref="BitFullCalendar"/> shows or announces (<see cref="BitFullCalendar.Texts"/>), in English by
/// default: set the ones to translate or reword. A text ending in <c>Format</c> is a composite format string whose
/// placeholders are described on it, so a translation decides the word order.
/// </summary>
public class BitFullCalendarTexts
{
    /// <summary>Name of the day view on the view tabs (<see cref="GetViewLabel"/>).</summary>
    public string ViewDay { get; set; } = "Day";

    /// <summary>Name of the week view on the view tabs (<see cref="GetViewLabel"/>).</summary>
    public string ViewWeek { get; set; } = "Week";

    /// <summary>Name of the month view on the view tabs (<see cref="GetViewLabel"/>).</summary>
    public string ViewMonth { get; set; } = "Month";

    /// <summary>Name of the year view on the view tabs (<see cref="GetViewLabel"/>).</summary>
    public string ViewYear { get; set; } = "Year";

    /// <summary>Name of the agenda view on the view tabs (<see cref="GetViewLabel"/>).</summary>
    public string ViewAgenda { get; set; } = "Agenda";

    /// <summary>Name of the events mode on the mode tabs (<see cref="GetModeLabel"/>).</summary>
    public string ModeEvent { get; set; } = "Events";

    /// <summary>Name of the timeline mode on the mode tabs (<see cref="GetModeLabel"/>).</summary>
    public string ModeTimeline { get; set; } = "Timeline";

    /// <summary>Accessible name of the view tab strip (day, week, month, year, agenda).</summary>
    public string ViewTabsAriaLabel { get; set; } = "Views";

    /// <summary>Accessible name of the mode tab strip (events, timeline).</summary>
    public string ModeTabsAriaLabel { get; set; } = "Modes";

    /// <summary>Label of the "today" navigation button.</summary>
    public string TodayButton { get; set; } = "Today";

    /// <summary>Label of the header's button that opens the add dialog.</summary>
    public string AddEventButton { get; set; } = "Add Event";

    /// <summary>
    /// The hint a time slot of the day and week views, or a month cell, shows under the pointer or the keyboard focus to
    /// say an event can be added there. It also starts their accessible names, ahead of the time or the date.
    /// </summary>
    public string AddEventHoverHint { get; set; } = "Add event";

    /// <summary>Tooltip and accessible name of the header's button that goes back one day, week, month or year.</summary>
    public string PreviousButtonTitle { get; set; } = "Previous";

    /// <summary>Tooltip and accessible name of the header's button that goes forward one day, week, month or year.</summary>
    public string NextButtonTitle { get; set; } = "Next";

    /// <summary>
    /// Accessible name of the button that shows the previous month, in the day view's mini calendar and in the date
    /// pickers of the add/edit dialog.
    /// </summary>
    public string PreviousMonthAriaLabel { get; set; } = "Previous month";

    /// <summary>
    /// Accessible name of the button that shows the next month, in the day view's mini calendar and in the date
    /// pickers of the add/edit dialog.
    /// </summary>
    public string NextMonthAriaLabel { get; set; } = "Next month";

    /// <summary>Accessible name of the hour field of the add/edit dialog's start and end pickers.</summary>
    public string PickerHourAriaLabel { get; set; } = "Hour";

    /// <summary>Accessible name of the minute field of the add/edit dialog's start and end pickers.</summary>
    public string PickerMinuteAriaLabel { get; set; } = "Minute";

    /// <summary>
    /// Accessible name of the AM/PM field of the add/edit dialog's start and end pickers, shown in the 12-hour format.
    /// </summary>
    public string PickerMeridiemAriaLabel { get; set; } = "AM/PM";

    /// <summary>
    /// Added to the accessible name of the selected day, after its date, in the add/edit dialog's date pickers and the
    /// day view's mini calendar.
    /// </summary>
    public string PickerSelectedDayAriaLabel { get; set; } = "selected";

    /// <summary>Tooltip and accessible name of the header's button that opens the settings panel.</summary>
    public string SettingsButtonTitle { get; set; } = "Settings";

    /// <summary>Accessible name of the header's color filter.</summary>
    public string FilterByColorAriaLabel { get; set; } = "Filter events by color";

    /// <summary>Accessible name of the header's attendee filter, which offers the attendees of the events in view.</summary>
    public string FilterByPersonAriaLabel { get; set; } = "Filter events by person in current view";

    /// <summary>The color filter's option that shows the events of every color.</summary>
    public string AllColorsOption { get; set; } = "All colors";

    /// <summary>The attendee filter's option that shows the events of every attendee.</summary>
    public string AllPeopleOption { get; set; } = "All people";

    /// <summary>
    /// The attendee filter's name for an attendee with neither a name nor an <see cref="BitFullCalendarAttendee.Id"/>.
    /// The filter leaves such an attendee out, so this text is not shown at present.
    /// </summary>
    public string UnnamedAttendee { get; set; } = "(Unnamed)";

    /// <summary>Title of the settings panel.</summary>
    public string CalendarSettingsLabel { get; set; } = "Calendar settings";

    /// <summary>
    /// Label of the settings panel's switch between the <see cref="BitFullCalendarBadgeVariant.Dot"/> and
    /// <see cref="BitFullCalendarBadgeVariant.Colored"/> badges.
    /// </summary>
    public string DotBadgeLabel { get; set; } = "Dot badge";

    /// <summary>Label of the settings panel's switch between the 24-hour and the 12-hour time format.</summary>
    public string TwentyFourHourFormatLabel { get; set; } = "24-hour format";

    /// <summary>
    /// Label of the settings panel's field for the hour the day and week views scroll to
    /// (<see cref="BitFullCalendarSettings.StartOfDayHour"/>).
    /// </summary>
    public string DayStartsAtLabel { get; set; } = "Day starts at";

    /// <summary>The unit shown after the settings panel's "day starts at" hour.</summary>
    public string HourSuffix { get; set; } = "h";

    /// <summary>Label of the settings panel's choice of how the agenda view groups its events.</summary>
    public string AgendaGroupByLabel { get; set; } = "Agenda group by";

    /// <summary>The agenda grouping choice <see cref="BitFullCalendarAgendaGroupBy.Date"/>.</summary>
    public string AgendaGroupByDate { get; set; } = "Date";

    /// <summary>The agenda grouping choice <see cref="BitFullCalendarAgendaGroupBy.Color"/>.</summary>
    public string AgendaGroupByColor { get; set; } = "Color";

    /// <summary>
    /// Label of the settings panel's switch between the <see cref="BitFullCalendarEventLayout.Stack"/> and
    /// <see cref="BitFullCalendarEventLayout.Overlap"/> layouts of overlapping events.
    /// </summary>
    public string StackedEventsLabel { get; set; } = "Stack overlapping events";

    /// <summary>
    /// Label of the settings panel's switch for the day view's mini calendar
    /// (<see cref="BitFullCalendarSettings.ShowDayViewCalendar"/>).
    /// </summary>
    public string ShowDayViewCalendarLabel { get; set; } = "Show calendar in day view";

    /// <summary>
    /// Label of the settings panel's switch for the week numbers (<see cref="BitFullCalendarSettings.ShowWeekNumbers"/>).
    /// </summary>
    public string ShowWeekNumbersLabel { get; set; } = "Show week numbers";

    /// <summary>
    /// Label of the settings panel's switch for the current-time line
    /// (<see cref="BitFullCalendarSettings.ShowCurrentTimeIndicator"/>).
    /// </summary>
    public string ShowCurrentTimeIndicatorLabel { get; set; } = "Show current time";

    /// <summary>
    /// Label of the settings panel's choice of the time grids' slot length
    /// (<see cref="BitFullCalendarSettings.SlotDurationMinutes"/>).
    /// </summary>
    public string SlotDurationLabel { get; set; } = "Slot duration";

    /// <summary>The unit shown after each slot length the settings panel offers.</summary>
    public string MinuteSuffix { get; set; } = "min";

    /// <summary>
    /// The notice above the week view on a screen 768 pixels wide or narrower, while the week shows more than three
    /// days.
    /// </summary>
    public string WeekMobileWarning { get; set; } = "Weekly view is not recommended on smaller devices. Please switch to a desktop device or use the daily view instead.";

    /// <summary>Title of the day view sidebar's list of the events in progress.</summary>
    public string HappeningNowTitle { get; set; } = "Happening now";

    /// <summary>Shown under <see cref="HappeningNowTitle"/> while no event is in progress.</summary>
    public string NoAppointmentsNow { get; set; } = "No appointments at the moment";

    /// <summary>Placeholder and accessible name of the agenda view's search box.</summary>
    public string SearchEventsPlaceholder { get; set; } = "Search events...";

    /// <summary>Shown by the agenda view when it has no event to list, with or without a search.</summary>
    public string NoEventsFound { get; set; } = "No events found.";

    // Full format templates ({0} = the relevant value) so localized strings control word order and
    // placement rather than concatenating fixed English fragments at the call site.

    /// <summary>
    /// Title of the dialog listing a day's events, opened from a month cell's "+N more" or a day of the year view
    /// ({0} = the long date).
    /// </summary>
    public string EventListTitleFormat { get; set; } = "Events on {0}";

    /// <summary>
    /// A number of events ({0} = the count): under the title of the dialog listing a day's events, in the accessible
    /// name of a day of the year view, and announced as the agenda's search narrows its list.
    /// </summary>
    public string EventListCountFormat { get; set; } = "{0} event(s)";

    /// <summary>
    /// The button of a month cell that has more events than it shows, which opens the day's list ({0} = the number of
    /// events not shown).
    /// </summary>
    public string MoreEventsFormat { get; set; } = "+{0} more";

    /// <summary>Title of the add dialog.</summary>
    public string AddEventDialogTitle { get; set; } = "Add New Event";

    /// <summary>Title of the edit dialog.</summary>
    public string EditEventDialogTitle { get; set; } = "Edit Event";

    /// <summary>The line under the add dialog's title.</summary>
    public string AddEventDialogSubtitle { get; set; } = "Create a new event for your calendar.";

    /// <summary>The line under the edit dialog's title.</summary>
    public string EditEventDialogSubtitle { get; set; } = "Modify your existing event.";

    /// <summary>Accessible name of the close (X) button of the add/edit dialog and of the details dialog.</summary>
    public string CloseAriaLabel { get; set; } = "Close";

    /// <summary>Label of the button that closes the details dialog or the dialog listing a day's events.</summary>
    public string CloseButton { get; set; } = "Close";

    /// <summary>
    /// Label of the button that closes the add/edit dialog without saving, and of the one that cancels the prompt asking
    /// which occurrences of a recurring event a change applies to.
    /// </summary>
    public string CancelButton { get; set; } = "Cancel";

    /// <summary>Label of the details dialog's button that opens the edit dialog.</summary>
    public string EditButton { get; set; } = "Edit";

    /// <summary>Label of the details dialog's button that deletes the event.</summary>
    public string DeleteButton { get; set; } = "Delete";

    /// <summary>Label of the add dialog's button that saves the new event.</summary>
    public string CreateEventButton { get; set; } = "Create Event";

    /// <summary>Label of the edit dialog's button that saves the changes.</summary>
    public string SaveChangesButton { get; set; } = "Save Changes";

    /// <summary>Label of the add/edit dialog's title field.</summary>
    public string TitleLabel { get; set; } = "Title";

    /// <summary>Placeholder of the add/edit dialog's title field.</summary>
    public string EventTitlePlaceholder { get; set; } = "Event title";

    /// <summary>Caption of the add/edit dialog's start picker.</summary>
    public string StartDateTimeLabel { get; set; } = "Start Date & Time";

    /// <summary>Caption of the add/edit dialog's end picker.</summary>
    public string EndDateTimeLabel { get; set; } = "End Date & Time";

    /// <summary>Label of the add/edit dialog's color field, and of the color in the details dialog.</summary>
    public string ColorLabel { get; set; } = "Color";

    /// <summary>Accessible name of the add/edit dialog's color field.</summary>
    public string EventColorAriaLabel { get; set; } = "Event color";

    /// <summary>Label of the add/edit dialog's description field, and of the description in the details dialog.</summary>
    public string DescriptionLabel { get; set; } = "Description";

    /// <summary>Placeholder of the add/edit dialog's description field.</summary>
    public string EventDescriptionPlaceholder { get; set; } = "Event description";

    /// <summary>Label of the add/edit dialog's attendees, and of the attendees in the details dialog.</summary>
    public string AttendeesLabel { get; set; } = "Attendees";

    /// <summary>Shown by the details dialog for an event with no attendees.</summary>
    public string NoAttendeesText { get; set; } = "No attendees";

    /// <summary>Placeholder and accessible name of the first-name field of a new attendee in the add/edit dialog.</summary>
    public string FirstNamePlaceholder { get; set; } = "First name";

    /// <summary>Placeholder and accessible name of the last-name field of a new attendee in the add/edit dialog.</summary>
    public string LastNamePlaceholder { get; set; } = "Last name";

    /// <summary>
    /// Placeholder and accessible name of the field for a new attendee's <see cref="BitFullCalendarAttendee.Id"/> in the
    /// add/edit dialog.
    /// </summary>
    public string IdOptionalPlaceholder { get; set; } = "ID (optional)";

    /// <summary>Label of the add/edit dialog's button that adds the attendee typed in.</summary>
    public string AddButton { get; set; } = "Add";

    /// <summary>
    /// Accessible name of the button that removes an attendee in the add/edit dialog, followed by a colon and the
    /// attendee's name.
    /// </summary>
    public string RemoveAttendeeAriaLabel { get; set; } = "Remove attendee";

    /// <summary>Label of the start in the details dialog.</summary>
    public string StartDateLabel { get; set; } = "Start Date";

    /// <summary>Label of the end in the details dialog.</summary>
    public string EndDateLabel { get; set; } = "End Date";

    /// <summary>The word between the date and the time of the start and the end in the details dialog.</summary>
    public string AtWord { get; set; } = "at";

    /// <summary>
    /// The words for an all-day event: the label of the add/edit dialog's all-day switch, and what stands in for the
    /// time of an all-day event on its badge or block, in its tooltip and accessible name, in the agenda and in the
    /// dialogs.
    /// </summary>
    public string AllDayLabel { get; set; } = "All day";

    /// <summary>
    /// Format template of the week-number cell ({0} = the ISO-8601 week number).
    /// </summary>
    public string WeekNumberFormat { get; set; } = "W{0}";

    /// <summary>Accessible name of a week-number cell ({0} = the ISO-8601 week number).</summary>
    public string WeekNumberAriaLabelFormat { get; set; } = "Week {0}";

    /// <summary>
    /// Why a move, resize, or save was refused because it would overlap another event: the calendar's notice after a
    /// move or resize, and shown in the add/edit dialog after a save.
    /// </summary>
    public string EventOverlapMessage { get; set; } = "This time range is already taken on that resource.";

    /// <summary>
    /// Why a move, resize, or save was refused because it falls outside the allowed date range: the calendar's notice
    /// after a move or resize, and shown in the add/edit dialog after a save.
    /// </summary>
    public string OutOfRangeMessage { get; set; } = "That date is outside the allowed range.";

    /// <summary>
    /// Why a move, resize, or save was refused because it falls outside the business hours while
    /// <see cref="BitFullCalendarSettings.RestrictToBusinessHours"/> is on: the calendar's notice after a move or
    /// resize, and shown in the add/edit dialog after a save.
    /// </summary>
    public string OutsideBusinessHoursMessage { get; set; } = "That time is outside business hours.";

    /// <summary>
    /// Why a move, resize, or save was refused because it would overlap an event marked
    /// <see cref="BitFullCalendarEvent.IsBlocking"/>: the calendar's notice after a move or resize, and shown in the
    /// add/edit dialog after a save.
    /// </summary>
    public string BlockedMessage { get; set; } = "That time is unavailable.";

    /// <summary>Label of the business-hours toggle in the settings panel.</summary>
    public string HighlightBusinessHoursLabel { get; set; } = "Highlight business hours";

    /// <summary>
    /// Accessible name of a day number that navigates to that day ({0} = the formatted date). A column header of the
    /// week view is named by the text it shows ahead of it ("Mon 5, Go to Monday, January 5, 2026").
    /// </summary>
    public string NavLinkDayAriaLabelFormat { get; set; } = "Go to {0}";

    /// <summary>
    /// Accessible name of a week number that navigates to that week ({0} = the week number), after the text the
    /// link shows (<see cref="WeekNumberFormat"/>): "W27, Go to week 27".
    /// </summary>
    public string NavLinkWeekAriaLabelFormat { get; set; } = "Go to week {0}";

    /// <summary>The add/edit dialog's message when the title is empty.</summary>
    public string ValidationTitleRequired { get; set; } = "Title is required";

    /// <summary>
    /// The add/edit dialog's message when the description is empty while
    /// <see cref="BitFullCalendarSettings.RequireEventDescription"/> is on.
    /// </summary>
    public string ValidationDescriptionRequired { get; set; } = "Description is required";

    /// <summary>The add/edit dialog's message when the end is not after the start.</summary>
    public string ValidationEndAfterStart { get; set; } = "End date must be after start date";

    /// <summary>The add/edit dialog's message when an attendee is added with neither a first nor a last name.</summary>
    public string ValidationAttendeeNameRequired { get; set; } = "First name or last name is required";

    /// <summary>
    /// Accessible name of the time range shown on an event while its edge is dragged, in the day and week views and
    /// the timeline.
    /// </summary>
    public string ResizePreviewAriaLabel { get; set; } = "New time range";

    /// <summary>Label of the repeat-rule row in the event details dialog.</summary>
    public string RepeatsLabel { get; set; } = "Repeats";

    /// <summary>Name of a daily repeat rule.</summary>
    public string RepeatsDaily { get; set; } = "Daily";

    /// <summary>Name of a weekly repeat rule.</summary>
    public string RepeatsWeekly { get; set; } = "Weekly";

    /// <summary>Name of a monthly repeat rule.</summary>
    public string RepeatsMonthly { get; set; } = "Monthly";

    /// <summary>Name of a yearly repeat rule.</summary>
    public string RepeatsYearly { get; set; } = "Yearly";

    /// <summary>Appended to the frequency when the rule repeats every N units ({0} = the interval).</summary>
    public string RepeatsIntervalFormat { get; set; } = "every {0}";

    /// <summary>Appended when the series is closed by a number of occurrences ({0} = the count).</summary>
    public string RepeatsCountFormat { get; set; } = "{0} times";

    /// <summary>Appended when the series is closed by a date ({0} = the formatted date).</summary>
    public string RepeatsUntilFormat { get; set; } = "until {0}";

    /// <summary>
    /// Appended when a weekly series names its weekdays ({0} = the abbreviated weekday names, joined
    /// with <see cref="ListSeparator"/>).
    /// </summary>
    public string RepeatsOnDaysFormat { get; set; } = "on {0}";

    /// <summary>
    /// Appended when a monthly or yearly series lands on a weekday of the month ({0} = the week of the
    /// month, from <see cref="GetWeekOfMonthLabel"/>; {1} = the weekday names).
    /// </summary>
    public string RepeatsOnWeekOfMonthFormat { get; set; } = "on the {0} {1}";

    /// <summary>Separator between the items of a list in a summary, such as weekday names.</summary>
    public string ListSeparator { get; set; } = ", ";

    /// <summary>Name of <see cref="BitFullCalendarWeekOfMonth.First"/>.</summary>
    public string WeekOfMonthFirst { get; set; } = "first";

    /// <summary>Name of <see cref="BitFullCalendarWeekOfMonth.Second"/>.</summary>
    public string WeekOfMonthSecond { get; set; } = "second";

    /// <summary>Name of <see cref="BitFullCalendarWeekOfMonth.Third"/>.</summary>
    public string WeekOfMonthThird { get; set; } = "third";

    /// <summary>Name of <see cref="BitFullCalendarWeekOfMonth.Fourth"/>.</summary>
    public string WeekOfMonthFourth { get; set; } = "fourth";

    /// <summary>Name of <see cref="BitFullCalendarWeekOfMonth.Last"/>.</summary>
    public string WeekOfMonthLast { get; set; } = "last";

    /// <summary>Label of the repeat picker in the add/edit dialog.</summary>
    public string RepeatLabel { get; set; } = "Repeat";

    /// <summary>The repeat picker's option for a one-off event.</summary>
    public string DoesNotRepeatOption { get; set; } = "Does not repeat";

    /// <summary>Label of the interval field in the add/edit dialog.</summary>
    public string RepeatEveryLabel { get; set; } = "Repeat every";

    /// <summary>Unit shown after the interval of a daily series.</summary>
    public string RepeatDaysUnit { get; set; } = "day(s)";

    /// <summary>Unit shown after the interval of a weekly series.</summary>
    public string RepeatWeeksUnit { get; set; } = "week(s)";

    /// <summary>Unit shown after the interval of a monthly series.</summary>
    public string RepeatMonthsUnit { get; set; } = "month(s)";

    /// <summary>Unit shown after the interval of a yearly series.</summary>
    public string RepeatYearsUnit { get; set; } = "year(s)";

    /// <summary>Label of the weekday and day-of-the-month choices in the add/edit dialog.</summary>
    public string RepeatOnLabel { get; set; } = "Repeat on";

    /// <summary>The option that keeps a monthly series on the start date's day number ({0} = that day).</summary>
    public string RepeatOnDayOfMonthFormat { get; set; } = "Day {0}";

    /// <summary>
    /// The option that moves a monthly or yearly series onto a weekday of the month ({0} = the week of
    /// the month, from <see cref="GetWeekOfMonthLabel"/>). The weekdays are picked beside it.
    /// </summary>
    public string RepeatOnWeekOfMonthFormat { get; set; } = "The {0}";

    /// <summary>Label of the field that decides when a series ends.</summary>
    public string EndsLabel { get; set; } = "Ends";

    /// <summary>The option for an open-ended series.</summary>
    public string EndsNeverOption { get; set; } = "Never";

    /// <summary>The option for a series that ends on a date.</summary>
    public string EndsOnDateOption { get; set; } = "On date";

    /// <summary>The option for a series that ends after a number of occurrences.</summary>
    public string EndsAfterOption { get; set; } = "After";

    /// <summary>Shown after the occurrence count of a series that ends after a number of occurrences.</summary>
    public string OccurrencesLabel { get; set; } = "occurrence(s)";

    /// <summary>Label of the skipped and added dates of a series in the add/edit dialog.</summary>
    public string RepeatExceptionsLabel { get; set; } = "Exceptions";

    /// <summary>Button that adds the picked date to the dates a series skips.</summary>
    public string SkipDateButton { get; set; } = "Skip date";

    /// <summary>Button that adds the picked date to the dates a series also occurs on.</summary>
    public string AddDateButton { get; set; } = "Add date";

    /// <summary>Caption of the dates a series skips.</summary>
    public string SkippedDatesLabel { get; set; } = "Skipped";

    /// <summary>Caption of the dates a series also occurs on.</summary>
    public string AddedDatesLabel { get; set; } = "Added";

    /// <summary>Accessible name of the button that removes a date from the skipped or added dates.</summary>
    public string RemoveDateAriaLabel { get; set; } = "Remove date";

    /// <summary>Validation message when an interval or an occurrence count is below 1.</summary>
    public string ValidationRepeatAtLeastOne { get; set; } = "Must be at least 1";

    /// <summary>Validation message when a series is set to end before it starts.</summary>
    public string ValidationUntilBeforeStart { get; set; } = "The series cannot end before it starts";

    /// <summary>Title of the prompt that asks whether an edit applies to one occurrence or the series.</summary>
    public string RecurringEditTitle { get; set; } = "Edit recurring event";

    /// <summary>Title of the prompt that asks whether a delete applies to one occurrence or the series.</summary>
    public string RecurringDeleteTitle { get; set; } = "Delete recurring event";

    /// <summary>The choice that applies an edit or delete to the opened occurrence only.</summary>
    public string ThisOccurrenceOption { get; set; } = "This event";

    /// <summary>
    /// The choice that applies an edit or delete to the opened occurrence and every later one: the series ends before
    /// it, and an edit starts a new series from it.
    /// </summary>
    public string ThisAndFollowingOption { get; set; } = "This and following events";

    /// <summary>The choice that applies an edit or delete to the whole series.</summary>
    public string AllOccurrencesOption { get; set; } = "All events in the series";

    /// <summary>Label of the button that confirms a prompt.</summary>
    public string OkButton { get; set; } = "OK";

    /// <summary>The localized name of a week of the month, as used in repeat summaries and the dialog.</summary>
    /// <param name="week">The week of the month to name.</param>
    /// <returns>The text of <paramref name="week"/> (<see cref="WeekOfMonthFirst"/> and the others), or the value's own
    /// name for a value the enum does not define.</returns>
    public string GetWeekOfMonthLabel(BitFullCalendarWeekOfMonth week) => week switch
    {
        BitFullCalendarWeekOfMonth.First => WeekOfMonthFirst,
        BitFullCalendarWeekOfMonth.Second => WeekOfMonthSecond,
        BitFullCalendarWeekOfMonth.Third => WeekOfMonthThird,
        BitFullCalendarWeekOfMonth.Fourth => WeekOfMonthFourth,
        BitFullCalendarWeekOfMonth.Last => WeekOfMonthLast,
        _ => week.ToString()
    };

    /// <summary>
    /// A one-line summary of a repeat rule, as shown in the event details dialog: the frequency, then
    /// the interval, the weekdays or the weekday of the month, the occurrence count, and the end date
    /// when the rule names them.
    /// <para>
    /// <paramref name="seriesStart"/> is the master's start: a monthly rule on a weekday of the month
    /// that lists no weekday follows the start date's own, which only the start can name.
    /// </para>
    /// </summary>
    /// <param name="recurrence">The repeat rule to describe.</param>
    /// <param name="culture">The culture the numbers, the weekday names and the end date are written in;
    /// <see cref="System.Globalization.CultureInfo.CurrentUICulture"/> when <c>null</c>.</param>
    /// <param name="seriesStart">The start of the series' first event, if it is known.</param>
    /// <returns>The parts of the summary, joined by " · ".</returns>
    /// <exception cref="ArgumentNullException"><paramref name="recurrence"/> is <c>null</c>.</exception>
    public string GetRecurrenceSummary(BitFullCalendarRecurrence recurrence, System.Globalization.CultureInfo? culture = null, DateTime? seriesStart = null)
    {
        ArgumentNullException.ThrowIfNull(recurrence);
        culture ??= System.Globalization.CultureInfo.CurrentUICulture;

        var parts = new List<string>(5)
        {
            recurrence.Frequency switch
            {
                BitFullCalendarRecurrenceFrequency.Daily => RepeatsDaily,
                BitFullCalendarRecurrenceFrequency.Weekly => RepeatsWeekly,
                BitFullCalendarRecurrenceFrequency.Monthly => RepeatsMonthly,
                BitFullCalendarRecurrenceFrequency.Yearly => RepeatsYearly,
                _ => recurrence.Frequency.ToString()
            }
        };

        if (recurrence.Interval > 1)
            parts.Add(string.Format(culture, RepeatsIntervalFormat, recurrence.Interval));

        var namesDays = recurrence.DaysOfWeek?.Any(Enum.IsDefined) is true;
        // Weekdays read in the order the culture's week runs, not in DayOfWeek's Sunday-first order.
        var firstDay = (int)culture.DateTimeFormat.FirstDayOfWeek;
        IEnumerable<DayOfWeek> OrderedDays(DateTime start) => recurrence.ResolveWeekDays(start).OrderBy(d => ((int)d - firstDay + 7) % 7);

        if (recurrence.ResolveWeekOfMonth() is { } week)
        {
            var days = namesDays || seriesStart is not null
                ? string.Join(ListSeparator, OrderedDays(seriesStart ?? default).Select(culture.DateTimeFormat.GetDayName))
                : string.Empty;
            parts.Add(string.Format(culture, RepeatsOnWeekOfMonthFormat, GetWeekOfMonthLabel(week), days).Trim());
        }
        else if (recurrence.Frequency is BitFullCalendarRecurrenceFrequency.Weekly && namesDays)
        {
            var days = string.Join(ListSeparator, OrderedDays(default).Select(culture.DateTimeFormat.GetAbbreviatedDayName));
            parts.Add(string.Format(culture, RepeatsOnDaysFormat, days));
        }

        if (recurrence.Count is { } count && count > 0)
            parts.Add(string.Format(culture, RepeatsCountFormat, count));

        if (recurrence.Until is { } until)
            parts.Add(string.Format(culture, RepeatsUntilFormat, until.ToString("d", culture)));

        return string.Join(" · ", parts);
    }

    /// <summary>
    /// Label of the add/edit dialog's resource field, shown when the calendar has resources, and of the resource in the
    /// details dialog.
    /// </summary>
    public string ResourceLabel { get; set; } = "Resource";

    /// <summary>Header of the timeline's resource column.</summary>
    public string ResourceColumnHeader { get; set; } = "Resource";

    /// <summary>
    /// Name of the timeline row of the events assigned to no resource, and what the details dialog shows as such an
    /// event's resource.
    /// </summary>
    public string NoResourceLabel { get; set; } = "Unassigned";

    /// <summary>The add/edit dialog's resource option that assigns the event to no resource.</summary>
    public string NoResourceOption { get; set; } = "(none)";

    /// <summary>Shown by the timeline when it has no resource row to show.</summary>
    public string NoResourcesMessage { get; set; } = "No resources to display.";

    /// <summary>The name of a view on the view tabs.</summary>
    /// <param name="view">The view to name.</param>
    /// <returns>The text of <paramref name="view"/> (<see cref="ViewDay"/> and the others), or the value's own name for
    /// a value the enum does not define.</returns>
    public string GetViewLabel(BitFullCalendarView view) => view switch
    {
        BitFullCalendarView.Day => ViewDay,
        BitFullCalendarView.Week => ViewWeek,
        BitFullCalendarView.Month => ViewMonth,
        BitFullCalendarView.Year => ViewYear,
        BitFullCalendarView.Agenda => ViewAgenda,
        _ => view.ToString()
    };

    /// <summary>The name of a mode on the mode tabs.</summary>
    /// <param name="mode">The mode to name.</param>
    /// <returns>The text of <paramref name="mode"/> (<see cref="ModeEvent"/> or <see cref="ModeTimeline"/>), or the
    /// value's own name for a value the enum does not define.</returns>
    public string GetModeLabel(BitFullCalendarMode mode) => mode switch
    {
        BitFullCalendarMode.Event => ModeEvent,
        BitFullCalendarMode.Timeline => ModeTimeline,
        _ => mode.ToString()
    };
}

