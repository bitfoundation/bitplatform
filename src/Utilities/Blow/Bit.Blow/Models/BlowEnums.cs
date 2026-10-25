namespace Bit.Blow;

/// <summary>
/// The side of a node a handle is anchored to. Also used to drive edge path direction.
/// </summary>
public enum BlowPosition
{
    /// <summary>The left-hand side of the node.</summary>
    Left,

    /// <summary>The top of the node.</summary>
    Top,

    /// <summary>The right-hand side of the node.</summary>
    Right,

    /// <summary>The bottom of the node.</summary>
    Bottom
}

/// <summary>
/// Whether a handle is a connection source or target (or can be both).
/// </summary>
public enum BlowHandleType
{
    /// <summary>A handle connections are drawn from: the source end of an edge.</summary>
    Source,

    /// <summary>A handle connections are dropped on: the target end of an edge.</summary>
    Target
}

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

/// <summary>
/// Keyboard modifier used to add to the current selection (multi-select).
/// </summary>
public enum BlowModifierKey
{
    /// <summary>The Shift key.</summary>
    Shift,

    /// <summary>The Control key.</summary>
    Control,

    /// <summary>The Alt (Option) key.</summary>
    Alt,

    /// <summary>The Meta key: Command on macOS, the Windows key elsewhere.</summary>
    Meta
}

/// <summary>
/// Controls which handle pairings are allowed when connecting.
/// </summary>
public enum BlowConnectionMode
{
    /// <summary>Only source-to-target connections are valid (default).</summary>
    Strict,

    /// <summary>Any handle can connect to any other handle.</summary>
    Loose
}

/// <summary>
/// Color theme applied to the flow.
/// </summary>
public enum BlowColorMode
{
    /// <summary>The light theme (the default).</summary>
    Light,

    /// <summary>The dark theme.</summary>
    Dark,

    /// <summary>Follows the OS <c>prefers-color-scheme</c> setting.</summary>
    System
}

/// <summary>
/// How box selection decides whether a node is selected.
/// </summary>
public enum BlowSelectionMode
{
    /// <summary>A node is selected if the selection box overlaps it at all.</summary>
    Partial,

    /// <summary>A node is selected only if it is fully enclosed by the selection box.</summary>
    Full
}

/// <summary>
/// Restricts the axis along which pan-on-scroll moves the viewport.
/// </summary>
public enum BlowPanOnScrollMode
{
    /// <summary>Scrolling pans on both axes.</summary>
    Free,

    /// <summary>Scrolling only pans horizontally.</summary>
    Horizontal,

    /// <summary>Scrolling only pans vertically.</summary>
    Vertical
}

/// <summary>
/// Built-in background pattern variants.
/// </summary>
public enum BlowBackgroundVariant
{
    /// <summary>A dot at each grid intersection (the default).</summary>
    Dots,

    /// <summary>A full grid of lines.</summary>
    Lines,

    /// <summary>A small cross at each grid intersection.</summary>
    Cross
}

/// <summary>
/// How much of the stacking order the canvas manages for you. Mirrors React Flow's
/// <c>zIndexMode</c>.
/// </summary>
public enum BlowZIndexMode
{
    /// <summary>
    /// Selected and dragged elements are lifted, and a child node is kept above the group
    /// container it sits in. The right default for sub-flows, where a child that renders
    /// behind its own parent is never what was meant.
    /// </summary>
    Auto,

    /// <summary>Only selection and dragging lift an element; nesting is left alone.</summary>
    Basic,

    /// <summary>
    /// Nothing is lifted automatically - the only stacking is the explicit <c>ZIndex</c> you
    /// set on a node or edge. Choose this when the layering is yours to own.
    /// </summary>
    Manual,
}

/// <summary>
/// Where a <c>BlowNodeToolbar</c> sits along the side of the node it is anchored to.
/// </summary>
public enum BlowToolbarAlign
{
    /// <summary>Centred on the side (the default).</summary>
    Center,

    /// <summary>Flush with the leading corner: the left edge for a top/bottom toolbar, the top edge for a left/right one.</summary>
    Start,

    /// <summary>Flush with the trailing corner.</summary>
    End,
}

/// <summary>
/// Arrowhead marker styles for edge ends, mirroring React Flow's MarkerType.
/// </summary>
public enum BlowMarkerType
{
    /// <summary>An open (stroked) arrowhead.</summary>
    Arrow,

    /// <summary>A closed (filled) arrowhead.</summary>
    ArrowClosed
}

/// <summary>
/// Whether the flow animates: viewport transitions that ease rather than jump, and the marching dash of animated
/// edges. Motion helps most people keep their bearings and makes some people unwell, which is what the operating
/// system's "reduce motion" setting is for.
/// </summary>
public enum BlowReducedMotion
{
    /// <summary>Follow the operating system's <c>prefers-reduced-motion</c> setting (the default).</summary>
    System,

    /// <summary>
    /// Never animate: viewport transitions jump straight to where they were going, and animated edges keep their
    /// dash but stop moving.
    /// </summary>
    Always,

    /// <summary>Always animate, whatever the operating system says.</summary>
    Never,
}
