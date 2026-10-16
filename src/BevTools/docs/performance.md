# Performance and overhead

BevTools reports its own cost in the **About** panel of every session (bookkeeping per application render, UI render
time, inspection, diagnostics, tree refresh). This page gives the reproducible numbers behind that, so the cost is a
measured fact rather than a claim.

## Reproducing

```
dotnet run -c Release --project src/BevTools/Tests/Bit.BevTools.Benchmarks
```

The harness drives a real `Renderer` with a component that re-renders 21 components per batch, 20 000 times. Every
configuration is measured twice and the second result is kept, so the first configuration does not pay for JIT
tiering on behalf of the others.

## Measured (net10.0, Release, x64 desktop)

| Configuration | Time per batch of 21 components | Overhead | Allocated |
|---|---|---|---|
| Without BevTools | 4.24 µs | — | 23.9 MB |
| BevTools referenced, `Enabled = false` | 4.34 µs | +2 % | 23.9 MB |
| Recording, `RecordRenderEvents = false` | 11.73 µs | +177 % | 129 MB |
| Recording everything | 12.48 µs | +194 % | 195 MB |

Read these carefully:

* **A disabled BevTools costs nothing.** No activator, no handler, no logger provider is registered, so a production
  build that leaves the package referenced pays about 2 % — inside the noise of the measurement.
* **The percentages are a worst case.** The benchmark component does almost no work (`<span>` with two contents), so
  the fixed per-render bookkeeping dominates. In absolute terms BevTools adds roughly **0.4 µs and ~0.4 KB per
  component render**; a real component that formats text, evaluates conditions or renders children costs far more
  than that, and the relative overhead falls accordingly. The About panel in the demo reports 10–40 µs per *batch*.
* **The cost is one object graph per render**, by design: one `BevRenderSample` (kept in the component's bounded render
  history) plus one timeline event (kept in the bounded timeline). Nothing is formatted into a string on the hot
  path — titles are pre-built per component, cause descriptions come from a cache, and the timeline event's data bag
  is projected from the sample instead of copied into a dictionary.

## Levers for large applications

| Option | Effect |
|---|---|
| `RecordRenderEvents = false` | Drops the per-render timeline rows; keeps counts, durations, causes and the profiler. Saves about a third of the allocations. |
| `MaxEvents`, `MaxRendersPerComponent`, `MaxTrackedComponents` | Bound memory. Every buffer is a ring buffer; nothing grows without a limit. |
| `CaptureComponentStateOnRender = false` (default) | Field and parameter snapshots and diffs are taken only for the component selected in the inspector. |
| `TrackJsInterop = false`, `TrackHttp = false` | Removes the JS runtime wrapper / HTTP handler entirely. |
| `UseFrameworkInstrumentation = false` | Stops BevTools registering the framework's ActivitySource and meters. Render causes degrade to *StateHasChanged*. |
| Pause capture (header button, `session.PauseCapture()`) | Stops all recording at runtime without uninstalling anything: the render wrapper degrades to one flag check and one counter increment, and every other path returns before allocating. |

Forced-render detection (parameter flattening and diffing before a render) runs for the component selected in the
inspector, like the field diff it sits next to, and is bounded the same way (depth 2, 25 items, 150 entries per
parameter, one 50 ms wall-clock budget shared by all parameters of the render). It is not on the hot path for any other
component unless `CaptureComponentStateOnRender` is on, which applies both diffs to every component on every render.

## Where the remaining cost is

Ordered by measured contribution:

1. **Per-render bookkeeping** (sample + timeline event). Bounded and explained above.
2. **Diagnostics evaluation**, at most once per second and only while the panel is open. It scans the timeline
   snapshot; on a busy session this is the largest single slice of BevTools CPU time in the About panel.
3. **Tree refresh**, at most once per second (and once per second of component churn even when the panel is closed,
   so that disposed components are released promptly).
4. **State inspection**, only for the selected component, bounded by depth, item count and a 50 ms wall-clock budget.

## Things that are deliberately *not* optimised away

* The render fragment wrapper is installed for every `ComponentBase`, including ones that never render. Skipping it
  conditionally would make render counts wrong for exactly the components a developer is hunting.
* Component records are kept for 60 s after disposal so the timeline and errors can still name them.
