namespace Bit.BlazorUI.Legacy;

/// <summary>
/// Represents a dataset for a scatter chart.
/// As per documentation <a href="https://www.chartjs.org/docs/latest/charts/scatter.html#dataset-properties">here (Chart.js)</a>.
/// </summary>
public class BitChartLegacyScatterDataset : BitChartLegacyLineDataset<BitChartLegacyPoint>
{
    /// <summary>
    /// Creates a new instance of <see cref="BitChartLegacyScatterDataset"/>.
    /// </summary>
    public BitChartLegacyScatterDataset() : base(BitChartLegacyChartType.Scatter) { }

    /// <summary>
    /// Creates a new instance of <see cref="BitChartLegacyScatterDataset"/> with initial data.
    /// </summary>
    public BitChartLegacyScatterDataset(IEnumerable<BitChartLegacyPoint> data) : base(BitChartLegacyChartType.Scatter)
    {
        AddRange(data);
    }

    /// <summary>
    /// Creates a new instance of <see cref="BitChartLegacyScatterDataset"/> with
    /// a custom <see cref="BitChartLegacyChartType"/>. Use this constructor when
    /// you implement a scatter-like chart.
    /// </summary>
    /// <param name="type">The <see cref="BitChartLegacyChartType"/> to use instead of <see cref="BitChartLegacyChartType.Scatter"/>.</param>
    protected BitChartLegacyScatterDataset(BitChartLegacyChartType type) : base(type) { }
}
