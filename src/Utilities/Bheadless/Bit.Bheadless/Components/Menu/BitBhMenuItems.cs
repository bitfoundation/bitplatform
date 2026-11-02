using Microsoft.AspNetCore.Components;

namespace Bit.Bheadless;

/// <summary>
/// The popup panel containing the menu's items. Renders as <c>&lt;div role="menu"&gt;</c>
/// by default and is hidden via the HTML <c>hidden</c> attribute while closed.
///
/// <para>
/// Set <see cref="BitBhFloatingContentBase.Anchor"/> to position it relative to the
/// <see cref="BitBhMenuButton"/> (with flip / shift and the <c>--button-width</c>,
/// <c>--bit-bh-available-height</c> … CSS variables) and
/// <see cref="BitBhFloatingContentBase.Portal"/> to render it at the end of <c>&lt;body&gt;</c>.
/// </para>
/// </summary>
public class BitBhMenuItems : BitBhMenuContentBase
{
    [CascadingParameter]
    private BitBhMenuContext? MenuContext { get; set; }

    protected override bool IsOpen => MenuContext?.IsOpen ?? false;

    protected override string ContentId => MenuContext?.ItemsId ?? ComponentId;

    protected override string? LabelledById => MenuContext?.ButtonId;

    private protected override BitBhMenuLevel CreateLevel(Action refresh)
        => new((IBitBhMenuRoot?)MenuContext?.Menu ?? DetachedMenuRoot.Instance, null, null, ContentId, Interop, refresh);

    private protected override BitBhMenuFocus ConsumeFocusIntent() => MenuContext?.Menu.ConsumeFocusIntent() ?? BitBhMenuFocus.Container;

    private protected override BitBhMenuFocus PeekFocusIntent() => MenuContext?.Menu.PeekFocusIntent() ?? BitBhMenuFocus.Container;

    protected override BitBhFloatingRequest CreateFloatingRequest()
    {
        var layer = new BitBhLayerOptions { ParentHandle = ParentLayerHandle };
        layer.BranchIds.Add(MenuContext?.ButtonId);
        return new BitBhFloatingRequest
        {
            ContentId = ContentId,
            ReferenceId = MenuContext?.ButtonId,
            Anchor = EffectiveAnchor,
            ArrowId = ArrowId,
            Portal = EffectivePortal,
            PortalTarget = PortalTarget,
            Layer = layer,
            Focus = CreateFocusOptions(MenuContext?.ButtonId),
        };
    }

    protected override Task OnDismissAsync(BitBhDismissReason reason)
        => MenuContext?.CloseAsync() ?? Task.CompletedTask;
}

/// <summary>Root used when a menu panel is rendered outside of a menu (design-time / tests).</summary>
internal sealed class DetachedMenuRoot : IBitBhMenuRoot
{
    public static readonly DetachedMenuRoot Instance = new();
    public bool IsOpen => false;
    public bool Disabled => false;
    public BitBhDirection Dir => BitBhDirection.Ltr;
    public string? TriggerId => null;
    public BitBhMenubarContext? Menubar => null;
    public Task CloseAsync() => Task.CompletedTask;
}
