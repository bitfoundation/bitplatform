using System.Diagnostics.CodeAnalysis;
using System.Globalization;

namespace Bit.BlazorUI;
public partial class BitTimePicker
{
    protected override bool UseVisual => false;

    private const string FORMAT_24_HOURS = "HH:mm";
    private const string FORMAT_12_HOURS = "hh:mm tt";

    private bool isOpen;
    private CultureInfo culture = CultureInfo.CurrentUICulture;
    private BitIconLocation iconLocation = BitIconLocation.Right;
    private string focusClass = string.Empty;

    private bool _isMouseDown;
    private int _initialHour;
    private int _initialMinute;
    private int? _hour;
    private int? _minute;
    private BitTimePickerDial _currentView = BitTimePickerDial.Hours;
    private DotNetObjectReference<BitTimePicker> _dotnetObj = default!;
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
    [Parameter] public RenderFragment? LabelFragment { get; set; }

    /// <summary>
    /// If true, sets 12 hour selection clock.
    /// </summary>
    [Parameter] public bool AmPm { get; set; }

    /// <summary>
    /// Choose the edition mode. By default, you can edit hours and minutes.
    /// </summary>
    [Parameter] public BitTimeEditMode TimeEditMode { get; set; } = BitTimeEditMode.Normal;

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
    [Parameter] public RenderFragment? IconFragment { get; set; }

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
    [Parameter] public BitIconName IconName { get; set; } = BitIconName.Clock;

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
    [Parameter] public Dictionary<string, object> CalloutHtmlAttributes { get; set; } = new Dictionary<string, object>();

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

    public string LabelId => $"TimePicker-Label-{UniqueId}";
    public string TextFieldId => $"TimePicker-TextField-{UniqueId}";
    public string WrapperId => $"TimePicker-Wrapper-{UniqueId}";
    public string CalloutId => $"TimePicker-Callout-{UniqueId}";
    public string OverlayId => $"TimePicker-Overlay-{UniqueId}";

    protected override string RootElementClass => "bit-tp";

    [JSInvokable("CloseCallout")]
    public void CloseCalloutBeforeAnotherCalloutIsOpened()
    {
        IsOpen = false;
    }

    protected override void OnInitialized()
    {
        _hour = CurrentValue?.Hours;
        _minute = CurrentValue?.Minutes;

        _initialHour = _hour.GetValueOrDefault();
        _initialMinute = _minute.GetValueOrDefault();

        _dotnetObj = DotNetObjectReference.Create(this);

        OnValueChanged += HandleOnValueChanged;

        base.OnInitialized();
    }

    protected override void RegisterComponentClasses()
    {
        ClassBuilder.Register(() => IconLocation is BitIconLocation.Left ? "left-icon" : string.Empty);

        ClassBuilder.Register(() => IsUnderlined ? "underlined" : string.Empty);

        ClassBuilder.Register(() => HasBorder is false ? "no-border" : string.Empty);

        ClassBuilder.Register(() => _focusClass);
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

    private async Task CloseCallout()
    {
        await _js.InvokeVoidAsync("BitTimePicker.toggleTimePickerCallout", _dotnetObj, UniqueId, CalloutId, OverlayId, IsOpen, IsResponsive);
        IsOpen = false;
        StateHasChanged();
    }

    private async Task HandleChange(ChangeEventArgs e)
    {
        if (IsEnabled is false) return;
        if (ValueHasBeenSet && ValueChanged.HasDelegate is false) return;
        if (AllowTextInput is false) return;

        CurrentValueAsString = e.Value?.ToString();
        await OnSelectTime.InvokeAsync(CurrentValue);
    }

    private async Task HandleClick()
    {
        if (IsEnabled is false) return;

        await _js.InvokeVoidAsync("BitTimePicker.toggleTimePickerCallout", _dotnetObj, UniqueId, CalloutId, OverlayId, IsOpen, IsResponsive);

        IsOpen = !IsOpen;

        await OnClick.InvokeAsync();
    }

    private string GetTransformStyle(double angle, double radius, double offsetX, double offsetY)
    {
        angle = angle / 180 * Math.PI;
        var x = (Math.Sin(angle) * radius + offsetX).ToString("F3", CultureInfo.InvariantCulture);
        var y = ((Math.Cos(angle) + 1) * radius + offsetY).ToString("F3", CultureInfo.InvariantCulture);
        return $"transform: translate({x}px, {y}px);";
    }

    private string GetNumberClass(int number)
    {
        if (_currentView == BitTimePickerDial.Hours)
        {
            var h = _hour.GetValueOrDefault();
            if (AmPm)
            {
                h = _hour.GetValueOrDefault() % 12;
                if (h == 0)
                    h = 12;
            }
            if (h == number)
                return $"selected";
        }
        else if (_currentView == BitTimePickerDial.Minutes && _minute == number)
        {
            return $"selected";
        }

        return string.Empty;
    }

    private string GetPointerHeight()
    {
        var height = 40;
        if (_currentView == BitTimePickerDial.Minutes)
            height = 40;
        if (_currentView == BitTimePickerDial.Hours)
        {
            if (!AmPm && _hour > 0 && _hour < 13)
                height = 26;
            else
                height = 40;
        }
        return $"{height}%;";
    }

    private string GetPointerRotation()
    {
        double deg = 0;
        if (_currentView == BitTimePickerDial.Hours)
            deg = (_hour.GetValueOrDefault() * 30) % 360;
        if (_currentView == BitTimePickerDial.Minutes)
            deg = (_minute.GetValueOrDefault() * 6) % 360;
        return $"rotateZ({deg}deg);";
    }

    private string GetClockPointerThumbClass()
    {
        double deg = 0;
        if (_currentView == BitTimePickerDial.Hours)
            deg = (_hour.GetValueOrDefault() * 30) % 360;
        if (_currentView == BitTimePickerDial.Minutes)
            deg = (_minute.GetValueOrDefault() * 6) % 360;

        if (deg % 30 == 0)
        {
            return string.Empty;
        }
        else
        {
            return "min";
        }
    }

    private async Task HandleOnClickHour(int value)
    {
        var hour = value;
        if (AmPm)
        {
            if (IsAm() && value == 12)
                hour = 0;
            else if (IsAm() is false && value < 12)
                hour = value + 12;
        }
        _hour = hour;
        await UpdateCurrentValue();

        if (TimeEditMode == BitTimeEditMode.Normal)
        {
            _currentView = BitTimePickerDial.Minutes;
        }
        else if (TimeEditMode == BitTimeEditMode.OnlyHours)
        {
            if (AutoClose)
            {
                await CloseCallout();
            }
        }
    }

    private async Task HandleOnMouseOverHour(int value)
    {
        if (_isMouseDown)
        {
            _hour = value;
            await UpdateCurrentValue();
        }
    }

    private void OnMouseDown(MouseEventArgs e)
    {
        _isMouseDown = true;
    }

    private async Task HandleOnMouseUp(MouseEventArgs e)
    {
        if (_isMouseDown && _currentView == BitTimePickerDial.Minutes && _minute != _initialMinute || _currentView == BitTimePickerDial.Hours && _hour != _initialHour && TimeEditMode == BitTimeEditMode.OnlyHours)
        {
            _isMouseDown = false;
            if (AutoClose)
            {
                await CloseCallout();
            }
        }

        _isMouseDown = false;

        if (_currentView == BitTimePickerDial.Hours && _hour != _initialHour && TimeEditMode == BitTimeEditMode.Normal)
        {
            _currentView = BitTimePickerDial.Minutes;
        }
    }

    private async Task HandleOnMouseOverMinute(int value)
    {
        if (_isMouseDown)
        {
            _minute = value;
            await UpdateCurrentValue();
        }
    }

    private async Task HandleOnClickMinute(int value)
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
        if (_hour.HasValue is false || _minute.HasValue is false)
        {
            CurrentValue = null;
        }
        else
        {
            CurrentValue = new TimeSpan(_hour.Value, _minute.Value, 0);
        }

        await OnSelectTime.InvokeAsync(CurrentValue);
    }

    private string GetHourString()
    {
        if (CurrentValue.HasValue is false)
            return "--";
        var h = AmPm ? GetAmPmHour(CurrentValue.Value) : CurrentValue.Value.Hours;
        return Math.Min(23, Math.Max(0, h)).ToString(CultureInfo.InvariantCulture);
    }

    private string GetMinuteString()
    {
        if (CurrentValue.HasValue is false)
            return "--";
        return $"{Math.Min(59, Math.Max(0, CurrentValue.Value.Minutes)):D2}";
    }

    private int GetAmPmHour(TimeSpan time)
    {
        var hour = time.Hours % 12;
        if (hour == 0)
            hour = 12;
        return hour;
    }

    private void HandleOnClickHour()
    {
        _currentView = BitTimePickerDial.Hours;
    }

    private void HandleOnClickMinute()
    {
        _currentView = BitTimePickerDial.Minutes;
    }

    private async Task HandleOnClickAm()
    {
        _hour %= 12;  // "12:-- am" is "00:--" in 24h
        await UpdateCurrentValue();
    }

    private async Task HandleOnClickPm()
    {
        if (_hour <= 12) // "12:-- pm" is "12:--" in 24h
            _hour += 12;
        _hour %= 24;
        await UpdateCurrentValue();
    }

    private void HandleOnValueChanged(object? sender, EventArgs args)
    {
        _hour = CurrentValue?.Hours;
        _minute = CurrentValue?.Minutes;
    }

    private bool IsAm() => _hour.GetValueOrDefault() >= 00 && _hour < 12; // am is 00:00 to 11:59 

    public async Task OpenCallout()
    {
        await HandleClick();
    }

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
        if (value.HasValue)
        {
            DateTime time = DateTime.Today.Add(value.Value);
            return time.ToString(_timeFormat ?? Culture.DateTimeFormat.ShortTimePattern, Culture);
        }
        else
        {
            return null;
        }
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            OnValueChanged -= HandleOnValueChanged;
            _dotnetObj.Dispose();
        }

        base.Dispose(disposing);
    }
}
