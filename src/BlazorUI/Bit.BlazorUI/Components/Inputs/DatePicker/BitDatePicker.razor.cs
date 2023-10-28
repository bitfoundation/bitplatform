using System.Text;
using System.Globalization;
using System.Diagnostics.CodeAnalysis;

namespace Bit.BlazorUI;

public partial class BitDatePicker
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

    private int _timeHour;
    private int _timeHourView
    {
        get
        {
            if (TimeFormat == BitTimeFormat.TwelveHours)
            {
                if (_timeHour > 12)
                {
                    return _timeHour - 12;
                }

                if (_timeHour == 0)
                {
                    return 12;
                }
            }

            return _timeHour;
        }
        set
        {
            if (value > 23)
            {
                _timeHour = 23;
            }
            else if (value < 0)
            {
                _timeHour = 0;
            }
            else
            {
                _timeHour = value;
            }

            UpdateTime();
        }
    }

    private int _timeMinute;
    private int _timeMinuteView
    {
        get => _timeMinute;
        set
        {
            if (value > 59)
            {
                _timeMinute = 59;
            }
            else if (value < 0)
            {
                _timeMinute = 0;
            }
            else
            {
                _timeMinute = value;
            }

            UpdateTime();
        }
    }



    private int _currentDay;
    private int _currentYear;
    private int _displayYear;
    private int _currentMonth;
    private int? _selectedDateWeek;
    private int _yearPickerEndYear;
    private int _yearPickerStartYear;
    private int? _selectedDateDayOfWeek;
    private bool _showMonthPicker = true;
    private bool _isMonthPickerOverlayOnTop;
    private string _monthTitle = string.Empty;
    private bool _showMonthPickerAsOverlayInternal;
    private DotNetObjectReference<BitDatePicker> _dotnetObj = default!;
    private int[,] _daysOfCurrentMonth = new int[DEFAULT_WEEK_COUNT, DEFAULT_DAY_COUNT_PER_WEEK];

    private string _datePickerId = string.Empty;
    private string _calloutId = string.Empty;
    private string? _labelId;
    private string? _textFieldId;
    private string? _activeDescendantId;
    private ElementReference _inputTimeHourRef = default!;
    private ElementReference _inputTimeMinuteRef = default!;



    [Inject] private IJSRuntime _js { get; set; } = default!;



    /// <summary>
    /// Whether or not the DatePicker allows a string date input.
    /// </summary>
    [Parameter] public bool AllowTextInput { get; set; }

    /// <summary>
    /// Aria label of the DatePicker's callout for screen readers.
    /// </summary>
    [Parameter] public string CalloutAriaLabel { get; set; } = "Calendar";

    /// <summary>
    /// Capture and render additional html attributes for the DatePicker's callout.
    /// </summary>
    [Parameter] public Dictionary<string, object> CalloutHtmlAttributes { get; set; } = new();

    /// <summary>
    /// Custom CSS classes for different parts of the BitDatePicker component.
    /// </summary>
    [Parameter] public BitDatePickerClassStyles? Classes { get; set; }

    /// <summary>
    /// CultureInfo for the DatePicker.
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
    /// The format of the date in the DatePicker.
    /// </summary>
    [Parameter] public string? DateFormat { get; set; }

    /// <summary>
    /// Custom template to render the day cells of the DatePicker.
    /// </summary>
    [Parameter] public RenderFragment<DateTimeOffset>? DayCellTemplate { get; set; }

    /// <summary>
    /// The title of the Go to next month button.
    /// </summary>
    [Parameter] public string GoToNextMonthTitle { get; set; } = "Go to next month";

    /// <summary>
    /// The title of the Go to next year button (tooltip).
    /// </summary>
    [Parameter] public string GoToNextYearTitle { get; set; } = "Go to next year {0}";

    /// <summary>
    /// The title of the Go to next year range button (tooltip).
    /// </summary>
    [Parameter] public string GoToNextYearRangeTitle { get; set; } = "Next year range {0} - {1}";

    /// <summary>
    /// The title of the Go to previous year range button (tooltip).
    /// </summary>
    [Parameter] public string GoToPreviousYearRangeTitle { get; set; } = "Previous year range {0} - {1}";

    /// <summary>
    /// The title of the Go to previous month button.
    /// </summary>
    [Parameter] public string GoToPrevMonthTitle { get; set; } = "Go to previous month";

    /// <summary>
    /// The title of the Go to previous year button (tooltip).
    /// </summary>
    [Parameter] public string GoToPrevYearTitle { get; set; } = "Go to previous year {0}";

    /// <summary>
    /// The title of the GoToToday button (tooltip).
    /// </summary>
    [Parameter] public string GoToTodayTitle { get; set; } = "Go to today";

    /// <summary>
    /// Determines if the DatePicker has a border.
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
    /// Custom template for the DatePicker's icon.
    /// </summary>
    [Parameter] public RenderFragment? IconTemplate { get; set; }

    /// <summary>
    /// Determines the location of the DatePicker's icon.
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
    [Parameter]
    public bool IsOpen
    {
        get => isOpen;
        set
        {
            if (isOpen == value) return;

            isOpen = value;
            ClassBuilder.Reset();
        }
    }

    [Parameter] public EventCallback<bool> IsOpenChanged { get; set; }

    /// <summary>
    /// Enables the responsive mode in small screens.
    /// </summary>
    [Parameter] public bool IsResponsive { get; set; }

    /// <summary>
    /// Whether or not the text field of the DatePicker is underlined.
    /// </summary>
    [Parameter] public bool IsUnderlined { get; set; }

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
    [Parameter] public DateTimeOffset? MaxDate { get; set; }

    /// <summary>
    /// The minimum date allowed for the DatePicker.
    /// </summary>
    [Parameter] public DateTimeOffset? MinDate { get; set; }

    /// <summary>
    /// Custom template to render the month cells of the DatePicker.
    /// </summary>
    [Parameter] public RenderFragment<DateTimeOffset>? MonthCellTemplate { get; set; }

    /// <summary>
    /// The aria-label of the month picker's toggle.
    /// </summary>
    [Parameter] public string MonthPickerToggleAriaLabel { get; set; } = "{0}, change month";

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
    /// The callback for selecting a date in the DatePicker.
    /// </summary>
    [Parameter] public EventCallback<DateTimeOffset?> OnSelectDate { get; set; }

    /// <summary>
    /// The text of selected date aria-atomic of the DatePicker.
    /// </summary>
    [Parameter] public string SelectedDateAriaAtomic { get; set; } = "Selected date {0}";

    /// <summary>
    /// The placeholder text of the DatePicker's input.
    /// </summary>
    [Parameter] public string Placeholder { get; set; } = string.Empty;

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
    /// The aria-label of the week number.
    /// </summary>
    [Parameter] public string WeekNumberAriaLabel { get; set; } = "Week number {0}";

    /// <summary>
    /// The title of the week number (tooltip).
    /// </summary>
    [Parameter] public string WeekNumberTitle { get; set; } = "Week number {0}";

    /// <summary>
    /// Custom template to render the year cells of the DatePicker.
    /// </summary>
    [Parameter] public RenderFragment<int>? YearCellTemplate { get; set; }

    /// <summary>
    /// The aria-label of the year picker's toggle.
    /// </summary>
    [Parameter] public string YearPickerToggleAriaLabel { get; set; } = "{0}, change year";

    /// <summary>
    /// The aria-label of the year range picker's toggle.
    /// </summary>
    [Parameter] public string YearRangePickerToggleAriaLabel { get; set; } = "{0} - {1}, change month";


    public Task OpenCallout()
    {
        return HandleOnClick();
    }


    protected override string RootElementClass { get; } = "bit-dtp";

    protected override void RegisterCssClasses()
    {
        ClassBuilder.Register(() => Classes?.Root);

        ClassBuilder.Register(() => Culture.TextInfo.IsRightToLeft ? $"{RootElementClass}-rtl" : string.Empty);

        ClassBuilder.Register(() => IconLocation is BitIconLocation.Left ? $"{RootElementClass}-lic" : string.Empty);

        ClassBuilder.Register(() => IsUnderlined ? $"{RootElementClass}-und" : string.Empty);

        ClassBuilder.Register(() => HasBorder is false ? $"{RootElementClass}-nbd" : string.Empty);

        ClassBuilder.Register(() => _focusClass);
    }

    protected override void RegisterCssStyles()
    {
        StyleBuilder.Register(() => Styles?.Root);
    }

    protected override void OnInitialized()
    {
        _dotnetObj = DotNetObjectReference.Create(this);

        _datePickerId = $"DatePicker-{UniqueId}";
        _labelId = $"{_datePickerId}-label";
        _calloutId = $"{_datePickerId}-callout";
        _textFieldId = $"{_datePickerId}-text-field";
        _activeDescendantId = $"{_datePickerId}-active-descendant";

        base.OnInitialized();
    }

    protected override void OnParametersSet()
    {
        var dateTime = CurrentValue.GetValueOrDefault(DateTimeOffset.Now);

        if (MinDate.HasValue && MinDate > dateTime)
        {
            dateTime = MinDate.GetValueOrDefault(DateTimeOffset.Now);
        }

        if (MaxDate.HasValue && MaxDate < dateTime)
        {
            dateTime = MaxDate.GetValueOrDefault(DateTimeOffset.Now);
        }

        _timeHour = CurrentValue.HasValue ? CurrentValue.Value.Hour : 0;
        _timeMinute = CurrentValue.HasValue ? CurrentValue.Value.Minute : 0;

        GenerateCalendarData(dateTime.DateTime);

        base.OnParametersSet();
    }

    protected override bool TryParseValueFromString(string? value, [MaybeNullWhen(false)] out DateTimeOffset? result, [NotNullWhen(false)] out string? validationErrorMessage)
    {
        if (value.HasNoValue())
        {
            result = null;
            validationErrorMessage = null;
            return true;
        }

        if (DateTime.TryParseExact(value, DateFormat ?? Culture.DateTimeFormat.ShortDatePattern, Culture, DateTimeStyles.None, out DateTime parsedValue))
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
            ? value.Value.ToString(DateFormat ?? Culture.DateTimeFormat.ShortDatePattern, Culture)
            : null;
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

        if (CurrentValue.HasValue)
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

        var oldValue = CurrentValue.GetValueOrDefault(DateTimeOffset.Now);
        CurrentValueAsString = e.Value?.ToString();
        var curValue = CurrentValue.GetValueOrDefault(DateTimeOffset.Now);
        if (IsOpen && oldValue != curValue)
        {
            CheckCurrentCalendarMatchesCurrentValue();
            if (curValue.Year != oldValue.Year)
            {
                _displayYear = curValue.Year;
                ChangeYearRanges(_currentYear - 1);
            }
        }

        await OnSelectDate.InvokeAsync(CurrentValue);
    }

    private async Task SelectDate(int dayIndex, int weekIndex)
    {
        if (IsEnabled is false) return;
        if (ValueHasBeenSet && ValueChanged.HasDelegate is false) return;
        if (IsOpenHasBeenSet && IsOpenChanged.HasDelegate is false) return;
        if (IsWeekDayOutOfMinAndMaxDate(dayIndex, weekIndex)) return;

        _currentDay = _daysOfCurrentMonth[weekIndex, dayIndex];
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

        IsOpen = false;
        await ToggleCallout();

        _displayYear = _currentYear;
        _currentMonth = selectedMonth;

        var currentDateTime = Culture.Calendar.ToDateTime(_currentYear, _currentMonth, _currentDay, _timeHour, _timeMinute, 0, 0);
        CurrentValue = new DateTimeOffset(currentDateTime, DateTimeOffset.Now.Offset);

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
            ToggleMonthPickerAsOverlay();
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
        _selectedDateWeek = null;
        _selectedDateDayOfWeek = null;
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

        SetSelectedDateWeek();
    }

    private void SetSelectedDateWeek()
    {
        if (Culture is null) return;
        if (CurrentValue.HasValue is false || (_selectedDateWeek.HasValue && _selectedDateDayOfWeek.HasValue)) return;

        var year = Culture.Calendar.GetYear(CurrentValue.Value.DateTime);
        var month = Culture.Calendar.GetMonth(CurrentValue.Value.DateTime);

        if (year == _currentYear && month == _currentMonth)
        {
            var dayOfMonth = Culture.Calendar.GetDayOfMonth(CurrentValue.Value.DateTime);
            var startWeekDay = (int)Culture.DateTimeFormat.FirstDayOfWeek;
            var weekDayOfFirstDay = (int)Culture.Calendar.ToDateTime(year, month, 1, 0, 0, 0, 0).DayOfWeek;
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

    private bool IsGoToTodayButtonDisabled(int todayYear, int todayMonth)
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
        int dayOfWeek = (int)Culture.DateTimeFormat.FirstDayOfWeek + index;

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

    private void ToggleMonthPickerAsOverlay()
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
        var currentValue = CurrentValue.GetValueOrDefault(DateTimeOffset.Now);
        var currentValueYear = Culture.Calendar.GetYear(currentValue.DateTime);
        var currentValueMonth = Culture.Calendar.GetMonth(currentValue.DateTime);
        var currentValueDay = Culture.Calendar.GetDayOfMonth(currentValue.DateTime);

        if (currentValueYear != _currentYear || currentValueMonth != _currentMonth || (AllowTextInput && currentValueDay != _currentDay))
        {
            _currentYear = currentValueYear;
            _currentMonth = currentValueMonth;
            GenerateMonthData(_currentYear, _currentMonth);
        }
    }

    private string GetDateCellCssClass(int day, int week)
    {
        return (week == _selectedDateWeek && day == _selectedDateDayOfWeek)
            ? " bit-dtp-dcs"
            : string.Empty;
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
        if (todayYear == _currentYear && todayMonth == _currentMonth && todayDay == currentDay)
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
        if (HighlightCurrentMonth && todayMonth == monthIndex && todayYear == _displayYear)
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

        return new DateTimeOffset(Culture.Calendar.ToDateTime(currentYear, selectedMonth, currentDay, 0, 0, 0, 0), DateTimeOffset.Now.Offset);
    }

    private DateTimeOffset GetDateTimeOfMonthCell(int monthIndex)
    {
        return new(Culture.Calendar.ToDateTime(_currentYear, monthIndex, 1, 0, 0, 0, 0), DateTimeOffset.Now.Offset);
    }

    private void UpdateTime()
    {
        if (CurrentValue.HasValue is false) return;

        var currentValueYear = Culture.Calendar.GetYear(CurrentValue.Value.LocalDateTime);
        var currentValueMonth = Culture.Calendar.GetMonth(CurrentValue.Value.LocalDateTime);
        var currentValueDay = Culture.Calendar.GetDayOfMonth(CurrentValue.Value.LocalDateTime);
        CurrentValue = new DateTimeOffset(Culture.Calendar.ToDateTime(currentValueYear, currentValueMonth, currentValueDay, _timeHour, _timeMinute, 0, 0), DateTimeOffset.Now.Offset);
    }

    private async Task HandleOnTimeHourFocus()
    {
        if (IsEnabled is false || ShowTimePicker is false) return;

        await _js.SelectText(_inputTimeHourRef);
    }

    private async Task HandleOnTimeMinuteFocus()
    {
        if (IsEnabled is false || ShowTimePicker is false) return;

        await _js.SelectText(_inputTimeMinuteRef);
    }

    private void ToggleAmPmTime()
    {
        if (IsEnabled is false) return;

        _timeHourView = _timeHour + (_timeHour >= 12 ? -12 : 12);
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
                                       _datePickerId,
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
