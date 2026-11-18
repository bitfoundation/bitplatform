namespace Bit.Blow;

/// <summary>What an <see cref="IBlowEdgeRouter"/> is asked to route: one edge, its two anchors and the nodes near it.</summary>
/// <param name="Edge">The edge.</param>
/// <param name="Source">Where the path starts: the source handle, in flow coordinates.</param>
/// <param name="SourceSide">The side of its node the source handle is on, which the path leaves through.</param>
/// <param name="Target">Where the path ends: the target handle.</param>
/// <param name="TargetSide">The side of its node the target handle is on, which the path arrives through.</param>
/// <param name="Obstacles">
/// The boxes of the shown nodes near the edge, in flow coordinates: its own two nodes included, so the path does not
/// cut back across them, and the groups either end sits inside left out, since the path has to run inside them.
/// </param>
public sealed record BlowRouteRequest(
    BlowEdge Edge,
    BlowXYPosition Source,
    BlowPosition SourceSide,
    BlowXYPosition Target,
    BlowPosition TargetSide,
    IReadOnlyList<BlowRect> Obstacles);
