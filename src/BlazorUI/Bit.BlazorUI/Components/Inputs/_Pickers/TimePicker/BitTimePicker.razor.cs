using System.Globalization;
using System.Diagnostics.CodeAnalysis;

namespace Bit.BlazorUI;

/// <summary>
/// A BitTimePicker offers a drop-down control that’s optimized for picking a single time from a clock view where contextual information like the day of the week or fullness of the calendar is important.
/// </summary>
public partial class BitTimePicker : BitInputBase<TimeSpan?>, IAsyncDisposable
{
    private bool _hasFocus;
    private bool _disposed;
    private string? _labelId;
    private string? _inputId;
    private string _calloutId = string.Empty;
    private string _timePickerId = string.Empty;
    private ElementReference _inputHourRef = default!;
    private ElementReference _inputMinuteRef = default!;
    private CultureInfo _culture = CultureInfo.CurrentUICulture;
    private CancellationTokenSource _cancellationTokenSource = new();
    private DotNetObjectReference<BitTimePicker> _dotnetObj = default!;



    private int? _hour;
    private string? _hourView
    {
        get
        {
            if (TimeFormat == BitTimeFormat.TwelveHours)
            {
                if (_hour > 12)
                {
                    return (_hour - 12)?.ToString("D2");
                }

                if (_hour == 0)
                {
                    return "12";
                }
            }

            return _hour?.ToString("D2");
        }
        set
        {
            if (IsEnabled is false) return;
            if (int.TryParse(value, out int val) is false) return;

            if (val > 23)
            {
                _hour = 23;
            }
            else if (val < 0)
            {
                _hour = 0;
            }
            else
            {
                _hour = val;
            }

            _ = UpdateCurrentValue();
        }
    }

    private int? _minute;
    private string? _minuteView
    {
        get => _minute?.ToString("D2");
        set
        {
            if (IsEnabled is false) return;
            if (int.TryParse(value, out int val) is false) return;

            if (val > 59)
            {
                _minute = 59;
            }
            else if (val < 0)
            {
                _minute = 0;
            }
            else
            {
                _minute = val;
            }

            _ = UpdateCurrentValue();
        }
    }



    [Inject] private IJSRuntime _js { get; set; } = default!;



    /// <summary>
    /// Whether the TimePicker allows input a date string directly or not
    /// </summary>
    [Parameter] public bool AllowTextInput { get; set; }

    /// <summary>
    /// Aria label for time picker popup for screen reader users.
    /// </summary>
    [Parameter] public string CalloutAriaLabel { get; set; } = "Clock";

    /// <summary>
    /// Capture and render additional attributes in addition to the main callout's parameters
    /// </summary>
    [Parameter] public Dictionary<string, object> CalloutHtmlAttributes { get; set; } = [];

    /// <summary>
    /// Custom CSS classes for different parts of the BitTimePicker component.
    /// </summary>
    [Parameter] public BitTimePickerClassStyles? Classes { get; set; }

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
    /// Determines the allowed drop directions of the callout.
    /// </summary>
    [Parameter] public BitDropDirection DropDirection { get; set; } = BitDropDirection.TopAndBottom;

    /// <summary>
    /// Determines if the TimePicker has a border.
    /// </summary>
    [Parameter] public bool HasBorder { get; set; } = true;

    /// <summary>
    /// Determines increment/decrement steps for time-picker's hour.
    /// </summary>
    [Parameter] public int HourStep { get; set; } = 1;

    /// <summary>
    /// Optional TimePicker icon
    /// </summary>
    [Parameter] public string IconName { get; set; } = "Clock";

    /// <summary>
    /// TimePicker icon location
    /// </summary>
    [Parameter, ResetClassBuilder]
    public BitIconLocation IconLocation { get; set; } = BitIconLocation.Right;

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
    /// Determines increment/decrement steps for time-picker's minute.
    /// </summary>
    [Parameter] public int MinuteStep { get; set; } = 1;

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
    /// Whether the BitTimePicker's close button should be shown or not.
    /// </summary>
    [Parameter] public bool ShowCloseButton { get; set; }

    /// <summary>
    /// Custom CSS styles for different parts of the BitTimePicker component.
    /// </summary>
    [Parameter] public BitTimePickerClassStyles? Styles { get; set; }

    /// <summary>
    /// Whether the BitTimePicker is rendered standalone or with the input component and callout.
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
    /// The format of the time in the time-picker
    /// </summary>
    [Parameter] public string? ValueFormat { get; set; }



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



    public Task OpenCallout() => HandleOnClick();



    protected override string RootElementClass => "bit-tpc";

    protected override void RegisterCssClasses()
    {
        ClassBuilder.Register(() => Classes?.Root);

        ClassBuilder.Register(() => IconLocation is BitIconLocation.Left ? "bit-tpc-lic" : string.Empty);

        ClassBuilder.Register(() => Underlined ? "bit-tpc-und" : string.Empty);

        ClassBuilder.Register(() => HasBorder is false ? "bit-tpc-nbd" : string.Empty);

        ClassBuilder.Register(() => Standalone ? "bit-tpc-sta" : string.Empty);

        ClassBuilder.Register(() => _hasFocus ? $"bit-tpc-foc {Classes?.Focused}" : string.Empty);
    }

    protected override void RegisterCssStyles()
    {
        StyleBuilder.Register(() => Styles?.Root);

        StyleBuilder.Register(() => _hasFocus ? Styles?.Focused : string.Empty);
    }

    protected override void OnInitialized()
    {
        _dotnetObj = DotNetObjectReference.Create(this);

        _timePickerId = $"BitTimePicker-{UniqueId}";
        _labelId = $"BitTimePicker-{UniqueId}-label";
        _inputId = $"BitTimePicker-{UniqueId}-input";
        _calloutId = $"BitTimePicker-{UniqueId}-callout";

        _hour = CurrentValue?.Hours;
        _minute = CurrentValue?.Minutes;

        OnValueChanged += HandleOnValueChanged;

        base.OnInitialized();
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        await base.OnAfterRenderAsync(firstRender);

        if (firstRender is false) return;
        if (Responsive is false) return;

        await _js.BitSwipesSetup(_calloutId, 0.25m, BitPanelPosition.Top, Dir is BitDir.Rtl, BitSwipeOrientation.Vertical, _dotnetObj);
    }

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

        if (DateTime.TryParseExact(value, GetValueFormat(), Culture, DateTimeStyles.None, out DateTime parsedValue))
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
        return time.ToString(GetValueFormat(), Culture);
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
        if (await AssignIsOpen(false) is false) return;

        await ToggleCallout();
    }

    private async Task ToggleCallout()
    {
        if (Standalone) return;
        if (IsEnabled is false) return;

        await _js.BitCalloutToggleCallout(_dotnetObj,
                                _timePickerId,
                                null,
                                _calloutId,
                                null,
                                IsOpen,
                                Responsive ? BitResponsiveMode.Top : BitResponsiveMode.None,
                                DropDirection,
                                Dir is BitDir.Rtl,
                                string.Empty,
                                0,
                                string.Empty,
                                string.Empty,
                                true);
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
        if (Standalone) return;
        if (IsEnabled is false) return;

        if (await AssignIsOpen(true) is false) return;

        await ToggleCallout();

        await OnClick.InvokeAsync();
    }

    private void OnSetCulture()
    {
        _culture = Culture ?? CultureInfo.CurrentUICulture;
    }

    private async Task UpdateCurrentValue()
    {
        CurrentValue = (_hour.HasValue is false || _minute.HasValue is false) ? null : new TimeSpan(_hour.Value, _minute.Value, 0);

        await OnSelectTime.InvokeAsync(CurrentValue);
    }

    private async Task HandleOnAmClick()
    {
        if (IsEnabled is false) return;

        _hour %= 12;  // "12:-- am" is "00:--" in 24h
        await UpdateCurrentValue();
    }

    private async Task HandleOnPmClick()
    {
        if (IsEnabled is false) return;

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

    private bool? IsAm()
    {
        if (CurrentValue.HasValue is false || _hour.HasValue is false) return null;

        return _hour.Value >= 0 && _hour < 12; // am is 00:00 to 11:59
    }

    private async Task HandleOnHourFocus()
    {
        if (IsEnabled is false) return;

        await _js.BitUtilsSelectText(_inputHourRef);
    }

    private async Task HandleOnMinuteFocus()
    {
        if (IsEnabled is false) return;

        await _js.BitUtilsSelectText(_inputMinuteRef);
    }

    private async Task ChangeHour(bool isNext)
    {
        if (isNext)
        {
            _hour += HourStep;
        }
        else
        {
            _hour -= HourStep;
        }

        if (_hour.HasValue is false)
        {
            _hour = 0;
        }
        else if (_hour > 23)
        {
            _hour -= 24;
        }
        else if (_hour < 0)
        {
            _hour += 24;
        }

        await UpdateCurrentValue();
    }

    private async Task ChangeMinute(bool isNext)
    {
        if (isNext)
        {
            _minute += MinuteStep;
        }
        else
        {
            _minute -= MinuteStep;
        }

        if (_minute.HasValue is false)
        {
            _minute = 0;
        }
        else if (_minute > 59)
        {
            _minute -= 60;
        }
        else if (_minute < 0)
        {
            _minute += 60;
        }

        await UpdateCurrentValue();
    }

    private async Task HandleOnPointerDown(bool isNext, bool isHour)
    {
        if (IsEnabled is false) return;

        await ChangeTime(isNext, isHour);
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

        await ChangeTime(isNext, isHour);

        StateHasChanged();

        await Task.Delay(75);
        await ContinuousChangeTime(isNext, isHour, cts);
    }

    private async Task ChangeTime(bool isNext, bool isHour)
    {
        if (isHour)
        {
            await ChangeHour(isNext);
        }
        else
        {
            await ChangeMinute(isNext);
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

    private string GetValueFormat()
    {
        return ValueFormat.HasValue()
            ? ValueFormat!
            : TimeFormat == BitTimeFormat.TwentyFourHours
                ? "HH:mm"
                : "hh:mm tt";
    }

    private string GetCalloutCssClasses()
    {
        List<string> classes = ["bit-tpc-cal"];

        if (Classes?.Callout is not null)
        {
            classes.Add(Classes.Callout);
        }

        if (Responsive)
        {
            classes.Add("bit-tpc-res");
        }

        return string.Join(' ', classes).Trim();
    }



    public async ValueTask DisposeAsync()
    {
        await DisposeAsync(true);
        GC.SuppressFinalize(this);
    }

    protected virtual async ValueTask DisposeAsync(bool disposing)
    {
        if (_disposed || disposing is false) return;

        _cancellationTokenSource?.Dispose();
        OnValueChanged -= HandleOnValueChanged;

        try
        {
            await _js.BitCalloutClearCallout(_calloutId);
            await _js.BitSwipesDispose(_calloutId);
        }
        catch (JSDisconnectedException) { } // we can ignore this exception here

        _disposed = true;
    }
}
