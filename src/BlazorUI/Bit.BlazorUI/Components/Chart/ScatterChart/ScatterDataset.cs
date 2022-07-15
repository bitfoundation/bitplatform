namespace Bit.BlazorUI;

/// <summary>
/// Represents a dataset for a scatter chart.
/// As per documentation <a href="https://www.chartjs.org/docs/latest/charts/scatter.html#dataset-properties">here (Chart.js)</a>.
/// </summary>
public class ScatterDataset : LineDataset<Point>
{
    /// <summary>
    /// Creates a new instance of <see cref="ScatterDataset"/>.
    /// </summary>
    public ScatterDataset() : base(ChartType.Scatter) { }

    /// <summary>
    /// Creates a new instance of <see cref="ScatterDataset"/> with initial data.
    /// </summary>
    public ScatterDataset(IEnumerable<Point> data) : base(ChartType.Scatter)
    {
        AddRange(data);
    }

    /// <summary>
    /// Creates a new instance of <see cref="ScatterDataset"/> with
    /// a custom <see cref="ChartType"/>. Use this constructor when
    /// you implement a scatter-like chart.
    /// </summary>
    /// <param name="type">The <see cref="ChartType"/> to use instead of <see cref="ChartType.Scatter"/>.</param>
    protected ScatterDataset(ChartType type) : base(type) { }
}
