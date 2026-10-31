using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;

namespace Bit.Blow;

/// <summary>
/// The flow canvas: a pannable, zoomable surface that renders a graph of nodes and edges and
/// owns every interaction with it - dragging, selecting, connecting, resizing and the keyboard.
/// </summary>
/// <remarks>
/// <para>
/// Give it a <see cref="Nodes"/> list (and usually an <see cref="Edges"/> one) and it renders
/// them; it mutates those lists in place as the user edits and raises <see cref="NodesChanged"/>
/// and <see cref="EdgesChanged"/> so a caller can persist the result. Overlays -
/// <c>BlowBackground</c>, <c>BlowControls</c>, <c>BlowMiniMap</c>, <c>BlowPanel</c> - go in
/// <see cref="ChildContent"/>; node and edge appearance is replaced with
/// <see cref="NodeTemplate"/> and <see cref="EdgeTemplate"/>.
/// </para>
/// <para>
/// Capture it with <c>@ref</c> for the imperative API: <see cref="FitView()"/>,
/// <see cref="SetViewport(BlowViewport)"/>, <see cref="ToObject"/>, the clipboard and so on. It is
/// the Blazor counterpart of React Flow's <c>&lt;ReactFlow&gt;</c> plus <c>useReactFlow()</c>.
/// </para>
/// </remarks>
public partial class BlowCanvas : IBlowContext, IAsyncDisposable
{
    [Inject] private IJSRuntime JS { get; set; } = default!;

    // ---- public API ----

    /// <summary>The nodes to render. Mutated in place during drag/select.</summary>
    [Parameter, EditorRequired] public List<BlowNode> Nodes { get; set; } = [];

    /// <summary>The edges to render.</summary>
    [Parameter] public List<BlowEdge> Edges { get; set; } = [];

    /// <summary>Optional custom renderer for node content. Switch on <c>node.Type</c> inside.</summary>
    [Parameter] public RenderFragment<BlowNode>? NodeTemplate { get; set; }

    /// <summary>
    /// Optional custom renderer for edges. Receives an <see cref="BlowEdgeContext"/> with the
    /// computed geometry and endpoints; render SVG (path, labels, etc.). Switch on
    /// <c>context.Edge.Type</c> or custom data inside. When null, the built-in edge is drawn.
    /// </summary>
    [Parameter] public RenderFragment<BlowEdgeContext>? EdgeTemplate { get; set; }

    /// <summary>
    /// Optional HTML renderer for edge labels, drawn in a non-transformed overlay so labels
    /// stay a constant screen size (React Flow's EdgeLabelRenderer). Positioned at the edge's
    /// label point. When set, it is used instead of the built-in SVG label.
    /// </summary>
    [Parameter] public RenderFragment<BlowEdgeContext>? EdgeLabelTemplate { get; set; }

    /// <summary>Overlay content: place <c>BlowBackground</c>, <c>BlowControls</c>, <c>BlowMiniMap</c>, <c>BlowPanel</c> here.</summary>
    [Parameter] public RenderFragment? ChildContent { get; set; }

    /// <summary>Raised when the user completes a connection, with the wiring that was created.</summary>
    [Parameter] public EventCallback<BlowConnection> OnConnect { get; set; }

    /// <summary>Raised when the user begins dragging a connection from a handle.</summary>
    [Parameter] public EventCallback<BlowConnectionStartInfo> OnConnectStart { get; set; }

    /// <summary>
    /// Raised when a connection drag ends, whether it produced an edge or not. The payload
    /// carries the final flow/screen position and whether the drop landed on a valid handle,
    /// enabling interactions like adding a node where the connection was released.
    /// </summary>
    [Parameter] public EventCallback<BlowConnectEndInfo> OnConnectEnd { get; set; }

    /// <summary>
    /// Raised on connection start, on each pointer move while connecting, and once with
    /// <c>null</c> when the connection ends. Mirrors React Flow's <c>useConnection</c> updates.
    /// </summary>
    [Parameter] public EventCallback<BlowConnectionLineContext?> OnConnectionChange { get; set; }

    /// <summary>
    /// Optional custom renderer for the line drawn while creating a connection. Receives the
    /// live <see cref="BlowConnectionLineContext"/>; render SVG. When null, a default path is drawn.
    /// </summary>
    [Parameter] public RenderFragment<BlowConnectionLineContext>? ConnectionLineTemplate { get; set; }

    /// <summary>Globally enables dragging existing edge endpoints to new handles.</summary>
    [Parameter] public bool EdgesReconnectable { get; set; } = true;

    /// <summary>
    /// Radius in screen pixels of the grab area around a selected edge's endpoints. The visible
    /// dot stays small; this is how close the pointer has to come to pick the endpoint up.
    /// Mirrors React Flow's <c>reconnectRadius</c>.
    /// </summary>
    [Parameter] public double ReconnectRadius { get; set; } = 10;

    /// <summary>Raised when an existing edge is reconnected to a new handle.</summary>
    [Parameter] public EventCallback<BlowEdgeReconnect> OnReconnect { get; set; }

    /// <summary>Raised when the user grabs one end of an existing edge to reconnect it.</summary>
    [Parameter] public EventCallback<BlowEdgeReconnectStart> OnReconnectStart { get; set; }

    /// <summary>
    /// Raised when a reconnect gesture finishes, whether or not it rewired the edge.
    /// A common use is deleting the edge when it was dropped on empty space.
    /// </summary>
    [Parameter] public EventCallback<BlowEdgeReconnectEnd> OnReconnectEnd { get; set; }

    /// <summary>
    /// Optional predicate to validate a prospective connection while the user drags.
    /// Return false to reject; rejected targets are highlighted differently.
    /// </summary>
    [Parameter] public Func<BlowConnection, bool>? IsValidConnection { get; set; }
    /// <summary>
    /// Raised once for every completed edit, with what kind of edit it was, where it came from, and the nodes and edges
    /// it added, changed or removed - at the end of a drag rather than on every move. The place to persist, validate,
    /// or send an operation to a collaboration server; see <see cref="BlowGraphChange"/>.
    /// </summary>
    [Parameter] public EventCallback<BlowGraphChange> OnGraphChanged { get; set; }

    /// <summary>Raised after the canvas has changed the node list, for persistence or two-way binding.</summary>
    [Parameter] public EventCallback<List<BlowNode>> NodesChanged { get; set; }
    /// <summary>Raised after the canvas has changed the edge list, for persistence or two-way binding.</summary>
    [Parameter] public EventCallback<List<BlowEdge>> EdgesChanged { get; set; }

    /// <summary>Raised with the nodes that were just deleted.</summary>
    [Parameter] public EventCallback<IReadOnlyList<BlowNode>> OnNodesDelete { get; set; }

    /// <summary>Raised with the edges that were just deleted.</summary>
    [Parameter] public EventCallback<IReadOnlyList<BlowEdge>> OnEdgesDelete { get; set; }

    /// <summary>Raised after any deletion with the removed nodes and edges.</summary>
    [Parameter] public EventCallback<BlowSelectionChange> OnDelete { get; set; }
    /// <summary>Raised when a node is clicked. A drag that travelled past NodeClickDistance is not a click.</summary>
    [Parameter] public EventCallback<BlowNode> OnNodeClick { get; set; }
    /// <summary>Raised when an edge is clicked.</summary>
    [Parameter] public EventCallback<BlowEdge> OnEdgeClick { get; set; }
    /// <summary>Raised when a pan or zoom settles, with the resulting viewport.</summary>
    [Parameter] public EventCallback<BlowViewport> OnViewportChanged { get; set; }

    /// <summary>
    /// Optional controlled viewport. When set, the flow adopts the value whenever it changes;
    /// pair it with <see cref="OnViewportChanged"/> to drive pan/zoom entirely from your own
    /// state. Leave null (the default) for an uncontrolled viewport.
    /// </summary>
    [Parameter] public BlowViewport? Viewport { get; set; }

    // ---- richer event matrix (mirrors React Flow) ----

    /// <summary>Raised when a node is double-clicked.</summary>
    [Parameter] public EventCallback<BlowNodeMouseEvent> OnNodeDoubleClick { get; set; }
    /// <summary>Raised when a node is right-clicked. The browser menu is suppressed.</summary>
    [Parameter] public EventCallback<BlowNodeMouseEvent> OnNodeContextMenu { get; set; }
    /// <summary>Raised when the pointer enters a node.</summary>
    [Parameter] public EventCallback<BlowNodeMouseEvent> OnNodeMouseEnter { get; set; }
    /// <summary>Raised as the pointer moves over a node. Only listened for when a handler is supplied.</summary>
    [Parameter] public EventCallback<BlowNodeMouseEvent> OnNodeMouseMove { get; set; }
    /// <summary>Raised when the pointer leaves a node.</summary>
    [Parameter] public EventCallback<BlowNodeMouseEvent> OnNodeMouseLeave { get; set; }
    /// <summary>Raised when a node drag begins, with the node the pointer grabbed.</summary>
    [Parameter] public EventCallback<BlowNode> OnNodeDragStart { get; set; }
    /// <summary>Raised on every pointer move while a node is dragged.</summary>
    [Parameter] public EventCallback<BlowNode> OnNodeDrag { get; set; }
    /// <summary>Raised when a node drag ends. The usual place to capture an undo step.</summary>
    [Parameter] public EventCallback<BlowNode> OnNodeDragStop { get; set; }

    /// <summary>Raised when an edge is double-clicked.</summary>
    [Parameter] public EventCallback<BlowEdgeMouseEvent> OnEdgeDoubleClick { get; set; }
    /// <summary>Raised when an edge is right-clicked. The browser menu is suppressed.</summary>
    [Parameter] public EventCallback<BlowEdgeMouseEvent> OnEdgeContextMenu { get; set; }
    /// <summary>Raised when the pointer enters an edge.</summary>
    [Parameter] public EventCallback<BlowEdgeMouseEvent> OnEdgeMouseEnter { get; set; }
    /// <summary>Raised as the pointer moves over an edge. Only listened for when a handler is supplied.</summary>
    [Parameter] public EventCallback<BlowEdgeMouseEvent> OnEdgeMouseMove { get; set; }
    /// <summary>Raised when the pointer leaves an edge.</summary>
    [Parameter] public EventCallback<BlowEdgeMouseEvent> OnEdgeMouseLeave { get; set; }

    /// <summary>Raised when the empty pane is clicked. A drag that panned the viewport is not a click.</summary>
    [Parameter] public EventCallback<MouseEventArgs> OnPaneClick { get; set; }
    /// <summary>Raised when the empty pane is right-clicked.</summary>
    [Parameter] public EventCallback<MouseEventArgs> OnPaneContextMenu { get; set; }
    /// <summary>Raised as the pointer moves over the pane. Only listened for when a handler is supplied.</summary>
    [Parameter] public EventCallback<MouseEventArgs> OnPaneMouseMove { get; set; }
    /// <summary>Raised when the pointer enters the pane.</summary>
    [Parameter] public EventCallback<MouseEventArgs> OnPaneMouseEnter { get; set; }
    /// <summary>Raised when the pointer leaves the pane.</summary>
    [Parameter] public EventCallback<MouseEventArgs> OnPaneMouseLeave { get; set; }

    /// <summary>Raised for every wheel event over the pane, before pan/zoom handling.</summary>
    [Parameter] public EventCallback<WheelEventArgs> OnPaneScroll { get; set; }

    /// <summary>Raised when a node resize gesture starts.</summary>
    [Parameter] public EventCallback<BlowNode> OnNodeResizeStart { get; set; }

    /// <summary>Raised on every pointer move during a node resize.</summary>
    [Parameter] public EventCallback<BlowNode> OnNodeResize { get; set; }

    /// <summary>Raised when a node resize gesture ends.</summary>
    [Parameter] public EventCallback<BlowNode> OnNodeResizeEnd { get; set; }

    /// <summary>Raised when the user starts panning or zooming the viewport.</summary>
    [Parameter] public EventCallback<BlowViewport> OnMoveStart { get; set; }

    /// <summary>Raised continuously while the viewport is panned or zoomed.</summary>
    [Parameter] public EventCallback<BlowViewport> OnMove { get; set; }

    /// <summary>Raised when a pan/zoom gesture ends.</summary>
    [Parameter] public EventCallback<BlowViewport> OnMoveEnd { get; set; }

    /// <summary>Raised when the user begins drawing a selection box on the pane.</summary>
    [Parameter] public EventCallback<PointerEventArgs> OnSelectionStart { get; set; }

    /// <summary>Raised when a selection-box gesture ends.</summary>
    [Parameter] public EventCallback<PointerEventArgs> OnSelectionEnd { get; set; }

    /// <summary>Raised whenever the set of selected nodes/edges changes.</summary>
    [Parameter] public EventCallback<BlowSelectionChange> OnSelectionChange { get; set; }

    /// <summary>
    /// Raised when a drag begins that moves more than one node, with everything that is moving.
    /// Nudging a single node raises only <see cref="OnNodeDragStart"/>; moving a marquee
    /// selection raises both, so a listener can treat the group as one edit.
    /// </summary>
    [Parameter] public EventCallback<BlowSelectionMouseEvent> OnSelectionDragStart { get; set; }

    /// <summary>Raised on every pointer move while a multi-node selection is being dragged.</summary>
    [Parameter] public EventCallback<BlowSelectionMouseEvent> OnSelectionDrag { get; set; }

    /// <summary>Raised when a multi-node drag ends.</summary>
    [Parameter] public EventCallback<BlowSelectionMouseEvent> OnSelectionDragStop { get; set; }

    /// <summary>
    /// Raised when a node that is part of a multi-node selection is right-clicked, carrying the
    /// whole selection. The usual use is a context menu that acts on everything selected rather
    /// than only on the node under the cursor.
    /// </summary>
    [Parameter] public EventCallback<BlowSelectionMouseEvent> OnSelectionContextMenu { get; set; }

    /// <summary>
    /// When true, dragging on the pane background draws a selection box instead of panning.
    /// Box selection is also available while <see cref="SelectionKey"/> is held, regardless of
    /// this setting.
    /// </summary>
    [Parameter] public bool SelectionOnDrag { get; set; }

    /// <summary>
    /// Key that, while held, turns a pane drag into a box selection. Defaults to Shift, matching
    /// React Flow's <c>selectionKeyCode</c>. Set to null to require <see cref="SelectionOnDrag"/>.
    /// </summary>
    [Parameter] public BlowModifierKey? SelectionKey { get; set; } = BlowModifierKey.Shift;

    /// <summary>Smallest zoom factor the viewport may reach.</summary>
    [Parameter] public double MinZoom { get; set; } = 0.2;
    /// <summary>Largest zoom factor the viewport may reach.</summary>
    [Parameter] public double MaxZoom { get; set; } = 2.5;

    /// <summary>
    /// Radius in screen pixels around the pointer within which a released connection
    /// snaps to the nearest valid handle. Set to 0 to require releasing exactly on a handle.
    /// </summary>
    [Parameter] public double ConnectionRadius { get; set; } = 20;

    /// <summary>
    /// When true, connections are made by clicking a source handle and then clicking a
    /// target handle, instead of (or in addition to) dragging.
    /// </summary>
    [Parameter] public bool ConnectOnClick { get; set; }

    /// <summary>
    /// The key that starts a connection from the focused node - and, while one is being made, picks another port on
    /// the focused node. Tab to the node to connect to and press Enter; Escape cancels. Null turns connecting from the
    /// keyboard off, as do <see cref="DisableKeyboardA11y"/> and <see cref="NodesConnectable"/>.
    /// </summary>
    /// <remarks>Ignored with Ctrl, Cmd or Alt held, and while focus is in a text field inside the node.</remarks>
    [Parameter] public string? ConnectionKey { get; set; } = "c";

    /// <summary>
    /// A validation result to show on the graph - from <see cref="BlowGraphValidator"/> or rules of your own. Nodes,
    /// edges and ports with an error or a warning are marked by more than colour (a badge and a ring on a node, a
    /// dashed line for an edge), and a screen reader hears the problems as part of the element's description. Null
    /// marks nothing.
    /// </summary>
    [Parameter] public BlowValidationResult? Validation { get; set; }

    /// <summary>
    /// Where each node is in a run of the graph - a workflow executing, a build, a state machine - keyed by node id.
    /// A node with a state gets the class <c>bit-blow-run-{status}</c> and a badge whose shape differs by status (a
    /// ring while it runs, a tick, a cross), and a screen reader hears the status and its message with the node. Pass
    /// a new dictionary, or the same one again, as the run moves on; nodes not in it show nothing.
    /// </summary>
    [Parameter] public IReadOnlyDictionary<string, BlowRunState>? RunStates { get; set; }

    /// <summary>
    /// What routes <see cref="BlowEdgeType.Routed"/> edges round the nodes in their way: a <see cref="BlowRoutingOptions"/>
    /// to tune the built-in orthogonal router, or an <see cref="IBlowEdgeRouter"/> of the app's own to replace it. The
    /// built-in router with its defaults when null.
    /// </summary>
    [Parameter] public IBlowEdgeRouter? Routing { get; set; }

    /// <summary>Which modifier key adds to the current selection (multi-select). Defaults to Shift.</summary>
    [Parameter] public BlowModifierKey MultiSelectionKey { get; set; } = BlowModifierKey.Shift;
    /// <summary>Path type given to edges the user creates, unless BlowDefaultEdgeOptions overrides it.</summary>
    [Parameter] public BlowEdgeType DefaultEdgeType { get; set; } = BlowEdgeType.Bezier;

    /// <summary>Default values applied to edges created by user interaction.</summary>
    [Parameter] public BlowDefaultEdgeOptions? DefaultEdgeOptions { get; set; }

    /// <summary>Controls which handle pairings are valid when connecting (Strict vs Loose).</summary>
    [Parameter] public BlowConnectionMode ConnectionMode { get; set; } = BlowConnectionMode.Strict;

    /// <summary>Path style used for the in-progress connection line while dragging.</summary>
    [Parameter] public BlowEdgeType ConnectionLineType { get; set; } = BlowEdgeType.Bezier;

    /// <summary>
    /// Inline style applied to the default in-progress connection line, e.g.
    /// <c>"stroke:#f6ab6c;stroke-width:3;stroke-dasharray:5 5"</c>. Ignored when
    /// <see cref="ConnectionLineTemplate"/> draws the line instead.
    /// </summary>
    [Parameter] public string? ConnectionLineStyle { get; set; }

    /// <summary>Extra CSS class(es) for the default in-progress connection line.</summary>
    [Parameter] public string? ConnectionLineClass { get; set; }

    /// <summary>
    /// Arrowhead colour used by edges that do not name one of their own. When null (the default)
    /// an arrowhead takes its edge's stroke colour. Mirrors React Flow's <c>defaultMarkerColor</c>.
    /// </summary>
    [Parameter] public string? DefaultMarkerColor { get; set; }

    /// <summary>Color theme applied to the flow.</summary>
    [Parameter] public BlowColorMode ColorMode { get; set; } = BlowColorMode.Light;

    /// <summary>
    /// Whether viewport transitions ease and animated edges move. <see cref="BlowReducedMotion.System"/> (the
    /// default) follows the operating system's "reduce motion" setting: with it on, every animated viewport helper
    /// jumps straight to its destination and animated edges stop moving while keeping their dash.
    /// </summary>
    [Parameter] public BlowReducedMotion ReducedMotion { get; set; } = BlowReducedMotion.System;

    /// <summary>
    /// Default origin (0,0 = top-left, 0.5,0.5 = center) defining which point of a node its
    /// Position refers to. Individual nodes can override via <c>BlowNode.Origin</c>.
    /// </summary>
    [Parameter] public BlowXYPosition NodeOrigin { get; set; } = new(0, 0);

    /// <summary>How box selection decides membership: Partial (any overlap) or Full (fully enclosed).</summary>
    [Parameter] public BlowSelectionMode SelectionMode { get; set; } = BlowSelectionMode.Partial;

    /// <summary>
    /// When true, box selection also picks up edges whose two endpoints are both selected,
    /// so dragging a box round a cluster selects the sub-graph rather than only its nodes.
    /// </summary>
    [Parameter] public bool SelectEdgesOnSelectionBox { get; set; } = true;

    /// <summary>Raises a node above its peers while it is selected or dragged.</summary>
    [Parameter] public bool ElevateNodesOnSelect { get; set; } = true;

    /// <summary>Raises an edge above its peers while it is selected.</summary>
    [Parameter] public bool ElevateEdgesOnSelect { get; set; }

    /// <summary>
    /// How much of the stacking order the canvas manages. <see cref="BlowZIndexMode.Manual"/>
    /// hands it back to you entirely, ignoring <see cref="ElevateNodesOnSelect"/> and
    /// <see cref="ElevateEdgesOnSelect"/>.
    /// </summary>
    [Parameter] public BlowZIndexMode ZIndexMode { get; set; } = BlowZIndexMode.Auto;

    /// <summary>
    /// When true (the default), pressing a node selects it immediately. Turn it off to make a
    /// plain drag move the node without changing the selection, so a marquee selection survives
    /// nudging one of its members.
    /// </summary>
    [Parameter] public bool SelectNodesOnDrag { get; set; } = true;

    /// <summary>
    /// Pointer travel in screen pixels before the in-progress connection line is drawn. Keeps a
    /// click on a handle from flashing a line. Mirrors React Flow's <c>connectionDragThreshold</c>.
    /// </summary>
    [Parameter] public double ConnectionDragThreshold { get; set; } = 1;

    /// <summary>Restricts pan-on-scroll to a single axis when set.</summary>
    [Parameter] public BlowPanOnScrollMode PanOnScrollMode { get; set; } = BlowPanOnScrollMode.Free;

    /// <summary>
    /// When true, dragging a node past the edge of the pane scrolls the viewport to follow,
    /// so nodes can be moved beyond the currently visible area.
    /// </summary>
    [Parameter] public bool AutoPanOnNodeDrag { get; set; } = true;

    /// <summary>Same as <see cref="AutoPanOnNodeDrag"/>, for dragging a connection.</summary>
    [Parameter] public bool AutoPanOnConnect { get; set; } = true;

    /// <summary>
    /// When true, tabbing to a node that is off screen pans the viewport to bring it into view.
    /// Without this a keyboard user can move focus into a node they cannot see, which is the
    /// point at which tab navigation stops being usable. Mirrors React Flow's
    /// <c>autoPanOnNodeFocus</c>.
    /// </summary>
    [Parameter] public bool AutoPanOnNodeFocus { get; set; } = true;

    /// <summary>
    /// Milliseconds the viewport takes to travel when <see cref="AutoPanOnNodeFocus"/> brings a
    /// focused node into view. Zero jumps straight there.
    /// </summary>
    [Parameter] public int AutoPanOnNodeFocusDuration { get; set; } = 200;

    /// <summary>Screen pixels the viewport travels per auto-pan step.</summary>
    [Parameter] public double AutoPanSpeed { get; set; } = 15;

    /// <summary>
    /// Distance in screen pixels from the pane border at which auto-panning kicks in.
    /// </summary>
    [Parameter] public double AutoPanMargin { get; set; } = 30;

    /// <summary>Hides the small "bit Blow" attribution badge when true.</summary>
    [Parameter] public bool HideAttribution { get; set; }

    /// <summary>Corner the attribution badge is anchored to. Defaults to bottom-right.</summary>
    [Parameter] public BlowPanelPosition AttributionPosition { get; set; } = BlowPanelPosition.BottomRight;

    /// <summary>
    /// When true (the default) wheel events over the flow are consumed so the page behind it
    /// doesn't scroll. Set to false to let the page scroll normally. A wheel the flow does not
    /// act on - a plain scroll while <see cref="ZoomActivationKey"/> is not held, or while
    /// <see cref="ZoomOnScroll"/> is off - always reaches the page.
    /// </summary>
    [Parameter] public bool PreventScrolling { get; set; } = true;

    /// <summary>Accessible name for the flow region. Announced by screen readers.</summary>
    [Parameter] public string AriaLabel { get; set; } = "Flow diagram";

    /// <summary>
    /// The strings the flow announces and labels itself with. Replace it to localize the flow
    /// or to reword the announcements; see <see cref="BlowAriaLabels"/> for the defaults.
    /// </summary>
    [Parameter] public BlowAriaLabels AriaLabels { get; set; } = BlowAriaLabels.Default;

    /// <summary>
    /// CSS class marking an element inside a node that must not start a drag - the standard
    /// escape hatch for inputs, sliders and buttons living inside custom nodes. Mirrors React
    /// Flow's <c>noDragClassName</c>. Set to null to disable the guard.
    /// </summary>
    [Parameter] public string? NoDragClass { get; set; } = "nodrag";

    /// <summary>
    /// CSS class marking an element that must not pan the viewport when dragged.
    /// Mirrors React Flow's <c>noPanClassName</c>. Set to null to disable the guard.
    /// </summary>
    [Parameter] public string? NoPanClass { get; set; } = "nopan";

    /// <summary>
    /// CSS class marking an element that keeps its own wheel events - so a scrollable panel
    /// inside the flow scrolls instead of zooming the canvas. Mirrors React Flow's
    /// <c>noWheelClassName</c>. Set to null to disable the guard.
    /// </summary>
    [Parameter] public string? NoWheelClass { get; set; } = "nowheel";

    /// <summary>
    /// Raised when the flow notices something wrong with the graph it was handed - a duplicate
    /// node id, an edge pointing at a node that isn't there. Each distinct problem is reported
    /// once rather than on every render.
    /// </summary>
    [Parameter] public EventCallback<BlowError> OnError { get; set; }

    /// <summary>
    /// Raised once every node has been measured, and again whenever that becomes true after a
    /// node was added. Layout algorithms need real node sizes, so this is the cue to run one.
    /// </summary>
    [Parameter] public EventCallback OnNodesInitialized { get; set; }

    /// <summary>
    /// Initial viewport for an uncontrolled flow, applied on first render. Ignored when
    /// <see cref="Viewport"/> is set (which controls the viewport outright) or when
    /// <see cref="FitViewOnInit"/> wins by fitting the graph instead.
    /// </summary>
    [Parameter] public BlowViewport? DefaultViewport { get; set; }

    /// <summary>Raised once after the flow is first rendered and measured; receives the canvas for imperative use.</summary>
    [Parameter] public EventCallback<BlowCanvas> OnInit { get; set; }

    /// <summary>
    /// Optional veto for deletions. Receives the nodes and edges about to be deleted;
    /// return false to cancel the deletion. Mirrors React Flow's <c>onBeforeDelete</c>.
    /// </summary>
    [Parameter] public Func<IReadOnlyList<BlowNode>, IReadOnlyList<BlowEdge>, bool>? OnBeforeDelete { get; set; }
    /// <summary>
    /// When true, the graph is fitted to the pane once, after the first measurement that finds
    /// something to fit - so a graph loaded after the canvas first rendered is still fitted.
    /// </summary>
    [Parameter] public bool FitViewOnInit { get; set; }

    /// <summary>
    /// Options used by the initial <see cref="FitViewOnInit"/> fit. Ignored when
    /// <see cref="FitViewOnInit"/> is false.
    /// </summary>
    [Parameter] public BlowFitViewOptions? FitViewOptions { get; set; }

    /// <summary>
    /// When true, only nodes/edges intersecting the current viewport (plus a margin) are rendered.
    /// Improves performance on large graphs. Unmeasured nodes are always rendered once so they can be sized.
    /// </summary>
    [Parameter] public bool OnlyRenderVisibleElements { get; set; }
    /// <summary>When true, dragging the pane background pans the viewport.</summary>
    [Parameter] public bool PanOnDrag { get; set; } = true;

    /// <summary>
    /// Mouse buttons that pan the viewport when dragged on the pane: 0 = left, 1 = middle,
    /// 2 = right. Defaults to left only. Middle-button panning is a common addition for flows
    /// that use left-drag for box selection.
    /// </summary>
    [Parameter] public IReadOnlyList<int> PanButtons { get; set; } = [0];

    /// <summary>
    /// Key that, while held, turns a left-drag anywhere on the pane into a pan even when
    /// <see cref="SelectionOnDrag"/> is on. Set to null to disable. Defaults to the space bar.
    /// </summary>
    [Parameter] public string? PanActivationKey { get; set; } = " ";

    /// <summary>When true, the mouse wheel zooms the viewport (unless PanOnScroll takes it over).</summary>
    [Parameter] public bool ZoomOnScroll { get; set; } = true;

    /// <summary>When true, mouse-wheel/trackpad scrolling pans the viewport; pinch (Ctrl+wheel) still zooms.</summary>
    [Parameter] public bool PanOnScroll { get; set; }

    /// <summary>Pan speed multiplier applied to wheel deltas when <see cref="PanOnScroll"/> is enabled.</summary>
    [Parameter] public double PanOnScrollSpeed { get; set; } = 0.5;

    /// <summary>When true, double-clicking the pane zooms in (Shift+double-click zooms out).</summary>
    [Parameter] public bool ZoomOnDoubleClick { get; set; } = true;

    /// <summary>
    /// When true, a two-finger pinch on the pane zooms the viewport about the midpoint between
    /// the fingers. This is the only way to zoom on a touch device, so it defaults to on.
    /// </summary>
    [Parameter] public bool ZoomOnPinch { get; set; } = true;
    /// <summary>Globally enables dragging nodes. Individual nodes can still opt out.</summary>
    [Parameter] public bool NodesDraggable { get; set; } = true;

    /// <summary>Globally enables starting/accepting connections. Individual nodes can still opt out.</summary>
    [Parameter] public bool NodesConnectable { get; set; } = true;

    /// <summary>Globally enables selecting nodes and edges. Individual elements can still opt out.</summary>
    [Parameter] public bool ElementsSelectable { get; set; } = true;
    /// <summary>Enables the DeleteKeys shortcut for removing the selection.</summary>
    [Parameter] public bool DeleteKeyEnabled { get; set; } = true;

    /// <summary>
    /// The keys that delete the selection. Mirrors React Flow's <c>deleteKeyCode</c>; set it to
    /// just <c>Delete</c> when Backspace should always mean "go back" instead.
    /// Ignored when <see cref="DeleteKeyEnabled"/> is false.
    /// </summary>
    [Parameter] public IReadOnlyList<string> DeleteKeys { get; set; } = ["Delete", "Backspace"];

    /// <summary>
    /// Key that must be held for the wheel to zoom. Null (the default) means the wheel zooms
    /// unconditionally; set it to make plain scrolling pass through to the page.
    /// Mirrors React Flow's <c>zoomActivationKeyCode</c>.
    /// </summary>
    [Parameter] public BlowModifierKey? ZoomActivationKey { get; set; }

    /// <summary>
    /// When true, a connection whose source/target/handles exactly match an existing edge is
    /// rejected instead of creating a duplicate. Mirrors <c>addEdge</c>'s behaviour.
    /// </summary>
    [Parameter] public bool PreventDuplicateEdges { get; set; } = true;

    /// <summary>
    /// When true a node may connect to itself (through two different handles). Off by default,
    /// matching the intuition that a node shouldn't loop back on itself unless asked for.
    /// </summary>
    [Parameter] public bool AllowSelfConnections { get; set; }

    /// <summary>
    /// When true, a connection that would close a directed cycle is refused - for pipelines, dependency graphs and
    /// any other flow that has to stay acyclic. A self-connection counts as a cycle. Off by default.
    /// </summary>
    [Parameter] public bool PreventCycles { get; set; }

    /// <summary>
    /// Decides whether data of a source port's <see cref="BlowPort.DataType"/> may flow into a target port's. When
    /// null, <see cref="BlowConnectionRules.AreTypesCompatible"/> applies: untyped ports and <c>"*"</c> accept
    /// anything, and otherwise the two comma-separated type lists must share a type, ignoring case.
    /// </summary>
    [Parameter] public Func<string?, string?, bool>? ConnectionTypeCompatibility { get; set; }

    /// <summary>
    /// An asynchronous last word on a connection the user has just completed - asking a server whether the user
    /// may make it, say. Runs after the user lets go on a valid handle and before the edge is added (or an
    /// existing edge rewired); returning false abandons it. The graph may change while it runs, so the connection
    /// is checked against every rule again once it returns, and dropped if it is no longer allowed. When the wiring
    /// itself has changed - the end of a reconnected edge that stays put moved meanwhile - it is asked again about the
    /// wiring as it is now, so it only ever approves what is made; a wiring still changing after three answers is
    /// dropped. Leave it null
    /// for the ordinary synchronous connect: the rules a connection has to pass while it is dragged belong in
    /// <see cref="IsValidConnection"/>.
    /// </summary>
    [Parameter] public Func<BlowConnection, Task<bool>>? OnBeforeConnect { get; set; }

    /// <summary>Enables Ctrl/Cmd+A to select every selectable element.</summary>
    [Parameter] public bool SelectAllKeyEnabled { get; set; } = true;

    /// <summary>
    /// Enables Ctrl/Cmd+C, X, V and D for copy, cut, paste and duplicate, using the canvas's
    /// own clipboard. Off by default: a flow embedded in a page whose surrounding UI already
    /// handles those keys should not quietly take them over.
    /// </summary>
    [Parameter] public bool ClipboardShortcutsEnabled { get; set; }

    /// <summary>
    /// Turns on undo/redo, driven by the canvas itself: it records a step at each point a user
    /// would recognise as one - a drag finished, an edge connected or reconnected, something
    /// deleted, pasted, resized, aligned or nudged - and binds Ctrl/Cmd+Z and Ctrl/Cmd+Shift+Z
    /// (or Ctrl+Y) to move through them.
    /// </summary>
    /// <remarks>
    /// Off by default, for the same two reasons the clipboard shortcuts are: a flow embedded in
    /// a page whose surrounding UI already owns those keys should not quietly take them, and an
    /// app that already has its own undo stack wants its edits in that one, not in a second one
    /// the canvas keeps privately. Driving <see cref="BlowHistory"/> yourself from the
    /// change callbacks remains fully supported and is the right answer for that case.
    /// </remarks>
    [Parameter] public bool HistoryEnabled { get; set; }

    /// <summary>
    /// How many undo steps the built-in history keeps; values below one keep one. Ignored when
    /// <see cref="HistoryEnabled"/> is false.
    /// </summary>
    [Parameter] public int HistoryCapacity { get; set; } = 100;

    /// <summary>
    /// Raised whenever the built-in history changes - a step recorded, or one undone or redone.
    /// The usual use is enabling and disabling undo/redo buttons.
    /// </summary>
    [Parameter] public EventCallback<BlowHistoryState> OnHistoryChanged { get; set; }

    /// <summary>
    /// How far a paste is offset from what it was copied from, so the copy is visible rather
    /// than sitting exactly on top of the original.
    /// </summary>
    [Parameter] public BlowXYPosition PasteOffset { get; set; } = new(24, 24);

    /// <summary>Raised after elements are pasted, with the fragment that was added.</summary>
    [Parameter] public EventCallback<BlowFragment> OnPaste { get; set; }

    /// <summary>
    /// Minimum pointer travel in screen pixels before a node drag begins. Until this distance
    /// is exceeded the gesture is treated as a click (so the node selects but does not move).
    /// Mirrors React Flow's <c>nodeDragThreshold</c>.
    /// </summary>
    [Parameter] public double NodeDragThreshold { get; set; } = 1;

    /// <summary>
    /// How far the pointer may travel during a node gesture and still count as a click.
    /// Beyond this, <see cref="OnNodeClick"/> is suppressed on release.
    /// </summary>
    [Parameter] public double NodeClickDistance { get; set; } = 3;

    /// <summary>
    /// How far the pointer may travel during a pane gesture and still count as a click.
    /// Beyond this, <see cref="OnPaneClick"/> is suppressed on release.
    /// </summary>
    [Parameter] public double PaneClickDistance { get; set; } = 3;

    /// <summary>Disables keyboard interactions (node focus + arrow-key movement) when true.</summary>
    [Parameter] public bool DisableKeyboardA11y { get; set; }

    /// <summary>When false, nodes are skipped by Tab navigation.</summary>
    [Parameter] public bool NodesFocusable { get; set; } = true;

    /// <summary>When false, edges are skipped by Tab navigation.</summary>
    [Parameter] public bool EdgesFocusable { get; set; } = true;

    /// <summary>When true, dragged node positions snap to <see cref="SnapGridX"/>/<see cref="SnapGridY"/>.</summary>
    [Parameter] public bool SnapToGrid { get; set; }

    /// <summary>
    /// When true, a dragged node clicks into alignment with the nodes around it - edges and
    /// centres - and a thin guide line shows what it lined up with. This is the "smart guides"
    /// behaviour of a drawing tool, and it takes precedence over <see cref="SnapToGrid"/> on
    /// whichever axis actually found an alignment.
    /// </summary>
    [Parameter] public bool SnapToGuides { get; set; }

    /// <summary>
    /// How close, in screen pixels, a dragged node has to come before it snaps to a guide.
    /// Measured on screen rather than in flow units so the feel stays the same at any zoom.
    /// </summary>
    [Parameter] public double GuideDistance { get; set; } = 6;

    /// <summary>
    /// Colour of the alignment guides. Defaults to a magenta that is conventional for the
    /// feature and unlikely to be confused with the graph itself.
    /// </summary>
    [Parameter] public string GuideColor { get; set; } = "#f43f5e";

    /// <summary>Horizontal grid step (flow units) used when <see cref="SnapToGrid"/> is enabled.</summary>
    [Parameter] public double SnapGridX { get; set; } = 15;

    /// <summary>Vertical grid step (flow units) used when <see cref="SnapToGrid"/> is enabled.</summary>
    [Parameter] public double SnapGridY { get; set; } = 15;

    /// <summary>Optional flow-space rectangle that constrains how far the viewport can be panned.</summary>
    [Parameter] public BlowRect? TranslateExtent { get; set; }

    /// <summary>Optional flow-space rectangle that constrains where root nodes can be dragged.</summary>
    [Parameter] public BlowRect? NodeExtent { get; set; }

    /// <summary>Additional CSS class(es) for the flow root element.</summary>
    [Parameter] public string? Class { get; set; }
    /// <summary>Inline style for the flow root element. Give it a height: the canvas fills its container.</summary>
    [Parameter] public string? Style { get; set; }

    /// <summary>
    /// Any additional attributes (id, data-*, aria-*, ...) are splatted onto the flow's root
    /// element, so the canvas can be wired into surrounding markup without wrapping it.
    /// </summary>
    [Parameter(CaptureUnmatchedValues = true)]
    public IReadOnlyDictionary<string, object>? AdditionalAttributes { get; set; }

    /// <summary>The flow this one is nested in (inside one of its nodes, say), or null for a top-level flow.</summary>
    [CascadingParameter(Name = BlowCascadingNames.Context)] private IBlowContext? EnclosingFlow { get; set; }

    /// <summary>
    /// True when this flow sits inside another one. Its pane then keeps its input to itself: a press,
    /// a wheel or a key here is this flow's gesture, and bubbling on it would also drag the node around
    /// it, zoom the outer flow or delete there.
    /// </summary>
    private bool IsNested => EnclosingFlow is not null;

    // ---- state ----

    private ElementReference _paneRef;
    private ElementReference _viewportRef;

    /// <summary>Makes the ids of this canvas's elements unique on a page that holds several flows.</summary>
    private readonly string _idPrefix = Guid.NewGuid().ToString("N");

    private readonly string _markerPrefix = $"bit-blow-arrow-{Guid.NewGuid():N}";

    /// <summary>The id of one of the hidden keyboard-instruction elements.</summary>
    private string DescriptionId(string kind) => $"bit-blow-{kind}-help-{_idPrefix}";

    /// <summary>
    /// The pane's <c>aria-describedby</c>: the flow's own instructions, after whatever the app described it by
    /// through an attribute of its own, so neither replaces the other.
    /// </summary>
    private string? PaneDescribedBy
    {
        get
        {
            var own = string.IsNullOrEmpty(AriaLabels.PaneInstructions) ? null : DescriptionId("pane");
            var app = AdditionalAttributes?.GetValueOrDefault("aria-describedby")?.ToString();
            return string.IsNullOrWhiteSpace(app) ? own : own is null ? app : $"{app} {own}";
        }
    }

    /// <summary>What a focused node is described by, or null when nodes are not described (or not focusable).</summary>
    private string? NodeDescriptionId =>
        DisableKeyboardA11y || !NodesFocusable || string.IsNullOrEmpty(AriaLabels.NodeInstructions)
            ? null
            : DescriptionId("node");

    /// <summary>What a focused edge is described by, or null when edges are not described (or not focusable).</summary>
    private string? EdgeDescriptionId =>
        DisableKeyboardA11y || !EdgesFocusable || string.IsNullOrEmpty(AriaLabels.EdgeInstructions)
            ? null
            : DescriptionId("edge");

    private BlowViewport _viewport = BlowViewport.Identity;
    private BlowDomRect? _paneRect;
    private BlowInterop _interop = default!;

    // resize observation (pane + node sizes change without any C#-visible event)
    private DotNetObjectReference<BlowCanvas>? _selfRef;
    private IJSObjectReference? _resizeObserver;

    // nodrag / nopan / nowheel guards (installed in JS: only the DOM knows the event target)
    private IJSObjectReference? _interactionGuards;

    /// <summary>The class names and wheel policy the live guards were built for, so a change can be noticed.</summary>
    private (string? Drag, string? Pan, string? Wheel, BlowInputPolicy Policy)? _installedGuards;

    private bool _needsMeasure = true;

    // ---- render scoping ----
    //
    // Most renders of a busy canvas change very little: a frame of a pan moves the viewport transform and nothing
    // else, a frame of a drag moves a node or two. Re-rendering every node, edge and handle component for them is
    // what made a pan of a 1,000-node graph cost a full render of 6,000 components per frame. So every render is
    // tagged with what it may have changed, and the parts that draw something else are told nothing:
    //
    //   Overlay   - only the canvas's own markup: the viewport transform, the connection line, guides.
    //   Geometry  - nodes may have moved or changed size, or stopped being dragged (drag, drop, resize, measurement).
    //   Full      - anything (selection, the lists, parameters, a render of the host component).
    //
    // A render nobody tagged - the automatic one after an event handler, a parameter update - is Full. The version
    // numbers below are cascaded (see BlowCascadingNames) and passed to the node and edge renderers, which compare
    // them, and the node or edge they draw, with what they last rendered.

    [Flags]
    private enum RenderScope { None = 0, Overlay = 1, Geometry = 2, Full = 4 }

    private RenderScope _pendingScope;
    private int _viewportVersion, _geometryVersion, _graphVersion, _connectionVersion;
    private BlowViewport _renderedViewport;
    private BlowDomRect? _renderedPane;

    /// <summary>Queues a render that may have changed anything. Every unqualified call in the canvas means this.</summary>
    private new void StateHasChanged()
    {
        _pendingScope |= RenderScope.Full;

        // Inside EditAsync the edit renders once, at its end; the scope is kept for that render.
        if (InEdit) return;
        base.StateHasChanged();
    }

    /// <summary>Queues a render of the canvas's own markup only: the viewport, the connection line, the guides.</summary>
    private void RenderOverlay()
    {
        _pendingScope |= RenderScope.Overlay;
        base.StateHasChanged();
    }

    /// <summary>Queues a render in which nodes may have moved or been resized, and nothing else changed.</summary>
    private void RenderGeometry()
    {
        _pendingScope |= RenderScope.Geometry;
        base.StateHasChanged();
    }

    // ---- the callbacks each node and edge is rendered with ----
    //
    // A node is handed nine callbacks and an edge eight, each bound to that node or edge. Written as lambdas in the
    // markup they would be allocated afresh for every element on every render - two megabytes a frame on a
    // 1,000-node graph, most of the garbage a pan made. They are made once per element instead, and made again only
    // when the canvas's parameters change, since which of them are wired up depends on which events the app handles.

    private readonly Dictionary<BlowNode, BlowNodeCallbacks> _nodeCallbacks = new(ReferenceEqualityComparer.Instance);
    private readonly Dictionary<BlowEdge, BlowEdgeCallbacks> _edgeCallbacks = new(ReferenceEqualityComparer.Instance);

    // What every node and edge renderer is told alike, rebuilt only when something in it changes - so a thousand
    // renderers share one object rather than each being handed a dozen boxed values on every frame.
    private BlowNodeRenderSettings _nodeSettings = new();
    private BlowEdgeRenderSettings _edgeSettings = new();

    private void UpdateRenderSettings()
    {
        var nodesFocusable = !DisableKeyboardA11y && NodesFocusable;
        var elevate = ZIndexMode != BlowZIndexMode.Manual && ElevateNodesOnSelect;
        var elevateDragged = ZIndexMode != BlowZIndexMode.Manual;
        var connectionKey = KeyboardConnections ? ConnectionKey : null;
        if (!_nodeSettings.SameAs(NodeTemplate, _graphVersion, nodesFocusable, NodeDescriptionId, elevate, elevateDragged, AriaLabels,
                connectionKey))
        {
            _nodeSettings = new BlowNodeRenderSettings
            {
                Template = NodeTemplate,
                RenderEpoch = _graphVersion,
                Focusable = nodesFocusable,
                DescribedBy = NodeDescriptionId,
                Elevate = elevate,
                ElevateWhileDragging = elevateDragged,
                AriaLabels = AriaLabels,
                ConnectionKey = connectionKey,
                State = _nodeStateOf ??= NodeStateOf,
            };
        }

        var edgesFocusable = !DisableKeyboardA11y && EdgesFocusable;
        var suppressLabel = EdgeLabelTemplate is not null;
        var reconnectionKey = KeyboardReconnections ? ConnectionKey : null;
        if (!_edgeSettings.SameAs(EdgeTemplate, suppressLabel, _graphVersion, edgesFocusable, EdgeDescriptionId, AriaLabels,
                reconnectionKey))
        {
            _edgeSettings = new BlowEdgeRenderSettings
            {
                Template = EdgeTemplate,
                SuppressLabel = suppressLabel,
                RenderEpoch = _graphVersion,
                Focusable = edgesFocusable,
                DescribedBy = EdgeDescriptionId,
                AriaLabels = AriaLabels,
                ConnectionKey = reconnectionKey,
                State = _edgeStateOf ??= EdgeStateOf,
                NodeName = _nodeNameOf ??= NameOf,
            };
        }
    }

    // ---- what a node or an edge is described by ----

    private Func<BlowNode, BlowNodeState>? _nodeStateOf;
    private Func<BlowEdge, BlowEdgeState>? _edgeStateOf;
    private Func<string, string>? _nodeNameOf;

    /// <summary>How many edges end at and start from each node, counted once per graph version.</summary>
    private readonly Dictionary<string, (int In, int Out)> _degrees = new(StringComparer.Ordinal);
    private int _degreesVersion = -1;

    /// <summary>A node's state, for its description and its validation marks. Asked for by its renderer.</summary>
    private BlowNodeState NodeStateOf(BlowNode node)
    {
        if (_degreesVersion != _graphVersion)
        {
            _degrees.Clear();
            _ = Index;
            foreach (var edge in Edges)
            {
                // Counted where the edge is drawn: an edge into a collapsed group's child is the group's.
                if (edge.Hidden || !IsEdgeVisible(edge)) continue;
                if (DisplayedEndpoint(edge.Source, out _) is not { } from || DisplayedEndpoint(edge.Target, out _) is not { } to) continue;
                var source = _degrees.GetValueOrDefault(from.Id);
                _degrees[from.Id] = (source.In, source.Out + 1);
                var target = _degrees.GetValueOrDefault(to.Id);
                _degrees[to.Id] = (target.In + 1, target.Out);
            }
            _degreesVersion = _graphVersion;
        }

        var (incoming, outgoing) = _degrees.GetValueOrDefault(node.Id);
        return new BlowNodeState(node, incoming, outgoing, MostSeriousFirst(Validation?.ForNode(node.Id)),
            RunStates?.GetValueOrDefault(node.Id) ?? default);
    }

    /// <summary>An edge's state, for its description and its validation marks. Asked for by its renderer.</summary>
    private BlowEdgeState EdgeStateOf(BlowEdge edge) => new(edge, MostSeriousFirst(Validation?.ForEdge(edge.Id)));

    private static IReadOnlyList<BlowValidationIssue> MostSeriousFirst(IReadOnlyList<BlowValidationIssue>? issues)
        => issues is null or { Count: 0 } ? []
            : issues.Count == 1 ? issues
            : issues.OrderByDescending(i => i.Severity).ToList();

    /// <summary>The most serious problem the validation found at one port of a node, or null when none.</summary>
    public BlowValidationSeverity? ValidationSeverityAt(string nodeId, BlowHandleType handleType, string? handleId)
    {
        if (Validation is null) return null;

        BlowValidationSeverity? worst = null;
        foreach (var issue in Validation.ForNode(nodeId))
        {
            if (issue.PortType != handleType || !string.Equals(issue.PortId ?? "", handleId ?? "", StringComparison.Ordinal)) continue;
            if (worst is null || issue.Severity > worst) worst = issue.Severity;
        }
        return worst;
    }

    private BlowNodeCallbacks CallbacksFor(BlowNode node)
    {
        if (_nodeCallbacks.TryGetValue(node, out var callbacks)) return callbacks;

        callbacks = new BlowNodeCallbacks
        {
            DragStart = EventCallback.Factory.Create<PointerEventArgs>(this, e => StartNodeDrag(node, e)),
            Activate = EventCallback.Factory.Create<bool>(this, additive => ActivateNode(node, additive)),
            Connect = KeyboardConnections ? EventCallback.Factory.Create(this, () => OnNodeConnectKey(node)) : default,
            Focus = NodeFocusHandler(node),
            Click = EventCallback.Factory.Create<MouseEventArgs>(this, _ => OnNodeClickHandler(node)),
            DoubleClick = NodeMouse(OnNodeDoubleClick, node),
            ContextMenu = NodeContextMenu(node),
            MouseEnter = NodeMouse(OnNodeMouseEnter, node),
            MouseMove = NodeMouse(OnNodeMouseMove, node),
            MouseLeave = NodeMouse(OnNodeMouseLeave, node),
        };
        _nodeCallbacks[node] = callbacks;
        return callbacks;
    }

    private BlowEdgeCallbacks CallbacksFor(BlowEdge edge)
    {
        if (_edgeCallbacks.TryGetValue(edge, out var callbacks)) return callbacks;

        callbacks = new BlowEdgeCallbacks
        {
            Activate = EventCallback.Factory.Create<bool>(this, additive => ActivateEdge(edge, additive)),
            Select = EventCallback.Factory.Create<PointerEventArgs>(this, e => SelectEdge(edge, e)),
            Connect = KeyboardReconnections ? EventCallback.Factory.Create(this, () => OnEdgeConnectKey(edge)) : default,
            Click = EventCallback.Factory.Create<MouseEventArgs>(this, _ => OnEdgeClickHandler(edge)),
            DoubleClick = EdgeMouse(OnEdgeDoubleClick, edge),
            ContextMenu = EdgeMouse(OnEdgeContextMenu, edge),
            MouseEnter = EdgeMouse(OnEdgeMouseEnter, edge),
            MouseMove = EdgeMouse(OnEdgeMouseMove, edge),
            MouseLeave = EdgeMouse(OnEdgeMouseLeave, edge),
        };
        _edgeCallbacks[edge] = callbacks;
        return callbacks;
    }

    /// <summary>
    /// Drops the callbacks of nodes and edges that have left the graph. Nothing is scanned while there are no more
    /// entries than elements, which is every render that removed nothing.
    /// </summary>
    private void EvictStaleCallbacks()
    {
        if (_nodeCallbacks.Count > Nodes.Count)
        {
            foreach (var node in _nodeCallbacks.Keys.ToArray())
            {
                if (!ReferenceEquals(_index.GetValueOrDefault(node.Id), node)) _nodeCallbacks.Remove(node);
            }
        }

        if (_edgeCallbacks.Count > Edges.Count)
        {
            var edges = EdgeIndex;
            foreach (var edge in _edgeCallbacks.Keys.ToArray())
            {
                if (!ReferenceEquals(edges.GetValueOrDefault(edge.Id), edge)) _edgeCallbacks.Remove(edge);
            }
        }
    }

    /// <summary>The version of the node and edge layers; see <see cref="BlowRenderGate"/>.</summary>
    private int _layerVersion;

    /// <summary>
    /// Settles what the render that is starting has to tell the parts it draws. Called once per render. Returns
    /// false for a render that can only have moved the viewport, which leaves the graph exactly as last drawn.
    /// </summary>
    private bool AdvanceRenderVersions()
    {
        var scope = _pendingScope;
        _pendingScope = RenderScope.None;
        var graphMayHaveChanged = true;

        if (scope == RenderScope.None || (scope & RenderScope.Full) != 0)
        {
            _graphVersion++;
            _geometryVersion++;
        }
        else if ((scope & RenderScope.Geometry) != 0)
        {
            _geometryVersion++;
        }
        else
        {
            graphMayHaveChanged = false;
        }

        if (_viewport != _renderedViewport || !Equals(_paneRect, _renderedPane))
        {
            _viewportVersion++;
            _renderedViewport = _viewport;
            _renderedPane = _paneRect;
        }

        return graphMayHaveChanged;
    }

    /// <summary>Per-handle validation predicates and connect callbacks, keyed by handle.</summary>
    private readonly Dictionary<string, BlowHandleRegistration> _handles = [];

    /// <summary>
    /// Problems noticed while rendering. Raising <see cref="OnError"/> mid-render would
    /// re-enter the renderer, so they are buffered and flushed after the render completes.
    /// </summary>
    private readonly List<BlowError> _pendingErrors = [];

    /// <summary>Error keys already reported, so a persistent problem is announced once, not per render.</summary>
    private readonly HashSet<string> _reportedErrors = [];

    /// <summary>Tracks the measured/unmeasured transition so OnNodesInitialized fires on the edge.</summary>
    private bool _nodesInitialized;

    /// <summary>
    /// Depth guard for parent chains. Cycles are caught separately and reported as parent-cycle. The SVG export follows
    /// chains exactly as far, so its picture of a deep nesting is the one on screen.
    /// </summary>
    internal const int MaxParentDepth = 50;

    // per-render caches (rebuilt by PrepareRender)
    private bool _indexDirty = true;
    private readonly Dictionary<string, BlowNode> _index = [];
    private readonly Dictionary<string, BlowXYPosition> _rawAbsolute = [];
    private readonly Dictionary<string, int> _depth = [];
    private readonly Dictionary<string, BlowEdgeContext> _edgeContexts = [];
    private readonly List<BlowNode> _renderNodes = [];
    private readonly List<BlowEdge> _renderEdges = [];

    // id -> edge lookup, kept in step with the edge list the same way the node index is
    private bool _edgeIndexDirty = true;
    private readonly Dictionary<string, BlowEdge> _edgeIndex = [];

    /// <summary>
    /// The distinct arrowhead shapes the current edges ask for, keyed by their spec. SVG markers
    /// have to be declared up front in <c>&lt;defs&gt;</c>, so the set is collected during
    /// <see cref="PrepareRender"/> and emitted before any edge references it.
    /// </summary>
    private readonly Dictionary<string, BlowMarkerSpec> _markerDefs = [];

    /// <summary>
    /// The id key of every arrowhead spec seen so far. Building a key formats a string, and the marker of every
    /// visible edge is looked up on every render; there are only ever a handful of distinct specs.
    /// </summary>
    private readonly Dictionary<BlowMarkerSpec, string> _markerKeys = [];

    private string MarkerKey(BlowMarkerSpec spec)
    {
        if (_markerKeys.TryGetValue(spec, out var key)) return key;
        if (_markerKeys.Count > 1024) _markerKeys.Clear(); // a graph styling every edge differently
        return _markerKeys[spec] = spec.Key;
    }

    /// <summary>Ids of the currently selected elements, used to suppress no-op selection events.</summary>
    private readonly HashSet<string> _lastSelection = [];

    /// <summary>Current contents of the aria-live region.</summary>
    private string _liveMessage = string.Empty;

    /// <summary>Last value received through the controlled <see cref="Viewport"/> parameter.</summary>
    private BlowViewport? _lastControlledViewport;

    // pan
    private bool _panning;
    private bool _panKeyHeld;
    private double _panStartX, _panStartY;
    private BlowViewport _panStartViewport;

    // click suppression: set when a gesture travelled far enough to count as a drag
    private bool _suppressNextClick;

    /// <summary>Set by a plain press on the empty pane, for the click that ends it to clear the selection.</summary>
    private bool _clearSelectionOnClick;

    // box selection
    private bool _selecting;
    private BlowXYPosition _selStart;
    private BlowXYPosition _selEnd;
    private bool _selAdditive;
    private readonly HashSet<string> _preSelected = [];

    // viewport portals (BlowViewportPortal component content)
    private readonly Dictionary<object, RenderFragment> _viewportPortals = [];

    // node drag
    private bool _draggingNodes;
    private bool _dragArmed;
    private BlowNode? _dragPrimary;
    private double _dragStartClientX, _dragStartClientY;
    private BlowXYPosition _dragStartFlow;
    private readonly List<(BlowNode node, BlowXYPosition start)> _dragSet = [];

    /// <summary>
    /// How the running drag found every node it may change: the dragged nodes, and for a child that
    /// grows its parent, that parent and the children inside it, which growing it leftwards or
    /// upwards renumbers. Cancelling the drag puts all of them back.
    /// </summary>
    private readonly Dictionary<BlowNode, (BlowXYPosition Position, double? Width, double? Height)> _dragOriginals = [];

    /// <summary>Absolute rectangle the primary dragged node started from, the reference for guides.</summary>
    private BlowRect _dragPrimaryStartRect;

    /// <summary>Guides currently shown; empty except while a drag is actually aligned to something.</summary>
    private IReadOnlyList<BlowAlignmentGuide> _guides = [];

    // wheel gesture (a scroll has no start or end of its own; see BeginWheelGesture)
    private CancellationTokenSource? _wheelIdleCts;
    private bool _wheelActive;

    /// <summary>Milliseconds of wheel silence after which a scroll gesture counts as finished.</summary>
    private const int WheelIdleDelayMs = 160;

    // viewport animation (SetViewport/FitView with a duration)
    private CancellationTokenSource? _viewportAnimationCts;

    // auto-pan
    private CancellationTokenSource? _autoPanCts;
    private (double X, double Y) _autoPanVector;
    private (double X, double Y) _autoPanPointer;

    /// <summary>True once the running auto-pan has moved the viewport, so its end is reported as a move's end.</summary>
    private bool _autoPanned;

    // pointer capture (the pane captures the pointer for the duration of a gesture)
    private long? _capturedPointerId;

    /// <summary>Pointer id passed by the overloads that have no real pointer to capture.</summary>
    private const long NoPointer = -1;

    // pinch to zoom
    private readonly Dictionary<long, (double X, double Y)> _activePointers = [];
    private bool _pinching;
    private double _pinchStartDistance;
    private BlowViewport _pinchStartViewport;

    // connection
    private bool _connecting;
    private bool _connMoved;

    /// <summary>Where the connection gesture began, in screen pixels, for the drag threshold.</summary>
    private double _connStartClientX, _connStartClientY;

    private ConnectionSource _connSource;
    private BlowXYPosition _connEnd;
    private double _connEndClientX, _connEndClientY;
    private bool _connEndValid;
    private (string nodeId, string? handleId, BlowHandleType type)? _connHover;

    /// <summary>The valid handle within snapping distance of the connection's loose end, if any.</summary>
    private (string nodeId, string? handleId, BlowHandleType type)? _connSnap;

    /// <summary>
    /// When no handle within snapping distance would accept the connection, the nearest one that refuses it and
    /// why - what the connection line's context reports as its rejection.
    /// </summary>
    private (string NodeId, string? HandleId, BlowHandleType Type, BlowConnectionRejection Rejection)? _connNear;

    /// <summary>Why the gesture is about to end without connecting, when a handle refused it.</summary>
    private BlowConnectionRejection _connRejection;

    /// <summary>Set while a released drag is being resolved, so the end event can say it was a drop.</summary>
    private bool _connDropping;
    private BlowEdge? _reconnectEdge;
    private bool _reconnectRewired;

    // resize
    private bool _resizing;
    private BlowNode? _resizeNode;
    private BlowResizeDirection _resizeDir;
    private double _resizeStartClientX, _resizeStartClientY;
    private BlowXYPosition _resizeStartPos;
    private BlowDimensions _resizeStartSize;

    /// <summary>The resized node's own width and height before the resize, unset ones included.</summary>
    private (double? Width, double? Height) _resizeOriginalSize;
    private double _resizeMinW, _resizeMinH;
    private double _resizeMaxW = double.MaxValue, _resizeMaxH = double.MaxValue;
    private bool _resizeKeepAspect;

    private readonly record struct ConnectionSource(
        string NodeId, string? HandleId, BlowHandleType Type, BlowPosition Position, BlowXYPosition Anchor);

    // ---- IBlowContext ----

    // Explicit, so the public surface can expose `Viewport` as the controlled-viewport
    // parameter. Consumers holding a BlowCanvas reference use GetViewport() instead.
    BlowViewport IBlowContext.Viewport => _viewport;

    /// <summary>True while the user is drawing a connection from a handle.</summary>
    public bool IsConnecting => _connecting;
    /// <summary>The live node list, exposed to overlays such as the minimap.</summary>
    public IReadOnlyList<BlowNode> CurrentNodes => Nodes;
    /// <summary>The pane's measured size in screen pixels, or <see cref="BlowDimensions.Empty"/> before the first measurement.</summary>
    public BlowDimensions PaneSize => _paneRect is { } r ? new BlowDimensions(r.Width, r.Height) : BlowDimensions.Empty;

    // ---- lifecycle ----

    /// <inheritdoc />
    protected override void OnInitialized()
    {
        _interop = new BlowInterop(JS);

        // The uncontrolled starting viewport. Applied before the first render so the flow never
        // paints at the identity transform and then jumps.
        if (Viewport is null && DefaultViewport is { } initial)
        {
            _viewport = initial with { Zoom = Math.Clamp(initial.Zoom, MinZoom, MaxZoom) };
        }
    }

    /// <inheritdoc />
    protected override void OnParametersSet()
    {
        // Re-measuring costs a JS round-trip over every node in the DOM, and what it reads -
        // node sizes and handle offsets - is decided by content, not by pan or zoom. A parent
        // that re-renders on every OnMove (any controlled-viewport flow does) would otherwise
        // pay for a full measurement on every frame of every gesture. The gestures that really
        // do change geometry (drag stop, resize, expand-parent) set the flag themselves.
        // A wheel zoom or an animated transition moves the viewport just as continuously as a
        // drag does, and a controlled parent re-renders on every step of it too.
        if (!IsGesturing && !_wheelActive && _viewportAnimationCts is null) _needsMeasure = true;

        // The host re-rendered: its templates may draw anything differently now, and which of the node and edge
        // callbacks are wired up depends on the event parameters it just handed over.
        _pendingScope |= RenderScope.Full;
        _nodeCallbacks.Clear();
        _edgeCallbacks.Clear();

        _indexDirty = true;
        _edgeIndexDirty = true;

        SyncHistory();

        // Controlled viewport: the parameter is the source of truth, so adopt it whenever the
        // caller hands us a different one. Comparing against the last value we were *given*
        // (rather than the current viewport) keeps user pans from being reverted on re-render.
        if (Viewport is { } controlled && controlled != _lastControlledViewport)
        {
            _lastControlledViewport = controlled;
            _viewport = ClampViewport(controlled with
            {
                Zoom = Math.Clamp(controlled.Zoom, MinZoom, MaxZoom),
            });
        }
    }

    /// <inheritdoc />
    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            _initialFitPending = FitViewOnInit;
            await RefreshPaneRectAsync();
            await StartResizeObserverAsync();
            await ReadMotionPreferenceAsync();
        }

        await InstallInteractionGuardsAsync();

        if (_needsMeasure)
        {
            _needsMeasure = false;
            var changed = await MeasureAsync();
            if (_initialFitPending && TryInitialFit()) changed = true;
            // Sizes and handle anchors: edges move, nodes do not change what they draw.
            if (changed)
                RenderGeometry();
        }

        if (_keepFocusInPane)
        {
            _keepFocusInPane = false;
            try { await _interop.KeepFocusAsync(_paneRef); }
            catch (Exception ex) when (IsInteropTeardown(ex)) { }
        }

        await FlushErrorsAsync();
        await NotifyNodesInitializedAsync();

        if (firstRender && OnInit.HasDelegate)
            await OnInit.InvokeAsync(this);
    }

    /// <summary>
    /// Set on the first render when <see cref="FitViewOnInit"/> is on, and cleared once the fit
    /// has actually happened. A graph that arrives after the first render - loaded from a
    /// server, say - has nothing to fit on that render, and fitting nothing must not count as
    /// having fitted.
    /// </summary>
    private bool _initialFitPending;

    /// <summary>Performs the initial fit when there is something to fit. True when it happened.</summary>
    private bool TryInitialFit()
    {
        if (ComputeFitView(FitViewOptions ?? new BlowFitViewOptions()) is not { } target) return false;

        _initialFitPending = false;
        StopViewportAnimation();
        ApplyViewport(target);
        return true;
    }

    /// <summary>
    /// True once every visible node has been measured. Layout algorithms need real sizes, so
    /// this is the signal that the graph is ready to be arranged.
    /// </summary>
    public bool NodesInitialized => AllNodesMeasured();

    private bool AllNodesMeasured()
    {
        var any = false;
        foreach (var n in Nodes)
        {
            if (n.Hidden) continue;
            any = true;
            if (n.Measured.IsEmpty) return false;
        }
        return any;
    }

    private async Task NotifyNodesInitializedAsync()
    {
        var initialized = AllNodesMeasured();
        if (initialized == _nodesInitialized) return;

        _nodesInitialized = initialized;
        if (initialized && OnNodesInitialized.HasDelegate)
            await OnNodesInitialized.InvokeAsync();
    }

    /// <summary>
    /// Reports the problems noticed during the render that just finished. Buffering them until
    /// here keeps <see cref="OnError"/> from re-entering the renderer mid-render.
    /// </summary>
    private async Task FlushErrorsAsync()
    {
        if (_pendingErrors.Count == 0) return;

        var errors = _pendingErrors.ToArray();
        _pendingErrors.Clear();

        if (!OnError.HasDelegate) return;
        foreach (var error in errors) await OnError.InvokeAsync(error);
    }

    /// <summary>
    /// Records a problem for reporting after the render, at most once per distinct occurrence.
    /// Without the de-duplication a graph that stays broken would raise the same error on every
    /// frame of every drag.
    /// </summary>
    private void ReportError(string code, string key, string message)
    {
        if (!OnError.HasDelegate) return;
        if (!_reportedErrors.Add($"{code}{key}")) return;
        _pendingErrors.Add(new BlowError(code, message));
    }

    /// <summary>
    /// Installs the <c>nodrag</c> / <c>nopan</c> / <c>nowheel</c> guards. They have to live in
    /// JS: only the DOM knows which element an event actually started on, and by the time Blazor
    /// hands us the event the chance to opt out has passed.
    /// </summary>
    private async Task InstallInteractionGuardsAsync()
    {
        // The guards are DOM listeners bound to specific class names and an input policy, so
        // changing either means tearing the old listeners down and putting new ones up.
        var policy = new BlowInputPolicy(PreventScrolling, PanOnScroll, ZoomOnScroll, ZoomActivationKey switch
        {
            BlowModifierKey.Shift => "shiftKey",
            BlowModifierKey.Control => "ctrlKey",
            BlowModifierKey.Alt => "altKey",
            BlowModifierKey.Meta => "metaKey",
            _ => null,
        }, PanActivationKey, !DisableKeyboardA11y, SelectAllKeyEnabled && ElementsSelectable,
            ClipboardShortcutsEnabled, HistoryEnabled,
            // One string rather than the list, so the policy still compares by value from render to render.
            DeleteKeyEnabled && DeleteKeys.Count > 0 ? string.Join('\n', DeleteKeys) : null);
        var wanted = (NoDragClass, NoPanClass, NoWheelClass, policy);
        if (_installedGuards == wanted) return;

        var previous = _interactionGuards;
        _interactionGuards = null;
        _installedGuards = wanted;
        await DisposeJsHandleAsync(previous);

        IJSObjectReference? installed;
        try
        {
            installed = await _interop.InstallInteractionGuardsAsync(
                _paneRef, NoDragClass, NoPanClass, NoWheelClass, policy);
        }
        catch (Exception ex) when (IsInteropTeardown(ex)) { return; }

        // Renders overlap across the await: the component may have been disposed meanwhile, or a
        // later render may have asked for different guards. Either way these are not wanted, and
        // keeping them would leave their listeners attached for good.
        if (_disposed || _installedGuards != wanted || _interactionGuards is not null)
        {
            await DisposeJsHandleAsync(installed);
            return;
        }
        _interactionGuards = installed;
    }

    private async Task StartResizeObserverAsync()
    {
        var selfRef = DotNetObjectReference.Create(this);
        IJSObjectReference? observer;
        try
        {
            observer = await _interop.ObserveResizeAsync(_paneRef, _viewportRef, selfRef);
        }
        catch (Exception ex) when (IsInteropTeardown(ex))
        {
            selfRef.Dispose();
            return;
        }

        // Disposed while the observer was being set up (navigating away right after the first
        // render): DisposeAsync has already run and found nothing to stop, so stop it here, or
        // its window listeners keep calling into a component that no longer exists.
        if (_disposed)
        {
            await DisposeJsHandleAsync(observer);
            selfRef.Dispose();
            return;
        }

        _selfRef = selfRef;
        _resizeObserver = observer;
    }

    /// <summary>Set once <see cref="DisposeAsync"/> has run, for work that was in flight across it.</summary>
    private bool _disposed;

    /// <summary>What the operating system's <c>prefers-reduced-motion</c> said when the canvas started.</summary>
    private bool _systemReducesMotion;

    /// <summary>
    /// True when the flow is not animating: <see cref="ReducedMotion"/> asks for that outright, or leaves it to an
    /// operating system set to reduce motion. Viewport transitions then jump instead of easing.
    /// </summary>
    public bool ReducesMotion => ReducedMotion switch
    {
        BlowReducedMotion.Always => true,
        BlowReducedMotion.Never => false,
        _ => _systemReducesMotion,
    };

    private async Task ReadMotionPreferenceAsync()
    {
        try { _systemReducesMotion = await _interop.PrefersReducedMotionAsync(); }
        catch (Exception ex) when (IsInteropTeardown(ex) || ex is JSException) { }
    }

    /// <summary>
    /// Called from JS when the pane or any node changes size. Nothing in C# can observe this,
    /// yet both the pane rectangle (screen &lt;-&gt; flow conversions, minimap mask) and the node
    /// measurements (edge anchors, fit-view bounds) depend on it.
    /// </summary>
    /// <param name="measureNodes">
    /// False when only the pane's on-screen position moved (a scroll), where re-measuring every
    /// node would be wasted work.
    /// </param>
    [JSInvokable]
    public async Task OnResizeObserved(bool measureNodes = true)
    {
        var before = _paneRect;
        await RefreshPaneRectAsync();

        var paneResized = before is null || _paneRect is null
            || Math.Abs(before.Width - _paneRect.Width) > 0.5
            || Math.Abs(before.Height - _paneRect.Height) > 0.5;

        var nodesChanged = measureNodes && await MeasureAsync();

        // A pure scroll only refreshes the cached rectangle; nothing rendered depends on it,
        // so there is no need to re-render.
        if (nodesChanged) RenderGeometry();
        else if (paneResized) RenderOverlay();
    }

    /// <summary>
    /// True for the exceptions that simply mean "the JS side is going away" (circuit dropped,
    /// component disposed, prerendering). These are expected during teardown, not real errors.
    /// </summary>
    private static bool IsInteropTeardown(Exception ex) =>
        ex is JSDisconnectedException or ObjectDisposedException or OperationCanceledException
            or InvalidOperationException;

    private async Task RefreshPaneRectAsync()
    {
        try { _paneRect = await _interop.GetRectAsync(new BlowElementReferenceLike(_paneRef)); }
        catch (Exception ex) when (IsInteropTeardown(ex)) { }
    }

    /// <summary>
    /// The node object the browser's last report of each id went to. It remembers its reports by id and reports only
    /// what changed since, so a node object that is not the one its last report of that id went to - one just added,
    /// put in place of another with the same id, or put back after one was - is asked for by name.
    /// </summary>
    private readonly Dictionary<string, BlowNode> _reportedTo = new(StringComparer.Ordinal);

    /// <summary>Nodes to read afresh at the next measurement whatever the browser remembers (<see cref="UpdateNodeInternals"/>).</summary>
    private readonly HashSet<string> _remeasure = new(StringComparer.Ordinal);

    /// <summary>True to read every node afresh at the next measurement (<see cref="Refresh"/>, <see cref="UpdateNodeInternals"/>).</summary>
    private bool _remeasureAll;

    private async Task<bool> MeasureAsync()
    {
        // Taking a node's measurements in the browser is cheap; bringing them across to .NET is not - a quarter of a
        // second for 900 nodes in a WebAssembly app. They rarely change (moving a node changes neither its size nor
        // its handles), so after the first pass only the nodes that did change come across.
        var include = new List<string>();
        foreach (var node in _renderNodes)
        {
            if (!ReferenceEquals(_reportedTo.GetValueOrDefault(node.Id), node) || _remeasure.Contains(node.Id)) include.Add(node.Id);
        }
        var everything = _remeasureAll || include.Count * 2 > _renderNodes.Count;
        _remeasureAll = false;
        _remeasure.Clear();

        BlowMeasuredNode[] measured;
        try
        {
            measured = await _interop.MeasureNodesAsync(new BlowElementReferenceLike(_viewportRef), _viewport.Zoom,
                changedOnly: !everything, everything ? null : include);
        }
        catch (Exception ex) when (IsInteropTeardown(ex)) { return false; }

        if (_reportedTo.Count > 2 * Nodes.Count + 64)
        {
            foreach (var id in _reportedTo.Keys.Where(id => !Index.ContainsKey(id)).ToList()) _reportedTo.Remove(id);
        }

        bool changed = false;
        var byId = Index;
        foreach (var m in measured)
        {
            if (m.Id is null || !byId.TryGetValue(m.Id, out var node)) continue;
            _reportedTo[m.Id] = node;

            var dims = new BlowDimensions(m.Width, m.Height);
            if (node.Measured != dims) { node.Measured = dims; changed = true; }

            // Only rebuild the handle map when it actually differs: an unconditional rebuild
            // would report "changed" on every measure and force a redundant extra render.
            if (HandlesDiffer(node, m.Handles))
            {
                node.Handles.Clear();
                foreach (var h in m.Handles)
                {
                    var type = h.Type == "target" ? BlowHandleType.Target : BlowHandleType.Source;
                    var pos = ParsePosition(h.Position);
                    var handleId = string.IsNullOrEmpty(h.HandleId) ? null : h.HandleId;
                    var key = BlowHandleBounds.Key(type, handleId);

                    // Two handles of the same type sharing an id (usually: two of them with no id
                    // at all) are indistinguishable to every edge that names one, so the second
                    // silently takes the first one's edges. Say so rather than let it puzzle.
                    if (node.Handles.ContainsKey(key))
                    {
                        ReportError(BlowErrorCode.DuplicateHandleId, node.Id + key,
                            $"Node '{node.Id}' has more than one {type.ToString().ToLowerInvariant()} handle "
                            + (handleId is null
                                ? "without an id; give each one an Id so edges can tell them apart."
                                : $"with the id '{handleId}'; handle ids must be unique within a node."));
                    }

                    node.Handles[key] = new BlowHandleBounds(h.HandleId, type, pos, h.OffsetX, h.OffsetY,
                        h.ConnectableEnd ?? true);
                }
                changed = true;
            }
        }
        return changed;
    }

    private static bool HandlesDiffer(BlowNode node, BlowMeasuredHandle[] measured)
    {
        if (node.Handles.Count != measured.Length) return true;
        foreach (var h in measured)
        {
            var type = h.Type == "target" ? BlowHandleType.Target : BlowHandleType.Source;
            var key = BlowHandleBounds.Key(type, string.IsNullOrEmpty(h.HandleId) ? null : h.HandleId);
            if (!node.Handles.TryGetValue(key, out var existing)) return true;
            if (existing.Position != ParsePosition(h.Position)) return true;
            if (Math.Abs(existing.OffsetX - h.OffsetX) > 0.01) return true;
            if (Math.Abs(existing.OffsetY - h.OffsetY) > 0.01) return true;
            if (existing.ConnectableEnd != (h.ConnectableEnd ?? true)) return true;
        }
        return false;
    }

    // ---- rendering helpers ----

    /// <summary>
    /// Single per-render preparation pass: resolves the layout once, drops the previous
    /// render's edge-context cache and materialises the node/edge draw lists. Called from the
    /// top of the component markup so every part of the render sees consistent geometry.
    /// </summary>
    private void PrepareRender()
    {
        var graphMayHaveChanged = AdvanceRenderVersions();
        UpdateRenderSettings();

        // A frame that moved nothing but the viewport keeps the layout and the draw lists of the last one, and the
        // node and edge layers stay behind their gate. With culling the lists depend on the viewport, so they are
        // rebuilt on every frame.
        if (!graphMayHaveChanged && !OnlyRenderVisibleElements) return;
        _layerVersion++;

        ComputeLayout();
        _handleCounts = null;

        _renderNodes.Clear();
        _renderEdges.Clear();

        BlowRect? view = OnlyRenderVisibleElements && _paneRect is not null ? VisibleFlowRect() : null;

        foreach (var n in Nodes)
        {
            if (n.Hidden) continue;
            // Elements are keyed by id, and Blazor refuses to diff a list with a repeated key.
            // Rendering only the node the id resolves to keeps a duplicated id a reported
            // mistake rather than a crash on the next render.
            if (!ReferenceEquals(_index.GetValueOrDefault(n.Id), n)) continue;
            // Always render not-yet-measured nodes so they can be sized at least once.
            if (view is { } v && !n.Measured.IsEmpty && !RectsIntersect(n.GetRect(), v)) continue;
            _renderNodes.Add(n);
        }

        // Render shallower nodes first so group containers sit behind their children. Flat
        // graphs (the common case) are already in order, so skip the sort entirely. In Manual
        // and Basic mode the nesting order is the caller's to decide, so it is left alone.
        if (ZIndexMode == BlowZIndexMode.Auto && _renderNodes.Exists(static n => n.ParentId is not null))
            StableSortByKey(_renderNodes, Depth);

        var edgeIndex = EdgeIndex;
        foreach (var e in Edges)
        {
            if (e.Hidden) continue;
            // Same reasoning as for nodes: one element per id, never two.
            if (!ReferenceEquals(edgeIndex.GetValueOrDefault(e.Id), e)) continue;
            // An edge is the line between two nodes. Hide one of them and there is nothing left
            // for it to be a line between, so drawing it anyway leaves it hanging in mid-air
            // pointing at a gap - which is what makes collapsing part of a graph look broken.
            if (!IsEdgeVisible(e)) continue;
            if (view is { } v && !EdgeIntersects(e, v)) continue;
            _renderEdges.Add(e);
        }

        // SVG has no z-index, so edge stacking is expressed purely by draw order.
        if (_renderEdges.Exists(e => e.ZIndex is not null || (ElevateEdgesOnSelect && e.Selected)))
            StableSortByKey(_renderEdges, EdgeZIndex);

        CollectMarkerDefs();
        EvictStaleEdgeContexts();
        EvictStaleCallbacks();
    }

    /// <summary>
    /// Drops cached geometry for edges that are no longer in the graph. The cache now outlives
    /// a render, so without this a long editing session would accumulate an entry per edge that
    /// ever existed. Nothing is scanned while the cache is no larger than the edge list, which
    /// is the case in every render that did not delete anything.
    /// </summary>
    private void EvictStaleEdgeContexts()
    {
        if (_edgeContexts.Count <= Edges.Count) return;

        var live = EdgeIndex;
        foreach (var id in _edgeContexts.Keys.ToArray())
        {
            if (!live.ContainsKey(id)) _edgeContexts.Remove(id);
        }
    }

    /// <summary>
    /// Gathers the distinct arrowheads the visible edges need, so <c>&lt;defs&gt;</c> declares
    /// exactly those and no more. Runs before anything references a marker url.
    /// </summary>
    private void CollectMarkerDefs()
    {
        _markerDefs.Clear();
        foreach (var e in _renderEdges)
        {
            if (!e.MarkerStart && !e.MarkerEnd) continue;
            var spec = e.ResolveMarker(DefaultMarkerColor);
            _markerDefs.TryAdd(MarkerKey(spec), spec);
        }
    }

    /// <summary>
    /// Order-preserving sort by an integer key. <c>List.Sort</c> is unstable, which would let
    /// same-depth nodes swap places between renders and thrash the diff.
    /// </summary>
    private static void StableSortByKey<T>(List<T> items, Func<T, int> key)
    {
        if (items.Count < 2) return;

        var keyed = new (int Key, int Order, T Item)[items.Count];
        for (var i = 0; i < items.Count; i++) keyed[i] = (key(items[i]), i, items[i]);

        Array.Sort(keyed, static (a, b) => a.Key != b.Key ? a.Key.CompareTo(b.Key) : a.Order.CompareTo(b.Order));

        for (var i = 0; i < keyed.Length; i++) items[i] = keyed[i].Item;
    }

    /// <summary>The currently visible region of flow space, expanded by a 20% margin on each side.</summary>
    private BlowRect VisibleFlowRect()
    {
        var topLeft = _viewport.ScreenToFlow(0, 0);
        var w = _paneRect!.Width / _viewport.Zoom;
        var h = _paneRect.Height / _viewport.Zoom;
        var mx = w * 0.2;
        var my = h * 0.2;
        return new BlowRect(topLeft.X - mx, topLeft.Y - my, w + 2 * mx, h + 2 * my);
    }

    /// <summary>
    /// Cheap visibility test for an edge: resolves the two anchors without building the path,
    /// so culling a large graph doesn't pay for the geometry of the edges it throws away.
    /// </summary>
    private bool EdgeIntersects(BlowEdge edge, BlowRect view)
    {
        var index = _index;
        if (!index.TryGetValue(edge.Source, out var source) || !index.TryGetValue(edge.Target, out var target))
            return false;

        var (sx, sy, _) = ResolveAnchor(source, BlowHandleType.Source, edge.SourceHandle, source.SourcePosition);
        var (tx, ty, _) = ResolveAnchor(target, BlowHandleType.Target, edge.TargetHandle, target.TargetPosition);

        var minX = Math.Min(sx, tx);
        var minY = Math.Min(sy, ty);

        // Every path type leaves its endpoints' bounding box: a bezier's control points reach out
        // along the handle normals, a step edge runs its offset straight out before turning, and a
        // self-loop lives entirely outside. Culling on the bare box would drop an edge whose curve
        // is on screen even though neither of its ends is - the exact case a long edge across a
        // large graph is in. The box is padded by the furthest any of them can stray.
        var slack = EdgeCullingSlack(edge, sx, sy, tx, ty, ReferenceEquals(source, target) ? source.EffectiveSize : null);

        var rect = new BlowRect(
            minX - slack, minY - slack,
            Math.Abs(tx - sx) + 1 + 2 * slack, Math.Abs(ty - sy) + 1 + 2 * slack);
        return RectsIntersect(rect, view);
    }

    /// <summary>
    /// An upper bound on how far the edge's path can stray outside the box its endpoints span.
    /// Deliberately generous: over-estimating renders an edge that was not needed, while
    /// under-estimating makes one vanish.
    /// </summary>
    /// <remarks><paramref name="loopNode"/> is the node's size when the edge loops back to it, else null.</remarks>
    private static double EdgeCullingSlack(BlowEdge edge, double sx, double sy, double tx, double ty, BlowDimensions? loopNode)
    {
        var options = edge.PathOptions;

        // A loop swings round its node, clearing the far side of it by the radius; its control
        // points sit that far out again divided by the 0.75 a cubic reaches towards them.
        if (loopNode is { } size)
        {
            var radius = Math.Max(1, options?.LoopRadius ?? BlowEdgePath.DefaultLoopRadius);
            return (Math.Max(size.Width, size.Height) + radius) / 0.75 + radius;
        }

        return edge.Type switch
        {
            BlowEdgeType.Straight => 0,
            BlowEdgeType.Step or BlowEdgeType.SmoothStep => options?.Offset ?? 20,
            // A bezier control point reaches out by half the gap when the handles face each other,
            // and by curvature * 25 * sqrt(gap) when they face away.
            _ => BezierReach(Math.Abs(tx - sx), options?.Curvature ?? 0.25)
                 + BezierReach(Math.Abs(ty - sy), options?.Curvature ?? 0.25),
        };

        static double BezierReach(double distance, double curvature)
            => Math.Max(0.5 * distance, curvature * 25 * Math.Sqrt(distance));
    }

    /// <summary>
    /// Rebuilds the id-&gt;node lookup. Duplicate ids are reported rather than thrown on, so a
    /// user mistake degrades gracefully; the first node with a given id is the one that counts,
    /// both here and in the render list.
    /// </summary>
    private void RebuildIndex()
    {
        _index.Clear();
        foreach (var n in Nodes)
        {
            if (!_index.TryAdd(n.Id, n))
                ReportError(BlowErrorCode.DuplicateNodeId, n.Id, $"More than one node uses the id '{n.Id}'.");
        }
        _indexDirty = false;
        _nodeGrid = null;
    }

    /// <summary>Ensures the id lookup reflects the current node list (used outside the render path).</summary>
    private Dictionary<string, BlowNode> Index
    {
        get
        {
            if (_indexDirty || _index.Count != Nodes.Count) RebuildIndex();
            return _index;
        }
    }

    /// <summary>
    /// Resolves each node's absolute position by walking its parent chain, then applies node
    /// origin. Runs in O(n): parent chains are memoized and the id lookup is built once.
    /// </summary>
    private void ComputeLayout()
    {
        RebuildIndex();
        _rawAbsolute.Clear();
        _depth.Clear();

        foreach (var n in Nodes) ResolveRaw(n, 0);

        foreach (var n in Nodes)
        {
            var raw = _rawAbsolute[n.Id];
            var origin = n.Origin ?? NodeOrigin;
            if (origin.X == 0 && origin.Y == 0)
            {
                n.AbsolutePosition = raw;
            }
            else
            {
                var size = n.EffectiveSize;
                n.AbsolutePosition = new BlowXYPosition(raw.X - origin.X * size.Width, raw.Y - origin.Y * size.Height);
            }
        }
    }

    /// <summary>
    /// Memoized parent-chain walk returning the un-origin-adjusted absolute position and the
    /// nesting depth. The depth guard also breaks accidental parent cycles.
    /// </summary>
    private (BlowXYPosition Position, int Depth) ResolveRaw(BlowNode n, int guard)
    {
        if (_rawAbsolute.TryGetValue(n.Id, out var cached))
            return (cached, _depth[n.Id]);

        var pos = n.Position;
        var depth = 0;

        if (guard < MaxParentDepth && n.ParentId is { } pid)
        {
            if (_index.TryGetValue(pid, out var parent) && (ReferenceEquals(parent, n) || _resolving.Contains(parent.Id)))
            {
                // The parent is this node, or is still waiting on this node's own position: a cycle. Walking it
                // would add the same offsets round and round until the depth guard gave up, flinging every
                // node on it far off screen, so it is cut here and this node laid out as a root.
                ReportError(BlowErrorCode.ParentCycle, n.Id,
                    $"Node '{n.Id}' is its own ancestor: following its parents leads back to it. "
                    + "It is laid out as a root node instead.");
            }
            else if (parent is not null)
            {
                _resolving.Add(n.Id);
                var (pp, pd) = ResolveRaw(parent, guard + 1);
                _resolving.Remove(n.Id);
                pos = new BlowXYPosition(pp.X + n.Position.X, pp.Y + n.Position.Y);
                depth = pd + 1;
            }
            else
            {
                ReportError(BlowErrorCode.MissingParentNode, n.Id,
                    $"Node '{n.Id}' names parent '{pid}', which is not in the node list; "
                    + "it is laid out as a root node instead.");
            }
        }

        _rawAbsolute[n.Id] = pos;
        _depth[n.Id] = depth;
        return (pos, depth);
    }

    /// <summary>The nodes whose parent chain is being resolved right now, for <see cref="ResolveRaw"/> to spot a cycle.</summary>
    private readonly HashSet<string> _resolving = [];

    private int Depth(BlowNode n) => _depth.TryGetValue(n.Id, out var d) ? d : 0;

    private int EdgeZIndex(BlowEdge e)
    {
        if (e.ZIndex is { } explicitZ) return explicitZ;
        if (ZIndexMode == BlowZIndexMode.Manual) return 0;
        return ElevateEdgesOnSelect && e.Selected ? 1 : 0;
    }

    private bool HasAncestorIn(BlowNode node, HashSet<string> ids)
    {
        var index = Index;
        var current = node;
        int guard = 0;
        while (guard++ < MaxParentDepth && current.ParentId is { } pid && index.TryGetValue(pid, out var parent))
        {
            if (ids.Contains(pid)) return true;
            current = parent;
        }
        return false;
    }

    internal BlowEdgeContext GetEdgeContext(BlowEdge edge)
    {
        // The edge layer, the reconnect anchors and the HTML label overlay all ask for the same
        // edge within one render, and across renders the answer usually has not changed either:
        // panning and zooming move the whole viewport layer without touching flow-space geometry.
        // Keeping the cache and checking what the path actually depends on turns a pan of a large
        // graph from "rebuild every path string" into a dictionary lookup - and hands the diff
        // back the identical strings, so nothing is written to the DOM.
        var key = EdgeGeometryKey(edge);
        if (_edgeContexts.TryGetValue(edge.Id, out var cached) && !ReferenceEquals(cached.Edge, edge)) cached = null;
        if (cached is not null && cached.GeometryKey == key) return cached;

        var ctx = BuildEdgeContext(edge, key, cached);
        _edgeContexts[edge.Id] = ctx;
        return ctx;
    }

    /// <summary>
    /// Everything the computed path and markers depend on. Two renders that agree on this
    /// produce byte-identical geometry, so the cached context can be handed back untouched.
    /// </summary>
    private BlowEdgeGeometryKey EdgeGeometryKey(BlowEdge edge)
    {
        _ = Index;
        var source = DisplayedEndpoint(edge.Source, out var sourceMoved);
        var target = DisplayedEndpoint(edge.Target, out var targetMoved);

        var (sx, sy, sPos) = source is null
            ? (0d, 0d, BlowPosition.Bottom)
            : EndpointAnchor(source, sourceMoved, BlowHandleType.Source, edge.SourceHandle);
        var (tx, ty, tPos) = target is null
            ? (0d, 0d, BlowPosition.Top)
            : EndpointAnchor(target, targetMoved, BlowHandleType.Target, edge.TargetHandle);

        var options = edge.PathOptions;
        var selfLoop = IsSelfLoop(edge, source, target);
        return new BlowEdgeGeometryKey(
            source is not null && target is not null,
            selfLoop,
            // A loop is routed round its node, so the node's own box shapes it too.
            selfLoop ? source!.GetRect() : default,
            sx, sy, sPos, tx, ty, tPos,
            edge.Type,
            options?.Curvature, options?.BorderRadius, options?.Offset, options?.StepPosition,
            options?.LoopRadius,
            edge.MarkerStart, edge.MarkerEnd, edge.ResolveMarker(DefaultMarkerColor),
            // A routed edge goes round the other nodes, so any of them moving can change its path.
            edge.Type == BlowEdgeType.Routed && !selfLoop ? _geometryVersion : 0);
    }

    /// <param name="edge">The edge.</param>
    /// <param name="key">What its geometry is computed from now.</param>
    /// <param name="previous">The context the edge was last drawn with, if any, for a route that has not changed.</param>
    private BlowEdgeContext BuildEdgeContext(BlowEdge edge, BlowEdgeGeometryKey key, BlowEdgeContext? previous)
    {
        var index = Index;
        if (!index.TryGetValue(edge.Source, out var source) || !index.TryGetValue(edge.Target, out var target))
        {
            var missing = index.ContainsKey(edge.Source) ? edge.Target : edge.Source;
            ReportError(BlowErrorCode.MissingEdgeEndpoint, edge.Id,
                $"Edge '{edge.Id}' references node '{missing}', which is not in the node list.");

            return new BlowEdgeContext
            {
                Edge = edge,
                Geometry = new BlowPathResult(string.Empty, 0, 0),
                GeometryKey = key,
                MarkerStartUrl = MarkerUrl(edge, edge.MarkerStart),
                MarkerEndUrl = MarkerUrl(edge, edge.MarkerEnd),
            };
        }

        source = DisplayedEndpoint(edge.Source, out var sourceMoved)!;
        target = DisplayedEndpoint(edge.Target, out var targetMoved)!;
        var (sx, sy, sPos) = EndpointAnchor(source, sourceMoved, BlowHandleType.Source, edge.SourceHandle);
        var (tx, ty, tPos) = EndpointAnchor(target, targetMoved, BlowHandleType.Target, edge.TargetHandle);

        // An edge that comes back to the node it left has no meaningful "between the two nodes"
        // for an ordinary path algorithm to route through, so it is looped instead.
        var geometry = IsSelfLoop(edge, source, target)
            ? BlowEdgePath.Loop(sx, sy, sPos, tx, ty, tPos,
                edge.PathOptions?.LoopRadius ?? BlowEdgePath.DefaultLoopRadius, source.GetRect())
            : edge.Type == BlowEdgeType.Routed && RouteAround(edge, source, target, sx, sy, sPos, tx, ty, tPos) is { } routed
                ? routed
                : BlowEdgePath.Compute(edge.Type, sx, sy, sPos, tx, ty, tPos, edge.PathOptions);

        // A routed edge is asked again whenever any node moved, since any of them may be in its way. A route that comes
        // back the same leaves the context it was drawn with right as it is: handed back, the edge does not render
        // again for nothing.
        if (previous is not null && previous.Geometry == geometry
            && previous.GeometryKey with { RouteVersion = key.RouteVersion } == key)
        {
            previous.GeometryKey = key;
            return previous;
        }

        return new BlowEdgeContext
        {
            Edge = edge,
            Geometry = geometry,
            GeometryKey = key,
            Source = new BlowXYPosition(sx, sy),
            Target = new BlowXYPosition(tx, ty),
            SourcePosition = sPos,
            TargetPosition = tPos,
            MarkerStartUrl = MarkerUrl(edge, edge.MarkerStart),
            MarkerEndUrl = MarkerUrl(edge, edge.MarkerEnd),
        };
    }

    /// <summary>
    /// The path of a <see cref="BlowEdgeType.Routed"/> edge from <see cref="Routing"/>, round the shown nodes near it -
    /// its own two included, so it does not cut back across them, but not the groups either end sits in, which it has
    /// to run inside. Null when no route was found, for the caller to fall back to a smooth step.
    /// </summary>
    private BlowPathResult? RouteAround(BlowEdge edge, BlowNode source, BlowNode target, double sx, double sy, BlowPosition sPos,
        double tx, double ty, BlowPosition tPos)
    {
        var index = Index;
        var area = BlowEdgeLayout.ObstacleArea(source.GetRect(), target.GetRect());
        var obstacles = BlowEdgeLayout.Obstacles(ShownNodesNear(area), area, source, target, index, MaxParentDepth);

        // The edge's geometry is asked for again whenever any node moved; the router is only asked again when its own
        // ends, or what is near enough to be in its way, did - or the router itself was replaced.
        var router = Routing ?? BlowRoutingOptions.Default;
        // The built-in router's settings by value, as an edge's path options are: tuning them in place re-routes.
        var settings = router is BlowRoutingOptions o ? (o.Margin, o.StubLength, o.BendPenalty, o.BorderRadius, o.MaxObstacles) : default;
        var request = new RouteKey(new BlowXYPosition(sx, sy), sPos, new BlowXYPosition(tx, ty), tPos, router, settings);
        if (_routes.TryGetValue(edge.Id, out var cached)
            && cached.Key == request && cached.Obstacles.SequenceEqual(obstacles))
        {
            return cached.Result;
        }

        var result = router.Route(new BlowRouteRequest(edge, request.Source, sPos, request.Target, tPos, obstacles));
        if (result is { Path: null }) result = null;
        if (_routes.Count > 2 * Edges.Count + 64) _routes.Clear();
        _routes[edge.Id] = (request, obstacles, result);
        return result;
    }

    /// <summary>
    /// What a route was asked for with, besides the edge's id and the obstacles: its anchors, the router by reference
    /// and the built-in router's settings by value. Not the edge object: a host that builds its edges afresh on every
    /// render keeps its routes.
    /// </summary>
    private readonly record struct RouteKey(BlowXYPosition Source, BlowPosition SourceSide, BlowXYPosition Target,
        BlowPosition TargetSide, IBlowEdgeRouter Router, (double, double, double, double, int) Settings);

    /// <summary>The last route found for each edge, and what it went round.</summary>
    private readonly Dictionary<string, (RouteKey Key, List<BlowRect> Obstacles, BlowPathResult? Result)> _routes = new(StringComparer.Ordinal);

    /// <summary>
    /// Every node by where it was at the last layout, for each routed edge to find the nodes near it without looking
    /// at all of them; built on first use after the index is next rebuilt - every layout, every change to the list.
    /// </summary>
    private BlowSpatialGrid<BlowNode>? _nodeGrid;

    /// <summary>The shown nodes whose boxes may meet <paramref name="area"/>, each with its box as it is now.</summary>
    private IEnumerable<(string Id, BlowRect Rect)> ShownNodesNear(BlowRect area)
    {
        if (_nodeGrid is null)
        {
            _nodeGrid = new BlowSpatialGrid<BlowNode>();
            foreach (var node in Nodes) _nodeGrid.Add(node, node.GetRect());
        }

        foreach (var node in _nodeGrid.Near(area))
        {
            if (!node.Hidden) yield return (node.Id, node.GetRect());
        }
    }

    /// <summary>
    /// True when both ends of the edge land on the same node. Compared by reference rather than
    /// by id so a graph carrying a duplicate id still agrees with what was actually rendered.
    /// </summary>
    private static bool IsSelfLoop(BlowEdge edge, BlowNode? source, BlowNode? target)
        => source is not null && ReferenceEquals(source, target);

    /// <summary>
    /// Whether the edge has two visible nodes to run between. An edge naming a node that is not
    /// in the list at all is still drawn (as nothing) so the missing endpoint is reported once
    /// through <see cref="OnError"/>; one naming a hidden node is simply not drawn, because
    /// nothing is wrong - something has been collapsed.
    /// </summary>
    private bool IsEdgeVisible(BlowEdge edge)
    {
        var source = DisplayedEndpoint(edge.Source, out var sourceMoved);
        var target = DisplayedEndpoint(edge.Target, out var targetMoved);
        if (source is { Hidden: true } || target is { Hidden: true }) return false;

        // Both ends folded into the same collapsed node: the edge is inside it, out of sight with the rest.
        return !((sourceMoved || targetMoved) && source is not null && ReferenceEquals(source, target));
    }

    /// <summary>
    /// The node an edge is drawn to for one of its ends: the node itself, or - when it is hidden inside a collapsed
    /// node - the nearest collapsed ancestor that is on show, with <paramref name="moved"/> set. Null when the id names
    /// no node.
    /// </summary>
    private BlowNode? DisplayedEndpoint(string nodeId, out bool moved)
        => BlowEdgeLayout.DisplayedEnd(_index, nodeId, static n => !n.Hidden, MaxParentDepth, out moved);

    /// <summary>
    /// Where one end of an edge is drawn: at its handle, or - for an end folded into a collapsed node - at that node's
    /// side, since the handle it named is out of sight.
    /// </summary>
    private (double x, double y, BlowPosition pos) EndpointAnchor(BlowNode node, bool moved, BlowHandleType type, string? handleId)
        => ResolveAnchor(node, type, moved ? null : handleId,
            type == BlowHandleType.Source ? node.SourcePosition : node.TargetPosition);

    private string? MarkerUrl(BlowEdge edge, bool show)
    {
        if (!show) return null;

        var spec = edge.ResolveMarker(DefaultMarkerColor);

        // An edge culled out of the render list (or reached before PrepareRender) still needs a
        // usable url, so register on demand rather than assuming the collection pass saw it.
        _markerDefs.TryAdd(MarkerKey(spec), spec);

        return $"url(#{_markerPrefix}-{MarkerKey(spec)})";
    }

    private (double x, double y, BlowPosition pos) ResolveAnchor(
        BlowNode node, BlowHandleType type, string? handleId, BlowPosition fallback)
    {
        var key = BlowHandleBounds.Key(type, handleId);
        if (node.Handles.TryGetValue(key, out var h)
            // In Loose mode either end can be a handle of either type - a source dropped on another source names that
            // source as the edge's target - so the handle is looked for among the other type too, as React Flow does.
            || (ConnectionMode == BlowConnectionMode.Loose
                && node.Handles.TryGetValue(BlowHandleBounds.Key(
                    type == BlowHandleType.Source ? BlowHandleType.Target : BlowHandleType.Source, handleId), out h)))
            return (node.AbsolutePosition.X + h.OffsetX, node.AbsolutePosition.Y + h.OffsetY, h.Position);

        // No measured handle: anchor on the fallback side at the node's edge midpoint.
        var size = node.EffectiveSize;
        var p = node.AbsolutePosition;
        return fallback switch
        {
            BlowPosition.Top => (p.X + size.Width / 2, p.Y, fallback),
            BlowPosition.Bottom => (p.X + size.Width / 2, p.Y + size.Height, fallback),
            BlowPosition.Left => (p.X, p.Y + size.Height / 2, fallback),
            _ => (p.X + size.Width, p.Y + size.Height / 2, fallback),
        };
    }

    /// <summary>
    /// Whether to draw the in-progress connection line. A press that has not travelled far
    /// enough yet is still a click on the handle, and flashing a zero-length line under the
    /// cursor for it looks like a glitch. Click-to-connect is exempt: there the line is the
    /// only feedback that a connection is pending.
    /// </summary>
    private bool ShowConnectionLine => _connecting && (_connMoved || ConnectOnClick || _keyboardConnecting);

    private string ConnectionLinePath()
    {
        var s = _connSource;
        var endPos = OppositePosition(s.Position);
        var result = BlowEdgePath.Compute(ConnectionLineType, s.Anchor.X, s.Anchor.Y, s.Position, _connEnd.X, _connEnd.Y, endPos);
        return result.Path;
    }

    /// <summary>Builds the live connection state for the connection-line template / OnConnectionChange.</summary>
    private BlowConnectionLineContext BuildConnectionLineContext()
    {
        var s = _connSource;
        var valid = _connSnap is not null
                    || (_connHover is { } h && HoverCheck(h.nodeId, h.handleId, h.type).IsValid);
        return new BlowConnectionLineContext
        {
            FromNode = s.NodeId,
            FromHandle = s.HandleId,
            FromHandleType = s.Type,
            FromPosition = s.Position,
            From = s.Anchor,
            To = _connEnd,
            ToPosition = OppositePosition(s.Position),
            IsValid = valid,
            Rejection = valid ? BlowConnectionRejection.None : HoverRejection(),
        };
    }

    /// <summary>
    /// Why the handle the line currently ends at would refuse it: the handle under the pointer when the canvas
    /// knows it, else the nearest one within snapping distance, else nothing.
    /// </summary>
    private BlowConnectionRejection HoverRejection()
    {
        if (_connHover is { } h) return HoverCheck(h.nodeId, h.handleId, h.type).Rejection;
        return _connNear?.Rejection ?? BlowConnectionRejection.None;
    }

    private void NotifyConnectionChange()
    {
        if (OnConnectionChange.HasDelegate)
            _ = OnConnectionChange.InvokeAsync(_connecting ? BuildConnectionLineContext() : null);
    }

    // ---- pane interactions ----

    /// <summary>True when the configured multi-selection modifier is held.</summary>
    private bool IsMultiSelect(MouseEventArgs e) => IsModifierHeld(MultiSelectionKey, e);

    private static bool IsModifierHeld(BlowModifierKey key, MouseEventArgs e) => key switch
    {
        BlowModifierKey.Control => e.CtrlKey,
        BlowModifierKey.Alt => e.AltKey,
        BlowModifierKey.Meta => e.MetaKey,
        _ => e.ShiftKey,
    };

    private async Task OnPanePointerDown(PointerEventArgs e)
    {
        // Reaching here means the pointer hit the pane background (nodes/handles stop propagation).
        // Self-healing reset: if a previous gesture never produced its click (released off-page,
        // say), the stale flags are cleared here rather than acting on the next real click.
        _suppressNextClick = false;
        _clearSelectionOnClick = false;
        _pressedNode = null;
        _clickedNode = null;

        TrackPointer(e);
        if (TryStartPinch()) return;

        // A non-primary button can only ever pan (there is nothing to select or connect with it).
        if (e.Button != 0)
        {
            if (PanOnDrag && PanButtons.Contains((int)e.Button))
            {
                await StartPanAsync(e);
                return;
            }

            // Nothing captures this pointer, so its release is not guaranteed to come back here
            // (a right-click's context menu swallows it on some platforms). Left tracked, it
            // would keep the canvas believing a gesture is running until the next press.
            _activePointers.Remove(e.PointerId);
            return;
        }

        // A pending click-connection is cancelled by clicking empty space - and so is one made from the keyboard,
        // which then says so.
        if (_connecting)
        {
            var cancelled = KeyboardCancelledText;
            RaiseConnectionEnd(TakeConnectionEnd());
            if (cancelled is not null) Announce(cancelled);

            // Only the connection line and the handles change; see AbandonKeyboardConnection.
            RenderOverlay();
            return;
        }

        if (!PanButtons.Contains(0)) return;

        var multi = IsMultiSelect(e);

        // Holding the pan-activation key forces a pan, overriding selection-on-drag.
        if (PanOnDrag && _panKeyHeld)
        {
            await StartPanAsync(e);
            return;
        }

        // Box selection: while the selection key is held, or on a plain drag when SelectionOnDrag
        // is enabled. The two keys are separate settings but default to the same one, so holding
        // it both opens the box and adds what it catches to the existing selection. The
        // multi-selection key on its own only makes a box additive; it does not open one, which
        // is what lets a null SelectionKey really mean "only SelectionOnDrag draws a box".
        var selectionKeyHeld = SelectionKey is { } selKey && IsModifierHeld(selKey, e);

        if (ElementsSelectable && (selectionKeyHeld || SelectionOnDrag))
        {
            _selAdditive = multi;

            // Recorded whatever the mode: an additive box merges with it, and either way it is
            // what the selection goes back to if the gesture is abandoned with Escape.
            _preSelected.Clear();
            foreach (var n in Nodes)
                if (n.Selected) _preSelected.Add(NodeKey(n.Id));
            foreach (var ed in Edges)
                if (ed.Selected) _preSelected.Add(EdgeKey(ed.Id));

            // The gesture is armed before anything is awaited. Over a Blazor Server circuit a
            // round trip can outlast a quick click, and a release handled in that gap would find
            // no box to finish - leaving one that follows the pointer with no button held.
            _selecting = true;
            _selStart = ScreenToFlow(e);
            _selEnd = _selStart;
            CapturePane(e.PointerId);

            if (!_selAdditive)
            {
                ClearSelection();
                await NotifySelectionChangedAsync();
            }

            // The pane may have moved on the page since the cached rectangle was taken (content
            // above it changed size, say); re-anchor the box on the refreshed one.
            var rectBefore = _paneRect;
            var startBefore = _selStart;
            await RefreshPaneRectAsync();
            if (_selecting && rectBefore != _paneRect)
            {
                var fresh = ScreenToFlow(e);
                if (_selEnd == startBefore) _selEnd = fresh;
                _selStart = fresh;
            }

            await OnSelectionStart.InvokeAsync(e);
            StateHasChanged();
            return;
        }

        // Whether this press is a click that deselects or the start of a pan is only known once the
        // pointer is released, so the selection is cleared by the click, not here: panning to see
        // more of the graph must not throw away what was selected in it.
        _clearSelectionOnClick = ElementsSelectable;

        if (PanOnDrag) await StartPanAsync(e);
    }

    private async Task StartPanAsync(PointerEventArgs e)
    {
        // The user has taken the viewport back; a transition still easing towards somewhere else
        // would fight the drag for the same transform.
        StopViewportAnimation();

        // Armed before anything is awaited: over a Blazor Server circuit a round trip can outlast
        // a quick click, and a release handled in that gap would find no pan to end - leaving one
        // that follows the pointer with no button held.
        _panning = true;
        _panStartX = e.ClientX;
        _panStartY = e.ClientY;
        _panStartViewport = _viewport;
        CapturePane(e.PointerId);
        await OnMoveStart.InvokeAsync(_viewport);

        // A pan works in screen deltas, so the rectangle is only refreshed for what comes after.
        await RefreshPaneRectAsync();
    }

    /// <summary>
    /// Moves the dragged nodes to follow the pointer. The offset is measured in <em>flow</em>
    /// space rather than screen pixels so the drag stays correct if the viewport pans or zooms
    /// mid-gesture (which is exactly what auto-pan does).
    /// </summary>
    private void ApplyNodeDrag(double clientX, double clientY)
    {
        var dxRaw = clientX - _dragStartClientX;
        var dyRaw = clientY - _dragStartClientY;
        var travel = Math.Sqrt(dxRaw * dxRaw + dyRaw * dyRaw);

        // Past the click distance this is a drag, not a click, so the click that the browser
        // fires on pointer-up must not be reported as a node click.
        if (travel > NodeClickDistance) _suppressNextClick = true;

        // Hold off moving until the pointer has travelled past the drag threshold.
        if (!_dragArmed)
        {
            if (travel < NodeDragThreshold) return;
            _dragArmed = true;
        }

        var current = ScreenToFlowPosition(clientX, clientY);
        var dx = current.X - _dragStartFlow.X;
        var dy = current.Y - _dragStartFlow.Y;

        // Guides act on the whole drag at once: the primary node decides what to line up with,
        // and everything moving with it shifts by the same amount, so the group keeps its shape.
        var (snapX, snapY) = ResolveGuides(ref dx, ref dy);

        // Indexed rather than foreach: growing a parent leftwards renumbers the coordinates
        // inside it, and ExpandParentFor rewrites the remembered start positions to match.
        for (var i = 0; i < _dragSet.Count; i++)
        {
            var (node, start) = _dragSet[i];
            var moved = new BlowXYPosition(start.X + dx, start.Y + dy);

            // Grid snapping only applies on an axis the guides did not already claim; letting
            // both act would drag the node straight back off the guide it just found.
            if (SnapToGrid)
                moved = new BlowXYPosition(snapX ? moved.X : SnapAxis(moved.X, SnapStepX),
                                       snapY ? moved.Y : SnapAxis(moved.Y, SnapStepY));

            node.Position = ClampNodePosition(node, moved);
            ExpandParentFor(node);
        }

        if (_dragPrimary is not null)
            _ = OnNodeDrag.InvokeAsync(_dragPrimary);

        if (IsSelectionDrag && OnSelectionDrag.HasDelegate)
        {
            // Auto-pan drives this from a remembered pointer position rather than a live event,
            // so the coordinates are carried across on their own rather than a DOM event faked up.
            _ = OnSelectionDrag.InvokeAsync(new BlowSelectionMouseEvent(
                DraggedNodes(), new MouseEventArgs { ClientX = clientX, ClientY = clientY }));
        }
    }

    /// <summary>
    /// Adjusts the drag delta so the primary node lines up with a neighbour, and records the
    /// guides to draw. Returns which axes were claimed, so grid snapping can stay off those.
    /// </summary>
    private (bool X, bool Y) ResolveGuides(ref double dx, ref double dy)
    {
        if (!SnapToGuides || _dragPrimary is null)
        {
            _guides = [];
            return (false, false);
        }

        var moving = new BlowRect(
            _dragPrimaryStartRect.X + dx, _dragPrimaryStartRect.Y + dy,
            _dragPrimaryStartRect.Width, _dragPrimaryStartRect.Height);

        // Screen pixels, so the snap feels equally forgiving whatever the zoom.
        var tolerance = GuideDistance / _viewport.Zoom;
        var result = BlowAlignment.Snap(moving, StationaryRects(), tolerance);

        _guides = result.Guides;
        dx += result.OffsetX;
        dy += result.OffsetY;
        return (result.OffsetX != 0, result.OffsetY != 0);
    }

    /// <summary>
    /// The rectangles a dragged node can line up against: everything visible that is not moving
    /// with the drag. Nodes moving together must not attract each other.
    /// </summary>
    private IEnumerable<BlowRect> StationaryRects()
    {
        var moving = DragSubtree();
        foreach (var n in Nodes)
        {
            if (n.Hidden || moving.Contains(n.Id)) continue;
            yield return n.GetRect();
        }
    }

    /// <summary>Ids of everything the running drag moves, filled on first use. See <see cref="DragSubtree"/>.</summary>
    private HashSet<string>? _dragSubtree;

    /// <summary>
    /// The dragged nodes and all of their descendants. A child rides along with its dragged parent
    /// without being marked <see cref="BlowNode.Dragging"/>, and its rectangle still holds where the
    /// parent was a frame ago - so a parent lining up with its own children would be pinned to its
    /// previous position.
    /// </summary>
    private HashSet<string> DragSubtree()
    {
        if (_dragSubtree is not null) return _dragSubtree;

        var ids = new HashSet<string>();
        foreach (var (node, _) in _dragSet) ids.Add(node.Id);
        foreach (var n in Nodes)
        {
            if (!ids.Contains(n.Id) && HasAncestorIn(n, ids)) ids.Add(n.Id);
        }
        return _dragSubtree = ids;
    }

    private void UpdateConnectionEnd(double clientX, double clientY)
    {
        _connEnd = ScreenToFlowPosition(clientX, clientY);
        _connEndClientX = clientX;
        _connEndClientY = clientY;

        // The pane holds the pointer for the whole drag, so the handles never see it arrive and
        // cannot report hovering. Whether the line currently ends where it could connect is
        // worked out from its position instead - the same snap a release would use - and only
        // when something is going to show it.
        if (OnConnectionChange.HasDelegate || ConnectionLineTemplate is not null)
        {
            var (valid, nearest) = ClosestHandles();
            _connSnap = valid;
            _connNear = nearest;
        }
        else
        {
            _connSnap = null;
            _connNear = null;
        }

        NotifyConnectionChange();
    }

    // ---- auto-pan (drag a node or connection past the pane border) ----

    /// <summary>
    /// Starts or stops the auto-pan loop based on how close the pointer is to the pane border.
    /// The loop runs on a timer rather than off pointer-move so the viewport keeps scrolling
    /// while the pointer is held still against the edge.
    /// </summary>
    private void UpdateAutoPan(double clientX, double clientY)
    {
        var enabled = (_draggingNodes && AutoPanOnNodeDrag) || (_connecting && AutoPanOnConnect);
        if (!enabled || _paneRect is null || AutoPanMargin <= 0)
        {
            StopAutoPan();
            return;
        }

        var x = clientX - _paneRect.Left;
        var y = clientY - _paneRect.Top;

        var dx = x < AutoPanMargin ? 1
            : x > _paneRect.Width - AutoPanMargin ? -1
            : 0;
        var dy = y < AutoPanMargin ? 1
            : y > _paneRect.Height - AutoPanMargin ? -1
            : 0;

        if (dx == 0 && dy == 0)
        {
            StopAutoPan();
            return;
        }

        _autoPanVector = (dx * AutoPanSpeed, dy * AutoPanSpeed);
        _autoPanPointer = (clientX, clientY);
        StartAutoPan();
    }

    private void StartAutoPan()
    {
        if (_autoPanCts is not null) return;
        _autoPanCts = new CancellationTokenSource();
        _ = RunAutoPanAsync(_autoPanCts.Token);
    }

    private void StopAutoPan()
    {
        if (_autoPanCts is null) return;
        _autoPanCts.Cancel();
        _autoPanCts.Dispose();
        _autoPanCts = null;

        // The viewport has stopped where the auto-pan left it: the end of a move, as a pan's end is.
        if (!_autoPanned) return;
        _autoPanned = false;
        if (!_disposed) _ = RaiseMoveEndAsync();

        async Task RaiseMoveEndAsync()
        {
            await OnViewportChanged.InvokeAsync(_viewport);
            await OnMoveEnd.InvokeAsync(_viewport);
        }
    }

    private async Task RunAutoPanAsync(CancellationToken token)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromMilliseconds(16));
        var last = System.Diagnostics.Stopwatch.GetTimestamp();
        try
        {
            while (await timer.WaitForNextTickAsync(token))
            {
                await InvokeAsync(() =>
                {
                    if (!_draggingNodes && !_connecting)
                    {
                        StopAutoPan();
                        return;
                    }

                    // AutoPanSpeed is per sixtieth of a second, and the timer's ticks are not: WebKit runs them at half
                    // that rate, and a busy thread drops some. Scaled by the time that really passed - but never by more
                    // than a few frames' worth, so a stall does not throw the view.
                    var now = System.Diagnostics.Stopwatch.GetTimestamp();
                    var frames = Math.Clamp(System.Diagnostics.Stopwatch.GetElapsedTime(last, now).TotalMilliseconds * 60 / 1000, 0, 4);
                    last = now;

                    var (vx, vy) = _autoPanVector;
                    if (!_autoPanned) _ = OnMoveStart.InvokeAsync(_viewport);
                    _autoPanned = true;
                    _viewport = ClampViewport(_viewport with { X = _viewport.X + vx * frames, Y = _viewport.Y + vy * frames });

                    // The pointer hasn't moved, but the world under it has: re-derive whatever
                    // the active gesture is tracking so the node/connection follows the pan.
                    var (px, py) = _autoPanPointer;
                    if (_draggingNodes) ApplyNodeDrag(px, py);
                    else if (_connecting) UpdateConnectionEnd(px, py);

                    _ = OnMove.InvokeAsync(_viewport);
                    if (_draggingNodes) RenderGeometry();
                    else RenderOverlay();
                });
            }
        }
        catch (OperationCanceledException) { /* the gesture ended */ }
        // The loop outlives a single render, so the component can be torn down between ticks.
        // That is a normal end to the gesture, not a fault to surface on an unobserved task.
        catch (ObjectDisposedException) { }
    }

    // ---- pinch to zoom (two pointers on the pane) ----

    private void TrackPointer(PointerEventArgs e) => _activePointers[e.PointerId] = (e.ClientX, e.ClientY);

    /// <summary>
    /// Promotes a second simultaneous pointer into a pinch gesture, abandoning whatever
    /// single-pointer gesture was in progress. Returns true when a pinch took over.
    /// </summary>
    private bool TryStartPinch()
    {
        if (!ZoomOnPinch || _activePointers.Count < 2 || _pinching) return false;

        StopViewportAnimation();

        // Drop any single-pointer gesture that the first finger had started, exactly the way
        // Escape would: a half-dragged node goes back, a half-drawn box gives the selection back,
        // a resize is undone. A pan is the one exception in spirit - it was already moving the
        // viewport, so the pinch simply carries on the same move rather than ending it and
        // starting another.
        var wasPanning = _panning;
        _panning = false;
        _clearSelectionOnClick = false;
        CancelActiveGesture();

        _pinching = true;
        _pinchStartDistance = PointerDistance();
        _pinchStartViewport = _viewport;
        _ = RefreshPaneRectAsync();
        if (!wasPanning) _ = OnMoveStart.InvokeAsync(_viewport);
        StateHasChanged();
        return true;
    }

    private void ApplyPinch()
    {
        if (_activePointers.Count < 2 || _pinchStartDistance <= 0) return;

        var scale = PointerDistance() / _pinchStartDistance;
        var newZoom = Math.Clamp(_pinchStartViewport.Zoom * scale, MinZoom, MaxZoom);

        // Anchor the zoom on the midpoint between the two fingers, so the content under the
        // pinch stays put the way it does in native map/photo gestures.
        var (midX, midY) = PointerMidpoint();
        var px = midX - (_paneRect?.Left ?? 0);
        var py = midY - (_paneRect?.Top ?? 0);
        var flowX = (px - _pinchStartViewport.X) / _pinchStartViewport.Zoom;
        var flowY = (py - _pinchStartViewport.Y) / _pinchStartViewport.Zoom;

        _viewport = ClampViewport(new BlowViewport(px - flowX * newZoom, py - flowY * newZoom, newZoom));
        _ = OnMove.InvokeAsync(_viewport);
        RenderOverlay();
    }

    private double PointerDistance()
    {
        var points = _activePointers.Values.Take(2).ToArray();
        if (points.Length < 2) return 0;
        var dx = points[1].X - points[0].X;
        var dy = points[1].Y - points[0].Y;
        return Math.Sqrt(dx * dx + dy * dy);
    }

    private (double X, double Y) PointerMidpoint()
    {
        var points = _activePointers.Values.Take(2).ToArray();
        if (points.Length < 2) return (0, 0);
        return ((points[0].X + points[1].X) / 2, (points[0].Y + points[1].Y) / 2);
    }

    /// <summary>
    /// Captures the pointer on the pane so a gesture keeps receiving move/up events after the
    /// pointer leaves the flow's bounds. Per the Pointer Events spec, boundary events are
    /// suppressed for the capture target, which is why the leave handler doesn't abort the drag.
    /// </summary>
    private void CapturePane(long pointerId)
    {
        // Note: pointerId 0 is a legitimate id (Firefox uses it for the mouse), so it is
        // captured like any other rather than treated as "no pointer". Only a negative id - the
        // value the Pointer Events spec reserves for events no pointing device produced - means
        // the caller had no pointer to give. Capturing it anyway would make the pane wait for a
        // release from a pointer that does not exist, and ignore the real one's.
        if (pointerId < 0)
        {
            _capturedPointerId = null;
            return;
        }
        _capturedPointerId = pointerId;
        _ = _interop.SetPointerCaptureAsync(_paneRef, pointerId);
    }

    private void ReleasePane()
    {
        if (_capturedPointerId is not { } id) return;
        _capturedPointerId = null;
        _ = _interop.ReleasePointerCaptureAsync(_paneRef, id);
    }

    /// <summary>True while any pointer-driven gesture is in progress.</summary>
    /// <remarks>
    /// A pointer merely resting on the pane counts: its position has to keep being tracked so a
    /// second finger can start a pinch from an up-to-date baseline.
    /// </remarks>
    /// <remarks>
    /// A connection made from the keyboard is not one: the pointer has nothing to do with it, so the pane does not
    /// listen to the pointer for it, and a mouse resting on the canvas can neither steer the line nor end it.
    /// </remarks>
    private bool IsGesturing =>
        _panning || _draggingNodes || (_connecting && !_keyboardConnecting) || _selecting || _resizing || _pinching
        || _activePointers.Count > 0;

    /// <summary>
    /// Pointer-move is only wired up while a gesture is running. Every DOM event on this
    /// element round-trips into .NET and schedules a render, so leaving a move listener
    /// attached on an idle canvas is pure overhead - badly so over a Blazor Server circuit.
    /// </summary>
    /// <remarks>An empty <see cref="EventCallback"/> makes the renderer omit the attribute entirely.</remarks>
    private EventCallback<PointerEventArgs> PointerMoveHandler => IsGesturing
        ? EventCallback.Factory.Create<PointerEventArgs>(this, OnPanePointerMove)
        : default;

    /// <summary>Likewise: only listen for mouse-move when the consumer actually asked for it.</summary>
    private EventCallback<MouseEventArgs> PaneMouseMoveHandler =>
        OnPaneMouseMove.HasDelegate ? OnPaneMouseMove : default;

    private async Task OnPanePointerMove(PointerEventArgs e)
    {
        // A mouse moving with no button held while the pane still holds its press: the release never arrived - let go
        // outside the window, or taken by the browser for a drag of its own - and the gesture would follow the pointer
        // until the next click. It ends here, as the release would have ended it.
        if (e.Buttons == 0 && e.PointerType == "mouse" && _capturedPointerId == e.PointerId)
        {
            await OnPanePointerUp(e);
            return;
        }

        // Pointer moves are the busiest event there is, and most of them change only what the canvas draws itself.
        // The render that follows the handler is scoped to that unless a branch below says otherwise.
        _pendingScope |= RenderScope.Overlay;

        if (_activePointers.ContainsKey(e.PointerId)) TrackPointer(e);

        if (_pinching)
        {
            ApplyPinch();
            return;
        }

        if (_panning)
        {
            var dx = e.ClientX - _panStartX;
            var dy = e.ClientY - _panStartY;
            if (Math.Sqrt(dx * dx + dy * dy) > PaneClickDistance) _suppressNextClick = true;

            _viewport = ClampViewport(_panStartViewport with
            {
                X = _panStartViewport.X + dx,
                Y = _panStartViewport.Y + dy,
            });
            _ = OnMove.InvokeAsync(_viewport);
            RenderOverlay();
        }
        else if (_draggingNodes)
        {
            ApplyNodeDrag(e.ClientX, e.ClientY);
            UpdateAutoPan(e.ClientX, e.ClientY);
            RenderGeometry();
        }
        else if (_connecting && !_keyboardConnecting)
        {
            // Below the threshold the gesture is still a click on the handle, so the line is
            // not drawn yet and a click-to-connect can still complete on release.
            if (!_connMoved)
            {
                var dx = e.ClientX - _connStartClientX;
                var dy = e.ClientY - _connStartClientY;
                if (Math.Sqrt(dx * dx + dy * dy) < ConnectionDragThreshold) return;
                _connMoved = true;
            }

            UpdateConnectionEnd(e.ClientX, e.ClientY);
            UpdateAutoPan(e.ClientX, e.ClientY);
            RenderOverlay();
        }
        else if (_selecting)
        {
            _selEnd = ScreenToFlow(e);
            ApplyBoxSelection();
            StateHasChanged();
        }
        else if (_resizing && _resizeNode is not null)
        {
            ApplyResize(e);
            RenderGeometry();
        }
    }

    private async Task OnPanePointerUp(PointerEventArgs e)
    {
        _activePointers.Remove(e.PointerId);

        if (_pinching)
        {
            // A pinch ends as soon as one finger lifts; the remaining one must not silently
            // turn into a pan, so the whole gesture is finished here.
            if (_activePointers.Count < 2)
            {
                _pinching = false;
                ReleasePane();
                await OnViewportChanged.InvokeAsync(_viewport);
                await OnMoveEnd.InvokeAsync(_viewport);
                StateHasChanged();
            }
            return;
        }

        // While a pointer is captured, only that pointer's release ends the gesture. This has to
        // come before anything is torn down: a stray second pointer lifting mid-drag must not
        // stop the auto-pan the first one is still driving.
        if (_capturedPointerId is { } captured && e.PointerId != captured) return;

        StopAutoPan();

        if (_panning)
        {
            _panning = false;
            await OnViewportChanged.InvokeAsync(_viewport);
            await OnMoveEnd.InvokeAsync(_viewport);
        }

        if (_draggingNodes)
        {
            // Letting go changes where the nodes are and that they are no longer being dragged, both of which each node
            // compares for itself - not the selection or the lists. Left untagged, the render after this handler would
            // count as one that changed anything, and every node of the graph would render again for it: 400 ms at
            // 900 nodes in a WebAssembly app. Anything below that does change more (an app's handler) still says so.
            _pendingScope |= RenderScope.Geometry;

            _draggingNodes = false;
            var dragged = _dragPrimary;
            var moved = _dragArmed;
            var wasSelectionDrag = IsSelectionDrag;
            var movedNodes = DraggedNodes();
            var draggedSet = wasSelectionDrag && OnSelectionDragStop.HasDelegate ? movedNodes : null;
            _dragPrimary = null;
            _guides = [];
            foreach (var (node, _) in _dragSet) node.Dragging = false;
            _dragSet.Clear();
            _dragOriginals.Clear();
            _dragSubtree = null;
            _needsMeasure = true; // node moved: re-measure handle anchors
            if (dragged is not null)
                await OnNodeDragStop.InvokeAsync(dragged);
            if (draggedSet is not null)
                await OnSelectionDragStop.InvokeAsync(new BlowSelectionMouseEvent(draggedSet, e));
            if (moved)
            {
                Commit(BlowChangeKind.Moved, "move", movedNodes, origin: BlowChangeOrigin.User);
                await RaiseNodesChangedAsync();
            }

            // Again for the render that follows an app's handler that awaited something: the first went when it did.
            _pendingScope |= RenderScope.Geometry;
        }

        // A connection made from the keyboard is no business of a pointer being released or leaving the pane.
        if (_connecting && !_keyboardConnecting)
        {
            // Click-to-connect: a press-and-release without travel leaves the connection pending
            // so the next handle click can complete it.
            if (ConnectOnClick && !_connMoved)
            {
                ReleasePane();
                return;
            }

            // Released over the pane => hit-test the release point for a handle, then fall back
            // to snapping to the nearest handle within ConnectionRadius, then give up.
            // A press-and-release that never moved only ever completes on an exact hit: it would
            // be surprising for a plain click on a handle to wire it to a neighbour.
            _connDropping = _connMoved;
            var drop = await TryCompleteByHitTestAsync(e.ClientX, e.ClientY);
            var completed = drop == HitTestDrop.Completed
                            // Released right on a handle that refused: that verdict stands. Snapping to a valid
                            // handle nearby instead would connect somewhere the user did not let go.
                            || (drop == HitTestDrop.NoHandle && _connMoved && TryCompleteByProximity());
            if (!completed) CancelConnection();
        }

        if (_selecting)
        {
            _selecting = false;
            _preSelected.Clear();
            await NotifySelectionChangedAsync();
            await OnSelectionEnd.InvokeAsync(e);
            StateHasChanged();
        }

        if (_resizing)
        {
            _resizing = false;
            if (_resizeNode is { } rn)
            {
                rn.Resizing = false;
                await OnNodeResizeEnd.InvokeAsync(rn);
            }
            var resized = _resizeNode;
            _resizeNode = null;
            _needsMeasure = true; // size changed: re-measure handle anchors
            Commit(BlowChangeKind.Resized, "resize", resized is null ? null : [resized], origin: BlowChangeOrigin.User);
            await RaiseNodesChangedAsync();
            StateHasChanged();
        }

        ReleasePane();
    }

    /// <summary>
    /// Ends every in-progress gesture without committing it. Bound to <c>pointercancel</c>,
    /// which fires when the browser takes the pointer over (OS gestures, touch scrolling).
    /// </summary>
    private void OnPanePointerCancel(PointerEventArgs e)
    {
        _activePointers.Remove(e.PointerId);

        // The browser took the pointer over: the click a press on a node was waiting for is not coming.
        _pressedNode = null;
        CancelActiveGesture(e);
        StateHasChanged();
    }

    /// <summary>
    /// Abandons whatever gesture is running without committing it, putting back anything it had
    /// already moved. Shared by <c>pointercancel</c> (the browser taking the pointer over) and by
    /// Escape, which is how a user says the same thing.
    /// </summary>
    /// <remarks>
    /// The gesture's end event is still raised - with everything already put back - so a listener
    /// that paired it with the start event is not left waiting for an end that never comes.
    /// </remarks>
    /// <param name="e">The event that ended the gesture, when it was a pointer event.</param>
    /// <returns>True when there was something to abandon.</returns>
    private bool CancelActiveGesture(MouseEventArgs? e = null)
    {
        var wasActive = _pinching || _panning || _selecting || _connecting || _draggingNodes || _resizing;

        // A pan or a pinch is not undone: the viewport stays where it was taken, so it has settled there.
        var viewportMoved = _pinching || _panning;

        _pinching = false;
        StopAutoPan();

        // The browser taking a pointer over (e set) is no business of a connection made from the keyboard.
        if (_connecting && !(e is not null && _keyboardConnecting)) CancelConnection();

        _panning = false;

        if (_selecting)
        {
            // Put the selection back to what the box found it as: the box has been repainting it
            // on every pointer move, and abandoning the gesture has to abandon that too.
            _selecting = false;
            foreach (var n in Nodes) n.Selected = _preSelected.Contains(NodeKey(n.Id));
            foreach (var ed in Edges) ed.Selected = _preSelected.Contains(EdgeKey(ed.Id));
            _ = NotifySelectionChangedAsync();
            _ = OnSelectionEnd.InvokeAsync(e as PointerEventArgs ?? new PointerEventArgs());
        }
        _preSelected.Clear();

        if (_draggingNodes)
        {
            var dragged = _dragPrimary;
            var draggedSet = IsSelectionDrag && OnSelectionDragStop.HasDelegate ? DraggedNodes() : null;

            // Restore the pre-drag state: a cancelled drag must not leave nodes half-moved, nor a
            // parent grown (and its children renumbered) for a child that has now gone back inside.
            foreach (var (node, original) in _dragOriginals)
            {
                node.Position = original.Position;
                node.Width = original.Width;
                node.Height = original.Height;
            }
            foreach (var (node, _) in _dragSet) node.Dragging = false;
            _dragOriginals.Clear();
            _dragSet.Clear();
            _dragSubtree = null;
            _draggingNodes = false;
            _dragPrimary = null;
            _guides = [];
            _needsMeasure = true;

            if (dragged is not null) _ = OnNodeDragStop.InvokeAsync(dragged);
            if (draggedSet is not null)
                _ = OnSelectionDragStop.InvokeAsync(new BlowSelectionMouseEvent(draggedSet, e ?? new MouseEventArgs()));
        }

        if (_resizing)
        {
            if (_resizeNode is { } rn)
            {
                rn.Resizing = false;
                rn.Position = _resizeStartPos;
                rn.Width = _resizeStartSize.Width;
                rn.Height = _resizeStartSize.Height;
                _ = OnNodeResizeEnd.InvokeAsync(rn);
            }
            _resizing = false;
            _resizeNode = null;
        }

        if (viewportMoved)
        {
            _ = OnViewportChanged.InvokeAsync(_viewport);
            _ = OnMoveEnd.InvokeAsync(_viewport);
        }

        ReleasePane();
        return wasActive;
    }

    /// <summary>
    /// Resolves a connection drop by asking the DOM which handle sits under the release point.
    /// This makes drops work even when <see cref="ConnectionRadius"/> is 0 and the pointer was
    /// captured by the pane (so the handle never saw the pointer-up itself).
    /// </summary>
    private async Task<HitTestDrop> TryCompleteByHitTestAsync(double clientX, double clientY)
    {
        BlowHandleHit? hit;
        try
        {
            hit = await _interop.ElementDataAtPointAsync(_paneRef, clientX, clientY, ".bit-blow-handle");
        }
        catch (Exception ex) when (IsInteropTeardown(ex))
        {
            return HitTestDrop.NoHandle;
        }

        // What the pointer was let go on - or near, for the proximity fallback that follows - is judged against the
        // graph as it is now: a node may have been added, or a full port freed, since the drag began.
        if (_connecting) RefreshConnectionSession();

        if (hit?.NodeId is not { Length: > 0 } nodeId) return HitTestDrop.NoHandle;

        var type = hit.HandleType == "target" ? BlowHandleType.Target : BlowHandleType.Source;
        var handleId = string.IsNullOrEmpty(hit.HandleId) ? null : hit.HandleId;

        // A handle that refuses drops is painted as rejected; releasing on it must not connect.
        var check = hit.ConnectableEnd == false
            ? new BlowConnectionCheck(BlowConnectionRejection.NotConnectable)
            : CheckConnection(nodeId, handleId, type);
        if (!check.IsValid)
        {
            _connRejection = check.Rejection;
            return HitTestDrop.Refused;
        }

        CompleteConnection(nodeId, handleId, type);
        return HitTestDrop.Completed;
    }

    /// <summary>What was under the pointer when a connection was let go.</summary>
    private enum HitTestDrop { NoHandle, Refused, Completed }

    private async Task OnWheel(WheelEventArgs e)
    {
        // A wheel pans or zooms: the render after it is the viewport's, like a pointer move's.
        _pendingScope |= RenderScope.Overlay;

        await OnPaneScroll.InvokeAsync(e);
        if (_paneRect is null) await RefreshPaneRectAsync();

        // Pinch (Ctrl/Cmd+wheel) always zooms; otherwise PanOnScroll decides pan vs zoom.
        var zoomGesture = e.CtrlKey || !PanOnScroll;

        if (zoomGesture)
        {
            if (!ZoomOnScroll && !e.CtrlKey) return;

            // A required activation key makes plain scrolling fall through to the page, which is
            // what a flow embedded in a long document usually wants. Ctrl+wheel is the browser's
            // own pinch gesture and stays exempt.
            if (ZoomActivationKey is { } zoomKey && !e.CtrlKey && !IsModifierHeld(zoomKey, e)) return;

            BeginWheelGesture();
            var px = e.ClientX - (_paneRect?.Left ?? 0);
            var py = e.ClientY - (_paneRect?.Top ?? 0);
            ZoomAtScreenPoint(px, py, WheelZoomFactor(e));
            return;
        }

        BeginWheelGesture();

        // Pan: vertical scroll moves Y; Shift swaps to horizontal. Some platforms (macOS) already
        // turn Shift+wheel into a horizontal delta themselves, so the swap only applies when the
        // event did not arrive horizontal already - swapping it again would throw it away.
        var scale = WheelDeltaScale(e);
        var deltaX = e.DeltaX * scale;
        var deltaY = e.DeltaY * scale;
        var (dx, dy) = e.ShiftKey && deltaX == 0 ? (deltaY, 0.0) : (deltaX, deltaY);

        // A single-axis mode keeps whichever delta the device produced but routes it onto that
        // axis: a mouse wheel only ever reports Y, a trackpad swipe along the axis reports it there.
        if (PanOnScrollMode == BlowPanOnScrollMode.Horizontal) { dx = dx != 0 ? dx : dy; dy = 0; }
        else if (PanOnScrollMode == BlowPanOnScrollMode.Vertical) { dy = dy != 0 ? dy : dx; dx = 0; }
        _viewport = ClampViewport(_viewport with
        {
            X = _viewport.X - dx * PanOnScrollSpeed,
            Y = _viewport.Y - dy * PanOnScrollSpeed,
        });
        await OnViewportChanged.InvokeAsync(_viewport);
        await OnMove.InvokeAsync(_viewport);
        RenderOverlay();
    }

    /// <summary>
    /// Marks the start of a wheel-driven pan/zoom, and (re)arms the timer that ends it. The
    /// browser reports no start or end for a scroll gesture - a trackpad flick is just a burst of
    /// wheel events - so the end is inferred from the first quiet moment. Without this,
    /// <see cref="OnMoveStart"/> and <see cref="OnMoveEnd"/> would fire for dragging and pinching
    /// but silently not for the most common way of moving the viewport at all.
    /// </summary>
    private void BeginWheelGesture()
    {
        StopViewportAnimation();

        if (!_wheelActive)
        {
            _wheelActive = true;
            _ = OnMoveStart.InvokeAsync(_viewport);
        }

        _wheelIdleCts?.Cancel();
        _wheelIdleCts?.Dispose();
        _wheelIdleCts = new CancellationTokenSource();
        _ = EndWheelGestureWhenIdleAsync(_wheelIdleCts.Token);
    }

    private async Task EndWheelGestureWhenIdleAsync(CancellationToken token)
    {
        try
        {
            await Task.Delay(WheelIdleDelayMs, token);
            await InvokeAsync(async () =>
            {
                if (!_wheelActive) return;
                _wheelActive = false;
                await OnMoveEnd.InvokeAsync(_viewport);
            });
        }
        catch (OperationCanceledException) { /* another wheel event arrived, or we are going away */ }
        // The delay outlives a render, so the component can be torn down before it elapses.
        catch (ObjectDisposedException) { }
    }

    private void StopWheelGesture()
    {
        _wheelActive = false;
        _wheelIdleCts?.Cancel();
        _wheelIdleCts?.Dispose();
        _wheelIdleCts = null;
    }

    /// <summary>
    /// How much one wheel event zooms by, proportional to how far it scrolled. A mouse wheel
    /// sends one large delta per notch, while a trackpad (and a trackpad pinch, which browsers
    /// report as Ctrl+wheel) sends a stream of small ones - a fixed step per event would make
    /// those zoom many times faster than a mouse. The curve is d3-zoom's, so a notch of a mouse
    /// wheel still zooms by about 15%, and no single event can zoom by more than that.
    /// </summary>
    private static double WheelZoomFactor(WheelEventArgs e)
    {
        var perUnit = e.DeltaMode switch
        {
            1 => 0.05, // lines
            2 => 1,    // pages
            _ => 0.002 // pixels
        };
        var delta = -e.DeltaY * perUnit * (e.CtrlKey ? 10 : 1);
        return Math.Pow(2, Math.Clamp(delta, -MaxWheelZoomStep, MaxWheelZoomStep));
    }

    /// <summary>Largest zoom exponent a single wheel event may apply (2^0.2, about 15%).</summary>
    private const double MaxWheelZoomStep = 0.2;

    /// <summary>
    /// Converts a wheel delta into pixels. Firefox reports mouse wheels in lines, and a few
    /// devices in pages; taken as pixels, those would pan the canvas a pixel or two per notch.
    /// </summary>
    private double WheelDeltaScale(WheelEventArgs e) => e.DeltaMode switch
    {
        1 => 20,
        2 => _paneRect?.Height ?? 800,
        _ => 1,
    };

    private void ZoomAtScreenPoint(double px, double py, double factor)
    {
        var newZoom = Math.Clamp(_viewport.Zoom * factor, MinZoom, MaxZoom);
        if (Math.Abs(newZoom - _viewport.Zoom) < 1e-6) return;

        // Keep the flow point under the cursor stationary.
        var flowX = (px - _viewport.X) / _viewport.Zoom;
        var flowY = (py - _viewport.Y) / _viewport.Zoom;
        ApplyViewport(ClampViewport(new BlowViewport(px - flowX * newZoom, py - flowY * newZoom, newZoom)));
    }

    private async Task OnPaneDoubleClick(MouseEventArgs e)
    {
        // The two presses of a double click on a node captured the pointer on the pane, so the browser reports the
        // double click here rather than on the node: it is the node's, and does not zoom.
        var clickedNode = _clickedNode;
        _clickedNode = null;
        if (clickedNode is not null)
        {
            if (OnNodeDoubleClick.HasDelegate) await OnNodeDoubleClick.InvokeAsync(new BlowNodeMouseEvent(clickedNode, e));
            return;
        }

        if (!ZoomOnDoubleClick) return;
        if (_paneRect is null) await RefreshPaneRectAsync();
        var px = e.ClientX - (_paneRect?.Left ?? 0);
        var py = e.ClientY - (_paneRect?.Top ?? 0);
        ZoomAtScreenPoint(px, py, e.ShiftKey ? 1 / 1.2 : 1.2);
    }

    private void OnKeyUp(KeyboardEventArgs e)
    {
        if (PanActivationKey is { } key && e.Key == key)
        {
            _panKeyHeld = false;
            StateHasChanged();
        }
    }

    /// <summary>
    /// The key-up for the pan key never arrives once focus has left the flow (the user tabbed or
    /// switched windows while holding it), which would leave every later drag panning. Losing focus
    /// therefore lets go of it; a key still physically held re-arms on its next auto-repeat.
    /// Only listened for while the key is held, so focus moving around an idle flow costs nothing.
    /// </summary>
    private EventCallback<FocusEventArgs> PanKeyReleaseHandler => _panKeyHeld
        ? EventCallback.Factory.Create<FocusEventArgs>(this, () => _panKeyHeld = false)
        : default;

    /// <summary>A key a part inside the pane (the minimap) has acted on, which the pane must not act on as well.</summary>
    private string? _keyHandled;

    /// <summary>
    /// Tells the canvas that the key being pressed was acted on by a part inside it - the minimap's arrows - so the
    /// pane, which hears the same key press next, leaves it alone: the arrows would move the selection too.
    /// </summary>
    public void MarkKeyHandled(string key) => _keyHandled = key;

    private async Task OnKeyDown(KeyboardEventArgs e)
    {
        if (_keyHandled is { } handled)
        {
            _keyHandled = null;
            if (handled == e.Key) return;
        }

        if (PanActivationKey is { } panKey && e.Key == panKey && !_panKeyHeld)
        {
            _panKeyHeld = true;
            StateHasChanged();
        }

        if (e.Key == "Escape")
        {
            // Escape means "undo what I am in the middle of" before it means "deselect": with a
            // selection box half-drawn or a node half-dragged, clearing the selection instead
            // would leave the gesture running and finish it a moment later anyway.
            var cancelled = KeyboardCancelledText;
            if (CancelActiveGesture())
            {
                if (cancelled is not null) Announce(cancelled);
                StateHasChanged();
                return;
            }

            if (ElementsSelectable && await ShouldHandleKeyAsync())
            {
                var hadSelection = Nodes.Any(n => n.Selected) || Edges.Any(ed => ed.Selected);
                ClearSelection();
                await NotifySelectionChangedAsync();
                // Selecting is announced, so is letting go of it: a screen reader would otherwise still be left with
                // the last thing said, "... selected".
                if (hadSelection && !string.IsNullOrEmpty(AriaLabels.SelectionCleared)) Announce(AriaLabels.SelectionCleared);
                StateHasChanged();
            }
            return;
        }

        var command = e.CtrlKey || e.MetaKey;

        var isDelete = DeleteKeyEnabled && DeleteKeys.Contains(e.Key);
        var isArrow = !DisableKeyboardA11y && e.Key is "ArrowLeft" or "ArrowRight" or "ArrowUp" or "ArrowDown";
        var isSelectAll = SelectAllKeyEnabled && command && e.Key is "a" or "A";

        var clipboard = ClipboardShortcutsEnabled && command
            ? e.Key switch
            {
                "c" or "C" => ClipboardCommand.Copy,
                "x" or "X" => ClipboardCommand.Cut,
                "v" or "V" => ClipboardCommand.Paste,
                "d" or "D" => ClipboardCommand.Duplicate,
                _ => ClipboardCommand.None,
            }
            : ClipboardCommand.None;

        // Ctrl/Cmd+Z steps back; both of the two conventions for stepping forward are accepted,
        // since which one a user reaches for depends entirely on what else they use.
        var history = HistoryEnabled && command
            ? e.Key switch
            {
                "z" or "Z" => e.ShiftKey ? HistoryCommand.Redo : HistoryCommand.Undo,
                "y" or "Y" => HistoryCommand.Redo,
                _ => HistoryCommand.None,
            }
            : HistoryCommand.None;

        if (!isDelete && !isArrow && !isSelectAll
            && clipboard == ClipboardCommand.None && history == HistoryCommand.None)
        {
            return;
        }

        // Never hijack keys while the user is typing inside the flow (a custom node with an
        // input, a label being edited, ...) - Backspace there must edit text, not delete nodes,
        // and Ctrl+C must copy the text rather than the node around it.
        if (!await ShouldHandleKeyAsync()) return;

        try
        {
            switch (history)
            {
                case HistoryCommand.Undo: await UndoAsync(); return;
                case HistoryCommand.Redo: await RedoAsync(); return;
            }

            // The edits made from here are the user's. The origin is handed down rather than set for the duration: the
            // app's handlers awaited along the way (OnPaste, NodesChanged, ...) are not the user, and nor is anything
            // else that happens to run meanwhile.
            switch (clipboard)
            {
                case ClipboardCommand.Copy: Copy(); return;
                case ClipboardCommand.Cut: await CutAsync(BlowChangeOrigin.User); return;
                case ClipboardCommand.Paste: await PasteAsync(_clipboard, null, "paste", BlowChangeOrigin.User); return;
                case ClipboardCommand.Duplicate: await DuplicateSelectionAsync(BlowChangeOrigin.User); return;
            }

            if (isSelectAll) await SelectAllAsync();
            else if (isDelete) await DeleteSelectionAsync("delete", BlowChangeOrigin.User);
            else await MoveSelectedNodesAsync(e);
        }
        finally
        {
            // Each of these can remove the very node or edge that has focus, and the browser then hands focus to the
            // page: the keys that follow would no longer reach the flow. The render after the edit - not one of those
            // made along the way, while the element is still there - hands it back to the pane.
            if (isDelete || history != HistoryCommand.None || clipboard == ClipboardCommand.Cut)
            {
                _keepFocusInPane = true;
                StateHasChanged();
            }
        }
    }

    private enum ClipboardCommand { None, Copy, Cut, Paste, Duplicate }

    /// <summary>
    /// Set by a key that may delete the focused element; the render after it gives focus back to the pane if it fell to
    /// the page.
    /// </summary>
    private bool _keepFocusInPane;

    private enum HistoryCommand { None, Undo, Redo }

    /// <summary>False when focus sits in a text field, so graph-level keys should be ignored.</summary>
    private async Task<bool> ShouldHandleKeyAsync()
    {
        try { return !await _interop.IsEditableFocusedAsync(); }
        catch (Exception ex) when (IsInteropTeardown(ex)) { return false; }
    }

    private async Task MoveSelectedNodesAsync(KeyboardEventArgs e)
    {
        // With the grid on, one press moves one cell - and each axis uses its own cell size,
        // since a grid is not necessarily square.
        var stepX = e.ShiftKey ? CoarseStep : (SnapToGrid ? SnapStepX : FineStep);
        var stepY = e.ShiftKey ? CoarseStep : (SnapToGrid ? SnapStepY : FineStep);

        var (dx, dy) = e.Key switch
        {
            "ArrowLeft" => (-stepX, 0.0),
            "ArrowRight" => (stepX, 0.0),
            "ArrowUp" => (0.0, -stepY),
            "ArrowDown" => (0.0, stepY),
            _ => (0.0, 0.0),
        };

        await MoveSelectionAsync(dx, dy, BlowChangeOrigin.User);
    }

    /// <summary>
    /// Moves the selected nodes by the given distance, in flow units - what the arrow keys do, for a single-pointer
    /// alternative to dragging (WCAG 2.5.7): move buttons on a node toolbar, say. Draggable, visible nodes only; a
    /// node moves with its parent rather than twice; positions snap to the grid and stay inside extents, and a parent
    /// with <c>ExpandParent</c> children grows, all as for a drag. One undo step (<c>move</c>), announced.
    /// </summary>
    public Task MoveSelectionAsync(double dx, double dy) => MoveSelectionAsync(dx, dy, BlowChangeOrigin.Api);

    private async Task MoveSelectionAsync(double dx, double dy, BlowChangeOrigin origin)
    {
        // The same test a pointer drag applies, hidden included: a node nobody can see is not one
        // an arrow key should be quietly shifting around.
        var moving = NodesDraggable
            ? Nodes.Where(n => n.Selected && n.Draggable && !n.Hidden).ToList()
            : [];
        var movingIds = moving.Select(n => n.Id).ToHashSet();

        var moved = 0;
        BlowNode? last = null;
        var movedNodes = new List<BlowNode>();
        foreach (var n in moving)
        {
            // As in a drag, a node whose ancestor is moving already travels with it; stepping it
            // as well would move it twice, and out of its group.
            if (HasAncestorIn(n, movingIds)) continue;

            // Snapping after the step is what keeps a node that started off-grid from staying
            // off it forever: stepping by the cell size alone preserves the original offset.
            var next = Snap(new BlowXYPosition(n.Position.X + dx, n.Position.Y + dy));
            n.Position = ClampNodePosition(n, next);

            // A node nudged by the keyboard ends up in the same place as one dragged there, so
            // its container has to grow for it in the same way.
            ExpandParentFor(n);

            moved++;
            last = n;
            movedNodes.Add(n);
        }

        if (moved > 0)
        {
            _needsMeasure = true;
            Commit(BlowChangeKind.Moved, "move", movedNodes, origin: origin);
            Announce(moved == 1 && last is not null
                ? AriaLabels.NodeMoved(NameOf(last), last.Position)
                : AriaLabels.NodesMoved(moved));
            await RaiseNodesChangedAsync();
            StateHasChanged();
        }
    }

    /// <summary>
    /// Consumes the click that the browser fires at the end of every gesture. Returns true when
    /// the gesture was a drag, in which case the click is not a click and should be dropped.
    /// </summary>
    private bool ConsumeSuppressedClick()
    {
        if (!_suppressNextClick) return false;
        _suppressNextClick = false;
        return true;
    }

    private async Task OnPaneClickHandler(MouseEventArgs e)
    {
        var clearSelection = _clearSelectionOnClick;
        _clearSelectionOnClick = false;
        var pressedNode = _pressedNode;
        _pressedNode = null;

        // A press that panned the viewport or dragged a node was not a click: it neither deselects nor reports one,
        // and the render after this handler has nothing to show - left untagged it would count as one that changed
        // anything, and every node would render again for a click that did nothing.
        if (ConsumeSuppressedClick())
        {
            _pendingScope |= RenderScope.Overlay;
            return;
        }

        // A press on a node captures the pointer on the pane, so the browser reports the click here rather than on
        // the node: it is the node's click all the same.
        if (pressedNode is not null)
        {
            _clickedNode = pressedNode;
            await OnNodeClick.InvokeAsync(pressedNode);
            return;
        }

        if (clearSelection)
        {
            ClearSelection();
            await NotifySelectionChangedAsync();
        }

        await OnPaneClick.InvokeAsync(e);
    }

    private Task OnPaneContextMenuHandler(MouseEventArgs e) => OnPaneContextMenu.InvokeAsync(e);

    private Task OnNodeClickHandler(BlowNode node)
    {
        _pressedNode = null;
        return ConsumeSuppressedClick() ? Task.CompletedTask : OnNodeClick.InvokeAsync(node);
    }

    /// <summary>
    /// The node the last press on a node landed on, until its click arrives. The press captures the pointer on the
    /// pane, so a browser may report that click at the pane instead of at the node.
    /// </summary>
    private BlowNode? _pressedNode;

    /// <summary>
    /// The node whose click the pane heard last, until the next press: the double click that may follow it is reported
    /// at the pane as well, for the same reason.
    /// </summary>
    private BlowNode? _clickedNode;

    /// <summary>
    /// What a press of the pointer inside the canvas does before the gesture it starts, wherever it lands that keeps
    /// the press from the pane - a node, an edge, a handle, a resize control, the minimap: a connection being made
    /// from the keyboard ends, and says so, and the node an earlier press landed on is forgotten, since a click the
    /// pane hears from now on is not that node's. A press on the pane itself does the same in its own handler.
    /// </summary>
    /// <param name="node">The node this press landed on, whose click the pane may hear instead of the node.</param>
    private void OnPointerPress(BlowNode? node = null)
    {
        _pressedNode = node;
        _clickedNode = null;
        AbandonKeyboardConnection();
    }

    void IBlowContext.PointerPressed() => OnPointerPress();

    private Task OnEdgeClickHandler(BlowEdge edge)
        => ConsumeSuppressedClick() ? Task.CompletedTask : OnEdgeClick.InvokeAsync(edge);

    /// <summary>
    /// The node's context-menu callback, or an empty one when the app listens for neither node
    /// nor selection context menus - in which case the browser keeps its own menu.
    /// </summary>
    private EventCallback<MouseEventArgs> NodeContextMenu(BlowNode node)
        => OnNodeContextMenu.HasDelegate || OnSelectionContextMenu.HasDelegate
            ? EventCallback.Factory.Create<MouseEventArgs>(this, e => OnNodeContextMenuHandler(node, e))
            : default;

    /// <summary>
    /// Binds one of the node mouse events to a node, or hands back an empty callback when nobody
    /// is listening. An empty callback makes the renderer leave the DOM listener off altogether:
    /// otherwise every mouse move across every node would round-trip into .NET and re-render the
    /// whole canvas for nothing - badly so over a Blazor Server circuit. It also lets the browser
    /// keep its own context menu where the app has none to offer.
    /// </summary>
    private EventCallback<MouseEventArgs> NodeMouse(EventCallback<BlowNodeMouseEvent> handler, BlowNode node)
        => handler.HasDelegate
            ? EventCallback.Factory.Create<MouseEventArgs>(this, e => handler.InvokeAsync(new BlowNodeMouseEvent(node, e)))
            : default;

    /// <summary>The edge counterpart of <see cref="NodeMouse"/>.</summary>
    private EventCallback<MouseEventArgs> EdgeMouse(EventCallback<BlowEdgeMouseEvent> handler, BlowEdge edge)
        => handler.HasDelegate
            ? EventCallback.Factory.Create<MouseEventArgs>(this, e => handler.InvokeAsync(new BlowEdgeMouseEvent(edge, e)))
            : default;

    /// <summary>
    /// Right-clicking a node always reports that node. When the node is one of several selected,
    /// the whole selection is reported as well, so a menu can offer "delete these 6" rather than
    /// acting on whichever one happened to be under the cursor.
    /// </summary>
    private async Task OnNodeContextMenuHandler(BlowNode node, MouseEventArgs e)
    {
        await OnNodeContextMenu.InvokeAsync(new BlowNodeMouseEvent(node, e));

        if (!OnSelectionContextMenu.HasDelegate || !node.Selected) return;

        var selected = Nodes.Where(n => n.Selected).ToList();
        if (selected.Count > 1)
            await OnSelectionContextMenu.InvokeAsync(new BlowSelectionMouseEvent(selected, e));
    }

    // ---- node drag / selection ----

    private void StartNodeDrag(BlowNode node, PointerEventArgs e)
    {
        // A finger on a node is a finger of a pinch all the same: the pane, which tracks the pinch's pointers, never
        // hears this press, so without this two fingers with one on a node panned instead of zooming.
        if (e.PointerType == "touch")
        {
            TrackPointer(e);
            if (TryStartPinch()) return;
        }

        OnPointerPress(node);
        _suppressNextClick = false;
        SelectOnPointerDown(node, e);

        // When the node uses a custom drag handle, the body selects but doesn't drag.
        if (node.UseCustomDragHandle) return;

        BeginDrag(node, e);
    }

    /// <summary>Starts dragging a node, bypassing the custom-drag-handle restriction. Called by <c>BlowDragHandle</c>.</summary>
    public void BeginNodeDrag(BlowNode node, PointerEventArgs e)
    {
        OnPointerPress(node);
        SelectOnPointerDown(node, e);
        BeginDrag(node, e);
    }

    private void SelectOnPointerDown(BlowNode node, PointerEventArgs e)
    {
        if (!ElementsSelectable || !node.Selectable) return;

        // With SelectNodesOnDrag off, pressing a node that is already part of the selection
        // leaves that selection intact, so nudging one member does not collapse the rest.
        if (!SelectNodesOnDrag && !IsMultiSelect(e) && node.Selected) return;

        if (!node.Selected && !IsMultiSelect(e)) ClearSelection();
        node.Selected = true;
        _ = NotifySelectionChangedAsync();
    }

    private void BeginDrag(BlowNode node, PointerEventArgs e)
    {
        if (!NodesDraggable || !node.Draggable) return;

        _draggingNodes = true;
        _dragArmed = false;
        _dragPrimary = node;
        _dragStartClientX = e.ClientX;
        _dragStartClientY = e.ClientY;
        _dragStartFlow = ScreenToFlowPosition(e.ClientX, e.ClientY);
        _dragPrimaryStartRect = node.GetRect();
        _guides = [];
        _dragSet.Clear();
        _dragSubtree = null;

        // Multi-drag only carries the other selected nodes when the grabbed node is itself
        // part of the selection; otherwise only the grabbed node moves.
        var moving = node.Selected
            ? Nodes.Where(n => n.Selected && n.Draggable && !n.Hidden).ToList()
            : [];
        if (moving.Count == 0) moving.Add(node);

        // Exclude nodes whose ancestor is also moving: a parent drag already carries its children.
        var movingIds = moving.Select(n => n.Id).ToHashSet();
        foreach (var n in moving)
        {
            if (HasAncestorIn(n, movingIds)) continue;
            n.Dragging = true;
            _dragSet.Add((n, n.Position));
        }
        RememberDragOriginals();

        CapturePane(e.PointerId);
        _ = OnNodeDragStart.InvokeAsync(node);

        // A drag that carries more than one node is a selection being moved, not a node being
        // nudged, and callers usually want to treat it as a single edit.
        if (IsSelectionDrag && OnSelectionDragStart.HasDelegate)
            _ = OnSelectionDragStart.InvokeAsync(new BlowSelectionMouseEvent(DraggedNodes(), e));

        // Required: the pane's pointer-move handler is only attached while a gesture is
        // running, and a drag begun from a BlowDragHandle doesn't re-render the canvas by itself.
        StateHasChanged();
    }

    /// <summary>Records what the drag about to start may change, for <see cref="_dragOriginals"/>.</summary>
    private void RememberDragOriginals()
    {
        _dragOriginals.Clear();

        HashSet<string>? growing = null;
        foreach (var (node, start) in _dragSet)
        {
            _dragOriginals[node] = (start, node.Width, node.Height);
            if (node.ExpandParent && !node.ExtentParent && node.ParentId is { } pid)
                (growing ??= []).Add(pid);
        }

        if (growing is null) return;

        foreach (var n in Nodes)
        {
            if (growing.Contains(n.Id) || (n.ParentId is { } pid && growing.Contains(pid)))
                _dragOriginals.TryAdd(n, (n.Position, n.Width, n.Height));
        }
    }

    /// <summary>True while the running drag is moving several nodes at once.</summary>
    private bool IsSelectionDrag => _dragSet.Count > 1;

    /// <summary>The nodes the running drag is moving, as an independent list.</summary>
    private List<BlowNode> DraggedNodes()
    {
        var nodes = new List<BlowNode>(_dragSet.Count);
        foreach (var (node, _) in _dragSet) nodes.Add(node);
        return nodes;
    }

    private void SelectEdge(BlowEdge edge, PointerEventArgs e)
    {
        OnPointerPress();
        if (ElementsSelectable && edge.Selectable)
        {
            if (!IsMultiSelect(e)) ClearSelection();
            edge.Selected = true;
            _ = NotifySelectionChangedAsync();
        }
        StateHasChanged();
    }

    private void ClearSelection()
    {
        foreach (var n in Nodes)
        {
            if (n.Selected) _deselected = true;
            n.Selected = false;
        }
        foreach (var ed in Edges)
        {
            if (ed.Selected) _deselected = true;
            ed.Selected = false;
        }
    }

    /// <summary>
    /// Set when <see cref="ClearSelection"/> deselected something, so the next notification goes out even when the set
    /// it leaves matches the last one told: a selection the app made itself (<c>Selected = true</c>) was never told, and
    /// clearing it would otherwise compare equal to an empty last selection and go unreported.
    /// </summary>
    private bool _deselected;

    /// <summary>
    /// Keyboard activation of a focused node (Enter/Space). <paramref name="additive"/> is set
    /// when Shift is held, matching the Shift+click behaviour.
    /// </summary>
    private async Task ActivateNode(BlowNode node, bool additive)
    {
        // While connecting from the keyboard, Enter on a node is the end of the connection, not a selection.
        if (_keyboardConnecting)
        {
            await CompleteKeyboardConnectionAsync(node);
            return;
        }

        if (!ElementsSelectable || !node.Selectable) return;

        // The node's key handler also hears keys typed into an input inside it, and a space or a
        // new line in a text field is not a request to select the node around it.
        if (!await ShouldHandleKeyAsync()) return;

        if (!additive) ClearSelection();
        node.Selected = !additive || !node.Selected;
        await NotifySelectionChangedAsync();
        Announce(node.Selected
            ? AriaLabels.NodeSelected(NameOf(node))
            : AriaLabels.NodeDeselected(NameOf(node)));
        StateHasChanged();
    }

    /// <summary>The name a node is announced by: its explicit aria label, then its label, then its id - a blank one names nothing.</summary>
    private static string NameOf(BlowNode node)
        => !string.IsNullOrWhiteSpace(node.AriaLabel) ? node.AriaLabel
            : !string.IsNullOrWhiteSpace(node.Label) ? node.Label
            : node.Id;

    /// <summary>
    /// Only wire up node focus when something is actually going to be done with it - an empty
    /// callback makes the renderer omit the attribute for every node in the graph.
    /// </summary>
    private EventCallback<FocusEventArgs> NodeFocusHandler(BlowNode node) => AutoPanOnNodeFocus || KeyboardConnections
        ? EventCallback.Factory.Create<FocusEventArgs>(this, () => OnNodeFocused(node))
        : default;

    private async Task OnNodeFocused(BlowNode node)
    {
        // Moving focus is how a keyboard connection is aimed: the line follows it to the node, and the reader hears
        // whether Enter would connect there.
        if (_keyboardConnecting)
        {
            if (node.Id == _connSource.NodeId) ClearKeyboardTarget();
            else TargetKeyboardConnection(node, after: null);
        }

        // Only the keyboard's focus is followed. A press focuses the node too - one in a nodrag control, which starts
        // no gesture, or on a control WebKit will not focus and hands the focus to the node instead - and moving the
        // view under the pointer then would be the canvas jumping on a click.
        if (!AutoPanOnNodeFocus || _paneRect is null || IsGesturing) return;
        try
        {
            if (await _interop.IsPointerFocusAsync(_paneRef, node.Id)) return;
        }
        catch (Exception ex) when (IsInteropTeardown(ex))
        {
            return;
        }

        BringNodeIntoView(node);
    }

    /// <summary>
    /// Pans the viewport so a node that has just taken keyboard focus is on screen. Tabbing into
    /// something you cannot see is where tab navigation of a large graph stops being usable, so
    /// the viewport follows the focus - but only far enough to reveal the node, since re-centring
    /// on every Tab would throw the reader's sense of place away at each step.
    /// </summary>
    private void BringNodeIntoView(BlowNode node)
    {
        if (!AutoPanOnNodeFocus || _paneRect is null) return;

        // Ignore focus that lands on a node while it is being dragged or connected from: the
        // viewport belongs to that gesture. A connection made from the keyboard is not a gesture
        // in that sense (see IsGesturing): focus is how it is aimed, and the node it reaches has
        // to be on screen.
        if (IsGesturing) return;

        var rect = node.GetRect();
        var topLeft = _viewport.FlowToScreen(new BlowXYPosition(rect.Left, rect.Top));
        var bottomRight = _viewport.FlowToScreen(new BlowXYPosition(rect.Right, rect.Bottom));

        var margin = Math.Max(0, AutoPanMargin);
        var dx = Nudge(topLeft.X, bottomRight.X, _paneRect.Width, margin);
        var dy = Nudge(topLeft.Y, bottomRight.Y, _paneRect.Height, margin);
        if (dx == 0 && dy == 0) return;

        var target = ResolveViewport(_viewport with { X = _viewport.X + dx, Y = _viewport.Y + dy });
        _ = AnimateViewportAsync(target, AutoPanOnNodeFocusDuration);

        // How far the viewport has to move on one axis for [near, far] to sit inside [0, extent],
        // leaving `margin` either side. Zero when it already does.
        static double Nudge(double near, double far, double extent, double margin)
        {
            // A node too big to fit is aligned to the leading edge rather than jittering.
            if (far - near > extent - 2 * margin) return margin - near;
            if (near < margin) return margin - near;
            if (far > extent - margin) return extent - margin - far;
            return 0;
        }
    }

    /// <summary>Keyboard activation of a focused edge (Enter/Space).</summary>
    private async Task ActivateEdge(BlowEdge edge, bool additive)
    {
        if (!ElementsSelectable || !edge.Selectable) return;

        // Likewise for an input an edge template renders (inside a foreignObject, say).
        if (!await ShouldHandleKeyAsync()) return;

        if (!additive) ClearSelection();
        edge.Selected = !additive || !edge.Selected;
        await NotifySelectionChangedAsync();
        Announce(edge.Selected
            ? AriaLabels.EdgeSelected(NameOf(edge.Source), NameOf(edge.Target))
            : AriaLabels.EdgeDeselected(NameOf(edge.Source), NameOf(edge.Target)));
        StateHasChanged();
    }

    /// <summary>Publishes a message to the flow's aria-live region for screen readers.</summary>
    /// <remarks>
    /// A live region is only re-read when its text changes, so announcing the same thing twice
    /// in a row (nudging a node twice with the same arrow key, say) would be silent the second
    /// time. Alternating an invisible zero-width space makes the text differ without changing
    /// a single spoken word.
    /// </remarks>
    public void Announce(string message)
    {
        // Same words as last time: flip the marker so the region's text differs even though
        // what gets spoken does not.
        var text = _liveMessage.TrimEnd(RepeatMarker) == message
            ? (_liveMessage.EndsWith(RepeatMarker) ? message : message + RepeatMarker)
            : message;

        if (_liveMessage == text) return;

        // Only the canvas's own live region changed: no node or edge has anything new to draw for it. Inside an edit
        // the render the edit makes at its end shows it.
        _liveMessage = text;
        if (!InEdit) RenderOverlay();
    }

    /// <summary>Zero-width space: invisible on screen and silent to a screen reader.</summary>
    private const char RepeatMarker = '\u200B';

    // ---- built-in undo/redo ----

    private BlowHistory? _history;

    /// <summary>
    /// True while a snapshot is being applied. Restoring rewrites the graph and raises the same
    /// change callbacks an edit does, and recording that as a step would push the state the user
    /// just came back from onto the undo stack and throw the redo branch away.
    /// </summary>
    private bool _restoring;

    /// <summary>
    /// The canvas's own history, or null unless <see cref="HistoryEnabled"/> is set. Exposed so
    /// a toolbar can read its depth, and so an app can capture a step of its own at a moment the
    /// canvas has no way to know about.
    /// </summary>
    public BlowHistory? History => _history;

    /// <summary>True when the built-in history has an earlier state to go back to.</summary>
    public bool CanUndo => _history?.CanUndo ?? false;

    /// <summary>True when the built-in history has a state to go forward to.</summary>
    public bool CanRedo => _history?.CanRedo ?? false;

    /// <summary>
    /// Records the current graph as an undo step. Does nothing when the built-in history is off,
    /// or when the graph is identical to the step already recorded - so it is safe to call from
    /// anywhere an edit might have happened without checking first.
    /// </summary>
    public void CaptureHistory() => CaptureHistory(null);

    /// <summary>
    /// Records the current graph as an undo step labelled with what changed - the label <see cref="BlowHistory.UndoLabel"/>
    /// reports for it. Only the nodes and edges that changed since the last step are copied.
    /// </summary>
    /// <remarks>Inside <see cref="EditAsync(Action, string?, BlowChangeOrigin, bool)"/> it does nothing: the edit is recorded as a whole when it ends.</remarks>
    public void CaptureHistory(string? label)
    {
        if (_history is null || _restoring || InEdit) return;
        if (_history.Capture(Nodes, Edges, _viewport, label)) NotifyHistoryChanged();
    }

    /// <summary>What an edit does to the built-in history.</summary>
    private enum HistoryAction
    {
        /// <summary>Records the edit as an undo step.</summary>
        Record,

        /// <summary>Leaves the history alone (a restore: the app records one if it means to).</summary>
        Skip,

        /// <summary>Writes the edit into every recorded step, so no undo takes it back (an edit from elsewhere).</summary>
        Rebase,
    }

    /// <summary>
    /// Closes an edit: records it as an undo step and tells <see cref="OnGraphChanged"/> about it. Every edit the canvas
    /// makes ends here, once.
    /// </summary>
    private void Commit(BlowChangeKind kind, string label, IReadOnlyList<BlowNode>? nodes = null, IReadOnlyList<BlowEdge>? edges = null,
        IReadOnlyList<BlowNode>? removedNodes = null, IReadOnlyList<BlowEdge>? removedEdges = null,
        BlowChangeOrigin origin = BlowChangeOrigin.Api, HistoryAction history = HistoryAction.Record)
    {
        // Inside EditAsync the edit as a whole is committed once, at its end.
        if (InEdit) return;

        if (history == HistoryAction.Record) CaptureHistory(label);
        else if (history == HistoryAction.Rebase && _history is not null && !_restoring
                 && _history.Rebase(NodesOutsideGesture(), Edges, _viewport))
        {
            NotifyHistoryChanged();
        }
        if (!OnGraphChanged.HasDelegate) return;

        _ = OnGraphChanged.InvokeAsync(new BlowGraphChange(kind, origin)
        {
            Nodes = nodes ?? [],
            Edges = edges ?? [],
            RemovedNodes = removedNodes ?? [],
            RemovedEdges = removedEdges ?? [],
            Label = label,
        });
    }

    /// <summary>
    /// The nodes as they stand outside the gesture running now: a node being dragged or resized - or a parent growing
    /// for it - at its place and size from before the gesture, which the gesture's own step moves it from when it ends.
    /// A change written into every step from elsewhere must not take a half-finished move with it: the place the node
    /// started from would be lost, and undoing the move would leave it wherever the pointer happened to be.
    /// </summary>
    private IReadOnlyList<BlowNode> NodesOutsideGesture()
    {
        var resized = _resizing ? _resizeNode : null;
        if (_dragOriginals.Count == 0 && resized is null) return Nodes;

        var nodes = new List<BlowNode>(Nodes.Count);
        foreach (var node in Nodes)
        {
            if (_dragOriginals.TryGetValue(node, out var original))
                nodes.Add(Settled(node, original.Position, original.Width, original.Height));
            else if (ReferenceEquals(node, resized))
                nodes.Add(Settled(node, _resizeStartPos, _resizeOriginalSize.Width, _resizeOriginalSize.Height));
            else
                nodes.Add(node);
        }
        return nodes;

        static BlowNode Settled(BlowNode node, BlowXYPosition position, double? width, double? height)
        {
            var copy = node.Clone();
            copy.Position = position;
            copy.Width = width;
            copy.Height = height;
            return copy;
        }
    }

    /// <summary>Goes back one step. Returns false when there was nothing to go back to.</summary>
    public async Task<bool> UndoAsync()
    {
        if (_history?.Undo() is not { } snapshot) return false;
        await ApplyHistoryAsync(snapshot, "undo");
        return true;
    }

    /// <summary>Goes forward one step. Returns false when nothing had been undone.</summary>
    public async Task<bool> RedoAsync()
    {
        if (_history?.Redo() is not { } snapshot) return false;
        await ApplyHistoryAsync(snapshot, "redo");
        return true;
    }

    private async Task ApplyHistoryAsync(BlowSnapshot snapshot, string label)
    {
        _restoring = true;
        try
        {
            // The graph only, not the viewport: undoing an edit should not also throw away where
            // the user had scrolled to since making it.
            await RestoreCoreAsync(snapshot, restoreViewport: false, detach: true, BlowChangeOrigin.History, label);
        }
        finally
        {
            _restoring = false;
        }

        NotifyHistoryChanged();
    }

    private void NotifyHistoryChanged()
    {
        if (_history is null || !OnHistoryChanged.HasDelegate) return;
        _ = OnHistoryChanged.InvokeAsync(new BlowHistoryState(
            _history.CanUndo, _history.CanRedo, _history.UndoDepth, _history.RedoDepth)
        {
            UndoLabel = _history.UndoLabel,
            RedoLabel = _history.RedoLabel,
        });
    }

    /// <summary>
    /// Creates or discards the built-in history to match <see cref="HistoryEnabled"/>. Called on
    /// every parameter set, so turning it on part-way through a session starts from what is on
    /// screen rather than from nothing.
    /// </summary>
    private void SyncHistory()
    {
        if (!HistoryEnabled)
        {
            _history = null;
            return;
        }

        // Compared with the capacity actually used, or a value below one would never match it and
        // every re-render would throw the undo stack away.
        var capacity = Math.Max(1, HistoryCapacity);
        if (_history is not null && _history.Capacity == capacity) return;

        _history = new BlowHistory(capacity);
        _history.Capture(ToObject());
    }

    // ---- clipboard ----

    /// <summary>
    /// What was last copied or cut from this flow. Held per canvas rather than in the system
    /// clipboard, which a browser will not let a component read without a user gesture aimed
    /// at it - and which would carry no graph structure anyway.
    /// </summary>
    private BlowFragment _clipboard = BlowFragment.Empty;

    /// <summary>True when there is something to paste.</summary>
    public bool CanPaste => !_clipboard.IsEmpty;

    /// <summary>
    /// Copies the current selection into the flow's clipboard and returns it.
    /// </summary>
    /// <remarks>
    /// Every edge running between two selected nodes comes along, whether or not the edge itself
    /// was selected: copying three nodes and getting back three unconnected ones is not what
    /// anyone means by copying part of a graph. An edge with one end outside the selection is
    /// left behind, since there would be nothing for it to connect to.
    /// </remarks>
    public BlowFragment Copy()
    {
        var selected = Nodes.Where(n => n.Selected).ToList();
        if (selected.Count == 0) return _clipboard;

        _clipboard = BlowClipboard.Copy(selected, Edges, NodeOrigin);
        return _clipboard;
    }

    /// <summary>Copies the current selection, then deletes it.</summary>
    public Task<BlowFragment> CutAsync() => CutAsync(BlowChangeOrigin.Api);

    private async Task<BlowFragment> CutAsync(BlowChangeOrigin origin)
    {
        var copied = Copy();
        await DeleteSelectionAsync("cut", origin);
        return copied;
    }

    /// <summary>
    /// Adds a fresh copy of the flow's clipboard to the graph, selected and offset so it is
    /// visible. Returns what was added, or an empty fragment when there was nothing to paste.
    /// </summary>
    public Task<BlowFragment> PasteAsync() => PasteAsync(_clipboard);

    /// <summary>Adds a fresh copy of the given fragment to the graph.</summary>
    public Task<BlowFragment> PasteAsync(BlowFragment fragment, BlowXYPosition? offset = null)
        => PasteAsync(fragment, offset, "paste", BlowChangeOrigin.Api);

    private async Task<BlowFragment> PasteAsync(BlowFragment fragment, BlowXYPosition? offset, string label, BlowChangeOrigin origin)
    {
        if (fragment.IsEmpty) return BlowFragment.Empty;

        var pasted = BlowClipboard.Paste(fragment, new BlowPasteOptions
        {
            Offset = offset ?? PasteOffset,
            ExistingIds = Nodes.Select(n => n.Id).Concat(Edges.Select(e => e.Id)),
            Select = ElementsSelectable,
        });

        // The paste becomes the selection, which is what the user expects to be able to move or
        // delete straight afterwards.
        if (ElementsSelectable) ClearSelection();

        Nodes.AddRange(pasted.Nodes);
        Edges.AddRange(pasted.Edges);

        _indexDirty = true;
        _edgeIndexDirty = true;
        _needsMeasure = true;

        Commit(BlowChangeKind.Added, label, pasted.Nodes, pasted.Edges, origin: origin);
        await NotifySelectionChangedAsync();
        await OnPaste.InvokeAsync(pasted);
        await RaiseNodesChangedAsync();
        await RaiseEdgesChangedAsync();
        StateHasChanged();

        return pasted;
    }

    /// <summary>
    /// Copies the selection and pastes it in one step, leaving the clipboard untouched - the
    /// Ctrl+D that every editor has, and which should not cost you what you had copied.
    /// </summary>
    public Task<BlowFragment> DuplicateSelectionAsync() => DuplicateSelectionAsync(BlowChangeOrigin.Api);

    private async Task<BlowFragment> DuplicateSelectionAsync(BlowChangeOrigin origin)
    {
        var selected = Nodes.Where(n => n.Selected).ToList();
        if (selected.Count == 0) return BlowFragment.Empty;

        var fragment = BlowClipboard.Copy(selected, Edges, NodeOrigin);
        return await PasteAsync(fragment, null, "duplicate", origin);
    }

    /// <summary>Selects every selectable, visible node and edge. Mirrors a Ctrl+A style select-all.</summary>
    public async Task SelectAllAsync()
    {
        if (!ElementsSelectable) return;
        foreach (var n in Nodes) n.Selected = n.Selectable && !n.Hidden;
        _ = Index;
        foreach (var ed in Edges) ed.Selected = ed.Selectable && !ed.Hidden && IsEdgeVisible(ed);
        await NotifySelectionChangedAsync();
        StateHasChanged();
    }

    private void ApplyBoxSelection()
    {
        var rect = SelectionRect();
        var partial = SelectionMode == BlowSelectionMode.Partial;

        var selectedNodes = new HashSet<string>();
        foreach (var n in Nodes)
        {
            if (n.Hidden || !n.Selectable) continue;
            var inside = NodeIntersects(n.GetRect(), rect, partial);
            n.Selected = inside || (_selAdditive && _preSelected.Contains(NodeKey(n.Id)));
            if (n.Selected) selectedNodes.Add(n.Id);
        }

        if (!SelectEdgesOnSelectionBox) return;

        // An edge belongs to the box when both of the nodes it joins do.
        foreach (var e in Edges)
        {
            if (e.Hidden || !e.Selectable) continue;
            var inside = selectedNodes.Contains(e.Source) && selectedNodes.Contains(e.Target);
            e.Selected = inside || (_selAdditive && _preSelected.Contains(EdgeKey(e.Id)));
        }
    }

    private BlowRect SelectionRect()
    {
        var x = Math.Min(_selStart.X, _selEnd.X);
        var y = Math.Min(_selStart.Y, _selEnd.Y);
        var w = Math.Abs(_selEnd.X - _selStart.X);
        var h = Math.Abs(_selEnd.Y - _selStart.Y);
        return new BlowRect(x, y, w, h);
    }

    private static bool RectsIntersect(BlowRect a, BlowRect b) =>
        a.Left < b.Right && a.Right > b.Left && a.Top < b.Bottom && a.Bottom > b.Top;

    /// <summary>
    /// Places one alignment guide. The line is drawn inside the transformed viewport layer, so
    /// its length is in flow units, but its thickness is divided by the zoom to keep it a
    /// hairline on screen at any scale.
    /// </summary>
    private string GuideStyle(BlowAlignmentGuide guide)
    {
        var thickness = 1 / _viewport.Zoom;
        var length = guide.End - guide.Start;

        return guide.Orientation == BlowGuideOrientation.Vertical
            ? string.Create(System.Globalization.CultureInfo.InvariantCulture,
                $"transform:translate({guide.Coordinate - thickness / 2}px,{guide.Start}px);"
                + $"width:{thickness}px;height:{length}px;background:{GuideColor};")
            : string.Create(System.Globalization.CultureInfo.InvariantCulture,
                $"transform:translate({guide.Start}px,{guide.Coordinate - thickness / 2}px);"
                + $"width:{length}px;height:{thickness}px;background:{GuideColor};");
    }

    private string SelectionStyle() => string.Create(
        System.Globalization.CultureInfo.InvariantCulture,
        $"transform:translate({SelectionRect().X}px,{SelectionRect().Y}px);width:{SelectionRect().Width}px;height:{SelectionRect().Height}px;");

    /// <summary>
    /// Raises <see cref="OnSelectionChange"/>, but only when the selected set actually changed.
    /// Interactions call this liberally (a click on empty space clears the selection whether or
    /// not anything was selected), so the no-op guard keeps consumers from re-rendering for nothing.
    /// </summary>
    private Task NotifySelectionChangedAsync()
    {
        var nodes = Nodes.Where(n => n.Selected).ToList();
        var edges = Edges.Where(ed => ed.Selected).ToList();

        var deselected = _deselected;
        _deselected = false;
        if (!deselected
            && _lastSelection.Count == nodes.Count + edges.Count
            && nodes.TrueForAll(n => _lastSelection.Contains(NodeKey(n.Id)))
            && edges.TrueForAll(e => _lastSelection.Contains(EdgeKey(e.Id))))
        {
            return Task.CompletedTask;
        }

        _lastSelection.Clear();
        foreach (var n in nodes) _lastSelection.Add(NodeKey(n.Id));
        foreach (var e in edges) _lastSelection.Add(EdgeKey(e.Id));

        return OnSelectionChange.HasDelegate
            ? OnSelectionChange.InvokeAsync(new BlowSelectionChange(nodes, edges))
            : Task.CompletedTask;
    }

    // Node and edge ids live in the same sets, so each is tagged. The separator matters for the
    // same reason it does when keying connections: without it a node called "e:x" and an edge
    // called "x" would be the same entry.
    private static string NodeKey(string id) => "n\u001f" + id;

    private static string EdgeKey(string id) => "e\u001f" + id;

    private async Task DeleteSelectionAsync(string label, BlowChangeOrigin origin)
    {
        var nodesToDelete = WithDescendants(Nodes.Where(n => n.Selected && n.Deletable).ToList());
        var edgesToDelete = Edges.Where(ed => ed.Selected && ed.Deletable).ToList();
        if (nodesToDelete.Count == 0 && edgesToDelete.Count == 0) return;

        var removedNodes = nodesToDelete.Select(n => n.Id).ToHashSet();
        var selectedEdgeIds = edgesToDelete.Select(e => e.Id).ToHashSet();

        // Edges removed = explicitly selected edges plus those attached to removed nodes.
        // An edge dangling off a deleted node always goes, regardless of its Deletable flag.
        var removedEdges = Edges
            .Where(ed => selectedEdgeIds.Contains(ed.Id)
                         || removedNodes.Contains(ed.Source) || removedNodes.Contains(ed.Target))
            .ToList();

        // The veto sees the full effect of the deletion, including cascaded edges.
        if (OnBeforeDelete is not null && !OnBeforeDelete(nodesToDelete, removedEdges))
            return;

        if (removedNodes.Count > 0)
        {
            Nodes.RemoveAll(n => removedNodes.Contains(n.Id));
            Edges.RemoveAll(ed => removedNodes.Contains(ed.Source) || removedNodes.Contains(ed.Target));
        }
        Edges.RemoveAll(ed => selectedEdgeIds.Contains(ed.Id));

        _indexDirty = true;
        _edgeIndexDirty = true;
        EndConnectionIfOrphaned();
        Commit(BlowChangeKind.Removed, label, removedNodes: nodesToDelete, removedEdges: removedEdges, origin: origin);
        await NotifySelectionChangedAsync();
        Announce(AriaLabels.Deleted(nodesToDelete.Count, removedEdges.Count));
        await RaiseDeleteEventsAsync(nodesToDelete, removedEdges);
        await RaiseNodesChangedAsync();
        await RaiseEdgesChangedAsync();
        StateHasChanged();
    }

    /// <summary>
    /// The given nodes plus every node nested inside them. A child's position is relative to its
    /// parent and means nothing without it - left behind, it would be laid out as a root node at its
    /// relative coordinates - so it goes with the parent whatever its own <see cref="BlowNode.Deletable"/>
    /// says, the way an edge goes with the node it hangs off.
    /// </summary>
    private List<BlowNode> WithDescendants(List<BlowNode> nodes)
    {
        if (nodes.Count == 0) return nodes;

        var ids = nodes.Select(n => n.Id).ToHashSet();
        List<BlowNode>? result = null;
        foreach (var n in Nodes)
        {
            if (ids.Contains(n.Id) || !HasAncestorIn(n, ids)) continue;
            ids.Add(n.Id);
            (result ??= [.. nodes]).Add(n);
        }
        return result ?? nodes;
    }

    private async Task RaiseDeleteEventsAsync(IReadOnlyList<BlowNode> nodes, IReadOnlyList<BlowEdge> edges)
    {
        if (nodes.Count > 0 && OnNodesDelete.HasDelegate) await OnNodesDelete.InvokeAsync(nodes);
        if (edges.Count > 0 && OnEdgesDelete.HasDelegate) await OnEdgesDelete.InvokeAsync(edges);
        if ((nodes.Count > 0 || edges.Count > 0) && OnDelete.HasDelegate)
            await OnDelete.InvokeAsync(new BlowSelectionChange(nodes, edges));
    }

    // ---- connection handling (called by Handle via IBlowContext) ----

    /// <summary>Begins a connection drag from the given handle. Called by <c>BlowHandle</c>.</summary>
    public void StartConnection(string nodeId, string? handleId, BlowHandleType handleType, BlowPosition position)
        => StartConnection(nodeId, handleId, handleType, position, NoPointer);

    /// <summary>
    /// Begins a connection drag from the given handle, capturing the pointer on the pane so the
    /// gesture survives the pointer leaving the flow.
    /// </summary>
    public void StartConnection(string nodeId, string? handleId, BlowHandleType handleType,
        BlowPosition position, long pointerId)
        => StartConnection(nodeId, handleId, handleType, position, pointerId, double.NaN, double.NaN);

    /// <summary>
    /// Begins a connection drag, additionally telling the canvas where on screen the gesture
    /// started so <see cref="ConnectionDragThreshold"/> has a real origin to measure from.
    /// </summary>
    public void StartConnection(string nodeId, string? handleId, BlowHandleType handleType,
        BlowPosition position, long pointerId, double clientX, double clientY)
    {
        AbandonKeyboardConnection();

        // A full port starts nothing: every connection from it would be refused anyway.
        if (!CanStartConnection(nodeId, handleId, handleType)) return;
        var node = Index[nodeId];

        // Nothing of an earlier gesture's aim carries over into this one.
        _connHover = null;
        _connSnap = null;
        _connNear = null;
        _connRejection = BlowConnectionRejection.None;

        var (x, y, pos) = ResolveAnchor(node, handleType, handleId, position);
        _connSource = new ConnectionSource(nodeId, handleId, handleType, pos, new BlowXYPosition(x, y));
        _connEnd = new BlowXYPosition(x, y);

        // Callers that know where the gesture began give the drag threshold a real origin;
        // the rest fall back to the handle's own position on screen.
        if (double.IsNaN(clientX) || double.IsNaN(clientY))
        {
            var onScreen = FlowToScreenPosition(new BlowXYPosition(x, y));
            clientX = onScreen.X;
            clientY = onScreen.Y;
        }
        _connStartClientX = clientX;
        _connStartClientY = clientY;

        _connecting = true;
        _connectionVersion++;
        _connEndValid = false;
        _connMoved = false;
        _reconnectEdge = null;
        BeginConnectionSession();
        CapturePane(pointerId);
        _ = OnConnectStart.InvokeAsync(new BlowConnectionStartInfo(nodeId, handleId, handleType));
        NotifyConnectionChange();
        _ = RefreshPaneRectAsync();
        StateHasChanged();
    }

    /// <summary>
    /// Begins reconnecting one end of an existing edge. The opposite (fixed) end becomes
    /// the connection source, so dragging behaves like drawing a new connection from it.
    /// </summary>
    private void StartReconnect(BlowEdge edge, BlowHandleType movingEnd, PointerEventArgs e)
    {
        if (e.Button != 0 || !EdgesReconnectable || !edge.Reconnectable) return;
        OnPointerPress();
        BeginReconnect(edge, movingEnd, ScreenToFlow(e), e.ClientX, e.ClientY, e.PointerId);
    }

    /// <summary>
    /// Picks up one end of an existing edge, for a drag (<paramref name="pointerId"/> set) or the keyboard. The line
    /// starts out ending at <paramref name="end"/>. False when the node of the end that stays has gone.
    /// </summary>
    private bool BeginReconnect(BlowEdge edge, BlowHandleType movingEnd, BlowXYPosition end, double clientX, double clientY,
        long? pointerId)
    {
        _connHover = null;
        _connSnap = null;
        _connNear = null;
        _connRejection = BlowConnectionRejection.None;

        var fixedType = movingEnd == BlowHandleType.Target ? BlowHandleType.Source : BlowHandleType.Target;
        var fixedNodeId = fixedType == BlowHandleType.Source ? edge.Source : edge.Target;
        var fixedHandle = fixedType == BlowHandleType.Source ? edge.SourceHandle : edge.TargetHandle;

        if (!Index.TryGetValue(fixedNodeId, out var node)) return false;
        var fallback = fixedType == BlowHandleType.Source ? node.SourcePosition : node.TargetPosition;

        var (x, y, pos) = ResolveAnchor(node, fixedType, fixedHandle, fallback);
        _connSource = new ConnectionSource(node.Id, fixedHandle, fixedType, pos, new BlowXYPosition(x, y));
        _connEnd = end;
        _connEndClientX = clientX;
        _connEndClientY = clientY;
        _connecting = true;
        _connectionVersion++;
        _connEndValid = false;
        _connMoved = pointerId is not null; // a reconnect drawn with the pointer is always a drag, never a click-to-connect
        _reconnectEdge = edge;
        BeginConnectionSession();
        if (pointerId is { } id) CapturePane(id);
        _ = OnReconnectStart.InvokeAsync(new BlowEdgeReconnectStart(edge, movingEnd));
        _ = OnConnectStart.InvokeAsync(new BlowConnectionStartInfo(node.Id, fixedHandle, fixedType));
        NotifyConnectionChange();
        _ = RefreshPaneRectAsync();
        StateHasChanged();
        return true;
    }

    /// <summary>Resolves the source and target endpoint anchors of an edge in flow space.</summary>
    private (BlowXYPosition Source, BlowXYPosition Target) GetEdgeEndpoints(BlowEdge edge)
    {
        var ctx = GetEdgeContext(edge);
        return (ctx.Source, ctx.Target);
    }

    /// <summary>Completes the in-progress connection on the given handle, if it is a valid target.</summary>
    /// <remarks>
    /// With <see cref="OnBeforeConnect"/> set, the outcome arrives later: the gesture ends here, and the edge is
    /// added once the confirmation comes back.
    /// </remarks>
    public void CompleteConnection(string nodeId, string? handleId, BlowHandleType handleType)
    {
        if (!_connecting) return;

        // The session was built when the drag began, and the graph may have changed since - an edit from code, a
        // collaborator's edge. What the user let go on is judged against the graph as it is now.
        var drop = new ConnectionDrop(_connSource.NodeId, _connSource.HandleId, _connSource.Type, nodeId, handleId, handleType);
        var check = RecheckConnection(drop, _reconnectEdge, out var fresh);
        if (!check.IsValid)
        {
            _connEndValid = false;
            _connRejection = check.Rejection;
            CancelConnection();
            return;
        }

        var connection = fresh ?? DescribeConnection(nodeId, handleId, handleType);
        var reconnecting = _reconnectEdge;

        if (OnBeforeConnect is not { } confirm)
        {
            ApplyConnection(connection, reconnecting);
            _connEndValid = true;
            _reconnectRewired = reconnecting is not null;
            CancelConnection();
            return;
        }

        // The gesture is over as far as the user can see: the line goes now, not when the answer comes back. The
        // confirmation runs on its own, so the pointer release that got here finishes - releasing its capture -
        // without waiting on a server, and the user can start the next gesture meanwhile.
        var end = TakeConnectionEnd();
        StateHasChanged();
        _ = SurfaceFailureAsync(ConfirmConnectionAsync(confirm, connection, drop, reconnecting, end));
    }

    /// <summary>Hands an exception from work nobody awaits to the renderer, rather than losing it.</summary>
    private async Task SurfaceFailureAsync(Task work)
    {
        try { await work; }
        catch (Exception ex) when (!_disposed) { await DispatchExceptionAsync(ex); }
    }

    /// <summary>Which two handles a connection joins, each as the kind of handle it physically is.</summary>
    private readonly record struct ConnectionDrop(
        string FromNode, string? FromHandle, BlowHandleType FromType, string ToNode, string? ToHandle, BlowHandleType ToType);

    /// <summary>
    /// How many times <see cref="OnBeforeConnect"/> is asked about one connection whose wiring keeps changing while it
    /// answers, before the connection is dropped.
    /// </summary>
    private const int MaxConfirmations = 3;

    private async Task ConfirmConnectionAsync(Func<BlowConnection, Task<bool>> confirm, BlowConnection connection,
        ConnectionDrop drop, BlowEdge? reconnecting, ConnectionEnd end)
    {
        var raised = false;
        try
        {
            BlowConnectionCheck now;
            for (var asked = 1; ; asked++)
            {
                var confirmed = await confirm(connection);
                if (_disposed) return;

                // The graph may have moved on while the confirmation ran - another connection made, the edge being
                // reconnected deleted, a node removed by a collaborator - so the connection is judged again as of now.
                BlowConnection? fresh = null;
                now = confirmed
                    ? RecheckConnection(drop, reconnecting, out fresh)
                    : new BlowConnectionCheck(BlowConnectionRejection.RejectedByRule);
                if (!now.IsValid || fresh == connection) break;

                // Still allowed, but no longer the wiring the app said yes to: the end of a reconnected edge that stays
                // has moved meanwhile - another reconnect of the same edge confirmed first, say. The app approves
                // exactly what is made, so it is asked again about the wiring as it is now; one that will not hold
                // still is dropped after a few tries.
                if (asked == MaxConfirmations)
                {
                    now = new BlowConnectionCheck(BlowConnectionRejection.RejectedByRule);
                    break;
                }
                connection = fresh!.Value;
            }

            if (now.IsValid) ApplyConnection(connection, reconnecting);

            raised = true;
            RaiseConnectionEnd(end with
            {
                Valid = now.IsValid,
                Rewired = now.IsValid && reconnecting is not null,
                Rejection = now.Rejection,
            });
            StateHasChanged();
        }
        finally
        {
            // A confirmation that threw still ends the gesture it was confirming: every start has its end.
            if (!raised && !_disposed) RaiseConnectionEnd(end with { Valid = false, Rejection = BlowConnectionRejection.RejectedByRule });
        }
    }

    /// <summary>
    /// Checks a connection against the graph as it is now, with a session built afresh - the one built when the
    /// drag began may describe a graph that no longer exists. Each end is looked up as the kind of handle it
    /// physically is, exactly as the session the drag used does, so loose mode is judged the same way twice.
    /// </summary>
    /// <param name="drop">The two handles the connection joins.</param>
    /// <param name="reconnecting">The edge whose end is being moved, if any. Its other end is taken as it is now.</param>
    /// <param name="connection">The connection as of now, when it is valid.</param>
    private BlowConnectionCheck RecheckConnection(ConnectionDrop drop, BlowEdge? reconnecting, out BlowConnection? connection)
    {
        connection = null;
        if (!NodesConnectable) return new BlowConnectionCheck(BlowConnectionRejection.NotConnectable);
        if (reconnecting is not null)
        {
            if (!Edges.Contains(reconnecting)) return new BlowConnectionCheck(BlowConnectionRejection.MissingNode);
            if (!EdgesReconnectable || !reconnecting.Reconnectable) return new BlowConnectionCheck(BlowConnectionRejection.NotConnectable);

            // The end that stays is wherever it is now: the app, or another reconnect confirmed meanwhile, may have
            // moved it since this one began, and writing back the old one would undo that.
            drop = drop.FromType == BlowHandleType.Source
                ? drop with { FromNode = reconnecting.Source, FromHandle = reconnecting.SourceHandle }
                : drop with { FromNode = reconnecting.Target, FromHandle = reconnecting.TargetHandle };
            if (!Index.ContainsKey(drop.FromNode)) return new BlowConnectionCheck(BlowConnectionRejection.MissingNode);
        }

        var session = BlowConnectionRules.Begin(new BlowGraphIndex(Nodes, Edges),
            drop.FromNode, drop.FromHandle, drop.FromType, ConnectionPolicy(), reconnecting);
        var check = session.Check(drop.ToNode, drop.ToHandle, drop.ToType);
        if (!check.IsValid) return check;

        var described = session.Describe(drop.ToNode, drop.ToHandle, drop.ToType);
        if (HandleFor(drop.ToNode, drop.ToType, drop.ToHandle)?.IsValidConnection is { } targetRule && !targetRule(described))
            return new BlowConnectionCheck(BlowConnectionRejection.RejectedByRule);

        if (HandleFor(drop.FromNode, drop.FromType, drop.FromHandle)?.IsValidConnection is { } sourceRule && !sourceRule(described))
            return new BlowConnectionCheck(BlowConnectionRejection.RejectedByRule);

        connection = described;
        return check;
    }

    /// <summary>
    /// Makes the edge a completed connection describes: a new one, or the rewired edge being reconnected. Touches no
    /// gesture state - after an asynchronous confirmation the user may already be in the middle of the next one.
    /// </summary>
    private void ApplyConnection(BlowConnection connection, BlowEdge? reconnecting)
    {
        if (reconnecting is { } re)
        {
            // Reconnecting an existing edge: rewire it in place.
            re.Source = connection.Source!;
            re.SourceHandle = connection.SourceHandle;
            re.Target = connection.Target!;
            re.TargetHandle = connection.TargetHandle;
            _needsMeasure = true;
            Commit(BlowChangeKind.Reconnected, "reconnect", edges: [re], origin: BlowChangeOrigin.User);
            Announce(AriaLabels.Reconnected(NameOf(re.Source), NameOf(re.Target)));
            _ = OnReconnect.InvokeAsync(new BlowEdgeReconnect(re, connection));
            _ = RaiseEdgesChangedAsync();
            return;
        }

        // The id is derived from the wiring, so it can already be taken: by a parallel
        // edge when duplicates are allowed, or by an edge that was created here and has
        // since been reconnected elsewhere (reconnecting keeps the id). Reusing it would
        // give two edges one id, and the second would silently never be drawn.
        var id = BlowGraphUtils.BuildEdgeId(connection);
        if (EdgeIndex.ContainsKey(id))
        {
            var suffix = 2;
            while (EdgeIndex.ContainsKey($"{id}-{suffix}")) suffix++;
            id = $"{id}-{suffix}";
        }

        var edge = new BlowEdge
        {
            Id = id,
            Source = connection.Source!,
            Target = connection.Target!,
            SourceHandle = connection.SourceHandle,
            TargetHandle = connection.TargetHandle,
            Type = DefaultEdgeType,
        };
        DefaultEdgeOptions?.ApplyTo(edge, DefaultEdgeType);
        Edges.Add(edge);
        _edgeIndexDirty = true;
        Commit(BlowChangeKind.Connected, "connect", edges: [edge], origin: BlowChangeOrigin.User);
        Announce(AriaLabels.Connected(NameOf(connection.Source!), NameOf(connection.Target!)));
        _ = OnConnect.InvokeAsync(connection);
        _ = RaiseEdgesChangedAsync();
        NotifyHandlesConnected(connection);
    }

    /// <summary>The name a node id is announced by, or the id itself for a node no longer in the graph.</summary>
    private string NameOf(string nodeId) => Index.TryGetValue(nodeId, out var node) ? NameOf(node) : nodeId;

    /// <summary>
    /// Raises the <c>OnConnect</c> callback of both handles a new connection joins. The pane
    /// captures the pointer for the whole gesture, so the handle itself never sees the release
    /// that completed the connection and cannot raise this for itself.
    /// </summary>
    private void NotifyHandlesConnected(BlowConnection connection)
    {
        if (_handles.Count == 0) return;

        if (connection.Source is { } src
            && HandleFor(src, BlowHandleType.Source, connection.SourceHandle) is { } sourceReg
            && sourceReg.OnConnect.HasDelegate)
        {
            _ = sourceReg.OnConnect.InvokeAsync(connection);
        }

        if (connection.Target is { } tgt
            && HandleFor(tgt, BlowHandleType.Target, connection.TargetHandle) is { } targetReg
            && targetReg.OnConnect.HasDelegate)
        {
            _ = targetReg.OnConnect.InvokeAsync(connection);
        }
    }

    private bool TryCompleteByProximity()
    {
        var (valid, nearest) = ClosestHandles();
        if (valid is not { } bh)
        {
            // Dropped close to a handle that refused, and to none that would accept: say why it did not connect.
            if (_connRejection == BlowConnectionRejection.None && nearest is { } refused) _connRejection = refused.Rejection;
            return false;
        }

        CompleteConnection(bh.nodeId, bh.handleId, bh.type);
        return true;
    }

    /// <summary>
    /// The handles within <see cref="ConnectionRadius"/> of the connection's loose end: the nearest one the
    /// connection could legally complete on, and, when there is none, the nearest one that refuses it and why.
    /// </summary>
    private ((string nodeId, string? handleId, BlowHandleType type)? Valid,
        (string NodeId, string? HandleId, BlowHandleType Type, BlowConnectionRejection Rejection)? Refused) ClosestHandles()
    {
        if (ConnectionRadius <= 0) return (null, null);
        var radius = ConnectionRadius / _viewport.Zoom;

        double best = double.MaxValue, bestRefused = double.MaxValue;
        (string nodeId, string? handleId, BlowHandleType type)? bestHandle = null;
        (string, string?, BlowHandleType, BlowConnectionRejection)? refused = null;

        foreach (var n in Nodes)
        {
            if (n.Hidden || !n.Connectable) continue;
            foreach (var (_, h) in n.Handles)
            {
                if (!h.ConnectableEnd) continue;

                var hx = n.AbsolutePosition.X + h.OffsetX;
                var hy = n.AbsolutePosition.Y + h.OffsetY;
                var dx = hx - _connEnd.X;
                var dy = hy - _connEnd.Y;
                var d = Math.Sqrt(dx * dx + dy * dy);
                if (d > radius || (d >= best && d >= bestRefused)) continue;

                var hid = string.IsNullOrEmpty(h.HandleId) ? null : h.HandleId;

                // The handle the line comes out of is where it started, not somewhere it was refused.
                if (IsConnectionSource(n.Id, hid, h.Type)) continue;

                var check = CheckConnection(n.Id, hid, h.Type);
                if (check.IsValid)
                {
                    if (d >= best) continue;
                    best = d;
                    bestHandle = (n.Id, hid, h.Type);
                }
                else if (d < bestRefused)
                {
                    bestRefused = d;
                    refused = (n.Id, hid, h.Type, check.Rejection);
                }
            }
        }

        return (bestHandle, bestHandle is null ? refused : null);
    }

    private void CancelConnection()
    {
        var end = TakeConnectionEnd();
        RaiseConnectionEnd(end);
        StateHasChanged();
    }

    /// <summary>Everything the end of a connection gesture reports, captured before its state is reset.</summary>
    private readonly record struct ConnectionEnd(
        BlowConnectionStartInfo Start, BlowXYPosition FlowPosition, double ClientX, double ClientY, bool Valid,
        bool Dropped, BlowEdge? Reconnected, bool Rewired, BlowConnectionRejection Rejection);

    /// <summary>Ends the gesture: records how it ended and resets the connection state, raising nothing yet.</summary>
    private ConnectionEnd TakeConnectionEnd()
    {
        var end = new ConnectionEnd(
            new BlowConnectionStartInfo(_connSource.NodeId, _connSource.HandleId, _connSource.Type),
            _connEnd, _connEndClientX, _connEndClientY, _connEndValid, _connDropping, _reconnectEdge,
            _reconnectRewired, _connEndValid ? BlowConnectionRejection.None : _connRejection);

        _connecting = false;
        _keyboardConnecting = false;
        _keyboardAimValid = false;
        _connectionVersion++;
        _connMoved = false;
        _connDropping = false;
        _connHover = null;
        _connSnap = null;
        _connNear = null;
        _connRejection = BlowConnectionRejection.None;
        _connEndValid = false;
        _connSession = null;
        _reconnectEdge = null;
        _reconnectRewired = false;
        NotifyConnectionChange();
        return end;
    }

    private void RaiseConnectionEnd(ConnectionEnd end)
    {
        if (end.Rejection != BlowConnectionRejection.None)
            Announce(AriaLabels.ConnectionRejected(end.Rejection));

        if (end.Reconnected is { } reconnected && OnReconnectEnd.HasDelegate)
        {
            var movingEnd = end.Start.HandleType == BlowHandleType.Source ? BlowHandleType.Target : BlowHandleType.Source;
            _ = OnReconnectEnd.InvokeAsync(new BlowEdgeReconnectEnd(reconnected, movingEnd, end.Rewired));
        }

        _ = OnConnectEnd.InvokeAsync(new BlowConnectEndInfo(end.Start, end.FlowPosition, end.ClientX, end.ClientY, end.Valid)
        {
            IsDropped = end.Dropped,
            IsReconnect = end.Reconnected is not null,
            Rejection = end.Rejection,
        });
    }

    /// <summary>Tells the canvas the pointer has entered or left a handle while connecting.</summary>
    public void SetConnectionHover(string nodeId, string? handleId, BlowHandleType handleType, bool isOver)
    {
        // A connection made from the keyboard is aimed by focus; a pointer passing over a handle does not steer it.
        if (!_connecting || _keyboardConnecting) return;
        _connHover = isOver ? (nodeId, handleId, handleType) : null;
        NotifyConnectionChange();
    }

    /// <summary>
    /// The verdict on a handle the pointer is over: the rules', unless the handle itself refuses drops (its
    /// <c>IsConnectableEnd</c> or <c>IsConnectable</c>), which only its measured anchor records.
    /// </summary>
    private BlowConnectionCheck HoverCheck(string nodeId, string? handleId, BlowHandleType handleType)
    {
        if (Index.TryGetValue(nodeId, out var node)
            && node.Handles.TryGetValue(BlowHandleBounds.Key(handleType, handleId), out var bounds)
            && !bounds.ConnectableEnd)
        {
            return new BlowConnectionCheck(BlowConnectionRejection.NotConnectable);
        }
        return CheckConnection(nodeId, handleId, handleType);
    }

    /// <summary>
    /// True when the in-progress connection started from this exact handle, so it can be shown
    /// as the origin of the drag rather than as a rejected target.
    /// </summary>
    public bool IsConnectionSource(string nodeId, string? handleId, BlowHandleType handleType)
        => _connecting
           && _connSource.NodeId == nodeId
           && _connSource.HandleId == handleId
           && _connSource.Type == handleType;

    /// <summary>True when the in-progress connection could legally complete on the given handle.</summary>
    public bool IsValidConnectionTarget(string nodeId, string? handleId, BlowHandleType handleType)
        => CheckConnection(nodeId, handleId, handleType).IsValid;

    /// <summary>
    /// Whether the in-progress connection could complete on the given handle, and if not, why. Every rule is
    /// consulted, in the order <see cref="BlowConnectionRules"/> documents: the canvas-wide ones, the ports'
    /// data types and limits, <see cref="IsValidConnection"/>, and finally the rules of the two handles involved.
    /// Outside a connection gesture every handle is refused as <see cref="BlowConnectionRejection.NotConnectable"/>.
    /// </summary>
    public BlowConnectionCheck CheckConnection(string nodeId, string? handleId, BlowHandleType handleType)
    {
        if (!_connecting || !NodesConnectable || _connSession is not { } session)
            return new BlowConnectionCheck(BlowConnectionRejection.NotConnectable);

        var check = session.Check(nodeId, handleId, handleType);
        if (!check.IsValid) return check;

        // Both ends get a say: the handle being dropped on, and the one the drag started from.
        var connection = session.Describe(nodeId, handleId, handleType);
        if (HandleFor(nodeId, handleType, handleId)?.IsValidConnection is { } targetRule && !targetRule(connection))
            return new BlowConnectionCheck(BlowConnectionRejection.RejectedByRule);

        if (HandleFor(_connSource.NodeId, _connSource.Type, _connSource.HandleId)?.IsValidConnection is { } sourceRule
            && !sourceRule(connection))
        {
            return new BlowConnectionCheck(BlowConnectionRejection.RejectedByRule);
        }

        return check;
    }

    /// <summary>
    /// Describes the connection completing on the given handle would produce, normalised so
    /// <c>Source</c> always refers to the source-side handle.
    /// </summary>
    public BlowConnection DescribeConnection(string nodeId, string? handleId, BlowHandleType handleType)
        => _connSource.Type == BlowHandleType.Source
            ? new BlowConnection(_connSource.NodeId, nodeId, _connSource.HandleId, handleId)
            : new BlowConnection(nodeId, _connSource.NodeId, handleId, _connSource.HandleId);

    /// <summary>
    /// The rules the running connection gesture is checked against, bound to the handle it started from. Built
    /// once when the gesture starts: every handle re-evaluates its validity on every render while a connection is
    /// being dragged, and the session answers each of them in a few lookups rather than a scan of the edge list.
    /// </summary>
    private BlowConnectionSession? _connSession;

    private void BeginConnectionSession()
        => _connSession = BlowConnectionRules.Begin(new BlowGraphIndex(Nodes, Edges),
            _connSource.NodeId, _connSource.HandleId, _connSource.Type, ConnectionPolicy(), _reconnectEdge);

    /// <summary>
    /// Builds the session again for the graph as it is now - nodes added, ports filled or freed, the other end of the
    /// edge being moved changed - when what the connection is checked against has to be current, not as it was when
    /// the gesture began.
    /// </summary>
    private void RefreshConnectionSession()
    {
        if (_reconnectEdge is { } moving) FollowStayingEnd(moving);
        BeginConnectionSession();
    }

    /// <summary>The canvas's connection parameters as the policy <see cref="BlowConnectionRules"/> applies.</summary>
    private BlowConnectionPolicy ConnectionPolicy() => new()
    {
        Mode = ConnectionMode,
        AllowSelfConnections = AllowSelfConnections,
        PreventDuplicates = PreventDuplicateEdges,
        PreventCycles = PreventCycles,
        TypeCompatibility = ConnectionTypeCompatibility,
        IsValidConnection = IsValidConnection,
        ResolvePort = ResolvePort,
    };

    /// <summary>
    /// The port standing for a handle: the one declared on the node's model, with whatever the rendered
    /// <c>BlowHandle</c> says about its data type and limit laid over it.
    /// </summary>
    private BlowPort? ResolvePort(BlowNode node, BlowHandleType type, string? handleId)
    {
        var port = node.FindPort(type, handleId);
        if (HandleFor(node.Id, type, handleId) is not { } handle || (handle.DataType is null && handle.MaxConnections is null))
            return port;

        return new BlowPort
        {
            Id = handleId,
            Type = type,
            Position = port?.Position,
            Label = port?.Label,
            DataType = handle.DataType ?? port?.DataType,
            MaxConnections = handle.MaxConnections ?? port?.MaxConnections,
            MinConnections = port?.MinConnections ?? 0,
            Data = port?.Data,
        };
    }

    /// <summary>
    /// Whether a connection can be started from the given handle right now: the canvas and the node take
    /// connections and the handle's port is not already at its <c>MaxConnections</c>. Used by <c>BlowHandle</c>,
    /// which neither starts a drag nor shows itself as connectable when this is false.
    /// </summary>
    public bool CanStartConnection(string nodeId, string? handleId, BlowHandleType handleType)
        => NodesConnectable && Index.TryGetValue(nodeId, out var node) && node.Connectable && !node.Hidden
           && !IsHandleAtLimit(nodeId, handleId, handleType);

    /// <summary>
    /// True when the given handle's port already has as many edges as its <c>MaxConnections</c> allows. Such a
    /// handle neither starts nor accepts connections, and is not drawn as connectable.
    /// </summary>
    public bool IsHandleAtLimit(string nodeId, string? handleId, BlowHandleType handleType)
    {
        if (!Index.TryGetValue(nodeId, out var node) || ResolvePort(node, handleType, handleId)?.MaxConnections is not { } max)
            return false;

        var count = CountConnections(nodeId, handleType, handleId);

        // The edge being reconnected is the one this connection replaces: dropping it back where it came from is
        // allowed, so its own handle must not look full because of it.
        if (_reconnectEdge is { } re && Attaches(re, nodeId, handleType, handleId)) count--;

        return count >= max;
    }

    /// <summary>
    /// True when the edge is attached to the given handle: at the end of the handle's own type, or in loose mode -
    /// where any handle can be either end - at either end with that handle id.
    /// </summary>
    private bool Attaches(BlowEdge edge, string nodeId, BlowHandleType handleType, string? handleId)
    {
        var atSource = edge.Source == nodeId && BlowGraphIndex.SameHandle(edge.SourceHandle, handleId);
        var atTarget = edge.Target == nodeId && BlowGraphIndex.SameHandle(edge.TargetHandle, handleId);
        if (ConnectionMode == BlowConnectionMode.Loose) return atSource || atTarget;
        return handleType == BlowHandleType.Source ? atSource : atTarget;
    }

    /// <summary>
    /// How many edges are attached to one handle: leaving it for a source handle, arriving for a target - or in
    /// <see cref="BlowConnectionMode.Loose"/> mode, where a handle can be either end, both. Counted once per render
    /// for all handles.
    /// </summary>
    public int CountConnections(string nodeId, BlowHandleType handleType, string? handleId)
        => (_handleCounts ??= new BlowHandleCounts(Edges)).At(nodeId, handleType, handleId, ConnectionMode);

    /// <summary>Edges per handle, built on first use after each render's preparation; see <see cref="CountConnections"/>.</summary>
    private BlowHandleCounts? _handleCounts;

    // ---- viewport controls ----

    /// <summary>Zooms in one step about the pane centre.</summary>
    public void ZoomIn() => ZoomBy(1.2);
    /// <summary>Zooms out one step about the pane centre.</summary>
    public void ZoomOut() => ZoomBy(1 / 1.2);

    private void ZoomBy(double factor) => ZoomTo(_viewport.Zoom * factor);

    /// <summary>Applies a viewport, clamping zoom to Min/Max and honouring <see cref="TranslateExtent"/>.</summary>
    public void SetViewport(BlowViewport viewport)
    {
        StopViewportAnimation();
        ApplyViewport(ResolveViewport(viewport));
    }

    /// <summary>
    /// Eases the viewport to <paramref name="viewport"/> over <paramref name="durationMs"/>
    /// milliseconds. Mirrors the <c>duration</c> option React Flow's viewport helpers take: a
    /// jump cut between two parts of a large graph loses the reader, where a short transition
    /// keeps them oriented. A duration of zero applies the viewport immediately.
    /// </summary>
    public Task SetViewportAsync(BlowViewport viewport, int durationMs)
        => AnimateViewportAsync(ResolveViewport(viewport), durationMs);

    /// <summary>Clamps a requested viewport into the zoom range and the translate extent.</summary>
    private BlowViewport ResolveViewport(BlowViewport viewport)
        => ClampViewport(viewport with { Zoom = Math.Clamp(viewport.Zoom, MinZoom, MaxZoom) });

    /// <summary>
    /// Eases the viewport to a target. Cancels whatever transition was already running, so the
    /// last request wins rather than two animations fighting over the same transform.
    /// </summary>
    private async Task AnimateViewportAsync(BlowViewport target, int durationMs)
    {
        StopViewportAnimation();

        var from = _viewport;
        if (durationMs <= 0 || from == target || ReducesMotion)
        {
            ApplyViewport(target);
            return;
        }

        var cts = new CancellationTokenSource();
        _viewportAnimationCts = cts;

        await OnMoveStart.InvokeAsync(from);

        var clock = System.Diagnostics.Stopwatch.StartNew();
        try
        {
            using var timer = new PeriodicTimer(TimeSpan.FromMilliseconds(16));
            while (await timer.WaitForNextTickAsync(cts.Token))
            {
                var t = Math.Clamp(clock.Elapsed.TotalMilliseconds / durationMs, 0, 1);
                var e = EaseInOut(t);

                await InvokeAsync(() => ApplyViewport(new BlowViewport(
                    from.X + (target.X - from.X) * e,
                    from.Y + (target.Y - from.Y) * e,
                    from.Zoom + (target.Zoom - from.Zoom) * e)));

                if (t >= 1) break;
            }
        }
        catch (OperationCanceledException) { return; }
        // The transition outlives a render, so the component can be torn down mid-flight.
        catch (ObjectDisposedException) { return; }
        finally
        {
            if (ReferenceEquals(_viewportAnimationCts, cts))
            {
                _viewportAnimationCts = null;
                cts.Dispose();
            }
        }

        await OnMoveEnd.InvokeAsync(_viewport);
    }

    /// <summary>
    /// Cubic ease-in-out: the transition starts and finishes gently, which is what makes a moved
    /// viewport read as the same view travelling rather than two views cutting.
    /// </summary>
    private static double EaseInOut(double t) =>
        t < 0.5 ? 4 * t * t * t : 1 - Math.Pow(-2 * t + 2, 3) / 2;

    /// <summary>Abandons any viewport transition in flight, leaving the viewport where it got to.</summary>
    private void StopViewportAnimation()
    {
        if (_viewportAnimationCts is not { } cts) return;
        _viewportAnimationCts = null;
        cts.Cancel();
        cts.Dispose();
    }

    private void ApplyViewport(BlowViewport viewport)
    {
        _viewport = viewport;
        _ = OnViewportChanged.InvokeAsync(_viewport);
        _ = OnMove.InvokeAsync(_viewport);
        RenderOverlay();
    }

    // ---- public imperative API (the useReactFlow() equivalent, accessed via @ref) ----

    /// <summary>The current viewport pan/zoom transform.</summary>
    public BlowViewport GetViewport() => _viewport;

    /// <summary>Converts a screen-space (client) point to flow coordinates.</summary>
    public BlowXYPosition ScreenToFlowPosition(double clientX, double clientY)
    {
        var px = clientX - (_paneRect?.Left ?? 0);
        var py = clientY - (_paneRect?.Top ?? 0);
        return _viewport.ScreenToFlow(px, py);
    }

    /// <summary>Converts a flow-space point to a screen-space (client) point.</summary>
    public BlowXYPosition FlowToScreenPosition(BlowXYPosition flow)
    {
        var p = _viewport.FlowToScreen(flow);
        return new BlowXYPosition(p.X + (_paneRect?.Left ?? 0), p.Y + (_paneRect?.Top ?? 0));
    }

    /// <summary>Sets the absolute zoom level, keeping the pane center stationary.</summary>
    public void ZoomTo(double zoom)
    {
        if (ComputeZoomTo(zoom) is not { } target) return;
        StopViewportAnimation();
        ApplyViewport(target);
    }

    /// <summary>Eases to the given zoom level over <paramref name="durationMs"/> milliseconds.</summary>
    public Task ZoomToAsync(double zoom, int durationMs)
        => ComputeZoomTo(zoom) is { } target ? AnimateViewportAsync(target, durationMs) : Task.CompletedTask;

    /// <summary>The viewport that zooming to <paramref name="zoom"/> produces, or null for a no-op.</summary>
    private BlowViewport? ComputeZoomTo(double zoom)
    {
        var newZoom = Math.Clamp(zoom, MinZoom, MaxZoom);
        if (Math.Abs(newZoom - _viewport.Zoom) < 1e-9) return null;

        var cx = (_paneRect?.Width ?? 0) / 2;
        var cy = (_paneRect?.Height ?? 0) / 2;
        var flowX = (cx - _viewport.X) / _viewport.Zoom;
        var flowY = (cy - _viewport.Y) / _viewport.Zoom;
        return ClampViewport(new BlowViewport(cx - flowX * newZoom, cy - flowY * newZoom, newZoom));
    }

    /// <summary>The current zoom factor.</summary>
    public double GetZoom() => _viewport.Zoom;

    /// <summary>Centers the viewport on the given flow point, optionally setting the zoom.</summary>
    public void SetCenter(double x, double y, double? zoom = null)
    {
        StopViewportAnimation();
        ApplyViewport(ComputeCenter(x, y, zoom));
    }

    /// <summary>Eases the viewport to centre on the given flow point.</summary>
    public Task SetCenterAsync(double x, double y, double? zoom, int durationMs)
        => AnimateViewportAsync(ComputeCenter(x, y, zoom), durationMs);

    private BlowViewport ComputeCenter(double x, double y, double? zoom)
    {
        var z = Math.Clamp(zoom ?? _viewport.Zoom, MinZoom, MaxZoom);
        var paneW = _paneRect?.Width ?? 0;
        var paneH = _paneRect?.Height ?? 0;
        return ClampViewport(new BlowViewport(paneW / 2 - x * z, paneH / 2 - y * z, z));
    }

    /// <summary>Pans/zooms so the given flow-space rectangle fills the pane.</summary>
    public void FitBounds(BlowRect bounds, double padding = 0.1)
    {
        if (ComputeFitBounds(bounds, padding) is not { } target) return;
        StopViewportAnimation();
        ApplyViewport(target);
    }

    /// <summary>Eases the viewport so the given flow-space rectangle fills the pane.</summary>
    public Task FitBoundsAsync(BlowRect bounds, double padding, int durationMs)
        => ComputeFitBounds(bounds, padding) is { } target
            ? AnimateViewportAsync(target, durationMs)
            : Task.CompletedTask;

    private BlowViewport? ComputeFitBounds(BlowRect bounds, double padding)
    {
        if (_paneRect is null) return null;
        return ClampViewport(BlowGraphUtils.GetViewportForBounds(
            bounds, _paneRect.Width, _paneRect.Height, MinZoom, MaxZoom, padding));
    }

    /// <summary>Returns the node with the given id, or null.</summary>
    public BlowNode? GetNode(string id) => Index.GetValueOrDefault(id);

    /// <summary>Returns the edge with the given id, or null.</summary>
    public BlowEdge? GetEdge(string id) => EdgeIndex.GetValueOrDefault(id);

    /// <summary>Ensures the id-&gt;edge lookup reflects the current edge list.</summary>
    private Dictionary<string, BlowEdge> EdgeIndex
    {
        get
        {
            if (!_edgeIndexDirty && _edgeIndex.Count == Edges.Count) return _edgeIndex;

            _edgeIndex.Clear();
            foreach (var e in Edges)
            {
                // Duplicate ids are tolerated the way duplicate node ids are: the first one
                // wins and the problem is reported rather than thrown.
                if (!_edgeIndex.TryAdd(e.Id, e))
                    ReportError(BlowErrorCode.DuplicateEdgeId, e.Id, $"More than one edge uses the id '{e.Id}'.");
            }
            _edgeIndexDirty = false;
            return _edgeIndex;
        }
    }

    /// <summary>
    /// Returns the connections attached to a node, optionally filtered to one handle type
    /// and/or handle id. Mirrors React Flow's <c>getNodeConnections</c>.
    /// </summary>
    public IReadOnlyList<BlowConnection> GetNodeConnections(
        string nodeId, BlowHandleType? type = null, string? handleId = null)
        => BlowGraphUtils.GetNodeConnections(nodeId, Edges, type, handleId);

    /// <summary>Returns the connections attached to one specific handle.</summary>
    public IReadOnlyList<BlowConnection> GetHandleConnections(string nodeId, BlowHandleType type, string? handleId)
        => BlowGraphUtils.GetHandleConnections(nodeId, type, handleId, Edges);

    /// <summary>Appends nodes and triggers a re-measure.</summary>
    public void AddNodes(params BlowNode[] nodes)
    {
        if (nodes.Length == 0) return;
        Nodes.AddRange(nodes);
        _needsMeasure = true;
        _indexDirty = true;
        // Deleting through the API is an undo step, so adding through it has to be one too:
        // otherwise undoing straight after an "add" would take back whatever came before it.
        Commit(BlowChangeKind.Added, "add", nodes);
        _ = RaiseNodesChangedAsync();
        StateHasChanged();
    }

    /// <summary>Appends edges.</summary>
    public void AddEdges(params BlowEdge[] edges)
    {
        if (edges.Length == 0) return;
        Edges.AddRange(edges);
        _edgeIndexDirty = true;
        Commit(BlowChangeKind.Added, "add", edges: edges);
        _ = RaiseEdgesChangedAsync();
        StateHasChanged();
    }

    /// <summary>Removes the given nodes (and their connected edges) and/or edges.</summary>
    public async Task DeleteElementsAsync(IEnumerable<BlowNode>? nodes = null, IEnumerable<BlowEdge>? edges = null)
    {
        var nodeList = WithDescendants(nodes?.Where(n => n.Deletable).ToList() ?? []);
        var edgeList = edges?.Where(e => e.Deletable).ToList() ?? [];
        if (nodeList.Count == 0 && edgeList.Count == 0) return;

        var nodeIds = nodeList.Select(n => n.Id).ToHashSet();
        var edgeIds = edgeList.Select(e => e.Id).ToHashSet();

        // Everything that actually disappears, including edges cascaded off deleted nodes.
        var removedEdges = Edges
            .Where(e => edgeIds.Contains(e.Id) || nodeIds.Contains(e.Source) || nodeIds.Contains(e.Target))
            .ToList();

        if (OnBeforeDelete is not null && !OnBeforeDelete(nodeList, removedEdges))
            return;

        if (nodeIds.Count > 0)
        {
            Nodes.RemoveAll(n => nodeIds.Contains(n.Id));
            Edges.RemoveAll(e => nodeIds.Contains(e.Source) || nodeIds.Contains(e.Target));
        }
        if (edgeIds.Count > 0)
            Edges.RemoveAll(e => edgeIds.Contains(e.Id));

        _indexDirty = true;
        _edgeIndexDirty = true;
        EndConnectionIfOrphaned();
        Commit(BlowChangeKind.Removed, "delete", removedNodes: nodeList, removedEdges: removedEdges);
        await NotifySelectionChangedAsync();
        await RaiseDeleteEventsAsync(nodeList, removedEdges);
        await RaiseNodesChangedAsync();
        await RaiseEdgesChangedAsync();
        StateHasChanged();
    }

    /// <summary>
    /// Lines the selected nodes up on one edge or axis - the align buttons of a drawing tool.
    /// Does nothing when fewer than two selected nodes can be moved.
    /// </summary>
    /// <remarks>
    /// Positions are parent-relative, so a selection spanning several parents is aligned within
    /// each parent's own space. Selecting siblings is the case this is meant for.
    /// </remarks>
    public async Task AlignSelectionAsync(BlowAlignEdge edge)
    {
        var moved = BlowAlign.Align(SelectedDraggableNodes(), edge, NodeOrigin);
        await AfterNodesMovedAsync(moved, "align");
    }

    /// <summary>Spaces the selected nodes evenly across the span they already occupy.</summary>
    /// <param name="horizontal">True to space them along X, false along Y.</param>
    /// <param name="gap">
    /// When given, the exact gap to leave between neighbours instead of filling the existing span.
    /// </param>
    public async Task DistributeSelectionAsync(bool horizontal, double? gap = null)
    {
        var moved = BlowAlign.Distribute(SelectedDraggableNodes(), horizontal, NodeOrigin, gap);
        await AfterNodesMovedAsync(moved, "distribute");
    }

    private List<BlowNode> SelectedDraggableNodes()
        => NodesDraggable ? Nodes.Where(n => n.Selected).ToList() : [];

    /// <summary>
    /// Common tail for the imperative move helpers: keep the moved nodes inside whatever
    /// constrains them, re-measure, announce, and tell the caller its list changed.
    /// </summary>
    private async Task AfterNodesMovedAsync(IReadOnlyList<BlowNode> moved, string label)
    {
        if (moved.Count == 0) return;

        foreach (var node in moved)
        {
            node.Position = ClampNodePosition(node, node.Position);
            ExpandParentFor(node);
        }

        _needsMeasure = true;
        Commit(BlowChangeKind.Moved, label, moved);
        Announce(AriaLabels.NodesMoved(moved.Count));
        await RaiseNodesChangedAsync();
        StateHasChanged();
    }

    // ---- connecting from the keyboard ----

    /// <summary>True while a connection started with <see cref="ConnectionKey"/> waits for the node it ends on.</summary>
    private bool _keyboardConnecting;

    /// <summary>Whether connections can be made from the keyboard at all.</summary>
    private bool KeyboardConnections => !DisableKeyboardA11y && NodesConnectable && !string.IsNullOrEmpty(ConnectionKey);

    /// <summary>The connection key as the instructions name it: a letter upper-cased, anything else as given.</summary>
    private string ConnectionKeyName => ConnectionKey is { Length: 1 } key ? key.ToUpperInvariant() : ConnectionKey ?? "";

    /// <summary>The keyboard instructions of a node, with how to connect from it when that can be done.</summary>
    private string NodeInstructionsText => KeyboardConnections
        ? $"{AriaLabels.NodeInstructions} {AriaLabels.KeyboardConnectionInstructions(ConnectionKeyName)}"
        : AriaLabels.NodeInstructions;

    /// <summary>
    /// A node's measured handles in the order the connection key steps through them: outputs first, then in reading
    /// order.
    /// </summary>
    private static List<BlowHandleBounds> PortsOf(BlowNode node) => node.Handles.Values
        .OrderBy(h => h.Type == BlowHandleType.Source ? 0 : 1)
        .ThenBy(h => h.OffsetY)
        .ThenBy(h => h.OffsetX)
        .ToList();

    /// <summary>A handle's id as edges record it: null for the unnamed handle.</summary>
    private static string? HandleIdOf(BlowHandleBounds handle) => string.IsNullOrEmpty(handle.HandleId) ? null : handle.HandleId;

    /// <summary>What a port is called when announced: its declared label, else its id; null for an unnamed handle.</summary>
    private static string? PortLabel(BlowNode node, BlowHandleBounds handle)
        => node.FindPort(handle.Type, HandleIdOf(handle))?.Label is { } label && !string.IsNullOrWhiteSpace(label)
            ? label
            : HandleIdOf(handle);

    private static int IndexOfPort(List<BlowHandleBounds> ports, string? handleId, BlowHandleType type)
        => ports.FindIndex(p => p.Type == type && HandleIdOf(p) == handleId);

    /// <summary>The connection key pressed on a focused node: start a connection from it, or pick another port.</summary>
    private async Task OnNodeConnectKey(BlowNode node)
    {
        if (!KeyboardConnections || !await ShouldHandleKeyAsync()) return;

        if (_keyboardConnecting)
        {
            if (node.Id != _connSource.NodeId)
                TargetKeyboardConnection(node, after: _connHover is { } hover && hover.nodeId == node.Id ? (hover.handleId, hover.type) : null);

            // On the node it started from the key steps on to the next port to start from - unless what is being
            // moved is one end of an edge, whose other end stays where it is.
            else if (_reconnectEdge is null) StartKeyboardConnection(node, after: (_connSource.HandleId, _connSource.Type));
            else ClearKeyboardTarget();
            return;
        }

        // A connection being drawn with the pointer is not the keyboard's to take over.
        if (_connecting) return;
        StartKeyboardConnection(node, after: null);
    }

    /// <summary>
    /// Starts a connection from the first port of <paramref name="node"/> one can start from - the first after
    /// <paramref name="after"/>, when stepping on to the next.
    /// </summary>
    private void StartKeyboardConnection(BlowNode node, (string? HandleId, BlowHandleType Type)? after)
    {
        var ports = PortsOf(node);
        var start = after is { } current ? IndexOfPort(ports, current.HandleId, current.Type) + 1 : 0;
        for (var k = 0; k < ports.Count; k++)
        {
            var port = ports[(start + k) % ports.Count];
            if (!CanStartConnection(node.Id, HandleIdOf(port), port.Type)) continue;

            // Stepping to another port ends the connection from the previous one, as releasing a drag would.
            if (_connecting) RaiseConnectionEnd(TakeConnectionEnd());

            StartConnection(node.Id, HandleIdOf(port), port.Type, port.Position);
            if (!_connecting) continue;

            _keyboardConnecting = true;
            Announce(AriaLabels.KeyboardConnectionStarted(NameOf(node), PortLabel(node, port), ConnectionKeyName));
            return;
        }

        if (!_keyboardConnecting) Announce(AriaLabels.KeyboardConnectionUnavailable(NameOf(node)));
    }

    /// <summary>
    /// Aims the keyboard connection at <paramref name="node"/>: at the first of its ports the connection could end on
    /// (after <paramref name="after"/> when stepping on, or from <paramref name="after"/> itself with
    /// <paramref name="stay"/>), or - when none could - at its first port, so the reason it cannot can be given.
    /// </summary>
    /// <remarks>
    /// A keyboard connection can wait as long as the user likes, so every aim is judged against the graph as it is
    /// now - nodes added, ports filled or freed, the moving edge's other end changed meanwhile - not as it was when the
    /// connection started.
    /// </remarks>
    private void TargetKeyboardConnection(BlowNode node, (string? HandleId, BlowHandleType Type)? after, bool stay = false)
    {
        if (!_keyboardConnecting) return;

        // The node the connection started from went (deleted, a collaborator's edit), or the edge whose end is being
        // moved did: there is nothing to connect.
        if (EndConnectionIfOrphaned()) return;

        RefreshConnectionSession();

        var ports = PortsOf(node)
            .Where(p => !(node.Id == _connSource.NodeId && p.Type == _connSource.Type && HandleIdOf(p) == _connSource.HandleId))
            .Where(p => ConnectionMode == BlowConnectionMode.Loose || p.Type != _connSource.Type)
            .ToList();

        if (ports.Count == 0)
        {
            ClearKeyboardTarget();
            Announce(AriaLabels.ConnectionRejected(BlowConnectionRejection.NotConnectable));
            return;
        }

        var at = after is { } current ? IndexOfPort(ports, current.HandleId, current.Type) : -1;
        var start = after is null ? 0 : Math.Max(0, at + (stay ? 0 : 1));
        var chosen = ports[start % ports.Count];
        var check = HoverCheck(node.Id, HandleIdOf(chosen), chosen.Type);

        // Staying on a port that is still there judges that port alone: one that would no longer take the connection
        // says why, rather than the aim moving on to a port the user never chose.
        var keep = stay && at >= 0;
        for (var k = 0; !keep && k < ports.Count && !check.IsValid; k++)
        {
            var port = ports[(start + k) % ports.Count];
            var verdict = HoverCheck(node.Id, HandleIdOf(port), port.Type);
            if (!verdict.IsValid) continue;
            (chosen, check) = (port, verdict);
        }

        var (x, y, _) = ResolveAnchor(node, chosen.Type, HandleIdOf(chosen), chosen.Position);
        SetKeyboardEnd(new BlowXYPosition(x, y));
        _connHover = (node.Id, HandleIdOf(chosen), chosen.Type);
        _keyboardAimValid = check.IsValid;
        NotifyConnectionChange();
        Announce(check.IsValid
            ? AriaLabels.KeyboardConnectionTarget(NameOf(node), PortLabel(node, chosen))
            : AriaLabels.ConnectionRejected(check.Rejection));
        StateHasChanged();
    }

    /// <summary>Whether the port the keyboard connection is aimed at would take it, as of the aim.</summary>
    private bool _keyboardAimValid;

    /// <summary>
    /// Ends the connection being made when what it was made from has gone - its source node, or the edge whose end
    /// is being moved - with the reason, as letting go of it there would. True when it ended.
    /// </summary>
    private bool EndConnectionIfOrphaned()
    {
        if (!_connecting) return false;
        var sourceGone = !Index.ContainsKey(_connSource.NodeId);
        var edgeGone = _reconnectEdge is { } moving && !ReferenceEquals(EdgeIndex.GetValueOrDefault(moving.Id), moving);
        if (!sourceGone && !edgeGone) return false;

        _connEndValid = false;
        _connRejection = BlowConnectionRejection.MissingNode;
        CancelConnection();
        return true;
    }

    /// <summary>
    /// Takes the end of <paramref name="edge"/> that stays where it is as the connection's source as it is now: the app
    /// or another reconnect may have moved it since it was picked up.
    /// </summary>
    private void FollowStayingEnd(BlowEdge edge)
    {
        var nodeId = _connSource.Type == BlowHandleType.Source ? edge.Source : edge.Target;
        var handleId = _connSource.Type == BlowHandleType.Source ? edge.SourceHandle : edge.TargetHandle;
        if (nodeId == _connSource.NodeId && handleId == _connSource.HandleId) return;
        if (!Index.TryGetValue(nodeId, out var node)) return;

        var fallback = _connSource.Type == BlowHandleType.Source ? node.SourcePosition : node.TargetPosition;
        var (x, y, pos) = ResolveAnchor(node, _connSource.Type, handleId, fallback);
        _connSource = new ConnectionSource(nodeId, handleId, _connSource.Type, pos, new BlowXYPosition(x, y));
    }

    /// <summary>What to announce when the connection being made from the keyboard is abandoned; null when there is none.</summary>
    private string? KeyboardCancelledText => !_keyboardConnecting ? null
        : _reconnectEdge is null ? AriaLabels.KeyboardConnectionCancelled
        : AriaLabels.KeyboardReconnectionCancelled;

    /// <summary>
    /// Ends a connection made from the keyboard because the pointer has started something else - a press on the pane,
    /// a node, an edge or a handle - and says so, as Escape would.
    /// </summary>
    private void AbandonKeyboardConnection()
    {
        if (KeyboardCancelledText is not { } cancelled) return;
        RaiseConnectionEnd(TakeConnectionEnd());
        Announce(cancelled);

        // The connection line is the canvas's own markup, and the handles follow the connection version: no node or
        // edge has anything else to draw.
        RenderOverlay();
    }

    /// <summary>Where the keyboard connection's line ends, on screen - reported by OnConnectEnd like a pointer's release point.</summary>
    private void SetKeyboardEnd(BlowXYPosition flow)
    {
        _connEnd = flow;
        var screen = FlowToScreenPosition(flow);
        _connEndClientX = screen.X;
        _connEndClientY = screen.Y;
    }

    /// <summary>Takes the keyboard connection's aim off any node: the line ends back at its source.</summary>
    private void ClearKeyboardTarget()
    {
        _connHover = null;
        SetKeyboardEnd(_connSource.Anchor);
        _keyboardAimValid = false;
        NotifyConnectionChange();
        StateHasChanged();
    }

    /// <summary>Enter on a node while connecting from the keyboard: connect to it when it can be, else say why not.</summary>
    private async Task CompleteKeyboardConnectionAsync(BlowNode node)
    {
        if (!await ShouldHandleKeyAsync()) return;

        // The connection may have ended while that was asked - a click on the pane, say.
        if (!_keyboardConnecting) return;

        // Aimed again, at the same port when it is there: the graph may have changed since the aim was taken - an
        // edit, a collaborator, an undo - and a port that would no longer take the connection says why and leaves it
        // waiting, as aiming at it would.
        (string nodeId, string? handleId, BlowHandleType type)? aim = _connHover is { } hover && hover.nodeId == node.Id ? hover : null;
        TargetKeyboardConnection(node, aim is { } aimed ? (aimed.handleId, aimed.type) : null, stay: true);

        // Enter on a node the line was not aimed at - the one it starts from, which focus does not aim at - only aims
        // it, and says where; the next Enter connects. A loop back onto that node is never made by one key press.
        if (aim is not { } target) return;

        // Connected only where it was aimed: a port that has gone since moved the aim, and the move was announced.
        if (_connHover != target || !_keyboardAimValid) return; // the reason was announced

        CompleteConnection(target.nodeId, target.handleId, target.type);
    }

    // ---- reconnecting from the keyboard ----

    /// <summary>Whether the ends of edges can be moved from the keyboard at all.</summary>
    private bool KeyboardReconnections => KeyboardConnections && EdgesReconnectable;

    /// <summary>The keyboard instructions of an edge, with how to move its ends when that can be done.</summary>
    private string EdgeInstructionsText => KeyboardReconnections
        ? $"{AriaLabels.EdgeInstructions} {AriaLabels.KeyboardReconnectionInstructions(ConnectionKeyName)}"
        : AriaLabels.EdgeInstructions;

    /// <summary>The name an edge is announced by, as its element is named: its label, else its two nodes'.</summary>
    private string NameOf(BlowEdge edge)
        => string.IsNullOrWhiteSpace(edge.Label) ? AriaLabels.EdgeLabel(NameOf(edge.Source), NameOf(edge.Target)) : edge.Label;

    /// <summary>
    /// The connection key pressed on a focused edge: picks up its target end to move it - or its source end, when the
    /// target end is the one already picked up. From there it is a connection from the end that stays: focus aims the
    /// moving end, Enter attaches it, Escape leaves the edge as it was.
    /// </summary>
    private async Task OnEdgeConnectKey(BlowEdge edge)
    {
        if (!KeyboardReconnections || !await ShouldHandleKeyAsync()) return;

        // A connection being drawn with the pointer is not the keyboard's to take over.
        if (_connecting && !_keyboardConnecting) return;

        var again = _keyboardConnecting && ReferenceEquals(_reconnectEdge, edge);
        var moving = again ? _connSource.Type : BlowHandleType.Target;

        if (!edge.Reconnectable || !Index.ContainsKey(edge.Source) || !Index.ContainsKey(edge.Target))
        {
            Announce(AriaLabels.KeyboardReconnectionUnavailable(NameOf(edge)));
            return;
        }

        // Whatever was being connected from the keyboard ends first, as releasing a drag would - the other end of this
        // edge included.
        if (_connecting) RaiseConnectionEnd(TakeConnectionEnd());

        var (source, target) = GetEdgeEndpoints(edge);
        var end = moving == BlowHandleType.Target ? target : source;
        var screen = FlowToScreenPosition(end);
        if (!BeginReconnect(edge, moving, end, screen.X, screen.Y, pointerId: null)) return;

        _keyboardConnecting = true;
        Announce(AriaLabels.KeyboardReconnectionStarted(
            NameOf(edge), NameOf(moving == BlowHandleType.Target ? edge.Target : edge.Source), ConnectionKeyName));
    }

    // ---- focus, selection and search ----

    /// <summary>
    /// Takes the user to a node: centres the view on it and moves keyboard focus to it, so a screen reader reads it
    /// out and the keys act on it - for a search result, a validation problem, a "go to" command.
    /// </summary>
    /// <param name="nodeId">The node.</param>
    /// <param name="select">True to make it the selection as well.</param>
    /// <param name="zoom">The zoom to show it at; null keeps the current one.</param>
    /// <param name="durationMs">How long the view takes to travel there; 0 jumps. With reduced motion it always jumps.</param>
    /// <returns>
    /// False when there is no such node, it is hidden, or its element could not take focus (nodes are not focusable).
    /// </returns>
    public async Task<bool> FocusNodeAsync(string nodeId, bool select = false, double? zoom = null, int durationMs = 0)
    {
        ArgumentNullException.ThrowIfNull(nodeId);
        if (!Index.TryGetValue(nodeId, out var node) || node.Hidden) return false;

        ComputeLayout();
        if (select) await SelectAsync([node]);

        var rect = node.GetRect();
        await SetCenterAsync(rect.X + rect.Width / 2, rect.Y + rect.Height / 2, zoom, durationMs);
        return await FocusElementAsync("node", nodeId);
    }

    /// <summary>Takes the user to an edge: centres the view between its ends and moves keyboard focus to it.</summary>
    /// <param name="edgeId">The edge.</param>
    /// <param name="select">True to make it the selection as well.</param>
    /// <param name="zoom">The zoom to show it at; null keeps the current one.</param>
    /// <param name="durationMs">How long the view takes to travel there; 0 jumps. With reduced motion it always jumps.</param>
    /// <returns>False when there is no such visible edge, or its element could not take focus.</returns>
    public async Task<bool> FocusEdgeAsync(string edgeId, bool select = false, double? zoom = null, int durationMs = 0)
    {
        ArgumentNullException.ThrowIfNull(edgeId);
        _ = Index; // brought up to date: IsEdgeVisible reads it directly
        if (!EdgeIndex.TryGetValue(edgeId, out var edge) || edge.Hidden || !IsEdgeVisible(edge)) return false;

        ComputeLayout();
        if (select) await SelectAsync(edges: [edge]);

        var (source, target) = GetEdgeEndpoints(edge);
        await SetCenterAsync((source.X + target.X) / 2, (source.Y + target.Y) / 2, zoom, durationMs);
        return await FocusElementAsync("edge", edgeId);
    }

    private async Task<bool> FocusElementAsync(string kind, string id)
    {
        // The element has to be in the page before it can take focus - culled while it was off screen, it comes back
        // in the render the move to it makes - so the render goes first.
        StateHasChanged();
        await Task.Yield();
        try
        {
            return await _interop.FocusElementAsync(_paneRef, kind, id);
        }
        catch (Exception ex) when (IsInteropTeardown(ex))
        {
            return false;
        }
    }

    /// <summary>
    /// Makes the given nodes and edges the selection - or adds them to it, with <paramref name="additive"/>. Elements
    /// that cannot be selected, or are hidden, are passed over.
    /// </summary>
    public async Task SelectAsync(IEnumerable<BlowNode>? nodes = null, IEnumerable<BlowEdge>? edges = null, bool additive = false)
    {
        if (!ElementsSelectable) return;
        if (!additive) ClearSelection();
        foreach (var node in nodes ?? []) if (node.Selectable && !node.Hidden) node.Selected = true;
        _ = Index;
        foreach (var edge in edges ?? []) if (edge.Selectable && !edge.Hidden && IsEdgeVisible(edge)) edge.Selected = true;
        await NotifySelectionChangedAsync();
        StateHasChanged();
    }

    /// <summary>
    /// The nodes matching a search, best matches first; see <see cref="BlowGraphSearch.FindNodes"/>.
    /// </summary>
    public IReadOnlyList<BlowNode> FindNodes(string query, Func<BlowNode, string?>? text = null)
        => BlowGraphSearch.FindNodes(Nodes.Where(n => !n.Hidden), query, text);

    /// <summary>The edges matching a search, best matches first; see <see cref="BlowGraphSearch.FindEdges"/>.</summary>
    public IReadOnlyList<BlowEdge> FindEdges(string query, Func<BlowEdge, string?>? text = null)
    {
        _ = Index; // brought up to date: IsEdgeVisible reads it directly
        return BlowGraphSearch.FindEdges(Edges.Where(e => !e.Hidden && IsEdgeVisible(e)), query, NameOf, text);
    }

    // ---- transactions, grouping and stacking ----

    // An edit is open from the start of EditAsync to its end, and the code running inside it - the edit itself and
    // everything it calls or awaits - carries the edit's token in an async-local. Asking both is what tells the edit's
    // own calls, which it folds in, from a key press or a server push handled while it awaits, which are edits of
    // their own; and from work it started without awaiting, which outlives it.
    private readonly AsyncLocal<object?> _editScope = new();
    private readonly HashSet<object> _openEdits = [];

    /// <summary>True for code running inside an open <see cref="EditAsync(Func{Task}, string?, BlowChangeOrigin, bool)"/>.</summary>
    private bool InEdit => _editScope.Value is { } token && _openEdits.Contains(token);

    /// <summary>Raises <see cref="NodesChanged"/>, unless inside an edit, which raises it once at its end.</summary>
    private Task RaiseNodesChangedAsync() => InEdit ? Task.CompletedTask : NodesChanged.InvokeAsync(Nodes);

    /// <summary>Raises <see cref="EdgesChanged"/>, unless inside an edit, which raises it once at its end.</summary>
    private Task RaiseEdgesChangedAsync() => InEdit ? Task.CompletedTask : EdgesChanged.InvokeAsync(Edges);

    /// <summary>
    /// Makes several changes to the graph as one edit: one undo step, one <see cref="OnGraphChanged"/>, one
    /// <see cref="NodesChanged"/> and <see cref="EdgesChanged"/>, and one render - however many nodes and edges
    /// <paramref name="edit"/> adds, removes or changes.
    /// </summary>
    /// <param name="edit">
    /// The changes: mutate <see cref="Nodes"/>, <see cref="Edges"/> and their elements directly, or call the canvas's
    /// own methods (<see cref="AddNodes"/>, <see cref="DeleteElementsAsync"/>, ...), whose steps and change events are
    /// folded into this edit's.
    /// </param>
    /// <param name="label">What the edit is called in the history and in its change; <c>edit</c> by default.</param>
    /// <param name="origin">
    /// Where the edit comes from. A collaboration adapter applying another user's edit passes
    /// <see cref="BlowChangeOrigin.Remote"/>, so its own change listener can tell what not to send back.
    /// </param>
    /// <param name="recordHistory">
    /// False makes the edit one the local user cannot undo - for remote edits. It is not recorded as a step; it is
    /// written into every step the history holds instead, so neither undoing nor redoing takes it back.
    /// </param>
    /// <remarks>
    /// <para>
    /// The change raised lists the nodes and edges the edit added and removed; changes to elements that stayed are
    /// not itemized, so a listener should treat an <see cref="BlowChangeKind.Edited"/> change as "the graph changed".
    /// The specific events of what happens inside - <see cref="OnSelectionChange"/>, <see cref="OnDelete"/>,
    /// <see cref="OnPaste"/> - are raised as it happens.
    /// </para>
    /// <para>
    /// When <paramref name="edit"/> throws, what it changed before throwing is committed as the edit all the same,
    /// and the exception is rethrown: the changes are already in the lists, and left unrecorded they would be folded
    /// into whatever step came next.
    /// </para>
    /// <para>
    /// Only the edit's own code is folded in. An edit the canvas handles while an asynchronous edit is waiting - a key
    /// press, a server push - is an edit of its own; keep what an edit awaits short, since an undo in that window
    /// changes the lists under it.
    /// </para>
    /// </remarks>
    public Task EditAsync(Action edit, string? label = null, BlowChangeOrigin origin = BlowChangeOrigin.Api, bool recordHistory = true)
    {
        ArgumentNullException.ThrowIfNull(edit);
        return EditAsync(() =>
        {
            edit();
            return Task.CompletedTask;
        }, label, origin, recordHistory);
    }

    /// <inheritdoc cref="EditAsync(Action, string?, BlowChangeOrigin, bool)"/>
    public async Task EditAsync(Func<Task> edit, string? label = null, BlowChangeOrigin origin = BlowChangeOrigin.Api, bool recordHistory = true)
    {
        ArgumentNullException.ThrowIfNull(edit);

        // An edit nested in another one is part of it: the outer edit commits for both.
        if (InEdit)
        {
            await edit();
            return;
        }

        var nodesBefore = new HashSet<BlowNode>(Nodes, ReferenceEqualityComparer.Instance);
        var edgesBefore = new HashSet<BlowEdge>(Edges, ReferenceEqualityComparer.Instance);

        var token = new object();
        _openEdits.Add(token);
        _editScope.Value = token;
        try
        {
            await edit();
        }
        finally
        {
            _openEdits.Remove(token);
            _editScope.Value = null;
            await CommitEditAsync(nodesBefore, edgesBefore, label ?? "edit", origin,
                recordHistory ? HistoryAction.Record : HistoryAction.Rebase);
        }
    }

    private async Task CommitEditAsync(HashSet<BlowNode> nodesBefore, HashSet<BlowEdge> edgesBefore, string label,
        BlowChangeOrigin origin, HistoryAction history)
    {
        var nodesAfter = new HashSet<BlowNode>(Nodes, ReferenceEqualityComparer.Instance);
        var edgesAfter = new HashSet<BlowEdge>(Edges, ReferenceEqualityComparer.Instance);

        _indexDirty = true;
        _edgeIndexDirty = true;
        _needsMeasure = true;

        Commit(BlowChangeKind.Edited, label,
            Nodes.Where(n => !nodesBefore.Contains(n)).ToList(),
            Edges.Where(e => !edgesBefore.Contains(e)).ToList(),
            nodesBefore.Where(n => !nodesAfter.Contains(n)).ToList(),
            edgesBefore.Where(e => !edgesAfter.Contains(e)).ToList(),
            origin, history);

        await NotifySelectionChangedAsync();
        await RaiseNodesChangedAsync();
        await RaiseEdgesChangedAsync();
        StateHasChanged();
    }

    /// <summary>
    /// Puts the selected nodes in a new group: a node of type <c>group</c>, sized to enclose them with
    /// <paramref name="padding"/> to spare, becomes their parent. Nothing moves on screen - each node's position is
    /// made relative to the group - and the group becomes the selection.
    /// </summary>
    /// <param name="group">
    /// The node to group them in, when the application wants its own (a custom type, an id of its choosing); its
    /// position, size and parent are set here. Null creates a <c>group</c> node with a fresh id.
    /// </param>
    /// <param name="padding">Space left between the grouped nodes and the group's border, in flow units.</param>
    /// <returns>
    /// The group, or null when nothing was grouped: no node is selected, or the selected nodes do not all sit in the
    /// same parent (grouping across parents would have to move nodes between coordinate spaces the user can see).
    /// </returns>
    public async Task<BlowNode?> GroupSelectionAsync(BlowNode? group = null, double padding = 20)
    {
        ComputeLayout();

        var selected = Nodes.Where(n => n.Selected && !n.Hidden).ToList();
        var selectedIds = selected.Select(n => n.Id).ToHashSet();

        // Only the outermost selected nodes: a selected child goes along with its selected parent.
        var members = selected.Where(n => !HasAncestorIn(n, selectedIds)).ToList();
        if (members.Count == 0 || members.Select(n => n.ParentId).Distinct().Count() != 1) return null;

        // A child's position is measured from its parent's position point - the point its own Position names, which
        // NodeOrigin can put anywhere in it - so that, not the top-left corner, is what everything is relative to.
        var parentId = members[0].ParentId;
        var parentPoint = parentId is not null && Index.TryGetValue(parentId, out var parent) ? PositionPoint(parent) : BlowXYPosition.Zero;
        var memberPoints = members.Select(PositionPoint).ToList();
        var bounds = BlowGraphUtils.GetNodesBounds(members);
        var topLeft = new BlowXYPosition(bounds.X - padding, bounds.Y - padding);

        group ??= new BlowNode { Id = UniqueNodeId("group"), Type = "group" };
        group.ParentId = parentId;
        group.Width = bounds.Width + 2 * padding;
        group.Height = bounds.Height + 2 * padding;
        var groupPoint = OriginPosition(group, topLeft);
        group.Position = groupPoint.Subtract(parentPoint);

        for (var i = 0; i < members.Count; i++)
        {
            members[i].Position = memberPoints[i].Subtract(groupPoint);
            members[i].ParentId = group.Id;
        }

        // Before its first member, so the list order - which the Basic and Manual z-index modes draw by - keeps the
        // group behind what it contains.
        Nodes.Insert(Nodes.IndexOf(members[0]), group);
        foreach (var node in Nodes) node.Selected = ReferenceEquals(node, group);

        await FinishEditAsync(BlowChangeKind.Edited, "group", [group, .. members]);
        return group;
    }

    /// <summary>
    /// Dissolves a group: its children move up to the group's own parent, staying where they are on screen, and the
    /// group node is removed - with any edges attached to it.
    /// </summary>
    /// <returns>The nodes that were in the group.</returns>
    /// <remarks>
    /// Removing the group is a deletion like any other: a group whose <see cref="BlowNode.Deletable"/> is off is not
    /// dissolved, <see cref="OnBeforeDelete"/> can veto it, and <see cref="OnNodesDelete"/> and
    /// <see cref="OnEdgesDelete"/> report it.
    /// </remarks>
    public async Task<IReadOnlyList<BlowNode>> UngroupAsync(BlowNode group)
    {
        ArgumentNullException.ThrowIfNull(group);
        if (!Nodes.Contains(group) || !group.Deletable) return [];

        ComputeLayout();

        var children = Nodes.Where(n => n.ParentId == group.Id).ToList();
        var removedEdges = Edges.Where(e => e.Source == group.Id || e.Target == group.Id).ToList();
        if (OnBeforeDelete is not null && !OnBeforeDelete([group], removedEdges)) return [];

        var newParentPoint = group.ParentId is { } pid && Index.TryGetValue(pid, out var newParent)
            ? PositionPoint(newParent)
            : BlowXYPosition.Zero;
        var childPoints = children.Select(PositionPoint).ToList();

        for (var i = 0; i < children.Count; i++)
        {
            children[i].Position = childPoints[i].Subtract(newParentPoint);
            children[i].ParentId = group.ParentId;
        }

        Edges.RemoveAll(e => e.Source == group.Id || e.Target == group.Id);
        Nodes.Remove(group);
        _indexDirty = true;
        _edgeIndexDirty = true;
        EndConnectionIfOrphaned();

        await FinishEditAsync(BlowChangeKind.Edited, "ungroup", children, removedNodes: [group], removedEdges: removedEdges);
        await RaiseDeleteEventsAsync([group], removedEdges);
        return children;
    }

    /// <summary>
    /// Draws the given nodes - the selection when none are given - above the others, by moving them, with everything
    /// nested in them, to the end of <see cref="Nodes"/>. Nodes keep their order among themselves. An explicit
    /// <see cref="BlowNode.ZIndex"/> still wins, and in <see cref="BlowZIndexMode.Auto"/> a child is still drawn above
    /// its parent.
    /// </summary>
    public Task BringToFrontAsync(IEnumerable<BlowNode>? nodes = null) => ReorderAsync(nodes, toFront: true);

    /// <summary>Draws the given nodes - the selection when none are given - below the others; see <see cref="BringToFrontAsync"/>.</summary>
    public Task SendToBackAsync(IEnumerable<BlowNode>? nodes = null) => ReorderAsync(nodes, toFront: false);

    private async Task ReorderAsync(IEnumerable<BlowNode>? nodes, bool toFront)
    {
        var picked = (nodes ?? Nodes.Where(n => n.Selected)).ToHashSet(ReferenceEqualityComparer.Instance);
        if (picked.Count == 0) return;

        // A node moves with everything nested in it, so a parent never ends up after (in front of) its own children.
        var ids = picked.Cast<BlowNode>().Select(n => n.Id).ToHashSet();
        var block = Nodes.Where(n => picked.Contains(n) || HasAncestorIn(n, ids)).ToList();
        var inBlock = block.ToHashSet(ReferenceEqualityComparer.Instance);
        var rest = Nodes.Where(n => !inBlock.Contains(n)).ToList();

        if (toFront)
        {
            rest.AddRange(block);
        }
        else
        {
            // To the back of its own parent, not of the whole list: before its parent it would be drawn beneath it. Each
            // moved subtree goes right after its parent - or to the very start, for a root - taken last to first so
            // the moved nodes keep their order among themselves.
            var roots = block.Where(n => !HasAncestorIn(n, ids)).ToList();
            for (var r = roots.Count - 1; r >= 0; r--)
            {
                var root = roots[r];
                var subtree = block.Where(n => ReferenceEquals(n, root) || HasAncestorIn(n, [root.Id])).ToList();
                var parentAt = root.ParentId is { } pid ? rest.FindIndex(n => n.Id == pid) : -1;
                rest.InsertRange(parentAt + 1, subtree);
            }
        }

        Nodes.Clear();
        Nodes.AddRange(rest);
        await FinishEditAsync(BlowChangeKind.Edited, "order", block);
    }

    /// <summary>
    /// Arranges nodes with a layout - the top level of the graph, or a set of nodes such as the selection - as one undo
    /// step.
    /// </summary>
    /// <param name="layout">
    /// The layout: a built-in one's options (<see cref="BlowTreeLayoutOptions"/>, <see cref="BlowLayeredLayoutOptions"/>,
    /// ...) or an <see cref="IBlowLayout"/> of your own. A built-in layout is told the canvas's <see cref="NodeOrigin"/>.
    /// </param>
    /// <param name="nodes">
    /// The nodes to arrange. Null takes the selection when something is selected, else every top-level node. Only the
    /// nodes sharing the first one's parent are arranged - positions in different parents are not comparable - and
    /// hidden nodes are left where they are. The layout sees the edges among them - an edge into a node's children
    /// counting as an edge into the node, so a group is placed by what its contents connect to.
    /// </param>
    /// <param name="keepPlace">
    /// True moves the arrangement so its top-left corner stays where the nodes' top-left corner was, so laying out a
    /// selection tidies it where it is instead of moving it to the layout's origin.
    /// </param>
    /// <returns>The nodes that were arranged.</returns>
    public async Task<IReadOnlyList<BlowNode>> LayoutAsync(IBlowLayout layout, IEnumerable<BlowNode>? nodes = null, bool keepPlace = true)
    {
        ArgumentNullException.ThrowIfNull(layout);
        ComputeLayout();

        var chosen = (nodes ?? (Nodes.Exists(n => n.Selected && !n.Hidden) ? Nodes.Where(n => n.Selected) : Nodes.Where(n => n.ParentId is null)))
            .Where(n => !n.Hidden)
            .ToList();
        if (chosen.Count == 0) return [];

        var parentId = chosen[0].ParentId;
        var set = chosen.Where(n => n.ParentId == parentId).Distinct().ToList();
        var ids = set.Select(n => n.Id).ToHashSet();
        var edges = new List<BlowEdge>();
        var lifted = new HashSet<(string, string)>();
        foreach (var edge in Edges)
        {
            if (MemberOf(edge.Source) is not { } source || MemberOf(edge.Target) is not { } target || source == target) continue;
            if (source == edge.Source && target == edge.Target) edges.Add(edge);
            else if (lifted.Add((source, target))) edges.Add(new BlowEdge { Id = edge.Id, Source = source, Target = target });
        }

        var before = TopLeftOf(set);
        if (layout is BlowLayoutOptions options)
        {
            // The canvas's origin for this run only: the options object is the caller's.
            var callersOrigin = options.NodeOrigin;
            options.NodeOrigin = NodeOrigin;
            try { layout.Arrange(set, edges); }
            finally { options.NodeOrigin = callersOrigin; }
        }
        else
        {
            layout.Arrange(set, edges);
        }

        // Pinned nodes hold the arrangement where it is: moving it would move them.
        var pinned = layout is BlowForceLayoutOptions { IsPinned: { } isPinned } && set.Exists(n => isPinned(n));
        if (keepPlace && !pinned)
        {
            var after = TopLeftOf(set);
            var shift = before.Subtract(after);
            foreach (var node in set) node.Position = new BlowXYPosition(node.Position.X + shift.X, node.Position.Y + shift.Y);
        }

        await FinishEditAsync(BlowChangeKind.Moved, "layout", set);
        return set;

        // The node being laid out that a node is, or is nested in; null for one outside the set.
        string? MemberOf(string id)
        {
            string? current = id;
            for (var guard = 0; guard <= MaxParentDepth && current is not null; guard++)
            {
                if (ids.Contains(current)) return current;
                current = Index.TryGetValue(current, out var node) ? node.ParentId : null;
            }
            return null;
        }

        // The top-left corner of the nodes' boxes together, in their parent's space.
        BlowXYPosition TopLeftOf(List<BlowNode> group)
        {
            double left = double.MaxValue, top = double.MaxValue;
            foreach (var node in group)
            {
                var origin = node.Origin ?? NodeOrigin;
                var size = node.EffectiveSize;
                left = Math.Min(left, node.Position.X - origin.X * size.Width);
                top = Math.Min(top, node.Position.Y - origin.Y * size.Height);
            }
            return new BlowXYPosition(left, top);
        }
    }

    /// <summary>
    /// Collapses or expands a node with children. Collapsing hides everything nested in it and deselects it; edges that
    /// ran into it from outside are drawn to the node itself, and edges among its children are hidden with them.
    /// Expanding shows the children again - all but those inside a nested node that is itself still collapsed. One
    /// undo step either way, and announced.
    /// </summary>
    /// <param name="node">The node.</param>
    /// <param name="collapsed">True to collapse, false to expand, null to toggle.</param>
    /// <remarks>
    /// The children's <see cref="BlowNode.Hidden"/> is what collapsing sets, so a saved graph reopens collapsed; and
    /// expanding shows a child that was hidden for another reason as well.
    /// </remarks>
    public async Task SetCollapsedAsync(BlowNode node, bool? collapsed = null)
    {
        ArgumentNullException.ThrowIfNull(node);
        if (!Nodes.Contains(node)) return;

        var collapse = collapsed ?? !node.Collapsed;
        node.Collapsed = collapse;

        var changed = new List<BlowNode> { node };
        var descendants = WithDescendants([node]).Where(d => !ReferenceEquals(d, node)).ToList();
        var count = 0;
        foreach (var descendant in descendants)
        {
            // A node that is itself out of sight - inside a collapsed ancestor, or hidden - keeps its children out of
            // sight when it is expanded: they come back with it.
            var hide = collapse || node.Hidden || InsideCollapsed(descendant, node);
            if (hide) descendant.Selected = false;
            if (descendant.Hidden == hide) continue;
            descendant.Hidden = hide;
            changed.Add(descendant);
            count++;
        }

        // An edge folded away with the children goes out of the selection with them, or the delete key would remove
        // what nobody can see.
        _ = Index;
        foreach (var edge in Edges)
        {
            if (edge.Selected && !IsEdgeVisible(edge)) edge.Selected = false;
        }

        Announce(collapse ? AriaLabels.NodeCollapsed(NameOf(node), count) : AriaLabels.NodeExpanded(NameOf(node), count));
        await FinishEditAsync(BlowChangeKind.Edited, collapse ? "collapse" : "expand", changed);

        // True when a node between this descendant and the node being expanded is collapsed in its own right.
        bool InsideCollapsed(BlowNode descendant, BlowNode top)
        {
            var current = descendant;
            for (var guard = 0; guard < MaxParentDepth && current.ParentId is { } pid && pid != top.Id
                 && Index.TryGetValue(pid, out var parent); guard++)
            {
                if (parent.Collapsed) return true;
                current = parent;
            }
            return false;
        }
    }

    /// <summary>The common tail of the editing commands: commit, tell, render.</summary>
    private async Task FinishEditAsync(BlowChangeKind kind, string label, IReadOnlyList<BlowNode> nodes,
        IReadOnlyList<BlowNode>? removedNodes = null, IReadOnlyList<BlowEdge>? removedEdges = null)
    {
        _indexDirty = true;
        _edgeIndexDirty = true;
        _needsMeasure = true;
        Commit(kind, label, nodes, removedNodes: removedNodes, removedEdges: removedEdges);
        await NotifySelectionChangedAsync();
        await RaiseNodesChangedAsync();
        if (removedEdges is { Count: > 0 }) await RaiseEdgesChangedAsync();
        StateHasChanged();
    }

    /// <summary>
    /// Where a node's position point sits in flow space: the point its <see cref="BlowNode.Position"/> names, and the
    /// one its children's positions are measured from. Valid after <see cref="ComputeLayout"/>.
    /// </summary>
    private BlowXYPosition PositionPoint(BlowNode node) => ResolveRaw(node, 0).Position;

    /// <summary>
    /// Where a node's position point would sit, in flow space, with its top-left corner at <paramref name="topLeft"/> -
    /// the position names the node's origin point, which is the top-left only by default.
    /// </summary>
    private BlowXYPosition OriginPosition(BlowNode node, BlowXYPosition topLeft)
    {
        var origin = node.Origin ?? NodeOrigin;
        var size = node.EffectiveSize;
        return new BlowXYPosition(topLeft.X + origin.X * size.Width, topLeft.Y + origin.Y * size.Height);
    }

    /// <summary>A node id starting with <paramref name="prefix"/> that no node uses yet.</summary>
    private string UniqueNodeId(string prefix)
    {
        var index = Index;
        for (var i = 1; ; i++)
        {
            var id = $"{prefix}-{i}";
            if (!index.ContainsKey(id)) return id;
        }
    }

    /// <summary>Returns the nodes overlapping the given flow-space rectangle.</summary>
    public IReadOnlyList<BlowNode> GetIntersectingNodes(BlowRect area, bool partially = true)
    {
        ComputeLayout();
        var list = new List<BlowNode>();
        foreach (var n in Nodes)
        {
            if (n.Hidden) continue;
            if (NodeIntersects(n.GetRect(), area, partially)) list.Add(n);
        }
        return list;
    }

    /// <summary>Returns the nodes overlapping the given node's bounds (excluding itself).</summary>
    public IReadOnlyList<BlowNode> GetIntersectingNodes(BlowNode node, bool partially = true)
    {
        // Brought up to date first: asked from OnNodeDrag, the dragged node has just been moved and the absolute
        // positions the rectangles are read from are still the last render's - every answer one move behind.
        ComputeLayout();
        var area = node.GetRect();
        var list = new List<BlowNode>();
        foreach (var n in Nodes)
        {
            if (n.Hidden || ReferenceEquals(n, node)) continue;
            if (NodeIntersects(n.GetRect(), area, partially)) list.Add(n);
        }
        return list;
    }

    /// <summary>True when the node overlaps (or is fully within) the given area.</summary>
    public bool IsNodeIntersecting(BlowNode node, BlowRect area, bool partially = true)
    {
        ComputeLayout();
        return NodeIntersects(node.GetRect(), area, partially);
    }

    private static bool NodeIntersects(BlowRect node, BlowRect area, bool partially)
        => partially
            ? RectsIntersect(node, area)
            : node.Left >= area.Left && node.Right <= area.Right
              && node.Top >= area.Top && node.Bottom <= area.Bottom;

    /// <summary>
    /// Returns a snapshot of the current nodes, edges and viewport. By default the nodes and
    /// edges are cloned, so the snapshot keeps describing the flow as it was even as the user
    /// carries on editing. Pass <c>deep: false</c> to get the live instances instead.
    /// </summary>
    public BlowSnapshot ToObject(bool deep = true) => deep
        ? new BlowSnapshot(Nodes.Select(n => n.Clone()).ToList(), Edges.Select(e => e.Clone()).ToList(), _viewport)
        : new BlowSnapshot(Nodes.ToList(), Edges.ToList(), _viewport);

    /// <summary>
    /// The graph and the viewport as a document, ready for <see cref="BlowSerializer"/>. Holds the live nodes and
    /// edges, not copies: serialize it straight away.
    /// </summary>
    public BlowDocument ToDocument() => new() { Nodes = [.. Nodes], Edges = [.. Edges], Viewport = _viewport };

    /// <summary>Saves the graph and the viewport as JSON. See <see cref="BlowSerializer"/>.</summary>
    /// <exception cref="BlowSerializationException">A node or edge carries data no mapping says how to save.</exception>
    public string ToJson(BlowSerializerOptions? options = null) => BlowSerializer.Serialize(ToDocument(), options);

    /// <summary>
    /// Reads a saved document and, when it was read without errors, replaces the graph with it - in place, as
    /// <see cref="RestoreAsync"/> does - and records the load as an undo step when the built-in history is on.
    /// </summary>
    /// <param name="json">The document, as <see cref="ToJson"/> or <see cref="BlowSerializer"/> wrote it.</param>
    /// <param name="options">Data mappings, limits and migrations; the same options it was saved with.</param>
    /// <param name="restoreViewport">Whether to adopt the document's viewport, when it has one.</param>
    /// <returns>
    /// What reading found. When <see cref="BlowImportResult.Succeeded"/> is false the graph on screen is left as it
    /// was, and the issues say what was wrong and where.
    /// </returns>
    public async Task<BlowImportResult> LoadJsonAsync(string json, BlowSerializerOptions? options = null, bool restoreViewport = true)
    {
        var result = BlowSerializer.Deserialize(json, options);
        if (!result.Succeeded || result.Document is not { } document) return result;

        await RestoreCoreAsync(document.ToSnapshot(), restoreViewport && document.Viewport is not null, detach: false,
            BlowChangeOrigin.Api, "load");
        CaptureHistory("load");
        return result;
    }

    /// <summary>
    /// The graph as it is on screen, as a standalone SVG image - for documentation, a report, printing; see
    /// <see cref="BlowSvgExport"/>. Measured handles, the canvas's <see cref="NodeOrigin"/> and its
    /// <see cref="Routing"/> are used, so edges attach and route as they are drawn here.
    /// </summary>
    public string ToSvg(BlowSvgExportOptions? options = null)
    {
        ComputeLayout();
        var o = options ?? new BlowSvgExportOptions();
        if (options is null && AriaLabel is { Length: > 0 } name) o.Title = name;

        // The canvas's origin and routing for this export only: the options object is the caller's.
        var (callersOrigin, callersRouting) = (o.NodeOrigin, o.Routing);
        o.NodeOrigin = NodeOrigin;
        o.Routing ??= Routing;
        try { return BlowSvgExport.ToSvg(Nodes, Edges, o); }
        finally { (o.NodeOrigin, o.Routing) = (callersOrigin, callersRouting); }
    }

    /// <summary>Re-renders the canvas, optionally re-measuring node sizes and handle anchors first.</summary>
    /// <param name="remeasure">True to re-read node geometry from the DOM before rendering.</param>
    public void Refresh(bool remeasure)
    {
        if (remeasure)
        {
            _needsMeasure = true;
            _remeasureAll = true;
        }
        _indexDirty = true;
        _edgeIndexDirty = true;
        StateHasChanged();
    }

    /// <summary>
    /// Re-measures node sizes and handle anchors. Call it after changing a node's handles -
    /// adding one, moving one to another side - since nothing about that is visible to C#.
    /// Mirrors React Flow's <c>useUpdateNodeInternals</c>.
    /// </summary>
    /// <remarks>
    /// The named nodes are read afresh whatever the browser last reported of them, and brought across even when
    /// nothing changed; with none named, every node is.
    /// </remarks>
    public void UpdateNodeInternals(params string[] nodeIds)
    {
        if (nodeIds is not { Length: > 0 })
        {
            Refresh(remeasure: true);
            return;
        }

        foreach (var id in nodeIds) _remeasure.Add(id);
        _needsMeasure = true;
        Refresh(remeasure: false);
    }

    /// <summary>
    /// Replaces the current graph and viewport with a saved snapshot, in place - the bound
    /// <see cref="Nodes"/> and <see cref="Edges"/> lists keep their identity, so a caller
    /// holding a reference to either still sees the restored graph.
    /// </summary>
    /// <param name="snapshot">The snapshot to apply, typically from <see cref="ToObject"/>.</param>
    /// <param name="restoreViewport">
    /// Whether to adopt the snapshot's pan/zoom as well as its graph. Pass false to drop the
    /// restored graph into the view the user is already looking at.
    /// </param>
    /// <param name="detach">
    /// Whether to put copies of the snapshot's nodes and edges into the graph rather than the
    /// instances the snapshot holds. True by default, and worth leaving that way: nodes are
    /// mutable, so handing the snapshot's own instances to the live graph lets the user's next
    /// drag edit the snapshot too - which is how restoring the same history step twice ends up
    /// restoring two different graphs. Pass false only when you mean to hand over the instances.
    /// </param>
    public Task RestoreAsync(BlowSnapshot snapshot, bool restoreViewport = true, bool detach = true)
        => RestoreCoreAsync(snapshot, restoreViewport, detach, BlowChangeOrigin.Api, "restore");

    private async Task RestoreCoreAsync(BlowSnapshot snapshot, bool restoreViewport, bool detach, BlowChangeOrigin origin, string label)
    {
        var removedNodes = Nodes.ToList();
        var removedEdges = Edges.ToList();
        var source = detach ? snapshot.Detach() : snapshot;

        Nodes.Clear();
        Nodes.AddRange(source.Nodes);
        Edges.Clear();
        Edges.AddRange(source.Edges);

        _indexDirty = true;
        _edgeIndexDirty = true;
        _needsMeasure = true;

        // The restored graph has nothing to do with what was selected a moment ago, and the
        // cached geometry describes edges that may no longer exist.
        _lastSelection.Clear();
        _edgeContexts.Clear();

        if (restoreViewport) SetViewport(snapshot.Viewport);

        // Restoring is not an undo step of its own (an app records one when it means to), so only the change is told.
        Commit(BlowChangeKind.Replaced, label, Nodes.ToList(), Edges.ToList(), removedNodes, removedEdges, origin, HistoryAction.Skip);
        await NotifySelectionChangedAsync();
        await RaiseNodesChangedAsync();
        await RaiseEdgesChangedAsync();
        StateHasChanged();
    }

    /// <summary>Captures a pointer on an element so a drag started there survives leaving it.</summary>
    public void CapturePointer(ElementReference element, long pointerId)
        => _ = _interop.SetPointerCaptureAsync(element, pointerId);

    /// <summary>Registers content to render inside the transformed viewport layer. Used by <c>BlowViewportPortal</c>.</summary>
    public void RegisterViewportPortal(object key, RenderFragment fragment)
    {
        // Only force a re-render when the portal is newly added; updating an existing
        // key's fragment on every cascade render would cause an infinite render loop.
        var isNew = !_viewportPortals.ContainsKey(key);
        _viewportPortals[key] = fragment;
        if (isNew) StateHasChanged();
    }

    /// <summary>Removes previously registered viewport-portal content.</summary>
    public void UnregisterViewportPortal(object key)
    {
        if (_viewportPortals.Remove(key))
            StateHasChanged();
    }

    /// <summary>Registers a handle's validation predicate and connect callback. Used by <c>BlowHandle</c>.</summary>
    public void RegisterHandle(string key, BlowHandleRegistration registration) => _handles[key] = registration;

    /// <summary>Removes a previously registered handle.</summary>
    public void UnregisterHandle(string key) => _handles.Remove(key);

    /// <summary>Removes a handle's registration only while it is still the one held for the key.</summary>
    public void UnregisterHandle(string key, BlowHandleRegistration registration)
    {
        if (_handles.TryGetValue(key, out var current) && ReferenceEquals(current, registration))
            _handles.Remove(key);
    }

    /// <summary>
    /// The per-handle hooks for one handle, or null when that handle registered none. Handles
    /// only register when they actually carry a predicate or a callback, so this is null for
    /// the overwhelming majority of them.
    /// </summary>
    private BlowHandleRegistration? HandleFor(string nodeId, BlowHandleType type, string? handleId)
        => _handles.GetValueOrDefault(BlowHandleRegistration.Key(nodeId, type, handleId));

    /// <summary>Begins a node resize gesture. Called by <c>BlowNodeResizer</c> and <c>BlowNodeResizeControl</c>.</summary>
    public void StartResize(string nodeId, BlowResizeDirection direction,
        double clientX, double clientY, double minWidth, double minHeight, bool keepAspectRatio)
        => StartResize(nodeId, direction, clientX, clientY, minWidth, minHeight, keepAspectRatio,
            double.MaxValue, double.MaxValue, NoPointer);

    /// <summary>
    /// Begins a node resize gesture with size bounds, capturing the pointer on the pane so the
    /// gesture survives the pointer leaving the flow.
    /// </summary>
    public void StartResize(string nodeId, BlowResizeDirection direction,
        double clientX, double clientY, double minWidth, double minHeight, bool keepAspectRatio,
        double maxWidth, double maxHeight, long pointerId)
    {
        OnPointerPress();
        if (!Index.TryGetValue(nodeId, out var node)) return;

        _resizing = true;
        _resizeNode = node;
        _resizeDir = direction;
        _resizeStartClientX = clientX;
        _resizeStartClientY = clientY;
        _resizeStartPos = node.Position;
        _resizeStartSize = node.EffectiveSize;
        _resizeOriginalSize = (node.Width, node.Height);
        _resizeMinW = Math.Max(1, minWidth);
        _resizeMinH = Math.Max(1, minHeight);
        _resizeMaxW = Math.Max(_resizeMinW, maxWidth);
        _resizeMaxH = Math.Max(_resizeMinH, maxHeight);

        // A node pinned inside its parent must not be resizable straight out of it. Folding the
        // extent into the size bounds here means every later step - the clamp, the aspect-ratio
        // pass - respects it without having to know about extents at all. Which bound applies
        // depends on the side being dragged: the anchored edge stays put, so only the distance
        // from it to the extent is available.
        if (ResolveExtent(node) is { } extent)
        {
            var startSize = _resizeStartSize;
            if (direction.HasFlag(BlowResizeDirection.Right))
                _resizeMaxW = Math.Min(_resizeMaxW, Math.Max(_resizeMinW, extent.Right - _resizeStartPos.X));
            if (direction.HasFlag(BlowResizeDirection.Left))
                _resizeMaxW = Math.Min(_resizeMaxW, Math.Max(_resizeMinW, _resizeStartPos.X + startSize.Width - extent.Left));
            if (direction.HasFlag(BlowResizeDirection.Bottom))
                _resizeMaxH = Math.Min(_resizeMaxH, Math.Max(_resizeMinH, extent.Bottom - _resizeStartPos.Y));
            if (direction.HasFlag(BlowResizeDirection.Top))
                _resizeMaxH = Math.Min(_resizeMaxH, Math.Max(_resizeMinH, _resizeStartPos.Y + startSize.Height - extent.Top));
        }
        _resizeKeepAspect = keepAspectRatio;
        node.Resizing = true;
        CapturePane(pointerId);
        _ = OnNodeResizeStart.InvokeAsync(node);
        StateHasChanged();
    }

    private void ApplyResize(PointerEventArgs e)
    {
        var node = _resizeNode!;
        var dx = (e.ClientX - _resizeStartClientX) / _viewport.Zoom;
        var dy = (e.ClientY - _resizeStartClientY) / _viewport.Zoom;

        var left = _resizeDir.HasFlag(BlowResizeDirection.Left);
        var top = _resizeDir.HasFlag(BlowResizeDirection.Top);

        double w = _resizeStartSize.Width;
        double h = _resizeStartSize.Height;

        if (_resizeDir.HasFlag(BlowResizeDirection.Right)) w = _resizeStartSize.Width + dx;
        if (_resizeDir.HasFlag(BlowResizeDirection.Bottom)) h = _resizeStartSize.Height + dy;
        if (left) w = _resizeStartSize.Width - dx;
        if (top) h = _resizeStartSize.Height - dy;

        if (SnapToGrid)
        {
            w = Math.Round(w / SnapStepX) * SnapStepX;
            h = Math.Round(h / SnapStepY) * SnapStepY;
        }

        w = Math.Clamp(w, _resizeMinW, _resizeMaxW);
        h = Math.Clamp(h, _resizeMinH, _resizeMaxH);

        if (_resizeKeepAspect)
        {
            var aspect = _resizeStartSize.Width / Math.Max(1, _resizeStartSize.Height);
            // Let the axis the user is actually dragging drive the other one, then re-clamp
            // both so the aspect ratio never pushes a side past its min/max.
            var drivenByWidth = _resizeDir is BlowResizeDirection.Left or BlowResizeDirection.Right
                || Math.Abs(w - _resizeStartSize.Width) >= Math.Abs(h - _resizeStartSize.Height);

            if (drivenByWidth) h = w / aspect;
            else w = h * aspect;

            w = Math.Clamp(w, _resizeMinW, _resizeMaxW);
            h = Math.Clamp(h, _resizeMinH, _resizeMaxH);
            if (drivenByWidth) w = Math.Clamp(h * aspect, _resizeMinW, _resizeMaxW);
            else h = Math.Clamp(w / aspect, _resizeMinH, _resizeMaxH);
        }

        // Keep the anchored (non-dragged) edge fixed: only left/top drags move the origin,
        // and they move it by exactly the amount the size changed.
        var x = left ? _resizeStartPos.X + (_resizeStartSize.Width - w) : _resizeStartPos.X;
        var y = top ? _resizeStartPos.Y + (_resizeStartSize.Height - h) : _resizeStartPos.Y;

        node.Width = w;
        node.Height = h;
        node.Position = ClampNodePosition(node, new BlowXYPosition(x, y));
        _ = OnNodeResize.InvokeAsync(node);
    }

    /// <summary>Pans and zooms so the whole graph fits the pane, with a 10% margin.</summary>
    public void FitView() => FitView(0.1);

    /// <summary>Pans and zooms so the whole graph fits the pane.</summary>
    /// <param name="padding">Fraction of the pane left empty around the graph, 0..1.</param>
    public void FitView(double padding) => FitView(new BlowFitViewOptions { Padding = padding });

    /// <summary>
    /// Pans/zooms so the requested nodes fill the pane. Mirrors React Flow's <c>fitView</c>,
    /// including its options for padding, hidden nodes, a zoom range and a node subset.
    /// </summary>
    public void FitView(BlowFitViewOptions options)
    {
        if (ComputeFitView(options) is not { } target) return;
        StopViewportAnimation();
        ApplyViewport(target);
    }

    /// <summary>
    /// Fits the view, easing there over <see cref="BlowFitViewOptions.Duration"/> milliseconds
    /// instead of jumping. Mirrors React Flow's <c>fitView({ duration })</c>.
    /// </summary>
    public Task FitViewAsync(BlowFitViewOptions options)
        => ComputeFitView(options) is { } target
            ? AnimateViewportAsync(target, options.Duration)
            : Task.CompletedTask;

    /// <summary>Fits the whole graph, easing there over <paramref name="durationMs"/> milliseconds.</summary>
    public Task FitViewAsync(int durationMs)
        => FitViewAsync(new BlowFitViewOptions { Duration = durationMs });

    private BlowViewport? ComputeFitView(BlowFitViewOptions options)
    {
        if (_paneRect is null) return null;

        ComputeLayout();

        IEnumerable<BlowNode> source = options.Nodes ?? Nodes;
        var candidates = (options.IncludeHiddenNodes ? source : source.Where(n => !n.Hidden)).ToList();
        if (candidates.Count == 0) return null;

        // The options can only narrow the canvas's own zoom range, never widen it.
        var minZoom = Math.Max(MinZoom, options.MinZoom ?? MinZoom);
        var maxZoom = Math.Min(MaxZoom, options.MaxZoom ?? MaxZoom);

        return ClampViewport(BlowGraphUtils.GetViewportForBounds(
            BlowGraphUtils.GetNodesBounds(candidates), _paneRect.Width, _paneRect.Height,
            minZoom, maxZoom, options.Padding));
    }

    // ---- utils ----

    private BlowXYPosition ScreenToFlow(PointerEventArgs e)
    {
        var px = e.ClientX - (_paneRect?.Left ?? 0);
        var py = e.ClientY - (_paneRect?.Top ?? 0);
        return _viewport.ScreenToFlow(px, py);
    }

    private double SnapStepX => SnapGridX <= 0 ? 1 : SnapGridX;
    private double SnapStepY => SnapGridY <= 0 ? 1 : SnapGridY;

    /// <summary>Flow units an arrow key moves the selection when no grid is in play.</summary>
    private const double FineStep = 5;

    /// <summary>Flow units a Shift+arrow moves the selection.</summary>
    private const double CoarseStep = 25;

    /// <summary>Snaps a flow-space point to the configured grid when snapping is enabled.</summary>
    private BlowXYPosition Snap(BlowXYPosition p)
    {
        if (!SnapToGrid) return p;
        return new BlowXYPosition(SnapAxis(p.X, SnapStepX), SnapAxis(p.Y, SnapStepY));
    }

    private static double SnapAxis(double value, double step) => Math.Round(value / step) * step;

    /// <summary>
    /// Clamps a node's (relative) position against, in order of precedence: its parent's bounds
    /// when <c>ExtentParent</c> is set, its own <c>Extent</c>, or the canvas-wide
    /// <see cref="NodeExtent"/> (root nodes only, since a child's coordinates are parent-relative).
    /// </summary>
    private BlowXYPosition ClampNodePosition(BlowNode node, BlowXYPosition pos)
    {
        var size = node.EffectiveSize;
        return ResolveExtent(node) is { } extent ? ClampToRect(extent, pos, size) : pos;
    }

    /// <summary>
    /// The rectangle a node may not leave, in the node's own coordinate space (parent-relative
    /// when it has a parent), or null when it is free to go anywhere. In order of precedence:
    /// its parent's bounds when <c>ExtentParent</c> is set, then its own <c>Extent</c>, then the
    /// canvas-wide <see cref="NodeExtent"/> - which applies to root nodes only, since a child's
    /// coordinates are not in the same space.
    /// </summary>
    private BlowRect? ResolveExtent(BlowNode node)
    {
        if (node.ExtentParent && node.ParentId is { } pid && Index.TryGetValue(pid, out var parent))
        {
            var ps = parent.EffectiveSize;
            return new BlowRect(0, 0, ps.Width, ps.Height);
        }

        if (node.Extent is { } own) return own;

        if (NodeExtent is { } ext && node.ParentId is null) return ext;

        return null;
    }

    private static BlowXYPosition ClampToRect(BlowRect extent, BlowXYPosition pos, BlowDimensions size)
    {
        var maxX = Math.Max(extent.Left, extent.Right - size.Width);
        var maxY = Math.Max(extent.Top, extent.Bottom - size.Height);
        return new BlowXYPosition(Math.Clamp(pos.X, extent.Left, maxX), Math.Clamp(pos.Y, extent.Top, maxY));
    }

    /// <summary>
    /// Grows a parent so it keeps enclosing a child that was dragged past its edge. Moving the
    /// parent's origin also shifts every sibling the other way, so their absolute positions
    /// stay put - only the parent's box changes.
    /// </summary>
    private void ExpandParentFor(BlowNode child)
    {
        if (!child.ExpandParent || child.ExtentParent) return;
        if (child.ParentId is not { } pid || !Index.TryGetValue(pid, out var parent)) return;

        var size = child.EffectiveSize;
        var parentSize = parent.EffectiveSize;

        var shiftX = Math.Max(0, -child.Position.X);
        var shiftY = Math.Max(0, -child.Position.Y);
        var growX = Math.Max(0, child.Position.X + size.Width - parentSize.Width);
        var growY = Math.Max(0, child.Position.Y + size.Height - parentSize.Height);

        if (shiftX == 0 && shiftY == 0 && growX == 0 && growY == 0) return;

        if (shiftX > 0 || shiftY > 0)
        {
            parent.Position = new BlowXYPosition(parent.Position.X - shiftX, parent.Position.Y - shiftY);
            foreach (var sibling in Nodes)
            {
                if (sibling.ParentId != pid) continue;
                sibling.Position = new BlowXYPosition(sibling.Position.X + shiftX, sibling.Position.Y + shiftY);
            }

            // Moving the parent's origin has just renumbered every coordinate inside it. A node
            // being dragged carries its pre-drag position in the old numbering, and each pointer
            // move re-derives where it should be from that - so leaving it stale would undo this
            // shift on the very next move and then apply a fresh one, and the parent would run
            // away from the pointer instead of following it.
            for (var i = 0; i < _dragSet.Count; i++)
            {
                var (node, start) = _dragSet[i];
                if (node.ParentId != pid) continue;
                _dragSet[i] = (node, new BlowXYPosition(start.X + shiftX, start.Y + shiftY));
            }
        }

        parent.Width = parentSize.Width + shiftX + growX;
        parent.Height = parentSize.Height + shiftY + growY;
        _needsMeasure = true;
    }

    /// <summary>Clamps the viewport so the visible flow region stays within <see cref="TranslateExtent"/>.</summary>
    private BlowViewport ClampViewport(BlowViewport vp)
    {
        if (TranslateExtent is not { } ext || _paneRect is null) return vp;

        var paneW = _paneRect.Width;
        var paneH = _paneRect.Height;
        var minX = paneW - ext.Right * vp.Zoom;
        var maxX = -ext.Left * vp.Zoom;
        var minY = paneH - ext.Bottom * vp.Zoom;
        var maxY = -ext.Top * vp.Zoom;

        var x = minX <= maxX ? Math.Clamp(vp.X, minX, maxX) : (minX + maxX) / 2;
        var y = minY <= maxY ? Math.Clamp(vp.Y, minY, maxY) : (minY + maxY) / 2;
        return vp with { X = x, Y = y };
    }

    private static string Inv(double v) => v.ToString(System.Globalization.CultureInfo.InvariantCulture);

    // The reconnect anchors live inside the transformed viewport layer, so their radii are in
    // flow units; dividing by the zoom keeps both a constant size on screen.
    private double ReconnectDotRadius => 4 / _viewport.Zoom;

    private double ReconnectHitRadius => Math.Max(ReconnectDotRadius, ReconnectRadius / _viewport.Zoom);

    private string AttributionPositionClass => BlowPanelPositionClass.For(AttributionPosition);

    private string ColorModeClass => ColorMode switch
    {
        BlowColorMode.Dark => "bit-blow-dark",
        BlowColorMode.System => "bit-blow-system",
        _ => string.Empty,
    };

    // The stylesheet follows prefers-reduced-motion on its own; these two say when the app has overruled it.
    private string? MotionClass => ReducedMotion switch
    {
        BlowReducedMotion.Always => "bit-blow-reduce-motion",
        BlowReducedMotion.Never => "bit-blow-allow-motion",
        _ => null,
    };

    /// <summary>
    /// The transform of the zoomed layer, plus the zoom as a custom property: what lets the stylesheet keep hit
    /// areas, outlines and the like a constant size on screen without the canvas re-rendering every node and edge
    /// at each step of a zoom to tell them.
    /// </summary>
    private string ViewportStyle => string.Create(System.Globalization.CultureInfo.InvariantCulture,
        $"transform:{_viewport.ToTransform()};--bit-blow-zoom:{_viewport.Zoom};");

    private static BlowPosition ParsePosition(string? s) => s switch
    {
        "top" => BlowPosition.Top,
        "right" => BlowPosition.Right,
        "bottom" => BlowPosition.Bottom,
        "left" => BlowPosition.Left,
        _ => BlowPosition.Bottom,
    };

    private static BlowPosition OppositePosition(BlowPosition p) => p switch
    {
        BlowPosition.Top => BlowPosition.Bottom,
        BlowPosition.Bottom => BlowPosition.Top,
        BlowPosition.Left => BlowPosition.Right,
        _ => BlowPosition.Left,
    };

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        _disposed = true;
        StopAutoPan();
        StopWheelGesture();
        StopViewportAnimation();

        await DisposeJsHandleAsync(_resizeObserver);
        _resizeObserver = null;

        await DisposeJsHandleAsync(_interactionGuards);
        _interactionGuards = null;

        _selfRef?.Dispose();
        _selfRef = null;

        if (_interop is not null)
            await _interop.DisposeAsync();

        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Tears down one of the JS-side handles (the resize observer, the interaction guards).
    /// Both expose the same <c>dispose()</c> shape, and both may already be gone if the circuit
    /// dropped before the component did.
    /// </summary>
    private static async ValueTask DisposeJsHandleAsync(IJSObjectReference? handle)
    {
        if (handle is null) return;
        try
        {
            await handle.InvokeVoidAsync("dispose");
        }
        catch (Exception ex) when (IsInteropTeardown(ex) || ex is JSException) { }
        finally
        {
            // Released even when dispose() failed, or the reference itself would leak.
            try { await handle.DisposeAsync(); }
            catch (Exception ex) when (IsInteropTeardown(ex) || ex is JSException) { }
        }
    }
}
