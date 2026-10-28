using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;

namespace Bit.BlazorHeadlessUI;

/// <summary>
/// The filled part of a <see cref="BitBhSlider"/>'s track — from the minimum to the
/// value, or between the thumbs of a range. Receives positioning styles.
/// </summary>
public class BitBhSliderRange : BitBhComponentBase
{
    [CascadingParameter]
    private BitBhSlider? Slider { get; set; }

    protected override string DefaultTag => "span";

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        builder.OpenElement(0, Tag);
        builder.AddMultipleAttributes(10, GetFinalAttributes());
        builder.AddElementReferenceCapture(20, CaptureElement);
        builder.CloseElement();
    }

    protected override Dictionary<string, object> BuildComponentAttributes()
    {
        var attrs = base.BuildComponentAttributes();
        if (Slider is not null)
        {
            attrs["style"] = Slider.RangeStyle();
            SetDataValue(attrs, "orientation", Slider.SliderOrientation.ToAttribute());
            SetDataFlag(attrs, "disabled", Slider.SliderDisabled);
        }
        return attrs;
    }
}
