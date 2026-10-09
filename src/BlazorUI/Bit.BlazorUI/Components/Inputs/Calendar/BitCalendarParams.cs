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

        if (AllowDeselect.HasValue)
        {
            bitCalendar.TakeFromCascade(nameof(AllowDeselect), AllowDeselect.Value, static c => c.AllowDeselect, static (c, v) => c.AllowDeselect = v);
        }

        if (Classes is not null)
        {
            bitCalendar.TakeFromCascade(nameof(Classes), Classes, static c => c.Classes, static (c, v) => c.Classes = v);
        }

        if (Color.HasValue)
        {
            bitCalendar.TakeFromCascade(nameof(Color), Color.Value, static c => c.Color, static (c, v) => c.Color = v);
        }

        if (ContinuousSpinDelay.HasValue)
        {
            bitCalendar.TakeFromCascade(nameof(ContinuousSpinDelay), ContinuousSpinDelay.Value, static c => c.ContinuousSpinDelay, static (c, v) => c.ContinuousSpinDelay = v);
        }

        if (ContinuousSpinInterval.HasValue)
        {
            bitCalendar.TakeFromCascade(nameof(ContinuousSpinInterval), ContinuousSpinInterval.Value, static c => c.ContinuousSpinInterval, static (c, v) => c.ContinuousSpinInterval = v);
        }

        if (Culture is not null)
        {
            bitCalendar.TakeFromCascade(nameof(Culture), Culture, static c => c.Culture, static (c, v) => c.Culture = v);
        }

        if (DateFormat.HasValue())
        {
            bitCalendar.TakeFromCascade(nameof(DateFormat), DateFormat, static c => c.DateFormat, static (c, v) => c.DateFormat = v);
        }

        if (DayCellTemplate is not null)
        {
            bitCalendar.TakeFromCascade(nameof(DayCellTemplate), DayCellTemplate, static c => c.DayCellTemplate, static (c, v) => c.DayCellTemplate = v);
        }

        if (DisableFuture.HasValue)
        {
            bitCalendar.TakeFromCascade(nameof(DisableFuture), DisableFuture.Value, static c => c.DisableFuture, static (c, v) => c.DisableFuture = v);
        }

        if (DisablePast.HasValue)
        {
            bitCalendar.TakeFromCascade(nameof(DisablePast), DisablePast.Value, static c => c.DisablePast, static (c, v) => c.DisablePast = v);
        }

        if (DisabledDates is not null)
        {
            bitCalendar.TakeFromCascade(nameof(DisabledDates), DisabledDates, static c => c.DisabledDates, static (c, v) => c.DisabledDates = v);
        }

        if (DisabledDaysOfWeek is not null)
        {
            bitCalendar.TakeFromCascade(nameof(DisabledDaysOfWeek), DisabledDaysOfWeek, static c => c.DisabledDaysOfWeek, static (c, v) => c.DisabledDaysOfWeek = v);
        }

        if (Events is not null)
        {
            bitCalendar.TakeFromCascade(nameof(Events), Events, static c => c.Events, static (c, v) => c.Events = v);
        }

        if (EventDetailsCloseButtonTitle.HasValue())
        {
            bitCalendar.TakeFromCascade(nameof(EventDetailsCloseButtonTitle), EventDetailsCloseButtonTitle!, static c => c.EventDetailsCloseButtonTitle, static (c, v) => c.EventDetailsCloseButtonTitle = v);
        }

        if (EventTemplate is not null)
        {
            bitCalendar.TakeFromCascade(nameof(EventTemplate), EventTemplate, static c => c.EventTemplate, static (c, v) => c.EventTemplate = v);
        }

        if (EventTimeFromText.HasValue())
        {
            bitCalendar.TakeFromCascade(nameof(EventTimeFromText), EventTimeFromText!, static c => c.EventTimeFromText, static (c, v) => c.EventTimeFromText = v);
        }

        if (EventTimeUntilText.HasValue())
        {
            bitCalendar.TakeFromCascade(nameof(EventTimeUntilText), EventTimeUntilText!, static c => c.EventTimeUntilText, static (c, v) => c.EventTimeUntilText = v);
        }

        if (FooterTemplate is not null)
        {
            bitCalendar.TakeFromCascade(nameof(FooterTemplate), FooterTemplate, static c => c.FooterTemplate, static (c, v) => c.FooterTemplate = v);
        }

        if (FirstDayOfWeek.HasValue)
        {
            bitCalendar.TakeFromCascade(nameof(FirstDayOfWeek), FirstDayOfWeek.Value, static c => c.FirstDayOfWeek, static (c, v) => c.FirstDayOfWeek = v);
        }

        if (FixedWeeks.HasValue)
        {
            bitCalendar.TakeFromCascade(nameof(FixedWeeks), FixedWeeks.Value, static c => c.FixedWeeks, static (c, v) => c.FixedWeeks = v);
        }

        if (GetDayClass is not null)
        {
            bitCalendar.TakeFromCascade(nameof(GetDayClass), GetDayClass, static c => c.GetDayClass, static (c, v) => c.GetDayClass = v);
        }

        if (HeaderTemplate is not null)
        {
            bitCalendar.TakeFromCascade(nameof(HeaderTemplate), HeaderTemplate, static c => c.HeaderTemplate, static (c, v) => c.HeaderTemplate = v);
        }

        if (GoToNextMonthTitle.HasValue())
        {
            bitCalendar.TakeFromCascade(nameof(GoToNextMonthTitle), GoToNextMonthTitle!, static c => c.GoToNextMonthTitle, static (c, v) => c.GoToNextMonthTitle = v);
        }

        if (GoToNextYearRangeTitle.HasValue())
        {
            bitCalendar.TakeFromCascade(nameof(GoToNextYearRangeTitle), GoToNextYearRangeTitle!, static c => c.GoToNextYearRangeTitle, static (c, v) => c.GoToNextYearRangeTitle = v);
        }

        if (GoToNextYearTitle.HasValue())
        {
            bitCalendar.TakeFromCascade(nameof(GoToNextYearTitle), GoToNextYearTitle!, static c => c.GoToNextYearTitle, static (c, v) => c.GoToNextYearTitle = v);
        }


        if (NowButtonIcon is not null)
        {
            bitCalendar.TakeFromCascade(nameof(NowButtonIcon), NowButtonIcon, static c => c.NowButtonIcon, static (c, v) => c.NowButtonIcon = v);
        }

        if (NowButtonIconName.HasValue())
        {
            bitCalendar.TakeFromCascade(nameof(NowButtonIconName), NowButtonIconName, static c => c.NowButtonIconName, static (c, v) => c.NowButtonIconName = v);
        }

        if (NowButtonTitle.HasValue())
        {
            bitCalendar.TakeFromCascade(nameof(NowButtonTitle), NowButtonTitle!, static c => c.NowButtonTitle, static (c, v) => c.NowButtonTitle = v);
        }

        if (GoToPrevMonthTitle.HasValue())
        {
            bitCalendar.TakeFromCascade(nameof(GoToPrevMonthTitle), GoToPrevMonthTitle!, static c => c.GoToPrevMonthTitle, static (c, v) => c.GoToPrevMonthTitle = v);
        }

        if (GoToPrevYearRangeTitle.HasValue())
        {
            bitCalendar.TakeFromCascade(nameof(GoToPrevYearRangeTitle), GoToPrevYearRangeTitle!, static c => c.GoToPrevYearRangeTitle, static (c, v) => c.GoToPrevYearRangeTitle = v);
        }

        if (GoToPrevYearTitle.HasValue())
        {
            bitCalendar.TakeFromCascade(nameof(GoToPrevYearTitle), GoToPrevYearTitle!, static c => c.GoToPrevYearTitle, static (c, v) => c.GoToPrevYearTitle = v);
        }


        if (GoToTodayIcon is not null)
        {
            bitCalendar.TakeFromCascade(nameof(GoToTodayIcon), GoToTodayIcon, static c => c.GoToTodayIcon, static (c, v) => c.GoToTodayIcon = v);
        }

        if (GoToTodayIconName.HasValue())
        {
            bitCalendar.TakeFromCascade(nameof(GoToTodayIconName), GoToTodayIconName, static c => c.GoToTodayIconName, static (c, v) => c.GoToTodayIconName = v);
        }

        if (GoToTodayTitle.HasValue())
        {
            bitCalendar.TakeFromCascade(nameof(GoToTodayTitle), GoToTodayTitle!, static c => c.GoToTodayTitle, static (c, v) => c.GoToTodayTitle = v);
        }


        if (HideTimePickerIcon is not null)
        {
            bitCalendar.TakeFromCascade(nameof(HideTimePickerIcon), HideTimePickerIcon, static c => c.HideTimePickerIcon, static (c, v) => c.HideTimePickerIcon = v);
        }

        if (HideTimePickerIconName.HasValue())
        {
            bitCalendar.TakeFromCascade(nameof(HideTimePickerIconName), HideTimePickerIconName, static c => c.HideTimePickerIconName, static (c, v) => c.HideTimePickerIconName = v);
        }

        if (HideTimePickerTitle.HasValue())
        {
            bitCalendar.TakeFromCascade(nameof(HideTimePickerTitle), HideTimePickerTitle!, static c => c.HideTimePickerTitle, static (c, v) => c.HideTimePickerTitle = v);
        }

        if (HighlightCurrentMonth.HasValue)
        {
            bitCalendar.TakeFromCascade(nameof(HighlightCurrentMonth), HighlightCurrentMonth.Value, static c => c.HighlightCurrentMonth, static (c, v) => c.HighlightCurrentMonth = v);
        }

        if (HighlightedDates is not null)
        {
            bitCalendar.TakeFromCascade(nameof(HighlightedDates), HighlightedDates, static c => c.HighlightedDates, static (c, v) => c.HighlightedDates = v);
        }

        if (HighlightSelectedMonth.HasValue)
        {
            bitCalendar.TakeFromCascade(nameof(HighlightSelectedMonth), HighlightSelectedMonth.Value, static c => c.HighlightSelectedMonth, static (c, v) => c.HighlightSelectedMonth = v);
        }

        if (HighlightToday.HasValue)
        {
            bitCalendar.TakeFromCascade(nameof(HighlightToday), HighlightToday.Value, static c => c.HighlightToday, static (c, v) => c.HighlightToday = v);
        }

        if (HourStep.HasValue)
        {
            bitCalendar.TakeFromCascade(nameof(HourStep), HourStep.Value, static c => c.HourStep, static (c, v) => c.HourStep = v);
        }

        if (InvalidErrorMessage.HasValue())
        {
            bitCalendar.TakeFromCascade(nameof(InvalidErrorMessage), InvalidErrorMessage, static c => c.InvalidErrorMessage, static (c, v) => c.InvalidErrorMessage = v);
        }

        if (IsDateDisabled is not null)
        {
            bitCalendar.TakeFromCascade(nameof(IsDateDisabled), IsDateDisabled, static c => c.IsDateDisabled, static (c, v) => c.IsDateDisabled = v);
        }

        if (MaxDate.HasValue)
        {
            bitCalendar.TakeFromCascade(nameof(MaxDate), MaxDate.Value, static c => c.MaxDate, static (c, v) => c.MaxDate = v);
        }

        if (MinDate.HasValue)
        {
            bitCalendar.TakeFromCascade(nameof(MinDate), MinDate.Value, static c => c.MinDate, static (c, v) => c.MinDate = v);
        }

        if (MinuteStep.HasValue)
        {
            bitCalendar.TakeFromCascade(nameof(MinuteStep), MinuteStep.Value, static c => c.MinuteStep, static (c, v) => c.MinuteStep = v);
        }

        if (MonthCount.HasValue)
        {
            bitCalendar.TakeFromCascade(nameof(MonthCount), MonthCount.Value, static c => c.MonthCount, static (c, v) => c.MonthCount = v);
        }

        if (MonthCellTemplate is not null)
        {
            bitCalendar.TakeFromCascade(nameof(MonthCellTemplate), MonthCellTemplate, static c => c.MonthCellTemplate, static (c, v) => c.MonthCellTemplate = v);
        }

        if (MonthPickerToggleTitle.HasValue())
        {
            bitCalendar.TakeFromCascade(nameof(MonthPickerToggleTitle), MonthPickerToggleTitle!, static c => c.MonthPickerToggleTitle, static (c, v) => c.MonthPickerToggleTitle = v);
        }


        if (NextMonthNavIcon is not null)
        {
            bitCalendar.TakeFromCascade(nameof(NextMonthNavIcon), NextMonthNavIcon, static c => c.NextMonthNavIcon, static (c, v) => c.NextMonthNavIcon = v);
        }

        if (NextMonthNavIconName.HasValue())
        {
            bitCalendar.TakeFromCascade(nameof(NextMonthNavIconName), NextMonthNavIconName, static c => c.NextMonthNavIconName, static (c, v) => c.NextMonthNavIconName = v);
        }


        if (NextYearNavIcon is not null)
        {
            bitCalendar.TakeFromCascade(nameof(NextYearNavIcon), NextYearNavIcon, static c => c.NextYearNavIcon, static (c, v) => c.NextYearNavIcon = v);
        }

        if (NextYearNavIconName.HasValue())
        {
            bitCalendar.TakeFromCascade(nameof(NextYearNavIconName), NextYearNavIconName, static c => c.NextYearNavIconName, static (c, v) => c.NextYearNavIconName = v);
        }


        if (NextYearRangeNavIcon is not null)
        {
            bitCalendar.TakeFromCascade(nameof(NextYearRangeNavIcon), NextYearRangeNavIcon, static c => c.NextYearRangeNavIcon, static (c, v) => c.NextYearRangeNavIcon = v);
        }

        if (NextYearRangeNavIconName.HasValue())
        {
            bitCalendar.TakeFromCascade(nameof(NextYearRangeNavIconName), NextYearRangeNavIconName, static c => c.NextYearRangeNavIconName, static (c, v) => c.NextYearRangeNavIconName = v);
        }

        if (PagedNavigation.HasValue)
        {
            bitCalendar.TakeFromCascade(nameof(PagedNavigation), PagedNavigation.Value, static c => c.PagedNavigation, static (c, v) => c.PagedNavigation = v);
        }


        if (PrevMonthNavIcon is not null)
        {
            bitCalendar.TakeFromCascade(nameof(PrevMonthNavIcon), PrevMonthNavIcon, static c => c.PrevMonthNavIcon, static (c, v) => c.PrevMonthNavIcon = v);
        }

        if (PrevMonthNavIconName.HasValue())
        {
            bitCalendar.TakeFromCascade(nameof(PrevMonthNavIconName), PrevMonthNavIconName, static c => c.PrevMonthNavIconName, static (c, v) => c.PrevMonthNavIconName = v);
        }


        if (PrevYearNavIcon is not null)
        {
            bitCalendar.TakeFromCascade(nameof(PrevYearNavIcon), PrevYearNavIcon, static c => c.PrevYearNavIcon, static (c, v) => c.PrevYearNavIcon = v);
        }

        if (PrevYearNavIconName.HasValue())
        {
            bitCalendar.TakeFromCascade(nameof(PrevYearNavIconName), PrevYearNavIconName, static c => c.PrevYearNavIconName, static (c, v) => c.PrevYearNavIconName = v);
        }


        if (PrevYearRangeNavIcon is not null)
        {
            bitCalendar.TakeFromCascade(nameof(PrevYearRangeNavIcon), PrevYearRangeNavIcon, static c => c.PrevYearRangeNavIcon, static (c, v) => c.PrevYearRangeNavIcon = v);
        }

        if (PrevYearRangeNavIconName.HasValue())
        {
            bitCalendar.TakeFromCascade(nameof(PrevYearRangeNavIconName), PrevYearRangeNavIconName, static c => c.PrevYearRangeNavIconName, static (c, v) => c.PrevYearRangeNavIconName = v);
        }

        if (SelectedDateAriaAtomic.HasValue())
        {
            bitCalendar.TakeFromCascade(nameof(SelectedDateAriaAtomic), SelectedDateAriaAtomic!, static c => c.SelectedDateAriaAtomic, static (c, v) => c.SelectedDateAriaAtomic = v);
        }

        if (ShowNowButton.HasValue)
        {
            bitCalendar.TakeFromCascade(nameof(ShowNowButton), ShowNowButton.Value, static c => c.ShowNowButton, static (c, v) => c.ShowNowButton = v);
        }

        if (ShowGoToToday.HasValue)
        {
            bitCalendar.TakeFromCascade(nameof(ShowGoToToday), ShowGoToToday.Value, static c => c.ShowGoToToday, static (c, v) => c.ShowGoToToday = v);
        }

        if (ShowEventDetails.HasValue)
        {
            bitCalendar.TakeFromCascade(nameof(ShowEventDetails), ShowEventDetails.Value, static c => c.ShowEventDetails, static (c, v) => c.ShowEventDetails = v);
        }

        if (ShowMonthPicker.HasValue)
        {
            bitCalendar.TakeFromCascade(nameof(ShowMonthPicker), ShowMonthPicker.Value, static c => c.ShowMonthPicker, static (c, v) => c.ShowMonthPicker = v);
        }

        if (ShowMonthPickerAsOverlay.HasValue)
        {
            bitCalendar.TakeFromCascade(nameof(ShowMonthPickerAsOverlay), ShowMonthPickerAsOverlay.Value, static c => c.ShowMonthPickerAsOverlay, static (c, v) => c.ShowMonthPickerAsOverlay = v);
        }

        if (ShowOutsideDays.HasValue)
        {
            bitCalendar.TakeFromCascade(nameof(ShowOutsideDays), ShowOutsideDays.Value, static c => c.ShowOutsideDays, static (c, v) => c.ShowOutsideDays = v);
        }

        if (ShowTimePicker.HasValue)
        {
            bitCalendar.TakeFromCascade(nameof(ShowTimePicker), ShowTimePicker.Value, static c => c.ShowTimePicker, static (c, v) => c.ShowTimePicker = v);
        }

        if (ShowTimePickerAsOverlay.HasValue)
        {
            bitCalendar.TakeFromCascade(nameof(ShowTimePickerAsOverlay), ShowTimePickerAsOverlay.Value, static c => c.ShowTimePickerAsOverlay, static (c, v) => c.ShowTimePickerAsOverlay = v);
        }


        if (ShowTimePickerIcon is not null)
        {
            bitCalendar.TakeFromCascade(nameof(ShowTimePickerIcon), ShowTimePickerIcon, static c => c.ShowTimePickerIcon, static (c, v) => c.ShowTimePickerIcon = v);
        }

        if (ShowTimePickerIconName.HasValue())
        {
            bitCalendar.TakeFromCascade(nameof(ShowTimePickerIconName), ShowTimePickerIconName, static c => c.ShowTimePickerIconName, static (c, v) => c.ShowTimePickerIconName = v);
        }

        if (ShowTimePickerTitle.HasValue())
        {
            bitCalendar.TakeFromCascade(nameof(ShowTimePickerTitle), ShowTimePickerTitle!, static c => c.ShowTimePickerTitle, static (c, v) => c.ShowTimePickerTitle = v);
        }

        if (ShowWeekNumbers.HasValue)
        {
            bitCalendar.TakeFromCascade(nameof(ShowWeekNumbers), ShowWeekNumbers.Value, static c => c.ShowWeekNumbers, static (c, v) => c.ShowWeekNumbers = v);
        }

        if (Size.HasValue)
        {
            bitCalendar.TakeFromCascade(nameof(Size), Size.Value, static c => c.Size, static (c, v) => c.Size = v);
        }

        if (StartingValue.HasValue)
        {
            bitCalendar.TakeFromCascade(nameof(StartingValue), StartingValue.Value, static c => c.StartingValue, static (c, v) => c.StartingValue = v);
        }

        if (Styles is not null)
        {
            bitCalendar.TakeFromCascade(nameof(Styles), Styles, static c => c.Styles, static (c, v) => c.Styles = v);
        }

        if (TimeFormat.HasValue)
        {
            bitCalendar.TakeFromCascade(nameof(TimeFormat), TimeFormat.Value, static c => c.TimeFormat, static (c, v) => c.TimeFormat = v);
        }


        if (TimePickerDecreaseHourIcon is not null)
        {
            bitCalendar.TakeFromCascade(nameof(TimePickerDecreaseHourIcon), TimePickerDecreaseHourIcon, static c => c.TimePickerDecreaseHourIcon, static (c, v) => c.TimePickerDecreaseHourIcon = v);
        }

        if (TimePickerDecreaseHourIconName.HasValue())
        {
            bitCalendar.TakeFromCascade(nameof(TimePickerDecreaseHourIconName), TimePickerDecreaseHourIconName, static c => c.TimePickerDecreaseHourIconName, static (c, v) => c.TimePickerDecreaseHourIconName = v);
        }

        if (TimePickerDecreaseHourTitle.HasValue())
        {
            bitCalendar.TakeFromCascade(nameof(TimePickerDecreaseHourTitle), TimePickerDecreaseHourTitle!, static c => c.TimePickerDecreaseHourTitle, static (c, v) => c.TimePickerDecreaseHourTitle = v);
        }

        if (TimePickerDecreaseMinuteTitle.HasValue())
        {
            bitCalendar.TakeFromCascade(nameof(TimePickerDecreaseMinuteTitle), TimePickerDecreaseMinuteTitle!, static c => c.TimePickerDecreaseMinuteTitle, static (c, v) => c.TimePickerDecreaseMinuteTitle = v);
        }


        if (TimePickerDecreaseMinuteIcon is not null)
        {
            bitCalendar.TakeFromCascade(nameof(TimePickerDecreaseMinuteIcon), TimePickerDecreaseMinuteIcon, static c => c.TimePickerDecreaseMinuteIcon, static (c, v) => c.TimePickerDecreaseMinuteIcon = v);
        }

        if (TimePickerDecreaseMinuteIconName.HasValue())
        {
            bitCalendar.TakeFromCascade(nameof(TimePickerDecreaseMinuteIconName), TimePickerDecreaseMinuteIconName, static c => c.TimePickerDecreaseMinuteIconName, static (c, v) => c.TimePickerDecreaseMinuteIconName = v);
        }

        if (TimePickerHourTitle.HasValue())
        {
            bitCalendar.TakeFromCascade(nameof(TimePickerHourTitle), TimePickerHourTitle!, static c => c.TimePickerHourTitle, static (c, v) => c.TimePickerHourTitle = v);
        }

        if (TimePickerMinuteTitle.HasValue())
        {
            bitCalendar.TakeFromCascade(nameof(TimePickerMinuteTitle), TimePickerMinuteTitle!, static c => c.TimePickerMinuteTitle, static (c, v) => c.TimePickerMinuteTitle = v);
        }


        if (TimePickerIncreaseHourIcon is not null)
        {
            bitCalendar.TakeFromCascade(nameof(TimePickerIncreaseHourIcon), TimePickerIncreaseHourIcon, static c => c.TimePickerIncreaseHourIcon, static (c, v) => c.TimePickerIncreaseHourIcon = v);
        }

        if (TimePickerIncreaseHourIconName.HasValue())
        {
            bitCalendar.TakeFromCascade(nameof(TimePickerIncreaseHourIconName), TimePickerIncreaseHourIconName, static c => c.TimePickerIncreaseHourIconName, static (c, v) => c.TimePickerIncreaseHourIconName = v);
        }


        if (TimePickerIncreaseMinuteIcon is not null)
        {
            bitCalendar.TakeFromCascade(nameof(TimePickerIncreaseMinuteIcon), TimePickerIncreaseMinuteIcon, static c => c.TimePickerIncreaseMinuteIcon, static (c, v) => c.TimePickerIncreaseMinuteIcon = v);
        }

        if (TimePickerIncreaseMinuteIconName.HasValue())
        {
            bitCalendar.TakeFromCascade(nameof(TimePickerIncreaseMinuteIconName), TimePickerIncreaseMinuteIconName, static c => c.TimePickerIncreaseMinuteIconName, static (c, v) => c.TimePickerIncreaseMinuteIconName = v);
        }

        if (TimePickerIncreaseHourTitle.HasValue())
        {
            bitCalendar.TakeFromCascade(nameof(TimePickerIncreaseHourTitle), TimePickerIncreaseHourTitle!, static c => c.TimePickerIncreaseHourTitle, static (c, v) => c.TimePickerIncreaseHourTitle = v);
        }

        if (TimePickerIncreaseMinuteTitle.HasValue())
        {
            bitCalendar.TakeFromCascade(nameof(TimePickerIncreaseMinuteTitle), TimePickerIncreaseMinuteTitle!, static c => c.TimePickerIncreaseMinuteTitle, static (c, v) => c.TimePickerIncreaseMinuteTitle = v);
        }

        if (TimeZone is not null)
        {
            bitCalendar.TakeFromCascade(nameof(TimeZone), TimeZone, static c => c.TimeZone, static (c, v) => c.TimeZone = v);
        }

        if (Today.HasValue)
        {
            bitCalendar.TakeFromCascade(nameof(Today), Today.Value, static c => c.Today, static (c, v) => c.Today = v);
        }

        if (WeekNumberRule.HasValue)
        {
            bitCalendar.TakeFromCascade(nameof(WeekNumberRule), WeekNumberRule.Value, static c => c.WeekNumberRule, static (c, v) => c.WeekNumberRule = v);
        }

        if (WeekNumbersHeaderTitle.HasValue())
        {
            bitCalendar.TakeFromCascade(nameof(WeekNumbersHeaderTitle), WeekNumbersHeaderTitle!, static c => c.WeekNumbersHeaderTitle, static (c, v) => c.WeekNumbersHeaderTitle = v);
        }

        if (WeekNumberTitle.HasValue())
        {
            bitCalendar.TakeFromCascade(nameof(WeekNumberTitle), WeekNumberTitle!, static c => c.WeekNumberTitle, static (c, v) => c.WeekNumberTitle = v);
        }

        if (YearCellTemplate is not null)
        {
            bitCalendar.TakeFromCascade(nameof(YearCellTemplate), YearCellTemplate, static c => c.YearCellTemplate, static (c, v) => c.YearCellTemplate = v);
        }

        if (YearPickerToggleTitle.HasValue())
        {
            bitCalendar.TakeFromCascade(nameof(YearPickerToggleTitle), YearPickerToggleTitle!, static c => c.YearPickerToggleTitle, static (c, v) => c.YearPickerToggleTitle = v);
        }

        if (YearRangePickerToggleTitle.HasValue())
        {
            bitCalendar.TakeFromCascade(nameof(YearRangePickerToggleTitle), YearRangePickerToggleTitle!, static c => c.YearRangePickerToggleTitle, static (c, v) => c.YearRangePickerToggleTitle = v);
        }
    }
}
