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
    /// Detailed description of the DatePicker for the benefit of screen readers, read after
    /// <see cref="Description"/>.
    /// </summary>
    public string? AriaDescription { get; set; }

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
    /// The helper text rendered below the DatePicker, also tied to its input as its accessible description.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// The custom template for the description of the DatePicker, which replaces <see cref="Description"/>.
    /// </summary>
    public RenderFragment? DescriptionTemplate { get; set; }

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
    public BitPlacement? IconPlacement { get; set; }

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

        if (AllowDeselect.HasValue)
        {
            bitDatePicker.TakeFromCascade(nameof(AllowDeselect), AllowDeselect.Value, static d => d.AllowDeselect, static (d, v) => d.AllowDeselect = v);
        }

        if (AllowedHours is not null)
        {
            bitDatePicker.TakeFromCascade(nameof(AllowedHours), AllowedHours, static d => d.AllowedHours, static (d, v) => d.AllowedHours = v);
        }

        if (AllowedMinutes is not null)
        {
            bitDatePicker.TakeFromCascade(nameof(AllowedMinutes), AllowedMinutes, static d => d.AllowedMinutes, static (d, v) => d.AllowedMinutes = v);
        }

        if (AllowedSeconds is not null)
        {
            bitDatePicker.TakeFromCascade(nameof(AllowedSeconds), AllowedSeconds, static d => d.AllowedSeconds, static (d, v) => d.AllowedSeconds = v);
        }

        if (AllowTextInput.HasValue)
        {
            bitDatePicker.TakeFromCascade(nameof(AllowTextInput), AllowTextInput.Value, static d => d.AllowTextInput, static (d, v) => d.AllowTextInput = v);
        }

        if (AriaDescription.HasValue())
        {
            bitDatePicker.TakeFromCascade(nameof(AriaDescription), AriaDescription, static d => d.AriaDescription, static (d, v) => d.AriaDescription = v);
        }

        if (AutoClose.HasValue)
        {
            bitDatePicker.TakeFromCascade(nameof(AutoClose), AutoClose.Value, static d => d.AutoClose, static (d, v) => d.AutoClose = v);
        }

        if (AutoFocus.HasValue)
        {
            bitDatePicker.TakeFromCascade(nameof(AutoFocus), AutoFocus.Value, static d => d.AutoFocus, static (d, v) => d.AutoFocus = v);
        }

        if (CalloutAriaLabel.HasValue())
        {
            bitDatePicker.TakeFromCascade(nameof(CalloutAriaLabel), CalloutAriaLabel!, static d => d.CalloutAriaLabel, static (d, v) => d.CalloutAriaLabel = v);
        }

        if (CalloutFooterTemplate is not null)
        {
            bitDatePicker.TakeFromCascade(nameof(CalloutFooterTemplate), CalloutFooterTemplate, static d => d.CalloutFooterTemplate, static (d, v) => d.CalloutFooterTemplate = v);
        }

        if (CalloutHeaderTemplate is not null)
        {
            bitDatePicker.TakeFromCascade(nameof(CalloutHeaderTemplate), CalloutHeaderTemplate, static d => d.CalloutHeaderTemplate, static (d, v) => d.CalloutHeaderTemplate = v);
        }

        if (CalloutHtmlAttributes is not null)
        {
            bitDatePicker.TakeFromCascade(nameof(CalloutHtmlAttributes), CalloutHtmlAttributes, static d => d.CalloutHtmlAttributes, static (d, v) => d.CalloutHtmlAttributes = v);
        }

        if (Classes is not null)
        {
            bitDatePicker.TakeFromCascade(nameof(Classes), Classes, static d => d.Classes, static (d, v) => d.Classes = v);
        }

        if (ClearButtonIcon is not null)
        {
            bitDatePicker.TakeFromCascade(nameof(ClearButtonIcon), ClearButtonIcon, static d => d.ClearButtonIcon, static (d, v) => d.ClearButtonIcon = v);
        }

        if (ClearButtonIconName.HasValue())
        {
            bitDatePicker.TakeFromCascade(nameof(ClearButtonIconName), ClearButtonIconName, static d => d.ClearButtonIconName, static (d, v) => d.ClearButtonIconName = v);
        }

        if (ClearButtonTitle.HasValue())
        {
            bitDatePicker.TakeFromCascade(nameof(ClearButtonTitle), ClearButtonTitle!, static d => d.ClearButtonTitle, static (d, v) => d.ClearButtonTitle = v);
        }

        if (CloseButtonIcon is not null)
        {
            bitDatePicker.TakeFromCascade(nameof(CloseButtonIcon), CloseButtonIcon, static d => d.CloseButtonIcon, static (d, v) => d.CloseButtonIcon = v);
        }

        if (CloseButtonIconName.HasValue())
        {
            bitDatePicker.TakeFromCascade(nameof(CloseButtonIconName), CloseButtonIconName, static d => d.CloseButtonIconName, static (d, v) => d.CloseButtonIconName = v);
        }

        if (CloseButtonTitle.HasValue())
        {
            bitDatePicker.TakeFromCascade(nameof(CloseButtonTitle), CloseButtonTitle!, static d => d.CloseButtonTitle, static (d, v) => d.CloseButtonTitle = v);
        }

        if (Color.HasValue)
        {
            bitDatePicker.TakeFromCascade(nameof(Color), Color.Value, static d => d.Color, static (d, v) => d.Color = v);
        }

        if (ContinuousSpinDelay.HasValue)
        {
            bitDatePicker.TakeFromCascade(nameof(ContinuousSpinDelay), ContinuousSpinDelay.Value, static d => d.ContinuousSpinDelay, static (d, v) => d.ContinuousSpinDelay = v);
        }

        if (ContinuousSpinInterval.HasValue)
        {
            bitDatePicker.TakeFromCascade(nameof(ContinuousSpinInterval), ContinuousSpinInterval.Value, static d => d.ContinuousSpinInterval, static (d, v) => d.ContinuousSpinInterval = v);
        }

        if (Culture is not null)
        {
            bitDatePicker.TakeFromCascade(nameof(Culture), Culture, static d => d.Culture, static (d, v) => d.Culture = v);
        }

        if (DateFormat.HasValue())
        {
            bitDatePicker.TakeFromCascade(nameof(DateFormat), DateFormat, static d => d.DateFormat, static (d, v) => d.DateFormat = v);
        }

        if (DateFormatAriaDescription is not null)
        {
            bitDatePicker.TakeFromCascade(nameof(DateFormatAriaDescription), DateFormatAriaDescription, static d => d.DateFormatAriaDescription, static (d, v) => d.DateFormatAriaDescription = v);
        }

        if (DayCellTemplate is not null)
        {
            bitDatePicker.TakeFromCascade(nameof(DayCellTemplate), DayCellTemplate, static d => d.DayCellTemplate, static (d, v) => d.DayCellTemplate = v);
        }

        if (Description.HasValue())
        {
            bitDatePicker.TakeFromCascade(nameof(Description), Description, static d => d.Description, static (d, v) => d.Description = v);
        }

        if (DescriptionTemplate is not null)
        {
            bitDatePicker.TakeFromCascade(nameof(DescriptionTemplate), DescriptionTemplate, static d => d.DescriptionTemplate, static (d, v) => d.DescriptionTemplate = v);
        }

        if (DisabledDateErrorMessage.HasValue())
        {
            bitDatePicker.TakeFromCascade(nameof(DisabledDateErrorMessage), DisabledDateErrorMessage, static d => d.DisabledDateErrorMessage, static (d, v) => d.DisabledDateErrorMessage = v);
        }

        if (DisabledDates is not null)
        {
            bitDatePicker.TakeFromCascade(nameof(DisabledDates), DisabledDates, static d => d.DisabledDates, static (d, v) => d.DisabledDates = v);
        }

        if (DisabledDaysOfWeek is not null)
        {
            bitDatePicker.TakeFromCascade(nameof(DisabledDaysOfWeek), DisabledDaysOfWeek, static d => d.DisabledDaysOfWeek, static (d, v) => d.DisabledDaysOfWeek = v);
        }

        if (DisableFuture.HasValue)
        {
            bitDatePicker.TakeFromCascade(nameof(DisableFuture), DisableFuture.Value, static d => d.DisableFuture, static (d, v) => d.DisableFuture = v);
        }

        if (DisablePast.HasValue)
        {
            bitDatePicker.TakeFromCascade(nameof(DisablePast), DisablePast.Value, static d => d.DisablePast, static (d, v) => d.DisablePast = v);
        }

        if (DisallowedTimeErrorMessage.HasValue())
        {
            bitDatePicker.TakeFromCascade(nameof(DisallowedTimeErrorMessage), DisallowedTimeErrorMessage, static d => d.DisallowedTimeErrorMessage, static (d, v) => d.DisallowedTimeErrorMessage = v);
        }

        if (DropDirection.HasValue)
        {
            bitDatePicker.TakeFromCascade(nameof(DropDirection), DropDirection.Value, static d => d.DropDirection, static (d, v) => d.DropDirection = v);
        }

        if (FirstDayOfWeek.HasValue)
        {
            bitDatePicker.TakeFromCascade(nameof(FirstDayOfWeek), FirstDayOfWeek.Value, static d => d.FirstDayOfWeek, static (d, v) => d.FirstDayOfWeek = v);
        }

        if (FixedWeeks.HasValue)
        {
            bitDatePicker.TakeFromCascade(nameof(FixedWeeks), FixedWeeks.Value, static d => d.FixedWeeks, static (d, v) => d.FixedWeeks = v);
        }

        if (GetDayClass is not null)
        {
            bitDatePicker.TakeFromCascade(nameof(GetDayClass), GetDayClass, static d => d.GetDayClass, static (d, v) => d.GetDayClass = v);
        }

        if (GoToNextMonthTitle.HasValue())
        {
            bitDatePicker.TakeFromCascade(nameof(GoToNextMonthTitle), GoToNextMonthTitle!, static d => d.GoToNextMonthTitle, static (d, v) => d.GoToNextMonthTitle = v);
        }

        if (GoToNextYearRangeTitle.HasValue())
        {
            bitDatePicker.TakeFromCascade(nameof(GoToNextYearRangeTitle), GoToNextYearRangeTitle!, static d => d.GoToNextYearRangeTitle, static (d, v) => d.GoToNextYearRangeTitle = v);
        }

        if (GoToNextYearTitle.HasValue())
        {
            bitDatePicker.TakeFromCascade(nameof(GoToNextYearTitle), GoToNextYearTitle!, static d => d.GoToNextYearTitle, static (d, v) => d.GoToNextYearTitle = v);
        }

        if (GoToPrevMonthTitle.HasValue())
        {
            bitDatePicker.TakeFromCascade(nameof(GoToPrevMonthTitle), GoToPrevMonthTitle!, static d => d.GoToPrevMonthTitle, static (d, v) => d.GoToPrevMonthTitle = v);
        }

        if (GoToPrevYearRangeTitle.HasValue())
        {
            bitDatePicker.TakeFromCascade(nameof(GoToPrevYearRangeTitle), GoToPrevYearRangeTitle!, static d => d.GoToPrevYearRangeTitle, static (d, v) => d.GoToPrevYearRangeTitle = v);
        }

        if (GoToPrevYearTitle.HasValue())
        {
            bitDatePicker.TakeFromCascade(nameof(GoToPrevYearTitle), GoToPrevYearTitle!, static d => d.GoToPrevYearTitle, static (d, v) => d.GoToPrevYearTitle = v);
        }

        if (GoToTodayIcon is not null)
        {
            bitDatePicker.TakeFromCascade(nameof(GoToTodayIcon), GoToTodayIcon, static d => d.GoToTodayIcon, static (d, v) => d.GoToTodayIcon = v);
        }

        if (GoToTodayIconName.HasValue())
        {
            bitDatePicker.TakeFromCascade(nameof(GoToTodayIconName), GoToTodayIconName, static d => d.GoToTodayIconName, static (d, v) => d.GoToTodayIconName = v);
        }

        if (GoToTodayTitle.HasValue())
        {
            bitDatePicker.TakeFromCascade(nameof(GoToTodayTitle), GoToTodayTitle!, static d => d.GoToTodayTitle, static (d, v) => d.GoToTodayTitle = v);
        }

        if (HasBorder.HasValue)
        {
            bitDatePicker.TakeFromCascade(nameof(HasBorder), HasBorder.Value, static d => d.HasBorder, static (d, v) => d.HasBorder = v);
        }

        if (HideTimePickerIcon is not null)
        {
            bitDatePicker.TakeFromCascade(nameof(HideTimePickerIcon), HideTimePickerIcon, static d => d.HideTimePickerIcon, static (d, v) => d.HideTimePickerIcon = v);
        }

        if (HideTimePickerIconName.HasValue())
        {
            bitDatePicker.TakeFromCascade(nameof(HideTimePickerIconName), HideTimePickerIconName, static d => d.HideTimePickerIconName, static (d, v) => d.HideTimePickerIconName = v);
        }

        if (HideTimePickerTitle.HasValue())
        {
            bitDatePicker.TakeFromCascade(nameof(HideTimePickerTitle), HideTimePickerTitle!, static d => d.HideTimePickerTitle, static (d, v) => d.HideTimePickerTitle = v);
        }

        if (HighlightCurrentMonth.HasValue)
        {
            bitDatePicker.TakeFromCascade(nameof(HighlightCurrentMonth), HighlightCurrentMonth.Value, static d => d.HighlightCurrentMonth, static (d, v) => d.HighlightCurrentMonth = v);
        }

        if (HighlightedDates is not null)
        {
            bitDatePicker.TakeFromCascade(nameof(HighlightedDates), HighlightedDates, static d => d.HighlightedDates, static (d, v) => d.HighlightedDates = v);
        }

        if (HighlightedDateAriaLabel is not null)
        {
            bitDatePicker.TakeFromCascade(nameof(HighlightedDateAriaLabel), HighlightedDateAriaLabel, static d => d.HighlightedDateAriaLabel, static (d, v) => d.HighlightedDateAriaLabel = v);
        }

        if (HighlightSelectedMonth.HasValue)
        {
            bitDatePicker.TakeFromCascade(nameof(HighlightSelectedMonth), HighlightSelectedMonth.Value, static d => d.HighlightSelectedMonth, static (d, v) => d.HighlightSelectedMonth = v);
        }

        if (HighlightToday.HasValue)
        {
            bitDatePicker.TakeFromCascade(nameof(HighlightToday), HighlightToday.Value, static d => d.HighlightToday, static (d, v) => d.HighlightToday = v);
        }

        if (HourStep.HasValue)
        {
            bitDatePicker.TakeFromCascade(nameof(HourStep), HourStep.Value, static d => d.HourStep, static (d, v) => d.HourStep = v);
        }

        if (Icon is not null)
        {
            bitDatePicker.TakeFromCascade(nameof(Icon), Icon, static d => d.Icon, static (d, v) => d.Icon = v);
        }

        if (IconPlacement.HasValue)
        {
            bitDatePicker.TakeFromCascade(nameof(IconPlacement), IconPlacement.Value, static d => d.IconPlacement, static (d, v) => d.IconPlacement = v);
        }

        if (IconName.HasValue())
        {
            bitDatePicker.TakeFromCascade(nameof(IconName), IconName, static d => d.IconName, static (d, v) => d.IconName = v);
        }

        if (IconTemplate is not null)
        {
            bitDatePicker.TakeFromCascade(nameof(IconTemplate), IconTemplate, static d => d.IconTemplate, static (d, v) => d.IconTemplate = v);
        }

        if (InvalidErrorMessage.HasValue())
        {
            bitDatePicker.TakeFromCascade(nameof(InvalidErrorMessage), InvalidErrorMessage, static d => d.InvalidErrorMessage, static (d, v) => d.InvalidErrorMessage = v);
        }

        if (IsDateDisabled is not null)
        {
            bitDatePicker.TakeFromCascade(nameof(IsDateDisabled), IsDateDisabled, static d => d.IsDateDisabled, static (d, v) => d.IsDateDisabled = v);
        }

        if (IsMonthPickerVisible.HasValue)
        {
            bitDatePicker.TakeFromCascade(nameof(IsMonthPickerVisible), IsMonthPickerVisible.Value, static d => d.IsMonthPickerVisible, static (d, v) => d.IsMonthPickerVisible = v);
        }

        if (Label.HasValue())
        {
            bitDatePicker.TakeFromCascade(nameof(Label), Label, static d => d.Label, static (d, v) => d.Label = v);
        }

        if (LabelTemplate is not null)
        {
            bitDatePicker.TakeFromCascade(nameof(LabelTemplate), LabelTemplate, static d => d.LabelTemplate, static (d, v) => d.LabelTemplate = v);
        }

        if (MaxDate.HasValue)
        {
            bitDatePicker.TakeFromCascade(nameof(MaxDate), MaxDate.Value, static d => d.MaxDate, static (d, v) => d.MaxDate = v);
        }

        if (MaxTime.HasValue)
        {
            bitDatePicker.TakeFromCascade(nameof(MaxTime), MaxTime.Value, static d => d.MaxTime, static (d, v) => d.MaxTime = v);
        }

        if (MinDate.HasValue)
        {
            bitDatePicker.TakeFromCascade(nameof(MinDate), MinDate.Value, static d => d.MinDate, static (d, v) => d.MinDate = v);
        }

        if (MinTime.HasValue)
        {
            bitDatePicker.TakeFromCascade(nameof(MinTime), MinTime.Value, static d => d.MinTime, static (d, v) => d.MinTime = v);
        }

        if (MonthCount.HasValue)
        {
            bitDatePicker.TakeFromCascade(nameof(MonthCount), MonthCount.Value, static d => d.MonthCount, static (d, v) => d.MonthCount = v);
        }

        if (PagedNavigation.HasValue)
        {
            bitDatePicker.TakeFromCascade(nameof(PagedNavigation), PagedNavigation.Value, static d => d.PagedNavigation, static (d, v) => d.PagedNavigation = v);
        }

        if (MinuteStep.HasValue)
        {
            bitDatePicker.TakeFromCascade(nameof(MinuteStep), MinuteStep.Value, static d => d.MinuteStep, static (d, v) => d.MinuteStep = v);
        }

        if (Mode.HasValue)
        {
            bitDatePicker.TakeFromCascade(nameof(Mode), Mode.Value, static d => d.Mode, static (d, v) => d.Mode = v);
        }

        if (MonthCellTemplate is not null)
        {
            bitDatePicker.TakeFromCascade(nameof(MonthCellTemplate), MonthCellTemplate, static d => d.MonthCellTemplate, static (d, v) => d.MonthCellTemplate = v);
        }

        if (MonthPickerToggleTitle.HasValue())
        {
            bitDatePicker.TakeFromCascade(nameof(MonthPickerToggleTitle), MonthPickerToggleTitle!, static d => d.MonthPickerToggleTitle, static (d, v) => d.MonthPickerToggleTitle = v);
        }

        if (NextMonthNavIcon is not null)
        {
            bitDatePicker.TakeFromCascade(nameof(NextMonthNavIcon), NextMonthNavIcon, static d => d.NextMonthNavIcon, static (d, v) => d.NextMonthNavIcon = v);
        }

        if (NextMonthNavIconName.HasValue())
        {
            bitDatePicker.TakeFromCascade(nameof(NextMonthNavIconName), NextMonthNavIconName, static d => d.NextMonthNavIconName, static (d, v) => d.NextMonthNavIconName = v);
        }

        if (NextYearNavIcon is not null)
        {
            bitDatePicker.TakeFromCascade(nameof(NextYearNavIcon), NextYearNavIcon, static d => d.NextYearNavIcon, static (d, v) => d.NextYearNavIcon = v);
        }

        if (NextYearNavIconName.HasValue())
        {
            bitDatePicker.TakeFromCascade(nameof(NextYearNavIconName), NextYearNavIconName, static d => d.NextYearNavIconName, static (d, v) => d.NextYearNavIconName = v);
        }

        if (NextYearRangeNavIcon is not null)
        {
            bitDatePicker.TakeFromCascade(nameof(NextYearRangeNavIcon), NextYearRangeNavIcon, static d => d.NextYearRangeNavIcon, static (d, v) => d.NextYearRangeNavIcon = v);
        }

        if (NextYearRangeNavIconName.HasValue())
        {
            bitDatePicker.TakeFromCascade(nameof(NextYearRangeNavIconName), NextYearRangeNavIconName, static d => d.NextYearRangeNavIconName, static (d, v) => d.NextYearRangeNavIconName = v);
        }

        if (NowButtonIcon is not null)
        {
            bitDatePicker.TakeFromCascade(nameof(NowButtonIcon), NowButtonIcon, static d => d.NowButtonIcon, static (d, v) => d.NowButtonIcon = v);
        }

        if (NowButtonIconName.HasValue())
        {
            bitDatePicker.TakeFromCascade(nameof(NowButtonIconName), NowButtonIconName, static d => d.NowButtonIconName, static (d, v) => d.NowButtonIconName = v);
        }

        if (NowButtonTitle.HasValue())
        {
            bitDatePicker.TakeFromCascade(nameof(NowButtonTitle), NowButtonTitle!, static d => d.NowButtonTitle, static (d, v) => d.NowButtonTitle = v);
        }

        if (OutOfRangeErrorMessage.HasValue())
        {
            bitDatePicker.TakeFromCascade(nameof(OutOfRangeErrorMessage), OutOfRangeErrorMessage, static d => d.OutOfRangeErrorMessage, static (d, v) => d.OutOfRangeErrorMessage = v);
        }

        if (Placeholder.HasValue())
        {
            bitDatePicker.TakeFromCascade(nameof(Placeholder), Placeholder!, static d => d.Placeholder, static (d, v) => d.Placeholder = v);
        }

        if (PrevMonthNavIcon is not null)
        {
            bitDatePicker.TakeFromCascade(nameof(PrevMonthNavIcon), PrevMonthNavIcon, static d => d.PrevMonthNavIcon, static (d, v) => d.PrevMonthNavIcon = v);
        }

        if (PrevMonthNavIconName.HasValue())
        {
            bitDatePicker.TakeFromCascade(nameof(PrevMonthNavIconName), PrevMonthNavIconName, static d => d.PrevMonthNavIconName, static (d, v) => d.PrevMonthNavIconName = v);
        }

        if (PrevYearNavIcon is not null)
        {
            bitDatePicker.TakeFromCascade(nameof(PrevYearNavIcon), PrevYearNavIcon, static d => d.PrevYearNavIcon, static (d, v) => d.PrevYearNavIcon = v);
        }

        if (PrevYearNavIconName.HasValue())
        {
            bitDatePicker.TakeFromCascade(nameof(PrevYearNavIconName), PrevYearNavIconName, static d => d.PrevYearNavIconName, static (d, v) => d.PrevYearNavIconName = v);
        }

        if (PrevYearRangeNavIcon is not null)
        {
            bitDatePicker.TakeFromCascade(nameof(PrevYearRangeNavIcon), PrevYearRangeNavIcon, static d => d.PrevYearRangeNavIcon, static (d, v) => d.PrevYearRangeNavIcon = v);
        }

        if (PrevYearRangeNavIconName.HasValue())
        {
            bitDatePicker.TakeFromCascade(nameof(PrevYearRangeNavIconName), PrevYearRangeNavIconName, static d => d.PrevYearRangeNavIconName, static (d, v) => d.PrevYearRangeNavIconName = v);
        }

        if (Responsive.HasValue)
        {
            bitDatePicker.TakeFromCascade(nameof(Responsive), Responsive.Value, static d => d.Responsive, static (d, v) => d.Responsive = v);
        }

        if (SecondStep.HasValue)
        {
            bitDatePicker.TakeFromCascade(nameof(SecondStep), SecondStep.Value, static d => d.SecondStep, static (d, v) => d.SecondStep = v);
        }

        if (SelectedDateAriaAtomic.HasValue())
        {
            bitDatePicker.TakeFromCascade(nameof(SelectedDateAriaAtomic), SelectedDateAriaAtomic!, static d => d.SelectedDateAriaAtomic, static (d, v) => d.SelectedDateAriaAtomic = v);
        }

        if (ShowClearButton.HasValue)
        {
            bitDatePicker.TakeFromCascade(nameof(ShowClearButton), ShowClearButton.Value, static d => d.ShowClearButton, static (d, v) => d.ShowClearButton = v);
        }

        if (ShowCloseButton.HasValue)
        {
            bitDatePicker.TakeFromCascade(nameof(ShowCloseButton), ShowCloseButton.Value, static d => d.ShowCloseButton, static (d, v) => d.ShowCloseButton = v);
        }

        if (ShowGoToToday.HasValue)
        {
            bitDatePicker.TakeFromCascade(nameof(ShowGoToToday), ShowGoToToday.Value, static d => d.ShowGoToToday, static (d, v) => d.ShowGoToToday = v);
        }

        if (ShowMonthPickerAsOverlay.HasValue)
        {
            bitDatePicker.TakeFromCascade(nameof(ShowMonthPickerAsOverlay), ShowMonthPickerAsOverlay.Value, static d => d.ShowMonthPickerAsOverlay, static (d, v) => d.ShowMonthPickerAsOverlay = v);
        }

        if (ShowNowButton.HasValue)
        {
            bitDatePicker.TakeFromCascade(nameof(ShowNowButton), ShowNowButton.Value, static d => d.ShowNowButton, static (d, v) => d.ShowNowButton = v);
        }

        if (ShowOutsideDays.HasValue)
        {
            bitDatePicker.TakeFromCascade(nameof(ShowOutsideDays), ShowOutsideDays.Value, static d => d.ShowOutsideDays, static (d, v) => d.ShowOutsideDays = v);
        }

        if (ShowSeconds.HasValue)
        {
            bitDatePicker.TakeFromCascade(nameof(ShowSeconds), ShowSeconds.Value, static d => d.ShowSeconds, static (d, v) => d.ShowSeconds = v);
        }

        if (ShowTimePicker.HasValue)
        {
            bitDatePicker.TakeFromCascade(nameof(ShowTimePicker), ShowTimePicker.Value, static d => d.ShowTimePicker, static (d, v) => d.ShowTimePicker = v);
        }

        if (ShowTimePickerAsOverlay.HasValue)
        {
            bitDatePicker.TakeFromCascade(nameof(ShowTimePickerAsOverlay), ShowTimePickerAsOverlay.Value, static d => d.ShowTimePickerAsOverlay, static (d, v) => d.ShowTimePickerAsOverlay = v);
        }

        if (ShowTimePickerIcon is not null)
        {
            bitDatePicker.TakeFromCascade(nameof(ShowTimePickerIcon), ShowTimePickerIcon, static d => d.ShowTimePickerIcon, static (d, v) => d.ShowTimePickerIcon = v);
        }

        if (ShowTimePickerIconName.HasValue())
        {
            bitDatePicker.TakeFromCascade(nameof(ShowTimePickerIconName), ShowTimePickerIconName, static d => d.ShowTimePickerIconName, static (d, v) => d.ShowTimePickerIconName = v);
        }

        if (ShowTimePickerTitle.HasValue())
        {
            bitDatePicker.TakeFromCascade(nameof(ShowTimePickerTitle), ShowTimePickerTitle!, static d => d.ShowTimePickerTitle, static (d, v) => d.ShowTimePickerTitle = v);
        }

        if (ShowWeekNumbers.HasValue)
        {
            bitDatePicker.TakeFromCascade(nameof(ShowWeekNumbers), ShowWeekNumbers.Value, static d => d.ShowWeekNumbers, static (d, v) => d.ShowWeekNumbers = v);
        }

        if (Size.HasValue)
        {
            bitDatePicker.TakeFromCascade(nameof(Size), Size.Value, static d => d.Size, static (d, v) => d.Size = v);
        }

        if (Standalone.HasValue)
        {
            bitDatePicker.TakeFromCascade(nameof(Standalone), Standalone.Value, static d => d.Standalone, static (d, v) => d.Standalone = v);
        }

        if (StartingValue.HasValue)
        {
            bitDatePicker.TakeFromCascade(nameof(StartingValue), StartingValue.Value, static d => d.StartingValue, static (d, v) => d.StartingValue = v);
        }

        if (Styles is not null)
        {
            bitDatePicker.TakeFromCascade(nameof(Styles), Styles, static d => d.Styles, static (d, v) => d.Styles = v);
        }

        if (TimeFormat.HasValue)
        {
            bitDatePicker.TakeFromCascade(nameof(TimeFormat), TimeFormat.Value, static d => d.TimeFormat, static (d, v) => d.TimeFormat = v);
        }

        if (TimePickerDecreaseHourIcon is not null)
        {
            bitDatePicker.TakeFromCascade(nameof(TimePickerDecreaseHourIcon), TimePickerDecreaseHourIcon, static d => d.TimePickerDecreaseHourIcon, static (d, v) => d.TimePickerDecreaseHourIcon = v);
        }

        if (TimePickerDecreaseHourIconName.HasValue())
        {
            bitDatePicker.TakeFromCascade(nameof(TimePickerDecreaseHourIconName), TimePickerDecreaseHourIconName, static d => d.TimePickerDecreaseHourIconName, static (d, v) => d.TimePickerDecreaseHourIconName = v);
        }

        if (TimePickerDecreaseHourTitle.HasValue())
        {
            bitDatePicker.TakeFromCascade(nameof(TimePickerDecreaseHourTitle), TimePickerDecreaseHourTitle!, static d => d.TimePickerDecreaseHourTitle, static (d, v) => d.TimePickerDecreaseHourTitle = v);
        }

        if (TimePickerDecreaseMinuteIcon is not null)
        {
            bitDatePicker.TakeFromCascade(nameof(TimePickerDecreaseMinuteIcon), TimePickerDecreaseMinuteIcon, static d => d.TimePickerDecreaseMinuteIcon, static (d, v) => d.TimePickerDecreaseMinuteIcon = v);
        }

        if (TimePickerDecreaseMinuteIconName.HasValue())
        {
            bitDatePicker.TakeFromCascade(nameof(TimePickerDecreaseMinuteIconName), TimePickerDecreaseMinuteIconName, static d => d.TimePickerDecreaseMinuteIconName, static (d, v) => d.TimePickerDecreaseMinuteIconName = v);
        }

        if (TimePickerDecreaseMinuteTitle.HasValue())
        {
            bitDatePicker.TakeFromCascade(nameof(TimePickerDecreaseMinuteTitle), TimePickerDecreaseMinuteTitle!, static d => d.TimePickerDecreaseMinuteTitle, static (d, v) => d.TimePickerDecreaseMinuteTitle = v);
        }

        if (TimePickerHourTitle.HasValue())
        {
            bitDatePicker.TakeFromCascade(nameof(TimePickerHourTitle), TimePickerHourTitle!, static d => d.TimePickerHourTitle, static (d, v) => d.TimePickerHourTitle = v);
        }

        if (TimePickerIncreaseHourIcon is not null)
        {
            bitDatePicker.TakeFromCascade(nameof(TimePickerIncreaseHourIcon), TimePickerIncreaseHourIcon, static d => d.TimePickerIncreaseHourIcon, static (d, v) => d.TimePickerIncreaseHourIcon = v);
        }

        if (TimePickerIncreaseHourIconName.HasValue())
        {
            bitDatePicker.TakeFromCascade(nameof(TimePickerIncreaseHourIconName), TimePickerIncreaseHourIconName, static d => d.TimePickerIncreaseHourIconName, static (d, v) => d.TimePickerIncreaseHourIconName = v);
        }

        if (TimePickerIncreaseHourTitle.HasValue())
        {
            bitDatePicker.TakeFromCascade(nameof(TimePickerIncreaseHourTitle), TimePickerIncreaseHourTitle!, static d => d.TimePickerIncreaseHourTitle, static (d, v) => d.TimePickerIncreaseHourTitle = v);
        }

        if (TimePickerIncreaseMinuteIcon is not null)
        {
            bitDatePicker.TakeFromCascade(nameof(TimePickerIncreaseMinuteIcon), TimePickerIncreaseMinuteIcon, static d => d.TimePickerIncreaseMinuteIcon, static (d, v) => d.TimePickerIncreaseMinuteIcon = v);
        }

        if (TimePickerIncreaseMinuteIconName.HasValue())
        {
            bitDatePicker.TakeFromCascade(nameof(TimePickerIncreaseMinuteIconName), TimePickerIncreaseMinuteIconName, static d => d.TimePickerIncreaseMinuteIconName, static (d, v) => d.TimePickerIncreaseMinuteIconName = v);
        }

        if (TimePickerIncreaseMinuteTitle.HasValue())
        {
            bitDatePicker.TakeFromCascade(nameof(TimePickerIncreaseMinuteTitle), TimePickerIncreaseMinuteTitle!, static d => d.TimePickerIncreaseMinuteTitle, static (d, v) => d.TimePickerIncreaseMinuteTitle = v);
        }

        if (TimePickerMinuteTitle.HasValue())
        {
            bitDatePicker.TakeFromCascade(nameof(TimePickerMinuteTitle), TimePickerMinuteTitle!, static d => d.TimePickerMinuteTitle, static (d, v) => d.TimePickerMinuteTitle = v);
        }

        if (TimePickerDecreaseSecondIcon is not null)
        {
            bitDatePicker.TakeFromCascade(nameof(TimePickerDecreaseSecondIcon), TimePickerDecreaseSecondIcon, static d => d.TimePickerDecreaseSecondIcon, static (d, v) => d.TimePickerDecreaseSecondIcon = v);
        }

        if (TimePickerDecreaseSecondIconName.HasValue())
        {
            bitDatePicker.TakeFromCascade(nameof(TimePickerDecreaseSecondIconName), TimePickerDecreaseSecondIconName, static d => d.TimePickerDecreaseSecondIconName, static (d, v) => d.TimePickerDecreaseSecondIconName = v);
        }

        if (TimePickerDecreaseSecondTitle.HasValue())
        {
            bitDatePicker.TakeFromCascade(nameof(TimePickerDecreaseSecondTitle), TimePickerDecreaseSecondTitle!, static d => d.TimePickerDecreaseSecondTitle, static (d, v) => d.TimePickerDecreaseSecondTitle = v);
        }

        if (TimePickerIncreaseSecondIcon is not null)
        {
            bitDatePicker.TakeFromCascade(nameof(TimePickerIncreaseSecondIcon), TimePickerIncreaseSecondIcon, static d => d.TimePickerIncreaseSecondIcon, static (d, v) => d.TimePickerIncreaseSecondIcon = v);
        }

        if (TimePickerIncreaseSecondIconName.HasValue())
        {
            bitDatePicker.TakeFromCascade(nameof(TimePickerIncreaseSecondIconName), TimePickerIncreaseSecondIconName, static d => d.TimePickerIncreaseSecondIconName, static (d, v) => d.TimePickerIncreaseSecondIconName = v);
        }

        if (TimePickerIncreaseSecondTitle.HasValue())
        {
            bitDatePicker.TakeFromCascade(nameof(TimePickerIncreaseSecondTitle), TimePickerIncreaseSecondTitle!, static d => d.TimePickerIncreaseSecondTitle, static (d, v) => d.TimePickerIncreaseSecondTitle = v);
        }

        if (TimePickerSecondTitle.HasValue())
        {
            bitDatePicker.TakeFromCascade(nameof(TimePickerSecondTitle), TimePickerSecondTitle!, static d => d.TimePickerSecondTitle, static (d, v) => d.TimePickerSecondTitle = v);
        }

        if (TimeZone is not null)
        {
            bitDatePicker.TakeFromCascade(nameof(TimeZone), TimeZone, static d => d.TimeZone, static (d, v) => d.TimeZone = v);
        }

        if (Today.HasValue)
        {
            bitDatePicker.TakeFromCascade(nameof(Today), Today.Value, static d => d.Today, static (d, v) => d.Today = v);
        }

        if (Underlined.HasValue)
        {
            bitDatePicker.TakeFromCascade(nameof(Underlined), Underlined.Value, static d => d.Underlined, static (d, v) => d.Underlined = v);
        }

        if (WeekNumberRule.HasValue)
        {
            bitDatePicker.TakeFromCascade(nameof(WeekNumberRule), WeekNumberRule.Value, static d => d.WeekNumberRule, static (d, v) => d.WeekNumberRule = v);
        }

        if (WeekNumbersHeaderTitle.HasValue())
        {
            bitDatePicker.TakeFromCascade(nameof(WeekNumbersHeaderTitle), WeekNumbersHeaderTitle!, static d => d.WeekNumbersHeaderTitle, static (d, v) => d.WeekNumbersHeaderTitle = v);
        }

        if (WeekNumberTitle.HasValue())
        {
            bitDatePicker.TakeFromCascade(nameof(WeekNumberTitle), WeekNumberTitle!, static d => d.WeekNumberTitle, static (d, v) => d.WeekNumberTitle = v);
        }

        if (YearCellTemplate is not null)
        {
            bitDatePicker.TakeFromCascade(nameof(YearCellTemplate), YearCellTemplate, static d => d.YearCellTemplate, static (d, v) => d.YearCellTemplate = v);
        }

        if (YearPickerToggleTitle.HasValue())
        {
            bitDatePicker.TakeFromCascade(nameof(YearPickerToggleTitle), YearPickerToggleTitle!, static d => d.YearPickerToggleTitle, static (d, v) => d.YearPickerToggleTitle = v);
        }

        if (YearRangePickerToggleTitle.HasValue())
        {
            bitDatePicker.TakeFromCascade(nameof(YearRangePickerToggleTitle), YearRangePickerToggleTitle!, static d => d.YearRangePickerToggleTitle, static (d, v) => d.YearRangePickerToggleTitle = v);
        }
    }
}
