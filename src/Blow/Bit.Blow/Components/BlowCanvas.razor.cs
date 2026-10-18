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
    /// act on - a plain scroll while <see cref="ZoomActivationKey"/> is not held - always
    /// reaches the page.
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

    /// <summary>How many undo steps the built-in history keeps. Ignored when <see cref="HistoryEnabled"/> is false.</summary>
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

    // ---- state ----

    private ElementReference _paneRef;
    private ElementReference _viewportRef;
    private readonly string _markerPrefix = $"bit-blow-arrow-{Guid.NewGuid():N}";

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

    /// <summary>Depth guard for parent chains; also breaks accidental parent cycles.</summary>
    private const int MaxParentDepth = 50;

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

    /// <summary>Set while a released drag is being resolved, so the end event can say it was a drop.</summary>
    private bool _connDropping;
    private BlowEdge? _reconnectEdge;
    private bool _reconnectRewired;
    private readonly HashSet<string> _existingConnectionKeys = [];

    // resize
    private bool _resizing;
    private BlowNode? _resizeNode;
    private BlowResizeDirection _resizeDir;
    private double _resizeStartClientX, _resizeStartClientY;
    private BlowXYPosition _resizeStartPos;
    private BlowDimensions _resizeStartSize;
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
        }

        await InstallInteractionGuardsAsync();

        if (_needsMeasure)
        {
            _needsMeasure = false;
            var changed = await MeasureAsync();
            if (_initialFitPending && TryInitialFit()) changed = true;
            if (changed)
                StateHasChanged();
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
        var policy = new BlowInputPolicy(PreventScrolling, PanOnScroll, ZoomActivationKey switch
        {
            BlowModifierKey.Shift => "shiftKey",
            BlowModifierKey.Control => "ctrlKey",
            BlowModifierKey.Alt => "altKey",
            BlowModifierKey.Meta => "metaKey",
            _ => null,
        }, PanActivationKey, !DisableKeyboardA11y);
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
        if (paneResized || nodesChanged) StateHasChanged();
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

    private async Task<bool> MeasureAsync()
    {
        BlowMeasuredNode[] measured;
        try { measured = await _interop.MeasureNodesAsync(new BlowElementReferenceLike(_viewportRef), _viewport.Zoom); }
        catch (Exception ex) when (IsInteropTeardown(ex)) { return false; }

        bool changed = false;
        var byId = Index;
        foreach (var m in measured)
        {
            if (m.Id is null || !byId.TryGetValue(m.Id, out var node)) continue;

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
        ComputeLayout();

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
            _markerDefs.TryAdd(spec.Key, spec);
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
            if (_index.TryGetValue(pid, out var parent) && !ReferenceEquals(parent, n))
            {
                var (pp, pd) = ResolveRaw(parent, guard + 1);
                pos = new BlowXYPosition(pp.X + n.Position.X, pp.Y + n.Position.Y);
                depth = pd + 1;
            }
            else if (!_index.ContainsKey(pid))
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

    private BlowEdgeContext GetEdgeContext(BlowEdge edge)
    {
        // The edge layer, the reconnect anchors and the HTML label overlay all ask for the same
        // edge within one render, and across renders the answer usually has not changed either:
        // panning and zooming move the whole viewport layer without touching flow-space geometry.
        // Keeping the cache and checking what the path actually depends on turns a pan of a large
        // graph from "rebuild every path string" into a dictionary lookup - and hands the diff
        // back the identical strings, so nothing is written to the DOM.
        var key = EdgeGeometryKey(edge);
        if (_edgeContexts.TryGetValue(edge.Id, out var cached)
            && ReferenceEquals(cached.Edge, edge)
            && cached.GeometryKey == key)
        {
            return cached;
        }

        var ctx = BuildEdgeContext(edge, key);
        _edgeContexts[edge.Id] = ctx;
        return ctx;
    }

    /// <summary>
    /// Everything the computed path and markers depend on. Two renders that agree on this
    /// produce byte-identical geometry, so the cached context can be handed back untouched.
    /// </summary>
    private BlowEdgeGeometryKey EdgeGeometryKey(BlowEdge edge)
    {
        var index = Index;
        index.TryGetValue(edge.Source, out var source);
        index.TryGetValue(edge.Target, out var target);

        var (sx, sy, sPos) = source is null
            ? (0d, 0d, BlowPosition.Bottom)
            : ResolveAnchor(source, BlowHandleType.Source, edge.SourceHandle, source.SourcePosition);
        var (tx, ty, tPos) = target is null
            ? (0d, 0d, BlowPosition.Top)
            : ResolveAnchor(target, BlowHandleType.Target, edge.TargetHandle, target.TargetPosition);

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
            edge.MarkerStart, edge.MarkerEnd, edge.ResolveMarker(DefaultMarkerColor));
    }

    private BlowEdgeContext BuildEdgeContext(BlowEdge edge, BlowEdgeGeometryKey key)
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

        var (sx, sy, sPos) = ResolveAnchor(source, BlowHandleType.Source, edge.SourceHandle, source.SourcePosition);
        var (tx, ty, tPos) = ResolveAnchor(target, BlowHandleType.Target, edge.TargetHandle, target.TargetPosition);

        // An edge that comes back to the node it left has no meaningful "between the two nodes"
        // for an ordinary path algorithm to route through, so it is looped instead.
        var geometry = IsSelfLoop(edge, source, target)
            ? BlowEdgePath.Loop(sx, sy, sPos, tx, ty, tPos,
                edge.PathOptions?.LoopRadius ?? BlowEdgePath.DefaultLoopRadius, source.GetRect())
            : BlowEdgePath.Compute(edge.Type, sx, sy, sPos, tx, ty, tPos, edge.PathOptions);

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
        var index = _index;
        if (index.TryGetValue(edge.Source, out var source) && source.Hidden) return false;
        if (index.TryGetValue(edge.Target, out var target) && target.Hidden) return false;
        return true;
    }

    private string? MarkerUrl(BlowEdge edge, bool show)
    {
        if (!show) return null;

        var spec = edge.ResolveMarker(DefaultMarkerColor);

        // An edge culled out of the render list (or reached before PrepareRender) still needs a
        // usable url, so register on demand rather than assuming the collection pass saw it.
        _markerDefs.TryAdd(spec.Key, spec);

        return $"url(#{_markerPrefix}-{spec.Key})";
    }

    private (double x, double y, BlowPosition pos) ResolveAnchor(
        BlowNode node, BlowHandleType type, string? handleId, BlowPosition fallback)
    {
        var key = BlowHandleBounds.Key(type, handleId);
        if (node.Handles.TryGetValue(key, out var h))
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
    private bool ShowConnectionLine => _connecting && (_connMoved || ConnectOnClick);

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
        return new BlowConnectionLineContext
        {
            FromNode = s.NodeId,
            FromHandle = s.HandleId,
            FromHandleType = s.Type,
            FromPosition = s.Position,
            From = s.Anchor,
            To = _connEnd,
            ToPosition = OppositePosition(s.Position),
            IsValid = _connSnap is not null
                      || (_connHover is { } h && IsValidConnectionTarget(h.nodeId, h.handleId, h.type)),
        };
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
        // say), the stale flag is cleared here rather than eating the next real click.
        _suppressNextClick = false;

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

        // A pending click-connection is cancelled by clicking empty space.
        if (_connecting)
        {
            CancelConnection();
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

        if (ElementsSelectable)
        {
            ClearSelection();
            await NotifySelectionChangedAsync();
        }

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
    /// The rectangles a dragged node can line up against: everything visible that is not itself
    /// being dragged. Nodes moving together must not attract each other.
    /// </summary>
    private IEnumerable<BlowRect> StationaryRects()
    {
        foreach (var n in Nodes)
        {
            if (n.Hidden || n.Dragging) continue;
            yield return n.GetRect();
        }
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
        _connSnap = OnConnectionChange.HasDelegate || ConnectionLineTemplate is not null
            ? ClosestValidHandle()
            : null;

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
    }

    private async Task RunAutoPanAsync(CancellationToken token)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromMilliseconds(16));
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

                    var (vx, vy) = _autoPanVector;
                    _viewport = ClampViewport(_viewport with { X = _viewport.X + vx, Y = _viewport.Y + vy });

                    // The pointer hasn't moved, but the world under it has: re-derive whatever
                    // the active gesture is tracking so the node/connection follows the pan.
                    var (px, py) = _autoPanPointer;
                    if (_draggingNodes) ApplyNodeDrag(px, py);
                    else if (_connecting) UpdateConnectionEnd(px, py);

                    _ = OnMove.InvokeAsync(_viewport);
                    StateHasChanged();
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
        // viewport, so the pinch simply carries on the same move rather than starting another.
        var wasPanning = _panning;
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
        StateHasChanged();
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
    private bool IsGesturing =>
        _panning || _draggingNodes || _connecting || _selecting || _resizing || _pinching
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

    private void OnPanePointerMove(PointerEventArgs e)
    {
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
            StateHasChanged();
        }
        else if (_draggingNodes)
        {
            ApplyNodeDrag(e.ClientX, e.ClientY);
            UpdateAutoPan(e.ClientX, e.ClientY);
            StateHasChanged();
        }
        else if (_connecting)
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
            StateHasChanged();
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
            StateHasChanged();
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
            _draggingNodes = false;
            var dragged = _dragPrimary;
            var moved = _dragArmed;
            var wasSelectionDrag = IsSelectionDrag;
            var draggedSet = wasSelectionDrag && OnSelectionDragStop.HasDelegate ? DraggedNodes() : null;
            _dragPrimary = null;
            _guides = [];
            foreach (var (node, _) in _dragSet) node.Dragging = false;
            _dragSet.Clear();
            _needsMeasure = true; // node moved: re-measure handle anchors
            if (dragged is not null)
                await OnNodeDragStop.InvokeAsync(dragged);
            if (draggedSet is not null)
                await OnSelectionDragStop.InvokeAsync(new BlowSelectionMouseEvent(draggedSet, e));
            if (moved)
            {
                CaptureHistory();
                await NodesChanged.InvokeAsync(Nodes);
            }
        }

        if (_connecting)
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
            var completed = await TryCompleteByHitTestAsync(e.ClientX, e.ClientY)
                            || (_connMoved && TryCompleteByProximity());
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
            _resizeNode = null;
            _needsMeasure = true; // size changed: re-measure handle anchors
            CaptureHistory();
            await NodesChanged.InvokeAsync(Nodes);
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
        CancelActiveGesture();
        StateHasChanged();
    }

    /// <summary>
    /// Abandons whatever gesture is running without committing it, putting back anything it had
    /// already moved. Shared by <c>pointercancel</c> (the browser taking the pointer over) and by
    /// Escape, which is how a user says the same thing.
    /// </summary>
    /// <returns>True when there was something to abandon.</returns>
    private bool CancelActiveGesture()
    {
        var wasActive = _pinching || _panning || _selecting || _connecting || _draggingNodes || _resizing;

        _pinching = false;
        StopAutoPan();

        if (_connecting) CancelConnection();

        _panning = false;

        if (_selecting)
        {
            // Put the selection back to what the box found it as: the box has been repainting it
            // on every pointer move, and abandoning the gesture has to abandon that too.
            _selecting = false;
            foreach (var n in Nodes) n.Selected = _preSelected.Contains(NodeKey(n.Id));
            foreach (var ed in Edges) ed.Selected = _preSelected.Contains(EdgeKey(ed.Id));
            _ = NotifySelectionChangedAsync();
        }
        _preSelected.Clear();

        if (_draggingNodes)
        {
            // Restore the pre-drag positions: a cancelled drag must not leave nodes half-moved.
            foreach (var (node, start) in _dragSet)
            {
                node.Position = start;
                node.Dragging = false;
            }
            _dragSet.Clear();
            _draggingNodes = false;
            _dragPrimary = null;
            _guides = [];
        }

        if (_resizing)
        {
            if (_resizeNode is { } rn)
            {
                rn.Resizing = false;
                rn.Position = _resizeStartPos;
                rn.Width = _resizeStartSize.Width;
                rn.Height = _resizeStartSize.Height;
            }
            _resizing = false;
            _resizeNode = null;
        }

        ReleasePane();
        return wasActive;
    }

    /// <summary>
    /// Resolves a connection drop by asking the DOM which handle sits under the release point.
    /// This makes drops work even when <see cref="ConnectionRadius"/> is 0 and the pointer was
    /// captured by the pane (so the handle never saw the pointer-up itself).
    /// </summary>
    private async Task<bool> TryCompleteByHitTestAsync(double clientX, double clientY)
    {
        BlowHandleHit? hit;
        try
        {
            hit = await _interop.ElementDataAtPointAsync(_paneRef, clientX, clientY, ".bit-blow-handle");
        }
        catch (Exception ex) when (IsInteropTeardown(ex))
        {
            return false;
        }

        if (hit?.NodeId is not { Length: > 0 } nodeId) return false;

        // A handle that refuses drops is painted as rejected; releasing on it must not connect.
        if (hit.ConnectableEnd == false) return false;

        var type = hit.HandleType == "target" ? BlowHandleType.Target : BlowHandleType.Source;
        var handleId = string.IsNullOrEmpty(hit.HandleId) ? null : hit.HandleId;
        if (!IsValidConnectionTarget(nodeId, handleId, type)) return false;

        CompleteConnection(nodeId, handleId, type);
        return true;
    }

    private async Task OnWheel(WheelEventArgs e)
    {
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
        StateHasChanged();
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

    private async Task OnKeyDown(KeyboardEventArgs e)
    {
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
            if (CancelActiveGesture())
            {
                StateHasChanged();
                return;
            }

            if (ElementsSelectable && await ShouldHandleKeyAsync())
            {
                ClearSelection();
                await NotifySelectionChangedAsync();
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

        switch (history)
        {
            case HistoryCommand.Undo: await UndoAsync(); return;
            case HistoryCommand.Redo: await RedoAsync(); return;
        }

        switch (clipboard)
        {
            case ClipboardCommand.Copy: Copy(); return;
            case ClipboardCommand.Cut: await CutAsync(); return;
            case ClipboardCommand.Paste: await PasteAsync(); return;
            case ClipboardCommand.Duplicate: await DuplicateSelectionAsync(); return;
        }

        if (isSelectAll) await SelectAllAsync();
        else if (isDelete) await DeleteSelectionAsync();
        else await MoveSelectedNodesAsync(e);
    }

    private enum ClipboardCommand { None, Copy, Cut, Paste, Duplicate }

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

        var moved = 0;
        BlowNode? last = null;
        foreach (var n in Nodes)
        {
            // The same test a pointer drag applies, hidden included: a node nobody can see is not
            // one an arrow key should be quietly shifting around.
            if (!n.Selected || !n.Draggable || n.Hidden || !NodesDraggable) continue;

            // Snapping after the step is what keeps a node that started off-grid from staying
            // off it forever: stepping by the cell size alone preserves the original offset.
            var next = Snap(new BlowXYPosition(n.Position.X + dx, n.Position.Y + dy));
            n.Position = ClampNodePosition(n, next);

            // A node nudged by the keyboard ends up in the same place as one dragged there, so
            // its container has to grow for it in the same way.
            ExpandParentFor(n);

            moved++;
            last = n;
        }

        if (moved > 0)
        {
            _needsMeasure = true;
            CaptureHistory();
            Announce(moved == 1 && last is not null
                ? AriaLabels.NodeMoved(NameOf(last), last.Position)
                : AriaLabels.NodesMoved(moved));
            await NodesChanged.InvokeAsync(Nodes);
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

    private Task OnPaneClickHandler(MouseEventArgs e)
        => ConsumeSuppressedClick() ? Task.CompletedTask : OnPaneClick.InvokeAsync(e);

    private Task OnPaneContextMenuHandler(MouseEventArgs e) => OnPaneContextMenu.InvokeAsync(e);

    private Task OnNodeClickHandler(BlowNode node)
        => ConsumeSuppressedClick() ? Task.CompletedTask : OnNodeClick.InvokeAsync(node);

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
        _suppressNextClick = false;
        SelectOnPointerDown(node, e);

        // When the node uses a custom drag handle, the body selects but doesn't drag.
        if (node.UseCustomDragHandle) return;

        BeginDrag(node, e);
    }

    /// <summary>Starts dragging a node, bypassing the custom-drag-handle restriction. Called by <c>BlowDragHandle</c>.</summary>
    public void BeginNodeDrag(BlowNode node, PointerEventArgs e)
    {
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
        foreach (var n in Nodes) n.Selected = false;
        foreach (var ed in Edges) ed.Selected = false;
    }

    /// <summary>
    /// Keyboard activation of a focused node (Enter/Space). <paramref name="additive"/> is set
    /// when Shift is held, matching the Shift+click behaviour.
    /// </summary>
    private async Task ActivateNode(BlowNode node, bool additive)
    {
        if (!ElementsSelectable || !node.Selectable) return;
        if (!additive) ClearSelection();
        node.Selected = !additive || !node.Selected;
        await NotifySelectionChangedAsync();
        Announce(node.Selected
            ? AriaLabels.NodeSelected(NameOf(node))
            : AriaLabels.NodeDeselected(NameOf(node)));
        StateHasChanged();
    }

    /// <summary>The name a node is announced by: its explicit aria label, then its label, then its id.</summary>
    private static string NameOf(BlowNode node) => node.AriaLabel ?? node.Label ?? node.Id;

    /// <summary>
    /// Only wire up node focus when something is actually going to be done with it - an empty
    /// callback makes the renderer omit the attribute for every node in the graph.
    /// </summary>
    private EventCallback<FocusEventArgs> NodeFocusHandler(BlowNode node) => AutoPanOnNodeFocus
        ? EventCallback.Factory.Create<FocusEventArgs>(this, () => BringNodeIntoView(node))
        : default;

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
        // viewport belongs to that gesture.
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
        if (!additive) ClearSelection();
        edge.Selected = !additive || !edge.Selected;
        await NotifySelectionChangedAsync();
        Announce(edge.Selected
            ? AriaLabels.EdgeSelected(edge.Source, edge.Target)
            : AriaLabels.EdgeDeselected(edge.Source, edge.Target));
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

        _liveMessage = text;
        StateHasChanged();
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
    public void CaptureHistory()
    {
        if (_history is null || _restoring) return;
        if (_history.Capture(ToObject())) NotifyHistoryChanged();
    }

    /// <summary>Goes back one step. Returns false when there was nothing to go back to.</summary>
    public async Task<bool> UndoAsync()
    {
        if (_history?.Undo() is not { } snapshot) return false;
        await ApplyHistoryAsync(snapshot);
        return true;
    }

    /// <summary>Goes forward one step. Returns false when nothing had been undone.</summary>
    public async Task<bool> RedoAsync()
    {
        if (_history?.Redo() is not { } snapshot) return false;
        await ApplyHistoryAsync(snapshot);
        return true;
    }

    private async Task ApplyHistoryAsync(BlowSnapshot snapshot)
    {
        _restoring = true;
        try
        {
            // The graph only, not the viewport: undoing an edit should not also throw away where
            // the user had scrolled to since making it.
            await RestoreAsync(snapshot, restoreViewport: false);
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
            _history.CanUndo, _history.CanRedo, _history.UndoDepth, _history.RedoDepth));
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

        if (_history is not null && _history.Capacity == HistoryCapacity) return;

        _history = new BlowHistory(Math.Max(1, HistoryCapacity));
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
    public async Task<BlowFragment> CutAsync()
    {
        var copied = Copy();
        await DeleteSelectionAsync();
        return copied;
    }

    /// <summary>
    /// Adds a fresh copy of the flow's clipboard to the graph, selected and offset so it is
    /// visible. Returns what was added, or an empty fragment when there was nothing to paste.
    /// </summary>
    public Task<BlowFragment> PasteAsync() => PasteAsync(_clipboard);

    /// <summary>Adds a fresh copy of the given fragment to the graph.</summary>
    public async Task<BlowFragment> PasteAsync(BlowFragment fragment, BlowXYPosition? offset = null)
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

        CaptureHistory();
        await NotifySelectionChangedAsync();
        await OnPaste.InvokeAsync(pasted);
        await NodesChanged.InvokeAsync(Nodes);
        await EdgesChanged.InvokeAsync(Edges);
        StateHasChanged();

        return pasted;
    }

    /// <summary>
    /// Copies the selection and pastes it in one step, leaving the clipboard untouched - the
    /// Ctrl+D that every editor has, and which should not cost you what you had copied.
    /// </summary>
    public async Task<BlowFragment> DuplicateSelectionAsync()
    {
        var selected = Nodes.Where(n => n.Selected).ToList();
        if (selected.Count == 0) return BlowFragment.Empty;

        var fragment = BlowClipboard.Copy(selected, Edges, NodeOrigin);
        return await PasteAsync(fragment);
    }

    /// <summary>Selects every selectable, visible node and edge. Mirrors a Ctrl+A style select-all.</summary>
    public async Task SelectAllAsync()
    {
        if (!ElementsSelectable) return;
        foreach (var n in Nodes) n.Selected = n.Selectable && !n.Hidden;
        foreach (var ed in Edges) ed.Selected = ed.Selectable && !ed.Hidden;
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

        if (_lastSelection.Count == nodes.Count + edges.Count
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

    private async Task DeleteSelectionAsync()
    {
        var nodesToDelete = Nodes.Where(n => n.Selected && n.Deletable).ToList();
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

        _edgeIndexDirty = true;
        CaptureHistory();
        await NotifySelectionChangedAsync();
        Announce(AriaLabels.Deleted(nodesToDelete.Count, removedEdges.Count));
        await RaiseDeleteEventsAsync(nodesToDelete, removedEdges);
        await NodesChanged.InvokeAsync(Nodes);
        await EdgesChanged.InvokeAsync(Edges);
        StateHasChanged();
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
        if (!NodesConnectable) return;
        if (!Index.TryGetValue(nodeId, out var node) || !node.Connectable) return;

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
        _connEndValid = false;
        _connMoved = false;
        _reconnectEdge = null;
        RefreshConnectionKeys();
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

        var fixedType = movingEnd == BlowHandleType.Target ? BlowHandleType.Source : BlowHandleType.Target;
        var fixedNodeId = fixedType == BlowHandleType.Source ? edge.Source : edge.Target;
        var fixedHandle = fixedType == BlowHandleType.Source ? edge.SourceHandle : edge.TargetHandle;

        if (!Index.TryGetValue(fixedNodeId, out var node)) return;
        var fallback = fixedType == BlowHandleType.Source ? node.SourcePosition : node.TargetPosition;

        var (x, y, pos) = ResolveAnchor(node, fixedType, fixedHandle, fallback);
        _connSource = new ConnectionSource(node.Id, fixedHandle, fixedType, pos, new BlowXYPosition(x, y));
        _connEnd = ScreenToFlow(e);
        _connEndClientX = e.ClientX;
        _connEndClientY = e.ClientY;
        _connecting = true;
        _connEndValid = false;
        _connMoved = true; // a reconnect is always a drag gesture, never a click-to-connect
        _reconnectEdge = edge;
        RefreshConnectionKeys();
        CapturePane(e.PointerId);
        _ = OnReconnectStart.InvokeAsync(new BlowEdgeReconnectStart(edge, movingEnd));
        _ = OnConnectStart.InvokeAsync(new BlowConnectionStartInfo(node.Id, fixedHandle, fixedType));
        NotifyConnectionChange();
        _ = RefreshPaneRectAsync();
        StateHasChanged();
    }

    /// <summary>Resolves the source and target endpoint anchors of an edge in flow space.</summary>
    private (BlowXYPosition Source, BlowXYPosition Target) GetEdgeEndpoints(BlowEdge edge)
    {
        var ctx = GetEdgeContext(edge);
        return (ctx.Source, ctx.Target);
    }

    /// <summary>Completes the in-progress connection on the given handle, if it is a valid target.</summary>
    public void CompleteConnection(string nodeId, string? handleId, BlowHandleType handleType)
    {
        if (!_connecting) return;

        if (IsValidConnectionTarget(nodeId, handleId, handleType))
        {
            _connEndValid = true;
            // Normalize so the edge always flows source-handle -> target-handle.
            var (sourceNode, sourceHandle, targetNode, targetHandle) =
                _connSource.Type == BlowHandleType.Source
                    ? (_connSource.NodeId, _connSource.HandleId, nodeId, handleId)
                    : (nodeId, handleId, _connSource.NodeId, _connSource.HandleId);

            if (_reconnectEdge is { } re)
            {
                // Reconnecting an existing edge: rewire it in place.
                re.Source = sourceNode;
                re.SourceHandle = sourceHandle;
                re.Target = targetNode;
                re.TargetHandle = targetHandle;
                var conn = new BlowConnection(sourceNode, targetNode, sourceHandle, targetHandle);
                _needsMeasure = true;
                _reconnectRewired = true;
                CaptureHistory();
                _ = OnReconnect.InvokeAsync(new BlowEdgeReconnect(re, conn));
                _ = EdgesChanged.InvokeAsync(Edges);
            }
            else
            {
                var connection = new BlowConnection(sourceNode, targetNode, sourceHandle, targetHandle);

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
                    Source = sourceNode,
                    Target = targetNode,
                    SourceHandle = sourceHandle,
                    TargetHandle = targetHandle,
                    Type = DefaultEdgeType,
                };
                DefaultEdgeOptions?.ApplyTo(edge, DefaultEdgeType);
                Edges.Add(edge);
                _edgeIndexDirty = true;
                CaptureHistory();
                _ = OnConnect.InvokeAsync(connection);
                _ = EdgesChanged.InvokeAsync(Edges);
                NotifyHandlesConnected(connection);
            }
        }

        CancelConnection();
    }

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
        if (ClosestValidHandle() is not { } bh) return false;
        CompleteConnection(bh.nodeId, bh.handleId, bh.type);
        return true;
    }

    /// <summary>
    /// The nearest handle within <see cref="ConnectionRadius"/> of the connection's loose end that
    /// the connection could legally complete on, or null.
    /// </summary>
    private (string nodeId, string? handleId, BlowHandleType type)? ClosestValidHandle()
    {
        if (ConnectionRadius <= 0) return null;
        var radius = ConnectionRadius / _viewport.Zoom;

        double best = double.MaxValue;
        (string nodeId, string? handleId, BlowHandleType type)? bestHandle = null;

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
                if (d > radius || d >= best) continue;

                var hid = string.IsNullOrEmpty(h.HandleId) ? null : h.HandleId;
                if (!IsValidConnectionTarget(n.Id, hid, h.Type)) continue;

                best = d;
                bestHandle = (n.Id, hid, h.Type);
            }
        }

        return bestHandle;
    }

    private void CancelConnection()
    {
        var start = new BlowConnectionStartInfo(_connSource.NodeId, _connSource.HandleId, _connSource.Type);
        var reconnected = _reconnectEdge;
        var rewired = _reconnectRewired;
        var endInfo = new BlowConnectEndInfo(start, _connEnd, _connEndClientX, _connEndClientY, _connEndValid)
        {
            IsDropped = _connDropping,
            IsReconnect = reconnected is not null,
        };

        _connecting = false;
        _connMoved = false;
        _connDropping = false;
        _connHover = null;
        _connSnap = null;
        _reconnectEdge = null;
        _reconnectRewired = false;
        NotifyConnectionChange();

        if (reconnected is not null && OnReconnectEnd.HasDelegate)
        {
            var movingEnd = _connSource.Type == BlowHandleType.Source ? BlowHandleType.Target : BlowHandleType.Source;
            _ = OnReconnectEnd.InvokeAsync(new BlowEdgeReconnectEnd(reconnected, movingEnd, rewired));
        }

        _ = OnConnectEnd.InvokeAsync(endInfo);
        StateHasChanged();
    }

    /// <summary>Tells the canvas the pointer has entered or left a handle while connecting.</summary>
    public void SetConnectionHover(string nodeId, string? handleId, BlowHandleType handleType, bool isOver)
    {
        if (!_connecting) return;
        _connHover = isOver ? (nodeId, handleId, handleType) : null;
        NotifyConnectionChange();
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
    {
        if (!_connecting || !NodesConnectable) return false;
        if (ConnectionMode == BlowConnectionMode.Strict && handleType == _connSource.Type) return false; // strict: source<->target only

        if (nodeId == _connSource.NodeId)
        {
            // A self-connection needs an explicit opt-in, and must still join two *different* handles.
            if (!AllowSelfConnections) return false;
            if (handleId == _connSource.HandleId && handleType == _connSource.Type) return false;
        }

        if (!Index.TryGetValue(nodeId, out var target) || !target.Connectable || target.Hidden) return false;

        var (sourceNode, sourceHandle, targetNode, targetHandle) =
            _connSource.Type == BlowHandleType.Source
                ? (_connSource.NodeId, _connSource.HandleId, nodeId, handleId)
                : (nodeId, handleId, _connSource.NodeId, _connSource.HandleId);

        if (PreventDuplicateEdges && IsDuplicateConnection(sourceNode, sourceHandle, targetNode, targetHandle))
            return false;

        var connection = new BlowConnection(sourceNode, targetNode, sourceHandle, targetHandle);

        if (IsValidConnection is not null && !IsValidConnection(connection)) return false;

        // Both ends get a say: the handle being dropped on, and the one the drag started from.
        if (HandleFor(nodeId, handleType, handleId)?.IsValidConnection is { } targetRule
            && !targetRule(connection))
        {
            return false;
        }

        if (HandleFor(_connSource.NodeId, _connSource.Type, _connSource.HandleId)?.IsValidConnection
                is { } sourceRule
            && !sourceRule(connection))
        {
            return false;
        }

        return true;
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
    /// True when an identical edge already exists. The edge currently being reconnected is
    /// ignored so dropping it back where it came from is still allowed.
    /// </summary>
    /// <remarks>
    /// Backed by a set snapshotted when the gesture starts: every handle re-evaluates its
    /// validity on every render while a connection is being dragged, so a linear scan of the
    /// edge list here would be O(handles x edges) per frame.
    /// </remarks>
    private bool IsDuplicateConnection(string source, string? sourceHandle, string target, string? targetHandle)
        => _existingConnectionKeys.Contains(ConnectionKey(source, sourceHandle, target, targetHandle));

    private static string ConnectionKey(string source, string? sourceHandle, string target, string? targetHandle)
        => $"{source}\u001f{sourceHandle}\u001f{target}\u001f{targetHandle}";

    /// <summary>Snapshots the existing wiring so duplicate checks during the gesture are O(1).</summary>
    private void RefreshConnectionKeys()
    {
        _existingConnectionKeys.Clear();
        foreach (var e in Edges)
        {
            if (ReferenceEquals(e, _reconnectEdge)) continue;
            _existingConnectionKeys.Add(ConnectionKey(e.Source, e.SourceHandle, e.Target, e.TargetHandle));
        }
    }

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
        if (durationMs <= 0 || from == target)
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
        StateHasChanged();
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
        var w = Math.Max(1, bounds.Width);
        var h = Math.Max(1, bounds.Height);
        var paneW = _paneRect.Width;
        var paneH = _paneRect.Height;
        var zoom = Math.Clamp(Math.Min(paneW / w, paneH / h) * (1 - padding), MinZoom, MaxZoom);
        return ClampViewport(
            new BlowViewport(paneW / 2 - (bounds.X + w / 2) * zoom, paneH / 2 - (bounds.Y + h / 2) * zoom, zoom));
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
        CaptureHistory();
        _ = NodesChanged.InvokeAsync(Nodes);
        StateHasChanged();
    }

    /// <summary>Appends edges.</summary>
    public void AddEdges(params BlowEdge[] edges)
    {
        if (edges.Length == 0) return;
        Edges.AddRange(edges);
        _edgeIndexDirty = true;
        CaptureHistory();
        _ = EdgesChanged.InvokeAsync(Edges);
        StateHasChanged();
    }

    /// <summary>Removes the given nodes (and their connected edges) and/or edges.</summary>
    public async Task DeleteElementsAsync(IEnumerable<BlowNode>? nodes = null, IEnumerable<BlowEdge>? edges = null)
    {
        var nodeList = nodes?.Where(n => n.Deletable).ToList() ?? [];
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
        CaptureHistory();
        await NotifySelectionChangedAsync();
        await RaiseDeleteEventsAsync(nodeList, removedEdges);
        await NodesChanged.InvokeAsync(Nodes);
        await EdgesChanged.InvokeAsync(Edges);
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
        await AfterNodesMovedAsync(moved);
    }

    /// <summary>Spaces the selected nodes evenly across the span they already occupy.</summary>
    /// <param name="horizontal">True to space them along X, false along Y.</param>
    /// <param name="gap">
    /// When given, the exact gap to leave between neighbours instead of filling the existing span.
    /// </param>
    public async Task DistributeSelectionAsync(bool horizontal, double? gap = null)
    {
        var moved = BlowAlign.Distribute(SelectedDraggableNodes(), horizontal, NodeOrigin, gap);
        await AfterNodesMovedAsync(moved);
    }

    private List<BlowNode> SelectedDraggableNodes()
        => NodesDraggable ? Nodes.Where(n => n.Selected).ToList() : [];

    /// <summary>
    /// Common tail for the imperative move helpers: keep the moved nodes inside whatever
    /// constrains them, re-measure, announce, and tell the caller its list changed.
    /// </summary>
    private async Task AfterNodesMovedAsync(IReadOnlyList<BlowNode> moved)
    {
        if (moved.Count == 0) return;

        foreach (var node in moved)
        {
            node.Position = ClampNodePosition(node, node.Position);
            ExpandParentFor(node);
        }

        _needsMeasure = true;
        CaptureHistory();
        Announce(AriaLabels.NodesMoved(moved.Count));
        await NodesChanged.InvokeAsync(Nodes);
        StateHasChanged();
    }

    /// <summary>Returns the nodes overlapping the given flow-space rectangle.</summary>
    public IReadOnlyList<BlowNode> GetIntersectingNodes(BlowRect area, bool partially = true)
    {
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
        => NodeIntersects(node.GetRect(), area, partially);

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

    /// <summary>Re-renders the canvas, optionally re-measuring node sizes and handle anchors first.</summary>
    /// <param name="remeasure">True to re-read node geometry from the DOM before rendering.</param>
    public void Refresh(bool remeasure)
    {
        if (remeasure) _needsMeasure = true;
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
    /// Measuring is a single pass over the whole flow, so naming particular nodes does not make
    /// it cheaper; the ids are accepted because that is how the intent reads at the call site.
    /// </remarks>
    public void UpdateNodeInternals(params string[] nodeIds) => Refresh(remeasure: true);

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
    public async Task RestoreAsync(BlowSnapshot snapshot, bool restoreViewport = true, bool detach = true)
    {
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

        await NotifySelectionChangedAsync();
        await NodesChanged.InvokeAsync(Nodes);
        await EdgesChanged.InvokeAsync(Edges);
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
        if (!Index.TryGetValue(nodeId, out var node)) return;

        _resizing = true;
        _resizeNode = node;
        _resizeDir = direction;
        _resizeStartClientX = clientX;
        _resizeStartClientY = clientY;
        _resizeStartPos = node.Position;
        _resizeStartSize = node.EffectiveSize;
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

        var bounds = BlowGraphUtils.GetNodesBounds(candidates);
        var w = Math.Max(1, bounds.Width);
        var h = Math.Max(1, bounds.Height);
        var paneW = _paneRect.Width;
        var paneH = _paneRect.Height;

        var minZoom = Math.Max(MinZoom, options.MinZoom ?? MinZoom);
        var maxZoom = Math.Min(MaxZoom, options.MaxZoom ?? MaxZoom);
        if (minZoom > maxZoom) minZoom = maxZoom;

        var zoom = Math.Clamp(
            Math.Min(paneW / w, paneH / h) * (1 - options.Padding),
            minZoom, maxZoom);

        var x = paneW / 2 - (bounds.X + w / 2) * zoom;
        var y = paneH / 2 - (bounds.Y + h / 2) * zoom;
        return ClampViewport(new BlowViewport(x, y, zoom));
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
