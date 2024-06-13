using System.Diagnostics.CodeAnalysis;

namespace Bit.BlazorUI;

public partial class BitCheckbox : IDisposable
{
    private bool IsIndeterminateHasBeenSet;

    private bool isIndeterminate;
    private BitCheckboxSide boxSide;

    private string _inputId = string.Empty;
    private ElementReference _checkboxElement;

    [Inject] private IJSRuntime _js { get; set; } = default!;

    /// <summary>
    /// Detailed description of the checkbox input for the benefit of screen readers
    /// </summary>
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
    /// Determines whether the checkbox should be shown before the label (start) or after (end)
    /// </summary>
    [Parameter]
    public BitCheckboxSide BoxSide
    {
        get => boxSide;
        set
        {
            if (value == boxSide) return;
            boxSide = value;
            ClassBuilder.Reset();
        }
    }

    /// <summary>
    /// Custom icon for the check mark rendered by the checkbox instead of default check mark icon
    /// </summary>
    [Parameter] public string CheckmarkIconName { get; set; } = "Accept";

    /// <summary>
    /// he aria label of the icon for the benefit of screen readers
    /// </summary>
    [Parameter] public string? CheckmarkIconAriaLabel { get; set; }

    /// <summary>
    /// Used to customize the content of checkbox(Label and Box).
    /// </summary>
    [Parameter] public RenderFragment? ChildContent { get; set; }

    /// <summary>
    /// Custom CSS classes for different parts of the BitCheckbox.
    /// </summary>
    [Parameter] public BitCheckboxClassStyles? Classes { get; set; }

    /// <summary>
    /// Default indeterminate visual state for checkbox
    /// </summary>
    [Parameter] public bool? DefaultIsIndeterminate { get; set; }

    /// <summary>
    /// Default checkbox state
    /// Use this if you want an uncontrolled component, meaning the Checkbox instance maintains its own state.
    /// </summary>
    [Parameter] public bool? DefaultValue { get; set; }

    /// <summary>
    /// An indeterminate visual state for checkbox. 
    /// Setting indeterminate state takes visual precedence over checked given but does not affect on Value state
    /// </summary>
    [Parameter]
    public bool IsIndeterminate
    {
        get => isIndeterminate;
        set
        {
            if (value == isIndeterminate) return;
            isIndeterminate = value;
            _ = _js.SetProperty(_checkboxElement, "indeterminate", value);
            ClassBuilder.Reset();
            _ = IsIndeterminateChanged.InvokeAsync(value);
        }
    }
    [Parameter] public EventCallback<bool> IsIndeterminateChanged { get; set; }

    /// <summary>
    /// Descriptive label for the checkbox.
    /// </summary>
    [Parameter] public string? Label { get; set; }

    /// <summary>
    /// Used to customize the label for the checkbox.
    /// </summary>
    [Parameter] public RenderFragment? LabelTemplate { get; set; }

    /// <summary>
    /// Name for the checkbox input. This is intended for use with forms and NOT displayed in the UI
    /// </summary>
    [Parameter] public string? Name { get; set; }

    /// <summary>
    ///  Callback that is called when the check box is clicked
    /// </summary>
    [Parameter] public EventCallback<MouseEventArgs> OnClick { get; set; }

    /// <summary>
    /// Custom CSS styles for different parts of the BitCheckbox.
    /// </summary>
    [Parameter] public BitCheckboxClassStyles? Styles { get; set; }

    /// <summary>
    /// Title text applied to the root element and the hidden checkbox input
    /// </summary>
    [Parameter] public string? Title { get; set; }



    protected override string RootElementClass => "bit-chb";
    protected override void RegisterCssClasses()
    {
        ClassBuilder.Register(() => Classes?.Root);

        ClassBuilder.Register(() => IsIndeterminate ? $"{RootElementClass}-ind" : string.Empty);

        ClassBuilder.Register(() => CurrentValue ? $"{RootElementClass}-ckd" : string.Empty);

        ClassBuilder.Register(() => BoxSide == BitCheckboxSide.End ? $"{RootElementClass}-end" : string.Empty);
    }
    protected override void RegisterCssStyles()
    {
        StyleBuilder.Register(() => Styles?.Root);
    }

    protected override void OnInitialized()
    {
        _inputId = $"BitCheckbox-{UniqueId}-input";

        OnValueChanged += HandleOnValueChanged;

        if (ValueHasBeenSet is false && DefaultValue is not null)
        {
            InitCurrentValue(DefaultValue.Value);
        }

        if (IsIndeterminateHasBeenSet is false && DefaultIsIndeterminate is not null)
        {
            IsIndeterminate = DefaultIsIndeterminate.Value;
        }

        base.OnInitialized();
    }

    protected override void OnAfterRender(bool firstRender)
    {
        if (firstRender)
        {
            _ = _js.SetProperty(_checkboxElement, "indeterminate", IsIndeterminate);
        }

        base.OnAfterRender(firstRender);
    }

    protected override bool TryParseValueFromString(string? value, out bool result, [NotNullWhen(false)] out string? validationErrorMessage)
        => throw new NotSupportedException($"This component does not parse string inputs. Bind to the '{nameof(CurrentValue)}' property, not '{nameof(CurrentValueAsString)}'.");

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            OnValueChanged -= HandleOnValueChanged;
        }

        base.Dispose(disposing);
    }



    private async Task HandleCheckboxClick(MouseEventArgs args)
    {
        if (IsEnabled is false) return;

        await OnClick.InvokeAsync(args);

        if (IsIndeterminate)
        {
            if (IsIndeterminateHasBeenSet && IsIndeterminateChanged.HasDelegate is false) return;

            IsIndeterminate = false;
        }

        CurrentValue = !CurrentValue;
    }

    private void HandleOnValueChanged(object? sender, EventArgs args)
    {
        ClassBuilder.Reset();
    }
}
