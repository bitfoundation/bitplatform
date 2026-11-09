using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;

namespace Bit.Bheadless;

/// <summary>
/// A custom scrollbar of a <see cref="BitBhScrollArea"/> - the track a <see cref="BitBhScrollAreaThumb"/> moves in.
/// Use one per axis (<see cref="Orientation"/>). It is positioned over the edge of the area (inline style:
/// <c>position: absolute</c> along the inline end, or the bottom, clear of the <see cref="BitBhScrollAreaCorner"/>);
/// you give it its thickness, padding and colors.
///
/// <para>
/// Dragging the thumb scrolls the content (pointer capture, touch too), a press on the track scrolls a page
/// towards the pointer (repeating while held) and the wheel over the scrollbar scrolls the content. The
/// scrollbar is a pointer convenience: it is hidden from assistive technology (<c>aria-hidden</c>) and not
/// focusable, since the viewport itself stays scrollable by keyboard and assistive technology.
/// </para>
///
/// <para>
/// <b>Styling hooks:</b> <c>data-orientation</c>; <c>data-state="visible|hidden"</c> by the area's <c>Type</c> (a
/// hidden scrollbar takes no pointer input - fade it out with <c>opacity</c>); <c>data-hovering</c>,
/// <c>data-scrolling</c> (this axis is scrolling) and <c>data-dragging</c>; the variable
/// <c>--bit-bh-scroll-area-thumb-height</c> (vertical) or <c>--bit-bh-scroll-area-thumb-width</c> (horizontal).
/// </para>
/// </summary>
public class BitBhScrollAreaScrollbar : BitBhComponentBase
{
    [CascadingParameter]
    private BitBhScrollArea? Area { get; set; }

    /// <summary>The axis it scrolls. Default: <see cref="BitBhOrientation.Vertical"/>.</summary>
    [Parameter]
    public BitBhOrientation Orientation { get; set; } = BitBhOrientation.Vertical;

    /// <summary>Usually a <see cref="BitBhScrollAreaThumb"/>.</summary>
    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    protected override string DefaultTag => "div";

    internal BitBhOrientation ScrollbarOrientation => Orientation;

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        builder.OpenElement(0, Tag);
        builder.AddAttribute(10, "id", ComponentId);
        builder.AddMultipleAttributes(20, GetFinalAttributes());
        builder.AddElementReferenceCapture(30, CaptureElement);
        builder.OpenComponent<CascadingValue<BitBhScrollAreaScrollbar>>(40);
        builder.AddComponentParameter(41, "Value", this);
        builder.AddComponentParameter(42, "IsFixed", false);
        builder.AddComponentParameter(43, "ChildContent", ChildContent);
        builder.CloseComponent();
        builder.CloseElement();
    }

    protected override Dictionary<string, object> BuildComponentAttributes()
    {
        var attrs = base.BuildComponentAttributes();
        attrs["aria-hidden"] = "true";
        SetDataValue(attrs, "orientation", Orientation.ToAttribute());
        if (Area is null) return attrs;

        attrs["data-bit-bh-scroll-area-scrollbar"] = Area.RootId;
        // The initial state, before the runtime measures the content: only an Always scrollbar is known to show.
        SetDataState(attrs, Area.AreaType == BitBhScrollAreaType.Always, "visible", "hidden");
        // Functional: laid over the area's edge, short of the corner the other scrollbar leaves; a touch drag moves
        // the thumb instead of panning the page, and dragging never selects text.
        attrs["style"] = (Orientation == BitBhOrientation.Vertical
                ? "position:absolute;top:0;bottom:var(--bit-bh-scroll-area-corner-height,0px);inset-inline-end:0;"
                : "position:absolute;bottom:0;inset-inline-start:0;inset-inline-end:var(--bit-bh-scroll-area-corner-width,0px);")
            + "touch-action:none;user-select:none;-webkit-user-select:none";
        return attrs;
    }
}
