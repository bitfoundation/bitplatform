namespace Bit.BlazorUI.Legacy;

/// <summary>
/// Represents the possible axis directions.
/// </summary>
public sealed class BitChartLegacyAxisDirection : BitChartLegacyStringEnum
{
    /// <summary>
    /// The X-axis direction.
    /// </summary>
    public static BitChartLegacyAxisDirection X => new BitChartLegacyAxisDirection("x");

    /// <summary>
    /// The Y-axis direction.
    /// </summary>
    public static BitChartLegacyAxisDirection Y => new BitChartLegacyAxisDirection("y");

    /// <summary>
    /// Both the X- and Y-axis direction.
    /// </summary>
    public static BitChartLegacyAxisDirection XY => new BitChartLegacyAxisDirection("xy");

    private BitChartLegacyAxisDirection(string stringRep) : base(stringRep) { }
}
