namespace Bit.BlazorUI.Legacy;

/// <summary>
/// Represents a relative direction or position on a 2D canvas.
/// </summary>
public sealed class BitChartLegacyPosition : BitChartLegacyStringEnum
{
    public static BitChartLegacyPosition Left => new BitChartLegacyPosition("left");
    public static BitChartLegacyPosition Right => new BitChartLegacyPosition("right");
    public static BitChartLegacyPosition Top => new BitChartLegacyPosition("top");
    public static BitChartLegacyPosition Bottom => new BitChartLegacyPosition("bottom");

    private BitChartLegacyPosition(string stringRep) : base(stringRep) { }
}
