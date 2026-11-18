namespace Bit.BlazorUI;

/// <summary>
/// Arguments raised when a row is reordered via drag-and-drop. Mirrors the intent of
/// react-data-grid's row reordering example.
/// </summary>
/// <typeparam name="TItem">The row item type.</typeparam>
public sealed class BitDataGridRowReorderEventArgs<TItem>
{
    /// <summary>The item of the row that was moved.</summary>
    public required TItem DraggedItem { get; init; }

    /// <summary>The item of the row it was dropped on, whose place it takes: the moved row ends up where this one was,
    /// and this row and the ones between the two move one place toward where the moved row came from.</summary>
    public required TItem TargetItem { get; init; }

    /// <summary>
    /// The original index of the dragged item within the bound source, or <c>null</c> when the index
    /// is unavailable (for example when the bound <c>Items</c> is not an indexable <see cref="IList{T}"/>).
    /// </summary>
    public int? FromIndex { get; init; }

    /// <summary>
    /// The destination index within the bound source, or <c>null</c> when the index is unavailable
    /// (for example when the bound <c>Items</c> is not an indexable <see cref="IList{T}"/>).
    /// </summary>
    public int? ToIndex { get; init; }
}
