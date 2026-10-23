# Progress

One entry per improvement round: what was found, what changed, how it was verified, what is left. The plan is in
[roadmap.md](roadmap.md), the evidence in [audit.md](audit.md).

Test environment for every round: Windows 10, .NET SDK 10.0.402 with only the .NET 10 runtime installed, so the
net8.0 and net9.0 test runs cannot start on this machine (they need their runtimes; CI runs them); Chromium through
Playwright for browser checks against the demo.

## Baseline (before round 1)

Build clean (0 warnings). .NET tests: 309/309 on net10.0. vitest: 27/27.

## Round 1 — components in the page

**Gap.** No component ↔ DOM mapping: no element picker, no outline of a component in the page, no render flashing
(the capability matrix said "N/A"). It is the feature React, Vue and Angular DevTools users reach for first, and the
one community Blazor tool that has it gets it by adding markers to the application's HTML.

**Finding that made it possible.** Blazor's browser renderer keeps a logical element tree on its DOM nodes
(Symbol-keyed, with a `true` marker on attached root containers), and every frame of a component's current render tree
maps to exactly one logical child (`blazor.web.js` 10.0.13, `BrowserRenderer.insertFrame`).

**Changes**

- `Inspection/BevDomMap.cs`: component → path of verified *(logical index, kind)* steps, path → owning component
  (nearest application component unless *internals*; markup and text handled; stale paths reported), `BevDomMapBatch`
  for many lookups in one pass; costs counted as *page mapping* overhead.
- Page hub: key learning from the panel's own element (its logical parent is a comment, which no physical parent can
  be), root detection excluding `true` keys common to ordinary nodes, verified path walking, outline overlay outside the
  app's DOM that follows its component on scroll, pick mode that swallows the picking click, render flashes, all removed
  on detach. Hook additions: `startPick`, `stopPick`, `isPicking`, `lastPick`, `highlightComponent`, `clearHighlight`,
  `componentAt`.
- Protocol: `locate`, `resolve` (path length capped).
- In-app panel: ⌖ *Pick* (header, collapses the panel while picking), tree-row hover outline (a receiver-less callback,
  so hovering never re-renders the panel), keyboard selection outlines too, *Show in page*, *flash renders* toggle
  (saved preference), palette commands `components.pick` and `components.flash`.
- Companion window and browser extension: *Pick*, tree hover, *Show in page*; the extension follows the Elements
  panel's selection (`$0`) to its component.
- Docs: capability matrix row, architecture section, README, extension README, performance.

**Verification**

- .NET: 325/325 on net10.0 → 327 with the batch and scale tests (new: `DomMapTests` 14, `PagePickingPanelTests` 5).
- vitest: 44/44 (new: `hub-components.test.js` 12, companion 3, extension 2).
- Browser (Playwright, Chromium, the demo, Interactive Auto): picking, hover outline, Show in page, tree hover, flash
  renders and Escape on a Server circuit and then on WebAssembly; the picking click never reached the app; companion
  window picking and hover; the extension's Blazor panel picking and Elements-panel selection → component. No console
  errors.
- Measured: 0.03 ms per lookup on Server, 0.38 ms on WebAssembly; 132/132 components located, 400/400 sampled elements
  resolved on `/products`.

**Found and fixed during the round**

- happy-dom (and potentially other libraries) keep their own Symbol-keyed tree on nodes; the first key-learning rule
  latched onto it. The rule now requires the panel element's logical parent to be a comment.
- *Show in page* scrolled the component into view and the resulting scroll event cleared the outline; outlines now
  follow their component instead of disappearing.
- The docked panel covered half the page while picking; it collapses to its header now.

**Limits, documented**: .NET 8 and 9 hub behaviour verified only through the shared `LogicalElements` structure, not in
a browser on those runtimes; Blazor Hybrid not exercised in a WebView.

**Independent review (after the round).** A reviewer read the round's diff against `blazor.web.js` 10.0.13 and found,
all fixed and covered by tests (`hub-components-roots.test.js`, `DomMapTests`):

- *High*: a panel that is itself an interactive root (per-page/component interactivity, a WebAssembly root) made every
  root invisible, because the root markers of its parent were learned as "common". Keys are now learned in two passes
  (comment parent first; a marked element parent only when no panel has a comment one), common keys only from what the
  panel rendered.
- *High*: identical roots could be matched to the wrong one - the ordinal was applied to the verified subset, counted
  the anchor on one side only, and the hub took the first runtime that answered. Now every runtime's anchor is excluded
  on both sides; a path that fits one root identifies it; position decides only when the page holds exactly the
  runtime's own identical roots (and the outline says "matched by position"); two runtimes answering is "ambiguous".
  Positions are never taken from the root cache.
- Disposed Web App roots keep their preserve-content mark; a container with a `false` mark is no longer a root.
- Clicking *Pick* while picking restarted it (the hub cancelled, then the button started again): the button is marked
  and passes through.
- Outline and flash DOM changes decided whose a render batch was; the overlay is now ignored there.
- Hovering tree rows sent a Blazor event per row over the circuit; hovering is now handled by the hub (`data-bev-id`,
  a `locate` request), and every Blazor event raised in the panel counts its `DispatchEventAsync` message as BevTools'
  own (a gap that clicks had before this round too).
- Root scans were repeated per flashed component; cached for a second (positions excepted).
- A pick resolved in another runtime left the starting panel collapsed; `Ui.Picking` survived a panel's disposal;
  the companion could read a pick in flight as cancelled (`isPicking()` now covers resolution) and left the page
  picking when closed; taps could not pick (non-passive touch listeners, pick on `touchend`); `DescribeElement` read
  past an element's subtree in a reused buffer; an unexpected exception while outlining could escape a handler.

## Round 2 — defects the audit found

| Defect | Fix | Test |
| --- | --- | --- |
| DI findings computed once, evicted first by per-occurrence findings, gone for good | The analysis is kept and re-reported each evaluation (no new timeline rows; dismissals kept) | `Service_graph_findings_survive_a_flood_of_per_occurrence_findings` |
| Cancelled requests and `HttpClient.Timeout` counted as failures, never reached Errors, not told apart | *cancelled* state (`IsCancelled`, `cancelled` in exports, `status:cancelled`), warning severity, both causes named; an uncaught timeout still reaches Errors as the app's exception | `HttpCancellationTests` (caller token, real `HttpClient.Timeout`) |
| A request per keystroke never fired `http.duplicate`; `Signature` documented as query-free but was not | New `http.burst` (one endpoint - method, path, query names - called `HttpBurstCount` times in `HttpBurstWindowMs` with changing values), with the triggering events and how many overlapped (the stale-response race); `Endpoint` added, `Signature` documented as what it is | `A_request_per_keystroke_is_a_burst_and_overlapping_requests_are_called_a_race`, `An_endpoint_is_...` |
| A navigation was dropped when its path was contained in the previous one's (`/products` after `/products/1`; `/` always) | Each navigation the framework's Navigate activity recorded absorbs exactly one `LocationChanged`; nothing is matched by title | two `PanelInteractionTests` |
| Notifications not announced, lingering until something re-rendered | An always-present `role="status"` live region; expiry on the refresh loop | `Notifications_are_announced_and_leave_by_themselves` |

## Round 3 — memory and leaks

**Gap.** The most common Blazor leak - a handler left on a long-lived service, an undisposed `DotNetObjectReference`,
a timer never stopped - was invisible: the Runtime panel showed heap numbers, nothing said *what* was retained.

**Finding.** Measured on a plain renderer with and without BevTools: Blazor keeps a removed component in its parent's
previous render tree until the parent renders again. A naive "alive after GC" check would call every navigation a
leak; the check excludes instances held only there. (bUnit keeps every component it rendered until its context is
disposed, so the leak tests run on a plain renderer.)

**Changes.** `BevLeakTracker`, `BevRetentionAnalyzer`, rule `memory.retained-components`, Runtime panel section with
*Collect garbage and check* (refused outside Development in a multi-user process), protocol command `collect`, demo
problem 16 (`PriceAlert` subscribes to the scoped `FilterState` with a lambda and never unsubscribes).

**Verification.** .NET 347/347 (new `LeakTests` 9: handler, capturing lambda, `DotNetObjectReference`, both timer kinds,
a tidy component reclaimed, a buffer-held one not reported and reclaimed after its parent renders, the grace rule, the
protocol and the multi-user refusal); vitest 55/55; Playwright: problem 16 on a Server circuit and on WebAssembly -
3 of 3 hidden alerts reported, cause *FilterState (injected) → Changed: lambda in PriceAlert.OnInitialized*, nothing
else reported, the Diagnostics finding present; all round 1 browser checks re-run green after the review fixes.

## Round 4 — exact source locations

**Gap.** The inspector inferred `TypeName.razor`; the competing community tool shows the file, and the React DevTools
"view source" habit is to jump to the code.

**Changes.** `BevSourceLocator` (PDB-based, verified per build), `BevSourceLocation`, `Ui.EditorUrlTemplate`, links in
the inspector for the component and each bound handler (`BevSourceLink`), `sources`/`handlerSource`/`sourcesPending`
in the protocol and the companion, the hub's `fetchPdb` for WebAssembly.

**Verification.** .NET 353/353 (`SourceLocatorTests` 6: type, handler and async locations against this file's own
lines read through the PDB's path, missing and unreadable symbols, editor URLs and the script-scheme refusal,
cross-platform file names, the inspector's links and the protocol); vitest 57/57 (`fetchPdb` in the .NET 9+ and .NET 8
resource formats, companion links never `javascript:`); Playwright on the demo: `Counter.razor:1` for the component and
`Counter.razor:53` for `IncrementCount` on a Server circuit and on WebAssembly.

**Found and fixed.** WebAssembly reads Windows paths from PDBs built on Windows: the file name is now split on either
separator. A `.razor` file's first sequence point is its first C# expression, which read as "the component starts at
line 24"; the component link now opens the file at its top.

## Round 5 — forms and validation

**Gap.** Nothing read EditContext. The silent form bugs - validation attributes no validator checks (`[Required]`
that does nothing), messages nothing displays - had no diagnosis.

**Changes.** `BevFormTracker` (EditForms watched from their first render; inputs' contexts on discovery), a Forms tab,
`forms.validation` rule, `forms` in the export and the companion, `forms.show` command, demo problem 17
(`NewsletterSignup`: `[Required, EmailAddress]` and no validator).

**Verification.** .NET 360/360 (`FormsTests` 7: discovery, a submit's outcome and messages, typing counted without
values, the export, both rules, the form-mapping exclusion, ValueAccess None, the panel); vitest 57/57; Playwright on
`/checkout`, Server circuit and WebAssembly: both forms listed, the newsletter form "validated by nothing" with its rules,
the shipping form's DataAnnotationsValidator, ValidationSummary and five messages, the Diagnostics finding, and the
shipping form not flagged.

**Found and fixed.** Watching from the first render subscribed BevTools before the validator, so a failing validation
read as valid: the outcome is now read after the synchronous validation completes. On a Server circuit the framework's
form-mapping subscription made the unvalidated newsletter form look validated: it is no longer counted.

## Round 6 — navigation

**Gaps.** Nothing said what can block a navigation (NavigationLock, LocationChanging handlers) or that two pages were
redirecting to each other; navigations recorded from the .NET 10 activity were titled with the raw display name
("Route /x -> App.Pages.X").

**Changes.** `BevNavigationGuards` and the Routing panel's *Navigation guards*; renders and render time per navigation
in the history; `navigation.loop` (ping-pong and storm); activity navigations titled "Navigate to /route"; demo
problem 18 (a bounded account ↔ login redirect loop, interactive only so prerendering cannot loop over HTTP).

**Verification.** .NET 365/365 (`NavigationDiagnosticsTests` 5: a NavigationLock and a handler named with their
component, the loop, ordinary navigation not flagged, a storm, the panel's guards and per-navigation renders; the
activity title asserted in `BatchAttributionTests`); Playwright on the demo, Server circuit and WebAssembly: the loop
stops after its bounded hops, *Navigation loop: Navigate to /loop/login ↔ Navigate to /loop/account*, no guards,
every hop in the history. One run right after a server restart did not show the finding in the panel on the Server
circuit; three runs since, and a step-by-step run comparing the panel with the protocol, showed it every time - noted,
not explained.

## Round 7 — accessibility

**Gaps.** BevTools' own UI: tabs not keyboard-operable, icon-only buttons named by their glyph, no focus management,
an unlabelled palette and filters, white-on-colour chips below AA (2:1 in the dark theme), no forced-colours support.
The host page: no checks at all.

**Changes.** Own UI: a labelled region; `role=tablist` with arrow keys, Home/End, roving `tabindex` and focus following
(`focusIn` in the hub, which also stops the arrows scrolling the page); `aria-label`s on every glyph button; Escape
closes the panel; focus into the panel on open and back to the badge on close (only when focus was in the panel, so a
shortcut pressed in the app keeps focus there); the palette as dialog + combobox + listbox with `aria-activedescendant`;
labels on every filter; event-kind chips at AA in both themes; `forced-colors` rules. Page: `auditPage` /
`highlightFinding` in the hub, Browser → *Accessibility*, `a11y.page`, findings in the export.

**Verification.** .NET 369/369 (`OwnAccessibilityTests` 4: a name for every control on every tab with a component
inspected, the tab keyboard model and focus, Escape and focus return, the palette's roles); vitest 58/58 (each page check
with a negative case, the component named, BevTools excluded unless asked, *Show*); Playwright: the demo's `/checkout`
has 0 findings, BevTools' own UI on every tab (inspector open) 0 findings, and two defects injected into the live page
are both found - the button inside `Counter`'s markup attributed to Counter, the input to static markup - and outlined.

**Limits.** Automated checks are a subset of WCAG; contrast and anything about meaning or order need a person. Not
verified with an actual screen reader in this environment.

## Round 8 — profiler

**Gaps.** No percentiles; a batch row only jumped to the timeline, so "where did this batch's time go" had no answer in
the app; no way to compare two recordings, so "did my change make anything render more" needed two screenshots; sort
headers worked with a mouse only; sort, grouping and *since reset* were lost when switching tabs.

**Changes.** p95 column (instances and types, sortable, the sample count in its title), sorted durations cached per
instance until it renders again. Header cells are `<th aria-sort>` with a button. A selected batch shows its cost tree
(self and with descendants, same-type leaf siblings grouped, click selects the component or filters by type), above the
batch list and scrolling by itself. Exports have a `profile` section (`BevRenderProfile`) counting components the
registry already let go; `BevProfileComparison` (public: `Compare(json, json)`, `Compare(json, session)`, options,
`ToMarkdown`) classifies each type as regressed / improved / new / gone with reasons and warnings; Profiler →
*Compare…* (a focusable file input) runs it against the live session, *Compare again* reuses the upload, *Copy as
Markdown*. View state moved to `BevToolsUiState.Profiler`. `IncludeProfile` export option; protocol snapshots name it
to get it.

**Verification.** .NET 383/383 (`ProfileComparisonTests` 8: thresholds both ways, new/gone, warnings, refusals with
reasons, an export without a profile section, gone components still counted, Markdown escaping, the panel's upload,
tab switch and *Compare again*; `ProfilerTests` +4: nearest rank, p95 cell and keyboard sort, a batch's tree with
self/subtree sums, grouping; a protocol test that polls leave the profile out; a scale test). vitest 58/58. Playwright
on `/products`, Server and WebAssembly: Enter on the p95 header sorts by it; reset, type *a* and Backspace, export:
the profile has 120 `ProductRow` instances and 211 renders; reset, the same twice, *Compare…* the export: 3 regressed
(`ProductRow` 211 → 422, `ProductList` and `FilterBar` 2 → 4, "+100 %"); the comparison survives a tab switch;
a 122-component batch reads as 3 rows (`ProductList` 0.40 ms itself, 9.90 ms with its rows; `ProductRow` ×120
9.50 ms, WebAssembly); the profiler UI adds no accessibility findings; no page errors.

**Found and fixed on the way.** The first browser scenario (type *a*, then *bc*) filtered the list to nothing, and the
comparison rightly said `ProductRow` was *gone*, not regressed: the check, not the feature, was wrong. Two profiler
tests were flaky (1 in 4 runs) because the panel's 250 ms refresh loop could re-render between a `FindAll` and its
`Click`; the tests now refresh explicitly.

**Independent review** (a separate agent, 383/383 at the time) found two bugs, both fixed and tested: a type that
rendered slightly more *and* clearly longer was *Unchanged* (the time check excluded any render increase); and the
batch tree's subtree totals recursed once per level, so a batch rendering a chain several thousand deep overflowed the
stack and, on Server, would end the process (now iterative, and the tree moved to `BevBatchCostTree`). Risks fixed:
generic types were matched by `FullName`, which carries the assembly versions of their type arguments, so a CI build
that stamps versions saw `Grid<Product>` as *gone* plus *new* and never as regressed (now a version-free key); the docs
said counts were exact while the default tolerated 10 % (now exact by default, `RenderChangeRatio` opt-in); a selected
batch could not be closed and its tree was rebuilt on every refresh (now a toggle, ✕, dropped with the timeline, built
on selection); uploads were read into a string (now `JsonDocument.ParseAsync` on the stream, 32 MB cap) and only some
exceptions were caught (now all, shown as the comparison's error, so a bad file cannot end a circuit). Smaller: text
from the file could add Markdown lines through the warnings (now one line); exports without a profile section are
warned about; hidden framework components no longer appear in batch trees; caps say how many rows they leave out;
tree and comparison rows have buttons for the keyboard; the by-type p95 is cached; `profiler.reset` and the protocol's
`baseline` switch the table to *since reset*; a patch release no longer counts as a different runtime. Tests added for
each, including p95 that differs from the maximum, row order by p95 against max, the reset window, gone totals reset
and cleared, and a 20,000-deep chain. After the fixes: .NET 391/391.

**Limits.** p95 and batch trees cover the retained samples only (stated in the UI). Times are not comparable between
Server and WebAssembly or Debug and Release; the comparison warns for render mode and runtime, not for build
configuration. No time-axis flame chart and no per-route breakdown.

## Round 9 — network

**Gaps.** Requests could be filtered but not grouped or sorted; ids in paths made every request to `/api/orders/{id}`
its own endpoint, so neither the panel nor a rule could say "this list calls the server once per row". Event titles
named inline lambdas by their compiled names (*onclick → Orders.&lt;BuildRenderTree&gt;b__0_1*), the most common way
Blazor handlers are written.

**Changes.** `BevHttpEndpoints.Template` and `BevHttpRecord.EndpointTemplate` (ids as `{id}`); Network →
*Requests | Endpoints* (a pressed-button group), the endpoints table (requests, failed, cancelled, p50, p95, max,
received, last) with an endpoint chip that filters the requests; every column sortable through `BevSortHeader`, shared
with the Profiler, whose sort now also reverses; `BevSortState` and `BevNetworkView` keep sorts and views across tabs;
`http.per-item` (`HttpPerItemCount`); `httpEndpoints` in exports and *Busiest endpoints* in the Markdown report;
`BevLambdaTargets` for event titles and the inspector; `BevStats.Percentile` shared by the profiler and the network.
Demo: problem 19 (each order row loads its own details, `GET /api/orders/{id}`), and the "fifteen problems" text that
had not followed problems 16-18.

**Verification.** .NET 406/406 (`NetworkEndpointsTests` 12: eight path shapes, a summary that keeps cancellations
and pending requests out of the durations, the rule against duplicates, bursts and too few ids, the panel's grouping,
drill-down, sort and reverse, exports and the report; `LambdaNamesTests` 3: this-capturing, closure and async lambdas,
two calls, a property, the framework's `StateHasChanged`, a type that cannot be found). vitest 58/58. Playwright on
`/orders` with problem 19, Server and WebAssembly: 25 rows load their details; *Endpoints* shows
`GET /api/orders/{id}` with 25 requests and its percentiles (Server p50 108 ms, p95 144 ms; WebAssembly 252 / 440 ms);
Enter on the p95 header sorts, again reverses; the view adds no accessibility findings; the endpoint opens its 25
requests; Diagnostics: *GET …/api/orders/{id}: one request per item (25 ids)* "during onclick → Orders.Load (lambda)"
- the IL is readable on WebAssembly too; no page errors.

**Found and fixed on the way.** The first browser run showed endpoints without their method (the panel stripped the
origin from "GET https://…" as if it were a URL) and *0 B* received where no response had reported a size (chunked
JSON): the method is kept, and an unknown size is *–*. Two lambda-name tests were wrong, not the code: a method group
is not a lambda (now only compiled lambda names are decoded), and a nested type's short name.

**Independent review** (421/421 at the time; its experiments drove a real renderer, the .NET 10 `HandleEvent`
activity and `dotnet watch`). Bugs, fixed and tested: a compiled lambda name is not unique - two overloads' closure
classes, or a base class and its derived class, both have `<Row>b__0` - and the first match named a method the
clicked lambda never calls (now every candidate is collected, and more than one is no name); names outlived hot
reload, which keeps a lambda's compiled name while its body changes (now a `MetadataUpdateHandler` clears them);
*Received* summed the sizes that were reported and showed "0 B" for 24 chunked bodies and one empty 404 (now a total
only when every response reported its size). Risks, fixed: a lambda calling a service and a component method was
named after the component method (now any second call into application code - a service, a local function, a base
class in another assembly - makes the name a guess); `http.per-item` re-read every request after each window start
(75-150 ms for 500 requests; now one sliding window with running distinct counts, about 1 ms, and endpoints computed
once per request); a lookup typed into a box (`/orders/1`, `/12`, `/123`…) was called one request per row (now runs
whose requests mostly come from events of their own are `http.burst`'s, by endpoint template). Smaller: a request
that failed without a response sorted below every 200; ties sorted oldest first; *Clear* left an endpoint filter that
matched nothing; the sort arrow was part of the header's accessible name; a profiler sort by a column the grouping
hides kept an order no header showed; `sort:MAX` sorted by total; an address with no path lost its query names. Tests
for each (`LambdaNamesTests` 6, `NetworkEndpointsTests` 17, `ProfilerTests` +1). After the fixes: .NET 430/430.

**Limits.** `{id}` is a heuristic: a slug or a short code (`/orders/ab12`) stays literal, and a numeric segment that
is not an id (`/v2/` stays, `/2024/` becomes `{id}`). The per-item rule cannot tell which component issued the
requests, only the UI event, if any.

## Round 10 — errors

**Gaps.** Errors collapsed only when the exact message repeated within a minute, for one component instance: the same
failure with another id in each message ("Order 1042 not found", "Order 1043 not found") was a new line every time,
and `error.repeated`, grouping timeline titles, never added them up. The filter's hint left out the `resource` and
`csp` sources the browser reports.

**Changes.** `BevErrorGroups` (public `Template`; internal group key and summaries); `BevErrorRecord.MessageTemplate`
and `Group`, set where errors are recorded; Errors → *Errors | Groups* with sortable columns in both, a group chip that
filters the errors and selects the latest; `error.repeated` by group, titled with the template and the number of
different messages; `messageTemplate` and `errorGroups` in exports, and the groups with more than one message in the
Markdown report. Demo: problem 20 (*Open a missing order*, another id each time).

**Verification.** .NET 421/421 (`ErrorGroupsTests` 15: eleven messages - what is replaced and what is kept, including
status codes, durations, versions and apostrophes; one throw site with different ids is one group, another site or
type is not; the rule by group; the panel's groups, drill-down and chip; exports and the report). Playwright on
`/errors` with problem 20, Server (page ErrorBoundary, *Recover* between) and WebAssembly (Blazor's error bar): three
errors, one per id; *Groups*: one row, 3 messages, 3 occurrences; no accessibility findings; the group opens its three
errors with the latest selected; Diagnostics: "… Order {n} was not found. occurred 3 times with 3 different messages".

**Found on the way.** The framework logs a handler exception as "Unhandled exception rendering component: <message>",
and that logged text is what the error carries; the group's title includes it. The browser check expected the bare
message and was corrected, as was the problem card's text.

**Independent review** (15/15 at the time; experiments against the built assembly). Bugs, fixed and tested: the throw
location was templated, so in a file of a thousand lines every throw site with the same message shape was one group
(`BigService.cs:1042` and `:1311`); a V8 stack starts with its message, so JavaScript errors from three scripts were
one group and `error.repeated` reported one error three times; the rule looked every occurrence's record up in the
ring buffer (20 ms per evaluation with 5000 errors) and counted an occurrence by its raw title once its record had
been evicted (now each occurrence's timeline event carries its group and title: one pass, and evicted errors still
count); the Groups view kept filtering by a group opened earlier without showing it; BevTools' own HTTP error messages
name requests by relative path ("GET /api/orders/42"), which were not templated. Risks, fixed: ports were templated
(`localhost:5000` and `:7000` merged) - addresses and host:port pairs are now set aside before the other passes; a
framework-only stack or a shared throw helper made distinct failures one group - the anchor is now the first
application frame, and without one quoted values stay; ISO timestamps split groups by the second. Smaller: the filter
hint named a source nothing records and missed three that are; the Groups table now shows where each group was thrown;
exports carry each error's and each group's `group` key; the problem card quotes the text the framework logs. After
the fixes: .NET 438/438.

**Limits.** Templates are heuristics: a two- or three-digit id ("Order 42") stays in the message, so those errors
group only by their own exact text; a quoted value that names a different failure ("Could not find 'initMap'" and
"'initChart'") is merged when it comes from the same throw location.

## Review of rounds 2-7 (after round 10)

Two independent reviews, run after round 10 because these rounds had not had one of their own; every finding was
reproduced by the reviewer and fixed here with a test.

**Rounds 2-4.** Leaks: collections were counted from the disposal, so a component the renderer held through a forced
check was reported moments after its parent let it go, with no collection since; the renderer-held exclusion was
skipped whenever any cause was found, and a transient view model counted as a holder - now a probe starts counting
only once it is free to go (past its grace period and out of the renderer's buffers), only singleton and scoped
services are holders, and a forced check confirms a renderer-held instance only on such a cause. Timers were named
when merely not disposed - now only when still scheduled with a callback or state that reaches the component. The
analysis was quadratic (2000 leaked rows on one event: 5 s on the dispatcher) - now each holder is indexed once per
check (1000 rows: 56 ms), dictionaries are read through their enumerator, invocation lists capped. `IOptionsMonitor.OnChange`
listeners are found through the subscription object. While fixing this, BevTools' own one-second cache of
renderer-held components turned out to keep them alive, and a debug build kept the marking's list on the stack through
the forced collection: both gone. HTTP: a cancellation the caller did not ask for (a connect timeout) was neither
cancelled nor reported - now a failure in Errors with its inner message; the cancelled text names pipeline handlers.
Sources: a downloaded PDB is fetched with its integrity hash and past the cache, a failed download is retried, hot
reload edits mark locations *before hot reload*, assemblies without a file outside WebAssembly say so, editor links
are judged on the final address (`java\tscript:` and a leading control character are refused), and an unreadable
PDB beside an assembly no longer stops the search.

**Rounds 5-7.** Forms: the field-change handler captured the EditContext, so BevTools kept every form - and through it
the page - alive and reported its own leak; forms were never forgotten, so a gone form read as "never checked" and 200
visits filled the list; a form reset after submit was not watched again; a form validated by hand in `OnSubmit` was
called unchecked; the first validation read messages `OnValidSubmit` had added. Navigation: loops were only found within
10 s of evaluation (the cause of round 6's unexplained miss), and a person clicking back and forth was a loop. Own UI:
Escape in the palette closed the whole panel (a round 7 regression), Tab left the modal palette, and focus fell to the
page after a command or a tab switch. Page checks: `tabindex="-1"`, hidden and inert content were called focusable;
names counted aria-hidden and hidden text, missed shadow-DOM text and `aria-labelledby` targets' `aria-label`, and
`aria-label`led images were called unlabelled; a failed component lookup read as "static markup"; findings outlived
navigation. Smaller: validator names showed closure classes, `ObjectGraphDataAnnotationsValidator` and subclasses of
`ValidationMessage<T>` were misread, the routing panel's renders per navigation absorbed later renders, a doc comment
had moved.

**Verification.** .NET 457/457 (new: release baseline, renderer-held collections, early forced check, transient view
model, fired timer, options monitor, 1000 rows, held with a cause; connect timeout; mismatched PDB, assembly from memory,
hot reload, script schemes; form forgotten, form not kept alive, reset form, submit handled in code; late loop, user
navigation, a redirect inside the previous navigation; palette Escape and focus). vitest 59/59 with the PDB integrity, failed download and a second page-check case
(screen-reader names, shadow DOM, keyboard reachability, negative cases).

**Found by the browser re-run of every round.** Two of the fixes above were wrong in a real browser and right in the
tests. The leak check marked what was free to go just before its forced collections, and on WebAssembly - whose
garbage collector scans the stack conservatively, stale slots included - the components it had just looked at survived
those collections: 29 list rows read as leaked. Marking now happens after the collections, with the count from before
them (a collection does not change what the renderer holds). And on .NET 10 a redirect made in `OnInitialized` runs
inside the router's activity for the navigation before it, so its parent is that navigation: "has a parent" is not
"made by a person" - only a UI-event parent is. Both covered by tests; every browser check passes again.

## After round 10

Ten rounds. Rounds 1, 8, 9 and 10 were reviewed by a separate agent after the round, and every finding was fixed or is
listed under its round's limits; rounds 2-7 were reviewed together afterwards (below).

- .NET tests: 309 → 457 on net10.0, all passing. vitest: 27 → 59, all passing. net8.0/net9.0: built, not run here
  (no runtimes on this machine).
- Browser (Playwright, Chromium, the demo on a Server circuit and on WebAssembly): every round's checks re-run after
  round 10 - picking, outlines, flash renders and the extension's Elements selection; leaks; source links; forms;
  navigation loops; accessibility (0 findings in the app and in BevTools' own UI); profiler comparisons and batch
  trees; endpoints and the per-item rule; error groups - all passing, no page errors.
- Overhead: allocations identical to the code before round 1 in all four benchmark configurations (see
  [performance.md](performance.md)); times not comparable on this machine.
- Audit: areas A, B, C, E, F, G, H, K, L, O strong after the rounds (were partial or gaps); D (state), I (browser) and
  Q (advanced: MCP) stay partial, with the reasons in [audit.md](audit.md) and the next items in [roadmap.md](roadmap.md).
