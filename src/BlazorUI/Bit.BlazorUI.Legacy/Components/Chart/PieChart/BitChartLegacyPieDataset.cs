namespace Bit.BlazorUI.Legacy;

/// <inheritdoc/>
public class BitChartLegacyPieDataset : BitChartLegacyPieDataset<double>
{
    /// <inheritdoc/>
    public BitChartLegacyPieDataset(bool useDoughnutDefaults = false) : base(useDoughnutDefaults) { }

    /// <inheritdoc/>
    public BitChartLegacyPieDataset(IEnumerable<double> data, bool useDoughnutDefaults = false) : base(data, useDoughnutDefaults) { }

    /// <inheritdoc/>
    protected BitChartLegacyPieDataset(BitChartLegacyChartType type) : base(type) { }
}

/// <summary>
/// Represents a dataset for a pie or doughnut chart.
/// As per documentation <a href="https://www.chartjs.org/docs/latest/charts/doughnut.html#dataset-properties">here (Chart.js)</a>.
/// </summary>
public class BitChartLegacyPieDataset<T> : BitChartLegacyDataset<T>
{
    /// <summary>
    /// Creates a new instance of <see cref="BitChartLegacyPieDataset{T}"/>.
    /// </summary>
    /// <param name="useDoughnutDefaults">
    /// If <see langword="true"/>, the dataset-type will be set to <see cref="BitChartLegacyChartType.Doughnut"/>
    /// which causes Chart.js to use the doughnut defaults.
    /// If <see langword="false"/>, the dataset-type will be set to <see cref="BitChartLegacyChartType.Pie"/>.
    /// Unless the defaults were changed manually,
    /// the pie defaults are identical to the doughnut defaults and then this setting doesn't matter.
    /// </param>
    public BitChartLegacyPieDataset(bool useDoughnutDefaults = false) : base(useDoughnutDefaults ? BitChartLegacyChartType.Doughnut : BitChartLegacyChartType.Pie) { }

    /// <summary>
    /// Creates a new instance of <see cref="BitChartLegacyPieDataset{T}"/> with initial data.
    /// </summary>
    /// <inheritdoc cref="BitChartLegacyPieDataset(bool)"/>
    public BitChartLegacyPieDataset(IEnumerable<T> data, bool useDoughnutDefaults = false) : this(useDoughnutDefaults)
    {
        AddRange(data);
    }

    /// <summary>
    /// Creates a new instance of <see cref="BitChartLegacyPieDataset{T}"/> with
    /// a custom <see cref="BitChartLegacyChartType"/>. Use this constructor when
    /// you implement a pie-like chart.
    /// </summary>
    /// <param name="type">The <see cref="BitChartLegacyChartType"/> to use instead of <see cref="BitChartLegacyChartType.Pie"/>.</param>
    protected BitChartLegacyPieDataset(BitChartLegacyChartType type) : base(type) { }

    /// <summary>
    /// Gets or sets the background color of the arcs.
    /// This property should usually be indexed, otherwise it's hard to distinguish the individual arcs.
    /// <para>See <see cref="BitChartLegacyColorUtil"/> for working with colors.</para>
    /// </summary>
    public BitChartLegacyIndexableOption<string>? BackgroundColor { get; set; }

    /// <summary>
    /// Gets or sets the border alignment. When <see cref="BitChartLegacyBorderAlign.Center" /> is set,
    /// the borders of arcs next to each other will overlap. When <see cref="BitChartLegacyBorderAlign.Inner" />
    /// is set, it is guaranteed that all the borders will not overlap.
    /// </summary>
    public BitChartLegacyIndexableOption<BitChartLegacyBorderAlign>? BorderAlign { get; set; }

    /// <summary>
    /// Gets or sets the border color of the arcs.
    /// <para>See <see cref="BitChartLegacyColorUtil"/> for working with colors.</para>
    /// </summary>
    public BitChartLegacyIndexableOption<string>? BorderColor { get; set; }

    /// <summary>
    /// Gets or sets the border width of the arcs (in pixels).
    /// </summary>
    public BitChartLegacyIndexableOption<int>? BorderWidth { get; set; }

    /// <summary>
    /// Gets or sets the background color of the arcs when hovered.
    /// <para>See <see cref="BitChartLegacyColorUtil"/> for working with colors.</para>
    /// </summary>
    public BitChartLegacyIndexableOption<string>? HoverBackgroundColor { get; set; }

    /// <summary>
    /// Gets or sets the border color of the arcs when hovered.
    /// <para>See <see cref="BitChartLegacyColorUtil"/> for working with colors.</para>
    /// </summary>
    public BitChartLegacyIndexableOption<string>? HoverBorderColor { get; set; }

    /// <summary>
    /// Gets or sets the border width of the arcs when hovered (in pixels).
    /// </summary>
    public BitChartLegacyIndexableOption<int>? HoverBorderWidth { get; set; }

    /// <summary>
    /// Gets or sets the relative thickness of the dataset.
    /// Providing a value for <see cref="Weight"/> will cause the pie or doughnut dataset to be
    /// drawn with a thickness relative to the sum of all the dataset weight values.
    /// </summary>
    public int? Weight { get; set; }
}
