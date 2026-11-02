using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.Web;

namespace Bit.Bheadless;

/// <summary>
/// The card shown by a <see cref="BitBhHoverCard"/>. Hidden while closed. Set
/// <see cref="BitBhFloatingContentBase.Anchor"/> for automatic positioning and add a
/// <see cref="BitBhHoverCardArrow"/> for an arrow. Keeping the pointer over it keeps it open.
/// </summary>
public class BitBhHoverCardContent : BitBhFloatingContentBase
{
    [CascadingParameter]
    private BitBhHoverCardContext? HoverCardContext { get; set; }

    /// <summary>The content.</summary>
    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    /// <summary>
    /// Content that receives the render state (<c>BitBhHoverCardRenderContext</c>). Use it instead of
    /// <see cref="ChildContent"/> when the markup depends on that state:
    /// <c>&lt;Template Context="state"&gt;…&lt;/Template&gt;</c>.
    /// </summary>
    [Parameter]
    public RenderFragment<BitBhHoverCardRenderContext>? Template { get; set; }

    protected override string DefaultTag => "div";

    protected override bool IsOpen => HoverCardContext?.IsOpen ?? false;

    private string ContentId => HoverCardContext?.ContentId ?? ComponentId;

    protected override BitBhFloatingRequest CreateFloatingRequest()
    {
        var layer = new BitBhLayerOptions { Hover = true, ParentHandle = ParentLayerHandle };
        layer.BranchIds.Add(HoverCardContext?.TriggerId);
        return new BitBhFloatingRequest
        {
            ContentId = ContentId,
            ReferenceId = HoverCardContext?.TriggerId,
            Anchor = EffectiveAnchor,
            ArrowId = ArrowId,
            Portal = EffectivePortal,
            PortalTarget = PortalTarget,
            Layer = layer,
        };
    }

    protected override Task OnDismissAsync(BitBhDismissReason reason)
        => HoverCardContext?.SetOpenAsync(false) ?? Task.CompletedTask;

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        if (!ShouldRenderContent) return;

        Scope.Owner = ContentId;

        builder.OpenElement(0, Tag);
        builder.AddAttribute(10, "id", ContentId);
        builder.AddMultipleAttributes(20, GetFinalAttributes());
        if (IsHidden)
            builder.AddAttribute(30, "hidden", true);
        builder.AddAttribute(31, "onpointerenter",
            On<PointerEventArgs>("onpointerenter", HandlePointerEnter));
        builder.AddAttribute(32, "onpointerleave",
            On<PointerEventArgs>("onpointerleave", HandlePointerLeave));
        builder.AddAttribute(33, "onfocusin",
            On<FocusEventArgs>("onfocusin", HandlePointerEnter));
        builder.AddAttribute(34, "onfocusout",
            On<FocusEventArgs>("onfocusout", HandlePointerLeave));
        builder.AddElementReferenceCapture(40, CaptureElement);
        if (Template is not null || ChildContent is not null)
            BitBhFloatingScope.Render(builder, 50, Scope, BitBhContent.Pick(Template, ChildContent, new BitBhHoverCardRenderContext { IsOpen = IsOpen }));
        builder.CloseElement();
    }

    protected override Dictionary<string, object> BuildComponentAttributes()
    {
        var attrs = base.BuildComponentAttributes();
        SetDataState(attrs, IsOpen);
        return attrs;
    }

    private Task HandlePointerEnter() => HoverCardContext?.ScheduleOpenAsync() ?? Task.CompletedTask;

    private Task HandlePointerLeave() => HoverCardContext?.ScheduleCloseAsync() ?? Task.CompletedTask;
}
