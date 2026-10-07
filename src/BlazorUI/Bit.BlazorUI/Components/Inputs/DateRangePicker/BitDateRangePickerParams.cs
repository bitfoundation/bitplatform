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
    public BitPlacement? IconPlacement { get; set; }

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

        if (AllowTextInput.HasValue)
        {
            bitDateRangePicker.TakeFromCascade(nameof(AllowTextInput), AllowTextInput.Value, static d => d.AllowTextInput, static (d, v) => d.AllowTextInput = v);
        }

        if (ApplyButtonText.HasValue())
        {
            bitDateRangePicker.TakeFromCascade(nameof(ApplyButtonText), ApplyButtonText!, static d => d.ApplyButtonText, static (d, v) => d.ApplyButtonText = v);
        }

        if (AutoApply.HasValue)
        {
            bitDateRangePicker.TakeFromCascade(nameof(AutoApply), AutoApply.Value, static d => d.AutoApply, static (d, v) => d.AutoApply = v);
        }

        if (AutoClose.HasValue)
        {
            bitDateRangePicker.TakeFromCascade(nameof(AutoClose), AutoClose.Value, static d => d.AutoClose, static (d, v) => d.AutoClose = v);
        }

        if (AutoFocus.HasValue)
        {
            bitDateRangePicker.TakeFromCascade(nameof(AutoFocus), AutoFocus.Value, static d => d.AutoFocus, static (d, v) => d.AutoFocus = v);
        }

        if (CalloutAriaLabel.HasValue())
        {
            bitDateRangePicker.TakeFromCascade(nameof(CalloutAriaLabel), CalloutAriaLabel!, static d => d.CalloutAriaLabel, static (d, v) => d.CalloutAriaLabel = v);
        }

        if (CalloutFooterTemplate is not null)
        {
            bitDateRangePicker.TakeFromCascade(nameof(CalloutFooterTemplate), CalloutFooterTemplate, static d => d.CalloutFooterTemplate, static (d, v) => d.CalloutFooterTemplate = v);
        }

        if (CalloutHeaderTemplate is not null)
        {
            bitDateRangePicker.TakeFromCascade(nameof(CalloutHeaderTemplate), CalloutHeaderTemplate, static d => d.CalloutHeaderTemplate, static (d, v) => d.CalloutHeaderTemplate = v);
        }

        if (CalloutHtmlAttributes is not null)
        {
            bitDateRangePicker.TakeFromCascade(nameof(CalloutHtmlAttributes), CalloutHtmlAttributes, static d => d.CalloutHtmlAttributes, static (d, v) => d.CalloutHtmlAttributes = v);
        }

        if (CancelButtonText.HasValue())
        {
            bitDateRangePicker.TakeFromCascade(nameof(CancelButtonText), CancelButtonText!, static d => d.CancelButtonText, static (d, v) => d.CancelButtonText = v);
        }

        if (Classes is not null)
        {
            bitDateRangePicker.TakeFromCascade(nameof(Classes), Classes, static d => d.Classes, static (d, v) => d.Classes = v);
        }

        if (ClearButtonIcon is not null)
        {
            bitDateRangePicker.TakeFromCascade(nameof(ClearButtonIcon), ClearButtonIcon, static d => d.ClearButtonIcon, static (d, v) => d.ClearButtonIcon = v);
        }

        if (ClearButtonIconName.HasValue())
        {
            bitDateRangePicker.TakeFromCascade(nameof(ClearButtonIconName), ClearButtonIconName, static d => d.ClearButtonIconName, static (d, v) => d.ClearButtonIconName = v);
        }

        if (ClearButtonTitle.HasValue())
        {
            bitDateRangePicker.TakeFromCascade(nameof(ClearButtonTitle), ClearButtonTitle!, static d => d.ClearButtonTitle, static (d, v) => d.ClearButtonTitle = v);
        }

        if (CloseButtonIcon is not null)
        {
            bitDateRangePicker.TakeFromCascade(nameof(CloseButtonIcon), CloseButtonIcon, static d => d.CloseButtonIcon, static (d, v) => d.CloseButtonIcon = v);
        }

        if (CloseButtonIconName.HasValue())
        {
            bitDateRangePicker.TakeFromCascade(nameof(CloseButtonIconName), CloseButtonIconName, static d => d.CloseButtonIconName, static (d, v) => d.CloseButtonIconName = v);
        }

        if (CloseButtonTitle.HasValue())
        {
            bitDateRangePicker.TakeFromCascade(nameof(CloseButtonTitle), CloseButtonTitle!, static d => d.CloseButtonTitle, static (d, v) => d.CloseButtonTitle = v);
        }

        if (Color.HasValue)
        {
            bitDateRangePicker.TakeFromCascade(nameof(Color), Color, static d => d.Color, static (d, v) => d.Color = v);
        }

        if (ContinuousSpinDelay.HasValue)
        {
            bitDateRangePicker.TakeFromCascade(nameof(ContinuousSpinDelay), ContinuousSpinDelay.Value, static d => d.ContinuousSpinDelay, static (d, v) => d.ContinuousSpinDelay = v);
        }

        if (ContinuousSpinInterval.HasValue)
        {
            bitDateRangePicker.TakeFromCascade(nameof(ContinuousSpinInterval), ContinuousSpinInterval.Value, static d => d.ContinuousSpinInterval, static (d, v) => d.ContinuousSpinInterval = v);
        }

        if (Culture is not null && bitDateRangePicker.TakeFromCascade(nameof(Culture), Culture, static d => d.Culture, static (d, v) => d.Culture = v))
        {
            rebuildView = true;
        }

        if (DateFormat.HasValue())
        {
            bitDateRangePicker.TakeFromCascade(nameof(DateFormat), DateFormat, static d => d.DateFormat, static (d, v) => d.DateFormat = v);
        }

        if (DayCellTemplate is not null)
        {
            bitDateRangePicker.TakeFromCascade(nameof(DayCellTemplate), DayCellTemplate, static d => d.DayCellTemplate, static (d, v) => d.DayCellTemplate = v);
        }

        if (DisableFuture.HasValue && bitDateRangePicker.TakeFromCascade(nameof(DisableFuture), DisableFuture.Value, static d => d.DisableFuture, static (d, v) => d.DisableFuture = v))
        {
            rebuildView = true;
        }

        if (DisablePast.HasValue && bitDateRangePicker.TakeFromCascade(nameof(DisablePast), DisablePast.Value, static d => d.DisablePast, static (d, v) => d.DisablePast = v))
        {
            rebuildView = true;
        }

        if (DisabledDateErrorMessage.HasValue())
        {
            bitDateRangePicker.TakeFromCascade(nameof(DisabledDateErrorMessage), DisabledDateErrorMessage, static d => d.DisabledDateErrorMessage, static (d, v) => d.DisabledDateErrorMessage = v);
        }

        if (DisabledDates is not null && bitDateRangePicker.TakeFromCascade(nameof(DisabledDates), DisabledDates, static d => d.DisabledDates, static (d, v) => d.DisabledDates = v))
        {
            rebuildView = true;
        }

        if (DisabledDaysOfWeek is not null && bitDateRangePicker.TakeFromCascade(nameof(DisabledDaysOfWeek), DisabledDaysOfWeek, static d => d.DisabledDaysOfWeek, static (d, v) => d.DisabledDaysOfWeek = v))
        {
            rebuildView = true;
        }

        if (DropDirection.HasValue)
        {
            bitDateRangePicker.TakeFromCascade(nameof(DropDirection), DropDirection.Value, static d => d.DropDirection, static (d, v) => d.DropDirection = v);
        }

        if (EndTimeDecreaseHourIcon is not null)
        {
            bitDateRangePicker.TakeFromCascade(nameof(EndTimeDecreaseHourIcon), EndTimeDecreaseHourIcon, static d => d.EndTimeDecreaseHourIcon, static (d, v) => d.EndTimeDecreaseHourIcon = v);
        }

        if (EndTimeDecreaseHourIconName.HasValue())
        {
            bitDateRangePicker.TakeFromCascade(nameof(EndTimeDecreaseHourIconName), EndTimeDecreaseHourIconName, static d => d.EndTimeDecreaseHourIconName, static (d, v) => d.EndTimeDecreaseHourIconName = v);
        }

        if (EndTimeDecreaseHourTitle.HasValue())
        {
            bitDateRangePicker.TakeFromCascade(nameof(EndTimeDecreaseHourTitle), EndTimeDecreaseHourTitle!, static d => d.EndTimeDecreaseHourTitle, static (d, v) => d.EndTimeDecreaseHourTitle = v);
        }

        if (EndTimeDecreaseMinuteIcon is not null)
        {
            bitDateRangePicker.TakeFromCascade(nameof(EndTimeDecreaseMinuteIcon), EndTimeDecreaseMinuteIcon, static d => d.EndTimeDecreaseMinuteIcon, static (d, v) => d.EndTimeDecreaseMinuteIcon = v);
        }

        if (EndTimeDecreaseMinuteIconName.HasValue())
        {
            bitDateRangePicker.TakeFromCascade(nameof(EndTimeDecreaseMinuteIconName), EndTimeDecreaseMinuteIconName, static d => d.EndTimeDecreaseMinuteIconName, static (d, v) => d.EndTimeDecreaseMinuteIconName = v);
        }

        if (EndTimeDecreaseMinuteTitle.HasValue())
        {
            bitDateRangePicker.TakeFromCascade(nameof(EndTimeDecreaseMinuteTitle), EndTimeDecreaseMinuteTitle!, static d => d.EndTimeDecreaseMinuteTitle, static (d, v) => d.EndTimeDecreaseMinuteTitle = v);
        }

        if (EndTimeHourInputAriaLabel.HasValue())
        {
            bitDateRangePicker.TakeFromCascade(nameof(EndTimeHourInputAriaLabel), EndTimeHourInputAriaLabel!, static d => d.EndTimeHourInputAriaLabel, static (d, v) => d.EndTimeHourInputAriaLabel = v);
        }

        if (EndTimeIncreaseHourIcon is not null)
        {
            bitDateRangePicker.TakeFromCascade(nameof(EndTimeIncreaseHourIcon), EndTimeIncreaseHourIcon, static d => d.EndTimeIncreaseHourIcon, static (d, v) => d.EndTimeIncreaseHourIcon = v);
        }

        if (EndTimeIncreaseHourIconName.HasValue())
        {
            bitDateRangePicker.TakeFromCascade(nameof(EndTimeIncreaseHourIconName), EndTimeIncreaseHourIconName, static d => d.EndTimeIncreaseHourIconName, static (d, v) => d.EndTimeIncreaseHourIconName = v);
        }

        if (EndTimeIncreaseHourTitle.HasValue())
        {
            bitDateRangePicker.TakeFromCascade(nameof(EndTimeIncreaseHourTitle), EndTimeIncreaseHourTitle!, static d => d.EndTimeIncreaseHourTitle, static (d, v) => d.EndTimeIncreaseHourTitle = v);
        }

        if (EndTimeIncreaseMinuteIcon is not null)
        {
            bitDateRangePicker.TakeFromCascade(nameof(EndTimeIncreaseMinuteIcon), EndTimeIncreaseMinuteIcon, static d => d.EndTimeIncreaseMinuteIcon, static (d, v) => d.EndTimeIncreaseMinuteIcon = v);
        }

        if (EndTimeIncreaseMinuteIconName.HasValue())
        {
            bitDateRangePicker.TakeFromCascade(nameof(EndTimeIncreaseMinuteIconName), EndTimeIncreaseMinuteIconName, static d => d.EndTimeIncreaseMinuteIconName, static (d, v) => d.EndTimeIncreaseMinuteIconName = v);
        }

        if (EndTimeIncreaseMinuteTitle.HasValue())
        {
            bitDateRangePicker.TakeFromCascade(nameof(EndTimeIncreaseMinuteTitle), EndTimeIncreaseMinuteTitle!, static d => d.EndTimeIncreaseMinuteTitle, static (d, v) => d.EndTimeIncreaseMinuteTitle = v);
        }

        if (EndTimeMinuteInputAriaLabel.HasValue())
        {
            bitDateRangePicker.TakeFromCascade(nameof(EndTimeMinuteInputAriaLabel), EndTimeMinuteInputAriaLabel!, static d => d.EndTimeMinuteInputAriaLabel, static (d, v) => d.EndTimeMinuteInputAriaLabel = v);
        }

        if (ExcludeDisabledDates.HasValue && bitDateRangePicker.TakeFromCascade(nameof(ExcludeDisabledDates), ExcludeDisabledDates.Value, static d => d.ExcludeDisabledDates, static (d, v) => d.ExcludeDisabledDates = v))
        {
            rebuildView = true;
        }

        if (FirstDayOfWeek.HasValue && bitDateRangePicker.TakeFromCascade(nameof(FirstDayOfWeek), FirstDayOfWeek, static d => d.FirstDayOfWeek, static (d, v) => d.FirstDayOfWeek = v))
        {
            rebuildView = true;
        }

        if (FixedWeeks.HasValue && bitDateRangePicker.TakeFromCascade(nameof(FixedWeeks), FixedWeeks.Value, static d => d.FixedWeeks, static (d, v) => d.FixedWeeks = v))
        {
            rebuildView = true;
        }

        if (GetDayClass is not null)
        {
            bitDateRangePicker.TakeFromCascade(nameof(GetDayClass), GetDayClass, static d => d.GetDayClass, static (d, v) => d.GetDayClass = v);
        }

        if (GoToNextMonthTitle.HasValue())
        {
            bitDateRangePicker.TakeFromCascade(nameof(GoToNextMonthTitle), GoToNextMonthTitle!, static d => d.GoToNextMonthTitle, static (d, v) => d.GoToNextMonthTitle = v);
        }

        if (GoToNextYearRangeTitle.HasValue())
        {
            bitDateRangePicker.TakeFromCascade(nameof(GoToNextYearRangeTitle), GoToNextYearRangeTitle!, static d => d.GoToNextYearRangeTitle, static (d, v) => d.GoToNextYearRangeTitle = v);
        }

        if (GoToNextYearTitle.HasValue())
        {
            bitDateRangePicker.TakeFromCascade(nameof(GoToNextYearTitle), GoToNextYearTitle!, static d => d.GoToNextYearTitle, static (d, v) => d.GoToNextYearTitle = v);
        }

        if (GoToPrevMonthTitle.HasValue())
        {
            bitDateRangePicker.TakeFromCascade(nameof(GoToPrevMonthTitle), GoToPrevMonthTitle!, static d => d.GoToPrevMonthTitle, static (d, v) => d.GoToPrevMonthTitle = v);
        }

        if (GoToPrevYearRangeTitle.HasValue())
        {
            bitDateRangePicker.TakeFromCascade(nameof(GoToPrevYearRangeTitle), GoToPrevYearRangeTitle!, static d => d.GoToPrevYearRangeTitle, static (d, v) => d.GoToPrevYearRangeTitle = v);
        }

        if (GoToPrevYearTitle.HasValue())
        {
            bitDateRangePicker.TakeFromCascade(nameof(GoToPrevYearTitle), GoToPrevYearTitle!, static d => d.GoToPrevYearTitle, static (d, v) => d.GoToPrevYearTitle = v);
        }

        if (GoToTodayIcon is not null)
        {
            bitDateRangePicker.TakeFromCascade(nameof(GoToTodayIcon), GoToTodayIcon, static d => d.GoToTodayIcon, static (d, v) => d.GoToTodayIcon = v);
        }

        if (GoToTodayIconName.HasValue())
        {
            bitDateRangePicker.TakeFromCascade(nameof(GoToTodayIconName), GoToTodayIconName, static d => d.GoToTodayIconName, static (d, v) => d.GoToTodayIconName = v);
        }

        if (GoToTodayTitle.HasValue())
        {
            bitDateRangePicker.TakeFromCascade(nameof(GoToTodayTitle), GoToTodayTitle!, static d => d.GoToTodayTitle, static (d, v) => d.GoToTodayTitle = v);
        }

        if (HasBorder.HasValue)
        {
            bitDateRangePicker.TakeFromCascade(nameof(HasBorder), HasBorder.Value, static d => d.HasBorder, static (d, v) => d.HasBorder = v);
        }

        if (HideTimePickerIcon is not null)
        {
            bitDateRangePicker.TakeFromCascade(nameof(HideTimePickerIcon), HideTimePickerIcon, static d => d.HideTimePickerIcon, static (d, v) => d.HideTimePickerIcon = v);
        }

        if (HideTimePickerIconName.HasValue())
        {
            bitDateRangePicker.TakeFromCascade(nameof(HideTimePickerIconName), HideTimePickerIconName, static d => d.HideTimePickerIconName, static (d, v) => d.HideTimePickerIconName = v);
        }

        if (HideTimePickerTitle.HasValue())
        {
            bitDateRangePicker.TakeFromCascade(nameof(HideTimePickerTitle), HideTimePickerTitle!, static d => d.HideTimePickerTitle, static (d, v) => d.HideTimePickerTitle = v);
        }

        if (HighlightCurrentMonth.HasValue)
        {
            bitDateRangePicker.TakeFromCascade(nameof(HighlightCurrentMonth), HighlightCurrentMonth.Value, static d => d.HighlightCurrentMonth, static (d, v) => d.HighlightCurrentMonth = v);
        }

        if (HighlightSelectedMonth.HasValue)
        {
            bitDateRangePicker.TakeFromCascade(nameof(HighlightSelectedMonth), HighlightSelectedMonth.Value, static d => d.HighlightSelectedMonth, static (d, v) => d.HighlightSelectedMonth = v);
        }

        if (HighlightToday.HasValue)
        {
            bitDateRangePicker.TakeFromCascade(nameof(HighlightToday), HighlightToday.Value, static d => d.HighlightToday, static (d, v) => d.HighlightToday = v);
        }

        if (HighlightedDates is not null && bitDateRangePicker.TakeFromCascade(nameof(HighlightedDates), HighlightedDates, static d => d.HighlightedDates, static (d, v) => d.HighlightedDates = v))
        {
            rebuildView = true;
        }

        if (HourStep.HasValue)
        {
            bitDateRangePicker.TakeFromCascade(nameof(HourStep), HourStep.Value, static d => d.HourStep, static (d, v) => d.HourStep = v);
        }

        if (Icon is not null)
        {
            bitDateRangePicker.TakeFromCascade(nameof(Icon), Icon, static d => d.Icon, static (d, v) => d.Icon = v);
        }

        if (IconPlacement.HasValue)
        {
            bitDateRangePicker.TakeFromCascade(nameof(IconPlacement), IconPlacement.Value, static d => d.IconPlacement, static (d, v) => d.IconPlacement = v);
        }

        if (IconName.HasValue())
        {
            bitDateRangePicker.TakeFromCascade(nameof(IconName), IconName, static d => d.IconName, static (d, v) => d.IconName = v);
        }

        if (IconTemplate is not null)
        {
            bitDateRangePicker.TakeFromCascade(nameof(IconTemplate), IconTemplate, static d => d.IconTemplate, static (d, v) => d.IconTemplate = v);
        }

        if (InvalidErrorMessage.HasValue())
        {
            bitDateRangePicker.TakeFromCascade(nameof(InvalidErrorMessage), InvalidErrorMessage, static d => d.InvalidErrorMessage, static (d, v) => d.InvalidErrorMessage = v);
        }

        if (IsDateDisabled is not null && bitDateRangePicker.TakeFromCascade(nameof(IsDateDisabled), IsDateDisabled, static d => d.IsDateDisabled, static (d, v) => d.IsDateDisabled = v))
        {
            rebuildView = true;
        }

        if (IsMonthPickerVisible.HasValue && bitDateRangePicker.TakeFromCascade(nameof(IsMonthPickerVisible), IsMonthPickerVisible.Value, static d => d.IsMonthPickerVisible, static (d, v) => d.IsMonthPickerVisible = v))
        {
            rebuildView = true;
        }

        if (Label.HasValue())
        {
            bitDateRangePicker.TakeFromCascade(nameof(Label), Label, static d => d.Label, static (d, v) => d.Label = v);
        }

        if (LabelTemplate is not null)
        {
            bitDateRangePicker.TakeFromCascade(nameof(LabelTemplate), LabelTemplate, static d => d.LabelTemplate, static (d, v) => d.LabelTemplate = v);
        }

        if (MaxDate.HasValue && bitDateRangePicker.TakeFromCascade(nameof(MaxDate), MaxDate, static d => d.MaxDate, static (d, v) => d.MaxDate = v))
        {
            rebuildView = true;
        }

        if (MaxRange.HasValue)
        {
            bitDateRangePicker.TakeFromCascade(nameof(MaxRange), MaxRange, static d => d.MaxRange, static (d, v) => d.MaxRange = v);
        }

        if (MinDate.HasValue && bitDateRangePicker.TakeFromCascade(nameof(MinDate), MinDate, static d => d.MinDate, static (d, v) => d.MinDate = v))
        {
            rebuildView = true;
        }

        if (MinRange.HasValue)
        {
            bitDateRangePicker.TakeFromCascade(nameof(MinRange), MinRange, static d => d.MinRange, static (d, v) => d.MinRange = v);
        }

        if (MinuteStep.HasValue)
        {
            bitDateRangePicker.TakeFromCascade(nameof(MinuteStep), MinuteStep.Value, static d => d.MinuteStep, static (d, v) => d.MinuteStep = v);
        }

        if (MonthCellTemplate is not null)
        {
            bitDateRangePicker.TakeFromCascade(nameof(MonthCellTemplate), MonthCellTemplate, static d => d.MonthCellTemplate, static (d, v) => d.MonthCellTemplate = v);
        }

        if (MonthCount.HasValue && bitDateRangePicker.TakeFromCascade(nameof(MonthCount), MonthCount.Value, static d => d.MonthCount, static (d, v) => d.MonthCount = v))
        {
            rebuildView = true;
        }

        if (MonthPickerToggleTitle.HasValue())
        {
            bitDateRangePicker.TakeFromCascade(nameof(MonthPickerToggleTitle), MonthPickerToggleTitle!, static d => d.MonthPickerToggleTitle, static (d, v) => d.MonthPickerToggleTitle = v);
        }

        if (NextMonthNavIcon is not null)
        {
            bitDateRangePicker.TakeFromCascade(nameof(NextMonthNavIcon), NextMonthNavIcon, static d => d.NextMonthNavIcon, static (d, v) => d.NextMonthNavIcon = v);
        }

        if (NextMonthNavIconName.HasValue())
        {
            bitDateRangePicker.TakeFromCascade(nameof(NextMonthNavIconName), NextMonthNavIconName, static d => d.NextMonthNavIconName, static (d, v) => d.NextMonthNavIconName = v);
        }

        if (NextYearNavIcon is not null)
        {
            bitDateRangePicker.TakeFromCascade(nameof(NextYearNavIcon), NextYearNavIcon, static d => d.NextYearNavIcon, static (d, v) => d.NextYearNavIcon = v);
        }

        if (NextYearNavIconName.HasValue())
        {
            bitDateRangePicker.TakeFromCascade(nameof(NextYearNavIconName), NextYearNavIconName, static d => d.NextYearNavIconName, static (d, v) => d.NextYearNavIconName = v);
        }

        if (NextYearRangeNavIcon is not null)
        {
            bitDateRangePicker.TakeFromCascade(nameof(NextYearRangeNavIcon), NextYearRangeNavIcon, static d => d.NextYearRangeNavIcon, static (d, v) => d.NextYearRangeNavIcon = v);
        }

        if (NextYearRangeNavIconName.HasValue())
        {
            bitDateRangePicker.TakeFromCascade(nameof(NextYearRangeNavIconName), NextYearRangeNavIconName, static d => d.NextYearRangeNavIconName, static (d, v) => d.NextYearRangeNavIconName = v);
        }

        if (NoDateText.HasValue())
        {
            bitDateRangePicker.TakeFromCascade(nameof(NoDateText), NoDateText!, static d => d.NoDateText, static (d, v) => d.NoDateText = v);
        }

        if (OutOfRangeErrorMessage.HasValue())
        {
            bitDateRangePicker.TakeFromCascade(nameof(OutOfRangeErrorMessage), OutOfRangeErrorMessage, static d => d.OutOfRangeErrorMessage, static (d, v) => d.OutOfRangeErrorMessage = v);
        }

        if (PagedNavigation.HasValue)
        {
            bitDateRangePicker.TakeFromCascade(nameof(PagedNavigation), PagedNavigation.Value, static d => d.PagedNavigation, static (d, v) => d.PagedNavigation = v);
        }

        if (Placeholder.HasValue())
        {
            bitDateRangePicker.TakeFromCascade(nameof(Placeholder), Placeholder!, static d => d.Placeholder, static (d, v) => d.Placeholder = v);
        }

        if (Presets is not null)
        {
            bitDateRangePicker.TakeFromCascade(nameof(Presets), Presets, static d => d.Presets, static (d, v) => d.Presets = v);
        }

        if (PresetsAriaLabel.HasValue())
        {
            bitDateRangePicker.TakeFromCascade(nameof(PresetsAriaLabel), PresetsAriaLabel!, static d => d.PresetsAriaLabel, static (d, v) => d.PresetsAriaLabel = v);
        }

        if (PrevMonthNavIcon is not null)
        {
            bitDateRangePicker.TakeFromCascade(nameof(PrevMonthNavIcon), PrevMonthNavIcon, static d => d.PrevMonthNavIcon, static (d, v) => d.PrevMonthNavIcon = v);
        }

        if (PrevMonthNavIconName.HasValue())
        {
            bitDateRangePicker.TakeFromCascade(nameof(PrevMonthNavIconName), PrevMonthNavIconName, static d => d.PrevMonthNavIconName, static (d, v) => d.PrevMonthNavIconName = v);
        }

        if (PrevYearNavIcon is not null)
        {
            bitDateRangePicker.TakeFromCascade(nameof(PrevYearNavIcon), PrevYearNavIcon, static d => d.PrevYearNavIcon, static (d, v) => d.PrevYearNavIcon = v);
        }

        if (PrevYearNavIconName.HasValue())
        {
            bitDateRangePicker.TakeFromCascade(nameof(PrevYearNavIconName), PrevYearNavIconName, static d => d.PrevYearNavIconName, static (d, v) => d.PrevYearNavIconName = v);
        }

        if (PrevYearRangeNavIcon is not null)
        {
            bitDateRangePicker.TakeFromCascade(nameof(PrevYearRangeNavIcon), PrevYearRangeNavIcon, static d => d.PrevYearRangeNavIcon, static (d, v) => d.PrevYearRangeNavIcon = v);
        }

        if (PrevYearRangeNavIconName.HasValue())
        {
            bitDateRangePicker.TakeFromCascade(nameof(PrevYearRangeNavIconName), PrevYearRangeNavIconName, static d => d.PrevYearRangeNavIconName, static (d, v) => d.PrevYearRangeNavIconName = v);
        }

        if (Responsive.HasValue)
        {
            bitDateRangePicker.TakeFromCascade(nameof(Responsive), Responsive.Value, static d => d.Responsive, static (d, v) => d.Responsive = v);
        }

        if (SelectedDateAriaAtomic.HasValue())
        {
            bitDateRangePicker.TakeFromCascade(nameof(SelectedDateAriaAtomic), SelectedDateAriaAtomic!, static d => d.SelectedDateAriaAtomic, static (d, v) => d.SelectedDateAriaAtomic = v);
        }

        if (ShowClearButton.HasValue)
        {
            bitDateRangePicker.TakeFromCascade(nameof(ShowClearButton), ShowClearButton.Value, static d => d.ShowClearButton, static (d, v) => d.ShowClearButton = v);
        }

        if (ShowCloseButton.HasValue)
        {
            bitDateRangePicker.TakeFromCascade(nameof(ShowCloseButton), ShowCloseButton.Value, static d => d.ShowCloseButton, static (d, v) => d.ShowCloseButton = v);
        }

        if (ShowGoToToday.HasValue)
        {
            bitDateRangePicker.TakeFromCascade(nameof(ShowGoToToday), ShowGoToToday.Value, static d => d.ShowGoToToday, static (d, v) => d.ShowGoToToday = v);
        }

        if (ShowMonthPickerAsOverlay.HasValue && bitDateRangePicker.TakeFromCascade(nameof(ShowMonthPickerAsOverlay), ShowMonthPickerAsOverlay.Value, static d => d.ShowMonthPickerAsOverlay, static (d, v) => d.ShowMonthPickerAsOverlay = v))
        {
            rebuildView = true;
        }

        if (ShowOutsideDays.HasValue)
        {
            bitDateRangePicker.TakeFromCascade(nameof(ShowOutsideDays), ShowOutsideDays.Value, static d => d.ShowOutsideDays, static (d, v) => d.ShowOutsideDays = v);
        }

        if (ShowTimePicker.HasValue && bitDateRangePicker.TakeFromCascade(nameof(ShowTimePicker), ShowTimePicker.Value, static d => d.ShowTimePicker, static (d, v) => d.ShowTimePicker = v))
        {
            rebuildView = true;
        }

        if (ShowTimePickerAsOverlay.HasValue && bitDateRangePicker.TakeFromCascade(nameof(ShowTimePickerAsOverlay), ShowTimePickerAsOverlay.Value, static d => d.ShowTimePickerAsOverlay, static (d, v) => d.ShowTimePickerAsOverlay = v))
        {
            rebuildView = true;
        }

        if (ShowTimePickerIcon is not null)
        {
            bitDateRangePicker.TakeFromCascade(nameof(ShowTimePickerIcon), ShowTimePickerIcon, static d => d.ShowTimePickerIcon, static (d, v) => d.ShowTimePickerIcon = v);
        }

        if (ShowTimePickerIconName.HasValue())
        {
            bitDateRangePicker.TakeFromCascade(nameof(ShowTimePickerIconName), ShowTimePickerIconName, static d => d.ShowTimePickerIconName, static (d, v) => d.ShowTimePickerIconName = v);
        }

        if (ShowTimePickerTitle.HasValue())
        {
            bitDateRangePicker.TakeFromCascade(nameof(ShowTimePickerTitle), ShowTimePickerTitle!, static d => d.ShowTimePickerTitle, static (d, v) => d.ShowTimePickerTitle = v);
        }

        if (ShowWeekNumbers.HasValue)
        {
            bitDateRangePicker.TakeFromCascade(nameof(ShowWeekNumbers), ShowWeekNumbers.Value, static d => d.ShowWeekNumbers, static (d, v) => d.ShowWeekNumbers = v);
        }

        if (Size.HasValue)
        {
            bitDateRangePicker.TakeFromCascade(nameof(Size), Size, static d => d.Size, static (d, v) => d.Size = v);
        }

        if (Standalone.HasValue && bitDateRangePicker.TakeFromCascade(nameof(Standalone), Standalone.Value, static d => d.Standalone, static (d, v) => d.Standalone = v))
        {
            rebuildView = true;
        }

        if (StartTimeDecreaseHourIcon is not null)
        {
            bitDateRangePicker.TakeFromCascade(nameof(StartTimeDecreaseHourIcon), StartTimeDecreaseHourIcon, static d => d.StartTimeDecreaseHourIcon, static (d, v) => d.StartTimeDecreaseHourIcon = v);
        }

        if (StartTimeDecreaseHourIconName.HasValue())
        {
            bitDateRangePicker.TakeFromCascade(nameof(StartTimeDecreaseHourIconName), StartTimeDecreaseHourIconName, static d => d.StartTimeDecreaseHourIconName, static (d, v) => d.StartTimeDecreaseHourIconName = v);
        }

        if (StartTimeDecreaseHourTitle.HasValue())
        {
            bitDateRangePicker.TakeFromCascade(nameof(StartTimeDecreaseHourTitle), StartTimeDecreaseHourTitle!, static d => d.StartTimeDecreaseHourTitle, static (d, v) => d.StartTimeDecreaseHourTitle = v);
        }

        if (StartTimeDecreaseMinuteIcon is not null)
        {
            bitDateRangePicker.TakeFromCascade(nameof(StartTimeDecreaseMinuteIcon), StartTimeDecreaseMinuteIcon, static d => d.StartTimeDecreaseMinuteIcon, static (d, v) => d.StartTimeDecreaseMinuteIcon = v);
        }

        if (StartTimeDecreaseMinuteIconName.HasValue())
        {
            bitDateRangePicker.TakeFromCascade(nameof(StartTimeDecreaseMinuteIconName), StartTimeDecreaseMinuteIconName, static d => d.StartTimeDecreaseMinuteIconName, static (d, v) => d.StartTimeDecreaseMinuteIconName = v);
        }

        if (StartTimeDecreaseMinuteTitle.HasValue())
        {
            bitDateRangePicker.TakeFromCascade(nameof(StartTimeDecreaseMinuteTitle), StartTimeDecreaseMinuteTitle!, static d => d.StartTimeDecreaseMinuteTitle, static (d, v) => d.StartTimeDecreaseMinuteTitle = v);
        }

        if (StartTimeHourInputAriaLabel.HasValue())
        {
            bitDateRangePicker.TakeFromCascade(nameof(StartTimeHourInputAriaLabel), StartTimeHourInputAriaLabel!, static d => d.StartTimeHourInputAriaLabel, static (d, v) => d.StartTimeHourInputAriaLabel = v);
        }

        if (StartTimeIncreaseHourIcon is not null)
        {
            bitDateRangePicker.TakeFromCascade(nameof(StartTimeIncreaseHourIcon), StartTimeIncreaseHourIcon, static d => d.StartTimeIncreaseHourIcon, static (d, v) => d.StartTimeIncreaseHourIcon = v);
        }

        if (StartTimeIncreaseHourIconName.HasValue())
        {
            bitDateRangePicker.TakeFromCascade(nameof(StartTimeIncreaseHourIconName), StartTimeIncreaseHourIconName, static d => d.StartTimeIncreaseHourIconName, static (d, v) => d.StartTimeIncreaseHourIconName = v);
        }

        if (StartTimeIncreaseHourTitle.HasValue())
        {
            bitDateRangePicker.TakeFromCascade(nameof(StartTimeIncreaseHourTitle), StartTimeIncreaseHourTitle!, static d => d.StartTimeIncreaseHourTitle, static (d, v) => d.StartTimeIncreaseHourTitle = v);
        }

        if (StartTimeIncreaseMinuteIcon is not null)
        {
            bitDateRangePicker.TakeFromCascade(nameof(StartTimeIncreaseMinuteIcon), StartTimeIncreaseMinuteIcon, static d => d.StartTimeIncreaseMinuteIcon, static (d, v) => d.StartTimeIncreaseMinuteIcon = v);
        }

        if (StartTimeIncreaseMinuteIconName.HasValue())
        {
            bitDateRangePicker.TakeFromCascade(nameof(StartTimeIncreaseMinuteIconName), StartTimeIncreaseMinuteIconName, static d => d.StartTimeIncreaseMinuteIconName, static (d, v) => d.StartTimeIncreaseMinuteIconName = v);
        }

        if (StartTimeIncreaseMinuteTitle.HasValue())
        {
            bitDateRangePicker.TakeFromCascade(nameof(StartTimeIncreaseMinuteTitle), StartTimeIncreaseMinuteTitle!, static d => d.StartTimeIncreaseMinuteTitle, static (d, v) => d.StartTimeIncreaseMinuteTitle = v);
        }

        if (StartTimeMinuteInputAriaLabel.HasValue())
        {
            bitDateRangePicker.TakeFromCascade(nameof(StartTimeMinuteInputAriaLabel), StartTimeMinuteInputAriaLabel!, static d => d.StartTimeMinuteInputAriaLabel, static (d, v) => d.StartTimeMinuteInputAriaLabel = v);
        }

        if (StartingValue is not null && bitDateRangePicker.TakeFromCascade(nameof(StartingValue), StartingValue, static d => d.StartingValue, static (d, v) => d.StartingValue = v))
        {
            rebuildView = true;
        }

        if (Styles is not null)
        {
            bitDateRangePicker.TakeFromCascade(nameof(Styles), Styles, static d => d.Styles, static (d, v) => d.Styles = v);
        }

        if (TimeFormat.HasValue)
        {
            bitDateRangePicker.TakeFromCascade(nameof(TimeFormat), TimeFormat.Value, static d => d.TimeFormat, static (d, v) => d.TimeFormat = v);
        }

        if (TimeZone is not null && bitDateRangePicker.TakeFromCascade(nameof(TimeZone), TimeZone, static d => d.TimeZone, static (d, v) => d.TimeZone = v))
        {
            rebuildView = true;
        }

        if (Today.HasValue && bitDateRangePicker.TakeFromCascade(nameof(Today), Today, static d => d.Today, static (d, v) => d.Today = v))
        {
            rebuildView = true;
        }

        if (Underlined.HasValue)
        {
            bitDateRangePicker.TakeFromCascade(nameof(Underlined), Underlined.Value, static d => d.Underlined, static (d, v) => d.Underlined = v);
        }

        if (ValueFormat.HasValue())
        {
            bitDateRangePicker.TakeFromCascade(nameof(ValueFormat), ValueFormat!, static d => d.ValueFormat, static (d, v) => d.ValueFormat = v);
        }

        if (WeekNumberRule.HasValue)
        {
            bitDateRangePicker.TakeFromCascade(nameof(WeekNumberRule), WeekNumberRule, static d => d.WeekNumberRule, static (d, v) => d.WeekNumberRule = v);
        }

        if (WeekNumberTitle.HasValue())
        {
            bitDateRangePicker.TakeFromCascade(nameof(WeekNumberTitle), WeekNumberTitle!, static d => d.WeekNumberTitle, static (d, v) => d.WeekNumberTitle = v);
        }

        if (WeekNumbersHeaderTitle.HasValue())
        {
            bitDateRangePicker.TakeFromCascade(nameof(WeekNumbersHeaderTitle), WeekNumbersHeaderTitle!, static d => d.WeekNumbersHeaderTitle, static (d, v) => d.WeekNumbersHeaderTitle = v);
        }

        if (YearCellTemplate is not null)
        {
            bitDateRangePicker.TakeFromCascade(nameof(YearCellTemplate), YearCellTemplate, static d => d.YearCellTemplate, static (d, v) => d.YearCellTemplate = v);
        }

        if (YearPickerToggleTitle.HasValue())
        {
            bitDateRangePicker.TakeFromCascade(nameof(YearPickerToggleTitle), YearPickerToggleTitle!, static d => d.YearPickerToggleTitle, static (d, v) => d.YearPickerToggleTitle = v);
        }

        if (YearRangePickerToggleTitle.HasValue())
        {
            bitDateRangePicker.TakeFromCascade(nameof(YearRangePickerToggleTitle), YearRangePickerToggleTitle!, static d => d.YearRangePickerToggleTitle, static (d, v) => d.YearRangePickerToggleTitle = v);
        }

        if (rebuildView)
        {
            bitDateRangePicker.OnSetParameters();
        }
    }
}
