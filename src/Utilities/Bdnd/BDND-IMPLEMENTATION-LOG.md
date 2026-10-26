# Bit.Bdnd implementation log

What each improvement round set out to do, what it changed, how it was validated and what it left for the next
one. The plan is [BDND-ROADMAP.md](BDND-ROADMAP.md); the per-capability status is
[BDND-FEATURE-MATRIX.md](BDND-FEATURE-MATRIX.md).

## Baseline (before round 1)

Environment: Windows 11, .NET SDK 10.0.402 (8.0.414 also installed), Node 24, Playwright Chromium 1248.

| Check | Command (from `src/Utilities/Bdnd`) | Result |
| --- | --- | --- |
| Build | `dotnet build Bit.Bdnd.slnx` | Succeeded; 1 warning, pre-existing, in `Tests/Bit.Bdnd.Tests.Mcp/McpSurfaceTests.cs` (CS8602). |
| Unit tests | `dotnet test --project Tests/Bit.Bdnd.Tests/Bit.Bdnd.Tests.csproj` | 657 passed (219 tests x net8.0, net9.0, net10.0). |
| MCP tests | `dotnet test --project Tests/Bit.Bdnd.Tests.Mcp/Bit.Bdnd.Tests.Mcp.csproj` | 101 passed. |
| E2E | `cd Tests/Bit.Bdnd.Tests.E2E && dotnet test` | 49 passed (Chromium, WebAssembly demo). |

Findings from reading the code (before any change):

- `HandleDragEnd` (which runs `OnEnd`) and `HandleNativeDrop` (which runs `OnNativeDrop`) are called from the
  module through `invokeMethodAsync`, and the module discards the returned promise's rejection. An exception
  thrown by those application callbacks therefore never reached the renderer, an `ErrorBoundary` or the console.
- Hover and drop logic is split between managed events (`ondragstart`, `ondragover`, `ondrop`) and JS-invoked
  methods; nothing but a native drag can drive a drag, which blocks keyboard and touch input.
- The sortable recipe measures the hovered element over interop on every `dragover` it acts on.

## Round 1 - reliability and accessibility foundation

### Reassessment and priorities

The two release-blocking gaps were accessibility (no keyboard alternative, nothing for screen readers) and the
lost exceptions; both were taken first. Touch support (the other P0) needs the backend-neutral drop and hover paths
this round builds, so it follows in round 2.

### What changed

- **Exceptions reach the app.** `OnEnd`, `OnBegin` (when published from a JS-invoked call), `OnNativeDrop` and every
  callback a keyboard drag runs are guarded: an exception goes to `ComponentBase.DispatchExceptionAsync`, so an
  `ErrorBoundary` or the app's error UI sees it, and the drag state is cleared regardless. Before, these ran from
  `invokeMethodAsync` and their exceptions only rejected a promise the module discarded. `EndAsync` also no longer
  leaves the drag stuck when a pending `OnBegin` throws while the end publishes it.
- **Backend-neutral target chain.** Drop targets register an internal handler (`IBdndDropTargetHandler`) with the
  manager; `ChainOf`, `HoverChainAsync`, `DropChainAsync` and `SetKeyboardTarget` drive hover and drop for drags
  that have no native events, innermost first and with the same "each starts after the previous one's synchronous
  part" ordering native event dispatch has. The managed `ondragover`/`ondrop` handlers now share
  `HoverCoreAsync`/`DropCoreAsync` with it.
- **Keyboard dragging.** Space/Enter on a focused draggable (or its `BdndDragHandle`) picks it up; arrows and
  Home/End move between the targets that can take the item (document order, skipping targets inside the source);
  Space/Enter drops, Escape cancels, and Tab or a pointer press cancels. When a hover reorders the list under the
  drag, the next arrow continues from the item's new place, so a hover-based sortable list moves one place per key.
  New API: `BdndDraggable.AllowKeyboard` (default true), `BdndDraggable.Label`, `BdndDropTarget.Label`,
  `BdndDragInput`, `IBdndMonitor.GetDragInput()` (a default interface member, so other implementations keep
  compiling).
- **Screen readers.** `BdndAnnouncements` (instructions, pickup/over/no-further/drop/cancel announcements, names for
  unlabelled sources and targets), settable through `BdndProvider.Announcements` or
  `BdndDragDropManager.Announcements`. The module keeps one assertive live region and one hidden instructions
  element per distinct text at the end of `body`; sources and handles reference the instructions through
  `aria-describedby` (merged with any the app sets). Draggable hosts get `tabindex="0"` unless given one; handles
  get `role="button"`, `tabindex="0"` and the source's `Label` as `aria-label` unless given their own.
- **Focus.** The focus stays on the dragged item: a `MutationObserver` puts it back when a re-render moves the
  element, and when the item is re-rendered as another draggable (a card dropped in another column) the draggable
  showing the same `Item` takes the focus over (`adoptFocus`), during the drag and for 1.5 s after it.
- **Styling hooks.** `data-bit-bdnd-dragging` on a dragged source's host; `data-bit-bdnd-can-drop`,
  `data-bit-bdnd-over` and `data-bit-bdnd-over-shallow` on drop target hosts.
- **Demo.** New "Accessibility / Keyboard and screen readers" page: a kanban board with labelled columns and cards,
  a WIP-limited column the keyboard skips, move buttons as the non-drag alternative, and columns styled from the
  `data-bit-bdnd-*` attributes only (with a forced-colors rule).
- **Docs.** README: highlights, "When a callback throws", "Keyboard and screen readers", "Styling", monitor table,
  architecture (keyboard flow), troubleshooting rows, component reference.

### Bugs found while implementing

- A key pressed right after a keyboard drop - before .NET's answer reached the module - was taken by the ending
  drag and lost (the E2E suite caught it as an intermittent failure of a "drop, then pick up again with Enter"
  step). Fixed by ending the module's keyboard session at once on a drop or cancel and chaining every .NET call of
  every keyboard drag on one queue, so a new pickup reaches .NET only after the previous drop was handled.
  Regression: `KeyboardTests.Space_picks_a_box_up_the_arrows_move_it_onto_the_bin_and_space_drops_it`.
- A `null` attribute rendered after `@attributes` removes the splatted attribute of the same name (Razor's
  duplicate-attribute rule), which made a handle lose a `role` the app gave it. The keyboard attributes are now
  rendered before the splat. Regression: `BdndKeyboardDragTests.AHandleKeepsTheRoleAndLabelItWasGiven`.
- Keyboard navigation over a hover-reordered list skipped a place on the way back (it continued from the last
  target rather than from where the item had moved). Regression:
  `KeyboardTests.The_arrows_reorder_a_sortable_list_one_place_a_key_and_the_focus_follows_the_card`.

### Validation

| Check | Result |
| --- | --- |
| `dotnet build Bit.Bdnd.slnx` | Succeeded; only the pre-existing CS8602 warning in the MCP tests. |
| Unit tests (all TFMs) | 777 passed (259 x 3; 40 new: `BdndKeyboardDragTests`, `BdndCallbackFailureTests`, `BdndStylingAttributeTests`). |
| MCP tests | 101 passed. |
| E2E (Chromium, WebAssembly) | 58 passed (8 new in `KeyboardTests`, and the page sweep's run of the new demo page). |
| Trimming roots | `BdndTrimmingTests` passes with the new `BdndKeyboardTarget` DTO rooted. |

### Adversarial review (round 1)

An independent review of the round's diff found three high and five medium defects; all were fixed in the round:

| Finding | Severity | Fix | Regression test |
| --- | --- | --- | --- |
| A keyboard session outlived the focus when the focused element was removed and nothing took the focus over (an `ItemFactory` source); its document-level key handler then dropped the item on the next Space typed anywhere. | High | The session cancels on a `focusin` elsewhere, and a key pressed while another element has the focus cancels it and is left to that element. | Reasoned; the Tab test covers the `focusin` path. |
| The focus watch of a drop ended 300 ms after the *previous* call settled, so a slow drop (Server latency, async `OnDrop`) that re-rendered the item lost its focus. | High | The drop is queued before the session ends, so the watch lasts until it has been handled. | `KeyboardTests.Escape_on_a_sortable_list_dropped_outside_reverts_the_order` (focus after the revert). |
| `DispatchExceptionAsync` on a disposed component throws, so an `OnEnd` exception of a source the drop re-rendered away was still lost. | High | Exceptions of disposed components go through the provider (`AddExceptionSink`), then the error output. | `BdndCallbackFailureTests.AnOnEndThatThrowsAfterADropRemovedTheSourceReachesTheRendererThroughTheProvider` |
| An exception escaping a keyboard call (a throwing `CanDropPredicate`) left the drag in progress in .NET while the module had ended it, blocking every later pickup. | Medium | Keyboard calls catch everything, end the drag as a cancel and report the exception; a pickup ends a stale drag. | `AFailingPredicateEndsTheKeyboardDragAndIsReported`, `AKeyboardPickupEndsADragWhoseEndNeverArrived` |
| A native drag could begin over a keyboard drag without its `OnEnd`, and a key still in flight could act on the native drag of the same source. | Medium | `BeginDrag` ends a drag still in progress through its source (`IBdndDragSourceHandler.AbortAsync`); keyboard steps re-check the drag id and input after every await. | `ANativeDragStartingDuringAKeyboardDragEndsItFirst` |
| Arrows landed on the item's own wrapper target, and on hidden or inert targets. | Medium | Targets around the item that hold no other target are skipped, as are hidden and `inert` ones. | `KeyboardTests.A_kanban_card_moves_across_columns...` (own column no longer counted) |
| Every host became keyboard-draggable, including `a`/`button` hosts whose Space and Enter do something. | Medium | Interactive host tags keep their keys (use a handle); the change is documented under "Upgrading". | `AnInteractiveHostKeepsItsOwnKeys`, `AnInteractiveHostIsDraggedByKeyboardFromItsHandle` |
| Focus hand-over matched any draggable whose `Item` was null. | Medium | A null item matches nothing. | Manager logic; covered by `ADraggableShowingAnotherItemDoesNotTakeTheFocus`. |
| One failing link stalled the keyboard queue for good; a handle wrapping a button was a nested button; a native OS drag could overwrite a keyboard drag's hover; changed instructions were not re-rendered. | Low | Queue links never reject; a control inside a handle picks the source up (documented `tabindex="-1" role="presentation"` for the handle); hover reports are ignored during keyboard drags; the instructions id is part of a source's collected state. | `ChangedInstructionsAreWhatTheSourcesAreDescribedBy` |

Kept as is: draggable hosts get no `role` (a `role="button"` on a list item or a card holding buttons would break
its own semantics); their content is read when focused, followed by the instructions. Instruction elements of texts
no longer used stay in the document (one per distinct text, so the number is bounded).

### Validation after the review

| Check | Result |
| --- | --- |
| Unit tests (net10.0) | 267 passed. |
| E2E | 58 passed. |

## Round 2 - touch, pen and pointer drags

### Reassessment and priorities

With the backend-neutral chain in place, the remaining P0 was touch: the HTML5 drag-and-drop API is not raised by
touch on most mobile browsers (and where a platform does raise it - iOS, Windows, some Android builds - only after a
long press that competes with scrolling and the context menu). React DnD solves this with a separate touch backend;
dnd-kit with pointer and touch sensors (activation delay plus tolerance, `touch-action`, an overlay). Bdnd gets a
pointer backend in the module, driving the same manager.

### What changed

- **`BdndPointerDrag`** (`Touch` default, `All`, `None`) on `BdndDraggable.PointerDrag`, with `TouchDelay` (250 ms),
  `DragThreshold` (5 px) and `AutoScroll` (true). `BdndDragInput.Pointer`.
- **Activation.** A touch on a source must rest `TouchDelay` ms (moving more than the threshold before then gives the
  press up, so swipes scroll); a handle, a pen on a handle and the mouse start after `DragThreshold` px. Presses on
  form fields and inside `data-bit-bdnd-no-drag` regions never start one (and a native drag never starts from a
  no-drag region either). During a touch press the source selects no text, shows no callout, opens no context menu
  and is not natively draggable (no platform long-press drag); handles carry `touch-action: none`.
- **The drag.** Pointer capture on the source; a copy of the source (`data-bit-bdnd-preview`, inert, no ids, field
  values copied, compensated for a transformed containing block, moved to the body under a scaled one) follows the
  pointer with a transform; `elementFromPoint` hit testing on every frame and every 100 ms while the pointer rests;
  hover changes and coalesced `OnHover` reports (`HandlePointerHover`) go through the drag queue shared with keyboard
  drags; auto-scroll of the innermost scroll container (or the page) the pointer is within 48 px of an edge of.
- **The end.** Release drops: `HandlePointerDrop` is asked of the targets under the pointer innermost first until
  the provider that owns the drag answers, and that provider runs its chain; then `HandleDragEnd`. Escape,
  `pointercancel`, window blur and a hidden page cancel; a release outside the window is noticed on the next move.
  The click the release raises is swallowed.
- **Manager.** `BeginDrag` ends a drag still in progress through its source (`IBdndDragSourceHandler.AbortAsync`), so
  every `OnBegin` gets its `OnEnd` whatever input started the next drag.
- **Demo.** "Customize / Touch and pointer drags": `PointerDrag="All"` boxes with an info button in a no-drag
  region, a styled preview, and a scrolling tray of slots.
- **Docs.** README "Touch and pointer drags", requirements, hosting (Hybrid), architecture, troubleshooting,
  reference, "Upgrading: touch drags by default".

### Bugs found while implementing

- `FlushBeginAsync` let an exception of `OnBegin` escape synchronously from the call, before any guard saw a task -
  on the path where the end publishes a begin not yet published, that skipped `EndDrag` and left the drag stuck.
  It now returns a faulted task. Regression:
  `BdndCallbackFailureTests.AnOnBeginThatThrowsWhenTheEndPublishesTheDragStillEndsIt`.
- The MCP oracle test caught the README's component reference missing the new parameters (and a default written
  as `Touch` instead of `BdndPointerDrag.Touch`).

### Validation

| Check | Result |
| --- | --- |
| Unit tests (all TFMs) | 858 passed (286 x 3; 19 new in `BdndPointerDragTests`, 1 in `BdndCallbackFailureTests`). |
| MCP tests | 101 passed. |
| E2E | 66 passed (7 new in `PointerTests`: touch long-press drag and swipe through the DevTools protocol, mouse pointer drag with preview and no native drag, Escape, no-drag region, auto-scroll, click). |

### Adversarial review (round 2)

| Finding | Severity | Fix | Regression test |
| --- | --- | --- | --- |
| On iOS the `touchmove` listener added at `pointerdown` comes too late: WebKit fixes at `touchstart` whether moves are cancelable, so the drag scrolled the page and was cancelled. | High (device) | A non-passive `touchmove` listener on every source from registration, cancelling only its own drag's moves. | Needs an iOS device; the DevTools-protocol touch tests cover Chromium. |
| A re-render removing the pressed element: its touch (and on some engines pointer) events stop reaching the document - Chrome pans and cancels, WebKit may never report the release, leaving the session (and every later drag) stuck. | High | The pressed element listens too (`pointer*`, `touchmove/end/cancel`), events are handled once; another press ends a stuck session. | Reasoned; to verify on devices. |
| Native long-press drags of images, links and draggable ancestors could still lift; restoring `draggable="true"` could undo a re-render. | Medium | `-webkit-user-drag: none` on images, links and draggables during a touch press; `draggable` put back from the current options; every `dragstart` refused during a pointer drag and 600 ms after a touch press. | Reasoned. |
| `OnHover` reports could run against the hover state of other targets, and the last position was dropped at release. | Medium | One report slot per set of hovered targets, queued between the hover changes it belongs to; a final hover update at the release point before the drop. | Reasoned; the tray drop test drops at the release point. |
| Auto-scroll scrolled a page whose root or body hides overflow, and never re-measured containers. | Medium | Page axes from the root's and body's overflow; container axes re-measured every 500 ms. | Reasoned. |
| The preview was clipped by containing-block ancestors and stacking contexts, kept Blazor's `_bl_` ElementReference attributes, broke on file inputs, and replayed media. | Medium | Top layer (`popover="manual"`, user-agent popover styles overridden from the source's computed style); `_bl_*`, `autoplay` and iframe `src` removed; file inputs skipped; preview failures do not stop the drag. | `PointerTests.The_mouse_drags_a_pointer_source...` asserts `:popover-open`. |
| An async `OnEnd` held up the next drag's start in the shared queue. | Medium | The end returns once the drag state is cleared; the rest of `OnEnd` runs on, guarded. | `BdndDragLifecycleTests.TheDragStateIsClearedRightAfterTheSynchronousPartOfOnEnd` (updated). |
| A mouse release outside the window counted as a drop; the click of a release after Escape got through; `DragThreshold=0` made touch drags impossible; an `ItemFactory` exception on a pointer or keyboard start was lost. | Low | Cancel instead; clicks are blocked until that pointer's release; a 4 px touch tolerance during the delay; guarded item production. | `The_release_of_a_drag_cancelled_with_Escape_clicks_nothing`, `AnItemFactoryThatThrowsRefusesThePointerDragAndIsReported` |

Not changed: a drag that begins while another is in progress ends the earlier one synchronously, which on
WebAssembly re-renders during a native `dragstart` (the native path otherwise avoids that). It only happens when the
end of the earlier drag never arrived, and a pointer press cancels a keyboard drag before a native drag can begin.

### Validation after the review

| Check | Result |
| --- | --- |
| Unit tests (all TFMs) | 861 passed. |
| E2E | 67 passed. |

## Round 3 - insertion positions and collections

### Reassessment and priorities

The sortable recipe inherited from React DnD measures the hovered element over interop on every hover and reorders
live, which costs a round trip per hover on Blazor Server and gives the keyboard (round 1) an awkward model. The
pattern Pragmatic drag and drop popularized - each item's target reports the edge the pointer is closest to, an
indicator shows the insertion point, the list changes on drop - needs no measuring in .NET, works the same for every
input, and gives the keyboard real places to visit.

### What changed

- **`BdndEdges`/`BdndEdge`**, `BdndDropTarget.Edges`, `BdndDropState.ClosestEdge`, `BdndDropEventArgs.ClosestEdge`,
  `BdndNativeDropEventArgs.ClosestEdge`, `data-bit-bdnd-edge` on the host. The module computes the closest allowed
  edge from the target's box (display:contents aware) on every `dragover` and pointer frame and reports it only when
  it changes (`HandleEdgeChanged`); the manager keeps it per target, shows it while the target is hovered, and forgets
  it when the hover ends. An edge reported before the hover's own report (an async `OnBegin` can delay that on
  Blazor Server) is kept and shown once the hover arrives.
- **`BdndList`**: `InsertionIndex`, `ReorderIndex`, `Reorder`, `Move` (within a list or across lists; no-op moves
  answer false).
- **Keyboard places.** Candidates go to the module with their parent target and edges, and the source with the
  targets around it. A target with edges is two places (before, after); the gap between neighbouring items of one
  list is one place; the gap the item is in is skipped; a container's end place sorts after everything it holds.
  The keyboard "pointer" sits in the half of the target next to the edge, and announcements say "before"/"after".
- **Demo.** "Sortable / Drop indicators": two lists, indicator line from `data-bit-bdnd-edge`, `BdndList.Move` on
  drop, list targets taking drops below the cards.
- **Docs.** README: highlights, "Drop indicators and insertion positions", state table, styling table, keyboard
  section, reference.

### Validation

| Check | Result |
| --- | --- |
| Unit tests (all TFMs) | 972 passed (324 x 3; 37 new: `BdndListTests`, `BdndEdgeTests`). |
| MCP tests | 101 passed. |
| E2E | 5 new in `IndicatorTests` (mouse within and across lists, the indicator following the pointer, keyboard places, drop on the list end) pass; full run below. |

### Adversarial review (round 3)

`BdndList` was found free of off-by-one errors; the keyboard places and the edge bookkeeping were not.

| Finding | Severity | Fix | Regression test |
| --- | --- | --- | --- |
| Gap merging compared only parent ids: two root-level lists merged their boundary places, and a refusing (or hidden) target between two items did not keep their places apart. | High | Items are neighbours only with no registered target between them and, without a list target, the same parent element. | `IndicatorTests.The_keyboard_never_offers_the_place...` (root-level horizontal list) |
| The "own slot" filter missed root-level items and picked document-order neighbours instead of siblings in nested lists. | Medium | The item's list is the first target around it that is not its own; its neighbours are that list's nearest items. | Same |
| With an async `OnBegin`, a later hover report could overtake the one waiting for it and leave a stale hover and edge. | Medium | Every caller of `FlushBeginAsync` waits for a running begin (only the first gets its exception); a leave always forgets the edge. | `BdndEdgeTests.HoverReportsWaitForAnAsyncOnBeginInTheirOrder`, `ALeaveForgetsTheEdgeEvenWhenItOvertookTheHover` |
| Right-to-left and reversed layouts inverted placement. | Medium | `InsertionIndex`/`ReorderIndex(..., reversed)`; keyboard: arrows swap and horizontal edges are ordered by `direction`; announcements use the list-order `Before`. | `BdndListTests.AReversedLayoutSwapsBeforeAndAfter` |
| Nested targets with edges drew two indicators and gave the outer `OnDrop` an edge. | Medium | The edge shows, and drops carry it, only on the shallow hovered target. | `OnlyTheInnermostHoveredTargetShowsAndDropsWithItsEdge` |
| A list target's place came before its cards; the arrows wrapped around at pickup. | Medium | A target holding other candidates sorts after them; no wrap-around (the drag stays and says so). | `IndicatorTests` (keyboard), `KeyboardTests` (dustbin above the boxes: ArrowUp) |
| The only item of a list was offered its list's end; every edge change ran every component's collect; the indicator showed over refusing targets; edges were stored with no drag; toggling `Edges` mid-hover went stale; "no further" ignored the edge; a weak test assertion. | Low | All fixed: per-target change notification (`BdndDropTargetEntry.Changed`), edge gated on `CanDrop`, edges only for this manager's drags or native hovers, the module resets its edge when the target's edges change, edge-aware wording, the assertion checks the attribute is gone. | `AnEdgeChangeReRendersOnlyItsTarget`, `ATargetThatRefusesTheItemShowsNoEdge`, `AnEdgeReportedWithNoDragOfThisProviderIsIgnored` |

### Validation after the review

| Check | Result |
| --- | --- |
| Unit tests (all TFMs) | 1002 passed. |
| MCP tests | 101 passed. |
| E2E | 75 passed. |

## Round 4 - native data

### Reassessment and priorities

Native drops exposed file metadata only (the README sent users to `InputFile` or their own interop for contents),
offered no type filter while hovering, no folder support and no limit on how many files a drop hands over, and
in-app drags could carry nothing out of the page.

### What changed

- **File contents.** The module keeps the `File` objects of a target's last drop (released on its next drop and when
  it is disposed); `BdndNativeFile.OpenReadStreamAsync(maxAllowedSize, cancellationToken)` streams one through an
  `IJSStreamReference` (a `File` is a `Blob`), refusing files above the limit (512,000 bytes by default, as
  `IBrowserFile`) before reading; the stream owns and releases its reference. `CanRead`, `LastModified`,
  `RelativePath`, `IsDirectory`.
- **Filters and limits.** `BdndDropTarget.AcceptFileTypes` (MIME types, groups, extensions): while hovering, a drag
  whose reported MIME types match none of the listed MIME types is refused - also in the hover state .NET sees - and
  on drop files of other types are left out. `MaxFiles`; `BdndNativeDropEventArgs.TotalFileCount`.
- **Folders.** `ExpandDirectories` walks dropped folders (entries taken during the drop, walked after, capped at
  10,000 files) into files with their relative paths; without it a folder arrives as one `IsDirectory` entry.
- **Out of the page.** `BdndDraggable.NativeData` puts strings on a native drag's `DataTransfer` at `dragstart`; the
  drag stays an in-app drag for the page's own targets.
- **Demos.** Native Files: folder expansion, a contents preview of small text files read as streams, an images-only
  zone. Native HTML: a box dragged out of the page, and the note that dropped HTML is untrusted.
- **Docs.** README: files (contents, metadata, filters, folders, lifetime), "Dragging data out of the page",
  "Dropped content is untrusted" (HTML sanitizing, URL schemes, file names and types), troubleshooting, reference.

### Bugs found while implementing

- The first hover filter gave up whenever an extension was listed, so an images-only target taking `image/*` and
  `.png` lit up for a text file. A file whose MIME type is known and matches no listed MIME type is now refused (the
  browser types files from their extensions); with no MIME type listed nothing is decided before the drop.
  Regression: `NativeDragTests.A_target_taking_images_only_refuses_other_files...`.

### Validation

| Check | Result |
| --- | --- |
| Unit tests (all TFMs) | 1035 passed (345 x 3; 11 new in `BdndNativeDataTests`). |
| MCP tests | 101 passed. |
| E2E | Native suite 6 passed (contents read through the stream, the images-only filter on hover and drop, native data carried out by an in-app drag); full run below. |

## Performance measurements (after round 4)

`PerfTests` (`--filter "TestCategory=Perf"`) runs a scripted mouse drag - 120 moves of a few pixels - across three
stress pages and records, in the page, the animation frames (how many, and the longest gap between two), long tasks
and DOM mutation records while it runs. It was run against two Release builds of the demo served side by side:
the baseline (`develop` before this work, built from a sparse worktree of the same commit) and the current working
tree, three runs each, alternating.

Conditions: Windows 11 desktop, Chromium 1248 (Playwright, new headless), viewport 1280 x 900, WebAssembly demo,
Release builds, Development environment (static web assets from the build output).

| Scenario | Baseline (3 runs) | Current (3 runs) |
| --- | --- | --- |
| Sortable stress: 100 cards that are each a draggable and a target, reordering on hover | 54-56 fps; longest frame gap 19-36 ms; 0 long tasks; 130 DOM mutation records | 55-57 fps; longest frame gap 35-41 ms; 0 long tasks; 422 DOM mutation records |
| Dustbin stress: 30 sources, 6 targets of random types | 55-60 fps; longest gap 18-19 ms; 0 long tasks; 14 mutation records | 57-58 fps; longest gap 18-19 ms; 0 long tasks; 20 mutation records |
| Custom drag layer: a preview rendered by .NET on every pointer move | 58 fps; longest gap 18-19 ms; 0 long tasks; 9-10 mutation records | 59 fps; longest gap 18-20 ms; 0 long tasks; 18 mutation records |

Reading: frame rates are unchanged and no long task appeared in either build. The extra DOM mutation records are the
`data-bit-bdnd-*` styling attributes: when a drag begins and ends every target that can take the item flips
`data-bit-bdnd-can-drop` (100 targets x 2 on the sortable page). Those targets re-rendered in the baseline too - their
`CanDrop` changes - so the cost is the attribute writes themselves, not extra renders. The longest frame gap on the
sortable page is noisy in both builds (19-36 ms baseline) and stays within two frames. The edge tracking and the
per-target edge notification of round 3 do not show here because these pages have no `Edges`.

Limits of the method: Playwright dispatches the moves faster than a hand, so a run lasts about a second; the numbers
compare builds on one machine, they are not absolute budgets. Interop message counts (relevant on Blazor Server) are
not measured: the module's .NET calls cannot be observed from the page without patching Blazor's script.

### Adversarial review (round 4)

| Finding | Severity | Fix | Regression test |
| --- | --- | --- | --- |
| The hover filter refused files of a listed extension when MIME types and extensions were mixed (the README's own `image/*`, `.pdf` example refused PDFs); `*`/`*/*` refused everything. | High | Only a target listing MIME types alone refuses on hover; with an extension the drop decides; `*` and `*/*` match anything. The README example and the demo now say which is which. | `NativeDragTests.A_target_listing_an_extension_takes_any_file_drag...` |
| All file descriptions went to .NET in one message: a few hundred files exceed Blazor Server's 32 KB message limit and drop the circuit. | High | The first 50 descriptions come with the drop, the rest are read page by page (`nativeDropFiles`). | `NativeDragTests.A_drop_of_many_files_reaches_dotnet_in_full` (120 files), `BdndNativeDataTests.TheRestOfAManyFileDropIsReadFromTheModulePageByPage` |
| A folder walk raced with later drops (an older drop delivered last, releasing a newer drop's files) and with disposal (files pinned for the page's life). | High | A per-target drop serial; a walk finishing after a newer drop or after disposal is discarded. | Reasoned (folder entries cannot be produced by a synthetic DataTransfer). |
| `CanRead` never turned false. | Medium | A .NET-side holder per drop, released on the target's next drop and on disposal; reading a released file throws without asking the module. | `TheFilesOfTheLastDrop...NextDropArrives`, `...TargetIsDisposed` |
| One unreadable entry lost the whole walk; empty folders were unbounded; `MaxFiles` did not stop the walk; truncation was invisible. | Medium | Per-entry error handling, folders count toward the 10,000-entry cap, the walk stops at `MaxFiles` accepted files, `FileListIncomplete` reports it. | Reasoned. |
| The hover stayed on during a long walk. | Medium | The leave is reported when the drop goes asynchronous. | Reasoned. |
| Folders vanished with `AcceptFileTypes`; folders misreported where the browser cannot tell; a cancelled read could pin a file; `NativeData` caveats and file-name handling undocumented. | Low | Documented (folders are of no type; browsers that cannot tell folders); reads take no token into the interop call and release a reference handed out after a cancellation; README covers fields outside targets, other Bdnd windows, and `Path.GetFileName`. | `ACancelledReadAsksTheModuleForNothing`, `DisposingTheStreamReleasesTheModulesReference` |

### Validation after the review

| Check | Result |
| --- | --- |
| Unit tests (all TFMs) | 1050 passed. |
| MCP tests | 101 passed. |
| E2E | 83 passed (includes 3 `PerfTests` measurements). |

## Round 5 - observers, disabled targets and Blazor Server

### Reassessment and priorities

The feature matrix still listed enter/leave notifications as state-only, disabled targets as predicate-only, no
context-wide drag observer, and Blazor Server and Hybrid as untested: every E2E run used the WebAssembly demo.
Programmatic drops were weighed and left out (see below).

### What changed

- **`OnEnter`/`OnLeave`** on drop targets, for in-app drags of every input: raised when the target's `IsOver` flips
  while a drag goes on; a drag ending over the target is no leave.
- **`Disabled`** on drop targets: no accepted types, so no hover, `CanDrop`, drop or native drop, and no keyboard stop.
- **Watching every drag.** `BdndDragDropManager.DragStarted`/`DragEnded` and `BdndProvider.OnDragStart`/`OnDragEnd`
  with `BdndDragEventArgs` (item, type, input, `DidDrop`, drop result): for undo histories, analytics, page-wide drag
  state. A drag that ends before its begin was published reports neither.
- **Blazor Server, verified.** The demo now also registers `InteractiveServer`, and `App.razor` serves
  `/hosting/server-check` with it (every other page stays on WebAssembly). The page drags every way the library
  drags; `ServerHostingTests` drives it: native mouse, keyboard, pointer, drag layer, insertion edges, and file
  contents streamed over the circuit.
- **Docs.** README: "Entering and leaving", "Disabling a target", "Watching every drag", why there is no programmatic
  drop API, the Blazor Server section, reference rows.

### Decided against

- **Programmatic drops** (a "drop this item on that target" API for non-drag alternatives). The operation a drop
  performs is the application's own method; a button calling it directly is simpler and clearer than a fake drag
  through the library, and the keyboard demo shows the pattern. Documented under "A non-drag alternative".

### Validation

| Check | Result |
| --- | --- |
| Unit tests (all TFMs) | 1074 passed (358 x 3; 8 new in `BdndObserverTests`). |
| MCP tests | 101 passed. |
| E2E | 90 passed (6 new in `ServerHostingTests`; the page sweep covers the server-rendered page). |

### Adversarial review (round 5)

| Finding | Severity | Fix | Regression test |
| --- | --- | --- | --- |
| A `DragStarted`/`DragEnded` handler that threw left the drag half begun (no `OnBegin`) or half ended, and stopped the other handlers. | High | The manager raises each handler on its own; a failure goes to the renderer through the provider's exception sink. | `AThrowingDragStartedHandlerDoesNotKeepOnBeginFromRunning`, `AThrowingDragEndedHandlerDoesNotStopTheDragFromEnding` |
| A drag ended because another began reported `DidDrop` false to `OnEnd` while `OnDragEnd` saw true. | Medium | The aborted drag's `OnEnd` gets what the manager has so far: `DidDrop()` and the published result. | `ADragEndedBecauseAnotherBeganReportsTheSameOutcomeToOnEndAndOnDragEnd` |
| Disabling (or narrowing `Accept` of) a hovered target kept it over, with its edge, until the pointer moved; its `ondragover` stayed wired. | Medium | Re-registering a target that no longer accepts the hovering type clears `IsOver`, the edge and the native hover at once; a disabled target wires no `ondragover`. | `DisablingAHoveredTargetTakesItsHoverAwayAtOnce`; the disabled test now also tries a native hover and drop |
| The provider's `OnDragStart` ran inside the manager's update, before the source's `OnBegin`, and a target removed by a callback could re-render after disposal. | Medium | Provider callbacks run a yield after the update; `OnStateChanged` returns once the target is disposed. | `ADragRunsStartEnterLeaveEnterDropAndEndInOrder` (new order), `MovingBetweenNestedTargetsEntersAndLeavesEachOnce` |
| The server check page read whole files of any type, any number, over the circuit; an unreadable file threw. | Medium | `AcceptFileTypes="text/plain"`, `MaxFiles="5"`, the first 200 characters, read errors shown per file. | `Dropped_file_contents_stream_to_the_server` |
| Navigating in the app between the server page and the others rendered the next page in the current render mode (the server page on WebAssembly, or the rest over the circuit). | Medium | The demo's layout loads the page afresh when the target needs the other render mode. The page is now in the docs navigation. | `Moving_between_the_server_page_and_the_others_switches_the_render_mode` |
| The README claimed touch drags over the circuit without a test. | Low | Touch test added. | `A_touch_drag_drops_over_the_circuit` |
| Enter/leave order between neighbouring native targets, the provider callbacks' timing and what an aborted drag reports were undocumented. | Low | README: "Entering and leaving", "Watching every drag", "Drop results", the upgrade note. | Docs |

Deployment note: the server check page needs WebSockets (or SignalR's fallbacks) on the host; on IIS the WebSocket
Protocol feature must be enabled, or the circuit falls back to long polling and drags lag noticeably.

### Validation after the review

| Check | Result |
| --- | --- |
| Unit tests (all TFMs) | 1089 passed (363 x 3; the observer tests follow the new callback order, five new). |
| MCP tests | 101 passed (guide-heading suggestions now rank top-level sections first: new README sub-sections had pushed "Drag handles" out of the ten suggestions). |
| E2E (Chromium) | Covered by the round 6 run below. |

## Round 6 - grids, virtualized lists and the remaining inputs

### Reassessment and priorities

The matrix still showed wrapping grids and virtualized lists as untested, pen input and window blur/hidden-page
cancellation as unautomated, and the Blazor Server page without a touch test. A keyboard user moving a tile down one
row of a four-column grid needed four presses. Those gaps ranked above the planned performance round, which moved to
round 7.

### What changed

- **Keyboard Up/Down by row.** In a layout that wraps, Down and Up go to the nearest row below or above, to the place
  of the same list closest across (`rowPlace` in the module), so a tile moved down stays in its column. Vertical
  lists, kanban columns, trees and single rows keep the list order: the row rule applies only when the reference
  place has places of its own list beside it, and falls back to the list order where no row lies that way.
- **Grid demo** (`/sortable/grid`): twelve tiles, four a row, `Edges="BdndEdges.Horizontal"`.
- **Virtualized demo** (`/sortable/virtualized`): a thousand rows in `Virtualize`, pointer and keyboard drags.
- **Pointer copy outlives its source's container.** Found by the virtualized test: the copy that follows the pointer
  is placed next to the source, so when a re-render removed the source's container (a virtualized row scrolled out,
  a list re-rendered under the drag) the copy vanished while the drag went on. The module now moves it to the body
  and shows it again; the text look it inherited is set on it when it is made.
- **Blazor Server page:** reads at most five `text/plain` files and their first 200 characters; moving between it and
  the WebAssembly pages loads the page afresh in the other render mode (the demo's layout); it is in the docs
  navigation. Touch drags over the circuit are tested.
- **Docs:** README "Grids and wrapping lists" and "Virtualized lists", the keyboard table, the enter/leave order of
  neighbouring native targets, the provider callbacks' timing, what an aborted drag reports.

### Validation

| Check | Result |
| --- | --- |
| Unit tests (all TFMs) | 1089 passed. |
| MCP tests | 101 passed. |
| E2E (Chromium) | 108 run: 107 passed; the sidebar sweep failed on the server page's forced reload (a fresh document on a page with no draggables never loads the module). Fixed by leaving that link to `ServerHostingTests`; rerun passed. New: `GridTests` (4), `VirtualizedTests` (2), `PointerTests` pen and blur/hidden (3), `ServerHostingTests` touch and render-mode switching (2). |

## Round 7 - performance over the wire

### Reassessment and priorities

The frame and mutation measurements of round 4 cover WebAssembly, where an interop call costs little. On Blazor
Server every call is a message over the circuit, and nothing measured how many a drag sends. That was the gap.

### What was measured

`ServerHostingTests.A_drag_sends_a_few_messages_over_the_circuit_however_long_it_moves` counts the frames the browser
sends on the circuit's WebSocket during one drag of 120 moves on the Blazor Server page.

| Drag | Before | After |
| --- | --- | --- |
| Native mouse drag (Glass onto the bin) | 23 messages | 16 messages |
| Pointer drag (`PointerDrag="All"`) | 250 messages: 59 `HandleDragMove` calls and the renders they caused | 16 messages |

The cause: the page has a `BdndDragLayer` (for one box that draws its own preview), and any drag layer made every
source report every pointer move - so a pointer drag of any box sent one message per move, and re-rendered the layer
for each, though the layer drew nothing for it.

### What changed

- **`BdndDragLayer.Accept` / `AcceptTypes`.** A layer can name the item types it draws. A drag of another type is no
  drag to it (`IsDragging` false, no re-render per move), and a source sends its moves only when a layer draws its
  type (or a layer, or an `OffsetChanged` handler, follows every drag). Without `Accept` a layer follows every drag,
  as before: no breaking change. The manager keeps the followers per type (`FollowsPointer`, `FollowOffsets`).
- The Blazor Server page gives its layered box its own type and its layer `Accept`; the test asserts fewer than 30
  messages per drag, for native and pointer drags.
- README: the custom drag layers section, the Blazor Server section and the reference table.

### Frame rates, baseline against current

`PerfTests`, two runs of each, alternating, both demos as Release builds served side by side (the baseline from the
sparse worktree of `develop`):

| Scenario | Baseline | Current |
| --- | --- | --- |
| Sortable stress | 32 fps; longest gap 31.8-31.9 ms; 0 long tasks; 130 mutation records | 32-33 fps; longest gap 31.7-31.9 ms; 0 long tasks; 422 mutation records |
| Dustbin stress | 32-34 fps; longest gap 31.4-31.5 ms; 0 long tasks; 14 records | 32-33 fps; longest gap 31.5-31.7 ms; 0 long tasks; 20 records |
| Custom drag layer | 32-35 fps; longest gap 31.6-31.8 ms; 0 long tasks; 10 records | 33 fps; longest gap 31.5-31.6 ms; 0 long tasks; 17 records |

This session's display ran at 30 Hz (a remote desktop session): both builds hit the same cap, every frame gap is two
60 Hz frames, and no long task appeared. Against each other the builds are level, as in round 4; the mutation counts
are the same as then (the styling attributes).

### Validation

| Check | Result |
| --- | --- |
| Unit tests | 365 passed on net10.0 (two new: a layer of other types neither sees the drag nor has its moves sent; a layer of the dragged type follows it). |
| MCP tests | 101 passed (the reference table lists the new parameters). |
| E2E | `ServerHostingTests`, `DragAroundTests`, `CustomizeTests`: 15 passed. |

### Adversarial review (rounds 6 and 7)

| Finding | Severity | Fix | Regression test |
| --- | --- | --- | --- |
| The pointer copy stayed hidden when a keyed re-render moved its container: moving an element closes the popovers in it, and the copy was still connected. | Medium-High | The copy is shown again whenever it is out of the document or no longer `:popover-open`. | `VirtualizedTests` (the copy must be visible, not only present) |
| The popover reset copied the source's border, padding and colours with `!important`, so the documented `[data-bit-bdnd-preview]` rules lost (the demo's own pointer-box rule never applied); the new inherited text look did the same to font rules. | Medium | The copy is inserted in place first and its own computed style - source rules and preview rules together - is what the reset keeps; the text look is only applied when the copy has to go on from the body. | Reasoned (computed-style ordering); `PointerTests` pass. |
| A gap at a row break was offered as "after the last tile of the row above", so Up from the first column landed one column off. | Medium | Where a row breaks between two neighbours, the gap is the place before the second, at the start of its row (where its line shows). | `Up_from_the_first_column_lands_at_the_start_of_the_row_above`, `Down_from_the_start_of_a_row_keeps_to_the_first_column` |
| Row grouping assumed equal heights; any 1px vertical overlap made a list "wrap". | Medium | Beside = no horizontal overlap and a vertical overlap of half the shorter box; the nearest row is the places overlapping the nearest one. | Grid suite; kanban test (a single row of columns does not wrap) |
| The item's own wrapper was found among candidates only: a tree row refusing its own node made the row the "list", so the place the node is in was offered. | Medium | Targets around the item are its own by where they are, whether or not they take it. | `TreeTests`, `IndicatorTests` |
| Up and Down in the first or last row moved sideways along it. | Low | With no row that way they go on past the row in the list's order - to another list, or nowhere. A single row of targets (kanban columns) keeps next/previous: found when the kanban test failed on the first version of the fix. | `Down_on_the_last_row_goes_nowhere`, kanban keyboard test |
| Without popovers, a re-attached copy kept a correction made for a transformed ancestor. | Low | The corner it started at is restored when it goes on from the body. | Reasoned (every engine tested has popovers). |
| `MainLayout`: a forced load of the same address with a fragment only scrolls; an external target would throw in the handler. | Low | `location.reload()` for the same address; addresses of other sites are let through. | `Moving_between_the_server_page_and_the_others_switches_the_render_mode` |
| Server page: read failures over the circuit other than I/O escaped; two quick drops could finish out of order. | Low | Timeouts, `JSException` and cancellations are reported per file; only the latest drop writes the contents. | `Dropped_file_contents_stream_to_the_server` |
| Fixed 400 ms waits against the 250 ms touch delay; an assertion that a re-attached copy merely exists. | Low | The touch and pen tests wait for the drag to begin (the copy appears); the copy must be visible. | Same tests |

Firefox showed two more: Blazor's own `WebSocket is not in the OPEN state` as a Server page unloads (now ignored by the
problem tracker, by that message only), and the virtualized keyboard walk outrunning `Virtualize` (a key pressed
before the rows ahead render finds nothing further; the test now presses at a person's pace and the README says so).

## Round 8 - interactive content, previews and selections

### Reassessment and priorities

Four rows of the matrix were still partial: interactive descendants, axis lock and snapping, multi-item drags, and
(from the review) the copy that follows the pointer. A browser check found the first a real bug: a press in a text
field inside a draggable started a native drag in Chromium, and in Firefox selected no text.

### What changed

- **Fields and no-drag regions inside a draggable.** A press there holds the source (or its handle) non-draggable
  until it ends, so the field selects its text and no native drag starts. The test was run with the fix disabled:
  Chromium recorded a `dragstart` and Firefox selected nothing; with it, both pass.
- **`PreviewAxis`** on `BdndDraggable` keeps the pointer copy on one axis; the virtualized list uses `Vertical`.
- **`BdndXYCoord.KeepTo` / `SnapTo` / `Clamp`** and **`BdndDragLayerState.SourceClientOffsetOn`** for drag layers;
  **`BdndAxis`**.
- **Dragging several items** demo (`/sortable/multi-select`): checkboxes make the selection (from the keyboard too),
  a drag of a ticked card carries all of them.
- README: the new parameters, the copy and its axis, fields inside sources, the multi-select page.

### Validation

| Check | Result |
| --- | --- |
| Unit tests (all TFMs) | 1140 passed (380 x 3). |
| MCP tests | 101 passed. |
| E2E (Chromium) | Affected suites 46 + 18 passed; full runs below. |
| E2E (Firefox) | Field and multi-select tests passed; full run below. |

## Round 9 - every engine, the published build, release readiness

### What the full runs found

- **WebKit crashed mid-run** (on Windows, after about half the suite): every later test failed in milliseconds on a
  closed browser. The same suites pass on their own. The E2E session now launches the browser again when it went
  away, so a crash fails one test, not the rest of the run.
- **WebKit-only test assumptions**, not library faults: WebKit reads `dropEffect` as "none" while a dragover runs
  (the module leaves the effect to the browser on purpose, so modifier keys still switch copy and move; the drop
  then uses the declared effect, which the test still checks); a dragged link lists `text/html` with no data there
  (the URL and the text arrive); its driver reports no navigation for a navigation inside the page, so the sidebar
  test polls the address and waits for the page's link to be marked.
- **A race in the virtualized test** (a drop read one row before the list settled): the hovered row is now read
  twice, a quiet moment apart.

### Validation (final)

| Check | Command | Result |
| --- | --- | --- |
| Build | `dotnet build Bit.Bdnd.slnx` | Succeeded; the only warning is in `McpSurfaceTests.cs` and predates this work. |
| Unit | `dotnet test --project Tests/Bit.Bdnd.Tests/Bit.Bdnd.Tests.csproj -f net8.0` (and net9.0, net10.0) | 380 passed on each (1140). |
| MCP | `dotnet test --project Tests/Bit.Bdnd.Tests.Mcp/Bit.Bdnd.Tests.Mcp.csproj` | 101 passed. |
| E2E Chromium | `dotnet test` in `Tests/Bit.Bdnd.Tests.E2E` | 117 passed. |
| E2E Firefox | `BDND_E2E_BROWSER=firefox dotnet test` | 113 passed, 4 skipped (touch and pen go through Chromium's DevTools protocol). |
| E2E WebKit | `BDND_E2E_BROWSER=webkit dotnet test` | 112 passed, 4 skipped, 1 failed - the virtualized race, fixed afterwards and rerun: `VirtualizedTests` 2 passed twice on WebKit and once each on Chromium and Firefox. The sidebar and link tests were fixed before this run and passed in it. |
| Trim gate | `bash Tests/Bit.Bdnd.Tests.E2E/publish-gate.sh <out>` | No trim/AOT analysis warnings originate in Bit.Bdnd. |
| E2E on the trimmed publish | `BDND_E2E_PUBLISHED_HOST=<out> dotnet test` | 117 passed (Chromium). |

The baseline worktree used for the performance comparison was removed.

## Round 10 - what remained

### Reassessment and priorities

Round 9 left the matrix with Blazor Hybrid, folder drops, a release outside the window, real touch input and screen
readers untested, three rows partial (native edge scrolling, forced colors, dashboards and palettes) and a known
limit (a fast keyboard walk through a virtualized list). Each was taken in turn, as far as an automated suite reaches.

### What changed

- **Blazor Hybrid under E2E.** `Tests/Bit.Bdnd.Tests.Harness.Hybrid`, a WinForms `BlazorWebView` app (the pattern of
  the Brouter and Butil harnesses), opens WebView2's debugging port through its API; `HybridHostingTests` attaches
  with Playwright over CDP and drags in the WebView: native mouse, keyboard, pointer, touch, a drag layer, edges and
  file contents read into .NET. An `e2e-hybrid` job on windows-2025 runs it in CI. The base test class gained an
  opt-out (`UsesDemo`) so these tests need neither the demo nor a downloaded browser.
- **A native drag released outside the window** ends as a cancel: where the browser reports no end, the first
  button-less mouse move after it (no mouse events reach a page during a native drag) ends the session.
- **The keyboard in a virtualized list**: a move that finds nothing further among the rendered rows scrolls the list's
  container on by about a row; .NET asks again a little later, up to ten times, as the rows it brought in register.
- **Folder drops** tested: the page's `webkitGetAsEntry` answers with a tree of entries, which the real module walks
  (paths, contents, batches, an empty folder, an unreadable entry, the file limit). The demo now says when the list is
  incomplete.
- **Touch on every engine**: pointer events of type touch drive the pointer path in Firefox and WebKit too.
- **Screen readers**: the computed description and roles on every engine, and Chromium's own accessibility tree over
  CDP; the pointer copy adds nothing to the accessibility tree.
- **Forced colors** (emulated, every engine) and **native edge scrolling** (Chromium's drag controller) tested.
- **A page builder demo**: palette copies at insertion edges, moves, a bin that refuses palette blocks, the keyboard.
- **A checklist for people**: screen readers (NVDA, JAWS, VoiceOver, TalkBack) and devices (iPhone, iPad, Android,
  Windows touch, pens), with expected results, in the E2E README.
- The MCP suite's last compiler warning (a nullable dereference) is gone.

### Engine notes found on the way

- Playwright's Firefox driver keeps a drag session of its own after a release outside the window and takes the input
  meanwhile: no key or mouse event reaches the page. The outside-release test checks the cancel there and the next
  drag on the other engines.
- WebKit on Windows crashed again once mid-suite; the relaunch kept it to one test, and the suite passed three times
  in a row on its own.

### Adversarial review (round 10)

| Finding | Severity | Fix | Regression test |
| --- | --- | --- | --- |
| Any button-less `mousemove` ended a native drag still going: one a page script dispatched, or one from a second input device. | Medium | Only an event the browser raised counts, and only if no `drag` or `dragover` event follows within 500 ms (a live drag keeps raising them). | `A_drag_released_outside_the_window_ends_as_a_cancel_and_the_next_one_works` (the module's own end on Firefox, whose driver reports none) |
| The keyboard's "scroll on for more" scrolled any scrollable ancestor of the last place, on every retry, for lists that are not virtualized; it never scrolled a virtualized list with a target after it, nor one scrolling with the window, nor right to left. | Medium | Rebuilt around Virtualize's spacer: a step that would leave the current list (or go nowhere) while that list has an empty, inline-sized spacer that way scrolls the spacer into view once and waits (up to 2 s, retries do not scroll). After 1.5 s it goes on as if there were none, so a look-alike element can only delay a step. | `VirtualizedTests` at full key speed on all engines; `AMoveWithRowsStillComingThatWayAsksAgainWaitingForThem` |
| A cancel during the wait was announced after a move the retries made. | Low | A waiting ask after the keyboard session ended neither scrolls nor moves. | Reasoned (the queue serializes the cancel after the wait). |
| The hybrid harness was built only when missing - stale against library changes - and could be built mid-run, under the running demo; a build that timed out was left running. | Medium | Built incrementally before anything runs (`BDND_E2E_SKIP_BUILD=1` skips it, as CI does after its own build step); killed on timeout; a failure is reported by the hybrid tests. | `HybridHostingTests` |
| A failing Playwright shutdown skipped the hybrid one (a window, WebView2 processes and a profile folder left behind). | Low | try/finally. | Reasoned. |
| Problems raised during a reload were cleared unseen; the cleanup's assertion buried a test's own failure. | Low | Cleared before the reload, asserted only after a passing test. | `HybridHostingTests` |
| Tests that could not fail: the bin test (palette blocks are ignored by `Remove` anyway), the forced-colors test where emulation does not force colours. | Low | The bin test checks the page can take the block and that no drop was raised; the line must be drawn in the system's Highlight. | Same tests |
| The incomplete-list caption hid when no file could be read. | Low | Shown whenever the list is incomplete. | `An_unreadable_entry_is_skipped_and_marks_the_list_incomplete` |

Not changed: processes tied to the test host's lifetime (a killed test host can leave the harness window behind) -
a Windows job object for a test-only harness was judged not worth its P/Invoke.

### Validation (final)

| Check | Command | Result |
| --- | --- | --- |
| Build | `dotnet build Bit.Bdnd.slnx` (now with the hybrid harness) | Succeeded, no compiler warnings (the MCP suite's last one is fixed). The harness build reports the WebView2 package's `WindowsBase` reference conflict (MSB3277), as the Brouter and Butil harnesses do. |
| Unit | `dotnet test --project Tests/Bit.Bdnd.Tests/Bit.Bdnd.Tests.csproj -f net8.0` (and net9.0, net10.0) | 382 passed on each (1146). |
| MCP | `dotnet test --project Tests/Bit.Bdnd.Tests.Mcp/Bit.Bdnd.Tests.Mcp.csproj` | 101 passed. |
| E2E Chromium | `dotnet test` in `Tests/Bit.Bdnd.Tests.E2E` | 143 passed, including the 8 Blazor Hybrid tests in a WebView2. |
| E2E Firefox | `BDND_E2E_BROWSER=firefox dotnet test` | 128 passed, 14 skipped (Chromium-only and hybrid), 1 failed: the outside-release test, whose move back came inside the new half-second confirmation and met the driver's stray dragover. Fixed in the test; rerun on Firefox twice, Chromium and WebKit: passed. |
| E2E WebKit | `BDND_E2E_BROWSER=webkit dotnet test` | 129 passed, 14 skipped, 0 failed. |
| Trim gate | `bash Tests/Bit.Bdnd.Tests.E2E/publish-gate.sh <out>` after a clean | No trim/AOT analysis warnings originate in Bit.Bdnd. |
| E2E on the trimmed publish | `BDND_E2E_PUBLISHED_HOST=<out> dotnet test` | 143 passed (Chromium). |

The matrix has no row left Partial, Untested or Missing. What the suite cannot do - listen to screen readers, touch
real devices - has a checklist in the E2E README, to be run and recorded here before a release.

## Round 11 - code review of the branch

A review of the whole branch found fifteen issues; all were taken.

| Finding | Severity | Fix | Regression test |
| --- | --- | --- | --- |
| A source dragged again while its last drag still ended (an async `OnDrop` awaiting) ended that drag through `AbortAsync` with the new item, and the new drag's `OnEnd` got none: the item lived in a field the next drag overwrote before `BeginDrag`. | High | `OnEnd` reads the item from the manager; the field is gone. | `ADragOfTheSourceBeginningWhileItsLastDragEndsHandsEachOnEndItsOwnItem` |
| During the grace period after a touch press the source's `dragstart` listener still tagged the refused native drag and began a session: a spurious `OnBegin`/`OnEnd`, and any pointer drag aborted. | High | The listener leaves a drag the document's capture listener refused (`defaultPrevented`) alone. | Reasoned (the platform's long-press drag cannot be synthesized). |
| A folder-expanding drop reported the leave before the drop, which forgot the edge: `OnNativeDrop` always got `ClosestEdge` null. | Medium | The target keeps the edge a native drop would carry when the leave comes, for the drop that follows. | `ANativeDropWhoseHoverEndedBeforeItStillCarriesTheEdge`, `ANativeDropCarriesNoEdgeAHoverBeforeItLeftBehind` |
| A pointer drag ended by the window losing the focus (or the page hidden) left the click suppression waiting for a release that never came: the next click was swallowed. A press of another pointer that ended a drag swallowed its own click. | Medium | The next press ends the suppression; a drag a new press ended suppresses nothing. | `The_next_click_still_clicks_after_a_drag_the_window_cancelled_and_whose_release_went_unseen` |
| Arrow keys still on their way to .NET when Enter (or Space) dropped navigated without the session: from the source's original element, without the reorder check or the wait for a virtualized list's rows. | Medium | Each source keeps its last keyboard session; keys pressed before a drop move from where its item is - waiting for rows too - and a draggable adopting the item updates it. A cancel still stops them. | Reasoned (key timing over the circuit). |
| A folder walk reaching `MaxFiles` on its last file reported the list incomplete. | Low | The walk stops at a matching file past the limit, which is not counted. | `A_folder_of_exactly_the_file_limit_is_listed_complete` |
| A native drop whose files were still being listed when a newer drop (or the target's disposal) came took `_lastDrop` from the newer one: stale `CanRead`/`Released` flags. | Medium | An outdated drop is dropped, as an outdated folder walk is. | `ADropOutdatedByANewerOneWhileItsFilesWereListedIsDropped`, `ATargetDisposedWhileTheFilesOfADropWereListedHandsNoDrop` |
| A draggable removed inline during a state-change notification still ran its predicates from the delegate snapshot. | Low | `OnStateChanged` returns once disposed, as the drop target's does. | `ADraggableRemovedWhileADragStateChangeIsNotifiedNoLongerRunsItsPredicates` |
| Keyboard dragging on by default made every draggable a tab stop, with an opt-out per draggable only. | Medium | `BdndProvider.AllowKeyboard` (the manager's `AllowKeyboard`) turns it off for a whole provider; the upgrade note says so, and names hosts that handle Space or Enter themselves. | `AProviderTakesEveryDraggableOutOfKeyboardDraggingAndBack`, `ADraggableOfAProviderThatTurnsTheKeyboardOffIsNotPickedUp`, `TurningTheKeyboardOffOrOnNotifiesTheSources` |
| `EndAsync` and `AbortAsync` repeated the `OnEnd` block. | Low | One `StartOnEnd`. | Existing lifecycle tests |
| `ReportAsync` was copied in the draggable and the drop target; the accept list was built in the drop target and the drag layer. | Low | `BdndDragDropManager.ReportExceptionAsync(exception, dispatch)`; `BdndAcceptTypes.Of`. | Existing tests |
| A draggable that took the focus over during a drag was asked to again on every render (one interop call each). | Low | The manager records who took it; that draggable is not asked again during the drag. | `ADraggableThatTookTheFocusOverDuringADragIsNotAskedToAgain`, `TheFocusTakenAfterADragIsTakenForGood` |
| A pointer drag reported edge changes to targets that refuse the item, or belong to another provider - a circuit message each, discarded. | Low | Only targets that can drop get them while hovering; the drop reports every edge (`canDrop` may lag right after the drag began). | Existing indicator tests |
| Interop types lacked `[DynamicDependency(All)]` at their call sites (`KeyboardNavigateAsync`, `GetNativeDropFilesAsync`, the `[JSInvokable]` methods taking `BdndDomRect` and `BdndNativeDropPayload`, the registrations). | Low | Added, besides the central roots. | `BdndTrimmingTests` |
| The touch helpers sat between `NativeDataTransferAsync`'s summary and the method. | Low | Each helper has its own summary. | - |

The new tests were checked against the code without each fix: they fail there.

### Validation

| Check | Command | Result |
| --- | --- | --- |
| Build | `dotnet build Bit.Bdnd.slnx` | Succeeded; the only warning is the hybrid harness's known MSB3277. |
| Unit | `dotnet test --project Tests/Bit.Bdnd.Tests/Bit.Bdnd.Tests.csproj` | 393 passed on each of net8.0, net9.0, net10.0 (1179). |
| MCP | `dotnet test --project Tests/Bit.Bdnd.Tests.Mcp/Bit.Bdnd.Tests.Mcp.csproj` | 101 passed. |
| E2E Chromium | `dotnet test` in `Tests/Bit.Bdnd.Tests.E2E` | 145 passed, including the 8 Blazor Hybrid tests. |

## Round 12 - every demo page in every engine

Every demo page was driven by hand-written Playwright scripts in Chromium, Firefox and WebKit, beyond what the suite
asserts: each page at desktop and phone width, light and dark, for console errors, failed requests, horizontal
overflow and the error banner (none on any of the 270 loads); mouse drags through every page's own scenario, with
screenshots of the hover states; a keyboard drag on every page; touch drags (Chromium) on fifteen pages at phone
width; the site's search palette, theme toggle, docs drawer and "View code". Seven problems turned up, three of them
in the library.

| Finding | Where | Fix | Regression test |
| --- | --- | --- | --- |
| Firefox: a target kept its hover - and its insertion line - after the drag had left it, so two lines showed (the source's own slot kept one for the whole drag). Gecko rebuilds an element's box when its line, a pseudo-element, appears, and then raises no `dragleave` for it: the next element is entered "from nowhere". Every edge demo showed it. | Library | A drag event captured on the document leaves every hovered target that is not on the event's path. | `Only_the_target_under_the_pointer_shows_an_edge` (fails on Firefox without the fix) |
| WebKit: the first press after a native drag raises no `pointerdown` - the drag swallowed the release, so WebKit still counts the button as down and reports the press as a `pointermove` whose button changed. A `PointerDrag="All"` source did not drag on that press, a press in a text field inside a draggable dragged the box instead of selecting text, a no-drag region went by the previous press, and the press did not cancel a keyboard drag. | Library | Such a `pointermove` (primary button changed, no other held) is taken as the press by the sources and the keyboard session. | `A_press_in_a_text_field_right_after_a_drag_still_selects_text_and_drags_nothing`, `A_pointer_drag_right_after_a_native_drag_drops` (fail on WebKit without the fix) |
| Every engine: text selected in a text field inside a draggable and dragged out of it was tagged as the source's drag - dropped on the bin, it dropped the box. | Library | A `dragstart` raised inside a form field is left to the field: a native text drag. | `Text_selected_in_a_text_field_drags_as_text_and_not_as_the_box` (fails on every engine without the fix) |
| Copy or move: the bin had a fixed height; from the seventh drop on, the list spilled out of it, the count above the bin in white on the light page. | Demo | The bin grows with its list. | `The_dustbin_grows_with_the_list_of_what_it_took` |
| Dustbin stress test: a bin's types broke inside a word ("knight/card/dr" "ag"). | Demo | Spaced separators, so a long list wraps between the types. | - |
| Drag around: a box dropped near the board's edge was drawn over the page beyond it. | Demo | The board clips; the point the box was held by is always on the board (the board is the target), so it can be picked up again. | - |
| Page builder: the bin's two-line "Release to remove" touched its left edge. | Demo | Padding, centred text. | - |

### Engine notes found on the way

- Firefox logs a warning on every page that the WebAssembly `try` instruction is deprecated: it comes from the .NET
  runtime's `dotnet.native.wasm`, not from the library or the demo.
- Only Chromium's driver drags through the browser's drag controller, which scrolls a container held at its edge;
  the scripted drags of Firefox and WebKit do not (the suite already treats it as Chromium-only).
- With the three engines' suites running side by side, four failures over two such runs came from the load alone: a
  WebKit browser crash, the tray's auto-scroll (once on WebKit before these fixes, once on Chromium after them) and an
  aborted WebAssembly download in the sidebar sweep. Each test passed on its own, the Chromium ones three times in a
  row.

### Validation

| Check | Command | Result |
| --- | --- | --- |
| Build | `dotnet build Bit.Bdnd.slnx` | Succeeded; the only warning is the hybrid harness's known MSB3277. |
| Unit | `dotnet test --project Tests/Bit.Bdnd.Tests/Bit.Bdnd.Tests.csproj` | 393 passed on each of net8.0, net9.0, net10.0 (1179). |
| MCP | `dotnet test --project Tests/Bit.Bdnd.Tests.Mcp/Bit.Bdnd.Tests.Mcp.csproj` | 101 passed. |
| E2E Chromium | `dotnet test` in `Tests/Bit.Bdnd.Tests.E2E` | 148 of 150 passed in the run beside the other engines, including the 8 Blazor Hybrid tests; the 2 that failed on load passed three times in a row on their own. |
| E2E Firefox | `BDND_E2E_BROWSER=firefox dotnet test` | 136 passed, 14 skipped (Chromium-only and hybrid). |
| E2E WebKit | `BDND_E2E_BROWSER=webkit dotnet test` | 136 passed, 14 skipped. |

## Round 13 - every feature on a demo page

Each demo page was read against the README and the feature matrix: what the page shows, what its text says, and which
public parameter, event or helper no page showed at all. The landing page still answered two questions with what was
true before rounds 2 and 3, and a dozen features had tests and documentation but no page.

| Finding | Where | Fix | Test |
| --- | --- | --- | --- |
| The landing page's FAQ said Bdnd has no touch support and cannot read a dropped file's contents. | Home | Rewritten from what the library does (pointer drags, `OpenReadStreamAsync`, folders); a keyboard question added; capability cards for drop indicators, touch and the keyboard; the comparison table lists touch and keyboard. | - |
| No page used the provider's `OnDragStart`/`OnDragEnd`, a target's `OnEnter`/`OnLeave` or `Disabled`, or a `Manager` read from outside the provider. | New page | `/other/drag-events`: a log of every event of a drag, an undo history built from `OnDragEnd`'s drop result, a tray that can be closed, the drag state read through the page's own manager. | `DragEventsTests` |
| No page showed separate drag contexts. | New page | `/other/contexts`: providers side by side, one nested in another, and two providers sharing one manager. | `ContextsTests` |
| Nested drop targets had no `OnDrop`: the drop order and the result rule were nowhere to be seen. | Nesting - drop targets | Every level logs its `OnDrop` and sets a result; `OnEnd` shows the one that won; a "greedy" switch (`Monitor.DidDrop()`). Rewritten as a Razor template instead of a render-tree builder. | `A_drop_reaches_the_nested_targets_innermost_first...`, `Greedy_targets_leave_a_drop...` |
| `CanDrag` and `CanDragPredicate` were on no page. | Nesting - drag sources; page builder | "Forbid drag" switches (React DnD's own example has them); a page of six blocks at most, whose palette stops dragging. | `A_source_that_may_not_drag_hands_the_press...`, `A_full_page_stops_the_palette_from_dragging...` |
| `SourceClientOffsetOn`, `KeepTo`, `SnapTo` and `Clamp` were on no page; neither were most of the layer's offsets. | Drag around - custom layer | Four modes (free, horizontal only, snap to grid, inside an area) applied to the preview and the drop alike; a second layer shows every offset live; the layer takes `Accept`. | `Snapped_to_the_grid...`, `Kept_to_the_horizontal_axis...` |
| `BdndAnnouncements` and the provider's `AllowKeyboard` were on no page, and a sighted reader could not see what the live region says. | Keyboard | English or German texts, a panel with what was announced, a keyboard switch, and a column that can be locked (`Disabled`). | `Announcements_given_to_the_provider...`, `The_provider_can_turn_keyboard_dragging_off...` |
| `TouchDelay`, `DragThreshold`, `AutoScroll` and the other `PointerDrag` modes could not be tried. | Touch and pointer drags | Controls under the tray, and a note on what each does. | existing `PointerTests` |
| `BdndList.ReorderIndex`/`Reorder` and `reversed` were on no page. | Grid; drop indicators | The grid reorders with them; the labels can be laid out right to left. | `Right_to_left_a_label_dropped_on_the_left_edge...` |
| The README's "a drag layer can draw 3 cards" had no example; nothing gave the archived cards back. | Dragging several items | A `BdndDragLayer` badge (skipped for keyboard drags through `GetDragInput()`) and "Put them back". | `A_drag_layer_says_how_many_cards...` |
| `LastModified`, `Data` and the editable-field rule of native drops were on no page. | Native files; native HTML | A "Modified" column; the drop's `Data` listed; a text-only target with a field inside. | `Files_dragged_in_from_the_system...` (date), `Text_dropped_on_a_field_inside_a_target...` |
| Only `copy` and `move` drop effects were shown. | Drop effects | `link` and `copyMove` boxes; every value in the note. | existing `CustomizeTests` |
| Texts that left things out: the tutorial's colours, the handle page's "previews", what a dashed bin means, keyboard use on the sortable pages; sortable cards had no `Label`; the knight and the squares had none. | Several | Legends and sentences added (details in notes under the samples, so every sample stays above the fold); `Label` on the sortable cards, the knight and every square (chess notation). | existing tests |

The first full run showed the limit of that: four tests failed because a longer subtitle (or controls above a tray)
had pushed a sample below the 900-pixel viewport, where the suite's mouse cannot reach. Subtitles say what a page shows;
the rest went into notes under the samples, and the pointer page's controls under its tray.

### Validation

| Check | Command | Result |
| --- | --- | --- |
| Build | `dotnet build Demo/Server/Bit.Bdnd.Demo.Server.csproj` | Succeeded, no warnings from the demo. |
| MCP | `dotnet test --project Tests/Bit.Bdnd.Tests.Mcp/Bit.Bdnd.Tests.Mcp.csproj` | 101 passed. |
| E2E Chromium | `dotnet test` in `Tests/Bit.Bdnd.Tests.E2E` (all but the hybrid and perf classes) | 156 passed, the 15 new tests included. |
| E2E Firefox | `BDND_E2E_BROWSER=firefox dotnet test`, the classes of the pages this round changed | 134 passed, 5 skipped (Chromium-only). |
