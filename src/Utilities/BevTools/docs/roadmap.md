# Roadmap

Prioritised from [audit.md](audit.md): defects first where they make BevTools say something wrong, then the gaps that
are both specific to Blazor and feasible without faking data. Each item lists its acceptance criteria; status lives
in [progress.md](progress.md).

## Done

Items 1-10 below are complete (rounds 1-10, see [progress.md](progress.md)); their acceptance criteria are kept for
reference.

### 1. Component ↔ DOM mapping (element picker, highlight, render flashing)

- Picking: a ⌖ button (panel header, components toolbar, command palette, companion) starts a pick mode in the page;
  hovering outlines the component that rendered the element under the pointer and names it; a click selects that
  component in the inspector; Escape cancels. The click never reaches the application.
- Highlighting: hovering a tree row or pressing *Show in page* in the inspector outlines the component in the page
  (scrolling it into view on request).
- Highlight renders: an opt-in toggle flashes an outline around components as they render, coloured by how often.
- Exactness: the path is computed from the renderer's frames and verified step by step in the browser; a mismatch
  (the DOM has not caught up, foreign DOM manipulation) is reported as such, never resolved to a guess. Works for
  Server, WebAssembly, Web App (several roots), Hybrid; components that render no node say so.
- No change to the application's DOM other than BevTools' own overlay outside the app's elements; no markers.
- Protocol `locate`/`resolve` so the companion window and the extension get the same, plus the extension's Elements
  panel selection resolving to its component.
- Tests: .NET round trip (locate ↔ resolve) over real render trees including regions, markup, nested components,
  `@key`ed lists; vitest over a logical tree built like Blazor's; Playwright against the demo (pick, hover, flash).

### 2. Defects found by the audit

- DI findings are never evicted by per-occurrence findings.
- Cancelled requests are labelled *cancelled*, naming both possible causes (the caller, or `HttpClient.Timeout`: the
  handler pipeline cannot tell them apart), and are not counted as failures.
- A request per keystroke is reported (`http.burst`, by endpoint: method + path + query keys), with the race it implies.
- A navigation is never dropped because its path is contained in the previous one's.
- Notifications are announced (`role="status"`) and expire on their own.

### 3. Leaks

Components still alive after disposal (after full collections, or on a developer-requested one) and what holds them:
the service event, lambda, `DotNetObjectReference` or timer; nothing reported for what only the renderer holds.

### 4. Exact source locations

From portable PDBs verified to belong to the build (beside the assembly, embedded, or the one the WebAssembly runtime
lists), with configurable editor links; inferred and labelled without symbols.

### 5. Forms

EditContext fields, changes, validations and outcomes, validators and message displays (values never read; messages
per `ValueAccess`), and the rule for attributes nothing checks and messages nothing shows.

### 6. Navigation

Guards (NavigationLock, LocationChanging handlers) read without registering any, render cost per navigation, redirect
loops and storms. Cancelled navigations, `forceLoad` and NotFound stay out (see the capability matrix).

### 7. Accessibility

Own UI keyboard model, names, focus, contrast, forced colours; page checks that markup proves, mapped to components.

### 8. Profiler

p95 per component and type; what one batch rendered as a cost tree (self and subtree time, list rows grouped); two
recordings compared per type in the Profiler and in tests (`BevProfileComparison`), with exact counts and
noise-tolerant times; sort, grouping and comparison kept across tabs; keyboard-operable sorting with `aria-sort`.
Round 13: a batch on a time axis (*Tree* | *Time*) and renders per page.

### 9. Network

Endpoints with ids as `{id}`, counts, failures, p50/p95/max and bytes; sorting everywhere by keyboard-operable headers;
`http.per-item` for one request per list row; endpoints in exports and the report. Inline lambda handlers named by the
method they call, in event titles and the inspector. Round 12: the component that sent each request, per request, per
endpoint and in `http.per-item`'s evidence.

### 10. Errors

Message templates and groups (source, type, template, location); Errors → *Groups* with sorting and a group filter;
`error.repeated` by group; groups in exports and the report.

### 11-15. Verification, senders, time axis, companion parity, CI

net8.0 and net9.0 suites and Firefox/WebKit browser runs (round 11, with two fixes they found); the component that sent
each request (12); a batch on a time axis and renders per page (13); the companion's endpoints, senders and error
groups (14); `BevSessionCheck` and the `bevtools` tool for CI (15). See progress.md.

### 16. Logs

Structured `ILogger` capture with a panel of its own: level, category, event id, template and values, scopes, exception,
trace and span ids, the sending component and the UI event; application errors that were only logged reach Errors;
work that left the dispatcher is attributed through the `Activity` of the event it descends from; `log.flood`; exports,
the report, the trace, the palette, the companion and the extension. See progress.md, round 16.

### 17. Allocations per render

Bytes each render fragment allocated, per component and type (Profiler, inspector, exports, the report), compared
between recordings (a regression gate for allocations per render) and reported by `render.allocations`. See
progress.md, round 17.

### 18. Responsiveness

Interaction latency (Event Timing: input delay, processing, presentation, INP) and long frames (long animation frames,
long tasks), in the Browser panel, the timeline and exports; on WebAssembly linked to the UI event, navigation or batch
that ran inside them; `browser.slow-interaction` and `browser.long-frame`; demo problem 26. See progress.md, round 18.

### 19. The page's other storage

Cookies, IndexedDB, Cache Storage, service workers, quota and persistence, read on request without values;
`pwa.update-waiting`. See progress.md, round 19.

### 20. Correlation

The trace id each request carried to the server (and the links between a trace's requests and log entries),
exceptions of tasks nobody awaited, timeline markers. See progress.md, round 20.

### 21. State: details, search, restoring

Change details and a filter in the State panel; `IBevStateRestorer` (a provider-owned restore token per change), ⟲ in
the history and the protocol's `restore`; the demo's `CartState` restores. See progress.md, round 21.

### 22. Memory over time

The heap read once a second, full collections as timeline events with the heap after each, WebAssembly linear memory,
`memory.growth`. See progress.md, round 22.

### 23. Closing the open items

The MCP server and CLI report formats (built in a separate session and brought in, then aligned with the logs and
allocations exports), the CLI and `Bit.BevTools.Blex` in the release workflows, EF Core's commands (Logs → *Database*,
`db.per-item`, `db.slow`), opt-in request and response bodies, gRPC-Web / server-sent events / GraphQL told apart, the
profiler's batch views in the companion and the extension, BevTools' own rendering told apart in long frames, and the
Blex integration. See progress.md, round 23.

## Next

What is left has a reason it is left; nothing on this list is a known defect.

- Heap snapshots on WebAssembly: the runtime API lists `collectGcDump`, but leaves it undefined unless the app is built
  with the runtime's diagnostics enabled (checked on the demo's Development build: the member is there, its value is
  not). Offering it would need an app built for it; heap snapshots stay with dotnet-gcdump and the browser's memory
  tools, and the retained-components check names the holders it can.
- Container resolutions at runtime: `Microsoft-Extensions-DependencyInjection` reports only top-level `GetService` calls,
  not the dependencies a call site constructs - so a service that is only ever a dependency would read as never
  resolved, which is wrong data - and needs a process-wide verbose EventSource on a server. Left out on purpose.
- A Fluxor package: Fluxor is not part of this repository and its `IState<T>` already fits
  `AddBevToolsStateProviderWithSubscription` (snapshot `state.Value`, subscribe `StateChanged`); restoring would need
  Fluxor's own history, which it does not expose.
- Senders across an await in a service: a request a service sends after awaiting something of its own has no component
  on its stack. Naming it needs the component carried with the execution context from where BevTools sees it start
  (a lifecycle method, an event), which the framework exposes for events but not for lifecycle methods.
- Live access for agents (MCP against a running app): it needs an authenticated transport from outside the page, which
  the page hub deliberately does not offer; agents read exports (`bevtools mcp`), and the companion window and the
  extension read the live page.

## Later / investigated

- Per-instance timing of `OnInitializedAsync` and the other lifecycle methods: the renderer calls them through
  `IComponent.SetParametersAsync`, which BevTools cannot wrap without a proxy of every component; the framework's meters
  give them per type.
- Components that implement `IComponent` directly: listed, not measured - they hand the renderer fragments BevTools
  never sees.
- What streams after a response's headers (server-sent events, gRPC streams): BevTools' handler sees the headers; the
  body is read above it, by the app.
