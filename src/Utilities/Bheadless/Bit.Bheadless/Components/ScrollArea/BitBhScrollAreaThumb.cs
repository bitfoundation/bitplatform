using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;

namespace Bit.Bheadless;

/// <summary>
/// The draggable thumb of a <see cref="BitBhScrollAreaScrollbar"/>. Its length is the visible fraction of the
/// content (at least 18px), set inline from <c>--bit-bh-scroll-area-thumb-height</c> (vertical) or
/// <c>--bit-bh-scroll-area-thumb-width</c> (horizontal); its position along the track is a <c>transform</c> the
/// runtime writes as the content scrolls. You give it its thickness (usually <c>flex: 1</c> or 100% of the
/// scrollbar), shape and colors - and leave <c>transform</c> to the component.
///
/// <para><b>Styling hooks:</b> <c>data-orientation</c>, <c>data-dragging</c> while dragged.</para>
/// </summary>
public class BitBhScrollAreaThumb : BitBhComponentBase
{
    [CascadingParameter]
    private BitBhScrollArea? Area { get; set; }

    [CascadingParameter]
    private BitBhScrollAreaScrollbar? Scrollbar { get; set; }

    /// <summary>Optional content (a grip).</summary>
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
        var orientation = Scrollbar?.ScrollbarOrientation ?? BitBhOrientation.Vertical;
        SetDataValue(attrs, "orientation", orientation.ToAttribute());
        if (Area is not null) attrs["data-bit-bh-scroll-area-thumb"] = Area.RootId;
        // Functional: the length follows the visible fraction of the content the runtime measures.
        attrs["style"] = orientation == BitBhOrientation.Vertical
            ? "height:var(--bit-bh-scroll-area-thumb-height)"
            : "width:var(--bit-bh-scroll-area-thumb-width)";
        return attrs;
    }
}
