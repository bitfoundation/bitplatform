namespace Bit.BlazorUI;

/// <summary>
/// Specifies where the tooltip will be displayed.
/// <para>As per documentation <a href="https://www.chartjs.org/docs/latest/configuration/tooltip.html#position-modes">here (Chart.js)</a>.</para>
/// </summary>
public sealed class BitChartTooltipPosition : BitChartStringEnum
{
    /// <summary>
    /// When <see cref="BitChartTooltipPosition.Average" /> is set, the tooltip will be placed at the average position of the items displayed in the tooltip.
    /// </summary>
    public static BitChartTooltipPosition Average => new BitChartTooltipPosition("average");

    /// <summary>
    /// When <see cref="BitChartTooltipPosition.Nearest" /> is set, the tooltip will be placed at the position of the element closest to the event position.
    /// </summary>
    public static BitChartTooltipPosition Nearest => new BitChartTooltipPosition("nearest");

    private BitChartTooltipPosition(string stringRep) : base(stringRep) { }
}
