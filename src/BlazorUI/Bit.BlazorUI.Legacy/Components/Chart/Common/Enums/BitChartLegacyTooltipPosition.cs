namespace Bit.BlazorUI.Legacy;

/// <summary>
/// Specifies where the tooltip will be displayed.
/// <para>As per documentation <a href="https://www.chartjs.org/docs/latest/configuration/tooltip.html#position-modes">here (Chart.js)</a>.</para>
/// </summary>
public sealed class BitChartLegacyTooltipPosition : BitChartLegacyStringEnum
{
    /// <summary>
    /// When <see cref="BitChartLegacyTooltipPosition.Average" /> is set, the tooltip will be placed at the average position of the items displayed in the tooltip.
    /// </summary>
    public static BitChartLegacyTooltipPosition Average => new BitChartLegacyTooltipPosition("average");

    /// <summary>
    /// When <see cref="BitChartLegacyTooltipPosition.Nearest" /> is set, the tooltip will be placed at the position of the element closest to the event position.
    /// </summary>
    public static BitChartLegacyTooltipPosition Nearest => new BitChartLegacyTooltipPosition("nearest");

    private BitChartLegacyTooltipPosition(string stringRep) : base(stringRep) { }
}
