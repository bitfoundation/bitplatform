namespace Bit.BlazorUI;

/// <summary>
/// The model of the legend items which are displayed in the chart-legend.
/// <para>
/// As per documentation here:
/// <a href="https://www.chartjs.org/docs/latest/configuration/legend.html#legend-item-interface"/>
/// </para>
/// </summary>
public class LegendItem
{
    /// <summary>
    /// Gets or sets the index of the dataset this legend item corresponds to.
    /// DO NOT set this value when returning an instance of this class to Chart.js.
    /// Only use this property when retrieving the index in a legend-event.
    /// This value might not be set in charts that have legend labels per value
    /// (instead of per dataset) like pie-chart.
    /// </summary>
    public int? DatasetIndex { get; set; }

    /// <summary>
    /// Gets or sets the index of the value this legend item corresponds to.
    /// DO NOT set this value when returning an instance of this class to Chart.js.
    /// Only use this property when retrieving the index in a legend-event.
    /// This value is only set in charts that have legend labels per value
    /// (instead of per dataset) like pie-chart.
    /// </summary>
    public int? Index { get; set; }

    /// <summary>
    /// Gets or sets the label-text that will be displayed.
    /// </summary>
    public string Text { get; set; }

    /// <summary>
    /// Gets or sets the color (style) of the legend box.
    /// <para>See <see cref="Util.ColorUtil"/> for working with colors.</para>
    /// </summary>
    public string FillStyle { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether or not this legend-item represents a
    /// hidden dataset. Label will be rendered with a strike-through effect if <see langword="true"/>.
    /// </summary>
    public bool? Hidden { get; set; }

    /// <summary>
    /// Gets or sets the <see cref="BorderCapStyle"/> for the legend box border.
    /// </summary>
    public BorderCapStyle LineCap { get; set; }

    /// <summary>
    /// Gets or sets the line dash segments for the legend box border.
    /// Details on
    /// <a href="https://developer.mozilla.org/en-US/docs/Web/API/CanvasRenderingContext2D/setLineDash"/>.
    /// </summary>
    public double[] LineDash { get; set; }

    /// <summary>
    /// Gets or sets the line dash offset.
    /// Details on
    /// <a href="https://developer.mozilla.org/en-US/docs/Web/API/CanvasRenderingContext2D/lineDashOffset"/>.
    /// </summary>
    public double? LineDashOffset { get; set; }

    /// <summary>
    /// Gets or sets the <see cref="BorderJoinStyle"/> of the legend box border.
    /// </summary>
    public BorderJoinStyle LineJoin { get; set; }

    /// <summary>
    /// Gets or sets the width of the box border.
    /// </summary>
    public double? LineWidth { get; set; }

    /// <summary>
    /// Gets or sets the color (style) of the legend box border.
    /// <para>See <see cref="Util.ColorUtil"/> for working with colors.</para>
    /// </summary>
    public string StrokeStyle { get; set; }

    /// <summary>
    /// Gets or sets the <see cref="Enums.PointStyle"/> of the legend box
    /// (only used if <see cref="LegendLabels.UsePointStyle"/> is <see langword="true"/>).
    /// </summary>
    public PointStyle PointStyle { get; set; }

    /// <summary>
    /// Gets or sets the rotation of the point in degrees
    /// (only used if <see cref="LegendLabels.UsePointStyle"/> is <see langword="true"/>).
    /// </summary>
    public double? Rotation { get; set; }
}
