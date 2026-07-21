namespace Bit.BlazorUI.Legacy;

/// <summary>
/// Defines the chart types.
/// </summary>
public sealed class BitChartLegacyChartType : BitChartLegacyStringEnum
{
    /// <summary>
    /// The bar chart type.
    /// </summary>
    public static BitChartLegacyChartType Bar => new BitChartLegacyChartType("bar");

    /// <summary>
    /// The horizontal bar chart type.
    /// </summary>
    public static BitChartLegacyChartType HorizontalBar => new BitChartLegacyChartType("horizontalBar");

    /// <summary>
    /// The line chart type.
    /// </summary>
    public static BitChartLegacyChartType Line => new BitChartLegacyChartType("line");

    /// <summary>
    /// The pie chart type.
    /// </summary>
    public static BitChartLegacyChartType Pie => new BitChartLegacyChartType("pie");

    /// <summary>
    /// The doughnut chart type.
    /// </summary>
    public static BitChartLegacyChartType Doughnut => new BitChartLegacyChartType("doughnut");

    /// <summary>
    /// The radar chart type.
    /// </summary>
    public static BitChartLegacyChartType Radar => new BitChartLegacyChartType("radar");

    /// <summary>
    /// The bubble chart type.
    /// </summary>
    public static BitChartLegacyChartType Bubble => new BitChartLegacyChartType("bubble");

    /// <summary>
    /// The polar area chart type.
    /// </summary>
    public static BitChartLegacyChartType PolarArea => new BitChartLegacyChartType("polarArea");

    /// <summary>
    /// The scatter chart type.
    /// </summary>
    public static BitChartLegacyChartType Scatter => new BitChartLegacyChartType("scatter");

    /// <summary>
    /// This method constructs a <see cref="BitChartLegacyChartType" /> which represents the given value.
    /// Only use this method if you're implementing your own chart e.g. for a Chart.js
    /// extension. Otherwise use the static properties.
    /// </summary>
    /// <param name="customChartType">The string representation of your custom chart type.</param>
    public static BitChartLegacyChartType Custom(string customChartType) => new BitChartLegacyChartType(customChartType);

    private BitChartLegacyChartType(string stringValue) : base(stringValue) { }
}
