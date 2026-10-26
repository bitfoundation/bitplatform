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
        new("empty", "Empty", BasicGroup, "An empty canvas built up at runtime, with OnNodesInitialized and NodesInitialized telling when new nodes are measured.",
            "empty blank canvas add nodes runtime clear OnNodesInitialized NodesInitialized Measured size line up layout FitViewAsync", typeof(EmptyExample)),
        new("default-nodes", "Default Nodes", BasicGroup, "The built-in input, default, output and group node types, and restyling a type with CSS.",
            "built-in node types input default output group Type handles bit-blow-node CSS class restyle Class", typeof(DefaultNodesExample)),
        new("overview", "Overview", BasicGroup, "A bigger graph combining node types, edge types and the add-ons, with a lock button that makes it read-only.",
            "overview showcase graph edge types minimap NodeColorFunc MinZoom MaxZoom panel lock ShowInteractive Interactive NodesDraggable NodesConnectable ElementsSelectable smoothstep step straight animated label BlowPanel Anchor", typeof(OverviewExample)),
        new("broken-nodes", "Broken Nodes", BasicGroup, "How the canvas tolerates edges to missing or hidden nodes, missing handles and duplicate edge ids, and what OnError reports.",
            "broken missing node hidden dangling edge missing handle duplicate edge id tolerant skipped graceful OnError BlowError missing-edge-endpoint duplicate-edge-id Ports", typeof(BrokenNodesExample)),

        // ---- Nodes ----
        new("custom-node", "Custom Node", NodesGroup, "A node rendered with a fully custom template and multiple handles.",
            "custom node NodeTemplate text updater input data nodrag BlowHandle SourceHandle handle Id EdgesChanged BlowDefaultNode", typeof(CustomNodeExample)),
        new("default-node-overwrite", "Default Node Overwrite", NodesGroup, "Replace the built-in default node with your own renderer.",
            "override default node renderer NodeTemplate BlowDefaultNode card TargetPosition SourcePosition", typeof(DefaultNodeOverwriteExample)),
        new("drag-handle", "Drag Handle", NodesGroup, "Only a designated area of the node initiates dragging.",
            "drag handle grip BlowDragHandle UseCustomDragHandle title bar interactive content", typeof(DragHandleExample)),
        new("node-type-change", "Node Type Change", NodesGroup, "Switch a node's type at runtime.",
            "change node type runtime input default output select missing handle", typeof(NodeTypeChangeExample)),
        new("update-node", "Update Node", NodesGroup, "Update a node's label, style, position and interaction flags from outside the canvas, and duplicate it with Clone.",
            "update node label style background position imperative updateNode Draggable Selectable Connectable Focusable flags Clone duplicate copy", typeof(UpdateNodeExample)),
        new("stress", "Stress", NodesGroup, "Many nodes and edges with only-render-visible virtualization.",
            "stress performance large graph many nodes OnlyRenderVisibleElements culling virtualization BlowViewportView MinZoom rendered count", typeof(StressExample)),
        new("hidden", "Hidden", NodesGroup, "Hide nodes and edges, and fit the view with or without the hidden nodes.",
            "hidden hide show collapse visibility Hidden flag FitViewAsync BlowFitViewOptions IncludeHiddenNodes", typeof(HiddenExample)),
        new("subflow", "Subflow", NodesGroup, "Nested nodes inside group containers (parent/child).",
            "subflow sub-flow group parent child ParentId ExtentParent nested nested groups ZIndexMode", typeof(SubflowExample)),
        new("handle-positions", "Handle Positions", NodesGroup, "Move the built-in node handles for top-down or left-right flows.",
            "handle positions SourcePosition TargetPosition left right top bottom horizontal vertical direction UpdateNodeInternals", typeof(HandlePositionsExample)),
        new("node-extent", "Node Extents", NodesGroup, "Constrain a node to a rectangle, its parent or the canvas's NodeExtent, or grow the parent to fit.",
            "extent constrain bounds Extent BlowRect ExtentParent ExpandParent group NodeExtent BlowViewportPortal", typeof(NodeExtentExample)),
        new("editable-node", "Editable Node", NodesGroup, "Text inputs inside a node, with Backspace left to the field.",
            "editable node input textarea form field Backspace focus drag handle", typeof(EditableNodeExample)),

        // ---- Edges ----
        new("edges", "Edges", EdgesGroup, "All six built-in edge path types side by side, with labelled, coloured, dashed, animated and arrow-headed variants.",
            "edges path types bezier SimpleBezier straight step smoothstep routed animated label Stroke StrokeWidth Style dashed MarkerStart ArrowClosed InteractionWidth hit area Focusable tab", typeof(EdgesExample)),
        new("edge-types", "Edge Types", EdgesGroup, "One edge of each of the six path types, and a picker for the type, animation and arrowhead of the edges you draw.",
            "edge types BlowEdgeType Bezier SimpleBezier Straight Step SmoothStep Routed DefaultEdgeType ConnectionLineType DefaultEdgeOptions animated markers", typeof(EdgeTypesExample)),
        new("custom-edges", "Custom Edges", EdgesGroup, "Custom edge templates built on BlowBaseEdge: colours from edge data, passed-on arrowheads and tinted labels.",
            "custom edge EdgeTemplate BlowBaseEdge BlowEdgeContext BlowEdgeText MarkerEndUrl stroke color width data label", typeof(CustomEdgesExample)),
        new("edge-renderer", "Edge Renderer", EdgesGroup, "HTML edge labels with a delete button, kept at one size on the edge label layer.",
            "edge label renderer EdgeLabelTemplate HTML button delete EdgeLabelRenderer nopan nodrag constant size zoom", typeof(EdgeRendererExample)),
        new("default-edge-overwrite", "Default Edge Overwrite", EdgesGroup, "Apply default options to every newly created edge.",
            "default edge options DefaultEdgeOptions BlowDefaultEdgeOptions new connections PathOptions InteractionWidth Reconnectable Stroke StrokeWidth animated ArrowClosed", typeof(DefaultEdgeOverwriteExample)),
        new("floating-edges", "Floating Edges", EdgesGroup, "Edges that attach to the facing point of each node's border, in any path type.",
            "floating edges GetFloatingEdgeParams BlowGraphUtils border nearest point BlowEdgePath.Compute EdgeTemplate ConnectionMode Loose", typeof(FloatingEdgesExample)),
        new("reconnect-edge", "Reconnect Edge", EdgesGroup, "Drag an existing edge's end to a new handle, with the reconnect events and edges deleted when dropped on nothing.",
            "reconnect rewire edge endpoint EdgesReconnectable Reconnectable ReconnectRadius OnReconnect OnReconnectStart OnReconnectEnd Rewired delete keyboard", typeof(ReconnectEdgeExample)),
        new("edge-toolbar", "Edge Toolbar", EdgesGroup, "A toolbar that appears on the selected edge.",
            "edge toolbar BlowEdgeToolbar selected edge relabel recolor delete EdgeLabelTemplate input rename", typeof(EdgeToolbarExample)),
        new("edge-path-options", "Edge Path Options", EdgesGroup, "Tune curvature, corner radius, offset and bend position per edge.",
            "path options PathOptions BlowEdgePathOptions curvature BorderRadius Offset StepPosition SimpleBezier Clone", typeof(EdgePathOptionsExample)),
        new("edge-markers", "Edge Markers", EdgesGroup, "Per-edge arrowheads: open or closed, in any colour and size.",
            "markers arrowheads arrow ArrowClosed MarkerColor MarkerWidth MarkerStart MarkerEnd DefaultMarkerColor MarkerHeight", typeof(EdgeMarkersExample)),
        new("edge-label-styling", "Edge Label Styling", EdgesGroup, "Restyle, pad, round or remove the plate behind an edge label.",
            "edge label style plate LabelStyle LabelBgStyle LabelShowBg LabelBgPaddingX LabelBgBorderRadius LabelBgPaddingY BlowTextMetrics", typeof(EdgeLabelStylingExample)),
        new("self-loop-edges", "Self-Connections", EdgesGroup, "Edges back to their own node loop clear of it instead of collapsing onto it.",
            "self loop self-connection AllowSelfConnections LoopRadius recursion retry SourceHandle TargetHandle ConnectionRadius", typeof(SelfLoopEdgesExample)),

        // ---- Handles & Connections ----
        new("validation", "Validation", HandlesGroup, "Validate connections while dragging and reject invalid ones.",
            "validation IsValidConnection valid invalid handle connect reject OnConnect OnConnectEnd Rejection RejectedByRule bit-blow-handle-valid", typeof(ValidationExample)),
        new("custom-connection-line", "Custom Connection Line", HandlesGroup, "Restyle the line drawn while connecting, or draw it yourself with a template.",
            "connection line ConnectionLineTemplate ConnectionLineStyle ConnectionLineClass ConnectionLineType BlowConnectionLineContext BlowEdgePath gradient custom line dragging", typeof(CustomConnectionLineExample)),
        new("easy-connect", "Easy Connect", HandlesGroup, "Start a connection from anywhere on a node.",
            "easy connect whole node handle loose ConnectionMode floating edges GetFloatingEdgeParams ConnectionLineType straight", typeof(EasyConnectExample)),
        new("connection-mode", "Connection Mode", HandlesGroup, "Loose vs strict connection modes.",
            "connection mode strict loose ConnectionMode source target SameHandleType OnConnectEnd Rejection", typeof(ConnectionModeExample)),
        new("add-node-on-edge-drop", "Add Node on Edge Drop", HandlesGroup, "Drop a connection on the pane to spawn a connected node.",
            "add node drop connection pane OnConnectEnd BlowConnectEndInfo IsDropped IsReconnect Rejection FlowPosition", typeof(AddNodeOnEdgeDropExample)),
        new("cancel-connection", "Cancel Connection", HandlesGroup, "Cancel a connection with Escape or a click, and tell every way a gesture ends apart.",
            "cancel connection Escape OnConnectStart OnConnectEnd abort ConnectOnClick click to connect IsDropped Rejection BlowAriaLabels ConnectionRejected", typeof(CancelConnectionExample)),
        new("use-connection", "Use Connection", HandlesGroup, "Observe the in-progress connection state live.",
            "useConnection OnConnectionChange BlowConnectionLineContext live connection state IsValid Rejection FromHandle ToPosition", typeof(UseConnectionExample)),
        new("use-node-connections", "Use Node Connections", HandlesGroup, "List a node's connections, per handle, and its neighbours.",
            "node connections GetNodeConnections GetHandleConnections GetConnectedEdges GetIncomers GetOutgoers BlowGraphUtils neighbours handle ports", typeof(UseNodeConnectionsExample)),
        new("moving-handles", "Moving Handles", HandlesGroup, "Add and remove handles at runtime; edges follow their handles.",
            "moving handles add remove runtime UpdateNodeInternals remeasure anchors SourceHandle orphaned edge", typeof(MovingHandlesExample)),
        new("connection-rules", "Connection Rules", HandlesGroup, "Duplicate, self, cycle and non-connectable rules with their reasons, from the canvas and as a pure function.",
            "connection rules PreventDuplicateEdges AllowSelfConnections PreventCycles Connectable ConnectionRadius CheckConnection DescribeConnection CanStartConnection CountConnections BlowConnectionRules Check BlowConnectionPolicy BlowConnectionRejection", typeof(ConnectionRulesExample)),
        new("handle-rules", "Handle Rules", HandlesGroup, "Validation, data types, limits and connectability set on the handle itself.",
            "handle rules per-handle IsValidConnection OnConnect DataType MaxConnections IsConnectable IsConnectableStart IsConnectableEnd IsHandleAtLimit CountConnections", typeof(HandleRulesExample)),
        new("typed-ports", "Typed Ports", HandlesGroup, "Declared ports with data types, limits and required inputs, enforced while connecting and validated.",
            "typed ports BlowPort DataType MaxConnections MinConnections required input PreventCycles rejection reason BlowGraphValidator TopologicalSort dataflow ConnectionTypeCompatibility BlowValidationRules Data optional default ConnectionKey keyboard FindNodes FocusNodeAsync", typeof(TypedPortsExample)),
        new("confirm-connection", "Confirm Connection", HandlesGroup, "Confirm a finished connection asynchronously, as a server would, before the edge is made.",
            "confirm connection async OnBeforeConnect server approve deny latency reconnect OnConnectEnd Rejection RejectedByRule IsValidConnection Duplicate", typeof(ConfirmConnectionExample)),

        // ---- Interaction ----
        new("interaction", "Interaction", InteractionGroup, "Toggle dragging, connecting, selecting, panning and zooming options live.",
            "interaction NodesDraggable NodesConnectable ElementsSelectable PanOnDrag PanButtons ZoomOnScroll PanOnScroll PanOnScrollMode PanOnScrollSpeed ZoomOnDoubleClick PreventScrolling middle right mouse button", typeof(InteractionExample)),
        new("drag-n-drop", "Drag and Drop", InteractionGroup, "Drag new nodes from a sidebar onto the canvas, or click to add them.",
            "drag and drop sidebar palette ScreenToFlowPosition FlowToScreenPosition AddNodes PaneSize ondrop click to add new nodes", typeof(DragNDropExample)),
        new("use-key-press", "Use Key Press", InteractionGroup, "Bind keyboard shortcuts of your own around the flow.",
            "key press keyboard shortcut onkeydown onkeyup held keys DeleteKeyEnabled MoveSelectionAsync DeleteElementsAsync AddNodes", typeof(UseKeyPressExample)),
        new("intersection", "Intersection", InteractionGroup, "Detect which nodes a dragged node overlaps, and which lie in an area.",
            "intersection overlap GetIntersectingNodes IsNodeIntersecting BlowRect partially collision drop zone OnNodeDrag BlowViewportPortal", typeof(IntersectionExample)),
        new("click-distance", "Click Distance", InteractionGroup, "Distinguish a click from a drag using a distance threshold.",
            "click distance NodeClickDistance PaneClickDistance NodeDragThreshold OnNodeClick OnPaneClick threshold drag", typeof(ClickDistanceExample)),
        new("use-on-selection-change", "Selection Change", InteractionGroup, "React to selection changes, and change the selection from code.",
            "selection change OnSelectionChange SelectAsync additive Selected selected nodes edges box select SelectionOnDrag", typeof(UseOnSelectionChangeExample)),
        new("delete-protection", "Delete Protection", InteractionGroup, "Deletable/Selectable flags, select-all and the delete veto.",
            "delete protection Deletable Selectable locked read-only OnBeforeDelete veto OnNodesDelete OnEdgesDelete OnDelete DeleteElementsAsync SelectAllAsync SelectAllKeyEnabled select all cascade", typeof(DeleteProtectionExample)),
        new("auto-pan", "Auto Pan & Pinch", InteractionGroup, "Drag past the canvas edge to scroll, tab to an off-screen node to bring it into view; pinch with two fingers to zoom.",
            "auto pan AutoPanOnNodeDrag AutoPanOnConnect AutoPanSpeed AutoPanMargin AutoPanOnNodeFocus AutoPanOnNodeFocusDuration OnMove pinch ZoomOnPinch touch", typeof(AutoPanExample)),
        new("interaction-guards", "Interaction Guards", InteractionGroup, "nodrag, nopan and nowheel: carve interactivity back out of the canvas.",
            "nodrag nopan nowheel guards NoDragClass NoPanClass NoWheelClass rename disable slider scroll inside node overlay", typeof(InteractionGuardsExample)),
        new("key-bindings", "Key Bindings", InteractionGroup, "Choose which keys delete, zoom, multi-select, connect and select all.",
            "key bindings DeleteKeys DeleteKeyEnabled ZoomActivationKey MultiSelectionKey ConnectionKey SelectAllKeyEnabled Backspace modifier", typeof(KeyBindingsExample)),
        new("alignment-guides", "Alignment Guides", InteractionGroup, "Dragged nodes click into line with their neighbours.",
            "alignment guides snap SnapToGuides GuideDistance GuideColor SnapToGrid BlowAlignment", typeof(AlignmentGuidesExample)),
        new("selection-gestures", "Selection Gestures", InteractionGroup, "Box selection keys and modes, and dragging and right-clicking a whole selection.",
            "selection box drag OnSelectionStart OnSelectionEnd OnSelectionDragStart OnSelectionDrag OnSelectionDragStop OnSelectionContextMenu SelectionKey MultiSelectionKey SelectionOnDrag PanActivationKey SelectionMode Partial Full SelectEdgesOnSelectionBox", typeof(SelectionGesturesExample)),
        new("event-reference", "Event Reference", InteractionGroup, "Every event the canvas raises, logged live with its payload.",
            "events event reference log callbacks OnNodeClick OnNodeMouseEnter OnNodeDragStart OnEdgeClick OnPaneClick OnPaneScroll OnMoveStart OnMove OnMoveEnd OnInit OnNodesInitialized OnSelectionChange OnSelectionDragStart OnConnect OnConnectStart OnConnectEnd OnReconnect OnNodeResize OnDelete NodesChanged EdgesChanged OnGraphChanged OnHistoryChanged OnPaste OnError", typeof(EventReferenceExample)),

        // ---- Viewport & Layout ----
        new("backgrounds", "Backgrounds", ViewportGroup, "Dots, lines and cross patterns with every option live, and two backgrounds stacked into a grid.",
            "background pattern BlowBackground dots lines cross Gap GapX GapY Size LineWidth OffsetX OffsetY Color BgColor PatternClass stacked grid", typeof(BackgroundsExample)),
        new("controlled-viewport", "Controlled Viewport", ViewportGroup, "Drive the viewport from your own state or the canvas's methods, within a zoom range and a translate extent.",
            "controlled viewport Viewport OnViewportChanged OnMove BlowViewport SetViewport GetViewport GetZoom SetCenter FitView PaneSize MinZoom MaxZoom TranslateExtent clamp pan zoom state", typeof(ControlledViewportExample)),
        new("color-mode", "Color Mode", ViewportGroup, "Light, dark and system colour modes, a palette of your own from the --bit-blow-* properties, and reduced motion.",
            "color mode colour theme dark light system ColorMode BlowColorMode custom properties --bit-blow palette accent ReducedMotion BlowReducedMotion forced colors high contrast", typeof(ColorModeExample)),
        new("interactive-minimap", "Interactive MiniMap", ViewportGroup, "Steer the view from the minimap by pointer, wheel or keyboard, and pick nodes on it.",
            "minimap BlowMiniMap Pannable Zoomable ZoomStep KeyboardPanStep OnNodeClick AriaLabel RoleDescription KeyboardInstructions keyboard navigate", typeof(InteractiveMinimapExample)),
        new("custom-minimap-node", "Custom MiniMap Node", ViewportGroup, "Render minimap nodes with custom colours and shapes.",
            "minimap NodeColorFunc NodeStrokeColorFunc NodeStrokeWidth BorderRadius NodeTemplate NodeClassFunc svg shapes colours", typeof(CustomMiniMapNodeExample)),
        new("edge-routing", "Edge Routing", EdgesGroup, "Edges routed round the nodes in their way, re-routed as you drag, and the graph exported as SVG.",
            "routing route orthogonal obstacle avoid BlowEdgeType.Routed BlowEdgeRouting BlowRoutingOptions IBlowEdgeRouter custom router curve margin ToSvg export svg image BendPenalty StubLength MaxObstacles BorderRadius BlowSvgExportOptions NodeFill EdgeStroke TextColor FontFamily GroupFill group", typeof(EdgeRoutingExample)),
        new("workflow-runner", "Workflow Runner", StateGroup, "A pipeline validated, then run step by step with each node's state shown on it.",
            "workflow run execute pipeline RunStates BlowRunState BlowRunStatus running succeeded failed skipped topological validation announce BlowGraphValidator Validation ToSvg", typeof(WorkflowRunnerExample)),
        new("state-machine", "State Machine", StateGroup, "States and transitions drawn as a graph and driven by events, with routed edges and a self-loop.",
            "state machine transitions events current state routed self-loop RunStates DefaultEdgeOptions OnConnect Announce", typeof(StateMachineExample)),
        new("layouts-and-collapsing", "Layouts & Collapsing", ViewportGroup, "Tree, radial and force layouts tuned live and applied to the graph or the selection, and a group that collapses into itself.",
            "layout tree radial force LayoutAsync IBlowLayout selection collapse expand SetCollapsedAsync group hidden LevelGap RingGap StartAngle IdealEdgeLength Iterations IsPinned StartFromCurrentPositions undo", typeof(LayoutsAndCollapsingExample)),
        new("layouting", "Layouting", ViewportGroup, "Layered, grid and circle auto-layouts from the built-in BlowLayout, every option live.",
            "layout auto-layout BlowLayout Layered Grid Circle dagre elk direction LayerGap NodeGap CrossingReductionPasses AlignHandlesToDirection Columns ColumnGap RowGap Radius StartAngle", typeof(LayoutingExample)),
        new("align-distribute", "Align & Distribute", ViewportGroup, "Line up and evenly space the selected nodes, the way a drawing tool does.",
            "align distribute pack gap AlignSelectionAsync DistributeSelectionAsync BlowAlignEdge BlowAlign SelectAllAsync undo tidy", typeof(AlignDistributeExample)),
        new("viewport-transitions", "Viewport Transitions", ViewportGroup, "Ease the viewport between two parts of a graph instead of cutting.",
            "viewport transition animate SetCenterAsync FitViewAsync ZoomToAsync duration OnMoveStart OnMoveEnd FitBoundsAsync SetViewportAsync GetViewport OnMove ReducedMotion", typeof(ViewportTransitionsExample)),
        new("zindex-mode", "Z-Index Mode", ViewportGroup, "Control stacking of nodes and edges: modes, elevation on select and list order.",
            "z-index stacking order ZIndexMode ZIndex auto basic manual ElevateNodesOnSelect ElevateEdgesOnSelect BringToFrontAsync SendToBackAsync", typeof(ZIndexModeExample)),
        new("devtools", "DevTools", ViewportGroup, "Inspect viewport, nodes and change events.",
            "devtools inspector viewport logger change log debug OnGraphChanged BlowGraphChange OnMove OnNodeDrag OnSelectionChange Measured AddNodes", typeof(DevToolsExample)),
        new("overlay-layout", "Overlay Layout", ViewportGroup, "Anchor controls, minimap, panels and attribution; tailor the control bar and lock the canvas.",
            "overlay controls position minimap panel attribution lock ShowInteractive orientation ShowZoom ShowFitView FitViewOptions OnZoomIn OnZoomOut OnFitView InteractiveChanged LockLabel UnlockLabel BlowControlButton HideAttribution AttributionPosition BlowPanel Anchor", typeof(OverlayLayoutExample)),
        new("responsive-canvas", "Responsive Canvas", ViewportGroup, "Container and node resizes are tracked automatically; controlled viewport.",
            "responsive resize container ResizeObserver node grows measure Measured UpdateNodeInternals Viewport controlled", typeof(ResponsiveCanvasExample)),

        // ---- State & API ----
        new("use-reactflow", "Use Flow API", StateGroup, "Drive the canvas from your own buttons through its imperative API: zoom, fit, read the viewport, add, look up and delete elements, snapshot.",
            "useReactFlow imperative API @ref ZoomIn ZoomOut FitView GetZoom GetViewport ScreenToFlow PaneSize AddNodes AddEdges DeleteElementsAsync ToObject GetNode GetEdge SetCenterAsync", typeof(UseReactFlowExample)),
        new("provider", "Provider", StateGroup, "A sidebar outside the canvas reads the graph and drives the flow - no provider needed.",
            "provider ReactFlowProvider sidebar outside reference OnInit AddNodes AddEdges FitViewAsync FocusNodeAsync NodesChanged OnSelectionChange", typeof(ProviderExample)),
        new("save-restore", "Save & Restore", StateGroup, "Save the flow as versioned JSON to local storage or a file, load it back with every problem reported, and exchange it with React Flow.",
            "save restore persist local storage json file download upload ToJson LoadJsonAsync BlowSerializer BlowDocument metadata BlowImportResult BlowImportCodes BlowImportLimits BlowSerializationException IncludeSelection MapNodeData migration BlowMigration RestoreAsync BlowReactFlowFormat", typeof(SaveRestoreExample)),
        new("controlled-uncontrolled", "Controlled vs Uncontrolled", StateGroup, "Compare controlled and uncontrolled node state.",
            "controlled uncontrolled NodesChanged EdgesChanged state ToObject bind-Nodes", typeof(ControlledUncontrolledExample)),
        new("multi-set-nodes", "Multi setNodes", StateGroup, "Several changes to the nodes in one handler, drawn once; Refresh for changes made outside a render.",
            "setNodes multiple updates one render batch shuffle Refresh timer PeriodicTimer", typeof(MultiSetNodesExample)),
        new("set-nodes-batching", "setNodes Batching", StateGroup, "Add many nodes as one change - AddNodes with an array, or EditAsync - and compare the events and undo steps with adding them one by one.",
            "batch AddNodes EditAsync many nodes single update NodesChanged undo HistoryEnabled OnlyRenderVisibleElements", typeof(SetNodesBatchingExample)),
        new("use-nodes-data", "Use Nodes Data", StateGroup, "Nodes compute their value from the data of the nodes connected into them.",
            "useNodesData useNodeConnections nodes data Data derived computed GetIncomers source slider", typeof(UseNodesDataExample)),
        new("use-update-node-internals", "Update Node Internals", StateGroup, "Re-measure a node after its handles change.",
            "useUpdateNodeInternals UpdateNodeInternals Refresh remeasure handles measure css", typeof(UseUpdateNodeInternalsExample)),
        new("multi-flows", "Multi Flows", StateGroup, "Two independent canvases on one page, and a selection copied from one to the other.",
            "multiple flows two canvases independent instances copy paste PasteAsync BlowFragment clipboard AriaLabel", typeof(MultiFlowsExample)),
        new("redux", "External Store", StateGroup, "Drive a flow from a shared external store.",
            "redux zustand store external state dispatch action subscribe OnGraphChanged", typeof(ReduxExample)),
        new("diagnostics", "Diagnostics", StateGroup, "OnError reports a malformed graph instead of throwing on it.",
            "diagnostics OnError BlowError duplicate id dangling edge orphan BlowErrorCode duplicate-edge-id parent-cycle duplicate-handle-id missing-parent-node hidden", typeof(DiagnosticsExample)),
        new("graph-analysis", "Graph Analysis", StateGroup, "Order, cycles, components, reachability, shortest paths, validation and search, run over a graph you edit and marked on it.",
            "graph algorithms analysis BlowGraphAlgorithms BlowGraphIndex TopologicalSort HasCycle FindCycles IsAcyclic StronglyConnectedComponents ConnectedComponents GetAncestors GetDescendants IsReachable Traverse ShortestPath Dijkstra GetRoots GetLeaves GetIsolatedNodes GetUnreachable GetStatistics BlowGraphValidator BlowValidationRules NoIsolatedNodes IBlowValidationRule MaxIssues severity Validation search FindNodes FindEdges BlowGraphSearch FocusNodeAsync FocusEdgeAsync SelectAsync", typeof(GraphAnalysisExample)),
        new("undo-redo", "Undo & Redo", StateGroup, "Snapshot-based history you drive from your own change events.",
            "undo redo BlowHistory Capture snapshot history Rebase Capacity Clear UndoLabel RedoLabel OnGraphChanged RestoreAsync remote", typeof(UndoRedoExample)),
        new("built-in-undo-redo", "Built-in Undo & Redo", StateGroup, "Let the canvas keep the undo stack and bind the shortcuts itself.",
            "undo redo HistoryEnabled HistoryCapacity OnHistoryChanged UndoAsync RedoAsync Ctrl+Z CaptureHistory BlowHistoryState UndoLabel", typeof(BuiltInUndoRedoExample)),
        new("grouping", "Grouping & Transactions", StateGroup, "Group and ungroup the selection, reorder it, batch edits into one undo step and log every graph change.",
            "group ungroup GroupSelectionAsync UngroupAsync BringToFrontAsync SendToBackAsync EditAsync transaction batch OnGraphChanged BlowGraphChange origin remote UndoLabel z-order", typeof(GroupingExample)),
        new("copy-paste", "Copy & Paste", StateGroup, "Copy, cut, paste and duplicate a selection, groups and all.",
            "copy paste cut duplicate clipboard ClipboardShortcutsEnabled BlowFragment Ctrl+C Ctrl+V PasteOffset BlowClipboard BlowPasteOptions IdFactory ExistingIds CutAsync PasteAsync DuplicateSelectionAsync OnPaste", typeof(CopyPasteExample)),

        // ---- Add-ons ----
        new("node-resizer", "Node Resizer", AddonGroup, "Resize nodes with BlowNodeResizer: when its controls show, their colour, the side strips, size limits and the resize events.",
            "resize resizer BlowNodeResizer MinWidth MinHeight MaxWidth MaxHeight KeepAspectRatio Visible Color ShowEdgeControls OnNodeResizeStart OnNodeResize OnNodeResizeEnd", typeof(NodeResizerExample)),
        new("node-toolbar", "Node Toolbar", AddonGroup, "A toolbar anchored to a node at a constant size: its side, alignment, gap and when it shows.",
            "node toolbar BlowNodeToolbar selected Position Align BlowToolbarAlign Offset Visible actions", typeof(NodeToolbarExample)),
        new("touch-device", "Touch Device", AddonGroup, "Pointer-based interactions that work on touch.",
            "touch mobile pointer events tablet SelectionOnDrag ConnectOnClick tap ZoomOnPinch pinch", typeof(TouchDeviceExample)),
        new("a11y", "Accessibility", AddonGroup, "Everything without a pointer: Tab and arrow keys, connecting and reconnecting from the keyboard, single-click move buttons.",
            "accessibility a11y keyboard focus arrow keys AriaLabel screen reader ConnectionKey connect reconnect ConnectOnClick MoveSelectionAsync BlowNodeToolbar WCAG 2.5.7", typeof(A11yExample)),
        new("keyboard-a11y", "Keyboard Accessibility", AddonGroup, "The tab order and keyboard support switched on and off, panning to the focused node, and announcements of your own.",
            "keyboard accessibility tab NodesFocusable EdgesFocusable DisableKeyboardA11y AutoPanOnNodeFocus AutoPanOnNodeFocusDuration Announce aria-live Enter Space Escape DefaultViewport", typeof(KeyboardA11yExample)),
        new("localization", "Localization", AddonGroup, "Translate every label and announcement the canvas makes.",
            "localization translate i18n AriaLabels BlowAriaLabels language DefaultViewport LockLabel UnlockLabel ZoomInLabel BlowMiniMap RoleDescription KeyboardInstructions rtl dir", typeof(LocalizationExample)),
    ];

    public static IEnumerable<IGrouping<string, ExampleInfo>> Grouped() =>
        All.GroupBy(e => e.Group);
}
