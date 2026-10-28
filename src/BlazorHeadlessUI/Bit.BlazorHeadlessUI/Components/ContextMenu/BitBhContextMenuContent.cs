using Microsoft.AspNetCore.Components;

namespace Bit.BlazorHeadlessUI;

/// <summary>
/// The menu panel of a <see cref="BitBhContextMenu"/> (<c>role="menu"</c>), positioned
/// at the pointer with collision handling. Accepts the same children as
/// <see cref="BitBhMenuItems"/>: items, checkbox / radio items, groups, labels,
/// separators and submenus.
///
/// <para>Emits <c>data-side</c> / <c>data-align</c> describing the resolved placement.</para>
/// </summary>
public class BitBhContextMenuContent : BitBhMenuContentBase, IBitBhPointAnchoredContent
{
    [CascadingParameter]
    private BitBhContextMenuContext? MenuContext { get; set; }

    /// <summary>Minimum distance (px) kept between the menu and the viewport edges. Default: 8.</summary>
    [Parameter]
    public int CollisionPadding { get; set; } = 8;

    protected override bool IsOpen => MenuContext?.IsOpen ?? false;

    protected override string ContentId => MenuContext?.ContentId ?? ComponentId;

    protected override BitBhAnchorOptions? EffectiveAnchor => Anchor ?? new BitBhAnchorOptions
    {
        To = "bottom start",
        Padding = CollisionPadding,
    };

    protected override void OnInitialized()
    {
        if (MenuContext is not null) MenuContext.Menu.Content = this;
    }

    private protected override BitBhMenuLevel CreateLevel(Action refresh)
        => new((IBitBhMenuRoot?)MenuContext?.Menu ?? DetachedMenuRoot.Instance, null, null, ContentId, Interop, refresh);

    private protected override BitBhMenuFocus ConsumeFocusIntent() => MenuContext?.Menu.ConsumeFocusIntent() ?? BitBhMenuFocus.Container;

    private protected override BitBhMenuFocus PeekFocusIntent() => MenuContext?.Menu.PeekFocusIntent() ?? BitBhMenuFocus.Container;

    protected override BitBhFloatingRequest CreateFloatingRequest()
    {
        var layer = new BitBhLayerOptions { ParentHandle = ParentLayerHandle };
        // A right-click in the trigger area while open moves the menu (BitBhContextMenu.OpenAtAsync) rather
        // than dismissing it and opening a new one, exit animation and all.
        layer.SecondaryPressIds.Add(MenuContext?.TriggerId);
        return new()
        {
            ContentId = ContentId,
            Point = (MenuContext?.X ?? 0, MenuContext?.Y ?? 0),
            Anchor = EffectiveAnchor,
            ArrowId = ArrowId,
            Portal = EffectivePortal,
            PortalTarget = PortalTarget,
            Layer = layer,
            Focus = CreateFocusOptions(null),
        };
    }

    protected override Task OnDismissAsync(BitBhDismissReason reason)
        => MenuContext?.CloseAsync() ?? Task.CompletedTask;

    async Task IBitBhPointAnchoredContent.MoveToAsync(double x, double y)
    {
        Level.Reset();
        await Floating.SetPointAsync(x, y);
    }

    public override ValueTask DisposeAsync()
    {
        if (MenuContext?.Menu.Content == this) MenuContext.Menu.Content = null;
        return base.DisposeAsync();
    }
}
