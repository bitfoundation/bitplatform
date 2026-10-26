namespace Bit.BlazorHeadlessUI;

/// <summary>State of one item of a <see cref="BitBhSortable{TItem}"/>, passed to its template.</summary>
/// <typeparam name="TItem">The item type.</typeparam>
public sealed record BitBhSortableItemContext<TItem>
{
    /// <summary>The item.</summary>
    public required TItem Item { get; init; }

    /// <summary>The item's current (preview) position.</summary>
    public required int Index { get; init; }

    /// <summary>Whether the item is being moved (keyboard or pointer).</summary>
    public required bool IsDragging { get; init; }

    /// <summary>Whether reordering is disabled.</summary>
    public required bool Disabled { get; init; }
}
