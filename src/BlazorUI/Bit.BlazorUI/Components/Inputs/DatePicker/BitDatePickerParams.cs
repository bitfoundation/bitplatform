using System.Globalization;

namespace Bit.BlazorUI;

/// <summary>
/// The parameters for <see cref="BitDatePicker"/> component.
/// </summary>
public class BitDatePickerParams : BitComponentBaseParams, IBitComponentParams
{
    /// <summary>
    /// Represents the parameter name used to identify the <see cref="BitDatePicker"/> cascading parameters within <see cref="BitParams"/>.
    /// </summary>
    /// <remarks>
    /// This constant is typically used when referencing or accessing the BitDatePicker value in
    /// parameterized APIs or configuration settings. Using this constant helps ensure consistency and reduces the risk
    /// of typographical errors.
    /// </remarks>
    public const string ParamName = $"{nameof(BitParams)}.{nameof(BitDatePicker)}";



    public string Name => ParamName;



    /// <summary>
    /// Whether selecting the already selected date deselects it, clearing the value.
    /// The callout stays open after a deselection, so another date can be picked right away.
    /// </summary>
    public bool? AllowDeselect { get; set; }

    /// <summary>
    /// The hours the time picker can be set to, on top of what <see cref="MinTime"/>, <see cref="MaxTime"/> and
    /// the bounds of the day already allow.
    /// </summary>
    public Func<int, bool>? AllowedHours { get; set; }

    /// <summary>
    /// The minutes the time picker can be set to, on top of what <see cref="MinTime"/>, <see cref="MaxTime"/> and
    /// the bounds of the day already allow.
    /// </summary>
    public Func<int, bool>? AllowedMinutes { get; set; }

    /// <summary>
    /// The seconds the time picker can be set to, on top of what <see cref="MinTime"/>, <see cref="MaxTime"/> and
    /// the bounds of the day already allow. It only has an effect while <see cref="ShowSeconds"/> is set.
    /// </summary>
    public Func<int, bool>? AllowedSeconds { get; set; }

    /// <summary>
    /// Whether or not the DatePicker allows a string date input.
    /// </summary>
    public bool? AllowTextInput { get; set; }

    /// <summary>
    /// Whether the DatePicker closes automatically after selecting the date.
    /// It has no effect while the time picker is shown, where the callout stays open so the time of the
    /// selected day can be set as well.
    /// </summary>
    public bool? AutoClose { get; set; }

    /// <summary>
    /// Whether the input of the picker gets the focus as soon as it renders for the first time.
    /// </summary>
    /// <remarks>
    /// A standalone picker carries its value in a hidden input nobody is meant to land on, so it has nothing
    /// to place the focus on and the parameter does nothing there.
    /// </remarks>
    public bool? AutoFocus { get; set; }

    /// <summary>
    /// Aria label of the DatePicker's callout for screen readers.
    /// </summary>
    public string? CalloutAriaLabel { get; set; }

    /// <summary>
    /// Custom template to render at the bottom of the DatePicker's callout, below the pickers
    /// (e.g. preset buttons that set the value from the code).
    /// </summary>
    public RenderFragment? CalloutFooterTemplate { get; set; }

    /// <summary>
    /// Custom template to render at the top of the DatePicker's callout, above the pickers.
    /// </summary>
    public RenderFragment? CalloutHeaderTemplate { get; set; }

    /// <summary>
    /// Capture and render additional html attributes for the DatePicker's callout.
    /// </summary>
    public Dictionary<string, object>? CalloutHtmlAttributes { get; set; }

    /// <summary>
    /// Custom CSS classes for different parts of the BitDatePicker component.
    /// </summary>
    public BitDatePickerClassStyles? Classes { get; set; }

    /// <summary>
    /// The icon to display inside the clear button.
    /// Takes precedence over <see cref="ClearButtonIconName"/> when both are set.
    /// </summary>
    public BitIconInfo? ClearButtonIcon { get; set; }

    /// <summary>
    /// The name of the clear button's icon from the built-in Fluent UI icon set.
    /// </summary>
    public string? ClearButtonIconName { get; set; }

    /// <summary>
    /// The title (tooltip) and the accessible name of the clear button.
    /// </summary>
    public string? ClearButtonTitle { get; set; }

    /// <summary>
    /// The icon to display inside the close button.
    /// Takes precedence over <see cref="CloseButtonIconName"/> when both are set.
    /// </summary>
    public BitIconInfo? CloseButtonIcon { get; set; }

    /// <summary>
    /// The name of the close button's icon from the built-in Fluent UI icon set.
    /// </summary>
    public string? CloseButtonIconName { get; set; }

    /// <summary>
    /// The title of the close button (tooltip).
    /// </summary>
    public string? CloseButtonTitle { get; set; }

    /// <summary>
    /// The general color of the DatePicker that applies to the today day button, the highlighted current month,
    /// and the selected AM/PM button.
    /// </summary>
    public BitColor? Color { get; set; }

    /// <summary>
    /// The delay in milliseconds before the hour/minute of the time picker starts changing continuously while an
    /// increase/decrease button is held down.
    /// </summary>
    public int? ContinuousSpinDelay { get; set; }

    /// <summary>
    /// The interval in milliseconds between two consecutive changes while an increase/decrease
    /// button is held down.
    /// </summary>
    public int? ContinuousSpinInterval { get; set; }

    /// <summary>
    /// CultureInfo for the DatePicker.
    /// </summary>
    public CultureInfo? Culture { get; set; }

    /// <summary>
    /// The format of the date in the DatePicker.
    /// </summary>
    public string? DateFormat { get; set; }

    /// <summary>
    /// The accessible description of the input of a picker that accepts a typed date, which the pattern the
    /// date is read with is formatted into.
    /// </summary>
    public string? DateFormatAriaDescription { get; set; }

    /// <summary>
    /// Custom template to render the day cells of the DatePicker.
    /// </summary>
    public RenderFragment<DateTimeOffset>? DayCellTemplate { get; set; }

    /// <summary>
    /// The custom validation error message for a typed value that the DatePicker does not allow to be
    /// selected, through <see cref="DisabledDates"/>, <see cref="DisabledDaysOfWeek"/> or
    /// <see cref="IsDateDisabled"/>.
    /// </summary>
    public string? DisabledDateErrorMessage { get; set; }

    /// <summary>
    /// The list of dates that are disabled (not selectable) in the DatePicker, in addition to
    /// <see cref="MinDate"/> and <see cref="MaxDate"/>. Only the date part of each value is considered.
    /// </summary>
    public IEnumerable<DateTimeOffset>? DisabledDates { get; set; }

    /// <summary>
    /// The days of the week that are disabled (not selectable) in the DatePicker (e.g. weekends).
    /// </summary>
    public IEnumerable<DayOfWeek>? DisabledDaysOfWeek { get; set; }

    /// <summary>
    /// Disables all days after today, exactly as a <see cref="MaxDate"/> of today would.
    /// When both are set, the earlier of the two bounds wins.
    /// </summary>
    public bool? DisableFuture { get; set; }

    /// <summary>
    /// Disables all days before today, exactly as a <see cref="MinDate"/> of today would.
    /// When both are set, the later of the two bounds wins.
    /// </summary>
    public bool? DisablePast { get; set; }

    /// <summary>
    /// The custom validation error message for a date entered as text whose time of day the DatePicker does not
    /// allow to be picked, through <see cref="MinTime"/>, <see cref="MaxTime"/>, <see cref="AllowedHours"/>,
    /// <see cref="AllowedMinutes"/> or <see cref="AllowedSeconds"/>.
    /// </summary>
    public string? DisallowedTimeErrorMessage { get; set; }

    /// <summary>
    /// Determines the allowed drop directions of the callout.
    /// </summary>
    public BitDropDirection? DropDirection { get; set; }

    /// <summary>
    /// Overrides the first day of the week of the day picker. If not set, the first day of the week
    /// of the <see cref="Culture"/> is used.
    /// </summary>
    public DayOfWeek? FirstDayOfWeek { get; set; }

    /// <summary>
    /// Whether the day picker should always render six weeks, filling the extra rows with the days of the
    /// adjacent months, to keep the height of the calendar fixed while navigating between the months.
    /// </summary>
    public bool? FixedWeeks { get; set; }

    /// <summary>
    /// Custom function to provide additional CSS classes for each day button of the DatePicker.
    /// </summary>
    public Func<DateTimeOffset, string?>? GetDayClass { get; set; }

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
    /// The icon to display inside the GoToToday button.
    /// Takes precedence over <see cref="GoToTodayIconName"/> when both are set.
    /// </summary>
    public BitIconInfo? GoToTodayIcon { get; set; }

    /// <summary>
    /// The name of the GoToToday button's icon from the built-in Fluent UI icon set.
    /// </summary>
    public string? GoToTodayIconName { get; set; }

    /// <summary>
    /// The title of the GoToToday button (tooltip).
    /// </summary>
    public string? GoToTodayTitle { get; set; }

    /// <summary>
    /// Determines if the DatePicker has a border.
    /// </summary>
    public bool? HasBorder { get; set; }

    /// <summary>
    /// The icon to display inside the HideTimePicker button.
    /// Takes precedence over <see cref="HideTimePickerIconName"/> when both are set.
    /// </summary>
    public BitIconInfo? HideTimePickerIcon { get; set; }

    /// <summary>
    /// The name of the HideTimePicker button's icon from the built-in Fluent UI icon set.
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
    /// The accessible name of a day of <see cref="HighlightedDates"/>, which its full date is formatted into.
    /// </summary>
    public string? HighlightedDateAriaLabel { get; set; }

    /// <summary>
    /// Whether the month picker should highlight the selected month.
    /// </summary>
    public bool? HighlightSelectedMonth { get; set; }

    /// <summary>
    /// Whether the day picker should highlight today's day. It only affects the visual style of the
    /// day cell; the accessibility attributes still report the day as the current date.
    /// </summary>
    public bool? HighlightToday { get; set; }

    /// <summary>
    /// The step, in hours, the spin buttons of the time picker move the hour by.
    /// </summary>
    /// <remarks>
    /// A step greater than 1 lays a grid over the day that every hour the picker produces sits on, starting at
    /// midnight, so a picker that only accepts times on a three-hour grid can say so. The buttons, the keys and
    /// what is typed into the hour are all held to it. Values below 1 are treated as 1.
    /// </remarks>
    public int? HourStep { get; set; }

    /// <summary>
    /// The icon to display in the DatePicker input.
    /// Takes precedence over <see cref="IconName"/> when both are set.
    /// </summary>
    /// <remarks>
    /// Use this property to render icons from external libraries like FontAwesome, Material Icons, or Bootstrap Icons.
    /// For built-in Fluent UI icons, use <see cref="IconName"/> instead.
    /// </remarks>
    /// <example>
    /// Bootstrap: Icon="BitIconInfo.Bi("calendar3")"
    /// FontAwesome: Icon="BitIconInfo.Fa("solid calendar")"
    /// Custom CSS: Icon="BitIconInfo.Css("my-icon-class")"
    /// </example>
    public BitIconInfo? Icon { get; set; }

    /// <summary>
    /// Determines the location of the DatePicker's icon.
    /// </summary>
    public BitIconLocation? IconLocation { get; set; }

    /// <summary>
    /// The name of the DatePicker's icon from the built-in Fluent UI icon set.
    /// </summary>
    /// <remarks>
    /// The icon name should be from the Fluent UI icon set (e.g., <c>BitIconName.CalendarMirrored</c>).
    /// For external icon libraries, use <see cref="Icon"/> instead.
    /// </remarks>
    public string? IconName { get; set; }

    /// <summary>
    /// Custom template for the DatePicker's icon.
    /// </summary>
    public RenderFragment? IconTemplate { get; set; }

    /// <summary>
    /// The custom validation error message for the invalid value.
    /// </summary>
    public string? InvalidErrorMessage { get; set; }

    /// <summary>
    /// Custom function to determine if a specific date is disabled (not selectable) in the DatePicker.
    /// </summary>
    public Func<DateTimeOffset, bool>? IsDateDisabled { get; set; }

    /// <summary>
    /// Whether the month picker is shown next to the day picker or hidden.
    /// It has no effect in the MonthPicker mode, where the month picker is the only view.
    /// </summary>
    public bool? IsMonthPickerVisible { get; set; }

    /// <summary>
    /// The text of the DatePicker's label.
    /// </summary>
    public string? Label { get; set; }

    /// <summary>
    /// Custom template for the DatePicker's label.
    /// </summary>
    public RenderFragment? LabelTemplate { get; set; }

    /// <summary>
    /// The maximum date allowed for the DatePicker.
    /// </summary>
    /// <remarks>
    /// The days after it are ruled out as a whole, and the day it itself falls on stays selectable. Where a
    /// time picker is on screen, the time it carries bounds the hours of that day too, so the DatePicker
    /// cannot produce an instant past the bound.
    /// </remarks>
    public DateTimeOffset? MaxDate { get; set; }

    /// <summary>
    /// The latest time of day the time picker can be set to, on every day the DatePicker offers.
    /// </summary>
    public TimeSpan? MaxTime { get; set; }

    /// <summary>
    /// The minimum date allowed for the DatePicker.
    /// </summary>
    /// <inheritdoc cref="MaxDate" path="/remarks"/>
    public DateTimeOffset? MinDate { get; set; }

    /// <summary>
    /// The earliest time of day the time picker can be set to, on every day the DatePicker offers.
    /// </summary>
    public TimeSpan? MinTime { get; set; }

    /// <summary>
    /// The number of consecutive months rendered side by side in the day picker (1 to 3).
    /// </summary>
    /// <inheritdoc cref="BitDatePicker.MonthCount" path="/remarks"/>
    public int? MonthCount { get; set; }

    /// <summary>
    /// The step, in minutes, the spin buttons of the time picker move the minute by.
    /// </summary>
    /// <remarks>
    /// A step greater than 1 lays a grid over the hour that every minute the picker produces sits on, starting
    /// at the top of the hour, which is what turns it into a five-minute or quarter-hour picker. The buttons,
    /// the keys and what is typed into the minute are all held to it. Values below 1 are treated as 1.
    /// </remarks>
    public int? MinuteStep { get; set; }

    /// <summary>
    /// The selection mode of the DatePicker (DatePicker or MonthPicker).
    /// </summary>
    public BitDatePickerMode? Mode { get; set; }

    /// <summary>
    /// Custom template to render the month cells of the DatePicker.
    /// </summary>
    public RenderFragment<DateTimeOffset>? MonthCellTemplate { get; set; }

    /// <summary>
    /// The title of the month picker's toggle (tooltip).
    /// </summary>
    public string? MonthPickerToggleTitle { get; set; }

    /// <summary>
    /// The icon to display inside the next-month navigation button.
    /// Takes precedence over <see cref="NextMonthNavIconName"/> when both are set.
    /// </summary>
    public BitIconInfo? NextMonthNavIcon { get; set; }

    /// <summary>
    /// The name of the next-month navigation button's icon from the built-in Fluent UI icon set.
    /// </summary>
    public string? NextMonthNavIconName { get; set; }

    /// <summary>
    /// The icon to display inside the next-year navigation button.
    /// Takes precedence over <see cref="NextYearNavIconName"/> when both are set.
    /// </summary>
    public BitIconInfo? NextYearNavIcon { get; set; }

    /// <summary>
    /// The name of the next-year navigation button's icon from the built-in Fluent UI icon set.
    /// </summary>
    public string? NextYearNavIconName { get; set; }

    /// <summary>
    /// The icon to display inside the next-year-range navigation button.
    /// Takes precedence over <see cref="NextYearRangeNavIconName"/> when both are set.
    /// </summary>
    public BitIconInfo? NextYearRangeNavIcon { get; set; }

    /// <summary>
    /// The name of the next-year-range navigation button's icon from the built-in Fluent UI icon set.
    /// </summary>
    public string? NextYearRangeNavIconName { get; set; }

    /// <summary>
    /// The icon to display inside the now button.
    /// Takes precedence over <see cref="NowButtonIconName"/> when both are set.
    /// </summary>
    public BitIconInfo? NowButtonIcon { get; set; }

    /// <summary>
    /// The name of the now button's icon from the built-in Fluent UI icon set.
    /// </summary>
    public string? NowButtonIconName { get; set; }

    /// <summary>
    /// The title of the now button (tooltip).
    /// </summary>
    public string? NowButtonTitle { get; set; }

    /// <summary>
    /// The custom validation error message for a typed value that falls outside of the
    /// <see cref="MinDate"/> and <see cref="MaxDate"/> range.
    /// </summary>
    public string? OutOfRangeErrorMessage { get; set; }

    /// <summary>
    /// Whether the previous and next navigation buttons move the day picker by all of its rendered
    /// months instead of one.
    /// </summary>
    public bool? PagedNavigation { get; set; }

    /// <summary>
    /// The placeholder text of the DatePicker's input.
    /// </summary>
    public string? Placeholder { get; set; }

    /// <summary>
    /// The icon to display inside the previous-month navigation button.
    /// Takes precedence over <see cref="PrevMonthNavIconName"/> when both are set.
    /// </summary>
    public BitIconInfo? PrevMonthNavIcon { get; set; }

    /// <summary>
    /// The name of the previous-month navigation button's icon from the built-in Fluent UI icon set.
    /// </summary>
    public string? PrevMonthNavIconName { get; set; }

    /// <summary>
    /// The icon to display inside the previous-year navigation button.
    /// Takes precedence over <see cref="PrevYearNavIconName"/> when both are set.
    /// </summary>
    public BitIconInfo? PrevYearNavIcon { get; set; }

    /// <summary>
    /// The name of the previous-year navigation button's icon from the built-in Fluent UI icon set.
    /// </summary>
    public string? PrevYearNavIconName { get; set; }

    /// <summary>
    /// The icon to display inside the previous-year-range navigation button.
    /// Takes precedence over <see cref="PrevYearRangeNavIconName"/> when both are set.
    /// </summary>
    public BitIconInfo? PrevYearRangeNavIcon { get; set; }

    /// <summary>
    /// The name of the previous-year-range navigation button's icon from the built-in Fluent UI icon set.
    /// </summary>
    public string? PrevYearRangeNavIconName { get; set; }

    /// <summary>
    /// Enables the responsive mode in small screens.
    /// </summary>
    public bool? Responsive { get; set; }

    /// <summary>
    /// The step, in seconds, the spin buttons of the time picker move the second by.
    /// </summary>
    public int? SecondStep { get; set; }

    /// <summary>
    /// The text of selected date aria-atomic of the DatePicker.
    /// </summary>
    public string? SelectedDateAriaAtomic { get; set; }

    /// <summary>
    /// Whether the clear button should be shown or not when the BitDatePicker has a value.
    /// </summary>
    public bool? ShowClearButton { get; set; }

    /// <summary>
    /// Whether the DatePicker's close button should be shown or not.
    /// </summary>
    public bool? ShowCloseButton { get; set; }

    /// <summary>
    /// Whether the GoToToday button should be shown or not.
    /// </summary>
    public bool? ShowGoToToday { get; set; }

    /// <summary>
    /// Show month picker on top of date picker when visible.
    /// </summary>
    public bool? ShowMonthPickerAsOverlay { get; set; }

    /// <summary>
    /// Whether the now button should be shown or not.
    /// </summary>
    public bool? ShowNowButton { get; set; }

    /// <summary>
    /// Whether the days of the previous and next months should be shown in the day picker.
    /// </summary>
    public bool? ShowOutsideDays { get; set; }

    /// <summary>
    /// Whether the time picker shows a seconds field beside the hour and the minute, which adds the second to
    /// the value and to the default date format.
    /// </summary>
    public bool? ShowSeconds { get; set; }

    /// <summary>
    /// Whether or not render the time-picker.
    /// </summary>
    public bool? ShowTimePicker { get; set; }

    /// <summary>
    /// Show the time picker as an overlay on top of the date picker when visible.
    /// </summary>
    public bool? ShowTimePickerAsOverlay { get; set; }

    /// <summary>
    /// The icon to display inside the ShowTimePicker button.
    /// Takes precedence over <see cref="ShowTimePickerIconName"/> when both are set.
    /// </summary>
    public BitIconInfo? ShowTimePickerIcon { get; set; }

    /// <summary>
    /// The name of the ShowTimePicker button's icon from the built-in Fluent UI icon set.
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
    /// The size of the DatePicker.
    /// </summary>
    public BitSize? Size { get; set; }

    /// <summary>
    /// Whether the date-picker is rendered standalone or with the input component and callout.
    /// </summary>
    public bool? Standalone { get; set; }

    /// <summary>
    /// Specifies the date and time of the date-picker when it is opened without any selected value.
    /// </summary>
    public DateTimeOffset? StartingValue { get; set; }

    /// <summary>
    /// Custom CSS styles for different parts of the BitDatePicker component.
    /// </summary>
    public BitDatePickerClassStyles? Styles { get; set; }

    /// <summary>
    /// The time format of the time-picker, 24H or 12H.
    /// </summary>
    public BitTimeFormat? TimeFormat { get; set; }

    /// <summary>
    /// The icon to display inside the time-picker's decrease-hour button.
    /// Takes precedence over <see cref="TimePickerDecreaseHourIconName"/> when both are set.
    /// </summary>
    public BitIconInfo? TimePickerDecreaseHourIcon { get; set; }

    /// <summary>
    /// The name of the time-picker's decrease-hour button icon from the built-in Fluent UI icon set.
    /// </summary>
    public string? TimePickerDecreaseHourIconName { get; set; }

    /// <summary>
    /// The title (tooltip) and the accessible name of the time-picker's decrease-hour button.
    /// </summary>
    public string? TimePickerDecreaseHourTitle { get; set; }

    /// <summary>
    /// The icon to display inside the time-picker's decrease-minute button.
    /// Takes precedence over <see cref="TimePickerDecreaseMinuteIconName"/> when both are set.
    /// </summary>
    public BitIconInfo? TimePickerDecreaseMinuteIcon { get; set; }

    /// <summary>
    /// The name of the time-picker's decrease-minute button icon from the built-in Fluent UI icon set.
    /// </summary>
    public string? TimePickerDecreaseMinuteIconName { get; set; }

    /// <summary>
    /// The title (tooltip) and the accessible name of the time-picker's decrease-minute button.
    /// </summary>
    public string? TimePickerDecreaseMinuteTitle { get; set; }

    /// <summary>
    /// The icon to display inside the time-picker's decrease-second button.
    /// Takes precedence over <see cref="TimePickerDecreaseSecondIconName"/> when both are set.
    /// </summary>
    public BitIconInfo? TimePickerDecreaseSecondIcon { get; set; }

    /// <summary>
    /// The name of the time-picker's decrease-second button icon from the built-in Fluent UI icon set.
    /// </summary>
    public string? TimePickerDecreaseSecondIconName { get; set; }

    /// <summary>
    /// The title (tooltip) and the accessible name of the time-picker's decrease-second button.
    /// </summary>
    public string? TimePickerDecreaseSecondTitle { get; set; }

    /// <summary>
    /// The title (tooltip) and the accessible name of the time-picker's hour input.
    /// </summary>
    public string? TimePickerHourTitle { get; set; }

    /// <summary>
    /// The icon to display inside the time-picker's increase-hour button.
    /// Takes precedence over <see cref="TimePickerIncreaseHourIconName"/> when both are set.
    /// </summary>
    public BitIconInfo? TimePickerIncreaseHourIcon { get; set; }

    /// <summary>
    /// The name of the time-picker's increase-hour button icon from the built-in Fluent UI icon set.
    /// </summary>
    public string? TimePickerIncreaseHourIconName { get; set; }

    /// <summary>
    /// The title (tooltip) and the accessible name of the time-picker's increase-hour button.
    /// </summary>
    public string? TimePickerIncreaseHourTitle { get; set; }

    /// <summary>
    /// The icon to display inside the time-picker's increase-minute button.
    /// Takes precedence over <see cref="TimePickerIncreaseMinuteIconName"/> when both are set.
    /// </summary>
    public BitIconInfo? TimePickerIncreaseMinuteIcon { get; set; }

    /// <summary>
    /// The name of the time-picker's increase-minute button icon from the built-in Fluent UI icon set.
    /// </summary>
    public string? TimePickerIncreaseMinuteIconName { get; set; }

    /// <summary>
    /// The title (tooltip) and the accessible name of the time-picker's increase-minute button.
    /// </summary>
    public string? TimePickerIncreaseMinuteTitle { get; set; }

    /// <summary>
    /// The icon to display inside the time-picker's increase-second button.
    /// Takes precedence over <see cref="TimePickerIncreaseSecondIconName"/> when both are set.
    /// </summary>
    public BitIconInfo? TimePickerIncreaseSecondIcon { get; set; }

    /// <summary>
    /// The name of the time-picker's increase-second button icon from the built-in Fluent UI icon set.
    /// </summary>
    public string? TimePickerIncreaseSecondIconName { get; set; }

    /// <summary>
    /// The title (tooltip) and the accessible name of the time-picker's increase-second button.
    /// </summary>
    public string? TimePickerIncreaseSecondTitle { get; set; }

    /// <summary>
    /// The title (tooltip) and the accessible name of the time-picker's minute input.
    /// </summary>
    public string? TimePickerMinuteTitle { get; set; }

    /// <summary>
    /// The title (tooltip) and the accessible name of the time-picker's second input.
    /// </summary>
    public string? TimePickerSecondTitle { get; set; }

    /// <summary>
    /// TimeZone for the DatePicker.
    /// </summary>
    public TimeZoneInfo? TimeZone { get; set; }

    /// <summary>
    /// Overrides the current date and time considered as "today" and "now" in the DatePicker
    /// (useful for testing or custom time providers).
    /// </summary>
    public DateTimeOffset? Today { get; set; }

    /// <summary>
    /// Whether or not the text field of the DatePicker is underlined.
    /// </summary>
    public bool? Underlined { get; set; }

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
    /// Custom template to render the year cells of the DatePicker.
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
    /// Updates the properties of the specified <see cref="BitDatePicker"/> instance with any values that have been set on
    /// this object, if those properties have not already been set on the <see cref="BitDatePicker"/>.
    /// </summary>
    /// <remarks>
    /// Only properties that have a value set and have not already been set on the <paramref name="bitDatePicker"/> will be updated.
    /// This method does not overwrite existing values on <paramref name="bitDatePicker"/>.
    /// </remarks>
    /// <param name="bitDatePicker">
    /// The <see cref="BitDatePicker"/> instance whose properties will be updated. Cannot be null.
    /// </param>
    public void UpdateParameters(BitDatePicker bitDatePicker)
    {
        if (bitDatePicker is null) return;

        UpdateBaseParameters(bitDatePicker);

        // The parameters the picker rebuilds its view from are the ones carrying a [CallOnSet(OnSetParameters)] on
        // the component, and the component has already run that pass in OnInitialized - before anything cascaded
        // here reached it. So whichever of them the cascade CHANGES, the pass is run once more at the end. Only a
        // change: this runs on every render of the ancestor holding the BitParams, and cascading the same value a
        // second time must not fling the calendar back off the month the user navigated it to.
        var rebuildView = false;

        if (AllowDeselect.HasValue && bitDatePicker.HasNotBeenSet(nameof(AllowDeselect)))
        {
            bitDatePicker.AllowDeselect = AllowDeselect.Value;
        }

        if (AllowedHours is not null && bitDatePicker.HasNotBeenSet(nameof(AllowedHours)))
        {
            bitDatePicker.AllowedHours = AllowedHours;
        }

        if (AllowedMinutes is not null && bitDatePicker.HasNotBeenSet(nameof(AllowedMinutes)))
        {
            bitDatePicker.AllowedMinutes = AllowedMinutes;
        }

        if (AllowedSeconds is not null && bitDatePicker.HasNotBeenSet(nameof(AllowedSeconds)))
        {
            bitDatePicker.AllowedSeconds = AllowedSeconds;
        }

        if (AllowTextInput.HasValue && bitDatePicker.HasNotBeenSet(nameof(AllowTextInput)))
        {
            bitDatePicker.AllowTextInput = AllowTextInput.Value;
        }

        if (AutoClose.HasValue && bitDatePicker.HasNotBeenSet(nameof(AutoClose)))
        {
            bitDatePicker.AutoClose = AutoClose.Value;
        }

        if (AutoFocus.HasValue && bitDatePicker.HasNotBeenSet(nameof(AutoFocus)))
        {
            bitDatePicker.AutoFocus = AutoFocus.Value;
        }

        if (CalloutAriaLabel.HasValue() && bitDatePicker.HasNotBeenSet(nameof(CalloutAriaLabel)))
        {
            bitDatePicker.CalloutAriaLabel = CalloutAriaLabel!;
        }

        if (CalloutFooterTemplate is not null && bitDatePicker.HasNotBeenSet(nameof(CalloutFooterTemplate)))
        {
            bitDatePicker.CalloutFooterTemplate = CalloutFooterTemplate;
        }

        if (CalloutHeaderTemplate is not null && bitDatePicker.HasNotBeenSet(nameof(CalloutHeaderTemplate)))
        {
            bitDatePicker.CalloutHeaderTemplate = CalloutHeaderTemplate;
        }

        if (CalloutHtmlAttributes is not null && bitDatePicker.HasNotBeenSet(nameof(CalloutHtmlAttributes)))
        {
            bitDatePicker.CalloutHtmlAttributes = CalloutHtmlAttributes;
        }

        if (Classes is not null && bitDatePicker.HasNotBeenSet(nameof(Classes)))
        {
            bitDatePicker.Classes = Classes;

            bitDatePicker.ClassBuilder.Reset();
        }

        if (ClearButtonIcon is not null && bitDatePicker.HasNotBeenSet(nameof(ClearButtonIcon)))
        {
            bitDatePicker.ClearButtonIcon = ClearButtonIcon;
        }

        if (ClearButtonIconName.HasValue() && bitDatePicker.HasNotBeenSet(nameof(ClearButtonIconName)))
        {
            bitDatePicker.ClearButtonIconName = ClearButtonIconName;
        }

        if (ClearButtonTitle.HasValue() && bitDatePicker.HasNotBeenSet(nameof(ClearButtonTitle)))
        {
            bitDatePicker.ClearButtonTitle = ClearButtonTitle!;
        }

        if (CloseButtonIcon is not null && bitDatePicker.HasNotBeenSet(nameof(CloseButtonIcon)))
        {
            bitDatePicker.CloseButtonIcon = CloseButtonIcon;
        }

        if (CloseButtonIconName.HasValue() && bitDatePicker.HasNotBeenSet(nameof(CloseButtonIconName)))
        {
            bitDatePicker.CloseButtonIconName = CloseButtonIconName;
        }

        if (CloseButtonTitle.HasValue() && bitDatePicker.HasNotBeenSet(nameof(CloseButtonTitle)))
        {
            bitDatePicker.CloseButtonTitle = CloseButtonTitle!;
        }

        if (Color.HasValue && bitDatePicker.HasNotBeenSet(nameof(Color)))
        {
            bitDatePicker.Color = Color.Value;

            bitDatePicker.ClassBuilder.Reset();
        }

        if (ContinuousSpinDelay.HasValue && bitDatePicker.HasNotBeenSet(nameof(ContinuousSpinDelay)))
        {
            bitDatePicker.ContinuousSpinDelay = ContinuousSpinDelay.Value;
        }

        if (ContinuousSpinInterval.HasValue && bitDatePicker.HasNotBeenSet(nameof(ContinuousSpinInterval)))
        {
            bitDatePicker.ContinuousSpinInterval = ContinuousSpinInterval.Value;
        }

        if (Culture is not null && bitDatePicker.HasNotBeenSet(nameof(Culture)))
        {
            rebuildView |= Equals(bitDatePicker.Culture, Culture) is false;

            bitDatePicker.Culture = Culture;

            bitDatePicker.ClassBuilder.Reset();
        }

        if (DateFormat.HasValue() && bitDatePicker.HasNotBeenSet(nameof(DateFormat)))
        {
            bitDatePicker.DateFormat = DateFormat;
        }

        if (DateFormatAriaDescription is not null && bitDatePicker.HasNotBeenSet(nameof(DateFormatAriaDescription)))
        {
            bitDatePicker.DateFormatAriaDescription = DateFormatAriaDescription;
        }

        if (DayCellTemplate is not null && bitDatePicker.HasNotBeenSet(nameof(DayCellTemplate)))
        {
            bitDatePicker.DayCellTemplate = DayCellTemplate;
        }

        if (DisabledDateErrorMessage.HasValue() && bitDatePicker.HasNotBeenSet(nameof(DisabledDateErrorMessage)))
        {
            bitDatePicker.DisabledDateErrorMessage = DisabledDateErrorMessage;
        }

        if (DisabledDates is not null && bitDatePicker.HasNotBeenSet(nameof(DisabledDates)))
        {
            bitDatePicker.DisabledDates = DisabledDates;
        }

        if (DisabledDaysOfWeek is not null && bitDatePicker.HasNotBeenSet(nameof(DisabledDaysOfWeek)))
        {
            bitDatePicker.DisabledDaysOfWeek = DisabledDaysOfWeek;
        }

        if (DisableFuture.HasValue && bitDatePicker.HasNotBeenSet(nameof(DisableFuture)))
        {
            rebuildView |= bitDatePicker.DisableFuture != DisableFuture.Value;

            bitDatePicker.DisableFuture = DisableFuture.Value;
        }

        if (DisablePast.HasValue && bitDatePicker.HasNotBeenSet(nameof(DisablePast)))
        {
            rebuildView |= bitDatePicker.DisablePast != DisablePast.Value;

            bitDatePicker.DisablePast = DisablePast.Value;
        }

        if (DisallowedTimeErrorMessage.HasValue() && bitDatePicker.HasNotBeenSet(nameof(DisallowedTimeErrorMessage)))
        {
            bitDatePicker.DisallowedTimeErrorMessage = DisallowedTimeErrorMessage;
        }

        if (DropDirection.HasValue && bitDatePicker.HasNotBeenSet(nameof(DropDirection)))
        {
            bitDatePicker.DropDirection = DropDirection.Value;
        }

        if (FirstDayOfWeek.HasValue && bitDatePicker.HasNotBeenSet(nameof(FirstDayOfWeek)))
        {
            rebuildView |= bitDatePicker.FirstDayOfWeek != FirstDayOfWeek.Value;

            bitDatePicker.FirstDayOfWeek = FirstDayOfWeek.Value;
        }

        if (FixedWeeks.HasValue && bitDatePicker.HasNotBeenSet(nameof(FixedWeeks)))
        {
            rebuildView |= bitDatePicker.FixedWeeks != FixedWeeks.Value;

            bitDatePicker.FixedWeeks = FixedWeeks.Value;
        }

        if (GetDayClass is not null && bitDatePicker.HasNotBeenSet(nameof(GetDayClass)))
        {
            bitDatePicker.GetDayClass = GetDayClass;
        }

        if (GoToNextMonthTitle.HasValue() && bitDatePicker.HasNotBeenSet(nameof(GoToNextMonthTitle)))
        {
            bitDatePicker.GoToNextMonthTitle = GoToNextMonthTitle!;
        }

        if (GoToNextYearRangeTitle.HasValue() && bitDatePicker.HasNotBeenSet(nameof(GoToNextYearRangeTitle)))
        {
            bitDatePicker.GoToNextYearRangeTitle = GoToNextYearRangeTitle!;
        }

        if (GoToNextYearTitle.HasValue() && bitDatePicker.HasNotBeenSet(nameof(GoToNextYearTitle)))
        {
            bitDatePicker.GoToNextYearTitle = GoToNextYearTitle!;
        }

        if (GoToPrevMonthTitle.HasValue() && bitDatePicker.HasNotBeenSet(nameof(GoToPrevMonthTitle)))
        {
            bitDatePicker.GoToPrevMonthTitle = GoToPrevMonthTitle!;
        }

        if (GoToPrevYearRangeTitle.HasValue() && bitDatePicker.HasNotBeenSet(nameof(GoToPrevYearRangeTitle)))
        {
            bitDatePicker.GoToPrevYearRangeTitle = GoToPrevYearRangeTitle!;
        }

        if (GoToPrevYearTitle.HasValue() && bitDatePicker.HasNotBeenSet(nameof(GoToPrevYearTitle)))
        {
            bitDatePicker.GoToPrevYearTitle = GoToPrevYearTitle!;
        }

        if (GoToTodayIcon is not null && bitDatePicker.HasNotBeenSet(nameof(GoToTodayIcon)))
        {
            bitDatePicker.GoToTodayIcon = GoToTodayIcon;
        }

        if (GoToTodayIconName.HasValue() && bitDatePicker.HasNotBeenSet(nameof(GoToTodayIconName)))
        {
            bitDatePicker.GoToTodayIconName = GoToTodayIconName;
        }

        if (GoToTodayTitle.HasValue() && bitDatePicker.HasNotBeenSet(nameof(GoToTodayTitle)))
        {
            bitDatePicker.GoToTodayTitle = GoToTodayTitle!;
        }

        if (HasBorder.HasValue && bitDatePicker.HasNotBeenSet(nameof(HasBorder)))
        {
            bitDatePicker.HasBorder = HasBorder.Value;

            bitDatePicker.ClassBuilder.Reset();
        }

        if (HideTimePickerIcon is not null && bitDatePicker.HasNotBeenSet(nameof(HideTimePickerIcon)))
        {
            bitDatePicker.HideTimePickerIcon = HideTimePickerIcon;
        }

        if (HideTimePickerIconName.HasValue() && bitDatePicker.HasNotBeenSet(nameof(HideTimePickerIconName)))
        {
            bitDatePicker.HideTimePickerIconName = HideTimePickerIconName;
        }

        if (HideTimePickerTitle.HasValue() && bitDatePicker.HasNotBeenSet(nameof(HideTimePickerTitle)))
        {
            bitDatePicker.HideTimePickerTitle = HideTimePickerTitle!;
        }

        if (HighlightCurrentMonth.HasValue && bitDatePicker.HasNotBeenSet(nameof(HighlightCurrentMonth)))
        {
            bitDatePicker.HighlightCurrentMonth = HighlightCurrentMonth.Value;
        }

        if (HighlightedDates is not null && bitDatePicker.HasNotBeenSet(nameof(HighlightedDates)))
        {
            bitDatePicker.HighlightedDates = HighlightedDates;
        }

        if (HighlightedDateAriaLabel is not null && bitDatePicker.HasNotBeenSet(nameof(HighlightedDateAriaLabel)))
        {
            bitDatePicker.HighlightedDateAriaLabel = HighlightedDateAriaLabel;
        }

        if (HighlightSelectedMonth.HasValue && bitDatePicker.HasNotBeenSet(nameof(HighlightSelectedMonth)))
        {
            bitDatePicker.HighlightSelectedMonth = HighlightSelectedMonth.Value;
        }

        if (HighlightToday.HasValue && bitDatePicker.HasNotBeenSet(nameof(HighlightToday)))
        {
            bitDatePicker.HighlightToday = HighlightToday.Value;
        }

        if (HourStep.HasValue && bitDatePicker.HasNotBeenSet(nameof(HourStep)))
        {
            bitDatePicker.HourStep = HourStep.Value;
        }

        if (Icon is not null && bitDatePicker.HasNotBeenSet(nameof(Icon)))
        {
            bitDatePicker.Icon = Icon;
        }

        if (IconLocation.HasValue && bitDatePicker.HasNotBeenSet(nameof(IconLocation)))
        {
            bitDatePicker.IconLocation = IconLocation.Value;

            bitDatePicker.ClassBuilder.Reset();
        }

        if (IconName.HasValue() && bitDatePicker.HasNotBeenSet(nameof(IconName)))
        {
            bitDatePicker.IconName = IconName;
        }

        if (IconTemplate is not null && bitDatePicker.HasNotBeenSet(nameof(IconTemplate)))
        {
            bitDatePicker.IconTemplate = IconTemplate;
        }

        if (InvalidErrorMessage.HasValue() && bitDatePicker.HasNotBeenSet(nameof(InvalidErrorMessage)))
        {
            bitDatePicker.InvalidErrorMessage = InvalidErrorMessage;
        }

        if (IsDateDisabled is not null && bitDatePicker.HasNotBeenSet(nameof(IsDateDisabled)))
        {
            bitDatePicker.IsDateDisabled = IsDateDisabled;
        }

        if (IsMonthPickerVisible.HasValue && bitDatePicker.HasNotBeenSet(nameof(IsMonthPickerVisible)))
        {
            rebuildView |= bitDatePicker.IsMonthPickerVisible != IsMonthPickerVisible.Value;

            bitDatePicker.IsMonthPickerVisible = IsMonthPickerVisible.Value;
        }

        if (Label.HasValue() && bitDatePicker.HasNotBeenSet(nameof(Label)))
        {
            bitDatePicker.Label = Label;
        }

        if (LabelTemplate is not null && bitDatePicker.HasNotBeenSet(nameof(LabelTemplate)))
        {
            bitDatePicker.LabelTemplate = LabelTemplate;
        }

        if (MaxDate.HasValue && bitDatePicker.HasNotBeenSet(nameof(MaxDate)))
        {
            rebuildView |= bitDatePicker.MaxDate != MaxDate.Value;

            bitDatePicker.MaxDate = MaxDate.Value;
        }

        if (MaxTime.HasValue && bitDatePicker.HasNotBeenSet(nameof(MaxTime)))
        {
            bitDatePicker.MaxTime = MaxTime.Value;
        }

        if (MinDate.HasValue && bitDatePicker.HasNotBeenSet(nameof(MinDate)))
        {
            rebuildView |= bitDatePicker.MinDate != MinDate.Value;

            bitDatePicker.MinDate = MinDate.Value;
        }

        if (MinTime.HasValue && bitDatePicker.HasNotBeenSet(nameof(MinTime)))
        {
            bitDatePicker.MinTime = MinTime.Value;
        }

        if (MonthCount.HasValue && bitDatePicker.HasNotBeenSet(nameof(MonthCount)))
        {
            rebuildView |= bitDatePicker.MonthCount != MonthCount.Value;

            bitDatePicker.MonthCount = MonthCount.Value;
        }

        if (PagedNavigation.HasValue && bitDatePicker.HasNotBeenSet(nameof(PagedNavigation)))
        {
            bitDatePicker.PagedNavigation = PagedNavigation.Value;
        }

        if (MinuteStep.HasValue && bitDatePicker.HasNotBeenSet(nameof(MinuteStep)))
        {
            bitDatePicker.MinuteStep = MinuteStep.Value;
        }

        if (Mode.HasValue && bitDatePicker.HasNotBeenSet(nameof(Mode)))
        {
            rebuildView |= bitDatePicker.Mode != Mode.Value;

            bitDatePicker.Mode = Mode.Value;
        }

        if (MonthCellTemplate is not null && bitDatePicker.HasNotBeenSet(nameof(MonthCellTemplate)))
        {
            bitDatePicker.MonthCellTemplate = MonthCellTemplate;
        }

        if (MonthPickerToggleTitle.HasValue() && bitDatePicker.HasNotBeenSet(nameof(MonthPickerToggleTitle)))
        {
            bitDatePicker.MonthPickerToggleTitle = MonthPickerToggleTitle!;
        }

        if (NextMonthNavIcon is not null && bitDatePicker.HasNotBeenSet(nameof(NextMonthNavIcon)))
        {
            bitDatePicker.NextMonthNavIcon = NextMonthNavIcon;
        }

        if (NextMonthNavIconName.HasValue() && bitDatePicker.HasNotBeenSet(nameof(NextMonthNavIconName)))
        {
            bitDatePicker.NextMonthNavIconName = NextMonthNavIconName;
        }

        if (NextYearNavIcon is not null && bitDatePicker.HasNotBeenSet(nameof(NextYearNavIcon)))
        {
            bitDatePicker.NextYearNavIcon = NextYearNavIcon;
        }

        if (NextYearNavIconName.HasValue() && bitDatePicker.HasNotBeenSet(nameof(NextYearNavIconName)))
        {
            bitDatePicker.NextYearNavIconName = NextYearNavIconName;
        }

        if (NextYearRangeNavIcon is not null && bitDatePicker.HasNotBeenSet(nameof(NextYearRangeNavIcon)))
        {
            bitDatePicker.NextYearRangeNavIcon = NextYearRangeNavIcon;
        }

        if (NextYearRangeNavIconName.HasValue() && bitDatePicker.HasNotBeenSet(nameof(NextYearRangeNavIconName)))
        {
            bitDatePicker.NextYearRangeNavIconName = NextYearRangeNavIconName;
        }

        if (NowButtonIcon is not null && bitDatePicker.HasNotBeenSet(nameof(NowButtonIcon)))
        {
            bitDatePicker.NowButtonIcon = NowButtonIcon;
        }

        if (NowButtonIconName.HasValue() && bitDatePicker.HasNotBeenSet(nameof(NowButtonIconName)))
        {
            bitDatePicker.NowButtonIconName = NowButtonIconName;
        }

        if (NowButtonTitle.HasValue() && bitDatePicker.HasNotBeenSet(nameof(NowButtonTitle)))
        {
            bitDatePicker.NowButtonTitle = NowButtonTitle!;
        }

        if (OutOfRangeErrorMessage.HasValue() && bitDatePicker.HasNotBeenSet(nameof(OutOfRangeErrorMessage)))
        {
            bitDatePicker.OutOfRangeErrorMessage = OutOfRangeErrorMessage;
        }

        if (Placeholder.HasValue() && bitDatePicker.HasNotBeenSet(nameof(Placeholder)))
        {
            bitDatePicker.Placeholder = Placeholder!;
        }

        if (PrevMonthNavIcon is not null && bitDatePicker.HasNotBeenSet(nameof(PrevMonthNavIcon)))
        {
            bitDatePicker.PrevMonthNavIcon = PrevMonthNavIcon;
        }

        if (PrevMonthNavIconName.HasValue() && bitDatePicker.HasNotBeenSet(nameof(PrevMonthNavIconName)))
        {
            bitDatePicker.PrevMonthNavIconName = PrevMonthNavIconName;
        }

        if (PrevYearNavIcon is not null && bitDatePicker.HasNotBeenSet(nameof(PrevYearNavIcon)))
        {
            bitDatePicker.PrevYearNavIcon = PrevYearNavIcon;
        }

        if (PrevYearNavIconName.HasValue() && bitDatePicker.HasNotBeenSet(nameof(PrevYearNavIconName)))
        {
            bitDatePicker.PrevYearNavIconName = PrevYearNavIconName;
        }

        if (PrevYearRangeNavIcon is not null && bitDatePicker.HasNotBeenSet(nameof(PrevYearRangeNavIcon)))
        {
            bitDatePicker.PrevYearRangeNavIcon = PrevYearRangeNavIcon;
        }

        if (PrevYearRangeNavIconName.HasValue() && bitDatePicker.HasNotBeenSet(nameof(PrevYearRangeNavIconName)))
        {
            bitDatePicker.PrevYearRangeNavIconName = PrevYearRangeNavIconName;
        }

        if (Responsive.HasValue && bitDatePicker.HasNotBeenSet(nameof(Responsive)))
        {
            bitDatePicker.Responsive = Responsive.Value;
        }

        if (SecondStep.HasValue && bitDatePicker.HasNotBeenSet(nameof(SecondStep)))
        {
            bitDatePicker.SecondStep = SecondStep.Value;
        }

        if (SelectedDateAriaAtomic.HasValue() && bitDatePicker.HasNotBeenSet(nameof(SelectedDateAriaAtomic)))
        {
            bitDatePicker.SelectedDateAriaAtomic = SelectedDateAriaAtomic!;
        }

        if (ShowClearButton.HasValue && bitDatePicker.HasNotBeenSet(nameof(ShowClearButton)))
        {
            bitDatePicker.ShowClearButton = ShowClearButton.Value;
        }

        if (ShowCloseButton.HasValue && bitDatePicker.HasNotBeenSet(nameof(ShowCloseButton)))
        {
            bitDatePicker.ShowCloseButton = ShowCloseButton.Value;
        }

        if (ShowGoToToday.HasValue && bitDatePicker.HasNotBeenSet(nameof(ShowGoToToday)))
        {
            bitDatePicker.ShowGoToToday = ShowGoToToday.Value;
        }

        if (ShowMonthPickerAsOverlay.HasValue && bitDatePicker.HasNotBeenSet(nameof(ShowMonthPickerAsOverlay)))
        {
            rebuildView |= bitDatePicker.ShowMonthPickerAsOverlay != ShowMonthPickerAsOverlay.Value;

            bitDatePicker.ShowMonthPickerAsOverlay = ShowMonthPickerAsOverlay.Value;
        }

        if (ShowNowButton.HasValue && bitDatePicker.HasNotBeenSet(nameof(ShowNowButton)))
        {
            bitDatePicker.ShowNowButton = ShowNowButton.Value;
        }

        if (ShowOutsideDays.HasValue && bitDatePicker.HasNotBeenSet(nameof(ShowOutsideDays)))
        {
            bitDatePicker.ShowOutsideDays = ShowOutsideDays.Value;
        }

        if (ShowSeconds.HasValue && bitDatePicker.HasNotBeenSet(nameof(ShowSeconds)))
        {
            rebuildView |= bitDatePicker.ShowSeconds != ShowSeconds.Value;

            bitDatePicker.ShowSeconds = ShowSeconds.Value;
        }

        if (ShowTimePicker.HasValue && bitDatePicker.HasNotBeenSet(nameof(ShowTimePicker)))
        {
            rebuildView |= bitDatePicker.ShowTimePicker != ShowTimePicker.Value;

            bitDatePicker.ShowTimePicker = ShowTimePicker.Value;
        }

        if (ShowTimePickerAsOverlay.HasValue && bitDatePicker.HasNotBeenSet(nameof(ShowTimePickerAsOverlay)))
        {
            rebuildView |= bitDatePicker.ShowTimePickerAsOverlay != ShowTimePickerAsOverlay.Value;

            bitDatePicker.ShowTimePickerAsOverlay = ShowTimePickerAsOverlay.Value;
        }

        if (ShowTimePickerIcon is not null && bitDatePicker.HasNotBeenSet(nameof(ShowTimePickerIcon)))
        {
            bitDatePicker.ShowTimePickerIcon = ShowTimePickerIcon;
        }

        if (ShowTimePickerIconName.HasValue() && bitDatePicker.HasNotBeenSet(nameof(ShowTimePickerIconName)))
        {
            bitDatePicker.ShowTimePickerIconName = ShowTimePickerIconName;
        }

        if (ShowTimePickerTitle.HasValue() && bitDatePicker.HasNotBeenSet(nameof(ShowTimePickerTitle)))
        {
            bitDatePicker.ShowTimePickerTitle = ShowTimePickerTitle!;
        }

        if (ShowWeekNumbers.HasValue && bitDatePicker.HasNotBeenSet(nameof(ShowWeekNumbers)))
        {
            bitDatePicker.ShowWeekNumbers = ShowWeekNumbers.Value;
        }

        if (Size.HasValue && bitDatePicker.HasNotBeenSet(nameof(Size)))
        {
            bitDatePicker.Size = Size.Value;

            bitDatePicker.ClassBuilder.Reset();
        }

        if (Standalone.HasValue && bitDatePicker.HasNotBeenSet(nameof(Standalone)))
        {
            rebuildView |= bitDatePicker.Standalone != Standalone.Value;

            bitDatePicker.Standalone = Standalone.Value;

            bitDatePicker.ClassBuilder.Reset();
        }

        if (StartingValue.HasValue && bitDatePicker.HasNotBeenSet(nameof(StartingValue)))
        {
            rebuildView |= bitDatePicker.StartingValue != StartingValue.Value;

            bitDatePicker.StartingValue = StartingValue.Value;
        }

        if (Styles is not null && bitDatePicker.HasNotBeenSet(nameof(Styles)))
        {
            bitDatePicker.Styles = Styles;

            bitDatePicker.StyleBuilder.Reset();
        }

        if (TimeFormat.HasValue && bitDatePicker.HasNotBeenSet(nameof(TimeFormat)))
        {
            bitDatePicker.TimeFormat = TimeFormat.Value;
        }

        if (TimePickerDecreaseHourIcon is not null && bitDatePicker.HasNotBeenSet(nameof(TimePickerDecreaseHourIcon)))
        {
            bitDatePicker.TimePickerDecreaseHourIcon = TimePickerDecreaseHourIcon;
        }

        if (TimePickerDecreaseHourIconName.HasValue() && bitDatePicker.HasNotBeenSet(nameof(TimePickerDecreaseHourIconName)))
        {
            bitDatePicker.TimePickerDecreaseHourIconName = TimePickerDecreaseHourIconName;
        }

        if (TimePickerDecreaseHourTitle.HasValue() && bitDatePicker.HasNotBeenSet(nameof(TimePickerDecreaseHourTitle)))
        {
            bitDatePicker.TimePickerDecreaseHourTitle = TimePickerDecreaseHourTitle!;
        }

        if (TimePickerDecreaseMinuteIcon is not null && bitDatePicker.HasNotBeenSet(nameof(TimePickerDecreaseMinuteIcon)))
        {
            bitDatePicker.TimePickerDecreaseMinuteIcon = TimePickerDecreaseMinuteIcon;
        }

        if (TimePickerDecreaseMinuteIconName.HasValue() && bitDatePicker.HasNotBeenSet(nameof(TimePickerDecreaseMinuteIconName)))
        {
            bitDatePicker.TimePickerDecreaseMinuteIconName = TimePickerDecreaseMinuteIconName;
        }

        if (TimePickerDecreaseMinuteTitle.HasValue() && bitDatePicker.HasNotBeenSet(nameof(TimePickerDecreaseMinuteTitle)))
        {
            bitDatePicker.TimePickerDecreaseMinuteTitle = TimePickerDecreaseMinuteTitle!;
        }

        if (TimePickerHourTitle.HasValue() && bitDatePicker.HasNotBeenSet(nameof(TimePickerHourTitle)))
        {
            bitDatePicker.TimePickerHourTitle = TimePickerHourTitle!;
        }

        if (TimePickerIncreaseHourIcon is not null && bitDatePicker.HasNotBeenSet(nameof(TimePickerIncreaseHourIcon)))
        {
            bitDatePicker.TimePickerIncreaseHourIcon = TimePickerIncreaseHourIcon;
        }

        if (TimePickerIncreaseHourIconName.HasValue() && bitDatePicker.HasNotBeenSet(nameof(TimePickerIncreaseHourIconName)))
        {
            bitDatePicker.TimePickerIncreaseHourIconName = TimePickerIncreaseHourIconName;
        }

        if (TimePickerIncreaseHourTitle.HasValue() && bitDatePicker.HasNotBeenSet(nameof(TimePickerIncreaseHourTitle)))
        {
            bitDatePicker.TimePickerIncreaseHourTitle = TimePickerIncreaseHourTitle!;
        }

        if (TimePickerIncreaseMinuteIcon is not null && bitDatePicker.HasNotBeenSet(nameof(TimePickerIncreaseMinuteIcon)))
        {
            bitDatePicker.TimePickerIncreaseMinuteIcon = TimePickerIncreaseMinuteIcon;
        }

        if (TimePickerIncreaseMinuteIconName.HasValue() && bitDatePicker.HasNotBeenSet(nameof(TimePickerIncreaseMinuteIconName)))
        {
            bitDatePicker.TimePickerIncreaseMinuteIconName = TimePickerIncreaseMinuteIconName;
        }

        if (TimePickerIncreaseMinuteTitle.HasValue() && bitDatePicker.HasNotBeenSet(nameof(TimePickerIncreaseMinuteTitle)))
        {
            bitDatePicker.TimePickerIncreaseMinuteTitle = TimePickerIncreaseMinuteTitle!;
        }

        if (TimePickerMinuteTitle.HasValue() && bitDatePicker.HasNotBeenSet(nameof(TimePickerMinuteTitle)))
        {
            bitDatePicker.TimePickerMinuteTitle = TimePickerMinuteTitle!;
        }

        if (TimePickerDecreaseSecondIcon is not null && bitDatePicker.HasNotBeenSet(nameof(TimePickerDecreaseSecondIcon)))
        {
            bitDatePicker.TimePickerDecreaseSecondIcon = TimePickerDecreaseSecondIcon;
        }

        if (TimePickerDecreaseSecondIconName.HasValue() && bitDatePicker.HasNotBeenSet(nameof(TimePickerDecreaseSecondIconName)))
        {
            bitDatePicker.TimePickerDecreaseSecondIconName = TimePickerDecreaseSecondIconName;
        }

        if (TimePickerDecreaseSecondTitle.HasValue() && bitDatePicker.HasNotBeenSet(nameof(TimePickerDecreaseSecondTitle)))
        {
            bitDatePicker.TimePickerDecreaseSecondTitle = TimePickerDecreaseSecondTitle!;
        }

        if (TimePickerIncreaseSecondIcon is not null && bitDatePicker.HasNotBeenSet(nameof(TimePickerIncreaseSecondIcon)))
        {
            bitDatePicker.TimePickerIncreaseSecondIcon = TimePickerIncreaseSecondIcon;
        }

        if (TimePickerIncreaseSecondIconName.HasValue() && bitDatePicker.HasNotBeenSet(nameof(TimePickerIncreaseSecondIconName)))
        {
            bitDatePicker.TimePickerIncreaseSecondIconName = TimePickerIncreaseSecondIconName;
        }

        if (TimePickerIncreaseSecondTitle.HasValue() && bitDatePicker.HasNotBeenSet(nameof(TimePickerIncreaseSecondTitle)))
        {
            bitDatePicker.TimePickerIncreaseSecondTitle = TimePickerIncreaseSecondTitle!;
        }

        if (TimePickerSecondTitle.HasValue() && bitDatePicker.HasNotBeenSet(nameof(TimePickerSecondTitle)))
        {
            bitDatePicker.TimePickerSecondTitle = TimePickerSecondTitle!;
        }

        if (TimeZone is not null && bitDatePicker.HasNotBeenSet(nameof(TimeZone)))
        {
            rebuildView |= Equals(bitDatePicker.TimeZone, TimeZone) is false;

            bitDatePicker.TimeZone = TimeZone;
        }

        if (Today.HasValue && bitDatePicker.HasNotBeenSet(nameof(Today)))
        {
            rebuildView |= bitDatePicker.Today != Today.Value;

            bitDatePicker.Today = Today.Value;
        }

        if (Underlined.HasValue && bitDatePicker.HasNotBeenSet(nameof(Underlined)))
        {
            bitDatePicker.Underlined = Underlined.Value;

            bitDatePicker.ClassBuilder.Reset();
        }

        if (WeekNumberRule.HasValue && bitDatePicker.HasNotBeenSet(nameof(WeekNumberRule)))
        {
            bitDatePicker.WeekNumberRule = WeekNumberRule.Value;
        }

        if (WeekNumbersHeaderTitle.HasValue() && bitDatePicker.HasNotBeenSet(nameof(WeekNumbersHeaderTitle)))
        {
            bitDatePicker.WeekNumbersHeaderTitle = WeekNumbersHeaderTitle!;
        }

        if (WeekNumberTitle.HasValue() && bitDatePicker.HasNotBeenSet(nameof(WeekNumberTitle)))
        {
            bitDatePicker.WeekNumberTitle = WeekNumberTitle!;
        }

        if (YearCellTemplate is not null && bitDatePicker.HasNotBeenSet(nameof(YearCellTemplate)))
        {
            bitDatePicker.YearCellTemplate = YearCellTemplate;
        }

        if (YearPickerToggleTitle.HasValue() && bitDatePicker.HasNotBeenSet(nameof(YearPickerToggleTitle)))
        {
            bitDatePicker.YearPickerToggleTitle = YearPickerToggleTitle!;
        }

        if (YearRangePickerToggleTitle.HasValue() && bitDatePicker.HasNotBeenSet(nameof(YearRangePickerToggleTitle)))
        {
            bitDatePicker.YearRangePickerToggleTitle = YearRangePickerToggleTitle!;
        }

        if (rebuildView)
        {
            bitDatePicker.OnSetParameters();
        }
    }
}
