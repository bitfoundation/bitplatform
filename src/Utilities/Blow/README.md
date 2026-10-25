# Bit.Blow

A Blazor-native library for node-based UIs: editors, flow charts, pipelines, state machines and diagrams - the
Blazor counterpart to [React Flow](https://reactflow.dev). Dragging, selecting, connecting, resizing, keyboard
handling, layout and edge geometry are written in C# and run on Blazor's own event system. JavaScript does only
what the browser alone can do: measure DOM rectangles, notice when they change, find the handle under a point, hold
pointer capture and keep the page from scrolling under a gesture. There is no JavaScript diagram library underneath.

```razor
<div style="height: 400px">
    <BlowCanvas @ref="_flow" Nodes="_nodes" Edges="_edges" FitViewOnInit="true">
        <BlowBackground />
        <BlowControls />
    </BlowCanvas>
</div>

@code {
    private BlowCanvas? _flow;

    private List<BlowNode> _nodes =
    [
        new BlowNode { Id = "1", Type = "input", Label = "Order placed", Position = new(0, 0) },
        new BlowNode { Id = "2", Label = "Payment", Position = new(0, 100) },
        new BlowNode { Id = "3", Type = "output", Label = "Shipped", Position = new(0, 200) },
    ];

    private List<BlowEdge> _edges =
    [
        new BlowEdge { Id = "e1-2", Source = "1", Target = "2" },
        new BlowEdge { Id = "e2-3", Source = "2", Target = "3", Animated = true },
    ];

    private BlowSnapshot Save() => _flow!.ToObject();
}
```

## What it is for

Workflow and approval editors, data and ETL pipelines, automation and chatbot designers, state machines, org
charts, mind maps, dependency graphs, visual programming and node graphs, and any other screen where people
arrange boxes and draw lines between them.

The graph is plain C#: a `List<BlowNode>` and a `List<BlowEdge>` that the canvas edits in place. The same objects
can be built, validated, laid out, copied and saved with no UI at all, which is what makes a flow testable and
usable on a server.

## Highlights

- **A pannable, zoomable canvas**: drag to pan, wheel to zoom toward the cursor, two-finger pinch, pan-on-scroll,
  middle-button pan and hold-Space-to-pan.
- **Nodes** with single and multi-selection, box selection, drag thresholds, snap-to-grid, alignment guides and
  auto-panning past the edge of the pane.
- **Connections** drawn by dragging or clicking from handle to handle, with proximity snapping, duplicate and
  self-connection rules, live validation on the canvas and on each handle, and reconnection of existing edges.
- **Typed ports**: declare a node's inputs and outputs with data types, connection limits and required
  connections; the canvas refuses what does not fit and says why, cycles can be prevented, and an async hook can
  confirm a connection with a server.
- **Graph algorithms and validation with no UI**: topological order, cycles, strongly connected components,
  reachability, shortest paths, statistics, and a rule-based validator that ties every problem to a node, edge or
  port.
- **Built-in node types** (`input`, `default`, `output`, `group`) and a `NodeTemplate` for anything else.
- **Six edge paths** (bezier, simple bezier, straight, step, smooth step, and orthogonal routes that go round the
  nodes in their way), each tunable per edge, with labels, arrowheads in any colour and size, animation, and
  self-connections that loop clear of their node.
- **Sub-flows**: children move with their parent and can be pinned inside it or grow it; selections group and
  ungroup in place, and a group collapses into itself with its edges redrawn to it.
- **Add-ons**: background patterns, a control bar, a minimap, panels, a node resizer, node and edge toolbars, and
  a portal into the zoomed layer.
- **Auto-layout and align/distribute in C#**: layered (with crossing reduction), tree, radial, force-directed,
  grid and circle layouts behind one `IBlowLayout` interface, applied to the graph or just the selection, and the
  align and distribute commands of a drawing tool. No JavaScript layout engine.
- **Animated viewport helpers** that ease instead of cutting, and give way the moment the user takes over.
- **Editing for real applications**: typed change events with their origin (user, code, history, remote),
  transactions that make many changes one undo step, labelled undo steps whose memory grows with the change rather
  than the graph, edits from a collaborator that local undo never takes back, and **copy and paste** that rewires
  edges and keeps groups intact.
- **Saving and loading** as a versioned, deterministic JSON document with typed custom data, application
  migrations, bounded and located import errors, and React Flow interchange; **SVG export** that is safe to embed.
- **Workflows and validation on the graph**: validation problems and run states (queued, running, succeeded,
  failed, skipped) marked on nodes, edges and ports by shape as well as colour, and read out with each element.
- **Keyboard and screen reader support**: nodes and edges in the tab order, Enter/Space, arrow keys, Escape and
  Delete, connections made from the keyboard, a keyboard-driven minimap, search and "go to" that move focus, each
  element's state in its description, edits announced through a live region, reduced motion and Windows high
  contrast respected, and every string localizable.
- **Forgiving**: a malformed graph is reported through `OnError` instead of throwing.
- **Fast where it counts**: a pan or zoom re-renders no node, edge or handle; a drag re-renders only what moved.
  Benchmarks are in the repository.
- **Theming** through `--bit-blow-*` CSS custom properties, with light, dark and system colour modes.
- Works on **Blazor WebAssembly, Blazor Server, Blazor Web App and Blazor Hybrid**, on .NET 8, 9 and 10.

## Quick start

```bash
dotnet add package Bit.Blow
```

Add the namespace to `_Imports.razor`:

```razor
@using Bit.Blow
```

Link the stylesheet from your host page (`App.razor`, `index.html` or `_Host.cshtml`):

```html
<link rel="stylesheet" href="_content/Bit.Blow/bit-blow.css" />
```

Put a canvas in a container that has a height:

```razor
<div style="height: 600px">
    <BlowCanvas Nodes="_nodes" Edges="_edges" FitViewOnInit="true" />
</div>
```

There are no services to register. The stylesheet is not injected for you: without the link the canvas and its
add-ons render unstyled. The script module, `_content/Bit.Blow/bit-blow.js`, is imported by the component itself
the first time it needs it. The full walkthrough is in [getting started](#getting-started).

## Requirements

- .NET 8, .NET 9 or .NET 10
- Blazor WebAssembly, Blazor Server, Blazor Web App or Blazor Hybrid

## The demo

The demo site, [blow.bitplatform.dev](https://blow.bitplatform.dev), is a catalogue of more than eighty working
examples under `/examples`, grouped the way this guide is: basics, nodes, edges, handles and connections,
interaction, viewport and layout, state and API, and add-ons. Most of them are ports of React Flow's own examples,
so a React Flow user can open the same example on both sides. A getting-started page (`/getting-started`) and a
handful of hand-written tour pages sit next to the catalogue (`/basic-flow`, `/custom-nodes`, `/edge-types`,
`/custom-edges`, `/interactions`, `/labels-handles`, `/extras`, `/advanced`, `/connection-line`, `/add-node-on-drop`,
`/floating-edges`, `/minimap`, `/drag-threshold`), all of them reachable from the docs navigation and the search
palette (`/` or Ctrl+K). Every page can show its own source, and every flow on the site follows its light/dark theme.

To run it locally:

```bash
dotnet run --project src/Utilities/Blow/Demo/Server
```

## How it fits together

```text
Your state          List<BlowNode>, List<BlowEdge>     plain, mutable C# objects
  |
BlowCanvas          interaction, selection, keyboard, parent chains, edge geometry, viewport
  |-- NodeTemplate / EdgeTemplate                       your markup, with BlowHandle inside
  |-- ChildContent                                      BlowBackground, BlowControls, BlowMiniMap, ...
  |
bit-blow.js         measures nodes and handles, watches for resizes, finds the handle under a point
```

Everything ships in one package, `Bit.Blow`, under one namespace, `Bit.Blow`:

| Part | Contents |
| --- | --- |
| The component | `BlowCanvas` and its add-ons: `BlowHandle`, `BlowBackground`, `BlowControls`, `BlowMiniMap`, `BlowPanel`, `BlowNodeResizer`, `BlowNodeToolbar`, `BlowEdgeToolbar`, `BlowViewportPortal` and the rest. |
| The model | `BlowNode`, `BlowEdge`, `BlowViewport`, `BlowSnapshot` and the small value types around them. |
| The helpers | `BlowLayout`, `BlowAlign`, `BlowAlignment`, `BlowClipboard`, `BlowHistory`, `BlowGraphUtils`, `BlowEdgePath`. Pure functions over the model: no UI and no JavaScript, so they run on a server or in a unit test. |

The canvas never copies your graph. It renders the lists you hand it, edits them in place as the user works, and
raises `NodesChanged` and `EdgesChanged` when an edit is complete. Pointer and wheel input arrive through ordinary
Blazor events; the browser layer only answers questions C# cannot.

## MCP server

The demo hosts a [Model Context Protocol](https://modelcontextprotocol.io) server at
`https://blow.bitplatform.dev/mcp`, so an AI agent writing flow code reads the library itself - its API, this
guide and the demo's working pages - instead of guessing at an API it half remembers.

```json
{
  "mcpServers": {
    "blow": { "type": "http", "url": "https://blow.bitplatform.dev/mcp" }
  }
}
```

Start with `SearchBlow`, which searches the guide, every public type and member, the setup guides, the
stylesheet's theme properties and classes, and the demo's pages at once, and points each hit at the exact
follow-up call. `GetBlowSetupGuide` gives the wiring for one render mode, `GetBlowGuideSection` one section of this
guide, `GetBlowApi` one public type with its members, real defaults and documentation, `GetBlowTheme` the
`--bit-blow-*` custom properties with their light and dark values, and `GetBlowSourceFile` one of the demo's
working pages; each returns its index when called without a key. `InspectBlowFlow` runs a flow - nodes and edges
as JSON, the shape `BlowCanvas.ToObject()` returns as a `BlowSnapshot` - through the library's own checks and
layout and reports what it finds, or, called with no flow, returns a sample one. Every tool is read-only.

## Documentation

- [Getting started](#getting-started)
- [Nodes](#nodes)
- [Handles and connections](#handles-and-connections)
- [Edges](#edges)
- [Interaction](#interaction)
- [The viewport](#the-viewport)
- [Layout and alignment](#layout-and-alignment)
- [Graph algorithms and validation](#graph-algorithms-and-validation)
- [Add-ons](#add-ons)
- [Saving, undo and clipboard](#saving-undo-and-clipboard)
- [Theming](#theming)
- [Keyboard and accessibility](#keyboard-and-accessibility)
- [Localization](#localization)
- [Errors and diagnostics](#errors-and-diagnostics)
- [Performance](#performance)
- [Hosting models](#hosting-models)
- [Architecture](#architecture)
- [Upgrading](#upgrading)
- [Coming from React Flow](#coming-from-react-flow)

## Getting started

### Install

```bash
dotnet add package Bit.Blow
```

Add the namespace to `_Imports.razor`. Everything in the library - the components, the model, layout, history and
the clipboard - lives in it:

```razor
@using Bit.Blow
```

Link the stylesheet from the host page. In a Blazor Web App that is `Components/App.razor`; in a standalone
WebAssembly app or a Blazor Hybrid app it is `wwwroot/index.html`:

```html
<link rel="stylesheet" href="_content/Bit.Blow/bit-blow.css" />
```

On .NET 9 and later, `href="@Assets["_content/Bit.Blow/bit-blow.css"]"` in `App.razor` serves the fingerprinted
copy. The link is required: the library does not inject its stylesheet, so leaving it out gives an unstyled
canvas rather than an error. Put your own overrides after it.

There are no services to register. The component imports its script module, `_content/Bit.Blow/bit-blow.js`, on
demand.

### A first flow

```razor
@page "/workflow"

<div style="height: 600px">
    <BlowCanvas @ref="_flow"
                Nodes="_nodes"
                Edges="_edges"
                FitViewOnInit="true"
                OnConnect="OnConnect"
                NodesChanged="OnNodesChanged">
        <BlowBackground Variant="BlowBackgroundVariant.Dots" />
        <BlowControls />
        <BlowMiniMap />
        <BlowPanel Anchor="BlowPanelPosition.TopRight">
            <button @onclick="AddStep">Add step</button>
        </BlowPanel>
    </BlowCanvas>
</div>

@code {
    private BlowCanvas? _flow;
    private int _next = 4;

    private List<BlowNode> _nodes =
    [
        new BlowNode { Id = "1", Type = "input",  Label = "Request", Position = new(250, 0) },
        new BlowNode { Id = "2",                  Label = "Review",  Position = new(100, 120) },
        new BlowNode { Id = "3", Type = "output", Label = "Approve", Position = new(250, 240) },
    ];

    private List<BlowEdge> _edges =
    [
        new BlowEdge { Id = "e1-2", Source = "1", Target = "2", Animated = true },
        new BlowEdge { Id = "e2-3", Source = "2", Target = "3", Label = "ok" },
    ];

    // The canvas has already added the new edge to _edges; this is only for side effects.
    private void OnConnect(BlowConnection connection)
        => Console.WriteLine($"{connection.Source} -> {connection.Target}");

    // Raised once a drag, resize, delete or paste has finished - a good moment to persist.
    private Task OnNodesChanged(List<BlowNode> nodes) => Task.CompletedTask;

    private void AddStep()
    {
        var id = (_next++).ToString();
        _flow!.AddNodes(new BlowNode { Id = id, Label = $"Step {id}", Position = new(400, 120) });
    }
}
```

That is a working editor. Nodes drag; dragging from a handle to another node connects them; clicking selects,
Shift+click adds to the selection and Shift+drag on the background draws a selection box; Delete removes the
selection; the wheel zooms toward the cursor, dragging the background pans and two fingers pinch. Tab reaches every
node and edge, Enter selects, the arrow keys move the selection and Escape clears it.

The canvas fills its container (`width: 100%; height: 100%`), so the container needs a height. `Style` on the
canvas works as well: `<BlowCanvas Style="height: 600px" ... />`.

### Reacting to edits

The canvas edits the lists and the node and edge objects you gave it, so your state is always the graph on screen.
What it raises tells you when something worth knowing has happened:

- `NodesChanged` and `EdgesChanged` once an edit is complete - a drag released, a resize ended, a connection made,
  something deleted, pasted, aligned or nudged with the keyboard. Not on every pointer move.
- `OnNodeDrag`, `OnNodeResize` and `OnMove` while a gesture runs, for live readouts.
- `OnSelectionChange` when the selected set actually changes.
- `OnConnect`, `OnReconnect` and `OnDelete` for the specific edits.
- `OnGraphChanged` once per completed edit of any kind, with what kind it was, who made it and the elements it
  touched - the one place to persist, validate or broadcast from (see
  [graph changes and transactions](#graph-changes-and-transactions)).

`Nodes`/`NodesChanged` and `Edges`/`EdgesChanged` are bindable pairs, so `@bind-Nodes="_nodes"` compiles, but
since the list instance never changes it is mostly a signal to persist. The full list is in the
[event reference](#event-reference).

When your own code changes the graph - adds a node, edits a label, flips `Hidden` - the usual Blazor rule applies:
your component re-renders after its event handler, hands the canvas its parameters again, and the canvas re-reads
the lists and re-measures. For a change made outside a render of your component (a timer, a message from a
server), call `_flow.Refresh(remeasure: true)`, or `InvokeAsync(StateHasChanged)` on your component.

### Calling the canvas from code

Capture the canvas with `@ref` for the imperative API - the counterpart of React Flow's `useReactFlow()`:

```csharp
_flow.FitView();
await _flow.SetCenterAsync(x: 400, y: 200, zoom: 1.5, durationMs: 300);
BlowXYPosition point = _flow.ScreenToFlowPosition(e.ClientX, e.ClientY);

_flow.AddNodes(node);
await _flow.DeleteElementsAsync(nodes: [node]);
BlowNode? found = _flow.GetNode("2");

BlowSnapshot snapshot = _flow.ToObject();
await _flow.RestoreAsync(snapshot);
```

`OnInit` hands the same reference to you once the canvas has rendered and measured for the first time. To drive a
flow from a sidebar or a toolbar elsewhere on the page, pass the reference to them; nothing like React Flow's
provider is needed (see `/examples/provider`).

### Where to go next

- [Nodes](#nodes) and [Custom nodes](#custom-nodes) for your own node markup.
- [Handles and connections](#handles-and-connections) for what may connect to what.
- [Saving, undo and clipboard](#saving-undo-and-clipboard) for persistence and editing commands.
- [Hosting models](#hosting-models) before deploying on Blazor Server or a Web App.

## Nodes

### The node model

`BlowNode` is a mutable class, so the canvas can update it in place as the user drags and selects.

| Property | Default | Meaning |
| --- | --- | --- |
| `Id` | required | Unique across the flow. |
| `Position` | `(0, 0)` | Flow coordinates. Relative to the parent's top-left when `ParentId` is set. Names the point of the node given by its [origin](#node-origin). |
| `Type` | `"default"` | Selects the built-in look, and is what a `NodeTemplate` switches on. |
| `Label` | `null` | Text drawn by the built-in node types; also the node's accessible name. |
| `Data` | `null` | Any payload of yours, for templates to read. |
| `Width`, `Height` | `null` | An explicit size. When null the node sizes to its content and is measured. |
| `Selected` | `false` | Maintained by the canvas; you may set it too. |
| `Draggable` | `true` | False keeps the node still, by pointer and by keyboard. |
| `Selectable` | `true` | False skips it for clicks, box selection and select-all. |
| `Connectable` | `true` | False stops connections starting from or ending on any of its handles. |
| `Deletable` | `true` | False makes it survive Delete and `DeleteElementsAsync`. |
| `Focusable` | `true` | False takes it out of the tab order. |
| `Hidden` | `false` | Not drawn, not interactive, and its edges are hidden with it. |
| `SourcePosition` | `Bottom` | Side of the built-in source handle, and the fallback anchor for edges. |
| `TargetPosition` | `Top` | Side of the built-in target handle, and the fallback anchor for edges. |
| `ParentId` | `null` | Makes the node a child of another; see [sub-flows](#sub-flows-and-grouping). |
| `ExtentParent`, `Extent`, `ExpandParent` | off | Where the node may be dragged; see [constraining](#constraining-where-a-node-can-go). |
| `UseCustomDragHandle` | `false` | Only a `BlowDragHandle` inside the node starts a drag. |
| `AriaLabel` | `null` | Accessible name, when it should differ from `Label`. |
| `Class`, `Style` | `null` | Extra CSS class and inline style on the node element. |
| `ZIndex` | `null` | Explicit stacking order; always wins. |
| `Origin` | `null` | Per-node override of the canvas's `NodeOrigin`. |
| `Ports` | `null` | Declared inputs and outputs, with data types and limits; see [ports](#ports-typed-inputs-and-outputs). |

The canvas maintains a few more for itself: `Dragging`, `Resizing`, `Measured` (the measured size, empty until the
first measurement), `AbsolutePosition` (flow position after walking the parent chain), `Handles` (the measured
handle anchors) and `EffectiveSize`. `GetRect()` returns the node's absolute bounds and `Clone(newId)` copies it;
`Data` is copied by reference.

### Built-in node types

| `Type` | Renders |
| --- | --- |
| `"input"` | The label and a source handle. |
| `"default"` | A target handle, the label and a source handle. |
| `"output"` | A target handle and the label. |
| `"group"` | A translucent, dashed container with its label in the corner and no handles - the usual parent of a sub-flow. |

Without a `NodeTemplate`, any other `Type` value is drawn like `"default"`. The built-in nodes show `Label`, or the
id when there is no label, and place their handles on `TargetPosition` and `SourcePosition`. Every node element
carries the classes `bit-blow-node` and `bit-blow-node-{Type}`, so a type can be restyled with CSS alone.

### Custom nodes

`NodeTemplate` replaces the content of every node. Switch on `node.Type` inside it and hand the rest back to the
built-in renderer with `BlowDefaultNode`:

```razor
<BlowCanvas Nodes="_nodes" Edges="_edges" FitViewOnInit="true">
    <NodeTemplate Context="node">
        @if (node.Type == "task" && node.Data is TaskData task)
        {
            <BlowHandle Type="BlowHandleType.Target" Position="BlowPosition.Left" />
            <div class="task-node">
                <strong>@node.Label</strong>
                <input class="nodrag" @bind="task.Owner" placeholder="Owner" />
            </div>
            <BlowHandle Type="BlowHandleType.Source" Position="BlowPosition.Right" Id="done" />
            <BlowHandle Type="BlowHandleType.Source" Position="BlowPosition.Bottom" Id="failed" />
        }
        else
        {
            <BlowDefaultNode Node="node" />
        }
    </NodeTemplate>
    <ChildContent>
        <BlowBackground />
        <BlowControls />
    </ChildContent>
</BlowCanvas>

@code {
    public sealed class TaskData { public string Owner { get; set; } = ""; }

    private List<BlowNode> _nodes =
    [
        new BlowNode { Id = "start", Type = "input", Label = "Start", Position = new(0, 40) },
        new BlowNode { Id = "t1", Type = "task", Label = "Check stock", Position = new(200, 0), Data = new TaskData() },
        new BlowNode { Id = "ok", Type = "output", Label = "Ship", Position = new(450, 0) },
    ];

    private List<BlowEdge> _edges =
    [
        new BlowEdge { Id = "e1", Source = "start", Target = "t1" },
        new BlowEdge { Id = "e2", Source = "t1", SourceHandle = "done", Target = "ok" },
    ];
}
```

A few things are worth knowing:

- Once the canvas has a `NodeTemplate`, the add-ons go in an explicit `<ChildContent>`, as above.
- The template renders inside the node element, which keeps the stylesheet's node chrome: padding, a border, a
  white background and an 80px minimum width. Restyle it per type with `.bit-blow-node-task`, or per node with
  `Class` and `Style`. An explicit `Width` or `Height` also lifts the minimum.
- Place `BlowHandle` anywhere in the template; see [handles](#handles). A node with several handles of the same
  type needs an `Id` on each, and edges name them through `SourceHandle` and `TargetHandle`.
- Inputs, sliders and selectable text belong in an element marked `nodrag`, so pressing them does not drag the
  node; see the [escape hatches](#interaction-escape-hatches). Graph keys such as Backspace are ignored while
  focus is in a text field, so typing is safe.
- The template is part of your component, so bindings in it update your state and re-render your component, as
  any other markup would.

Demo: `/examples/custom-node`, `/examples/default-node-overwrite`, `/examples/editable-node`.

### Node size and measurement

A node with an explicit `Width` and `Height` is that size. Otherwise it sizes to its content, and the browser layer
measures it - along with the position of every handle inside it - after it renders. Until then the canvas assumes
`BlowNode.DefaultWidth` by `BlowNode.DefaultHeight` (150 by 40), so a node that has never been on screen still has
usable bounds.

Re-measuring happens on its own when a node changes size (a `ResizeObserver` watches every node) and when nodes or
handles are added or removed. What the browser cannot report is a handle that moved without anything resizing - a
handle switched to another side, or moved by a style change. After such a change call:

```csharp
_flow.UpdateNodeInternals();
```

The browser reads every node in one pass, which is cheap, and sends across only what changed since it last did;
`UpdateNodeInternals("a", "b")` has the named nodes sent whatever it remembers, and with none named every node is.
`Refresh(remeasure: true)` sends every node too.

`OnNodesInitialized` is raised once every visible node has been measured, and again whenever that becomes true
after nodes were added; `NodesInitialized` reports the same thing as a property. It is the cue to run a layout that
needs real sizes.

### Node origin

By default a node's `Position` is its top-left corner. `NodeOrigin` on the canvas changes which point it names -
`new BlowXYPosition(0.5, 0.5)` positions nodes by their centre, `(1, 1)` by their bottom-right - and a node's own
`Origin` overrides it. The layout, align and clipboard helpers take the same origin, so they keep working on such a
canvas.

### Sub-flows and grouping

Give a node a `ParentId` and it becomes a child: its `Position` is relative to the parent's top-left, it moves when
the parent moves, and it is drawn above its parent.

```csharp
_nodes =
[
    new BlowNode { Id = "A", Type = "group", Label = "Group A", Position = new(0, 0), Width = 320, Height = 220 },
    new BlowNode { Id = "A-1", Label = "Child A1", Position = new(20, 40), ParentId = "A", ExtentParent = true },
    new BlowNode { Id = "A-2", Label = "Child A2", Position = new(160, 120), ParentId = "A", ExtentParent = true },
    new BlowNode { Id = "out", Type = "output", Label = "Output", Position = new(250, 320) },
];
```

- Any node can be a parent; the `group` type is simply the built-in look for a container. Groups can nest.
- Dragging a selection that contains both a parent and its children moves the children once, with the parent.
- With the default `ZIndexMode.Auto`, deeper nodes are drawn after shallower ones, so a child is never hidden
  behind its own group. In `Basic` and `Manual` mode the list order decides, so keep parents before children.
- Edges are drawn beneath every node, children included. A group with an opaque background hides the edges running
  inside it, which is why the built-in group is translucent.
- A `ParentId` naming a node that is not in the list is reported through `OnError`, and the node is laid out as a
  root. A parent cycle (a node that is, through its parents, its own ancestor) is reported as `parent-cycle` and
  cut where it was found. A chain deeper than 50 levels is cut off there.

To group from the UI rather than in data, `GroupSelectionAsync` wraps the selected nodes in a new `group` node sized
to enclose them, and `UngroupAsync` dissolves one again:

```csharp
BlowNode? group = await _flow.GroupSelectionAsync(padding: 20);   // null when nothing could be grouped
IReadOnlyList<BlowNode> children = await _flow.UngroupAsync(group);
```

Nothing moves on screen either way: each node's `Position` is rewritten relative to its new parent. Grouping takes
the outermost selected nodes (a selected child goes with its selected parent) and refuses a selection spread across
different parents, since that would move nodes between coordinate spaces. Pass your own node to
`GroupSelectionAsync(group)` for a custom type or id. Ungrouping hands the children to the group's own parent and
removes the group with any edges attached to it - a deletion like any other, so a group that is not `Deletable` is
left alone, `OnBeforeDelete` can veto it and `OnNodesDelete` reports it. Positions are worked out from each parent's
position point, so any `NodeOrigin` works. Both are one undo step (`group`, `ungroup`).

Demo: `/examples/subflow`, `/examples/grouping`.

### Constraining where a node can go

| Setting | Effect |
| --- | --- |
| `BlowNode.ExtentParent = true` | The child cannot leave its parent's box (React Flow's `extent: 'parent'`). |
| `BlowNode.Extent = new BlowRect(...)` | The node cannot leave this rectangle, written in its own coordinate space (parent-relative for a child). |
| `BlowCanvas.NodeExtent` | A rectangle no root node may leave. It does not apply to children, whose coordinates are in another space. |
| `BlowNode.ExpandParent = true` | Dragging the child past its parent's edge grows the parent instead. Ignored when `ExtentParent` is set. |

`ExtentParent` wins over `Extent`, which wins over `NodeExtent`. The same limits apply to keyboard nudges, to align
and distribute, and to resizing: a node pinned inside its parent cannot be resized out of it either. Growing a
parent leftwards or upwards moves the parent's origin and shifts its other children back, so nothing on screen
jumps. Demo: `/examples/node-extent`.

### Resizing

Drop a `BlowNodeResizer` into a node's template and the node gets resize controls while it is selected:

```razor
<NodeTemplate Context="node">
    <BlowNodeResizer MinWidth="80" MinHeight="50" KeepAspectRatio="@(node.Type == "image")" />
    <BlowHandle Type="BlowHandleType.Target" Position="BlowPosition.Left" />
    <div style="display:flex;align-items:center;justify-content:center;height:100%">@node.Label</div>
    <BlowHandle Type="BlowHandleType.Source" Position="BlowPosition.Right" />
</NodeTemplate>
```

| Parameter | Default | Meaning |
| --- | --- | --- |
| `Visible` | `null` | Null shows the controls only while the node is selected; true or false forces them. |
| `MinWidth`, `MinHeight` | 40, 30 | Smallest size, in flow units. |
| `MaxWidth`, `MaxHeight` | unbounded | Largest size. |
| `KeepAspectRatio` | `false` | The dragged axis drives the other one. |
| `Color` | `var(--bit-blow-node-selected)` | Colour of the corner squares and outline. |
| `ShowEdgeControls` | `true` | Also resize from the four sides, which are grabbed along their whole length. |

Resizing writes `Width` and `Height`, and `Position` when the left or top edge is dragged so the opposite edge stays
put. With `SnapToGrid` on the size snaps to the grid. Escape puts the node back as it was. `OnNodeResizeStart`,
`OnNodeResize` and `OnNodeResizeEnd` report the gesture, `NodesChanged` follows the end, and the built-in history
records it as one step.

The side strips run along the whole edge rather than sitting as squares at the midpoints, because the midpoints are
exactly where connection handles are: a square there would turn every attempt to connect into a resize.

`BlowNodeResizeControl` is a single control instead of the full set - `Position` takes a `BlowResizeDirection`
(`BottomRight` by default), and its `ChildContent` replaces the default square. Give the template's content
`height: 100%` if it should fill the resized node. Demo: `/examples/node-resizer`.

### Node toolbars

`BlowNodeToolbar` renders controls anchored to a node that keep a constant size on screen at any zoom:

```razor
<NodeTemplate Context="node">
    <BlowNodeToolbar Position="BlowPosition.Top" Align="BlowToolbarAlign.Center">
        <button @onclick="() => Duplicate(node)">Duplicate</button>
        <button @onclick="() => Remove(node)">Delete</button>
    </BlowNodeToolbar>
    <BlowDefaultNode Node="node" />
</NodeTemplate>
```

`Visible` (null: only while the node is selected), `Position` (the side: `Top` by default), `Align` (`Center`,
`Start` or `End` along that side) and `Offset` (the gap in screen pixels, 8 by default) place it. A press inside it
does not start a drag of the node or a pan. Demo: `/examples/node-toolbar`.

### Drag handles

Set `UseCustomDragHandle = true` on a node and its body stops starting drags; only a `BlowDragHandle` inside its
template does. The body can then host content that needs the pointer for itself:

```razor
<div class="card">
    <BlowDragHandle Class="grip">::</BlowDragHandle>
    <div class="body">@node.Label</div>
</div>
```

Pressing the body still selects the node. Demo: `/examples/drag-handle`.

### Hiding and collapsing

A node with `Hidden = true` is not drawn and takes no part in selection, box selection, select-all, connecting,
fit-view (unless `BlowFitViewOptions.IncludeHiddenNodes` is set), alignment guides, the minimap, keyboard nudges or
align and distribute. Its edges are hidden with it: an edge with one end gone has nothing to be a line between, so
folding away a branch takes one flag per node and no bookkeeping over the edge list. The layout helpers do not skip
hidden nodes - pass them only the nodes you want arranged (`LayoutAsync` does skip them). Demo: `/examples/hidden`.

A node with children can be collapsed instead, which keeps its edges to the outside:

```csharp
await _flow.SetCollapsedAsync(group);          // toggle; pass true or false to set
```

Collapsing sets the node's `Collapsed` and hides everything nested in it, deselecting it so the delete key cannot
remove what nobody can see. An edge that ran into a hidden child from outside is drawn to the collapsed node instead,
ending on its side; an edge between two of its children is hidden with them, and leaves the selection. Expanding
shows the children again, all but those inside a nested node that is itself still collapsed - and none at all while
the node being expanded is itself folded away inside another: its children come back with it. Either way it is one undo step (`collapse`,
`expand`), announced to a screen reader ("Services collapsed, 4 nodes hidden"), and a collapsed node is described as
collapsed. The node gets the class `collapsed` for styling; its size is left alone, so shrink it yourself if a
collapsed group should take less room. `Collapsed` is saved by `BlowSerializer` and compared by the history; it has no
React Flow counterpart, so an export to React Flow keeps the children hidden but not the redirected edges. Demo:
`/examples/layouts-and-collapsing`.

### Stacking order

With `ElevateNodesOnSelect` (on by default) a selected node is lifted above its peers, and a dragged node is lifted
above the rest so it is never lost behind the nodes it is pulled across. `ElevateEdgesOnSelect` (off) does
the same for edges. `ZIndexMode` decides how much of this the canvas manages:

| `BlowZIndexMode` | Behaviour |
| --- | --- |
| `Auto` (default) | Selection and dragging lift, and children are drawn above their group. |
| `Basic` | Selection and dragging lift; nesting order is left to the list. |
| `Manual` | Nothing is lifted, not even a dragged node; only the `ZIndex` you set counts. |

An explicit `ZIndex` on a node or an edge always wins. Edges live in one SVG layer beneath all nodes, so an edge's
`ZIndex` orders it among the edges only. Demo: `/examples/zindex-mode`.

`BringToFrontAsync(nodes)` and `SendToBackAsync(nodes)` - the selection when no nodes are given - change the list
order itself: the nodes, with everything nested in them, move to the end or the start of `Nodes`, keeping their
order among themselves. A child sent to the back goes to the back of its group - just after its parent - since
before the parent it would be drawn beneath it. That is the order `Basic` and `Manual` mode draw by, and the order among peers in `Auto`
mode; an explicit `ZIndex` still overrides it. The reorder is one undo step (`order`).

## Handles and connections

### Handles

A `BlowHandle` is a connection point on a node. The built-in node types place their own; in a `NodeTemplate` you
place them yourself.

| Parameter | Default | Meaning |
| --- | --- | --- |
| `Type` | `Source` | `BlowHandleType.Source` (connections start here) or `Target` (they end here). |
| `Position` | `Bottom` | The side it sits on, which also decides which way an edge leaves it. |
| `Id` | `null` | Required when a node has more than one handle of the same type. |
| `IsConnectable` | `true` | False: neither starts nor accepts connections. |
| `IsConnectableStart` | `true` | False: a connection cannot be started here. |
| `IsConnectableEnd` | `true` | False: a connection cannot be dropped here. |
| `IsValidConnection` | `null` | A rule of this handle's own; see [validation](#validation). |
| `DataType` | `null` | The kind of value it carries, overriding its declared port's; see [ports](#ports-typed-inputs-and-outputs). |
| `MaxConnections` | `null` | The most edges it may have, overriding its declared port's. |
| `OnConnect` | - | Raised when a connection touching this handle is made. |
| `Class`, `Style` | `null` | Extra CSS on the handle element. |

The stylesheet centres a handle on its side as an 8px dot; `Style="left: 25%"` moves it along the side, and a
handle placed inside a positioned element of your markup (`position: relative`) sits on that element's side
instead - one way to give each row of a node its own port. Wherever it ends up, the measured position is what edges
anchor to.

An edge names its handles by id through `SourceHandle` and `TargetHandle`. An edge that names none uses the handle
without an id. When the node has no matching handle at all, the edge is anchored to the middle of the node's
`SourcePosition` or `TargetPosition` side - which is also what happens before the first measurement. Two handles of
the same type with the same id (or both without one) are indistinguishable to an edge, so the canvas reports
`duplicate-handle-id` through `OnError`. A handle outside a node template renders as an inert dot.

### Ports: typed inputs and outputs

A handle is markup; a port is its model. Declare a node's ports in `BlowNode.Ports` and the rules travel with the
data - the canvas enforces them while the user connects, and `BlowGraphValidator` checks them with no UI at all:

```csharp
new BlowNode
{
    Id = "sum", Type = "add", Label = "Add",
    Ports =
    [
        new BlowPort { Id = "a", Type = BlowHandleType.Target, DataType = "number", MaxConnections = 1, MinConnections = 1 },
        new BlowPort { Id = "b", Type = BlowHandleType.Target, DataType = "number", MaxConnections = 1, MinConnections = 1 },
        new BlowPort { Id = "result", Type = BlowHandleType.Source, DataType = "number", Label = "Result" },
    ],
}
```

| `BlowPort` | Default | Meaning |
| --- | --- | --- |
| `Id`, `Type` | `null`, `Source` | Which handle the port stands for: an edge's `SourceHandle` or `TargetHandle`. |
| `Position` | `null` | The side the built-in node types draw it on; null uses the node's `SourcePosition` or `TargetPosition`. |
| `Label` | `null` | The handle's tooltip and name. |
| `DataType` | `null` | What it carries. Null, empty or `"*"` accepts anything; `"int,float"` lists several; case is ignored. |
| `MaxConnections` | `null` | The most edges it may have. At the limit it neither accepts nor starts connections, and is not drawn as connectable. |
| `MinConnections` | 0 | The fewest it needs. Not enforced while editing - a graph is built one edge at a time - but reported by validation. |
| `Data` | `null` | Anything of yours: a default value, a schema. |

The built-in node types draw one handle per declared port, spread evenly along each side. In a `NodeTemplate`, a
`BlowHandle` with the port's `Type` and `Id` picks up its definition - its data type is rendered as
`data-port-type`, so `.bit-blow-handle[data-port-type="number"] { --bit-blow-handle-bg: #2563eb; }` colours every
number port - and a handle's own `DataType` and `MaxConnections` parameters override the port's, for templates
that would rather say it in markup. A node without ports behaves exactly as before.

Whether two types are compatible is decided by `BlowConnectionRules.AreTypesCompatible`; set
`ConnectionTypeCompatibility` on the canvas to replace it - to let an `int` flow into a `float`, say. Demo:
`/examples/typed-ports`.

### Making a connection

Drag from a handle and release on another. Releasing within `ConnectionRadius` (20 screen pixels) of a valid handle
snaps to the nearest one, so the user does not have to hit an 8px dot. The line appears once the pointer has moved
`ConnectionDragThreshold` (1 pixel), so a click on a handle does not flash a line. With `ConnectOnClick` a
connection can also be made by clicking one handle and then another; clicking empty space or pressing Escape
cancels it. Dragging a connection past the edge of the pane scrolls it (`AutoPanOnConnect`).

When the connection is valid the canvas itself appends a new `BlowEdge` to `Edges` - you do not add it in
`OnConnect`, as you would with React Flow's `addEdge`. The new edge:

- gets a deterministic id derived from the wiring (see `BlowGraphUtils.BuildEdgeId`), with a numeric suffix when
  that id is already taken;
- runs from the source-side handle to the target-side one, whichever end the drag started from;
- takes `DefaultEdgeType` and then [`DefaultEdgeOptions`](#defaults-for-new-edges).

Then `OnConnect` (with a `BlowConnection`: `Source`, `Target`, `SourceHandle`, `TargetHandle`), `EdgesChanged` and
the `OnConnect` of both handles are raised, and the built-in history records a step.

### Connection rules

A prospective connection is refused, for the reason in brackets (a `BlowConnectionRejection`), when:

- either node does not exist (`MissingNode`), or the canvas's `NodesConnectable` or either node's `Connectable` is
  off, or a node is hidden (`NotConnectable`);
- `ConnectionMode` is `Strict` (the default) and both handles have the same type - `Loose` lets any handle connect
  to any other (`SameHandleType`);
- it would connect a node to itself, unless `AllowSelfConnections` is on - and even then it must join two
  different handles (`SelfConnection`);
- an identical edge already exists and `PreventDuplicateEdges` is on, the default (`Duplicate`);
- the two ports carry incompatible [data types](#ports-typed-inputs-and-outputs) (`IncompatibleTypes`);
- the source's or the target's port is at its `MaxConnections` (`SourceFull`, `TargetFull`);
- `PreventCycles` is on and the edge would close a directed cycle - a self-connection is one (`Cycle`);
- the canvas's `IsValidConnection`, or the rule of either handle, returns false (`RejectedByRule`);
- the drop handle has `IsConnectableEnd` (or `IsConnectable`) off.

The checks run in that order and the first that fails is the reason, so the application's own rules only ever see
connections that are otherwise allowed. When a drop is refused - on a handle, or near one with no valid handle
nearby - the reason is announced to screen readers (`AriaLabels.ConnectionRejected`) and carried by
`BlowConnectEndInfo.Rejection`; while dragging, `BlowConnectionLineContext.Rejection` says why the handle the line
ends at would refuse it, and `CheckConnection(nodeId, handleId, type)` answers for any handle. A connection that is
made is announced too (`AriaLabels.Connected`).

The rules themselves are `BlowConnectionRules`, a pure function over the graph, so the same decision can be made
from code, on a server or in a test:

```csharp
var check = BlowConnectionRules.Check(nodes, edges, new BlowConnection("a", "b", "out", "in"),
    new BlowConnectionPolicy { PreventCycles = true });
if (!check.IsValid) Console.WriteLine(check.Rejection);
```

`BlowConnectionRules.Begin(graph, nodeId, handleId, type, policy)` binds them to one starting handle for checking
many candidates cheaply - what the canvas does once per drag. Demo: `/examples/connection-rules`,
`/examples/connection-mode`, `/examples/typed-ports`.

### Confirming a connection asynchronously

`OnBeforeConnect` is a last word on a connection the user has just completed - asking a server whether they may
make it, for instance. It runs once, when the user lets go on a valid handle; the line disappears at once, and the
edge is added (or the reconnected edge rewired) only if it returns true. The graph may change while it runs, so the
connection is checked against every rule again when it returns and dropped if it is no longer allowed; either way
`OnConnectEnd` reports the outcome, with `Rejection` set to `RejectedByRule` for a refusal. An exception it throws
reaches the renderer like one from an event handler. Rules a connection must pass *while it is dragged* belong in
`IsValidConnection`, which is synchronous because it runs for every handle on every frame.

### Validation

`IsValidConnection` on the canvas is one predicate for the whole flow. It receives the `BlowConnection` that
completing would produce:

```razor
<BlowCanvas Nodes="_nodes" Edges="_edges" IsValidConnection="IsValid" />

@code {
    // Only an "out" handle may feed an "in" handle, and never into the start node.
    private bool IsValid(BlowConnection c) =>
        c.SourceHandle == "out" && c.TargetHandle == "in" && c.Target != "start";
}
```

A rule can also live on the handle it belongs to, which reads better when each port has its own type:

```razor
<BlowHandle Type="BlowHandleType.Target" Position="BlowPosition.Left" Id="value"
            IsValidConnection="@(c => c.SourceHandle == "number")"
            OnConnect="@(c => _log = $"{c.Source} connected")" />
```

Both ends get a say: the rule of the handle being dropped on and the rule of the handle the drag started from are
both consulted, together with the canvas's. While a connection is being dragged every handle shows the verdict: one
that would accept it gets `bit-blow-handle-valid` (green), one that would not gets `bit-blow-handle-invalid` (red),
and the handle the drag came from gets `bit-blow-handle-connectingfrom`. Releasing on a rejected handle connects
nothing. The rules run for every handle on every frame of the drag, so keep them cheap.

Demo: `/examples/validation`, `/examples/handle-rules`.

### Connection events

| Event | Payload | Raised |
| --- | --- | --- |
| `OnConnectStart` | `BlowConnectionStartInfo` (`NodeId`, `HandleId`, `HandleType`) | A connection drag begins. |
| `OnConnectionChange` | `BlowConnectionLineContext?` | At the start, on every move, and once with `null` at the end - React Flow's `useConnection`. |
| `OnConnect` | `BlowConnection` | A new edge was made. |
| `OnConnectEnd` | `BlowConnectEndInfo` | The gesture ended, whether or not it connected, with the `Rejection` when a handle refused it. |

`BlowConnectEndInfo` carries the `Start` handle, the final `FlowPosition`, `ClientX`/`ClientY`, `IsValid` (it
connected), `IsDropped` (released after a real drag, rather than a click, Escape or a cancelled pointer) and
`IsReconnect`. Adding a node where a connection was dropped on empty space is then a few lines:

```csharp
private int _count;

private void OnConnectEnd(BlowConnectEndInfo info)
{
    // Only a new connection, dragged out of a source handle and let go on empty space.
    // /examples/add-node-on-edge-drop also handles drags that start from a target handle.
    if (info.IsValid || !info.IsDropped || info.IsReconnect) return;
    if (info.Start.HandleType != BlowHandleType.Source) return;

    var id = $"n{++_count}";
    _nodes.Add(new BlowNode
    {
        Id = id,
        Label = $"Node {_count}",
        Position = new(info.FlowPosition.X - BlowNode.DefaultWidth / 2, info.FlowPosition.Y),
    });
    _edges.Add(new BlowEdge
    {
        Id = $"e-{id}",
        Source = info.Start.NodeId,
        SourceHandle = info.Start.HandleId,
        Target = id,
    });
}
```

Demo: `/examples/add-node-on-edge-drop`, `/examples/use-connection`.

### Reconnecting an edge

Select an edge and a small dot appears at each end; drag one onto another handle to move that end. The grab area is
`ReconnectRadius` screen pixels (10) around the dot. It is on unless `EdgesReconnectable` on the canvas or
`Reconnectable` on the edge is off, and needs `ElementsSelectable`.

The edge is rewired in place and keeps its `Id`. `OnReconnectStart` (`BlowEdgeReconnectStart`: `Edge`, `MovingEnd`)
opens the gesture, `OnReconnect` (`BlowEdgeReconnect`: `Edge`, `Connection`) reports the new wiring, and
`OnReconnectEnd` (`BlowEdgeReconnectEnd`: `Edge`, `MovingEnd`, `Rewired`) closes it either way. `Rewired` false means
the end was dropped somewhere invalid, which is the usual cue to delete the edge:

```csharp
private void OnReconnectEnd(BlowEdgeReconnectEnd end)
{
    if (!end.Rewired) _edges.Remove(end.Edge);
}
```

Dropping an end back where it came from is allowed; the duplicate check ignores the edge being moved. An end can be
moved from the keyboard too, with the same events: see [connecting from the keyboard](#connecting-from-the-keyboard).
Demo: `/examples/reconnect-edge`.

### The connection line

The line drawn while connecting follows `ConnectionLineType` (`Bezier` by default) and can be restyled with
`ConnectionLineStyle` (an inline style such as `"stroke:#f6ab6c;stroke-width:3"`) and `ConnectionLineClass`. For
full control, `ConnectionLineTemplate` draws it yourself as SVG from a `BlowConnectionLineContext`: `FromNode`,
`FromHandle`, `FromHandleType`, `FromPosition`, `From`, `To`, `ToPosition` and `IsValid`.

```razor
<ConnectionLineTemplate Context="line">
    <path d="@(BlowEdgePath.Compute(BlowEdgeType.SmoothStep, line.From.X, line.From.Y, line.FromPosition, line.To.X, line.To.Y, line.ToPosition).Path)"
          fill="none" stroke="@(line.IsValid ? "#16a34a" : "#6366f1")" stroke-width="2" />
</ConnectionLineTemplate>
```

Demo: `/examples/custom-connection-line`, `/connection-line`.

## Edges

### The edge model

| Property | Default | Meaning |
| --- | --- | --- |
| `Id`, `Source`, `Target` | required | The edge's id and the ids of the nodes it joins. |
| `SourceHandle`, `TargetHandle` | `null` | Ids of the specific handles. |
| `Type` | `Bezier` | The path algorithm. |
| `PathOptions` | `null` | Per-edge tuning of that algorithm. |
| `Label` | `null` | Text at the path's midpoint; also the accessible name. |
| `LabelStyle`, `LabelShowBg`, `LabelBgStyle`, `LabelBgPaddingX`, `LabelBgPaddingY`, `LabelBgBorderRadius` | `null`, `true`, `null`, 4, 2, 3 | How the label and its backing plate look. |
| `Animated` | `false` | A moving dash, the usual way of showing flow. |
| `Stroke`, `StrokeWidth`, `Style`, `Class` | theme colour, 1.5, `null`, `null` | Appearance of the path and the edge group. |
| `MarkerEnd`, `MarkerStart` | `true`, `false` | Arrowheads at either end. |
| `MarkerType`, `MarkerColor`, `MarkerWidth`, `MarkerHeight` | `Arrow`, stroke colour, 7, width | The arrowhead's shape, colour and size. |
| `InteractionWidth` | 14 | Width of the invisible path that makes the edge easy to click, in screen pixels at any zoom. |
| `Selected` | `false` | Maintained by the canvas. |
| `Selectable`, `Focusable`, `Deletable`, `Reconnectable` | `true` | Opt-outs per edge. An edge is still removed when a node it joins is deleted. |
| `Hidden` | `false` | Not drawn and not interactive. |
| `ZIndex` | `null` | Order among the edges. |
| `Data` | `null` | Any payload of yours. |

A selected edge is drawn in `--bit-blow-edge-selected`, overriding its own stroke.

### Edge types

| `BlowEdgeType` | Path |
| --- | --- |
| `Bezier` (default) | A cubic curve whose control points reach further when the edge has to double back, so it still leaves the node cleanly. |
| `SimpleBezier` | A cubic curve whose control points sit halfway along the handle axis: flatter and more predictable. |
| `Straight` | A straight line between the anchors, ignoring which side each handle is on. |
| `Step` | Orthogonal segments with square corners. |
| `SmoothStep` | Orthogonal segments with rounded corners. |
| `Routed` | Orthogonal segments with rounded corners that go round the nodes in the way - see [routing round nodes](#routing-round-nodes). |

`DefaultEdgeType` sets the type of edges the user creates, and `ConnectionLineType` the line drawn while they
create one. The same algorithms are public as `BlowEdgePath.Bezier`, `SimpleBezier`, `Straight`, `SmoothStep` and
`Compute`, each returning a `BlowPathResult` with the SVG `Path` and the label point. Demo: `/examples/edge-types`,
`/edge-types`.

### Routing round nodes

A `Routed` edge leaves each handle straight, keeps a margin from every node near it - its own two included, so it
never cuts back across them; a node closer to a handle than its stub is kept clear by its own box rather than the
whole margin - and takes the route with the fewest bends that is not much longer, the way a circuit
or a BPMN diagram is drawn. It runs inside the groups its ends are in rather than round them. The canvas's `Routing`
parameter takes the router: a `BlowRoutingOptions` tunes the built-in one - the margin (16), the straight stub out of
a handle (24), what a bend costs in length (60), the corner radius (6) and the most obstacles a route considers
(100). When no route exists - the target walled in - the edge falls back to a smooth step.

Any `IBlowEdgeRouter` can take its place. It is handed a `BlowRouteRequest` - the edge, its two anchors and the sides
they leave through, and the boxes of the nodes near it - and returns the path, or null for a smooth step. One that
draws the built-in router's corners as wide curves:

```csharp
sealed class CurvedRouter(double margin) : IBlowEdgeRouter
{
    public BlowPathResult? Route(BlowRouteRequest r)
    {
        var corners = BlowEdgeRouting.Orthogonal(r.Source, r.SourceSide, r.Target, r.TargetSide, r.Obstacles,
            new BlowRoutingOptions { Margin = margin });
        return corners is null ? null : new BlowPathResult(CurveThrough(corners), labelX, labelY);
    }
}
```

```razor
<BlowCanvas Nodes="_nodes" Edges="_edges" Routing="new CurvedRouter(16)" />
```

The canvas asks again only when the edge's ends or the nodes near it moved, or the router was replaced, so a router
answers the same request the same way; one whose answer depends on something else is replaced with a new instance
when that changes. A `BlowRoutingOptions` can be changed in place: the canvas compares its settings. A router that
throws fails the render, as a template that throws does. The path may use any SVG path commands; the SVG export reads
them all to size the picture. `ToSvg` routes with the canvas's router, and `BlowSvgExportOptions.Routing` takes one
too.

Routes are looked at again whenever a node moves or is resized, not on pan or zoom, and searched again only when the
edge's ends or the nodes near them changed; each considers only the nodes near its two ends, so a few dozen routed
edges are cheap, while on a large graph whose every edge is routed a drag still costs more than with the other
types. The router itself is a pure function:

```csharp
IReadOnlyList<BlowXYPosition>? corners = BlowEdgeRouting.Orthogonal(
    source, BlowPosition.Right, target, BlowPosition.Left, obstacles, new BlowRoutingOptions { Margin = 10 });
BlowPathResult? path = BlowEdgeRouting.Route(source, BlowPosition.Right, target, BlowPosition.Left, obstacles);
```

It is an A* search over the sparse grid formed by the obstacles' margin lines, with every turn charged a penalty;
it uses no randomness, so the same layout always routes the same way. Demo: `/examples/edge-routing` (with the curved
router above to switch to), `/examples/state-machine`.

### Path options

`BlowEdgePathOptions` tunes one edge's path; every value is optional and falls back to the algorithm's default:

| Option | Default | Applies to |
| --- | --- | --- |
| `Curvature` | 0.25 | Bezier. How far the control points reach when the target lies behind the source handle; between handles that face each other the reach is half the distance either way. |
| `BorderRadius` | 5 (0 for `Step`) | Step and smooth step. Corner rounding. |
| `Offset` | 20 | Step and smooth step. How far the edge runs straight out of a handle before turning. |
| `StepPosition` | 0.5 | Step and smooth step. Where the bend sits: 0 hugs the source, 1 the target. |
| `LoopRadius` | 60 | Self-connections. How far the loop stands off its node. |

```csharp
new BlowEdge
{
    Id = "e1", Source = "a", Target = "b", Type = BlowEdgeType.SmoothStep,
    PathOptions = new BlowEdgePathOptions { BorderRadius = 12, Offset = 30, StepPosition = 0.3 },
}
```

The options object is mutable, so each edge needs its own; `Clone()` copies one, and `DefaultEdgeOptions` clones
its options into every new edge. Tuning an edge's options in place is noticed on the next render. Demo:
`/examples/edge-path-options`.

### Self-connections

An edge whose `Source` and `Target` are the same node is drawn as a loop that swings clear of the node, whatever its
`Type` - every ordinary path algorithm would otherwise collapse onto a line through the node body. `LoopRadius`
sets how far out it goes. Such an edge can always be put in the list; the user can only draw one with
`AllowSelfConnections` on, and only between two different handles. Demo: `/examples/self-loop-edges`.

### Labels

`Label` is drawn at the path's midpoint on a rounded plate, inside the zoomed layer, so it scales with the flow.
`LabelStyle` styles the SVG text (`"fill:#111;font-weight:600"`), `LabelShowBg = false` drops the plate,
`LabelBgStyle` styles it (`"fill:#fef3c7"`), and the padding and radius properties shape it. SVG cannot size a
rectangle to its text, and measuring every label would cost a browser round trip per edge per render, so the plate
is sized from an estimate (`BlowTextMetrics.EstimateWidth`); for an unusual font, tune `LabelBgPaddingX`. For a label
that stays a constant size, or that needs HTML, use [HTML labels](#html-labels-and-edge-toolbars). Demo:
`/examples/edge-label-styling`.

### Arrowheads

`MarkerEnd` (on by default) and `MarkerStart` draw arrowheads. `MarkerType` is `BlowMarkerType.Arrow` (open) or
`ArrowClosed` (filled). With no `MarkerColor` the head takes the edge's own stroke colour - almost always right -
unless the canvas's `DefaultMarkerColor` says otherwise. `MarkerWidth` defaults to 7 (8 for a closed head) and
`MarkerHeight` follows the width.

```csharp
new BlowEdge
{
    Id = "e2", Source = "a", Target = "c",
    Stroke = "#94a3b8",
    MarkerType = BlowMarkerType.ArrowClosed,
    MarkerColor = "#e11d48",
    MarkerWidth = 12,
}
```

SVG markers must be declared up front, so the canvas declares one `<marker>` per distinct shape, colour and size in
use: a thousand identical edges share a single definition. Demo: `/examples/edge-markers`.

### Custom edges

`EdgeTemplate` replaces how every edge is drawn. It receives a `BlowEdgeContext` and renders SVG; switch on
`ctx.Edge.Type`, `ctx.Edge.Data` or anything else inside. `BlowBaseEdge` draws the standard path, markers and label,
so a custom edge usually only decides its colours:

```razor
<BlowCanvas Nodes="_nodes" Edges="_edges" DefaultEdgeType="BlowEdgeType.SmoothStep">
    <EdgeTemplate Context="ctx">
        <BlowBaseEdge Path="@ctx.Path"
                      Stroke="@(ctx.Edge.Data as string ?? "var(--bit-blow-edge-stroke)")"
                      StrokeWidth="@(ctx.Edge.Selected ? 3 : 1.5)"
                      MarkerEnd="@ctx.MarkerEndUrl"
                      Label="@ctx.Edge.Label"
                      LabelX="@ctx.LabelX"
                      LabelY="@ctx.LabelY" />
    </EdgeTemplate>
    <ChildContent>
        <BlowBackground />
    </ChildContent>
</BlowCanvas>
```

`BlowEdgeContext` holds the `Edge`, the computed `Geometry` (also as `Path`, `LabelX` and `LabelY`), the anchors
`Source` and `Target` (and `SourceX`, `SourceY`, `TargetX`, `TargetY`), the sides `SourcePosition` and
`TargetPosition`, and `MarkerStartUrl`/`MarkerEndUrl` - ready-made `url(#...)` references to the built-in
arrowheads, so a custom edge does not need to know the generated marker ids. Pass them on, as above, or the custom
edge has no arrowheads.

`BlowBaseEdge` takes `Path` (required), `Stroke`, `StrokeWidth` (1.5), `MarkerStart`, `MarkerEnd`,
`InteractionWidth` (14), `Label`, `LabelX`, `LabelY` and `Class`. `BlowEdgeText` draws a label on its own plate at
any point (`X`, `Y`, `Label`, `ShowBg`, `Style`, `BgStyle`, `PaddingX`, `PaddingY`, `BorderRadius`). Around your
markup the canvas still draws the wide invisible click path and the focusable, selectable edge group. Demo:
`/examples/custom-edges`, `/custom-edges`.

### HTML labels and edge toolbars

`EdgeLabelTemplate` renders HTML at each edge's label point in an overlay that is not zoomed, so the label keeps a
constant size and can hold buttons and inputs. When it is set it replaces the built-in SVG labels.
`BlowEdgeToolbar` is a styled container for exactly that, which keeps its clicks from reaching the pane:

```razor
<EdgeLabelTemplate Context="ctx">
    @if (ctx.Edge.Selected)
    {
        <BlowEdgeToolbar>
            <button @onclick="() => _edges.Remove(ctx.Edge)">Delete</button>
        </BlowEdgeToolbar>
    }
</EdgeLabelTemplate>
```

Demo: `/examples/edge-renderer`, `/examples/edge-toolbar`, `/labels-handles`.

### Floating edges

A floating edge attaches to whichever point of each node's border faces the other node, instead of to a fixed
handle. `BlowGraphUtils.GetFloatingEdgeParams` works out the points and sides, and `BlowEdgePath.Compute` turns them
into a path:

```razor
<EdgeTemplate Context="ctx">
    <BlowBaseEdge Path="@FloatingPath(ctx.Edge)" MarkerEnd="@ctx.MarkerEndUrl" />
</EdgeTemplate>

@code {
    private string FloatingPath(BlowEdge edge)
    {
        if (_nodes.Find(n => n.Id == edge.Source) is not { } source
            || _nodes.Find(n => n.Id == edge.Target) is not { } target)
        {
            return string.Empty;
        }

        var (from, fromSide, to, toSide) = BlowGraphUtils.GetFloatingEdgeParams(source, target);
        return BlowEdgePath.Compute(BlowEdgeType.Bezier, from.X, from.Y, fromSide, to.X, to.Y, toSide).Path;
    }
}
```

The geometry meets each node's bounding box, so it suits rectangular nodes. Demo: `/examples/floating-edges`.

### Defaults for new edges

`DefaultEdgeOptions` shapes every edge the user creates; edges you put in the list yourself are left alone.

| `BlowDefaultEdgeOptions` | Default | Meaning |
| --- | --- | --- |
| `Type` | `null` | Path type; null falls back to the canvas's `DefaultEdgeType`. |
| `Animated` | `false` | |
| `MarkerEnd`, `MarkerStart` | `true`, `false` | |
| `MarkerType` | `Arrow` | |
| `PathOptions` | `null` | Cloned into each new edge. |
| `Stroke`, `StrokeWidth`, `Style`, `Class` | theme, 1.5, `null`, `null` | |
| `InteractionWidth` | 14 | |
| `Reconnectable` | `true` | |

```razor
<BlowCanvas Nodes="_nodes" Edges="_edges" DefaultEdgeOptions="_edgeDefaults" />

@code {
    private readonly BlowDefaultEdgeOptions _edgeDefaults = new()
    {
        Type = BlowEdgeType.SmoothStep,
        Animated = true,
        MarkerType = BlowMarkerType.ArrowClosed,
    };
}
```

Demo: `/examples/default-edge-overwrite`.

## Interaction

### Selecting

Pressing a node or an edge selects it and clears the rest of the selection - except that pressing a node that is
already selected keeps the selection, so it can be dragged as a group. Holding `MultiSelectionKey` (Shift) adds to
the selection instead. Clicking empty space clears it; a press there that goes on to pan keeps it, so the graph can
be moved around to see more of it without losing what was selected. Box selection draws a
rectangle while `SelectionKey` (Shift) is held, or on any plain drag of the pane when `SelectionOnDrag` is on - then
holding `PanActivationKey` (Space) pans instead. Holding the multi-selection key while drawing the box adds to the
existing selection; since both keys default to Shift, Shift+drag does exactly that. Set `SelectionKey` to null to
leave box selection to `SelectionOnDrag` alone.

| Parameter | Default | Meaning |
| --- | --- | --- |
| `ElementsSelectable` | `true` | Selection on or off for the whole flow. |
| `SelectionMode` | `Partial` | `Partial` selects a node the box touches; `Full` only one it encloses. |
| `SelectEdgesOnSelectionBox` | `true` | The box also selects edges whose two nodes it selects. |
| `SelectNodesOnDrag` | `true` | Pressing a node selects it. Off, pressing a member of a selection keeps the selection, so one node can be nudged without collapsing it. |
| `SelectAllKeyEnabled` | `true` | Ctrl/Cmd+A selects every selectable, visible node and edge. |

`OnSelectionChange` (a `BlowSelectionChange` with `Nodes` and `Edges`) is raised only when the selected set really
changes, so handling it is cheap. `OnSelectionStart` and `OnSelectionEnd` bracket a box. `SelectAllAsync()` selects
everything from code. Setting `Selected` yourself changes what is drawn, but raises no `OnSelectionChange`. Demo:
`/examples/use-on-selection-change`, `/examples/selection-gestures`.

### Dragging nodes

Pressing a node and moving more than `NodeDragThreshold` screen pixels (1) starts a drag. Dragging a node that is
part of the selection moves the whole selection; dragging one that is not moves only that node. A press that
travels further than `NodeClickDistance` (3) is not reported as `OnNodeClick`, and a pan that travels further than
`PaneClickDistance` (3) is not reported as `OnPaneClick`.

`OnNodeDragStart`, `OnNodeDrag` and `OnNodeDragStop` report the node the pointer grabbed. When more than one node is
moving, `OnSelectionDragStart`, `OnSelectionDrag` and `OnSelectionDragStop` report all of them as a
`BlowSelectionMouseEvent`, so a listener can treat the group as one edit. `NodesChanged` follows a drag that
actually moved something. Escape, or the browser cancelling the pointer, puts every node back where the drag found
it - and shrinks back a parent the drag grew - and still raises the stop events, so every start has its stop. The
pointer is captured for the whole gesture, so a drag survives the cursor leaving the canvas. Demo:
`/examples/click-distance`, `/drag-threshold`.

### Snapping to a grid

`SnapToGrid` snaps dragged positions to a `SnapGridX` by `SnapGridY` grid (15 by 15 flow units). The same grid
makes each arrow-key press move one cell and makes resizing snap the size. Show the grid with a background of the
same gap:

```razor
<BlowCanvas Nodes="_nodes" Edges="_edges" SnapToGrid="true" SnapGridX="20" SnapGridY="20">
    <BlowBackground Variant="BlowBackgroundVariant.Lines" Gap="20" />
</BlowCanvas>
```

When [alignment guides](#alignment-guides) are on too, a guide wins on whichever axis it found an alignment and the
grid keeps the other.

### Panning and zooming input

| Parameter | Default | Meaning |
| --- | --- | --- |
| `PanOnDrag` | `true` | Dragging the background pans. |
| `PanButtons` | `[0]` | Mouse buttons that pan: 0 left, 1 middle, 2 right. |
| `PanActivationKey` | `" "` (Space) | Held, turns any drag on the pane into a pan, even with `SelectionOnDrag`. Null disables it. |
| `ZoomOnScroll` | `true` | The wheel zooms toward the cursor. |
| `ZoomActivationKey` | `null` | A modifier that must be held for the wheel to zoom; without it a plain scroll reaches the page. |
| `PanOnScroll` | `false` | The wheel pans instead (Ctrl+wheel still zooms). Shift turns a vertical scroll horizontal. |
| `PanOnScrollMode` | `Free` | Restrict wheel panning to `Horizontal` or `Vertical`. |
| `PanOnScrollSpeed` | 0.5 | Multiplier for wheel panning. |
| `ZoomOnPinch` | `true` | Two fingers pinch-zoom about their midpoint - the only way to zoom on a touch screen. |
| `ZoomOnDoubleClick` | `true` | Double-clicking the pane zooms in; Shift+double-click zooms out. |
| `PreventScrolling` | `true` | A wheel the canvas acts on does not also scroll the page. |

A wheel zoom is proportional to how far the wheel moved, so a trackpad's stream of small deltas zooms as fast as a
mouse's notches, and no single event zooms by more than about 15%. Ctrl+wheel - how browsers report a trackpad
pinch - always zooms. A wheel over a panel, the control bar or a minimap that does not zoom scrolls the page as
ordinary content would.

```razor
<BlowCanvas Nodes="_nodes" Edges="_edges" SelectionOnDrag="true" PanButtons="_panButtons" />

@code {
    // Left-drag draws a selection box; the middle button pans.
    private readonly int[] _panButtons = [1];
}
```

Demo: `/examples/interaction`, `/examples/key-bindings`, `/examples/touch-device`.

### Auto-pan

Dragging a node or a connection to within `AutoPanMargin` screen pixels (30) of the pane's border scrolls the
viewport by `AutoPanSpeed` pixels (15) about sixty times a second, and keeps going while the pointer is held still
against the edge. `AutoPanOnNodeDrag` and `AutoPanOnConnect` (both on) control it. `AutoPanOnNodeFocus` (on) pans a
node that receives keyboard focus into view, over `AutoPanOnNodeFocusDuration` milliseconds (200) - only as far as
needed, so tabbing through a large graph does not re-centre the view at every step. Demo: `/examples/auto-pan`.

### Deleting

Delete or Backspace (`DeleteKeys`) remove the selection while `DeleteKeyEnabled` is on; set `DeleteKeys` to just
`["Delete"]` when Backspace should always mean "go back". Elements with `Deletable = false` stay, but an edge always
goes with a node it is attached to, and a node always goes with the parent it is nested in. `OnBeforeDelete` can
veto the whole deletion; it is synchronous and sees the cascaded nodes and edges too:

```razor
<BlowCanvas Nodes="_nodes" Edges="_edges" OnBeforeDelete="CanDelete" OnDelete="OnDeleted" />

@code {
    private bool CanDelete(IReadOnlyList<BlowNode> nodes, IReadOnlyList<BlowEdge> edges)
        => !nodes.Any(n => n.Type == "input");

    private void OnDeleted(BlowSelectionChange removed)
        => Console.WriteLine($"{removed.Nodes.Count} nodes, {removed.Edges.Count} edges");
}
```

`OnNodesDelete` and `OnEdgesDelete` report each kind separately. From code, `DeleteElementsAsync(nodes, edges)`
deletes with the same rules, veto and events. Demo: `/examples/delete-protection`.

### Interaction escape hatches

Pointer and wheel events inside the canvas normally belong to it. Three classes hand them back, matching React
Flow's `noDragClassName`, `noPanClassName` and `noWheelClassName`:

| Class | Parameter | Effect |
| --- | --- | --- |
| `nodrag` | `NoDragClass` | A press on the element, or inside it, never reaches the canvas: it does not select or drag its node, and does not pan. For inputs, sliders and selectable text. |
| `nopan` | `NoPanClass` | The same guard, named for overlays of your own that must not pan the canvas when dragged. |
| `nowheel` | `NoWheelClass` | The element keeps its wheel events, so it scrolls instead of zooming the flow. |

```razor
<NodeTemplate Context="node">
    <div class="title">@node.Label</div>
    <div class="nodrag">
        <input type="range" @bind="_volume" />
    </div>
    <div class="nowheel" style="overflow-y:auto;max-height:80px">@_log</div>
</NodeTemplate>
```

The guards are listeners in the browser, because only the DOM knows which element an event started on, and by the
time Blazor delivers the event it is too late to opt out. Only elements inside the canvas count, so a page that
wraps the whole flow in an element carrying one of these classes does not switch the flow off. Set a parameter to
null to disable that guard, or to another name if the class clashes with your CSS. Keys typed into an input,
textarea, select or contenteditable element always belong to it. A flow nested inside a node of another flow keeps
its presses, wheels and keys to itself, so it needs no `nodrag` wrapper: using it never drags the node around it,
zooms the outer flow or deletes there. Demo: `/examples/interaction-guards`.

### Locking the canvas

`NodesDraggable`, `NodesConnectable` and `ElementsSelectable` switch dragging, connecting and selecting off for the
whole flow. Bound to the lock button of `BlowControls` they make a read-only view:

```razor
<BlowCanvas Nodes="_nodes" Edges="_edges"
            NodesDraggable="_interactive" NodesConnectable="_interactive" ElementsSelectable="_interactive">
    <BlowControls ShowInteractive="true" @bind-Interactive="_interactive" />
</BlowCanvas>

@code {
    private bool _interactive = true;
}
```

The button only reports the toggle; which parameters it controls is up to you. Panning and zooming stay available.
Demo: `/examples/overlay-layout`.

### Event reference

| Event | Payload | Raised when |
| --- | --- | --- |
| `NodesChanged` | `List<BlowNode>` | The canvas finished changing the node list or its nodes. |
| `EdgesChanged` | `List<BlowEdge>` | The canvas finished changing the edge list or its edges. |
| `OnNodeClick` | `BlowNode` | A node was clicked without dragging. |
| `OnNodeDoubleClick`, `OnNodeContextMenu`, `OnNodeMouseEnter`, `OnNodeMouseMove`, `OnNodeMouseLeave` | `BlowNodeMouseEvent` (`Node`, `Event`) | The matching DOM event on a node. A handled context menu suppresses the browser's. |
| `OnNodeDragStart`, `OnNodeDrag`, `OnNodeDragStop` | `BlowNode` | A node drag begins, moves, ends. |
| `OnSelectionDragStart`, `OnSelectionDrag`, `OnSelectionDragStop` | `BlowSelectionMouseEvent` (`Nodes`, `Event`) | A drag moving more than one node. |
| `OnSelectionContextMenu` | `BlowSelectionMouseEvent` | A right-click on a node that is one of several selected. |
| `OnSelectionStart`, `OnSelectionEnd` | `PointerEventArgs` | A selection box begins, ends. |
| `OnSelectionChange` | `BlowSelectionChange` (`Nodes`, `Edges`) | The selected set changed. |
| `OnEdgeClick` | `BlowEdge` | An edge was clicked. |
| `OnEdgeDoubleClick`, `OnEdgeContextMenu`, `OnEdgeMouseEnter`, `OnEdgeMouseMove`, `OnEdgeMouseLeave` | `BlowEdgeMouseEvent` (`Edge`, `Event`) | The matching DOM event on an edge. |
| `OnPaneClick`, `OnPaneContextMenu`, `OnPaneMouseEnter`, `OnPaneMouseMove`, `OnPaneMouseLeave` | `MouseEventArgs` | The matching DOM event on the empty pane. |
| `OnPaneScroll` | `WheelEventArgs` | Every wheel event over the pane, before it pans or zooms. |
| `OnNodeResizeStart`, `OnNodeResize`, `OnNodeResizeEnd` | `BlowNode` | A resize begins, moves, ends. |
| `OnConnectStart`, `OnConnectionChange`, `OnConnect`, `OnConnectEnd` | see [connection events](#connection-events) | |
| `OnReconnectStart`, `OnReconnect`, `OnReconnectEnd` | see [reconnecting](#reconnecting-an-edge) | |
| `OnNodesDelete`, `OnEdgesDelete`, `OnDelete` | the removed elements | After a deletion. |
| `OnMoveStart`, `OnMove`, `OnMoveEnd`, `OnViewportChanged` | `BlowViewport` | See [controlled and uncontrolled](#controlled-and-uncontrolled-viewports). |
| `OnPaste` | `BlowFragment` | Elements were pasted. |
| `OnHistoryChanged` | `BlowHistoryState` | The built-in history changed. |
| `OnGraphChanged` | `BlowGraphChange` (`Kind`, `Origin`, `Nodes`, `Edges`, `RemovedNodes`, `RemovedEdges`, `Label`) | An edit of any kind completed; see [graph changes and transactions](#graph-changes-and-transactions). |
| `OnInit` | `BlowCanvas` | Once, after the first render and measurement. |
| `OnNodesInitialized` | - | Every visible node has been measured. |
| `OnError` | `BlowError` | Something is wrong with the graph. |

A handled `OnNodeContextMenu`, `OnSelectionContextMenu` or `OnEdgeContextMenu` suppresses the browser's own menu;
`OnPaneContextMenu` does not, so the browser's menu still opens over the empty pane.

The mouse-move, enter, leave, double-click and context-menu listeners on nodes and edges are only attached when you
handle the event, and the pane's pointer-move listener only while a gesture runs. Every DOM event round-trips into
.NET and re-renders, so an idle canvas that listened for everything would cost a great deal for nothing - most of
all over a Blazor Server circuit.

## The viewport

### Coordinates

Nodes live in flow coordinates, which never change when the user pans or zooms. The viewport is a `BlowViewport`
(`X`, `Y`, `Zoom`): screen = flow * `Zoom` + (`X`, `Y`), measured from the pane's top-left. `BlowViewport.Identity`
is no pan and no zoom.

`ScreenToFlowPosition(clientX, clientY)` converts a point in client coordinates - from a drop event, say - into flow
coordinates, and `FlowToScreenPosition` goes back. `BlowViewport.ScreenToFlow` and `FlowToScreen` do the same
relative to the pane. `PaneSize` is the pane's measured size in screen pixels, and `GetViewport()` and `GetZoom()`
read the current transform.

```razor
<div @ondragover:preventDefault="true" @ondrop="OnDrop" style="height: 100%">
    <BlowCanvas @ref="_flow" Nodes="_nodes" Edges="_edges" />
</div>

@code {
    private void OnDrop(DragEventArgs e)
    {
        var at = _flow!.ScreenToFlowPosition(e.ClientX, e.ClientY);
        _nodes.Add(new BlowNode { Id = Guid.NewGuid().ToString("N"), Label = "New", Position = new(at.X - 75, at.Y - 20) });
    }
}
```

Demo: `/examples/drag-n-drop`.

### Fitting the view

`FitViewOnInit` fits the graph once, after the first measurement that finds something to fit - so a graph loaded
after the canvas first rendered is still fitted - using `FitViewOptions` when given. From code, `FitView()` fits
with a 10% margin, `FitView(padding)` with another, and `FitView(options)` with a `BlowFitViewOptions`:

| Option | Default | Meaning |
| --- | --- | --- |
| `Padding` | 0.1 | Fraction of the pane left empty around the content. |
| `IncludeHiddenNodes` | `false` | Count hidden nodes too. |
| `MinZoom`, `MaxZoom` | the canvas's | A narrower zoom range for this fit. |
| `Nodes` | all | Fit only these nodes. |
| `Duration` | 0 | Milliseconds to ease there, for `FitViewAsync`. |

`FitBounds(rect, padding)` fits an arbitrary flow-space rectangle instead.

### Moving the viewport from code

`SetViewport(viewport)` applies a viewport, `ZoomIn()` and `ZoomOut()` zoom by a factor of 1.2 about the pane's
centre, `ZoomTo(zoom)` sets an absolute zoom about the centre, and `SetCenter(x, y, zoom)` centres a flow point,
optionally at a new zoom. Every one of them clamps to `MinZoom` and `MaxZoom` and honours `TranslateExtent`.

### Animated transitions

Every viewport helper has an async twin that eases over a duration in milliseconds:

```csharp
await _flow.FitViewAsync(new BlowFitViewOptions { Padding = 0.15, Duration = 600 });
await _flow.FitViewAsync(600);
await _flow.SetCenterAsync(x, y, zoom: 1.2, durationMs: 400);
await _flow.ZoomToAsync(2, durationMs: 300);
await _flow.SetViewportAsync(saved, durationMs: 500);
await _flow.FitBoundsAsync(rect, padding: 0.1, durationMs: 400);
```

Jumping between two far-apart parts of a graph loses the reader; a short eased transition tells them which way they
travelled. A new request cancels the one in flight, so the last one wins rather than two fighting over the same
transform, and a transition is abandoned the moment the user pans, pinches or scrolls - it would otherwise fight
the pointer. A duration of 0 applies the viewport at once, and the synchronous helpers never animate.

Motion makes some people unwell, which is what the operating system's "reduce motion" setting is for. `ReducedMotion`
(`BlowReducedMotion.System` by default) follows it: with it on, every transition jumps straight to its destination,
and animated edges keep their dash but stop marching. `Always` reduces motion regardless, `Never` animates regardless,
and `ReducesMotion` says which is in force. Demo: `/examples/viewport-transitions`.

### Controlled and uncontrolled viewports

Left alone, the canvas owns its viewport; `DefaultViewport` sets where it starts. It is ignored when `Viewport` is
set, and `FitViewOnInit` replaces it with a fit.

To drive the viewport from your own state, bind `Viewport` and keep it up to date from `OnViewportChanged`. The
canvas adopts the parameter whenever you hand it a different value, and leaves the user's own pans alone otherwise:

```razor
<BlowCanvas Nodes="_nodes" Edges="_edges" Viewport="_viewport" OnViewportChanged="v => _viewport = v" />

<button @onclick="() => _viewport = BlowViewport.Identity">Reset</button>

@code {
    private BlowViewport _viewport = BlowViewport.Identity;
}
```

`OnMoveStart` and `OnMoveEnd` bracket a drag-pan, a pinch, a wheel gesture (which the browser gives no end to, so it
is taken to end after 160 ms of quiet) and an animated transition; `OnMove` reports every step in between, as well
as each auto-pan step and each instant change made from code. `OnViewportChanged` reports the viewports the canvas
commits: once at the release of a drag-pan or a pinch, and on every wheel step, every frame of a transition and
every instant change from code. A parent that re-renders on every `OnMove` re-renders on every frame of a gesture,
so update cheap state there and save the heavy work for `OnMoveEnd`. Demo: `/examples/controlled-viewport`.

### Zoom range and extents

`MinZoom` and `MaxZoom` (0.2 and 2.5) bound every zoom, whether from the user or from code. `TranslateExtent` is a
flow-space rectangle the visible area may not leave; when the pane is larger than the extent, the extent is centred.
`NodeExtent` bounds where root nodes may be dragged - see [constraining](#constraining-where-a-node-can-go).

## Layout and alignment

### Auto-layout

`BlowLayout` arranges nodes in place, in C#, so no JavaScript layout engine is needed:

```csharp
BlowLayout.Layered(_nodes, _edges, new BlowLayeredLayoutOptions
{
    Direction = BlowLayoutDirection.LeftToRight,
    LayerGap = 90,
});
_flow.UpdateNodeInternals();   // the handles moved with the direction
_flow.FitView();
```

| Layout | Shape |
| --- | --- |
| `Layered` | Flow chart or org chart. Each node sits one layer beyond the furthest node pointing at it; layers are ordered to reduce edge crossings and packed by their own thickness, so a layer of tall nodes never overlaps the next. Cycles are handled. |
| `Tree` | A tidy tree: each node centred over the nodes it leads to, every subtree in a band of its own, so branches of any size never overlap. The tree is the graph's breadth-first spanning forest; other edges are drawn but do not shape it. |
| `Radial` | The same tree in rings around its root - a mind map: each subtree in a wedge sized by its leaves, rings spaced so nothing overlaps. Several roots share the first ring around an empty centre. |
| `Force` | A network with no hierarchy: connected nodes pull together, all nodes push apart, and leftover overlaps are pushed apart at the end. Deterministic - the same graph always gives the same picture - and able to keep pinned nodes in place. Exact up to 500 nodes; past that a far-off group of nodes pushes as one (Barnes-Hut), so 5,000 nodes lay out in about a second. |
| `Grid` | Rows and columns in the order given, in uniform cells sized to the largest node - for tidying a scattered graph. |
| `Circle` | An even ring, clockwise from twelve o'clock - for showing peers rather than a hierarchy. |

| Option | Default | Applies to |
| --- | --- | --- |
| `Origin` | `(0, 0)` | All. Top-left of the laid-out block. |
| `NodeOrigin` | `(0, 0)` | All. Must match the canvas's `NodeOrigin`. |
| `FallbackSize` | 150 by 40 | All. Size assumed for a node that has neither an explicit size nor a measurement. |
| `Direction` | `TopToBottom` | Layered, Tree. Also `LeftToRight`, `BottomToTop`, `RightToLeft`. |
| `LayerGap`, `NodeGap` | 80, 40 | Layered. Gaps between layers and within one. |
| `CrossingReductionPasses` | 4 | Layered. 0 keeps the given order within each layer. |
| `AlignHandlesToDirection` | `true` | Layered, Tree. Points `SourcePosition` and `TargetPosition` along the flow. |
| `LevelGap`, `NodeGap` | 60, 30 | Tree. Gaps between levels and between neighbouring subtrees. |
| `IsRoot` | nodes nothing leads to | Radial. Which nodes are the roots. |
| `RingGap`, `NodeGap`, `StartAngle` | 60, 20, 0 | Radial. Least gaps between rings and between neighbours on one; where the first subtree starts. |
| `IdealEdgeLength`, `Iterations` | 180, 300 | Force. How far apart connected nodes settle; simulation steps. |
| `NodeGap` | 20 | Force. The least gap between any two nodes at the end. |
| `StartFromCurrentPositions` | `false` | Force. Tidy the current arrangement instead of starting from a spiral. |
| `IsPinned` | none | Force. Nodes that must not move. |
| `Theta` | 0.9 | Force, past 500 nodes. How far off a group must be to push as one: its width under this fraction of its distance. Higher is faster and coarser; 0 is exact. |
| `Columns` | square-ish | Grid. |
| `ColumnGap`, `RowGap` | 40, 40 | Grid. |
| `Radius` | derived | Circle. Null derives one at which neighbours do not overlap. |
| `StartAngle`, `NodeGap` | 0, 30 | Circle. Degrees clockwise from twelve o'clock; minimum gap used to derive the radius. |

The layouts write `Position` and nothing else - except `Layered`, which with `AlignHandlesToDirection` also points
each node's `SourcePosition` and `TargetPosition` along the flow, because a left-to-right layout whose edges still
leave the bottom of each node looks wrong. Only the built-in nodes read those two properties; a custom template
places its own handles. Positions are parent-relative, so lay out one parent's children at a time, or a set of
roots, rather than mixing depths in one call.

A layout uses each node's explicit or measured size, and `FallbackSize` for the rest. Laying out a graph that has
never been on screen works, but for the best result run it once the nodes are measured:

```razor
<BlowCanvas @ref="_flow" Nodes="_nodes" Edges="_edges" OnNodesInitialized="Arrange" />

@code {
    private BlowCanvas? _flow;
    private bool _arranged;

    private void Arrange()
    {
        // Raised again whenever newly added nodes finish measuring.
        if (_arranged) return;
        _arranged = true;

        BlowLayout.Layered(_nodes, _edges);
        _flow!.UpdateNodeInternals();
        _flow.FitView();
    }
}
```

Every layout's options class is also an `IBlowLayout` - one method, `Arrange(nodes, edges)` - and so is any layout
of your own, or an adapter to an external engine such as ELK. That is what `LayoutAsync` takes, to arrange part of
the graph on the canvas as one undo step:

```csharp
await _flow.LayoutAsync(new BlowTreeLayoutOptions());            // the selection, or the whole top level
await _flow.LayoutAsync(new BlowForceLayoutOptions(), someNodes);  // these nodes only
```

With nothing passed it arranges the selection when something is selected, else every top-level node. Only nodes
sharing one parent are arranged together (positions in different parents are not comparable), hidden nodes stay put,
and by default the arrangement is moved so its top-left corner stays where the nodes' was - so laying out a
selection tidies it in place; with pinned nodes (`BlowForceLayoutOptions.IsPinned`) it stays where the pins hold it. The layout sees the edges among those nodes, an edge into a node's children counting as
an edge into the node, so a group is placed by what its contents connect to. The canvas's `NodeOrigin` is handed to a
built-in layout for you. The change is reported as `Moved` with the label `layout`.

Measured on .NET 10 (x64): tree and radial take about 0.1 ms at 1,000 nodes and 1 ms at 5,000; force takes 9 ms at
200 nodes, 0.12 s at 1,000 and 0.96 s at 5,000 - several times that on WebAssembly, where a large force layout is
better run on a server or ahead of time.

Demo: `/examples/layouting`, `/examples/layouts-and-collapsing`.

### Align and distribute

Alignment guides tidy a node while it is dragged; these tidy a selection after the fact, the way the align buttons
of a drawing tool do:

```csharp
await _flow.AlignSelectionAsync(BlowAlignEdge.Left);              // Left, CenterX, Right, Top, CenterY, Bottom
await _flow.DistributeSelectionAsync(horizontal: true);           // even gaps across the existing span
await _flow.DistributeSelectionAsync(horizontal: true, gap: 24);  // a fixed gap from the first node
```

Aligning needs two selected nodes. Distributing keeps the two outermost nodes where they are and evens out the gaps
between the rest, so the group does not move, and needs three; with a `gap` it lays them out from the first node at
that spacing and needs two. Hidden and non-draggable nodes are left alone, and nothing moves while `NodesDraggable`
is off. The result is clamped to each node's extent, grows parents that expand, is announced to screen readers,
raises `NodesChanged` and is one undo step.

The same operations are pure functions on `BlowAlign` - `Align`, `DistributeHorizontally`, `DistributeVertically`
and `Distribute`, with overloads taking the canvas's node origin - returning the nodes that moved. Positions are
parent-relative, so align siblings, or roots, rather than nodes of different parents. Demo:
`/examples/align-distribute`.

### Alignment guides

Turn on `SnapToGuides` and a dragged node clicks into line with the nodes around it - by its edges or its centre, on
each axis independently - while a thin guide shows what it matched:

```razor
<BlowCanvas Nodes="_nodes" Edges="_edges" SnapToGuides="true" GuideDistance="6" GuideColor="#f43f5e" />
```

`GuideDistance` is measured in screen pixels, so the snap feels the same at any zoom. The nearest alignment within
it wins on each axis, so a node can line up with one neighbour horizontally and another vertically. The node being
dragged is tested against every visible node that is not moving with it, and a selection moves as one block, keeping
its shape. The matching is public as `BlowAlignment.Snap`. Demo: `/examples/alignment-guides`.

## Graph algorithms and validation

### Algorithms

`BlowGraphAlgorithms` answers the questions flow applications keep asking, with no UI and in time linear in the
size of the graph. None of them recurses, so a graph of any depth cannot overflow the stack:

| Method | Answers |
| --- | --- |
| `TopologicalSort` | Dependency order - what runs first. Returns `Order` and the `Unordered` nodes a cycle blocks. |
| `HasCycle`, `FindCycles` | Whether there is a directed cycle, and the groups of nodes that form them. |
| `StronglyConnectedComponents` | Groups of mutually reachable nodes, in reverse topological order. |
| `ConnectedComponents` | The pieces the graph falls into, ignoring direction. |
| `GetDescendants`, `GetAncestors`, `Traverse` | Everything downstream or upstream of a node, nearest first, optionally to a depth or in both directions. |
| `IsReachable` | Whether one node leads to another. A node reaches itself only through a cycle. |
| `ShortestPath` | The fewest-edges route, or with a weight function the cheapest (Dijkstra). |
| `GetRoots`, `GetLeaves`, `GetIsolatedNodes`, `GetUnreachable` | Where flows start and end, what is disconnected, what can never run. |
| `GetStatistics` | Counts: nodes, edges, roots, leaves, components, self-loops, degrees, acyclicity, dangling edges. |

Each takes either the node and edge lists or a `BlowGraphIndex` - the lookups (by id, incoming and outgoing edges,
edges per handle) built once, for asking several questions of the same graph:

```csharp
var graph = new BlowGraphIndex(_nodes, _edges);
foreach (var node in BlowGraphAlgorithms.TopologicalSort(graph).Order)
    Run(node, inputs: graph.Incoming(node.Id));
```

The index is a snapshot: build a new one after the graph changes. Edges naming a missing node are left out of the
adjacency and listed in `DanglingEdges`; hidden elements count like any other, so pass only what you want
considered. Demo: `/examples/typed-ports` computes its values in topological order.

### Validation

`BlowGraphValidator` checks a graph against rules and reports each problem as a `BlowValidationIssue`: a stable
`Code`, a `Severity` (`Info`, `Warning`, `Error`), an English `Message`, and the `NodeId`, `EdgeId`, `PortId` and
`PortType` it concerns, so a UI can mark the element and take the user to it.

```csharp
var validator = BlowGraphValidator.CreateDefault()          // structure + ports
    .Add(BlowValidationRules.NoCycles())
    .Add(BlowValidationRules.AllReachableFrom(n => n.Type == "input"))
    .Add(BlowValidationRules.ForNodes("no-owner", n => n.Type == "task" && n.Data is null ? $"{n.Label} has no owner." : null));

BlowValidationResult result = validator.Validate(_nodes, _edges);
if (!result.IsValid) ShowProblems(result.Issues);
var severity = result.SeverityOf(node);   // for a marker on that node
```

| Rule | Reports (`BlowValidationCodes`) |
| --- | --- |
| `Structure()` | Duplicate node and edge ids, edges to missing nodes, missing parents, parent cycles, edges naming a port their node does not declare. |
| `Ports(typeCompatibility)` | Ports with fewer edges than `MinConnections` (an unconnected required input) or more than `MaxConnections`, and edges between incompatible data types. |
| `NoCycles(severity)` | Every node on a directed cycle. |
| `NoIsolatedNodes(severity)` | Nodes with no edges (a warning by default). |
| `AllReachableFrom(isStart, severity)` | Nodes no start node leads to (a warning by default). |
| `ForNodes(code, check)`, `ForEdges(code, check)` | Your own rule per node or per edge: return a message, or null when it is fine. |

`CreateDefault()` holds `Structure` and `Ports`; `new BlowGraphValidator(rules...)` holds exactly the rules given,
and `Add` takes an `IBlowValidationRule` or a function over the `BlowValidationContext` (the lists as given, and a
`BlowGraphIndex` over them). `MaxIssues` (1,000) bounds the report of a hopelessly broken - or hostile - graph, and
the cancellation token stops a long validation. Messages are English; localize by `Code`.

### Showing a run

A workflow executing, a build, a state machine stepping: hand the canvas each node's `BlowRunState` - a
`BlowRunStatus` (`Queued`, `Running`, `Succeeded`, `Failed`, `Skipped`, or `Idle` for nothing) and an optional
message - through `RunStates`, keyed by node id, and change it as the run moves on:

```razor
<BlowCanvas Nodes="_nodes" Edges="_edges" RunStates="_states" />

@code {
    private readonly Dictionary<string, BlowRunState> _states = [];

    private async Task Step(BlowNode node)
    {
        _states[node.Id] = new(BlowRunStatus.Running);
        StateHasChanged();
        var ok = await _engine.ExecuteAsync(node);
        _states[node.Id] = ok ? new(BlowRunStatus.Succeeded) : new(BlowRunStatus.Failed, "The service timed out.");
    }
}
```

Each status is a class (`bit-blow-run-running`, ...) and a badge on the node's top-left corner whose glyph and
shape say the status - a ring while running (turning, unless motion is reduced), a tick, a cross, a dash, an
ellipsis - so it reads without colour, including in Windows high contrast. A screen reader hears the status and its
message as part of the node's description ("Failed: The service timed out."); announce milestones yourself with
`Announce`. The canvas only shows the run: executing it, in `BlowGraphAlgorithms.TopologicalSort` order or any
other, is the application's. Demo: `/examples/workflow-runner`, `/examples/state-machine`.

### Showing problems on the graph

Hand a result to the canvas's `Validation` parameter and it marks the graph:

```razor
<BlowCanvas Nodes="_nodes" Edges="_edges" Validation="_validation"
            OnGraphChanged="_ => _validation = _validator.Validate(_nodes, _edges)" />
```

| Element | Error | Warning |
| --- | --- | --- |
| Node | class `bit-blow-error`: a ring and a round badge with "!" | class `bit-blow-warning`: a ring and a triangular badge |
| Edge | class `bit-blow-error`: long dashes | class `bit-blow-warning`: short dashes |
| Port (an issue with `PortId`/`PortType`) | class `bit-blow-handle-error`: a ring | class `bit-blow-handle-warning` |

Every mark differs by shape as well as colour, so a problem can be told without telling red from amber; in Windows
high contrast the shapes stay and the colours become the system's. `Info` issues are not marked. The colours are
`--bit-blow-error` and `--bit-blow-warning`, with dark-mode values that keep 4.5:1 against the node background. A
screen reader hears the problems, most serious first, as part of the element's description (see
[what a screen reader hears](#what-a-screen-reader-hears)). Pair it with `FocusNodeAsync` to take the user to a
problem. Demo: `/examples/typed-ports`.

## Add-ons

Add-ons go in the canvas's `ChildContent` and are drawn over the flow in screen space, so they do not zoom. Those
that anchor to the pane take a `BlowPanelPosition`: `TopLeft`, `TopCenter`, `TopRight`, `BottomLeft`, `BottomCenter`
or `BottomRight`. Presses, clicks and wheels on them do not reach the pane, so using the minimap or a button does not
also pan or clear the selection. Placed outside a canvas, an add-on renders inert rather than throwing.

The add-ons that live inside a node or an edge - `BlowHandle`, [`BlowNodeResizer`](#resizing),
[`BlowNodeToolbar`](#node-toolbars), [`BlowDragHandle`](#drag-handles) and
[`BlowEdgeToolbar`](#html-labels-and-edge-toolbars) - are described with nodes and edges.

### Background

`BlowBackground` draws a pattern that pans and zooms with the flow.

| Parameter | Default | Meaning |
| --- | --- | --- |
| `Variant` | `Dots` | `Dots`, `Lines` or `Cross`. |
| `Gap` | 20 | Pattern spacing in flow units; `GapX` and `GapY` override each axis. |
| `Size` | 1 (dots), 6 (cross) | Dot diameter or cross arm length. |
| `LineWidth` | 1 | Stroke width for lines and crosses. |
| `OffsetX`, `OffsetY` | 0 | Extra offset of the pattern. |
| `Color` | `--bit-blow-background-pattern` | Pattern colour; the default follows the colour mode. |
| `BgColor` | transparent | A solid colour behind the pattern. |
| `PatternClass`, `Class`, `Style` | `null` | CSS on the pattern shape and the container. |

The pattern is drawn in screen space from the current viewport, so it stays crisp at any zoom. Demo:
`/examples/backgrounds`.

### Controls

`BlowControls` is a bar with zoom-in, zoom-out and fit-view buttons, and optionally a lock toggle.

| Parameter | Default | Meaning |
| --- | --- | --- |
| `Position` | `BottomLeft` | Anchor. |
| `Orientation` | `Vertical` | Or `Horizontal`. |
| `ShowZoom`, `ShowFitView` | `true` | The built-in buttons. |
| `ShowInteractive` | `false` | The lock button; bind `Interactive` to it, see [locking](#locking-the-canvas). |
| `FitViewOptions` | `null` | How the fit button fits, including a `Duration` to ease over. |
| `OnZoomIn`, `OnZoomOut`, `OnFitView` | - | Raised after the built-in behaviour. |
| `AriaLabel` | `"Flow controls"` | Accessible name of the toolbar. |
| `ZoomInLabel`, `ZoomOutLabel`, `FitViewLabel` | `"Zoom in"`, `"Zoom out"`, `"Fit view"` | Accessible name and tooltip of each built-in button. |
| `LockLabel`, `UnlockLabel` | `"Lock interaction"`, `"Unlock interaction"` | The lock button's name, and its tooltip in each state. |
| `ChildContent` | - | Extra buttons, after the built-ins. |

The zoom buttons disable themselves at the zoom limits. `BlowControlButton` is a button styled to match (`OnClick`,
`Title`, `Disabled`, `Class`, `ChildContent`):

```razor
<BlowControls>
    <BlowControlButton Title="Auto layout" OnClick="Arrange">L</BlowControlButton>
</BlowControls>
```

### Mini map

`BlowMiniMap` is an overview of the whole graph, with the visible area cut out of a mask.

| Parameter | Default | Meaning |
| --- | --- | --- |
| `Position` | `BottomRight` | Anchor. |
| `Width`, `Height` | 200, 150 | Size in screen pixels. |
| `Pannable` | `true` | Clicking or dragging on it moves the main view there. |
| `Zoomable` | `false` | The wheel over it zooms the main view, by `ZoomStep` percent (10) per notch. |
| `NodeColor`, `NodeStrokeColor`, `NodeStrokeWidth`, `BorderRadius` | theme, transparent, 2, 2 | How nodes are drawn. |
| `NodeColorFunc`, `NodeStrokeColorFunc`, `NodeClassFunc` | `null` | Per-node colour, outline and class. |
| `NodeTemplate` | `null` | Your own SVG per node, translated to the node's position. |
| `MaskColor`, `MaskStrokeColor`, `MaskStrokeWidth` | theme, transparent, 1 | The area outside the visible viewport. |
| `BgColor` | stylesheet | Background of the map. |
| `OnNodeClick` | - | A node on the map was clicked. |
| `AriaLabel` | `"Mini map"` | Accessible name. |
| `RoleDescription`, `KeyboardInstructions` | `"mini map"`, the keys it answers to | What a screen reader calls it and how it says to steer it, while it is pannable or zoomable. |
| `KeyboardPanStep` | 0.1 | How far one arrow press moves the view, as a fraction of the pane (five times as far with `Shift`). |

```razor
<BlowMiniMap Zoomable="true" NodeColorFunc="@(n => n.Type == "input" ? "#0041d0" : "#e2e2e2")" />
```

A pannable or zoomable map is in the tab order: the arrow keys move the view, `+` and `-` zoom it (with
`Zoomable`), and `Home` fits the whole flow. Its keys stop there - the same arrows on the pane would move the
selected nodes - and do not scroll the page. A map that steers nothing stays a picture (`role="img"`), out of the
tab order.

The map's frame holds still while the user steers with it, because the bounds include the viewport: recomputing
them as the drag moved the view would move the map under the pointer, and the view would run away. Demo:
`/examples/interactive-minimap`, `/examples/custom-minimap-node`, `/minimap`.

### Panels

`BlowPanel` anchors any content of yours to the pane - a toolbar, a legend, a status line. `Anchor` takes a
`BlowPanelPosition`; `Position` takes the same as a string (`"top-right"`, `"bottom-center"`, ...) and is used when
`Anchor` is not set.

```razor
<BlowPanel Anchor="BlowPanelPosition.TopRight">
    <button @onclick="Save">Save</button>
</BlowPanel>
```

### Viewport portal

`BlowViewportPortal` renders its content inside the zoomed layer, so it pans and zooms with the nodes. Position it
with flow coordinates - absolute positioning or a transform - for annotations, region outlines or a drawn extent:

```razor
<BlowViewportPortal>
    <div style="position:absolute;left:0;top:0;width:360px;height:200px;border:1px dashed #f59e0b;pointer-events:none"></div>
</BlowViewportPortal>
```

Demo: `/examples/node-extent`, `/extras`.

### Attribution

A small "bit Blow" link sits in a corner of the canvas. `AttributionPosition` moves it (`BottomRight` by default) and
`HideAttribution` removes it.

## Saving, undo and clipboard

### Exporting an image

`ToSvg()` draws the graph as it is on screen into a standalone SVG document, for documentation, a report or
printing; `BlowSvgExport.ToSvg(nodes, edges, options)` does the same without a canvas. Nodes are drawn in the
built-in look - a rectangle with the label, groups beneath their children - and edges with the same paths and
arrowheads as on screen, attached at the measured handles and routed the same way. Hidden nodes and their edges are
left out, and an edge into a collapsed group is drawn to the group, as on screen; the image is sized to everything
drawn, loops and detours included. Characters XML does not allow in a label are left out rather than breaking the
document. `BlowSvgExportOptions` sets the padding, title (the image's accessible name, by default the canvas's
`AriaLabel`), background and colours.

The output is safe to embed: labels are written as escaped text, never as markup, and an element's colour is only
written when it is a plain colour value (a hex code, a name, `rgb()`, `hsl()`), so nothing in a graph - an
imported one included - can put a script, an event attribute or a `url()` into the image. Custom node templates and
CSS are not reproduced; the image shows the structure and the labels. Demo: `/examples/edge-routing`,
`/examples/workflow-runner`.

### Saving and restoring

`ToObject()` returns a `BlowSnapshot` - `Nodes`, `Edges` and `Viewport` - as deep copies, so the snapshot keeps
describing the flow as it was while the user carries on editing; `ToObject(deep: false)` hands back the live
instances instead. `RestoreAsync` puts one back:

```csharp
BlowSnapshot saved = _flow.ToObject();
// ...
await _flow.RestoreAsync(saved);                         // graph and viewport
await _flow.RestoreAsync(saved, restoreViewport: false); // the graph, in the view the user already has
```

Restoring replaces the contents of the bound `Nodes` and `Edges` lists in place, so references to them stay valid,
and puts copies of the snapshot's elements in - pass `detach: false` only when you mean to hand over the instances
themselves. It then raises `NodesChanged` and `EdgesChanged`.

### Saving to JSON

`BlowSerializer` writes a graph as a versioned JSON document and reads it back. The canvas has the shortcuts:

```csharp
string json = _flow.ToJson(options);                      // the graph and the viewport
BlowImportResult result = await _flow.LoadJsonAsync(json, options);
if (!result.Succeeded) ShowProblems(result.Issues);       // nothing was replaced
```

`LoadJsonAsync` replaces the graph in place (as `RestoreAsync` does) only when the document was read without errors,
and records the load as an undo step. Without a canvas - on a server, in a test, in a background job - the same is
`BlowSerializer.Serialize(document)` and `BlowSerializer.Deserialize(json)`, with `BlowDocument` (`Nodes`, `Edges`,
`Viewport`, an optional `Id`, `Metadata` for anything else of yours, and the application's `Version`) and
`BlowDocument.FromSnapshot` / `ToSnapshot` to go between a document and what `ToObject()` and `RestoreAsync` use.

What is saved is the persistent state only: what the canvas maintains for itself - measured sizes, handle anchors,
drag state, absolute positions - is worked out again when the graph is drawn, and selection is left out unless
`IncludeSelection` is set. The output is deterministic - properties in a fixed order, defaults left out, numbers in
invariant round-trip form - so saving the same graph twice produces the same text, and saved graphs diff cleanly:

```json
{"format":"bit-blow","schemaVersion":1,"version":0,
 "nodes":[{"id":"a","type":"input","label":"Order placed","position":{"x":0,"y":0}},
          {"id":"b","label":"Pack","position":{"x":0,"y":120},"data":{"owner":"ana"}}],
 "edges":[{"id":"e1","source":"a","target":"b","animated":true}],
 "viewport":{"x":0,"y":0,"zoom":1}}
```

| Property | Meaning |
| --- | --- |
| `format`, `schemaVersion` | `"bit-blow"` and the version of this schema (1). A document from a newer schema is refused rather than half-read. |
| `version` | The application's own document version; see migrations below. |
| `id`, `metadata` | `BlowDocument.Id` and `Metadata`, when set. |
| `nodes[]` | `id`, `type` (when not `default`), `label`, `position` `{x, y}`, `width`, `height`, `parentId`, `origin`, `extentParent`, `extent` `{x, y, width, height}`, `expandParent`, `sourcePosition`, `targetPosition` (`left`/`top`/`right`/`bottom`), `hidden`, `draggable`, `selectable`, `connectable`, `deletable`, `focusable`, `useCustomDragHandle`, `ariaLabel`, `class`, `style`, `zIndex`, `ports[]` (`id`, `type`, `position`, `label`, `dataType`, `maxConnections`, `minConnections`, `data`), `data`. Booleans and other properties are written only when they differ from the default. |
| `edges[]` | `id`, `source`, `target`, `sourceHandle`, `targetHandle`, `type` (`bezier`, `straight`, `step`, `smoothstep`, `simplebezier`, `routed`), `pathOptions`, `label` and its styling, `animated`, the opt-outs, `strokeWidth`, `stroke`, the markers (`markerEnd`, `markerStart`, `markerType` `arrow`/`arrowclosed`, `markerColor`, `markerWidth`, `markerHeight`), `class`, `style`, `interactionWidth`, `zIndex`, `data`. |
| `viewport` | `{x, y, zoom}`, when the document has one. |

### Custom data

`Data` is written as JSON when it is JSON already (a `JsonElement` or a `JsonNode`), a string, a number or a boolean.
Anything else needs its JSON contract registered, as a source-generated type info so the application stays
trimmable:

```csharp
[JsonSerializable(typeof(TaskInfo))]
internal partial class AppJsonContext : JsonSerializerContext;

var options = new BlowSerializerOptions()
    .MapNodeData("task", AppJsonContext.Default.TaskInfo)   // nodes of type "task" read their data as TaskInfo
    .MapEdgeData(AppJsonContext.Default.Wire);               // every edge's data reads as Wire
```

Saving data of a type no mapping covers throws a `BlowSerializationException` naming the element, rather than
writing something that cannot be read back. Data with no mapping on the way in is kept as a `JsonElement` and
written back unchanged, so a document survives a round trip through an application that does not understand all
of it; data that does not fit its mapping is kept as JSON too, with a `data-not-read` warning. Nothing in a document
names a .NET type, and nothing is instantiated from one except through a contract the application registered: a
graph file is data, never code.

### Loading untrusted documents

A graph file is input like any other. `Deserialize` never throws on bad input; it returns a `BlowImportResult` with
the `Document` and the `Issues` it found, each with a stable `Code` (`BlowImportCodes`), a `Severity` and a JSON
`Path` such as `$.nodes[3].position.x`:

- invalid JSON (with line and position), a document that is not a Blow document, or one from a newer schema or
  application version: no document at all;
- an element with no `id` (or an edge with no `source` or `target`): left out, an error;
- a property with a value of the wrong kind or out of range: ignored, an error;
- an unknown property: ignored, for information;
- a duplicate id, an edge to a missing node, a parent cycle: kept, a warning - the canvas can draw around them,
  and reports them through `OnError` too.

`Succeeded` is true when there is a document and no error. `BlowImportLimits` (on `BlowSerializerOptions.Limits`)
bounds what is read before it can cost much: `MaxBytes` (32 MB), `MaxNodes` (50,000), `MaxEdges` (100,000),
`MaxPortsPerNode` (256), `MaxDepth` (64 levels of nesting, data included) and `MaxStringLength` (64 KB for an id,
a label, a class or a style). `DeserializeAsync(stream)` stops reading at `MaxBytes`, so an upload of any size cannot
make it buffer more than that.

### Migrations

`BlowSerializerOptions.CurrentVersion` is the application's document version, written into every document saved.
When the shape of what the application keeps changes, raise it and add a migration from the previous version; an
older document is brought up to date one version at a time as it is loaded:

```csharp
var options = new BlowSerializerOptions { CurrentVersion = 2 };
options.Migrations.Add(new BlowMigration(0, doc => { /* version 0 -> 1 */ return doc; }));
options.Migrations.Add(new BlowMigration(1, doc =>
{
    foreach (var node in doc["nodes"]!.AsArray())
        if ((string?)node!["type"] == "job") node["type"] = "task";
    return doc;
}));
```

A migration receives the whole document as a `JsonObject`. A gap in the chain is a `missing-migration` error, a
migration that throws is `migration-failed`, and a document saved by a newer version of the application is refused
(`newer-version`) rather than read wrongly. `BlowImportResult.SourceVersion` says what the file was.

### React Flow interchange

`BlowReactFlowFormat.Import(json)` reads React Flow's `toObject()` - `{ nodes, edges, viewport }` - and
`BlowReactFlowFormat.Export(document)` writes it, so flows can move between a React Flow front end and a Blazor one,
or an existing application's saved flows can be brought over. `data.label` becomes the node's `Label` (the whole
`data` object is kept as its data), style objects become inline CSS and back, and markers are read as such - React
Flow draws an arrowhead only when asked, so an edge without `markerEnd` comes over without one. What has no
counterpart, such as a drag-handle selector or a custom edge type, is reported as a warning rather than guessed at.

Demo: `/examples/save-restore` (local storage, file download and upload, a migration, React Flow import and export).

### Graph changes and transactions

`OnGraphChanged` is raised once for every completed edit, whatever made it, with a `BlowGraphChange`:

| Member | Meaning |
| --- | --- |
| `Kind` | `Added`, `Removed`, `Moved`, `Resized`, `Connected`, `Reconnected`, `Replaced` (a restore, a load, an undo or a redo) or `Edited` (a transaction, grouping, reordering). |
| `Origin` | `User` (a gesture or a key on the canvas), `Api` (your code calling the canvas), `History` (undo, redo) or `Remote`. |
| `Nodes`, `Edges` | The live elements the edit added, moved, resized or connected; for `Replaced`, the whole new graph. |
| `RemovedNodes`, `RemovedEdges` | What the edit removed, including edges that went with a removed node. |
| `Label` | `move`, `resize`, `connect`, `reconnect`, `delete`, `add`, `paste`, `duplicate`, `cut`, `align`, `distribute`, `group`, `ungroup`, `order`, `load`, `restore`, `undo`, `redo`, or your own. The same label names the step in the built-in history. |

It comes at the end of a gesture - when a drag is released, not on every move - after the canvas has updated the
lists, so it is the place to save, to run validation, or to send an operation to a server:

```csharp
private async Task OnGraphChanged(BlowGraphChange change)
{
    if (change.Origin == BlowChangeOrigin.Remote) return;   // it came from the server; do not echo it back
    await _sync.SendAsync(change.Kind, change.Nodes, change.Edges, change.RemovedNodes, change.RemovedEdges);
}
```

To make several changes as one edit, wrap them in `EditAsync`. However much happens inside, the history gets one
step, `OnGraphChanged`, `NodesChanged` and `EdgesChanged` are raised once, and the canvas renders once:

```csharp
await _flow.EditAsync(() =>
{
    _nodes.Add(new BlowNode { Id = "n7", Position = new(400, 80) });
    _edges.Add(new BlowEdge { Id = "e7", Source = "n6", Target = "n7" });
    _nodes.RemoveAll(n => n.Data is Obsolete);
}, label: "import");
```

Inside the edit, change the lists and their elements directly or call the canvas's own methods (`AddNodes`,
`DeleteElementsAsync`, `GroupSelectionAsync`, `LoadJsonAsync`, ...): their steps, change events and list events are
folded into the edit's, and a nested `EditAsync` is part of the outer one. The specific events of what happens inside
(`OnSelectionChange`, `OnDelete`, `OnPaste`) are still raised as it happens. The change raised lists the nodes and
edges the edit added and removed; changes to elements that stayed are not itemized.

An overload takes a `Func<Task>` for edits that await. Only the edit's own code is folded in: a key press or a
server push the canvas handles while the edit waits is an edit of its own, and so is work the edit started without
awaiting. Keep what an edit awaits short all the same - an undo in that window changes the lists under it. If the
edit throws, what it changed before throwing is committed as the edit and the exception is rethrown, so the half-done
change is neither lost from the history nor folded into the next step.

Code applying an edit that came from elsewhere - a collaborator's change, a server push - passes
`origin: BlowChangeOrigin.Remote` and `recordHistory: false`. The change listener then knows not to send it back,
and the local user cannot undo it: rather than being recorded as a step, the edit is written into every step the
history holds (`BlowHistory.Rebase`), so neither undo nor redo takes it back, and a local step whose whole effect it
overwrote is dropped. The model is ready for a collaboration adapter (stable ids, typed operations, origins);
conflict resolution and presence are the adapter's job, not the canvas's.

### Undo and redo

Set `HistoryEnabled` and the canvas keeps an undo stack itself, recording a step at each point a user would call
one, and binding Ctrl/Cmd+Z to undo and Ctrl/Cmd+Shift+Z or Ctrl/Cmd+Y to redo:

```razor
<BlowCanvas @ref="_flow" Nodes="_nodes" Edges="_edges"
            HistoryEnabled="true" HistoryCapacity="50"
            OnHistoryChanged="s => _history = s" />

<button disabled="@(!_history.CanUndo)" @onclick="() => _flow!.UndoAsync()">Undo</button>
<button disabled="@(!_history.CanRedo)" @onclick="() => _flow!.RedoAsync()">Redo</button>

@code {
    private BlowCanvas? _flow;
    private BlowHistoryState _history;
}
```

A step is recorded when a drag that moved something ends, a resize ends, an edge is connected or reconnected,
something is deleted, pasted, duplicated or cut, the keyboard nudges nodes, a selection is aligned or distributed,
and when nodes or edges are added through `AddNodes` or `AddEdges`. Selecting and panning are not edits, so they are
not steps, and undo restores the graph but leaves the viewport where the user put it. `HistoryCapacity` (100) bounds
the stack. `UndoAsync` and `RedoAsync` return false when there was nothing to do; `CanUndo`, `CanRedo` and `History`
are on the canvas too, and `OnHistoryChanged` carries a `BlowHistoryState` (`CanUndo`, `CanRedo`, `UndoDepth`,
`RedoDepth`, `UndoLabel`, `RedoLabel`) for toolbars. Every step is labelled with what made it, so a button can say
what it will do:

```razor
<button disabled="@(!_history.CanUndo)" @onclick="() => _flow!.UndoAsync()">
    Undo @_history.UndoLabel
</button>
```

The labels are the codes listed under [graph changes](#graph-changes-and-transactions); map them to your own words
for display. `CaptureHistory(label)` and `EditAsync(..., label)` give a step your own label.

A step costs memory in proportion to what changed, not to the graph: each node and edge is compared with its copy
in the previous step, and an unchanged copy is shared rather than copied again - unchanged including what undo
restores without counting it as an edit (selection, measured size, handle anchors). Recording a step after moving one
node of 5,000 takes about 0.17 ms and 90 KB (a whole-graph copy took 7.7 ms and 3.3 MB). The copies are never handed
out live, so editing the graph never alters a recorded step.

For an edit the canvas cannot see - your code changing a label, a node's data, a list you mutated yourself - call
`CaptureHistory()` afterwards. A capture identical to the current step is ignored, so calling it liberally is
harmless, and pressing undo always does something. Every property you can set on a node or an edge counts as a
change, including a replaced `Data` object; but `Data` is held by reference, so replace it with a modified copy
rather than mutating it in place if the old value should come back on undo. After loading a saved graph with
`RestoreAsync`, call `CaptureHistory()` so the loaded graph becomes the step undo returns to.

The history starts from the graph on screen when it is switched on, and changing `HistoryCapacity` starts a fresh
one. It is off by default for two reasons: a flow embedded in a page whose surrounding UI already owns those keys
should not quietly take them, and an app that already has an undo stack wants its edits in that one, not in a
second one the canvas keeps privately. For that case drive `BlowHistory` yourself:

```razor
<BlowCanvas @ref="_flow" Nodes="_nodes" Edges="_edges"
            OnInit="_ => Capture()"
            OnNodeDragStop="_ => Capture()"
            OnConnect="_ => Capture()"
            OnDelete="_ => Capture()" />

@code {
    private BlowCanvas? _flow;
    private readonly BlowHistory _history = new(capacity: 50);

    private void Capture() => _history.Capture(_flow!.Nodes, _flow.Edges, _flow.GetViewport());

    private async Task Undo() { if (_history.Undo() is { } s) await _flow!.RestoreAsync(s); }
    private async Task Redo() { if (_history.Redo() is { } s) await _flow!.RestoreAsync(s); }
}
```

`BlowHistory` holds snapshots: `Capture` records one and returns false for a no-op - pass the live lists, as above,
to have unchanged elements shared with the previous step, or a `BlowSnapshot` you made yourself - `Undo` and `Redo`
return the snapshot to restore or null, `Rebase` writes a change nobody should undo into every step, `UndoLabel` and
`RedoLabel` name the steps either side, and `Clear`, `Current`, `CanUndo`, `CanRedo`, `UndoDepth`, `RedoDepth` and `Capacity`
do what they say. Making a change after undoing ends the redo branch, as in every editor. Capture the starting
point, or the first undo has nothing to return to. Demo: `/examples/built-in-undo-redo`, `/examples/undo-redo`.

### Copy and paste

```razor
<BlowCanvas @ref="_flow" Nodes="_nodes" Edges="_edges" ClipboardShortcutsEnabled="true" OnPaste="OnPaste" />
```

Ctrl/Cmd+C, X, V and D copy, cut, paste and duplicate the selection. It is off by default so a flow does not quietly
take those keys from the page around it, and while focus is in a text field they copy text as usual. The same is
available from code as `Copy()`, `CutAsync()`, `PasteAsync()` and `DuplicateSelectionAsync()`; `CanPaste` says
whether there is anything to paste and `OnPaste` reports what was added.

- Copying takes the selected nodes and every edge running between two of them, selected or not: copying three
  nodes and getting back three unconnected ones is not what anyone means. An edge with one end outside the
  selection is left behind.
- A paste mints fresh ids (`{id}-copy`, then `{id}-copy-2` and so on), rewires edges to the copies, keeps a copied
  child inside its copied parent, and is offset by `PasteOffset` (24, 24) so it is visibly a copy. The pasted
  elements become the selection, ready to be moved.
- A child copied without its parent becomes a root at the same place on screen.
- Duplicate copies and pastes in one step and leaves the clipboard as it was.
- Cut copies and then deletes, with the usual `Deletable` rules and `OnBeforeDelete` veto.

The clipboard belongs to the canvas and lives in memory. The system clipboard cannot be read by a component
without a user gesture aimed at it, and would carry no graph structure anyway. `Copy()` returns the copied
`BlowFragment`, and `PasteAsync(fragment, offset)` pastes any fragment, so moving a selection between two canvases
is `await target.PasteAsync(source.Copy())`. The underlying operations are pure functions on `BlowClipboard`:
`Copy(nodes, edges)` and `Paste(fragment, options)`, where `BlowPasteOptions` takes the `Offset`, the `ExistingIds`
to avoid, an `IdFactory` and whether to `Select` the result. Demo: `/examples/copy-paste`.

## Theming

### The custom properties

Every colour in the stylesheet is a CSS custom property on the `.bit-blow` element - not only the canvas and its
nodes, but the control bar, the minimap, toolbars, label plates and the attribution badge - so a theme is a handful
of overrides rather than a fork. A test over the stylesheet fails when a rule writes a colour directly, because that
is the one kind of theming mistake that looks fine until someone switches the colour mode.

| Property | Light | Dark | Used for |
| --- | --- | --- | --- |
| `--bit-blow-bg` | `#fafafa` | `#1a1a1a` | The pane. |
| `--bit-blow-node-bg` | `#ffffff` | `#2b2b2b` | Node fill. |
| `--bit-blow-node-border` | `#1a192b` | `#555555` | Node border. |
| `--bit-blow-node-input-border` | `#0041d0` | `#4d8bff` | Border of `input` nodes. |
| `--bit-blow-node-output-border` | `#ff0072` | `#ff6aa8` | Border of `output` nodes. |
| `--bit-blow-node-color` | `#222` | `#e6e6e6` | Node text. |
| `--bit-blow-node-selected` | `#2563eb` | `#60a5fa` | Selection and focus rings, the selection box, resize controls, selected minimap nodes. |
| `--bit-blow-edge-stroke` | `#b1b1b7` | `#6b6b6b` | Edges without a `Stroke` of their own. |
| `--bit-blow-edge-selected` | `#2563eb` | `#60a5fa` | Selected and focused edges, reconnect anchors. |
| `--bit-blow-connection-stroke` | `#2563eb` | `#60a5fa` | The line drawn while connecting, and the handle it started from. |
| `--bit-blow-handle-bg` | `#1a192b` | `#cbd5e1` | Handles. |
| `--bit-blow-handle-valid` | `#22c55e` | `#22c55e` | A handle that would accept the connection being dragged; a connectable handle on hover. |
| `--bit-blow-handle-invalid` | `#ef4444` | `#ef4444` | A handle that would not. |
| `--bit-blow-surface` | `#ffffff` | `#2b2b2b` | Controls, toolbars, label plates, the outline around handles. |
| `--bit-blow-surface-hover` | `#f4f4f4` | `#3a3a3a` | Control buttons on hover. |
| `--bit-blow-surface-muted` | `#f4f4f5` | `#3a3a3a` | Buttons in a node toolbar. |
| `--bit-blow-surface-muted-hover` | `#e4e4e7` | `#4a4a4a` | The same on hover. |
| `--bit-blow-surface-translucent` | `rgba(255, 255, 255, 0.9)` | `rgba(32, 32, 32, 0.9)` | Minimap and attribution backgrounds. |
| `--bit-blow-border` | `#e2e2e2` | `#454545` | Minimap and toolbar borders. |
| `--bit-blow-border-subtle` | `#eeeeee` | `#383838` | Separators between control buttons. |
| `--bit-blow-text` | `#333333` | `#e6e6e6` | Edge labels and the text of the chrome. |
| `--bit-blow-text-muted` | `#6b7280` | `#9ca3af` | Group labels, the attribution. |
| `--bit-blow-shadow` | `rgba(0, 0, 0, 0.15)` | `rgba(0, 0, 0, 0.5)` | Shadows of nodes, controls and toolbars. |
| `--bit-blow-group-bg` | `rgba(120, 120, 160, 0.08)` | `rgba(160, 170, 210, 0.07)` | `group` node fill. |
| `--bit-blow-group-border` | `#9aa0b5` | `#5b6070` | `group` node border. |
| `--bit-blow-background-pattern` | `#aaaaaa` | `#4a4a4a` | `BlowBackground` without a `Color`. |
| `--bit-blow-minimap-node` | `#e2e2e2` | `#4a4a4a` | Minimap nodes, unless set on the component. |
| `--bit-blow-minimap-mask` | `rgba(120, 120, 120, 0.25)` | `rgba(0, 0, 0, 0.45)` | The minimap outside the visible area, unless set on the component. |

The alignment guide colour is the canvas's `GuideColor` parameter rather than a property.

One property is a size rather than a colour: `--bit-blow-handle-hit-size` (12px) is the smallest a handle's hit
area gets on screen. Handles are drawn in flow units, so zoomed out to 20% an 8px dot is a pixel and a half across;
an invisible hit area around it is held at this size whatever the zoom, while the dot keeps shrinking with the graph.
Edges do the same through `InteractionWidth`.

### Changing the palette

Override the properties in a global stylesheet linked after `bit-blow.css`. CSS isolation does not reach the
canvas's elements, so an isolated stylesheet needs `::deep`.

```css
.bit-blow {
    --bit-blow-node-selected: #0d9488;     /* selection, focus rings and the selection box */
    --bit-blow-edge-selected: #0d9488;
    --bit-blow-connection-stroke: #0d9488;
    --bit-blow-dark-accent: #2dd4bf;       /* the same three in dark mode */
}
```

The dark palette is written once, as a second set of properties named `--bit-blow-dark-*` (`-bg`, `-node-bg`,
`-node-border`, `-node-input-border`, `-node-output-border`, `-node-color`, `-accent`, `-edge-stroke`, `-handle-bg`,
`-surface`, `-surface-hover`, `-surface-muted`, `-surface-muted-hover`, `-surface-translucent`, `-border`,
`-border-subtle`, `-text`, `-text-muted`, `-shadow`, `-group-bg`, `-group-border`, `-background-pattern`,
`-minimap-node`, `-minimap-mask`), which the dark mode copies over the light ones; `-accent` feeds the three
selection colours. The dark rule uses two classes so that it outranks an override written on `.bit-blow` alone -
otherwise a single light-theme override would quietly switch dark mode off. So: override a property to change the
light theme, and its `--bit-blow-dark-*` twin to change the dark one.

To theme one canvas rather than all of them, give it a class and scope the overrides to it:

```razor
<BlowCanvas Class="ocean" Nodes="_nodes" Edges="_edges" />
```

```css
.bit-blow.ocean { --bit-blow-bg: #f0f9ff; --bit-blow-node-border: #0369a1; }
```

### Colour modes

`ColorMode` is `BlowColorMode.Light` (the default), `Dark` or `System`, which follows the operating system's
`prefers-color-scheme`. Dark and system modes reassign every property from the dark palette, so the whole component
follows - not just the canvas going dark under a control bar that stayed white. Demo: `/examples/color-mode`.

### Forced colours and reduced motion

In forced-colours mode (Windows high contrast) the browser replaces the colours of HTML but not the strokes and fills
of SVG, nor box shadows - which is how edges, handles and the selection are drawn. The stylesheet maps those onto
the system palette (`CanvasText`, `Highlight`, `GrayText`), and draws selection and focus as outlines, since shadows
are dropped. Handle states never rely on colour alone: a handle that would accept the connection being dragged gets a
ring, one that would not is dimmed. Reduced motion is covered under [animated transitions](#animated-transitions).

### Styling hooks

| Element | Classes |
| --- | --- |
| Canvas | `bit-blow`, plus `bit-blow-dark` or `bit-blow-system`, plus `Class` |
| Node | `bit-blow-node`, `bit-blow-node-{Type}`, and `selected`, `dragging`, `resizing`, `undraggable` |
| Built-in node label | `bit-blow-node-label` |
| Handle | `bit-blow-handle`, `bit-blow-handle-{top,right,bottom,left}`, `source` or `target`, `connectable`, and while connecting `bit-blow-handle-valid`, `-invalid` or `-connectingfrom` |
| Edge | `bit-blow-edge` (the group), `selected`, `animated`; `bit-blow-edge-path`, `bit-blow-edge-interaction` |
| Edge label | `bit-blow-edge-text`, `bit-blow-edge-label`, `bit-blow-edge-label-bg`; HTML labels in `bit-blow-edge-label-html` |
| Connection line | `bit-blow-connectionline`, plus `ConnectionLineClass` |
| Selection box, guides | `bit-blow-selection`, `bit-blow-guide` |
| Add-ons | `bit-blow-background`, `bit-blow-controls`, `bit-blow-controls-button`, `bit-blow-minimap`, `bit-blow-minimap-node`, `bit-blow-minimap-mask`, `bit-blow-panel`, `bit-blow-node-toolbar`, `bit-blow-edge-toolbar`, `bit-blow-resize-handle`, `bit-blow-resize-line`, `bit-blow-drag-handle`, `bit-blow-attribution` |

Set handle colours through the handle's own property rather than a background with `!important`, so the live valid
and invalid colours still win while a connection is dragged:

```css
.port-number { --bit-blow-handle-bg: #2563eb; }
```

## Keyboard and accessibility

### The keyboard map

The canvas is a focusable region with `role="application"`; keys work while it, or a node or edge in it, has focus.

| Key | Action |
| --- | --- |
| `Tab`, `Shift+Tab` | Move focus through the flow's edges and nodes. A node that receives focus off screen is panned into view (`AutoPanOnNodeFocus`). |
| `Enter`, `Space` | Select the focused node or edge, replacing the selection. With `Shift`, toggle it within the selection. While connecting from the keyboard, `Enter` connects to the focused node. |
| `C` | On a focused node, start a connection from its first port; pressed again, step to its next port. While connecting, on another node, step to the next port the connection can end on (`ConnectionKey`). |
| Arrow keys | Move the selected nodes 5 flow units - one grid cell with `SnapToGrid`. |
| `Shift` + arrow keys | Move them 25 flow units. |
| `Escape` | Abandon the gesture in progress - a drag, a selection box, a connection, a resize or a pinch - putting back anything it had moved; otherwise clear the selection. |
| `Delete`, `Backspace` | Delete the selection (`DeleteKeys`, `DeleteKeyEnabled`). |
| `Ctrl`/`Cmd` + `A` | Select every selectable, visible node and edge (`SelectAllKeyEnabled`). |
| `Ctrl`/`Cmd` + `Z` | Undo (`HistoryEnabled`). |
| `Ctrl`/`Cmd` + `Shift` + `Z`, `Ctrl`/`Cmd` + `Y` | Redo (`HistoryEnabled`). |
| `Ctrl`/`Cmd` + `C`, `X`, `V`, `D` | Copy, cut, paste, duplicate (`ClipboardShortcutsEnabled`). |
| `Shift+F10`, context-menu key | On a focused node or edge, raise `OnNodeContextMenu` / `OnEdgeContextMenu`, as a right-click does - the browser fires the same `contextmenu` event. |
| Hold `Space` | Drag on the pane to pan, even with `SelectionOnDrag` (`PanActivationKey`). |
| Hold `Shift` | Drag on the pane to draw a selection box (`SelectionKey`); click to add to the selection (`MultiSelectionKey`). |

Graph keys are ignored while focus is in a text input, textarea, select or contenteditable element, so nodes can
host editable content safely: Backspace edits the text rather than deleting the node. Escape still abandons a
gesture. Keys the canvas uses do not also scroll the page.

### Connecting from the keyboard

Every connection a pointer can make can be made from the keyboard, so no part of building a graph needs a pointer
(WCAG 2.1.1, and the single-pointer alternative to dragging of 2.5.7):

1. Tab to the node to connect from and press `C`. The connection starts from its first port that can start one -
   outputs first, then in reading order - and is announced with the port's label; `C` again steps to the next port.
2. Tab (or move focus any other way) to the node to connect to. The line follows focus to the first port there the
   connection could end on, and the reader hears either "Press Enter to connect to ..." or why it cannot (the same
   reasons a drag shows). `C` steps through that node's other acceptable ports.
3. `Enter` connects; a refused target leaves the connection waiting for another node. `Escape` abandons it.

Every rule applies exactly as it does to a drag - data types, limits, cycles, `IsValidConnection`,
`OnBeforeConnect` - and `OnConnectStart`, `OnConnect` and `OnConnectEnd` are raised the same way. `ConnectionKey`
picks another key, or turns this off with null; it is off anyway with `DisableKeyboardA11y` or without
`NodesConnectable`. The key is ignored with `Ctrl`, `Cmd` or `Alt` held, so `Ctrl+C` still copies. Ports are found
from the measured handles, so a node shows the ports it can connect by as it is drawn.

An existing edge's end moves the same way, so reconnecting needs no pointer either:

1. Tab to the edge and press `C`. Its target end is picked up and announced ("Moving the end of Edge from Source to
   Sink at Sink ..."); `C` again picks up its source end instead.
2. Move focus to the node the end should go to. As above, the line follows focus and the reader hears whether
   `Enter` would attach it there, and `C` steps through that node's ports.
3. `Enter` attaches it and is announced ("Reconnected Source to Other sink"); `Escape` leaves the edge as it was.

It is the pointer's reconnect in every respect: the same rules (the edge being moved is not counted against a port's
limit or as a duplicate of itself), `OnReconnectStart`, `OnReconnect` and `OnReconnectEnd`, one undo step, and the edge
keeps its `Id`. It is on while connecting from the keyboard is and `EdgesReconnectable` is; an edge whose
`Reconnectable` is off says so. The edge instructions mention the key while it is on.

### Without dragging

Everything a drag does has a way that needs no drag (WCAG 2.5.7): connections are made from the keyboard or, with
`ConnectOnClick`, by clicking one handle and then the other; nodes move with the arrow keys, or with
`MoveSelectionAsync(dx, dy)` behind buttons of your own - on a `BlowNodeToolbar`, say - which moves the selection as
an arrow key does (grid, extents, growing parents, one undo step, announced). `/examples/a11y` puts move buttons on
the selected node.

### Taking the user somewhere

`FocusNodeAsync(id, select, zoom, durationMs)` and `FocusEdgeAsync(...)` centre the view on an element and move
keyboard focus to it, so a screen reader reads it out and the keys act on it - for a search result, a validation
problem, a "go to" command. They return false when the element is missing, hidden or not focusable. With
`OnlyRenderVisibleElements`, the element is rendered before it is focused. `SelectAsync(nodes, edges, additive)`
sets the selection from code.

`FindNodes(query)` and `FindEdges(query)` search what is shown - `BlowGraphSearch` searches any list - by labels,
ids, types and port names, plus any text of yours; case and accents are ignored, every word of the query has to
match somewhere, and the best matches come first. The query is text, never a pattern, so no query can make a search
expensive:

```razor
<input type="search" aria-label="Find a node" @bind="_query" @onkeydown="Go" />

@code {
    private async Task Go(KeyboardEventArgs e)
    {
        if (e.Key == "Enter" && _flow.FindNodes(_query).FirstOrDefault() is { } best)
            await _flow.FocusNodeAsync(best.Id, select: true, durationMs: 300);
    }
}
```

### Focus and the tab order

Nodes and edges are in the tab order by default, with visible focus rings. Focus follows document order, which is
the order they are drawn in: the edges, which sit beneath the nodes, and then the nodes. `NodesFocusable` and
`EdgesFocusable` take each kind out of the tab order, `Focusable` on a node or edge takes out one, and
`DisableKeyboardA11y` takes everything out and turns arrow-key movement off.

### What a screen reader hears

The canvas is named by `AriaLabel` ("Flow diagram"). Each node is a group whose role description is "node" and whose
name is its `AriaLabel`, else its `Label`, else "Node {id}"; each edge is a group described as "edge" and named by
its `Label`, else "Edge from {source} to {target}", the two nodes named as they are announced. After the name comes
the element's state - for a node "Selected. Error: Input a must be connected. 1 incoming and 2 outgoing
connections.", for an edge whether it is selected and its problems - and then the keyboard instructions. Selecting with Enter or Space, moving with the arrow keys,
aligning or distributing, and deleting with the delete keys are announced through a visually hidden, polite
`aria-live` region. Repeating the same announcement - nudging a node twice in the same direction - is still spoken
the second time. `Announce(message)` puts a message of your own in the same region.

A canvas gives a screen-reader user nothing to discover its keys by, so the flow says what they are: the pane, each
node and each edge are described (`aria-describedby`) by short keyboard instructions held in hidden elements, read
after the name when focus arrives. A description the app gives the canvas through its own `aria-describedby`
attribute is kept, before the flow's. How to replace these strings, or switch any of them off, is in
[localization](#localization). The state and instruction elements are hidden: they are reached only through
`aria-describedby`, never read twice in the page's reading order. Demo: `/examples/a11y`, `/examples/keyboard-a11y`,
`/examples/typed-ports`.

## Localization

Every string the canvas reads out or labels its nodes and edges with comes from `AriaLabels`, a `BlowAriaLabels`,
so a flow can be translated without reimplementing its accessibility:

```razor
<BlowCanvas Nodes="_nodes" Edges="_edges" AriaLabel="Arbeitsablauf" AriaLabels="_labels" />

@code {
    private readonly BlowAriaLabels _labels = new()
    {
        NodeRoleDescription = "Knoten",
        EdgeRoleDescription = "Kante",
        NodeLabel = name => $"Knoten {name}",
        EdgeLabel = (source, target) => $"Kante von {source} nach {target}",
        NodeSelected = name => $"{name} ausgewählt",
        NodeDeselected = name => $"{name} abgewählt",
        EdgeSelected = (source, target) => $"Kante {source} nach {target} ausgewählt",
        EdgeDeselected = (source, target) => $"Kante {source} nach {target} abgewählt",
        NodeMoved = (name, position) => $"{name} verschoben nach {position}",
        NodesMoved = count => $"{count} Knoten verschoben",
        Deleted = (nodes, edges) => $"{nodes} Knoten und {edges} Kanten gelöscht",
    };
}
```

| Member | English default |
| --- | --- |
| `NodeLabel` | the name itself |
| `NodeRoleDescription`, `EdgeRoleDescription` | `"node"`, `"edge"` |
| `EdgeLabel` | `"Edge from {source} to {target}"` |
| `NodeSelected`, `NodeDeselected` | `"{name} selected"`, `"{name} deselected"` |
| `EdgeSelected`, `EdgeDeselected` | `"Edge {source} to {target} selected"`, `"... deselected"` |
| `NodeMoved` | `"{name} moved to {position}"` |
| `NodesMoved` | `"{count} nodes moved"` |
| `Deleted` | `"Deleted {nodes} node(s) and {edges} edge(s)"` |
| `Connected` | `"Connected {source} to {target}"` |
| `ConnectionRejected` | `"Cannot connect: ..."`, a sentence per `BlowConnectionRejection` |
| `NodeState`, `EdgeState` | a `BlowNodeState` / `BlowEdgeState` read as `"Selected. Error: .... 2 incoming connections."` |
| `KeyboardConnectionStarted` | `"Connecting from {node}, {port}. Move to another node and press Enter to connect, {key} to choose another port, or Escape to cancel."` |
| `KeyboardConnectionTarget` | `"Press Enter to connect to {node}, {port}."` |
| `KeyboardConnectionCancelled` | `"Connection cancelled."` |
| `KeyboardConnectionUnavailable` | `"No connection can start from {node}."` |
| `KeyboardConnectionInstructions` | `"Press {key} to start a connection from it."`, added to the node instructions |
| `KeyboardReconnectionStarted` | `"Moving the end of {edge} at {end}. Move to another node and press Enter to attach it there, {key} to move its other end instead, or Escape to leave it as it was."` |
| `KeyboardReconnectionCancelled` | `"Reconnection cancelled. The edge is unchanged."` |
| `KeyboardReconnectionUnavailable` | `"{edge} cannot be reconnected."` |
| `KeyboardReconnectionInstructions` | `"Press {key} to move its end to another node."`, added to the edge instructions |
| `Reconnected` | `"Reconnected {source} to {target}"` |
| `PaneInstructions` | `"Press Tab to move between the nodes and edges of the diagram."` |
| `NodeInstructions` | how to select, add to the selection, move, delete and deselect |
| `EdgeInstructions` | how to select, delete and deselect |

The members are functions rather than format strings, so a translation is free to reorder or drop the values it is
handed - which a positional format string cannot do in a language whose word order differs. A member left out keeps
its English default. Edge strings receive the names of the nodes the edge joins; a node's name is its `AriaLabel`,
else its `Label`, else its id. A moved position is written in the current culture, with a semicolon between the coordinates where the
decimal separator is a comma. The three instruction members are plain strings; an empty one leaves that kind of
element undescribed.

A few strings live elsewhere: the canvas's own `AriaLabel`, `AriaLabel`, `RoleDescription` and
`KeyboardInstructions` on `BlowMiniMap`, the fallback node name
"Node {id}" (which a `Label` or `AriaLabel` on the node replaces), and the control bar's, which `BlowControls` takes as
parameters: `AriaLabel` for the bar and `ZoomInLabel`, `ZoomOutLabel`, `FitViewLabel`, `LockLabel` and `UnlockLabel`
for the buttons' names and tooltips. Attributes the canvas does not set itself, such as `aria-roledescription` or
`dir` on the canvas, pass straight through to its element; `dir="rtl"` lays out the text in nodes, labels and
panels right to left, while flow coordinates stay as they are. Demo: `/examples/localization`.

## Errors and diagnostics

### What gets reported

A graph assembled from real data is not always well formed. Rather than throw, the canvas reports what it found
through `OnError` and carries on drawing what it can:

| `BlowError.Code` (`BlowErrorCode`) | Problem | What the canvas does |
| --- | --- | --- |
| `duplicate-node-id` (`DuplicateNodeId`) | Two nodes share an id. | Only the first one is drawn and reachable. |
| `duplicate-edge-id` (`DuplicateEdgeId`) | Two edges share an id. | Only the first one is drawn. |
| `missing-edge-endpoint` (`MissingEdgeEndpoint`) | An edge names a node that is not in the list. | The edge is not drawn. |
| `missing-parent-node` (`MissingParentNode`) | A node names a parent that is not in the list. | The node is laid out as a root. |
| `duplicate-handle-id` (`DuplicateHandleId`) | A node has two handles of the same type with the same id, or both without one. | The second takes the first one's edges. |
| `parent-cycle` (`ParentCycle`) | Following a node's parents leads back to it, or a node names itself. | The cycle is cut where it was found: that node is laid out as a root. |

```razor
<BlowCanvas Nodes="_nodes" Edges="_edges" OnError="@(e => _log.Add($"{e.Code}: {e.Message}"))" />
```

`BlowError` carries a stable machine-readable `Code` and a human-readable `Message` naming the offending ids.

### Reporting each problem once

Each distinct problem is reported once for the lifetime of the canvas, not on every render - a graph that stays
broken would otherwise raise the same error on every frame of every drag. To have the same mistake reported again
after the user starts over, give the canvas a new `@key` so a fresh one is mounted. Errors are raised after the
render that found them, never during it.

### Other mistakes it absorbs

- An edge to a hidden node is not an error: it is simply not drawn, because something has been collapsed.
- A chain of parents deeper than 50 is cut off rather than followed.
- A handle, resizer, toolbar or drag handle outside a node template, and a background, control bar or minimap
  outside a canvas, render inert instead of throwing a null reference.
- The JavaScript side going away - a dropped circuit, a component disposed mid-call, prerendering - is treated as
  the normal end it is, not as an error.

Demo: `/examples/diagnostics`, `/examples/broken-nodes`.

## Performance

### What the canvas does for you

- **A render redraws only what changed.** Every render of the canvas is tagged with what it may have changed:
  the viewport alone (a frame of a pan, a zoom, a pinch, a viewport transition, the connection line moving), the
  nodes' geometry (a frame of a drag or a resize), or anything at all (selection, the lists, the canvas's
  parameters, a render of the component hosting it). Node and edge components skip a render in which nothing they
  draw from changed, and the add-ons inside the canvas re-render only for the part they show - the background and
  minimap for the viewport, a handle when a connection starts or ends. A frame of a pan of a 1,000-node graph renders
  7 components instead of 6,293; a frame of a drag renders the dragged node and its edges. See
  [templates and the viewport](#templates-and-the-viewport) for what that means for your own templates.
- **Nothing is allocated per element per frame.** The callbacks each node and edge is wired to are made once per
  element, and what every renderer is told alike is one shared object, so a frame's garbage does not grow with the
  graph: about 100 KB for a 1,000-node frame, down from 7 MB.
- **One layout pass per render.** Every node's absolute position is resolved once, with parent chains memoized, so
  the cost is linear in the number of nodes and edges, and every part of the render reads the same geometry.
- **Edge geometry is cached across renders**, keyed by exactly what the path depends on: its anchors and sides, its
  type and options, its markers and, for a loop, its node. Panning and zooming change only the viewport transform,
  so a pan of a large graph rebuilds no path strings and hands the diff identical markup.
- **Arrowheads are shared**: one SVG marker per distinct shape, colour and size.
- **Measuring is rare, and brings across only what changed.** It runs after something that can change sizes -
  content, a drag ending, a resize, nodes added - and never during a gesture, a wheel zoom or a viewport transition,
  even when a controlled parent re-renders the canvas on every frame of one. The browser reads every node, which is
  cheap, but reports only those whose size or handles changed since its last report, and those the canvas names as
  new to it; bringing 900 measurements across to .NET cost a quarter of a second on WebAssembly.
- **Resize notifications are coalesced** to one per animation frame, and a page scroll only refreshes the pane's
  position without re-measuring.
- **Listeners are attached only when needed**: the pane's pointer-move listener while a gesture runs, and node and
  edge mouse listeners only for events you handle.
- **Connection checks are O(1)**: the rules are bound to the starting handle once, when a connection drag starts
  (existing wiring, connection counts, and the nodes a cycle would come back through), so checking a candidate handle
  is a few lookups. Handles show their verdict when the drag starts and are not re-rendered while it moves.

### Large graphs

`OnlyRenderVisibleElements` renders only the nodes and edges that intersect the visible area plus a 20% margin on
each side. A node that has not been measured yet is always rendered once, so it can be sized. Edges are culled by
their anchors, padded by the furthest their path can stray, so a long edge whose curve crosses the screen is kept
even when neither end is on it.

```razor
<BlowCanvas Nodes="_nodes" Edges="_edges" OnlyRenderVisibleElements="true" MinZoom="0.05" />
```

The stress page, `/examples/stress`, builds grids of up to 900 nodes and lets you switch culling on and off.

### Measured

`Tests/Bit.Blow.Benchmarks` measures the C# side of a frame - building and diffing the render tree, which is what a
pan costs on WebAssembly and what every user's interaction costs a Blazor Server host - and the graph helpers. On
.NET 10 (x64, a desktop machine):

| Frame | 200 nodes | 1,000 nodes |
| --- | ---: | ---: |
| Pan or zoom | 5 us, 7 components | 11 us, 7 components |
| Drag one node | 0.11 ms, 14 components | 0.57 ms, 14 components |

Before render scoping the same frames took 1.2 ms and 13.7 ms and re-rendered every node, edge and handle (1,250
and 6,293 components). A drag still walks the node list once per frame to find the node that moved, which is why it
grows with the graph while a pan does not. These are the numbers to multiply for the browser: a Release WebAssembly
build without AOT runs the same code interpreted, about a hundred times slower (a drag frame of 900 nodes takes about
65 ms there), and the browser's own style, layout and paint add only about 3 ms. The benchmark project's README has
the method and the full tables, including layout, edge paths, undo steps and paste.

In the browser, the E2E suite's performance tests drive the stress page with 900 nodes and culling off, in a Release
build in headless Chromium, and time each step end to end - input, .NET, rendering, the browser's style, layout and
paint (medians; the machine matters):

| Step, 900 nodes | Before | Now |
| --- | ---: | ---: |
| A zoom step | 83 ms | 58 ms |
| A drag step | - | 66 ms |
| A zoom right after letting go of a dragged node | 873 ms | 113 ms |

A Chrome performance trace of those steps puts the browser's own style, layout and paint at about 3 ms a frame; the
rest is .NET, which a Release WebAssembly build without AOT runs interpreted. Letting go of a node used to cost two
full measurement passes (about 240 ms each, nearly all of it bringing the measurements across to .NET) and a render of
every node; now the measurement brings across nothing that did not change and only the dragged node renders. A drag
step is the canvas walking its 900 node components to find the one that moved, about 66 ms interpreted. On a graph
that size turn on `OnlyRenderVisibleElements`, which keeps that walk to what is on screen, or compile with AOT.

### Templates and the viewport

Node and edge templates are re-rendered when their node or edge changes, when the selection or the lists change,
and whenever the component hosting the canvas re-renders - but not on the frames of a pan or a zoom, which is what
keeps those frames cheap. A template that draws something from the viewport - a level-of-detail card that shows
more when zoomed in - wraps that part in `BlowViewportView`, which follows the viewport:

```razor
<NodeTemplate Context="node">
    <BlowViewportView Select="@(v => v.Zoom >= 0.6)" Context="viewport">
        @if (viewport.Zoom >= 0.6) { <TaskCard Task="@node.Data" /> } else { <strong>@node.Label</strong> }
    </BlowViewportView>
</NodeTemplate>
```

`Select` keeps it cheap too: the content re-renders only when the value it returns changes - here, when the zoom
crosses 0.6 - rather than on every frame. `BlowViewportView` renders no element of its own, so it works inside an
SVG edge template as well. To redraw everything after a change the canvas cannot see, call `Refresh()`.

### Habits that keep it fast

- Prefer WebAssembly for heavy editing; see [hosting models](#hosting-models).
- Keep node templates light: every node is a component, and a template with heavy markup is paid for per node.
- Handle `OnMove`, `OnNodeDrag` and `OnNodeResize` cheaply, and save expensive work for `OnMoveEnd`,
  `OnNodeDragStop` and `NodesChanged`. A parent that re-renders on every `OnMove` re-renders on every frame.
- Make many changes, then render once: mutate the lists, then call `Refresh(remeasure: true)` a single time.
- The minimap draws every visible node, culling or not; on a very large graph consider leaving it out.
- Keep `IsValidConnection` and handle rules cheap; they run for every handle when a connection drag starts, and for
  the handles within snapping distance as it moves.
- Draw level-of-detail content with `BlowViewportView` and a `Select`, so zooming re-renders only the nodes whose
  detail level actually changed.

## Hosting models

The same component works in every Blazor hosting model. What differs is where the C# runs, and therefore what sits
between a pointer moving and a node following it: every pointer move of a drag, a pan or a connection is a Blazor
event handled in C#.

### Blazor WebAssembly

The recommended model for editing. The canvas runs in the browser, in the same process as the UI, so every pointer
move is handled without a network hop and dragging feels native. No configuration is needed.

### Blazor Server

Works, with the same features, but every drag move is a round trip over the SignalR circuit. On a local network that
is a few milliseconds; over the internet it is enough for a dragged node to trail the pointer. The canvas keeps the
traffic down - the pointer-move listener exists only while a gesture runs, node mouse listeners only when handled -
and arms each gesture before awaiting anything, so a quick click that outruns a round trip cannot leave a drag or a
selection box stuck to the pointer. A read-only viewer, or light editing, is fine on Server; sustained editing over a
high-latency connection is what WebAssembly is for.

### Blazor Web App

Put the pages that host a canvas in the `.Client` project and give them an interactive WebAssembly render mode:

```razor
@rendermode InteractiveWebAssembly
```

`InteractiveAuto` works too, running on the server until the WebAssembly runtime has downloaded. A statically
rendered page shows the canvas's markup but never becomes interactive, so a canvas needs an interactive render mode.
The demo itself is a Web App with `<Routes @rendermode="InteractiveWebAssembly" />`.

### Blazor Hybrid

MAUI, WPF and Windows Forms hosts run the canvas in-process and render it in the embedded WebView. Everything works,
through the standard `IJSRuntime`, with no network in between. Link the stylesheet from `wwwroot/index.html`.

### Prerendering

Prerendering is fine. The canvas renders its markup on the server, and measures nodes and handles once it is
interactive in the browser; `FitViewOnInit` waits for that first measurement before fitting, and `OnInit` and
`OnNodesInitialized` are raised only once it has happened.

### Content security policy

The script module is imported with a dynamic `import()` from your own origin, so `script-src 'self'` covers it, and
it needs no `eval`. The canvas positions nodes, labels and overlays with inline `style` attributes, so the policy has
to allow those (`style-src-attr 'unsafe-inline'`, or `'unsafe-inline'` in `style-src`). If you serve static web
assets from a CDN, allow that origin.

## Architecture

### The shape of the system

```text
BlowCanvas                 owns the interaction state; cascades IBlowContext to everything inside it
  |-- viewport layer       one element transformed by translate(x, y) scale(zoom)
  |     |-- edge layer     one SVG in flow coordinates: edges, markers, the connection line
  |     |-- nodes          absolutely positioned elements, each wrapping NodeTemplate content
  |     |-- overlays       selection box, alignment guides, viewport portals
  |-- HTML label layer     EdgeLabelTemplate content at screen positions, not zoomed
  |-- ChildContent         add-ons in screen space
  |-- live region          screen reader announcements
bit-blow.js                measurement, resize observation, hit testing, pointer capture, input guards
```

`BlowCanvas` cascades itself as `IBlowContext` - the viewport, the pane size, the zoom limits and the calls that start
connections, resizes and drags - which is how handles, resizers, toolbars, the control bar and the minimap talk to it.
The interface is public, so an add-on of your own can read the viewport the same way. Taken unnamed
(`[CascadingParameter] IBlowContext`), it re-renders the add-on on every render of the canvas - simple, and fine for
a panel or two. The built-in parts take it by name instead, as a fixed value, together with version numbers that
change exactly when the viewport, the nodes' geometry (where they are, their size, whether they are being dragged),
the graph or the connection state do; an add-on of your own can do the same to re-render only when what it shows has
changed:

```csharp
[CascadingParameter(Name = BlowCascadingNames.Context)] private IBlowContext? Flow { get; set; }
[CascadingParameter(Name = BlowCascadingNames.ViewportVersion)] private int ViewportVersion { get; set; }
```

Blazor re-renders a subscriber to a cascading `int` only when its value changes, so this costs nothing on the
frames that do not concern it.

### One pass per render

At the top of every render the canvas resolves the whole graph once: it rebuilds the id lookup (reporting duplicate
ids), walks each node's parent chain with memoization to get its absolute position and nesting depth, applies the
node origin, builds the lists of nodes and edges to draw (dropping hidden ones and, with culling, invisible ones),
orders them for stacking, and collects the arrowheads in use. Edge paths come from the per-edge cache when nothing
they depend on has changed. Every loop in the markup then reads the same geometry.

The same pass settles what the render may have changed (the viewport alone, the nodes' geometry, or anything) and
advances the version numbers the canvas cascades. Each node and edge renderer compares what it draws from - its
element's state, the shared settings object, for an edge the cached geometry - with what it drew last, and skips the
render when nothing differs.

### Measuring the DOM

Edge endpoints come from measured handle positions, which is what lets a custom node put its handles anywhere. After
a render that may have changed geometry, the browser layer reads every node's size and every handle's centre,
divided by the scale the viewport is actually drawn at - measured on screen rather than taken from .NET, which
might be a step ahead mid-zoom, and which also accounts for a CSS transform on an ancestor. It remembers what it last
reported of each node and reports only what differs, plus the nodes the canvas names: the node objects it has had no
report for (just added, or replaced by others with the same id). Results are compared with what the canvas holds, and
only a real difference causes another render. A `ResizeObserver` on the pane and on every
node, and a `MutationObserver` for nodes and handles coming and going, trigger measurements C# could not otherwise
know it needs.

### The browser layer

`bit-blow.js` contains no graph model, no geometry and no interaction logic. It does what only the DOM can:

1. Measures nodes and handles, and the pane's position on the page.
2. Watches the pane and the nodes for size changes and ancestor scrolling.
3. Finds the handle under a point, to resolve a connection drop while the pane holds the pointer.
4. Sets and releases pointer capture, so a gesture survives the pointer leaving the canvas.
5. Installs the `nodrag`/`nopan`/`nowheel` guards and the parts of the input policy that must be decided while an
   event is still being dispatched - whether a wheel may scroll the page, and which keys must not - because a DOM
   event can only be cancelled synchronously and cannot wait for .NET.
6. Reports whether focus is in a text field.

Pointer, wheel and keyboard input otherwise arrive as ordinary Blazor events. Everything the module does is scoped to
its own canvas, so several flows on one page, and flows nested inside nodes, do not see each other's nodes or
handles.

### How changes flow

The canvas works on your objects. A drag writes `Position` on the nodes as the pointer moves, selection writes
`Selected`, a connection appends to `Edges`, a deletion removes from both lists. The gesture state a template may
want (`Dragging`, `Resizing`, `Measured`) lives on the node too, read-only to you. When an edit completes, the canvas
raises `NodesChanged` or `EdgesChanged` with the same list instance, records a history step if history is on, and
re-renders. Nothing is copied except by `ToObject`, the history and the clipboard, which take deep copies precisely so
that later edits cannot reach back into them.

## Upgrading

Most of what is new is additive: new parameters with defaults that keep the old behaviour, new types and methods.
These are the changes an existing application can notice.

| Change | What to do |
| --- | --- |
| Node and edge templates are no longer re-rendered on every frame of a pan or a zoom. | A template that reads the viewport through a canvas reference wraps that part in `BlowViewportView` (see [templates and the viewport](#templates-and-the-viewport)). |
| `BlowNodeRenderer` and `BlowEdgeRenderer` take `Settings` and `Callbacks` objects instead of one parameter per value. | Nothing, unless you render them yourself, which is not supported; the canvas does. |
| An add-on written against the cascaded `IBlowContext` still re-renders with every canvas render. | To re-render only when what it draws changes, take the named cascades in `BlowCascadingNames` instead. |
| `BlowHandle` reports hovering to the canvas for every handle, so a refused handle says why. | Nothing. |
| A parent cycle is reported (`parent-cycle`) and cut where it is found instead of flinging nodes off screen. | Fix the data; the error says which node. |
| An edge without a label is named after its nodes' names ("Edge from Start to Done"), not their ids; `EdgeLabel`, `EdgeSelected` and `EdgeDeselected` receive names. | A translation of those strings keeps working; it now receives names. |
| A focused node's and edge's description starts with its state ("Selected. 2 incoming connections."), then the instructions; node instructions mention the connection key. | Set `NodeState`/`EdgeState` to return an empty string, or `ConnectionKey` to null, to keep the old description. |
| `C` on a focused node starts a connection (`ConnectionKey`). | Set `ConnectionKey` to null if the app uses the plain key itself. |
| A pannable or zoomable minimap is in the tab order. | Nothing; set neither to keep it a picture. |
| The built-in history shares unchanged copies between steps and records labels. | Nothing. `BlowHistory.Capture(snapshot)` behaves as before. |
| `EditAsync` with `recordHistory: false` rewrites the recorded steps (`Rebase`) instead of leaving them. | Nothing; it is what the parameter always promised. |
| `BlowLayoutOptions` declares `Arrange` (it implements `IBlowLayout`). | A class deriving from it - none should - implements `Arrange`. |
| `BlowEdgeType.Routed` is a new last member. | A `switch` over the enum without a default case gains one value to handle. |
| `BlowNode.Collapsed` is a new persistent property: compared by the history, written by the serializer. | Nothing. |
| `BlowCanvas.Routing` and `BlowSvgExportOptions.Routing` are `IBlowEdgeRouter`, which `BlowRoutingOptions` implements. | Nothing, unless code reads the options back through the parameter: cast, or keep the options in a field. |
| `C` on a focused edge picks up its end to move it, the edge instructions say so, and a reconnect is announced. | Set `ConnectionKey` to null, or `EdgesReconnectable` to false, to turn it off. |
| A node or edge whose label is blank is named as one without a label ("Node a"), not with an empty name. | Nothing. |
| Letting go of a dragged node re-renders the nodes that moved, not every node. A template that shows state from outside the graph refreshes when the host re-renders, as every template does. | Nothing, unless a template changes on a drop without the host re-rendering; re-render the host (handling `NodesChanged` does). |
| A force layout of more than 500 nodes is approximated (Barnes-Hut), so its picture differs from before. | Set `Theta` to 0 for the exact layout. |
| A mouse click on a node raises `OnNodeClick`. In Chromium it used to raise `OnPaneClick`, because the press captures the pointer on the pane and the browser fires the click there. | Drop any workaround that treated those pane clicks as node clicks. |
| Letting go of a drag changes `BlowCascadingNames.GeometryVersion`, not `GraphVersion`, so an add-on that shows which nodes are being dragged takes the geometry version. | Take `GeometryVersion` in such an add-on. |
| `UpdateNodeInternals(ids)` re-reads the named nodes only; `UpdateNodeInternals()` and `Refresh(remeasure: true)` re-read all of them. | Nothing; name the nodes whose handles moved, or none. |

## Coming from React Flow

Blow follows React Flow's concepts and, wherever C# allows, its names. The main difference is state: there are no
change objects to apply. The canvas edits your lists in place and tells you afterwards, and a connection adds its own
edge.

| React Flow | Blow |
| --- | --- |
| `<ReactFlow>` | `<BlowCanvas>` |
| `useReactFlow()`, `ReactFlowInstance` | a `@ref` to `BlowCanvas` (also handed to `OnInit`) |
| `<ReactFlowProvider>` | not needed: pass the `@ref` to whatever needs it |
| `nodes`, `edges` | `Nodes`, `Edges` - `List<BlowNode>`, `List<BlowEdge>` |
| `onNodesChange` + `applyNodeChanges` | nothing to apply; `NodesChanged` after the canvas has edited the list |
| `onConnect` + `addEdge` | the edge is added for you; `OnConnect` is for side effects |
| `nodeTypes` | `NodeTemplate`, switching on `node.Type`, with `<BlowDefaultNode>` for the rest |
| `edgeTypes` | `EdgeTemplate`, switching on `ctx.Edge.Type` or `ctx.Edge.Data` |
| `<Handle type="source" position={Position.Right} id="a" />` | `<BlowHandle Type="BlowHandleType.Source" Position="BlowPosition.Right" Id="a" />` |
| `isConnectable`, `isConnectableStart`, `isConnectableEnd` on a handle | `IsConnectable`, `IsConnectableStart`, `IsConnectableEnd` |
| `<BaseEdge>`, `<EdgeText>`, `<EdgeLabelRenderer>` | `<BlowBaseEdge>`, `<BlowEdgeText>`, `EdgeLabelTemplate` |
| `getBezierPath`, `getSimpleBezierPath`, `getSmoothStepPath`, `getStraightPath` | `BlowEdgePath.Bezier`, `SimpleBezier`, `SmoothStep`, `Straight` |
| `MarkerType.Arrow`, `MarkerType.ArrowClosed` | `BlowMarkerType.Arrow`, `BlowMarkerType.ArrowClosed` |
| `pathOptions` | `BlowEdge.PathOptions` (`BlowEdgePathOptions`) |
| `defaultEdgeOptions` | `DefaultEdgeOptions` (`BlowDefaultEdgeOptions`) |
| `defaultMarkerColor` | `DefaultMarkerColor` |
| `<Background variant={BackgroundVariant.Dots}>` | `<BlowBackground Variant="BlowBackgroundVariant.Dots">` |
| `<Controls>`, `<ControlButton>` | `<BlowControls>`, `<BlowControlButton>` |
| `<MiniMap>` | `<BlowMiniMap>` |
| `<Panel position="top-right">` | `<BlowPanel Position="top-right">` or `Anchor="BlowPanelPosition.TopRight"` |
| `<NodeResizer>`, `<NodeResizeControl>` | `<BlowNodeResizer>`, `<BlowNodeResizeControl>` |
| `<NodeToolbar>` | `<BlowNodeToolbar>` |
| `<EdgeToolbar>` | `<BlowEdgeToolbar>` inside `EdgeLabelTemplate` |
| `<ViewportPortal>` | `<BlowViewportPortal>` |
| `dragHandle` on a node | `UseCustomDragHandle = true` plus `<BlowDragHandle>` |
| `parentId`, `extent: 'parent'`, `expandParent` | `ParentId`, `ExtentParent`, `ExpandParent` |
| `extent: [[x1, y1], [x2, y2]]` | `Extent = new BlowRect(x, y, width, height)` |
| the `fitView` and `fitViewOptions` props | `FitViewOnInit`, `FitViewOptions` |
| `defaultViewport` | `DefaultViewport` |
| `viewport` + `onViewportChange` | `Viewport` + `OnViewportChanged` |
| `onConnectStart`, `onConnectEnd` | `OnConnectStart`, `OnConnectEnd` |
| `isValidConnection` | `IsValidConnection` |
| `connectionLineComponent`, `connectionLineType`, `connectionLineStyle` | `ConnectionLineTemplate`, `ConnectionLineType`, `ConnectionLineStyle` |
| `useConnection` | `OnConnectionChange` |
| `connectionMode`, `connectionRadius`, `connectOnClick` | `ConnectionMode`, `ConnectionRadius`, `ConnectOnClick` |
| `onReconnect`, `onReconnectStart`, `onReconnectEnd`, `reconnectRadius`, `edgesReconnectable` | `OnReconnect`, `OnReconnectStart`, `OnReconnectEnd`, `ReconnectRadius`, `EdgesReconnectable` |
| `onBeforeDelete` | `OnBeforeDelete` - synchronous, returning `bool` |
| `onNodesDelete`, `onEdgesDelete`, `onDelete` | `OnNodesDelete`, `OnEdgesDelete`, `OnDelete` |
| `deleteKeyCode`, `selectionKeyCode`, `multiSelectionKeyCode` | `DeleteKeys`, `SelectionKey`, `MultiSelectionKey` |
| `panActivationKeyCode`, `zoomActivationKeyCode` | `PanActivationKey`, `ZoomActivationKey` |
| `selectionOnDrag`, `selectionMode` | `SelectionOnDrag`, `SelectionMode` |
| `panOnDrag` (boolean or button list) | `PanOnDrag` and `PanButtons` |
| `panOnScroll`, `panOnScrollMode`, `panOnScrollSpeed` | `PanOnScroll`, `PanOnScrollMode`, `PanOnScrollSpeed` |
| `zoomOnScroll`, `zoomOnPinch`, `zoomOnDoubleClick`, `preventScrolling` | `ZoomOnScroll`, `ZoomOnPinch`, `ZoomOnDoubleClick`, `PreventScrolling` |
| `minZoom`, `maxZoom`, `translateExtent`, `nodeExtent` | `MinZoom`, `MaxZoom`, `TranslateExtent`, `NodeExtent` |
| `snapToGrid`, `snapGrid={[x, y]}` | `SnapToGrid`, `SnapGridX`, `SnapGridY` |
| `nodeOrigin` | `NodeOrigin` |
| `nodeDragThreshold`, `connectionDragThreshold`, `nodeClickDistance`, `paneClickDistance` | the same names, in PascalCase |
| `autoPanOnNodeDrag`, `autoPanOnConnect`, `autoPanOnNodeFocus`, `autoPanSpeed` | the same names, in PascalCase |
| `selectNodesOnDrag`, `elevateNodesOnSelect`, `elevateEdgesOnSelect`, `zIndexMode` | the same names, in PascalCase |
| `nodesDraggable`, `nodesConnectable`, `elementsSelectable`, `nodesFocusable`, `edgesFocusable`, `disableKeyboardA11y` | the same names, in PascalCase |
| `onlyRenderVisibleElements`, `colorMode` | `OnlyRenderVisibleElements`, `ColorMode` |
| `noDragClassName`, `noPanClassName`, `noWheelClassName` | `NoDragClass`, `NoPanClass`, `NoWheelClass` |
| `onInit`, `onError` | `OnInit`, `OnError` |
| `useNodesInitialized` | `OnNodesInitialized`, `NodesInitialized` |
| `useUpdateNodeInternals` | `UpdateNodeInternals()` |
| `useOnSelectionChange`, `onSelectionChange` | `OnSelectionChange` |
| `onSelectionDragStart`, `onSelectionDrag`, `onSelectionDragStop`, `onSelectionContextMenu` | the same names, in PascalCase |
| `onMoveStart`, `onMove`, `onMoveEnd` | `OnMoveStart`, `OnMove`, `OnMoveEnd` |
| `fitView`, `setViewport`, `getViewport`, `setCenter`, `fitBounds`, `zoomIn`, `zoomOut`, `zoomTo`, `getZoom` | the same names, in PascalCase; a `duration` becomes the `...Async` twin |
| `screenToFlowPosition`, `flowToScreenPosition` | `ScreenToFlowPosition`, `FlowToScreenPosition` |
| `getNode`, `getEdge`, `addNodes`, `addEdges`, `deleteElements` | `GetNode`, `GetEdge`, `AddNodes`, `AddEdges`, `DeleteElementsAsync` |
| `getIntersectingNodes`, `isNodeIntersecting` | `GetIntersectingNodes`, `IsNodeIntersecting` |
| `getNodeConnections`, `getHandleConnections` | `GetNodeConnections`, `GetHandleConnections` |
| `toObject()` | `ToObject()`, returning a `BlowSnapshot`; `RestoreAsync` puts one back |
| `addEdge`, `reconnectEdge`, `getConnectedEdges`, `getIncomers`, `getOutgoers` | `BlowGraphUtils.AddEdge`, `ReconnectEdge`, `GetConnectedEdges`, `GetIncomers`, `GetOutgoers` |
| `getNodesBounds`, `getViewportForBounds` | `BlowGraphUtils.GetNodesBounds`, `GetViewportForBounds` |

What has no React Flow counterpart: the built-in layouts (`BlowLayout`), align and distribute (`BlowAlign`), alignment
guides, the built-in undo stack (`HistoryEnabled`, `BlowHistory`), the clipboard (`ClipboardShortcutsEnabled`,
`BlowClipboard`), declared ports with data types and limits (`BlowPort`), rejection reasons and cycle prevention
(`BlowConnectionRules`, `PreventCycles`), asynchronous connection confirmation (`OnBeforeConnect`), graph algorithms
(`BlowGraphAlgorithms`) and validation (`BlowGraphValidator`) shown on the graph (`Validation`), typed change events
and transactions (`OnGraphChanged`, `EditAsync`), grouping, collapsing and stacking commands, the versioned JSON
format (`BlowSerializer`) and SVG export (`ToSvg`), obstacle-avoiding edges (`BlowEdgeType.Routed`), run states
(`RunStates`), connections from the keyboard (`ConnectionKey`), and search and focus helpers (`FindNodes`,
`FocusNodeAsync`). `BlowReactFlowFormat` reads and writes React Flow's `toObject()` shape.
