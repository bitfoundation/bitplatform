# Bink improvement programme: progress record

This file tracks the audit, the requirements matrix, the backlog and the verification evidence of the ongoing
effort to make Bink a complete drawing and ink infrastructure. It is a working record for maintainers, not user
documentation: the README is the user guide.

## Baseline (start of this round)

- Branch `msynk/14106-bink-improvements`, after commit `d1f929d95e improve Bink #14106`.
- `dotnet build Bit.Bink.slnx`: succeeds, one nullable warning in `Bit.Bink.Tests.Mcp/McpSurfaceTests.cs:58`.
- `Bit.Bink.Tests` (net10.0): 590 / 590 passing.
- Library: ~18k lines (C# engine, Blazor component, 1.6k lines of TypeScript), net8/9/10, trimmable, no runtime
  dependencies beyond `Microsoft.AspNetCore.Components.Web`.

## Assessment

Bink is already a serious engine: an immutable vector document of layers and elements, a closed and invertible
operation set that is the single unit of undo, diffing and collaboration, pressure-aware variable-width ink, three
erasers, selection/transform/snapping/lasso, clipboard, SVG + browser PNG/JPEG/WebP + a pure .NET rasterizer and PNG
codec, a validated compact file format, retained rendering over a command buffer, collaboration, and an honest README.

The architecture is sound and is preserved. The gaps are in breadth of editing features (grouping, locking,
alignment, more primitives), extensibility (custom element types cannot be saved), persistence helpers, and a few
platform capabilities. See the matrix.

### Architectural decisions taken in this round

| Decision | Rationale |
| --- | --- |
| Groups are a property of elements (`GroupIds`, innermost first), not a container element. | Keeps the operation set closed: grouping is a batch of replacements, so undo, serialization, clipboard and collaboration work unchanged. Members keep their own layers. Same model as Excalidraw. |
| Grouping gathers members contiguously in each layer's stacking order. | Prevents non-members sandwiched between members, which makes "bring group to front" and drag previews predictable. |
| A group with members outside the edited set is "partial": grouping keeps the new group inside it, ungrouping leaves it alone, duplicating keeps the copy in it. | Gives nested groups and "work inside a group" consistent, testable rules without a separate scene graph. |
| Element locks (`IsLocked`) protect from tools, not from the API. | Applications must be able to unlock; locked elements still draw, export and snap. |
| For edits made on a selection (duplicate, align/distribute/flip, ungroup), a group is whole when every *selectable* member is given; grouping counts every member. | A selection can never hold a locked member, or one on a locked or hidden layer, so the strict rule made a group with such a member impossible to treat as a group. Grouping must stay strict, or a new group would contain part of another. |

## Requirements matrix

Status: **Done** (implemented and verified by tests in this or an earlier round), **Partial**, **Planned**,
**Deferred** (with reason), **N/A**. Priority P0 correctness/security, P1 essential, P2 advanced, P3 specialised.

### A. Core graphics

| Requirement | P | Status | Evidence / notes |
| --- | --- | --- | --- |
| Surface lifecycle, resize, DPR, context loss | P0 | Done | `BinkCanvas` attach/dispose; `contextrestored` redraw; E2E DisplayAndDemoTests |
| Vector document, render order, coordinate spaces | P1 | Done | `BinkDocument`, `BinkViewport`; README "Coordinate spaces" |
| Clipping, compositing, opacity, fill rule, caps/joins | P1 | Done | `IBinkRenderContext`, three implementations, VisualRegressionTests |
| Culling, retained paths, batching | P1 | Done | `BinkCanvasResourceCache`, RetainedRenderingTests |
| Spatial index | P2 | Deferred | Linear culling measured at tens of µs for 10k elements (README Performance) |
| Headless render/export | P1 | Done | `BinkPngExporter`, `BinkSvgExporter` |
| Layer blend modes | P2 | **Done** | LayerFeatureTests |

### B. Input and ink fidelity

| Requirement | P | Status | Evidence / notes |
| --- | --- | --- | --- |
| Mouse/touch/pen, capture, cancel, coalesced, multi-pointer | P0 | Done | bit-bink.ts, PointerRegressionTests, E2E |
| Pressure, tilt, twist capture | P1 | Done | `BinkInputSample`, serialized |
| Smoothing, spacing, simplification | P1 | Done | `BinkStrokeBuilder`, StrokeTests |
| Predicted events (not committed) | P1 | Done | PredictionTests |
| Palm rejection | P1 | Done | E2E KeyboardAndTouchTests |
| Pen eraser end | P1 | Done | `BinkDrawTool.HonorPenEraserButton` |
| Delegated ink trail (`navigator.ink`) | P3 | Planned | Chromium only; optional |
| Pen hover | P3 | Partial | Delivered to tools that ask (`WantsHover`); no hover cursor preview yet |

### C. Brushes

| Requirement | P | Status | Evidence / notes |
| --- | --- | --- | --- |
| Pen, pencil, marker, highlighter, pixel eraser | P1 | Done | `BinkBuiltInBrushes` |
| Custom brush registration and serialization | P1 | Done | `BinkBrushRegistry`; brush-specific settings saved since pass 9 |
| Pressure curves | P1 | Done | `BinkPressureCurve` |
| Calligraphy (nib angle) | P2 | **Done** | CalligraphyBrushTests |
| Independent pressure→opacity | P3 | Deferred | One filled outline per stroke cannot vary opacity without per-segment compositing artefacts |
| Textured/airbrush | P3 | Deferred | Raster stamping does not fit the vector model |

### D. Tools and primitives

| Requirement | P | Status | Evidence / notes |
| --- | --- | --- | --- |
| Freehand, erase (3 modes) | P1 | Done | |
| Rect, rounded rect, ellipse, line, arrow, highlight, callout, text, image | P1 | Done | |
| Polygon / polyline / diamond / triangle / star | P1 | **Done** | PrimitiveTests, PrimitiveToolTests |
| Generic path element (Bézier), SVG path data | P2 | **Done** | PrimitiveTests (parser, round trip, hostile input) |
| Selection, marquee, lasso, hit testing | P1 | Done | |
| Move/resize/rotate/duplicate/delete | P1 | Done | |
| Flip | P1 | **Done** | LayoutCommandTests |
| Align / distribute | P1 | **Done** | LayoutCommandTests |
| Snapping, guides, grid snapping | P1 | Done | `BinkSnapper` |
| Grouping (nested), ungrouping, work inside group | P1 | **Done (this round)** | GroupingTests (28 tests) |
| Element locking | P1 | **Done (this round)** | GroupingTests |
| Hide individual elements | P3 | Deferred | Layers provide visibility; per-element hide adds a second visibility model |
| Shape recognition, flood fill, gradients | P3 | Deferred | Evaluate after core breadth |

### E. Navigation

| Requirement | P | Status | Evidence / notes |
| --- | --- | --- | --- |
| Pan, zoom, fit, reset, wheel, pinch, keyboard, limits | P1 | Done | |
| Grid / dot background | P2 | **Done** | GridTests |
| Canvas rotation | P3 | Deferred | Every tool maps through `BinkViewport`; rotation would touch all of them for a niche feature |
| Minimap, rulers | P3 | Deferred | Application UI; possible from the public API |

### F. Document model

| Requirement | P | Status | Evidence / notes |
| --- | --- | --- | --- |
| Stable ids, ownership, ordering | P0 | Done | |
| Layers: add/remove/reorder/visibility/lock/opacity | P1 | Done | |
| Layer duplicate | P2 | **Done** | LayerFeatureTests |
| Groups | P1 | **Done (this round)** | see D |
| Metadata on document/layer/element | P1 | Done | |
| Custom element types that serialize | P1 | **Done** | CustomElementTests |
| Versioning, unknown-element preservation | P1 | Done | SerializationFidelityTests |
| Multiple pages | P3 | Deferred | One document per page is the supported pattern |

### G. Editing and history

| Requirement | P | Status | Evidence / notes |
| --- | --- | --- | --- |
| Undo/redo for every mutation, groups, compaction | P0 | Done | HistoryRegressionTests |
| Cancellation/rollback of gestures | P0 | Done | |
| Clipboard copy/cut/paste, cross-document | P1 | Done | |
| Keyboard shortcuts | P1 | Done | |
| Customizable key bindings | P2 | Planned | |
| History memory budget | P2 | Planned | Only a step-count limit today |

### H. Import, export, persistence

| Requirement | P | Status | Evidence / notes |
| --- | --- | --- | --- |
| Native format, validation, limits | P0 | Done | |
| SVG export | P1 | Done | |
| PNG/JPEG/WebP export | P1 | Done | |
| SVG import (safe subset) | P2 | **Done** | SvgImportTests, canvas drop tests |
| PDF export | P3 | Deferred | Evaluate after path element |
| Image import: file, clipboard | P1 | Done | |
| Image import: drag and drop | P2 | **Done** | BinkCanvasDropTests, E2E |
| Dirty tracking, autosave, storage abstraction | P2 | **Done** | PersistenceTests, E2E reload |

### I–N

| Requirement | P | Status | Evidence / notes |
| --- | --- | --- | --- |
| Eyedropper | P3 | **Done** | BinkCanvasDropTests (Chromium only at runtime) |
| Image filters/adjustments | P3 | N/A | Out of scope by design (README "What Bink deliberately is not") |
| Keyboard operability, live region, descriptions | P1 | Done | KeyboardTests, E2E |
| Group-aware keyboard navigation | P1 | **Done (this round)** | Tab treats a group as one stop |
| Blazor WASM/Server/Hybrid, prerender | P1 | Done | README Hosting models |
| Collaboration (optional module) | P2 | Done | CollaborationTests |
| Security: hostile documents, SVG safety | P0 | Done | RobustnessTests, ExportRegressionTests |

## Review passes

| # | Focus | Outcome |
| --- | --- | --- |
| 1 | Reconnaissance, baseline, requirements matrix; grouping and element locking | Implemented: `GroupIds`, `IsLocked`, `GroupElements`/`UngroupElements`/`SetLocked`, `ExpandToGroups`, group-aware select tool (double-click enters, Escape leaves, marquee/Tab by group), clipboard and duplicate group renaming, serialization, Ctrl+G / Ctrl+Shift+G / Ctrl+Shift+L. 28 new tests; suite 618/618. |
| 2 | Independent audits (browser layer, engine) and competitive research, run as parallel reviewers; every finding re-verified against the code before fixing | Fixed 10 defects, listed below. Two more found by the E2E suite. 14 new unit tests, 2 new E2E tests; unit 632/632, E2E 48/48, MCP 73/73. |
| 3 | Align, distribute, flip | `BinkDocument.AlignElements/DistributeElements/FlipElements` (groups move as units; text stays readable; callouts swap sides; double flip restores identity), canvas API, Shift+H/V. 9 tests. |
| 4 | Primitives | `BinkPolygonShape` (polygon/polyline/smooth/arrow heads, Diamond/Triangle/Regular/Star), `BinkPathElement` + `BinkSvgPathData` parser (all commands, arcs to cubics, bounded), shape kinds Diamond/Triangle/Polygon/Star, `BinkPolylineTool`, `IBinkTool.WantsHover`, serialization. 18 tests; suite 659/659. |
| 5 | Extensibility of the format | `BinkElementConverter<T>` + `BinkElementRegistry`: custom elements save, paste and synchronize; readers without the registration preserve them in place (bounds written). Collaboration reads with the registries it writes with. 6 tests. |
| 6 | Layers | Layer `BlendMode` (Multiply/Screen; DestinationOut refused) on canvas, raster and SVG; `DuplicateLayer`; file and wire formats. 4 tests. |
| 7 | Persistence | `IsDirty`/`MarkSaved(revision)`, `IBinkDocumentStore`, `BinkMemoryDocumentStore`, `BinkBrowserDocumentStore` (IndexedDB), `BinkAutosave` (debounce + ceiling, one write at a time, sync-context aware, retry, discard, restore). E2E restore-after-reload found that Blazor cannot stream an empty Blob; fixed with a presence byte and made the test double enforce it. 9 unit + 1 E2E test. |
| 8 | View aids and import | `BinkGrid` (lines/dots, coarsening, tile-cached paths; dots as dashed rows), drag-and-drop (`EnableDrop`: images, `.bink`, `.svg`), eyedropper, `BinkSvgImporter` (safe XML: no DTD, no fetching; shapes, paths, text, embedded images, transforms, viewBox, inheritance, style attribute; warnings for the rest), `ImportSvg` (one undo step, grouped), shape style `LineCap`/`LineJoin` with miter-aware bounds. Whiteboard demo restructured after E2E showed the canvas pushed below the fold. 21 unit + 1 E2E test. |
| 9 | Brushes | Brush parameters (`GetParameters`/`WithParameters`, `params` in the format, preserved by unknown brushes; fixes custom brush settings never being saved), `BinkCalligraphyBrush` (broad nib: convex hull per segment, non-zero union). 6 tests; suite 702/702; E2E 50/50. |
| 10 | Clipboard, history, accessibility, shortcuts | Copy puts a PNG of the selection beside the drawing (E2E reads `image/png` back); Bink paste prefers its own payload. `MemoryBudget`/`EstimatedMemory` on history (+ canvas `HistoryMemoryBudget`). Enter with a shape tool adds a shape for keyboard users. `DisabledShortcuts` + `BinkShortcut` chord normalization, honoured before tools and by the browser layer. README browser-support matrix. |
| 11 | Independent review of the whole diff (passes 1–10), parallel reviewers, each claim re-read against the code; the stream-limit claim checked against the shipped `Microsoft.JSInterop` assembly | 13 defects confirmed and fixed, listed below. 15 new tests, plus pass 10's 4 shortcut tests built now; suite 726/726 (net10.0; the net8.0/net9.0 builds compile, but those runtimes are not installed on this machine). |
| 12 | Performance: baseline (a worktree of `d1f929d95e`) against current, the drag hot path | A drag frame allocated 33% more than the baseline and ran slower. A probe measuring each stage's allocations put it on the history's memory accounting from pass 10: a hash set of every element per recorded step, about 150 KB per frame at 1,000 strokes, paid even with no budget set. Rewritten: payloads (points, text, images, paths) are reference-counted and counted once across all steps, which also corrects a large overestimate (a drag's steps were each charged the stroke's points); accounting runs only while a budget is set. After: time equal to the baseline within noise, allocation +3% (the 8 bytes `GroupIds` adds to each element record, copied twice per frame). 2 tests; suite 728/728. Details below. |

### Defects fixed in pass 2

| Severity | Defect | Fix | Evidence |
| --- | --- | --- | --- |
| High | Blazor Server: image export returned a byte array, one message over the 32 KB circuit limit; the circuit closed | Stream through `IJSStreamReference` | `An_exported_image_is_read_from_a_stream`, E2E `Export_downloads_a_real_png` |
| High | Blazor Server: long editor text and large sample bursts could exceed 32 KB | Text over 8 KB streamed (`OnTextEditorKeyStream`); samples chunked at 128 | `A_long_text_arrives_as_a_stream_and_is_committed` |
| High | Non-drawing tools lost every measured move when predictions followed coalesced samples; pinch ignored the first finger | TS leaves the dispatched sample unflagged; C# keeps each pointer's last measured move | `A_select_drag_moves_with_the_measured_samples...`, `A_pinch_follows_both_fingers...` |
| Medium | Pinch lost the first finger's first move (pan tool only learnt of the second finger) | Seed every touch already down when routing starts | same pinch test |
| Medium | `PasteAsync` never read the system clipboard: the module returned a stream reference where Blazor expects a Blob (found by the E2E suite) | Return the Blob | E2E `The_paste_button_reads_the_system_clipboard` |
| Medium | A late text commit (after the measuring round trip) was applied to the element being edited next | `CommitEdit(context, elementId, ...)`; canvas captures the id up front | `A_late_commit_saves_its_text_on_the_element_it_was_typed_for` |
| Medium | Selection kept elements whose layer became locked or hidden; Delete/arrows then edited them | Selection refuses and drops them | `The_selection_drops_what_its_layer_locks_or_hides...`, `Delete_after_locking_the_layer...` |
| Medium | A cancelled stroke/segment erase (palm, pinch) stayed and was recorded | Restore inverses, discard group | `A_cancelled_erase_puts_back_what_it_erased...` |
| Medium | Wet ink trail shown for select/shape/eraser drags and on read-only canvases | Style only for ink tools on editable canvases; none for the eraser end or multi-touch | `The_provisional_trail_is_only_offered_for_tools_that_draw_ink` |
| Medium | Hit tolerance and marquee padding wrong under non-uniform scale (edge-handle stretch) | Document-space measurement with per-segment band width and elliptical caps | `Hit_testing_a_stretched_*` |
| Medium | Safari: the Enter confirming an IME composition committed the text editor | Ignore `keyCode 229` | code review (no Safari in CI) |
| Low | `EnableKeyboardShortcuts=false` also disabled the text editor's Enter/Escape | Editor keys handled first | code review |
| Low | A copy with nothing to copy marked the clipboard as failed, so later pastes ignored the system clipboard | Distinguish a withdrawn write | code review |

### Research findings folded into the backlog

- An asset table (Excalidraw `files`, tldraw assets) would stop embedded images being re-sent with every copy and
  collaboration operation. P2.
- Fractional z-order keys would remove the "concurrent reorders are not reconciled" collaboration limit; it changes
  the format. P3, deferred.
- Clipboard: also write `image/png` so a copied selection pastes into chat and documents. P2.
- Delegated ink (`navigator.ink`) cannot be combined with a desynchronized canvas or the app's own prediction; Chromium
  only. P3.
- Shortcut customization has to be sent to the browser layer, which decides `preventDefault`. P2. Done in pass 10
  as `DisabledShortcuts`.
- Clipboard `image/png`: done in pass 10.

### Defects fixed in pass 11

| Severity | Defect | Fix | Evidence |
| --- | --- | --- | --- |
| Medium | A browser stream over its size limit throws `ArgumentOutOfRangeException` (confirmed in the shipped `Microsoft.JSInterop`), but every caller caught `IOException`: an oversized drop or paste escaped unannounced, `RestoreAsync` threw an undocumented exception. The test double threw `IOException`, so tests passed against behaviour the runtime does not have | `BinkInterop.ReadBytesAsync` checks the known length first and throws `IOException`; the double now throws what the runtime throws | `A_stream_over_the_limit_is_refused_as_too_large`, `PersistenceTests` load-limit test |
| Medium | A long (streamed) text commit read the editor's element id after the stream arrived, by when the click that ended the edit could have opened another text: A's text replaced B's and A's undo group stayed open | The browser callback fixes the element id before reading; Escape is applied at once without reading | `A_long_text_still_arriving_goes_to_the_box_it_was_typed_in` |
| Medium | A group was "whole" only if every member was given, but a selection never holds locked or hidden members: with one member locked, Ctrl+D copies joined the original group, align scattered the group, ungroup did nothing | Selection-driven edits (duplicate, layout, ungroup) count only selectable members; grouping still counts all; ungroup also removes the dissolved group from unselectable members | `A_group_with_a_locked_member_is_whole_to_what_is_done_with_its_selection`, `Grouping_counts_every_member_so_a_new_group_never_splits_one` |
| Medium | SVG import: `XmlReader.Skip` already moves to the next node, and the loop's `Read` stepped over it, so after foreign markup an element was dropped or inherited a closed group's transform and paint | Skip to the end tag instead | `Content_after_skipped_foreign_markup_keeps_its_place_in_the_tree` |
| Medium | SVG import refused any DOCTYPE, which Illustrator and older Inkscape write | `DtdProcessing.Ignore` with no resolver: skipped unread, entities never declared | `A_doctype_as_illustrators_write_it_is_skipped`; the entity-expansion test still refuses |
| Low–medium | `SaveNowAsync` during a timer save returned that save, which had serialized an older revision; an early return skipped the follow-up flag; a posted timer save could run after disposal | A forced save follows the one under way; the check moved inside `try`; disposed saves do nothing | `Saving_now_during_a_save_also_saves_what_changed_since_it_began` |
| Low–medium | A path could be added with more segments than the document's limits let it be read back, so the saved file failed to load | Added paths are held to the reader's bound (`MaxPointsPerStroke`, at most 100,000) | `A_path_the_document_could_not_read_back_is_not_added` |
| Low–medium | Arcs with very large radii overflowed `rx²ry²` in float and produced NaN control points; the whole SVG import then failed | Arc conversion in double; the short way round a huge ellipse is a line; anything still beyond float range is a parse error | `An_arc_with_enormous_radii_stays_finite` |
| Low | Lock and ungroup changed unknown-type elements, whose saved form is their original text, so the change was lost on save and on the wire | Skipped, as grouping already did | `Elements_of_unknown_types_are_not_locked_or_ungrouped_since_they_save_as_read` |
| Low | `FlipElements` reported a change (undo step, revision) for a lone text or rotated callout that does not move | Unchanged elements are not replaced; nothing changed returns false | `Flipping_what_cannot_change_is_not_an_undo_step` |
| Low | Path hit test filled only closed sub-paths and ignored the fill rule, unlike the canvas | Winding number summed over all sub-paths, by the path's rule | `A_path_is_hit_where_it_is_filled` |
| Low | Importer: `scale(0)` turned into identity (hidden content shown full size); one over-long path aborted the import with a "total" message; `1.5.5` lost a number | Non-invertible transforms skip the element; an over-long path is left out with a warning; points split as path data does | three `SvgImportTests` |
| Low | IndexedDB: a connection the browser closed stayed cached; wet ink resumed from a finger's pre-pinch point when the other finger lifted | `onclose` forgets the connection; fingers of a multi-touch gesture stay out of wet ink until they lift | code review (no multi-touch in CI) |
| Low | `GroupIds` accepted `Guid.Empty` and repeats, which the reader drops, so a round trip changed the element | The setter refuses them | `A_group_chain_a_document_could_not_read_back_the_same_is_refused` |
| Low | `BinkShortcut.Normalize("f5")` kept the lower case, which never matches the browser's `F5` | Function keys normalized | `Chords_have_one_spelling` |

### Performance measurements of pass 12

Baseline is a git worktree of `d1f929d95e` built in Release beside the current tree; the two ran alternately,
three rounds each, BenchmarkDotNet `--inProcess --job short` (the worktree's path is too long for out-of-process
builds, and the in-process toolchain refuses a default-length run of this benchmark). The machine was shared with
other work, so absolute times are noisier and slower than the README's; the comparison is the meaningful part, and
allocation, which noise does not touch, is exact.

| `EditingBenchmarks.DragFrame` | Baseline | Current, before the fix | Current, after |
| --- | --- | --- | --- |
| 1,000 strokes, mean of 3 rounds | 844 us, 505.7 KB | 985 us, 672.0 KB | 804 us, 521.4 KB |
| 10,000 strokes, mean of 3 rounds | 12.6 ms, 4.94 MB | 16.4 ms, 6.43 MB | 11.7 ms, 5.09 MB |

Attribution, from a probe measuring `GC.GetAllocatedBytesForCurrentThread` per stage at 1,000 strokes: copying the
elements +8 KB, the document's replacement +8 KB (both the 8-byte `GroupIds` reference), history accounting
+154 KB before the fix and 0 after.

## Final verification of this round

Run after the last change, against the same tree:

| Check | Result |
| --- | --- |
| `dotnet build Bit.Bink.slnx` | Succeeds; the one warning is the baseline's (`McpSurfaceTests.cs:58`) |
| `Bit.Bink.Tests`, net10.0 | 729 / 729 (baseline 590). net8.0 and net9.0 compile; their runtimes are not installed on this machine, so they were not run |
| `Bit.Bink.Tests.Mcp` | 73 / 73 |
| `Bit.Bink.Tests.E2E` (Playwright, Chromium, the demo server) | 51 / 51 (baseline 46) |
| Benchmarks | Drag frame equal to the baseline in time, +3% allocation; grouping and SVG import measured (benchmarks README) |

## Round 2 (next steps after the first report)

| # | Focus | Outcome |
| --- | --- | --- |
| 13 | Cross-browser E2E | `BINK_E2E_BROWSER` (chromium, firefox, webkit); the CI E2E workflow runs a job per engine. The unit pipeline already ran .NET 8, 9 and 10. First runs: Firefox 47 passed, 3 inconclusive, 1 failed; WebKit 47 passed, 4 failed. Every failure was investigated with an in-browser probe before deciding: Firefox gives a constructed paste event an empty copy of its data (a limit of simulating a paste, so the test now detects it and is inconclusive), both engines refuse the clipboard permissions (inconclusive, with the reason), and **WebKit refused to store a `Blob` in IndexedDB, so autosave never worked in Safari private windows or any non-persistent WebKit session** - a real defect, fixed by storing bytes; old Blob records still load. |
| 14 | Accessibility automation | `AccessibilityTests` (axe-core 4.14 via `Deque.AxeCore.Playwright`, test-only): every demo page and the canvas with its text editor open against WCAG 2.1 A and AA. The canvas had no violations; the demo shell had three, fixed: the primary colour (#1276C6) failed text contrast (4.34 and 3.97 against 4.5) and is now #0B6AB4 (5.2 and 4.7); the search button's name did not contain its visible text (WCAG 2.5.3), and its two key hints had no space between them, so the visible text read "CtrlK"; the footer link was told apart from its sentence by colour alone. |
| 15 | Asset table | Format version 2: an embedded picture shown by more than one element is written once in `assets`, keyed by a SHA-256 content id, and shared again on load; a document without one is still version 1; `ShareImageData` turns it off. On the wire: a picture crosses once per session, then changes refer to it, so dragging a photo no longer re-sends it every frame. Messages carry `Protocol`; references are used only when everyone heard from is at 1, and snapshots drop the table otherwise. A receiver missing a picture holds the change, asks for a snapshot, and applies it after. 9 tests, one of them for the recovery rate limit; suite 738/738. |

### Decisions of round 2

| Decision | Rationale |
| --- | --- |
| Asset ids are content hashes, verified by the receiver. | The same picture has the same id in every file and on every participant, so files and the wire share one mechanism, and a peer cannot make an id stand for a different picture: receivers hash what they receive themselves. |
| The table holds only pictures shown more than once, and only those files are version 2. | Keeps every other file readable by earlier versions; an earlier version refuses a version 2 file with a message rather than silently dropping its pictures. |
| Wire references are gated on every heard-from participant's protocol, not on a session option. | Mixed versions (a stale tab after a deploy) keep working with no configuration; the saving is automatic once everyone is current. |
| An unresolvable reference triggers the existing snapshot recovery rather than a new "send me this picture" message. | Reuses a tested path; the case is rare (joined after the picture was sent, and the picture has since left the document). |
| That recovery asks at most once every 10 seconds. | Every participant answers a snapshot request with the whole document, so without a limit one peer sending references to pictures that do not exist could make everyone send the document over and over (found in review before release; tested). |
| Lazy loading on WebAssembly needed no code change, unlike Bmage's. | Bmage's editor had to build its own services when the app could not register them. Bink registers nothing, and the canvas injects only `IJSRuntime` and `NavigationManager`, which every host provides. `ComponentServicesTests` keeps it that way. |

### Verification of round 2

| Check | Result |
| --- | --- |
| `dotnet build Bit.Bink.slnx` | Succeeds; the one warning is the baseline's |
| `Bit.Bink.Tests` | 738 / 738 on net10.0; the net8.0 and net9.0 builds also 738 / 738, run on the .NET 10 runtime (`DOTNET_ROLL_FORWARD=Major`), since only that runtime is installed here. CI runs them on .NET 8 and 9 themselves. |
| `Bit.Bink.Tests.Mcp` | 73 / 73 (one test assumed the sample's declared version was always the current one; with version 2 that holds only for files with an asset table, so it now asserts the version the sample declares) |
| `Bit.Bink.Tests.E2E`, Chromium | 63 / 63 (51 plus 12 accessibility) |
| `Bit.Bink.Tests.E2E`, Firefox | 59 passed, 4 inconclusive with the reasons in its README, 0 failed |
| `Bit.Bink.Tests.E2E`, WebKit | 60 passed, 3 inconclusive, 0 failed; autosave and restore pass after the IndexedDB fix |
| Not measured | The cost of the asset-table scan on save (one pass over the elements before writing); no save benchmark was rerun in this round. |

## Round 3: the demo pages in every engine

Every demo page was driven with Playwright in Chromium, Firefox and WebKit, at desktop and phone widths and in both
themes: every control on every page, synthetic pen (pressure, tilt) and touch input, keyboard-only use, opening,
saving and exporting files, autosave across reloads, collaboration with latency and offline, and a 1.5 million point
document. Defects found and fixed:

| Page | Defect | Fix |
| --- | --- | --- |
| Getting started | The live canvas had no status line until the first stroke: nothing re-rendered the page once the canvas was ready. The brush and tool lists left out `BinkCalligraphyBrush` and `BinkPolylineTool`. | `Ready` re-renders, as on every other page; the lists are complete. |
| Every page with a status line | The zoom it showed stayed at the old figure after a zoom from the keyboard, the wheel or a pinch: only the page's own buttons re-rendered it. | `CanvasStatus.ViewportChanged`, bound to the canvas, re-renders the line alone and only when the zoom changed, so panning renders nothing. |
| Whiteboard | The Align list kept showing the last alignment, so picking the same one for the next selection was no change to the browser and aligned nothing. | The list returns to "Choose…" after each use. |
| Whiteboard, and the canvas | With the active layer locked or hidden, new ink went to another layer while the layer list still named the locked one. The canvas swapped a locked or hidden `ActiveLayerId` for the top-most editable layer before any tool saw it, so the tools' own refusal could never happen. | A layer the host names is used as named: while it is locked or hidden the tools, paste, drop, SVG import and `AddImageAsync` refuse, announcing why, and Blazor Server's provisional trail stays off. A layer locked or hidden mid-gesture refuses the stroke, shape or polyline when it ends, where the draw tool used to move it to the active layer. Without `ActiveLayerId` the top-most editable layer is still used. The whiteboard says why drawing does nothing, and opens a board on its top-most editable layer. 11 tests (`LockedLayerTests`, `BinkCanvasLayerTests`). |
| Image annotation | Undoing "Open image" left the status line naming the picture it had just removed. | The line reads the picture in the document; its file name is kept in the element's metadata. |
| Image annotation | A file the browser cannot decode replaced the picture with an invisible 0 by 0 one, with no message. | Refused with a message; the picture stays. |
| Document review | Emptying a comment's box left it empty while the comment kept its text. | The box shows the comment's text again. |
| Image annotation, document review, diagram | On a phone the 900 and 620 unit pages opened at 100%: a third of the picture, the first words of each line, two of the three nodes. | Fitted to the view when the content does not fit it at 100%, never zooming in. |
| Handwriting | The eraser set a pixel-eraser brush that the eraser tool never reads, under a comment describing the opposite of the code. | Removed; the comment says what happens. |
| README | The keyboard table did not say that Enter, Tab, the arrow keys, R and Shift+H/V act through the select tool. | It does now. |

Investigated and found not to be defects:

| Observation | Finding |
| --- | --- |
| In Firefox a stroke released outside the window was lost. | Playwright's Firefox sends no `pointerup` for a release outside the viewport, only one `pointermove` at x 0 with no buttons. Chromium and WebKit deliver the `pointerup` to the capturing element; so does a real Firefox. |
| Chrome failed WebAssembly downloads with `ERR_INSUFFICIENT_RESOURCES`. | The machine was near its commit limit from other processes. Thirty sequential loads in one browser are identical: 229 requests, 37 MB, nothing requested after load. |
| Generating 10,000 strokes of 150 points froze the page for 26 s in Chromium and 43 s in Firefox, against 3 s in WebKit. | The Development host turns on the Mono debugger in Chromium and Firefox only. |
| The footer lists five of the seven use cases and no Tooling column. | `Take(3)` and `Take(5)` are the shared site kit's, the same in all sixteen Utilities demos. |

### Decision of round 3

| Decision | Rationale |
| --- | --- |
| A locked or hidden layer named in `ActiveLayerId` refuses new elements; it is not swapped for another layer. | The README, the parameter's documentation, the tools' checks and their tests already said so; only the canvas's substitution did not. Elements on a layer nobody chose are harder to notice and undo than a refusal that says why, a hidden layer would make them invisible, and a pixel-eraser mask would erase the wrong layer. It is what drawing applications do with a locked layer. With no layer named, the top-most editable one is the right default and is kept. |

### Verification of round 3

| Check | Result |
| --- | --- |
| `dotnet build Bit.Bink.slnx` | Succeeds; the one warning is the baseline's (`McpSurfaceTests.cs:58`) |
| `Bit.Bink.Tests` | 750 / 750 on net10.0, and on the net8.0 and net9.0 builds run on the .NET 10 runtime (`DOTNET_ROLL_FORWARD=Major`) |
| `Bit.Bink.Tests.Mcp` | 73 / 73 |
| `Bit.Bink.Tests.E2E`, Chromium (installed Chrome 156) | 71 / 71; eight of them new in `DemoRegressionTests`, one for each defect above that a browser can show |
| `Bit.Bink.Tests.E2E`, Firefox | 67 passed, 4 inconclusive with the reasons in its README, 0 failed |
| `Bit.Bink.Tests.E2E`, WebKit | 68 passed, 3 inconclusive, 0 failed |

## Round 4: every feature shown and explained

Each demo page was read against the README and the public API of `BinkCanvas`, the tools, the brushes, the document,
the serializer, the exporters and the collaboration session, asking two questions of every feature: does some page let
a visitor try it, and does that page say what it is and how to use it? A search of the demo's sources for each public
member found dozens that no page used - among them the eyedropper, SVG import, JPEG and WebP export, cropping, pressure
curves, prediction, disabling shortcuts, the wheel's zoom mode, zoom limits, every extension point but tools, joining
by snapshot, dirty tracking, the asset table and the reader's limits - and several that a page used without a word.

Two pages were added, because what they cover had no natural home: **Input and accessibility** (touch against
scrolling, the wheel modes, zoom limits, the complete keyboard map, `EnableKeyboardShortcuts` and `DisabledShortcuts`
live, what a screen reader is told, `Announce`, the device's capabilities, WCAG) and **Extending Bink** (a custom
brush with a saved parameter, a custom element type with a converter, both read by an application without them and
handed back intact, and a custom `IBinkRenderContext`). Both are in the catalog, the search palette, the home page,
the README and the E2E page lists.

| Page | Added |
| --- | --- |
| Getting started | `Unavailable` handled on the live canvas; cards for undo, redo and the keyboard (`UndoLabel`, `HistoryLimit`, `HistoryMemoryBudget`), saving, loading and export (`IsDirty`, JPEG), a drawing built and exported with no canvas (live), lazy loading, a blocked browser layer and CSP; the eyedropper |
| Signature | Why the field sets `EnableTouchGestures` and what leaving it off does; `StrokeCompleted` and `HasRealPressure`; what read-only refuses; the stored metadata |
| Handwriting | A second toolbar: smoothing, pressure curve, simulated pressure, `PredictInk`, palm rejection, calligraphy nib angle; cards for each, the pen's eraser end, tilt and twist, and observed capabilities |
| Whiteboard | Line, triangle, polygon (sides), star (points), rounded rectangle, smooth curve and elbow arrow; dashed and filled shapes; bold text; the eyedropper; Insert image (`AddImageAsync`) and Import SVG (`ImportSvg`); layer rename, raise, lower and opacity; Cut, Forward and Backward; exporting the selection only; the next undo step and unsaved changes in the status line; a card for every part (tools, shapes, erasers, transforms and the view, the selection and queries in code, grouping, layers, clipboard and import, saving and autosave) |
| Diagram | Graph-paper grid and snapping, with the demo's own node tool snapping through `BinkSnapper` and drawing its guides; wheel pan and zoom; cards for both and for the locked label layer |
| Image annotation | A pixel-mask eraser, Hide picture, a non-destructive crop, JPEG and WebP export (`SaveImageAsync`, `NotSupportedException` on Safari); cards for each, for picture sources and their safety (`TryCreate`), and for export regions and options |
| Document review | A styled callout; a card building highlights and callouts directly; the panel's actions explained |
| Collaboration | Reload a participant, which leaves and joins again by `RequestSnapshotAsync`; cards for joining and `SendSnapshotAsync`, the stacking-order limit, presence options and threading for a real transport |
| Serialization | Unsaved changes (`IsDirty`, `MarkSaved`); Add a picture twice, which makes a version 2 file with an asset table; Strict limits, loading through `BinkDeserializerOptions`; format version and the pure .NET PNG's completeness (`ExportWithDetails`) in the figures; cards for the format, untrusted input and unsaved changes |
| Performance | Moving every stroke and undoing it, and SVG export to a stream beside the string, with what each allocates; a card on why it stays fast |
| Home | The two new pages; the whiteboard and image annotation cards brought up to date |

Defects found and fixed on the way:

| Page | Defect | Fix |
| --- | --- | --- |
| Whiteboard | The eraser set a pixel-eraser brush that the eraser tool never reads, the same defect round 3 removed from the handwriting page. | Removed; the comment says the eraser makes its own brush. |
| Image annotation | Placing the picture unlocked its layer, added the picture and locked the layer again, and the card showed that as the way to do it, but a lock keeps tools away, not code: the document API adds to a locked layer as it is. | The picture is added to the locked layer directly, and the card says why that works. |
| Image annotation, home | "Drop in a screenshot", on a canvas that does not accept dropped files. | "Open a screenshot". |
| Handwriting | "The badges above it", for badges on the line below the canvas. | Reworded. |

The axe audit caught two problems in the new material before it shipped, both fixed: links placed inside the pages'
explanations were told apart from their sentence by colour alone (WCAG 1.4.1), so a link in a card's or a note's text
is now underlined (`demo.css`), and the new page's "What a screen reader is told" panel scrolled without being
focusable.

### Verification of round 4

| Check | Result |
| --- | --- |
| `dotnet build Bit.Bink.slnx` | Succeeds, no warnings |
| `Bit.Bink.Tests.E2E`, Chromium | 82 / 82: the 71 of round 3, both new pages in the console-error and WCAG 2.1 AA page lists (4), and 7 new `DemoFeatureTests`, one for each new demonstration a browser can check |
| `Bit.Bink.Tests.Mcp` | 73 / 73; the new pages and `Demo/Client/Extending/` are in the source index |
| `Bit.Bink.Tests` | Not rerun: nothing in the library changed in this round |
| Firefox, WebKit | Not run in this round |

## Round 5: the improvement plan of November 2026

A broad improvement plan (drawing engine, editing, tools, document model, rendering, persistence, collaboration,
accessibility, testing, ecosystem integrations) was checked item by item against the code on `develop` before
anything was built. Baseline: `Bit.Bink.Tests` 782 / 782 on net10.0 and net11.0, `Bit.Bink.Tests.Mcp` 75 / 75.

### Audit against the plan

Most of the plan describes what rounds 1 to 4 already built and verified (the matrix above). What it asks for that
was genuinely missing, and what this round decided about each:

| Plan item | Finding | Decision |
| --- | --- | --- |
| Shape recognition (sketch to clean geometry) | Missing (matrix D, deferred P3) | **Done**: `BinkShapeRecognizer`, `BinkDrawTool.ShapeRecognition` |
| Configurable smoothing and stabilization | Only exponential streamline smoothing | **Done**: pulled-string stabilizer, `BinkDrawTool.Stabilization` |
| PDF export | Missing (matrix H, deferred P3) | **Done**: `BinkPdfExporter`, vector, multi-page |
| Cross-backend rendering consistency | Rasterizer drew round joins as whole circles: butt-capped strokes (the highlighter) exported to PNG with round ends | **Fixed** |
| Robust import validation | The PNG decoder let `ZLibException` escape on corrupt data (found by the existing fuzz test once its input changed) | **Fixed** |
| Tilt, azimuth and twist dynamics | Captured and saved; no built-in brush uses them | Backlog P2: calligraphy nib following the pen's azimuth or twist |
| Bézier node editing | `BinkPathElement` exists; no tool edits its nodes | Backlog P2 |
| Textured, charcoal, chalk, watercolour, oil, airbrush brushes | One filled outline per stroke cannot vary texture or opacity along it | Deferred, as in matrix C; the brush API is open for applications that rasterize their own |
| Brush presets, categories, favourites | A brush is an immutable record, so a preset is a saved instance | N/A in the engine: application UI |
| Boolean shape operations, text on paths, rich text | Large; robust polygon clipping alone is a project | Deferred P3 |
| Spatial index (R-tree) | README measures culling 10k boxes in tens of µs | Deferred until a benchmark shows culling or hit testing dominating, as the plan itself asks |
| Rulers, protractor, measurement overlays | Application UI over the public API (`DrawOverlay`, viewport) | Deferred |
| Ecosystem adapters (Bcrdt, Bmage, Bdnd, Blex, BlazeDb, Butil, Bortable, Bmotion, BevTools, Belemetry, Borm) | Bink has no dependencies beyond `Microsoft.AspNetCore.Components.Web` and already ships its own IndexedDB store, drop and clipboard handling | **Not in the core package.** Adapters would add package references and the risk of circular ones for little gain; each is a sample or a separate package when an application needs it. Bcrdt is evaluated against the operation protocol only after fractional z-order keys, the protocol's one known gap |
| Application experiences (Notes, Whiteboard, Review, Studio) | | Outside the core package, as the plan says |

### Increments

Each is one reviewable change with its own tests, documentation and demo.

| # | Change | Tests | Docs and demo |
| --- | --- | --- | --- |
| 16 | **Shape recognition.** Resampled to 96 evenly spaced points so speed and density do not matter. Open strokes: a line (chord error, and a length ratio that refuses strokes doubling back), or a one-stroke arrow (straight shaft to the first point near the farthest, barbs turning back 10 to 75 degrees). Closed strokes (end near the start, or crossing past it): an ellipse by least squares in its principal axes, and a polygon by simplification from the point farthest from the centre with weak corners removed, the better fit winning; four right angles make a rectangle, corners at the middles of the sides a diamond. Angle and proportion snapping. In the draw tool: `Off`, `OnHold` (the pen rests half a second before lifting) or `Always`; the stroke is committed and then replaced by the shape in a second step, so one undo returns the ink. | 27, including 200 seeded drawings across five shapes and two sizes | README "Recognizing shapes"; whiteboard "Shapes" choice (hold to snap by default) and card |
| 17 | **Raster round joins.** A wedge on the outside of the turn, as canvas, SVG and PDF draw; a turn straight back gets the half circle ahead of it. | 2 (the first fails on the old code) | |
| 18 | **PNG decoder.** Inflate errors of either exception type become `FormatException`. | 1 (fails on the old code) | |
| 19 | **PDF export.** Each paint is written self-contained (`q` clips `cm` paint `Q`) from a transform and clips the context tracks itself, so the content drawn so far can become a form at any moment; that is how pixel erasers work: PDF has no destination-out, so the content before a run of erasers becomes a form drawn through a luminosity soft mask the erasers paint black on white. Layers with opacity, a blend mode or an eraser are isolated transparency groups, as on the canvas. JPEG passes through (`DCTDecode`, frame header read for size and components; CMYK refused); PNG is decoded, stored with Flate and an `SMask` for alpha, each picture once. Text in the standard 14 fonts with WinAnsi encoding and their metrics for alignment; other characters skip the element and are counted. UTF-16 properties, `/Lang`, deterministic bytes. `BinkCanvas.ExportPdf` and `SavePdfAsync`. | 22 PDF tests with a reader that checks every cross-reference offset, plus a canvas test | README "PDF"; whiteboard "Export PDF" |
| 20 | **Stabilizer.** `BinkStrokeBuilder.StabilizerRadius` pulls the ink along a string before smoothing; the tool measures it in CSS pixels, finishes the stroke to the lift point and draws no prediction while it is on. Not part of the brush, so the format is unchanged. | 7 | README "Stabilizing a line"; handwriting "Stabilizer" slider |

The PDF output was also checked outside the test suite: rendered by pdf.js in Chromium (the browser the E2E suite
uses) beside the engine's own PNG of the same drawing, they match, erasers included; pdf.js parses it with no
warnings and extracts the text. pdf.js under Node with a native canvas does not apply soft masks, which is a
limitation of that canvas, not of the file.

### Review of round 5

An independent reviewer read the whole diff against the code, ran checks of its own, and confirmed two defects;
four more it rated plausible were each checked and three were real. All fixed, each with a test that fails without
the fix:

| Severity | Defect | Fix | Test |
| --- | --- | --- | --- |
| Medium | A stabilized stroke that never pulled the string taut (the bar of a t, a dash, an accent) ended as a dot: the finishing segment was only added to strokes with two points or more | Added from the first point | `A_stroke_shorter_than_the_string_is_still_a_line_to_where_it_lifted` |
| Medium | PDF: translucent erasers painted opaque grey into the mask, so where two crossed the second replaced the first; the canvas and the rasterizer compound them (two half erasers leave a quarter) | Erasers multiply into the mask | `Translucent_erasers_compound_where_they_overlap` |
| Medium | PDF: a phone photo's EXIF orientation is applied by browsers, so by the canvas, but ignored by PDF's DCT filter, so a turned photo came out turned differently | The orientation is read from the Exif segment (both byte orders, bounds checked) and the picture placed turned, with its displayed size; crops are in the picture as shown | `A_photo_is_placed_turned_...` (both byte orders), `A_crop_of_a_turned_photo_...` |
| Low | Recognition reported the fit's error, measured before snapping, so a snapped shape could stray further than the tolerance the result promises | Measured as drawn; snapping that would pass the tolerance is not done | `The_reported_error_is_that_of_the_shape_as_drawn_...` |
| Low | PDF: a region too large overflowed the page size to zero; a lone carriage return broke a line, unlike the canvas and SVG | Pages over 14,400 points (the viewers' limit) are scaled down, a region that is not finite is refused; carriage returns draw nothing | `A_page_too_large_...`, `Lines_break_at_line_feeds_only` |

The reviewer also confirmed as correct: the round-join wedge in both turn directions, the PDF structure (20-byte
cross-reference entries, offsets, balanced `q`/`Q` and `BT`/`ET`, the mask group's colour space and coordinates, layer
isolation keeping the page background from being erased), WinAnsi ranges, JPEG frame parsing bounds, and the draw
tool's history: the shape replacement is its own undo step and keeps the stroke's stacking index.

A second review read only those fixes and found two defects they introduced, both fixed with tests:

| Severity | Defect | Fix | Test |
| --- | --- | --- | --- |
| Medium | A page scaled down to fit was mapped with its scale rounded to three decimals: a page 70,000 units wide drew 20 points past its edge, one of 30 million drew nothing | The page transform is written at full precision | `A_page_too_large_for_viewers_...` |
| Medium | Now that a diamond's or rectangle's error was honest, one that fitted badly was still returned and hid the better readings: a box turned 45 degrees came out as an ellipse, a quadrilateral with one slanted side as a rectangle | A regular reading is used only within the tolerance and nearly as close as the raw corners; otherwise the next is tried, down to the polygon | `A_regular_reading_that_fits_badly_gives_way_to_the_next` |

It confirmed the stabilizer fix, all eight EXIF orientation entries against the specification, every bound in the
Exif reader, and the multiply blend inside the luminosity mask. Left as they are, as minor: a snap is accepted or
refused for angle and proportion together rather than each on its own, and an arrow's snap is judged against the
stroke's diagonal rather than its shaft. A third review round was not run.

The Chromium E2E suite's accessibility, display, feature, drawing and editing tests (62) were run against the demo with
these changes. One failed, on a page this round did not touch: the Getting Started table's links were told apart
from their sentence by colour alone (WCAG 1.4.1), as round 4 had fixed for cards and notes; links in table cells are
now underlined too.

### Observed, not changed

`dotnet build` with SDK 11.0.100 intermittently fails in `ApplyCompressionNegotiation` with "An item with the same key
has already been added" for `Bit.Bink.lib.module.js`, on the first build after a source change; running it again
succeeds. It is in the static web assets pipeline, not in anything this round touched.

## Next priorities

1. Fractional z-order keys, which would let concurrent reorders reconcile (a format change).
2. Bézier node editing for `BinkPathElement` and polygons.
3. A calligraphy nib that follows the pen's azimuth or twist, the first built-in use of tilt.
4. Rebinding shortcuts to other keys inside the canvas, beyond disabling them.
5. A per-picture request message, should snapshot recovery for a missing picture prove too heavy in large sessions.
