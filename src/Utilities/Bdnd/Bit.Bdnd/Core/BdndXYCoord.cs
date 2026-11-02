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

    /// <summary>
    /// This offset kept to <paramref name="axis"/>: the other coordinate is <paramref name="origin"/>'s - for a
    /// preview that only moves up and down a list (<see cref="BdndAxis.Vertical"/>) or along a row.
    /// </summary>
    public BdndXYCoord KeepTo(BdndAxis axis, BdndXYCoord origin) => axis switch
    {
        BdndAxis.Horizontal => this with { Y = origin.Y },
        BdndAxis.Vertical => this with { X = origin.X },
        _ => this,
    };

    /// <summary>
    /// This offset moved to the nearest corner of a grid of <paramref name="width"/> by <paramref name="height"/>
    /// cells laid from <paramref name="origin"/> - for a preview that moves cell by cell. A size of zero or less leaves
    /// that coordinate as it is.
    /// </summary>
    public BdndXYCoord SnapTo(double width, double height, BdndXYCoord origin = default) => new(
        width > 0 ? origin.X + Math.Round((X - origin.X) / width) * width : X,
        height > 0 ? origin.Y + Math.Round((Y - origin.Y) / height) * height : Y);

    /// <summary>
    /// This offset kept between <paramref name="min"/> and <paramref name="max"/> - for a preview that stays inside an
    /// area (its top-left corner between the area's top-left and its bottom-right less the preview's size).
    /// </summary>
    public BdndXYCoord Clamp(BdndXYCoord min, BdndXYCoord max) =>
        new(Math.Min(Math.Max(X, min.X), Math.Max(min.X, max.X)), Math.Min(Math.Max(Y, min.Y), Math.Max(min.Y, max.Y)));
}
