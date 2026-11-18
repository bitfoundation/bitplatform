namespace Bit.Bheadless;

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
