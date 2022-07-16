namespace Bit.BlazorUI;

/// <summary>
/// Represents a dataset for a scatter chart.
/// As per documentation <a href="https://www.chartjs.org/docs/latest/charts/scatter.html#dataset-properties">here (Chart.js)</a>.
/// </summary>
public class BitChartScatterDataset : BitChartLineDataset<BitChartPoint>
{
    /// <summary>
    /// Creates a new instance of <see cref="BitChartScatterDataset"/>.
    /// </summary>
    public BitChartScatterDataset() : base(BitChartChartType.Scatter) { }

    /// <summary>
    /// Creates a new instance of <see cref="BitChartScatterDataset"/> with initial data.
    /// </summary>
    public BitChartScatterDataset(IEnumerable<BitChartPoint> data) : base(BitChartChartType.Scatter)
    {
        AddRange(data);
    }

    /// <summary>
    /// Creates a new instance of <see cref="BitChartScatterDataset"/> with
    /// a custom <see cref="BitChartChartType"/>. Use this constructor when
    /// you implement a scatter-like chart.
    /// </summary>
    /// <param name="type">The <see cref="BitChartChartType"/> to use instead of <see cref="BitChartChartType.Scatter"/>.</param>
    protected BitChartScatterDataset(BitChartChartType type) : base(type) { }
}
