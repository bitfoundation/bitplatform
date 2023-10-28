using System.Text;
using System.Globalization;
using System.Diagnostics.CodeAnalysis;

namespace Bit.BlazorUI;

public partial class BitCalendar
{
    private const int DEFAULT_DAY_COUNT_PER_WEEK = 7;
    private const int DEFAULT_WEEK_COUNT = 6;



    private CultureInfo culture = CultureInfo.CurrentUICulture;
    private bool isMonthPickerVisible = true;

    private int timeHour;
    private int _timeHour
    {
        get => timeHour;
        set
        {
            timeHour = value > 23 ? 23
                : value < 0 ? 0
                : value;

            UpdateTime();
        }
    }

    private int timeMinute;
    private int _timeMinute
    {
        get => timeMinute;
        set
        {
            timeMinute = value > 59 ? 59
                : value < 0 ? 0
                : value;

            UpdateTime();
        }
    }



    private int _currentDay;
    private int _currentYear;
    private int _displayYear;
    private int _yearPickerEndYear;
    private int _currentMonth;
    private int _yearPickerStartYear;
    private bool _showYearPicker;
    private bool _showMonthPicker;
    private int? _selectedDateWeek;
    private int? _selectedDateDayOfWeek;
    private string? _activeDescendantId;
    private string _monthTitle = string.Empty;
    private ElementReference _inputTimeHourRef = default!;
    private ElementReference _inputTimeMinuteRef = default!;
    private int[,] _daysOfCurrentMonth = new int[DEFAULT_WEEK_COUNT, DEFAULT_DAY_COUNT_PER_WEEK];



    [Inject] private IJSRuntime _js { get; set; } = default!;



    /// <summary>
    /// Custom CSS classes for different parts of the BitCalendar component.
    /// </summary>
    [Parameter] public BitCalendarClassStyles? Classes { get; set; }

    /// <summary>
    /// CultureInfo for the Calendar.
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
    /// The title of the Go to previous month button (tooltip).
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
    public bool IsMonthPickerVisible
    {
        get => isMonthPickerVisible;
        set
        {
            isMonthPickerVisible = value;

            if (value is false)
            {
                _showMonthPicker = false;
            }
        }
    }

    /// <summary>
    /// The maximum allowable date of the calendar.
    /// </summary>
    [Parameter] public DateTimeOffset? MaxDate { get; set; }

    /// <summary>
    /// The minimum allowable date of the calendar.
    /// </summary>
    [Parameter] public DateTimeOffset? MinDate { get; set; }

    /// <summary>
    /// Used to customize how content inside the month cell is rendered. 
    /// </summary>
    [Parameter] public RenderFragment<DateTimeOffset>? MonthCellTemplate { get; set; }

    /// <summary>
    /// The title of the month picker's toggle.
    /// </summary>
    [Parameter] public string MonthPickerToggleTitle { get; set; } = "{0}, change month";

    /// <summary>
    /// Used to set the month picker position. 
    /// </summary>
    [Parameter] public BitCalendarMonthPickerPosition MonthPickerPosition { get; set; } = BitCalendarMonthPickerPosition.Besides;

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
    /// Whether the time picker should be shown or not.
    /// </summary>
    [Parameter] public bool ShowTimePicker { get; set; }

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
    /// The title of the year picker's toggle.
    /// </summary>
    [Parameter] public string YearPickerToggleTitle { get; set; } = "{0}, change year";

    /// <summary>
    /// The title of the year range picker's toggle.
    /// </summary>
    [Parameter] public string YearRangePickerToggleTitle { get; set; } = "{0} - {1}, change month";


    protected override string RootElementClass { get; } = "bit-cal";

    protected override void RegisterCssClasses()
    {
        ClassBuilder.Register(() => Classes?.Root);

        ClassBuilder.Register(() => Culture.TextInfo.IsRightToLeft ? $"{RootElementClass}-rtl" : string.Empty);
    }

    protected override void RegisterCssStyles()
    {
        StyleBuilder.Register(() => Styles?.Root);
    }

    protected override void OnInitialized()
    {
        _activeDescendantId = $"BitCalendar-{UniqueId}-active-descendant";

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

        timeHour = CurrentValue.HasValue ? CurrentValue.Value.Hour : 0;
        timeMinute = CurrentValue.HasValue ? CurrentValue.Value.Minute : 0;

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

    protected override string? FormatValueAsString(DateTimeOffset? value) =>
        value.HasValue ? value.Value.ToString(DateFormat ?? Culture.DateTimeFormat.ShortDatePattern, Culture) : null;



    private void SelectDate(int dayIndex, int weekIndex)
    {
        if (IsEnabled is false) return;
        if (ValueHasBeenSet && ValueChanged.HasDelegate is false) return;
        if (IsWeekDayOutOfMinAndMaxDate(dayIndex, weekIndex)) return;

        _currentDay = _daysOfCurrentMonth[weekIndex, dayIndex];
        var selectedMonth = FindMonth(weekIndex, dayIndex);
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

        var currentDateTime = Culture.Calendar.ToDateTime(_currentYear, _currentMonth, _currentDay, _timeHour, _timeMinute, 0, 0);
        CurrentValue = new DateTimeOffset(currentDateTime, DateTimeOffset.Now.Offset);

        GenerateMonthData(_currentYear, _currentMonth);

        _ = OnSelectDate.InvokeAsync(CurrentValue);
    }

    private void SelectMonth(int month)
    {
        if (IsEnabled is false) return;
        if (IsMonthOutOfMinAndMaxDate(month)) return;

        _currentMonth = month;
        _currentYear = _displayYear;

        GenerateMonthData(_currentYear, _currentMonth);

        if (MonthPickerPosition == BitCalendarMonthPickerPosition.Overlay)
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
        if (MonthPickerPosition == BitCalendarMonthPickerPosition.Overlay)
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
        var date = Culture.Calendar.ToDateTime(year, month, day, 0, 0, 0, 0);

        return Culture.Calendar.GetWeekOfYear(date, CalendarWeekRule.FirstFullWeek, Culture.DateTimeFormat.FirstDayOfWeek);
    }

    private void ToggleMonthPickerOverlay()
    {
        _showMonthPicker = !_showMonthPicker;
    }

    private bool CanChangeMonth(bool isNext)
    {
        if (isNext && MaxDate.HasValue)
        {
            var maxDateYear = Culture.Calendar.GetYear(MaxDate.Value.DateTime);
            var maxDateMonth = Culture.Calendar.GetMonth(MaxDate.Value.DateTime);

            if (maxDateYear == _displayYear && maxDateMonth == _currentMonth) return false;
        }


        if (isNext is false && MinDate.HasValue)
        {
            var minDateYear = Culture.Calendar.GetYear(MinDate.Value.DateTime);
            var minDateMonth = Culture.Calendar.GetMonth(MinDate.Value.DateTime);

            if (minDateYear == _displayYear && minDateMonth == _currentMonth) return false;
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
            var maxDateYear = Culture.Calendar.GetYear(MaxDate.Value.DateTime);
            var maxDateMonth = Culture.Calendar.GetMonth(MaxDate.Value.DateTime);
            var maxDateDay = Culture.Calendar.GetDayOfMonth(MaxDate.Value.DateTime);

            if (_displayYear > maxDateYear ||
                (_displayYear == maxDateYear && month > maxDateMonth) ||
                (_displayYear == maxDateYear && month == maxDateMonth && day > maxDateDay)) return true;
        }

        if (MinDate.HasValue)
        {
            var minDateYear = Culture.Calendar.GetYear(MinDate.Value.DateTime);
            var minDateMonth = Culture.Calendar.GetMonth(MinDate.Value.DateTime);
            var minDateDay = Culture.Calendar.GetDayOfMonth(MinDate.Value.DateTime);

            if (_displayYear < minDateYear ||
                (_displayYear == minDateYear && month < minDateMonth) ||
                (_displayYear == minDateYear && month == minDateMonth && day < minDateDay)) return true;
        }

        return false;
    }

    private bool IsMonthOutOfMinAndMaxDate(int month)
    {
        if (MaxDate.HasValue)
        {
            var maxDateYear = Culture.Calendar.GetYear(MaxDate.Value.DateTime);
            var maxDateMonth = Culture.Calendar.GetMonth(MaxDate.Value.DateTime);

            if (_displayYear > maxDateYear || (_displayYear == maxDateYear && month > maxDateMonth)) return true;
        }

        if (MinDate.HasValue)
        {
            var minDateYear = Culture.Calendar.GetYear(MinDate.Value.DateTime);
            var minDateMonth = Culture.Calendar.GetMonth(MinDate.Value.DateTime);

            if (_displayYear < minDateYear || (_displayYear == minDateYear && month < minDateMonth)) return true;
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

        if (currentValueYear != _currentYear || currentValueMonth != _currentMonth || currentValueDay != _currentDay)
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
        if (todayYear == _currentYear && todayMonth == _currentMonth && todayDay == currentDay)
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
        if (HighlightCurrentMonth && todayMonth == monthIndex && todayYear == _displayYear)
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
}
