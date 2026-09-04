using System.Globalization;
using System.Diagnostics.CodeAnalysis;

namespace Bit.BlazorUI;

/// <summary>
/// A BitTimePicker offers a drop-down control that's optimized for picking a single time of day using hour and minute
/// spin buttons, with optional seconds, 12/24-hour formats, culture-aware formatting and parsing, stepping in
/// intervals, min/max bounds and custom allowed-value rules, and an inline (standalone) rendering.
/// </summary>
public partial class BitTimePicker : BitInputBase<TimeSpan?>
{
    private bool _hasFocus;
    private string? _labelId;
    private string? _inputId;
    private string? _abortControllerId;
    private bool _internalIsOpenChange;
    private string _headerId = string.Empty;
    private string _footerId = string.Empty;
    private string _calloutId = string.Empty;
    private string _overlayId = string.Empty;
    private string _hourInputId = string.Empty;
    private string _timePickerId = string.Empty;
    private ElementReference _calloutRef = default!;
    private ElementReference _inputHourRef = default!;
    private ElementReference _inputMinuteRef = default!;
    private ElementReference _inputSecondRef = default!;
    private CultureInfo _culture = CultureInfo.CurrentUICulture;
    private CancellationTokenSource _cancellationTokenSource = new();
    private DotNetObjectReference<BitTimePicker> _dotnetObj = default!;


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

            SeedFromStartingValue();

            int candidate;
            int? allowed;

            if (TimeFormat == BitTimeFormat.TwelveHours)
            {
                // A typed value is an hour of the clock face (1-12), so it lands in the half of the day the
                // picker is already on: typing 5 into an afternoon value means 17:00, not a silent flip to
                // the morning. Both 12 and 0 mean the top of the clock, which is hour zero of the half.
                var offset = _hour is >= 12 ? 12 : 0;

                candidate = (((val % 12) + 12) % 12) + offset;
                allowed = SnapHourInHalf(candidate, _hour);
            }
            else
            {
                candidate = Math.Clamp(val, 0, 23);
                allowed = BitTimeSteps.FindAllowedNear(candidate, _hour, 24, IsHourAllowed);
            }

            if (allowed.HasValue is false)
            {
                UndoSeed();
                return;
            }

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

            SeedFromStartingValue();

            var allowed = BitTimeSteps.FindAllowedNear(Math.Clamp(val, 0, 59), _minute, 60, IsMinuteAllowed);
            if (allowed.HasValue is false)
            {
                UndoSeed();
                return;
            }

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

            SeedFromStartingValue();

            var allowed = BitTimeSteps.FindAllowedNear(Math.Clamp(val, 0, 59), _second, 60, IsSecondAllowed);
            if (allowed.HasValue is false)
            {
                UndoSeed();
                return;
            }

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
    /// Custom template to render at the bottom of the TimePicker's callout, below the time inputs and the
    /// action buttons (e.g. preset buttons that set the value from the code).
    /// </summary>
    [Parameter] public RenderFragment? CalloutFooterTemplate { get; set; }

    /// <summary>
    /// Custom template to render at the top of the TimePicker's callout, above the time inputs.
    /// </summary>
    [Parameter] public RenderFragment? CalloutHeaderTemplate { get; set; }

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
    /// The general color of the TimePicker, which applies to the selected AM/PM button, the now and clear
    /// action buttons, and the focus indicator of the input.
    /// </summary>
    [Parameter, ResetClassBuilder]
    public BitColor? Color { get; set; }

    /// <summary>
    /// The delay in milliseconds before the time part starts changing continuously while an
    /// increase/decrease button is held down.
    /// </summary>
    [Parameter] public int ContinuousSpinDelay { get; set; } = 400;

    /// <summary>
    /// The interval in milliseconds between two consecutive changes while an increase/decrease
    /// button is held down.
    /// </summary>
    [Parameter] public int ContinuousSpinInterval { get; set; } = 75;

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
    /// The custom validation error message for a time entered as text that
    /// <see cref="AllowedHours"/>, <see cref="AllowedMinutes"/> or <see cref="AllowedSeconds"/> rejects.
    /// </summary>
    [Parameter] public string? DisallowedTimeErrorMessage { get; set; }

    /// <summary>
    /// Disables every time of day after the current time, exactly as a <see cref="MaxTime"/> of now would.
    /// When both are set, the earlier of the two bounds wins.
    /// </summary>
    /// <remarks>
    /// The current time is taken to the precision the picker shows - to the minute unless
    /// <see cref="ShowSeconds"/> is set - so the minute that is running is still one that can be picked.
    /// </remarks>
    [Parameter] public bool DisableFuture { get; set; }

    /// <summary>
    /// Disables every time of day before the current time, exactly as a <see cref="MinTime"/> of now would.
    /// When both are set, the later of the two bounds wins.
    /// </summary>
    /// <inheritdoc cref="DisableFuture" path="/remarks"/>
    [Parameter] public bool DisablePast { get; set; }

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
    /// The step, in hours, the spin buttons and the keyboard move the hour by.
    /// </summary>
    /// <remarks>
    /// A step greater than 1 lays a grid over the day that every hour the controls produce sits on, so a picker
    /// that only accepts times on a three-hour grid can say so. The grid starts at the hour of <see cref="MinTime"/>,
    /// and at midnight where there is none. A time entered as text is not held to it - a step is the granularity
    /// the controls move in, and a typed time inside the range is a time the person meant. Values below 1 are
    /// treated as 1.
    /// </remarks>
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
    [Parameter, ResetClassBuilder, ResetStyleBuilder, TwoWayBound]
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
    /// The step, in minutes, the spin buttons and the keyboard move the minute by.
    /// </summary>
    /// <remarks>
    /// A step greater than 1 lays a grid over the hour that every minute the controls produce sits on, which is
    /// what turns the picker into a five-minute or quarter-hour one. The grid starts at the minute of
    /// <see cref="MinTime"/>, and at the top of the hour where there is none. A time entered as text is not held
    /// to it. Values below 1 are treated as 1.
    /// </remarks>
    [Parameter] public int MinuteStep { get; set; } = 1;

    /// <summary>
    /// The text of the now button, shown when <see cref="ShowNowButton"/> is set.
    /// </summary>
    [Parameter] public string NowButtonText { get; set; } = "Now";

    /// <summary>
    /// Callback for when the value is cleared using the clear button.
    /// </summary>
    [Parameter] public EventCallback OnClear { get; set; }

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
    /// The custom validation error message for a time entered as text that falls outside of
    /// <see cref="MinTime"/> and <see cref="MaxTime"/>.
    /// </summary>
    [Parameter] public string? OutOfRangeErrorMessage { get; set; }

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
    /// The step, in seconds, the spin buttons and the keyboard move the second by.
    /// </summary>
    /// <remarks>
    /// A step greater than 1 lays a grid over the minute that every second the controls produce sits on. The grid
    /// starts at the second of <see cref="MinTime"/>, and at the top of the minute where there is none. A time
    /// entered as text is not held to it. Values below 1 are treated as 1.
    /// </remarks>
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
    /// The size of the TimePicker, which scales the input, the label, the time inputs and the spin buttons.
    /// </summary>
    [Parameter, ResetClassBuilder]
    public BitSize? Size { get; set; }

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
    /// The time an empty TimePicker starts from, instead of midnight.
    /// </summary>
    /// <remarks>
    /// It is not a value: an untouched picker stays empty and its inputs stay blank. It is only where the
    /// first change lands - stepping the hour of an empty picker whose starting value is 09:30 selects
    /// 10:30 rather than 01:00 - so a picker that mostly gets times around the working day does not have
    /// to be stepped there from midnight every time.
    /// </remarks>
    [Parameter] public TimeSpan? StartingValue { get; set; }

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

        // A culture that writes right to left implies the direction of the picker as well, so a picker
        // that was given no explicit Dir still lays itself out the way its culture reads.
        ClassBuilder.Register(() => BitCssClasses.CultureRtl(Dir, _culture));

        ClassBuilder.Register(GetColorClass);

        ClassBuilder.Register(GetSizeClass);

        ClassBuilder.Register(() => IconLocation is BitIconLocation.Left ? "bit-tpc-lic" : string.Empty);

        ClassBuilder.Register(() => Underlined ? "bit-tpc-und" : string.Empty);

        ClassBuilder.Register(() => HasBorder is false ? "bit-tpc-nbd" : string.Empty);

        ClassBuilder.Register(() => Standalone ? "bit-tpc-sta" : string.Empty);

        ClassBuilder.Register(() => HasFocus ? $"bit-tpc-foc {Classes?.Focused}" : string.Empty);

        ClassBuilder.Register(() => IsEnabled && Required ? "bit-tpc-req" : string.Empty);

        // A read-only picker is not a switched off one: the buttons of the callout carry the disabled
        // attribute there only to keep them from being pressed, and this class takes the look of one back
        // off them.
        ClassBuilder.Register(() => IsEnabled && ReadOnly ? "bit-tpc-rdl" : string.Empty);
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
        _headerId = $"{_timePickerId}-header";
        _footerId = $"{_timePickerId}-footer";
        _calloutId = $"{_timePickerId}-callout";
        _overlayId = $"{_timePickerId}-overlay";
        _hourInputId = $"{_timePickerId}-hour-input";

        SetDefaultValue();

        SetTimeParts(CurrentValue);

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
                await _js.BitSwipesSetup(_calloutId, 0.25m, BitPanelPosition.Top, IsRtl(), BitSwipeOrientation.Vertical, _dotnetObj);

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

            // Prevents the default behavior (scrolling the page) of the keys the field and the parts of the
            // callout are worked with, since Blazor cannot conditionally preventDefault per key, and keeps the
            // focus inside the callout while it is the modal dialog it reports itself to be.
            // A standalone picker has no field to pass along: what it carries instead is a hidden input nobody
            // can land on, so there are no field keys to cancel the defaults of.
            _abortControllerId = await _js.BitTimePickerSetup(_calloutRef,
                                                             Standalone ? null : InputElement,
                                                             Standalone is false);

            // The setup is a round trip, so the picker can be gone by the time the controller id comes back -
            // at a point where DisposeAsync had nothing to abort yet. The listeners it registered would
            // outlive the component otherwise, so they are torn down here instead.
            if (IsDisposed)
            {
                await _js.BitTimePickerDispose(_abortControllerId);
                return;
            }

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

        if (DateTime.TryParseExact(value, GetParseFormats(), _culture, DateTimeStyles.AllowWhiteSpaces, out DateTime parsedValue) is false)
        {
            result = default;
            validationErrorMessage = InvalidErrorMessage.HasValue()
                ? InvalidErrorMessage!
                : $"The {DisplayName ?? FieldIdentifier.FieldName} field is not valid.";
            return false;
        }

        var time = parsedValue.TimeOfDay;

        // A time typed by hand is the only way a value the picker itself refuses can reach the component,
        // so it is reported rather than quietly accepted - and rather than snapped, since text that parses
        // to a refused time is a mistake to report, not a nearby intention to guess at.
        if (IsWithinBounds(time) is false)
        {
            result = default;
            validationErrorMessage = OutOfRangeErrorMessage.HasValue()
                ? OutOfRangeErrorMessage!
                : $"The {DisplayName ?? FieldIdentifier.FieldName} field is out of the allowed range.";
            return false;
        }

        if (IsTimeAllowed(time) is false)
        {
            result = default;
            validationErrorMessage = DisallowedTimeErrorMessage.HasValue()
                ? DisallowedTimeErrorMessage!
                : $"The {DisplayName ?? FieldIdentifier.FieldName} field is not an allowed time.";
            return false;
        }

        result = time;
        SetTimeParts(time);
        validationErrorMessage = null;
        return true;
    }

    protected override string? FormatValueAsString(TimeSpan? value)
    {
        if (value.HasValue is false) return null;

        DateTime time = DateTime.Today.Add(NormalizeToDay(value.Value));
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
            // The same direction the callout renders in (the bit-tpc-rtl class covers the culture-implied
            // RTL as well), so the positioning matches the layout.
            isRtl: IsRtl(),
            scrollContainerId: string.Empty,
            scrollOffset: 0,
            headerId: CalloutHeaderTemplate is not null ? _headerId : string.Empty,
            footerId: CalloutFooterTemplate is not null ? _footerId : string.Empty,
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

    // The keys the spin button pattern asks for on top of what a number input already does itself (the arrow
    // keys, which move it by one): PageUp and PageDown move it by the step of the picker - the same move the
    // spin buttons make, wrapping around and skipping what is not allowed - and Home and End jump to the ends
    // of what can be selected. Enter takes the time that is on the inputs and puts the focus back on the
    // field, the way a dialog is accepted rather than dismissed.
    private async Task HandleOnTimeInputKeyDown(KeyboardEventArgs e, TimeUnit unit)
    {
        if (IsEnabled is false) return;

        // Closing is not a change, so it stays open to a picker that only shows its value.
        if (e.Key == "Enter")
        {
            await CloseCallout();
            return;
        }

        if (IsInteractive is false) return;

        switch (e.Key)
        {
            case "PageUp":
                await ChangeTime(isNext: true, unit);
                break;

            case "PageDown":
                await ChangeTime(isNext: false, unit);
                break;

            case "Home":
                await MoveToEdge(unit, first: true);
                break;

            case "End":
                await MoveToEdge(unit, first: false);
                break;
        }
    }

    // The first or the last value the unit can be set to, which is the first one its predicate accepts from
    // either end of the clock face; the bounds are applied by the update that follows.
    private async Task MoveToEdge(TimeUnit unit, bool first)
    {
        SeedFromStartingValue();

        switch (unit)
        {
            case TimeUnit.Hour:
                // In the 12-hour format the ends of the hour input are the ends of the half of the day it
                // is in, so Home and End do not carry a morning time into the afternoon.
                if (TimeFormat == BitTimeFormat.TwelveHours)
                {
                    var start = _hour is >= 12 ? 12 : 0;
                    var inHalf = BitTimeSteps.FindAllowedFrom(first ? 0 : 11, first, 12, h => IsHourAllowed(start + h));
                    if (inHalf.HasValue is false)
                    {
                        UndoSeed();
                        return;
                    }
                    _hour = start + inHalf.Value;
                }
                else
                {
                    var hour = BitTimeSteps.FindAllowedFrom(first ? 0 : 23, first, 24, IsHourAllowed);
                    if (hour.HasValue is false)
                    {
                        UndoSeed();
                        return;
                    }
                    _hour = hour;
                }
                break;

            case TimeUnit.Minute:
                var minute = BitTimeSteps.FindAllowedFrom(first ? 0 : 59, first, 60, IsMinuteAllowed);
                if (minute.HasValue is false)
                {
                    UndoSeed();
                    return;
                }
                _minute = minute;
                break;

            case TimeUnit.Second:
                var second = BitTimeSteps.FindAllowedFrom(first ? 0 : 59, first, 60, IsSecondAllowed);
                if (second.HasValue is false)
                {
                    UndoSeed();
                    return;
                }
                _second = second;
                break;
        }

        await UpdateCurrentValue();
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

            // The callout holds the tab order while it is open, so an open pushed in from the outside has to
            // move the focus into it exactly as a click on the field does - otherwise the focus is left on the
            // page behind an overlay that it can no longer reach. FocusHourInput leaves an editable field
            // alone, where the text the person came to type has to keep the focus.
            if (isOpen)
            {
                await FocusHourInput();
            }

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
            NormalizeParts();

            CurrentValue = new TimeSpan(_hour!.Value, _minute!.Value, _second!.Value);
        }

        await OnSelectTime.InvokeAsync(CurrentValue);
    }

    private Task HandleOnAmClick() => MoveToHalf(pm: false);

    private Task HandleOnPmClick() => MoveToHalf(pm: true);

    // "12:-- am" is "00:--" and "12:-- pm" is "12:--" in 24h, so the meridiem is the twelve-hour offset the
    // hour of the day is carried across - and an hour already in the target half stays exactly where it is,
    // noon included. An empty picker has no hour to carry, so the buttons leave it empty - unless a starting
    // value says where an empty picker begins, which is the hour they then carry across.
    private async Task MoveToHalf(bool pm)
    {
        if (IsInteractive is false) return;

        SeedFromStartingValue();

        if (_hour.HasValue is false) return;

        var target = (_hour.Value % 12) + (pm ? 12 : 0);

        var allowed = SnapHourInHalf(target, current: null);
        if (allowed.HasValue is false)
        {
            UndoSeed();
            return;
        }

        _hour = allowed;

        await UpdateCurrentValue();
    }

    private async Task HandleOnNowClick()
    {
        if (IsInteractive is false) return;

        var now = DateTime.Now.TimeOfDay;

        // The button picks the time the picker itself could have been moved to: the current time snapped to
        // the nearest the constraints accept. Each part is judged against the parts already picked rather than
        // against what the inputs still hold, since a bound that bites inside an hour makes the minutes it
        // leaves depend on the hour being landed on.
        var hour = BitTimeSteps.FindNearestAllowed(now.Hours, 24, IsHourAllowed);
        if (hour.HasValue is false) return;

        var minute = BitTimeSteps.FindNearestAllowed(now.Minutes, 60, m => IsMinuteAllowed(m, hour));
        if (minute.HasValue is false) return;

        // A picker that does not show the seconds should not hold a hidden value the input cannot display.
        var second = ShowSeconds
            ? BitTimeSteps.FindNearestAllowed(now.Seconds, 60, s => IsSecondAllowed(s, hour, minute))
            : 0;
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

        await OnClear.InvokeAsync();
    }

    private void HandleOnValueChanged(object? sender, EventArgs args)
    {
        SetTimeParts(CurrentValue);
    }

    // Where an empty picker begins: the parts of the starting value, so the first change made to it lands
    // around there instead of around midnight. Without a starting value an empty picker stays empty, and a
    // picker that already holds a time is left alone.
    private void SeedFromStartingValue()
    {
        if (StartingValue.HasValue is false) return;

        if (_hour.HasValue || _minute.HasValue || _second.HasValue) return;

        SetTimeParts(StartingValue);
    }

    // Seeding leaves the parts holding a time the value itself has not been given: every flow that seeds only
    // reaches the value through UpdateCurrentValue, so parts without a value are a seed nothing committed yet.
    private bool HasUncommittedSeed => CurrentValue.HasValue is false
                                    && (_hour.HasValue || _minute.HasValue || _second.HasValue);

    // A change that aborts hands the seed it took back. The starting value is where an empty picker begins,
    // not a time it was given, so a change the constraints refused must leave the inputs as empty as it found
    // them rather than showing a time the value does not have.
    private void UndoSeed()
    {
        if (HasUncommittedSeed is false) return;

        SetTimeParts(null);
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
        SeedFromStartingValue();

        switch (unit)
        {
            case TimeUnit.Hour:
                _hour = BitTimeSteps.StepToAllowed(_hour, isNext, 24, IsHourAllowed) ?? _hour;
                break;

            case TimeUnit.Minute:
                _minute = BitTimeSteps.StepToAllowed(_minute, isNext, 60, IsMinuteAllowed) ?? _minute;
                break;

            case TimeUnit.Second:
                _second = BitTimeSteps.StepToAllowed(_second, isNext, 60, IsSecondAllowed) ?? _second;
                break;
        }

        await UpdateCurrentValue();
    }

    // The search stays inside the half of the day the hour is in, so snapping never flips a morning time into
    // an afternoon one behind the person's back.
    private int? SnapHourInHalf(int hour, int? current)
    {
        var start = hour >= 12 ? 12 : 0;

        // An hour in the other half is no anchor for the direction of the change, since the change is the
        // jump between the halves itself.
        int? currentInHalf = current.HasValue && (current.Value >= 12) == (start == 12) ? current.Value - start : null;

        var nearest = BitTimeSteps.FindAllowedNear(hour - start, currentInHalf, 12, h => IsHourAllowed(start + h));

        return nearest.HasValue ? start + nearest.Value : null;
    }

    // Where the grids of the steps start: the minimum the application declared, so a picker whose range begins
    // at 09:07 can still be set to 09:07. Not the effective bound, which DisablePast moves along with the clock
    // and which would drag the grid with it, leaving the selectable times a different set every minute.
    private TimeSpan? GridAnchor => BitTimeSteps.ClampToDay(MinTime);

    // What the hour can be set to, which is every constraint of the picker at once: the step lays a grid over
    // the day that the hour sits on, the predicate of the application has its say, and the bounds cut the ends
    // off. Everything that moves the hour - the spin buttons, the keyboard, the now button, a typed hour -
    // asks this one question, so none of them can land somewhere another one would refuse.
    private bool IsHourAllowed(int hour)
    {
        if (hour is < 0 or > 23) return false;

        if (BitTimeSteps.IsOnGrid(hour, HourStep, GridAnchor?.Hours ?? 0, 24) is false) return false;

        if (AllowedHours is not null && AllowedHours(hour) is false) return false;

        var min = MinBound;
        var max = MaxBound;

        if (min.HasValue && hour < min.Value.Hours) return false;

        if (max.HasValue && hour > max.Value.Hours) return false;

        return true;
    }

    /// <inheritdoc cref="IsHourAllowed"/>
    private bool IsMinuteAllowed(int minute) => IsMinuteAllowed(minute, _hour);

    private bool IsMinuteAllowed(int minute, int? hour)
    {
        if (minute is < 0 or > 59) return false;

        if (BitTimeSteps.IsOnGrid(minute, MinuteStep, GridAnchor?.Minutes ?? 0, 60) is false) return false;

        if (AllowedMinutes is not null && AllowedMinutes(minute) is false) return false;

        // The bounds only bite inside their own hour: every minute of an hour that is strictly within the
        // range is selectable, and no minute of an hour outside of it is - which the hour has already refused.
        if (hour.HasValue is false) return true;

        var min = MinBound;
        var max = MaxBound;

        if (min.HasValue && hour == min.Value.Hours && minute < min.Value.Minutes) return false;

        if (max.HasValue && hour == max.Value.Hours && minute > max.Value.Minutes) return false;

        return true;
    }

    /// <inheritdoc cref="IsHourAllowed"/>
    private bool IsSecondAllowed(int second) => IsSecondAllowed(second, _hour, _minute);

    private bool IsSecondAllowed(int second, int? hour, int? minute)
    {
        if (second is < 0 or > 59) return false;

        if (BitTimeSteps.IsOnGrid(second, SecondStep, GridAnchor?.Seconds ?? 0, 60) is false) return false;

        if (AllowedSeconds is not null && AllowedSeconds(second) is false) return false;

        // As with the minutes, the bounds only bite inside the minute they fall in.
        if (hour.HasValue is false || minute.HasValue is false) return true;

        var min = MinBound;
        var max = MaxBound;

        if (min.HasValue && hour == min.Value.Hours && minute == min.Value.Minutes
                         && second < min.Value.Seconds) return false;

        if (max.HasValue && hour == max.Value.Hours && minute == max.Value.Minutes
                         && second > max.Value.Seconds) return false;

        return true;
    }

    // Whether a whole time is one the picker itself could have been moved to. The parts are asked about each
    // other rather than about whatever is on the inputs, since the time being judged is not the one they hold.
    private bool IsTimeAllowed(TimeSpan time)
    {
        return AllowedHours?.Invoke(time.Hours) is not false
            && AllowedMinutes?.Invoke(time.Minutes) is not false
            && AllowedSeconds?.Invoke(time.Seconds) is not false;
    }

    // Every part brought onto a value the picker accepts, from the hour down. A part that was never touched
    // starts at zero rather than holding the whole value at null, and the parts below the one that just moved
    // follow it: a bound that bites inside an hour leaves a minute selectable in one hour and refused in the
    // next, so an hour that moves has to take the minute with it rather than produce a time out of range.
    private void NormalizeParts()
    {
        _hour ??= 0;
        _minute ??= 0;
        _second ??= 0;

        var min = MinBound;
        var max = MaxBound;

        if (IsHourAllowed(_hour.Value) is false)
        {
            _hour = SnapIntoRange(_hour.Value, min?.Hours, max?.Hours, true, true, 24, IsHourAllowed) ?? _hour;
        }

        if (IsMinuteAllowed(_minute.Value) is false)
        {
            _minute = SnapIntoRange(_minute.Value, min?.Minutes, max?.Minutes,
                                    _hour == min?.Hours, _hour == max?.Hours, 60, IsMinuteAllowed) ?? _minute;
        }

        if (IsSecondAllowed(_second.Value) is false)
        {
            _second = SnapIntoRange(_second.Value, min?.Seconds, max?.Seconds,
                                    _hour == min?.Hours && _minute == min?.Minutes,
                                    _hour == max?.Hours && _minute == max?.Minutes, 60, IsSecondAllowed) ?? _second;
        }
    }

    // A part that has fallen out of the range moves to the end of the range it fell out of rather than to
    // whichever accepted value happens to be nearest: dropping below the minute the range starts at is a move
    // to that minute, not a wrap to the far end of the hour. The ends only bite where the parts above the one
    // being moved are the ones the bound falls in, which is what the two flags say.
    private static int? SnapIntoRange(int value, int? min, int? max, bool atMin, bool atMax, int range, Func<int, bool> isAllowed)
    {
        if (atMin && min.HasValue && value < min.Value) return BitTimeSteps.FindAllowedFrom(min.Value, true, range, isAllowed);

        if (atMax && max.HasValue && value > max.Value) return BitTimeSteps.FindAllowedFrom(max.Value, false, range, isAllowed);

        return BitTimeSteps.FindNearestAllowed(value, range, isAllowed);
    }

    private async Task HandleOnPointerDown(bool isNext, TimeUnit unit)
    {
        if (IsInteractive is false) return;

        await ChangeTime(isNext, unit);

        if (IsDisposed) return;

        ResetCts();

        // The press-and-hold spin is deliberately not awaited: it lives as long as the button is held, so
        // awaiting it would leave the pointerdown event handler (and the render it drives) pending for the
        // whole duration of the press. Its lifetime is owned by the cancellation token source instead, which
        // HandleOnPointerUpOrOut and DisposeAsync cancel.
        _ = ContinuousChangeTimeAfterDelay(isNext, unit, _cancellationTokenSource);
    }

    /// <summary>
    /// Waits out the <see cref="ContinuousSpinDelay"/> and then starts the continuous spin, unless the
    /// button was released (or the component went away) in the meantime.
    /// </summary>
    private async Task ContinuousChangeTimeAfterDelay(bool isNext, TimeUnit unit, CancellationTokenSource cts)
    {
        try
        {
            await Task.Delay(Math.Max(1, ContinuousSpinDelay), cts.Token);

            await InvokeAsync(() => ContinuousChangeTime(isNext, unit, cts));
        }
        catch (OperationCanceledException) { } // the button was released before the continuous spin started
        catch (ObjectDisposedException) { } // the component was disposed while the delay was pending
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

    // A loop rather than a call that ends in another one of itself: a button held for a few seconds is
    // hundreds of ticks, and every one of them would otherwise leave a frame of its own alive until the whole
    // chain unwinds at the end of the press.
    private async Task ContinuousChangeTime(bool isNext, TimeUnit unit, CancellationTokenSource cts)
    {
        while (cts.IsCancellationRequested is false && IsDisposed is false)
        {
            if (IsInteractive is false) return;

            var partBeforeStep = GetTimePart(unit);

            await ChangeTime(isNext, unit);

            if (cts.IsCancellationRequested || IsDisposed) return;

            // A tick that moved nothing will not move anything on the next one either - the constraints of
            // the picker can leave a part with a single selectable value, or with none - so the held button
            // has run out of room. Without this it would spend the rest of the press re-rendering a value
            // that never changes again.
            if (GetTimePart(unit) == partBeforeStep) return;

            StateHasChanged();

            try
            {
                await Task.Delay(Math.Max(1, ContinuousSpinInterval), cts.Token);
            }
            catch (OperationCanceledException)
            {
                // The button was released while the next tick was pending; ending the loop here stops the
                // spin right away instead of waiting the interval out first.
                return;
            }
        }
    }

    private int? GetTimePart(TimeUnit unit) => unit switch
    {
        TimeUnit.Hour => _hour,
        TimeUnit.Minute => _minute,
        _ => _second
    };

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

    private bool HasActions => ShowNowButton || ShowClearButton;

    // The format the value is written in: the one the application asked for, otherwise the pattern of the
    // culture, rewritten into the clock format of the picker - so a time is written with the separators,
    // the order and the designators of the culture rather than a pattern hardcoded here. The parts are
    // padded, so the field spells the time the same way the two-digit inputs of the callout do.
    private string GetValueFormat()
    {
        if (ValueFormat.HasValue()) return ValueFormat!;

        return BitTimePatterns.GetTimePattern(_culture, TimeFormat, ShowSeconds, padded: true);
    }

    // What a typed time is read with. A format the application set is taken literally - it asked for that
    // one - but the default is only how the picker writes a time, not the only way a person may write it:
    // the other clock format, and either of them with or without the seconds, are the same time spelled
    // differently, so they are accepted as well and rewritten into the canonical format afterwards. The
    // patterns here are the narrow ones, which accept a part written with or without its leading zero -
    // the padded ones the field is written with would accept nothing but a padded value.
    private string[] GetParseFormats()
    {
        if (ValueFormat.HasValue()) return [ValueFormat!];

        List<string> formats = [];

        void Add(string format)
        {
            if (formats.Contains(format)) return;

            formats.Add(format);
        }

        Add(BitTimePatterns.GetTimePattern(_culture, TimeFormat, ShowSeconds, padded: true));
        Add(BitTimePatterns.GetTimePattern(_culture, TimeFormat, ShowSeconds));
        Add(BitTimePatterns.GetTimePattern(_culture, TimeFormat, ShowSeconds is false));
        Add(BitTimePatterns.GetTimePattern(_culture, OtherTimeFormat, ShowSeconds));
        Add(BitTimePatterns.GetTimePattern(_culture, OtherTimeFormat, ShowSeconds is false));

        // The bare spellings on top of the patterns of the culture, so a time typed with a plain colon
        // still lands in a picker whose culture separates the parts with something else.
        Add("H:mm");
        Add("H:mm:ss");
        Add("h:mm tt");
        Add("h:mm:ss tt");

        return [.. formats];
    }

    private BitTimeFormat OtherTimeFormat => TimeFormat == BitTimeFormat.TwelveHours
        ? BitTimeFormat.TwentyFourHours
        : BitTimeFormat.TwelveHours;

    // Every part of the picker works on a time of day, so a value that is not one - a negative span, or one
    // of a day or more - is read as the time of day it lands on rather than rendered as a broken part.
    private static TimeSpan NormalizeToDay(TimeSpan time)
    {
        var ticks = time.Ticks % TimeSpan.TicksPerDay;

        return TimeSpan.FromTicks(ticks < 0 ? ticks + TimeSpan.TicksPerDay : ticks);
    }

    private void SetTimeParts(TimeSpan? value)
    {
        if (value.HasValue is false)
        {
            _hour = null;
            _minute = null;
            _second = null;
            return;
        }

        var time = NormalizeToDay(value.Value);

        _hour = time.Hours;
        _minute = time.Minutes;
        _second = time.Seconds;
    }

    private bool IsRtl() => BitCssClasses.IsRtl(Dir, _culture);

    private string GetColorClass()
    {
        return BitCssClasses.Color(Color, "bit-tpc");
    }

    private string GetSizeClass()
    {
        return BitCssClasses.Size(Size, "bit-tpc");
    }

    // The bounds are times of day, so one that falls outside of a day is pulled back into one before anything
    // is compared against it. Without it the typed input would compare the whole span while the spin buttons
    // work on the parts of it, and the two would disagree about the same value. Disabling the past or the
    // future puts the current time in as a bound of its own, and where the two overlap the narrower one wins.
    private TimeSpan? MinBound
    {
        get
        {
            var min = BitTimeSteps.ClampToDay(MinTime);

            if (DisablePast is false) return min;

            var now = NowBound();

            return (min.HasValue && min.Value > now) ? min : now;
        }
    }

    /// <inheritdoc cref="MinBound"/>
    private TimeSpan? MaxBound
    {
        get
        {
            var max = BitTimeSteps.ClampToDay(MaxTime);

            if (DisableFuture is false) return max;

            var now = NowBound();

            return (max.HasValue && max.Value < now) ? max : now;
        }
    }

    // The current time at the precision the picker works in, so the minute that is running is still one that
    // can be picked: a bound of 10:30:45 would leave 10:30 in the past on a picker that cannot pick seconds.
    private TimeSpan NowBound()
    {
        var now = DateTime.Now.TimeOfDay;

        return ShowSeconds
            ? new TimeSpan(now.Hours, now.Minutes, now.Seconds)
            : new TimeSpan(now.Hours, now.Minutes, 0);
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
        // The callout is moved out to the body while it is open, so the custom properties of the color and
        // the size have to be declared on it as well - nothing of the root cascades down to it there.
        List<string> classes = ["bit-tpc-cal", GetColorClass()];

        var sizeClass = GetSizeClass();
        if (sizeClass.HasValue())
        {
            classes.Add(sizeClass);
        }

        if (Classes?.Callout is not null)
        {
            classes.Add(Classes.Callout);
        }

        // The responsive mode turns the callout into a sheet that slides in from the top of the screen,
        // which a standalone picker - part of the page, with no callout to slide anywhere - would only be
        // hidden by.
        if (Responsive && Standalone is false)
        {
            classes.Add("bit-tpc-res");
        }

        if (IsRtl())
        {
            classes.Add("bit-tpc-rtl");
        }

        // The buttons of a read-only picker all sit in the callout, which is moved out of the root while it
        // is open, so the class that keeps them from looking disabled has to be repeated here.
        if (IsEnabled && ReadOnly)
        {
            classes.Add("bit-tpc-rdl");
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
            await _js.BitTimePickerDispose(_abortControllerId);
            await _js.BitCalloutClearCallout(_calloutId);
            await _js.BitSwipesDispose(_calloutId);
        }
        catch (JSDisconnectedException) { } // we can ignore this exception here
        finally
        {
            // Owned here rather than left to the JS side, which only holds the reference while a callout or a
            // swipe gesture is set up - leaking it in every other case. Disposing after the JS cleanup keeps
            // its callbacks on a live target, and the finally releases the reference even when that cleanup
            // throws something other than JSDisconnectedException.
            _dotnetObj?.Dispose();
        }
    }
}
