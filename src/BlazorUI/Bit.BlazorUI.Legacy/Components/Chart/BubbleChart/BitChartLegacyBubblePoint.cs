namespace Bit.BlazorUI.Legacy;

/// <summary>
/// Represents a bubble on a plane with an X, Y and radius value.
/// </summary>
public readonly struct BitChartLegacyBubblePoint : IEquatable<BitChartLegacyBubblePoint>
{
    /// <summary>
    /// Gets the X-value of this <see cref="BitChartLegacyBubblePoint"/>.
    /// </summary>
    public double X { get; }

    /// <summary>
    /// Gets the Y-value of this <see cref="BitChartLegacyBubblePoint"/>.
    /// </summary>
    public double Y { get; }

    /// <summary>
    /// Gets the radius of this <see cref="BitChartLegacyBubblePoint"/> in pixels. Will be serialized as 'r'.
    /// <para>
    /// Important: this property is not scaled by the chart,
    /// it is the raw radius in pixels of the bubble that is drawn on the canvas.
    /// </para>
    /// </summary>
    [Newtonsoft.Json.JsonProperty("r")]
    public double Radius { get; }

    /// <summary>
    /// Creates a new instance of <see cref="BitChartLegacyBubblePoint"/>.
    /// </summary>
    /// <param name="x">The X-value for this <see cref="BitChartLegacyBubblePoint"/>.</param>
    /// <param name="y">The Y-value for this <see cref="BitChartLegacyBubblePoint"/>.</param>
    /// <param name="radius">The radius for this <see cref="BitChartLegacyBubblePoint"/> in pixels.</param>
    public BitChartLegacyBubblePoint(double x, double y, double radius)
    {
        X = x;
        Y = y;
        Radius = radius;
    }

    public override bool Equals(object? obj) => obj is BitChartLegacyBubblePoint point && Equals(point);
    public bool Equals(BitChartLegacyBubblePoint other) => X == other.X && Y == other.Y && Radius == other.Radius;
    public override int GetHashCode() => HashCode.Combine(X, Y, Radius);

    public static bool operator ==(BitChartLegacyBubblePoint left, BitChartLegacyBubblePoint right) => left.Equals(right);
    public static bool operator !=(BitChartLegacyBubblePoint left, BitChartLegacyBubblePoint right) => !(left == right);
}
