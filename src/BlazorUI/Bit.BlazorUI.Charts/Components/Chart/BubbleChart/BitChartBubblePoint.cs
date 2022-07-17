namespace Bit.BlazorUI;

/// <summary>
/// Represents a bubble on a plane with an X, Y and radius value.
/// </summary>
public readonly struct BitChartBubblePoint : IEquatable<BitChartBubblePoint>
{
    /// <summary>
    /// Gets the X-value of this <see cref="BitChartBubblePoint"/>.
    /// </summary>
    public double X { get; }

    /// <summary>
    /// Gets the Y-value of this <see cref="BitChartBubblePoint"/>.
    /// </summary>
    public double Y { get; }

    /// <summary>
    /// Gets the radius of this <see cref="BitChartBubblePoint"/> in pixels. Will be serialized as 'r'.
    /// <para>
    /// Important: this property is not scaled by the chart,
    /// it is the raw radius in pixels of the bubble that is drawn on the canvas.
    /// </para>
    /// </summary>
    [Newtonsoft.Json.JsonProperty("r")]
    public double Radius { get; }

    /// <summary>
    /// Creates a new instance of <see cref="BitChartBubblePoint"/>.
    /// </summary>
    /// <param name="x">The X-value for this <see cref="BitChartBubblePoint"/>.</param>
    /// <param name="y">The Y-value for this <see cref="BitChartBubblePoint"/>.</param>
    /// <param name="radius">The radius for this <see cref="BitChartBubblePoint"/> in pixels.</param>
    public BitChartBubblePoint(double x, double y, double radius)
    {
        X = x;
        Y = y;
        Radius = radius;
    }

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
    public override bool Equals(object obj) => obj is BitChartBubblePoint point && Equals(point);
    public bool Equals(BitChartBubblePoint other) => X == other.X && Y == other.Y && Radius == other.Radius;
    public override int GetHashCode() => HashCode.Combine(X, Y, Radius);

    public static bool operator ==(BitChartBubblePoint left, BitChartBubblePoint right) => left.Equals(right);
    public static bool operator !=(BitChartBubblePoint left, BitChartBubblePoint right) => !(left == right);
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
}
