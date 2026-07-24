namespace Bit.BlazorUI.Legacy;

/// <summary>
/// Represents a point on a plane with an X and a Y-value.
/// </summary>
public readonly struct BitChartLegacyPoint : IEquatable<BitChartLegacyPoint>
{
    /// <summary>
    /// Gets the X-value of this <see cref="BitChartLegacyPoint"/>.
    /// </summary>
    public double X { get; }

    /// <summary>
    /// Gets the Y-value of this <see cref="BitChartLegacyPoint"/>.
    /// </summary>
    public double Y { get; }

    /// <summary>
    /// Creates a new instance of <see cref="BitChartLegacyPoint"/>.
    /// </summary>
    /// <param name="x">The X-value for this <see cref="BitChartLegacyPoint"/>.</param>
    /// <param name="y">The Y-value for this <see cref="BitChartLegacyPoint"/>.</param>
    public BitChartLegacyPoint(double x, double y)
    {
        X = x;
        Y = y;
    }

    public override bool Equals(object? obj) => obj is BitChartLegacyPoint point && Equals(point);
    public bool Equals(BitChartLegacyPoint other) => X == other.X && Y == other.Y;
    public override int GetHashCode() => HashCode.Combine(X, Y);

    public static bool operator ==(BitChartLegacyPoint left, BitChartLegacyPoint right) => left.Equals(right);
    public static bool operator !=(BitChartLegacyPoint left, BitChartLegacyPoint right) => !(left == right);
}
