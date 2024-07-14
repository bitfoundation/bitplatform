using System.Diagnostics.CodeAnalysis;

namespace Bit.BlazorUI;

public partial class BitTextField : BitTextInputBase<string?>
{
    private bool isMultiline;
    private bool isUnderlined;
    private bool hasBorder = true;
    private bool isResizable = true;
    private BitTextFieldType type = BitTextFieldType.Text;

    private bool _hasFocus;
    private bool _isPasswordRevealed;
    private BitTextFieldType _elementType;
    private string _inputId = string.Empty;
    private string _labelId = string.Empty;
    private string _inputType = string.Empty;
    private string _descriptionId = string.Empty;



    /// <summary>
    /// AutoComplete is a string that maps to the autocomplete attribute of the HTML input element.
    /// </summary>
    [Parameter] public string? AutoComplete { get; set; }

    /// <summary>
    /// Determines if the text field is auto focused on first render.
    /// </summary>
    [Parameter] public bool AutoFocus { get; set; }

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
    /// Whether or not the text field is borderless.
    /// </summary>
    [Parameter, ResetClassBuilder]
    public bool HasBorder { get; set; }

    /// <summary>
    /// Whether or not the text field is a Multiline text field.
    /// </summary>
    [Parameter, ResetClassBuilder]
    public bool IsMultiline { get; set; }

    /// <summary>
    /// Whether or not the text field is underlined.
    /// </summary>
    [Parameter, ResetClassBuilder]
    public bool IsUnderlined { get; set; }

    /// <summary>
    /// For multiline text fields, whether or not the field is resizable.
    /// </summary>
    [Parameter, ResetClassBuilder]
    public bool IsResizable { get; set; }

    /// <summary>
    /// The icon name for the icon shown in the far right end of the text field.
    /// </summary>
    [Parameter] public string? IconName { get; set; }

    /// <summary>
    /// Specifies whether to remove any leading or trailing whitespace from the value.
    /// </summary>
    [Parameter] public bool IsTrimmed { get; set; }

    /// <summary>
    /// Label displayed above the text field and read by screen readers.
    /// </summary>
    [Parameter] public string? Label { get; set; }

    /// <summary>
    /// Shows the custom label for text field.
    /// </summary>
    [Parameter] public RenderFragment? LabelTemplate { get; set; }

    /// <summary>
    /// Specifies the maximum number of characters allowed in the input.
    /// </summary>
    [Parameter] public int MaxLength { get; set; } = -1;

    /// <summary>
    /// Callback for when focus moves into the input
    /// </summary>
    [Parameter] public EventCallback<FocusEventArgs> OnFocusIn { get; set; }

    /// <summary>
    /// Callback for when focus moves out of the input
    /// </summary>
    [Parameter] public EventCallback<FocusEventArgs> OnFocusOut { get; set; }

    /// <summary>
    /// Callback for when focus moves into the input
    /// </summary>
    [Parameter] public EventCallback<FocusEventArgs> OnFocus { get; set; }

    /// <summary>
    /// Callback for when a keyboard key is pressed
    /// </summary>
    [Parameter] public EventCallback<KeyboardEventArgs> OnKeyDown { get; set; }

    /// <summary>
    /// Callback for When a keyboard key is released
    /// </summary>
    [Parameter] public EventCallback<KeyboardEventArgs> OnKeyUp { get; set; }

    /// <summary>
    /// Callback for when the input clicked.
    /// </summary>
    [Parameter] public EventCallback<MouseEventArgs> OnClick { get; set; }

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
    /// For multiline text, Number of rows.
    /// </summary>
    [Parameter] public int Rows { get; set; } = 3;

    /// <summary>
    /// Aria label for the reveal password button.
    /// </summary>
    [Parameter] public string? RevealPasswordAriaLabel { get; set; }

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
    /// Input type.
    /// </summary>
    [Parameter]
    public BitTextFieldType Type
    {
        get => type;
        set
        {
            if (type == value) return;

            type = value;
            SetElementType();
            ClassBuilder.Reset();
        }
    }



    public void ToggleRevealPassword()
    {
        _isPasswordRevealed = !_isPasswordRevealed;
        SetElementType();
    }



    protected override string RootElementClass => "bit-txt";

    protected override void RegisterCssClasses()
    {
        ClassBuilder.Register(() => Classes?.Root);

        ClassBuilder.Register(() => IsMultiline && Type == BitTextFieldType.Text
                                    ? $"bit-txt-{(IsResizable ? "mln" : "mlf")}"
                                    : string.Empty);

        ClassBuilder.Register(() => IsEnabled && Required ? "bit-txt-req" : string.Empty);

        ClassBuilder.Register(() => IsUnderlined ? "bit-txt-und" : string.Empty);

        ClassBuilder.Register(() => HasBorder is false ? "bit-txt-nbd" : string.Empty);

        ClassBuilder.Register(() => _hasFocus ? $"bit-txt-fcs {Classes?.Focused}" : string.Empty);

        ClassBuilder.Register(() => Required && Label is null ? "bit-txt-rnl" : string.Empty);
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

        if (firstRender is false || IsEnabled is false) return;

        if (AutoFocus)
        {
            await InputElement.FocusAsync();
        }
    }

    protected override bool TryParseValueFromString(string? value, [MaybeNullWhen(false)] out string? result, [NotNullWhen(false)] out string? parsingErrorMessage)
    {
        result = IsTrimmed ? value?.Trim() : value;
        parsingErrorMessage = null;
        return true;
    }



    private void SetElementType()
    {
        _elementType = type is BitTextFieldType.Password && CanRevealPassword && _isPasswordRevealed
                         ? BitTextFieldType.Text
                         : type;

        _inputType = _elementType switch
        {
            BitTextFieldType.Text => "text",
            BitTextFieldType.Password => "password",
            BitTextFieldType.Number => "number",
            BitTextFieldType.Email => "email",
            BitTextFieldType.Tel => "tel",
            BitTextFieldType.Url => "url",
            _ => string.Empty,
        };
    }

    private async Task HandleOnFocusIn(FocusEventArgs e)
    {
        if (IsEnabled is false) return;

        _hasFocus = true;
        ClassBuilder.Reset();
        await OnFocusIn.InvokeAsync(e);
    }

    private async Task HandleOnFocusOut(FocusEventArgs e)
    {
        if (IsEnabled is false) return;

        _hasFocus = false;
        ClassBuilder.Reset();
        await OnFocusOut.InvokeAsync(e);
    }

    private async Task HandleOnFocus(FocusEventArgs e)
    {
        if (IsEnabled is false) return;

        _hasFocus = true;
        ClassBuilder.Reset();
        await OnFocus.InvokeAsync(e);
    }

    private async Task HandleOnKeyDown(KeyboardEventArgs e)
    {
        if (IsEnabled is false) return;

        await OnKeyDown.InvokeAsync(e);
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
}
