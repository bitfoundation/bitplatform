# VirtualKeyboard: manual accessibility verification

| | |
|---|---|
| Component | `BitVirtualKeyboard` (`Bit.BlazorUI.Extras`) |
| Pattern | A group of native buttons (toggle buttons with `aria-pressed` for the modifiers); no WAI-ARIA Authoring Practices pattern exists for an on-screen keyboard |

## Run: 2026-11-25, AI implementing agent (no screen reader available), working tree on develop `6e83ea7d7e`

| | |
|---|---|
| Pages used | The E2E page (`/e2e/virtual-keyboard`), sections value, target, follow, rtl, german, compact |
| Configurations | A: value mode (roving grid); B: typing into an input (`Target`); C: `FollowFocus` docked; D: `Language="fa"` |

### Pairs

| Screen reader (version) | Browser (version) | Platform (version) | Render mode | Configurations | Result |
|---|---|---|---|---|---|
| NVDA | Firefox | Windows | Server / WebAssembly | | Not run |
| NVDA | Chrome | Windows | | | Not run |
| JAWS | Chrome | Windows | | | Not run |
| VoiceOver | Safari | macOS | | | Not run |
| VoiceOver | Safari | iOS | | | Not run |
| TalkBack | Chrome | Android | | | Not run |
| Narrator | Edge | Windows | | | Not run |

### Checks

| # | Check | Result | Pairs and configurations | Notes, issue |
|---|---|---|---|---|
| 1 | **Name, role, state** announced on focus for every part | Not run | | The markup is checked automatically: axe scans of every E2E section pass in Chromium, Firefox and WebKit, and bUnit tests pin the names (a visible label inside every name), `aria-pressed` and the groups. |
| 2 | **Reading** in browse/virtual mode, and by swiping and touch exploration on touch | Not run | | |
| 3 | **Operation** with the pattern's keys in focus/forms mode; with gestures on touch | Not run | | The keyboard path (arrows, Enter/Space, Shift+F10 for alternates) is covered by the script; not checked with a reader. |
| 4 | **Changes** announced once without moving the focus | Not run | | The polite live region announces layer, Shift and Caps Lock changes. |
| 5 | **Focus** moves into a popup or dialog, returns on close | Not run | | The long-press popup takes the focus only when opened from the keyboard in value mode. |
| 6 | **Keyboard only**, no screen reader | Not run | | |
| 7 | **Forced colors** | Not run | | The stylesheet has a forced-colors block. |
| 8 | **Zoom and reflow** | Not run | | |
| 9 | **Reduced motion** | Not run | | Durations go through the motion tokens. |
| 10 | **Right-to-left** | Not run | | The grid stays left to right by design (PLAN.md D6). |

### Findings

- None recorded; no manual run was made.

### Not verified

- Every pair and check above: the run was made by an AI agent with no screen reader or touch device. A maintainer's
  Tier 1 run (NVDA with Firefox and Chrome, JAWS, macOS VoiceOver) and, since the component is touch-aware (long press),
  iOS VoiceOver, TalkBack and Narrator are still owed.