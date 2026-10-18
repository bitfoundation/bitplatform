namespace Bit.Blow;

/// <summary>
/// An edge connecting a source node (handle) to a target node (handle).
/// Mirrors React Flow's edge object.
/// </summary>
public class BlowEdge
{
    /// <summary>Unique id. Required and must be unique across the flow.</summary>
    public required string Id { get; set; }

    /// <summary>Id of the source node.</summary>
    public required string Source { get; set; }

    /// <summary>Id of the target node.</summary>
    public required string Target { get; set; }

    /// <summary>Optional id of the specific source handle.</summary>
    public string? SourceHandle { get; set; }

    /// <summary>Optional id of the specific target handle.</summary>
    public string? TargetHandle { get; set; }

    /// <summary>Edge path algorithm. Defaults to <see cref="BlowEdgeType.Bezier"/>.</summary>
    public BlowEdgeType Type { get; set; } = BlowEdgeType.Bezier;

    /// <summary>Optional per-edge tuning of the path algorithm (curvature, corner radius, offset).</summary>
    public BlowEdgePathOptions? PathOptions { get; set; }

    /// <summary>Optional label rendered at the edge's midpoint.</summary>
    public string? Label { get; set; }

    /// <summary>Inline style applied to the label text, e.g. <c>"fill:#111;font-weight:600"</c>.</summary>
    public string? LabelStyle { get; set; }

    /// <summary>When false the label is drawn straight onto the edge with no backing plate.</summary>
    public bool LabelShowBg { get; set; } = true;

    /// <summary>Inline style applied to the label's backing plate, e.g. <c>"fill:#fff"</c>.</summary>
    public string? LabelBgStyle { get; set; }

    /// <summary>Horizontal breathing space between the label text and the edge of its plate.</summary>
    public double LabelBgPaddingX { get; set; } = 4;

    /// <summary>Vertical breathing space between the label text and the edge of its plate.</summary>
    public double LabelBgPaddingY { get; set; } = 2;

    /// <summary>Corner radius of the label's backing plate.</summary>
    public double LabelBgBorderRadius { get; set; } = 3;

    /// <summary>When true the edge is drawn with a marching-ants dash, the usual way of showing flow.</summary>
    public bool Animated { get; set; }

    /// <summary>Whether the edge is part of the current selection. Maintained by the canvas.</summary>
    public bool Selected { get; set; }

    /// <summary>When true the edge is not rendered and takes no part in interaction.</summary>
    public bool Hidden { get; set; }

    /// <summary>When false the edge cannot be selected by clicking or box selection.</summary>
    public bool Selectable { get; set; } = true;

    /// <summary>When true the edge is reachable with Tab and can be selected with Enter/Space.</summary>
    public bool Focusable { get; set; } = true;

    /// <summary>
    /// When false the edge survives delete interactions. Note that an edge is still removed
    /// when one of the nodes it connects is deleted.
    /// </summary>
    public bool Deletable { get; set; } = true;

    /// <summary>When true (and the canvas allows it), the edge's endpoints can be dragged to new handles.</summary>
    public bool Reconnectable { get; set; } = true;

    /// <summary>Stroke width in pixels.</summary>
    public double StrokeWidth { get; set; } = 1.5;

    /// <summary>Stroke color (any CSS color). Falls back to the theme default when null.</summary>
    public string? Stroke { get; set; }

    /// <summary>Whether to draw an arrowhead marker at the target end.</summary>
    public bool MarkerEnd { get; set; } = true;

    /// <summary>Whether to draw an arrowhead marker at the source end.</summary>
    public bool MarkerStart { get; set; }

    /// <summary>Arrowhead style used for <see cref="MarkerEnd"/>/<see cref="MarkerStart"/>.</summary>
    public BlowMarkerType MarkerType { get; set; } = BlowMarkerType.Arrow;

    /// <summary>
    /// Arrowhead colour. When null the arrowhead takes the edge's own stroke colour, which is
    /// almost always what you want - set it only to make the head stand out from the line.
    /// </summary>
    public string? MarkerColor { get; set; }

    /// <summary>Arrowhead width in pixels. Defaults to 7 (8 reads better for a closed head).</summary>
    public double? MarkerWidth { get; set; }

    /// <summary>Arrowhead height in pixels. Defaults to match <see cref="MarkerWidth"/>.</summary>
    public double? MarkerHeight { get; set; }

    /// <summary>The concrete arrowhead this edge asks for, with the defaults filled in.</summary>
    internal BlowMarkerSpec ResolveMarker() => ResolveMarker(null);

    /// <summary>
    /// The concrete arrowhead this edge asks for. <paramref name="defaultColor"/> is the canvas's
    /// <c>DefaultMarkerColor</c>, used only when the edge names no colour of its own; when both
    /// are absent the head takes the edge's stroke colour.
    /// </summary>
    internal BlowMarkerSpec ResolveMarker(string? defaultColor)
    {
        var fallback = MarkerType == BlowMarkerType.ArrowClosed ? 8 : BlowMarkerSpec.DefaultSize;
        var w = MarkerWidth ?? fallback;
        return new BlowMarkerSpec(MarkerType, MarkerColor ?? defaultColor, w, MarkerHeight ?? w);
    }

    /// <summary>Additional CSS class(es) applied to the edge group.</summary>
    public string? Class { get; set; }

    /// <summary>Inline style applied to the edge's visible path.</summary>
    public string? Style { get; set; }

    /// <summary>
    /// Width of the invisible path drawn on top of the edge to make it easier to click.
    /// Mirrors React Flow's <c>interactionWidth</c>.
    /// </summary>
    public double InteractionWidth { get; set; } = 14;

    /// <summary>Optional stacking order for the edge group within the edge layer.</summary>
    public int? ZIndex { get; set; }

    /// <summary>Arbitrary user payload.</summary>
    public object? Data { get; set; }

    /// <summary>
    /// Copies the edge. <see cref="Data"/> is copied by reference - the clone shares whatever
    /// payload you attached. <see cref="PathOptions"/> is copied, so the clone can be retuned
    /// without touching the original.
    /// </summary>
    public BlowEdge Clone(string? newId = null) => new()
    {
        Id = newId ?? Id,
        Source = Source,
        Target = Target,
        SourceHandle = SourceHandle,
        TargetHandle = TargetHandle,
        Type = Type,
        PathOptions = PathOptions?.Clone(),
        Label = Label,
        LabelStyle = LabelStyle,
        LabelShowBg = LabelShowBg,
        LabelBgStyle = LabelBgStyle,
        LabelBgPaddingX = LabelBgPaddingX,
        LabelBgPaddingY = LabelBgPaddingY,
        LabelBgBorderRadius = LabelBgBorderRadius,
        Animated = Animated,
        Selected = Selected,
        Hidden = Hidden,
        Selectable = Selectable,
        Focusable = Focusable,
        Deletable = Deletable,
        Reconnectable = Reconnectable,
        StrokeWidth = StrokeWidth,
        Stroke = Stroke,
        MarkerEnd = MarkerEnd,
        MarkerStart = MarkerStart,
        MarkerType = MarkerType,
        MarkerColor = MarkerColor,
        MarkerWidth = MarkerWidth,
        MarkerHeight = MarkerHeight,
        Class = Class,
        Style = Style,
        InteractionWidth = InteractionWidth,
        ZIndex = ZIndex,
        Data = Data,
    };
}

/// <summary>
/// A pending or completed connection between two handles, produced while the
/// user drags from a handle.
/// </summary>
public readonly record struct BlowConnection(
    string? Source,
    string? Target,
    string? SourceHandle,
    string? TargetHandle);
