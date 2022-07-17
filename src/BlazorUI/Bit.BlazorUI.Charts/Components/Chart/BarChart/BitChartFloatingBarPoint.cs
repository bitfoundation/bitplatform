using Newtonsoft.Json;

namespace Bit.BlazorUI;

/// <summary>
/// Represents a floating bar within a bar chart (use in <see cref="BitChartBarDataset{T}"/>).
/// Using this type, the bars will be rendered with gaps between them (floating-bars).
/// <para>
/// When serialized, this value is represented as an array of two numbers.
/// <see cref="Start"/> will be the first number in the array, <see cref="End"/> the second one.
/// </para>
/// </summary>
[JsonConverter(typeof(FloatingBarPointConverter))]
public readonly struct BitChartFloatingBarPoint : IEquatable<BitChartFloatingBarPoint>
{
    /// <summary>
    /// Gets the start-value of this <see cref="BitChartFloatingBarPoint"/>.
    /// When serialized, this will be the first value in the array.
    /// </summary>
    public double Start { get; }

    /// <summary>
    /// Gets the end-value of this <see cref="BitChartFloatingBarPoint"/>.
    /// When serialized, this will be the second value in the array.
    /// </summary>
    public double End { get; }

    /// <summary>
    /// Creates a new instance of <see cref="BitChartFloatingBarPoint"/>.
    /// </summary>
    /// <param name="start">The start-value for this <see cref="BitChartFloatingBarPoint"/>.</param>
    /// <param name="end">The end-value for this <see cref="BitChartFloatingBarPoint"/>.</param>
    public BitChartFloatingBarPoint(double start, double end)
    {
        Start = start;
        End = end;
    }

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
    public override bool Equals(object obj) => obj is BitChartFloatingBarPoint point && Equals(point);
    public bool Equals(BitChartFloatingBarPoint other) => Start == other.Start && End == other.End;
    public override int GetHashCode() => HashCode.Combine(Start, End);

    public static bool operator ==(BitChartFloatingBarPoint left, BitChartFloatingBarPoint right) => left.Equals(right);
    public static bool operator !=(BitChartFloatingBarPoint left, BitChartFloatingBarPoint right) => !(left == right);
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
}
