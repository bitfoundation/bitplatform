using System.Globalization;
using System.Diagnostics.CodeAnalysis;

namespace Bit.BlazorUI;

/// <summary>
/// A BitTimePicker offers a drop-down control that's optimized for picking a single time value using hour and minute
/// spin buttons, with optional seconds, 12/24-hour formats, and min/max constraints.
/// </summary>
public partial class BitTimePicker : BitInputBase<TimeSpan?>
{
    private bool _hasFocus;
    private string? _labelId;
    private string? _inputId;
    private bool _internalIsOpenChange;
    private string _calloutId = string.Empty;
    private string _overlayId = string.Empty;
    private string _timePickerId = string.Empty;
    private ElementReference _inputHourRef = default!;
    private ElementReference _inputMinuteRef = default!;
    private ElementReference _inputSecondRef = default!;
    private CultureInfo _culture = CultureInfo.CurrentUICulture;
    private CancellationTokenSource _cancellationTokenSource = new();
    private DotNetObjectReference<BitTimePicker> _dotnetObj = default!;

    private static readonly TimeSpan _endOfDay = new(23, 59, 59);

    private enum TimeUnit { Hour, Minute, Second }



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
            if (IsInteractive is false) return;
            if (int.TryParse(value, out int val) is false) return;

            int candidate;
            int? allowed;

            if (TimeFormat == BitTimeFormat.TwelveHours)
            {
                // A typed value is an hour of the clock face (1-12), so it lands in the half of the day the
                // picker is already on: typing 5 into an afternoon value means 17:00, not a silent flip to
                // the morning. Both 12 and 0 mean the top of the clock, which is hour zero of the half.
                var offset = _hour is >= 12 ? 12 : 0;

                candidate = (((val % 12) + 12) % 12) + offset;
                allowed = FindNearestAllowedHourInHalf(candidate);
            }
            else
            {
                candidate = Math.Clamp(val, 0, 23);
                allowed = FindNearestAllowed(candidate, 24, IsHourAllowed);
            }

            if (allowed.HasValue is false) return;

            _hour = allowed;

            _ = UpdateCurrentValue();
        }
    }

    private int? _minute;
    private string? _minuteView
    {
        get => _minute?.ToString("D2");
        set
        {
            if (IsInteractive is false) return;
            if (int.TryParse(value, out int val) is false) return;

            var allowed = FindNearestAllowed(Math.Clamp(val, 0, 59), 60, IsMinuteAllowed);
            if (allowed.HasValue is false) return;

            _minute = allowed;

            _ = UpdateCurrentValue();
        }
    }

    private int? _second;
    private string? _secondView
    {
        get => _second?.ToString("D2");
        set
        {
            if (IsInteractive is false) return;
            if (int.TryParse(value, out int val) is false) return;

            var allowed = FindNearestAllowed(Math.Clamp(val, 0, 59), 60, IsSecondAllowed);
            if (allowed.HasValue is false) return;

            _second = allowed;

            _ = UpdateCurrentValue();
        }
    }



    [Inject] private IJSRuntime _js { get; set; } = default!;



    /// <summary>
    /// Whether the TimePicker allows input a time string directly or not
    /// </summary>
    [Parameter] public bool AllowTextInput { get; set; }

    /// <summary>
    /// The hours that can be selected, on top of what <see cref="MinTime"/> and <see cref="MaxTime"/> already allow.
    /// </summary>
    /// <remarks>
    /// The predicate receives an hour of the day (0-23), whichever <see cref="TimeFormat"/> the picker is in,
    /// and returns whether it can be picked. The spin buttons skip over the hours it rejects, a typed one snaps
    /// to the nearest it accepts, and a time entered as text that lands on one fails validation. The bounds are
    /// applied after it, so where the two disagree the bounds win.
    /// </remarks>
    [Parameter] public Func<int, bool>? AllowedHours { get; set; }

    /// <summary>
    /// The minutes that can be selected, on top of what <see cref="MinTime"/> and <see cref="MaxTime"/> already allow.
    /// </summary>
    /// <remarks>
    /// The predicate receives a minute of the hour (0-59) and returns whether it can be picked. The spin buttons
    /// skip over the minutes it rejects, a typed one snaps to the nearest it accepts, and a time entered as text
    /// that lands on one fails validation. The bounds are applied after it, so where the two disagree the bounds win.
    /// </remarks>
    [Parameter] public Func<int, bool>? AllowedMinutes { get; set; }

    /// <summary>
    /// The seconds that can be selected, on top of what <see cref="MinTime"/> and <see cref="MaxTime"/> already allow.
    /// </summary>
    /// <remarks>
    /// The predicate receives a second of the minute (0-59) and returns whether it can be picked. The spin buttons
    /// skip over the seconds it rejects, a typed one snaps to the nearest it accepts, and a time entered as text
    /// that lands on one fails validation. The bounds are applied after it, so where the two disagree the bounds win.
    /// </remarks>
    [Parameter] public Func<int, bool>? AllowedSeconds { get; set; }

    /// <summary>
    /// Whether the input of the TimePicker gets the focus as soon as it renders for the first time.
    /// </summary>
    [Parameter] public bool AutoFocus { get; set; }

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
    /// The text of the clear button, shown when <see cref="ShowClearButton"/> is set.
    /// </summary>
    [Parameter] public string ClearButtonText { get; set; } = "Clear";

    /// <summary>
    /// Gets or sets the close button icon using custom CSS classes for external icon libraries.
    /// Takes precedence over <see cref="CloseButtonIconName"/> when both are set.
    /// </summary>
    [Parameter] public BitIconInfo? CloseButtonIcon { get; set; }

    /// <summary>
    /// Gets or sets the name of the close button icon from the built-in Fluent UI icons.
    /// </summary>
    [Parameter] public string? CloseButtonIconName { get; set; }

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
    /// Gets or sets the decrease hour button icon using custom CSS classes for external icon libraries.
    /// Takes precedence over <see cref="DecreaseHourIconName"/> when both are set.
    /// </summary>
    [Parameter] public BitIconInfo? DecreaseHourIcon { get; set; }

    /// <summary>
    /// Gets or sets the name of the decrease hour button icon from the built-in Fluent UI icons.
    /// </summary>
    [Parameter] public string? DecreaseHourIconName { get; set; }

    /// <summary>
    /// The title of the decrease hour button (tooltip and aria-label).
    /// </summary>
    [Parameter] public string DecreaseHourTitle { get; set; } = "Decrease hour";

    /// <summary>
    /// Gets or sets the decrease minute button icon using custom CSS classes for external icon libraries.
    /// Takes precedence over <see cref="DecreaseMinuteIconName"/> when both are set.
    /// </summary>
    [Parameter] public BitIconInfo? DecreaseMinuteIcon { get; set; }

    /// <summary>
    /// Gets or sets the name of the decrease minute button icon from the built-in Fluent UI icons.
    /// </summary>
    [Parameter] public string? DecreaseMinuteIconName { get; set; }

    /// <summary>
    /// The title of the decrease minute button (tooltip and aria-label).
    /// </summary>
    [Parameter] public string DecreaseMinuteTitle { get; set; } = "Decrease minute";

    /// <summary>
    /// Gets or sets the decrease second button icon using custom CSS classes for external icon libraries.
    /// Takes precedence over <see cref="DecreaseSecondIconName"/> when both are set.
    /// </summary>
    [Parameter] public BitIconInfo? DecreaseSecondIcon { get; set; }

    /// <summary>
    /// Gets or sets the name of the decrease second button icon from the built-in Fluent UI icons.
    /// </summary>
    [Parameter] public string? DecreaseSecondIconName { get; set; }

    /// <summary>
    /// The title of the decrease second button (tooltip and aria-label).
    /// </summary>
    [Parameter] public string DecreaseSecondTitle { get; set; } = "Decrease second";

    /// <summary>
    /// Determines the allowed drop directions of the callout.
    /// </summary>
    [Parameter] public BitDropDirection DropDirection { get; set; } = BitDropDirection.TopAndBottom;

    /// <summary>
    /// Determines if the TimePicker has a border.
    /// </summary>
    [Parameter, ResetClassBuilder]
    public bool HasBorder { get; set; } = true;

    /// <summary>
    /// The aria-label of the hour input.
    /// </summary>
    [Parameter] public string HourInputAriaLabel { get; set; } = "Hour";

    /// <summary>
    /// Determines increment/decrement steps for time-picker's hour.
    /// </summary>
    [Parameter] public int HourStep { get; set; } = 1;

    /// <summary>
    /// Gets or sets the icon to display using custom CSS classes for external icon libraries.
    /// Takes precedence over <see cref="IconName"/> when both are set.
    /// </summary>
    /// <remarks>
    /// Use this property to render icons from external libraries like FontAwesome, Material Icons, or Bootstrap Icons.
    /// For built-in Fluent UI icons, use <see cref="IconName"/> instead.
    /// </remarks>
    /// <example>
    /// Bootstrap: Icon="BitIconInfo.Bi("gear-fill")"
    /// FontAwesome: Icon="BitIconInfo.Fa("solid house")"
    /// Custom CSS: Icon="BitIconInfo.Css("my-icon-class")"
    /// </example>
    [Parameter] public BitIconInfo? Icon { get; set; }

    /// <summary>
    /// Gets or sets the name of the icon to display from the built-in Fluent UI icons.
    /// </summary>
    /// <remarks>
    /// The icon name should be from the Fluent UI icon set (e.g., <c>BitIconName.Clock</c>).
    /// <br />
    /// Browse available names in <c>BitIconName</c> of the <c>Bit.BlazorUI.Icons</c> nuget package or the gallery:
    /// <see href="https://blazorui.bitplatform.dev/iconography"/>.
    /// <br />
    /// The value is case-sensitive and must match a valid icon identifier.
    /// If not set or set to <c>null</c>, the default Clock icon will be rendered.
    /// <br />
    /// For external icon libraries, use <see cref="Icon"/> instead.
    /// </remarks>
    [Parameter] public string? IconName { get; set; }

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
    /// Gets or sets the increase hour button icon using custom CSS classes for external icon libraries.
    /// Takes precedence over <see cref="IncreaseHourIconName"/> when both are set.
    /// </summary>
    [Parameter] public BitIconInfo? IncreaseHourIcon { get; set; }

    /// <summary>
    /// Gets or sets the name of the increase hour button icon from the built-in Fluent UI icons.
    /// </summary>
    [Parameter] public string? IncreaseHourIconName { get; set; }

    /// <summary>
    /// The title of the increase hour button (tooltip and aria-label).
    /// </summary>
    [Parameter] public string IncreaseHourTitle { get; set; } = "Increase hour";

    /// <summary>
    /// Gets or sets the increase minute button icon using custom CSS classes for external icon libraries.
    /// Takes precedence over <see cref="IncreaseMinuteIconName"/> when both are set.
    /// </summary>
    [Parameter] public BitIconInfo? IncreaseMinuteIcon { get; set; }

    /// <summary>
    /// Gets or sets the name of the increase minute button icon from the built-in Fluent UI icons.
    /// </summary>
    [Parameter] public string? IncreaseMinuteIconName { get; set; }

    /// <summary>
    /// The title of the increase minute button (tooltip and aria-label).
    /// </summary>
    [Parameter] public string IncreaseMinuteTitle { get; set; } = "Increase minute";

    /// <summary>
    /// Gets or sets the increase second button icon using custom CSS classes for external icon libraries.
    /// Takes precedence over <see cref="IncreaseSecondIconName"/> when both are set.
    /// </summary>
    [Parameter] public BitIconInfo? IncreaseSecondIcon { get; set; }

    /// <summary>
    /// Gets or sets the name of the increase second button icon from the built-in Fluent UI icons.
    /// </summary>
    [Parameter] public string? IncreaseSecondIconName { get; set; }

    /// <summary>
    /// The title of the increase second button (tooltip and aria-label).
    /// </summary>
    [Parameter] public string IncreaseSecondTitle { get; set; } = "Increase second";

    /// <summary>
    /// The custom validation error message for the invalid value.
    /// </summary>
    [Parameter] public string? InvalidErrorMessage { get; set; }

    /// <summary>
    /// Whether or not this TimePicker is open
    /// </summary>
    [Parameter, ResetClassBuilder, TwoWayBound]
    [CallOnSet(nameof(OnSetIsOpen))]
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
    /// The maximum time of day that can be selected.
    /// </summary>
    /// <remarks>
    /// A value outside of a day (negative or over 23:59:59) is clamped into one before it is applied.
    /// </remarks>
    [Parameter] public TimeSpan? MaxTime { get; set; }

    /// <summary>
    /// The minimum time of day that can be selected.
    /// </summary>
    /// <inheritdoc cref="MaxTime" path="/remarks"/>
    [Parameter] public TimeSpan? MinTime { get; set; }

    /// <summary>
    /// The aria-label of the minute input.
    /// </summary>
    [Parameter] public string MinuteInputAriaLabel { get; set; } = "Minute";

    /// <summary>
    /// Determines increment/decrement steps for time-picker's minute.
    /// </summary>
    [Parameter] public int MinuteStep { get; set; } = 1;

    /// <summary>
    /// The text of the now button, shown when <see cref="ShowNowButton"/> is set.
    /// </summary>
    [Parameter] public string NowButtonText { get; set; } = "Now";

    /// <summary>
    /// Callback for when clicking on TimePicker input
    /// </summary>
    [Parameter] public EventCallback OnClick { get; set; }

    /// <summary>
    /// Callback for when the callout of the TimePicker is closed.
    /// </summary>
    [Parameter] public EventCallback OnClose { get; set; }

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
    /// Callback for when the callout of the TimePicker is opened.
    /// </summary>
    [Parameter] public EventCallback OnOpen { get; set; }

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
    /// The aria-label of the second input.
    /// </summary>
    [Parameter] public string SecondInputAriaLabel { get; set; } = "Second";

    /// <summary>
    /// Determines increment/decrement steps for time-picker's second.
    /// </summary>
    [Parameter] public int SecondStep { get; set; } = 1;

    /// <summary>
    /// Whether the BitTimePicker's clear button should be shown or not.
    /// </summary>
    [Parameter] public bool ShowClearButton { get; set; }

    /// <summary>
    /// Whether the BitTimePicker's close button should be shown or not.
    /// </summary>
    [Parameter] public bool ShowCloseButton { get; set; }

    /// <summary>
    /// Whether the BitTimePicker's now button should be shown or not.
    /// </summary>
    [Parameter] public bool ShowNowButton { get; set; }

    /// <summary>
    /// Whether the BitTimePicker shows the seconds input or not.
    /// </summary>
    [Parameter] public bool ShowSeconds { get; set; }

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
    /// The time format of the time-picker, 24H or 12H.
    /// </summary>
    [Parameter] public BitTimeFormat TimeFormat { get; set; }

    /// <summary>
    /// Whether or not the Text field of the TimePicker is underlined.
    /// </summary>
    [Parameter, ResetClassBuilder]
    public bool Underlined { get; set; }

    /// <summary>
    /// The format of the time in the time-picker
    /// </summary>
    [Parameter] public string? ValueFormat { get; set; }



    /// <summary>
    /// The id of the input element of the TimePicker.
    /// </summary>
    public string? InputId => _inputId;



    [JSInvokable("CloseCallout")]
    public async Task _CloseCalloutBeforeAnotherCalloutIsOpened()
    {
        if (Standalone) return;
        if (IsEnabled is false) return;
        if (IsOpen is false) return;

        if (await AssignIsOpenInternal(false) is false) return;

        // The focus is on its way to whatever callout is being opened in this one's place, so this is the one
        // close that must not pull it back onto the field.
        await OnClose.InvokeAsync();

        StateHasChanged();
    }

    [JSInvokable("OnStart")]
    public Task _OnStart(decimal startX, decimal startY) => Task.CompletedTask;

    [JSInvokable("OnMove")]
    public Task _OnMove(decimal diffX, decimal diffY) => Task.CompletedTask;

    [JSInvokable("OnEnd")]
    public Task _OnEnd(decimal diffX, decimal diffY) => Task.CompletedTask;

    [JSInvokable("OnClose")]
    public async Task _OnClose()
    {
        await CloseCallout();
        await InvokeAsync(StateHasChanged);
    }



    /// <summary>
    /// Opens the callout of the TimePicker, doing nothing when it is already open or when the picker is
    /// standalone and has no callout to open.
    /// </summary>
    public async Task OpenCallout()
    {
        if (Standalone) return;
        if (IsEnabled is false) return;

        // Assigning the same state over again counts as a change, so an open that is already open would
        // report itself a second time. Every path into here has to be able to fire without checking first:
        // a second ArrowDown on an editable field, a click that lands on the picker again.
        if (IsOpen) return;

        if (await AssignIsOpenInternal(true) is false) return;

        await ToggleCallout();

        await OnOpen.InvokeAsync();

        await FocusHourInput();
    }

    /// <summary>
    /// Closes the callout of the TimePicker, leaving the focus wherever it is.
    /// </summary>
    public Task DismissCallout() => CloseCallout(restoreFocus: false);

    /// <inheritdoc />
    /// <remarks>
    /// A standalone picker carries its value in a hidden input nobody can see or tab to, so the focus goes on
    /// the hour input - the first part of the picker that is actually on the screen.
    /// </remarks>
    public override ValueTask FocusAsync() => Standalone ? _inputHourRef.FocusAsync() : base.FocusAsync();

    /// <inheritdoc />
    /// <inheritdoc cref="FocusAsync()" path="/remarks"/>
    public override ValueTask FocusAsync(bool preventScroll)
    {
        return Standalone ? _inputHourRef.FocusAsync(preventScroll) : base.FocusAsync(preventScroll);
    }



    protected override string RootElementClass => "bit-tpc";

    // The callout takes elements out of the field when it opens, so as long as it is up the field is still the
    // control being operated: an open callout counts as focus of the picker and keeps the focused class and
    // style on it until the callout is closed again.
    private bool HasFocus => _hasFocus || (IsOpen && Standalone is false);

    protected override void RegisterCssClasses()
    {
        ClassBuilder.Register(() => Classes?.Root);

        ClassBuilder.Register(() => IconLocation is BitIconLocation.Left ? "bit-tpc-lic" : string.Empty);

        ClassBuilder.Register(() => Underlined ? "bit-tpc-und" : string.Empty);

        ClassBuilder.Register(() => HasBorder is false ? "bit-tpc-nbd" : string.Empty);

        ClassBuilder.Register(() => Standalone ? "bit-tpc-sta" : string.Empty);

        ClassBuilder.Register(() => HasFocus ? $"bit-tpc-foc {Classes?.Focused}" : string.Empty);

        ClassBuilder.Register(() => IsEnabled && Required ? "bit-tpc-req" : string.Empty);
    }

    protected override void RegisterCssStyles()
    {
        StyleBuilder.Register(() => Styles?.Root);

        StyleBuilder.Register(() => HasFocus ? Styles?.Focused : string.Empty);
    }

    protected override void OnInitialized()
    {
        _timePickerId = $"BitTimePicker-{UniqueId}";
        _labelId = $"{_timePickerId}-label";
        _inputId = $"{_timePickerId}-input";
        _calloutId = $"{_timePickerId}-callout";
        _overlayId = $"{_timePickerId}-overlay";

        SetDefaultValue();

        _hour = CurrentValue?.Hours;
        _minute = CurrentValue?.Minutes;
        _second = CurrentValue?.Seconds;

        OnValueChanged += HandleOnValueChanged;

        base.OnInitialized();
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        await base.OnAfterRenderAsync(firstRender);

        if (firstRender is false) return;

        _dotnetObj = DotNetObjectReference.Create(this);

        try
        {
            // The swipe-to-dismiss gesture belongs to the sheet the responsive mode turns the callout into,
            // so a picker that never becomes a sheet does not get a gesture that would close it out of nowhere.
            if (Responsive && Standalone is false)
            {
                await _js.BitSwipesSetup(_calloutId, 0.25m, BitPanelPosition.Top, Dir is BitDir.Rtl, BitSwipeOrientation.Vertical, _dotnetObj);

                // The setup is a round trip, so the picker can be gone by the time it comes back - at a point
                // where DisposeAsync had nothing to tear down yet. The gesture it registered would outlive the
                // component otherwise, so it is torn down here instead.
                if (IsDisposed)
                {
                    await _js.BitSwipesDispose(_calloutId);
                    return;
                }
            }

            if (IsDisposed) return;

            // An initial IsOpen fired the OnSetIsOpen hook before the first render, when there was no
            // callout element to toggle yet, so the open state is applied here instead.
            if (IsOpen && Standalone is false)
            {
                await ToggleCallout();
            }

            // The autofocus attribute is only honored by the browser for an element that is part of the
            // initial document, which the input of an interactively rendered picker is not. A standalone
            // picker carries the value in a hidden input nobody is meant to land on, so the hour input - the
            // first part that is actually on the screen - takes the focus in its place.
            if (AutoFocus && IsEnabled)
            {
                await (Standalone ? _inputHourRef.FocusAsync() : InputElement.FocusAsync());
            }
        }
        catch (JSDisconnectedException) { } // we can ignore this exception here
    }

    protected override bool TryParseValueFromString(string? value, [MaybeNullWhen(false)] out TimeSpan? result, [NotNullWhen(false)] out string? validationErrorMessage)
    {
        if (value.HasNoValue())
        {
            _hour = null;
            _minute = null;
            _second = null;
            result = null;
            validationErrorMessage = null;
            return true;
        }

        // Only refuses what the predicates refuse, without snapping: text that parses to a refused time is a
        // mistake to report, not a nearby intention to guess at.
        if (DateTime.TryParseExact(value, GetValueFormat(), _culture, DateTimeStyles.None, out DateTime parsedValue) &&
            IsWithinBounds(parsedValue.TimeOfDay) &&
            IsTimeAllowed(parsedValue.TimeOfDay))
        {
            result = parsedValue.TimeOfDay;
            _hour = result.Value.Hours;
            _minute = result.Value.Minutes;
            _second = result.Value.Seconds;
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
        return time.ToString(GetValueFormat(), _culture);
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

    // The callout takes the focus when it opens, so closing it has to hand the focus back rather than let it
    // fall onto the document. Only the closes the person themselves asked for restore it; a close driven from
    // code leaves the focus wherever that code put it.
    private Task CloseCallout() => CloseCallout(restoreFocus: true);

    private async Task CloseCallout(bool restoreFocus)
    {
        if (Standalone) return;
        if (IsEnabled is false) return;

        // A close that has nothing to close must stay silent, since the keys that dismiss the picker -
        // Escape, Tab - reach here whether or not it was open at the time.
        if (IsOpen is false) return;

        if (await AssignIsOpenInternal(false) is false) return;

        await ToggleCallout();

        await OnClose.InvokeAsync();

        if (restoreFocus)
        {
            await FocusInput();
        }
    }

    private async Task ToggleCallout()
    {
        if (Standalone) return;
        if (IsEnabled is false || IsDisposed) return;

        await _js.BitCalloutToggleCallout(
            dotnetObj: _dotnetObj,
            componentId: _timePickerId,
            component: null,
            calloutId: _calloutId,
            callout: null,
            overlayId: _overlayId,
            isCalloutOpen: IsOpen,
            responsiveMode: Responsive ? BitResponsiveMode.Top : BitResponsiveMode.None,
            dropDirection: DropDirection,
            isRtl: Dir is BitDir.Rtl,
            scrollContainerId: string.Empty,
            scrollOffset: 0,
            headerId: string.Empty,
            footerId: string.Empty,
            setCalloutWidth: true,
            fixedCalloutWidth: false,
            maxWindowWidth: 0);
    }

    private async Task HandleOnChange(ChangeEventArgs e)
    {
        if (IsEnabled is false || ReadOnly || InvalidValueBinding()) return;
        if (AllowTextInput is false) return;

        CurrentValueAsString = e.Value?.ToString();
        await OnSelectTime.InvokeAsync(CurrentValue);
    }

    // The handler of the field itself, which is the only thing OnClick reports: opening the callout from code
    // through OpenCallout is not a click on it, so it does not raise one.
    private async Task HandleOnClick()
    {
        if (IsEnabled is false) return;

        await OpenCallout();

        await OnClick.InvokeAsync();
    }

    private async Task HandleOnInputKeyDown(KeyboardEventArgs e)
    {
        if (IsEnabled is false) return;
        if (Standalone) return;

        switch (e.Key)
        {
            case "Escape":
                await CloseCallout();
                break;

            case "ArrowDown":
            case "ArrowUp":
                // Alt+Down to open and Alt+Up to close is the convention every major picker follows; a plain
                // arrow also opens, so the callout is never more than one unmodified keystroke away.
                if (e.AltKey && e.Key == "ArrowUp")
                {
                    await CloseCallout();
                }
                else
                {
                    await OpenCallout();
                }
                break;

            case "Tab":
                // An open callout is relocated to the end of the document, so it is not what the tab order
                // runs into from the field: tabbing on would leave the popup open behind an overlay that
                // swallows every click that could dismiss it. The focus is on its way to the next control,
                // so it is left there rather than pulled back onto the field.
                await CloseCallout(restoreFocus: false);
                break;

            case "Enter":
            case " ":
                // The editable input owns those two keys - Enter submits the form it sits in and the space bar
                // types a space - so only a read-only input opens the callout with them.
                if (AllowTextInput is false)
                {
                    await OpenCallout();
                }
                break;
        }
    }

    // The dialog convention: Escape dismisses the popup from anywhere inside of it and hands the focus back
    // to the field. The keydown bubbles up here from whichever part of the callout holds it.
    private async Task HandleOnCalloutKeyDown(KeyboardEventArgs e)
    {
        if (IsEnabled is false) return;

        if (e.Key == "Escape")
        {
            await CloseCallout();
        }
    }

    private void OnSetCulture()
    {
        _culture = Culture ?? CultureInfo.CurrentUICulture;
    }

    private void OnSetIsOpen()
    {
        // Captured now: the lambda below runs later, so a rapid second change to IsOpen before it has
        // run must not make both invocations act on the same (latest) state.
        var isOpen = IsOpen;

        // The internal open/close flows toggle the callout themselves right after assigning IsOpen, so they
        // can await the toggle and order their focus work after it. The hook only toggles for a change pushed
        // from the outside through the IsOpen parameter, which otherwise has no path to the JS side that
        // actually shows and hides the callout. Before the first render there is no element to toggle (and
        // during prerendering not even a JS runtime to call); an initial IsOpen is applied by OnAfterRenderAsync.
        if (_internalIsOpenChange || IsRendered is false || Standalone) return;

        _ = InvokeAsync(async () =>
        {
            await ToggleCallout();

            await (isOpen ? OnOpen.InvokeAsync() : OnClose.InvokeAsync());
        });
    }

    // The flows that follow AssignIsOpen with their own awaited ToggleCallout mark the change as internal,
    // so the OnSetIsOpen hook does not toggle the callout a second time.
    private async Task<bool> AssignIsOpenInternal(bool value)
    {
        _internalIsOpenChange = true;
        try
        {
            return await AssignIsOpen(value);
        }
        finally
        {
            _internalIsOpenChange = false;
        }
    }

    private async Task FocusInput()
    {
        if (Standalone || IsRendered is false || IsDisposed) return;

        try
        {
            await InputElement.FocusAsync();
        }
        catch (JSDisconnectedException) { } // we can ignore this exception here
    }

    // The time inputs are the part of an opened picker the keyboard acts on, so the first of them takes the
    // focus - unless the input is editable, where the text the person came to type has to keep it.
    private async Task FocusHourInput()
    {
        if (AllowTextInput || IsRendered is false || IsDisposed) return;

        try
        {
            await _inputHourRef.FocusAsync();
        }
        catch (JSDisconnectedException) { } // we can ignore this exception here
    }

    private async Task UpdateCurrentValue()
    {
        // A change of any part always produces a time: the parts that have not been touched yet fall back to
        // zero rather than holding the value at null until every one of them has been set by hand. The seconds
        // of a bound value are carried through even on a picker that does not show them, so stepping an hour
        // does not quietly throw away a part of the value it was given.
        if (_hour.HasValue is false && _minute.HasValue is false && _second.HasValue is false)
        {
            CurrentValue = null;
        }
        else
        {
            var time = ClampToBounds(new TimeSpan(_hour.GetValueOrDefault(), _minute.GetValueOrDefault(), _second.GetValueOrDefault()));

            _hour = time.Hours;
            _minute = time.Minutes;
            _second = time.Seconds;

            CurrentValue = time;
        }

        await OnSelectTime.InvokeAsync(CurrentValue);
    }

    private Task HandleOnAmClick() => MoveToHalf(pm: false);

    private Task HandleOnPmClick() => MoveToHalf(pm: true);

    // "12:-- am" is "00:--" and "12:-- pm" is "12:--" in 24h, so the meridiem is the twelve-hour offset the
    // hour of the day is carried across - and an hour already in the target half stays exactly where it is,
    // noon included. An empty picker has no hour to carry, so the buttons leave it empty.
    private async Task MoveToHalf(bool pm)
    {
        if (IsInteractive is false) return;
        if (_hour.HasValue is false) return;

        var target = (_hour.Value % 12) + (pm ? 12 : 0);

        var allowed = FindNearestAllowedHourInHalf(target);
        if (allowed.HasValue is false) return;

        _hour = allowed;

        await UpdateCurrentValue();
    }

    private async Task HandleOnNowClick()
    {
        if (IsInteractive is false) return;

        var now = DateTime.Now.TimeOfDay;

        // The button picks the time the picker itself could have stepped to: the current time snapped to the
        // steps and to the allowed values, with the bounds applied by the update that follows.
        var hour = FindNearestAllowed(RoundToStep(now.Hours, SafeHourStep, 24), 24, IsHourAllowed);
        if (hour.HasValue is false) return;

        var minute = FindNearestAllowed(RoundToStep(now.Minutes, SafeMinuteStep, 60), 60, IsMinuteAllowed);
        if (minute.HasValue is false) return;

        // A picker that does not show the seconds should not hold a hidden value the input cannot display.
        var second = ShowSeconds ? FindNearestAllowed(RoundToStep(now.Seconds, SafeSecondStep, 60), 60, IsSecondAllowed) : 0;
        if (second.HasValue is false) return;

        _hour = hour;
        _minute = minute;
        _second = second;

        await UpdateCurrentValue();
    }

    private async Task HandleOnClearClick()
    {
        if (IsInteractive is false) return;

        _hour = null;
        _minute = null;
        _second = null;

        await UpdateCurrentValue();
    }

    private void HandleOnValueChanged(object? sender, EventArgs args)
    {
        _hour = CurrentValue?.Hours;
        _minute = CurrentValue?.Minutes;
        _second = CurrentValue?.Seconds;
    }

    private bool? IsAm()
    {
        if (_hour.HasValue is false) return null;

        return _hour.Value < 12; // am is 00:00 to 11:59
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

    private async Task HandleOnSecondFocus()
    {
        if (IsEnabled is false) return;

        await _js.BitUtilsSelectText(_inputSecondRef);
    }

    private async Task ChangeTime(bool isNext, TimeUnit unit)
    {
        switch (unit)
        {
            case TimeUnit.Hour:
                _hour = StepToAllowed(_hour, isNext, SafeHourStep, 24, IsHourAllowed) ?? _hour;
                break;

            case TimeUnit.Minute:
                _minute = StepToAllowed(_minute, isNext, SafeMinuteStep, 60, IsMinuteAllowed) ?? _minute;
                break;

            case TimeUnit.Second:
                _second = StepToAllowed(_second, isNext, SafeSecondStep, 60, IsSecondAllowed) ?? _second;
                break;
        }

        await UpdateCurrentValue();
    }

    private static int Wrap(int value, int range)
    {
        return ((value % range) + range) % range;
    }

    // The step lands where the granularity says, then walks on one by one until the predicate accepts a value -
    // so a picker whose step and allowed set never coincide still moves to the next selectable value instead of
    // refusing to move at all. A predicate that accepts nothing leaves the value where it was.
    private static int? StepToAllowed(int? current, bool isNext, int step, int range, Func<int, bool> isAllowed)
    {
        var direction = isNext ? 1 : -1;
        var candidate = Wrap((current ?? 0) + (direction * step), range);

        for (var offset = 0; offset < range; offset++)
        {
            var value = Wrap(candidate + (direction * offset), range);

            if (isAllowed(value)) return value;
        }

        return null;
    }

    // The nearest is by distance on the wrapping clock face - 23 sits next to 0 - preferring the exact value,
    // then the closer of the two sides, the lower one on a tie.
    private static int? FindNearestAllowed(int value, int range, Func<int, bool> isAllowed)
    {
        for (var offset = 0; offset < range; offset++)
        {
            var lower = Wrap(value - offset, range);
            if (isAllowed(lower)) return lower;

            var upper = Wrap(value + offset, range);
            if (isAllowed(upper)) return upper;
        }

        return null;
    }

    // The search stays inside the half of the day the hour is in, so snapping never flips a morning time into
    // an afternoon one behind the person's back.
    private int? FindNearestAllowedHourInHalf(int hour)
    {
        var start = hour >= 12 ? 12 : 0;

        var nearest = FindNearestAllowed(hour - start, 12, h => IsHourAllowed(start + h));

        return nearest.HasValue ? start + nearest.Value : null;
    }

    private static int RoundToStep(int value, int step, int range)
    {
        if (step <= 1) return value;

        step = Math.Min(step, range);

        var result = (value + (step / 2)) / step * step;

        return result >= range ? 0 : result;
    }

    private bool IsHourAllowed(int hour) => AllowedHours is null || AllowedHours(hour);

    private bool IsMinuteAllowed(int minute) => AllowedMinutes is null || AllowedMinutes(minute);

    private bool IsSecondAllowed(int second) => AllowedSeconds is null || AllowedSeconds(second);

    private bool IsTimeAllowed(TimeSpan time)
    {
        return IsHourAllowed(time.Hours) && IsMinuteAllowed(time.Minutes) && IsSecondAllowed(time.Seconds);
    }

    private async Task HandleOnPointerDown(bool isNext, TimeUnit unit)
    {
        if (IsInteractive is false) return;

        await ChangeTime(isNext, unit);

        if (IsDisposed) return;

        ResetCts();

        var cts = _cancellationTokenSource;
        try
        {
            await Task.Run(async () =>
            {
                await InvokeAsync(async () =>
                {
                    await Task.Delay(400);
                    await ContinuousChangeTime(isNext, unit, cts);
                });
            }, cts.Token);
        }
        catch (OperationCanceledException) { }
    }

    // The keyboard fires no pointer events on a button, only the click - which a pointer press also fires
    // after it has already stepped once and started the press-and-hold. A keyboard "click" is told apart by
    // its detail of zero, so each source steps exactly once.
    private async Task HandleOnSpinClick(MouseEventArgs e, bool isNext, TimeUnit unit)
    {
        if (e.Detail != 0) return;
        if (IsInteractive is false) return;

        await ChangeTime(isNext, unit);
    }

    private async Task ContinuousChangeTime(bool isNext, TimeUnit unit, CancellationTokenSource cts)
    {
        if (cts.IsCancellationRequested || IsDisposed) return;
        if (IsInteractive is false) return;

        await ChangeTime(isNext, unit);

        if (IsDisposed) return;

        StateHasChanged();

        await Task.Delay(75);
        await ContinuousChangeTime(isNext, unit, cts);
    }

    private void HandleOnPointerUpOrOut()
    {
        ResetCts();
    }

    private void ResetCts()
    {
        if (IsDisposed) return;

        _cancellationTokenSource?.Cancel();
        _cancellationTokenSource?.Dispose();
        _cancellationTokenSource = new();
    }

    // Whether the picker currently accepts a change, which is what every pointer, keyboard and button path is
    // gated on so none of them has to repeat the three states that close the picker to the user.
    private bool IsInteractive => IsEnabled && ReadOnly is false && InvalidValueBinding() is false;

    // A step of zero would leave the spin buttons pressing without moving, and a negative one would swap what
    // up and down mean, so anything below one steps by one.
    private int SafeHourStep => HourStep > 1 ? HourStep : 1;

    private int SafeMinuteStep => MinuteStep > 1 ? MinuteStep : 1;

    private int SafeSecondStep => SecondStep > 1 ? SecondStep : 1;

    private bool HasActions => ShowNowButton || ShowClearButton;

    private string GetValueFormat()
    {
        if (ValueFormat.HasValue()) return ValueFormat!;

        return TimeFormat == BitTimeFormat.TwentyFourHours
            ? (ShowSeconds ? "HH:mm:ss" : "HH:mm")
            : (ShowSeconds ? "hh:mm:ss tt" : "hh:mm tt");
    }

    // The bounds are times of day, so one that falls outside of a day is pulled back into one before anything
    // is compared against it. Without it the typed input would compare the whole span while the spin buttons
    // work on the parts of it, and the two would disagree about the same value.
    private TimeSpan? MinBound => ClampToDay(MinTime);

    /// <inheritdoc cref="MinBound"/>
    private TimeSpan? MaxBound => ClampToDay(MaxTime);

    private static TimeSpan? ClampToDay(TimeSpan? time)
    {
        if (time.HasValue is false) return null;

        if (time.Value < TimeSpan.Zero) return TimeSpan.Zero;

        return time.Value > _endOfDay ? _endOfDay : time.Value;
    }

    private TimeSpan ClampToBounds(TimeSpan time)
    {
        var min = MinBound;
        var max = MaxBound;

        if (min.HasValue && time < min.Value) return min.Value;

        if (max.HasValue && time > max.Value) return max.Value;

        return time;
    }

    // Only the bounds are enforced on typed text, not the steps: a step is the granularity the spin buttons
    // move in, and a typed time that is inside the range is a time the person meant.
    private bool IsWithinBounds(TimeSpan time)
    {
        var min = MinBound;
        var max = MaxBound;

        if (min.HasValue && time < min.Value) return false;

        if (max.HasValue && time > max.Value) return false;

        return true;
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

        if (Dir is BitDir.Rtl)
        {
            classes.Add("bit-tpc-rtl");
        }

        return string.Join(' ', classes).Trim();
    }



    protected override async ValueTask DisposeAsync(bool disposing)
    {
        if (IsDisposed || disposing is false) return;

        await base.DisposeAsync(disposing);

        _cancellationTokenSource?.Cancel();
        _cancellationTokenSource?.Dispose();
        OnValueChanged -= HandleOnValueChanged;

        try
        {
            await _js.BitCalloutClearCallout(_calloutId);
            await _js.BitSwipesDispose(_calloutId);
        }
        catch (JSDisconnectedException) { } // we can ignore this exception here
        finally
        {
            // Owned here rather than left to the JS side, which only holds the reference while a callout or a
            // swipe gesture is set up - leaking it in every other case.
            _dotnetObj?.Dispose();
        }
    }
}
