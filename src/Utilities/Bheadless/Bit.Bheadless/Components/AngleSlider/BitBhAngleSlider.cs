using System.Globalization;
using System.Linq.Expressions;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;

namespace Bit.Bheadless;

/// <summary>
/// A headless dial that picks an angle - a rotation, a wind direction, a gradient angle (Ark UI's AngleSlider). The root is
/// the dial: a press or drag anywhere on it points the value at the pointer. A <see cref="BitBhAngleSliderThumb"/> inside it
/// is the focusable <c>role="slider"</c>; <see cref="BitBhAngleSliderMarker"/>s mark angles around it.
///
/// <para><b>Angles</b> run clockwise from the top (0 = 12 o'clock, 90 = 3 o'clock), from 0 up to, not including, 360.</para>
/// <para><b>Keyboard</b> (on the thumb) — → / ↑ add a <see cref="Step"/>, ← / ↓ take one away (wrapping past 0), Page Up / Page Down
/// and Shift + arrows move by <see cref="LargeStep"/>, Home / End go to 0 / the last step before 360.</para>
///
/// <code>
/// &lt;BitBhAngleSlider @bind-Value="angle" class="dial"&gt;
///     @foreach (var mark in new[] { 0, 90, 180, 270 })
///     {
///         &lt;BitBhAngleSliderMarker Value="mark" class="dial-mark" /&gt;
///     }
///     &lt;BitBhAngleSliderThumb aria-label="Rotation" class="dial-needle" /&gt;
/// &lt;/BitBhAngleSlider&gt;
/// </code>
///
/// <para><b>Styling hooks:</b> the root carries <c>--bit-bh-angle</c> (e.g. <c>90deg</c>) for the consumer's own drawing, and
/// <c>data-dragging</c>, <c>data-disabled</c>, <c>data-readonly</c>, <c>data-invalid</c>. The thumb and the markers fill the dial
/// (<c>position: absolute; inset: 0</c>) rotated to their angle, so a needle drawn at their top center points at it.</para>
/// </summary>
public class BitBhAngleSlider : BitBhComponentBase, IAsyncDisposable
{
    private BitBhFieldBinding? _binding;
    private double _value;
    private bool _dragging;
    private int _handle;
    private bool _attaching;
    private bool _disposed;
    private string? _thumbId;
    private DotNetObjectReference<Callbacks>? _ref;

    [CascadingParameter]
    private BitBhFieldContext? FieldContext { get; set; }

    [CascadingParameter]
    private BitBhFieldsetContext? FieldsetContext { get; set; }

    [CascadingParameter]
    private EditContext? EditContext { get; set; }

    /// <summary>The angle in degrees. Supplying it makes the slider controlled (use <c>@bind-Value</c>).</summary>
    [Parameter]
    public double Value { get; set; }

    /// <summary>Two-way binding callback for <see cref="Value"/>.</summary>
    [Parameter]
    public EventCallback<double> ValueChanged { get; set; }

    /// <summary>Identifies the bound field for <c>EditForm</c> validation.</summary>
    [Parameter]
    public Expression<Func<double>>? ValueExpression { get; set; }

    /// <summary>Initial angle when uncontrolled.</summary>
    [Parameter]
    public double DefaultValue { get; set; }

    /// <summary>Fires whenever the angle changes (while dragging too).</summary>
    [Parameter]
    public EventCallback<double> OnValueChange { get; set; }

    /// <summary>Fires once a change is done: on pointer up, or per key press - the place to save.</summary>
    [Parameter]
    public EventCallback<double> OnValueCommit { get; set; }

    /// <summary>The angle the value snaps to. Default: 1.</summary>
    [Parameter]
    public double Step { get; set; } = 1;

    /// <summary>The angle of Page Up / Page Down and Shift + arrows. Default: 15.</summary>
    [Parameter]
    public double LargeStep { get; set; } = 15;

    /// <summary>Formats the thumb's <c>aria-valuetext</c>. Default: <see cref="BitBhStrings.Angle"/> ("90 degrees").</summary>
    [Parameter]
    public Func<double, string>? ValueText { get; set; }

    /// <summary>Disables the slider.</summary>
    [Parameter]
    public bool Disabled { get; set; }

    /// <summary>Shows the angle without letting it change.</summary>
    [Parameter]
    public bool ReadOnly { get; set; }

    /// <summary>Marks the slider invalid (in addition to EditContext validation).</summary>
    [Parameter]
    public bool Invalid { get; set; }

    /// <summary>Form field name: a hidden input carries the angle.</summary>
    [Parameter]
    public string? Name { get; set; }

    /// <summary>The thumb, markers and anything drawn on the dial.</summary>
    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    protected override string DefaultTag => "div";

    /// <summary>The current angle, from 0 up to 360.</summary>
    public double CurrentValue => Normalize(IsProvided(nameof(Value)) ? Value : _value);

    internal bool IsDisabled => Disabled || (FieldContext?.Disabled ?? false) || (FieldsetContext?.Disabled ?? false);
    internal bool IsInvalid => Invalid || (FieldContext?.Invalid ?? false) || (_binding?.IsInvalid ?? false);
    internal bool IsReadOnly => ReadOnly;
    internal bool CanChange => !IsDisabled && !ReadOnly;
    internal BitBhFieldContext? Field => FieldContext;
    // The last step before 360 (Step 7: 357), so End and aria-valuemax are values a step can reach.
    internal double MaxValue => Math.Floor((360 - 1e-9) / StepSize) * StepSize;
    private double StepSize => Step > 0 && Step < 360 ? Step : 1;

    internal string FormatValueText(double value) => ValueText?.Invoke(value) ?? Strings.Angle(value);

    internal void RegisterThumb(string id) => _thumbId = id;

    protected override void OnInitialized()
    {
        _value = DefaultValue;
        FieldContext?.SetControlFocus(() => _thumbId is null ? Task.CompletedTask : Interop.FocusAsync(_thumbId).AsTask());
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

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (!firstRender) return;
        _attaching = true;
        _ref = DotNetObjectReference.Create(new Callbacks(this));
        _handle = await Interop.AreaAttachAsync(_ref, ComponentId);
        _attaching = false;
        if (_disposed) await Interop.AreaDetachAsync(_handle);
    }

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        builder.OpenElement(0, Tag);
        builder.AddAttribute(10, "id", ComponentId);
        builder.AddMultipleAttributes(20, GetFinalAttributes());
        builder.AddElementReferenceCapture(30, CaptureElement);
        builder.OpenComponent<CascadingValue<BitBhAngleSlider>>(40);
        builder.AddComponentParameter(41, "Value", this);
        builder.AddComponentParameter(42, "IsFixed", false);
        builder.AddComponentParameter(43, "ChildContent", ChildContent);
        builder.CloseComponent();
        builder.CloseElement();

        if (!string.IsNullOrEmpty(Name) && !IsDisabled)
        {
            builder.OpenElement(50, "input");
            builder.AddAttribute(51, "type", "hidden");
            builder.AddAttribute(52, "name", Name);
            builder.AddAttribute(53, "value", CurrentValue.ToString(CultureInfo.InvariantCulture));
            builder.CloseElement();
        }
    }

    protected override Dictionary<string, object> BuildComponentAttributes()
    {
        var attrs = base.BuildComponentAttributes();
        // Functional: the thumb and markers are placed against the dial, and a touch drag turns it rather than scrolls.
        attrs["style"] = $"position:relative;touch-action:none;user-select:none;--bit-bh-angle:{Deg(CurrentValue)}";
        SetDataFlag(attrs, "dragging", _dragging);
        SetDataFlag(attrs, "disabled", IsDisabled);
        SetDataFlag(attrs, "readonly", ReadOnly);
        SetDataFlag(attrs, "invalid", IsInvalid);
        return attrs;
    }

    internal static string Deg(double angle) => angle.ToString("0.###", CultureInfo.InvariantCulture) + "deg";

    internal async Task HandleKeyDownAsync(KeyboardEventArgs e)
    {
        if (!CanChange || e.CtrlKey || e.MetaKey || e.AltKey) return;
        // Steps count along the ring of values the step reaches (0, 7, ... 357 for Step 7), so a step past the last one
        // lands on 0 rather than on a value off the grid.
        var count = (int)Math.Round(MaxValue / StepSize) + 1;
        var index = (int)Math.Round(CurrentValue / StepSize) % count;
        var large = Math.Max(1, (int)Math.Round(LargeStep / StepSize));
        var by = e.ShiftKey ? large : 1;
        int? next = e.Key switch
        {
            "ArrowRight" or "ArrowUp" => index + by,
            "ArrowLeft" or "ArrowDown" => index - by,
            "PageUp" => index + large,
            "PageDown" => index - large,
            "Home" => 0,
            "End" => count - 1,
            _ => null,
        };
        if (next is not { } position) return;
        await OnValueCommit.InvokeAsync(await ApplyAsync((((position % count) + count) % count) * StepSize));
    }

    /// <summary>Sets the angle (wrapped into 0 - 360 and snapped to <see cref="Step"/>).</summary>
    public Task SetValueAsync(double angle) => ApplyAsync(angle);

    // Returns the angle set: a controlled Value only catches up when the parent renders again.
    private async Task<double> ApplyAsync(double angle)
    {
        if (!CanChange) return CurrentValue;
        var value = Snap(angle);
        if (Math.Abs(value - CurrentValue) < 1e-9) return value;
        if (!IsProvided(nameof(Value))) _value = value;
        await ValueChanged.InvokeAsync(value);
        await OnValueChange.InvokeAsync(value);
        _binding?.NotifyChanged();
        _binding?.ReportTo(FieldContext);
        StateHasChanged();
        return value;
    }

    // The nearest value a step reaches; past the last step that may be 0, across the top.
    private double Snap(double angle)
    {
        var a = Normalize(angle);
        var value = Math.Round(a / StepSize) * StepSize;
        if (value > MaxValue + 1e-9) value = 360 - a < a - MaxValue ? 0 : MaxValue;
        return Normalize(value);
    }

    internal static double Normalize(double angle)
    {
        if (double.IsNaN(angle) || double.IsInfinity(angle)) return 0;
        var a = angle % 360;
        if (a < 0) a += 360;
        // Rounding can land on 360 itself, which is 0.
        return a >= 360 - 1e-9 ? 0 : Math.Round(a, 6);
    }

    /// <summary>The angle a point of the dial stands at, from fractions of its width (left to right) and height (bottom to top).</summary>
    internal static double AngleAt(double x, double y)
    {
        var dx = x - 0.5;
        var dy = y - 0.5;
        if (Math.Abs(dx) < 1e-9 && Math.Abs(dy) < 1e-9) return 0;
        return Normalize(Math.Atan2(dx, dy) * 180 / Math.PI);
    }

    private sealed class Callbacks(BitBhAngleSlider owner)
    {
        /// <summary>The runtime calls it by this name: "start" / "move" / "end" with the pointer's fractions of the dial.</summary>
        [JSInvokable]
        public Task OnAreaPointer(string phase, double x, double y) => owner.RunFromRuntimeAsync(async () =>
        {
            if (owner._disposed) return;
            // Cleared even when the slider was disabled mid-drag, so data-dragging does not stick.
            owner._dragging = phase != "end" && owner.CanChange;
            if (!owner.CanChange)
            {
                owner.StateHasChanged();
                return;
            }
            var value = await owner.ApplyAsync(AngleAt(x, y));
            if (phase == "end") await owner.OnValueCommit.InvokeAsync(value);
            owner.StateHasChanged();
        });
    }

    public async ValueTask DisposeAsync()
    {
        _disposed = true;
        _binding?.Dispose();
        FieldContext?.SetControlFocus(null);
        if (!_attaching && _handle > 0) await Interop.AreaDetachAsync(_handle);
        _ref?.Dispose();
        GC.SuppressFinalize(this);
    }
}

/// <summary>
/// The focusable part of a <see cref="BitBhAngleSlider"/>: a <c>role="slider"</c> with the angle in <c>aria-valuenow</c>, filling the
/// dial (<c>position: absolute; inset: 0</c>) rotated to the angle - draw the needle at its top center (a child or <c>::before</c>).
/// Name it with <c>aria-label</c>, or put the slider in a <see cref="BitBhField"/> with a <see cref="BitBhLabel"/>.
/// <para><b>Styling hooks:</b> <c>data-dragging</c>, <c>data-disabled</c>, <c>data-readonly</c>, <c>data-invalid</c>,
/// <c>data-focus-visible</c>.</para>
/// </summary>
public class BitBhAngleSliderThumb : BitBhComponentBase
{
    [CascadingParameter]
    private BitBhAngleSlider? Slider { get; set; }

    /// <summary>Optional content (the needle).</summary>
    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    protected override string DefaultTag => "div";

    protected override void OnInitialized() => Slider?.RegisterThumb(ComponentId);

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        builder.OpenElement(0, Tag);
        builder.AddAttribute(10, "id", ComponentId);
        builder.AddMultipleAttributes(20, GetFinalAttributes());
        if (Slider is not null) builder.AddAttribute(30, "onkeydown", On<KeyboardEventArgs>("onkeydown", Slider.HandleKeyDownAsync));
        builder.AddElementReferenceCapture(40, CaptureElement);
        builder.AddContent(50, ChildContent);
        builder.CloseElement();
    }

    protected override Dictionary<string, object> BuildComponentAttributes()
    {
        var attrs = base.BuildComponentAttributes();
        attrs["role"] = "slider";
        if (Slider is null) return attrs;

        var value = Slider.CurrentValue;
        attrs["tabindex"] = Slider.IsDisabled ? "-1" : "0";
        attrs["aria-valuemin"] = "0";
        attrs["aria-valuemax"] = Slider.MaxValue.ToString(CultureInfo.InvariantCulture);
        attrs["aria-valuenow"] = value.ToString(CultureInfo.InvariantCulture);
        attrs["aria-valuetext"] = Slider.FormatValueText(value);
        if (Slider.IsDisabled) attrs["aria-disabled"] = "true";
        if (Slider.IsReadOnly) attrs["aria-readonly"] = "true";
        if (Slider.IsInvalid) attrs["aria-invalid"] = "true";
        if (Slider.Field?.LabelledBy is { } label && GetAdditionalAttribute("aria-label") is null) attrs["aria-labelledby"] = label;
        if (Slider.Field?.DescribedBy is { } description) attrs["aria-describedby"] = description;
        if (Slider.CanChange) attrs[BitBhKeyboard.KeysAttribute] = "ArrowLeft ArrowRight ArrowUp ArrowDown Home End PageUp PageDown";
        attrs["data-bit-bh-interactive"] = "";
        attrs["style"] = $"position:absolute;inset:0;transform:rotate({BitBhAngleSlider.Deg(value)})";
        SetDataFlag(attrs, "disabled", Slider.IsDisabled);
        SetDataFlag(attrs, "readonly", Slider.IsReadOnly);
        SetDataFlag(attrs, "invalid", Slider.IsInvalid);
        return attrs;
    }
}

/// <summary>
/// A mark at an angle of a <see cref="BitBhAngleSlider"/>'s dial: decorative (<c>aria-hidden</c>), filling the dial rotated to
/// <see cref="Value"/> like the thumb - draw the tick at its top center.
/// <para><b>Styling hooks:</b> <c>data-value</c>, <c>data-state="under-value" | "at-value" | "over-value"</c>, and
/// <c>--bit-bh-marker-angle</c> - a label turns upright with <c>transform: rotate(calc(-1 * var(--bit-bh-marker-angle)))</c>.</para>
/// </summary>
public class BitBhAngleSliderMarker : BitBhComponentBase
{
    [CascadingParameter]
    private BitBhAngleSlider? Slider { get; set; }

    /// <summary>The angle in degrees.</summary>
    [Parameter]
    public double Value { get; set; }

    /// <summary>Optional content (a label).</summary>
    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    protected override string DefaultTag => "span";

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        builder.OpenElement(0, Tag);
        builder.AddMultipleAttributes(10, GetFinalAttributes());
        builder.AddElementReferenceCapture(20, CaptureElement);
        builder.AddContent(30, ChildContent);
        builder.CloseElement();
    }

    protected override Dictionary<string, object> BuildComponentAttributes()
    {
        var attrs = base.BuildComponentAttributes();
        attrs["aria-hidden"] = "true";
        attrs["style"] = $"position:absolute;inset:0;pointer-events:none;transform:rotate({BitBhAngleSlider.Deg(Value)});--bit-bh-marker-angle:{BitBhAngleSlider.Deg(Value)}";
        SetDataValue(attrs, "value", Value.ToString(CultureInfo.InvariantCulture));
        if (Slider is not null)
        {
            // A marker at 360 stands where 0 does.
            var mark = BitBhAngleSlider.Normalize(Value);
            var current = Slider.CurrentValue;
            SetDataValue(attrs, "state", Math.Abs(mark - current) < 1e-9 ? "at-value" : mark < current ? "under-value" : "over-value");
        }
        return attrs;
    }
}
