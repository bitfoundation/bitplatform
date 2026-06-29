using System.Globalization;
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
    private string? _displayValue;
    private TValue? _displayValueSource;
    private bool _keepDisplayValueOnNextChange;
    private bool _lastNormalizationActive;
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
    /// Gets or sets the icon to display on the decrement button using custom CSS classes for external icon libraries.
    /// Takes precedence over <see cref="DecrementIconName"/> when both are set.
    /// </summary>
    /// <remarks>
    /// Use this property to render icons from external libraries like FontAwesome, Material Icons, or Bootstrap Icons.
    /// For built-in Fluent UI icons, use <see cref="DecrementIconName"/> instead.
    /// </remarks>
    /// <example>
    /// Bootstrap: DecrementIcon="BitIconInfo.Bi("dash")"
    /// FontAwesome: DecrementIcon="BitIconInfo.Fa("solid minus")"
    /// Custom CSS: DecrementIcon="BitIconInfo.Css("my-icon-class")"
    /// </example>
    [Parameter] public BitIconInfo? DecrementIcon { get; set; }

    /// <summary>
    /// Gets or sets the name of the icon for the decrement button from the built-in Fluent UI icons.
    /// For external icon libraries, use <see cref="DecrementIcon"/> instead.
    /// </summary>
    [Parameter] public string? DecrementIconName { get; set; }

    /// <summary>
    /// The title to show when the mouse is placed on the decrement button.
    /// </summary>
    [Parameter] public string? DecrementTitle { get; set; }

    /// <summary>
    /// A custom function to normalize the raw input string before it gets parsed into the value.
    /// When provided, it takes precedence over <see cref="NormalizeDigits"/> and lets the developer plug in their own
    /// culture-specific or domain-specific transformation (e.g. mapping characters from a particular keyboard layout).
    /// Note that, like <see cref="NormalizeDigits"/>, this function is also applied to the <see cref="Min"/>, <see cref="Max"/>
    /// and <see cref="Step"/> parameters (and to the precision derived from <see cref="Step"/>), not only to user input, so it
    /// affects range/step semantics as well. The original typed text is only kept visible in the input when it is digit-equivalent
    /// to the resulting value (i.e. a pure non-Latin rendering of the same number); transformations that strip units, symbols or
    /// aliases will display the canonical value instead.
    /// </summary>
    [Parameter] public Func<string?, string?>? DigitsNormalizer { get; set; }

    /// <summary>
    /// If true, the input is hidden.
    /// </summary>
    [Parameter] public bool HideInput { get; set; }

    /// <summary>
    /// The aria label of the icon for the benefit of screen readers.
    /// </summary>
    [Parameter] public string? IconAriaLabel { get; set; }

    /// <summary>
    /// Gets or sets the icon to display alongside the number field using custom CSS classes for external icon libraries.
    /// Takes precedence over <see cref="IconName"/> when both are set.
    /// </summary>
    /// <remarks>
    /// Use this property to render icons from external libraries like FontAwesome, Material Icons, or Bootstrap Icons.
    /// For built-in Fluent UI icons, use <see cref="IconName"/> instead.
    /// </remarks>
    /// <example>
    /// Bootstrap: Icon="BitIconInfo.Bi("calculator")"
    /// FontAwesome: Icon="BitIconInfo.Fa("solid calculator")"
    /// Custom CSS: Icon="BitIconInfo.Css("my-icon-class")"
    /// </example>
    [Parameter] public BitIconInfo? Icon { get; set; }

    /// <summary>
    /// Gets or sets the name of the icon to display alongside the number field from the built-in Fluent UI icons.
    /// For external icon libraries, use <see cref="Icon"/> instead.
    /// </summary>
    [Parameter] public string? IconName { get; set; }

    /// <summary>
    /// Accessible label text for the increment button (for screen reader users).
    /// </summary>
    [Parameter] public string? IncrementAriaLabel { get; set; }

    /// <summary>
    /// Gets or sets the icon to display on the increment button using custom CSS classes for external icon libraries.
    /// Takes precedence over <see cref="IncrementIconName"/> when both are set.
    /// </summary>
    /// <remarks>
    /// Use this property to render icons from external libraries like FontAwesome, Material Icons, or Bootstrap Icons.
    /// For built-in Fluent UI icons, use <see cref="IncrementIconName"/> instead.
    /// </remarks>
    /// <example>
    /// Bootstrap: IncrementIcon="BitIconInfo.Bi("plus")"
    /// FontAwesome: IncrementIcon="BitIconInfo.Fa("solid plus")"
    /// Custom CSS: IncrementIcon="BitIconInfo.Css("my-icon-class")"
    /// </example>
    [Parameter] public BitIconInfo? IncrementIcon { get; set; }

    /// <summary>
    /// Gets or sets the name of the icon for the increment button from the built-in Fluent UI icons.
    /// For external icon libraries, use <see cref="IncrementIcon"/> instead.
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
    /// Normalizes non-Latin (e.g. Persian "۱۲۳" or Arabic "١٢٣") decimal digits to their Latin (0-9) equivalents before parsing.
    /// This is culture-agnostic and works for any Unicode decimal digit system, including digits in the supplementary planes
    /// (surrogate pairs). The Arabic decimal separator (U+066B) is mapped to '.', and the Arabic thousands separator (U+066C) is stripped.
    /// The same normalization is also applied to the <see cref="Min"/>, <see cref="Max"/> and <see cref="Step"/> parameters so that
    /// non-Latin constraints (e.g. <c>Min="۱۰"</c>) are parsed consistently with user input.
    /// </summary>
    [Parameter] public bool NormalizeDigits { get; set; }

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
    /// Gets or sets the icon to display on the clear button using custom CSS classes for external icon libraries.
    /// Takes precedence over <see cref="ClearButtonIconName"/> when both are set.
    /// </summary>
    /// <remarks>
    /// Use this property to render icons from external libraries like FontAwesome, Material Icons, or Bootstrap Icons.
    /// For built-in Fluent UI icons, use <see cref="ClearButtonIconName"/> instead.
    /// </remarks>
    /// <example>
    /// Bootstrap: ClearButtonIcon="BitIconInfo.Bi("x-circle-fill")"
    /// FontAwesome: ClearButtonIcon="BitIconInfo.Fa("solid xmark")"
    /// Custom CSS: ClearButtonIcon="BitIconInfo.Css("my-icon-class")"
    /// </example>
    [Parameter] public BitIconInfo? ClearButtonIcon { get; set; }

    /// <summary>
    /// Gets or sets the name of the icon for the clear button from the built-in Fluent UI icons.
    /// For external icon libraries, use <see cref="ClearButtonIcon"/> instead.
    /// </summary>
    [Parameter] public string? ClearButtonIconName { get; set; }

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

        SetDefaultValue();

        NormalizeValue();

        await base.OnInitializedAsync();
    }

    protected override void OnParametersSet()
    {
        // Whether digit normalization (built-in NormalizeDigits or a custom DigitsNormalizer) is
        // currently active. The Min/Max/Step string parameters are parsed through this normalization,
        // so their cached numeric values (and the derived precision) must be recomputed whenever the
        // normalization is toggled. Re-running only on a state change - rather than on every render -
        // avoids repeatedly invoking a potentially expensive or side-effectful custom DigitsNormalizer
        // delegate, while still covering:
        //   * the first render, where the Min/Max/Step CallOnSet handlers may have executed during
        //     SetParametersAsync before NormalizeDigits/DigitsNormalizer were assigned (parameter
        //     assignment order is not guaranteed), and
        //   * toggling normalization off, where a previously parsed non-Latin Min/Max/Step no longer
        //     parses and must fall back to the type defaults instead of keeping its stale value.
        var normalizationActive = NormalizeDigits || DigitsNormalizer is not null;
        if (normalizationActive != _lastNormalizationActive)
        {
            _lastNormalizationActive = normalizationActive;

            // Only re-run for parameters that were actually provided. Re-running a setter for an
            // unset parameter would reset it to its default (and is unnecessary work).
            if (Min is not null) OnSetMin();
            if (Max is not null) OnSetMax();
            if (Step is not null) OnSetStep();

            // Precision can be derived from Step (CalculatePrecision), so it must be recomputed using
            // the now-normalized Step; otherwise a non-Latin decimal Step (e.g. "۰٫۰۱") could leave
            // the precision stale and round fractional values incorrectly.
            OnSetPrecision();
        }

        base.OnParametersSet();
    }

    protected override bool TryParseValueFromString(string? value, [MaybeNullWhen(false)] out TValue result, [NotNullWhen(false)] out string? parsingErrorMessage)
    {
        // Reset the preserved display text. It is set again below only when the digit
        // normalization is the sole transformation applied to the user's input.
        _displayValue = null;
        _displayValueSource = default;
        _keepDisplayValueOnNextChange = false;

        var originalValue = value;
        var digitsNormalized = false;

        if (DigitsNormalizer is not null)
        {
            value = DigitsNormalizer(value);
            digitsNormalized = string.Equals(value, originalValue, StringComparison.Ordinal) is false;
        }
        else if (NormalizeDigits)
        {
            value = NormalizeUnicodeDigits(value);
            digitsNormalized = string.Equals(value, originalValue, StringComparison.Ordinal) is false;
        }

        if (NumberFormat is not null)
        {
            value = CleanValue(value);
        }

        // The input collapsed to an empty string purely because digit normalization stripped its
        // contents (e.g. an Arabic thousands separator "٬" typed on its own, or a custom normalizer
        // removing units/symbols). For nullable types BindConverter would happily turn "" into null,
        // silently clearing the value. Since the user did type something non-empty, surface a parse
        // error instead so the value is not silently lost.
        if (digitsNormalized && value.HasNoValue() && originalValue.HasValue())
        {
            result = default;
            parsingErrorMessage = string.Format(CultureInfo.InvariantCulture, ParsingErrorMessage, DisplayName ?? FieldIdentifier.FieldName);
            return false;
        }

        if (BindConverter.TryConvertTo(value, CultureInfo.InvariantCulture, out result))
        {
            var parsedValue = result;

            result = CheckMinAndMax(result);

            result = Normalize(result);

            // Keep the user's original text visible in the input when digit normalization was the
            // only transformation, i.e. the parsed number wasn't altered by min/max clamping or
            // precision rounding. This avoids visibly converting the typed digits (culture-agnostic,
            // since it compares the numeric values rather than their formatted strings) while still
            // updating the bound .NET value to the normalized number.
            // When NumberFormat is set the formatted string takes precedence (e.g. on focus-out the
            // field should show "123.00" rather than the raw typed digits), so the original text is
            // only preserved when no further formatting will be applied.
            // Crucially, the original text is only preserved when it is digit-equivalent to the
            // canonical value (see IsDisplayDigitEquivalent). This prevents an arbitrary custom
            // DigitsNormalizer (or one that strips units/symbols/aliases) from showing one thing while
            // a different number is bound - the visible text and the bound value must represent the
            // same number.
            if (digitsNormalized
                && NumberFormat is null
                && EqualityComparer<TValue>.Default.Equals(parsedValue, result)
                && IsDisplayDigitEquivalent(originalValue, result))
            {
                _displayValue = originalValue;
                _displayValueSource = result;
                // The value assignment that immediately follows this parse (raised through
                // OnValueChanged) is the one that produced this preserved text, so it must not clear
                // it. Any later value change comes from elsewhere (parent/model) and should discard it.
                _keepDisplayValueOnNextChange = true;
            }

            parsingErrorMessage = null;
            return true;
        }
        else
        {
            parsingErrorMessage = string.Format(CultureInfo.InvariantCulture, ParsingErrorMessage, DisplayName ?? FieldIdentifier.FieldName);
            return false;
        }
    }

    /// <summary>
    /// Returns the string to display in the input. When digit normalization preserved the user's
    /// original text (see <see cref="TryParseValueFromString"/>), that text is shown as long as it
    /// still corresponds to the current value; otherwise the regular formatted value is used.
    /// </summary>
    private string? GetDisplayValueAsString()
    {
        if (_displayValue is not null
            && NumberFormat is null
            && EqualityComparer<TValue>.Default.Equals(CurrentValue, _displayValueSource))
        {
            return _displayValue;
        }

        return CurrentValueAsString;
    }

    /// <summary>
    /// Determines whether <paramref name="originalValue"/> (the raw text the user typed) is merely a
    /// non-Latin-digit rendering of <paramref name="value"/> (the canonical bound number). It is used
    /// to decide whether the original text is safe to keep visible: only when mapping its Unicode
    /// decimal digits to Latin reproduces the canonical formatted value exactly. This guards against a
    /// custom <see cref="DigitsNormalizer"/> (or any transformation that strips units, symbols, spaces
    /// or aliases) leaving pre-normalized text visible while a different number is bound.
    /// </summary>
    private bool IsDisplayDigitEquivalent(string? originalValue, TValue value)
    {
        return string.Equals(NormalizeUnicodeDigits(originalValue), FormatValueAsString(value), StringComparison.Ordinal);
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



    private async Task HandleOnStringValueSet(string? value)
    {
        var args = new ChangeEventArgs() { Value = value };

        if (Immediate)
        {
            await HandleOnStringValueInputAsync(args);
        }
        else
        {
            await HandleOnStringValueChangeAsync(args);
        }
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

        if (IsDisposed) return;

        ResetCts();

        var cts = _continuousChangeValueCts;
        try
        {
            await Task.Run(async () =>
            {
                await InvokeAsync(async () =>
                {
                    await Task.Delay(400);
                    await ContinuousChangeValue(isIncrement, cts);
                });
            }, cts.Token);
        }
        catch (OperationCanceledException) { }
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
        if (cts.IsCancellationRequested || IsDisposed) return;

        await ChangeValueAndInvokeEvents(isIncrement);

        if (IsDisposed) return;

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

        // The value is being changed via the spin buttons / wheel / arrow keys, so any preserved
        // user-typed display text is no longer relevant and the formatted value should be shown.
        _displayValue = null;
        _displayValueSource = default;

        CurrentValue = result;

        StateHasChanged();
    }

    private void ResetCts()
    {
        if (IsDisposed) return;

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

    private static string? NormalizeUnicodeDigits(string? value)
    {
        if (value.HasNoValue()) return value;

        var sb = new System.Text.StringBuilder(value!.Length);
        var changed = false;

        for (var i = 0; i < value!.Length; i++)
        {
            var c = value[i];

            if (c is >= '0' and <= '9' or '.' or '-')
            {
                sb.Append(c);
                continue;
            }

            // Decimal digits in the Unicode supplementary planes (e.g. U+1D7CE..U+1D7FF Mathematical
            // digits) are represented by surrogate pairs, so they must be handled before the single
            // 'char' lookup below which cannot see an astral code point. GetDecimalDigitValue(string,
            // int) understands the surrogate pair when the index points at the high surrogate.
            if (char.IsHighSurrogate(c) && i + 1 < value.Length && char.IsLowSurrogate(value[i + 1]))
            {
                var surrogateDigit = CharUnicodeInfo.GetDecimalDigitValue(value, i);
                if (surrogateDigit >= 0)
                {
                    sb.Append((char)('0' + surrogateDigit));
                    changed = true;
                }
                else
                {
                    sb.Append(c);
                    sb.Append(value[i + 1]);
                }

                i++; // the low surrogate has been consumed as part of this code point.
                continue;
            }

            // Any Unicode decimal digit in the BMP (e.g. Persian U+06F0-U+06F9, Arabic-Indic U+0660-U+0669, etc.).
            var digit = CharUnicodeInfo.GetDecimalDigitValue(c);
            if (digit >= 0)
            {
                sb.Append((char)('0' + digit));
                changed = true;
                continue;
            }

            // Decimal separator emitted by Persian/Arabic keyboard layouts.
            if (c is '٫') // U+066B ARABIC DECIMAL SEPARATOR
            {
                sb.Append('.');
                changed = true;
                continue;
            }

            // Thousands/group separator emitted by Persian/Arabic keyboard layouts. It carries no
            // numeric meaning, so it's dropped (analogous to how CleanValue strips the Latin grouping
            // separator) to avoid a silent parse failure on common real-world input like "۱٬۲۳۴".
            if (c is '٬') // U+066C ARABIC THOUSANDS SEPARATOR
            {
                changed = true;
                continue;
            }

            sb.Append(c);
        }

        return changed ? sb.ToString() : value;
    }

    private static string? CleanValue(string? value)
    {
        if (value.HasNoValue()) return null;

        var pattern = new Regex(@"-?\d*(?:\.\d*)?");
        var matchCollection = pattern.Matches(value!);

        return matchCollection is null ? value : string.Join("", matchCollection.Select(m => m.Value));
    }

    /// <summary>
    /// Applies the same digit normalization used for user input (<see cref="DigitsNormalizer"/> or
    /// <see cref="NormalizeDigits"/>) to the numeric string parameters (<see cref="Min"/>,
    /// <see cref="Max"/> and <see cref="Step"/>) so that markup like <c>Min="۱۰"</c> or
    /// <c>Step="۰٫۵"</c> is parsed consistently instead of silently falling back to defaults.
    /// </summary>
    private string? NormalizeNumericParameter(string? value)
    {
        if (DigitsNormalizer is not null)
        {
            return DigitsNormalizer(value);
        }

        if (NormalizeDigits)
        {
            return NormalizeUnicodeDigits(value);
        }

        return value;
    }

    private void OnSetMin()
    {
        var min = CleanValue(NormalizeNumericParameter(Min));
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
        var max = CleanValue(NormalizeNumericParameter(Max));
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
        var step = CleanValue(NormalizeNumericParameter(Step));
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
            return (TValue)Convert.ChangeType(Math.Round(doubleValue, _precision), _typeOfValue);
        }
        else if (value is float floatValue)
        {
            return (TValue)Convert.ChangeType(Math.Round(floatValue, _precision), _typeOfValue);
        }
        else if (value is decimal decimalValue)
        {
            return (TValue)Convert.ChangeType(Math.Round(decimalValue, _precision), _typeOfValue);
        }

        return value;
    }

    private int CalculatePrecision()
    {
        var step = NormalizeNumericParameter(Step) ?? _step?.ToString() ?? "1";
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
        if (_keepDisplayValueOnNextChange)
        {
            // This change is the one produced by the user input we intentionally preserved
            // (see TryParseValueFromString), so keep the display text for this single change only.
            _keepDisplayValueOnNextChange = false;
        }
        else
        {
            // The value changed from a source other than the preserved user input (e.g. the parent
            // resetting or reloading the bound value), so any preserved display text is now stale and
            // must be discarded to avoid re-showing old user-typed text for a model-driven value.
            _displayValue = null;
            _displayValueSource = default;
        }

        NormalizeValue();
    }



    protected override async ValueTask DisposeAsync(bool disposing)
    {
        if (IsDisposed || disposing is false) return;

        OnValueChanged -= HandleOnValueChanged;

        _continuousChangeValueCts?.Cancel();
        _continuousChangeValueCts?.Dispose();

        await base.DisposeAsync(disposing);
    }
}
