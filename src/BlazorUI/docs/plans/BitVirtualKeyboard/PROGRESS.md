# BitVirtualKeyboard implementation progress

The running log the plan asks for (PLAN.md section 0.7): what each phase delivered, its gate, and every deviation from the
plan with its reason. The hand-off (PLAN.md section 13) is built from it.

## Phase 0 - Reconnaissance (2026-11-25)

- develop at `6e83ea7d7e`; `bit-vkb` and `VirtualKeyboard` unused (only an unrelated E2E test name).
- `spacing(n)` = `calc(var(--bit-spa-scaling-factor) * var(--bit-layout-density-scale) * n)`, 0.5rem at density 1, so
  `spacing(3)` = 24 px, `spacing(5.5)` = 44 px, `spacing(8)` = 64 px.
- `Utils.escapeClaimer(e)` exists (`Bit.BlazorUI/Scripts/Utils.Escape.ts` 55); the claim walk starts at `e.target`.
- Templates read: BitImageViewer (component, script, Params, Strings, ClassStyles, scss), ResourceTimeline layout.

## Phase 1 - Model, shorthand, JSON, compiler, geometry, first layouts (2026-11-25)

Delivered: the model (`Models/`), `BitVirtualKeyboardShorthand`, the row JSON converter and source-generated context,
the built-in keys, the compiler with the compiled shapes, the language data record, the Full/Compact builders, the Latin
symbol layers and the `en` layouts. Tests: shorthand, compiler, JSON, catalog (77, all green).

Deviations:
- **Escapes in literals.** The parser applies the escapes to a whole token that starts with a backslash
  (`\u{65}\u{301}` types e + combining acute), not only to its first escape. The plan's examples all hold.
- **Shorthand errors are `FormatException`** (a `JsonException` when read from JSON); layout errors found by the
  compiler are `ArgumentException`, as planned.
- **Language data, not `CreateCompact(full)`.** The Full and Compact forms are both built from one internal record of
  scan-ordered rows (`BitVirtualKeyboardLanguage`), which the Compact form needs anyway to find each letter's shift
  counterpart. The Compact form's shift layer is explicit (from the Full shift rows), not derived.
- **Built-in labels rank below `Displays`.** A built-in key's label is kept apart from the merge (as plan 5.4 step 9
  orders), so a layout's `Displays["sym"]` relabels `{sym}`.
- **Derived layers reuse the base rows' alignment.**
- `BitVirtualKeyboardLayouts.ForCulture(culture, form)` added (used by the component's language resolution, plan 6.10).
## Phase 2 - Rendering, styles and the first registrations (2026-11-25)

Delivered: the component (`BitVirtualKeyboard.razor`, `.razor.cs`, `.Icons.cs`), ClassStyles, Strings, Params, the
stylesheet (imported after Virtualize), the nav item, the catalog glyph and summary, the demo page skeleton (Basic
example and every table), the MCP DataRow, the disabled-activation list entry. bUnit tests: rendering and Params.
Gate: the whole unit suite green (30,492 tests) after two fixes the contract tests named: the demo's `Direction` row
links `#component-dir`, and `.bit-vkb-lbl` is listed in `BitFieldLabelFontWeightContractTests.OtherLabels`.

Deviations:
- **Auto form starts Compact** until the script measures the keyboard (a choice the plan left open).
- **Popup and preview visibility** is the script attribute `data-bit-vkb-show`, not `hidden`, so the empty containers
  are hidden by the stylesheet before the script runs.
- **Docked open state before the script**: the root also renders `bit-vkb-opn` from `Open`, so a prerendered page
  shows an open docked keyboard; the script's `data-bit-vkb-open` on the panel drives it afterwards.
- **The icon-flip class is `bit-vkb-mir`**, not `bit-vkb-rtl-ico` (no `rtl` in a class that is not about the page
  direction, which `BitComponentDirFallbackTests` reads).
- **Value-mode Escape**: the root always claims in value mode (`EscapeClaim` is `"claim"` there), the script acting on
  it (closing a popup, a docked keyboard, or blurring).
- A long label (more than three characters) gets the smaller-type class `bit-vkb-lbs`.
## Phase 3 - Scripts and interop (2026-11-25)

Delivered: `BitVirtualKeyboardEngine.ts` (on-demand, pure), `BitVirtualKeyboard.ts` (the DOM side), the payload records
and setup call, the `[JSInvokable]` callbacks, setup/update/attach in `OnAfterRenderAsync`, `DisposeAfterAsync` teardown,
`escapeClaimer` declared in `Scripts/BitBlazorUI.d.ts`, the Node suite (`Scripts/engine.test.mjs` + fixture + README +
`BitVirtualKeyboardScriptTests`) and the bUnit interop tests. Tests: 107 of the component (Node suite 11/11 inside);
`BitAssetTrimmingContractTests`, `BitLazyScriptsContractTests`, `BitEscapeClaimContractTests`,
`BitJsInvokableDisposalContractTests`, `SourceFilesBomTests` green.

Deviations:
- **The script was written in one pass**, including what phases 5 and 6 list (follow focus, docked open/close, Escape on
  the target, Enter behaviours, change tracking, long-press popup, multitap, dead keys, repeat, preview, physical-key
  highlight, sound, haptics). Those phases verify and complete it instead of writing it.
- **`OnKeyPress` and `OnTarget` take plain arguments** (a `[JSInvokable]` cannot take an internal record); `OnKeyPress`
  returns the public `BitVirtualKeyboardKeyPressArgs`.
- **The long-press keyboard path is the context menu** (Shift+F10, the menu key) rather than ArrowUp, which moves the
  focus through the grid in value mode.
- **Keys act on release** (so sliding off takes a press back), except repeating keys, which act on press and repeat.
- **The payload version** bumps on any change of a value-compared record of what the script is told (the compiled
  layout by reference, which the compiler's content cache keeps stable), instead of comparing serialized JSON.
## Phase 4 - Catalog (2026-11-25)

Delivered: the 13 purpose pads (`BitVirtualKeyboardLayouts.Purpose.cs`, `ForPurpose`), the P0 languages (`en`, `en-GB`,
`de`, `fr-t-k0-windows`, `es`, `tr` in `.Latin.cs`; `ru`, `fa`, `ar`, `he` in `.Rtl.cs`) in Full and Compact forms,
the dead-key tables from Unicode composition (plus the Turkish dotless/dotted i), the Latin, Persian and Arabic symbol
sets, `Get`/`Ids` over pads too, and `ForCulture` (fr maps to the legacy AZERTY). The component picks the pad of a
non-text purpose. Tests: every pad row fills its grid (Inline and Docked), every Full row is 15 units, every symbol row
10 units, data spot checks per language. 118 component tests green on net11.0 and net10.0 (`-p:BuildAllTfms=true`).

Deviations:
- **Full layouts carry the AltGr layer of every language whose data lists AltGr characters** (en-GB, de, fr, es, tr,
  ru, fa, he), Arabic none (its one recent AltGr entry is ignored, as layout-data.md says).
- **Arabic's Full form types European digits** (as KBDA1); its Compact symbol layers type the Arabic-Indic ones.
- **Persian format characters** (LRO, RLO, PDF, LRE, RLE) on the AltGr layer get text labels through `Displays`.
- `ForCulture` resolves French to `fr-t-k0-windows` (the only French layout in v1).
## Phases 5-7 - Modes, targets, gestures, secure (2026-11-25)

The script written in phase 3 already carries these; this round verified and fixed them in real browsers:
- **Fixed:** an explicit `Target` committed no `change` on blur (only follow focus did); every attached input now
  commits as the focus leaves it.
- **Fixed:** presses were held back during every value round trip (the gate compared the options version); the gate is
  now a key-table version (`keysVersion`, rendered as `data-bit-vkb-v`) that changes only with the layout.
- **Fixed:** WebKit on Windows reports an unsupported `type=date` as `text`; inputs now qualify by their `type` attribute.
- **Fixed:** an inline scrambled PIN pad in value mode was never shuffled (it never opens or attaches); it is shuffled
  as it first renders too.
- **Documented:** an `@ref` taken in the same render reaches `Target` on the page's next render (the demo and the E2E
  page re-render once after the first render).

## Phase 8 - Demo page (2026-11-25)

17 sections: Basic, Target, FollowFocus, Purpose, Languages, Form, Shorthand layout, Object layout, Customizing a
built-in, JSON layout, Events, Secure & Scramble, KeyTemplate, Disabled, Size, Style & Class, RTL. The whole unit suite
(30,515, demo contract tests included) and the MCP suite (267) are green.

Deviation: no separate "Enter behavior" section (it needs an input in a form to show; the parameter table and the E2E
tests cover it).

## Phase 9 - E2E and final verification (2026-11-25)

- E2E (`/e2e/virtual-keyboard`, 12 tests): Chromium 12/12, Firefox 12/12, WebKit 11/12 in server mode; WebAssembly mode
  Chromium 12/12, Firefox 12/12. The WebKit failure is `ALongPress_OffersTheAlternates`: a held mouse press never marks
  the key pressed, so the long-press timer never runs (the context-menu path to the same popup passes in WebKit). Other
  components' WebKit pointer-gesture tests fail the same way on develop on this machine (BitSwipeTrap), so it is likely
  Playwright WebKit on Windows; it still needs a check in Safari on macOS or iOS.
- Release build of the Extras with `-p:RunTrimAnalyzers=true`, both target frameworks: green (the IL warnings are all in
  the existing DataGrid).
- One MCP run failed to start the demo server on Razor errors in files nothing touched; `dotnet build-server shutdown`
  cleared it (a stale compiler server), and the suite passed.
- Accessibility record: `docs/accessibility/records/BitVirtualKeyboard.md`, every manual check Not run (no screen
  reader or touch device here).

## Follow-up round (2026-11-25)

- **WebKit long press was a test bug, not a WebKit limitation.** A raw `Mouse.Down` at a key's box does not scroll the
  key into view as a click does; in WebKit the key was off screen, so it got no events at all (a probe logged none). The
  test scrolls the key in first and passes in every engine.
- **Enter is named by the input's enterkeyhint** (Next, Done, Go, Previous, Search, Send), as plan 6.7 says; the names
  re-render when the keyboard attaches to an input.
- **`data-bit-vkb-sync`**: the script writes the key-table version it holds on the root, so a test (or a page) can wait
  until it matches the rendered `data-bit-vkb-v` before pressing; the E2E helper does, which removed the one retry seen.
- **New E2E tests**: key repeat while Backspace is held and its stop on release; Enter submitting the form, moving to the
  next field (enterkeyhint next) and typing a new line in a textarea; a page cancelling the keydown keeping Backspace and
  Enter from acting; Caps Lock upper-casing letters but not digits; AltGr typing the third character once; a password
  input showing no pressed paint and no preview; the value-mode grid moved through with the arrows and activated with
  Enter; the reserve spacer of an open docked keyboard holding the panel's height and dropping to 0 when it closes.
- **Results**: E2E 19 tests × Chromium, Firefox, WebKit × server and WebAssembly modes, all green with no retry; the
  whole unit suite 30,519 green; the trimming suite (`RUN_BROWSER_TESTS=1 RUN_PACKAGE_TESTS=1`) 68/68; the MCP suite
  267/267.

## Not done

- The manual screen-reader and touch-device checks (iOS Safari focus retention, VoiceOver, TalkBack, NVDA, JAWS); the
  record at `docs/accessibility/records/BitVirtualKeyboard.md` marks them Not run.
- An E2E check of the scroll-into-view of the focused input above a docked keyboard (the spacer is checked; the scroll
  itself depends on the page height).
- A native-speaker review of the long-press tables and the Arabic-script symbol layers (PLAN.md section 15, question 4).