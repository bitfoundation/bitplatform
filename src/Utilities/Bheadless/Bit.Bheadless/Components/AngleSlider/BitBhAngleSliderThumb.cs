using System.Globalization;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.Web;

namespace Bit.Bheadless;

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
