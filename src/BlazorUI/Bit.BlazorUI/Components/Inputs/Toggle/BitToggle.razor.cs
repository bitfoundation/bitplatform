using System.Diagnostics.CodeAnalysis;

namespace Bit.BlazorUI;

/// <summary>
/// A toggle represents a physical switch that allows someone to choose between two mutually exclusive options.
/// For example, “On/Off”, “Show/Hide”. It supports state texts, custom content inside the track, an icon or a
/// template inside the knob, a description, an error message, a loading state, cancellable changes, read-only
/// and required modes, label placement on any side, and it is fully operable from the keyboard.
/// </summary>
public partial class BitToggle : BitInputBase<bool>
{
    private bool _isChanging;
    private bool _autoLoading;
    private string? _errorId;
    private string? _labelId;
    private string? _buttonId;
    private string? _stateText;
    private string? _stateTextId;
    private string? _labelledById;
    private string? _describedById;
    private string? _descriptionId;
    private string? _ariaDescriptionId;
    private ElementReference _buttonRef;
    private string? _ariaChecked => CurrentValue ? "true" : "false";



    /// <summary>
    /// Gets or sets the cascading parameters for the toggle component.
    /// </summary>
    /// <remarks>
    /// This property receives its value from an ancestor component via Blazor's cascading parameter mechanism.
    /// <br />
    /// The intended use is to allow shared configuration or settings to be applied to multiple toggle components through the <see cref="BitParams"/> component.
    /// </remarks>
    [CascadingParameter(Name = BitToggleParams.ParamName)]
    public BitToggleParams? CascadingParameters { get; set; }



    /// <summary>
    /// Keeps a disabled toggle focusable and discoverable by assistive technologies.
    /// </summary>
    /// <remarks>
    /// The disabled state is then conveyed by <c>aria-disabled</c> rather than by the native <c>disabled</c>
    /// attribute, so the switch stays in the tab order, keeps answering the pointer - which is what lets its
    /// <see cref="Title"/> explain why it cannot be used - and still refuses every change.
    /// </remarks>
    [Parameter] public bool AllowDisabledFocus { get; set; }

    /// <summary>
    /// The id of the element the toggle controls, rendered as <c>aria-controls</c> on the switch.
    /// </summary>
    /// <remarks>
    /// Use it when flipping the toggle governs another region of the page - a panel it reveals, a list it
    /// filters - so assistive technologies can tie the switch to what it acts on.
    /// </remarks>
    [Parameter] public string? AriaControls { get; set; }

    /// <summary>
    /// Detailed description of the toggle for the benefit of screen readers, rendered as a visually
    /// hidden element that the switch points to via <c>aria-describedby</c>.
    /// </summary>
    /// <remarks>
    /// Use <see cref="Description"/> instead when the explanation should be visible to everyone.
    /// </remarks>
    [Parameter] public string? AriaDescription { get; set; }

    /// <summary>
    /// The id of an existing element that describes the toggle, rendered as part of <c>aria-describedby</c> on the switch.
    /// </summary>
    /// <remarks>
    /// It is added to what the toggle already describes itself with - its state text and its
    /// <see cref="AriaDescription"/> - rather than replacing either of them, so an explanation already on the
    /// page can be announced after the name without being repeated inside the toggle.
    /// </remarks>
    [Parameter] public string? AriaDescribedby { get; set; }

    /// <summary>
    /// The id of an existing element that labels the toggle, rendered as <c>aria-labelledby</c> on the switch.
    /// </summary>
    /// <remarks>
    /// Takes precedence over the label of the toggle and over <see cref="BitComponentBase.AriaLabel"/>, so an
    /// external heading or caption already on the page can name the switch instead of being repeated inside it.
    /// </remarks>
    [Parameter] public string? AriaLabelledby { get; set; }

    /// <summary>
    /// If true, the toggle automatically receives focus when the page renders (rendered as the <c>autofocus</c> attribute).
    /// </summary>
    [Parameter] public bool AutoFocus { get; set; }

    /// <summary>
    /// Turns the toggle busy on its own for as long as the callbacks behind a change are still running,
    /// without a loading flag having to be tracked outside the component.
    /// </summary>
    /// <remarks>
    /// It is the <see cref="Loading"/> state in every respect - the knob becomes a spinner and the toggle
    /// stops accepting clicks - only raised and cleared by the toggle itself around the awaited
    /// <see cref="OnClick"/>, <see cref="OnChanging"/> and <c>OnChange</c> callbacks. A <see cref="Loading"/>
    /// set from the outside still applies on top of it, so the two can be mixed.
    /// </remarks>
    [Parameter] public bool AutoLoading { get; set; }

    /// <summary>
    /// Custom CSS classes for different parts of the toggle.
    /// </summary>
    [Parameter] public BitToggleClassStyles? Classes { get; set; }

    /// <summary>
    /// The general color of the toggle, applied to the track of the checked state.
    /// The unchecked track stays neutral so it keeps reading as "off" whichever color is picked.
    /// </summary>
    [Parameter, ResetClassBuilder]
    public BitColor? Color { get; set; }

    /// <summary>
    /// A visible explanation of what the toggle switches, rendered on a line of its own under it and
    /// announced after the name of the switch through <c>aria-describedby</c>.
    /// </summary>
    /// <remarks>
    /// This is the second line of a settings row - the sentence that says what turning the switch on
    /// actually does. Use <see cref="AriaDescription"/> instead when it should only be announced.
    /// </remarks>
    [Parameter, ResetClassBuilder]
    public string? Description { get; set; }

    /// <summary>
    /// Custom description of the toggle, replacing <see cref="Description"/> with arbitrary markup.
    /// </summary>
    [Parameter, ResetClassBuilder]
    public RenderFragment? DescriptionTemplate { get; set; }

    /// <summary>
    /// A line under the toggle saying why its state was rejected, which marks it invalid in the same way
    /// <see cref="Invalid"/> does and is announced the moment it shows up rather than only on the next focus.
    /// </summary>
    /// <remarks>
    /// It is meant for a rejection the app itself knows about - a server that refused to save the new state,
    /// a rule spanning two controls. A toggle inside an <c>EditForm</c> already gets its messages from the
    /// cascading EditContext through the <c>ValidationMessage</c> component.
    /// </remarks>
    [Parameter, ResetClassBuilder]
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Custom content of the error message, replacing the plain <see cref="ErrorMessage"/> text and marking
    /// the toggle invalid in the same way.
    /// </summary>
    /// <remarks>
    /// Only the plain <see cref="ErrorMessage"/> is announced by the live region, since a template is free
    /// to render anything at all - set both to have a message that is both formatted and announced.
    /// </remarks>
    [Parameter, ResetClassBuilder]
    public RenderFragment? ErrorMessageTemplate { get; set; }

    /// <summary>
    /// Renders the toggle in full width of its container while putting space between the label and the knob.
    /// </summary>
    [Parameter, ResetClassBuilder]
    public bool FullWidth { get; set; }

    /// <summary>
    /// Renders the label and the knob in a single line together.
    /// </summary>
    [Parameter, ResetClassBuilder]
    public bool Inline { get; set; }

    /// <summary>
    /// Marks the state of the toggle as invalid, giving a state rejected by something other than the cascading
    /// EditContext - a server, a rule of the app, a validator of its own - the same look and the same
    /// <c>aria-invalid</c> attribute that a failing data annotation gives it.
    /// </summary>
    /// <remarks>
    /// A toggle failing its own validation stays invalid regardless of this parameter. Pair it with
    /// <see cref="ErrorMessage"/> to say what is wrong; a message on its own already implies this state.
    /// </remarks>
    [Parameter, ResetClassBuilder]
    public bool Invalid { get; set; }

    /// <summary>
    /// Label of the toggle.
    /// </summary>
    [Parameter, ResetClassBuilder]
    public string? Label { get; set; }

    /// <summary>
    /// The position of the label in regards to the knob of the toggle.
    /// Takes precedence over <see cref="Inline"/> and <see cref="Reversed"/> when set.
    /// </summary>
    [Parameter, ResetClassBuilder]
    public BitLabelPosition? LabelPosition { get; set; }

    /// <summary>
    /// Custom label of the toggle.
    /// </summary>
    [Parameter, ResetClassBuilder]
    public RenderFragment? LabelTemplate { get; set; }

    /// <summary>
    /// Renders a spinner in place of whatever the knob carries and suspends the toggle until the pending
    /// work behind the change is done.
    /// </summary>
    /// <remarks>
    /// A loading toggle keeps its current state and ignores clicks, but stays focusable and is
    /// announced as busy and unavailable, so the change that is still in flight is not toggled a second
    /// time. The spinner is drawn to the size of the knob it takes over, so turning the toggle
    /// busy never resizes it. Use <see cref="AutoLoading"/> to have the toggle raise this state itself
    /// around the callbacks of a change.
    /// </remarks>
    [Parameter, ResetClassBuilder]
    public bool Loading { get; set; }

    /// <summary>
    /// Content rendered inside the track of the toggle while it is OFF, next to the knob.
    /// </summary>
    /// <remarks>
    /// It is hidden from assistive technologies, which already get the state from <c>aria-checked</c>,
    /// and it widens the track to fit rather than overflowing it.
    /// </remarks>
    [Parameter, ResetClassBuilder]
    public RenderFragment? OffContent { get; set; }

    /// <summary>
    /// Gets or sets the icon rendered inside the knob while the toggle is OFF, using custom CSS classes
    /// for external icon libraries. Takes precedence over <see cref="OffIconName"/> when both are set.
    /// </summary>
    /// <remarks>
    /// Use this property to render icons from external libraries like FontAwesome, Material Icons, or Bootstrap Icons.
    /// For built-in Fluent UI icons, use <see cref="OffIconName"/> instead.
    /// </remarks>
    /// <example>
    /// Bootstrap: OffIcon="BitIconInfo.Bi("x-lg")"
    /// FontAwesome: OffIcon="BitIconInfo.Fa("solid xmark")"
    /// Custom CSS: OffIcon="BitIconInfo.Css("my-icon-class")"
    /// </example>
    [Parameter, ResetClassBuilder]
    public BitIconInfo? OffIcon { get; set; }

    /// <summary>
    /// The name of the built-in icon rendered inside the knob while the toggle is OFF.
    /// For external icon libraries, use <see cref="OffIcon"/> instead.
    /// </summary>
    [Parameter, ResetClassBuilder]
    public string? OffIconName { get; set; }

    /// <summary>
    /// Text to display when toggle is OFF.
    /// </summary>
    [Parameter] public string? OffText { get; set; }

    /// <summary>
    /// Content rendered inside the track of the toggle while it is ON, next to the knob.
    /// </summary>
    /// <remarks>
    /// It is hidden from assistive technologies, which already get the state from <c>aria-checked</c>,
    /// and it widens the track to fit rather than overflowing it.
    /// </remarks>
    [Parameter, ResetClassBuilder]
    public RenderFragment? OnContent { get; set; }

    /// <summary>
    /// Callback for when the toggle receives focus.
    /// </summary>
    [Parameter] public EventCallback<FocusEventArgs> OnFocus { get; set; }

    /// <summary>
    /// Callback for when focus moves into the toggle.
    /// </summary>
    [Parameter] public EventCallback<FocusEventArgs> OnFocusIn { get; set; }

    /// <summary>
    /// Callback for when focus moves out of the toggle.
    /// </summary>
    [Parameter] public EventCallback<FocusEventArgs> OnFocusOut { get; set; }

    /// <summary>
    /// Gets or sets the icon rendered inside the knob while the toggle is ON, using custom CSS classes
    /// for external icon libraries. Takes precedence over <see cref="OnIconName"/> when both are set.
    /// </summary>
    /// <remarks>
    /// Use this property to render icons from external libraries like FontAwesome, Material Icons, or Bootstrap Icons.
    /// For built-in Fluent UI icons, use <see cref="OnIconName"/> instead.
    /// </remarks>
    /// <example>
    /// Bootstrap: OnIcon="BitIconInfo.Bi("check-lg")"
    /// FontAwesome: OnIcon="BitIconInfo.Fa("solid check")"
    /// Custom CSS: OnIcon="BitIconInfo.Css("my-icon-class")"
    /// </example>
    [Parameter, ResetClassBuilder]
    public BitIconInfo? OnIcon { get; set; }

    /// <summary>
    /// The name of the built-in icon rendered inside the knob while the toggle is ON.
    /// For external icon libraries, use <see cref="OnIcon"/> instead.
    /// </summary>
    [Parameter, ResetClassBuilder]
    public string? OnIconName { get; set; }

    /// <summary>
    /// Text to display when toggle is ON.
    /// </summary>
    [Parameter] public string? OnText { get; set; }

    /// <summary>
    /// Callback for when the toggle loses focus.
    /// </summary>
    [Parameter] public EventCallback<FocusEventArgs> OnBlur { get; set; }

    /// <summary>
    /// Callback invoked before the state of the toggle changes, letting the change be cancelled.
    /// </summary>
    /// <remarks>
    /// Set <c>Cancel</c> on the provided <see cref="BitToggleChangeArgs"/> to keep the current state.
    /// Since the callback is awaited, it can also run asynchronous work like a confirmation prompt.
    /// </remarks>
    [Parameter] public EventCallback<BitToggleChangeArgs> OnChanging { get; set; }

    /// <summary>
    /// Callback for when the toggle is clicked, invoked before the state changes.
    /// </summary>
    [Parameter] public EventCallback<MouseEventArgs> OnClick { get; set; }

    /// <summary>
    /// Reverses the positions of the label and input of the toggle.
    /// </summary>
    [Parameter, ResetClassBuilder]
    public bool Reversed { get; set; }

    /// <summary>
    /// Denotes role of the toggle, default is switch.
    /// </summary>
    [Parameter] public string? Role { get; set; } = "switch";

    /// <summary>
    /// The size of the toggle.
    /// </summary>
    [Parameter, ResetClassBuilder]
    public BitSize? Size { get; set; }

    /// <summary>
    /// If true, stops the click event from bubbling up to the parent elements.
    /// </summary>
    [Parameter] public bool StopPropagation { get; set; }

    /// <summary>
    /// Custom CSS styles for different parts of the toggle.
    /// </summary>
    [Parameter] public BitToggleClassStyles? Styles { get; set; }

    /// <summary>
    /// Arbitrary content rendered inside the knob, receiving the current state of the toggle, in place of
    /// the glyph <see cref="OnIconName"/> and <see cref="OffIconName"/> would have drawn there.
    /// </summary>
    /// <remarks>
    /// The knob steps up to the roomier geometry a glyph asks for - in both states, so flipping the toggle
    /// never resizes it - and the content is hidden from assistive technologies along with the rest of the
    /// knob, which already reports its state through <c>aria-checked</c>. While the toggle is busy the
    /// spinner takes the knob back, the same way it takes it back from a glyph.
    /// </remarks>
    [Parameter, ResetClassBuilder]
    public RenderFragment<bool>? ThumbTemplate { get; set; }

    /// <summary>
    /// The default text used when the On or Off texts are null.
    /// </summary>
    [Parameter] public string? Text { get; set; }

    /// <summary>
    /// The native tooltip of the toggle, shown when the pointer rests anywhere on it.
    /// </summary>
    /// <remarks>
    /// It sits on the root rather than on the track, so the label answers a hover as well - which is what
    /// keeps it reachable on a disabled toggle, whose track itself stops answering the pointer.
    /// <see cref="AllowDisabledFocus"/> gives the track back too, along with the tab stop.
    /// </remarks>
    [Parameter] public string? Title { get; set; }



    protected override string RootElementClass => "bit-tgl";

    protected override void RegisterCssClasses()
    {
        ClassBuilder.Register(() => Classes?.Root);

        ClassBuilder.Register(() => Color switch
        {
            BitColor.Primary => "bit-tgl-pri",
            BitColor.Secondary => "bit-tgl-sec",
            BitColor.Tertiary => "bit-tgl-ter",
            BitColor.Info => "bit-tgl-inf",
            BitColor.Success => "bit-tgl-suc",
            BitColor.Warning => "bit-tgl-wrn",
            BitColor.SevereWarning => "bit-tgl-swr",
            BitColor.Error => "bit-tgl-err",
            BitColor.PrimaryBackground => "bit-tgl-pbg",
            BitColor.SecondaryBackground => "bit-tgl-sbg",
            BitColor.TertiaryBackground => "bit-tgl-tbg",
            BitColor.PrimaryForeground => "bit-tgl-pfg",
            BitColor.SecondaryForeground => "bit-tgl-sfg",
            BitColor.TertiaryForeground => "bit-tgl-tfg",
            BitColor.PrimaryBorder => "bit-tgl-pbr",
            BitColor.SecondaryBorder => "bit-tgl-sbr",
            BitColor.TertiaryBorder => "bit-tgl-tbr",
            _ => "bit-tgl-pri"
        });

        ClassBuilder.Register(() => Size switch
        {
            BitSize.Small => "bit-tgl-sm",
            BitSize.Medium => "bit-tgl-md",
            BitSize.Large => "bit-tgl-lg",
            _ => "bit-tgl-md"
        });

        ClassBuilder.Register(() => CurrentValue ? $"bit-tgl-chk {Classes?.Checked}" : string.Empty);

        ClassBuilder.Register(() => FullWidth ? "bit-tgl-fwi" : string.Empty);

        ClassBuilder.Register(() => Inline ? "bit-tgl-inl" : string.Empty);

        ClassBuilder.Register(() => Reversed ? "bit-tgl-rvs" : string.Empty);

        ClassBuilder.Register(() => LabelPosition switch
        {
            BitLabelPosition.Top => "bit-tgl-ltp",
            BitLabelPosition.Bottom => "bit-tgl-lbt",
            BitLabelPosition.Start => "bit-tgl-lst",
            BitLabelPosition.End => "bit-tgl-lnd",
            _ => string.Empty
        });

        ClassBuilder.Register(() => ReadOnly ? "bit-tgl-rdl" : string.Empty);

        ClassBuilder.Register(() => IsLoading ? "bit-tgl-ldg" : string.Empty);

        // The description takes a line of its own under the toggle, which the row shapes only have room
        // for once the root is allowed to wrap - so the wrapping is turned on only when there is one.
        ClassBuilder.Register(() => HasDescription ? "bit-tgl-hds" : string.Empty);

        // An error message asks for the same extra line, and for the same reason, so it turns the same
        // wrapping on through a class of its own rather than by widening what the description class means.
        ClassBuilder.Register(() => HasErrorMessage ? "bit-tgl-her" : string.Empty);

        // The invalid look is already registered by the base class for a value the EditContext rejected, so
        // a state rejected by the app is only marked here when the base has not marked it already - two
        // identical classes on one element say nothing the one does not.
        ClassBuilder.Register(() => HasError && ValueInvalid is not true ? "bit-inv" : string.Empty);

        ClassBuilder.Register(() => IsEnabled && Required && HasLabel ? "bit-tgl-req" : string.Empty);

        // The knob grows to hold a glyph as soon as any of them is configured, rather than only in the
        // state that has one, so that toggling never resizes the toggle underneath the pointer.
        ClassBuilder.Register(() => HasThumbContent ? "bit-tgl-tic" : string.Empty);
    }

    protected override void RegisterCssStyles()
    {
        StyleBuilder.Register(() => Styles?.Root);

        StyleBuilder.Register(() => CurrentValue ? Styles?.Checked : string.Empty);
    }

    protected override void OnInitialized()
    {
        _errorId = $"BitToggle-{UniqueId}-error";
        _labelId = $"BitToggle-{UniqueId}-label";
        _buttonId = $"BitToggle-{UniqueId}-button";
        _stateTextId = $"BitToggle-{UniqueId}-state-text";
        _descriptionId = $"BitToggle-{UniqueId}-description";
        _ariaDescriptionId = $"BitToggle-{UniqueId}-aria-description";

        SetDefaultValue();

        OnValueChanged += HandleOnValueChanged;

        base.OnInitialized();
    }

    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(BitToggleParams))]
    protected override void OnParametersSet()
    {
        CascadingParameters?.UpdateParameters(this);

        // Recomputed on every parameter change rather than only when the value moves, so a state text or
        // a label swapped from the outside is reflected by the accessible name along with the visible one.
        SetStateText();

        base.OnParametersSet();
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        // The hidden checkbox is the form value carrier of the toggle, not something the user is ever meant
        // to land on, so FocusAsync is pointed at the switch button that actually holds the tab stop instead.
        InputElement = _buttonRef;

        await base.OnAfterRenderAsync(firstRender);
    }

    /// <summary>
    /// Flips the state of the toggle from code, as a click would.
    /// </summary>
    /// <remarks>
    /// The change runs through the same path a click takes, so <see cref="OnChanging"/> still gets to cancel it
    /// and <c>OnChange</c> still reports it. Unlike a click it is not blocked by the read-only or loading states,
    /// which only close the toggle to the user; a disabled toggle changes through neither. A change that is
    /// already in flight does block it, the same way it blocks a click, so the two cannot compete.
    /// </remarks>
    public Task ToggleAsync() => ToggleAsync(CurrentValue is false);

    /// <summary>
    /// Moves the toggle to a specific state from code, doing nothing when it is already in it.
    /// </summary>
    /// <param name="value">The state to move the toggle to.</param>
    /// <inheritdoc cref="ToggleAsync()" path="/remarks"/>
    public async Task ToggleAsync(bool value)
    {
        if (_isChanging) return;

        _isChanging = true;

        // read once, so the flag that is cleared in the end is the one that was raised in the
        // beginning even if the parameter is swapped while the change is still running
        var autoLoading = AutoLoading;

        if (autoLoading) SetAutoLoading(true);

        try
        {
            await ChangeValueAsync(value);
        }
        finally
        {
            _isChanging = false;

            if (autoLoading) SetAutoLoading(false);
        }
    }


    // The value travels through the value attribute of a native checkbox, which is read back as plain text,
    // so it is pinned to the invariant "true"/"false" instead of following the culture of the UI.
    protected override string? FormatValueAsString(bool value) => value ? "true" : "false";

    protected override bool TryParseValueFromString(string? value, [MaybeNullWhen(false)] out bool result, [NotNullWhen(false)] out string? parsingErrorMessage)
        => throw new NotSupportedException($"This component does not parse string inputs. Bind to the '{nameof(CurrentValue)}' property, not '{nameof(CurrentValueAsString)}'.");



    /// <summary>
    /// Whether the toggle renders a label of its own, which is what an asterisk or an accessible name can hang on.
    /// </summary>
    private bool HasLabel => LabelTemplate is not null || Label.HasValue();

    /// <summary>
    /// Whether the track carries content in either state, which is what the stacked content cell is rendered for.
    /// </summary>
    private bool HasContent => OnContent is not null || OffContent is not null;

    /// <summary>
    /// Whether the toggle carries a visible description, which is what the extra line under it is rendered for.
    /// </summary>
    private bool HasDescription => DescriptionTemplate is not null || Description.HasValue();

    /// <summary>
    /// Whether the toggle carries a visible error message, which is what the error line is rendered for.
    /// </summary>
    private bool HasErrorMessage => ErrorMessageTemplate is not null || ErrorMessage.HasValue();

    /// <summary>
    /// Whether the app itself has rejected the state of the toggle, by the flag or by a message saying why.
    /// </summary>
    /// <remarks>
    /// A message saying what is wrong with the state is a rejection of it, so it marks the toggle the same
    /// way the flag does instead of leaving a red line under a switch that still looks accepted.
    /// </remarks>
    private bool HasError => Invalid || HasErrorMessage;

    /// <summary>
    /// Whether the switch reports itself as invalid, from the app or from the cascading EditContext.
    /// </summary>
    private bool IsInvalid => HasError || ValueInvalid is true;

    /// <summary>
    /// What the live region carries, which is the plain error message and nothing else.
    /// </summary>
    /// <remarks>
    /// Only the plain text, never the template: a template is free to render anything at all, and a live
    /// region reading out a block of markup is worse than one saying nothing.
    /// </remarks>
    private string? LiveText => ErrorMessage.HasValue() ? ErrorMessage : null;

    /// <summary>
    /// Whether the toggle is busy, from the <see cref="Loading"/> parameter or from
    /// <see cref="AutoLoading"/> having raised it around a change of its own.
    /// </summary>
    private bool IsLoading => Loading || _autoLoading;

    /// <summary>
    /// Whether the knob has something to hold - a glyph in either state, or a template - which is what
    /// decides its enlarged geometry.
    /// </summary>
    /// <remarks>
    /// The loading state is deliberately not part of it: a spinner is drawn to the size of whatever knob it
    /// lands in, so that turning a toggle busy never resizes it in front of the person waiting on it.
    /// </remarks>
    private bool HasThumbContent => ThumbTemplate is not null
                                 || OnIcon is not null || OnIconName.HasValue()
                                 || OffIcon is not null || OffIconName.HasValue();

    /// <summary>
    /// Whether the toggle currently accepts a change from the user.
    /// </summary>
    /// <remarks>
    /// A change that is still running its awaited callbacks also closes the toggle to further clicks, so a
    /// second click landing while the first one is in flight cannot start a competing change.
    /// </remarks>
    private bool IsInteractive => IsEnabled && ReadOnly is false && IsLoading is false && _isChanging is false;

    private BitIconInfo? GetStateIcon()
    {
        return CurrentValue
            ? BitIconInfo.From(OnIcon, OnIconName)
            : BitIconInfo.From(OffIcon, OffIconName);
    }

    private void HandleOnValueChanged(object? sender, EventArgs args)
    {
        SetStateText();

        ClassBuilder.Reset();
        StyleBuilder.Reset();
    }

    private Task HandleOnBlur(FocusEventArgs e) => OnBlur.InvokeAsync(e);

    private Task HandleOnFocus(FocusEventArgs e) => OnFocus.InvokeAsync(e);

    private Task HandleOnFocusIn(FocusEventArgs e) => OnFocusIn.InvokeAsync(e);

    private Task HandleOnFocusOut(FocusEventArgs e) => OnFocusOut.InvokeAsync(e);

    private async Task HandleOnClick(MouseEventArgs e)
    {
        if (IsInteractive is false) return;

        // Held for the whole handler rather than only around the change itself, so a click landing while an
        // awaited OnClick or OnChanging is still running is dropped instead of racing the change it precedes.
        _isChanging = true;

        var autoLoading = AutoLoading;

        if (autoLoading) SetAutoLoading(true);

        try
        {
            await OnClick.InvokeAsync(e);

            await ChangeValueAsync(CurrentValue is false);
        }
        finally
        {
            _isChanging = false;

            if (autoLoading) SetAutoLoading(false);
        }
    }

    private void SetAutoLoading(bool value)
    {
        if (_autoLoading == value) return;

        _autoLoading = value;

        ClassBuilder.Reset();

        // The spinner has to show up while the callbacks behind the change are still running rather than
        // after they are done, so the render is asked for here instead of being left to the handler.
        // Skipped once the toggle is gone, which an awaited callback leaves room for it to be by now.
        if (IsDisposed) return;

        StateHasChanged();
    }

    private async Task ChangeValueAsync(bool newValue)
    {
        if (IsEnabled is false || newValue == CurrentValue) return;

        // A Value passed one way with nothing to write back through dictates the state, so the change cannot
        // land at all. It is dropped here rather than deeper down, so a change that is going nowhere never
        // reaches OnChanging either - a veto callback should not be asked about a change that cannot happen.
        if (InvalidValueBinding()) return;

        if (OnChanging.HasDelegate)
        {
            var args = new BitToggleChangeArgs(newValue);

            await OnChanging.InvokeAsync(args);

            if (args.Cancel) return;
        }

        // Awaited rather than assigned through CurrentValue, whose setter starts the change without waiting
        // for it, so the callbacks of a change are done before the toggle opens up to the next one.
        await SetCurrentValueAsync(newValue);

        // A click is followed by a render of its own, but a change coming from ToggleAsync is not, and an
        // uncontrolled toggle has no binding to bring one about either - so the new state is asked for here.
        // Skipped once the toggle is gone, which an awaited OnChanging leaves room for it to be by now.
        if (IsDisposed) return;

        StateHasChanged();
    }

    private void SetStateText()
    {
        var onText = OnText ?? Text;
        var offText = OffText ?? Text;

        _stateText = CurrentValue ? onText : offText;

        // The accessible name of a switch must not change along with its state, so it is taken from the
        // label. A toggle with no label of any kind falls back to its state text, but only when that text
        // is the same in both states - a text that reads "On" one moment and "Off" the next would rename
        // the switch every time it is flipped instead of naming it. An explicitly supplied id outranks all
        // of it, since it names the switch after something the page already shows.
        _labelledById = AriaLabelledby.HasValue()
                      ? AriaLabelledby
                      : AriaLabel.HasValue()
                        ? null
                        : HasLabel
                          ? _labelId
                          : (_stateText.HasValue() && string.Equals(onText, offText, StringComparison.Ordinal)) ? _stateTextId : null;

        // What is not part of the name becomes a description instead, so a custom state text is still
        // announced - after the name, where a changing value belongs. The visible description comes before
        // the screen-reader-only one, in the order the two are read on the page.
        _describedById = string.Join(' ', new[]
        {
            _stateText.HasValue() && _labelledById != _stateTextId ? _stateTextId : null,
            HasErrorMessage ? _errorId : null,
            HasDescription ? _descriptionId : null,
            AriaDescription.HasValue() ? _ariaDescriptionId : null,
            AriaDescribedby.HasValue() ? AriaDescribedby : null
        }.Where(id => id.HasValue()));
    }



    protected override async ValueTask DisposeAsync(bool disposing)
    {
        if (IsDisposed || disposing is false) return;

        OnValueChanged -= HandleOnValueChanged;

        await base.DisposeAsync(disposing);
    }
}
