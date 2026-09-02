namespace Bit.Butil;

/// <summary>
/// One stroke of handwriting: the points sampled between a pointer going down and coming back up.
/// </summary>
/// <remarks>
/// Stroke boundaries carry meaning - a "t" is two strokes, an "l" is one - so collect them as the
/// user draws rather than merging everything into a single stroke.
/// </remarks>
public class HandwritingStroke
{
    /// <summary>The points, in the order they were sampled.</summary>
    public HandwritingPoint[] Points { get; set; } = [];
}
