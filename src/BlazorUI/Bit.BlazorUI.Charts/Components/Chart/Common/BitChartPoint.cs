namespace Bit.BlazorUI;

/// <summary>
/// Represents a point on a plane with an X and a Y-value.
/// </summary>
public readonly struct BitChartPoint : IEquatable<BitChartPoint>
{
    /// <summary>
    /// Gets the X-value of this <see cref="BitChartPoint"/>.
    /// </summary>
    public double X { get; }

    /// <summary>
    /// Gets the Y-value of this <see cref="BitChartPoint"/>.
    /// </summary>
    public double Y { get; }

    /// <summary>
    /// Creates a new instance of <see cref="BitChartPoint"/>.
    /// </summary>
    /// <param name="x">The X-value for this <see cref="BitChartPoint"/>.</param>
    /// <param name="y">The Y-value for this <see cref="BitChartPoint"/>.</param>
    public BitChartPoint(double x, double y)
    {
        X = x;
        Y = y;
    }

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
    public override bool Equals(object obj) => obj is BitChartPoint point && Equals(point);
    public bool Equals(BitChartPoint other) => X == other.X && Y == other.Y;
    public override int GetHashCode() => HashCode.Combine(X, Y);

    public static bool operator ==(BitChartPoint left, BitChartPoint right) => left.Equals(right);
    public static bool operator !=(BitChartPoint left, BitChartPoint right) => !(left == right);
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
}
