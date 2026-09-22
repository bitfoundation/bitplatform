using System.Globalization;

namespace Bit.BlazorUI;

/// <summary>
/// The parameters for <see cref="BitCalendar"/> component.
/// </summary>
public class BitCalendarParams : BitComponentBaseParams, IBitComponentParams
{
    /// <summary>
    /// Represents the parameter name used to identify the <see cref="BitCalendar"/> cascading parameters within <see cref="BitParams"/>.
    /// </summary>
    /// <remarks>
    /// This constant is typically used when referencing or accessing the BitCalendar value in
    /// parameterized APIs or configuration settings. Using this constant helps ensure consistency and reduces the risk
    /// of typographical errors.
    /// </remarks>
    public const string ParamName = $"{nameof(BitParams)}.{nameof(BitCalendar)}";



    public string Name => ParamName;



    /// <summary>
    /// Whether selecting the already selected day deselects it, clearing the value.
    /// </summary>
    public bool? AllowDeselect { get; set; }

    /// <summary>
    /// Custom CSS classes for different parts of the BitCalendar component.
    /// </summary>
    public BitCalendarClassStyles? Classes { get; set; }

    /// <summary>
    /// The general color of the calendar that applies to the today day button, the highlighted current month,
    /// the selected AM/PM button, and the event indicators.
    /// </summary>
    public BitColor? Color { get; set; }

    /// <summary>
    /// The delay in milliseconds before the hour/minute of the time picker starts changing continuously while an
    /// increase/decrease button is held down.
    /// </summary>
    public int? ContinuousSpinDelay { get; set; }

    /// <summary>
    /// The interval in milliseconds between two consecutive changes while an increase/decrease button is held down.
    /// </summary>
    public int? ContinuousSpinInterval { get; set; }

    /// <summary>
    /// CultureInfo for the Calendar.
    /// </summary>
    public CultureInfo? Culture { get; set; }

    /// <summary>
    /// The format of the date in the Calendar.
    /// </summary>
    public string? DateFormat { get; set; }

    /// <summary>
    /// Used to customize how content inside the day cell is rendered.
    /// </summary>
    public RenderFragment<DateTimeOffset>? DayCellTemplate { get; set; }

    /// <summary>
    /// Disables every day after today, exactly as a <see cref="MaxDate"/> of now would.
    /// When both are set, the earlier of the two bounds wins.
    /// </summary>
    public bool? DisableFuture { get; set; }

    /// <summary>
    /// Disables every day before today, exactly as a <see cref="MinDate"/> of now would.
    /// When both are set, the later of the two bounds wins.
    /// </summary>
    public bool? DisablePast { get; set; }

    /// <summary>
    /// The list of dates that are disabled (not selectable) in the calendar, in addition to MinDate and MaxDate.
    /// </summary>
    public IEnumerable<DateTimeOffset>? DisabledDates { get; set; }

    /// <summary>
    /// The days of the week that are disabled (not selectable) in the calendar (e.g. weekends).
    /// </summary>
    public IEnumerable<DayOfWeek>? DisabledDaysOfWeek { get; set; }

    /// <summary>
    /// The list of events to display on calendar days.
    /// </summary>
    public IEnumerable<BitCalendarEvent>? Events { get; set; }

    /// <summary>
    /// The title (tooltip) and the accessible name of the close button of the event details dialog.
    /// </summary>
    public string? EventDetailsCloseButtonTitle { get; set; }

    /// <summary>
    /// Used to customize how an event is rendered in the details dialog, in place of its title, its time and its body.
    /// </summary>
    public RenderFragment<BitCalendarEvent>? EventTemplate { get; set; }

    /// <summary>
    /// The text shown before the start time of an event when only a start time is present (e.g. "From 09:00").
    /// </summary>
    public string? EventTimeFromText { get; set; }

    /// <summary>
    /// The text shown before the end time of an event when only an end time is present (e.g. "Until 17:00").
    /// </summary>
    public string? EventTimeUntilText { get; set; }

    /// <summary>
    /// Rendered under the pickers, inside the root of the calendar.
    /// </summary>
    public RenderFragment? FooterTemplate { get; set; }

    /// <summary>
    /// Overrides the first day of the week in the day picker. If not set, the first day of the week of the Culture is used.
    /// </summary>
    public DayOfWeek? FirstDayOfWeek { get; set; }

    /// <summary>
    /// Whether the day picker should always render six weeks, filling the extra rows with the days of the adjacent months.
    /// </summary>
    public bool? FixedWeeks { get; set; }

    /// <summary>
    /// Custom function to provide additional CSS classes for each day button of the calendar.
    /// </summary>
    public Func<DateTimeOffset, string?>? GetDayClass { get; set; }

    /// <summary>
    /// Rendered above the pickers, inside the root of the calendar.
    /// </summary>
    public RenderFragment? HeaderTemplate { get; set; }

    /// <summary>
    /// The title of the Go to next month button (tooltip).
    /// </summary>
    public string? GoToNextMonthTitle { get; set; }

    /// <summary>
    /// The title of the Go to next year range button (tooltip).
    /// </summary>
    public string? GoToNextYearRangeTitle { get; set; }

    /// <summary>
    /// The title of the Go to next year button (tooltip).
    /// </summary>
    public string? GoToNextYearTitle { get; set; }

    /// <summary>
    /// Gets or sets the icon to display in the now button using custom CSS classes for external icon libraries.
    /// Takes precedence over <see cref="NowButtonIconName"/> when both are set.
    /// </summary>
    public BitIconInfo? NowButtonIcon { get; set; }

    /// <summary>
    /// Gets or sets the name of the icon to display in the now button from the built-in Fluent UI icons.
    /// </summary>
    public string? NowButtonIconName { get; set; }

    /// <summary>
    /// The title of the now button (tooltip).
    /// </summary>
    public string? NowButtonTitle { get; set; }

    /// <summary>
    /// The title of the Go to previous month button (tooltip).
    /// </summary>
    public string? GoToPrevMonthTitle { get; set; }

    /// <summary>
    /// The title of the Go to previous year range button (tooltip).
    /// </summary>
    public string? GoToPrevYearRangeTitle { get; set; }

    /// <summary>
    /// The title of the Go to previous year button (tooltip).
    /// </summary>
    public string? GoToPrevYearTitle { get; set; }

    /// <summary>
    /// Gets or sets the icon to display in the GoToToday button using custom CSS classes for external icon libraries.
    /// Takes precedence over <see cref="GoToTodayIconName"/> when both are set.
    /// </summary>
    public BitIconInfo? GoToTodayIcon { get; set; }

    /// <summary>
    /// Gets or sets the name of the icon to display in the GoToToday button from the built-in Fluent UI icons.
    /// </summary>
    public string? GoToTodayIconName { get; set; }

    /// <summary>
    /// The title of the GoToToday button (tooltip).
    /// </summary>
    public string? GoToTodayTitle { get; set; }

    /// <summary>
    /// Gets or sets the icon to display in the HideTimePicker button using custom CSS classes for external icon libraries.
    /// Takes precedence over <see cref="HideTimePickerIconName"/> when both are set.
    /// </summary>
    public BitIconInfo? HideTimePickerIcon { get; set; }

    /// <summary>
    /// Gets or sets the name of the icon to display in the HideTimePicker button from the built-in Fluent UI icons.
    /// </summary>
    public string? HideTimePickerIconName { get; set; }

    /// <summary>
    /// The title of the HideTimePicker button (tooltip).
    /// </summary>
    public string? HideTimePickerTitle { get; set; }

    /// <summary>
    /// Whether the month picker should highlight the current month.
    /// </summary>
    public bool? HighlightCurrentMonth { get; set; }

    /// <summary>
    /// The list of dates that are highlighted (marked) in the day picker.
    /// </summary>
    public IEnumerable<DateTimeOffset>? HighlightedDates { get; set; }

    /// <summary>
    /// Whether the month picker should highlight the selected month.
    /// </summary>
    public bool? HighlightSelectedMonth { get; set; }

    /// <summary>
    /// Whether the day picker should highlight today's day.
    /// </summary>
    public bool? HighlightToday { get; set; }

    /// <summary>
    /// The step, in hours, the spin buttons of the time picker move the hour by.
    /// </summary>
    public int? HourStep { get; set; }

    /// <summary>
    /// The custom validation error message for the invalid value.
    /// </summary>
    public string? InvalidErrorMessage { get; set; }

    /// <summary>
    /// Custom function to determine if a specific date is disabled (not selectable) in the calendar.
    /// </summary>
    public Func<DateTimeOffset, bool>? IsDateDisabled { get; set; }

    /// <summary>
    /// The maximum allowable date of the calendar.
    /// </summary>
    public DateTimeOffset? MaxDate { get; set; }

    /// <summary>
    /// The minimum allowable date of the calendar.
    /// </summary>
    public DateTimeOffset? MinDate { get; set; }

    /// <summary>
    /// The step, in minutes, the spin buttons of the time picker move the minute by.
    /// </summary>
    public int? MinuteStep { get; set; }

    /// <summary>
    /// The number of consecutive months rendered side by side in the day picker (1 to 3).
    /// </summary>
    public int? MonthCount { get; set; }

    /// <summary>
    /// Used to customize how content inside the month cell is rendered.
    /// </summary>
    public RenderFragment<DateTimeOffset>? MonthCellTemplate { get; set; }

    /// <summary>
    /// The title of the month picker's toggle (tooltip).
    /// </summary>
    public string? MonthPickerToggleTitle { get; set; }

    /// <summary>
    /// Gets or sets the icon to display in the Go to next month button using custom CSS classes for external icon libraries.
    /// Takes precedence over <see cref="NextMonthNavIconName"/> when both are set.
    /// </summary>
    public BitIconInfo? NextMonthNavIcon { get; set; }

    /// <summary>
    /// Gets or sets the name of the icon to display in the Go to next month button from the built-in Fluent UI icons.
    /// </summary>
    public string? NextMonthNavIconName { get; set; }

    /// <summary>
    /// Gets or sets the icon to display in the Go to next year button using custom CSS classes for external icon libraries.
    /// Takes precedence over <see cref="NextYearNavIconName"/> when both are set.
    /// </summary>
    public BitIconInfo? NextYearNavIcon { get; set; }

    /// <summary>
    /// Gets or sets the name of the icon to display in the Go to next year button from the built-in Fluent UI icons.
    /// </summary>
    public string? NextYearNavIconName { get; set; }

    /// <summary>
    /// Gets or sets the icon to display in the Go to next year range button using custom CSS classes for external icon libraries.
    /// Takes precedence over <see cref="NextYearRangeNavIconName"/> when both are set.
    /// </summary>
    public BitIconInfo? NextYearRangeNavIcon { get; set; }

    /// <summary>
    /// Gets or sets the name of the icon to display in the Go to next year range button from the built-in Fluent UI icons.
    /// </summary>
    public string? NextYearRangeNavIconName { get; set; }

    /// <summary>
    /// Whether the previous and next navigation buttons move the calendar by all of its rendered months instead of one.
    /// </summary>
    public bool? PagedNavigation { get; set; }

    /// <summary>
    /// Gets or sets the icon to display in the Go to previous month button using custom CSS classes for external icon libraries.
    /// Takes precedence over <see cref="PrevMonthNavIconName"/> when both are set.
    /// </summary>
    public BitIconInfo? PrevMonthNavIcon { get; set; }

    /// <summary>
    /// Gets or sets the name of the icon to display in the Go to previous month button from the built-in Fluent UI icons.
    /// </summary>
    public string? PrevMonthNavIconName { get; set; }

    /// <summary>
    /// Gets or sets the icon to display in the Go to previous year button using custom CSS classes for external icon libraries.
    /// Takes precedence over <see cref="PrevYearNavIconName"/> when both are set.
    /// </summary>
    public BitIconInfo? PrevYearNavIcon { get; set; }

    /// <summary>
    /// Gets or sets the name of the icon to display in the Go to previous year button from the built-in Fluent UI icons.
    /// </summary>
    public string? PrevYearNavIconName { get; set; }

    /// <summary>
    /// Gets or sets the icon to display in the Go to previous year range button using custom CSS classes for external icon libraries.
    /// Takes precedence over <see cref="PrevYearRangeNavIconName"/> when both are set.
    /// </summary>
    public BitIconInfo? PrevYearRangeNavIcon { get; set; }

    /// <summary>
    /// Gets or sets the name of the icon to display in the Go to previous year range button from the built-in Fluent UI icons.
    /// </summary>
    public string? PrevYearRangeNavIconName { get; set; }

    /// <summary>
    /// The template of the text a screen reader is given when the selection changes, where {0} is the selected
    /// date written with the <see cref="DateFormat"/>.
    /// </summary>
    public string? SelectedDateAriaAtomic { get; set; }

    /// <summary>
    /// Whether the now button should be shown or not.
    /// </summary>
    public bool? ShowNowButton { get; set; }

    /// <summary>
    /// Whether the GoToToday button should be shown or not.
    /// </summary>
    public bool? ShowGoToToday { get; set; }

    /// <summary>
    /// Whether clicking a day that carries events opens the dialog listing them.
    /// </summary>
    public bool? ShowEventDetails { get; set; }

    /// <summary>
    /// Whether the month picker is shown or hidden.
    /// </summary>
    public bool? ShowMonthPicker { get; set; }

    /// <summary>
    /// Show month picker on top of date picker when visible.
    /// </summary>
    public bool? ShowMonthPickerAsOverlay { get; set; }

    /// <summary>
    /// Whether the days of the previous and next months should be shown in the day picker.
    /// </summary>
    public bool? ShowOutsideDays { get; set; }

    /// <summary>
    /// Whether the time picker should be shown or not.
    /// </summary>
    public bool? ShowTimePicker { get; set; }

    /// <summary>
    /// Show time picker on top of date picker when visible.
    /// </summary>
    public bool? ShowTimePickerAsOverlay { get; set; }

    /// <summary>
    /// Gets or sets the icon to display in the ShowTimePicker button using custom CSS classes for external icon libraries.
    /// Takes precedence over <see cref="ShowTimePickerIconName"/> when both are set.
    /// </summary>
    public BitIconInfo? ShowTimePickerIcon { get; set; }

    /// <summary>
    /// Gets or sets the name of the icon to display in the ShowTimePicker button from the built-in Fluent UI icons.
    /// </summary>
    public string? ShowTimePickerIconName { get; set; }

    /// <summary>
    /// The title of the ShowTimePicker button (tooltip).
    /// </summary>
    public string? ShowTimePickerTitle { get; set; }

    /// <summary>
    /// Whether the week number (weeks 1 to 53) should be shown before each week row.
    /// </summary>
    public bool? ShowWeekNumbers { get; set; }

    /// <summary>
    /// The size of the calendar, which scales its cells and their text.
    /// </summary>
    public BitSize? Size { get; set; }

    /// <summary>
    /// Specifies the date and time of the calendar when it is showing without any selected value.
    /// </summary>
    public DateTimeOffset? StartingValue { get; set; }

    /// <summary>
    /// Custom CSS styles for different parts of the BitCalendar component.
    /// </summary>
    public BitCalendarClassStyles? Styles { get; set; }

    /// <summary>
    /// The time format of the time-picker, 24H or 12H.
    /// </summary>
    public BitTimeFormat? TimeFormat { get; set; }

    /// <summary>
    /// Gets or sets the icon to display in the decrease-hour button using custom CSS classes for external icon libraries.
    /// Takes precedence over <see cref="TimePickerDecreaseHourIconName"/> when both are set.
    /// </summary>
    public BitIconInfo? TimePickerDecreaseHourIcon { get; set; }

    /// <summary>
    /// Gets or sets the name of the icon to display in the decrease-hour button from the built-in Fluent UI icons.
    /// </summary>
    public string? TimePickerDecreaseHourIconName { get; set; }

    /// <summary>
    /// The title (tooltip) and the accessible name of the time-picker's decrease-hour button.
    /// </summary>
    public string? TimePickerDecreaseHourTitle { get; set; }

    /// <summary>
    /// The title (tooltip) and the accessible name of the time-picker's decrease-minute button.
    /// </summary>
    public string? TimePickerDecreaseMinuteTitle { get; set; }

    /// <summary>
    /// Gets or sets the icon to display in the decrease-minute button using custom CSS classes for external icon libraries.
    /// Takes precedence over <see cref="TimePickerDecreaseMinuteIconName"/> when both are set.
    /// </summary>
    public BitIconInfo? TimePickerDecreaseMinuteIcon { get; set; }

    /// <summary>
    /// Gets or sets the name of the icon to display in the decrease-minute button from the built-in Fluent UI icons.
    /// </summary>
    public string? TimePickerDecreaseMinuteIconName { get; set; }

    /// <summary>
    /// The title (tooltip) and the accessible name of the time-picker's hour input.
    /// </summary>
    public string? TimePickerHourTitle { get; set; }

    /// <summary>
    /// The title (tooltip) and the accessible name of the time-picker's minute input.
    /// </summary>
    public string? TimePickerMinuteTitle { get; set; }

    /// <summary>
    /// Gets or sets the icon to display in the increase-hour button using custom CSS classes for external icon libraries.
    /// Takes precedence over <see cref="TimePickerIncreaseHourIconName"/> when both are set.
    /// </summary>
    public BitIconInfo? TimePickerIncreaseHourIcon { get; set; }

    /// <summary>
    /// Gets or sets the name of the icon to display in the increase-hour button from the built-in Fluent UI icons.
    /// </summary>
    public string? TimePickerIncreaseHourIconName { get; set; }

    /// <summary>
    /// Gets or sets the icon to display in the increase-minute button using custom CSS classes for external icon libraries.
    /// Takes precedence over <see cref="TimePickerIncreaseMinuteIconName"/> when both are set.
    /// </summary>
    public BitIconInfo? TimePickerIncreaseMinuteIcon { get; set; }

    /// <summary>
    /// Gets or sets the name of the icon to display in the increase-minute button from the built-in Fluent UI icons.
    /// </summary>
    public string? TimePickerIncreaseMinuteIconName { get; set; }

    /// <summary>
    /// The title (tooltip) and the accessible name of the time-picker's increase-hour button.
    /// </summary>
    public string? TimePickerIncreaseHourTitle { get; set; }

    /// <summary>
    /// The title (tooltip) and the accessible name of the time-picker's increase-minute button.
    /// </summary>
    public string? TimePickerIncreaseMinuteTitle { get; set; }

    /// <summary>
    /// TimeZone for the Calendar.
    /// </summary>
    public TimeZoneInfo? TimeZone { get; set; }

    /// <summary>
    /// Overrides the current date and time considered as "today" and "now" in the calendar.
    /// </summary>
    public DateTimeOffset? Today { get; set; }

    /// <summary>
    /// The rule used to calculate the week numbers. Defaults to the FirstFullWeek rule.
    /// </summary>
    public CalendarWeekRule? WeekNumberRule { get; set; }

    /// <summary>
    /// The accessible name of the empty column header above the week numbers.
    /// </summary>
    public string? WeekNumbersHeaderTitle { get; set; }

    /// <summary>
    /// The title of the week number (tooltip).
    /// </summary>
    public string? WeekNumberTitle { get; set; }

    /// <summary>
    /// Used to customize how content inside the year cell is rendered.
    /// </summary>
    public RenderFragment<int>? YearCellTemplate { get; set; }

    /// <summary>
    /// The title of the year picker's toggle (tooltip).
    /// </summary>
    public string? YearPickerToggleTitle { get; set; }

    /// <summary>
    /// The title of the year range picker's toggle (tooltip).
    /// </summary>
    public string? YearRangePickerToggleTitle { get; set; }



    /// <summary>
    /// Updates the properties of the specified <see cref="BitCalendar"/> instance with any values that have been set on
    /// this object, if those properties have not already been set on the <see cref="BitCalendar"/>.
    /// </summary>
    /// <remarks>
    /// Only properties that have a value set and have not already been set on the <paramref name="bitCalendar"/> will be updated.
    /// This method does not overwrite existing values on <paramref name="bitCalendar"/>.
    /// </remarks>
    /// <param name="bitCalendar">
    /// The <see cref="BitCalendar"/> instance whose properties will be updated. Cannot be null.
    /// </param>
    public void UpdateParameters(BitCalendar bitCalendar)
    {
        if (bitCalendar is null) return;

        UpdateBaseParameters(bitCalendar);

        // The parameters the calendar rebuilds its view from are the ones carrying a [CallOnSet(OnSetParameters)] on
        // the component, and the component has already run that pass in OnInitialized - before anything cascaded
        // here reached it. So whichever of them the cascade fills in, the pass is run once more at the end.
        var rebuildView = false;

        if (AllowDeselect.HasValue && bitCalendar.HasNotBeenSet(nameof(AllowDeselect)))
        {
            bitCalendar.AllowDeselect = AllowDeselect.Value;
        }

        if (Classes is not null && bitCalendar.HasNotBeenSet(nameof(Classes)))
        {
            bitCalendar.Classes = Classes;

            bitCalendar.ClassBuilder.Reset();
        }

        if (Color.HasValue && bitCalendar.HasNotBeenSet(nameof(Color)))
        {
            bitCalendar.Color = Color.Value;

            bitCalendar.ClassBuilder.Reset();
        }

        if (ContinuousSpinDelay.HasValue && bitCalendar.HasNotBeenSet(nameof(ContinuousSpinDelay)))
        {
            bitCalendar.ContinuousSpinDelay = ContinuousSpinDelay.Value;
        }

        if (ContinuousSpinInterval.HasValue && bitCalendar.HasNotBeenSet(nameof(ContinuousSpinInterval)))
        {
            bitCalendar.ContinuousSpinInterval = ContinuousSpinInterval.Value;
        }

        if (Culture is not null && bitCalendar.HasNotBeenSet(nameof(Culture)))
        {
            bitCalendar.Culture = Culture;

            bitCalendar.ClassBuilder.Reset();

            rebuildView = true;
        }

        if (DateFormat.HasValue() && bitCalendar.HasNotBeenSet(nameof(DateFormat)))
        {
            bitCalendar.DateFormat = DateFormat;
        }

        if (DayCellTemplate is not null && bitCalendar.HasNotBeenSet(nameof(DayCellTemplate)))
        {
            bitCalendar.DayCellTemplate = DayCellTemplate;
        }

        if (DisableFuture.HasValue && bitCalendar.HasNotBeenSet(nameof(DisableFuture)))
        {
            bitCalendar.DisableFuture = DisableFuture.Value;

            rebuildView = true;
        }

        if (DisablePast.HasValue && bitCalendar.HasNotBeenSet(nameof(DisablePast)))
        {
            bitCalendar.DisablePast = DisablePast.Value;

            rebuildView = true;
        }

        if (DisabledDates is not null && bitCalendar.HasNotBeenSet(nameof(DisabledDates)))
        {
            bitCalendar.DisabledDates = DisabledDates;
        }

        if (DisabledDaysOfWeek is not null && bitCalendar.HasNotBeenSet(nameof(DisabledDaysOfWeek)))
        {
            bitCalendar.DisabledDaysOfWeek = DisabledDaysOfWeek;
        }

        if (Events is not null && bitCalendar.HasNotBeenSet(nameof(Events)))
        {
            bitCalendar.Events = Events;
        }

        if (EventDetailsCloseButtonTitle.HasValue() && bitCalendar.HasNotBeenSet(nameof(EventDetailsCloseButtonTitle)))
        {
            bitCalendar.EventDetailsCloseButtonTitle = EventDetailsCloseButtonTitle!;
        }

        if (EventTemplate is not null && bitCalendar.HasNotBeenSet(nameof(EventTemplate)))
        {
            bitCalendar.EventTemplate = EventTemplate;
        }

        if (EventTimeFromText.HasValue() && bitCalendar.HasNotBeenSet(nameof(EventTimeFromText)))
        {
            bitCalendar.EventTimeFromText = EventTimeFromText!;
        }

        if (EventTimeUntilText.HasValue() && bitCalendar.HasNotBeenSet(nameof(EventTimeUntilText)))
        {
            bitCalendar.EventTimeUntilText = EventTimeUntilText!;
        }

        if (FooterTemplate is not null && bitCalendar.HasNotBeenSet(nameof(FooterTemplate)))
        {
            bitCalendar.FooterTemplate = FooterTemplate;
        }

        if (FirstDayOfWeek.HasValue && bitCalendar.HasNotBeenSet(nameof(FirstDayOfWeek)))
        {
            bitCalendar.FirstDayOfWeek = FirstDayOfWeek.Value;

            rebuildView = true;
        }

        if (FixedWeeks.HasValue && bitCalendar.HasNotBeenSet(nameof(FixedWeeks)))
        {
            bitCalendar.FixedWeeks = FixedWeeks.Value;

            rebuildView = true;
        }

        if (GetDayClass is not null && bitCalendar.HasNotBeenSet(nameof(GetDayClass)))
        {
            bitCalendar.GetDayClass = GetDayClass;
        }

        if (HeaderTemplate is not null && bitCalendar.HasNotBeenSet(nameof(HeaderTemplate)))
        {
            bitCalendar.HeaderTemplate = HeaderTemplate;
        }

        if (GoToNextMonthTitle.HasValue() && bitCalendar.HasNotBeenSet(nameof(GoToNextMonthTitle)))
        {
            bitCalendar.GoToNextMonthTitle = GoToNextMonthTitle!;
        }

        if (GoToNextYearRangeTitle.HasValue() && bitCalendar.HasNotBeenSet(nameof(GoToNextYearRangeTitle)))
        {
            bitCalendar.GoToNextYearRangeTitle = GoToNextYearRangeTitle!;
        }

        if (GoToNextYearTitle.HasValue() && bitCalendar.HasNotBeenSet(nameof(GoToNextYearTitle)))
        {
            bitCalendar.GoToNextYearTitle = GoToNextYearTitle!;
        }

        if (NowButtonIcon is not null && bitCalendar.HasNotBeenSet(nameof(NowButtonIcon)))
        {
            bitCalendar.NowButtonIcon = NowButtonIcon;
        }

        if (NowButtonIconName.HasValue() && bitCalendar.HasNotBeenSet(nameof(NowButtonIconName)))
        {
            bitCalendar.NowButtonIconName = NowButtonIconName;
        }

        if (NowButtonTitle.HasValue() && bitCalendar.HasNotBeenSet(nameof(NowButtonTitle)))
        {
            bitCalendar.NowButtonTitle = NowButtonTitle!;
        }

        if (GoToPrevMonthTitle.HasValue() && bitCalendar.HasNotBeenSet(nameof(GoToPrevMonthTitle)))
        {
            bitCalendar.GoToPrevMonthTitle = GoToPrevMonthTitle!;
        }

        if (GoToPrevYearRangeTitle.HasValue() && bitCalendar.HasNotBeenSet(nameof(GoToPrevYearRangeTitle)))
        {
            bitCalendar.GoToPrevYearRangeTitle = GoToPrevYearRangeTitle!;
        }

        if (GoToPrevYearTitle.HasValue() && bitCalendar.HasNotBeenSet(nameof(GoToPrevYearTitle)))
        {
            bitCalendar.GoToPrevYearTitle = GoToPrevYearTitle!;
        }

        if (GoToTodayIcon is not null && bitCalendar.HasNotBeenSet(nameof(GoToTodayIcon)))
        {
            bitCalendar.GoToTodayIcon = GoToTodayIcon;
        }

        if (GoToTodayIconName.HasValue() && bitCalendar.HasNotBeenSet(nameof(GoToTodayIconName)))
        {
            bitCalendar.GoToTodayIconName = GoToTodayIconName;
        }

        if (GoToTodayTitle.HasValue() && bitCalendar.HasNotBeenSet(nameof(GoToTodayTitle)))
        {
            bitCalendar.GoToTodayTitle = GoToTodayTitle!;
        }

        if (HideTimePickerIcon is not null && bitCalendar.HasNotBeenSet(nameof(HideTimePickerIcon)))
        {
            bitCalendar.HideTimePickerIcon = HideTimePickerIcon;
        }

        if (HideTimePickerIconName.HasValue() && bitCalendar.HasNotBeenSet(nameof(HideTimePickerIconName)))
        {
            bitCalendar.HideTimePickerIconName = HideTimePickerIconName;
        }

        if (HideTimePickerTitle.HasValue() && bitCalendar.HasNotBeenSet(nameof(HideTimePickerTitle)))
        {
            bitCalendar.HideTimePickerTitle = HideTimePickerTitle!;
        }

        if (HighlightCurrentMonth.HasValue && bitCalendar.HasNotBeenSet(nameof(HighlightCurrentMonth)))
        {
            bitCalendar.HighlightCurrentMonth = HighlightCurrentMonth.Value;
        }

        if (HighlightedDates is not null && bitCalendar.HasNotBeenSet(nameof(HighlightedDates)))
        {
            bitCalendar.HighlightedDates = HighlightedDates;
        }

        if (HighlightSelectedMonth.HasValue && bitCalendar.HasNotBeenSet(nameof(HighlightSelectedMonth)))
        {
            bitCalendar.HighlightSelectedMonth = HighlightSelectedMonth.Value;
        }

        if (HighlightToday.HasValue && bitCalendar.HasNotBeenSet(nameof(HighlightToday)))
        {
            bitCalendar.HighlightToday = HighlightToday.Value;
        }

        if (HourStep.HasValue && bitCalendar.HasNotBeenSet(nameof(HourStep)))
        {
            bitCalendar.HourStep = HourStep.Value;
        }

        if (InvalidErrorMessage.HasValue() && bitCalendar.HasNotBeenSet(nameof(InvalidErrorMessage)))
        {
            bitCalendar.InvalidErrorMessage = InvalidErrorMessage;
        }

        if (IsDateDisabled is not null && bitCalendar.HasNotBeenSet(nameof(IsDateDisabled)))
        {
            bitCalendar.IsDateDisabled = IsDateDisabled;
        }

        if (MaxDate.HasValue && bitCalendar.HasNotBeenSet(nameof(MaxDate)))
        {
            bitCalendar.MaxDate = MaxDate.Value;

            rebuildView = true;
        }

        if (MinDate.HasValue && bitCalendar.HasNotBeenSet(nameof(MinDate)))
        {
            bitCalendar.MinDate = MinDate.Value;

            rebuildView = true;
        }

        if (MinuteStep.HasValue && bitCalendar.HasNotBeenSet(nameof(MinuteStep)))
        {
            bitCalendar.MinuteStep = MinuteStep.Value;
        }

        if (MonthCount.HasValue && bitCalendar.HasNotBeenSet(nameof(MonthCount)))
        {
            bitCalendar.MonthCount = MonthCount.Value;

            bitCalendar.ClassBuilder.Reset();

            rebuildView = true;
        }

        if (MonthCellTemplate is not null && bitCalendar.HasNotBeenSet(nameof(MonthCellTemplate)))
        {
            bitCalendar.MonthCellTemplate = MonthCellTemplate;
        }

        if (MonthPickerToggleTitle.HasValue() && bitCalendar.HasNotBeenSet(nameof(MonthPickerToggleTitle)))
        {
            bitCalendar.MonthPickerToggleTitle = MonthPickerToggleTitle!;
        }

        if (NextMonthNavIcon is not null && bitCalendar.HasNotBeenSet(nameof(NextMonthNavIcon)))
        {
            bitCalendar.NextMonthNavIcon = NextMonthNavIcon;
        }

        if (NextMonthNavIconName.HasValue() && bitCalendar.HasNotBeenSet(nameof(NextMonthNavIconName)))
        {
            bitCalendar.NextMonthNavIconName = NextMonthNavIconName;
        }

        if (NextYearNavIcon is not null && bitCalendar.HasNotBeenSet(nameof(NextYearNavIcon)))
        {
            bitCalendar.NextYearNavIcon = NextYearNavIcon;
        }

        if (NextYearNavIconName.HasValue() && bitCalendar.HasNotBeenSet(nameof(NextYearNavIconName)))
        {
            bitCalendar.NextYearNavIconName = NextYearNavIconName;
        }

        if (NextYearRangeNavIcon is not null && bitCalendar.HasNotBeenSet(nameof(NextYearRangeNavIcon)))
        {
            bitCalendar.NextYearRangeNavIcon = NextYearRangeNavIcon;
        }

        if (NextYearRangeNavIconName.HasValue() && bitCalendar.HasNotBeenSet(nameof(NextYearRangeNavIconName)))
        {
            bitCalendar.NextYearRangeNavIconName = NextYearRangeNavIconName;
        }

        if (PagedNavigation.HasValue && bitCalendar.HasNotBeenSet(nameof(PagedNavigation)))
        {
            bitCalendar.PagedNavigation = PagedNavigation.Value;
        }

        if (PrevMonthNavIcon is not null && bitCalendar.HasNotBeenSet(nameof(PrevMonthNavIcon)))
        {
            bitCalendar.PrevMonthNavIcon = PrevMonthNavIcon;
        }

        if (PrevMonthNavIconName.HasValue() && bitCalendar.HasNotBeenSet(nameof(PrevMonthNavIconName)))
        {
            bitCalendar.PrevMonthNavIconName = PrevMonthNavIconName;
        }

        if (PrevYearNavIcon is not null && bitCalendar.HasNotBeenSet(nameof(PrevYearNavIcon)))
        {
            bitCalendar.PrevYearNavIcon = PrevYearNavIcon;
        }

        if (PrevYearNavIconName.HasValue() && bitCalendar.HasNotBeenSet(nameof(PrevYearNavIconName)))
        {
            bitCalendar.PrevYearNavIconName = PrevYearNavIconName;
        }

        if (PrevYearRangeNavIcon is not null && bitCalendar.HasNotBeenSet(nameof(PrevYearRangeNavIcon)))
        {
            bitCalendar.PrevYearRangeNavIcon = PrevYearRangeNavIcon;
        }

        if (PrevYearRangeNavIconName.HasValue() && bitCalendar.HasNotBeenSet(nameof(PrevYearRangeNavIconName)))
        {
            bitCalendar.PrevYearRangeNavIconName = PrevYearRangeNavIconName;
        }

        if (SelectedDateAriaAtomic.HasValue() && bitCalendar.HasNotBeenSet(nameof(SelectedDateAriaAtomic)))
        {
            bitCalendar.SelectedDateAriaAtomic = SelectedDateAriaAtomic!;
        }

        if (ShowNowButton.HasValue && bitCalendar.HasNotBeenSet(nameof(ShowNowButton)))
        {
            bitCalendar.ShowNowButton = ShowNowButton.Value;
        }

        if (ShowGoToToday.HasValue && bitCalendar.HasNotBeenSet(nameof(ShowGoToToday)))
        {
            bitCalendar.ShowGoToToday = ShowGoToToday.Value;
        }

        if (ShowEventDetails.HasValue && bitCalendar.HasNotBeenSet(nameof(ShowEventDetails)))
        {
            bitCalendar.ShowEventDetails = ShowEventDetails.Value;
        }

        if (ShowMonthPicker.HasValue && bitCalendar.HasNotBeenSet(nameof(ShowMonthPicker)))
        {
            bitCalendar.ShowMonthPicker = ShowMonthPicker.Value;

            rebuildView = true;
        }

        if (ShowMonthPickerAsOverlay.HasValue && bitCalendar.HasNotBeenSet(nameof(ShowMonthPickerAsOverlay)))
        {
            bitCalendar.ShowMonthPickerAsOverlay = ShowMonthPickerAsOverlay.Value;

            rebuildView = true;
        }

        if (ShowOutsideDays.HasValue && bitCalendar.HasNotBeenSet(nameof(ShowOutsideDays)))
        {
            bitCalendar.ShowOutsideDays = ShowOutsideDays.Value;
        }

        if (ShowTimePicker.HasValue && bitCalendar.HasNotBeenSet(nameof(ShowTimePicker)))
        {
            bitCalendar.ShowTimePicker = ShowTimePicker.Value;

            rebuildView = true;
        }

        if (ShowTimePickerAsOverlay.HasValue && bitCalendar.HasNotBeenSet(nameof(ShowTimePickerAsOverlay)))
        {
            bitCalendar.ShowTimePickerAsOverlay = ShowTimePickerAsOverlay.Value;

            rebuildView = true;
        }

        if (ShowTimePickerIcon is not null && bitCalendar.HasNotBeenSet(nameof(ShowTimePickerIcon)))
        {
            bitCalendar.ShowTimePickerIcon = ShowTimePickerIcon;
        }

        if (ShowTimePickerIconName.HasValue() && bitCalendar.HasNotBeenSet(nameof(ShowTimePickerIconName)))
        {
            bitCalendar.ShowTimePickerIconName = ShowTimePickerIconName;
        }

        if (ShowTimePickerTitle.HasValue() && bitCalendar.HasNotBeenSet(nameof(ShowTimePickerTitle)))
        {
            bitCalendar.ShowTimePickerTitle = ShowTimePickerTitle!;
        }

        if (ShowWeekNumbers.HasValue && bitCalendar.HasNotBeenSet(nameof(ShowWeekNumbers)))
        {
            bitCalendar.ShowWeekNumbers = ShowWeekNumbers.Value;
        }

        if (Size.HasValue && bitCalendar.HasNotBeenSet(nameof(Size)))
        {
            bitCalendar.Size = Size.Value;

            bitCalendar.ClassBuilder.Reset();
        }

        if (StartingValue.HasValue && bitCalendar.HasNotBeenSet(nameof(StartingValue)))
        {
            bitCalendar.StartingValue = StartingValue.Value;

            rebuildView = true;
        }

        if (Styles is not null && bitCalendar.HasNotBeenSet(nameof(Styles)))
        {
            bitCalendar.Styles = Styles;

            bitCalendar.StyleBuilder.Reset();
        }

        if (TimeFormat.HasValue && bitCalendar.HasNotBeenSet(nameof(TimeFormat)))
        {
            bitCalendar.TimeFormat = TimeFormat.Value;
        }

        if (TimePickerDecreaseHourIcon is not null && bitCalendar.HasNotBeenSet(nameof(TimePickerDecreaseHourIcon)))
        {
            bitCalendar.TimePickerDecreaseHourIcon = TimePickerDecreaseHourIcon;
        }

        if (TimePickerDecreaseHourIconName.HasValue() && bitCalendar.HasNotBeenSet(nameof(TimePickerDecreaseHourIconName)))
        {
            bitCalendar.TimePickerDecreaseHourIconName = TimePickerDecreaseHourIconName;
        }

        if (TimePickerDecreaseHourTitle.HasValue() && bitCalendar.HasNotBeenSet(nameof(TimePickerDecreaseHourTitle)))
        {
            bitCalendar.TimePickerDecreaseHourTitle = TimePickerDecreaseHourTitle!;
        }

        if (TimePickerDecreaseMinuteTitle.HasValue() && bitCalendar.HasNotBeenSet(nameof(TimePickerDecreaseMinuteTitle)))
        {
            bitCalendar.TimePickerDecreaseMinuteTitle = TimePickerDecreaseMinuteTitle!;
        }

        if (TimePickerDecreaseMinuteIcon is not null && bitCalendar.HasNotBeenSet(nameof(TimePickerDecreaseMinuteIcon)))
        {
            bitCalendar.TimePickerDecreaseMinuteIcon = TimePickerDecreaseMinuteIcon;
        }

        if (TimePickerDecreaseMinuteIconName.HasValue() && bitCalendar.HasNotBeenSet(nameof(TimePickerDecreaseMinuteIconName)))
        {
            bitCalendar.TimePickerDecreaseMinuteIconName = TimePickerDecreaseMinuteIconName;
        }

        if (TimePickerHourTitle.HasValue() && bitCalendar.HasNotBeenSet(nameof(TimePickerHourTitle)))
        {
            bitCalendar.TimePickerHourTitle = TimePickerHourTitle!;
        }

        if (TimePickerMinuteTitle.HasValue() && bitCalendar.HasNotBeenSet(nameof(TimePickerMinuteTitle)))
        {
            bitCalendar.TimePickerMinuteTitle = TimePickerMinuteTitle!;
        }

        if (TimePickerIncreaseHourIcon is not null && bitCalendar.HasNotBeenSet(nameof(TimePickerIncreaseHourIcon)))
        {
            bitCalendar.TimePickerIncreaseHourIcon = TimePickerIncreaseHourIcon;
        }

        if (TimePickerIncreaseHourIconName.HasValue() && bitCalendar.HasNotBeenSet(nameof(TimePickerIncreaseHourIconName)))
        {
            bitCalendar.TimePickerIncreaseHourIconName = TimePickerIncreaseHourIconName;
        }

        if (TimePickerIncreaseMinuteIcon is not null && bitCalendar.HasNotBeenSet(nameof(TimePickerIncreaseMinuteIcon)))
        {
            bitCalendar.TimePickerIncreaseMinuteIcon = TimePickerIncreaseMinuteIcon;
        }

        if (TimePickerIncreaseMinuteIconName.HasValue() && bitCalendar.HasNotBeenSet(nameof(TimePickerIncreaseMinuteIconName)))
        {
            bitCalendar.TimePickerIncreaseMinuteIconName = TimePickerIncreaseMinuteIconName;
        }

        if (TimePickerIncreaseHourTitle.HasValue() && bitCalendar.HasNotBeenSet(nameof(TimePickerIncreaseHourTitle)))
        {
            bitCalendar.TimePickerIncreaseHourTitle = TimePickerIncreaseHourTitle!;
        }

        if (TimePickerIncreaseMinuteTitle.HasValue() && bitCalendar.HasNotBeenSet(nameof(TimePickerIncreaseMinuteTitle)))
        {
            bitCalendar.TimePickerIncreaseMinuteTitle = TimePickerIncreaseMinuteTitle!;
        }

        if (TimeZone is not null && bitCalendar.HasNotBeenSet(nameof(TimeZone)))
        {
            bitCalendar.TimeZone = TimeZone;

            rebuildView = true;
        }

        if (Today.HasValue && bitCalendar.HasNotBeenSet(nameof(Today)))
        {
            bitCalendar.Today = Today.Value;
        }

        if (WeekNumberRule.HasValue && bitCalendar.HasNotBeenSet(nameof(WeekNumberRule)))
        {
            bitCalendar.WeekNumberRule = WeekNumberRule.Value;
        }

        if (WeekNumbersHeaderTitle.HasValue() && bitCalendar.HasNotBeenSet(nameof(WeekNumbersHeaderTitle)))
        {
            bitCalendar.WeekNumbersHeaderTitle = WeekNumbersHeaderTitle!;
        }

        if (WeekNumberTitle.HasValue() && bitCalendar.HasNotBeenSet(nameof(WeekNumberTitle)))
        {
            bitCalendar.WeekNumberTitle = WeekNumberTitle!;
        }

        if (YearCellTemplate is not null && bitCalendar.HasNotBeenSet(nameof(YearCellTemplate)))
        {
            bitCalendar.YearCellTemplate = YearCellTemplate;
        }

        if (YearPickerToggleTitle.HasValue() && bitCalendar.HasNotBeenSet(nameof(YearPickerToggleTitle)))
        {
            bitCalendar.YearPickerToggleTitle = YearPickerToggleTitle!;
        }

        if (YearRangePickerToggleTitle.HasValue() && bitCalendar.HasNotBeenSet(nameof(YearRangePickerToggleTitle)))
        {
            bitCalendar.YearRangePickerToggleTitle = YearRangePickerToggleTitle!;
        }

        if (rebuildView)
        {
            bitCalendar.OnSetParameters();
        }
    }
}
