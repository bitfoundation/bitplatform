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

## Next priorities

1. Fractional z-order keys, which would let concurrent reorders reconcile (a format change).
2. Rebinding shortcuts to other keys inside the canvas, beyond disabling them.
3. A per-picture request message, should snapshot recovery for a missing picture prove too heavy in large sessions.
