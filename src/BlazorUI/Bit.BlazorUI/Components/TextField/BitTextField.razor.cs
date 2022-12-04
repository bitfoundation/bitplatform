using System.Diagnostics.CodeAnalysis;

namespace Bit.BlazorUI;

public partial class BitTextField
{
    private bool hasBorder = true;
    private bool isMultiline;
    private bool isReadonly;
    private bool isRequired;
    private bool isUnderlined;
    private bool isResizable = true;
    private BitTextFieldType type = BitTextFieldType.Text;

    private string _textFieldId = string.Empty;
    private string _inputType = string.Empty;
    private string _labelId = string.Empty;
    private string _descriptionId = string.Empty;
    private bool _isPasswordRevealed;
    private BitTextFieldType _textFieldType;

    private string focusClass = string.Empty;
    public string FocusClass
    {
        get => focusClass;
        set
        {
            focusClass = value;
            ClassBuilder.Reset();
        }
    }

    /// <summary>
    /// AutoComplete is a string that maps to the autocomplete attribute of the HTML input element.
    /// </summary>
    [Parameter] public string? AutoComplete { get; set; }

    /// <summary>
    /// Whether to show the reveal password button for input type 'password'.
    /// </summary>
    [Parameter] public bool CanRevealPassword { get; set; }

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
    [Parameter]
    public bool HasBorder
    {
        get => hasBorder;
        set
        {
            hasBorder = value;
            ClassBuilder.Reset();
        }
    }

    /// <summary>
    /// Whether or not the text field is a Multiline text field.
    /// </summary>
    [Parameter]
    public bool IsMultiline
    {
        get => isMultiline;
        set
        {
            isMultiline = value;
            ClassBuilder.Reset();
        }
    }

    /// <summary>
    /// If true, the text field is readonly.
    /// </summary>
    [Parameter]
    public bool IsReadonly
    {
        get => isReadonly;
        set
        {
            isReadonly = value;
            ClassBuilder.Reset();
        }
    }

    /// <summary>
    /// Whether the associated input is required or not, add an asterisk "*" to its label.
    /// </summary>
    [Parameter]
    public bool IsRequired
    {
        get => isRequired;
        set
        {
            isRequired = value;
            ClassBuilder.Reset();
        }
    }

    /// <summary>
    /// Whether or not the text field is underlined.
    /// </summary>
    [Parameter]
    public bool IsUnderlined
    {
        get => isUnderlined;
        set
        {
            isUnderlined = value;
            ClassBuilder.Reset();
        }
    }

    /// <summary>
    /// For multiline text fields, whether or not the field is resizable.
    /// </summary>
    [Parameter]
    public bool IsResizable
    {
        get => isResizable;
        set
        {
            isResizable = value;
            ClassBuilder.Reset();
        }
    }

    /// <summary>
    /// The icon name for the icon shown in the far right end of the text field.
    /// </summary>
    [Parameter] public BitIconName? IconName { get; set; }

    /// <summary>
    /// Specifies whether to remove any leading or trailing whitespace from the value.
    /// </summary>
    [Parameter] public bool IsTrimed { get; set; }

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
    /// Callback for when the input value changes. This is called on both input and change events. 
    /// </summary>
    [Parameter] public EventCallback<string?> OnChange { get; set; }

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
            type = value;
            SetTextFieldType();
            ClassBuilder.Reset();
        }
    }

    protected override string RootElementClass => "bit-txt";

    protected override Task OnInitializedAsync()
    {
        if (CurrentValueAsString.HasNoValue() && DefaultValue.HasValue())
        {
            CurrentValueAsString = DefaultValue;
        }

        _textFieldId = $"TextField_{UniqueId}";
        _labelId = $"TextFieldLabel_{UniqueId}";
        _descriptionId = $"TextFieldDescription_{UniqueId}";

        return base.OnInitializedAsync();
    }

    protected override void RegisterComponentClasses()
    {
        ClassBuilder.Register(() => IsMultiline && Type == BitTextFieldType.Text
                                    ? $"{RootElementClass}-multiline-{(IsResizable is false ? "fix-" : string.Empty)}{VisualClassRegistrar()}" 
                                    : string.Empty);

        ClassBuilder.Register(() => IsEnabled && IsReadonly
                                    ? $"{RootElementClass}-readonly-{VisualClassRegistrar()}" 
                                    : string.Empty);

        ClassBuilder.Register(() => IsEnabled && IsRequired
                                    ? $"{RootElementClass}-required-{VisualClassRegistrar()}" 
                                    : string.Empty);

        ClassBuilder.Register(() => IsUnderlined
                                   ? $"{RootElementClass}-underlined-{(IsEnabled is false ? "disabled-" : string.Empty)}{VisualClassRegistrar()}" 
                                   : string.Empty);

        ClassBuilder.Register(() => HasBorder is false
                                   ? $"{RootElementClass}-no-border-{VisualClassRegistrar()}" : string.Empty);

        ClassBuilder.Register(() => FocusClass.HasValue()
                                    ? $"{RootElementClass}-{(IsUnderlined ? "underlined-" : "")}{FocusClass}-{VisualClassRegistrar()}" 
                                    : string.Empty);

        ClassBuilder.Register(() => ValueInvalid is true
                                   ? $"{RootElementClass}-invalid-{VisualClassRegistrar()}" 
                                   : string.Empty);

        ClassBuilder.Register(() => IsRequired && Label is null
                                   ? $"{RootElementClass}-required-no-label-{VisualClassRegistrar()}" 
                                   : string.Empty);
    }

    private void SetTextFieldType()
    {
        _textFieldType = type is BitTextFieldType.Password && CanRevealPassword && _isPasswordRevealed
                         ? BitTextFieldType.Text
                         : type;

        _inputType = _textFieldType switch
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

    private async Task HandleFocusIn(FocusEventArgs e)
    {
        if (IsEnabled)
        {
            FocusClass = "focused";
            await OnFocusIn.InvokeAsync(e);
        }
    }

    private async Task HandleFocusOut(FocusEventArgs e)
    {
        if (IsEnabled)
        {
            FocusClass = "";
            await OnFocusOut.InvokeAsync(e);
        }
    }

    private async Task HandleFocus(FocusEventArgs e)
    {
        if (IsEnabled)
        {
            FocusClass = "focused";
            await OnFocus.InvokeAsync(e);
        }
    }

    private async Task HandleChange(ChangeEventArgs e)
    {
        if (IsEnabled is false) return;
        if (ValueHasBeenSet && ValueChanged.HasDelegate is false) return;

        CurrentValueAsString = e.Value?.ToString();
        await OnChange.InvokeAsync(Value);
    }

    private async Task HandleKeyDown(KeyboardEventArgs e)
    {
        if (IsEnabled)
        {
            await OnKeyDown.InvokeAsync(e);
        }
    }

    private async Task HandleKeyUp(KeyboardEventArgs e)
    {
        if (IsEnabled)
        {
            await OnKeyUp.InvokeAsync(e);
        }
    }

    private async Task HandleClick(MouseEventArgs e)
    {
        if (IsEnabled)
        {
            await OnClick.InvokeAsync(e);
        }
    }

    public void ToggleRevealPassword()
    {
        _isPasswordRevealed = !_isPasswordRevealed;
        SetTextFieldType();
    }

    /// <inheritdoc />
    protected override bool TryParseValueFromString(string? value, out string? result, [NotNullWhen(false)] out string? validationErrorMessage)
    {
        result = IsTrimed ? value?.Trim() : value;
        validationErrorMessage = null;
        return true;
    }
}
