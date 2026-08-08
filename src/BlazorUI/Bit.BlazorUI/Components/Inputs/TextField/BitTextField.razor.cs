using System.Diagnostics.CodeAnalysis;

namespace Bit.BlazorUI;

/// <summary>
/// Text fields give people a way to enter and edit text. They’re used in forms, modal dialogs, tables, and other surfaces where text input is required.
/// </summary>
public partial class BitTextField : BitTextInputBase<string?>
{
    private int _charCount;
    private bool _hasFocus;
    private bool _jsSetup;
    private bool _ghostSetup;
    private int? _oldMaxRows;
    private string? _oldValue;
    private string? _oldGhostText;
    private string? _oldValueForCount;
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



    [Inject] private IJSRuntime _js { get; set; } = default!;



    /// <summary>
    /// The general color of the text field used when focused.
    /// </summary>
    [Parameter, ResetClassBuilder]
    public BitColor? Accent { get; set; }

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
    /// Label displayed above the text field and read by screen readers.
    /// </summary>
    [Parameter, ResetClassBuilder]
    public string? Label { get; set; }

    /// <summary>
    /// Shows the custom label for text field.
    /// </summary>
    [Parameter, ResetClassBuilder]
    public RenderFragment? LabelTemplate { get; set; }

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
    /// Callback executed when the user clears the text field by clicking the clear button.
    /// </summary>
    [Parameter] public EventCallback OnClear { get; set; }

    /// <summary>
    /// Callback for when the input clicked.
    /// </summary>
    [Parameter] public EventCallback<MouseEventArgs> OnClick { get; set; }

    /// <summary>
    /// Callback for when the Enter key is pressed while input has focus.
    /// </summary>
    [Parameter] public EventCallback<KeyboardEventArgs> OnEnter { get; set; }

    /// <summary>
    /// Callback for when the Escape key is pressed while input has focus.
    /// </summary>
    [Parameter] public EventCallback<KeyboardEventArgs> OnEscape { get; set; }

    /// <summary>
    /// Callback for when focus moves into the input
    /// </summary>
    [Parameter] public EventCallback<FocusEventArgs> OnFocus { get; set; }

    /// <summary>
    /// Callback for when focus moves into the input
    /// </summary>
    [Parameter] public EventCallback<FocusEventArgs> OnFocusIn { get; set; }

    /// <summary>
    /// Callback for when focus moves out of the input
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
    /// Whether to show the clear button when the text field has a value.
    /// </summary>
    [Parameter] public bool ShowClearButton { get; set; }

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
    /// Called by JavaScript when the ghost text is accepted.
    /// </summary>
    [JSInvokable("OnGhostTextAccepted")]
    public async Task _NotifyGhostTextAccepted(string? acceptedText)
    {
        if (IsEnabled is false || ReadOnly) return;

        await OnGhostTextAccepted.InvokeAsync(acceptedText);
    }



    /// <summary>
    /// Toggles the revealed state of the value while the type of the input is password and
    /// <see cref="CanRevealPassword"/> is enabled.
    /// </summary>
    public void ToggleRevealPassword()
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

        ClassBuilder.Register(() => Underlined ? "bit-tfl-und" : string.Empty);

        ClassBuilder.Register(() => NoBorder ? "bit-tfl-nbd" : string.Empty);

        ClassBuilder.Register(() => ReadOnly ? "bit-tfl-rdl" : string.Empty);

        ClassBuilder.Register(() => _hasFocus ? "bit-tfl-fcs" : string.Empty);

        ClassBuilder.Register(() => _hasFocus ? Classes?.Focused : string.Empty);

        ClassBuilder.Register(() => Required && HasLabel is false ? "bit-tfl-rnl" : string.Empty);

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

        // The CallOnSet handler of the Type parameter only runs when the parameter is actually assigned,
        // so the element type has to be resolved here as well for the default (unset) case.
        SetElementType();

        SetDefaultValue();

        await base.OnInitializedAsync();
    }

    protected override void OnParametersSet()
    {
        // The counter follows what the input reports while the user types, which is what keeps it live even
        // when Immediate is off. It is only re-synced from the Value when the Value itself changes.
        if (_oldValueForCount != Value)
        {
            _oldValueForCount = Value;
            _charCount = Value?.Length ?? 0;
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

            if (IsMultilineElement)
            {
                _jsSetup = true;
                await _js.BitTextFieldSetupMultilineInput(_Id, InputElement, AutoHeight, PreventEnter, MaxRows);

                if (AutoHeight)
                {
                    // An input that is rendered with a value already in it has to be measured once,
                    // otherwise it stays one row tall until the first keystroke.
                    await _js.BitTextFieldAdjustHeight(_Id, InputElement, MaxRows);
                }
            }
        }

        // The ghost text machinery attaches a handful of listeners to the input, so it is only wired up
        // once a suggestion actually shows up instead of on every text field of an app.
        if (_ghostSetup is false && GhostText.HasValue())
        {
            _jsSetup = true;
            _ghostSetup = true;
            _dotnetObj ??= DotNetObjectReference.Create(this);
            await _js.BitTextFieldSetupGhostText(_Id, InputElement, _dotnetObj);
            _oldGhostText = null;
        }

        if (_ghostSetup && (GhostText != _oldGhostText || _oldValue != Value))
        {
            _oldGhostText = GhostText;
            await _js.BitTextFieldSetGhostText(_Id, GhostText ?? string.Empty);
        }

        if (firstRender is false && IsMultilineElement && AutoHeight && (_oldValue != Value || _oldMaxRows != MaxRows))
        {
            _oldMaxRows = MaxRows;
            await _js.BitTextFieldAdjustHeight(_Id, InputElement, MaxRows);
        }

        _oldValue = Value;
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

        if (ShowCount)
        {
            var length = e.Value?.ToString()?.Length ?? 0;
            if (length != _charCount)
            {
                _charCount = length;
                StateHasChanged();
            }
        }

        await base.HandleOnStringValueInputAsync(e);
    }



    private bool HasLabel => Label.HasValue() || LabelTemplate is not null;

    private bool HasDescription => Description.HasValue() || DescriptionTemplate is not null;

    // The textarea is only rendered for the plain text types: every other type has a behavior of its own
    // (a password mask, a number spinner, ...) that a textarea cannot provide.
    private bool IsMultilineElement => Multiline && Type is null or BitInputType.Text;

    private bool ShowClear => ShowClearButton && CurrentValue.HasValue();

    private string? DescribedBy
    {
        get
        {
            var hasDescription = HasDescription;
            // The counter is only pointed at when it carries the limit, so a screen reader learns how much
            // room is left instead of hearing a bare number on every focus.
            var hasCount = ShowCount && MaxLength >= 0;

            if (hasDescription && hasCount) return $"{_descriptionId} {_countId}";
            if (hasDescription) return _descriptionId;
            if (hasCount) return _countId;
            return null;
        }
    }

    private string CountText => MaxLength >= 0 ? $"{_charCount}/{MaxLength}" : _charCount.ToString();


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
    }

    private async Task HandleOnClearButtonClick()
    {
        if (IsEnabled is false || ReadOnly) return;

        await SetCurrentValueAsStringAsync(string.Empty, true);

        _charCount = 0;

        await InputElement.FocusAsync();

        await OnClear.InvokeAsync();
    }

    private async Task HandleOnRevealPasswordClick()
    {
        ToggleRevealPassword();

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
