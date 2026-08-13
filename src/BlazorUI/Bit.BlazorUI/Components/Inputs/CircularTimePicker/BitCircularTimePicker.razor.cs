using System.Text;
using System.Globalization;
using System.Diagnostics.CodeAnalysis;

namespace Bit.BlazorUI;

/// <summary>
/// A BitCircularTimePicker picks a single time from an analog clock dial: an hour ring the pointer snaps to,
/// then a minute ring and an optional second ring, with an optional AM/PM pair in 12-hour mode. It supports
/// mouse, touch and keyboard selection, selectable-time constraints, hour, minute and second steps, an
/// editable text input, a standalone (inline) mode and a responsive mode that turns the callout into a sheet
/// on small screens.
/// </summary>
public partial class BitCircularTimePicker : BitInputBase<TimeSpan?>
{

    private int? _hour;
    private int? _minute;
    private int? _second;
    private bool _hasFocus;
    private bool _clockHasFocus;
    private string? _labelId;
    private string? _inputId;
    private string? _clockId;
    private bool _pointerPicked;
    private bool _isPointerDown;
    private ElementReference _clockRef;
    private ElementReference _calloutRef;
    private string? _abortControllerId;
    private bool _internalIsOpenChange;
    private string _headerId = string.Empty;
    private string _footerId = string.Empty;
    private string _calloutId = string.Empty;
    private string _overlayId = string.Empty;
    private string _circularTimePickerId = string.Empty;
    private BitCircularTimePickerView _view = BitCircularTimePickerView.Hour;
    private CultureInfo _culture = CultureInfo.CurrentUICulture;
    private DotNetObjectReference<BitCircularTimePicker> _dotnetObj = default!;



    [Inject] private IJSRuntime _js { get; set; } = default!;



    /// <summary>
    /// Whether the TimePicker allows input a time string directly or not.
    /// </summary>
    /// <remarks>
    /// The text is parsed with the exact <see cref="ValueFormat"/> of the picker, so what is typed has to
    /// match the format the picker itself writes.
    /// </remarks>
    [Parameter] public bool AllowTextInput { get; set; }

    /// <summary>
    /// The minutes that can be selected, on top of what <see cref="MinTime"/>, <see cref="MaxTime"/> and
    /// <see cref="MinuteStep"/> already allow.
    /// </summary>
    /// <remarks>
    /// The predicate receives a minute of the hour (0-59) and returns whether it can be picked. Minutes it
    /// rejects are dimmed on the dial, skipped by the keyboard and refused by the pointer.
    /// </remarks>
    [Parameter] public Func<int, bool>? AllowedMinutes { get; set; }

    /// <summary>
    /// The hours that can be selected, on top of what <see cref="MinTime"/>, <see cref="MaxTime"/> and
    /// <see cref="HourStep"/> already allow.
    /// </summary>
    /// <remarks>
    /// The predicate receives an hour of the day (0-23), whichever <see cref="TimeFormat"/> the clock is in,
    /// and returns whether it can be picked. Hours it rejects are dimmed on the dial, skipped by the keyboard
    /// and refused by the pointer.
    /// </remarks>
    [Parameter] public Func<int, bool>? AllowedHours { get; set; }

    /// <summary>
    /// The seconds that can be selected, on top of what <see cref="MinTime"/>, <see cref="MaxTime"/> and
    /// <see cref="SecondStep"/> already allow.
    /// </summary>
    /// <remarks>
    /// The predicate receives a second of the minute (0-59) and returns whether it can be picked. Seconds it
    /// rejects are dimmed on the dial, skipped by the keyboard and refused by the pointer.
    /// </remarks>
    [Parameter] public Func<int, bool>? AllowedSeconds { get; set; }

    /// <summary>
    /// Renders the AM/PM pair under the clock instead of beside the time in the toolbar.
    /// </summary>
    /// <remarks>
    /// Only the 12-hour format has a meridiem to place. Moving it out of the toolbar is what keeps the read-out
    /// legible when the seconds are shown as well, and gives the pair a touch-sized target of its own.
    /// </remarks>
    [Parameter] public bool AmPmInClock { get; set; }

    /// <summary>
    /// Closes the callout as soon as the selection is complete, without waiting for the close button or a
    /// click outside of it.
    /// </summary>
    /// <remarks>
    /// The selection is complete once the last part the dial offers is picked - the minute, or the second on a
    /// picker that shows the seconds - and as soon as the single editable part is picked in the other edit
    /// modes. The "now" button completes it on its own. A standalone picker has no callout to close, so it
    /// ignores this.
    /// </remarks>
    [Parameter] public bool AutoClose { get; set; }

    /// <summary>
    /// If true, the input of the TimePicker automatically receives focus when the page renders.
    /// </summary>
    [Parameter] public bool AutoFocus { get; set; }

    /// <summary>
    /// Aria label for time picker popup for screen reader users.
    /// </summary>
    [Parameter] public string CalloutAriaLabel { get; set; } = "Clock";

    /// <summary>
    /// Custom template to render at the bottom of the TimePicker's callout, below everything it holds
    /// (e.g. preset buttons that set the value from the code).
    /// </summary>
    [Parameter] public RenderFragment? CalloutFooterTemplate { get; set; }

    /// <summary>
    /// Custom template to render at the top of the TimePicker's callout, above everything it holds.
    /// </summary>
    [Parameter] public RenderFragment? CalloutHeaderTemplate { get; set; }

    /// <summary>
    /// Capture and render additional attributes in addition to the main callout's parameters
    /// </summary>
    [Parameter] public Dictionary<string, object> CalloutHtmlAttributes { get; set; } = [];

    /// <summary>
    /// Custom CSS classes for different parts of the TimePicker component.
    /// </summary>
    [Parameter] public BitCircularTimePickerClassStyles? Classes { get; set; }

    /// <summary>
    /// The text of the button that clears the value of the TimePicker.
    /// </summary>
    [Parameter] public string ClearButtonText { get; set; } = "Clear";

    /// <summary>
    /// The icon to display on the close button using custom CSS classes for external icon libraries.
    /// Takes precedence over <see cref="CloseButtonIconName"/> when both are set.
    /// </summary>
    /// <remarks>
    /// Use this property to render icons from external libraries like FontAwesome, Material Icons, or Bootstrap Icons.
    /// For built-in Fluent UI icons, use <see cref="CloseButtonIconName"/> instead.
    /// </remarks>
    [Parameter] public BitIconInfo? CloseButtonIcon { get; set; }

    /// <summary>
    /// The name of the icon to display on the close button from the built-in Fluent UI icons.
    /// </summary>
    /// <remarks>
    /// For external icon libraries, use <see cref="CloseButtonIcon"/> instead.
    /// </remarks>
    [Parameter] public string? CloseButtonIconName { get; set; }

    /// <summary>
    /// The title of the close button (tooltip).
    /// </summary>
    [Parameter] public string CloseButtonTitle { get; set; } = "Close time picker";

    /// <summary>
    /// The general color of the TimePicker, applied to the toolbar, the dial pointer and the selected numbers.
    /// </summary>
    [Parameter, ResetClassBuilder]
    public BitColor? Color { get; set; }

    /// <summary>
    /// CultureInfo for the TimePicker
    /// </summary>
    [Parameter, ResetClassBuilder]
    [CallOnSet(nameof(OnSetCulture))]
    public CultureInfo? Culture { get; set; }

    /// <summary>
    /// Disables every time of day after the current time, exactly as a <see cref="MaxTime"/> of now would.
    /// When both are set, the earlier of the two bounds wins.
    /// </summary>
    /// <remarks>
    /// The current time is taken to the precision the dial shows - to the minute unless
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
    /// Choose the edition mode. By default, you can edit every part the picker shows.
    /// </summary>
    [Parameter, ResetClassBuilder]
    [CallOnSet(nameof(OnSetEditMode))]
    public BitCircularTimePickerEditMode EditMode { get; set; } = BitCircularTimePickerEditMode.Normal;

    /// <summary>
    /// Determines if the TimePicker has a border.
    /// </summary>
    [Parameter, ResetClassBuilder]
    public bool HasBorder { get; set; } = true;

    /// <summary>
    /// The step, in hours, the dial and the keyboard move the hour by.
    /// </summary>
    /// <remarks>
    /// A step greater than 1 lays a grid over the day that the hour snaps to, dimming the hours in between, so
    /// a picker that only accepts times on a three-hour grid can say so. The grid starts at the hour of
    /// <see cref="MinTime"/>, and at midnight where there is none. Values below 1 are treated as 1.
    /// </remarks>
    [Parameter] public int HourStep { get; set; } = 1;

    /// <summary>
    /// The title (and accessible name) of the button that switches the dial to the hours.
    /// </summary>
    [Parameter] public string HourButtonTitle { get; set; } = "Select hour";

    /// <summary>
    /// TimePicker icon location
    /// </summary>
    [Parameter, ResetClassBuilder]
    public BitIconLocation IconLocation { get; set; } = BitIconLocation.Right;

    /// <summary>
    /// The icon to display using custom CSS classes for external icon libraries.
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
    /// The name of the icon to display from the built-in Fluent UI icons.
    /// </summary>
    /// <remarks>
    /// The icon name should be from the Fluent UI icon set (e.g., <c>BitIconName.Clock</c>).
    /// Browse available names in <c>BitIconName</c> of the <c>Bit.BlazorUI.Icons</c> nuget package or the gallery:
    /// <see href="https://blazorui.bitplatform.dev/iconography"/>.
    /// For external icon libraries, use <see cref="Icon"/> instead.
    /// </remarks>
    [Parameter] public string? IconName { get; set; }

    /// <summary>
    /// Custom TimePicker icon template
    /// </summary>
    [Parameter] public RenderFragment? IconTemplate { get; set; }

    /// <summary>
    /// The custom validation error message for the invalid value.
    /// </summary>
    [Parameter] public string? InvalidErrorMessage { get; set; }

    /// <summary>
    /// Reverses the direction the mouse wheel moves the dial in.
    /// </summary>
    /// <inheritdoc cref="NoMouseWheel" path="/remarks"/>
    [Parameter] public bool InvertMouseWheel { get; set; }

    /// <summary>
    /// Whether or not this TimePicker is open
    /// </summary>
    [Parameter, ResetClassBuilder, ResetStyleBuilder, TwoWayBound]
    [CallOnSet(nameof(OnSetIsOpen))]
    public bool IsOpen { get; set; }

    /// <summary>
    /// Lays the clock out beside its toolbar instead of under it, for a picker that has more width than
    /// height to work with.
    /// </summary>
    /// <remarks>
    /// The toolbar becomes a column down the leading edge - the parts of the time stacked instead of spelled
    /// out on one line - which is what halves the height of the picker. It only takes effect from the small
    /// breakpoint up: a screen narrower than that has no width to give the toolbar, and the responsive mode is
    /// already using all of it.
    /// </remarks>
    [Parameter] public bool Landscape { get; set; }

    /// <summary>
    /// Label for the TimePicker
    /// </summary>
    [Parameter] public string? Label { get; set; }

    /// <summary>
    /// Shows the custom label for text field
    /// </summary>
    [Parameter] public RenderFragment? LabelTemplate { get; set; }

    /// <summary>
    /// The latest time that can be selected.
    /// </summary>
    /// <remarks>
    /// Hours past it, and the minutes past it inside its own hour, are dimmed on the dial and refused by the
    /// pointer and the keyboard. When the text input is allowed, a time typed past it fails validation the
    /// same way an unparsable one does. A value outside of a day is a time of day all the same, so it is
    /// clamped into one before anything is compared against it.
    /// </remarks>
    [Parameter] public TimeSpan? MaxTime { get; set; }

    /// <summary>
    /// The earliest time that can be selected.
    /// </summary>
    /// <inheritdoc cref="MaxTime" path="/remarks"/>
    [Parameter] public TimeSpan? MinTime { get; set; }

    /// <summary>
    /// The title (and accessible name) of the button that switches the dial to the minutes.
    /// </summary>
    [Parameter] public string MinuteButtonTitle { get; set; } = "Select minute";

    /// <summary>
    /// The step, in minutes, the dial and the keyboard move the minute by.
    /// </summary>
    /// <remarks>
    /// A step greater than 1 lays a grid over the hour that the minute snaps to, which is what turns the dial
    /// into a five-minute or quarter-hour picker. The grid starts at the minute of <see cref="MinTime"/>, and
    /// at the top of the hour where there is none. Values below 1 are treated as 1.
    /// </remarks>
    [Parameter] public int MinuteStep { get; set; } = 1;

    /// <summary>
    /// Disables moving the dial with the mouse wheel entirely.
    /// </summary>
    /// <remarks>
    /// By default the wheel moves the dial by one step - the same step the arrow keys move it by - while it is
    /// scrolled over the focused dial with the Shift key held down. Both conditions are what keep an ordinary
    /// scroll of the page from silently changing the time.
    /// </remarks>
    [Parameter] public bool NoMouseWheel { get; set; }

    /// <summary>
    /// The text of the button that sets the TimePicker to the current time.
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
    /// Callback for when the callout of the TimePicker closes.
    /// </summary>
    [Parameter] public EventCallback OnClose { get; set; }

    /// <summary>
    /// Callback for when the input receives focus. Unlike <see cref="OnFocusIn"/> it does not bubble, so it
    /// is the one to use when only the input itself receiving focus is of interest.
    /// </summary>
    [Parameter] public EventCallback OnFocus { get; set; }

    /// <summary>
    /// Callback for when focus moves into the input or any of its descendants, since unlike
    /// <see cref="OnFocus"/> it bubbles.
    /// </summary>
    [Parameter] public EventCallback OnFocusIn { get; set; }

    /// <summary>
    /// Callback for when focus moves out the TimePicker input.
    /// </summary>
    [Parameter] public EventCallback OnFocusOut { get; set; }

    /// <summary>
    /// Callback for when the callout of the TimePicker opens.
    /// </summary>
    [Parameter] public EventCallback OnOpen { get; set; }

    /// <summary>
    /// Callback for when the time changes.
    /// </summary>
    [Parameter] public EventCallback<TimeSpan?> OnSelectTime { get; set; }

    /// <summary>
    /// Callback for when the dial switches between the hours, the minutes and the seconds.
    /// </summary>
    [Parameter] public EventCallback<BitCircularTimePickerView> OnViewChange { get; set; }

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
    /// The title (and accessible name) of the button that switches the dial to the seconds.
    /// </summary>
    [Parameter] public string SecondButtonTitle { get; set; } = "Select second";

    /// <summary>
    /// The step, in seconds, the dial and the keyboard move the second by.
    /// </summary>
    /// <remarks>
    /// A step greater than 1 snaps the second to the nearest multiple of it, exactly as
    /// <see cref="MinuteStep"/> does for the minutes. Values below 1 are treated as 1.
    /// </remarks>
    [Parameter] public int SecondStep { get; set; } = 1;

    /// <summary>
    /// Renders a button that clears the value of the TimePicker under the clock.
    /// </summary>
    [Parameter] public bool ShowClearButton { get; set; }

    /// <summary>
    /// Whether the TimePicker's close button should be shown or not.
    /// </summary>
    [Parameter] public bool ShowCloseButton { get; set; }

    /// <summary>
    /// Renders a button that sets the TimePicker to the current time under the clock.
    /// </summary>
    /// <remarks>
    /// The time it picks is snapped to <see cref="HourStep"/> and <see cref="MinuteStep"/> and clamped into
    /// the selectable range, so the button can never land on a time the dial itself refuses.
    /// </remarks>
    [Parameter] public bool ShowNowButton { get; set; }

    /// <summary>
    /// Adds the seconds to the picker: a third ring the dial moves on to after the minute, a third part in the
    /// toolbar, and the seconds of the value kept instead of zeroed.
    /// </summary>
    /// <remarks>
    /// The default <see cref="ValueFormat"/> grows a seconds part with it. A picker that is left without the
    /// seconds still keeps the ones a bound value came with - it just never offers a way to change them.
    /// </remarks>
    [Parameter]
    [CallOnSet(nameof(OnSetShowSeconds))]
    public bool ShowSeconds { get; set; }

    /// <summary>
    /// The size of the TimePicker.
    /// </summary>
    [Parameter, ResetClassBuilder]
    public BitSize? Size { get; set; }

    /// <summary>
    /// The time an empty TimePicker starts from, instead of midnight.
    /// </summary>
    /// <remarks>
    /// It is not a value: an untouched picker stays empty and the dial shows nothing selected. It is only
    /// where the first change lands - moving the hour of an empty picker whose starting value is 09:30
    /// keeps the 30 minutes rather than resetting them - so a picker that mostly gets times around the
    /// working day does not have to be dragged there from midnight every time.
    /// </remarks>
    [Parameter] public TimeSpan? StartingValue { get; set; }

    /// <summary>
    /// The part of the time the clock starts on when the picker opens.
    /// </summary>
    /// <remarks>
    /// The edit mode wins over it: a picker that only edits one part always starts on that one. So does a
    /// <see cref="BitCircularTimePickerView.Second"/> start view on a picker that does not show the seconds.
    /// </remarks>
    [Parameter] public BitCircularTimePickerView StartView { get; set; } = BitCircularTimePickerView.Hour;

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
    /// The time format of the time-picker, 24H or 12H.
    /// </summary>
    [Parameter] public BitTimeFormat TimeFormat { get; set; }

    /// <summary>
    /// Whether or not the Text field of the TimePicker is underlined.
    /// </summary>
    [Parameter, ResetClassBuilder]
    public bool Underlined { get; set; }

    /// <summary>
    /// The format of the time in the TimePicker
    /// </summary>
    [Parameter] public string? ValueFormat { get; set; }



    /// <summary>
    /// The id of the input element of the TimePicker.
    /// </summary>
    public string? InputId => _inputId;

    /// <summary>
    /// The part of the time the clock is currently editing.
    /// </summary>
    public BitCircularTimePickerView View => _view;



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
    /// Called from JavaScript when the focus leaves the callout for something outside of it.
    /// </summary>
    [JSInvokable(nameof(_HandleCalloutFocusOut))]
    public async Task _HandleCalloutFocusOut()
    {
        // The focus is already on whatever it was moved to, so this is the one close that must not pull it
        // back onto the field: doing so would make it impossible to tab past an open picker at all.
        await CloseCallout(restoreFocus: false);

        await InvokeAsync(StateHasChanged);
    }

    /// <summary>
    /// Called from JavaScript when a pointer goes down on the clock face.
    /// </summary>
    /// <param name="angle">The angle of the pointer on the dial, in degrees clockwise from 12 o'clock.</param>
    /// <param name="distance">The distance of the pointer from the center of the dial, as a fraction of its radius.</param>
    [JSInvokable(nameof(_HandlePointerDown))]
    public async Task _HandlePointerDown(double angle, double distance)
    {
        if (IsInteractive is false) return;

        _isPointerDown = true;

        _pointerPicked = await UpdateTime(angle, distance);

        StateHasChanged();
    }

    /// <summary>
    /// Called from JavaScript while a pointer that went down on the clock face is being dragged.
    /// </summary>
    /// <inheritdoc cref="_HandlePointerDown" path="/param"/>
    [JSInvokable(nameof(_HandlePointerMove))]
    public async Task _HandlePointerMove(double angle, double distance)
    {
        if (_isPointerDown is false) return;

        if (await UpdateTime(angle, distance))
        {
            _pointerPicked = true;
        }

        StateHasChanged();
    }

    /// <summary>
    /// Called from JavaScript when the pointer that was dragging the clock face is released.
    /// </summary>
    [JSInvokable(nameof(_HandlePointerUp))]
    public async Task _HandlePointerUp()
    {
        if (_isPointerDown is false) return;

        _isPointerDown = false;

        // Only a press that landed on a value the picker accepts settles the view. A press on a number the
        // bounds, the steps or the predicates rule out has picked nothing, so moving the dial on to the next
        // part - or closing an AutoClose picker - would be acting on a selection that was never made.
        if (IsInteractive && _pointerPicked) await CommitView();

        _pointerPicked = false;

        StateHasChanged();
    }



    /// <summary>
    /// Opens the callout of the TimePicker, doing nothing when it is already open or when the picker is
    /// standalone and has no callout to open.
    /// </summary>
    public async Task OpenCallout()
    {
        if (IsEnabled is false) return;
        if (Standalone) return;

        // Assigning the same state over again counts as a change, so an open that is already open would
        // report itself a second time - and, worse, throw away the part of the time the dial had been moved
        // on to by starting the picker over. Every path into here has to be able to fire without checking
        // first: a second ArrowDown on an editable field, a click that lands on the picker again.
        if (IsOpen) return;

        if (await AssignIsOpenInternal(true) is false) return;

        _view = GetInitialView();

        await ToggleCallout();

        await OnOpen.InvokeAsync();

        await FocusClock();
    }

    /// <summary>
    /// Closes the callout of the TimePicker.
    /// </summary>
    public Task DismissCallout() => CloseCallout(restoreFocus: false);

    /// <summary>
    /// Switches the dial to the hours, the minutes or the seconds, as far as the <see cref="EditMode"/> and
    /// <see cref="ShowSeconds"/> allow it.
    /// </summary>
    public Task SwitchView(BitCircularTimePickerView view) => ChangeView(view);

    /// <inheritdoc />
    /// <remarks>
    /// A standalone picker carries its value in a hidden input nobody can see or tab to, so the focus goes on
    /// the dial - the part that is actually on the screen and the one every key of the picker acts on.
    /// </remarks>
    public override ValueTask FocusAsync() => Standalone ? _clockRef.FocusAsync() : base.FocusAsync();

    /// <inheritdoc />
    /// <inheritdoc cref="FocusAsync()" path="/remarks"/>
    public override ValueTask FocusAsync(bool preventScroll)
    {
        return Standalone ? _clockRef.FocusAsync(preventScroll) : base.FocusAsync(preventScroll);
    }



    // The callout is a sibling of the root rather than a child of it, so the focus it takes when it opens - the
    // dial, or anything else inside it - leaves the input for good as far as the DOM is concerned and clears
    // _hasFocus. The field is still the control being operated though, so an open callout counts as focus of the
    // picker and keeps the focused class and style on it until the callout is closed again.
    private bool HasFocus => _hasFocus || (IsOpen && Standalone is false);

    protected override string RootElementClass => "bit-ctp";

    protected override void RegisterCssClasses()
    {
        ClassBuilder.Register(() => Classes?.Root);

        ClassBuilder.Register(() => ColorClass);

        ClassBuilder.Register(() => SizeClass);

        ClassBuilder.Register(() => IconLocation is BitIconLocation.Left ? "bit-ctp-lic" : string.Empty);

        ClassBuilder.Register(() => Underlined ? "bit-ctp-und" : string.Empty);

        ClassBuilder.Register(() => HasBorder ? string.Empty : "bit-ctp-nbd");

        ClassBuilder.Register(() => Standalone ? "bit-ctp-sta" : string.Empty);

        ClassBuilder.Register(() => HasFocus ? $"bit-ctp-foc {Classes?.Focused}" : string.Empty);

        ClassBuilder.Register(() => IsEnabled && Required ? "bit-ctp-req" : string.Empty);

        ClassBuilder.Register(() => IsEnabled && ReadOnly ? "bit-ctp-rdl" : string.Empty);
    }

    protected override void RegisterCssStyles()
    {
        StyleBuilder.Register(() => Styles?.Root);

        StyleBuilder.Register(() => HasFocus ? Styles?.Focused : string.Empty);
    }

    protected override void OnInitialized()
    {
        _circularTimePickerId = $"BitCircularTimePicker-{UniqueId}";
        _labelId = $"{_circularTimePickerId}-label";
        _inputId = $"{_circularTimePickerId}-input";
        _clockId = $"{_circularTimePickerId}-clock";
        _headerId = $"{_circularTimePickerId}-header";
        _footerId = $"{_circularTimePickerId}-footer";
        _calloutId = $"{_circularTimePickerId}-callout";
        _overlayId = $"{_circularTimePickerId}-overlay";

        SetDefaultValue();

        _hour = CurrentValue?.Hours;
        _minute = CurrentValue?.Minutes;
        _second = CurrentValue?.Seconds;

        _view = GetInitialView();

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

            _abortControllerId = await _js.BitCircularTimePickerSetup(_dotnetObj, _clockRef, InputElement, _calloutRef,
                                                                     Standalone is false,
                                                                     nameof(_HandlePointerDown),
                                                                     nameof(_HandlePointerMove),
                                                                     nameof(_HandlePointerUp),
                                                                     nameof(_HandleCalloutFocusOut));

            // The setup is a round trip, so the picker can be gone by the time the controller id comes back -
            // at a point where DisposeAsync had nothing to abort yet. The listeners it registered would
            // outlive the component otherwise, so they are torn down here instead.
            if (IsDisposed)
            {
                await _js.BitCircularTimePickerDispose(_abortControllerId);
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
            // picker carries the value in a hidden input nobody is meant to land on, so the dial - the part
            // that is actually on the screen - takes the focus in its place.
            if (AutoFocus && IsEnabled)
            {
                await (Standalone ? _clockRef.FocusAsync() : InputElement.FocusAsync());
            }
        }
        catch (JSDisconnectedException) { } // we can ignore this exception here
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
    // fall onto the document: whatever held it - the dial, the close button, an action - is being hidden.
    // Only the closes the person themselves asked for restore it; a close driven from code leaves the focus
    // wherever that code put it.
    private Task CloseCallout() => CloseCallout(restoreFocus: true);

    private async Task CloseCallout(bool restoreFocus)
    {
        if (Standalone) return;
        if (IsEnabled is false) return;

        // See OpenCallout: a close that has nothing to close must stay silent, since the keys that dismiss the
        // picker - Escape, Tab - reach here whether or not it was open at the time.
        if (IsOpen is false) return;

        if (await AssignIsOpenInternal(false) is false) return;

        await ToggleCallout();

        await OnClose.InvokeAsync();

        if (restoreFocus)
        {
            await FocusInput();
        }
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

    private void OnSetCulture()
    {
        _culture = Culture ?? CultureInfo.CurrentUICulture;
    }

    private void OnSetEditMode()
    {
        // A mode that only edits one part has a single view to be on, so the dial is moved onto it right away
        // instead of waiting for an opening that a standalone picker never has.
        _view = GetInitialView();
    }

    private void OnSetShowSeconds()
    {
        // A dial left on a part the picker has just stopped carrying would draw a ring nothing on it can be
        // picked from, so it falls back to where an opening picker would have started.
        if (IsViewEditable(_view) is false)
        {
            _view = GetInitialView();
        }
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
            if (isOpen)
            {
                _view = GetInitialView();
            }

            await ToggleCallout();

            await (isOpen ? OnOpen.InvokeAsync() : OnClose.InvokeAsync());

            // A picker opened from the outside is operated the same way as one opened by a click, so the
            // keyboard has to land on the same place in both.
            if (isOpen)
            {
                await FocusClock();
            }
        });
    }

    // See OnSetIsOpen: the flows that follow AssignIsOpen with their own awaited ToggleCallout mark the
    // change as internal, so the hook does not toggle the callout a second time.
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

    private string GetNumberClass(int value)
    {
        StringBuilder classes = new();

        if (Classes?.ClockNumber.HasValue() ?? false)
        {
            classes.Append(Classes.ClockNumber);
        }

        if (IsSelectable(value) is false)
        {
            if (classes.Length > 0) classes.Append(' ');

            classes.Append("bit-ctp-dis");

            if (Classes?.ClockDisabledNumber.HasValue() ?? false)
            {
                classes.Append(' ').Append(Classes.ClockDisabledNumber);
            }
        }

        if (IsSelected(value))
        {
            if (classes.Length > 0) classes.Append(' ');

            classes.Append("bit-ctp-sel");

            if (Classes?.ClockSelectedNumber.HasValue() ?? false)
            {
                classes.Append(' ').Append(Classes.ClockSelectedNumber);
            }
        }

        return classes.ToString();
    }

    private string GetNumberStyle(int value, int index)
    {
        StringBuilder styles = new();

        // Placed by rotating the number out to the radius of its ring and rotating the glyph back upright, so
        // the position follows the radius the stylesheet sets for the current size instead of the pixel math a
        // fixed clock diameter used to be hard-coded around.
        var degree = (index * 30) % 360;
        styles.Append(FormattableString.Invariant(
            $"transform: rotate({degree}deg) translateY(calc(var(--bit-ctp-num-r) * -1)) rotate({-degree}deg);"));

        if (Styles?.ClockNumber.HasValue() ?? false)
        {
            styles.Append(' ').Append(Styles.ClockNumber);
        }

        if ((Styles?.ClockDisabledNumber.HasValue() ?? false) && IsSelectable(value) is false)
        {
            styles.Append(' ').Append(Styles.ClockDisabledNumber);
        }

        if ((Styles?.ClockSelectedNumber.HasValue() ?? false) && IsSelected(value))
        {
            styles.Append(' ').Append(Styles.ClockSelectedNumber);
        }

        return styles.ToString();
    }

    // Whether the number is the one the dial currently points at, which is what the selected paint and the
    // aria-activedescendant of the listbox both hang on. Every number is addressed by the value it stands for -
    // an hour of the day even on the 12-hour dial, which shows it as 1-12 - so one comparison covers both formats.
    private bool IsSelected(int value)
    {
        return CurrentPart == value;
    }

    // Whether the number can be picked at all, in the units of the view it belongs to: an hour of the day for
    // the hour ring (whichever format the dial is in), and a minute or a second of the sixty for the other two.
    private bool IsSelectable(int value)
    {
        return _view switch
        {
            BitCircularTimePickerView.Hour => IsHourAllowed(value),
            BitCircularTimePickerView.Minute => IsMinuteAllowed(value),
            _ => IsSecondAllowed(value)
        };
    }

    private string GetNumberId(int value) => $"{_clockId}-{ViewKey}{value}";

    // The option the dial rests on, or nothing when it rests between two of them - a minute or a second with no
    // number of its own has no option for aria-activedescendant to point at.
    private string? _activeDescendantId => CurrentPart switch
    {
        null => null,
        int value when IsHourView is false && value % 5 != 0 => null,
        int value => GetNumberId(value)
    };

    // Whether the hand points at the inner ring, which only the 24-hour dial has: it carries 1-12 there, so a
    // hand resting on one of them is shortened to that radius.
    private bool IsOnInnerRing => IsHourView
                               && TimeFormat == BitTimeFormat.TwentyFourHours
                               && _hour is > 0 and < 13;

    private double GetPointerDegree()
    {
        return IsHourView
            ? (_hour.GetValueOrDefault() * 30 % 360)
            : (CurrentPart.GetValueOrDefault() * 6 % 360);
    }

    // The dial and the keyboard both land here, so the rounding, the constraints and the value update stay in
    // one place whichever of the two moved the hour.
    private async Task SetHour(int hour)
    {
        SeedFromStartingValue();

        hour = ((hour % 24) + 24) % 24;

        if (IsHourAllowed(hour) is false) return;

        if (_hour == hour) return;

        _hour = hour;

        // The hour the minute belongs to has changed, so a minute that the new hour puts outside of the range
        // is pulled back into it instead of quietly holding a value the dial itself refuses - and the same for
        // the second, which the minute it was pulled to may in turn rule out.
        PullMinuteIntoRange();

        PullSecondIntoRange();

        await UpdateCurrentValue();
    }

    private async Task SetMinute(int minute)
    {
        SeedFromStartingValue();

        minute = ((minute % 60) + 60) % 60;

        if (IsMinuteAllowed(minute) is false) return;

        if (_minute == minute) return;

        _minute = minute;

        PullSecondIntoRange();

        await UpdateCurrentValue();
    }

    private async Task SetSecond(int second)
    {
        SeedFromStartingValue();

        second = ((second % 60) + 60) % 60;

        if (IsSecondAllowed(second) is false) return;

        if (_second == second) return;

        _second = second;

        await UpdateCurrentValue();
    }

    // A part of the time that the part above it has just put outside of the range is moved to the closest value
    // that is still inside it, in the direction of the bound it broke - up from below the minimum, down from
    // above the maximum. Searching both ways would wrap the long way round the ring instead: a minute of :00
    // under a minimum of :30 is one step from :59 going backwards, which is the far end of the range rather
    // than the near one.
    // A part that has not been picked yet still reaches the value as a zero (see UpdateCurrentValue), so it is
    // corrected like a picked one rather than left to carry the value outside of the range.
    private void PullMinuteIntoRange()
    {
        var minute = _minute.GetValueOrDefault();

        var min = MinBound;
        var max = MaxBound;

        var isUnderMin = min.HasValue && _hour == min.Value.Hours && minute < min.Value.Minutes;
        var isOverMax = max.HasValue && _hour == max.Value.Hours && minute > max.Value.Minutes;

        if (isUnderMin is false && isOverMax is false && IsMinuteAllowed(minute)) return;

        if (isUnderMin)
        {
            _minute = FindNearestAllowedMinute(min!.Value.Minutes, 1);
        }
        else if (isOverMax)
        {
            _minute = FindNearestAllowedMinute(max!.Value.Minutes, -1);
        }
        else
        {
            _minute = FindNearestAllowedMinute(minute);
        }
    }

    /// <inheritdoc cref="PullMinuteIntoRange"/>
    private void PullSecondIntoRange()
    {
        var hour = _hour.GetValueOrDefault();
        var minute = _minute.GetValueOrDefault();
        var second = _second.GetValueOrDefault();

        var min = MinBound;
        var max = MaxBound;

        var isUnderMin = min.HasValue && hour == min.Value.Hours
                                      && minute == min.Value.Minutes && second < min.Value.Seconds;
        var isOverMax = max.HasValue && hour == max.Value.Hours
                                     && minute == max.Value.Minutes && second > max.Value.Seconds;

        if (isUnderMin is false && isOverMax is false && IsSecondAllowed(second)) return;

        if (isUnderMin)
        {
            _second = FindNearestAllowedSecond(min!.Value.Seconds, 1);
        }
        else if (isOverMax)
        {
            _second = FindNearestAllowedSecond(max!.Value.Seconds, -1);
        }
        else
        {
            _second = FindNearestAllowedSecond(second);
        }
    }

    private async Task UpdateCurrentValue()
    {
        // A pick on the dial always produces a time: the part that has not been picked yet falls back to zero
        // rather than holding the value at null, which is what left the single-part edit modes unable to
        // produce a value at all. The seconds of a bound value are carried through even on a picker that does
        // not show them, so dialling an hour does not quietly throw away a part of the value it was given.
        CurrentValue = (_hour.HasValue || _minute.HasValue || _second.HasValue)
            ? new TimeSpan(_hour.GetValueOrDefault(), _minute.GetValueOrDefault(), _second.GetValueOrDefault())
            : null;

        await OnSelectTime.InvokeAsync(CurrentValue);
    }

    // Padded to two digits like the other parts, so the read-out neither disagrees with the field - whose
    // default format pads as well - nor shifts sideways as the hour crosses ten.
    private string GetHourString()
    {
        if (_hour.HasValue is false) return "--";

        var hours = TimeFormat == BitTimeFormat.TwelveHours ? GetAmPmHours(_hour.Value) : _hour.Value;

        return hours.ToString("D2", CultureInfo.InvariantCulture);
    }

    private string GetMinuteString()
    {
        return _minute.HasValue
            ? _minute.Value.ToString("D2", CultureInfo.InvariantCulture)
            : "--";
    }

    private string GetSecondString()
    {
        return _second.HasValue
            ? _second.Value.ToString("D2", CultureInfo.InvariantCulture)
            : "--";
    }

    // The whole time as the toolbar of a single-part edit mode reads it out, where there are no buttons to
    // split it across.
    private string GetTimeString()
    {
        return HasSeconds
            ? $"{GetHourString()}:{GetMinuteString()}:{GetSecondString()}"
            : $"{GetHourString()}:{GetMinuteString()}";
    }

    private Task HandleOnHourClick() => ChangeView(BitCircularTimePickerView.Hour);

    private Task HandleOnMinuteClick() => ChangeView(BitCircularTimePickerView.Minute);

    private Task HandleOnSecondClick() => ChangeView(BitCircularTimePickerView.Second);

    private async Task ChangeView(BitCircularTimePickerView view)
    {
        if (IsEnabled is false) return;
        if (_view == view) return;
        if (IsViewEditable(view) is false) return;

        _view = view;

        await OnViewChange.InvokeAsync(view);
    }

    // "12:-- am" is "00:--" and "12:-- pm" is "12:--" in 24h, so the meridiem is the twelve-hour offset the
    // hour of the day is carried across.
    private Task HandleOnAmClick() => SetMeridiem(isAm: true);

    private Task HandleOnPmClick() => SetMeridiem(isAm: false);

    private async Task SetMeridiem(bool isAm)
    {
        if (IsInteractive is false) return;

        SeedFromStartingValue();

        var offset = isAm ? 0 : 12;
        var hour = (GetAmPmHours(_hour.GetValueOrDefault()) % 12) + offset;

        // The constraints can rule out the hour the meridiem lands on - a range that starts in the afternoon
        // rules out the whole morning - so the half is entered at its first selectable hour instead of the
        // button becoming a dead end that no click can get past.
        if (IsHourAllowed(hour) is false)
        {
            var fallback = -1;

            for (var offsetInHalf = 0; offsetInHalf < 12; offsetInHalf++)
            {
                if (IsHourAllowed(offsetInHalf + offset) is false) continue;

                fallback = offsetInHalf + offset;
                break;
            }

            if (fallback < 0) return;

            hour = fallback;
        }

        await SetHour(hour);
    }

    private async Task HandleOnNowClick()
    {
        if (IsInteractive is false) return;

        var now = DateTime.Now.TimeOfDay;

        // The step is already part of what the allowed-value checks accept, so the current time only has to be
        // walked to the nearest they do accept - rounding it to the grid first would round it to a grid pinned
        // to the top of the range rather than to the one the picker actually uses.
        var hour = FindNearestAllowedHour(now.Hours);

        if (hour.HasValue is false) return;

        _hour = hour;
        _minute = FindNearestAllowedMinute(now.Minutes);
        _second = HasSeconds ? FindNearestAllowedSecond(now.Seconds) : 0;

        await UpdateCurrentValue();

        // The button sets every part at once, so the selection it makes is already complete: an AutoClose
        // picker is done rather than moved on to a part that has just been filled in anyway.
        if (AutoClose && Standalone is false)
        {
            await CloseCallout();
        }
    }

    private async Task HandleOnClearClick()
    {
        if (IsInteractive is false) return;

        _hour = null;
        _minute = null;
        _second = null;

        await UpdateCurrentValue();

        await ChangeView(GetInitialView());

        await OnClear.InvokeAsync();
    }

    // Where an empty picker begins: the parts of the starting value, so the first change made to it lands
    // around there instead of around midnight. Without a starting value an empty picker stays empty, and a
    // picker that already holds a time is left alone.
    private void SeedFromStartingValue()
    {
        if (StartingValue.HasValue is false) return;

        if (_hour.HasValue || _minute.HasValue || _second.HasValue) return;

        var time = BitTimeSteps.ClampToDay(StartingValue)!.Value;

        _hour = time.Hours;
        _minute = time.Minutes;
        _second = time.Seconds;
    }

    private void HandleOnValueChanged(object? sender, EventArgs args)
    {
        _hour = CurrentValue?.Hours;
        _minute = CurrentValue?.Minutes;
        _second = CurrentValue?.Seconds;
    }

    private bool IsAm()
    {
        if (_hour.HasValue) return _hour.Value < 12; // am is 00:00 to 11:59

        // Without a value there is no meridiem to be in, so the dial starts on the half it can actually be used
        // in: the morning, unless every one of its hours is ruled out - which a range that begins after noon
        // does - where an empty picker would otherwise open onto twelve dimmed numbers.
        return HasAllowedHourInHalf(isAm: true);
    }

    private bool HasAllowedHourInHalf(bool isAm)
    {
        var offset = isAm ? 0 : 12;

        for (var offsetInHalf = 0; offsetInHalf < 12; offsetInHalf++)
        {
            if (IsHourAllowed(offsetInHalf + offset)) return true;
        }

        return false;
    }

    // The format the value is written in: the one the application asked for, otherwise the pattern of the
    // culture, rewritten into the clock format of the picker (see BitTimePatterns) - so a time is written
    // with the separators, the order and the designators of the culture rather than a pattern hardcoded here.
    // The parts are padded, since a clock face reads a time as two digits either side of the separator.
    private string GetValueFormat()
    {
        if (ValueFormat.HasValue()) return ValueFormat!;

        return BitTimePatterns.GetTimePattern(_culture, TimeFormat, HasSeconds, padded: true);
    }

    // What a typed time is read with. A format the application set is taken literally - it asked for that one -
    // but the default is only how the picker writes a time, not the only way a person may write it: the same
    // time with the leading zeros left off is the same time, so it is accepted as well and rewritten into the
    // padded form afterwards. A padded specifier accepts nothing but a padded value, which is why the narrow
    // pattern has to be in the list of its own.
    private string[] GetParseFormats()
    {
        if (ValueFormat.HasValue()) return [ValueFormat!];

        return
        [
            BitTimePatterns.GetTimePattern(_culture, TimeFormat, HasSeconds, padded: true),
            BitTimePatterns.GetTimePattern(_culture, TimeFormat, HasSeconds)
        ];
    }

    private async Task ToggleCallout()
    {
        if (Standalone) return;
        if (IsEnabled is false || IsDisposed) return;

        await _js.BitCalloutToggleCallout(
            dotnetObj: _dotnetObj,
            componentId: _circularTimePickerId,
            component: null,
            calloutId: _calloutId,
            callout: null,
            overlayId: _overlayId,
            isCalloutOpen: IsOpen,
            responsiveMode: Responsive ? BitResponsiveMode.Top : BitResponsiveMode.None,
            dropDirection: DropDirection,
            isRtl: Dir is BitDir.Rtl,
            scrollContainerId: "",
            scrollOffset: 0,
            headerId: "",
            footerId: "",
            setCalloutWidth: false,
            fixedCalloutWidth: false,
            maxWindowWidth: 0);
    }

    // Maps a position on the dial - an angle clockwise from 12 o'clock and a distance as a fraction of the
    // radius - onto the hour, the minute or the second it points at, and reports whether that value is one the
    // picker accepts. The 24-hour dial carries two rings, so the distance is what tells the inner one (1-12)
    // from the outer one (13-00).
    private async Task<bool> UpdateTime(double angle, double distance)
    {
        if (IsInteractive is false) return false;

        if (IsHourView)
        {
            var section = (int)Math.Round(angle / 30) % 12;
            int hour;

            if (TimeFormat == BitTimeFormat.TwelveHours)
            {
                var number = section == 0 ? 12 : section;

                hour = IsAm() ? number % 12 : (number % 12) + 12;
            }
            else
            {
                // The 24-hour dial carries 1-12 on the inner ring and 13-23 with 00 on the outer one. The rings
                // meet halfway between the two radii the numbers sit on, so the pick follows whichever one the
                // pointer is closer to instead of a fixed pixel band that only holds at one clock size.
                var isInnerRing = distance < InnerRingThreshold;

                hour = isInnerRing
                    ? (section == 0 ? 12 : section)
                    : (section == 0 ? 0 : section + 12);
            }

            if (IsHourAllowed(hour) is false) return false;

            await SetHour(hour);
            return true;
        }

        var value = BitTimeSteps.SnapToGrid((int)Math.Round(angle / 6) % 60,
                                            IsMinuteView ? MinuteStep : SecondStep,
                                            (IsMinuteView ? GridAnchor?.Minutes : GridAnchor?.Seconds) ?? 0,
                                            60);

        if (IsSelectable(value) is false) return false;

        await (IsMinuteView ? SetMinute(value) : SetSecond(value));
        return true;
    }

    // What happens once a pick is settled: the normal mode moves the dial on to the part that follows the one
    // just picked, and a complete selection closes the callout when the picker was asked to.
    private async Task CommitView()
    {
        var next = GetNextView();

        if (next.HasValue)
        {
            await ChangeView(next.Value);
            return;
        }

        if (AutoClose && Standalone is false)
        {
            await CloseCallout();
        }
    }

    // The part the dial moves on to once the current one is settled, or nothing when the current one is the
    // last the picker offers - which is every part at once in the single-part edit modes.
    private BitCircularTimePickerView? GetNextView()
    {
        if (EditMode != BitCircularTimePickerEditMode.Normal) return null;

        if (IsHourView) return BitCircularTimePickerView.Minute;

        if (IsMinuteView && HasSeconds) return BitCircularTimePickerView.Second;

        return null;
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
                await OpenCallout();
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
                // The scroll the space bar would also do is cancelled on the JS side, where the key is known;
                // Blazor can only preventDefault the whole handler, which would take Tab away with it.
                if (AllowTextInput is false)
                {
                    await OpenCallout();
                }
                break;
        }
    }

    private async Task HandleOnClockKeyDown(KeyboardEventArgs e)
    {
        if (IsEnabled is false) return;

        switch (e.Key)
        {
            case "ArrowUp":
            case "ArrowRight":
                await MoveByStep(1);
                break;

            case "ArrowDown":
            case "ArrowLeft":
                await MoveByStep(-1);
                break;

            case "PageUp":
                await MoveByStep(5);
                break;

            case "PageDown":
                await MoveByStep(-5);
                break;

            case "Home":
                await MoveToEdge(first: true);
                break;

            case "End":
                await MoveToEdge(first: false);
                break;

            case "Enter":
            case " ":
                // As with a press on the dial, there is nothing to settle until the part the dial is on has
                // actually been given a value.
                if (IsInteractive && CurrentPart.HasValue) await CommitView();
                break;

            case "Escape":
                await CloseCallout();
                break;
        }
    }

    private async Task HandleOnClockWheel(WheelEventArgs e)
    {
        if (IsWheelSpinEnabled is false) return;

        // The wheel only moves the dial the user is actually on. Reacting to a merely hovered one - or to a
        // scroll that carries no modifier - would silently change the time while the page is being scrolled.
        if (e.ShiftKey is false || _clockHasFocus is false || e.DeltaY == 0) return;

        await MoveByStep((e.DeltaY < 0) != InvertMouseWheel ? 1 : -1);
    }

    private async Task MoveByStep(int steps)
    {
        if (IsInteractive is false) return;

        // Seeded before the parts are read rather than inside the setter below: the step is measured from
        // where the picker already is, so an empty one has to start from the starting value first.
        SeedFromStartingValue();

        if (IsHourView)
        {
            var step = Math.Max(1, HourStep);
            var current = _hour ?? 0;
            var target = _hour.HasValue ? current + (steps * step) : current;

            var hour = FindNearestAllowedHour(target, Math.Sign(steps));
            if (hour.HasValue) await SetHour(hour.Value);

            return;
        }

        var partStep = Math.Max(1, IsMinuteView ? MinuteStep : SecondStep);
        var part = CurrentPart ?? 0;
        var wanted = CurrentPart.HasValue ? part + (steps * partStep) : part;

        if (IsMinuteView)
        {
            var minute = FindNearestAllowedMinute(((wanted % 60) + 60) % 60, Math.Sign(steps));
            if (minute.HasValue) await SetMinute(minute.Value);
        }
        else
        {
            var second = FindNearestAllowedSecond(((wanted % 60) + 60) % 60, Math.Sign(steps));
            if (second.HasValue) await SetSecond(second.Value);
        }
    }

    private async Task MoveToEdge(bool first)
    {
        if (IsInteractive is false) return;

        SeedFromStartingValue();

        if (IsHourView)
        {
            for (var offset = 0; offset < 24; offset++)
            {
                var hour = first ? offset : 23 - offset;

                if (IsHourAllowed(hour) is false) continue;

                await SetHour(hour);
                return;
            }

            return;
        }

        for (var offset = 0; offset < 60; offset++)
        {
            var value = first ? offset : 59 - offset;

            if (IsSelectable(value) is false) continue;

            await (IsMinuteView ? SetMinute(value) : SetSecond(value));
            return;
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

    // The dial is the part of an opened picker the keyboard acts on, so it takes the focus - unless the input
    // is editable, where the text the person came to type has to keep it.
    private async Task FocusClock()
    {
        if (AllowTextInput || IsRendered is false || IsDisposed) return;

        try
        {
            await _clockRef.FocusAsync();
        }
        catch (JSDisconnectedException) { } // we can ignore this exception here
    }

    private string? GetHourButtonStyle()
    {
        var style = $"{Styles?.HourButton?.Trim(';')};{(IsHourView ? Styles?.SelectedButtons : null)}".Trim(';');
        return style.HasValue() ? style : null;
    }

    private string? GetMinuteButtonStyle()
    {
        var style = $"{Styles?.MinuteButton?.Trim(';')};{(IsMinuteView ? Styles?.SelectedButtons : null)}".Trim(';');
        return style.HasValue() ? style : null;
    }

    private string? GetSecondButtonStyle()
    {
        var style = $"{Styles?.SecondButton?.Trim(';')};{(IsSecondView ? Styles?.SelectedButtons : null)}".Trim(';');
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

    private string GetClockPointerClass()
    {
        List<string> classes = ["bit-ctp-ptr"];

        if (IsOnInnerRing)
        {
            classes.Add("bit-ctp-pti");
        }

        // The hand is only animated between one resting place and the next: during a drag it has to follow the
        // pointer, and a transition would leave it trailing behind the finger that is moving it.
        if (_isPointerDown is false)
        {
            classes.Add("bit-ctp-ani");
        }

        if (Classes?.ClockPointer.HasValue() ?? false)
        {
            classes.Add(Classes.ClockPointer!);
        }

        return string.Join(' ', classes);
    }

    private string? GetClockPointerStyle(double degree)
    {
        var style = FormattableString.Invariant($"transform:rotateZ({degree}deg);{Styles?.ClockPointer?.Trim(';')}").Trim(';');
        return style.HasValue() ? style : null;
    }

    private string? GetClockPointerThumbStyle(bool isOnNumber)
    {
        var style = $"{Styles?.ClockPointerThumb?.Trim(';')};{(isOnNumber ? null : Styles?.ClockPointerThumbMinute)}".Trim(';');
        return style.HasValue() ? style : null;
    }

    private string GetCalloutCssClasses()
    {
        List<string> classes = ["bit-ctp-cal"];

        if (ColorClass.HasValue())
        {
            classes.Add(ColorClass);
        }

        if (SizeClass.HasValue())
        {
            classes.Add(SizeClass);
        }

        if (Classes?.Callout is not null)
        {
            classes.Add(Classes.Callout);
        }

        if (Landscape)
        {
            classes.Add("bit-ctp-lnd");
        }

        if (Standalone)
        {
            classes.Add("bit-ctp-sta");
        }

        // The responsive class hides the callout off the top of a small screen until the JS that opens it slides
        // it in. A standalone picker never goes through that - it has no callout to open - so carrying the class
        // would leave the clock permanently invisible on a narrow viewport.
        if (Responsive && Standalone is false)
        {
            classes.Add("bit-ctp-res");
        }

        if (IsEnabled is false)
        {
            classes.Add("bit-dis");
        }

        if (IsEnabled && ReadOnly)
        {
            classes.Add("bit-ctp-rdl");
        }

        return string.Join(' ', classes).Trim();
    }



    private bool IsHourView => _view == BitCircularTimePickerView.Hour;

    private bool IsMinuteView => _view == BitCircularTimePickerView.Minute;

    private bool IsSecondView => _view == BitCircularTimePickerView.Second;

    // Whether the picker deals in seconds at all: the mode that edits nothing but them brings them along on
    // its own, so the two never have to be set together.
    private bool HasSeconds => ShowSeconds || EditMode == BitCircularTimePickerEditMode.OnlySeconds;

    // The part of the time the dial is on, in its own units, or nothing when that part has not been set yet.
    // The three views only differ in which field they read, so everything that is about the current one - the
    // selected paint, the hand, the keyboard steps - goes through this.
    private int? CurrentPart => _view switch
    {
        BitCircularTimePickerView.Hour => _hour,
        BitCircularTimePickerView.Minute => _minute,
        _ => _second
    };

    private string ViewKey => _view switch
    {
        BitCircularTimePickerView.Hour => "h",
        BitCircularTimePickerView.Minute => "m",
        _ => "s"
    };

    private string ViewTitle => _view switch
    {
        BitCircularTimePickerView.Hour => HourButtonTitle,
        BitCircularTimePickerView.Minute => MinuteButtonTitle,
        _ => SecondButtonTitle
    };

    // What the live region of the callout carries. The name of the part the dial is on is enough wherever the
    // hand rests on a number, since the option it points at is announced along with it. Between two marks
    // there is no option to announce - a minute of :37 has no number of its own - so the value is spelled out
    // here instead, which is what keeps a keyboard step of one from moving the dial in silence.
    private string ViewAnnouncement => (_activeDescendantId is null && CurrentPart.HasValue)
        ? $"{ViewTitle} {CurrentPart.Value.ToString("D2", CultureInfo.InvariantCulture)}"
        : ViewTitle;

    // Whether the dial currently accepts a change, which is what every pointer, keyboard and button path is
    // gated on so none of them has to repeat the three states that close the picker to the user.
    private bool IsInteractive => IsEnabled && ReadOnly is false && InvalidValueBinding() is false;

    // Rendered onto the dial as a data attribute rather than pushed over interop, so the script that has to
    // cancel the browser's own Shift+wheel scrolling reads the current state straight off the element it is
    // already listening on, and stays in step with it without a call per change.
    private bool IsWheelSpinEnabled => IsInteractive && NoMouseWheel is false;

    // Whether the row under the clock is rendered at all. The states that close the picker to the user only
    // disable the buttons inside it, so a picker that is switched off does not also change height.
    private bool HasActions => ShowNowButton || ShowClearButton;

    private string ColorClass => BitCssClasses.Color(Color, "bit-ctp");

    private string SizeClass => Size switch
    {
        BitSize.Small => "bit-ctp-sm",
        BitSize.Large => "bit-ctp-lg",
        _ => "bit-ctp-md"
    };

    // Where the 24-hour dial stops reading the pointer as the outer ring and starts reading it as the inner
    // one: halfway between the two radii the numbers are laid out on, as a fraction of the radius of the dial.
    // The stylesheet keeps both radii in the same proportion to the dial at every size (the midpoint works out
    // between 0.69 and 0.71 of the radius across the three of them), so the one boundary holds for all of them.
    private const double InnerRingThreshold = 0.70;

    private BitCircularTimePickerView GetInitialView()
    {
        return EditMode switch
        {
            BitCircularTimePickerEditMode.OnlyHours => BitCircularTimePickerView.Hour,
            BitCircularTimePickerEditMode.OnlyMinutes => BitCircularTimePickerView.Minute,
            BitCircularTimePickerEditMode.OnlySeconds => BitCircularTimePickerView.Second,
            // A start view of a part the picker does not carry is no start view at all, so it falls back to the
            // one every picker has rather than opening onto a dial nothing can be picked on.
            _ => IsViewEditable(StartView) ? StartView : BitCircularTimePickerView.Hour
        };
    }

    private bool IsViewEditable(BitCircularTimePickerView view)
    {
        return EditMode switch
        {
            BitCircularTimePickerEditMode.OnlyHours => view == BitCircularTimePickerView.Hour,
            BitCircularTimePickerEditMode.OnlyMinutes => view == BitCircularTimePickerView.Minute,
            BitCircularTimePickerEditMode.OnlySeconds => view == BitCircularTimePickerView.Second,
            _ => view != BitCircularTimePickerView.Second || HasSeconds
        };
    }

    // The bounds are times of day, so one that falls outside of a day is pulled back into one before anything
    // is compared against it. Without it the dial reads the parts of a bound (a MinTime of 25:00 has an Hours
    // of 1) while the typed input compares the whole span, and the two would disagree about the same value.
    // Disabling the past or the future puts the current time in as a bound of its own, and where that and a
    // declared bound overlap the narrower of the two wins.
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

    // The current time at the precision the dial works in, so the minute that is running is still one that can
    // be picked: a bound of 10:30:45 would leave 10:30 in the past on a picker that cannot pick seconds.
    private TimeSpan NowBound()
    {
        var now = DateTime.Now.TimeOfDay;

        return HasSeconds
            ? new TimeSpan(now.Hours, now.Minutes, now.Seconds)
            : new TimeSpan(now.Hours, now.Minutes, 0);
    }

    // Where the grids of the steps start: the minimum the application declared, so a picker whose range begins
    // at 09:07 can still be set to 09:07. A grid pinned to the top of the range instead would leave the first
    // time of the range itself off it.
    private TimeSpan? GridAnchor => BitTimeSteps.ClampToDay(MinTime);

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

    private bool IsMinuteAllowed(int minute)
    {
        if (minute is < 0 or > 59) return false;

        if (BitTimeSteps.IsOnGrid(minute, MinuteStep, GridAnchor?.Minutes ?? 0, 60) is false) return false;

        if (AllowedMinutes is not null && AllowedMinutes(minute) is false) return false;

        // The bounds only bite inside their own hour: every minute of an hour that is strictly within the range
        // is selectable, and no minute of an hour outside of it is - which the hour ring has already refused.
        var hour = _hour;

        if (hour.HasValue is false) return true;

        var min = MinBound;
        var max = MaxBound;

        if (min.HasValue && hour == min.Value.Hours && minute < min.Value.Minutes) return false;

        if (max.HasValue && hour == max.Value.Hours && minute > max.Value.Minutes) return false;

        return true;
    }

    private bool IsSecondAllowed(int second)
    {
        if (second is < 0 or > 59) return false;

        if (BitTimeSteps.IsOnGrid(second, SecondStep, GridAnchor?.Seconds ?? 0, 60) is false) return false;

        if (AllowedSeconds is not null && AllowedSeconds(second) is false) return false;

        // As with the minutes, the bounds only bite inside the minute they fall in.
        if (_hour.HasValue is false || _minute.HasValue is false) return true;

        var min = MinBound;
        var max = MaxBound;

        if (min.HasValue && _hour == min.Value.Hours && _minute == min.Value.Minutes
                         && second < min.Value.Seconds) return false;

        if (max.HasValue && _hour == max.Value.Hours && _minute == max.Value.Minutes
                         && second > max.Value.Seconds) return false;

        return true;
    }

    // Walks outwards from the wanted value until it lands on one the constraints allow, so a keyboard step or
    // a "now" that falls into a disabled gap moves past it instead of stopping dead on it.
    private int? FindNearestAllowedHour(int hour, int direction = 0)
    {
        return FindNearestAllowed(hour, direction, 24, IsHourAllowed);
    }

    private int? FindNearestAllowedMinute(int minute, int direction = 0)
    {
        return FindNearestAllowed(minute, direction, 60, IsMinuteAllowed);
    }

    private int? FindNearestAllowedSecond(int second, int direction = 0)
    {
        return FindNearestAllowed(second, direction, 60, IsSecondAllowed);
    }

    // A direction of zero walks outwards from the value and takes the first allowed one on either side; one
    // of 1 or -1 only walks the way it was given, which is what a keyboard step that fell into a disabled gap
    // needs so it carries on the way it was going instead of turning back.
    private static int? FindNearestAllowed(int value, int direction, int range, Func<int, bool> isAllowed)
    {
        value = BitTimeSteps.Wrap(value, range);

        if (direction > 0) return BitTimeSteps.FindAllowedFrom(value, true, range, isAllowed);

        if (direction < 0) return BitTimeSteps.FindAllowedFrom(value, false, range, isAllowed);

        return BitTimeSteps.FindNearestAllowed(value, range, isAllowed);
    }

    private static int GetAmPmHours(int hours)
    {
        var result = hours % 12;
        return result == 0 ? 12 : result;
    }



    /// <inheritdoc />
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

        if (DateTime.TryParseExact(value, GetParseFormats(), _culture, DateTimeStyles.None, out DateTime parsedValue) is false)
        {
            result = default;
            validationErrorMessage = InvalidErrorMessage.HasValue()
                ? InvalidErrorMessage!
                : $"The {DisplayName ?? FieldIdentifier.FieldName} field is not valid.";
            return false;
        }

        // Text is the only way a time the dial itself refuses can reach the component, so a time that reads
        // correctly but falls outside of the range is reported as the range problem it is rather than as an
        // unreadable one - the two are different mistakes and deserve different messages.
        if (IsWithinBounds(parsedValue.TimeOfDay) is false)
        {
            result = default;
            validationErrorMessage = OutOfRangeErrorMessage.HasValue()
                ? OutOfRangeErrorMessage!
                : $"The {DisplayName ?? FieldIdentifier.FieldName} field is out of the allowed range.";
            return false;
        }

        result = parsedValue.TimeOfDay;
        _hour = result.Value.Hours;
        _minute = result.Value.Minutes;
        _second = result.Value.Seconds;
        validationErrorMessage = null;
        return true;
    }

    protected override string? FormatValueAsString(TimeSpan? value)
    {
        if (value.HasValue is false) return null;

        DateTime time = DateTime.Today.Add(value.Value);

        return time.ToString(GetValueFormat(), _culture);
    }

    // Only the bounds are enforced on typed text, not the steps or the predicates: a step is the granularity
    // the dial moves in, and a typed time that is inside the range is a time the person meant.
    private bool IsWithinBounds(TimeSpan time)
    {
        var min = MinBound;
        var max = MaxBound;

        if (min.HasValue && time < min.Value) return false;

        if (max.HasValue && time > max.Value) return false;

        return true;
    }



    protected override async ValueTask DisposeAsync(bool disposing)
    {
        if (IsDisposed || disposing is false) return;

        await base.DisposeAsync(disposing);

        OnValueChanged -= HandleOnValueChanged;

        try
        {
            await _js.BitCircularTimePickerDispose(_abortControllerId);
            await _js.BitCalloutClearCallout(_calloutId);
            await _js.BitSwipesDispose(_calloutId);
        }
        catch (JSDisconnectedException) { } // we can ignore this exception here
        finally
        {
            // Owned here rather than left to the swipe handler, which is only set up in the responsive mode
            // and bails out on its own on wide viewports - leaking the reference in every other case.
            _dotnetObj?.Dispose();
        }
    }
}
