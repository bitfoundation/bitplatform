# Capability audit

The state of BevTools against what a Blazor developer expects from developer tools, with the evidence behind each
verdict. [capabilities.md](capabilities.md) says how each capability works and where it stops;
[roadmap.md](roadmap.md) says what is being done about the gaps; [progress.md](progress.md) records each round.

Baseline (2026-10-22, branch `msynk/14103-bevtools-improvements`): the solution builds without warnings; 309 .NET tests
pass on net10.0 (net8.0 and net9.0 could not run on the audit machine, which has only the .NET 10 runtime); 27 vitest
tests pass.

## Reference points

| Tool | What developers rely on it for | Source |
| --- | --- | --- |
| Chrome / Edge DevTools | Elements, network waterfall, console, performance, memory, application storage, accessibility tree | Browser built-ins; BevTools links to them instead of rebuilding them |
| React Developer Tools | Component tree, **select an element in the page**, props/state/hooks, **highlight updates when components render**, "why did this render", profiler (flamegraph, ranked, timeline), source location | react.dev, the extension itself |
| Vue Devtools | Component tree, element picker, timeline (events, router, Pinia), state editing and time travel for Pinia | devtools.vuejs.org |
| Angular DevTools | Component explorer, change-detection profiler, **injector tree** | angular.dev/tools/devtools |
| Blazor Developer Tools (community, joegregorydev) | Component tree, **element picker**, **.razor file path**, timeline profiler with flamegraph, "why did this render" | addons.mozilla.org listing; it needs a NuGet package that adds **hidden markers to the app's HTML** |

What is specific to Blazor and served by none of the browser tools: render causes across the .NET/JS boundary, the
circuit, render modes and prerendering, JS interop in both directions, DI lifetimes per hosting model, and
components that leak because a long-lived service still holds their event handler (a pattern behind real reports,
e.g. ABP support #10730 and #10861).

## Verdicts by area

Legend: **Strong** — complete for its scope, tested · **Partial** — works, with gaps listed · **Gap** — missing ·
**Out of scope** — better served elsewhere, with the reason.

| Area | Verdict | Evidence and gaps |
| --- | --- | --- |
| A. Shell and developer experience | Strong (rounds 1, 2, 7) — was Partial: | Dock bottom/right, three sizes, themes, persisted preferences, command palette with global search (components, errors, requests, services, timeline), companion window and browser extension. Gaps: tabs have no arrow-key navigation; icon-only header buttons are named by their glyph; opening the panel does not move focus, closing does not restore it; notifications have no `aria-live` and no hide timer; the palette has no dialog/listbox semantics. |
| B. Component inspector | Strong (round 1) — was Partial: | Live tree, parameters, cascading values, fields, injected services, event wiring from the render tree, render modes, lifecycle, causes, forced renders. **Gap: no component ↔ DOM mapping** — no element picker, no highlight of a component in the page, no render flashing (capabilities.md said "N/A"); source location only inferred from the type name. |
| C. Profiler | Strong (round 8) — was Partial: | Ranked table per instance and per type, baseline, last batches, lifecycle metrics (.NET 10), Perfetto trace export. Gaps: no in-app flame/timeline view, no percentiles, no per-route breakdown, no comparison of two recordings. |
| D. State | Partial | Adapter contract, snapshots, ordered diffs, history, authentication built in. Time travel deliberately absent (restoring arbitrary graphs is unsafe). Gaps: `Detail` captured but never shown; no search. |
| E. Routing and lifecycle | Strong (rounds 2, 6) — was Partial: | Current route, route values, query, template, layout, route table with authorization, navigations with .NET 10 durations, enhanced navigation, prerender handoff. Gaps: no `LocationChanging`/`NavigationLock` visibility, no cancelled navigations, no redirect-loop detection. **Defect:** a navigation is dropped when it follows within 500 ms one whose title contains the new path (`/products` after `/products/1`; the root path always matches). |
| F. Network | Strong (rounds 2, 9) — was Partial: | `IHttpClientFactory` requests with redacted headers, triggers, retries as separate rows, WebSocket/SignalR traffic by message kind. **Defects:** cancellations and `HttpClient` timeouts count as failures but never reach Errors and are not told apart; `http.duplicate` groups by the exact URL, so a request per keystroke (`?q=a`, `?q=ab`) never fires it; the documented "signature without query values" includes them. Gaps: no grouping by endpoint, no sorting. |
| G. Errors | Strong (round 10) — was Partial: | .NET, render, event, JS, resource, CSP and HTTP errors, redacted, grouped within a minute, preceding events. Gaps: dynamic messages split groups; resource/CSP counters shown in no panel. |
| H. Memory and leaks | **Strong** (round 3) — was **Gap**: | Heap, allocations and collections are shown (Runtime). Nothing tells a developer that a disposed component is still alive, or what keeps it alive — the most common Blazor leak (an event handler left on a long-lived service). |
| I. Browser | Partial | Online/visibility, reconnects, JS heap (Chromium), load timing, storage inventory with in-browser redaction. Out of scope: IndexedDB, cookies, Cache Storage, service workers (the browser's Application panel serves them; no Blazor-specific value found). |
| J. DI | Strong | Registrations, lifetimes, constructor graph, consumers, captive dependencies, cycles, transient disposables, components injecting a service. **Defect:** DI findings are computed once and are the first evicted when 200 other findings accumulate, so they disappear for good. |
| K. Forms and validation | **Strong** (round 5) — was **Gap**: | Nothing reads `EditContext`: no field states, no validation messages, no "validation attributes are ignored because no validator component is present". |
| L. Accessibility | **Strong** (round 7) — was **Gap** (host) / Partial (own UI): | No checks of the host page. Own UI: see A; filters mostly unlabelled; only the tree is keyboard operable; event-kind chips are below AA contrast; no `forced-colors` support. |
| M. Security and privacy | Strong | Development-only by default, `AllowInMultiUserHost` + `Authorize` for production, capture-time redaction, `ValueAccess`, no outbound traffic, text-node rendering in the companion, extension without permissions. |
| N. Extensibility | Strong | Panels, state providers, rules, commands, formatters, timeline events; the Server package is itself an extension; failing rules and providers are isolated. |
| O. Recording and collaboration | Strong (round 8) — was Partial: | Pause/resume, JSON and Markdown export, Perfetto trace, the companion opens exported sessions. Gaps: no comparison of sessions; the in-app panel cannot open one. |
| P. Testing | Strong | Unit, bUnit, vitest and Playwright-verified flows; benchmark harness. Gap: keyboard/a11y of most panels untested. |
| Q. Advanced | Partial | OpenTelemetry-compatible framework activities are consumed; trace export. Investigated in later rounds: source mapping via PDBs, editor links (both done, round 4), MCP (not done: it needs an authenticated transport, see the roadmap). |

## Feasibility findings

- **Component ↔ DOM mapping is feasible without changing the page.** Blazor's browser renderer keeps a *logical
  element* tree on the DOM nodes it creates: an array of logical children and a pointer to the logical parent under
  `Symbol()` keys, and a `true`-valued marker on each interactive root container while it is attached
  (`blazor.web.js` 10.0.13: `LogicalElements`, `BrowserRenderer.attachRootComponentToLogicalElement`). Every frame of
  a component's current render tree maps to exactly one logical child (element, text, component container comment,
  markup container comment); regions are flattened; attributes and reference captures produce none
  (`BrowserRenderer.insertFrame`). A component's position is therefore a path of logical indices computed in .NET from
  `ComponentState.CurrentRenderTree`, and every step can be verified against the DOM (tag names), so a stale or
  unexpected structure is reported, never guessed. The competing community tool needs markers in the HTML for this.
- **Exact source files are feasible where PDBs exist**: the Razor compiler's `#line` directives make the `.razor` file
  the document of `BuildRenderTree` in the portable PDB.
- **Leak evidence is observable**: BevTools already holds disposed components weakly; one that survives a full,
  developer-requested collection after disposal is retained by something, and delegates on the services the component
  injected (event fields) can be checked for targets that are the component or a closure over it.
