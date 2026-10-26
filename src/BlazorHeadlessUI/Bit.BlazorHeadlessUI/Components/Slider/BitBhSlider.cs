using System.Globalization;
using System.Linq.Expressions;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;

namespace Bit.BlazorHeadlessUI;

/// <summary>
/// A headless Slider for picking a value — or a range, with several thumbs —
/// within a min / max (WAI-ARIA Slider and Multi-Thumb Slider patterns).
///
/// <para><b>Key features:</b></para>
/// <list type="bullet">
///   <item><b>Single or range</b> — <c>@bind-Value</c>, or <c>@bind-Values</c> with one <see cref="BitBhSliderThumb"/> per value.</item>
///   <item><b>Keyboard</b> — arrows (± <see cref="Step"/>), Shift+arrows / PageUp / PageDown (± <see cref="LargeStep"/>), Home / End; mirrored in RTL.</item>
///   <item><b>Pointer</b> — click or drag anywhere on the track (pointer capture; works on touch).</item>
///   <item><b>Forms</b> — <c>EditForm</c> validation, <see cref="BitBhField"/> labels, hidden inputs (<see cref="Name"/>).</item>
///   <item><b>Styling</b> — thumbs and the range get positioning styles (<c>left</c> / <c>right</c> / <c>bottom</c> / <c>top</c> in %) plus <c>data-orientation</c>, <c>data-disabled</c>, <c>data-dragging</c>.</item>
/// </list>
///
/// <code>
/// &lt;BitBhSlider @bind-Value="volume" Max="100" class="slider"&gt;
///     &lt;BitBhSliderTrack class="track"&gt;&lt;BitBhSliderRange class="range" /&gt;&lt;/BitBhSliderTrack&gt;
///     &lt;BitBhSliderThumb class="thumb" aria-label="Volume" /&gt;
/// &lt;/BitBhSlider&gt;
/// </code>
/// </summary>
public class BitBhSlider : BitBhComponentBase, IAsyncDisposable
{
    private readonly List<BitBhSliderThumb> _thumbs = new();
    private double[] _values = [];
    private int _activeThumb = -1;
    private int _handle;
    private bool _attachTried;
    private string? _sentOptions;
    private bool _syncing;
    private bool _disposed;
    private DotNetObjectReference<PointerCallbacks>? _callbacksRef;
    private BitBhFieldBinding? _binding;
    private string? _trackId;

    [CascadingParameter]
    private BitBhDirection CascadedDir { get; set; }

    [CascadingParameter]
    private BitBhFieldContext? FieldContext { get; set; }

    [CascadingParameter]
    private BitBhFieldsetContext? FieldsetContext { get; set; }

    [CascadingParameter]
    private EditContext? EditContext { get; set; }

    /// <summary>The value (single thumb). Supplying it makes the slider controlled (use <c>@bind-Value</c>).</summary>
    [Parameter]
    public double Value { get; set; }

    /// <summary>Two-way binding callback for <see cref="Value"/>.</summary>
    [Parameter]
    public EventCallback<double> ValueChanged { get; set; }

    /// <summary>Identifies the bound field for <c>EditForm</c> validation.</summary>
    [Parameter]
    public Expression<Func<double>>? ValueExpression { get; set; }

    /// <summary>The values (one per thumb). Supplying it makes the slider controlled (use <c>@bind-Values</c>).</summary>
    [Parameter]
    public IReadOnlyList<double>? Values { get; set; }

    /// <summary>Two-way binding callback for <see cref="Values"/>.</summary>
    [Parameter]
    public EventCallback<IReadOnlyList<double>> ValuesChanged { get; set; }

    /// <summary>Initial value when uncontrolled (single thumb).</summary>
    [Parameter]
    public double? DefaultValue { get; set; }

    /// <summary>Initial values when uncontrolled (range).</summary>
    [Parameter]
    public IReadOnlyList<double>? DefaultValues { get; set; }

    /// <summary>Fires when the user finishes changing the value (pointer up, key press).</summary>
    [Parameter]
    public EventCallback<IReadOnlyList<double>> OnValueCommit { get; set; }

    /// <summary>The minimum value. Default: 0.</summary>
    [Parameter]
    public double Min { get; set; }

    /// <summary>The maximum value. Default: 100.</summary>
    [Parameter]
    public double Max { get; set; } = 100;

    /// <summary>The step. Default: 1.</summary>
    [Parameter]
    public double Step { get; set; } = 1;

    /// <summary>The step for PageUp / PageDown / Shift+arrows. Default: 10 steps.</summary>
    [Parameter]
    public double? LargeStep { get; set; }

    /// <summary>Minimum number of steps between thumbs of a range. Default: 0.</summary>
    [Parameter]
    public int MinStepsBetweenThumbs { get; set; }

    /// <summary>Horizontal (default) or vertical.</summary>
    [Parameter]
    public BitBhOrientation Orientation { get; set; } = BitBhOrientation.Horizontal;

    /// <summary>Reverses the direction (max on the left / bottom).</summary>
    [Parameter]
    public bool Inverted { get; set; }

    /// <summary>Reading direction. Inherits from <see cref="BitBhDirectionProvider"/>.</summary>
    [Parameter]
    public BitBhDirection? Dir { get; set; }

    /// <summary>Disables the slider.</summary>
    [Parameter]
    public bool Disabled { get; set; }

    /// <summary>Marks the slider invalid (in addition to EditContext validation).</summary>
    [Parameter]
    public bool Invalid { get; set; }

    /// <summary>Marks the slider required (<c>data-required</c>; <c>aria-required</c> is not allowed on a slider).</summary>
    [Parameter]
    public bool Required { get; set; }

    /// <summary>Form field name: one hidden input per value.</summary>
    [Parameter]
    public string? Name { get; set; }

    /// <summary>Formats <c>aria-valuetext</c> for a thumb value (e.g. "$40").</summary>
    [Parameter]
    public Func<double, string>? ValueText { get; set; }

    /// <summary>Track, range and thumbs.</summary>
    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    protected override string DefaultTag => "span";

    private bool IsDisabled => Disabled || (FieldContext?.Disabled ?? false) || (FieldsetContext?.Disabled ?? false);
    private bool IsInvalid => Invalid || (FieldContext?.Invalid ?? false) || (_binding?.IsInvalid ?? false);
    private bool IsRequired => Required || (FieldContext?.Required ?? false);
    private BitBhDirection EffectiveDir => Dir ?? CascadedDir;
    private bool IsRange => IsProvided(nameof(Values)) || DefaultValues is not null;
    private double EffectiveLargeStep => LargeStep ?? Math.Max(Step, (Max - Min) / 10);

    /// <summary>The current values (one per thumb).</summary>
    public IReadOnlyList<double> CurrentValues
    {
        get
        {
            if (IsProvided(nameof(Values))) return Values ?? [];
            if (IsProvided(nameof(Value))) return [Value];
            return _values;
        }
    }

    protected override void OnInitialized()
    {
        _values = DefaultValues?.ToArray() ?? [DefaultValue ?? Min];
        FieldContext?.SetControlFocus(FocusAsync);
    }

    /// <summary>Focuses the first thumb.</summary>
    public Task FocusAsync()
        => _thumbs.Count == 0 ? Task.CompletedTask : Interop.FocusAsync(_thumbs[0].ThumbId).AsTask();

    protected override void OnParametersSet()
    {
        if (ValueExpression is not null || _binding is not null)
        {
            _binding ??= new BitBhFieldBinding(() => InvokeAsync(OnValidationStateChanged));
            _binding.Update(EditContext, ValueExpression);
            _binding.ReportTo(FieldContext);
        }
    }

    // Validation also changes without this control changing (a submit, an async validator): the field's
    // error message and data-invalid follow it as well as the thumbs' aria-invalid.
    private void OnValidationStateChanged()
    {
        _binding?.ReportTo(FieldContext);
        StateHasChanged();
    }

    protected override Task OnAfterRenderAsync(bool firstRender) => SyncAsync();

    // Renders can arrive while the attach is in flight. One loop applies them in order, and a slider disposed
    // meanwhile detaches what the in-flight call attached: the runtime would otherwise keep its listener and
    // the element for good.
    private async Task SyncAsync()
    {
        if (_syncing) return;
        _syncing = true;
        try
        {
            while (_disposed is false)
            {
                var options = $"{Orientation}|{Inverted}";
                if (_handle == 0)
                {
                    // One attempt, as before: a 0 handle (no runtime, no element) isn't retried on every render.
                    if (_attachTried) break;
                    _attachTried = true;
                    _callbacksRef ??= DotNetObjectReference.Create(new PointerCallbacks(this));
                    _handle = await Interop.SliderAttachAsync(_callbacksRef, ComponentId, _trackId, RuntimeOptions());
                    _sentOptions = options;
                    continue;
                }
                if (options == _sentOptions) break;
                _sentOptions = options;
                await Interop.SliderUpdateAsync(_handle, RuntimeOptions());
            }
        }
        finally
        {
            _syncing = false;
        }
        if (_disposed) await DetachAsync();
    }

    private Dictionary<string, object?> RuntimeOptions() => new()
    {
        ["orientation"] = Orientation.ToAttribute(),
        ["inverted"] = Inverted,
    };

    private async Task DetachAsync()
    {
        if (_handle <= 0) return;
        var handle = _handle;
        _handle = 0;
        await Interop.SliderDetachAsync(handle);
    }

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        builder.OpenElement(0, Tag);
        builder.AddAttribute(10, "id", ComponentId);
        builder.AddMultipleAttributes(20, GetFinalAttributes());
        builder.AddElementReferenceCapture(30, CaptureElement);
        builder.OpenComponent<CascadingValue<BitBhSlider>>(40);
        builder.AddComponentParameter(41, "Value", this);
        // Not fixed: parameterless parts (<BitBhSliderRange />, <BitBhSliderTrack />) follow the value.
        builder.AddComponentParameter(42, "IsFixed", false);
        builder.AddComponentParameter(43, "ChildContent", ChildContent);
        builder.CloseComponent();
        builder.CloseElement();

        // Like a native disabled control, a disabled slider posts nothing.
        if (!string.IsNullOrEmpty(Name) && !IsDisabled)
        {
            builder.OpenRegion(60);
            foreach (var v in CurrentValues)
            {
                builder.OpenElement(0, "input");
                builder.AddAttribute(1, "type", "hidden");
                builder.AddAttribute(2, "name", IsRange ? $"{Name}[]" : Name);
                builder.AddAttribute(3, "value", v.ToString(CultureInfo.InvariantCulture));
                builder.CloseElement();
            }
            builder.CloseRegion();
        }
    }

    protected override Dictionary<string, object> BuildComponentAttributes()
    {
        var attrs = base.BuildComponentAttributes();
        if (Dir is { } dir) attrs["dir"] = dir.ToAttribute();
        SetDataValue(attrs, "orientation", Orientation.ToAttribute());
        SetDataFlag(attrs, "disabled", IsDisabled);
        SetDataFlag(attrs, "invalid", IsInvalid);
        SetDataFlag(attrs, "required", IsRequired);
        SetDataFlag(attrs, "dragging", _activeThumb >= 0);
        if (GetAdditionalAttribute("style") is null && GetAdditionalAttribute("class") is null)
            attrs["style"] = Orientation == BitBhOrientation.Vertical
                ? "position:relative;display:inline-flex;flex-direction:column;touch-action:none;user-select:none;"
                : "position:relative;display:flex;align-items:center;touch-action:none;user-select:none;";
        return attrs;
    }

    // ── Geometry ─────────────────────────────────────────────────────────────

    internal double Percent(double value) => Max > Min ? Math.Clamp((value - Min) / (Max - Min) * 100, 0, 100) : 0;

    /// <summary>CSS for absolutely positioning something at <paramref name="percent"/> along the slider.</summary>
    internal string PositionStyle(double percent)
    {
        var p = percent.ToString("0.###", CultureInfo.InvariantCulture);
        if (Orientation == BitBhOrientation.Vertical)
            return Inverted ? $"position:absolute;top:{p}%;transform:translateY(-50%)" : $"position:absolute;bottom:{p}%;transform:translateY(50%)";
        var fromRight = (EffectiveDir == BitBhDirection.Rtl) != Inverted;
        return fromRight ? $"position:absolute;right:{p}%;transform:translateX(50%)" : $"position:absolute;left:{p}%;transform:translateX(-50%)";
    }

    internal string RangeStyle()
    {
        var values = CurrentValues;
        var start = values.Count > 1 ? Percent(values.Min()) : 0;
        var end = 100 - Percent(values.Count > 0 ? values.Max() : Min);
        string F(double d) => d.ToString("0.###", CultureInfo.InvariantCulture);
        if (Orientation == BitBhOrientation.Vertical)
            return Inverted ? $"position:absolute;top:{F(start)}%;bottom:{F(end)}%" : $"position:absolute;bottom:{F(start)}%;top:{F(end)}%";
        var fromRight = (EffectiveDir == BitBhDirection.Rtl) != Inverted;
        return fromRight ? $"position:absolute;right:{F(start)}%;left:{F(end)}%" : $"position:absolute;left:{F(start)}%;right:{F(end)}%";
    }

    internal BitBhOrientation SliderOrientation => Orientation;
    internal bool SliderDisabled => IsDisabled;
    internal bool SliderInvalid => IsInvalid;
    internal bool SliderRequired => IsRequired;
    internal BitBhFieldContext? Field => FieldContext;
    internal string FormatValueText(double value) => ValueText?.Invoke(value) ?? value.ToString(CultureInfo.CurrentCulture);
    internal double MinValue => Min;
    internal double MaxValue => Max;

    internal void RegisterTrack(string id) => _trackId = id;

    internal int RegisterThumb(BitBhSliderThumb thumb)
    {
        if (!_thumbs.Contains(thumb)) _thumbs.Add(thumb);
        return _thumbs.IndexOf(thumb);
    }

    internal void UnregisterThumb(BitBhSliderThumb thumb) => _thumbs.Remove(thumb);

    internal int IndexOf(BitBhSliderThumb thumb) => _thumbs.IndexOf(thumb);

    internal double ValueAt(int index) => index >= 0 && index < CurrentValues.Count ? CurrentValues[index] : Min;

    internal bool IsActiveThumb(BitBhSliderThumb thumb) => _activeThumb >= 0 && _activeThumb == _thumbs.IndexOf(thumb);

    // ── Value updates ────────────────────────────────────────────────────────

    private double Snap(double value)
    {
        var snapped = Math.Round((value - Min) / Step) * Step + Min;
        var decimals = Math.Max(0, (int)Math.Ceiling(-Math.Log10(Step)) + 1);
        return Math.Clamp(Math.Round(snapped, Math.Min(decimals, 10)), Min, Max);
    }

    /// <summary>
    /// Moves a thumb. Returns the resulting values - the ones to report: a controlled <see cref="Value"/> /
    /// <see cref="Values"/> only catches up once the parent re-rendered - and whether they changed.
    /// </summary>
    private async Task<(double[] Values, bool Changed)> SetThumbValueAsync(int index, double value)
    {
        var values = CurrentValues.ToArray();
        if (IsDisabled || index < 0 || index >= values.Length) return (values, false);

        var next = Snap(value);
        var gap = MinStepsBetweenThumbs * Step;
        if (index > 0) next = Math.Max(next, values[index - 1] + gap);
        if (index < values.Length - 1) next = Math.Min(next, values[index + 1] - gap);
        next = Math.Clamp(next, Min, Max);

        if (Math.Abs(values[index] - next) <= double.Epsilon) return (values, false);

        values[index] = next;
        if (!IsProvided(nameof(Values)) && !IsProvided(nameof(Value))) _values = values;
        if (IsRange) await ValuesChanged.InvokeAsync(values);
        else await ValueChanged.InvokeAsync(next);
        _binding?.NotifyChanged();
        _binding?.ReportTo(FieldContext);
        StateHasChanged();
        return (values, true);
    }

    internal async Task HandleThumbKeyDownAsync(BitBhSliderThumb thumb, KeyboardEventArgs e)
    {
        var index = _thumbs.IndexOf(thumb);
        if (index < 0 || IsDisabled) return;
        var current = ValueAt(index);
        var step = e.ShiftKey ? EffectiveLargeStep : Step;
        var rtl = EffectiveDir == BitBhDirection.Rtl && Orientation == BitBhOrientation.Horizontal;

        // Up and Right increase whichever way the track is drawn (APG); Inverted only moves the thumb the
        // other way. A right-to-left slider mirrors Left / Right.
        double? target = e.Key switch
        {
            "ArrowRight" => current + (rtl ? -step : step),
            "ArrowUp" => current + step,
            "ArrowLeft" => current - (rtl ? -step : step),
            "ArrowDown" => current - step,
            "PageUp" => current + EffectiveLargeStep,
            "PageDown" => current - EffectiveLargeStep,
            "Home" => Min,
            "End" => Max,
            _ => null,
        };
        if (target is null) return;
        var (values, changed) = await SetThumbValueAsync(index, target.Value);
        // A key at Min / Max changes nothing, and there is nothing to commit.
        if (changed) await OnValueCommit.InvokeAsync(values);
    }

    /// <summary>
    /// The runtime's pointer callback. A separate object, not the slider: a reference to the component
    /// would root it for the runtime and make its JS-invokable method part of its public API.
    /// </summary>
    private sealed class PointerCallbacks(BitBhSlider slider)
    {
        /// <summary>Pointer interaction (fraction 0–1 along the track). The runtime calls it by this name.</summary>
        [JSInvokable]
        public Task OnSliderPointer(string phase, double fraction) => slider.HandlePointerAsync(phase, fraction);
    }

    private async Task HandlePointerAsync(string phase, double fraction)
    {
        if (_disposed || IsDisabled || _thumbs.Count == 0) return;
        var value = Min + fraction * (Max - Min);
        if (phase == "start")
        {
            // Pick the closest thumb (the one that can move in that direction on ties).
            var values = CurrentValues;
            var best = 0;
            for (var i = 1; i < values.Count; i++)
                if (Math.Abs(values[i] - value) < Math.Abs(values[best] - value) ||
                    (Math.Abs(values[i] - value) == Math.Abs(values[best] - value) && value > values[i]))
                    best = i;
            _activeThumb = best;
        }
        if (_activeThumb < 0) return;
        var (next, _) = await SetThumbValueAsync(_activeThumb, value);
        if (phase == "end")
        {
            await OnValueCommit.InvokeAsync(next);
            _activeThumb = -1;
            StateHasChanged();
        }
    }

    public async ValueTask DisposeAsync()
    {
        _disposed = true;
        _binding?.Dispose();
        FieldContext?.SetControlFocus(null);
        // An attach in flight is undone by SyncAsync once it returns.
        if (_syncing is false) await DetachAsync();
        _callbacksRef?.Dispose();
        GC.SuppressFinalize(this);
    }
}
