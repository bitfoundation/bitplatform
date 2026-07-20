namespace Bit.BlazorUI.Legacy;

/// <inheritdoc/>
public class BitChartLegacyRadarDataset : BitChartLegacyRadarDataset<double>
{
    /// <inheritdoc/>
    public BitChartLegacyRadarDataset() { }

    /// <inheritdoc/>
    public BitChartLegacyRadarDataset(IEnumerable<double> data) : base(data) { }

    /// <inheritdoc/>
    protected BitChartLegacyRadarDataset(BitChartLegacyChartType type) : base(type) { }
}

/// <summary>
/// Represents a dataset for a radar chart.
/// As per documentation <a href="https://www.chartjs.org/docs/latest/charts/radar.html#dataset-properties">here (Chart.js)</a>.
/// </summary>
// Very similar to LineDataset, so the summaries are inherited.
public class BitChartLegacyRadarDataset<T> : BitChartLegacyDataset<T>
{
    /// <summary>
    /// Creates a new instance of <see cref="BitChartLegacyRadarDataset{T}"/>.
    /// </summary>
    public BitChartLegacyRadarDataset() : base(BitChartLegacyChartType.Radar) { }

    /// <summary>
    /// Creates a new instance of <see cref="BitChartLegacyRadarDataset{T}"/> with initial data.
    /// </summary>
    public BitChartLegacyRadarDataset(IEnumerable<T> data) : this()
    {
        AddRange(data);
    }

    /// <summary>
    /// Creates a new instance of <see cref="BitChartLegacyRadarDataset{T}"/> with
    /// a custom <see cref="BitChartLegacyChartType"/>. Use this constructor when
    /// you implement a radar-like chart.
    /// </summary>
    /// <param name="type">The <see cref="BitChartLegacyChartType"/> to use instead of <see cref="BitChartLegacyChartType.Radar"/>.</param>
    protected BitChartLegacyRadarDataset(BitChartLegacyChartType type) : base(type) { }

    /// <inheritdoc cref="BitChartLegacyLineDataset{T}.BackgroundColor"/>
    public string? BackgroundColor { get; set; }

    /// <inheritdoc cref="BitChartLegacyLineDataset{T}.BorderCapStyle"/>
    public BitChartLegacyBorderCapStyle? BorderCapStyle { get; set; }

    /// <inheritdoc cref="BitChartLegacyLineDataset{T}.BorderColor"/>
    public string? BorderColor { get; set; }

    /// <inheritdoc cref="BitChartLegacyLineDataset{T}.BorderDash"/>
    public int[]? BorderDash { get; set; }

    /// <inheritdoc cref="BitChartLegacyLineDataset{T}.BorderDashOffset"/>
    public int? BorderDashOffset { get; set; }

    /// <inheritdoc cref="BitChartLegacyLineDataset{T}.BorderJoinStyle"/>
    public BitChartLegacyBorderJoinStyle? BorderJoinStyle { get; set; }

    /// <inheritdoc cref="BitChartLegacyLineDataset{T}.BorderWidth"/>
    public int? BorderWidth { get; set; }

    /// <inheritdoc cref="BitChartLegacyLineDataset{T}.Fill"/>
    public BitChartLegacyFillingMode? Fill { get; set; }

    /// <inheritdoc cref="BitChartLegacyLineDataset{T}.HoverBackgroundColor"/>
    public string? HoverBackgroundColor { get; set; }

    /// <inheritdoc cref="BitChartLegacyLineDataset{T}.HoverBorderCapStyle"/>
    public BitChartLegacyBorderCapStyle? HoverBorderCapStyle { get; set; }

    /// <inheritdoc cref="BitChartLegacyLineDataset{T}.HoverBorderColor"/>
    public string? HoverBorderColor { get; set; }

    /// <inheritdoc cref="BitChartLegacyLineDataset{T}.HoverBorderDash"/>
    public int[]? HoverBorderDash { get; set; }

    /// <inheritdoc cref="BitChartLegacyLineDataset{T}.HoverBorderDashOffset"/>
    public int? HoverBorderDashOffset { get; set; }

    /// <inheritdoc cref="BitChartLegacyLineDataset{T}.HoverBorderJoinStyle"/>
    public BitChartLegacyBorderJoinStyle? HoverBorderJoinStyle { get; set; }

    /// <inheritdoc cref="BitChartLegacyLineDataset{T}.HoverBorderWidth"/>
    public int? HoverBorderWidth { get; set; }

    /// <inheritdoc cref="BitChartLegacyLineDataset{T}.Label"/>
    public string? Label { get; set; }

    /// <summary>
    /// Gets or sets the bezier curve tension of the line. Set to 0 to draw straight lines.
    /// </summary>
    public double? LineTension { get; set; }

    /// <inheritdoc cref="BitChartLegacyLineDataset{T}.Order"/>
    public int? Order { get; set; }

    /// <inheritdoc cref="BitChartLegacyLineDataset{T}.PointBackgroundColor"/>
    public BitChartLegacyIndexableOption<string>? PointBackgroundColor { get; set; }

    /// <inheritdoc cref="BitChartLegacyLineDataset{T}.PointBorderColor"/>
    public BitChartLegacyIndexableOption<string>? PointBorderColor { get; set; }

    /// <inheritdoc cref="BitChartLegacyLineDataset{T}.PointBorderWidth"/>
    public BitChartLegacyIndexableOption<int>? PointBorderWidth { get; set; }

    /// <inheritdoc cref="BitChartLegacyLineDataset{T}.PointHitRadius"/>
    public BitChartLegacyIndexableOption<int>? PointHitRadius { get; set; }

    /// <inheritdoc cref="BitChartLegacyLineDataset{T}.PointHoverBackgroundColor"/>
    public BitChartLegacyIndexableOption<string>? PointHoverBackgroundColor { get; set; }

    /// <inheritdoc cref="BitChartLegacyLineDataset{T}.PointHoverBorderColor"/>
    public BitChartLegacyIndexableOption<string>? PointHoverBorderColor { get; set; }

    /// <inheritdoc cref="BitChartLegacyLineDataset{T}.PointHoverBorderWidth"/>
    public BitChartLegacyIndexableOption<int>? PointHoverBorderWidth { get; set; }

    /// <inheritdoc cref="BitChartLegacyLineDataset{T}.PointHoverRadius"/>
    public BitChartLegacyIndexableOption<int>? PointHoverRadius { get; set; }

    /// <inheritdoc cref="BitChartLegacyLineDataset{T}.PointRadius"/>
    public BitChartLegacyIndexableOption<int>? PointRadius { get; set; }

    /// <inheritdoc cref="BitChartLegacyLineDataset{T}.PointRotation"/>
    public BitChartLegacyIndexableOption<double>? PointRotation { get; set; }

    /// <inheritdoc cref="BitChartLegacyLineDataset{T}.PointStyle"/>
    public BitChartLegacyIndexableOption<BitChartLegacyPointStyle>? PointStyle { get; set; }

    /// <inheritdoc cref="BitChartLegacyLineDataset{T}.SpanGaps"/>
    public bool? SpanGaps { get; set; }
}
