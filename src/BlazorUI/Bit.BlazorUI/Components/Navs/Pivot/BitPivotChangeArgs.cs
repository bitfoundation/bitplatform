namespace Bit.BlazorUI;

/// <summary>
/// Arguments for the OnChanging callback of BitPivot.
/// Set <see cref="Cancel"/> to true to keep the pivot on the item that is currently selected.
/// </summary>
public class BitPivotChangeArgs
{
    /// <summary>
    /// Creates a new instance of <see cref="BitPivotChangeArgs"/>.
    /// </summary>
    /// <param name="item">
    /// The pivot item the selection is about to move to.
    /// </param>
    public BitPivotChangeArgs(BitPivotItem item)
    {
        Item = item;
    }

    /// <summary>
    /// The pivot item the selection is about to move to.
    /// </summary>
    public BitPivotItem Item { get; }

    /// <summary>
    /// Set to true to cancel the change and keep the item that is currently selected.
    /// </summary>
    public bool Cancel { get; set; }
}
