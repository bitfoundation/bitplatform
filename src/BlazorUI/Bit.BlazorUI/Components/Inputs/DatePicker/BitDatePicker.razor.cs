using System.Text;
using System.Globalization;
using System.Diagnostics.CodeAnalysis;

namespace Bit.BlazorUI;

/// <summary>
/// A BitDatePicker offers a drop-down control that’s optimized for picking a single date from a calendar view where contextual information like the day of the week or fullness of the calendar is important.
/// It offers day, month, and year views, an optional time picker, flexible day and week rules such as disabled and highlighted dates, any culture and time zone, typed input, and complete keyboard accessibility.
/// </summary>
public partial class BitDatePicker : BitInputBase<DateTimeOffset?>
{
    private const int MAX_WIDTH = 470;
    private const int DEFAULT_WEEK_COUNT = 6;
    private const int DEFAULT_DAY_COUNT_PER_WEEK = 7;



    private bool _hasFocus;
    private int _currentYear;
    private int _currentMonth;
    private int _yearPickerEndYear;
    private int _yearPickerStartYear;
    private bool _showMonthPicker = true;
    private bool _isTimePickerOverlayOnTop;
    private bool _isMonthPickerOverlayOnTop;
    private string _monthTitle = string.Empty;
    private bool _showTimePickerAsOverlayInternal;
    private bool _showMonthPickerAsOverlayInternal;
    private TimeZoneInfo _timeZone = TimeZoneInfo.Local;
    private CultureInfo _culture = CultureInfo.CurrentUICulture;
    private CancellationTokenSource _cancellationTokenSource = new();
    private DotNetObjectReference<BitDatePicker>? _dotnetObj;
    private readonly DateTime?[,] _daysOfCurrentMonth = new DateTime?[DEFAULT_WEEK_COUNT, DEFAULT_DAY_COUNT_PER_WEEK];

    private bool _focusDayOnOpen;
    private bool _focusTimePickerAfterRender;
    private int? _focusedYearCell;
    private int? _focusedMonthCell;
    private DateTime? _focusedDate;
    private string? _focusElementIdAfterRender;
    private HashSet<DateTime> _disabledDates = [];
    private HashSet<DateTime> _highlightedDates = [];
    private HashSet<DayOfWeek> _disabledDaysOfWeek = [];

    private string? _labelId;
    private string? _inputId;
    private string _calloutId = string.Empty;
    private string _overlayId = string.Empty;
    private string _datePickerId = string.Empty;
    private ElementReference _inputTimeHourRef = default!;
    private ElementReference _inputTimeMinuteRef = default!;



    private int _hour;
    private int _hourView
    {
        get
        {
            if (TimeFormat == BitTimeFormat.TwelveHours)
            {
                if (_hour > 12)
                {
                    return _hour - 12;
                }

                if (_hour == 0)
                {
                    return 12;
                }
            }

            return _hour;
        }
        set
        {
            if (TimeFormat == BitTimeFormat.TwelveHours)
            {
                // The input of a 12-hour clock carries no meridiem of its own, so the one already
                // selected is kept: typing 5 while the time reads 15:30 gives 17:30, not 05:30.
                var isPm = _hour >= 12;

                _hour = (Math.Clamp(value, 0, 12) % 12) + (isPm ? 12 : 0);
            }
            else
            {
                _hour = Math.Clamp(value, 0, 23);
            }

            _ = UpdateCurrentValue();
        }
    }

    private int _minute;
    private int _minuteView
    {
        get => _minute;
        set
        {
            if (value > 59)
            {
                _minute = 59;
            }
            else if (value < 0)
            {
                _minute = 0;
            }
            else
            {
                _minute = value;
            }

            _ = UpdateCurrentValue();
        }
    }



    [Inject] private IJSRuntime _js { get; set; } = default!;



    /// <summary>
    /// Whether or not the DatePicker allows a string date input.
    /// </summary>
    [Parameter] public bool AllowTextInput { get; set; }

    /// <summary>
    /// Whether the DatePicker closes automatically after selecting the date.
    /// It has no effect while the time picker is shown, where the callout stays open so the time of the
    /// selected day can be set as well.
    /// </summary>
    [Parameter] public bool AutoClose { get; set; } = true;

    /// <summary>
    /// Aria label of the DatePicker's callout for screen readers.
    /// </summary>
    [Parameter] public string CalloutAriaLabel { get; set; } = "Calendar";

    /// <summary>
    /// Capture and render additional html attributes for the DatePicker's callout.
    /// </summary>
    [Parameter] public Dictionary<string, object> CalloutHtmlAttributes { get; set; } = [];

    /// <summary>
    /// Custom CSS classes for different parts of the BitDatePicker component.
    /// </summary>
    [Parameter] public BitDatePickerClassStyles? Classes { get; set; }

    /// <summary>
    /// The icon to display inside the clear button.
    /// Takes precedence over <see cref="ClearButtonIconName"/> when both are set.
    /// </summary>
    [Parameter] public BitIconInfo? ClearButtonIcon { get; set; }

    /// <summary>
    /// The name of the clear button's icon from the built-in Fluent UI icon set.
    /// </summary>
    [Parameter] public string? ClearButtonIconName { get; set; }

    /// <summary>
    /// The title (tooltip) and the accessible name of the clear button.
    /// </summary>
    [Parameter] public string ClearButtonTitle { get; set; } = "Clear date";

    /// <summary>
    /// The icon to display inside the close button.
    /// Takes precedence over <see cref="CloseButtonIconName"/> when both are set.
    /// </summary>
    [Parameter] public BitIconInfo? CloseButtonIcon { get; set; }

    /// <summary>
    /// The name of the close button's icon from the built-in Fluent UI icon set.
    /// </summary>
    [Parameter] public string? CloseButtonIconName { get; set; }

    /// <summary>
    /// The title of the CloseDatePicker button (tooltip).
    /// </summary>
    [Parameter] public string CloseDatePickerTitle { get; set; } = "Close date picker";

    /// <summary>
    /// The general color of the DatePicker that applies to the today day button, the highlighted current month,
    /// and the selected AM/PM button.
    /// </summary>
    [Parameter, ResetClassBuilder]
    public BitColor? Color { get; set; }

    /// <summary>
    /// CultureInfo for the DatePicker.
    /// </summary>
    [Parameter, ResetClassBuilder]
    [CallOnSet(nameof(OnSetParameters))]
    public CultureInfo? Culture { get; set; }

    /// <summary>
    /// The format of the date in the DatePicker.
    /// </summary>
    [Parameter] public string? DateFormat { get; set; }

    /// <summary>
    /// Custom template to render the day cells of the DatePicker.
    /// </summary>
    [Parameter] public RenderFragment<DateTimeOffset>? DayCellTemplate { get; set; }

    /// <summary>
    /// The custom validation error message for a typed value that the DatePicker does not allow to be
    /// selected, through <see cref="DisabledDates"/>, <see cref="DisabledDaysOfWeek"/> or
    /// <see cref="IsDateDisabled"/>.
    /// </summary>
    [Parameter] public string? DisabledDateErrorMessage { get; set; }

    /// <summary>
    /// The list of dates that are disabled (not selectable) in the DatePicker, in addition to
    /// <see cref="MinDate"/> and <see cref="MaxDate"/>. Only the date part of each value is considered.
    /// </summary>
    [Parameter] public IEnumerable<DateTimeOffset>? DisabledDates { get; set; }

    /// <summary>
    /// The days of the week that are disabled (not selectable) in the DatePicker (e.g. weekends).
    /// </summary>
    [Parameter] public IEnumerable<DayOfWeek>? DisabledDaysOfWeek { get; set; }

    /// <summary>
    /// Overrides the first day of the week of the day picker. If not set, the first day of the week
    /// of the <see cref="Culture"/> is used.
    /// </summary>
    [Parameter]
    [CallOnSet(nameof(OnSetParameters))]
    public DayOfWeek? FirstDayOfWeek { get; set; }

    /// <summary>
    /// Whether the day picker should always render six weeks, filling the extra rows with the days of the
    /// adjacent months, to keep the height of the calendar fixed while navigating between the months.
    /// </summary>
    [Parameter]
    [CallOnSet(nameof(OnSetParameters))]
    public bool FixedWeeks { get; set; }

    /// <summary>
    /// Custom function to provide additional CSS classes for each day button of the DatePicker.
    /// </summary>
    [Parameter] public Func<DateTimeOffset, string?>? GetDayClass { get; set; }

    /// <summary>
    /// The title of the Go to next month button (tooltip).
    /// </summary>
    [Parameter] public string GoToNextMonthTitle { get; set; } = "Go to next month";

    /// <summary>
    /// The title of the Go to next year range button (tooltip).
    /// </summary>
    [Parameter] public string GoToNextYearRangeTitle { get; set; } = "Next year range {0} - {1}";

    /// <summary>
    /// The title of the Go to next year button (tooltip).
    /// </summary>
    [Parameter] public string GoToNextYearTitle { get; set; } = "Go to next year {0}";

    /// <summary>
    /// The icon to display inside the GoToNow button.
    /// Takes precedence over <see cref="GoToNowIconName"/> when both are set.
    /// </summary>
    [Parameter] public BitIconInfo? GoToNowIcon { get; set; }

    /// <summary>
    /// The name of the GoToNow button's icon from the built-in Fluent UI icon set.
    /// </summary>
    [Parameter] public string? GoToNowIconName { get; set; }

    /// <summary>
    /// The title of the GoToNow button (tooltip).
    /// </summary>
    [Parameter] public string GoToNowTitle { get; set; } = "Go to now";

    /// <summary>
    /// The title of the Go to previous month button (tooltip).
    /// </summary>
    [Parameter] public string GoToPrevMonthTitle { get; set; } = "Go to previous month";

    /// <summary>
    /// The title of the Go to previous year range button (tooltip).
    /// </summary>
    [Parameter] public string GoToPrevYearRangeTitle { get; set; } = "Previous year range {0} - {1}";

    /// <summary>
    /// The title of the Go to previous year button (tooltip).
    /// </summary>
    [Parameter] public string GoToPrevYearTitle { get; set; } = "Go to previous year {0}";

    /// <summary>
    /// The icon to display inside the GoToToday button.
    /// Takes precedence over <see cref="GoToTodayIconName"/> when both are set.
    /// </summary>
    [Parameter] public BitIconInfo? GoToTodayIcon { get; set; }

    /// <summary>
    /// The name of the GoToToday button's icon from the built-in Fluent UI icon set.
    /// </summary>
    [Parameter] public string? GoToTodayIconName { get; set; }

    /// <summary>
    /// The title of the GoToToday button (tooltip).
    /// </summary>
    [Parameter] public string GoToTodayTitle { get; set; } = "Go to today";

    /// <summary>
    /// Determines if the DatePicker has a border.
    /// </summary>
    [Parameter, ResetClassBuilder]
    public bool HasBorder { get; set; } = true;

    /// <summary>
    /// The icon to display inside the HideTimePicker button.
    /// Takes precedence over <see cref="HideTimePickerIconName"/> when both are set.
    /// </summary>
    [Parameter] public BitIconInfo? HideTimePickerIcon { get; set; }

    /// <summary>
    /// The name of the HideTimePicker button's icon from the built-in Fluent UI icon set.
    /// </summary>
    [Parameter] public string? HideTimePickerIconName { get; set; }

    /// <summary>
    /// The title of the HideTimePicker button (tooltip).
    /// </summary>
    [Parameter] public string HideTimePickerTitle { get; set; } = "Hide time picker";

    /// <summary>
    /// Whether the month picker should highlight the current month.
    /// </summary>
    [Parameter] public bool HighlightCurrentMonth { get; set; }

    /// <summary>
    /// The list of dates that are highlighted (marked) in the day picker.
    /// </summary>
    [Parameter] public IEnumerable<DateTimeOffset>? HighlightedDates { get; set; }

    /// <summary>
    /// Whether the month picker should highlight the selected month.
    /// </summary>
    [Parameter] public bool HighlightSelectedMonth { get; set; }

    /// <summary>
    /// Determines increment/decrement steps for date-picker's hour.
    /// </summary>
    [Parameter] public int HourStep { get; set; } = 1;

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
    [Parameter] public BitIconInfo? Icon { get; set; }

    /// <summary>
    /// Determines the location of the DatePicker's icon.
    /// </summary>
    [Parameter, ResetClassBuilder]
    public BitIconLocation IconLocation { get; set; } = BitIconLocation.Right;

    /// <summary>
    /// The name of the DatePicker's icon from the built-in Fluent UI icon set.
    /// </summary>
    /// <remarks>
    /// The icon name should be from the Fluent UI icon set (e.g., <c>BitIconName.CalendarMirrored</c>).
    /// For external icon libraries, use <see cref="Icon"/> instead.
    /// </remarks>
    [Parameter] public string? IconName { get; set; }

    /// <summary>
    /// Custom template for the DatePicker's icon.
    /// </summary>
    [Parameter] public RenderFragment? IconTemplate { get; set; }

    /// <summary>
    /// The custom validation error message for the invalid value.
    /// </summary>
    [Parameter] public string? InvalidErrorMessage { get; set; }

    /// <summary>
    /// Custom function to determine if a specific date is disabled (not selectable) in the DatePicker.
    /// </summary>
    [Parameter] public Func<DateTimeOffset, bool>? IsDateDisabled { get; set; }

    /// <summary>
    /// Whether the month picker is shown next to the day picker or hidden.
    /// It has no effect in the MonthPicker mode, where the month picker is the only view.
    /// </summary>
    [Parameter]
    [CallOnSet(nameof(OnSetParameters))]
    public bool IsMonthPickerVisible { get; set; } = true;

    /// <summary>
    /// Whether or not the DatePicker's callout is open
    /// </summary>
    [Parameter, TwoWayBound]
    public bool IsOpen { get; set; }

    /// <summary>
    /// The text of the DatePicker's label.
    /// </summary>
    [Parameter] public string? Label { get; set; }

    /// <summary>
    /// Custom template for the DatePicker's label.
    /// </summary>
    [Parameter] public RenderFragment? LabelTemplate { get; set; }

    /// <summary>
    /// The maximum date allowed for the DatePicker.
    /// </summary>
    [Parameter]
    [CallOnSet(nameof(OnSetParameters))]
    public DateTimeOffset? MaxDate { get; set; }

    /// <summary>
    /// The minimum date allowed for the DatePicker.
    /// </summary>
    [Parameter]
    [CallOnSet(nameof(OnSetParameters))]
    public DateTimeOffset? MinDate { get; set; }

    /// <summary>
    /// Determines increment/decrement steps for date-picker's minute.
    /// </summary>
    [Parameter] public int MinuteStep { get; set; } = 1;

    /// <summary>
    /// The selection mode of the DatePicker (DatePicker or MonthPicker).
    /// </summary>
    [Parameter]
    [CallOnSet(nameof(OnSetParameters))]
    public BitDatePickerMode Mode { get; set; } = BitDatePickerMode.DatePicker;

    /// <summary>
    /// Custom template to render the month cells of the DatePicker.
    /// </summary>
    [Parameter] public RenderFragment<DateTimeOffset>? MonthCellTemplate { get; set; }

    /// <summary>
    /// The title of the month picker's toggle (tooltip).
    /// </summary>
    [Parameter] public string MonthPickerToggleTitle { get; set; } = "{0}, change month";

    /// <summary>
    /// The icon to display inside the next-month navigation button.
    /// Takes precedence over <see cref="NextMonthNavIconName"/> when both are set.
    /// </summary>
    [Parameter] public BitIconInfo? NextMonthNavIcon { get; set; }

    /// <summary>
    /// The name of the next-month navigation button's icon from the built-in Fluent UI icon set.
    /// </summary>
    [Parameter] public string? NextMonthNavIconName { get; set; }

    /// <summary>
    /// The icon to display inside the next-year navigation button.
    /// Takes precedence over <see cref="NextYearNavIconName"/> when both are set.
    /// </summary>
    [Parameter] public BitIconInfo? NextYearNavIcon { get; set; }

    /// <summary>
    /// The name of the next-year navigation button's icon from the built-in Fluent UI icon set.
    /// </summary>
    [Parameter] public string? NextYearNavIconName { get; set; }

    /// <summary>
    /// The icon to display inside the next-year-range navigation button.
    /// Takes precedence over <see cref="NextYearRangeNavIconName"/> when both are set.
    /// </summary>
    [Parameter] public BitIconInfo? NextYearRangeNavIcon { get; set; }

    /// <summary>
    /// The name of the next-year-range navigation button's icon from the built-in Fluent UI icon set.
    /// </summary>
    [Parameter] public string? NextYearRangeNavIconName { get; set; }

    /// <summary>
    /// The callback for clicking on the DatePicker's input.
    /// </summary>
    [Parameter] public EventCallback OnClick { get; set; }

    /// <summary>
    /// The callback for focusing the DatePicker's input.
    /// </summary>
    [Parameter] public EventCallback OnFocus { get; set; }

    /// <summary>
    /// The callback for when the focus moves into the DatePicker's input.
    /// </summary>
    [Parameter] public EventCallback OnFocusIn { get; set; }

    /// <summary>
    /// The callback for when the focus moves out of the DatePicker's input.
    /// </summary>
    [Parameter] public EventCallback OnFocusOut { get; set; }

    /// <summary>
    /// The callback for when the displayed month of the day picker changes.
    /// The argument is the first day of the newly displayed month.
    /// </summary>
    [Parameter] public EventCallback<DateTimeOffset> OnMonthChange { get; set; }

    /// <summary>
    /// The callback for when the user selects a date.
    /// </summary>
    [Parameter] public EventCallback<DateTimeOffset?> OnSelectDate { get; set; }

    /// <summary>
    /// The custom validation error message for a typed value that falls outside of the
    /// <see cref="MinDate"/> and <see cref="MaxDate"/> range.
    /// </summary>
    [Parameter] public string? OutOfRangeErrorMessage { get; set; }

    /// <summary>
    /// The placeholder text of the DatePicker's input.
    /// </summary>
    [Parameter] public string Placeholder { get; set; } = string.Empty;

    /// <summary>
    /// The icon to display inside the previous-month navigation button.
    /// Takes precedence over <see cref="PrevMonthNavIconName"/> when both are set.
    /// </summary>
    [Parameter] public BitIconInfo? PrevMonthNavIcon { get; set; }

    /// <summary>
    /// The name of the previous-month navigation button's icon from the built-in Fluent UI icon set.
    /// </summary>
    [Parameter] public string? PrevMonthNavIconName { get; set; }

    /// <summary>
    /// The icon to display inside the previous-year navigation button.
    /// Takes precedence over <see cref="PrevYearNavIconName"/> when both are set.
    /// </summary>
    [Parameter] public BitIconInfo? PrevYearNavIcon { get; set; }

    /// <summary>
    /// The name of the previous-year navigation button's icon from the built-in Fluent UI icon set.
    /// </summary>
    [Parameter] public string? PrevYearNavIconName { get; set; }

    /// <summary>
    /// The icon to display inside the previous-year-range navigation button.
    /// Takes precedence over <see cref="PrevYearRangeNavIconName"/> when both are set.
    /// </summary>
    [Parameter] public BitIconInfo? PrevYearRangeNavIcon { get; set; }

    /// <summary>
    /// The name of the previous-year-range navigation button's icon from the built-in Fluent UI icon set.
    /// </summary>
    [Parameter] public string? PrevYearRangeNavIconName { get; set; }

    /// <summary>
    /// Enables the responsive mode in small screens.
    /// </summary>
    [Parameter] public bool Responsive { get; set; }

    /// <summary>
    /// The text of selected date aria-atomic of the DatePicker.
    /// </summary>
    [Parameter] public string SelectedDateAriaAtomic { get; set; } = "Selected date {0}";

    /// <summary>
    /// Whether the clear button should be shown or not when the BitDatePicker has a value.
    /// </summary>
    [Parameter] public bool ShowClearButton { get; set; }

    /// <summary>
    /// Whether the DatePicker's close button should be shown or not.
    /// </summary>
    [Parameter] public bool ShowCloseButton { get; set; }

    /// <summary>
    /// Whether the GoToNow button should be shown or not.
    /// </summary>
    [Parameter] public bool ShowGoToNow { get; set; } = true;

    /// <summary>
    /// Whether the GoToToday button should be shown or not.
    /// </summary>
    [Parameter] public bool ShowGoToToday { get; set; } = true;

    /// <summary>
    /// Show month picker on top of date picker when visible.
    /// </summary>
    [Parameter]
    [CallOnSet(nameof(OnSetParameters))]
    public bool ShowMonthPickerAsOverlay { get; set; }

    /// <summary>
    /// Whether the days of the previous and next months should be shown in the day picker.
    /// </summary>
    [Parameter] public bool ShowOutsideDays { get; set; } = true;

    /// <summary>
    /// Whether or not render the time-picker.
    /// </summary>
    [Parameter]
    [CallOnSet(nameof(OnSetParameters))]
    public bool ShowTimePicker { get; set; }

    /// <summary>
    /// Show the time picker as an overlay on top of the date picker when visible.
    /// </summary>
    [Parameter]
    [CallOnSet(nameof(OnSetParameters))]
    public bool ShowTimePickerAsOverlay { get; set; }

    /// <summary>
    /// The icon to display inside the ShowTimePicker button.
    /// Takes precedence over <see cref="ShowTimePickerIconName"/> when both are set.
    /// </summary>
    [Parameter] public BitIconInfo? ShowTimePickerIcon { get; set; }

    /// <summary>
    /// The name of the ShowTimePicker button's icon from the built-in Fluent UI icon set.
    /// </summary>
    [Parameter] public string? ShowTimePickerIconName { get; set; }

    /// <summary>
    /// The title of the ShowTimePicker button (tooltip).
    /// </summary>
    [Parameter] public string ShowTimePickerTitle { get; set; } = "Show time picker";

    /// <summary>
    /// Whether the week number (weeks 1 to 53) should be shown before each week row.
    /// </summary>
    [Parameter] public bool ShowWeekNumbers { get; set; }

    /// <summary>
    /// The size of the DatePicker.
    /// </summary>
    [Parameter, ResetClassBuilder]
    public BitSize? Size { get; set; }

    /// <summary>
    /// Whether the date-picker is rendered standalone or with the input component and callout.
    /// </summary>
    [Parameter, ResetClassBuilder]
    [CallOnSet(nameof(OnSetParameters))]
    public bool Standalone { get; set; }

    /// <summary>
    /// Specifies the date and time of the date-picker when it is opened without any selected value.
    /// </summary>
    [Parameter]
    [CallOnSet(nameof(OnSetParameters))]
    public DateTimeOffset? StartingValue { get; set; }

    /// <summary>
    /// Custom CSS styles for different parts of the BitDatePicker component.
    /// </summary>
    [Parameter] public BitDatePickerClassStyles? Styles { get; set; }

    /// <summary>
    /// The time format of the time-picker, 24H or 12H.
    /// </summary>
    [Parameter] public BitTimeFormat TimeFormat { get; set; }

    /// <summary>
    /// The icon to display inside the time-picker's decrease-hour button.
    /// Takes precedence over <see cref="TimePickerDecreaseHourIconName"/> when both are set.
    /// </summary>
    [Parameter] public BitIconInfo? TimePickerDecreaseHourIcon { get; set; }

    /// <summary>
    /// The name of the time-picker's decrease-hour button icon from the built-in Fluent UI icon set.
    /// </summary>
    [Parameter] public string? TimePickerDecreaseHourIconName { get; set; }

    /// <summary>
    /// The icon to display inside the time-picker's decrease-minute button.
    /// Takes precedence over <see cref="TimePickerDecreaseMinuteIconName"/> when both are set.
    /// </summary>
    [Parameter] public BitIconInfo? TimePickerDecreaseMinuteIcon { get; set; }

    /// <summary>
    /// The name of the time-picker's decrease-minute button icon from the built-in Fluent UI icon set.
    /// </summary>
    [Parameter] public string? TimePickerDecreaseMinuteIconName { get; set; }

    /// <summary>
    /// The title (tooltip) and the accessible name of the time-picker's hour input.
    /// </summary>
    [Parameter] public string TimePickerHourTitle { get; set; } = "Hour";

    /// <summary>
    /// The icon to display inside the time-picker's increase-hour button.
    /// Takes precedence over <see cref="TimePickerIncreaseHourIconName"/> when both are set.
    /// </summary>
    [Parameter] public BitIconInfo? TimePickerIncreaseHourIcon { get; set; }

    /// <summary>
    /// The name of the time-picker's increase-hour button icon from the built-in Fluent UI icon set.
    /// </summary>
    [Parameter] public string? TimePickerIncreaseHourIconName { get; set; }

    /// <summary>
    /// The icon to display inside the time-picker's increase-minute button.
    /// Takes precedence over <see cref="TimePickerIncreaseMinuteIconName"/> when both are set.
    /// </summary>
    [Parameter] public BitIconInfo? TimePickerIncreaseMinuteIcon { get; set; }

    /// <summary>
    /// The name of the time-picker's increase-minute button icon from the built-in Fluent UI icon set.
    /// </summary>
    [Parameter] public string? TimePickerIncreaseMinuteIconName { get; set; }

    /// <summary>
    /// The title (tooltip) and the accessible name of the time-picker's minute input.
    /// </summary>
    [Parameter] public string TimePickerMinuteTitle { get; set; } = "Minute";

    /// <summary>
    /// TimeZone for the DatePicker.
    /// </summary>
    [Parameter]
    [CallOnSet(nameof(OnSetParameters))]
    public TimeZoneInfo? TimeZone { get; set; }

    /// <summary>
    /// Overrides the current date and time considered as "today" and "now" in the DatePicker
    /// (useful for testing or custom time providers).
    /// </summary>
    [Parameter]
    [CallOnSet(nameof(OnSetParameters))]
    public DateTimeOffset? Today { get; set; }

    /// <summary>
    /// Whether or not the text field of the DatePicker is underlined.
    /// </summary>
    [Parameter, ResetClassBuilder]
    public bool Underlined { get; set; }

    /// <summary>
    /// The rule used to calculate the week numbers. Defaults to the FirstFullWeek rule.
    /// </summary>
    [Parameter] public CalendarWeekRule? WeekNumberRule { get; set; }

    /// <summary>
    /// The title of the week number (tooltip).
    /// </summary>
    [Parameter] public string WeekNumberTitle { get; set; } = "Week number {0}";

    /// <summary>
    /// Custom template to render the year cells of the DatePicker.
    /// </summary>
    [Parameter] public RenderFragment<int>? YearCellTemplate { get; set; }

    /// <summary>
    /// The title of the year picker's toggle (tooltip).
    /// </summary>
    [Parameter] public string YearPickerToggleTitle { get; set; } = "{0}, change year";

    /// <summary>
    /// The title of the year range picker's toggle (tooltip).
    /// </summary>
    [Parameter] public string YearRangePickerToggleTitle { get; set; } = "{0} - {1}, change month";



    [JSInvokable("CloseCallout")]
    public async Task _CloseCalloutBeforeAnotherCalloutIsOpened()
    {
        if (Standalone) return;
        if (IsEnabled is false) return;

        if (await AssignIsOpen(false) is false) return;

        StateHasChanged();
    }

    // The swipe of the responsive mode only reports its end, but the JS side of it calls the whole set,
    // so the three that carry nothing here still have to be there to be called.
    [JSInvokable("OnStart")]
    public Task _OnStart(decimal startX, decimal startY) => Task.CompletedTask;

    [JSInvokable("OnMove")]
    public Task _OnMove(decimal diffX, decimal diffY) => Task.CompletedTask;

    [JSInvokable("OnEnd")]
    public Task _OnEnd(decimal diffX, decimal diffY) => Task.CompletedTask;

    [JSInvokable("OnClose")]
    public async Task _OnClose()
    {
        await CloseCallout();
        await InvokeAsync(StateHasChanged);
    }



    /// <summary>
    /// Opens the callout of the DatePicker exactly as clicking its input would.
    /// </summary>
    public Task OpenCallout()
    {
        // Called from application code, which may well be off the renderer's dispatcher, so the whole
        // body - state mutations and JS interop alike - runs through InvokeAsync.
        return InvokeAsync(HandleOnClick);
    }

    /// <summary>
    /// Closes the callout of the DatePicker and moves the focus back to its input.
    /// </summary>
    public Task CloseCalloutAndFocus()
    {
        return InvokeAsync(async () =>
        {
            await CloseCalloutAndRestoreFocus();
            StateHasChanged();
        });
    }



    protected override string RootElementClass => "bit-dtp";

    protected override void RegisterCssClasses()
    {
        ClassBuilder.Register(() => Classes?.Root);

        ClassBuilder.Register(() => (Dir is null && _culture.TextInfo.IsRightToLeft) ? "bit-rtl" : string.Empty);

        ClassBuilder.Register(GetColorClass);

        ClassBuilder.Register(GetSizeClass);

        ClassBuilder.Register(() => IconLocation is BitIconLocation.Left ? "bit-dtp-lic" : string.Empty);

        ClassBuilder.Register(() => Underlined ? "bit-dtp-und" : string.Empty);

        ClassBuilder.Register(() => HasBorder is false ? "bit-dtp-nbd" : string.Empty);

        ClassBuilder.Register(() => Standalone ? "bit-dtp-sta" : string.Empty);

        ClassBuilder.Register(() => _hasFocus ? $"bit-dtp-foc {Classes?.Focused}" : string.Empty);

        ClassBuilder.Register(() => IsEnabled && Required ? "bit-dtp-req" : string.Empty);
    }

    protected override void RegisterCssStyles()
    {
        StyleBuilder.Register(() => Styles?.Root);

        StyleBuilder.Register(() => _hasFocus ? Styles?.Focused : string.Empty);
    }

    protected override void OnInitialized()
    {
        _datePickerId = $"DatePicker-{UniqueId}";
        _labelId = $"{_datePickerId}-label";
        _calloutId = $"{_datePickerId}-callout";
        _overlayId = $"{_datePickerId}-overlay";
        _inputId = $"{_datePickerId}-input";

        SetDefaultValue();

        OnValueChanged += HandleOnValueChanged;

        OnSetParameters();

        base.OnInitialized();
    }

    protected override void OnParametersSet()
    {
        base.OnParametersSet();

        BuildDatesLookups();
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        await base.OnAfterRenderAsync(firstRender);

        if (firstRender)
        {
            _dotnetObj = DotNetObjectReference.Create(this);

            try
            {
                // Prevents the default behavior (scrolling) of the navigation keys handled by the grid
                // cells' keydown handlers, since Blazor cannot conditionally preventDefault per key, and
                // keeps the focus inside the callout while it is the modal dialog it reports itself to be.
                await _js.BitCalendarsSetup(_calloutId, Standalone is false);

                if (Responsive)
                {
                    await _js.BitSwipesSetup(_calloutId, 0.25m, BitPanelPosition.Top, Dir is BitDir.Rtl, BitSwipeOrientation.Vertical, _dotnetObj);
                }
            }
            catch (JSDisconnectedException) { } // we can ignore this exception here
        }

        if (_focusElementIdAfterRender.HasValue())
        {
            var elementId = _focusElementIdAfterRender!;
            _focusElementIdAfterRender = null;

            try
            {
                await _js.BitCalendarsFocusCell(elementId);
            }
            catch (JSDisconnectedException) { } // we can ignore this exception here
        }

        if (_focusTimePickerAfterRender)
        {
            _focusTimePickerAfterRender = false;

            try
            {
                await _inputTimeHourRef.FocusAsync();
            }
            catch (JSDisconnectedException) { } // we can ignore this exception here
        }
    }

    protected override bool TryParseValueFromString(string? value, [MaybeNullWhen(false)] out DateTimeOffset? result, [NotNullWhen(false)] out string? validationErrorMessage)
    {
        if (value.HasNoValue())
        {
            result = null;
            validationErrorMessage = null;
            return true;
        }

        var pattern = DateFormat ?? GetDefaultDateFormat();

        var parsed = DateTime.TryParseExact(value, pattern, _culture, DateTimeStyles.None, out DateTime parsedValue);

        // When a custom DateFormat is not set and the time picker is enabled, the default pattern
        // includes the time portion. Fall back to a date-only parse so users can still type a bare
        // date (e.g. via AllowTextInput) without being forced to include the time.
        if (parsed is false && DateFormat is null && ShowTimePicker)
        {
            var dateOnlyPattern = Mode == BitDatePickerMode.MonthPicker
                ? _culture.DateTimeFormat.YearMonthPattern
                : _culture.DateTimeFormat.ShortDatePattern;

            parsed = DateTime.TryParseExact(value, dateOnlyPattern, _culture, DateTimeStyles.None, out parsedValue);
        }

        if (parsed)
        {
            // A typed month is a whole month, whose first day can fall before MinDate (or whose last day
            // after MaxDate) while the month itself is still selectable, so it is pulled into the range the
            // same way SelectMonth pulls a month clicked in the calendar. The typed time of day survives it.
            if (Mode == BitDatePickerMode.MonthPicker)
            {
                parsedValue = ClampToRange(parsedValue.Date, _culture.Calendar.GetMonth(parsedValue)) + parsedValue.TimeOfDay;
            }

            // A date typed by hand is the only way a value outside of the allowed range can reach the
            // component (the calendar disables those days), so it is rejected here rather than silently
            // accepted - which would leave the input showing a date the calendar refuses to select.
            if (IsWeekDayOutOfMinAndMaxDate(parsedValue.Date))
            {
                result = default;
                validationErrorMessage = OutOfRangeErrorMessage.HasValue()
                    ? OutOfRangeErrorMessage!
                    : $"The {DisplayName ?? FieldIdentifier.FieldName} field is out of the allowed range.";
                return false;
            }

            // The same goes for the days the calendar disables one by one: typing one of them would
            // otherwise be the way around a rule the picker itself enforces.
            if (IsDayDisabled(parsedValue.Date))
            {
                result = default;
                validationErrorMessage = DisabledDateErrorMessage.HasValue()
                    ? DisabledDateErrorMessage!
                    : $"The {DisplayName ?? FieldIdentifier.FieldName} field is not an allowed date.";
                return false;
            }

            result = new DateTimeOffset(parsedValue, _timeZone.GetUtcOffset(parsedValue));
            validationErrorMessage = null;
            return true;
        }

        result = default;
        validationErrorMessage = InvalidErrorMessage.HasValue() ? InvalidErrorMessage! : $"The {DisplayName ?? FieldIdentifier.FieldName} field is not valid.";
        return false;
    }

    protected override string? FormatValueAsString(DateTimeOffset? value)
    {
        if (value.HasValue is false) return null;

        // The text of the input is the same wall clock the calendar shows, so it is read in the TimeZone
        // of the component rather than in whatever offset the value happens to carry.
        return GetDateTime(value.Value).ToString(DateFormat ?? GetDefaultDateFormat(), _culture);
    }

    private string GetDefaultDateFormat()
    {
        if (Mode == BitDatePickerMode.MonthPicker)
        {
            return _culture.DateTimeFormat.YearMonthPattern;
        }

        var pattern = _culture.DateTimeFormat.ShortDatePattern;

        if (ShowTimePicker)
        {
            pattern = $"{pattern} {GetTimePattern()}";
        }

        return pattern;
    }

    private string GetTimePattern()
    {
        var shortTimePattern = _culture.DateTimeFormat.ShortTimePattern;

        // A lowercase 'h' hour specifier (outside any quoted literal) indicates the culture uses a
        // 12-hour clock, an uppercase 'H' a 24-hour clock.
        var isCulture12Hours = HasSpecifier(shortTimePattern, 'h');

        if (TimeFormat == BitTimeFormat.TwelveHours)
        {
            if (isCulture12Hours) return shortTimePattern;

            // Convert the culture's 24-hour pattern to 12-hour by switching the hour specifier
            // and appending the AM/PM designator.
            return $"{ReplaceSpecifier(shortTimePattern, 'H', 'h')} tt";
        }

        if (isCulture12Hours is false) return shortTimePattern;

        // Convert the culture's 12-hour pattern to 24-hour by switching the hour specifier
        // and removing the AM/PM ('t'/'tt') designator.
        return RemoveSpecifier(ReplaceSpecifier(shortTimePattern, 'h', 'H'), 't');
    }

    // Determines whether the given format specifier appears outside of any quoted literal.
    private static bool HasSpecifier(string pattern, char specifier)
    {
        var quote = '\0';
        foreach (var ch in pattern)
        {
            if (quote != '\0')
            {
                if (ch == quote) quote = '\0';
                continue;
            }

            if (ch is '\'' or '"') { quote = ch; continue; }

            if (ch == specifier) return true;
        }

        return false;
    }

    // Replaces the given format specifier with another, leaving quoted literals untouched.
    private static string ReplaceSpecifier(string pattern, char from, char to)
    {
        var builder = new StringBuilder(pattern.Length);
        var quote = '\0';
        foreach (var ch in pattern)
        {
            if (quote != '\0')
            {
                builder.Append(ch);
                if (ch == quote) quote = '\0';
                continue;
            }

            if (ch is '\'' or '"') { quote = ch; builder.Append(ch); continue; }

            builder.Append(ch == from ? to : ch);
        }

        return builder.ToString();
    }

    // Removes the given format specifier (and any resulting redundant whitespace), leaving quoted literals untouched.
    private static string RemoveSpecifier(string pattern, char specifier)
    {
        var builder = new StringBuilder(pattern.Length);
        var quote = '\0';
        foreach (var ch in pattern)
        {
            if (quote != '\0')
            {
                builder.Append(ch);
                if (ch == quote) quote = '\0';
                continue;
            }

            if (ch is '\'' or '"') { quote = ch; builder.Append(ch); continue; }

            if (ch == specifier) continue;

            builder.Append(ch);
        }

        // Collapse any double spaces left behind by the removed designator and trim the edges.
        return builder.ToString().Replace("  ", " ").Trim();
    }



    private async Task HandleOnClick()
    {
        if (Standalone) return;
        if (IsEnabled is false) return;

        var wasOpen = IsOpen;

        if (await AssignIsOpen(true) is false)
        {
            _focusDayOnOpen = false;
            return;
        }

        ResetPickersState();

        var bodyWidth = await _js.BitUtilsGetBodyWidth();
        var notEnoughWidthAvailable = bodyWidth < MAX_WIDTH;

        if (_showMonthPickerAsOverlayInternal is false)
        {
            _showMonthPickerAsOverlayInternal = notEnoughWidthAvailable;
        }

        if (_showMonthPickerAsOverlayInternal)
        {
            _isMonthPickerOverlayOnTop = false;
        }

        if (_showTimePickerAsOverlayInternal is false)
        {
            _showTimePickerAsOverlayInternal = notEnoughWidthAvailable;
        }

        if (_showTimePickerAsOverlayInternal)
        {
            _isTimePickerOverlayOnTop = false;
        }

        if (CurrentValue.HasValue)
        {
            CheckCurrentCalendarMatchesCurrentValue();
        }

        await ToggleCallout();

        // Only a callout opened from the keyboard takes the focus with it: a pointer press leaves the
        // focus on the input, where a user typing a date (AllowTextInput) expects to keep it.
        if (wasOpen is false && _focusDayOnOpen)
        {
            if (ShowDayPicker())
            {
                _focusedDate = GetFocusableDay();
                _focusElementIdAfterRender = GetDayButtonId(_focusedDate.Value);
            }
            else if (ShowMonthPicker())
            {
                // With no day picker on screen (the MonthPicker mode, or the month picker as an overlay)
                // the month grid is what the keyboard opens onto.
                _focusedMonthCell = GetFocusableMonth();
                _focusElementIdAfterRender = GetMonthButtonId(_focusedMonthCell.Value);
            }
        }

        _focusDayOnOpen = false;

        await OnClick.InvokeAsync();
    }

    // The keys the input answers itself, per the APG combobox pattern: the popup opens with
    // ArrowDown/ArrowUp (with or without Alt) and is dismissed with Escape. Enter and the space bar
    // are deliberately left alone - the first submits the form the input sits in and the second types
    // a space where text input is allowed, and Blazor cannot prevent one default without the other.
    private async Task HandleOnInputKeyDown(KeyboardEventArgs e)
    {
        if (IsEnabled is false) return;

        if (e.Key is "Escape")
        {
            await CloseCalloutAndRestoreFocus();
            return;
        }

        if (IsOpen) return;

        if (e.Key is not ("ArrowDown" or "ArrowUp")) return;

        _focusDayOnOpen = true;

        await HandleOnClick();
    }

    // Escape dismisses the callout from anywhere inside it and hands the focus back to the input,
    // which is where the modal dialog pattern requires the focus to return.
    private async Task HandleOnCalloutKeyDown(KeyboardEventArgs e)
    {
        if (IsEnabled is false) return;
        if (e.Key is not "Escape") return;

        await CloseCalloutAndRestoreFocus();
    }

    private async Task CloseCalloutAndRestoreFocus()
    {
        if (Standalone) return;
        if (IsOpen is false) return;

        await CloseCallout();

        // A refused close (a one-way bound IsOpen) leaves the callout open, so the focus stays in it.
        if (IsOpen) return;

        try
        {
            await InputElement.FocusAsync();
        }
        catch (JSDisconnectedException) { } // we can ignore this exception here
    }

    private async Task HandleOnFocusIn()
    {
        if (IsEnabled is false) return;

        _hasFocus = true;
        ClassBuilder.Reset();
        StyleBuilder.Reset();
        await OnFocusIn.InvokeAsync();
    }

    private async Task HandleOnFocusOut()
    {
        if (IsEnabled is false) return;

        _hasFocus = false;
        ClassBuilder.Reset();
        StyleBuilder.Reset();
        await OnFocusOut.InvokeAsync();
    }

    private async Task HandleOnFocus()
    {
        if (IsEnabled is false) return;

        _hasFocus = true;
        ClassBuilder.Reset();
        StyleBuilder.Reset();
        await OnFocus.InvokeAsync();
    }

    private void HandleOnChange(ChangeEventArgs e)
    {
        if (IsEnabled is false || InvalidValueBinding()) return;
        if (ReadOnly) return;
        if (AllowTextInput is false) return;

        var oldValue = CurrentValue;

        CurrentValueAsString = e.Value?.ToString();

        // The comparison is on the nullable values themselves: text that fails to parse leaves
        // CurrentValue null, and the calendar has nothing to synchronize with in that case.
        if (IsOpen is false || oldValue == CurrentValue || CurrentValue.HasValue is false) return;

        var previousYear = _currentYear;

        CheckCurrentCalendarMatchesCurrentValue();

        // The year range shown by the year picker is anchored on the year of the value, so a typed date
        // that lands in another year has to move that range along with the calendar. The comparison runs
        // on the year of the culture's calendar, which is what the picker itself counts in.
        if (_currentYear != previousYear)
        {
            ChangeYearRanges(_currentYear - 1);
        }
    }

    private async Task HandleOnClearButtonClick()
    {
        if (ReadOnly) return;
        if (IsEnabled is false) return;

        CurrentValue = null;

        _hour = 0;
        _minute = 0;
        _focusedDate = null;

        await InputElement.FocusAsync();
    }

    private void HandleOnValueChanged(object? sender, EventArgs args)
    {
        OnSetParameters();
    }

    private void OnSetParameters()
    {
        _timeZone = TimeZone ?? TimeZoneInfo.Local;
        _culture = Culture ?? CultureInfo.CurrentUICulture;

        var value = CurrentValue.GetValueOrDefault(StartingValue.GetValueOrDefault(GetNow()));

        if (MinDate.HasValue && MinDate > value)
        {
            value = MinDate.Value;
        }

        if (MaxDate.HasValue && MaxDate < value)
        {
            value = MaxDate.Value;
        }

        // Everything the calendar shows - the month it opens on, the time in the time picker - belongs
        // to the TimeZone of the component, not to the offset the value happens to carry.
        var dateTime = GetDateTime(value);

        _hour = CurrentValue.HasValue || StartingValue.HasValue ? dateTime.Hour : 0;
        _minute = CurrentValue.HasValue || StartingValue.HasValue ? dateTime.Minute : 0;

        GenerateCalendarData(dateTime);

        if (Standalone)
        {
            ResetPickersState();

            if (_showMonthPickerAsOverlayInternal)
            {
                _isMonthPickerOverlayOnTop = false;
            }

            if (_showTimePickerAsOverlayInternal)
            {
                _isTimePickerOverlayOnTop = false;
            }

            if (CurrentValue.HasValue)
            {
                CheckCurrentCalendarMatchesCurrentValue();
            }
        }
    }

    private async Task SelectDate(DateTime selectedDate)
    {
        if (ReadOnly) return;
        if (IsEnabled is false || InvalidValueBinding()) return;
        if (IsDayDisabled(selectedDate)) return;

        var previousYear = _currentYear;
        var previousMonth = _currentMonth;

        _focusedDate = selectedDate;

        selectedDate = selectedDate.AddHours(_hour);
        selectedDate = selectedDate.AddMinutes(_minute);

        // With the time picker on screen, picking a day is only half of the value: closing right away
        // would send the user back to reopen the callout to set the time they were about to set.
        // A one-way bound IsOpen cannot be closed by the selection either, so the callout stays open on
        // the date that was just picked instead - the selection itself still goes through.
        if (AutoClose && Standalone is false && ShowTimePicker is false &&
            (IsOpenHasBeenSet is false || IsOpenChanged.HasDelegate))
        {
            await AssignIsOpen(false);

            await ToggleCallout();

            // The day that was activated is inside the callout that just closed, so the focus has to be
            // handed back to the input - otherwise a keyboard selection drops the focus onto the body.
            if (IsOpen is false)
            {
                try
                {
                    await InputElement.FocusAsync();
                }
                catch (JSDisconnectedException) { } // we can ignore this exception here
            }
        }

        CurrentValue = new DateTimeOffset(selectedDate, _timeZone.GetUtcOffset(selectedDate));

        _currentYear = _culture.Calendar.GetYear(selectedDate);
        _currentMonth = _culture.Calendar.GetMonth(selectedDate);

        GenerateMonthData(_currentYear, _currentMonth);

        await OnSelectDate.InvokeAsync(CurrentValue);

        await NotifyMonthChange(previousYear, previousMonth);
    }

    private async Task SelectMonth(int month)
    {
        if (IsEnabled is false) return;
        if (IsMonthOutOfMinAndMaxDate(month)) return;

        var previousYear = _currentYear;
        var previousMonth = _currentMonth;

        _currentMonth = month;

        GenerateMonthData(_currentYear, _currentMonth);

        if (Mode == BitDatePickerMode.MonthPicker)
        {
            var selectedDate = _culture.Calendar.ToDateTime(_currentYear, _currentMonth, 1, 0, 0, 0, 0);

            // The first of the month can fall before MinDate (or after MaxDate) while the month itself is
            // still selectable, so the selection is pulled to the first day of it the range allows.
            selectedDate = ClampToRange(selectedDate, month);

            await SelectDate(selectedDate);
        }
        else if (_showMonthPickerAsOverlayInternal || ShowTimePicker)
        {
            ToggleMonthPickerOverlay();
        }

        await NotifyMonthChange(previousYear, previousMonth);
    }

    private DateTime ClampToRange(DateTime date, int month)
    {
        // The bounds are truncated to their day: what this returns is a day of the calendar, and a time of
        // day carried over from MinDate would both miss the day cell _focusedDate is matched against and
        // be added on top of the hour and minute the time picker contributes in SelectDate.
        if (MinDate.HasValue)
        {
            var minDate = GetDateTime(MinDate.Value).Date;
            if (date < minDate && _culture.Calendar.GetMonth(minDate) == month) return minDate;
        }

        if (MaxDate.HasValue)
        {
            var maxDate = GetDateTime(MaxDate.Value).Date;
            if (date > maxDate && _culture.Calendar.GetMonth(maxDate) == month) return maxDate;
        }

        return date;
    }

    private async Task SelectYear(int year)
    {
        if (IsEnabled is false) return;
        if (IsYearOutOfMinAndMaxDate(year)) return;

        var previousYear = _currentYear;
        var previousMonth = _currentMonth;

        _currentYear = year;

        ChangeYearRanges(_currentYear - 1);

        ClampCurrentMonthToYear();

        GenerateMonthData(_currentYear, _currentMonth);

        ToggleBetweenMonthAndYearPicker();

        // The year that was activated goes away with the year grid, so the focus is handed to the month
        // grid that replaces it - otherwise a keyboard selection drops the focus onto the body.
        FocusMonthCell(GetFocusableMonth());

        await NotifyMonthChange(previousYear, previousMonth);
    }

    private void ToggleBetweenMonthAndYearPicker()
    {
        if (IsEnabled is false) return;

        _showMonthPicker = !_showMonthPicker;

        // The grid that comes into view starts its roving tabindex over, on the month or the year the
        // calendar is actually displaying, rather than on wherever the keyboard left it last time.
        _focusedYearCell = null;
        _focusedMonthCell = null;
    }

    private async Task HandleMonthChange(bool isNext)
    {
        if (IsEnabled is false) return;
        if (CanChangeMonth(isNext) is false) return;

        var previousYear = _currentYear;
        var previousMonth = _currentMonth;

        if (isNext)
        {
            if (_currentMonth < GetMonthsInCurrentYear())
            {
                _currentMonth++;
            }
            else
            {
                _currentYear++;
                _currentMonth = 1;
            }
        }
        else
        {
            if (_currentMonth > 1)
            {
                _currentMonth--;
            }
            else
            {
                _currentYear--;
                _currentMonth = GetMonthsInCurrentYear();
            }
        }

        GenerateMonthData(_currentYear, _currentMonth);

        await NotifyMonthChange(previousYear, previousMonth);
    }

    private async Task HandleYearChange(bool isNext)
    {
        if (IsEnabled is false) return;
        if (CanChangeYear(isNext) is false) return;

        var previousYear = _currentYear;
        var previousMonth = _currentMonth;

        _currentYear += isNext ? +1 : -1;

        ClampCurrentMonthToYear();

        GenerateMonthData(_currentYear, _currentMonth);

        await NotifyMonthChange(previousYear, previousMonth);
    }

    private void HandleYearRangeChange(bool isNext)
    {
        if (IsEnabled is false) return;
        if (CanChangeYearRange(isNext) is false) return;

        var fromYear = _yearPickerStartYear + (isNext ? +12 : -12);

        ChangeYearRanges(fromYear);
    }

    private async Task HandleGoToToday()
    {
        if (IsEnabled is false) return;

        var previousYear = _currentYear;
        var previousMonth = _currentMonth;

        GenerateCalendarData(GetToday());

        await NotifyMonthChange(previousYear, previousMonth);
    }

    private async Task NotifyMonthChange(int previousYear, int previousMonth)
    {
        if (previousYear == _currentYear && previousMonth == _currentMonth) return;
        if (OnMonthChange.HasDelegate is false) return;

        var date = _culture.Calendar.ToDateTime(_currentYear, _currentMonth, 1, 0, 0, 0, 0);

        await OnMonthChange.InvokeAsync(new(date, _timeZone.GetUtcOffset(date)));
    }

    private void GenerateCalendarData(DateTime dateTime)
    {
        _currentMonth = _culture.Calendar.GetMonth(dateTime);
        _currentYear = _culture.Calendar.GetYear(dateTime);

        _yearPickerStartYear = _currentYear - 1;
        _yearPickerEndYear = _currentYear + 10;

        GenerateMonthData(_currentYear, _currentMonth);
    }

    private void GenerateMonthData(int year, int month)
    {
        _monthTitle = $"{_culture.DateTimeFormat.GetMonthName(month)} {year}";

        var calendar = _culture.Calendar;
        var firstDayOfMonth = new DateTime(year, month, 1, calendar);
        int daysInMonth = calendar.GetDaysInMonth(year, month);
        int dayOfWeek = (int)calendar.GetDayOfWeek(firstDayOfMonth);
        int firstDayOfWeek = (int)GetFirstDayOfWeek();

        // Adjust dayOfWeek to match the culture's first day of week
        dayOfWeek = (dayOfWeek - firstDayOfWeek + 7) % 7;

        // The adjacent months are kept as plain year/month numbers of the culture's own calendar: a
        // DateTime built out of them would be a Gregorian date of a year that calendar never had, and a
        // thirteenth month - which a leap year of the Hebrew calendar does have - has no Gregorian
        // counterpart to build at all.
        int monthsInYear = calendar.GetMonthsInYear(year);

        int previousYear = month == 1 ? year - 1 : year;
        int previousMonth = month == 1 ? calendar.GetMonthsInYear(previousYear) : month - 1;
        int daysInPreviousMonth = calendar.GetDaysInMonth(previousYear, previousMonth);

        int nextYear = month == monthsInYear ? year + 1 : year;
        int nextMonth = month == monthsInYear ? 1 : month + 1;

        int day = daysInPreviousMonth - dayOfWeek + 1;

        for (int i = 0; i < 1; i++)
        {
            for (int j = 0; j < dayOfWeek; j++)
            {
                _daysOfCurrentMonth[i, j] = new(previousYear, previousMonth, day, calendar);
                day++;
            }
        }

        day = 1;
        var ended = false;
        for (int i = 0; i < 6; i++)
        {
            for (int j = 0; j < 7; j++)
            {
                if (i == 0 && j < dayOfWeek) continue;

                if (day <= daysInMonth)
                {
                    _daysOfCurrentMonth[i, j] = new(year, month, day, calendar);
                    day++;
                }
                else
                {
                    if (j == 0 && FixedWeeks is false)
                    {
                        ended = true;
                    }
                    _daysOfCurrentMonth[i, j] = ended ? null : new(nextYear, nextMonth, day - daysInMonth, calendar);
                    day++;
                }
            }
        }
    }

    // Moving to another year of a calendar whose years do not all have the same number of months (the
    // Hebrew one) can leave the displayed month past the end of the year that is now displayed.
    private void ClampCurrentMonthToYear()
    {
        var monthsInYear = GetMonthsInCurrentYear();

        if (_currentMonth > monthsInYear)
        {
            _currentMonth = monthsInYear;
        }
    }

    private void ChangeYearRanges(int fromYear)
    {
        _yearPickerStartYear = fromYear;
        _yearPickerEndYear = fromYear + 11;
    }

    private int GetDayOfCurrentMonth(DateTime date)
    {
        return _culture.Calendar.GetDayOfMonth(date);
    }

    private bool IsGoToTodayButtonDisabled(int todayYear, int todayMonth, bool showYearPicker = false)
    {
        if (IsEnabled is false) return true;

        if (showYearPicker)
        {
            return _yearPickerStartYear == todayYear - 1
                && _yearPickerEndYear == todayYear + 10
                && todayMonth == _currentMonth
                && todayYear == _currentYear;
        }
        else
        {
            return todayMonth == _currentMonth
                && todayYear == _currentYear;
        }
    }

    private DayOfWeek GetFirstDayOfWeek()
    {
        return FirstDayOfWeek ?? _culture.DateTimeFormat.FirstDayOfWeek;
    }

    private DayOfWeek GetDayOfWeek(int index)
    {
        int dayOfWeek = (int)GetFirstDayOfWeek() + index;

        if (dayOfWeek > 6)
        {
            dayOfWeek -= 7;
        }

        return (DayOfWeek)dayOfWeek;
    }

    private int GetWeekNumber(int weekIndex)
    {
        return _culture.Calendar.GetWeekOfYear(_daysOfCurrentMonth[weekIndex, 0]!.Value, WeekNumberRule ?? CalendarWeekRule.FirstFullWeek, GetFirstDayOfWeek());
    }

    private void ToggleMonthPickerOverlay()
    {
        _isMonthPickerOverlayOnTop = !_isMonthPickerOverlayOnTop;

        // Each toggle swaps one whole picker for another, taking the button that was activated out of
        // the DOM with it, so the focus has to be handed over to the picker that takes its place.
        _focusedYearCell = null;
        _focusedMonthCell = null;

        MoveFocusToTheVisiblePicker();
    }

    private void ToggleTimePickerOverlay()
    {
        _isTimePickerOverlayOnTop = !_isTimePickerOverlayOnTop;

        MoveFocusToTheVisiblePicker();
    }

    private void MoveFocusToTheVisiblePicker()
    {
        if (ShowDayPicker())
        {
            _focusedDate = GetFocusableDay();
            _focusElementIdAfterRender = GetDayButtonId(_focusedDate.Value);
        }
        else if (ShowMonthPicker() && _showMonthPicker)
        {
            FocusMonthCell(GetFocusableMonth());
        }
        else if (ShowMonthPicker())
        {
            FocusYearCell(GetFocusableYear());
        }
        else if (ShowTimePicker)
        {
            // Nothing but the time picker is left on screen, so its hour input is where the focus goes.
            _focusTimePickerAfterRender = true;
        }
    }

    private bool CanChangeMonth(bool isNext)
    {
        if (IsEnabled is false) return false;

        if (isNext && MaxDate.HasValue)
        {
            var maxDate = GetDateTime(MaxDate.Value);
            var maxDateYear = _culture.Calendar.GetYear(maxDate);
            var maxDateMonth = _culture.Calendar.GetMonth(maxDate);

            if (maxDateYear == _currentYear && maxDateMonth == _currentMonth) return false;
        }


        if (isNext is false && MinDate.HasValue)
        {
            var minDate = GetDateTime(MinDate.Value);
            var minDateYear = _culture.Calendar.GetYear(minDate);
            var minDateMonth = _culture.Calendar.GetMonth(minDate);

            if (minDateYear == _currentYear && minDateMonth == _currentMonth) return false;
        }

        return true;
    }

    private bool CanChangeYear(bool isNext)
    {
        if (IsEnabled is false) return false;

        return (
                (isNext && MaxDate.HasValue && _culture.Calendar.GetYear(GetDateTime(MaxDate.Value)) == _currentYear) ||
                (isNext is false && MinDate.HasValue && _culture.Calendar.GetYear(GetDateTime(MinDate.Value)) == _currentYear)
               ) is false;
    }

    private bool CanChangeYearRange(bool isNext)
    {
        if (IsEnabled is false) return false;

        return (
                (isNext && MaxDate.HasValue && _culture.Calendar.GetYear(GetDateTime(MaxDate.Value)) < _yearPickerStartYear + 12) ||
                (isNext is false && MinDate.HasValue && _culture.Calendar.GetYear(GetDateTime(MinDate.Value)) >= _yearPickerStartYear)
               ) is false;
    }

    // Every caller weighs a day of the calendar, so the comparison is day against day: a MinDate that
    // carries a time of day rules out the days before it, not the day it itself falls on.
    private bool IsWeekDayOutOfMinAndMaxDate(DateTime date)
    {
        if (MaxDate.HasValue)
        {
            if (date.Date > GetDateTime(MaxDate.Value).Date) return true;
        }

        if (MinDate.HasValue)
        {
            if (date.Date < GetDateTime(MinDate.Value).Date) return true;
        }

        return false;
    }

    private bool IsMonthOutOfMinAndMaxDate(int month)
    {
        if (MaxDate.HasValue)
        {
            var maxDate = GetDateTime(MaxDate.Value);
            var maxDateYear = _culture.Calendar.GetYear(maxDate);
            var maxDateMonth = _culture.Calendar.GetMonth(maxDate);

            if (_currentYear > maxDateYear || (_currentYear == maxDateYear && month > maxDateMonth)) return true;
        }

        if (MinDate.HasValue)
        {
            var minDate = GetDateTime(MinDate.Value);
            var minDateYear = _culture.Calendar.GetYear(minDate);
            var minDateMonth = _culture.Calendar.GetMonth(minDate);

            if (_currentYear < minDateYear || (_currentYear == minDateYear && month < minDateMonth)) return true;
        }

        return false;
    }

    private bool IsYearOutOfMinAndMaxDate(int year)
    {
        return (MaxDate.HasValue && year > _culture.Calendar.GetYear(GetDateTime(MaxDate.Value)))
            || (MinDate.HasValue && year < _culture.Calendar.GetYear(GetDateTime(MinDate.Value)));
    }

    private void CheckCurrentCalendarMatchesCurrentValue()
    {
        // The day cells are rendered in the TimeZone of the component, so the month the calendar opens
        // on has to be read in it as well - otherwise a value near midnight opens the month next to the
        // one holding the day that is actually marked as selected.
        var currentValue = GetDateTime(CurrentValue.GetValueOrDefault(GetNow()));
        var currentValueYear = _culture.Calendar.GetYear(currentValue);
        var currentValueMonth = _culture.Calendar.GetMonth(currentValue);

        if (currentValueYear != _currentYear || currentValueMonth != _currentMonth)
        {
            _currentYear = currentValueYear;
            _currentMonth = currentValueMonth;
            GenerateMonthData(_currentYear, _currentMonth);
        }
    }

    private (string style, string klass) GetDayButtonCss(DateTime date)
    {
        StringBuilder klass = new StringBuilder();
        StringBuilder style = new StringBuilder();

        if (CurrentValue.HasValue && date == GetDateTime(CurrentValue.Value).Date)
        {
            klass.Append(" bit-dtp-dbs");

            if (Classes?.SelectedDayButton is not null)
            {
                klass.Append(' ').Append(Classes?.SelectedDayButton);
            }

            AppendStyle(style, Styles?.SelectedDayButton);
        }

        var month = _culture.Calendar.GetMonth(date);

        //Isn't in current month
        if (month != _currentMonth)
        {
            klass.Append(" bit-dtp-dbo");
        }

        //Is highlighted
        if (_highlightedDates.Contains(date.Date))
        {
            klass.Append(" bit-dtp-dhl");

            if (Classes?.HighlightedDayButton is not null)
            {
                klass.Append(' ').Append(Classes?.HighlightedDayButton);
            }

            AppendStyle(style, Styles?.HighlightedDayButton);
        }

        //Is today
        if (month == _currentMonth && date == GetToday().Date)
        {
            klass.Append(" bit-dtp-dtd");

            if (Classes?.TodayDayButton is not null)
            {
                klass.Append(' ').Append(Classes?.TodayDayButton);
            }

            AppendStyle(style, Styles?.TodayDayButton);
        }

        var customClass = GetDayClass?.Invoke(GetDateTimeOfDayCell(date));
        if (customClass.HasValue())
        {
            klass.Append(' ').Append(customClass);
        }

        // The style of every day comes last so it wins over the state specific ones, and it goes
        // through the same appender so it is separated from them by a semicolon.
        AppendStyle(style, Styles?.DayButton);

        return (style.ToString(), klass.ToString());
    }

    // The styles of a day come from more than one state at a time (a selected day that is also today),
    // so each one is closed with a semicolon before the next is appended - without it the last
    // declaration of one and the first of the next would run together into a single invalid one.
    private static void AppendStyle(StringBuilder builder, string? style)
    {
        if (style.HasNoValue()) return;

        if (builder.Length > 0 && builder[^1] != ';')
        {
            builder.Append(';');
        }

        builder.Append(style);
    }

    private string GetMonthCellCssClass(int monthIndex, int todayYear, int todayMonth)
    {
        var className = new StringBuilder();
        if (HighlightCurrentMonth && todayMonth == monthIndex && todayYear == _currentYear)
        {
            className.Append(" bit-dtp-pcm");
        }

        if (HighlightSelectedMonth && _currentMonth == monthIndex)
        {
            className.Append(" bit-dtp-psm");
        }
        else if (Mode == BitDatePickerMode.MonthPicker && CurrentValue.HasValue)
        {
            var selectedValue = GetDateTime(CurrentValue.Value);
            var selectedYear = _culture.Calendar.GetYear(selectedValue);
            var selectedMonth = _culture.Calendar.GetMonth(selectedValue);

            if (selectedYear == _currentYear && selectedMonth == monthIndex)
            {
                className.Append(" bit-dtp-psm");
            }
        }

        return className.ToString();
    }

    private DateTimeOffset GetDateTimeOfDayCell(DateTime date)
    {
        return new(date, _timeZone.GetUtcOffset(date));
    }

    private DateTimeOffset GetDateTimeOfMonthCell(int monthIndex)
    {
        var date = _culture.Calendar.ToDateTime(_currentYear, monthIndex, 1, 0, 0, 0, 0);
        return new(date, _timeZone.GetUtcOffset(date));
    }

    private bool IsSelectedDate(DateTime date)
    {
        if (CurrentValue is null) return false;

        return date == GetDateTime(CurrentValue.Value).Date;
    }

    private void BuildDatesLookups()
    {
        _disabledDates = DisabledDates is null ? [] : DisabledDates.Select(d => GetDateTime(d).Date).ToHashSet();
        _highlightedDates = HighlightedDates is null ? [] : HighlightedDates.Select(d => GetDateTime(d).Date).ToHashSet();
        _disabledDaysOfWeek = DisabledDaysOfWeek is null ? [] : DisabledDaysOfWeek.ToHashSet();
    }

    private bool IsDayDisabled(DateTime date)
    {
        if (IsWeekDayOutOfMinAndMaxDate(date)) return true;

        if (_disabledDaysOfWeek.Contains(date.DayOfWeek)) return true;

        if (_disabledDates.Contains(date.Date)) return true;

        if (IsDateDisabled is not null && IsDateDisabled(GetDateTimeOfDayCell(date))) return true;

        return false;
    }

    private DateTimeOffset GetNow()
    {
        return Today ?? DateTimeOffset.Now;
    }

    private DateTime GetToday()
    {
        return GetDateTime(GetNow());
    }

    private bool IsInCurrentMonth(DateTime date)
    {
        return _culture.Calendar.GetYear(date) == _currentYear && _culture.Calendar.GetMonth(date) == _currentMonth;
    }

    private string GetDayButtonId(DateTime date)
    {
        return FormattableString.Invariant($"{_datePickerId}-day-{date.Year:D4}-{date.Month:D2}-{date.Day:D2}");
    }

    private string GetMonthButtonId(int month)
    {
        return FormattableString.Invariant($"{_datePickerId}-month-{month:D2}");
    }

    private string GetYearButtonId(int year)
    {
        return FormattableString.Invariant($"{_datePickerId}-year-{year:D4}");
    }

    private int GetMonthsInCurrentYear()
    {
        // Not every calendar has twelve months: a leap year of the Hebrew calendar has thirteen.
        return _culture.Calendar.GetMonthsInYear(_currentYear);
    }

    // The single month of the month grid that is in the tab sequence (the roving tabindex of the APG
    // grid pattern): the one the keyboard last landed on, otherwise the month the calendar displays,
    // and as a last resort the first month the Min/Max range allows - the grid must never be
    // unreachable, so a month is always returned even when every one of them is disabled.
    private int GetFocusableMonth()
    {
        var monthsInYear = GetMonthsInCurrentYear();

        if (_focusedMonthCell.HasValue &&
            _focusedMonthCell.Value >= 1 && _focusedMonthCell.Value <= monthsInYear &&
            IsMonthOutOfMinAndMaxDate(_focusedMonthCell.Value) is false) return _focusedMonthCell.Value;

        if (_currentMonth >= 1 && _currentMonth <= monthsInYear &&
            IsMonthOutOfMinAndMaxDate(_currentMonth) is false) return _currentMonth;

        for (var month = 1; month <= monthsInYear; month++)
        {
            if (IsMonthOutOfMinAndMaxDate(month) is false) return month;
        }

        return Math.Clamp(_currentMonth, 1, monthsInYear);
    }

    // The same roving tabindex for the year grid. The displayed year is not always inside the range the
    // year picker shows (browsing the ranges moves the range alone), so the first year of the range is
    // what the tab sequence falls back to.
    private int GetFocusableYear()
    {
        if (_focusedYearCell.HasValue &&
            _focusedYearCell.Value >= _yearPickerStartYear && _focusedYearCell.Value <= _yearPickerEndYear &&
            IsYearOutOfMinAndMaxDate(_focusedYearCell.Value) is false) return _focusedYearCell.Value;

        if (_currentYear >= _yearPickerStartYear && _currentYear <= _yearPickerEndYear &&
            IsYearOutOfMinAndMaxDate(_currentYear) is false) return _currentYear;

        for (var year = _yearPickerStartYear; year <= _yearPickerEndYear; year++)
        {
            if (IsYearOutOfMinAndMaxDate(year) is false) return year;
        }

        return _yearPickerStartYear;
    }

    // The month grid answers the same keys as the day grid, one row being four months wide, and
    // PageUp/PageDown moving to the same month of the adjacent year.
    private async Task HandleMonthKeyDown(KeyboardEventArgs e, int month)
    {
        if (IsEnabled is false) return;

        if (e.Key is "Escape")
        {
            await CloseCalloutAndRestoreFocus();
            return;
        }

        if (e.Key is "PageUp" or "PageDown")
        {
            var isNext = e.Key is "PageDown";

            if (CanChangeYear(isNext) is false) return;

            await HandleYearChange(isNext);

            FocusMonthCell(GetFocusableMonth());
            return;
        }

        var isRtl = IsRtl();

        int? target = e.Key switch
        {
            "ArrowLeft" => FindEnabledMonth(month, isRtl ? 1 : -1),
            "ArrowRight" => FindEnabledMonth(month, isRtl ? -1 : 1),
            "ArrowUp" => FindEnabledMonth(month, -4),
            "ArrowDown" => FindEnabledMonth(month, 4),
            "Home" => FindEnabledMonthFrom(1, 1),
            "End" => FindEnabledMonthFrom(GetMonthsInCurrentYear(), -1),
            _ => null
        };

        if (target.HasValue is false) return;

        FocusMonthCell(target.Value);
    }

    private void FocusMonthCell(int month)
    {
        _focusedMonthCell = month;
        _focusElementIdAfterRender = GetMonthButtonId(month);
    }

    private int? FindEnabledMonth(int from, int step)
    {
        var monthsInYear = GetMonthsInCurrentYear();
        var month = from + step;

        while (month >= 1 && month <= monthsInYear)
        {
            if (IsMonthOutOfMinAndMaxDate(month) is false) return month;

            month += step;
        }

        return null;
    }

    private int? FindEnabledMonthFrom(int from, int step)
    {
        var monthsInYear = GetMonthsInCurrentYear();
        var month = from;

        while (month >= 1 && month <= monthsInYear)
        {
            if (IsMonthOutOfMinAndMaxDate(month) is false) return month;

            month += step;
        }

        return null;
    }

    // The year grid answers the same keys, one row being four years wide, and PageUp/PageDown moving to
    // the adjacent range of years.
    private async Task HandleYearKeyDown(KeyboardEventArgs e, int year)
    {
        if (IsEnabled is false) return;

        if (e.Key is "Escape")
        {
            await CloseCalloutAndRestoreFocus();
            return;
        }

        if (e.Key is "PageUp" or "PageDown")
        {
            var isNext = e.Key is "PageDown";

            if (CanChangeYearRange(isNext) is false) return;

            HandleYearRangeChange(isNext);

            _focusedYearCell = null;
            FocusYearCell(GetFocusableYear());
            return;
        }

        var isRtl = IsRtl();

        int? target = e.Key switch
        {
            "ArrowLeft" => FindEnabledYear(year, isRtl ? 1 : -1),
            "ArrowRight" => FindEnabledYear(year, isRtl ? -1 : 1),
            "ArrowUp" => FindEnabledYear(year, -4),
            "ArrowDown" => FindEnabledYear(year, 4),
            "Home" => FindEnabledYearFrom(_yearPickerStartYear, 1),
            "End" => FindEnabledYearFrom(_yearPickerEndYear, -1),
            _ => null
        };

        if (target.HasValue is false) return;

        FocusYearCell(target.Value);
    }

    private void FocusYearCell(int year)
    {
        _focusedYearCell = year;
        _focusElementIdAfterRender = GetYearButtonId(year);
    }

    private int? FindEnabledYear(int from, int step)
    {
        var year = from + step;

        while (year >= _yearPickerStartYear && year <= _yearPickerEndYear)
        {
            if (IsYearOutOfMinAndMaxDate(year) is false) return year;

            year += step;
        }

        return null;
    }

    private int? FindEnabledYearFrom(int from, int step)
    {
        var year = from;

        while (year >= _yearPickerStartYear && year <= _yearPickerEndYear)
        {
            if (IsYearOutOfMinAndMaxDate(year) is false) return year;

            year += step;
        }

        return null;
    }

    private bool IsRtl()
    {
        return Dir == BitDir.Rtl || (Dir is null && _culture.TextInfo.IsRightToLeft);
    }

    // The single day of the grid that is in the tab sequence (the roving tabindex of the APG grid
    // pattern): the one the keyboard last landed on, otherwise the selection, otherwise today, and as a
    // last resort the first day that can actually be selected - the grid must never be unreachable.
    private DateTime GetFocusableDay()
    {
        if (_focusedDate.HasValue && IsInCurrentMonth(_focusedDate.Value) && IsDayDisabled(_focusedDate.Value) is false) return _focusedDate.Value;

        if (CurrentValue.HasValue)
        {
            var selectedDate = GetDateTime(CurrentValue.Value).Date;
            if (IsInCurrentMonth(selectedDate) && IsDayDisabled(selectedDate) is false) return selectedDate;
        }

        var today = GetToday().Date;
        if (IsInCurrentMonth(today) && IsDayDisabled(today) is false) return today;

        for (var week = 0; week < DEFAULT_WEEK_COUNT; week++)
        {
            for (var day = 0; day < DEFAULT_DAY_COUNT_PER_WEEK; day++)
            {
                var date = _daysOfCurrentMonth[week, day];
                if (date.HasValue && IsInCurrentMonth(date.Value) && IsDayDisabled(date.Value) is false) return date.Value;
            }
        }

        // A month can be disabled from end to end, and a disabled day is not focusable anyway - but the
        // tabindex still has to land on a day of the month itself: with ShowOutsideDays turned off the
        // days around it are not rendered as buttons at all, so pointing at one loses the grid entirely.
        for (var week = 0; week < DEFAULT_WEEK_COUNT; week++)
        {
            for (var day = 0; day < DEFAULT_DAY_COUNT_PER_WEEK; day++)
            {
                var date = _daysOfCurrentMonth[week, day];
                if (date.HasValue && IsInCurrentMonth(date.Value)) return date.Value;
            }
        }

        return today;
    }

    private async Task HandleDayKeyDown(KeyboardEventArgs e, DateTime date)
    {
        if (IsEnabled is false) return;

        if (e.Key is "Escape")
        {
            await CloseCalloutAndRestoreFocus();
            return;
        }

        var isRtl = IsRtl();

        DateTime? target = e.Key switch
        {
            "ArrowLeft" => FindEnabledDay(date, isRtl ? 1 : -1),
            "ArrowRight" => FindEnabledDay(date, isRtl ? -1 : 1),
            "ArrowUp" => FindEnabledDay(date, -7),
            "ArrowDown" => FindEnabledDay(date, 7),
            "Home" => FindEnabledDayTowards(GetStartOfWeek(date), date),
            "End" => FindEnabledDayTowards(GetStartOfWeek(date).AddDays(6), date),
            "PageUp" => FindEnabledDayTowards(e.ShiftKey ? _culture.Calendar.AddYears(date, -1) : _culture.Calendar.AddMonths(date, -1), date),
            "PageDown" => FindEnabledDayTowards(e.ShiftKey ? _culture.Calendar.AddYears(date, 1) : _culture.Calendar.AddMonths(date, 1), date),
            _ => null
        };

        if (target.HasValue is false) return;

        await MoveFocusToDay(target.Value);
    }

    private DateTime? FindEnabledDay(DateTime from, int stepDays)
    {
        var date = from;

        for (var i = 0; i < 366; i++)
        {
            date = date.AddDays(stepDays);

            if (IsWeekDayOutOfMinAndMaxDate(date)) return null;

            if (IsDayDisabled(date) is false) return date;
        }

        return null;
    }

    private DateTime? FindEnabledDayTowards(DateTime target, DateTime origin)
    {
        var step = target < origin ? 1 : -1;
        var date = target;

        while (date != origin)
        {
            if (IsDayDisabled(date) is false) return date;

            date = date.AddDays(step);
        }

        return null;
    }

    private DateTime GetStartOfWeek(DateTime date)
    {
        var diff = ((int)date.DayOfWeek - (int)GetFirstDayOfWeek() + 7) % 7;

        return date.AddDays(-diff);
    }

    private async Task MoveFocusToDay(DateTime target)
    {
        var previousYear = _currentYear;
        var previousMonth = _currentMonth;

        var year = _culture.Calendar.GetYear(target);
        var month = _culture.Calendar.GetMonth(target);

        if (year != _currentYear || month != _currentMonth)
        {
            _currentYear = year;
            _currentMonth = month;

            GenerateMonthData(_currentYear, _currentMonth);
        }

        _focusedDate = target;
        _focusElementIdAfterRender = GetDayButtonId(target);

        await NotifyMonthChange(previousYear, previousMonth);
    }

    private string GetColorClass()
    {
        return Color switch
        {
            BitColor.Primary => "bit-dtp-pri",
            BitColor.Secondary => "bit-dtp-sec",
            BitColor.Tertiary => "bit-dtp-ter",
            BitColor.Info => "bit-dtp-inf",
            BitColor.Success => "bit-dtp-suc",
            BitColor.Warning => "bit-dtp-wrn",
            BitColor.SevereWarning => "bit-dtp-swr",
            BitColor.Error => "bit-dtp-err",
            BitColor.PrimaryBackground => "bit-dtp-pbg",
            BitColor.SecondaryBackground => "bit-dtp-sbg",
            BitColor.TertiaryBackground => "bit-dtp-tbg",
            BitColor.PrimaryForeground => "bit-dtp-pfg",
            BitColor.SecondaryForeground => "bit-dtp-sfg",
            BitColor.TertiaryForeground => "bit-dtp-tfg",
            BitColor.PrimaryBorder => "bit-dtp-pbr",
            BitColor.SecondaryBorder => "bit-dtp-sbr",
            BitColor.TertiaryBorder => "bit-dtp-tbr",
            _ => "bit-dtp-pri"
        };
    }

    private string GetSizeClass()
    {
        return Size switch
        {
            BitSize.Small => "bit-dtp-sm",
            BitSize.Medium => "bit-dtp-md",
            BitSize.Large => "bit-dtp-lg",
            _ => string.Empty
        };
    }

    private Task UpdateCurrentValue()
    {
        if (CurrentValue.HasValue is false) return Task.CompletedTask;

        var currentValue = GetDateTime(CurrentValue.Value);
        var currentValueYear = _culture.Calendar.GetYear(currentValue);
        var currentValueMonth = _culture.Calendar.GetMonth(currentValue);
        var currentValueDay = _culture.Calendar.GetDayOfMonth(currentValue);
        var dateTime = _culture.Calendar.ToDateTime(currentValueYear, currentValueMonth, currentValueDay, _hour, _minute, 0, 0);

        CurrentValue = new(dateTime, _timeZone.GetUtcOffset(dateTime));

        return Task.CompletedTask;
    }

    private DateTime GetDateTime(DateTimeOffset dateTimeOffset)
    {
        return TimeZoneInfo.ConvertTimeFromUtc(dateTimeOffset.UtcDateTime, _timeZone);
    }

    private async Task HandleOnTimeHourFocus()
    {
        if (IsEnabled is false || ShowTimePicker is false || ReadOnly) return;

        await _js.BitUtilsSelectText(_inputTimeHourRef);
    }

    private async Task HandleOnTimeMinuteFocus()
    {
        if (IsEnabled is false || ShowTimePicker is false || ReadOnly) return;

        await _js.BitUtilsSelectText(_inputTimeMinuteRef);
    }

    private async Task HandleOnAmClick()
    {
        if (ReadOnly) return;
        if (IsEnabled is false) return;

        _hour %= 12;  // "12:-- am" is "00:--" in 24h
        await UpdateCurrentValue();
    }

    private async Task HandleOnPmClick()
    {
        if (ReadOnly) return;
        if (IsEnabled is false) return;

        if (_hour <= 12) // "12:-- pm" is "12:--" in 24h
        {
            _hour += 12;
        }

        _hour %= 24;
        await UpdateCurrentValue();
    }

    private bool? IsAm()
    {
        if (CurrentValue.HasValue is false) return null;

        return _hour >= 0 && _hour < 12; // am is 00:00 to 11:59
    }

    private async Task HandleOnPointerDown(bool isNext, bool isHour)
    {
        if (ReadOnly) return;
        if (IsEnabled is false) return;

        await ChangeTime(isNext, isHour);

        if (IsDisposed) return;

        ResetCts();

        var cts = _cancellationTokenSource;
        try
        {
            await Task.Run(async () =>
            {
                await InvokeAsync(async () =>
                {
                    await Task.Delay(400);
                    await ContinuousChangeTime(isNext, isHour, cts);
                });
            }, cts.Token);
        }
        catch (OperationCanceledException) { }
    }

    private async Task ContinuousChangeTime(bool isNext, bool isHour, CancellationTokenSource cts)
    {
        if (cts.IsCancellationRequested || IsDisposed) return;

        await ChangeTime(isNext, isHour);

        if (IsDisposed) return;

        StateHasChanged();

        await Task.Delay(75);
        await ContinuousChangeTime(isNext, isHour, cts);
    }

    private async Task ChangeTime(bool isNext, bool isHour)
    {
        if (isHour)
        {
            await ChangeHour(isNext);
        }
        else
        {
            await ChangeMinute(isNext);
        }
    }

    private void HandleOnPointerUpOrOut()
    {
        ResetCts();
    }

    private void ResetCts()
    {
        if (IsDisposed) return;

        _cancellationTokenSource?.Cancel();
        _cancellationTokenSource?.Dispose();
        _cancellationTokenSource = new();
    }

    private async Task ChangeHour(bool isNext)
    {
        var hourStep = Math.Clamp(Math.Abs(HourStep), 1, 23);

        _hour = (_hour + (isNext ? hourStep : -hourStep) + 24) % 24;

        await UpdateCurrentValue();
    }

    private async Task ChangeMinute(bool isNext)
    {
        var minuteStep = Math.Clamp(Math.Abs(MinuteStep), 1, 59);

        _minute = (_minute + (isNext ? minuteStep : -minuteStep) + 60) % 60;

        await UpdateCurrentValue();
    }

    private async Task CloseCallout()
    {
        if (IsEnabled is false) return;

        if (await AssignIsOpen(false) is false) return;

        await ToggleCallout();

        StateHasChanged();
    }

    private bool ShowDayPicker()
    {
        if (Mode == BitDatePickerMode.MonthPicker) return false;

        if (ShowTimePicker)
        {
            if (ShowTimePickerAsOverlay)
            {
                return _showMonthPickerAsOverlayInternal is false || (_showMonthPickerAsOverlayInternal && _isMonthPickerOverlayOnTop is false && _isTimePickerOverlayOnTop is false);
            }
            else
            {
                return (_showMonthPickerAsOverlayInternal is false && _isMonthPickerOverlayOnTop is false) || (_showTimePickerAsOverlayInternal && _isMonthPickerOverlayOnTop is false && _isTimePickerOverlayOnTop is false);
            }
        }
        else
        {
            return _showMonthPickerAsOverlayInternal is false || (_showMonthPickerAsOverlayInternal && _isMonthPickerOverlayOnTop is false);
        }
    }

    private bool ShowMonthPicker()
    {
        if (Mode == BitDatePickerMode.MonthPicker) return true;

        // The month picker of the MonthPicker mode is the whole component, so only the one that sits
        // next to (or on top of) the day picker can be turned off.
        if (IsMonthPickerVisible is false) return false;

        if (ShowTimePicker)
        {
            if (ShowTimePickerAsOverlay)
            {
                return (_showMonthPickerAsOverlayInternal is false || (_showMonthPickerAsOverlayInternal && _isMonthPickerOverlayOnTop)) && _isTimePickerOverlayOnTop is false;
            }
            else
            {
                return (_showMonthPickerAsOverlayInternal is false && _isMonthPickerOverlayOnTop) || (_showTimePickerAsOverlayInternal && _isMonthPickerOverlayOnTop && _isTimePickerOverlayOnTop is false);
            }
        }
        else
        {
            return _showMonthPickerAsOverlayInternal is false || (_showMonthPickerAsOverlayInternal && _isMonthPickerOverlayOnTop);
        }
    }

    private void ResetPickersState()
    {
        _showMonthPicker = true;
        _isMonthPickerOverlayOnTop = Mode == BitDatePickerMode.MonthPicker;
        // A hidden month picker must not take the day picker's place as an overlay either, so the
        // overlay mode is only entered while the month picker is actually rendered.
        _showMonthPickerAsOverlayInternal = Mode == BitDatePickerMode.MonthPicker ||
                                            (ShowMonthPickerAsOverlay && IsMonthPickerVisible);
        _isTimePickerOverlayOnTop = false;
        _showTimePickerAsOverlayInternal = ShowTimePickerAsOverlay;
        _focusedYearCell = null;
        _focusedMonthCell = null;
    }

    private async Task<bool> ToggleCallout()
    {
        if (Standalone) return false;
        if (IsEnabled is false || IsDisposed) return false;
        // The reference is created on the first render, so nothing can toggle the callout before it.
        if (_dotnetObj is null) return false;

        return await _js.BitCalloutToggleCallout(
            dotnetObj: _dotnetObj,
            componentId: _datePickerId,
            component: null,
            calloutId: _calloutId,
            callout: null,
            overlayId: _overlayId,
            isCalloutOpen: IsOpen,
            responsiveMode: Responsive ? BitResponsiveMode.Top : BitResponsiveMode.None,
            dropDirection: BitDropDirection.TopAndBottom,
            isRtl: Dir is BitDir.Rtl,
            scrollContainerId: "",
            scrollOffset: 0,
            headerId: "",
            footerId: "",
            setCalloutWidth: false,
            fixedCalloutWidth: false,
            maxWindowWidth: MAX_WIDTH);
    }

    private string GetCalloutCssClasses()
    {
        // The callout is rendered outside of the root element (and is reparented to the body while it is
        // open), so the custom properties of the color and the size have to be declared on it as well -
        // nothing of the root cascades down to it.
        List<string> classes = ["bit-dtp-cal", GetColorClass()];

        var sizeClass = GetSizeClass();
        if (sizeClass.HasValue())
        {
            classes.Add(sizeClass);
        }

        if (Classes?.Callout is not null)
        {
            classes.Add(Classes.Callout);
        }

        if (Standalone)
        {
            classes.Add("bit-dtp-sta");
        }

        if (Responsive)
        {
            classes.Add("bit-dtp-res");
        }

        if (IsRtl())
        {
            classes.Add("bit-dtp-rtl");
        }

        return string.Join(' ', classes).Trim();
    }

    private async Task HandleGoToNow()
    {
        if (ReadOnly) return;
        if (IsEnabled is false) return;

        var now = GetToday();

        _hour = now.Hour;
        _minute = now.Minute;

        await UpdateCurrentValue();
    }



    protected override async ValueTask DisposeAsync(bool disposing)
    {
        if (IsDisposed || disposing is false) return;

        await base.DisposeAsync(disposing);

        _cancellationTokenSource?.Cancel();
        _cancellationTokenSource?.Dispose();
        OnValueChanged -= HandleOnValueChanged;

        try
        {
            await _js.BitCalloutClearCallout(_calloutId);
            await _js.BitSwipesDispose(_calloutId);
            await _js.BitCalendarsDispose(_calloutId);
        }
        catch (JSDisconnectedException) { } // we can ignore this exception here

        _dotnetObj?.Dispose();
    }
}
