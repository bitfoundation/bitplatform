using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.Web;

namespace Bit.Bheadless;

/// <summary>
/// The popup of a <see cref="BitBhTooltip"/> (<c>role="tooltip"</c>). Hidden while closed.
/// Set <see cref="BitBhFloatingContentBase.Anchor"/> (e.g. <c>"top"</c>) for automatic
/// positioning with flip / shift, and add a <see cref="BitBhTooltipArrow"/> for an arrow.
/// Escape dismisses it from anywhere (WCAG 1.4.13).
/// </summary>
public class BitBhTooltipContent : BitBhFloatingContentBase
{
    [CascadingParameter]
    private BitBhTooltipContext? TooltipContext { get; set; }

    /// <summary>The content.</summary>
    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    /// <summary>
    /// Content that receives the render state (<c>BitBhTooltipRenderContext</c>). Use it instead of
    /// <see cref="ChildContent"/> when the markup depends on that state:
    /// <c>&lt;Template Context="state"&gt;…&lt;/Template&gt;</c>.
    /// </summary>
    [Parameter]
    public RenderFragment<BitBhTooltipRenderContext>? Template { get; set; }

    protected override string DefaultTag => "div";

    protected override bool IsOpen => TooltipContext?.IsOpen ?? false;

    private string ContentId => TooltipContext?.ContentId ?? ComponentId;

    protected override void OnInitialized() => TooltipContext?.RegisterContent(ShouldRenderContent);

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        // The trigger is described by the content only while the content element exists: an
        // unmounted (Unmount) closed tooltip would leave aria-describedby pointing at nothing.
        TooltipContext?.RegisterContent(ShouldRenderContent);
        await base.OnAfterRenderAsync(firstRender);
    }

    public override ValueTask DisposeAsync()
    {
        TooltipContext?.RegisterContent(false);
        return base.DisposeAsync();
    }

    protected override BitBhFloatingRequest CreateFloatingRequest()
    {
        var layer = new BitBhLayerOptions { Hover = true, PointerDownOutside = false, ParentHandle = ParentLayerHandle };
        layer.BranchIds.Add(TooltipContext?.TriggerId);
        return new BitBhFloatingRequest
        {
            ContentId = ContentId,
            ReferenceId = TooltipContext?.TriggerId,
            Anchor = EffectiveAnchor,
            ArrowId = ArrowId,
            Portal = EffectivePortal,
            PortalTarget = PortalTarget,
            Layer = layer,
        };
    }

    protected override Task OnDismissAsync(BitBhDismissReason reason)
        => TooltipContext?.SetOpenAsync(false) ?? Task.CompletedTask;

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
        builder.AddElementReferenceCapture(40, CaptureElement);
        if (Template is not null || ChildContent is not null)
            BitBhFloatingScope.Render(builder, 50, Scope, BitBhContent.Pick(Template, ChildContent, new BitBhTooltipRenderContext
            {
                IsOpen = IsOpen,
                DelayedOpen = TooltipContext?.DelayedOpen ?? false,
            }));
        builder.CloseElement();
    }

    protected override Dictionary<string, object> BuildComponentAttributes()
    {
        var attrs = base.BuildComponentAttributes();
        attrs["role"] = "tooltip";
        attrs["data-state"] = TooltipContext?.DataState ?? "closed";
        return attrs;
    }

    private Task HandlePointerEnter(PointerEventArgs args)
    {
        // Hoverable content: entering it cancels the pending close.
        if (TooltipContext is null || TooltipContext.DisableHoverableContent) return Task.CompletedTask;
        return TooltipContext.ScheduleOpenAsync();
    }

    private Task HandlePointerLeave(PointerEventArgs args)
        => TooltipContext?.ScheduleCloseAsync() ?? Task.CompletedTask;
}
