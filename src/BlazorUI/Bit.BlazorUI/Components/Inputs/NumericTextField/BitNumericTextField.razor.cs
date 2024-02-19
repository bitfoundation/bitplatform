﻿using System.Globalization;
using System.Text.RegularExpressions;
using System.Diagnostics.CodeAnalysis;

namespace Bit.BlazorUI;

public partial class BitNumericTextField<TValue>
{
    private const int INITIAL_STEP_DELAY = 400;
    private const int STEP_DELAY = 75;

    private TValue? step;
    private TValue? min;
    private TValue? max;
    private bool required;
    private BitNumericTextFieldLabelPosition labelPosition = BitNumericTextFieldLabelPosition.Top;

    private double _internalStep;
    private double? _internalMin;
    private double? _internalMax;
    private int _precision;
    private string? _intermediateValue;
    private readonly string _labelId;
    private readonly string _inputId;
    private Timer? _timer;
    private ElementReference _inputRef;
    private ElementReference _buttonIncrement;
    private ElementReference _buttonDecrement;
    private readonly Type _typeOfValue;
    private bool _hasFocus;
    private readonly bool _isDecimals;
    private readonly double _minGenericValue;
    private readonly double _maxGenericValue;

    public BitNumericTextField()
    {
        _typeOfValue = typeof(TValue);
        _typeOfValue = Nullable.GetUnderlyingType(_typeOfValue) ?? _typeOfValue;

        _isDecimals = _typeOfValue == typeof(float) || _typeOfValue == typeof(double) || _typeOfValue == typeof(decimal);
        _minGenericValue = GetMinValue();
        _maxGenericValue = GetMaxValue();
        _inputId = $"BitNumericTextField-{UniqueId}-input";
        _labelId = $"BitNumericTextField-{UniqueId}-label";
    }

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
    [Parameter] public TValue? AriaValueNow { get; set; }

    /// <summary>
    /// Sets the control's aria-valuetext.
    /// </summary>
    [Parameter] public string? AriaValueText { get; set; }

    /// <summary>
    /// Custom CSS classes for different parts of the BitNumericTextField.
    /// </summary>
    [Parameter] public BitNumericTextFieldClassStyles? Classes { get; set; }

    /// <summary>
    /// Initial value of the numeric text field.
    /// </summary>
    [Parameter] public TValue? DefaultValue { get; set; }

    /// <summary>
    /// Accessible label text for the decrement button (for screen reader users).
    /// </summary>
    [Parameter] public string? DecrementAriaLabel { get; set; }

    /// <summary>
    /// Custom icon name for the decrement button.
    /// </summary>
    [Parameter] public string DecrementIconName { get; set; } = "ChevronDownSmall";

    /// <summary>
    /// Icon name for an icon to display alongside the numeric text field's label.
    /// </summary>
    [Parameter] public string? IconName { get; set; }

    /// <summary>
    /// The aria label of the icon for the benefit of screen readers.
    /// </summary>
    [Parameter] public string IconAriaLabel { get; set; } = string.Empty;

    /// <summary>
    /// Accessible label text for the increment button (for screen reader users).
    /// </summary>
    [Parameter] public string? IncrementAriaLabel { get; set; }

    /// <summary>
    /// Custom icon name for the increment button.
    /// </summary>
    [Parameter] public string IncrementIconName { get; set; } = "ChevronUpSmall";

    /// <summary>
    /// Descriptive label for the numeric text field, Label displayed above the numeric text field and read by screen readers.
    /// </summary>
    [Parameter] public string Label { get; set; } = string.Empty;

    /// <summary>
    /// Shows the custom Label for numeric text field. If you don't call default label, ensure that you give your custom label an id and that you set the input's aria-labelledby prop to that id.
    /// </summary>
    [Parameter] public RenderFragment? LabelTemplate { get; set; }

    /// <summary>
    /// The position of the label in regards to the numeric text field.
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
    /// Min value of the numeric text field. If not provided, the numeric text field has minimum value.
    /// </summary>
    [Parameter]
    public TValue? Min
    {
        get => min;
        set
        {
            _internalMin = GetDoubleValueOrDefault(value);
            min = value;
        }
    }

    /// <summary>
    /// Max value of the numeric text field. If not provided, the numeric text field has max value.
    /// </summary>
    [Parameter]
    public TValue? Max
    {
        get => max;
        set
        {
            _internalMax = GetDoubleValueOrDefault(value);
            max = value;
        }
    }

    /// <summary>
    /// The format of the number in the numeric text field.
    /// </summary>
    [Parameter] public string NumberFormat { get; set; } = "{0}";

    /// <summary>
    /// Callback for when the numeric text field value change.
    /// </summary>
    [Parameter] public EventCallback<TValue> OnChange { get; set; }

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
    /// Callback for when the control loses focus.
    /// </summary>
    [Parameter] public EventCallback<FocusEventArgs> OnBlur { get; set; }

    /// <summary>
    /// Callback for when the decrement button or down arrow key is pressed.
    /// </summary>
    [Parameter] public EventCallback<TValue> OnDecrement { get; set; }

    /// <summary>
    /// Callback for when the increment button or up arrow key is pressed.
    /// </summary>
    [Parameter] public EventCallback<TValue> OnIncrement { get; set; }

    /// <summary>
    /// How many decimal places the value should be rounded to.
    /// </summary>
    [Parameter] public int? Precision { get; set; }

    /// <summary>
    /// Input placeholder text.
    /// </summary>
    [Parameter] public string? Placeholder { get; set; }

    /// <summary>
    /// Difference between two adjacent values of the numeric text field.
    /// </summary>
    [Parameter]
    public TValue? Step
    {
        get => step;
        set
        {
            _internalStep = GetDoubleValueOrDefault(value) ?? 1;
            step = value;
        }
    }

    /// <summary>
    /// Custom CSS styles for different parts of the BitNumericTextField.
    /// </summary>
    [Parameter] public BitNumericTextFieldClassStyles? Styles { get; set; }

    /// <summary>
    /// Whether to show the increment and decrement buttons.
    /// </summary>
    [Parameter] public bool ShowButtons { get; set; }

    /// <summary>
    /// A more descriptive title for the control, visible on its tooltip.
    /// </summary>
    [Parameter] public string? Title { get; set; }

    /// <summary>
    /// The message format used for invalid values entered in the input.
    /// </summary>
    [Parameter] public string ValidationMessage { get; set; } = "The {0} field is not valid.";

    /// <summary>
    /// Prefix displayed before the numeric field contents. This is not included in the value.
    /// Ensure a descriptive label is present to assist screen readers, as the value does not include the prefix.
    /// </summary>
    [Parameter] public string? Prefix { get; set; }

    /// <summary>
    /// Shows the custom prefix for numeric field.
    /// </summary>
    [Parameter] public RenderFragment? PrefixTemplate { get; set; }

    /// <summary>
    /// Suffix displayed after the numeric field contents. This is not included in the value. 
    /// Ensure a descriptive label is present to assist screen readers, as the value does not include the suffix.
    /// </summary>
    [Parameter] public string? Suffix { get; set; }

    /// <summary>
    /// Shows the custom suffix for numeric field.
    /// </summary>
    [Parameter] public RenderFragment? SuffixTemplate { get; set; }

    /// <summary>
    /// Whether the associated input is required or not, add an asterisk "*" to its label.
    /// </summary>
    [Parameter]
    public bool Required
    {
        get => required;
        set
        {
            if (required == value) return;

            required = value;
            ClassBuilder.Reset();
        }
    }



    protected override string RootElementClass => "bit-ntf";

    protected override void RegisterCssClasses()
    {
        ClassBuilder.Register(() => Classes?.Root);

        ClassBuilder.Register(() => _hasFocus ? $"{RootElementClass}-fcs {Classes?.Focused}" : string.Empty);

        ClassBuilder.Register(() => $"{RootElementClass}-{(LabelPosition == BitNumericTextFieldLabelPosition.Left ? "llf" : "ltp")}");

        ClassBuilder.Register(() => IsEnabled && Required ? $"{RootElementClass}-req" : string.Empty);

        ClassBuilder.Register(() => IsEnabled && Required && Label.HasNoValue() ? $"{RootElementClass}-rnl" : string.Empty);
    }

    protected override void RegisterCssStyles()
    {
        StyleBuilder.Register(() => Styles?.Root);

        StyleBuilder.Register(() => _hasFocus ? Styles?.Focused : string.Empty);
    }

    protected override async Task OnParametersSetAsync()
    {
        if (_internalMin.HasValue is false)
        {
            _internalMin = _minGenericValue;
        }

        if (_internalMax.HasValue is false)
        {
            _internalMax = _maxGenericValue;
        }

        if (_internalMin > _internalMax)
        {
            _internalMin = _minGenericValue;
            _internalMax = _maxGenericValue;
        }

        _precision = Precision is not null ? Precision.Value : CalculatePrecision(Step);

        if (ValueHasBeenSet is false)
        {
            SetValue(GetDoubleValueOrDefault(DefaultValue).GetValueOrDefault());
        }
        else
        {
            SetDisplayValue();
        }

        await base.OnParametersSetAsync();
    }

    private async Task ApplyValueChange(BitNumericTextFieldAction action)
    {
        double result = 0;
        bool isValid = false;

        switch (action)
        {
            case BitNumericTextFieldAction.Increment:
                result = GetDoubleValueOrDefault(CurrentValue, 0d)!.Value + _internalStep;
                isValid = result <= _internalMax && result >= _internalMin;
                break;

            case BitNumericTextFieldAction.Decrement:
                result = GetDoubleValueOrDefault(CurrentValue, 0d)!.Value - _internalStep;
                isValid = result <= _internalMax && result >= _internalMin;
                break;

            default:
                break;
        }

        if (isValid is false) return;

        SetValue(result);

        await OnChange.InvokeAsync(CurrentValue);

        StateHasChanged();
    }

    private async Task HandleOnPointerDown(BitNumericTextFieldAction action, MouseEventArgs e)
    {
        //Change focus from input to numeric text field
        if (action == BitNumericTextFieldAction.Increment)
        {
            await _buttonIncrement.FocusAsync();
        }
        else
        {
            await _buttonDecrement.FocusAsync();
        }


        await HandleOnPointerDownAction(action, e);
        _timer = new Timer(async (_) =>
        {
            await InvokeAsync(async () =>
            {
                await HandleOnPointerDownAction(action, e);
                StateHasChanged();
            });
        }, null, INITIAL_STEP_DELAY, STEP_DELAY);
    }

    private void HandleOnPointerUpOrOut()
    {
        if (_timer is null) return;
        _timer.Dispose();
    }

    private void HandleOnChange(ChangeEventArgs e)
    {
        if (IsEnabled is false) return;
        if (ValueHasBeenSet && ValueChanged.HasDelegate is false) return;

        _intermediateValue = GetCleanValue(e.Value?.ToString());
    }

    private async Task HandleOnPointerDownAction(BitNumericTextFieldAction action, MouseEventArgs e)
    {
        if (IsEnabled is false) return;
        if (ValueHasBeenSet && ValueChanged.HasDelegate is false) return;

        await ApplyValueChange(action);
        if (action is BitNumericTextFieldAction.Increment && OnIncrement.HasDelegate)
        {
            await OnIncrement.InvokeAsync(CurrentValue);
        }

        if (action is BitNumericTextFieldAction.Decrement && OnDecrement.HasDelegate)
        {
            await OnDecrement.InvokeAsync(CurrentValue);
        }
    }

    private async Task HandleOnKeyDown(KeyboardEventArgs e)
    {
        if (IsEnabled is false) return;
        if (ValueHasBeenSet && ValueChanged.HasDelegate is false) return;

        switch (e.Key)
        {
            case "ArrowUp":
                await CheckIntermediateValueAndSetValue();
                await ApplyValueChange(BitNumericTextFieldAction.Increment);

                if (OnIncrement.HasDelegate)
                {
                    await OnIncrement.InvokeAsync(CurrentValue);
                }
                break;

            case "ArrowDown":
                await CheckIntermediateValueAndSetValue();
                await ApplyValueChange(BitNumericTextFieldAction.Decrement);

                if (OnDecrement.HasDelegate)
                {
                    await OnDecrement.InvokeAsync(CurrentValue);
                }
                break;

            case "Enter":
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

    private async Task HandleOnFocusIn(FocusEventArgs e)
    {
        if (IsEnabled is false) return;

        _hasFocus = true;
        ClassBuilder.Reset();
        await _js.SelectText(_inputRef);
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
        await _js.SelectText(_inputRef);
        await OnFocus.InvokeAsync(e);
    }

    private int CalculatePrecision(TValue? value)
    {
        if (value is null) return 0;

        var pattern = _isDecimals ? @"[1-9]([0]+$)|\.([0-9]*)" : @"(^-\d+$)|\d+";
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

        if (value > _internalMax)
        {
            CurrentValue = GetGenericValue(_internalMax.Value);
        }
        else if (value < _internalMin)
        {
            CurrentValue = GetGenericValue(_internalMin.Value);
        }
        else
        {
            CurrentValue = GetGenericValue(value);
        }

        SetDisplayValue();
    }

    private void SetDisplayValue()
    {
        _intermediateValue = CurrentValueAsString;
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

    private double GetMaxValue()
    {
        if (_typeOfValue == typeof(byte))
        {
            return byte.MaxValue;
        }
        else if (_typeOfValue == typeof(sbyte))
        {
            return sbyte.MaxValue;
        }
        else if (_typeOfValue == typeof(short))
        {
            return short.MaxValue;
        }
        else if (_typeOfValue == typeof(ushort))
        {
            return ushort.MaxValue;
        }
        else if (_typeOfValue == typeof(int))
        {
            return int.MaxValue;
        }
        else if (_typeOfValue == typeof(uint))
        {
            return uint.MaxValue;
        }
        else if (_typeOfValue == typeof(long))
        {
            return long.MaxValue;
        }
        else if (_typeOfValue == typeof(ulong))
        {
            return ulong.MaxValue;
        }
        else if (_typeOfValue == typeof(double))
        {
            return double.MaxValue;
        }
        else if (_typeOfValue == typeof(float))
        {
            return float.MaxValue;
        }
        else if (_typeOfValue == typeof(decimal))
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
        if (_typeOfValue == typeof(byte))
        {
            return byte.MinValue;
        }
        else if (_typeOfValue == typeof(sbyte))
        {
            return sbyte.MinValue;
        }
        else if (_typeOfValue == typeof(short))
        {
            return short.MinValue;
        }
        else if (_typeOfValue == typeof(ushort))
        {
            return ushort.MinValue;
        }
        else if (_typeOfValue == typeof(int))
        {
            return int.MinValue;
        }
        else if (_typeOfValue == typeof(uint))
        {
            return uint.MinValue;
        }
        else if (_typeOfValue == typeof(long))
        {
            return long.MinValue;
        }
        else if (_typeOfValue == typeof(ulong))
        {
            return ulong.MinValue;
        }
        else if (_typeOfValue == typeof(double))
        {
            return double.MinValue;
        }
        else if (_typeOfValue == typeof(float))
        {
            return float.MinValue;
        }
        else if (_typeOfValue == typeof(decimal))
        {
            return (double)decimal.MinValue;
        }
        else
        {
            return double.MinValue;
        }
    }

    private double Normalize(double value) => Math.Round(value, _precision);
    private double NormalizeDecimal(decimal value) => Convert.ToDouble(Math.Round(value, _precision));
    private TValue? GetAriaValueNow => AriaValueNow is not null ? AriaValueNow : CurrentValue;
    private string? GetAriaValueText => AriaValueText.HasValue() ? AriaValueText : CurrentValueAsString;
    private string? GetIconRole => IconAriaLabel.HasValue() ? "img" : null;
    private TValue? GetGenericValue(double? value) => value.HasValue ? (TValue)Convert.ChangeType(value, _typeOfValue, CultureInfo.InvariantCulture) : default;
    private double? GetDoubleValueOrDefault(TValue? value, double? defaultValue = null) => value is null ? defaultValue : (double?)Convert.ChangeType(value, typeof(double), CultureInfo.InvariantCulture);

    protected override bool TryParseValueFromString(string? value, [MaybeNullWhen(false)] out TValue? result, [NotNullWhen(false)] out string? validationErrorMessage)
    {
        if (_typeOfValue == typeof(byte))
        {
            if (byte.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsedValue))
            {
                result = GetGenericValue(Normalize(parsedValue));
                validationErrorMessage = null;
                return true;
            }
        }
        else if (_typeOfValue == typeof(sbyte))
        {
            if (sbyte.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsedValue))
            {
                result = GetGenericValue(Normalize(parsedValue));
                validationErrorMessage = null;
                return true;
            }
        }
        else if (_typeOfValue == typeof(short))
        {
            if (short.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsedValue))
            {
                result = GetGenericValue(Normalize(parsedValue));
                validationErrorMessage = null;
                return true;
            }
        }
        else if (_typeOfValue == typeof(ushort))
        {
            if (ushort.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsedValue))
            {
                result = GetGenericValue(Normalize(parsedValue));
                validationErrorMessage = null;
                return true;
            }
        }
        else if (_typeOfValue == typeof(int))
        {
            if (int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsedValue))
            {
                result = GetGenericValue(Normalize(parsedValue));
                validationErrorMessage = null;
                return true;
            }
        }
        else if (_typeOfValue == typeof(uint))
        {
            if (uint.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsedValue))
            {
                result = GetGenericValue(Normalize(parsedValue));
                validationErrorMessage = null;
                return true;
            }
        }
        else if (_typeOfValue == typeof(long))
        {
            if (long.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsedValue))
            {
                result = GetGenericValue(Normalize(parsedValue));
                validationErrorMessage = null;
                return true;
            }
        }
        else if (_typeOfValue == typeof(ulong))
        {
            if (ulong.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsedValue))
            {
                result = GetGenericValue(Normalize(parsedValue));
                validationErrorMessage = null;
                return true;
            }
        }
        else if (_typeOfValue == typeof(double))
        {
            if (double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var parsedValue))
            {
                result = GetGenericValue(Normalize(parsedValue));
                validationErrorMessage = null;
                return true;
            }
        }
        else if (_typeOfValue == typeof(float))
        {
            if (float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var parsedValue))
            {
                result = GetGenericValue(Normalize(parsedValue));
                validationErrorMessage = null;
                return true;
            }
        }
        else if (_typeOfValue == typeof(decimal))
        {
            if (decimal.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var parsedValue))
            {
                result = GetGenericValue(NormalizeDecimal(parsedValue));
                validationErrorMessage = null;
                return true;
            }
        }

        result = default;
        validationErrorMessage = string.Format(CultureInfo.InvariantCulture, ValidationMessage, DisplayName ?? FieldIdentifier.FieldName);
        return false;
    }

    protected override string? FormatValueAsString(TValue? value)
    {
        if (value is null) return null;

        var normalValue = Normalize(GetDoubleValueOrDefault(value)!.Value);
        return string.Format(NumberFormat, normalValue);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _timer?.Dispose();
        }

        base.Dispose(disposing);
    }
}
