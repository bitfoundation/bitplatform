namespace Bit.BlazorUI;

/// <summary>
/// Represents a relative direction or position on a 2D canvas.
/// </summary>
public sealed class Position : StringEnum
{
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
    public static Position Left => new Position("left");
    public static Position Right => new Position("right");
    public static Position Top => new Position("top");
    public static Position Bottom => new Position("bottom");
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member

    private Position(string stringRep) : base(stringRep) { }
}
