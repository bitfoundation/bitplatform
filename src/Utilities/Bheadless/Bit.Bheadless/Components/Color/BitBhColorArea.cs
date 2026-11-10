using System.Globalization;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;

namespace Bit.Bheadless;

/// <summary>
/// A two-dimensional color picker (React Aria / Ark ColorArea): one channel along each axis - saturation and brightness by
/// default, the hue of the color held - picked by dragging anywhere in the area or with the arrow keys on its
/// <see cref="BitBhColorAreaThumb"/>.
///
/// <para><b>Styling hooks:</b> the root gets <c>--bit-bh-color-area-hue</c> (the pure hue, for the usual
/// <c>linear-gradient(to top, #000, transparent), linear-gradient(to right, #fff, var(--bit-bh-color-area-hue))</c> background),
/// <c>--bit-bh-color</c> (the color) and <c>data-dragging</c>; the thumb is positioned with <c>left</c> / <c>top</c> in percent.</para>
///
/// <code>
/// &lt;BitBhColorArea @bind-Value="color" class="color-area"&gt;
///     &lt;BitBhColorAreaThumb class="color-area-thumb" aria-label="Color" /&gt;
/// &lt;/BitBhColorArea&gt;
/// </code>
/// </summary>
public class BitBhColorArea : BitBhComponentBase, IAsyncDisposable
{
    private BitBhColor _value = BitBhColor.FromRgb(255, 0, 0);
    private DotNetObjectReference<Callbacks>? _ref;
    private int _handle;
    private bool _attaching;
    private bool _disposed;
    private bool _dragging;

    [CascadingParameter]
    private BitBhFieldsetContext? FieldsetContext { get; set; }

    /// <summary>The color. Supplying it makes the area controlled (use <c>@bind-Value</c>).</summary>
    [Parameter]
    public BitBhColor Value { get; set; }

    /// <summary>Two-way binding callback for <see cref="Value"/>.</summary>
    [Parameter]
    public EventCallback<BitBhColor> ValueChanged { get; set; }

    /// <summary>Initial color when uncontrolled. Default: red.</summary>
    [Parameter]
    public BitBhColor? DefaultValue { get; set; }

    /// <summary>Fires when the user finishes a change (pointer up, key press).</summary>
    [Parameter]
    public EventCallback<BitBhColor> OnValueCommit { get; set; }

    /// <summary>The channel along the horizontal axis. Default: <see cref="BitBhColorChannel.Saturation"/>.</summary>
    [Parameter]
    public BitBhColorChannel XChannel { get; set; } = BitBhColorChannel.Saturation;

    /// <summary>The channel along the vertical axis (bottom to top). Default: <see cref="BitBhColorChannel.Brightness"/>.</summary>
    [Parameter]
    public BitBhColorChannel YChannel { get; set; } = BitBhColorChannel.Brightness;

    /// <summary>Disables the area.</summary>
    [Parameter]
    public bool Disabled { get; set; }

    /// <summary>Form field name: a hidden input carries the color as hex.</summary>
    [Parameter]
    public string? Name { get; set; }

    /// <summary>The thumb (and anything drawn over the area).</summary>
    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    protected override string DefaultTag => "div";

    /// <summary>The current color.</summary>
    public BitBhColor CurrentValue => IsProvided(nameof(Value)) ? Value : _value;

    internal bool IsDisabled => Disabled || (FieldsetContext?.Disabled ?? false);
    internal bool IsDragging => _dragging;

    protected override void OnInitialized()
    {
        if (DefaultValue is { } initial) _value = initial;
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
        builder.OpenComponent<CascadingValue<BitBhColorArea>>(40);
        builder.AddComponentParameter(41, "Value", this);
        builder.AddComponentParameter(42, "IsFixed", false);
        builder.AddComponentParameter(43, "ChildContent", ChildContent);
        builder.CloseComponent();
        builder.CloseElement();

        if (!string.IsNullOrEmpty(Name) && !IsDisabled)
        {
            builder.OpenElement(60, "input");
            builder.AddAttribute(61, "type", "hidden");
            builder.AddAttribute(62, "name", Name);
            builder.AddAttribute(63, "value", CurrentValue.ToHex());
            builder.CloseElement();
        }
    }

    protected override Dictionary<string, object> BuildComponentAttributes()
    {
        var attrs = base.BuildComponentAttributes();
        var color = CurrentValue;
        var hue = new BitBhColor(color.Hue, 1, 1).ToHex();
        // Functional: the area is dragged, so a touch drag must not scroll the page.
        attrs["style"] = $"position:relative;touch-action:none;user-select:none;--bit-bh-color-area-hue:{hue};--bit-bh-color:{color.ToHex()}";
        if (IsDisabled) attrs["data-disabled"] = "";
        SetDataFlag(attrs, "dragging", _dragging);
        return attrs;
    }

    /// <summary>The fraction (0–1) of the area a channel value sits at.</summary>
    internal static double Fraction(BitBhColor color, BitBhColorChannel channel)
    {
        var (min, max, _) = BitBhColor.RangeOf(channel);
        return Math.Clamp((color.Get(channel) - min) / (max - min), 0, 1);
    }

    internal async Task SetAsync(BitBhColor color, bool commit)
    {
        if (IsDisabled) return;
        if (color != CurrentValue)
        {
            if (!IsProvided(nameof(Value))) _value = color;
            await ValueChanged.InvokeAsync(color);
            StateHasChanged();
        }
        if (commit) await OnValueCommit.InvokeAsync(color);
    }

    internal Task SetFractionsAsync(double x, double y, bool commit)
    {
        var (xMin, xMax, _) = BitBhColor.RangeOf(XChannel);
        var (yMin, yMax, _) = BitBhColor.RangeOf(YChannel);
        var color = CurrentValue
            .With(XChannel, Math.Round(xMin + Math.Clamp(x, 0, 1) * (xMax - xMin)))
            .With(YChannel, Math.Round(yMin + Math.Clamp(y, 0, 1) * (yMax - yMin)));
        return SetAsync(color, commit);
    }

    internal async Task HandleKeyDownAsync(KeyboardEventArgs e)
    {
        if (IsDisabled || e.CtrlKey || e.MetaKey || e.AltKey) return;
        var color = CurrentValue;
        var x = color.Get(XChannel);
        var y = color.Get(YChannel);
        var (xMin, xMax, xStep) = BitBhColor.RangeOf(XChannel);
        var (yMin, yMax, yStep) = BitBhColor.RangeOf(YChannel);
        var big = e.ShiftKey ? 10 : 1;
        (double X, double Y)? next = e.Key switch
        {
            "ArrowRight" => (x + xStep * big, y),
            "ArrowLeft" => (x - xStep * big, y),
            "ArrowUp" => (x, y + yStep * big),
            "ArrowDown" => (x, y - yStep * big),
            "PageUp" => (x, y + yStep * 10),
            "PageDown" => (x, y - yStep * 10),
            "Home" => (xMin, y),
            "End" => (xMax, y),
            _ => null,
        };
        if (next is not { } n) return;
        var updated = color.With(XChannel, Math.Clamp(Math.Round(n.X), xMin, xMax)).With(YChannel, Math.Clamp(Math.Round(n.Y), yMin, yMax));
        await SetAsync(updated, commit: true);
    }

    private sealed class Callbacks(BitBhColorArea owner)
    {
        /// <summary>The runtime calls it by this name: "start", "move" or "end", with the pointer's fractions (y from the bottom).</summary>
        [JSInvokable]
        public Task OnAreaPointer(string phase, double x, double y) => owner.RunFromRuntimeAsync(async () =>
        {
            if (owner._disposed) return;
            // Cleared even when the area was disabled mid-drag, so data-dragging does not stick.
            owner._dragging = phase != "end" && !owner.IsDisabled;
            if (owner.IsDisabled)
            {
                owner.StateHasChanged();
                return;
            }
            await owner.SetFractionsAsync(x, y, commit: phase == "end");
            owner.StateHasChanged();
        });
    }

    public async ValueTask DisposeAsync()
    {
        _disposed = true;
        if (!_attaching && _handle > 0) await Interop.AreaDetachAsync(_handle);
        _ref?.Dispose();
        GC.SuppressFinalize(this);
    }
}

/// <summary>
/// The thumb of a <see cref="BitBhColorArea"/>: focusable, <c>role="slider"</c> with <c>aria-roledescription="2D slider"</c>
/// and both channels in <c>aria-valuetext</c> ("Saturation 40%, Brightness 80%"). ← / → change the horizontal channel,
/// ↑ / ↓ the vertical one, Shift by ten steps. Name it with <c>aria-label</c>. Positioned with <c>left</c> / <c>top</c> in
/// percent and given <c>--bit-bh-color</c>.
/// </summary>
public class BitBhColorAreaThumb : BitBhComponentBase
{
    [CascadingParameter]
    private BitBhColorArea? Area { get; set; }

    /// <summary>Optional content.</summary>
    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    protected override string DefaultTag => "div";

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        builder.OpenElement(0, Tag);
        builder.AddAttribute(10, "id", ComponentId);
        builder.AddMultipleAttributes(20, GetFinalAttributes());
        if (Area is { } area) builder.AddAttribute(30, "onkeydown", On<KeyboardEventArgs>("onkeydown", area.HandleKeyDownAsync));
        builder.AddElementReferenceCapture(40, CaptureElement);
        builder.AddContent(50, ChildContent);
        builder.CloseElement();
    }

    protected override Dictionary<string, object> BuildComponentAttributes()
    {
        var attrs = base.BuildComponentAttributes();
        if (Area is not { } area) return attrs;
        var color = area.CurrentValue;
        var x = BitBhColorArea.Fraction(color, area.XChannel);
        var y = BitBhColorArea.Fraction(color, area.YChannel);
        string P(double f) => (f * 100).ToString("0.###", CultureInfo.InvariantCulture);
        string Channel(BitBhColorChannel c) => FormatChannel(c, color.Get(c));

        attrs["role"] = "slider";
        attrs["aria-roledescription"] = "2D slider";
        var (min, max, _) = BitBhColor.RangeOf(area.XChannel);
        attrs["aria-valuemin"] = min.ToString(CultureInfo.InvariantCulture);
        attrs["aria-valuemax"] = max.ToString(CultureInfo.InvariantCulture);
        attrs["aria-valuenow"] = Math.Round(color.Get(area.XChannel)).ToString(CultureInfo.InvariantCulture);
        attrs["aria-valuetext"] = $"{area.XChannel} {Channel(area.XChannel)}, {area.YChannel} {Channel(area.YChannel)}";
        attrs["tabindex"] = area.IsDisabled ? "-1" : "0";
        if (area.IsDisabled) attrs["aria-disabled"] = "true";
        attrs[BitBhKeyboard.KeysAttribute] = "ArrowLeft ArrowRight ArrowUp ArrowDown PageUp PageDown Home End";
        attrs["style"] = $"position:absolute;left:{P(x)}%;top:{P(1 - y)}%;transform:translate(-50%,-50%);--bit-bh-color:{color.ToHex()}";
        attrs["data-bit-bh-interactive"] = "";
        SetDataFlag(attrs, "dragging", area.IsDragging);
        SetDataFlag(attrs, "disabled", area.IsDisabled);
        return attrs;
    }

    internal static string FormatChannel(BitBhColorChannel channel, double value) => channel switch
    {
        BitBhColorChannel.Hue => string.Create(CultureInfo.CurrentCulture, $"{Math.Round(value)}°"),
        BitBhColorChannel.Red or BitBhColorChannel.Green or BitBhColorChannel.Blue => Math.Round(value).ToString(CultureInfo.CurrentCulture),
        _ => string.Create(CultureInfo.CurrentCulture, $"{Math.Round(value)}%"),
    };
}
