# Bit.Bdnd roadmap

This file is the working plan for taking Bdnd from a faithful React DnD port on the HTML5 backend to a complete
drag-and-drop library for Blazor. It is kept next to the code so that every change to the plan is reviewed with
the change that motivated it. The capability-by-capability status lives in
[BDND-FEATURE-MATRIX.md](BDND-FEATURE-MATRIX.md); what each round did, measured and found lives in
[BDND-IMPLEMENTATION-LOG.md](BDND-IMPLEMENTATION-LOG.md).

## Where Bdnd started

Bdnd 10.x is a port of React DnD's model to Razor components: `BdndProvider` (the context), `BdndDraggable<TItem>`
(`useDrag`), `BdndDropTarget<TItem>` (`useDrop`), `BdndDragLayer<TItem>` (`useDragLayer`) and `BdndDragHandle`.
The drag state lives in C# (`BdndDragDropManager`, read through `IBdndMonitor`); a small TypeScript module
configures the native drag at `dragstart`, tracks hover per target, follows the pointer for drag layers, gives
drop-effect feedback and reads native drops.

What it does well, and what this plan keeps:

- A small, declarative surface whose semantics are precise and documented (callback order, nested drops
  innermost first, drop results, `DidDrop`, async `OnDrop`).
- Robustness across re-renders: a dragged source may be removed mid-drag and its `OnEnd` still runs; hover
  survives Blazor replacing the target's children.
- Render economy: components re-render only when the state they hand their content changes, and pointer moves
  cross the interop boundary only while a drag layer follows them.
- Strong test infrastructure: bUnit component tests, a Playwright suite that drives real HTML5 drags against the
  demo, a publish gate for trimming/AOT, and an MCP server whose API reference is reflected from the assembly.

What it lacked, in order of severity:

1. **No keyboard alternative and no screen-reader support.** WCAG 2.2 SC 2.5.7 (Dragging Movements, AA) and
   2.1.1 (Keyboard, A) cannot be met with Bdnd alone.
2. **No touch support.** The HTML5 drag-and-drop API is not reliably raised by touch on mobile browsers; Bdnd had
   no pointer-driven backend, so phones and tablets could not drag at all.
3. **Exceptions thrown by `OnEnd` and `OnNativeDrop` were lost**: those callbacks run from JS-invoked methods, so
   an exception became a rejected promise that the module swallowed.
4. **Sorting required hand-written geometry**: every hover measured the hovered element over interop
   (`GetBoundingClientRectAsync`) and compared the pointer with its midpoint; there was no insertion position, no
   drop indicator support and no collection helper.
5. **No auto-scroll** beyond what each browser does natively for HTML5 drags.
6. **Native file drops exposed metadata only**: no way to read a dropped file's contents.
7. **No stable styling hooks**: every visual state had to be rendered from the collected state by hand.

## Principles for the work

- Keep the React DnD model and its documented semantics. New input methods are additional *backends* that drive
  the same manager, so a target's `OnDrop`, a source's `OnEnd` and the collected states mean the same thing
  whichever way a drag was made.
- Keep high-frequency work in the browser (pointer tracking, hit testing, preview movement, auto-scroll) and send
  .NET only meaningful transitions (begin, hover enter/leave, edge changes, drop, end).
- Sorting at the SortableJS level (FLIP animations, swap thresholds, multi-drag) is what the sibling library
  **Bit.Bortable** is for; Bdnd provides the primitives a sortable list is built from (insertion edges, reorder
  helpers, keyboard moves), not a second sortable component.
- Accessibility is a release criterion, not an add-on: every drag must be possible without a pointer, and every
  essential operation must have a non-drag alternative pattern documented.

## Rounds

Each round runs the loop described in the mission: reassess, discover gaps, prioritize, implement, validate,
adversarially review, update this plan and the matrix, decide whether to continue.

| Round | Theme | Planned scope |
| --- | --- | --- |
| 1 | Reliability and accessibility foundation | Route exceptions from JS-invoked callbacks to the renderer; backend-neutral begin/hover/drop/end paths in the components and the manager; keyboard dragging (pick up, move between targets, drop, cancel), live-region announcements, screen-reader instructions and focus restoration; `data-bit-bdnd-*` styling attributes. |
| 2 | Touch, pen and pointer dragging | A pointer-events backend for touch and pen (and optionally the mouse): activation delay and distance, a cloned preview that follows the pointer outside clipping contexts, hit testing, nested drops innermost first, Escape/blur/visibility cancellation, click suppression after a drag, auto-scroll of the window and nested scroll containers. |
| 3 | Insertion positions and collections | Closest-edge tracking on drop targets (`Edges`, `ClosestEdge`) reported only when it changes, drop-indicator styling, `BdndList` reorder/transfer helpers, keyboard moves through edge positions, a Kanban demo. |
| 4 | Native data | Streaming the contents of dropped files, directory entries, MIME filtering while hovering, custom native data on in-app drags for external destinations, security guidance. |
| 5 | Lifecycle observers and extensibility | Enter/leave callbacks, manager-level lifecycle events (began, dropped, ended) for monitors, analytics and undo/redo, programmatic drops for non-drag alternatives. |
| 6 | Layouts and the remaining inputs (revised) | Keyboard Up/Down by row in wrapping grids, a grid demo; drags in `Virtualize` (the pointer copy outliving its source's container); pen input, window blur and hidden-page cancellation under E2E; touch and render-mode switching on the Blazor Server page. Performance moved to round 7: the matrix showed these as the larger gaps. |
| 7 | Performance | Circuit messages per drag on Blazor Server (found: any drag layer made every source send every pointer move; fixed with `BdndDragLayer.Accept`), frame rates against the baseline. |
| 8 | Interactive content, previews, selections (revised) | Fields and no-drag regions inside draggables, `PreviewAxis` and offset helpers, a multi-select demo; the round 6/7 review's fixes (row breaks, the copy's styling and visibility, own targets by the DOM). |
| 9 | Cross-browser validation and release readiness | Full E2E on Chromium, Firefox and WebKit; the remaining failures fixed or recorded; documentation, MCP catalogue and file hygiene; the release recommendation. |
| 10 | What remained | Blazor Hybrid under E2E (a WinForms BlazorWebView harness), folder drops, a release outside the window, touch on every engine, screen-reader semantics in the accessibility tree, forced colors, native edge scrolling, the virtualized keyboard walk, a page builder demo; a checklist for what only people and devices can check. |
| 14 | The improvement plan of November 2026 | A gap analysis of that plan against the code (below); moves worked out before they are made - one item or a selection, applied and reverted for undo (`BdndList.PlanMove`, `BdndListMove<T>`); tree moves that refuse cycles (`BdndTree`); the boundaries with the other bit utilities, documented. |

The scope of later rounds is revised at the end of every round from what the round found.

## Where it stands after round 10

Every capability in the matrix is Verified, Excluded or not applicable. Two checks are left to people, with a written
checklist in the E2E README and a place to record the outcome in the log:

- **Screen readers**: what they are given is tested (descriptions, roles, the live region, in Chromium's
  accessibility tree too); hearing NVDA, JAWS, VoiceOver and TalkBack read it is a listening pass.
- **Real touch devices and pens**: the touch path runs on all three engines from pointer events and in Chromium from
  synthesized touches; a finger on an iPhone, an iPad, an Android phone and a Windows touch screen is a hands-on pass.

## Round 14 - the improvement plan of November 2026, against the code

A plan arrived asking for, among much else, sensors, collision strategies, movement modifiers, transactional sorting,
multi-drag and integrations with Bortable, Blow, Bink, Bmotion, Blex, Bcrdt, BlazeDb and BevTools. Each of its
requests was read against the code, the tests and this file, and put in one of four classes.

**Already implemented and tested** (nothing to do): one terminal state per drag (a drag that begins while another is in
progress ends it; `OnEnd` waits for async drops); exceptions from callbacks reach the renderer; sources and targets
removed, re-parented or re-keyed mid-drag; nested targets and isolated or shared providers; mouse, touch, pen,
keyboard and native input with a distance threshold, a press-and-hold delay, no-drag regions and form fields;
cancellation by Escape, blur, a hidden page and a release outside the window; auto-scrolling of nested containers and
the page; drag layers filtered by type; axis, grid and bounds constraints (`PreviewAxis`, `KeepTo`, `SnapTo`, `Clamp`);
insertion edges, grids, trees and virtualized lists; announcements and instructions, translatable; forced colours;
file streams, size and count limits, MIME filtering and folders; circuit traffic measured on Blazor Server; the trim
and AOT gate; lazy loading; Hybrid under E2E; an MCP catalogue reflected from the assembly.

**Implemented but incomplete** (done this round):

- A selection could be dragged to a bin but not reordered: `BdndList.PlanMove` takes several indexes and moves the
  items together, keeping their order, within a list or to another.
- Undo was written by hand on every page that had it: a `BdndListMove<T>` is worked out before it is made, checked,
  applied and reverted - the integration point for an undo history or a state container's actions.
- Trees needed the app's own walk to refuse a node's own subtree: `BdndTree.Contains`, `ParentOf` and `PlanMove`,
  without recursion.
- Nothing said where Bdnd stops and the other utilities begin: the README's "Bdnd and the other bit utilities".

**Missing, appropriate, not done yet** (candidates for the next round, in order):

1. Announcements of a drag that carries several items speak the grabbed source's label ("Picked up Charlie."); a
   count ("Picked up 2 items.") needs the announcement to see the item - a field on `BdndAnnouncement`.
2. Keyboard bindings are WAI-ARIA's and fixed; remapping them has no practical request behind it yet.
3. Micro-benchmarks of the C# side (registration, hover chains with thousands of targets) beside the E2E frame and
   circuit measurements.

**Not appropriate for Bdnd** (and why):

- *Rebuilding Bortable, Blow, Bink or Bmotion on Bdnd's engine.* Each has its own browser module tuned to its
  interaction - Bortable's swap thresholds and FLIP, Blow's pointer capture on a transformed canvas, Bink's pressure
  sampling, Bmotion's gestures - and a shared engine would tie their releases together for no behaviour a user would
  notice. Bdnd works beside them (dragging onto a canvas, a palette, a layer list); the boundaries are documented.
- *Adapter packages for Blex, Bcrdt and BlazeDb.* The integration is plain C#: a drop handler calls the app's store,
  and a `BdndListMove<T>` is the reversible move a store keeps. A package would wrap one method call.
- *Collision strategies (closest centre, closest corner, rectangle overlap) and a public sensor interface.* The native
  backend decides the target from the element under the pointer and the pointer backend keeps the same rule, so a drop
  means the same whichever input made it; sensors run in the browser so that pointer moves do not cross the circuit.
  Gaps between items are covered by making the list a target too, and insertion edges cover before and after.
- *Drag-to-resize, drag-to-select, drag-to-connect, freehand drawing and pen pressure.* Gestures inside a surface, owned
  by Blow and Bink; Bdnd moves things between places.
- *Sorting animations.* Bmotion's `Layout` animates a list whose order a drop changed; Bdnd animates nothing.

