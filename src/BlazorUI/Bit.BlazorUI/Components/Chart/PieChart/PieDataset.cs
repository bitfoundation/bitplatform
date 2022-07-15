namespace Bit.BlazorUI;

/// <inheritdoc/>
public class PieDataset : PieDataset<double>
{
    /// <inheritdoc/>
    public PieDataset(bool useDoughnutDefaults = false) : base(useDoughnutDefaults) { }

    /// <inheritdoc/>
    public PieDataset(IEnumerable<double> data, bool useDoughnutDefaults = false) : base(data, useDoughnutDefaults) { }

    /// <inheritdoc/>
    protected PieDataset(ChartType type) : base(type) { }
}

/// <summary>
/// Represents a dataset for a pie or doughnut chart.
/// As per documentation <a href="https://www.chartjs.org/docs/latest/charts/doughnut.html#dataset-properties">here (Chart.js)</a>.
/// </summary>
public class PieDataset<T> : Dataset<T>
{
    /// <summary>
    /// Creates a new instance of <see cref="PieDataset{T}"/>.
    /// </summary>
    /// <param name="useDoughnutDefaults">
    /// If <see langword="true"/>, the dataset-type will be set to <see cref="ChartType.Doughnut"/>
    /// which causes Chart.js to use the doughnut defaults.
    /// If <see langword="false"/>, the dataset-type will be set to <see cref="ChartType.Pie"/>.
    /// Unless the defaults were changed manually,
    /// the pie defaults are identical to the doughnut defaults and then this setting doesn't matter.
    /// </param>
    public PieDataset(bool useDoughnutDefaults = false) : base(useDoughnutDefaults ? ChartType.Doughnut : ChartType.Pie) { }

    /// <summary>
    /// Creates a new instance of <see cref="PieDataset{T}"/> with initial data.
    /// </summary>
    /// <inheritdoc cref="PieDataset(bool)"/>
    public PieDataset(IEnumerable<T> data, bool useDoughnutDefaults = false) : this(useDoughnutDefaults)
    {
        AddRange(data);
    }

    /// <summary>
    /// Creates a new instance of <see cref="PieDataset{T}"/> with
    /// a custom <see cref="ChartType"/>. Use this constructor when
    /// you implement a pie-like chart.
    /// </summary>
    /// <param name="type">The <see cref="ChartType"/> to use instead of <see cref="ChartType.Pie"/>.</param>
    protected PieDataset(ChartType type) : base(type) { }

    /// <summary>
    /// Gets or sets the background color of the arcs.
    /// This property should usually be indexed, otherwise it's hard to distinguish the individual arcs.
    /// <para>See <see cref="ColorUtil"/> for working with colors.</para>
    /// </summary>
    public IndexableOption<string> BackgroundColor { get; set; }

    /// <summary>
    /// Gets or sets the border alignment. When <see cref="BorderAlign.Center" /> is set,
    /// the borders of arcs next to each other will overlap. When <see cref="BorderAlign.Inner" />
    /// is set, it is guaranteed that all the borders will not overlap.
    /// </summary>
    public IndexableOption<BorderAlign> BorderAlign { get; set; }

    /// <summary>
    /// Gets or sets the border color of the arcs.
    /// <para>See <see cref="ColorUtil"/> for working with colors.</para>
    /// </summary>
    public IndexableOption<string> BorderColor { get; set; }

    /// <summary>
    /// Gets or sets the border width of the arcs (in pixels).
    /// </summary>
    public IndexableOption<int> BorderWidth { get; set; }

    /// <summary>
    /// Gets or sets the background color of the arcs when hovered.
    /// <para>See <see cref="ColorUtil"/> for working with colors.</para>
    /// </summary>
    public IndexableOption<string> HoverBackgroundColor { get; set; }

    /// <summary>
    /// Gets or sets the border color of the arcs when hovered.
    /// <para>See <see cref="ColorUtil"/> for working with colors.</para>
    /// </summary>
    public IndexableOption<string> HoverBorderColor { get; set; }

    /// <summary>
    /// Gets or sets the border width of the arcs when hovered (in pixels).
    /// </summary>
    public IndexableOption<int> HoverBorderWidth { get; set; }

    /// <summary>
    /// Gets or sets the relative thickness of the dataset.
    /// Providing a value for <see cref="Weight"/> will cause the pie or doughnut dataset to be
    /// drawn with a thickness relative to the sum of all the dataset weight values.
    /// </summary>
    public int? Weight { get; set; }
}
