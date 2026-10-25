# Blow improvement programme: requirements and progress record

This file is the working record of the effort to make Blow a complete, production-grade foundation for node-based
UIs in Blazor: the audit, the requirements inventory with its gap analysis, the decisions taken, and the evidence
behind every status. It is for maintainers and for agents continuing the work - the README is the user guide.

## Baseline (start of round 1, 2026-10-24)

- Branch `develop` at `e04a003ff4`.
- `dotnet build src/Utilities/Blow/Bit.Blow.slnx`: succeeds, one pre-existing nullable warning
  (`Bit.Blow.Tests.Mcp/McpSurfaceTests.cs:58`, CS8602).
- `Bit.Blow.Tests` (net10.0): 565 / 565 passing. `Bit.Blow.Tests.Mcp`: 88 / 88. `Bit.Blow.Tests.E2E` (Playwright,
  Chromium, Debug): 135 / 135 in 5 min 19 s.
- Library: about 11.3k lines - C# (net8/9/10, trimmable), a 420-line TypeScript module, a 640-line stylesheet. No
  runtime dependency beyond `Microsoft.AspNetCore.Components.Web`. 84 demo examples, an MCP server in the demo host.
- No benchmark project existed; `Tests/Bit.Blow.Benchmarks` was added in round 1 to record the baseline below.

## Assessment

Blow is already a careful, well-documented port of React Flow's model and interaction set: a mutable
`List<BlowNode>` / `List<BlowEdge>` graph edited in place, a 4.6k-line `BlowCanvas` owning every gesture, measured
handles, five edge path types with a geometry cache, sub-flows with extents, minimap/controls/background, layered/
grid/circle layouts, align/distribute, alignment guides, snapshot undo/redo, an in-memory clipboard, keyboard
selection and movement with aria-live announcements, `OnError` diagnostics and viewport culling. Interaction
correctness has clearly been reviewed hard (cancellation restores state, every start event has its end, nested
flows isolate their input, Blazor Server races are armed before awaits).

The architecture is sound and is preserved. The gaps are in the layers above the canvas that real applications
need and currently write themselves:

1. **No domain-level port model.** Handles exist only as rendered markup; there is no declared port with a data
   type, a connection limit or a "required" flag, so typed connections, cardinality and "this input must be wired"
   validation cannot be expressed or checked without the DOM.
2. **Connection validation is a boolean.** No reason is given for a rejection, no cycle prevention, no limits, and
   the rules live inside the component rather than in a testable, UI-free function.
3. **No graph algorithms or validation framework** beyond incomers/outgoers: no cycle detection, topological order,
   reachability, components, path finding or structured validation results.
4. **No serialization.** The README asks every app to write its own DTOs; there is no versioned schema, import
   validation, size limits, migration or round-tripping of custom data.
5. **Editing infrastructure** is thin above the gestures: no transactions or batched programmatic edits, no typed
   change notifications (only "the list changed"), no group/ungroup, no z-order commands, history steps without
   labels, and a history that deep-copies the whole graph per step.
6. **Accessibility** is good for selection and movement but stops there: no usage instructions for screen readers,
   no keyboard way to create or reconnect a connection, no keyboard minimap, handle hit areas that shrink to a
   pixel or two when zoomed out, no reduced-motion or forced-colours support, control labels not localizable.
7. **Performance** has never been measured. Every canvas render re-renders every node, edge and handle component
   (the cascaded context is not fixed and node/edge renderers do not implement `ShouldRender`), so a pan of a large
   graph costs a full component render per frame.

## Research summary

Sources: the official documentation, source and release notes of React Flow 12.12 (Sep 2026), Rete.js v2, Drawflow,
Svelvet 11, LiteGraph.js (and its ComfyUI fork), JointJS core and JointJS+, ELK and Graphviz, plus GoJS and tldraw
where they had the best documented answer. What it changed in this plan:

- **Typed ports and connection limits.** Rete validates through socket compatibility, LiteGraph through slot types
  with a `*` wildcard and comma-separated multi-types; React Flow's core `isConnectable` is boolean and connection
  limits exist only as an example. Blow adopts LiteGraph's type grammar (`*`, lists, case-insensitive) for its
  default rule and makes limits a first-class port property.
- **Keyboard connection creation is undocumented everywhere** except GoJS (focus separate from selection,
  following links with Ctrl+arrows, linking from the keyboard with a virtual pointer). That is the model for
  A11Y-07, rather than anything in React Flow.
- **Versioned serialization.** React Flow, JointJS, Drawflow and Rete have no version field; LiteGraph writes one
  but never reads it; ComfyUI publishes a JSON Schema per version; tldraw keeps an ordered migration sequence that
  runs on load. SER-01..04 follow tldraw's approach.
- **Routing and layout.** JointJS's manhattan/metro routers (MPL-2.0) and ELK (EPL-2.0) are copyleft; MSAGL is MIT and
  C#. Blow's own router and layouts are written from the published algorithms (Sugiyama layering, orthogonal
  visibility routing), and the layout abstraction (ALG-04) leaves room for an MSAGL or ELK adapter outside the core.
- **History.** Rete merges actions inside a short window; JointJS batches nested commands and drops no-ops. Blow
  already drops no-op captures; labelled steps and batching (EDT-03, HIS-02) cover the rest.

## Decisions

| Decision | Rationale |
| --- | --- |
| Keep the mutable, in-place model (`List<BlowNode>`, `List<BlowEdge>`) and the React Flow vocabulary. | It is the library's identity and what every demo and user relies on. New capabilities are layered on top as pure helpers plus thin canvas integration, never as a parallel model. |
| Pure, UI-free helpers for everything that is not interaction (algorithms, validation, connection rules, serialization). | Testable without bUnit, usable on a server, and the canvas becomes a client of the same code an app can call. |
| No new runtime dependencies. | Every feature planned here is implementable in C# with System.Text.Json (in the shared framework). Layout engines such as ELK stay possible through the pluggable layout abstraction rather than shipped. |
| Ports are declared on the node (`BlowNode.Ports`), optional, and `BlowHandle` reads its definition from there. | Gives typed connections, limits and required-connection validation a model that works headless, while every existing template that places `BlowHandle`s by hand keeps working unchanged. |
| `BlowHandle` may override a port's `DataType` and `MaxConnections`. | Templates that would rather say it in markup can, as React Flow's custom-handle examples do; the model stays the only thing headless validation needs. |
| Rejections are an ordered enum, first failing check wins. | Deterministic, testable, localizable reasons; the application's rule only sees otherwise-valid connections. |
| `OnBeforeConnect` re-validates after its await against a fresh session. | The graph can change during the await (other edits, collaborators); applying a stale verdict could create duplicates or over-full ports. |
| Edge and handle hit areas are sized in screen pixels through a `--bit-blow-zoom` custom property. | Constant usability at any zoom without re-rendering every edge on every zoom step. |
| The render benchmarks host the canvas in a headless `Renderer`, not bUnit. | bUnit regenerates the HTML of every parent of each rendered component - quadratic in nodes - and measured itself rather than Blow (a 2.3 s "drag frame" that was 1.2 ms in fact). |
| Render scoping by version numbers cascaded under names, plus a gate per layer, rather than immutable node records. | Keeps the mutable model (and every app) as it is while a pan renders nothing in the node and edge layers. |
| Serialization by hand over `Utf8JsonWriter`/`JsonDocument`, with data mapped per node/edge type to source-generated `JsonTypeInfo`. | Trim and AOT safe, deterministic, bounded while parsing, and no type names in documents - so a document cannot pick what is instantiated. |
| History shares unchanged element copies between steps, keyed by id, and compares restored-but-not-edited state (selection, measurement) before sharing. | Memory proportional to the change; a shared copy must restore exactly what a full copy would have. |
| Edits that must not be undone (remote) are written into every recorded step (`Rebase`) rather than kept out of the history. | Keeping them out made the next local step undo them; operational transforms are out of scope, so "the remote edit wins in every step" is the predictable rule. |
| An edit is an async-local scope plus a set of open edits, not a counter. | Only the edit's own code may be folded in: a counter swallowed a key press or a server push handled while an async edit waited. |
| Element state for screen readers goes in a hidden element referenced first by `aria-describedby`, not in `aria-description`. | `aria-description` is ignored when `aria-describedby` is present, and the instructions already use `aria-describedby`; `aria-invalid` is not allowed on `role="group"`. |
| Connections from the keyboard are aimed by focus (Tab), not by a separate list or dialog. | Reuses the tab order and the screen reader's own reading of each node; every connection rule and event is the drag's. A keyboard connection is not a pointer gesture, so the mouse cannot steer or end it by accident. |
| Validation and run states are shown by shape as well as colour (badges with distinct glyphs and shapes, dash patterns), and in the description. | WCAG 1.4.1 (use of colour) and 1.4.11 (non-text contrast); forced colours keep the shapes. |
| Layouts are options classes that implement `IBlowLayout`; the canvas lays out one parent's children at a time and lifts child edges to their group. | One extension point for built-in layouts, an app's own, or an external engine; positions are parent-relative, so mixing depths would be meaningless. |
| Routing is an A* over the sparse grid of obstacle margin lines, re-run on geometry changes only and only against nodes near the edge. | Deterministic, dependency-free and fast enough per edge (tens of microseconds); pan and zoom never re-route. |
| SVG export writes text escaped and colours only from an allow-list of plain colour values. | Graphs come from untrusted documents; the image must be safe to inline. |

## Requirements inventory

Status: **Missing**, **Planned** (scheduled for a round), **In progress**, **Implemented** (code exists),
**Verified** (implemented and proven by executed tests), **Deferred** (with reason), **Partial** (part verified,
remainder stated). Priority: **P0** correctness/security/data integrity, **P1** essential production capability,
**P2** important advanced capability, **P3** specialised.

"Evidence" names the code and the tests that prove the status. Baseline rows that were already in place are marked
Verified with the existing tests that cover them.

### MOD - graph document and model

| ID | Capability (problem solved) | P | Status | Current / evidence | Gap and approach | Acceptance and verification |
| --- | --- | --- | --- | --- | --- | --- |
| MOD-01 | Nodes, edges, handles with stable ids, positions, sizes, data | P0 | Verified | `BlowNode`, `BlowEdge`, `BlowHandleBounds`; BlowCanvasTests, StateTests | - | - |
| MOD-02 | Invalid states reported, never thrown (duplicate ids, dangling edges, missing parents, duplicate handles, parent cycles) | P0 | Verified | `OnError`, `BlowErrorCode`; CanvasApiTests, ReviewRegressionTests; R2: `parent-cycle` (TypedConnectionTests.AParentCycle..., ANodeThatIsItsOwnParent...) | Was: a parent cycle summed offsets fifty times and flung its nodes off screen, unreported (the MCP oracle test documented it). Fixed: cut and reported. | Done |
| MOD-03 | Declared ports: id, side, data type, max connections, required, label | P1 | Verified (R2) | Handles exist only as markup | `BlowPort` + `BlowNode.Ports`; `BlowHandle` and `BlowDefaultNode` read/render them | Headless tests over the model; bUnit test that a default node renders declared ports |
| MOD-04 | Graph-level metadata and document id | P2 | Verified (R4) | `BlowDocument.Id`, `Version`, `Metadata` (`JsonObject`); SerializerTests.ADocumentSurvivesARoundTripLosslessly | - | Done |
| MOD-05 | Efficient lookup by id and adjacency | P1 | Verified (R2) | Canvas keeps id indexes; helpers scan lists | `BlowGraphIndex` (id + in/out adjacency) used by algorithms and validation | Benchmarks + unit tests |
| MOD-06 | Separation of persistent, transient and runtime state | P1 | Verified (R4) | Transient state stays on the node (`Dragging`, `Measured`, internal setters); the serializer writes persistent state only, selection on request (SerializerTests.SelectionAndMeasuredStateAreNotSavedUnlessAskedFor); README "Saving to JSON" | - | Done |
| MOD-07 | Multigraphs, self-loops, cycles, disconnected graphs | P1 | Verified | `PreventDuplicateEdges` opt-out, `AllowSelfConnections`, loops drawn; SelfLoopTests | - | - |
| MOD-08 | Locked / read-only elements | P1 | Verified | `Draggable`, `Selectable`, `Connectable`, `Deletable`, canvas-wide flags; InteractionTests | - | - |
| MOD-09 | Z-order and layering | P1 | Verified (R5) | `ZIndex`, `ZIndexMode`, elevate on select; `BringToFrontAsync`/`SendToBackAsync` move nodes with their descendants (EditingTests.BringingToFrontAndSendingToBack...; E2E via the grouping demo) | - | Done |

### NOD - nodes and rendering

| ID | Capability | P | Status | Current / evidence | Gap and approach | Acceptance and verification |
| --- | --- | --- | --- | --- | --- | --- |
| NOD-01 | Built-in and custom node templates | P0 | Verified | `NodeTemplate`, `BlowDefaultNode`; PresentationTests | - | - |
| NOD-02 | Resizable nodes, toolbars, drag handles | P1 | Verified | `BlowNodeResizer`, `BlowNodeToolbar`, `BlowDragHandle`; E2E GestureTests | - | - |
| NOD-03 | Embedded controls do not start drags/pans | P0 | Verified | `nodrag`/`nopan`/`nowheel` guards; E2E GuardTests | - | - |
| NOD-04 | Validation/status state on nodes (invalid, warning, running, done) with a non-colour indicator | P1 | Verified (R6, R8) | Validation: `Validation` parameter (ring + shaped badge, dashed edges, port rings; ValidationMarksAndFocusTests; E2E KeyboardAccessTests). Run states: `RunStates` (glyph badges, running ring; RoutingExportAndRunTests; E2E RunAndRoutingTests). `aria-invalid` is not allowed on `role="group"`, so states are in the description | - | Done |
| NOD-05 | Collapsible groups with boundary connections | P2 | Verified (R7) | `BlowNode.Collapsed`, `SetCollapsedAsync`: descendants hidden and deselected, boundary edges drawn to the collapsed node, inner edges hidden, nested collapse kept on expand, one undo step, announced, serialized (MoreLayoutTests) | Size is left to the app | Done |

### CON - edges and connections

| ID | Capability | P | Status | Current / evidence | Gap and approach | Acceptance and verification |
| --- | --- | --- | --- | --- | --- | --- |
| CON-01 | Drag/click to connect, previews, snapping, reconnection | P0 | Verified | E2E GestureTests, InteractionTests | - | - |
| CON-02 | Centralized, UI-free connection rules | P1 | Verified (R2) | Rules live in `BlowCanvas.IsValidConnectionTarget` | `BlowConnectionRules.Check` used by the canvas | Unit tests per rule without bUnit |
| CON-03 | Rejection reasons and feedback | P1 | Verified (R2) | Boolean only | `BlowConnectionCheck` with a `BlowConnectionRejection` code; announced; on the connection-line context | Unit + bUnit tests |
| CON-04 | Typed port compatibility | P1 | Verified (R2) | Only via a custom predicate | `BlowPort.DataType` + `ConnectionTypeCompatibility` | Unit tests |
| CON-05 | Cardinality limits per handle | P1 | Verified (R2) | None | `BlowPort.MaxConnections` / `BlowHandle.MaxConnections` | Unit + bUnit tests |
| CON-06 | Cycle prevention | P1 | Verified (R2) | None | `PreventCycles`; ancestors precomputed at gesture start | Unit + bUnit tests |
| CON-07 | Async confirmation of a connection with stale-result protection | P2 | Verified (R2) | None | `OnBeforeConnect` (async veto) re-validated after the await | bUnit tests incl. a graph change during the await |
| CON-08 | Edge hit areas usable at any zoom | P1 | Verified (R1) | Interaction width is in flow units: 14 px at zoom 1, 2.8 px at 0.2 | Width in screen pixels via a zoom custom property | bUnit test of the style; E2E click at low zoom |
| CON-09 | Obstacle-avoiding orthogonal routing | P2 | Verified (R8) | `BlowEdgeRouting.Orthogonal/Route` (sparse-grid A* with bend cost), `BlowEdgeType.Routed`, `Routing` parameter; re-routed on geometry changes only (RoutingExportAndRunTests; E2E RunAndRoutingTests; RoutingBenchmarks) | - | Done |
| CON-10 | Edge path types, markers, labels, animation | P1 | Verified | EdgePathTests, CanvasApiTests | - | - |

### CAN - canvas and viewport

| ID | Capability | P | Status | Current / evidence | Gap and approach | Acceptance and verification |
| --- | --- | --- | --- | --- | --- | --- |
| CAN-01 | Pan, zoom, pinch, fit view, extents, animated transitions | P0 | Verified | ViewportTests, E2E GestureTests | - | - |
| CAN-02 | Coordinate transforms screen/flow | P0 | Verified | `BlowViewport`, `ScreenToFlowPosition`; ViewportTests | - | - |
| CAN-03 | Grid snap, alignment guides, auto-pan | P1 | Verified | AlignmentGuides, auto-pan tests | - | - |
| CAN-04 | Minimap navigation | P1 | Verified (R6) | A pannable/zoomable minimap is focusable (`role="group"`, described): arrows pan, +/- zoom, Home fits; keys stop at the minimap (ValidationMarksAndFocusTests; E2E KeyboardAccessTests) | - | Done |
| CAN-05 | Centre on / focus a node from code | P1 | Verified (R6) | `FocusNodeAsync`, `FocusEdgeAsync` (centre, optional select and zoom, then DOM focus of the canvas's own element) (bUnit; E2E search test) | - | Done |

### EDT - editing

| ID | Capability | P | Status | Current / evidence | Gap and approach | Acceptance and verification |
| --- | --- | --- | --- | --- | --- | --- |
| EDT-01 | Selection (click, modifier, box, all), move, delete, cascade | P0 | Verified | InteractionTests, SelectionAndViewportTests | - | - |
| EDT-02 | Copy, cut, paste, duplicate with rewiring | P1 | Verified | HistoryAndClipboardTests | - | - |
| EDT-03 | Batched programmatic edits as one step (transactions) | P1 | Verified (R5) | `EditAsync(Action / Func<Task>, label, origin, recordHistory)`; nested canvas calls fold in (EditingTests.AnEditIsOneUndoStep...; E2E GroupingTests.A_batched_edit_is_one_undo_step) | - | Done |
| EDT-04 | Typed change notifications with origin | P1 | Verified (R5) | `OnGraphChanged(BlowGraphChange)`: kind, origin (User/Api/History/Remote), touched and removed elements, label; raised at every edit site (EditingTests; E2E GroupingTests) | - | Done |
| EDT-05 | Group / ungroup selection | P2 | Verified (R5) | `GroupSelectionAsync(group?, padding)`, `UngroupAsync(group)`; positions preserved, mixed parents refused (EditingTests; E2E GroupingTests) | - | Done |
| EDT-06 | Keyboard context menu | P1 | Verified (R6) | Shift+F10 on a focused node raises `OnNodeContextMenu` (E2E KeyboardAccessTests); documented in the keyboard map | - | Done |

### HIS - history

| ID | Capability | P | Status | Current / evidence | Gap and approach | Acceptance and verification |
| --- | --- | --- | --- | --- | --- | --- |
| HIS-01 | Undo/redo, capacity, one step per gesture | P0 | Verified | BuiltInHistoryTests | - | - |
| HIS-02 | Labelled steps for "Undo move" UI | P2 | Verified (R5) | `BlowHistory.UndoLabel`/`RedoLabel`, `BlowHistoryState.UndoLabel`/`RedoLabel`, labels at every capture site (EditingTests.HistoryLabelsReachTheHistoryState, UndoAndRedo...) | - | Done |
| HIS-03 | History memory proportional to the change, not the graph | P1 | Verified (R5) | `BlowHistory.Capture(nodes, edges, viewport, label)` shares unchanged clones: 5,000 nodes 7.70 ms / 3.3 MB -> 172 us / 91 KB per step (EditingTests.AHistoryStepSharesTheCopies..., SharingFindsUnchangedCopiesWhenTheListShifted) | - | Done |

### ALG - algorithms and layout

| ID | Capability | P | Status | Current / evidence | Gap and approach | Acceptance and verification |
| --- | --- | --- | --- | --- | --- | --- |
| ALG-01 | Layered, grid, circle layouts; align/distribute | P1 | Verified | LayoutTests, BlowAlignTests | - | - |
| ALG-02 | Cycle detection, topological order, SCCs | P1 | Verified (R2) | None | `BlowGraphAlgorithms` (iterative, O(V+E)) | Unit tests incl. 100k-node chain (no stack overflow) |
| ALG-03 | Reachability, ancestors/descendants, components, shortest path, roots/leaves/isolated | P1 | Verified (R2) | Incomers/outgoers only | Same class | Unit tests |
| ALG-04 | Tree, force-directed and radial layouts; pluggable layout abstraction | P2 | Verified (R7) | `BlowLayout.Tree/Radial/Force`, `IBlowLayout` implemented by every options class (MoreLayoutTests: no overlaps, determinism, 20k-deep chain, pinned nodes; LayoutBenchmarks) | - | Done |
| ALG-05 | Layout of the selection only | P2 | Verified (R7) | `LayoutAsync(layout, nodes?, keepPlace)`: selection or top level, one parent, kept in place, child edges lifted to their group, one undo step (MoreLayoutTests) | - | Done |

### SRC - search and inspection

| ID | Capability | P | Status | Current / evidence | Gap and approach | Acceptance and verification |
| --- | --- | --- | --- | --- | --- | --- |
| SRC-01 | Search nodes/edges by text and predicate, select results, navigate | P2 | Verified (R6) | `BlowGraphSearch.FindNodes/FindEdges` (ranked, case- and accent-insensitive, words, ports, own text, no patterns), canvas `FindNodes`/`FindEdges`/`SelectAsync`/`FocusNodeAsync` (GraphSearchTests; E2E search box in the typed-ports demo) | - | Done |
| SRC-02 | Graph statistics | P3 | Verified (R2) | - | `BlowGraphAlgorithms.Statistics` | Unit |

### SER - serialization and interchange

| ID | Capability | P | Status | Current / evidence | Gap and approach | Acceptance and verification |
| --- | --- | --- | --- | --- | --- | --- |
| SER-01 | Versioned, documented JSON format | P1 | Verified (R4) | `BlowSerializer` (format `bit-blow`, schema 1) over `Utf8JsonWriter`/`JsonDocument`, deterministic, culture-invariant; README "Saving to JSON" (SerializerTests round trip, determinism, culture) | No golden file: determinism is asserted on exact output instead | Done |
| SER-02 | Import validation with located errors | P1 | Verified (R4) | `BlowImportResult` / `BlowImportIssue` with JSON paths; broken elements skipped and located; graph problems as warnings (SerializerTests) | - | Done |
| SER-03 | Limits against oversized/hostile input | P0 | Verified (R4) | `BlowImportLimits` (bytes, nodes, edges, ports, depth, string length), checked before and while parsing; streams read no further than the limit (SerializerTests) | - | Done |
| SER-04 | Schema migration | P1 | Verified (R4) | `BlowMigration` chain keyed by application version; gaps, failures and newer documents reported (SerializerTests) | - | Done |
| SER-05 | Custom data round-trip without arbitrary type instantiation | P0 | Verified (R4) | Data stays `JsonNode` unless the app maps a node/edge type to a source-generated `JsonTypeInfo<T>`; no type names are read or written (SerializerTests.NothingInADocumentNamesAType, DataOf...) | - | Done |
| SER-07 | React Flow interchange | P2 | Verified (R4) | `BlowReactFlowFormat.Import/Export`; what has no counterpart is reported, not guessed (ReactFlowFormatTests) | - | Done |
| SER-06 | SVG export | P2 | Verified (R8) | `BlowSvgExport.ToSvg`, canvas `ToSvg`: escaped text, colour allow-list, measured anchors, routed edges (RoutingExportAndRunTests incl. injection attempts; E2E export link) | - | Done |

### VAL - validation and integration

| ID | Capability | P | Status | Current / evidence | Gap and approach | Acceptance and verification |
| --- | --- | --- | --- | --- | --- | --- |
| VAL-01 | Structured graph validation (rules, severities, element references) | P1 | Verified (R2) | `OnError` covers structural problems only | `BlowGraphValidator` with built-in and custom rules | Unit tests |
| VAL-02 | Required ports and min/max cardinality | P1 | Verified (R2) | - | Rules over `BlowPort` | Unit tests |
| VAL-03 | Error markers and navigation to invalid elements | P1 | Verified (R6) | Canvas `Validation` parameter marks nodes, edges and ports; problems in the accessible description; the demo's problem list focuses the node (ValidationMarksAndFocusTests; E2E) | - | Done |
| VAL-04 | Execution integration points (status, runtime adapters) | P2 | Verified (R8) | `BlowRunState`/`BlowRunStatus`, `RunStates`; demos `/examples/workflow-runner` (validate, run in topological order, failure skips dependants, cancel) and `/examples/state-machine` (E2E RunAndRoutingTests) | The engine is the app's by design | Done |

### A11Y - accessibility

| ID | Capability | P | Status | Current / evidence | Gap and approach | Acceptance and verification |
| --- | --- | --- | --- | --- | --- | --- |
| A11Y-01 | Tab to nodes/edges, Enter select, arrows move, Delete, Escape, announcements | P0 | Verified | AccessibilityTests, E2E KeyboardTests | - | - |
| A11Y-02 | Keyboard usage instructions for screen readers, localizable | P1 | Verified (R1) | None | Hidden descriptions referenced by `aria-describedby` from pane, nodes and edges | bUnit tests |
| A11Y-03 | Localizable control labels | P1 | Verified (R1) | Hard-coded English tooltips | Label parameters on `BlowControls` | bUnit tests |
| A11Y-04 | Reduced motion | P1 | Verified (R1) | Animated edges and transitions ignore the preference | CSS media query + `MotionPreference` parameter honoured by transitions | bUnit + theme tests |
| A11Y-05 | Forced colours (Windows high contrast) | P1 | Verified (R1, R9) | `@media (forced-colors: active)` rules for edges, handles, selection, focus, guides, minimap, validation and run badges; ThemeTests; E2E ForcedColorsAndPointerTests renders under emulated forced colours (selected outline, badges in CanvasText with their glyphs) | - | Done |
| A11Y-06 | Handle targets usable at any zoom (WCAG 2.5.8 spirit) | P1 | Verified (R1) | 8 px dot scales with zoom | Invisible hit area with a screen-pixel minimum | bUnit/theme tests; E2E connect at low zoom |
| A11Y-07 | Keyboard connection creation and reconnection | P1 | Partial (R6): creation verified (`ConnectionKey`: start, step ports, aim by focus, Enter, Escape, every rule and event as for a drag; KeyboardConnectionTests; E2E). Reconnecting an existing edge from the keyboard is not built: delete it and connect again | - | Keyboard reconnection | - |
| A11Y-08 | Node descriptions carry state (selected, connections, validation) | P2 | Verified (R6) | Hidden state element referenced first by `aria-describedby` (`aria-description` would be ignored beside `aria-describedby`); `NodeState`/`EdgeState` localizable; edges named by node names | - | Done |
| A11Y-09 | Single-pointer alternative to dragging (WCAG 2.5.7) | P1 | Verified (R9) | Connections: keyboard and `ConnectOnClick`. Moving: arrow keys and `MoveSelectionAsync(dx, dy)` behind buttons (EditingTests; E2E move buttons on the `/examples/a11y` toolbar) | - | Done |

### PERF - performance

| ID | Capability | P | Status | Current / evidence | Gap and approach | Acceptance and verification |
| --- | --- | --- | --- | --- | --- | --- |
| PERF-01 | Repeatable benchmarks | P1 | Verified (R1) | `Tests/Bit.Blow.Benchmarks` | - | Baseline recorded below |
| PERF-02 | A pan or zoom does not re-render nodes and edges | P1 | Verified (R3) | Every render re-renders every node, edge and handle | Fixed cascade + change notifications + `ShouldRender` | Render-count tests; benchmark before/after |
| PERF-03 | Culling of off-screen elements | P1 | Verified | `OnlyRenderVisibleElements` | - | - |
| PERF-05 | Level-of-detail rendering in templates without re-rendering every node per frame | P2 | Verified (R3) | `BlowViewportView` with `Select`; RenderScopeTests | - | - |
| PERF-06 | No per-element allocation per frame | P1 | Verified (R3) | Cached callbacks, shared renderer settings, cached marker keys: 1,000-node frame 7.3 MB -> ~100 KB | - | Probe + benchmarks |
| PERF-04 | Measurement cost on large graphs | P2 | Deferred | Every node is measured on every measurement pass, including after a drag, although moving a node changes neither its size nor its handle offsets | Measure only nodes whose size can have changed; deferred because it needs browser profiling to justify and touches how growing parents are re-measured | - |
| PERF-07 | The node and edge layers skip a frame that cannot have changed them | P1 | Verified (R4) | `BlowRenderGate` around each layer: pan/zoom frame at 1,000 nodes 1.7 ms -> 11 us (RenderScopeTests; benchmarks) | - | Done |

### LIFE, SEC, EXT, DOC

| ID | Capability | P | Status | Current / evidence | Gap and approach | Acceptance and verification |
| --- | --- | --- | --- | --- | --- | --- |
| LIFE-01 | Server/WASM/Hybrid, prerendering, disposal, multiple instances | P0 | Verified | README hosting notes; E2E multi-flow; interop teardown handling | - | - |
| SEC-01 | Untrusted labels and content rendered as text | P0 | Verified | Razor encodes; no `MarkupString` in the library | - | Grep in review |
| SEC-02 | Expensive algorithms bounded on hostile graphs | P1 | Verified (R4) | Algorithms iterative and linear (100k-deep test), validator report bounded by `MaxIssues`, import limits (SerializerTests) | - | Done |
| EXT-01 | Custom renderers, templates, per-handle rules | P1 | Verified | Templates, `BlowHandleRegistration` | - | - |
| EXT-02 | Pluggable layout and routing | P2 | Partial (R7, R8) | Layout pluggable through `IBlowLayout` (verified). Routing is a pure function an app can call from its own edge template; the canvas's routed type is not swappable | A routing interface on the canvas if asked for | - |
| DOC-01 | Requirements record, README, demos for each new capability | P1 | Verified (R9) | This file; README sections for every capability and an Upgrading table; package README; 6 new demos (typed ports, grouping, layouts and collapsing, edge routing, workflow runner, state machine) and 4 extended (save and restore, localization, viewport transitions, accessibility), all in the catalog and swept by `DemoPagesTests`; MCP reference generated from the code and README | - | Done |

## Round log

### Round 1 - baseline, research, accessibility and hit-area fixes

Implemented:

- `Tests/Bit.Blow.Benchmarks` (BenchmarkDotNet): render frames hosted in a headless Blazor `Renderer`, layout,
  edge paths, undo capture, paste. Baseline recorded in its README.
- Keyboard instructions for screen readers: pane, node and edge `aria-describedby` hidden descriptions,
  localizable (`BlowAriaLabels.PaneInstructions`, `NodeInstructions`, `EdgeInstructions`); an app's own
  `aria-describedby` is kept.
- `BlowControls` button labels as parameters (`ZoomInLabel`, `ZoomOutLabel`, `FitViewLabel`, `LockLabel`,
  `UnlockLabel`).
- Reduced motion: `ReducedMotion` (`BlowReducedMotion.System/Always/Never`), `ReducesMotion`; transitions jump,
  animated edges stop marching (CSS media query plus override classes).
- Forced colours: system-palette rules for edges, handles, selection, guides, minimap; selection and focus as
  outlines. Handle states also differ without colour (ring for valid, dimming for invalid).
- Hit areas in screen pixels at any zoom: `--bit-blow-zoom` on the viewport; edge interaction paths
  (`calc(Wpx / zoom)`), handle `::before` hit area with `--bit-blow-handle-hit-size` (12px).
- Demos: localization (instructions, control labels, Persian with `dir="rtl"`), viewport transitions (motion).
- Fixed the pre-existing CS8602 warning in `McpSurfaceTests`.

Verified (commands from `src/Utilities/Blow`):

- `dotnet build Bit.Blow.slnx` - succeeded, 0 warnings after the fix.
- `dotnet test Tests/Bit.Blow.Tests/Bit.Blow.Tests.csproj` (net8/9/10) - 1,764 / 1,764 (588 per TFM).
- `dotnet test Tests/Bit.Blow.Tests.Mcp/...` - 88 / 88.
- E2E (`Tests/Bit.Blow.Tests.E2E`, Chromium) - 139 / 139, including four new `AccessibilityTests`. The two
  low-zoom tests were checked to fail with the fix removed (handle `::before` disabled, interaction style empty).

Findings: the render benchmark proved every canvas render re-renders every node, edge and handle component
(1,250 components per pan frame at 200 nodes, 6,293 at 1,000; 13.7 ms per frame on JIT, worse on WebAssembly) -
PERF-02, scheduled for round 3. An undo step at 5,000 nodes costs 7.7 ms and 3.3 MB (HIS-03).

### Round 2 - core model: ports, connection rules, algorithms, validation

Implemented:

- `BlowPort` and `BlowNode.Ports` (+ `FindPort`), cloned with the node and compared by the history.
- `BlowConnectionRules` / `BlowConnectionSession` / `BlowConnectionPolicy` / `BlowConnectionRejection`: one ordered,
  UI-free rule set (missing node, connectable, handle types, self, duplicate, data types, source/target limit,
  cycle, application rule). The canvas builds one session per gesture.
- Canvas: `PreventCycles`, `ConnectionTypeCompatibility`, `OnBeforeConnect` (async, re-validated after the await,
  exceptions surfaced through `DispatchExceptionAsync`), `CheckConnection`, `CanStartConnection`,
  `IsHandleAtLimit`, `CountConnections`; rejection reasons on `BlowConnectEndInfo.Rejection` and
  `BlowConnectionLineContext.Rejection`; `AriaLabels.Connected` and `ConnectionRejected` announcements.
- `BlowHandle.DataType`, `BlowHandle.MaxConnections`, `data-port-type`, port label as tooltip; a handle at its limit
  neither starts nor accepts connections. Built-in nodes draw declared ports, spread along each side.
- `BlowGraphIndex`, `BlowGraphAlgorithms` (topological sort, cycles, SCC, components, traversal, reachability,
  shortest path, roots/leaves/isolated/unreachable, statistics) - all iterative.
- `BlowGraphValidator`, `BlowValidationRules` (Structure, Ports, NoCycles, NoIsolatedNodes, AllReachableFrom,
  ForNodes, ForEdges), `BlowValidationResult`, `BlowValidationCodes`.
- Parent cycles reported (`parent-cycle`) and cut instead of flinging nodes away; the MCP flow lab's notes and its
  oracle test updated to the new behaviour.
- Demo `/examples/typed-ports`: a dataflow editor with typed, limited, required ports, a validation panel that
  navigates to problems, and values computed in topological order. README: ports, rules, async confirmation,
  algorithms and validation sections.

Verified:

- `Bit.Blow.Tests` net10.0: 664 / 664 (76 new: GraphAlgorithmTests, ConnectionRulesTests, GraphValidatorTests,
  TypedConnectionTests).
- `Bit.Blow.Tests.Mcp`: 88 / 88.
- E2E `TypedPortTests` (4 new): incompatible types refused with the reason shown and announced, a compatible wire
  completes the graph and the value 7 is computed, a full input refuses a second wire, a self-connection refused.

Review fixes within the round: an asynchronous confirmation landing during the next gesture leaked
"connected" into it (regression test added); duplicate port declarations would have crashed the built-in node on
a repeated `@key`.

Review (independent reviewer agent, then fixed in the round, each with a regression test in
`ConnectionReviewTests`): a release on a refusing handle snapped to a valid neighbour; an asynchronous
confirmation awaited inside the pointer-up handler could release the next gesture's pointer capture and lose the end
event on an exception (the confirmation now runs on its own, its end raised in a `finally`); loose-mode connections
were re-checked, counted and validated by role instead of by the handle's own type; a reconnected edge could not go
back onto its own full handle; a drop was judged against the session built at drag start rather than the graph at
release (a collaborator's edge could slip a cycle through); hovering a refusing handle reported no reason.
`BlowGraphValidator.CreateDefault(mode)` and the `Structure`/`Ports` rules now take the connection mode.

### Round 3 - render performance

Implemented:

- Render scoping in `BlowCanvas`: renders tagged Overlay (viewport, connection line), Geometry (drag, resize,
  measurement) or Full (default; parameters, selection, lists). Version numbers cascaded as named `int` values
  (`BlowCascadingNames`: `ViewportVersion`, `GeometryVersion`, `GraphVersion`, `ConnectionVersion`) beside a fixed,
  named `IBlowContext` (`BlowCascadingNames.Context`). The unnamed, non-fixed context remains for add-ons written
  against it.
- Built-in parts subscribe to exactly what they draw from: handles (graph, connection), resizers (graph), toolbars
  (viewport, graph), background (viewport), controls (viewport, graph), minimap (viewport, geometry). A nested canvas
  takes the fixed context, so the outer flow re-rendering no longer re-renders the inner one.
- `BlowNodeRenderer` and `BlowEdgeRenderer` implement `ShouldRender` over a "look" of what they draw (an edge's cached
  geometry context by reference). Their parameters are now `Settings` (`BlowNodeRenderSettings`,
  `BlowEdgeRenderSettings`, shared and replaced only on change) and `Callbacks` (`BlowNodeCallbacks`,
  `BlowEdgeCallbacks`, made once per element and cleared when the canvas's parameters change) - infrastructure
  types, documented as not for direct use.
- `BlowViewportView` for level-of-detail content in templates, with a `Select` key so only a crossing re-renders.
- Constant keys for unnamed handles; cached arrowhead keys.

Measured (`Tests/Bit.Blow.Benchmarks`, .NET 10 x64, `--job short`, Release):

| Frame | 200 nodes before | after | 1,000 nodes before | after |
| --- | ---: | ---: | ---: | ---: |
| Pan | 1.18 ms, 1.4 MB, 1,250 components | 0.29 ms, 7 components | 13.7 ms, 7.3 MB, 6,293 components | 1.7 ms, 7 components |
| Drag one node (pointer move) | (every render re-rendered everything) | 0.30 ms, 12 components | ~14 ms | 1.7 ms, 12 components |

Allocation per 1,000-node frame (headless renderer probe): 1.13 MB after scoping alone, 100 KB after the callback and
settings changes.

Verified: `Bit.Blow.Tests` net10.0 683 / 683 (11 new `RenderScopeTests`: a pan renders no node, edge or handle; add-ons
follow the viewport; a drag frame renders only the dragged node and its edge; handles show and forget verdicts; the
connection line moving renders no node; selection reaches resizers and toolbars inside templates; toolbars keep
their scale; a host re-render redraws every template; `Refresh` redraws everything; an app's own add-on on the
unnamed context still follows; `BlowViewportView` re-renders only at its threshold). E2E after scoping: 144 / 144.

Known limitation recorded: templates that read the viewport directly through a canvas reference no longer re-render on
pan or zoom; they use `BlowViewportView` (documented under "Templates and the viewport").

Next: round 4 - serialization and interchange (SER-01..05).

### Round 4 - serialization, interchange and the layer gates

Implemented:

- `BlowDocument` (nodes, edges, viewport, id, application version, `Metadata`) and `BlowSerializer`: a versioned
  JSON format (`"format": "bit-blow"`, schema 1) written with `Utf8JsonWriter`, deterministic and culture-invariant,
  defaults left out; `Serialize`, `SerializeToUtf8Bytes`, `SerializeAsync`, `Deserialize` (string, bytes),
  `DeserializeAsync` (stream, bounded).
- Import is a validation pass, not a cast: `BlowImportResult` with issues located by JSON path and severity; broken
  elements are left out and reported; graph problems (dangling edges, duplicate ids, parent cycles) become warnings
  at the element. `BlowImportLimits` bound bytes, nodes, edges, ports per node, nesting depth and string length,
  checked before parsing where possible.
- Custom data: kept as `JsonNode` unless the application maps a node or edge type to a source-generated
  `JsonTypeInfo<T>` (`MapNodeData`, `MapEdgeData`, `MapData`) - trim and AOT safe; no type names are read or
  written, so a document cannot choose what gets instantiated.
- Application migrations (`BlowMigration(FromVersion, Migrate)`) applied step by step; gaps, failures and newer
  documents are reported.
- `BlowReactFlowFormat.Import/Export` for React Flow's `toObject()` shape; what has no counterpart (custom edge
  types, `dragHandle`) is reported rather than guessed.
- Canvas: `ToDocument`, `ToJson`, `LoadJsonAsync` (the graph is replaced only when the import succeeded).
- `BlowRenderGate` around the node and edge layers: a frame that changed nothing in a layer stops at one integer
  comparison instead of visiting every renderer's `ShouldRender`. `BlowViewportView` now detects a changed
  `ChildContent`/`Select` by delegate reference.
- Demo `/examples/save-restore` rewritten: save and load JSON, download and upload a file, a migration, React Flow
  import and export. README: saving to JSON, custom data, untrusted documents, migrations, React Flow interchange.

Measured (`Tests/Bit.Blow.Benchmarks`, .NET 10 x64, `--job short`, Release; rerun in round 5):

| Frame | 200 nodes, round 3 | round 4 | 1,000 nodes, round 3 | round 4 |
| --- | ---: | ---: | ---: | ---: |
| Pan | 0.29 ms | 5.2 us, 12 KB | 1.7 ms | 10.9 us, 45 KB |
| Zoom | 0.29 ms | 5.1 us | 1.7 ms | 11.0 us |
| Drag one node | 0.30 ms | 110 us, 35 KB | 1.7 ms | 573 us, 125 KB |

In the browser (E2E `PerformanceTests`, Chromium, demo published in Release, 900 nodes, culling off, end to end
including style, layout and paint): zoom step median 83.2 ms before the gates, 55.9 ms (p90 61.0 ms) after; drag
step median 64.5 ms before, 65.2 ms (p90 66.4 ms) after - unchanged within noise. Not yet profiled; the likely
remainder is the browser's own work (a zoom changes `--bit-blow-zoom`, which restyles every element that reads it)
and, for a drag, the per-frame walk of the node list on the Mono interpreter. Recorded under PERF for a later round.

Verified: `Bit.Blow.Tests` net10.0 712 / 712 (29 new: SerializerTests, ReactFlowFormatTests). Release build of the
demo (which caught a source generator that cannot see types nested in a Razor component - the demo's data contract
moved to `SaveRestoreJsonContext.cs`).

### Round 5 - editing: change events, transactions, labelled and shared history, grouping, stacking

Implemented:

- `BlowGraphChange` (`Kind`: Added, Removed, Moved, Resized, Connected, Reconnected, Replaced, Edited; `Origin`:
  User, Api, History, Remote; touched and removed nodes and edges; `Label`) raised through `OnGraphChanged` once per
  completed edit, from every site that edits: drag, resize, connect, reconnect, delete, cut, paste, duplicate, add,
  align, distribute, keyboard nudge, load, restore, undo, redo, group, ungroup, order.
- `EditAsync(Action | Func<Task>, label, origin, recordHistory)`: any number of changes as one undo step, one change,
  one `NodesChanged`/`EdgesChanged`, one render; canvas methods called inside are folded in; `Remote` origin and
  `recordHistory: false` for collaboration adapters.
- History: every step labelled (`BlowHistory.UndoLabel`/`RedoLabel`, `BlowHistoryState.UndoLabel`/`RedoLabel`,
  `CaptureHistory(label)`); `Capture(nodes, edges, viewport, label)` shares the unchanged clones of the previous step
  (by id, so a shifted list still shares) and returns false for a no-op.
- `GroupSelectionAsync(group?, padding)` and `UngroupAsync(group)`, keeping every node where it is on screen;
  `BringToFrontAsync` / `SendToBackAsync` moving nodes with their descendants.
- Demo `/examples/grouping` (group, ungroup, reorder, a batched edit, a simulated remote edit, a live change log, undo
  buttons naming their step). README: graph changes and transactions, grouping, stacking commands, history labels
  and memory.

Measured: recording an undo step after one node moved - 1,000 nodes 1.00 ms / 654 KB -> 30 us / 18 KB; 5,000 nodes
7.70 ms / 3.3 MB -> 172 us / 91 KB. A full 100-step history at 5,000 nodes: about 330 MB -> about 9 MB.

Verified: `Bit.Blow.Tests` net10.0 726 / 726 (14 new `EditingTests`), all three TFMs 2,178 / 2,178; MCP 88 / 88;
E2E 150 / 150 including the new `GroupingTests` (grouping keeps the nodes in place and undo names the step; a
batched edit is one undo step; a remote edit is reported as remote and cannot be undone).

Review (independent reviewer agent, which confirmed seven of its nine findings with scratch bUnit tests outside the
repository; all nine fixed, each with a regression test in `EditingReviewTests`):

- An asynchronous `EditAsync` swallowed every edit made while it waited (a flag, not a scope). The edit is now an
  async-local scope plus a set of open edits: only the edit's own code is folded in - not a key press handled
  meanwhile, and not work it left running.
- Grouping and ungrouping moved nodes when `NodeOrigin` was not the top-left: positions are relative to the parent's
  position point, not its corner. Now computed from position points.
- Shared history copies carried stale selection and pre-measurement sizes back on undo. Copies are now shared only
  when that restored-but-not-compared state matches too.
- A throwing edit left its partial change uncommitted, to be folded into the next step. It is now committed, then
  rethrown.
- Nested calls still raised `NodesChanged`/`EdgesChanged` and rendered mid-edit, and `CaptureHistory` (so
  `LoadJsonAsync`) recorded steps inside one. All held back to the end of the edit.
- `recordHistory: false` edits were still undone by the next local step. They are now written into every recorded
  step (`BlowHistory.Rebase`), dropping steps they overwrite and edges they orphan.
- The keyboard set an ambient "user" origin across awaits, so app handlers' edits were reported as the user's.
  Origins are now passed explicitly.
- `SendToBackAsync` put a child before its own parent; it now goes just after it.
- `UngroupAsync` removed the group without `Deletable`, `OnBeforeDelete` or delete events; it now honours all three.

After the fixes: `Bit.Blow.Tests` net10.0 740 / 740 (14 regression tests). The regression tests were written after
the fixes; the failures they cover were reproduced by the reviewer's scratch tests against the code before them.

### Round 6 - accessibility: keyboard connections, state descriptions, validation marks, focus and search

Implemented:

- Connecting from the keyboard (`ConnectionKey`, default `C`): start from the focused node's first port that can
  start one, step through its ports, aim by moving focus (the line follows, the first acceptable port is chosen and
  announced, or the reason it cannot), Enter connects, Escape cancels; every connection rule, `OnBeforeConnect` and
  the connect events apply as for a drag. Instructions and announcements localizable.
- Element state in the accessible description: a hidden element referenced first by `aria-describedby` (an
  `aria-description` would be ignored next to `aria-describedby`): selected, connections, validation problems most
  serious first; edges named by their nodes' names. `BlowAriaLabels.NodeState`/`EdgeState`.
- `Validation` parameter: errors and warnings marked on nodes (ring and a round or triangular badge), edges (long or
  short dashes) and ports (ring); dark-mode colours meeting 4.5:1; forced-colours rules.
- `FocusNodeAsync`/`FocusEdgeAsync` (centre, optional select, DOM focus of the canvas's own element), `SelectAsync`,
  `FindNodes`/`FindEdges` and `BlowGraphSearch` (ranked, case- and accent-insensitive, words, ports, own text).
- Minimap: focusable while it steers the view, described; arrows pan, +/- zoom, Home fits; its keys are marked handled
  so the pane does not also move the selection, and other keys (Escape) reach the pane.
- Shift+F10 on a focused node verified end to end. Typed-ports demo: validation marks, search box, problem list that
  focuses the node.

Verified: unit 770 / 770 net10.0 after the round (28 new: KeyboardConnectionTests, ValidationMarksAndFocusTests,
GraphSearchTests; instruction tests updated to the two-part description). E2E KeyboardAccessTests 6 / 6 (keyboard
connection with the value computed; a refused target explained and Escape; badges by shape and description; search to
focus; Shift+F10; minimap arrows without page scroll).

Review (independent reviewer agent; four findings confirmed with scratch bUnit tests, all fixed with regression tests
in `KeyboardConnectionReviewTests`): the mouse leaving the pane cancelled a keyboard connection silently, and a
hovering mouse steered its line (a keyboard connection is no longer a pointer gesture: the pane does not listen to the
pointer for it); a press on a handle started a second connection without ending the first (a pointer press now ends
it, announced); an Enter whose async check outlived the connection acted on it and left stale state for the next
gesture (re-checked; every gesture now starts from clean state); a deleted source node was not noticed until Enter;
focused edges with a validation mark had no visible focus (WCAG 2.4.7); the minimap's key guard swallowed browser
shortcuts with modifiers; the minimap stopped Escape from reaching the pane; edges to hidden nodes could be focused,
selected and found; keyboard `OnConnectEnd` reported a stale pointer position.

### Round 7 - layouts and collapsible groups

Implemented:

- `BlowLayout.Tree` (tidy subtree bands over the breadth-first spanning forest; iterative), `BlowLayout.Radial`
  (rings sized so neighbours and rings clear each other's bounding circles), `BlowLayout.Force` (deterministic
  Fruchterman-Reingold from a golden-angle spiral, pinned nodes, then overlap removal).
- `IBlowLayout`, implemented by every layout's options class; canvas `LayoutAsync(layout, nodes?, keepPlace)` for the
  selection or the top level, one parent at a time, kept in place, with edges into a group's children lifted to the
  group; one undo step labelled `layout`.
- `BlowNode.Collapsed` and `SetCollapsedAsync`: descendants hidden and deselected, edges from outside drawn to the
  collapsed node, edges inside hidden, nested collapsed groups kept on expand, announced, one undo step, saved by the
  serializer and compared by the history.
- Demo `/examples/layouts-and-collapsing`; README auto-layout and collapsing sections.

Measured (`LayoutBenchmarks`, .NET 10 x64, `--job short`): tree 12 us / 69 us / 0.88 ms at 200 / 1,000 / 5,000 nodes;
radial 14 us / 83 us / 1.03 ms; force 9.1 ms at 200 and 217 ms at 1,000 (quadratic; documented as such).

Verified: unit 787 / 787 net10.0 (17 new `MoreLayoutTests`, including the lifted-edge case, which fails without the
lift: every node lands in one layer). All TFMs, MCP and E2E after rounds 6 and 7: unit 2,385 / 2,385 (795 per TFM,
the round 6 review tests included), MCP 88 / 88, E2E 157 / 157.

### Round 8 - routing, SVG export, run states, workflow and state-machine demos

Implemented:

- `BlowEdgeRouting.Orthogonal`/`Route`: orthogonal routes round obstacles with a margin, stubs out of the handles, and
  a bend cost; an A* over the sparse grid of the obstacles' margin lines with an admissible bound for early exit.
  `BlowEdgeType.Routed` (appended to the enum), canvas `Routing` parameter; routed edges re-route when the geometry
  version changes (a node moved or resized), not on pan or zoom, considering only nodes near their ends and never the
  groups their ends are in; fallback to a smooth step.
- `BlowSvgExport.ToSvg` and canvas `ToSvg()`: standalone SVG in the built-in look, measured anchors, routed edges,
  markers; labels written as escaped text, colours only from an allow-list of plain colour values, invalid XML
  characters dropped.
- `BlowRunState`/`BlowRunStatus` and `RunStates`: classes, glyph badges (ellipsis, ring, tick, cross, dash) on
  `::before` beside the validation badge on `::after`, running ring that stops under reduced motion, forced colours,
  dark tokens; status and message in the node's description.
- Demos `/examples/workflow-runner` (validated first, run in topological order, a failure skips dependants, stop,
  SVG export), `/examples/state-machine` (routed, labelled transitions, a self-loop, events as buttons),
  `/examples/edge-routing` (drag the blocks, margin slider, SVG export).

Measured (`RoutingBenchmarks`): one routed edge across a block of 10 / 40 / 100 nodes: 4.7 us / 11.4 us / 28 us
(the setup asserts a route is found, so a give-up is not being timed).

Verified: unit 804 / 804 net10.0 (9 new `RoutingExportAndRunTests`, including injection attempts through labels,
colours and the title). E2E `RunAndRoutingTests` 4 / 4 and the demo sweep (`DemoPagesTests`) over the new pages. The
first E2E run caught a demo bug: the SVG download link stayed `#` because the page rendered before the canvas
reference existed; the demos now re-render on `OnInit` and on edits.

Review of rounds 7 and 8 (independent reviewer agent; eight of ten findings confirmed with scratch probes; all ten
fixed, each with a regression test in `LayoutRoutingReviewTests`):

- Expanding a group that was itself folded away inside a collapsed group showed its children loose on the canvas;
  now they stay hidden until it is on show.
- Edges folded away by a collapse stayed selected, and select-all selected edges to hidden nodes, so the delete key
  could remove what nobody could see.
- `LayoutAsync` moved pinned nodes when keeping the arrangement in place.
- The force layout could leave nodes overlapping (a dense start, a tiny edge length) and gave NaN positions for an
  astronomic edge length; a final placement pass now guarantees no overlap, and the length is bounded.
- The router dropped a node entirely when a handle's stub ended inside its margin, and the route then ran through it;
  such a node is now kept clear by its own box.
- The SVG export let U+FFFE, U+FFFF and lone surrogates through (unparseable output), clipped self-loops and detours,
  dropped edges drawn to a collapsed group, and routed against every node in the graph (past the obstacle limit,
  unlike the canvas).
- Every routed edge was searched again on every drag frame; the search now reruns only when the edge's ends or the
  nodes near it changed.
- Run-state styles overrode selection, problem and focus rings (box shadow, border colour, outline order) and dimmed
  the skipped badge below its contrast; running is now an outline before those rules, skipped a dashed border.
- Minor: a radial root asked for was not a root when another root led to it; `LayoutAsync` and `ToSvg` changed the
  caller's options; a collapsed group was described as "Not connected" though edges were drawn to it.

### Round 9 - documentation, MCP, single-pointer moves, forced colours, security pass

Implemented:

- `MoveSelectionAsync(dx, dy)`: the arrow keys' move as an API, for single-pointer alternatives to dragging (WCAG
  2.5.7); `/examples/a11y` puts move buttons on the selected node's toolbar.
- README: highlights, every new capability's section (graph changes and transactions, grouping, stacking, history
  labels and memory, showing problems, showing a run, routing, SVG export, keyboard connections, taking the user
  somewhere, without dragging, minimap keys, layouts, collapsing), localization table, an Upgrading table of every
  behaviour and API change, measured numbers in .NET and in the browser. Package README features list.
- MCP: the API reference and the guide are generated from the assembly and the README, so they cover the new surface
  without changes; `InspectBlowFlow` gained the tree, radial and force layouts, and the oracle test checks them
  against the library.
- Security pass over everything added: no `MarkupString`, `innerHTML` or `eval` in the library (grep); every new
  user-visible string is rendered as text by Razor; imports bounded and free of type names (round 4); search compares
  text, never builds a pattern; the SVG export escapes text and allow-lists colours (with a regex timeout), and its
  tests try injection through labels, colours and the title; `focusElement` compares ids by value rather than building
  a selector from them.

Verified: unit 805 / 805 net10.0; MCP 88 / 88; E2E ForcedColorsAndPointerTests 3 / 3 plus KeyboardTests and
AccessibilityTests (11 / 11): under emulated forced colours a selected node has a solid outline and the run badges are
drawn in CanvasText with their glyphs; a node moves with single clicks. `dotnet build Bit.Blow.slnx -c Release
--no-incremental`: 0 warnings, 0 errors. Browser performance (Release, 900 nodes, culling off): zoom step median
58.0 ms (p90 59.8), drag step median 66.2 ms (p90 67.8) - within noise of round 4's 55.9 / 65.2, so rounds 5 to 8 added
no measurable cost in the browser.

### Round 10 - final audit

Done:

- The review findings of rounds 7 and 8 fixed (logged under round 8); 15 regression tests.
- Final runs, from `src/Utilities/Blow`:
  - `dotnet test Tests/Bit.Blow.Tests/Bit.Blow.Tests.csproj` (net8.0, net9.0, net10.0): 2,460 / 2,460 (820 per TFM;
    the baseline was 565).
  - `dotnet test Tests/Bit.Blow.Tests.Mcp/Bit.Blow.Tests.Mcp.csproj`: 88 / 88.
  - `dotnet build Bit.Blow.slnx -c Release --no-incremental`: 0 warnings, 0 errors (the baseline had one warning).
  - E2E (`Tests/Bit.Blow.Tests.E2E`, Chromium, Debug): 167 / 167 (the baseline was 135).
  - Render benchmarks after rounds 6 to 9: pan frame 5.4 us at 200 nodes and 10.9 us at 1,000; drag frame 112 us and
    562 us - unchanged from round 4, so the state descriptions, validation and run marks added nothing measurable
    to a frame.
- Repository hygiene: benchmark artifacts and test results are ignored by `.gitignore`; no commits were made.

Status at the end: 78 requirements - 75 verified (54 by this work, 21 at the baseline by the existing tests), 2
partial (A11Y-07: reconnecting an existing edge from the keyboard; EXT-02: the canvas's routed edge type is not
swappable, though the router is a public function), 1 deferred (PERF-04: measuring only the nodes that changed,
pending browser profiling). Every P0 requirement is verified.
