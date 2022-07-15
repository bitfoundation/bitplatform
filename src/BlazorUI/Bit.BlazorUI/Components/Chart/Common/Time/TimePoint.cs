namespace Bit.BlazorUI;

/// <summary>
/// Represents a point on a plane with an X and a Y-value where the
/// X-value is represented by a <see cref="DateTime"/>.
/// <para>Should be used together with a <see cref="Axes.TimeAxis"/>.</para>
/// </summary>
public readonly struct TimePoint : IEquatable<TimePoint>
{
    /// <summary>
    /// Gets the time-value of this <see cref="TimePoint"/>.
    /// It represents the X-value and will be serialized as 't'.
    /// </summary>
    [Newtonsoft.Json.JsonProperty("t")]
    public DateTime Time { get; }

    /// <summary>
    /// Gets the Y-value of this <see cref="TimePoint"/>.
    /// </summary>
    public double Y { get; }

    /// <summary>
    /// Creates a new instance of <see cref="TimePoint"/>.
    /// </summary>
    /// <param name="time">The X / time-value for this <see cref="TimePoint"/>.</param>
    /// <param name="y">The Y-value for this <see cref="TimePoint"/>.</param>
    public TimePoint(DateTime time, double y)
    {
        Time = time;
        Y = y;
    }

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
    public override bool Equals(object obj) => obj is TimePoint point && Equals(point);
    public bool Equals(TimePoint other) => Time == other.Time && Y == other.Y;
    public override int GetHashCode() => HashCode.Combine(Time, Y);

    public static bool operator ==(TimePoint left, TimePoint right) => left.Equals(right);
    public static bool operator !=(TimePoint left, TimePoint right) => !(left == right);
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
}
