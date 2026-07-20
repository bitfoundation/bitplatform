namespace Bit.BlazorUI.Legacy;

/// <inheritdoc/>
public class BitChartLegacyPolarAreaDataset : BitChartLegacyPolarAreaDataset<double>
{
    /// <inheritdoc/>
    public BitChartLegacyPolarAreaDataset() { }

    /// <inheritdoc/>
    public BitChartLegacyPolarAreaDataset(IEnumerable<double> data) : base(data) { }

    /// <inheritdoc/>
    protected BitChartLegacyPolarAreaDataset(BitChartLegacyChartType type) : base(type) { }
}

/// <summary>
/// Represents a dataset for a polar area chart.
/// As per documentation <a href="https://www.chartjs.org/docs/latest/charts/polar.html#dataset-properties">here (Chart.js)</a>.
/// </summary>
// Very similar to PieDataset, so the summaries are inherited.
public class BitChartLegacyPolarAreaDataset<T> : BitChartLegacyDataset<T>
{
    /// <summary>
    /// Creates a new instance of <see cref="BitChartLegacyPolarAreaDataset{T}"/>.
    /// </summary>
    public BitChartLegacyPolarAreaDataset() : base(BitChartLegacyChartType.PolarArea) { }

    /// <summary>
    /// Creates a new instance of <see cref="BitChartLegacyPolarAreaDataset{T}"/> with initial data.
    /// </summary>
    public BitChartLegacyPolarAreaDataset(IEnumerable<T> data) : this()
    {
        AddRange(data);
    }

    /// <summary>
    /// Creates a new instance of <see cref="BitChartLegacyPolarAreaDataset{T}"/> with
    /// a custom <see cref="BitChartLegacyChartType"/>. Use this constructor when
    /// you implement a polar-area-like chart.
    /// </summary>
    /// <param name="type">The <see cref="BitChartLegacyChartType"/> to use instead of <see cref="BitChartLegacyChartType.PolarArea"/>.</param>
    protected BitChartLegacyPolarAreaDataset(BitChartLegacyChartType type) : base(type) { }

    /// <inheritdoc cref="BitChartLegacyPieDataset{T}.BackgroundColor"/>
    public BitChartLegacyIndexableOption<string>? BackgroundColor { get; set; }

    /// <inheritdoc cref="BitChartLegacyPieDataset{T}.BorderAlign"/>
    public BitChartLegacyIndexableOption<BitChartLegacyBorderAlign>? BorderAlign { get; set; }

    /// <inheritdoc cref="BitChartLegacyPieDataset{T}.BorderColor"/>
    public BitChartLegacyIndexableOption<string>? BorderColor { get; set; }

    /// <inheritdoc cref="BitChartLegacyPieDataset{T}.BorderWidth"/>
    public BitChartLegacyIndexableOption<int>? BorderWidth { get; set; }

    /// <inheritdoc cref="BitChartLegacyPieDataset{T}.HoverBackgroundColor"/>
    public BitChartLegacyIndexableOption<string>? HoverBackgroundColor { get; set; }

    /// <inheritdoc cref="BitChartLegacyPieDataset{T}.HoverBorderColor"/>
    public BitChartLegacyIndexableOption<string>? HoverBorderColor { get; set; }

    /// <inheritdoc cref="BitChartLegacyPieDataset{T}.HoverBorderWidth"/>
    public BitChartLegacyIndexableOption<int>? HoverBorderWidth { get; set; }
}
