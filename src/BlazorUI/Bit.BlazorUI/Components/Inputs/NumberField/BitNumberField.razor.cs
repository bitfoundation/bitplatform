﻿using System.Globalization;
using System.Text.RegularExpressions;
using System.Diagnostics.CodeAnalysis;

namespace Bit.BlazorUI;

/// <summary>
/// A NumberField allows you to enter any number type and format you want. It could be a decimal number or integer number with a suffix and so on.
/// </summary>
public partial class BitNumberField<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] TValue> : BitTextInputBase<TValue>
{
    private int _precision;
    private bool _hasFocus;
    private string? _tempValue;
    private TValue _min = default!;
    private TValue _max = default!;
    private TValue _step = default!;
    private readonly string _labelId;
    private readonly string _inputId;
    private readonly string _inputMode;
    private readonly Type _typeOfValue;
    private readonly TValue _zeroValue;
    private ElementReference _buttonIncrement;
    private ElementReference _buttonDecrement;
    private CancellationTokenSource _continuousChangeValueCts = new();



    public BitNumberField()
    {
        BindConverter.TryConvertTo("1", CultureInfo.InvariantCulture, out _step!);
        BindConverter.TryConvertTo("0", CultureInfo.InvariantCulture, out _zeroValue!);

        _typeOfValue = typeof(TValue);
        _typeOfValue = Nullable.GetUnderlyingType(_typeOfValue) ?? _typeOfValue;

        _min = GetTypeMinValue();
        _max = GetTypeMaxValue();

        _inputId = $"BitNumberField-{UniqueId}-input";
        _labelId = $"BitNumberField-{UniqueId}-label";

        _inputMode = (_typeOfValue == typeof(decimal) || _typeOfValue == typeof(double) || _typeOfValue == typeof(float)) ? "decimal" : "numeric";
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
    [Parameter] public string? DecrementIconName { get; set; }

    /// <summary>
    /// The title to show when the mouse is placed on the decrement button.
    /// </summary>
    [Parameter] public string? DecrementTitle { get; set; }

    /// <summary>
    /// Initial value of the number field.
    /// </summary>
    [Parameter] public TValue? DefaultValue { get; set; }

    /// <summary>
    /// If true, the input is hidden.
    /// </summary>
    [Parameter] public bool HideInput { get; set; }

    /// <summary>
    /// The aria label of the icon for the benefit of screen readers.
    /// </summary>
    [Parameter] public string? IconAriaLabel { get; set; }

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
    [Parameter] public string? IncrementIconName { get; set; }

    /// <summary>
    /// The title to show when the mouse is placed on the increment button.
    /// </summary>
    [Parameter] public string? IncrementTitle { get; set; }

    /// <summary>
    /// Reverses the mouse wheel direction.
    /// </summary>
    [Parameter] public bool InvertMouseWheel { get; set; }

    /// <summary>
    /// If true, the input is readonly.
    /// </summary>
    [Parameter] public bool IsInputReadOnly { get; set; }

    /// <summary>
    /// The position of the label in regards to the spin button.
    /// </summary>
    [Parameter, ResetClassBuilder]
    public BitLabelPosition? LabelPosition { get; set; }

    /// <summary>
    /// Descriptive label for the number field, Label displayed above the number field and read by screen readers.
    /// </summary>
    [Parameter, ResetClassBuilder]
    public string? Label { get; set; }

    /// <summary>
    /// Shows the custom Label for number field. If you don't call default label, ensure that you give your custom label an id and that you set the input's aria-labelledby prop to that id.
    /// </summary>
    [Parameter] public RenderFragment? LabelTemplate { get; set; }

    /// <summary>
    /// Min value of the number field.
    /// </summary>
    [Parameter]
    [CallOnSet(nameof(OnSetMin))]
    public string? Min { get; set; }

    /// <summary>
    /// Max value of the number field.
    /// </summary>
    [Parameter]
    [CallOnSet(nameof(OnSetMax))]
    public string? Max { get; set; }

    /// <summary>
    /// Determines how the spinning buttons should be rendered.
    /// </summary>
    [Parameter] public BitSpinButtonMode? Mode { get; set; }

    /// <summary>
    /// The format of the number in the number field.
    /// </summary>
    [Parameter] public string? NumberFormat { get; set; }

    /// <summary>
    /// Callback for when the control loses focus.
    /// </summary>
    [Parameter] public EventCallback<FocusEventArgs> OnBlur { get; set; }

    /// <summary>
    /// Callback executed when the user clears the number field by either clicking 'X' or hitting escape.
    /// </summary>
    [Parameter] public EventCallback OnClear { get; set; }

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
    /// How many decimal places the value should be rounded to.
    /// </summary>
    [Parameter]
    [CallOnSet(nameof(OnSetPrecision))]
    public int? Precision { get; set; }

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
    /// Whether to shows the clear button when the BitNumberField has value.
    /// </summary>
    [Parameter] public bool ShowClearButton { get; set; }

    /// <summary>
    /// Difference between two adjacent values of the number field.
    /// </summary>
    [Parameter]
    [CallOnSet(nameof(OnSetStep))]
    public string? Step { get; set; }

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



    protected override string RootElementClass => "bit-nfl";

    protected override void RegisterCssClasses()
    {
        ClassBuilder.Register(() => Classes?.Root);

        ClassBuilder.Register(() => _hasFocus ? $"bit-nfl-fcs {Classes?.Focused}" : string.Empty);

        ClassBuilder.Register(() => LabelPosition switch
        {
            BitLabelPosition.Bottom => "bit-nfl-lbt",
            BitLabelPosition.Start => "bit-nfl-lst",
            BitLabelPosition.End => "bit-nfl-led",
            _ => "bit-nfl-ltp"
        });

        ClassBuilder.Register(() => IsEnabled && Required ? "bit-nfl-req" : string.Empty);

        ClassBuilder.Register(() => IsEnabled && Required && Label.HasNoValue() ? "bit-nfl-rnl" : string.Empty);
    }

    protected override void RegisterCssStyles()
    {
        StyleBuilder.Register(() => Styles?.Root);

        StyleBuilder.Register(() => _hasFocus ? Styles?.Focused : string.Empty);
    }

    protected override async Task OnInitializedAsync()
    {
        OnValueChanged += HandleOnValueChanged;

        if (ValueHasBeenSet is false && DefaultValue is not null)
        {
            Value = DefaultValue;
        }

        NormalizeValue();

        await base.OnInitializedAsync();
    }

    protected override bool TryParseValueFromString(string? value, [MaybeNullWhen(false)] out TValue result, [NotNullWhen(false)] out string? parsingErrorMessage)
    {
        if (NumberFormat is not null)
        {
            value = CleanValue(value);
        }

        if (BindConverter.TryConvertTo(value, CultureInfo.InvariantCulture, out result))
        {
            result = CheckMinAndMax(result);

            result = Normalize(result);

            parsingErrorMessage = null;
            return true;
        }
        else
        {
            parsingErrorMessage = string.Format(CultureInfo.InvariantCulture, ParsingErrorMessage, DisplayName ?? FieldIdentifier.FieldName);
            return false;
        }
    }

    protected override string? FormatValueAsString(TValue? value)
    {
        if (value is null) return null;
        if (NumberFormat is null) return value.ToString();

        return _typeOfValue == typeof(byte) ? Convert.ToByte(value).ToString(NumberFormat)
             : _typeOfValue == typeof(sbyte) ? Convert.ToSByte(value).ToString(NumberFormat)
             : _typeOfValue == typeof(short) ? Convert.ToInt16(value).ToString(NumberFormat)
             : _typeOfValue == typeof(ushort) ? Convert.ToUInt16(value).ToString(NumberFormat)
             : _typeOfValue == typeof(int) ? Convert.ToInt32(value).ToString(NumberFormat)
             : _typeOfValue == typeof(uint) ? Convert.ToUInt32(value).ToString(NumberFormat)
             : _typeOfValue == typeof(long) ? Convert.ToInt64(value).ToString(NumberFormat)
             : _typeOfValue == typeof(ulong) ? Convert.ToUInt64(value).ToString(NumberFormat)
             : _typeOfValue == typeof(float) ? Convert.ToSingle(value).ToString(NumberFormat)
             : _typeOfValue == typeof(decimal) ? Convert.ToDecimal(value).ToString(NumberFormat)
             : _typeOfValue == typeof(double) ? Convert.ToDouble(value).ToString(NumberFormat)
             : "0";
    }

    protected override Task HandleOnStringValueChangeAsync(ChangeEventArgs e)
    {
        _tempValue = e.Value?.ToString();

        return base.HandleOnStringValueChangeAsync(e);
    }



    private async Task HandleOnKeyDown(KeyboardEventArgs e)
    {
        if (IsEnabled is false || ReadOnly || InvalidValueBinding()) return;

        switch (e.Key)
        {
            case "ArrowUp":
                ChangeValue(+1);

                if (OnIncrement.HasDelegate)
                {
                    await OnIncrement.InvokeAsync(CurrentValue);
                }
                break;

            case "ArrowDown":
                ChangeValue(-1);

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
        StyleBuilder.Reset();
        await _js.BitUtilsSelectText(InputElement);
        await OnFocusIn.InvokeAsync(e);
    }

    private async Task HandleOnFocusOut(FocusEventArgs e)
    {
        if (IsEnabled is false) return;

        _hasFocus = false;
        ClassBuilder.Reset();
        StyleBuilder.Reset();
        await OnFocusOut.InvokeAsync(e);
    }

    private async Task HandleOnFocus(FocusEventArgs e)
    {
        if (IsEnabled is false) return;

        _hasFocus = true;
        ClassBuilder.Reset();
        StyleBuilder.Reset();
        await _js.BitUtilsSelectText(InputElement);
        await OnFocus.InvokeAsync(e);
    }

    private async Task HandleOnPointerDown(bool isIncrement)
    {
        if (IsEnabled is false || ReadOnly || InvalidValueBinding()) return;

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
                await Task.Delay(400);
                await ContinuousChangeValue(isIncrement, cts);
            });
        }, cts.Token);
    }

    private async Task HandleOnPointerUpOrOut()
    {
        ResetCts();
    }

    private async Task HandleOnMouseWheel(WheelEventArgs e)
    {
        if (IsEnabled is false || ReadOnly) return;
        if (e.ShiftKey is false) return;

        if (e.DeltaY < 0)
        {
            ChangeValue(InvertMouseWheel ? -1 : +1);
        }
        else if (e.DeltaY > 0)
        {
            ChangeValue(InvertMouseWheel ? +1 : -1);
        }
    }

    private async Task HandleOnClearButtonClick()
    {
        if (IsEnabled is false || ReadOnly) return;

        await HandleOnStringValueChangeAsync(new() { Value = string.Empty });

        await InputElement.FocusAsync();

        await OnClear.InvokeAsync();
    }



    private async Task ContinuousChangeValue(bool isIncrement, CancellationTokenSource cts)
    {
        if (cts.IsCancellationRequested) return;

        await ChangeValueAndInvokeEvents(isIncrement);

        StateHasChanged();

        await Task.Delay(75);
        await ContinuousChangeValue(isIncrement, cts);
    }

    private async Task ChangeValueAndInvokeEvents(bool isIncrement)
    {
        ChangeValue(isIncrement ? +1 : -1);

        if (isIncrement && OnIncrement.HasDelegate)
        {
            await OnIncrement.InvokeAsync(CurrentValue);
        }

        if (isIncrement is false && OnDecrement.HasDelegate)
        {
            await OnDecrement.InvokeAsync(CurrentValue);
        }
    }

    private void ChangeValue(int factor)
    {
        TValue result;

        if (_typeOfValue == typeof(ushort))
        {
            var r = factor > 0
                        ? (Convert.ToInt16(CurrentValue) + Convert.ToInt16(_step))
                        : (Convert.ToInt16(CurrentValue) - Convert.ToInt16(_step));
            result = (TValue)(object)Convert.ToUInt16(r < 0 ? 0 : r);
        }
        else if (_typeOfValue == typeof(uint))
        {
            var r = factor > 0
                        ? (Convert.ToInt32(CurrentValue) + Convert.ToInt32(_step))
                        : (Convert.ToInt32(CurrentValue) - Convert.ToInt32(_step));
            result = (TValue)(object)Convert.ToUInt32(r < 0 ? 0 : r);
        }
        else if (_typeOfValue == typeof(ulong))
        {
            var r = factor > 0
                        ? (Convert.ToInt64(CurrentValue) + Convert.ToInt64(_step))
                        : (Convert.ToInt64(CurrentValue) - Convert.ToInt64(_step));
            result = (TValue)(object)Convert.ToUInt64(r < 0 ? 0 : r);
        }
        else
        {
            result = _typeOfValue == typeof(byte) ? (TValue)(object)(Convert.ToByte(CurrentValue) + (Convert.ToByte(factor) * Convert.ToByte(_step)))
                   : _typeOfValue == typeof(sbyte) ? (TValue)(object)(Convert.ToSByte(CurrentValue) + (Convert.ToSByte(factor) * Convert.ToSByte(_step)))
                   : _typeOfValue == typeof(short) ? (TValue)(object)(Convert.ToInt16(CurrentValue) + (Convert.ToInt16(factor) * Convert.ToInt16(_step)))
                   : _typeOfValue == typeof(int) ? (TValue)(object)(Convert.ToInt32(CurrentValue) + (Convert.ToInt32(factor) * Convert.ToInt32(_step)))
                   : _typeOfValue == typeof(long) ? (TValue)(object)(Convert.ToInt64(CurrentValue) + (Convert.ToInt64(factor) * Convert.ToInt64(_step)))
                   : _typeOfValue == typeof(float) ? (TValue)(object)(Convert.ToSingle(CurrentValue) + (Convert.ToSingle(factor) * Convert.ToSingle(_step)))
                   : _typeOfValue == typeof(decimal) ? (TValue)(object)(Convert.ToDecimal(CurrentValue) + (Convert.ToDecimal(factor) * Convert.ToDecimal(_step)))
                   : _typeOfValue == typeof(double) ? (TValue)(object)(Convert.ToDouble(CurrentValue) + (Convert.ToDouble(factor) * Convert.ToDouble(_step)))
                   : _zeroValue;
        }

        result = CheckMinAndMax(result);

        CurrentValue = result;

        StateHasChanged();
    }

    private void ResetCts()
    {
        _continuousChangeValueCts?.Cancel();
        _continuousChangeValueCts?.Dispose();
        _continuousChangeValueCts = new();
    }

    private TValue GetTypeMaxValue()
    {
        return _typeOfValue == typeof(byte) ? (TValue)(object)byte.MaxValue
             : _typeOfValue == typeof(sbyte) ? (TValue)(object)sbyte.MaxValue
             : _typeOfValue == typeof(short) ? (TValue)(object)short.MaxValue
             : _typeOfValue == typeof(ushort) ? (TValue)(object)ushort.MaxValue
             : _typeOfValue == typeof(int) ? (TValue)(object)int.MaxValue
             : _typeOfValue == typeof(uint) ? (TValue)(object)uint.MaxValue
             : _typeOfValue == typeof(long) ? (TValue)(object)long.MaxValue
             : _typeOfValue == typeof(ulong) ? (TValue)(object)ulong.MaxValue
             : _typeOfValue == typeof(float) ? (TValue)(object)float.MaxValue
             : _typeOfValue == typeof(decimal) ? (TValue)(object)decimal.MaxValue
             : _typeOfValue == typeof(double) ? (TValue)(object)double.MaxValue
             : _zeroValue;
    }

    private TValue GetTypeMinValue()
    {
        return _typeOfValue == typeof(byte) ? (TValue)(object)byte.MinValue
             : _typeOfValue == typeof(sbyte) ? (TValue)(object)sbyte.MinValue
             : _typeOfValue == typeof(short) ? (TValue)(object)short.MinValue
             : _typeOfValue == typeof(ushort) ? (TValue)(object)ushort.MinValue
             : _typeOfValue == typeof(int) ? (TValue)(object)int.MinValue
             : _typeOfValue == typeof(uint) ? (TValue)(object)uint.MinValue
             : _typeOfValue == typeof(long) ? (TValue)(object)long.MinValue
             : _typeOfValue == typeof(ulong) ? (TValue)(object)ulong.MinValue
             : _typeOfValue == typeof(float) ? (TValue)(object)float.MinValue
             : _typeOfValue == typeof(decimal) ? (TValue)(object)decimal.MinValue
             : _typeOfValue == typeof(double) ? (TValue)(object)double.MinValue
             : _zeroValue;
    }

    private TValue CheckMinAndMax(TValue result)
    {
        return _typeOfValue == typeof(byte) ? Convert.ToByte(result) < Convert.ToByte(_min) ? _min : Convert.ToByte(result) > Convert.ToByte(_max) ? _max : result
             : _typeOfValue == typeof(sbyte) ? Convert.ToSByte(result) < Convert.ToSByte(_min) ? _min : Convert.ToSByte(result) > Convert.ToSByte(_max) ? _max : result
             : _typeOfValue == typeof(short) ? Convert.ToInt16(result) < Convert.ToInt16(_min) ? _min : Convert.ToInt16(result) > Convert.ToInt16(_max) ? _max : result
             : _typeOfValue == typeof(ushort) ? Convert.ToUInt16(result) < Convert.ToUInt16(_min) ? _min : Convert.ToUInt16(result) > Convert.ToUInt16(_max) ? _max : result
             : _typeOfValue == typeof(int) ? Convert.ToInt32(result) < Convert.ToInt32(_min) ? _min : Convert.ToInt32(result) > Convert.ToInt32(_max) ? _max : result
             : _typeOfValue == typeof(uint) ? Convert.ToUInt32(result) < Convert.ToUInt32(_min) ? _min : Convert.ToUInt32(result) > Convert.ToUInt32(_max) ? _max : result
             : _typeOfValue == typeof(long) ? Convert.ToInt64(result) < Convert.ToInt64(_min) ? _min : Convert.ToInt64(result) > Convert.ToInt64(_max) ? _max : result
             : _typeOfValue == typeof(ulong) ? Convert.ToUInt64(result) < Convert.ToUInt64(_min) ? _min : Convert.ToUInt64(result) > Convert.ToUInt64(_max) ? _max : result
             : _typeOfValue == typeof(float) ? Convert.ToSingle(result) < Convert.ToSingle(_min) ? _min : Convert.ToSingle(result) > Convert.ToSingle(_max) ? _max : result
             : _typeOfValue == typeof(decimal) ? Convert.ToDecimal(result) < Convert.ToDecimal(_min) ? _min : Convert.ToDecimal(result) > Convert.ToDecimal(_max) ? _max : result
             : _typeOfValue == typeof(double) ? Convert.ToDouble(result) < Convert.ToDouble(_min) ? _min : Convert.ToDouble(result) > Convert.ToDouble(_max) ? _max : result
             : _zeroValue;
    }

    private static string? CleanValue(string? value)
    {
        if (value.HasNoValue()) return null;

        var pattern = new Regex(@"-?\d*(?:\.\d*)?");
        var matchCollection = pattern.Matches(value!);

        return matchCollection is null ? value : string.Join("", matchCollection.Select(m => m.Value));
    }

    private void OnSetMin()
    {
        var min = CleanValue(Min);
        if (BindConverter.TryConvertTo(min, CultureInfo.InvariantCulture, out TValue? result))
        {
            _min = result ?? GetTypeMinValue();
        }
        else
        {
            _min = GetTypeMinValue();
        }
    }

    private void OnSetMax()
    {
        var max = CleanValue(Max);
        if (BindConverter.TryConvertTo(max, CultureInfo.InvariantCulture, out TValue? result))
        {
            _max = result ?? GetTypeMaxValue();
        }
        else
        {
            _max = GetTypeMaxValue();
        }
    }

    private void OnSetStep()
    {
        var step = CleanValue(Step);
        if (BindConverter.TryConvertTo(step, CultureInfo.InvariantCulture, out TValue? result))
        {
            _step = result ?? ((TValue)(object)1);
        }
        else
        {
            _step = (TValue)(object)1;
        }
    }

    private void OnSetPrecision()
    {
        _precision = Precision is not null ? Precision.Value : CalculatePrecision();
    }

    private TValue Normalize(TValue value)
    {
        if (value is double doubleValue)
        {
            return (TValue)Convert.ChangeType(Math.Round(doubleValue, _precision), typeof(TValue));
        }
        else if (value is float floatValue)
        {
            return (TValue)Convert.ChangeType(Math.Round(floatValue, _precision), typeof(TValue));
        }
        else if (value is decimal decimalValue)
        {
            return (TValue)Convert.ChangeType(Math.Round(decimalValue, _precision), typeof(TValue));
        }

        return value;
    }

    private int CalculatePrecision()
    {
        var step = Step ?? _step?.ToString() ?? "1";
        var regex = new Regex(@"[1-9]([0]+$)|\.([0-9]*)");
        if (regex.IsMatch(step) is false) return 0;

        var matches = regex.Matches(step);
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

    private void NormalizeValue()
    {
        if (Value is null) return;

        var val = Normalize(Value);

        if (EqualityComparer<TValue>.Default.Equals(val, Value)) return;

        Value = val;
    }

    private void HandleOnValueChanged(object? sender, EventArgs args)
    {
        NormalizeValue();
    }



    protected override async ValueTask DisposeAsync(bool disposing)
    {
        if (IsDisposed || disposing is false) return;

        OnValueChanged -= HandleOnValueChanged;

        _continuousChangeValueCts?.Dispose();

        await base.DisposeAsync(disposing);
    }
}
