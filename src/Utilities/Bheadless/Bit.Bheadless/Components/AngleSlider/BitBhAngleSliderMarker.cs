using System.Globalization;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;

namespace Bit.Bheadless;

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
