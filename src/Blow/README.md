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
- **Built-in node types** (`input`, `default`, `output`, `group`) and a `NodeTemplate` for anything else.
- **Five edge paths** (bezier, simple bezier, straight, step, smooth step), each tunable per edge, with labels,
  arrowheads in any colour and size, animation, and self-connections that loop clear of their node.
- **Sub-flows**: children move with their parent and can be pinned inside it or grow it.
- **Add-ons**: background patterns, a control bar, a minimap, panels, a node resizer, node and edge toolbars, and
  a portal into the zoomed layer.
- **Auto-layout and align/distribute in C#**: layered (with crossing reduction), grid and circle layouts, and the
  align and distribute commands of a drawing tool. No JavaScript layout engine.
- **Animated viewport helpers** that ease instead of cutting, and give way the moment the user takes over.
- **Undo and redo** kept by the canvas or driven by you, and **copy and paste** that rewires edges and keeps
  groups intact.
- **Keyboard and screen reader support**: nodes and edges in the tab order, Enter/Space, arrow keys, Escape and
  Delete, with keyboard edits announced through a live region, and every string localizable.
- **Forgiving**: a malformed graph is reported through `OnError` instead of throwing.
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
so a React Flow user can open the same example on both sides. A handful of hand-written tour pages sit next to the
catalogue (`/custom-nodes`, `/edge-types`, `/custom-edges`, `/interactions`, `/labels-handles`, `/extras`,
`/advanced`, `/connection-line`, `/add-node-on-drop`, `/floating-edges`, `/minimap`, `/drag-threshold`). Every
page can show its own source.

To run it locally:

```bash
dotnet run --project src/Blow/Demo/Server
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
guide and the demo's working pages - instead of guessing at an API it half remembers. It is also served, next to
the other bit libraries, through the aggregated `https://bitplatform.dev/mcp` endpoint.

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
- [Add-ons](#add-ons)
- [Saving, undo and clipboard](#saving-undo-and-clipboard)
- [Theming](#theming)
- [Keyboard and accessibility](#keyboard-and-accessibility)
- [Localization](#localization)
- [Errors and diagnostics](#errors-and-diagnostics)
- [Performance](#performance)
- [Hosting models](#hosting-models)
- [Architecture](#architecture)
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

Measuring is one pass over the whole flow, so there is no cheaper per-node form; the ids it accepts are only there
so the call site reads naturally.

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
  root. A parent chain is cut off at a depth of 50, which also stops an accidental cycle from looping.

Demo: `/examples/subflow`.

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
hidden nodes - pass them only the nodes you want arranged. Demo: `/examples/hidden`.

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

A prospective connection is refused when:

- the canvas's `NodesConnectable` is off, or either node's `Connectable` is off, or the target node is hidden;
- `ConnectionMode` is `Strict` (the default) and both handles have the same type - `Loose` lets any handle connect
  to any other;
- it would connect a node to itself, unless `AllowSelfConnections` is on - and even then it must join two
  different handles;
- an identical edge already exists and `PreventDuplicateEdges` is on (the default);
- the drop handle has `IsConnectableEnd` (or `IsConnectable`) off;
- the canvas's `IsValidConnection`, or the rule of either handle, returns false.

Demo: `/examples/connection-rules`, `/examples/connection-mode`.

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
| `OnConnectEnd` | `BlowConnectEndInfo` | The gesture ended, whether or not it connected. |

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

Dropping an end back where it came from is allowed; the duplicate check ignores the edge being moved. Demo:
`/examples/reconnect-edge`.

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
| `InteractionWidth` | 14 | Width of the invisible path that makes the edge easy to click. |
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

`DefaultEdgeType` sets the type of edges the user creates, and `ConnectionLineType` the line drawn while they
create one. The same algorithms are public as `BlowEdgePath.Bezier`, `SimpleBezier`, `Straight`, `SmoothStep` and
`Compute`, each returning a `BlowPathResult` with the SVG `Path` and the label point. Demo: `/examples/edge-types`,
`/edge-types`.

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
the pointer. A duration of 0 applies the viewport at once, and the synchronous helpers never animate. Demo:
`/examples/viewport-transitions`.

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
| `Grid` | Rows and columns in the order given, in uniform cells sized to the largest node - for tidying a scattered graph. |
| `Circle` | An even ring, clockwise from twelve o'clock - for showing peers rather than a hierarchy. |

| Option | Default | Applies to |
| --- | --- | --- |
| `Origin` | `(0, 0)` | All. Top-left of the laid-out block. |
| `NodeOrigin` | `(0, 0)` | All. Must match the canvas's `NodeOrigin`. |
| `FallbackSize` | 150 by 40 | All. Size assumed for a node that has neither an explicit size nor a measurement. |
| `Direction` | `TopToBottom` | Layered. Also `LeftToRight`, `BottomToTop`, `RightToLeft`. |
| `LayerGap`, `NodeGap` | 80, 40 | Layered. Gaps between layers and within one. |
| `CrossingReductionPasses` | 4 | Layered. 0 keeps the given order within each layer. |
| `AlignHandlesToDirection` | `true` | Layered. Points `SourcePosition` and `TargetPosition` along the flow. |
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

Demo: `/examples/layouting`.

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

```razor
<BlowMiniMap Zoomable="true" NodeColorFunc="@(n => n.Type == "input" ? "#0041d0" : "#e2e2e2")" />
```

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

For storage, map the snapshot to records of your own and serialize those. The format is then yours to version, it
leaves out what the canvas maintains for itself (measured sizes, handle anchors, drag state), and your `Data`
payloads get a concrete type rather than `object`:

```csharp
private record SavedNode(string Id, string Type, string? Label, double X, double Y, double? Width, double? Height);
private record SavedEdge(string Id, string Source, string Target, string? SourceHandle, string? TargetHandle, BlowEdgeType Type);
private record SavedFlow(List<SavedNode> Nodes, List<SavedEdge> Edges, double X, double Y, double Zoom);

private string Save()
{
    var s = _flow!.ToObject();
    return System.Text.Json.JsonSerializer.Serialize(new SavedFlow(
        s.Nodes.Select(n => new SavedNode(n.Id, n.Type, n.Label, n.Position.X, n.Position.Y, n.Width, n.Height)).ToList(),
        s.Edges.Select(e => new SavedEdge(e.Id, e.Source, e.Target, e.SourceHandle, e.TargetHandle, e.Type)).ToList(),
        s.Viewport.X, s.Viewport.Y, s.Viewport.Zoom));
}

private async Task LoadAsync(string json)
{
    var f = System.Text.Json.JsonSerializer.Deserialize<SavedFlow>(json)!;
    await _flow!.RestoreAsync(new BlowSnapshot(
        f.Nodes.Select(n => new BlowNode { Id = n.Id, Type = n.Type, Label = n.Label, Position = new(n.X, n.Y), Width = n.Width, Height = n.Height }).ToList(),
        f.Edges.Select(e => new BlowEdge { Id = e.Id, Source = e.Source, Target = e.Target, SourceHandle = e.SourceHandle, TargetHandle = e.TargetHandle, Type = e.Type }).ToList(),
        new BlowViewport(f.X, f.Y, f.Zoom)));
}
```

Demo: `/examples/save-restore`.

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
`RedoDepth`) for toolbars.

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

    private void Capture() => _history.Capture(_flow!.ToObject());

    private async Task Undo() { if (_history.Undo() is { } s) await _flow!.RestoreAsync(s); }
    private async Task Redo() { if (_history.Redo() is { } s) await _flow!.RestoreAsync(s); }
}
```

`BlowHistory` holds snapshots: `Capture` records one and returns false for a no-op, `Undo` and `Redo` return the
snapshot to restore or null, and `Clear`, `Current`, `CanUndo`, `CanRedo`, `UndoDepth`, `RedoDepth` and `Capacity`
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
| `Enter`, `Space` | Select the focused node or edge, replacing the selection. With `Shift`, toggle it within the selection. |
| Arrow keys | Move the selected nodes 5 flow units - one grid cell with `SnapToGrid`. |
| `Shift` + arrow keys | Move them 25 flow units. |
| `Escape` | Abandon the gesture in progress - a drag, a selection box, a connection, a resize or a pinch - putting back anything it had moved; otherwise clear the selection. |
| `Delete`, `Backspace` | Delete the selection (`DeleteKeys`, `DeleteKeyEnabled`). |
| `Ctrl`/`Cmd` + `A` | Select every selectable, visible node and edge (`SelectAllKeyEnabled`). |
| `Ctrl`/`Cmd` + `Z` | Undo (`HistoryEnabled`). |
| `Ctrl`/`Cmd` + `Shift` + `Z`, `Ctrl`/`Cmd` + `Y` | Redo (`HistoryEnabled`). |
| `Ctrl`/`Cmd` + `C`, `X`, `V`, `D` | Copy, cut, paste, duplicate (`ClipboardShortcutsEnabled`). |
| Hold `Space` | Drag on the pane to pan, even with `SelectionOnDrag` (`PanActivationKey`). |
| Hold `Shift` | Drag on the pane to draw a selection box (`SelectionKey`); click to add to the selection (`MultiSelectionKey`). |

Graph keys are ignored while focus is in a text input, textarea, select or contenteditable element, so nodes can
host editable content safely: Backspace edits the text rather than deleting the node. Escape still abandons a
gesture. Keys the canvas uses do not also scroll the page.

### Focus and the tab order

Nodes and edges are in the tab order by default, with visible focus rings. Focus follows document order, which is
the order they are drawn in: the edges, which sit beneath the nodes, and then the nodes. `NodesFocusable` and
`EdgesFocusable` take each kind out of the tab order, `Focusable` on a node or edge takes out one, and
`DisableKeyboardA11y` takes everything out and turns arrow-key movement off.

### What a screen reader hears

The canvas is named by `AriaLabel` ("Flow diagram"). Each node is a group whose role description is "node" and whose
name is its `AriaLabel`, else its `Label`, else "Node {id}"; each edge is a group described as "edge" and named by
its `Label`, else "Edge from {source} to {target}". Selecting with Enter or Space, moving with the arrow keys,
aligning or distributing, and deleting with the delete keys are announced through a visually hidden, polite
`aria-live` region. Repeating the same announcement - nudging a node twice in the same direction - is still spoken
the second time. `Announce(message)` puts a message of your own in the same region. How to replace these strings is
in [localization](#localization). Demo: `/examples/a11y`, `/examples/keyboard-a11y`.

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

The members are functions rather than format strings, so a translation is free to reorder or drop the values it is
handed - which a positional format string cannot do in a language whose word order differs. A member left out keeps
its English default. Edge strings receive the node ids; the node name is the node's `AriaLabel`, else its `Label`,
else its id. A moved position is written in the current culture, with a semicolon between the coordinates where the
decimal separator is a comma.

A few strings live elsewhere: the canvas's own `AriaLabel`, `AriaLabel` on `BlowControls` and `BlowMiniMap`, and the
fallback node name "Node {id}", which a `Label` or `AriaLabel` on the node replaces. The tooltips of the built-in
control buttons are English; to translate them, turn the built-ins off with `ShowZoom="false"` and
`ShowFitView="false"` and add `BlowControlButton`s of your own that call `ZoomIn()`, `ZoomOut()` and `FitView()`.
Attributes the canvas does not set itself, such as `aria-roledescription` on the canvas, pass straight through to
its element. Demo: `/examples/localization`.

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
- A parent cycle, or a chain deeper than 50, is cut off rather than looping.
- A handle, resizer, toolbar or drag handle outside a node template, and a background, control bar or minimap
  outside a canvas, render inert instead of throwing a null reference.
- The JavaScript side going away - a dropped circuit, a component disposed mid-call, prerendering - is treated as
  the normal end it is, not as an error.

Demo: `/examples/diagnostics`, `/examples/broken-nodes`.

## Performance

### What the canvas does for you

- **One layout pass per render.** Every node's absolute position is resolved once, with parent chains memoized, so
  the cost is linear in the number of nodes and edges, and every part of the render reads the same geometry.
- **Edge geometry is cached across renders**, keyed by exactly what the path depends on: its anchors and sides, its
  type and options, its markers and, for a loop, its node. Panning and zooming change only the viewport transform,
  so a pan of a large graph rebuilds no path strings and hands the diff identical markup.
- **Arrowheads are shared**: one SVG marker per distinct shape, colour and size.
- **Measuring costs a browser round trip over every node, so it is rare.** It runs after something that can change
  sizes - content, a drag ending, a resize, nodes added - and never during a gesture, a wheel zoom or a viewport
  transition, even when a controlled parent re-renders the canvas on every frame of one.
- **Resize notifications are coalesced** to one per animation frame, and a page scroll only refreshes the pane's
  position without re-measuring.
- **Listeners are attached only when needed**: the pane's pointer-move listener while a gesture runs, and node and
  edge mouse listeners only for events you handle.
- **Connection checks are O(1)**: existing wiring is snapshotted into a set when a connection drag starts, since every
  handle re-checks its validity on every frame of the drag.

### Large graphs

`OnlyRenderVisibleElements` renders only the nodes and edges that intersect the visible area plus a 20% margin on
each side. A node that has not been measured yet is always rendered once, so it can be sized. Edges are culled by
their anchors, padded by the furthest their path can stray, so a long edge whose curve crosses the screen is kept
even when neither end is on it.

```razor
<BlowCanvas Nodes="_nodes" Edges="_edges" OnlyRenderVisibleElements="true" MinZoom="0.05" />
```

The stress page, `/examples/stress`, builds grids of up to 900 nodes and lets you switch culling on and off.

### Habits that keep it fast

- Prefer WebAssembly for heavy editing; see [hosting models](#hosting-models).
- Keep node templates light: every node is a component, and a template with heavy markup is paid for per node.
- Handle `OnMove`, `OnNodeDrag` and `OnNodeResize` cheaply, and save expensive work for `OnMoveEnd`,
  `OnNodeDragStop` and `NodesChanged`. A parent that re-renders on every `OnMove` re-renders on every frame.
- Make many changes, then render once: mutate the lists, then call `Refresh(remeasure: true)` a single time.
- The minimap draws every visible node, culling or not; on a very large graph consider leaving it out.
- Keep `IsValidConnection` and handle rules cheap; they run for every handle on every frame of a connection drag.

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
The interface is public, so an add-on of your own can read the viewport the same way.

### One pass per render

At the top of every render the canvas resolves the whole graph once: it rebuilds the id lookup (reporting duplicate
ids), walks each node's parent chain with memoization to get its absolute position and nesting depth, applies the
node origin, builds the lists of nodes and edges to draw (dropping hidden ones and, with culling, invisible ones),
orders them for stacking, and collects the arrowheads in use. Edge paths come from the per-edge cache when nothing
they depend on has changed. Every loop in the markup then reads the same geometry.

### Measuring the DOM

Edge endpoints come from measured handle positions, which is what lets a custom node put its handles anywhere. After
a render that may have changed geometry, the browser layer reads every node's size and every handle's centre,
divided by the scale the viewport is actually drawn at - measured on screen rather than taken from .NET, which
might be a step ahead mid-zoom, and which also accounts for a CSS transform on an ancestor. Results are compared with
what the canvas holds, and only a real difference causes another render. A `ResizeObserver` on the pane and on every
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
guides, the built-in undo stack (`HistoryEnabled`, `BlowHistory`) and the clipboard (`ClipboardShortcutsEnabled`,
`BlowClipboard`).
