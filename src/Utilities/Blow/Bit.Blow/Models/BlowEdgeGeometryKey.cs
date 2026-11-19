namespace Bit.Blow;

/// <summary>
/// The inputs an edge's geometry is derived from, as a comparable value. Anything not listed
/// here (selection, colour, animation, label text) changes how the edge is <em>painted</em>
/// but not where it runs, so it must not invalidate the cached path.
/// </summary>
/// <remarks>
/// The path options are flattened into their individual values rather than held as a reference,
/// so tuning an <c>BlowEdgePathOptions</c> in place is observed just as readily as replacing it.
/// </remarks>
internal readonly record struct BlowEdgeGeometryKey(
    bool Resolved,
    bool SelfLoop,
    BlowRect LoopNode,
    double SourceX, double SourceY, BlowPosition SourcePosition,
    double TargetX, double TargetY, BlowPosition TargetPosition,
    BlowEdgeType Type,
    double? Curvature, double? BorderRadius, double? Offset, double? StepPosition,
    double? LoopRadius,
    bool MarkerStart, bool MarkerEnd, BlowMarkerSpec Marker,
    int RouteVersion = 0);
