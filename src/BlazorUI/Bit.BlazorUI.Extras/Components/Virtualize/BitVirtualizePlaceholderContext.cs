namespace Bit.BlazorUI;

/// <summary>
/// The context passed to the placeholder template of the BitVirtualize component while real items are being loaded.
/// </summary>
public readonly struct BitVirtualizePlaceholderContext
{
    /// <summary>
    /// Creates a new <see cref="BitVirtualizePlaceholderContext"/>.
    /// </summary>
    public BitVirtualizePlaceholderContext(int index, double size)
    {
        Index = index;
        Size = size;
    }

    /// <summary>
    /// The zero-based index of the item this placeholder represents.
    /// </summary>
    public int Index { get; }

    /// <summary>
    /// The estimated size (px) reserved for the placeholder along the scroll axis.
    /// </summary>
    public double Size { get; }
}
