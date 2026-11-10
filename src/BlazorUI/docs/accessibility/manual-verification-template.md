# <Component>: manual accessibility verification

<!-- Copy this file to records/<Component>.md. A later run adds a section of its own above the earlier ones, so the
     newest result is read first and the history stays in one file. See README.md for when a run is required, the
     matrix and what each check means. Mark every check Pass, Fail, N/A or Not run; a Fail links its issue. -->

| | |
|---|---|
| Component | `Bit<Component>` (`Bit.BlazorUI` / `Bit.BlazorUI.Extras` / `Bit.BlazorUI.AI`) |
| Pattern | The WAI-ARIA Authoring Practices pattern it implements (link), or "native" for a native control |

## Run: <yyyy-mm-dd>, <tester>, `<package version or commit>`

| | |
|---|---|
| Pages used | The demo page and its example ids, or the E2E page (`/e2e/<component>`) |
| Configurations | The parameter sets this run checked, each with a short name the tables below use (e.g. A: `CellNavigation` on, B: `CellNavigation` off, C: `Dir="BitDir.Rtl"`) |

### Pairs

| Screen reader (version) | Browser (version) | Platform (version) | Render mode | Configurations | Result |
|---|---|---|---|---|---|
| NVDA | Firefox | Windows | Server / WebAssembly / Hybrid | A, B, C | Pass / Fail / Partial / Not run |
| NVDA | Chrome | Windows | | | |
| JAWS | Chrome | Windows | | | |
| VoiceOver | Safari | macOS | | | |
| VoiceOver | Safari | iOS | | | |
| TalkBack | Chrome | Android | | | |
| Narrator | Edge | Windows | | | |

### Checks

<!-- A check names every pair and configuration it ran on together (e.g. "NVDA/Firefox: A, B; VoiceOver/iOS: A").
     A combination it does not name was not checked, and is listed under Not verified. -->

| # | Check | Result | Pairs and configurations | Notes, issue |
|---|---|---|---|---|
| 1 | **Name, role, state** announced on focus for every part | | | |
| 2 | **Reading** in browse/virtual mode, and by swiping and touch exploration on touch: order, headers, nothing hidden from AT read out (text hidden only visually, for screen reader users, is read) | | | |
| 3 | **Operation** with the pattern's keys in focus/forms mode; with gestures on touch | | | |
| 4 | **Changes** announced once without moving the focus (sort, filter, loading, error, count) | | | |
| 5 | **Focus** moves into a popup or dialog (or stays on a combobox's input while `aria-activedescendant` names the active option), returns on close, is handed on when its element is removed | | | |
| 6 | **Keyboard only**, no screen reader: every action reachable, focus visible, never lost or trapped | | | |
| 7 | **Forced colors**: a light and a dark Windows contrast theme | | | |
| 8 | **Zoom and reflow**: 200% zoom; a 320 CSS-pixel-wide viewport | | | |
| 9 | **Reduced motion**: the OS setting on | | | |
| 10 | **Right-to-left**: checks 1, 3 and 5 with `Dir="BitDir.Rtl"` | | | |

### Findings

<!-- One entry per Fail or accepted limitation: what was heard or done, what was expected, the pair, and the issue.
     A screen reader's own bug is named as one, with its version and its own issue where there is one. -->

- None.

### Not verified

<!-- Every pair, configuration, pair-and-configuration combination or check the run did not reach, and why (no
     device, out of scope for this change). -->

- None.
