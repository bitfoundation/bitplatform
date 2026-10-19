# Bit.Bink engine benchmarks

A [BenchmarkDotNet](https://benchmarkdotnet.org) console harness for the parts of the Bink engine that decide
whether a drawing stays interactive: the per-frame input path, stroke geometry, redraw recording, hit testing,
saving/loading/export, the pure .NET rasterizer and undo/redo.

Synthetic documents are shaped like handwriting (many short curved strokes with a pressure envelope, spread over
a 1600 by 1000 page) rather than convenient straight lines, so the numbers reflect real documents.

## What it measures

| Class | What it covers |
|-------|----------------|
| `InputBenchmarks` | Processing one animation frame of pointer samples (4, 16 and 64 samples) and decoding the packed samples that arrive from the browser. This is the hot path while drawing and must not allocate. |
| `StrokeGenerationBenchmarks` | Turning stored points into renderable geometry: a 60-point handwriting stroke (variable-width outline and constant-width centerline), a continuous 1,000/10,000-point stroke, and the simplification that runs when a stroke is committed. Runs once per stroke, not once per frame. |
| `RenderBenchmarks` | Recording a redraw of a 500/5,000-stroke document into the canvas command buffer, for the whole document and for a 900 by 600 viewport (culling). |
| `QueryBenchmarks` | One click with the selection tool (hit test) and a marquee selection over a quarter of the page, on 1,000/10,000-stroke documents. |
| `SerializationBenchmarks` | Saving and loading 1,000/10,000-stroke documents, encoding/decoding 10,000 points and SVG export. |
| `RasterBenchmarks` | The built-in .NET rasterizer (used on servers and in tests) on 200/2,000-stroke documents: rendering to pixels alone, and full PNG export including compression. |
| `HistoryBenchmarks` | Adding 100 strokes and undoing all of them, the cost of a burst of edits; mutation and undo must not copy the document. |

## Running it

```bash
# From the repository's src folder. Release is required - a Debug build's numbers are meaningless.
dotnet run -c Release --project Bink/Tests/Bit.Bink.Benchmarks

# A single set, selected with BenchmarkDotNet's glob filter:
dotnet run -c Release --project Bink/Tests/Bit.Bink.Benchmarks -- --filter "*Render*"

# A quick smoke run (one iteration, in-process, numbers not meaningful):
dotnet run -c Release --project Bink/Tests/Bit.Bink.Benchmarks -- --filter "*InputBenchmarks*" --job dry --inProcess
```

Run with no `--filter` to get an interactive picker. Results (Markdown, CSV and HTML reports) are written to
`BenchmarkDotNet.Artifacts` in the current directory.

## Indicative results

Captured on .NET 10, x64, a desktop machine. Sizes are binary: MB means 2^20 bytes, as BenchmarkDotNet reports
it. Absolute numbers depend on hardware; the ratios and the allocation figures are the meaningful part, so re-run
locally for your own hardware.

### Input, the per-frame hot path

| Samples per frame | Time | Allocated |
| ---: | ---: | ---: |
| 4 | 81 ns | 0 B |
| 16 | 299 ns | 0 B |
| 64 | 1.24 us | 0 B |

Sixty-four samples is a 240 Hz pen with coalescing on a slow frame. Processing them costs about one hundredth of a
16 ms frame budget and allocates nothing, so drawing produces no garbage collection pressure.

### Stroke geometry

| Case | Time | Allocated |
| --- | ---: | ---: |
| Handwriting stroke, 60 points, variable width | 3.3 us | 2.2 KB |
| Handwriting stroke, 60 points, centerline | 0.7 us | 1.6 KB |
| Continuous stroke, 10,000 points, variable width | 534 us | 333 KB |

### Redraw

Recording a redraw with a 900 by 600 viewport:

| Strokes | Whole document | Visible region only |
| ---: | ---: | ---: |
| 500 | 293 us | 141 us |
| 5,000 | 7.8 ms | 3.6 ms |

Allocation is under 200 bytes either way, because the command buffer is reused between frames.

### Saving, loading and export

Document of 10,000 strokes, 600,000 points:

| Operation | Time | Allocated |
| --- | ---: | ---: |
| Save | 28 ms | 22 MB |
| Load | 72 ms | 22 MB |
| Encode 10,000 points | 81 us | 101 KB |
| Decode 10,000 points | 55 us | 156 KB |
| Export SVG | 569 ms | 146 MB |

SVG export is the expensive one, and inherently so: 600,000 points become about 35 million characters of path
data. Most of that memory is the string itself, twice over, because `Export` returns one; the streaming overload
avoids the second copy for documents this size.
