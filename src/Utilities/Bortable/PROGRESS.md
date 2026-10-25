# Bortable improvement programme: progress record

This file tracks the audit, the competitor matrix, the gap backlog and the verification evidence of the effort to
make Bortable a complete sortable and drag-and-drop library. It is a working record for maintainers, not user
documentation: the README is the user guide.

## Baseline (round 1)

- Branch `msynk/14189-bortable-improvements`, on top of `80762ae405`.
- `Bit.Bortable` builds for net8.0/net9.0/net10.0 with no warnings; trimmable; no runtime dependency beyond
  `Microsoft.AspNetCore.Components.Web`.
- `Bit.Bortable.Tests` (bUnit, net10.0): 131 / 131 passing.
- `Bit.Bortable.Tests.Mcp` (demo MCP server): 111 / 111 passing.
- `Bit.Bortable.Tests.E2E` (Playwright, Chromium, real mouse drags against the demo): 58 / 58 passing, 3 min 05 s.

## Assessment of the starting point

Bortable is a careful port of SortableJS onto Blazor: a C# engine (`Bortable<TItem>` + the scoped
`BortableService` coordinator) driven by Blazor's handlers for the native HTML5 drag events, and a small TypeScript
module for the hit test (a faithful port of SortableJS's swap-threshold logic), drag handles and FLIP animations.
Within that model the engine is solid: live reorder through `@key`, a placeholder preview across lists, clone /
none pull modes, put, sort, filter, swap and multi-drag, nested lists with cycle prevention, a serialized gate,
round-trip detection, a single `ItemsChanged` per changed list, a deterministic event order, mid-drag disposal.

The gaps are structural rather than local:

1. **Every `dragover` is a .NET round trip, and every reorder two or three more.** Blazor dispatches each
   `dragover` (~20/s while the pointer rests, more while it moves) to .NET, which calls back into JS for the hit
   test and awaits a FLIP snapshot of each list before mutating. On Blazor Server that is several network round
   trips per pointer step, and the gate drops events while it is busy, so the last pointer position may never be
   acted on (the E2E harness nudges the pointer to compensate).
2. **Mouse only.** No pointer-event engine: no touch on Firefox for Android or in web views without HTML5 DnD, no
   pen-specific behaviour, no long-press activation, no activation delay or tolerance.
3. **No keyboard operation and no accessibility layer.** Items are not focusable, nothing is announced, there is no
   non-drag way to move an item, no ARIA. Fails WCAG 2.1.1 (keyboard) and 2.5.7 (dragging movements).
4. **No validation or rollback.** No veto callback (SortableJS `onMove`), no capacity limit, no async confirmation,
   and Escape or a release outside every list commits what the drag did (SortableJS semantics) with no opt-in revert.
5. **No auto-scroll of our own.** Only the browser's native drag auto-scroll, which differs between browsers.
6. **Hit-test defects:** a horizontal list laid out right-to-left (RTL or `row-reverse`) resolves "before/after" the
   wrong way in the gaps and in the inverted zones; an invalid `Handle` selector throws on every `pointerdown`.
7. **Limited observability:** `OnEnd` does not say whether the drag was cancelled; events do not carry the input
   modality or whether the move was a clone.
8. **No programmatic API** (move an item, move it to another list) that an action menu could use.

## Competitor matrix

Legend: ✓ supported, ~ partial, ✗ not supported. "Bortable (before)" is the baseline above.

| Capability | SortableJS 1.15 | dnd-kit | Pragmatic DnD | Angular CDK | BlazorSortable / FluentSortableList | MudDropContainer | Bortable (before) |
| --- | --- | --- | --- | --- | --- | --- | --- |
| Engine | native DnD + touch fallback | pointer/keyboard sensors | native DnD | pointer events | SortableJS via interop | native DnD in C# | native DnD in C# |
| Live reorder in list | ✓ | ✓ | app-driven | ✓ | ✓ (SortableJS) | ✗ (on drop) | ✓ |
| Connected lists, pull/put rules | ✓ (functions) | ✓ | app-driven | ✓ (connectedTo, enterPredicate) | ✓ | ✓ (CanDrop) | ✓ (bool/enum) |
| Clone | ✓ | app-driven | app-driven | ✗ | ✓ | ✗ | ✓ |
| Veto during drag | ✓ `onMove` | ✓ | ✓ `canDrop` | ✓ predicates | ~ | ✓ | ✗ |
| Async confirm / rollback | ✗ | app-driven | app-driven | ✗ | ✗ | ✗ | ✗ |
| Swap mode | ✓ plugin | ✓ strategy | app-driven | ✗ | ✗ | ✗ | ✓ |
| Multi-drag | ✓ plugin | ✗ | ✗ | ✗ | ✗ | ✗ | ✓ (click) |
| Nested / tree | ✓ | ✓ | ✓ (tree hitbox) | ~ | ~ | ✗ | ✓ |
| Touch | ✓ fallback | ✓ | native | ✓ | ✓ | ✗ | ✗ |
| Activation delay / tolerance | ✓ | ✓ | ✗ | ✓ | ✓ | ✗ | ✗ |
| Keyboard dragging | ✗ | ✓ | ✗ (menus) | ✗ | ✗ | ✗ | ✗ |
| Live-region announcements | ✗ | ✓ | ✓ package | ✗ | ✗ | ✗ | ✗ |
| Auto-scroll | ✓ plugin | ✓ | ✓ package | ✓ | ✓ | browser | browser |
| Collision strategies | swap thresholds | 4 built-in + custom | hitbox | pointer | SortableJS | n/a | swap thresholds |
| Programmatic moves | ✓ `sort()` | app state | app state | helpers | ✗ | ✗ | ✗ |
| Strongly typed C# state | n/a | n/a | n/a | n/a | ~ | ✓ | ✓ |
| Server round trips per pointer step | n/a | n/a | n/a | n/a | 0 (JS) | many | many |

Differentiation to aim for: the only Blazor library that keeps the engine and the typed state in C#, keeps the hot
path in the browser, and adds keyboard dragging, announcements, validation with rollback and touch.

## Architecture decision (round 2)

| Decision | Rationale |
| --- | --- |
| The browser layer routes input. JS owns `dragstart`/`dragover`/`drop`/`dragend` (and, later, pointer and keyboard input), runs the hit test where the pointer is, and calls .NET only when the drop position changes. | Removes the per-`dragover` .NET dispatch and the JS round trips inside it. Gives one router that native drags, pointer drags and keyboard drags share. Serializes the calls instead of dropping events, so the last pointer position is always acted on. |
| .NET stays the authority on state and policy: which lists take a drag (answered once, at start), what a position means, every mutation, every event. | Keeps the typed engine, the tests of its rules and the event contract. |
| Items carry a stable per-list key (`data-bortable-key`), so the browser names an item without an index. | An index read from the DOM can be stale on Blazor Server; the key cannot. |
| FLIP snapshots are taken in the browser right before the call that may change a list, and played when Blazor's mutation of the list arrives. | No awaited round trip before a mutation; the inverted frame lands before the first paint. |
| JS → .NET calls go to an internal bridge object, not to public methods of the component. | Keeps `[JSInvokable]` plumbing out of the public API. |

## Gap backlog

Priority: P0 correctness/accessibility blocker, P1 essential, P2 valuable, P3 nice to have.
Status: Open, **Done** (round), Deferred (reason).

| # | Gap | P | Status |
| --- | --- | --- | --- |
| G1 | Per-`dragover` .NET dispatch and awaited JS calls (latency on Server, dropped final positions) | P0 | **Done** (R2) |
| G2 | RTL / reversed horizontal lists resolve before/after wrongly in gaps and inverted zones | P1 | **Done** (R2) |
| G3 | Invalid `Handle` selector throws on every pointerdown | P2 | **Done** (R2) |
| G4 | Keyboard dragging (pick up, move, cross-list, drop, cancel) with focus retention | P0 | **Done** (R4) |
| G5 | Live-region announcements, instructions, ARIA semantics, localizable texts | P0 | **Done** (R4) |
| G6 | Non-drag alternative: programmatic move API usable from action menus | P0 | **Done** (R3, announced R4) |
| G7 | Veto predicate during drag (`CanDrop`), capacity (`MaxItems`) | P1 | **Done** (R3) |
| G8 | Revert on spill/cancel, async confirmation with rollback | P1 | **Done** (R3) |
| G9 | Event args: cancellation, input modality, clone flag | P1 | **Done** (R3) |
| G10 | Pointer-event engine for touch and pen, activation delay and tolerance, Escape/pointercancel | P1 | **Done** (R5) |
| G11 | Auto-scroll of scroll containers and the viewport, nested scrollers | P1 | **Done** (R6, pointer drags; native left to the browser) |
| G12 | Drop-target feedback (accepting / over / refused lists) without re-rendering | P2 | **Done** (R6) |
| G13 | Reduced motion, forced-colors, focus-visible styles | P1 | **Done** (R2 motion, R4 styles) |
| G14 | Multi-drag: range selection, bindable selection | P2 | **Done** (R7) |
| G15 | Global defaults through the service registration | P2 | **Done** (R8) |
| G16 | `AdditionalAttributes` on the container (aria-label, data-*) | P2 | **Done** (R4) |
| G17 | Large lists: render cost per drag step | P2 | **Done** (R7: `IsolateItems`, measured) |
| G18 | Demos and README for every new capability; MCP catalog in step | P1 | **Done** (R9) |
| G19 | Virtualization (`Virtualize`) | P3 | Deferred: unrendered items can be neither measured nor dropped beside; documented, with paging / `IsolateItems` as the alternatives |
| G20 | Tree-specific drop-onto-node and expand-on-hover | P3 | Deferred: nested lists cover "into a node" (an empty child list is a drop zone); a tree presentation is the app's |

## Rounds

### Round 1 - audit and baseline

Read every file of the library, its tests (bUnit, E2E, MCP), the demo, the README and the CI workflows; ran the three
suites (above); compared against SortableJS, dnd-kit, Pragmatic drag and drop, Angular CDK, BlazorSortable /
FluentSortableList and MudBlazor's drop container (current docs, see the matrix). Mobile browser facts checked: Chrome
for Android and iOS Safari 15+ support native drag and drop on long press, Firefox for Android does not, so a
pointer-event engine is still needed for touch. Pragmatic DnD's accessibility guidance (action menus, announcements
with old and new position, focus restoration) and dnd-kit's keyboard sensor (Space/Enter pick up and drop, arrows
move, Escape cancels) informed G4-G6.

Next: round 2, the browser-routed input architecture (G1-G3), with the existing suites as the regression oracle.

### Round 2 - the browser routes, .NET decides (G1-G3)

- `bit-bortable.ts` now owns the native drag events. Lists register (`register`/`configure`/`unregister`) with an
  internal `BortableBridge` (`[JSInvokable] Start/Over/End`) and their hit-test options (`BortableJsOptions`, a
  record so an unchanged set is never resent). `Start` answers the ids of the lists that take the drag; the router
  sends `Over(index)` only for a new position, and serializes calls (the latest position waits for the call in flight
  instead of being dropped). Blazor's `@ondragstart/@ondragover/@ondrop/@ondragend` and the per-drag stop-propagation
  re-renders are gone.
- Items carry `data-bortable-key` (per-scope unique, stable while the item stays in the list); `Start` names the
  item by key, so a stale index on Blazor Server cannot pick the wrong item.
- FLIP snapshots are taken in JS right before a call and played from a `MutationObserver` the moment Blazor's
  batch lands (before paint); the C# hot path no longer awaits any JS call. Reduced motion skips the slides.
- The engine methods became synchronous (no awaits inside the gate), removing the staleness checks they needed.
- RTL / `row-reverse` / `column-reverse` lists are measured in their own order (G2). An invalid handle selector warns
  once instead of throwing (G3). A lost `dragend` is detected by the next mouse move (500 ms after the last
  dragover) and reported as `abandon`.
- Tests: bUnit harness drives the bridge instead of DOM events (helpers kept their names); E2E `BrowserLayerTests`
  rewritten for the session-aware drop rule plus RTL/reversed and invalid-handle cases. MCP drag lab calls the
  bridge's `[JSInvokable]` methods by reflection, as the browser does. Result: 138 unit, 111 MCP, 61 E2E passing.

### Round 3 - the operation model (G6-G9)

- `CanDrop` (per position, before anything shows; the SortableJS `onMove`), `MaxItems` (a full list does not take
  the drag at all), `ConfirmDrop` (async, optimistic: the drop shows, a `false` restores every changed list from the
  pre-drag snapshots, with a slide back), `RevertOnSpill`.
- End kinds: `drop`, `release` (native Escape is indistinguishable and counts as a release, preserving SortableJS
  semantics), `cancel` (always puts back: identity-based, so an item the app added mid-drag survives), `abandon`.
- `Over` answers 1/0/-1; a refused position shows `dropEffect: none` and resets the stateful hit test.
- Event args gained `Input` (`BortableInput`), `IsClone`, `Canceled`. `MoveAsync` / `MoveToAsync` run a whole drag in
  one call through the same rules and events (`Input = Api`).
- Regression found by E2E and fixed: native `dragend` with `dropEffect: none` was reported as `cancel`, which now
  reverts; native always reports `release`.
- Result: 171 unit, 111 MCP, 61 E2E passing.

### Round 4 - keyboard and accessibility (G4, G5, G13 partly)

- Lists render `role=list`, items `role=listitem` with a roving `tabindex` (one tab stop per list, C#-owned, updated
  through `Focus(key)`), `aria-roledescription`, `aria-describedby` to hidden instructions; `AriaLabel`,
  `ItemLabel`, `Messages` (`BortableMessages`, all texts localizable), `AdditionalAttributes` on the container.
- Keyboard drag: Space/Enter pick up and drop, Escape cancels, Tab drops, Home/End, arrows. The browser layer finds
  the next item - or the nearest accepting list - *in the direction of the arrow* from the layout, so vertical,
  horizontal, grid, RTL and boards behave alike; .NET moves the item itself (cross-list too, with clone copies that
  come and go), answers the announcement and the item's new key/list, and the focus follows.
- Announcements through one assertive live region: pick-up and end for every input, every step for the keyboard,
  and `MoveAsync` results. Focus-visible and forced-colors styles; keyboard drags are "chosen", not ghosted.
- Result: 196 unit, 111 MCP; E2E keyboard suite added (focus roving, live move, Escape, cross-list carry, keys inside
  an item left alone).

### Round 5 - pointer engine for touch and pen (G10)

- Touch and pen (and the mouse with `ForceFallback`) drag with pointer events through the same router: a long press
  (`TouchDelay`, default 200 ms) or `Delay` with `DelayTolerance`, a 4 px threshold, a preview cloned from the item
  (`DragClass`, computed style copied minus what the class owns), `elementFromPoint` routing, a non-passive
  `touchmove` on each list so the page cannot scroll under a dragging finger, `contextmenu`/`selectstart`
  suppression during the press, click suppression after the drop, Escape/`pointercancel`/blur/tab-hide cancel.
  The mouse's native drag honours `Delay` by holding `draggable` off.
- New demo page `/touch`. E2E drives real touch events and pen events through CDP.
- Result: 197 unit, 111 MCP, 76 E2E passing.

### Round 6 - feedback and auto-scroll (G11, G12)

- The browser layer marks lists with `data-bortable-accepts` / `-over` / `-refused` and `<html data-bortable-dragging>`
  (attributes Blazor never renders, so no render cost); the stylesheet draws them only through opt-in custom
  properties, so existing apps look the same. A native clone drag shows the copy cursor; a refused position the
  no-drop cursor.
- Auto-scroll for pointer drags (`Scroll`, `ScrollSensitivity`, `ScrollSpeed`): per frame while the pointer rests in an
  edge band, innermost scroller that can still scroll that way first, then the page; each scroll re-runs the hit test.
  Native drags keep the browser's own auto-scroll (as SortableJS does on Chrome/Firefox).

### Round 7 - selection and performance (G14, G17)

- `@bind-SelectedItems`, Shift+click ranges, Shift+Space from the keyboard (announced), selection published when a
  drag spends or clears it.
- `IsolateItems`: each template in a `BortableItemView` that skips renders unless the item, the template or the
  list's parameters changed.
- `diagnostics()` / `resetDiagnostics()` in the module.
- Measured (E2E `LargeListTests`, 300 rows, Chromium, Debug WebAssembly on the dev machine): a drag of 60 pointer
  moves sent 15 positions to .NET - one per row crossed - with an average round trip of ~12 ms (Over + render + FLIP
  arm); slowest 29 ms without `IsolateItems`, 13 ms with it. Before round 2 every `dragover` (more than the 60 moves:
  Chromium repeats them every 50 ms) was a .NET dispatch plus an awaited JS hit test and up to three awaited FLIP
  snapshots.

### Round 8 - app-wide defaults (G15)

- `AddBitBortableServices(options => ...)` (`BortableOptions`): defaults applied at the first `SetParametersAsync` to
  every parameter the page does not set; several calls compose.

### Round 9 - demos and documentation (G18)

- New pages: `/accessibility` (keyboard, announcements, move buttons), `/rules` (MaxItems, CanDrop, ConfirmDrop with a
  refusing server, RevertOnSpill, target outlines), `/touch`, `/large-list`; all swept by `DemoPagesTests`.
- README: rules, cancelling, moves from code, touch/pen/mouse, keyboard, accessibility, valid/invalid targets,
  performance, app-wide defaults, lifecycle stages, API tables, SortableJS mapping and limitations updated; the MCP
  parity tests enforce the tables.
- E2E `RulesTests` found a real defect: after `ConfirmDrop` refused, a page that had rendered the optimistic drop was
  never told the lists were put back. `ItemsChanged` is now raised for the restored lists (README and tests updated).

### Round 10 - independent audit and release checks

A fresh reviewer (no access to this record) read the library end to end. Findings and their fate:

| # | Finding | Fix | Evidence |
| --- | --- | --- | --- |
| R1 | A late `End`/`Over` of one drag could end or steer the next (Server latency); a new `Start` could abandon a drop still on its way | Drag ids (`BortableDragContext.Id`, sent back with `Over`/`End`/`Key`; .NET ignores another drag's calls); the next drag's `Start` waits for the previous `End` (`endsInFlight`) | `ReviewRegressionTests` |
| R2 | During `starting`, any list - including one inside the dragged item - was a target, so a quick drop could nest an item in itself | `wouldNest` applies in every state, and to the target kept from `starting` | code path |
| R3 | A delayed mouse press that moved too soon left the item undraggable for good (and a listener behind) | `holdNativeDrag` restores the rendered `draggable` on every end of the press | `BrowserLayerTests` |
| R4 | A keyboard drag carried out of a swap list kept the swap highlight, and the drop swapped | `MoveByKey` clears the swap target | `ReviewRegressionTests` |
| R5 | A keyboard clone brought home landed one place too far | gap index adjusted for the original still in the list | `ReviewRegressionTests` |
| R6 | `Confirming` dropped before a refusal had restored the lists | raised until the restore is done | code path |
| R7 | An `OnStart` that threw left a stale drag (and `MoveAsync` skipped its report) | the drag is cancelled and reported, then the exception passes on | `ReviewRegressionTests` |
| R8 | Space during a pointer drag orphaned its preview and marks | keys start nothing during a pointer or native drag; `pickUp` cleans up a previous session | `PointerTests` |
| R10 | During a keyboard drag, Enter/Space/Escape were taken from a `ConfirmDrop` dialog | only keys on the dragged item are the drag's | `KeyboardTests` |
| R11 | Tab or a click that ended a keyboard drag pulled the focus back | no refocus for those drops | `KeyboardTests` |
| R12 | The first announcement could be missed (region created with its text) | region created when the first list registers; repeated text re-announced by clearing first | code path |
| R13 | Inert-ness of the placeholder and the preview; `ChildContent` inside `role=list` | `inert` on both; README tells how to label `ChildContent` | markup |

Release checks (all run, all passing):

- `dotnet build Bit.Bortable.slnx` (net8/net9/net10, every project): 0 errors; the one warning is the pre-existing
  CS8602 in `McpSurfaceTests.cs:58` (present at the baseline). Two ambiguous `cref`s introduced by the new overload
  were fixed.
- `Bit.Bortable.Tests`: 654 / 654 (218 tests x net8, net9, net10). `Bit.Bortable.Tests.Mcp`: 111 / 111.
- `Bit.Bortable.Tests.E2E` against the Debug build: 89 / 89 (4 m 57 s).
- The demo published trimmed in Release (as `bortable.demo.cd.yml` does, `InvariantGlobalization=true`): no trim
  warnings; the full browser suite against it: 89 / 89 (4 m 16 s) - the interop payloads survive trimming.
- Benchmark on that publish (`LargeListTests`, 300 rows; Playwright's Chromium, headless; Intel Core i9-14900K,
  Windows 11): a drag of 60 pointer moves fired 43 `dragover` events and sent 15 positions to .NET; average round
  trip 6.6 ms, slowest 22.9 ms - with `IsolateItems` 4.6 ms and 7.5 ms. Under Playwright, Chromium fires `dragover`
  only on a move; a real browser also repeats it every 50 ms while the pointer rests, none of which reaches .NET now.

### Round 11 - every demo page in Chromium, Firefox and WebKit

The E2E suite gained `BORTABLE_E2E_BROWSER` (`chromium` | `firefox` | `webkit`; the CDP touch and pen tests are
Inconclusive outside Chromium) and ran in all three engines; on top of it, Playwright scripts swept every page in each
engine: load without console errors, narrow widths (320, 375, 768) without overflow, dark mode, mid-drag states,
touch and pen drags through synthetic pointer events (so the pointer engine ran in Firefox and WebKit too), the
rules board, the event log, the threshold sliders, keyboard drags on every plugin page, selection ranges, the large
list's auto-scroll and keyboard. Findings and their fate:

| # | Finding | Fix | Evidence |
| --- | --- | --- | --- |
| X1 | WebKit (Playwright's Windows build) carries none of a drag's data to the events of the drag - `types` lists only `text/plain`, `text/uri-list`, `text/html` - so no native drag was ever allowed to drop: 35 of the suite's tests failed there | The first drag event after the session's own `dragstart` decides whether the browser shows the mark; where it does not, the drag in progress is taken for the lists' (never one carrying files), as SortableJS does. A `dragstart` the lists do not own ends a native session whose end was lost | WebKit suite green; `BrowserLayerTests` adjusted for the one check such a browser cannot make |
| X2 | An interrupted FLIP slide's `transitioncancel` arrives a frame later at every listener on the element, and the new slide's took it for its own end: the item kept sliding (on the stylesheet's transition) while the hit test and the keyboard took it for settled | A transition event that arrives while the item is still offset (translated) is ignored: a slide's own end leaves none | `KeyboardTests` (below) |
| X3 | Arrow keys pressed faster than the steps return were lost: each one's target was measured at keydown, from items seen mid-slide, and a waiting step was replaced by the next - 15 presses moved an item one place | Arrows, Home and End queue as keys and are resolved when their turn comes, from the laid-out boxes (`layoutRect`, the slides' offsets taken off); a held key's repeats are let go while a step is in flight | `KeyboardTests.Keys_pressed_faster_than_the_steps_come_back_each_move_the_item_one_place` |
| X7 | In WebKit (and Firefox), a keyboard-dragged item could leave its scroll container's view: a moved element keeps the focus there (Chromium takes it away, and the refocus scrolled), and nothing else brought it into view | Every step reveals the item (`scrollIntoView` nearest, a scroll margin covering the slide still to come), and again once its slide is over, for WebKit, which leaves the margin out | `KeyboardTests.Keys_pressed_faster_than_the_steps_come_back_each_move_the_item_one_place` |
| X4 | The demo's card styles for the swap target and the multi-drag selection used `--bit-bortable-swap-*` / `-selected-*` without fallbacks, properties the library only uses as fallbacks: the declarations were void, so a selected card showed bare and the swap target lost its outline | The library's fallbacks repeated in `demo.css` | screenshots; a static check of every `var()` in the demo's CSS |
| X5 | `/accessibility` said → carries an item to the Sprint; where the columns stack, ↓ does | Text says both | narrow-width keyboard check |
| X6 | `LargeListTests` asserted Row 1 past index 15, which it reaches only when the panel has scrolled a little more than the poll waits for | Asserted past the rows that were in view at pick-up, the stated intent | - |

Not defects of the library or the demo: under heavy memory pressure (other sessions' browsers on the machine) Chromium
pages sometimes never became interactive - WebAssembly downloads took 20-25 s and renderers crashed - and the
shared site kit's search ranks "Swap thresholds" above "Swap" for "swap" (every Utilities demo has the same palette).

## Remaining limitations and follow-ups

- Virtualization (G19) and tree-specific drop-on-node / expand-on-hover (G20) are deliberate boundaries, documented.
- Keyboard dragging in screen readers' browse modes depends on the reader passing keys through; `MoveAsync` /
  `MoveToAsync` buttons are the documented alternative (demonstrated on `/accessibility`). Not yet verified by hand
  with NVDA/JAWS/VoiceOver - recommended before a stable release.
- The E2E suite runs in Chromium, Firefox and WebKit (round 11), but the CI workflow still runs Chromium only, and the
  touch engine has run on real touch input only in Chromium (CDP) - not yet on real mobile devices.
- `Over` positions are measured against the layout at the time of the step; content that resizes mid-drag (images
  loading) is measured again on the next pointer move, not before.
