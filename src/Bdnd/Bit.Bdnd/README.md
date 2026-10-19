# Bit.Bdnd

A declarative drag-and-drop library for Blazor, inspired by [React DnD](https://react-dnd.github.io/react-dnd/).
Instead of hooks, Bdnd gives you composable components - `BdndDraggable`, `BdndDropTarget`, `BdndDragLayer` and
`BdndDragHandle` - backed by the HTML5 drag-and-drop API. You drag *data* (a typed item) of a *type* (a string),
and drop targets accept the types they list. The drag state lives in C#; the browser layer only configures the
native drag, tracks hover and reads native drops.

## Quick start

```bash
dotnet add package Bit.Bdnd
```

```csharp
// Program.cs
builder.Services.AddBitBdndServices();
```

```razor
@* _Imports.razor *@
@using Bit.Bdnd
```

No stylesheet to link: the script module, `_content/Bit.Bdnd/bit-bdnd.js`, is imported by the components on demand.

```razor
<BdndProvider>
    <BdndDraggable TItem="Card" Type="card" Item="card" Context="drag">
        <div class="card" style="opacity: @(drag.IsDragging ? 0.4 : 1)">@card.Title</div>
    </BdndDraggable>

    <BdndDropTarget TItem="Card" Accept="card" OnDrop="HandleDrop" Context="drop">
        <div class="bin @(drop.IsOver && drop.CanDrop ? "active" : "")">Drop cards here</div>
    </BdndDropTarget>
</BdndProvider>

@code {
    private record Card(string Title);

    private Card card = new("Write the docs");

    private void HandleDrop(BdndDropEventArgs<Card> args) => Console.WriteLine($"Dropped {args.Item?.Title}");
}
```

## Features

- Declarative `useDrag` / `useDrop` style components with strongly typed item payloads.
- Type matching between draggables and drop targets through `Accept` / `AcceptTypes`, plus `CanDropPredicate`.
- Custom drag layers for fully styled, pointer-following previews (`BdndDragLayer`).
- Drag handles that restrict where a drag can start (`BdndDragHandle`).
- Nested drag sources and nested drop targets, with shallow `IsOverShallow` hover and innermost-first drops.
- Drop results passed back to the source's `OnEnd`, and `DidDrop` to tell a drop from a cancel.
- Native OS drags: dropped files, selected text, links and HTML (`BdndNativeTypes`).
- Isolated drag contexts through nestable providers, and a read-only `IBdndMonitor` over the live drag state.
- Blazor WebAssembly, Server, Web App (including prerendering) and Hybrid on .NET 8, 9 and 10.

## For AI agents

The demo site hosts an MCP server at `https://bdnd.bitplatform.dev/mcp` (also served through
`https://bitplatform.dev/mcp`) that answers from the shipped library: its API, its guide and the demo's working
pages, and it can run a drag through the real components to show which targets accept it.

## Documentation

The full guide - providers, draggables, drop targets, drag layers, handles, nesting, sortable lists, native drags,
the monitor, hosting models and a React DnD mapping - is in the
[Bdnd README](https://github.com/bitfoundation/bitplatform/tree/develop/src/Bdnd). See it running at
[bdnd.bitplatform.dev](https://bdnd.bitplatform.dev).
