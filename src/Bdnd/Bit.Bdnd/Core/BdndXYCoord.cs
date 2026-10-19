namespace Bit.Bdnd;

/// <summary>
/// Represents an { x, y } coordinate pair, mirroring React DnD's <c>XYCoord</c>.
/// Used for the various client/source offsets exposed by the monitor.
/// </summary>
/// <param name="X">The horizontal client coordinate, in CSS pixels.</param>
/// <param name="Y">The vertical client coordinate, in CSS pixels.</param>
public readonly record struct BdndXYCoord(double X, double Y)
{
    public static BdndXYCoord operator -(BdndXYCoord a, BdndXYCoord b) => new(a.X - b.X, a.Y - b.Y);
    public static BdndXYCoord operator +(BdndXYCoord a, BdndXYCoord b) => new(a.X + b.X, a.Y + b.Y);
}
