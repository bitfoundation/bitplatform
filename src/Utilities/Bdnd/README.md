# Bit.Bdnd

A declarative drag-and-drop library for Blazor, inspired by [React DnD](https://react-dnd.github.io/react-dnd/).
Where React DnD has hooks, Bdnd has composable components - `BdndDraggable`, `BdndDropTarget`, `BdndDragLayer` and
`BdndDragHandle` - backed by the browser's HTML5 drag-and-drop API. You drag *data* (an item of any C# type) of a
*type* (a string), and drop targets accept the types they list. The drag state lives in C#, in a manager shared by
every component of a `BdndProvider`; the browser layer only does what C# cannot: configure the native drag while
`dragstart` is being dispatched, follow the pointer, track hover and read native drops.

```razor
<BdndProvider>
    @foreach (var card in _cards)
    {
        <BdndDraggable TItem="Card" Type="card" Item="card" Context="drag">
            <div class="card" style="opacity: @(drag.IsDragging ? 0.4 : 1)">@card.Title</div>
        </BdndDraggable>
    }

    <BdndDropTarget TItem="Card" Accept="card" OnDrop="Archive" Context="drop">
        <div class="bin @(drop.IsOver && drop.CanDrop ? "active" : "")">Drop a card to archive it</div>
    </BdndDropTarget>
</BdndProvider>

@code {
    private record Card(string Title);

    private readonly List<Card> _cards = [new("Write the docs"), new("Review the PR")];

    private void Archive(BdndDropEventArgs<Card> args) => _cards.Remove(args.Item!);
}
```

## What it is for

Kanban boards and sortable lists, palettes you drag tools out of, dustbins and trays, file drop zones, chess and
card games, dashboards whose widgets can be rearranged, and any screen where something is picked up in one place and
put down in another - including files, text and links dragged in from outside the browser.

## Highlights

- **Declarative sources and targets**: `BdndDraggable` is React DnD's `useDrag`, `BdndDropTarget` is `useDrop`, and
  their child content receives the collected state (`IsDragging`, `IsOver`, `CanDrop`, ...) to render from.
- **Strongly typed items** matched by **type strings**: a target accepts the types it lists in `Accept` or
  `AcceptTypes`, and `CanDropPredicate` narrows that further from the item itself.
- **Custom drag layers**: hide the native drag image and render your own pointer-following preview with
  `BdndDragLayer`, from the live offsets.
- **Drag handles**: restrict where a drag may start with `BdndDragHandle`, while the rest of the item stays
  interactive.
- **Nesting**: nested drag sources drag the innermost one; nested drop targets report deep and shallow hover, and
  drops reach them innermost first.
- **Drop results**: a target publishes a result, and the source's `OnEnd` receives it together with `DidDrop`, which
  tells a drop from a cancel.
- **Native OS drags**: accept dropped files, selected text, links and HTML with `BdndNativeTypes`.
- **Isolated contexts** through nestable providers, and a read-only `IBdndMonitor` over the live drag state.
- **Robust** across re-renders: a dragged item may be re-parented, re-keyed or removed mid-drag and its `OnEnd` still
  runs; hover survives the target's children being re-rendered.
- Works on **Blazor WebAssembly, Blazor Server, Blazor Web App and Blazor Hybrid**, on .NET 8, 9 and 10.

## Quick start

```bash
dotnet add package Bit.Bdnd
```

Register the service in `Program.cs` (in every project that renders the components - in a Blazor Web App, both
the server and the client):

```csharp
builder.Services.AddBitBdndServices();
```

Add the namespace to `_Imports.razor`:

```razor
@using Bit.Bdnd
```

Wrap the region that drags and drops in a `BdndProvider` and declare sources and targets inside it:

```razor
<BdndProvider>
    <BdndDraggable TItem="string" Type="box" Item="@("crate")" Context="drag">
        <div class="box" style="opacity: @(drag.IsDragging ? 0.4 : 1)">Drag me</div>
    </BdndDraggable>

    <BdndDropTarget TItem="string" Accept="box" OnDrop="@(e => _dropped = e.Item)" Context="drop">
        <div class="dustbin">@(drop.IsOver ? "Release!" : _dropped ?? "Drop here")</div>
    </BdndDropTarget>
</BdndProvider>

@code {
    private string? _dropped;
}
```

There is no stylesheet: the script module, `_content/Bit.Bdnd/bit-bdnd.js`, is imported by the components
themselves the first time they need it. The full walkthrough is in [getting started](#getting-started).

## Requirements

- .NET 8, .NET 9 or .NET 10
- Blazor WebAssembly, Blazor Server, Blazor Web App or Blazor Hybrid, on an interactive render mode
- A pointer that raises HTML5 drag events (a mouse, a pen or a trackpad). Like React DnD's HTML5 backend, Bdnd has
  no touch backend.

## The demo

The demo site, [bdnd.bitplatform.dev](https://bdnd.bitplatform.dev), ports React DnD's own examples, so a React DnD
user can open the same example on both sides: the chessboard tutorial, dustbins (single and multiple targets, copy
or move, inside an iframe, a stress test), drag-around (naive and with a custom drag layer), nesting (drag sources
and drop targets), sortable lists (simple, cancel on drop outside, a hundred-item stress test), handles and previews,
drop effects, and native files and HTML. Every page can show its own source.

To run it locally:

```bash
dotnet run --project src/Utilities/Bdnd/Demo/Server
```

## How it fits together

```text
BdndProvider        creates a BdndDragDropManager and cascades it
  |
  |-- BdndDraggable<TItem>    drag source: Type + Item, renders from BdndDragState
  |     `-- BdndDragHandle    optional grab region
  |-- BdndDropTarget<TItem>   drop target: Accept + OnDrop, renders from BdndDropState<TItem>
  `-- BdndDragLayer<TItem>    custom preview, renders from BdndDragLayerState<TItem>

BdndDragDropManager  the authoritative drag state, read through IBdndMonitor
  |
bit-bdnd.js          dragstart configuration, pointer tracking, hover tracking, drop effects, native drops
```

Everything ships in one package, `Bit.Bdnd`, under one namespace, `Bit.Bdnd`:

| Part | Contents |
| --- | --- |
| The components | `BdndProvider`, `BdndDraggable<TItem>`, `BdndDropTarget<TItem>`, `BdndDragLayer<TItem>`, `BdndDragHandle`. |
| The state | `BdndDragDropManager` and its read-only view `IBdndMonitor`; the collected states `BdndDragState`, `BdndDropState<TItem>`, `BdndDragLayerState<TItem>`; the callback arguments `BdndDropEventArgs<TItem>`, `BdndDragEndContext<TItem>` and `BdndNativeDropEventArgs`. |
| The values | `BdndXYCoord`, `BdndNativeTypes`, `BdndNativeFile`, `BdndDomRect`. |
| The services | `AddBitBdndServices()` and `BdndInterop`, which also measures elements for you. |

## MCP server

The demo hosts a [Model Context Protocol](https://modelcontextprotocol.io) server at
`https://bdnd.bitplatform.dev/mcp`, so an AI agent writing drag-and-drop code reads the library itself - its API,
this guide and the demo's working pages - instead of guessing at an API it half remembers.

```json
{
  "mcpServers": {
    "bdnd": { "type": "http", "url": "https://bdnd.bitplatform.dev/mcp" }
  }
}
```

Start with `SearchBdnd`, which searches the guide, every public type and member, the setup guides and the demo's
pages at once, and points each hit at the exact follow-up call. `GetBdndSetupGuide` gives the wiring for one render
mode, `GetBdndGuideSection` one section of this guide, `GetBdndApi` one public type with its members, real defaults
and documentation, and `GetBdndSourceFile` one of the demo's working pages; each returns its index when called
without a key. `InspectBdndDrop` runs a drag - a draggable's type and a tree of drop targets, as JSON - through the
real components and reports which targets can drop, which are hovered, the order the drops run in and what `OnEnd`
receives, or, called with no scenario, returns a sample one. Every tool is read-only.

## Documentation

- [Getting started](#getting-started)
- [The provider](#the-provider)
- [Draggables](#draggables)
- [Drop targets](#drop-targets)
- [Nesting](#nesting)
- [Custom drag layers](#custom-drag-layers)
- [Drag handles](#drag-handles)
- [Sortable lists](#sortable-lists)
- [Native drags](#native-drags)
- [The monitor](#the-monitor)
- [Hosting models](#hosting-models)
- [Troubleshooting](#troubleshooting)
- [Architecture](#architecture)
- [Coming from React DnD](#coming-from-react-dnd)
- [Component reference](#component-reference)

## Getting started

### Install

```bash
dotnet add package Bit.Bdnd
```

### Register the service

```csharp
using Bit.Bdnd;

builder.Services.AddBitBdndServices();
```

`AddBitBdndServices()` registers `BdndInterop`, the scoped wrapper around the script module that every draggable
and drop target injects. Calling it more than once is harmless. The drag-and-drop manager itself is not a service:
each `BdndProvider` creates its own and shares it with the components inside it.

In a Blazor Web App with an interactive client, register it in both `Program.cs` files: the pages are prerendered on
the server before they run in the browser, so anything a component injects has to resolve in both containers.

### A first drag and drop

```razor
@page "/kanban"
@rendermode InteractiveWebAssembly

<BdndProvider>
    <div class="columns">
        @foreach (var column in _columns)
        {
            <BdndDropTarget TItem="TodoItem" Accept="todo" OnDrop="@(e => Move(e.Item!, column))" Context="drop">
                <section class="column @(drop.IsOver && drop.CanDrop ? "highlight" : "")">
                    <h4>@column</h4>
                    @foreach (var todo in _todos.Where(t => t.Column == column))
                    {
                        <BdndDraggable TItem="TodoItem" Type="todo" Item="todo" Context="drag" @key="todo">
                            <div class="todo" style="opacity: @(drag.IsDragging ? 0.3 : 1)">@todo.Title</div>
                        </BdndDraggable>
                    }
                </section>
            </BdndDropTarget>
        }
    </div>
</BdndProvider>

@code {
    private class TodoItem
    {
        public required string Title { get; init; }
        public required string Column { get; set; }
    }

    private readonly string[] _columns = ["To do", "Doing", "Done"];

    private readonly List<TodoItem> _todos =
    [
        new() { Title = "Write the docs", Column = "To do" },
        new() { Title = "Review the PR", Column = "Doing" },
    ];

    private void Move(TodoItem todo, string column) => todo.Column = column;
}
```

Three things make this work: the components sit inside one `BdndProvider`; the draggable's `Type` is one of the
types the target lists in `Accept`; and the page is interactive, because a statically rendered page never wires up
drag events.

### Where to go next

- Reorder items in a list: [sortable lists](#sortable-lists).
- Render your own preview instead of the browser's ghost image: [custom drag layers](#custom-drag-layers).
- Only start a drag from a grip: [drag handles](#drag-handles).
- Accept files dropped from the desktop: [native drags](#native-drags).

## The provider

`BdndProvider` is the root of a drag-and-drop context, the equivalent of React DnD's
`<DndProvider backend={HTML5Backend}>`. It creates a `BdndDragDropManager` and cascades it to every descendant
`BdndDraggable`, `BdndDropTarget` and `BdndDragLayer`. A draggable, drop target or drag layer outside a provider
throws an `InvalidOperationException` when it initializes.

### One context per provider

A drag started by a draggable is seen by the drop targets and drag layers of the same provider. Put a provider
around each region that drags independently - or one around the whole layout when items move between regions.

### Nesting providers

Providers nest. The components inside an inner provider belong to its manager and ignore drags of the outer one:
an item of the outer provider is not accepted by a target of the inner provider, even when its type matches, and a
nested target's shallow hover only considers targets of its own provider.

### Supplying your own manager

Pass `Manager` to share one manager between two providers, or to read the monitor from outside the subtree:

```razor
<BdndProvider Manager="_manager">...</BdndProvider>

@code {
    private readonly BdndDragDropManager _manager = new();

    private bool IsBusy => _manager.Monitor.IsDragging();
}
```

The manager of a provider cannot be changed once set; replacing it throws, because the descendants keep the manager
they were initialized with. `BdndDragDropManager` raises `StateChanged` when a drag begins or ends or a hover
changes, and `OffsetChanged` as the pointer moves.

## Draggables

`BdndDraggable<TItem>` renders a host element (a `div`, or the `Tag` you choose) and wires it into the system as a
drag source. Every attribute it does not know - `class`, `style`, `id`, `data-*` - goes onto that element.

### Type and item

`Type` is required: it is the string drop targets match against their `Accept` list. `Item` is the payload a target
receives. Use a constant class for the type strings, as the React DnD docs recommend:

```csharp
public static class ItemTypes
{
    public const string Card = "card";
    public const string Knight = "knight";
}
```

```razor
<BdndDraggable TItem="Card" Type="@ItemTypes.Card" Item="card">...</BdndDraggable>
```

### Collected state

The child content receives a `BdndDragState`: `IsDragging` (this source is the one being dragged), `CanDrag` and the
`Monitor`. Name it with `Context`:

```razor
<BdndDraggable TItem="Card" Type="card" Item="card" Context="drag">
    <div class="card @(drag.IsDragging ? "ghost" : "")">@card.Title</div>
</BdndDraggable>
```

### Lazy items and cancelling a drag

`ItemFactory` produces the item when the drag starts instead of at render time; it wins over `Item`. Returning
`null` cancels the drag (for a value-type `TItem`, use a nullable one such as `int?`, since `0` is a valid item). In
Blazor WebAssembly the native drag is cancelled too; in Blazor Server the browser has
already started it by the time .NET answers, so it goes on as an inert drag that no target accepts.

```razor
<BdndDraggable TItem="Order" Type="order" ItemFactory="() => _selected.Count > 0 ? new Order(_selected) : null">
```

### Allowing and forbidding drags

`CanDrag` (default `true`) turns dragging on and off. `CanDragPredicate` overrides it with a function evaluated at
render time and again when a drag starts. A draggable that cannot drag renders `draggable="false"`.

### Lifecycle: OnBegin and OnEnd

`OnBegin` runs when the drag begins, with the item, before any hover, drop or end callback of that drag. `OnEnd`
runs when it ends - dropped or cancelled - with a `BdndDragEndContext<TItem>`:

| Member | Meaning |
| --- | --- |
| `Item` | The item that was dragged. |
| `DidDrop` | True if a drop target accepted the drop; false for a cancel or a drop on nothing. |
| `DropResult` | The result the handling target published, if any. |
| `Monitor` | The provider's monitor. |

`OnEnd` runs even if the draggable was removed during the drag - for instance because the drop re-rendered the list
it was in. The drag state is cleared right after `OnEnd`'s synchronous part, so an async handler does not keep every
target highlighted.

```razor
<BdndDraggable TItem="Box" Type="box" Item="box" OnEnd="@(e => { if (e.DidDrop) _palette.Remove(box); })">
```

### Drop effects

`DropEffect` sets the native `dataTransfer.effectAllowed`, which is what the browser draws the cursor from: `move`
(default), `copy`, `link`, `copyMove`, `copyLink`, `linkMove` or `all`. With a combined value such as `copyMove`
the user picks with a modifier key (<kbd>Ctrl</kbd>, or <kbd>⌥</kbd> on macOS).

### Host element and attributes

`Tag` picks the host element (`li`, `tr`, `span`...). The host is a real element, so a draggable inside a flex or
grid container is a flex or grid item: style the host itself as the item - `class` and `style` go onto it - rather
than wrapping the item in it. Do not give a draggable `style="display:contents"`: the host is the element the
browser drags and the one Bdnd measures and pictures as the drag image, and an element without a box of its own
measures as an empty rectangle at `0,0` (a drag layer's preview then starts in the corner of the viewport), makes an
empty drag image, and may not start a drag at all. A drop target, which is never measured, can take
`display:contents`, as the sortable list below does.

### Custom isDragging

`IsDragging` is true for the instance that started the drag. When an item can be re-rendered by a different
component instance mid-drag - moved to another list, re-parented - decide it from the item instead:

```razor
<BdndDraggable TItem="Card" Type="card" Item="card"
               IsDraggingOverride="@(m => m.GetItem<Card>()?.Id == card.Id)" Context="drag">
```

## Drop targets

`BdndDropTarget<TItem>` renders a host element (or `Tag`) and wires it into the system as a drop target. Unknown
attributes go onto that element.

### Accepting types

`Accept` takes one type; `AcceptTypes` takes several; both combine. A target accepts a drag whose item type is in
the list - the item's .NET type does not take part:

```razor
<BdndDropTarget TItem="Piece" AcceptTypes="@([ItemTypes.Knight, ItemTypes.Card])" OnDrop="Place">
```

### Collected state

The child content receives a `BdndDropState<TItem>`:

| Member | Meaning |
| --- | --- |
| `IsOver` | A drag is in progress and the pointer is over this target or a target nested in it. |
| `IsOverShallow` | The pointer is over this target and not over a nested one. |
| `CanDrop` | This target can accept the item being dragged - for every compatible in-app drag, hovered or not (a native drag is only known once it hovers the target). |
| `ItemType` | The type of the item being dragged, or null. |
| `Item` | The item being dragged, when it is a `TItem`. |
| `Monitor` | The provider's monitor. |

`CanDrop` without `IsOver` is how a chessboard highlights the legal squares while a piece is in the air.

### Dropping

`OnDrop` receives a `BdndDropEventArgs<TItem>` with the `Item` and the `Monitor`. It only runs for a drag this
target can accept, and the drop's final pointer position is recorded first, so
`Monitor.GetDifferenceFromInitialOffset()` gives the exact distance moved:

```razor
<BdndDropTarget TItem="Box" Accept="box" OnDrop="MoveBox">

@code {
    private void MoveBox(BdndDropEventArgs<Box> e)
    {
        var delta = e.Monitor.GetDifferenceFromInitialOffset()!.Value;
        e.Item!.Left += delta.X;
        e.Item.Top += delta.Y;
    }
}
```

### Drop results

Set `args.Result` in `OnDrop` to tell the source what happened. The source reads it in `OnEnd` as `DropResult`
(or `Monitor.GetDropResult<T>()`). With nested targets every accepting target's `OnDrop` runs, innermost first, and
a non-null result set by an outer target overrides an inner one - React DnD's rule. An outer target can check
`args.Monitor.DidDrop()` to see whether an inner one already handled the drop.

An async `OnDrop` may set `args.Result` after an `await`. The drop counts as soon as the handler starts - an outer
target's `DidDrop()` is true while the inner handler is still running - and the drag ends, running `OnEnd` with the
result, only once every async `OnDrop` of the drop has completed. Until then the drag is still in progress and the
targets keep their hover state, so keep slow work (a confirmation dialog, a long save) out of `OnDrop` - or start it
without awaiting it - when the source does not need its outcome.

### Hovering

`OnHover` runs repeatedly while an item of an accepted type is over the target, with the same arguments as
`OnDrop` - like React DnD's `hover`, also when `CanDropPredicate` refuses the item. It is what sortable lists
reorder from. The `dragover` listener behind it is only attached when `OnHover` has a handler,
because `dragover` fires continuously and, on Blazor Server, each one is a message over the circuit.

### Gating drops with CanDropPredicate

`CanDropPredicate` receives the item and the monitor and decides whether this target accepts it, on top of the type
check:

```razor
<BdndDropTarget TItem="string" Accept="knight" CanDropPredicate="@((_, _) => CanMoveKnight(col, row))"
                OnDrop="@(_ => MoveKnight(col, row))">
```

It is evaluated when the target renders, so keep it cheap and free of side effects.

### Feedback while dragging

Over a target that cannot accept the drag - wrong type, or a predicate answering false - the browser shows its
no-drop cursor and no drop happens; an enclosing target that can accept the item still receives it. Over a target
that can, the cursor follows the source's `DropEffect`.

## Nesting

### Nested drag sources

A draggable inside another draggable drags itself, not its ancestors: the innermost source claims the `dragstart`
and stops it from bubbling to the outer ones. Grab the outer source by the area not covered by the inner one.

### Nested drop targets

Drop targets nest naturally. While the pointer is over an inner target, both it and its ancestors report `IsOver`;
only the innermost hovered one reports `IsOverShallow`. On drop, every accepting target in the chain runs `OnDrop`,
innermost first (see [drop results](#drop-results)).

Targets of a nested provider are skipped when Bdnd works out a target's parent, so shallow hover is decided among
the targets of one provider.

## Custom drag layers

The browser draws a translucent snapshot of the source while it is dragged. To draw your own preview, set
`UseCustomDragLayer="true"` on the draggable - which replaces the native image with an empty one - and render the
preview in a `BdndDragLayer<TItem>` inside the same provider:

```razor
@using System.Globalization

<BdndDragLayer TItem="Card" Context="layer">
    @if (layer.IsDragging && layer.SourceClientOffset is { } pos)
    {
        @* format with the invariant culture: a decimal comma would be invalid CSS *@
        <div class="card preview" style="position: fixed; pointer-events: none; z-index: 1000;
                    left: @(pos.X.ToString(CultureInfo.InvariantCulture))px;
                    top: @(pos.Y.ToString(CultureInfo.InvariantCulture))px;">
            @layer.Item?.Title
        </div>
    }
</BdndDragLayer>
```

The layer's `BdndDragLayerState<TItem>` carries `IsDragging`, `ItemType`, `Item` and the offsets:

| Offset | Meaning |
| --- | --- |
| `InitialClientOffset` | The pointer when the drag started. |
| `InitialSourceClientOffset` | The source element's top-left corner when the drag started. |
| `ClientOffset` | The pointer now. |
| `DifferenceFromInitialOffset` | How far the pointer has moved. |
| `SourceClientOffset` | Where the source's top-left corner would be now: where to draw the preview. |

The layer re-renders on every pointer move, so keep its markup small, give it `pointer-events: none` (or the
preview itself becomes the drop target under the pointer), and position it `fixed`, because the offsets are client
coordinates.

## Drag handles

Set `UseHandle="true"` on a draggable and put a `BdndDragHandle` inside it; drags then only start from the handle,
and the rest of the item - inputs, buttons, selectable text - stays interactive:

```razor
<BdndDraggable TItem="Card" Type="card" Item="card" UseHandle="true">
    <div class="card">
        <BdndDragHandle>
            <span class="grip">⠿</span>
        </BdndDragHandle>
        <input @bind="card.Title" />
    </div>
</BdndDraggable>
```

The handle is the element the browser drags, but Bdnd still pictures the whole draggable as the drag image. The
handle prepends `cursor: move` to its `Style` while the draggable can drag, and takes any other attribute you give it.

## Sortable lists

A sortable list is a list of cards that are each both a draggable and a drop target. While a card is hovered, its
`OnHover` compares the pointer with the hovered card's vertical midpoint and moves the dragged card past it - React
DnD's algorithm, which avoids flicker:

```razor
@inject BdndInterop Interop

<BdndDraggable TItem="DragItem" Type="card" Item="_dragItem" Context="d">
    <BdndDropTarget TItem="DragItem" Accept="card" OnHover="HandleHover" style="display:contents">
        <div class="card" @ref="_element" style="opacity: @(d.IsDragging ? 0.2 : 1)">@Card.Text</div>
    </BdndDropTarget>
</BdndDraggable>

@code {
    public class DragItem { public int Id { get; init; } public int Index { get; set; } }

    [Parameter, EditorRequired] public CardModel Card { get; set; } = default!;
    [Parameter] public int Index { get; set; }
    [Parameter] public EventCallback<(int From, int To)> OnMove { get; set; }

    private ElementReference _element;
    private DragItem _dragItem = new();

    protected override void OnParametersSet()
    {
        if (_dragItem.Id != Card.Id) _dragItem = new DragItem { Id = Card.Id };
        _dragItem.Index = Index;
    }

    private async Task HandleHover(BdndDropEventArgs<DragItem> e)
    {
        if (e.Item is not { } item || item.Index == Index) return;
        var rect = await Interop.GetBoundingClientRectAsync(_element);
        if (rect is null || e.Monitor.GetClientOffset() is not { } pointer) return;

        // read both indices before moving: the move re-renders the list, and this card's Index with it
        var (dragIndex, hoverIndex) = (item.Index, Index);
        if (dragIndex == hoverIndex) return;

        var middle = rect.Height / 2;
        var y = pointer.Y - rect.Top;
        if (dragIndex < hoverIndex && y < middle) return;
        if (dragIndex > hoverIndex && y > middle) return;

        await OnMove.InvokeAsync((dragIndex, hoverIndex));
        item.Index = hoverIndex; // the dragged item now sits where this card was
    }
}
```

Four details matter:

- Render the cards with `@key`, so a reorder moves elements instead of re-binding them.
- Mutate the dragged item's index after a move, as the code above does: the next hover compares against it.
- Read the indices into locals before `OnMove`. The move re-renders the list synchronously, so after the `await`
  this card's `Index` is already its new position, and assigning it would put the dragged item's old index back.
- Make the list itself a drop target that accepts the card type as well. Then the gaps between cards are not no-drop
  zones, and a drop in a gap still counts as a drop (`DidDrop` is true), which is what "cancel on drop outside" -
  reverting the order in `OnEnd` when `DidDrop` is false - relies on.

The demo's `Pages/Sortable/SortableCard.razor` is the complete card, measuring guard included.

## Native drags

A drop target can accept drags that start outside the application by listing one of the `BdndNativeTypes`:

| Constant | Source | DataTransfer type |
| --- | --- | --- |
| `BdndNativeTypes.File` | Dropped files | `Files` |
| `BdndNativeTypes.Url` | Dragged links | `text/uri-list` |
| `BdndNativeTypes.Text` | Selected text | `text/plain` |
| `BdndNativeTypes.Html` | Dragged HTML | `text/html` |

Native drops arrive through `OnNativeDrop`, not `OnDrop`, with a `BdndNativeDropEventArgs`: the matched `ItemType`,
the `Files`, the raw `Data` keyed by DataTransfer type, and the `Text`, `Html`, `Url` and `Urls` shortcuts. While a
native drag hovers, the target's state reports `IsOver` and `CanDrop` as true and `ItemType` as the matched native
type; `IsOverShallow` is true on the innermost hovered target that accepts the drag, as for in-app drags. Use
`TItem="object"` on a target that only takes native drags.

Native drops have no `DidDrop`: when drop targets that accept the same native type are nested, each of them
receives `OnNativeDrop`, innermost first, and an outer one cannot tell that an inner one already took the drop. Give
only one of them the native type, or keep the decision in your own state.

### Files

```razor
<BdndDropTarget TItem="object" Accept="@BdndNativeTypes.File" OnNativeDrop="HandleFiles" Context="drop">
    <div class="dropzone @(drop.IsOver ? "over" : "")">Drop files here</div>
</BdndDropTarget>

@code {
    private void HandleFiles(BdndNativeDropEventArgs e)
    {
        foreach (var file in e.Files)
            Console.WriteLine($"{file.Name} ({file.Type}, {file.Size} bytes)");
    }
}
```

The browser only exposes a dropped file's metadata - name, size and MIME type - during the drop. To read the
contents, use an `InputFile` (for instance an invisible one layered over the drop zone) or your own JS interop.

### Text, links and HTML

```razor
<BdndDropTarget TItem="object" AcceptTypes="@([BdndNativeTypes.Html, BdndNativeTypes.Text, BdndNativeTypes.Url])"
                OnNativeDrop="@(e => _last = e)">
```

The first accepted type found in the drag decides `ItemType`, in the order the target lists them; `Data` holds the
strings of every accepted type the drag carried, whichever matched. Types the target does not accept are not read:
a target accepting `BdndNativeTypes.Text` gets the `text/plain` of a selection dragged from another page, not its
`text/html`. On Blazor Server the strings travel over the circuit, and a page's HTML easily outgrows SignalR's
default 32 KB `MaximumReceiveMessageSize`, which disconnects the circuit; a target accepting `BdndNativeTypes.Html`
there needs a larger limit (`AddHubOptions(o => o.MaximumReceiveMessageSize = ...)` on
`AddInteractiveServerComponents()`).

### Editable fields inside a target

A native text, link or HTML drag over a text input, a textarea or a `contenteditable` element inside a drop target
is left to the browser, so text can still be dropped into the field. A drag carrying files always stays the
target's, as does a drag over an input that takes no text (a checkbox, a button) or a disabled or read-only field. A
drop anywhere else on a target is never handed to the browser, which would otherwise open or navigate to the dropped
file or link.

## The monitor

`IBdndMonitor` is a read-only view over a provider's drag state - the union of React DnD's `DragSourceMonitor`,
`DropTargetMonitor` and `DragLayerMonitor`. Every collected state and callback argument carries it as `Monitor`, and
`BdndDragDropManager.Monitor` exposes it outside the components.

| Member | Answers |
| --- | --- |
| `IsDragging()` | Whether a drag is in progress. |
| `IsDragging(sourceId)` | Whether the given source is the one being dragged (`null` means any). |
| `GetItemType()` | The dragged item's type, or null. |
| `GetItem()` / `GetItem<T>()` | The dragged item. |
| `GetDropResult()` / `GetDropResult<T>()` | The result recorded by the targets that handled the drop. |
| `DidDrop()` | Whether a target has handled the drop. |
| `CanDrop(handlerId)` / `IsOver(handlerId, shallow)` | A target's state by handler id. |
| `GetInitialClientOffset()` | The pointer when the drag started. |
| `GetInitialSourceClientOffset()` | The source's top-left corner when the drag started. |
| `GetClientOffset()` | The pointer now. |
| `GetDifferenceFromInitialOffset()` | How far the pointer has moved. |
| `GetSourceClientOffset()` | Where the source's top-left corner would be now. |

Offsets are `BdndXYCoord` values in client (viewport) CSS pixels; they subtract and add with `-` and `+`. The
handler-id members mirror React DnD's monitors, but the components identify themselves with private handler ids,
so application code reads a component's own answers from its collected state (`IsDragging`, `CanDrop`, `IsOver`,
`IsOverShallow`) rather than through those members; `IsDragging(null)` is the same as `IsDragging()`.
`BdndDragDropManager.GenerateHandlerId(prefix)` hands out ids unique within a manager for code of your own.

The monitor is live: inside an async `OnEnd`, `OnDrop` or `OnHover` handler, read what you need from it before the
first `await` - the drag may have ended by the time the handler resumes. `BdndDragEndContext` carries `Item`,
`DidDrop` and `DropResult` as they were when the drag ended.

`BdndInterop.GetBoundingClientRectAsync(element)` measures an element in the same client coordinates - the
measurement a sortable list compares the pointer against.

## Hosting models

Bdnd needs an interactive render mode: drag events are wired up by the components after they render interactively.
On a statically rendered page the markup appears but nothing can be dragged.

### Blazor WebAssembly

The best fit. Every `dragover` and hover change is handled without a network round trip, `ItemFactory` can cancel
the native drag, and the source's start rectangle is read synchronously while `dragstart` is dispatched, so a custom
drag layer is aligned from the very first frame.

### Blazor Server

Works the same, with three differences that come from the circuit: drag callbacks run a round trip later than the
native events; a drag `ItemFactory` cancels cannot be stopped in the browser any more, so it goes on as an inert
drag that no target accepts; and a custom drag layer starts from an estimated source position that is corrected as
soon as the measured one arrives. Prefer `OnHover` only where you need it - it is the one callback that fires
continuously; pointer moves are only sent while a `BdndDragLayer` follows them. A target accepting
`BdndNativeTypes.Html` receives the dropped page's HTML over the circuit, which may need a larger
`MaximumReceiveMessageSize` (see [text, links and HTML](#text-links-and-html)).

### Blazor Web App

Register `AddBitBdndServices()` in the server's and the client's `Program.cs`, and put the pages that drag in the
client project with `InteractiveWebAssembly` (or `InteractiveAuto`); the demo is built exactly this way.

### Blazor Hybrid

In a MAUI, WPF or Windows Forms `BlazorWebView` the JS runtime is not in-process, so the components behave as on
Blazor Server (without the network latency). Drag-and-drop support of the platform's web view applies: desktop web
views support HTML5 drags; touch input on mobile web views generally does not raise them.

### Prerendering

Prerendered markup is shown before the page is interactive; draggables and drop targets render their idle state
and start working once the page becomes interactive. No browser API is called while prerendering.

## Troubleshooting

| Symptom | Cause and fix |
| --- | --- |
| `InvalidOperationException: ... must be placed inside a BdndProvider` | Wrap the components in a `BdndProvider`. |
| `Cannot provide a value for property 'Interop'` | `AddBitBdndServices()` is missing - in a Web App, from one of the two `Program.cs` files. |
| Nothing drags at all | The page is not interactive: add a render mode (`@rendermode InteractiveWebAssembly`), or render it from an interactive parent. |
| The item drags but the target never lights up | The draggable's `Type` is not in the target's `Accept`/`AcceptTypes`, the target's `CanDropPredicate` answers false, or source and target are in different providers. |
| The no-drop cursor shows in the gaps of a list | Make the list a drop target that accepts the item type too. |
| `OnDrop` runs on the outer target as well | Nested targets all receive the drop, innermost first; check `args.Monitor.DidDrop()` in the outer one. |
| A custom preview flickers or blocks the drop | Give the preview `pointer-events: none` and `position: fixed`. |
| `left: 12,5px` in a preview's style | Format offsets with `CultureInfo.InvariantCulture`. |
| A dropped file has no contents | Browsers expose only file metadata during a drop; read the contents with `InputFile` or your own interop. |
| `OnEnd` reports `DidDrop = false` after dropping between two items | The drop landed on no target; see the gaps row above. |

## Architecture

### The shape of the system

```text
Blazor managed events          ondragstart, ondragover (only with OnHover), ondrop
  |                                     |
BdndDraggable / BdndDropTarget  ->  BdndDragDropManager  ->  StateChanged / OffsetChanged  ->  re-render
  ^                                     ^
  |  [JSInvokable] HandleDragMove, HandleDragEnd, HandleHoverChanged, HandleNativeDrop
bit-bdnd.js  (one registration per draggable and per drop target)
```

`BdndDragDropManager` owns the authoritative state - the item, its type, the source, the offsets, every target's
accept list and hover flag, the drop result. Components read it through `IBdndMonitor` when they render, and
re-render when the manager raises `StateChanged` (begin, end, hover changes) or, for drag layers, `OffsetChanged`.
Like React DnD's `collect`, a draggable or drop target re-renders on `StateChanged` only when the state it hands its
content changed - every one of them when a drag begins or ends, and on a hover change just the targets whose
`IsOver`, `IsOverShallow` or `CanDrop` it flipped - so a long list does not re-render as the pointer crosses it.
Draggables and drop targets suppress the automatic re-render `ComponentBase` performs after each event, so a
`dragover` storm does not re-render the page; the manager decides when something changed.

### The browser layer

Managed event handlers cannot touch a live `DataTransfer`, and on Blazor Server they do not run while the native
event is being dispatched. The script module covers exactly that gap:

1. At `dragstart`, it decides which source a drag belongs to (the innermost registered one, and only from a handle
   when `UseHandle` is set), tags the drag with a `DataTransfer` type carrying the source's handler id, sets
   `effectAllowed` and the drag image, and measures the source.
2. During the drag, it follows the pointer from `dragover` (Firefox reports `0,0` in `drag` events) and, while a
   drag layer listens, reports it once per animation frame.
3. It reports the end of a drag from the source element's own `dragend` listener, which still fires after the
   element was removed from the DOM - and, because some browsers never raise `dragend` for a removed source, also
   after a drop or the next mouse move.
4. It tracks hover per drop target by the nodes that raised `dragenter`, pruning nodes Blazor re-rendered away,
   so a target's hover survives its children changing.
5. It gives drop-effect feedback - `preventDefault` and `dropEffect` on `dragenter`/`dragover`, innermost target
   first - and reads native drop payloads during the `drop` event, the only moment `getData` works.

### How a drag flows

1. The browser raises `dragstart`; the module tags it; the draggable's managed handler sees its own tag, produces
   the item and calls `BeginDrag` on the manager - without notifying anyone yet, because a re-render during
   `dragstart` can abort the drag in some browsers.
2. Once dispatch is over, the manager publishes the drag: `StateChanged` re-renders the components and `OnBegin`
   runs. Every later callback publishes a pending begin first, so `OnBegin` always comes first.
3. Hover changes arrive from the module; targets that accept the type set their hover flag; `CanDrop` is decided
   from the accept list and the predicate.
4. On drop, each accepting target in the chain runs `OnDrop`, innermost first, and records the drop and its result.
5. The source's `dragend` reaches .NET; `OnEnd` runs with `DidDrop` and `DropResult`, and the manager clears the
   drag.

## Coming from React DnD

| React DnD | Bit.Bdnd |
| --- | --- |
| `<DndProvider backend={HTML5Backend}>` | `<BdndProvider>` (the HTML5 backend is built in) |
| `useDrag({ type, item })` | `<BdndDraggable Type="..." Item="...">` |
| `item: () => ...` | `ItemFactory` |
| `canDrag` | `CanDrag` / `CanDragPredicate` |
| `isDragging: monitor => ...` | `IsDraggingOverride` |
| `end(item, monitor)` | `OnEnd` with `BdndDragEndContext<TItem>` |
| `collect` of `useDrag` | the child content's `BdndDragState` |
| `useDrop({ accept })` | `<BdndDropTarget Accept="..." AcceptTypes="...">` |
| `drop(item, monitor)` returning a result | `OnDrop`, setting `args.Result` |
| `hover(item, monitor)` | `OnHover` |
| `canDrop(item, monitor)` | `CanDropPredicate` |
| `isOver({ shallow: true })` | `IsOverShallow` |
| `collect` of `useDrop` | the child content's `BdndDropState<TItem>` |
| `useDragLayer` | `<BdndDragLayer>` and `BdndDragLayerState<TItem>` |
| `getEmptyImage()` + `preview` | `UseCustomDragLayer="true"` |
| `dragPreview` / a separate drag handle ref | `UseHandle="true"` and `<BdndDragHandle>` |
| `dropEffect` option | `DropEffect` |
| `NativeTypes.FILE` / `URL` / `TEXT` / `HTML` | `BdndNativeTypes.File` / `Url` / `Text` / `Html` |
| `DragSourceMonitor` / `DropTargetMonitor` / `DragLayerMonitor` | `IBdndMonitor` |
| `XYCoord` | `BdndXYCoord` |

What is different: there are no connector refs to attach - the component's host element is the source or target;
the item type is matched by string, never by .NET type; and native drops have their own callback, `OnNativeDrop`,
instead of arriving in `drop`.

## Component reference

### BdndProvider

| Parameter | Type | Default | Description |
| --- | --- | --- | --- |
| `Manager` | `BdndDragDropManager?` | `null` | An externally-owned manager. When omitted, one is created for the subtree. Cannot change once set. |
| `ChildContent` | `RenderFragment?` | | The drag-and-drop content. |

### BdndDraggable&lt;TItem&gt;

| Parameter | Type | Default | Description |
| --- | --- | --- | --- |
| `Type` | `string` | (required) | The item type matched against drop targets' `Accept` lists. |
| `Item` | `TItem?` | | The item payload. Ignored when `ItemFactory` is supplied. |
| `ItemFactory` | `Func<TItem?>?` | | Produces the item at drag start; returning `null` cancels the drag. |
| `CanDrag` | `bool` | `true` | Whether dragging is allowed. |
| `CanDragPredicate` | `Func<bool>?` | | Overrides `CanDrag`; evaluated at render time and at drag start. |
| `IsDraggingOverride` | `Func<IBdndMonitor, bool>?` | | A custom `IsDragging`, e.g. by item id. |
| `UseCustomDragLayer` | `bool` | `false` | Hide the native drag image (pair with a `BdndDragLayer`). |
| `UseHandle` | `bool` | `false` | Only start drags from a `BdndDragHandle` inside. |
| `DropEffect` | `string` | `"move"` | `dataTransfer.effectAllowed`: `move`, `copy`, `link`, `copyMove`, `copyLink`, `linkMove` or `all`. |
| `OnBegin` | `EventCallback<TItem?>` | | The drag began. |
| `OnEnd` | `EventCallback<BdndDragEndContext<TItem>>` | | The drag ended (drop or cancel), even if the draggable was removed meanwhile. |
| `Tag` | `string` | `"div"` | The host element's tag. |
| `ChildContent` | `RenderFragment<BdndDragState>?` | | The content, given the collected state. |
| (any other attribute) | | | Applied to the host element. |

### BdndDropTarget&lt;TItem&gt;

| Parameter | Type | Default | Description |
| --- | --- | --- | --- |
| `Accept` | `string?` | | One accepted item type. |
| `AcceptTypes` | `IEnumerable<string>?` | | More accepted item types, combined with `Accept`. |
| `OnDrop` | `EventCallback<BdndDropEventArgs<TItem>>` | | A compatible in-app item was dropped; set `args.Result` to publish a result. Nested targets run innermost first. |
| `OnHover` | `EventCallback<BdndDropEventArgs<TItem>>` | | An in-app item of an accepted type is hovering (called repeatedly, whatever `CanDropPredicate` answers). |
| `OnNativeDrop` | `EventCallback<BdndNativeDropEventArgs>` | | A native drag of an accepted `BdndNativeTypes` type was dropped. |
| `CanDropPredicate` | `Func<TItem?, IBdndMonitor, bool>?` | | Whether this target accepts the current item, on top of the type check. |
| `Tag` | `string` | `"div"` | The host element's tag. |
| `ChildContent` | `RenderFragment<BdndDropState<TItem>>?` | | The content, given the collected state. |
| (any other attribute) | | | Applied to the host element. |

### BdndDragLayer&lt;TItem&gt;

| Parameter | Type | Default | Description |
| --- | --- | --- | --- |
| `ChildContent` | `RenderFragment<BdndDragLayerState<TItem>>?` | | The preview, given the live drag state and offsets. |

### BdndDragHandle

| Parameter | Type | Default | Description |
| --- | --- | --- | --- |
| `ChildContent` | `RenderFragment?` | | The handle's content. |
| `Style` | `string?` | | The handle's inline style; `cursor: move` is prepended while the draggable can drag. |
| (any other attribute) | | | Applied to the handle element. |

## License

Bit.Bdnd is part of the [bit platform](https://github.com/bitfoundation/bitplatform) and is licensed under the
[MIT License](https://github.com/bitfoundation/bitplatform/blob/develop/LICENSE).
