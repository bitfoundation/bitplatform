using System.Diagnostics.CodeAnalysis;
using System.Globalization;

namespace Bit.BlazorUI;

public partial class BitDatePicker
{
    private const int DEFAULT_DAY_COUNT_PER_WEEK = 7;
    private const int DEFAULT_WEEK_COUNT = 6;

    private CultureInfo culture = CultureInfo.CurrentUICulture;
    private string focusClass = string.Empty;
    private bool isOpen;

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional
    private int[,] _currentMonthCalendar = new int[DEFAULT_WEEK_COUNT, DEFAULT_DAY_COUNT_PER_WEEK];
#pragma warning restore CA1814 // Prefer jagged arrays over multidimensional
    private int _currentMonth;
    private int _currentYear;
    private int _displayYear;
    private bool _isMonthPickerOverlayOnTop;
    private int _monthLength;
    private string _monthTitle = string.Empty;
    private int? _selectedDateWeek;
    private int? selectedDateDayOfWeek;
    private bool showMonthPicker = true;
    private bool showMonthPickerAsOverlayInternal;
    private int yearRangeFrom;
    private int yearRangeTo;

    [Inject] public IJSRuntime JSRuntime { get; set; } = default!;

    /// <summary>
    /// Whether or not this DatePicker is open
    /// </summary>
    [Parameter]
    public bool IsOpen
    {
        get => isOpen;
        set
        {
            if (isOpen = value) return;

            isOpen = value;
            ClassBuilder.Reset();
        }
    }

    /// <summary>
    /// GoToToday text for the DatePicker
    /// </summary>
    [Parameter] public string GoToToday { get; set; } = "Go to today";

    /// <summary>
    /// Placeholder text for the DatePicker
    /// </summary>
    [Parameter] public string Placeholder { get; set; } = "Select a date...";

    /// <summary>
    /// CultureInfo for the DatePicker
    /// </summary>
    [Parameter]
    public CultureInfo Culture
    {
        get => culture;
        set
        {
            culture = value;
            ClassBuilder.Reset();
        }
    }

    /// <summary>
    /// MaxDate for the DatePicker
    /// </summary>
    [Parameter] public DateTimeOffset? MaxDate { get; set; }

    /// <summary>
    /// MinDate for the DatePicker
    /// </summary>
    [Parameter] public DateTimeOffset? MinDate { get; set; }

    /// <summary>
    /// FormatDate for the DatePicker
    /// </summary>
    [Parameter] public string? FormatDate { get; set; }

    /// <summary>
    /// Callback for when clicking on DatePicker input
    /// </summary>
    [Parameter] public EventCallback<MouseEventArgs> OnClick { get; set; }

    /// <summary>
    /// Callback for when focus moves into the DatePicker input
    /// </summary>
    [Parameter] public EventCallback<FocusEventArgs> OnFocusIn { get; set; }

    /// <summary>
    /// Callback for when focus moves out the DatePicker input
    /// </summary>
    [Parameter] public EventCallback<FocusEventArgs> OnFocusOut { get; set; }

    /// <summary>
    /// Callback for when focus moves into the input
    /// </summary>
    [Parameter] public EventCallback<FocusEventArgs> OnFocus { get; set; }

    /// <summary>
    /// Callback for when the date changes
    /// </summary>
    [Parameter] public EventCallback<DateTimeOffset?> OnSelectDate { get; set; }

    /// <summary>
    /// Label for the DatePicker
    /// </summary>
    [Parameter] public string? Label { get; set; }

    /// <summary>
    /// Shows the custom label for text field
    /// </summary>
    [Parameter] public RenderFragment? LabelFragment { get; set; }

    /// <summary>
    /// Determines if the DatePicker has a border.
    /// </summary>
    [Parameter] public bool HasBorder { get; set; } = true;

    /// <summary>
    /// Whether or not the Textfield of the DatePicker is underlined.
    /// </summary>
    [Parameter] public bool IsUnderlined { get; set; }

    /// <summary>
    /// The tabIndex of the TextField
    /// </summary>
    [Parameter] public int TabIndex { get; set; }

    /// <summary>
    /// Whether the DatePicker allows input a date string directly or not
    /// </summary>
    [Parameter] public bool AllowTextInput { get; set; }

    /// <summary>
    /// Whether the month picker is shown beside the day picker or hidden.
    /// </summary>
    [Parameter] public bool IsMonthPickerVisible { get; set; } = true;

    /// <summary>
    /// Show month picker on top of date picker when visible.
    /// </summary>
    [Parameter] public bool ShowMonthPickerAsOverlay { get; set; }

    /// <summary>
    /// Whether the calendar should show the week number (weeks 1 to 53) before each week row
    /// </summary>
    [Parameter] public bool ShowWeekNumbers { get; set; }

    public string FocusClass
    {
        get => focusClass;
        set
        {
            focusClass = value;
            ClassBuilder.Reset();
        }
    }

    public string CalloutId { get; set; } = string.Empty;
    public string OverlayId { get; set; } = string.Empty;
    public string WrapperId { get; set; } = string.Empty;
    public string MonthAndYearId { get; set; } = Guid.NewGuid().ToString();
    public string ActiveDescendantId { get; set; } = Guid.NewGuid().ToString();
    public string TextFieldId { get; set; } = string.Empty;
    public string LabelId { get; set; } = string.Empty;

    [JSInvokable("CloseCallout")]
    public void CloseCalloutBeforeAnotherCalloutIsOpened()
    {
        IsOpen = false;
    }

    protected override string RootElementClass { get; } = "bit-dtp";

    protected override void RegisterComponentClasses()
    {
        ClassBuilder.Register(() => IsEnabled is false
            ? $"{RootElementClass}-disabled-{VisualClassRegistrar()}" : string.Empty);

        ClassBuilder.Register(() => Culture.TextInfo.IsRightToLeft
            ? $"{RootElementClass}-rtl-{VisualClassRegistrar()}" : string.Empty);

        ClassBuilder.Register(() => IsUnderlined
            ? $"{RootElementClass}-underlined-{(IsEnabled is false ? "disabled-" : string.Empty)}{VisualClassRegistrar()}" : string.Empty);

        ClassBuilder.Register(() => HasBorder is false
            ? $"{RootElementClass}-no-border-{VisualClassRegistrar()}" : string.Empty);

        ClassBuilder.Register(() => FocusClass.HasValue()
            ? $"{RootElementClass}-{(IsUnderlined ? "underlined-" : null)}{FocusClass}-{VisualClassRegistrar()}" : string.Empty);

        ClassBuilder.Register(() => ValueInvalid is true
                                   ? $"{RootElementClass}-invalid-{VisualClassRegistrar()}" : string.Empty);
    }

    protected override Task OnInitializedAsync()
    {
        CalloutId = $"DatePicker-Callout{UniqueId}";
        OverlayId = $"DatePicker-Overlay{UniqueId}";
        WrapperId = $"DatePicker-Wrapper{UniqueId}";
        TextFieldId = $"DatePicker-TextField{UniqueId}";
        LabelId = $"DatePicker-Label{UniqueId}";

        return base.OnInitializedAsync();
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

        CreateMonthCalendar(dateTime);

        return base.OnParametersSetAsync();
    }

    public async Task HandleClick(MouseEventArgs eventArgs)
    {
        if (IsEnabled is false) return;

        showMonthPickerAsOverlayInternal = ShowMonthPickerAsOverlay;

        var obj = DotNetObjectReference.Create(this);

        await JSRuntime.InvokeVoidAsync("BitDatePicker.toggleDatePickerCallout", obj, UniqueId, CalloutId, OverlayId, IsOpen);

        if (showMonthPickerAsOverlayInternal is false)
        {
            showMonthPickerAsOverlayInternal = await JSRuntime.InvokeAsync<bool>("BitDatePicker.checkMonthPickerWidth", CalloutId);
        }

        if (showMonthPickerAsOverlayInternal)
        {
            _isMonthPickerOverlayOnTop = false;
        }

        IsOpen = !IsOpen;

        if (IsOpen && CurrentValue != null)
        {
            CheckCurrentCalendarMatchesCurrentValue();
        }

        _displayYear = _currentYear;
        await OnClick.InvokeAsync(eventArgs);
    }

    public async Task HandleFocusIn(FocusEventArgs eventArgs)
    {
        if (IsEnabled)
        {
            FocusClass = "focused";
            await OnFocusIn.InvokeAsync(eventArgs);
        }
    }

    public async Task HandleFocusOut(FocusEventArgs eventArgs)
    {
        if (IsEnabled)
        {
            FocusClass = string.Empty;
            await OnFocusOut.InvokeAsync(eventArgs);
        }
    }

    private async Task HandleFocus(FocusEventArgs e)
    {
        if (IsEnabled)
        {
            FocusClass = "focused";
            await OnFocus.InvokeAsync(e);
        }
    }

    private async Task HandleChange(ChangeEventArgs e)
    {
        if (IsEnabled is false) return;
        if (ValueHasBeenSet && ValueChanged.HasDelegate is false) return;
        if (AllowTextInput is false) return;

        CurrentValueAsString = e.Value?.ToString();
        await OnSelectDate.InvokeAsync(CurrentValue);
    }

    public async Task SelectDate(int dayIndex, int weekIndex)
    {
        if (IsEnabled is false) return;

        if (ValueHasBeenSet && ValueChanged.HasDelegate is false) return;

        if (CheckDayForMaxAndMinDate(dayIndex, weekIndex)) return;

        var currentDay = _currentMonthCalendar[weekIndex, dayIndex];
        int selectedMonth = GetCorrectTargetMonth(weekIndex, dayIndex);
        if (selectedMonth < _currentMonth && _currentMonth == 12 && IsInCurrentMonth(weekIndex, dayIndex) is false)
        {
            _currentYear++;
        }

        if (selectedMonth > _currentMonth && _currentMonth == 1 && IsInCurrentMonth(weekIndex, dayIndex) is false)
        {
            _currentYear--;
        }

        var obj = DotNetObjectReference.Create(this);
        await JSRuntime.InvokeVoidAsync("BitDatePicker.toggleDatePickerCallout", obj, UniqueId, CalloutId, OverlayId, IsOpen);
        IsOpen = false;
        _displayYear = _currentYear;
        _currentMonth = selectedMonth;
        CurrentValue = new DateTimeOffset(Culture.DateTimeFormat.Calendar.ToDateTime(_currentYear, _currentMonth, currentDay, 0, 0, 0, 0), DateTimeOffset.Now.Offset);
        CreateMonthCalendar(_currentYear, _currentMonth);
        await OnSelectDate.InvokeAsync(CurrentValue);
    }

    public void HandleMonthChange(ChangeDirection direction)
    {
        if (IsEnabled is false) return;
        if (CheckMonthForMaxAndMinDate(direction)) return;

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

    public void SelectMonth(int month)
    {
        if (IsEnabled is false) return;
        if (CheckMonthForMaxAndMinDate(month)) return;

        _currentMonth = month;
        _currentYear = _displayYear;
        CreateMonthCalendar(_currentYear, _currentMonth);
        if (showMonthPickerAsOverlayInternal is false) return;

        ToggleMonthPickerAsOverlay();
    }

    public void SelectYear(int year)
    {
        if (IsEnabled is false) return;
        if (CheckYearForMaxAndMinDate(year)) return;

        _currentYear = _displayYear = year;
        ChangeYearRanges(_currentYear - 1);
        CreateMonthCalendar(_currentYear, _currentMonth);

        ToggleBetweenMonthAndYearPicker();
    }

    public void ToggleBetweenMonthAndYearPicker()
    {
        if (IsEnabled is false) return;

        showMonthPicker = !showMonthPicker;
    }

    public void HandleYearChange(ChangeDirection direction)
    {
        if (IsEnabled is false) return;
        if (CheckYearForMaxAndMinDate(direction)) return;

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

    public void HandleYearRangeChange(ChangeDirection direction)
    {
        if (IsEnabled is false) return;
        if (CheckYearRangeForMaxAndMinDate(direction)) return;

        var fromYear = direction == ChangeDirection.Next ? yearRangeFrom + 12 : yearRangeFrom - 12;

        ChangeYearRanges(fromYear);
    }

    public void HandleGoToToday(MouseEventArgs args)
    {
        if (IsEnabled)
        {
            CreateMonthCalendar(DateTime.Now);
        }
    }

    private void CreateMonthCalendar(DateTime dateTime)
    {
        _currentMonth = Culture.DateTimeFormat.Calendar.GetMonth(dateTime);
        _currentYear = Culture.DateTimeFormat.Calendar.GetYear(dateTime);
        _displayYear = _currentYear;
        yearRangeFrom = _currentYear - 1;
        yearRangeTo = _currentYear + 10;
        CreateMonthCalendar(_currentYear, _currentMonth);
    }

    private void CreateMonthCalendar(int year, int month)
    {
        _monthTitle = $"{Culture.DateTimeFormat.GetMonthName(month)} {year}";
        _monthLength = Culture.DateTimeFormat.Calendar.GetDaysInMonth(year, month);
        var firstDay = Culture.DateTimeFormat.Calendar.ToDateTime(year, month, 1, 0, 0, 0, 0);
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
                        previousMonthDaysCount = Culture.DateTimeFormat.Calendar.GetDaysInMonth(year - 1, previousMonth);
                    }
                    else
                    {
                        previousMonth = month - 1;
                        previousMonthDaysCount = Culture.DateTimeFormat.Calendar.GetDaysInMonth(year, previousMonth);
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

        if (CurrentValue.HasValue is false || (_selectedDateWeek.HasValue && selectedDateDayOfWeek.HasValue)) return;

        var year = Culture.DateTimeFormat.Calendar.GetYear(CurrentValue.Value.DateTime);
        var month = Culture.DateTimeFormat.Calendar.GetMonth(CurrentValue.Value.DateTime);

        if (year == _currentYear && month == _currentMonth)
        {
            var day = Culture.DateTimeFormat.Calendar.GetDayOfMonth(CurrentValue.Value.DateTime);
            var firstDayOfWeek = (int)Culture.DateTimeFormat.FirstDayOfWeek;
            var firstDayOfWeekInMonth = (int)Culture.DateTimeFormat.Calendar.ToDateTime(year, month, 1, 0, 0, 0, 0).DayOfWeek;
            var firstDayOfWeekInMonthIndex = (firstDayOfWeekInMonth - firstDayOfWeek + DEFAULT_DAY_COUNT_PER_WEEK) % DEFAULT_DAY_COUNT_PER_WEEK;
            selectedDateDayOfWeek = ((int)CurrentValue.Value.DayOfWeek - firstDayOfWeek + DEFAULT_DAY_COUNT_PER_WEEK) % DEFAULT_DAY_COUNT_PER_WEEK;
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
        selectedDateDayOfWeek = null;
    }

    private void ChangeYearRanges(int fromYear)
    {
        yearRangeFrom = fromYear;
        yearRangeTo = fromYear + 11;
    }

    private async Task CloseCallout()
    {
        var obj = DotNetObjectReference.Create(this);
        await JSRuntime.InvokeVoidAsync("BitDatePicker.toggleDatePickerCallout", obj, UniqueId, CalloutId, OverlayId, IsOpen);
        IsOpen = false;
        StateHasChanged();
    }

    private string GetDateElClass(int day, int week)
    {
        var className = string.Empty;
        var todayYear = Culture.DateTimeFormat.Calendar.GetYear(DateTime.Now);
        var todayMonth = Culture.DateTimeFormat.Calendar.GetMonth(DateTime.Now);
        var todayDay = Culture.DateTimeFormat.Calendar.GetDayOfMonth(DateTime.Now);
        var currentDay = _currentMonthCalendar[week, day];

        if (IsInCurrentMonth(week, day) is false)
        {
            className += className.Length == 0 ? "date-cell--outside-month" : " date-cell--outside-month";
        }

        if (IsInCurrentMonth(week, day) && todayYear == _currentYear && todayMonth == _currentMonth && todayDay == currentDay)
        {
            className = "date-cell--today";
        }

        if (week == _selectedDateWeek && day == selectedDateDayOfWeek)
        {
            className += className.Length == 0 ? "date-cell--selected" : " date-cell--selected";
        }

        return className;
    }

    private bool IsInCurrentMonth(int week, int day)
    {
        if ((week == 0 || week == 1) && _currentMonthCalendar[week, day] > 20) return false;
        if ((week == 4 || week == 5) && _currentMonthCalendar[week, day] < 7) return false;
        return true;
    }

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

    private bool IsMonthSelected(int month)
    {
        return month == _currentMonth;
    }

    private bool IsYearSelected(int year)
    {
        return year == _currentYear;
    }

    private bool IsGoTodayDisabeld()
    {
        var todayMonth = Culture.DateTimeFormat.Calendar.GetMonth(DateTime.Now);
        var todayYear = Culture.DateTimeFormat.Calendar.GetYear(DateTime.Now);

        if (showMonthPickerAsOverlayInternal)
        {
            return (yearRangeFrom == todayYear - 1 && yearRangeTo == todayYear + 10 && todayMonth == _currentMonth && todayYear == _currentYear);
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
        var date = Culture.DateTimeFormat.Calendar.ToDateTime(year, month, day, 0, 0, 0, 0);
        return Culture.DateTimeFormat.Calendar.GetWeekOfYear(date, CalendarWeekRule.FirstFullWeek, Culture.DateTimeFormat.FirstDayOfWeek);
    }

    private void ToggleMonthPickerAsOverlay()
    {
        _isMonthPickerOverlayOnTop = !_isMonthPickerOverlayOnTop;
    }

    private int GetValueForComparison(int firstDay)
    {
        var firstDayOfWeek = (int)(Culture.DateTimeFormat.FirstDayOfWeek);

        return firstDay > firstDayOfWeek ? firstDayOfWeek : firstDayOfWeek - 7;
    }

    private bool CheckMonthForMaxAndMinDate(ChangeDirection direction)
    {
        if (direction == ChangeDirection.Next && MaxDate.HasValue && MaxDate.Value.Year == _displayYear && MaxDate.Value.Month == _currentMonth)
            return true;

        if (direction == ChangeDirection.Previous && MinDate.HasValue && MinDate.Value.Year == _displayYear && MinDate.Value.Month == _currentMonth)
            return true;

        return false;
    }

    private bool CheckYearForMaxAndMinDate(ChangeDirection direction)
    {
        if (direction == ChangeDirection.Next && MaxDate.HasValue && MaxDate.Value.Year == _displayYear)
            return true;

        if (direction == ChangeDirection.Previous && MinDate.HasValue && MinDate.Value.Year == _displayYear)
            return true;

        return false;
    }

    private bool CheckYearRangeForMaxAndMinDate(ChangeDirection direction)
    {
        if (direction == ChangeDirection.Next && MaxDate.HasValue && MaxDate.Value.Year < yearRangeFrom + 12)
            return true;

        if (direction == ChangeDirection.Previous && MinDate.HasValue && MinDate.Value.Year >= yearRangeFrom)
            return true;

        return false;
    }

    private bool CheckDayForMaxAndMinDate(int dayIndex, int weekIndex)
    {
        var day = _currentMonthCalendar[weekIndex, dayIndex];
        var month = GetCorrectTargetMonth(weekIndex, dayIndex);

        if (MaxDate.HasValue &&
           (_displayYear > MaxDate.Value.Year ||
           (_displayYear == MaxDate.Value.Year && month > MaxDate.Value.Month) ||
           (_displayYear == MaxDate.Value.Year && month == MaxDate.Value.Month && day > MaxDate.Value.Day)))
            return true;

        if (MinDate.HasValue &&
           (_displayYear < MinDate.Value.Year ||
           (_displayYear == MinDate.Value.Year && month < MinDate.Value.Month) ||
           (_displayYear == MinDate.Value.Year && month == MinDate.Value.Month && day < MinDate.Value.Day)))
            return true;

        return false;
    }

    private bool CheckMonthForMaxAndMinDate(int month)
    {
        if (MaxDate.HasValue &&
           (_displayYear > MaxDate.Value.Year ||
           (_displayYear == MaxDate.Value.Year && month > MaxDate.Value.Month)))
            return true;

        if (MinDate.HasValue &&
           (_displayYear < MinDate.Value.Year ||
           (_displayYear == MinDate.Value.Year && month < MinDate.Value.Month)))
            return true;

        return false;
    }

    private bool CheckYearForMaxAndMinDate(int year)
    {
        if (MaxDate.HasValue && year > MaxDate.Value.Year)
            return true;

        if (MinDate.HasValue && year < MinDate.Value.Year)
            return true;

        return false;
    }

    private void CheckCurrentCalendarMatchesCurrentValue()
    {
        var currentValue = CurrentValue.GetValueOrDefault();
        var currentValueYear = currentValue.Year;
        var currentValueMonth = currentValue.Month;
        if (currentValueYear != _currentYear || currentValueMonth != _currentMonth)
        {
            _currentYear = currentValueYear;
            _currentMonth = currentValueMonth;
            CreateMonthCalendar(_currentYear, _currentMonth);
        }
    }

    /// <inheritdoc />
    protected override bool TryParseValueFromString(string? value, [MaybeNullWhen(false)] out DateTimeOffset? result, [NotNullWhen(false)] out string? validationErrorMessage)
    {
        if (value.HasNoValue())
        {
            result = null;
            validationErrorMessage = null;
            return true;
        }

        if (DateTime.TryParseExact(value, FormatDate ?? Culture.DateTimeFormat.ShortDatePattern, Culture, DateTimeStyles.None, out DateTime parsedValue))
        {
            result = new DateTimeOffset(parsedValue, DateTimeOffset.Now.Offset);
            validationErrorMessage = null;
            return true;
        }

        result = default;
        validationErrorMessage = $"The {DisplayName ?? FieldIdentifier.FieldName} field is not valid.";
        return false;
    }

    protected override string? FormatValueAsString(DateTimeOffset? value)
    {
        if (value.HasValue)
        {
            return value.Value.ToString(FormatDate ?? Culture.DateTimeFormat.ShortDatePattern, Culture);
        }
        else
        {
            return null;
        }
    }
}
