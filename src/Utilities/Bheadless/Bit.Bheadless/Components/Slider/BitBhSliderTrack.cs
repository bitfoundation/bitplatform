using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;

namespace Bit.Bheadless;

/// <summary>
/// The rail of a <see cref="BitBhSlider"/>. Pointer positions are measured against it
/// (or the slider root when absent). Positioned <c>relative</c> by default so the
/// <see cref="BitBhSliderRange"/> can sit inside it.
/// </summary>
public class BitBhSliderTrack : BitBhComponentBase
{
    [CascadingParameter]
    private BitBhSlider? Slider { get; set; }

    /// <summary>Usually a <see cref="BitBhSliderRange"/>.</summary>
    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    protected override string DefaultTag => "span";

    protected override void OnInitialized() => Slider?.RegisterTrack(ComponentId);

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        builder.OpenElement(0, Tag);
        builder.AddAttribute(10, "id", ComponentId);
        builder.AddMultipleAttributes(20, GetFinalAttributes());
        builder.AddElementReferenceCapture(30, CaptureElement);
        builder.AddContent(40, ChildContent);
        builder.CloseElement();
    }

    protected override Dictionary<string, object> BuildComponentAttributes()
    {
        var attrs = base.BuildComponentAttributes();
        if (Slider is not null)
        {
            SetDataValue(attrs, "orientation", Slider.SliderOrientation.ToAttribute());
            SetDataFlag(attrs, "disabled", Slider.SliderDisabled);
        }
        attrs["style"] = "position:relative;flex-grow:1;";
        return attrs;
    }
}
