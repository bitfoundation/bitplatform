using System.Globalization;
using System.Diagnostics.CodeAnalysis;
using System.Text;

namespace Bit.BlazorUI;

public partial class BitCircularTimePicker
{
    private const string FORMAT_24_HOURS = "HH:mm";
    private const string FORMAT_12_HOURS = "hh:mm tt";



    private bool IsOpenHasBeenSet;

    private bool isOpen;
    private string? focusClass;
    private CultureInfo culture = CultureInfo.CurrentUICulture;
    private BitIconLocation iconLocation = BitIconLocation.Right;

    private int? _hour;
    private int? _minute;
    private string? _labelId;
    private string? _inputId;
    private bool _isPointerDown;
    private bool _showHourView = true;
    private string _calloutId = string.Empty;
    private string _circularTimePickerId = string.Empty;
    private string? _pointerUpAbortControllerId;
    private string? _pointerMoveAbortControllerId;
    private ElementReference _clockRef;
    private DotNetObjectReference<BitCircularTimePicker> _dotnetObj = default!;
    private string? _focusClass
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
    /// Whether the TimePicker allows input a date string directly or not
    /// </summary>
    [Parameter] public bool AllowTextInput { get; set; }

    /// <summary>
    /// If AutoClose is set to true and PickerActions are defined, the hour and the minutes can be defined without any action.
    /// </summary>
    [Parameter] public bool AutoClose { get; set; }

    /// <summary>
    /// Aria label for time picker popup for screen reader users.
    /// </summary>
    [Parameter] public string CalloutAriaLabel { get; set; } = "Clock";

    /// <summary>
    /// Capture and render additional attributes in addition to the main callout's parameters
    /// </summary>
    [Parameter] public Dictionary<string, object> CalloutHtmlAttributes { get; set; } = [];

    /// <summary>
    /// Custom CSS classes for different parts of the TimePicker component.
    /// </summary>
    [Parameter] public BitCircularTimePickerClassStyles? Classes { get; set; }

    /// <summary>
    /// The title of the close button (tooltip).
    /// </summary>
    [Parameter] public string CloseButtonTitle { get; set; } = "Close time picker";

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
    /// Choose the edition mode. By default, you can edit hours and minutes.
    /// </summary>
    [Parameter] public BitCircularTimePickerEditMode EditMode { get; set; } = BitCircularTimePickerEditMode.Normal;

    /// <summary>
    /// Determines if the TimePicker has a border.
    /// </summary>
    [Parameter] public bool HasBorder { get; set; } = true;

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
    /// Custom TimePicker icon template
    /// </summary>
    [Parameter] public RenderFragment? IconTemplate { get; set; }

    /// <summary>
    /// The custom validation error message for the invalid value.
    /// </summary>
    [Parameter] public string? InvalidErrorMessage { get; set; }

    /// <summary>
    /// Whether or not this TimePicker is open
    /// </summary>
    public bool IsOpen
    {
        get => isOpen;
        set
        {
            if (isOpen == value) return;

            isOpen = value;

            ClassBuilder.Reset();

            _ = IsOpenChanged.InvokeAsync(value);
        }
    }

    [Parameter] public EventCallback<bool> IsOpenChanged { get; set; }

    /// <summary>
    /// Enables the responsive mode in small screens
    /// </summary>
    [Parameter] public bool IsResponsive { get; set; }

    /// <summary>
    /// Whether or not the Text field of the TimePicker is underlined.
    /// </summary>
    [Parameter] public bool IsUnderlined { get; set; }

    /// <summary>
    /// Label for the TimePicker
    /// </summary>
    [Parameter] public string? Label { get; set; }

    /// <summary>
    /// Shows the custom label for text field
    /// </summary>
    [Parameter] public RenderFragment? LabelTemplate { get; set; }

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
    /// Callback for when the time changes.
    /// </summary>
    [Parameter] public EventCallback<TimeSpan?> OnSelectTime { get; set; }

    /// <summary>
    /// Placeholder text for the TimePicker.
    /// </summary>
    [Parameter] public string? Placeholder { get; set; }

    /// <summary>
    /// Whether the TimePicker's close button should be shown or not.
    /// </summary>
    [Parameter] public bool ShowCloseButton { get; set; }

    /// <summary>
    /// Custom CSS styles for different parts of the TimePicker component.
    /// </summary>
    [Parameter] public BitCircularTimePickerClassStyles? Styles { get; set; }

    /// <summary>
    /// The tabIndex of the TextField.
    /// </summary>
    [Parameter] public int TabIndex { get; set; }

    /// <summary>
    /// The time format of the time-picker, 24H or 12H.
    /// </summary>
    [Parameter] public BitTimeFormat TimeFormat { get; set; }

    /// <summary>
    /// The format of the time in the TimePicker
    /// </summary>
    [Parameter] public string? ValueFormat { get; set; }



    public string? InputId => _inputId;



    [JSInvokable("CloseCallout")]
    public void CloseCalloutBeforeAnotherCalloutIsOpened()
    {
        if (IsEnabled is false) return;
        if (IsOpenHasBeenSet && IsOpenChanged.HasDelegate is false) return;

        IsOpen = false;
        StateHasChanged();
    }

    [JSInvokable(nameof(HandlePointerUp))]
    public async Task HandlePointerUp(MouseEventArgs e)
    {
        if (_isPointerDown is false) return;

        _isPointerDown = false;

        if (AutoClose && ((_showHourView is false && _minute.HasValue) || (_showHourView && EditMode == BitCircularTimePickerEditMode.OnlyHours)))
        {
            await CloseCallout();
        }

        if (_showHourView && _hour.HasValue && EditMode == BitCircularTimePickerEditMode.Normal)
        {
            _showHourView = false;
        }

        StateHasChanged();
    }

    [JSInvokable(nameof(HandlePointerMove))]
    public async Task HandlePointerMove(MouseEventArgs e)
    {
        if (_isPointerDown is false) return;

        await UpdateTime(e);
    }

    public Task OpenCallout() => HandleOnClick();

    protected override string RootElementClass => "bit-ctp";

    protected override void RegisterCssClasses()
    {
        ClassBuilder.Register(() => Classes?.Root);

        ClassBuilder.Register(() => IconLocation is BitIconLocation.Left ? "bit-ctp-lic" : string.Empty);

        ClassBuilder.Register(() => IsUnderlined ? "bit-ctp-und" : string.Empty);

        ClassBuilder.Register(() => HasBorder ? string.Empty : "bit-ctp-nbd");

        ClassBuilder.Register(() => _focusClass);
    }

    protected override void RegisterCssStyles()
    {
        StyleBuilder.Register(() => Styles?.Root);
    }

    protected override void OnInitialized()
    {
        _circularTimePickerId = $"BitCircularTimePicker-{UniqueId}";
        _labelId = $"{_circularTimePickerId}-label";
        _inputId = $"{_circularTimePickerId}-input";
        _calloutId = $"{_circularTimePickerId}-callout";

        _hour = CurrentValue?.Hours;
        _minute = CurrentValue?.Minutes;

        _dotnetObj = DotNetObjectReference.Create(this);

        OnValueChanged += HandleOnValueChanged;

        base.OnInitialized();
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        await base.OnAfterRenderAsync(firstRender);

        if (firstRender is false) return;

        _pointerUpAbortControllerId = await _js.BitCircularTimePickerRegisterPointerUp(_dotnetObj, nameof(HandlePointerUp));
        _pointerMoveAbortControllerId = await _js.BitCircularTimePickerRegisterPointerMove(_dotnetObj, nameof(HandlePointerMove));
    }

    private async Task HandleOnFocusIn()
    {
        if (IsEnabled is false) return;

        _focusClass = "bit-ctp-foc";
        await OnFocusIn.InvokeAsync();
    }

    private async Task HandleOnFocusOut()
    {
        if (IsEnabled is false) return;

        _focusClass = null;
        await OnFocusOut.InvokeAsync();
    }

    private async Task HandleOnFocus()
    {
        if (IsEnabled is false) return;

        _focusClass = "bit-ctp-foc";
        await OnFocus.InvokeAsync();
    }

    private async Task CloseCallout()
    {
        if (IsEnabled is false) return;
        if (IsOpenHasBeenSet && IsOpenChanged.HasDelegate is false) return;

        IsOpen = false;
        await ToggleCallout();

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
        if (IsOpenHasBeenSet && IsOpenChanged.HasDelegate is false) return;

        _showHourView = true;
        IsOpen = true;
        await ToggleCallout();

        await OnClick.InvokeAsync();
    }

    private string GetTransformStyle(int index, double radius, double offsetX, double offsetY)
    {
        double angle = (6 - index) * 30 / 180d * Math.PI;
        var x = Math.Sin(angle) * radius + offsetX;
        var y = (Math.Cos(angle) + 1) * radius + offsetY;
        return $"{x:F3}px, {y:F3}px";
    }

    private string GetHoursMinutesClass(int hourMinute)
    {
        StringBuilder classes = new();

        if (Classes?.ClockNumber.HasValue() ?? false)
        {
            classes.Append(Classes.ClockNumber);
        }

        if ((_showHourView && GetHours() == hourMinute) || (_showHourView is false && _minute.GetValueOrDefault() == hourMinute))
        {
            if (classes.Length > 0)
            {
                classes.Append(' ');
            }

            classes.Append("bit-ctp-sel");

            if ((Classes?.ClockSelectedNumber.HasValue() ?? false))
            {
                classes.Append(' ').Append(Classes.ClockSelectedNumber);
            }
        }

        return classes.ToString();
    }

    private string GetHoursMinutesStyle(int hourMinute, int index, double radius, double offsetX, double offsetY)
    {
        StringBuilder styles = new();

        styles.Append($"transform: translate({GetTransformStyle(index, radius, offsetX, offsetY)}); ");

        if (Styles?.ClockNumber.HasValue() ?? false)
        {
            styles.Append(' ').Append(Styles.ClockNumber);
        }

        if ((Styles?.ClockSelectedNumber.HasValue() ?? false) && ((_showHourView && GetHours() == hourMinute) || (_showHourView is false && _minute.GetValueOrDefault() == hourMinute)))
        {
            styles.Append(' ').Append(Styles.ClockSelectedNumber);
        }

        return styles.ToString();
    }

    private int GetClockHandHeightPercent() => (_showHourView && TimeFormat == BitTimeFormat.TwentyFourHours && _hour > 0 && _hour < 13) ? 26 : 40;

    private double GetPointerDegree() => _showHourView ? ((_hour.GetValueOrDefault() * 30) % 360) : ((_minute.GetValueOrDefault() * 6) % 360);

    private async Task HandleOnPointerDown(MouseEventArgs e)
    {
        _isPointerDown = true;

        await UpdateTime(e);
    }

    private async Task UpdateCurrentValue()
    {
        CurrentValue = (_hour.HasValue is false || _minute.HasValue is false) ? null : new TimeSpan(_hour.Value, _minute.Value, 0);

        await OnSelectTime.InvokeAsync(CurrentValue);
    }

    private string GetHourString()
    {
        if (_hour.HasValue is false) return "--";

        var hours = TimeFormat == BitTimeFormat.TwelveHours ? GetAmPmHours(_hour.Value) : _hour.Value;
        return Math.Min(23, Math.Max(0, hours)).ToString(CultureInfo.InvariantCulture);
    }

    private string GetMinuteString() => _minute.HasValue ? $"{Math.Min(59, Math.Max(0, _minute.Value)):D2}" : "--";

    private int GetAmPmHours(int hours)
    {
        var result = hours % 12;
        return result == 0 ? 12 : result;
    }

    private int GetHours() => TimeFormat == BitTimeFormat.TwelveHours ? GetAmPmHours(_hour.GetValueOrDefault()) : _hour.GetValueOrDefault();

    private void HandleOnHourClick() => _showHourView = true;

    private void HandleOnMinuteClick() => _showHourView = false;

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

    private string GetValueFormat()
    {
        return ValueFormat.HasValue() ? ValueFormat! : (TimeFormat == BitTimeFormat.TwentyFourHours ? FORMAT_24_HOURS : FORMAT_12_HOURS);
    }

    private async Task ToggleCallout()
    {
        if (IsEnabled is false) return;

        await _js.ToggleCallout(_dotnetObj,
                                _circularTimePickerId,
                                _calloutId,
                                IsOpen,
                                IsResponsive ? BitResponsiveMode.Top : BitResponsiveMode.None,
                                BitDropDirection.TopAndBottom,
                                Dir is BitDir.Rtl,
                                "",
                                0,
                                "",
                                "",
                                false,
                                RootElementClass);
    }

    private async Task UpdateTime(MouseEventArgs e)
    {
        if (IsOpen is false) return;
        if (ValueHasBeenSet && ValueChanged.HasDelegate is false) return;

        var rect = await _js.GetBoundingClientRect(_clockRef);
        var radius = rect.Width / 2;
        var centerX = radius;
        var centerY = radius;
        var sections = _showHourView ? 12 : 60;
        var angleIncrement = Math.PI * 2 / sections;
        var startAngleOffset = Math.PI / -2; // To start the first section at the top

        var mouseX = e.ClientX - rect.Left;
        var mouseY = e.ClientY - rect.Top;

        var angle = Math.Atan2(mouseY - centerY, mouseX - centerX);
        var section = Math.Round((angle - startAngleOffset) / angleIncrement);
        if (section < 0)
        {
            section += sections;
        }

        if (_showHourView)
        {
            if (TimeFormat == BitTimeFormat.TwelveHours)
            {
                _hour = (int)section + 1;

                if (IsAm() && _hour == 12)
                {
                    _hour = 0;
                }
                else if (IsAm() is false && _hour < 12)
                {
                    _hour += 12;
                }
            }
            else
            {
                _hour = (int)section;

                var circleCenterX = rect.Left + rect.Width / 2;
                var circleCenterY = rect.Top + rect.Height / 2;
                var distanceX = e.ClientX - circleCenterX;
                var distanceY = e.ClientY - circleCenterY;
                var distance = Math.Sqrt(distanceX * distanceX + distanceY * distanceY);
                var isWithInSmallCircle = distance < (radius - 40);
                if ((isWithInSmallCircle && _hour == 0) || (isWithInSmallCircle is false && _hour > 0))
                {
                    _hour += 12;
                }
            }
        }
        else
        {
            _minute = (int)section + 1;

            if (_minute.Value == 60)
            {
                _minute = 0;
            }
        }
        await UpdateCurrentValue();

        StateHasChanged();
    }

    private string? GetHourButtonStyle()
    {
        var style = $"{Styles?.HourButton?.Trim(';')};{(_showHourView ? Styles?.SelectedButtons : null)}".Trim(';');
        return style.HasValue() ? style : null;
    }

    private string? GetMinuteButtonStyle()
    {
        var style = $"{Styles?.MinuteButton?.Trim(';')};{(_showHourView ? null : Styles?.SelectedButtons)}".Trim(';');
        return style.HasValue() ? style : null;
    }

    private string? GetAmButtonStyle(bool isAm)
    {
        var style = $"{Styles?.AmButton?.Trim(';')};{(isAm ? Styles?.SelectedButtons : null)}".Trim(';');
        return style.HasValue() ? style : null;
    }

    private string? GetPmButtonStyle(bool isAm)
    {
        var style = $"{Styles?.PmButton?.Trim(';')};{(isAm ? null : Styles?.SelectedButtons)}".Trim(';');
        return style.HasValue() ? style : null;
    }

    private string? GetClockPointerThumbStyle(bool isMinute)
    {
        var style = $"{Styles?.ClockPointerThumb?.Trim(';')};{(isMinute ? null : Styles?.ClockPointerThumbMinute)}".Trim(';');
        return style.HasValue() ? style : null;
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

        if (DateTime.TryParseExact(value, GetValueFormat() ?? Culture.DateTimeFormat.ShortTimePattern, Culture, DateTimeStyles.None, out DateTime parsedValue))
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
        return time.ToString(GetValueFormat() ?? Culture.DateTimeFormat.ShortTimePattern, Culture);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            OnValueChanged -= HandleOnValueChanged;

            _ = _js.BitCircularTimePickerAbort(_pointerUpAbortControllerId, true);
            _ = _js.BitCircularTimePickerAbort(_pointerMoveAbortControllerId);
        }

        base.Dispose(disposing);
    }
}
