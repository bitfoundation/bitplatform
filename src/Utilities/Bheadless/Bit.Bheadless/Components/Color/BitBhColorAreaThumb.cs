using System.Globalization;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.Web;

namespace Bit.Bheadless;

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
