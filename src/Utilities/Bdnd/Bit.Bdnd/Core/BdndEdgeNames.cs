namespace Bit.Bdnd;

/// <summary>The names the module and the <c>data-bit-bdnd-edge</c> attribute give the edges.</summary>
internal static class BdndEdgeNames
{
    public static BdndEdge? Parse(string? edge) => edge switch
    {
        "top" => BdndEdge.Top,
        "bottom" => BdndEdge.Bottom,
        "left" => BdndEdge.Left,
        "right" => BdndEdge.Right,
        _ => null,
    };

    public static string? Name(BdndEdge? edge) => edge switch
    {
        BdndEdge.Top => "top",
        BdndEdge.Bottom => "bottom",
        BdndEdge.Left => "left",
        BdndEdge.Right => "right",
        _ => null,
    };
}
