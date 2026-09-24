using System.Globalization;

namespace Bit.BlazorUI;

/// <summary>
/// The parameters for <see cref="BitDateRangePicker"/> component.
/// </summary>
public class BitDateRangePickerParams : BitComponentBaseParams, IBitComponentParams
{
    /// <summary>
    /// Represents the parameter name used to identify the <see cref="BitDateRangePicker"/> cascading parameters within <see cref="BitParams"/>.
    /// </summary>
    /// <remarks>
    /// This constant is typically used when referencing or accessing the BitDateRangePicker value in
    /// parameterized APIs or configuration settings. Using this constant helps ensure consistency and reduces the risk
    /// of typographical errors.
    /// </remarks>
    public const string ParamName = $"{nameof(BitParams)}.{nameof(BitDateRangePicker)}";



    public string Name => ParamName;



    /// <summary>
    /// Whether or not the DateRangePicker allows string date inputs. A typed range is validated against
    /// every restriction the calendar enforces (MinDate, MaxDate, MinRange, MaxRange, the disabled days
    /// and ExcludeDisabledDates), so an out-of-bounds range is rejected as an invalid value.
    /// </summary>
    public bool? AllowTextInput { get; set; }

    /// <summary>
    /// The text of the button that commits the picked range, rendered while <see cref="AutoApply"/> is off.
    /// </summary>
    public string? ApplyButtonText { get; set; }

    /// <summary>
    /// Whether every pick is applied to the value as it is made. Turning it off makes the callout a
    /// transaction: it renders a Cancel and an Apply button, and the range the callout was opened on is
    /// put back unless Apply commits the new one. The picks still reach the value while the callout is
    /// open - that is what the calendar, the presets and the time picker all read - so an application
    /// watching the value sees the range being built and then either kept or rolled back.
    /// </summary>
    /// <remarks>
    /// It has no effect on a <see cref="Standalone"/> picker, which has no callout to commit or discard,
    /// and it overrides <see cref="AutoClose"/>, since the callout has to stay open for its Apply button.
    /// </remarks>
    public bool? AutoApply { get; set; }

    /// <summary>
    /// Whether the DateRangePicker closes automatically after selecting the second value.
    /// </summary>
    /// <remarks>
    /// Ignored while <see cref="AutoApply"/> is off: the callout then waits for its Apply button.
    /// </remarks>
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
    /// Aria label of the DateRangePicker's callout for screen readers.
    /// </summary>
    public string? CalloutAriaLabel { get; set; }

    /// <summary>
    /// Custom template to render at the bottom of the DateRangePicker's callout, below everything it holds
    /// (e.g. preset buttons that set the value from the code).
    /// </summary>
    public RenderFragment? CalloutFooterTemplate { get; set; }

    /// <summary>
    /// Custom template to render at the top of the DateRangePicker's callout, above everything it holds.
    /// </summary>
    public RenderFragment? CalloutHeaderTemplate { get; set; }

    /// <summary>
    /// Capture and render additional html attributes for the DateRangePicker's callout.
    /// </summary>
    public Dictionary<string, object>? CalloutHtmlAttributes { get; set; }

    /// <summary>
    /// The text of the button that discards the picked range, rendered while <see cref="AutoApply"/> is off.
    /// </summary>
    public string? CancelButtonText { get; set; }

    /// <summary>
    /// Custom CSS classes for different parts of the BitDateRangePicker component.
    /// </summary>
    public BitDateRangePickerClassStyles? Classes { get; set; }

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
    /// The title and the aria-label of the clear button.
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
    /// The general color of the DateRangePicker that applies to the today day button, the selected range,
    /// the highlighted current month and the selected AM/PM buttons.
    /// </summary>
    public BitColor? Color { get; set; }

    /// <summary>
    /// The delay in milliseconds before the hour/minute starts changing continuously while an
    /// increase/decrease button of the time picker is held down.
    /// </summary>
    public int? ContinuousSpinDelay { get; set; }

    /// <summary>
    /// The interval in milliseconds between two consecutive changes while an increase/decrease
    /// button of the time picker is held down.
    /// </summary>
    public int? ContinuousSpinInterval { get; set; }

    /// <summary>
    /// CultureInfo for the DateRangePicker.
    /// </summary>
    public CultureInfo? Culture { get; set; }

    /// <summary>
    /// The format of the date in the DateRangePicker.
    /// </summary>
    public string? DateFormat { get; set; }

    /// <summary>
    /// Custom template to render the day cells of the DateRangePicker.
    /// </summary>
    public RenderFragment<DateTimeOffset>? DayCellTemplate { get; set; }

    /// <summary>
    /// Disables every day after today, exactly as a <see cref="MaxDate"/> of today would.
    /// When both are set, the earlier of the two bounds wins.
    /// </summary>
    public bool? DisableFuture { get; set; }

    /// <summary>
    /// Disables every day before today, exactly as a <see cref="MinDate"/> of today would.
    /// When both are set, the later of the two bounds wins.
    /// </summary>
    public bool? DisablePast { get; set; }

    /// <summary>
    /// The custom validation error message for a typed range that the DateRangePicker does not allow to be
    /// selected, through <see cref="DisabledDates"/>, <see cref="DisabledDaysOfWeek"/> or
    /// <see cref="IsDateDisabled"/>.
    /// </summary>
    public string? DisabledDateErrorMessage { get; set; }

    /// <summary>
    /// The list of dates that are disabled (not selectable) in the DateRangePicker, in addition to MinDate and MaxDate.
    /// </summary>
    public IEnumerable<DateTimeOffset>? DisabledDates { get; set; }

    /// <summary>
    /// The days of the week that are disabled (not selectable) in the DateRangePicker (e.g. weekends).
    /// </summary>
    public IEnumerable<DayOfWeek>? DisabledDaysOfWeek { get; set; }

    /// <summary>
    /// Determines the allowed drop directions of the callout.
    /// </summary>
    public BitDropDirection? DropDirection { get; set; }

    /// <summary>
    /// The icon to display inside the end time-picker's decrease-hour button.
    /// Takes precedence over <see cref="EndTimeDecreaseHourIconName"/> when both are set.
    /// </summary>
    public BitIconInfo? EndTimeDecreaseHourIcon { get; set; }

    /// <summary>
    /// The name of the end time-picker's decrease-hour button icon from the built-in Fluent UI icon set.
    /// </summary>
    public string? EndTimeDecreaseHourIconName { get; set; }

    /// <summary>
    /// The title and the aria-label of the end time-picker's decrease-hour button.
    /// </summary>
    public string? EndTimeDecreaseHourTitle { get; set; }

    /// <summary>
    /// The icon to display inside the end time-picker's decrease-minute button.
    /// Takes precedence over <see cref="EndTimeDecreaseMinuteIconName"/> when both are set.
    /// </summary>
    public BitIconInfo? EndTimeDecreaseMinuteIcon { get; set; }

    /// <summary>
    /// The name of the end time-picker's decrease-minute button icon from the built-in Fluent UI icon set.
    /// </summary>
    public string? EndTimeDecreaseMinuteIconName { get; set; }

    /// <summary>
    /// The title and the aria-label of the end time-picker's decrease-minute button.
    /// </summary>
    public string? EndTimeDecreaseMinuteTitle { get; set; }

    /// <summary>
    /// The aria-label of the end time-picker's hour input.
    /// </summary>
    public string? EndTimeHourInputAriaLabel { get; set; }

    /// <summary>
    /// The icon to display inside the end time-picker's increase-hour button.
    /// Takes precedence over <see cref="EndTimeIncreaseHourIconName"/> when both are set.
    /// </summary>
    public BitIconInfo? EndTimeIncreaseHourIcon { get; set; }

    /// <summary>
    /// The name of the end time-picker's increase-hour button icon from the built-in Fluent UI icon set.
    /// </summary>
    public string? EndTimeIncreaseHourIconName { get; set; }

    /// <summary>
    /// The title and the aria-label of the end time-picker's increase-hour button.
    /// </summary>
    public string? EndTimeIncreaseHourTitle { get; set; }

    /// <summary>
    /// The icon to display inside the end time-picker's increase-minute button.
    /// Takes precedence over <see cref="EndTimeIncreaseMinuteIconName"/> when both are set.
    /// </summary>
    public BitIconInfo? EndTimeIncreaseMinuteIcon { get; set; }

    /// <summary>
    /// The name of the end time-picker's increase-minute button icon from the built-in Fluent UI icon set.
    /// </summary>
    public string? EndTimeIncreaseMinuteIconName { get; set; }

    /// <summary>
    /// The title and the aria-label of the end time-picker's increase-minute button.
    /// </summary>
    public string? EndTimeIncreaseMinuteTitle { get; set; }

    /// <summary>
    /// The aria-label of the end time-picker's minute input.
    /// </summary>
    public string? EndTimeMinuteInputAriaLabel { get; set; }

    /// <summary>
    /// Whether the disabled days are excluded from the selected range. By default a range simply spans over
    /// the disabled days between its two ends. When enabled, once the start date is picked every day whose
    /// range would contain a disabled day becomes unselectable, so the produced range never covers one.
    /// </summary>
    public bool? ExcludeDisabledDates { get; set; }

    /// <summary>
    /// Overrides the first day of the week in the day picker. If not set, the first day of the week of the Culture is used.
    /// </summary>
    public DayOfWeek? FirstDayOfWeek { get; set; }

    /// <summary>
    /// Whether the day picker should always render six weeks, filling the extra rows with the days of the adjacent months,
    /// to keep the calendar height fixed while navigating between months. It is always on when <see cref="MonthCount"/>
    /// renders more than one month, so the months keep an even height next to each other.
    /// </summary>
    public bool? FixedWeeks { get; set; }

    /// <summary>
    /// Custom function to provide additional CSS classes for each day button of the DateRangePicker.
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
    /// Determines if the DateRangePicker has a border.
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
    /// Whether the month picker should highlight the selected month.
    /// </summary>
    public bool? HighlightSelectedMonth { get; set; }

    /// <summary>
    /// Whether the day picker should highlight today's day. It only affects the visual style of the
    /// day cell; the accessibility attributes still report the day as the current date.
    /// </summary>
    public bool? HighlightToday { get; set; }

    /// <summary>
    /// The list of dates that are highlighted (marked) in the day picker of the DateRangePicker.
    /// </summary>
    public IEnumerable<DateTimeOffset>? HighlightedDates { get; set; }

    /// <summary>
    /// The step, in hours, the spin buttons of the time picker move the hour by.
    /// </summary>
    /// <remarks>
    /// A step greater than 1 lays a grid over the day that every hour the buttons produce sits on, starting at
    /// midnight, so a picker that only accepts times on a three-hour grid can say so. A time entered as text is
    /// not held to it. Values below 1 are treated as 1.
    /// </remarks>
    public int? HourStep { get; set; }

    /// <summary>
    /// Gets or sets the icon to display using custom CSS classes for external icon libraries.
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
    /// Determines the location of the DateRangePicker's icon.
    /// </summary>
    public BitIconLocation? IconLocation { get; set; }

    /// <summary>
    /// Gets or sets the name of the icon to display from the built-in Fluent UI icons.
    /// </summary>
    /// <remarks>
    /// The icon name should be from the Fluent UI icon set (e.g., <c>BitIconName.CalendarMirrored</c>).
    /// <br />
    /// Browse available names in <c>BitIconName</c> of the <c>Bit.BlazorUI.Icons</c> nuget package or the gallery:
    /// <see href="https://blazorui.bitplatform.dev/iconography"/>.
    /// <br />
    /// For external icon libraries, use <see cref="Icon"/> instead.
    /// </remarks>
    public string? IconName { get; set; }

    /// <summary>
    /// Custom template for the DateRangePicker's icon.
    /// </summary>
    public RenderFragment? IconTemplate { get; set; }

    /// <summary>
    /// The custom validation error message for the invalid value.
    /// </summary>
    public string? InvalidErrorMessage { get; set; }

    /// <summary>
    /// Custom function to determine if a specific date is disabled (not selectable) in the DateRangePicker.
    /// </summary>
    public Func<DateTimeOffset, bool>? IsDateDisabled { get; set; }

    /// <summary>
    /// Whether the month picker is shown or hidden.
    /// </summary>
    public bool? IsMonthPickerVisible { get; set; }

    /// <summary>
    /// The text of the DateRangePicker's label.
    /// </summary>
    public string? Label { get; set; }

    /// <summary>
    /// Custom template for the DateRangePicker's label.
    /// </summary>
    public RenderFragment? LabelTemplate { get; set; }

    /// <summary>
    /// The maximum date allowed for the DateRangePicker.
    /// </summary>
    public DateTimeOffset? MaxDate { get; set; }

    /// <summary>
    /// The maximum range of day and times allowed for selection in DateRangePicker.
    /// </summary>
    public TimeSpan? MaxRange { get; set; }

    /// <summary>
    /// The minimum date allowed for the DateRangePicker.
    /// </summary>
    public DateTimeOffset? MinDate { get; set; }

    /// <summary>
    /// The minimum number of days that the selected range must span in the DateRangePicker.
    /// Only the days part of the provided TimeSpan is considered.
    /// </summary>
    public TimeSpan? MinRange { get; set; }

    /// <summary>
    /// The step, in minutes, the spin buttons of the time picker move the minute by.
    /// </summary>
    /// <remarks>
    /// A step greater than 1 lays a grid over the hour that every minute the buttons produce sits on, starting
    /// at the top of the hour, which is what turns it into a five-minute or quarter-hour picker. A time entered
    /// as text is not held to it. Values below 1 are treated as 1.
    /// </remarks>
    public int? MinuteStep { get; set; }

    /// <summary>
    /// Custom template to render the month cells of the DateRangePicker.
    /// </summary>
    public RenderFragment<DateTimeOffset>? MonthCellTemplate { get; set; }

    /// <summary>
    /// The number of consecutive months rendered side by side in the day picker (1 to 3), which makes
    /// picking a range that spans two months a single move. It falls back to a single month whenever
    /// the viewport is not wide enough to fit them all.
    /// </summary>
    public int? MonthCount { get; set; }

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
    /// The text rendered in place of a date that has not been picked yet, which is also the token
    /// accepted back for an open-ended range when <see cref="AllowTextInput"/> is enabled.
    /// </summary>
    public string? NoDateText { get; set; }

    /// <summary>
    /// The custom validation error message for a range entered as text that breaks the bounds, the
    /// disabled days, or <see cref="MinRange"/> and <see cref="MaxRange"/>.
    /// </summary>
    public string? OutOfRangeErrorMessage { get; set; }

    /// <summary>
    /// Whether the previous and next navigation buttons move the calendar by all of its rendered months
    /// instead of one, so consecutive pages of a multi-month calendar never overlap.
    /// It has no effect when <see cref="MonthCount"/> renders a single month.
    /// </summary>
    public bool? PagedNavigation { get; set; }

    /// <summary>
    /// The placeholder text of the DateRangePicker's input.
    /// </summary>
    public string? Placeholder { get; set; }

    /// <summary>
    /// The list of shortcuts, rendered next to the calendar, that fill the DateRangePicker
    /// with a predefined range (e.g. "Last 7 days").
    /// </summary>
    public IEnumerable<BitDateRangePickerPreset>? Presets { get; set; }

    /// <summary>
    /// The aria label of the presets' container for screen readers.
    /// </summary>
    public string? PresetsAriaLabel { get; set; }

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
    /// The aria-atomic live text announcing the currently selected date range, formatted with the value of the input.
    /// </summary>
    public string? SelectedDateAriaAtomic { get; set; }

    /// <summary>
    /// Whether the clear button should be shown or not when the DateRangePicker has a value.
    /// </summary>
    public bool? ShowClearButton { get; set; }

    /// <summary>
    /// Whether the DateRangePicker's close button should be shown or not.
    /// </summary>
    public bool? ShowCloseButton { get; set; }

    /// <summary>
    /// Whether the GoToToday button should be shown or not.
    /// </summary>
    public bool? ShowGoToToday { get; set; }

    /// <summary>
    /// Show month picker on top of date range picker when visible.
    /// </summary>
    public bool? ShowMonthPickerAsOverlay { get; set; }

    /// <summary>
    /// Whether the days of the previous and next months, filling the first and last week rows, should be rendered.
    /// It has no effect when <see cref="MonthCount"/> renders more than one month, since those days would then
    /// show up in two grids at once.
    /// </summary>
    public bool? ShowOutsideDays { get; set; }

    /// <summary>
    /// Whether or not render the time-picker.
    /// </summary>
    public bool? ShowTimePicker { get; set; }

    /// <summary>
    /// Show the time picker as an overlay on top of the date range picker when visible.
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
    /// Sets the preset size (Small, Medium, Large) of the field, the calendar cells and the label.
    /// </summary>
    public BitSize? Size { get; set; }

    /// <summary>
    /// Whether the DateRangePicker is rendered standalone or with the input component and callout.
    /// </summary>
    public bool? Standalone { get; set; }

    /// <summary>
    /// The icon to display inside the start time-picker's decrease-hour button.
    /// Takes precedence over <see cref="StartTimeDecreaseHourIconName"/> when both are set.
    /// </summary>
    public BitIconInfo? StartTimeDecreaseHourIcon { get; set; }

    /// <summary>
    /// The name of the start time-picker's decrease-hour button icon from the built-in Fluent UI icon set.
    /// </summary>
    public string? StartTimeDecreaseHourIconName { get; set; }

    /// <summary>
    /// The title and the aria-label of the start time-picker's decrease-hour button.
    /// </summary>
    public string? StartTimeDecreaseHourTitle { get; set; }

    /// <summary>
    /// The icon to display inside the start time-picker's decrease-minute button.
    /// Takes precedence over <see cref="StartTimeDecreaseMinuteIconName"/> when both are set.
    /// </summary>
    public BitIconInfo? StartTimeDecreaseMinuteIcon { get; set; }

    /// <summary>
    /// The name of the start time-picker's decrease-minute button icon from the built-in Fluent UI icon set.
    /// </summary>
    public string? StartTimeDecreaseMinuteIconName { get; set; }

    /// <summary>
    /// The title and the aria-label of the start time-picker's decrease-minute button.
    /// </summary>
    public string? StartTimeDecreaseMinuteTitle { get; set; }

    /// <summary>
    /// The aria-label of the start time-picker's hour input.
    /// </summary>
    public string? StartTimeHourInputAriaLabel { get; set; }

    /// <summary>
    /// The icon to display inside the start time-picker's increase-hour button.
    /// Takes precedence over <see cref="StartTimeIncreaseHourIconName"/> when both are set.
    /// </summary>
    public BitIconInfo? StartTimeIncreaseHourIcon { get; set; }

    /// <summary>
    /// The name of the start time-picker's increase-hour button icon from the built-in Fluent UI icon set.
    /// </summary>
    public string? StartTimeIncreaseHourIconName { get; set; }

    /// <summary>
    /// The title and the aria-label of the start time-picker's increase-hour button.
    /// </summary>
    public string? StartTimeIncreaseHourTitle { get; set; }

    /// <summary>
    /// The icon to display inside the start time-picker's increase-minute button.
    /// Takes precedence over <see cref="StartTimeIncreaseMinuteIconName"/> when both are set.
    /// </summary>
    public BitIconInfo? StartTimeIncreaseMinuteIcon { get; set; }

    /// <summary>
    /// The name of the start time-picker's increase-minute button icon from the built-in Fluent UI icon set.
    /// </summary>
    public string? StartTimeIncreaseMinuteIconName { get; set; }

    /// <summary>
    /// The title and the aria-label of the start time-picker's increase-minute button.
    /// </summary>
    public string? StartTimeIncreaseMinuteTitle { get; set; }

    /// <summary>
    /// The aria-label of the start time-picker's minute input.
    /// </summary>
    public string? StartTimeMinuteInputAriaLabel { get; set; }

    /// <summary>
    /// Specifies the date and time of the date and time picker when it is opened without any selected value.
    /// </summary>
    public BitDateRangePickerValue? StartingValue { get; set; }

    /// <summary>
    /// Custom CSS styles for different parts of the BitDateRangePicker component.
    /// </summary>
    public BitDateRangePickerClassStyles? Styles { get; set; }

    /// <summary>
    /// Time format of the time-pickers, 24H or 12H.
    /// </summary>
    public BitTimeFormat? TimeFormat { get; set; }

    /// <summary>
    /// TimeZone for the DateRangePicker.
    /// </summary>
    public TimeZoneInfo? TimeZone { get; set; }

    /// <summary>
    /// Overrides the date considered as today by the DateRangePicker, which is <c>DateTimeOffset.Now</c> by default.
    /// </summary>
    public DateTimeOffset? Today { get; set; }

    /// <summary>
    /// Whether or not the Text field of the DateRangePicker is underlined.
    /// </summary>
    public bool? Underlined { get; set; }

    /// <summary>
    /// The string format used to show the DateRangePicker's value in its input.
    /// </summary>
    public string? ValueFormat { get; set; }

    /// <summary>
    /// The rule used to calculate the week numbers. If not set, <c>CalendarWeekRule.FirstFullWeek</c> is used.
    /// </summary>
    public CalendarWeekRule? WeekNumberRule { get; set; }

    /// <summary>
    /// The title of the week number (tooltip).
    /// </summary>
    public string? WeekNumberTitle { get; set; }

    /// <summary>
    /// The accessible name of the empty header cell above the week numbers column.
    /// </summary>
    public string? WeekNumbersHeaderTitle { get; set; }

    /// <summary>
    /// Custom template to render the year cells of the DateRangePicker.
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
    /// Updates the properties of the specified <see cref="BitDateRangePicker"/> instance with any values that have been set on
    /// this object, if those properties have not already been set on the <see cref="BitDateRangePicker"/>.
    /// </summary>
    /// <remarks>
    /// Only properties that have a value set and have not already been set on the <paramref name="bitDateRangePicker"/> will be updated.
    /// This method does not overwrite existing values on <paramref name="bitDateRangePicker"/>.
    /// </remarks>
    /// <param name="bitDateRangePicker">
    /// The <see cref="BitDateRangePicker"/> instance whose properties will be updated. Cannot be null.
    /// </param>
    public void UpdateParameters(BitDateRangePicker bitDateRangePicker)
    {
        if (bitDateRangePicker is null) return;

        UpdateBaseParameters(bitDateRangePicker);

        // The parameters the picker rebuilds its view from are the ones carrying a [CallOnSet(OnSetParameters)]
        // on the component, and the component has already run that pass in OnInitialized - before anything
        // cascaded here reached it. So whichever of them the cascade fills in, the pass is run once more at the end.
        // Only a value that actually differs asks for it: this runs on every OnParametersSet, and the pass rebases
        // the calendar on the selected month, so rebuilding on an unchanged cascade would slide an open calendar
        // back off whatever month the user had navigated to.
        var rebuildView = false;

        if (AllowTextInput.HasValue && bitDateRangePicker.HasNotBeenSet(nameof(AllowTextInput)))
        {
            bitDateRangePicker.AllowTextInput = AllowTextInput.Value;
        }

        if (ApplyButtonText.HasValue() && bitDateRangePicker.HasNotBeenSet(nameof(ApplyButtonText)))
        {
            bitDateRangePicker.ApplyButtonText = ApplyButtonText!;
        }

        if (AutoApply.HasValue && bitDateRangePicker.HasNotBeenSet(nameof(AutoApply)))
        {
            bitDateRangePicker.AutoApply = AutoApply.Value;
        }

        if (AutoClose.HasValue && bitDateRangePicker.HasNotBeenSet(nameof(AutoClose)))
        {
            bitDateRangePicker.AutoClose = AutoClose.Value;
        }

        if (AutoFocus.HasValue && bitDateRangePicker.HasNotBeenSet(nameof(AutoFocus)))
        {
            bitDateRangePicker.AutoFocus = AutoFocus.Value;
        }

        if (CalloutAriaLabel.HasValue() && bitDateRangePicker.HasNotBeenSet(nameof(CalloutAriaLabel)))
        {
            bitDateRangePicker.CalloutAriaLabel = CalloutAriaLabel!;
        }

        if (CalloutFooterTemplate is not null && bitDateRangePicker.HasNotBeenSet(nameof(CalloutFooterTemplate)))
        {
            bitDateRangePicker.CalloutFooterTemplate = CalloutFooterTemplate;
        }

        if (CalloutHeaderTemplate is not null && bitDateRangePicker.HasNotBeenSet(nameof(CalloutHeaderTemplate)))
        {
            bitDateRangePicker.CalloutHeaderTemplate = CalloutHeaderTemplate;
        }

        if (CalloutHtmlAttributes is not null && bitDateRangePicker.HasNotBeenSet(nameof(CalloutHtmlAttributes)))
        {
            bitDateRangePicker.CalloutHtmlAttributes = CalloutHtmlAttributes;
        }

        if (CancelButtonText.HasValue() && bitDateRangePicker.HasNotBeenSet(nameof(CancelButtonText)))
        {
            bitDateRangePicker.CancelButtonText = CancelButtonText!;
        }

        if (Classes is not null && bitDateRangePicker.HasNotBeenSet(nameof(Classes)))
        {
            bitDateRangePicker.Classes = Classes;

            bitDateRangePicker.ClassBuilder.Reset();
        }

        if (ClearButtonIcon is not null && bitDateRangePicker.HasNotBeenSet(nameof(ClearButtonIcon)))
        {
            bitDateRangePicker.ClearButtonIcon = ClearButtonIcon;
        }

        if (ClearButtonIconName.HasValue() && bitDateRangePicker.HasNotBeenSet(nameof(ClearButtonIconName)))
        {
            bitDateRangePicker.ClearButtonIconName = ClearButtonIconName;
        }

        if (ClearButtonTitle.HasValue() && bitDateRangePicker.HasNotBeenSet(nameof(ClearButtonTitle)))
        {
            bitDateRangePicker.ClearButtonTitle = ClearButtonTitle!;
        }

        if (CloseButtonIcon is not null && bitDateRangePicker.HasNotBeenSet(nameof(CloseButtonIcon)))
        {
            bitDateRangePicker.CloseButtonIcon = CloseButtonIcon;
        }

        if (CloseButtonIconName.HasValue() && bitDateRangePicker.HasNotBeenSet(nameof(CloseButtonIconName)))
        {
            bitDateRangePicker.CloseButtonIconName = CloseButtonIconName;
        }

        if (CloseButtonTitle.HasValue() && bitDateRangePicker.HasNotBeenSet(nameof(CloseButtonTitle)))
        {
            bitDateRangePicker.CloseButtonTitle = CloseButtonTitle!;
        }

        if (Color.HasValue && bitDateRangePicker.HasNotBeenSet(nameof(Color)))
        {
            bitDateRangePicker.Color = Color;

            bitDateRangePicker.ClassBuilder.Reset();
        }

        if (ContinuousSpinDelay.HasValue && bitDateRangePicker.HasNotBeenSet(nameof(ContinuousSpinDelay)))
        {
            bitDateRangePicker.ContinuousSpinDelay = ContinuousSpinDelay.Value;
        }

        if (ContinuousSpinInterval.HasValue && bitDateRangePicker.HasNotBeenSet(nameof(ContinuousSpinInterval)))
        {
            bitDateRangePicker.ContinuousSpinInterval = ContinuousSpinInterval.Value;
        }

        if (Culture is not null && bitDateRangePicker.HasNotBeenSet(nameof(Culture)))
        {
            rebuildView = rebuildView || ReferenceEquals(bitDateRangePicker.Culture, Culture) is false;

            bitDateRangePicker.Culture = Culture;

            bitDateRangePicker.ClassBuilder.Reset();
        }

        if (DateFormat.HasValue() && bitDateRangePicker.HasNotBeenSet(nameof(DateFormat)))
        {
            bitDateRangePicker.DateFormat = DateFormat;
        }

        if (DayCellTemplate is not null && bitDateRangePicker.HasNotBeenSet(nameof(DayCellTemplate)))
        {
            bitDateRangePicker.DayCellTemplate = DayCellTemplate;
        }

        if (DisableFuture.HasValue && bitDateRangePicker.HasNotBeenSet(nameof(DisableFuture)))
        {
            rebuildView = rebuildView || bitDateRangePicker.DisableFuture != DisableFuture.Value;

            bitDateRangePicker.DisableFuture = DisableFuture.Value;
        }

        if (DisablePast.HasValue && bitDateRangePicker.HasNotBeenSet(nameof(DisablePast)))
        {
            rebuildView = rebuildView || bitDateRangePicker.DisablePast != DisablePast.Value;

            bitDateRangePicker.DisablePast = DisablePast.Value;
        }

        if (DisabledDateErrorMessage.HasValue() && bitDateRangePicker.HasNotBeenSet(nameof(DisabledDateErrorMessage)))
        {
            bitDateRangePicker.DisabledDateErrorMessage = DisabledDateErrorMessage;
        }

        if (DisabledDates is not null && bitDateRangePicker.HasNotBeenSet(nameof(DisabledDates)))
        {
            rebuildView = rebuildView || ReferenceEquals(bitDateRangePicker.DisabledDates, DisabledDates) is false;

            bitDateRangePicker.DisabledDates = DisabledDates;
        }

        if (DisabledDaysOfWeek is not null && bitDateRangePicker.HasNotBeenSet(nameof(DisabledDaysOfWeek)))
        {
            rebuildView = rebuildView || ReferenceEquals(bitDateRangePicker.DisabledDaysOfWeek, DisabledDaysOfWeek) is false;

            bitDateRangePicker.DisabledDaysOfWeek = DisabledDaysOfWeek;
        }

        if (DropDirection.HasValue && bitDateRangePicker.HasNotBeenSet(nameof(DropDirection)))
        {
            bitDateRangePicker.DropDirection = DropDirection.Value;
        }

        if (EndTimeDecreaseHourIcon is not null && bitDateRangePicker.HasNotBeenSet(nameof(EndTimeDecreaseHourIcon)))
        {
            bitDateRangePicker.EndTimeDecreaseHourIcon = EndTimeDecreaseHourIcon;
        }

        if (EndTimeDecreaseHourIconName.HasValue() && bitDateRangePicker.HasNotBeenSet(nameof(EndTimeDecreaseHourIconName)))
        {
            bitDateRangePicker.EndTimeDecreaseHourIconName = EndTimeDecreaseHourIconName;
        }

        if (EndTimeDecreaseHourTitle.HasValue() && bitDateRangePicker.HasNotBeenSet(nameof(EndTimeDecreaseHourTitle)))
        {
            bitDateRangePicker.EndTimeDecreaseHourTitle = EndTimeDecreaseHourTitle!;
        }

        if (EndTimeDecreaseMinuteIcon is not null && bitDateRangePicker.HasNotBeenSet(nameof(EndTimeDecreaseMinuteIcon)))
        {
            bitDateRangePicker.EndTimeDecreaseMinuteIcon = EndTimeDecreaseMinuteIcon;
        }

        if (EndTimeDecreaseMinuteIconName.HasValue() && bitDateRangePicker.HasNotBeenSet(nameof(EndTimeDecreaseMinuteIconName)))
        {
            bitDateRangePicker.EndTimeDecreaseMinuteIconName = EndTimeDecreaseMinuteIconName;
        }

        if (EndTimeDecreaseMinuteTitle.HasValue() && bitDateRangePicker.HasNotBeenSet(nameof(EndTimeDecreaseMinuteTitle)))
        {
            bitDateRangePicker.EndTimeDecreaseMinuteTitle = EndTimeDecreaseMinuteTitle!;
        }

        if (EndTimeHourInputAriaLabel.HasValue() && bitDateRangePicker.HasNotBeenSet(nameof(EndTimeHourInputAriaLabel)))
        {
            bitDateRangePicker.EndTimeHourInputAriaLabel = EndTimeHourInputAriaLabel!;
        }

        if (EndTimeIncreaseHourIcon is not null && bitDateRangePicker.HasNotBeenSet(nameof(EndTimeIncreaseHourIcon)))
        {
            bitDateRangePicker.EndTimeIncreaseHourIcon = EndTimeIncreaseHourIcon;
        }

        if (EndTimeIncreaseHourIconName.HasValue() && bitDateRangePicker.HasNotBeenSet(nameof(EndTimeIncreaseHourIconName)))
        {
            bitDateRangePicker.EndTimeIncreaseHourIconName = EndTimeIncreaseHourIconName;
        }

        if (EndTimeIncreaseHourTitle.HasValue() && bitDateRangePicker.HasNotBeenSet(nameof(EndTimeIncreaseHourTitle)))
        {
            bitDateRangePicker.EndTimeIncreaseHourTitle = EndTimeIncreaseHourTitle!;
        }

        if (EndTimeIncreaseMinuteIcon is not null && bitDateRangePicker.HasNotBeenSet(nameof(EndTimeIncreaseMinuteIcon)))
        {
            bitDateRangePicker.EndTimeIncreaseMinuteIcon = EndTimeIncreaseMinuteIcon;
        }

        if (EndTimeIncreaseMinuteIconName.HasValue() && bitDateRangePicker.HasNotBeenSet(nameof(EndTimeIncreaseMinuteIconName)))
        {
            bitDateRangePicker.EndTimeIncreaseMinuteIconName = EndTimeIncreaseMinuteIconName;
        }

        if (EndTimeIncreaseMinuteTitle.HasValue() && bitDateRangePicker.HasNotBeenSet(nameof(EndTimeIncreaseMinuteTitle)))
        {
            bitDateRangePicker.EndTimeIncreaseMinuteTitle = EndTimeIncreaseMinuteTitle!;
        }

        if (EndTimeMinuteInputAriaLabel.HasValue() && bitDateRangePicker.HasNotBeenSet(nameof(EndTimeMinuteInputAriaLabel)))
        {
            bitDateRangePicker.EndTimeMinuteInputAriaLabel = EndTimeMinuteInputAriaLabel!;
        }

        if (ExcludeDisabledDates.HasValue && bitDateRangePicker.HasNotBeenSet(nameof(ExcludeDisabledDates)))
        {
            rebuildView = rebuildView || bitDateRangePicker.ExcludeDisabledDates != ExcludeDisabledDates.Value;

            bitDateRangePicker.ExcludeDisabledDates = ExcludeDisabledDates.Value;
        }

        if (FirstDayOfWeek.HasValue && bitDateRangePicker.HasNotBeenSet(nameof(FirstDayOfWeek)))
        {
            rebuildView = rebuildView || bitDateRangePicker.FirstDayOfWeek != FirstDayOfWeek;

            bitDateRangePicker.FirstDayOfWeek = FirstDayOfWeek;
        }

        if (FixedWeeks.HasValue && bitDateRangePicker.HasNotBeenSet(nameof(FixedWeeks)))
        {
            rebuildView = rebuildView || bitDateRangePicker.FixedWeeks != FixedWeeks.Value;

            bitDateRangePicker.FixedWeeks = FixedWeeks.Value;
        }

        if (GetDayClass is not null && bitDateRangePicker.HasNotBeenSet(nameof(GetDayClass)))
        {
            bitDateRangePicker.GetDayClass = GetDayClass;
        }

        if (GoToNextMonthTitle.HasValue() && bitDateRangePicker.HasNotBeenSet(nameof(GoToNextMonthTitle)))
        {
            bitDateRangePicker.GoToNextMonthTitle = GoToNextMonthTitle!;
        }

        if (GoToNextYearRangeTitle.HasValue() && bitDateRangePicker.HasNotBeenSet(nameof(GoToNextYearRangeTitle)))
        {
            bitDateRangePicker.GoToNextYearRangeTitle = GoToNextYearRangeTitle!;
        }

        if (GoToNextYearTitle.HasValue() && bitDateRangePicker.HasNotBeenSet(nameof(GoToNextYearTitle)))
        {
            bitDateRangePicker.GoToNextYearTitle = GoToNextYearTitle!;
        }

        if (GoToPrevMonthTitle.HasValue() && bitDateRangePicker.HasNotBeenSet(nameof(GoToPrevMonthTitle)))
        {
            bitDateRangePicker.GoToPrevMonthTitle = GoToPrevMonthTitle!;
        }

        if (GoToPrevYearRangeTitle.HasValue() && bitDateRangePicker.HasNotBeenSet(nameof(GoToPrevYearRangeTitle)))
        {
            bitDateRangePicker.GoToPrevYearRangeTitle = GoToPrevYearRangeTitle!;
        }

        if (GoToPrevYearTitle.HasValue() && bitDateRangePicker.HasNotBeenSet(nameof(GoToPrevYearTitle)))
        {
            bitDateRangePicker.GoToPrevYearTitle = GoToPrevYearTitle!;
        }

        if (GoToTodayIcon is not null && bitDateRangePicker.HasNotBeenSet(nameof(GoToTodayIcon)))
        {
            bitDateRangePicker.GoToTodayIcon = GoToTodayIcon;
        }

        if (GoToTodayIconName.HasValue() && bitDateRangePicker.HasNotBeenSet(nameof(GoToTodayIconName)))
        {
            bitDateRangePicker.GoToTodayIconName = GoToTodayIconName;
        }

        if (GoToTodayTitle.HasValue() && bitDateRangePicker.HasNotBeenSet(nameof(GoToTodayTitle)))
        {
            bitDateRangePicker.GoToTodayTitle = GoToTodayTitle!;
        }

        if (HasBorder.HasValue && bitDateRangePicker.HasNotBeenSet(nameof(HasBorder)))
        {
            bitDateRangePicker.HasBorder = HasBorder.Value;

            bitDateRangePicker.ClassBuilder.Reset();
        }

        if (HideTimePickerIcon is not null && bitDateRangePicker.HasNotBeenSet(nameof(HideTimePickerIcon)))
        {
            bitDateRangePicker.HideTimePickerIcon = HideTimePickerIcon;
        }

        if (HideTimePickerIconName.HasValue() && bitDateRangePicker.HasNotBeenSet(nameof(HideTimePickerIconName)))
        {
            bitDateRangePicker.HideTimePickerIconName = HideTimePickerIconName;
        }

        if (HideTimePickerTitle.HasValue() && bitDateRangePicker.HasNotBeenSet(nameof(HideTimePickerTitle)))
        {
            bitDateRangePicker.HideTimePickerTitle = HideTimePickerTitle!;
        }

        if (HighlightCurrentMonth.HasValue && bitDateRangePicker.HasNotBeenSet(nameof(HighlightCurrentMonth)))
        {
            bitDateRangePicker.HighlightCurrentMonth = HighlightCurrentMonth.Value;
        }

        if (HighlightSelectedMonth.HasValue && bitDateRangePicker.HasNotBeenSet(nameof(HighlightSelectedMonth)))
        {
            bitDateRangePicker.HighlightSelectedMonth = HighlightSelectedMonth.Value;
        }

        if (HighlightToday.HasValue && bitDateRangePicker.HasNotBeenSet(nameof(HighlightToday)))
        {
            bitDateRangePicker.HighlightToday = HighlightToday.Value;
        }

        if (HighlightedDates is not null && bitDateRangePicker.HasNotBeenSet(nameof(HighlightedDates)))
        {
            rebuildView = rebuildView || ReferenceEquals(bitDateRangePicker.HighlightedDates, HighlightedDates) is false;

            bitDateRangePicker.HighlightedDates = HighlightedDates;
        }

        if (HourStep.HasValue && bitDateRangePicker.HasNotBeenSet(nameof(HourStep)))
        {
            bitDateRangePicker.HourStep = HourStep.Value;
        }

        if (Icon is not null && bitDateRangePicker.HasNotBeenSet(nameof(Icon)))
        {
            bitDateRangePicker.Icon = Icon;
        }

        if (IconLocation.HasValue && bitDateRangePicker.HasNotBeenSet(nameof(IconLocation)))
        {
            bitDateRangePicker.IconLocation = IconLocation.Value;

            bitDateRangePicker.ClassBuilder.Reset();
        }

        if (IconName.HasValue() && bitDateRangePicker.HasNotBeenSet(nameof(IconName)))
        {
            bitDateRangePicker.IconName = IconName;
        }

        if (IconTemplate is not null && bitDateRangePicker.HasNotBeenSet(nameof(IconTemplate)))
        {
            bitDateRangePicker.IconTemplate = IconTemplate;
        }

        if (InvalidErrorMessage.HasValue() && bitDateRangePicker.HasNotBeenSet(nameof(InvalidErrorMessage)))
        {
            bitDateRangePicker.InvalidErrorMessage = InvalidErrorMessage;
        }

        if (IsDateDisabled is not null && bitDateRangePicker.HasNotBeenSet(nameof(IsDateDisabled)))
        {
            rebuildView = rebuildView || ReferenceEquals(bitDateRangePicker.IsDateDisabled, IsDateDisabled) is false;

            bitDateRangePicker.IsDateDisabled = IsDateDisabled;
        }

        if (IsMonthPickerVisible.HasValue && bitDateRangePicker.HasNotBeenSet(nameof(IsMonthPickerVisible)))
        {
            rebuildView = rebuildView || bitDateRangePicker.IsMonthPickerVisible != IsMonthPickerVisible.Value;

            bitDateRangePicker.IsMonthPickerVisible = IsMonthPickerVisible.Value;
        }

        if (Label.HasValue() && bitDateRangePicker.HasNotBeenSet(nameof(Label)))
        {
            bitDateRangePicker.Label = Label;
        }

        if (LabelTemplate is not null && bitDateRangePicker.HasNotBeenSet(nameof(LabelTemplate)))
        {
            bitDateRangePicker.LabelTemplate = LabelTemplate;
        }

        if (MaxDate.HasValue && bitDateRangePicker.HasNotBeenSet(nameof(MaxDate)))
        {
            rebuildView = rebuildView || bitDateRangePicker.MaxDate != MaxDate;

            bitDateRangePicker.MaxDate = MaxDate;
        }

        if (MaxRange.HasValue && bitDateRangePicker.HasNotBeenSet(nameof(MaxRange)))
        {
            bitDateRangePicker.MaxRange = MaxRange;
        }

        if (MinDate.HasValue && bitDateRangePicker.HasNotBeenSet(nameof(MinDate)))
        {
            rebuildView = rebuildView || bitDateRangePicker.MinDate != MinDate;

            bitDateRangePicker.MinDate = MinDate;
        }

        if (MinRange.HasValue && bitDateRangePicker.HasNotBeenSet(nameof(MinRange)))
        {
            bitDateRangePicker.MinRange = MinRange;
        }

        if (MinuteStep.HasValue && bitDateRangePicker.HasNotBeenSet(nameof(MinuteStep)))
        {
            bitDateRangePicker.MinuteStep = MinuteStep.Value;
        }

        if (MonthCellTemplate is not null && bitDateRangePicker.HasNotBeenSet(nameof(MonthCellTemplate)))
        {
            bitDateRangePicker.MonthCellTemplate = MonthCellTemplate;
        }

        if (MonthCount.HasValue && bitDateRangePicker.HasNotBeenSet(nameof(MonthCount)))
        {
            rebuildView = rebuildView || bitDateRangePicker.MonthCount != MonthCount.Value;

            bitDateRangePicker.MonthCount = MonthCount.Value;
        }

        if (MonthPickerToggleTitle.HasValue() && bitDateRangePicker.HasNotBeenSet(nameof(MonthPickerToggleTitle)))
        {
            bitDateRangePicker.MonthPickerToggleTitle = MonthPickerToggleTitle!;
        }

        if (NextMonthNavIcon is not null && bitDateRangePicker.HasNotBeenSet(nameof(NextMonthNavIcon)))
        {
            bitDateRangePicker.NextMonthNavIcon = NextMonthNavIcon;
        }

        if (NextMonthNavIconName.HasValue() && bitDateRangePicker.HasNotBeenSet(nameof(NextMonthNavIconName)))
        {
            bitDateRangePicker.NextMonthNavIconName = NextMonthNavIconName;
        }

        if (NextYearNavIcon is not null && bitDateRangePicker.HasNotBeenSet(nameof(NextYearNavIcon)))
        {
            bitDateRangePicker.NextYearNavIcon = NextYearNavIcon;
        }

        if (NextYearNavIconName.HasValue() && bitDateRangePicker.HasNotBeenSet(nameof(NextYearNavIconName)))
        {
            bitDateRangePicker.NextYearNavIconName = NextYearNavIconName;
        }

        if (NextYearRangeNavIcon is not null && bitDateRangePicker.HasNotBeenSet(nameof(NextYearRangeNavIcon)))
        {
            bitDateRangePicker.NextYearRangeNavIcon = NextYearRangeNavIcon;
        }

        if (NextYearRangeNavIconName.HasValue() && bitDateRangePicker.HasNotBeenSet(nameof(NextYearRangeNavIconName)))
        {
            bitDateRangePicker.NextYearRangeNavIconName = NextYearRangeNavIconName;
        }

        if (NoDateText.HasValue() && bitDateRangePicker.HasNotBeenSet(nameof(NoDateText)))
        {
            bitDateRangePicker.NoDateText = NoDateText!;
        }

        if (OutOfRangeErrorMessage.HasValue() && bitDateRangePicker.HasNotBeenSet(nameof(OutOfRangeErrorMessage)))
        {
            bitDateRangePicker.OutOfRangeErrorMessage = OutOfRangeErrorMessage;
        }

        if (PagedNavigation.HasValue && bitDateRangePicker.HasNotBeenSet(nameof(PagedNavigation)))
        {
            bitDateRangePicker.PagedNavigation = PagedNavigation.Value;
        }

        if (Placeholder.HasValue() && bitDateRangePicker.HasNotBeenSet(nameof(Placeholder)))
        {
            bitDateRangePicker.Placeholder = Placeholder!;
        }

        if (Presets is not null && bitDateRangePicker.HasNotBeenSet(nameof(Presets)))
        {
            bitDateRangePicker.Presets = Presets;
        }

        if (PresetsAriaLabel.HasValue() && bitDateRangePicker.HasNotBeenSet(nameof(PresetsAriaLabel)))
        {
            bitDateRangePicker.PresetsAriaLabel = PresetsAriaLabel!;
        }

        if (PrevMonthNavIcon is not null && bitDateRangePicker.HasNotBeenSet(nameof(PrevMonthNavIcon)))
        {
            bitDateRangePicker.PrevMonthNavIcon = PrevMonthNavIcon;
        }

        if (PrevMonthNavIconName.HasValue() && bitDateRangePicker.HasNotBeenSet(nameof(PrevMonthNavIconName)))
        {
            bitDateRangePicker.PrevMonthNavIconName = PrevMonthNavIconName;
        }

        if (PrevYearNavIcon is not null && bitDateRangePicker.HasNotBeenSet(nameof(PrevYearNavIcon)))
        {
            bitDateRangePicker.PrevYearNavIcon = PrevYearNavIcon;
        }

        if (PrevYearNavIconName.HasValue() && bitDateRangePicker.HasNotBeenSet(nameof(PrevYearNavIconName)))
        {
            bitDateRangePicker.PrevYearNavIconName = PrevYearNavIconName;
        }

        if (PrevYearRangeNavIcon is not null && bitDateRangePicker.HasNotBeenSet(nameof(PrevYearRangeNavIcon)))
        {
            bitDateRangePicker.PrevYearRangeNavIcon = PrevYearRangeNavIcon;
        }

        if (PrevYearRangeNavIconName.HasValue() && bitDateRangePicker.HasNotBeenSet(nameof(PrevYearRangeNavIconName)))
        {
            bitDateRangePicker.PrevYearRangeNavIconName = PrevYearRangeNavIconName;
        }

        if (Responsive.HasValue && bitDateRangePicker.HasNotBeenSet(nameof(Responsive)))
        {
            bitDateRangePicker.Responsive = Responsive.Value;
        }

        if (SelectedDateAriaAtomic.HasValue() && bitDateRangePicker.HasNotBeenSet(nameof(SelectedDateAriaAtomic)))
        {
            bitDateRangePicker.SelectedDateAriaAtomic = SelectedDateAriaAtomic!;
        }

        if (ShowClearButton.HasValue && bitDateRangePicker.HasNotBeenSet(nameof(ShowClearButton)))
        {
            bitDateRangePicker.ShowClearButton = ShowClearButton.Value;
        }

        if (ShowCloseButton.HasValue && bitDateRangePicker.HasNotBeenSet(nameof(ShowCloseButton)))
        {
            bitDateRangePicker.ShowCloseButton = ShowCloseButton.Value;
        }

        if (ShowGoToToday.HasValue && bitDateRangePicker.HasNotBeenSet(nameof(ShowGoToToday)))
        {
            bitDateRangePicker.ShowGoToToday = ShowGoToToday.Value;
        }

        if (ShowMonthPickerAsOverlay.HasValue && bitDateRangePicker.HasNotBeenSet(nameof(ShowMonthPickerAsOverlay)))
        {
            rebuildView = rebuildView || bitDateRangePicker.ShowMonthPickerAsOverlay != ShowMonthPickerAsOverlay.Value;

            bitDateRangePicker.ShowMonthPickerAsOverlay = ShowMonthPickerAsOverlay.Value;
        }

        if (ShowOutsideDays.HasValue && bitDateRangePicker.HasNotBeenSet(nameof(ShowOutsideDays)))
        {
            bitDateRangePicker.ShowOutsideDays = ShowOutsideDays.Value;
        }

        if (ShowTimePicker.HasValue && bitDateRangePicker.HasNotBeenSet(nameof(ShowTimePicker)))
        {
            rebuildView = rebuildView || bitDateRangePicker.ShowTimePicker != ShowTimePicker.Value;

            bitDateRangePicker.ShowTimePicker = ShowTimePicker.Value;
        }

        if (ShowTimePickerAsOverlay.HasValue && bitDateRangePicker.HasNotBeenSet(nameof(ShowTimePickerAsOverlay)))
        {
            rebuildView = rebuildView || bitDateRangePicker.ShowTimePickerAsOverlay != ShowTimePickerAsOverlay.Value;

            bitDateRangePicker.ShowTimePickerAsOverlay = ShowTimePickerAsOverlay.Value;
        }

        if (ShowTimePickerIcon is not null && bitDateRangePicker.HasNotBeenSet(nameof(ShowTimePickerIcon)))
        {
            bitDateRangePicker.ShowTimePickerIcon = ShowTimePickerIcon;
        }

        if (ShowTimePickerIconName.HasValue() && bitDateRangePicker.HasNotBeenSet(nameof(ShowTimePickerIconName)))
        {
            bitDateRangePicker.ShowTimePickerIconName = ShowTimePickerIconName;
        }

        if (ShowTimePickerTitle.HasValue() && bitDateRangePicker.HasNotBeenSet(nameof(ShowTimePickerTitle)))
        {
            bitDateRangePicker.ShowTimePickerTitle = ShowTimePickerTitle!;
        }

        if (ShowWeekNumbers.HasValue && bitDateRangePicker.HasNotBeenSet(nameof(ShowWeekNumbers)))
        {
            bitDateRangePicker.ShowWeekNumbers = ShowWeekNumbers.Value;
        }

        if (Size.HasValue && bitDateRangePicker.HasNotBeenSet(nameof(Size)))
        {
            bitDateRangePicker.Size = Size;

            bitDateRangePicker.ClassBuilder.Reset();
        }

        if (Standalone.HasValue && bitDateRangePicker.HasNotBeenSet(nameof(Standalone)))
        {
            rebuildView = rebuildView || bitDateRangePicker.Standalone != Standalone.Value;

            bitDateRangePicker.Standalone = Standalone.Value;

            bitDateRangePicker.ClassBuilder.Reset();
        }

        if (StartTimeDecreaseHourIcon is not null && bitDateRangePicker.HasNotBeenSet(nameof(StartTimeDecreaseHourIcon)))
        {
            bitDateRangePicker.StartTimeDecreaseHourIcon = StartTimeDecreaseHourIcon;
        }

        if (StartTimeDecreaseHourIconName.HasValue() && bitDateRangePicker.HasNotBeenSet(nameof(StartTimeDecreaseHourIconName)))
        {
            bitDateRangePicker.StartTimeDecreaseHourIconName = StartTimeDecreaseHourIconName;
        }

        if (StartTimeDecreaseHourTitle.HasValue() && bitDateRangePicker.HasNotBeenSet(nameof(StartTimeDecreaseHourTitle)))
        {
            bitDateRangePicker.StartTimeDecreaseHourTitle = StartTimeDecreaseHourTitle!;
        }

        if (StartTimeDecreaseMinuteIcon is not null && bitDateRangePicker.HasNotBeenSet(nameof(StartTimeDecreaseMinuteIcon)))
        {
            bitDateRangePicker.StartTimeDecreaseMinuteIcon = StartTimeDecreaseMinuteIcon;
        }

        if (StartTimeDecreaseMinuteIconName.HasValue() && bitDateRangePicker.HasNotBeenSet(nameof(StartTimeDecreaseMinuteIconName)))
        {
            bitDateRangePicker.StartTimeDecreaseMinuteIconName = StartTimeDecreaseMinuteIconName;
        }

        if (StartTimeDecreaseMinuteTitle.HasValue() && bitDateRangePicker.HasNotBeenSet(nameof(StartTimeDecreaseMinuteTitle)))
        {
            bitDateRangePicker.StartTimeDecreaseMinuteTitle = StartTimeDecreaseMinuteTitle!;
        }

        if (StartTimeHourInputAriaLabel.HasValue() && bitDateRangePicker.HasNotBeenSet(nameof(StartTimeHourInputAriaLabel)))
        {
            bitDateRangePicker.StartTimeHourInputAriaLabel = StartTimeHourInputAriaLabel!;
        }

        if (StartTimeIncreaseHourIcon is not null && bitDateRangePicker.HasNotBeenSet(nameof(StartTimeIncreaseHourIcon)))
        {
            bitDateRangePicker.StartTimeIncreaseHourIcon = StartTimeIncreaseHourIcon;
        }

        if (StartTimeIncreaseHourIconName.HasValue() && bitDateRangePicker.HasNotBeenSet(nameof(StartTimeIncreaseHourIconName)))
        {
            bitDateRangePicker.StartTimeIncreaseHourIconName = StartTimeIncreaseHourIconName;
        }

        if (StartTimeIncreaseHourTitle.HasValue() && bitDateRangePicker.HasNotBeenSet(nameof(StartTimeIncreaseHourTitle)))
        {
            bitDateRangePicker.StartTimeIncreaseHourTitle = StartTimeIncreaseHourTitle!;
        }

        if (StartTimeIncreaseMinuteIcon is not null && bitDateRangePicker.HasNotBeenSet(nameof(StartTimeIncreaseMinuteIcon)))
        {
            bitDateRangePicker.StartTimeIncreaseMinuteIcon = StartTimeIncreaseMinuteIcon;
        }

        if (StartTimeIncreaseMinuteIconName.HasValue() && bitDateRangePicker.HasNotBeenSet(nameof(StartTimeIncreaseMinuteIconName)))
        {
            bitDateRangePicker.StartTimeIncreaseMinuteIconName = StartTimeIncreaseMinuteIconName;
        }

        if (StartTimeIncreaseMinuteTitle.HasValue() && bitDateRangePicker.HasNotBeenSet(nameof(StartTimeIncreaseMinuteTitle)))
        {
            bitDateRangePicker.StartTimeIncreaseMinuteTitle = StartTimeIncreaseMinuteTitle!;
        }

        if (StartTimeMinuteInputAriaLabel.HasValue() && bitDateRangePicker.HasNotBeenSet(nameof(StartTimeMinuteInputAriaLabel)))
        {
            bitDateRangePicker.StartTimeMinuteInputAriaLabel = StartTimeMinuteInputAriaLabel!;
        }

        if (StartingValue is not null && bitDateRangePicker.HasNotBeenSet(nameof(StartingValue)))
        {
            rebuildView = rebuildView || ReferenceEquals(bitDateRangePicker.StartingValue, StartingValue) is false;

            bitDateRangePicker.StartingValue = StartingValue;
        }

        if (Styles is not null && bitDateRangePicker.HasNotBeenSet(nameof(Styles)))
        {
            bitDateRangePicker.Styles = Styles;

            bitDateRangePicker.StyleBuilder.Reset();
        }

        if (TimeFormat.HasValue && bitDateRangePicker.HasNotBeenSet(nameof(TimeFormat)))
        {
            bitDateRangePicker.TimeFormat = TimeFormat.Value;
        }

        if (TimeZone is not null && bitDateRangePicker.HasNotBeenSet(nameof(TimeZone)))
        {
            rebuildView = rebuildView || ReferenceEquals(bitDateRangePicker.TimeZone, TimeZone) is false;

            bitDateRangePicker.TimeZone = TimeZone;
        }

        if (Today.HasValue && bitDateRangePicker.HasNotBeenSet(nameof(Today)))
        {
            rebuildView = rebuildView || bitDateRangePicker.Today != Today;

            bitDateRangePicker.Today = Today;
        }

        if (Underlined.HasValue && bitDateRangePicker.HasNotBeenSet(nameof(Underlined)))
        {
            bitDateRangePicker.Underlined = Underlined.Value;

            bitDateRangePicker.ClassBuilder.Reset();
        }

        if (ValueFormat.HasValue() && bitDateRangePicker.HasNotBeenSet(nameof(ValueFormat)))
        {
            bitDateRangePicker.ValueFormat = ValueFormat!;
        }

        if (WeekNumberRule.HasValue && bitDateRangePicker.HasNotBeenSet(nameof(WeekNumberRule)))
        {
            bitDateRangePicker.WeekNumberRule = WeekNumberRule;
        }

        if (WeekNumberTitle.HasValue() && bitDateRangePicker.HasNotBeenSet(nameof(WeekNumberTitle)))
        {
            bitDateRangePicker.WeekNumberTitle = WeekNumberTitle!;
        }

        if (WeekNumbersHeaderTitle.HasValue() && bitDateRangePicker.HasNotBeenSet(nameof(WeekNumbersHeaderTitle)))
        {
            bitDateRangePicker.WeekNumbersHeaderTitle = WeekNumbersHeaderTitle!;
        }

        if (YearCellTemplate is not null && bitDateRangePicker.HasNotBeenSet(nameof(YearCellTemplate)))
        {
            bitDateRangePicker.YearCellTemplate = YearCellTemplate;
        }

        if (YearPickerToggleTitle.HasValue() && bitDateRangePicker.HasNotBeenSet(nameof(YearPickerToggleTitle)))
        {
            bitDateRangePicker.YearPickerToggleTitle = YearPickerToggleTitle!;
        }

        if (YearRangePickerToggleTitle.HasValue() && bitDateRangePicker.HasNotBeenSet(nameof(YearRangePickerToggleTitle)))
        {
            bitDateRangePicker.YearRangePickerToggleTitle = YearRangePickerToggleTitle!;
        }

        if (rebuildView)
        {
            bitDateRangePicker.OnSetParameters();
        }
    }
}
