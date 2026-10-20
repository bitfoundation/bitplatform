# Bit.Bortable

A Blazor-native drag-and-drop sortable list, inspired by SortableJS: reorder the items of a list, or drag them
between lists that share a group. The sorting engine is written in C# and runs on the browser's own HTML5 drag
events; a small JavaScript module only measures the pointer against the items, installs drag handles and plays the
slide animations - there is no JavaScript sortable library underneath.

## Quick start

```bash
dotnet add package Bit.Bortable
```

```csharp
// Program.cs (both projects of a Blazor Web App)
builder.Services.AddBitBortableServices();
```

```razor
@* _Imports.razor *@
@using Bit.Bortable
```

Link the stylesheet from your host page (`App.razor` or `index.html`); it is not injected for you. The script
module, `_content/Bit.Bortable/bit-bortable.js`, is imported by the component on demand.

```html
<link rel="stylesheet" href="_content/Bit.Bortable/bit-bortable.css" />
```

```razor
<Bortable @bind-Items="_todo" TItem="string" Group="board">
    <ItemTemplate Context="item">@item</ItemTemplate>
</Bortable>

<Bortable @bind-Items="_done" TItem="string" Group="board" OnAdd='e => Console.WriteLine($"done: {e.Item}")'>
    <ItemTemplate Context="item">@item</ItemTemplate>
</Bortable>

@code {
    // Changed in place, and ItemsChanged is raised once per drag for each list that changed.
    private IList<string> _todo = new List<string> { "Write the spec", "Review the PR" };
    private IList<string> _done = new List<string> { "Set up CI" };
}
```

## Features

- Live reordering while dragging, with FLIP slide animations.
- Lists that share a `Group` exchange items, with a placeholder previewing where the item will land.
- `Move`, `Clone` and `None` pull modes, and per-list `Put`, `Sort` and `Disabled`.
- The SortableJS swap thresholds: `SwapThreshold`, `InvertSwap`, `InvertedSwapThreshold` and `Direction`.
- Swap mode (the Swap plugin) and multi-drag (the MultiDrag plugin).
- Drag handles, filters for locked items, and per-item classes that follow an item between lists.
- Vertical lists, horizontal strips, grids and nested lists.
- `@bind-Items` and the SortableJS lifecycle events: `OnStart`, `OnEnd`, `OnAdd`, `OnRemove`, `OnUpdate`, `OnSort`
  and `OnChange`.
- Theming of the drag states through `--bit-bortable-*` CSS custom properties.
- Blazor WebAssembly, Server, Web App (including prerendering) and Hybrid on .NET 8, 9 and 10.

## For AI agents

The demo site hosts an MCP server at `https://bortable.bitplatform.dev/mcp` (also served through
`https://bitplatform.dev/mcp`) that answers from the shipped library: its API, its guide, its stylesheet and the
demo's working pages, and it can run a drag over lists you describe and report what it did.

## Documentation

The full guide - sorting, shared lists and cloning, nested lists, handles and filters, swap mode, multi-drag, events,
styling, hosting models, how it works and a SortableJS mapping - is in the
[Bortable README](https://github.com/bitfoundation/bitplatform/tree/develop/src/Utilities/Bortable). See it running at
[bortable.bitplatform.dev](https://bortable.bitplatform.dev).
