using System.Diagnostics.CodeAnalysis;

namespace Bit.BlazorUI;

public partial class BitSearchBox
{
    private string _inputId = string.Empty;
    private ElementReference _inputRef = default!;
    private bool disableAnimation;
    private bool isUnderlined;
    private bool inputHasFocus;
    private bool showIcon;

    private bool InputHasFocus
    {
        get => inputHasFocus;
        set
        {
            inputHasFocus = value;
            ClassBuilder.Reset();
        }
    }

    [Inject] private IJSRuntime _js { get; set; } = default!;

    /// <summary>
    /// Specifies the value of the autocomplete attribute of the input component.
    /// </summary>
    [Parameter] public string? Autocomplete { get; set; }

    /// <summary>
    /// The default value of the text in the SearchBox, in the case of an uncontrolled component.
    /// </summary>
    [Parameter] public string? DefaultValue { get; set; }

    /// <summary>
    /// Whether or not to animate the search box icon on focus.
    /// </summary>
    [Parameter]
    public bool DisableAnimation
    {
        get => disableAnimation;
        set
        {
            disableAnimation = value;
            ClassBuilder.Reset();
        }
    }

    /// <summary>
    /// Whether or not the SearchBox is underlined.
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
    /// The icon name for the icon shown at the beginning of the search box.
    /// </summary>
    [Parameter] public BitIconName IconName { get; set; } = BitIconName.Search;

    /// <summary>
    /// Callback for when the input value changes.
    /// </summary>
    [Parameter] public EventCallback<string?> OnChange { get; set; }

    /// <summary>
    /// Callback executed when the user presses escape in the search box.
    /// </summary>
    [Parameter] public EventCallback OnEscape { get; set; }

    /// <summary>
    /// Callback executed when the user clears the search box by either clicking 'X' or hitting escape.
    /// </summary>
    [Parameter] public EventCallback OnClear { get; set; }

    /// <summary>
    /// Callback executed when the user presses enter in the search box.
    /// </summary>
    [Parameter] public EventCallback<string> OnSearch { get; set; }

    /// <summary>
    /// Placeholder for the search box.
    /// </summary>
    [Parameter] public string? Placeholder { get; set; }

    /// <summary>
    /// Whether or not to make the icon be always visible (it hides by default when the search box is focused).
    /// </summary>
    [Parameter]
    public bool ShowIcon
    {
        get => showIcon;
        set
        {
            showIcon = value;
            ClassBuilder.Reset();
        }
    }

    protected override Task OnInitializedAsync()
    {
        if (CurrentValueAsString.HasNoValue() && DefaultValue.HasValue())
        {
            CurrentValueAsString = DefaultValue;
        }

        OnValueChanged += HandleOnValueChanged;

        _inputId = $"{RootElementClass}-{UniqueId}";

        return base.OnInitializedAsync();
    }

    protected override string RootElementClass => "bit-srb";

    protected override void RegisterComponentClasses()
    {
        ClassBuilder.Register(() => CurrentValue.HasValue()
                                    ? $"{RootElementClass}{(ShowIcon ? "-fixed-icon" : string.Empty)}-has-value-{VisualClassRegistrar()}"
                                    : string.Empty);

        ClassBuilder.Register(() => DisableAnimation
                                    ? $"{RootElementClass}-no-animation-{VisualClassRegistrar()}"
                                    : string.Empty);

        ClassBuilder.Register(() => IsUnderlined
                                    ? $"{RootElementClass}-underlined-{VisualClassRegistrar()}"
                                    : string.Empty);

        ClassBuilder.Register(() => InputHasFocus
                                    ? $"{RootElementClass}{(ShowIcon ? "-fixed-icon" : string.Empty)}-focused-{VisualClassRegistrar()}"
                                    : string.Empty);

        ClassBuilder.Register(() => ValueInvalid is true
                                    ? $"{RootElementClass}-invalid-{VisualClassRegistrar()}"
                                    : string.Empty);
    }

    private void HandleOnValueChanged(object? sender, EventArgs args) => ClassBuilder.Reset();

    private void HandleInputFocusIn()
    {
        InputHasFocus = true;
    }

    private void HandleInputFocusOut()
    {
        InputHasFocus = false;
    }

    private async Task HandleOnClear()
    {
        if (IsEnabled is false) return;

        CurrentValueAsString = string.Empty;
        await _inputRef.FocusAsync();
        await OnClear.InvokeAsync();
    }

    private async Task HandleOnChange(ChangeEventArgs e)
    {
        if (IsEnabled is false) return;
        if (ValueHasBeenSet && ValueChanged.HasDelegate is false) return;

        CurrentValueAsString = e.Value?.ToString();
        await OnChange.InvokeAsync(CurrentValue);
    }

    private async Task HandleOnKeyDown(KeyboardEventArgs eventArgs)
    {
        if (IsEnabled is false) return;

        if (eventArgs.Code == "Escape")
        {
            CurrentValueAsString = string.Empty;
            //await InputRef.FocusAsync(); // is it required when the keydown event is captured on the input itself?
            await OnEscape.InvokeAsync();
            await OnClear.InvokeAsync();
        }
        else if (eventArgs.Code == "Enter")
        {
            CurrentValueAsString = await _js.GetProperty(_inputRef, "value");
            await OnSearch.InvokeAsync(CurrentValue);
        }
    }

    /// <inheritdoc />
    protected override bool TryParseValueFromString(string? value, out string? result, [NotNullWhen(false)] out string? validationErrorMessage)
    {
        result = value;
        validationErrorMessage = null;
        return true;
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            OnValueChanged -= HandleOnValueChanged;
        }

        base.Dispose(disposing);
    }
}
