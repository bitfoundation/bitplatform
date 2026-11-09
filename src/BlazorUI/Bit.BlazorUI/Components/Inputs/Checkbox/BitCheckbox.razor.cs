using System.Diagnostics.CodeAnalysis;

namespace Bit.BlazorUI;

/// <summary>
/// BitCheckbox is a component that permits the user to make a binary choice, a choice between one of two possible mutually exclusive options.
/// It supports an indeterminate state, three-state cycling, per-state icons, a visible description, cancellable changes, a busy state,
/// read-only and required modes, and is keyboard accessible through its underlying native checkbox input.
/// </summary>
public partial class BitCheckbox : BitInputBase<bool>
{
    private bool _autoLoading;
    private bool _isChanging;
    private bool _hasAttempted;
    private bool _hasLiveRegion;
    private int _liveTextId;
    private string? _liveText;
    private string? _announcedLiveText;
    private string _inputId = string.Empty;
    private string _errorId = string.Empty;
    private string _errorLabelId = string.Empty;
    private string _descriptionId = string.Empty;
    private string _ariaDescriptionId = string.Empty;



    [Inject] private IJSRuntime _js { get; set; } = default!;



    /// <summary>
    /// Gets or sets the cascading parameters for the checkbox component.
    /// </summary>
    /// <remarks>
    /// This property receives its value from an ancestor component via Blazor's cascading parameter mechanism.
    /// <br />
    /// The intended use is to allow shared configuration or settings to be applied to multiple checkbox components through the <see cref="BitParams"/> component.
    /// </remarks>
    [CascadingParameter(Name = BitCheckboxParams.ParamName)]
    public BitCheckboxParams? CascadingParameters { get; set; }



    /// <summary>
    /// Keeps the disabled checkbox focusable and discoverable by assistive technologies.
    /// When enabled, the disabled state is conveyed using the <c>aria-disabled</c> attribute instead of the
    /// native <c>disabled</c> attribute, so the checkbox remains in the tab order while its toggling is suppressed.
    /// </summary>
    [Parameter] public bool AllowDisabledFocus { get; set; }

    /// <summary>
    /// The id of the element the checkbox controls, rendered as <c>aria-controls</c> on the checkbox input.
    /// </summary>
    /// <remarks>
    /// The relationship a select-all checkbox has with the list it governs: it points at the container of
    /// the items it checks and clears, so assistive technologies can offer the way from the one to the other.
    /// </remarks>
    [Parameter] public string? AriaControls { get; set; }

    /// <summary>
    /// The ids of the elements that describe the checkbox, rendered into <c>aria-describedby</c> beside the
    /// ids the component contributes itself.
    /// </summary>
    /// <remarks>
    /// Use it to point the checkbox at text the page already shows - a validation summary, a hint shared by a
    /// group of inputs. <see cref="Description"/> and <see cref="AriaDescription"/> are added to the same
    /// attribute, so the three can be combined.
    /// </remarks>
    [Parameter] public string? AriaDescribedby { get; set; }

    /// <summary>
    /// Detailed description of the checkbox for the benefit of screen readers, rendered as a visually
    /// hidden element that the checkbox input points to via <c>aria-describedby</c>.
    /// </summary>
    /// <remarks>
    /// Use <see cref="Description"/> instead when the explanation should also be visible on the page.
    /// </remarks>
    [Parameter] public string? AriaDescription { get; set; }

    /// <summary>
    /// ID for element that contains label information for the checkbox
    /// </summary>
    [Parameter] public string? AriaLabelledby { get; set; }

    /// <summary>
    /// The position in the parent set (if in a set) for aria-posinset
    /// </summary>
    [Parameter] public int? AriaPositionInSet { get; set; }

    /// <summary>
    /// The total size of the parent set (if in a set) for aria-setsize
    /// </summary>
    [Parameter] public int? AriaSetSize { get; set; }

    /// <summary>
    /// Moves the focus onto the checkbox when it first renders.
    /// </summary>
    /// <remarks>
    /// The <c>autofocus</c> attribute is rendered for a statically rendered page, where the browser acts on
    /// it while parsing the document; an interactively rendered one is focused from code after the first
    /// render instead, since by then the parse the attribute belongs to is long over. A disabled checkbox
    /// is skipped either way unless <see cref="AllowDisabledFocus"/> keeps it focusable.
    /// </remarks>
    [Parameter] public bool AutoFocus { get; set; }

    /// <summary>
    /// Turns the checkbox busy by itself for as long as the callbacks behind a change are still running.
    /// </summary>
    /// <remarks>
    /// It is the <see cref="Loading"/> state in every respect - the glyph becomes a spinner and the checkbox
    /// stops accepting clicks - only raised and cleared by the checkbox itself around the awaited
    /// <see cref="OnClick"/>, <see cref="OnChanging"/> and <c>OnChange</c> callbacks. A <see cref="Loading"/>
    /// set from the outside still applies on top of it, so the two can be mixed.
    /// </remarks>
    [Parameter] public bool AutoLoading { get; set; }

    /// <summary>
    /// Gets or sets the check icon using custom CSS classes for external icon libraries.
    /// Takes precedence over <see cref="CheckIconName"/> when both are set.
    /// </summary>
    /// <remarks>
    /// Use this property to render icons from external libraries like FontAwesome, Material Icons, or Bootstrap Icons.
    /// For built-in Fluent UI icons, use <see cref="CheckIconName"/> instead.
    /// </remarks>
    /// <example>
    /// Bootstrap: CheckIcon="BitIconInfo.Bi("gear-fill")"
    /// FontAwesome: CheckIcon="BitIconInfo.Fa("solid house")"
    /// Custom CSS: CheckIcon="BitIconInfo.Css("my-icon-class")"
    /// </example>
    [Parameter] public BitIconInfo? CheckIcon { get; set; }

    /// <summary>
    /// The name of the built-in icon to render as the check mark inside the checkbox.
    /// </summary>
    [Parameter] public string? CheckIconName { get; set; }

    /// <summary>
    /// The aria label of the icon for the benefit of screen readers.
    /// </summary>
    /// <remarks>
    /// The glyph inside the box is decorative by default - the state of the checkbox is already announced by
    /// the input itself - so it is hidden from assistive technologies. Setting this exposes it as an image
    /// with that name, read as part of the checkbox. Worth doing only where the glyph carries meaning the
    /// label does not, such as a custom icon standing for the state rather than repeating the check mark. The name
    /// takes hold only while there is a glyph to see: an empty box holds the check mark at zero opacity, to be
    /// previewed on hover, and naming it there would have it read out while it is not on the screen.
    /// </remarks>
    [Parameter] public string? CheckIconAriaLabel { get; set; }

    /// <summary>
    /// Used to customize the content of checkbox(Label and Box).
    /// </summary>
    [Parameter, ResetClassBuilder]
    public RenderFragment? ChildContent { get; set; }

    /// <summary>
    /// Custom CSS classes for different parts of the BitCheckbox.
    /// </summary>
    [Parameter, ResetClassBuilder] public BitCheckboxClassStyles? Classes { get; set; }

    /// <summary>
    /// The general color of the checkbox.
    /// </summary>
    /// <remarks>
    /// An explicit value wins over the <c>--bit-Checkbox-*</c> color variables (the checked, indeterminate, focus and
    /// disabled colors); left unset, the checkbox is primary unless those variables say otherwise.
    /// </remarks>
    [Parameter, ResetClassBuilder]
    public BitColor? Color { get; set; }

    /// <summary>
    /// Default indeterminate visual state for checkbox
    /// </summary>
    [Parameter] public bool? DefaultIndeterminate { get; set; }

    /// <summary>
    /// A visible explanation of what checking the box means, rendered on a line of its own under it and
    /// announced after the name of the checkbox through <c>aria-describedby</c>.
    /// </summary>
    /// <remarks>
    /// This is the second line of a settings row - the sentence that says what agreeing actually commits to.
    /// Use <see cref="AriaDescription"/> instead when it should only be announced. It is indented to line up
    /// with the label rather than with the box, and sits outside the click target, so selecting the text of
    /// the description does not toggle the checkbox.
    /// </remarks>
    [Parameter, ResetClassBuilder]
    public string? Description { get; set; }

    /// <summary>
    /// Custom description of the checkbox, replacing <see cref="Description"/> with arbitrary markup.
    /// </summary>
    [Parameter, ResetClassBuilder]
    public RenderFragment? DescriptionTemplate { get; set; }

    /// <summary>
    /// A line under the checkbox saying why its state was rejected, which marks it invalid in the same way
    /// <see cref="Invalid"/> does and is announced the moment it shows up rather than only on the next focus.
    /// </summary>
    /// <remarks>
    /// It is meant for a rejection the app itself knows about - a server that refused to save the new state,
    /// a rule spanning two controls. A checkbox inside an <c>EditForm</c> already gets its messages from the
    /// cascading EditContext through the <c>ValidationMessage</c> component.
    /// </remarks>
    [Parameter, ResetClassBuilder]
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// The text a screen reader is given for a rejection that has no text of its own: an
    /// <see cref="ErrorMessageTemplate"/>, which is free to render anything at all, or the forced
    /// <see cref="Invalid"/> state. It is announced through the live region of the checkbox the moment the
    /// rejection shows up - in place of the default "Invalid input" for a template, and only when it is set for
    /// the forced state, whose reason is usually explained (and announced) elsewhere - and the checkbox
    /// references it through its aria-describedby attribute, so the same reason is read out on every later
    /// focus too. A plain <see cref="ErrorMessage"/> is its own text and never uses it.
    /// </summary>
    /// <remarks>
    /// Give it the reason the template shows, in the language of the page: it is compared as text, so a
    /// template switching to another reason is only announced again when this text switches along with it.
    /// </remarks>
    [Parameter] public string? ErrorMessageAriaLabel { get; set; }

    /// <summary>
    /// Custom content of the error message, replacing the plain <see cref="ErrorMessage"/> text and marking
    /// the checkbox invalid in the same way.
    /// </summary>
    /// <remarks>
    /// It replaces the message for a screen reader as well: a template is free to render anything at all, so
    /// the live region announces <see cref="ErrorMessageAriaLabel"/> for it, or a bare "Invalid input" when
    /// that is not set - never the plain <see cref="ErrorMessage"/> it hides.
    /// </remarks>
    [Parameter, ResetClassBuilder]
    public RenderFragment? ErrorMessageTemplate { get; set; }

    /// <summary>
    /// Stretches the checkbox across the full available width, pushing the box and the label to opposite edges.
    /// </summary>
    /// <remarks>
    /// The shape of a settings row: the label on the reading side of the line and the box at the far edge.
    /// It applies to the single-line label placements only - stacked above or below, the label already spans
    /// the whole width. Pair it with <see cref="NoWrap"/> where the label has to stay on one line.
    /// </remarks>
    [Parameter, ResetClassBuilder]
    public bool FullWidth { get; set; }

    /// <summary>
    /// An indeterminate visual state for checkbox.
    /// The indeterminate state takes visual precedence over the checked state but does not affect the Value.
    /// </summary>
    [Parameter, ResetClassBuilder, ResetStyleBuilder, TwoWayBound]
    [CallOnSet(nameof(OnSetIndeterminate))]
    public bool Indeterminate { get; set; }

    /// <summary>
    /// Gets or sets the icon to render in the indeterminate state using custom CSS classes for external icon libraries,
    /// replacing the default filled square. Takes precedence over <see cref="IndeterminateIconName"/> when both are set.
    /// </summary>
    [Parameter, ResetClassBuilder]
    public BitIconInfo? IndeterminateIcon { get; set; }

    /// <summary>
    /// The name of the built-in icon to render in the indeterminate state, replacing the default filled square.
    /// </summary>
    [Parameter, ResetClassBuilder]
    public string? IndeterminateIconName { get; set; }

    /// <summary>
    /// Marks the state of the checkbox as invalid, giving a state rejected by something other than the cascading
    /// EditContext - a server, a rule of the app, a validator of its own - the same look and the same
    /// <c>aria-invalid</c> attribute that a failing data annotation gives it.
    /// </summary>
    /// <remarks>
    /// A checkbox failing its own validation stays invalid regardless of this parameter. Pair it with
    /// <see cref="ErrorMessage"/> to say what is wrong; a message on its own already implies this state.
    /// On its own it says nothing more to a screen reader unless <see cref="ErrorMessageAriaLabel"/> gives it a
    /// reason to announce.
    /// </remarks>
    [Parameter, ResetClassBuilder]
    public bool Invalid { get; set; }

    /// <summary>
    /// Descriptive label for the checkbox.
    /// </summary>
    [Parameter, ResetClassBuilder]
    public string? Label { get; set; }

    /// <summary>
    /// The position of the label in regards to the checkbox box.
    /// Takes precedence over <see cref="Reversed"/> when both are set.
    /// </summary>
    /// <remarks>
    /// Only Top, Bottom, Start and End are meaningful here; the physical pair and the two combined values
    /// leave the layout as it is with this unset.
    /// </remarks>
    [Parameter, ResetClassBuilder]
    public BitPlacement? LabelPlacement { get; set; }

    /// <summary>
    /// Used to customize the label for the checkbox.
    /// </summary>
    [Parameter, ResetClassBuilder]
    public RenderFragment? LabelTemplate { get; set; }

    /// <summary>
    /// Turns the checkbox busy while the change it has just accepted is still being carried out.
    /// </summary>
    /// <remarks>
    /// A busy checkbox keeps the state it is in and ignores clicks, but stays focusable and is announced as
    /// busy and unavailable, so a change that is still in flight is not started a second time. The spinner
    /// replaces the glyph inside the box and is drawn to the size the glyph would have been, so turning a
    /// checkbox busy never resizes it. A <see cref="ChildContent"/> replaces that box along with the glyph,
    /// so a custom face shows the state itself - the root still carries the <c>bit-chb-ldg</c> class to
    /// style against. Use <see cref="AutoLoading"/> to have the checkbox raise this state itself around the
    /// callbacks of a change.
    /// </remarks>
    [Parameter, ResetClassBuilder]
    public bool Loading { get; set; }

    /// <summary>
    /// Keeps the label of the checkbox on a single line and ends it with an ellipsis where it does not fit.
    /// </summary>
    /// <remarks>
    /// The label wraps onto as many lines as it needs by default, which is what keeps a long one readable.
    /// Turn this on where the layout has a width of its own to protect - a stretched settings row, a toolbar,
    /// a cell of a grid - and pair it with a <see cref="Title"/> so the part that was cut off is still
    /// reachable. It has no effect on a checkbox left to hug its content, which is never narrower than its label.
    /// </remarks>
    [Parameter, ResetClassBuilder]
    public bool NoWrap { get; set; }

    /// <summary>
    /// Callback for when the checkbox loses focus.
    /// </summary>
    [Parameter] public EventCallback<FocusEventArgs> OnBlur { get; set; }

    /// <summary>
    /// Callback invoked before the state of the checkbox changes, letting the change be cancelled.
    /// </summary>
    /// <remarks>
    /// Set <c>Cancel</c> on the provided <see cref="BitCheckboxChangeArgs"/> to keep the current state.
    /// Since the callback is awaited, it can also run asynchronous work like a confirmation prompt.
    /// </remarks>
    [Parameter] public EventCallback<BitCheckboxChangeArgs> OnChanging { get; set; }

    /// <summary>
    ///  Callback that is called when the check box is clicked
    /// </summary>
    [Parameter] public EventCallback<MouseEventArgs> OnClick { get; set; }

    /// <summary>
    /// Callback for when the checkbox receives focus.
    /// </summary>
    [Parameter] public EventCallback<FocusEventArgs> OnFocus { get; set; }

    /// <summary>
    /// Callback for when the focus moves into the checkbox.
    /// </summary>
    [Parameter] public EventCallback<FocusEventArgs> OnFocusIn { get; set; }

    /// <summary>
    /// Callback for when the focus moves out of the checkbox.
    /// </summary>
    [Parameter] public EventCallback<FocusEventArgs> OnFocusOut { get; set; }

    /// <summary>
    /// Reverses the label and checkbox location.
    /// </summary>
    [Parameter, ResetClassBuilder]
    public bool Reversed { get; set; }

    /// <summary>
    /// The size of the checkbox.
    /// </summary>
    /// <remarks>
    /// An explicit value wins over <c>--bit-Checkbox-box-size</c>, <c>--bit-Checkbox-font-size</c> and
    /// <c>--bit-Checkbox-description-font-size</c>; left unset, the checkbox is medium unless those variables say
    /// otherwise.
    /// </remarks>
    [Parameter, ResetClassBuilder]
    public BitSize? Size { get; set; }

    /// <summary>
    /// If true, stops the click event from bubbling up to the parent elements.
    /// </summary>
    [Parameter] public bool StopPropagation { get; set; }

    /// <summary>
    /// Custom CSS styles for different parts of the BitCheckbox.
    /// </summary>
    [Parameter, ResetStyleBuilder] public BitCheckboxClassStyles? Styles { get; set; }

    /// <summary>
    /// Enables cycling through the unchecked, checked and indeterminate states on each click,
    /// instead of the indeterminate state being reachable only programmatically.
    /// </summary>
    [Parameter] public bool ThreeState { get; set; }

    /// <summary>
    /// Title text applied to the label container of the checkbox.
    /// </summary>
    [Parameter] public string? Title { get; set; }

    /// <summary>
    /// Gets or sets the icon to render in the unchecked state using custom CSS classes for external icon libraries.
    /// Takes precedence over <see cref="UncheckedIconName"/> when both are set.
    /// </summary>
    [Parameter, ResetClassBuilder]
    public BitIconInfo? UncheckedIcon { get; set; }

    /// <summary>
    /// The name of the built-in icon to render in the unchecked state.
    /// By default the unchecked box is empty and previews the check icon on hover.
    /// </summary>
    [Parameter, ResetClassBuilder]
    public string? UncheckedIconName { get; set; }



    protected override async Task OnInitializedAsync()
    {
        // The cascade is read here as well as in OnParametersSet because DefaultIndeterminate is acted on while
        // initializing, which is before OnParametersSet has run for the first time - a cascaded one applied only
        // there would arrive after the state it is the default for has already been settled.
        CascadingParameters?.UpdateParameters(this);

        _inputId = $"BitCheckbox-{UniqueId}-input";
        _errorId = $"BitCheckbox-{UniqueId}-error";
        _errorLabelId = $"BitCheckbox-{UniqueId}-error-label";
        _descriptionId = $"BitCheckbox-{UniqueId}-description";
        _ariaDescriptionId = $"BitCheckbox-{UniqueId}-aria-description";

        OnValueChanged += HandleOnValueChanged;

        SetDefaultValue();

        if (IndeterminateHasBeenSet is false && DefaultIndeterminate is not null)
        {
            await SetIndeterminate(DefaultIndeterminate.Value);
        }

        await base.OnInitializedAsync();
    }

    protected override void OnParametersSet()
    {
        CascadingParameters?.UpdateParameters(this);

        // The live region is only rendered for a checkbox that is wired to say something through it - one whose
        // ErrorMessage, ErrorMessageTemplate or ErrorMessageAriaLabel is written at all, even as a null for now -
        // so a list of plain checkboxes does not carry an empty live region per row. A label a BitParams hands down
        // is not written by the markup, yet it is said out of the region the moment Invalid turns on, so holding
        // one wires the checkbox too. Once there it stays, since it has to be on the page before the text that is
        // meant to be announced out of it.
        _hasLiveRegion |= HasNotBeenSet(nameof(ErrorMessage)) is false
                          || HasNotBeenSet(nameof(ErrorMessageTemplate)) is false
                          || HasNotBeenSet(nameof(ErrorMessageAriaLabel)) is false
                          || ErrorMessageAriaLabel.HasValue()
                          || LiveText is not null;

        SyncLiveRegion();

        base.OnParametersSet();
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            // A freshly rendered input already has its indeterminate property at false, so only a checkbox
            // that starts out mixed has anything to push - which keeps a grid of selection checkboxes from
            // opening with one interop call per row that changes nothing.
            if (Indeterminate) await SetIndeterminate();

            // The autofocus attribute is only honoured while the browser is parsing the document, which is
            // never when the markup arrives from an interactive render - so the attribute alone covers the
            // statically rendered page and nothing else. The focus is moved here for the rest.
            if (AutoFocus && (Disabled is false || AllowDisabledFocus))
            {
                await InputElement.FocusSafelyAsync();
            }
        }

        await base.OnAfterRenderAsync(firstRender);
    }

    protected override string RootElementClass => "bit-chb";

    protected override void RegisterCssClasses()
    {
        ClassBuilder.Register(() => Classes?.Root);

        // Color and Size publish nothing while they are unset, which is what lets the stylesheet tell a default
        // from a choice: the public --bit-Checkbox-* variables restyle the default and never an explicit value.
        ClassBuilder.Register(() => BitCssClasses.Color(Color, "bit-chb"));

        ClassBuilder.Register(() => BitCssClasses.Size(Size, "bit-chb"));

        ClassBuilder.Register(() => CurrentValue ? $"bit-chb-ckd {Classes?.Checked}" : string.Empty);

        ClassBuilder.Register(() => Indeterminate ? $"bit-chb-ind {Classes?.Indeterminate}" : string.Empty);

        ClassBuilder.Register(() => Reversed ? "bit-chb-rvs" : string.Empty);

        ClassBuilder.Register(() => LabelPlacement switch
        {
            BitPlacement.Top => "bit-chb-ltp",
            BitPlacement.Bottom => "bit-chb-lbt",
            BitPlacement.Start => "bit-chb-lst",
            BitPlacement.End => "bit-chb-lnd",
            _ => string.Empty
        });

        ClassBuilder.Register(() => ReadOnly ? "bit-chb-rdl" : string.Empty);

        ClassBuilder.Register(() => IsLoading ? "bit-chb-ldg" : string.Empty);

        // The asterisk hangs on the label, so a checkbox named only by an AriaLabel has nowhere to put one
        // and says it is required through the native attribute alone.
        ClassBuilder.Register(() => Disabled is false && Required && HasLabel ? "bit-chb-req" : string.Empty);

        ClassBuilder.Register(() => (UncheckedIcon is not null || UncheckedIconName.HasValue()) ? "bit-chb-uci" : string.Empty);

        ClassBuilder.Register(() => (IndeterminateIcon is not null || IndeterminateIconName.HasValue()) ? "bit-chb-cii" : string.Empty);

        ClassBuilder.Register(() => FullWidth ? "bit-chb-fwi" : string.Empty);

        ClassBuilder.Register(() => NoWrap ? "bit-chb-nwr" : string.Empty);

        ClassBuilder.Register(() => HasDescription ? "bit-chb-hds" : string.Empty);

        // An error message takes a line of its own under the checkbox too, and frees the label row from the full
        // width in the same way - through a class of its own rather than by widening what the description class
        // means.
        ClassBuilder.Register(() => HasErrorMessage ? "bit-chb-her" : string.Empty);

        // The focus ring is drawn around the box, which a custom face replaces - so the checkbox says that
        // it has no box and the stylesheet puts the ring around the whole face instead of nowhere.
        ClassBuilder.Register(() => ChildContent is not null ? "bit-chb-cct" : string.Empty);
    }

    protected override void RegisterCssStyles()
    {
        StyleBuilder.Register(() => Styles?.Root);

        StyleBuilder.Register(() => CurrentValue ? Styles?.Checked : string.Empty);

        StyleBuilder.Register(() => Indeterminate ? Styles?.Indeterminate : string.Empty);
    }

    // The value lands in the `value` attribute a form posts, which is markup rather than text for a reader,
    // so it is pinned to the invariant "true"/"false" instead of the "True"/"False" ToString would give it.
    protected override string? FormatValueAsString(bool value) => value ? "true" : "false";

    protected override bool TryParseValueFromString(string? value, [MaybeNullWhen(false)] out bool result, [NotNullWhen(false)] out string? parsingErrorMessage)
        => throw new NotSupportedException($"This component does not parse string inputs. Bind to the '{nameof(CurrentValue)}' property, not '{nameof(CurrentValueAsString)}'.");



    /// <summary>
    /// Whether the checkbox renders a label of its own, which is what the required asterisk hangs on.
    /// </summary>
    private bool HasLabel => LabelTemplate is not null || Label.HasValue();

    /// <summary>
    /// Whether the checkbox carries a visible description, which is what the extra line under it is rendered for.
    /// </summary>
    private bool HasDescription => DescriptionTemplate is not null || Description.HasValue();

    /// <summary>
    /// Whether the checkbox carries a visible error message, which is what the error line is rendered for.
    /// </summary>
    private bool HasErrorMessage => ErrorMessageTemplate is not null || ErrorMessage.HasValue();

    /// <summary>
    /// Whether the app itself has rejected the state of the checkbox, by the flag or by a message saying why.
    /// </summary>
    /// <remarks>
    /// A message saying what is wrong with the state is a rejection of it, so it marks the checkbox the same
    /// way the flag does instead of leaving a red line under a box that still looks accepted.
    /// </remarks>
    protected override bool HasError => Invalid || HasErrorMessage;

    /// <summary>
    /// Whether <see cref="ErrorMessageAriaLabel"/> speaks for the rejection on screen: a template, which wins
    /// over a plain message and is free to render anything at all, or the forced state without a message.
    /// </summary>
    /// <remarks>
    /// A whitespace label is no label - announcing it would leave the checkbox sounding accepted.
    /// </remarks>
    private bool HasErrorAriaLabel => ErrorMessageAriaLabel.HasValue()
                                      && (ErrorMessageTemplate is not null || (Invalid && ErrorMessage.HasNoValue()));

    /// <summary>
    /// What the live region carries, which is the rejection on screen and nothing else.
    /// </summary>
    /// <remarks>
    /// It always matches what is shown: a plain message is its own text, while a template - never read out
    /// itself, since a live region reading out a block of markup is worse than one saying nothing - is
    /// announced through its label, or as "Invalid input" rather than left in silence. The forced state is
    /// the one rejection only announced with a label of its own, its reason being explained elsewhere.
    /// </remarks>
    private string? LiveText => HasErrorAriaLabel ? ErrorMessageAriaLabel
                                : ErrorMessageTemplate is not null ? BitStrings.Current.InvalidInput
                                : ErrorMessage.HasValue() ? ErrorMessage
                                : null;

    /// <summary>
    /// Whether the checkbox is busy, from the <see cref="Loading"/> parameter or from
    /// <see cref="AutoLoading"/> having raised it around a change of its own.
    /// </summary>
    private bool IsLoading => Loading || _autoLoading;

    /// <summary>
    /// Whether the checkbox currently accepts a change from the user.
    /// </summary>
    /// <remarks>
    /// A change that is still running its awaited callbacks also closes the checkbox to further clicks, so a
    /// second click landing while the first one is in flight cannot start a competing change - which on a
    /// three-state checkbox would skip a state, and on any of them would ask <see cref="OnChanging"/> about
    /// a move from a state the checkbox is no longer in.
    /// </remarks>
    private bool IsInteractive => Disabled is false && ReadOnly is false && IsLoading is false && _isChanging is false;

    private BitIconInfo? GetStateIcon()
    {
        if (Indeterminate)
        {
            return BitIconInfo.From(IndeterminateIcon, IndeterminateIconName);
        }

        if (CurrentValue is false)
        {
            var uncheckedIcon = BitIconInfo.From(UncheckedIcon, UncheckedIconName);

            if (uncheckedIcon is not null) return uncheckedIcon;
        }

        return BitIconInfo.From(CheckIcon, CheckIconName ?? "Accept");
    }

    private (bool value, bool indeterminate) GetNextState()
    {
        if (ThreeState)
        {
            if (Indeterminate) return (false, false);

            if (CurrentValue) return (false, true);

            return (true, false);
        }

        return (CurrentValue is false, false);
    }

    /// <summary>
    /// Pushes the mixed state the component holds onto the <c>indeterminate</c> property of the element,
    /// which is a DOM property rather than an attribute and so cannot be rendered.
    /// </summary>
    private async Task SetIndeterminate()
    {
        await _js.BitUtilsSetProperty(InputElement, "indeterminate", Indeterminate);
    }

    // Before the first render there is no element to push onto yet: the state the checkbox starts out with is pushed
    // from OnAfterRenderAsync instead, whether the markup or a BitParams handed it down.
    private void OnSetIndeterminate()
    {
        if (IsRendered is false) return;

        _ = SetIndeterminate();
    }

    private Task HandleOnBlur(FocusEventArgs e) => OnBlur.InvokeAsync(e);

    private Task HandleOnFocus(FocusEventArgs e) => OnFocus.InvokeAsync(e);

    private Task HandleOnFocusIn(FocusEventArgs e) => OnFocusIn.InvokeAsync(e);

    private Task HandleOnFocusOut(FocusEventArgs e) => OnFocusOut.InvokeAsync(e);

    private async Task HandleOnCheckboxClick(MouseEventArgs args)
    {
        // A disabled or read-only checkbox never arrives here having let the browser change anything: the
        // click is prevented in the markup, which is where a state known at render time belongs.
        if (Disabled || ReadOnly) return;

        // A change still running its awaited callbacks is only found out about here, after the browser has
        // already toggled the element under the pointer - so what the checkbox still holds is put back.
        if (IsInteractive is false)
        {
            await RestoreInputState();
            return;
        }

        // Held for the whole handler rather than only around the change itself, so a click landing while an
        // awaited OnClick or OnChanging is still running is dropped instead of racing the change it precedes.
        _isChanging = true;

        // read once, so the flag that is cleared in the end is the one that was raised in the beginning
        // even if the parameter is swapped while the change is still running
        var autoLoading = AutoLoading;

        if (autoLoading) SetAutoLoading(true);

        try
        {
            await OnClick.InvokeAsync(args);

            var oldValue = CurrentValue;
            var (newValue, newIndeterminate) = GetNextState();

            _hasAttempted = true;

            if (OnChanging.HasDelegate)
            {
                var changingArgs = new BitCheckboxChangeArgs(newValue, newIndeterminate);

                await OnChanging.InvokeAsync(changingArgs);

                if (changingArgs.Cancel)
                {
                    await SyncInputCheckedProperty(oldValue);
                    // the browser also clears the native indeterminate property on every click, so it gets put back too
                    await SetIndeterminate();
                    return;
                }
            }

            if (newIndeterminate != Indeterminate)
            {
                await SetIndeterminate(newIndeterminate);
            }

            CurrentValue = newValue;

            await SyncInputCheckedProperty(oldValue);
        }
        finally
        {
            _isChanging = false;

            // What the app made of the attempt is only known once its callbacks are done, so that is when it is
            // announced - and an error it still shows is said again even when it is the very message the last
            // attempt was refused with, which is a rejection all the same. The render that follows the click
            // carries it to the page.
            SyncLiveRegion(_hasAttempted);

            _hasAttempted = false;

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
        // Skipped once the checkbox is gone, which an awaited callback leaves room for it to be by now.
        if (IsDisposed) return;

        StateHasChanged();
    }

    /// <summary>
    /// Puts back onto the element the two native properties a click changes behind the component's back,
    /// for a click the component then refuses outright.
    /// </summary>
    private async Task RestoreInputState()
    {
        await _js.BitUtilsSetProperty(InputElement, "checked", CurrentValue);
        await SetIndeterminate();
    }

    private async Task SyncInputCheckedProperty(bool oldValue)
    {
        // the browser toggles the native checked property on every click; when the committed value is not
        // that toggle result (a cancelled, one-way bound or three-state change), the property gets put back
        if (CurrentValue == (oldValue is false)) return;

        await _js.BitUtilsSetProperty(InputElement, "checked", CurrentValue);
    }

    /// <summary>
    /// Brings the live region in step with the message, announcing it only when it is not what was said last.
    /// </summary>
    /// <remarks>
    /// Nothing is announced while a change is in flight: the parameters may come and go several times as the
    /// callbacks of the change re-render the page, and only where they land once the change is over is its
    /// outcome. The text sits in a keyed element so every announcement replaces the element rather than only
    /// rewriting its text - the same message said twice would otherwise change nothing in the DOM, and a live
    /// region that did not change is one nothing is read out of.
    /// </remarks>
    /// <param name="reannounce">Says a message again even when it is the one that was said last.</param>
    private void SyncLiveRegion(bool reannounce = false)
    {
        if (_isChanging) return;

        var text = LiveText;

        if (text == _announcedLiveText && (reannounce is false || text is null)) return;

        _announcedLiveText = text;
        _liveText = text;
        _liveTextId++;
    }

    private void HandleOnValueChanged(object? sender, EventArgs args)
    {
        ClassBuilder.Reset();
        StyleBuilder.Reset();
    }

    private async Task SetIndeterminate(bool value)
    {
        // A one-way bound Indeterminate refuses the change and keeps the state it was given. The browser
        // has already cleared the native property on the click that got here, so what the parameter still
        // says is put back rather than left contradicting the box on the screen.
        if (await AssignIndeterminate(value) is false)
        {
            await SetIndeterminate();
            return;
        }

        // A change that was taken has already run OnSetIndeterminate, which pushed the property to the
        // element; pushing it a second time here would be an interop call for nothing.
    }



    protected override async ValueTask DisposeAsync(bool disposing)
    {
        if (IsDisposed || disposing is false) return;

        OnValueChanged -= HandleOnValueChanged;

        await base.DisposeAsync(disposing);
    }
}
