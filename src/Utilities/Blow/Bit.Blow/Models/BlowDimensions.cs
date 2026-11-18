namespace Bit.Blow;

/// <summary>
/// A measured width/height in pixels (at zoom = 1).
/// </summary>
public readonly record struct BlowDimensions(double Width, double Height)
{
    /// <summary>A zero size, the value a node carries until it has been measured.</summary>
    public static readonly BlowDimensions Empty = new(0, 0);

    /// <summary>True when either dimension is zero or negative, i.e. nothing has been measured yet.</summary>
    public bool IsEmpty => Width <= 0 || Height <= 0;
}
