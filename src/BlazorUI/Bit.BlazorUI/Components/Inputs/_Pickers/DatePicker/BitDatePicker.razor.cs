using System.Text;
using System.Globalization;
using System.Diagnostics.CodeAnalysis;

namespace Bit.BlazorUI;

/// <summary>
/// A BitDatePicker offers a drop-down control that’s optimized for picking a single date from a calendar view where contextual information like the day of the week or fullness of the calendar is important.
/// </summary>
public partial class BitDatePicker : BitInputBase<DateTimeOffset?>, IAsyncDisposable
{
    private const int MAX_WIDTH = 470;
    private const int DEFAULT_WEEK_COUNT = 6;
    private const int DEFAULT_DAY_COUNT_PER_WEEK = 7;



    private bool _disposed;
    private bool _hasFocus;
    private int _currentDay;
    private int _currentYear;
    private int _currentMonth;
    private int? _selectedDateWeek;
    private int _yearPickerEndYear;
    private int _yearPickerStartYear;
    private int? _selectedDateDayOfWeek;
    private bool _showMonthPicker = true;
    private bool _isTimePickerOverlayOnTop;
    private bool _isMonthPickerOverlayOnTop;
    private string _monthTitle = string.Empty;
    private bool _showTimePickerAsOverlayInternal;
    private bool _showMonthPickerAsOverlayInternal;
    private CultureInfo _culture = CultureInfo.CurrentUICulture;
    private CancellationTokenSource _cancellationTokenSource = new();
    private DotNetObjectReference<BitDatePicker> _dotnetObj = default!;
    private readonly int[,] _daysOfCurrentMonth = new int[DEFAULT_WEEK_COUNT, DEFAULT_DAY_COUNT_PER_WEEK];

    private string? _labelId;
    private string? _inputId;
    private string _calloutId = string.Empty;
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
            if (value > 23)
            {
                _hour = 23;
            }
            else if (value < 0)
            {
                _hour = 0;
            }
            else
            {
                _hour = value;
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
    /// The title of the GoToToday button (tooltip).
    /// </summary>
    [Parameter] public string GoToTodayTitle { get; set; } = "Go to today";

    /// <summary>
    /// The title of the GoToNow button (tooltip).
    /// </summary>
    [Parameter] public string GoToNowTitle { get; set; } = "Go to now";

    /// <summary>
    /// The title of the ShowTimePicker button (tooltip).
    /// </summary>
    [Parameter] public string ShowTimePickerTitle { get; set; } = "Show time picker";

    /// <summary>
    /// The title of the HideTimePicker button (tooltip).
    /// </summary>
    [Parameter] public string HideTimePickerTitle { get; set; } = "Hide time picker";

    /// <summary>
    /// The title of the CloseDatePicker button (tooltip).
    /// </summary>
    [Parameter] public string CloseDatePickerTitle { get; set; } = "Close date picker";

    /// <summary>
    /// Determines if the DatePicker has a border.
    /// </summary>
    [Parameter, ResetClassBuilder]
    public bool HasBorder { get; set; } = true;

    /// <summary>
    /// Whether the month picker should highlight the current month.
    /// </summary>
    [Parameter] public bool HighlightCurrentMonth { get; set; }

    /// <summary>
    /// Whether the month picker should highlight the selected month.
    /// </summary>
    [Parameter] public bool HighlightSelectedMonth { get; set; }

    /// <summary>
    /// Custom template for the DatePicker's icon.
    /// </summary>
    [Parameter] public RenderFragment? IconTemplate { get; set; }

    /// <summary>
    /// Determines the location of the DatePicker's icon.
    /// </summary>
    [Parameter, ResetClassBuilder]
    public BitIconLocation IconLocation { get; set; } = BitIconLocation.Right;

    /// <summary>
    /// The name of the DatePicker's icon.
    /// </summary>
    [Parameter] public string IconName { get; set; } = "CalendarMirrored";

    /// <summary>
    /// The custom validation error message for the invalid value.
    /// </summary>
    [Parameter] public string? InvalidErrorMessage { get; set; }

    /// <summary>
    /// Whether the month picker is shown or hidden.
    /// </summary>
    [Parameter] public bool IsMonthPickerVisible { get; set; } = true;

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
    /// Custom template to render the month cells of the DatePicker.
    /// </summary>
    [Parameter] public RenderFragment<DateTimeOffset>? MonthCellTemplate { get; set; }

    /// <summary>
    /// The title of the month picker's toggle (tooltip).
    /// </summary>
    [Parameter] public string MonthPickerToggleTitle { get; set; } = "{0}, change month";

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
    /// The text of selected date aria-atomic of the DatePicker.
    /// </summary>
    [Parameter] public string SelectedDateAriaAtomic { get; set; } = "Selected date {0}";

    /// <summary>
    /// The placeholder text of the DatePicker's input.
    /// </summary>
    [Parameter] public string Placeholder { get; set; } = string.Empty;

    /// <summary>
    /// Enables the responsive mode in small screens.
    /// </summary>
    [Parameter] public bool Responsive { get; set; }

    /// <summary>
    /// Whether the DatePicker's close button should be shown or not.
    /// </summary>
    [Parameter] public bool ShowCloseButton { get; set; }

    /// <summary>
    /// Whether the GoToToday button should be shown or not.
    /// </summary>
    [Parameter] public bool ShowGoToToday { get; set; } = true;

    /// <summary>
    /// Whether the GoToNow button should be shown or not.
    /// </summary>
    [Parameter] public bool ShowGoToNow { get; set; } = true;

    /// <summary>
    /// Show month picker on top of date picker when visible.
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
    /// Whether the week number (weeks 1 to 53) should be shown before each week row.
    /// </summary>
    [Parameter] public bool ShowWeekNumbers { get; set; }

    /// <summary>
    /// Custom CSS styles for different parts of the BitDatePicker component.
    /// </summary>
    [Parameter] public BitDatePickerClassStyles? Styles { get; set; }

    /// <summary>
    /// The tabIndex of the DatePicker's input.
    /// </summary>
    [Parameter] public int TabIndex { get; set; }

    /// <summary>
    /// The time format of the time-picker, 24H or 12H.
    /// </summary>
    [Parameter] public BitTimeFormat TimeFormat { get; set; }

    /// <summary>
    /// Whether or not the text field of the DatePicker is underlined.
    /// </summary>
    [Parameter, ResetClassBuilder]
    public bool Underlined { get; set; }

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

    /// <summary>
    /// Show month picker on top of date picker when visible.
    /// </summary>
    [Parameter]
    [CallOnSet(nameof(OnSetParameters))]
    public bool ShowTimePickerAsOverlay { get; set; }

    /// <summary>
    /// Whether the clear button should be shown or not when the BitDatePicker has a value.
    /// </summary>
    [Parameter] public bool ShowClearButton { get; set; }

    /// <summary>
    /// Determines increment/decrement steps for date-picker's hour.
    /// </summary>
    [Parameter] public int HourStep { get; set; } = 1;

    /// <summary>
    /// Determines increment/decrement steps for date-picker's minute.
    /// </summary>
    [Parameter] public int MinuteStep { get; set; } = 1;

    /// <summary>
    /// Specifies the date and time of the date-picker when it is opened without any selected value.
    /// </summary>
    [Parameter]
    [CallOnSet(nameof(OnSetParameters))]
    public DateTimeOffset? StartingValue { get; set; }

    /// <summary>
    /// Whether the date-picker is rendered standalone or with the input component and callout.
    /// </summary>
    [Parameter, ResetClassBuilder]
    [CallOnSet(nameof(OnSetParameters))]
    public bool Standalone { get; set; }



    [JSInvokable("CloseCallout")]
    public async Task _CloseCalloutBeforeAnotherCalloutIsOpened()
    {
        if (Standalone) return;
        if (IsEnabled is false) return;

        if (await AssignIsOpen(false) is false) return;

        StateHasChanged();
    }

    [JSInvokable("OnStart")]
    public async Task _OnStart(decimal startX, decimal startY)
    {

    }

    [JSInvokable("OnMove")]
    public async Task _OnMove(decimal diffX, decimal diffY)
    {

    }

    [JSInvokable("OnEnd")]
    public async Task _OnEnd(decimal diffX, decimal diffY)
    {

    }

    [JSInvokable("OnClose")]
    public async Task _OnClose()
    {
        await CloseCallout();
        await InvokeAsync(StateHasChanged);
    }



    public Task OpenCallout()
    {
        return HandleOnClick();
    }



    protected override string RootElementClass { get; } = "bit-dtp";

    protected override void RegisterCssClasses()
    {
        ClassBuilder.Register(() => Classes?.Root);

        ClassBuilder.Register(() => (Dir is null && _culture.TextInfo.IsRightToLeft) ? "bit-rtl" : string.Empty);

        ClassBuilder.Register(() => IconLocation is BitIconLocation.Left ? "bit-dtp-lic" : string.Empty);

        ClassBuilder.Register(() => Underlined ? "bit-dtp-und" : string.Empty);

        ClassBuilder.Register(() => HasBorder is false ? "bit-dtp-nbd" : string.Empty);

        ClassBuilder.Register(() => Standalone ? "bit-dtp-sta" : string.Empty);

        ClassBuilder.Register(() => _hasFocus ? $"bit-dtp-foc {Classes?.Focused}" : string.Empty);
    }

    protected override void RegisterCssStyles()
    {
        StyleBuilder.Register(() => Styles?.Root);

        StyleBuilder.Register(() => _hasFocus ? Styles?.Focused : string.Empty);
    }

    protected override void OnInitialized()
    {
        _dotnetObj = DotNetObjectReference.Create(this);

        _datePickerId = $"DatePicker-{UniqueId}";
        _labelId = $"{_datePickerId}-label";
        _calloutId = $"{_datePickerId}-callout";
        _inputId = $"{_datePickerId}-input";

        OnValueChanged += HandleOnValueChanged;

        OnSetParameters();

        base.OnInitialized();
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        await base.OnAfterRenderAsync(firstRender);

        if (firstRender is false) return;
        if (Responsive is false) return;

        await _js.BitSwipesSetup(_calloutId, 0.25m, BitPanelPosition.Top, Dir is BitDir.Rtl, BitSwipeOrientation.Vertical, _dotnetObj);
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
            result = new DateTimeOffset(parsedValue, DateTimeOffset.Now.Offset);
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



    private async Task HandleOnClick()
    {
        if (Standalone) return;
        if (IsEnabled is false) return;

        if (await AssignIsOpen(true) is false) return;

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

    private async Task HandleOnChange(ChangeEventArgs e)
    {
        if (IsEnabled is false || InvalidValueBinding()) return;
        if (AllowTextInput is false) return;

        var oldValue = CurrentValue.GetValueOrDefault(DateTimeOffset.Now);

        CurrentValueAsString = e.Value?.ToString();

        var curValue = CurrentValue.GetValueOrDefault(DateTimeOffset.Now);

        if (IsOpen && oldValue != curValue)
        {
            CheckCurrentCalendarMatchesCurrentValue();
            if (curValue.Year != oldValue.Year)
            {
                _currentYear = curValue.Year;
                ChangeYearRanges(_currentYear - 1);
            }
        }
    }

    private async Task HandleOnClearButtonClick()
    {
        if (IsEnabled is false) return;

        CurrentValue = null;

        _hour = 0;
        _minute = 0;

        _selectedDateWeek = null;
        _selectedDateDayOfWeek = null;

        await InputElement.FocusAsync();
    }

    private void HandleOnValueChanged(object? sender, EventArgs args)
    {
        OnSetParameters();
    }

    private void OnSetParameters()
    {
        _culture = Culture ?? CultureInfo.CurrentUICulture;

        var dateTime = CurrentValue.GetValueOrDefault(StartingValue.GetValueOrDefault(DateTimeOffset.Now));

        if (MinDate.HasValue && MinDate > dateTime)
        {
            dateTime = MinDate.Value;
        }

        if (MaxDate.HasValue && MaxDate < dateTime)
        {
            dateTime = MaxDate.Value;
        }

        _hour = CurrentValue.HasValue || StartingValue.HasValue ? dateTime.Hour : 0;
        _minute = CurrentValue.HasValue || StartingValue.HasValue ? dateTime.Minute : 0;

        GenerateCalendarData(dateTime.DateTime);

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

    private async Task SelectDate(int dayIndex, int weekIndex)
    {
        if (IsEnabled is false || InvalidValueBinding()) return;
        if (IsOpenHasBeenSet && IsOpenChanged.HasDelegate is false) return;
        if (IsWeekDayOutOfMinAndMaxDate(dayIndex, weekIndex)) return;

        _currentDay = _daysOfCurrentMonth[weekIndex, dayIndex];
        int selectedMonth = FindMonth(weekIndex, dayIndex);
        var isNotInCurrentMonth = IsInCurrentMonth(weekIndex, dayIndex) is false;

        //The number of days displayed in the picker is about 34 days, and if the selected day is less than 15, it means that the next month has been selected in next year.
        if (selectedMonth < _currentMonth && _currentMonth == 12 && isNotInCurrentMonth && _currentDay < 15)
        {
            _currentYear++;
        }

        //The number of days displayed in the picker is about 34 days, and if the selected day is greater than 15, it means that the previous month has been selected in previous year.
        if (selectedMonth > _currentMonth && _currentMonth == 1 && isNotInCurrentMonth && _currentDay > 15)
        {
            _currentYear--;
        }

        if (AutoClose && Standalone is false)
        {
            await AssignIsOpen(false);

            await ToggleCallout();
        }

        _currentMonth = selectedMonth;

        var currentDateTime = _culture.Calendar.ToDateTime(_currentYear, _currentMonth, _currentDay, _hour, _minute, 0, 0);
        CurrentValue = new DateTimeOffset(currentDateTime, DateTimeOffset.Now.Offset);

        GenerateMonthData(_currentYear, _currentMonth);
    }

    private void SelectMonth(int month)
    {
        if (IsEnabled is false) return;
        if (IsMonthOutOfMinAndMaxDate(month)) return;

        _currentMonth = month;

        GenerateMonthData(_currentYear, _currentMonth);

        if (_showMonthPickerAsOverlayInternal || ShowTimePicker)
        {
            ToggleMonthPickerOverlay();
        }
    }

    private void SelectYear(int year)
    {
        if (IsEnabled is false) return;
        if (IsYearOutOfMinAndMaxDate(year)) return;

        _currentYear = year;

        ChangeYearRanges(_currentYear - 1);

        GenerateMonthData(_currentYear, _currentMonth);

        ToggleBetweenMonthAndYearPicker();
    }

    private void ToggleBetweenMonthAndYearPicker()
    {
        if (IsEnabled is false) return;

        _showMonthPicker = !_showMonthPicker;
    }

    private void HandleMonthChange(bool isNext)
    {
        if (IsEnabled is false) return;
        if (CanChangeMonth(isNext) is false) return;

        if (isNext)
        {
            if (_currentMonth < 12)
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
                _currentMonth = 12;
            }
        }

        GenerateMonthData(_currentYear, _currentMonth);
    }

    private void HandleYearChange(bool isNext)
    {
        if (IsEnabled is false) return;
        if (CanChangeYear(isNext) is false) return;

        _currentYear += isNext ? +1 : -1;

        GenerateMonthData(_currentYear, _currentMonth);
    }

    private void HandleYearRangeChange(bool isNext)
    {
        if (IsEnabled is false) return;
        if (CanChangeYearRange(isNext) is false) return;

        var fromYear = _yearPickerStartYear + (isNext ? +12 : -12);

        ChangeYearRanges(fromYear);
    }

    private void HandleGoToToday()
    {
        if (IsEnabled is false) return;

        GenerateCalendarData(DateTime.Now);
    }

    private async Task HandleGoToNow()
    {
        if (IsEnabled is false) return;

        _hour = DateTime.Now.Hour;
        _minute = DateTime.Now.Minute;

        await UpdateCurrentValue();
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
        _selectedDateWeek = null;
        _selectedDateDayOfWeek = null;
        _monthTitle = $"{_culture.DateTimeFormat.GetMonthName(month)} {year}";

        for (int weekIndex = 0; weekIndex < DEFAULT_WEEK_COUNT; weekIndex++)
        {
            for (int dayIndex = 0; dayIndex < DEFAULT_DAY_COUNT_PER_WEEK; dayIndex++)
            {
                _daysOfCurrentMonth[weekIndex, dayIndex] = 0;
            }
        }

        var monthDays = _culture.Calendar.GetDaysInMonth(year, month);
        var firstDayOfMonth = _culture.Calendar.ToDateTime(year, month, 1, 0, 0, 0, 0);
        var startWeekDay = (int)_culture.DateTimeFormat.FirstDayOfWeek;
        var weekDayOfFirstDay = (int)firstDayOfMonth.DayOfWeek;
        var correctedWeekDayOfFirstDay = weekDayOfFirstDay > startWeekDay ? startWeekDay : startWeekDay - 7;

        var currentDay = 1;
        var isCurrentMonthEnded = false;
        for (int weekIndex = 0; weekIndex < DEFAULT_WEEK_COUNT; weekIndex++)
        {
            for (int dayIndex = 0; dayIndex < DEFAULT_DAY_COUNT_PER_WEEK; dayIndex++)
            {
                if (weekIndex == 0 && currentDay == 1 && weekDayOfFirstDay > dayIndex + correctedWeekDayOfFirstDay)
                {
                    int prevMonth;
                    int prevMonthDays;
                    if (month > 1)
                    {
                        prevMonth = month - 1;
                        prevMonthDays = _culture.Calendar.GetDaysInMonth(year, prevMonth);
                    }
                    else
                    {
                        prevMonth = 12;
                        prevMonthDays = _culture.Calendar.GetDaysInMonth(year - 1, prevMonth);
                    }

                    if (weekDayOfFirstDay > startWeekDay)
                    {
                        _daysOfCurrentMonth[weekIndex, dayIndex] = prevMonthDays + dayIndex - (weekDayOfFirstDay - startWeekDay - 1);
                    }
                    else
                    {
                        _daysOfCurrentMonth[weekIndex, dayIndex] = prevMonthDays + dayIndex - (7 + weekDayOfFirstDay - startWeekDay - 1);
                    }
                }
                else if (currentDay <= monthDays)
                {
                    _daysOfCurrentMonth[weekIndex, dayIndex] = currentDay;
                    currentDay++;
                }

                if (currentDay > monthDays)
                {
                    currentDay = 1;
                    isCurrentMonthEnded = true;
                }
            }

            if (isCurrentMonthEnded)
            {
                break;
            }
        }

        SetSelectedDateWeek();
    }

    private void SetSelectedDateWeek()
    {
        if (CurrentValue.HasValue is false || (_selectedDateWeek.HasValue && _selectedDateDayOfWeek.HasValue)) return;

        var year = _culture.Calendar.GetYear(CurrentValue.Value.DateTime);
        var month = _culture.Calendar.GetMonth(CurrentValue.Value.DateTime);

        if (year == _currentYear && month == _currentMonth)
        {
            var dayOfMonth = _culture.Calendar.GetDayOfMonth(CurrentValue.Value.DateTime);
            var startWeekDay = (int)_culture.DateTimeFormat.FirstDayOfWeek;
            var weekDayOfFirstDay = (int)_culture.Calendar.ToDateTime(year, month, 1, 0, 0, 0, 0).DayOfWeek;
            var indexOfWeekDayOfFirstDay = (weekDayOfFirstDay - startWeekDay + DEFAULT_DAY_COUNT_PER_WEEK) % DEFAULT_DAY_COUNT_PER_WEEK;

            _selectedDateDayOfWeek = ((int)CurrentValue.Value.DayOfWeek - startWeekDay + DEFAULT_DAY_COUNT_PER_WEEK) % DEFAULT_DAY_COUNT_PER_WEEK;

            var days = indexOfWeekDayOfFirstDay + dayOfMonth;

            _selectedDateWeek = days % DEFAULT_DAY_COUNT_PER_WEEK == 0 ? (days / DEFAULT_DAY_COUNT_PER_WEEK) - 1 : days / DEFAULT_DAY_COUNT_PER_WEEK;

            if (indexOfWeekDayOfFirstDay is 0)
            {
                _selectedDateWeek++;
            }
        }
    }

    private void ChangeYearRanges(int fromYear)
    {
        _yearPickerStartYear = fromYear;
        _yearPickerEndYear = fromYear + 11;
    }

    private bool IsInCurrentMonth(int week, int day)
    {
        return (
                ((week == 0 || week == 1) && _daysOfCurrentMonth[week, day] > 20) ||
                ((week == 4 || week == 5) && _daysOfCurrentMonth[week, day] < 7)
               ) is false;
    }

    private int FindMonth(int week, int day)
    {
        int month = _currentMonth;

        if (IsInCurrentMonth(week, day) is false)
        {
            if (week >= 4)
            {
                month = _currentMonth < 12 ? _currentMonth + 1 : 1;
            }
            else
            {
                month = _currentMonth > 1 ? _currentMonth - 1 : 12;
            }
        }

        return month;
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

    private DayOfWeek GetDayOfWeek(int index)
    {
        int dayOfWeek = (int)_culture.DateTimeFormat.FirstDayOfWeek + index;

        if (dayOfWeek > 6)
        {
            dayOfWeek -= 7;
        }

        return (DayOfWeek)dayOfWeek;
    }

    private int GetWeekNumber(int weekIndex)
    {
        int year = _currentYear;
        int month = FindMonth(weekIndex, 0);

        if (IsInCurrentMonth(weekIndex, 0) is false)
        {
            if (_currentMonth == 12 && month == 1)
            {
                year++;
            }
            else if (_currentMonth == 1 && month == 12)
            {
                year--;
            }
        }

        int day = _daysOfCurrentMonth[weekIndex, 0];
        var date = _culture.Calendar.ToDateTime(year, month, day, 0, 0, 0, 0);

        return _culture.Calendar.GetWeekOfYear(date, CalendarWeekRule.FirstFullWeek, _culture.DateTimeFormat.FirstDayOfWeek);
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
        if (isNext && MaxDate.HasValue)
        {
            var MaxDateYear = _culture.Calendar.GetYear(MaxDate.Value.DateTime);
            var MaxDateMonth = _culture.Calendar.GetMonth(MaxDate.Value.DateTime);

            if (MaxDateYear == _currentYear && MaxDateMonth == _currentMonth) return false;
        }


        if (isNext is false && MinDate.HasValue)
        {
            var MinDateYear = _culture.Calendar.GetYear(MinDate.Value.DateTime);
            var MinDateMonth = _culture.Calendar.GetMonth(MinDate.Value.DateTime);

            if (MinDateYear == _currentYear && MinDateMonth == _currentMonth) return false;
        }

        return true;
    }

    private bool CanChangeYear(bool isNext)
    {
        return (
                (isNext && MaxDate.HasValue && _culture.Calendar.GetYear(MaxDate.Value.DateTime) == _currentYear) ||
                (isNext is false && MinDate.HasValue && _culture.Calendar.GetYear(MinDate.Value.DateTime) == _currentYear)
               ) is false;
    }

    private bool CanChangeYearRange(bool isNext)
    {
        if (IsEnabled is false) return false;

        return (
                (isNext && MaxDate.HasValue && _culture.Calendar.GetYear(MaxDate.Value.DateTime) < _yearPickerStartYear + 12) ||
                (isNext is false && MinDate.HasValue && _culture.Calendar.GetYear(MinDate.Value.DateTime) >= _yearPickerStartYear)
               ) is false;
    }

    private bool IsWeekDayOutOfMinAndMaxDate(int dayIndex, int weekIndex)
    {
        var day = _daysOfCurrentMonth[weekIndex, dayIndex];
        var month = FindMonth(weekIndex, dayIndex);

        if (MaxDate.HasValue)
        {
            var MaxDateYear = _culture.Calendar.GetYear(MaxDate.Value.DateTime);
            var MaxDateMonth = _culture.Calendar.GetMonth(MaxDate.Value.DateTime);
            var MaxDateDay = _culture.Calendar.GetDayOfMonth(MaxDate.Value.DateTime);

            if (_currentYear > MaxDateYear ||
                (_currentYear == MaxDateYear && month > MaxDateMonth) ||
                (_currentYear == MaxDateYear && month == MaxDateMonth && day > MaxDateDay)) return true;
        }

        if (MinDate.HasValue)
        {
            var MinDateYear = _culture.Calendar.GetYear(MinDate.Value.DateTime);
            var MinDateMonth = _culture.Calendar.GetMonth(MinDate.Value.DateTime);
            var MinDateDay = _culture.Calendar.GetDayOfMonth(MinDate.Value.DateTime);

            if (_currentYear < MinDateYear ||
                (_currentYear == MinDateYear && month < MinDateMonth) ||
                (_currentYear == MinDateYear && month == MinDateMonth && day < MinDateDay)) return true;
        }

        return false;
    }

    private bool IsMonthOutOfMinAndMaxDate(int month)
    {
        if (MaxDate.HasValue)
        {
            var MaxDateYear = _culture.Calendar.GetYear(MaxDate.Value.DateTime);
            var MaxDateMonth = _culture.Calendar.GetMonth(MaxDate.Value.DateTime);

            if (_currentYear > MaxDateYear || (_currentYear == MaxDateYear && month > MaxDateMonth)) return true;
        }

        if (MinDate.HasValue)
        {
            var MinDateYear = _culture.Calendar.GetYear(MinDate.Value.DateTime);
            var MinDateMonth = _culture.Calendar.GetMonth(MinDate.Value.DateTime);

            if (_currentYear < MinDateYear || (_currentYear == MinDateYear && month < MinDateMonth)) return true;
        }

        return false;
    }

    private bool IsYearOutOfMinAndMaxDate(int year)
    {
        return (MaxDate.HasValue && year > _culture.Calendar.GetYear(MaxDate.Value.DateTime))
            || (MinDate.HasValue && year < _culture.Calendar.GetYear(MinDate.Value.DateTime));
    }

    private void CheckCurrentCalendarMatchesCurrentValue()
    {
        var currentValue = CurrentValue.GetValueOrDefault(DateTimeOffset.Now);
        var currentValueYear = _culture.Calendar.GetYear(currentValue.DateTime);
        var currentValueMonth = _culture.Calendar.GetMonth(currentValue.DateTime);
        var currentValueDay = _culture.Calendar.GetDayOfMonth(currentValue.DateTime);

        if (currentValueYear != _currentYear || currentValueMonth != _currentMonth || (AllowTextInput && currentValueDay != _currentDay))
        {
            _currentYear = currentValueYear;
            _currentMonth = currentValueMonth;
            GenerateMonthData(_currentYear, _currentMonth);
        }
    }

    private (string style, string klass) GetDayButtonCss(int day, int week, int todayYear, int todayMonth, int todayDay)
    {
        StringBuilder klass = new StringBuilder();
        StringBuilder style = new StringBuilder();

        if (week == _selectedDateWeek && day == _selectedDateDayOfWeek)
        {
            klass.Append(" bit-dtp-dbs");

            if (Classes?.SelectedDayButton is not null)
            {
                klass.Append(' ').Append(Classes?.SelectedDayButton);
            }

            if (Styles?.SelectedDayButton is not null)
            {
                style.Append(Styles?.SelectedDayButton);
            }
        }

        if (IsInCurrentMonth(week, day) is false)
        {
            klass.Append(" bit-dtp-dbo");
        }

        var currentDay = _daysOfCurrentMonth[week, day];
        if (IsInCurrentMonth(week, day) && todayYear == _currentYear && todayMonth == _currentMonth && todayDay == currentDay)
        {
            klass.Append(" bit-dtp-dtd");

            if (Classes?.TodayDayButton is not null)
            {
                klass.Append(' ').Append(Classes?.TodayDayButton);
            }

            if (Styles?.TodayDayButton is not null)
            {
                style.Append(' ').Append(Styles?.TodayDayButton);
            }
        }

        return (style.ToString(), klass.ToString());
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

        return className.ToString();
    }

    private DateTimeOffset GetDateTimeOfDayCell(int dayIndex, int weekIndex)
    {
        int selectedMonth = FindMonth(weekIndex, dayIndex);
        var currentDay = _daysOfCurrentMonth[weekIndex, dayIndex];
        var currentYear = _currentYear;
        if (selectedMonth < _currentMonth && _currentMonth == 12 && IsInCurrentMonth(weekIndex, dayIndex) is false)
        {
            currentYear++;
        }

        if (selectedMonth > _currentMonth && _currentMonth == 1 && IsInCurrentMonth(weekIndex, dayIndex) is false)
        {
            currentYear--;
        }

        return new DateTimeOffset(_culture.Calendar.ToDateTime(currentYear, selectedMonth, currentDay, 0, 0, 0, 0), DateTimeOffset.Now.Offset);
    }

    private DateTimeOffset GetDateTimeOfMonthCell(int monthIndex)
    {
        return new(_culture.Calendar.ToDateTime(_currentYear, monthIndex, 1, 0, 0, 0, 0), DateTimeOffset.Now.Offset);
    }

    private async Task UpdateCurrentValue()
    {
        if (CurrentValue.HasValue is false) return;

        var currentValueYear = _culture.Calendar.GetYear(CurrentValue.Value.LocalDateTime);
        var currentValueMonth = _culture.Calendar.GetMonth(CurrentValue.Value.LocalDateTime);
        var currentValueDay = _culture.Calendar.GetDayOfMonth(CurrentValue.Value.LocalDateTime);

        var dateTime = _culture.Calendar.ToDateTime(currentValueYear, currentValueMonth, currentValueDay, _hour, _minute, 0, 0);
        CurrentValue = new(dateTime, DateTimeOffset.Now.Offset);
    }

    private async Task HandleOnTimeHourFocus()
    {
        if (IsEnabled is false || ShowTimePicker is false) return;

        await _js.BitUtilsSelectText(_inputTimeHourRef);
    }

    private async Task HandleOnTimeMinuteFocus()
    {
        if (IsEnabled is false || ShowTimePicker is false) return;

        await _js.BitUtilsSelectText(_inputTimeMinuteRef);
    }

    private void ToggleAmPmTime()
    {
        if (IsEnabled is false) return;

        _hourView = _hour + (_hour >= 12 ? -12 : 12);
    }

    private async Task HandleOnAmClick()
    {
        _hour %= 12;  // "12:-- am" is "00:--" in 24h
        await UpdateCurrentValue();
    }

    private async Task HandleOnPmClick()
    {
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
        if (IsEnabled is false) return;

        await ChangeTime(isNext, isHour);
        ResetCts();

        var cts = _cancellationTokenSource;
        await Task.Run(async () =>
        {
            await InvokeAsync(async () =>
            {
                await Task.Delay(400);
                await ContinuousChangeTime(isNext, isHour, cts);
            });
        }, cts.Token);
    }

    private async Task ContinuousChangeTime(bool isNext, bool isHour, CancellationTokenSource cts)
    {
        if (cts.IsCancellationRequested) return;

        await ChangeTime(isNext, isHour);

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
        _cancellationTokenSource?.Cancel();
        _cancellationTokenSource?.Dispose();
        _cancellationTokenSource = new();
    }

    private async Task ChangeHour(bool isNext)
    {
        if (isNext)
        {
            _hour += HourStep;
        }
        else
        {
            _hour -= HourStep;
        }

        if (_hour > 23)
        {
            _hour -= 24;
        }
        else if (_hour < 0)
        {
            _hour += 24;
        }

        await UpdateCurrentValue();
    }

    private async Task ChangeMinute(bool isNext)
    {
        if (isNext)
        {
            _minute += MinuteStep;
        }
        else
        {
            _minute -= MinuteStep;
        }

        if (_minute > 59)
        {
            _minute -= 60;
        }
        else if (_minute < 0)
        {
            _minute += 60;
        }

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
        _isMonthPickerOverlayOnTop = false;
        _showMonthPickerAsOverlayInternal = ShowMonthPickerAsOverlay;
        _isTimePickerOverlayOnTop = false;
        _showTimePickerAsOverlayInternal = ShowTimePickerAsOverlay;
    }

    private async Task<bool> ToggleCallout()
    {
        if (Standalone) return false;
        if (IsEnabled is false) return false;

        return await _js.BitCalloutToggleCallout(_dotnetObj,
                                       _datePickerId,
                                       null,
                                       _calloutId,
                                       null,
                                       IsOpen,
                                       Responsive ? BitResponsiveMode.Top : BitResponsiveMode.None,
                                       BitDropDirection.TopAndBottom,
                                       Dir is BitDir.Rtl,
                                       "",
                                       0,
                                       "",
                                       "",
                                       false, 
                                       MAX_WIDTH);
    }

    private string GetCalloutCssClasses()
    {
        List<string> classes = ["bit-dtp-cal"];

        if (Classes?.Callout is not null)
        {
            classes.Add(Classes.Callout);
        }

        if (Responsive)
        {
            classes.Add("bit-dtp-res");
        }

        return string.Join(' ', classes).Trim();
    }



    public async ValueTask DisposeAsync()
    {
        await DisposeAsync(true);
        GC.SuppressFinalize(this);
    }

    protected virtual async ValueTask DisposeAsync(bool disposing)
    {
        if (_disposed || disposing is false) return;

        _cancellationTokenSource?.Dispose();
        OnValueChanged -= HandleOnValueChanged;

        try
        {
            await _js.BitCalloutClearCallout(_calloutId);
            await _js.BitSwipesDispose(_calloutId);
        }
        catch (JSDisconnectedException) { } // we can ignore this exception here

        _disposed = true;
    }
}
