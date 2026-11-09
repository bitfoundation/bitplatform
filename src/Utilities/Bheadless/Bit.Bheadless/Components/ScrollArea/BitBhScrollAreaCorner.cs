using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;

namespace Bit.Bheadless;

/// <summary>
/// The corner where the vertical and the horizontal scrollbar of a <see cref="BitBhScrollArea"/> meet. It has a
/// size - the vertical scrollbar's width by the horizontal one's height - only while both scrollbars are visible
/// (not with <see cref="BitBhScrollAreaType.Scroll"/>, where they come and go), so give it a background to fill
/// the square the scrollbars leave. Hidden from assistive technology.
///
/// <para>
/// <b>Styling hooks:</b> <c>data-state="visible|hidden"</c>; the root's <c>--bit-bh-scroll-area-corner-width</c> /
/// <c>--bit-bh-scroll-area-corner-height</c> variables, which the scrollbars stop short of.
/// </para>
/// </summary>
public class BitBhScrollAreaCorner : BitBhComponentBase
{
    [CascadingParameter]
    private BitBhScrollArea? Area { get; set; }

    /// <summary>Optional content.</summary>
    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    protected override string DefaultTag => "div";

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
        attrs["aria-hidden"] = "true";
        if (Area is null) return attrs;
        attrs["data-bit-bh-scroll-area-corner"] = Area.RootId;
        SetDataState(attrs, false, "visible", "hidden");
        // Functional: the square at the inline end of the bottom edge, sized by the runtime.
        attrs["style"] = "position:absolute;bottom:0;inset-inline-end:0;width:var(--bit-bh-scroll-area-corner-width,0px);height:var(--bit-bh-scroll-area-corner-height,0px)";
        return attrs;
    }
}
