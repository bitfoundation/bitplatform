namespace Bit.BlazorUI;

/// <summary>
/// Defines the chart types.
/// </summary>
public sealed class ChartType : StringEnum
{
    /// <summary>
    /// The bar chart type.
    /// </summary>
    public static ChartType Bar => new ChartType("bar");

    /// <summary>
    /// The horizontal bar chart type.
    /// </summary>
    public static ChartType HorizontalBar => new ChartType("horizontalBar");

    /// <summary>
    /// The line chart type.
    /// </summary>
    public static ChartType Line => new ChartType("line");

    /// <summary>
    /// The pie chart type.
    /// </summary>
    public static ChartType Pie => new ChartType("pie");

    /// <summary>
    /// The doughnut chart type.
    /// </summary>
    public static ChartType Doughnut => new ChartType("doughnut");

    /// <summary>
    /// The radar chart type.
    /// </summary>
    public static ChartType Radar => new ChartType("radar");

    /// <summary>
    /// The bubble chart type.
    /// </summary>
    public static ChartType Bubble => new ChartType("bubble");

    /// <summary>
    /// The polar area chart type.
    /// </summary>
    public static ChartType PolarArea => new ChartType("polarArea");

    /// <summary>
    /// The scatter chart type.
    /// </summary>
    public static ChartType Scatter => new ChartType("scatter");

    /// <summary>
    /// This method constructs a <see cref="ChartType" /> which represents the given value.
    /// Only use this method if you're implementing your own chart e.g. for a Chart.js
    /// extension. Otherwise use the static properties.
    /// </summary>
    /// <param name="customChartType">The string representation of your custom chart type.</param>
    public static ChartType Custom(string customChartType) => new ChartType(customChartType);

    private ChartType(string stringValue) : base(stringValue) { }
}
