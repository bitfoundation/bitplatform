using Bit.Blow.Demo.Client.Pages.Examples;

namespace Bit.Blow.Demo.Client.Examples;

/// <summary>
/// Metadata for a single ported React Flow example. <paramref name="Keywords"/> feed the site's search
/// palette, and <paramref name="Page"/> is the component its route renders - which the docs shell turns into
/// the "Edit this page on GitHub" link.
/// </summary>
public sealed record ExampleInfo(string Route, string Title, string Group, string Summary, string Keywords, Type Page);

/// <summary>
/// The catalog of React Flow examples ported to Blow. This is the single source of truth for the examples
/// gallery: the index page's cards, the docs navigation and search palette (DocsCatalog builds a section per
/// group from it) and the MCP server's search all read it. Each entry maps to a routable page under
/// <c>/examples/{route}</c>.
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
        new("basic", "Basic", BasicGroup, "The minimal flow: a few nodes and edges with pan, zoom and selection.",
            "minimal first flow BlowCanvas nodes edges pan zoom select connect FitViewOnInit", typeof(BasicExample)),
        new("empty", "Empty", BasicGroup, "An empty canvas you can build on by adding nodes at runtime.",
            "empty blank canvas add nodes runtime Refresh remeasure clear", typeof(EmptyExample)),
        new("default-nodes", "Default Nodes", BasicGroup, "The built-in input, default and output node types.",
            "built-in node types input default output Type handles", typeof(DefaultNodesExample)),
        new("overview", "Overview", BasicGroup, "A bigger graph showcasing node types, edge types and overlays together.",
            "overview showcase graph edge types minimap NodeColorFunc MinZoom MaxZoom panel", typeof(OverviewExample)),
        new("broken-nodes", "Broken Nodes", BasicGroup, "How the flow tolerates edges that reference missing nodes/handles.",
            "broken missing node dangling edge handle tolerant skipped graceful", typeof(BrokenNodesExample)),

        // ---- Nodes ----
        new("custom-node", "Custom Node", NodesGroup, "A node rendered with a fully custom template and multiple handles.",
            "custom node NodeTemplate text updater input data nodrag BlowHandle", typeof(CustomNodeExample)),
        new("default-node-overwrite", "Default Node Overwrite", NodesGroup, "Replace the built-in default node with your own renderer.",
            "override default node renderer NodeTemplate BlowDefaultNode card", typeof(DefaultNodeOverwriteExample)),
        new("drag-handle", "Drag Handle", NodesGroup, "Only a designated area of the node initiates dragging.",
            "drag handle grip BlowDragHandle UseCustomDragHandle", typeof(DragHandleExample)),
        new("node-type-change", "Node Type Change", NodesGroup, "Switch a node's type at runtime.",
            "change node type runtime input default output cycle", typeof(NodeTypeChangeExample)),
        new("update-node", "Update Node", NodesGroup, "Update a node's label, position and style imperatively.",
            "update node label style background position imperative updateNode", typeof(UpdateNodeExample)),
        new("stress", "Stress", NodesGroup, "Many nodes and edges with only-render-visible virtualization.",
            "stress performance large graph many nodes OnlyRenderVisibleElements culling virtualization", typeof(StressExample)),
        new("hidden", "Hidden", NodesGroup, "Toggle visibility of nodes and edges.",
            "hidden hide show collapse visibility Hidden flag", typeof(HiddenExample)),
        new("subflow", "Subflow", NodesGroup, "Nested nodes inside group containers (parent/child).",
            "subflow sub-flow group parent child ParentId ExtentParent nested", typeof(SubflowExample)),
        new("handle-positions", "Handle Positions", NodesGroup, "Move the built-in node handles for top-down or left-right flows.",
            "handle positions SourcePosition TargetPosition left right top bottom horizontal vertical", typeof(HandlePositionsExample)),
        new("node-extent", "Node Extents", NodesGroup, "Constrain a node to a rectangle or its parent, or grow the parent to fit.",
            "extent constrain bounds Extent BlowRect ExtentParent ExpandParent group", typeof(NodeExtentExample)),
        new("editable-node", "Editable Node", NodesGroup, "Text inputs inside a node, with Backspace left to the field.",
            "editable node input textarea form field Backspace focus drag handle", typeof(EditableNodeExample)),

        // ---- Edges ----
        new("edges", "Edges", EdgesGroup, "All built-in edge path types side by side.",
            "edges path types bezier straight step smoothstep animated label", typeof(EdgesExample)),
        new("edge-types", "Edge Types", EdgesGroup, "Bezier, straight, step and smooth-step edges, animated and labelled.",
            "edge types DefaultEdgeType ConnectionLineType DefaultEdgeOptions animated markers", typeof(EdgeTypesExample)),
        new("custom-edges", "Custom Edges", EdgesGroup, "Custom edge templates built on BlowBaseEdge with interactive labels.",
            "custom edge EdgeTemplate BlowBaseEdge stroke color width data", typeof(CustomEdgesExample)),
        new("edge-renderer", "Edge Renderer", EdgesGroup, "Custom edge with a button rendered on the edge label layer.",
            "edge label renderer EdgeLabelTemplate HTML button delete EdgeLabelRenderer", typeof(EdgeRendererExample)),
        new("default-edge-overwrite", "Default Edge Overwrite", EdgesGroup, "Apply default options to every newly created edge.",
            "default edge options DefaultEdgeOptions BlowDefaultEdgeOptions new connections", typeof(DefaultEdgeOverwriteExample)),
        new("floating-edges", "Floating Edges", EdgesGroup, "Edges that attach to the nearest point of each node.",
            "floating edges GetFloatingEdgeParams BlowGraphUtils border nearest point", typeof(FloatingEdgesExample)),
        new("reconnect-edge", "Reconnect Edge", EdgesGroup, "Drag an existing edge endpoint to a new handle.",
            "reconnect rewire edge endpoint EdgesReconnectable OnReconnect", typeof(ReconnectEdgeExample)),
        new("edge-toolbar", "Edge Toolbar", EdgesGroup, "A toolbar that appears on the selected edge.",
            "edge toolbar BlowEdgeToolbar selected edge relabel recolor delete", typeof(EdgeToolbarExample)),
        new("edge-path-options", "Edge Path Options", EdgesGroup, "Tune curvature, corner radius, offset and bend position per edge.",
            "path options PathOptions BlowEdgePathOptions curvature BorderRadius Offset StepPosition", typeof(EdgePathOptionsExample)),
        new("edge-markers", "Edge Markers", EdgesGroup, "Per-edge arrowheads: open or closed, in any colour and size.",
            "markers arrowheads arrow ArrowClosed MarkerColor MarkerWidth MarkerStart MarkerEnd", typeof(EdgeMarkersExample)),
        new("edge-label-styling", "Edge Label Styling", EdgesGroup, "Restyle, pad, round or remove the plate behind an edge label.",
            "edge label style plate LabelStyle LabelBgStyle LabelShowBg LabelBgPaddingX LabelBgBorderRadius", typeof(EdgeLabelStylingExample)),
        new("self-loop-edges", "Self-Connections", EdgesGroup, "Edges back to their own node loop clear of it instead of collapsing onto it.",
            "self loop self-connection AllowSelfConnections LoopRadius recursion retry", typeof(SelfLoopEdgesExample)),

        // ---- Handles & Connections ----
        new("validation", "Validation", HandlesGroup, "Validate connections while dragging and reject invalid ones.",
            "validation IsValidConnection valid invalid handle connect reject", typeof(ValidationExample)),
        new("custom-connection-line", "Custom Connection Line", HandlesGroup, "Customize the line drawn while creating a connection.",
            "connection line ConnectionLineTemplate gradient custom line dragging", typeof(CustomConnectionLineExample)),
        new("easy-connect", "Easy Connect", HandlesGroup, "Start a connection from anywhere on a node.",
            "easy connect whole node handle loose ConnectionMode floating edges", typeof(EasyConnectExample)),
        new("connection-mode", "Connection Mode", HandlesGroup, "Loose vs strict connection modes.",
            "connection mode strict loose ConnectionMode source target", typeof(ConnectionModeExample)),
        new("add-node-on-edge-drop", "Add Node on Edge Drop", HandlesGroup, "Drop a connection on the pane to spawn a connected node.",
            "add node drop connection pane OnConnectEnd BlowConnectEndInfo IsDropped FlowPosition", typeof(AddNodeOnEdgeDropExample)),
        new("cancel-connection", "Cancel Connection", HandlesGroup, "Press Escape to cancel an in-progress connection.",
            "cancel connection Escape OnConnectStart OnConnectEnd abort", typeof(CancelConnectionExample)),
        new("use-connection", "Use Connection", HandlesGroup, "Observe the in-progress connection state live.",
            "useConnection OnConnectionChange BlowConnectionLineContext live connection state", typeof(UseConnectionExample)),
        new("use-node-connections", "Use Node Connections", HandlesGroup, "List the edges connected to a node.",
            "node connections GetConnectedEdges GetIncomers GetOutgoers BlowGraphUtils neighbours", typeof(UseNodeConnectionsExample)),
        new("moving-handles", "Moving Handles", HandlesGroup, "Add/move handles at runtime and refresh edge anchors.",
            "moving handles add remove runtime Refresh remeasure anchors", typeof(MovingHandlesExample)),
        new("connection-rules", "Connection Rules", HandlesGroup, "Duplicate, self and non-connectable rules applied while dragging.",
            "connection rules PreventDuplicateEdges AllowSelfConnections Connectable ConnectionRadius", typeof(ConnectionRulesExample)),
        new("handle-rules", "Handle Rules", HandlesGroup, "Validation and connect callbacks that live on the handle itself.",
            "handle rules per-handle IsValidConnection OnConnect typed ports", typeof(HandleRulesExample)),

        // ---- Interaction ----
        new("interaction", "Interaction", InteractionGroup, "Toggle draggable, selectable, zoom and pan options live.",
            "interaction NodesDraggable ElementsSelectable ZoomOnScroll PanOnDrag ZoomOnDoubleClick", typeof(InteractionExample)),
        new("drag-n-drop", "Drag and Drop", InteractionGroup, "Drag new nodes from a sidebar onto the canvas.",
            "drag and drop sidebar palette ScreenToFlowPosition ondrop new nodes", typeof(DragNDropExample)),
        new("use-key-press", "Use Key Press", InteractionGroup, "React to keyboard shortcuts over the flow.",
            "key press keyboard shortcut onkeydown DeleteKeyEnabled", typeof(UseKeyPressExample)),
        new("intersection", "Intersection", InteractionGroup, "Detect which nodes intersect a dragged node.",
            "intersection overlap GetIntersectingNodes collision OnNodeDrag", typeof(IntersectionExample)),
        new("click-distance", "Click Distance", InteractionGroup, "Distinguish a click from a drag using a distance threshold.",
            "click distance NodeClickDistance PaneClickDistance OnNodeClick OnPaneClick threshold", typeof(ClickDistanceExample)),
        new("use-on-selection-change", "Selection Change", InteractionGroup, "React to selection changes and show the selected ids.",
            "selection change OnSelectionChange selected nodes edges box select", typeof(UseOnSelectionChangeExample)),
        new("delete-protection", "Delete Protection", InteractionGroup, "Deletable/Selectable flags, select-all and the delete veto.",
            "delete protection Deletable Selectable locked read-only OnDelete select all", typeof(DeleteProtectionExample)),
        new("auto-pan", "Auto Pan & Pinch", InteractionGroup, "Drag past the canvas edge to scroll; pinch with two fingers to zoom.",
            "auto pan AutoPanOnNodeDrag AutoPanSpeed AutoPanMargin pinch ZoomOnPinch touch", typeof(AutoPanExample)),
        new("interaction-guards", "Interaction Guards", InteractionGroup, "nodrag, nopan and nowheel: carve interactivity back out of the canvas.",
            "nodrag nopan nowheel guards NoDragClass slider scroll inside node", typeof(InteractionGuardsExample)),
        new("key-bindings", "Key Bindings", InteractionGroup, "Choose which keys delete, zoom and multi-select.",
            "key bindings DeleteKeys ZoomActivationKey MultiSelectionKey Backspace modifier", typeof(KeyBindingsExample)),
        new("alignment-guides", "Alignment Guides", InteractionGroup, "Dragged nodes click into line with their neighbours.",
            "alignment guides snap SnapToGuides GuideDistance SnapToGrid", typeof(AlignmentGuidesExample)),
        new("selection-gestures", "Selection Gestures", InteractionGroup, "Drag and right-click a whole selection; choose the selection and multi-select keys.",
            "selection drag OnSelectionDragStart OnSelectionContextMenu SelectionKey SelectionOnDrag", typeof(SelectionGesturesExample)),

        // ---- Viewport & Layout ----
        new("backgrounds", "Backgrounds", ViewportGroup, "Dots, lines and cross background patterns.",
            "background pattern BlowBackground dots lines cross gap color", typeof(BackgroundsExample)),
        new("controlled-viewport", "Controlled Viewport", ViewportGroup, "Drive the viewport from your own state.",
            "controlled viewport Viewport OnViewportChanged BlowViewport pan zoom state", typeof(ControlledViewportExample)),
        new("color-mode", "Color Mode", ViewportGroup, "Light, dark and system color themes.",
            "color mode colour theme dark light system ColorMode BlowColorMode custom properties", typeof(ColorModeExample)),
        new("interactive-minimap", "Interactive MiniMap", ViewportGroup, "Pan and zoom the flow using the minimap.",
            "minimap BlowMiniMap Pannable Zoomable navigate", typeof(InteractiveMinimapExample)),
        new("custom-minimap-node", "Custom MiniMap Node", ViewportGroup, "Render minimap nodes with custom colors/shapes.",
            "minimap NodeColorFunc NodeStrokeColorFunc BorderRadius colours", typeof(CustomMiniMapNodeExample)),
        new("layouting", "Layouting", ViewportGroup, "Layered, grid and circle auto-layouts from the built-in BlowLayout.",
            "layout auto-layout BlowLayout Layered Grid Circle dagre elk direction", typeof(LayoutingExample)),
        new("align-distribute", "Align & Distribute", ViewportGroup, "Line up and evenly space the selected nodes, the way a drawing tool does.",
            "align distribute AlignSelectionAsync DistributeSelectionAsync BlowAlignEdge tidy", typeof(AlignDistributeExample)),
        new("viewport-transitions", "Viewport Transitions", ViewportGroup, "Ease the viewport between two parts of a graph instead of cutting.",
            "viewport transition animate SetCenterAsync FitViewAsync ZoomToAsync duration OnMoveStart OnMoveEnd", typeof(ViewportTransitionsExample)),
        new("zindex-mode", "Z-Index Mode", ViewportGroup, "Control stacking of nodes and edges.",
            "z-index stacking order ZIndexMode ZIndex auto basic manual", typeof(ZIndexModeExample)),
        new("devtools", "DevTools", ViewportGroup, "Inspect viewport, nodes and change events.",
            "devtools inspector viewport logger change log debug", typeof(DevToolsExample)),
        new("overlay-layout", "Overlay Layout", ViewportGroup, "Anchor controls, minimap, panels and attribution; lock the canvas.",
            "overlay controls position minimap panel attribution lock ShowInteractive orientation", typeof(OverlayLayoutExample)),
        new("responsive-canvas", "Responsive Canvas", ViewportGroup, "Container and node resizes are tracked automatically; controlled viewport.",
            "responsive resize container ResizeObserver node grows measure", typeof(ResponsiveCanvasExample)),

        // ---- State & API ----
        new("use-reactflow", "Use Flow API", StateGroup, "Call the imperative flow API (zoom, fit, add, delete).",
            "useReactFlow imperative API @ref ZoomIn ZoomOut FitView AddNodes DeleteElementsAsync ToObject", typeof(UseReactFlowExample)),
        new("provider", "Provider", StateGroup, "Control a flow from outside via a captured reference.",
            "provider ReactFlowProvider sidebar outside reference AddEdges", typeof(ProviderExample)),
        new("save-restore", "Save & Restore", StateGroup, "Persist and reload the flow from local storage.",
            "save restore persist local storage ToObject RestoreAsync BlowSnapshot json", typeof(SaveRestoreExample)),
        new("controlled-uncontrolled", "Controlled vs Uncontrolled", StateGroup, "Compare controlled and uncontrolled node state.",
            "controlled uncontrolled NodesChanged EdgesChanged state ToObject", typeof(ControlledUncontrolledExample)),
        new("multi-set-nodes", "Multi setNodes", StateGroup, "Multiple successive state updates in one tick.",
            "setNodes multiple updates one render batch shuffle", typeof(MultiSetNodesExample)),
        new("set-nodes-batching", "setNodes Batching", StateGroup, "Batch many node updates efficiently.",
            "batch AddNodes many nodes single update measure", typeof(SetNodesBatchingExample)),
        new("use-nodes-data", "Use Nodes Data", StateGroup, "Read and react to another node's data.",
            "nodes data Data derived computed source result slider", typeof(UseNodesDataExample)),
        new("use-update-node-internals", "Update Node Internals", StateGroup, "Re-measure a node after its handles change.",
            "useUpdateNodeInternals UpdateNodeInternals Refresh remeasure handles", typeof(UseUpdateNodeInternalsExample)),
        new("multi-flows", "Multi Flows", StateGroup, "Several independent flow instances on one page.",
            "multiple flows two canvases independent instances", typeof(MultiFlowsExample)),
        new("redux", "External Store", StateGroup, "Drive a flow from a shared external store.",
            "redux zustand store external state dispatch", typeof(ReduxExample)),
        new("diagnostics", "Diagnostics", StateGroup, "OnError reports a malformed graph instead of throwing on it.",
            "diagnostics OnError BlowError duplicate id dangling edge orphan", typeof(DiagnosticsExample)),
        new("undo-redo", "Undo & Redo", StateGroup, "Snapshot-based history you drive from your own change events.",
            "undo redo BlowHistory Capture snapshot history", typeof(UndoRedoExample)),
        new("built-in-undo-redo", "Built-in Undo & Redo", StateGroup, "Let the canvas keep the undo stack and bind the shortcuts itself.",
            "undo redo HistoryEnabled HistoryCapacity OnHistoryChanged UndoAsync RedoAsync Ctrl+Z", typeof(BuiltInUndoRedoExample)),
        new("copy-paste", "Copy & Paste", StateGroup, "Copy, cut, paste and duplicate a selection, groups and all.",
            "copy paste cut duplicate clipboard ClipboardShortcutsEnabled BlowFragment Ctrl+C Ctrl+V", typeof(CopyPasteExample)),

        // ---- Add-ons ----
        new("node-resizer", "Node Resizer", AddonGroup, "Resize nodes with the BlowNodeResizer control.",
            "resize resizer BlowNodeResizer MinWidth MinHeight KeepAspectRatio", typeof(NodeResizerExample)),
        new("node-toolbar", "Node Toolbar", AddonGroup, "A toolbar anchored to the selected node.",
            "node toolbar BlowNodeToolbar selected Position actions", typeof(NodeToolbarExample)),
        new("touch-device", "Touch Device", AddonGroup, "Pointer-based interactions that work on touch.",
            "touch mobile pointer events tablet SelectionOnDrag pinch", typeof(TouchDeviceExample)),
        new("a11y", "Accessibility", AddonGroup, "Keyboard focus and arrow-key movement of nodes.",
            "accessibility a11y keyboard focus arrow keys AriaLabel screen reader", typeof(A11yExample)),
        new("keyboard-a11y", "Keyboard Accessibility", AddonGroup, "Tab to nodes and edges, activate with Enter/Space, live announcements.",
            "keyboard accessibility tab EdgesFocusable aria-live Enter Space Escape", typeof(KeyboardA11yExample)),
        new("localization", "Localization", AddonGroup, "Translate every label and announcement the canvas makes.",
            "localization translate i18n AriaLabels BlowAriaLabels language DefaultViewport", typeof(LocalizationExample)),
    ];

    public static IEnumerable<IGrouping<string, ExampleInfo>> Grouped() =>
        All.GroupBy(e => e.Group);
}
