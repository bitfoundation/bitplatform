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
    private string? _displayValue;
    private TValue? _displayValueSource;
    private bool _displayValueIsTransient;
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
    private string _inputMode;
    private readonly string _labelId;
    private readonly string _inputId;
    private readonly string _descriptionId;
    private readonly string _defaultInputMode;
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
        _descriptionId = $"BitNumberField-{UniqueId}-description";

        _defaultInputMode = (_typeOfValue == typeof(decimal) || _typeOfValue == typeof(double) || _typeOfValue == typeof(float)) ? "decimal" : "numeric";
        _inputMode = _defaultInputMode;
    }



    [Inject] private IJSRuntime _js { get; set; } = default!;



    /// <summary>
    /// The general color of the number field, used for its focus indicator and for the icon,
    /// prefix and suffix while the field is focused (Primary by default).
    /// </summary>
    [Parameter, ResetClassBuilder]
    public BitColor? Accent { get; set; }

    /// <summary>
    /// Detailed description of the input for the benefit of screen readers. It is rendered into a
    /// visually hidden element that the input references through its aria-describedby attribute.
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
    /// The color kind of the number field background (Primary by default).
    /// </summary>
    [Parameter, ResetClassBuilder]
    public BitColorKind? Background { get; set; }

    /// <summary>
    /// The color kind of the number field border (Primary by default).
    /// </summary>
    [Parameter, ResetClassBuilder]
    public BitColorKind? Border { get; set; }

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
    /// A hint rendered under the field, describing what is expected of it (e.g. the accepted range
    /// or the unit). Unlike <see cref="AriaDescription"/> it is visible, and the input references it
    /// through its aria-describedby attribute so it is announced along with the field.
    /// </summary>
    [Parameter, ResetClassBuilder]
    public string? Description { get; set; }

    /// <summary>
    /// A custom template rendered in place of the <see cref="Description"/>, referenced by the input
    /// through its aria-describedby attribute just the same.
    /// </summary>
    [Parameter, ResetClassBuilder]
    public RenderFragment? DescriptionTemplate { get; set; }

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
    /// Stretches the number field to the full width of its container. By default the field only
    /// takes the width it needs, which keeps a stepper from spanning a whole form row.
    /// </summary>
    [Parameter, ResetClassBuilder]
    public bool FullWidth { get; set; }

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
    /// Overrides the virtual keyboard the browser shows for the input. By default it is Numeric for the
    /// integral types and Decimal for the fractional ones (float, double and decimal). Since neither of
    /// those keypads offers a minus sign on every platform, a field that has to accept negative values
    /// on touch devices is better served by Text, which brings up the full keyboard.
    /// </summary>
    [Parameter]
    [CallOnSet(nameof(OnSetInputMode))]
    public BitInputMode? InputMode { get; set; }

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
    /// Removes the border of the number field, which is what you want when it sits inside a surface
    /// that already provides one (a toolbar, a table cell or a card).
    /// </summary>
    [Parameter, ResetClassBuilder]
    public bool NoBorder { get; set; }

    /// <summary>
    /// Keeps values typed outside of the <see cref="Min"/>/<see cref="Max"/> range intact instead of clamping
    /// them to the nearest bound, so that a form validation (e.g. a <c>[Range]</c> data annotation) can report
    /// the out-of-range value to the user instead of it being silently corrected.
    /// Stepping with the increment/decrement buttons, the arrow keys or the mouse wheel still stays inside the
    /// range, and the Home/End keys still jump to the bounds.
    /// </summary>
    [Parameter] public bool NoClamp { get; set; }

    /// <summary>
    /// Disables changing the value using the mouse wheel entirely (by default the value changes when the wheel
    /// is scrolled over the focused field while the Shift key is held down).
    /// </summary>
    [Parameter] public bool NoMouseWheel { get; set; }

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
    /// Callback for when the input is clicked.
    /// </summary>
    [Parameter] public EventCallback<MouseEventArgs> OnClick { get; set; }

    /// <summary>
    /// Callback for when the decrement button or down arrow key is pressed.
    /// </summary>
    [Parameter] public EventCallback<TValue> OnDecrement { get; set; }

    /// <summary>
    /// Callback for when the Enter key is pressed on the input. It is invoked after the typed text has
    /// been committed, so the bound value it observes is already the one the user just entered.
    /// </summary>
    [Parameter] public EventCallback<KeyboardEventArgs> OnEnter { get; set; }

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
    /// Callback for when a key is pressed down on the input. It is invoked for every key, including the ones
    /// the field handles itself (the arrow keys, PageUp/PageDown, Home/End and Escape).
    /// </summary>
    [Parameter] public EventCallback<KeyboardEventArgs> OnKeyDown { get; set; }

    /// <summary>
    /// Callback for when a key is released on the input.
    /// </summary>
    [Parameter] public EventCallback<KeyboardEventArgs> OnKeyUp { get; set; }

    /// <summary>
    /// Callback for when a step lands the value on (or beyond) the explicit <see cref="Max"/>, letting
    /// the consumer react to the ceiling being hit - by explaining why the value stopped growing, for
    /// instance. It only fires for an explicit Max, and only on the step that reaches it.
    /// </summary>
    [Parameter] public EventCallback<TValue> OnMaxReached { get; set; }

    /// <summary>
    /// Callback for when a step lands the value on (or beyond) the explicit <see cref="Min"/>. It only
    /// fires for an explicit Min, and only on the step that reaches it.
    /// </summary>
    [Parameter] public EventCallback<TValue> OnMinReached { get; set; }

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
    /// Whether to show the clear button whenever the field is showing something,
    /// resetting the value to null with a single click (most useful with nullable value types).
    /// "Showing something" covers a string the user typed that failed to parse as well as a real value,
    /// so the button is also there to wipe an entry that has to be corrected. It is not rendered while
    /// the field is read-only or empty.
    /// It stays out of the tab order (like the increment/decrement buttons), the Escape key being
    /// the keyboard equivalent of clicking it.
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

    /// <summary>
    /// Renders the number field with a single bottom rule instead of a full border, the classic
    /// "underlined" input variant.
    /// </summary>
    [Parameter, ResetClassBuilder]
    public bool Underlined { get; set; }



    /// <summary>
    /// Increments the value by the <see cref="Step"/>, exactly as the increment button does - bounds,
    /// snapping, rounding and the <see cref="OnIncrement"/>/<see cref="OnMaxReached"/> callbacks all
    /// included. It lets an external control (a slider, a keypad, a hardware button) drive the field
    /// without the consumer having to reimplement its arithmetic. It does nothing while the field is
    /// disabled or read-only, or when the value already sits at the <see cref="Max"/>.
    /// </summary>
    public Task IncrementAsync() => SpinAsync(isIncrement: true);

    /// <summary>
    /// Decrements the value by the <see cref="Step"/>, the mirror image of <see cref="IncrementAsync"/>.
    /// </summary>
    public Task DecrementAsync() => SpinAsync(isIncrement: false);

    /// <summary>
    /// Clears whatever the field is showing - a value or a string that failed to parse - and raises
    /// <see cref="OnClear"/>, exactly as the clear button and the Escape key do (without requiring
    /// <see cref="ShowClearButton"/>, since there is no button involved). It does nothing while the
    /// field is disabled or read-only.
    /// </summary>
    public Task ClearAsync() => InvokeAsync(async () =>
    {
        if (IsEnabled is false || ReadOnly) return;

        await ClearValue();

        // Unlike the clear button, this call does not arrive through an event handler, so nothing
        // re-renders the component on its own.
        StateHasChanged();

        await OnClear.InvokeAsync();
    });

    /// <summary>
    /// The shared body of <see cref="IncrementAsync"/>/<see cref="DecrementAsync"/>. It is dispatched
    /// through InvokeAsync so that a call arriving from a background thread (a timer, a SignalR message)
    /// still mutates the component on its own renderer's synchronization context.
    /// </summary>
    private Task SpinAsync(bool isIncrement) => InvokeAsync(async () =>
    {
        if (IsEnabled is false || ReadOnly || InvalidValueBinding()) return;

        if (IsSpinBlocked(isIncrement)) return;

        await ChangeValueAndInvokeEvents(isIncrement);
    });



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

        ClassBuilder.Register(() => FullWidth ? "bit-nfl-fwd" : string.Empty);

        // The description is a third child of the root flex box, which the row label layouts have to
        // wrap onto a line of its own; the class is what scopes that to the fields that need it.
        ClassBuilder.Register(() => Description.HasValue() || DescriptionTemplate is not null ? "bit-nfl-hds" : string.Empty);

        ClassBuilder.Register(() => NoBorder ? "bit-nfl-nbd" : string.Empty);

        ClassBuilder.Register(() => Underlined ? "bit-nfl-und" : string.Empty);

        ClassBuilder.Register(() => Accent switch
        {
            BitColor.Primary => "bit-nfl-pri",
            BitColor.Secondary => "bit-nfl-sec",
            BitColor.Tertiary => "bit-nfl-ter",
            BitColor.Info => "bit-nfl-inf",
            BitColor.Success => "bit-nfl-suc",
            BitColor.Warning => "bit-nfl-wrn",
            BitColor.SevereWarning => "bit-nfl-swr",
            BitColor.Error => "bit-nfl-err",
            BitColor.PrimaryBackground => "bit-nfl-pbg",
            BitColor.SecondaryBackground => "bit-nfl-sbg",
            BitColor.TertiaryBackground => "bit-nfl-tbg",
            BitColor.PrimaryForeground => "bit-nfl-pfg",
            BitColor.SecondaryForeground => "bit-nfl-sfg",
            BitColor.TertiaryForeground => "bit-nfl-tfg",
            BitColor.PrimaryBorder => "bit-nfl-pbr",
            BitColor.SecondaryBorder => "bit-nfl-sbr",
            BitColor.TertiaryBorder => "bit-nfl-tbr",
            _ => "bit-nfl-pri"
        });

        ClassBuilder.Register(() => Background switch
        {
            BitColorKind.Primary => "bit-nfl-bpr",
            BitColorKind.Secondary => "bit-nfl-bse",
            BitColorKind.Tertiary => "bit-nfl-btr",
            BitColorKind.Transparent => "bit-nfl-btn",
            _ => "bit-nfl-bpr"
        });

        ClassBuilder.Register(() => Border switch
        {
            BitColorKind.Primary => "bit-nfl-brp",
            BitColorKind.Secondary => "bit-nfl-brs",
            BitColorKind.Tertiary => "bit-nfl-brt",
            BitColorKind.Transparent => "bit-nfl-brn",
            _ => "bit-nfl-brp"
        });
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
            // The arrows spin the value, so the browser must not also move the caret to the start/end
            // of the text underneath - the caret is expected to stay where the user left it.
            keys.Add("ArrowUp");
            keys.Add("ArrowDown");
            keys.Add("PageUp");
            keys.Add("PageDown");
            if (_hasExplicitMin) keys.Add("Home");
            if (_hasExplicitMax) keys.Add("End");
        }

        // Shift+wheel is handled as a value change, so the browser's (horizontal) scrolling has to be
        // suppressed along with it; the flag piggybacks on the same change detection as the key list.
        // It mirrors the conditions of HandleOnMouseWheel exactly - notably the focus requirement, so
        // that scrolling the page over a merely hovered field keeps its normal browser behavior.
        var preventWheel = interactive && NoMouseWheel is false && _hasFocus;

        var joinedKeys = $"{string.Join(',', keys)}|{interactive}|{preventWheel}";
        if (string.Equals(joinedKeys, _registeredPreventKeys, StringComparison.Ordinal)) return;

        try
        {
            await _js.BitUtilsRegisterPreventKeys(InputElement, [.. keys]);
            await _js.BitUtilsRegisterPreventShiftWheel(InputElement, preventWheel);
            _registeredPreventKeys = joinedKeys;
        }
        catch { } // JS is unavailable (e.g. a disconnected circuit); the keys still work, only with their browser default side effects.
    }

    protected override bool TryParseValueFromString(string? value, [MaybeNullWhen(false)] out TValue result, [NotNullWhen(false)] out string? parsingErrorMessage)
    {
        // Reset the preserved display text. It is set again below only when the text the user typed
        // still represents exactly the value that got committed.
        ClearPreservedDisplayValue();
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

        if (BindConverter.TryConvertTo(value, CultureInfo.InvariantCulture, out result) is false)
        {
            // A number carrying whitespace between its digit groups ("1 234", or the non-breaking and
            // narrow no-break spaces that spreadsheets and web pages use for grouping) is a perfectly
            // ordinary thing to paste into a number field, yet the invariant parse rejects it. Retrying
            // without the whitespace rescues that input. Only whitespace is removed, so nothing that
            // is not a plain number starts parsing, and no separator whose meaning differs per culture
            // is reinterpreted.
            var unspaced = RemoveWhiteSpace(value);
            if (string.Equals(unspaced, value, StringComparison.Ordinal) ||
                unspaced.HasNoValue() ||
                BindConverter.TryConvertTo(unspaced, CultureInfo.InvariantCulture, out result) is false)
            {
                parsingErrorMessage = string.Format(CultureInfo.InvariantCulture, ParsingErrorMessage, DisplayName ?? FieldIdentifier.FieldName);
                return false;
            }
        }

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

            // The precision rounding runs before the clamping, so that it cannot push the committed
            // value back out of the range afterwards (with Max=1.005 and Precision=2, rounding a
            // clamped 1.005 would commit 1.01, i.e. a value above the Max the field promises).
            result = Normalize(result);

            // NoClamp lets an out-of-range typed value through so that the form validation can report
            // it. The bounds still apply to every other way of changing the value (stepping, Home/End).
            if (NoClamp is false)
            {
                result = CheckMinAndMax(result);
            }

            // While typing in Immediate mode the value is committed on every keystroke, which would
            // otherwise reformat the input text under the caret and make intermediate states
            // unreachable - typing "1." would immediately snap back to "1", so a decimal could never
            // be typed at all. The raw text is therefore kept visible as long as it still parses into
            // exactly the committed value (i.e. no clamping, snapping or rounding altered it).
            if (Immediate
                && _hasFocus
                && NumberFormat is null
                && digitsNormalized is false
                && originalValue.HasValue()
                && EqualityComparer<TValue>.Default.Equals(parsedValue, result))
            {
                SetPreservedDisplayValue(originalValue, result, transient: true);
            }

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
                SetPreservedDisplayValue(originalValue, result);
            }

            parsingErrorMessage = null;
            return true;
        }
    }

    /// <summary>
    /// Removes every Unicode whitespace character (including the non-breaking and narrow no-break
    /// spaces commonly used as digit group separators) from the value.
    /// </summary>
    private static string? RemoveWhiteSpace(string? value)
    {
        if (value.HasNoValue()) return value;

        var sb = new System.Text.StringBuilder(value!.Length);
        foreach (var c in value!)
        {
            if (char.IsWhiteSpace(c)) continue;

            sb.Append(c);
        }

        return sb.ToString();
    }

    /// <summary>
    /// Keeps <paramref name="originalValue"/> (the exact text the user typed) visible in the input
    /// instead of the canonical formatting of <paramref name="value"/>, for as long as the value stays
    /// the one this text produced.
    /// </summary>
    private void SetPreservedDisplayValue(string? originalValue, TValue value, bool transient = false)
    {
        _displayValue = originalValue;
        _displayValueSource = value;

        // A transient preservation only exists to keep the caret usable while the user is still
        // typing; it is dropped as soon as the field loses focus so the canonical (and possibly
        // formatted) value becomes visible again, as it does in the regular on-commit flow.
        _displayValueIsTransient = transient;



        // The value assignment that immediately follows this parse (raised through OnValueChanged) is
        // the one that produced this preserved text, so it must not clear it. Any later value change
        // comes from elsewhere (parent/model) and should discard it. When the parse did not actually
        // change the value, no OnValueChanged follows at all, so the flag must not be armed either -
        // it would otherwise be consumed by (and swallow the reset of) a later unrelated change.
        _keepDisplayValueOnNextChange = EqualityComparer<TValue>.Default.Equals(value, CurrentValue) is false;
    }

    /// <summary>
    /// Drops any preserved user-typed text so that the canonical (and possibly formatted) rendering
    /// of the current value becomes visible again.
    /// </summary>
    private void ClearPreservedDisplayValue()
    {
        _displayValue = null;
        _displayValueSource = default;
        _displayValueIsTransient = false;
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
        return _hasExplicitMin ? BindConverter.FormatValue(GetOrderedBounds().Min, CultureInfo.InvariantCulture)?.ToString() : null;
    }

    private string? GetAriaValueMax()
    {
        return _hasExplicitMax ? BindConverter.FormatValue(GetOrderedBounds().Max, CultureInfo.InvariantCulture)?.ToString() : null;
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

        var current = CurrentValueAsString;

        // A NumberFormat is a presentation concern: "$1,234" or "001363" is how the value should read,
        // not what the user should have to edit around. While the field is focused the plain number is
        // shown instead, and the formatted rendering comes back on the way out. This is skipped when
        // NoSelectOnFocus is set: without the select-all, swapping the text on focus would move the
        // caret the user just placed, and keeping the text stable then matters more.
        if (_hasFocus is false || NumberFormat is null || NoSelectOnFocus) return current;

        // Only the formatted rendering is swapped out. A string the user typed that failed to parse is
        // still sitting in the input (CurrentValueAsString returns it verbatim) and must stay there for
        // them to correct.
        if (string.Equals(current, FormatValueAsString(CurrentValue), StringComparison.Ordinal) is false) return current;

        return CurrentValue is null ? null : BindConverter.FormatValue(CurrentValue, CultureInfo.InvariantCulture)?.ToString();
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

    /// <summary>
    /// Whether the input is currently showing anything at all - either a value or a string the user
    /// typed that failed to parse and is still sitting there to be corrected. It is what decides
    /// whether there is something for the clear button (and the Escape key) to clear.
    /// </summary>
    private bool HasVisibleText() => GetDisplayValueAsString().HasValue();

    /// <summary>
    /// The effective range, with the bounds ordered so that a misconfigured Min greater than Max
    /// simply describes the same (swapped) range rather than an empty one - matching
    /// <see cref="CheckMinAndMax"/>, which is what actually keeps the value inside it.
    /// </summary>
    private (TValue Min, TValue Max) GetOrderedBounds()
    {
        return Comparer<TValue>.Default.Compare(_min, _max) <= 0 ? (_min, _max) : (_max, _min);
    }

    /// <summary>
    /// Whether <paramref name="value"/> sits at (or beyond) the maximum, i.e. incrementing can no
    /// longer move it. Only an explicit <see cref="Max"/> counts: the numeric type's own extreme is
    /// an overflow guard rather than a bound the user is meant to be told about.
    /// </summary>
    private bool IsAtMax(TValue? value)
    {
        if (_hasExplicitMax is false || value is null) return false;

        return Comparer<TValue>.Default.Compare(value, GetOrderedBounds().Max) >= 0;
    }

    /// <summary>
    /// Whether <paramref name="value"/> sits at (or below) the minimum, i.e. decrementing can no
    /// longer move it.
    /// </summary>
    private bool IsAtMin(TValue? value)
    {
        if (_hasExplicitMin is false || value is null) return false;

        return Comparer<TValue>.Default.Compare(value, GetOrderedBounds().Min) <= 0;
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
        if (IsEnabled is false) return;

        // The consumer callback is invoked for every key (even the ones handled below as value
        // commands) and before the internal handling, so that it observes the same key sequence a
        // plain input would report.
        await OnKeyDown.InvokeAsync(e);

        // Enter is the commit gesture of a text input, and it stays one even on a read-only field
        // (where there is nothing to commit but the consumer may still want to act on it), so it is
        // handled before the ReadOnly guard that stops the value-changing keys below.
        if (e.Key is "Enter" && HasModifier(e) is false && OnEnter.HasDelegate)
        {
            // The browser raises its change event on Enter, but only for text that actually differs
            // from what it last committed; reading the live text makes the callback observe the typed
            // value in every case, including the Immediate mode where nothing is pending at all.
            if (ReadOnly is false && InvalidValueBinding() is false)
            {
                await CommitPendingInputValue();

                if (IsDisposed) return;
            }

            await OnEnter.InvokeAsync(e);
            return;
        }

        if (ReadOnly || InvalidValueBinding()) return;

        switch (e.Key)
        {
            // A modifier turns an arrow into a text-editing command rather than a spin (Shift+ArrowUp
            // extends the selection to the start of the text, and Ctrl/Alt combinations belong to the
            // browser and the OS), so those are left alone - as they are for PageUp/PageDown below.
            case "ArrowUp":
                if (HasModifier(e)) return;
                // On key auto-repeat the input text cannot have changed since the previous step, so
                // the JS roundtrip that reads the live text is skipped.
                if (e.Repeat is false)
                {
                    await CommitPendingInputValue();
                }
                // The value is already sitting on the bound this key steps towards, exactly as for the
                // spin button that goes inert there: nothing changes, so nothing is reported either.
                if (IsSpinBlocked(isIncrement: true)) break;

                await ChangeValueAsync(+1);

                if (OnIncrement.HasDelegate)
                {
                    await OnIncrement.InvokeAsync(CurrentValue);
                }
                break;

            case "ArrowDown":
                if (HasModifier(e)) return;
                if (e.Repeat is false)
                {
                    await CommitPendingInputValue();
                }
                if (IsSpinBlocked(isIncrement: false)) break;

                await ChangeValueAsync(-1);

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
                if (IsSpinBlocked(isIncrement: true)) break;

                if (_hasPageStep) await ChangeValueAsync(+1, _pageStep); else await ChangeValueAsync(+10, _step);

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
                if (IsSpinBlocked(isIncrement: false)) break;

                if (_hasPageStep) await ChangeValueAsync(-1, _pageStep); else await ChangeValueAsync(-10, _step);

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
                await SetBoundValueAsync(_min);
                break;

            case "End":
                if (HasModifier(e) || _hasExplicitMax is false) return;
                await SetBoundValueAsync(_max);
                break;

            // The clear button is deliberately kept out of the tab order (like the spin buttons), so
            // Escape provides the keyboard path to the clear action, as it does in BitSearchBox. It
            // only acts when the clear button is actually rendered, i.e. there is something to clear.
            case "Escape":
                if (HasModifier(e) || ShowClearButton is false) return;
                if (HasVisibleText() is false) return;
                await HandleOnClearButtonClick();
                break;

            default:
                break;
        }
    }

    private static bool HasModifier(KeyboardEventArgs e) => e.ShiftKey || e.CtrlKey || e.AltKey || e.MetaKey;

    private async Task HandleOnKeyUp(KeyboardEventArgs e)
    {
        if (IsEnabled is false) return;

        await OnKeyUp.InvokeAsync(e);
    }

    private async Task HandleOnClick(MouseEventArgs e)
    {
        if (IsEnabled is false) return;

        await OnClick.InvokeAsync(e);
    }

    /// <summary>
    /// Clicking anywhere on the field - its prefix, suffix, icon or the padding around the text - puts
    /// the caret in the input, the way a native text field behaves. The input itself and the buttons
    /// take care of their own focus, so this only ever has an effect on the decorative parts.
    /// </summary>
    private async Task HandleOnInputContainerClick()
    {
        // Clicking the input itself already focused it, so the interop round trip is skipped there.
        if (IsEnabled is false || HideInput || _hasFocus) return;

        await InputElement.FocusAsync();
    }

    /// <summary>
    /// Activates an increment/decrement button from the keyboard. The buttons are driven by pointer
    /// events (to support the press-and-hold continuous spin), which the browser does not synthesize
    /// for a keyboard activation, so Enter/Space are handled explicitly. This only matters while the
    /// buttons are reachable by keyboard, i.e. in <see cref="HideInput"/> mode.
    /// </summary>
    private async Task HandleOnButtonKeyDown(KeyboardEventArgs e, bool isIncrement)
    {
        if (IsEnabled is false || ReadOnly || InvalidValueBinding()) return;

        if (e.Key is not ("Enter" or " " or "Spacebar")) return;

        if (IsSpinBlocked(isIncrement)) return;

        await ChangeValueAndInvokeEvents(isIncrement);
    }

    /// <summary>
    /// Sets the value straight to one of the range bounds (used by the Home/End keys).
    /// </summary>
    private async Task SetBoundValueAsync(TValue bound)
    {
        ClearPreservedDisplayValue();

        var previous = CurrentValue;

        CurrentValue = CheckMinAndMax(bound);

        StateHasChanged();

        await NotifyBoundReached(previous);
    }

    /// <summary>
    /// Raises <see cref="OnMaxReached"/>/<see cref="OnMinReached"/> when the value has just landed on
    /// a bound it was not already sitting on, so a consumer hears about the ceiling (or the floor)
    /// exactly once per approach rather than on every further attempt to pass it.
    /// </summary>
    private async Task NotifyBoundReached(TValue? previousValue)
    {
        if (OnMaxReached.HasDelegate && IsAtMax(CurrentValue) && IsAtMax(previousValue) is false)
        {
            await OnMaxReached.InvokeAsync(CurrentValue);
        }

        if (OnMinReached.HasDelegate && IsAtMin(CurrentValue) && IsAtMin(previousValue) is false)
        {
            await OnMinReached.InvokeAsync(CurrentValue);
        }
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
        catch (JSDisconnectedException) { } // the circuit is gone, the last committed value is used
        catch (InvalidOperationException) { } // JS interop is unavailable during prerendering
        catch (JSException) { } // the element is gone or the property read failed, the last committed value is used

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

        // The text kept visible only to keep the caret usable while typing (Immediate mode) has served
        // its purpose; leaving the field is the commit point where the canonical value must show.
        if (_displayValueIsTransient)
        {
            ClearPreservedDisplayValue();
        }

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

        // Focus belongs on the input: it is the element carrying the spinbutton role and its value, so
        // keeping it focused is what lets a screen reader announce each change and lets the user carry
        // on with the arrow keys after a click. The buttons themselves suppress the browser's default
        // focus-on-press (see the pointerdown preventDefault in the markup) so this is the only focus
        // move that happens. With HideInput there is no input to focus, and the pressed button - which
        // is then a real tab stop - takes the focus instead.
        if (HideInput)
        {
            await (isIncrement ? _buttonIncrement : _buttonDecrement).FocusAsync();
        }
        else
        {
            if (_hasFocus is false)
            {
                await InputElement.FocusAsync();
            }

            // Since the press no longer moves focus out of the input, the browser no longer raises the
            // change event that would have committed freshly typed text. The step therefore has to read
            // the live text itself, exactly as the arrow keys and the wheel do, so that typing a number
            // and then clicking a spin button steps from what is on screen rather than from the stale
            // previously committed value.
            await CommitPendingInputValue();

            if (IsDisposed) return;
        }

        // The pending text was committed just above, so this is the first point where it is known
        // whether the value the user is looking at still has room to move in this direction.
        if (IsSpinBlocked(isIncrement)) return;

        await ChangeValueAndInvokeEvents(isIncrement);

        if (IsDisposed) return;

        ResetCts();

        // The press-and-hold spin is deliberately not awaited: it lives as long as the button is held,
        // so awaiting it would leave the pointerdown event handler (and the render it drives) pending
        // for the whole duration of the press. Its lifetime is owned by the cancellation token source
        // instead, which HandleOnPointerUpOrOut and DisposeAsync cancel.
        _ = ContinuousChangeValueAfterDelay(isIncrement, _continuousChangeValueCts);
    }

    /// <summary>
    /// Waits out the <see cref="ContinuousSpinDelay"/> and then starts the continuous spin, unless the
    /// button was released (or the component went away) in the meantime.
    /// </summary>
    private async Task ContinuousChangeValueAfterDelay(bool isIncrement, CancellationTokenSource cts)
    {
        try
        {
            await Task.Delay(Math.Max(1, ContinuousSpinDelay), cts.Token);

            await InvokeAsync(() => ContinuousChangeValue(isIncrement, cts));
        }
        catch (OperationCanceledException) { } // the button was released before the continuous spin started
        catch (ObjectDisposedException) { } // the component was disposed while the delay was pending
    }

    private void HandleOnPointerUpOrOut()
    {
        ResetCts();
    }

    private async Task HandleOnMouseWheel(WheelEventArgs e)
    {
        if (IsEnabled is false || ReadOnly || NoMouseWheel || InvalidValueBinding()) return;
        if (e.ShiftKey is false) return;
        // The wheel only spins the value of the field the user is actually editing. Reacting to a
        // merely hovered field would silently change data while the user is scrolling the page.
        if (_hasFocus is false) return;

        if (e.DeltaY == 0) return;

        var isIncrement = (e.DeltaY < 0) != InvertMouseWheel;

        await CommitPendingInputValue();

        // As for the keys and the buttons: the wheel cannot push the value past a bound it already
        // sits on, so it neither changes anything nor reports anything.
        if (IsSpinBlocked(isIncrement)) return;

        // The wheel is a spin like any other, so it reports through OnIncrement/OnDecrement as the
        // buttons and the arrow keys do - a consumer listening for "the user stepped the value"
        // should not have to special-case the input device it came from.
        await ChangeValueAndInvokeEvents(isIncrement);
    }

    private async Task HandleOnClearButtonClick()
    {
        if (IsEnabled is false || ReadOnly) return;

        await ClearValue();

        await InputElement.FocusAsync();

        await OnClear.InvokeAsync();
    }

    /// <summary>
    /// Wipes whatever the field is showing - a value or a string that failed to parse. It is shared by
    /// the clear button, the Escape key and the public <see cref="ClearAsync"/>, none of which should
    /// differ in what "clearing" means.
    /// </summary>
    private async Task ClearValue()
    {
        ClearPreservedDisplayValue();

        await HandleOnStringValueChangeAsync(new() { Value = string.Empty });
    }



    /// <summary>
    /// Repeats the value change while an increment/decrement button is held down. It is an iterative
    /// loop rather than a self-recursive call so that a long press cannot pile up an ever growing
    /// chain of pending async continuations.
    /// </summary>
    private async Task ContinuousChangeValue(bool isIncrement, CancellationTokenSource cts)
    {
        while (cts.IsCancellationRequested is false && IsDisposed is false)
        {
            // A held button that has run the value into its bound has nothing left to do; stopping the
            // loop keeps it from spending the rest of the press re-raising OnIncrement/OnDecrement for
            // a value that no longer moves.
            if (IsSpinBlocked(isIncrement)) return;

            var valueBeforeStep = CurrentValue;

            await ChangeValueAndInvokeEvents(isIncrement);

            if (cts.IsCancellationRequested || IsDisposed) return;

            // A step that moved nothing will not move anything on the next pass either, so the press
            // has run out of room even though no explicit bound reports it. That happens whenever the
            // grid and the range disagree - SnapToStep with a Max that is not a multiple of the Step
            // pins the value just below it - and without this the held button would spin forever,
            // re-raising OnIncrement/OnDecrement for a value that never changes again.
            if (EqualityComparer<TValue>.Default.Equals(CurrentValue, valueBeforeStep)) return;

            StateHasChanged();

            try
            {
                await Task.Delay(Math.Max(1, ContinuousSpinInterval), cts.Token);
            }
            catch (OperationCanceledException)
            {
                // The button was released while the next tick was pending; ending the loop here stops
                // the spin right away instead of waiting the interval out first.
                return;
            }
        }
    }

    /// <summary>
    /// Whether a step in the given direction is pointless because the value already sits at the bound
    /// it would move towards. It is what turns the corresponding spin button into its "at the bound"
    /// state and what keeps that button from raising events that cannot change anything.
    /// </summary>
    private bool IsSpinBlocked(bool isIncrement) => isIncrement ? IsAtMax(CurrentValue) : IsAtMin(CurrentValue);

    private async Task ChangeValueAndInvokeEvents(bool isIncrement)
    {
        await ChangeValueAsync(isIncrement ? +1 : -1);

        if (isIncrement && OnIncrement.HasDelegate)
        {
            await OnIncrement.InvokeAsync(CurrentValue);
        }

        if (isIncrement is false && OnDecrement.HasDelegate)
        {
            await OnDecrement.InvokeAsync(CurrentValue);
        }
    }

    private Task ChangeValueAsync(int factor)
    {
        return ChangeValueAsync(factor, _step);
    }

    private async Task ChangeValueAsync(int factor, TValue step)
    {
        var previousValue = CurrentValue;

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
            var decimalStep = Convert.ToDecimal(step);

            decimal r;
            try
            {
                // The multiplication is inside the try as well: a PageUp with an oversized PageStep
                // can already blow the decimal range before anything is added to the current value.
                r = current + (factor * decimalStep);
            }
            catch (OverflowException)
            {
                // The delta itself is out of range, so its sign is the one of the direction the step
                // was going in (a negative Step reverses what the increment factor means).
                r = (factor > 0) == (decimalStep >= 0) ? Convert.ToDecimal(_max) : Convert.ToDecimal(_min);
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
        ClearPreservedDisplayValue();

        CurrentValue = result;

        StateHasChanged();

        await NotifyBoundReached(previousValue);
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
        // and the full decimal range). The bounds come ordered, which keeps the clamping sensible
        // even when a misconfigured Min is greater than Max (the effective range is simply swapped).
        var comparer = Comparer<TValue>.Default;
        var (min, max) = GetOrderedBounds();

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

    private void OnSetInputMode()
    {
        _inputMode = InputMode?.ToString().ToLowerInvariant() ?? _defaultInputMode;
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

        // An extreme negative precision overflows the scale to infinity, which would turn the scaled
        // rounding into NaN; the original value is kept as is (as RoundDecimal does on overflow).
        if (double.IsFinite(scale) is false) return value;

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
            ClearPreservedDisplayValue();
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
