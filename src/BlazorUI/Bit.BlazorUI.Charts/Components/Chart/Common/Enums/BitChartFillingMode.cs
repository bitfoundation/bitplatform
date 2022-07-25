namespace Bit.BlazorUI;

/// <summary>
/// The filling mode is used in area charts (like line and radar) to define how the area around
/// the lines is filled.
/// <para>
/// As per documentation <a href="https://www.chartjs.org/docs/latest/charts/area.html#filling-modes">here (Chart.js)</a>.
/// </para>
/// </summary>
public sealed class BitChartFillingMode : BitChartObjectEnum
{
    /// <summary>
    /// Gets a <see cref="BitChartFillingMode"/> that represents filling the area between the current
    /// dataset and the dataset at the specified relative index.
    /// <para>
    /// Example: Specifying -1 for the third dataset in the chart will cause the area between
    /// the third and the second dataset to be filled.
    /// </para>
    /// </summary>
    /// <param name="relativeDatasetIndex">The relative index of the dataset to fill to.</param>
    public static BitChartFillingMode Relative(int relativeDatasetIndex)
    {
        if (relativeDatasetIndex == 0)
            throw new ArgumentOutOfRangeException(nameof(relativeDatasetIndex));

        if (relativeDatasetIndex < 0)
        {
            return new BitChartFillingMode(relativeDatasetIndex.ToString());
        }
        else
        {
            return new BitChartFillingMode($"+{relativeDatasetIndex}");
        }
    }

    /// <summary>
    /// Gets a <see cref="BitChartFillingMode"/> that represents filling the area between the current
    /// dataset and the dataset at the specified (zero-based) index.
    /// <para>
    /// Example: Specifying 1 for the third dataset in the chart will cause the area between
    /// the third and the second dataset to be filled.
    /// </para>
    /// </summary>
    /// <param name="absoluteDatasetIndex">The absolute (zero-based) index of the dataset to fill to.</param>
    public static BitChartFillingMode Absolute(int absoluteDatasetIndex)
    {
        if (absoluteDatasetIndex < 0)
            throw new ArgumentOutOfRangeException(nameof(absoluteDatasetIndex));

        return new BitChartFillingMode(absoluteDatasetIndex);
    }

    /// <summary>
    /// Gets a <see cref="BitChartFillingMode"/> that represents no filling.
    /// </summary>
    public static BitChartFillingMode Disabled => new BitChartFillingMode(false);

    /// <summary>
    /// Gets a <see cref="BitChartFillingMode"/> that represents filling the area between
    /// the current dataset and the origin.
    /// </summary>
    public static BitChartFillingMode Origin => new BitChartFillingMode("origin");

    /// <summary>
    /// Gets a <see cref="BitChartFillingMode"/> that represents filling the area between
    /// the current dataset and the start.
    /// </summary>
    public static BitChartFillingMode Start => new BitChartFillingMode("start");

    /// <summary>
    /// Gets a <see cref="BitChartFillingMode"/> that represents filling the area between
    /// the current dataset and the end.
    /// </summary>
    public static BitChartFillingMode End => new BitChartFillingMode("end");

    /// <summary>
    /// Converts a <see cref="bool"/> value to a <see cref="BitChartFillingMode"/> value.
    /// <see langword="false"/> is equal to <see cref="Disabled"/> and <see langword="true"/>
    /// is equal to <see cref="Origin"/>.
    /// </summary>
    /// <param name="filled">A value indicating whether or not to fill the area.</param>
    public static implicit operator BitChartFillingMode(bool filled) => new BitChartFillingMode(filled);

    private BitChartFillingMode(int value) : base(value) { }
    private BitChartFillingMode(string value) : base(value) { }
    private BitChartFillingMode(bool value) : base(value) { }
}
