using Newtonsoft.Json;

namespace Bit.BlazorUI.Legacy;

/// <summary>
/// Represents how lines are clipped relative to the chart area.
/// As per documentation <a href="https://www.chartjs.org/docs/latest/charts/line.html#line-styling">here (Chart.js)</a>.
/// <para>For any given edge:
/// <list type="bullet">
/// <item>0 means clipping at the chart area.</item>
/// <item>negative values mean clipping inside the chart area.</item>
/// <item>positive values mean clipping outside the chart area.</item>
/// <item><see langword="null"/> means no clipping.</item>
/// </list>
/// </para>
/// </summary>
[JsonConverter(typeof(ClippingJsonConverter))]
public readonly struct BitChartLegacyClipping : IEquatable<BitChartLegacyClipping>
{
    internal readonly bool _equalSides;

    /// <summary>
    /// Gets the clipping for the top edge.
    /// </summary>
    public int? Top { get; }

    /// <summary>
    /// Gets the clipping for the right edge.
    /// </summary>
    public int? Right { get; }

    /// <summary>
    /// Gets the clipping for the bottom edge.
    /// </summary>
    public int? Bottom { get; }

    /// <summary>
    /// Gets the clipping for the left edge.
    /// </summary>
    public int? Left { get; }

    /// <summary>
    /// Creates a new instance of <see cref="BitChartLegacyClipping"/>
    /// using the supplied value for all edges.
    /// </summary>
    /// <param name="all">The clipping value for all edges.</param>
    public BitChartLegacyClipping(int all)
    {
        Top = Right = Bottom = Left = all;
        _equalSides = true;
    }

    /// <summary>
    /// Creates a new instance of <see cref="BitChartLegacyClipping"/>
    /// using individual values for all edges.
    /// </summary>
    /// <param name="bottom">The clipping value for the bottom edge.</param>
    /// <param name="left">The clipping value for the left edge.</param>
    /// <param name="top">The clipping value for the top edge.</param>
    /// <param name="right">The clipping value for the right edge.</param>
    public BitChartLegacyClipping(int? top = null, int? right = null, int? bottom = null, int? left = null)
    {
        Top = top;
        Right = right;
        Bottom = bottom;
        Left = left;
        _equalSides = false;
    }

    /// <summary>
    /// Converts an <see cref="int"/> value to a <see cref="BitChartLegacyClipping"/> implicitly.
    /// The supplied value will be used for all edges.
    /// </summary>
    /// <param name="value">The clipping value for all edges.</param>
    public static implicit operator BitChartLegacyClipping(int value) => new BitChartLegacyClipping(value);

    /// <inheritdoc />
    public override string ToString()
    {
        return $"{nameof(Top)}: {ValOrNull(Top)}, " +
               $"{nameof(Right)}: {ValOrNull(Right)}, " +
               $"{nameof(Bottom)}: {ValOrNull(Bottom)}, " +
               $"{nameof(Left)}: {ValOrNull(Left)}";

        static string ValOrNull(int? value) => value.HasValue ? value.Value.ToString() : "null";
    }

    public override bool Equals(object? obj) => obj is BitChartLegacyClipping clipping && Equals(clipping);

    public bool Equals(BitChartLegacyClipping other)
    {
        if (_equalSides && other._equalSides) return Top == other.Top;

        return Top == other.Top &&
               Right == other.Right &&
               Bottom == other.Bottom &&
               Left == other.Left;
    }

    public override int GetHashCode() => HashCode.Combine(Bottom, Left, Top, Right);

    public static bool operator ==(BitChartLegacyClipping left, BitChartLegacyClipping right) => left.Equals(right);
    public static bool operator !=(BitChartLegacyClipping left, BitChartLegacyClipping right) => !(left == right);
}
