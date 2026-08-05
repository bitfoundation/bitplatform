using System.Globalization;
using System.Text.RegularExpressions;
using System.Diagnostics.CodeAnalysis;

namespace Bit.BlazorUI;

/// <summary>
/// A NumberField (number input / spin button) allows entering values of any .NET numeric type, including their
/// nullable variants. It supports min/max clamping, custom steps with optional snapping, rounding precision,
/// .NET number formatting, increment/decrement buttons in several layouts, the full ARIA spinbutton keyboard set
/// (Up/Down arrows, PageUp/PageDown, Home/End), mouse wheel interaction, non-Latin digit normalization and full
/// form validation integration.
/// </summary>
public partial class BitNumberField<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] TValue> : BitTextInputBase<TValue>
{
    private int? _precision;
    private bool _hasFocus;
    private string? _tempValue;
    private string? _displayValue;
    private TValue? _displayValueSource;
    private bool _keepDisplayValueOnNextChange;
    private bool _lastNormalizationActive;
    private TValue _min = default!;
    private TValue _max = default!;
    private bool _hasExplicitMin;
    private bool _hasExplicitMax;
    private TValue _step = default!;
    private TValue _pageStep = default!;
    private bool _hasPageStep;
    private string? _registeredPreventKeys;
    private readonly string _labelId;
    private readonly string _inputId;
    private readonly string _inputMode;
    private readonly Type _typeOfValue;
    private readonly TValue _zeroValue;
    private ElementReference _buttonIncrement;
    private ElementReference _buttonDecrement;
    private CancellationTokenSource _continuousChangeValueCts = new();



    private static readonly Type[] _supportedTypes =
    [
        typeof(byte), typeof(sbyte), typeof(short), typeof(ushort), typeof(int), typeof(uint),
        typeof(long), typeof(ulong), typeof(float), typeof(double), typeof(decimal)
    ];

    public BitNumberField()
    {
        _typeOfValue = typeof(TValue);
        _typeOfValue = Nullable.GetUnderlyingType(_typeOfValue) ?? _typeOfValue;

        if (Array.IndexOf(_supportedTypes, _typeOfValue) < 0)
        {
            throw new InvalidOperationException($"BitNumberField does not support the type '{typeof(TValue)}'. " +
                                                 "The supported types are byte, sbyte, short, ushort, int, uint, long, ulong, float, double and decimal (including their nullable variants).");
        }

        BindConverter.TryConvertTo("1", CultureInfo.InvariantCulture, out _step!);
        BindConverter.TryConvertTo("0", CultureInfo.InvariantCulture, out _zeroValue!);

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
    /// The delay in milliseconds before the value starts changing continuously while an
    /// increment/decrement button is held down.
    /// </summary>
    [Parameter] public int ContinuousSpinDelay { get; set; } = 400;

    /// <summary>
    /// The interval in milliseconds between two consecutive value changes while an
    /// increment/decrement button is held down.
    /// </summary>
    [Parameter] public int ContinuousSpinInterval { get; set; } = 75;

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
    /// Hides the text input element while keeping the increment/decrement buttons functional,
    /// turning the component into a stepper-only control.
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
    /// Reverses the direction of the value change when the user spins the value using the mouse wheel
    /// (the wheel only changes the value while the Shift key is held down, to keep normal page scrolling intact).
    /// </summary>
    [Parameter] public bool InvertMouseWheel { get; set; }

    /// <summary>
    /// Makes only the text input part read-only, preventing typing, while the value can still be
    /// changed using the increment/decrement buttons, the arrow keys and the mouse wheel
    /// (unlike ReadOnly, which blocks all of them).
    /// </summary>
    [Parameter] public bool IsInputReadOnly { get; set; }

    /// <summary>
    /// The position of the label in regards to the field (Top by default).
    /// </summary>
    [Parameter, ResetClassBuilder]
    public BitLabelPosition? LabelPosition { get; set; }

    /// <summary>
    /// Descriptive label for the number field, rendered next to it (per LabelPosition) and read by screen readers.
    /// </summary>
    [Parameter, ResetClassBuilder]
    public string? Label { get; set; }

    /// <summary>
    /// Shows the custom Label for number field. If you don't call default label, ensure that you give your custom label an id and that you set the input's aria-labelledby prop to that id.
    /// </summary>
    [Parameter] public RenderFragment? LabelTemplate { get; set; }

    /// <summary>
    /// The minimum value of the number field. Values below it get clamped to it, both when typed and when spinning.
    /// It is a string to support any numeric type of the field; an unparsable value falls back to the type's MinValue.
    /// </summary>
    [Parameter]
    [CallOnSet(nameof(OnSetMin))]
    public string? Min { get; set; }

    /// <summary>
    /// The maximum value of the number field. Values above it get clamped to it, both when typed and when spinning.
    /// It is a string to support any numeric type of the field; an unparsable value falls back to the type's MaxValue.
    /// </summary>
    [Parameter]
    [CallOnSet(nameof(OnSetMax))]
    public string? Max { get; set; }

    /// <summary>
    /// Determines how the increment/decrement buttons render: Compact (stacked at the end of the input),
    /// Inline (side by side at the end) or Spread (one on each side). When null (default), no buttons render,
    /// while the value can still be changed using the arrow keys and the mouse wheel.
    /// </summary>
    [Parameter] public BitSpinButtonMode? Mode { get; set; }

    /// <summary>
    /// Disables the automatic select-all of the input's text when the field receives focus.
    /// </summary>
    [Parameter] public bool NoSelectOnFocus { get; set; }

    /// <summary>
    /// Normalizes non-Latin (e.g. Persian "۱۲۳" or Arabic "١٢٣") decimal digits to their Latin (0-9) equivalents before parsing.
    /// This is culture-agnostic and works for any Unicode decimal digit system, including digits in the supplementary planes
    /// (surrogate pairs). The Arabic decimal separator (U+066B) is mapped to '.', and the Arabic thousands separator (U+066C) is stripped.
    /// The same normalization is also applied to the <see cref="Min"/>, <see cref="Max"/> and <see cref="Step"/> parameters so that
    /// non-Latin constraints (e.g. <c>Min="۱۰"</c>) are parsed consistently with user input.
    /// </summary>
    [Parameter] public bool NormalizeDigits { get; set; }

    /// <summary>
    /// The format of the number in the number field, using the standard or custom .NET numeric format strings
    /// (e.g. "N0", "C0" or "000000"). The formatting is applied whenever the value is committed, while the bound
    /// value stays a plain number. Note that value-scaling formats (like the percent "P" format, which multiplies
    /// the displayed value by 100) are not suitable, since the scaled display cannot be parsed back into the same value.
    /// </summary>
    [Parameter] public string? NumberFormat { get; set; }

    /// <summary>
    /// Callback for when the control loses focus.
    /// </summary>
    [Parameter] public EventCallback<FocusEventArgs> OnBlur { get; set; }

    /// <summary>
    /// Callback executed when the user clears the number field by clicking the clear button.
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
    /// The amount by which the value changes when the user presses the PageUp/PageDown keys, providing a
    /// larger jump than the regular <see cref="Step"/>. It is a string to support any numeric type of the
    /// field; when not provided (or unparsable), PageUp/PageDown change the value by 10 times the Step.
    /// </summary>
    [Parameter]
    [CallOnSet(nameof(OnSetPageStep))]
    public string? PageStep { get; set; }

    /// <summary>
    /// The message format used for invalid values entered in the input.
    /// </summary>
    [Parameter] public string ParsingErrorMessage { get; set; } = "The {0} field is not valid.";

    /// <summary>
    /// Input placeholder text.
    /// </summary>
    [Parameter] public string? Placeholder { get; set; }

    /// <summary>
    /// How many decimal places the value should be rounded to. When not provided, the precision is derived
    /// from the fractional digits of the <see cref="Step"/> parameter (if any); otherwise no rounding is applied.
    /// A negative value rounds to a power of ten (e.g. -2 rounds to the nearest hundred).
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
    /// Accessible label text for the clear button (for screen reader users), useful for localization.
    /// </summary>
    [Parameter] public string? ClearButtonAriaLabel { get; set; }

    /// <summary>
    /// Whether to show the clear button when the BitNumberField has a value,
    /// resetting the value to null with a single click (most useful with nullable value types).
    /// The button is not rendered while the field is read-only or has no value.
    /// </summary>
    [Parameter] public bool ShowClearButton { get; set; }

    /// <summary>
    /// Snaps the committed value to the nearest multiple of the <see cref="Step"/> (anchored at the
    /// <see cref="Min"/> when one is provided), so typed values align to the same grid that the
    /// increment/decrement stepping produces. Without it, typed values are kept as-is (aside from
    /// min/max clamping and precision rounding).
    /// </summary>
    [Parameter] public bool SnapToStep { get; set; }

    /// <summary>
    /// The difference between two adjacent values of the number field, applied when spinning the value using
    /// the increment/decrement buttons, the Up/Down arrow keys or the mouse wheel.
    /// A fractional step (e.g. "0.01") also implies the rounding precision of the field, unless an explicit
    /// <see cref="Precision"/> is provided. It is a string to support any numeric type of the field;
    /// an unparsable value falls back to 1.
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
            if (PageStep is not null) OnSetPageStep();

            // Precision can be derived from Step (CalculatePrecision), so it must be recomputed using
            // the now-normalized Step; otherwise a non-Latin decimal Step (e.g. "۰٫۰۱") could leave
            // the precision stale and round fractional values incorrectly.
            OnSetPrecision();
        }

        base.OnParametersSet();
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        await base.OnAfterRenderAsync(firstRender);

        await RegisterPreventKeys();
    }

    /// <summary>
    /// Suppresses the browser's default action of the navigation keys the field handles as value
    /// commands (PageUp/PageDown scrolling the page, and Home/End moving the caret when an explicit
    /// Min/Max makes them jump to the bounds). The registration is a plain JS-side listener, updated
    /// only when the effective key list changes (e.g. when ReadOnly or Min/Max toggles).
    /// </summary>
    private async Task RegisterPreventKeys()
    {
        if (IsDisposed) return;

        var interactive = IsEnabled && ReadOnly is false && HideInput is false;

        List<string> keys = [];
        if (interactive)
        {
            keys.Add("PageUp");
            keys.Add("PageDown");
            if (_hasExplicitMin) keys.Add("Home");
            if (_hasExplicitMax) keys.Add("End");
        }

        // Shift+wheel is handled as a value change, so the browser's (horizontal) scrolling has to be
        // suppressed along with it; the flag piggybacks on the same change detection as the key list.
        var joinedKeys = $"{string.Join(',', keys)}|{interactive}";
        if (string.Equals(joinedKeys, _registeredPreventKeys, StringComparison.Ordinal)) return;

        try
        {
            await _js.BitUtilsRegisterPreventKeys(InputElement, [.. keys]);
            await _js.BitUtilsRegisterPreventShiftWheel(InputElement, interactive);
            _registeredPreventKeys = joinedKeys;
        }
        catch { } // JS is unavailable (e.g. a disconnected circuit); the keys still work, only with their browser default side effects.
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
            // Accounting-style formats render negative values in parentheses (e.g. "($1,234)" for the
            // en-US "C" format). CleanValue below only extracts digits, dots and minus signs, so the
            // negativity carried by the parentheses has to be detected first and restored afterwards.
            var openParenIndex = value?.IndexOf('(') ?? -1;
            var isParenthesizedNegative = openParenIndex >= 0 && (value?.IndexOf(')') ?? -1) > openParenIndex;

            // The formatted display is produced with the current culture (e.g. "1.234,50" in German),
            // so its culture-specific separators have to be mapped back to the invariant form before
            // the symbol stripping and the invariant parse.
            value = CleanValue(MapCultureSeparatorsToInvariant(value));

            if (isParenthesizedNegative && value.HasValue() && value![0] is not '-')
            {
                value = $"-{value}";
            }
        }

        // The input collapsed to an empty string purely because a transformation stripped its
        // contents: digit normalization (e.g. an Arabic thousands separator "٬" typed on its own, or
        // a custom normalizer removing units/symbols) or the NumberFormat symbol cleaning (e.g. "abc"
        // containing no digits at all). For nullable types BindConverter would happily turn "" into
        // null, silently clearing the value. Since the user did type something non-empty, surface a
        // parse error instead so the value is not silently lost.
        if (value.HasNoValue() && originalValue.HasValue())
        {
            result = default;
            parsingErrorMessage = string.Format(CultureInfo.InvariantCulture, ParsingErrorMessage, DisplayName ?? FieldIdentifier.FieldName);
            return false;
        }

        if (BindConverter.TryConvertTo(value, CultureInfo.InvariantCulture, out result))
        {
            // The invariant culture parses "NaN" and "Infinity" into valid float/double values, but a
            // NaN value would escape the min/max clamping entirely (every NaN comparison is false) and
            // neither is meaningful numeric input, so both are rejected as unparsable.
            if ((result is double d && double.IsFinite(d) is false) ||
                (result is float f && float.IsFinite(f) is false))
            {
                result = default;
                parsingErrorMessage = string.Format(CultureInfo.InvariantCulture, ParsingErrorMessage, DisplayName ?? FieldIdentifier.FieldName);
                return false;
            }

            var parsedValue = result;

            result = Snap(result);

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
    /// Returns the value for the aria-valuenow attribute, which per the ARIA spec must be a plain
    /// decimal number, so it is always rendered with the invariant culture.
    /// </summary>
    private string? GetAriaValueNow()
    {
        var value = AriaValueNow ?? CurrentValue;

        return value is null ? null : BindConverter.FormatValue(value, CultureInfo.InvariantCulture)?.ToString();
    }

    /// <summary>
    /// The aria-valuemin/valuemax attributes render only when an explicit Min/Max is provided
    /// (announcing the underlying type's extremes would be noise) and, like aria-valuenow,
    /// must be plain invariant decimal numbers.
    /// </summary>
    private string? GetAriaValueMin()
    {
        return _hasExplicitMin ? BindConverter.FormatValue(_min, CultureInfo.InvariantCulture)?.ToString() : null;
    }

    private string? GetAriaValueMax()
    {
        return _hasExplicitMax ? BindConverter.FormatValue(_max, CultureInfo.InvariantCulture)?.ToString() : null;
    }

    /// <summary>
    /// Returns the value for the aria-valuetext attribute. Per the ARIA authoring practices it should
    /// only be present when the plain number would be ambiguous or incomplete - i.e. when the visible
    /// text (formatted or user-typed non-Latin digits) differs from the aria-valuenow number.
    /// </summary>
    private string? GetAriaValueText()
    {
        if (AriaValueText.HasValue()) return AriaValueText;

        var display = GetDisplayValueAsString();

        return string.Equals(display, GetAriaValueNow(), StringComparison.Ordinal) ? null : display;
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

        // The displayed text must round-trip through TryParseValueFromString, which parses with the
        // invariant culture. A culture-sensitive ToString would render "1,5" in e.g. a German culture
        // and then fail to parse (or worse, parse as 15, since ',' is the invariant group separator).
        if (NumberFormat is null) return BindConverter.FormatValue(value, CultureInfo.InvariantCulture)?.ToString();

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
                // On key auto-repeat the input text cannot have changed since the previous step, so
                // the JS roundtrip that reads the live text is skipped.
                if (e.Repeat is false)
                {
                    await CommitPendingInputValue();
                }
                ChangeValue(+1);

                if (OnIncrement.HasDelegate)
                {
                    await OnIncrement.InvokeAsync(CurrentValue);
                }
                break;

            case "ArrowDown":
                if (e.Repeat is false)
                {
                    await CommitPendingInputValue();
                }
                ChangeValue(-1);

                if (OnDecrement.HasDelegate)
                {
                    await OnDecrement.InvokeAsync(CurrentValue);
                }
                break;

            // PageUp/PageDown change the value by a larger step than the arrow keys (per the ARIA
            // spinbutton pattern): the PageStep when provided, otherwise 10 times the regular Step.
            // A modifier key means the user is not spinning (e.g. Shift+PageUp extends a selection),
            // so those combinations are left to the browser.
            case "PageUp":
                if (HasModifier(e)) return;
                if (e.Repeat is false)
                {
                    await CommitPendingInputValue();
                }
                if (_hasPageStep) ChangeValue(+1, _pageStep); else ChangeValue(+10, _step);

                if (OnIncrement.HasDelegate)
                {
                    await OnIncrement.InvokeAsync(CurrentValue);
                }
                break;

            case "PageDown":
                if (HasModifier(e)) return;
                if (e.Repeat is false)
                {
                    await CommitPendingInputValue();
                }
                if (_hasPageStep) ChangeValue(-1, _pageStep); else ChangeValue(-10, _step);

                if (OnDecrement.HasDelegate)
                {
                    await OnDecrement.InvokeAsync(CurrentValue);
                }
                break;

            // Home/End jump to the minimum/maximum (per the ARIA spinbutton pattern), but only when an
            // explicit Min/Max is provided - jumping to the underlying type's extreme (e.g.
            // int.MinValue) would hardly ever be what the user wants. Modified keys (e.g. Shift+Home
            // selecting to the start of the text) keep their standard text-editing behavior.
            case "Home":
                if (HasModifier(e) || _hasExplicitMin is false) return;
                SetBoundValue(_min);
                break;

            case "End":
                if (HasModifier(e) || _hasExplicitMax is false) return;
                SetBoundValue(_max);
                break;

            default:
                break;
        }
    }

    private static bool HasModifier(KeyboardEventArgs e) => e.ShiftKey || e.CtrlKey || e.AltKey || e.MetaKey;

    /// <summary>
    /// Sets the value straight to one of the range bounds (used by the Home/End keys).
    /// </summary>
    private void SetBoundValue(TValue bound)
    {
        _displayValue = null;
        _displayValueSource = default;

        CurrentValue = CheckMinAndMax(bound);

        StateHasChanged();
    }

    /// <summary>
    /// Commits the text currently sitting in the input element before a step is applied. Without the
    /// Immediate mode, typed text only commits on blur/Enter, so stepping right after typing would
    /// otherwise apply to the stale previous value instead of what the user currently sees.
    /// The live text is read through JS; when that is unavailable (prerendering) it comes back empty
    /// and the step simply applies to the last committed value.
    /// </summary>
    private async Task CommitPendingInputValue()
    {
        string? liveValue = null;
        try
        {
            liveValue = await _js.BitUtilsGetProperty(InputElement, "value");
        }
        catch { }

        if (liveValue.HasValue() && string.Equals(liveValue, GetDisplayValueAsString(), StringComparison.Ordinal) is false)
        {
            await SetCurrentValueAsStringAsync(liveValue);
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
        // The text selection is handled in HandleOnFocus; the focus event always accompanies
        // focusin, so selecting here too would just issue a duplicate JS call.
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

        if (NoSelectOnFocus is false)
        {
            await _js.BitUtilsSelectText(InputElement);
        }

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
                    await Task.Delay(Math.Max(1, ContinuousSpinDelay));
                    await ContinuousChangeValue(isIncrement, cts);
                });
            }, cts.Token);
        }
        catch (OperationCanceledException) { }
    }

    private void HandleOnPointerUpOrOut()
    {
        ResetCts();
    }

    private async Task HandleOnMouseWheel(WheelEventArgs e)
    {
        if (IsEnabled is false || ReadOnly || InvalidValueBinding()) return;
        if (e.ShiftKey is false) return;

        if (e.DeltaY < 0)
        {
            await CommitPendingInputValue();
            ChangeValue(InvertMouseWheel ? -1 : +1);
        }
        else if (e.DeltaY > 0)
        {
            await CommitPendingInputValue();
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

        await Task.Delay(Math.Max(1, ContinuousSpinInterval));
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
        ChangeValue(factor, _step);
    }

    private void ChangeValue(int factor, TValue step)
    {
        TValue result;

        if (_typeOfValue == typeof(float) || _typeOfValue == typeof(double))
        {
            // double covers the full range of both float and double; going out of range saturates to
            // an infinity which the clamp below brings back to the min/max bound.
            var r = Convert.ToDouble(CurrentValue) + (factor * Convert.ToDouble(step));

            var min = Convert.ToDouble(_min);
            var max = Convert.ToDouble(_max);
            r = r < min ? min : r > max ? max : r;

            result = (TValue)Convert.ChangeType(r, _typeOfValue, CultureInfo.InvariantCulture);
        }
        else if (_typeOfValue == typeof(decimal))
        {
            var current = Convert.ToDecimal(CurrentValue);
            var delta = factor * Convert.ToDecimal(step);

            decimal r;
            try
            {
                r = current + delta;
            }
            catch (OverflowException)
            {
                r = delta > 0 ? Convert.ToDecimal(_max) : Convert.ToDecimal(_min);
            }

            var min = Convert.ToDecimal(_min);
            var max = Convert.ToDecimal(_max);
            r = r < min ? min : r > max ? max : r;

            result = (TValue)(object)r;
        }
        else
        {
            // All integral types (byte, sbyte, short, ushort, int, uint, long, ulong): decimal spans
            // the whole long.MinValue..ulong.MaxValue range, so the arithmetic can neither overflow
            // nor wrap around; the result is clamped before narrowing back to the target type. This
            // also avoids the int-promotion of small-type arithmetic (byte + byte is an int) that
            // cannot be unboxed back into the smaller TValue.
            var r = Convert.ToDecimal(CurrentValue) + (factor * Convert.ToDecimal(step));

            var min = Convert.ToDecimal(_min);
            var max = Convert.ToDecimal(_max);
            r = r < min ? min : r > max ? max : r;

            result = (TValue)Convert.ChangeType(r, _typeOfValue, CultureInfo.InvariantCulture);
        }

        result = Snap(result);
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

    /// <summary>
    /// Snaps the value to the nearest multiple of the <see cref="Step"/> when <see cref="SnapToStep"/>
    /// is enabled. The stepping grid is anchored at the <see cref="Min"/> when one is provided (so with
    /// Min=2 and Step=3 the reachable values are 2, 5, 8, ...), otherwise at zero. Values that cannot
    /// be snapped safely (overflow at the extremes of the numeric type) are returned unchanged; the
    /// regular min/max clamping still applies afterwards.
    /// </summary>
    private TValue Snap(TValue value)
    {
        if (SnapToStep is false || value is null) return value;

        try
        {
            if (_typeOfValue == typeof(float) || _typeOfValue == typeof(double))
            {
                var step = Convert.ToDouble(_step);
                if (step <= 0 || double.IsFinite(step) is false) return value;

                var basis = _hasExplicitMin ? Convert.ToDouble(_min) : 0d;
                var snapped = basis + (Math.Round((Convert.ToDouble(value) - basis) / step, MidpointRounding.AwayFromZero) * step);
                if (double.IsFinite(snapped) is false) return value;

                return (TValue)Convert.ChangeType(snapped, _typeOfValue, CultureInfo.InvariantCulture);
            }
            else
            {
                // decimal arithmetic is exact for the fractional steps of the decimal type and spans
                // the full range of every integral type, avoiding any floating-point drift.
                var step = Convert.ToDecimal(_step);
                if (step <= 0) return value;

                var basis = _hasExplicitMin ? Convert.ToDecimal(_min) : 0m;
                var snapped = basis + (Math.Round((Convert.ToDecimal(value) - basis) / step, MidpointRounding.AwayFromZero) * step);

                return (TValue)Convert.ChangeType(snapped, _typeOfValue, CultureInfo.InvariantCulture);
            }
        }
        catch (OverflowException)
        {
            return value;
        }
    }

    private TValue CheckMinAndMax(TValue result)
    {
        if (result is null) return result;

        // Comparer<TValue>.Default uses the numeric type's own IComparable<T> implementation, so the
        // comparison is exact for every supported type (including ulong values beyond long.MaxValue
        // and the full decimal range). Ordering the bounds first keeps the clamping sensible even
        // when a misconfigured Min is greater than Max (the effective range is simply swapped).
        var comparer = Comparer<TValue>.Default;
        var (min, max) = comparer.Compare(_min, _max) <= 0 ? (_min, _max) : (_max, _min);

        if (comparer.Compare(result, min) < 0) return min;
        if (comparer.Compare(result, max) > 0) return max;

        return result;
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

    /// <summary>
    /// Maps the current culture's group and decimal separators to their invariant equivalents
    /// (group separators get removed, decimal separators become '.'). The group separators must be
    /// handled first: in cultures like German the group separator is '.' itself, which would
    /// otherwise collide with the invariant decimal point.
    /// </summary>
    private static string? MapCultureSeparatorsToInvariant(string? value)
    {
        if (value.HasNoValue()) return value;

        var numberFormatInfo = CultureInfo.CurrentCulture.NumberFormat;
        var result = value!;

        foreach (var groupSeparator in new[] { numberFormatInfo.NumberGroupSeparator, numberFormatInfo.CurrencyGroupSeparator })
        {
            if (groupSeparator.HasValue())
            {
                result = result.Replace(groupSeparator, string.Empty);
            }
        }

        foreach (var decimalSeparator in new[] { numberFormatInfo.NumberDecimalSeparator, numberFormatInfo.CurrencyDecimalSeparator })
        {
            if (decimalSeparator.HasValue() && decimalSeparator != ".")
            {
                result = result.Replace(decimalSeparator, ".");
            }
        }

        return result;
    }

    private static readonly Regex _cleanValueRegex = new(@"-?\d*(?:\.\d*)?", RegexOptions.Compiled);

    private static string? CleanValue(string? value)
    {
        if (value.HasNoValue()) return null;

        var matchCollection = _cleanValueRegex.Matches(value!);

        return string.Join("", matchCollection.Select(m => m.Value));
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
        if (BindConverter.TryConvertTo(min, CultureInfo.InvariantCulture, out TValue? result) && result is not null)
        {
            _min = result;
            _hasExplicitMin = true;
        }
        else
        {
            // An absent or unparsable Min falls back to the type's own minimum, which only acts as an
            // overflow guard - it is not treated as an explicit bound (no aria-valuemin, no Home key
            // jump, no snap anchoring).
            _min = GetTypeMinValue();
            _hasExplicitMin = false;
        }
    }

    private void OnSetMax()
    {
        var max = CleanValue(NormalizeNumericParameter(Max));
        if (BindConverter.TryConvertTo(max, CultureInfo.InvariantCulture, out TValue? result) && result is not null)
        {
            _max = result;
            _hasExplicitMax = true;
        }
        else
        {
            _max = GetTypeMaxValue();
            _hasExplicitMax = false;
        }
    }

    private void OnSetStep()
    {
        var step = CleanValue(NormalizeNumericParameter(Step));
        if (BindConverter.TryConvertTo(step, CultureInfo.InvariantCulture, out TValue? result) && result is not null)
        {
            _step = result;
        }
        else
        {
            // A direct (TValue)(object)1 cast would throw an InvalidCastException for any non-int
            // TValue (an int cannot be unboxed as a double/decimal/...), so the fallback goes through
            // the same converter used by the constructor.
            BindConverter.TryConvertTo("1", CultureInfo.InvariantCulture, out _step!);
        }

        // The precision can be derived from the Step parameter, so it has to be recomputed whenever
        // the Step changes (an explicitly provided Precision parameter still takes precedence).
        OnSetPrecision();
    }

    private void OnSetPageStep()
    {
        var pageStep = CleanValue(NormalizeNumericParameter(PageStep));
        if (BindConverter.TryConvertTo(pageStep, CultureInfo.InvariantCulture, out TValue? result) && result is not null)
        {
            _pageStep = result;
            _hasPageStep = true;
        }
        else
        {
            // No usable PageStep: PageUp/PageDown fall back to 10 times the regular Step.
            _pageStep = default!;
            _hasPageStep = false;
        }
    }

    private void OnSetPrecision()
    {
        _precision = Precision ?? CalculatePrecision();
    }

    private TValue Normalize(TValue value)
    {
        // No rounding is applied unless an explicit Precision is provided or a fractional Step
        // implies one. Rounding by default would silently mutilate user input (e.g. a plain
        // BitNumberField<double> turning a typed "1.23" into "1").
        if (_precision is not int precision) return value;

        if (value is double doubleValue)
        {
            return (TValue)(object)RoundDouble(doubleValue, precision);
        }

        if (value is float floatValue)
        {
            return (TValue)(object)(float)RoundDouble(floatValue, precision);
        }

        if (value is decimal decimalValue)
        {
            return (TValue)(object)RoundDecimal(decimalValue, precision);
        }

        return value;
    }

    private static double RoundDouble(double value, int precision)
    {
        // Math.Round only accepts 0..15 digits for doubles; a negative precision means rounding to a
        // power of ten (e.g. -1 rounds to the nearest ten), which is done by scaling.
        if (precision >= 0) return Math.Round(value, Math.Min(precision, 15));

        var scale = Math.Pow(10, -precision);
        return Math.Round(value / scale) * scale;
    }

    private static decimal RoundDecimal(decimal value, int precision)
    {
        if (precision >= 0) return Math.Round(value, Math.Min(precision, 28));

        try
        {
            var scale = (decimal)Math.Pow(10, -precision);
            return Math.Round(value / scale) * scale;
        }
        catch (OverflowException)
        {
            // The scaled rounding went out of the decimal range; the original value is kept as is.
            return value;
        }
    }

    /// <summary>
    /// Derives the rounding precision from the fractional digits of the <see cref="Step"/> parameter
    /// (e.g. a Step of "0.25" implies 2 decimal places). Returns null (no rounding) when no Step is
    /// provided or the Step has no fractional part.
    /// </summary>
    private int? CalculatePrecision()
    {
        var step = NormalizeNumericParameter(Step);
        if (step.HasNoValue()) return null;

        var dotIndex = step!.IndexOf('.');
        if (dotIndex < 0) return null;

        var fractionLength = step.AsSpan(dotIndex + 1).TrimEnd('0').Length;
        return fractionLength == 0 ? null : fractionLength;
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
