namespace Bit.BlazorUI.Legacy;

/// <summary>
/// The options-subconfig of a <see cref="BitChartLegacyLineConfig"/>
/// </summary>
public class BitChartLegacyLineOptions : BitChartLegacyBaseConfigOptions
{
    /// <summary>
    /// The scales for this chart. You can use any <see cref="BitChartLegacyCartesianAxis"/> for x and y.
    /// </summary>
    public BitChartLegacyScales? Scales { get; set; }

    /// <summary>
    /// If false, the lines between points are not drawn.
    /// </summary>
    public bool? ShowLines { get; set; }

    /// <summary>
    /// If false, NaN data causes a break in the line.
    /// </summary>
    public bool? SpanGaps { get; set; }
}
