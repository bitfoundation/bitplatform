using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.Web;

namespace Bit.Bheadless;

/// <summary>
/// An optional shared panel for a <see cref="BitBhNavigationMenu"/>: while an item is open its
/// <see cref="BitBhNavigationMenuContent"/> is moved into the viewport (the component tree stays where it is), so
/// one surface - under the whole bar, say - shows every item's content and can animate between them. The Tab
/// order and the reading order stay those of a content that follows its trigger.
///
/// <code>
/// .nav-viewport {
///     position: relative; overflow: hidden;
///     width: var(--bit-bh-navigation-menu-viewport-width);
///     height: var(--bit-bh-navigation-menu-viewport-height);
///     transition: width 250ms, height 250ms;
/// }
/// .nav-content { position: absolute; top: 0; left: 0; }   /* contents are laid out over one another */
/// </code>
///
/// <para><b>Styling hooks:</b> <c>data-state="open|closed"</c> (hidden while closed, visible while an exit animation
/// of its own or of the content in it runs), <c>data-orientation</c>, and
/// <c>--bit-bh-navigation-menu-viewport-width</c> / <c>-height</c>: the size of the open content, followed as it
/// changes and kept while the viewport closes.</para>
/// </summary>
public class BitBhNavigationMenuViewport : BitBhComponentBase, IAsyncDisposable
{
    [CascadingParameter]
    private BitBhNavigationMenuContext? MenuContext { get; set; }

    protected override string DefaultTag => "div";

    private bool IsOpen => MenuContext?.IsOpen ?? false;

    protected override void OnInitialized() => MenuContext?.Root.RegisterViewport(ComponentId);

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        builder.OpenElement(0, Tag);
        builder.AddAttribute(10, "id", ComponentId);
        builder.AddMultipleAttributes(20, GetFinalAttributes());
        if (!IsOpen)
            builder.AddAttribute(25, "hidden", true);
        builder.AddAttribute(30, "onpointerenter", On<PointerEventArgs>("onpointerenter", HandlePointerEnter));
        builder.AddAttribute(31, "onpointerleave", On<PointerEventArgs>("onpointerleave", HandlePointerLeave));
        builder.AddElementReferenceCapture(40, CaptureElement);
        builder.CloseElement();
    }

    protected override Dictionary<string, object> BuildComponentAttributes()
    {
        var attrs = base.BuildComponentAttributes();
        SetDataState(attrs, IsOpen);
        if (MenuContext is not null) SetDataValue(attrs, "orientation", MenuContext.Orientation.ToAttribute());
        // Stays visible while it, or the content closing in it, animates out.
        attrs["data-bit-bh-presence"] = "subtree";
        return attrs;
    }

    private void HandlePointerEnter(PointerEventArgs args)
    {
        if (args.PointerType == "mouse") MenuContext?.Root.ContentEnter();
    }

    private void HandlePointerLeave(PointerEventArgs args)
    {
        if (args.PointerType == "mouse") MenuContext?.Root.ContentLeave();
    }

    public async ValueTask DisposeAsync()
    {
        if (MenuContext?.ViewportId == ComponentId) MenuContext.Root.RegisterViewport(null);
        await Interop.NavigationMenuReleaseAsync(ComponentId);
        GC.SuppressFinalize(this);
    }
}
