using Microsoft.AspNetCore.Components;

namespace Bit.Bheadless;

/// <summary>
/// A decorative marker that follows the open item's trigger of a <see cref="BitBhNavigationMenu"/> - an arrow or a
/// bar under the trigger whose content is showing. A <see cref="BitBhSelectionIndicator"/> preset for the menu:
/// place it inside the <see cref="BitBhNavigationMenuList"/> (it renders an <c>&lt;li&gt;</c> there) or next to
/// it in a positioned container, and size and move it with the CSS variables below.
///
/// <code>
/// .nav-list { position: relative; }
/// .nav-indicator {
///     position: absolute; top: 100%; height: 0.5rem;
///     left: var(--bit-bh-indicator-left); width: var(--bit-bh-indicator-width);
///     transition: left 200ms, width 200ms, opacity 200ms;
/// }
/// .nav-indicator[data-state="hidden"] { opacity: 0; }
/// </code>
///
/// <para><b>Styling hooks:</b> <c>data-state="visible|hidden"</c> (whether an item is open), <c>data-orientation</c>,
/// <c>--bit-bh-indicator-left</c> / <c>-top</c> / <c>-width</c> / <c>-height</c> (the trigger's box in the container's
/// coordinates; kept while hidden), <c>data-measured</c>, <c>data-activation-direction</c>.</para>
/// </summary>
public class BitBhNavigationMenuIndicator : BitBhSelectionIndicator
{
    [CascadingParameter]
    private BitBhNavigationMenuContext? MenuContext { get; set; }

    [CascadingParameter]
    private BitBhNavigationMenuFocusGroup? Group { get; set; }

    /// <summary>An <c>&lt;li&gt;</c> inside the menu's list (a list holds only list items), else a <c>&lt;div&gt;</c>.</summary>
    protected override string DefaultTag => Group is { IsList: true } ? "li" : "div";

    protected override void OnParametersSet()
    {
        // The open trigger of this menu (not of a submenu in its content).
        if (MenuContext is not null && !IsProvided(nameof(Selector)))
            Selector = $"[data-bit-bh-navigation-menu-trigger=\"{MenuContext.BaseId}\"][aria-expanded=\"true\"]";
    }

    protected override Dictionary<string, object> BuildComponentAttributes()
    {
        var attrs = base.BuildComponentAttributes();
        SetDataState(attrs, MenuContext?.IsOpen ?? false, "visible", "hidden");
        if (MenuContext is not null) SetDataValue(attrs, "orientation", MenuContext.Orientation.ToAttribute());
        return attrs;
    }
}
