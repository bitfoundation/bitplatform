using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;

namespace Bit.Bheadless;

/// <summary>
/// A swatch of a color (React Aria ColorSwatch): <c>role="img"</c> named by <see cref="Label"/> (default: the hex code), painted
/// with the color (<c>background-color</c> and <c>--bit-bh-color</c>).
/// </summary>
public class BitBhColorSwatch : BitBhComponentBase
{
    /// <summary>The color.</summary>
    [Parameter, EditorRequired]
    public BitBhColor Color { get; set; }

    /// <summary>The swatch's accessible name ("Ocean blue"). Default: the hex code.</summary>
    [Parameter]
    public string? Label { get; set; }

    /// <summary>Optional content (a checkered background for transparent colors …).</summary>
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
        attrs["role"] = "img";
        attrs["aria-label"] = Label ?? Color.ToHex();
        // The swatch is the color: painting it is its job, not a style choice.
        attrs["style"] = $"background-color:{Color.ToRgbString()};--bit-bh-color:{Color.ToHex()}";
        attrs["data-color"] = Color.ToHex();
        return attrs;
    }
}
