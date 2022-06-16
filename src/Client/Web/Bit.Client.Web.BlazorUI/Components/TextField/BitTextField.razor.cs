using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;

namespace Bit.Client.Web.BlazorUI;

public partial class BitTextField
{
    private bool isMultiline;
    private bool isReadonly;
    private bool isRequired;
    private bool isUnderlined;
    private bool hasBorder = true;
    private string focusClass = string.Empty;
    private BitTextFieldType type = BitTextFieldType.Text;
    private bool isResizable = true;
    private bool _isPasswordRevealed;

    /// <summary>
    /// Whether or not the text field is a Multiline text field
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
    /// If true, the text field is readonly
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
    /// Whether the associated input is required or not, add an asterisk "*" to its label
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
    /// Whether or not the text field is underlined
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
    /// Whether or not the text field is borderless
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
    /// Callback for when the input value changes. This is called on both input and change events. 
    /// </summary>
    [Parameter] public EventCallback<string?> OnChange { get; set; }

    /// <summary>
    /// Default value of the text field. Only provide this if the text field is an uncontrolled component; otherwise, use the value property
    /// </summary>
    [Parameter] public string? DefaultValue { get; set; }

    /// <summary>
    /// Input placeholder text
    /// </summary>
    [Parameter] public string? Placeholder { get; set; }

    /// <summary>
    /// Label displayed above the text field and read by screen readers
    /// </summary>
    [Parameter] public string? Label { get; set; }

    /// <summary>
    /// Shows the custom label for text field
    /// </summary>
    [Parameter] public RenderFragment? LabelFragment { get; set; }

    /// <summary>
    /// Description displayed below the text field to provide additional details about what text to enter.
    /// </summary>
    [Parameter] public string? Description { get; set; }

    /// <summary>
    /// Shows the custom description for text field
    /// </summary>
    [Parameter] public RenderFragment? DescriptionFragment { get; set; }

    /// <summary>
    /// Specifies the maximum number of characters allowed in the input
    /// </summary>
    [Parameter] public int MaxLength { get; set; } = -1;

    /// <summary>
    /// For multiline text, Number of rows
    /// </summary>
    [Parameter] public int Rows { get; set; } = 3;

    /// <summary>
    /// The icon name for the icon shown in the far right end of the text field
    /// </summary>
    [Parameter] public BitIconName? IconName { get; set; }

    /// <summary>
    /// Prefix displayed before the text field contents. This is not included in the value.
    /// Ensure a descriptive label is present to assist screen readers, as the value does not include the prefix.
    /// </summary>
    [Parameter] public string? Prefix { get; set; }

    /// <summary>
    /// Shows the custom prefix for text field
    /// </summary>
    [Parameter] public RenderFragment? PrefixFragment { get; set; }

    /// <summary>
    /// Suffix displayed after the text field contents. This is not included in the value. 
    /// Ensure a descriptive label is present to assist screen readers, as the value does not include the suffix.
    /// </summary>
    [Parameter] public string? Suffix { get; set; }

    /// <summary>
    /// Shows the custom suffix for text field
    /// </summary>
    [Parameter] public RenderFragment? SuffixFragment { get; set; }

    /// <summary>
    /// Whether to show the reveal password button for input type 'password'
    /// </summary>
    [Parameter] public bool CanRevealPassword { get; set; }

    /// <summary>
    /// Aria label for the reveal password button
    /// </summary>
    [Parameter] public string? RevealPasswordAriaLabel { get; set; }

    /// <summary>
    /// AutoComplete is a string that maps to the autocomplete attribute of the HTML input element
    /// </summary>
    [Parameter] public string? AutoComplete { get; set; }

    /// <summary>
    /// Input type
    /// </summary>
    [Parameter]
    public BitTextFieldType Type
    {
        get => type;
        set
        {
            type = value;
            SetElementType();
            ClassBuilder.Reset();
        }
    }

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
    /// Callback for when the input clicked
    /// </summary>
    [Parameter] public EventCallback<MouseEventArgs> OnClick { get; set; }

    /// <summary>
    /// Specifies whether to remove any leading or trailing whitespace from the value.
    /// </summary>
    [Parameter] public bool Trim { get; set; }

    public BitTextFieldType ElementType { get; set; }

    public string FocusClass
    {
        get => focusClass;
        set
        {
            focusClass = value;
            ClassBuilder.Reset();
        }
    }

    public string TextFieldId { get; set; } = string.Empty;
    public string LabelId { get; set; } = string.Empty;
    public string DescriptionId { get; set; } = string.Empty;

    protected override string RootElementClass => "bit-txt";

    protected override void RegisterComponentClasses()
    {
        ClassBuilder.Register(() => IsMultiline && Type == BitTextFieldType.Text
                                    ? $"{RootElementClass}-multiline-{(IsResizable is false ? "fix-" : string.Empty)}{VisualClassRegistrar()}" : string.Empty);

        ClassBuilder.Register(() => IsEnabled && IsReadonly
                                    ? $"{RootElementClass}-readonly-{VisualClassRegistrar()}" : string.Empty);

        ClassBuilder.Register(() => IsEnabled && IsRequired
                                    ? $"{RootElementClass}-required-{VisualClassRegistrar()}" : string.Empty);

        ClassBuilder.Register(() => IsUnderlined
                                   ? $"{RootElementClass}-underlined-{(IsEnabled is false ? "disabled-" : string.Empty)}{VisualClassRegistrar()}" : string.Empty);

        ClassBuilder.Register(() => HasBorder is false
                                   ? $"{RootElementClass}-no-border-{VisualClassRegistrar()}" : string.Empty);

        ClassBuilder.Register(() => FocusClass.HasValue()
                                    ? $"{RootElementClass}-{(IsUnderlined ? "underlined-" : "")}{FocusClass}-{VisualClassRegistrar()}" : string.Empty);

        ClassBuilder.Register(() => ValueInvalid is true
                                   ? $"{RootElementClass}-invalid-{VisualClassRegistrar()}" : string.Empty);

        ClassBuilder.Register(() => IsRequired && Label is null
                                   ? $"{RootElementClass}-required-no-label-{VisualClassRegistrar()}" : string.Empty);
    }

    protected override Task OnInitializedAsync()
    {
        if (DefaultValue.HasValue())
        {
            CurrentValueAsString = DefaultValue;
        }

        TextFieldId = $"TextField{UniqueId}";
        LabelId = $"TextFieldLabel{UniqueId}";
        DescriptionId = $"TextFieldDescription{UniqueId}";

        return base.OnInitializedAsync();
    }

    private void SetElementType()
    {
        ElementType = CanRevealPassword && type == BitTextFieldType.Password && _isPasswordRevealed
            ? BitTextFieldType.Text
            : type;
    }

    private async Task HandleFocusIn(FocusEventArgs e)
    {
        if (IsEnabled)
        {
            FocusClass = "focused";
            await OnFocusIn.InvokeAsync(e).ConfigureAwait(false);
        }
    }

    private async Task HandleFocusOut(FocusEventArgs e)
    {
        if (IsEnabled)
        {
            FocusClass = "";
            await OnFocusOut.InvokeAsync(e).ConfigureAwait(false);
        }
    }

    private async Task HandleFocus(FocusEventArgs e)
    {
        if (IsEnabled)
        {
            FocusClass = "focused";
            await OnFocus.InvokeAsync(e).ConfigureAwait(false);
        }
    }

    private async Task HandleChange(ChangeEventArgs e)
    {
        if (IsEnabled is false) return;
        if (ValueHasBeenSet && ValueChanged.HasDelegate is false) return;

        CurrentValueAsString = e.Value?.ToString();
        await OnChange.InvokeAsync(Value).ConfigureAwait(false);
    }

    private async Task HandleKeyDown(KeyboardEventArgs e)
    {
        if (IsEnabled)
        {
            await OnKeyDown.InvokeAsync(e).ConfigureAwait(false);
        }
    }

    private async Task HandleKeyUp(KeyboardEventArgs e)
    {
        if (IsEnabled)
        {
            await OnKeyUp.InvokeAsync(e).ConfigureAwait(false);
        }
    }

    private async Task HandleClick(MouseEventArgs e)
    {
        if (IsEnabled)
        {
            await OnClick.InvokeAsync(e).ConfigureAwait(false);
        }
    }

    public void ToggleRevealPassword()
    {
        _isPasswordRevealed = !_isPasswordRevealed;
        SetElementType();
    }

    /// <inheritdoc />
    protected override bool TryParseValueFromString(string? value, out string? result, [NotNullWhen(false)] out string? validationErrorMessage)
    {
        result = Trim ? value?.Trim() : value;
        validationErrorMessage = null;
        return true;
    }
}
