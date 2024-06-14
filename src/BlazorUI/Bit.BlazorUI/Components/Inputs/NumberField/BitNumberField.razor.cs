using System.Globalization;
using System.Text.RegularExpressions;
using System.Diagnostics.CodeAnalysis;

namespace Bit.BlazorUI;

public partial class BitNumberField<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] TValue>
{
    private const int INITIAL_STEP_DELAY = 400;
    private const int STEP_DELAY = 75;

    private TValue? step;
    private TValue? min;
    private TValue? max;
    private bool required;
    private bool leftLabel;

    private double? _internalMin;
    private double? _internalMax;
    private double _internalStep = 1;
    private readonly string _labelId;
    private readonly string _inputId;
    private ElementReference _inputRef;
    private ElementReference _buttonIncrement;
    private ElementReference _buttonDecrement;
    private readonly Type _typeOfValue;
    private readonly bool _isNullableType;
    private bool _hasFocus;
    private readonly bool _isDecimals;
    private readonly double _minGenericValue;
    private readonly double _maxGenericValue;
    private CancellationTokenSource _continuousChangeValueCts = new();

    public BitNumberField()
    {
        _typeOfValue = typeof(TValue);
        _isNullableType = Nullable.GetUnderlyingType(_typeOfValue) is not null;
        _typeOfValue = Nullable.GetUnderlyingType(_typeOfValue) ?? _typeOfValue;

        _isDecimals = _typeOfValue == typeof(float) || _typeOfValue == typeof(double) || _typeOfValue == typeof(decimal);
        _minGenericValue = GetMinValue();
        _maxGenericValue = GetMaxValue();
        _inputId = $"BitNumberField-{UniqueId}-input";
        _labelId = $"BitNumberField-{UniqueId}-label";
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
    /// Custom CSS classes for different parts of the BitNumberField.
    /// </summary>
    [Parameter] public BitNumberFieldClassStyles? Classes { get; set; }

    /// <summary>
    /// Accessible label text for the decrement button (for screen reader users).
    /// </summary>
    [Parameter] public string? DecrementAriaLabel { get; set; }

    /// <summary>
    /// Custom icon name for the decrement button.
    /// </summary>
    [Parameter] public string DecrementIconName { get; set; } = "ChevronDownSmall";

    /// <summary>
    /// Initial value of the number field.
    /// </summary>
    [Parameter] public TValue? DefaultValue { get; set; }

    /// <summary>
    /// The aria label of the icon for the benefit of screen readers.
    /// </summary>
    [Parameter] public string IconAriaLabel { get; set; } = string.Empty;

    /// <summary>
    /// Icon name for an icon to display alongside the number field's label.
    /// </summary>
    [Parameter] public string? IconName { get; set; }

    /// <summary>
    /// Accessible label text for the increment button (for screen reader users).
    /// </summary>
    [Parameter] public string? IncrementAriaLabel { get; set; }

    /// <summary>
    /// Custom icon name for the increment button.
    /// </summary>
    [Parameter] public string IncrementIconName { get; set; } = "ChevronUpSmall";

    /// <summary>
    /// Descriptive label for the number field, Label displayed above the number field and read by screen readers.
    /// </summary>
    [Parameter] public string Label { get; set; } = string.Empty;

    /// <summary>
    /// Shows the custom Label for number field. If you don't call default label, ensure that you give your custom label an id and that you set the input's aria-labelledby prop to that id.
    /// </summary>
    [Parameter] public RenderFragment? LabelTemplate { get; set; }

    /// <summary>
    /// The position of the label in regards to the number field.
    /// </summary>
    [Parameter]
    public bool LeftLabel
    {
        get => leftLabel;
        set
        {
            if (leftLabel == value) return;

            leftLabel = value;
            ClassBuilder.Reset();
        }
    }

    /// <summary>
    /// Min value of the number field. If not provided, the number field has minimum value.
    /// </summary>
    [Parameter]
    public TValue? Min
    {
        get => min;
        set
        {
            min = value;
            _internalMin = ConvertToDouble(value);
        }
    }

    /// <summary>
    /// Max value of the number field. If not provided, the number field has max value.
    /// </summary>
    [Parameter]
    public TValue? Max
    {
        get => max;
        set
        {
            max = value;
            _internalMax = ConvertToDouble(value);
        }
    }

    /// <summary>
    /// The format of the number in the number field.
    /// </summary>
    [Parameter] public string? NumberFormat { get; set; }

    /// <summary>
    /// Callback for when the control loses focus.
    /// </summary>
    [Parameter] public EventCallback<FocusEventArgs> OnBlur { get; set; }

    /// <summary>
    /// Callback for when the decrement button or down arrow key is pressed.
    /// </summary>
    [Parameter] public EventCallback<TValue> OnDecrement { get; set; }

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
    /// Callback for when the increment button or up arrow key is pressed.
    /// </summary>
    [Parameter] public EventCallback<TValue> OnIncrement { get; set; }

    /// <summary>
    /// The message format used for invalid values entered in the input.
    /// </summary>
    [Parameter] public string ParsingErrorMessage { get; set; } = "The {0} field is not valid.";

    /// <summary>
    /// Input placeholder text.
    /// </summary>
    [Parameter] public string? Placeholder { get; set; }

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

    /// <summary>
    /// Whether to show the increment and decrement buttons.
    /// </summary>
    [Parameter] public bool ShowButtons { get; set; }

    /// <summary>
    /// Difference between two adjacent values of the number field.
    /// </summary>
    [Parameter]
    public TValue? Step
    {
        get => step;
        set
        {
            step = value;
            _internalStep = ConvertToDouble(value) ?? 1;
        }
    }

    /// <summary>
    /// Custom CSS styles for different parts of the BitNumberField.
    /// </summary>
    [Parameter] public BitNumberFieldClassStyles? Styles { get; set; }

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
    /// A more descriptive title for the control, visible on its tooltip.
    /// </summary>
    [Parameter] public string? Title { get; set; }



    /// <summary>
    /// The ElementReference to the input element of the BitNumberField.
    /// </summary>
    public ElementReference InputElement => _inputRef;

    /// <summary>
    /// Gives focus to the input element of the BitNumberField.
    /// </summary>
    public ValueTask FocusAsync() => _inputRef.FocusAsync();



    protected override string RootElementClass => "bit-nfl";

    protected override void RegisterCssClasses()
    {
        ClassBuilder.Register(() => Classes?.Root);

        ClassBuilder.Register(() => _hasFocus ? $"{RootElementClass}-fcs {Classes?.Focused}" : string.Empty);

        ClassBuilder.Register(() => $"{RootElementClass}-{(LeftLabel ? "llf" : "ltp")}");

        ClassBuilder.Register(() => IsEnabled && Required ? $"{RootElementClass}-req" : string.Empty);

        ClassBuilder.Register(() => IsEnabled && Required && Label.HasNoValue() ? $"{RootElementClass}-rnl" : string.Empty);
    }

    protected override void RegisterCssStyles()
    {
        StyleBuilder.Register(() => Styles?.Root);

        StyleBuilder.Register(() => _hasFocus ? Styles?.Focused : string.Empty);
    }

    protected override Task OnInitializedAsync()
    {
        if ((ValueHasBeenSet is false || CurrentValue is null) && DefaultValue is not null)
        {
            InitCurrentValue(DefaultValue);
        }

        return base.OnInitializedAsync();
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

        await base.OnParametersSetAsync();
    }



    private async Task HandleOnKeyDown(KeyboardEventArgs e)
    {
        if (IsEnabled is false) return;
        if (ValueHasBeenSet && ValueChanged.HasDelegate is false) return;

        switch (e.Key)
        {
            case "ArrowUp":
                ChangeValue(true);

                if (OnIncrement.HasDelegate)
                {
                    await OnIncrement.InvokeAsync(CurrentValue);
                }
                break;

            case "ArrowDown":
                ChangeValue(false);

                if (OnDecrement.HasDelegate)
                {
                    await OnDecrement.InvokeAsync(CurrentValue);
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

    private async Task HandleOnInputChange(ChangeEventArgs e)
    {
        var value = e.Value?.ToString();

        CurrentValueAsString = value;
    }

    private async Task HandleOnPointerDown(bool isIncrement)
    {
        if (IsEnabled is false) return;
        if (ValueHasBeenSet && ValueChanged.HasDelegate is false) return;

        //Change focus from input to number field
        if (isIncrement)
        {
            await _buttonIncrement.FocusAsync();
        }
        else
        {
            await _buttonDecrement.FocusAsync();
        }

        await ChangeValueAndInvokeEvents(isIncrement);
        ResetCts();

        var cts = _continuousChangeValueCts;
        await Task.Run(async () =>
        {
            await InvokeAsync(async () =>
            {
                await Task.Delay(INITIAL_STEP_DELAY);
                await ContinuousChangeValue(isIncrement, cts);
            });
        }, cts.Token);
    }

    private async Task HandleOnPointerUpOrOut()
    {
        ResetCts();
    }



    private async Task ContinuousChangeValue(bool isIncrement, CancellationTokenSource cts)
    {
        if (cts.IsCancellationRequested) return;

        await ChangeValueAndInvokeEvents(isIncrement);

        StateHasChanged();

        await Task.Delay(STEP_DELAY);
        await ContinuousChangeValue(isIncrement, cts);
    }

    private async Task ChangeValueAndInvokeEvents(bool isIncrement)
    {
        ChangeValue(isIncrement);

        if (isIncrement && OnIncrement.HasDelegate)
        {
            await OnIncrement.InvokeAsync(CurrentValue);
        }

        if (isIncrement is false && OnDecrement.HasDelegate)
        {
            await OnDecrement.InvokeAsync(CurrentValue);
        }
    }

    private void ChangeValue(bool isIncrement)
    {
        bool isValid;
        double result;

        var value1 = ConvertToDouble(CurrentValue, 0d)!.Value;

        if (isIncrement)
        {
            result = value1 + _internalStep;
            isValid = result <= _internalMax && result >= _internalMin;
        }
        else
        {
            result = value1 - _internalStep;
            isValid = result <= _internalMax && result >= _internalMin;
        }

        if (isValid is false) return;

        if (result > _internalMax)
        {
            result = _internalMax.Value;
        }

        if (result < _internalMin)
        {
            result = _internalMin.Value;
        }

        CurrentValue = ConvertToGeneric(result);

        StateHasChanged();
    }

    private void ResetCts()
    {
        _continuousChangeValueCts.Cancel();
        _continuousChangeValueCts.Dispose();
        _continuousChangeValueCts = new();
    }

    private double GetMaxValue()
    {
        return _typeOfValue == typeof(byte) ? byte.MaxValue
             : _typeOfValue == typeof(sbyte) ? sbyte.MaxValue
             : _typeOfValue == typeof(short) ? short.MaxValue
             : _typeOfValue == typeof(ushort) ? ushort.MaxValue
             : _typeOfValue == typeof(int) ? int.MaxValue
             : _typeOfValue == typeof(uint) ? uint.MaxValue
             : _typeOfValue == typeof(long) ? long.MaxValue
             : _typeOfValue == typeof(ulong) ? ulong.MaxValue
             : _typeOfValue == typeof(float) ? float.MaxValue
             : _typeOfValue == typeof(decimal) ? (double)decimal.MaxValue
             : _typeOfValue == typeof(double) ? double.MaxValue
             : double.MaxValue;
    }

    private double GetMinValue()
    {
        return _typeOfValue == typeof(byte) ? byte.MinValue
             : _typeOfValue == typeof(sbyte) ? sbyte.MinValue
             : _typeOfValue == typeof(short) ? short.MinValue
             : _typeOfValue == typeof(ushort) ? ushort.MinValue
             : _typeOfValue == typeof(int) ? int.MinValue
             : _typeOfValue == typeof(uint) ? uint.MinValue
             : _typeOfValue == typeof(long) ? long.MinValue
             : _typeOfValue == typeof(ulong) ? ulong.MinValue
             : _typeOfValue == typeof(float) ? float.MinValue
             : _typeOfValue == typeof(decimal) ? (double)decimal.MinValue
             : _typeOfValue == typeof(double) ? double.MinValue
             : double.MinValue;
    }

    private TValue? GetAriaValueNow => AriaValueNow is not null ? AriaValueNow : CurrentValue;

    private string? GetAriaValueText => AriaValueText.HasValue() ? AriaValueText : CurrentValueAsString;

    private string? GetIconRole => IconAriaLabel.HasValue() ? "img" : null;

    private TValue? ConvertToGeneric(double? value)
    {
        return value.HasValue
            ? (TValue)Convert.ChangeType(value, _typeOfValue, CultureInfo.InvariantCulture)
            : default;
    }

    private double? ConvertToDouble(TValue? value, double? defaultValue = null)
    {
        return value is null
            ? defaultValue
            : (double?)Convert.ChangeType(value, typeof(double), CultureInfo.InvariantCulture);
    }

    protected override bool TryParseValueFromString(string? value, [MaybeNullWhen(false)] out TValue? result, [NotNullWhen(false)] out string? parsingErrorMessage)
    {
        if (NumberFormat is not null)
        {
            value = CleanValue(value);
        }

        if (BindConverter.TryConvertTo(value, CultureInfo.InvariantCulture, out result))
        {
            var v = ConvertToDouble(result);
            v = v < _internalMin ? _internalMin : v;
            v = v > _internalMax ? _internalMax : v;
            result = ConvertToGeneric(v);
            parsingErrorMessage = null;
            return true;
        }
        else
        {
            parsingErrorMessage = string.Format(CultureInfo.InvariantCulture, ParsingErrorMessage, DisplayName ?? FieldIdentifier.FieldName);
            return false;
        }
    }

    private static string? CleanValue(string? value)
    {
        if (value.HasNoValue()) return null;

        var pattern = new Regex(@"-?\d*(?:\.\d*)?");
        var matchCollection = pattern.Matches(value!);

        return matchCollection is null ? value : string.Join("", matchCollection.Select(m => m.Value));
    }

    protected override string? FormatValueAsString(TValue? value)
    {
        if (value is null) return null;
        if (NumberFormat is null) return value.ToString();

        var normalValue = ConvertToDouble(value)!.Value;
        return normalValue.ToString(NumberFormat!);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _continuousChangeValueCts.Dispose();
        }

        base.Dispose(disposing);
    }
}
