namespace Bit.BlazorUI;

/// <summary>
/// Represents the possible axis directions.
/// </summary>
public sealed class AxisDirection : StringEnum
{
    /// <summary>
    /// The X-axis direction.
    /// </summary>
    public static AxisDirection X => new AxisDirection("x");

    /// <summary>
    /// The Y-axis direction.
    /// </summary>
    public static AxisDirection Y => new AxisDirection("y");

    /// <summary>
    /// Both the X- and Y-axis direction.
    /// </summary>
    public static AxisDirection XY => new AxisDirection("xy");

    private AxisDirection(string stringRep) : base(stringRep) { }
}
