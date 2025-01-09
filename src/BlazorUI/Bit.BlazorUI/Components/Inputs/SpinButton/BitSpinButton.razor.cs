using System.Globalization;
using System.Text.RegularExpressions;
using System.Diagnostics.CodeAnalysis;

namespace Bit.BlazorUI;

/// <summary>
/// A spin button (SpinButton) allows someone to incrementally adjust a value in small steps. It’s mainly used for numeric values, but other values are supported too.
/// </summary>
public partial class BitSpinButton : BitInputBase<double>
{
    private double _min;
    private double _max;
    private int _precision;
    private string? _intermediateValue;
    private string _inputId = default!;
    private ElementReference _incrementBtnRef;
    private ElementReference _decrementBtnRef;
    private CancellationTokenSource _cancellationTokenSource = new();



    [Inject] private IJSRuntime _js { get; set; } = default!;



    /// <summary>
    /// Detailed description of the input for the benefit of screen readers.
    /// </summary>
    [Parameter] public string? AriaDescription { get; set; }

    /// <summary>
    /// The position in the parent set (if in a set).
    /// </summary>
    [Parameter] public int? AriaPositionInSet { get; set; }

    /// <summary>
    /// The total size of the parent set (if in a set).
    /// </summary>
    [Parameter] public int? AriaSetSize { get; set; }

    /// <summary>
    /// Sets the control's aria-valuenow. Providing this only makes sense when using as a controlled component.
    /// </summary>
    [Parameter] public double? AriaValueNow { get; set; }

    /// <summary>
    /// Sets the control's aria-valuetext.
    /// </summary>
    [Parameter] public string? AriaValueText { get; set; }

    /// <summary>
    /// Custom CSS classes for different parts of the BitSpinButton.
    /// </summary>
    [Parameter] public BitSpinButtonClassStyles? Classes { get; set; }

    /// <summary>
    /// Accessible label text for the decrement button (for screen reader users).
    /// </summary>
    [Parameter] public string? DecrementAriaLabel { get; set; }

    /// <summary>
    /// Custom icon name for the decrement button.
    /// </summary>
    [Parameter] public string? DecrementIconName { get; set; }

    /// <summary>
    /// The title to show when the mouse is placed on the decrement button.
    /// </summary>
    [Parameter] public string? DecrementTitle { get; set; }

    /// <summary>
    /// Initial value of the spin button.
    /// </summary>
    [Parameter] public double? DefaultValue { get; set; }

    /// <summary>
    /// The aria label of the icon for the benefit of screen readers.
    /// </summary>
    [Parameter] public string IconAriaLabel { get; set; } = string.Empty;

    /// <summary>
    /// Icon name for an icon to display alongside the spin button's label.
    /// </summary>
    [Parameter] public string? IconName { get; set; }

    /// <summary>
    /// Accessible label text for the increment button (for screen reader users).
    /// </summary>
    [Parameter] public string? IncrementAriaLabel { get; set; }

    /// <summary>
    /// Custom icon name for the increment button.
    /// </summary>
    [Parameter] public string? IncrementIconName { get; set; }

    /// <summary>
    /// The title to show when the mouse is placed on the increment button.
    /// </summary>
    [Parameter] public string? IncrementTitle { get; set; }

    /// <summary>
    /// If true, the input is readonly.
    /// </summary>
    [Parameter] public bool IsInputReadOnly { get; set; }

    /// <summary>
    /// Descriptive label for the spin button, Label displayed above the spin button and read by screen readers.
    /// </summary>
    [Parameter] public string Label { get; set; } = string.Empty;

    /// <summary>
    /// The position of the label in regards to the spin button.
    /// </summary>
    [Parameter, ResetClassBuilder]
    public BitLabelPosition LabelPosition { get; set; } = BitLabelPosition.Top;

    /// <summary>
    /// Custom Label content for spin button.
    /// </summary>
    [Parameter] public RenderFragment? LabelTemplate { get; set; }

    /// <summary>
    /// Max value of the spin button. If not provided, the spin button has max value of double type.
    /// </summary>
    [Parameter] public double? Max { get; set; }

    /// <summary>
    /// Min value of the spin button. If not provided, the spin button has minimum value of double type.
    /// </summary>
    [Parameter] public double? Min { get; set; }

    /// <summary>
    /// Determines how the spinning buttons should be rendered.
    /// </summary>
    [Parameter] public BitSpinButtonMode Mode { get; set; } = BitSpinButtonMode.Compact;

    /// <summary>
    /// Callback for when the control loses focus.
    /// </summary>
    [Parameter] public EventCallback<FocusEventArgs> OnBlur { get; set; }

    /// <summary>
    /// Callback for when the decrement button or down arrow key is pressed.
    /// </summary>
    [Parameter] public EventCallback<double> OnDecrement { get; set; }

    /// <summary>
    /// Callback for when focus moves into the input.
    /// </summary>
    [Parameter] public EventCallback<FocusEventArgs> OnFocus { get; set; }

    /// <summary>
    /// Callback for when the increment button or up arrow key is pressed.
    /// </summary>
    [Parameter] public EventCallback<double> OnIncrement { get; set; }

    /// <summary>
    /// How many decimal places the value should be rounded to.
    /// </summary>
    [Parameter] public int? Precision { get; set; }

    /// <summary>
    /// If false, the input is hidden.
    /// </summary>
    [Parameter] public bool ShowInput { get; set; } = true;

    /// <summary>
    /// Difference between two adjacent values of the spin button.
    /// </summary>
    [Parameter] public double Step { get; set; } = 1;

    /// <summary>
    /// Custom CSS styles for different parts of the BitSpinButton.
    /// </summary>
    [Parameter] public BitSpinButtonClassStyles? Styles { get; set; }

    /// <summary>
    /// A text is shown after the spin button value.
    /// </summary>
    [Parameter] public string Suffix { get; set; } = string.Empty;

    /// <summary>
    /// A more descriptive title for the control, visible on its tooltip.
    /// </summary>
    [Parameter] public string? Title { get; set; }

    /// <summary>
    /// The message format used for invalid values entered in the input.
    /// </summary>
    [Parameter] public string ValidationMessage { get; set; } = "The {DisplayName ?? FieldIdentifier.FieldName} field is not valid.";



    protected override string RootElementClass => "bit-spb";

    protected override void RegisterCssClasses()
    {
        ClassBuilder.Register(() => Classes?.Root);

        ClassBuilder.Register(() => LabelPosition switch
        {
            BitLabelPosition.Bottom => $"{RootElementClass}-lbt",
            BitLabelPosition.Start => $"{RootElementClass}-lst",
            BitLabelPosition.End => $"{RootElementClass}-led",
            _ => $"{RootElementClass}-ltp"
        });
    }

    protected override void RegisterCssStyles()
    {
        StyleBuilder.Register(() => Styles?.Root);
    }

    protected override async Task OnInitializedAsync()
    {
        _inputId = $"BitSpinButton-{UniqueId}-input";

        if (ValueHasBeenSet is false && DefaultValue.HasValue)
        {
            Value = DefaultValue.Value;
        }

        await base.OnInitializedAsync();
    }

    protected override async Task OnParametersSetAsync()
    {
        if (_min != Min || _max != Max)
        {
            _min = Min ?? double.MinValue;
            _max = Max ?? double.MaxValue;

            if (_min > _max)
            {
                _min += _max;
                _max = _min - _max;
                _min -= _max;
            }
        }

        if (_precision != Precision)
        {
            _precision = Precision is not null ? Precision.Value : CalculatePrecision(Step);
        }

        SetDisplayValue();

        await base.OnParametersSetAsync();
    }

    protected override bool TryParseValueFromString(string? value, [MaybeNullWhen(false)] out double result, [NotNullWhen(false)] out string? validationErrorMessage)
    {
        if (double.TryParse(value, out var parsedValue))
        {
            result = Normalize(parsedValue);
            validationErrorMessage = null;
            return true;
        }

        result = default;
        validationErrorMessage = string.Format(CultureInfo.InvariantCulture, ValidationMessage, DisplayName ?? FieldIdentifier.FieldName);
        return false;
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _cancellationTokenSource?.Dispose();
        }

        base.Dispose(disposing);
    }



    private async Task ApplyValueChange(bool isIncrement)
    {
        double result = 0;
        bool isValid = false;

        if (isIncrement)
        {
            result = CurrentValue + Step;
            isValid = result <= _max && result >= _min;
        }
        else
        {
            result = CurrentValue - Step;
            isValid = result <= _max && result >= _min;
        }

        if (isValid is false) return;

        SetValue(result);

        StateHasChanged();
    }

    private async Task HandleOnPointerDown(bool isIncrement)
    {
        if (IsEnabled is false || InvalidValueBinding()) return;

        //Change focus from input to number field
        if (isIncrement)
        {
            await _incrementBtnRef.FocusAsync();
        }
        else
        {
            await _decrementBtnRef.FocusAsync();
        }

        await ChangeValue(isIncrement);
        ResetCts();

        var cts = _cancellationTokenSource;
        await Task.Run(async () =>
        {
            await InvokeAsync(async () =>
            {
                await Task.Delay(400);
                await ContinuousChangeValue(isIncrement, cts);
            });
        }, cts.Token);
    }

    private async Task ContinuousChangeValue(bool isIncrement, CancellationTokenSource cts)
    {
        if (cts.IsCancellationRequested) return;

        await ChangeValue(isIncrement);

        StateHasChanged();

        await Task.Delay(75);
        await ContinuousChangeValue(isIncrement, cts);
    }

    private async Task ChangeValue(bool isIncrement)
    {
        await ApplyValueChange(isIncrement);
        if (isIncrement && OnIncrement.HasDelegate)
        {
            await OnIncrement.InvokeAsync(CurrentValue);
        }

        if (isIncrement is false && OnDecrement.HasDelegate)
        {
            await OnDecrement.InvokeAsync(CurrentValue);
        }
    }

    private void HandleOnPointerUpOrOut()
    {
        ResetCts();
    }

    private void ResetCts()
    {
        _cancellationTokenSource?.Cancel();
        _cancellationTokenSource?.Dispose();
        _cancellationTokenSource = new();
    }

    private void HandleOnChange(ChangeEventArgs e)
    {
        if (IsEnabled is false || InvalidValueBinding()) return;
        if (IsInputReadOnly) return;

        _intermediateValue = GetCleanValue(e.Value?.ToString());
    }

    private async Task HandleOnKeyDown(KeyboardEventArgs e)
    {
        if (IsEnabled is false || InvalidValueBinding()) return;
        if (IsInputReadOnly) return;

        switch (e.Key)
        {
            case "ArrowUp":
                {
                    await CheckIntermediateValueAndSetValue();
                    await ApplyValueChange(true);

                    if (OnIncrement.HasDelegate is true)
                    {
                        await OnIncrement.InvokeAsync(CurrentValue);
                    }

                    break;
                }

            case "ArrowDown":
                {
                    await CheckIntermediateValueAndSetValue();
                    await ApplyValueChange(false);

                    if (OnDecrement.HasDelegate is true)
                    {
                        await OnDecrement.InvokeAsync(CurrentValue);
                    }

                    break;
                }

            case "Enter":
                {
                    if (_intermediateValue == CurrentValueAsString) break;

                    var isNumber = double.TryParse(_intermediateValue, out var numericValue);

                    if (isNumber)
                    {
                        SetValue(numericValue);
                    }
                    else
                    {
                        SetDisplayValue();
                    }

                    break;
                }

            default:
                break;
        }
    }

    private async Task HandleOnBlur(FocusEventArgs e)
    {
        if (IsEnabled is false) return;

        await OnBlur.InvokeAsync(e);

        await CheckIntermediateValueAndSetValue();
    }

    private async Task HandleOnFocus(FocusEventArgs e)
    {
        if (IsEnabled is false) return;

        await OnFocus.InvokeAsync(e);

        await _js.BitUtilsSelectText(InputElement);
    }

    private void SetValue(double value)
    {
        value = Normalize(value);

        if (value > _max)
        {
            CurrentValue = _max;
        }
        else if (value < _min)
        {
            CurrentValue = _min;
        }
        else
        {
            CurrentValue = value;
        }

        SetDisplayValue();
    }

    private void SetDisplayValue() => _intermediateValue = CurrentValueAsString + Suffix;

    private async Task CheckIntermediateValueAndSetValue()
    {
        if (InvalidValueBinding()) return;
        if (_intermediateValue == CurrentValueAsString) return;

        var isNumber = double.TryParse(_intermediateValue, out var numericValue);
        if (isNumber)
        {
            SetValue(numericValue);
        }
        else
        {
            SetDisplayValue();
        }
    }

    private double Normalize(double value) => Math.Round(value, _precision);

    private double? GetAriaValueNow => AriaValueNow is not null ? AriaValueNow : Suffix.HasNoValue() ? CurrentValue : null;
    private string? GetAriaValueText => AriaValueText.HasValue() ? AriaValueText : Suffix.HasValue() ? CurrentValueAsString + Suffix : null;
    private string? GetIconRole => IconAriaLabel.HasValue() ? "img" : null;
    private string GetLabelId => Label.HasValue() ? $"SpinButton-{UniqueId}-Label" : string.Empty;

    private static int CalculatePrecision(double value)
    {
        var regex = new Regex(@"[1-9]([0]+$)|\.([0-9]*)");
        if (regex.IsMatch(value.ToString(CultureInfo.InvariantCulture)) is false) return 0;

        var matches = regex.Matches(value.ToString(CultureInfo.InvariantCulture));
        if (matches.Count == 0) return 0;

        var groups = matches[0].Groups;
        if (groups[1] != null && groups[1].Length != 0)
        {
            return -groups[1].Length;
        }

        if (groups[2] != null && groups[2].Length != 0)
        {
            return groups[2].Length;
        }

        return 0;
    }

    private static string? GetCleanValue(string? value)
    {
        if (value.HasNoValue()) return value;

        if (char.IsDigit(value![0]))
        {
            Regex pattern = new Regex(@"-?\d+(?:\.\d+)?");
            var match = pattern.Match(value);
            if (match.Success)
            {
                return match.Value;
            }
        }

        return value;
    }
}
