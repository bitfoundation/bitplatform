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
    // The floor every pane of the callout is laid out on - --bit-dtp-pane-min-w of the size class, plus the
    // padding on either side of it (the panes are content-box, so the padding is added to the floor). These
    // are the values of the default rhythm: a design system that scales spacing moves the real widths, which
    // only decides how early the callout folds its extra months and its pickers away, never whether it fits.
    private const int PANE_WIDTH_SM = 208;
    private const int PANE_WIDTH_MD = 232;
    private const int PANE_WIDTH_LG = 272;
    private const int PANE_PADDING = 12;
    private const int WIDTH_SLACK = 6;
    private const int SECONDS_WIDTH = 50;
    private const int MAX_MONTH_COUNT = 3;
    private const int DEFAULT_WEEK_COUNT = 6;
    private const int DEFAULT_DAY_COUNT_PER_WEEK = 7;

    // The parts of the time of day the time picker edits. Everything that moves the time - the spin buttons,
    // the keys, what is typed - names the part it moves rather than carrying a flag per part.
    private enum TimeUnit { Hour, Minute, Second }



    private bool _hasFocus;
    private int _currentYear;
    private int _currentMonth;
    private int _yearPickerEndYear;
    private int _yearPickerStartYear;
    private bool _showMonthPicker = true;
    private bool _isTimePickerOverlayOnTop;
    private bool _isMonthPickerOverlayOnTop;
    // The months rendered side by side: what MonthCount asks for, cut back to what the viewport can
    // actually hold and to what the culture's calendar still has months for.
    private int _monthCount = 1;
    private int _fittingMonthCount = MAX_MONTH_COUNT;
    private (int Year, int Month)[] _renderedMonths = [(0, 0)];
    private string[] _monthTitles = [string.Empty];
    private bool _showTimePickerAsOverlayInternal;
    private bool _showMonthPickerAsOverlayInternal;
    private TimeZoneInfo _timeZone = TimeZoneInfo.Local;
    private CultureInfo _culture = CultureInfo.CurrentUICulture;
    private CancellationTokenSource _cancellationTokenSource = new();
    private DotNetObjectReference<BitDatePicker>? _dotnetObj;
    private DateTime?[][,] _daysOfMonths = [new DateTime?[DEFAULT_WEEK_COUNT, DEFAULT_DAY_COUNT_PER_WEEK]];

    private bool _focusDayOnOpen;
    private bool _internalIsOpenChange;
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
    private string _dialogId = string.Empty;
    private string _formatHintId = string.Empty;
    private string _overlayId = string.Empty;
    private string _headerId = string.Empty;
    private string _footerId = string.Empty;
    private string _datePickerId = string.Empty;
    private ElementReference _inputTimeHourRef = default!;
    private ElementReference _inputTimeMinuteRef = default!;
    private ElementReference _inputTimeSecondRef = default!;



    private int _hour;
    // What the field held when the current edit of it started, and how many input events that edit has
    // fired so far. An arrow key and the spin buttons of the number input fire exactly one, so a session of
    // one event moving by one is a step; a number typed digit by digit fires one per digit and is a typed
    // value, whatever it happens to land next to. Both are reset once the field is committed.
    private int _hourBeforeInput;
    private int _hourInputCount;
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
            if (IsEnabled is false || ReadOnly) return;

            if (_hourInputCount++ == 0)
            {
                _hourBeforeInput = _hour;
            }

            // The field is bound on every keystroke, so what lands here is whatever has been typed so far -
            // the first digit of a two-digit hour among it. It is only brought into the day here; the
            // HourStep grid and the bounds of the day are laid over it once the field is committed
            // (HandleOnTimeInputChange), which is what lets 12 be typed into a picker whose step is 3.
            if (TimeFormat == BitTimeFormat.TwelveHours)
            {
                // The input of a 12-hour clock carries no meridiem of its own, so the one already
                // selected is kept: typing 5 while the time reads 15:30 gives 17:30, not 05:30.
                // Both 12 and 0 mean the top of the clock, which is hour zero of the half.
                _hour = BitTimeSteps.Wrap(value, 12) + (_hour >= 12 ? 12 : 0);
            }
            else
            {
                _hour = Math.Clamp(value, 0, 23);
            }

            _ = UpdateCurrentValue();
        }
    }

    private int _minute;
    private int _minuteBeforeInput;
    private int _minuteInputCount;
    private int _minuteView
    {
        get => _minute;
        set
        {
            if (IsEnabled is false || ReadOnly) return;

            // Brought into the hour here and held to the MinuteStep grid and the bounds on commit, for the
            // same reason the hour above is.
            if (_minuteInputCount++ == 0)
            {
                _minuteBeforeInput = _minute;
            }

            _minute = Math.Clamp(value, 0, 59);

            _ = UpdateCurrentValue();
        }
    }

    private int _second;
    private int _secondBeforeInput;
    private int _secondInputCount;
    private int _secondView
    {
        get => _second;
        set
        {
            if (IsEnabled is false || ReadOnly) return;

            // Brought into the minute here and held to the SecondStep grid and the bounds on commit, for the
            // same reason the hour and the minute above are.
            if (_secondInputCount++ == 0)
            {
                _secondBeforeInput = _second;
            }

            _second = Math.Clamp(value, 0, 59);

            _ = UpdateCurrentValue();
        }
    }



    [Inject] private IJSRuntime _js { get; set; } = default!;



    /// <summary>
    /// Gets or sets the cascading parameters for the DatePicker component.
    /// </summary>
    /// <remarks>
    /// This property receives its value from an ancestor component via Blazor's cascading parameter mechanism.
    /// <br />
    /// The intended use is to allow shared configuration or settings to be applied to multiple DatePicker components through the <see cref="BitParams"/> component.
    /// </remarks>
    [CascadingParameter(Name = BitDatePickerParams.ParamName)]
    public BitDatePickerParams? CascadingParameters { get; set; }



    /// <summary>
    /// Whether selecting the already selected date deselects it, clearing the value.
    /// The callout stays open after a deselection, so another date can be picked right away.
    /// </summary>
    [Parameter] public bool AllowDeselect { get; set; }

    /// <summary>
    /// The hours the time picker can be set to, on top of what <see cref="MinTime"/>, <see cref="MaxTime"/> and
    /// the bounds of the day already allow.
    /// </summary>
    /// <remarks>
    /// The predicate receives an hour of the day (0-23), whichever <see cref="TimeFormat"/> the picker is in,
    /// and returns whether it can be picked. The spin buttons skip over the hours it rejects, a typed one snaps
    /// to the nearest it accepts, and a date entered as text whose time lands on one fails validation.
    /// </remarks>
    [Parameter] public Func<int, bool>? AllowedHours { get; set; }

    /// <summary>
    /// The minutes the time picker can be set to, on top of what <see cref="MinTime"/>, <see cref="MaxTime"/> and
    /// the bounds of the day already allow.
    /// </summary>
    /// <remarks>
    /// The predicate receives a minute of the hour (0-59) and returns whether it can be picked. The spin buttons
    /// skip over the minutes it rejects, a typed one snaps to the nearest it accepts, and a date entered as text
    /// whose time lands on one fails validation.
    /// </remarks>
    [Parameter] public Func<int, bool>? AllowedMinutes { get; set; }

    /// <summary>
    /// The seconds the time picker can be set to, on top of what <see cref="MinTime"/>, <see cref="MaxTime"/> and
    /// the bounds of the day already allow. It only has an effect while <see cref="ShowSeconds"/> is set.
    /// </summary>
    /// <remarks>
    /// The predicate receives a second of the minute (0-59) and returns whether it can be picked. The spin buttons
    /// skip over the seconds it rejects, a typed one snaps to the nearest it accepts, and a date entered as text
    /// whose time lands on one fails validation.
    /// </remarks>
    [Parameter] public Func<int, bool>? AllowedSeconds { get; set; }

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
    /// Whether the input of the picker gets the focus as soon as it renders for the first time.
    /// </summary>
    /// <remarks>
    /// A standalone picker carries its value in a hidden input nobody is meant to land on, so it has nothing
    /// to place the focus on and the parameter does nothing there.
    /// </remarks>
    [Parameter] public bool AutoFocus { get; set; }

    /// <summary>
    /// Aria label of the DatePicker's callout for screen readers.
    /// </summary>
    [Parameter] public string CalloutAriaLabel { get; set; } = "Calendar";

    /// <summary>
    /// Custom template to render at the bottom of the DatePicker's callout, below the pickers
    /// (e.g. preset buttons that set the value from the code).
    /// </summary>
    [Parameter] public RenderFragment? CalloutFooterTemplate { get; set; }

    /// <summary>
    /// Custom template to render at the top of the DatePicker's callout, above the pickers.
    /// </summary>
    [Parameter] public RenderFragment? CalloutHeaderTemplate { get; set; }

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
    /// The title of the close button (tooltip).
    /// </summary>
    [Parameter] public string CloseButtonTitle { get; set; } = "Close date picker";

    /// <summary>
    /// The general color of the DatePicker that applies to the today day button, the highlighted current month,
    /// and the selected AM/PM button.
    /// </summary>
    [Parameter, ResetClassBuilder]
    public BitColor? Color { get; set; }

    /// <summary>
    /// The delay in milliseconds before the hour/minute of the time picker starts changing continuously while an
    /// increase/decrease button is held down.
    /// </summary>
    [Parameter] public int ContinuousSpinDelay { get; set; } = 400;

    /// <summary>
    /// The interval in milliseconds between two consecutive changes while an increase/decrease
    /// button is held down.
    /// </summary>
    [Parameter] public int ContinuousSpinInterval { get; set; } = 75;

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
    /// The accessible description of the input of a picker that accepts a typed date, which the pattern the
    /// date is read with is formatted into. Setting it to an empty string leaves the input without one.
    /// </summary>
    /// <remarks>
    /// A placeholder is gone as soon as the first character is typed, so the pattern the field expects is
    /// carried by a description of the input instead, which a screen reader reads out after its name and which
    /// stays there while the date is being typed. It is only rendered where <see cref="AllowTextInput"/> makes
    /// the pattern something the user has to meet.
    /// </remarks>
    [Parameter] public string DateFormatAriaDescription { get; set; } = "Expected format: {0}";

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
    /// Disables all days after today, exactly as a <see cref="MaxDate"/> of today would.
    /// When both are set, the earlier of the two bounds wins.
    /// </summary>
    [Parameter]
    [CallOnSet(nameof(OnSetParameters))]
    public bool DisableFuture { get; set; }

    /// <summary>
    /// Disables all days before today, exactly as a <see cref="MinDate"/> of today would.
    /// When both are set, the later of the two bounds wins.
    /// </summary>
    [Parameter]
    [CallOnSet(nameof(OnSetParameters))]
    public bool DisablePast { get; set; }

    /// <summary>
    /// The custom validation error message for a date entered as text whose time of day the DatePicker does not
    /// allow to be picked, through <see cref="MinTime"/>, <see cref="MaxTime"/>, <see cref="AllowedHours"/>,
    /// <see cref="AllowedMinutes"/> or <see cref="AllowedSeconds"/>.
    /// </summary>
    [Parameter] public string? DisallowedTimeErrorMessage { get; set; }

    /// <summary>
    /// Determines the allowed drop directions of the callout.
    /// </summary>
    [Parameter] public BitDropDirection DropDirection { get; set; } = BitDropDirection.TopAndBottom;

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
    /// It is always on when <see cref="MonthCount"/> renders more than one month, since months of unequal
    /// height would leave the strip ragged.
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
    /// The accessible name of a day of <see cref="HighlightedDates"/>, which its full date is formatted into.
    /// Setting it to an empty string leaves a highlighted day named like any other.
    /// </summary>
    /// <remarks>
    /// A highlighted day is marked by a background alone, which a screen reader has nothing to read out and a
    /// person who cannot tell the two backgrounds apart has nothing to see - so the mark is said in the name of
    /// the day as well.
    /// </remarks>
    [Parameter] public string HighlightedDateAriaLabel { get; set; } = "{0}, highlighted";

    /// <summary>
    /// Whether the month picker should highlight the selected month.
    /// </summary>
    [Parameter] public bool HighlightSelectedMonth { get; set; }

    /// <summary>
    /// Whether the day picker should highlight today's day. It only affects the visual style of the
    /// day cell; the accessibility attributes still report the day as the current date.
    /// </summary>
    [Parameter] public bool HighlightToday { get; set; } = true;

    /// <summary>
    /// The step, in hours, the spin buttons of the time picker move the hour by.
    /// </summary>
    /// <remarks>
    /// A step greater than 1 lays a grid over the day that every hour the picker produces sits on, so a picker
    /// that only accepts times on a three-hour grid can say so. The grid starts at the hour of
    /// <see cref="MinTime"/>, and at midnight where there is none. The buttons, the keys and what is typed into
    /// the hour are all held to it. Values below 1 are treated as 1.
    /// </remarks>
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
    [Parameter, ResetClassBuilder, TwoWayBound]
    [CallOnSet(nameof(OnSetIsOpen))]
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
    /// <remarks>
    /// The days after it are ruled out as a whole, and the day it itself falls on stays selectable. Where a
    /// time picker is on screen, the time it carries bounds the hours of that day too, so the DatePicker
    /// cannot produce an instant past the bound.
    /// </remarks>
    [Parameter]
    [CallOnSet(nameof(OnSetParameters))]
    public DateTimeOffset? MaxDate { get; set; }

    /// <summary>
    /// The latest time of day the time picker can be set to, on every day the DatePicker offers.
    /// </summary>
    /// <remarks>
    /// It is the time of day <see cref="MaxDate"/> is not: a bound on the hours of every day rather than on the
    /// days themselves, which is what a picker of business hours needs. Where both have something to say about
    /// the day being set - the day a MaxDate falls on - the earlier of the two wins. A value outside of a day
    /// (negative or over 23:59:59) is clamped into one before it is applied, and it only has an effect while
    /// <see cref="ShowTimePicker"/> is set.
    /// </remarks>
    [Parameter] public TimeSpan? MaxTime { get; set; }

    /// <summary>
    /// The minimum date allowed for the DatePicker.
    /// </summary>
    /// <inheritdoc cref="MaxDate" path="/remarks"/>
    [Parameter]
    [CallOnSet(nameof(OnSetParameters))]
    public DateTimeOffset? MinDate { get; set; }

    /// <summary>
    /// The earliest time of day the time picker can be set to, on every day the DatePicker offers.
    /// </summary>
    /// <inheritdoc cref="MaxTime" path="/remarks"/>
    [Parameter] public TimeSpan? MinTime { get; set; }

    /// <summary>
    /// The number of consecutive months rendered side by side in the day picker (1 to 3), which opens the
    /// picker on a whole season at once - the arrow keys and the selection carry on across the months of it.
    /// </summary>
    /// <remarks>
    /// A strip of months always draws six week rows and never draws the days of the adjacent months (each
    /// of them is a day of the pane beside it). A picker that opens a callout falls back to fewer months on
    /// a viewport too narrow to hold them - the same width that decides whether the month and time pickers
    /// collapse into overlays - while a <see cref="Standalone"/> one renders what it is asked for, since
    /// the room it has is the page's business rather than the viewport's.
    /// </remarks>
    [Parameter]
    [CallOnSet(nameof(OnSetParameters))]
    public int MonthCount { get; set; } = 1;

    /// <summary>
    /// The step, in minutes, the spin buttons of the time picker move the minute by.
    /// </summary>
    /// <remarks>
    /// A step greater than 1 lays a grid over the hour that every minute the picker produces sits on, which is
    /// what turns it into a five-minute or quarter-hour picker. The grid starts at the minute of
    /// <see cref="MinTime"/>, and at the top of the hour where there is none. The buttons, the keys and what is
    /// typed into the minute are all held to it. Values below 1 are treated as 1.
    /// </remarks>
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
    /// The icon to display inside the now button.
    /// Takes precedence over <see cref="NowButtonIconName"/> when both are set.
    /// </summary>
    [Parameter] public BitIconInfo? NowButtonIcon { get; set; }

    /// <summary>
    /// The name of the now button's icon from the built-in Fluent UI icon set.
    /// </summary>
    [Parameter] public string? NowButtonIconName { get; set; }

    /// <summary>
    /// The title of the now button (tooltip).
    /// </summary>
    /// <remarks>
    /// The button sets the time of the selected day to the current time - and, on a picker with no value yet,
    /// picks today along with it, since "now" is a whole instant rather than a time of day.
    /// </remarks>
    [Parameter] public string NowButtonTitle { get; set; } = "Go to now";

    /// <summary>
    /// The callback that is called when the value gets cleared by the clear button.
    /// </summary>
    [Parameter] public EventCallback OnClear { get; set; }

    /// <summary>
    /// The callback for clicking on the DatePicker's input.
    /// </summary>
    [Parameter] public EventCallback OnClick { get; set; }

    /// <summary>
    /// The callback for when the callout of the DatePicker is closed.
    /// </summary>
    [Parameter] public EventCallback OnClose { get; set; }

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
    /// The argument is the first day of the newly displayed month - of the first of them where
    /// <see cref="MonthCount"/> renders a strip.
    /// </summary>
    [Parameter] public EventCallback<DateTimeOffset> OnMonthChange { get; set; }

    /// <summary>
    /// The callback for when the callout of the DatePicker is opened.
    /// </summary>
    [Parameter] public EventCallback OnOpen { get; set; }

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
    /// Whether the previous and next navigation buttons move the day picker by all of its rendered months
    /// instead of one. It has no effect when <see cref="MonthCount"/> renders a single month.
    /// </summary>
    [Parameter] public bool PagedNavigation { get; set; }

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
    /// The step, in seconds, the spin buttons of the time picker move the second by.
    /// </summary>
    /// <remarks>
    /// A step greater than 1 lays a grid over the minute that every second the picker produces sits on. The grid
    /// starts at the second of <see cref="MinTime"/>, and at the top of the minute where there is none. The
    /// buttons, the keys and what is typed into the second are all held to it. Values below 1 are treated as 1.
    /// </remarks>
    [Parameter] public int SecondStep { get; set; } = 1;

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
    /// Whether the now button should be shown or not.
    /// </summary>
    [Parameter] public bool ShowNowButton { get; set; } = true;

    /// <summary>
    /// Whether the days of the previous and next months should be shown in the day picker.
    /// It has no effect when <see cref="MonthCount"/> renders more than one month, since those days would
    /// then appear in two panes at once.
    /// </summary>
    [Parameter] public bool ShowOutsideDays { get; set; } = true;

    /// <summary>
    /// Whether the time picker shows a seconds field beside the hour and the minute, which adds the second to
    /// the value and to the default date format. It has no effect without <see cref="ShowTimePicker"/>.
    /// </summary>
    [Parameter]
    [CallOnSet(nameof(OnSetParameters))]
    public bool ShowSeconds { get; set; }

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
    /// The title (tooltip) and the accessible name of the time-picker's decrease-hour button.
    /// </summary>
    [Parameter] public string TimePickerDecreaseHourTitle { get; set; } = "Decrease hour";

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
    /// The title (tooltip) and the accessible name of the time-picker's decrease-minute button.
    /// </summary>
    [Parameter] public string TimePickerDecreaseMinuteTitle { get; set; } = "Decrease minute";

    /// <summary>
    /// The icon to display inside the time-picker's decrease-second button.
    /// Takes precedence over <see cref="TimePickerDecreaseSecondIconName"/> when both are set.
    /// </summary>
    [Parameter] public BitIconInfo? TimePickerDecreaseSecondIcon { get; set; }

    /// <summary>
    /// The name of the time-picker's decrease-second button icon from the built-in Fluent UI icon set.
    /// </summary>
    [Parameter] public string? TimePickerDecreaseSecondIconName { get; set; }

    /// <summary>
    /// The title (tooltip) and the accessible name of the time-picker's decrease-second button.
    /// </summary>
    [Parameter] public string TimePickerDecreaseSecondTitle { get; set; } = "Decrease second";

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
    /// The title (tooltip) and the accessible name of the time-picker's increase-hour button.
    /// </summary>
    [Parameter] public string TimePickerIncreaseHourTitle { get; set; } = "Increase hour";

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
    /// The title (tooltip) and the accessible name of the time-picker's increase-minute button.
    /// </summary>
    [Parameter] public string TimePickerIncreaseMinuteTitle { get; set; } = "Increase minute";

    /// <summary>
    /// The icon to display inside the time-picker's increase-second button.
    /// Takes precedence over <see cref="TimePickerIncreaseSecondIconName"/> when both are set.
    /// </summary>
    [Parameter] public BitIconInfo? TimePickerIncreaseSecondIcon { get; set; }

    /// <summary>
    /// The name of the time-picker's increase-second button icon from the built-in Fluent UI icon set.
    /// </summary>
    [Parameter] public string? TimePickerIncreaseSecondIconName { get; set; }

    /// <summary>
    /// The title (tooltip) and the accessible name of the time-picker's increase-second button.
    /// </summary>
    [Parameter] public string TimePickerIncreaseSecondTitle { get; set; } = "Increase second";

    /// <summary>
    /// The title (tooltip) and the accessible name of the time-picker's minute input.
    /// </summary>
    [Parameter] public string TimePickerMinuteTitle { get; set; } = "Minute";

    /// <summary>
    /// The title (tooltip) and the accessible name of the time-picker's second input.
    /// </summary>
    [Parameter] public string TimePickerSecondTitle { get; set; } = "Second";

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
    /// The accessible name of the empty column header above the week numbers.
    /// </summary>
    [Parameter] public string WeekNumbersHeaderTitle { get; set; } = "Week";

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

        if (await AssignIsOpenInternal(false) is false) return;

        // The focus is on its way to whatever callout is being opened in this one's place, so this is the
        // one close that must not pull it back onto the field.
        await OnClose.InvokeAsync();

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

        ClassBuilder.Register(() => BitCssClasses.CultureRtl(Dir, _culture));

        ClassBuilder.Register(GetColorClass);

        ClassBuilder.Register(GetSizeClass);

        ClassBuilder.Register(() => IconLocation is BitIconLocation.Left ? "bit-dtp-lic" : string.Empty);

        ClassBuilder.Register(() => Underlined ? "bit-dtp-und" : string.Empty);

        ClassBuilder.Register(() => HasBorder is false ? "bit-dtp-nbd" : string.Empty);

        ClassBuilder.Register(() => Standalone ? "bit-dtp-sta" : string.Empty);

        // The callout takes the focus with it when it opens, leaving the input with no focus ring to
        // show which control the callout belongs to - so the input carries the open state itself.
        ClassBuilder.Register(() => (Standalone is false && IsOpen) ? "bit-dtp-opn" : string.Empty);

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
        _dialogId = $"{_datePickerId}-dialog";
        _formatHintId = $"{_datePickerId}-format";
        _overlayId = $"{_datePickerId}-overlay";
        _headerId = $"{_datePickerId}-header";
        _footerId = $"{_datePickerId}-footer";
        _inputId = $"{_datePickerId}-input";

        SetDefaultValue();

        OnValueChanged += HandleOnValueChanged;

        OnSetParameters();

        base.OnInitialized();
    }

    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(BitDatePickerParams))]
    protected override void OnParametersSet()
    {
        CascadingParameters?.UpdateParameters(this);

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
                // Prevents the default behavior (scrolling the page) of the keys handled by the keydown
                // handlers of the grid cells and of the field, since Blazor cannot conditionally
                // preventDefault per key, and keeps the focus inside the callout while it is the modal
                // dialog it reports itself to be. A standalone picker has no field to pass along: what it
                // carries instead is a hidden input nobody can land on.
                await _js.BitCalendarsSetup(_calloutId, Standalone is false, Standalone ? null : _datePickerId);

                // The swipe dismisses the callout, and standalone there is no callout to dismiss.
                if (Responsive && Standalone is false)
                {
                    await _js.BitSwipesSetup(_calloutId, 0.25m, BitPanelPosition.Top, IsRtl(), BitSwipeOrientation.Vertical, _dotnetObj);
                }
                // An initial IsOpen fired the OnSetIsOpen hook before the first render, when there was no
                // callout element to toggle yet, so the open state is applied here instead - with the focus
                // handling and the event of the hook, so a picker that starts open and one opened from the
                // outside a moment later end up in the same state.
                if (IsOpen && Standalone is false)
                {
                    await ToggleCallout();

                    if (AllowTextInput is false || ReadOnly)
                    {
                        FocusCalloutOnOpen();
                        StateHasChanged();
                    }

                    await OnOpen.InvokeAsync();
                }

                // The autofocus attribute is only honored by the browser for an element that is part of the
                // initial document, which the input of an interactively rendered picker is not, so the focus
                // is placed from here instead. A standalone picker carries the value in a hidden input nobody
                // is meant to land on, so it has nothing to focus.
                if (AutoFocus && IsEnabled && Standalone is false)
                {
                    await InputElement.FocusAsync();
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

        var parsed = DateTime.TryParseExact(value, GetParseFormats(), _culture, DateTimeStyles.None, out DateTime parsedValue);

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

            // And the same for the time of day, which the time picker holds to the bounds and to the allowed
            // values the application declared: typing one it refuses would be the way around them.
            if (IsTimeOfDayAllowed(parsedValue) is false)
            {
                result = default;
                validationErrorMessage = DisallowedTimeErrorMessage.HasValue()
                    ? DisallowedTimeErrorMessage!
                    : $"The {DisplayName ?? FieldIdentifier.FieldName} field is not an allowed time.";
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

    // The description the input carries while it accepts a typed date: the pattern it is read with, which the
    // placeholder cannot keep saying once the typing has started.
    private string? GetDateFormatDescription()
    {
        if (AllowTextInput is false || Standalone) return null;

        if (DateFormatAriaDescription.HasNoValue()) return null;

        // A description the application put on the input itself is the one that stands.
        if (InputHtmlAttributes?.ContainsKey("aria-describedby") is true) return null;

        return string.Format(_culture, DateFormatAriaDescription, DateFormat ?? GetDefaultDateFormat());
    }

    // The pattern of the time of day, in the clock format of the component, built out of the patterns
    // of the culture (see BitTimePatterns) so the DatePicker and the TimePicker write times the same way.
    private string GetTimePattern() => BitTimePatterns.GetTimePattern(_culture, TimeFormat, withSeconds: ShowSeconds);

    private BitTimeFormat OtherTimeFormat => TimeFormat == BitTimeFormat.TwelveHours
        ? BitTimeFormat.TwentyFourHours
        : BitTimeFormat.TwelveHours;

    // What a typed date is read with. A DateFormat the application set is taken literally - it asked for that
    // one - but the default is only how the picker writes a date, not the only way a person may write it: the
    // same day with the time left off, with the seconds left off or spelled out, and in either clock format,
    // are the same instant spelled differently, so they are accepted as well and rewritten into the canonical
    // format afterwards.
    private string[] GetParseFormats()
    {
        if (DateFormat.HasValue()) return [DateFormat!];

        if (Mode == BitDatePickerMode.MonthPicker) return [_culture.DateTimeFormat.YearMonthPattern];

        var date = _culture.DateTimeFormat.ShortDatePattern;

        if (ShowTimePicker is false) return [date];

        List<string> formats = [];

        void Add(string format)
        {
            var pattern = $"{date} {format}";

            if (formats.Contains(pattern)) return;

            formats.Add(pattern);
        }

        Add(BitTimePatterns.GetTimePattern(_culture, TimeFormat, ShowSeconds));
        Add(BitTimePatterns.GetTimePattern(_culture, TimeFormat, ShowSeconds is false));
        Add(BitTimePatterns.GetTimePattern(_culture, OtherTimeFormat, ShowSeconds));
        Add(BitTimePatterns.GetTimePattern(_culture, OtherTimeFormat, ShowSeconds is false));

        // The day on its own, so a date can still be typed into a picker that also offers a time.
        formats.Add(date);

        return [.. formats];
    }



    private async Task HandleOnClick()
    {
        if (Standalone) return;
        if (IsEnabled is false) return;

        var wasOpen = IsOpen;

        if (await AssignIsOpenInternal(true) is false)
        {
            _focusDayOnOpen = false;
            return;
        }

        await PrepareCalloutForOpen();

        // A click on the field of an already open picker is not a second opening: the callout is shown and
        // positioned, and reporting it open again would have the application counting one opening per click.
        if (wasOpen is false)
        {
            await ToggleCallout();

            await OnOpen.InvokeAsync();
        }

        // The callout is a modal dialog, so it takes the focus with it and the user browses and picks
        // the date from inside it. The exception is a pointer press on a picker whose input accepts
        // text: there the user is about to type the date, and the focus has to stay where they typed.
        if (wasOpen is false && (_focusDayOnOpen || AllowTextInput is false || ReadOnly))
        {
            FocusCalloutOnOpen();
        }

        _focusDayOnOpen = false;

        await OnClick.InvokeAsync();
    }

    // Everything the callout has to be brought to before it is shown: the pickers back at their starting
    // view, the overlay decisions remade against the width available right now, and the calendar moved onto
    // the current value. Every path that opens the callout runs it, so a click, a call to OpenCallout and an
    // IsOpen pushed in from the outside all open onto the same state.
    private async Task PrepareCalloutForOpen()
    {
        ResetPickersState();

        var bodyWidth = await _js.BitUtilsGetBodyWidth();

        // The extra months are the first thing to go on a narrow viewport, and only what is left decides
        // whether the month and time pickers still have to collapse into overlays.
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
        _second = 0;
        _focusedDate = null;

        try
        {
            await InputElement.FocusAsync();
        }
        catch (JSDisconnectedException) { } // we can ignore this exception here

        await OnClear.InvokeAsync();
    }

    private void HandleOnValueChanged(object? sender, EventArgs args)
    {
        OnSetParameters();
    }

    internal void OnSetParameters()
    {
        _timeZone = TimeZone ?? TimeZoneInfo.Local;
        _culture = Culture ?? CultureInfo.CurrentUICulture;
        _monthCount = Math.Min(Math.Clamp(MonthCount, 1, MAX_MONTH_COUNT), _fittingMonthCount);

        var value = CurrentValue.GetValueOrDefault(StartingValue.GetValueOrDefault(GetNow()));

        var minDate = GetMinDate();
        if (minDate.HasValue && minDate > value)
        {
            value = minDate.Value;
        }

        var maxDate = GetMaxDate();
        if (maxDate.HasValue && maxDate < value)
        {
            value = maxDate.Value;
        }

        // Everything the calendar shows - the month it opens on, the time in the time picker - belongs
        // to the TimeZone of the component, not to the offset the value happens to carry.
        var dateTime = GetDateTime(value);

        var hasTime = CurrentValue.HasValue || StartingValue.HasValue;
        _hour = hasTime ? dateTime.Hour : 0;
        _minute = hasTime ? dateTime.Minute : 0;
        _second = hasTime && ShowSeconds ? dateTime.Second : 0;

        // A picker with no value yet is showing a time of its own making, so it shows one it actually offers -
        // the top of the day is not a time a nine-to-five picker has. A value the application gave it is left
        // alone instead: the fields are what the value reads, and correcting one without the other would have
        // them disagree.
        if (CurrentValue.HasValue is false)
        {
            ClampTimeToBounds();
        }

        GenerateCalendarData(dateTime, keepViewIfVisible: true);

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

        // Selecting the selected day again deselects it (AllowDeselect). The callout stays open - the
        // user just emptied the value, so the calendar is exactly what they need to pick another one.
        if (AllowDeselect && IsSelectedDate(selectedDate))
        {
            // A day a strip of months already shows leaves the strip where it is - moving its start onto that
            // day's month would shove the other panes along under a click that changed nothing but the value.
            var isRendered = IsInRenderedMonths(selectedDate.Date);
            var year = isRendered ? _currentYear : _culture.Calendar.GetYear(selectedDate);
            var month = isRendered ? _currentMonth : _culture.Calendar.GetMonth(selectedDate);

            _focusedDate = selectedDate;

            CurrentValue = null;

            // Clearing the value resets the calendar onto today (OnSetParameters), but the user is
            // still looking at the month of the day they just deselected, so it is put back on screen.
            _currentYear = year;
            _currentMonth = month;
            GenerateMonthData(_currentYear, _currentMonth);

            await OnSelectDate.InvokeAsync(null);

            return;
        }

        var previousYear = _currentYear;
        var previousMonth = _currentMonth;

        _focusedDate = selectedDate;

        // The time the picker is holding travels onto the day being selected, where a bound carrying a time
        // of day can allow less of it than the day it came from did - the morning of a picker bounded at
        // "now", for one. So it arrives inside what this day allows rather than pushing the value past it.
        ClampTimeToBounds(selectedDate);

        selectedDate = selectedDate.AddHours(_hour);
        selectedDate = selectedDate.AddMinutes(_minute);
        selectedDate = selectedDate.AddSeconds(_second);

        // With the time picker on screen, picking a day is only half of the value: closing right away
        // would send the user back to reopen the callout to set the time they were about to set.
        // A one-way bound IsOpen cannot be closed by the selection either, so the callout stays open on
        // the date that was just picked instead - the selection itself still goes through.
        if (AutoClose && Standalone is false && ShowTimePicker is false &&
            (IsOpenHasBeenSet is false || IsOpenChanged.HasDelegate))
        {
            await AssignIsOpenInternal(false);

            await ToggleCallout();

            await OnClose.InvokeAsync();

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

        // A day of a month the strip already shows leaves the view where it is - which is what keeps a
        // multi-month picker still while its second and third months are picked from.
        if (IsInRenderedMonths(selectedDate.Date) is false)
        {
            _currentYear = _culture.Calendar.GetYear(selectedDate);
            _currentMonth = _culture.Calendar.GetMonth(selectedDate);
        }

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
            var selectedDate = GetFirstDayOfMonthOrClamp(_currentYear, _currentMonth);

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
        var min = GetMinDate();
        if (min.HasValue)
        {
            var minDate = GetDateTime(min.Value).Date;
            if (date < minDate && _culture.Calendar.GetMonth(minDate) == month) return minDate;
        }

        var max = GetMaxDate();
        if (max.HasValue)
        {
            var maxDate = GetDateTime(max.Value).Date;
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

        // With PagedNavigation the picker moves a whole strip of months at once, but never past the
        // point where the single-month navigation would have stopped.
        var steps = PagedNavigation ? _renderedMonths.Length : 1;

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

        var date = GetFirstDayOfMonthOrClamp(_currentYear, _currentMonth);

        await OnMonthChange.InvokeAsync(new(date, _timeZone.GetUtcOffset(date)));
    }

    // Moves the view onto the given date and rebuilds it. A date a strip of months already shows can
    // leave the view where it is (keepViewIfVisible), which is what keeps a multi-month picker still
    // while its second and third months are picked from; the navigation that is meant to move - the
    // GoToToday button - asks for the move instead.
    private void GenerateCalendarData(DateTime dateTime, bool keepViewIfVisible = false)
    {
        // Only once the picker is on screen: while the parameters are still being applied the view has
        // to settle on the value it was given, whatever the months a half-built strip happens to hold.
        if (keepViewIfVisible && IsRendered && _renderedMonths.Length > 1 && IsInRenderedMonths(dateTime.Date))
        {
            GenerateMonthData(_currentYear, _currentMonth);
            return;
        }

        _currentMonth = _culture.Calendar.GetMonth(dateTime);
        _currentYear = _culture.Calendar.GetYear(dateTime);

        _yearPickerStartYear = _currentYear - 1;
        _yearPickerEndYear = _currentYear + 10;

        GenerateMonthData(_currentYear, _currentMonth);
    }

    // The strip of months the day picker renders, starting at the given one. A month past the last one
    // the culture's calendar supports has nothing to draw and no id of its own to draw it under, so the
    // strip simply ends there rather than repeating the month before it.
    private void GenerateMonthData(int year, int month)
    {
        var months = new List<(int Year, int Month)> { (year, month) };

        for (var i = 1; i < _monthCount; i++)
        {
            var next = AddMonths(months[^1].Year, months[^1].Month, 1);

            if (next == months[^1]) break;

            months.Add(next);
        }

        if (_daysOfMonths.Length != months.Count)
        {
            _daysOfMonths = new DateTime?[months.Count][,];
            for (var i = 0; i < months.Count; i++)
            {
                _daysOfMonths[i] = new DateTime?[DEFAULT_WEEK_COUNT, DEFAULT_DAY_COUNT_PER_WEEK];
            }

            _monthTitles = new string[months.Count];
        }

        _renderedMonths = [.. months];

        for (var i = 0; i < months.Count; i++)
        {
            GenerateSingleMonthData(i, months[i].Year, months[i].Month);
        }
    }

    // Walks the (year, month) pair the given number of months forward or backward, in the months of the
    // culture's own calendar - not every year of every calendar has twelve of them - and stops at the
    // first and the last month that calendar supports.
    private (int Year, int Month) AddMonths(int year, int month, int offset)
    {
        var calendar = _culture.Calendar;
        var (minYear, minMonth) = GetMinCalendarYearMonth();
        var (maxYear, maxMonth) = GetMaxCalendarYearMonth();

        while (offset > 0)
        {
            if (year > maxYear || (year == maxYear && month >= maxMonth)) break;

            if (month < calendar.GetMonthsInYear(year))
            {
                month++;
            }
            else
            {
                year++;
                month = 1;
            }

            offset--;
        }

        while (offset < 0)
        {
            if (year < minYear || (year == minYear && month <= minMonth)) break;

            if (month > 1)
            {
                month--;
            }
            else
            {
                year--;
                month = calendar.GetMonthsInYear(year);
            }

            offset++;
        }

        return (year, month);
    }

    private void GenerateSingleMonthData(int monthIndex, int year, int month)
    {
        _monthTitles[monthIndex] = $"{_culture.DateTimeFormat.GetMonthName(month)} {year}";

        var days = _daysOfMonths[monthIndex];
        var calendar = _culture.Calendar;
        int daysInMonth = calendar.GetDaysInMonth(year, month);
        int firstDayOfWeek = (int)GetFirstDayOfWeek();
        int dayOfWeek;

        var firstDayOfMonth = TryCreateDate(year, month, 1);
        if (firstDayOfMonth.HasValue)
        {
            dayOfWeek = (int)calendar.GetDayOfWeek(firstDayOfMonth.Value);
        }
        else
        {
            // The first supported month of a calendar does not have to start at its first day (the
            // minimum of the Hebrew calendar falls in the middle of a month), so the weekday of the
            // unrepresentable first day is walked back from the first day the calendar does support.
            var minDate = calendar.MinSupportedDateTime;
            dayOfWeek = ((int)calendar.GetDayOfWeek(minDate) - (calendar.GetDayOfMonth(minDate) - 1)) % 7;
            if (dayOfWeek < 0)
            {
                dayOfWeek += 7;
            }
        }

        // Adjust dayOfWeek to match the culture's first day of week
        dayOfWeek = (dayOfWeek - firstDayOfWeek + 7) % 7;

        // The adjacent months are kept as plain year/month numbers of the culture's own calendar: a
        // DateTime built out of them would be a Gregorian date of a year that calendar never had, and a
        // thirteenth month - which a leap year of the Hebrew calendar does have - has no Gregorian
        // counterpart to build at all.
        int monthsInYear = calendar.GetMonthsInYear(year);

        int previousYear = month == 1 ? year - 1 : year;
        int previousMonth;
        int daysInPreviousMonth;
        if (previousYear < GetMinCalendarYearMonth().Year)
        {
            // The year before the calendar's first year cannot be asked anything - every one of its
            // days comes out as an empty cell anyway, so any day numbers at all do for the counting.
            previousMonth = 1;
            daysInPreviousMonth = dayOfWeek;
        }
        else
        {
            previousMonth = month == 1 ? calendar.GetMonthsInYear(previousYear) : month - 1;
            daysInPreviousMonth = calendar.GetDaysInMonth(previousYear, previousMonth);
        }

        int nextYear = month == monthsInYear ? year + 1 : year;
        int nextMonth = month == monthsInYear ? 1 : month + 1;

        int day = daysInPreviousMonth - dayOfWeek + 1;

        for (int i = 0; i < 1; i++)
        {
            for (int j = 0; j < dayOfWeek; j++)
            {
                days[i, j] = TryCreateDate(previousYear, previousMonth, day);
                day++;
            }
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
                    days[i, j] = TryCreateDate(year, month, day);
                    day++;
                }
                else
                {
                    // Months of unequal height would make a multi-month strip ragged, so the six rows
                    // are always laid out there even when FixedWeeks is off.
                    if (j == 0 && FixedWeeks is false && _daysOfMonths.Length == 1)
                    {
                        ended = true;
                    }
                    days[i, j] = ended ? null : TryCreateDate(nextYear, nextMonth, day - daysInMonth);
                    day++;
                }
            }
        }
    }

    // A day at the very edge of the calendar - the days around its first and last supported months -
    // cannot be represented as a DateTime at all, so it becomes an empty cell instead of an exception.
    private DateTime? TryCreateDate(int year, int month, int day)
    {
        try
        {
            return new DateTime(year, month, day, _culture.Calendar);
        }
        catch (ArgumentException)
        {
            return null;
        }
    }

    // The first day of a month at the very edge of the calendar's supported range does not have to be
    // representable (the range of the Hebrew calendar starts in the middle of a month), so the nearest
    // day the calendar does support stands in for it.
    private DateTime GetFirstDayOfMonthOrClamp(int year, int month)
    {
        var date = TryCreateDate(year, month, 1);
        if (date.HasValue) return date.Value;

        var calendar = _culture.Calendar;

        return year == GetMinCalendarYearMonth().Year && month <= GetMinCalendarYearMonth().Month
            ? calendar.MinSupportedDateTime.Date
            : calendar.MaxSupportedDateTime.Date;
    }

    // DateTime is bounded to the years 1 through 9999 of the Gregorian calendar, and some calendars
    // support even less, so everything the navigation can reach is bounded by the calendar's own range
    // the same way MinDate and MaxDate bound it.
    private (int Year, int Month) GetMinCalendarYearMonth()
    {
        var calendar = _culture.Calendar;
        var minDate = calendar.MinSupportedDateTime;

        return (calendar.GetYear(minDate), calendar.GetMonth(minDate));
    }

    private (int Year, int Month) GetMaxCalendarYearMonth()
    {
        var calendar = _culture.Calendar;
        var maxDate = calendar.MaxSupportedDateTime;

        return (calendar.GetYear(maxDate), calendar.GetMonth(maxDate));
    }

    private bool IsWeekRowEmpty(int monthIndex, int weekIndex)
    {
        for (var day = 0; day < DEFAULT_DAY_COUNT_PER_WEEK; day++)
        {
            if (_daysOfMonths[monthIndex][weekIndex, day].HasValue) return false;
        }

        return true;
    }

    // Moving to another year of a calendar whose years do not all have the same number of months (the
    // Hebrew one) can leave the displayed month past the end of the year that is now displayed - and
    // moving to the first or the last supported year can leave it past the supported part of that year.
    private void ClampCurrentMonthToYear()
    {
        var monthsInYear = GetMonthsInCurrentYear();

        if (_currentMonth > monthsInYear)
        {
            _currentMonth = monthsInYear;
        }

        var (minCalendarYear, minCalendarMonth) = GetMinCalendarYearMonth();
        if (_currentYear == minCalendarYear && _currentMonth < minCalendarMonth)
        {
            _currentMonth = minCalendarMonth;
        }

        var (maxCalendarYear, maxCalendarMonth) = GetMaxCalendarYearMonth();
        if (_currentYear == maxCalendarYear && _currentMonth > maxCalendarMonth)
        {
            _currentMonth = maxCalendarMonth;
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
        // The first cells of the week can be empty at the very edge of the calendar's supported range,
        // so the number of the week is read off the first day of it that actually exists.
        var date = _daysOfMonths[monthIndex][weekIndex, 0];
        for (var day = 1; date.HasValue is false && day < DEFAULT_DAY_COUNT_PER_WEEK; day++)
        {
            date = _daysOfMonths[monthIndex][weekIndex, day];
        }

        return _culture.Calendar.GetWeekOfYear(date!.Value, WeekNumberRule ?? CalendarWeekRule.FirstFullWeek, GetFirstDayOfWeek());
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

        if (isNext)
        {
            // The strip moves as a whole, so what it is about to run past is judged on its last pane -
            // derived from where the strip starts rather than read off _renderedMonths, which a paged move
            // steps past one month at a time before the strip is regenerated.
            var (lastYear, lastMonth) = AddMonths(_currentYear, _currentMonth, _renderedMonths.Length - 1);

            var (maxCalendarYear, maxCalendarMonth) = GetMaxCalendarYearMonth();
            if ((maxCalendarYear, maxCalendarMonth).CompareTo((lastYear, lastMonth)) <= 0) return false;

            var max = GetMaxDate();
            if (max.HasValue)
            {
                var maxDate = GetDateTime(max.Value);
                var maxDateYear = _culture.Calendar.GetYear(maxDate);
                var maxDateMonth = _culture.Calendar.GetMonth(maxDate);

                // A month the strip already shows is as far as it goes: a strip of several months would
                // otherwise step right over the month the bound falls in and leave it behind.
                if ((maxDateYear, maxDateMonth).CompareTo((lastYear, lastMonth)) <= 0) return false;
            }
        }
        else
        {
            // Backwards the strip is judged on its first pane, which is where the view starts.
            var (minCalendarYear, minCalendarMonth) = GetMinCalendarYearMonth();
            if ((minCalendarYear, minCalendarMonth).CompareTo((_currentYear, _currentMonth)) >= 0) return false;

            var min = GetMinDate();
            if (min.HasValue)
            {
                var minDate = GetDateTime(min.Value);
                var minDateYear = _culture.Calendar.GetYear(minDate);
                var minDateMonth = _culture.Calendar.GetMonth(minDate);

                if ((minDateYear, minDateMonth).CompareTo((_currentYear, _currentMonth)) >= 0) return false;
            }
        }

        return true;
    }

    private bool CanChangeYear(bool isNext)
    {
        if (IsEnabled is false) return false;

        if (isNext && _currentYear >= GetMaxCalendarYearMonth().Year) return false;
        if (isNext is false && _currentYear <= GetMinCalendarYearMonth().Year) return false;

        var maxDate = GetMaxDate();
        var minDate = GetMinDate();

        return (
                (isNext && maxDate.HasValue && _culture.Calendar.GetYear(GetDateTime(maxDate.Value)) == _currentYear) ||
                (isNext is false && minDate.HasValue && _culture.Calendar.GetYear(GetDateTime(minDate.Value)) == _currentYear)
               ) is false;
    }

    private bool CanChangeYearRange(bool isNext)
    {
        if (IsEnabled is false) return false;

        if (isNext && GetMaxCalendarYearMonth().Year < _yearPickerStartYear + 12) return false;
        if (isNext is false && GetMinCalendarYearMonth().Year >= _yearPickerStartYear) return false;

        var maxDate = GetMaxDate();
        var minDate = GetMinDate();

        return (
                (isNext && maxDate.HasValue && _culture.Calendar.GetYear(GetDateTime(maxDate.Value)) < _yearPickerStartYear + 12) ||
                (isNext is false && minDate.HasValue && _culture.Calendar.GetYear(GetDateTime(minDate.Value)) >= _yearPickerStartYear)
               ) is false;
    }

    // DisablePast and DisableFuture bound the selectable days by today exactly the way MinDate and
    // MaxDate do, so every consumer of the allowed range reads the bounds through these two accessors.
    private DateTimeOffset? GetMinDate()
    {
        if (DisablePast is false) return MinDate;

        var now = GetNow();

        return MinDate.HasValue && MinDate.Value > now ? MinDate : now;
    }

    private DateTimeOffset? GetMaxDate()
    {
        if (DisableFuture is false) return MaxDate;

        var now = GetNow();

        return MaxDate.HasValue && MaxDate.Value < now ? MaxDate : now;
    }

    // Every caller weighs a day of the calendar, so the comparison is day against day: a MinDate that
    // carries a time of day rules out the days before it, not the day it itself falls on.
    private bool IsWeekDayOutOfMinAndMaxDate(DateTime date)
    {
        var maxDate = GetMaxDate();
        if (maxDate.HasValue)
        {
            if (date.Date > GetDateTime(maxDate.Value).Date) return true;
        }

        var minDate = GetMinDate();
        if (minDate.HasValue)
        {
            if (date.Date < GetDateTime(minDate.Value).Date) return true;
        }

        return false;
    }

    private bool IsMonthOutOfMinAndMaxDate(int month)
    {
        // The supported range of the calendar itself bounds the selection the same way MinDate and
        // MaxDate do: a month past its edge has no representable days at all.
        var (minCalendarYear, minCalendarMonth) = GetMinCalendarYearMonth();
        if (_currentYear < minCalendarYear || (_currentYear == minCalendarYear && month < minCalendarMonth)) return true;

        var (maxCalendarYear, maxCalendarMonth) = GetMaxCalendarYearMonth();
        if (_currentYear > maxCalendarYear || (_currentYear == maxCalendarYear && month > maxCalendarMonth)) return true;

        var max = GetMaxDate();
        if (max.HasValue)
        {
            var maxDate = GetDateTime(max.Value);
            var maxDateYear = _culture.Calendar.GetYear(maxDate);
            var maxDateMonth = _culture.Calendar.GetMonth(maxDate);

            if (_currentYear > maxDateYear || (_currentYear == maxDateYear && month > maxDateMonth)) return true;
        }

        var min = GetMinDate();
        if (min.HasValue)
        {
            var minDate = GetDateTime(min.Value);
            var minDateYear = _culture.Calendar.GetYear(minDate);
            var minDateMonth = _culture.Calendar.GetMonth(minDate);

            if (_currentYear < minDateYear || (_currentYear == minDateYear && month < minDateMonth)) return true;
        }

        return false;
    }

    private bool IsYearOutOfMinAndMaxDate(int year)
    {
        var maxDate = GetMaxDate();
        var minDate = GetMinDate();

        // The years outside of the calendar's own supported range are as unselectable as the ones
        // outside of MinDate and MaxDate - the year picker can show them at the edges of its ranges.
        return year < GetMinCalendarYearMonth().Year
            || year > GetMaxCalendarYearMonth().Year
            || (maxDate.HasValue && year > _culture.Calendar.GetYear(GetDateTime(maxDate.Value)))
            || (minDate.HasValue && year < _culture.Calendar.GetYear(GetDateTime(minDate.Value)));
    }

    private void CheckCurrentCalendarMatchesCurrentValue()
    {
        // The day cells are rendered in the TimeZone of the component, so the month the calendar opens
        // on has to be read in it as well - otherwise a value near midnight opens the month next to the
        // one holding the day that is actually marked as selected.
        var currentValue = GetDateTime(CurrentValue.GetValueOrDefault(GetNow()));

        if (IsInRenderedMonths(currentValue.Date)) return;

        _currentYear = _culture.Calendar.GetYear(currentValue);
        _currentMonth = _culture.Calendar.GetMonth(currentValue);
        GenerateMonthData(_currentYear, _currentMonth);
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

        //Isn't in one of the months on screen
        if (IsInRenderedMonths(date) is false)
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
        if (HighlightToday && IsInRenderedMonths(date) && date == GetToday().Date)
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
        var date = GetFirstDayOfMonthOrClamp(_currentYear, monthIndex);
        return new(date, _timeZone.GetUtcOffset(date));
    }

    // The name a day carries: its full date, and the mark of a highlighted one, which is otherwise conveyed
    // by a background alone.
    private string GetDayAriaLabel(DateTime date)
    {
        var label = date.ToString(_culture.DateTimeFormat.LongDatePattern, _culture);

        if (HighlightedDateAriaLabel.HasNoValue()) return label;

        return _highlightedDates.Contains(date.Date) ? string.Format(_culture, HighlightedDateAriaLabel, label) : label;
    }

    private bool IsSelectedDate(DateTime date)
    {
        if (CurrentValue is null) return false;

        return date == GetDateTime(CurrentValue.Value).Date;
    }

    private void BuildDatesLookups()
    {
        // Only the date part of each value counts, as supplied by the caller - converting the value
        // into the picker's time zone first could shift it to the adjacent day and disable (or
        // highlight) a different date than the one that was configured.
        _disabledDates = DisabledDates is null ? [] : DisabledDates.Select(d => d.Date).ToHashSet();
        _highlightedDates = HighlightedDates is null ? [] : HighlightedDates.Select(d => d.Date).ToHashSet();
        _disabledDaysOfWeek = DisabledDaysOfWeek is null ? [] : DisabledDaysOfWeek.ToHashSet();
    }

    private bool IsDayDisabled(DateTime date)
    {
        // A day the calendar itself cannot represent is not selectable (nor focusable) at all - the
        // supported range of a calendar does not have to cover the whole range of DateTime.
        if (date < _culture.Calendar.MinSupportedDateTime.Date || date > _culture.Calendar.MaxSupportedDateTime.Date) return true;

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

    // Whether the day belongs to the month of the given pane of the strip.
    private bool IsInMonth(DateTime date, int monthIndex)
    {
        var (year, month) = _renderedMonths[monthIndex];

        return _culture.Calendar.GetYear(date) == year && _culture.Calendar.GetMonth(date) == month;
    }

    // Whether the day belongs to any of the months on screen, which is what tells a day the picker is
    // already showing from one it would have to scroll to.
    private bool IsInRenderedMonths(DateTime date)
    {
        for (var i = 0; i < _renderedMonths.Length; i++)
        {
            if (IsInMonth(date, i)) return true;
        }

        return false;
    }

    // The outside days of the adjacent months would appear in two panes at once while a strip of months
    // is rendered, so they are dropped there to keep every rendered day - and its id - unique.
    private bool ShowOutsideDaysInternal => ShowOutsideDays && _renderedMonths.Length == 1;

    private bool IsDayRendered(DateTime date, int monthIndex)
    {
        return ShowOutsideDaysInternal || IsInMonth(date, monthIndex);
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
        return BitCssClasses.IsRtl(Dir, _culture);
    }

    // The single day of the grid that is in the tab sequence (the roving tabindex of the APG grid
    // pattern): the one the keyboard last landed on, otherwise the selection, otherwise today, and as a
    // last resort the first day that can actually be selected - the grid must never be unreachable.
    private DateTime GetFocusableDay()
    {
        if (_focusedDate.HasValue && IsInRenderedMonths(_focusedDate.Value) && IsDayDisabled(_focusedDate.Value) is false) return _focusedDate.Value;

        if (CurrentValue.HasValue)
        {
            var selectedDate = GetDateTime(CurrentValue.Value).Date;
            if (IsInRenderedMonths(selectedDate) && IsDayDisabled(selectedDate) is false) return selectedDate;
        }

        var today = GetToday().Date;
        if (IsInRenderedMonths(today) && IsDayDisabled(today) is false) return today;

        for (var monthIndex = 0; monthIndex < _renderedMonths.Length; monthIndex++)
        {
            for (var week = 0; week < DEFAULT_WEEK_COUNT; week++)
            {
                for (var day = 0; day < DEFAULT_DAY_COUNT_PER_WEEK; day++)
                {
                    var date = _daysOfMonths[monthIndex][week, day];
                    if (date.HasValue && IsInMonth(date.Value, monthIndex) && IsDayDisabled(date.Value) is false) return date.Value;
                }
            }
        }

        // A month can be disabled from end to end, and a disabled day is not focusable anyway - but the
        // tabindex still has to land on a day of a month of the strip itself: with the outside days not
        // rendered as buttons at all, pointing at one loses the grid entirely.
        for (var monthIndex = 0; monthIndex < _renderedMonths.Length; monthIndex++)
        {
            for (var week = 0; week < DEFAULT_WEEK_COUNT; week++)
            {
                for (var day = 0; day < DEFAULT_DAY_COUNT_PER_WEEK; day++)
                {
                    var date = _daysOfMonths[monthIndex][week, day];
                    if (date.HasValue && IsInMonth(date.Value, monthIndex)) return date.Value;
                }
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

        DateTime? target;
        try
        {
            target = e.Key switch
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
        }
        catch (ArgumentException)
        {
            // Stepping over the edge of the calendar's supported range (the days around its first and
            // last representable dates) throws instead of wrapping, and the focus simply stays put.
            return;
        }

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

        if (IsInRenderedMonths(target) is false)
        {
            var year = _culture.Calendar.GetYear(target);
            var month = _culture.Calendar.GetMonth(target);

            // A target past the last rendered month only has to scroll far enough to become the last one,
            // so the months already on screen keep as much of their place as they can.
            var movingForward = (year, month).CompareTo((_renderedMonths[^1].Year, _renderedMonths[^1].Month)) > 0;

            (_currentYear, _currentMonth) = movingForward
                                            ? AddMonths(year, month, -(_renderedMonths.Length - 1))
                                            : (year, month);

            GenerateMonthData(_currentYear, _currentMonth);
        }

        _focusedDate = target;
        _focusElementIdAfterRender = GetDayButtonId(target);

        await NotifyMonthChange(previousYear, previousMonth);
    }

    private string GetColorClass()
    {
        return BitCssClasses.Color(Color, "bit-dtp");
    }

    private string GetSizeClass()
    {
        return BitCssClasses.Size(Size, "bit-dtp");
    }

    private Task UpdateCurrentValue()
    {
        if (CurrentValue.HasValue is false) return Task.CompletedTask;

        var currentValue = GetDateTime(CurrentValue.Value);
        var currentValueYear = _culture.Calendar.GetYear(currentValue);
        var currentValueMonth = _culture.Calendar.GetMonth(currentValue);
        var currentValueDay = _culture.Calendar.GetDayOfMonth(currentValue);
        var dateTime = _culture.Calendar.ToDateTime(currentValueYear, currentValueMonth, currentValueDay, _hour, _minute, _second, 0);

        CurrentValue = new(dateTime, _timeZone.GetUtcOffset(dateTime));

        return Task.CompletedTask;
    }

    private DateTime GetDateTime(DateTimeOffset dateTimeOffset)
    {
        return TimeZoneInfo.ConvertTimeFromUtc(dateTimeOffset.UtcDateTime, _timeZone);
    }

    private async Task HandleOnTimeHourFocus()
    {
        // A new edit of the field starts here, so an edit left uncommitted - typed and then typed back -
        // does not count towards it.
        _hourInputCount = 0;

        if (IsEnabled is false || ShowTimePicker is false || ReadOnly) return;

        try
        {
            await _js.BitUtilsSelectText(_inputTimeHourRef);
        }
        catch (JSDisconnectedException) { } // we can ignore this exception here
    }

    private async Task HandleOnTimeMinuteFocus()
    {
        // A new edit of the field starts here, so an edit left uncommitted - typed and then typed back -
        // does not count towards it.
        _minuteInputCount = 0;

        if (IsEnabled is false || ShowTimePicker is false || ReadOnly) return;

        try
        {
            await _js.BitUtilsSelectText(_inputTimeMinuteRef);
        }
        catch (JSDisconnectedException) { } // we can ignore this exception here
    }

    private async Task HandleOnTimeSecondFocus()
    {
        // A new edit of the field starts here, so an edit left uncommitted - typed and then typed back -
        // does not count towards it.
        _secondInputCount = 0;

        if (IsEnabled is false || ShowTimePicker is false || ReadOnly) return;

        try
        {
            await _js.BitUtilsSelectText(_inputTimeSecondRef);
        }
        catch (JSDisconnectedException) { } // we can ignore this exception here
    }

    private async Task HandleOnAmClick()
    {
        if (ReadOnly) return;
        if (IsEnabled is false) return;

        _hour %= 12;  // "12:-- am" is "00:--" in 24h

        SnapTimeIntoHalfOfDay(true);

        await UpdateCurrentValue();
    }

    private async Task HandleOnPmClick()
    {
        if (ReadOnly) return;
        if (IsEnabled is false) return;

        // "12:-- pm" is already "12:--" in 24h, so only the hours before noon move forward -
        // otherwise clicking pm at 12:-- pm would wrap the hour around to 12:-- am.
        if (_hour < 12)
        {
            _hour += 12;
        }

        SnapTimeIntoHalfOfDay(false);

        await UpdateCurrentValue();
    }

    // Half a day is a wide move, so it can land on an hour the picker does not offer - one a bound or an
    // AllowedHours rules out. The nearest hour of the half the user asked for is where it settles instead of
    // being pulled back into the other half, and the minute and the second follow the hour they now sit in.
    private void SnapTimeIntoHalfOfDay(bool am)
    {
        var start = am ? 0 : 12;

        if (IsHourAllowed(_hour) is false)
        {
            var inHalf = BitTimeSteps.FindNearestAllowed(_hour - start, 12, h => IsHourAllowed(start + h));

            if (inHalf.HasValue)
            {
                _hour = start + inHalf.Value;
            }
        }

        ClampTimeToBounds();

        _minute = BitTimeSteps.FindNearestAllowed(_minute, 60, IsMinuteAllowed) ?? _minute;
        _second = BitTimeSteps.FindNearestAllowed(_second, 60, IsSecondAllowed) ?? _second;
    }

    private bool? IsAm()
    {
        if (CurrentValue.HasValue is false) return null;

        return _hour >= 0 && _hour < 12; // am is 00:00 to 11:59
    }

    private async Task HandleOnPointerDown(bool isNext, TimeUnit unit)
    {
        if (ReadOnly) return;
        if (IsEnabled is false) return;

        await ChangeTime(isNext, unit);

        if (IsDisposed) return;

        ResetCts();

        // The press-and-hold spin is deliberately not awaited: it lives as long as the button is held, so
        // awaiting it would leave the pointerdown event handler (and the render it drives) pending for the
        // whole duration of the press. Its lifetime is owned by the cancellation token source instead, which
        // HandleOnPointerUpOrOut and DisposeAsync cancel.
        _ = ContinuousChangeTimeAfterDelay(isNext, unit, _cancellationTokenSource);
    }

    // A press of a spin button that came from the keyboard rather than from a pointer: Enter and the space bar
    // fire a click and no pointer event at all, so without this the buttons would be reachable by keyboard and
    // do nothing. A click that follows a real press carries the number of that press in its detail, which is
    // what tells the two apart - and keeps a pointer press from stepping the time twice.
    private async Task HandleOnSpinClick(MouseEventArgs e, bool isNext, TimeUnit unit)
    {
        if (e.Detail != 0) return;
        if (ReadOnly) return;
        if (IsEnabled is false) return;

        await ChangeTime(isNext, unit);
    }

    /// <summary>
    /// Waits out the <see cref="ContinuousSpinDelay"/> and then starts the continuous spin, unless the
    /// button was released (or the component went away) in the meantime.
    /// </summary>
    private async Task ContinuousChangeTimeAfterDelay(bool isNext, TimeUnit unit, CancellationTokenSource cts)
    {
        try
        {
            await Task.Delay(Math.Max(1, ContinuousSpinDelay), cts.Token);

            await InvokeAsync(() => ContinuousChangeTime(isNext, unit, cts));
        }
        catch (OperationCanceledException) { } // the button was released before the continuous spin started
        catch (ObjectDisposedException) { } // the component was disposed while the delay was pending
    }

    // A loop rather than a call that ends in another one of itself: a button held for a few seconds is
    // hundreds of ticks, and every one of them would otherwise leave a frame of its own alive until the whole
    // chain unwinds at the end of the press.
    private async Task ContinuousChangeTime(bool isNext, TimeUnit unit, CancellationTokenSource cts)
    {
        while (cts.IsCancellationRequested is false && IsDisposed is false)
        {
            var partBeforeStep = GetTimePart(unit);

            await ChangeTime(isNext, unit);

            if (cts.IsCancellationRequested || IsDisposed) return;

            // A tick that moved nothing will not move anything on the next one either - a step of a whole
            // range leaves a single value on the grid - so the held button has run out of room. Without this
            // it would spend the rest of the press re-rendering a value that never changes again.
            if (GetTimePart(unit) == partBeforeStep) return;

            StateHasChanged();

            try
            {
                await Task.Delay(Math.Max(1, ContinuousSpinInterval), cts.Token);
            }
            catch (OperationCanceledException)
            {
                // The button was released while the next tick was pending; ending the loop here stops the
                // spin right away instead of waiting the interval out first.
                return;
            }
        }
    }

    private int GetTimePart(TimeUnit unit) => unit switch
    {
        TimeUnit.Hour => _hour,
        TimeUnit.Minute => _minute,
        _ => _second
    };

    private async Task ChangeTime(bool isNext, TimeUnit unit)
    {
        switch (unit)
        {
            case TimeUnit.Hour:
                await ChangeHour(isNext);
                break;
            case TimeUnit.Minute:
                await ChangeMinute(isNext);
                break;
            default:
                await ChangeSecond(isNext);
                break;
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

    // The step lays a grid over the day rather than adding itself to whatever the hour happens to be, so every
    // hour the buttons produce is a multiple of it - a bound value between two grid points moves onto the next
    // one, and a step that does not divide the day wraps to the top of the grid instead of drifting off it.
    private async Task ChangeHour(bool isNext)
    {
        _hour = BitTimeSteps.StepToAllowed(_hour, isNext, 24, IsHourAllowed) ?? _hour;

        // Stepping onto the hour a bound falls in can leave the minute past the bound, so it comes along -
        // and the second with it.
        _minute = BitTimeSteps.FindNearestAllowed(_minute, 60, IsMinuteAllowed) ?? _minute;
        _second = BitTimeSteps.FindNearestAllowed(_second, 60, IsSecondAllowed) ?? _second;

        await UpdateCurrentValue();
    }

    /// <inheritdoc cref="ChangeHour"/>
    private async Task ChangeMinute(bool isNext)
    {
        _minute = BitTimeSteps.StepToAllowed(_minute, isNext, 60, IsMinuteAllowed) ?? _minute;

        _second = BitTimeSteps.FindNearestAllowed(_second, 60, IsSecondAllowed) ?? _second;

        await UpdateCurrentValue();
    }

    /// <inheritdoc cref="ChangeHour"/>
    private async Task ChangeSecond(bool isNext)
    {
        _second = BitTimeSteps.StepToAllowed(_second, isNext, 60, IsSecondAllowed) ?? _second;

        await UpdateCurrentValue();
    }

    // Where the step grids start: the same part of MinTime, so a picker whose day begins at 09:07 can still be
    // set to 09:07, and the top of the day without one.
    private TimeSpan? GridAnchor => BitTimeSteps.ClampToDay(MinTime);

    // The grid HourStep, MinuteStep and SecondStep lay over the day, the hour and the minute, which everything
    // that moves the time - the spin buttons, the keys, what is typed - is held to, so no two of them can
    // disagree about which times the picker offers.
    private bool IsHourOnGrid(int hour) => BitTimeSteps.IsOnGrid(hour, HourStep, GridAnchor?.Hours ?? 0, 24);

    /// <inheritdoc cref="IsHourOnGrid"/>
    private bool IsMinuteOnGrid(int minute) => BitTimeSteps.IsOnGrid(minute, MinuteStep, GridAnchor?.Minutes ?? 0, 60);

    /// <inheritdoc cref="IsHourOnGrid"/>
    private bool IsSecondOnGrid(int second) => BitTimeSteps.IsOnGrid(second, SecondStep, GridAnchor?.Seconds ?? 0, 60);

    // MinDate and MaxDate rule out whole days everywhere else in the picker and the very hours of one day
    // here: the day a bound falls on stays selectable, but only from the time of day the bound carries. So
    // the time picker of a DatePicker bounded at "now" cannot be wound back into this morning, the way its
    // day grid cannot be wound back to yesterday. Everything that moves the time is held to it alongside
    // the step grid, since both are simply which times this picker offers.
    private bool IsHourAllowed(int hour)
    {
        if (IsHourOnGrid(hour) is false) return false;

        if (AllowedHours is not null && AllowedHours(hour) is false) return false;

        var (min, max) = GetTimeBounds();

        if (min.HasValue && hour < min.Value.Hours) return false;
        if (max.HasValue && hour > max.Value.Hours) return false;

        return true;
    }

    /// <inheritdoc cref="IsHourAllowed"/>
    private bool IsMinuteAllowed(int minute)
    {
        if (IsMinuteOnGrid(minute) is false) return false;

        if (AllowedMinutes is not null && AllowedMinutes(minute) is false) return false;

        var (min, max) = GetTimeBounds();

        // Only the hour a bound falls in is bounded by its minutes: every later hour of the day the minimum
        // falls on, and every earlier one of the day the maximum does, is in range from end to end.
        if (min.HasValue && _hour == min.Value.Hours && minute < min.Value.Minutes) return false;
        if (max.HasValue && _hour == max.Value.Hours && minute > max.Value.Minutes) return false;

        return true;
    }

    /// <inheritdoc cref="IsHourAllowed"/>
    private bool IsSecondAllowed(int second)
    {
        if (IsSecondOnGrid(second) is false) return false;

        if (AllowedSeconds is not null && AllowedSeconds(second) is false) return false;

        var (min, max) = GetTimeBounds();

        // And only the minute a bound falls in is bounded by its seconds, for the same reason.
        if (min.HasValue && _hour == min.Value.Hours && _minute == min.Value.Minutes && second < min.Value.Seconds) return false;
        if (max.HasValue && _hour == max.Value.Hours && _minute == max.Value.Minutes && second > max.Value.Seconds) return false;

        return true;
    }

    // Whether the time of day of a value the picker did not produce itself - one typed into the field - is one
    // it offers. The step grids are left out of it, exactly as they are in the TimePicker: a step is how far the
    // controls move, not a claim that nothing between two of their stops exists.
    private bool IsTimeOfDayAllowed(DateTime dateTime)
    {
        if (ShowTimePicker is false) return true;

        if (AllowedHours is not null && AllowedHours(dateTime.Hour) is false) return false;
        if (AllowedMinutes is not null && AllowedMinutes(dateTime.Minute) is false) return false;
        if (ShowSeconds && AllowedSeconds is not null && AllowedSeconds(dateTime.Second) is false) return false;

        var (min, max) = GetTimeBounds(dateTime.Date);

        var time = new TimeSpan(dateTime.Hour, dateTime.Minute, ShowSeconds ? dateTime.Second : 0);

        if (min.HasValue && time < min.Value) return false;
        if (max.HasValue && time > max.Value) return false;

        return true;
    }

    /// <inheritdoc cref="IsHourAllowed"/>
    private (TimeSpan? Min, TimeSpan? Max) GetTimeBounds()
    {
        // The picker writes into the value, so the day it is setting the time of is the day the value is on.
        // Without one it produces nothing and only the bounds that hold on every day constrain it.
        return GetTimeBounds(CurrentValue.HasValue ? GetDateTime(CurrentValue.Value).Date : null);
    }

    /// <inheritdoc cref="IsHourAllowed"/>
    private (TimeSpan? Min, TimeSpan? Max) GetTimeBounds(DateTime? day)
    {
        // Only a picker carrying a time picker picks a time at all. Without one the time of the value is
        // whatever it was given rather than something the user chose here, and a day bound rules out days:
        // pulling today's midnight up to this very minute would hand the same minute to every later day
        // picked afterwards, none of it ever shown.
        if (ShowTimePicker is false) return (null, null);

        // MinTime and MaxTime bound the hours of every day the picker offers, so they hold whether or not there
        // is a value yet - which is what lets a picker of business hours open on one.
        var min = TruncateBound(BitTimeSteps.ClampToDay(MinTime));
        var max = TruncateBound(BitTimeSteps.ClampToDay(MaxTime));

        if (day.HasValue)
        {
            // MinDate and MaxDate bound the hours of the one day they fall on, and where both have something to
            // say about it the narrower of the two wins.
            var minDate = GetMinDate();
            if (minDate.HasValue)
            {
                var date = GetDateTime(minDate.Value);
                if (date.Date == day.Value.Date)
                {
                    // The seconds of a bound are dropped rather than rounded up while the picker does not show
                    // them, so the minute it falls in - the one a bound of "now" is in - is a minute the picker
                    // can still be set to.
                    var bound = new TimeSpan(date.Hour, date.Minute, ShowSeconds ? date.Second : 0);
                    if (min.HasValue is false || bound > min.Value)
                    {
                        min = bound;
                    }
                }
            }

            var maxDate = GetMaxDate();
            if (maxDate.HasValue)
            {
                var date = GetDateTime(maxDate.Value);
                if (date.Date == day.Value.Date)
                {
                    var bound = new TimeSpan(date.Hour, date.Minute, ShowSeconds ? date.Second : 0);
                    if (max.HasValue is false || bound < max.Value)
                    {
                        max = bound;
                    }
                }
            }
        }

        return (min, max);
    }

    // A bound taken to the precision the picker shows: with no seconds field on screen, the seconds of a bound
    // are dropped rather than rounded up, so the minute it falls in is a minute the picker can still be set to -
    // the same convention the day bounds are read with.
    private TimeSpan? TruncateBound(TimeSpan? bound)
    {
        if (ShowSeconds || bound.HasValue is false) return bound;

        return new TimeSpan(bound.Value.Hours, bound.Value.Minutes, 0);
    }

    /// <inheritdoc cref="ClampTimeToBounds(DateTime?)"/>
    private void ClampTimeToBounds()
    {
        ClampTimeToBounds(CurrentValue.HasValue ? GetDateTime(CurrentValue.Value).Date : null);
    }

    // The time brought into what the bounds allow on the given day. The bound itself is where an out of
    // range time lands, whether or not it sits on the step grid: a time the application declared is a time
    // the picker may produce.
    private void ClampTimeToBounds(DateTime? day)
    {
        var (min, max) = GetTimeBounds(day);

        if (min.HasValue is false && max.HasValue is false) return;

        var time = new TimeSpan(_hour, _minute, _second);

        if (min.HasValue && time < min.Value)
        {
            _hour = min.Value.Hours;
            _minute = min.Value.Minutes;
            _second = ShowSeconds ? min.Value.Seconds : 0;
        }
        else if (max.HasValue && time > max.Value)
        {
            _hour = max.Value.Hours;
            _minute = max.Value.Minutes;
            _second = ShowSeconds ? max.Value.Seconds : 0;
        }
    }

    // A committed hour or minute - the field left, Enter pressed, an arrow key or a spinner of the number
    // input stepped, each of which fires a change of its own - is held to the HourStep/MinuteStep grid and
    // to the bounds of the day, so the two halves of the same control cannot disagree about which times the
    // picker offers. The value typed on the way there was left alone (see the setters above), so the grid
    // never rewrites a half-typed number; the value the editing started from is what tells a move of exactly
    // one - an arrow key - from a number that was typed, so a step of a sparser grid moves on rather than
    // sitting still.
    private async Task HandleOnTimeInputChange(TimeUnit unit)
    {
        if (IsEnabled is false || ReadOnly) return;

        // A typed number is brought inside the bounds before the grid has its say: distance on a clock is
        // measured around it, so the allowed value nearest to a number below the minimum can be the one late
        // on the other side of the day - while what was meant is plainly the earliest time on offer.
        ClampTimeToBounds();

        switch (unit)
        {
            case TimeUnit.Hour:
                _hour = BitTimeSteps.FindAllowedNear(_hour, StartOfEdit(_hourInputCount, _hourBeforeInput), 24, IsHourAllowed) ?? _hour;
                _minute = BitTimeSteps.FindNearestAllowed(_minute, 60, IsMinuteAllowed) ?? _minute;
                _second = BitTimeSteps.FindNearestAllowed(_second, 60, IsSecondAllowed) ?? _second;
                _hourInputCount = 0;
                break;
            case TimeUnit.Minute:
                _minute = BitTimeSteps.FindAllowedNear(_minute, StartOfEdit(_minuteInputCount, _minuteBeforeInput), 60, IsMinuteAllowed) ?? _minute;
                _second = BitTimeSteps.FindNearestAllowed(_second, 60, IsSecondAllowed) ?? _second;
                _minuteInputCount = 0;
                break;
            default:
                _second = BitTimeSteps.FindAllowedNear(_second, StartOfEdit(_secondInputCount, _secondBeforeInput), 60, IsSecondAllowed) ?? _second;
                _secondInputCount = 0;
                break;
        }

        await UpdateCurrentValue();
    }

    // The value an edit of a time field started from, to whoever is deciding whether it was stepped or typed -
    // and nothing at all once more than one input event has landed, since only a step fires a single one.
    private static int? StartOfEdit(int inputCount, int valueBeforeInput) => inputCount == 1 ? valueBeforeInput : null;

    // The hour, the minute and the second answer PageUp and PageDown with the same step the spin buttons next
    // to them move by, so the time can be set without leaving the keyboard or the field.
    private async Task HandleOnTimeInputKeyDown(KeyboardEventArgs e, TimeUnit unit)
    {
        if (IsEnabled is false || ReadOnly) return;

        if (e.Key is not ("PageUp" or "PageDown")) return;

        await ChangeTime(e.Key is "PageUp", unit);
    }

    // Where the focus lands when the callout opens: the grid the callout opens onto, which is the day picker
    // unless the picker is in a mode that starts on the months.
    private void FocusCalloutOnOpen()
    {
        if (ShowDayPicker())
        {
            _focusedDate = GetFocusableDay();
            _focusElementIdAfterRender = GetDayButtonId(_focusedDate.Value);
        }
        else if (ShowMonthPicker())
        {
            // With no day picker on screen (the MonthPicker mode, or the month picker as an overlay)
            // the month grid is what the callout opens onto.
            _focusedMonthCell = GetFocusableMonth();
            _focusElementIdAfterRender = GetMonthButtonId(_focusedMonthCell.Value);
        }
    }

    private void OnSetIsOpen()
    {
        // Captured now: the lambda below runs later, so a rapid second change to IsOpen before it has run
        // must not make both invocations act on the same (latest) state.
        var isOpen = IsOpen;

        // The internal open/close flows toggle the callout themselves right after assigning IsOpen, so they
        // can await the toggle and order their focus work after it. The hook only toggles for a change pushed
        // from the outside through the IsOpen parameter, which otherwise has no path to the JS side that
        // actually shows and hides the callout. Before the first render there is no element to toggle (and
        // during prerendering not even a JS runtime to call); an initial IsOpen is applied by OnAfterRenderAsync.
        if (_internalIsOpenChange || IsRendered is false || Standalone) return;

        _ = InvokeAsync(async () =>
        {
            if (isOpen)
            {
                await PrepareCalloutForOpen();
                StateHasChanged();
            }

            await ToggleCallout();

            // The callout holds the tab order while it is open, so an open pushed in from the outside has to
            // move the focus into it exactly as a click on the field does - otherwise the focus is left on the
            // page behind an overlay that it can no longer reach. The exception is the same one the click makes:
            // an editable field keeps the focus, since the text the person came to type has to keep it.
            if (isOpen && (AllowTextInput is false || ReadOnly))
            {
                FocusCalloutOnOpen();
                StateHasChanged();
            }

            await (isOpen ? OnOpen.InvokeAsync() : OnClose.InvokeAsync());
        });
    }

    // The flows that follow AssignIsOpen with their own awaited ToggleCallout mark the change as internal,
    // so the OnSetIsOpen hook does not toggle the callout a second time.
    private async Task<bool> AssignIsOpenInternal(bool value)
    {
        _internalIsOpenChange = true;
        try
        {
            return await AssignIsOpen(value);
        }
        finally
        {
            _internalIsOpenChange = false;
        }
    }

    private async Task CloseCallout()
    {
        if (IsEnabled is false) return;

        if (await AssignIsOpenInternal(false) is false) return;

        await ToggleCallout();

        await OnClose.InvokeAsync();

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
            dropDirection: DropDirection,
            // The same direction the callout renders in (bit-dtp-rtl covers the culture-implied RTL
            // as well), so the positioning matches the layout.
            isRtl: IsRtl(),
            scrollContainerId: "",
            scrollOffset: 0,
            headerId: CalloutHeaderTemplate is not null ? _headerId : "",
            footerId: CalloutFooterTemplate is not null ? _footerId : "",
            setCalloutWidth: false,
            fixedCalloutWidth: false,
            maxWindowWidth: GetMaxWidth());
    }

    // Every extra month widens the callout, so the threshold that decides whether the pickers have to
    // collapse into overlays has to account for them - at the width the panes of the size in force actually
    // have, which a Large picker overran while every size was measured as the medium one.
    private int GetMaxWidth(int? monthCount = null)
    {
        var pane = Size switch
        {
            BitSize.Small => PANE_WIDTH_SM,
            BitSize.Large => PANE_WIDTH_LG,
            _ => PANE_WIDTH_MD
        };

        // The month picker stands beside the strip in a pane of its own, and every month after the first
        // drops the padding on its leading edge (.bit-dtp-dwp + .bit-dtp-dwp).
        var width = (2 * pane) + WIDTH_SLACK + (((monthCount ?? _renderedMonths.Length) - 1) * (pane - PANE_PADDING));

        // A seconds field is a whole column more of the time pane, so the viewport that still holds the pickers
        // side by side has to be that much wider before they stop collapsing into overlays.
        return ShowTimePicker && ShowSeconds ? width + SECONDS_WIDTH : width;
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

    private async Task HandleNowButtonClick()
    {
        if (ReadOnly) return;
        if (IsEnabled is false) return;

        var now = GetToday();

        // "Now" is a time like any other the picker produces, so the bounds, the allowed values and the step
        // grids all have the same say over it: what it lands on is the nearest time on offer.
        _hour = BitTimeSteps.FindNearestAllowed(now.Hour, 24, IsHourAllowed) ?? now.Hour;
        _minute = BitTimeSteps.FindNearestAllowed(now.Minute, 60, IsMinuteAllowed) ?? now.Minute;
        _second = ShowSeconds ? BitTimeSteps.FindNearestAllowed(now.Second, 60, IsSecondAllowed) ?? now.Second : 0;

        ClampTimeToBounds();

        // "Now" names a whole instant, not a time of day, so on a picker with no value yet it picks today as
        // well - the time picker writes into the value, and without one the button would be a control that
        // never does anything. Today being one of the days the picker rules out is the one case it cannot.
        if (CurrentValue.HasValue is false)
        {
            var today = GetToday().Date;

            if (IsDayDisabled(today)) return;

            await SelectDate(today);

            return;
        }

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
