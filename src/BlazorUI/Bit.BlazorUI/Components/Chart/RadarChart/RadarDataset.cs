namespace Bit.BlazorUI;

/// <inheritdoc/>
public class RadarDataset : RadarDataset<double>
{
    /// <inheritdoc/>
    public RadarDataset() { }

    /// <inheritdoc/>
    public RadarDataset(IEnumerable<double> data) : base(data) { }

    /// <inheritdoc/>
    protected RadarDataset(ChartType type) : base(type) { }
}

/// <summary>
/// Represents a dataset for a radar chart.
/// As per documentation <a href="https://www.chartjs.org/docs/latest/charts/radar.html#dataset-properties">here (Chart.js)</a>.
/// </summary>
// Very similar to LineDataset, so the summaries are inherited.
public class RadarDataset<T> : Dataset<T>
{
    /// <summary>
    /// Creates a new instance of <see cref="RadarDataset{T}"/>.
    /// </summary>
    public RadarDataset() : base(ChartType.Radar) { }

    /// <summary>
    /// Creates a new instance of <see cref="RadarDataset{T}"/> with initial data.
    /// </summary>
    public RadarDataset(IEnumerable<T> data) : this()
    {
        AddRange(data);
    }

    /// <summary>
    /// Creates a new instance of <see cref="RadarDataset{T}"/> with
    /// a custom <see cref="ChartType"/>. Use this constructor when
    /// you implement a radar-like chart.
    /// </summary>
    /// <param name="type">The <see cref="ChartType"/> to use instead of <see cref="ChartType.Radar"/>.</param>
    protected RadarDataset(ChartType type) : base(type) { }

    /// <inheritdoc cref="LineDataset{T}.BackgroundColor"/>
    public string BackgroundColor { get; set; }

    /// <inheritdoc cref="LineDataset{T}.BorderCapStyle"/>
    public BorderCapStyle BorderCapStyle { get; set; }

    /// <inheritdoc cref="LineDataset{T}.BorderColor"/>
    public string BorderColor { get; set; }

    /// <inheritdoc cref="LineDataset{T}.BorderDash"/>
    public int[] BorderDash { get; set; }

    /// <inheritdoc cref="LineDataset{T}.BorderDashOffset"/>
    public int? BorderDashOffset { get; set; }

    /// <inheritdoc cref="LineDataset{T}.BorderJoinStyle"/>
    public BorderJoinStyle BorderJoinStyle { get; set; }

    /// <inheritdoc cref="LineDataset{T}.BorderWidth"/>
    public int? BorderWidth { get; set; }

    /// <inheritdoc cref="LineDataset{T}.Fill"/>
    public FillingMode Fill { get; set; }

    /// <inheritdoc cref="LineDataset{T}.HoverBackgroundColor"/>
    public string HoverBackgroundColor { get; set; }

    /// <inheritdoc cref="LineDataset{T}.HoverBorderCapStyle"/>
    public BorderCapStyle HoverBorderCapStyle { get; set; }

    /// <inheritdoc cref="LineDataset{T}.HoverBorderColor"/>
    public string HoverBorderColor { get; set; }

    /// <inheritdoc cref="LineDataset{T}.HoverBorderDash"/>
    public int[] HoverBorderDash { get; set; }

    /// <inheritdoc cref="LineDataset{T}.HoverBorderDashOffset"/>
    public int? HoverBorderDashOffset { get; set; }

    /// <inheritdoc cref="LineDataset{T}.HoverBorderJoinStyle"/>
    public BorderJoinStyle HoverBorderJoinStyle { get; set; }

    /// <inheritdoc cref="LineDataset{T}.HoverBorderWidth"/>
    public int? HoverBorderWidth { get; set; }

    /// <inheritdoc cref="LineDataset{T}.Label"/>
    public string Label { get; set; }

    /// <summary>
    /// Gets or sets the bezier curve tension of the line. Set to 0 to draw straight lines.
    /// </summary>
    public double? LineTension { get; set; }

    /// <inheritdoc cref="LineDataset{T}.Order"/>
    public int? Order { get; set; }

    /// <inheritdoc cref="LineDataset{T}.PointBackgroundColor"/>
    public IndexableOption<string> PointBackgroundColor { get; set; }

    /// <inheritdoc cref="LineDataset{T}.PointBorderColor"/>
    public IndexableOption<string> PointBorderColor { get; set; }

    /// <inheritdoc cref="LineDataset{T}.PointBorderWidth"/>
    public IndexableOption<int> PointBorderWidth { get; set; }

    /// <inheritdoc cref="LineDataset{T}.PointHitRadius"/>
    public IndexableOption<int> PointHitRadius { get; set; }

    /// <inheritdoc cref="LineDataset{T}.PointHoverBackgroundColor"/>
    public IndexableOption<string> PointHoverBackgroundColor { get; set; }

    /// <inheritdoc cref="LineDataset{T}.PointHoverBorderColor"/>
    public IndexableOption<string> PointHoverBorderColor { get; set; }

    /// <inheritdoc cref="LineDataset{T}.PointHoverBorderWidth"/>
    public IndexableOption<int> PointHoverBorderWidth { get; set; }

    /// <inheritdoc cref="LineDataset{T}.PointHoverRadius"/>
    public IndexableOption<int> PointHoverRadius { get; set; }

    /// <inheritdoc cref="LineDataset{T}.PointRadius"/>
    public IndexableOption<int> PointRadius { get; set; }

    /// <inheritdoc cref="LineDataset{T}.PointRotation"/>
    public IndexableOption<double> PointRotation { get; set; }

    /// <inheritdoc cref="LineDataset{T}.PointStyle"/>
    public IndexableOption<PointStyle> PointStyle { get; set; }

    /// <inheritdoc cref="LineDataset{T}.SpanGaps"/>
    public bool? SpanGaps { get; set; }
}
