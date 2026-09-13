namespace Bit.BlazorUI;

/// <summary>
/// The details of a reorder requested on the header of a <see cref="BitPivot"/>, either by dragging a
/// tab onto another one or by moving the focused tab with the Ctrl+Arrow keys.
/// </summary>
public class BitPivotReorderEventArgs
{
    /// <summary>
    /// The pivot item that is being moved.
    /// </summary>
    public BitPivotItem Item { get; init; } = default!;

    /// <summary>
    /// The index the item is moving from.
    /// </summary>
    public int OldIndex { get; init; }

    /// <summary>
    /// The index the item is moving to.
    /// </summary>
    public int NewIndex { get; init; }
}
