namespace Bit.Blow;

/// <summary>
/// Draws <see cref="BlowEdgeType.Routed"/> edges: the path of one edge from its source handle to its target handle,
/// round the nodes in its way. Set as <c>BlowCanvas.Routing</c> (and <see cref="BlowSvgExportOptions.Routing"/>) to
/// replace the built-in orthogonal router, which is <see cref="BlowRoutingOptions"/>.
/// </summary>
/// <remarks>
/// The canvas asks again only when the edge's ends or the nodes near it moved, or the router was replaced, so a router
/// has to answer the same request the same way. One whose answer depends on anything else - settings it reads, the
/// edge's data - is replaced with a new instance when that changes. (<see cref="BlowRoutingOptions"/> is the exception:
/// the canvas compares its settings, so changing them in place re-routes at the next render.) A router that throws
/// fails the render, as a template that throws does.
/// </remarks>
public interface IBlowEdgeRouter
{
    /// <summary>The edge's path, or null to have it drawn as a smooth step instead.</summary>
    BlowPathResult? Route(BlowRouteRequest request);
}

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
