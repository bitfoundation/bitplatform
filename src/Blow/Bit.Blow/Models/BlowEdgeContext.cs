namespace Bit.Blow;

/// <summary>
/// Everything a custom edge template needs to render: the edge model, the computed
/// path/label geometry, and the resolved source/target anchors and sides. Mirrors the
/// props React Flow passes to a custom edge component.
/// </summary>
public sealed class BlowEdgeContext
{
    /// <summary>The edge being rendered.</summary>
    public required BlowEdge Edge { get; init; }

    /// <summary>The computed path data and label anchor.</summary>
    public required BlowPathResult Geometry { get; init; }

    /// <summary>
    /// Snapshot of the inputs the geometry was computed from. The canvas compares it against
    /// the current inputs to decide whether this context can be reused, which is what lets a
    /// pan or zoom of a large graph avoid rebuilding every path string.
    /// </summary>
    internal BlowEdgeGeometryKey GeometryKey { get; init; }

    /// <summary>Where the edge leaves its source handle, in flow space.</summary>
    public BlowXYPosition Source { get; init; }

    /// <summary>Where the edge meets its target handle, in flow space.</summary>
    public BlowXYPosition Target { get; init; }

    /// <summary>Which side of the source node the edge leaves from.</summary>
    public BlowPosition SourcePosition { get; init; }

    /// <summary>Which side of the target node the edge arrives at.</summary>
    public BlowPosition TargetPosition { get; init; }

    /// <summary>
    /// Ready-to-use <c>url(#…)</c> reference for the edge's start marker, or null when the edge
    /// has none. Pass straight to <c>BlowBaseEdge.MarkerStart</c> so custom edges get the built-in
    /// arrowheads without having to know the canvas's generated marker ids.
    /// </summary>
    public string? MarkerStartUrl { get; init; }

    /// <summary>Ready-to-use <c>url(#…)</c> reference for the edge's end marker, or null.</summary>
    public string? MarkerEndUrl { get; init; }

    /// <summary>X coordinate of <see cref="Source"/>, for templates that want the parts separately.</summary>
    public double SourceX => Source.X;

    /// <summary>Y coordinate of <see cref="Source"/>.</summary>
    public double SourceY => Source.Y;

    /// <summary>X coordinate of <see cref="Target"/>.</summary>
    public double TargetX => Target.X;

    /// <summary>Y coordinate of <see cref="Target"/>.</summary>
    public double TargetY => Target.Y;

    /// <summary>SVG path data for the edge.</summary>
    public string Path => Geometry.Path;

    /// <summary>X coordinate of the edge's label anchor in flow space.</summary>
    public double LabelX => Geometry.LabelX;

    /// <summary>Y coordinate of the edge's label anchor in flow space.</summary>
    public double LabelY => Geometry.LabelY;
}

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
    bool MarkerStart, bool MarkerEnd, BlowMarkerSpec Marker);
