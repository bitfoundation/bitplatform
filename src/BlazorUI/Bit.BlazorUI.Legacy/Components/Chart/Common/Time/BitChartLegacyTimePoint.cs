namespace Bit.BlazorUI.Legacy;

/// <summary>
/// Represents a point on a plane with an X and a Y-value where the
/// X-value is represented by a <see cref="DateTime"/>.
/// <para>Should be used together with a <see cref="BitChartLegacyTimeAxis"/>.</para>
/// </summary>
public readonly struct BitChartLegacyTimePoint : IEquatable<BitChartLegacyTimePoint>
{
    /// <summary>
    /// Gets the time-value of this <see cref="BitChartLegacyTimePoint"/>.
    /// It represents the X-value and will be serialized as 't'.
    /// </summary>
    [Newtonsoft.Json.JsonProperty("t")]
    public DateTime Time { get; }

    /// <summary>
    /// Gets the Y-value of this <see cref="BitChartLegacyTimePoint"/>.
    /// </summary>
    public double Y { get; }

    /// <summary>
    /// Creates a new instance of <see cref="BitChartLegacyTimePoint"/>.
    /// </summary>
    /// <param name="time">The X / time-value for this <see cref="BitChartLegacyTimePoint"/>.</param>
    /// <param name="y">The Y-value for this <see cref="BitChartLegacyTimePoint"/>.</param>
    public BitChartLegacyTimePoint(DateTime time, double y)
    {
        Time = time;
        Y = y;
    }

    public override bool Equals(object? obj) => obj is BitChartLegacyTimePoint point && Equals(point);
    public bool Equals(BitChartLegacyTimePoint other) => Time == other.Time && Y == other.Y;
    public override int GetHashCode() => HashCode.Combine(Time, Y);

    public static bool operator ==(BitChartLegacyTimePoint left, BitChartLegacyTimePoint right) => left.Equals(right);
    public static bool operator !=(BitChartLegacyTimePoint left, BitChartLegacyTimePoint right) => !(left == right);
}
