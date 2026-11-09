using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.Web;

namespace Bit.Bheadless;

/// <summary>
/// The optional dim layer behind a <see cref="BitBhDrawerPanel"/>. Pressing it dismisses the drawer (unless
/// <see cref="DismissOnClick"/> or the drawer's <c>DismissOnOutsideClick</c> is off). It is decorative: hidden from
/// assistive tech.
///
/// <para><b>Styling hooks:</b> <c>data-state="open|closed"</c> (kept <c>closed</c> during exit animations),
/// <c>data-side</c>, and while the panel is swiped <c>data-swiping</c> and <c>--bit-bh-drawer-swipe-progress</c> (0-1) -
/// fade it with the swipe: <c>opacity: calc(1 - var(--bit-bh-drawer-swipe-progress, 0))</c>.</para>
/// </summary>
public class BitBhDrawerBackdrop : BitBhComponentBase, IDisposable
{
    [CascadingParameter]
    private BitBhDrawerContext? DrawerContext { get; set; }

    /// <summary>Whether pressing the backdrop closes the drawer. Default: true.</summary>
    [Parameter]
    public bool DismissOnClick { get; set; } = true;

    /// <summary>Optional content (rare; usually empty).</summary>
    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    protected override string DefaultTag => "div";

    protected override void OnInitialized() => DrawerContext?.Register(BitBhDrawerPart.Backdrop, true);

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        if (DrawerContext is { IsPresent: false }) return;

        builder.OpenElement(0, Tag);
        builder.AddAttribute(10, "id", DrawerContext?.BackdropId ?? ComponentId);
        builder.AddMultipleAttributes(20, GetFinalAttributes());
        if (DrawerContext?.IsHidden == true)
            builder.AddAttribute(25, "hidden", true);
        if (DismissOnClick)
            builder.AddAttribute(30, "onclick", On<MouseEventArgs>("onclick", HandleClick));
        builder.AddElementReferenceCapture(40, CaptureElement);
        builder.AddContent(50, ChildContent);
        builder.CloseElement();
    }

    protected override Dictionary<string, object> BuildComponentAttributes()
    {
        var attrs = base.BuildComponentAttributes();
        SetDataState(attrs, DrawerContext?.IsOpen ?? false);
        if (DrawerContext is not null) SetDataValue(attrs, "side", DrawerContext.Side);
        attrs["data-bit-bh-presence"] = "";
        // Decorative: hidden from assistive tech.
        attrs["aria-hidden"] = "true";
        // Its own click handler is the one dismissal a press on it makes; the runtime's outside-press dismissal leaves it alone.
        attrs["data-bit-bh-dismiss-ignore"] = "";
        if (GetAdditionalAttribute("style") is null && GetAdditionalAttribute("class") is null)
            attrs["style"] = "position:fixed;inset:0;z-index:50;";
        BitBhDrawer.AddNestingAttributes(attrs, DrawerContext);
        return attrs;
    }

    private Task HandleClick(MouseEventArgs _)
        => DrawerContext?.DismissAsync(BitBhDismissReason.PointerDownOutside) ?? Task.CompletedTask;

    public void Dispose() => DrawerContext?.Register(BitBhDrawerPart.Backdrop, false);
}
