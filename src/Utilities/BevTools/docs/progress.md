# Progress

One entry per improvement round: what was found, what changed, how it was verified, what is left. The plan is in
[roadmap.md](roadmap.md), the evidence in [audit.md](audit.md).

Test environment for rounds 1-10: Windows 10, .NET SDK 10.0.402 with only the .NET 10 runtime installed, so the
net8.0 and net9.0 test runs could not start on this machine (CI runs them); Chromium through Playwright for browser
checks against the demo. From round 11: the .NET 8.0.32 and 9.0.21 runtimes installed into a private folder
(`dotnet-install -Runtime aspnetcore`, nothing system-wide) run the net8.0 and net9.0 test assemblies, and the browser
checks run in Firefox and WebKit as well as Chromium.

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

## Round 11 — what had not been run

**Gap.** Two verifications every round had skipped: the net8.0 and net9.0 suites (no runtimes on the machine) and any
browser but Chromium.

**Result.** net8.0 444/444 and net9.0 445/445 on the private runtimes (the difference with net10.0 is the .NET 10-only
tests). Firefox and WebKit ran every browser check; both found the same defect: Diagnostics did not show a redirect
loop that was over within milliseconds. The rules run at most once a second, a loop that ends within the second after a
run changes nothing afterwards, and nothing evaluated again - so the panel showed the loop only if something else
happened later. Fixed with a trailing evaluation: when nothing changed but the findings are behind the data
(`DiagnosticsEngine.IsStale`), the open panel runs the rules on its next tick. The first version ran them and did not
re-render, so the panel still showed nothing - its test asserted on the engine, not on what the panel showed; the
re-run of the browser checks caught it, and the test now reads the panel's markup (it fails without the re-render).
Exports had the same lag: an export (and the Markdown report) now runs the rules first when anything was captured since
their last run. With both fixes the browser check still failed now and then: it opened the panel as soon as the page
said "Arrived after N redirects", which shows while the last hops still run, and read Diagnostics once, 1.2 s later -
the loop showed about 1.25 s after the panel opened, on the trailing run. That is the documented latency (within about
a second of the last change), so the check now waits up to 4 s for the finding; a finding that never shows still fails
it. Afterwards: 3 of 3 runs passing in each of the three browsers. Firefox also reports "Cannot send data if the connection is not in the 'Connected' State" now and then
during reloads: the stack is Blazor's own SignalR send loop, not BevTools.

## Round 12 — the component that sent a request

**Gap.** A request per list row (`http.per-item`) said which UI event started the list, not which component sent the
requests - and a row loads its data in `OnInitializedAsync`, inside no event, so often nothing at all.

**Changes.** `BevCallSites`: the nearest component method on the stack and the application method between them
(*OrderRow.OnInitializedAsync via ProductApi.GetOrderDetailsAsync*), naming async methods through their state machines,
local functions by themselves, lambdas by their method; framework and BevTools assemblies by exact name, so
`Bit.BevTools.Demo.*` stays application code. Read at the outer end of an `IHttpClientFactory` pipeline, once per
request, so a retry handler that awaits first does not lose it and every attempt carries it; a hand-built client's
handler reads it itself. `CaptureHttpCallers` (on). Network: *Sent from* in the detail, the Trigger column's fallback,
the filter, the endpoints' *Sent from* column; `http.per-item`'s evidence; exports; the companion.

**Verification.** `HttpCallerTests` (async method after an await with the service between, local function, option
off, a factory pipeline whose retry handler awaits - fails when the outer capture is removed - a continuation run by
another component's completion, endpoint counts). Playwright on `/orders` with problem 19, Server and WebAssembly, in
all three browsers: the request detail, the endpoint row (*OrderRow.OnInitializedAsync ×25*), the finding.

**Cost.** The first version walked 54 µs and 25 KB per request at a stack depth of 26 (every frame's
`Assembly.GetName()` allocates); with the per-type facts cached, 10 µs and 8 KB, nearly all of it the runtime's stack
capture (see performance.md).

## Round 13 — a batch on a time axis, renders per page

**Gap.** The two profiler items the roadmap had left: no time-axis view (only the Perfetto export) and no breakdown by
page.

**Changes.** `BevBatchTimeline`: a batch's renders, each at its start, one row per depth among the components that
rendered in it (depth memoized, no recursion; 300 bars at most); *Tree* | *Time* in the batch section, an image to a
screen reader with the tree as its alternative, names hidden on bars too narrow for them, equal widths in start order
when the clock could not separate the renders. `BevRouteProfile`: each retained render on the page shown when it
happened, with visits, renders, time and the three costliest components; *Renders per page*, computed only while open.

**Verification.** `ProfilerTimeAndRouteTests` (order, depth, start and span; a 20 000-deep chain; route assignment;
navigations at the same instant; enhanced navigations; a real `Router`, below); Playwright, three browsers, Server and
WebAssembly: 93 bars for a keystroke on `/products`, a bar opens its component, the view is remembered, per-page rows for
`/products` and `/orders` with *OrderRow* costliest on the latter, no accessibility findings.

## Round 14 — the companion and the extension

**Changes.** The companion's Network view lists the endpoints with their senders (the origin left out, as in the panel)
and each request's *Sent from* (or why there is none); its Errors view lists the groups that merged more than one
message. The extension is the same frontend. vitest for both views; Playwright on the live demo in three browsers.

## Round 15 — a CI gate

**Gap.** `BevProfileComparison` could fail a test on render regressions, but nothing read an export's findings, and
both needed code.

**Changes.** `BevSessionCheck` (public): an export's findings against a severity, ignored rules, dismissed findings,
optionally recorded errors; refuses an export without the section it needs; caveats for what limits a pass.
`Bit.BevTools.Cli`: the `bevtools` .NET tool, `check` and `compare` over files, Markdown out, exit codes 0/1/2. It packs
and installs as a tool (verified locally into a scratch folder) and is in no release workflow: publishing it is the
maintainers' decision. Live access for agents (MCP) stays out until there is an authenticated transport.

**Verification.** `SessionCheckTests` (findings from a real session; thresholds; errors; refusals; the tool's exit codes,
report file and argument edge cases; comparisons with thresholds); the installed tool run on the demo's exports.

## Review of rounds 11-15

An independent review reproduced fifteen defects; all are fixed with tests unless listed under limits.

- **Renders per page put each page's first render on the page it left** (.NET 8 and 9, or framework instrumentation
  off): the router renders the new page inside its own `LocationChanged` handler, before the panel records the
  navigation. The navigation is now dated from the render batch the router just ran. A test with a real `Router` fails
  without it.
- **A rule's window passed the CI gate on evidence the export carried**: with no frontend open, the rules ran only at
  export, and requests 45 s old were outside their 30 s window. `Diagnostics.EvaluateWhileClosed` runs them as data
  arrives; the check's caveats say when it was off. The claims that an export's findings are "those of the data it
  carries" were corrected.
- Live updates off no longer re-renders the open tab through the trailing evaluation.
- `--fail-on warning,error` (and an export's severity `-1`) parsed as flag combinations and passed everything: severities
  are read by name only.
- On WebAssembly a continuation run by another component's completion named that component as the sender: the walk now
  stops at the completion and says so.
- The tool crashed on `--report ""` (an unset `$GITHUB_STEP_SUMMARY`) and on error counts past `int`; an option's value
  could be the next option; `--help` after a command failed.
- A zero-span batch stacked its bars; renders older than the oldest navigation held were labelled "before the first
  navigation"; navigations at the same instant took the earlier one; the endpoints' "+N senders" counted at most four; a
  missing errors or http section read as zero; the stack was walked for requests that would not be recorded; "no
  component" did not say why (too deep, unreadable stack); timeline HTTP events carried a null caller.
- Plausible ones fixed as well: evaluations from different threads are serialized; an export evaluates before writing
  anything, so its version, timeline and findings agree; renders per page is recomputed at most once a second;
  enhanced navigations are the same page; the check reports paused capture and disabled rules; the tool rejects
  non-finite and out-of-range numbers; `BevCheckedFinding` is not a positional record, so it can grow.

**Limits.** A bar's start includes BevTools' state capture for an inspected component (documented, not changed: it
would move a public property's setter or the hot path); a generic component is named by its definition; a sender
across an await inside a service is not named (roadmap).

## After round 15

- .NET tests: net10.0 482/482, net8.0 469/469, net9.0 470/470. vitest: 60/60.
- Overhead: render-path allocations unchanged (23.9, 23.9, 142.0 and 209.1 MB in the four benchmark configurations,
  as after round 10).
- Browser: every check in Chromium, Firefox and WebKit, Server and WebAssembly (see the rounds above); WebKit's
  accessibility run: 0 findings in the app, 0 in BevTools' own UI.

## Review of the demo

Every demo page was read against what the panels show, and every claim checked in a browser.

**Claims that were wrong.** The product search's requests were called duplicate HTTP requests; they are a burst
(`http.burst`, different query values). Problem 13 pointed at the route table's `[Authorize]` column, which no demo
page fills. The Errors page left out its own *Open a missing order* and *Groups*; the Circuit page left out
`circuit.blocked`, and *Block 5× in a row* times only its first block as an inbound message. The Orders page said no
click waits on the per-row requests: the click rendered the rows, so they are linked to it, and *Sent from* is what
names the row. Calls on a JS object read `new module.Tally`; palette commands need `>`.

**What no page showed, now shown.** Picking, outlining and flashing components; *Type & source* and handler links;
Lifecycle; a batch's cost tree and time axis; renders per page; *Compare…* against a fix the Products page switches on
(`ProductRow.ShouldRender`); *Sent from*; a cancelled request; route values, a token in a query string, navigation
guards and sign-in (a new *Routing & sign-in* page); the accessibility check (a planted coupon box on Checkout); a
resource that fails to load and a CSP violation (the demo's server sends `frame-src 'none'`); a dropped circuit
connection that comes back (the Circuit page closes the socket from the browser, as a network hiccup would - the
browser's offline mode leaves an open WebSocket alone, so the page no longer suggests it); .NET 10 `new`/`get`/`set`
interop; `[BevToolsIgnore]`; the companion window, the browser extension, the exports, the CLI and the page hub (*Share
& automate*); and an extension of the demo's own - a panel, a state adapter for `FilterState`, a rule over
`IBevToolsTimeline` events, commands and a value formatter (*Extending*). Getting started gained the loader, the value
access levels, the header's controls and a table of every tab. Problems 21-24 are new; 15 is back in order.

**Defects the review found in BevTools, fixed with tests.**

- Showing *Renders per page* could end a Blazor Server process with a stack overflow: the trailing rebuild asked the
  one-second interval again, and when its timer fired a fraction of a millisecond early it scheduled a `Task.Delay(0)`,
  which completes on the same stack, again and again. It now builds directly.
- Clicks in an extension's panel were recorded as application UI events, and the profiler's *Lifecycle cost per type*
  listed BevTools' own panels and the `Virtualize` of its component tree: the framework's activity and meter name only a
  component type. The registry now remembers the types only BevTools' own UI rendered (`IsBevToolsOnlyType`).
- About → Capabilities said source locations were inferred from type names, that requests from lifecycle methods have
  no origin and that component ↔ DOM mapping was not implemented; it had no rows for forms, page accessibility, retained
  components, navigation guards, error groups, endpoints or comparisons.
- The badge covered the dismiss button of Blazor's own error bar (`#blazor-error-ui`): it moves above the bar while the
  bar shows.
- No reconnect was ever recorded: the hub listened for `components:reconnect-state-changed`, a name no Blazor version
  dispatches. Blazor's is `components-reconnect-state-changed`, raised on the app's reconnect dialog without bubbling,
  so the hub listens in the capture phase. Its *show* and *retrying* happen while the connection is down, when a
  circuit cannot be called - Blazor raises *show* from the socket's close handler, sometimes before the hub hears the
  close - so the hub holds a Server runtime's states until *hide* and delivers them with their age.

The demo itself: the cart's product names broke letter by letter in its narrow pane (now a wrapping list), and with
two Content Security Policy headers - the demo's and Blazor's `frame-ancestors` - WebKit kept only the last (now one
policy).

**Verification.** .NET tests: net10.0 496/496, net8.0 480/480, net9.0 482/482; vitest 65/65. A Playwright check of
each page's claims - about 110 of them, on a Server circuit and on WebAssembly - passes in Chromium and WebKit (WebKit's
WebAssembly run on the interop, errors, routing, checkout and extension pages); in Firefox all but a row click lost
while the request list was still filling, which passes on its own. At phone width no page scrolls sideways, and the
page's accessibility check finds nothing on any page but the two planted problems on Checkout. Benchmark allocations
unchanged (23.9, 23.9, 142.0 and 209.1 MB).

## Round 16 — logs

Test environment from this round: Windows 11, .NET SDK 11.0.100-rc.3 with the .NET 10.0.14 and 11.0 rc.3 runtimes; the
suites target net10.0 and net11.0 (net8.0 and net9.0 are no longer targets). Baseline before the round: 534/534 tests
on each of net10.0 and net11.0, vitest 71/71.

**Gap.** BevTools captured only the warnings and errors Blazor itself logs, as errors. What the application's own
loggers wrote - the first thing a developer reads - was invisible, an exception the app caught and logged never reached
Errors, and nothing connected an entry to the component, click or trace it came from. Every other developer-tool
category has this (a console or log viewer); for Blazor it is also where Entity Framework Core's commands, the HTTP
client's and every library's diagnostics appear.

**Changes**

- `BevToolsLoggerProvider` (alias `BevTools`, `ISupportExternalScope`) records, with `TrackLogs` (on), each entry at
  `MinimumLogLevel` (Information) or above that the host's logging configuration let through: level, category, event id
  and name, message, template and its values, scopes, exception and its inner chain, W3C trace and span id, the UI event
  running, and the sending component (`CaptureLogCallers`, a stack walk per entry, `BevCallSites` as for HTTP). Values and
  scopes go through the inspector (value access, redaction by name and shape); a value redacted by its name is redacted
  in the message as well, which is then rebuilt from the template, as it is under `Shallow`; under `None` the template
  stands for the message. `BevLogRecord`, `BevLogStore` (`MaxLogs`, counts per level for the session),
  `BevToolsEventKind.Log` (an addition to the contract).
- An application entry at Error or above is an error (source *log*); Blazor's own warnings and errors behave as before,
  and an entry that became an error shares its timeline row instead of adding one.
- `SessionResolver`: after the dispatcher checks, the session whose UI event or navigation the current `Activity`
  descends from (`ActivityObserver.SessionOfCurrentActivity`) - work that left the dispatcher after a library's
  `ConfigureAwait(false)` provably began in that session's event. HTTP requests and every other ambient lookup gain it
  too. Under BevTools' own events it is nobody's; outside every session in a server process it still is nobody's.
- Logs panel (entries newest first; `level:` as a minimum, `category:`, `event:`, `trace:`, text; *Categories*; an
  entry's template, values, scopes, sender, *During*, trace, timeline row and error), a warning count on its tab, the
  palette's search, `logs.show` and `logs.warnings`, the timeline's lanes, the performance trace (*Logs* track),
  exports (`logs`, `logCounts`, the options), the Markdown report (logged warnings and errors), the companion and the
  extension (a *Logs* view), About → Capabilities (three *Logging* rows).
- `log.flood` (`LogFloodCount`, 100 within ten seconds) for one category's template logged over and over, with senders.
- Demo: `ProductApi` logs structured entries (`Loaded {Count} products in {ElapsedMs} ms`, a warning when the flaky
  endpoint fails); problem 25 (*rows log every update* on Products: 120 entries per keystroke); Getting started lists
  the Logs tab.

**Verification**

- .NET: 551/551 on net10.0 and on net11.0 (new: `LogsTests` 17 and `LogsPanelTests` 1 - the template, values, level,
  category and event; minimum levels and the host's filter first; a sensitive value redacted in its values, the message,
  the timeline and the export; a token in a plain string, which is its own template; `None` and `Shallow`; scopes with a
  sensitive key; an application error in Errors with one timeline row; Blazor's warnings unchanged; paused capture;
  `TrackLogs` off; BevTools' own work, its own category and a value that logs from `ToString`; nobody's entry in a server
  process; an entry written after leaving the dispatcher attributed through the click's activity, with its trace id and
  trigger; exports and the report; the filter; `log.flood` below and at its threshold; a component logging through a
  service named as *LoggingRow.OnInitializedAsync via LoggingRowService.Load* and the panel's entries, detail and
  categories). Every existing test passes unchanged.
- vitest: 72/72 (the companion's Logs view: the tab's count, a minimum level, an entry's detail, logged markup as text).
- Browser (a Playwright driver against the demo, problem 25, on a Server circuit and then on WebAssembly, in Chromium,
  Firefox and WebKit): the rows' entries listed with their sender and the keystroke, the template and values in the
  detail, *Categories* with `ProductRow` and `ProductApi`, the *Log flood* finding, every control of the panel named;
  no page errors. All passing.
- Overhead: the render path is untouched - 23.7, 23.8, 142.6 and 209.7 MB in the four benchmark configurations, against
  23.7, 23.7, 142.6 and 209.8 MB for the code before the round on the same machine, back to back. Per log entry: 7.9 µs
  and 6.4 KB recording everything, 2.9 µs and 1.8 KB without senders, nothing with `TrackLogs` off (performance.md).

**Found and fixed on the way.** The first export test found a token in the export: an entry logged as a plain string
is its own `{OriginalFormat}`, so the template carried what the message had redacted. Templates are redacted too.

**Limits.** An entry written from a lifecycle method's continuation off the dispatcher with no UI event or navigation
above it (an `OnInitializedAsync` awaiting a library that uses `ConfigureAwait(false)`) has no activity BevTools can
follow on a server, and is nobody's there; on WebAssembly the only session takes it. Entries written before BevTools
attached are not replayed. The sender is a stack walk: a log written after the code went asynchronous names none.

## Round 17 — allocations per render

**Gap.** The profiler said how long each render took, never what it allocated, although on WebAssembly the garbage
collector pauses the only thread the UI has, and allocations - unlike times - repeat from run to run, so they make a
steadier regression gate than durations. The capability matrix had no row for it.

**Changes**

- `BevRenderSample.AllocatedBytes`: the runtime's per-thread allocation counter read right around each instrumented
  `BuildRenderTree`, after BevTools' own work for the render, so it is the component's render alone (its children
  render separately). `BevAllocations.IsMeasurable` probes once whether the counter moves; where it does not, nothing is
  shown as zero. Stored with `Failed` in one int: the sample, the most numerous object BevTools keeps, stays its size.
- `BevComponentRecord`: total, max, since the baseline; components let go fold theirs into the gone totals.
- Profiler: *Allocated* per component and per type (sortable, per render in each row's title); the inspector's
  *Rendering*: per render, max, last; each render's timeline data (`allocatedBytes`); exports (per component, and per
  type in `profile`), the Markdown report (*Allocated per render*); the companion's profiler.
- `BevProfileComparison`: a type allocating `MinAllocationChangeBytes` (1 KB) and `AllocationChangeRatio` (25 %) more
  per render regresses, fewer improves - per render, so rendering more often is not counted twice - when both
  recordings measured; the Markdown table has the column; different render modes warn that allocations are not
  comparable. `bevtools compare` and `Profiler → Compare…` gain it through the same code.
- `render.allocations` (`HeavyRenderAllocationBytes`, 256 KB): a type averaging that much per render over at least
  three renders of the last 30 s. A capability row.

**Verification**

- .NET: 558/558 on net10.0 and on net11.0 (new `AllocationTests` 6: the runtime counts; a parent's render counts its own
  buffer and not its child's; totals, maxima and the timeline data; the baseline window, the profile written and read
  back, the components section; regressions, improvements, a change under the thresholds, a recording without the
  measure, the Markdown column; the rule; the profiler's column and sort). Two profiler tests that read the causes
  column by position now read it one cell further, after the new column; their assertions are unchanged.
- vitest: 73/73 (the companion's column, per type, and none for an export without allocations).
- Browser (the Playwright driver, problem 25's page, Server then WebAssembly, Chromium, Firefox and WebKit): the column
  is there and `ProductRow`'s renders allocated something on both runtimes - Mono on WebAssembly counts allocations per
  thread too (`ProductRow` 3.2 KB, `ProductList` 100 KB on WebAssembly; 992 B and 104.6 KB on a Server circuit).
- Overhead: the first version stored the bytes in a `long?` and every sample grew: 148.7 and 215.9 MB in the two
  recording configurations against 142.6 and 209.7 MB. Packed with `Failed` into one int: 23.7, 23.7, 142.6 and
  209.7 MB, as before the round.

**Limits.** An EditForm's first render with an EditContext includes BevTools' watch of it. A render that allocates
more than about 2 GB is shown as that. Allocations are not comparable between runtimes (Mono and CoreCLR size objects
differently), which comparisons say when the render modes differ.

## Round 18 — responsiveness

**Gap.** BevTools timed renders and handlers in .NET and never asked the browser what a person felt: how long a click
took to show anything, and whether the main thread was blocked. On WebAssembly that thread is the one .NET renders
on, so a slow handler freezes the page - and the browser measures exactly that (Event Timing, long animation frames),
with nothing linking its measurement to the Blazor event that caused it.

**Changes**

- Page hub: `long-animation-frame` (else `longtask`) and Event Timing observers (`TrackResponsiveness`, on), reported
  once a second as ages; an interaction merged across its entries and named after its slowest event; BevTools' own UI
  left out; what the browser supports in the attach snapshot.
- `BevResponsiveness` (the Browser state): recent interactions and long frames, session totals, INP. Each long frame
  and each interaction of 50 ms or more is a timeline event dated by its age. On WebAssembly: the UI event whose handler
  started inside an interaction's processing (exactly one, within 4 ms), and the events and batches inside a frame.
- Browser → *Responsiveness* (INP, slowest interaction, long frames, blocking time, what the browser does not report,
  the circuit caveat; tables of slow interactions with their three parts and handler, and of long frames with what ran
  in them); exports (`browser.responsiveness`, the options and thresholds); two capability rows whose status follows
  the browser.
- `browser.slow-interaction` (`SlowInteractionMs`, 200) and `browser.long-frame` (`LongFrameBlockingMs`, 150).
- Demo: problem 26 (*Recalculate ratings*: a 400 ms synchronous handler) - a frozen page on WebAssembly, a slow
  inbound message on a circuit.

**Verification**

- .NET: 566/566 on net10.0 and on net11.0 (new: `ResponsivenessTests` 7 and `ResponsivenessPanelTests` 1 - parts, INP, timeline events and severities, no match
  on a circuit; on WebAssembly the handler and the work inside a frame, and two handlers naming neither; findings that
  say which part was slow; paused capture, the option, non-finite and oversized payloads, a token in a target; INP's
  outlier rule; exports; the panel and the capability statuses).
- vitest: 75/75 (new: the observers, an interaction across three entries, BevTools' own interaction across two, ages
  and script sources; the long-task fallback; nothing observed when off; observers disconnected on detach).
- Browser (problem 26 on Server and WebAssembly, Chromium, Firefox and WebKit): on WebAssembly *click on
  &lt;button.btn&gt;*, about 408 ms to the next paint, about 402 ms of it processing, *Ran: onclick →
  Products.RecalculateRatings*, and the *Slow interaction* finding, in all three; on a circuit no handler is claimed.
  Chromium reports the long frames; Firefox and WebKit report none, and the panel says so.

**Found and fixed on the way.** The first browser run showed the 400 ms click as a *pointerdown* with 0.1 ms of
processing and 400 ms of "presentation": the hub kept one entry per interaction, and a click's pointerdown entry
carries the click handler's time as its own wait for the paint. Entries are merged now, as the metric defines an
interaction; the vitest case covers it.

**Limits.** Durations are the browser's, rounded to 8 ms. A long frame on WebAssembly can include BevTools' own panel
rendering while it is open. On a circuit an interaction's time is the browser's part only.

## Round 19 — the page's other storage

**Gap.** The Browser panel read localStorage and sessionStorage; cookies, IndexedDB, Cache Storage and service workers
were "out of scope". For a Blazor WebAssembly app installed as a PWA (Bit.Bswup) or keeping data in the browser
(IndexedDB, OPFS), the questions are structural - which caches exist and how full they are, is an update waiting, will
the browser keep the data - and nothing answered them.

**Changes.** The hub's `getAppStorage` (read with local and session storage, on request): cookie names and value
lengths (values never read), IndexedDB databases by name and version (never opened), Cache Storage caches with their
request counts, service worker registrations (active, waiting, installing; URLs redacted) and the controller, usage,
quota, usage per kind and persistence; each part that fails or times out says why. `BevAppStorage`, bounded and
redacted on arrival (`BevToolsPanel.Clean`); the Browser panel's *Read storage*; exports (`browser.storage`); a
capability row; `pwa.update-waiting`.

**Verification.** .NET 569/569 on net10.0 and on net11.0 (new `AppStorageTests` 3: the panel's quota, cookies,
databases, caches and the waiting worker; the finding and the export without values; bounds and redaction of what the
page reported; one existing test clicks the button by its new label). vitest 77/77 (cookie names and sizes and no value,
every API read, a failed part reported, browsers without the APIs). Browser: *Read storage* on the demo in Chromium,
Firefox and WebKit lists every kind and reports the quota (Firefox 84.4 KB of 10 GB, WebKit 0 B of 1 GB), no failure.

**Limits.** Contents stay in the browser's Application panel: no IndexedDB records, cache responses or cookie values.
`indexedDB.databases()` is missing in older browsers, and the panel says so.

## Round 20 — correlation

**Gaps.** A request in the Network panel could not be found in a tracing backend: nothing recorded the trace it
carried. A fire-and-forget task that failed vanished unless the runtime's unobserved-exception event was handled by
the app. A long recording had no way to mark "here I clicked Save".

**Changes**

- `BevHttpRecord.TraceId` and `SpanId`, from the request's `traceparent` header read once it completes (the client
  writes it below BevTools' handler); the request's *Trace* row with *logs in it*, the log entry's *requests in it*,
  `trace:` in the Network filter, exports, the companion, a capability row.
- `BevUnobservedTasks`: `TaskScheduler.UnobservedTaskException` into Errors (source *task*), never marking the
  exception observed; tasks BevTools started left out by their outermost frame below the task machinery.
- Markers: `BevToolsSession.AddMarker`, the Timeline's *⚑ Mark*, `timeline.mark`, the protocol's `mark` command.

**Verification.** .NET 573/573 on net10.0 and on net11.0 (new `CorrelationTests` 3 and `MarkerPanelTests` 1: the trace
from a header written below the handler, none, an all-zero id and garbage refused, the export; an application task, a
task of BevTools' own async method and an application's call through the tracked `IJSRuntime` told apart; markers from
the API, the protocol, with an empty label and while paused; the toolbar's numbered markers). vitest 77/77. Browser
(the demo, Chromium): on a circuit `GET /api/orders` from *onclick → Orders.Load* carried trace
`dad67c25…` with its client span; the products the page loads in `OnInitializedAsync` carried none, and neither did
WebAssembly's requests - as documented.

**Limits.** Whether a request carries a trace is the client's decision (propagation needs a current activity or a
listener); BevTools reads what was sent and never adds a header. Unobserved task exceptions are raised when the task is
collected, so they arrive late, and in a server process they belong to no session.

## Round 21 — state: details, search, restoring

**Gaps (audit D).** Each change's `Detail` was captured and never shown; the State panel could not be searched; and
time travel was "N/A" - right for BevTools writing arbitrary objects, but nothing let a provider that *can* restore
itself offer it.

**Changes.** `IBevStateRestorer` and `BevStateChangeInfo.RestoreToken` (additions to the contract): the provider names
the state after each change and restores itself when asked. `StateStore.RestoreAsync` asks - only where
`BevToolsSession.AllowsStateRestore` (Development or a single-user process), only for a provider that can, only with its
own token - and answers what happened, never throwing; the restore is a timeline event linked to the change. State
panel: *⟲ restore* on restorable changes, details (*Add · Quantum Lamp 3*), a filter over providers, actions, details
and members. The protocol's `restore` command (never blocking the dispatcher). Exports mark changes `restorable`, never
with the token. A capability row whose status follows the gate. Demo: `CartState` keeps its last hundred versions and
restores them (problem 10).

**Verification.** .NET 578/578 on net10.0 and on net11.0 (new `StateRestoreTests` 5: a restore back to the first of
three changes, the restore recorded as a change and a timeline event linked to it, a change no longer held; tokens
dropped for a provider that cannot restore, a failing restore answered and recorded as BevTools' own error; refused on a
Server session outside Development, with the capability *Not available*; the protocol by change id; the export's
`restorable` without the token; the panel's detail, filter and ⟲). The first panel test failed because the session was
a Server circuit outside Development: the gate worked, and the test now runs in a Development host. vitest 77/77.
Browser (the demo, a Server circuit, Chromium, Firefox and WebKit): three products added, ⟲ on the first change, the
page's cart back to one item and a *Restore* change in the history.

**Limits.** Restoring is as good as the provider's own implementation. A provider that restores asynchronously,
asked through the protocol, is reported as started; the outcome is in the timeline.

## Round 22 — memory over time

**Gap.** The Runtime panel showed the heap and collection counts at the moment it was opened: no trend, no link
between a collection's pause and what the app was doing, no WebAssembly memory, and nothing that said "this keeps
growing" - the outside view of every leak the retained-components check cannot name.

**Changes.** `BevMemoryTrend` (a reading a second from the panel's refresh loop, ten minutes kept): the heap, full
collections of the highest generation (gen 1 on Mono), the total pause, the heap after each full collection, and on
WebAssembly the linear memory (the hub's `wasmMemory`, read in-process). Each full collection is a timeline event.
Runtime → *Memory over time* (a sparkline with the collections marked, the window's range, the heap after the last
collection, WebAssembly memory and its rise). `memory.growth`. Exports (`memory`). A capability row. A failure to read
the counters is recorded once as BevTools' own error instead of disappearing.

**Verification.** .NET 583/583 on net10.0 and on net11.0, twice in a row (new `MemoryTrendTests` 4 and
`MemoryTrendPanelTests` 1, fed readings: once a second, an exact and an inferred collection with their events, a
climbing heap found and a sawtooth not, exports, the runtime's own counters, the panel's sparkline, mark and
WebAssembly memory). vitest 78/78. Browser (the demo, Chromium, Firefox and WebKit, Server then WebAssembly): the trend
on both runtimes, and WebAssembly memory of about 96-98 MB next to a managed heap of 5-7 MB.

**Found and fixed on the way.** The first browser run had no trend on WebAssembly at all, and nothing said why: the
sampler swallowed the exception. Recording it showed Mono's `GCMemoryInfo` is a default whose getters throw a
`NullReferenceException`; every member is read guarded now, and a failure is recorded once.

**Limits.** Readings come from the panel component's loop: an app that does not render `<BevToolsPanel />` (an
external frontend only) has no trend. On Mono the heap after a collection is the next reading, not the runtime's own
figure. A heap snapshot of objects and their paths stays with dotnet-gcdump and the browser's memory tools.

## After round 22

Seven rounds (16-22), each verified in tests and in real browsers before the next began.

- .NET tests: 534 → 583 on each of net10.0 and net11.0, all passing, twice in a row after the last round. vitest:
  71 → 78. End to end (`Bit.BevTools.Tests.E2E`, the published, trimmed, lazy-loaded sample): 2/2.
- Browser: a Playwright driver against the demo ran every round's checks on a Server circuit and on WebAssembly, in
  Chromium, Firefox and WebKit - logs, allocations, responsiveness (problem 26), storage, request traces, state
  restoring and memory over time - with no page errors.
- Overhead: the render path's allocations are what they were before round 16 (23.7-23.8, 23.7-23.8, 142.6 and
  209.7-209.8 MB in the four configurations, run back to back with the code before the work); the per-entry cost of
  logs is measured in performance.md, and every other addition is off the render path.
- New diagnostics rules: `log.flood`, `render.allocations`, `browser.slow-interaction`, `browser.long-frame`,
  `pwa.update-waiting`, `memory.growth`. New contract members (additions only): `BevToolsEventKind.Log`,
  `IBevStateRestorer`, `BevStateChangeInfo.RestoreToken`.
- What is left, and why, is in roadmap.md (*Next*, *Later / investigated*) and audit.md (area Q).

## Round 23 — closing the open items

Everything the roadmap's *Next* and the audit's area Q listed, taken to an end: done, or left with the reason found.

**Changes**

- **MCP and CLI formats.** A separate session had built `bevtools mcp` (a read-only Model Context Protocol server over
  stdio, serving only the export files it is given: findings, errors, groups, components, requests, endpoints, timeline,
  logs, check, compare; every answer bounded) and `--format json|sarif|github` for `check`, then stopped without merging.
  Reviewed and brought in; its `get_logs` tool, written before exports had logs, finds an entry by its template too, and
  its test now strips the section to stand for an older export. `compare` takes `--min-allocation-change-bytes` and
  `--allocation-change-ratio`, and its JSON carries the allocations. `Bit.BevTools.Cli` is in the release workflows
  (as `Bit.Cli` is); the package installs and runs as a tool.
- **Entity Framework Core.** `BevDbCommands`: EF's command entries (events 20101, 20102) read while their values are
  still objects - SQL at Full and Shallow access, redacted, and the elapsed time; Logs → *Database*; `db.per-item`
  (`DbPerItemCount`) and `db.slow` (`SlowDbCommandMs`); exports and the companion.
- **Bodies, opt-in.** `CaptureHttpBodies` (off), `MaxHttpBodyBytes`: text only, redacted, never a stream, a response
  only with a known length; at Full access where a forced collection is allowed. gRPC, server-sent events and GraphQL
  (with the operation when bodies are read) are told apart. The demo turns bodies on and sends `/api/profile` with
  its length.
- **The companion's batch views.** Protocol `batch` (`{ batchId }`): the cost tree and the time axis the Profiler
  shows; the companion's Profiler lists the timeline's batches and opens one on a live page.
- **BevTools' own rendering in long frames.** The render tracker remembers when BevTools' own components rendered
  (a fixed array, written only for them); a long frame on WebAssembly says how much of it was the panel, and one that
  was mostly the panel is not the application's blocked thread.
- **`Bit.BevTools.Blex`.** A package of its own against the public contract: the *Blex* state provider, a change per
  action with its payload redacted, restoring through Blex's time travel; `AddBevToolsBlex()`; in the release
  workflows; the BevTools CI also runs when Blex changes. The inspector flattens JSON for it (any provider that
  snapshots JSON gets real diffs).
- **Looked at and left:** WebAssembly heap snapshots (`collectGcDump` is undefined without a diagnostics build),
  runtime container resolutions (would report dependencies as never resolved), a Fluxor package (not in this
  repository; the adapter overload covers it). See roadmap.md.

**Verification**

- .NET: 611/611 on net10.0 and on net11.0 (new: `DbCommandsTests` 5 against EF Core 10 and 11 on SQLite - twelve
  queries for twelve rows and the finding, a failed command, value access None, a slow command, elapsed parsing -
  and `DatabaseViewTests` 1; `HttpBodiesTests` 5 - off by default, JSON both ways redacted with the server and the app
  getting exactly their bytes, a stream and an unknown length left alone, forms and limits, Shallow not honoured,
  gRPC / SSE / GraphQL; `BlexIntegrationTests` 3 with a store from Blex's own generator; the protocol's `batch`; an
  own-rendering long frame; JSON flattening; the CLI's allocation thresholds; the MCP and format tests brought in).
- vitest 79/79 (the companion's batch view on a live page, its database commands, names as text).
- Browser (the demo, Chromium, Firefox, WebKit, Server and WebAssembly): `/api/profile`'s response body shown with the
  e-mail and the access token redacted; every check of rounds 16-22 again.
- Packages: `Bit.BevTools.Cli` and `Bit.BevTools.Blex` pack (the latter depending on `Bit.BevTools` and `Bit.Blex` of
  the same version) and the tool installs and runs; the browser extension assembles.
- End to end: 2/2 on a fresh publish of the lazy loading sample. Benchmark allocations 23.7, 23.7, 142.6 and 209.7 MB,
  as before round 16; times were noise on a loaded machine (the disabled configuration, which runs none of BevTools'
  code, measured +130 %).
- Found while verifying: fifty Playwright browser processes left by timed-out runs had starved Firefox into failing
  checks that pass on a clean machine; only processes under the Playwright browsers folder were stopped.

**Found on the way.** EF Core 10 and 11 log the elapsed time as a culture-formatted string; a seekable stream reports
its length (so it is safe to read, and is), while a forward-only one does not; JSON snapshots were never diffed, since
the inspector shows JSON as text - now flattened for diffs.
