namespace Bit.BlazorUI;

/// <summary>
/// Defines the chart types.
/// </summary>
public sealed class BitChartChartType : BitChartStringEnum
{
    /// <summary>
    /// The bar chart type.
    /// </summary>
    public static BitChartChartType Bar => new BitChartChartType("bar");

    /// <summary>
    /// The horizontal bar chart type.
    /// </summary>
    public static BitChartChartType HorizontalBar => new BitChartChartType("horizontalBar");

    /// <summary>
    /// The line chart type.
    /// </summary>
    public static BitChartChartType Line => new BitChartChartType("line");

    /// <summary>
    /// The pie chart type.
    /// </summary>
    public static BitChartChartType Pie => new BitChartChartType("pie");

    /// <summary>
    /// The doughnut chart type.
    /// </summary>
    public static BitChartChartType Doughnut => new BitChartChartType("doughnut");

    /// <summary>
    /// The radar chart type.
    /// </summary>
    public static BitChartChartType Radar => new BitChartChartType("radar");

    /// <summary>
    /// The bubble chart type.
    /// </summary>
    public static BitChartChartType Bubble => new BitChartChartType("bubble");

    /// <summary>
    /// The polar area chart type.
    /// </summary>
    public static BitChartChartType PolarArea => new BitChartChartType("polarArea");

    /// <summary>
    /// The scatter chart type.
    /// </summary>
    public static BitChartChartType Scatter => new BitChartChartType("scatter");

    /// <summary>
    /// This method constructs a <see cref="BitChartChartType" /> which represents the given value.
    /// Only use this method if you're implementing your own chart e.g. for a Chart.js
    /// extension. Otherwise use the static properties.
    /// </summary>
    /// <param name="customChartType">The string representation of your custom chart type.</param>
    public static BitChartChartType Custom(string customChartType) => new BitChartChartType(customChartType);

    private BitChartChartType(string stringValue) : base(stringValue) { }
}
