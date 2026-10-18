namespace Bit.Blow;

/// <summary>
/// A node in the flow graph. This mirrors React Flow's node object.
/// Mutable so interactions (drag/select) can update it in place.
/// </summary>
public class BlowNode
{
    /// <summary>Unique id. Required and must be unique across the flow.</summary>
    public required string Id { get; set; }

    /// <summary>Position in flow coordinate space (top-left of the node).</summary>
    public BlowXYPosition Position { get; set; }

    /// <summary>
    /// Node type. Maps to a registered custom node renderer. "default", "input"
    /// and "output" are built in. Defaults to "default".
    /// </summary>
    public string Type { get; set; } = "default";

    /// <summary>Convenience label used by the built-in node renderers.</summary>
    public string? Label { get; set; }

    /// <summary>Arbitrary user payload available to custom node renderers.</summary>
    public object? Data { get; set; }

    /// <summary>Optional explicit width. When null the node is auto-sized and measured.</summary>
    public double? Width { get; set; }

    /// <summary>Optional explicit height. When null the node is auto-sized and measured.</summary>
    public double? Height { get; set; }

    /// <summary>Whether the node is part of the current selection. Maintained by the canvas as the user selects.</summary>
    public bool Selected { get; set; }

    /// <summary>When false the node cannot be moved, by pointer or by keyboard.</summary>
    public bool Draggable { get; set; } = true;

    /// <summary>When false the node is skipped by clicking, box selection and select-all.</summary>
    public bool Selectable { get; set; } = true;

    /// <summary>When false no connection can start from or end on any of this node's handles.</summary>
    public bool Connectable { get; set; } = true;

    /// <summary>When true the node is not rendered and takes no part in interaction or fit-view. The auto-layouts still place it.</summary>
    public bool Hidden { get; set; }

    /// <summary>When false the node is skipped by delete interactions (key press, <c>DeleteElementsAsync</c>).</summary>
    public bool Deletable { get; set; } = true;

    /// <summary>
    /// Side the built-in node renderers place the <em>source</em> handle on. Defaults to
    /// <see cref="BlowPosition.Bottom"/>; set to <see cref="BlowPosition.Right"/> for left-to-right layouts.
    /// </summary>
    public BlowPosition SourcePosition { get; set; } = BlowPosition.Bottom;

    /// <summary>
    /// Side the built-in node renderers place the <em>target</em> handle on. Defaults to
    /// <see cref="BlowPosition.Top"/>; set to <see cref="BlowPosition.Left"/> for left-to-right layouts.
    /// </summary>
    public BlowPosition TargetPosition { get; set; } = BlowPosition.Top;

    /// <summary>
    /// When true, the node body no longer initiates dragging; only a nested
    /// <c>&lt;BlowDragHandle&gt;</c> element can start a drag (React Flow's <c>dragHandle</c>).
    /// </summary>
    public bool UseCustomDragHandle { get; set; }

    /// <summary>When true the node is keyboard-focusable (tab order). Defaults to true.</summary>
    public bool Focusable { get; set; } = true;

    /// <summary>Accessible label announced by screen readers. Falls back to <see cref="Label"/>.</summary>
    public string? AriaLabel { get; set; }

    /// <summary>
    /// Optional id of a parent node. When set, <see cref="Position"/> is interpreted
    /// relative to the parent's top-left, enabling sub-flows / grouping.
    /// </summary>
    public string? ParentId { get; set; }

    /// <summary>
    /// When true and <see cref="ParentId"/> is set, the node is constrained so it cannot
    /// be dragged outside its parent's bounds (React Flow's <c>extent: 'parent'</c>).
    /// </summary>
    public bool ExtentParent { get; set; }

    /// <summary>
    /// Optional rectangle (in the node's own coordinate space - relative to its parent when it
    /// has one) that the node cannot be dragged outside of. Takes precedence over the canvas's
    /// <c>NodeExtent</c>; <see cref="ExtentParent"/> takes precedence over both.
    /// </summary>
    public BlowRect? Extent { get; set; }

    /// <summary>
    /// When true and the node has a parent, dragging the node past the parent's edge grows the
    /// parent to keep it enclosed (React Flow's <c>expandParent</c>). Ignored when
    /// <see cref="ExtentParent"/> is set, since that already pins the node inside.
    /// </summary>
    public bool ExpandParent { get; set; }

    /// <summary>Additional CSS class(es) applied to the node element.</summary>
    public string? Class { get; set; }

    /// <summary>Inline style applied to the node element.</summary>
    public string? Style { get; set; }

    /// <summary>Optional z-index override.</summary>
    public int? ZIndex { get; set; }

    /// <summary>
    /// Optional per-node origin (0,0 = top-left, 0.5,0.5 = center, 1,1 = bottom-right) that
    /// defines which point of the node <see cref="Position"/> refers to. Overrides the canvas default.
    /// </summary>
    public BlowXYPosition? Origin { get; set; }

    // ---- internal measured / transient state (maintained by the canvas) ----

    /// <summary>True while the user is dragging this node. Maintained by the canvas.</summary>
    public bool Dragging { get; internal set; }

    /// <summary>True while the user is resizing this node. Maintained by the canvas.</summary>
    public bool Resizing { get; internal set; }

    /// <summary>Measured dimensions in flow units. Empty until first measurement.</summary>
    public BlowDimensions Measured { get; internal set; } = BlowDimensions.Empty;

    /// <summary>
    /// Absolute position in flow space, resolved by walking parent nodes. Maintained by the
    /// canvas each render; equals <see cref="Position"/> for root nodes. Before the node has
    /// ever been laid out this falls back to <see cref="Position"/>, so helpers like
    /// <see cref="GetRect"/> and <c>BlowGraphUtils.GetNodesBounds</c> give sensible answers for
    /// nodes that have not been rendered yet.
    /// </summary>
    public BlowXYPosition AbsolutePosition
    {
        get => _absolutePosition ?? Position;
        internal set => _absolutePosition = value;
    }

    private BlowXYPosition? _absolutePosition;

    /// <summary>
    /// Measured handle anchors keyed by handle key (see <see cref="BlowHandleBounds"/>),
    /// expressed as offsets from the node's top-left in flow units.
    /// </summary>
    public Dictionary<string, BlowHandleBounds> Handles { get; } = new();

    /// <summary>
    /// The size the node is treated as having: its explicit <see cref="Width"/>/<see cref="Height"/>
    /// where given, else what was measured, else <see cref="DefaultWidth"/>/<see cref="DefaultHeight"/>
    /// so a node that has never been rendered still has usable bounds.
    /// </summary>
    public BlowDimensions EffectiveSize
    {
        get
        {
            var w = Width ?? (Measured.Width > 0 ? Measured.Width : DefaultWidth);
            var h = Height ?? (Measured.Height > 0 ? Measured.Height : DefaultHeight);
            return new BlowDimensions(w, h);
        }
    }

    /// <summary>The node's bounds in absolute flow space, using <see cref="EffectiveSize"/>.</summary>
    public BlowRect GetRect()
    {
        var size = EffectiveSize;
        return new BlowRect(AbsolutePosition.X, AbsolutePosition.Y, size.Width, size.Height);
    }

    /// <summary>
    /// Copies the node. <see cref="Data"/> is copied by reference - the clone shares whatever
    /// payload you attached. Useful for duplicate/copy-paste and for taking a snapshot that
    /// won't change under you as the user keeps editing.
    /// </summary>
    public BlowNode Clone(string? newId = null) => new()
    {
        // The resolved position is carried over only if the canvas has actually resolved one;
        // a node that was never laid out keeps following its own Position, as the original does.
        _absolutePosition = _absolutePosition,
        Id = newId ?? Id,
        Position = Position,
        Type = Type,
        Label = Label,
        Data = Data,
        Width = Width,
        Height = Height,
        Selected = Selected,
        Draggable = Draggable,
        Selectable = Selectable,
        Connectable = Connectable,
        Hidden = Hidden,
        Deletable = Deletable,
        SourcePosition = SourcePosition,
        TargetPosition = TargetPosition,
        UseCustomDragHandle = UseCustomDragHandle,
        Focusable = Focusable,
        AriaLabel = AriaLabel,
        ParentId = ParentId,
        ExtentParent = ExtentParent,
        Extent = Extent,
        ExpandParent = ExpandParent,
        Class = Class,
        Style = Style,
        ZIndex = ZIndex,
        Origin = Origin,
        Measured = Measured,
    };

    /// <summary>Width assumed for a node that has neither an explicit width nor a measurement.</summary>
    public const double DefaultWidth = 150;

    /// <summary>Height assumed for a node that has neither an explicit height nor a measurement.</summary>
    public const double DefaultHeight = 40;
}

/// <summary>
/// A measured handle anchor relative to its node's top-left, in flow units.
/// </summary>
/// <param name="HandleId">The handle's own id, or an empty string when it has none.</param>
/// <param name="Type">Whether the handle is a source or a target.</param>
/// <param name="Position">The side of the node the handle sits on.</param>
/// <param name="OffsetX">Horizontal offset of the handle's centre from the node's left edge.</param>
/// <param name="OffsetY">Vertical offset of the handle's centre from the node's top edge.</param>
/// <param name="ConnectableEnd">
/// False when the handle refuses to have connections dropped on it (its <c>IsConnectable</c> or
/// <c>IsConnectableEnd</c> is off), so snapping to the nearest handle must pass it over.
/// </param>
public readonly record struct BlowHandleBounds(
    string HandleId,
    BlowHandleType Type,
    BlowPosition Position,
    double OffsetX,
    double OffsetY,
    bool ConnectableEnd = true)
{
    /// <summary>Builds the dictionary key used to store/lookup a handle.</summary>
    public static string Key(BlowHandleType type, string? handleId) =>
        $"{type}:{handleId ?? string.Empty}";
}
