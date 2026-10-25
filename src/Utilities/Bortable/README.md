# Bit.Bortable

A Blazor-native drag-and-drop sortable list, inspired by [SortableJS](https://github.com/SortableJS/Sortable):
reorder the items of a list, or drag them between lists that share a group.

Bortable is **not** a JavaScript-interop wrapper around SortableJS: the sorting engine - which list may take an
item, what a position means, what is moved, cloned or swapped, and which events are raised - is written in C#, over
your own `IList<TItem>`. A small JavaScript module does what has to happen where the pointer is: it follows the
browser's drag events, measures where the pointer is against the items (the swap thresholds), and calls .NET only
when the drop position changes - so a drag stays smooth on Blazor Server too - and it plays the FLIP animations
that slide items into place.

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
- **Rules**: `CanDrop` vets every position before it is shown (the SortableJS `onMove`), `MaxItems` caps a list,
  and `ConfirmDrop` asks your code - a server, a dialog - before a drop is kept, putting everything back if it says
  no.
- **Cancel and spill**: a cancelled drag leaves no partial change behind; `RevertOnSpill` puts a drag released
  outside every list back where it was.
- **Moves from code**: `MoveAsync` and `MoveToAsync` move an item as a drag would - the rules, the animation and
  the events included - for "move up" buttons and item menus.
- **Keyboard and screen readers**: pick an item up with Space, move it with the arrow keys - within its list and
  across to the next one - and drop it; every step is announced, in words you can translate.
- **Any layout**: vertical lists, horizontal strips, grids, right-to-left and reversed layouts, and nested trees.
- **Two-way binding** with `@bind-Items`, and the SortableJS lifecycle events (`OnStart`, `OnEnd`, `OnAdd`,
  `OnRemove`, `OnUpdate`, `OnSort`, `OnChange`), which also say how the drag was made and whether it was
  cancelled.
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
- Any evergreen browser: the mouse uses HTML5 drag and drop, a finger or a pen pointer events, so touch screens work
  whether or not their browser drags natively

## The demo

The demo site, [bortable.bitplatform.dev](https://bortable.bitplatform.dev), mirrors the SortableJS demo: a simple
list, shared lists, cloning, disabling sorting, a handle, a filter, the swap thresholds with a live view of the
swap zones, a grid, nested lists, an events log, and the multi-drag and swap plugins. Every page shows its own
source.

## How it fits together

```text
dragstart (an item)         bit-bortable.js names the item to its list; Bortable records the drag on the scoped
                            BortableService and answers which lists take it
  -> the pointer moves      bit-bortable.js finds the list under the pointer that takes the drag, measures the
                            pointer against its items (swap thresholds), and calls .NET only for a new position
       the home list        the item is moved in the bound list, live, and Blazor moves its DOM node
       another list         a placeholder previews where the item will land; the item itself stays put
       swap mode            the item under the pointer is highlighted as the swap target
       CanDrop says no      nothing moves, and the pointer shows it cannot drop there
  -> drop / release         the pending move, clone or swap is committed to the bound lists (or, cancelled,
                            everything is put back)
  -> ConfirmDrop            the destination may still refuse it, which puts everything back
  -> ItemsChanged, events   once per changed list, then OnUpdate / OnRemove / OnAdd / OnSort / OnEnd
```

Everything ships in one package, `Bit.Bortable`, under one namespace, `Bit.Bortable`:

| Part | Contents |
| --- | --- |
| The component | `Bortable<TItem>`, the list. |
| The model | `BortablePull`, `BortableDirection`, `BortableInput` and `BortableEventArgs<TItem>`. |
| The services | `AddBitBortableServices`, which registers the scoped coordinator that tracks the active drag across lists. |
| The browser layer | `bit-bortable.js` (hit testing, drag handles, FLIP animations) and `bit-bortable.css` (the drag states). |

## MCP server

The demo hosts a [Model Context Protocol](https://modelcontextprotocol.io) server at
`https://bortable.bitplatform.dev/mcp`, so an AI agent writing sortable-list code reads the library itself - its
API, this guide and the demo's working pages - instead of guessing at an API it half remembers.

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
- [Rules](#rules)
- [Cancelling a drag](#cancelling-a-drag)
- [Moving items from code](#moving-items-from-code)
- [Touch, pen and mouse](#touch-pen-and-mouse)
- [The keyboard](#the-keyboard)
- [Accessibility](#accessibility)
- [Events](#events)
- [Styling](#styling)
- [Hosting models](#hosting-models)
- [How it works](#how-it-works)
- [Performance](#performance)
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

### App-wide defaults

The same call takes defaults for every list of the app - the texts in its language, a longer long press, a slower
slide:

```csharp
builder.Services.AddBitBortableServices(options =>
{
    options.Messages = MyTexts.German;
    options.TouchDelay = 300;
    options.Animation = 200;
});
```

A default replaces the parameter's own default for every list that does not set the parameter itself; a list that
does keeps its value. `BortableOptions` covers `Animation`, `SwapThreshold`, `Delay`, `TouchDelay`, `DelayTolerance`,
`ForceFallback`, `Scroll`, `ScrollSensitivity`, `ScrollSpeed`, `Keyboard`, `RevertOnSpill`, `Messages`, and the
classes `GhostClass`, `ChosenClass`, `DragClass`, `SwapClass` and `SelectedClass`. Several calls add up, in order.

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
selection (highlighted with `SelectedClass`, default `bit-bortable-selected`), Shift+click selects every item from
the one last clicked, and from the keyboard Shift+Space toggles the focused item. Dragging a selected item moves the
whole selection, which lands as a contiguous block in its original order; dragging an unselected item clears the
selection and moves just that item. It works within a list and across lists that share a group, with the pointer
and with the keyboard.

```razor
<Bortable @bind-Items="_items" @bind-SelectedItems="_selected" TItem="string" MultiDrag="true">
    <ItemTemplate Context="item">@item</ItemTemplate>
</Bortable>

<button @onclick="() => _selected = []">Clear the selection</button>

@code {
    private IList<string> _items = new List<string> { "One", "Two", "Three" };
    private IReadOnlyCollection<string> _selected = [];
}
```

`@bind-SelectedItems` keeps the selection in step with your field both ways: the list reports every change - a
click, a range, Shift+Space, a drag that moves the selection (which spends it) or picks up an unselected item (which
clears it) - and takes a selection you set. Items that are not in the list, or are locked by `Filter`, are left out.

The `Items` property of the event arguments lists every item a drag moved. Multi-drag moves; combined with a clone
pull, dragging into another list clones only the dragged item. Items that leave the list, or are replaced along
with the whole `Items` list, leave the selection too.

## Rules

`Group`, `Pull`, `Put` and `Sort` decide which lists exchange items. Three more parameters bring your own rules -
a kanban board's workflow, a column's work-in-progress limit, a server that has the last word.

### CanDrop

`CanDrop` is asked whether the dragged item may take a position in this list, as the item reaches it and before
anything is shown - for a reorder within the list and for an item coming in from another list alike. It receives
the proposed move as `BortableEventArgs<TItem>`: `Item` (and `Items` for a multi-drag), `FromId` and `FromGroup`
(where it comes from), `OldIndex`, and `NewIndex`, the index the item would have here. Return `false` to refuse: the
item stays where it was, and the pointer shows that it cannot be dropped there. This is the SortableJS `onMove`.

```razor
@* "Done" takes cards from "Review" only, and never above the pinned first card. *@
<Bortable @bind-Items="_done" TItem="Card" Group="board" Id="done"
          CanDrop='e => (e.FromId is "review" or "done") && e.NewIndex > 0'>
    <ItemTemplate Context="card">@card.Title</ItemTemplate>
</Bortable>
```

It runs while the pointer moves, so keep it fast and free of side effects.

### MaxItems

`MaxItems` caps how many items a list holds. A full list takes nothing from other lists - the pointer passes over
it to the list around it, or shows that it cannot drop - while its own items still sort. A multi-drag counts every
item it carries; a swap brings one item and takes one, so it is never refused for room.

```razor
<Bortable @bind-Items="_inProgress" TItem="Card" Group="board" MaxItems="3">
```

### ConfirmDrop

`ConfirmDrop` is asked once an item has been dropped into the list - for a check that has to wait: a server call, a
confirmation dialog. The drop shows at once, and the lists stay as dropped while the task runs. Return `true` to keep
it, and the usual events follow; return `false` to put back every list the drag changed, with the items sliding back:
`OnEnd` then reports `Canceled`, and no `OnUpdate`, `OnAdd`, `OnRemove` or `OnSort` is raised. `ItemsChanged` is
raised for the lists put back - your page may have rendered them as dropped while the task ran - with what they held
before the drag.

```razor
<Bortable @bind-Items="_done" TItem="Card" Group="board" ConfirmDrop="SaveAsync">
    <ItemTemplate Context="card">@card.Title</ItemTemplate>
</Bortable>

@code {
    private async Task<bool> SaveAsync(BortableEventArgs<Card> e)
        => await Api.TryMoveAsync(e.Item.Id, e.ToId, e.NewIndex);
}
```

It is asked on the list the item lands in, with the move as committed: `NewIndex` is where the item is now. No other
drag starts until it answers, and the lists must not be changed meanwhile - a refusal puts back what they held at
the drop. A `ConfirmDrop` that throws puts the lists back too, raises `OnEnd` with `Canceled`, and the exception is
passed on.

## Cancelling a drag

A drag that is cancelled leaves nothing behind: a live reorder is undone, an item previewed in another list goes
back home, a pending swap or multi-drag is dropped, and `OnEnd` reports `Canceled` - with no `ItemsChanged`,
`OnUpdate`, `OnAdd`, `OnRemove` or `OnSort`. Only the dragged item's own move is undone, so an item your code added
to the list meanwhile stays.

What counts as a cancel depends on how the item is dragged:

- **A mouse drag** is the browser's native drag, which reports Escape exactly like a release outside every list.
  Both keep what the drag did, as in SortableJS - an item previewed in another list lands there - unless the list
  the drag started in has `RevertOnSpill="true"`, which puts both back.
- **A keyboard, touch or pen drag** is told apart: Escape (or a pointer the system takes over) always cancels.

```razor
@* Released outside every list, the card goes back where it was. *@
<Bortable @bind-Items="_backlog" TItem="Card" Group="board" RevertOnSpill="true">
```

## Moving items from code

`MoveAsync(item, newIndex)` moves an item within its list, and `MoveToAsync(item, listId, newIndex)` into another
list of the group, exactly as a drag would: the same rules decide (`Sort`, `Disabled`, `Pull`, `Put`, `Group`,
`MaxItems`, `CanDrop`), `ConfirmDrop` is asked, the items slide into place, and the drag's events follow with
`Input` set to `BortableInput.Api`. Both return `true` when the item moved. A clone source puts a copy in the
target; a swap-mode list trades the item with the one at `newIndex`. `Filter` locks an item against the user's drags
only, and a multi-drag selection is not carried along.

That is what an accessible list offers next to dragging (WCAG 2.5.7): "move up", "move to top" buttons, or an item
menu that moves a card to another column.

```razor
<Bortable @ref="_list" @bind-Items="_items" TItem="string">
    <ItemTemplate Context="item">
        @item
        <button @onclick="() => _list.MoveAsync(item, Math.Max(0, _items.IndexOf(item) - 1))"
                aria-label="@($"Move {item} up")">↑</button>
    </ItemTemplate>
</Bortable>

@code {
    private Bortable<string> _list = default!;
    private IList<string> _items = new List<string> { "One", "Two", "Three" };
}
```

A move is refused while a drag is in progress. It is announced to screen readers, like a keyboard drag's.

## Touch, pen and mouse

A mouse drags with the browser's own drag and drop, as SortableJS does on the desktop: the browser draws the drag
image and the cursor. A finger or a pen drags with pointer events instead - the item stays in its list, dimmed, while
a copy of it follows the pointer - which works the same in every browser and web view, whether or not it supports
HTML5 drag and drop on touch. Both feed the same router: the same lists take the drag, the same hit test places the
item, the same events follow.

| Parameter | Default | |
| --- | --- | --- |
| `TouchDelay` | `200` | A finger must rest on an item this long (ms) before it drags it, so a swipe over the list still scrolls the page. |
| `Delay` | `0` | The same for the mouse and a pen. |
| `DelayTolerance` | `5` | How far (px) the pointer may move while the delay runs; further, and the press is a scroll or a click. |
| `ForceFallback` | `false` | The mouse drags like a finger, with the preview: for a preview of your own style, or a web view without HTML5 drag and drop. |
| `DragClass` | `bit-bortable-drag` | The class of the preview, which is a copy of the item placed above the page. |

Once a finger drags an item the page does not scroll under it; Escape, a system gesture that takes the pointer
over (`pointercancel`), or leaving the page cancels the drag and puts everything back. The click a browser follows a
press with is swallowed after a drag, so dropping an item does not also click it. A press on a control inside an
item - a button, a link, an input - is left to the control.

Near an edge, a pointer drag scrolls (`Scroll`, on by default): the innermost scrolling element under the pointer
that can still scroll that way - the list itself, a panel around it - then the page, faster the closer the pointer
comes (`ScrollSensitivity`, `ScrollSpeed`), and the items under the pointer are measured again as they pass. A mouse
dragging natively is scrolled by the browser; a keyboard drag scrolls the focused item into view.

For a handle that drags at once on a touch screen, without the long press, give it `touch-action: none` and set
`TouchDelay="0"`:

```razor
<Bortable @bind-Items="_items" TItem="string" Handle=".grip" TouchDelay="0">
    <ItemTemplate Context="item"><span class="grip" style="touch-action: none">⠿</span> @item</ItemTemplate>
</Bortable>
```

## The keyboard

Every list can be sorted without a pointer (`Keyboard`, on by default):

| Key | Not dragging | Dragging |
| --- | --- | --- |
| <kbd>Tab</kbd> | Enters the list at its last focused item; the list is a single tab stop. | Drops the item where it is, and moves on. |
| Arrow keys | Move the focus to the item in that direction. | Move the item to the next place in that direction; past the end of its list, into the nearest list of the group that way. |
| <kbd>Home</kbd> / <kbd>End</kbd> | Focus the first / last item. | Move the item to the start / end of its list. |
| <kbd>Space</kbd> / <kbd>Enter</kbd> | Pick the focused item up. | Drop it. |
| <kbd>Shift</kbd>+<kbd>Space</kbd> | In a `MultiDrag` list, select or deselect the focused item. | - |
| <kbd>Escape</kbd> | - | Put it back where it was. |

The arrows follow the layout - up and down a vertical list, along a row, right to left in an RTL one, row to row in a
grid, from column to column of a board, into a nested list - because the browser layer looks for the next item, or
list, in the direction of the key. A keyboard drag moves the item itself, into another list too, and the focus
follows it; a clone source puts a copy in the other list, which goes away again if it is carried back. In swap
mode the arrows move the swap highlight instead, and the drop trades the two items.

Every rule applies as for a pointer: a position `CanDrop` refuses, a full list, a list of another group are
announced as refused and the item stays put, and `ConfirmDrop` is asked on the drop.

Keys pressed inside an item - in an input, on a button of its own - are left alone: only an item itself, or its
drag handle, answers them. With a `Handle`, make the handle a `<button type="button">` with an accessible name, and
it picks the item up too. `Keyboard="false"` turns it all off, and a `Disabled` list is not focusable.

## Accessibility

A list is a `list` of `listitem`s, each described as a "sortable item" with the keyboard instructions, and named to
assistive technology by `AriaLabel`:

```razor
<Bortable @bind-Items="_cards" TItem="Card" AriaLabel="In progress" ItemLabel="c => c.Title">
```

Each step of a drag is announced through a live region - picked up, moved, dropped, cancelled, refused - with the
item's name (`ItemLabel`, or its `ToString()`), its position and the list it is in: "Picked up Write the spec. It is
at position 2 of 5 in In progress." A pointer drag is announced when it starts and ends, a keyboard drag at every
step, and a move made by code when it lands.

The texts are a `BortableMessages`, to translate or reword through `Messages` - one shared instance for the app, say:

```csharp
public static readonly BortableMessages German = new()
{
    Instructions = "Leertaste oder Eingabe zum Aufnehmen, Pfeiltasten zum Verschieben, Escape zum Abbrechen.",
    RoleDescription = "sortierbares Element",
    PickedUp = a => $"{a.Item} aufgenommen, Position {a.Position} von {a.Count}.",
    Moved = a => $"{a.Item} auf Position {a.Position} von {a.Count} verschoben.",
    Dropped = a => $"{a.Item} auf Position {a.Position} von {a.Count} abgelegt.",
    Canceled = a => $"Abgebrochen. {a.Item} ist wieder auf Position {a.Position}.",
};
```

| Message | When |
| --- | --- |
| `Instructions` | Read with every item (`aria-describedby`). |
| `RoleDescription` | What an item is called in place of "list item" (`aria-roledescription`). |
| `PickedUp` | A drag starts. |
| `Moved` | A keyboard drag, or a move made by code, moves the item. |
| `SwapTarget` | In swap mode, the item the dragged one will trade places with changes. |
| `Dropped` | The drag ends with a drop. |
| `Canceled` | The drag is cancelled, or `ConfirmDrop` refuses it. |
| `Refused` | A position the keyboard asked for is refused. |
| `Locked` | A locked (`Filter`) item is asked to be picked up. |
| `SelectionInstructions` | Added to `Instructions` in a `MultiDrag` list. |
| `Selected` / `Deselected` | Shift+Space selects or deselects an item of a `MultiDrag` list. |

Each takes a `BortableAnnouncement`: `Item`, `Position` and `Count` (from 1), `List`, `OldPosition`, `FromList`, in
swap mode `Target`, and for a selection `Selected`, the number of items selected.

A list's `ChildContent` - an empty-state message, say - is rendered inside the `list`, where only items belong: give
it `role="listitem"` (it is then read as part of the list), or render it next to the list. The drop placeholder and
the preview that follows a finger are pictures of the item: hidden from assistive technology and inert, so a control
the template renders in them can be neither focused nor clicked.

Dragging is never the only way: a keyboard drag covers keyboard and switch users, and `MoveAsync` / `MoveToAsync`
give an app the buttons and menus that WCAG 2.5.7 (dragging movements) asks for and that some screen readers in
browse mode need. The focus outline of an item and of a picked-up item are drawn by the stylesheet, also in forced
colours (Windows high contrast), and the slide animations are skipped when the system asks for reduced motion.

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
| `Input` | `BortableInput` | How the drag was made: `Mouse`, `Touch`, `Pen`, `Keyboard`, or `Api` for a move made by code. |
| `IsClone` | `bool` | `true` when a copy goes into another list and the original stays. |
| `Canceled` | `bool` | On `OnEnd`: the drag kept nothing - cancelled, spilled with `RevertOnSpill`, or refused by `ConfirmDrop`. |

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

A cancelled drag raises `OnEnd` alone, with `Canceled` set (see [cancelling a drag](#cancelling-a-drag)); one
`ConfirmDrop` refused raises `ItemsChanged` for the lists it put back, then `OnEnd` with `Canceled`. With a
`ConfirmDrop`, the completion events wait for its answer.

### Proposed, committed, cancelled

Every drag goes through the same stages, whatever made it:

| Stage | What happens | What you see |
| --- | --- | --- |
| Started | The item is picked up. | `OnStart` |
| Proposed | Each new position is put to `CanDrop` (and `MaxItems`, `Put`, `Pull`...) before it is shown. | `OnChange` for each one shown |
| Released | Dropped on a list, released elsewhere, or cancelled. | - |
| Committed | The pending move, clone or swap is applied to the bound lists. | - |
| Confirmed | The destination's `ConfirmDrop`, if any, keeps it - or puts everything back. | - |
| Reported | Once per changed list, then the completion events. | `ItemsChanged`, `OnUpdate` / `OnRemove` / `OnAdd`, `OnSort`, `OnEnd` |

A drag is finished exactly once: a drop followed by the browser's `dragend`, or a list that leaves the page as the
drag ends, never commits it twice.

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
| `--bit-bortable-focus-outline` | `2px solid` accent at 90% | The outline of an item focused from the keyboard. |
| `--bit-bortable-focus-offset` | `2px` | How far that outline sits outside the item. |
| `--bit-bortable-lifted-outline-style` | `dashed` | The outline style of an item picked up from the keyboard. |
| `--bit-bortable-drag-opacity` | `0.92` | The preview that follows a finger, a pen or a `ForceFallback` mouse. |
| `--bit-bortable-drag-shadow` | `0 10px 24px rgba(0, 0, 0, 0.18), 0 2px 6px rgba(0, 0, 0, 0.12)` | That preview's shadow. |
| `--bit-bortable-accepts-outline` | `none` | During a drag, the outline of every list that takes it. |
| `--bit-bortable-over-outline` | `none` | The outline of the list under the pointer. |
| `--bit-bortable-refused-outline` | `none` | The outline of that list when it refused the position. |
| `--bit-bortable-target-offset` | `2px` | How far those outlines sit outside the list. |

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
| `bit-bortable-instructions` | The hidden keyboard instructions every item refers to. |
| `bit-bortable-drag` (`DragClass`) | The preview that follows a finger, a pen or a `ForceFallback` mouse. |

`GhostClass`, `ChosenClass`, `SwapClass` and `SelectedClass` replace the class used for their state, for a list
that wants its own look instead of the stylesheet's.

### Valid and invalid drop targets

While a drag runs, the lists carry attributes the app can style - set by the browser layer, so they cost no render:

| Attribute | On |
| --- | --- |
| `data-bortable-accepts` | Every list that takes the drag. |
| `data-bortable-over` | The list under the pointer (from the keyboard: the list the item is in). |
| `data-bortable-refused` | That list, when it refused the last position (`CanDrop`). |
| `data-bortable-dragging` | The page's `<html>`, set to `native`, `pointer` or `keyboard`. |

The stylesheet draws nothing for them until you set its custom properties - a board that shows where a card may go:

```css
.board { --bit-bortable-accepts-outline: 2px dashed #94a3b8; --bit-bortable-over-outline: 2px solid #2563eb;
         --bit-bortable-refused-outline: 2px solid #dc2626; }
```

A mouse dragging natively also shows the cursor: a copy over another list of a clone drag, no drop over a refused
position.

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

Register the services in `Program.cs` and link the stylesheet from `App.razor`. The pointer is followed and
measured in the browser, and the circuit carries only what changes: the start of a drag, each new drop position,
and its end. While one call is on its way the browser keeps measuring, and sends the latest position once the call
returns, so the last position is always acted on; the preview trails the pointer by the network's latency only when
it moves to a new position. The coordinator is scoped to the circuit, so users never see each other's drags.

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

### The browser routes, .NET decides

Each item wrapper is `draggable` and carries a key of its own (`data-bortable-key`). Every list registers with the
browser layer after its first interactive render, handing it an object to call it through and the options the hit
test needs. From then on:

1. On `dragstart` the browser layer names the item, by its key, to its list. The list records the drag on the
   scoped coordinator and answers with the lists that take it - its own, and every list that could receive the item
   (or trade an item for it, in swap mode).
2. On each `dragover` the browser layer finds the innermost of those lists under the pointer - a nested list that
   does not take the drag leaves it to the list around it - and hit-tests it. Only a new drop position is sent to
   .NET; while a call is on its way, the latest position waits for it and is sent once it returns.
3. The list answers whether anything changed - or that it refused the position, which the pointer then shows.
4. On `drop`, or on `dragend` when the item was released elsewhere, the browser layer tells the list how the drag
   ended.

The calls are serialized through the coordinator, and whichever of `drop` and `dragend` comes first finishes the
drag. An item is named by its key, never by an index the page may have moved on from.

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

Right before a call that may change the lists, the browser layer records the position of every item of the lists
the drag has touched; as soon as Blazor's update of a list arrives - before the browser paints it - each item is
moved back to where it was with a transform and transitioned to its new place. A change .NET makes on its own (a
move made by code, a refused drop sliding back) is recorded and played the same way, around the render. With the
system's reduced-motion setting on, nothing slides.

### The browser layer

`bit-bortable.js` routes the input to the lists that take the drag, marks the lists' own drags and allows their
drop, measures the pointer against the items for the swap thresholds, decides on `pointerdown` whether a drag
handle lets an item be dragged, refuses a drop into the dragged item's own subtree, and plays the animations. It
keeps no copy of the lists and decides nothing about them.

## Performance

What a drag costs, step by step:

- **The pointer**: followed and measured in the browser. A step that does not change the drop position - the
  pointer resting, moving within an item - never reaches .NET. A step that does is one call, and while it is on its
  way the browser keeps the latest position for when it returns, so no position is lost and none queues up.
- **A new position**: the list changes its bound list in place and renders once; the browser snapshots the items
  before the call and slides them when the render lands, without another round trip.
- **The render**: a list renders all its items, so a step of a drag through a list of a thousand items with a heavy
  template renders a thousand templates. `IsolateItems="true"` puts each template in a component of its own that a
  step skips - only the wrappers move - and renders again when the item changes or the page renders the list. Leave
  it off when a template shows something other items' moves change, such as an item's position number: that would
  only catch up when the drag ends.

The browser layer counts its own work: in the browser's console,
`(await import('/_content/Bit.Bortable/bit-bortable.js')).diagnostics()` returns the drags started, the positions
sent to .NET (how many changed a list or were refused), the keyboard steps, the ends, and the average and slowest
round trip of a position in milliseconds; `resetDiagnostics()` starts the count again.

A list renders every item it is given: virtualizing a long list (Blazor's `Virtualize`) is not supported, as the
items that are not rendered could be neither measured nor dropped beside. Page a list that is too long to render, or
let a filter narrow it.

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
| `CanDrop` | `Func<BortableEventArgs<TItem>, bool>?` | `null` | Whether the item may take a position here; asked before it is shown (SortableJS `onMove`). |
| `MaxItems` | `int?` | `null` | The most items the list holds; a full list takes nothing from other lists. |
| `ConfirmDrop` | `Func<BortableEventArgs<TItem>, Task<bool>>?` | `null` | Asked after a drop here whether to keep it; `false` puts everything back. |
| `RevertOnSpill` | `bool` | `false` | A drag from this list released outside every list is put back (SortableJS `revertOnSpill`). |
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
| `SelectedItems` | `IReadOnlyCollection<TItem>?` | `null` | The multi-drag selection, to read or set; bind with `@bind-SelectedItems`. |
| `SelectedItemsChanged` | `EventCallback<IReadOnlyCollection<TItem>>` | | Raised with the selection, in list order, when it changes. |
| `IsolateItems` | `bool` | `false` | Renders each item's template in a component of its own, skipped on the steps of a drag. See [performance](#performance). |
| `Delay` | `int` | `0` | Milliseconds a mouse or pen press must be held before a drag starts (SortableJS `delay`). |
| `TouchDelay` | `int` | `200` | Milliseconds a finger must rest on an item before it drags it (a long press); a swipe scrolls instead. |
| `DelayTolerance` | `int` | `5` | Pixels the pointer may move while a delay runs before the press counts as a scroll or a click. |
| `ForceFallback` | `bool` | `false` | The mouse drags like a finger: with pointer events and a preview, not the browser's drag (SortableJS `forceFallback`). |
| `DragClass` | `string` | `bit-bortable-drag` | Class of the preview that follows a finger, a pen, or a `ForceFallback` mouse. |
| `Scroll` | `bool` | `true` | A finger, pen or `ForceFallback` drag near an edge scrolls the list, the panels around it, or the page. |
| `ScrollSensitivity` | `int` | `40` | How close (px) to an edge the pointer must come to scroll it. |
| `ScrollSpeed` | `int` | `16` | The most pixels an edge scrolls per frame, at the very edge. |
| `Keyboard` | `bool` | `true` | Items can be picked up, moved and dropped from the keyboard. See [the keyboard](#the-keyboard). |
| `AriaLabel` | `string?` | `null` | The list's accessible name, which announcements name it by. |
| `ItemLabel` | `Func<TItem, string>?` | `null` | How announcements name an item; the item's `ToString()` without it. |
| `Messages` | `BortableMessages?` | `null` | The instructions and announcements, to translate or reword; `BortableMessages.Default` without it. |
| `AdditionalAttributes` | `IReadOnlyDictionary<string, object>?` | `null` | Any other attribute, rendered on the list's container. |
| `OnStart`, `OnEnd`, `OnAdd`, `OnRemove`, `OnUpdate`, `OnSort`, `OnChange` | `EventCallback<BortableEventArgs<TItem>>` | | See [events](#events). |

### Bortable methods

| Method | Description |
| --- | --- |
| `MoveAsync(TItem item, int newIndex)` | Moves the item within the list as a drag would; `true` when it moved. See [moving items from code](#moving-items-from-code). |
| `MoveToAsync(TItem item, string listId, int newIndex)` | Moves the item into the list with that `Id` as a drag would; `true` when it moved. |

### BortableEventArgs

See [event arguments](#event-arguments).

### BortableMessages

See [accessibility](#accessibility). `BortableMessages.Default` holds the English texts every list uses unless it is
given its own.

### BortableInput

| Value | Meaning |
| --- | --- |
| `Mouse` | A mouse or a trackpad. |
| `Touch` | A finger on a touch screen. |
| `Pen` | A pen or stylus. |
| `Keyboard` | Keys: the item was picked up, moved and dropped from the keyboard. |
| `Api` | Code: `MoveAsync` or `MoveToAsync`. |

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
| `AddBitBortableServices(this IServiceCollection, Action<BortableOptions>)` | The same, with [app-wide defaults](#app-wide-defaults) for every list. |

### BortableOptions

The defaults every list takes for the parameters it does not set itself; every property is nullable, and `null`
leaves the parameter's own default. See [app-wide defaults](#app-wide-defaults).

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
| `onMove` (return `false`) | `CanDrop` (return `false`) |
| OnSpill plugin: `revertOnSpill` | `RevertOnSpill` |
| `group: { put: (to) => ... }` with a count | `MaxItems` |
| `toArray()`, `store` | Not needed: the bound `IList<TItem>` is the state |
| `sort(order)` | `MoveAsync`, `MoveToAsync` - one item at a time, as a drag would move it |
| `forceFallback`, `dragClass` | `ForceFallback`, `DragClass` (touch and pen always drag this way) |
| `delay`, `delayOnTouchOnly`, `touchStartThreshold` | `Delay` (mouse and pen), `TouchDelay` (touch), `DelayTolerance` |
| AutoScroll plugin: `scroll`, `scrollSensitivity`, `scrollSpeed` | `Scroll`, `ScrollSensitivity`, `ScrollSpeed` |
| MultiDrag: Shift-click range selection | Shift+click, and `@bind-SelectedItems` |
| `easing`, `fallbackOnBody`, `fallbackTolerance`, `onClone`, `onChoose`, `onFilter`, `multiDragKey`, `removeOnSpill` | Not supported |
| No keyboard support | The keyboard, and screen reader announcements: see [the keyboard](#the-keyboard) |

## Limitations

- Items must be unique within a list, as Blazor's `@key` requires. An item is never dropped into a list that
  already holds an equal one, so a value-equal duplicate (the same string twice) is refused rather than breaking
  rendering. For clone mode, use a reference type and a `Clone` factory that returns a distinct object.
- An item moving between lists with the pointer is committed when the drag ends, with a live placeholder preview
  meanwhile, rather than physically moving between lists mid-drag. (A keyboard drag moves the item itself.)
- A mouse drags with the browser's own HTML5 drag and drop, with its drag image and auto-scrolling, unless the list
  has `ForceFallback`; a finger and a pen always drag with pointer events.
- Lists are not virtualized: every item is rendered (see [performance](#performance)).
- Custom easing and the `toArray`/`store` helpers are not implemented.
- The lists that take a drag are settled when it starts: a list that appears mid-drag, or one whose `Put`, `Group`
  or `MaxItems` changes mid-drag, is not offered the item until the next drag. `CanDrop` is asked anew at every
  position.

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
