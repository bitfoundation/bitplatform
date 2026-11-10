using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;

namespace Bit.Bheadless;

/// <summary>
/// The scrolling element of a <see cref="BitBhScrollArea"/>: a native scroll container (<c>overflow: scroll</c>)
/// whose own scrollbars are hidden, so the content scrolls with the keyboard, the wheel, touch and assistive
/// technology as in any scrolling element. Size it to its area (<c>width: 100%; height: 100%</c>, or a
/// <c>max-height</c>).
///
/// <para>
/// <b>Keyboard access.</b> A keyboard user scrolls content by focusing something inside it - or, when the
/// content has nothing focusable (text, an image), by focusing the viewport itself. So while
/// <see cref="Focusable"/> is left automatic, the viewport is a tab stop (<c>tabindex="0"</c>) exactly while its
/// content overflows and contains nothing focusable. A focusable viewport should say what it is: give it
/// <c>role="region"</c> and a name (<c>aria-label</c> / <c>aria-labelledby</c>); both pass through.
/// </para>
///
/// <para>
/// <b>Styling hooks:</b> <c>data-overflow-x</c> / <c>data-overflow-y</c>, <c>data-overflow-{x|y}-{start|end}</c>, and,
/// inherited from the root, the <c>--bit-bh-scroll-area-overflow-{x|y}-{start|end}</c> variables (pixels of
/// content hidden past each edge; start and end follow the reading direction). For a fade at the edges:
/// <c>mask-image: linear-gradient(to bottom, transparent, #000 min(2rem, var(--bit-bh-scroll-area-overflow-y-start)), #000 calc(100% - min(2rem, var(--bit-bh-scroll-area-overflow-y-end))), transparent)</c>.
/// </para>
/// </summary>
public class BitBhScrollAreaViewport : BitBhComponentBase
{
    [CascadingParameter]
    private BitBhScrollArea? Area { get; set; }

    /// <summary>
    /// Whether the viewport is a tab stop. Null (default): while its content overflows and contains nothing
    /// focusable, which is decided in the browser as the content changes. True: always (<c>tabindex="0"</c>).
    /// False: never - the content has focusable items of its own. A <c>tabindex</c> attribute you set wins.
    /// </summary>
    [Parameter]
    public bool? Focusable { get; set; }

    /// <summary>The scrolling content.</summary>
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
        if (Area is not null) attrs["data-bit-bh-scroll-area-viewport"] = Area.RootId;
        // Functional: the element scrolls natively on both axes, without the browser's scrollbars (the
        // ::-webkit-scrollbar rule for older WebKit / Blink is added by the runtime).
        attrs["style"] = "overflow:scroll;scrollbar-width:none;-ms-overflow-style:none";
        if (GetAdditionalAttribute("tabindex") is null)
        {
            if (Focusable is true) attrs["tabindex"] = "0";
            // Left to the runtime, which knows the overflow and the content's focusable elements.
            else if (Focusable is null) attrs["data-bit-bh-focusable"] = "auto";
        }
        return attrs;
    }
}
