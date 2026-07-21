using Newtonsoft.Json;

namespace Bit.BlazorUI.Legacy;

/// <summary>
/// Represents a floating bar within a bar chart (use in <see cref="BitChartLegacyBarDataset{T}"/>).
/// Using this type, the bars will be rendered with gaps between them (floating-bars).
/// <para>
/// When serialized, this value is represented as an array of two numbers.
/// <see cref="Start"/> will be the first number in the array, <see cref="End"/> the second one.
/// </para>
/// </summary>
[JsonConverter(typeof(FloatingBarPointConverter))]
public readonly struct BitChartLegacyFloatingBarPoint : IEquatable<BitChartLegacyFloatingBarPoint>
{
    /// <summary>
    /// Gets the start-value of this <see cref="BitChartLegacyFloatingBarPoint"/>.
    /// When serialized, this will be the first value in the array.
    /// </summary>
    public double Start { get; }

    /// <summary>
    /// Gets the end-value of this <see cref="BitChartLegacyFloatingBarPoint"/>.
    /// When serialized, this will be the second value in the array.
    /// </summary>
    public double End { get; }

    /// <summary>
    /// Creates a new instance of <see cref="BitChartLegacyFloatingBarPoint"/>.
    /// </summary>
    /// <param name="start">The start-value for this <see cref="BitChartLegacyFloatingBarPoint"/>.</param>
    /// <param name="end">The end-value for this <see cref="BitChartLegacyFloatingBarPoint"/>.</param>
    public BitChartLegacyFloatingBarPoint(double start, double end)
    {
        Start = start;
        End = end;
    }

    public override bool Equals(object? obj) => obj is BitChartLegacyFloatingBarPoint point && Equals(point);
    public bool Equals(BitChartLegacyFloatingBarPoint other) => Start == other.Start && End == other.End;
    public override int GetHashCode() => HashCode.Combine(Start, End);

    public static bool operator ==(BitChartLegacyFloatingBarPoint left, BitChartLegacyFloatingBarPoint right) => left.Equals(right);
    public static bool operator !=(BitChartLegacyFloatingBarPoint left, BitChartLegacyFloatingBarPoint right) => !(left == right);
}
