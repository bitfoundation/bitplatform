namespace Bit.BlazorUI;

/// <inheritdoc/>
public class BitChartRadarDataset : BitChartRadarDataset<double>
{
    /// <inheritdoc/>
    public BitChartRadarDataset() { }

    /// <inheritdoc/>
    public BitChartRadarDataset(IEnumerable<double> data) : base(data) { }

    /// <inheritdoc/>
    protected BitChartRadarDataset(BitChartChartType type) : base(type) { }
}

/// <summary>
/// Represents a dataset for a radar chart.
/// As per documentation <a href="https://www.chartjs.org/docs/latest/charts/radar.html#dataset-properties">here (Chart.js)</a>.
/// </summary>
// Very similar to LineDataset, so the summaries are inherited.
public class BitChartRadarDataset<T> : BitChartDataset<T>
{
    /// <summary>
    /// Creates a new instance of <see cref="BitChartRadarDataset{T}"/>.
    /// </summary>
    public BitChartRadarDataset() : base(BitChartChartType.Radar) { }

    /// <summary>
    /// Creates a new instance of <see cref="BitChartRadarDataset{T}"/> with initial data.
    /// </summary>
    public BitChartRadarDataset(IEnumerable<T> data) : this()
    {
        AddRange(data);
    }

    /// <summary>
    /// Creates a new instance of <see cref="BitChartRadarDataset{T}"/> with
    /// a custom <see cref="BitChartChartType"/>. Use this constructor when
    /// you implement a radar-like chart.
    /// </summary>
    /// <param name="type">The <see cref="BitChartChartType"/> to use instead of <see cref="BitChartChartType.Radar"/>.</param>
    protected BitChartRadarDataset(BitChartChartType type) : base(type) { }

    /// <inheritdoc cref="BitChartLineDataset{T}.BackgroundColor"/>
    public string BackgroundColor { get; set; }

    /// <inheritdoc cref="BitChartLineDataset{T}.BorderCapStyle"/>
    public BitChartBorderCapStyle BorderCapStyle { get; set; }

    /// <inheritdoc cref="BitChartLineDataset{T}.BorderColor"/>
    public string BorderColor { get; set; }

    /// <inheritdoc cref="BitChartLineDataset{T}.BorderDash"/>
    public int[] BorderDash { get; set; }

    /// <inheritdoc cref="BitChartLineDataset{T}.BorderDashOffset"/>
    public int? BorderDashOffset { get; set; }

    /// <inheritdoc cref="BitChartLineDataset{T}.BorderJoinStyle"/>
    public BitChartBorderJoinStyle BorderJoinStyle { get; set; }

    /// <inheritdoc cref="BitChartLineDataset{T}.BorderWidth"/>
    public int? BorderWidth { get; set; }

    /// <inheritdoc cref="BitChartLineDataset{T}.Fill"/>
    public BitChartFillingMode Fill { get; set; }

    /// <inheritdoc cref="BitChartLineDataset{T}.HoverBackgroundColor"/>
    public string HoverBackgroundColor { get; set; }

    /// <inheritdoc cref="BitChartLineDataset{T}.HoverBorderCapStyle"/>
    public BitChartBorderCapStyle HoverBorderCapStyle { get; set; }

    /// <inheritdoc cref="BitChartLineDataset{T}.HoverBorderColor"/>
    public string HoverBorderColor { get; set; }

    /// <inheritdoc cref="BitChartLineDataset{T}.HoverBorderDash"/>
    public int[] HoverBorderDash { get; set; }

    /// <inheritdoc cref="BitChartLineDataset{T}.HoverBorderDashOffset"/>
    public int? HoverBorderDashOffset { get; set; }

    /// <inheritdoc cref="BitChartLineDataset{T}.HoverBorderJoinStyle"/>
    public BitChartBorderJoinStyle HoverBorderJoinStyle { get; set; }

    /// <inheritdoc cref="BitChartLineDataset{T}.HoverBorderWidth"/>
    public int? HoverBorderWidth { get; set; }

    /// <inheritdoc cref="BitChartLineDataset{T}.Label"/>
    public string Label { get; set; }

    /// <summary>
    /// Gets or sets the bezier curve tension of the line. Set to 0 to draw straight lines.
    /// </summary>
    public double? LineTension { get; set; }

    /// <inheritdoc cref="BitChartLineDataset{T}.Order"/>
    public int? Order { get; set; }

    /// <inheritdoc cref="BitChartLineDataset{T}.PointBackgroundColor"/>
    public BitChartIndexableOption<string> PointBackgroundColor { get; set; }

    /// <inheritdoc cref="BitChartLineDataset{T}.PointBorderColor"/>
    public BitChartIndexableOption<string> PointBorderColor { get; set; }

    /// <inheritdoc cref="BitChartLineDataset{T}.PointBorderWidth"/>
    public BitChartIndexableOption<int> PointBorderWidth { get; set; }

    /// <inheritdoc cref="BitChartLineDataset{T}.PointHitRadius"/>
    public BitChartIndexableOption<int> PointHitRadius { get; set; }

    /// <inheritdoc cref="BitChartLineDataset{T}.PointHoverBackgroundColor"/>
    public BitChartIndexableOption<string> PointHoverBackgroundColor { get; set; }

    /// <inheritdoc cref="BitChartLineDataset{T}.PointHoverBorderColor"/>
    public BitChartIndexableOption<string> PointHoverBorderColor { get; set; }

    /// <inheritdoc cref="BitChartLineDataset{T}.PointHoverBorderWidth"/>
    public BitChartIndexableOption<int> PointHoverBorderWidth { get; set; }

    /// <inheritdoc cref="BitChartLineDataset{T}.PointHoverRadius"/>
    public BitChartIndexableOption<int> PointHoverRadius { get; set; }

    /// <inheritdoc cref="BitChartLineDataset{T}.PointRadius"/>
    public BitChartIndexableOption<int> PointRadius { get; set; }

    /// <inheritdoc cref="BitChartLineDataset{T}.PointRotation"/>
    public BitChartIndexableOption<double> PointRotation { get; set; }

    /// <inheritdoc cref="BitChartLineDataset{T}.PointStyle"/>
    public BitChartIndexableOption<BitChartPointStyle> PointStyle { get; set; }

    /// <inheritdoc cref="BitChartLineDataset{T}.SpanGaps"/>
    public bool? SpanGaps { get; set; }
}
