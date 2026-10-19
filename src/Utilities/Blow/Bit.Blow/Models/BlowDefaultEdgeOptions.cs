namespace Bit.Blow;

/// <summary>
/// Default values applied to edges created by user interaction (dragging/clicking
/// connections). Mirrors React Flow's <c>defaultEdgeOptions</c>.
/// </summary>
public sealed class BlowDefaultEdgeOptions
{
    /// <summary>Edge path type. When null, the canvas's <c>DefaultEdgeType</c> is used.</summary>
    public BlowEdgeType? Type { get; set; }

    /// <summary>Whether new edges are drawn with the marching-ants dash.</summary>
    public bool Animated { get; set; }

    /// <summary>Whether new edges draw a target arrowhead. When null, defaults to true.</summary>
    public bool? MarkerEnd { get; set; }

    /// <summary>Whether new edges draw a source arrowhead.</summary>
    public bool MarkerStart { get; set; }

    /// <summary>Arrowhead style used for new edges' markers.</summary>
    public BlowMarkerType MarkerType { get; set; } = BlowMarkerType.Arrow;

    /// <summary>Per-edge path tuning applied to new edges (curvature, corner radius, offset).</summary>
    public BlowEdgePathOptions? PathOptions { get; set; }

    /// <summary>Inline style applied to new edges' paths.</summary>
    public string? Style { get; set; }

    /// <summary>Click-target width for new edges. When null, the edge default (14) is kept.</summary>
    public double? InteractionWidth { get; set; }

    /// <summary>Stroke width for new edges. When null, the edge default (1.5) is kept.</summary>
    public double? StrokeWidth { get; set; }

    /// <summary>Stroke colour for new edges. When null they take the theme's edge colour.</summary>
    public string? Stroke { get; set; }

    /// <summary>Whether new edges may have their endpoints dragged to other handles.</summary>
    public bool Reconnectable { get; set; } = true;

    /// <summary>CSS class(es) applied to new edges.</summary>
    public string? Class { get; set; }

    /// <summary>Applies these options to a freshly created edge.</summary>
    public void ApplyTo(BlowEdge edge, BlowEdgeType fallbackType)
    {
        edge.Type = Type ?? fallbackType;
        // Each edge gets its own copy; sharing one would let tuning one edge retune them all.
        edge.PathOptions = PathOptions?.Clone();
        edge.Animated = Animated;
        edge.MarkerEnd = MarkerEnd ?? true;
        edge.MarkerStart = MarkerStart;
        edge.MarkerType = MarkerType;
        if (StrokeWidth is { } sw) edge.StrokeWidth = sw;
        if (InteractionWidth is { } iw) edge.InteractionWidth = iw;
        edge.Stroke = Stroke;
        edge.Style = Style;
        edge.Reconnectable = Reconnectable;
        edge.Class = Class;
    }
}
