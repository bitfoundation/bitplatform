using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;

namespace Bit.BlazorHeadlessUI;

/// <summary>
/// The panel of a <see cref="BitBhCollapsible"/>. Hidden while closed (via the
/// <c>hidden</c> attribute), visible while an exit animation runs, and exposes
/// <c>--bit-bh-content-height</c> / <c>--bit-bh-content-width</c> for size animations:
/// <code>
/// .content[data-state=open]   { animation: slide-down 200ms ease-out; }
/// .content[data-state=closed] { animation: slide-up 200ms ease-in; }
/// @keyframes slide-down { from { height: 0 } to { height: var(--bit-bh-content-height) } }
/// @keyframes slide-up   { from { height: var(--bit-bh-content-height) } to { height: 0 } }
/// </code>
/// </summary>
public class BitBhCollapsibleContent : BitBhComponentBase
{
    [CascadingParameter]
    private BitBhCollapsibleContext? CollapsibleContext { get; set; }

    /// <summary>
    /// Use <c>hidden="until-found"</c> while closed so the browser's find-in-page can
    /// search the content and open the collapsible when it finds a match.
    /// </summary>
    [Parameter]
    public bool HiddenUntilFound { get; set; }

    /// <summary>The content.</summary>
    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    /// <summary>
    /// Content that receives the render state (<c>BitBhCollapsibleRenderContext</c>). Use it instead of
    /// <see cref="ChildContent"/> when the markup depends on that state:
    /// <c>&lt;Template Context="state"&gt;…&lt;/Template&gt;</c>.
    /// </summary>
    [Parameter]
    public RenderFragment<BitBhCollapsibleRenderContext>? Template { get; set; }

    protected override string DefaultTag => "div";

    private bool IsOpen => CollapsibleContext?.IsOpen ?? false;

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        builder.OpenElement(0, Tag);
        builder.AddAttribute(10, "id", CollapsibleContext?.ContentId ?? ComponentId);
        builder.AddMultipleAttributes(20, GetFinalAttributes());
        builder.AddElementReferenceCapture(40, CaptureElement);
        if (Template is not null || ChildContent is not null)
            builder.AddContent(50, BitBhContent.Pick(Template, ChildContent, new BitBhCollapsibleRenderContext
            {
                IsOpen = IsOpen,
                Close = CollapsibleContext?.Close ?? (() => { }),
            }));
        builder.CloseElement();
    }

    protected override Dictionary<string, object> BuildComponentAttributes()
    {
        var attrs = base.BuildComponentAttributes();
        SetDataState(attrs, IsOpen);
        SetDataFlag(attrs, "disabled", CollapsibleContext?.Disabled ?? false);
        BitBhToggleAttributes.ApplyPanel(attrs, IsOpen, HiddenUntilFound, CollapsibleContext?.TriggerId);
        return attrs;
    }
}
