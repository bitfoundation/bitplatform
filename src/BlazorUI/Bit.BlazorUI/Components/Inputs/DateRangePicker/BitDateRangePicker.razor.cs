using System.Text;
using System.Globalization;
using System.Diagnostics.CodeAnalysis;

namespace Bit.BlazorUI;

public partial class BitDateRangePicker
{
    private const int DEFAULT_DAY_COUNT_PER_WEEK = 7;
    private const int DEFAULT_WEEK_COUNT = 6;



    private bool IsOpenHasBeenSet;

    private bool isOpen;
    private CultureInfo culture = CultureInfo.CurrentUICulture;
    private BitIconLocation iconLocation = BitIconLocation.Right;

    private string focusClass = string.Empty;
    private string _focusClass
    {
        get => focusClass;
        set
        {
            if (focusClass == value) return;

            focusClass = value;
            ClassBuilder.Reset();
        }
    }

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
                _startTimeHour = 23;
            }
            else if (value < 0)
            {
                _startTimeHour = 0;
            }
            else
            {
                _startTimeHour = value;
            }

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
                _startTimeMinute = 59;
            }
            else if (value < 0)
            {
                _startTimeMinute = 0;
            }
            else
            {
                _startTimeMinute = value;
            }

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
                _endTimeHour = 23;
            }
            else if (value < 0)
            {
                _endTimeHour = 0;
            }
            else
            {
                _endTimeHour = value;
            }

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
                _endTimeMinute = 59;
            }
            else if (value < 0)
            {
                _endTimeMinute = 0;
            }
            else
            {
                _endTimeMinute = value;
            }

            UpdateTime();
        }
    }



    private int _currentYear;
    private int _displayYear;
    private int _currentMonth;
    private int _yearPickerEndYear;
    private int _yearPickerStartYear;
    private int? _selectedEndDateWeek;
    private int? _selectedStartDateWeek;
    private bool _showMonthPicker = true;
    private int? _selectedEndDateDayOfWeek;
    private bool _isMonthPickerOverlayOnTop;
    private int? _selectedStartDateDayOfWeek;
    private string _monthTitle = string.Empty;
    private bool _showMonthPickerAsOverlayInternal;
    private DotNetObjectReference<BitDateRangePicker> _dotnetObj = default!;
    private int[,] _daysOfCurrentMonth = new int[DEFAULT_WEEK_COUNT, DEFAULT_DAY_COUNT_PER_WEEK];

    private string _dateRangePickerId = string.Empty;
    private string _calloutId = string.Empty;
    private string? _labelId;
    private string? _inputId;
    private string? _activeDescendantId;
    private ElementReference _inputStartTimeHourRef = default!;
    private ElementReference _inputStartTimeMinuteRef = default!;
    private ElementReference _inputEndTimeHourRef = default!;
    private ElementReference _inputEndTimeMinuteRef = default!;

    [Inject] private IJSRuntime _js { get; set; } = default!;


    /// <summary>
    /// Whether or not the DateRangePicker allows string date inputs.
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
    /// The title of the close button (tooltip).
    /// </summary>
    [Parameter] public string CloseButtonTitle { get; set; } = "Close date range picker";

    /// <summary>
    /// CultureInfo for the DateRangePicker.
    /// </summary>
    [Parameter]
    public CultureInfo Culture
    {
        get => culture;
        set
        {
            if (culture == value) return;

            culture = value;
            ClassBuilder.Reset();
        }
    }

    /// <summary>
    /// The format of the date in the DateRangePicker.
    /// </summary>
    [Parameter] public string? DateFormat { get; set; }

    /// <summary>
    /// Custom template to render the day cells of the DateRangePicker.
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
    /// Determines if the DateRangePicker has a border.
    /// </summary>
    [Parameter] public bool HasBorder { get; set; } = true;

    /// <summary>
    /// Whether the month picker should highlight the current month.
    /// </summary>
    [Parameter] public bool HighlightCurrentMonth { get; set; } = false;

    /// <summary>
    /// Whether the month picker should highlight the selected month.
    /// </summary>
    [Parameter] public bool HighlightSelectedMonth { get; set; } = false;

    /// <summary>
    /// Custom template for the DateRangePicker's icon.
    /// </summary>
    [Parameter] public RenderFragment? IconTemplate { get; set; }

    /// <summary>
    /// Determines the location of the DateRangePicker's icon.
    /// </summary>
    [Parameter]
    public BitIconLocation IconLocation
    {
        get => iconLocation;
        set
        {
            if (iconLocation == value) return;

            iconLocation = value;
            ClassBuilder.Reset();
        }
    }

    /// <summary>
    /// The name of the DateRangePicker's icon.
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
    /// Whether or not the DateRangePicker's callout is open.
    /// </summary>
    [Parameter]
    public bool IsOpen
    {
        get => isOpen;
        set
        {
            if (isOpen == value) return;

            isOpen = value;

            _ = IsOpenChanged.InvokeAsync(value);
        }
    }

    [Parameter] public EventCallback<bool> IsOpenChanged { get; set; }

    /// <summary>
    /// Enables the responsive mode in small screens.
    /// </summary>
    [Parameter] public bool IsResponsive { get; set; }

    /// <summary>
    /// Whether or not the Text field of the DateRangePicker is underlined.
    /// </summary>
    [Parameter] public bool IsUnderlined { get; set; }

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
    [Parameter] public DateTimeOffset? MaxDate { get; set; }

    /// <summary>
    /// The minimum date allowed for the DateRangePicker.
    /// </summary>
    [Parameter] public DateTimeOffset? MinDate { get; set; }

    /// <summary>
    /// Custom template to render the month cells of the DateRangePicker.
    /// </summary>
    [Parameter] public RenderFragment<DateTimeOffset>? MonthCellTemplate { get; set; }

    /// <summary>
    /// The title of the month picker's toggle (tooltip).
    /// </summary>
    [Parameter] public string MonthPickerToggleTitle { get; set; } = "{0}, change month";

    /// <summary>
    /// The callback for clicking on the DateRangePicker's input.
    /// </summary>
    [Parameter] public EventCallback OnClick { get; set; }

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
    /// The callback for selecting a date in the DateRangePicker.
    /// </summary>
    [Parameter] public EventCallback<BitDateRangePickerValue> OnSelectDate { get; set; }

    /// <summary>
    /// The placeholder text of the DateRangePicker's input.
    /// </summary>
    [Parameter] public string Placeholder { get; set; } = string.Empty;

    /// <summary>
    /// Whether the DateRangePicker's close button should be shown or not.
    /// </summary>
    [Parameter] public bool ShowCloseButton { get; set; }

    /// <summary>
    /// Whether the GoToToday button should be shown or not.
    /// </summary>
    [Parameter] public bool ShowGoToToday { get; set; } = true;

    /// <summary>
    /// Show month picker on top of date picker when visible.
    /// </summary>
    [Parameter] public bool ShowMonthPickerAsOverlay { get; set; }

    /// <summary>
    /// Whether or not render the time-picker.
    /// </summary>
    [Parameter] public bool ShowTimePicker { get; set; }

    /// <summary>
    /// Whether the week number (weeks 1 to 53) should be shown before each week row.
    /// </summary>
    [Parameter] public bool ShowWeekNumbers { get; set; }

    /// <summary>
    /// The tabIndex of the DateRangePicker's input.
    /// </summary>
    [Parameter] public int TabIndex { get; set; }

    /// <summary>
    /// Time format of the time-pickers, 24H or 12H.
    /// </summary>
    [Parameter] public BitTimeFormat TimeFormat { get; set; }

    /// <summary>
    /// The title of the week number (tooltip).
    /// </summary>
    [Parameter] public string WeekNumberTitle { get; set; } = "Week number {0}";

    /// <summary>
    /// The string format used to show the DateRangePicker's value in its input.
    /// </summary>
    [Parameter] public string ValueFormat { get; set; } = "Start: {0} - End: {1}";

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



    public Task OpenCallout()
    {
        return HandleOnClick();
    }



    protected override string RootElementClass => "bit-dtrp";

    protected override void RegisterCssClasses()
    {
        ClassBuilder.Register(() => Culture.TextInfo.IsRightToLeft ? $"{RootElementClass}-rtl" : string.Empty);

        ClassBuilder.Register(() => IconLocation is BitIconLocation.Left ? $"{RootElementClass}-lic" : string.Empty);

        ClassBuilder.Register(() => IsUnderlined ? $"{RootElementClass}-und" : string.Empty);

        ClassBuilder.Register(() => HasBorder is false ? $"{RootElementClass}-nbd" : string.Empty);

        ClassBuilder.Register(() => _focusClass);
    }

    protected override void OnInitialized()
    {
        _dotnetObj = DotNetObjectReference.Create(this);

        _dateRangePickerId = $"DateRangePicker-{UniqueId}";
        _labelId = $"{_dateRangePickerId}-label";
        _calloutId = $"{_dateRangePickerId}-callout";
        _inputId = $"{_dateRangePickerId}-input";
        _activeDescendantId = $"{_dateRangePickerId}-active-descendant";

        base.OnInitialized();
    }

    protected override void OnParametersSet()
    {
        CurrentValue ??= new();

        var startDateTime = CurrentValue.StartDate.GetValueOrDefault(DateTimeOffset.Now);

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
            CurrentValue.EndDate = null;
        }

        _startTimeHour = CurrentValue.StartDate.HasValue ? CurrentValue.StartDate.Value.Hour : 0;
        _startTimeMinute = CurrentValue.StartDate.HasValue ? CurrentValue.StartDate.Value.Minute : 0;

        _endTimeHour = CurrentValue.EndDate.HasValue ? CurrentValue.EndDate.Value.Hour : 23;
        _endTimeMinute = CurrentValue.EndDate.HasValue ? CurrentValue.EndDate.Value.Minute : 59;

        GenerateCalendarData(startDateTime.DateTime);

        base.OnParametersSet();
    }

    protected override bool TryParseValueFromString(string? value, [MaybeNullWhen(false)] out BitDateRangePickerValue? result, [NotNullWhen(false)] out string? validationErrorMessage)
    {
        //if (value.HasNoValue())
        //{
        //    result = null;
        //    validationErrorMessage = null;
        //    return true;
        //}

        //if (DateTime.TryParseExact(value, DateFormat ?? Culture.DateTimeFormat.ShortDatePattern, Culture, DateTimeStyles.None, out DateTime parsedValue))
        //{
        //    result = new DateTimeOffset(parsedValue, DateTimeOffset.Now.Offset);
        //    validationErrorMessage = null;
        //    return true;
        //}

        result = default;
        validationErrorMessage = InvalidErrorMessage.HasValue() ? InvalidErrorMessage! : $"The {DisplayName ?? FieldIdentifier.FieldName} field is not valid.";
        return false;
    }

    protected override string? FormatValueAsString(BitDateRangePickerValue? value)
    {
        if (value is null) return null;
        if (value.StartDate.HasValue is false && value.EndDate.HasValue is false) return null;

        return string.Format(CultureInfo.CurrentCulture, ValueFormat,
                            value.StartDate.GetValueOrDefault(DateTimeOffset.Now)
                                           .ToString(DateFormat ?? Culture.DateTimeFormat.ShortDatePattern, Culture),
                            value.EndDate.HasValue ?
                                value.EndDate.GetValueOrDefault(DateTimeOffset.Now)
                                             .ToString(DateFormat ?? Culture.DateTimeFormat.ShortDatePattern, Culture)
                                : "---");
    }


    private async Task HandleOnClick()
    {
        if (IsEnabled is false) return;
        if (IsOpenHasBeenSet && IsOpenChanged.HasDelegate is false) return;

        IsOpen = true;
        var result = await ToggleCallout();

        if (_showMonthPickerAsOverlayInternal is false)
        {
            _showMonthPickerAsOverlayInternal = result;
        }

        if (_showMonthPickerAsOverlayInternal)
        {
            _isMonthPickerOverlayOnTop = false;
        }

        if (CurrentValue is not null)
        {
            CheckCurrentCalendarMatchesCurrentValue();
        }

        _displayYear = _currentYear;

        await OnClick.InvokeAsync();
    }

    private async Task HandleOnFocusIn()
    {
        if (IsEnabled is false) return;

        _focusClass = $"{RootElementClass}-foc";

        await OnFocusIn.InvokeAsync();
    }

    private async Task HandleOnFocusOut()
    {
        if (IsEnabled is false) return;

        _focusClass = string.Empty;

        await OnFocusOut.InvokeAsync();
    }

    private async Task HandleOnFocus()
    {
        if (IsEnabled is false) return;

        _focusClass = $"{RootElementClass}-foc";

        await OnFocus.InvokeAsync();
    }

    private async Task HandleOnChange(ChangeEventArgs e)
    {
        if (IsEnabled is false) return;
        if (ValueHasBeenSet && ValueChanged.HasDelegate is false) return;
        if (AllowTextInput is false) return;

        CurrentValueAsString = e.Value?.ToString();

        await OnSelectDate.InvokeAsync(CurrentValue);
    }

    private async Task SelectDate(int dayIndex, int weekIndex)
    {
        if (IsEnabled is false) return;
        if (ValueHasBeenSet && ValueChanged.HasDelegate is false) return;
        if (IsOpenHasBeenSet && IsOpenChanged.HasDelegate is false) return;
        if (CurrentValue is null) return;
        if (IsWeekDayOutOfMinAndMaxDate(dayIndex, weekIndex)) return;

        if (CurrentValue.StartDate.HasValue && CurrentValue.EndDate.HasValue)
        {
            CurrentValue.StartDate = null;
            CurrentValue.EndDate = null;
        }

        var currentDay = _daysOfCurrentMonth[weekIndex, dayIndex];
        int selectedMonth = FindMonth(weekIndex, dayIndex);
        var isNotInCurrentMonth = IsInCurrentMonth(weekIndex, dayIndex) is false;

        if (selectedMonth < _currentMonth && _currentMonth == 12 && isNotInCurrentMonth)
        {
            _currentYear++;
        }

        if (selectedMonth > _currentMonth && _currentMonth == 1 && isNotInCurrentMonth)
        {
            _currentYear--;
        }

        _displayYear = _currentYear;
        _currentMonth = selectedMonth;

        var hour = CurrentValue.StartDate.HasValue ? _endTimeHour : _startTimeHour;
        var minute = CurrentValue.StartDate.HasValue ? _endTimeMinute : _startTimeMinute;

        var selectedDate = new DateTimeOffset(Culture.Calendar.ToDateTime(_currentYear, _currentMonth, currentDay, hour, minute, 0, 0), DateTimeOffset.Now.Offset);
        if (CurrentValue.StartDate.HasValue is false)
        {
            CurrentValue.StartDate = selectedDate;
        }
        else
        {
            CurrentValue.EndDate = selectedDate;
            if (AutoClose)
            {
                IsOpen = false;
                await ToggleCallout();
            }
        }

        if (CurrentValue.EndDate.HasValue && CurrentValue.StartDate > CurrentValue.EndDate)
        {
            (CurrentValue.EndDate, CurrentValue.StartDate) = (CurrentValue.StartDate, CurrentValue.EndDate);
        }

        CurrentValue = new BitDateRangePickerValue { StartDate = CurrentValue.StartDate, EndDate = CurrentValue.EndDate };

        GenerateMonthData(_currentYear, _currentMonth);

        await OnSelectDate.InvokeAsync(CurrentValue);
    }

    private void SelectMonth(int month)
    {
        if (IsEnabled is false) return;
        if (IsMonthOutOfMinAndMaxDate(month)) return;

        _currentMonth = month;
        _currentYear = _displayYear;

        GenerateMonthData(_currentYear, _currentMonth);

        if (_showMonthPickerAsOverlayInternal)
        {
            ToggleMonthPickerOverlay();
        }
    }

    private void SelectYear(int year)
    {
        if (IsEnabled is false) return;
        if (IsYearOutOfMinAndMaxDate(year)) return;

        _currentYear = _displayYear = year;

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

        _displayYear = _currentYear;

        GenerateMonthData(_currentYear, _currentMonth);
    }

    private void HandleYearChange(bool isNext)
    {
        if (IsEnabled is false) return;
        if (CanChangeYear(isNext) is false) return;

        _displayYear += isNext ? +1 : -1;

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

    private void GenerateCalendarData(DateTime dateTime)
    {
        _currentMonth = Culture.Calendar.GetMonth(dateTime);
        _currentYear = Culture.Calendar.GetYear(dateTime);

        _displayYear = _currentYear;

        _yearPickerStartYear = _currentYear - 1;
        _yearPickerEndYear = _currentYear + 10;

        GenerateMonthData(_currentYear, _currentMonth);
    }

    private void GenerateMonthData(int year, int month)
    {
        _selectedStartDateWeek = null;
        _selectedEndDateWeek = null;
        _selectedStartDateDayOfWeek = null;
        _selectedEndDateDayOfWeek = null;
        _monthTitle = $"{Culture.DateTimeFormat.GetMonthName(month)} {year}";

        for (int weekIndex = 0; weekIndex < DEFAULT_WEEK_COUNT; weekIndex++)
        {
            for (int dayIndex = 0; dayIndex < DEFAULT_DAY_COUNT_PER_WEEK; dayIndex++)
            {
                _daysOfCurrentMonth[weekIndex, dayIndex] = 0;
            }
        }

        var monthDays = Culture.Calendar.GetDaysInMonth(year, month);
        var firstDayOfMonth = Culture.Calendar.ToDateTime(year, month, 1, 0, 0, 0, 0);
        var startWeekDay = (int)Culture.DateTimeFormat.FirstDayOfWeek;
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
                        prevMonthDays = Culture.Calendar.GetDaysInMonth(year, prevMonth);
                    }
                    else
                    {
                        prevMonth = 12;
                        prevMonthDays = Culture.Calendar.GetDaysInMonth(year - 1, prevMonth);
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

        SetSelectedStartDateWeek();
        SetSelectedEndDateWeek();
    }

    private void SetSelectedStartDateWeek()
    {
        if (Culture is null) return;
        if (CurrentValue is null) return;
        if (CurrentValue.StartDate.HasValue is false || (_selectedStartDateWeek.HasValue && _selectedStartDateDayOfWeek.HasValue)) return;

        var year = Culture.Calendar.GetYear(CurrentValue.StartDate.Value.DateTime);
        var month = Culture.Calendar.GetMonth(CurrentValue.StartDate.Value.DateTime);

        if (year == _currentYear && month == _currentMonth)
        {
            var day = Culture.Calendar.GetDayOfMonth(CurrentValue.StartDate.Value.DateTime);
            var firstDayOfWeek = (int)Culture.DateTimeFormat.FirstDayOfWeek;
            var firstDayOfWeekInMonth = (int)Culture.Calendar.ToDateTime(year, month, 1, 0, 0, 0, 0).DayOfWeek;
            var firstDayOfWeekInMonthIndex = (firstDayOfWeekInMonth - firstDayOfWeek + DEFAULT_DAY_COUNT_PER_WEEK) % DEFAULT_DAY_COUNT_PER_WEEK;
            _selectedStartDateDayOfWeek = ((int)CurrentValue.StartDate.Value.DayOfWeek - firstDayOfWeek + DEFAULT_DAY_COUNT_PER_WEEK) % DEFAULT_DAY_COUNT_PER_WEEK;
            var days = firstDayOfWeekInMonthIndex + day;
            _selectedStartDateWeek = days % DEFAULT_DAY_COUNT_PER_WEEK == 0 ? (days / DEFAULT_DAY_COUNT_PER_WEEK) - 1 : days / DEFAULT_DAY_COUNT_PER_WEEK;
            if (firstDayOfWeekInMonthIndex is 0)
            {
                _selectedStartDateWeek++;
            }
        }
    }

    private void SetSelectedEndDateWeek()
    {
        if (Culture is null) return;
        if (CurrentValue is null) return;
        if (CurrentValue.EndDate.HasValue is false || (_selectedEndDateWeek.HasValue && _selectedEndDateDayOfWeek.HasValue)) return;

        var year = Culture.Calendar.GetYear(CurrentValue.EndDate.Value.DateTime);
        var month = Culture.Calendar.GetMonth(CurrentValue.EndDate.Value.DateTime);

        if (year == _currentYear && month == _currentMonth)
        {
            var day = Culture.Calendar.GetDayOfMonth(CurrentValue.EndDate.Value.DateTime);
            var firstDayOfWeek = (int)Culture.DateTimeFormat.FirstDayOfWeek;
            var firstDayOfWeekInMonth = (int)Culture.Calendar.ToDateTime(year, month, 1, 0, 0, 0, 0).DayOfWeek;
            var firstDayOfWeekInMonthIndex = (firstDayOfWeekInMonth - firstDayOfWeek + DEFAULT_DAY_COUNT_PER_WEEK) % DEFAULT_DAY_COUNT_PER_WEEK;
            _selectedEndDateDayOfWeek = ((int)CurrentValue.EndDate.Value.DayOfWeek - firstDayOfWeek + DEFAULT_DAY_COUNT_PER_WEEK) % DEFAULT_DAY_COUNT_PER_WEEK;
            var days = firstDayOfWeekInMonthIndex + day;
            _selectedEndDateWeek = days % DEFAULT_DAY_COUNT_PER_WEEK == 0 ? (days / DEFAULT_DAY_COUNT_PER_WEEK) - 1 : days / DEFAULT_DAY_COUNT_PER_WEEK;
            if (firstDayOfWeekInMonthIndex is 0)
            {
                _selectedEndDateWeek++;
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
        var month = _currentMonth;

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

    private bool IsGoTodayButtonDisabled(int todayYear, int todayMonth)
    {
        if (_showMonthPickerAsOverlayInternal)
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
        int dayOfWeek = (int)(Culture.DateTimeFormat.FirstDayOfWeek) + index;

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
        var date = Culture.Calendar.ToDateTime(year, month, day, 0, 0, 0, 0);

        return Culture.Calendar.GetWeekOfYear(date, CalendarWeekRule.FirstFullWeek, Culture.DateTimeFormat.FirstDayOfWeek);
    }

    private void ToggleMonthPickerOverlay()
    {
        _isMonthPickerOverlayOnTop = !_isMonthPickerOverlayOnTop;
    }

    private bool CanChangeMonth(bool isNext)
    {
        if (isNext && MaxDate.HasValue)
        {
            var MaxDateYear = Culture.Calendar.GetYear(MaxDate.Value.DateTime);
            var MaxDateMonth = Culture.Calendar.GetMonth(MaxDate.Value.DateTime);

            if (MaxDateYear == _displayYear && MaxDateMonth == _currentMonth) return false;
        }


        if (isNext is false && MinDate.HasValue)
        {
            var MinDateYear = Culture.Calendar.GetYear(MinDate.Value.DateTime);
            var MinDateMonth = Culture.Calendar.GetMonth(MinDate.Value.DateTime);

            if (MinDateYear == _displayYear && MinDateMonth == _currentMonth) return false;
        }

        return true;
    }

    private bool CanChangeYear(bool isNext)
    {
        return (
                (isNext && MaxDate.HasValue && Culture.Calendar.GetYear(MaxDate.Value.DateTime) == _displayYear) ||
                (isNext is false && MinDate.HasValue && Culture.Calendar.GetYear(MinDate.Value.DateTime) == _displayYear)
               ) is false;
    }

    private bool CanChangeYearRange(bool isNext)
    {
        return (
                (isNext && MaxDate.HasValue && Culture.Calendar.GetYear(MaxDate.Value.DateTime) < _yearPickerStartYear + 12) ||
                (isNext is false && MinDate.HasValue && Culture.Calendar.GetYear(MinDate.Value.DateTime) >= _yearPickerStartYear)
               ) is false;
    }

    private bool IsWeekDayOutOfMinAndMaxDate(int dayIndex, int weekIndex)
    {
        var day = _daysOfCurrentMonth[weekIndex, dayIndex];
        var month = FindMonth(weekIndex, dayIndex);

        if (MaxDate.HasValue)
        {
            var MaxDateYear = Culture.Calendar.GetYear(MaxDate.Value.DateTime);
            var MaxDateMonth = Culture.Calendar.GetMonth(MaxDate.Value.DateTime);
            var MaxDateDay = Culture.Calendar.GetDayOfMonth(MaxDate.Value.DateTime);

            if (_displayYear > MaxDateYear ||
               (_displayYear == MaxDateYear && month > MaxDateMonth) ||
               (_displayYear == MaxDateYear && month == MaxDateMonth && day > MaxDateDay)) return true;
        }

        if (MinDate.HasValue)
        {
            var MinDateYear = Culture.Calendar.GetYear(MinDate.Value.DateTime);
            var MinDateMonth = Culture.Calendar.GetMonth(MinDate.Value.DateTime);
            var MinDateDay = Culture.Calendar.GetDayOfMonth(MinDate.Value.DateTime);
            if (_displayYear < MinDateYear ||
               (_displayYear == MinDateYear && month < MinDateMonth) ||
               (_displayYear == MinDateYear && month == MinDateMonth && day < MinDateDay)) return true;
        }

        return false;
    }

    private bool IsMonthOutOfMinAndMaxDate(int month)
    {
        if (MaxDate.HasValue)
        {
            var MaxDateYear = Culture.Calendar.GetYear(MaxDate.Value.DateTime);
            var MaxDateMonth = Culture.Calendar.GetMonth(MaxDate.Value.DateTime);

            if (_displayYear > MaxDateYear || (_displayYear == MaxDateYear && month > MaxDateMonth)) return true;
        }

        if (MinDate.HasValue)
        {
            var MinDateYear = Culture.Calendar.GetYear(MinDate.Value.DateTime);
            var MinDateMonth = Culture.Calendar.GetMonth(MinDate.Value.DateTime);

            if (_displayYear < MinDateYear || (_displayYear == MinDateYear && month < MinDateMonth)) return true;
        }

        return false;
    }

    private bool IsYearOutOfMinAndMaxDate(int year)
    {
        return (MaxDate.HasValue && year > Culture.Calendar.GetYear(MaxDate.Value.DateTime))
            || (MinDate.HasValue && year < Culture.Calendar.GetYear(MinDate.Value.DateTime));
    }

    private void CheckCurrentCalendarMatchesCurrentValue()
    {
        if (CurrentValue is null) return;
        if (CurrentValue.StartDate is null) return;

        var currentValue = CurrentValue.StartDate.GetValueOrDefault(DateTimeOffset.Now);
        var currentValueYear = Culture.Calendar.GetYear(currentValue.DateTime);
        var currentValueMonth = Culture.Calendar.GetMonth(currentValue.DateTime);

        if (currentValueYear != _currentYear || currentValueMonth != _currentMonth)
        {
            _currentYear = currentValueYear;
            _currentMonth = currentValueMonth;
            GenerateMonthData(_currentYear, _currentMonth);
        }
    }

    private string GetDayButtonCss(int day, int week, int todayYear, int todayMonth, int todayDay)
    {
        StringBuilder className = new();
        var currentDay = _daysOfCurrentMonth[week, day];

        if (IsInCurrentMonth(week, day) is false)
        {
            className.Append(" bit-dtrp-dbo");
        }

        if (IsInCurrentMonth(week, day) && todayYear == _currentYear && todayMonth == _currentMonth && todayDay == currentDay)
        {
            className.Append(" bit-dtrp-dtd");
        }

        if (IsInCurrentMonth(week, day) && week == _selectedStartDateWeek && day == _selectedStartDateDayOfWeek)
        {
            className.Append(" bit-dtrp-dss");
        }

        if (IsInCurrentMonth(week, day) && week == _selectedEndDateWeek && day == _selectedEndDateDayOfWeek)
        {
            className.Append(" bit-dtrp-dse");
        }

        if (IsInCurrentMonth(week, day) && week == _selectedEndDateWeek && day == _selectedEndDateDayOfWeek && week == _selectedStartDateWeek && day == _selectedStartDateDayOfWeek)
        {
            className.Append(" bit-dtrp-dsse");
        }

        if (IsBetweenTwoSelectedDate(day, week))
        {
            className.Append(" bit-dtrp-dsb");
        }

        return className.ToString();
    }

    private string GetMonthCellCssClass(int monthIndex, int todayYear, int todayMonth)
    {
        var className = new StringBuilder();
        if (HighlightCurrentMonth && todayMonth == monthIndex && todayYear == _displayYear)
        {
            className.Append(" bit-dtrp-pcm");
        }

        if (HighlightSelectedMonth && _currentMonth == monthIndex)
        {
            className.Append(" bit-dtrp-psm");
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

        return new DateTimeOffset(Culture.Calendar.ToDateTime(currentYear, selectedMonth, currentDay, 0, 0, 0, 0), DateTimeOffset.Now.Offset);
    }

    private DateTimeOffset GetDateTimeOfMonthCell(int monthIndex)
    {
        return new(Culture.Calendar.ToDateTime(_currentYear, monthIndex, 1, 0, 0, 0, 0), DateTimeOffset.Now.Offset);
    }

    private bool IsBetweenTwoSelectedDate(int day, int week)
    {
        if (CurrentValue is null) return false;
        if (CurrentValue.StartDate.HasValue is false || CurrentValue.EndDate.HasValue is false) return false;

        return (_selectedEndDateWeek.HasValue is false && IsInCurrentMonth(week, day) && ((week == _selectedStartDateWeek && day > _selectedStartDateDayOfWeek) || week > _selectedStartDateWeek)) ||
               (_selectedStartDateWeek.HasValue is false && IsInCurrentMonth(week, day) && ((week == _selectedEndDateWeek && day < _selectedEndDateDayOfWeek) || week < _selectedEndDateWeek)) ||
               (_selectedEndDateWeek.HasValue && _selectedStartDateWeek.HasValue &&
                    ((week == _selectedStartDateWeek && day > _selectedStartDateDayOfWeek) || week > _selectedStartDateWeek) &&
                    ((week == _selectedEndDateWeek && day < _selectedEndDateDayOfWeek) || week < _selectedEndDateWeek)) ||
               (_selectedEndDateWeek.HasValue is false && _selectedStartDateWeek.HasValue is false && IsInCurrentMonth(week, day) &&
                    CurrentValue.StartDate.Value.Month < _currentMonth &&
                    CurrentValue.EndDate.Value.Month > _currentMonth &&
                    CurrentValue.EndDate.Value.Year >= _currentYear &&
                    CurrentValue.StartDate.Value.Year <= _currentYear);
    }

    private void UpdateTime()
    {
        if (CurrentValue is null) return;
        if (CurrentValue.StartDate.HasValue is false && CurrentValue.EndDate.HasValue is false) return;

        CurrentValue = new BitDateRangePickerValue
        {
            StartDate = GetDateTimeOffset(CurrentValue.StartDate, _startTimeHour, _startTimeMinute),
            EndDate = GetDateTimeOffset(CurrentValue.EndDate, _endTimeHour, _endTimeMinute)
        };
    }

    private DateTimeOffset? GetDateTimeOffset(DateTimeOffset? date, int hour, int minute)
    {
        if (date.HasValue is false) return null;

        var year = Culture.Calendar.GetYear(date.Value.LocalDateTime);
        var month = Culture.Calendar.GetMonth(date.Value.LocalDateTime);
        var day = Culture.Calendar.GetDayOfMonth(date.Value.LocalDateTime);

        return new DateTimeOffset(Culture.Calendar.ToDateTime(year, month, day, hour, minute, 0, 0), DateTimeOffset.Now.Offset);
    }

    private async Task HandleOnStartTimeHourFocus()
    {
        if (IsEnabled is false || ShowTimePicker is false) return;

        await _js.SelectText(_inputStartTimeHourRef);
    }

    private async Task HandleOnStartTimeMinuteFocus()
    {
        if (IsEnabled is false || ShowTimePicker is false) return;

        await _js.SelectText(_inputStartTimeMinuteRef);
    }

    private async Task HandleOnEndTimeHourFocus()
    {
        if (IsEnabled is false || ShowTimePicker is false) return;

        await _js.SelectText(_inputEndTimeHourRef);
    }

    private async Task HandleOnEndTimeMinuteFocus()
    {
        if (IsEnabled is false || ShowTimePicker is false) return;

        await _js.SelectText(_inputEndTimeMinuteRef);
    }

    private void ToggleStartTimeAmPm()
    {
        if (IsEnabled is false) return;

        _startTimeHourView = _startTimeHour + (_startTimeHour >= 12 ? -12 : 12);
    }

    private void ToggleEndTimeAmPm()
    {
        if (IsEnabled is false) return;

        _endTimeHourView = _endTimeHour + (_endTimeHour >= 12 ? -12 : 12);
    }

    private async Task CloseCallout()
    {
        if (IsEnabled is false) return;
        if (IsOpenHasBeenSet && IsOpenChanged.HasDelegate is false) return;

        IsOpen = false;
        await ToggleCallout();

        StateHasChanged();
    }

    [JSInvokable("CloseCallout")]
    public void CloseCalloutBeforeAnotherCalloutIsOpened()
    {
        if (IsEnabled is false) return;
        if (IsOpenHasBeenSet && IsOpenChanged.HasDelegate is false) return;

        IsOpen = false;
        StateHasChanged();
    }

    private async Task<bool> ToggleCallout()
    {
        if (IsEnabled is false) return false;

        _isMonthPickerOverlayOnTop = false;
        _showMonthPickerAsOverlayInternal = ShowMonthPickerAsOverlay;

        return await _js.ToggleCallout(_dotnetObj,
                                       _dateRangePickerId,
                                       _calloutId,
                                       IsOpen,
                                       IsResponsive ? BitResponsiveMode.Top : BitResponsiveMode.None,
                                       BitDropDirection.TopAndBottom,
                                       false,
                                       "",
                                       0,
                                       "",
                                       "",
                                       false,
                                       RootElementClass);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _dotnetObj.Dispose();
        }

        base.Dispose(disposing);
    }
}
