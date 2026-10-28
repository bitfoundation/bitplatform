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
- Native OS drags: dropped files (streamed, folders expanded, filtered by type and count), selected text, links and HTML (`BdndNativeTypes`), and data of your own carried out of the page (`NativeData`).
- Touch, pen and pointer drags through pointer events, with a press-and-hold that leaves swipes to scroll, a copy that follows the pointer (`PreviewAxis` keeps it on an axis) and auto-scrolling.
- Keyboard dragging (Space/Enter, arrows - by row in a wrapping grid - Escape) with screen-reader announcements and instructions, translatable through `BdndAnnouncements`.
- Insertion positions (`Edges`, `ClosestEdge`) and `BdndList` moves for sortable lists, grids, trees and virtualized lists; several items dragged as one.
- `OnEnter` / `OnLeave` and `Disabled` on targets; `OnDragStart` / `OnDragEnd` on the provider for every drag.
- Styling hooks on the host elements: `data-bit-bdnd-dragging`, `data-bit-bdnd-can-drop`, `data-bit-bdnd-over`, `data-bit-bdnd-over-shallow`.
- Isolated drag contexts through nestable providers, and a read-only `IBdndMonitor` over the live drag state.
- Blazor WebAssembly, Server, Web App (including prerendering) and Hybrid on .NET 10 and 11.

## For AI agents

The demo site hosts an MCP server at `https://bdnd.bitplatform.dev/mcp`
that answers from the shipped library: its API, its guide and the demo's working
pages, and it can run a drag through the real components to show which targets accept it.

## Documentation

The full guide - providers, draggables, drop targets, drag layers, handles, nesting, sortable lists, native drags,
the monitor, hosting models and a React DnD mapping - is in the
[Bdnd README](https://github.com/bitfoundation/bitplatform/tree/develop/src/Utilities/Bdnd). See it running at
[bdnd.bitplatform.dev](https://bdnd.bitplatform.dev).
