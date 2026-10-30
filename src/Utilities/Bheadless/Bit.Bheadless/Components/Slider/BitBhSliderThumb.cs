using System.Globalization;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.Web;

namespace Bit.Bheadless;

/// <summary>
/// A draggable handle of a <see cref="BitBhSlider"/> (<c>role="slider"</c>) with
/// <c>aria-valuenow</c> / <c>aria-valuemin</c> / <c>aria-valuemax</c> /
/// <c>aria-valuetext</c> and keyboard support. Use one thumb per value. Give each a
/// name with <c>aria-label</c> (e.g. "Minimum price"); inside a <see cref="BitBhField"/> a
/// single thumb is labelled by the field's label.
/// </summary>
public class BitBhSliderThumb : BitBhComponentBase, IDisposable
{
    [CascadingParameter]
    private BitBhSlider? Slider { get; set; }

    /// <summary>The content.</summary>
    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    /// <summary>
    /// Content that receives the render state (<c>double</c>). Use it instead of
    /// <see cref="ChildContent"/> when the markup depends on that state:
    /// <c>&lt;Template Context="state"&gt;…&lt;/Template&gt;</c>.
    /// </summary>
    [Parameter]
    public RenderFragment<double>? Template { get; set; }

    protected override string DefaultTag => "span";

    private int Index => Slider?.IndexOf(this) ?? -1;
    private double CurrentValue => Slider?.ValueAt(Index) ?? 0;

    internal string ThumbId => ComponentId;

    protected override void OnInitialized() => Slider?.RegisterThumb(this);

    public void Dispose() => Slider?.UnregisterThumb(this);


    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        builder.OpenElement(0, Tag);
        builder.AddAttribute(10, "id", ComponentId);
        builder.AddMultipleAttributes(20, GetFinalAttributes());
        builder.AddAttribute(30, "onkeydown",
            On<KeyboardEventArgs>("onkeydown", e => Slider?.HandleThumbKeyDownAsync(this, e) ?? Task.CompletedTask));
        builder.AddElementReferenceCapture(40, CaptureElement);
        builder.AddContent(50, BitBhContent.Pick(Template, ChildContent, CurrentValue));
        builder.CloseElement();
    }

    protected override Dictionary<string, object> BuildComponentAttributes()
    {
        var attrs = base.BuildComponentAttributes();
        if (Slider is null) return attrs;

        var value = CurrentValue;
        attrs["role"] = "slider";
        attrs["aria-valuemin"] = Slider.MinValue.ToString(CultureInfo.InvariantCulture);
        attrs["aria-valuemax"] = Slider.MaxValue.ToString(CultureInfo.InvariantCulture);
        attrs["aria-valuenow"] = value.ToString(CultureInfo.InvariantCulture);
        attrs["aria-valuetext"] = Slider.FormatValueText(value);
        attrs["aria-orientation"] = Slider.SliderOrientation.ToAttribute();
        attrs["tabindex"] = Slider.SliderDisabled ? "-1" : "0";
        attrs[BitBhKeyboard.KeysAttribute] = "ArrowLeft ArrowRight ArrowUp ArrowDown PageUp PageDown Home End";
        if (Slider.SliderDisabled) attrs["aria-disabled"] = "true";
        if (Slider.SliderInvalid) attrs["aria-invalid"] = "true";
        if (Slider.Field?.LabelledBy is { } label && GetAdditionalAttribute("aria-label") is null && GetAdditionalAttribute("aria-labelledby") is null)
            attrs["aria-labelledby"] = label;
        if (Slider.Field?.DescribedBy is { } describedBy) attrs["aria-describedby"] = describedBy;

        attrs["style"] = Slider.PositionStyle(Slider.Percent(value));
        attrs["data-bit-bh-interactive"] = "";
        SetDataValue(attrs, "orientation", Slider.SliderOrientation.ToAttribute());
        SetDataFlag(attrs, "disabled", Slider.SliderDisabled);
        SetDataFlag(attrs, "invalid", Slider.SliderInvalid);
        SetDataFlag(attrs, "required", Slider.SliderRequired);
        SetDataFlag(attrs, "dragging", Slider.IsActiveThumb(this));
        return attrs;
    }
}
