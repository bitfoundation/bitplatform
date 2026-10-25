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
- **Native OS drags**: accept dropped files - streaming their contents, expanding folders, limited to the types and
  count you allow - selected text, links and HTML with `BdndNativeTypes`, and carry data of your own out of the page.
- **Touch, pen and pointer drags**: touch and pen drag every draggable through pointer events - most touch browsers
  raise no native drag events - with a press-and-hold that leaves swipes to scroll, a preview that follows the
  finger, Escape and interruption handling, and auto-scrolling; `PointerDrag="All"` drags with the mouse the same way.
- **Keyboard dragging and screen readers**: every draggable can be picked up with Space or Enter, moved between the
  targets that can take it with the arrow keys and dropped or cancelled, with live-region announcements and
  instructions screen readers read out - translatable through `BdndAnnouncements`.
- **Insertion positions**: a target with `Edges` reports the edge a drag is closest to - before or after the item
  it shows - for drop indicators, and `BdndList` turns it into list moves, within a list or across lists, in
  vertical lists, wrapping grids (where the keyboard moves by row), trees and virtualized lists.
- **Watching every drag**: `OnEnter`/`OnLeave` on targets, `OnDragStart`/`OnDragEnd` on the provider for undo
  histories and analytics, and `Disabled` to close a target.
- **Styling hooks**: hosts carry `data-bit-bdnd-dragging`, `data-bit-bdnd-can-drop`, `data-bit-bdnd-over` and
  `data-bit-bdnd-over-shallow`, so a stylesheet can follow a drag without the content rendering classes for it.
- **Isolated contexts** through nestable providers, and a read-only `IBdndMonitor` over the live drag state.
- **Robust** across re-renders: a dragged item may be re-parented, re-keyed or removed mid-drag and its `OnEnd` still
  runs; hover survives the target's children being re-rendered.
- Works on **Blazor WebAssembly, Blazor Server, Blazor Web App and Blazor Hybrid**, on .NET 8, 9 and 10 - driven by
  the end-to-end suite in Chromium, Firefox and WebKit, over a Server circuit and in a WinForms `BlazorWebView`.

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
- A mouse, a trackpad, a pen, a touch screen or the keyboard: the mouse drags through the browser's HTML5 drag and
  drop, touch and pen through pointer events (any browser with pointer events), the keyboard through key handling.

## The demo

The demo site, [bdnd.bitplatform.dev](https://bdnd.bitplatform.dev), ports React DnD's own examples, so a React DnD
user can open the same example on both sides: the chessboard tutorial, dustbins (single and multiple targets, copy
or move, inside an iframe, a stress test), drag-around (naive and with a custom drag layer), nesting (drag sources
and drop targets), sortable lists (simple, cancel on drop outside, a hundred-item stress test), handles and previews,
drop effects, and native files and HTML - and pages of its own: drop indicators, a sortable grid, a virtualized
list, several items at once, a page builder, a folder tree, touch and pointer drags, a keyboard kanban board, and a
Blazor Server page that drags over a SignalR circuit. Every page can show its own source.

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
| The values | `BdndXYCoord`, `BdndNativeTypes`, `BdndNativeFile`, `BdndDomRect`, `BdndEdge`, `BdndEdges`, `BdndAxis`, `BdndDragInput`, `BdndPointerDrag`, `BdndAnnouncements`. |
| The helpers | `BdndList`: insertion indexes and moves within and across lists. |
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
- [Touch and pointer drags](#touch-and-pointer-drags)
- [Keyboard and screen readers](#keyboard-and-screen-readers)
- [Styling](#styling)
- [Hosting models](#hosting-models)
- [Troubleshooting](#troubleshooting)
- [Architecture](#architecture)
- [Coming from React DnD](#coming-from-react-dnd)
- [Component reference](#component-reference)
- [Upgrading](#upgrading)

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

### Watching every drag

`OnDragStart` and `OnDragEnd` on the provider run for every drag of its context, whichever source and input made
it, with a `BdndDragEventArgs`: the `Item`, its `ItemType`, the `Input` and - on the end - `DidDrop` and the
`DropResult`. They are where an undo history records the moves, analytics count them, or a page dims what cannot
take the item:

```razor
<BdndProvider OnDragStart="@(e => _dragging = e.ItemType)" OnDragEnd="RecordMove">...</BdndProvider>

@code {
    private string? _dragging;

    private void RecordMove(BdndDragEventArgs e)
    {
        _dragging = null;
        if (e.DidDrop && e.DropResult is MoveResult move) _undo.Push(move); // the result a target's OnDrop published
    }
}
```

`OnDragStart` runs once the drag is published and its source's `OnBegin` started; `OnDragEnd` once the drag state is
cleared and the source's `OnEnd` began - so it sees the monitor no longer dragging. Both run a moment after that,
outside the manager's update, so what they render or throw lands on its own. The manager raises the same moments
synchronously as the `DragStarted` and `DragEnded` events, for code outside the provider (a manager passed with
`Manager`); a handler of those that throws does not stop the drag from beginning or ending, and its exception reaches
the renderer through the provider.

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
it was in - and for a mouse drag released outside the browser window, as a cancel: when the browser reports it, or,
where it reports nothing, at the pointer's first move back over the page. The drag state is cleared right after `OnEnd`'s synchronous part, so an async handler does not keep every
target highlighted.

```razor
<BdndDraggable TItem="Box" Type="box" Item="box" OnEnd="@(e => { if (e.DidDrop) _palette.Remove(box); })">
```

### When a callback throws

An exception thrown by a callback reaches the renderer the way an exception of an ordinary event handler does: an
`ErrorBoundary` around the component shows it, and otherwise the app's error UI does (Blazor WebAssembly) or the
circuit ends (Blazor Server). That holds for the callbacks the browser module runs from JavaScript - `OnEnd`,
`OnNativeDrop`, and every callback of a keyboard drag - which would otherwise only reject a promise nobody reads.
The drag still ends: the drag state is cleared whether or not `OnEnd`, `OnBegin` or an `OnDrop` throws.

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

### Dragging several items

A drag carries one item, and that item can be a collection. Let `ItemFactory` hand over the selection when the
grabbed card is part of it, and the card alone otherwise; `IsDraggingOverride` marks every card of the selection as
dragged, and a target receives the whole list:

```razor
@foreach (var card in _cards)
{
    <BdndDraggable TItem="IReadOnlyList<Card>" Type="cards" Label="@card.Title"
                   ItemFactory="@(() => _selected.Contains(card) ? _selected.ToList() : [card])"
                   IsDraggingOverride="@(m => m.GetItem<IReadOnlyList<Card>>()?.Contains(card) == true)" Context="drag">
        <div class="card @(_selected.Contains(card) ? "selected" : "") @(drag.IsDragging ? "dragging" : "")"
             @onclick="() => Toggle(card)">@card.Title</div>
    </BdndDraggable>
}

<BdndDropTarget TItem="IReadOnlyList<Card>" Accept="cards" Label="Archive" OnDrop="@(e => Archive(e.Item!))">
    <div class="bin">Archive</div>
</BdndDropTarget>

@code {
    private readonly HashSet<Card> _selected = [];

    private void Toggle(Card card)
    {
        if (_selected.Add(card) is false) _selected.Remove(card);
    }

    private void Archive(IReadOnlyList<Card> cards)
    {
        foreach (var card in cards) _cards.Remove(card);
        _selected.ExceptWith(cards);
    }
}
```

A `BdndDragLayer<IReadOnlyList<Card>>` can draw "3 cards" as the preview. The keyboard drags the selection the same
way (Space on a selected card picks them all up). The demo's
[dragging several items](https://bdnd.bitplatform.dev/sortable/multi-select) page is the complete example, with a
checkbox per card so the selection can be made with the keyboard too.

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
| `ClosestEdge` | The edge of the target the drag is closest to while it is over it, of the target's `Edges`; null otherwise. |
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

If another drag begins before those handlers complete, the first drag ends at once without waiting for them: its
`OnEnd` and the provider's `OnDragEnd` see `DidDrop` true and the result published so far - not one a handler sets
after that.

### Hovering

`OnHover` runs repeatedly while an item of an accepted type is over the target, with the same arguments as
`OnDrop` - like React DnD's `hover`, also when `CanDropPredicate` refuses the item. It is what sortable lists
reorder from. The `dragover` listener behind it is only attached when `OnHover` has a handler,
because `dragover` fires continuously and, on Blazor Server, each one is a message over the circuit.

### Entering and leaving

`OnEnter` runs when an in-app drag of an accepted type moves over the target (or a target nested in it) - its
`IsOver` turns true - and `OnLeave` when it moves off while the drag goes on. A drag that ends over the target does
not leave it: its `OnDrop` (or the source's `OnEnd`, for a cancel) tells the end. Both run for mouse, touch and
keyboard drags alike; for native drags (files, text), follow `IsOver` in the state.

Nested targets are entered outside in and left inside out. Between two neighbouring targets, a native mouse drag may
enter the next one before it leaves the previous one - the browser reports them in that order - so code that tracks
the hovered targets should keep a set (add on enter, remove on leave) rather than a single "current" target.

### Disabling a target

`Disabled="true"` takes a target out of every drag: it accepts nothing - no hover, no `CanDrop`, no drop, no native
drop - and the keyboard skips it. It is the switch to flip for a target that is temporarily closed (a full list, a
locked column); `CanDropPredicate` is for decisions that depend on the item.

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

### Trees

A tree is nested targets with edges: each node's row is a target with `Edges="BdndEdges.Vertical"` (a node dropped
on its top half goes before it, on its bottom half after it) and the list of its children another target (a node
dropped there becomes a child). A `CanDropPredicate` that refuses a node's own subtree keeps a node from being dropped
into itself; the keyboard skips those targets too, and only the innermost hovered row shows an insertion line. The
demo's [tree](https://bdnd.bitplatform.dev/nesting/tree) page is the complete example.

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

To keep the preview on one axis, snap it to a grid or hold it inside an area, draw it at an offset the helpers of
`BdndXYCoord` work out: `layer.SourceClientOffsetOn(BdndAxis.Vertical)` keeps it to the list's axis,
`offset.SnapTo(40, 40, origin)` moves it cell by cell, and `offset.Clamp(min, max)` keeps its corner inside an area.
They only move the preview: where the drop lands is still decided by the pointer. A pointer drag's own copy keeps to
an axis with `PreviewAxis` on the draggable.

A layer follows every drag of its provider. Give it `Accept` (or `AcceptTypes`) to draw the drags of some item types
only: a drag of another type is no drag to it (`IsDragging` false), and - what matters on Blazor Server, where each
pointer move is a message over the circuit - the sources of other types do not send their moves at all. With no
layer following a drag, its moves stay in the browser.

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

### Drop indicators and insertion positions

The recipe above is React DnD's, and it measures the hovered card over interop on every hover. For most lists there
is a simpler way, Pragmatic drag and drop's: let each item's target tell *where* on it the drag is. A target with
`Edges` reports the edge the pointer is closest to while a drag is over it - `ClosestEdge` in its state, and
`data-bit-bdnd-edge` on its host for a stylesheet to draw a line from - and its drop carries the edge too.
`BdndList` does the index arithmetic:

```razor
@foreach (var (card, index) in _cards.Select((c, i) => (c, i)))
{
    <BdndDropTarget TItem="Card" Accept="card" Edges="BdndEdges.Vertical" Label="@card.Title" class="slot" @key="card"
                    OnDrop="@(e => _ = BdndList.Move(_cards, _cards.IndexOf(e.Item!), _cards, BdndList.InsertionIndex(index, e.ClosestEdge)))">
        <BdndDraggable TItem="Card" Type="card" Item="card" Label="@card.Title" class="card">@card.Title</BdndDraggable>
    </BdndDropTarget>
}
```

```css
.slot { position: relative; }
.slot[data-bit-bdnd-edge]::after { content: ""; position: absolute; left: 0; right: 0; height: 2px; background: blue; }
.slot[data-bit-bdnd-edge="top"]::after { top: -5px; }
.slot[data-bit-bdnd-edge="bottom"]::after { bottom: -5px; }
```

| `Edges` | Reports | For |
| --- | --- | --- |
| `BdndEdges.Vertical` | `Top`, `Bottom` | the items of a vertical list |
| `BdndEdges.Horizontal` | `Left`, `Right` | the items of a horizontal list |
| `BdndEdges.All` | the nearest of the four | grid tiles |

| `BdndList` | Does |
| --- | --- |
| `InsertionIndex(targetIndex, edge)` | Where a drop on the item at `targetIndex` goes: that index (top, left, no edge) or the next (bottom, right). |
| `ReorderIndex(startIndex, targetIndex, edge)` | Where an item of the same list ends up, accounting for it leaving its place. |
| `Move(source, sourceIndex, destination, insertionIndex)` | Moves an item to an insertion index of a list - another, or the same one; false (and no change) when it would stay where it is. |
| `Reorder(list, startIndex, finishIndex)` | Moves an item within a list. |

The order is changed on drop, not on hover, so nothing re-renders as the pointer crosses the list, and a drop
elsewhere needs no undo. Edges are tracked in the browser and sent to .NET only when they change, for native,
touch and keyboard drags alike, and an edge change re-renders that one target and nothing else. The edge belongs to
the innermost hovered target (`IsOverShallow`) - a tree node around a hovered child shows no second indicator and
gets no edge in its `OnDrop` - and only where the drop would be taken (`CanDrop`): a full list shows no line.

Edges are where they are on screen. In a list laid out against its order - right to left (`dir="rtl"`),
`flex-direction: row-reverse` or `column-reverse` - the left (or bottom) edge is *after* the item: pass
`reversed: true` to `BdndList.InsertionIndex` and `ReorderIndex` there. Keyboard drags take the direction into
account by themselves: in a right-to-left layout the left arrow goes to the next place, and the right edge of a
horizontal item is the place before it. A keyboard drag visits every place - before the first card, between two
neighbouring cards, after the last - once, skips the place the item is already in, and places a list target after
its cards ("the end of Backlog"). Two items count as neighbours only when no other drop target lies between them, so
an item that refuses the drag still separates two places. Make the list itself a drop target too (taking drops below
the cards, or in an empty list) and check `Monitor.DidDrop()` there, as the demo's
[drop indicators](https://bdnd.bitplatform.dev/sortable/indicators) page does with two lists.

### Grids and wrapping lists

A grid is a horizontal list that wraps: give each item `Edges="BdndEdges.Horizontal"` and the same `BdndList` calls
place a drop left or right of the item it lands on, whatever row it is in. With the keyboard, Left and Right step
through the gaps in the list's order, and Up and Down go to the row below or above - to the gap of the same list
closest across, so an item moved down lands in its own column - rather than one item on. The gap where a row breaks
is offered at the start of the next row, where its line shows. Where no row of the list lies that way (the last row)
they go on past the row in the list's order - to another list, or nowhere; in a list that does not wrap (a column of
cards, a single row) they are the next and previous place as before. The
demo's [grid](https://bdnd.bitplatform.dev/sortable/grid) page is the complete example.

### Virtualized lists

Draggables and drop targets work inside `Virtualize<TItem>` as anywhere else: each rendered row registers itself, and
a row scrolled out of the rendered range unregisters - except the one being dragged, whose registration stays until
its drag ends, so `OnEnd` still runs. Pointer drags scroll the list's container near its edges (`AutoScroll`) and the
rows rendered as it scrolls take the drag at once; mouse drags scroll it as the browser does. The keyboard walks the
list a row at a time however fast the keys come: each move scrolls the place it lands on into view, and a key that
would leave the list - or go nowhere - while the list has rows it has not rendered that way (the spacer `Virtualize`
leaves for them) scrolls them into view and waits for them, up to a second and a half, before going on. `End` goes
to the last rendered row. `@key` the rows by item, so a row that comes back is the same component.

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
<BdndDropTarget TItem="object" Accept="@BdndNativeTypes.File" AcceptFileTypes="@(["image/*", "application/pdf"])"
                MaxFiles="20" OnNativeDrop="HandleFiles" Context="drop">
    <div class="dropzone @(drop.IsOver ? "over" : "")">Drop images or PDFs here</div>
</BdndDropTarget>

@code {
    private async Task HandleFiles(BdndNativeDropEventArgs e)
    {
        foreach (var file in e.Files)
        {
            await using var stream = await file.OpenReadStreamAsync(maxAllowedSize: 10_000_000);
            // the name comes from the user's system: never use it as a path as it is
            await _storage.SaveAsync(Guid.NewGuid(), Path.GetFileName(file.Name), file.Type, stream);
        }
        if (e.TotalFileCount > e.Files.Count) _message = $"{e.TotalFileCount - e.Files.Count} files were left out.";
    }
}
```

Each `BdndNativeFile` has its `Name`, `Size`, `Type` (the MIME type the browser reports, empty when it does not
know), `LastModified` and, from a dropped folder, its `RelativePath`. `OpenReadStreamAsync(maxAllowedSize)` streams
its contents from the browser - over the circuit on Blazor Server - like `IBrowserFile` does, and throws an
`IOException` for a file larger than `maxAllowedSize` (512,000 bytes by default) before reading anything. The browser
hands a drop's files to the page during the drop only; a target keeps those of its last drop until its next one, or
until it is disposed, so read them in `OnNativeDrop` or soon after (`CanRead` tells whether a file still can be).

| Parameter | Does |
| --- | --- |
| `AcceptFileTypes` | Limits a file drop to types, as `<input type="file" accept>` lists them: MIME types (`application/pdf`), MIME groups (`image/*`), extensions (`.png`), or any (`*/*`). On drop, files of other types - and folders - are left out of `Files`. While files hover, browsers tell their MIME types but not their names: a target listing MIME types only refuses a drag of none of them right away (no-drop cursor, no hover state); one listing an extension takes any file drag and sorts the files out at the drop. |
| `ExpandDirectories` | A dropped folder becomes the files inside it, recursively, each with its `RelativePath` (`photos/2024/beach.jpg`); off, a folder arrives as one entry with `IsDirectory` set and nothing to read. The walk runs after the drop (the target's hover ends at once), skips entries it cannot read, and stops at `MaxFiles` accepted files or 10,000 entries; `FileListIncomplete` says when it did not list everything. A newer drop on the target, or its disposal, discards a walk still running. |
| `MaxFiles` | The most files a drop hands over: the first ones, in the order the browser lists them. |

`TotalFileCount` tells how many files the drop carried before `AcceptFileTypes` and `MaxFiles` were applied, so the
page can say that some were left out. The descriptions of the files reach .NET fifty at a time, so a drop of many
files - a folder of thousands - never makes a message larger than a Blazor Server circuit takes. `CanRead` turns
false once the target took another drop, or was disposed; `OpenReadStreamAsync` then throws an
`InvalidOperationException`. Some browsers do not tell folders from files: such a folder reads as an empty or
failing stream.

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

### Dragging data out of the page

A draggable's in-app drag can carry data for destinations outside the page as well: `NativeData` puts strings on
the drag's `DataTransfer` at `dragstart`, by type. A text editor or another application reads `text/plain`, a
browser's address bar or tab strip `text/uri-list`, another window of your own app a type of your own:

```razor
<BdndDraggable TItem="Card" Type="card" Item="card"
               NativeData="@(new Dictionary<string, string> { ["text/plain"] = card.Title, ["text/uri-list"] = card.Url })">
```

Inside the page the drag stays the in-app drag it is: the page's drop targets treat it by its `Type`, and a target
taking `BdndNativeTypes.Text` does not take it for a native text drag. A text field on the page outside any drop
target takes the `text/plain` as any field takes dropped text. Only native (mouse) drags leave the page; pointer and
keyboard drags carry no native data. In another window running Bdnd the drag is marked as an in-app drag of another
document, which Bdnd targets there refuse: read it there with a handler of your own (`ondrop` and
`DataTransfer.getData`).

### Dropped content is untrusted

Everything a native drop carries comes from outside the application: another site's HTML, any file of any size
whose name and type the user's system made up. Treat it as untrusted input.

- `Html` is markup of another origin: never render it as `MarkupString` without sanitizing it first (with an HTML
  sanitizer that allows only the elements and attributes you need); show it as text otherwise, as the demo does.
- `Url`/`Urls` may be any scheme - `javascript:` included: check the scheme before you link to one.
- A file's `Type` and `Name` are hints, not facts: check the contents (and `Size` against `maxAllowedSize`) before
  you store, parse or show a file.
- On Blazor Server, file contents and dropped strings travel over the circuit: keep `maxAllowedSize` and the
  accepted types as tight as the feature allows, and see the message size note above.

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
| `GetDragInput()` | How the drag is being made - `BdndDragInput.Native`, `Pointer` or `Keyboard` - or null when nothing is dragged. |
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

## Touch and pointer drags

Most touch browsers never turn a touch into an HTML5 drag, so Bdnd drags touch and pen through pointer events of its
own - React DnD's touch backend, built in. The drag is the manager's like any other: the same `IsDragging`,
`IsOver` and `CanDrop`, the same `OnHover`, the same `OnDrop` chain innermost first and the same `OnEnd`.
`GetDragInput()` answers `BdndDragInput.Pointer` for it.

### Starting a drag

A touch has to rest on a source for `TouchDelay` milliseconds (250 by default) before the drag starts, so that a
swipe across a list of draggables still scrolls the page: moving more than `DragThreshold` pixels before then gives
the press up. A `BdndDragHandle` starts at once - it does not scroll, it carries `touch-action: none` - as do a pen
on a handle and the mouse (`PointerDrag="All"`), which start once they have moved `DragThreshold` pixels (5 by
default), so a click stays a click. A press on a form field inside a source (an input, a text area, a select, an
editable element) or inside an element marked `data-bit-bdnd-no-drag` never starts one - and a native drag never
starts from either: while such a press lasts the source is not draggable, so a field selects its text as any field
does (Firefox selects none inside a draggable element otherwise) and a button only clicks:

```razor
<BdndDraggable TItem="Card" Type="card" Item="card">
    <span>@card.Title</span>
    <button data-bit-bdnd-no-drag @onclick="() => Open(card)">Open</button>
</BdndDraggable>
```

While a touch press is pending or dragging, the source selects no text, shows no callout and opens no context
menu, and the platform's own long-press drag (iOS, Android and Windows browsers start native drags of draggable
elements on a long press) is kept from starting.

### During the drag

A copy of the source follows the pointer: it is inserted next to the source - so the stylesheet rules that draw the
source draw it too - and shown in the top layer (as a manual popover, where the browser has popovers), so no
ancestor's `overflow`, `transform` or stacking context clips or covers it. It takes no pointer events, focus or place
in the accessibility tree, plays no media and loads no frames a second time, and it carries `data-bit-bdnd-preview`
for styling it (its position is an inline `transform`, so tilt it with the `rotate` property):

```css
.card[data-bit-bdnd-preview] { rotate: 2deg; box-shadow: 0 8px 24px rgb(0 0 0 / .2); }
```

`PreviewAxis="BdndAxis.Vertical"` keeps the copy on the list's axis - it moves up and down with the pointer and stays
in its column - and `Horizontal` along a row; the pointer still decides where the drop lands. If a re-render removes
the source's container during the drag (a virtualized row scrolled away, a list re-rendered under it), the copy goes
on from the body.

With `UseCustomDragLayer="true"` there is no copy, and a `BdndDragLayer` draws the preview from the offsets as it
does for a native drag. The targets under the pointer are found the way the browser finds them for a native drag -
the elements under the pointer and the targets around them - and report hover changes, run `OnHover` (coalesced to
what .NET keeps up with, so a slow handler or a Blazor Server round trip is not flooded) and take the drop.

`AutoScroll` (on by default) scrolls the innermost scroll container under the pointer that can still scroll towards
the edge the pointer is within 48 pixels of - or the page - faster the nearer the edge it is.

### Ending it

Releasing drops on the targets under the pointer - after one last hover report at the release point, so a
sortable list's last move is never lost. Escape, a `pointercancel`, a second finger, the window losing the focus or
the page being hidden cancel; a mouse release outside the window is noticed on the next move back in, as a cancel.
The click a release raises is swallowed - also when the drag was cancelled before the button was released - so
dropping on a button does not press it. The drag goes on when a re-render removes the element the finger pressed:
its events are followed on that element too.

The end report returns to the browser as soon as the drag state is cleared - right after `OnEnd`'s synchronous part
- so an `OnEnd` that saves something does not hold up the next drag. An async `OnDrop`, whose result `OnEnd`
waits for, does; keep slow work out of it.

### Choosing which pointers

| `PointerDrag` | Mouse | Touch and pen |
| --- | --- | --- |
| `Touch` (default) | the browser's native drag and drop | pointer drags |
| `All` | pointer drags (the source is not natively draggable) | pointer drags |
| `None` | the browser's native drag and drop | whatever the platform's browser does with them |

`All` gives the same drag on every browser, previews and auto-scrolling included, and keeps a drag inside the page:
a pointer drag can neither leave the browser for another window or application nor take native data (files, text)
along. Native drags of files, text and links into the page are native drags whatever `PointerDrag` says.

## Keyboard and screen readers

Dragging must never be the only way to do something: WCAG 2.2 asks for every function to work from the keyboard
(2.1.1) and for every dragging movement to have a single-pointer alternative (2.5.7). Bdnd covers the first for
every draggable, and the demo's kanban board shows the usual pattern for the second.

### Dragging with the keyboard

A draggable that can drag is focusable (`tabindex="0"`, unless it was given a `tabindex` of its own), or - with
`UseHandle` - its `BdndDragHandle` is, as a `role="button"`. With the focus on it:

| Key | Does |
| --- | --- |
| <kbd>Space</kbd> or <kbd>Enter</kbd> | Picks the source up: the drag begins, `OnBegin` runs. |
| <kbd>↓</kbd> <kbd>→</kbd> / <kbd>↑</kbd> <kbd>←</kbd> | Moves the drag to the next / previous drop target that can take the item, in document order. In a layout that wraps (a grid), <kbd>↓</kbd> and <kbd>↑</kbd> go a row down or up instead - see [grids and wrapping lists](#grids-and-wrapping-lists). |
| <kbd>Home</kbd> / <kbd>End</kbd> | Moves it to the first / last of them. |
| (nothing further) | The drag stays where it is and says so - the arrows do not wrap around. In a right-to-left layout <kbd>←</kbd> and <kbd>→</kbd> swap. |
| <kbd>Space</kbd> or <kbd>Enter</kbd> | Drops it on the target it is over - or, over none, cancels. |
| <kbd>Escape</kbd> | Cancels. Moving the focus away (<kbd>Tab</kbd>, a click) cancels too. |

A keyboard drag is the manager's like any other. The targets it moves onto report `IsOver` (with their enclosing
targets) and `IsOverShallow`, their `OnHover` runs with the pointer at the middle of the target, the drop runs the
`OnDrop` chain innermost first, and the source's `OnEnd` gets `DidDrop` and the drop result. Only targets the
item can be dropped on are visited - their type matches and `CanDropPredicate` agrees - and only the ones on
screen (not hidden, not inside an `inert` region such as the page behind a modal dialog). The item's own targets
are skipped: those inside the dragged source (an item is never offered itself, nor a tree node its own subtree),
and one around it that holds no other target (a target wrapping each draggable, or a column holding just this
card) - the item is already there. A target around it that holds others, such as the list it is in or an
enclosing tree node, is still offered: dropping there is a move of its own.

A target with `Edges` is two places to the keyboard - before it and after it (top and bottom, or left and right) -
and the drop carries the edge as `ClosestEdge`; the place between two neighbouring items of one list is visited
once, and the place the item is already in not at all. Because `OnHover` runs as the drag moves, a sortable list
built on hover - the [sortable lists](#sortable-lists) recipe - reorders under a keyboard drag as it does under the
mouse, one place per key: once a hover has moved the dragged item, the next arrow goes on from where the item now
is. `GetDragInput()` tells a keyboard drag from a
native one, should a component want to (a drag layer, for one, may skip its preview).

The focus stays on the dragged source throughout - Bdnd puts it back when a re-render moves the element - and
when a hover or the drop re-renders the item as another draggable (a card moved to another column is a new
component there), the draggable that shows the same `Item` takes the focus over.

`AllowKeyboard="false"` takes a draggable out of all of this: it is not made focusable and ignores the keys; on the
`BdndProvider` it takes every draggable of the provider out (the manager's `AllowKeyboard`). A
host that is interactive by itself - a `Tag` of `a`, `button`, `input`, `select`, `textarea` or `summary` - keeps
its own Space and Enter and is not picked up from them; give it a `BdndDragHandle` to drag it with the keyboard. A
handle that wraps a button of its own works too: the keyboard picks the source up from that button (give the
handle `tabindex="-1"` and `role="presentation"` so it does not add a second tab stop and a nested button).

If a callback of the drag throws - a `CanDropPredicate` as the arrows look for targets, an `OnHover`, an `OnDrop` -
the keyboard drag ends as a cancel, and the exception reaches the renderer (see
[when a callback throws](#when-a-callback-throws)). A pointer press during a keyboard drag cancels it; a native
drag that begins before .NET has handled that cancel ends the keyboard drag first (its `OnEnd` runs with
`DidDrop` false), so every `OnBegin` is followed by its `OnEnd`.

### What screen readers hear

Every keyboard-draggable source is described (`aria-describedby`) by instructions, and a live region announces the
pickup, each move, the drop and the cancel. A source's host keeps its own role (an `li` stays a list item, an
`article` an article): a screen reader reads its content when it is focused, and the instructions after it.

> Picked up Write the docs. 3 drop targets are available. · Write the docs is over Doing. · Write the docs was dropped on Doing.

Name what is announced with `Label`: on a draggable it names the item (and labels its drag handle, unless the
handle has an `aria-label` of its own); on a drop target it names the target. Without one, a source is named by
its `aria-label` or its text, and a target by its `aria-label` or `title`, else by its place ("drop target 2 of
3").

Every text is in `BdndAnnouncements`; give the provider your own to translate or reword them - each announcement
is a function of a `BdndAnnouncement` (the source and target names, the target's place and count, the item), and
one answering `null` is not read out:

```razor
<BdndProvider Announcements="_german">...</BdndProvider>

@code {
    private readonly BdndAnnouncements _german = new()
    {
        Instructions = "Leertaste oder Eingabe zum Aufnehmen; Pfeiltasten zum Bewegen; Leertaste zum Ablegen; Escape zum Abbrechen.",
        DragStarted = a => $"{a.SourceLabel} aufgenommen. {a.TargetCount} Ablageziele.",
        DraggedOver = a => a.TargetLabel is null ? $"{a.SourceLabel} ist über keinem Ablageziel." : $"{a.SourceLabel} ist über {a.TargetLabel}.",
        Dropped = a => $"{a.SourceLabel} auf {a.TargetLabel} abgelegt.",
        Cancelled = a => $"Ziehen von {a.SourceLabel} abgebrochen.",
    };
}
```

The live region and the instructions are elements the module adds to the end of the `body` - one region per
document, one hidden element per distinct instruction text - so a provider's own markup is unchanged. Mouse drags
are not announced: a screen reader user drags with the keyboard.

### A non-drag alternative

The keyboard is not a single pointer. Give the operations a drag performs a control that does the same without
dragging - "Move to Done" buttons, a menu, a select - calling the same method your `OnDrop` calls. The
[keyboard demo](https://bdnd.bitplatform.dev/accessibility/keyboard) moves its kanban cards with arrow buttons and
with drags through one `Move` method.

Bdnd deliberately has no "drop programmatically" API: the operation a drop performs is the application's own method,
and calling it directly from a button is simpler, and easier to read, than faking a drag through the library. Keep
the drop handler a one-line call of that method and both paths stay the same.

## Styling

Draggables and drop targets put the drag state on their host elements, so a stylesheet can follow a drag without
the content rendering classes from the collected state:

| Attribute | On | Present while |
| --- | --- | --- |
| `data-bit-bdnd-dragging` | a draggable's host | it is the one being dragged (`IsDragging`) |
| `data-bit-bdnd-can-drop` | a drop target's host | a drag it can take is in progress (`CanDrop`) |
| `data-bit-bdnd-over` | a drop target's host | the drag is over it or over a target nested in it (`IsOver`) |
| `data-bit-bdnd-over-shallow` | a drop target's host | the drag is over it and over no target nested in it (`IsOverShallow`) |
| `data-bit-bdnd-edge` | a drop target's host with `Edges` | the drag is over it; the value is the closest edge: `top`, `bottom`, `left` or `right` |
| `data-bit-bdnd-preview` | the copy a pointer drag moves | always |
| `data-bit-bdnd-handle` | a drag handle | always |

```css
.column[data-bit-bdnd-can-drop] { border: 2px dashed var(--accent); }
.column[data-bit-bdnd-over][data-bit-bdnd-can-drop] { background: var(--accent-wash); }
.card[data-bit-bdnd-dragging] { opacity: .4; }

@media (forced-colors: active) {
    .column[data-bit-bdnd-over][data-bit-bdnd-can-drop] { outline: 3px solid Highlight; }
}
```

Bdnd ships no stylesheet and sets no styles of its own beyond `cursor: move` on an active drag handle: what a
drag looks like is the application's. A keyboard drag shows the browser's focus ring on the source; keep
`:focus-visible` styles visible on draggables and handles.

## Hosting models

Bdnd needs an interactive render mode: drag events are wired up by the components after they render interactively.
On a statically rendered page the markup appears but nothing can be dragged.

### Blazor WebAssembly

The best fit. Every `dragover` and hover change is handled without a network round trip, `ItemFactory` can cancel
the native drag, and the source's start rectangle is read synchronously while `dragstart` is dispatched, so a custom
drag layer is aligned from the very first frame.

### Blazor Server

The demo serves [/hosting/server-check](https://bdnd.bitplatform.dev/hosting/server-check) over a circuit
(`InteractiveServer`), and the end-to-end suite drags there: native mouse drags, keyboard drags, touch and pointer
drags, a drag layer, insertion edges and file contents streamed to the server all work over SignalR. It works the
same as WebAssembly, with three differences that come from the circuit: drag callbacks run a round trip later than the
native events; a drag `ItemFactory` cancels cannot be stopped in the browser any more, so it goes on as an inert
drag that no target accepts; and a custom drag layer starts from an estimated source position that is corrected as
soon as the measured one arrives. Prefer `OnHover` only where you need it - it is the one callback that fires
continuously; pointer moves are only sent while a `BdndDragLayer` follows them - give the layer `Accept` so that
only the drags it draws send them. A target accepting
`BdndNativeTypes.Html` receives the dropped page's HTML over the circuit, which may need a larger
`MaximumReceiveMessageSize` (see [text, links and HTML](#text-links-and-html)).

### Blazor Web App

Register `AddBitBdndServices()` in the server's and the client's `Program.cs`, and put the pages that drag in the
client project with `InteractiveWebAssembly` (or `InteractiveAuto`); the demo is built exactly this way.

### Blazor Hybrid

In a MAUI, WPF or Windows Forms `BlazorWebView` the JS runtime is not in-process, so the components behave as on
Blazor Server (without the network latency). Desktop web views support HTML5 drags for the mouse; touch on mobile
web views drags through pointer drags like any touch browser. The end-to-end suite drags in a WinForms
`BlazorWebView` on WebView2 - native mouse, keyboard, pointer and touch drags, a drag layer, insertion edges and
dropped files - the way MAUI renders on Windows too; MAUI's Android and iOS web views are not driven.

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
| Reading a dropped file throws `InvalidOperationException` | The target no longer keeps that drop's files: it keeps those of its last drop only, until it is disposed. Read them in `OnNativeDrop`. |
| Reading a dropped file throws `IOException` | It is larger than `maxAllowedSize`; pass a larger one to `OpenReadStreamAsync`. |
| A dropped folder arrives as one file with no contents | Set `ExpandDirectories="true"` on the target. |
| `OnEnd` reports `DidDrop = false` after dropping between two items | The drop landed on no target; see the gaps row above. |
| The arrow keys skip a target during a keyboard drag | It cannot take the item (type or `CanDropPredicate`), or it is inside the dragged source. |
| Screen readers announce "drop target 2 of 3" | Give the drop targets a `Label`. |
| A draggable became a tab stop | Keyboard dragging makes every draggable that can drag focusable; set `AllowKeyboard="false"` where another control does the same, or on the `BdndProvider` for all of its draggables. |
| A touch on a draggable scrolls instead of dragging | Touch drags start after the finger rests for `TouchDelay` ms; move sooner and the page scrolls. Use a `BdndDragHandle` (or `TouchDelay="0"` with your own `touch-action: none`) to drag at once. |
| A button inside a draggable starts a drag | Mark it (or a region around it) with `data-bit-bdnd-no-drag`. |
| The pointer drag's copy looks unstyled | It is inserted next to the source; selectors that depend on the source's position among its siblings (`:nth-child`, `+`) do not match it. Style `[data-bit-bdnd-preview]`, or use a drag layer. |

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
6. It runs pointer drags for touch and pen (and with `PointerDrag="All"` the mouse): it turns a qualifying press into
   a drag, moves a copy of the source with the pointer, hit-tests the targets under it on every frame, auto-scrolls,
   and reports to .NET - in order, through one queue shared with keyboard drags - the start
   (`HandlePointerDragStart`), each hover change, the `OnHover` moves (`HandlePointerHover`, coalesced), the drop
   (`HandlePointerDrop`, innermost target first until the provider that owns the drag answers) and the end.
7. It runs keyboard drags: Space or Enter on a source starts one, and from then on it takes the keys at the
   document, sends them to the source's `HandleKeyboardCommand` one at a time and in order, works out the target a
   move lands on (document order, among the targets .NET says can take the item), keeps the focus on the dragged
   item, and owns the live region screen readers hear.

There are no native events in a keyboard or pointer drag, so the manager walks the target chain itself: a target and its
enclosing targets of the same provider, innermost first - the order nested components put their host elements in,
and so the order native events bubble in.

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
| `Announcements` | `BdndAnnouncements?` | `null` | The keyboard instructions and screen-reader announcements; sets the manager's (`BdndAnnouncements.Default` otherwise). |
| `AllowKeyboard` | `bool?` | `null` | `false` takes every draggable of the context out of keyboard dragging; sets the manager's (`true` otherwise). |
| `OnDragStart` | `EventCallback<BdndDragEventArgs>` | | Any drag of the context began (right before its source's `OnBegin`). |
| `OnDragEnd` | `EventCallback<BdndDragEventArgs>` | | Any drag of the context ended, with `DidDrop` and the drop result. |
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
| `AllowKeyboard` | `bool` | `true` | Space/Enter on the source (or its handle) starts a keyboard drag; makes it focusable while it can drag. |
| `PointerDrag` | `BdndPointerDrag` | `BdndPointerDrag.Touch` | Which pointers drag through pointer events: `Touch` (touch and pen), `All` (the mouse too) or `None`. |
| `TouchDelay` | `int` | `250` | Milliseconds a touch rests on the source before a pointer drag starts (not on a handle). |
| `DragThreshold` | `double` | `5` | CSS pixels a pointer moves before a pointer drag starts (or, during the delay, gives it up). |
| `AutoScroll` | `bool` | `true` | A pointer drag scrolls the container (or page) under the pointer near its edge. |
| `PreviewAxis` | `BdndAxis` | `BdndAxis.None` | Keeps the copy following the pointer in a pointer drag on one axis (`Vertical` for a list). |
| `Label` | `string?` | | The source's name for screen readers (and its handle's `aria-label`). |
| `NativeData` | `IReadOnlyDictionary<string, string>?` | | Data a native drag carries for destinations outside the page, by `DataTransfer` type. |
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
| `OnEnter` | `EventCallback<BdndDropEventArgs<TItem>>` | | An in-app drag of an accepted type moved over the target (`IsOver` turned true). |
| `OnLeave` | `EventCallback<BdndDropEventArgs<TItem>>` | | An in-app drag moved off the target while the drag goes on. |
| `Disabled` | `bool` | `false` | The target accepts nothing: no hover, no drop, no native drop; the keyboard skips it. |
| `CanDropPredicate` | `Func<TItem?, IBdndMonitor, bool>?` | | Whether this target accepts the current item, on top of the type check. |
| `Label` | `string?` | | The target's name for screen readers, announced as a keyboard drag moves onto it. |
| `Edges` | `BdndEdges` | `BdndEdges.None` | The edges whose closest one is reported while a drag is over the target (`ClosestEdge`), for drops before or after it. |
| `AcceptFileTypes` | `IEnumerable<string>?` | | The file types (MIME types, `image/*`, `.png`) a native file drop is limited to. |
| `ExpandDirectories` | `bool` | `false` | A dropped folder becomes the files inside it, with their `RelativePath`. |
| `MaxFiles` | `int` | `0` | The most files a native drop hands over; 0 for no limit. |
| `Tag` | `string` | `"div"` | The host element's tag. |
| `ChildContent` | `RenderFragment<BdndDropState<TItem>>?` | | The content, given the collected state. |
| (any other attribute) | | | Applied to the host element. |

### BdndDragLayer&lt;TItem&gt;

| Parameter | Type | Default | Description |
| --- | --- | --- | --- |
| `Accept` | `string?` | | The item type the layer draws; a drag of another type is no drag to it, and its moves are not sent. None: every drag. |
| `AcceptTypes` | `IEnumerable<string>?` | | More item types the layer draws (combined with `Accept`). |
| `ChildContent` | `RenderFragment<BdndDragLayerState<TItem>>?` | | The preview, given the live drag state and offsets. |

### BdndDragHandle

| Parameter | Type | Default | Description |
| --- | --- | --- | --- |
| `ChildContent` | `RenderFragment?` | | The handle's content. |
| `Style` | `string?` | | The handle's inline style; `cursor: move` (and, while touch drags it, `touch-action: none`) is prepended while the draggable can drag. |
| (any other attribute) | | | Applied to the handle element. |

## Upgrading

### Keyboard dragging is on by default

This version makes every draggable that can drag keyboard-draggable, which changes what existing pages render and
do:

- A draggable host (or, with `UseHandle`, its `BdndDragHandle`) is now a tab stop: `tabindex="0"` unless the host
  was given a `tabindex`, plus an `aria-describedby` naming the keyboard instructions (after any ids it was
  given). A handle also gets `role="button"` and, from the draggable's `Label`, an `aria-label`, unless it was
  given its own. A click on a host now focuses it.
- Space and Enter on a focused host (or handle) start a keyboard drag: their default action is prevented and their
  `keyup` does not reach the page. Hosts with a `Tag` that is interactive by itself (`a`, `button`, `input`,
  `select`, `textarea`, `summary`) are left alone.
- The module adds a live region and a hidden instructions element at the end of `body`.
- Drop target hosts carry `data-bit-bdnd-can-drop`, `data-bit-bdnd-over` and `data-bit-bdnd-over-shallow` while a
  drag is in progress, and dragged hosts `data-bit-bdnd-dragging`.

A host that handles Space or Enter itself (an `@onkeydown` opening the item, say) now also picks the item up on
them. Set `AllowKeyboard="false"` on the draggables that should not take part - where another control already
performs the same operation, for instance - to get the previous markup and behavior back for them, or on the
`BdndProvider` to get it back for all of its draggables at once:

```razor
<BdndProvider AllowKeyboard="false">...</BdndProvider>
```

### Touch drags by default

Touch and pen now drag draggables (`PointerDrag="Touch"`): a touch that rests on a draggable for 250 ms starts a
drag instead of a long-press (which used to select text or open the context menu), and a `BdndDragHandle` gets
`touch-action: none`, so a swipe on a handle no longer scrolls. `PointerDrag="None"` gives a draggable the previous
behavior.

### Exceptions from callbacks reach the app

An exception thrown by `OnEnd` or `OnNativeDrop` used to be lost. It now reaches an `ErrorBoundary` or the app's
error UI like any event handler's (see [when a callback throws](#when-a-callback-throws)) - so a page whose
callbacks threw unnoticed will now show the error.

### A drag that begins while another is in progress ends it

When a drag begins while the manager still has one in progress (its end never arrived, or an async `OnDrop` of it is
still running), the earlier one now ends at once - its source's `OnEnd` runs with what the drag has so far: `DidDrop`
false unless its drop already counted, and the drop result published so far - instead of being overwritten without an
`OnEnd`.

## License

Bit.Bdnd is part of the [bit platform](https://github.com/bitfoundation/bitplatform) and is licensed under the
[MIT License](https://github.com/bitfoundation/bitplatform/blob/develop/LICENSE).
