using System.Globalization;
using System.Linq.Expressions;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;

namespace Bit.Bheadless;

/// <summary>How a <see cref="BitBhRatingItem"/> is filled.</summary>
public enum BitBhRatingItemState
{
    /// <summary>Not filled.</summary>
    Empty,

    /// <summary>Half filled (with <c>AllowHalf</c>).</summary>
    Half,

    /// <summary>Filled.</summary>
    Full,
}

/// <summary>What a <see cref="BitBhRatingItem"/>'s <c>Template</c> receives.</summary>
/// <param name="Index">The item's position (0-based): it stands for the values <c>Index + 0.5</c> and <c>Index + 1</c>.</param>
/// <param name="State">How the item is filled for the value shown (the hovered one while previewing).</param>
/// <param name="IsPreview">Whether the value shown is a pointer preview rather than the value.</param>
public sealed record BitBhRatingItemContext(int Index, BitBhRatingItemState State, bool IsPreview);

/// <summary>
/// A headless star rating. One focusable control (<c>role="slider"</c>, like Bits UI's RatingGroup - which lets
/// half values be announced and set) over decorative <see cref="BitBhRatingItem"/>s.
///
/// <para><b>Key features:</b></para>
/// <list type="bullet">
///   <item><b>Keyboard</b> — ← / → / ↑ / ↓ step by one (or a half with <see cref="AllowHalf"/>), Home / End go to 0 / <see cref="Max"/>,
///     digit keys set the value; mirrored in RTL.</item>
///   <item><b>Pointer</b> — hovering previews the value (<c>data-highlighted</c>, the items' <c>data-state</c>), a click sets it, a
///     click on the current value clears it (<see cref="AllowClear"/>); the left half of an item picks a half value.</item>
///   <item><b>Forms</b> — <c>EditForm</c> validation, <see cref="BitBhField"/> wiring and a hidden input (<see cref="Name"/>).</item>
/// </list>
///
/// <code>
/// &lt;BitBhRating @bind-Value="stars" aria-label="Rating" class="rating"&gt;
///     @for (var i = 0; i &lt; 5; i++)
///     {
///         &lt;BitBhRatingItem Index="i" class="star"&gt;★&lt;/BitBhRatingItem&gt;
///     }
/// &lt;/BitBhRating&gt;
/// </code>
///
/// <para><b>Styling hooks:</b> <c>data-state="full|half|empty"</c>, <c>data-highlighted</c> (pointer preview) and <c>data-index</c> on
/// items; <c>data-disabled</c>, <c>data-readonly</c>, <c>data-invalid</c>, <c>data-hovering</c> on the root.</para>
/// </summary>
public class BitBhRating : BitBhComponentBase, IAsyncDisposable
{
    private BitBhFieldBinding? _binding;
    private double _value;
    private double? _preview;
    private int _handle;
    private bool _attached;
    private bool _disposed;
    private DotNetObjectReference<PointerCallbacks>? _ref;

    [CascadingParameter]
    private BitBhFieldContext? FieldContext { get; set; }

    [CascadingParameter]
    private BitBhFieldsetContext? FieldsetContext { get; set; }

    [CascadingParameter]
    private EditContext? EditContext { get; set; }

    [CascadingParameter]
    private BitBhDirection CascadedDir { get; set; }

    /// <summary>The rating (0 = none). Supplying it makes the rating controlled (use <c>@bind-Value</c>).</summary>
    [Parameter]
    public double Value { get; set; }

    /// <summary>Two-way binding callback for <see cref="Value"/>.</summary>
    [Parameter]
    public EventCallback<double> ValueChanged { get; set; }

    /// <summary>Identifies the bound field for <c>EditForm</c> validation.</summary>
    [Parameter]
    public Expression<Func<double>>? ValueExpression { get; set; }

    /// <summary>Initial rating when uncontrolled.</summary>
    [Parameter]
    public double DefaultValue { get; set; }

    /// <summary>Fires whenever the rating changes.</summary>
    [Parameter]
    public EventCallback<double> OnValueChange { get; set; }

    /// <summary>Fires when the pointer previews a value (null when it leaves).</summary>
    [Parameter]
    public EventCallback<double?> OnHoverChange { get; set; }

    /// <summary>The number of items (the highest rating). Default: 5.</summary>
    [Parameter]
    public int Max { get; set; } = 5;

    /// <summary>Allows half values (3.5). Default: false.</summary>
    [Parameter]
    public bool AllowHalf { get; set; }

    /// <summary>A click on the current value clears the rating. Default: true.</summary>
    [Parameter]
    public bool AllowClear { get; set; } = true;

    /// <summary>Formats <c>aria-valuetext</c>. Default: <see cref="BitBhStrings.RatingItem"/> ("3 stars").</summary>
    [Parameter]
    public Func<double, string>? ValueText { get; set; }

    /// <summary>Disables the rating.</summary>
    [Parameter]
    public bool Disabled { get; set; }

    /// <summary>Makes the rating read-only (shown and announced, not changeable).</summary>
    [Parameter]
    public bool ReadOnly { get; set; }

    /// <summary>Marks the rating invalid (in addition to EditContext validation).</summary>
    [Parameter]
    public bool Invalid { get; set; }

    /// <summary>Marks the rating required.</summary>
    [Parameter]
    public bool Required { get; set; }

    /// <summary>Form field name: a hidden input carries the rating.</summary>
    [Parameter]
    public string? Name { get; set; }

    /// <summary>Reading direction (mirrors ← / → and the pointer). Inherits from <see cref="BitBhDirectionProvider"/>.</summary>
    [Parameter]
    public BitBhDirection? Dir { get; set; }

    /// <summary>The <see cref="BitBhRatingItem"/>s.</summary>
    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    protected override string DefaultTag => "div";

    /// <summary>The current rating.</summary>
    public double CurrentValue => Math.Clamp(IsProvided(nameof(Value)) ? Value : _value, 0, Math.Max(1, Max));

    private double StepSize => AllowHalf ? 0.5 : 1;
    private bool IsDisabled => Disabled || (FieldContext?.Disabled ?? false) || (FieldsetContext?.Disabled ?? false);
    private bool IsInvalid => Invalid || (FieldContext?.Invalid ?? false) || (_binding?.IsInvalid ?? false);
    private bool IsRequired => Required || (FieldContext?.Required ?? false);
    private bool CanChange => !IsDisabled && !ReadOnly;
    private BitBhDirection EffectiveDir => Dir ?? CascadedDir;

    internal double ShownValue => _preview ?? CurrentValue;
    internal bool IsPreviewing => _preview is not null;

    protected override void OnInitialized()
    {
        _value = DefaultValue;
        FieldContext?.SetControlFocus(this, () => Interop.FocusAsync(ComponentId).AsTask());
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
        _ref = DotNetObjectReference.Create(new PointerCallbacks(this));
        _attached = true;
        _handle = await Interop.RatingAttachAsync(_ref, ComponentId);
        _attached = false;
        if (_disposed) await Interop.RatingDetachAsync(_handle);
    }

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        builder.OpenElement(0, Tag);
        builder.AddAttribute(10, "id", ComponentId);
        builder.AddMultipleAttributes(20, GetFinalAttributes());
        builder.AddAttribute(30, "onkeydown", On<KeyboardEventArgs>("onkeydown", HandleKeyDownAsync));
        builder.AddElementReferenceCapture(40, CaptureElement);
        builder.OpenComponent<CascadingValue<BitBhRating>>(50);
        builder.AddComponentParameter(51, "Value", this);
        builder.AddComponentParameter(52, "IsFixed", false);
        builder.AddComponentParameter(53, "ChildContent", ChildContent);
        builder.CloseComponent();
        builder.CloseElement();

        if (!string.IsNullOrEmpty(Name) && !IsDisabled)
        {
            builder.OpenElement(60, "input");
            builder.AddAttribute(61, "type", "hidden");
            builder.AddAttribute(62, "name", Name);
            builder.AddAttribute(63, "value", CurrentValue.ToString(CultureInfo.InvariantCulture));
            builder.CloseElement();
        }
    }

    protected override Dictionary<string, object> BuildComponentAttributes()
    {
        var attrs = base.BuildComponentAttributes();
        var value = CurrentValue;
        attrs["role"] = "slider";
        attrs["tabindex"] = IsDisabled ? "-1" : "0";
        attrs["aria-valuemin"] = "0";
        attrs["aria-valuemax"] = Math.Max(1, Max).ToString(CultureInfo.InvariantCulture);
        attrs["aria-valuenow"] = value.ToString(CultureInfo.InvariantCulture);
        attrs["aria-valuetext"] = ValueText?.Invoke(value) ?? Strings.RatingItem(value, Max);
        if (IsDisabled) attrs["aria-disabled"] = "true";
        if (ReadOnly) attrs["aria-readonly"] = "true";
        if (IsInvalid) attrs["aria-invalid"] = "true";
        if (FieldContext?.LabelledBy is { } label && GetAdditionalAttribute("aria-label") is null) attrs["aria-labelledby"] = label;
        if (FieldContext?.DescribedBy is { } description) attrs["aria-describedby"] = description;
        if (Dir is { } dir) attrs["dir"] = dir.ToAttribute();
        if (CanChange) attrs[BitBhKeyboard.KeysAttribute] = "ArrowLeft ArrowRight ArrowUp ArrowDown Home End PageUp PageDown";
        attrs["data-bit-bh-rating"] = CanChange ? (AllowHalf ? "half" : "whole") : "off";
        if (EffectiveDir == BitBhDirection.Rtl) attrs["data-bit-bh-rtl"] = "";
        attrs["data-bit-bh-interactive"] = "";
        SetDataFlag(attrs, "disabled", IsDisabled);
        SetDataFlag(attrs, "readonly", ReadOnly);
        SetDataFlag(attrs, "invalid", IsInvalid);
        SetDataFlag(attrs, "required", IsRequired);
        SetDataFlag(attrs, "hovering", _preview is not null);
        return attrs;
    }

    internal BitBhRatingItemState StateOf(int index)
    {
        var shown = ShownValue;
        if (shown >= index + 1) return BitBhRatingItemState.Full;
        if (shown >= index + 0.5) return BitBhRatingItemState.Half;
        return BitBhRatingItemState.Empty;
    }

    private async Task HandleKeyDownAsync(KeyboardEventArgs e)
    {
        if (!CanChange || e.CtrlKey || e.MetaKey || e.AltKey) return;
        var rtl = EffectiveDir == BitBhDirection.Rtl;
        var current = CurrentValue;
        double? next = e.Key switch
        {
            "ArrowRight" => current + (rtl ? -StepSize : StepSize),
            "ArrowLeft" => current - (rtl ? -StepSize : StepSize),
            "ArrowUp" or "PageUp" => current + StepSize,
            "ArrowDown" or "PageDown" => current - StepSize,
            "Home" => 0,
            "End" => Max,
            _ when e.Key.Length == 1 && char.IsDigit(e.Key[0]) => char.GetNumericValue(e.Key[0]),
            _ => null,
        };
        if (next is { } v) await SetValueAsync(v);
    }

    /// <summary>Sets the rating (clamped and rounded to whole or half values).</summary>
    public async Task SetValueAsync(double value)
    {
        if (!CanChange) return;
        value = Math.Clamp(Math.Round(value / StepSize) * StepSize, 0, Math.Max(1, Max));
        if (value == CurrentValue) return;
        if (!IsProvided(nameof(Value))) _value = value;
        await ValueChanged.InvokeAsync(value);
        await OnValueChange.InvokeAsync(value);
        _binding?.NotifyChanged();
        _binding?.ReportTo(FieldContext);
        StateHasChanged();
    }

    private sealed class PointerCallbacks(BitBhRating owner)
    {
        /// <summary>The runtime calls it by this name: "hover" (a value), "leave" or "pick" (a value).</summary>
        [JSInvokable]
        public Task OnRatingPointer(string phase, double value) => owner.RunFromRuntimeAsync(() => owner.HandlePointerAsync(phase, value));
    }

    private async Task HandlePointerAsync(string phase, double value)
    {
        if (_disposed || !CanChange) return;
        switch (phase)
        {
            case "hover":
                if (_preview == value) return;
                _preview = value;
                await OnHoverChange.InvokeAsync(value);
                StateHasChanged();
                break;
            case "leave":
                if (_preview is null) return;
                _preview = null;
                await OnHoverChange.InvokeAsync(null);
                StateHasChanged();
                break;
            case "pick":
                await PickAsync(value);
                break;
        }
    }

    /// <summary>A pointer picked <paramref name="value"/>: the current value again clears the rating.</summary>
    internal Task PickAsync(double value)
        => SetValueAsync(AllowClear && value == CurrentValue ? 0 : value);

    public async ValueTask DisposeAsync()
    {
        _disposed = true;
        _binding?.Dispose();
        FieldContext?.ReleaseControlFocus(this);
        if (!_attached && _handle > 0) await Interop.RatingDetachAsync(_handle);
        _ref?.Dispose();
        GC.SuppressFinalize(this);
    }
}

/// <summary>
/// One item (a star) of a <see cref="BitBhRating"/>: decorative (<c>aria-hidden</c>), its fill exposed as
/// <c>data-state="full|half|empty"</c> and to its <see cref="Template"/>. It stands for the value <see cref="Index"/> + 1
/// (and <see cref="Index"/> + 0.5 on its first half with <c>AllowHalf</c>).
/// </summary>
public class BitBhRatingItem : BitBhComponentBase
{
    [CascadingParameter]
    private BitBhRating? Rating { get; set; }

    /// <summary>The item's position (0-based).</summary>
    [Parameter, EditorRequired]
    public int Index { get; set; }

    /// <summary>The content (a star icon).</summary>
    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    /// <summary>Content that receives the item's state, e.g. to draw a half star.</summary>
    [Parameter]
    public RenderFragment<BitBhRatingItemContext>? Template { get; set; }

    protected override string DefaultTag => "span";

    private BitBhRatingItemState State => Rating?.StateOf(Index) ?? BitBhRatingItemState.Empty;

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        builder.OpenElement(0, Tag);
        builder.AddMultipleAttributes(10, GetFinalAttributes());
        builder.AddElementReferenceCapture(20, CaptureElement);
        builder.AddContent(30, BitBhContent.Pick(Template, ChildContent, new BitBhRatingItemContext(Index, State, Rating?.IsPreviewing ?? false)));
        builder.CloseElement();
    }

    protected override Dictionary<string, object> BuildComponentAttributes()
    {
        var attrs = base.BuildComponentAttributes();
        attrs["aria-hidden"] = "true";
        attrs["data-bit-bh-rating-item"] = Index.ToString(CultureInfo.InvariantCulture);
        SetDataValue(attrs, "index", Index.ToString(CultureInfo.InvariantCulture));
        SetDataValue(attrs, "state", State switch
        {
            BitBhRatingItemState.Full => "full",
            BitBhRatingItemState.Half => "half",
            _ => "empty",
        });
        SetDataFlag(attrs, "highlighted", Rating?.IsPreviewing == true && State != BitBhRatingItemState.Empty);
        return attrs;
    }
}
