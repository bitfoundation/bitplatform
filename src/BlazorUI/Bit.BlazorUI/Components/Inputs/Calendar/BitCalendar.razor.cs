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



    private int _currentYear;
    private int _currentMonth;
    private bool _showYearPicker;
    private bool _showTimePicker;
    private bool _showMonthPicker;
    private int _yearPickerEndYear;
    private int _yearPickerStartYear;
    private string _monthTitle = string.Empty;
    private ElementReference _inputTimeHourRef = default!;
    private ElementReference _inputTimeMinuteRef = default!;
    private TimeZoneInfo _timeZone = TimeZoneInfo.Local;
    private CultureInfo _culture = CultureInfo.CurrentUICulture;
    private CancellationTokenSource _cancellationTokenSource = new();
    private readonly DateTime?[,] _daysOfCurrentMonth = new DateTime?[DEFAULT_WEEK_COUNT, DEFAULT_DAY_COUNT_PER_WEEK];



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
    /// TimeZone for the DatePicker.
    /// </summary>
    [Parameter]
    [CallOnSet(nameof(OnSetParameters))]
    public TimeZoneInfo? TimeZone { get; set; }

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



    private void HandleOnValueChanged(object? sender, EventArgs args)
    {
        OnSetParameters();
    }

    private void OnSetParameters()
    {
        _showTimePicker = ShowTimePicker && ShowTimePickerAsOverlay is false;
        _showMonthPicker = _showTimePicker is false && ShowMonthPicker && ShowMonthPickerAsOverlay is false;

        _timeZone = TimeZone ?? TimeZoneInfo.Local;
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

    private async Task SelectDate(DateTime selectedDate)
    {
        if (ReadOnly) return;
        if (IsEnabled is false || InvalidValueBinding()) return;
        if (IsWeekDayOutOfMinAndMaxDate(selectedDate)) return;

        selectedDate = selectedDate.AddHours(_hour);
        selectedDate = selectedDate.AddMinutes(_minute);

        CurrentValue = new DateTimeOffset(selectedDate, _timeZone.GetUtcOffset(selectedDate));

        _currentMonth = _culture.Calendar.GetMonth(selectedDate);

        GenerateMonthData(_currentYear, _currentMonth);

        await OnSelectDate.InvokeAsync(CurrentValue);
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
        _monthTitle = $"{_culture.DateTimeFormat.GetMonthName(month)} {year}";

        var calendar = _culture.Calendar;
        var firstDayOfMonth = new DateTime(year, month, 1, calendar);
        int daysInMonth = calendar.GetDaysInMonth(year, month);
        int dayOfWeek = (int)calendar.GetDayOfWeek(firstDayOfMonth);
        int firstDayOfWeek = (int)_culture.DateTimeFormat.FirstDayOfWeek;

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

        for (int i = 0; i < 1; i++)
        {
            for (int j = 0; j < dayOfWeek; j++)
            {
                _daysOfCurrentMonth[i, j] = new(previousMonth.Year, previousMonth.Month, day, calendar);
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
                    if (j == 0)
                    {
                        ended = true;
                    }
                    _daysOfCurrentMonth[i, j] = ended ? null : new(nextMonth.Year, nextMonth.Month, day - daysInMonth, calendar);
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
        return _culture.Calendar.GetWeekOfYear(_daysOfCurrentMonth[weekIndex, 0]!.Value, CalendarWeekRule.FirstFullWeek, _culture.DateTimeFormat.FirstDayOfWeek);
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

    private bool IsWeekDayOutOfMinAndMaxDate(DateTime date)
    {
        if (MaxDate.HasValue)
        {
            if (date > MaxDate.Value.LocalDateTime.Date) return true;
        }

        if (MinDate.HasValue)
        {
            if (date < MinDate.Value.LocalDateTime.Date) return true;
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

    private (string style, string klass) GetDayButtonCss(DateTime date)
    {
        StringBuilder klass = new StringBuilder();
        StringBuilder style = new StringBuilder();

        if (CurrentValue.HasValue && date == CurrentValue.Value.LocalDateTime.Date)
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

        var month = _culture.Calendar.GetMonth(date);

        //Isn't in current month
        if (month != _currentMonth)
        {
            klass.Append(" bit-cal-dbo");
        }

        //Is today
        if (month == _currentMonth && date == DateTimeOffset.Now.LocalDateTime.Date)
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

        return date == CurrentValue.Value.LocalDateTime.Date;
    }

    private void UpdateTime()
    {
        if (CurrentValue.HasValue is false) return;

        var currentValueYear = _culture.Calendar.GetYear(CurrentValue.Value.LocalDateTime);
        var currentValueMonth = _culture.Calendar.GetMonth(CurrentValue.Value.LocalDateTime);
        var currentValueDay = _culture.Calendar.GetDayOfMonth(CurrentValue.Value.LocalDateTime.Date);

        var date = _culture.Calendar.ToDateTime(currentValueYear, currentValueMonth, currentValueDay, _hour, _minute, 0, 0);
        CurrentValue = new(date, _timeZone.GetUtcOffset(date));
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



    protected override async ValueTask DisposeAsync(bool disposing)
    {
        if (IsDisposed || disposing is false) return;

        _cancellationTokenSource?.Dispose();
        OnValueChanged -= HandleOnValueChanged;

        await base.DisposeAsync(disposing);
    }
}
