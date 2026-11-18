namespace Bit.Blow;

/// <summary>
/// The built-in router: <see cref="BlowEdgeRouting.Orthogonal"/> with these settings. Set as <c>BlowCanvas.Routing</c>
/// to tune it, or set another <see cref="IBlowEdgeRouter"/> there to replace it.
/// </summary>
public sealed class BlowRoutingOptions : IBlowEdgeRouter
{
    /// <summary>The least clearance a route keeps from every obstacle, in flow units.</summary>
    public double Margin { get; set; } = 16;

    /// <summary>How far a route runs straight out of a handle before it may turn, in flow units.</summary>
    public double StubLength { get; set; } = 24;

    /// <summary>
    /// What a turn costs, in flow units of length: the route takes a detour of up to this much rather than one more
    /// bend. Higher gives fewer, longer-way-round bends.
    /// </summary>
    public double BendPenalty { get; set; } = 60;

    /// <summary>The radius of the rounded corners of the drawn path; 0 draws square corners.</summary>
    public double BorderRadius { get; set; } = 6;

    /// <summary>
    /// The most obstacles one route considers. Past it the search space grows with the square of the count, so the
    /// router gives up and the caller falls back to a plain step path.
    /// </summary>
    public int MaxObstacles { get; set; } = 100;

    /// <summary>The edge's orthogonal path round the obstacles, with these settings; see <see cref="BlowEdgeRouting.Route"/>.</summary>
    public BlowPathResult? Route(BlowRouteRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return BlowEdgeRouting.Route(request.Source, request.SourceSide, request.Target, request.TargetSide, request.Obstacles, this);
    }

    /// <summary>The settings the canvas and the SVG export route with when they are given no router.</summary>
    internal static readonly BlowRoutingOptions Default = new();
}
