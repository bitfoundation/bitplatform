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

## Next

- Senders across an await in a service: a request a service sends after awaiting something of its own has no component
  on its stack. Naming it needs the component carried with the execution context from where BevTools sees it start
  (a lifecycle method, an event), which the framework exposes for events but not for lifecycle methods.
- The profiler's batch views in the companion and the extension: they need the retained samples in a protocol answer
  of their own, asked for by batch.
- Publishing `Bit.BevTools.Cli`: it packs as a .NET tool (verified locally) but is in no release workflow yet.

## Later / investigated

- MCP or live CLI access for coding agents over the existing protocol (the `bevtools` tool reads exported files only;
  live access needs an authenticated transport; the page hub is
  deliberately reachable only from the page itself).
- Request/response bodies: kept out — see audit, area F/M; revisit only as an explicit, bounded, Development-only
  opt-in.
