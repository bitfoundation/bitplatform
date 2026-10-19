namespace Bit.Blow.Demo.Client.Examples;

/// <summary>Metadata for a single ported React Flow example.</summary>
public sealed record ExampleInfo(string Route, string Title, string Group, string Summary);

/// <summary>
/// The catalog of React Flow examples ported to Blow. This is the single source of
/// truth used by both the examples index page and (optionally) navigation. Each entry maps
/// to a routable page under <c>/examples/{route}</c>.
/// </summary>
public static class ExampleCatalog
{
    public const string BasicGroup = "Basics";
    public const string NodesGroup = "Nodes";
    public const string EdgesGroup = "Edges";
    public const string HandlesGroup = "Handles & Connections";
    public const string InteractionGroup = "Interaction";
    public const string ViewportGroup = "Viewport & Layout";
    public const string StateGroup = "State & API";
    public const string AddonGroup = "Add-ons";

    public static readonly IReadOnlyList<ExampleInfo> All =
    [
        // ---- Basics ----
        new("basic", "Basic", BasicGroup, "The minimal flow: a few nodes and edges with pan, zoom and selection."),
        new("empty", "Empty", BasicGroup, "An empty canvas you can build on by adding nodes at runtime."),
        new("default-nodes", "Default Nodes", BasicGroup, "The built-in input, default and output node types."),
        new("overview", "Overview", BasicGroup, "A bigger graph showcasing node types, edge types and overlays together."),
        new("broken-nodes", "Broken Nodes", BasicGroup, "How the flow tolerates edges that reference missing nodes/handles."),

        // ---- Nodes ----
        new("custom-node", "Custom Node", NodesGroup, "A node rendered with a fully custom template and multiple handles."),
        new("default-node-overwrite", "Default Node Overwrite", NodesGroup, "Replace the built-in default node with your own renderer."),
        new("drag-handle", "Drag Handle", NodesGroup, "Only a designated area of the node initiates dragging."),
        new("node-type-change", "Node Type Change", NodesGroup, "Switch a node's type at runtime."),
        new("update-node", "Update Node", NodesGroup, "Update a node's label, position and style imperatively."),
        new("stress", "Stress", NodesGroup, "Many nodes and edges with only-render-visible virtualization."),
        new("hidden", "Hidden", NodesGroup, "Toggle visibility of nodes and edges."),
        new("subflow", "Subflow", NodesGroup, "Nested nodes inside group containers (parent/child)."),
        new("handle-positions", "Handle Positions", NodesGroup, "Move the built-in node handles for top-down or left-right flows."),
        new("node-extent", "Node Extents", NodesGroup, "Constrain a node to a rectangle or its parent, or grow the parent to fit."),
        new("editable-node", "Editable Node", NodesGroup, "Text inputs inside a node, with Backspace left to the field."),

        // ---- Edges ----
        new("edges", "Edges", EdgesGroup, "All built-in edge path types side by side."),
        new("edge-types", "Edge Types", EdgesGroup, "Bezier, straight, step and smooth-step edges, animated and labelled."),
        new("custom-edges", "Custom Edges", EdgesGroup, "Custom edge templates built on BlowBaseEdge with interactive labels."),
        new("edge-renderer", "Edge Renderer", EdgesGroup, "Custom edge with a button rendered on the edge label layer."),
        new("default-edge-overwrite", "Default Edge Overwrite", EdgesGroup, "Apply default options to every newly created edge."),
        new("floating-edges", "Floating Edges", EdgesGroup, "Edges that attach to the nearest point of each node."),
        new("reconnect-edge", "Reconnect Edge", EdgesGroup, "Drag an existing edge endpoint to a new handle."),
        new("edge-toolbar", "Edge Toolbar", EdgesGroup, "A toolbar that appears on the selected edge."),
        new("edge-path-options", "Edge Path Options", EdgesGroup, "Tune curvature, corner radius, offset and bend position per edge."),
        new("edge-markers", "Edge Markers", EdgesGroup, "Per-edge arrowheads: open or closed, in any colour and size."),
        new("edge-label-styling", "Edge Label Styling", EdgesGroup, "Restyle, pad, round or remove the plate behind an edge label."),
        new("self-loop-edges", "Self-Connections", EdgesGroup, "Edges back to their own node loop clear of it instead of collapsing onto it."),

        // ---- Handles & Connections ----
        new("validation", "Validation", HandlesGroup, "Validate connections while dragging and reject invalid ones."),
        new("custom-connection-line", "Custom Connection Line", HandlesGroup, "Customize the line drawn while creating a connection."),
        new("easy-connect", "Easy Connect", HandlesGroup, "Start a connection from anywhere on a node."),
        new("connection-mode", "Connection Mode", HandlesGroup, "Loose vs strict connection modes."),
        new("add-node-on-edge-drop", "Add Node on Edge Drop", HandlesGroup, "Drop a connection on the pane to spawn a connected node."),
        new("cancel-connection", "Cancel Connection", HandlesGroup, "Press Escape to cancel an in-progress connection."),
        new("use-connection", "Use Connection", HandlesGroup, "Observe the in-progress connection state live."),
        new("use-node-connections", "Use Node Connections", HandlesGroup, "List the edges connected to a node."),
        new("moving-handles", "Moving Handles", HandlesGroup, "Add/move handles at runtime and refresh edge anchors."),
        new("connection-rules", "Connection Rules", HandlesGroup, "Duplicate, self and non-connectable rules applied while dragging."),
        new("handle-rules", "Handle Rules", HandlesGroup, "Validation and connect callbacks that live on the handle itself."),

        // ---- Interaction ----
        new("interaction", "Interaction", InteractionGroup, "Toggle draggable, selectable, zoom and pan options live."),
        new("drag-n-drop", "Drag and Drop", InteractionGroup, "Drag new nodes from a sidebar onto the canvas."),
        new("use-key-press", "Use Key Press", InteractionGroup, "React to keyboard shortcuts over the flow."),
        new("intersection", "Intersection", InteractionGroup, "Detect which nodes intersect a dragged node."),
        new("click-distance", "Click Distance", InteractionGroup, "Distinguish a click from a drag using a distance threshold."),
        new("use-on-selection-change", "Selection Change", InteractionGroup, "React to selection changes and show the selected ids."),
        new("delete-protection", "Delete Protection", InteractionGroup, "Deletable/Selectable flags, select-all and the delete veto."),
        new("auto-pan", "Auto Pan & Pinch", InteractionGroup, "Drag past the canvas edge to scroll; pinch with two fingers to zoom."),
        new("interaction-guards", "Interaction Guards", InteractionGroup, "nodrag, nopan and nowheel: carve interactivity back out of the canvas."),
        new("key-bindings", "Key Bindings", InteractionGroup, "Choose which keys delete, zoom and multi-select."),
        new("alignment-guides", "Alignment Guides", InteractionGroup, "Dragged nodes click into line with their neighbours."),
        new("selection-gestures", "Selection Gestures", InteractionGroup, "Drag and right-click a whole selection; choose the selection and multi-select keys."),

        // ---- Viewport & Layout ----
        new("backgrounds", "Backgrounds", ViewportGroup, "Dots, lines and cross background patterns."),
        new("controlled-viewport", "Controlled Viewport", ViewportGroup, "Drive the viewport from your own state."),
        new("color-mode", "Color Mode", ViewportGroup, "Light, dark and system color themes."),
        new("interactive-minimap", "Interactive MiniMap", ViewportGroup, "Pan and zoom the flow using the minimap."),
        new("custom-minimap-node", "Custom MiniMap Node", ViewportGroup, "Render minimap nodes with custom colors/shapes."),
        new("layouting", "Layouting", ViewportGroup, "Layered, grid and circle auto-layouts from the built-in BlowLayout."),
        new("align-distribute", "Align & Distribute", ViewportGroup, "Line up and evenly space the selected nodes, the way a drawing tool does."),
        new("viewport-transitions", "Viewport Transitions", ViewportGroup, "Ease the viewport between two parts of a graph instead of cutting."),
        new("zindex-mode", "Z-Index Mode", ViewportGroup, "Control stacking of nodes and edges."),
        new("devtools", "DevTools", ViewportGroup, "Inspect viewport, nodes and change events."),
        new("overlay-layout", "Overlay Layout", ViewportGroup, "Anchor controls, minimap, panels and attribution; lock the canvas."),
        new("responsive-canvas", "Responsive Canvas", ViewportGroup, "Container and node resizes are tracked automatically; controlled viewport."),

        // ---- State & API ----
        new("use-reactflow", "Use Flow API", StateGroup, "Call the imperative flow API (zoom, fit, add, delete)."),
        new("provider", "Provider", StateGroup, "Control a flow from outside via a captured reference."),
        new("save-restore", "Save & Restore", StateGroup, "Persist and reload the flow from local storage."),
        new("controlled-uncontrolled", "Controlled vs Uncontrolled", StateGroup, "Compare controlled and uncontrolled node state."),
        new("multi-set-nodes", "Multi setNodes", StateGroup, "Multiple successive state updates in one tick."),
        new("set-nodes-batching", "setNodes Batching", StateGroup, "Batch many node updates efficiently."),
        new("use-nodes-data", "Use Nodes Data", StateGroup, "Read and react to another node's data."),
        new("use-update-node-internals", "Update Node Internals", StateGroup, "Re-measure a node after its handles change."),
        new("multi-flows", "Multi Flows", StateGroup, "Several independent flow instances on one page."),
        new("redux", "External Store", StateGroup, "Drive a flow from a shared external store."),
        new("diagnostics", "Diagnostics", StateGroup, "OnError reports a malformed graph instead of throwing on it."),
        new("undo-redo", "Undo & Redo", StateGroup, "Snapshot-based history you drive from your own change events."),
        new("built-in-undo-redo", "Built-in Undo & Redo", StateGroup, "Let the canvas keep the undo stack and bind the shortcuts itself."),
        new("copy-paste", "Copy & Paste", StateGroup, "Copy, cut, paste and duplicate a selection, groups and all."),

        // ---- Add-ons ----
        new("node-resizer", "Node Resizer", AddonGroup, "Resize nodes with the BlowNodeResizer control."),
        new("node-toolbar", "Node Toolbar", AddonGroup, "A toolbar anchored to the selected node."),
        new("touch-device", "Touch Device", AddonGroup, "Pointer-based interactions that work on touch."),
        new("a11y", "Accessibility", AddonGroup, "Keyboard focus and arrow-key movement of nodes."),
        new("keyboard-a11y", "Keyboard Accessibility", AddonGroup, "Tab to nodes and edges, activate with Enter/Space, live announcements."),
        new("localization", "Localization", AddonGroup, "Translate every label and announcement the canvas makes."),
    ];

    public static IEnumerable<IGrouping<string, ExampleInfo>> Grouped() =>
        All.GroupBy(e => e.Group);
}
