namespace Bit.BlazorUI;

/// <summary>
/// Arguments raised when a row is reordered via drag-and-drop. Mirrors the intent of
/// react-data-grid's row reordering example.
/// </summary>
/// <typeparam name="TItem">The row item type.</typeparam>
public sealed class BitDataGridRowReorderEventArgs<TItem>
{
    public required TItem DraggedItem { get; init; }
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
