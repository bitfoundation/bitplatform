
namespace Bit.Bink;

/// <summary>How a stroke's path should be painted.</summary>
public enum BinkStrokeGeometryMode : byte
{
    /// <summary>The path is a closed outline polygon to be filled (variable width strokes).</summary>
    Fill = 0,
    /// <summary>The path is a centerline to be stroked with a constant width.</summary>
    Centerline = 1,
}
