namespace Bit.BlazorUI;

/// <summary>
/// The filling mode is used in area charts (like line and radar) to define how the area around
/// the lines is filled.
/// <para>
/// As per documentation <a href="https://www.chartjs.org/docs/latest/charts/area.html#filling-modes">here (Chart.js)</a>.
/// </para>
/// </summary>
public sealed class FillingMode : ObjectEnum
{
    /// <summary>
    /// Gets a <see cref="FillingMode"/> that represents filling the area between the current
    /// dataset and the dataset at the specified relative index.
    /// <para>
    /// Example: Specifying -1 for the third dataset in the chart will cause the area between
    /// the third and the second dataset to be filled.
    /// </para>
    /// </summary>
    /// <param name="relativeDatasetIndex">The relative index of the dataset to fill to.</param>
    public static FillingMode Relative(int relativeDatasetIndex)
    {
        if (relativeDatasetIndex == 0)
            throw new ArgumentOutOfRangeException(nameof(relativeDatasetIndex));

        if (relativeDatasetIndex < 0)
        {
            return new FillingMode(relativeDatasetIndex.ToString());
        }
        else
        {
            return new FillingMode($"+{relativeDatasetIndex}");
        }
    }

    /// <summary>
    /// Gets a <see cref="FillingMode"/> that represents filling the area between the current
    /// dataset and the dataset at the specified (zero-based) index.
    /// <para>
    /// Example: Specifying 1 for the third dataset in the chart will cause the area between
    /// the third and the second dataset to be filled.
    /// </para>
    /// </summary>
    /// <param name="absoluteDatasetIndex">The absolute (zero-based) index of the dataset to fill to.</param>
    public static FillingMode Absolute(int absoluteDatasetIndex)
    {
        if (absoluteDatasetIndex < 0)
            throw new ArgumentOutOfRangeException(nameof(absoluteDatasetIndex));

        return new FillingMode(absoluteDatasetIndex);
    }

    /// <summary>
    /// Gets a <see cref="FillingMode"/> that represents no filling.
    /// </summary>
    public static FillingMode Disabled => new FillingMode(false);

    /// <summary>
    /// Gets a <see cref="FillingMode"/> that represents filling the area between
    /// the current dataset and the origin.
    /// </summary>
    public static FillingMode Origin => new FillingMode("origin");

    /// <summary>
    /// Gets a <see cref="FillingMode"/> that represents filling the area between
    /// the current dataset and the start.
    /// </summary>
    public static FillingMode Start => new FillingMode("start");

    /// <summary>
    /// Gets a <see cref="FillingMode"/> that represents filling the area between
    /// the current dataset and the end.
    /// </summary>
    public static FillingMode End => new FillingMode("end");

    /// <summary>
    /// Converts a <see cref="bool"/> value to a <see cref="FillingMode"/> value.
    /// <see langword="false"/> is equal to <see cref="Disabled"/> and <see langword="true"/>
    /// is equal to <see cref="Origin"/>.
    /// </summary>
    /// <param name="filled">A value indicating whether or not to fill the area.</param>
    public static implicit operator FillingMode(bool filled) => new FillingMode(filled);

    private FillingMode(int value) : base(value) { }
    private FillingMode(string value) : base(value) { }
    private FillingMode(bool value) : base(value) { }
}
