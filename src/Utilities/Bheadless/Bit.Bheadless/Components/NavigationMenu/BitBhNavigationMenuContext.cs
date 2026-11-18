using Microsoft.AspNetCore.Components.Web;

namespace Bit.Bheadless;

/// <summary>
/// Cascading context provided by <see cref="BitBhNavigationMenu"/> to its parts: which item is open (and which
/// was open before it), the orientation and reading direction, and the ids the parts reference each other by.
/// </summary>
public sealed class BitBhNavigationMenuContext
{
    internal BitBhNavigationMenuContext(BitBhNavigationMenu root) => Root = root;

    internal BitBhNavigationMenu Root { get; }

    /// <summary>The root's id.</summary>
    public required string BaseId { get; init; }

    /// <summary>The value of the open item, or null when every item is closed.</summary>
    public string? Value { get; init; }

    /// <summary>The value that was open before <see cref="Value"/> (drives <c>data-motion</c>).</summary>
    public string? PreviousValue { get; init; }

    /// <summary>How the top-level items are laid out.</summary>
    public BitBhOrientation Orientation { get; init; }

    /// <summary>The reading direction.</summary>
    public BitBhDirection Dir { get; init; }

    /// <summary>Whether arrow keys wrap around at the ends.</summary>
    public bool Loop { get; init; }

    /// <summary>Whether the menu is a submenu (rendered inside the content of another navigation menu).</summary>
    public bool IsNested { get; init; }

    /// <summary>The id of the <see cref="BitBhNavigationMenuList"/>, once rendered.</summary>
    public string? ListId { get; init; }

    /// <summary>The id of the <see cref="BitBhNavigationMenuViewport"/> the contents are shown in, when there is one.</summary>
    public string? ViewportId { get; init; }

    /// <summary>Whether an item is open.</summary>
    public bool IsOpen => Value is not null;
}
