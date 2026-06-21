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
    public required int FromIndex { get; init; }
    public required int ToIndex { get; init; }
}
