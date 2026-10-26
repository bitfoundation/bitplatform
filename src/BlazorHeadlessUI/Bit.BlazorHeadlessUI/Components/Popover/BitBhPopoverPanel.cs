using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.Web;

namespace Bit.BlazorHeadlessUI;

/// <summary>
/// The floating content panel for an <see cref="BitBhPopover"/>. Hidden via the HTML
/// <c>hidden</c> attribute when the popover is closed.
///
/// <para><b>Behaviour:</b></para>
/// <list type="bullet">
///   <item>Receives focus on open (an element marked <c>data-bit-bh-autofocus</c>, else the first focusable element, else the panel).</item>
///   <item>Escape, pointer-down outside and focus leaving dismiss the popover.</item>
///   <item>Emits <c>data-state="open|closed"</c> (kept <c>closed</c> during exit animations) plus <c>data-side</c> / <c>data-align</c> when anchored.</item>
/// </list>
///
/// <para>
/// Set <see cref="BitBhFloatingContentBase.Anchor"/> to position the panel relative
/// to the <see cref="BitBhPopoverButton"/>, and <see cref="BitBhFloatingContentBase.Portal"/>
/// to render it at the end of <c>&lt;body&gt;</c>.
/// </para>
/// </summary>
public class BitBhPopoverPanel : BitBhFloatingContentBase
{
    [CascadingParameter]
    private BitBhPopoverContext? PopoverContext { get; set; }

    /// <summary>The content.</summary>
    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    /// <summary>
    /// Content that receives the render state (<c>BitBhPopoverRenderContext</c>). Use it instead of
    /// <see cref="ChildContent"/> when the markup depends on that state:
    /// <c>&lt;Template Context="state"&gt;…&lt;/Template&gt;</c>.
    /// </summary>
    [Parameter]
    public RenderFragment<BitBhPopoverRenderContext>? Template { get; set; }

    /// <summary>Move focus into the panel when it opens. Default: true.</summary>
    [Parameter]
    public bool AutoFocus { get; set; } = true;

    protected override string DefaultTag => "div";

    protected override bool IsOpen => PopoverContext?.IsOpen ?? false;

    private string PanelId => PopoverContext?.PanelId ?? ComponentId;

    private BitBhPopoverRenderContext RenderContext => new()
    {
        IsOpen = IsOpen,
        Close = () => _ = PopoverContext?.CloseAsync(),
    };

    protected override BitBhFloatingRequest CreateFloatingRequest()
    {
        var modal = PopoverContext?.Modal ?? false;
        var layer = new BitBhLayerOptions
        {
            FocusOutside = !modal,
            Modal = modal,
            ParentHandle = ParentLayerHandle,
        };
        layer.BranchIds.Add(PopoverContext?.ButtonId);

        return new BitBhFloatingRequest
        {
            ContentId = PanelId,
            ReferenceId = PopoverContext?.ButtonId,
            Anchor = EffectiveAnchor,
            ArrowId = ArrowId,
            Portal = EffectivePortal,
            PortalTarget = PortalTarget,
            Modal = modal,
            Layer = layer,
            Focus = new BitBhFocusScopeOptions
            {
                Trap = modal,
                AutoFocus = AutoFocus ? "first" : "none",
                RestoreFocus = true,
                ReturnFocusId = PopoverContext?.ButtonId,
            },
        };
    }

    protected override Task OnDismissAsync(BitBhDismissReason reason)
        => PopoverContext?.DismissAsync(reason) ?? Task.CompletedTask;

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        if (!ShouldRenderContent) return;

        Scope.Owner = PanelId;

        builder.OpenElement(0, Tag);
        builder.AddAttribute(10, "id", PanelId);
        builder.AddMultipleAttributes(20, GetFinalAttributes());
        if (IsHidden)
            builder.AddAttribute(30, "hidden", true);
        builder.AddAttribute(35, "onkeydown",
            On<KeyboardEventArgs>("onkeydown", HandleKeyDown));
        // While open, keep Escape from also closing an enclosing popover / dialog.
        builder.AddEventStopPropagationAttribute(36, "onkeydown", IsOpen);
        builder.AddElementReferenceCapture(40, CaptureElement);
        if (Template is not null || ChildContent is not null)
            BitBhFloatingScope.Render(builder, 50, Scope, BitBhContent.Pick(Template, ChildContent, RenderContext));
        builder.CloseElement();
    }

    protected override Dictionary<string, object> BuildComponentAttributes()
    {
        var attrs = base.BuildComponentAttributes();
        // tabindex=-1 so the panel is programmatically focusable when it has no
        // focusable descendants, but isn't in the natural tab order.
        attrs["tabindex"] = "-1";
        SetDataState(attrs, IsOpen);
        return attrs;
    }

    private Task HandleKeyDown(KeyboardEventArgs args)
    {
        if (args.Key == "Escape" && IsOpen && PopoverContext is not null)
            return PopoverContext.DismissAsync(BitBhDismissReason.EscapeKey);
        return Task.CompletedTask;
    }
}
