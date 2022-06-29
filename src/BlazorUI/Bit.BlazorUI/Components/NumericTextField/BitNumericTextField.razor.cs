using System;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;

namespace Bit.BlazorUI;

public partial class BitNumericTextField<TValue>
{
    const int INITIAL_STEP_DELAY = 400;
    const int STEP_DELAY = 75;
    private BitNumericTextFieldLabelPosition labelPosition = BitNumericTextFieldLabelPosition.Left;
    private int precision;
    private TValue? step;
    private TValue? min;
    private TValue? max;
    private TValue? ariaValueMin;
    private TValue? ariaValueMax;
    private double internalStep;
    private double? internalMin;
    private double? internalMax;
    private string? intermediateValue;
    private string InputId = $"input{Guid.NewGuid()}";
    private Timer? timer;
    private ElementReference inputRef;
    private ElementReference buttonIncrement;
    private ElementReference buttonDecrement;
    private readonly Type typeOfValue;
    private readonly bool isDecimals;
    private readonly double minGenericValue;
    private readonly double maxGenericValue;

    public BitNumericTextField()
    {
        typeOfValue = typeof(TValue);
        typeOfValue = Nullable.GetUnderlyingType(typeOfValue) ?? typeOfValue;

        isDecimals = typeOfValue == typeof(float) || typeOfValue == typeof(double) || typeOfValue == typeof(decimal);
        minGenericValue = GetMinValue();
        maxGenericValue = GetMaxValue();
    }

    [Inject] public IJSRuntime JSRuntime { get; set; } = default!;

    /// <summary>
    /// Detailed description of the input for the benefit of screen readers
    /// </summary>
    [Parameter] public string? AriaDescription { get; set; }

    /// <summary>
    /// The position in the parent set (if in a set)
    /// </summary>
    [Parameter] public int? AriaPositionInSet { get; set; }

    /// <summary>
    /// The total size of the parent set (if in a set)
    /// </summary>
    [Parameter] public int? AriaSetSize { get; set; }

    /// <summary>
    /// Sets the control's aria-valuenow. Providing this only makes sense when using as a controlled component.
    /// </summary>
    [Parameter] public TValue? AriaValueNow { get; set; }

    /// <summary>
    /// Sets the control's aria-valuetext.
    /// </summary>
    [Parameter] public string? AriaValueText { get; set; }

    /// <summary>
    /// Min value of the numeric text field. If not provided, the numeric text field has minimum value
    /// </summary>
    [Parameter]
    public TValue? Min
    {
        get => min;
        set
        {
            internalMin = GetDoubleValueOrDefault(value);
            min = value;
        }
    }

    /// <summary>
    /// Max value of the numeric text field. If not provided, the numeric text field has max value
    /// </summary>
    [Parameter]
    public TValue? Max
    {
        get => max;
        set
        {
            internalMax = GetDoubleValueOrDefault(value);
            max = value;
        }
    }

    /// <summary>
    /// Callback for when the numeric text field value change
    /// </summary>
    [Parameter] public EventCallback<TValue> OnChange { get; set; }

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
    [Parameter] public EventCallback<BitNumericTextFieldChangeValue<TValue>> OnDecrement { get; set; }

    /// <summary>
    /// Callback for when the increment button or up arrow key is pressed
    /// </summary>
    [Parameter] public EventCallback<BitNumericTextFieldChangeValue<TValue>> OnIncrement { get; set; }

    /// <summary>
    /// Initial value of the numeric text field
    /// </summary>
    [Parameter] public TValue? DefaultValue { get; set; }

    /// <summary>
    /// Difference between two adjacent values of the numeric text field
    /// </summary>
    [Parameter]
    public TValue? Step
    {
        get => step;
        set
        {
            internalStep = GetDoubleValueOrDefault(value) ?? 1;
            step = value;
        }
    }

    /// <summary>
    /// A text is shown after the numeric text field value
    /// </summary>
    [Parameter] public string Suffix { get; set; } = string.Empty;

    /// <summary>
    /// Descriptive label for the numeric text field, Label displayed above the numeric text field and read by screen readers
    /// </summary>
    [Parameter] public string Label { get; set; } = string.Empty;

    /// <summary>
    /// Shows the custom Label for numeric text field. If you don't call default label, ensure that you give your custom label an id and that you set the input's aria-labelledby prop to that id.
    /// </summary>
    [Parameter] public RenderFragment? LabelFragment { get; set; }

    /// <summary>
    /// Icon name for an icon to display alongside the numeric text field's label
    /// </summary>
    [Parameter] public BitIconName? IconName { get; set; }

    /// <summary>
    /// The aria label of the icon for the benefit of screen readers
    /// </summary>
    [Parameter] public string IconAriaLabel { get; set; } = string.Empty;

    /// <summary>
    /// The position of the label in regards to the numeric text field
    /// </summary>
    [Parameter]
    public BitNumericTextFieldLabelPosition LabelPosition
    {
        get => labelPosition;
        set
        {
            labelPosition = value;
            ClassBuilder.Reset();
        }
    }

    /// <summary>
    /// Accessible label text for the decrement button (for screen reader users)
    /// </summary>
    [Parameter] public string? DecrementButtonAriaLabel { get; set; }

    /// <summary>
    /// Accessible label text for the increment button (for screen reader users)
    /// </summary>
    [Parameter] public string? IncrementButtonAriaLabel { get; set; }

    /// <summary>
    /// Custom icon name for the decrement button
    /// </summary>
    [Parameter] public BitIconName DecrementButtonIconName { get; set; } = BitIconName.ChevronDownSmall;

    /// <summary>
    /// Custom icon name for the increment button
    /// </summary>
    [Parameter] public BitIconName IncrementButtonIconName { get; set; } = BitIconName.ChevronUpSmall;

    /// <summary>
    /// A more descriptive title for the control, visible on its tooltip
    /// </summary>
    [Parameter] public string? Title { get; set; }

    /// <summary>
    /// How many decimal places the value should be rounded to
    /// </summary>
    [Parameter] public int? Precision { get; set; }

    [Parameter] public EventCallback<BitNumericTextFieldAction> ChangeHandler { get; set; }

    /// <summary>
    /// Whether to show the up/down spinner arrows (buttons)
    /// </summary>
    [Parameter] public bool Arrows { get; set; }

    protected override string RootElementClass => "bit-ntf";

    protected override void RegisterComponentClasses()
    {
        ClassBuilder.Register(() => LabelPosition == BitNumericTextFieldLabelPosition.Left
                                            ? $"{RootElementClass}-label-left-{VisualClassRegistrar()}"
                                            : $"{RootElementClass}-label-top-{VisualClassRegistrar()}");

        ClassBuilder.Register(() => ValueInvalid is true
                                            ? $"{RootElementClass}-invalid-{VisualClassRegistrar()}"
                                            : string.Empty);
    }

    protected async override Task OnParametersSetAsync()
    {
        if (internalMin.HasValue is false)
        {
            internalMin = minGenericValue;
        }

        if (internalMax.HasValue is false)
        {
            internalMax = maxGenericValue;
        }

        if (internalMin > internalMax)
        {
            internalMin += internalMax;
            internalMax = internalMin - internalMax;
            internalMin -= internalMax;
        }

        precision = Precision is not null ? Precision.Value : CalculatePrecision(Step);
        if (ValueHasBeenSet is false)
        {
            SetValue(GetDoubleValueOrDefault(DefaultValue) ?? Math.Min(0, internalMin.Value));
        }
        else
        {
            SetDisplayValue();
        }
            if (Min is not null && Max is not null && GetDoubleValueOrDefault(Min) > GetDoubleValueOrDefault(Max))
            {
                ariaValueMin = Max;
                ariaValueMax = Min;
            }
            else
            {
                ariaValueMin = Min;
                ariaValueMax = Max;
            }

            precision = Precision is not null ? Precision.Value : CalculatePrecision(Step);
            if (ValueHasBeenSet is false)
            {
                SetValue(GetDoubleValueOrDefault(DefaultValue) ?? Math.Min(0, internalMin.Value));
            }
            else
            {
                SetDisplayValue();
            }

        if (ChangeHandler.HasDelegate is false)
        {
            ChangeHandler = EventCallback.Factory.Create(this, async (BitNumericTextFieldAction action) =>
            {
                double result = 0;
                bool isValid = false;

                switch (action)
                {
                    case BitNumericTextFieldAction.Increment:
                        result = GetDoubleValueOrDefault(CurrentValue, 0d)!.Value + internalStep;
                        isValid = result <= internalMax && result >= internalMin;
                        break;

                    case BitNumericTextFieldAction.Decrement:
                        result = GetDoubleValueOrDefault(CurrentValue, 0d)!.Value - internalStep;
                        isValid = result <= internalMax && result >= internalMin;
                        break;

                    default:
                        break;
                }

                if (isValid is false) return;

                SetValue(result);
                await OnChange.InvokeAsync(CurrentValue);
            });
        }

        await base.OnParametersSetAsync();
    }

    private async void HandleMouseDown(BitNumericTextFieldAction action, MouseEventArgs e)
    {
        //Change focus from input to numeric text field
        if (action == BitNumericTextFieldAction.Increment)
        {
            await buttonIncrement.FocusAsync();
        }
        else
        {
            await buttonDecrement.FocusAsync();
        }


        await HandleMouseDownAction(action, e);
        timer = new Timer((_) =>
        {
            InvokeAsync(async () =>
            {
                await HandleMouseDownAction(action, e);
                StateHasChanged();
            });
        }, null, INITIAL_STEP_DELAY, STEP_DELAY);
    }

    private void HandleMouseUpOrOut()
    {
        if (timer is null) return;
        timer.Dispose();
    }

    private void HandleChange(ChangeEventArgs e)
    {
        if (IsEnabled is false) return;
        if (ValueHasBeenSet && ValueChanged.HasDelegate is false) return;

        intermediateValue = GetCleanValue(e.Value?.ToString());
    }

    private async Task HandleMouseDownAction(BitNumericTextFieldAction action, MouseEventArgs e)
    {
        if (IsEnabled is false) return;
        if (ValueHasBeenSet && ValueChanged.HasDelegate is false) return;

        await ChangeHandler.InvokeAsync(action);
        if (action is BitNumericTextFieldAction.Increment && OnIncrement.HasDelegate is true)
        {
            var args = new BitNumericTextFieldChangeValue<TValue>
            {
                Value = CurrentValue,
                MouseEventArgs = e
            };
            await OnIncrement.InvokeAsync(args);
        }

        if (action is BitNumericTextFieldAction.Decrement && OnDecrement.HasDelegate is true)
        {
            var args = new BitNumericTextFieldChangeValue<TValue>
            {
                Value = CurrentValue,
                MouseEventArgs = e
            };
            await OnDecrement.InvokeAsync(args);
        }
    }

    private async Task HandleKeyDown(KeyboardEventArgs e)
    {
        if (IsEnabled is false) return;
        if (ValueHasBeenSet && ValueChanged.HasDelegate is false) return;

        switch (e.Key)
        {
            case "ArrowUp":
                await CheckIntermediateValueAndSetValue();
                await ChangeHandler.InvokeAsync(BitNumericTextFieldAction.Increment);
                break;

            case "ArrowDown":
                await CheckIntermediateValueAndSetValue();
                await ChangeHandler.InvokeAsync(BitNumericTextFieldAction.Decrement);
                break;

            case "Enter":
                if (intermediateValue == CurrentValueAsString) break;

                var isNumber = double.TryParse(intermediateValue, out var numericValue);
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

            default:
                break;
        }

        if (e.Key is "ArrowUp" && OnIncrement.HasDelegate is true)
        {
            var args = new BitNumericTextFieldChangeValue<TValue>
            {
                Value = CurrentValue,
                KeyboardEventArgs = e
            };
            await OnIncrement.InvokeAsync(args);
        }

        if (e.Key is "ArrowDown" && OnDecrement.HasDelegate is true)
        {
            var args = new BitNumericTextFieldChangeValue<TValue>
            {
                Value = CurrentValue,
                KeyboardEventArgs = e
            };
            await OnDecrement.InvokeAsync(args);
        }
    }

    private async Task HandleBlur(FocusEventArgs e)
    {
        if (IsEnabled is false) return;
        await OnBlur.InvokeAsync(e);

        await CheckIntermediateValueAndSetValue();
    }

    private async Task HandleFocus(FocusEventArgs e)
    {
        if (IsEnabled)
        {
            await OnFocus.InvokeAsync(e);
            await JSRuntime.SelectText(inputRef);
        }
    }

    private int CalculatePrecision(TValue? value)
    {
        if (value is null) return 0;

        var pattern = isDecimals ? @"[1-9]([0]+$)|\.([0-9]*)" : @"(^-\d+$)|\d+";
        var regex = new Regex(pattern);
        if (regex.IsMatch($"{value}") is false) return 0;

        var matches = regex.Matches($"{value}");
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

        if (value > internalMax)
        {
            CurrentValue = GetGenericValue(internalMax.Value);
        }
        else if (value < internalMin)
        {
            CurrentValue = GetGenericValue(internalMin.Value);
        }
        else
        {
            CurrentValue = GetGenericValue(value);
        }
        SetDisplayValue();
    }

    private void SetDisplayValue()
    {
        intermediateValue = CurrentValueAsString + Suffix;
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
        if (intermediateValue == CurrentValueAsString) return;

        var isNumber = double.TryParse(intermediateValue, out var numericValue);
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

    private double Normalize(double value) => Math.Round(value, precision);
    private double NormalizeDecimal(decimal value) => Convert.ToDouble(Math.Round(value, precision));

    private TValue? GetAriaValueNow => AriaValueNow is not null ? AriaValueNow : Suffix.HasNoValue() ? CurrentValue : default;
    private string? GetAriaValueText => AriaValueText.HasValue() ? AriaValueText : Suffix.HasValue() ? CurrentValueAsString + Suffix : null;
    private string? GetIconRole => IconAriaLabel.HasValue() ? "img" : null;
    private string GetLabelId => Label.HasValue() ? $"label{Guid.NewGuid()}" : string.Empty;

    private TValue? GetGenericValue(double? value) => value.HasValue ? (TValue)Convert.ChangeType(value, typeOfValue, CultureInfo.InvariantCulture) : default;
    private double? GetDoubleValueOrDefault(TValue? value, double? defaultValue = null) => value is null ? defaultValue : (double?)Convert.ChangeType(value, typeof(double), CultureInfo.InvariantCulture);

    private double GetMaxValue()
    {
        if (typeOfValue == typeof(byte))
        {
            return byte.MaxValue;
        }
        else if (typeOfValue == typeof(sbyte))
        {
            return sbyte.MaxValue;
        }
        else if (typeOfValue == typeof(short))
        {
            return short.MaxValue;
        }
        else if (typeOfValue == typeof(ushort))
        {
            return ushort.MaxValue;
        }
        else if (typeOfValue == typeof(int))
        {
            return int.MaxValue;
        }
        else if (typeOfValue == typeof(uint))
        {
            return uint.MaxValue;
        }
        else if (typeOfValue == typeof(long))
        {
            return long.MaxValue;
        }
        else if (typeOfValue == typeof(ulong))
        {
            return ulong.MaxValue;
        }
        else if (typeOfValue == typeof(double))
        {
            return double.MaxValue;
        }
        else if (typeOfValue == typeof(float))
        {
            return float.MaxValue;
        }
        else if (typeOfValue == typeof(decimal))
        {
            return (double)decimal.MaxValue;
        }
        else
        {
            return double.MaxValue;
        }
    }

    private double GetMinValue()
    {
        if (typeOfValue == typeof(byte))
        {
            return byte.MinValue;
        }
        else if (typeOfValue == typeof(sbyte))
        {
            return sbyte.MinValue;
        }
        else if (typeOfValue == typeof(short))
        {
            return short.MinValue;
        }
        else if (typeOfValue == typeof(ushort))
        {
            return ushort.MinValue;
        }
        else if (typeOfValue == typeof(int))
        {
            return int.MinValue;
        }
        else if (typeOfValue == typeof(uint))
        {
            return uint.MinValue;
        }
        else if (typeOfValue == typeof(long))
        {
            return long.MinValue;
        }
        else if (typeOfValue == typeof(ulong))
        {
            return ulong.MinValue;
        }
        else if (typeOfValue == typeof(double))
        {
            return double.MinValue;
        }
        else if (typeOfValue == typeof(float))
        {
            return float.MinValue;
        }
        else if (typeOfValue == typeof(decimal))
        {
            return (double)decimal.MinValue;
        }
        else
        {
            return double.MinValue;
        }
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            timer?.Dispose();
        }

        base.Dispose(disposing);
    }

    /// <inheritdoc />
    protected override bool TryParseValueFromString(string? value, [MaybeNullWhen(false)] out TValue? result, [NotNullWhen(false)] out string? validationErrorMessage)
    {
        if (typeOfValue == typeof(byte))
        {
            if (byte.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsedValue))
            {
                result = GetGenericValue(Normalize(parsedValue));
                validationErrorMessage = null;
                return true;
            }
        }
        else if (typeOfValue == typeof(sbyte))
        {
            if (sbyte.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsedValue))
            {
                result = GetGenericValue(Normalize(parsedValue));
                validationErrorMessage = null;
                return true;
            }
        }
        else if (typeOfValue == typeof(short))
        {
            if (short.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsedValue))
            {
                result = GetGenericValue(Normalize(parsedValue));
                validationErrorMessage = null;
                return true;
            }
        }
        else if (typeOfValue == typeof(ushort))
        {
            if (ushort.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsedValue))
            {
                result = GetGenericValue(Normalize(parsedValue));
                validationErrorMessage = null;
                return true;
            }
        }
        else if (typeOfValue == typeof(int))
        {
            if (int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsedValue))
            {
                result = GetGenericValue(Normalize(parsedValue));
                validationErrorMessage = null;
                return true;
            }
        }
        else if (typeOfValue == typeof(uint))
        {
            if (uint.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsedValue))
            {
                result = GetGenericValue(Normalize(parsedValue));
                validationErrorMessage = null;
                return true;
            }
        }
        else if (typeOfValue == typeof(long))
        {
            if (long.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsedValue))
            {
                result = GetGenericValue(Normalize(parsedValue));
                validationErrorMessage = null;
                return true;
            }
        }
        else if (typeOfValue == typeof(ulong))
        {
            if (ulong.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsedValue))
            {
                result = GetGenericValue(Normalize(parsedValue));
                validationErrorMessage = null;
                return true;
            }
        }
        else if (typeOfValue == typeof(double))
        {
            if (double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var parsedValue))
            {
                result = GetGenericValue(Normalize(parsedValue));
                validationErrorMessage = null;
                return true;
            }
        }
        else if (typeOfValue == typeof(float))
        {
            if (float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var parsedValue))
            {
                result = GetGenericValue(Normalize(parsedValue));
                validationErrorMessage = null;
                return true;
            }
        }
        else if (typeOfValue == typeof(decimal))
        {
            if (decimal.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var parsedValue))
            {
                result = GetGenericValue(NormalizeDecimal(parsedValue));
                validationErrorMessage = null;
                return true;
            }
        }

        result = default;
        validationErrorMessage = $"The {DisplayName ?? FieldIdentifier.FieldName} field is not valid.";
        return false;
    }
}
