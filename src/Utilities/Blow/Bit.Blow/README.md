# Bit.Blow

A Blazor-native library for node-based UIs - editors, flow charts, pipelines, state machines and diagrams - and the
Blazor counterpart to React Flow. Dragging, selecting, connecting, resizing, the keyboard, layout and edge geometry
are written in C# on Blazor's own event system. JavaScript only measures the DOM, watches it for size changes and
finds the handle under a point - there is no JavaScript diagram library underneath.

## Quick start

```bash
dotnet add package Bit.Blow
```

```razor
@* _Imports.razor *@
@using Bit.Blow
```

Link the stylesheet from your host page (`App.razor` or `index.html`); it is not injected for you. No services to
register: the script module, `_content/Bit.Blow/bit-blow.js`, is imported by the component on demand.

```html
<link rel="stylesheet" href="_content/Bit.Blow/bit-blow.css" />
```

```razor
<div style="height: 500px">
    <BlowCanvas @ref="_flow" Nodes="_nodes" Edges="_edges" FitViewOnInit="true">
        <BlowBackground />
        <BlowControls />
        <BlowMiniMap />
    </BlowCanvas>
</div>

@code {
    private BlowCanvas? _flow;

    private List<BlowNode> _nodes =
    [
        new BlowNode { Id = "1", Type = "input", Label = "Start", Position = new(0, 0) },
        new BlowNode { Id = "2", Label = "Review", Position = new(0, 100) },
        new BlowNode { Id = "3", Type = "output", Label = "Done", Position = new(0, 200) },
    ];

    private List<BlowEdge> _edges =
    [
        new BlowEdge { Id = "e1-2", Source = "1", Target = "2" },
        new BlowEdge { Id = "e2-3", Source = "2", Target = "3", Animated = true },
    ];

    // The canvas edits _nodes and _edges in place; ToObject() + RestoreAsync() save and bring back a graph.
    private BlowSnapshot Save() => _flow!.ToObject();
}
```

Give nodes your own markup with `NodeTemplate` and `BlowHandle`, and edges with `EdgeTemplate` and `BlowBaseEdge`.

## Features

- A pannable, zoomable canvas: wheel zoom toward the cursor, pinch, pan-on-scroll and hold-Space-to-pan.
- Draggable nodes with multi-selection, box selection, snap-to-grid, alignment guides and auto-panning.
- Connections by drag or click, with proximity snapping, live validation on the canvas and per handle, and
  reconnection of existing edges.
- Typed ports with data types, connection limits and required inputs; rejection reasons, cycle prevention and
  asynchronous confirmation of a connection.
- Graph algorithms (topological order, cycles, components, reachability, shortest paths) and a rule-based graph
  validator, all usable with no UI.
- Built-in `input`, `default`, `output` and `group` nodes, and custom node templates.
- Bezier, simple bezier, straight, step and smooth-step edges, and orthogonal routes round the nodes in the way
  from a router you can replace, tunable per edge, with labels, arrowheads, animation and self-loops.
- Sub-flows with parent extents and growing parents, grouping and ungrouping, collapsible groups; node resizing
  and node and edge toolbars.
- Background, controls, minimap, panels and a viewport portal.
- Layered, tree, radial, force, grid and circle auto-layouts for the graph or a selection, align and distribute,
  and animated viewport transitions - all in C#.
- Typed change events with origins, transactions, labelled and structurally shared undo history, edits a
  collaborator made that local undo leaves alone, and copy and paste that rewires edges and keeps groups intact.
- Versioned JSON documents with migrations and bounded imports, React Flow interchange, and SVG export.
- Validation problems and run states shown on the graph by shape as well as colour.
- Keyboard access including connections, reconnecting edges and the minimap, search and focus helpers, element state
  read by screen readers, reduced motion, high contrast and localizable strings.
- `OnError` diagnostics for malformed graphs instead of exceptions.
- Theming through `--bit-blow-*` CSS custom properties, with light, dark and system colour modes.
- Blazor WebAssembly, Server, Web App (including prerendering) and Hybrid on .NET 8, 9 and 10.

## For AI agents

The demo site hosts an MCP server at `https://blow.bitplatform.dev/mcp`
that answers from the shipped library: its API, its guide, its theme properties and
the demo's working pages, and it can inspect a flow you hand it.

## Documentation

The full guide - nodes, handles and connections, edges, interaction, the viewport, layout and alignment, add-ons,
saving, undo and the clipboard, theming, keyboard and accessibility, localization, diagnostics, performance, hosting
models, architecture and a React Flow mapping - is in the
[Blow README](https://github.com/bitfoundation/bitplatform/tree/develop/src/Utilities/Blow). See it running at
[blow.bitplatform.dev](https://blow.bitplatform.dev).
