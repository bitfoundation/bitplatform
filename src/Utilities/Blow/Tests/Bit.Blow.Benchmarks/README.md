# Bit.Blow benchmarks

A [BenchmarkDotNet](https://benchmarkdotnet.org) console harness for the parts of Blow whose cost grows with the
graph: the C# side of a canvas render (what every frame of a pan, a zoom or a drag costs), and the helpers that
run once per user action - auto-layout, edge geometry, recording an undo step and pasting.

The graphs are shaped like real flows rather than convenient ones: a grid of measured 150 by 40 nodes, each wired
to its right-hand neighbour and every third to the node below, about 1.3 edges per node - the density of a
workflow or data pipeline.

## What it measures

| Class | What it covers |
|-------|----------------|
| `RenderBenchmarks` | One frame of a pan, a zoom and a single-node drag on a 200- and a 1,000-node graph. The canvas is hosted in `HeadlessRenderer`, Blazor's own renderer with the browser taken away: it builds and diffs render trees exactly as the WebAssembly and Server renderers do, then drops the batch. Each run also prints how many components the last frame rendered. |
| `GraphBenchmarks` | Layered auto-layout, building every edge's SVG path, recording one undo step after a node moved (the canvas's built-in history does this at the end of every drag), and pasting half the graph, on 1,000 and 5,000 nodes. |
| `LayoutBenchmarks` | The tree, radial and force layouts on 200, 1,000 and 5,000 nodes (force, which is quadratic, up to 1,000). |
| `RoutingBenchmarks` | One `Routed` edge across a block of 10, 40 and 100 nodes, routed round them - the cost per routed edge per frame of a drag. |

The browser's own work - layout, paint, the interop round trips of measurement - is not here; the E2E suite's
stress scenario covers that end to end.

bUnit is deliberately not used to host the render benchmarks: its renderer regenerates the HTML of every parent of
each component that renders, which is quadratic in the number of nodes and swamps what is being measured.

## Running it

```bash
# From src/Utilities/Blow. Release is required - a Debug build's numbers are meaningless.
dotnet run -c Release --project Tests/Bit.Blow.Benchmarks

# A single set, selected with BenchmarkDotNet's glob filter:
dotnet run -c Release --project Tests/Bit.Blow.Benchmarks -- --filter "*RenderBenchmarks*"

# A quick run with fewer iterations (the numbers below were taken this way):
dotnet run -c Release --project Tests/Bit.Blow.Benchmarks -- --filter "*" --job short
```

Results (Markdown, CSV and HTML reports) are written to `BenchmarkDotNet.Artifacts` in the current directory.

## Results

.NET 10, x64, a desktop machine, `--job short`. Absolute numbers depend on the hardware; compare rows with each
other. WebAssembly runs the same code several times slower than this, so a render that costs a few milliseconds
here is a dropped frame in the browser.

### Baseline (round 1, before any optimisation)

| Benchmark | 200 nodes | 1,000 nodes |
| --- | ---: | ---: |
| Pan frame | 1.18 ms, 1.4 MB, 1,250 components rendered | 13.7 ms, 7.3 MB, 6,293 components rendered |
| Zoom frame | 1.18 ms, 1.4 MB | 14.1 ms, 7.3 MB |
| Drag frame (one node) | 1.21 ms, 1.4 MB | 14.2 ms, 7.3 MB |

A pan moves nothing but the viewport transform, yet every node, edge and handle component re-rendered: that is
what the component counts above say, and what round 3 set out to remove.

| Benchmark | 1,000 nodes | 5,000 nodes |
| --- | ---: | ---: |
| Layered layout | 135 us, 372 KB | 1.11 ms, 1.8 MB |
| Every edge path | 629 us, 1.1 MB | 3.12 ms, 5.5 MB |
| Record an undo step after one node moved | 1.00 ms, 654 KB | 7.70 ms, 3.3 MB |
| Paste half the graph | 93 us, 536 KB | 1.62 ms, 2.6 MB |

Recording an undo step deep-copies the whole graph: 3.3 MB per step at 5,000 nodes, so a full 100-step history
holds about 330 MB.

### After render scoping (round 3) and the render gate (round 4)

| Benchmark | 200 nodes | 1,000 nodes |
| --- | ---: | ---: |
| Pan frame | 5.2 us, 12 KB, 7 components rendered | 10.9 us, 45 KB, 7 components rendered |
| Zoom frame | 5.1 us, 12 KB | 11.0 us, 45 KB |
| Drag frame (one node) | 110 us, 35 KB, 14 components rendered | 573 us, 125 KB, 14 components rendered |

A pan or zoom now renders the canvas, the viewport layer and the add-ons that follow the viewport, and nothing in
the node or edge layers: the layer gates compare a version number and stop there. A drag frame renders the dragged
node and the edges attached to it; what is left grows with the graph because the canvas still walks its node and
edge lists once per frame.

### After structurally shared history (round 5)

| Benchmark | 1,000 nodes | 5,000 nodes |
| --- | ---: | ---: |
| Record an undo step after one node moved | 30 us, 18 KB | 172 us, 91 KB |

Each element is compared with its copy in the previous step and only what changed is copied, so a step costs the
list of references plus the changed elements: a full 100-step history at 5,000 nodes holds about 9 MB instead of
330 MB.

### Tree, radial and force layouts (round 7)

| Benchmark | 200 nodes | 1,000 nodes | 5,000 nodes |
| --- | ---: | ---: | ---: |
| Tree layout | 12 us, 61 KB | 69 us, 299 KB | 0.88 ms, 1.5 MB |
| Radial layout | 14 us, 67 KB | 83 us, 328 KB | 1.03 ms, 1.6 MB |
| Force layout (300 iterations) | 9.1 ms, 49 KB | 217 ms, 243 KB | not run |

Tree and radial are linear. Force compares every pair of nodes on every iteration: comfortable at a few hundred nodes,
a noticeable pause at a thousand (several times longer on WebAssembly), which is why its documentation points large
graphs at the linear layouts or at fewer iterations.

### Edge routing (round 8)

| Benchmark | 10 obstacles | 40 obstacles | 100 obstacles |
| --- | ---: | ---: | ---: |
| Route one edge across the block | 4.7 us, 16 KB | 11.4 us, 31 KB | 28 us, 57 KB |

The setup checks that a route is found, so the numbers are of a search that succeeded. The route here runs round the
outside of the block; one that has to thread between many obstacles explores more of the grid and costs more.
