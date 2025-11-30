using System.Diagnostics.CodeAnalysis;

namespace Bit.BlazorUI;

/// <summary>
/// Text fields give people a way to enter and edit text. They’re used in forms, modal dialogs, tables, and other surfaces where text input is required.
/// </summary>
public partial class BitTextField : BitTextInputBase<string?>
{
    private bool _hasFocus;
    private string? _oldValue;
    private string? _inputMode;
    private bool _isPasswordRevealed;
    private BitInputType? _elementType;
    private string _inputId = string.Empty;
    private string _labelId = string.Empty;
    private string _inputType = string.Empty;
    private string _descriptionId = string.Empty;



    [Inject] private IJSRuntime _js { get; set; } = default!;



    /// <summary>
    /// The general color of the text field used when focused.
    /// </summary>
    [Parameter] public BitColor? Accent { get; set; }

    /// <summary>
    /// Automatically adjust the height of the input in Multiline mode.
    /// </summary>
    [Parameter, ResetClassBuilder]
    public bool AutoHeight { get; set; }

    /// <summary>
    /// The color kind of the text field background.
    /// </summary>
    [Parameter] public BitColorKind? Background { get; set; }

    /// <summary>
    /// The color kind of the text field border.
    /// </summary>
    [Parameter, ResetClassBuilder]
    public BitColorKind? Border { get; set; }

    /// <summary>
    /// Whether to show the reveal password button for input type 'password'.
    /// </summary>
    [Parameter] public bool CanRevealPassword { get; set; }

    /// <summary>
    /// Custom CSS classes for different parts of the BitTextField.
    /// </summary>
    [Parameter] public BitTextFieldClassStyles? Classes { get; set; }

    /// <summary>
    /// Default value of the text field. Only provide this if the text field is an uncontrolled component; otherwise, use the value property.
    /// </summary>
    [Parameter] public string? DefaultValue { get; set; }

    /// <summary>
    /// Description displayed below the text field to provide additional details about what text to enter.
    /// </summary>
    [Parameter] public string? Description { get; set; }

    /// <summary>
    /// Shows the custom description for text field.
    /// </summary>
    [Parameter] public RenderFragment? DescriptionTemplate { get; set; }

    /// <summary>
    /// Forces the text field fill 100% of its container width.
    /// </summary>
    [Parameter] public bool FullWidth { get; set; }

    /// <summary>
    /// The icon name for the icon shown in the far right end of the text field.
    /// </summary>
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
    [Parameter] public RenderFragment? LabelTemplate { get; set; }

    /// <summary>
    /// Specifies the maximum number of characters allowed in the input.
    /// </summary>
    [Parameter] public int MaxLength { get; set; } = -1;

    /// <summary>
    /// Whether or not the text field is a Multiline text field.
    /// </summary>
    [Parameter, ResetClassBuilder]
    public bool Multiline { get; set; }

    /// <summary>
    /// Removes the border of the text input.
    /// </summary>
    [Parameter, ResetClassBuilder]
    public bool NoBorder { get; set; }

    /// <summary>
    /// Callback for when the input clicked.
    /// </summary>
    [Parameter] public EventCallback<MouseEventArgs> OnClick { get; set; }

    /// <summary>
    /// Callback for when the Enter key is pressed while input has focus.
    /// </summary>
    [Parameter] public EventCallback<KeyboardEventArgs> OnEnter { get; set; }

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
    /// Callback for when a keyboard key is pressed
    /// </summary>
    [Parameter] public EventCallback<KeyboardEventArgs> OnKeyDown { get; set; }

    /// <summary>
    /// Callback for When a keyboard key is released
    /// </summary>
    [Parameter] public EventCallback<KeyboardEventArgs> OnKeyUp { get; set; }

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
    /// Aria label for the reveal password button.
    /// </summary>
    [Parameter] public string? RevealPasswordAriaLabel { get; set; }

    /// <summary>
    /// For multiline text, Number of rows.
    /// </summary>
    [Parameter] public int? Rows { get; set; }

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



    public void ToggleRevealPassword()
    {
        _isPasswordRevealed = _isPasswordRevealed is false;
        SetElementType();
    }



    protected override string RootElementClass => "bit-tfl";

    protected override void RegisterCssClasses()
    {
        ClassBuilder.Register(() => Classes?.Root);

        ClassBuilder.Register(() => Multiline && Type is null or BitInputType.Text
                                    ? $"bit-tfl-{(Resizable ? "mln" : "mlf")}"
                                    : string.Empty);

        ClassBuilder.Register(() => Multiline && AutoHeight ? "bit-tfl-mla" : string.Empty);

        ClassBuilder.Register(() => IsEnabled && Required ? "bit-tfl-req" : string.Empty);

        ClassBuilder.Register(() => Underlined ? "bit-tfl-und" : string.Empty);

        ClassBuilder.Register(() => NoBorder ? "bit-tfl-nbd" : string.Empty);

        ClassBuilder.Register(() => _hasFocus ? $"bit-tfl-fcs {Classes?.Focused}" : string.Empty);

        ClassBuilder.Register(() => Required && Label is null ? "bit-tfl-rnl" : string.Empty);

        ClassBuilder.Register(() => FullWidth ? "bit-tfl-fwd" : string.Empty);

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
        _descriptionId = $"BitTextField-{UniqueId}-description";

        if (ValueHasBeenSet is false && DefaultValue is not null)
        {
            await SetCurrentValueAsStringAsync(DefaultValue, true);
        }

        await base.OnInitializedAsync();
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        await base.OnAfterRenderAsync(firstRender);

        if (firstRender)
        {
            if (Multiline)
            {
                await _js.BitTextFieldSetupMultilineInput(_Id, InputElement, AutoHeight, PreventEnter);
            }
        }
        else
        {
            if (Multiline && AutoHeight && _oldValue != Value)
            {
                _oldValue = Value;
                await _js.BitTextFieldAdjustHeight(InputElement);
            }
        }
    }

    protected override bool TryParseValueFromString(string? value, [MaybeNullWhen(false)] out string? result, [NotNullWhen(false)] out string? parsingErrorMessage)
    {
        result = Trim ? value?.Trim() : value;
        parsingErrorMessage = null;
        return true;
    }


    private void SetInputMode()
    {
        _inputMode = InputMode?.ToString().ToLower();
    }

    private void SetElementType()
    {
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
            _ => "text",
        };
    }

    private async Task HandleOnFocusIn(FocusEventArgs e)
    {
        if (IsEnabled is false) return;

        _hasFocus = true;
        ClassBuilder.Reset();
        StyleBuilder.Reset();
        await OnFocusIn.InvokeAsync(e);
    }

    private async Task HandleOnFocusOut(FocusEventArgs e)
    {
        if (IsEnabled is false) return;

        _hasFocus = false;
        ClassBuilder.Reset();
        StyleBuilder.Reset();
        await OnFocusOut.InvokeAsync(e);
    }

    private async Task HandleOnFocus(FocusEventArgs e)
    {
        if (IsEnabled is false) return;

        _hasFocus = true;
        ClassBuilder.Reset();
        StyleBuilder.Reset();
        await OnFocus.InvokeAsync(e);
    }

    private async Task HandleOnKeyDown(KeyboardEventArgs e)
    {
        if (IsEnabled is false) return;

        _ = OnKeyDown.InvokeAsync(e);

        if (e.Key == "Enter")
        {
            _ = OnEnter.InvokeAsync(e);
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


    protected override async ValueTask DisposeAsync(bool disposing)
    {
        if (IsDisposed || disposing is false) return;

        await base.DisposeAsync(disposing);

        try
        {
            await _js.BitTextFieldDispose(_Id);
        }
        catch (JSDisconnectedException) { } // we can ignore this exception here
    }
}
