namespace Bit.BlazorUI;

/// <inheritdoc/>
public class BitChartPolarAreaDataset : BitChartPolarAreaDataset<double>
{
    /// <inheritdoc/>
    public BitChartPolarAreaDataset() { }

    /// <inheritdoc/>
    public BitChartPolarAreaDataset(IEnumerable<double> data) : base(data) { }

    /// <inheritdoc/>
    protected BitChartPolarAreaDataset(BitChartChartType type) : base(type) { }
}

/// <summary>
/// Represents a dataset for a polar area chart.
/// As per documentation <a href="https://www.chartjs.org/docs/latest/charts/polar.html#dataset-properties">here (Chart.js)</a>.
/// </summary>
// Very similar to PieDataset, so the summaries are inherited.
public class BitChartPolarAreaDataset<T> : BitChartDataset<T>
{
    /// <summary>
    /// Creates a new instance of <see cref="BitChartPolarAreaDataset{T}"/>.
    /// </summary>
    public BitChartPolarAreaDataset() : base(BitChartChartType.PolarArea) { }

    /// <summary>
    /// Creates a new instance of <see cref="BitChartPolarAreaDataset{T}"/> with initial data.
    /// </summary>
    public BitChartPolarAreaDataset(IEnumerable<T> data) : this()
    {
        AddRange(data);
    }

    /// <summary>
    /// Creates a new instance of <see cref="BitChartPolarAreaDataset{T}"/> with
    /// a custom <see cref="BitChartChartType"/>. Use this constructor when
    /// you implement a polar-area-like chart.
    /// </summary>
    /// <param name="type">The <see cref="BitChartChartType"/> to use instead of <see cref="BitChartChartType.PolarArea"/>.</param>
    protected BitChartPolarAreaDataset(BitChartChartType type) : base(type) { }

    /// <inheritdoc cref="BitChartPieDataset{T}.BackgroundColor"/>
    public BitChartIndexableOption<string> BackgroundColor { get; set; }

    /// <inheritdoc cref="BitChartPieDataset{T}.BorderAlign"/>
    public BitChartIndexableOption<BitChartBorderAlign> BorderAlign { get; set; }

    /// <inheritdoc cref="BitChartPieDataset{T}.BorderColor"/>
    public BitChartIndexableOption<string> BorderColor { get; set; }

    /// <inheritdoc cref="BitChartPieDataset{T}.BorderWidth"/>
    public BitChartIndexableOption<int> BorderWidth { get; set; }

    /// <inheritdoc cref="BitChartPieDataset{T}.HoverBackgroundColor"/>
    public BitChartIndexableOption<string> HoverBackgroundColor { get; set; }

    /// <inheritdoc cref="BitChartPieDataset{T}.HoverBorderColor"/>
    public BitChartIndexableOption<string> HoverBorderColor { get; set; }

    /// <inheritdoc cref="BitChartPieDataset{T}.HoverBorderWidth"/>
    public BitChartIndexableOption<int> HoverBorderWidth { get; set; }
}
