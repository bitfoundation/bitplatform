﻿using System.Text;
using System.Globalization;
using System.Diagnostics.CodeAnalysis;

namespace Bit.BlazorUI;

/// <summary>
/// A BitDateRangePicker offers a drop-down control that’s optimized for picking two dates from a calendar view where contextual information like the day of the week or fullness of the calendar is important.
/// </summary>
public partial class BitDateRangePicker : BitInputBase<BitDateRangePickerValue?>
{
    private const int MAX_WIDTH = 470;
    private const int DEFAULT_WEEK_COUNT = 6;
    private const int DEFAULT_DAY_COUNT_PER_WEEK = 7;



    private bool _hasFocus;
    private int _currentYear;
    private int _currentMonth;
    private int _yearPickerEndYear;
    private int _yearPickerStartYear;
    private bool _showMonthPicker = true;
    private bool _isTimePickerOverlayOnTop;
    private bool _isMonthPickerOverlayOnTop;
    private string _monthTitle = string.Empty;
    private bool _showTimePickerAsOverlayInternal;
    private bool _showMonthPickerAsOverlayInternal;
    private TimeZoneInfo _timeZone = TimeZoneInfo.Local;
    private CultureInfo _culture = CultureInfo.CurrentUICulture;
    private CancellationTokenSource _cancellationTokenSource = new();
    private DotNetObjectReference<BitDateRangePicker> _dotnetObj = default!;
    private readonly DateTime?[,] _daysOfCurrentMonth = new DateTime?[DEFAULT_WEEK_COUNT, DEFAULT_DAY_COUNT_PER_WEEK];

    private string? _labelId;
    private string? _inputId;
    private string _calloutId = string.Empty;
    private string _dateRangePickerId = string.Empty;
    private ElementReference _startTimeHourInputRef = default!;
    private ElementReference _startTimeMinuteInputRef = default!;
    private ElementReference _endTimeHourInputRef = default!;
    private ElementReference _endTimeMinuteInputRef = default!;



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
                value = 23;
            }
            else if (value < 0)
            {
                value = 0;
            }

            if (CanChangeTime(startTimeHour: value) is false) return;

            _startTimeHour = value;

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
                value = 59;
            }
            else if (value < 0)
            {
                value = 0;
            }

            if (CanChangeTime(endTimeHour: value) is false) return;

            _startTimeMinute = value;

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
                value = 23;
            }
            else if (value < 0)
            {
                value = 0;
            }

            if (CanChangeTime(endTimeHour: value) is false) return;

            _endTimeHour = value;

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
                value = 59;
            }
            else if (value < 0)
            {
                value = 0;
            }

            if (CanChangeTime(endTimeMinute: value) is false) return;

            _endTimeMinute = value;

            UpdateTime();
        }
    }



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
    /// Custom CSS classes for different parts of the BitDateRangePicker component.
    /// </summary>
    [Parameter] public BitDateRangePickerClassStyles? Classes { get; set; }

    /// <summary>
    /// The title of the close button (tooltip).
    /// </summary>
    [Parameter] public string CloseButtonTitle { get; set; } = "Close date range picker";

    /// <summary>
    /// CultureInfo for the DateRangePicker.
    /// </summary>
    [Parameter, ResetClassBuilder]
    [CallOnSet(nameof(OnSetParameters))]
    public CultureInfo? Culture { get; set; }

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
    /// The title of the ShowTimePicker button (tooltip).
    /// </summary>
    [Parameter] public string ShowTimePickerTitle { get; set; } = "Show time picker";

    /// <summary>
    /// The title of the HideTimePicker button (tooltip).
    /// </summary>
    [Parameter] public string HideTimePickerTitle { get; set; } = "Hide time picker";

    /// <summary>
    /// Determines if the DateRangePicker has a border.
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
    /// Custom template for the DateRangePicker's icon.
    /// </summary>
    [Parameter] public RenderFragment? IconTemplate { get; set; }

    /// <summary>
    /// Determines the location of the DateRangePicker's icon.
    /// </summary>
    [Parameter, ResetClassBuilder]
    public BitIconLocation IconLocation { get; set; } = BitIconLocation.Right;

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
    [Parameter, TwoWayBound]
    public bool IsOpen { get; set; }

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
    [Parameter]
    [CallOnSet(nameof(OnSetParameters))]
    public DateTimeOffset? MaxDate { get; set; }

    /// <summary>
    /// The minimum date allowed for the DateRangePicker.
    /// </summary>
    [Parameter]
    [CallOnSet(nameof(OnSetParameters))]
    public DateTimeOffset? MinDate { get; set; }

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
    /// The placeholder text of the DateRangePicker's input.
    /// </summary>
    [Parameter] public string Placeholder { get; set; } = string.Empty;

    /// <summary>
    /// Enables the responsive mode in small screens.
    /// </summary>
    [Parameter] public bool Responsive { get; set; }

    /// <summary>
    /// Whether the DateRangePicker's close button should be shown or not.
    /// </summary>
    [Parameter] public bool ShowCloseButton { get; set; }

    /// <summary>
    /// Whether the GoToToday button should be shown or not.
    /// </summary>
    [Parameter] public bool ShowGoToToday { get; set; } = true;

    /// <summary>
    /// Show month picker on top of date range picker when visible.
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
    /// Custom CSS styles for different parts of the BitDateRangePicker component.
    /// </summary>
    [Parameter] public BitDateRangePickerClassStyles? Styles { get; set; }

    /// <summary>
    /// The tabIndex of the DateRangePicker's input.
    /// </summary>
    [Parameter] public int TabIndex { get; set; }

    /// <summary>
    /// Time format of the time-pickers, 24H or 12H.
    /// </summary>
    [Parameter] public BitTimeFormat TimeFormat { get; set; }

    /// <summary>
    /// TimeZone for the DateRangePicker.
    /// </summary>
    [Parameter]
    [CallOnSet(nameof(OnSetParameters))]
    public TimeZoneInfo? TimeZone { get; set; }

    /// <summary>
    /// Whether or not the Text field of the DateRangePicker is underlined.
    /// </summary>
    [Parameter, ResetClassBuilder]
    public bool Underlined { get; set; }

    /// <summary>
    /// The string format used to show the DateRangePicker's value in its input.
    /// </summary>
    [Parameter] public string ValueFormat { get; set; } = "Start: {0} - End: {1}";

    /// <summary>
    /// The title of the week number (tooltip).
    /// </summary>
    [Parameter] public string WeekNumberTitle { get; set; } = "Week number {0}";

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

    /// <summary>
    /// Show month picker on top of date range picker when visible.
    /// </summary>
    [Parameter]
    [CallOnSet(nameof(OnSetParameters))]
    public bool ShowTimePickerAsOverlay { get; set; }

    /// <summary>
    /// The maximum range of day and times allowed for selection in DateRangePicker.
    /// </summary>
    [Parameter] public TimeSpan? MaxRange { get; set; }

    /// <summary>
    /// Whether the clear button should be shown or not when the DateRangePicker has a value.
    /// </summary>
    [Parameter] public bool ShowClearButton { get; set; }

    /// <summary>
    /// Determines increment/decrement steps for DateRangePicker's hour.
    /// </summary>
    [Parameter] public int HourStep { get; set; } = 1;

    /// <summary>
    /// Determines increment/decrement steps for DateRangePicker's minute.
    /// </summary>
    [Parameter] public int MinuteStep { get; set; } = 1;

    /// <summary>
    /// Specifies the date and time of the date and time picker when it is opened without any selected value.
    /// </summary>
    [Parameter]
    [CallOnSet(nameof(OnSetParameters))]
    public BitDateRangePickerValue? StartingValue { get; set; }

    /// <summary>
    /// Whether the DateRangePicker is rendered standalone or with the input component and callout.
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



    protected override string RootElementClass => "bit-dtrp";

    protected override void RegisterCssClasses()
    {
        ClassBuilder.Register(() => Classes?.Root);

        ClassBuilder.Register(() => (Dir is null && _culture.TextInfo.IsRightToLeft) ? "bit-rtl" : string.Empty);

        ClassBuilder.Register(() => IconLocation is BitIconLocation.Left ? $"{RootElementClass}-lic" : string.Empty);

        ClassBuilder.Register(() => Underlined ? $"{RootElementClass}-und" : string.Empty);

        ClassBuilder.Register(() => HasBorder is false ? $"{RootElementClass}-nbd" : string.Empty);

        ClassBuilder.Register(() => Standalone ? "bit-dtrp-sta" : string.Empty);

        ClassBuilder.Register(() => _hasFocus ? $"bit-dtrp-foc {Classes?.Focused}" : string.Empty);

        ClassBuilder.Register(() => IsEnabled && Required ? "bit-dtrp-req" : string.Empty);
    }

    protected override void RegisterCssStyles()
    {
        StyleBuilder.Register(() => Styles?.Root);

        StyleBuilder.Register(() => _hasFocus ? Styles?.Focused : string.Empty);
    }

    protected override void OnInitialized()
    {
        _dateRangePickerId = $"DateRangePicker-{UniqueId}";
        _labelId = $"{_dateRangePickerId}-label";
        _calloutId = $"{_dateRangePickerId}-callout";
        _inputId = $"{_dateRangePickerId}-input";

        OnValueChanged += HandleOnValueChanged;

        OnSetParameters();

        base.OnInitialized();
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        await base.OnAfterRenderAsync(firstRender);

        if (firstRender is false) return;

        _dotnetObj = DotNetObjectReference.Create(this);

        if (Responsive is false) return;

        await _js.BitSwipesSetup(_calloutId, 0.25m, BitPanelPosition.Top, Dir is BitDir.Rtl, BitSwipeOrientation.Vertical, _dotnetObj);
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
        //    result = new DateTimeOffset(parsedValue, _timeZone.GetUtcOffset(parsedValue));
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

        return string.Format(_culture, ValueFormat,
                            value.StartDate.GetValueOrDefault(DateTimeOffset.Now)
                                           .ToString(DateFormat ?? _culture.DateTimeFormat.ShortDatePattern, _culture),
                            value.EndDate.HasValue ?
                                value.EndDate.GetValueOrDefault(DateTimeOffset.Now)
                                             .ToString(DateFormat ?? _culture.DateTimeFormat.ShortDatePattern, _culture)
                                : "---");
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

        if (_showMonthPickerAsOverlayInternal is false && ShowTimePicker && _showTimePickerAsOverlayInternal is false)
        {
            _showMonthPickerAsOverlayInternal = true;
        }

        if (CurrentValue is not null)
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

        CurrentValueAsString = e.Value?.ToString();
    }

    private async Task HandleOnClearButtonClick()
    {
        if (IsEnabled is false) return;

        CurrentValue = null;

        _startTimeHour = 0;
        _startTimeMinute = 0;

        _endTimeHour = MaxRange.HasValue && MaxRange.Value.TotalHours < 24 ? (int)MaxRange.Value.TotalHours : 23;
        _endTimeMinute = MaxRange.HasValue && MaxRange.Value.TotalMinutes < 60 ? (int)MaxRange.Value.TotalMinutes : 59;

        await InputElement.FocusAsync();
    }

    private void HandleOnValueChanged(object? sender, EventArgs args)
    {
        OnSetParameters();
    }

    private void OnSetParameters()
    {
        _timeZone = TimeZone ?? TimeZoneInfo.Local;
        _culture = Culture ?? CultureInfo.CurrentUICulture;

        if (CurrentValue is not null)
        {
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
        }

        var startDateHasValue = CurrentValue?.StartDate.HasValue ?? false;
        var endDateHasValue = CurrentValue?.EndDate.HasValue ?? false;
        var startingValueStartDateHasValue = StartingValue?.StartDate.HasValue ?? false;
        var startingValueEndDateHasValue = StartingValue?.EndDate.HasValue ?? false;

        _startTimeHour = startDateHasValue ? CurrentValue!.StartDate!.Value.Hour : (startingValueStartDateHasValue ? StartingValue!.StartDate!.Value.Hour : 0);
        _startTimeMinute = startDateHasValue ? CurrentValue!.StartDate!.Value.Minute : (startingValueStartDateHasValue ? StartingValue!.StartDate!.Value.Minute : 0);

        _endTimeHour = endDateHasValue ? CurrentValue!.EndDate!.Value.Hour : (startingValueEndDateHasValue ? StartingValue!.EndDate!.Value.Hour : 23);
        _endTimeMinute = endDateHasValue ? CurrentValue!.EndDate!.Value.Minute : (startingValueEndDateHasValue ? StartingValue!.EndDate!.Value.Minute : 59);

        if (endDateHasValue is false && MaxRange.HasValue && MaxRange.Value.TotalHours < 24)
        {
            if (_endTimeHour > MaxRange.Value.TotalHours)
            {
                _endTimeHour = (int)MaxRange.Value.TotalHours;
            }

            if (MaxRange.Value.Minutes < 60 && _endTimeMinute > MaxRange.Value.Minutes)
            {
                _endTimeMinute = MaxRange.Value.Minutes;
            }
        }

        var calendarDate = startDateHasValue ? CurrentValue!.StartDate!.Value.DateTime : (startingValueStartDateHasValue ? StartingValue!.StartDate!.Value.DateTime : DateTimeOffset.Now.DateTime);
        GenerateCalendarData(calendarDate);

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

            if (_showMonthPickerAsOverlayInternal is false && ShowTimePicker && _showTimePickerAsOverlayInternal is false)
            {
                _showMonthPickerAsOverlayInternal = true;
            }

            if (CurrentValue is not null)
            {
                CheckCurrentCalendarMatchesCurrentValue();
            }
        }
    }

    private async Task SelectDate(DateTime selectedDate)
    {
        if (ReadOnly) return;
        if (IsEnabled is false || InvalidValueBinding()) return;
        if (IsOpenHasBeenSet && IsOpenChanged.HasDelegate is false) return;
        if (IsWeekDayOutOfMinAndMaxDate(selectedDate)) return;

        var curValue = CurrentValue ?? new();

        if (curValue.StartDate.HasValue && curValue.EndDate.HasValue)
        {
            curValue.StartDate = null;
            curValue.EndDate = null;
        }

        var hour = curValue.StartDate.HasValue ? _endTimeHour : _startTimeHour;
        var minute = curValue.StartDate.HasValue ? _endTimeMinute : _startTimeMinute;

        selectedDate = selectedDate.AddHours(hour);
        selectedDate = selectedDate.AddMinutes(minute);

        var selectedDateTimeOffset = new DateTimeOffset(selectedDate, _timeZone.GetUtcOffset(selectedDate));
        if (curValue.StartDate.HasValue is false)
        {
            curValue.StartDate = selectedDateTimeOffset;
        }
        else
        {
            curValue.EndDate = selectedDateTimeOffset;

            if (AutoClose && Standalone is false)
            {
                await AssignIsOpen(false);

                await ToggleCallout();
            }
        }

        if (curValue.EndDate.HasValue && curValue.StartDate > curValue.EndDate)
        {
            if (curValue.StartDate!.Value.Date == curValue.EndDate.Value.Date)
            {
                (_endTimeHour, _startTimeHour) = (_startTimeHour, _endTimeHour);
                (_endTimeMinute, _startTimeMinute) = (_startTimeMinute, _endTimeMinute);
            }

            (curValue.EndDate, curValue.StartDate) = (curValue.StartDate, curValue.EndDate);
        }

        if (curValue.EndDate.HasValue && MaxRange.HasValue)
        {
            var maxDate = new DateTimeOffset(GetMaxEndDate(), curValue.EndDate.Value.Offset);

            if (maxDate < curValue.EndDate)
            {
                _endTimeHour = maxDate.Hour;
                _endTimeMinute = maxDate.Minute;
                curValue.EndDate = maxDate;
            }
        }

        CurrentValue = new BitDateRangePickerValue { StartDate = curValue.StartDate, EndDate = curValue.EndDate };

        _currentMonth = _culture.Calendar.GetMonth(selectedDate);

        GenerateMonthData(_currentYear, _currentMonth);
    }

    private void SelectMonth(int month)
    {
        if (IsEnabled is false) return;
        if (IsMonthOutOfMinAndMaxDate(month)) return;

        _currentMonth = month;

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
        int dayOfWeek = (int)(_culture.DateTimeFormat.FirstDayOfWeek) + index;

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
        _isMonthPickerOverlayOnTop = !_isMonthPickerOverlayOnTop;
    }

    private void ToggleTimePickerOverlay()
    {
        _isTimePickerOverlayOnTop = !_isTimePickerOverlayOnTop;
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

        if (MaxRange.HasValue && MaxRange.Value.TotalDays > 0 && CurrentValue?.StartDate is not null && CurrentValue.EndDate.HasValue is false)
        {
            if (isNext)
            {
                var maxDateYear = _culture.Calendar.GetYear(GetMaxEndDate());
                var maxDateMonth = _culture.Calendar.GetMonth(GetMaxEndDate());

                if (maxDateYear == _currentYear && maxDateMonth == _currentMonth) return false;
            }
            else
            {
                var minDateYear = _culture.Calendar.GetYear(GetMinEndDate());
                var minDateMonth = _culture.Calendar.GetMonth(GetMinEndDate());

                if (minDateYear == _currentYear && minDateMonth == _currentMonth) return false;
            }
        }

        return true;
    }

    private bool CanChangeYear(bool isNext)
    {
        if (IsEnabled is false) return false;

        if (isNext)
        {
            var isInMaxDateYear = MaxDate.HasValue && _culture.Calendar.GetYear(MaxDate.Value.DateTime) == _currentYear;
            if (isInMaxDateYear) return false;

            var isInMaxDayRangeYear = MaxRange.HasValue && MaxRange.Value.TotalDays > 0 && CurrentValue?.StartDate is not null &&
                (_culture.Calendar.GetYear(GetMaxEndDate()) == _currentYear ||
                 _culture.Calendar.GetYear(GetMinEndDate()) == _currentYear);

            return isInMaxDayRangeYear is false;
        }
        else
        {
            var isInMinDateYear = MinDate.HasValue && _culture.Calendar.GetYear(MinDate.Value.DateTime) == _currentYear;
            if (isInMinDateYear) return false;

            var isInMaxDayRangeYear = MaxRange.HasValue && MaxRange.Value.TotalDays > 0 && CurrentValue?.StartDate is not null && CurrentValue!.EndDate.HasValue is false &&
                (_culture.Calendar.GetYear(GetMaxEndDate()) == _currentYear ||
                 _culture.Calendar.GetYear(GetMinEndDate()) == _currentYear);

            return isInMaxDayRangeYear is false;
        }
    }

    private bool CanChangeYearRange(bool isNext)
    {
        if (IsEnabled is false) return false;

        if (isNext)
        {
            var isInMaxDateYearRange = MaxDate.HasValue && _culture.Calendar.GetYear(MaxDate.Value.DateTime) < _yearPickerStartYear + 12;
            if (isInMaxDateYearRange) return false;

            var isInMaxDayRangeYearRange = MaxRange.HasValue && MaxRange.Value.TotalDays > 0 && CurrentValue?.StartDate is not null && CurrentValue.EndDate.HasValue is false &&
                (_culture.Calendar.GetYear(GetMaxEndDate()) < _yearPickerStartYear + 12 ||
                 _culture.Calendar.GetYear(GetMinEndDate()) < _yearPickerStartYear + 12);

            return isInMaxDayRangeYearRange is false;
        }
        else
        {
            var isInMinDateYearRange = MinDate.HasValue && _culture.Calendar.GetYear(MinDate.Value.DateTime) >= _yearPickerStartYear;
            if (isInMinDateYearRange) return false;

            var isInMaxDayRangeYearRange = MaxRange.HasValue && MaxRange.Value.TotalDays > 0 && CurrentValue?.StartDate is not null && CurrentValue.EndDate.HasValue is false &&
                (_culture.Calendar.GetYear(GetMaxEndDate()) >= _yearPickerStartYear ||
                 _culture.Calendar.GetYear(GetMinEndDate()) >= _yearPickerStartYear);

            return isInMaxDayRangeYearRange is false;
        }
    }

    private bool IsWeekDayOutOfMinAndMaxDate(DateTime date)
    {
        if (MaxDate.HasValue)
        {
            if (date > GetDateTime(MaxDate.Value)) return true;
        }

        if (MinDate.HasValue)
        {
            if (date < GetDateTime(MinDate.Value)) return true;
        }

        if (MaxRange.HasValue && MaxRange.Value.TotalDays > 0 && CurrentValue?.StartDate is not null && CurrentValue.EndDate.HasValue is false)
        {
            var maxEndDate = GetMaxEndDate();
            if (date > maxEndDate) return true;

            var minEndDate = GetMinEndDate();
            if (date < minEndDate) return true;
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

        if (MaxRange.HasValue && MaxRange.Value.TotalDays > 0 && CurrentValue?.StartDate is not null && CurrentValue.EndDate.HasValue is false)
        {
            var maxEndDate = GetMaxEndDate();
            var maxDateYear = _culture.Calendar.GetYear(maxEndDate);
            var maxDateMonth = _culture.Calendar.GetMonth(maxEndDate);

            if (_currentYear > maxDateYear || (_currentYear == maxDateYear && month > maxDateMonth)) return true;

            var minEndDate = GetMinEndDate();
            var minDateYear = _culture.Calendar.GetYear(minEndDate);
            var minDateMonth = _culture.Calendar.GetMonth(minEndDate);

            if (_currentYear < minDateYear || (_currentYear == minDateYear && month < minDateMonth)) return true;
        }

        return false;
    }

    private bool IsYearOutOfMinAndMaxDate(int year)
    {
        return (MaxDate.HasValue && year > _culture.Calendar.GetYear(MaxDate.Value.DateTime))
            || (MinDate.HasValue && year < _culture.Calendar.GetYear(MinDate.Value.DateTime))
            || (MaxRange.HasValue && MaxRange.Value.TotalDays > 0 && CurrentValue?.StartDate is not null && CurrentValue!.EndDate.HasValue is false &&
                (year > _culture.Calendar.GetYear(GetMaxEndDate()) ||
                 year < _culture.Calendar.GetYear(GetMinEndDate())));
    }

    private void CheckCurrentCalendarMatchesCurrentValue()
    {
        if (CurrentValue is null) return;
        if (CurrentValue.StartDate.HasValue is false) return;

        var currentValue = CurrentValue.StartDate.GetValueOrDefault(DateTimeOffset.Now);
        var currentValueYear = _culture.Calendar.GetYear(currentValue.DateTime);
        var currentValueMonth = _culture.Calendar.GetMonth(currentValue.DateTime);

        if (currentValueYear != _currentYear || currentValueMonth != _currentMonth)
        {
            _currentYear = currentValueYear;
            _currentMonth = currentValueMonth;
            GenerateMonthData(_currentYear, _currentMonth);
        }
    }

    private (string style, string klass) GetDayButtonCss(DateTime date)
    {
        StringBuilder klass = new StringBuilder();
        StringBuilder style = new StringBuilder();

        var isStartDaySelectedDate = IsStartDaySelectedDate(date);
        var isEndDaySelectedDate = IsEndDaySelectedDate(date);

        if (isStartDaySelectedDate)
        {
            klass.Append(" bit-dtrp-dss");

            if (Classes?.StartDayButton is not null)
            {
                klass.Append(' ').Append(Classes?.StartDayButton);
            }

            if (Styles?.StartDayButton is not null)
            {
                style.Append(Styles?.StartDayButton);
            }

            if (Classes?.StartAndEndSelectionDays is not null)
            {
                klass.Append(' ').Append(Classes?.StartAndEndSelectionDays);
            }

            if (Styles?.StartAndEndSelectionDays is not null)
            {
                style.Append(Styles?.StartAndEndSelectionDays);
            }
        }

        if (isEndDaySelectedDate)
        {
            klass.Append(" bit-dtrp-dse");

            if (Classes?.EndDayButton is not null)
            {
                klass.Append(' ').Append(Classes?.EndDayButton);
            }

            if (Styles?.EndDayButton is not null)
            {
                style.Append(Styles?.EndDayButton);
            }

            if (Classes?.StartAndEndSelectionDays is not null)
            {
                klass.Append(' ').Append(Classes?.StartAndEndSelectionDays);
            }

            if (Styles?.StartAndEndSelectionDays is not null)
            {
                style.Append(Styles?.StartAndEndSelectionDays);
            }

            if (IsEqualStartAndEndDaySelectedDate(date))
            {
                klass.Append(" bit-dtrp-dsse");
            }
        }

        if (isStartDaySelectedDate is false && isEndDaySelectedDate is false && IsBetweenTwoSelectedDate(date))
        {
            klass.Append(" bit-dtrp-dsb");

            if (Classes?.SelectedDayButtons is not null)
            {
                klass.Append(' ').Append(Classes?.SelectedDayButtons);
            }

            if (Styles?.SelectedDayButtons is not null)
            {
                style.Append(Styles?.SelectedDayButtons);
            }
        }

        var month = _culture.Calendar.GetMonth(date);

        //Isn't in current month
        if (month != _currentMonth)
        {
            klass.Append(" bit-dtrp-dbo");
        }

        //Is today
        if (month == _currentMonth && date == GetDateTime(DateTimeOffset.Now).Date)
        {
            klass.Append(" bit-dtrp-dtd");

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
            className.Append(" bit-dtrp-pcm");
        }

        if (HighlightSelectedMonth && _currentMonth == monthIndex)
        {
            className.Append(" bit-dtrp-psm");
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

    private bool IsBetweenTwoSelectedDate(DateTime date)
    {
        if (CurrentValue is null) return false;
        if (CurrentValue.StartDate.HasValue is false || CurrentValue.EndDate.HasValue is false) return false;

        return date >= GetDateTime(CurrentValue.StartDate.Value).Date && date <= GetDateTime(CurrentValue.EndDate.Value).Date;
    }

    private bool IsStartDaySelectedDate(DateTime date)
    {
        if (CurrentValue is null) return false;
        if (CurrentValue.StartDate.HasValue is false) return false;

        return date == GetDateTime(CurrentValue.StartDate.Value).Date;
    }

    private bool IsEndDaySelectedDate(DateTime date)
    {
        if (CurrentValue is null) return false;
        if (CurrentValue.EndDate.HasValue is false) return false;

        return date == GetDateTime(CurrentValue.EndDate.Value).Date;
    }

    private bool IsEqualStartAndEndDaySelectedDate(DateTime date)
    {
        if (CurrentValue is null) return false;
        if (CurrentValue.StartDate.HasValue is false || CurrentValue.EndDate.HasValue is false) return false;

        var endDate = GetDateTime(CurrentValue.EndDate.Value).Date;

        return GetDateTime(CurrentValue.StartDate.Value).Date == endDate && date == endDate;
    }

    private void UpdateTime()
    {
        if (CurrentValue is null) return;
        if (CurrentValue.StartDate.HasValue is false && CurrentValue.EndDate.HasValue is false) return;

        var isEndGreaterInOneDayRange = CurrentValue.StartDate.HasValue &&
                                        CurrentValue.EndDate.HasValue &&
                                        CurrentValue.StartDate!.Value.Date == CurrentValue.EndDate!.Value.Date &&
                                        new TimeSpan(_startTimeHour, _startTimeMinute, 0) > new TimeSpan(_endTimeHour, _endTimeMinute, 0);

        if (isEndGreaterInOneDayRange)
        {
            _startTimeHour = _endTimeHour;
            _startTimeMinute = _endTimeMinute;
        }

        CurrentValue = new BitDateRangePickerValue
        {
            StartDate = GetDateTimeOffset(CurrentValue.StartDate, _startTimeHour, _startTimeMinute),
            EndDate = GetDateTimeOffset(CurrentValue.EndDate, _endTimeHour, _endTimeMinute)
        };
    }

    private DateTimeOffset? GetDateTimeOffset(DateTimeOffset? date, int hour, int minute)
    {
        if (date.HasValue is false) return null;

        var dateTime = GetDateTime(date.Value);
        var year = _culture.Calendar.GetYear(dateTime);
        var month = _culture.Calendar.GetMonth(dateTime);
        var day = _culture.Calendar.GetDayOfMonth(dateTime);

        var resultDate = _culture.Calendar.ToDateTime(year, month, day, hour, minute, 0, 0);
        return new(resultDate, _timeZone.GetUtcOffset(resultDate));
    }

    private DateTime GetDateTime(DateTimeOffset dateTimeOffset)
    {
        return TimeZoneInfo.ConvertTimeFromUtc(dateTimeOffset.UtcDateTime, _timeZone);
    }

    private async Task HandleOnHourInputFocus(bool isStartTime)
    {
        if (IsEnabled is false || ShowTimePicker is false) return;

        await _js.BitUtilsSelectText(isStartTime ? _startTimeHourInputRef : _endTimeHourInputRef);
    }

    private async Task HandleOnMinuteInputFocus(bool isStartTime)
    {
        if (IsEnabled is false || ShowTimePicker is false) return;

        await _js.BitUtilsSelectText(isStartTime ? _startTimeMinuteInputRef : _endTimeMinuteInputRef);
    }

    private void HandleOnAmClick(bool isStartTime)
    {
        if (ReadOnly) return;
        if (IsEnabled is false) return;

        if (isStartTime)
        {
            _startTimeHour %= 12;  // "12:-- am" is "00:--" in 24h
        }
        else
        {
            _endTimeHour %= 12;  // "12:-- am" is "00:--" in 24h
        }

        UpdateTime();
    }

    private void HandleOnPmClick(bool isStartTime)
    {
        if (ReadOnly) return;
        if (IsEnabled is false) return;

        if (isStartTime)
        {
            if (_startTimeHour <= 12) // "12:-- pm" is "12:--" in 24h
            {
                _startTimeHour += 12;
            }

            _startTimeHour %= 24;
        }
        else
        {
            if (_endTimeHour <= 12) // "12:-- pm" is "12:--" in 24h
            {
                _endTimeHour += 12;
            }

            _endTimeHour %= 24;
        }

        UpdateTime();
    }

    private bool? IsAm(int hour)
    {
        if (CurrentValue is null) return null;

        return hour >= 0 && hour < 12; // am is 00:00 to 11:59
    }

    private async Task HandleOnPointerDown(bool isNext, bool isHour, bool isStartTime)
    {
        if (ReadOnly) return;
        if (IsEnabled is false) return;

        await ChangeTime(isNext, isHour, isStartTime);
        ResetCts();

        var cts = _cancellationTokenSource;
        await Task.Run(async () =>
        {
            await InvokeAsync(async () =>
            {
                await Task.Delay(400);
                await ContinuousChangeTime(isNext, isHour, isStartTime, cts);
            });
        }, cts.Token);
    }

    private async Task ContinuousChangeTime(bool isNext, bool isHour, bool isStartTime, CancellationTokenSource cts)
    {
        if (cts.IsCancellationRequested) return;

        await ChangeTime(isNext, isHour, isStartTime);

        StateHasChanged();

        await Task.Delay(75);
        await ContinuousChangeTime(isNext, isHour, isStartTime, cts);
    }

    private async Task ChangeTime(bool isNext, bool isHour, bool isStartTime)
    {
        if (isHour)
        {
            ChangeHour(isNext, isStartTime);
        }
        else
        {
            ChangeMinute(isNext, isStartTime);
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

    private void ChangeHour(bool isNext, bool isStartTime)
    {
        if (isStartTime)
        {
            _startTimeHour = ChangeHour(_startTimeHour, isNext);
        }
        else
        {
            _endTimeHour = ChangeHour(_endTimeHour, isNext);
        }

        UpdateTime();
    }

    private int ChangeHour(int hour, bool isNext)
    {
        if (isNext)
        {
            hour += HourStep;
        }
        else
        {
            hour -= HourStep;
        }

        if (hour > 23)
        {
            hour -= 24;
        }
        else if (hour < 0)
        {
            hour += 24;
        }

        return hour;
    }

    private void ChangeMinute(bool isNext, bool isStartTime)
    {
        if (isStartTime)
        {
            _startTimeMinute = ChangeMinute(_startTimeMinute, isNext);
        }
        else
        {
            _endTimeMinute = ChangeMinute(_endTimeMinute, isNext);
        }

        UpdateTime();
    }

    private int ChangeMinute(int minute, bool isNext)
    {
        if (isNext)
        {
            minute += MinuteStep;
        }
        else
        {
            minute -= MinuteStep;
        }

        if (minute > 59)
        {
            minute -= 60;
        }
        else if (minute < 0)
        {
            minute += 60;
        }

        return minute;
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
                return _showMonthPickerAsOverlayInternal && _isMonthPickerOverlayOnTop is false && (_showTimePickerAsOverlayInternal is false || _isMonthPickerOverlayOnTop is false && _isTimePickerOverlayOnTop is false);
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
                return (_showMonthPickerAsOverlayInternal && _isMonthPickerOverlayOnTop) || (_showTimePickerAsOverlayInternal && _isMonthPickerOverlayOnTop && _isTimePickerOverlayOnTop is false);
            }
        }
        else
        {
            return _showMonthPickerAsOverlayInternal is false || (_showMonthPickerAsOverlayInternal && _isMonthPickerOverlayOnTop);
        }
    }

    private bool CanChangeTime(int? startTimeHour = null, int? startTimeMinute = null, int? endTimeHour = null, int? endTimeMinute = null)
    {
        if (MaxRange.HasValue is false) return true;

        var startTime = new TimeSpan(startTimeHour ?? _startTimeHour, startTimeMinute ?? _startTimeMinute, 0);
        var endTime = new TimeSpan(endTimeHour ?? _endTimeHour, endTimeMinute ?? _endTimeMinute, 0);
        var currentValueHasValue = CurrentValue?.StartDate is not null && CurrentValue.EndDate.HasValue;

        if (currentValueHasValue && CurrentValue!.StartDate!.Value.Date == CurrentValue.EndDate!.Value.Date && startTime > endTime)
        {
            return false;
        }

        if (currentValueHasValue)
        {
            var startDate = ChangeTimeInDateTimeOffset(CurrentValue!.StartDate!.Value, startTimeHour, startTimeMinute);
            var endDate = ChangeTimeInDateTimeOffset(CurrentValue!.EndDate!.Value, endTimeHour, endTimeMinute);
            var maxDate = new DateTimeOffset(GetMaxEndDate(), CurrentValue!.StartDate.Value.Offset);
            var minDate = new DateTimeOffset(GetMinEndDate(), CurrentValue!.StartDate.Value.Offset);

            return startDate >= minDate && endDate <= maxDate;
        }

        return new TimeSpan(MaxRange.Value.Hours, MaxRange.Value.Minutes, MaxRange.Value.Seconds).TotalMinutes > Math.Abs((startTime - endTime).TotalMinutes);
    }

    private DateTimeOffset ChangeTimeInDateTimeOffset(DateTimeOffset dateTime, int? hour, int? minute)
    {
        return new DateTimeOffset(dateTime.Year,
                                  dateTime.Month,
                                  dateTime.Day,
                                  hour ?? dateTime.Hour,
                                  minute ?? dateTime.Minute,
                                  dateTime.Second,
                                  dateTime.Offset);
    }

    private bool IsIncreaseOrDecreaseButtonDisabled(bool isNext, bool isHour, bool isStartTime)
    {
        if (IsEnabled is false) return true;
        if (MaxRange.HasValue is false) return false;

        var startTimeHour = _startTimeHour;
        var endTimeHour = _endTimeHour;
        var startTimeMinute = _startTimeMinute;
        var endTimeMinute = _endTimeMinute;
        if (isHour)
        {
            if (isStartTime)
            {
                startTimeHour = ChangeHour(startTimeHour, isNext);
            }
            else
            {
                endTimeHour = ChangeHour(endTimeHour, isNext);
            }
        }
        else
        {
            if (isStartTime)
            {
                startTimeMinute = ChangeMinute(startTimeMinute, isNext);
            }
            else
            {
                endTimeMinute = ChangeMinute(endTimeMinute, isNext);
            }
        }

        return IsButtonDisabled(startTimeHour, startTimeMinute, endTimeHour, endTimeMinute);
    }

    private bool IsAmPmButtonDisabled(bool isAm, bool isStartTime)
    {
        if (MaxRange.HasValue is false) return false;

        var startTimeHour = _startTimeHour;
        var endTimeHour = _endTimeHour;

        if (isStartTime)
        {
            if (isAm)
            {
                startTimeHour %= 12;  // "12:-- am" is "00:--" in 24h
            }
            else
            {
                if (startTimeHour <= 12) // "12:-- pm" is "12:--" in 24h
                {
                    startTimeHour += 12;
                }

                startTimeHour %= 24;
            }
        }
        else
        {
            if (isAm)
            {
                endTimeHour %= 12;  // "12:-- am" is "00:--" in 24h
            }
            else
            {
                if (endTimeHour <= 12) // "12:-- pm" is "12:--" in 24h
                {
                    endTimeHour += 12;
                }

                endTimeHour %= 24;
            }
        }

        return IsButtonDisabled(startTimeHour, _startTimeMinute, endTimeHour, _endTimeMinute);
    }

    private bool IsButtonDisabled(int startTimeHour, int startTimeMinute, int endTimeHour, int endTimeMinute)
    {
        if (MaxRange.HasValue is false) return false;

        var startTime = new TimeSpan(startTimeHour, startTimeMinute, 0);
        var endTime = new TimeSpan(endTimeHour, endTimeMinute, 0);

        if (CurrentValue?.StartDate is not null && CurrentValue.EndDate.HasValue)
        {
            var startDate = ChangeTimeInDateTimeOffset(CurrentValue!.StartDate!.Value, startTimeHour, startTimeMinute);
            var endDate = ChangeTimeInDateTimeOffset(CurrentValue!.EndDate!.Value, endTimeHour, endTimeMinute);
            if (startDate > endDate)
            {
                return true;
            }

            var maxDate = new DateTimeOffset(GetMaxEndDate(), CurrentValue!.StartDate.Value.Offset);
            var minDate = new DateTimeOffset(GetMinEndDate(), CurrentValue!.StartDate.Value.Offset);

            return startDate < minDate || endDate > maxDate;
        }

        return new TimeSpan(MaxRange.Value.Hours, MaxRange.Value.Minutes, MaxRange.Value.Seconds).TotalMinutes < Math.Abs((startTime - endTime).TotalMinutes);
    }

    private DateTime GetMaxEndDate()
    {
        return CurrentValue!.StartDate!.Value.DateTime.AddDays(MaxRange!.Value.TotalDays);
    }

    private DateTime GetMinEndDate()
    {
        return CurrentValue!.StartDate!.Value.DateTime.AddDays(-1 * MaxRange!.Value.TotalDays);
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
        if (IsEnabled is false || IsDisposed) return false;

        return await _js.BitCalloutToggleCallout(_dotnetObj,
                                       _dateRangePickerId,
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
        List<string> classes = ["bit-dtrp-cal"];

        if (Classes?.Callout is not null)
        {
            classes.Add(Classes.Callout);
        }

        if (Responsive)
        {
            classes.Add("bit-dtrp-res");
        }

        return string.Join(' ', classes).Trim();
    }



    protected override async ValueTask DisposeAsync(bool disposing)
    {
        if (IsDisposed || disposing is false) return;

        await base.DisposeAsync(disposing);

        _cancellationTokenSource?.Dispose();
        OnValueChanged -= HandleOnValueChanged;

        try
        {
            await _js.BitCalloutClearCallout(_calloutId);
            await _js.BitSwipesDispose(_calloutId);
        }
        catch (JSDisconnectedException) { } // we can ignore this exception here
    }
}
