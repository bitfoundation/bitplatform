namespace Bit.BlazorUI.Legacy;

/// <summary>
/// The filling mode is used in area charts (like line and radar) to define how the area around
/// the lines is filled.
/// <para>
/// As per documentation <a href="https://www.chartjs.org/docs/latest/charts/area.html#filling-modes">here (Chart.js)</a>.
/// </para>
/// </summary>
public sealed class BitChartLegacyFillingMode : BitChartLegacyObjectEnum
{
    /// <summary>
    /// Gets a <see cref="BitChartLegacyFillingMode"/> that represents filling the area between the current
    /// dataset and the dataset at the specified relative index.
    /// <para>
    /// Example: Specifying -1 for the third dataset in the chart will cause the area between
    /// the third and the second dataset to be filled.
    /// </para>
    /// </summary>
    /// <param name="relativeDatasetIndex">The relative index of the dataset to fill to.</param>
    public static BitChartLegacyFillingMode Relative(int relativeDatasetIndex)
    {
        if (relativeDatasetIndex == 0)
            throw new ArgumentOutOfRangeException(nameof(relativeDatasetIndex));

        if (relativeDatasetIndex < 0)
        {
            return new BitChartLegacyFillingMode(relativeDatasetIndex.ToString());
        }
        else
        {
            return new BitChartLegacyFillingMode($"+{relativeDatasetIndex}");
        }
    }

    /// <summary>
    /// Gets a <see cref="BitChartLegacyFillingMode"/> that represents filling the area between the current
    /// dataset and the dataset at the specified (zero-based) index.
    /// <para>
    /// Example: Specifying 1 for the third dataset in the chart will cause the area between
    /// the third and the second dataset to be filled.
    /// </para>
    /// </summary>
    /// <param name="absoluteDatasetIndex">The absolute (zero-based) index of the dataset to fill to.</param>
    public static BitChartLegacyFillingMode Absolute(int absoluteDatasetIndex)
    {
        if (absoluteDatasetIndex < 0)
            throw new ArgumentOutOfRangeException(nameof(absoluteDatasetIndex));

        return new BitChartLegacyFillingMode(absoluteDatasetIndex);
    }

    /// <summary>
    /// Gets a <see cref="BitChartLegacyFillingMode"/> that represents no filling.
    /// </summary>
    public static BitChartLegacyFillingMode Disabled => new BitChartLegacyFillingMode(false);

    /// <summary>
    /// Gets a <see cref="BitChartLegacyFillingMode"/> that represents filling the area between
    /// the current dataset and the origin.
    /// </summary>
    public static BitChartLegacyFillingMode Origin => new BitChartLegacyFillingMode("origin");

    /// <summary>
    /// Gets a <see cref="BitChartLegacyFillingMode"/> that represents filling the area between
    /// the current dataset and the start.
    /// </summary>
    public static BitChartLegacyFillingMode Start => new BitChartLegacyFillingMode("start");

    /// <summary>
    /// Gets a <see cref="BitChartLegacyFillingMode"/> that represents filling the area between
    /// the current dataset and the end.
    /// </summary>
    public static BitChartLegacyFillingMode End => new BitChartLegacyFillingMode("end");

    /// <summary>
    /// Converts a <see cref="bool"/> value to a <see cref="BitChartLegacyFillingMode"/> value.
    /// <see langword="false"/> is equal to <see cref="Disabled"/> and <see langword="true"/>
    /// is equal to <see cref="Origin"/>.
    /// </summary>
    /// <param name="filled">A value indicating whether or not to fill the area.</param>
    public static implicit operator BitChartLegacyFillingMode(bool filled) => new BitChartLegacyFillingMode(filled);

    private BitChartLegacyFillingMode(int value) : base(value) { }
    private BitChartLegacyFillingMode(string value) : base(value) { }
    private BitChartLegacyFillingMode(bool value) : base(value) { }
}
