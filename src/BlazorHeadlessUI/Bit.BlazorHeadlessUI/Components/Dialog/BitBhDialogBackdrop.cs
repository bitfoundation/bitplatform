using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.Web;

namespace Bit.BlazorHeadlessUI;

/// <summary>
/// The optional dim overlay rendered behind the dialog panel. Clicking the
/// backdrop dismisses the dialog.
///
/// <para>
/// Set <see cref="DismissOnClick"/> to <c>false</c> to disable dismiss-on-click
/// (useful for confirmation flows where the user must explicitly choose).
/// </para>
/// </summary>
public class BitBhDialogBackdrop : BitBhComponentBase
{
    [CascadingParameter]
    private BitBhDialogContext? DialogContext { get; set; }

    /// <summary>Whether clicking the backdrop should close the dialog. Defaults to <c>true</c>.</summary>
    [Parameter]
    public bool DismissOnClick { get; set; } = true;

    /// <summary>Optional content to render inside the backdrop (rare; typically empty).</summary>
    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    protected override string DefaultTag => "div";

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        builder.OpenElement(0, Tag);
        builder.AddAttribute(10, "id", ComponentId);
        builder.AddMultipleAttributes(20, GetFinalAttributes());

        if (DismissOnClick)
        {
            builder.AddAttribute(30, "onclick",
                On<MouseEventArgs>("onclick", HandleClick));
        }

        builder.AddElementReferenceCapture(40, CaptureElement);
        builder.AddContent(50, ChildContent);
        builder.CloseElement();
    }

    protected override Dictionary<string, object> BuildComponentAttributes()
    {
        var attrs = base.BuildComponentAttributes();
        SetDataState(attrs, DialogContext?.IsOpen ?? false);

        // Default styles position the backdrop behind the panel inside the
        // dialog's positioning container. Consumers can override via class=/style=.
        if (GetAdditionalAttribute("style") is null && GetAdditionalAttribute("class") is null)
            attrs["style"] = "position:fixed;inset:0;z-index:0;";

        // Decorative: hidden from assistive tech.
        attrs["aria-hidden"] = "true";

        // The runtime's outside-press dismissal leaves the backdrop alone: its own click handler is
        // the one dismissal a press on it makes (both would ask twice per press), and a backdrop that
        // does not dismiss on click must not count as "outside" either.
        attrs["data-bit-bh-dismiss-ignore"] = "";

        return attrs;
    }

    private Task HandleClick(MouseEventArgs _)
        => DialogContext?.DismissAsync(BitBhDismissReason.PointerDownOutside) ?? Task.CompletedTask;
}
