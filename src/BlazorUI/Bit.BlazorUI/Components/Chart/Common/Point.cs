namespace Bit.BlazorUI;

/// <summary>
/// Represents a point on a plane with an X and a Y-value.
/// </summary>
public readonly struct Point : IEquatable<Point>
{
    /// <summary>
    /// Gets the X-value of this <see cref="Point"/>.
    /// </summary>
    public double X { get; }

    /// <summary>
    /// Gets the Y-value of this <see cref="Point"/>.
    /// </summary>
    public double Y { get; }

    /// <summary>
    /// Creates a new instance of <see cref="Point"/>.
    /// </summary>
    /// <param name="x">The X-value for this <see cref="Point"/>.</param>
    /// <param name="y">The Y-value for this <see cref="Point"/>.</param>
    public Point(double x, double y)
    {
        X = x;
        Y = y;
    }

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
    public override bool Equals(object obj) => obj is Point point && Equals(point);
    public bool Equals(Point other) => X == other.X && Y == other.Y;
    public override int GetHashCode() => HashCode.Combine(X, Y);

    public static bool operator ==(Point left, Point right) => left.Equals(right);
    public static bool operator !=(Point left, Point right) => !(left == right);
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
}
