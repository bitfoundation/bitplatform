namespace Bit.Blow;

/// <summary>
/// Built-in edge path algorithms, mirroring React Flow's edge types.
/// </summary>
public enum BlowEdgeType
{
    /// <summary>
    /// A cubic curve whose control points reach further out the further apart the handles are,
    /// so an edge doubling back on itself still leaves the node cleanly. The default.
    /// </summary>
    Bezier,

    /// <summary>A straight line between the two anchors, ignoring which side each handle is on.</summary>
    Straight,

    /// <summary>Orthogonal segments with square corners.</summary>
    Step,

    /// <summary>Orthogonal segments with rounded corners.</summary>
    SmoothStep,

    /// <summary>
    /// A bezier whose control points always project straight out along the handle axis,
    /// producing a flatter, more predictable curve than <see cref="Bezier"/>.
    /// </summary>
    SimpleBezier,

    /// <summary>
    /// Orthogonal segments with rounded corners that go around the nodes in the way, keeping a margin from them
    /// (<c>BlowCanvas.Routing</c>; <see cref="BlowEdgeRouting"/>). Re-routed whenever a node moves, so it costs more
    /// than the other types on a large, busy graph. Falls back to <see cref="SmoothStep"/> when no route is found.
    /// </summary>
    Routed,
}
