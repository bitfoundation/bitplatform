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

/// <summary>
/// Cascading context provided by <see cref="BitBhNavigationMenuItem"/> to its trigger, content and links.
/// </summary>
public sealed class BitBhNavigationMenuItemContext
{
    internal BitBhNavigationMenuItemContext(BitBhNavigationMenuItem item) => Item = item;

    internal BitBhNavigationMenuItem Item { get; }

    /// <summary>The item's value (its own, or a generated one).</summary>
    public required string Value { get; init; }

    /// <summary>The base id the trigger and content ids derive from.</summary>
    public required string BaseId { get; init; }

    /// <summary>Whether the item's content is open.</summary>
    public bool IsOpen { get; init; }

    /// <summary>Whether the item has a <see cref="BitBhNavigationMenuContent"/>.</summary>
    public bool HasContent { get; init; }

    /// <summary>The deterministic id of the item's trigger.</summary>
    public string TriggerId => $"{BaseId}-trigger";

    /// <summary>The deterministic id of the item's content.</summary>
    public string ContentId => $"{BaseId}-content";
}

/// <summary>Render-fragment context exposed by the trigger and content of a navigation menu item.</summary>
public sealed record BitBhNavigationMenuRenderContext
{
    /// <summary>Whether the item's content is open.</summary>
    public required bool IsOpen { get; init; }

    /// <summary>Closes the item's content.</summary>
    public required Action Close { get; init; }
}

/// <summary>
/// The triggers and links one set of arrow keys moves between: the top-level items of a
/// <see cref="BitBhNavigationMenuList"/> (Left / Right, or Up / Down when vertical), or the links of one
/// <see cref="BitBhNavigationMenuContent"/> (every arrow). Every member stays in the Tab order.
/// </summary>
internal sealed class BitBhNavigationMenuFocusGroup
{
    private readonly BitBhInterop _interop;

    public BitBhNavigationMenuFocusGroup(BitBhInterop interop, bool isList)
    {
        _interop = interop;
        IsList = isList;
        Items = new BitBhCollection<IBitBhCollectionItem>(BitBhId.New("bit-bh-navmenu"));
    }

    /// <summary>The triggers and links, in document order.</summary>
    public BitBhCollection<IBitBhCollectionItem> Items { get; }

    /// <summary>Whether the group is a menu's list (the top-level items) rather than a content's links.</summary>
    public bool IsList { get; }

    /// <summary>The menu the group belongs to; set by its owner on every render.</summary>
    public BitBhNavigationMenuContext? Menu { get; set; }

    private bool Horizontal => Menu?.Orientation != BitBhOrientation.Vertical;

    private bool Rtl => Menu?.Dir == BitBhDirection.Rtl;

    /// <summary>The key that moves from a top-level trigger into its content.</summary>
    public string EntryKey => Horizontal ? "ArrowDown" : Rtl ? "ArrowLeft" : "ArrowRight";

    /// <summary>The keys the group handles, for <c>data-bit-bh-keys</c>.</summary>
    public string KeysAttribute => !IsList
        ? "ArrowUp ArrowDown ArrowLeft ArrowRight Home End"
        : Horizontal ? "ArrowLeft ArrowRight Home End" : "ArrowUp ArrowDown Home End";

    /// <summary>Moves focus for an arrow / Home / End key. Returns whether the key was the group's.</summary>
    public async Task<bool> HandleKeyDownAsync(IBitBhCollectionItem item, KeyboardEventArgs e)
    {
        if (e.CtrlKey || e.MetaKey || e.AltKey) return false;
        var next = Rtl ? "ArrowLeft" : "ArrowRight";
        var previous = Rtl ? "ArrowRight" : "ArrowLeft";
        var step = e.Key switch
        {
            "Home" or "End" => 0,
            _ when IsList && Horizontal => e.Key == next ? 1 : e.Key == previous ? -1 : (int?)null,
            _ when IsList => e.Key == "ArrowDown" ? 1 : e.Key == "ArrowUp" ? -1 : (int?)null,
            _ => e.Key == "ArrowDown" || e.Key == next ? 1 : e.Key == "ArrowUp" || e.Key == previous ? -1 : (int?)null,
        };
        if (step is null) return false;

        // Picked in document order: members that joined since the last key are in place first.
        await Items.EnsureOrderAsync(_interop);
        var loop = Menu?.Loop ?? false;
        var target = e.Key switch
        {
            "Home" => Items.First(),
            "End" => Items.Last(),
            _ => step > 0 ? Items.Next(item, loop) : Items.Previous(item, loop),
        };
        if (target is not null && !ReferenceEquals(target, item))
            await _interop.FocusAsync(target.ItemId);
        return true;
    }
}
