namespace Bit.BlazorUI;

/// <summary>
/// The options-subconfig of a <see cref="BitChartLineConfig"/>
/// </summary>
public class BitChartLineOptions : BitChartBaseConfigOptions
{
    /// <summary>
    /// The scales for this chart. You can use any <see cref="BitChartCartesianAxis"/> for x and y.
    /// </summary>
    public BitChartScales Scales { get; set; }

    /// <summary>
    /// If false, the lines between points are not drawn.
    /// </summary>
    public bool? ShowLines { get; set; }

    /// <summary>
    /// If false, NaN data causes a break in the line.
    /// </summary>
    public bool? SpanGaps { get; set; }
}
