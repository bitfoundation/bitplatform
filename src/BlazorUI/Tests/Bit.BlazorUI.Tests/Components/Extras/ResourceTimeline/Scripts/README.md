# BitResourceTimeline engine tests

Tests for the BitResourceTimeline rendering engine - the TypeScript in
`src/BlazorUI/Bit.BlazorUI.Extras/Components/ResourceTimeline/` - run against
the modules the Extras build compiles it into
(`Bit.BlazorUI.Extras/wwwroot/scripts/modules/BitResourceTimeline*.js`).

## Running them

`BitResourceTimelineScriptTests` (the MSTest file beside this folder) runs the
whole suite as part of `Bit.BlazorUI.Tests`, and fails with node's report when
any of it fails. It is inconclusive only where no `node` can be started.

To run them directly, build `Bit.BlazorUI.Extras` first (that is what writes the
modules - they are not checked in), then, from the repository root:

```bash
node --test "src/BlazorUI/Tests/Bit.BlazorUI.Tests/Components/Extras/ResourceTimeline/Scripts/"*.test.mjs
```

The fixture finds the modules relative to its own location; set the
`BIT_RTM_MODULES` environment variable to a folder to load them from somewhere
else (the MSTest wrapper passes the source checkout's folder this way).

Requires a Node.js that detects ES module syntax in a `.js` file on its own
(22.7+, or 20.19+). There are no npm dependencies and no `package.json` - these
use node's built-in test runner and `node:assert`, so nothing needs installing.

## How the modules are loaded

The compiled files are lazy ES modules written for a browser: each one adds its
class to the global `BitBlazorUI` namespace it finds on `window` instead of
exporting it, and `handleKeyDown` asks the core library's
`BitBlazorUI.Utils.isComposingKey` about every key. `helpers/engine-fixture.mjs`
therefore sets `window` to `globalThis`, stands in for `isComposingKey` (with
the core library's logic), imports `BitResourceTimelineEngine.js` (which imports
the zoned-time and tooltip modules), and exports the classes read off the
namespace: `ResourceTimelineEngine`, `ResourceTimelineZonedTime` and
`ResourceTimelineTooltip`, with `ZonedTime`, `Tooltip` and `utcHourBoundaries`
(the zoned-time class's static method) under the names the tests use.

It then builds engines on the prototype with only the state the DOM-independent
methods read, so these run under plain Node with no browser or DOM shim. The
bare engine has no `config.texts`, so the built-in English texts are what the
assertions see, and its wrapper has no `setAttribute`, so the `data-bit-esc`
claim is a no-op unless a test gives it one.

## What is covered

| File | Area |
| --- | --- |
| `time-axis.test.mjs` | Zoned hour boundaries, DST transitions, tick density, `addDays` calendar steps, day walks through a DST change that skips midnight, wall-clock and continuous snap, `Hour12`, `FirstDayOfWeek` |
| `allocation-index.test.mjs` | Per-resource index, stacking lanes (time overlap, label clearance, and label/delay-bar collisions across zoom levels), incremental re-index on edit, invalid-row sanitize, `MaxStackLanes` overflow, windowed merge by id (the payload is what stays loaded), a window of another session dropped, the relayout after one, the needed range within the fetched one |
| `engine-state.test.mjs` | Resource hierarchy, coordinate mapping, row heights, options, selection, resource-axis resize clamp, upper-lane marquee, Shift-range select, selection-changed notify, a data load reporting the selection it drops, the rows reported only when they change, the windowed relayout checking the loaded window, `whenRendered` without an animation frame |
| `scene.test.mjs` | Scene building, and the stale state pooled/refilled bar nodes can leak between frames |
| `bar-sections.test.mjs` | Bar sections: time/percent bounds and their precedence, clamping to the bar, text and icon placement and fit |
| `view-scroll.test.mjs` | Where the viewport lands on the first load and across reloads, the `panByDays` day/week steps (24-hour, `panToDayStart`, and per-call override), `zoomToDays` fitting N days into the viewport, the "now" indicator's refresh timer, `OnViewChanged`, and `scrollToAllocation` |
| `tooltip.test.mjs` | Show delay, subject switching and viewport-edge flipping, the `bit-rtm-ttp` class that styles the element and `setStyle`, against a minimal DOM stub; what a hover resolves to and the text it asks for, including `+N` overflow markers |
| `hit-test.test.mjs` | `_barAt` click tolerance, resize handle vs move, locked bars, stacked-lane Y, overflow hits, `_pointerHit` regions, double-click pairing, click-select notify |
| `renderer-contract.test.mjs` | `buildScene()` bar ids, non-overlap within a row, weekend/off-hour bands, surface stripes (days, shifts, rows, checker, bands), `Hour12` tick labels |
| `edit-commit.test.mjs` | Move/resize commit, overlap refuse, delete, multi-move accept/reject (re-filed and laid out once per move and per revert), whole-millisecond previews of a continuous drag, a held keyboard edit waiting for the host |
| `texts-theme.test.mjs` | The `texts` option (built-in English fallback, key-by-key override, unknown-key warning, composite formats, announcements), the `BitResourceTimeline` console prefix, and `setTheme` layering: theme colors under explicit option colors, the theme's font family only for fonts no option set |
| `disabled-escape.test.mjs` | `disabled`: no press, click, edit, marquee, context menu or keyboard selection, paging still works, switching it on cancels a marquee; the `data-bit-esc="claim"` the wrapper carries exactly while a selection exists; keys pressed during an IME composition |

## Conventions

- **Test against an oracle, not against the implementation.** The hour-boundary
  tests compare with a deliberately naive minute-by-minute scan. When both the
  code and its test encode the same clever idea, they agree on the same bug.
- **Prefer absolute invariants.** `indexViolations()` checks properties that
  must hold on their own terms (rows sorted, scan bounds cover their contents,
  nothing lost or duplicated) rather than only diffing against a full rebuild -
  a bug present in both paths passes a differential check.
- **Anything needing real layout belongs in a browser.** Hit-testing is covered
  here against engine geometry (`_barAt`, marquee Y, overflow labels). Renderer
  *paint* (canvas pixels, scrollbar clamping) is not; `renderer-contract.test.mjs`
  asserts the display list instead. The tooltip's look lives in the stylesheet's
  `bit-rtm-ttp` rule now, so its tests assert the class rather than inline
  styles.
- **Keep the fixture in step with the constructor.** When the engine gains a
  field that a DOM-independent method reads, `makeBareEngine` initialises it to
  the constructor's default.
