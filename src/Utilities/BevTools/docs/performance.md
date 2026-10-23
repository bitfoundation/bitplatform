# Performance and overhead

BevTools reports its own cost in the **About** panel of every session (bookkeeping per application render, UI render
time, inspection, diagnostics, tree refresh). This page gives the reproducible numbers behind that, so the cost is a
measured fact rather than a claim.

## Reproducing

```
dotnet run -c Release --project src/Utilities/BevTools/Tests/Bit.BevTools.Benchmarks
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

**Re-measured on 2026-10-23, after the ten improvement rounds** (`docs/progress.md`), on the same machine under other
load: the code before round 1 (commit `0ee277bf0e`) and after round 10, back to back, twice each. Allocations are
identical - 23.9 MB, 23.9 MB, 142.0 MB and 209.1 MB for the four configurations - so the rounds added nothing to the
render path (the per-component change is one type check when a component is instrumented, for `EditForm`). Both are
above the table (129 MB and 195 MB): the table predates later changes before this work. Times could not be compared on
that machine: *without BevTools* alone measured between 9.0 and 13.7 µs per batch across runs, two to three times the
table, and the two builds overlapped within that spread. The table keeps the earlier, steadier measurement.
Re-measured after round 15 on the same machine: allocations again 23.9, 23.9, 142.0 and 209.1 MB - the HTTP senders,
the profiler's time axis and renders per page, and the CI check are off the render path - and times as noisy as
before (9.08 µs per batch without BevTools).

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
| `MaxEvents`, `MaxRendersPerComponent`, `MaxTrackedComponents`, `MaxFindings` | Bound memory. Every buffer is a ring buffer; nothing grows without a limit. |
| `CaptureComponentStateOnRender = false` (default) | Field and parameter snapshots and diffs are taken only for the component selected in the inspector. |
| `TrackJsInterop = false`, `TrackHttp = false` | Removes the JS runtime wrapper / HTTP handler entirely. |
| `TrackWebSockets = false` | Leaves `WebSocket` and its `send` untouched in the browser. |
| `ValueAccess = Shallow` or `None` | Reads less on inspection, and skips the parameter walk of forced-render detection altogether. |
| `UseFrameworkInstrumentation = false` | Stops BevTools registering the framework's ActivitySource and meters. Render causes degrade to *StateHasChanged*. |
| Pause capture (header button, `session.PauseCapture()`) | Stops all recording at runtime without uninstalling anything: the render wrapper degrades to one flag check and one counter increment, and every other path returns before allocating. |

Forced-render detection (parameter flattening and diffing before a render) runs for the component selected in the
inspector, like the field diff it sits next to, and is bounded the same way (depth 2, 25 items, 150 entries per
parameter, one 50 ms wall-clock budget shared by all parameters of the render). It is not on the hot path for any other
component unless `CaptureComponentStateOnRender` is on, which applies both diffs to every component on every render.

## Where the remaining cost is

Ordered by measured contribution:

1. **Per-render bookkeeping** (sample + timeline event). Bounded and explained above.
2. **Diagnostics evaluation**, at most once per second and only while the panel is open - plus once after the last
   change, so a burst that ended within the second is still evaluated, and once when a session is exported with
   something captured since. `Diagnostics.EvaluateWhileClosed` (off by default) runs it at the same rate with the
   panel closed, for a run whose export is checked in CI. It scans the timeline snapshot; on a busy session this is the largest single slice of
   BevTools CPU time in the About panel.
3. **Tree refresh**, at most once per second (and once per second of component churn even when the panel is closed,
   so that disposed components are released promptly).
4. **State inspection**, only for the selected component, bounded by depth, item count and a 50 ms wall-clock budget.

## Costs off the render path

None of these run per render; each is bounded and runs only when asked or at a fixed cadence.

* **Event handlers and callbacks**: one copy of the inspected component's current frames, when the inspector reads it.
* **Render mode**: one renderer lookup per component, on its first render.
* **Routing, runtime facts**: read when their panel (or a snapshot) asks.
* **WebSocket traffic**: in the browser, per frame a size count and an envelope decode that skips every argument;
  reports to .NET at most every 2 s and only when application traffic changed (BevTools' own reports never trigger
  one), so a quiet circuit costs nothing.
* **Components in the page** (picking, outlining, *flash renders*): nothing runs until asked. One lookup walks the
  component's ancestors and scans each parent's frames up to the child; measured on the demo's `/products` (132
  components, a 120-row list): 0.03 ms per lookup on a Server circuit, 0.38 ms on WebAssembly (interpreted), every one
  of the 132 located and 400 of 400 sampled elements resolved back. *Flash renders* is opt-in and locates at most 60
  components per refresh tick through one `BevDomMapBatch`, which walks each parent's frames once, so a list's rows cost
  one scan of the list, not one per row. In the browser the cost is the outline itself (a layout read per component).
  All of it is reported as *page mapping* in About.
* **HTTP senders** (`CaptureHttpCallers`, on by default): one stack walk per recorded request (none while capture is
  paused), not per render or per retry.
  Measured on .NET 10 (JIT, x64, Release) with no component on the stack, so every frame is examined - the worst case: 10 µs and 8 KB at a depth of 26 frames, 23 µs and 17 KB at 66, 41 µs and 32 KB at 126. Nearly all of it
  is the runtime capturing the stack (`new StackTrace` alone: 8 µs and 7 KB at 26 frames); what BevTools does per
  frame is read once per type and cached. WebAssembly, interpreted, was not measured and is slower. Turning the option
  off removes it.
* **Profiler time axis and renders per page**: built only when shown. The time axis reads the retained samples once
  and the depth of each component that rendered in the batch once (a 20 000-deep chain is placed without recursion,
  tested); renders per page reads every retained sample with a binary search into the navigations, only while its
  section is open, only after something was captured since, and at most once a second.
* **Leak tracking**: one weak reference per disposed application component (at most 2000), checked at most once per
  diagnostics evaluation; analysis runs once per retained instance, and the renderer's frame buffers are read only
  when a retained instance has no cause of its own. *Collect garbage and check* runs full blocking collections on
  request: on a server process that pauses every circuit for a moment, which is why it is refused outside Development
  there.
* **Profiler p95 and batch trees**: the p95 column sorts each instance's retained durations once and keeps them until
  that instance renders again; by type, a type's p95 is kept until one of its instances renders again or the set of
  instances changes. A refresh therefore sorts only what rendered since the last one. A batch's cost tree reads the
  retained samples of every component once, when the batch is selected (or the tab opens with one selected), never on
  a refresh; its totals are computed without recursion, so a 20,000-deep chain costs a loop, not a stack overflow
  (`ScaleTests`).
* **Render profile** (exports, *Compare…*): reads every retained sample once. 5,000 components with full rings
  (500,000 samples): 144 ms (Debug, x64 desktop; `ScaleTests`). Protocol snapshots do not build it.
* **Endpoints** (Network → *Endpoints*, exports): one grouping of the request buffer (at most `MaxHttpRequests`, 500)
  per refresh of that view; `http.per-item` the same over the last 30 s, once per diagnostics evaluation.
* **Lambda names**: the IL of an inline handler is decoded the first time its event fires, then cached.
* **The companion and the extension** poll the runtime they show every 1.5 s while visible: one `snapshot` request,
  answered `unchanged` when the session version did not move, otherwise with the timeline events since the last one
  (plus the newest 200, which can still complete). On Blazor Server each poll is one circuit round trip; nothing is
  rendered into the app, which is why the companion is the better way to measure a circuit's own traffic.

The benchmark above is unchanged by these: the same workload measures the same allocations with and without them.

## Things that are deliberately *not* optimised away

* The render fragment wrapper is installed for every `ComponentBase`, including ones that never render. Skipping it
  conditionally would make render counts wrong for exactly the components a developer is hunting.
* Component records are kept for 60 s after disposal so the timeline and errors can still name them.
