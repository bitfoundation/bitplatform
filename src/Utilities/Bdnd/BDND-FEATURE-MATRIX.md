# Bit.Bdnd feature matrix

The status of every capability Bdnd is measured against, and how the leading libraries approach it. The plan
these statuses feed is [BDND-ROADMAP.md](BDND-ROADMAP.md); the history of every change is
[BDND-IMPLEMENTATION-LOG.md](BDND-IMPLEMENTATION-LOG.md).

Status values:

- **Verified** - implemented, covered by automated tests (unit and, where the browser matters, E2E) and documented.
- **Untested** - implemented, but its tests do not cover the behaviour that matters (usually the browser half).
- **Partial** - some of the capability exists; the gap is named.
- **Missing (Pn)** - not implemented; P0 is release-blocking, P3 is nice to have.
- **Excluded** - deliberately out of scope, not applicable, or prevented by the platform; the reason is given.

The *Baseline* column is the state at the start of round 1; *Status* is the current state.

Competitors: **dk** dnd-kit, **SJ** SortableJS, **RD** React DnD, **PD** Pragmatic drag and drop,
**IJ** Interact.js, **N** the native HTML5 drag-and-drop API.

## A. Core dragging and dropping

| Capability | How others do it | Baseline | Status | Plan / notes |
| --- | --- | --- | --- | --- |
| Start, move, drop, cancel (mouse) | All; RD/PD on N | Verified | Verified | |
| Typed payloads and type-based acceptance | RD `type`/`accept`; PD `canDrop`; dk `data` + `accepts` | Verified | Verified | Strings match types, `TItem` types the payload. |
| Custom acceptance predicate | RD `canDrop`; PD `canDrop`; dk collision filter | Verified | Verified | `CanDropPredicate`. |
| Combined source + target elements | All | Verified | Verified | Nest a target in a draggable. |
| Multiple independent contexts | RD `DndProvider`; dk `DndContext` | Verified | Verified | Nested providers ignore each other's drags. |
| Shared context across regions | RD/dk one provider | Verified | Verified | One provider, or one `Manager` for two providers. |
| Nested targets, deep and shallow hover | RD `isOver({shallow})`; PD `dropTargets` chain | Verified | Verified | |
| Nested drop propagation order | RD innermost first, outer may override result | Verified | Verified | |
| Enter / leave notifications | PD `onDragEnter`/`onDragLeave`; dk `onDragOver` | Partial | Verified | `OnEnter`/`OnLeave` for in-app drags of every input (unit). Native drags: state only. |
| Begin / end / drop callbacks | All | Verified | Verified | |
| Drop results and `DidDrop` | RD | Verified | Verified | |
| Async drop handling | PD (app-defined) | Verified | Verified | `OnEnd` waits for async `OnDrop`s. |
| Disabled sources | All | Verified | Verified | `CanDrag`, `CanDragPredicate`. |
| Disabled targets | dk `disabled`; PD `canDrop` | Partial | Verified | `Disabled` (unit). |
| Exceptions from callbacks reach the app | - | **Missing (P0)** | Verified | Round 1: JS-invoked callbacks dispatch to the renderer, through the provider once the component is gone. |
| Source removed mid-drag | RD | Verified | Verified | |
| Target removed mid-drag | RD | Verified | Verified | |

## B. Input methods and activation

| Capability | How others do it | Baseline | Status | Plan / notes |
| --- | --- | --- | --- | --- |
| Mouse | All | Verified | Verified | Native HTML5. |
| Touch | dk TouchSensor; SJ fallback; RD TouchBackend; IJ | **Missing (P0)** | Verified (Chromium) | Round 2 pointer backend; E2E with DevTools-protocol touches. iOS/Android devices not yet verified by hand. |
| Pen / stylus | dk PointerSensor; IJ | Missing (P1) | Verified (Chromium) | Handled as touch (delay, threshold); round 6 E2E drives a pen through the DevTools protocol. |
| Keyboard dragging | dk KeyboardSensor; rbd | **Missing (P0)** | Verified | Round 1: Space/Enter, arrows, Home/End, Escape; unit + E2E. |
| Activation distance | dk `activationConstraint.distance`; SJ `fallbackTolerance` | Missing (P1) | Verified | `DragThreshold`; click stays a click (E2E). |
| Activation delay / press-and-hold | dk `delay`+`tolerance`; SJ `delay` | Missing (P1) | Verified | `TouchDelay` (250 ms) with a 4 px tolerance; swipe scrolls (E2E). |
| Click vs drag disambiguation | dk distance; N threshold | Partial | Verified | Threshold, and the release's click swallowed after a drag. |
| Drag handles | All | Verified | Verified | |
| Non-draggable regions inside a source | SJ `filter`; dk activator only | Missing (P2) | Verified | `data-bit-bdnd-no-drag` (pointer and native); form fields never start a pointer drag. |
| Interactive descendants (inputs, buttons) | SJ ignores inputs; dk app-defined | Partial | Verified | Round 8: a press on a form field or a `data-bit-bdnd-no-drag` region holds the source non-draggable until it ends - text selects, no native drag (E2E on Chromium and Firefox; without it Chromium dragged and Firefox selected nothing). Pointer drags skip them too. |
| Escape cancels | All | Untested | Verified | Keyboard and pointer drags (E2E); native drags by the browser. |
| Release outside the window | N; dk pointer capture | Untested | Untested | Native. |
| Window blur / tab hidden | dk cancels | Untested | Verified | Pointer drags cancel on blur and on a hidden page (round 6 E2E). |
| Scroll vs drag on touch | dk delay + `touch-action`; SJ `touchStartThreshold` | Missing (P0) | Verified (Chromium) | Delay on sources, `touch-action: none` on handles, cancelable touchmove from registration (WebKit). |

## C. Previews and visual feedback

| Capability | How others do it | Baseline | Status | Plan / notes |
| --- | --- | --- | --- | --- |
| Native drag image | N; RD/PD | Verified | Verified | |
| Custom pointer-following preview | RD drag layer; dk `DragOverlay` | Verified | Verified | `BdndDragLayer`. |
| Hide the native image | RD `getEmptyImage`; PD `disableNativeDragPreview` | Verified | Verified | `UseCustomDragLayer`. |
| Whole source as image when dragging by handle | RD; PD | Verified | Verified | |
| Preview outside clipping/stacking contexts | dk overlay portal; SJ `fallbackOnBody` | Partial | Verified | Pointer preview in the top layer (popover); drag layers stay app-positioned. |
| Stable styling hooks | dk data attributes; SJ classes | **Missing (P1)** | Verified | Round 1: `data-bit-bdnd-dragging`, `-can-drop`, `-over`, `-over-shallow`. |
| Insertion indicators / placeholders | PD drop-indicator + closest edge | Missing (P1) | Verified | `Edges`, `ClosestEdge`, `data-bit-bdnd-edge` (E2E: vertical, horizontal). |
| Drop animations / sortable transitions | dk, SJ FLIP | Excluded | Excluded | Bit.Bortable's domain; Bdnd renders whatever the app renders. |
| Reduced motion | dk respects | N/A | N/A | Bdnd animates nothing; the pointer preview moves with the pointer only. |

## D. Sorting and collections

| Capability | How others do it | Baseline | Status | Plan / notes |
| --- | --- | --- | --- | --- |
| Reorder within a list | All | Partial | Verified | Edges + `BdndList` (drop indicators demo); the React DnD hover recipe still works. |
| Move between lists | All | Partial | Verified | `BdndList.Move` across lists (E2E). |
| Insertion before/after a target | PD closest edge; dk sortable | Missing (P1) | Verified | Innermost hovered target only; RTL via `reversed`. |
| Deterministic destination index | PD `getReorderDestinationIndex` | Missing (P1) | Verified | `BdndList.InsertionIndex`/`ReorderIndex`/`Move`/`Reorder`. |
| Empty-list drops | All | Verified | Verified | A list is a drop target. |
| Clone vs move | SJ `pull: clone`; RD drop effects | Verified | Verified | `DropEffect` + `OnEnd`. |
| Multi-item drag | SJ MultiDrag; dk app-defined | Partial | Verified | A collection as the item (`ItemFactory`, `IsDraggingOverride`); round 8 demo with checkboxes, mouse and keyboard (E2E). |
| Swap sorting, FLIP animation, thresholds | SJ | Excluded | Excluded | Bit.Bortable implements SortableJS. |
| Grids / wrapping layouts | dk `rectSortingStrategy`; SJ | Partial | Verified | Round 6: grid demo; pointer drops across rows; keyboard Up/Down go a row (E2E). |
| Capacity limits, duplicate prevention | app-defined everywhere | Verified | Verified | `CanDropPredicate`. |
| Undo/redo integration | app-defined | Partial | Verified | Drop results + `OnDragEnd`/`DragEnded` with `DidDrop` and the result (README recipe, unit). |

## E. Complex interfaces

| Capability | Baseline | Status | Plan / notes |
| --- | --- | --- | --- |
| Kanban boards | Partial | Verified | Keyboard kanban and two-list indicators demos (E2E). |
| Trees and outlines | Partial | Verified | Round 4 tree demo with edges, own-subtree refusal and keyboard (E2E); README recipe. |
| Dashboards, page builders, palettes | Partial | Partial | Copy-or-move demo. |
| Free positioning (canvas) | Verified | Verified | Drag-around demos. |
| Virtualized lists | Untested | Verified | Round 6: 1,000-row `Virtualize` demo; pointer drag with auto-scroll past the source's removal, keyboard walk (E2E); README section. |
| Action drop zones (bins) | Verified | Verified | Dustbin demos. |

## F. Target detection and movement constraints

| Capability | How others do it | Baseline | Status | Plan / notes |
| --- | --- | --- | --- | --- |
| Pointer intersection | N hit test; dk `pointerWithin` | Verified | Verified | |
| Rectangle / closest-center / closest-corner collision | dk | Excluded | Excluded | N decides the target from the element under the pointer; the pointer backend keeps the same rule so both backends agree. |
| Nested precedence | RD | Verified | Verified | Innermost first. |
| Axis lock, snapping, bounds | dk modifiers; IJ modifiers | Partial | Verified | Round 8: `PreviewAxis` keeps the pointer copy on an axis (E2E, virtualized list); `BdndXYCoord.KeepTo`/`SnapTo`/`Clamp` and `SourceClientOffsetOn` for drag layers (unit). The browser's native drag image cannot be constrained. |
| Exclusion regions | SJ `filter` | Missing (P2) | Verified | `data-bit-bdnd-no-drag`. |

## G. Auto-scrolling

| Capability | How others do it | Baseline | Status | Plan / notes |
| --- | --- | --- | --- | --- |
| Viewport edge scrolling | N (browser); dk; SJ | Partial | Partial | Pointer drags scroll the page (axes from its overflow); HTML5 drags as the browser does. |
| Nested scroll containers | dk; SJ `scrollSensitivity` | Partial | Verified (pointer) | Innermost container that can scroll that way (E2E); HTML5 drags as the browser does. |

## H. Native data

| Capability | How others do it | Baseline | Status | Plan / notes |
| --- | --- | --- | --- | --- |
| Files (metadata) | RD/PD | Verified | Verified | |
| File contents | PD gives `File` objects | Missing (P1) | Verified | `OpenReadStreamAsync` over `IJSStreamReference`; size limit; lifetime until the next drop (E2E reads a dropped file). |
| Directories | PD `getFiles` + entries | Missing (P2) | Untested | `ExpandDirectories` walk (capped, per-entry errors, `FileListIncomplete`); synthetic drags cannot carry folder entries, so no automated test. |
| Text, URLs, HTML | RD/PD | Verified | Verified | Only accepted types are read. |
| Editable fields inside targets | PD | Verified | Verified | |
| MIME filtering while hovering | PD `containsFiles` | Missing (P2) | Verified | `AcceptFileTypes` (MIME-only lists refuse on hover; extensions decide at the drop); `MaxFiles`, `TotalFileCount`. |
| Custom native data for external destinations | PD `getInitialDataForExternal` | Missing (P2) | Verified | `NativeData` (E2E checks the dragstart's types). |

## I. Accessibility

| Capability | Baseline | Status | Plan / notes |
| --- | --- | --- | --- |
| Keyboard pick-up, move, drop, cancel | **Missing (P0)** | Verified | Round 1; sortable lists move one place per key. |
| Live-region announcements | **Missing (P0)** | Verified | `BdndAnnouncements`, replaceable per provider. Manual screen-reader passes not yet recorded. |
| Screen-reader instructions | **Missing (P0)** | Verified | `aria-describedby` on hosts and handles. |
| Focus kept and restored | Missing (P0) | Verified | Kept through re-renders, handed to the draggable showing the same item. `ItemFactory` sources cannot be matched. |
| Non-drag alternatives | Missing (P1) | Verified | Documented pattern + kanban demo (move buttons, E2E). Programmatic drops: Excluded - the app's own method is the API. |
| Forced colors / high contrast | N/A | Partial | No library visuals; the demo shows a forced-colors rule on the data attributes. |

## J. Reliability, hosting, performance

| Capability | Baseline | Status | Plan / notes |
| --- | --- | --- | --- |
| Deterministic lifecycle, idempotent cleanup | Verified | Verified | |
| Blazor WebAssembly | Verified | Verified | E2E runs on WebAssembly. |
| Blazor Server | Untested | Verified | `/hosting/server-check` over a circuit, 8 E2E tests (native, keyboard, mouse and touch pointer, layer, edges, file streams, moving between render modes). |
| Blazor Hybrid | Untested | Untested | Same out-of-process code path as Server; no web view automation here. |
| Prerendering | Verified | Verified | |
| Trimming / AOT | Verified | Verified | Publish gate + `BdndTrimmingTests`. |
| Measured performance | Missing (P2) | Verified | `PerfTests` baseline vs current (frames, long tasks, mutations); round 7: circuit messages per drag on Blazor Server asserted (16 for a 120-move native or pointer drag; a pointer drag sent 250 before `BdndDragLayer.Accept`). |
