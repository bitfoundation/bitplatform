using System.Globalization;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;

namespace Bit.Bheadless;

/// <summary>
/// A tick mark (with an optional label) at a value along a <see cref="BitBhSlider"/>'s track (Ark UI's Slider.Marker).
/// It is positioned like a thumb - absolutely, at <see cref="Value"/>, mirrored for right to left and
/// <see cref="BitBhSlider.Inverted"/> - and hidden from assistive technology, since the thumbs already announce the value.
/// <code>
/// &lt;BitBhSliderTrack class="track"&gt;
///     &lt;BitBhSliderRange class="range" /&gt;
///     @foreach (var mark in new[] { 0, 25, 50, 75, 100 })
///     {
///         &lt;BitBhSliderMarker Value="mark" class="marker"&gt;@mark&lt;/BitBhSliderMarker&gt;
///     }
/// &lt;/BitBhSliderTrack&gt;
/// </code>
/// <para><b>Styling hooks:</b> <c>data-value</c>, <c>data-in-range</c> when inside the filled range,
/// <c>data-state="under-value" | "at-value" | "over-value"</c> (relative to the first thumb of a single-thumb slider,
/// to the range of a multi-thumb one), <c>data-orientation</c>, <c>data-disabled</c>.</para>
/// </summary>
public class BitBhSliderMarker : BitBhComponentBase
{
    [CascadingParameter]
    private BitBhSlider? Slider { get; set; }

    /// <summary>The value the marker stands at.</summary>
    [Parameter]
    public double Value { get; set; }

    /// <summary>Optional label content.</summary>
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
        SetDataValue(attrs, "value", Value.ToString(CultureInfo.InvariantCulture));
        if (Slider is null) return attrs;

        attrs["style"] = Slider.PositionStyle(Slider.Percent(Value));
        var values = Slider.CurrentValues;
        var low = values.Count > 1 ? values.Min() : Slider.MinValue;
        var high = values.Count > 0 ? values.Max() : Slider.MinValue;
        const double epsilon = 1e-9;
        var state = values.Count > 1
            ? Value < low - epsilon ? "under-value" : Value > high + epsilon ? "over-value" : "at-value"
            : Math.Abs(Value - high) <= epsilon ? "at-value" : Value < high ? "under-value" : "over-value";
        SetDataValue(attrs, "state", state);
        SetDataFlag(attrs, "in-range", Value >= low - epsilon && Value <= high + epsilon);
        SetDataValue(attrs, "orientation", Slider.SliderOrientation.ToAttribute());
        SetDataFlag(attrs, "disabled", Slider.SliderDisabled);
        return attrs;
    }
}
