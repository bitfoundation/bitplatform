namespace Bit.BlazorUI.Legacy;

/// <summary>
/// Represents the data-subconfig of any <see cref="BitChartLegacyConfigBase"/>.
/// </summary>
public class BitChartLegacyChartData
{
    /// <summary>
    /// Creates a new instance of <see cref="BitChartLegacyChartData"/>.
    /// </summary>
    public BitChartLegacyChartData()
    {
        Labels = [];
        XLabels = [];
        YLabels = [];
        Datasets = [];
    }

    /// <summary>
    /// Gets the labels the chart will use.
    /// <para>
    /// If defined (1 or more labels) the corresponding axis has to be of type
    /// <see cref="BitChartLegacyAxisType.Category"/> for the chart to work correctly.
    /// </para>
    /// </summary>
    public virtual List<string> Labels { get; }

    /// <summary>
    /// Gets the labels the horizontal axes will use.
    /// <para>
    /// If defined (1 or more labels) the x-axis has to be of type
    /// <see cref="BitChartLegacyAxisType.Category"/> for the chart to work correctly.
    /// </para>
    /// </summary>
    public virtual List<string> XLabels { get; }

    /// <summary>
    /// Gets the labels the vertical axes will use.
    /// <para>
    /// If defined (1 or more labels) the y-axis has to be of type
    /// <see cref="BitChartLegacyAxisType.Category"/> for the chart to work correctly.
    /// </para>
    /// </summary>
    public virtual List<string> YLabels { get; }

    /// <summary>
    /// Gets the datasets displayed in this chart.
    /// </summary>
    public List<IBitChartLegacyDataset> Datasets { get; }

    [Obsolete("json.net", true)]
    public bool ShouldSerializeLabels() => Labels?.Count > 0;
    [Obsolete("json.net", true)]
    public bool ShouldSerializeXLabels() => XLabels?.Count > 0;
    [Obsolete("json.net", true)]
    public bool ShouldSerializeYLabels() => YLabels?.Count > 0;
}
