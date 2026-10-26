using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;

namespace Bit.BlazorHeadlessUI;

/// <summary>
/// The nested panel of a <see cref="BitBhMenuSub"/> (<c>role="menu"</c>). Positioned
/// next to its trigger — <c>"right start"</c> by default (<c>"left start"</c> in RTL),
/// flipping when there isn't room — unless <see cref="BitBhFloatingContentBase.Anchor"/>
/// is set explicitly.
/// </summary>
public class BitBhMenuSubContent : BitBhMenuContentBase
{
    [CascadingParameter]
    private BitBhMenuSub? Sub { get; set; }

    /// <summary>Gap (px) between the trigger item and the submenu when using the default anchor. Default: 0.</summary>
    [Parameter]
    public int SideOffset { get; set; }

    /// <summary>Alignment offset (px) when using the default anchor. Default: -4 (aligns first items).</summary>
    [Parameter]
    public int AlignOffset { get; set; } = -4;

    protected override bool IsOpen => Sub?.IsOpen ?? false;

    protected override string ContentId => Sub?.ContentId ?? ComponentId;

    protected override string? LabelledById => Sub?.TriggerId;

    protected override BitBhAnchorOptions? EffectiveAnchor => Anchor ?? new BitBhAnchorOptions
    {
        To = Sub?.Dir == BitBhDirection.Rtl ? "left start" : "right start",
        Gap = SideOffset,
        Offset = AlignOffset,
    };

    protected override void OnInitialized()
    {
        if (Sub is not null) Sub.Content = this;
    }

    private protected override BitBhMenuLevel CreateLevel(Action refresh)
        => new(Sub?.Parent?.Root ?? DetachedMenuRoot.Instance, Sub?.Parent, Sub, ContentId, Interop, refresh);

    private protected override BitBhMenuFocus ConsumeFocusIntent() => Sub?.ConsumeFocusIntent() ?? BitBhMenuFocus.None;

    private protected override BitBhMenuFocus PeekFocusIntent() => Sub?.PeekFocusIntent() ?? BitBhMenuFocus.None;

    protected override BitBhFloatingRequest CreateFloatingRequest()
    {
        var layer = new BitBhLayerOptions { Escape = false, ParentHandle = ParentLayerHandle };
        layer.BranchIds.Add(Sub?.TriggerId);
        return new BitBhFloatingRequest
        {
            ContentId = ContentId,
            ReferenceId = Sub?.TriggerId,
            Anchor = EffectiveAnchor,
            ArrowId = ArrowId,
            Portal = EffectivePortal,
            PortalTarget = PortalTarget,
            Layer = layer,
            Focus = CreateFocusOptions(Sub?.Parent?.ContentId),
        };
    }

    protected override Task OnDismissAsync(BitBhDismissReason reason)
        => Sub?.CloseAsync(focusParent: false) ?? Task.CompletedTask;

    protected override Task OnPointerEnterContentAsync(PointerEventArgs args)
    {
        Sub?.CancelClose();
        return Task.CompletedTask;
    }

    internal void Refresh() => StateHasChanged();

    /// <summary>Moves keyboard focus into an already-open submenu.</summary>
    internal async Task EnterAsync(BitBhMenuFocus focus)
    {
        await Level.FocusAsync();
        await Level.Items.EnsureOrderAsync(Interop);
        Level.SetActive(focus == BitBhMenuFocus.Last ? Level.Items.Last() : Level.Items.First());
    }

    public override ValueTask DisposeAsync()
    {
        if (Sub?.Content == this) Sub.Content = null;
        return base.DisposeAsync();
    }
}
