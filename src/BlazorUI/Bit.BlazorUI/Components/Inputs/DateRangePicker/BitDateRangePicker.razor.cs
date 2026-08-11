using System.Text;
using System.Globalization;
using System.Diagnostics.CodeAnalysis;

namespace Bit.BlazorUI;

/// <summary>
/// A BitDateRangePicker offers a drop-down control that’s optimized for picking two dates from a calendar view where contextual information like the day of the week or fullness of the calendar is important.
/// </summary>
public partial class BitDateRangePicker : BitInputBase<BitDateRangePickerValue?>
{
    private const int MAX_WIDTH = 470;
    private const int MONTH_WIDTH = 240;
    private const int PRESETS_WIDTH = 130;
    private const int MAX_MONTH_COUNT = 3;
    private const int DEFAULT_WEEK_COUNT = 6;
    private const int DEFAULT_DAY_COUNT_PER_WEEK = 7;
    private const int MAX_EXCLUDED_SCAN_DAYS = 1100;



    private bool _hasFocus;
    private int _currentYear;
    private int _currentMonth;
    private int _yearPickerEndYear;
    private int _yearPickerStartYear;
    private bool _focusAfterRender;
    private DateTime? _focusedDate;
    private DateTime? _hoveredDate;
    private HashSet<DateTime> _disabledDates = [];
    private HashSet<DateTime> _highlightedDates = [];
    private HashSet<DayOfWeek> _disabledDaysOfWeek = [];
    private bool _showMonthPicker = true;
    private bool _isTimePickerOverlayOnTop;
    private bool _isMonthPickerOverlayOnTop;
    private bool _showTimePickerAsOverlayInternal;
    private bool _showMonthPickerAsOverlayInternal;
    private BitDateRangePickerPreset? _selectedPreset;
    private TimeZoneInfo _timeZone = TimeZoneInfo.Local;
    private CultureInfo _culture = CultureInfo.CurrentUICulture;
    private CancellationTokenSource _cancellationTokenSource = new();
    private DotNetObjectReference<BitDateRangePicker>? _dotnetObj;

    // The closest excluded day on each side of the picked start date, which bounds how far the range
    // can reach with ExcludeDisabledDates on without rescanning the span for every rendered cell.
    private DateTime? _excludedLowerBound;
    private DateTime? _excludedUpperBound;

    // The number of months actually rendered side by side, which drops back to a single month
    // when the viewport cannot fit the requested MonthCount.
    private int _monthCount = 1;
    private int _fittingMonthCount = MAX_MONTH_COUNT;
    private string[] _monthTitles = [string.Empty];
    private DateTime?[][,] _daysOfMonths = [new DateTime?[DEFAULT_WEEK_COUNT, DEFAULT_DAY_COUNT_PER_WEEK]];

    private string? _labelId;
    private string? _inputId;
    private string _calloutId = string.Empty;
    private string _overlayId = string.Empty;
    private string _dateRangePickerId = string.Empty;
    private ElementReference _startTimeHourInputRef = default!;
    private ElementReference _startTimeMinuteInputRef = default!;
    private ElementReference _endTimeHourInputRef = default!;
    private ElementReference _endTimeMinuteInputRef = default!;



    private int _startTimeHour;
    private int _startTimeHourView
    {
        get
        {
            if (TimeFormat == BitTimeFormat.TwelveHours)
            {
                if (_startTimeHour > 12)
                {
                    return _startTimeHour - 12;
                }

                if (_startTimeHour == 0)
                {
                    return 12;
                }
            }

            return _startTimeHour;
        }
        set
        {
            if (value > 23)
            {
                value = 23;
            }
            else if (value < 0)
            {
                value = 0;
            }

            if (TimeFormat == BitTimeFormat.TwelveHours && value <= 12)
            {
                // The input carries the 12-hour face value, so it has to be mapped back into the
                // 24-hour hour without flipping the currently selected AM/PM period.
                value %= 12;

                if (IsAm(_startTimeHour) is false)
                {
                    value += 12;
                }
            }

            if (CanChangeTime(startTimeHour: value) is false) return;

            _startTimeHour = value;

            UpdateTime();
        }
    }

    private int _startTimeMinute;
    private int _startTimeMinuteView
    {
        get => _startTimeMinute;
        set
        {
            if (value > 59)
            {
                value = 59;
            }
            else if (value < 0)
            {
                value = 0;
            }

            if (CanChangeTime(startTimeMinute: value) is false) return;

            _startTimeMinute = value;

            UpdateTime();
        }
    }

    private int _endTimeHour;
    private int _endTimeHourView
    {
        get
        {
            if (TimeFormat == BitTimeFormat.TwelveHours)
            {
                if (_endTimeHour > 12)
                {
                    return _endTimeHour - 12;
                }

                if (_endTimeHour == 0)
                {
                    return 12;
                }
            }

            return _endTimeHour;
        }
        set
        {
            if (value > 23)
            {
                value = 23;
            }
            else if (value < 0)
            {
                value = 0;
            }

            if (TimeFormat == BitTimeFormat.TwelveHours && value <= 12)
            {
                // The input carries the 12-hour face value, so it has to be mapped back into the
                // 24-hour hour without flipping the currently selected AM/PM period.
                value %= 12;

                if (IsAm(_endTimeHour) is false)
                {
                    value += 12;
                }
            }

            if (CanChangeTime(endTimeHour: value) is false) return;

            _endTimeHour = value;

            UpdateTime();
        }
    }

    private int _endTimeMinute;
    private int _endTimeMinuteView
    {
        get => _endTimeMinute;
        set
        {

            if (value > 59)
            {
                value = 59;
            }
            else if (value < 0)
            {
                value = 0;
            }

            if (CanChangeTime(endTimeMinute: value) is false) return;

            _endTimeMinute = value;

            UpdateTime();
        }
    }



    [Inject] private IJSRuntime _js { get; set; } = default!;



    /// <summary>
    /// Whether or not the DateRangePicker allows string date inputs. A typed range is validated against
    /// every restriction the calendar enforces (MinDate, MaxDate, MinRange, MaxRange, the disabled days
    /// and ExcludeDisabledDates), so an out-of-bounds range is rejected as an invalid value.
    /// </summary>
    [Parameter] public bool AllowTextInput { get; set; }

    /// <summary>
    /// Whether the DateRangePicker closes automatically after selecting the second value.
    /// </summary>
    [Parameter] public bool AutoClose { get; set; } = true;

    /// <summary>
    /// Aria label of the DateRangePicker's callout for screen readers.
    /// </summary>
    [Parameter] public string CalloutAriaLabel { get; set; } = "Calendar";

    /// <summary>
    /// Capture and render additional html attributes for the DateRangePicker's callout.
    /// </summary>
    [Parameter] public Dictionary<string, object> CalloutHtmlAttributes { get; set; } = [];

    /// <summary>
    /// Custom CSS classes for different parts of the BitDateRangePicker component.
    /// </summary>
    [Parameter] public BitDateRangePickerClassStyles? Classes { get; set; }

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
    /// The title and the aria-label of the clear button.
    /// </summary>
    [Parameter] public string ClearButtonTitle { get; set; } = "Clear the selected date range";

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
    /// The title of the close button (tooltip).
    /// </summary>
    [Parameter] public string CloseButtonTitle { get; set; } = "Close date range picker";

    /// <summary>
    /// The general color of the DateRangePicker that applies to the today day button, the selected range,
    /// the highlighted current month and the selected AM/PM buttons.
    /// </summary>
    [Parameter, ResetClassBuilder]
    public BitColor? Color { get; set; }

    /// <summary>
    /// The delay in milliseconds before the hour/minute starts changing continuously while an
    /// increase/decrease button of the time picker is held down.
    /// </summary>
    [Parameter] public int ContinuousSpinDelay { get; set; } = 400;

    /// <summary>
    /// CultureInfo for the DateRangePicker.
    /// </summary>
    [Parameter, ResetClassBuilder]
    [CallOnSet(nameof(OnSetParameters))]
    public CultureInfo? Culture { get; set; }

    /// <summary>
    /// The format of the date in the DateRangePicker.
    /// </summary>
    [Parameter] public string? DateFormat { get; set; }

    /// <summary>
    /// Custom template to render the day cells of the DateRangePicker.
    /// </summary>
    [Parameter] public RenderFragment<DateTimeOffset>? DayCellTemplate { get; set; }

    /// <summary>
    /// The list of dates that are disabled (not selectable) in the DateRangePicker, in addition to MinDate and MaxDate.
    /// </summary>
    [Parameter]
    [CallOnSet(nameof(OnSetParameters))]
    public IEnumerable<DateTimeOffset>? DisabledDates { get; set; }

    /// <summary>
    /// The days of the week that are disabled (not selectable) in the DateRangePicker (e.g. weekends).
    /// </summary>
    [Parameter]
    [CallOnSet(nameof(OnSetParameters))]
    public IEnumerable<DayOfWeek>? DisabledDaysOfWeek { get; set; }

    /// <summary>
    /// Whether the disabled days are excluded from the selected range. By default a range simply spans over
    /// the disabled days between its two ends. When enabled, once the start date is picked every day whose
    /// range would contain a disabled day becomes unselectable, so the produced range never covers one.
    /// </summary>
    [Parameter]
    [CallOnSet(nameof(OnSetParameters))]
    public bool ExcludeDisabledDates { get; set; }

    /// <summary>
    /// Overrides the first day of the week in the day picker. If not set, the first day of the week of the Culture is used.
    /// </summary>
    [Parameter]
    [CallOnSet(nameof(OnSetParameters))]
    public DayOfWeek? FirstDayOfWeek { get; set; }

    /// <summary>
    /// Whether the day picker should always render six weeks, filling the extra rows with the days of the adjacent months,
    /// to keep the calendar height fixed while navigating between months. It is always on when <see cref="MonthCount"/>
    /// renders more than one month, so the months keep an even height next to each other.
    /// </summary>
    [Parameter]
    [CallOnSet(nameof(OnSetParameters))]
    public bool FixedWeeks { get; set; }

    /// <summary>
    /// Custom function to provide additional CSS classes for each day button of the DateRangePicker.
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
    /// Determines if the DateRangePicker has a border.
    /// </summary>
    [Parameter, ResetClassBuilder]
    public bool HasBorder { get; set; } = true;

    /// <summary>
    /// Whether the month picker should highlight the current month.
    /// </summary>
    [Parameter] public bool HighlightCurrentMonth { get; set; }

    /// <summary>
    /// The list of dates that are highlighted (marked) in the day picker of the DateRangePicker.
    /// </summary>
    [Parameter]
    [CallOnSet(nameof(OnSetParameters))]
    public IEnumerable<DateTimeOffset>? HighlightedDates { get; set; }

    /// <summary>
    /// Whether the month picker should highlight the selected month.
    /// </summary>
    [Parameter] public bool HighlightSelectedMonth { get; set; }

    /// <summary>
    /// Custom template for the DateRangePicker's icon.
    /// </summary>
    [Parameter] public RenderFragment? IconTemplate { get; set; }

    /// <summary>
    /// Determines the location of the DateRangePicker's icon.
    /// </summary>
    [Parameter, ResetClassBuilder]
    public BitIconLocation IconLocation { get; set; } = BitIconLocation.Right;

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
    [Parameter] public BitIconInfo? Icon { get; set; }

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
    [Parameter] public string? IconName { get; set; }

    /// <summary>
    /// The custom validation error message for the invalid value.
    /// </summary>
    [Parameter] public string? InvalidErrorMessage { get; set; }

    /// <summary>
    /// Custom function to determine if a specific date is disabled (not selectable) in the DateRangePicker.
    /// </summary>
    [Parameter]
    [CallOnSet(nameof(OnSetParameters))]
    public Func<DateTimeOffset, bool>? IsDateDisabled { get; set; }

    /// <summary>
    /// Whether the month picker is shown or hidden.
    /// </summary>
    [Parameter]
    [CallOnSet(nameof(OnSetParameters))]
    public bool IsMonthPickerVisible { get; set; } = true;

    /// <summary>
    /// Whether or not the DateRangePicker's callout is open.
    /// </summary>
    [Parameter, TwoWayBound, ResetClassBuilder]
    public bool IsOpen { get; set; }

    /// <summary>
    /// The text of the DateRangePicker's label.
    /// </summary>
    [Parameter] public string? Label { get; set; }

    /// <summary>
    /// Custom template for the DateRangePicker's label.
    /// </summary>
    [Parameter] public RenderFragment? LabelTemplate { get; set; }

    /// <summary>
    /// The maximum date allowed for the DateRangePicker.
    /// </summary>
    [Parameter]
    [CallOnSet(nameof(OnSetParameters))]
    public DateTimeOffset? MaxDate { get; set; }

    /// <summary>
    /// The minimum date allowed for the DateRangePicker.
    /// </summary>
    [Parameter]
    [CallOnSet(nameof(OnSetParameters))]
    public DateTimeOffset? MinDate { get; set; }

    /// <summary>
    /// Custom template to render the month cells of the DateRangePicker.
    /// </summary>
    [Parameter] public RenderFragment<DateTimeOffset>? MonthCellTemplate { get; set; }

    /// <summary>
    /// The number of consecutive months rendered side by side in the day picker (1 to 3), which makes
    /// picking a range that spans two months a single move. It falls back to a single month whenever
    /// the viewport is not wide enough to fit them all.
    /// </summary>
    [Parameter]
    [CallOnSet(nameof(OnSetParameters))]
    public int MonthCount { get; set; } = 1;

    /// <summary>
    /// The title of the month picker's toggle (tooltip).
    /// </summary>
    [Parameter] public string MonthPickerToggleTitle { get; set; } = "{0}, change month";

    /// <summary>
    /// The text rendered in place of a date that has not been picked yet, which is also the token
    /// accepted back for an open-ended range when <see cref="AllowTextInput"/> is enabled.
    /// </summary>
    [Parameter] public string NoDateText { get; set; } = "---";

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
    /// The callback for clicking on the DateRangePicker's input.
    /// </summary>
    [Parameter] public EventCallback OnClick { get; set; }

    /// <summary>
    /// Whether the previous and next navigation buttons move the calendar by all of its rendered months
    /// instead of one, so consecutive pages of a multi-month calendar never overlap.
    /// It has no effect when <see cref="MonthCount"/> renders a single month.
    /// </summary>
    [Parameter] public bool PagedNavigation { get; set; }

    /// <summary>
    /// Callback for when the displayed month of the day picker changes.
    /// The argument is the first day of the newly displayed month.
    /// </summary>
    [Parameter] public EventCallback<DateTimeOffset> OnMonthChange { get; set; }

    /// <summary>
    /// The callback for focusing the DateRangePicker's input.
    /// </summary>
    [Parameter] public EventCallback OnFocus { get; set; }

    /// <summary>
    /// The callback for when the focus moves into the DateRangePicker's input.
    /// </summary>
    [Parameter] public EventCallback OnFocusIn { get; set; }

    /// <summary>
    /// The callback for when the focus moves out of the DateRangePicker's input.
    /// </summary>
    [Parameter] public EventCallback OnFocusOut { get; set; }

    /// <summary>
    /// The callback for when a preset is selected. The argument is the selected preset.
    /// </summary>
    [Parameter] public EventCallback<BitDateRangePickerPreset> OnPresetSelect { get; set; }

    /// <summary>
    /// The placeholder text of the DateRangePicker's input.
    /// </summary>
    [Parameter] public string Placeholder { get; set; } = string.Empty;

    /// <summary>
    /// The list of shortcuts, rendered next to the calendar, that fill the DateRangePicker
    /// with a predefined range (e.g. "Last 7 days").
    /// </summary>
    [Parameter] public IEnumerable<BitDateRangePickerPreset>? Presets { get; set; }

    /// <summary>
    /// The aria label of the presets' container for screen readers.
    /// </summary>
    [Parameter] public string PresetsAriaLabel { get; set; } = "Predefined date ranges";

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
    /// The aria-atomic live text announcing the currently selected date range, formatted with the value of the input.
    /// </summary>
    [Parameter] public string SelectedDateAriaAtomic { get; set; } = "Selected date range {0}";

    /// <summary>
    /// Whether the DateRangePicker's close button should be shown or not.
    /// </summary>
    [Parameter] public bool ShowCloseButton { get; set; }

    /// <summary>
    /// Whether the GoToToday button should be shown or not.
    /// </summary>
    [Parameter] public bool ShowGoToToday { get; set; } = true;

    /// <summary>
    /// Show month picker on top of date range picker when visible.
    /// </summary>
    [Parameter]
    [CallOnSet(nameof(OnSetParameters))]
    public bool ShowMonthPickerAsOverlay { get; set; }

    /// <summary>
    /// Whether or not render the time-picker.
    /// </summary>
    [Parameter]
    [CallOnSet(nameof(OnSetParameters))]
    public bool ShowTimePicker { get; set; }

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
    /// Whether the days of the previous and next months, filling the first and last week rows, should be rendered.
    /// It has no effect when <see cref="MonthCount"/> renders more than one month, since those days would then
    /// show up in two grids at once.
    /// </summary>
    [Parameter] public bool ShowOutsideDays { get; set; } = true;

    /// <summary>
    /// Whether the week number (weeks 1 to 53) should be shown before each week row.
    /// </summary>
    [Parameter] public bool ShowWeekNumbers { get; set; }

    /// <summary>
    /// The title and the aria-label of the start time-picker's increase-hour button.
    /// </summary>
    [Parameter] public string StartTimeIncreaseHourTitle { get; set; } = "Increase start hour";

    /// <summary>
    /// The title and the aria-label of the start time-picker's decrease-hour button.
    /// </summary>
    [Parameter] public string StartTimeDecreaseHourTitle { get; set; } = "Decrease start hour";

    /// <summary>
    /// The title and the aria-label of the start time-picker's increase-minute button.
    /// </summary>
    [Parameter] public string StartTimeIncreaseMinuteTitle { get; set; } = "Increase start minute";

    /// <summary>
    /// The title and the aria-label of the start time-picker's decrease-minute button.
    /// </summary>
    [Parameter] public string StartTimeDecreaseMinuteTitle { get; set; } = "Decrease start minute";

    /// <summary>
    /// The title and the aria-label of the end time-picker's increase-hour button.
    /// </summary>
    [Parameter] public string EndTimeIncreaseHourTitle { get; set; } = "Increase end hour";

    /// <summary>
    /// The title and the aria-label of the end time-picker's decrease-hour button.
    /// </summary>
    [Parameter] public string EndTimeDecreaseHourTitle { get; set; } = "Decrease end hour";

    /// <summary>
    /// The title and the aria-label of the end time-picker's increase-minute button.
    /// </summary>
    [Parameter] public string EndTimeIncreaseMinuteTitle { get; set; } = "Increase end minute";

    /// <summary>
    /// The title and the aria-label of the end time-picker's decrease-minute button.
    /// </summary>
    [Parameter] public string EndTimeDecreaseMinuteTitle { get; set; } = "Decrease end minute";

    /// <summary>
    /// The aria-label of the start time-picker's hour input.
    /// </summary>
    [Parameter] public string StartTimeHourInputAriaLabel { get; set; } = "Start hour";

    /// <summary>
    /// The aria-label of the start time-picker's minute input.
    /// </summary>
    [Parameter] public string StartTimeMinuteInputAriaLabel { get; set; } = "Start minute";

    /// <summary>
    /// The aria-label of the end time-picker's hour input.
    /// </summary>
    [Parameter] public string EndTimeHourInputAriaLabel { get; set; } = "End hour";

    /// <summary>
    /// The aria-label of the end time-picker's minute input.
    /// </summary>
    [Parameter] public string EndTimeMinuteInputAriaLabel { get; set; } = "End minute";

    /// <summary>
    /// The icon to display inside the start time-picker's decrease-hour button.
    /// Takes precedence over <see cref="StartTimeDecreaseHourIconName"/> when both are set.
    /// </summary>
    [Parameter] public BitIconInfo? StartTimeDecreaseHourIcon { get; set; }

    /// <summary>
    /// The name of the start time-picker's decrease-hour button icon from the built-in Fluent UI icon set.
    /// </summary>
    [Parameter] public string? StartTimeDecreaseHourIconName { get; set; }

    /// <summary>
    /// The icon to display inside the start time-picker's decrease-minute button.
    /// Takes precedence over <see cref="StartTimeDecreaseMinuteIconName"/> when both are set.
    /// </summary>
    [Parameter] public BitIconInfo? StartTimeDecreaseMinuteIcon { get; set; }

    /// <summary>
    /// The name of the start time-picker's decrease-minute button icon from the built-in Fluent UI icon set.
    /// </summary>
    [Parameter] public string? StartTimeDecreaseMinuteIconName { get; set; }

    /// <summary>
    /// The icon to display inside the start time-picker's increase-hour button.
    /// Takes precedence over <see cref="StartTimeIncreaseHourIconName"/> when both are set.
    /// </summary>
    [Parameter] public BitIconInfo? StartTimeIncreaseHourIcon { get; set; }

    /// <summary>
    /// The name of the start time-picker's increase-hour button icon from the built-in Fluent UI icon set.
    /// </summary>
    [Parameter] public string? StartTimeIncreaseHourIconName { get; set; }

    /// <summary>
    /// The icon to display inside the start time-picker's increase-minute button.
    /// Takes precedence over <see cref="StartTimeIncreaseMinuteIconName"/> when both are set.
    /// </summary>
    [Parameter] public BitIconInfo? StartTimeIncreaseMinuteIcon { get; set; }

    /// <summary>
    /// The name of the start time-picker's increase-minute button icon from the built-in Fluent UI icon set.
    /// </summary>
    [Parameter] public string? StartTimeIncreaseMinuteIconName { get; set; }

    /// <summary>
    /// The icon to display inside the end time-picker's decrease-hour button.
    /// Takes precedence over <see cref="EndTimeDecreaseHourIconName"/> when both are set.
    /// </summary>
    [Parameter] public BitIconInfo? EndTimeDecreaseHourIcon { get; set; }

    /// <summary>
    /// The name of the end time-picker's decrease-hour button icon from the built-in Fluent UI icon set.
    /// </summary>
    [Parameter] public string? EndTimeDecreaseHourIconName { get; set; }

    /// <summary>
    /// The icon to display inside the end time-picker's decrease-minute button.
    /// Takes precedence over <see cref="EndTimeDecreaseMinuteIconName"/> when both are set.
    /// </summary>
    [Parameter] public BitIconInfo? EndTimeDecreaseMinuteIcon { get; set; }

    /// <summary>
    /// The name of the end time-picker's decrease-minute button icon from the built-in Fluent UI icon set.
    /// </summary>
    [Parameter] public string? EndTimeDecreaseMinuteIconName { get; set; }

    /// <summary>
    /// The icon to display inside the end time-picker's increase-hour button.
    /// Takes precedence over <see cref="EndTimeIncreaseHourIconName"/> when both are set.
    /// </summary>
    [Parameter] public BitIconInfo? EndTimeIncreaseHourIcon { get; set; }

    /// <summary>
    /// The name of the end time-picker's increase-hour button icon from the built-in Fluent UI icon set.
    /// </summary>
    [Parameter] public string? EndTimeIncreaseHourIconName { get; set; }

    /// <summary>
    /// The icon to display inside the end time-picker's increase-minute button.
    /// Takes precedence over <see cref="EndTimeIncreaseMinuteIconName"/> when both are set.
    /// </summary>
    [Parameter] public BitIconInfo? EndTimeIncreaseMinuteIcon { get; set; }

    /// <summary>
    /// The name of the end time-picker's increase-minute button icon from the built-in Fluent UI icon set.
    /// </summary>
    [Parameter] public string? EndTimeIncreaseMinuteIconName { get; set; }

    /// <summary>
    /// Determines increment/decrement steps for DateRangePicker's hour.
    /// </summary>
    [Parameter] public int HourStep { get; set; } = 1;

    /// <summary>
    /// The maximum range of day and times allowed for selection in DateRangePicker.
    /// </summary>
    [Parameter] public TimeSpan? MaxRange { get; set; }

    /// <summary>
    /// The minimum number of days that the selected range must span in the DateRangePicker.
    /// Only the days part of the provided TimeSpan is considered.
    /// </summary>
    [Parameter] public TimeSpan? MinRange { get; set; }

    /// <summary>
    /// Whether the clear button should be shown or not when the DateRangePicker has a value.
    /// </summary>
    [Parameter] public bool ShowClearButton { get; set; }

    /// <summary>
    /// Show the time picker as an overlay on top of the date range picker when visible.
    /// </summary>
    [Parameter]
    [CallOnSet(nameof(OnSetParameters))]
    public bool ShowTimePickerAsOverlay { get; set; }

    /// <summary>
    /// Whether the DateRangePicker is rendered standalone or with the input component and callout.
    /// </summary>
    [Parameter, ResetClassBuilder]
    [CallOnSet(nameof(OnSetParameters))]
    public bool Standalone { get; set; }

    /// <summary>
    /// Specifies the date and time of the date and time picker when it is opened without any selected value.
    /// </summary>
    [Parameter]
    [CallOnSet(nameof(OnSetParameters))]
    public BitDateRangePickerValue? StartingValue { get; set; }

    /// <summary>
    /// Custom CSS styles for different parts of the BitDateRangePicker component.
    /// </summary>
    [Parameter] public BitDateRangePickerClassStyles? Styles { get; set; }

    /// <summary>
    /// Time format of the time-pickers, 24H or 12H.
    /// </summary>
    [Parameter] public BitTimeFormat TimeFormat { get; set; }

    /// <summary>
    /// TimeZone for the DateRangePicker.
    /// </summary>
    [Parameter]
    [CallOnSet(nameof(OnSetParameters))]
    public TimeZoneInfo? TimeZone { get; set; }

    /// <summary>
    /// Overrides the date considered as today by the DateRangePicker, which is <c>DateTimeOffset.Now</c> by default.
    /// </summary>
    [Parameter]
    [CallOnSet(nameof(OnSetParameters))]
    public DateTimeOffset? Today { get; set; }

    /// <summary>
    /// Whether or not the Text field of the DateRangePicker is underlined.
    /// </summary>
    [Parameter, ResetClassBuilder]
    public bool Underlined { get; set; }

    /// <summary>
    /// The string format used to show the DateRangePicker's value in its input.
    /// </summary>
    [Parameter] public string ValueFormat { get; set; } = "Start: {0} - End: {1}";

    /// <summary>
    /// The rule used to calculate the week numbers. If not set, <c>CalendarWeekRule.FirstFullWeek</c> is used.
    /// </summary>
    [Parameter] public CalendarWeekRule? WeekNumberRule { get; set; }

    /// <summary>
    /// The title of the week number (tooltip).
    /// </summary>
    [Parameter] public string WeekNumberTitle { get; set; } = "Week number {0}";

    /// <summary>
    /// Custom template to render the year cells of the DateRangePicker.
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

    /// <summary>
    /// Determines increment/decrement steps for DateRangePicker's minute.
    /// </summary>
    [Parameter] public int MinuteStep { get; set; } = 1;



    [JSInvokable("CloseCallout")]
    public async Task _CloseCalloutBeforeAnotherCalloutIsOpened()
    {
        if (Standalone) return;
        if (IsEnabled is false) return;

        if (await AssignIsOpen(false) is false) return;

        StateHasChanged();
    }

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
    /// Opens the callout of the DateRangePicker, exactly like clicking on its input.
    /// </summary>
    public Task OpenCallout()
    {
        return HandleOnClick();
    }

    /// <summary>
    /// Closes the callout of the DateRangePicker.
    /// </summary>
    public async Task CloseCallout()
    {
        if (Standalone) return;
        if (IsEnabled is false) return;

        _hoveredDate = null;

        if (await AssignIsOpen(false) is false) return;

        await ToggleCallout();

        StateHasChanged();
    }



    protected override string RootElementClass => "bit-dtrp";

    protected override void RegisterCssClasses()
    {
        ClassBuilder.Register(() => Classes?.Root);

        ClassBuilder.Register(() => GetColorClass());

        ClassBuilder.Register(() => (Dir is null && _culture.TextInfo.IsRightToLeft) ? "bit-rtl" : string.Empty);

        ClassBuilder.Register(() => IconLocation is BitIconLocation.Left ? "bit-dtrp-lic" : string.Empty);

        ClassBuilder.Register(() => Underlined ? "bit-dtrp-und" : string.Empty);

        ClassBuilder.Register(() => HasBorder is false ? "bit-dtrp-nbd" : string.Empty);

        ClassBuilder.Register(() => Standalone ? "bit-dtrp-sta" : string.Empty);

        ClassBuilder.Register(() => _hasFocus ? $"bit-dtrp-foc {Classes?.Focused}" : string.Empty);

        // The callout takes the keyboard focus with it, so the input alone cannot mark the picker as
        // active while its calendar is on screen. Standalone has no input to mark in the first place.
        ClassBuilder.Register(() => IsOpen && Standalone is false ? "bit-dtrp-opn" : string.Empty);

        ClassBuilder.Register(() => IsEnabled && Required ? "bit-dtrp-req" : string.Empty);
    }

    protected override void RegisterCssStyles()
    {
        StyleBuilder.Register(() => Styles?.Root);

        StyleBuilder.Register(() => _hasFocus ? Styles?.Focused : string.Empty);
    }

    protected override void OnInitialized()
    {
        _dateRangePickerId = $"DateRangePicker-{UniqueId}";
        _labelId = $"{_dateRangePickerId}-label";
        _calloutId = $"{_dateRangePickerId}-callout";
        _overlayId = $"{_dateRangePickerId}-overlay";
        _inputId = $"{_dateRangePickerId}-input";

        SetDefaultValue();

        OnValueChanged += HandleOnValueChanged;

        OnSetParameters();

        base.OnInitialized();
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        await base.OnAfterRenderAsync(firstRender);

        if (firstRender)
        {
            _dotnetObj = DotNetObjectReference.Create(this);

            try
            {
                // Prevents the default behavior (scrolling) of the navigation keys handled by the
                // day buttons' keydown handlers, since Blazor cannot conditionally preventDefault per key.
                await _js.BitCalendarsSetup(_calloutId);

                if (Responsive)
                {
                    await _js.BitSwipesSetup(
                        id: _calloutId,
                        trigger: 0.25m,
                        position: BitPanelPosition.Top,
                        isRtl: Dir is BitDir.Rtl,
                        orientationLock: BitSwipeOrientation.Vertical,
                        dotnetObj: _dotnetObj);
                }
            }
            catch (JSDisconnectedException) { } // we can ignore this exception here
        }

        if (_focusAfterRender)
        {
            // Consumed even without a focused date, so it cannot linger and fire on a later render.
            _focusAfterRender = false;

            if (_focusedDate.HasValue)
            {
                try
                {
                    await _js.BitCalendarsFocusDay(GetDayButtonId(_focusedDate.Value));
                }
                catch (JSDisconnectedException) { } // we can ignore this exception here
            }
        }
    }

    protected override bool TryParseValueFromString(
        string? value,
        [MaybeNullWhen(false)] out BitDateRangePickerValue? result,
        [NotNullWhen(false)] out string? validationErrorMessage)
    {
        if (value.HasNoValue())
        {
            result = null;
            validationErrorMessage = null;
            return true;
        }

        if (TryParseRange(value!, out var parsedValue) && IsRangeWithinRestrictions(parsedValue!))
        {
            result = parsedValue;
            validationErrorMessage = null;
            return true;
        }

        result = default;
        validationErrorMessage = InvalidErrorMessage.HasValue() ? InvalidErrorMessage! : $"The {DisplayName ?? FieldIdentifier.FieldName} field is not valid.";
        return false;
    }

    protected override string? FormatValueAsString(BitDateRangePickerValue? value)
    {
        if (value is null) return null;
        if (value.StartDate.HasValue is false && value.EndDate.HasValue is false) return null;

        return string.Format(_culture, ValueFormat, FormatDate(value.StartDate), FormatDate(value.EndDate));
    }

    private string FormatDate(DateTimeOffset? date)
    {
        return date.HasValue
                ? date.Value.ToString(DateFormat ?? GetDefaultDateFormat(), _culture)
                : NoDateText;
    }

    // Splits the incoming text with the literal parts of the ValueFormat and parses the two
    // remaining date tokens with the DateFormat (or the culture's default pattern).
    private bool TryParseRange(string value, out BitDateRangePickerValue? result)
    {
        result = null;

        var startIndex = ValueFormat.IndexOf("{0}", StringComparison.Ordinal);
        var endIndex = ValueFormat.IndexOf("{1}", StringComparison.Ordinal);

        if (startIndex < 0 || endIndex < 0 || endIndex < startIndex) return false;

        var prefix = ValueFormat[..startIndex];
        var separator = ValueFormat[(startIndex + 3)..endIndex];
        var suffix = ValueFormat[(endIndex + 3)..];

        if (separator.Length == 0) return false;

        var text = value.Trim();

        if (prefix.Length > 0)
        {
            if (text.StartsWith(prefix, StringComparison.OrdinalIgnoreCase) is false) return false;
            text = text[prefix.Length..];
        }

        if (suffix.Length > 0)
        {
            if (text.EndsWith(suffix, StringComparison.OrdinalIgnoreCase) is false) return false;
            text = text[..^suffix.Length];
        }

        var separatorIndex = text.IndexOf(separator, StringComparison.OrdinalIgnoreCase);
        if (separatorIndex < 0) return false;

        var startText = text[..separatorIndex];
        var endText = text[(separatorIndex + separator.Length)..];

        DateTimeOffset? startDate = null;
        if (IsEmptyDateToken(startText) is false)
        {
            if (TryParseDate(startText, out var parsedStartDate) is false) return false;

            startDate = parsedStartDate;
        }

        DateTimeOffset? endDate = null;
        if (IsEmptyDateToken(endText) is false)
        {
            if (TryParseDate(endText, out var parsedEndDate) is false) return false;

            endDate = parsedEndDate;
        }

        // A text that is nothing but the literal parts of the format holds no date at all, so it is
        // invalid input rather than an empty range.
        if (startDate.HasValue is false && endDate.HasValue is false) return false;

        if (startDate.HasValue && endDate.HasValue && endDate < startDate)
        {
            (startDate, endDate) = (endDate, startDate);
        }

        result = new BitDateRangePickerValue { StartDate = startDate, EndDate = endDate };
        return true;
    }

    private bool IsEmptyDateToken(string text)
    {
        text = text.Trim();

        return text.Length == 0 || string.Equals(text, NoDateText, StringComparison.OrdinalIgnoreCase);
    }

    private bool TryParseDate(string text, out DateTimeOffset result)
    {
        result = default;

        text = text.Trim();
        if (text.Length == 0) return false;

        var parsed = DateTime.TryParseExact(text, DateFormat ?? GetDefaultDateFormat(), _culture, DateTimeStyles.None, out DateTime parsedValue);

        // When a custom DateFormat is not set and the time picker is enabled, the default pattern
        // includes the time portion. Fall back to a date-only parse so users can still type a bare
        // date (e.g. via AllowTextInput) without being forced to include the time.
        if (parsed is false && DateFormat is null && ShowTimePicker)
        {
            parsed = DateTime.TryParseExact(text, _culture.DateTimeFormat.ShortDatePattern, _culture, DateTimeStyles.None, out parsedValue);
        }

        if (parsed is false) return false;

        result = new DateTimeOffset(parsedValue, _timeZone.GetUtcOffset(parsedValue));
        return true;
    }

    private string GetDefaultDateFormat()
    {
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

        if (await AssignIsOpen(true) is false) return;

        ResetPickersState();

        var bodyWidth = await _js.BitUtilsGetBodyWidth();

        // The extra months are the first thing to go on a narrow viewport, and only what is left
        // decides whether the month and time pickers still have to collapse into overlays.
        _fittingMonthCount = MAX_MONTH_COUNT;
        while (_fittingMonthCount > 1 && bodyWidth < GetMaxWidth(_fittingMonthCount))
        {
            _fittingMonthCount--;
        }

        var fittingMonthCount = Math.Min(Math.Clamp(MonthCount, 1, MAX_MONTH_COUNT), _fittingMonthCount);
        if (fittingMonthCount != _monthCount)
        {
            _monthCount = fittingMonthCount;
            GenerateMonthData(_currentYear, _currentMonth);
        }

        var notEnoughWidthAvailable = bodyWidth < GetMaxWidth();

        if (_showMonthPickerAsOverlayInternal is false)
        {
            _showMonthPickerAsOverlayInternal = IsMonthPickerVisible && notEnoughWidthAvailable;
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

        if (_showMonthPickerAsOverlayInternal is false &&
            _showTimePickerAsOverlayInternal is false &&
            ShowTimePicker && IsMonthPickerVisible)
        {
            _showMonthPickerAsOverlayInternal = true;
        }

        if (CurrentValue is not null)
        {
            CheckCurrentCalendarMatchesCurrentValue();
        }

        // The callout is a dialog, so the keyboard focus moves into the day grid with it. An editable
        // input keeps the focus instead, since the user may well want to go on typing the range.
        if (AllowTextInput is false)
        {
            _focusedDate = GetFocusableDay();
            _focusAfterRender = true;
        }
        else
        {
            // A click that landed on the icon rather than on the input never moved the focus into the
            // field, so the picker would open with none of the focus cues the very same click on the
            // input two pixels away produces.
            await InputElement.FocusAsync();
        }

        StateHasChanged();

        await ToggleCallout();

        await OnClick.InvokeAsync();
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
        if (AllowTextInput is false || ReadOnly) return;

        var now = Today ?? DateTimeOffset.Now;
        var oldStartValue = CurrentValue?.StartDate.GetValueOrDefault(now) ?? now;
        var oldEndValue = CurrentValue?.EndDate.GetValueOrDefault(now) ?? now;

        CurrentValueAsString = e.Value?.ToString();

        if (CurrentValue is null) return;

        var curStartValue = CurrentValue.StartDate.GetValueOrDefault(now);
        var curEndValue = CurrentValue.EndDate.GetValueOrDefault(now);

        if (IsOpen && (oldStartValue != curStartValue || oldEndValue != curEndValue))
        {
            CheckCurrentCalendarMatchesCurrentValue();

            if (curStartValue.Year != oldStartValue.Year || curEndValue.Year != oldEndValue.Year)
            {
                _currentYear = _culture.Calendar.GetYear(curStartValue.DateTime);
                ChangeYearRanges(_currentYear - 1);
            }
        }
    }

    private async Task HandleOnClearButtonClick()
    {
        if (ReadOnly) return;
        if (IsEnabled is false) return;

        // Clearing the value runs OnSetParameters, which puts the four time fields back to the ones
        // of StartingValue (or to the defaults) and re-applies the MaxRange clamp on its own.
        CurrentValue = null;

        _hoveredDate = null;
        _focusedDate = null;

        await InputElement.FocusAsync();
    }

    private void HandleOnValueChanged(object? sender, EventArgs args)
    {
        // Any change coming from anywhere but a preset button leaves no preset applied.
        // SelectPreset re-marks its own preset right after assigning the value.
        _selectedPreset = null;

        OnSetParameters();
    }

    private void OnSetParameters()
    {
        _timeZone = TimeZone ?? TimeZoneInfo.Local;
        _culture = Culture ?? CultureInfo.CurrentUICulture;
        _monthCount = Math.Min(Math.Clamp(MonthCount, 1, MAX_MONTH_COUNT), _fittingMonthCount);

        BuildDatesLookups();

        // An open-ended range holding only an end date has no start to precede, so the guard below
        // only applies once a start date exists.
        if (CurrentValue?.StartDate is not null)
        {
            var startDateTime = CurrentValue.StartDate.Value;
            if (MinDate.HasValue && MinDate > startDateTime)
            {
                startDateTime = MinDate.GetValueOrDefault(DateTimeOffset.Now);
            }

            if (MaxDate.HasValue && MaxDate < startDateTime)
            {
                startDateTime = MaxDate.GetValueOrDefault(DateTimeOffset.Now);
            }

            if (CurrentValue.EndDate.HasValue && CurrentValue.EndDate < startDateTime)
            {
                // Replaces the value instead of clearing the end date in place, so the instance
                // handed in through the Value binding is never mutated behind the consumer's back.
                CurrentValue = new BitDateRangePickerValue { StartDate = CurrentValue.StartDate };
            }
        }

        BuildExcludedBounds();

        var startDateHasValue = CurrentValue?.StartDate.HasValue ?? false;
        var endDateHasValue = CurrentValue?.EndDate.HasValue ?? false;
        var startingValueStartDateHasValue = StartingValue?.StartDate.HasValue ?? false;
        var startingValueEndDateHasValue = StartingValue?.EndDate.HasValue ?? false;

        _startTimeHour = startDateHasValue
                          ? CurrentValue!.StartDate!.Value.Hour
                          : (startingValueStartDateHasValue
                             ? StartingValue!.StartDate!.Value.Hour
                             : 0);
        _startTimeMinute = startDateHasValue
                            ? CurrentValue!.StartDate!.Value.Minute
                            : (startingValueStartDateHasValue
                                ? StartingValue!.StartDate!.Value.Minute
                                : 0);

        _endTimeHour = endDateHasValue
                        ? CurrentValue!.EndDate!.Value.Hour
                        : (startingValueEndDateHasValue
                           ? StartingValue!.EndDate!.Value.Hour
                           : 23);
        _endTimeMinute = endDateHasValue
                          ? CurrentValue!.EndDate!.Value.Minute
                          : (startingValueEndDateHasValue
                             ? StartingValue!.EndDate!.Value.Minute
                             : 59);

        if (endDateHasValue is false && MaxRange.HasValue && MaxRange.Value.TotalHours < 24)
        {
            // With no end date picked yet, the end time is pulled back just far enough for the
            // time-only span to fit a sub-day MaxRange, leaving an already fitting time untouched.
            var maxRangeTotalMinutes = (int)MaxRange.Value.TotalMinutes;
            var startTotalMinutes = (_startTimeHour * 60) + _startTimeMinute;
            var endTotalMinutes = (_endTimeHour * 60) + _endTimeMinute;

            if (Math.Abs(endTotalMinutes - startTotalMinutes) > maxRangeTotalMinutes)
            {
                endTotalMinutes = Math.Min(startTotalMinutes + maxRangeTotalMinutes, (24 * 60) - 1);
                _endTimeHour = endTotalMinutes / 60;
                _endTimeMinute = endTotalMinutes % 60;
            }
        }

        var calendarDate = startDateHasValue
                           ? CurrentValue!.StartDate!.Value.DateTime
                           : (startingValueStartDateHasValue
                             ? StartingValue!.StartDate!.Value.DateTime
                             : GetToday());

        // With several months on screen the start date is very often already one of them, and
        // rebasing the grid on its month would slide the whole calendar for no reason.
        var startDateIsAlreadyVisible = _monthCount > 1 &&
                                        _daysOfMonths.Length == _monthCount &&
                                        startDateHasValue &&
                                        IsInRenderedMonths(GetDateTime(CurrentValue!.StartDate!.Value).Date);

        if (startDateIsAlreadyVisible)
        {
            GenerateMonthData(_currentYear, _currentMonth);
        }
        else
        {
            GenerateCalendarData(calendarDate);
        }

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

            if (_showMonthPickerAsOverlayInternal is false &&
                _showTimePickerAsOverlayInternal is false &&
                ShowTimePicker && IsMonthPickerVisible)
            {
                _showMonthPickerAsOverlayInternal = true;
            }

            if (CurrentValue is not null)
            {
                CheckCurrentCalendarMatchesCurrentValue();
            }
        }
    }

    private async Task SelectDate(DateTime selectedDate)
    {
        if (ReadOnly) return;
        if (IsEnabled is false || InvalidValueBinding()) return;
        if (IsOpenHasBeenSet && IsOpenChanged.HasDelegate is false && Standalone is false) return;
        if (IsDayDisabled(selectedDate)) return;

        _hoveredDate = null;
        _focusedDate = selectedDate;

        // Works on a copy so the instance handed in through the Value binding is never mutated in place.
        var curValue = CurrentValue is null
                        ? new BitDateRangePickerValue()
                        : new BitDateRangePickerValue { StartDate = CurrentValue.StartDate, EndDate = CurrentValue.EndDate };

        // reset the current state if both start and end dates have values!
        if (curValue.StartDate.HasValue && curValue.EndDate.HasValue)
        {
            curValue.StartDate = null;
            curValue.EndDate = null;
        }

        var hour = curValue.StartDate.HasValue ? _endTimeHour : _startTimeHour;
        var minute = curValue.StartDate.HasValue ? _endTimeMinute : _startTimeMinute;

        selectedDate = selectedDate.AddHours(hour);
        selectedDate = selectedDate.AddMinutes(minute);

        var selectedDateTimeOffset = new DateTimeOffset(selectedDate, _timeZone.GetUtcOffset(selectedDate));
        if (curValue.StartDate.HasValue is false)
        {
            curValue.StartDate = selectedDateTimeOffset;
        }
        else
        {
            curValue.EndDate = selectedDateTimeOffset;

            if (AutoClose && Standalone is false)
            {
                await AssignIsOpen(false);

                await ToggleCallout();
            }
        }

        if (curValue.EndDate.HasValue && curValue.StartDate > curValue.EndDate)
        {
            if (curValue.StartDate!.Value.Date == curValue.EndDate.Value.Date)
            {
                (_endTimeHour, _startTimeHour) = (_startTimeHour, _endTimeHour);
                (_endTimeMinute, _startTimeMinute) = (_startTimeMinute, _endTimeMinute);
            }

            (curValue.EndDate, curValue.StartDate) = (curValue.StartDate, curValue.EndDate);
        }

        if (curValue.EndDate.HasValue && MaxRange.HasValue)
        {
            var maxDate = new DateTimeOffset(GetMaxEndDate(curValue.StartDate), curValue.EndDate.Value.Offset);

            if (maxDate < curValue.EndDate)
            {
                _endTimeHour = maxDate.Hour;
                _endTimeMinute = maxDate.Minute;
                curValue.EndDate = maxDate;
            }
        }

        CurrentValue = new BitDateRangePickerValue
        {
            StartDate = curValue.StartDate,
            EndDate = curValue.EndDate
        };

        var previousYear = _currentYear;
        var previousMonth = _currentMonth;

        // Keeps the clicked day visible without dragging the grid away from where it already is:
        // only a day of an adjacent month (rendered as an outside day) moves the calendar, and the
        // year has to travel with the month so a December-to-January pick does not jump a year back.
        if (IsInRenderedMonths(selectedDate) is false)
        {
            _currentYear = _culture.Calendar.GetYear(selectedDate);
            _currentMonth = _culture.Calendar.GetMonth(selectedDate);
        }

        GenerateMonthData(_currentYear, _currentMonth);

        await NotifyMonthChange(previousYear, previousMonth);
    }

    private async Task SelectPreset(BitDateRangePickerPreset preset)
    {
        if (ReadOnly) return;
        if (IsEnabled is false || preset.IsEnabled is false || InvalidValueBinding()) return;
        if (IsOpenHasBeenSet && IsOpenChanged.HasDelegate is false && Standalone is false) return;

        var presetValue = preset.ValueProvider is not null ? preset.ValueProvider() : preset.Value;
        if (presetValue is null) return;

        var startDate = presetValue.StartDate;
        var endDate = presetValue.EndDate;

        // A preset reaching outside the Min/Max bounds (or holding a blocked day as an end) is
        // rejected rather than clamped, since a shifted variant of the advertised range would not
        // be the range its button promised. IsDayExcluded compares exactly like the day grid does,
        // so a preset can never apply a day the grid itself disables.
        if ((startDate.HasValue && IsDayExcluded(GetDateTime(startDate.Value).Date)) ||
            (endDate.HasValue && IsDayExcluded(GetDateTime(endDate.Value).Date))) return;

        if (startDate.HasValue && endDate.HasValue && MaxRange.HasValue)
        {
            var maxEndDate = new DateTimeOffset(GetMaxEndDate(startDate), endDate.Value.Offset);

            if (maxEndDate < endDate)
            {
                // The four time fields are re-derived from the assigned value right after, so only
                // the date needs the clamp here.
                endDate = maxEndDate;
            }
        }

        // The blocked days reject the preset the same way the Min/Max bounds above do: a range whose
        // ends cannot be picked from the day grid must not be applied by a shortcut either. The check
        // runs after the MaxRange clamp so it judges the range that would actually be applied.
        var presetStartDate = startDate.HasValue ? GetDateTime(startDate.Value).Date : (DateTime?)null;
        var presetEndDate = endDate.HasValue ? GetDateTime(endDate.Value).Date : (DateTime?)null;

        if (presetStartDate.HasValue && IsDayBlocked(presetStartDate.Value)) return;
        if (presetEndDate.HasValue && IsDayBlocked(presetEndDate.Value)) return;

        if (presetStartDate.HasValue && presetEndDate.HasValue)
        {
            if (IsShorterThanMinRange(presetStartDate.Value, presetEndDate.Value)) return;

            if (ExcludeDisabledDates && RangeCoversBlockedDay(presetStartDate.Value, presetEndDate.Value)) return;
        }

        _hoveredDate = null;
        _focusedDate = startDate.HasValue ? GetDateTime(startDate.Value).Date : null;

        CurrentValue = new BitDateRangePickerValue
        {
            StartDate = startDate,
            EndDate = endDate
        };

        // A ValueProvider is re-evaluated on every call and a relative range therefore never compares
        // equal to the stored value down to the tick, so the applied preset is remembered instead.
        _selectedPreset = preset;

        CheckCurrentCalendarMatchesCurrentValue();

        await OnPresetSelect.InvokeAsync(preset);

        if (AutoClose && Standalone is false)
        {
            await AssignIsOpen(false);

            await ToggleCallout();
        }
    }

    private bool IsPresetSelected(BitDateRangePickerPreset preset)
    {
        if (CurrentValue is null) return false;

        if (ReferenceEquals(preset, _selectedPreset)) return true;

        // A ValueProvider is not evaluated here since it can be non-deterministic, so such a preset
        // only appears selected through _selectedPreset above.
        if (preset.Value is null) return false;

        return preset.Value.StartDate == CurrentValue.StartDate && preset.Value.EndDate == CurrentValue.EndDate;
    }

    private async Task HandleOnInputKeyDown(KeyboardEventArgs e)
    {
        if (Standalone) return;
        if (IsEnabled is false) return;

        if (e.Key == "Escape")
        {
            if (IsOpen)
            {
                await CloseCallout();
            }

            return;
        }

        // The input is a combobox, so it must be able to open its dialog from the keyboard too.
        // Enter and Space are left to the browser whenever the text input is editable.
        if (IsOpen is false && (e.Key is "ArrowDown" || ((e.Key is "Enter" or " ") && AllowTextInput is false)))
        {
            await HandleOnClick();
        }
    }

    private async Task SelectMonth(int month)
    {
        if (IsEnabled is false) return;
        if (IsMonthOutOfMinAndMaxDate(month)) return;

        var previousYear = _currentYear;
        var previousMonth = _currentMonth;

        _currentMonth = month;

        GenerateMonthData(_currentYear, _currentMonth);

        if (_showMonthPickerAsOverlayInternal || ShowTimePicker)
        {
            ToggleMonthPickerOverlay();
        }

        await NotifyMonthChange(previousYear, previousMonth);
    }

    private async Task SelectYear(int year)
    {
        if (IsEnabled is false) return;
        if (IsYearOutOfMinAndMaxDate(year)) return;

        var previousYear = _currentYear;
        var previousMonth = _currentMonth;

        _currentYear = year;

        ChangeYearRanges(_currentYear - 1);

        GenerateMonthData(_currentYear, _currentMonth);

        ToggleBetweenMonthAndYearPicker();

        await NotifyMonthChange(previousYear, previousMonth);
    }

    private void ToggleBetweenMonthAndYearPicker()
    {
        if (IsEnabled is false) return;

        // The year navigation of the month picker moves _currentYear without touching the year
        // picker's range, so the range is realigned whenever it no longer contains the current year.
        if (_showMonthPicker && (_currentYear < _yearPickerStartYear || _currentYear > _yearPickerEndYear))
        {
            ChangeYearRanges(_currentYear - 1);
        }

        _showMonthPicker = !_showMonthPicker;
    }

    private async Task HandleMonthChange(bool isNext)
    {
        if (IsEnabled is false) return;
        if (CanChangeMonth(isNext) is false) return;

        var previousYear = _currentYear;
        var previousMonth = _currentMonth;

        // With PagedNavigation the calendar moves a whole page of months at once, but never past the
        // point where the single-month navigation would have stopped.
        var steps = PagedNavigation ? _monthCount : 1;

        for (var i = 0; i < steps; i++)
        {
            if (i > 0 && CanChangeMonth(isNext) is false) break;

            (_currentYear, _currentMonth) = AddMonths(_currentYear, _currentMonth, isNext ? 1 : -1);
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

    private void GenerateCalendarData(DateTime dateTime)
    {
        _currentMonth = _culture.Calendar.GetMonth(dateTime);
        _currentYear = _culture.Calendar.GetYear(dateTime);

        _yearPickerStartYear = _currentYear - 1;
        _yearPickerEndYear = _currentYear + 10;

        GenerateMonthData(_currentYear, _currentMonth);
    }

    // Fills the grid of every rendered month, starting from the given one.
    private void GenerateMonthData(int year, int month)
    {
        if (_daysOfMonths.Length != _monthCount)
        {
            _daysOfMonths = new DateTime?[_monthCount][,];
            for (var i = 0; i < _monthCount; i++)
            {
                _daysOfMonths[i] = new DateTime?[DEFAULT_WEEK_COUNT, DEFAULT_DAY_COUNT_PER_WEEK];
            }

            _monthTitles = new string[_monthCount];
        }

        for (var i = 0; i < _monthCount; i++)
        {
            var (y, m) = AddMonths(year, month, i);

            GenerateSingleMonthData(i, y, m);
        }
    }

    // Walks the (year, month) pair the given number of months forward or backward.
    private static (int year, int month) AddMonths(int year, int month, int offset)
    {
        var total = ((year * 12) + (month - 1)) + offset;

        return (total / 12, (total % 12) + 1);
    }

    private void GenerateSingleMonthData(int monthIndex, int year, int month)
    {
        _monthTitles[monthIndex] = $"{_culture.DateTimeFormat.GetMonthName(month)} {year}";

        var days = _daysOfMonths[monthIndex];
        var calendar = _culture.Calendar;
        var firstDayOfMonth = new DateTime(year, month, 1, calendar);
        int daysInMonth = calendar.GetDaysInMonth(year, month);
        int dayOfWeek = (int)calendar.GetDayOfWeek(firstDayOfMonth);
        int firstDayOfWeek = (int)GetFirstDayOfWeek();

        // Adjust dayOfWeek to match the culture's first day of week
        dayOfWeek = (dayOfWeek - firstDayOfWeek + 7) % 7;

        DateTime previousMonth;
        if (month == 1)
        {
            previousMonth = new(year - 1, 12, 1);
        }
        else
        {
            previousMonth = new(year, month - 1, 1);
        }
        int daysInPreviousMonth = calendar.GetDaysInMonth(previousMonth.Year, previousMonth.Month);

        DateTime nextMonth;
        if (month == 12)
        {
            nextMonth = new(year + 1, 1, 1);
        }
        else
        {
            nextMonth = new(year, month + 1, 1);
        }

        int day = daysInPreviousMonth - dayOfWeek + 1;

        for (int j = 0; j < dayOfWeek; j++)
        {
            days[0, j] = new(previousMonth.Year, previousMonth.Month, day, calendar);
            day++;
        }

        day = 1;
        var ended = false;
        for (int i = 0; i < DEFAULT_WEEK_COUNT; i++)
        {
            for (int j = 0; j < DEFAULT_DAY_COUNT_PER_WEEK; j++)
            {
                if (i == 0 && j < dayOfWeek) continue;

                if (day <= daysInMonth)
                {
                    days[i, j] = new(year, month, day, calendar);
                    day++;
                }
                else
                {
                    // Months of unequal height would make a multi-month strip ragged, so the six
                    // rows are always laid out there even when FixedWeeks is off.
                    if (j == 0 && FixedWeeks is false && _monthCount == 1)
                    {
                        ended = true;
                    }
                    days[i, j] = ended
                                 ? null
                                 : new(nextMonth.Year, nextMonth.Month, day - daysInMonth, calendar);
                    day++;
                }
            }
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

    private int GetWeekNumber(int monthIndex, int weekIndex)
    {
        return _culture.Calendar.GetWeekOfYear(_daysOfMonths[monthIndex][weekIndex, 0]!.Value,
                                               WeekNumberRule ?? CalendarWeekRule.FirstFullWeek,
                                               GetFirstDayOfWeek());
    }

    private void ToggleMonthPickerOverlay()
    {
        _isMonthPickerOverlayOnTop = !_isMonthPickerOverlayOnTop;
    }

    private void ToggleTimePickerOverlay()
    {
        _isTimePickerOverlayOnTop = !_isTimePickerOverlayOnTop;
    }

    private bool CanChangeMonth(bool isNext)
    {
        if (IsEnabled is false) return false;

        if (isNext && MaxDate.HasValue)
        {
            var maxDateYear = _culture.Calendar.GetYear(GetDateTime(MaxDate.Value));
            var maxDateMonth = _culture.Calendar.GetMonth(GetDateTime(MaxDate.Value));

            if (maxDateYear == _currentYear && maxDateMonth == _currentMonth) return false;
        }


        if (isNext is false && MinDate.HasValue)
        {
            var minDateYear = _culture.Calendar.GetYear(GetDateTime(MinDate.Value));
            var minDateMonth = _culture.Calendar.GetMonth(GetDateTime(MinDate.Value));

            if (minDateYear == _currentYear && minDateMonth == _currentMonth) return false;
        }

        if (MaxRange.HasValue && MaxRange.Value.TotalDays > 0 && CurrentValue?.StartDate is not null && CurrentValue.EndDate.HasValue is false)
        {
            if (isNext)
            {
                var maxDateYear = _culture.Calendar.GetYear(GetMaxEndDate());
                var maxDateMonth = _culture.Calendar.GetMonth(GetMaxEndDate());

                if (maxDateYear == _currentYear && maxDateMonth == _currentMonth) return false;
            }
            else
            {
                var minDateYear = _culture.Calendar.GetYear(GetMinEndDate());
                var minDateMonth = _culture.Calendar.GetMonth(GetMinEndDate());

                if (minDateYear == _currentYear && minDateMonth == _currentMonth) return false;
            }
        }

        return true;
    }

    private bool CanChangeYear(bool isNext)
    {
        if (IsEnabled is false) return false;

        if (isNext)
        {
            var isInMaxDateYear = MaxDate.HasValue &&
                                  _culture.Calendar.GetYear(GetDateTime(MaxDate.Value)) == _currentYear;
            if (isInMaxDateYear) return false;

            var isInMaxDayRangeYear = MaxRange.HasValue &&
                                      MaxRange.Value.TotalDays > 0 &&
                                      CurrentValue?.StartDate is not null &&
                                      CurrentValue!.EndDate.HasValue is false &&
                                      (_culture.Calendar.GetYear(GetMaxEndDate()) == _currentYear ||
                                       _culture.Calendar.GetYear(GetMinEndDate()) == _currentYear);

            return isInMaxDayRangeYear is false;
        }
        else
        {
            var isInMinDateYear = MinDate.HasValue &&
                                  _culture.Calendar.GetYear(GetDateTime(MinDate.Value)) == _currentYear;
            if (isInMinDateYear) return false;

            var isInMaxDayRangeYear = MaxRange.HasValue &&
                                      MaxRange.Value.TotalDays > 0 &&
                                      CurrentValue?.StartDate is not null &&
                                      CurrentValue!.EndDate.HasValue is false &&
                                      (_culture.Calendar.GetYear(GetMaxEndDate()) == _currentYear ||
                                       _culture.Calendar.GetYear(GetMinEndDate()) == _currentYear);

            return isInMaxDayRangeYear is false;
        }
    }

    private bool CanChangeYearRange(bool isNext)
    {
        if (IsEnabled is false) return false;

        if (isNext)
        {
            var isInMaxDateYearRange = MaxDate.HasValue &&
                                       _culture.Calendar.GetYear(GetDateTime(MaxDate.Value)) < _yearPickerStartYear + 12;
            if (isInMaxDateYearRange) return false;

            var isInMaxDayRangeYearRange = MaxRange.HasValue &&
                                           MaxRange.Value.TotalDays > 0 &&
                                           CurrentValue?.StartDate is not null &&
                                           CurrentValue.EndDate.HasValue is false &&
                                           (_culture.Calendar.GetYear(GetMaxEndDate()) < _yearPickerStartYear + 12 ||
                                            _culture.Calendar.GetYear(GetMinEndDate()) < _yearPickerStartYear + 12);

            return isInMaxDayRangeYearRange is false;
        }
        else
        {
            var isInMinDateYearRange = MinDate.HasValue &&
                                       _culture.Calendar.GetYear(GetDateTime(MinDate.Value)) >= _yearPickerStartYear;
            if (isInMinDateYearRange) return false;

            var isInMaxDayRangeYearRange = MaxRange.HasValue &&
                                           MaxRange.Value.TotalDays > 0 &&
                                           CurrentValue?.StartDate is not null &&
                                           CurrentValue.EndDate.HasValue is false &&
                                           (_culture.Calendar.GetYear(GetMaxEndDate()) >= _yearPickerStartYear ||
                                            _culture.Calendar.GetYear(GetMinEndDate()) >= _yearPickerStartYear);

            return isInMaxDayRangeYearRange is false;
        }
    }

    private bool IsWeekDayOutOfMinAndMaxDate(DateTime date)
    {
        if (MaxDate.HasValue)
        {
            if (date > GetDateTime(MaxDate.Value)) return true;
        }

        if (MinDate.HasValue)
        {
            if (date < GetDateTime(MinDate.Value)) return true;
        }

        if (MaxRange.HasValue && MaxRange.Value.TotalDays > 0 &&
            CurrentValue?.StartDate is not null &&
            CurrentValue.EndDate.HasValue is false)
        {
            var maxEndDate = GetMaxEndDate();
            if (date > maxEndDate) return true;

            var minEndDate = GetMinEndDate();
            if (date < minEndDate) return true;
        }

        if (IsInMinRangeOfStartDate(date)) return true;

        return false;
    }

    // While only the start date is picked, every day closer to it than MinRange cannot close the range.
    private bool IsInMinRangeOfStartDate(DateTime date)
    {
        if (MinRange.HasValue is false) return false;

        var minRangeDays = (int)MinRange.Value.TotalDays;
        if (minRangeDays <= 0) return false;

        if (CurrentValue?.StartDate is null || CurrentValue.EndDate.HasValue) return false;

        var startDate = GetDateTime(CurrentValue.StartDate.Value).Date;

        return Math.Abs((date.Date - startDate).TotalDays) < minRangeDays;
    }

    private void BuildDatesLookups()
    {
        _disabledDates = DisabledDates is null ? [] : DisabledDates.Select(d => GetDateTime(d).Date).ToHashSet();
        _highlightedDates = HighlightedDates is null ? [] : HighlightedDates.Select(d => GetDateTime(d).Date).ToHashSet();
        _disabledDaysOfWeek = DisabledDaysOfWeek is null ? [] : DisabledDaysOfWeek.ToHashSet();
    }

    // The days blocked on their own, without the range-relative rules (MinRange, MaxRange and
    // ExcludeDisabledDates) that only make sense once a start date is picked.
    private bool IsDayExcluded(DateTime date)
    {
        if (MaxDate.HasValue && date > GetDateTime(MaxDate.Value)) return true;

        if (MinDate.HasValue && date < GetDateTime(MinDate.Value)) return true;

        return IsDayBlocked(date);
    }

    // The days blocked by the date-level rules alone (DisabledDaysOfWeek, DisabledDates and
    // IsDateDisabled), without the Min/Max bounds of the calendar.
    private bool IsDayBlocked(DateTime date)
    {
        if (_disabledDaysOfWeek.Contains(date.DayOfWeek)) return true;

        if (_disabledDates.Contains(date.Date)) return true;

        if (IsDateDisabled is not null && IsDateDisabled(GetDateTimeOfDayCell(date))) return true;

        return false;
    }

    // Whether any day strictly between the two ends of the range is blocked, which a range must not
    // cover when ExcludeDisabledDates is on. The walk is bounded like the excluded-bounds scan.
    private bool RangeCoversBlockedDay(DateTime startDate, DateTime endDate)
    {
        var date = startDate;

        for (var i = 0; i < MAX_EXCLUDED_SCAN_DAYS; i++)
        {
            date = date.AddDays(1);

            if (date >= endDate) return false;

            if (IsDayBlocked(date)) return true;
        }

        return false;
    }

    // Whether the two ends of the range sit closer to each other than MinRange allows.
    private bool IsShorterThanMinRange(DateTime startDate, DateTime endDate)
    {
        if (MinRange.HasValue is false) return false;

        var minRangeDays = (int)MinRange.Value.TotalDays;

        return minRangeDays > 0 && (endDate - startDate).TotalDays < minRangeDays;
    }

    // A typed range has to honor every restriction the calendar itself enforces (the Min/Max bounds,
    // the blocked days as its ends, MinRange, MaxRange and ExcludeDisabledDates), so no range that
    // could not be picked from the day grid can slip in through the text input.
    private bool IsRangeWithinRestrictions(BitDateRangePickerValue range)
    {
        DateTime? startDate = range.StartDate.HasValue ? GetDateTime(range.StartDate.Value).Date : null;
        DateTime? endDate = range.EndDate.HasValue ? GetDateTime(range.EndDate.Value).Date : null;

        if (startDate.HasValue && IsDayExcluded(startDate.Value)) return false;

        if (endDate.HasValue && IsDayExcluded(endDate.Value)) return false;

        if (startDate.HasValue is false || endDate.HasValue is false) return true;

        if (IsShorterThanMinRange(startDate.Value, endDate.Value)) return false;

        if (MaxRange.HasValue && range.EndDate!.Value - range.StartDate!.Value > MaxRange.Value) return false;

        if (ExcludeDisabledDates && RangeCoversBlockedDay(startDate.Value, endDate.Value)) return false;

        return true;
    }

    private bool IsDayDisabled(DateTime date)
    {
        if (IsWeekDayOutOfMinAndMaxDate(date)) return true;

        if (IsDayExcluded(date)) return true;

        if (RangeWouldCoverAnExcludedDay(date)) return true;

        return false;
    }

    // With ExcludeDisabledDates on, a day cannot close the range when any day between it and the
    // already picked start date is excluded, so the produced range never covers a disabled day.
    private bool RangeWouldCoverAnExcludedDay(DateTime date)
    {
        if (ExcludeDisabledDates is false) return false;

        if (CurrentValue?.StartDate is null || CurrentValue.EndDate.HasValue) return false;

        if (_excludedUpperBound.HasValue && date.Date > _excludedUpperBound.Value) return true;

        if (_excludedLowerBound.HasValue && date.Date < _excludedLowerBound.Value) return true;

        return false;
    }

    // Walks outwards from the start date once, instead of rescanning the whole span for every cell.
    private void BuildExcludedBounds()
    {
        _excludedLowerBound = null;
        _excludedUpperBound = null;

        if (ExcludeDisabledDates is false) return;

        if (CurrentValue?.StartDate is null || CurrentValue.EndDate.HasValue) return;

        // MinDate and MaxDate stop the walk instead of bounding the range, so with nothing else
        // excluding days there is nothing to look for.
        if (_disabledDates.Count == 0 && _disabledDaysOfWeek.Count == 0 && IsDateDisabled is null) return;

        var startDate = GetDateTime(CurrentValue.StartDate.Value).Date;

        _excludedUpperBound = FindNearestExcludedDay(startDate, 1);
        _excludedLowerBound = FindNearestExcludedDay(startDate, -1);
    }

    private DateTime? FindNearestExcludedDay(DateTime startDate, int step)
    {
        var date = startDate;

        for (var i = 0; i < MAX_EXCLUDED_SCAN_DAYS; i++)
        {
            date = date.AddDays(step);

            // Everything past the calendar's own bounds is unreachable anyway, so the walk can stop there.
            if (step > 0 && MaxDate.HasValue && date > GetDateTime(MaxDate.Value)) return null;
            if (step < 0 && MinDate.HasValue && date < GetDateTime(MinDate.Value)) return null;

            if (IsDayExcluded(date)) return date;
        }

        return null;
    }

    private DateTime GetToday()
    {
        return GetDateTime(Today ?? DateTimeOffset.Now);
    }

    // The first character of a shortest day name can be a surrogate pair, which indexing with [0] would split in half.
    private static string GetFirstTextElement(string value)
    {
        return value.Length == 0 ? value : StringInfo.GetNextTextElement(value, 0);
    }

    private bool IsInMonth(DateTime date, int monthIndex)
    {
        var (year, month) = AddMonths(_currentYear, _currentMonth, monthIndex);

        return _culture.Calendar.GetYear(date) == year && _culture.Calendar.GetMonth(date) == month;
    }

    // Whether the day belongs to any of the rendered months.
    private bool IsInRenderedMonths(DateTime date)
    {
        for (var i = 0; i < _monthCount; i++)
        {
            if (IsInMonth(date, i)) return true;
        }

        return false;
    }

    // The outside days of the adjacent months would appear in two grids at once when more than one
    // month is rendered, so they are dropped to keep every rendered day (and its id) unique.
    private bool ShowOutsideDaysInternal => ShowOutsideDays && _monthCount == 1;

    private string GetDayButtonId(DateTime date)
    {
        return FormattableString.Invariant($"{_dateRangePickerId}-day-{date.Year:D4}-{date.Month:D2}-{date.Day:D2}");
    }

    private bool IsDayRendered(DateTime date, int monthIndex)
    {
        return ShowOutsideDaysInternal || IsInMonth(date, monthIndex);
    }

    private DateTime GetFocusableDay()
    {
        if (_focusedDate.HasValue && IsFocusable(_focusedDate.Value)) return _focusedDate.Value;

        if (CurrentValue?.StartDate is not null)
        {
            var startDate = GetDateTime(CurrentValue.StartDate.Value).Date;
            if (IsFocusable(startDate)) return startDate;
        }

        var today = GetToday().Date;
        if (IsFocusable(today)) return today;

        for (var monthIndex = 0; monthIndex < _monthCount; monthIndex++)
        {
            for (var week = 0; week < DEFAULT_WEEK_COUNT; week++)
            {
                for (var day = 0; day < DEFAULT_DAY_COUNT_PER_WEEK; day++)
                {
                    var date = _daysOfMonths[monthIndex][week, day];
                    if (date.HasValue && IsFocusable(date.Value)) return date.Value;
                }
            }
        }

        // Only an in-month cell can carry the tabindex, so the last resort still has to be one of them.
        for (var week = 0; week < DEFAULT_WEEK_COUNT; week++)
        {
            for (var day = 0; day < DEFAULT_DAY_COUNT_PER_WEEK; day++)
            {
                var date = _daysOfMonths[0][week, day];
                if (date.HasValue && IsInMonth(date.Value, 0)) return date.Value;
            }
        }

        return today;
    }

    private bool IsFocusable(DateTime date)
    {
        return IsInRenderedMonths(date) && IsDayDisabled(date) is false;
    }

    // Escape closes the callout from anywhere inside it, as the dialog pattern requires.
    private async Task HandleOnCalloutKeyDown(KeyboardEventArgs e)
    {
        if (Standalone) return;
        if (IsEnabled is false) return;
        if (e.Key != "Escape") return;
        if (IsOpen is false) return;

        await CloseCallout();

        await InputElement.FocusAsync();
    }

    private async Task HandleDayKeyDown(KeyboardEventArgs e, DateTime date)
    {
        if (IsEnabled is false) return;

        var isRtl = Dir == BitDir.Rtl || (Dir is null && _culture.TextInfo.IsRightToLeft);

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

            // Only the calendar's own bounds end the walk. Days disabled by anything else
            // (MinRange, DisabledDates, IsDateDisabled, …) are skipped over instead.
            if (stepDays > 0 && MaxDate.HasValue && date > GetDateTime(MaxDate.Value)) return null;
            if (stepDays < 0 && MinDate.HasValue && date < GetDateTime(MinDate.Value)) return null;

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

        if (IsInRenderedMonths(target) is false)
        {
            var year = _culture.Calendar.GetYear(target);
            var month = _culture.Calendar.GetMonth(target);

            // A target past the last rendered month only has to scroll far enough to become the last
            // one, so the months already on screen keep as much of their place as they can.
            var movingForward = ((year * 12) + month) > ((_currentYear * 12) + _currentMonth);

            (_currentYear, _currentMonth) = movingForward
                                            ? AddMonths(year, month, -(_monthCount - 1))
                                            : (year, month);

            GenerateMonthData(_currentYear, _currentMonth);
        }

        _focusedDate = target;
        _focusAfterRender = true;

        await NotifyMonthChange(previousYear, previousMonth);
    }

    private async Task NotifyMonthChange(int previousYear, int previousMonth)
    {
        if (previousYear == _currentYear && previousMonth == _currentMonth) return;
        if (OnMonthChange.HasDelegate is false) return;

        var date = _culture.Calendar.ToDateTime(_currentYear, _currentMonth, 1, 0, 0, 0, 0);

        await OnMonthChange.InvokeAsync(new(date, _timeZone.GetUtcOffset(date)));
    }

    private void HandleOnDayPointerEnter(DateTime date)
    {
        if (IsEnabled is false || ReadOnly) return;
        if (CurrentValue?.StartDate is null || CurrentValue.EndDate.HasValue) return;
        if (IsDayDisabled(date)) return;

        _hoveredDate = date;
    }

    private void HandleOnDaysPointerLeave()
    {
        _hoveredDate = null;
    }

    // The prospective range shown while the pointer moves over the day cells and only the start date is picked.
    private bool IsInHoverRange(DateTime date)
    {
        if (_hoveredDate.HasValue is false) return false;
        if (CurrentValue?.StartDate is null || CurrentValue.EndDate.HasValue) return false;

        var startDate = GetDateTime(CurrentValue.StartDate.Value).Date;
        var hoveredDate = _hoveredDate.Value.Date;

        return date.Date >= (startDate < hoveredDate ? startDate : hoveredDate) &&
               date.Date <= (startDate < hoveredDate ? hoveredDate : startDate);
    }

    private string GetColorClass()
    {
        return Color switch
        {
            BitColor.Primary => "bit-dtrp-pri",
            BitColor.Secondary => "bit-dtrp-sec",
            BitColor.Tertiary => "bit-dtrp-ter",
            BitColor.Info => "bit-dtrp-inf",
            BitColor.Success => "bit-dtrp-suc",
            BitColor.Warning => "bit-dtrp-wrn",
            BitColor.SevereWarning => "bit-dtrp-swr",
            BitColor.Error => "bit-dtrp-err",
            BitColor.PrimaryBackground => "bit-dtrp-pbg",
            BitColor.SecondaryBackground => "bit-dtrp-sbg",
            BitColor.TertiaryBackground => "bit-dtrp-tbg",
            BitColor.PrimaryForeground => "bit-dtrp-pfg",
            BitColor.SecondaryForeground => "bit-dtrp-sfg",
            BitColor.TertiaryForeground => "bit-dtrp-tfg",
            BitColor.PrimaryBorder => "bit-dtrp-pbr",
            BitColor.SecondaryBorder => "bit-dtrp-sbr",
            BitColor.TertiaryBorder => "bit-dtrp-tbr",
            _ => "bit-dtrp-pri"
        };
    }

    private bool IsMonthOutOfMinAndMaxDate(int month)
    {
        if (MaxDate.HasValue)
        {
            var maxDateYear = _culture.Calendar.GetYear(GetDateTime(MaxDate.Value));
            var maxDateMonth = _culture.Calendar.GetMonth(GetDateTime(MaxDate.Value));

            if (_currentYear > maxDateYear || (_currentYear == maxDateYear && month > maxDateMonth)) return true;
        }

        if (MinDate.HasValue)
        {
            var minDateYear = _culture.Calendar.GetYear(GetDateTime(MinDate.Value));
            var minDateMonth = _culture.Calendar.GetMonth(GetDateTime(MinDate.Value));

            if (_currentYear < minDateYear || (_currentYear == minDateYear && month < minDateMonth)) return true;
        }

        if (MaxRange.HasValue && MaxRange.Value.TotalDays > 0 &&
            CurrentValue?.StartDate is not null &&
            CurrentValue.EndDate.HasValue is false)
        {
            var maxEndDate = GetMaxEndDate();
            var maxDateYear = _culture.Calendar.GetYear(maxEndDate);
            var maxDateMonth = _culture.Calendar.GetMonth(maxEndDate);

            if (_currentYear > maxDateYear || (_currentYear == maxDateYear && month > maxDateMonth)) return true;

            var minEndDate = GetMinEndDate();
            var minDateYear = _culture.Calendar.GetYear(minEndDate);
            var minDateMonth = _culture.Calendar.GetMonth(minEndDate);

            if (_currentYear < minDateYear || (_currentYear == minDateYear && month < minDateMonth)) return true;
        }

        return false;
    }

    private bool IsYearOutOfMinAndMaxDate(int year)
    {
        return (MaxDate.HasValue && year > _culture.Calendar.GetYear(GetDateTime(MaxDate.Value))) ||
               (MinDate.HasValue && year < _culture.Calendar.GetYear(GetDateTime(MinDate.Value))) ||
               (MaxRange.HasValue && MaxRange.Value.TotalDays > 0 &&
                CurrentValue?.StartDate is not null &&
                CurrentValue!.EndDate.HasValue is false &&
                (year > _culture.Calendar.GetYear(GetMaxEndDate()) || year < _culture.Calendar.GetYear(GetMinEndDate())));
    }

    private void CheckCurrentCalendarMatchesCurrentValue()
    {
        if (CurrentValue is null) return;
        if (CurrentValue.StartDate.HasValue is false) return;

        var currentValue = CurrentValue.StartDate.GetValueOrDefault(DateTimeOffset.Now);
        var currentValueYear = _culture.Calendar.GetYear(currentValue.DateTime);
        var currentValueMonth = _culture.Calendar.GetMonth(currentValue.DateTime);

        // Any of the rendered months showing the start date is enough, so the grid is only rebased
        // when the start date would otherwise be off screen.
        if (IsInRenderedMonths(currentValue.DateTime)) return;

        if (currentValueYear != _currentYear || currentValueMonth != _currentMonth)
        {
            _currentYear = currentValueYear;
            _currentMonth = currentValueMonth;
            GenerateMonthData(_currentYear, _currentMonth);
        }
    }

    private (string style, string klass) GetDayButtonCss(DateTime date, int monthIndex)
    {
        StringBuilder klass = new StringBuilder();
        StringBuilder style = new StringBuilder();

        var isStartDaySelectedDate = IsStartDaySelectedDate(date);
        var isEndDaySelectedDate = IsEndDaySelectedDate(date);

        if (isStartDaySelectedDate)
        {
            klass.Append(" bit-dtrp-dss");

            if (Classes?.StartDayButton is not null)
            {
                klass.Append(' ').Append(Classes?.StartDayButton);
            }

            AppendStyle(style, Styles?.StartDayButton);

            if (Classes?.StartAndEndSelectionDays is not null)
            {
                klass.Append(' ').Append(Classes?.StartAndEndSelectionDays);
            }

            AppendStyle(style, Styles?.StartAndEndSelectionDays);
        }

        if (isEndDaySelectedDate)
        {
            klass.Append(" bit-dtrp-dse");

            if (Classes?.EndDayButton is not null)
            {
                klass.Append(' ').Append(Classes?.EndDayButton);
            }

            AppendStyle(style, Styles?.EndDayButton);

            if (Classes?.StartAndEndSelectionDays is not null)
            {
                klass.Append(' ').Append(Classes?.StartAndEndSelectionDays);
            }

            AppendStyle(style, Styles?.StartAndEndSelectionDays);

            if (IsEqualStartAndEndDaySelectedDate(date))
            {
                klass.Append(" bit-dtrp-dsse");
            }
        }

        if (isStartDaySelectedDate is false && isEndDaySelectedDate is false && IsBetweenTwoSelectedDate(date))
        {
            klass.Append(" bit-dtrp-dsb");

            if (Classes?.SelectedDayButtons is not null)
            {
                klass.Append(' ').Append(Classes?.SelectedDayButtons);
            }

            AppendStyle(style, Styles?.SelectedDayButtons);
        }

        //Is in the prospective range being hovered
        if (isStartDaySelectedDate is false && IsInHoverRange(date))
        {
            klass.Append(" bit-dtrp-dhr");

            if (Classes?.HoveredDayButtons is not null)
            {
                klass.Append(' ').Append(Classes?.HoveredDayButtons);
            }

            AppendStyle(style, Styles?.HoveredDayButtons);
        }

        var isInMonth = IsInMonth(date, monthIndex);

        //Isn't in the month of its own grid
        if (isInMonth is false)
        {
            klass.Append(" bit-dtrp-dbo");
        }

        //Is highlighted
        if (_highlightedDates.Contains(date.Date))
        {
            klass.Append(" bit-dtrp-dhl");

            if (Classes?.HighlightedDayButton is not null)
            {
                klass.Append(' ').Append(Classes?.HighlightedDayButton);
            }

            AppendStyle(style, Styles?.HighlightedDayButton);
        }

        //Is today
        if (isInMonth && date == GetToday().Date)
        {
            klass.Append(" bit-dtrp-dtd");

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

        // The markup appends Styles?.DayButton after the produced style, so a missing trailing
        // semicolon would merge the last declaration with the first one of that slot.
        if (style.Length > 0 && style[^1] is not ';')
        {
            style.Append(';');
        }

        return (style.ToString(), klass.ToString());
    }

    // The style slots of a day button are joined with a semicolon rather than with a space, since a
    // style that omits its trailing one would otherwise swallow the declaration appended after it.
    private static void AppendStyle(StringBuilder styles, string? style)
    {
        if (style.HasNoValue()) return;

        if (styles.Length > 0 && styles[^1] is not ';')
        {
            styles.Append(';');
        }

        styles.Append(style);
    }

    private string GetMonthCellCssClass(int monthIndex, int todayYear, int todayMonth)
    {
        var className = new StringBuilder();
        if (HighlightCurrentMonth && todayMonth == monthIndex && todayYear == _currentYear)
        {
            className.Append(" bit-dtrp-pcm");
        }

        if (HighlightSelectedMonth && _currentMonth == monthIndex)
        {
            className.Append(" bit-dtrp-psm");
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

    private bool IsBetweenTwoSelectedDate(DateTime date)
    {
        if (CurrentValue is null) return false;
        if (CurrentValue.StartDate.HasValue is false ||
            CurrentValue.EndDate.HasValue is false) return false;

        return date >= GetDateTime(CurrentValue.StartDate.Value).Date &&
               date <= GetDateTime(CurrentValue.EndDate.Value).Date;
    }

    private bool IsStartDaySelectedDate(DateTime date)
    {
        if (CurrentValue is null) return false;
        if (CurrentValue.StartDate.HasValue is false) return false;

        return date == GetDateTime(CurrentValue.StartDate.Value).Date;
    }

    private bool IsEndDaySelectedDate(DateTime date)
    {
        if (CurrentValue is null) return false;
        if (CurrentValue.EndDate.HasValue is false) return false;

        return date == GetDateTime(CurrentValue.EndDate.Value).Date;
    }

    private bool IsEqualStartAndEndDaySelectedDate(DateTime date)
    {
        if (CurrentValue is null) return false;
        if (CurrentValue.StartDate.HasValue is false || CurrentValue.EndDate.HasValue is false) return false;

        var endDate = GetDateTime(CurrentValue.EndDate.Value).Date;

        return GetDateTime(CurrentValue.StartDate.Value).Date == endDate && date == endDate;
    }

    private void UpdateTime()
    {
        if (CurrentValue is null) return;
        if (CurrentValue.StartDate.HasValue is false && CurrentValue.EndDate.HasValue is false) return;

        var isEndGreaterInOneDayRange = CurrentValue.StartDate.HasValue &&
                                        CurrentValue.EndDate.HasValue &&
                                        CurrentValue.StartDate!.Value.Date == CurrentValue.EndDate!.Value.Date &&
                                        new TimeSpan(_startTimeHour, _startTimeMinute, 0) > new TimeSpan(_endTimeHour, _endTimeMinute, 0);

        if (isEndGreaterInOneDayRange)
        {
            _startTimeHour = _endTimeHour;
            _startTimeMinute = _endTimeMinute;
        }

        CurrentValue = new BitDateRangePickerValue
        {
            StartDate = GetDateTimeOffset(CurrentValue.StartDate, _startTimeHour, _startTimeMinute),
            EndDate = GetDateTimeOffset(CurrentValue.EndDate, _endTimeHour, _endTimeMinute)
        };
    }

    private DateTimeOffset? GetDateTimeOffset(DateTimeOffset? date, int hour, int minute)
    {
        if (date.HasValue is false) return null;

        var dateTime = GetDateTime(date.Value);
        var year = _culture.Calendar.GetYear(dateTime);
        var month = _culture.Calendar.GetMonth(dateTime);
        var day = _culture.Calendar.GetDayOfMonth(dateTime);

        var resultDate = _culture.Calendar.ToDateTime(year, month, day, hour, minute, 0, 0);
        return new(resultDate, _timeZone.GetUtcOffset(resultDate));
    }

    private DateTime GetDateTime(DateTimeOffset dateTimeOffset)
    {
        return TimeZoneInfo.ConvertTimeFromUtc(dateTimeOffset.UtcDateTime, _timeZone);
    }

    private async Task HandleOnHourInputFocus(bool isStartTime)
    {
        if (IsEnabled is false || ShowTimePicker is false || ReadOnly) return;

        await _js.BitUtilsSelectText(isStartTime ? _startTimeHourInputRef : _endTimeHourInputRef);
    }

    private async Task HandleOnMinuteInputFocus(bool isStartTime)
    {
        if (IsEnabled is false || ShowTimePicker is false || ReadOnly) return;

        await _js.BitUtilsSelectText(isStartTime ? _startTimeMinuteInputRef : _endTimeMinuteInputRef);
    }

    private void HandleOnAmClick(bool isStartTime)
    {
        if (ReadOnly) return;
        if (IsEnabled is false) return;

        if (isStartTime)
        {
            _startTimeHour %= 12;  // "12:-- am" is "00:--" in 24h
        }
        else
        {
            _endTimeHour %= 12;  // "12:-- am" is "00:--" in 24h
        }

        UpdateTime();
    }

    private void HandleOnPmClick(bool isStartTime)
    {
        if (ReadOnly) return;
        if (IsEnabled is false) return;

        if (isStartTime)
        {
            if (_startTimeHour <= 12) // "12:-- pm" is "12:--" in 24h
            {
                _startTimeHour += 12;
            }

            _startTimeHour %= 24;
        }
        else
        {
            if (_endTimeHour <= 12) // "12:-- pm" is "12:--" in 24h
            {
                _endTimeHour += 12;
            }

            _endTimeHour %= 24;
        }

        UpdateTime();
    }

    // Reflects the hour the time picker is currently showing, so the AM/PM pair stays in sync with
    // the inputs even before a date is picked and the value is still null.
    private bool IsAm(int hour)
    {
        return hour is >= 0 and < 12; // am is 00:00 to 11:59
    }

    private async Task HandleOnPointerDown(bool isNext, bool isHour, bool isStartTime)
    {
        if (ReadOnly) return;
        if (IsEnabled is false) return;

        await ChangeTime(isNext, isHour, isStartTime);

        if (IsDisposed) return;

        ResetCts();

        // The press-and-hold spin is deliberately not awaited: it lives as long as the button is held,
        // so awaiting it would leave the pointerdown event handler (and the render it drives) pending
        // for the whole duration of the press. Its lifetime is owned by the cancellation token source
        // instead, which HandleOnPointerUpOrOut and DisposeAsync cancel.
        _ = ContinuousChangeTimeAfterDelay(isNext, isHour, isStartTime, _cancellationTokenSource);
    }

    private async Task ContinuousChangeTimeAfterDelay(bool isNext, bool isHour, bool isStartTime, CancellationTokenSource cts)
    {
        try
        {
            await Task.Delay(Math.Max(1, ContinuousSpinDelay), cts.Token);

            await InvokeAsync(() => ContinuousChangeTime(isNext, isHour, isStartTime, cts));
        }
        catch (OperationCanceledException) { } // the button was released before the continuous spin started
        catch (ObjectDisposedException) { } // the component was disposed while the delay was pending
    }

    private async Task ContinuousChangeTime(bool isNext, bool isHour, bool isStartTime, CancellationTokenSource cts)
    {
        if (cts.IsCancellationRequested || IsDisposed) return;

        await ChangeTime(isNext, isHour, isStartTime);

        if (IsDisposed) return;

        StateHasChanged();

        await Task.Delay(75);
        await ContinuousChangeTime(isNext, isHour, isStartTime, cts);
    }

    private async Task ChangeTime(bool isNext, bool isHour, bool isStartTime)
    {
        if (isHour)
        {
            ChangeHour(isNext, isStartTime);
        }
        else
        {
            ChangeMinute(isNext, isStartTime);
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

    private void ChangeHour(bool isNext, bool isStartTime)
    {
        if (isStartTime)
        {
            _startTimeHour = ChangeHour(_startTimeHour, isNext);
        }
        else
        {
            _endTimeHour = ChangeHour(_endTimeHour, isNext);
        }

        UpdateTime();
    }

    private int ChangeHour(int hour, bool isNext)
    {
        if (isNext)
        {
            hour += HourStep;
        }
        else
        {
            hour -= HourStep;
        }

        if (hour > 23)
        {
            hour -= 24;
        }
        else if (hour < 0)
        {
            hour += 24;
        }

        return hour;
    }

    private void ChangeMinute(bool isNext, bool isStartTime)
    {
        if (isStartTime)
        {
            _startTimeMinute = ChangeMinute(_startTimeMinute, isNext);
        }
        else
        {
            _endTimeMinute = ChangeMinute(_endTimeMinute, isNext);
        }

        UpdateTime();
    }

    private int ChangeMinute(int minute, bool isNext)
    {
        if (isNext)
        {
            minute += MinuteStep;
        }
        else
        {
            minute -= MinuteStep;
        }

        if (minute > 59)
        {
            minute -= 60;
        }
        else if (minute < 0)
        {
            minute += 60;
        }

        return minute;
    }

    private bool ShowDayPicker()
    {
        if (IsMonthPickerVisible is false)
        {
            return _showTimePickerAsOverlayInternal is false || _isTimePickerOverlayOnTop is false;
        }

        if (ShowTimePicker)
        {
            if (ShowTimePickerAsOverlay)
            {
                return _showMonthPickerAsOverlayInternal is false ||
                       (_showMonthPickerAsOverlayInternal &&
                        _isMonthPickerOverlayOnTop is false &&
                        _isTimePickerOverlayOnTop is false);
            }
            else
            {
                return _showMonthPickerAsOverlayInternal &&
                       _isMonthPickerOverlayOnTop is false &&
                       (_showTimePickerAsOverlayInternal is false ||
                        _isMonthPickerOverlayOnTop is false &&
                        _isTimePickerOverlayOnTop is false);
            }
        }
        else
        {
            return _showMonthPickerAsOverlayInternal is false ||
                   (_showMonthPickerAsOverlayInternal && _isMonthPickerOverlayOnTop is false);
        }
    }

    private bool ShowMonthPicker()
    {
        if (IsMonthPickerVisible is false) return false;

        if (ShowTimePicker)
        {
            if (ShowTimePickerAsOverlay)
            {
                return (_showMonthPickerAsOverlayInternal is false ||
                        (_showMonthPickerAsOverlayInternal && _isMonthPickerOverlayOnTop)) &&
                       _isTimePickerOverlayOnTop is false;
            }
            else
            {
                return (_showMonthPickerAsOverlayInternal && _isMonthPickerOverlayOnTop) ||
                       (_showTimePickerAsOverlayInternal && _isMonthPickerOverlayOnTop && _isTimePickerOverlayOnTop is false);
            }
        }
        else
        {
            return _showMonthPickerAsOverlayInternal is false || (_showMonthPickerAsOverlayInternal && _isMonthPickerOverlayOnTop);
        }
    }

    private bool CanChangeTime(int? startTimeHour = null, int? startTimeMinute = null, int? endTimeHour = null, int? endTimeMinute = null)
    {
        if (MaxRange.HasValue is false) return true;

        var startTime = new TimeSpan(startTimeHour ?? _startTimeHour, startTimeMinute ?? _startTimeMinute, 0);
        var endTime = new TimeSpan(endTimeHour ?? _endTimeHour, endTimeMinute ?? _endTimeMinute, 0);
        var currentValueHasValue = CurrentValue?.StartDate is not null && CurrentValue.EndDate.HasValue;

        if (currentValueHasValue && CurrentValue!.StartDate!.Value.Date == CurrentValue.EndDate!.Value.Date && startTime > endTime)
        {
            return false;
        }

        if (currentValueHasValue)
        {
            var startDate = ChangeTimeInDateTimeOffset(CurrentValue!.StartDate!.Value, startTimeHour, startTimeMinute);
            var endDate = ChangeTimeInDateTimeOffset(CurrentValue!.EndDate!.Value, endTimeHour, endTimeMinute);

            // The span the proposed times would produce is what MaxRange bounds, so it is judged
            // directly instead of against bounds anchored on the current start date, which a series
            // of small changes could walk past.
            return startDate <= endDate && endDate - startDate <= MaxRange.Value;
        }

        // While the dates of the range are not picked yet, only a sub-day MaxRange can be violated
        // by the times alone: with a whole day available the times always fit inside it.
        if (MaxRange.Value.TotalHours >= 24) return true;

        var maxRangeTotalMinutes = new TimeSpan(MaxRange.Value.Hours, MaxRange.Value.Minutes, MaxRange.Value.Seconds).TotalMinutes;

        // A span of exactly MaxRange is still within it, matching the boundary the spinner buttons enforce.
        return maxRangeTotalMinutes >= Math.Abs((startTime - endTime).TotalMinutes);
    }

    private DateTimeOffset ChangeTimeInDateTimeOffset(DateTimeOffset dateTime, int? hour, int? minute)
    {
        return new DateTimeOffset(dateTime.Year,
                                  dateTime.Month,
                                  dateTime.Day,
                                  hour ?? dateTime.Hour,
                                  minute ?? dateTime.Minute,
                                  dateTime.Second,
                                  dateTime.Offset);
    }

    private bool IsIncreaseOrDecreaseButtonDisabled(bool isNext, bool isHour, bool isStartTime)
    {
        if (IsEnabled is false) return true;
        if (MaxRange.HasValue is false) return false;

        var startTimeHour = _startTimeHour;
        var endTimeHour = _endTimeHour;
        var startTimeMinute = _startTimeMinute;
        var endTimeMinute = _endTimeMinute;
        if (isHour)
        {
            if (isStartTime)
            {
                startTimeHour = ChangeHour(startTimeHour, isNext);
            }
            else
            {
                endTimeHour = ChangeHour(endTimeHour, isNext);
            }
        }
        else
        {
            if (isStartTime)
            {
                startTimeMinute = ChangeMinute(startTimeMinute, isNext);
            }
            else
            {
                endTimeMinute = ChangeMinute(endTimeMinute, isNext);
            }
        }

        return IsButtonDisabled(startTimeHour, startTimeMinute, endTimeHour, endTimeMinute);
    }

    private bool IsAmPmButtonDisabled(bool isAm, bool isStartTime)
    {
        if (MaxRange.HasValue is false) return false;

        var startTimeHour = _startTimeHour;
        var endTimeHour = _endTimeHour;

        if (isStartTime)
        {
            if (isAm)
            {
                startTimeHour %= 12;  // "12:-- am" is "00:--" in 24h
            }
            else
            {
                if (startTimeHour <= 12) // "12:-- pm" is "12:--" in 24h
                {
                    startTimeHour += 12;
                }

                startTimeHour %= 24;
            }
        }
        else
        {
            if (isAm)
            {
                endTimeHour %= 12;  // "12:-- am" is "00:--" in 24h
            }
            else
            {
                if (endTimeHour <= 12) // "12:-- pm" is "12:--" in 24h
                {
                    endTimeHour += 12;
                }

                endTimeHour %= 24;
            }
        }

        return IsButtonDisabled(startTimeHour, _startTimeMinute, endTimeHour, _endTimeMinute);
    }

    private bool IsButtonDisabled(int startTimeHour, int startTimeMinute, int endTimeHour, int endTimeMinute)
    {
        if (MaxRange.HasValue is false) return false;

        var startTime = new TimeSpan(startTimeHour, startTimeMinute, 0);
        var endTime = new TimeSpan(endTimeHour, endTimeMinute, 0);

        if (CurrentValue?.StartDate is not null && CurrentValue.EndDate.HasValue)
        {
            var startDate = ChangeTimeInDateTimeOffset(CurrentValue!.StartDate!.Value, startTimeHour, startTimeMinute);
            var endDate = ChangeTimeInDateTimeOffset(CurrentValue!.EndDate!.Value, endTimeHour, endTimeMinute);
            if (startDate > endDate)
            {
                return true;
            }

            // The span the proposed times would produce is what MaxRange bounds, so it is judged
            // directly instead of against bounds anchored on the current start date, which a series
            // of small changes could walk past.
            return endDate - startDate > MaxRange.Value;
        }

        // While the dates of the range are not picked yet, only a sub-day MaxRange can be violated
        // by the times alone: with a whole day available the times always fit inside it.
        if (MaxRange.Value.TotalHours >= 24) return false;

        var maxRangeTotalMinutes = new TimeSpan(MaxRange.Value.Hours, MaxRange.Value.Minutes, MaxRange.Value.Seconds).TotalMinutes;
        return maxRangeTotalMinutes < Math.Abs((startTime - endTime).TotalMinutes);
    }

    private DateTime GetMaxEndDate(DateTimeOffset? startDate = null)
    {
        return (startDate ?? CurrentValue!.StartDate!.Value).DateTime.AddDays(MaxRange!.Value.TotalDays);
    }

    private DateTime GetMinEndDate(DateTimeOffset? startDate = null)
    {
        return (startDate ?? CurrentValue!.StartDate!.Value).DateTime.AddDays(-1 * MaxRange!.Value.TotalDays);
    }

    private void ResetPickersState()
    {
        _hoveredDate = null;
        _showMonthPicker = true;
        _isMonthPickerOverlayOnTop = false;
        _showMonthPickerAsOverlayInternal = IsMonthPickerVisible && ShowMonthPickerAsOverlay;
        _isTimePickerOverlayOnTop = false;
        _showTimePickerAsOverlayInternal = ShowTimePickerAsOverlay;
    }

    private async Task<bool> ToggleCallout()
    {
        if (Standalone) return false;
        if (IsEnabled is false || IsDisposed) return false;
        if (_dotnetObj is null) return false;

        return await _js.BitCalloutToggleCallout(
            dotnetObj: _dotnetObj,
            componentId: _dateRangePickerId,
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
            maxWindowWidth: GetMaxWidth());
    }

    // The presets panel and every extra month widen the callout, so the threshold that decides
    // whether the pickers have to collapse into overlays has to account for them.
    private int GetMaxWidth(int? monthCount = null)
    {
        var width = MAX_WIDTH + (((monthCount ?? _monthCount) - 1) * MONTH_WIDTH);

        return Presets is not null && Presets.Any() ? width + PRESETS_WIDTH : width;
    }

    private string GetCalloutCssClasses()
    {
        List<string> classes = ["bit-dtrp-cal", GetColorClass()];

        if (IsEnabled is false)
        {
            // The callout renders outside of the root element, so it needs the disabled marker of its own.
            classes.Add("bit-dis");
        }

        if (Standalone)
        {
            classes.Add("bit-dtrp-sta");
        }

        if (Classes?.Callout is not null)
        {
            classes.Add(Classes.Callout);
        }

        if (Responsive)
        {
            classes.Add("bit-dtrp-res");
        }

        if (Dir is BitDir.Rtl || (Dir is null && _culture.TextInfo.IsRightToLeft))
        {
            classes.Add("bit-dtrp-rtl");
        }

        return string.Join(' ', classes).Trim();
    }



    protected override async ValueTask DisposeAsync(bool disposing)
    {
        if (IsDisposed || disposing is false) return;

        await base.DisposeAsync(disposing);

        _cancellationTokenSource?.Cancel();
        _cancellationTokenSource?.Dispose();
        _dotnetObj?.Dispose();
        _dotnetObj = null;
        OnValueChanged -= HandleOnValueChanged;

        try
        {
            await _js.BitCalendarsDispose(_calloutId);
            await _js.BitCalloutClearCallout(_calloutId);
            await _js.BitSwipesDispose(_calloutId);
        }
        catch (JSDisconnectedException) { } // we can ignore this exception here
    }
}
