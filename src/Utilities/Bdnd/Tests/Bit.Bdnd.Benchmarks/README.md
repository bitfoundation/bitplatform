# Bit.Bdnd benchmarks

A [BenchmarkDotNet](https://benchmarkdotnet.org) console harness for the .NET side of a drag: what the calls the
browser module makes during a drag cost in a list of hundreds or thousands of rows, and the arithmetic of a drop.
On Blazor WebAssembly this runs in the tab; on Blazor Server it runs on the server, for every connected user.

The browser's own work - hit testing, the preview, layout and paint, the interop round trips - is not here: the E2E
suite's `PerfTests` measure frames, long tasks and the circuit messages of a drag end to end.

## What it measures

| Class | What it covers |
| --- | --- |
| `SortableListBenchmarks` | A list of 100 and 1,000 rows, each a `BdndDropTarget` with `Edges` around a `BdndDraggable` (the README's drop-indicator recipe), hosted in `HeadlessRenderer` - Blazor's own renderer with the browser taken away, which builds and diffs render trees as the WebAssembly and Server renderers do and drops the batch. It measures a pointer drag starting and ending, the hovered row's edge changing, the hover moving on to the next row, and an arrow key of a keyboard drag (the candidates collected and sent; the stand-in module answers nowhere). Each run prints how many components the last operation rendered. |
| `ListBenchmarks` | `BdndList.PlanMove` with `Apply` and `Revert` - one item, and a selection of one item in a hundred - and `BdndTree.Contains` walking a whole tree, on 1,000 and 10,000 items. |

bUnit is not used to host the render benchmarks: its renderer regenerates the HTML of every parent of each
component that renders, which would swamp what is being measured.

## Running it

```bash
# From src/Utilities/Bdnd. Release is required - a Debug build's numbers are meaningless.
dotnet run -c Release --project Tests/Bit.Bdnd.Benchmarks -- --filter "*" --inProcess

# A single set, selected with BenchmarkDotNet's glob filter:
dotnet run -c Release --project Tests/Bit.Bdnd.Benchmarks -- --filter "*SortableList*" --inProcess

# A quick run with fewer iterations (the numbers below were taken this way):
dotnet run -c Release --project Tests/Bit.Bdnd.Benchmarks -- --filter "*" --job short --inProcess
```

`--inProcess` is needed on .NET 11: BenchmarkDotNet 0.15.8 does not recognize the runtime for its out-of-process
toolchain ("GetRuntimeVersion not implemented for NotRecognized"). Results (Markdown, CSV and HTML reports) are
written to `BenchmarkDotNet.Artifacts` in the current directory.

## Results

Windows 11, .NET 11, `--job short --inProcess` (3 iterations: read the means as orders of magnitude).

| Benchmark | 100 rows | 1,000 rows | Components rendered |
| --- | ---: | ---: | --- |
| `DragStartAndEnd` | 2.2 ms | 20.6 ms | 8 per row (4 at the start, 4 at the end) |
| `EdgeChange` | 75 µs | 200 µs | 4: the hovered row only |
| `HoverMove` | 160 µs | 840 µs | 8: the row left and the row entered |
| `KeyboardMove` | 41 µs | 200 µs (median) | 0 (the stand-in module answers nowhere) |

| Benchmark | 1,000 items | 10,000 items |
| --- | ---: | ---: |
| `MoveSelectionAndUndo` | 1.9 µs | 18 µs |
| `MoveOneAndUndo` | 0.15 µs | 0.54 µs |
| `TreeContains` | 0.24 µs | 0.99 µs |

What the numbers say:

- **Edges and hover stay local at any size.** An edge change renders the one row it belongs to, and a hover moving
  on renders the two rows involved, whether the list has 100 rows or 1,000; what grows with the list is the
  manager's notification reaching every row, each of which compares what it collected and does nothing.
- **The start and the end of a drag render every row that can take the item**: its `CanDrop` changes, so the target
  renders, and with it the draggable in its content (a `RenderFragment` parameter always re-renders its component).
  That is about 10 µs a row at each end of a drag, once - not per pointer move. A list of thousands of rows that
  drags often is what `Virtualize` is for: only the rendered rows are components.
- **A keyboard move** collects every target the item can go to, so it grows with the list; at 1,000 rows it is a
  fifth of a millisecond, per key.
- **Moving a selection** was 113 µs on 10,000 items before this harness existed: a `List<T>` took the selection out
  and put it back one item at a time, shifting the rest of the list once per item. `BdndListMove<T>` now moves a
  `List<T>` in one pass (18 µs); other `IList<T>` implementations still go one item at a time, so their change
  notifications stay one per item.
