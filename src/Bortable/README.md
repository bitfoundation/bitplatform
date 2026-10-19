# Bit.Bortable

A Blazor-native drag-and-drop sortable list, inspired by [SortableJS](https://github.com/SortableJS/Sortable):
reorder the items of a list, or drag them between lists that share a group.

Bortable runs on the browser's own HTML5 drag-and-drop events, handled by Blazor in C#. It is **not** a
JavaScript-interop wrapper around SortableJS: the sorting engine - where the item goes, which list may take it,
what is moved, cloned or swapped, and which events are raised - is written in C#. A small JavaScript module does
only what needs the DOM: drag handles, measuring where the pointer is (the swap thresholds), and the FLIP
animations that slide items into place.

```razor
<Bortable @bind-Items="_tasks" TItem="string">
    <ItemTemplate Context="task">@task</ItemTemplate>
</Bortable>

@code {
    private IList<string> _tasks = new List<string> { "Write the spec", "Review the PR", "Ship it" };
}
```

## What it is for

Any list people put in order by hand: to-do lists and backlogs, kanban boards, playlists and queues, form
builders and dashboards whose widgets are rearranged, priority rankings, file and menu trees, and pickers where
items are dragged from a palette into a target. The list is your own `IList<TItem>`, changed in place, so the
order a user makes is the order your code reads back.

## Highlights

- **Live reordering** while dragging, with smooth FLIP slide animations.
- **Shared lists**: lists with the same `Group` exchange items, with a ghost preview of where the item will land.
- **Pull and put rules** per list: move items out, clone them out, or keep them in; accept items or refuse them;
  sort or not.
- **Swap thresholds** ported from SortableJS - `SwapThreshold`, `InvertSwap`, `InvertedSwapThreshold` and
  `Direction` - including its protection against items of different sizes swapping back and forth.
- **Swap mode** (the SortableJS Swap plugin): drop an item onto another to trade their places.
- **Multi-drag** (the SortableJS MultiDrag plugin): select several items and drag them together.
- **Drag handles, filters** for locked items, and **per-item classes** that follow an item from list to list.
- **Any layout**: vertical lists, horizontal strips, grids and nested trees.
- **Two-way binding** with `@bind-Items`, and the SortableJS lifecycle events (`OnStart`, `OnEnd`, `OnAdd`,
  `OnRemove`, `OnUpdate`, `OnSort`, `OnChange`).
- **Generic and strongly typed**: `Bortable<TItem>`.
- Works on **Blazor WebAssembly, Blazor Server, Blazor Web App and Blazor Hybrid**, on .NET 8, 9 and 10, and is
  trimming and AOT friendly.

## Quick start

```bash
dotnet add package Bit.Bortable
```

Register the services in `Program.cs`:

```csharp
builder.Services.AddBitBortableServices();
```

Reference the stylesheet in the host page (`App.razor` or `index.html`):

```html
<link rel="stylesheet" href="_content/Bit.Bortable/bit-bortable.css" />
```

Add the namespace to `_Imports.razor`:

```razor
@using Bit.Bortable
```

And put a list on an interactively rendered page:

```razor
<Bortable @bind-Items="_items" TItem="string">
    <ItemTemplate Context="item">@item</ItemTemplate>
</Bortable>
```

The full walkthrough is in [getting started](#getting-started).

## Requirements

- .NET 8, .NET 9 or .NET 10
- Blazor WebAssembly, Blazor Server, Blazor Web App (any interactive render mode) or Blazor Hybrid
- A browser with HTML5 drag and drop - every evergreen desktop browser, and the mobile browsers that support it

## The demo

The demo site, [bortable.bitplatform.dev](https://bortable.bitplatform.dev), mirrors the SortableJS demo: a simple
list, shared lists, cloning, disabling sorting, a handle, a filter, the swap thresholds with a live view of the
swap zones, a grid, nested lists, an events log, and the multi-drag and swap plugins. Every page shows its own
source.

## How it fits together

```text
dragstart (an item)         Bortable records the drag on the scoped BortableService
  -> dragover (a list)      bit-bortable.js measures the pointer against the items (swap thresholds)
       the home list        the item is moved in the bound list, live, and Blazor moves its DOM node
       another list         a placeholder previews where the item will land; the item itself stays put
       swap mode            the item under the pointer is highlighted as the swap target
  -> drop / dragend         the pending move, clone or swap is committed to the bound lists
  -> ItemsChanged, events   once per changed list, then OnUpdate / OnRemove / OnAdd / OnSort / OnEnd
```

Everything ships in one package, `Bit.Bortable`, under one namespace, `Bit.Bortable`:

| Part | Contents |
| --- | --- |
| The component | `Bortable<TItem>`, the list. |
| The model | `BortablePull`, `BortableDirection` and `BortableEventArgs<TItem>`. |
| The services | `AddBitBortableServices`, which registers the scoped coordinator that tracks the active drag across lists. |
| The browser layer | `bit-bortable.js` (hit testing, drag handles, FLIP animations) and `bit-bortable.css` (the drag states). |

## MCP server

The demo hosts a [Model Context Protocol](https://modelcontextprotocol.io) server at
`https://bortable.bitplatform.dev/mcp`, so an AI agent writing sortable-list code reads the library itself - its
API, this guide and the demo's working pages - instead of guessing at an API it half remembers. It is also served,
next to the other bit libraries, through the aggregated `https://bitplatform.dev/mcp` endpoint.

```json
{
  "mcpServers": {
    "bortable": { "type": "http", "url": "https://bortable.bitplatform.dev/mcp" }
  }
}
```

Start with `SearchBortable`, which searches this guide, every public type and member, the setup guides, the
stylesheet's custom properties and classes and the demo's pages at once, and points each hit at the exact
follow-up call. `GetBortableSetupGuide` gives the wiring for one render mode, `GetBortableGuideSection` one section
of this guide, `GetBortableApi` one public type with its members, real defaults and documentation,
`GetBortableTheme` the stylesheet's custom properties and drag-state classes, and `GetBortableSourceFile` one of
the demo's working pages, or the library's component and stylesheet; each returns its index when called without a
key. `InspectBortableDrag` runs a real `Bortable` over lists you describe, performs one drag - within a list,
across lists, as a clone, a swap or a multi-drag - and reports whether it was allowed, the lists afterwards and
every event raised, in order. Every tool is read-only.

## Documentation

- [Getting started](#getting-started)
- [Sorting a list](#sorting-a-list)
- [Shared lists](#shared-lists)
- [Nested lists](#nested-lists)
- [Drag handles](#drag-handles)
- [Filtering items](#filtering-items)
- [Swap mode](#swap-mode)
- [Multi-drag](#multi-drag)
- [Events](#events)
- [Styling](#styling)
- [Hosting models](#hosting-models)
- [How it works](#how-it-works)
- [API reference](#api-reference)
- [Coming from SortableJS](#coming-from-sortablejs)
- [Limitations](#limitations)
- [Troubleshooting](#troubleshooting)

## Getting started

### Install

```bash
dotnet add package Bit.Bortable
```

In a Blazor Web App with an interactive client, the package belongs in the `.Client` project, and the services are
registered in both projects (see [hosting models](#hosting-models)).

### Register the services

```csharp
builder.Services.AddBitBortableServices();
```

It registers one scoped service, the coordinator that holds the drag in progress so that lists sharing a group can
hand an item to each other. Scoped means one per circuit on Blazor Server and one per application on WebAssembly,
so two users never share a drag. Calling it more than once is harmless. Without it, the first `Bortable` that
renders throws, because it cannot be given the service it injects.

### Reference the stylesheet

```html
<link rel="stylesheet" href="_content/Bit.Bortable/bit-bortable.css" />
```

It is not injected for you. It styles the drag states only - the ghost, the chosen item, the hidden original, the
swap target, the multi-drag selection and filtered items - and leaves the look of the list to you. The script
module, `_content/Bit.Bortable/bit-bortable.js`, needs no tag: the lists import it on demand, once for all of them.

### A first list

```razor
@page "/todo"

<Bortable @bind-Items="_items" TItem="string" Class="todo" ItemClass="todo-item">
    <ItemTemplate Context="item">@item</ItemTemplate>
</Bortable>

<p>Order: @string.Join(", ", _items)</p>

@code {
    private IList<string> _items = new List<string> { "Item 1", "Item 2", "Item 3", "Item 4" };
}
```

Each item is wrapped in a `<div>` that carries the drag handlers; `ItemTemplate` renders what goes inside it.
`Class` and `Style` go to the list container, `ItemClass` to every item wrapper.

### Two-way binding

`@bind-Items` keeps your field in step: the list is changed in place, and `ItemsChanged` is raised once per drag
for each list whose contents actually changed. Declare the field as `IList<TItem>` so its type matches the
parameter.

The list must be mutable - a `List<T>` or anything else whose `IsReadOnly` is `false`. A read-only list, such as an
array, is displayed and can be cloned from (`Pull="BortablePull.Clone"`), but it is never sorted, pulled from or
dropped into.

Items are rendered with `@key`, so they must be unique within a list: a list of strings must not hold the same
string twice. See [limitations](#limitations).

### Where to go next

- Drag between lists: [shared lists](#shared-lists).
- Copy instead of move: [cloning](#cloning).
- Restrict dragging to part of an item: [drag handles](#drag-handles).
- React to a drop: [events](#events).

## Sorting a list

### Live reordering

Within the list the drag started in, the item is moved in the bound list as the pointer passes other items, and the
other items slide out of its way. Because every item wrapper is rendered with `@key`, Blazor moves the existing DOM
node rather than recreating it, which keeps the browser's drag alive while the list reorders under it.

`Sort="false"` turns reordering off for a list: its items can still be dragged out to other lists of its group,
and it can still receive items, but its own order never changes.

### Direction

`Direction` is the axis the swap zones are measured along. `BortableDirection.Auto`, the default, reads it from
the layout of the first two items (the drop placeholder counts as one): side by side is horizontal, anything else
vertical. A list with a single item in view reads the layout instead, as SortableJS does: a flex row, or a grid of
several columns, is horizontal and a flex column vertical; otherwise an item that flows inline or floats is
horizontal. Set it explicitly for grids, whose first two items sit side by side even though the list wraps.

```razor
<Bortable @bind-Items="_tiles" TItem="Tile" Class="grid" Direction="BortableDirection.Horizontal">
    <ItemTemplate Context="tile">@tile.Name</ItemTemplate>
</Bortable>
```

Hit testing itself is two-dimensional, so a grid sorts in every direction whatever `Direction` says.

### Swap thresholds

The swap zone is the part of an item that, when the pointer enters it, moves the dragged item past that item.
`SwapThreshold` (0-1, default `1`) is how much of the item, measured from its centre, is the active zone; the rest
is a dead zone that does nothing. With `1` the dragged item swaps as soon as the pointer enters the next item; with
`0.3` the pointer must reach its middle 30%.

```razor
<Bortable @bind-Items="_items" TItem="string" SwapThreshold="0.65" Direction="BortableDirection.Vertical">
    <ItemTemplate Context="item">@item</ItemTemplate>
</Bortable>
```

After a swap, the area the dragged item has just vacated - its shadow - swaps it back. That is what keeps a short
item and a tall one from trading places back and forth while the pointer sits still. The logic is a port of
SortableJS's.

### Inverted swap

`InvertSwap="true"` moves the active zones to the item's edges: entering the outer part of an item inserts the
dragged item before or after it, depending on which half the pointer is in, for a "sort between items" feel.
`InvertedSwapThreshold` sets the size of those edge zones; left at `0` it is the same as `SwapThreshold`.

An item that enters a list it is not in yet always uses the inverted zones, so it can be dropped before the first
item as easily as after the last.

### Animation

`Animation` is the duration, in milliseconds, of the slide that moves items to their new places; the default is
`150` and `0` turns it off. Before each change the list records where every item is, lets Blazor update the DOM,
then transitions each item from its old position to its new one (the FLIP technique). An animation that is still
running when the next change comes is picked up from wherever it is, so items never jump.

### Disabling sorting

`Disabled="true"` turns every drag behaviour of a list off: its items cannot be dragged and nothing can be dropped
into it. The container gets the `bit-bortable-disabled` class. To keep a list's order fixed while still letting its
items be dragged out, use `Sort="false"` instead.

## Shared lists

### Groups

Lists with the same `Group` exchange items. While an item is dragged over another list of its group, a placeholder
shows where it will land; as in SortableJS, once it is previewed there it stays there even when the mouse is
released outside any list, or the drag is cancelled with Escape. Moving back over the list it came from takes it
back.

```razor
<Bortable @bind-Items="_todo" TItem="string" Group="board">
    <ItemTemplate Context="item">@item</ItemTemplate>
</Bortable>

<Bortable @bind-Items="_done" TItem="string" Group="board">
    <ItemTemplate Context="item">@item</ItemTemplate>
</Bortable>
```

The item itself is moved only when the drag ends. Until then it stays in its source list, hidden, because moving a
DOM node from one component to another would recreate it and cancel the browser's drag.

### Pull and put

`Pull` decides how items leave a list, and `Put` whether items of the group may enter it:

| `Pull` | Meaning |
| --- | --- |
| `BortablePull.Move` | Items can be moved out (the default). |
| `BortablePull.None` | Items cannot be dragged out. |
| `BortablePull.Clone` | A copy is placed in the target; the original stays. |

```razor
@* A source that can be dragged from but never dropped into, and keeps its own order. *@
<Bortable @bind-Items="_palette" TItem="string" Group="board" Put="false" Sort="false">
    <ItemTemplate Context="item">@item</ItemTemplate>
</Bortable>
```

### Cloning

With `Pull="BortablePull.Clone"` the item is copied into the target and the original stays where it was, so the
same item can be cloned again and again. `Clone` is the factory that makes the copy:

```razor
<Bortable @bind-Items="_source" TItem="Card" Group="cards" Pull="BortablePull.Clone" Clone="c => new Card { Name = c.Name }">
    <ItemTemplate Context="card">@card.Name</ItemTemplate>
</Bortable>

<Bortable @bind-Items="_target" TItem="Card" Group="cards">
    <ItemTemplate Context="card">@card.Name</ItemTemplate>
</Bortable>
```

The copy must be a distinct object: Blazor needs a unique `@key` per item, so a copy that equals an item already in
the target is refused. Without a `Clone` factory, an `ICloneable` item is copied with `Clone()`, and anything else
is shared as is - which is fine for an immutable reference type, and refused for a value-equal duplicate such as
the same string twice.

### Read-only lists

A read-only `IList<TItem>` (an array, `ReadOnlyCollection<T>`, `ImmutableList<T>`) is displayed and can be cloned
from, but it is never sorted, never pulled from, and never dropped into - a `Pull` of `Move` on it acts as `None`.

### Lists of different item types

Lists of different item types may share a group. An item is only ever offered to lists of its own type: a list of
`Task` items and a list of `Note` items in the same group simply ignore each other's items.

## Nested lists

A list can contain other lists, to any depth: render a `Bortable` inside the `ItemTemplate` of another. Every level
shares a group, so items move between branches.

```razor
@* NestedList.razor *@
<Bortable Items="Items" TItem="TreeItem" Group="tree" SwapThreshold="0.65" Class="tree">
    <ItemTemplate Context="item">
        <div>@item.Name</div>
        <NestedList Items="item.Children" />
    </ItemTemplate>
</Bortable>

@code {
    [Parameter, EditorRequired] public List<TreeItem> Items { get; set; } = [];
}
```

An item is never dropped inside itself: the lists within the dragged item's own subtree, and the lists inside the
drop preview, refuse it. Each nested list handles the events of its own items only, so a drag that starts in an
inner list is not also a drag of the outer item around it. A nested list takes the pointer only for a drag it could
receive: one of another group, of another item type, or with `Put="false"` leaves it to the list around it, so the
columns of a board still sort while the pointer crosses the card lists inside them. Give nested lists some padding
or a minimum height, so an empty branch still has an area to drop into.

## Drag handles

`Handle` is a CSS selector: with it, an item starts a drag only when it is pressed on an element matching the
selector inside it, and the rest of the item keeps its normal pointer behaviour - text can be selected, inputs
focused, buttons clicked.

```razor
<Bortable @bind-Items="_items" TItem="string" Handle=".handle">
    <ItemTemplate Context="item">
        <span class="handle">☰</span> @item
    </ItemTemplate>
</Bortable>
```

The container gets the `bit-bortable-has-handle` class, which the stylesheet uses to drop the grab cursor from the
rest of the item. A handle belongs to the item it sits in: a handle of a nested list's item never drags the outer
item.

## Filtering items

`Filter` returns `true` for items that must not be dragged. A filtered item cannot be picked up and gets the
`bit-bortable-filtered` class, but the other items can still be dragged past it, which moves it along by one: as in
SortableJS, a filter does not pin an item to its index.

```razor
<Bortable @bind-Items="_items" TItem="string" Filter='i => i.StartsWith("Locked")'>
    <ItemTemplate Context="item">@item</ItemTemplate>
</Bortable>
```

## Swap mode

`Swap="true"` trades places instead of sorting - the SortableJS Swap plugin. The list stays still while the item is
dragged; the item under the pointer is highlighted with `SwapClass` (default `bit-bortable-swap-highlight`), and on
drop the dragged item and that item swap positions. Swapping works within a list and between lists that share a
group, as long as each list could take the other's item. A swap always trades the items themselves, so a list whose
`Pull` is `Clone` gives its item away in a swap rather than a copy of it.

```razor
<Bortable @bind-Items="_items" TItem="string" Swap="true">
    <ItemTemplate Context="item">@item</ItemTemplate>
</Bortable>
```

## Multi-drag

`MultiDrag="true"` lets several items move as one - the SortableJS MultiDrag plugin. A click toggles an item's
selection (highlighted with `SelectedClass`, default `bit-bortable-selected`). Dragging a selected item moves the
whole selection, which lands as a contiguous block in its original order; dragging an unselected item clears the
selection and moves just that item. It works within a list and across lists that share a group.

```razor
<Bortable @bind-Items="_items" TItem="string" MultiDrag="true">
    <ItemTemplate Context="item">@item</ItemTemplate>
</Bortable>
```

The `Items` property of the event arguments lists every item a drag moved. Multi-drag moves; combined with a clone
pull, dragging into another list clones only the dragged item. Items that leave the list, or are replaced along
with the whole `Items` list, leave the selection too.

## Events

### The lifecycle

All events are `EventCallback<BortableEventArgs<TItem>>`:

| Event | Raised when | On |
| --- | --- | --- |
| `OnStart` | A drag begins. | The source list. |
| `OnChange` | The dragged item changes position during the drag. | The list it moves in. |
| `OnUpdate` | The order of the list changed within the same list. | The list. |
| `OnRemove` | An item was moved out to another list (not raised for a clone). | The source list. |
| `OnAdd` | An item was dropped in from another list. | The destination list. |
| `OnSort` | Any change to a list: add, remove or update. | Each list that changed. |
| `OnEnd` | The drag ended, whatever its outcome - also when it ended before its release (see below). | The source list. |

```razor
<Bortable @bind-Items="_items" TItem="string"
          OnUpdate='e => Log($"moved {e.Item}: {e.OldIndex} -> {e.NewIndex}")'
          OnAdd='e => Log($"added {e.Item} from {e.FromId}")'>
    <ItemTemplate Context="item">@item</ItemTemplate>
</Bortable>
```

### Event arguments

| Member | Type | Description |
| --- | --- | --- |
| `Item` | `TItem` | The dragged item (the copy, for a clone that was dropped). |
| `Items` | `IReadOnlyList<TItem>` | Every item moved by the drag: the multi-drag selection, or just `Item`. |
| `OldIndex` | `int` | Index in the source list when the drag started. |
| `NewIndex` | `int` | Index in the destination list. |
| `FromId` | `string` | `Id` of the source list. |
| `ToId` | `string` | `Id` of the destination list. |
| `FromGroup` | `string?` | Group of the source list. |
| `ToGroup` | `string?` | Group of the destination list. |
| `CrossedLists` | `bool` | `true` when the item moved between lists. |

Give the lists an `Id` of your own when a handler needs to tell them apart; otherwise each gets a generated one.

### Order of events

The completion events are raised after the bound lists have been updated and `ItemsChanged` has been notified, so a
handler sees the final state. Within a list: `OnUpdate`, `OnSort`, `OnEnd`. Across lists: `OnRemove` on the source
(unless it was a clone), `OnAdd` on the destination, `OnSort` on the source (unless it was a clone), `OnSort` on the
destination, then `OnEnd` on the source. A drag that leaves the lists as they were raises no completion event but
`OnEnd`. All of them are raised once the drag is over, so a handler that takes its time - a save in `ItemsChanged`,
say - never holds up the next drag.

A drag can also end before it is released: when its source list leaves the page (a tab or a panel closing over
it), or when the browser never reports its end and the next drag starts. It ends where it is: a preview in another
list is dropped, while a live reorder of the source list stays, and is reported like any other - `ItemsChanged`,
`OnUpdate`, `OnSort` - followed by `OnEnd`.

## Styling

### The stylesheet

`bit-bortable.css` covers the drag states only. The list and its items take whatever your own CSS gives them,
through `Class` and `Style` on the container and `ItemClass` and `ItemClassSelector` on the items.

### The custom properties

| Property | Default | Used for |
| --- | --- | --- |
| `--bit-bortable-accent-rgb` | `13, 110, 253` | The accent every state is tinted with, as an `r, g, b` triplet. |
| `--bit-bortable-ghost-opacity` | `0.4` | The dragged item and the drop placeholder. |
| `--bit-bortable-chosen-bg` | the accent at 12% | The chosen item's background. |
| `--bit-bortable-swap-bg` | the accent at 18% | The swap target's background. |
| `--bit-bortable-swap-outline` | `2px dashed` accent at 60% | The swap target's outline. |
| `--bit-bortable-selected-bg` | the accent at 25% | A multi-drag selected item's background. |
| `--bit-bortable-selected-ring` | `inset` 2px accent at 80% | A multi-drag selected item's ring (a `box-shadow`). |
| `--bit-bortable-filtered-opacity` | `0.7` | A filtered item. |

Override them on `:root`, on one list's `Class`, or inline through `Style`:

```css
:root { --bit-bortable-accent-rgb: 109, 40, 217; }
```

### Drag-state classes

| Class | Set on |
| --- | --- |
| `bit-bortable` | The list container. |
| `bit-bortable-item` | Every item wrapper, and the drop placeholder. |
| `bit-bortable-ghost` (`GhostClass`) | The item being dragged in its list, and the drop placeholder. |
| `bit-bortable-chosen` (`ChosenClass`) | The same: the item being dragged, and the drop placeholder. |
| `bit-bortable-hidden` | The original of a moved item while it is previewed in another list. |
| `bit-bortable-swap-highlight` (`SwapClass`) | The swap target, in swap mode. |
| `bit-bortable-selected` (`SelectedClass`) | Items in the multi-drag selection. |
| `bit-bortable-filtered` | Items the `Filter` excludes. |
| `bit-bortable-disabled` | The container of a `Disabled` list. |
| `bit-bortable-has-handle` | The container of a list with a `Handle`. |

`GhostClass`, `ChosenClass`, `SwapClass` and `SelectedClass` replace the class used for their state, for a list
that wants its own look instead of the stylesheet's.

### Per-item classes

`ItemClassSelector` appends a class computed from the item itself, so an item's styling follows it when it moves
to another list - and the drop placeholder shows it too, so the preview looks like the item will.

```razor
<Bortable @bind-Items="_left" TItem="string" Group="shared"
          ItemClass="card" ItemClassSelector='i => i.StartsWith("B") ? "tinted" : null'>
    <ItemTemplate Context="item">@item</ItemTemplate>
</Bortable>
```

## Hosting models

### Blazor WebAssembly

Register the services in `Program.cs` and link the stylesheet from `wwwroot/index.html`. Every drag event is
handled in the browser.

### Blazor Server

Register the services in `Program.cs` and link the stylesheet from `App.razor`. Each drag event is a round trip
over the circuit: the list keeps up because a `dragover` that arrives while the previous one is still being handled
is skipped rather than queued, but the preview trails the pointer by the network's latency. The coordinator is
scoped to the circuit, so users never see each other's drags.

### Blazor Web App

The lists must render interactively - `InteractiveServer`, `InteractiveWebAssembly` or `InteractiveAuto`. A
statically rendered page shows the items but never answers a drag. With a WebAssembly or Auto render mode, put the
pages with lists in the `.Client` project, reference `Bit.Bortable` from it, and register the services in both
`Program.cs` files: the server prerenders the same components and must be able to resolve what they inject. Link
the stylesheet from the server's `App.razor`.

### Blazor Hybrid

Register the services in `MauiProgram.cs` and link the stylesheet from `wwwroot/index.html`. Drag and drop is the
web view's own, so it works wherever the platform's web view supports HTML5 drag and drop.

### Prerendering

A prerendered list renders its items in their order, so the first response already shows them; dragging starts
working once the page becomes interactive. The script module is imported after the first interactive render, and
every call into it is skipped until then.

### Content security policy

The module is a static file under `_content/Bit.Bortable/`, loaded with `import()` from the app's own origin, so
`script-src 'self'` covers it. The animations set `transform` and `transition` through the CSSOM, which a
`style-src` policy does not block. The component renders no inline `style` attribute of its own; only the `Style`
parameter does, when you set it.

## How it works

### Native drag events

Each item wrapper is `draggable`, and the component handles `dragstart`, `dragover`, `drop` and `dragend` in C#.
The handlers of every list are serialized through the scoped coordinator, so a burst of `dragover` events can never
act on stale positions, and whichever of `drop` and `dragend` comes first finishes the drag.

The browser layer marks the drags of the lists with a data type of their own, `application/x-bit-bortable`, when it
lets an item start one. Any other drag - a file, selected text, a link or an element of your own inside an item -
is left alone: a list neither starts a sort for it nor allows it to be dropped, and its events reach your own
handlers, around the list or inside its items. The `dragover` and `drop` events of a list's drag stop at the list
that takes them.

### Live reordering within a list

Within its own list, the dragged item is moved in the bound list on each `dragover` that crosses a swap zone, and
`@key` makes Blazor move its DOM node instead of recreating it, which keeps the native drag alive.

### Cross-list preview

Over another list the item is not moved. The target renders a placeholder - the dragged item's own template, with
the ghost classes - where it would land, and the source hides its original with `display: none` (for a move; a clone
keeps it visible). The data moves when the drag ends, on drop or on release anywhere.

### FLIP animations

Before each change the lists that are about to change record the position of every item; after Blazor has updated
the DOM, each item is moved back to where it was with a transform and transitioned to its new place.

### The browser layer

`bit-bortable.js` marks the lists' own drags and allows their drop, measures the pointer against the items for
the swap thresholds, installs the `pointerdown` listener behind drag handles, refuses a drop into the dragged item's
own subtree, and plays the animations. It keeps no copy of the lists and decides nothing about them.

## API reference

### Bortable parameters

| Parameter | Type | Default | Description |
| --- | --- | --- | --- |
| `Items` | `IList<TItem>?` | `null` | The items to display and reorder (a mutable list). Bind with `@bind-Items`. |
| `ItemsChanged` | `EventCallback<IList<TItem>>` | | Raised once per drag when the list changed. |
| `ItemTemplate` | `RenderFragment<TItem>?` | `null` | Template rendered for each item. |
| `ChildContent` | `RenderFragment?` | `null` | Extra content rendered after the items, such as an empty state. |
| `Id` | `string` | generated | Stable DOM id of the list, reported as `FromId` and `ToId`. |
| `Group` | `string?` | `null` | Lists sharing a group name can exchange items. |
| `Pull` | `BortablePull` | `Move` | How items leave this list: `Move`, `None` or `Clone`. |
| `Put` | `bool` | `true` | Whether items from the group may be dropped here. |
| `Sort` | `bool` | `true` | Whether the list can be reordered. |
| `SwapThreshold` | `double` | `1` | Fraction (0-1) of an item that is an active swap zone, measured from its centre. |
| `InvertSwap` | `bool` | `false` | Moves the swap zones to the item's edges. |
| `InvertedSwapThreshold` | `double` | `0` | Fraction (0-1) used for the inverted zones; `0` means `SwapThreshold`. |
| `Direction` | `BortableDirection` | `Auto` | The sort axis: `Auto`, `Horizontal` or `Vertical`. |
| `Disabled` | `bool` | `false` | Turns all drag behaviour off. |
| `Animation` | `int` | `150` | Slide animation duration in milliseconds; `0` turns it off. |
| `Handle` | `string?` | `null` | CSS selector of the drag handle within each item. |
| `Filter` | `Func<TItem, bool>?` | `null` | Returns `true` for items that cannot be dragged. |
| `Clone` | `Func<TItem, TItem>?` | `null` | Factory used to copy an item in clone mode. |
| `Class` | `string?` | `null` | Extra CSS classes of the list container. |
| `Style` | `string?` | `null` | Inline style of the list container. |
| `ItemClass` | `string?` | `null` | CSS class of every item wrapper. |
| `ItemClassSelector` | `Func<TItem, string?>?` | `null` | Per-item class appended to the wrapper. |
| `GhostClass` | `string` | `bit-bortable-ghost` | Class of the dragged item and the drop placeholder. |
| `ChosenClass` | `string` | `bit-bortable-chosen` | Class of the chosen item. |
| `Swap` | `bool` | `false` | Trades item positions instead of sorting (Swap plugin). |
| `SwapClass` | `string` | `bit-bortable-swap-highlight` | Class of the highlighted swap target. |
| `MultiDrag` | `bool` | `false` | Selects several items and drags them together (MultiDrag plugin). |
| `SelectedClass` | `string` | `bit-bortable-selected` | Class of the items in the multi-drag selection. |
| `OnStart`, `OnEnd`, `OnAdd`, `OnRemove`, `OnUpdate`, `OnSort`, `OnChange` | `EventCallback<BortableEventArgs<TItem>>` | | See [events](#events). |

### BortableEventArgs

See [event arguments](#event-arguments).

### BortablePull

| Value | Meaning |
| --- | --- |
| `Move` | Items can be moved out (the default). |
| `None` | Items cannot be dragged out. |
| `Clone` | A copy is placed in the target; the original stays. |

### BortableDirection

| Value | Meaning |
| --- | --- |
| `Auto` | Read from the layout of the first two items, or of the list for a single one (the default). |
| `Horizontal` | Swap zones span the item's width. |
| `Vertical` | Swap zones span the item's height. |

### BitBortable

| Method | Description |
| --- | --- |
| `AddBitBortableServices(this IServiceCollection)` | Registers the scoped drag coordinator. Required. |

## Coming from SortableJS

| SortableJS | Bortable |
| --- | --- |
| `group: "name"` | `Group="name"` |
| `group: { pull: true / false / "clone" }` | `Pull="BortablePull.Move / None / Clone"` |
| `group: { put: false }` | `Put="false"` |
| `sort`, `disabled`, `animation` | `Sort`, `Disabled`, `Animation` |
| `handle: ".handle"` | `Handle=".handle"` |
| `filter: ".locked"` | `Filter="item => item.IsLocked"` - a predicate on the item, not a selector |
| `ghostClass`, `chosenClass` | `GhostClass`, `ChosenClass` |
| `swapThreshold`, `invertSwap`, `invertedSwapThreshold`, `direction` | The parameters of the same names |
| `onStart`, `onEnd`, `onAdd`, `onRemove`, `onUpdate`, `onSort`, `onChange` | `OnStart`, `OnEnd`, `OnAdd`, `OnRemove`, `OnUpdate`, `OnSort`, `OnChange` |
| Swap plugin: `swap`, `swapClass` | `Swap`, `SwapClass` |
| MultiDrag plugin: `multiDrag`, `selectedClass` | `MultiDrag`, `SelectedClass` |
| `toArray()`, `sort()`, `store` | Not needed: the bound `IList<TItem>` is the state |
| `forceFallback` and the `fallback*` options | Not supported: only native drag and drop is used |
| `easing`, `dragClass`, `onMove`, `onClone`, `onChoose`, `onFilter`, `multiDragKey` | Not supported |

## Limitations

- Items must be unique within a list, as Blazor's `@key` requires. An item is never dropped into a list that
  already holds an equal one, so a value-equal duplicate (the same string twice) is refused rather than breaking
  rendering. For clone mode, use a reference type and a `Clone` factory that returns a distinct object.
- An item moving between lists is committed when the drag ends, with a live placeholder preview meanwhile, rather
  than physically moving between lists mid-drag.
- Drag and drop is the browser's own HTML5 implementation, with its own drag image and auto-scrolling. There is no
  pointer-event fallback for browsers or devices without it.
- Custom easing, `onMove`-style veto callbacks and the `toArray`/`sort`/`store` helpers are not implemented.

## Troubleshooting

**"Cannot provide a value for property 'Service' on type 'Bit.Bortable.Bortable\`1[...]'. There is no registered
service of type 'Bit.Bortable.BortableService'."** - the services are not registered. Call `builder.Services.AddBitBortableServices()`, in both projects of a Blazor Web App.

**The items render, but nothing can be dragged** - the page is not interactive. Give it, or the app, an interactive
render mode.

**Items can be dragged, but the dragged item is not dimmed and the original does not hide while it is previewed in
another list** - the stylesheet is missing. Link `_content/Bit.Bortable/bit-bortable.css`.

**An item cannot be dropped into another list** - check that both lists have the same `Group`, that the source's
`Pull` is not `None`, that the target's `Put` is `true` and it is not `Disabled`, that the target's list is mutable,
that it has the same `TItem`, and that it does not already hold an equal item.

**A cloned item is refused** - the `Clone` factory returned an item equal to one the target already holds. Return a
new object.

**An empty list cannot be dropped into** - an empty container with no height has no area for the pointer to enter.
Give the list a `min-height` or some padding.

**`@bind-Items` does not compile** - declare the field as `IList<TItem>`, the parameter's type.
