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
    private string _inputId = string.Empty;
    private string _descriptionId = string.Empty;
    private string _ariaDescriptionId = string.Empty;



    [Inject] private IJSRuntime _js { get; set; } = default!;



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
    [Parameter] public BitCheckboxClassStyles? Classes { get; set; }

    /// <summary>
    /// The general color of the checkbox.
    /// </summary>
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
    [Parameter, ResetClassBuilder, TwoWayBound]
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
    /// Descriptive label for the checkbox.
    /// </summary>
    [Parameter, ResetClassBuilder]
    public string? Label { get; set; }

    /// <summary>
    /// The position of the label in regards to the checkbox box.
    /// Takes precedence over <see cref="Reversed"/> when both are set.
    /// </summary>
    [Parameter, ResetClassBuilder]
    public BitLabelPosition? LabelPosition { get; set; }

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
    [Parameter, ResetClassBuilder]
    public BitSize? Size { get; set; }

    /// <summary>
    /// If true, stops the click event from bubbling up to the parent elements.
    /// </summary>
    [Parameter] public bool StopPropagation { get; set; }

    /// <summary>
    /// Custom CSS styles for different parts of the BitCheckbox.
    /// </summary>
    [Parameter] public BitCheckboxClassStyles? Styles { get; set; }

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
        _inputId = $"BitCheckbox-{UniqueId}-input";
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
            if (AutoFocus && (IsEnabled || AllowDisabledFocus))
            {
                try
                {
                    await InputElement.FocusAsync();
                }
                catch (JSDisconnectedException) { } // the circuit is gone (e.g. the user navigated away), nothing to focus
                catch (JSException) { } // the element is no longer in the document, failing to focus it is not fatal
                catch (InvalidOperationException) { } // the element reference is detached from its renderer, same as above
            }
        }

        await base.OnAfterRenderAsync(firstRender);
    }

    protected override string RootElementClass => "bit-chb";

    protected override void RegisterCssClasses()
    {
        ClassBuilder.Register(() => Classes?.Root);

        ClassBuilder.Register(() => Color switch
        {
            BitColor.Primary => "bit-chb-pri",
            BitColor.Secondary => "bit-chb-sec",
            BitColor.Tertiary => "bit-chb-ter",
            BitColor.Info => "bit-chb-inf",
            BitColor.Success => "bit-chb-suc",
            BitColor.Warning => "bit-chb-wrn",
            BitColor.SevereWarning => "bit-chb-swr",
            BitColor.Error => "bit-chb-err",
            BitColor.PrimaryBackground => "bit-chb-pbg",
            BitColor.SecondaryBackground => "bit-chb-sbg",
            BitColor.TertiaryBackground => "bit-chb-tbg",
            BitColor.PrimaryForeground => "bit-chb-pfg",
            BitColor.SecondaryForeground => "bit-chb-sfg",
            BitColor.TertiaryForeground => "bit-chb-tfg",
            BitColor.PrimaryBorder => "bit-chb-pbr",
            BitColor.SecondaryBorder => "bit-chb-sbr",
            BitColor.TertiaryBorder => "bit-chb-tbr",
            _ => "bit-chb-pri"
        });

        ClassBuilder.Register(() => Size switch
        {
            BitSize.Small => "bit-chb-sm",
            BitSize.Medium => "bit-chb-md",
            BitSize.Large => "bit-chb-lg",
            _ => "bit-chb-md"
        });

        ClassBuilder.Register(() => CurrentValue ? $"bit-chb-ckd {Classes?.Checked}" : string.Empty);

        ClassBuilder.Register(() => Indeterminate ? $"bit-chb-ind {Classes?.Indeterminate}" : string.Empty);

        ClassBuilder.Register(() => Reversed ? "bit-chb-rvs" : string.Empty);

        ClassBuilder.Register(() => LabelPosition switch
        {
            BitLabelPosition.Top => "bit-chb-ltp",
            BitLabelPosition.Bottom => "bit-chb-lbt",
            BitLabelPosition.Start => "bit-chb-lst",
            BitLabelPosition.End => "bit-chb-lnd",
            _ => string.Empty
        });

        ClassBuilder.Register(() => ReadOnly ? "bit-chb-rdl" : string.Empty);

        ClassBuilder.Register(() => IsLoading ? "bit-chb-ldg" : string.Empty);

        // The asterisk hangs on the label, so a checkbox named only by an AriaLabel has nowhere to put one
        // and says it is required through the native attribute alone.
        ClassBuilder.Register(() => IsEnabled && Required && HasLabel ? "bit-chb-req" : string.Empty);

        ClassBuilder.Register(() => (UncheckedIcon is not null || UncheckedIconName.HasValue()) ? "bit-chb-uci" : string.Empty);

        ClassBuilder.Register(() => (IndeterminateIcon is not null || IndeterminateIconName.HasValue()) ? "bit-chb-cii" : string.Empty);

        ClassBuilder.Register(() => FullWidth ? "bit-chb-fwi" : string.Empty);

        ClassBuilder.Register(() => NoWrap ? "bit-chb-nwr" : string.Empty);

        ClassBuilder.Register(() => HasDescription ? "bit-chb-hds" : string.Empty);

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
    private bool IsInteractive => IsEnabled && ReadOnly is false && IsLoading is false && _isChanging is false;

    /// <summary>
    /// The value of an attribute the page wrote into <see cref="BitInputBase{TValue}.InputHtmlAttributes"/>
    /// rather than as a parameter of the component.
    /// </summary>
    /// <remarks>
    /// Those attributes are splatted onto the very input the component writes its own aria-* onto, and a null
    /// written over a splatted attribute does not leave that attribute alone - it removes it. So every name
    /// that is also reachable by hand is resolved against what the page wrote instead of being overwritten
    /// with the parameter's null. HTML attribute names are case insensitive, and so is the deduplication the
    /// render tree does, which is why a differently cased spelling has to be found here too.
    /// </remarks>
    private string? GetSplattedInputAttribute(string name)
    {
        if (InputHtmlAttributes is null || InputHtmlAttributes.Count == 0) return null;

        if (InputHtmlAttributes.TryGetValue(name, out var value)) return value?.ToString();

        foreach (var attribute in InputHtmlAttributes)
        {
            if (string.Equals(attribute.Key, name, StringComparison.OrdinalIgnoreCase)) return attribute.Value?.ToString();
        }

        return null;
    }

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

    private void OnSetIndeterminate()
    {
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
        if (IsEnabled is false || ReadOnly) return;

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
