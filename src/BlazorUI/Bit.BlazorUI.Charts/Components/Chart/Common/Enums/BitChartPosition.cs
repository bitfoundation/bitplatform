namespace Bit.BlazorUI;

/// <summary>
/// Represents a relative direction or position on a 2D canvas.
/// </summary>
public sealed class BitChartPosition : BitChartStringEnum
{
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
    public static BitChartPosition Left => new BitChartPosition("left");
    public static BitChartPosition Right => new BitChartPosition("right");
    public static BitChartPosition Top => new BitChartPosition("top");
    public static BitChartPosition Bottom => new BitChartPosition("bottom");
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member

    private BitChartPosition(string stringRep) : base(stringRep) { }
}
