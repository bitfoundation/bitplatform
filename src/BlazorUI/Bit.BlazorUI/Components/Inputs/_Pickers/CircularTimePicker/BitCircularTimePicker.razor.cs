using System.Text;
using System.Globalization;
using System.Diagnostics.CodeAnalysis;

namespace Bit.BlazorUI;

/// <summary>
/// A BitCircularTimePicker offers a drop-down control that’s optimized for picking a single time from a clock view where contextual information like the day of the week or fullness of the calendar is important.
/// </summary>
public partial class BitCircularTimePicker : BitInputBase<TimeSpan?>, IAsyncDisposable
{
    private int? _hour;
    private int? _minute;
    private bool _disposed;
    private bool _hasFocus;
    private string? _labelId;
    private string? _inputId;
    private bool _isPointerDown;
    private bool _showHourView = true;
    private ElementReference _clockRef;
    private string _calloutId = string.Empty;
    private string? _pointerUpAbortControllerId;
    private string? _pointerMoveAbortControllerId;
    private string _circularTimePickerId = string.Empty;
    private CultureInfo _culture = CultureInfo.CurrentUICulture;
    private DotNetObjectReference<BitCircularTimePicker> _dotnetObj = default!;



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
    [Parameter, ResetClassBuilder]
    [CallOnSet(nameof(OnSetCulture))]
    public CultureInfo? Culture { get; set; }

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
    [Parameter, ResetClassBuilder]
    public BitIconLocation IconLocation { get; set; } = BitIconLocation.Right;

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
    [Parameter, ResetClassBuilder, TwoWayBound]
    public bool IsOpen { get; set; }

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
    /// Enables the responsive mode in small screens
    /// </summary>
    [Parameter] public bool Responsive { get; set; }

    /// <summary>
    /// Whether the TimePicker's close button should be shown or not.
    /// </summary>
    [Parameter] public bool ShowCloseButton { get; set; }

    /// <summary>
    /// Custom CSS styles for different parts of the TimePicker component.
    /// </summary>
    [Parameter] public BitCircularTimePickerClassStyles? Styles { get; set; }

    /// <summary>
    /// Whether the TimePicker is rendered standalone or with the input component and callout.
    /// </summary>
    [Parameter, ResetClassBuilder]
    public bool Standalone { get; set; }

    /// <summary>
    /// The tabIndex of the TextField.
    /// </summary>
    [Parameter] public int TabIndex { get; set; }

    /// <summary>
    /// The time format of the time-picker, 24H or 12H.
    /// </summary>
    [Parameter] public BitTimeFormat TimeFormat { get; set; }

    /// <summary>
    /// Whether or not the Text field of the TimePicker is underlined.
    /// </summary>
    [Parameter] public bool Underlined { get; set; }

    /// <summary>
    /// The format of the time in the TimePicker
    /// </summary>
    [Parameter] public string? ValueFormat { get; set; }



    public string? InputId => _inputId;



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

    [JSInvokable(nameof(_HandlePointerUp))]
    public async Task _HandlePointerUp(MouseEventArgs e)
    {
        if (IsEnabled is false) return;
        if (_isPointerDown is false) return;

        _isPointerDown = false;

        if (AutoClose && Standalone is false && ((_showHourView is false && _minute.HasValue) || (_showHourView && EditMode == BitCircularTimePickerEditMode.OnlyHours)))
        {
            await CloseCallout();
        }

        if (_showHourView && _hour.HasValue && EditMode == BitCircularTimePickerEditMode.Normal)
        {
            _showHourView = false;
        }

        StateHasChanged();
    }

    [JSInvokable(nameof(_HandlePointerMove))]
    public async Task _HandlePointerMove(MouseEventArgs e)
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

        ClassBuilder.Register(() => Underlined ? "bit-ctp-und" : string.Empty);

        ClassBuilder.Register(() => HasBorder ? string.Empty : "bit-ctp-nbd");

        ClassBuilder.Register(() => Standalone ? "bit-ctp-sta" : string.Empty);

        ClassBuilder.Register(() => _hasFocus ? $"bit-ctp-foc {Classes?.Focused}" : string.Empty);
    }

    protected override void RegisterCssStyles()
    {
        StyleBuilder.Register(() => Styles?.Root);

        StyleBuilder.Register(() => _hasFocus ? Styles?.Focused : string.Empty);
    }

    protected override void OnInitialized()
    {
        _dotnetObj = DotNetObjectReference.Create(this);

        _circularTimePickerId = $"BitCircularTimePicker-{UniqueId}";
        _labelId = $"{_circularTimePickerId}-label";
        _inputId = $"{_circularTimePickerId}-input";
        _calloutId = $"{_circularTimePickerId}-callout";

        _hour = CurrentValue?.Hours;
        _minute = CurrentValue?.Minutes;

        OnValueChanged += HandleOnValueChanged;

        base.OnInitialized();
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        await base.OnAfterRenderAsync(firstRender);

        if (firstRender is false) return;

        await _js.BitSwipesSetup(_calloutId, 0.25m, BitPanelPosition.Top, Dir is BitDir.Rtl, BitSwipeOrientation.Vertical, _dotnetObj);

        _pointerUpAbortControllerId = await _js.BitCircularTimePickerRegisterPointerUp(_dotnetObj, nameof(_HandlePointerUp));
        _pointerMoveAbortControllerId = await _js.BitCircularTimePickerRegisterPointerMove(_dotnetObj, nameof(_HandlePointerMove));
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

    private async Task CloseCallout()
    {
        if (Standalone) return;
        if (IsEnabled is false) return;

        if (await AssignIsOpen(false) is false) return;

        await ToggleCallout();
    }

    private async Task HandleOnChange(ChangeEventArgs e)
    {
        if (IsEnabled is false || InvalidValueBinding()) return;
        if (AllowTextInput is false) return;

        CurrentValueAsString = e.Value?.ToString();
        await OnSelectTime.InvokeAsync(CurrentValue);
    }

    private async Task HandleOnClick()
    {
        if (IsEnabled is false) return;

        if (await AssignIsOpen(true) is false) return;

        _showHourView = true;
        await ToggleCallout();

        await OnClick.InvokeAsync();
    }

    private void OnSetCulture()
    {
        _culture = Culture ?? CultureInfo.CurrentUICulture;
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

    private void HandleOnHourClick()
    {
        if (IsEnabled is false) return;

        _showHourView = true;
    }

    private void HandleOnMinuteClick()
    {
        if (IsEnabled is false) return;

        _showHourView = false;
    }

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
        return ValueFormat.HasValue()
            ? ValueFormat!
            : TimeFormat == BitTimeFormat.TwentyFourHours
                ? "HH:mm"
                : "hh:mm tt";
    }

    private async Task ToggleCallout()
    {
        if (Standalone) return;
        if (IsEnabled is false) return;

        await _js.BitCalloutToggleCallout(_dotnetObj,
                                _circularTimePickerId,
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
                                false);
    }

    private async Task UpdateTime(MouseEventArgs e)
    {
        if (IsEnabled is false || InvalidValueBinding()) return;

        var rect = await _js.BitUtilsGetBoundingClientRect(_clockRef);
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

    private string GetCalloutCssClasses()
    {
        List<string> classes = ["bit-ctp-cal"];

        if (Classes?.Callout is not null)
        {
            classes.Add(Classes.Callout);
        }

        if (Responsive)
        {
            classes.Add("bit-ctp-res");
        }

        return string.Join(' ', classes).Trim();
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

        if (DateTime.TryParseExact(value, GetValueFormat() ?? _culture.DateTimeFormat.ShortTimePattern, _culture, DateTimeStyles.None, out DateTime parsedValue))
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

        return time.ToString(GetValueFormat() ?? _culture.DateTimeFormat.ShortTimePattern, _culture);
    }



    public async ValueTask DisposeAsync()
    {
        await DisposeAsync(true);
        GC.SuppressFinalize(this);
    }

    protected virtual async ValueTask DisposeAsync(bool disposing)
    {
        if (_disposed || disposing is false) return;

        OnValueChanged -= HandleOnValueChanged;

        try
        {
            await _js.BitCalloutClearCallout(_calloutId);
            await _js.BitSwipesDispose(_calloutId);
            await _js.BitCircularTimePickerAbort(_pointerUpAbortControllerId);
            await _js.BitCircularTimePickerAbort(_pointerMoveAbortControllerId);
        }
        catch (JSDisconnectedException) { } // we can ignore this exception here

        _disposed = true;
    }
}
