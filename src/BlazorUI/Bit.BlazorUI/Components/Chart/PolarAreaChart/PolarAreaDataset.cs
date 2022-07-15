namespace Bit.BlazorUI;

/// <inheritdoc/>
public class PolarAreaDataset : PolarAreaDataset<double>
{
    /// <inheritdoc/>
    public PolarAreaDataset() { }

    /// <inheritdoc/>
    public PolarAreaDataset(IEnumerable<double> data) : base(data) { }

    /// <inheritdoc/>
    protected PolarAreaDataset(ChartType type) : base(type) { }
}

/// <summary>
/// Represents a dataset for a polar area chart.
/// As per documentation <a href="https://www.chartjs.org/docs/latest/charts/polar.html#dataset-properties">here (Chart.js)</a>.
/// </summary>
// Very similar to PieDataset, so the summaries are inherited.
public class PolarAreaDataset<T> : Dataset<T>
{
    /// <summary>
    /// Creates a new instance of <see cref="PolarAreaDataset{T}"/>.
    /// </summary>
    public PolarAreaDataset() : base(ChartType.PolarArea) { }

    /// <summary>
    /// Creates a new instance of <see cref="PolarAreaDataset{T}"/> with initial data.
    /// </summary>
    public PolarAreaDataset(IEnumerable<T> data) : this()
    {
        AddRange(data);
    }

    /// <summary>
    /// Creates a new instance of <see cref="PolarAreaDataset{T}"/> with
    /// a custom <see cref="ChartType"/>. Use this constructor when
    /// you implement a polar-area-like chart.
    /// </summary>
    /// <param name="type">The <see cref="ChartType"/> to use instead of <see cref="ChartType.PolarArea"/>.</param>
    protected PolarAreaDataset(ChartType type) : base(type) { }

    /// <inheritdoc cref="PieDataset{T}.BackgroundColor"/>
    public IndexableOption<string> BackgroundColor { get; set; }

    /// <inheritdoc cref="PieDataset{T}.BorderAlign"/>
    public IndexableOption<BorderAlign> BorderAlign { get; set; }

    /// <inheritdoc cref="PieDataset{T}.BorderColor"/>
    public IndexableOption<string> BorderColor { get; set; }

    /// <inheritdoc cref="PieDataset{T}.BorderWidth"/>
    public IndexableOption<int> BorderWidth { get; set; }

    /// <inheritdoc cref="PieDataset{T}.HoverBackgroundColor"/>
    public IndexableOption<string> HoverBackgroundColor { get; set; }

    /// <inheritdoc cref="PieDataset{T}.HoverBorderColor"/>
    public IndexableOption<string> HoverBorderColor { get; set; }

    /// <inheritdoc cref="PieDataset{T}.HoverBorderWidth"/>
    public IndexableOption<int> HoverBorderWidth { get; set; }
}
