using System.Text;
using System.Globalization;
using System.Diagnostics.CodeAnalysis;

namespace Bit.BlazorUI;

/// <summary>
/// The calendar control lets people select and view a single date or a range of dates in their calendar. It’s made up of 3 separate views: the month view, year view, and decade view.
/// </summary>
public partial class BitCalendar : BitInputBase<DateTimeOffset?>
{
    private const int DEFAULT_WEEK_COUNT = 6;
    private const int DEFAULT_DAY_COUNT_PER_WEEK = 7;



    private int _currentDay;
    private int _currentYear;
    private int _currentMonth;
    private bool _showYearPicker;
    private bool _showTimePicker;
    private bool _showMonthPicker;
    private int? _selectedDateWeek;
    private int _yearPickerEndYear;
    private int _yearPickerStartYear;
    private int? _selectedDateDayOfWeek;
    private string _monthTitle = string.Empty;
    private ElementReference _inputTimeHourRef = default!;
    private ElementReference _inputTimeMinuteRef = default!;
    private CultureInfo _culture = CultureInfo.CurrentUICulture;
    private CancellationTokenSource _cancellationTokenSource = new();
    private readonly int[,] _daysOfCurrentMonth = new int[DEFAULT_WEEK_COUNT, DEFAULT_DAY_COUNT_PER_WEEK];



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

            UpdateTime();
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

            UpdateTime();
        }
    }



    [Inject] private IJSRuntime _js { get; set; } = default!;



    /// <summary>
    /// Custom CSS classes for different parts of the BitCalendar component.
    /// </summary>
    [Parameter] public BitCalendarClassStyles? Classes { get; set; }

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
    /// Whether the month picker should highlight the current month.
    /// </summary>
    [Parameter] public bool HighlightCurrentMonth { get; set; }

    /// <summary>
    /// Whether the month picker should highlight the selected month.
    /// </summary>
    [Parameter] public bool HighlightSelectedMonth { get; set; }

    /// <summary>
    /// The custom validation error message for the invalid value.
    /// </summary>
    [Parameter] public string? InvalidErrorMessage { get; set; }

    /// <summary>
    /// Whether the month picker is shown or hidden.
    /// </summary>
    [Parameter]
    [CallOnSet(nameof(OnSetParameters))]
    public bool ShowMonthPicker { get; set; } = true;

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
    /// Used to customize how content inside the month cell is rendered. 
    /// </summary>
    [Parameter] public RenderFragment<DateTimeOffset>? MonthCellTemplate { get; set; }

    /// <summary>
    /// The title of the month picker's toggle (tooltip).
    /// </summary>
    [Parameter] public string MonthPickerToggleTitle { get; set; } = "{0}, change month";

    /// <summary>
    /// Show month picker on top of date picker when visible.
    /// </summary>
    [Parameter] public bool ShowMonthPickerAsOverlay { get; set; }

    /// <summary>
    /// Callback for when the user selects a date.
    /// </summary>
    [Parameter] public EventCallback<DateTimeOffset?> OnSelectDate { get; set; }

    /// <summary>
    /// The text of selected date aria-atomic of the calendar.
    /// </summary>
    [Parameter] public string SelectedDateAriaAtomic { get; set; } = "Selected date {0}";

    /// <summary>
    /// Whether the GoToToday button should be shown or not.
    /// </summary>
    [Parameter] public bool ShowGoToToday { get; set; } = true;

    /// <summary>
    /// Whether the GoToNow button should be shown or not.
    /// </summary>
    [Parameter] public bool ShowGoToNow { get; set; } = true;

    /// <summary>
    /// Whether the time picker should be shown or not.
    /// </summary>
    [Parameter]
    [CallOnSet(nameof(OnSetParameters))]
    public bool ShowTimePicker { get; set; }

    /// <summary>
    /// The time format of the time-picker, 24H or 12H.
    /// </summary>
    [Parameter] public BitTimeFormat TimeFormat { get; set; }

    /// <summary>
    /// Whether the week number (weeks 1 to 53) should be shown before each week row.
    /// </summary>
    [Parameter] public bool ShowWeekNumbers { get; set; }

    /// <summary>
    /// Custom CSS styles for different parts of the BitCalendar component.
    /// </summary>
    [Parameter] public BitCalendarClassStyles? Styles { get; set; }

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

    /// <summary>
    /// Show month picker on top of date picker when visible.
    /// </summary>
    [Parameter] public bool ShowTimePickerAsOverlay { get; set; }

    /// <summary>
    /// Determines increment/decrement steps for calendar's hour.
    /// </summary>
    [Parameter] public int HourStep { get; set; } = 1;

    /// <summary>
    /// Determines increment/decrement steps for calendar's minute.
    /// </summary>
    [Parameter] public int MinuteStep { get; set; } = 1;

    /// <summary>
    /// Specifies the date and time of the calendar when it is showing without any selected value.
    /// </summary>
    [Parameter]
    [CallOnSet(nameof(OnSetParameters))]
    public DateTimeOffset? StartingValue { get; set; }



    protected override string RootElementClass { get; } = "bit-cal";

    protected override void RegisterCssClasses()
    {
        ClassBuilder.Register(() => Classes?.Root);

        ClassBuilder.Register(() => (Dir is null && _culture.TextInfo.IsRightToLeft) ? "bit-rtl" : string.Empty);
    }

    protected override void RegisterCssStyles()
    {
        StyleBuilder.Register(() => Styles?.Root);
    }

    protected override void OnInitialized()
    {
        OnValueChanged += HandleOnValueChanged;

        OnSetParameters();

        base.OnInitialized();
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

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _cancellationTokenSource?.Dispose();
            OnValueChanged -= HandleOnValueChanged;
        }

        base.Dispose(disposing);
    }



    private void HandleOnValueChanged(object? sender, EventArgs args)
    {
        OnSetParameters();
    }

    private void OnSetParameters()
    {
        _showTimePicker = ShowTimePicker && ShowTimePickerAsOverlay is false;
        _showMonthPicker = _showTimePicker is false && ShowMonthPicker && ShowMonthPickerAsOverlay is false;

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
    }

    private void SelectDate(int dayIndex, int weekIndex)
    {
        if (IsEnabled is false || InvalidValueBinding()) return;
        if (IsWeekDayOutOfMinAndMaxDate(dayIndex, weekIndex)) return;

        _currentDay = _daysOfCurrentMonth[weekIndex, dayIndex];
        var selectedMonth = FindMonth(weekIndex, dayIndex);
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

        _currentMonth = selectedMonth;

        var currentDateTime = _culture.Calendar.ToDateTime(_currentYear, _currentMonth, _currentDay, _hour, _minute, 0, 0);
        CurrentValue = new DateTimeOffset(currentDateTime, DateTimeOffset.Now.Offset);

        GenerateMonthData(_currentYear, _currentMonth);

        _ = OnSelectDate.InvokeAsync(CurrentValue);
    }

    private void SelectMonth(int month)
    {
        if (IsEnabled is false) return;
        if (IsMonthOutOfMinAndMaxDate(month)) return;

        _currentMonth = month;

        GenerateMonthData(_currentYear, _currentMonth);

        if (ShowMonthPickerAsOverlay || (ShowTimePicker && ShowTimePickerAsOverlay is false))
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

        _showYearPicker = !_showYearPicker;
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

        UpdateTime();
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
        var year = _currentYear;
        var month = FindMonth(weekIndex, 0);

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
        _showMonthPicker = !_showMonthPicker;
    }

    private void ToggleTimePickerOverlay()
    {
        _showTimePicker = !_showTimePicker;
    }

    private bool CanChangeMonth(bool isNext)
    {
        if (IsEnabled is false) return false;

        if (isNext && MaxDate.HasValue)
        {
            var maxDateYear = _culture.Calendar.GetYear(MaxDate.Value.DateTime);
            var maxDateMonth = _culture.Calendar.GetMonth(MaxDate.Value.DateTime);

            if (maxDateYear == _currentYear && maxDateMonth == _currentMonth) return false;
        }


        if (isNext is false && MinDate.HasValue)
        {
            var minDateYear = _culture.Calendar.GetYear(MinDate.Value.DateTime);
            var minDateMonth = _culture.Calendar.GetMonth(MinDate.Value.DateTime);

            if (minDateYear == _currentYear && minDateMonth == _currentMonth) return false;
        }

        return true;
    }

    private bool CanChangeYear(bool isNext)
    {
        if (IsEnabled is false) return false;

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
            var maxDateYear = _culture.Calendar.GetYear(MaxDate.Value.DateTime);
            var maxDateMonth = _culture.Calendar.GetMonth(MaxDate.Value.DateTime);
            var maxDateDay = _culture.Calendar.GetDayOfMonth(MaxDate.Value.DateTime);

            if (_currentYear > maxDateYear ||
                (_currentYear == maxDateYear && month > maxDateMonth) ||
                (_currentYear == maxDateYear && month == maxDateMonth && day > maxDateDay)) return true;
        }

        if (MinDate.HasValue)
        {
            var minDateYear = _culture.Calendar.GetYear(MinDate.Value.DateTime);
            var minDateMonth = _culture.Calendar.GetMonth(MinDate.Value.DateTime);
            var minDateDay = _culture.Calendar.GetDayOfMonth(MinDate.Value.DateTime);

            if (_currentYear < minDateYear ||
                (_currentYear == minDateYear && month < minDateMonth) ||
                (_currentYear == minDateYear && month == minDateMonth && day < minDateDay)) return true;
        }

        return false;
    }

    private bool IsMonthOutOfMinAndMaxDate(int month)
    {
        if (MaxDate.HasValue)
        {
            var maxDateYear = _culture.Calendar.GetYear(MaxDate.Value.DateTime);
            var maxDateMonth = _culture.Calendar.GetMonth(MaxDate.Value.DateTime);

            if (_currentYear > maxDateYear || (_currentYear == maxDateYear && month > maxDateMonth)) return true;
        }

        if (MinDate.HasValue)
        {
            var minDateYear = _culture.Calendar.GetYear(MinDate.Value.DateTime);
            var minDateMonth = _culture.Calendar.GetMonth(MinDate.Value.DateTime);

            if (_currentYear < minDateYear || (_currentYear == minDateYear && month < minDateMonth)) return true;
        }

        return false;
    }

    private bool IsYearOutOfMinAndMaxDate(int year)
    {
        return (MaxDate.HasValue && year > _culture.Calendar.GetYear(MaxDate.Value.DateTime))
            || (MinDate.HasValue && year < _culture.Calendar.GetYear(MinDate.Value.DateTime));
    }

    private (string style, string klass) GetDayButtonCss(int day, int week, int todayYear, int todayMonth, int todayDay)
    {
        StringBuilder klass = new StringBuilder();
        StringBuilder style = new StringBuilder();

        if (week == _selectedDateWeek && day == _selectedDateDayOfWeek)
        {
            klass.Append(" bit-cal-dbs");

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
            klass.Append(" bit-cal-dbo");
        }

        var currentDay = _daysOfCurrentMonth[week, day];
        if (IsInCurrentMonth(week, day) && todayYear == _currentYear && todayMonth == _currentMonth && todayDay == currentDay)
        {
            klass.Append(" bit-cal-dtd");

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
            className.Append(" bit-cal-pcm");
        }

        if (HighlightSelectedMonth && _currentMonth == monthIndex)
        {
            className.Append(" bit-cal-psm");
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

    private void UpdateTime()
    {
        if (CurrentValue.HasValue is false) return;

        var currentValueYear = _culture.Calendar.GetYear(CurrentValue.Value.LocalDateTime);
        var currentValueMonth = _culture.Calendar.GetMonth(CurrentValue.Value.LocalDateTime);
        var currentValueDay = _culture.Calendar.GetDayOfMonth(CurrentValue.Value.LocalDateTime);

        CurrentValue = new DateTimeOffset(_culture.Calendar.ToDateTime(currentValueYear, currentValueMonth, currentValueDay, _hour, _minute, 0, 0), DateTimeOffset.Now.Offset);
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

    private void HandleOnAmClick()
    {
        _hour %= 12;  // "12:-- am" is "00:--" in 24h
        UpdateTime();
    }

    private void HandleOnPmClick()
    {
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
        if (IsEnabled is false) return;

        ChangeTime(isNext, isHour);
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

        ChangeTime(isNext, isHour);

        StateHasChanged();

        await Task.Delay(75);
        await ContinuousChangeTime(isNext, isHour, cts);
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
        _cancellationTokenSource?.Cancel();
        _cancellationTokenSource?.Dispose();
        _cancellationTokenSource = new();
    }

    private void ChangeHour(bool isNext)
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

        UpdateTime();
    }

    private void ChangeMinute(bool isNext)
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

        UpdateTime();
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
}
