namespace Bit.BlazorUI;

/// <summary>
/// Represents the data-subconfig of any <see cref="BitChartConfigBase"/>.
/// </summary>
public class BitChartChartData
{
    /// <summary>
    /// Creates a new instance of <see cref="BitChartChartData"/>.
    /// </summary>
    public BitChartChartData()
    {
        Labels = new List<string>();
        XLabels = new List<string>();
        YLabels = new List<string>();
        Datasets = new List<IBitChartDataset>();
    }

    /// <summary>
    /// Gets the labels the chart will use.
    /// <para>
    /// If defined (1 or more labels) the corresponding axis has to be of type
    /// <see cref="Enums.AxisType.Category"/> for the chart to work correctly.
    /// </para>
    /// </summary>
    public virtual IList<string> Labels { get; }

    /// <summary>
    /// Gets the labels the horizontal axes will use.
    /// <para>
    /// If defined (1 or more labels) the x-axis has to be of type
    /// <see cref="Enums.AxisType.Category"/> for the chart to work correctly.
    /// </para>
    /// </summary>
    public virtual IList<string> XLabels { get; }

    /// <summary>
    /// Gets the labels the vertical axes will use.
    /// <para>
    /// If defined (1 or more labels) the y-axis has to be of type
    /// <see cref="Enums.AxisType.Category"/> for the chart to work correctly.
    /// </para>
    /// </summary>
    public virtual IList<string> YLabels { get; }

    /// <summary>
    /// Gets the datasets displayed in this chart.
    /// </summary>
    public IList<IBitChartDataset> Datasets { get; }

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
    [Obsolete("json.net", true)]
    public bool ShouldSerializeLabels() => Labels?.Count > 0;
    [Obsolete("json.net", true)]
    public bool ShouldSerializeXLabels() => XLabels?.Count > 0;
    [Obsolete("json.net", true)]
    public bool ShouldSerializeYLabels() => YLabels?.Count > 0;
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
}
