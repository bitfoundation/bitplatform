using System.Text;
using System.Globalization;
using System.Diagnostics.CodeAnalysis;

namespace Bit.BlazorUI;

public partial class BitDateRangePicker
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

    private BitIconLocation _iconLocation;
    private bool _isMonthPickerOverlayOnTop;
    private bool _showMonthPicker = true;
    private bool _showMonthPickerAsOverlayInternal;
#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional
    private int[,] _currentMonthCalendar = new int[DEFAULT_WEEK_COUNT, DEFAULT_DAY_COUNT_PER_WEEK];
#pragma warning restore CA1814 // Prefer jagged arrays over multidimensional
    private int _currentMonth;
    private int _currentYear;
    private int _displayYear;
    private int _monthLength;
    private int? _selectedEndDateWeek;
    private int? _selectedEndDateDayOfWeek;
    private int? _selectedStartDateWeek;
    private int? _selectedStartDateDayOfWeek;
    private int _yearRangeFrom;
    private int _yearRangeTo;
    private string _monthTitle = string.Empty;
    private DotNetObjectReference<BitDateRangePicker> _dotnetObj = default!;
    private bool _disposed;

    [Inject] public IJSRuntime JSRuntime { get; set; } = default!;


    /// <summary>
    /// Whether the DateRangePicker allows input a date string directly or not
    /// </summary>
    [Parameter] public bool AllowTextInput { get; set; }

    /// <summary>
    /// Whether the DateRangePicker closes automatically after selecting the second value
    /// </summary>
    [Parameter] public bool AutoClose { get; set; } = true;

    /// <summary>
    /// Capture and render additional attributes in addition to the main callout's parameters
    /// </summary>
    [Parameter] public Dictionary<string, object> CalloutHtmlAttributes { get; set; } = new Dictionary<string, object>();

    /// <summary>
    /// CultureInfo for the DateRangePicker
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
    /// Used to customize how content inside the day cell is rendered.
    /// </summary>
    [Parameter] public RenderFragment<DateTimeOffset>? DayCellTemplate { get; set; }

    /// <summary>
    /// DateFormat for the DateRangePicker
    /// </summary>
    [Parameter] public string? DateFormat { get; set; }

    /// <summary>
    /// GoToToday text for the DateRangePicker
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
    /// Custom DateRangePicker icon template
    /// </summary>
    [Parameter] public RenderFragment? IconFragment { get; set; }

    /// <summary>
    /// DateRangePicker icon location
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
    /// Optional DateRangePicker icon
    /// </summary>
    [Parameter] public BitIconName IconName { get; set; } = BitIconName.CalendarMirrored;

    /// <summary>
    /// Whether the month picker is shown beside the day picker or hidden.
    /// </summary>
    [Parameter] public bool IsMonthPickerVisible { get; set; } = true;

    /// <summary>
    /// Whether or not this DateRangePicker is open
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
    /// Whether or not the Textfield of the DateRangePicker is underlined.
    /// </summary>
    [Parameter] public bool IsUnderlined { get; set; }

    /// <summary>
    /// Label for the DateRangePicker
    /// </summary>
    [Parameter] public string? Label { get; set; }

    /// <summary>
    /// Shows the custom label for text field
    /// </summary>
    [Parameter] public RenderFragment? LabelFragment { get; set; }

    /// <summary>
    /// Maximum date for the DateRangePicker
    /// </summary>
    [Parameter] public DateTimeOffset? MaxDate { get; set; }

    /// <summary>
    /// Minimum date for the DateRangePicker
    /// </summary>
    [Parameter] public DateTimeOffset? MinDate { get; set; }

    /// <summary>
    /// Used to customize how content inside the month cell is rendered. 
    /// </summary>
    [Parameter] public RenderFragment<DateTimeOffset>? MonthCellTemplate { get; set; }

    /// <summary>
    /// Callback for when clicking on DateRangePicker input
    /// </summary>
    [Parameter] public EventCallback OnClick { get; set; }

    /// <summary>
    /// Callback for when focus moves into the input.
    /// </summary>
    [Parameter] public EventCallback OnFocus { get; set; }

    /// <summary>
    /// Callback for when focus moves into the DateRangePicker input.
    /// </summary>
    [Parameter] public EventCallback OnFocusIn { get; set; }

    /// <summary>
    /// Callback for when focus moves out the DateRangePicker input.
    /// </summary>
    [Parameter] public EventCallback OnFocusOut { get; set; }

    /// <summary>
    /// Callback for when the date changes.
    /// </summary>
    [Parameter] public EventCallback<BitDateRangePickerValue> OnSelectDate { get; set; }

    /// <summary>
    /// Aria label for date picker popup for screen reader users.
    /// </summary>
    [Parameter] public string PickerAriaLabel { get; set; } = "Calendar";

    /// <summary>
    /// Placeholder text for the DateRangePicker.
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
    /// ValueFormat for the DateRangePicker
    /// </summary>
    [Parameter] public string ValueFormat { get; set; } = "Start: {0} - End: {1}";

    /// <summary>
    /// Used to customize how content inside the year cell is rendered.
    /// </summary>
    [Parameter] public RenderFragment<int>? YearCellTemplate { get; set; }


    public string LabelId => $"DateRangePicker-Label-{UniqueId}";
    public string CalloutId => $"DateRangePicker-Callout-{UniqueId}";
    public string OverlayId => $"DateRangePicker-Overlay-{UniqueId}";
    public string WrapperId => $"DateRangePicker-Wrapper-{UniqueId}";
    public string TextFieldId => $"DateRangePicker-TextField-{UniqueId}";
    public string MonthTitleId => $"DateRangePicker-MonthTitle-{UniqueId}";
    public string ActiveDescendantId => $"DateRangePicker-ActiveDescendant-{UniqueId}";

    public async Task OpenCallout()
    {
        await HandleOnClick();
    }


    protected override string RootElementClass { get; } = "bit-dtrp";

    protected override Task OnInitializedAsync()
    {
        _dotnetObj = DotNetObjectReference.Create(this);

        return base.OnInitializedAsync();
    }

    protected override void RegisterComponentClasses()
    {
        ClassBuilder.Register(() => IsEnabled is false
            ? $"{RootElementClass}-disabled-{VisualClassRegistrar()}" : string.Empty);

        ClassBuilder.Register(() => Culture.TextInfo.IsRightToLeft
            ? $"{RootElementClass}-rtl-{VisualClassRegistrar()}" : string.Empty);

        ClassBuilder.Register(() => IconLocation is BitIconLocation.Left
            ? $"{RootElementClass}-left-icon-{VisualClassRegistrar()}" : string.Empty);

        ClassBuilder.Register(() => IsUnderlined
            ? $"{RootElementClass}-underlined-{(IsEnabled is false ? "disabled-" : string.Empty)}{VisualClassRegistrar()}" : string.Empty);

        ClassBuilder.Register(() => HasBorder is false
            ? $"{RootElementClass}-no-border-{VisualClassRegistrar()}" : string.Empty);

        ClassBuilder.Register(() => _focusClass.HasValue()
            ? $"{RootElementClass}-{(IsUnderlined ? "underlined-" : null)}{_focusClass}-{VisualClassRegistrar()}" : string.Empty);

        ClassBuilder.Register(() => ValueInvalid is true
                                   ? $"{RootElementClass}-invalid-{VisualClassRegistrar()}" : string.Empty);
    }

    protected override Task OnParametersSetAsync()
    {
        if (CurrentValue is null)
        {
            CurrentValue = new();
        }

        var startDateTime = CurrentValue.StartDate.GetValueOrDefault(DateTimeOffset.Now).DateTime;

        if (MinDate.HasValue && MinDate > new DateTimeOffset(startDateTime))
        {
            startDateTime = MinDate.GetValueOrDefault(DateTimeOffset.Now).DateTime;
        }

        if (MaxDate.HasValue && MaxDate < new DateTimeOffset(startDateTime))
        {
            startDateTime = MaxDate.GetValueOrDefault(DateTimeOffset.Now).DateTime;
        }

        if (CurrentValue.EndDate.HasValue && CurrentValue.EndDate < new DateTimeOffset(startDateTime))
        {
            CurrentValue.EndDate = null;
        }

        CreateMonthCalendar(startDateTime);

        return base.OnParametersSetAsync();
    }

    /// <inheritdoc />
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
        validationErrorMessage = $"The {DisplayName ?? FieldIdentifier.FieldName} field is not valid.";
        return false;
    }

    protected override string? FormatValueAsString(BitDateRangePickerValue? value)
    {
        if (value is null)
        {
            return null;
        }

        if (value.StartDate is null && value.EndDate is null)
        {
            return null;
        }

        var valueStr = string.Format(ValueFormat, value.StartDate.GetValueOrDefault(DateTimeOffset.Now).ToString(DateFormat ?? Culture.DateTimeFormat.ShortDatePattern, Culture), "---");
        if (value.EndDate is not null)
        {
            valueStr = string.Format(ValueFormat, value.StartDate.GetValueOrDefault(DateTimeOffset.Now).ToString(DateFormat ?? Culture.DateTimeFormat.ShortDatePattern, Culture), value.EndDate.GetValueOrDefault(DateTimeOffset.Now).ToString(DateFormat ?? Culture.DateTimeFormat.ShortDatePattern, Culture));
        }

        return valueStr;
    }


    private async Task HandleOnClick()
    {
        if (IsEnabled is false) return;

        _showMonthPickerAsOverlayInternal = ShowMonthPickerAsOverlay;

        await JSRuntime.InvokeVoidAsync("BitDateRangePicker.toggleDateRangePickerCallout", _dotnetObj, UniqueId, CalloutId, OverlayId, IsOpen);

        if (_showMonthPickerAsOverlayInternal is false)
        {
            _showMonthPickerAsOverlayInternal = await JSRuntime.InvokeAsync<bool>("BitDateRangePicker.checkMonthPickerWidth", CalloutId, IsResponsive);
        }

        if (_showMonthPickerAsOverlayInternal)
        {
            _isMonthPickerOverlayOnTop = false;
        }

        IsOpen = !IsOpen;

        if (IsOpen && CurrentValue != null)
        {
            CheckCurrentCalendarMatchesCurrentValue();
        }

        _displayYear = _currentYear;
        await OnClick.InvokeAsync();
    }

    private async Task HandleFocusIn()
    {
        if (IsEnabled is false) return;

        _focusClass = "focused";
        await OnFocusIn.InvokeAsync();
    }

    private async Task HandleFocusOut()
    {
        if (IsEnabled is false) return;

        _focusClass = string.Empty;
        await OnFocusOut.InvokeAsync();
    }

    private async Task HandleFocus()
    {
        if (IsEnabled is false) return;

        _focusClass = "focused";
        await OnFocus.InvokeAsync();
    }

    private async Task HandleChange(ChangeEventArgs e)
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

        if (CurrentValue is null) return;

        if (IsWeekDayOutOfMinAndMaxDate(dayIndex, weekIndex)) return;

        if (CurrentValue.StartDate is not null && CurrentValue.EndDate is not null)
        {
            CurrentValue.StartDate = null;
            CurrentValue.EndDate = null;
        }

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

        _displayYear = _currentYear;
        _currentMonth = selectedMonth;

        var selectedDate = new DateTimeOffset(Culture.DateTimeFormat.Calendar.ToDateTime(_currentYear, _currentMonth, currentDay, 0, 0, 0, 0), DateTimeOffset.Now.Offset);
        if (CurrentValue.StartDate is null)
        {
            CurrentValue.StartDate = selectedDate;
        }
        else
        {
            CurrentValue.EndDate = selectedDate;
            if (AutoClose)
            {
                await JSRuntime.InvokeVoidAsync("BitDateRangePicker.toggleDateRangePickerCallout", _dotnetObj, UniqueId, CalloutId, OverlayId, IsOpen);
                IsOpen = false;
            }
        }

        if (CurrentValue.EndDate is not null && CurrentValue.StartDate > CurrentValue.EndDate)
        {
            (CurrentValue.EndDate, CurrentValue.StartDate) = (CurrentValue.StartDate, CurrentValue.EndDate);
        }

        CurrentValue = new BitDateRangePickerValue { StartDate = CurrentValue.StartDate, EndDate = CurrentValue.EndDate };
        CreateMonthCalendar(_currentYear, _currentMonth);
        await OnSelectDate.InvokeAsync(CurrentValue);
    }

    private void HandleMonthChange(ChangeDirection direction)
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

    private void HandleYearRangeChange(ChangeDirection direction)
    {
        if (IsEnabled is false) return;
        if (CanYearRangeChange(direction) is false) return;

        var fromYear = direction == ChangeDirection.Next ? _yearRangeFrom + 12 : _yearRangeFrom - 12;

        ChangeYearRanges(fromYear);
    }

    private void HandleGoToToday()
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
        _yearRangeFrom = _currentYear - 1;
        _yearRangeTo = _currentYear + 10;
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

        SetSelectedStartDateInMonthCalendar();
        SetSelectedEndDateInMonthCalendar();
    }

    private void SetSelectedStartDateInMonthCalendar()
    {
        if (Culture is null) return;
        if (CurrentValue is null) return;
        if (CurrentValue.StartDate.HasValue is false || (_selectedStartDateWeek.HasValue && _selectedStartDateDayOfWeek.HasValue)) return;

        var year = Culture.DateTimeFormat.Calendar.GetYear(CurrentValue.StartDate.Value.DateTime);
        var month = Culture.DateTimeFormat.Calendar.GetMonth(CurrentValue.StartDate.Value.DateTime);

        if (year == _currentYear && month == _currentMonth)
        {
            var day = Culture.DateTimeFormat.Calendar.GetDayOfMonth(CurrentValue.StartDate.Value.DateTime);
            var firstDayOfWeek = (int)Culture.DateTimeFormat.FirstDayOfWeek;
            var firstDayOfWeekInMonth = (int)Culture.DateTimeFormat.Calendar.ToDateTime(year, month, 1, 0, 0, 0, 0).DayOfWeek;
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

    private void SetSelectedEndDateInMonthCalendar()
    {
        if (Culture is null) return;
        if (CurrentValue is null) return;
        if (CurrentValue.EndDate.HasValue is false || (_selectedEndDateWeek.HasValue && _selectedEndDateDayOfWeek.HasValue)) return;

        var year = Culture.DateTimeFormat.Calendar.GetYear(CurrentValue.EndDate.Value.DateTime);
        var month = Culture.DateTimeFormat.Calendar.GetMonth(CurrentValue.EndDate.Value.DateTime);

        if (year == _currentYear && month == _currentMonth)
        {
            var day = Culture.DateTimeFormat.Calendar.GetDayOfMonth(CurrentValue.EndDate.Value.DateTime);
            var firstDayOfWeek = (int)Culture.DateTimeFormat.FirstDayOfWeek;
            var firstDayOfWeekInMonth = (int)Culture.DateTimeFormat.Calendar.ToDateTime(year, month, 1, 0, 0, 0, 0).DayOfWeek;
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

    private void ResetCalendar()
    {
        for (int weekIndex = 0; weekIndex < DEFAULT_WEEK_COUNT; weekIndex++)
        {
            for (int dayIndex = 0; dayIndex < DEFAULT_DAY_COUNT_PER_WEEK; dayIndex++)
            {
                _currentMonthCalendar[weekIndex, dayIndex] = 0;
            }
        }

        _selectedStartDateWeek = null;
        _selectedEndDateWeek = null;
        _selectedStartDateDayOfWeek = null;
        _selectedEndDateDayOfWeek = null;
    }

    private void ChangeYearRanges(int fromYear)
    {
        _yearRangeFrom = fromYear;
        _yearRangeTo = fromYear + 11;
    }

    private async Task CloseCallout()
    {
        await JSRuntime.InvokeVoidAsync("BitDateRangePicker.toggleDateRangePickerCallout", _dotnetObj, UniqueId, CalloutId, OverlayId, IsOpen);

        IsOpen = false;

        StateHasChanged();
    }

    private string GetDateElClass(int day, int week)
    {
        StringBuilder className = new StringBuilder("date-cell ");
        var todayYear = Culture.DateTimeFormat.Calendar.GetYear(DateTime.Now);
        var todayMonth = Culture.DateTimeFormat.Calendar.GetMonth(DateTime.Now);
        var todayDay = Culture.DateTimeFormat.Calendar.GetDayOfMonth(DateTime.Now);
        var currentDay = _currentMonthCalendar[week, day];

        if (IsInCurrentMonth(week, day) is false)
        {
            className.Append("date-cell--outside-month");
        }

        if (IsInCurrentMonth(week, day) && todayYear == _currentYear && todayMonth == _currentMonth && todayDay == currentDay)
        {
            className.Append("date-cell--today ");
        }

        if (IsInCurrentMonth(week, day) && week == _selectedStartDateWeek && day == _selectedStartDateDayOfWeek)
        {
            className.Append("date-cell--selected-start ");
        }

        if (IsInCurrentMonth(week, day) && week == _selectedEndDateWeek && day == _selectedEndDateDayOfWeek)
        {
            className.Append("date-cell--selected-end ");
        }

        if (IsInCurrentMonth(week, day) && week == _selectedEndDateWeek && day == _selectedEndDateDayOfWeek && week == _selectedStartDateWeek && day == _selectedStartDateDayOfWeek)
        {
            className.Append("date-cell--selected-same-start-end");
        }

        if (IsBetweenTwoSelectedDate(day, week))
        {
            className.Append("date-cell--between-selected");
        }

        return className.ToString();
    }

    private bool IsBetweenTwoSelectedDate(int day, int week)
    {
        if (CurrentValue is null) return false;
        if (CurrentValue.StartDate.HasValue is false || CurrentValue.EndDate.HasValue is false) return false;

        var startDate = CurrentValue.StartDate.GetValueOrDefault(DateTimeOffset.Now);
        var endDate = CurrentValue.EndDate.GetValueOrDefault(DateTimeOffset.Now);
        if (_selectedEndDateWeek is null && IsInCurrentMonth(week, day) && ((week == _selectedStartDateWeek && day > _selectedStartDateDayOfWeek) || week > _selectedStartDateWeek))
        {
            return true;
        }
        else if (_selectedStartDateWeek is null && IsInCurrentMonth(week, day) && ((week == _selectedEndDateWeek && day < _selectedEndDateDayOfWeek) || week < _selectedEndDateWeek))
        {
            return true;
        }
        else if (_selectedEndDateWeek is not null && _selectedStartDateWeek is not null &&
            ((week == _selectedStartDateWeek && day > _selectedStartDateDayOfWeek) || week > _selectedStartDateWeek) &&
            ((week == _selectedEndDateWeek && day < _selectedEndDateDayOfWeek) || week < _selectedEndDateWeek))
        {
            return true;
        }
        else if (_selectedEndDateWeek is null && _selectedStartDateWeek is null && IsInCurrentMonth(week, day) &&
            startDate.Month < _currentMonth &&
            endDate.Month > _currentMonth &&
            endDate.Year >= _currentYear &&
            startDate.Year <= _currentYear)
        {
            return true;
        }
        else
        {
            return false;
        }
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

    private bool CanMonthChange(ChangeDirection direction)
    {
        if (direction == ChangeDirection.Next && MaxDate.HasValue)
        {
            var MaxDateYear = Culture.DateTimeFormat.Calendar.GetYear(MaxDate.Value.DateTime);
            var MaxDateMonth = Culture.DateTimeFormat.Calendar.GetMonth(MaxDate.Value.DateTime);
            if (MaxDateYear == _displayYear && MaxDateMonth == _currentMonth)
                return false;
        }


        if (direction == ChangeDirection.Previous && MinDate.HasValue)
        {
            var MinDateYear = Culture.DateTimeFormat.Calendar.GetYear(MinDate.Value.DateTime);
            var MinDateMonth = Culture.DateTimeFormat.Calendar.GetMonth(MinDate.Value.DateTime);
            if (MinDateYear == _displayYear && MinDateMonth == _currentMonth)
                return false;
        }

        return true;
    }

    private bool CanYearChange(ChangeDirection direction)
    {
        if (direction == ChangeDirection.Next && MaxDate.HasValue && Culture.DateTimeFormat.Calendar.GetYear(MaxDate.Value.DateTime) == _displayYear)
            return false;

        if (direction == ChangeDirection.Previous && MinDate.HasValue && Culture.DateTimeFormat.Calendar.GetYear(MinDate.Value.DateTime) == _displayYear)
            return false;

        return true;
    }

    private bool CanYearRangeChange(ChangeDirection direction)
    {
        if (direction == ChangeDirection.Next && MaxDate.HasValue && Culture.DateTimeFormat.Calendar.GetYear(MaxDate.Value.DateTime) < _yearRangeFrom + 12)
            return false;

        if (direction == ChangeDirection.Previous && MinDate.HasValue && Culture.DateTimeFormat.Calendar.GetYear(MinDate.Value.DateTime) >= _yearRangeFrom)
            return false;

        return true;
    }

    private bool IsWeekDayOutOfMinAndMaxDate(int dayIndex, int weekIndex)
    {
        var day = _currentMonthCalendar[weekIndex, dayIndex];
        var month = GetCorrectTargetMonth(weekIndex, dayIndex);

        if (MaxDate.HasValue)
        {
            var MaxDateYear = Culture.DateTimeFormat.Calendar.GetYear(MaxDate.Value.DateTime);
            var MaxDateMonth = Culture.DateTimeFormat.Calendar.GetMonth(MaxDate.Value.DateTime);
            var MaxDateDay = Culture.DateTimeFormat.Calendar.GetDayOfMonth(MaxDate.Value.DateTime);

            if (_displayYear > MaxDateYear ||
               (_displayYear == MaxDateYear && month > MaxDateMonth) ||
               (_displayYear == MaxDateYear && month == MaxDateMonth && day > MaxDateDay))
                return true;
        }

        if (MinDate.HasValue)
        {
            var MinDateYear = Culture.DateTimeFormat.Calendar.GetYear(MinDate.Value.DateTime);
            var MinDateMonth = Culture.DateTimeFormat.Calendar.GetMonth(MinDate.Value.DateTime);
            var MinDateDay = Culture.DateTimeFormat.Calendar.GetDayOfMonth(MinDate.Value.DateTime);
            if (_displayYear < MinDateYear ||
               (_displayYear == MinDateYear && month < MinDateMonth) ||
               (_displayYear == MinDateYear && month == MinDateMonth && day < MinDateDay))
                return true;
        }

        return false;
    }

    private bool IsMonthOutOfMinAndMaxDate(int month)
    {
        if (MaxDate.HasValue)
        {
            var MaxDateYear = Culture.DateTimeFormat.Calendar.GetYear(MaxDate.Value.DateTime);
            var MaxDateMonth = Culture.DateTimeFormat.Calendar.GetMonth(MaxDate.Value.DateTime);
            if (_displayYear > MaxDateYear || (_displayYear == MaxDateYear && month > MaxDateMonth))
                return true;
        }

        if (MinDate.HasValue)
        {
            var MinDateYear = Culture.DateTimeFormat.Calendar.GetYear(MinDate.Value.DateTime);
            var MinDateMonth = Culture.DateTimeFormat.Calendar.GetMonth(MinDate.Value.DateTime);
            if (_displayYear < MinDateYear || (_displayYear == MinDateYear && month < MinDateMonth))
                return true;
        }

        return false;
    }

    private bool IsYearOutOfMinAndMaxDate(int year)
    {
        if (MaxDate.HasValue && year > Culture.DateTimeFormat.Calendar.GetYear(MaxDate.Value.DateTime))
            return true;

        if (MinDate.HasValue && year < Culture.DateTimeFormat.Calendar.GetYear(MinDate.Value.DateTime))
            return true;

        return false;
    }

    private void CheckCurrentCalendarMatchesCurrentValue()
    {
        if (CurrentValue is null) return;
        if (CurrentValue.StartDate is null) return;

        var currentValue = CurrentValue.StartDate.GetValueOrDefault(DateTimeOffset.Now);
        var currentValueYear = Culture.DateTimeFormat.Calendar.GetYear(currentValue.DateTime);
        var currentValueMonth = Culture.DateTimeFormat.Calendar.GetMonth(currentValue.DateTime);
        if (currentValueYear != _currentYear || currentValueMonth != _currentMonth)
        {
            _currentYear = currentValueYear;
            _currentMonth = currentValueMonth;
            CreateMonthCalendar(_currentYear, _currentMonth);
        }
    }

    private string GetMonthCellClassName(int monthIndex)
    {
        var className = string.Empty;
        if (HighlightCurrentMonth)
        {
            var todayMonth = Culture.DateTimeFormat.Calendar.GetMonth(DateTime.Now);
            className += todayMonth == monthIndex ? "current-month" : null;
        }

        if (HighlightSelectedMonth && _currentMonth == monthIndex)
        {
            className += className.Length == 0 ? "selected-month" : " selected-month";
        }

        return className;
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

        var currentDate = new DateTimeOffset(Culture.DateTimeFormat.Calendar.ToDateTime(currentYear, selectedMonth, currentDay, 0, 0, 0, 0), DateTimeOffset.Now.Offset);
        return currentDate;
    }

    private DateTimeOffset GetMonthCellDate(int monthIndex)
    {
        var currentDate = new DateTimeOffset(Culture.DateTimeFormat.Calendar.ToDateTime(_currentYear, monthIndex, 1, 0, 0, 0, 0), DateTimeOffset.Now.Offset);
        return currentDate;
    }


    [JSInvokable("CloseCallout")]
    public void CloseCalloutBeforeAnotherCalloutIsOpened()
    {
        IsOpen = false;
    }


    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);

        if (_disposed || disposing is false) return;

        _dotnetObj.Dispose();

        _disposed = true;
    }
}
