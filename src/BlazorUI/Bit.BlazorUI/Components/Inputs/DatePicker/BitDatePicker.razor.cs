using System.Text;
using System.Globalization;
using System.Diagnostics.CodeAnalysis;

namespace Bit.BlazorUI;

public partial class BitDatePicker
{
    private const int DEFAULT_DAY_COUNT_PER_WEEK = 7;
    private const int DEFAULT_WEEK_COUNT = 6;

    private BitIconLocation iconLocation = BitIconLocation.Right;
    private bool isOpen;
    private CultureInfo culture = CultureInfo.CurrentUICulture;

    private string focusClass = string.Empty;
    private string _focusClass
    {
        get => focusClass;
        set
        {
            focusClass = value;
            ClassBuilder.Reset();
        }
    }

    private bool _isMonthPickerOverlayOnTop;
    private bool _showMonthPicker = true;
    private bool _showMonthPickerAsOverlayInternal;
    private int[,] _currentMonthCalendar = new int[DEFAULT_WEEK_COUNT, DEFAULT_DAY_COUNT_PER_WEEK];
    private int _currentDay;
    private int _currentMonth;
    private int _currentYear;
    private int _displayYear;
    private int _monthLength;
    private int? _selectedDateWeek;
    private int? _selectedDateDayOfWeek;
    private int _yearRangeFrom;
    private int _yearRangeTo;
    private string _monthTitle = string.Empty;
    private DotNetObjectReference<BitDatePicker> _dotnetObj = default!;
    private string? _labelId;
    private string? _calloutId;
    private string? _overlayId;
    private string? _wrapperId;
    private string? _textFieldId;
    private string? _monthTitleId;
    private string? _activeDescendantId;
    private ElementReference _inputTimeHourRef = default!;
    private ElementReference _inputTimeMinuteRef = default!;
    private int _timeHour;
    private int _timeMinute;

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

    [Inject] private IJSRuntime _js { get; set; } = default!;

    /// <summary>
    /// Whether the DatePicker allows input a date string directly or not
    /// </summary>
    [Parameter] public bool AllowTextInput { get; set; }

    /// <summary>
    /// Capture and render additional attributes in addition to the main callout's parameters
    /// </summary>
    [Parameter] public Dictionary<string, object> CalloutHtmlAttributes { get; set; } = new Dictionary<string, object>();

    /// <summary>
    /// CultureInfo for the DatePicker
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
    /// The format of the date in the DatePicker
    /// </summary>
    [Parameter] public string? DateFormat { get; set; }

    /// <summary>
    /// Used to customize how content inside the day cell is rendered.
    /// </summary>
    [Parameter] public RenderFragment<DateTimeOffset>? DayCellTemplate { get; set; }

    /// <summary>
    /// GoToToday text for the DatePicker
    /// </summary>
    [Parameter] public string GoToToday { get; set; } = "Go to today";

    /// <summary>
    /// The title of the Go to previous month button
    /// </summary>
    [Parameter] public string GoToPrevMonthTitle { get; set; } = "Go to previous month";

    /// <summary>
    /// The title of the Go to next month button
    /// </summary>
    [Parameter] public string GoToNextMonthTitle { get; set; } = "Go to next month";

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
    /// Custom DatePicker icon template
    /// </summary>
    [Parameter] public RenderFragment? IconTemplate { get; set; }

    /// <summary>
    /// DatePicker icon location
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
    /// Optional DatePicker icon
    /// </summary>
    [Parameter] public string IconName { get; set; } = "CalendarMirrored";

    /// <summary>
    /// The custom validation error message for the invalid value.
    /// </summary>
    [Parameter] public string? InvalidErrorMessage { get; set; }

    /// <summary>
    /// Whether the month picker is shown beside the day picker or hidden.
    /// </summary>
    [Parameter] public bool IsMonthPickerVisible { get; set; } = true;

    /// <summary>
    /// Whether or not this DatePicker is open
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

    /// <summary>
    /// Enables the responsive mode in small screens
    /// </summary>
    [Parameter] public bool IsResponsive { get; set; }

    /// <summary>
    /// Whether or not the Text field of the DatePicker is underlined.
    /// </summary>
    [Parameter] public bool IsUnderlined { get; set; }

    /// <summary>
    /// Label for the DatePicker
    /// </summary>
    [Parameter] public string? Label { get; set; }

    /// <summary>
    /// Shows the custom label for text field
    /// </summary>
    [Parameter] public RenderFragment? LabelTemplate { get; set; }

    /// <summary>
    /// MaxDate for the DatePicker
    /// </summary>
    [Parameter] public DateTimeOffset? MaxDate { get; set; }

    /// <summary>
    /// MinDate for the DatePicker
    /// </summary>
    [Parameter] public DateTimeOffset? MinDate { get; set; }

    /// <summary>
    /// Used to customize how content inside the month cell is rendered. 
    /// </summary>
    [Parameter] public RenderFragment<DateTimeOffset>? MonthCellTemplate { get; set; }

    /// <summary>
    /// Callback for when clicking on DatePicker input
    /// </summary>
    [Parameter] public EventCallback OnClick { get; set; }

    /// <summary>
    /// Callback for when focus moves into the input.
    /// </summary>
    [Parameter] public EventCallback OnFocus { get; set; }

    /// <summary>
    /// Callback for when focus moves into the DatePicker input.
    /// </summary>
    [Parameter] public EventCallback OnFocusIn { get; set; }

    /// <summary>
    /// Callback for when focus moves out the DatePicker input.
    /// </summary>
    [Parameter] public EventCallback OnFocusOut { get; set; }

    /// <summary>
    /// Callback for when the date changes.
    /// </summary>
    [Parameter] public EventCallback<DateTimeOffset?> OnSelectDate { get; set; }

    /// <summary>
    /// Aria label for date picker popup for screen reader users.
    /// </summary>
    [Parameter] public string PickerAriaLabel { get; set; } = "Calendar";

    /// <summary>
    /// Placeholder text for the DatePicker.
    /// </summary>
    [Parameter] public string Placeholder { get; set; } = string.Empty;

    /// <summary>
    /// Whether the date picker close button should be shown or not.
    /// </summary>
    [Parameter] public bool ShowCloseButton { get; set; }

    /// <summary>
    /// Whether the "Go to today" link should be shown or not.
    /// </summary>
    [Parameter] public bool ShowGoToToday { get; set; } = true;

    /// <summary>
    /// Show month picker on top of date picker when visible.
    /// </summary>
    [Parameter] public bool ShowMonthPickerAsOverlay { get; set; }

    /// <summary>
    /// Whether the calendar should show the week number (weeks 1 to 53) before each week row.
    /// </summary>
    [Parameter] public bool ShowWeekNumbers { get; set; }

    /// <summary>
    /// The tabIndex of the TextField.
    /// </summary>
    [Parameter] public int TabIndex { get; set; }

    /// <summary>
    /// Used to customize how content inside the year cell is rendered.
    /// </summary>
    [Parameter] public RenderFragment<int>? YearCellTemplate { get; set; }

    /// <summary>
    /// Show time picker for select times.
    /// </summary>
    [Parameter] public bool ShowTimePicker { get; set; }

    /// <summary>
    /// Time format of the time pickers, 24H or 12H.
    /// </summary>
    [Parameter] public BitTimeFormat TimeFormat { get; set; }


    protected override string RootElementClass { get; } = "bit-dtp";

    protected override Task OnInitializedAsync()
    {
        _dotnetObj = DotNetObjectReference.Create(this);

        _labelId = $"BitDatePicker-{UniqueId}-label";
        _calloutId = $"BitDatePicker-{UniqueId}-callout";
        _overlayId = $"BitDatePicker-{UniqueId}-overlay";
        _wrapperId = $"BitDatePicker-{UniqueId}-wrapper";
        _textFieldId = $"BitDatePicker-{UniqueId}-text-field";
        _monthTitleId = $"BitDatePicker-{UniqueId}-month-title";
        _activeDescendantId = $"BitDatePicker-{UniqueId}-active-descendant";

        return base.OnInitializedAsync();
    }

    protected override void RegisterCssClasses()
    {
        ClassBuilder.Register(() => Culture.TextInfo.IsRightToLeft ? $"{RootElementClass}-rtl" : string.Empty);

        ClassBuilder.Register(() => IconLocation is BitIconLocation.Left ? $"{RootElementClass}-lfic" : string.Empty);

        ClassBuilder.Register(() => IsUnderlined ? $"{RootElementClass}-und" : string.Empty);

        ClassBuilder.Register(() => HasBorder is false ? $"{RootElementClass}-no-brd" : string.Empty);

        ClassBuilder.Register(() => _focusClass);
    }

    protected override Task OnParametersSetAsync()
    {
        var dateTime = CurrentValue.GetValueOrDefault(DateTimeOffset.Now).DateTime;

        if (MinDate.HasValue && MinDate > new DateTimeOffset(dateTime))
        {
            dateTime = MinDate.GetValueOrDefault(DateTimeOffset.Now).DateTime;
        }

        if (MaxDate.HasValue && MaxDate < new DateTimeOffset(dateTime))
        {
            dateTime = MaxDate.GetValueOrDefault(DateTimeOffset.Now).DateTime;
        }

        _timeHour = CurrentValue.HasValue ? CurrentValue.Value.Hour : 0;
        _timeMinute = CurrentValue.HasValue ? CurrentValue.Value.Minute : 0;

        CreateMonthCalendar(dateTime);

        return base.OnParametersSetAsync();
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

    public Task OpenCallout() => HandleOnClick();

    private async Task HandleOnClick()
    {
        if (IsEnabled is false) return;

        _showMonthPickerAsOverlayInternal = ShowMonthPickerAsOverlay;

        await _js.InvokeVoidAsync("BitDatePicker.toggleDatePickerCallout", _dotnetObj, _Id, _calloutId, _overlayId, IsOpen);

        if (_showMonthPickerAsOverlayInternal is false)
        {
            _showMonthPickerAsOverlayInternal = await _js.InvokeAsync<bool>("BitDatePicker.checkMonthPickerWidth", _Id, _calloutId, IsResponsive);
        }

        if (_showMonthPickerAsOverlayInternal)
        {
            _isMonthPickerOverlayOnTop = false;
        }

        IsOpen = !IsOpen;

        if (IsOpen && CurrentValue.HasValue)
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
        if (IsWeekDayOutOfMinAndMaxDate(dayIndex, weekIndex)) return;

        _currentDay = _currentMonthCalendar[weekIndex, dayIndex];
        int selectedMonth = GetCorrectTargetMonth(weekIndex, dayIndex);
        if (selectedMonth < _currentMonth && _currentMonth == 12 && IsInCurrentMonth(weekIndex, dayIndex) is false)
        {
            _currentYear++;
        }

        if (selectedMonth > _currentMonth && _currentMonth == 1 && IsInCurrentMonth(weekIndex, dayIndex) is false)
        {
            _currentYear--;
        }

        await _js.InvokeVoidAsync("BitDatePicker.toggleDatePickerCallout", _dotnetObj, _Id, _calloutId, _overlayId, IsOpen);
        IsOpen = false;
        _displayYear = _currentYear;
        _currentMonth = selectedMonth;
        CurrentValue = new DateTimeOffset(Culture.Calendar.ToDateTime(_currentYear, _currentMonth, _currentDay, _timeHour, _timeMinute, 0, 0), DateTimeOffset.Now.Offset);
        CreateMonthCalendar(_currentYear, _currentMonth);
        await OnSelectDate.InvokeAsync(CurrentValue);
    }

    private void HandleOnMonthChange(ChangeDirection direction)
    {
        if (IsEnabled is false) return;
        if (CanMonthChange(direction) is false) return;

        if (direction == ChangeDirection.Next)
        {
            if (_currentMonth + 1 == 13)
            {
                _currentYear++;
                _currentMonth = 1;
            }
            else
            {
                _currentMonth++;
            }
        }
        else
        {
            if (_currentMonth - 1 == 0)
            {
                _currentYear--;
                _currentMonth = 12;
            }
            else
            {
                _currentMonth--;
            }
        }

        _displayYear = _currentYear;
        CreateMonthCalendar(_currentYear, _currentMonth);
    }

    private void SelectMonth(int month)
    {
        if (IsEnabled is false) return;
        if (IsMonthOutOfMinAndMaxDate(month)) return;

        _currentMonth = month;
        _currentYear = _displayYear;
        CreateMonthCalendar(_currentYear, _currentMonth);
        if (_showMonthPickerAsOverlayInternal is false) return;

        ToggleMonthPickerAsOverlay();
    }

    private void SelectYear(int year)
    {
        if (IsEnabled is false) return;
        if (IsYearOutOfMinAndMaxDate(year)) return;

        _currentYear = _displayYear = year;
        ChangeYearRanges(_currentYear - 1);
        CreateMonthCalendar(_currentYear, _currentMonth);
        ToggleBetweenMonthAndYearPicker();
    }

    private void ToggleBetweenMonthAndYearPicker()
    {
        if (IsEnabled is false) return;

        _showMonthPicker = !_showMonthPicker;
    }

    private void HandleYearChange(ChangeDirection direction)
    {
        if (IsEnabled is false) return;
        if (CanYearChange(direction) is false) return;

        if (direction == ChangeDirection.Next)
        {
            _displayYear++;
        }
        else
        {
            _displayYear--;
        }

        CreateMonthCalendar(_currentYear, _currentMonth);
    }

    private void HandleOnYearRangeChange(ChangeDirection direction)
    {
        if (IsEnabled is false) return;
        if (CanYearRangeChange(direction) is false) return;

        var fromYear = direction == ChangeDirection.Next ? _yearRangeFrom + 12 : _yearRangeFrom - 12;

        ChangeYearRanges(fromYear);
    }

    private void HandleGoToToday()
    {
        if (IsEnabled is false) return;

        CreateMonthCalendar(DateTime.Now);
    }

    private void CreateMonthCalendar(DateTime dateTime)
    {
        _currentMonth = Culture.Calendar.GetMonth(dateTime);
        _currentYear = Culture.Calendar.GetYear(dateTime);
        _displayYear = _currentYear;
        _yearRangeFrom = _currentYear - 1;
        _yearRangeTo = _currentYear + 10;
        CreateMonthCalendar(_currentYear, _currentMonth);
    }

    private void CreateMonthCalendar(int year, int month)
    {
        _monthTitle = $"{Culture.DateTimeFormat.GetMonthName(month)} {year}";
        _monthLength = Culture.Calendar.GetDaysInMonth(year, month);
        var firstDay = Culture.Calendar.ToDateTime(year, month, 1, 0, 0, 0, 0);
        var currentDay = 1;
        ResetCalendar();

        var isCalendarEnded = false;
        for (int weekIndex = 0; weekIndex < DEFAULT_WEEK_COUNT; weekIndex++)
        {
            for (int dayIndex = 0; dayIndex < DEFAULT_DAY_COUNT_PER_WEEK; dayIndex++)
            {
                if (weekIndex == 0
                    && currentDay == 1
                    && (int)firstDay.DayOfWeek > dayIndex + GetValueForComparison((int)firstDay.DayOfWeek))
                {
                    int previousMonth;
                    int previousMonthDaysCount;
                    if (month - 1 == 0)
                    {
                        previousMonth = 12;
                        previousMonthDaysCount = Culture.Calendar.GetDaysInMonth(year - 1, previousMonth);
                    }
                    else
                    {
                        previousMonth = month - 1;
                        previousMonthDaysCount = Culture.Calendar.GetDaysInMonth(year, previousMonth);
                    }

                    var firstDayOfWeek = (int)(Culture.DateTimeFormat.FirstDayOfWeek);

                    if ((int)firstDay.DayOfWeek > firstDayOfWeek)
                    {
                        _currentMonthCalendar[weekIndex, dayIndex] = previousMonthDaysCount + dayIndex - (int)firstDay.DayOfWeek + 1 + firstDayOfWeek;
                    }
                    else
                    {
                        _currentMonthCalendar[weekIndex, dayIndex] = previousMonthDaysCount + dayIndex - (7 + (int)firstDay.DayOfWeek - 1 - firstDayOfWeek);
                    }
                }
                else if (currentDay <= _monthLength)
                {
                    _currentMonthCalendar[weekIndex, dayIndex] = currentDay;
                    currentDay++;
                }

                if (currentDay > _monthLength)
                {
                    currentDay = 1;
                    isCalendarEnded = true;
                }
            }

            if (isCalendarEnded)
            {
                break;
            }
        }

        SetSelectedDateInMonthCalendar();
    }

    private void SetSelectedDateInMonthCalendar()
    {
        if (Culture is null) return;
        if (CurrentValue.HasValue is false || (_selectedDateWeek.HasValue && _selectedDateDayOfWeek.HasValue)) return;

        var year = Culture.Calendar.GetYear(CurrentValue.Value.DateTime);
        var month = Culture.Calendar.GetMonth(CurrentValue.Value.DateTime);

        if (year == _currentYear && month == _currentMonth)
        {
            var day = Culture.Calendar.GetDayOfMonth(CurrentValue.Value.DateTime);
            var firstDayOfWeek = (int)Culture.DateTimeFormat.FirstDayOfWeek;
            var firstDayOfWeekInMonth = (int)Culture.Calendar.ToDateTime(year, month, 1, 0, 0, 0, 0).DayOfWeek;
            var firstDayOfWeekInMonthIndex = (firstDayOfWeekInMonth - firstDayOfWeek + DEFAULT_DAY_COUNT_PER_WEEK) % DEFAULT_DAY_COUNT_PER_WEEK;
            _selectedDateDayOfWeek = ((int)CurrentValue.Value.DayOfWeek - firstDayOfWeek + DEFAULT_DAY_COUNT_PER_WEEK) % DEFAULT_DAY_COUNT_PER_WEEK;
            var days = firstDayOfWeekInMonthIndex + day;
            _selectedDateWeek = days % DEFAULT_DAY_COUNT_PER_WEEK == 0 ? (days / DEFAULT_DAY_COUNT_PER_WEEK) - 1 : days / DEFAULT_DAY_COUNT_PER_WEEK;
            if (firstDayOfWeekInMonthIndex is 0)
            {
                _selectedDateWeek++;
            }
        }
    }

    private void ResetCalendar()
    {
        for (int weekIndex = 0; weekIndex < DEFAULT_WEEK_COUNT; weekIndex++)
        {
            for (int dayIndex = 0; dayIndex < DEFAULT_DAY_COUNT_PER_WEEK; dayIndex++)
            {
                _currentMonthCalendar[weekIndex, dayIndex] = 0;
            }
        }

        _selectedDateWeek = null;
        _selectedDateDayOfWeek = null;
    }

    private void ChangeYearRanges(int fromYear)
    {
        _yearRangeFrom = fromYear;
        _yearRangeTo = fromYear + 11;
    }

    private async Task CloseCallout()
    {
        await _js.InvokeVoidAsync("BitDatePicker.toggleDatePickerCallout", _dotnetObj, _Id, _calloutId, _overlayId, IsOpen);
        IsOpen = false;
        StateHasChanged();
    }

    private string GetDateElClass(int day, int week)
    {
        StringBuilder className = new StringBuilder();
        var todayYear = Culture.Calendar.GetYear(DateTime.Now);
        var todayMonth = Culture.Calendar.GetMonth(DateTime.Now);
        var todayDay = Culture.Calendar.GetDayOfMonth(DateTime.Now);
        var currentDay = _currentMonthCalendar[week, day];

        if (IsInCurrentMonth(week, day) is false)
        {
            className.Append(' ').Append(RootElementClass).Append("-dc-ots-m");
        }
        else if (todayYear == _currentYear && todayMonth == _currentMonth && todayDay == currentDay)
        {
            className.Append(' ').Append(RootElementClass).Append("-dc-tdy");
        }

        if (week == _selectedDateWeek && day == _selectedDateDayOfWeek)
        {
            className.Append(' ').Append(RootElementClass).Append("-dc-sel");
        }

        return className.ToString();
    }

    private bool IsInCurrentMonth(int week, int day) =>
        (((week == 0 || week == 1) && _currentMonthCalendar[week, day] > 20) ||
        ((week == 4 || week == 5) && _currentMonthCalendar[week, day] < 7)) is false;

    private int GetCorrectTargetMonth(int week, int day)
    {
        int month = _currentMonth;
        if (IsInCurrentMonth(week, day) is false)
        {
            if (week >= 4)
            {
                month = _currentMonth + 1 == 13 ? 1 : _currentMonth + 1;
            }
            else
            {
                month = _currentMonth - 1 == 0 ? 12 : _currentMonth - 1;
            }
        }

        return month;
    }

    private string GetDateAriaLabel(int week, int day)
    {
        int month = GetCorrectTargetMonth(week, day);
        int year = _currentYear;
        if (IsInCurrentMonth(week, day) is false)
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

        return $"{_currentMonthCalendar[week, day]}, {Culture.DateTimeFormat.GetMonthName(month)}, {year}";
    }

    private bool IsMonthSelected(int month) => month == _currentMonth;

    private bool IsYearSelected(int year) => year == _currentYear;

    private bool IsGoTodayDisabled()
    {
        var todayMonth = Culture.Calendar.GetMonth(DateTime.Now);
        var todayYear = Culture.Calendar.GetYear(DateTime.Now);

        if (_showMonthPickerAsOverlayInternal)
        {
            return (_yearRangeFrom == todayYear - 1 && _yearRangeTo == todayYear + 10 && todayMonth == _currentMonth && todayYear == _currentYear);
        }
        else
        {
            return (todayMonth == _currentMonth && todayYear == _currentYear);
        }
    }

    private DayOfWeek GetDayOfWeek(int index)
    {
        int dayOfWeek = (int)(Culture.DateTimeFormat.FirstDayOfWeek) + index;
        if (dayOfWeek > 6) dayOfWeek -= 7;
        return (DayOfWeek)dayOfWeek;
    }

    private int GetWeekNumber(int weekIndex)
    {
        int month = GetCorrectTargetMonth(weekIndex, 0);
        int year = _currentYear;
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

        int day = _currentMonthCalendar[weekIndex, 0];
        var date = Culture.Calendar.ToDateTime(year, month, day, 0, 0, 0, 0);
        return Culture.Calendar.GetWeekOfYear(date, CalendarWeekRule.FirstFullWeek, Culture.DateTimeFormat.FirstDayOfWeek);
    }

    private void ToggleMonthPickerAsOverlay()
        => _isMonthPickerOverlayOnTop = _isMonthPickerOverlayOnTop is false;

    private int GetValueForComparison(int firstDay)
    {
        var firstDayOfWeek = (int)(Culture.DateTimeFormat.FirstDayOfWeek);

        return firstDay > firstDayOfWeek ? firstDayOfWeek : firstDayOfWeek - 7;
    }

    private bool CanMonthChange(ChangeDirection direction)
    {
        if (direction == ChangeDirection.Next && MaxDate.HasValue)
        {
            var MaxDateYear = Culture.Calendar.GetYear(MaxDate.Value.DateTime);
            var MaxDateMonth = Culture.Calendar.GetMonth(MaxDate.Value.DateTime);

            if (MaxDateYear == _displayYear && MaxDateMonth == _currentMonth) return false;
        }


        if (direction == ChangeDirection.Previous && MinDate.HasValue)
        {
            var MinDateYear = Culture.Calendar.GetYear(MinDate.Value.DateTime);
            var MinDateMonth = Culture.Calendar.GetMonth(MinDate.Value.DateTime);

            if (MinDateYear == _displayYear && MinDateMonth == _currentMonth) return false;
        }

        return true;
    }

    private bool CanYearChange(ChangeDirection direction) =>
        ((direction == ChangeDirection.Next && MaxDate.HasValue && Culture.Calendar.GetYear(MaxDate.Value.DateTime) == _displayYear) ||
        (direction == ChangeDirection.Previous && MinDate.HasValue && Culture.Calendar.GetYear(MinDate.Value.DateTime) == _displayYear)) is false;

    private bool CanYearRangeChange(ChangeDirection direction) =>
        ((direction == ChangeDirection.Next && MaxDate.HasValue && Culture.Calendar.GetYear(MaxDate.Value.DateTime) < _yearRangeFrom + 12) ||
        (direction == ChangeDirection.Previous && MinDate.HasValue && Culture.Calendar.GetYear(MinDate.Value.DateTime) >= _yearRangeFrom)) is false;

    private bool IsWeekDayOutOfMinAndMaxDate(int dayIndex, int weekIndex)
    {
        var day = _currentMonthCalendar[weekIndex, dayIndex];
        var month = GetCorrectTargetMonth(weekIndex, dayIndex);

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

    private bool IsYearOutOfMinAndMaxDate(int year) =>
        (MaxDate.HasValue && year > Culture.Calendar.GetYear(MaxDate.Value.DateTime)) ||
        (MinDate.HasValue && year < Culture.Calendar.GetYear(MinDate.Value.DateTime));

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
            CreateMonthCalendar(_currentYear, _currentMonth);
        }
    }

    private string GetMonthCellClassName(int monthIndex)
    {
        var className = new StringBuilder();
        if (HighlightCurrentMonth)
        {
            var todayMonth = Culture.Calendar.GetMonth(DateTime.Now);
            if (todayMonth == monthIndex)
            {
                className.Append(RootElementClass).Append("-crtm");
            }
        }

        if (HighlightSelectedMonth && _currentMonth == monthIndex)
        {
            if (className.Length > 0)
            {
                className.Append(' ');
            }
            className.Append(RootElementClass).Append("-selm");
        }

        return className.ToString();
    }

    private DateTimeOffset GetDayCellDate(int dayIndex, int weekIndex)
    {
        int selectedMonth = GetCorrectTargetMonth(weekIndex, dayIndex);
        var currentDay = _currentMonthCalendar[weekIndex, dayIndex];
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

    private DateTimeOffset GetMonthCellDate(int monthIndex)
        => new(Culture.Calendar.ToDateTime(_currentYear, monthIndex, 1, 0, 0, 0, 0), DateTimeOffset.Now.Offset);

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

    private void ToggleTimeAmPm()
    {
        if (IsEnabled is false) return;

        _timeHourView = _timeHour + (_timeHour >= 12 ? -12 : 12);
    }

    [JSInvokable("CloseCallout")]
    public void CloseCalloutBeforeAnotherCalloutIsOpened() => IsOpen = false;

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _dotnetObj.Dispose();
        }

        base.Dispose(disposing);
    }
}
