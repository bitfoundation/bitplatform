namespace Bit.BlazorUI;

/// <summary>
/// The base class for all tick mark configurations of cartesian axes (see <see cref="BitChartCartesianAxis"/>). Ticks-subconfig of <see cref="BitChartCartesianAxis"/>.
/// <para>As per documentation <a href="https://www.chartjs.org/docs/latest/axes/cartesian/#tick-configuration">here (Chart.js)</a>.</para>
/// </summary>
public abstract class BitChartCartesianTicks : BitChartTicks
{
    /// <summary>
    /// Gets or sets the user defined minimum number for the scale, overrides minimum value from data.
    /// </summary>
    public double? Min { get; set; }

    /// <summary>
    /// Gets or sets the user defined maximum number for the scale, overrides maximum value from data.
    /// </summary>
    public double? Max { get; set; }

    /// <summary>
    /// The number of ticks to examine when deciding how many labels will fit.
    /// Setting a smaller value will be faster, but may be less accurate when there is large variability in label length.
    /// </summary>
    public int? SampleSize { get; set; }

    /// <summary>
    /// If true, automatically calculates how many labels can be shown and hides labels accordingly.
    /// Labels will be rotated up to maxRotation before skipping any. Turn <see cref="AutoSkip" /> off to show all labels no matter what.
    /// </summary>
    public bool? AutoSkip { get; set; }

    /// <summary>
    /// Gets or sets the padding between the ticks on the horizontal axis when <see cref="AutoSkip" /> is enabled.
    /// </summary>
    public int? AutoSkipPadding { get; set; }

    /// <summary>
    /// Gets or sets the distance in pixels to offset the label from the centre point of the tick (in the x direction for the x axis, and the y direction for the y axis).
    /// <para>Note: this can cause labels at the edges to be cropped by the edge of the canvas.</para>
    /// </summary>
    public int? LabelOffset { get; set; }

    /// <summary>
    /// Gets or sets the maximum rotation for tick labels when rotating to condense labels.
    /// <para>Note: Rotation doesn't occur until necessary.</para>
    /// <para>Note: Only applicable to horizontal scales.</para>
    /// </summary>
    public int? MaxRotation { get; set; }

    /// <summary>
    /// Gets or sets the minimum rotation for tick labels.
    /// <para>Note: Only applicable to horizontal scales.</para>
    /// </summary>
    public int? MinRotation { get; set; }

    /// <summary>
    /// If true, flips tick labels around axis, displaying the labels inside the chart instead of outside.
    /// <para>Note: Only applicable to vertical scales.</para>
    /// </summary>
    public bool? Mirror { get; set; }
}
