using System.Globalization;
using System.Linq.Expressions;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Components.Rendering;

namespace Bit.Bheadless;

/// <summary>
/// A headless number input (WAI-ARIA Spinbutton pattern): a text input that understands numbers the way
/// the reader's culture writes them, with increment / decrement buttons, keyboard and wheel stepping and an
/// optional scrub area.
///
/// <para><b>Key features:</b></para>
/// <list type="bullet">
///   <item><b>Any numeric type</b> — <c>@bind-Value</c> an <c>int</c>, <c>decimal</c>, <c>double</c>, <c>long</c> …
///     or their nullable forms (an empty input is <c>null</c>). Stepping is exact (<c>0.1 + 0.2</c> is <c>0.3</c>).</item>
///   <item><b>Culture-aware</b> — <see cref="Format"/> is a .NET numeric format string (<c>"N2"</c>, <c>"C"</c>,
///     <c>"P0"</c>); typed text is parsed leniently in <see cref="Culture"/> (group separators, currency and percent
///     signs, the Unicode minus, native digits).</item>
///   <item><b>Keyboard</b> — ↑ / ↓ (± <see cref="Step"/>), Shift (± <see cref="LargeStep"/>), Alt (± <see cref="SmallStep"/>),
///     PageUp / PageDown, Home / End (to <see cref="Min"/> / <see cref="Max"/>), Enter commits.</item>
///   <item><b>Buttons</b> — <see cref="BitBhNumberFieldIncrement"/> / <see cref="BitBhNumberFieldDecrement"/> repeat while held,
///     stay out of the tab order and keep the focus in the input.</item>
///   <item><b>Forms</b> — <c>EditForm</c> validation, <see cref="BitBhField"/> label / description / error wiring and a hidden
///     input (<see cref="Name"/>) carrying the culture-invariant value.</item>
/// </list>
///
/// <code>
/// &lt;BitBhNumberField @bind-Value="quantity" Min="1" Max="99" class="number-field"&gt;
///     &lt;BitBhNumberFieldDecrement&gt;−&lt;/BitBhNumberFieldDecrement&gt;
///     &lt;BitBhNumberFieldInput aria-label="Quantity" /&gt;
///     &lt;BitBhNumberFieldIncrement&gt;+&lt;/BitBhNumberFieldIncrement&gt;
/// &lt;/BitBhNumberField&gt;
/// </code>
///
/// <para><b>Styling hooks:</b> <c>data-disabled</c>, <c>data-readonly</c>, <c>data-invalid</c>, <c>data-required</c>,
/// <c>data-focused</c>, <c>data-scrubbing</c> on the root and every part.</para>
/// </summary>
/// <typeparam name="TValue">A numeric type (<c>int</c>, <c>decimal</c>, <c>double</c> …) or its nullable form.</typeparam>
public class BitBhNumberField<TValue> : BitBhComponentBase, IDisposable
{
    private static readonly Type Underlying = Nullable.GetUnderlyingType(typeof(TValue)) ?? typeof(TValue);
    private static readonly bool IsNullableType = default(TValue) is null;
    private static readonly (decimal Min, decimal Max) TypeRange = RangeOf(Underlying);
    private static readonly bool IsIntegral = Type.GetTypeCode(Underlying) is TypeCode.Byte or TypeCode.SByte or TypeCode.Int16
        or TypeCode.UInt16 or TypeCode.Int32 or TypeCode.UInt32 or TypeCode.Int64 or TypeCode.UInt64;

    private readonly Context _context;
    private BitBhFieldBinding? _binding;
    private decimal? _value;
    private string? _text;
    private bool _focused;
    private bool _scrubbing;
    private bool _pendingCommit;

    [CascadingParameter]
    private BitBhFieldContext? FieldContext { get; set; }

    [CascadingParameter]
    private BitBhFieldsetContext? FieldsetContext { get; set; }

    [CascadingParameter]
    private EditContext? EditContext { get; set; }

    /// <summary>Creates the component.</summary>
    public BitBhNumberField() => _context = new Context(this);

    /// <summary>The value. Supplying it makes the field controlled (use <c>@bind-Value</c>).</summary>
    [Parameter]
    public TValue? Value { get; set; }

    /// <summary>Two-way binding callback for <see cref="Value"/>.</summary>
    [Parameter]
    public EventCallback<TValue?> ValueChanged { get; set; }

    /// <summary>Identifies the bound field for <c>EditForm</c> validation (set by <c>@bind-Value</c>).</summary>
    [Parameter]
    public Expression<Func<TValue?>>? ValueExpression { get; set; }

    /// <summary>Initial value when uncontrolled.</summary>
    [Parameter]
    public TValue? DefaultValue { get; set; }

    /// <summary>Fires whenever the value changes (every step, every committed edit).</summary>
    [Parameter]
    public EventCallback<TValue?> OnValueChange { get; set; }

    /// <summary>
    /// Fires when a change is committed: the input is left or Enter is pressed after typing, a key steps the
    /// value, or a held button / a scrub is released. Use it for expensive work instead of <see cref="OnValueChange"/>.
    /// </summary>
    [Parameter]
    public EventCallback<TValue?> OnValueCommit { get; set; }

    /// <summary>The minimum value. Home sets it. Default: none (the type's own minimum).</summary>
    [Parameter]
    public double? Min { get; set; }

    /// <summary>The maximum value. End sets it. Default: none (the type's own maximum).</summary>
    [Parameter]
    public double? Max { get; set; }

    /// <summary>The step of the arrow keys and buttons. Default: 1 (0.01 in a percent <see cref="Format"/>).</summary>
    [Parameter]
    public double? Step { get; set; }

    /// <summary>The step of Shift + arrows and PageUp / PageDown. Default: 10 steps.</summary>
    [Parameter]
    public double? LargeStep { get; set; }

    /// <summary>The step of Alt + arrows. Default: a tenth of a step (one step for integral types).</summary>
    [Parameter]
    public double? SmallStep { get; set; }

    /// <summary>
    /// Snaps a stepped value onto the step grid (multiples of <see cref="Step"/> from <see cref="Min"/>): 1.3 steps up to
    /// 2 rather than 2.3. Default: false.
    /// </summary>
    [Parameter]
    public bool SnapOnStep { get; set; }

    /// <summary>Clamps a typed value into <see cref="Min"/> / <see cref="Max"/> when it is committed. Default: true.</summary>
    [Parameter]
    public bool ClampOnBlur { get; set; } = true;

    /// <summary>
    /// A standard .NET numeric format string for the value shown in the input and announced
    /// (<c>aria-valuetext</c>): <c>"N0"</c>, <c>"N2"</c>, <c>"C"</c>, <c>"P1"</c> … Default: the shortest round-trip text.
    /// </summary>
    [Parameter]
    public string? Format { get; set; }

    /// <summary>
    /// The culture numbers are written and read in. Default: <see cref="CultureInfo.CurrentCulture"/>. An app built with
    /// <c>InvariantGlobalization</c> (common for WebAssembly) has the invariant culture only: give it a clone of
    /// <see cref="CultureInfo.InvariantCulture"/> whose <see cref="CultureInfo.NumberFormat"/> has the separators and
    /// currency you need.
    /// </summary>
    [Parameter]
    public CultureInfo? Culture { get; set; }

    /// <summary>Lets the mouse wheel step the value while the input has focus. Default: false.</summary>
    [Parameter]
    public bool AllowWheelScrub { get; set; }

    /// <summary>Pixels of pointer movement per step in a <see cref="BitBhNumberFieldScrubArea"/>. Default: 2.</summary>
    [Parameter]
    public double ScrubPixelSensitivity { get; set; } = 2;

    /// <summary>Disables the field.</summary>
    [Parameter]
    public bool Disabled { get; set; }

    /// <summary>Makes the field read-only: focusable and announced, but its value cannot change.</summary>
    [Parameter]
    public bool ReadOnly { get; set; }

    /// <summary>Marks the field invalid (in addition to EditContext validation).</summary>
    [Parameter]
    public bool Invalid { get; set; }

    /// <summary>Marks the field required.</summary>
    [Parameter]
    public bool Required { get; set; }

    /// <summary>Form field name: a hidden input carries the culture-invariant value.</summary>
    [Parameter]
    public string? Name { get; set; }

    /// <summary>The input, the increment / decrement buttons and anything else.</summary>
    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    protected override string DefaultTag => "div";

    /// <summary>The current value.</summary>
    public TValue? CurrentValue => FromDecimal(CurrentDecimal);

    private decimal? CurrentDecimal => IsProvided(nameof(Value)) ? ToDecimal(Value) : _value;

    private CultureInfo EffectiveCulture => Culture ?? CultureInfo.CurrentCulture;
    private bool IsPercent => BitBhNumberParser.IsPercent(Format);
    private decimal EffectiveStep => Whole(Step is > 0 and var s && double.IsFinite(s) ? (decimal)s : IsPercent ? 0.01m : 1m);
    private decimal EffectiveLargeStep => Whole(LargeStep is > 0 and var s && double.IsFinite(s) ? (decimal)s : EffectiveStep * 10);
    private decimal EffectiveSmallStep => Whole(SmallStep is > 0 and var s && double.IsFinite(s) ? (decimal)s : IsIntegral ? EffectiveStep : EffectiveStep / 10);
    // An integral type steps by whole numbers: a step of 0.5 would round straight back to where it started.
    private static decimal Whole(decimal step) => IsIntegral ? Math.Max(1, Math.Round(step, MidpointRounding.AwayFromZero)) : step;
    // The bounds the consumer set (announced, and what Home / End go to) and the ones values are kept in (those, or
    // the type's own limits). An integral type keeps whole bounds inside them: Min 1.4 is 2, Max 9.6 is 9.
    private decimal? ExplicitMin => Min is { } m && double.IsFinite(m)
        ? Math.Max(IsIntegral ? Math.Ceiling(ToDecimalSaturated(m)) : ToDecimalSaturated(m), TypeRange.Min) : null;
    private decimal? ExplicitMax => Max is { } m && double.IsFinite(m)
        ? Math.Min(IsIntegral ? Math.Floor(ToDecimalSaturated(m)) : ToDecimalSaturated(m), TypeRange.Max) : null;
    private decimal ClampMin => ExplicitMin ?? TypeRange.Min;
    private decimal ClampMax => ExplicitMax ?? TypeRange.Max;
    private bool IsDisabled => Disabled || (FieldContext?.Disabled ?? false) || (FieldsetContext?.Disabled ?? false);
    private bool IsInvalid => Invalid || (FieldContext?.Invalid ?? false) || (_binding?.IsInvalid ?? false);
    private bool IsRequired => Required || (FieldContext?.Required ?? false);
    private string? _inputId;
    private string InputId => _inputId ?? FieldContext?.ControlId ?? $"{ComponentId}-input";

    protected override void OnInitialized()
    {
        _value = ToDecimal(DefaultValue);
    }

    protected override void OnParametersSet()
    {
        if (ValueExpression is not null || _binding is not null)
        {
            _binding ??= new BitBhFieldBinding(() => InvokeAsync(OnValidationStateChanged));
            _binding.Update(EditContext, ValueExpression);
            _binding.ReportTo(FieldContext);
        }
    }

    private void OnValidationStateChanged()
    {
        _binding?.ReportTo(FieldContext);
        StateHasChanged();
    }

    /// <summary>Focuses the input.</summary>
    public Task FocusAsync() => Interop.FocusAsync(InputId).AsTask();

    /// <summary>Steps the value up by one <see cref="Step"/>.</summary>
    public Task IncrementAsync() => _context.StepAsync(1, BitBhStepSize.Normal, commit: true);

    /// <summary>Steps the value down by one <see cref="Step"/>.</summary>
    public Task DecrementAsync() => _context.StepAsync(-1, BitBhStepSize.Normal, commit: true);

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        builder.OpenElement(0, Tag);
        builder.AddAttribute(10, "id", ComponentId);
        builder.AddMultipleAttributes(20, GetFinalAttributes());
        builder.AddElementReferenceCapture(30, CaptureElement);
        builder.OpenComponent<CascadingValue<BitBhNumberFieldContext>>(40);
        builder.AddComponentParameter(41, "Value", (BitBhNumberFieldContext)_context);
        // Not fixed: the parts render the value, which changes without them getting new parameters.
        builder.AddComponentParameter(42, "IsFixed", false);
        builder.AddComponentParameter(43, "ChildContent", ChildContent);
        builder.CloseComponent();
        builder.CloseElement();

        // Like a native disabled control, a disabled field posts nothing.
        if (!string.IsNullOrEmpty(Name) && !IsDisabled)
        {
            builder.OpenElement(60, "input");
            builder.AddAttribute(61, "type", "hidden");
            builder.AddAttribute(62, "name", Name);
            builder.AddAttribute(63, "value", CurrentDecimal?.ToString(CultureInfo.InvariantCulture) ?? string.Empty);
            builder.CloseElement();
        }
    }

    protected override Dictionary<string, object> BuildComponentAttributes()
    {
        var attrs = base.BuildComponentAttributes();
        AddStateAttributes(attrs);
        return attrs;
    }

    internal void AddStateAttributes(Dictionary<string, object> attrs)
    {
        SetDataFlag(attrs, "disabled", IsDisabled);
        SetDataFlag(attrs, "readonly", ReadOnly);
        SetDataFlag(attrs, "invalid", IsInvalid);
        SetDataFlag(attrs, "required", IsRequired);
        SetDataFlag(attrs, "focused", _focused);
        SetDataFlag(attrs, "scrubbing", _scrubbing);
    }

    // ── Value changes ────────────────────────────────────────────────────────

    private decimal? Clamp(decimal? value)
    {
        if (value is not { } v) return null;
        // Max wins over a Min above it, as it does for a native range input.
        return Math.Min(Math.Max(v, ClampMin), ClampMax);
    }

    private decimal? Normalize(decimal? value)
    {
        if (value is not { } v) return null;
        v = Math.Clamp(v, TypeRange.Min, TypeRange.Max);
        // An integral type can't hold 2.5: the field shows and reports what the type keeps.
        if (IsIntegral) v = Math.Round(v, MidpointRounding.AwayFromZero);
        return v;
    }

    private async Task SetValueAsync(decimal? value, bool commit)
    {
        _text = null;
        value = Normalize(value);
        if (value is null && !IsNullableType)
        {
            // A non-nullable value can't be emptied: the input goes back to the value.
            StateHasChanged();
            return;
        }

        var changed = value != CurrentDecimal;
        if (!IsProvided(nameof(Value))) _value = value;
        if (changed)
        {
            var typed = FromDecimal(value);
            await ValueChanged.InvokeAsync(typed);
            await OnValueChange.InvokeAsync(typed);
            _binding?.NotifyChanged();
            _binding?.ReportTo(FieldContext);
            _pendingCommit = true;
        }
        if (commit && _pendingCommit)
        {
            _pendingCommit = false;
            await OnValueCommit.InvokeAsync(FromDecimal(value));
        }
        StateHasChanged();
    }

    private decimal StepOf(BitBhStepSize size) => size switch
    {
        BitBhStepSize.Large => EffectiveLargeStep,
        BitBhStepSize.Small => EffectiveSmallStep,
        _ => EffectiveStep,
    };

    private decimal? NextStepped(decimal? current, int direction, BitBhStepSize size)
    {
        if (current is not { } value)
        {
            // An empty field starts from the bound it moves away from, or from 0.
            return Clamp((direction > 0 ? ExplicitMin : ExplicitMax) ?? 0);
        }

        var step = StepOf(size);
        decimal next;
        if (SnapOnStep)
        {
            var origin = ExplicitMin ?? 0;
            var steps = (value - origin) / step;
            var snapped = direction > 0 ? Math.Floor(steps) + 1 : Math.Ceiling(steps) - 1;
            next = origin + snapped * step;
        }
        else
        {
            next = value + direction * step;
        }
        return Clamp(next);
    }

    private sealed class Context(BitBhNumberField<TValue> owner) : BitBhNumberFieldContext
    {
        public override string InputId => owner.InputId;
        public override decimal? NumericValue => owner.CurrentDecimal;
        public override string DisplayText => owner._text ?? ValueText ?? string.Empty;
        public override string? ValueText => owner.CurrentDecimal is { } v ? BitBhNumberParser.Format(v, owner.Format, owner.EffectiveCulture) : null;
        public override decimal? MinValue => owner.ExplicitMin;
        public override decimal? MaxValue => owner.ExplicitMax;
        public override bool IsDisabled => owner.IsDisabled;
        public override bool IsReadOnly => owner.ReadOnly;
        public override bool IsInvalid => owner.IsInvalid;
        public override bool IsRequired => owner.IsRequired;
        public override bool IsScrubbing => owner._scrubbing;
        public override bool IsFocused => owner._focused;
        public override bool CanIncrement => !IsDisabled && !IsReadOnly && (owner.CurrentDecimal is not { } v || v < owner.ClampMax);
        public override bool CanDecrement => !IsDisabled && !IsReadOnly && (owner.CurrentDecimal is not { } v || v > owner.ClampMin);
        internal override BitBhFieldContext? Field => owner.FieldContext;
        internal override bool AllowWheelScrub => owner.AllowWheelScrub;
        internal override bool HasHomeEnd => owner.ExplicitMin is not null || owner.ExplicitMax is not null;
        internal override double ScrubPixelSensitivity => owner.ScrubPixelSensitivity > 0 ? owner.ScrubPixelSensitivity : 2;
        internal override string AllowedCharacters => BitBhNumberParser.AllowedCharacters(owner.Format, owner.EffectiveCulture);

        internal override string InputMode
            => IsIntegral && owner.ClampMin >= 0 && !owner.IsPercent ? "numeric" : "decimal";

        internal override void SetInputId(string? id)
        {
            if (owner._inputId == id) return;
            owner._inputId = id;
            owner.StateHasChanged();
        }

        internal override Task OnTextInputAsync(string? text)
        {
            owner._text = text ?? string.Empty;
            owner.StateHasChanged();
            return Task.CompletedTask;
        }

        internal override async Task CommitTextAsync()
        {
            if (owner._text is not { } text) return;
            if (string.IsNullOrWhiteSpace(text))
            {
                await owner.SetValueAsync(null, commit: true);
                return;
            }
            if (BitBhNumberParser.TryParse(text, owner.Format, owner.EffectiveCulture, out var parsed))
            {
                await owner.SetValueAsync(owner.ClampOnBlur ? owner.Clamp(parsed) : parsed, commit: true);
                return;
            }
            // Not a number: the input goes back to the value it had.
            owner._text = null;
            owner.StateHasChanged();
        }

        internal override async Task StepAsync(int direction, BitBhStepSize size, bool commit)
        {
            if (IsDisabled || IsReadOnly) return;
            // Text being typed is what the step starts from.
            var current = owner.CurrentDecimal;
            if (owner._text is { } text)
                current = BitBhNumberParser.TryParse(text, owner.Format, owner.EffectiveCulture, out var parsed) ? owner.Clamp(parsed) : string.IsNullOrWhiteSpace(text) ? null : current;
            await owner.SetValueAsync(owner.NextStepped(current, direction, size), commit);
        }

        internal override Task SetToBoundAsync(bool max)
        {
            if (IsDisabled || IsReadOnly) return Task.CompletedTask;
            var bound = max ? owner.ExplicitMax : owner.ExplicitMin;
            return bound is null ? Task.CompletedTask : owner.SetValueAsync(bound, commit: true);
        }

        internal override async Task CommitAsync()
        {
            if (!owner._pendingCommit) return;
            owner._pendingCommit = false;
            await owner.OnValueCommit.InvokeAsync(owner.CurrentValue);
        }

        internal override void SetScrubbing(bool scrubbing)
        {
            if (owner._scrubbing == scrubbing) return;
            owner._scrubbing = scrubbing;
            owner.StateHasChanged();
        }

        internal override void SetFocused(bool focused)
        {
            if (owner._focused == focused) return;
            owner._focused = focused;
            owner.StateHasChanged();
        }
    }

    // ── Conversions ──────────────────────────────────────────────────────────

    private static decimal? ToDecimal(TValue? value)
    {
        if (value is null) return null;
        try
        {
            return value switch
            {
                double d => double.IsFinite(d) ? ToDecimalSaturated(d) : null,
                float f => float.IsFinite(f) ? ToDecimalSaturated(f) : null,
                _ => Convert.ToDecimal(value, CultureInfo.InvariantCulture),
            };
        }
        catch (Exception e) when (e is OverflowException or InvalidCastException or FormatException)
        {
            return null;
        }
    }

    private static decimal ToDecimalSaturated(double value)
        => value >= (double)decimal.MaxValue ? decimal.MaxValue : value <= (double)decimal.MinValue ? decimal.MinValue : (decimal)value;

    private static TValue? FromDecimal(decimal? value)
    {
        if (value is not { } v) return default;
        return Type.GetTypeCode(Underlying) switch
        {
            TypeCode.Double => (TValue)(object)(double)v,
            TypeCode.Single => (TValue)(object)(float)v,
            TypeCode.Decimal => (TValue)(object)v,
            _ => (TValue)Convert.ChangeType(v, Underlying, CultureInfo.InvariantCulture),
        };
    }

    private static (decimal Min, decimal Max) RangeOf(Type type) => Type.GetTypeCode(type) switch
    {
        TypeCode.Byte => (byte.MinValue, byte.MaxValue),
        TypeCode.SByte => (sbyte.MinValue, sbyte.MaxValue),
        TypeCode.Int16 => (short.MinValue, short.MaxValue),
        TypeCode.UInt16 => (ushort.MinValue, ushort.MaxValue),
        TypeCode.Int32 => (int.MinValue, int.MaxValue),
        TypeCode.UInt32 => (uint.MinValue, uint.MaxValue),
        TypeCode.Int64 => (long.MinValue, long.MaxValue),
        TypeCode.UInt64 => (ulong.MinValue, ulong.MaxValue),
        TypeCode.Single or TypeCode.Double or TypeCode.Decimal => (decimal.MinValue, decimal.MaxValue),
        _ => throw new NotSupportedException($"BitBhNumberField does not support {type.Name}: use a numeric type such as int, decimal or double."),
    };

    public virtual void Dispose() => _binding?.Dispose();
}
