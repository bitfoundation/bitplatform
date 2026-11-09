using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;

namespace Bit.Bheadless;

/// <summary>
/// The grab bar of a <see cref="BitBhDrawerPanel"/> - the visual cue that the drawer can be dragged. A drag that
/// starts on it is always a swipe, also on touch (it carries <c>touch-action: none</c>), whatever the content around
/// it does. It is decorative and hidden from assistive tech: keyboard and screen-reader users close the drawer with
/// Escape or a <see cref="BitBhDrawerClose"/>.
///
/// <para><b>Styling hooks:</b> <c>data-state="open|closed"</c>, <c>data-side</c>.</para>
/// </summary>
public class BitBhDrawerHandle : BitBhComponentBase
{
    [CascadingParameter]
    private BitBhDrawerContext? DrawerContext { get; set; }

    /// <summary>Optional content (most handles are an empty, styled bar).</summary>
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
        attrs["data-bit-bh-drawer-handle"] = "";
        SetDataState(attrs, DrawerContext?.IsOpen ?? false);
        if (DrawerContext is not null) SetDataValue(attrs, "side", DrawerContext.Side);
        // Functional: a touch drag on the handle is the drawer's, never a page pan.
        attrs["style"] = "touch-action:none;";
        return attrs;
    }
}
