using System.Diagnostics.CodeAnalysis;

namespace Bit.BlazorUI;

/// <summary>
/// Text fields give people a way to enter and edit text. They’re used in forms, modal dialogs, tables, and other surfaces where text input is required.
/// </summary>
public partial class BitTextField : BitTextInputBase<string?>
{
    private int _charCount;
    private bool _hasText;
    private bool _hasFocus;
    private bool _jsSetup;
    private bool _ghostSetup;
    private bool _ghostSetupIsMultiline;
    private bool _multilineSetup;
    private bool _oldAutoHeight;
    private bool _oldPreventEnter;
    private bool _compositionSetup;
    private bool _compositionSetupIsMultiline;
    private bool _selectOnFocusSetup;
    private bool _selectOnFocusSetupIsMultiline;
    private int? _oldMaxRows;
    private string? _oldValue;
    private string? _oldGhostText;
    private string? _oldValueForCount;
    private Func<string?, int>? _oldCountStrategy;
    private DotNetObjectReference<BitTextField>? _dotnetObj;
    private string? _inputMode;
    private bool _trimOnParse;
    private bool _isPasswordRevealed;
    private BitInputType? _elementType;
    private string _inputId = string.Empty;
    private string _labelId = string.Empty;
    private string _countId = string.Empty;
    private string _inputType = string.Empty;
    private string _descriptionId = string.Empty;
    private string _ariaDescriptionId = string.Empty;



    [Inject] private IJSRuntime _js { get; set; } = default!;



    /// <summary>
    /// The general color of the text field used when focused.
    /// </summary>
    [Parameter, ResetClassBuilder]
    public BitColor? Accent { get; set; }

    /// <summary>
    /// Detailed description of the input for the benefit of screen readers. It is rendered into a
    /// visually hidden element that the input references through its aria-describedby attribute, which
    /// is what lets a field carry an instruction that would be too long to show next to it.
    /// </summary>
    [Parameter] public string? AriaDescription { get; set; }

    /// <summary>
    /// Sets the autocapitalize html attribute of the input element, which tells the on-screen keyboard of a
    /// mobile device whether and how the typed text should be capitalized automatically.
    /// Accepted values are "off", "none", "on", "sentences", "words" and "characters".
    /// </summary>
    [Parameter] public string? AutoCapitalize { get; set; }

    /// <summary>
    /// Sets the autocorrect html attribute of the input element, which turns the automatic correction of
    /// the typed text on or off. Useful to turn off for identifiers, codes and other non-prose values.
    /// </summary>
    [Parameter] public bool? AutoCorrect { get; set; }

    /// <summary>
    /// Automatically adjust the height of the input in Multiline mode.
    /// </summary>
    [Parameter, ResetClassBuilder]
    public bool AutoHeight { get; set; }

    /// <summary>
    /// The color kind of the text field background.
    /// </summary>
    [Parameter, ResetClassBuilder]
    public BitColorKind? Background { get; set; }

    /// <summary>
    /// The color kind of the text field border.
    /// </summary>
    [Parameter, ResetClassBuilder]
    public BitColorKind? Border { get; set; }

    /// <summary>
    /// Whether to show the reveal password button for input type 'password'.
    /// </summary>
    [Parameter]
    [CallOnSet(nameof(SetElementType))]
    public bool CanRevealPassword { get; set; }

    /// <summary>
    /// Custom CSS classes for different parts of the BitTextField.
    /// </summary>
    [Parameter] public BitTextFieldClassStyles? Classes { get; set; }

    /// <summary>
    /// The aria-label of the clear button, which is what a screen reader announces for it since the button
    /// only holds an icon.
    /// </summary>
    [Parameter] public string? ClearButtonAriaLabel { get; set; }

    /// <summary>
    /// The icon to display inside the clear button.
    /// Takes precedence over <see cref="ClearButtonIconName"/> when both are set.
    /// </summary>
    /// <remarks>
    /// Use this property to render icons from external libraries like FontAwesome, Material Icons, or Bootstrap Icons.
    /// For built-in Fluent UI icons, use <see cref="ClearButtonIconName"/> instead.
    /// </remarks>
    /// <example>
    /// Bootstrap: ClearButtonIcon="BitIconInfo.Bi("x-circle-fill")"
    /// FontAwesome: ClearButtonIcon="BitIconInfo.Fa("solid xmark")"
    /// Custom CSS: ClearButtonIcon="BitIconInfo.Css("my-icon-class")"
    /// </example>
    [Parameter] public BitIconInfo? ClearButtonIcon { get; set; }

    /// <summary>
    /// The name of the clear button's icon from the built-in Fluent UI icon set.
    /// </summary>
    /// <remarks>
    /// For external icon libraries, use <see cref="ClearButtonIcon"/> instead.
    /// </remarks>
    [Parameter] public string? ClearButtonIconName { get; set; }

    /// <summary>
    /// The custom content of the clear button, which replaces its icon.
    /// </summary>
    [Parameter] public RenderFragment? ClearButtonTemplate { get; set; }

    /// <summary>
    /// Decides how the characters of the value are counted for the counter rendered by <see cref="ShowCount"/>.
    /// Leaving it unset counts the value the way the browser counts it against <see cref="MaxLength"/> - in
    /// UTF-16 code units - which makes an emoji count as two and a flag as four. A strategy of its own counts
    /// them the way the rest of the app does instead, for instance
    /// <c>v => new StringInfo(v ?? string.Empty).LengthInTextElements</c> to count what a reader actually sees.
    /// It only changes the number that is shown: the html maxlength attribute keeps holding the keyboard back
    /// at its own count.
    /// </summary>
    [Parameter] public Func<string?, int>? CountStrategy { get; set; }

    /// <summary>
    /// The custom content of the character counter, which receives the current number of characters and
    /// replaces the default "count/maxLength" text.
    /// </summary>
    [Parameter] public RenderFragment<int>? CountTemplate { get; set; }

    /// <summary>
    /// Description displayed below the text field to provide additional details about what text to enter.
    /// </summary>
    [Parameter] public string? Description { get; set; }

    /// <summary>
    /// Shows the custom description for text field.
    /// </summary>
    [Parameter] public RenderFragment? DescriptionTemplate { get; set; }

    /// <summary>
    /// Sets the enterkeyhint html attribute of the input element, which decides the label of the return key
    /// of an on-screen keyboard. Accepted values are "enter", "done", "go", "next", "previous", "search" and "send".
    /// </summary>
    [Parameter] public string? EnterKeyHint { get; set; }

    /// <summary>
    /// Forces the text field fill 100% of its container width.
    /// </summary>
    [Parameter, ResetClassBuilder]
    public bool FullWidth { get; set; }

    /// <summary>
    /// The ghost/suggestion text displayed inline after the current cursor position.
    /// Update this value from outside (e.g. from an AI or autocomplete suggestion) to show a faded
    /// inline suggestion. The user can accept it by pressing Tab or Enter, or clicking/touching the ghost text.
    /// </summary>
    [Parameter] public string? GhostText { get; set; }

    /// <summary>
    /// Gets or sets the icon for the reveal password button when password is shown using custom CSS classes for external icon libraries.
    /// Takes precedence over <see cref="HidePasswordIconName"/> when both are set.
    /// </summary>
    [Parameter] public BitIconInfo? HidePasswordIcon { get; set; }

    /// <summary>
    /// The icon name for the reveal password button when password is shown from the built-in Fluent UI icons.
    /// </summary>
    [Parameter] public string? HidePasswordIconName { get; set; }

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
    /// The accessible name of the icon shown at the trailing end of the text field. The icon is decorative
    /// and hidden from assistive technologies by default; setting this turns it into an image with a name,
    /// which is what an icon carrying a meaning of its own (a warning, a lock, a "saved" mark) needs.
    /// </summary>
    [Parameter] public string? IconAriaLabel { get; set; }

    /// <summary>
    /// The icon name for the icon shown in the far right end of the text field from the built-in Fluent UI icons.
    /// </summary>
    /// <remarks>
    /// For external icon libraries, use <see cref="Icon"/> instead.
    /// </remarks>
    [Parameter] public string? IconName { get; set; }

    /// <summary>
    /// Sets the inputmode html attribute of the input element.
    /// </summary>
    [Parameter]
    [CallOnSet(nameof(SetInputMode))]
    public BitInputMode? InputMode { get; set; }

    /// <summary>
    /// Marks the value of the field as invalid, which gives a value rejected by something other than the
    /// cascading EditContext - a server, a rule of the app, a validator of its own - the same look and the
    /// same aria-invalid attribute that a failing data annotation gives it. A field failing its own
    /// validation stays invalid regardless of this parameter.
    /// </summary>
    [Parameter, ResetClassBuilder]
    public bool Invalid { get; set; }

    /// <summary>
    /// Label displayed above the text field and read by screen readers.
    /// </summary>
    [Parameter, ResetClassBuilder]
    public string? Label { get; set; }

    /// <summary>
    /// Where the label sits relative to the input. Leaving it unset keeps the layout each variant comes
    /// with: above the input in the default one, and next to it in the <see cref="Underlined"/> one.
    /// </summary>
    [Parameter, ResetClassBuilder]
    public BitLabelPosition? LabelPosition { get; set; }

    /// <summary>
    /// Shows the custom label for text field.
    /// </summary>
    [Parameter, ResetClassBuilder]
    public RenderFragment? LabelTemplate { get; set; }

    /// <summary>
    /// Shows a busy indicator inside the field, which is what tells the user that something is running
    /// against what was typed - a suggestion being fetched, a value being checked against a server.
    /// The field stays editable while it is on, so the typing is never interrupted by it.
    /// </summary>
    [Parameter] public bool Loading { get; set; }

    /// <summary>
    /// What a screen reader announces while <see cref="Loading"/> is on, in place of the default "Loading".
    /// It is announced whichever indicator is drawn, so a <see cref="LoadingTemplate"/> drawing a bare
    /// spinner of its own still tells an assistive technology that something is running.
    /// </summary>
    [Parameter] public string? LoadingAriaLabel { get; set; }

    /// <summary>
    /// The custom content of the busy indicator, which replaces the default spinner.
    /// </summary>
    [Parameter] public RenderFragment? LoadingTemplate { get; set; }

    /// <summary>
    /// Specifies the maximum number of characters allowed in the input.
    /// A negative value (the default) removes the limit and renders no maxlength attribute.
    /// </summary>
    [Parameter] public int MaxLength { get; set; } = -1;

    /// <summary>
    /// The maximum number of rows the input grows to in the Multiline mode while <see cref="AutoHeight"/> is
    /// enabled. Beyond that height the input keeps its size and scrolls its content instead of pushing the
    /// rest of the page down. A null value (the default) lets the input grow indefinitely.
    /// </summary>
    [Parameter] public int? MaxRows { get; set; }

    /// <summary>
    /// Specifies the minimum number of characters the input accepts, which is what the browser validates
    /// the value against before the form is submitted.
    /// A negative value (the default) removes the constraint and renders no minlength attribute.
    /// </summary>
    [Parameter] public int MinLength { get; set; } = -1;

    /// <summary>
    /// Whether or not the text field is a Multiline text field.
    /// </summary>
    /// <remarks>
    /// It only applies to the plain text types: a <see cref="Type"/> of password, number, email, tel, url or
    /// search has a behavior of its own that a textarea cannot provide, so those keep rendering an input.
    /// </remarks>
    [Parameter, ResetClassBuilder]
    public bool Multiline { get; set; }

    /// <summary>
    /// Removes the border of the text input.
    /// </summary>
    [Parameter, ResetClassBuilder]
    public bool NoBorder { get; set; }

    /// <summary>
    /// Callback for when the input loses focus. Unlike <see cref="OnFocusOut"/> it does not bubble, so it
    /// is the one to use when only the input itself losing focus is of interest.
    /// </summary>
    [Parameter] public EventCallback<FocusEventArgs> OnBlur { get; set; }

    /// <summary>
    /// Callback executed when the user clears the text field by clicking the clear button.
    /// </summary>
    [Parameter] public EventCallback OnClear { get; set; }

    /// <summary>
    /// Callback for when the input clicked.
    /// </summary>
    [Parameter] public EventCallback<MouseEventArgs> OnClick { get; set; }

    /// <summary>
    /// Callback for when the Enter key is pressed while input has focus. It is not raised while an input
    /// method editor is composing, where Enter commits the candidate rather than the value.
    /// </summary>
    [Parameter] public EventCallback<KeyboardEventArgs> OnEnter { get; set; }

    /// <summary>
    /// Callback for when the Escape key is pressed while input has focus. It is not raised while an input
    /// method editor is composing, where Escape cancels the candidate rather than the editing.
    /// </summary>
    [Parameter] public EventCallback<KeyboardEventArgs> OnEscape { get; set; }

    /// <summary>
    /// Callback for when the input receives focus. Unlike <see cref="OnFocusIn"/> it does not bubble, so it
    /// is the one to use when only the input itself receiving focus is of interest.
    /// </summary>
    [Parameter] public EventCallback<FocusEventArgs> OnFocus { get; set; }

    /// <summary>
    /// Callback for when focus moves into the input or any of its descendants, since unlike
    /// <see cref="OnFocus"/> it bubbles.
    /// </summary>
    [Parameter] public EventCallback<FocusEventArgs> OnFocusIn { get; set; }

    /// <summary>
    /// Callback for when focus moves out of the input or any of its descendants, since unlike
    /// <see cref="OnBlur"/> it bubbles.
    /// </summary>
    [Parameter] public EventCallback<FocusEventArgs> OnFocusOut { get; set; }

    /// <summary>
    /// Callback invoked when the ghost text is accepted via Tab, Enter, or click/touch.
    /// The accepted ghost text string is passed as the argument.
    /// Use this to clear or update the GhostText parameter after acceptance.
    /// </summary>
    [Parameter] public EventCallback<string?> OnGhostTextAccepted { get; set; }

    /// <summary>
    /// Callback for when a keyboard key is pressed
    /// </summary>
    [Parameter] public EventCallback<KeyboardEventArgs> OnKeyDown { get; set; }

    /// <summary>
    /// Callback for When a keyboard key is released
    /// </summary>
    [Parameter] public EventCallback<KeyboardEventArgs> OnKeyUp { get; set; }

    /// <summary>
    /// Sets the pattern html attribute of the input element, which is the regular expression the value is
    /// checked against by the browser before the form is submitted.
    /// </summary>
    [Parameter] public string? Pattern { get; set; }

    /// <summary>
    /// Enables permanent ghost mode that forces the scrollbar-gutter to always be present, preventing layout shift of the ghost text rendering.
    /// </summary>
    [Parameter, ResetClassBuilder]
    public bool PermanentGhost { get; set; }

    /// <summary>
    /// Input placeholder text.
    /// </summary>
    [Parameter] public string? Placeholder { get; set; }

    /// <summary>
    /// Prefix displayed before the text field contents. This is not included in the value.
    /// Ensure a descriptive label is present to assist screen readers, as the value does not include the prefix.
    /// </summary>
    [Parameter] public string? Prefix { get; set; }

    /// <summary>
    /// Shows the custom prefix for text field.
    /// </summary>
    [Parameter] public RenderFragment? PrefixTemplate { get; set; }

    /// <summary>
    /// Prevents the enter to add new line character into the input in the Multiline mode.
    /// </summary>
    [Parameter] public bool PreventEnter { get; set; }

    /// <summary>
    /// For multiline text fields, whether or not the field is resizable.
    /// </summary>
    [Parameter, ResetClassBuilder]
    public bool Resizable { get; set; }

    /// <summary>
    /// Aria label for the reveal password button. It stays the same in both states on purpose: the pressed
    /// state of the button is what tells a screen reader whether the password is currently revealed, and
    /// swapping the label along with it is known to be announced inconsistently.
    /// </summary>
    [Parameter] public string? RevealPasswordAriaLabel { get; set; }

    /// <summary>
    /// Gets or sets the icon for the reveal password button when password is hidden using custom CSS classes for external icon libraries.
    /// Takes precedence over <see cref="RevealPasswordIconName"/> when both are set.
    /// </summary>
    [Parameter] public BitIconInfo? RevealPasswordIcon { get; set; }

    /// <summary>
    /// The icon name for the reveal password button when password is hidden from the built-in Fluent UI icons.
    /// </summary>
    [Parameter] public string? RevealPasswordIconName { get; set; }

    /// <summary>
    /// The custom content of the reveal password button, which receives whether the password is currently
    /// revealed and replaces the default icon.
    /// </summary>
    [Parameter] public RenderFragment<bool>? RevealPasswordTemplate { get; set; }

    /// <summary>
    /// For multiline text, Number of rows.
    /// </summary>
    [Parameter] public int? Rows { get; set; }

    /// <summary>
    /// Selects the whole value when the input receives focus, so that the next keystroke replaces it.
    /// It is what a field holding a value the user is expected to overwrite rather than edit - a search
    /// term, a quantity, a generated code - usually wants. It covers a click as well as a tab, and a
    /// read-only field too, where selecting the value is exactly what makes it easy to copy.
    /// </summary>
    [Parameter] public bool SelectOnFocus { get; set; }

    /// <summary>
    /// Whether to show the clear button when the text field has a value.
    /// </summary>
    [Parameter, ResetClassBuilder]
    public bool ShowClearButton { get; set; }

    /// <summary>
    /// Shows the number of characters that were typed under the text field, followed by the
    /// <see cref="MaxLength"/> when one is set.
    /// </summary>
    [Parameter] public bool ShowCount { get; set; }

    /// <summary>
    /// The size of the text field.
    /// </summary>
    [Parameter, ResetClassBuilder]
    public BitSize? Size { get; set; }

    /// <summary>
    /// Sets the spellcheck html attribute of the input element, which turns the spell checking of the
    /// browser on or off for this input.
    /// </summary>
    [Parameter] public bool? SpellCheck { get; set; }

    /// <summary>
    /// Custom CSS styles for different parts of the BitTextField.
    /// </summary>
    [Parameter] public BitTextFieldClassStyles? Styles { get; set; }

    /// <summary>
    /// Suffix displayed after the text field contents. This is not included in the value.
    /// Ensure a descriptive label is present to assist screen readers, as the value does not include the suffix.
    /// </summary>
    [Parameter] public string? Suffix { get; set; }

    /// <summary>
    /// Shows the custom suffix for text field.
    /// </summary>
    [Parameter] public RenderFragment? SuffixTemplate { get; set; }

    /// <summary>
    /// A more descriptive title of the text field, shown by the browser as its tooltip.
    /// </summary>
    [Parameter] public string? Title { get; set; }

    /// <summary>
    /// Specifies whether to remove any leading or trailing whitespace from the value.
    /// The trimming happens when the input reports a change, which is what lets a space still be typed in
    /// the middle of a word while <see cref="BitTextInputBase{TValue}.Immediate"/> is enabled.
    /// </summary>
    [Parameter] public bool Trim { get; set; }

    /// <summary>
    /// Input type.
    /// </summary>
    [Parameter, ResetClassBuilder]
    [CallOnSet(nameof(SetElementType))]
    public BitInputType? Type { get; set; }

    /// <summary>
    /// Whether or not the text field is underlined.
    /// </summary>
    [Parameter, ResetClassBuilder]
    public bool Underlined { get; set; }

    /// <summary>
    /// Sets the wrap html attribute of the textarea rendered in the <see cref="Multiline"/> mode, which decides
    /// how the text is wrapped and whether the breaks the wrapping adds travel with the value when a form is
    /// submitted. Accepted values are "soft" (what a browser does on its own), "hard" and "off", the last of
    /// which turns the wrapping off entirely and scrolls long lines sideways instead - what a field holding
    /// code or log lines wants.
    /// </summary>
    [Parameter] public string? Wrap { get; set; }



    /// <summary>
    /// Called by JavaScript when the ghost text is accepted.
    /// </summary>
    [JSInvokable("OnGhostTextAccepted")]
    public async Task _NotifyGhostTextAccepted(string? acceptedText)
    {
        if (IsEnabled is false || ReadOnly) return;

        await OnGhostTextAccepted.InvokeAsync(acceptedText);
    }



    /// <summary>
    /// Empties the text field and raises <see cref="OnClear"/>, exactly as the clear button does (without
    /// requiring <see cref="ShowClearButton"/>, since there is no button involved). It does nothing while
    /// the field is disabled or read-only.
    /// </summary>
    public Task ClearAsync() => InvokeAsync(async () =>
    {
        if (IsEnabled is false || ReadOnly) return;

        await ClearValue();

        // Unlike the clear button, this call does not arrive through an event handler, so nothing
        // re-renders the component on its own.
        StateHasChanged();

        await OnClear.InvokeAsync();
    });

    /// <summary>
    /// Selects the whole value of the input, the programmatic counterpart of <see cref="SelectOnFocus"/>.
    /// </summary>
    public ValueTask SelectAsync() => _js.BitUtilsSelectText(InputElement);

    /// <summary>
    /// Selects the text between the two given positions, or moves the caret when they are equal. A null
    /// start counts from the beginning of the value and a null end runs to its very end, and both are
    /// clamped to the length of the value.
    /// </summary>
    /// <param name="start">The index the selection starts at.</param>
    /// <param name="end">The index the selection ends at.</param>
    public ValueTask SelectRangeAsync(int? start, int? end) => _js.BitTextFieldSetSelectionRange(InputElement, start, end);

    /// <summary>
    /// Toggles the revealed state of the value while the type of the input is password and
    /// <see cref="CanRevealPassword"/> is enabled.
    /// </summary>
    public void ToggleRevealPassword()
    {
        // Like ClearAsync, this one can be called from anywhere, so the state and the re-render it asks
        // for are handed to the renderer instead of being touched on whatever thread the caller is on.
        _ = InvokeAsync(RevealPassword);
    }

    private void RevealPassword()
    {
        if (Type is not BitInputType.Password || CanRevealPassword is false) return;

        _isPasswordRevealed = _isPasswordRevealed is false;
        SetElementType();
        StateHasChanged();
    }



    protected override string RootElementClass => "bit-tfl";

    protected override void RegisterCssClasses()
    {
        ClassBuilder.Register(() => Classes?.Root);

        ClassBuilder.Register(() => IsMultilineElement
                                    ? $"bit-tfl-{(Resizable ? "mln" : "mlf")}"
                                    : string.Empty);

        ClassBuilder.Register(() => IsMultilineElement && AutoHeight ? "bit-tfl-mla" : string.Empty);

        ClassBuilder.Register(() => IsEnabled && Required ? "bit-tfl-req" : string.Empty);

        // The base class already marks a value the EditContext rejected, so the forced state only adds the
        // class where that one did not, instead of rendering it twice on a field that is invalid both ways.
        ClassBuilder.Register(() => Invalid && ValueInvalid is not true ? "bit-inv" : string.Empty);

        ClassBuilder.Register(() => Underlined ? "bit-tfl-und" : string.Empty);

        ClassBuilder.Register(() => NoBorder ? "bit-tfl-nbd" : string.Empty);

        ClassBuilder.Register(() => ReadOnly ? "bit-tfl-rdl" : string.Empty);

        ClassBuilder.Register(() => _hasFocus ? "bit-tfl-fcs" : string.Empty);

        ClassBuilder.Register(() => _hasFocus ? Classes?.Focused : string.Empty);

        ClassBuilder.Register(() => IsEnabled && Required && HasLabel is false ? "bit-tfl-rnl" : string.Empty);

        // Leaving the position unset renders no class at all, so each variant keeps the layout it comes
        // with instead of every field suddenly being laid out by the same rule.
        ClassBuilder.Register(() => LabelPosition switch
        {
            BitLabelPosition.Top => "bit-tfl-ltp",
            BitLabelPosition.Bottom => "bit-tfl-lbt",
            BitLabelPosition.Start => "bit-tfl-lst",
            BitLabelPosition.End => "bit-tfl-led",
            _ => string.Empty
        });

        // A search input of its own draws a clear affordance in WebKit browsers, which would sit right
        // next to the one of the component; the class is what lets the stylesheet take the native one away.
        ClassBuilder.Register(() => ShowClearButton ? "bit-tfl-clb" : string.Empty);

        ClassBuilder.Register(() => FullWidth ? "bit-tfl-fwd" : string.Empty);

        ClassBuilder.Register(() => PermanentGhost ? "bit-tfl-pgt" : string.Empty);

        ClassBuilder.Register(() => Size switch
        {
            BitSize.Small => "bit-tfl-sm",
            BitSize.Medium => "bit-tfl-md",
            BitSize.Large => "bit-tfl-lg",
            _ => string.Empty
        });

        ClassBuilder.Register(() => Accent switch
        {
            BitColor.Primary => "bit-tfl-pri",
            BitColor.Secondary => "bit-tfl-sec",
            BitColor.Tertiary => "bit-tfl-ter",
            BitColor.Info => "bit-tfl-inf",
            BitColor.Success => "bit-tfl-suc",
            BitColor.Warning => "bit-tfl-wrn",
            BitColor.SevereWarning => "bit-tfl-swr",
            BitColor.Error => "bit-tfl-err",
            BitColor.PrimaryBackground => "bit-tfl-pbg",
            BitColor.SecondaryBackground => "bit-tfl-sbg",
            BitColor.TertiaryBackground => "bit-tfl-tbg",
            BitColor.PrimaryForeground => "bit-tfl-pfg",
            BitColor.SecondaryForeground => "bit-tfl-sfg",
            BitColor.TertiaryForeground => "bit-tfl-tfg",
            BitColor.PrimaryBorder => "bit-tfl-pbr",
            BitColor.SecondaryBorder => "bit-tfl-sbr",
            BitColor.TertiaryBorder => "bit-tfl-tbr",
            _ => "bit-tfl-pri"
        });

        ClassBuilder.Register(() => Background switch
        {
            BitColorKind.Primary => "bit-tfl-bpr",
            BitColorKind.Secondary => "bit-tfl-bse",
            BitColorKind.Tertiary => "bit-tfl-btr",
            BitColorKind.Transparent => "bit-tfl-btn",
            _ => "bit-tfl-bpr"
        });

        ClassBuilder.Register(() => Border switch
        {
            BitColorKind.Primary => "bit-tfl-brp",
            BitColorKind.Secondary => "bit-tfl-brs",
            BitColorKind.Tertiary => "bit-tfl-brt",
            BitColorKind.Transparent => "bit-tfl-brn",
            _ => "bit-tfl-brp"
        });
    }

    protected override void RegisterCssStyles()
    {
        StyleBuilder.Register(() => Styles?.Root);

        StyleBuilder.Register(() => _hasFocus ? Styles?.Focused : string.Empty);
    }

    protected override async Task OnInitializedAsync()
    {
        _inputId = $"BitTextField-{UniqueId}-input";
        _labelId = $"BitTextField-{UniqueId}-label";
        _countId = $"BitTextField-{UniqueId}-count";
        _descriptionId = $"BitTextField-{UniqueId}-description";
        _ariaDescriptionId = $"BitTextField-{UniqueId}-aria-description";

        // The CallOnSet handler of the Type parameter only runs when the parameter is actually assigned,
        // so the element type has to be resolved here as well for the default (unset) case.
        SetElementType();

        SetDefaultValue();

        await base.OnInitializedAsync();
    }

    protected override void OnParametersSet()
    {
        // The counter follows what the input reports while the user types, which is what keeps it live even
        // when Immediate is off. It is only re-synced from the Value when the Value itself changes, or when
        // the way the characters are counted does.
        if (_oldValueForCount != Value || _oldCountStrategy != CountStrategy)
        {
            _oldValueForCount = Value;
            _oldCountStrategy = CountStrategy;
            _hasText = string.IsNullOrEmpty(Value) is false;
            _charCount = CountOf(Value);
        }

        base.OnParametersSet();
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        await base.OnAfterRenderAsync(firstRender);

        if (firstRender)
        {
            _oldValue = Value;

            _oldMaxRows = MaxRows;
        }

        // Every one of the JavaScript features below can be turned on and off at any point in the life of
        // the component, and the element they attach to is swapped between an input and a textarea by the
        // Multiline mode, so each of them is re-wired whenever what it depends on moves rather than only
        // on the first render.
        var isMultiline = IsMultilineElement;

        await SetupMultiline(isMultiline);

        await SetupGhostText(isMultiline);

        await SetupComposition(isMultiline);

        await SetupSelectOnFocus(isMultiline);

        if (firstRender is false && isMultiline && AutoHeight && (_oldValue != Value || _oldMaxRows != MaxRows))
        {
            _oldMaxRows = MaxRows;
            await _js.BitTextFieldAdjustHeight(_Id, InputElement, MaxRows);
        }

        _oldValue = Value;
    }

    private async Task SetupMultiline(bool isMultiline)
    {
        if (isMultiline is false)
        {
            if (_multilineSetup is false) return;

            _multilineSetup = false;
            await _js.BitTextFieldDisposeFeature(_Id, "multiline");
            return;
        }

        // The two behaviors of the textarea are wired as listeners, so a change of either of them has to
        // reach the element instead of only being read once at the setup time.
        if (_multilineSetup && AutoHeight == _oldAutoHeight && PreventEnter == _oldPreventEnter) return;

        _jsSetup = true;
        _multilineSetup = true;
        _oldAutoHeight = AutoHeight;
        _oldPreventEnter = PreventEnter;

        await _js.BitTextFieldSetupMultilineInput(_Id, InputElement, AutoHeight, PreventEnter, MaxRows);

        if (AutoHeight)
        {
            // An input that is rendered with a value already in it has to be measured once, otherwise it
            // stays one row tall until the first keystroke.
            _oldMaxRows = MaxRows;
            await _js.BitTextFieldAdjustHeight(_Id, InputElement, MaxRows);
        }
    }

    private async Task SetupGhostText(bool isMultiline)
    {
        // The ghost text machinery attaches a handful of listeners to the input, so it is only wired up
        // once a suggestion actually shows up instead of on every text field of an app.
        if (GhostText.HasValue() && (_ghostSetup is false || _ghostSetupIsMultiline != isMultiline))
        {
            _jsSetup = true;
            _ghostSetup = true;
            _ghostSetupIsMultiline = isMultiline;
            _dotnetObj ??= DotNetObjectReference.Create(this);
            await _js.BitTextFieldSetupGhostText(_Id, InputElement, _dotnetObj);
            _oldGhostText = null;
        }

        // A suggestion that is being shown is pushed again on every render: the browser side drops it on
        // every keystroke, and a field that only commits its value on blur changes neither the GhostText nor
        // the Value while it is typed into, so nothing else would ever put it back.
        if (_ghostSetup && (GhostText.HasValue() || GhostText != _oldGhostText || _oldValue != Value))
        {
            _oldGhostText = GhostText;
            await _js.BitTextFieldSetGhostText(_Id, GhostText ?? string.Empty);
        }
    }

    private async Task SetupComposition(bool isMultiline)
    {
        // The guard only earns its listeners on a field that an input method editor can actually disturb:
        // one that reports every keystroke, or one that gives Enter or Escape a meaning of its own.
        if (NeedsCompositionGuard is false)
        {
            if (_compositionSetup is false) return;

            _compositionSetup = false;
            await _js.BitTextFieldDisposeFeature(_Id, "composition");
            return;
        }

        if (_compositionSetup && _compositionSetupIsMultiline == isMultiline) return;

        _jsSetup = true;
        _compositionSetup = true;
        _compositionSetupIsMultiline = isMultiline;

        await _js.BitTextFieldSetupComposition(_Id, InputElement);
    }

    private async Task SetupSelectOnFocus(bool isMultiline)
    {
        if (SelectOnFocus is false)
        {
            if (_selectOnFocusSetup is false) return;

            _selectOnFocusSetup = false;
            await _js.BitTextFieldDisposeFeature(_Id, "selectOnFocus");
            return;
        }

        if (_selectOnFocusSetup && _selectOnFocusSetupIsMultiline == isMultiline) return;

        _jsSetup = true;
        _selectOnFocusSetup = true;
        _selectOnFocusSetupIsMultiline = isMultiline;

        await _js.BitTextFieldSetupSelectOnFocus(_Id, InputElement);
    }

    protected override bool TryParseValueFromString(string? value, [MaybeNullWhen(false)] out string? result, [NotNullWhen(false)] out string? parsingErrorMessage)
    {
        result = _trimOnParse ? value?.Trim() : value;
        parsingErrorMessage = null;
        return true;
    }

    protected override async Task HandleOnStringValueInputAsync(ChangeEventArgs e)
    {
        if (IsEnabled is false || ReadOnly) return;

        UpdateCharCount(e.Value?.ToString());

        await base.HandleOnStringValueInputAsync(e);
    }

    // The counter and the clear button both follow what the input reports rather than the bound value, which
    // is what keeps them right on a field that only commits its value when it loses focus: a clear button
    // that shows up a whole blur after the first character would be of no use.
    private void UpdateCharCount(string? text)
    {
        if (ShowCount is false && ShowClearButton is false) return;

        var hasText = string.IsNullOrEmpty(text) is false;
        var count = CountOf(text);

        if (hasText == _hasText && count == _charCount) return;

        _hasText = hasText;
        _charCount = count;
        StateHasChanged();
    }

    private int CountOf(string? text) => CountStrategy is null ? (text?.Length ?? 0) : CountStrategy(text);



    private bool HasLabel => Label.HasValue() || LabelTemplate is not null;

    private bool HasDescription => Description.HasValue() || DescriptionTemplate is not null;

    // The textarea is only rendered for the plain text types: every other type has a behavior of its own
    // (a password mask, a number spinner, ...) that a textarea cannot provide.
    private bool IsMultilineElement => Multiline && Type is null or BitInputType.Text;

    // Whether the input currently holds anything is what decides this rather than the bound value, so the
    // button is there from the first keystroke even on a field that only commits on blur. It is deliberately
    // not read off the counter: a counting strategy of its own is free to report a zero for a value that is
    // not empty - one counting words, say - and a field holding text would otherwise lose its clear button.
    private bool ShowClear => ShowClearButton && _hasText;

    private bool NeedsCompositionGuard => Immediate || OnEnter.HasDelegate || OnEscape.HasDelegate;

    // A value above the limit can only arrive from the code rather than from the keyboard, which the
    // maxlength attribute holds back, so the counter says so instead of quietly reading a number above the
    // limit right next to it.
    private string CountClass => MaxLength >= 0 && _charCount > MaxLength
                                    ? "bit-tfl-cnt bit-tfl-cno"
                                    : "bit-tfl-cnt";

    private string? DescribedBy
    {
        get
        {
            // The counter is only pointed at when it carries the limit, so a screen reader learns how much
            // room is left instead of hearing a bare number on every focus.
            var hasCount = ShowCount && MaxLength >= 0;

            // The visible description and the screen-reader-only one are both legitimate, and a field can
            // carry either or both, so every part that is present is referenced in reading order. A
            // describedby of the consumer's own comes first and is never dropped: the explicit attribute of
            // the input wins over the splatted ones, so an app pointing the field at an error message of its
            // own would otherwise lose it the moment the component has anything to reference.
            var ids = string.Join(' ', new[]
            {
                GetInputAttribute("aria-describedby"),
                HasDescription ? _descriptionId : null,
                AriaDescription.HasValue() ? _ariaDescriptionId : null,
                hasCount ? _countId : null
            }.Where(id => id.HasValue()));

            return ids.HasValue() ? ids : null;
        }
    }

    // The forced invalid state and the one the EditContext produces end up on the same attribute, and the
    // base class writes the latter into the splatted attributes, so the value of the consumer is read back
    // here instead of being overwritten by the explicit attribute of the input.
    private string? AriaInvalid => Invalid ? "true" : GetInputAttribute("aria-invalid");

    private string? AriaBusy => Loading ? "true" : GetInputAttribute("aria-busy");

    // A live region only announces what changes inside it after it is already on the page: one that is added
    // along with its text is regularly missed altogether. The busy state and the inline suggestion both have
    // to be announced the moment they show up, so a single empty region is kept in the markup and only its
    // text comes and goes. The busy state wins over the suggestion, since it is the newer of the two states.
    private string? LiveText => Loading
                                    ? (LoadingAriaLabel ?? "Loading")
                                    : (GhostText.HasValue() ? GhostText : null);

    // aria-labelledby takes precedence over aria-label, so pointing at the visible label while a name of its
    // own was given would quietly throw that name away. The visible label keeps naming the input through the
    // for/id pair either way, which is what the label element is for.
    private string? LabelledBy => HasLabel && AriaLabel.HasNoValue() ? _labelId : null;

    private string? GetInputAttribute(string name)
    {
        return InputHtmlAttributes is not null && InputHtmlAttributes.TryGetValue(name, out var value)
                ? value?.ToString()
                : null;
    }

    private string CountText => MaxLength >= 0 ? $"{_charCount}/{MaxLength}" : _charCount.ToString();

    // The busy indicator is sized in pixels rather than by its own size enum, whose smallest step is
    // already taller than the whole field, so it follows the size of the text field instead.
    private int LoadingSize => Size switch
    {
        BitSize.Small => 16,
        BitSize.Large => 24,
        _ => 20
    };


    private void SetInputMode()
    {
        _inputMode = InputMode?.ToString().ToLower();
    }

    private void SetElementType()
    {
        // A revealed value must not survive the reveal being taken away, whether that happens by changing
        // the type of the input or by turning the reveal button off.
        if (Type is not BitInputType.Password || CanRevealPassword is false)
        {
            _isPasswordRevealed = false;
        }

        _elementType = Type is BitInputType.Password && CanRevealPassword && _isPasswordRevealed
                         ? BitInputType.Text
                         : Type;

        _inputType = _elementType switch
        {
            BitInputType.Text => "text",
            BitInputType.Number => "number",
            BitInputType.Password => "password",
            BitInputType.Email => "email",
            BitInputType.Tel => "tel",
            BitInputType.Url => "url",
            BitInputType.Search => "search",
            _ => "text",
        };
    }

    private void SetHasFocus(bool value)
    {
        // Both focus and focusin (and blur and focusout) reach this, so the builders are only reset when
        // the state actually flips instead of once per event.
        if (_hasFocus == value) return;

        _hasFocus = value;
        ClassBuilder.Reset();
        StyleBuilder.Reset();
    }

    private async Task HandleOnFocusIn(FocusEventArgs e)
    {
        if (IsEnabled is false) return;

        SetHasFocus(true);
        await OnFocusIn.InvokeAsync(e);
    }

    private async Task HandleOnFocusOut(FocusEventArgs e)
    {
        if (IsEnabled is false) return;

        SetHasFocus(false);
        await OnFocusOut.InvokeAsync(e);
    }

    private async Task HandleOnFocus(FocusEventArgs e)
    {
        if (IsEnabled is false) return;

        SetHasFocus(true);
        await OnFocus.InvokeAsync(e);
    }

    private async Task HandleOnBlur(FocusEventArgs e)
    {
        if (IsEnabled is false) return;

        SetHasFocus(false);
        await OnBlur.InvokeAsync(e);
    }

    private async Task HandleOnKeyDown(KeyboardEventArgs e)
    {
        if (IsEnabled is false) return;

        await OnKeyDown.InvokeAsync(e);

        if (e.Key == "Enter")
        {
            await OnEnter.InvokeAsync(e);
        }
        else if (e.Key == "Escape")
        {
            await OnEscape.InvokeAsync(e);
        }
    }

    private async Task HandleOnKeyUp(KeyboardEventArgs e)
    {
        if (IsEnabled is false) return;

        await OnKeyUp.InvokeAsync(e);
    }

    private async Task HandleOnClick(MouseEventArgs e)
    {
        if (IsEnabled is false) return;

        await OnClick.InvokeAsync(e);
    }

    private async Task HandleOnChange(ChangeEventArgs e)
    {
        // Trimming belongs to the change event only: doing it on every keystroke of an Immediate text field
        // would swallow the space the moment it is typed.
        _trimOnParse = Trim;

        try
        {
            await HandleOnStringValueChangeAsync(e);
        }
        finally
        {
            _trimOnParse = false;
        }

        // The change event is the one that commits the value, so the counter and the clear button are
        // re-synced from what the value ended up being rather than from the text the event carried: an
        // internal change does not run OnParametersSet, and a trimmed value is shorter than what was typed.
        UpdateCharCount(CurrentValue);
    }

    private async Task HandleOnClearButtonClick()
    {
        if (IsEnabled is false || ReadOnly) return;

        await ClearValue();

        await InputElement.FocusAsync();

        await OnClear.InvokeAsync();
    }

    private async Task ClearValue()
    {
        await SetCurrentValueAsStringAsync(string.Empty, true);

        // The counter is read from whatever the value ended up being rather than assumed to be zero: a
        // one-way bound field with no way to report a change keeps its value, and a counter saying zero
        // next to a full input would be a lie.
        _hasText = string.IsNullOrEmpty(CurrentValue) is false;
        _charCount = CountOf(CurrentValue);
    }

    private async Task HandleOnRevealPasswordClick()
    {
        // The click handler already runs on the renderer synchronization context, so it takes the direct
        // route instead of asking the dispatcher for one it is already on.
        RevealPassword();

        // Swapping the type of an input drops the caret, so the focus is handed back to it explicitly.
        await InputElement.FocusAsync();
    }

    protected override async ValueTask DisposeAsync(bool disposing)
    {
        if (IsDisposed || disposing is false) return;

        await base.DisposeAsync(disposing);

        _dotnetObj?.Dispose();

        if (_jsSetup is false) return;

        try
        {
            await _js.BitTextFieldDispose(_Id);
        }
        catch (JSDisconnectedException) { } // we can ignore this exception here
    }
}
