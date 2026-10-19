using Microsoft.AspNetCore.Components;

namespace Bit.Blow;

/// <summary>
/// Internal contract exposed by <c>BlowCanvas</c> to its descendant components
/// (handles, nodes, edges) via a cascading value.
/// </summary>
public interface IBlowContext
{
    /// <summary>The canvas's current pan/zoom transform.</summary>
    BlowViewport Viewport { get; }

    /// <summary>True while the user is dragging a new connection from a handle.</summary>
    bool IsConnecting { get; }

    /// <summary>
    /// True when the in-progress connection started from this exact handle. The handle a
    /// connection is being dragged out of is not a place that connection can land, so without
    /// this it would answer "not a valid target" and be marked rejected - painting the one
    /// handle the user is actually using as the one thing they may not do.
    /// </summary>
    bool IsConnectionSource(string nodeId, string? handleId, BlowHandleType handleType);

    /// <summary>When true, connections are made by clicking handles rather than dragging.</summary>
    bool ConnectOnClick { get; }

    /// <summary>Globally enables starting/accepting connections.</summary>
    bool NodesConnectable { get; }

    /// <summary>Begins a connection drag originating from the given handle.</summary>
    void StartConnection(string nodeId, string? handleId, BlowHandleType handleType, BlowPosition position);

    /// <summary>
    /// Begins a connection drag from the given handle, capturing <paramref name="pointerId"/> on
    /// the pane so the gesture survives the pointer leaving the flow.
    /// </summary>
    void StartConnection(string nodeId, string? handleId, BlowHandleType handleType, BlowPosition position, long pointerId);

    /// <summary>
    /// Begins a connection drag, additionally telling the canvas where on screen the gesture
    /// started so the connection drag threshold has a real origin to measure from.
    /// </summary>
    void StartConnection(string nodeId, string? handleId, BlowHandleType handleType, BlowPosition position,
        long pointerId, double clientX, double clientY);

    /// <summary>Completes a connection drag when released over the given handle.</summary>
    void CompleteConnection(string nodeId, string? handleId, BlowHandleType handleType);

    /// <summary>Marks the given handle as a hover candidate while connecting.</summary>
    void SetConnectionHover(string nodeId, string? handleId, BlowHandleType handleType, bool isOver);

    /// <summary>Returns true when the given handle is a valid drop target for the in-progress connection.</summary>
    bool IsValidConnectionTarget(string nodeId, string? handleId, BlowHandleType handleType);

    /// <summary>
    /// Describes the connection that completing on the given handle would produce, normalised
    /// so source always refers to the source-side handle.
    /// </summary>
    BlowConnection DescribeConnection(string nodeId, string? handleId, BlowHandleType handleType);

    // ---- viewport controls (used by Controls / BlowMiniMap) ----

    /// <summary>The canvas's live node list, for overlays that draw from it (minimap, devtools).</summary>
    IReadOnlyList<BlowNode> CurrentNodes { get; }

    /// <summary>Current size of the pane (the flow's outer container) in screen pixels.</summary>
    BlowDimensions PaneSize { get; }

    /// <summary>Lower zoom bound configured on the canvas.</summary>
    double MinZoom { get; }

    /// <summary>Upper zoom bound configured on the canvas.</summary>
    double MaxZoom { get; }

    /// <summary>Zooms in one step about the pane centre.</summary>
    void ZoomIn();

    /// <summary>Zooms out one step about the pane centre.</summary>
    void ZoomOut();

    /// <summary>Pans and zooms so the whole graph fits the pane.</summary>
    void FitView();

    /// <summary>Applies a viewport, clamped to the canvas's zoom range and translate extent.</summary>
    void SetViewport(BlowViewport viewport);

    // ---- used by BlowNodeResizer / BlowNodeToolbar ----

    /// <summary>Requests a re-render of the canvas; optionally re-measures node/handle geometry.</summary>
    void Refresh(bool remeasure);

    /// <summary>Captures the pointer on the given element so a drag continues outside it.</summary>
    void CapturePointer(ElementReference element, long pointerId);

    /// <summary>Begins a node resize gesture (called by <c>BlowNodeResizer</c>).</summary>
    void StartResize(string nodeId, BlowResizeDirection direction,
        double clientX, double clientY, double minWidth, double minHeight, bool keepAspectRatio);

    /// <summary>
    /// Begins a node resize gesture with size bounds, capturing <paramref name="pointerId"/> on
    /// the pane so the gesture survives the pointer leaving the flow.
    /// </summary>
    void StartResize(string nodeId, BlowResizeDirection direction,
        double clientX, double clientY, double minWidth, double minHeight, bool keepAspectRatio,
        double maxWidth, double maxHeight, long pointerId);

    /// <summary>Begins dragging a node from a custom <c>BlowDragHandle</c> element.</summary>
    void BeginNodeDrag(BlowNode node, Microsoft.AspNetCore.Components.Web.PointerEventArgs e);

    /// <summary>Registers content to be rendered inside the (transformed) viewport layer.</summary>
    void RegisterViewportPortal(object key, RenderFragment fragment);

    /// <summary>Removes previously registered viewport-portal content.</summary>
    void UnregisterViewportPortal(object key);

    // ---- handle registry (used by Handle for per-handle validation / callbacks) ----

    /// <summary>
    /// Registers a handle's per-handle validation predicate and connect callback so the canvas
    /// can consult them while a connection is dragged and after one is made. Registering the
    /// same key again replaces the previous registration.
    /// </summary>
    void RegisterHandle(string key, BlowHandleRegistration registration);

    /// <summary>Removes a previously registered handle.</summary>
    void UnregisterHandle(string key);

    /// <summary>
    /// Removes a handle's registration, but only while <paramref name="registration"/> is still
    /// the one held for <paramref name="key"/> - so a handle being torn down cannot take out the
    /// entry of the handle that has just replaced it.
    /// </summary>
    void UnregisterHandle(string key, BlowHandleRegistration registration) => UnregisterHandle(key);
}

/// <summary>
/// The per-handle hooks a <c>BlowHandle</c> hands to the canvas: an optional validation predicate
/// consulted before the handle is offered as a drop target, and a callback raised once a
/// connection touching the handle has been made.
/// </summary>
public sealed class BlowHandleRegistration
{
    /// <summary>Extra validation for connections landing on (or starting from) this handle.</summary>
    public Func<BlowConnection, bool>? IsValidConnection { get; init; }

    /// <summary>Raised when a connection involving this handle is completed.</summary>
    public EventCallback<BlowConnection> OnConnect { get; init; }

    /// <summary>Builds the registry key identifying one handle of one node.</summary>
    public static string Key(string nodeId, BlowHandleType type, string? handleId) =>
        $"{nodeId}\u001f{type}\u001f{handleId ?? string.Empty}";
}

/// <summary>
/// Cascaded down to the content of a single node so nested handles know which
/// node they belong to.
/// </summary>
public sealed class BlowNodeContext
{
    /// <summary>
    /// The node this content belongs to. Settable because the context is cascaded as a fixed
    /// value: when the node instance behind an id is replaced (a restore, an undo), the renderer
    /// points the same context at the new instance rather than handing out a new context the
    /// content would never be told about.
    /// </summary>
    public required BlowNode Node { get; set; }
}
