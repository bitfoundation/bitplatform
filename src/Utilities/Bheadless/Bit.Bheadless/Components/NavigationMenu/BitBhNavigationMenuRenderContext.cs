namespace Bit.Bheadless;

/// <summary>Render-fragment context exposed by the trigger and content of a navigation menu item.</summary>
public sealed record BitBhNavigationMenuRenderContext
{
    /// <summary>Whether the item's content is open.</summary>
    public required bool IsOpen { get; init; }

    /// <summary>Closes the item's content.</summary>
    public required Action Close { get; init; }
}
