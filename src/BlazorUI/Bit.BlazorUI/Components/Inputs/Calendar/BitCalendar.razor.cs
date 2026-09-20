using System.Text;
using System.Globalization;
using System.Diagnostics.CodeAnalysis;

namespace Bit.BlazorUI;

/// <summary>
/// BitCalendar is a fully-featured inline calendar for browsing and selecting a single date, or a date and time when the built-in time picker is enabled.
/// It offers day, month and year grids - each of them operated with the arrow keys the way the WAI-ARIA grid pattern prescribes - day events with a details
/// dialog, flexible day/week rules, header and footer slots, three sizes, and any culture and time zone, including the non-Gregorian ones.
/// </summary>
public partial class BitCalendar : BitInputBase<DateTimeOffset?>
{
    private const int DEFAULT_WEEK_COUNT = 6;
    private const int DEFAULT_DAY_COUNT_PER_WEEK = 7;



    private int _currentYear;
    private int _currentMonth;
    private bool _showYearPicker;
    private bool _showTimePicker;
    private bool _showMonthPicker;
    private bool _showEventModal;
    private DateTime? _focusedDate;
    private int? _focusedYearCell;
    private int? _focusedMonthCell;
    private string? _focusElementIdAfterRender;
    private bool _focusTimePickerAfterRender;
    private bool _eventModalFocusTrapped;
    private bool _eventModalFocusStored;
    private DateOnly _eventModalDate;
    private HashSet<DateTime> _disabledDates = [];
    private HashSet<DateTime> _highlightedDates = [];
    private HashSet<DayOfWeek> _disabledDaysOfWeek = [];
    private Dictionary<DateOnly, List<BitCalendarEvent>> _eventsByDate = [];
    private int _yearPickerEndYear;
    private int _yearPickerStartYear;
    private string _monthTitle = string.Empty;
    private ElementReference _inputTimeHourRef = default!;
    private ElementReference _inputTimeMinuteRef = default!;
    private TimeZoneInfo _timeZone = TimeZoneInfo.Local;
    private CultureInfo _culture = CultureInfo.CurrentUICulture;
    private CancellationTokenSource _cancellationTokenSource = new();
    private readonly DateTime?[,] _daysOfCurrentMonth = new DateTime?[DEFAULT_WEEK_COUNT, DEFAULT_DAY_COUNT_PER_WEEK];

    // The parts of the event dialog that have to name one another: the surface the focus is trapped in and
    // restored from, and the heading it takes its accessible name from.
    private string _eventDialogId => $"{_Id}-event-dialog";
    private string _eventDialogTitleId => $"{_Id}-event-dialog-title";



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
            if (IsEnabled is false || ReadOnly) return;

            int candidate;

            if (TimeFormat == BitTimeFormat.TwelveHours)
            {
                // A value typed into the hour is an hour of the clock face (1-12), so it lands in the half of
                // the day the picker is already on: typing 5 into an afternoon time means 17:00, not a silent
                // flip to the morning - which half the time is in is what the AM/PM pair is there to change.
                // Both 12 and 0 mean the top of the clock, which is hour zero of the half.
                candidate = BitTimeSteps.Wrap(value, 12) + (_hour >= 12 ? 12 : 0);
            }
            else
            {
                candidate = Math.Clamp(value, 0, 23);
            }

            // What the spin buttons produce is held to the HourStep grid, so what is typed - and what the
            // arrow keys of the number input produce, which is a typed value of one more or one less - is
            // held to it too, instead of the two halves of the same control disagreeing about the same hour.
            _hour = BitTimeSteps.FindAllowedNear(candidate, _hour, 24, IsHourOnGrid) ?? _hour;

            UpdateTime();
        }
    }

    private int _minute;
    private int _minuteView
    {
        get => _minute;
        set
        {
            if (IsEnabled is false || ReadOnly) return;

            // Held to the MinuteStep grid for the same reason the hour above is held to its own.
            _minute = BitTimeSteps.FindAllowedNear(Math.Clamp(value, 0, 59), _minute, 60, IsMinuteOnGrid) ?? _minute;

            UpdateTime();
        }
    }



    [Inject] private IJSRuntime _js { get; set; } = default!;



    /// <summary>
    /// Whether selecting the already selected day deselects it, clearing the value.
    /// </summary>
    [Parameter] public bool AllowDeselect { get; set; }

    /// <summary>
    /// Custom CSS classes for different parts of the BitCalendar component.
    /// </summary>
    [Parameter] public BitCalendarClassStyles? Classes { get; set; }

    /// <summary>
    /// The general color of the calendar that applies to the today day button, the highlighted current month,
    /// the selected AM/PM button, and the event indicators.
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
    /// CultureInfo for the Calendar.
    /// </summary>
    [Parameter, ResetClassBuilder]
    [CallOnSet(nameof(OnSetParameters))]
    public CultureInfo? Culture { get; set; }

    /// <summary>
    /// The format of the date in the Calendar.
    /// </summary>
    [Parameter] public string? DateFormat { get; set; }

    /// <summary>
    /// Used to customize how content inside the day cell is rendered.
    /// </summary>
    [Parameter] public RenderFragment<DateTimeOffset>? DayCellTemplate { get; set; }

    /// <summary>
    /// Disables every day after today, exactly as a <see cref="MaxDate"/> of today would.
    /// When both are set, the earlier of the two bounds wins.
    /// </summary>
    [Parameter]
    [CallOnSet(nameof(OnSetParameters))]
    public bool DisableFuture { get; set; }

    /// <summary>
    /// Disables every day before today, exactly as a <see cref="MinDate"/> of today would.
    /// When both are set, the later of the two bounds wins.
    /// </summary>
    [Parameter]
    [CallOnSet(nameof(OnSetParameters))]
    public bool DisablePast { get; set; }

    /// <summary>
    /// The list of dates that are disabled (not selectable) in the calendar, in addition to MinDate and MaxDate.
    /// </summary>
    [Parameter] public IEnumerable<DateTimeOffset>? DisabledDates { get; set; }

    /// <summary>
    /// The days of the week that are disabled (not selectable) in the calendar (e.g. weekends).
    /// </summary>
    [Parameter] public IEnumerable<DayOfWeek>? DisabledDaysOfWeek { get; set; }

    /// <summary>
    /// The list of events to display on calendar days. The events of a day are ordered the way an agenda of it
    /// is: the all-day ones (which carry no start time) first, then the rest by the time they start at.
    /// </summary>
    [Parameter] public IEnumerable<BitCalendarEvent>? Events { get; set; }

    /// <summary>
    /// The title (tooltip) and the accessible name of the close button of the event details dialog.
    /// </summary>
    [Parameter] public string EventDetailsCloseButtonTitle { get; set; } = "Close";

    /// <summary>
    /// The text shown before the start time of an event when only a start time is present (e.g. "From 09:00").
    /// </summary>
    [Parameter] public string EventTimeFromText { get; set; } = "From";

    /// <summary>
    /// The text shown before the end time of an event when only an end time is present (e.g. "Until 17:00").
    /// </summary>
    [Parameter] public string EventTimeUntilText { get; set; } = "Until";

    /// <summary>
    /// Rendered under the pickers, inside the root of the calendar: the place for the actions a calendar is
    /// often given of its own, such as a Clear button or a summary of the selection.
    /// </summary>
    [Parameter] public RenderFragment? FooterTemplate { get; set; }

    /// <summary>
    /// Overrides the first day of the week in the day picker. If not set, the first day of the week of the Culture is used.
    /// </summary>
    [Parameter]
    [CallOnSet(nameof(OnSetParameters))]
    public DayOfWeek? FirstDayOfWeek { get; set; }

    /// <summary>
    /// Whether the day picker should always render six weeks, filling the extra rows with the days of the adjacent months,
    /// to keep the calendar height fixed while navigating between months.
    /// </summary>
    [Parameter]
    [CallOnSet(nameof(OnSetParameters))]
    public bool FixedWeeks { get; set; }

    /// <summary>
    /// Custom function to provide additional CSS classes for each day button of the calendar.
    /// </summary>
    [Parameter] public Func<DateTimeOffset, string?>? GetDayClass { get; set; }

    /// <summary>
    /// Rendered above the pickers, inside the root of the calendar: the place for a caption or a legend of
    /// its own, before the day, month and year grids.
    /// </summary>
    [Parameter] public RenderFragment? HeaderTemplate { get; set; }

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
    /// Gets or sets the icon to display in the now button using custom CSS classes for external icon libraries.
    /// Takes precedence over <see cref="NowButtonIconName"/> when both are set.
    /// </summary>
    [Parameter] public BitIconInfo? NowButtonIcon { get; set; }

    /// <summary>
    /// Gets or sets the name of the icon to display in the now button from the built-in Fluent UI icons.
    /// </summary>
    [Parameter] public string? NowButtonIconName { get; set; }

    /// <summary>
    /// The title of the now button (tooltip).
    /// </summary>
    [Parameter] public string NowButtonTitle { get; set; } = "Go to now";

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
    /// Gets or sets the icon to display in the GoToToday button using custom CSS classes for external icon libraries.
    /// Takes precedence over <see cref="GoToTodayIconName"/> when both are set.
    /// </summary>
    [Parameter] public BitIconInfo? GoToTodayIcon { get; set; }

    /// <summary>
    /// Gets or sets the name of the icon to display in the GoToToday button from the built-in Fluent UI icons.
    /// </summary>
    [Parameter] public string? GoToTodayIconName { get; set; }

    /// <summary>
    /// The title of the GoToToday button (tooltip).
    /// </summary>
    [Parameter] public string GoToTodayTitle { get; set; } = "Go to today";

    /// <summary>
    /// Gets or sets the icon to display in the HideTimePicker button using custom CSS classes for external icon libraries.
    /// Takes precedence over <see cref="HideTimePickerIconName"/> when both are set.
    /// </summary>
    [Parameter] public BitIconInfo? HideTimePickerIcon { get; set; }

    /// <summary>
    /// Gets or sets the name of the icon to display in the HideTimePicker button from the built-in Fluent UI icons.
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
    /// Whether the day picker should highlight today's day. It only affects the visual style of the day
    /// cell; the accessibility attributes still report the day as the current date.
    /// </summary>
    [Parameter] public bool HighlightToday { get; set; } = true;

    /// <summary>
    /// The step, in hours, the spin buttons of the time picker move the hour by.
    /// </summary>
    /// <remarks>
    /// A step greater than 1 lays a grid over the day that every hour the picker produces sits on, starting at
    /// midnight, so a picker that only accepts times on a three-hour grid can say so. The buttons, the keys and
    /// what is typed into the hour are all held to it. Values below 1 are treated as 1.
    /// </remarks>
    [Parameter] public int HourStep { get; set; } = 1;

    /// <summary>
    /// The custom validation error message for the invalid value.
    /// </summary>
    [Parameter] public string? InvalidErrorMessage { get; set; }

    /// <summary>
    /// Custom function to determine if a specific date is disabled (not selectable) in the calendar.
    /// </summary>
    [Parameter] public Func<DateTimeOffset, bool>? IsDateDisabled { get; set; }

    /// <summary>
    /// The maximum allowable date of the calendar.
    /// </summary>
    [Parameter]
    [CallOnSet(nameof(OnSetParameters))]
    public DateTimeOffset? MaxDate { get; set; }

    /// <summary>
    /// The minimum allowable date of the calendar.
    /// </summary>
    [Parameter]
    [CallOnSet(nameof(OnSetParameters))]
    public DateTimeOffset? MinDate { get; set; }

    /// <summary>
    /// The step, in minutes, the spin buttons of the time picker move the minute by.
    /// </summary>
    /// <remarks>
    /// A step greater than 1 lays a grid over the hour that every minute the picker produces sits on, starting
    /// at the top of the hour, which is what turns it into a five-minute or quarter-hour picker. The buttons,
    /// the keys and what is typed into the minute are all held to it. Values below 1 are treated as 1.
    /// </remarks>
    [Parameter] public int MinuteStep { get; set; } = 1;

    /// <summary>
    /// Used to customize how content inside the month cell is rendered. 
    /// </summary>
    [Parameter] public RenderFragment<DateTimeOffset>? MonthCellTemplate { get; set; }

    /// <summary>
    /// The title of the month picker's toggle (tooltip).
    /// </summary>
    [Parameter] public string MonthPickerToggleTitle { get; set; } = "{0}, change month";

    /// <summary>
    /// Gets or sets the icon to display in the Go to next month button using custom CSS classes for external icon libraries.
    /// Takes precedence over <see cref="NextMonthNavIconName"/> when both are set.
    /// </summary>
    [Parameter] public BitIconInfo? NextMonthNavIcon { get; set; }

    /// <summary>
    /// Gets or sets the name of the icon to display in the Go to next month button from the built-in Fluent UI icons.
    /// </summary>
    [Parameter] public string? NextMonthNavIconName { get; set; }

    /// <summary>
    /// Gets or sets the icon to display in the Go to next year button using custom CSS classes for external icon libraries.
    /// Takes precedence over <see cref="NextYearNavIconName"/> when both are set.
    /// </summary>
    [Parameter] public BitIconInfo? NextYearNavIcon { get; set; }

    /// <summary>
    /// Gets or sets the name of the icon to display in the Go to next year button from the built-in Fluent UI icons.
    /// </summary>
    [Parameter] public string? NextYearNavIconName { get; set; }

    /// <summary>
    /// Gets or sets the icon to display in the Go to next year range button using custom CSS classes for external icon libraries.
    /// Takes precedence over <see cref="NextYearRangeNavIconName"/> when both are set.
    /// </summary>
    [Parameter] public BitIconInfo? NextYearRangeNavIcon { get; set; }

    /// <summary>
    /// Gets or sets the name of the icon to display in the Go to next year range button from the built-in Fluent UI icons.
    /// </summary>
    [Parameter] public string? NextYearRangeNavIconName { get; set; }

    /// <summary>
    /// Callback for when the displayed month of the day picker changes.
    /// The argument is the first day of the newly displayed month.
    /// </summary>
    [Parameter] public EventCallback<DateTimeOffset> OnMonthChange { get; set; }

    /// <summary>
    /// Callback for when the user selects a date.
    /// </summary>
    [Parameter] public EventCallback<DateTimeOffset?> OnSelectDate { get; set; }

    /// <summary>
    /// Gets or sets the icon to display in the Go to previous month button using custom CSS classes for external icon libraries.
    /// Takes precedence over <see cref="PrevMonthNavIconName"/> when both are set.
    /// </summary>
    [Parameter] public BitIconInfo? PrevMonthNavIcon { get; set; }

    /// <summary>
    /// Gets or sets the name of the icon to display in the Go to previous month button from the built-in Fluent UI icons.
    /// </summary>
    [Parameter] public string? PrevMonthNavIconName { get; set; }

    /// <summary>
    /// Gets or sets the icon to display in the Go to previous year button using custom CSS classes for external icon libraries.
    /// Takes precedence over <see cref="PrevYearNavIconName"/> when both are set.
    /// </summary>
    [Parameter] public BitIconInfo? PrevYearNavIcon { get; set; }

    /// <summary>
    /// Gets or sets the name of the icon to display in the Go to previous year button from the built-in Fluent UI icons.
    /// </summary>
    [Parameter] public string? PrevYearNavIconName { get; set; }

    /// <summary>
    /// Gets or sets the icon to display in the Go to previous year range button using custom CSS classes for external icon libraries.
    /// Takes precedence over <see cref="PrevYearRangeNavIconName"/> when both are set.
    /// </summary>
    [Parameter] public BitIconInfo? PrevYearRangeNavIcon { get; set; }

    /// <summary>
    /// Gets or sets the name of the icon to display in the Go to previous year range button from the built-in Fluent UI icons.
    /// </summary>
    [Parameter] public string? PrevYearRangeNavIconName { get; set; }

    /// <summary>
    /// The template of the text a screen reader is given when the selection changes, where {0} is the selected
    /// date written with the <see cref="DateFormat"/>. Nothing is announced while no date is selected.
    /// </summary>
    [Parameter] public string SelectedDateAriaAtomic { get; set; } = "Selected date {0}";

    /// <summary>
    /// Whether the now button should be shown or not.
    /// </summary>
    [Parameter] public bool ShowNowButton { get; set; } = true;

    /// <summary>
    /// Whether the GoToToday button should be shown or not.
    /// </summary>
    [Parameter] public bool ShowGoToToday { get; set; } = true;

    /// <summary>
    /// Whether clicking a day that carries events opens the dialog listing them.
    /// The day is selected either way; turning this off leaves the events to the indicator, the tooltip and
    /// whatever the page itself shows for the selected day.
    /// </summary>
    [Parameter] public bool ShowEventDetails { get; set; } = true;

    /// <summary>
    /// Whether the month picker is shown or hidden.
    /// </summary>
    [Parameter]
    [CallOnSet(nameof(OnSetParameters))]
    public bool ShowMonthPicker { get; set; } = true;

    /// <summary>
    /// Show month picker on top of date picker when visible.
    /// </summary>
    [Parameter] public bool ShowMonthPickerAsOverlay { get; set; }

    /// <summary>
    /// Whether the days of the previous and next months should be shown in the day picker.
    /// </summary>
    [Parameter] public bool ShowOutsideDays { get; set; } = true;

    /// <summary>
    /// Whether the time picker should be shown or not.
    /// </summary>
    [Parameter]
    [CallOnSet(nameof(OnSetParameters))]
    public bool ShowTimePicker { get; set; }

    /// <summary>
    /// Show time picker on top of date picker when visible.
    /// </summary>
    [Parameter] public bool ShowTimePickerAsOverlay { get; set; }

    /// <summary>
    /// Gets or sets the icon to display in the ShowTimePicker button using custom CSS classes for external icon libraries.
    /// Takes precedence over <see cref="ShowTimePickerIconName"/> when both are set.
    /// </summary>
    [Parameter] public BitIconInfo? ShowTimePickerIcon { get; set; }

    /// <summary>
    /// Gets or sets the name of the icon to display in the ShowTimePicker button from the built-in Fluent UI icons.
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
    /// The size of the calendar, which scales its cells and their text.
    /// </summary>
    [Parameter, ResetClassBuilder]
    public BitSize? Size { get; set; }

    /// <summary>
    /// Specifies the date and time of the calendar when it is showing without any selected value.
    /// </summary>
    [Parameter]
    [CallOnSet(nameof(OnSetParameters))]
    public DateTimeOffset? StartingValue { get; set; }

    /// <summary>
    /// Custom CSS styles for different parts of the BitCalendar component.
    /// </summary>
    [Parameter] public BitCalendarClassStyles? Styles { get; set; }

    /// <summary>
    /// The time format of the time-picker, 24H or 12H.
    /// </summary>
    [Parameter] public BitTimeFormat TimeFormat { get; set; }

    /// <summary>
    /// Gets or sets the icon to display in the decrease-hour button using custom CSS classes for external icon libraries.
    /// Takes precedence over <see cref="TimePickerDecreaseHourIconName"/> when both are set.
    /// </summary>
    [Parameter] public BitIconInfo? TimePickerDecreaseHourIcon { get; set; }

    /// <summary>
    /// Gets or sets the name of the icon to display in the decrease-hour button from the built-in Fluent UI icons.
    /// </summary>
    [Parameter] public string? TimePickerDecreaseHourIconName { get; set; }

    /// <summary>
    /// The title (tooltip) and the accessible name of the time-picker's decrease-hour button.
    /// </summary>
    [Parameter] public string TimePickerDecreaseHourTitle { get; set; } = "Decrease hour";

    /// <summary>
    /// The title (tooltip) and the accessible name of the time-picker's decrease-minute button.
    /// </summary>
    [Parameter] public string TimePickerDecreaseMinuteTitle { get; set; } = "Decrease minute";

    /// <summary>
    /// Gets or sets the icon to display in the decrease-minute button using custom CSS classes for external icon libraries.
    /// Takes precedence over <see cref="TimePickerDecreaseMinuteIconName"/> when both are set.
    /// </summary>
    [Parameter] public BitIconInfo? TimePickerDecreaseMinuteIcon { get; set; }

    /// <summary>
    /// Gets or sets the name of the icon to display in the decrease-minute button from the built-in Fluent UI icons.
    /// </summary>
    [Parameter] public string? TimePickerDecreaseMinuteIconName { get; set; }

    /// <summary>
    /// The title (tooltip) and the accessible name of the time-picker's hour input.
    /// </summary>
    [Parameter] public string TimePickerHourTitle { get; set; } = "Hour";

    /// <summary>
    /// The title (tooltip) and the accessible name of the time-picker's minute input.
    /// </summary>
    [Parameter] public string TimePickerMinuteTitle { get; set; } = "Minute";

    /// <summary>
    /// Gets or sets the icon to display in the increase-hour button using custom CSS classes for external icon libraries.
    /// Takes precedence over <see cref="TimePickerIncreaseHourIconName"/> when both are set.
    /// </summary>
    [Parameter] public BitIconInfo? TimePickerIncreaseHourIcon { get; set; }

    /// <summary>
    /// Gets or sets the name of the icon to display in the increase-hour button from the built-in Fluent UI icons.
    /// </summary>
    [Parameter] public string? TimePickerIncreaseHourIconName { get; set; }

    /// <summary>
    /// Gets or sets the icon to display in the increase-minute button using custom CSS classes for external icon libraries.
    /// Takes precedence over <see cref="TimePickerIncreaseMinuteIconName"/> when both are set.
    /// </summary>
    [Parameter] public BitIconInfo? TimePickerIncreaseMinuteIcon { get; set; }

    /// <summary>
    /// Gets or sets the name of the icon to display in the increase-minute button from the built-in Fluent UI icons.
    /// </summary>
    [Parameter] public string? TimePickerIncreaseMinuteIconName { get; set; }

    /// <summary>
    /// The title (tooltip) and the accessible name of the time-picker's increase-hour button.
    /// </summary>
    [Parameter] public string TimePickerIncreaseHourTitle { get; set; } = "Increase hour";

    /// <summary>
    /// The title (tooltip) and the accessible name of the time-picker's increase-minute button.
    /// </summary>
    [Parameter] public string TimePickerIncreaseMinuteTitle { get; set; } = "Increase minute";

    /// <summary>
    /// TimeZone for the DatePicker.
    /// </summary>
    [Parameter]
    [CallOnSet(nameof(OnSetParameters))]
    public TimeZoneInfo? TimeZone { get; set; }

    /// <summary>
    /// Overrides the current date and time considered as "today" and "now" in the calendar (useful for testing or custom time providers).
    /// </summary>
    [Parameter] public DateTimeOffset? Today { get; set; }

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
    /// Used to customize how content inside the year cell is rendered.
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



    protected override string RootElementClass { get; } = "bit-cal";

    protected override void RegisterCssClasses()
    {
        ClassBuilder.Register(() => Classes?.Root);

        ClassBuilder.Register(() => BitCssClasses.Color(Color, "bit-cal"));

        ClassBuilder.Register(() => BitCssClasses.Size(Size, "bit-cal"));

        // A culture that writes right to left implies the direction of the calendar as well, so one that was
        // given no explicit Dir still lays itself out the way its culture reads.
        ClassBuilder.Register(() => BitCssClasses.CultureRtl(Dir, _culture));
    }

    protected override void RegisterCssStyles()
    {
        StyleBuilder.Register(() => Styles?.Root);
    }

    protected override void OnInitialized()
    {
        SetDefaultValue();

        OnValueChanged += HandleOnValueChanged;

        OnSetParameters();

        base.OnInitialized();
    }

    protected override void OnParametersSet()
    {
        base.OnParametersSet();

        BuildEventsLookup();
        BuildDatesLookups();

        // The dialog lists the events of its day, so it goes away with the last of them - a page that clears
        // the events of the visible month is not left with an empty dialog over its calendar.
        if (_showEventModal && GetEventModalEvents().Count == 0)
        {
            CloseEventModal();
        }
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        await base.OnAfterRenderAsync(firstRender);

        if (firstRender)
        {
            try
            {
                // Prevents the default behavior (scrolling) of the navigation keys handled by the
                // day buttons' keydown handlers, since Blazor cannot conditionally preventDefault per key.
                await _js.BitCalendarsSetup(_Id);
            }
            catch (JSDisconnectedException) { } // we can ignore this exception here
        }

        // The cell the keyboard moved to, which can be in a month - or in a whole picker - that was not on
        // screen when the key was pressed, so the focus is placed after the render that brought it there.
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

        await SyncEventDialogFocus();
    }

    /// <summary>
    /// Gives focus to the calendar: the day of the day grid that is in the tab sequence, or the month, the
    /// year or the hour input when the day grid is not the picker on screen.
    /// </summary>
    /// <remarks>
    /// The calendar renders no input element of its own - what the keyboard operates is a grid of buttons -
    /// so this overrides the base implementation rather than focusing a field.
    /// </remarks>
    public override ValueTask FocusAsync() => FocusAsync(false);

    /// <inheritdoc cref="FocusAsync()"/>
    /// <param name="preventScroll">Whether the browser should refrain from scrolling the newly focused cell
    /// into view.</param>
    public override async ValueTask FocusAsync(bool preventScroll)
    {
        MoveFocusToTheVisiblePicker();

        if (_focusElementIdAfterRender.HasValue())
        {
            var elementId = _focusElementIdAfterRender!;
            _focusElementIdAfterRender = null;

            try
            {
                await _js.BitCalendarsFocusCell(elementId, preventScroll);
            }
            catch (JSDisconnectedException) { } // we can ignore this exception here

            return;
        }

        if (_focusTimePickerAfterRender is false) return;

        _focusTimePickerAfterRender = false;

        try
        {
            await _inputTimeHourRef.FocusAsync(preventScroll);
        }
        catch (JSDisconnectedException) { } // we can ignore this exception here
    }

    protected override bool TryParseValueFromString(string? value, [MaybeNullWhen(false)] out DateTimeOffset? result, [NotNullWhen(false)] out string? validationErrorMessage)
    {
        if (value.HasNoValue())
        {
            result = null;
            validationErrorMessage = null;
            return true;
        }

        if (DateTime.TryParseExact(value, DateFormat ?? _culture.DateTimeFormat.ShortDatePattern, _culture, DateTimeStyles.None, out DateTime parsedValue))
        {
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
        return value.HasValue
            ? value.Value.ToString(DateFormat ?? _culture.DateTimeFormat.ShortDatePattern, _culture)
            : null;
    }



    // A value the user just picked is not a parameter change: the pickers that are on screen stay on screen.
    // Recomputing them here would close the time picker overlay on the first press of a spin button, and take
    // the month overlay away from under a selection made in it.
    private void HandleOnValueChanged(object? sender, EventArgs args)
    {
        OnSetParameters(resetPickers: false);
    }

    private void OnSetParameters() => OnSetParameters(true);

    private void OnSetParameters(bool resetPickers)
    {
        if (resetPickers)
        {
            _showTimePicker = ShowTimePicker && ShowTimePickerAsOverlay is false;
            _showMonthPicker = _showTimePicker is false && ShowMonthPicker && ShowMonthPickerAsOverlay is false;
        }

        _timeZone = TimeZone ?? TimeZoneInfo.Local;
        _culture = Culture ?? CultureInfo.CurrentUICulture;

        var value = CurrentValue.GetValueOrDefault(StartingValue.GetValueOrDefault(GetNow()));

        var minDate = GetMinDate();
        var maxDate = GetMaxDate();

        if (minDate.HasValue && minDate > value)
        {
            value = minDate.Value;
        }

        if (maxDate.HasValue && maxDate < value)
        {
            value = maxDate.Value;
        }

        // Everything the calendar shows - the month it opens on, the time in the time picker - belongs to the
        // TimeZone of the component, not to the offset the value happens to carry.
        var dateTime = GetDateTime(value);

        _hour = CurrentValue.HasValue || StartingValue.HasValue ? dateTime.Hour : 0;
        _minute = CurrentValue.HasValue || StartingValue.HasValue ? dateTime.Minute : 0;

        GenerateCalendarData(dateTime);
    }

    private async Task SelectDate(DateTime selectedDate)
    {
        if (ReadOnly) return;
        if (IsEnabled is false || InvalidValueBinding()) return;
        if (IsDayDisabled(selectedDate)) return;

        var previousYear = _currentYear;
        var previousMonth = _currentMonth;

        // Selecting the selected day again deselects it (AllowDeselect), leaving the calendar on the month
        // the user is looking at rather than jumping back onto today the way an emptied value otherwise would.
        if (AllowDeselect && IsSelectedDate(selectedDate))
        {
            var deselectedYear = _culture.Calendar.GetYear(selectedDate);
            var deselectedMonth = _culture.Calendar.GetMonth(selectedDate);

            _focusedDate = selectedDate;

            CurrentValue = null;

            _currentYear = deselectedYear;
            _currentMonth = deselectedMonth;

            GenerateMonthData(_currentYear, _currentMonth);

            await OnSelectDate.InvokeAsync(CurrentValue);

            await NotifyMonthChange(previousYear, previousMonth);

            return;
        }

        _focusedDate = selectedDate;

        selectedDate = selectedDate.AddHours(_hour);
        selectedDate = selectedDate.AddMinutes(_minute);

        CurrentValue = new DateTimeOffset(selectedDate, _timeZone.GetUtcOffset(selectedDate));

        _currentYear = _culture.Calendar.GetYear(selectedDate);
        _currentMonth = _culture.Calendar.GetMonth(selectedDate);

        if (_currentYear != previousYear || _currentMonth != previousMonth)
        {
            _focusElementIdAfterRender = GetDayButtonId(_focusedDate.Value);
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
        _focusedMonthCell = month;

        GenerateMonthData(_currentYear, _currentMonth);

        if (ShowMonthPickerAsOverlay || (ShowTimePicker && ShowTimePickerAsOverlay is false))
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

        ClampCurrentMonthToYear();

        GenerateMonthData(_currentYear, _currentMonth);

        ToggleBetweenMonthAndYearPicker();

        // The year that was activated goes away with the year grid, so the focus is handed to the month grid
        // that replaces it - otherwise a keyboard selection drops the focus onto the body.
        FocusMonthCell(GetFocusableMonth());

        await NotifyMonthChange(previousYear, previousMonth);
    }

    private void ToggleBetweenMonthAndYearPicker()
    {
        if (IsEnabled is false) return;

        _showYearPicker = !_showYearPicker;

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

        // The month and the year the roving tabindex was left on are not the ones on screen any more, so the
        // grids start it over on what they now display.
        _focusedYearCell = null;
        _focusedMonthCell = null;

        await NotifyMonthChange(previousYear, previousMonth);
    }

    private void HandleNowButtonClick()
    {
        if (IsEnabled is false) return;
        if (ReadOnly) return;

        var now = GetToday();

        _hour = now.Hour;
        _minute = now.Minute;

        UpdateTime();
    }

    private void GenerateCalendarData(DateTime dateTime)
    {
        var calendar = _culture.Calendar;

        // A date the culture's calendar does not support has no year or month to be read off it, so the view
        // opens on the nearest date it does - the Hebrew calendar of .NET, for one, starts in 1583.
        if (dateTime < calendar.MinSupportedDateTime)
        {
            dateTime = calendar.MinSupportedDateTime;
        }
        else if (dateTime > calendar.MaxSupportedDateTime)
        {
            dateTime = calendar.MaxSupportedDateTime;
        }

        _currentMonth = calendar.GetMonth(dateTime);
        _currentYear = calendar.GetYear(dateTime);

        _yearPickerStartYear = _currentYear - 1;
        _yearPickerEndYear = _currentYear + 10;

        GenerateMonthData(_currentYear, _currentMonth);
    }

    private void GenerateMonthData(int year, int month)
    {
        _monthTitle = $"{_culture.DateTimeFormat.GetMonthName(month)} {year}";

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
            // The first supported month of a calendar does not have to start at its first day (the minimum
            // of the Hebrew calendar falls in the middle of a month), so the weekday of the unrepresentable
            // first day is walked back from the first day the calendar does support.
            var minDate = calendar.MinSupportedDateTime;
            dayOfWeek = ((int)calendar.GetDayOfWeek(minDate) - (calendar.GetDayOfMonth(minDate) - 1)) % 7;
            if (dayOfWeek < 0)
            {
                dayOfWeek += 7;
            }
        }

        // Adjust dayOfWeek to match the culture's first day of week
        dayOfWeek = (dayOfWeek - firstDayOfWeek + 7) % 7;

        // The adjacent months are kept as plain year/month numbers of the culture's own calendar: a DateTime
        // built out of them would be a Gregorian date of a year that calendar never had, and a thirteenth
        // month - which a leap year of the Hebrew calendar does have - has no Gregorian counterpart at all.
        int monthsInYear = calendar.GetMonthsInYear(year);

        int previousYear = month == 1 ? year - 1 : year;
        int previousMonth;
        int daysInPreviousMonth;
        if (previousYear < GetMinCalendarYearMonth().Year)
        {
            // The year before the calendar's first year cannot be asked anything - every one of its days
            // comes out as an empty cell anyway, so any day numbers at all do for the counting.
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
                _daysOfCurrentMonth[i, j] = TryCreateDate(previousYear, previousMonth, day);
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
                    _daysOfCurrentMonth[i, j] = TryCreateDate(year, month, day);
                    day++;
                }
                else
                {
                    if (j == 0 && FixedWeeks is false)
                    {
                        ended = true;
                    }
                    _daysOfCurrentMonth[i, j] = ended ? null : TryCreateDate(nextYear, nextMonth, day - daysInMonth);
                    day++;
                }
            }
        }
    }

    // A day at the very edge of the calendar - the days around its first and last supported months - cannot
    // be represented as a DateTime at all, so it becomes an empty cell instead of an exception.
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
    // representable (the range of the Hebrew calendar starts in the middle of a month), so the nearest day
    // the calendar does support stands in for it.
    private DateTime GetFirstDayOfMonthOrClamp(int year, int month)
    {
        var date = TryCreateDate(year, month, 1);
        if (date.HasValue) return date.Value;

        var calendar = _culture.Calendar;
        var (minYear, minMonth) = GetMinCalendarYearMonth();

        return year == minYear && month <= minMonth
            ? calendar.MinSupportedDateTime.Date
            : calendar.MaxSupportedDateTime.Date;
    }

    // DateTime is bounded to the years 1 through 9999 of the Gregorian calendar, and some calendars support
    // even less, so everything the navigation can reach is bounded by the calendar's own range the same way
    // MinDate and MaxDate bound it.
    private (int Year, int Month) GetMinCalendarYearMonth()
    {
        var calendar = _culture.Calendar;
        var minDate = calendar.MinSupportedDateTime;

        return (calendar.GetYear(minDate), calendar.GetMonth(minDate));
    }

    /// <inheritdoc cref="GetMinCalendarYearMonth"/>
    private (int Year, int Month) GetMaxCalendarYearMonth()
    {
        var calendar = _culture.Calendar;
        var maxDate = calendar.MaxSupportedDateTime;

        return (calendar.GetYear(maxDate), calendar.GetMonth(maxDate));
    }

    private int GetMonthsInCurrentYear()
    {
        // Not every calendar has twelve months: a leap year of the Hebrew calendar has thirteen.
        return _culture.Calendar.GetMonthsInYear(_currentYear);
    }

    private bool IsWeekRowEmpty(int weekIndex)
    {
        for (var day = 0; day < DEFAULT_DAY_COUNT_PER_WEEK; day++)
        {
            if (_daysOfCurrentMonth[weekIndex, day].HasValue) return false;
        }

        return true;
    }

    // Moving to another year of a calendar whose years do not all have the same number of months (the Hebrew
    // one) can leave the displayed month past the end of the year that is now displayed - and moving to the
    // first or the last supported year can leave it past the supported part of that year.
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

    // The single letter each weekday column is headed with, taken off the culture's shortest day name. A
    // culture is free to leave those empty, which would be an index out of range, and the first character of
    // one that is written as a surrogate pair is two of them - half of it is not a character at all.
    private static string GetNarrowDayName(string name)
    {
        if (name.HasNoValue()) return string.Empty;

        return char.IsHighSurrogate(name[0]) && name.Length > 1 ? name[..2] : name[..1];
    }

    private int GetWeekNumber(int weekIndex)
    {
        // The first cells of the week can be empty at the very edge of the calendar's supported range, so the
        // number of the week is read off the first day of it that actually exists.
        var date = _daysOfCurrentMonth[weekIndex, 0];
        for (var day = 1; date.HasValue is false && day < DEFAULT_DAY_COUNT_PER_WEEK; day++)
        {
            date = _daysOfCurrentMonth[weekIndex, day];
        }

        return _culture.Calendar.GetWeekOfYear(date!.Value, WeekNumberRule ?? CalendarWeekRule.FirstFullWeek, GetFirstDayOfWeek());
    }

    private void ToggleMonthPickerOverlay()
    {
        _showMonthPicker = !_showMonthPicker;

        // Each toggle swaps one whole picker for another, taking the button that was activated out of the DOM
        // with it, so the focus has to be handed over to the picker that takes its place.
        _focusedYearCell = null;
        _focusedMonthCell = null;

        MoveFocusToTheVisiblePicker();
    }

    private void ToggleTimePickerOverlay()
    {
        _showTimePicker = !_showTimePicker;

        MoveFocusToTheVisiblePicker();
    }

    // A picker laid over the day grid is a surface the keyboard was sent into, so Escape is what leaves it -
    // the same way it leaves every other dismissible surface in the library - and the day grid underneath
    // takes the focus back. A picker that sits beside the day grid is not covering anything and stays put.
    private void DismissMonthPickerOverlay()
    {
        if (ShowMonthPickerAsOverlay is false && (ShowTimePicker && ShowTimePickerAsOverlay is false) is false) return;

        ToggleMonthPickerOverlay();
    }

    /// <inheritdoc cref="DismissMonthPickerOverlay"/>
    private void HandleTimePickerKeyDown(KeyboardEventArgs e)
    {
        if (IsEnabled is false) return;
        if (e.Key is not "Escape") return;
        if (ShowTimePickerAsOverlay is false) return;

        ToggleTimePickerOverlay();
    }

    private void MoveFocusToTheVisiblePicker()
    {
        if (DayPickerIsVisible())
        {
            _focusedDate = GetFocusableDay();
            _focusElementIdAfterRender = GetDayButtonId(_focusedDate.Value);
        }
        else if (MonthPickerIsVisible() && _showYearPicker is false)
        {
            FocusMonthCell(GetFocusableMonth());
        }
        else if (MonthPickerIsVisible())
        {
            FocusYearCell(GetFocusableYear());
        }
        else if (_showTimePicker)
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
            var (maxCalendarYear, maxCalendarMonth) = GetMaxCalendarYearMonth();
            if (_currentYear == maxCalendarYear && _currentMonth >= maxCalendarMonth) return false;

            var max = GetMaxDate();
            if (max.HasValue)
            {
                var maxDate = GetDateTime(max.Value);
                var maxDateYear = _culture.Calendar.GetYear(maxDate);
                var maxDateMonth = _culture.Calendar.GetMonth(maxDate);

                if (maxDateYear == _currentYear && maxDateMonth == _currentMonth) return false;
            }
        }
        else
        {
            var (minCalendarYear, minCalendarMonth) = GetMinCalendarYearMonth();
            if (_currentYear == minCalendarYear && _currentMonth <= minCalendarMonth) return false;

            var min = GetMinDate();
            if (min.HasValue)
            {
                var minDate = GetDateTime(min.Value);
                var minDateYear = _culture.Calendar.GetYear(minDate);
                var minDateMonth = _culture.Calendar.GetMonth(minDate);

                if (minDateYear == _currentYear && minDateMonth == _currentMonth) return false;
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

    private DateTimeOffset GetNow()
    {
        return Today ?? DateTimeOffset.Now;
    }

    // DisablePast and DisableFuture bound the selectable days by today exactly the way MinDate and MaxDate
    // do, so every consumer of the allowed range reads the bounds through these two accessors. Where both
    // apply, the narrower of the two wins. Today itself stays selectable under either of them, since every
    // comparison below weighs a whole day rather than an instant.
    private DateTimeOffset? GetMinDate()
    {
        if (DisablePast is false) return MinDate;

        var now = GetNow();

        return MinDate.HasValue && MinDate.Value > now ? MinDate : now;
    }

    /// <inheritdoc cref="GetMinDate"/>
    private DateTimeOffset? GetMaxDate()
    {
        if (DisableFuture is false) return MaxDate;

        var now = GetNow();

        return MaxDate.HasValue && MaxDate.Value < now ? MaxDate : now;
    }

    // Every caller weighs a day of the calendar, so the comparison is day against day: a MinDate that carries
    // a time of day rules out the days before it, not the day it itself falls on.
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
        // The supported range of the calendar itself bounds the selection the same way MinDate and MaxDate do:
        // a month past its edge has no representable days at all.
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

        // The years outside of the calendar's own supported range are as unselectable as the ones outside of
        // MinDate and MaxDate - the year picker can show them at the edges of its ranges.
        return year < GetMinCalendarYearMonth().Year
            || year > GetMaxCalendarYearMonth().Year
            || (maxDate.HasValue && year > _culture.Calendar.GetYear(GetDateTime(maxDate.Value)))
            || (minDate.HasValue && year < _culture.Calendar.GetYear(GetDateTime(minDate.Value)));
    }

    private (string style, string klass) GetDayButtonCss(DateTime date)
    {
        StringBuilder klass = new StringBuilder();
        StringBuilder style = new StringBuilder();

        if (CurrentValue.HasValue && date == GetDateTime(CurrentValue.Value).Date)
        {
            klass.Append(" bit-cal-dbs");

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
            klass.Append(" bit-cal-dbo");
        }

        //Is highlighted
        if (_highlightedDates.Contains(date.Date))
        {
            klass.Append(" bit-cal-dhl");

            if (Classes?.HighlightedDayButton is not null)
            {
                klass.Append(' ').Append(Classes?.HighlightedDayButton);
            }

            AppendStyle(style, Styles?.HighlightedDayButton);
        }

        //Is today
        if (HighlightToday && month == _currentMonth && date == GetToday().Date)
        {
            klass.Append(" bit-cal-dtd");

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

        // The style of every day comes last so it wins over the state specific ones, and it goes through the
        // same appender so it is separated from them by a semicolon.
        AppendStyle(style, Styles?.DayButton);

        return (style.ToString(), klass.ToString());
    }

    // The styles of a day come from more than one state at a time (a selected day that is also today), so each
    // one is closed with a semicolon before the next is appended - without it the last declaration of one and
    // the first of the next would run together into a single invalid one.
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
            className.Append(" bit-cal-pcm");
        }

        if (HighlightSelectedMonth && _currentMonth == monthIndex)
        {
            className.Append(" bit-cal-psm");
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

    private bool IsSelectedDate(DateTime date)
    {
        if (CurrentValue is null) return false;

        return date == GetDateTime(CurrentValue.Value).Date;
    }

    private void UpdateTime()
    {
        if (CurrentValue.HasValue is false) return;

        var currentValue = GetDateTime(CurrentValue.Value);
        var currentValueYear = _culture.Calendar.GetYear(currentValue);
        var currentValueMonth = _culture.Calendar.GetMonth(currentValue);
        var currentValueDay = _culture.Calendar.GetDayOfMonth(currentValue);

        var date = _culture.Calendar.ToDateTime(currentValueYear, currentValueMonth, currentValueDay, _hour, _minute, 0, 0);
        CurrentValue = new(date, _timeZone.GetUtcOffset(date));
    }

    private void BuildEventsLookup()
    {
        // The events of a day read as its agenda, so they are ordered the way one is: the all-day ones (which
        // carry no start time) first, then the rest by the time they start at.
        _eventsByDate = Events is null
            ? []
            : Events.GroupBy(e => e.Date)
                    .ToDictionary(g => g.Key, g => g.OrderBy(e => e.StartTime ?? TimeOnly.MinValue).ToList());
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

    private DateTime GetToday()
    {
        return GetDateTime(Today ?? DateTimeOffset.Now);
    }

    private bool IsInCurrentMonth(DateTime date)
    {
        return _culture.Calendar.GetYear(date) == _currentYear && _culture.Calendar.GetMonth(date) == _currentMonth;
    }

    private string GetDayButtonId(DateTime date)
    {
        return FormattableString.Invariant($"{_Id}-day-{date.Year:D4}-{date.Month:D2}-{date.Day:D2}");
    }

    private string GetMonthButtonId(int month)
    {
        return FormattableString.Invariant($"{_Id}-month-{month:D2}");
    }

    private string GetYearButtonId(int year)
    {
        return FormattableString.Invariant($"{_Id}-year-{year:D4}");
    }

    // The single day of the grid that is in the tab sequence (the roving tabindex of the APG grid pattern):
    // the one the keyboard last landed on, otherwise the selection, otherwise today, and as a last resort the
    // first day that can actually be selected - the grid must never be unreachable.
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
        // tabindex still has to land on a day of the month itself: with ShowOutsideDays turned off the days
        // around it are not rendered as buttons at all, so pointing at one loses the grid entirely.
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

    // The single month of the month grid that is in the tab sequence: the one the keyboard last landed on,
    // otherwise the month the calendar displays, and as a last resort the first month the Min/Max range
    // allows - a month is always returned, so the grid is reachable even when every month is disabled.
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

    // The same roving tabindex for the year grid. The displayed year is not always inside the range the year
    // picker shows (browsing the ranges moves the range alone), so the first year of the range is what the tab
    // sequence falls back to.
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
            DismissMonthPickerOverlay();
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

        var isRtl = BitCssClasses.IsRtl(Dir, _culture);

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

    // The year grid answers the same keys, one row being four years wide, and PageUp/PageDown moving to the
    // adjacent range of years.
    private void HandleYearKeyDown(KeyboardEventArgs e, int year)
    {
        if (IsEnabled is false) return;

        // Escape leaves the year grid the way it was reached: back to the months of the year it is showing,
        // and from there - a second Escape - out of the overlay entirely.
        if (e.Key is "Escape")
        {
            ToggleBetweenMonthAndYearPicker();
            FocusMonthCell(GetFocusableMonth());
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

        var isRtl = BitCssClasses.IsRtl(Dir, _culture);

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

    private async Task HandleDayKeyDown(KeyboardEventArgs e, DateTime date)
    {
        if (IsEnabled is false) return;

        var isRtl = BitCssClasses.IsRtl(Dir, _culture);

        DateTime? target = e.Key switch
        {
            "ArrowLeft" => FindEnabledDay(date, isRtl ? 1 : -1),
            "ArrowRight" => FindEnabledDay(date, isRtl ? -1 : 1),
            "ArrowUp" => FindEnabledDay(date, -7),
            "ArrowDown" => FindEnabledDay(date, 7),
            "Home" => FindEnabledDayTowards(GetStartOfWeek(date), date),
            "End" => FindEnabledDayTowards(GetEndOfWeek(date), date),
            "PageUp" => FindEnabledDayTowards(e.ShiftKey ? TryAddYears(date, -1) : TryAddMonths(date, -1), date),
            "PageDown" => FindEnabledDayTowards(e.ShiftKey ? TryAddYears(date, 1) : TryAddMonths(date, 1), date),
            _ => null
        };

        if (target.HasValue is false) return;

        await MoveFocusToDay(target.Value);
    }

    // A step that runs off the end of what a DateTime, or of what the culture's own calendar, can represent
    // has nowhere to land - so it is refused rather than thrown, and the key simply moves the focus nowhere.
    // The days at the very edge of the range are reachable all the same: every step that does land is taken.
    private static DateTime? TryAddDays(DateTime date, int days)
    {
        try
        {
            return date.AddDays(days);
        }
        catch (ArgumentOutOfRangeException)
        {
            return null;
        }
    }

    /// <inheritdoc cref="TryAddDays"/>
    private DateTime? TryAddMonths(DateTime date, int months)
    {
        try
        {
            return _culture.Calendar.AddMonths(date, months);
        }
        catch (ArgumentException)
        {
            return null;
        }
    }

    /// <inheritdoc cref="TryAddDays"/>
    private DateTime? TryAddYears(DateTime date, int years)
    {
        try
        {
            return _culture.Calendar.AddYears(date, years);
        }
        catch (ArgumentException)
        {
            return null;
        }
    }

    private DateTime? FindEnabledDay(DateTime from, int stepDays)
    {
        var date = from;

        for (var i = 0; i < 366; i++)
        {
            var next = TryAddDays(date, stepDays);
            if (next.HasValue is false) return null;

            date = next.Value;

            if (IsWeekDayOutOfMinAndMaxDate(date)) return null;

            if (IsDayDisabled(date) is false) return date;
        }

        return null;
    }

    private DateTime? FindEnabledDayTowards(DateTime? target, DateTime origin)
    {
        if (target.HasValue is false) return null;

        var step = target.Value < origin ? 1 : -1;
        var date = target.Value;

        // Both ends are days the calendar can represent, so every day walked between them is one too.
        while (date != origin)
        {
            if (IsDayDisabled(date) is false) return date;

            date = date.AddDays(step);
        }

        return null;
    }

    private DateTime? GetStartOfWeek(DateTime date)
    {
        var diff = ((int)date.DayOfWeek - (int)GetFirstDayOfWeek() + 7) % 7;

        return TryAddDays(date, -diff);
    }

    /// <inheritdoc cref="GetStartOfWeek"/>
    private DateTime? GetEndOfWeek(DateTime date)
    {
        var start = GetStartOfWeek(date);

        return start.HasValue ? TryAddDays(start.Value, 6) : null;
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

    private async Task NotifyMonthChange(int previousYear, int previousMonth)
    {
        if (previousYear == _currentYear && previousMonth == _currentMonth) return;
        if (OnMonthChange.HasDelegate is false) return;

        var date = GetFirstDayOfMonthOrClamp(_currentYear, _currentMonth);

        await OnMonthChange.InvokeAsync(new(date, _timeZone.GetUtcOffset(date)));
    }

    // How many dots a day can wear before the row of them stops saying anything a shorter row does not.
    private const int MAX_EVENT_INDICATORS = 3;

    private static IEnumerable<BitCalendarEvent> GetIndicatorEvents(IReadOnlyList<BitCalendarEvent> events)
    {
        return events.Take(MAX_EVENT_INDICATORS);
    }

    // An event of its own color paints its dot with it; one without takes whatever the calendar's event color
    // is, which is the Color role unless --bit-Calendar-event-color says otherwise.
    private static string? GetEventIndicatorColorClass(BitCalendarEvent evt)
    {
        return evt.Color.HasValue ? BitCssClasses.Color(evt.Color, "bit-cal-evi") : null;
    }

    private IReadOnlyList<BitCalendarEvent> GetDayEvents(DateTime date)
    {
        var dateOnly = DateOnly.FromDateTime(date);

        return _eventsByDate.TryGetValue(dateOnly, out var list) ? list : Array.Empty<BitCalendarEvent>();
    }

    // The time of an event is written the way its culture writes a time of day - with its separators, its
    // order and its designators (see BitTimePatterns) - rather than with a pattern hardcoded here, which
    // would spell the same time differently from every picker on the same page. The parts are padded, so
    // the times of a list of events line up under one another however wide each of them is.
    private string FormatEventTime(TimeOnly time)
    {
        return time.ToString(BitTimePatterns.GetTimePattern(_culture, TimeFormat, withSeconds: false, padded: true), _culture);
    }

    private string FormatEventModalDate(DateOnly date)
    {
        return date.ToDateTime(TimeOnly.MinValue).ToString(DateFormat ?? _culture.DateTimeFormat.ShortDatePattern, _culture);
    }

    // The events of a day as one line each, for the native tooltip of its cell.
    private string GetEventTooltip(IReadOnlyList<BitCalendarEvent> events)
    {
        return GetEventSummary(events, "\n");
    }

    private async Task HandleDayClick(DateTime date, IReadOnlyList<BitCalendarEvent> events)
    {
        if (ShowEventDetails && events.Count > 0)
        {
            OpenEventModal(date);
        }

        await SelectDate(date);
    }

    private void OpenEventModal(DateTime date)
    {
        _eventModalDate = DateOnly.FromDateTime(date);
        _showEventModal = true;
    }

    // Only the day is held, never the events of it: the events the dialog lists are read from the lookup on
    // every render, so a page that loads them for the visible month while the dialog is open shows what it
    // loaded rather than what the day held when it was opened.
    private IReadOnlyList<BitCalendarEvent> GetEventModalEvents()
    {
        return _eventsByDate.TryGetValue(_eventModalDate, out var list) ? list : [];
    }

    private void CloseEventModal()
    {
        _showEventModal = false;
    }

    // Escape closes the dialog, the way every dismissible surface in the library is left, and hands the
    // keyboard back to the day that opened it (SyncEventDialogFocus).
    private void HandleEventDialogKeyDown(KeyboardEventArgs e)
    {
        if (e.Key is not "Escape") return;

        CloseEventModal();
    }

    // The dialog takes the keyboard over while it is open: the focus is recorded and moved into it, Tab is kept
    // cycling inside it, and closing it puts the focus back on the day cell it was opened from. Driven from
    // OnAfterRenderAsync rather than from the open and close handlers, since the surface has to be in the DOM
    // before the focus can be placed in it and gone from it before the focus can be handed back.
    private async Task SyncEventDialogFocus()
    {
        if (IsDisposed) return;

        if (_showEventModal)
        {
            if (_eventModalFocusTrapped) return;

            _eventModalFocusTrapped = true;
            _eventModalFocusStored = true;

            try
            {
                await _js.BitUtilsStoreFocus(_eventDialogId);
                await _js.BitUtilsSetupFocusTrap(_eventDialogId);
                await _js.BitUtilsFocusFirstElement(_eventDialogId);
            }
            catch (JSDisconnectedException) { } // we can ignore this exception here

            return;
        }

        if (_eventModalFocusTrapped is false) return;

        _eventModalFocusTrapped = false;

        try
        {
            await _js.BitUtilsDisposeFocusTrap(_eventDialogId);

            if (_eventModalFocusStored)
            {
                _eventModalFocusStored = false;

                await _js.BitUtilsRestoreFocus(_eventDialogId);
            }
        }
        catch (JSDisconnectedException) { } // we can ignore this exception here
    }

    // What a screen reader is told a day is: the date, and the events it carries - the indicator dot and the
    // tooltip that report them to everyone else are both invisible to it. The label replaces the content of the
    // button, so the events cannot simply be added to the markup inside it.
    private string GetDayAriaLabel(DateTime date, IReadOnlyList<BitCalendarEvent> events)
    {
        var label = date.ToString(_culture.DateTimeFormat.LongDatePattern, _culture);

        if (events.Count == 0) return label;

        return $"{label}, {GetEventSummary(events, ", ")}";
    }

    private string GetEventSummary(IReadOnlyList<BitCalendarEvent> events, string separator)
    {
        return string.Join(separator, events.Select(e =>
            e.StartTime.HasValue ? $"{e.Title} ({FormatEventTime(e.StartTime.Value)})" : e.Title));
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

    private void ToggleAmPmTime()
    {
        if (ReadOnly) return;
        if (IsEnabled is false) return;

        _hourView = _hour + (_hour >= 12 ? -12 : 12);
    }

    private void HandleOnAmClick()
    {
        if (ReadOnly) return;
        if (IsEnabled is false) return;

        _hour %= 12;  // "12:-- am" is "00:--" in 24h
        UpdateTime();
    }

    private void HandleOnPmClick()
    {
        if (ReadOnly) return;
        if (IsEnabled is false) return;

        if (_hour <= 12) // "12:-- pm" is "12:--" in 24h
        {
            _hour += 12;
        }

        _hour %= 24;
        UpdateTime();
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

        ChangeTime(isNext, isHour);

        if (IsDisposed) return;

        ResetCts();

        // The press-and-hold spin is deliberately not awaited: it lives as long as the button is held, so
        // awaiting it would leave the pointerdown event handler (and the render it drives) pending for the
        // whole duration of the press. Its lifetime is owned by the cancellation token source instead, which
        // HandleOnPointerUpOrOut and DisposeAsync cancel.
        _ = ContinuousChangeTimeAfterDelay(isNext, isHour, _cancellationTokenSource);
    }

    /// <summary>
    /// Waits out the <see cref="ContinuousSpinDelay"/> and then starts the continuous spin, unless the
    /// button was released (or the component went away) in the meantime.
    /// </summary>
    private async Task ContinuousChangeTimeAfterDelay(bool isNext, bool isHour, CancellationTokenSource cts)
    {
        try
        {
            await Task.Delay(Math.Max(1, ContinuousSpinDelay), cts.Token);

            await InvokeAsync(() => ContinuousChangeTime(isNext, isHour, cts));
        }
        catch (OperationCanceledException) { } // the button was released before the continuous spin started
        catch (ObjectDisposedException) { } // the component was disposed while the delay was pending
    }

    // A loop rather than a call that ends in another one of itself: a button held for a few seconds is
    // hundreds of ticks, and every one of them would otherwise leave a frame of its own alive until the whole
    // chain unwinds at the end of the press.
    private async Task ContinuousChangeTime(bool isNext, bool isHour, CancellationTokenSource cts)
    {
        while (cts.IsCancellationRequested is false && IsDisposed is false)
        {
            var partBeforeStep = isHour ? _hour : _minute;

            ChangeTime(isNext, isHour);

            if (cts.IsCancellationRequested || IsDisposed) return;

            // A tick that moved nothing will not move anything on the next one either - a step of a whole
            // range leaves a single value on the grid - so the held button has run out of room. Without this
            // it would spend the rest of the press re-rendering a value that never changes again.
            if ((isHour ? _hour : _minute) == partBeforeStep) return;

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

    private void ChangeTime(bool isNext, bool isHour)
    {
        if (isHour)
        {
            ChangeHour(isNext);
        }
        else
        {
            ChangeMinute(isNext);
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
    private void ChangeHour(bool isNext)
    {
        _hour = BitTimeSteps.StepToAllowed(_hour, isNext, 24, IsHourOnGrid) ?? _hour;

        UpdateTime();
    }

    /// <inheritdoc cref="ChangeHour"/>
    private void ChangeMinute(bool isNext)
    {
        _minute = BitTimeSteps.StepToAllowed(_minute, isNext, 60, IsMinuteOnGrid) ?? _minute;

        UpdateTime();
    }

    // The grid HourStep and MinuteStep lay over the day and over the hour, which everything that moves the
    // time - the spin buttons, the keys, what is typed - is held to, so no two of them can disagree about
    // which times the picker offers.
    private bool IsHourOnGrid(int hour) => BitTimeSteps.IsOnGrid(hour, HourStep, 0, 24);

    /// <inheritdoc cref="IsHourOnGrid"/>
    private bool IsMinuteOnGrid(int minute) => BitTimeSteps.IsOnGrid(minute, MinuteStep, 0, 60);

    // The hour and the minute answer PageUp and PageDown with the same step the spin buttons next to them
    // move by, so the time can be set without leaving the keyboard or the field. (The arrow keys are the
    // number input's own, and the setter above snaps the one-step move they make onto the grid.)
    private void HandleOnTimeInputKeyDown(KeyboardEventArgs e, bool isHour)
    {
        if (IsEnabled is false || ReadOnly) return;

        if (e.Key is not ("PageUp" or "PageDown")) return;

        ChangeTime(e.Key is "PageUp", isHour);
    }

    public bool DayPickerIsVisible()
    {
        if (_showMonthPicker is false && _showTimePicker is false) return true;

        if (_showMonthPicker is false)
        {
            if (_showTimePicker)
            {
                return ShowTimePickerAsOverlay is false;
            }
        }
        else
        {
            if (_showTimePicker)
            {
                return ShowMonthPickerAsOverlay is false && ShowTimePickerAsOverlay;
            }
            else
            {
                return ShowMonthPickerAsOverlay is false;
            }
        }

        return false;
    }

    private bool MonthPickerIsVisible()
    {
        if (_showMonthPicker is false) return false;

        return (ShowMonthPickerAsOverlay is false && ShowTimePickerAsOverlay is false) ||
               (_showTimePicker && ShowMonthPickerAsOverlay && ShowTimePickerAsOverlay is false) ||
               (_showTimePicker is false && (ShowMonthPickerAsOverlay is false && ShowTimePickerAsOverlay is false) is false);

    }



    protected override async ValueTask DisposeAsync(bool disposing)
    {
        if (IsDisposed || disposing is false) return;

        _cancellationTokenSource?.Cancel();
        _cancellationTokenSource?.Dispose();
        OnValueChanged -= HandleOnValueChanged;

        try
        {
            await _js.BitCalendarsDispose(_Id);

            if (_eventModalFocusTrapped)
            {
                _eventModalFocusTrapped = false;

                await _js.BitUtilsDisposeFocusTrap(_eventDialogId);
            }

            if (_eventModalFocusStored)
            {
                _eventModalFocusStored = false;

                await _js.BitUtilsForgetFocus(_eventDialogId);
            }
        }
        catch (JSDisconnectedException) { } // we can ignore this exception here

        await base.DisposeAsync(disposing);
    }
}
