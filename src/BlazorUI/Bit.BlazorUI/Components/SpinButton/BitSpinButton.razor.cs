using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Text.RegularExpressions;

namespace Bit.BlazorUI;

public partial class BitSpinButton
{
    private const int INITIAL_STEP_DELAY = 400;
    private const int STEP_DELAY = 75;
    private BitSpinButtonLabelPosition labelPosition = BitSpinButtonLabelPosition.Top;
    private double _min;
    private double _max;
    private int _precision;
    private string? _intermediateValue;
    private Timer? _mouseDownTimer;
    private string _inputId = $"input-{Guid.NewGuid()}";
    private ElementReference _inputRef;
    private ElementReference _buttonIncrement;
    private ElementReference _buttonDecrement;

    [Inject] private IJSRuntime _js { get; set; } = default!;

    /// <summary>
    /// Detailed description of the input for the benefit of screen readers
    /// </summary>
    [Parameter] public string? AriaDescription { get; set; }

    /// <summary>
    /// The position in the parent set (if in a set)
    /// </summary>
    [Parameter]
    public int? AriaPositionInSet { get; set; }

    /// <summary>
    /// The total size of the parent set (if in a set)
    /// </summary>
    [Parameter]
    public int? AriaSetSize { get; set; }

    /// <summary>
    /// Sets the control's aria-valuenow. Providing this only makes sense when using as a controlled component.
    /// </summary>
    [Parameter]
    public double? AriaValueNow { get; set; }

    /// <summary>
    /// Sets the control's aria-valuetext.
    /// </summary>
    [Parameter]
    public string? AriaValueText { get; set; }

    /// <summary>
    /// 
    /// </summary>
    [Parameter] public EventCallback<BitSpinButtonAction> ChangeHandler { get; set; }

    /// <summary>
    /// Initial value of the spin button 
    /// </summary>
    [Parameter] public double? DefaultValue { get; set; }

    /// <summary>
    /// Accessible label text for the decrement button (for screen reader users)
    /// </summary>
    [Parameter] public string? DecrementButtonAriaLabel { get; set; }

    /// <summary>
    /// Custom icon name for the decrement button
    /// </summary>
    [Parameter] public BitIconName DecrementButtonIconName { get; set; } = BitIconName.ChevronDownSmall;

    /// <summary>
    /// Icon name for an icon to display alongside the spin button's label
    /// </summary>
    [Parameter] public BitIconName? IconName { get; set; }

    /// <summary>
    /// The aria label of the icon for the benefit of screen readers
    /// </summary>
    [Parameter] public string IconAriaLabel { get; set; } = string.Empty;

    /// <summary>
    /// Accessible label text for the increment button (for screen reader users)
    /// </summary>
    [Parameter] public string? IncrementButtonAriaLabel { get; set; }

    /// <summary>
    /// Custom icon name for the increment button
    /// </summary>
    [Parameter] public BitIconName IncrementButtonIconName { get; set; } = BitIconName.ChevronUpSmall;

    /// <summary>
    /// Descriptive label for the spin button, Label displayed above the spin button and read by screen readers
    /// </summary>
    [Parameter] public string Label { get; set; } = string.Empty;

    /// <summary>
    /// Shows the custom Label for spin button. If you don't call default label, ensure that you give your custom label an id and that you set the input's aria-labelledby prop to that id.
    /// </summary>
    [Parameter] public RenderFragment? LabelTemplate { get; set; }

    /// <summary>
    /// The position of the label in regards to the spin button
    /// </summary>
    [Parameter]
    public BitSpinButtonLabelPosition LabelPosition
    {
        get => labelPosition;
        set
        {
            labelPosition = value;
            ClassBuilder.Reset();
        }
    }

    /// <summary>
    /// Min value of the spin button. If not provided, the spin button has minimum value of double type
    /// </summary>
    [Parameter] public double? Min { get; set; }

    /// <summary>
    /// Max value of the spin button. If not provided, the spin button has max value of double type
    /// </summary>
    [Parameter] public double? Max { get; set; }

    /// <summary>
    /// Callback for when the spin button value change
    /// </summary>
    [Parameter] public EventCallback<double> OnChange { get; set; }

    /// <summary>
    /// Callback for when focus moves into the input
    /// </summary>
    [Parameter] public EventCallback<FocusEventArgs> OnFocus { get; set; }

    /// <summary>
    /// Callback for when the control loses focus
    /// </summary>
    [Parameter] public EventCallback<FocusEventArgs> OnBlur { get; set; }

    /// <summary>
    /// Callback for when the decrement button or down arrow key is pressed
    /// </summary>
    [Parameter] public EventCallback<BitSpinButtonChangeValue> OnDecrement { get; set; }

    /// <summary>
    /// Callback for when the increment button or up arrow key is pressed
    /// </summary>
    [Parameter] public EventCallback<BitSpinButtonChangeValue> OnIncrement { get; set; }

    /// <summary>
    /// How many decimal places the value should be rounded to
    /// </summary>
    [Parameter] public int? Precision { get; set; }

    /// <summary>
    /// Difference between two adjacent values of the spin button
    /// </summary>
    [Parameter] public double Step { get; set; } = 1;

    /// <summary>
    /// A text is shown after the spin button value
    /// </summary>
    [Parameter] public string Suffix { get; set; } = string.Empty;

    /// <summary>
    /// A more descriptive title for the control, visible on its tooltip
    /// </summary>
    [Parameter] public string? Title { get; set; }

    protected override string RootElementClass => "bit-spb";

    protected override async Task OnParametersSetAsync()
    {
        _min = Min.HasValue ? Min.Value : double.MinValue;
        _max = Max.HasValue ? Max.Value : double.MaxValue;

        if (_min > _max)
        {
            _min += _max;
            _max = _min - _max;
            _min -= _max;
        }

        _precision = Precision is not null ? Precision.Value : CalculatePrecision(Step);

        if (ValueHasBeenSet is false)
        {
            SetValue(DefaultValue ?? 0);
        }
        else
        {
            SetDisplayValue();
        }

        if (ChangeHandler.HasDelegate is false)
        {
            ChangeHandler = EventCallback.Factory.Create(this, async (BitSpinButtonAction action) =>
            {
                double result = 0;
                bool isValid = false;

                switch (action)
                {
                    case BitSpinButtonAction.Increment:
                        result = CurrentValue + Step;
                        isValid = result <= _max && result >= _min;
                        break;

                    case BitSpinButtonAction.Decrement:
                        result = CurrentValue - Step;
                        isValid = result <= _max && result >= _min;
                        break;
                }

                if (isValid is false) return;

                SetValue(result);
                await OnChange.InvokeAsync(CurrentValue);
            });
        }

        await base.OnParametersSetAsync();
    }

    protected override void RegisterComponentClasses()
    {
        ClassBuilder.Register(() => LabelPosition == BitSpinButtonLabelPosition.Left
                                    ? $"{RootElementClass}-label-left-{VisualClassRegistrar()}"
                                    : $"{RootElementClass}-label-top-{VisualClassRegistrar()}");

        ClassBuilder.Register(() => ValueInvalid is true
                                    ? $"{RootElementClass}-invalid-{VisualClassRegistrar()}"
                                    : string.Empty);
    }

    private async Task HandleOnMouseDown(BitSpinButtonAction action, MouseEventArgs e)
    {
        //Change focus from input to spin button
        if (action == BitSpinButtonAction.Increment)
        {
            await _buttonIncrement.FocusAsync();
        }
        else
        {
            await _buttonDecrement.FocusAsync();
        }

        await HandleMouseDownAction(action, e);

        _mouseDownTimer = new Timer(async (_) =>
        {
            await InvokeAsync(async () =>
            {
                await HandleMouseDownAction(action, e);
                StateHasChanged();
            });
        }, null, INITIAL_STEP_DELAY, STEP_DELAY);
    }

    private void HandleOnMouseUpOrOut()
    {
        if (_mouseDownTimer is not null) _mouseDownTimer.Dispose();
    }

    private void HandleOnChange(ChangeEventArgs e)
    {
        if (IsEnabled is false) return;
        if (ValueHasBeenSet && ValueChanged.HasDelegate is false) return;

        _intermediateValue = GetCleanValue(e.Value?.ToString());
    }

    private async Task HandleMouseDownAction(BitSpinButtonAction action, MouseEventArgs e)
    {
        if (IsEnabled is false) return;
        if (ValueHasBeenSet && ValueChanged.HasDelegate is false) return;

        await ChangeHandler.InvokeAsync(action);

        if (action is BitSpinButtonAction.Increment && OnIncrement.HasDelegate is true)
        {
            var args = new BitSpinButtonChangeValue
            {
                Value = CurrentValue,
                MouseEventArgs = e
            };
            await OnIncrement.InvokeAsync(args);
        }

        if (action is BitSpinButtonAction.Decrement && OnDecrement.HasDelegate is true)
        {
            var args = new BitSpinButtonChangeValue
            {
                Value = CurrentValue,
                MouseEventArgs = e
            };
            await OnDecrement.InvokeAsync(args);
        }
    }

    private async Task HandleOnKeyDown(KeyboardEventArgs e)
    {
        if (IsEnabled is false) return;
        if (ValueHasBeenSet && ValueChanged.HasDelegate is false) return;

        switch (e.Key)
        {
            case "ArrowUp":
                {
                    await CheckIntermediateValueAndSetValue();
                    await ChangeHandler.InvokeAsync(BitSpinButtonAction.Increment);

                    if (OnIncrement.HasDelegate is true)
                    {
                        var args = new BitSpinButtonChangeValue
                        {
                            Value = CurrentValue,
                            KeyboardEventArgs = e
                        };

                        await OnIncrement.InvokeAsync(args);
                    }

                    break;
                }

            case "ArrowDown":
                {
                    await CheckIntermediateValueAndSetValue();
                    await ChangeHandler.InvokeAsync(BitSpinButtonAction.Decrement);

                    if (OnDecrement.HasDelegate is true)
                    {
                        var args = new BitSpinButtonChangeValue
                        {
                            Value = CurrentValue,
                            KeyboardEventArgs = e
                        };

                        await OnDecrement.InvokeAsync(args);
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
                        await OnChange.InvokeAsync(CurrentValue);
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

        await _js.SelectText(_inputRef);
    }

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

    private void SetDisplayValue()
    {
        _intermediateValue = CurrentValueAsString + Suffix;
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

    private async Task CheckIntermediateValueAndSetValue()
    {
        if (ValueHasBeenSet && ValueChanged.HasDelegate is false) return;
        if (_intermediateValue == CurrentValueAsString) return;

        var isNumber = double.TryParse(_intermediateValue, out var numericValue);
        if (isNumber)
        {
            SetValue(numericValue);
            await OnChange.InvokeAsync(CurrentValue);
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
    private string GetLabelId => Label.HasValue() ? $"label{Guid.NewGuid()}" : string.Empty;

    /// <inheritdoc />
    protected override bool TryParseValueFromString(string? value, [MaybeNullWhen(false)] out double result, [NotNullWhen(false)] out string? validationErrorMessage)
    {
        if (double.TryParse(value, out var parsedValue))
        {
            result = Normalize(parsedValue);
            validationErrorMessage = null;
            return true;
        }

        result = default;
        validationErrorMessage = $"The {DisplayName ?? FieldIdentifier.FieldName} field is not valid.";
        return false;
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _mouseDownTimer?.Dispose();
        }

        base.Dispose(disposing);
    }
}
