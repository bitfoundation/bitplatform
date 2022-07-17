namespace Bit.BlazorUI;

/// <summary>
/// The grid lines sub-config of the axis-configuration (see <see cref="Axes.Axis"/>).
/// </summary>
public class BitChartGridLines
{
    /// <summary>
    /// If false, do not display grid lines for this axis.
    /// </summary>
    public bool? Display { get; set; }

    /// <summary>
    /// If true, gridlines are circular (on radar chart only).
    /// </summary>
    public bool? Circular { get; set; }

    /// <summary>
    /// The color of the grid lines. If specified as an array, the first color applies to the first grid line, the second to the second grid line and so on.
    /// <para>See <see cref="BitChartColorUtil"/> for working with colors.</para>
    /// </summary>
    public BitChartIndexableOption<string> Color { get; set; }

    /// <summary>
    /// Length and spacing of dashes on grid lines
    /// <para>See <a href="https://developer.mozilla.org/en-US/docs/Web/API/CanvasRenderingContext2D/setLineDash"/> for patterns and details</para>
    /// </summary>
    public double[] BorderDash { get; set; }

    /// <summary>
    /// Stroke width of grid lines.
    /// </summary>
    public BitChartIndexableOption<double> LineWidth { get; set; }

    /// <summary>
    /// If true, draw border at the edge between the axis and the chart area.
    /// </summary>
    public bool? DrawBorder { get; set; }

    /// <summary>
    /// If true, draw lines on the chart area inside the axis lines. This is useful when there are multiple axes and you need to control which grid lines are drawn.
    /// </summary>
    public bool? DrawOnChartArea { get; set; }

    /// <summary>
    /// If true, draw lines beside the ticks in the axis area beside the chart.
    /// </summary>
    public bool? DrawTicks { get; set; }

    /// <summary>
    /// Length in pixels that the grid lines will draw into the axis area.
    /// </summary>
    public int? TickMarkLength { get; set; }

    /// <summary>
    /// Stroke width of the grid line for the first index (index 0).
    /// </summary>
    public int? ZeroLineWidth { get; set; }

    /// <summary>
    /// Stroke color of the grid line for the first index (index 0).
    /// <para>See <see cref="BitChartColorUtil"/> for working with colors.</para>
    /// </summary>
    public string ZeroLineColor { get; set; }

    /// <summary>
    /// Length and spacing of dashes of the grid line for the first index (index 0).
    /// <para>See <a href="https://developer.mozilla.org/en-US/docs/Web/API/CanvasRenderingContext2D/setLineDash"/> for details.</para>
    /// </summary>
    public double[] ZeroLineBorderDash { get; set; }

    /// <summary>
    /// Offset for line dashes of the grid line for the first index (index 0).
    /// <para>See <a href="https://developer.mozilla.org/en-US/docs/Web/API/CanvasRenderingContext2D/lineDashOffset"/> for details.</para>
    /// </summary>
    public double? ZeroLineBorderDashOffset { get; set; }

    /// <summary>
    /// If true, grid lines will be shifted to be between labels. This is set to true for a category scale in a bar chart by default.
    /// </summary>
    public bool? OffsetGridLines { get; set; }
}
