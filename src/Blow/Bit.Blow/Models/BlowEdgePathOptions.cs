namespace Bit.Blow;

/// <summary>
/// Per-edge tuning for the built-in path algorithms. Mirrors React Flow's <c>pathOptions</c>;
/// every value is optional and falls back to the algorithm's default.
/// </summary>
public sealed class BlowEdgePathOptions
{
    /// <summary>
    /// How far a bezier edge bulges out when its target lies behind the side its source handle
    /// faces, so the curve has to double back. Higher values bulge more. When the target is ahead
    /// of the handle the control points reach half the distance whatever this is. Bezier edges
    /// only. Default 0.25.
    /// </summary>
    public double? Curvature { get; set; }

    /// <summary>Corner rounding for smooth-step edges. 0 gives sharp corners. Default 5.</summary>
    public double? BorderRadius { get; set; }

    /// <summary>
    /// How far a step edge travels straight out of a handle before it is allowed to turn.
    /// Step and smooth-step edges only. Default 20.
    /// </summary>
    public double? Offset { get; set; }

    /// <summary>
    /// Where along the route a step edge places its bend: 0 hugs the source, 1 hugs the target,
    /// 0.5 (the default) splits the difference. Step and smooth-step edges only.
    /// </summary>
    public double? StepPosition { get; set; }

    /// <summary>
    /// How far a self-connecting edge's loop stands off its node, in flow units. Only applies to
    /// an edge whose source and target are the same node, where every ordinary path algorithm
    /// would collapse onto the node itself. Default 60.
    /// </summary>
    public double? LoopRadius { get; set; }

    /// <summary>
    /// Copies the options. Edges each need their own instance: the options are mutable, so a
    /// shared one would let tuning one edge quietly retune every other edge holding it.
    /// </summary>
    public BlowEdgePathOptions Clone() => new()
    {
        Curvature = Curvature,
        BorderRadius = BorderRadius,
        Offset = Offset,
        StepPosition = StepPosition,
        LoopRadius = LoopRadius,
    };

    /// <summary>True when both are null, or both carry the same values.</summary>
    internal static bool SameValues(BlowEdgePathOptions? a, BlowEdgePathOptions? b)
        => ReferenceEquals(a, b)
           || (a is not null && b is not null
               && a.Curvature == b.Curvature
               && a.BorderRadius == b.BorderRadius
               && a.Offset == b.Offset
               && a.StepPosition == b.StepPosition
               && a.LoopRadius == b.LoopRadius);
}
