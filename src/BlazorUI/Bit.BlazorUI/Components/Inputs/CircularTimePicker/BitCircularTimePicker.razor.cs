using System.Globalization;
using System.Diagnostics.CodeAnalysis;

namespace Bit.BlazorUI;

public partial class BitCircularTimePicker
{
    private const string FORMAT_24_HOURS = "HH:mm";
    private const string FORMAT_12_HOURS = "hh:mm tt";

    private bool isOpen;
    private CultureInfo culture = CultureInfo.CurrentUICulture;
    private BitIconLocation iconLocation = BitIconLocation.Right;
    private string focusClass = string.Empty;

    private bool _isPointerDown;
    private int _initialHour;
    private int _initialMinute;
    private int? _hour;
    private int? _minute;
    private string? _labelId;
    private string? _textFieldId;
    private string? _wrapperId;
    private string? _calloutId;
    private string? _overlayId;
    private BitCircularTimePickerDialMode _currentView = BitCircularTimePickerDialMode.Hours;
    private DotNetObjectReference<BitCircularTimePicker> _dotnetObj = default!;
    private string _timeFormat => TimeFormat ?? (AmPm ? FORMAT_12_HOURS : FORMAT_24_HOURS);
    private string _focusClass
    {
        get => focusClass;
        set
        {
            focusClass = value;
            ClassBuilder.Reset();
        }
    }

    [Inject] private IJSRuntime _js { get; set; } = default!;

    /// <summary>
    /// Label for the TimePicker
    /// </summary>
    [Parameter] public string? Label { get; set; }

    /// <summary>
    /// Shows the custom label for text field
    /// </summary>
    [Parameter] public RenderFragment? LabelTemplate { get; set; }

    /// <summary>
    /// If true, sets 12 hour selection clock.
    /// </summary>
    [Parameter] public bool AmPm { get; set; }

    /// <summary>
    /// Choose the edition mode. By default, you can edit hours and minutes.
    /// </summary>
    [Parameter] public BitCircularTimePickerEditMode EditMode { get; set; } = BitCircularTimePickerEditMode.Normal;

    /// <summary>
    /// Whether the TimePicker allows input a date string directly or not
    /// </summary>
    [Parameter] public bool AllowTextInput { get; set; }

    /// <summary>
    /// The tabIndex of the TextField.
    /// </summary>
    [Parameter] public int TabIndex { get; set; }

    /// <summary>
    /// Placeholder text for the TimePicker.
    /// </summary>
    [Parameter] public string? Placeholder { get; set; }

    /// <summary>
    /// Custom TimePicker icon template
    /// </summary>
    [Parameter] public RenderFragment? IconTemplate { get; set; }

    /// <summary>
    /// TimePicker icon location
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
    /// Optional TimePicker icon
    /// </summary>
    [Parameter] public string IconName { get; set; } = "Clock";

    /// <summary>
    /// Whether or not this TimePicker is open
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
    /// Capture and render additional attributes in addition to the main callout's parameters
    /// </summary>
    [Parameter] public Dictionary<string, object> CalloutHtmlAttributes { get; set; } = [];

    /// <summary>
    /// Aria label for time picker popup for screen reader users.
    /// </summary>
    [Parameter] public string PickerAriaLabel { get; set; } = "Clock";

    /// <summary>
    /// Enables the responsive mode in small screens
    /// </summary>
    [Parameter] public bool IsResponsive { get; set; }

    /// <summary>
    /// Callback for when clicking on TimePicker input
    /// </summary>
    [Parameter] public EventCallback OnClick { get; set; }

    /// <summary>
    /// Callback for when focus moves into the input.
    /// </summary>
    [Parameter] public EventCallback OnFocus { get; set; }

    /// <summary>
    /// Callback for when focus moves into the TimePicker input.
    /// </summary>
    [Parameter] public EventCallback OnFocusIn { get; set; }

    /// <summary>
    /// Callback for when focus moves out the TimePicker input.
    /// </summary>
    [Parameter] public EventCallback OnFocusOut { get; set; }

    /// <summary>
    /// Whether or not the Text field of the TimePicker is underlined.
    /// </summary>
    [Parameter] public bool IsUnderlined { get; set; }

    /// <summary>
    /// Determines if the TimePicker has a border.
    /// </summary>
    [Parameter] public bool HasBorder { get; set; } = true;

    /// <summary>
    /// CultureInfo for the TimePicker
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
    /// The format of the time in the TimePicker
    /// </summary>
    [Parameter] public string? TimeFormat { get; set; }

    /// <summary>
    /// The custom validation error message for the invalid value.
    /// </summary>
    [Parameter] public string? InvalidErrorMessage { get; set; }

    /// <summary>
    /// Callback for when the time changes.
    /// </summary>
    [Parameter] public EventCallback<TimeSpan?> OnSelectTime { get; set; }

    /// <summary>
    /// If AutoClose is set to true and PickerActions are defined, the hour and the minutes can be defined without any action.
    /// </summary>
    [Parameter] public bool AutoClose { get; set; }


    [JSInvokable("CloseCallout")]
    public void CloseCalloutBeforeAnotherCalloutIsOpened() => IsOpen = false;

    public Task OpenCallout() => HandleOnClick();

    protected override string RootElementClass => "bit-ctp";

    protected override void RegisterCssClasses()
    {
        ClassBuilder.Register(() => IconLocation is BitIconLocation.Left ? $"{RootElementClass}-lfic" : string.Empty);

        ClassBuilder.Register(() => IsUnderlined ? $"{RootElementClass}-und" : string.Empty);

        ClassBuilder.Register(() => HasBorder is false ? $"{RootElementClass}-no-brd" : string.Empty);

        ClassBuilder.Register(() => _focusClass);
    }

    protected override void OnInitialized()
    {
        _labelId = $"BitCircularTimePicker-{UniqueId}-label";
        _wrapperId = $"BitCircularTimePicker-{UniqueId}-wrapper";
        _calloutId = $"BitCircularTimePicker-{UniqueId}-callout";
        _overlayId = $"BitCircularTimePicker-{UniqueId}-overlay";
        _textFieldId = $"BitCircularTimePicker-{UniqueId}-text-field";

        _hour = CurrentValue?.Hours;
        _minute = CurrentValue?.Minutes;

        _initialHour = _hour.GetValueOrDefault();
        _initialMinute = _minute.GetValueOrDefault();

        _dotnetObj = DotNetObjectReference.Create(this);

        OnValueChanged += HandleOnValueChanged;

        base.OnInitialized();
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

    private async Task CloseCallout()
    {
        await _js.InvokeVoidAsync("BitCircularTimePicker.toggleTimePickerCallout", _dotnetObj, _Id, _calloutId, _overlayId, IsOpen, IsResponsive);
        IsOpen = false;
        StateHasChanged();
    }

    private async Task HandleOnChange(ChangeEventArgs e)
    {
        if (IsEnabled is false) return;
        if (ValueHasBeenSet && ValueChanged.HasDelegate is false) return;
        if (AllowTextInput is false) return;

        CurrentValueAsString = e.Value?.ToString();
        await OnSelectTime.InvokeAsync(CurrentValue);
    }

    private async Task HandleOnClick()
    {
        if (IsEnabled is false) return;

        await _js.InvokeVoidAsync("BitCircularTimePicker.toggleTimePickerCallout", _dotnetObj, _Id, _calloutId, _overlayId, IsOpen, IsResponsive);

        IsOpen = !IsOpen;

        await OnClick.InvokeAsync();
    }

    private string GetTransformStyle(int index, double radius, double offsetX, double offsetY)
    {
        double angle = (6 - index) * 30 / 180d * Math.PI;
        var x = (Math.Sin(angle) * radius + offsetX).ToString("F3", CultureInfo.InvariantCulture);
        var y = ((Math.Cos(angle) + 1) * radius + offsetY).ToString("F3", CultureInfo.InvariantCulture);
        return $"{x}px, {y}px";
    }

    private string GetHoursMinutesClass(int value) =>
        (_currentView == BitCircularTimePickerDialMode.Hours && GetHours() == value) || (_currentView == BitCircularTimePickerDialMode.Minutes && _minute == value)
            ? "bit-ctp-sel"
            : string.Empty;

    private int GetClockHandHeightPercent() => (_currentView == BitCircularTimePickerDialMode.Hours && AmPm is false && _hour > 0 && _hour < 13) ? 26 : 40;

    private double GetPointerDegree() => _currentView switch
    {
        BitCircularTimePickerDialMode.Hours => (_hour.GetValueOrDefault() * 30) % 360,
        BitCircularTimePickerDialMode.Minutes => (_minute.GetValueOrDefault() * 6) % 360,
        _ => 0
    };

    private async Task HandleOnHourClockHandClick(int hour)
    {
        _hour = hour;
        if (AmPm)
        {
            if (IsAm() && _hour == 12)
            {
                _hour = 0;
            }
            else if (IsAm() is false && _hour < 12)
            {
                _hour += 12;
            }
        }
        await UpdateCurrentValue();

        if (EditMode == BitCircularTimePickerEditMode.Normal)
        {
            _currentView = BitCircularTimePickerDialMode.Minutes;
        }
        else if (EditMode == BitCircularTimePickerEditMode.OnlyHours)
        {
            if (AutoClose)
            {
                await CloseCallout();
            }
        }
    }

    private async Task HandleOnHourPointerOver(int hour)
    {
        if (_isPointerDown is false) return;

        _hour = hour;
        await UpdateCurrentValue();
    }

    private void HandleOnPointerDown(MouseEventArgs e) => _isPointerDown = true;

    private async Task HandleOnPointerUp(MouseEventArgs e)
    {
        if ((_isPointerDown && _currentView == BitCircularTimePickerDialMode.Minutes && _minute.HasValue && _minute.Value != _initialMinute) ||
            (_currentView == BitCircularTimePickerDialMode.Hours && _hour.HasValue && _hour.Value != _initialHour && EditMode == BitCircularTimePickerEditMode.OnlyHours))
        {
            _isPointerDown = false;
            if (AutoClose)
            {
                await CloseCallout();
            }
        }

        _isPointerDown = false;

        if (_currentView == BitCircularTimePickerDialMode.Hours && _hour.HasValue && _hour.Value != _initialHour && EditMode == BitCircularTimePickerEditMode.Normal)
        {
            await Task.Run(() => _currentView = BitCircularTimePickerDialMode.Minutes);
        }
    }

    private async Task HandleOnMinutePointerOver(int value)
    {
        if (_isPointerDown is false) return;

        _minute = value;
        await UpdateCurrentValue();
    }

    private async Task HandleOnMinuteClockHandClick(int value)
    {
        _minute = value;
        await UpdateCurrentValue();

        if (AutoClose)
        {
            await CloseCallout();
        }
    }

    private async Task UpdateCurrentValue()
    {
        CurrentValue = (_hour.HasValue is false || _minute.HasValue is false) ? null : new TimeSpan(_hour.Value, _minute.Value, 0);

        await OnSelectTime.InvokeAsync(CurrentValue);
    }

    private string GetHourString()
    {
        if (_hour.HasValue is false) return "--";

        var hours = AmPm ? GetAmPmHours(_hour.Value) : _hour.Value;
        return Math.Min(23, Math.Max(0, hours)).ToString(CultureInfo.InvariantCulture);
    }

    private string GetMinuteString() => _minute.HasValue ? $"{Math.Min(59, Math.Max(0, _minute.Value)):D2}" : "--";

    private int GetAmPmHours(int hours)
    {
        var result = hours % 12;
        return result == 0 ? 12 : result;
    }

    private int GetHours() => AmPm ? GetAmPmHours(_hour.GetValueOrDefault()) : _hour.GetValueOrDefault();

    private void HandleOnHourClick() => _currentView = BitCircularTimePickerDialMode.Hours;

    private void HandleOnMinuteClick() => _currentView = BitCircularTimePickerDialMode.Minutes;

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

    private void HandleOnValueChanged(object? sender, EventArgs args)
    {
        _hour = CurrentValue?.Hours;
        _minute = CurrentValue?.Minutes;
    }

    private bool IsAm() => _hour.GetValueOrDefault() >= 00 && _hour < 12; // am is 00:00 to 11:59 

    /// <inheritdoc />
    protected override bool TryParseValueFromString(string? value, [MaybeNullWhen(false)] out TimeSpan? result, [NotNullWhen(false)] out string? validationErrorMessage)
    {
        if (value.HasNoValue())
        {
            _hour = null;
            _minute = null;
            result = null;
            validationErrorMessage = null;
            return true;
        }

        if (DateTime.TryParseExact(value, _timeFormat ?? Culture.DateTimeFormat.ShortTimePattern, Culture, DateTimeStyles.None, out DateTime parsedValue))
        {
            result = parsedValue.TimeOfDay;
            _hour = result.Value.Hours;
            _minute = result.Value.Minutes;
            validationErrorMessage = null;
            return true;
        }

        result = default;
        validationErrorMessage = InvalidErrorMessage.HasValue() ? InvalidErrorMessage! : $"The {DisplayName ?? FieldIdentifier.FieldName} field is not valid.";
        return false;
    }

    protected override string? FormatValueAsString(TimeSpan? value)
    {
        if (value.HasValue is false) return null;

        DateTime time = DateTime.Today.Add(value.Value);
        return time.ToString(_timeFormat ?? Culture.DateTimeFormat.ShortTimePattern, Culture);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _dotnetObj.Dispose();
            OnValueChanged -= HandleOnValueChanged;
        }

        base.Dispose(disposing);
    }
}
