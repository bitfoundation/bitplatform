namespace Bit.BlazorUI;

/// <summary>
/// Represents the possible axis directions.
/// </summary>
public sealed class BitChartAxisDirection : BitChartStringEnum
{
    /// <summary>
    /// The X-axis direction.
    /// </summary>
    public static BitChartAxisDirection X => new BitChartAxisDirection("x");

    /// <summary>
    /// The Y-axis direction.
    /// </summary>
    public static BitChartAxisDirection Y => new BitChartAxisDirection("y");

    /// <summary>
    /// Both the X- and Y-axis direction.
    /// </summary>
    public static BitChartAxisDirection XY => new BitChartAxisDirection("xy");

    private BitChartAxisDirection(string stringRep) : base(stringRep) { }
}
