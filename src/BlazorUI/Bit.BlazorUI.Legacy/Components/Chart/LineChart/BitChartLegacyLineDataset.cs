using Newtonsoft.Json;

namespace Bit.BlazorUI.Legacy;

/// <summary>
/// Represents a dataset for a line chart.
/// As per documentation <a href="https://www.chartjs.org/docs/latest/charts/line.html#dataset-properties">here (Chart.js)</a>.
/// </summary>
/// <typeparam name="T">The type of data this <see cref="BitChartLegacyLineDataset{T}"/> contains.</typeparam>
public class BitChartLegacyLineDataset<T> : BitChartLegacyDataset<T>
{
    /// <summary>
    /// Creates a new instance of <see cref="BitChartLegacyLineDataset{T}"/>.
    /// </summary>
    public BitChartLegacyLineDataset() : base(BitChartLegacyChartType.Line) { }

    /// <summary>
    /// Creates a new instance of <see cref="BitChartLegacyLineDataset{T}"/> with initial data.
    /// </summary>
    public BitChartLegacyLineDataset(IEnumerable<T> data) : this()
    {
        AddRange(data);
    }

    /// <summary>
    /// Creates a new instance of <see cref="BitChartLegacyLineDataset{T}"/> with
    /// a custom <see cref="BitChartLegacyChartType"/>. Use this constructor when
    /// you implement a line-like chart.
    /// </summary>
    /// <param name="type">The <see cref="BitChartLegacyChartType"/> to use instead of <see cref="BitChartLegacyChartType.Line"/>.</param>
    protected BitChartLegacyLineDataset(BitChartLegacyChartType type) : base(type) { }

    /// <summary>
    /// Gets or sets the fill color under the line.
    /// <para>See <see cref="BitChartLegacyColorUtil"/> for working with colors.</para>
    /// </summary>
    public string? BackgroundColor { get; set; }

    /// <summary>
    /// Gets or sets the cap style of the line.
    /// </summary>
    public BitChartLegacyBorderCapStyle? BorderCapStyle { get; set; }

    /// <summary>
    /// Gets or sets the color of the line.
    /// <para>See <see cref="BitChartLegacyColorUtil"/> for working with colors.</para>
    /// </summary>
    public string? BorderColor { get; set; }

    /// <summary>
    /// Gets or sets the length and spacing of the line dashes.
    /// As per documentation <a href="https://developer.mozilla.org/en-US/docs/Web/API/CanvasRenderingContext2D/setLineDash">here (MDN)</a>.
    /// </summary>
    public int[]? BorderDash { get; set; }

    /// <summary>
    /// Gets or sets the offset for the line dashes.
    /// As per documentation <a href="https://developer.mozilla.org/en-US/docs/Web/API/CanvasRenderingContext2D/lineDashOffset">here (MDN)</a>.
    /// </summary>
    public int? BorderDashOffset { get; set; }

    /// <summary>
    /// Gets or sets the <see cref="BitChartLegacyBorderJoinStyle"/> for the lines.
    /// </summary>
    public BitChartLegacyBorderJoinStyle? BorderJoinStyle { get; set; }

    /// <summary>
    /// Gets or sets the width of the line (in pixels).
    /// </summary>
    public int? BorderWidth { get; set; }

    /// <summary>
    /// Gets or sets the algorithm used to interpolate a smooth curve from the discrete data points.
    /// </summary>
    public BitChartLegacyCubicInterpolationMode? CubicInterpolationMode { get; set; }

    /// <summary>
    /// Gets or sets how to clip relative to the chart area. Positive values allow overflow,
    /// negative values clip that many pixels inside the chart area.
    /// </summary>
    public BitChartLegacyClipping? Clip { get; set; }

    /// <summary>
    /// Gets or sets how to fill the area under the line.
    /// </summary>
    public BitChartLegacyFillingMode? Fill { get; set; }

    /// <summary>
    /// Gets or sets the fill color under the line when hovered.
    /// <para>See <see cref="BitChartLegacyColorUtil"/> for working with colors.</para>
    /// </summary>
    public string? HoverBackgroundColor { get; set; }

    /// <summary>
    /// Gets or sets the cap style of the line when hovered.
    /// </summary>
    public BitChartLegacyBorderCapStyle? HoverBorderCapStyle { get; set; }

    /// <summary>
    /// Gets or sets the color of the line when hovered.
    /// <para>See <see cref="BitChartLegacyColorUtil"/> for working with colors.</para>
    /// </summary>
    public string? HoverBorderColor { get; set; }

    /// <summary>
    /// Gets or sets the length and spacing of the line dashes when hovered.
    /// As per documentation <a href="https://developer.mozilla.org/en-US/docs/Web/API/CanvasRenderingContext2D/setLineDash">here (MDN)</a>.
    /// </summary>
    public int[]? HoverBorderDash { get; set; }

    /// <summary>
    /// Gets or sets the offset for the line dashes when hovered.
    /// As per documentation <a href="https://developer.mozilla.org/en-US/docs/Web/API/CanvasRenderingContext2D/lineDashOffset">here (MDN)</a>.
    /// </summary>
    public int? HoverBorderDashOffset { get; set; }

    /// <summary>
    /// Gets or sets the <see cref="BitChartLegacyBorderJoinStyle"/> for the lines when hovered.
    /// </summary>
    public BitChartLegacyBorderJoinStyle? HoverBorderJoinStyle { get; set; }

    /// <summary>
    /// Gets or sets the width of the line when hovered (in pixels).
    /// </summary>
    public int? HoverBorderWidth { get; set; }

    /// <summary>
    /// Gets or sets the label for the dataset which appears in the legend and the tooltips.
    /// </summary>
    public string? Label { get; set; }

    /// <summary>
    /// Gets or sets the bezier curve tension of the line. Set to 0 to draw straight lines.
    /// This option is ignored if <see cref="BitChartLegacyCubicInterpolationMode.Monotone"/> is used.
    /// </summary>
    public double? LineTension { get; set; }

    /// <summary>
    /// Gets or sets the drawing order of this dataset.
    /// Also affects the order for stacking, tooltips, and the legend.
    /// </summary>
    public int? Order { get; set; }

    /// <summary>
    /// Gets or sets the fill color for the points.
    /// <para>See <see cref="BitChartLegacyColorUtil"/> for working with colors.</para>
    /// </summary>
    public BitChartLegacyIndexableOption<string>? PointBackgroundColor { get; set; }

    /// <summary>
    /// Gets or sets the border color for the points.
    /// <para>See <see cref="BitChartLegacyColorUtil"/> for working with colors.</para>
    /// </summary>
    public BitChartLegacyIndexableOption<string>? PointBorderColor { get; set; }

    /// <summary>
    /// Gets or sets the width of the point border (in pixels).
    /// </summary>
    public BitChartLegacyIndexableOption<int>? PointBorderWidth { get; set; }

    /// <summary>
    /// Gets or sets the radius of the non-displayed point that reacts to mouse events (in pixels).
    /// </summary>
    public BitChartLegacyIndexableOption<int>? PointHitRadius { get; set; }

    /// <summary>
    /// Gets or sets the fill color for the points when hovering.
    /// <para>See <see cref="BitChartLegacyColorUtil"/> for working with colors.</para>
    /// </summary>
    public BitChartLegacyIndexableOption<string>? PointHoverBackgroundColor { get; set; }

    /// <summary>
    /// Gets or sets the border color for the points when hovering.
    /// <para>See <see cref="BitChartLegacyColorUtil"/> for working with colors.</para>
    /// </summary>
    public BitChartLegacyIndexableOption<string>? PointHoverBorderColor { get; set; }

    /// <summary>
    /// Gets or sets the width of the point border when hovered (in pixels).
    /// </summary>
    public BitChartLegacyIndexableOption<int>? PointHoverBorderWidth { get; set; }

    /// <summary>
    /// Gets or sets the radius of the point when hovered.
    /// </summary>
    public BitChartLegacyIndexableOption<int>? PointHoverRadius { get; set; }

    /// <summary>
    /// Gets or sets the radius of the point shape. If set to 0, the point is not rendered.
    /// </summary>
    public BitChartLegacyIndexableOption<int>? PointRadius { get; set; }

    /// <summary>
    /// Gets or sets the rotation of the points in degrees.
    /// </summary>
    public BitChartLegacyIndexableOption<double>? PointRotation { get; set; }

    /// <summary>
    /// Gets or sets the <see cref="BitChartLegacyPointStyle"/> for this dataset.
    /// </summary>
    public BitChartLegacyIndexableOption<BitChartLegacyPointStyle>? PointStyle { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether or not the line is drawn for this dataset.
    /// </summary>
    public bool? ShowLine { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether or not lines will be drawn between points with no or null data.
    /// If <see langword="false"/>, points with NaN data will create a break in the line.
    /// </summary>
    public bool? SpanGaps { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether or not the line is shown as a stepped line.
    /// <para>
    /// If this value is set to anything other than <see cref="BitChartLegacySteppedLine.False"/>,
    /// <see cref="LineTension"/> will be ignored.
    /// </para>
    /// </summary>
    public BitChartLegacySteppedLine? SteppedLine { get; set; }

    /// <summary>
    /// Gets or sets the ID of the x axis to plot this dataset on. If not specified,
    /// this defaults to the ID of the first found x axis.
    /// </summary>
    [JsonProperty("xAxisID")]
    public string? XAxisId { get; set; }

    /// <summary>
    /// Gets or sets the ID of the y axis to plot this dataset on. If not specified,
    /// this defaults to the ID of the first found y axis.
    /// </summary>
    [JsonProperty("yAxisID")]
    public string? YAxisId { get; set; }
}
